using System.Drawing;
using System.Runtime.InteropServices;

internal static class TargetIcon
{
    internal static Icon Load(AppTarget target)
    {
        nint pidl = 0;
        try
        {
            string path = target.Kind == TargetKind.Executable ? target.Id : "shell:AppsFolder\\" + target.Id;
            if (SHParseDisplayName(path, 0, out pidl, 0, out _) != 0) return (Icon)SystemIcons.Application.Clone();
            if (SHGetFileInfo(pidl, 0, out var info, (uint)Marshal.SizeOf<ShellFileInfo>(), 0x100 | 0x1 | 0x8) != 0 && info.Icon != 0)
            {
                try { return (Icon)Icon.FromHandle(info.Icon).Clone(); } finally { DestroyIcon(info.Icon); }
            }
        }
        catch { /* An unavailable icon does not make the application unselectable. */ }
        finally { if (pidl != 0) Marshal.FreeCoTaskMem(pidl); }
        return (Icon)SystemIcons.Application.Clone();
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct ShellFileInfo
    {
        internal nint Icon; internal int Index; internal uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] internal string TypeName;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern int SHParseDisplayName(string name, nint bind, out nint pidl, uint attributes, out uint result);
    [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)] static extern nint SHGetFileInfo(nint path, uint attributes, out ShellFileInfo info, uint size, uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(nint icon);
}
