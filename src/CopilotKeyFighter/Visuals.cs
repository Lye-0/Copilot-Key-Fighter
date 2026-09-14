using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

internal static class Visuals
{
    internal static ToolStripRenderer MenuRenderer()=>new MenuTheme();
    sealed class MenuColors:ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground=>Surface;
        public override Color ImageMarginGradientBegin=>Surface;
        public override Color ImageMarginGradientMiddle=>Surface;
        public override Color ImageMarginGradientEnd=>Surface;
        public override Color MenuBorder=>Border;
        public override Color MenuItemBorder=>Accent;
        public override Color MenuItemSelected=>SystemInformation.HighContrast?SystemColors.Highlight:Color.FromArgb(229,242,245);
        public override Color SeparatorDark=>Border;
        public override Color SeparatorLight=>Surface;
    }
    sealed class MenuTheme:ToolStripProfessionalRenderer
    {
        internal MenuTheme():base(new MenuColors()){RoundedEdges=false;}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor=!e.Item.Enabled?SystemColors.GrayText:SystemInformation.HighContrast&&e.Item.Selected?SystemColors.HighlightText:Ink;base.OnRenderItemText(e);
        }
    }
    internal static Color Navy => SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(18,42,61);
    internal static Color Ink => SystemInformation.HighContrast ? SystemColors.ControlText : Color.FromArgb(26,43,58);
    internal static Color Muted => SystemInformation.HighContrast ? SystemColors.ControlText : Color.FromArgb(92,109,123);
    internal static Color Paper => SystemInformation.HighContrast ? SystemColors.Control : Color.FromArgb(245,248,250);
    internal static Color Border => SystemInformation.HighContrast ? SystemColors.ControlText : Color.FromArgb(202,214,223);
    internal static Color Accent => SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(0,115,128);
    internal static Color Surface => SystemColors.Window;
    internal static void Apply(Control parent)
    {
        parent.ForeColor = Ink;
        if(parent is Form) parent.BackColor = Paper;
        foreach(Control child in parent.Controls) Apply(child);
    }
    internal static void Primary(Button button) { if(button is UiButton b) b.Primary = true; }
    internal static void PaintCanvas(Control control,Graphics graphics)
    {
        Control? parent=control.Parent;
        while(parent!=null && parent.BackColor.A!=255) parent=parent.Parent;
        using var brush=new SolidBrush(parent?.BackColor ?? Paper);
        graphics.FillRectangle(brush,control.ClientRectangle);
    }
    internal static GraphicsPath Round(Rectangle rect, int radius)
    {
        var p=new GraphicsPath(); int d=Math.Min(radius*2,Math.Min(rect.Width,rect.Height));
        if(d<2){p.AddRectangle(rect);return p;}
        p.AddArc(rect.Left,rect.Top,d,d,180,90);p.AddArc(rect.Right-d,rect.Top,d,d,270,90);
        p.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);p.AddArc(rect.Left,rect.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    internal static void Box(Graphics g,Rectangle bounds,Color fill,Color edge,int radius=7)
    {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Round(bounds,radius);using var brush=new SolidBrush(fill);using var pen=new Pen(edge);
        g.FillPath(brush,path);g.DrawPath(pen,path);
    }
    internal static Control Field(TextBox text, int height=42)
    {
        text.BorderStyle=BorderStyle.None;text.BackColor=Surface;text.ForeColor=Ink;text.Dock=DockStyle.Fill;
        var host=new FieldPanel { Dock=DockStyle.Fill,Height=height,Padding=new Padding(12,10,12,8),Margin=new Padding(0,0,0,8) };
        host.Controls.Add(text);text.GotFocus+=(_,_)=>host.Invalidate();text.LostFocus+=(_,_)=>host.Invalidate();return host;
    }
    sealed class FieldPanel:Panel
    {
        internal FieldPanel(){DoubleBuffered=true;BackColor=Surface;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(ContainsFocus?Accent:Border,ContainsFocus?2:1);e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);}
    }
}

internal class UiButton:Button
{
    bool hover,pressed,primary;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Primary {get=>primary;set{primary=value;Invalidate();}}
    internal UiButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);Height=42;Width=130;Cursor=Cursors.Hand;Margin=new Padding(0,0,8,0);}
    protected override void OnPaintBackground(PaintEventArgs e)=>Visuals.PaintCanvas(this,e.Graphics);
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        Visuals.PaintCanvas(this,e.Graphics);
        Color fill=!Enabled?Visuals.Paper:primary?Visuals.Navy:(hover||pressed)?SystemInformation.HighContrast?SystemColors.Highlight:Color.FromArgb(229,242,245):Visuals.Surface;
        Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),fill,Focused?Visuals.Accent:primary&&Enabled?Visuals.Navy:Visuals.Border);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(10,0,Width-20,Height),!Enabled?Visuals.Muted:primary||(SystemInformation.HighContrast&&(hover||pressed))?SystemColors.HighlightText:Visuals.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-11,Height-11));
    }
}

