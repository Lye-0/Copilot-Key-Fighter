using System.Windows.Forms;
using Xunit;

[Collection("Desktop")]
public class NotificationTests
{
    [Fact]
    public async Task InternalPairDoesNotKeepReleaseGateClosed()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var receiver = new Receiver();
                using var hotkey = new ForegroundHotkey(receiver.Handle);
                using var timer = new System.Windows.Forms.Timer { Interval = 100 };
                timer.Tick += (_, _) =>
                {
                    try { Assert.False(Native.Down(0xFC), "Reserved key remains down"); completion.SetResult(); }
                    catch(Exception e) { completion.SetException(e); }
                    finally { timer.Stop(); Application.ExitThread(); }
                };
                hotkey.Request(); timer.Start(); Application.Run();
            }
            catch(Exception e) { completion.TrySetException(e); }
        }) { IsBackground=true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
    sealed class Receiver : NativeWindow, IDisposable
    {
        internal Receiver() => CreateHandle(new CreateParams { Parent = -3 });
        public void Dispose() => DestroyHandle();
    }
}
