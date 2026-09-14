using System.Drawing;

internal sealed class SettingsForm : Form
{
    readonly Action<Settings> save;
    readonly CheckBox enabled = new() { Text = "Copilot キーの割り当てを有効にする", AutoSize = true };
    readonly ComboBox action = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    readonly TextBox targetName = new() { ReadOnly = true, Dock = DockStyle.Fill };
    readonly TextBox id = new() { Dock = DockStyle.Fill, ReadOnly = true };
    readonly TextBox arguments = new() { Dock = DockStyle.Fill };
    readonly TextBox directory = new() { Dock = DockStyle.Fill };
    readonly CheckBox existing = new() { Text = "起動済みのウィンドウを優先する", AutoSize = true };
    readonly CheckBox startup = new() { Text = "Windows へのログイン時に起動する", AutoSize = true };
    readonly ComboBox key = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    readonly CheckBox ctrl = new() { Text = "Ctrl", AutoSize = true }, shift = new() { Text = "Shift", AutoSize = true }, alt = new() { Text = "Alt", AutoSize = true }, win = new() { Text = "Win", AutoSize = true };
    readonly Label feedback = new() { AutoSize = true, MaximumSize = new(620, 0), ForeColor = Color.Firebrick };
    readonly TableLayoutPanel appPanel = Grid(), keyPanel = Grid();
    AppTarget? target;
    bool dirty;
    readonly Icon ownedIcon;

    internal SettingsForm(Settings settings, Action<Settings> save)
    {
        this.save = save;
        Text = "Copilot Key Fighter — 設定";
        ownedIcon = Program.LoadIcon(); Icon = ownedIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Yu Gothic UI", 10);
        ClientSize = new Size(690, 680); MinimumSize = new Size(640, 580);
        StartPosition = FormStartPosition.CenterScreen;
        var root = Grid(); root.Dock = DockStyle.Fill; root.AutoScroll = true; root.Padding = new Padding(24);
        Controls.Add(root);
        Add(root, new Label { Text = "Copilot キーの動作", Font = new Font(Font.FontFamily, 19, FontStyle.Bold), AutoSize = true });
        Add(root, new Label { Text = "アプリを呼び出す、キーを送る、標準動作を止める。", AutoSize = true, Margin = new Padding(0, 4, 0, 18) });
        Add(root, enabled);
        action.Items.AddRange(["アプリを開く", "キー・ショートカットを送る", "何もしない（標準動作を抑止）"]);
        Add(root, Label("割り当てる動作")); Add(root, action);

        Add(appPanel, Label("起動するアプリ")); Add(appPanel, targetName);
        var choose = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        choose.Controls.Add(Button("アプリ一覧から選ぶ…", async (_, _) =>
        {
            using var picker = new AppPicker();
            if (picker.ShowDialog(this) == DialogResult.OK) SetTarget(picker.Selected!);
            await Task.CompletedTask;
        }));
        choose.Controls.Add(Button("実行ファイルを選ぶ…", (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "実行ファイル (*.exe)|*.exe", CheckFileExists = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) SetTarget(new(TargetKind.Executable, Path.GetFileNameWithoutExtension(dialog.FileName), dialog.FileName, dialog.FileName));
        }));
        choose.Controls.Add(Button("ID を直接指定…", (_, _) =>
        {
            using var dialog = new Form { Text = "パッケージアプリの AUMID", ClientSize = new Size(540, 155), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };
            var field = new TextBox { Left = 16, Top = 45, Width = 500, Text = target?.Kind == TargetKind.Packaged ? target.Id : "" };
            var ok = new Button { Text = "指定", Left = 420, Top = 95, DialogResult = DialogResult.OK };
            dialog.Controls.AddRange([new Label { Text = "取得済みの AUMID を入力してください。", AutoSize = true, Left = 16, Top = 15 }, field, ok]); dialog.AcceptButton = ok;
            if (dialog.ShowDialog(this) == DialogResult.OK && field.Text.Trim().Contains('!')) SetTarget(new(TargetKind.Packaged, field.Text.Trim(), field.Text.Trim()));
        }));
        Add(appPanel, choose);
        Add(appPanel, Label("取得した ID / 実行ファイル")); Add(appPanel, id);
        Add(appPanel, Label("起動引数（省略可）")); Add(appPanel, arguments);
        Add(appPanel, Label("作業フォルダー（実行ファイルのみ・省略可）")); Add(appPanel, directory);
        Add(appPanel, existing);
        Add(appPanel, Button("このアプリを開いて確認", async (sender, _) =>
        {
            var button = (Button)sender!; button.Enabled = false;
            try { var value = Read(); value.Validate(); if (value.Target == null) throw new ArgumentException("アプリを選んでください。"); await AppLauncher.OpenAsync(value.Target, value.PreferExisting); feedback.Text = "起動処理を実行しました。ウィンドウをご確認ください。"; }
            catch (Exception e) { feedback.Text = e.Message; }
            finally { button.Enabled = true; }
        }));
        Add(root, appPanel);

