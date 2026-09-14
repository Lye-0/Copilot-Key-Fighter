using System.Windows.Forms;
using Xunit;

[Collection("Desktop")]
public class PickerScrollTests
{
    [Fact]
    public void WheelMovementIsBoundedAndDirectionChangesDiscardOldRemainder()
    {
        var wheel=new WheelSteps();
        Assert.Equal(-1,wheel.Read(120));Assert.Equal(-1,wheel.Read(720));
        Assert.Equal(1,wheel.Read(-120));Assert.Equal(1,wheel.Read(-720));
        Assert.Equal(0,wheel.Read(60));Assert.Equal(-1,wheel.Read(60));
        Assert.Equal(0,wheel.Read(60));Assert.Equal(0,wheel.Read(-60));
        Assert.Equal(1,wheel.Read(-60));Assert.Equal(0,wheel.Read(0));
    }

    [Fact]
    public async Task NativeWheelMessagesMoveOneRowWithoutChangingSelection()
    {
        var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            try
            {
                using var form=new Form();
                using var list=new SteadyListBox{Dock=DockStyle.Fill};form.Controls.Add(list);
                list.Items.AddRange(Enumerable.Range(0,100).Select(x=>(object)x).ToArray());
                form.Show();Application.DoEvents();list.SelectedIndex=10;list.TopIndex=10;
                var wndProc=typeof(SteadyListBox).GetMethod("WndProc",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
                void Wheel(int delta){var message=Message.Create(list.Handle,0x20A,(IntPtr)(delta<<16),IntPtr.Zero);wndProc.Invoke(list,new object[]{message});}
                Wheel(-720);Assert.Equal(11,list.TopIndex);Assert.Equal(10,list.SelectedIndex);
                Wheel(120);Assert.Equal(10,list.TopIndex);
                Wheel(-60);Assert.Equal(10,list.TopIndex);Wheel(-60);Assert.Equal(11,list.TopIndex);
                list.TopIndex=0;Wheel(720);Assert.Equal(0,list.TopIndex);
                using var chord=new KeyChordView();chord.SetBinding(new(37,true,true));
                Assert.Equal(new[]{"Ctrl","＋","Shift","＋","← 左"},chord.Controls.Cast<Control>().Select(x=>x.Text));
                form.Close();complete.SetResult();
            }
            catch(Exception e){complete.SetException(e);}
        }){IsBackground=true};
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
