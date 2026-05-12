using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WowSticky.Models;
using WowSticky.Services;
using WinBrush = System.Windows.Media.Brush;
using WinColor = System.Windows.Media.Color;
using WinBrushes = System.Windows.Media.Brushes;

namespace WowSticky.Windows;

public partial class NoteWindow : Window
{
    private const int MIN_WIDTH = 280;
    private const int MIN_HEIGHT = 300;

    private static readonly Dictionary<string, (string name, string border, string fg)> NotePalette = new()
    {
        ["#F5F0E8"] = ("offwhite", "#E0D8C8", "#1A1A1A"),
        ["#FFE066"] = ("yellow", "#E6C94D", "#1A1A1A"),
        ["#8CE99A"] = ("green", "#6DCF7A", "#1A1A1A"),
        ["#74C0FC"] = ("blue", "#5AA8E3", "#1A1A1A"),
        ["#FAA2C1"] = ("pink", "#E088A7", "#1A1A1A"),
        ["#B197FC"] = ("purple", "#977EE3", "#FFFFFF"),
        ["#FFA94D"] = ("orange", "#E69133", "#1A1A1A"),
        ["#66D9E8"] = ("teal", "#4DC0CF", "#1A1A1A"),
        ["#FF8787"] = ("red", "#E66E6E", "#FFFFFF"),
    };

    private static readonly string[] PaletteOrder =
        ["#FFE066", "#8CE99A", "#74C0FC", "#FAA2C1", "#B197FC", "#FFA94D", "#66D9E8", "#FF8787", "#F5F0E8"];

    private readonly NoteService _noteService;
    private readonly Note _note;
    private readonly DispatcherTimer _saveTimer;
    private bool _saving;
    private bool _isAlwaysOnTop = true;
    private bool _titleDirty;
    private bool _contentDirty;
    private bool _loaded;

    public NoteWindow(NoteService noteService, Note note)
    {
        InitializeComponent();
        _noteService = noteService;
        _note = note;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _saveTimer.Tick += SaveTimer_Tick;

        Width = Math.Max(280, note.Width);
        Height = Math.Max(300, note.Height + 40);
        Left = note.XPosition;
        Top = note.YPosition;
    }

    public event Action<string>? Dismissed;
    public string NoteId => _note.Id;
    public bool IsTrashed { get; private set; }

    // ─── Resize via Thumb controls ──────────────────────────

    private bool CanResize() => !_note.Pinned && !_note.Locked;

