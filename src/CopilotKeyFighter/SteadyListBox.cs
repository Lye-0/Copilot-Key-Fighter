internal sealed class WheelSteps
{
    int remainder;
    internal int Read(int delta)
    {
        if(delta==0)return 0;
        if(remainder!=0&&Math.Sign(remainder)!=Math.Sign(delta))remainder=0;
        remainder+=delta;
        if(Math.Abs(remainder)<120)return 0;
        int step=-Math.Sign(remainder);remainder=0;return step;
    }
    internal void Reset()=>remainder=0;
}

internal sealed class SteadyListBox:ListBox
{
    internal event EventHandler? ViewChanged;
    readonly WheelSteps wheel=new();
    internal void ApplyWheelDelta(int delta)
    {
        int step=wheel.Read(delta);
        if(step==0||Items.Count==0)return;
        int visible=Math.Max(1,ClientSize.Height/Math.Max(1,ItemHeight));
        TopIndex=Math.Clamp(TopIndex+step,0,Math.Max(0,Items.Count-visible));
    }
    protected override void OnDataSourceChanged(EventArgs e){wheel.Reset();base.OnDataSourceChanged(e);}
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x20A)
        {
            ApplyWheelDelta(unchecked((short)((long)m.WParam>>16)));
            m.Result=0;return; // Do not apply native wheel movement a second time.
        }
        base.WndProc(ref m);
        if(m.Msg is 0xF or 0x115 or 0x100 or 0x101 or 0x197 or 0x5)ViewChanged?.Invoke(this,EventArgs.Empty);
    }
}
