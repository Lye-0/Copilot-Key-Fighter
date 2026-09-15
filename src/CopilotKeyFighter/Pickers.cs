using System.Drawing;

internal sealed class KeyPicker:Form
{
    readonly SteadyListBox list=new(){Dock=DockStyle.Fill,IntegralHeight=false,BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=42};
    readonly TextBox search=new(){PlaceholderText="キー名で検索（Delete、F5、音量など）"};
    readonly UiButton choose=new(){Text="このキーを選択",Width=180,Primary=true};
    readonly Label count=new(){Dock=DockStyle.Bottom,Height=30,ForeColor=Visuals.Muted};
    readonly List<KeyItem> keys;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int SelectedCode {get;private set;}
    internal KeyPicker(int selectedCode)
    {
        SelectedCode=selectedCode;Text="送信するキーを選択";ClientSize=new Size(520,500);MinimumSize=new Size(440,400);StartPosition=FormStartPosition.CenterParent;Font=new Font("Yu Gothic UI",10);BackColor=Visuals.Paper;Padding=new Padding(22);
        ShowIcon=false;ShowInTaskbar=false;MinimizeBox=false;MaximizeBox=false;search.AccessibleName="キー名で検索";
        keys=KeyNames.Codes.Append(selectedCode).Distinct().Select(k=>new KeyItem(k,KeyNames.Name(k))).ToList();
        var field=SearchField(search,"キー名で検索");
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=66,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,14,0,0)};footer.Controls.Add(choose);footer.Controls.Add(SettingsForm.Button("キャンセル",(_,_)=>Close(),130));
        Controls.Add(new ThemedScrollHost(list));Controls.Add(count);Controls.Add(field);Controls.Add(footer);
        list.DrawItem+=(_,e)=>{if(e.Index<0)return;PickerPaint.Row(e,list.Items[e.Index].ToString()!,null);};
        search.TextChanged+=(_,_)=>Filter();list.SelectedIndexChanged+=(_,_)=>choose.Enabled=list.SelectedItem!=null;
        choose.Click+=(_,_)=>{if(list.SelectedItem is KeyItem item){SelectedCode=item.Code;DialogResult=DialogResult.OK;Close();}};
        list.DoubleClick+=(_,_)=>choose.PerformClick();AcceptButton=choose;
        Filter();list.SelectedItem=list.Items.Cast<KeyItem>().FirstOrDefault(x=>x.Code==selectedCode);Shown+=(_,_)=>search.Focus();
    }
    void Filter(){var matching=keys.Where(k=>k.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)).ToList();list.DataSource=matching;choose.Enabled=list.SelectedItem!=null;count.Text=matching.Count==0?"該当するキーがありません。":$"{matching.Count} 件";}
    sealed record KeyItem(int Code,string Name){public override string ToString()=>Name;}
    internal static Control SearchField(TextBox search,string label)
    {
        var panel=new Panel{Dock=DockStyle.Top,Height=76};var field=Visuals.Field(search);field.Dock=DockStyle.Bottom;
        panel.Controls.Add(field);panel.Controls.Add(new Label{Text=label,Dock=DockStyle.Top,Height=28,ForeColor=Visuals.Ink});return panel;
    }
}

internal sealed class AppPicker:Form
{
    readonly TextBox search=new(){PlaceholderText="アプリ名で検索"};
    readonly SteadyListBox list=new(){Dock=DockStyle.Fill,DisplayMember="Name",IntegralHeight=false,BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=48};
    readonly Label state=new(){Dock=DockStyle.Bottom,Height=38,Text="アプリを読み込んでいます…",ForeColor=Visuals.Muted};
    readonly UiButton select=new(){Text="このアプリを選択",Width=190,Primary=true,Enabled=false};
    readonly Dictionary<string,Icon> icons=new();
    List<AppTarget> apps=[];
    internal AppTarget? Selected=>list.SelectedItem as AppTarget;
    internal AppPicker()
    {
        Text="起動するアプリを選択";ClientSize=new Size(640,550);MinimumSize=new Size(500,400);Padding=new Padding(22);Font=new Font("Yu Gothic UI",10);BackColor=Visuals.Paper;StartPosition=FormStartPosition.CenterParent;
        ShowIcon=false;ShowInTaskbar=false;MinimizeBox=false;MaximizeBox=false;search.AccessibleName="アプリ名で検索";
        var field=KeyPicker.SearchField(search,"アプリ名で検索");
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=66,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,14,0,0)};footer.Controls.Add(select);footer.Controls.Add(SettingsForm.Button("キャンセル",(_,_)=>Close(),130));
        Controls.Add(new ThemedScrollHost(list));Controls.Add(state);Controls.Add(field);Controls.Add(footer);
        list.DrawItem+=(_,e)=>
        {
            if(e.Index<0||list.Items[e.Index] is not AppTarget app)return;
            if(!icons.TryGetValue(app.Id,out var icon))icons[app.Id]=icon=TargetIcon.Load(app);
            PickerPaint.Row(e,app.Name,icon);
        };
        search.TextChanged+=(_,_)=>Filter();list.SelectedIndexChanged+=(_,_)=>select.Enabled=Selected!=null;
        select.Click+=(_,_)=>{if(Selected!=null){DialogResult=DialogResult.OK;Close();}};list.DoubleClick+=(_,_)=>select.PerformClick();AcceptButton=select;
        Shown+=async (_,_)=>
        {
            search.Focus();
            try{apps=await AppCatalog.LoadAsync();if(!IsDisposed)Filter();}
            catch(Exception){if(!IsDisposed){state.Text="一覧を取得できません。戻って実行ファイルを指定してください。";state.ForeColor=Color.Firebrick;}}
        };
    }
    void Filter()
    {
        var matching=apps.Where(x=>x.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)).ToList();list.DataSource=matching;
        state.Text=matching.Count==0?"該当するアプリがありません。検索語を変えてください。":$"{matching.Count} 件";select.Enabled=Selected!=null;
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing){foreach(var icon in icons.Values)icon.Dispose();icons.Clear();}}
}

internal static class PickerPaint
{
    internal static void Row(DrawItemEventArgs e,string text,Icon? icon)
    {
        bool selected=(e.State&DrawItemState.Selected)!=0;
        using var fill=new SolidBrush(selected?Visuals.Navy:Visuals.Surface);e.Graphics.FillRectangle(fill,e.Bounds);
        int x=e.Bounds.Left+14;
        if(icon!=null){e.Graphics.DrawIcon(icon,new Rectangle(x,e.Bounds.Top+(e.Bounds.Height-24)/2,24,24));x+=38;}
        TextRenderer.DrawText(e.Graphics,text,e.Font,new Rectangle(x,e.Bounds.Top,e.Bounds.Right-x-10,e.Bounds.Height),selected?SystemColors.HighlightText:Visuals.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }
}
