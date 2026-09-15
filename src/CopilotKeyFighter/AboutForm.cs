using System.Drawing;
using Microsoft.Win32;

internal sealed class AboutForm:Form
{
    internal AboutForm()
    {
        Text="About — Copilot Key Fighter";Font=new Font("Yu Gothic UI",10);
        ClientSize=new Size(760,700);MinimumSize=new Size(620,520);AutoScaleMode=AutoScaleMode.Dpi;
        StartPosition=FormStartPosition.CenterParent;ShowIcon=false;ShowInTaskbar=false;MinimizeBox=false;MaximizeBox=false;
        var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
        var content=SettingsForm.Grid();content.Padding=new Padding(24);viewport.Controls.Add(content);
        var footer=new Panel{Dock=DockStyle.Bottom,Height=68,Padding=new Padding(20,12,20,12),BackColor=Visuals.Surface};
        var close=SettingsForm.Button("閉じる",(_,_)=>Close(),100);close.Dock=DockStyle.Right;footer.Controls.Add(close);CancelButton=close;
        Controls.Add(new ThemedScrollHost(viewport));Controls.Add(footer);
        Add(new Label{Text="Copilot Key Fighter",AutoSize=true,Font=new Font(Font.FontFamily,16,FontStyle.Bold),Margin=new Padding(0,0,0,6)});
        Add(SettingsForm.Caption("この環境の保存場所"));
        Location("現在の本体",Environment.ProcessPath??"実行ファイルの場所を取得できませんでした。");
        Location("設定ファイル",new SettingsStore(Program.StateDirectory).FilePath);
        Location("自動起動の登録場所",@"HKEY_CURRENT_USER\"+Startup.RegistryPath+"\r\n値の名前："+Startup.ValueName);
        string registration;
        try{using var key=Registry.CurrentUser.OpenSubKey(Startup.RegistryPath);registration=key?.GetValue(Startup.ValueName) as string??"未登録";}
        catch(Exception e) when(e is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException){registration="登録内容を読み取れませんでした。";}
        Location("現在の自動起動登録",registration);
        var install=SettingsForm.Grid();install.Padding=new Padding(0,4,0,8);
        Instructions(install,"1. 配布ZIPを、使い続けるフォルダーにすべて展開します。\n2. CopilotKeyFighter.exeを起動し、動作と対象を選びます。\n3. 「割り当てを有効にする」をオンにして「保存」を押します。\n4. 必要なら「ログイン時に自動起動」をオンにして保存します。\n\n登録はアプリが行います。レジストリを手動で編集する必要はありません。");
        Add(new AccordionSection("導入・自動起動の手順",install));
        var removal=SettingsForm.Grid();removal.Padding=new Padding(0,4,0,8);
        Instructions(removal,"1. 設定画面で「ログイン時に自動起動」をオフにして保存します。\n2. 通知領域のメニューから「終了」を選びます。\n3. 本体を展開したフォルダーを削除します。\n4. 設定・診断情報も不要なら、次のフォルダーを削除します。");
        var folder=PathField(Program.StateDirectory);SettingsForm.Add(removal,folder);
        Instructions(removal,"本体を削除する前に自動起動を解除してください。設定画面の「閉じる」だけでは常駐は終了しません。");
        Add(new AccordionSection("削除の手順",removal));
        Visuals.Apply(this);
        void Add(Control control)=>SettingsForm.Add(content,control);
        Shown+=(_,_)=>{ActiveControl=close;};
        void Location(string title,string value){Add(SettingsForm.Caption(title));var field=PathField(value);field.AccessibleName=title;Add(field);}
    }
    static Control PathField(string value)
        =>new AboutPathDisplay(value);
    static void Instructions(TableLayoutPanel parent,string text)
    {
        var label=new Label{Text=text,AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0,6,0,10)};
        parent.SizeChanged+=(_,_)=>label.MaximumSize=new Size(Math.Max(200,parent.ClientSize.Width-parent.Padding.Horizontal-12),0);
        SettingsForm.Add(parent,label);
    }
}

internal sealed class AboutPathDisplay:Panel
{
    internal AboutPathDisplay(string value)
    {
        Dock=DockStyle.Fill;Height=72;
        Padding=new Padding(12);Margin=new Padding(0,0,0,4);BackColor=Visuals.Surface;TabStop=false;
        var text=new ValueLabel{Text=value,AutoSize=false,UseMnemonic=false,AccessibleName=value,TabStop=false,Cursor=Cursors.Default};
        var copy=SettingsForm.Button("コピー",(_,_)=>{},80);copy.AccessibleName="表示内容をコピー";copy.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        copy.Click+=(_,_)=>{try{Clipboard.SetText(value);copy.Text="コピー済";}catch(System.Runtime.InteropServices.ExternalException){copy.Text="再試行";}};
        Controls.Add(text);Controls.Add(copy);
        void Arrange()
        {
            int width=Math.Max(100,ClientSize.Width-Padding.Horizontal-96);
            int height=TextRenderer.MeasureText(value,Font,new Size(width,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPrefix).Height+8;
            text.Bounds=new Rectangle(12,16,width,height);
            copy.Location=new Point(Math.Max(12,ClientSize.Width-92),12);
            Height=Math.Max(height+28,copy.Height+24);
        }
        SizeChanged+=(_,_)=>Arrange();FontChanged+=(_,_)=>Arrange();Arrange();
    }
    internal static void ScrollPage(Control source,MouseEventArgs e)
    {
        if(e is HandledMouseEventArgs handled){if(handled.Handled)return;handled.Handled=true;}
        for(Control? ancestor=source.Parent;ancestor!=null;ancestor=ancestor.Parent)
            if(ancestor is ScrollableControl viewport&&viewport.AutoScroll)
            {
                int distance=(int)Math.Round(e.Delta/120.0*48);
                viewport.AutoScrollPosition=new Point(0,Math.Max(0,-viewport.AutoScrollPosition.Y-distance));return;
            }
    }
    protected override void OnMouseWheel(MouseEventArgs e)=>ScrollPage(this,e);
    sealed class ValueLabel:Label
    {
        protected override void OnMouseWheel(MouseEventArgs e)=>ScrollPage(this,e);
    }
}
