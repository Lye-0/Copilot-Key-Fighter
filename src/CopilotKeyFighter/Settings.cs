using System.Text.Json;
using System.Text.Json.Serialization;

internal enum ActionKind { Application, Keyboard, Disabled }
internal enum TargetKind { Packaged, Executable, Shell }
internal sealed record AppTarget(TargetKind Kind, string Name, string Id, string Executable = "", string Arguments = "", string WorkingDirectory = "");
internal sealed record KeyBinding(int Key = 0x2E, bool Control = false, bool Shift = false, bool Alt = false, bool Win = false)
{
    internal static bool IsModifier(int key) => key is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or >= 0xA0 and <= 0xA5;
    internal void Validate()
    {
        if (Key < 8 || Key > 0xFE || IsModifier(Key) || Key is 0x86 or 0xFC or 0xE7 or 0xFF)
            throw new ArgumentException("通常キーを選んでください。修飾キー単独・F23・内部予約キーは指定できません。");
        if (Control && Alt && Key == 0x2E) throw new ArgumentException("Ctrl + Alt + Delete は Windows に保護されているため指定できません。");
    }
    public override string ToString() => string.Join(" + ", new[] { Control ? "Ctrl" : null, Shift ? "Shift" : null, Alt ? "Alt" : null, Win ? "Win" : null, ((Keys)Key).ToString() }.Where(x => x != null));
}
internal sealed record Settings(int Version = 1, bool Enabled = false, ActionKind Action = ActionKind.Application, AppTarget? Target = null, KeyBinding? Binding = null, bool PreferExisting = true, bool StartWithWindows = false)
{
    internal void Validate(bool checkFiles = true)
    {
        if (Version != 1 || !Enum.IsDefined(Action)) throw new ArgumentException("未対応の設定形式です。");
        if (Action == ActionKind.Keyboard) (Binding ?? throw new ArgumentException("送信するキーを指定してください。")).Validate();
        if (Action != ActionKind.Application || (!Enabled && Target == null)) return;
        var t = Target ?? throw new ArgumentException("起動するアプリを選んでください。");
        if (!Enum.IsDefined(t.Kind) || string.IsNullOrWhiteSpace(t.Id)) throw new ArgumentException("アプリの指定が不正です。");
        if (t.Kind == TargetKind.Packaged && !t.Id.Contains('!')) throw new ArgumentException("パッケージアプリの AUMID を指定してください。");
        if (t.Kind == TargetKind.Executable && (!Path.IsPathFullyQualified(t.Id) || !t.Id.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || (checkFiles && !File.Exists(t.Id))))
            throw new ArgumentException("存在する実行ファイル（.exe）を指定してください。");
        if (checkFiles && t.WorkingDirectory.Length > 0 && !Directory.Exists(t.WorkingDirectory)) throw new ArgumentException("作業フォルダーが見つかりません。");
    }
}
internal sealed class SettingsStore(string directory)
{
    internal string FilePath => Path.Combine(directory, "settings.json");
    internal static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    internal (Settings Settings, string? Error) Load()
    {
        if (!File.Exists(FilePath)) return (new(), null);
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Options) ?? throw new InvalidDataException("設定が空です。");
            settings.Validate();
            return (settings, null);
        }
        catch (Exception e) { return (new(), "設定を読み込めませんでした。割り当ては停止しています。\n" + e.Message); }
    }
    internal void Save(Settings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(directory);
        string temp = Path.Combine(directory, Guid.NewGuid() + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, FilePath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
