using System.ComponentModel;
using System.Drawing;

internal sealed class AccordionSection:TableLayoutPanel
{
    readonly DisclosureHeader header;
    readonly Control body;
    bool expanded;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Expanded
    {
        get=>expanded;
        set
        {
            expanded=value;body.Visible=value;header.Expanded=value;
            header.AccessibleDescription=value?"展開中。閉じるには押してください。":"閉じています。展開するには押してください。";
            header.Invalidate();PerformLayout();Parent?.PerformLayout();Invalidate();
        }
    }
    internal AccordionSection(string title,Control content)
    {
        DoubleBuffered=true;ColumnCount=1;RowCount=2;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Dock=DockStyle.Top;
        Margin=new Padding(0,12,0,12);BackColor=Visuals.Surface;
        RowStyles.Add(new RowStyle(SizeType.Absolute,46));RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header=new DisclosureHeader{Text=title,Dock=DockStyle.Fill,Margin=Padding.Empty,AccessibleName=title};
        body=content;body.Dock=DockStyle.Fill;body.Padding=new Padding(16,2,16,14);body.Margin=Padding.Empty;
        Controls.Add(header,0,0);Controls.Add(body,0,1);header.Click+=(_,_)=>Expanded=!Expanded;Expanded=false;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);using var pen=new Pen(Visuals.Border);
        e.Graphics.DrawLine(pen,0,0,Width,0);e.Graphics.DrawLine(pen,0,Height-1,Width,Height-1);
        if(expanded)e.Graphics.DrawLine(pen,16,46,Width-16,46);
    }
    sealed class DisclosureHeader:Button
    {
        internal bool Expanded;
        internal DisclosureHeader(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
        protected override void OnPaintBackground(PaintEventArgs e)=>Visuals.PaintCanvas(this,e.Graphics);
        protected override void OnPaint(PaintEventArgs e)
        {
            Visuals.PaintCanvas(this,e.Graphics);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(16,0,Math.Max(0,Width-64),Height),Visuals.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            using var pen=new Pen(Visuals.Accent,2);int x=Width-28,y=Height/2;
            Point[] points=Expanded?[new Point(x-5,y+2),new Point(x,y-3),new Point(x+5,y+2)]:[new Point(x-5,y-2),new Point(x,y+3),new Point(x+5,y-2)];
            e.Graphics.DrawLines(pen,points);
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(6,6,Width-13,Height-13));
        }
    }
}
