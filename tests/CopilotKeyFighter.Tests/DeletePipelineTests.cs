using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xunit;

[Collection("Desktop")]
public class DeletePipelineTests
{
    [Fact]
    public async Task CopilotThroughNativeHookDeletesInOwnedTextBox()
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            string directory = Path.Combine(Path.GetTempPath(), "CKF-native-" + Guid.NewGuid());
            try
            {
                var store = new SettingsStore(directory);
                store.Save(new(Enabled:true, Action:ActionKind.Keyboard, Binding:new(0x2E)));
                // Only this isolated hook accepts this tag; the production entry point
                // always leaves testInputTag=0 and ignores all injected Copilot chords.
                const nuint tag = 0x434B4654;
                using var resident = new Resident(true, store, tag);
                using var fixture = new Form { Text = "Copilot Key Fighter — isolated input test", Width = 500, Height = 180 };
                var text = new TextBox { Text = "abc", Dock = DockStyle.Top };
                fixture.Controls.Add(text);
                fixture.Shown += async (_, _) =>
                {
                    try
                    {
                        for(int attempt=0; attempt<3; attempt++)
                        {
                        text.Text = "abc";
                        var hotkey = (ForegroundHotkey)typeof(Resident).GetField("foreground", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(resident)!;
                        bool targetBeforePrime = Native.Down(0x2E);
                        hotkey.Request(); await Task.Delay(100);
                        bool targetAfterPrime = Native.Down(0x2E);
                        fixture.Activate(); text.Focus(); text.SelectionStart = 0; text.SelectionLength = 0;
                        await Task.Delay(40);
                        Assert.Equal(fixture.Handle, Native.GetForegroundWindow());
                        Assert.True(text.Focused);
                        Assert.False(Native.Down(0x5B) || Native.Down(0x10) || Native.Down(0x11) || Native.Down(0x12));
                        var events = new[] { new KeyEvent(0x5B,false),new KeyEvent(0xA0,false),new KeyEvent(0x86,false),new KeyEvent(0x86,true),new KeyEvent(0xA0,true),new KeyEvent(0x5B,true) };
                        var input = events.Select(e => Native.Input.Key((uint)e.Key,e.Up,tag)).ToArray();
                        uint sent = Native.SendInput((uint)input.Length,input,Marshal.SizeOf<Native.Input>());
                        if(sent != input.Length)
                        {
                            var cleanup = KeySequence.Recovery(events,(int)sent).Select(e => Native.Input.Key((uint)e.Key,true,tag)).ToArray();
                            if(cleanup.Length>0) Native.SendInput((uint)cleanup.Length,cleanup,Marshal.SizeOf<Native.Input>());
                        }
                        Assert.Equal((uint)input.Length,sent);
                        await Task.Delay(2500);
                        string diagnostic = File.ReadAllText(Path.Combine(directory,"last-gesture.json"));
                        Assert.True(text.Text == "bc", $"BeforePrime={targetBeforePrime}, AfterPrime={targetAfterPrime}. Native pipeline did not delete. " + diagnostic);
                        Assert.False(Native.Down(0x5B) || Native.Down(0x10) || Native.Down(0x11) || Native.Down(0x12));
                        }
                        done.SetResult();
                    }
                    catch(Exception e) { done.TrySetException(e); }
                    finally { fixture.Close(); }
                };
                Application.Run(fixture);
            }
            catch(Exception e) { done.TrySetException(e); }
            finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        }) { IsBackground=true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }
}
