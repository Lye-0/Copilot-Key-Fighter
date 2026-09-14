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
            // Ignore injected input, including our own sequences; never recursively trigger.
            if ((flags & 0x10) == 0)
            {
                bool down = message is 0x100 or 0x104;
                if (key < physical.Length) Volatile.Write(ref physical[key], down ? 1 : 0);
                if (down && key != 0x86) Interlocked.Increment(ref interference);
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