internal sealed class UiCheck:CheckBox
{
    bool hover;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Chip {get;set;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Switch {get;set;}
    internal UiCheck(){AutoSize=false;Height=34;Width=600;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);}
    public override Size GetPreferredSize(Size proposedSize)
    {
        if(Switch||Chip)return new Size(TextRenderer.MeasureText(Text,Font,Size.Empty,TextFormatFlags.NoPadding).Width+(Chip?30+34:42+36),Math.Max(42,Font.Height+16));
        return base.GetPreferredSize(proposedSize);
    }
    protected override void OnPaintBackground(PaintEventArgs e)=>Visuals.PaintCanvas(this,e.Graphics);
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnCheckedChanged(EventArgs e){Invalidate();base.OnCheckedChanged(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        Visuals.PaintCanvas(this,e.Graphics);
        if(Chip || Switch)
        {
            int trackWidth=Chip?30:42,trackHeight=Chip?16:22;
            int padding=Chip?8:12,gap=Chip?18:12;
            int textWidth=TextRenderer.MeasureText(Text,Font,Size.Empty,TextFormatFlags.NoPadding).Width;
            int trackX=Math.Min(Width-trackWidth-padding,padding+textWidth+gap);
            Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Checked&&!SystemInformation.HighContrast?Color.FromArgb(239,248,249):Visuals.Surface,Focused?Visuals.Accent:Visuals.Border);
            var track=new Rectangle(trackX,(Height-trackHeight)/2,trackWidth,trackHeight);
            Visuals.Box(e.Graphics,track,Checked?Visuals.Accent:Visuals.Border,Focused?Visuals.Accent:Checked?Visuals.Accent:Visuals.Border,trackHeight/2);
            int diameter=trackHeight-6;using var knob=new SolidBrush(SystemColors.Window);
            e.Graphics.FillEllipse(knob,Checked?track.Right-diameter-3:track.Left+3,track.Top+3,diameter,diameter);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(padding,0,Math.Max(0,trackX-padding-gap),Height),Enabled?Visuals.Ink:Visuals.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
        }
        else
        {
            int top=(Height-20)/2;
            Visuals.Box(e.Graphics,new Rectangle(1,top,20,20),Checked?Visuals.Accent:Visuals.Surface,hover||Focused?Visuals.Accent:Visuals.Border,4);
            if(Checked){using var pen=new Pen(SystemColors.HighlightText,2);e.Graphics.DrawLines(pen,[new Point(5,top+10),new Point(9,top+14),new Point(17,top+6)]);}
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(32,0,Width-34,Height),Enabled?Visuals.Ink:Visuals.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        }
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-7,Height-7));
    }
}

internal sealed class ModeSelector:FlowLayoutPanel
{
    readonly RadioButton[] choices;
    internal event EventHandler? SelectedIndexChanged;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int SelectedIndex {get=>Array.FindIndex(choices,c=>c.Checked);set{if(value>=0&&value<choices.Length)choices[value].Checked=true;}}
    internal ModeSelector()
    {
        AutoSize=true;Dock=DockStyle.Fill;WrapContents=false;Margin=new Padding(0,0,0,8);Padding=new Padding(4);
        BackColor=SystemInformation.HighContrast?SystemColors.Control:Color.FromArgb(226,234,240);
        choices=new[]{"アプリを開く","キーを送る","起動を抑止"}.Select(t=>(RadioButton)new Choice {Text=t,Height=46,Width=220,Margin=Padding.Empty}).ToArray();
        foreach(var c in choices){Controls.Add(c);c.CheckedChanged+=(_,_)=>{c.Invalidate();if(c.Checked)SelectedIndexChanged?.Invoke(this,EventArgs.Empty);};}
        SizeChanged+=(_,_)=>{foreach(var c in choices)c.Width=Math.Max(130,(ClientSize.Width-Padding.Horizontal)/3);};SelectedIndex=0;
    }
    internal void RefreshColors(){foreach(var c in choices)c.Invalidate();}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Visuals.PaintCanvas(this,e.Graphics);
        if(Width>2&&Height>2)Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),BackColor,BackColor,10);
    }
    sealed class Choice:RadioButton
    {
        bool hover;
        internal Choice(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
        protected override void OnPaintBackground(PaintEventArgs e)=>Visuals.PaintCanvas(this,e.Graphics);
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
        protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            Visuals.PaintCanvas(this,e.Graphics);
            if(Checked)Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Visuals.Navy,Focused?Visuals.Accent:Visuals.Navy);
            else if(hover)Visuals.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Visuals.Surface,Visuals.Surface);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Checked?SystemColors.HighlightText:Visuals.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-11,Height-11));
        }
    }
}
