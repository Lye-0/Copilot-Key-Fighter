using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

public class PaintTests
{
    [Fact]
    public async Task OwnerDrawControlsCoverTheirBufferWithoutBackgroundErase()
    {
        var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            try
            {
                using var parent=new Panel {BackColor=Color.FromArgb(245,248,250)};
                using var button=new UiButton {Text="保存"};
                using var check=new UiCheck {Text="割り当てを有効にする"};
                using var chip=new UiCheck {Text="Ctrl",Chip=true};
                using var modes=new ModeSelector();
                parent.Controls.AddRange([button,check,chip,modes]);
                var controls=new Control[]{button,check,chip}.Concat(modes.Controls.Cast<Control>()).ToArray();
                foreach(var color in new[]{Color.FromArgb(245,248,250),Color.White})
                foreach(bool state in new[]{false,true})
                foreach(int width in new[]{220,340})
                {
                    parent.BackColor=color;check.Checked=chip.Checked=state;button.Enabled=state;
                    modes.SelectedIndex=state?1:0;
                    foreach(var control in controls)
                    {
                        control.Size=new Size(width,44);
                        using var bitmap=new Bitmap(width,44);
                        using var graphics=Graphics.FromImage(bitmap);
                        graphics.Clear(Color.Black);
                        // Simulate the actual paint pass without WM_ERASEBKGND. A full
                        // DrawToBitmap pass used to hide the unpainted buffer regions.
                        control.GetType().GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(control,[new PaintEventArgs(graphics,control.ClientRectangle)]);
                        Assert.Equal(color.ToArgb(),bitmap.GetPixel(0,0).ToArgb());
                        Assert.Equal(color.ToArgb(),bitmap.GetPixel(width-1,43).ToArgb());
                        if(ReferenceEquals(control,check)) Assert.Equal(color.ToArgb(),bitmap.GetPixel(width-8,20).ToArgb());
                    }
                }
                complete.SetResult();
            }
            catch(Exception e){complete.SetException(e);}
        }){IsBackground=true};
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
