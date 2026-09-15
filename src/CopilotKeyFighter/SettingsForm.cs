using System.Drawing;

internal sealed class SettingsForm:Form
{
    readonly Action<Settings> save;
    readonly UiCheck enabled=new(){Text="割り当てを有効にする",Switch=true,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,0,0,6)};
    readonly ModeSelector action=new();
    readonly UiCheck existing=new(){Text="起動済みなら、そのウィンドウを表示",Dock=DockStyle.Fill};
    readonly UiCheck startup=new(){Text="ログイン時に自動起動",Switch=true,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,0,0,6)};
    readonly UiCheck ctrl=Chip("Ctrl"),shift=Chip("Shift"),alt=Chip("Alt"),win=Chip("Win");
    readonly TextBox id=new(){ReadOnly=true},arguments=new(),directory=new();
    readonly Label appName=new(){AutoSize=true,Text="アプリを選択してください"};
    readonly Label current=new(){AutoSize=true,Name="CurrentStatus"};
    readonly Label modeHelp=new(){AutoSize=true};
    readonly Label feedback=new(){AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    readonly KeyChordView combination=new();
    readonly UiButton chooseKey=new(){Width=250,Text="Delete  ▾",Name="KeyChoice"};
    readonly UiButton chooseApp=new(){Text="アプリを選択",Width=190};
    readonly UiButton tryApp=new(){Text="起動を試す",Width=130};
    readonly UiButton apply=new(){Text="保存",Width=110,Primary=true,Name="SaveSettings"};
    readonly TableLayoutPanel appPanel=Grid(),keyPanel=Grid(),details=Grid();
    readonly PictureBox appIcon=new(){SizeMode=PictureBoxSizeMode.CenterImage,Width=44,Height=44};
    readonly Icon ownedIcon;
    Settings baseline=null!;
    Settings saved;
    AppTarget? target;
    int keyCode=0x2E;
    bool initializing=true,paused;
    bool Dirty=>!initializing&&Read()!=baseline;

    internal SettingsForm(Settings settings,Action<Settings> save,bool paused=false)
    {
        this.save=save;this.saved=settings;this.paused=paused;
        Text="Copilot Key Fighter — 設定";ownedIcon=Program.LoadIcon();Icon=ownedIcon;
        Font=new Font("Yu Gothic UI",10);BackColor=Visuals.Paper;ForeColor=Visuals.Ink;
        AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(840,820);MinimumSize=new Size(720,640);StartPosition=FormStartPosition.CenterScreen;
        var header=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=3,RowCount=2,Padding=new Padding(28,14,28,12),BackColor=Visuals.Surface};
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,48));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var logo=new PictureBox{Image=ownedIcon.ToBitmap(),SizeMode=PictureBoxSizeMode.CenterImage,Dock=DockStyle.Fill};
        var heading=new Label{Text="Copilot Key Fighter",Font=new Font(Font.FontFamily,16,FontStyle.Bold),AutoSize=true,Margin=new Padding(12,0,0,2)};
        current.Margin=new Padding(12,0,0,0);header.Controls.Add(logo,0,0);header.SetRowSpan(logo,2);header.Controls.Add(heading,1,0);header.Controls.Add(current,1,1);
        var about=Button("About",(_,_)=>{using var dialog=new AboutForm();dialog.ShowDialog(this);},92);about.Name="AboutButton";about.AccessibleName="About：保存場所と導入・削除の手順";about.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        header.Controls.Add(about,2,0);header.SetRowSpan(about,2);
        header.SizeChanged+=(_,_)=>current.MaximumSize=new Size(Math.Max(160,header.Width-header.Padding.Horizontal-164),0);
        var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name="SettingsViewport"};
        var content=Grid();content.Dock=DockStyle.Top;content.Padding=new Padding(28,20,28,12);viewport.Controls.Add(content);
        var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=76,ColumnCount=2,Padding=new Padding(28,14,28,14),BackColor=Visuals.Surface};
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,244));
        var buttons=Row();buttons.WrapContents=false;buttons.AutoSize=true;buttons.Controls.Add(Button("閉じる",(_,_)=>Close(),100));buttons.Controls.Add(apply);
        footer.Controls.Add(feedback,0,0);footer.Controls.Add(buttons,1,0);
        Controls.Add(new ThemedScrollHost(viewport));Controls.Add(footer);Controls.Add(header);
        var general=Grid();general.Name="GlobalSettings";general.Padding=new Padding(16,8,16,12);general.BackColor=Visuals.Surface;general.Margin=new Padding(0,0,0,20);
        var generalTitle=Caption("全体設定");generalTitle.Font=new Font(Font,FontStyle.Bold);generalTitle.Margin=new Padding(0,0,0,4);
        Add(general,generalTitle);Add(general,enabled);Add(general,startup);Add(content,general);
        Add(content,Title("Copilot キーの割り当て"));Add(content,action);Add(content,modeHelp);

        var appCard=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,Padding=new Padding(12),BackColor=Visuals.Surface,Margin=new Padding(0,10,0,8)};
        appCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,54));appCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        appName.Anchor=AnchorStyles.Left;appName.Font=new Font(Font,FontStyle.Bold);appName.MaximumSize=new Size(380,0);
        appCard.Controls.Add(appIcon,0,0);appCard.Controls.Add(appName,1,0);
        Add(appPanel,appCard);
        var appActions=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3,Margin=new Padding(0,0,0,8),Name="AppActions"};
        appActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));appActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));appActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var selection=Row();selection.Name="AppSelectionMethods";selection.WrapContents=false;selection.Controls.Add(chooseApp);selection.Controls.Add(Button("ファイルから選ぶ…",(_,_)=>BrowseExe(),190));
        appActions.Controls.Add(selection,0,0);tryApp.Anchor=AnchorStyles.Right|AnchorStyles.Top;appActions.Controls.Add(tryApp,2,0);Add(appPanel,appActions);
        Add(appPanel,existing);
        Add(details,Caption("アプリID / 実行ファイル"));Add(details,Visuals.Field(id));
        var advancedActions=Row();advancedActions.Controls.Add(Button("IDを指定…",(_,_)=>EnterId(),125));
        advancedActions.Controls.Add(Button("コピー",(_,_)=>{if(id.Text.Length>0)Clipboard.SetText(id.Text);},100));Add(details,advancedActions);
        Add(details,Caption("起動引数（任意）"));Add(details,Visuals.Field(arguments));
        Add(details,Caption("作業フォルダー（任意）"));Add(details,Visuals.Field(directory));
        Add(appPanel,new AccordionSection("詳細設定",details){Name="AdvancedSettings"});
        Add(content,appPanel);

        var chord=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3,RowCount=2,Margin=new Padding(0,8,0,8),Name="ChordEditor"};
        chord.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));chord.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));chord.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        chord.RowStyles.Add(new RowStyle(SizeType.AutoSize));chord.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        chord.Controls.Add(Caption("修飾キー（任意）"),0,0);chord.Controls.Add(Caption("メインキー"),2,0);
        var modifiers=Row();modifiers.Name="ModifierSwitches";modifiers.WrapContents=false;modifiers.Margin=Padding.Empty;modifiers.Controls.AddRange([ctrl,shift,alt,win]);
        chord.Controls.Add(modifiers,0,1);
        chord.Controls.Add(new Label{Text="＋",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Visuals.Muted},1,1);
        chooseKey.Width=210;chooseKey.Dock=DockStyle.Fill;chooseKey.Margin=new Padding(0,0,0,0);chord.Controls.Add(chooseKey,2,1);Add(keyPanel,chord);
        var summary=Grid();summary.Name="KeySummary";summary.BackColor=Visuals.Surface;summary.Padding=new Padding(14,10,14,12);summary.Margin=new Padding(0,8,0,4);summary.MinimumSize=new Size(0,82);
        var summaryTitle=Caption("送信する操作");summaryTitle.Margin=new Padding(0,0,0,6);Add(summary,summaryTitle);
        combination.Font=new Font("Yu Gothic UI",14,FontStyle.Bold);combination.ForeColor=Visuals.Accent;combination.Margin=new Padding(0,0,0,6);Add(summary,combination);Add(keyPanel,summary);
        var testPanel=Grid();
        Add(testPanel,Caption("保存後、この欄で動作を試せます。"));
        var testInput=new TextBox{Text="abc",Multiline=true,AcceptsReturn=true,AccessibleName="キー入力の確認欄"};
        Add(testPanel,Visuals.Field(testInput,70));
        Add(testPanel,Button("リセット",(_,_)=>{testInput.Text="abc";testInput.Select(0,0);testInput.Focus();},110));
        Add(keyPanel,new AccordionSection("入力欄で試す",testPanel){Name="KeyTest"});
        Add(content,keyPanel);

        chooseApp.Click+=(_,_)=>{using var picker=new AppPicker();if(picker.ShowDialog(this)==DialogResult.OK)SetTarget(picker.Selected!);};
        chooseKey.Click+=(_,_)=>{using var picker=new KeyPicker(keyCode);if(picker.ShowDialog(this)==DialogResult.OK){keyCode=picker.SelectedCode;RefreshDraft();}};
        tryApp.Click+=async (_,_)=>
        {
            tryApp.Enabled=false;
            try{var value=Read();value.Validate();if(value.Target==null)throw new ArgumentException("アプリを選択してください。");await AppLauncher.OpenAsync(value.Target,value.PreferExisting);ShowFeedback("起動しました。",false);}
            catch(Exception e){ShowFeedback(e.Message,true);}
            finally{tryApp.Enabled=target!=null;}
        };
        apply.Click+=(_,_)=>
        {
            try{var value=Read();save(value);saved=value;baseline=value;this.paused=false;RefreshDraft();RefreshCurrent();ShowFeedback("保存しました。",false);}
            catch(Exception e){ShowFeedback(e.Message,true);}
        };
        enabled.Checked=settings.Enabled;action.SelectedIndex=(int)settings.Action;existing.Checked=settings.PreferExisting;startup.Checked=settings.StartWithWindows;
        var binding=settings.Binding??new();keyCode=binding.Key;ctrl.Checked=binding.Control;shift.Checked=binding.Shift;alt.Checked=binding.Alt;win.Checked=binding.Win;
        if(settings.Target!=null)SetTarget(settings.Target);
        foreach(var c in new[]{enabled,existing,startup,ctrl,shift,alt,win})c.CheckedChanged+=(_,_)=>RefreshDraft();
        arguments.TextChanged+=(_,_)=>RefreshDraft();directory.TextChanged+=(_,_)=>RefreshDraft();action.SelectedIndexChanged+=(_,_)=>RefreshDraft();
        Visuals.Apply(this);baseline=Read();initializing=false;RefreshCurrent();RefreshDraft();
        FormClosing+=(_,e)=>{if(Dirty&&!ConfirmDiscard())e.Cancel=true;};
        FormClosed+=(_,_)=>logo.Image?.Dispose();
    }
    Settings Read()=>new(Enabled:enabled.Checked,Action:(ActionKind)action.SelectedIndex,Target:target==null?null:target with{Arguments=arguments.Text,WorkingDirectory=directory.Text.Trim()},Binding:new(keyCode,ctrl.Checked,shift.Checked,alt.Checked,win.Checked),PreferExisting:existing.Checked,StartWithWindows:startup.Checked);
    void RefreshCurrent()
    {
        string state=!saved.Enabled?"停止中":paused?"一時停止中":"有効";
        string assignment=saved.Action==ActionKind.Keyboard?KeyNames.Binding(saved.Binding??new()):saved.Action==ActionKind.Application?saved.Target?.Name??"未設定":"標準の起動を抑止";
        current.Text=$"現在：{state}  ·  {assignment}";current.ForeColor=saved.Enabled&&!paused?Visuals.Accent:Visuals.Muted;
    }
    internal void UpdateRuntimeState(Settings value,bool isPaused){saved=value;paused=isPaused;RefreshCurrent();}
    void RefreshDraft()
    {
        if(initializing)return;
        appPanel.Visible=action.SelectedIndex==0;keyPanel.Visible=action.SelectedIndex==1;
        modeHelp.Text=action.SelectedIndex switch{0=>"選んだアプリを開きます。",1=>"キーを離したときに、選んだ操作を1回送信します。",_=>"Copilot キーの標準の起動動作を止めます。"};
        modeHelp.ForeColor=Visuals.Muted;
        chooseKey.Text=KeyNames.Name(keyCode)+"  ▾";
        chooseKey.AccessibleName="送信するキー："+KeyNames.Name(keyCode);
        combination.SetBinding(Read().Binding!);
        tryApp.Enabled=target!=null;chooseApp.Text="一覧から選ぶ…";
        apply.Enabled=Dirty;
        ShowFeedback(Dirty?"未保存の変更があります。保存すると適用されます。":"変更はありません。",false);
    }
    void SetTarget(AppTarget value)
    {
        target=value;appName.Text=value.Name;id.Text=value.Id;arguments.Text=value.Arguments;directory.Text=value.WorkingDirectory;
        arguments.Enabled=value.Kind!=TargetKind.Shell;directory.Enabled=value.Kind==TargetKind.Executable;
        appIcon.Image?.Dispose();using(var icon=TargetIcon.Load(value))appIcon.Image=icon.ToBitmap();RefreshDraft();
    }
    void BrowseExe()
    {
        using var dialog=new OpenFileDialog{Title="起動する実行ファイルを選択",Filter="実行ファイル (*.exe)|*.exe",CheckFileExists=true};
        if(dialog.ShowDialog(this)==DialogResult.OK)SetTarget(new(TargetKind.Executable,Path.GetFileNameWithoutExtension(dialog.FileName),dialog.FileName,dialog.FileName));
    }
    void EnterId()
    {
        using var dialog=new Form{Text="アプリIDを指定",ClientSize=new Size(600,230),StartPosition=FormStartPosition.CenterParent,Font=Font,MinimizeBox=false,MaximizeBox=false,BackColor=Visuals.Paper};
        dialog.ShowIcon=false;dialog.ShowInTaskbar=false;
        var layout=Grid();layout.Dock=DockStyle.Fill;layout.Padding=new Padding(24);dialog.Controls.Add(layout);
        Add(layout,Caption("パッケージアプリの AUMID"));
        var field=new TextBox{Text=target?.Kind==TargetKind.Packaged?target.Id:""};Add(layout,Visuals.Field(field));
        var error=new Label{AutoSize=true,ForeColor=Color.Firebrick};Add(layout,error);
        var confirm=Button("指定する",(_,_)=>{if(!field.Text.Trim().Contains('!')){error.Text="「パッケージ名!アプリ名」の形式で入力してください。";return;}dialog.DialogResult=DialogResult.OK;dialog.Close();},130);
        confirm.Primary=true;Add(layout,confirm);dialog.AcceptButton=confirm;
        if(dialog.ShowDialog(this)==DialogResult.OK)SetTarget(new(TargetKind.Packaged,field.Text.Trim(),field.Text.Trim()));
    }
    bool ConfirmDiscard()
    {
        using var dialog=new Form{Text="未保存の変更",ClientSize=new Size(530,165),StartPosition=FormStartPosition.CenterParent,Font=Font,BackColor=Visuals.Paper,MinimizeBox=false,MaximizeBox=false};
        dialog.ShowIcon=false;dialog.ShowInTaskbar=false;
        var root=Grid();root.Dock=DockStyle.Fill;root.Padding=new Padding(22);dialog.Controls.Add(root);
        Add(root,Caption("変更を保存せずに閉じますか？"));
        var row=Row();var back=Button("編集に戻る",(_,_)=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();},150);back.Primary=true;
        row.Controls.Add(back);row.Controls.Add(Button("保存せず閉じる",(_,_)=>{dialog.DialogResult=DialogResult.OK;dialog.Close();},170));Add(root,row);dialog.CancelButton=back;
        return dialog.ShowDialog(this)==DialogResult.OK;
    }
    void ShowFeedback(string text,bool error)
    {
        feedback.Text=text;feedback.ForeColor=error?Color.Firebrick:Visuals.Muted;
        if(feedback.Parent is TableLayoutPanel footer)
        {
            int width=Math.Max(180,footer.ClientSize.Width-footer.Padding.Horizontal-244);
            int height=TextRenderer.MeasureText(text,feedback.Font,new Size(width,int.MaxValue),TextFormatFlags.WordBreak).Height;
            footer.Height=Math.Clamp(height+28,76,140);
        }
    }
    internal static UiButton Button(string text,EventHandler handler,int width=140){var b=new UiButton{Text=text,Width=width};b.Click+=handler;return b;}
    static UiCheck Chip(string text)=>new(){Text=text,Chip=true,Width=104,Height=42,Margin=new Padding(0,0,6,0)};
    internal static TableLayoutPanel Grid()=>new(){ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top};
    internal static FlowLayoutPanel Row()=>new(){AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Margin=new Padding(0,4,0,8)};
    internal static void Add(TableLayoutPanel p,Control c){p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.Controls.Add(c,0,p.RowCount++);}
    internal static Label Caption(string text)=>new(){Text=text,AutoSize=true,Margin=new Padding(0,10,0,8)};
    Label Title(string text)=>new(){Text=text,AutoSize=true,Font=new Font(Font.FontFamily,16,FontStyle.Bold),Margin=new Padding(0,0,0,10)};
    protected override void Dispose(bool disposing){if(disposing){appIcon.Image?.Dispose();ownedIcon.Dispose();}base.Dispose(disposing);}
}

