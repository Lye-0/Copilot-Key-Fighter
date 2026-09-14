using System.Runtime.InteropServices;

internal readonly record struct KeyEvent(int Key, bool Up);
internal static class KeySequence
{
    internal static KeyEvent[] Create(KeyBinding binding)
    {
        binding.Validate();
        var keys = new List<int>();
        if (binding.Control) keys.Add(0xA2);
        if (binding.Shift) keys.Add(0xA0);
        if (binding.Alt) keys.Add(0xA4);
        if (binding.Win) keys.Add(0x5B);
        keys.Add(binding.Key);
        return keys.Select(k => new KeyEvent(k, false)).Concat(keys.AsEnumerable().Reverse().Select(k => new KeyEvent(k, true))).ToArray();
    }
    internal static KeyEvent[] Recovery(KeyEvent[] sequence, int sent)
    {
        var held = new List<int>();
        foreach (var e in sequence.Take(sent)) { if (e.Up) held.Remove(e.Key); else held.Add(e.Key); }
        return held.AsEnumerable().Reverse().Select(k => new KeyEvent(k, true)).ToArray();
    }
}
internal static class KeySender
{
    static bool unavailable;
    internal const nuint Tag = 0x434B4631;
    internal static bool IsExtended(int key) => key is 0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2D or 0x2E or 0x5B or 0x5C or 0x6F or >= 0xA6 and <= 0xB7;
    internal static void Send(KeyBinding binding, Func<int, bool>? physicallyDown = null)
    {
        if (unavailable) throw new InvalidOperationException("前回のキー解放を確認できなかったため送信を停止しています。キーを離して本アプリを再起動してください。");
        if (ReleaseGate.Blocked(physicallyDown ?? (_ => false), Native.Down, binding.Key)) throw new InvalidOperationException("押されているキーを離してから、もう一度お試しください。");
        var events = KeySequence.Create(binding);
        var input = events.Select(Convert).ToArray();
        uint sent = Native.SendInput((uint)input.Length, input, Marshal.SizeOf<Native.Input>());
        if (sent == input.Length) return;
        var recovery = KeySequence.Recovery(events, (int)sent).Where(e => physicallyDown?.Invoke(e.Key) != true).Select(Convert).ToArray();
        // SendInput inserts each batch without interleaving. Only balance our accepted prefix.
        if (recovery.Length > 0 && Native.SendInput((uint)recovery.Length, recovery, Marshal.SizeOf<Native.Input>()) != recovery.Length)
        {
            unavailable = true;
            throw new InvalidOperationException("キーの解放を完了できませんでした。送信を停止しました。指定した修飾キーを一度押して離し、本アプリを再起動してください。");
        }
        throw new InvalidOperationException("キーを送信できませんでした。管理者権限のアプリや保護された画面では使用できません。");
    }
    static Native.Input Convert(KeyEvent e)
    {
        var input = Native.Input.Key((uint)e.Key, e.Up, Tag);
        if (IsExtended(e.Key)) input.flags |= 1;
        return input;
    }
}
