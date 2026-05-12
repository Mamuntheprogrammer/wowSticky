using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using WowSticky.Models;
using WowSticky.Services;
using WowSticky.Windows;

namespace WowSticky;

public partial class App
{
    private static Mutex _singleInstanceMutex = null!;
    private DatabaseService _db = null!;
    private FolderService _folderService = null!;
    private NoteService _noteService = null!;
    private ReminderService _reminderService = null!;
    private ExplorerWatcher _explorerWatcher = null!;
    private System.Windows.Forms.NotifyIcon _trayIcon = null!;
    private System.Windows.Forms.ContextMenuStrip _trayMenu = null!;

    private readonly Dictionary<string, NoteWindow> _noteWindows = new();
    private readonly Dictionary<string, ReminderWindow> _reminderWindows = new();
    private readonly HashSet<string> _dismissedNoteIds = new();
    private readonly HashSet<string> _reminderActiveNoteIds = new();
    private Folder? _currentFolder;
    private EventWaitHandle _showEvent = null!;
    private HotkeyMessageWindow _hotkeyWnd = null!;
    private volatile bool _stopping;

    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WowSticky");

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, "WowSticky-SingleInstance", out var firstInstance);
        if (!firstInstance)
        {
            SignalExistingInstance();
            Environment.Exit(0);
            return;
        }

        DispatcherUnhandledException += (_, e) =>
        {
            File.AppendAllText(Path.Combine(AppDataDir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI: {e.Exception}\n");
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            File.AppendAllText(Path.Combine(AppDataDir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL: {e.ExceptionObject}\n");
        };

        try
        {
            Directory.CreateDirectory(AppDataDir);
            var dbPath = Path.Combine(AppDataDir, "sticky-notes.db");
            _db = new DatabaseService(dbPath);
            _folderService = new FolderService(_db);
            _noteService = new NoteService(_db);

            SeedIfEmpty();
            CreateTrayIcon();

            _reminderService = new ReminderService(_noteService);
            _reminderService.Start(OnReminderTriggered);

            _explorerWatcher = new ExplorerWatcher();
            _explorerWatcher.FolderActivated += OnFolderActivated;
            _explorerWatcher.FolderDeactivated += OnFolderDeactivated;
            _explorerWatcher.Start();

            // Second-instance listener
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "WowSticky-ShowEvent");
            Task.Run(ListenForShowSignal);

            // Startup tray balloon
            _trayIcon.ShowBalloonTip(3000, "WowSticky",
                "Running in the system tray. Right-click the icon or press Ctrl+Shift+W for quick access.",
                System.Windows.Forms.ToolTipIcon.Info);

            RegisterHotkey();
            SetAutoStartup();
            ShowTutorialIfFirstRun();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup error: {ex.Message}", "WowSticky");
        }
    }

    // ─── Single Instance ──────────────────────────────────────

    private static void SignalExistingInstance()
    {
        try
        {
            using var evt = EventWaitHandle.OpenExisting("WowSticky-ShowEvent");
            evt.Set();
        }
        catch { }
    }

    private void ListenForShowSignal()
    {
        while (!_stopping)
        {
            try
            {
                _showEvent.WaitOne();
                if (!_stopping)
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon.ShowBalloonTip(3000, "WowSticky",
                            "Already running in the system tray. Right-click here for options.",
                            System.Windows.Forms.ToolTipIcon.Info);
                        ShowAllNotes();
                    });
            }
            catch { break; }
        }
    }

    // ─── Global Hotkey ───────────────────────────────────────

    private void RegisterHotkey()
    {
        _hotkeyWnd = new HotkeyMessageWindow();
        _hotkeyWnd.CreateHandle(new System.Windows.Forms.CreateParams { Caption = "WowStickyHotkey" });
        _hotkeyWnd.HotkeyPressed += OnHotkeyPressed;
        RegisterHotKey(_hotkeyWnd.Handle, 1, MOD_CONTROL | MOD_SHIFT, (uint)System.Windows.Forms.Keys.W);
    }

    private class HotkeyMessageWindow : System.Windows.Forms.NativeWindow
    {
        public event Action? HotkeyPressed;
        public void Create() => CreateHandle(new System.Windows.Forms.CreateParams { Caption = "WowStickyHotkey" });
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_HOTKEY) HotkeyPressed?.Invoke();
            base.WndProc(ref m);
        }
    }

    private void OnHotkeyPressed() => Dispatcher.Invoke(ToggleAllNotes);

    private void ToggleAllNotes()
    {
        if (_noteWindows.Count > 0 && _noteWindows.Values.Any(w => w.Visibility == Visibility.Visible))
            HideAllNotes();
        else
            ShowAllNotes();
    }

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ─── Auto-Startup ────────────────────────────────────────

    private void SetAutoStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            var exePath = Environment.ProcessPath;
            if (exePath != null)
                key.SetValue("WowSticky", $"\"{exePath}\"");
        }
        catch { }
    }

    // ─── Tutorial ─────────────────────────────────────────────

    private void ShowTutorialIfFirstRun()
    {
        var flagFile = Path.Combine(AppDataDir, ".first-run-complete");
        if (File.Exists(flagFile)) return;

        var tutorial = new TutorialWindow();
        tutorial.ShowDialog();
        File.WriteAllText(flagFile, "done");
    }

    // ─── Seed ─────────────────────────────────────────────────

    private void SeedIfEmpty()
    {
        if (_folderService.GetAll().Count > 0) return;

        var desktop = _folderService.Create(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "#6366f1", "folder");
        _noteService.Create(desktop.Id, 150, 150);

        var cDrive = _folderService.Create("C:\\", "#818CF8", "folder");
        _noteService.Create(cDrive.Id, 150, 300);
    }

    // ─── Tray Icon ────────────────────────────────────────────

    private void CreateTrayIcon()
    {
        _trayMenu = new System.Windows.Forms.ContextMenuStrip();
        var folderItem = new System.Windows.Forms.ToolStripMenuItem("No folder active") { Enabled = false };
        _trayMenu.Items.Add(folderItem);
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("New Note Here", null, (_, _) => CreateNewNote()));
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show All Notes", null, (_, _) => ShowAllNotes()));
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Hide All Notes", null, (_, _) => HideAllNotes()));
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Open Data Folder", null, (_, _) =>
            System.Diagnostics.Process.Start("explorer.exe", AppDataDir)));
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("About WowSticky", null, (_, _) => ShowAboutDialog()));
        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Quit", null, (_, _) => ShutdownApp()));

        _trayMenu.Tag = folderItem;

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "Wow Sticky  (Ctrl+Shift+W)",
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        _trayIcon.DoubleClick += (_, _) => ShowAllNotes();
    }

    private static Icon CreateAppIcon()
    {
        try
        {
            var asm = typeof(App).Assembly;
            using var stream = asm.GetManifestResourceStream("WowSticky.icons.Square44x44Logo.targetsize-32.png");
            if (stream != null)
            {
                using var bmp = new Bitmap(stream);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
        catch { }

        using var fbmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(fbmp);
        g.Clear(Color.Transparent);
        using var bg = new SolidBrush(System.Drawing.Color.FromArgb(99, 54, 241));
        g.FillRoundedRectangle(bg, 2, 2, 28, 28, 6);
        using var tb = new SolidBrush(System.Drawing.Color.White);
        using var f = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString("W", f, tb, new RectangleF(2, 2, 28, 28),
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        return Icon.FromHandle(fbmp.GetHicon());
    }

    private static void ShowAboutDialog()
    {
        new Windows.AboutWindow().ShowDialog();
    }

    // ─── Reminder ─────────────────────────────────────────────

    private void OnReminderTriggered(Note note)
    {
        _dismissedNoteIds.Remove(note.Id);
        _reminderActiveNoteIds.Add(note.Id);

        if (_reminderWindows.TryGetValue(note.Id, out var existing))
        {
            existing.Topmost = true;
            existing.Show();
            existing.Focus();
            return;
        }

        var folder = _folderService.GetById(note.FolderId);
        var folderName = folder?.Name ?? "Unknown";
        var popup = new ReminderWindow(note, folderName);
        popup.Dismissed += OnReminderDismissed;
        popup.Show();
        _reminderWindows[note.Id] = popup;
        popup.Closed += (_, _) => _reminderWindows.Remove(note.Id);
    }

    private void OnReminderDismissed(string noteId)
    {
        _reminderActiveNoteIds.Remove(noteId);
    }

    // ─── Folder Events ────────────────────────────────────────

    private void OnFolderActivated(string path, IntPtr hwnd)
    {
        try
        {
            var folder = _folderService.GetByPath(path) ?? _folderService.Create(path);
            _currentFolder = folder;
            UpdateTrayMenu(path);
            OpenFolderNotes(folder);
        }
        catch { }
    }

    private void OnFolderDeactivated()
    {
        _currentFolder = null;
        UpdateTrayMenu(null);
        HideAllNotes();
    }

    private void UpdateTrayMenu(string? path)
    {
        if (_trayMenu.Tag is not System.Windows.Forms.ToolStripMenuItem folderItem) return;
        folderItem.Text = path != null ? $"📁 {path}" : "No folder active";
    }

    // ─── Note Windows ─────────────────────────────────────────

    private void SaveAllNotePositions()
    {
        foreach (var w in _noteWindows.Values) w.SavePosition();
    }

    private void OpenFolderNotes(Folder folder)
    {
        SaveAllNotePositions();
        var notes = _noteService.GetByFolder(folder.Id);
        var ids = notes.Select(n => n.Id).ToHashSet();

        foreach (var w in _noteWindows.Values)
            if (!ids.Contains(w.NoteId) && !_reminderActiveNoteIds.Contains(w.NoteId))
                w.Hide();

        foreach (var note in notes)
        {
            if (_dismissedNoteIds.Contains(note.Id)) continue;

            if (_noteWindows.TryGetValue(note.Id, out var existing))
            {
                existing.Topmost = true;
                existing.Show();
                existing.Focus();
            }
            else
            {
                var win = new NoteWindow(_noteService, note);
                win.Dismissed += OnNoteDismissed;
                win.Show();
                _noteWindows[note.Id] = win;
                win.Closed += (_, _) => _noteWindows.Remove(note.Id);
            }
        }
    }

    private void OnNoteDismissed(string id)
    {
        _dismissedNoteIds.Add(id);
        _reminderActiveNoteIds.Remove(id);
        if (_reminderWindows.TryGetValue(id, out var rw))
            rw.Close();
    }

    private void ShowAllNotes()
    {
        _dismissedNoteIds.Clear();
        foreach (var w in _noteWindows.Values) { w.Show(); w.Focus(); }
    }

    private void HideAllNotes()
    {
        SaveAllNotePositions();
        foreach (var w in _noteWindows.Values)
            if (!_reminderActiveNoteIds.Contains(w.NoteId))
                w.Hide();
    }

    private void CreateNewNote()
    {
        if (_currentFolder == null) return;
        var note = _noteService.Create(_currentFolder.Id);
        var win = new NoteWindow(_noteService, note);
        win.Dismissed += OnNoteDismissed;
        win.Show();
        _noteWindows[note.Id] = win;
        win.Closed += (_, _) => _noteWindows.Remove(note.Id);
    }

    // ─── Shutdown ─────────────────────────────────────────────

    private void ShutdownApp()
    {
        _stopping = true;
        _reminderService.Stop();
        _explorerWatcher.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        foreach (var w in _noteWindows.Values) w.Close();
        _noteWindows.Clear();
        foreach (var w in _reminderWindows.Values) w.Close();
        _reminderWindows.Clear();
        UnregisterHotKey(_hotkeyWnd.Handle, 1);
        _hotkeyWnd.DestroyHandle();
        _showEvent?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _stopping = true;
        _reminderService?.Stop();
        _explorerWatcher?.Dispose();
        _trayIcon?.Dispose();
        _showEvent?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float w, float h, float r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
        path.Dispose();
    }
}
