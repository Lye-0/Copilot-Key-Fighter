using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

[Collection("Desktop")]
public class ScrollThemeTests
{
    [Fact]
    public async Task NonScrollingPageFillsViewportOnFirstShowAndResize()
    {
        var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            try
            {
                using var form=new SettingsForm(new(Enabled:true,Action:ActionKind.Disabled),_=>{});
                static IEnumerable<Control> Walk(Control c)=>c.Controls.Cast<Control>().SelectMany(x=>new[]{x}.Concat(Walk(x)));
                form.Show();Application.DoEvents();
                foreach(var size in new[]{new Size(940,840),new Size(720,640),new Size(1080,950)})
                {
                    form.ClientSize=size;Application.DoEvents();
                    var viewport=Walk(form).OfType<Panel>().Single(p=>p.Name=="SettingsViewport");
                    var host=Walk(form).OfType<ThemedScrollHost>().Single();
                    Assert.True(viewport.ClientSize.Width>=host.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-2);
                    Assert.True(viewport.ClientSize.Width>600);
                    var bar=host.Controls.OfType<ScrollThumb>().Single();
                    Assert.Equal(viewport.VerticalScroll.Visible,bar.Visible);
                    Assert.False(viewport.HorizontalScroll.Visible);
                }
                form.Close();complete.SetResult();
            }
            catch(Exception e){complete.SetException(e);}
        }){IsBackground=true};thread.SetApartmentState(ApartmentState.STA);thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
    [Fact]
    public async Task CustomThumbTracksContentAndMovesListWithoutChangingSelection()
    {
        var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            try
            {
                using var form=new Form{ClientSize=new Size(400,300)};
                var list=new SteadyListBox{IntegralHeight=false,ItemHeight=30,DrawMode=DrawMode.OwnerDrawFixed,BorderStyle=BorderStyle.None};
                list.Items.AddRange(Enumerable.Range(0,100).Select(x=>(object)x).ToArray());
                var host=new ThemedScrollHost(list);form.Controls.Add(host);form.Show();Application.DoEvents();
                var bar=host.Controls.OfType<ScrollThumb>().Single();Assert.True(bar.Visible);
                Assert.True(list.Right>list.Parent!.ClientSize.Width);
                void Key(Keys key)=>typeof(ScrollThumb).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(bar,new object[]{new KeyEventArgs(key)});
                list.SelectedIndex=0;Key(Keys.End);Assert.True(list.TopIndex>80);Assert.Equal(0,list.SelectedIndex);
                Key(Keys.Home);Assert.Equal(0,list.TopIndex);
                void Mouse(string method,int y)=>typeof(ScrollThumb).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(bar,new object[]{new MouseEventArgs(MouseButtons.Left,1,bar.Width/2,y,0)});
                Mouse("OnMouseDown",10);Mouse("OnMouseMove",bar.Height-5);Mouse("OnMouseUp",bar.Height-5);
                Assert.True(list.TopIndex>80);Assert.Equal(0,list.SelectedIndex);
                Key(Keys.Home);
                list.ApplyWheelDelta(-120);Application.DoEvents();Assert.Equal(1,list.TopIndex);
                var value=typeof(ScrollThumb).GetField("value",BindingFlags.Instance|BindingFlags.NonPublic)!;
                Assert.Equal(1,(int)value.GetValue(bar)!);
                list.Items.Clear();Application.DoEvents();Assert.False(bar.Visible);
                form.Close();
                using var about=new AboutForm();about.Show();Application.DoEvents();
                static IEnumerable<Control> Walk(Control c)=>c.Controls.Cast<Control>().SelectMany(x=>new[]{x}.Concat(Walk(x)));
                Assert.Empty(Walk(about).OfType<TextBox>());
                foreach(var section in Walk(about).OfType<AccordionSection>())section.Expanded=true;
                var panel=Walk(about).OfType<Panel>().Single(p=>p.AutoScroll);
                Application.DoEvents();
                var display=Walk(about).OfType<AboutPathDisplay>().First();
                var path=display.Controls.OfType<Label>().Single();Assert.False(path.TabStop);
                panel.AutoScrollPosition=Point.Empty;Application.DoEvents();
                var wheel=new HandledMouseEventArgs(MouseButtons.None,0,10,10,-120);
                path.GetType().GetMethod("OnMouseWheel",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(path,new object[]{wheel});
                Application.DoEvents();Assert.True(wheel.Handled);Assert.True(panel.AutoScrollPosition.Y<0);
                panel.AutoScrollPosition=new Point(0,10000);panel.Refresh();Application.DoEvents();
                var aboutBar=Walk(about).OfType<ScrollThumb>().Single();
                Assert.True(panel.AutoScrollPosition.Y<0);
                Assert.Equal(-panel.AutoScrollPosition.Y,(int)value.GetValue(aboutBar)!);
                about.Close();complete.SetResult();
            }
            catch(Exception e){complete.SetException(e);}
        }){IsBackground=true};thread.SetApartmentState(ApartmentState.STA);thread.Start();
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
