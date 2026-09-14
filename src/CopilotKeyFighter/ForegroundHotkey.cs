using System.ComponentModel;
using System.Runtime.InteropServices;

// A real WM_HOTKEY notification grants foreground activation permission. A posted
// application message from a low-level hook does not. Use VK_NONAME (reserved,
// non-character key), never synthetic Win/Shift/Ctrl/Alt or input-queue attachment.
internal sealed class ForegroundHotkey : IDisposable
{
    const int FirstId = 0x4100;
    readonly nint window;
    int registered;
    bool armed;
    long armedAt;
    internal int Notifications { get; private set; }
    internal const uint ReservedKey = 0xFC;
    internal const nuint Tag = 0x43475054;

    internal ForegroundHotkey(nint window)
    {
        this.window = window;
        try
        {
            // Modifier releases may be in flight. Cover each combination without
            // reading, changing or replaying physical modifier state.
            for (uint modifiers = 0; modifiers < 16; modifiers++)
            {
                if (!Native.RegisterHotKey(window, FirstId + (int)modifiers, modifiers | 0x4000, ReservedKey))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Internal foreground hotkey registration failed.");
                registered++;
            }
        }
        catch { Dispose(); throw; }
    }

    internal void Request()
    {
        if (armed && Environment.TickCount64 - armedAt < 1500) return;
        armed = true;
        armedAt = Environment.TickCount64;
        var pair = new[] { Native.Input.Key(ReservedKey, false, Tag), Native.Input.Key(ReservedKey, true, Tag) };
        uint sent = Native.SendInput(2, pair, Marshal.SizeOf<Native.Input>());
        if (sent != 2)
        {
            int error = Marshal.GetLastWin32Error();
            // Balance a partial reserved-key down. Never release a physical modifier.
            if (sent == 1) Native.SendInput(1, new[] { pair[1] }, Marshal.SizeOf<Native.Input>());
            armed = false;
            throw new Win32Exception(error, "Internal foreground notification could not be delivered.");
        }
    }

    internal bool Accept(nint id)
    {
        if (id < FirstId || id >= FirstId + 16 || !armed) return false;
        armed = false;
        if (Environment.TickCount64 - armedAt >= 1500) return false;
        Notifications++;
        return true;
    }
    public void Dispose()
    {
        for (int i = 0; i < registered; i++) Native.UnregisterHotKey(window, FirstId + i);
        registered = 0;
    }
    internal void Cancel() => armed = false;
}
