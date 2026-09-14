using System.Windows.Forms;
using Xunit;

[CollectionDefinition("Desktop", DisableParallelization = true)]
public class DesktopCollection { }

[Collection("Desktop")]
public class WindowTests
{
    [Fact]
    public async Task SettingsEditorPreservesSelectedKeyAndAccessibleModes()
    {
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                Settings? applied=null;
                using var form = new SettingsForm(new(Action:ActionKind.Keyboard, Binding:new()), value => applied=value);
                static IEnumerable<Control> Walk(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] {x}.Concat(Walk(x)));
                var controls = Walk(form).ToArray();
                var key = Assert.Single(controls.OfType<Button>(),b=>b.Name=="KeyChoice");
                Assert.StartsWith("Delete",key.Text);
                Assert.Equal(3, controls.OfType<RadioButton>().Count());
                Assert.True(controls.OfType<RadioButton>().Single(x => x.Text == "キーを送る").Checked);
                form.Show();Application.DoEvents();
                var save=Assert.Single(controls.OfType<Button>(),b=>b.Name=="SaveSettings");
                var status=Assert.Single(controls.OfType<Label>(),b=>b.Name=="CurrentStatus");
                var enabled=Assert.Single(controls.OfType<CheckBox>(),b=>b.Text=="割り当てを有効にする");
                Assert.False(save.Enabled);Assert.Contains("停止中",status.Text);
                enabled.Checked=true;Assert.True(save.Enabled);Assert.Contains("停止中",status.Text);
                enabled.Checked=false;Assert.False(save.Enabled);
                enabled.Checked=true;save.PerformClick();Assert.NotNull(applied);Assert.True(applied.Enabled);
                Assert.Equal(46,applied.Binding!.Key);Assert.False(save.Enabled);Assert.Contains("有効",status.Text);
                form.UpdateRuntimeState(applied,true);Assert.Contains("一時停止中",status.Text);
                form.ClientSize=new System.Drawing.Size(720,640);Application.DoEvents();
                Assert.True(form.ClientRectangle.Contains(form.RectangleToClient(save.RectangleToScreen(save.ClientRectangle))));
                enabled.Checked=false;Application.DoEvents();
                Assert.True(form.ClientRectangle.Contains(form.RectangleToClient(save.RectangleToScreen(save.ClientRectangle))));
                enabled.Checked=true;
                form.Close();
                complete.SetResult();
            }
            catch (Exception e) { complete.SetException(e); }
        }) { IsBackground=true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
    [Fact]
    public async Task FindsAndRestoresExistingWindowAndHandlesForegroundDenial()
    {
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            using var fixture = new Form { Text = "Copilot Key Fighter window verification", Width = 400, Height = 180 };
            fixture.Shown += async (_, _) =>
            {
                try
                {
                    var target = new AppTarget(TargetKind.Executable, "Test fixture", Environment.ProcessPath!);
                    Assert.Equal(fixture.Handle, AppLauncher.FindWindow(target));
                    Assert.Equal(IntPtr.Zero, AppLauncher.FindWindow(target with { Id = @"C:\nonexistent\unrelated-app.exe" }));
                    fixture.WindowState = FormWindowState.Minimized;
                    try { await AppLauncher.OpenAsync(target, true); }
                    catch (InvalidOperationException e)
                    {
                        // A CI/test host has no last-input foreground grant. Refusal must not relaunch.
                        Assert.Contains("再起動は行っていません", e.Message);
                    }
                    Assert.NotEqual(FormWindowState.Minimized, fixture.WindowState);
                    Assert.Equal(fixture.Handle, AppLauncher.FindWindow(target));
                    complete.SetResult();
                }
                catch (Exception e) { complete.SetException(e); }
                finally { fixture.Close(); }
            };
            Application.Run(fixture);
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
