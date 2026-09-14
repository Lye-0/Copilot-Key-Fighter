using System.Drawing;

internal sealed class KeyChordView:FlowLayoutPanel
{
    KeyBinding? displayed;
    internal KeyChordView(){AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Dock=DockStyle.Fill;WrapContents=true;AccessibleName="送信するキーの組み合わせ";}
    internal void SetBinding(KeyBinding binding)
    {
        if(displayed==binding)return;
        displayed=binding;SuspendLayout();
        while(Controls.Count>0)Controls[0].Dispose();
        var names=new List<string>();
        if(binding.Control)names.Add("Ctrl");if(binding.Shift)names.Add("Shift");if(binding.Alt)names.Add("Alt");if(binding.Win)names.Add("Win");
        names.Add(KeyNames.Name(binding.Key));
        foreach(string name in names)
        {
            if(Controls.Count>0)Controls.Add(new Label{Text="＋",AutoSize=false,Width=30,Height=44,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Visuals.Muted,Margin=Padding.Empty});
            Controls.Add(new KeyCap(name,Font));
        }
        AccessibleDescription=string.Join(" + ",names);ResumeLayout(true);
    }
    sealed class KeyCap:Control
    {
        internal KeyCap(string text,Font font)
        {
            Text=text;Font=font;AccessibleName=text;AccessibleRole=AccessibleRole.StaticText;
            Width=Math.Max(62,TextRenderer.MeasureText(text,font).Width+24);Height=44;Margin=new Padding(0,0,0,4);
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)=>Visuals.PaintCanvas(this,e.Graphics);
        protected override void OnPaint(PaintEventArgs e)
        {
            Visuals.PaintCanvas(this,e.Graphics);
            Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Visuals.Surface,Visuals.Border);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Visuals.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }
    }
}
