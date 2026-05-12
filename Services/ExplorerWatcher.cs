using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace WowSticky.Services;

public class ExplorerWatcher
{
    private static readonly Guid ShellAppClsid = new("13709620-C279-11CE-A49E-444553540000");
    private static readonly string CurrentProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLowerInvariant();
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private string? _currentPath;
    private IntPtr _activeExplorerHwnd;

    public event Action<string, IntPtr>? FolderActivated;
    public event Action? FolderDeactivated;
    public string? CurrentPath => _currentPath;

    public ExplorerWatcher() => _timer.Tick += OnTick;
    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        try { PollExplorer(); }
        catch { }
    }

    private void PollExplorer()
    {
        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero) { DeactivateIfActive(); return; }

        GetWindowThreadProcessId(fg, out var pid);
        string proc;
        try { proc = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant(); }
        catch { DeactivateIfActive(); return; }
        if (proc != "explorer" && proc != CurrentProcessName) { DeactivateIfActive(); return; }

        if (proc == CurrentProcessName)
        {
            if (_activeExplorerHwnd != IntPtr.Zero && (IsIconic(_activeExplorerHwnd) || !IsWindowVisible(_activeExplorerHwnd)))
                DeactivateIfActive();
            return;
        }

        if (IsIconic(fg) || !IsWindowVisible(fg)) { DeactivateIfActive(); return; }

        string? matchedPath = null;
        try
        {
            var shellType = Type.GetTypeFromCLSID(ShellAppClsid);
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic windows = shell.Windows();
                int count = windows.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic wnd = windows.Item(i);
                        if (wnd == null) continue;
                        var wh = new IntPtr((long)wnd.HWND);
                        GetWindowThreadProcessId(wh, out var wp);
                        if (wp != pid) continue;
                        if (IsIconic(wh) || !IsWindowVisible(wh)) continue;
                        dynamic doc = wnd.Document;
                        if (doc == null) continue;
                        dynamic folder = doc.Folder;
                        if (folder == null) continue;
                        matchedPath = (string)folder.Self.Path;
                        break;
                    }
                    catch { }
                }
                Marshal.ReleaseComObject(shell);
            }
        }
        catch { }

        // Desktop fallback: any unmatched explorer foreground is the desktop
        if (matchedPath == null)
            matchedPath = DesktopPath;

        if (matchedPath != _currentPath)
        {
            _currentPath = matchedPath;
            FolderActivated?.Invoke(matchedPath, fg);
        }
        _activeExplorerHwnd = fg;
    }

    private void DeactivateIfActive()
    {
        if (_currentPath != null)
        {
            _currentPath = null;
            _activeExplorerHwnd = IntPtr.Zero;
            FolderDeactivated?.Invoke();
        }
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

    public void Dispose() => _timer.Stop();
}
