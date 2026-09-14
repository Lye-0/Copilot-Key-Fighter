using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class AppLauncher
{
    internal static async Task OpenAsync(AppTarget target, bool preferExisting, CancellationToken cancellation = default)
    {
        nint window = preferExisting ? FindWindow(target) : 0;
        if (window != 0)
        {
            if (await RestoreAsync(window, cancellation)) return;
            throw new InvalidOperationException("既存ウィンドウを前面にできませんでした。アプリの再起動は行っていません。");
        }
        if (target.Kind == TargetKind.Packaged)
        {
            var manager = (IApplicationActivationManager)new ApplicationActivationManager();
            try { CoAllowSetForegroundWindow(manager, 0); Marshal.ThrowExceptionForHR(manager.ActivateApplication(target.Id, target.Arguments, 0, out _)); }
            finally { Marshal.ReleaseComObject(manager); }
        }
        else if (target.Kind == TargetKind.Executable)
        {
            if (!File.Exists(target.Id)) throw new FileNotFoundException("アプリが見つかりません。設定から選び直してください。");
            using var process = Process.Start(new ProcessStartInfo(target.Id)
            {
                Arguments = target.Arguments,
                WorkingDirectory = target.WorkingDirectory.Length > 0 ? target.WorkingDirectory : Path.GetDirectoryName(target.Id)!,
                UseShellExecute = false
            });
        }
        else AppCatalog.LaunchShell(target.Id);
        // The target may have no window. Activation success remains success, and never retries launch.
        if (!preferExisting) return;
        var timer = Stopwatch.StartNew();
        do
        {
            cancellation.ThrowIfCancellationRequested();
            window = FindWindow(target);
            if (window != 0) { await RestoreAsync(window, cancellation); return; }
            await Task.Delay(75, cancellation);
        } while (timer.ElapsedMilliseconds < 5000);
    }
    static async Task<bool> RestoreAsync(nint window, CancellationToken cancellation)
    {
        if (Native.IsIconic(window)) ShowWindowAsync(window, 9);
        SetForegroundWindow(window);
        for (int i = 0; i < 12; i++)
        {
            if (Native.GetForegroundWindow() == window && !Native.IsIconic(window)) return true;
            await Task.Delay(25, cancellation);
        }
        return false;
    }
    internal static nint FindWindow(AppTarget target)
    {
        nint found = 0;
        var cache = new Dictionary<uint, bool>();
        bool Matches(nint window)
        {
            Native.GetWindowThreadProcessId(window, out uint pid);
            if (cache.TryGetValue(pid, out bool value)) return value;
            bool match = false;
            nint process = OpenProcess(0x1000, false, pid);
            if (process != 0)
            {
                try
                {
                    uint size = 1024;
                    var id = new StringBuilder((int)size);
                    match = target.Kind != TargetKind.Executable && GetApplicationUserModelId(process, ref size, id) == 0 && string.Equals(id.ToString(), target.Id, StringComparison.OrdinalIgnoreCase);
                    string path = target.Kind == TargetKind.Executable ? target.Id : target.Executable;
                    if (!match && path.Length > 0)
                    {
                        size = 32768; var buffer = new StringBuilder((int)size);
                        match = QueryFullProcessImageName(process, 0, buffer, ref size) && string.Equals(buffer.ToString(), path, StringComparison.OrdinalIgnoreCase);
                    }
                }
                finally { CloseHandle(process); }
            }
            cache[pid] = match;
            return match;
        }
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || GetWindow(window, 4) != 0 || GetWindowTextLengthW(window) == 0) return true;
            if (DwmGetWindowAttribute(window, 14, out int cloaked, 4) == 0 && cloaked != 0) return true;
            bool match = Matches(window);
            if (!match && target.Kind == TargetKind.Packaged)
                EnumChildWindows(window, (child, _) => { if (Matches(child)) { match = true; return false; } return true; }, 0);
            if (!match) return true;
            found = window; return false;
        }, 0);
        return found;
    }
    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")] class ApplicationActivationManager { }
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IApplicationActivationManager
    {
        [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appId, [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint pid);
        [PreserveSig] int ActivateForFile(nint appId, nint items, nint verb, out uint pid);
        [PreserveSig] int ActivateForProtocol(nint appId, nint items, out uint pid);
    }
    delegate bool EnumWindowsProc(nint window, nint data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc callback, nint data);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(nint window, EnumWindowsProc callback, nint data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextLengthW(nint window);
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(nint window, int command);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint window);
    [DllImport("kernel32.dll")] static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(nint process);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(nint process, ref uint size, StringBuilder id);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, EntryPoint="QueryFullProcessImageNameW")] static extern bool QueryFullProcessImageName(nint process, uint flags, StringBuilder path, ref uint size);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(nint window, uint attribute, out int value, int size);
    [DllImport("ole32.dll")] static extern int CoAllowSetForegroundWindow([MarshalAs(UnmanagedType.IUnknown)] object instance, nint reserved);
}