internal static class KeyNames
{
    internal static string Name(int code)=>code switch
    {
        8=>"Backspace",9=>"Tab",13=>"Enter",19=>"Pause",20=>"Caps Lock",27=>"Esc",32=>"Space",33=>"Page Up",34=>"Page Down",35=>"End",36=>"Home",
        37=>"← 左",38=>"↑ 上",39=>"→ 右",40=>"↓ 下",44=>"Print Screen",45=>"Insert",46=>"Delete",
        >=48 and <=57=>((char)code).ToString(),>=65 and <=90=>((char)code).ToString(),>=96 and <=105=>"テンキー "+(code-96),
        106=>"テンキー ×",107=>"テンキー ＋",109=>"テンキー −",110=>"テンキー .",111=>"テンキー ÷",
        >=112 and <=135=>"F"+(code-111),144=>"Num Lock",145=>"Scroll Lock",173=>"ミュート",174=>"音量を下げる",175=>"音量を上げる",176=>"次の曲",177=>"前の曲",178=>"停止",179=>"再生 / 一時停止",
        21=>"かな",22=>"IME オン",26=>"IME オフ",28=>"変換",29=>"無変換",
        166=>"ブラウザー：戻る",167=>"ブラウザー：進む",168=>"ブラウザー：更新",169=>"ブラウザー：停止",170=>"ブラウザー：検索",171=>"ブラウザー：お気に入り",172=>"ブラウザー：ホーム",
        186 or 187 or 188 or 189 or 190 or 191 or 192 or 219 or 220 or 221 or 222 or 226=>LayoutName(code),
        _=>((Keys)code).ToString()
    };
    internal static string Binding(KeyBinding b)=>string.Join(" + ",new[]{b.Control?"Ctrl":null,b.Shift?"Shift":null,b.Alt?"Alt":null,b.Win?"Win":null,Name(b.Key)}.Where(x=>x!=null));
    internal static IEnumerable<int> Codes=>new[]{46,8,13,27,9,32,45,36,35,33,34,37,38,39,40,44,19,20,144,145}.Concat(Enumerable.Range(112,24).Where(k=>k!=134)).Concat(Enumerable.Range(65,26)).Concat(Enumerable.Range(48,10)).Concat(Enumerable.Range(96,16).Where(k=>k!=108)).Concat(Enumerable.Range(166,14)).Concat(new[]{21,22,26,28,29,186,187,188,189,190,191,192,219,220,221,222,226});
    static string LayoutName(int code)
    {
        uint scan=MapVirtualKey((uint)code,4);var name=new System.Text.StringBuilder(80);
        int parameter=(int)((scan&255)<<16);if((scan&0xFF00)!=0)parameter|=1<<24;
        return GetKeyNameText(parameter,name,name.Capacity)>0?name.ToString():$"記号キー {code:X2}";
    }
    [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="MapVirtualKeyW")] static extern uint MapVirtualKey(uint key,uint mapType);
    [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern int GetKeyNameText(int parameter,System.Text.StringBuilder name,int size);
}
