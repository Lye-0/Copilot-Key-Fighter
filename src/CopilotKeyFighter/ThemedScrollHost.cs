using System.Drawing;
using System.Runtime.InteropServices;

// Keep native scrolling/selection semantics; clip the native scrollbar outside the visible viewport.
internal sealed class ThemedScrollHost:Panel
{
    readonly Control view;
    readonly ScrollThumb bar=new();
    readonly Panel clip=new(){Dock=DockStyle.None};
    bool syncing,queued;
    internal ThemedScrollHost(Control view)
    {
        this.view=view;Dock=DockStyle.Fill;Margin=Padding.Empty;
        view.Dock=DockStyle.None;clip.Controls.Add(view);Controls.Add(clip);Controls.Add(bar);
        view.HandleCreated+=(_,_)=>Sync();view.SizeChanged+=(_,_)=>Sync();
        view.Paint+=(_,_)=>Sync();
        if(view is ScrollableControl panel)
        {
            panel.Scroll+=(_,_)=>Schedule();panel.Layout+=(_,_)=>Schedule();
            void Observe(Control child){child.LocationChanged+=(_,_)=>Schedule();child.SizeChanged+=(_,_)=>Schedule();}
            foreach(Control child in panel.Controls)Observe(child);
            panel.ControlAdded+=(_,e)=>{if(e.Control!=null)Observe(e.Control);Schedule();};
        }
        if(view is SteadyListBox list)list.ViewChanged+=(_,_)=>Sync();
        bar.Moved+=value=>
        {
            if(view is ListBox items)items.TopIndex=value;
            else if(view is ScrollableControl content)content.AutoScrollPosition=new Point(0,value);
            Sync();
        };
    }
    void Schedule()
    {
        if(queued||!IsHandleCreated||IsDisposed)return;
        queued=true;BeginInvoke(()=>{queued=false;if(!IsDisposed)Sync();});
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int width=SystemInformation.VerticalScrollBarWidth;
        clip.Bounds=new Rectangle(0,0,Math.Max(0,ClientSize.Width-width),ClientSize.Height);
        bar.Bounds=new Rectangle(clip.Right,0,width,ClientSize.Height);
        view.Height=clip.Height;
        // Width is a layout invariant, including before a handle/scroll range exists.
        if(!view.IsHandleCreated)view.Width=clip.Width;
        Sync();
    }
    void Sync()
    {
        if(syncing||!view.IsHandleCreated||IsDisposed)return;
        syncing=true;
        try
        {
        var info=new ScrollInfo{Size=(uint)Marshal.SizeOf<ScrollInfo>(),Mask=0x17};
        bool available=GetScrollInfo(view.Handle,1,ref info);
        int maximum=available?Math.Max(info.Min,info.Max-(int)info.Page+1):0;
        bool scrollable=available&&maximum>info.Min;
        if(view is ScrollableControl panel)scrollable=panel.VerticalScroll.Visible;
        bar.Visible=scrollable;
        int position=view is ScrollableControl scrolled?-scrolled.AutoScrollPosition.Y:info.Position;
        bar.Configure(available?info.Min:0,maximum,(int)info.Page,position);
        view.Width=clip.Width+(scrollable?SystemInformation.VerticalScrollBarWidth:0);
        }
        finally{syncing=false;}
    }
    [StructLayout(LayoutKind.Sequential)]struct ScrollInfo{public uint Size,Mask;public int Min,Max;public uint Page;public int Position,Track;}
    [DllImport("user32.dll")]static extern bool GetScrollInfo(IntPtr window,int which,ref ScrollInfo info);
}

internal sealed class ScrollThumb:Control
{
    int minimum,maximum,page,value,dragStart,dragValue;
    bool hover,dragging;
    internal event Action<int>? Moved;
    internal ScrollThumb()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);
        TabStop=true;AccessibleRole=AccessibleRole.ScrollBar;AccessibleName="縦スクロール";Cursor=Cursors.Hand;
    }
    internal void Configure(int min,int max,int size,int position)
    {
        position=Math.Clamp(position,min,max);
        if((minimum,maximum,page,value)==(min,max,size,position))return;
        (minimum,maximum,page,value)=(min,max,size,position);Invalidate();
    }
    Rectangle Thumb
    {
        get
        {
            int track=Math.Max(1,Height-8);
            int length=Math.Min(track,Math.Max(32,(int)((double)track*Math.Max(1,page)/Math.Max(1,maximum-minimum+page))));
            int y=4+(maximum==minimum?0:(int)((double)(track-length)*(value-minimum)/(maximum-minimum)));
            return new Rectangle(4,y,Math.Max(4,Width-8),length);
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        using var background=new SolidBrush(Visuals.Paper);e.Graphics.FillRectangle(background,ClientRectangle);
        var track=new Rectangle(4,4,Math.Max(4,Width-8),Math.Max(1,Height-8));
        Visuals.Box(e.Graphics,track,Visuals.Surface,Visuals.Surface,5);
        Color color=dragging||hover||Focused?Visuals.Accent:Visuals.Muted;
        Visuals.Box(e.Graphics,Thumb,color,color,5);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(1,1,Width-2,Height-2));
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    void MoveTo(int target)=>Moved?.Invoke(Math.Clamp(target,minimum,maximum));
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();
        if(Thumb.Contains(e.Location)){dragging=true;dragStart=e.Y;dragValue=value;Capture=true;Invalidate();}
        else MoveTo(value+(e.Y<Thumb.Top?-1:1)*Math.Max(1,page));
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(!dragging)return;
        int travel=Math.Max(1,Height-8-Thumb.Height);
        MoveTo(dragValue+(int)Math.Round((double)(e.Y-dragStart)*(maximum-minimum)/travel));
    }
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left){dragging=false;Capture=false;Invalidate();}}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=false;Invalidate();}}
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End||base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int? target=e.KeyCode switch{Keys.Up=>value-1,Keys.Down=>value+1,Keys.PageUp=>value-Math.Max(1,page),Keys.PageDown=>value+Math.Max(1,page),Keys.Home=>minimum,Keys.End=>maximum,_=>null};
        if(target.HasValue){MoveTo(target.Value);e.Handled=true;e.SuppressKeyPress=true;}
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        // Forward to the existing view so its wheel policy is preserved.
        if(Parent is ThemedScrollHost host)
        {
            var target=host.Controls.OfType<Panel>().Single().Controls[0];
            SendMessage(target.Handle,0x20A,(IntPtr)(e.Delta<<16),IntPtr.Zero);
        }
    }
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
}
