using System.ComponentModel;
using System.Runtime.InteropServices;

internal sealed class KeyboardHook : IDisposable
{
    readonly Thread thread;
    readonly ManualResetEventSlim ready = new();
    readonly CopilotGesture gesture = new();
    readonly Native.HookProc callback;
    readonly nint destination;
    Exception? error;
    nint hook;
    uint threadId;
    int disposed;
    internal volatile bool Enabled;
    int interference;
    int lastInterference, f23Ups, f23InjectedUps;
    internal object ReleaseDiagnostic => new { F23Ups = Volatile.Read(ref f23Ups), F23InjectedUps = Volatile.Read(ref f23InjectedUps), LastInterference = Describe(Volatile.Read(ref lastInterference)) };
    static string Describe(int key) => KeyBinding.IsModifier(key) ? ((Keys)key).ToString() : key == 0xFC ? "internal-notification" : "other-key";
    internal static bool CancelsPending(uint key, bool down, bool wasDown, bool copilotCaptured = false)
        => down && !wasDown && key != 0x86 && !(copilotCaptured && key is 0x5B or 0xA0 or 0x10);
    readonly int[] physical = new int[256];
    internal bool PhysicallyDown(int key) => key >= 0 && key < physical.Length && Volatile.Read(ref physical[key]) != 0;
    internal int Interference => Volatile.Read(ref interference);
    internal KeyboardHook(nint destination)
    {
        this.destination = destination;
        callback = OnKey;
        thread = new Thread(Run) { IsBackground = true, Name = "Copilot input" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!ready.Wait(5000)) throw new TimeoutException("キー監視を開始できませんでした。");
        if (error != null) throw error;
    }
    void Run()
    {
        try
        {
            threadId = Native.GetCurrentThreadId();
            // Physical state comes from observed events, not reserved virtual-key snapshots.
            // ReleaseGate separately checks pre-existing modifiers and the target key.
            Native.PeekMessage(out _, 0, 0, 0, 0);
            hook = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
            if (hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            ready.Set();
            while (Native.GetMessage(out var message, 0, 0, 0) > 0) { Native.TranslateMessage(ref message); Native.DispatchMessage(ref message); }
        }
        catch (Exception ex) { error = ex; ready.Set(); }
        finally { if (hook != 0) Native.UnhookWindowsHookEx(hook); hook = 0; }
    }
    nint OnKey(int code, nint message, nint data)
    {
        if (code >= 0 && message is 0x100 or 0x104 or 0x101 or 0x105)
        {
            uint key = unchecked((uint)Marshal.ReadInt32(data));
            uint flags = unchecked((uint)Marshal.ReadInt32(data, 8));
            bool down = message is 0x100 or 0x104;
            if (key == 0x86 && !down) { Interlocked.Increment(ref f23Ups); if ((flags & 0x10) != 0) Interlocked.Increment(ref f23InjectedUps); }
            // Ignore injected input, including our own sequences; never recursively trigger.
            if ((flags & 0x10) == 0)
            {
                bool wasDown = key < physical.Length && Volatile.Read(ref physical[key]) != 0;
                if (key < physical.Length) Volatile.Write(ref physical[key], down ? 1 : 0);
                if (CancelsPending(key, down, wasDown, gesture.IsCaptured)) { Volatile.Write(ref lastInterference, (int)key); Interlocked.Increment(ref interference); }
                if (key == 0x86)
                {
                    var result = gesture.Process(key, down, Enabled && Native.Down(0x5B), Native.Down(0xA0), Native.Down(0x11) || Native.Down(0x12) || Native.Down(0x5C) || Native.Down(0xA1));
                    if (result.Trigger) Native.PostMessage(destination, Resident.CopilotMessage, Interference, Native.GetForegroundWindow());
                    if (result.Suppress) return 1;
                }
            }
        }
        return Native.CallNextHookEx(0, code, message, data);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Enabled = false;
        if (thread.IsAlive) { Native.PostThreadMessage(threadId, 0x12, 0, 0); thread.Join(2000); }
        ready.Dispose();
    }
}