    private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newTop = Top + e.VerticalChange;
        var newHeight = Height - e.VerticalChange;
        if (newHeight >= MIN_HEIGHT) { Top = newTop; Height = newHeight; }
        else { Top = Top + Height - MIN_HEIGHT; Height = MIN_HEIGHT; }
    }

    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newHeight = Height + e.VerticalChange;
        Height = newHeight >= MIN_HEIGHT ? newHeight : MIN_HEIGHT;
    }

    private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newLeft = Left + e.HorizontalChange;
        var newWidth = Width - e.HorizontalChange;
        if (newWidth >= MIN_WIDTH) { Left = newLeft; Width = newWidth; }
        else { Left = Left + Width - MIN_WIDTH; Width = MIN_WIDTH; }
    }

    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newWidth = Width + e.HorizontalChange;
        Width = newWidth >= MIN_WIDTH ? newWidth : MIN_WIDTH;
    }

    private void ResizeTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newTop = Top + e.VerticalChange;
        var newHeight = Height - e.VerticalChange;
        if (newHeight >= MIN_HEIGHT) { Top = newTop; Height = newHeight; }
        else { Top = Top + Height - MIN_HEIGHT; Height = MIN_HEIGHT; }

        var newLeft = Left + e.HorizontalChange;
        var newWidth = Width - e.HorizontalChange;
        if (newWidth >= MIN_WIDTH) { Left = newLeft; Width = newWidth; }
        else { Left = Left + Width - MIN_WIDTH; Width = MIN_WIDTH; }
    }

    private void ResizeTopRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newTop = Top + e.VerticalChange;
        var newHeight = Height - e.VerticalChange;
        if (newHeight >= MIN_HEIGHT) { Top = newTop; Height = newHeight; }
        else { Top = Top + Height - MIN_HEIGHT; Height = MIN_HEIGHT; }

        var newWidth = Width + e.HorizontalChange;
        if (newWidth >= MIN_WIDTH) Width = newWidth;
        else Width = MIN_WIDTH;
    }

    private void ResizeBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newHeight = Height + e.VerticalChange;
        Height = newHeight >= MIN_HEIGHT ? newHeight : MIN_HEIGHT;

        var newLeft = Left + e.HorizontalChange;
        var newWidth = Width - e.HorizontalChange;
        if (newWidth >= MIN_WIDTH) { Left = newLeft; Width = newWidth; }
        else { Left = Left + Width - MIN_WIDTH; Width = MIN_WIDTH; }
    }

    private void ResizeBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!CanResize()) return;
        var newHeight = Height + e.VerticalChange;
        Height = newHeight >= MIN_HEIGHT ? newHeight : MIN_HEIGHT;

        var newWidth = Width + e.HorizontalChange;
        Width = newWidth >= MIN_WIDTH ? newWidth : MIN_WIDTH;
    }

    // ─── Load ────────────────────────────────────────────────

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyNoteData();
        _loaded = true;
    }

    private void ApplyNoteData()
    {
        var palette = NotePalette.TryGetValue(_note.Color, out var p) ? p : NotePalette["#FFE066"];
        NoteBorder.Background = ParseColor(_note.Color);
        NoteBorder.BorderBrush = ParseColor(p.border);

        var fgBrush = ParseColor(p.fg);
        ColorBtn.Foreground = fgBrush;
        TitleBox.Text = _note.Title;
        TitleBox.Foreground = fgBrush;
        TitleBox.CaretBrush = fgBrush;

        ContentBox.Text = _note.Content;
        ContentBox.Foreground = fgBrush;
        DateLabel.Text = FormatDate(_note.UpdatedAt);
        DateLabel.Foreground = fgBrush;
        TitleLabel.Text = string.IsNullOrEmpty(_note.Title) ? "Untitled" : _note.Title;
        TitleLabel.Foreground = fgBrush;
        FontSizeLabel.Text = _note.FontSize.ToString();
        FontSizeLabel.Foreground = fgBrush;
        ContentBox.FontSize = _note.FontSize;
        ApplyPinState();
        UpdateTitlePlaceholder();

        AlwaysOnTopBtn.Foreground = _isAlwaysOnTop
            ? new SolidColorBrush(WinColor.FromRgb(255, 224, 102))
            : fgBrush;
        UpdateReminderButtonState();
    }

    private void UpdateTitlePlaceholder()
    {
        TitlePlaceholder.Visibility = string.IsNullOrEmpty(TitleBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyPinState()
    {
        PinBtn.Opacity = _note.Pinned ? 1.0 : 0.4;
        PinBtn.ToolTip = _note.Pinned ? "Pinned — position locked" : "Pin to lock position";
        LockBtn.Opacity = _note.Locked ? 1.0 : 0.4;
        ContentBox.IsReadOnly = _note.Locked;
        TitleBox.IsReadOnly = _note.Locked;
    }

    private static WinBrush ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        var b = Convert.FromHexString(hex);
        return new SolidColorBrush(WinColor.FromArgb(b[0], b[1], b[2], b[3]));
    }

    private static string FormatDate(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
            return dt.ToLocalTime().ToString("M/d/yyyy h:mm tt");
        return iso;
    }

    private string NextColor()
    {
        var idx = Array.IndexOf(PaletteOrder, _note.Color);
        return idx >= 0 ? PaletteOrder[(idx + 1) % PaletteOrder.Length] : PaletteOrder[0];
    }

    public void SavePosition()
    {
        _noteService.UpdatePosition(_note.Id, Left, Top);
        _noteService.UpdateSize(_note.Id, Width, Height - 40);
    }

    // ─── Drag ─────────────────────────────────────────────────
    private bool CanDrag() => !_note.Pinned && !_note.Locked;

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && CanDrag())
            DragMove();
    }

    private void Footer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && CanDrag())
            DragMove();
    }

    // ─── Auto-Save ────────────────────────────────────────────
    private void MarkDirty() { if (!_saveTimer.IsEnabled) _saveTimer.Start(); }

    private void Title_Changed(object sender, TextChangedEventArgs e)
    {
        UpdateTitlePlaceholder();
        if (_loaded) MarkDirty();
        _titleDirty = true;
    }

    private void Content_Changed(object sender, TextChangedEventArgs e) { if (_loaded) MarkDirty(); _contentDirty = true; }

    private void SyncNoteFromUI()
    {
        _note.Title = TitleBox.Text;
        _note.Content = ContentBox.Text;
    }

    private async void SaveTimer_Tick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        if (_saving) return;
        _saving = true;

        try
        {
            if (_titleDirty || _contentDirty)
            {
                var title = _titleDirty ? TitleBox.Text : null;
                var content = _contentDirty ? ContentBox.Text : null;
                await Task.Run(() => _noteService.Update(_note.Id, title, content));
                SyncNoteFromUI();
                _titleDirty = false;
                _contentDirty = false;

                TitleLabel.Text = string.IsNullOrEmpty(TitleBox.Text) ? "Untitled" : TitleBox.Text;
                DateLabel.Text = FormatDate(DateTime.UtcNow.ToString("o"));
            }
        }
        finally { _saving = false; }
    }

    // ─── Button Handlers ──────────────────────────────────────
    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _noteService.TogglePin(_note.Id);
        _note.Pinned = !_note.Pinned;
        ApplyPinState();
    }

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        _noteService.ToggleLock(_note.Id);
        _note.Locked = !_note.Locked;
        ApplyPinState();
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        SyncNoteFromUI();
        var newColor = NextColor();
        _note.Color = newColor;
        _noteService.Update(_note.Id, color: newColor);
        ApplyNoteData();
    }

    private void FontDown_Click(object sender, RoutedEventArgs e)
    {
        _note.FontSize = Math.Max(12, _note.FontSize - 2);
        ContentBox.FontSize = _note.FontSize;
        FontSizeLabel.Text = _note.FontSize.ToString();
        _noteService.Update(_note.Id, fontSize: _note.FontSize);
    }

    private void FontUp_Click(object sender, RoutedEventArgs e)
    {
        _note.FontSize = Math.Min(24, _note.FontSize + 2);
        ContentBox.FontSize = _note.FontSize;
        FontSizeLabel.Text = _note.FontSize.ToString();
        _noteService.Update(_note.Id, fontSize: _note.FontSize);
    }

    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        _isAlwaysOnTop = !_isAlwaysOnTop;
        Topmost = _isAlwaysOnTop;
        AlwaysOnTopBtn.Foreground = _isAlwaysOnTop
            ? new SolidColorBrush(WinColor.FromRgb(255, 224, 102))
            : WinBrushes.White;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ReminderPanel.Visibility = Visibility.Collapsed;
        Dismissed?.Invoke(_note.Id);
        Hide();
    }

    // ─── Reminder ─────────────────────────────────────────────

    private void Reminder_Click(object sender, RoutedEventArgs e)
    {
        ReminderPanel.Visibility = ReminderPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (ReminderPanel.Visibility == Visibility.Collapsed) return;

        if (_note.ReminderEnabled && !string.IsNullOrEmpty(_note.ReminderNextAt)
            && DateTime.TryParse(_note.ReminderNextAt, out var nextAt))
        {
            ReminderDatePicker.SelectedDate = nextAt.Date;
            ReminderHourBox.Text = nextAt.ToString("h");
            ReminderMinuteBox.Text = nextAt.ToString("mm");
            ReminderAmPmCombo.SelectedIndex = nextAt.ToString("tt") == "PM" ? 1 : 0;
            SelectInterval(_note.ReminderInterval);
            RemoveReminderBtn.IsEnabled = true;
        }
        else
        {
            ReminderDatePicker.SelectedDate = DateTime.Today;
            var def = DateTime.Now.AddHours(1);
            ReminderHourBox.Text = def.ToString("h");
            ReminderMinuteBox.Text = def.ToString("mm");
            ReminderAmPmCombo.SelectedIndex = def.ToString("tt") == "PM" ? 1 : 0;
            ReminderIntervalCombo.SelectedIndex = 0;
            RemoveReminderBtn.IsEnabled = false;
        }

        ReminderDatePicker.Focus();
    }

    private TimeOnly? ParseReminderTime()
    {
        if (!int.TryParse(ReminderHourBox.Text, out var hour) || hour < 1 || hour > 12)
        {
            System.Windows.MessageBox.Show("Enter a valid hour (1-12).");
            return null;
        }
        if (!int.TryParse(ReminderMinuteBox.Text, out var min) || min < 0 || min > 59)
        {
            System.Windows.MessageBox.Show("Enter a valid minute (0-59).");
            return null;
        }
        if (ReminderAmPmCombo.SelectedItem is ComboBoxItem ampm)
        {
            if (ampm.Content.ToString() == "PM" && hour != 12) hour += 12;
            else if (ampm.Content.ToString() == "AM" && hour == 12) hour = 0;
        }
        return new TimeOnly(hour, min);
    }

    private void SetReminder_Click(object sender, RoutedEventArgs e)
    {
        var date = ReminderDatePicker.SelectedDate;
        if (date == null) return;

        var time = ParseReminderTime();
        if (time == null) return;

        var interval = GetSelectedIntervalTag();
        var dateTime = date.Value.Date + time.Value.ToTimeSpan();

        if (interval == "firstday")
            dateTime = new DateTime(dateTime.Year, dateTime.Month, 1).Date + dateTime.TimeOfDay;
        else if (interval == "lastday")
            dateTime = new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddDays(-1).Date + dateTime.TimeOfDay;

        if (dateTime <= DateTime.Now)
        {
            var next = Services.ReminderService.ComputeNextReminder(interval, dateTime);
            if (next.HasValue) dateTime = next.Value;
        }

        _note.ReminderEnabled = true;
        _note.ReminderNextAt = dateTime.ToString("o");
        _note.ReminderInterval = interval;
        _noteService.UpdateReminder(_note.Id, true, dateTime.ToString("o"), interval);

        UpdateReminderButtonState();
        ReminderPanel.Visibility = Visibility.Collapsed;
    }

    private void RemoveReminder_Click(object sender, RoutedEventArgs e)
    {
        _note.ReminderEnabled = false;
        _note.ReminderNextAt = null;
        _note.ReminderInterval = "once";
        _noteService.RemoveReminder(_note.Id);

        UpdateReminderButtonState();
        ReminderPanel.Visibility = Visibility.Collapsed;
    }

    private string GetSelectedIntervalTag()
    {
        if (ReminderIntervalCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            return tag;
        return "once";
    }

    private void SelectInterval(string interval)
    {
        foreach (ComboBoxItem item in ReminderIntervalCombo.Items)
        {
            if (item.Tag is string tag && tag == interval)
            {
                ReminderIntervalCombo.SelectedItem = item;
                return;
            }
        }
        ReminderIntervalCombo.SelectedIndex = 0;
    }

    private void UpdateReminderButtonState()
    {
        if (_note.ReminderEnabled && _note.ReminderNextAt != null
            && DateTime.TryParse(_note.ReminderNextAt, out var nextAt))
        {
            ReminderBtn.Content = "🔔";
            ReminderBtn.Opacity = 1.0;
            ReminderBtn.ToolTip = $"Reminder: {nextAt:g} ({_note.ReminderInterval})";
        }
        else
        {
            ReminderBtn.Content = "🔔";
            ReminderBtn.Opacity = 0.7;
            ReminderBtn.ToolTip = "Set reminder";
        }
    }

    private void Trash_Click(object sender, RoutedEventArgs e)
    {
        _noteService.Trash(_note.Id);
        IsTrashed = true;
        Close();
    }

    // ─── Position Persistence ─────────────────────────────────
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _saveTimer.Stop();
        if (!IsTrashed)
        {
            _noteService.UpdatePosition(_note.Id, Left, Top);
            _noteService.UpdateSize(_note.Id, Width, Height - 40);
        }
        base.OnClosing(e);
    }
}
