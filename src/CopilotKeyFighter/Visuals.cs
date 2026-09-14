using System.Drawing;
using System.Drawing.Drawing2D;

internal static class Visuals
{
    internal static readonly Color Navy = Color.FromArgb(9, 28, 52), Cyan = Color.FromArgb(0, 221, 230), Ink = Color.FromArgb(21, 42, 63), Muted = Color.FromArgb(84, 103, 120), Paper = Color.FromArgb(244, 247, 250), Border = Color.FromArgb(205, 216, 225);
    internal static void Apply(Control parent)
    {
        if (SystemInformation.HighContrast) return;
        parent.ForeColor = Ink;
        if (parent is Form) parent.BackColor = Paper;
        foreach (Control child in parent.Controls)
        {
            if (child is BrandPanel) continue;
            Apply(child);
            if (child is Button b)
            {
                b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Border; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 242, 245);
                b.BackColor = Color.White; b.ForeColor = Ink; b.Cursor = Cursors.Hand;
            }
            if (child is TextBox text) { text.BackColor = text.ReadOnly ? Color.FromArgb(232, 238, 243) : Color.White; text.BorderStyle = BorderStyle.FixedSingle; }
            if (child is ComboBox combo)
            {
                combo.FlatStyle = FlatStyle.Flat; combo.BackColor = Color.White; combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += (_, e) =>
                {
                    if (e.Index < 0) return;
                    bool selected = (e.State & DrawItemState.Selected) != 0;
                    using var fill = new SolidBrush(selected ? Navy : Color.White); e.Graphics.FillRectangle(fill, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, combo.Items[e.Index]?.ToString() ?? "", e.Font, e.Bounds, selected ? Color.White : Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    e.DrawFocusRectangle();
                };
            }
        }
    }
    internal static void Primary(Button button)
    {
        if (SystemInformation.HighContrast) return;
        button.BackColor = Navy; button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 61, 87);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 85, 104);
        button.FlatAppearance.BorderColor = Navy;
    }
}

internal sealed class ModeSelector : FlowLayoutPanel
{
    readonly RadioButton[] choices;
    internal event EventHandler? SelectedIndexChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int SelectedIndex
    {
        get => Array.FindIndex(choices, c => c.Checked);
        set { if (value >= 0 && value < choices.Length) choices[value].Checked = true; }
    }
    internal ModeSelector()
    {
        AutoSize = true; Dock = DockStyle.Fill; WrapContents = true;
        choices = new[] { "アプリを開く", "キーを送る", "何もしない" }.Select(text => new RadioButton
        {
            Text = text, Appearance = Appearance.Button, AutoSize = true, Padding = new Padding(13, 9, 13, 9), FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 0, 7, 4), Cursor = Cursors.Hand,
            AccessibleDescription = "Copilot キーの割り当て方式"
        }).ToArray();
        foreach (var choice in choices)
        {
            choice.CheckedChanged += (_, _) => { RefreshColors(); if (choice.Checked) SelectedIndexChanged?.Invoke(this, EventArgs.Empty); };
            Controls.Add(choice);
        }
        SelectedIndex = 0;
    }
    internal void RefreshColors()
    {
        if (SystemInformation.HighContrast) return;
        foreach (var choice in choices)
        {
            choice.BackColor = choice.Checked ? Visuals.Navy : Color.White;
            choice.ForeColor = choice.Checked ? Color.White : Visuals.Ink;
            choice.FlatAppearance.BorderColor = choice.Checked ? Visuals.Navy : Visuals.Border;
            choice.FlatAppearance.CheckedBackColor = Visuals.Navy;
            choice.FlatAppearance.MouseOverBackColor = choice.Checked ? Color.FromArgb(18,61,87) : Color.FromArgb(225,242,245);
        }
    }
}

internal sealed class BrandPanel : Panel
{
    readonly Image key;
    internal BrandPanel()
    {
        DoubleBuffered = true; Width = 260; Dock = DockStyle.Left; BackColor = Visuals.Navy;
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("CopilotKeyFighter.Key.png")!;
        using var source = Image.FromStream(stream); key = new Bitmap(source);
        AccessibleName = "Copilot Key Fighter";
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        // Scale to the actual panel width; DPI may already have scaled the control.
        float scale = Width / 260f;
        using var title = new Font("Segoe UI", 28*scale, FontStyle.Bold, GraphicsUnit.Pixel);
        using var small = new Font("Segoe UI", 10*scale, FontStyle.Bold, GraphicsUnit.Pixel);
        using var japanese = new Font("Yu Gothic UI", 22*scale, FontStyle.Bold, GraphicsUnit.Pixel);
        using var text = new Font("Yu Gothic UI", 15*scale, FontStyle.Regular, GraphicsUnit.Pixel);
        using var white = new SolidBrush(Color.White);
        using var cyan = new SolidBrush(Visuals.Cyan);
        using var muted = new SolidBrush(Color.FromArgb(171,193,213));
        float x=26*scale, w=Width-52*scale;
        g.DrawString("COPILOT\nKEY FIGHTER", title, white, new RectangleF(x,34*scale,w,100*scale));
        g.FillRectangle(cyan,x,149*scale,34*scale,3*scale);
        var imageWidth = Math.Min(w,210*scale);
        g.DrawImage(key, new RectangleF((Width-imageWidth)/2,192*scale,imageWidth,imageWidth*key.Height/key.Width));
        g.DrawString("そのキーを、\n自分のために。", japanese, white, new RectangleF(x,400*scale,w,85*scale));
        g.DrawString("いつもの操作へ、\nひと押しで。", text, muted, new RectangleF(x,502*scale,w,65*scale));
        if (Height>640*scale) g.DrawString("YOUR KEY. YOUR CHOICE.",small,cyan,new PointF(x,Height-45*scale));
    }
    protected override void Dispose(bool disposing) { if(disposing) key.Dispose(); base.Dispose(disposing); }
}