        Add(keyPanel, Label("送信するキー"));
        var keys = Enum.GetValues<Keys>().Select(k => (int)k).Distinct().Where(k => k >= 8 && k <= 0xFE && !KeyBinding.IsModifier(k) && k is not (0x86 or 0xFC or 0xE7)).ToArray();
        foreach (var code in keys) key.Items.Add(new KeyItem(code));
        Add(keyPanel, key);
        var mods = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; mods.Controls.AddRange([ctrl, shift, alt, win]); Add(keyPanel, mods);
        Add(keyPanel, new Label { Text = "Copilot キーを離した後、1回送信します。\nCtrl + Alt + Delete、修飾キー単独、長押しの再現には対応していません。", AutoSize = true, MaximumSize = new Size(600, 0) });
        Add(keyPanel, Label("入力の確認欄（保存後、この欄を選んで Copilot キーを押せます）"));
        Add(keyPanel, new TextBox { Dock = DockStyle.Fill, AccessibleName = "キー入力の確認欄" });
        Add(root, keyPanel);
        Add(root, startup);
        Add(root, feedback);
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 15, 0, 0) };
        var apply = Button("保存して適用", (_, _) =>
        {
            try { save(Read()); dirty = false; feedback.ForeColor = Color.DarkGreen; feedback.Text = "保存しました。トレイに常駐します。この画面は閉じて構いません。"; }
            catch (Exception e) { feedback.ForeColor = Color.Firebrick; feedback.Text = e.Message; }
        });
        footer.Controls.Add(apply); footer.Controls.Add(Button("閉じる", (_, _) => Close())); Add(root, footer);
        AcceptButton = apply;
        enabled.Checked = settings.Enabled; action.SelectedIndex = (int)settings.Action; existing.Checked = settings.PreferExisting; startup.Checked = settings.StartWithWindows;
        var binding = settings.Binding ?? new();
        key.SelectedItem = key.Items.Cast<KeyItem>().FirstOrDefault(k => k.Code == binding.Key);
        ctrl.Checked = binding.Control; shift.Checked = binding.Shift; alt.Checked = binding.Alt; win.Checked = binding.Win;
        if (settings.Target != null) SetTarget(settings.Target);
        action.SelectedIndexChanged += (_, _) => { UpdatePanels(); dirty = true; };
        foreach (var control in new Control[] { enabled, existing, startup, ctrl, shift, alt, win }) ((CheckBox)control).CheckedChanged += (_, _) => dirty = true;
        foreach (var field in new[] { arguments, directory }) field.TextChanged += (_, _) => dirty = true;
        key.SelectedIndexChanged += (_, _) => dirty = true;
        UpdatePanels(); dirty = false;
        FormClosing += (_, e) => { if (dirty && MessageBox.Show(this, "保存していない変更を破棄して閉じますか？", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) e.Cancel = true; };
    }
    Settings Read() => new(Enabled: enabled.Checked, Action: (ActionKind)action.SelectedIndex,
        Target: target == null ? null : target with { Arguments = arguments.Text, WorkingDirectory = directory.Text.Trim() },
        Binding: new((key.SelectedItem as KeyItem)?.Code ?? 0, ctrl.Checked, shift.Checked, alt.Checked, win.Checked), PreferExisting: existing.Checked, StartWithWindows: startup.Checked);
    void SetTarget(AppTarget value)
    {
        target = value; targetName.Text = value.Name; id.Text = value.Id; arguments.Text = value.Arguments; directory.Text = value.WorkingDirectory;
        arguments.Enabled = value.Kind != TargetKind.Shell; directory.Enabled = value.Kind == TargetKind.Executable; dirty = true;
    }
    void UpdatePanels() { appPanel.Visible = action.SelectedIndex == 0; keyPanel.Visible = action.SelectedIndex == 1; }
    static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
    static TableLayoutPanel Grid() => new() { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
    static void Add(TableLayoutPanel panel, Control control) { control.Margin = new Padding(0, 3, 0, 5); panel.Controls.Add(control, 0, panel.RowCount++); }
    static Button Button(string text, EventHandler handler) { var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 4, 8, 4) }; button.Click += handler; return button; }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) ownedIcon.Dispose(); }
    sealed record KeyItem(int Code) { public override string ToString() => ((Keys)Code).ToString(); }
}

internal sealed class AppPicker : Form
{
    readonly TextBox search = new() { Dock = DockStyle.Top, PlaceholderText = "アプリ名で検索" };
    readonly ListBox list = new() { Dock = DockStyle.Fill, DisplayMember = "Name", IntegralHeight = false };
    readonly Label state = new() { Dock = DockStyle.Bottom, Height = 42, Text = "アプリ一覧を読み込んでいます…" };
    readonly Button select = new() { Text = "このアプリを選ぶ", Dock = DockStyle.Bottom, Height = 40, Enabled = false };
    List<AppTarget> apps = [];
    internal AppTarget? Selected => list.SelectedItem as AppTarget;
    internal AppPicker()
    {
        Text = "起動するアプリを選ぶ"; ClientSize = new Size(570, 520); Padding = new Padding(16);
        Font = new System.Drawing.Font("Yu Gothic UI", 10); StartPosition = FormStartPosition.CenterParent;
        Controls.AddRange([list, state, select, search]);
        search.TextChanged += (_, _) => Filter();
        list.SelectedIndexChanged += (_, _) => { select.Enabled = Selected != null; state.Text = Selected == null ? "アプリを選択してください。" : Selected.Kind == TargetKind.Executable ? "実行ファイルから起動します。" : "選択するとアプリの ID を自動で設定します。"; };
        select.Click += (_, _) => { if (Selected != null) { DialogResult = DialogResult.OK; Close(); } };
        list.DoubleClick += (_, _) => select.PerformClick();
        Shown += async (_, _) =>
        {
            try { apps = await AppCatalog.LoadAsync(); if (!IsDisposed) { Filter(); state.Text = $"{apps.Count} 件のアプリ。見つからない場合は実行ファイルを指定できます。"; } }
            catch (Exception e) { if (!IsDisposed) state.Text = "取得できませんでした: " + e.Message; }
        };
    }
    void Filter() { list.DataSource = apps.Where(x => x.Name.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).ToList(); }
}
