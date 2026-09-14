internal static class ReleaseGate
{
    // Only the hook can reliably pair the suppressed F23 down/up. Do not use the
    // OS state of every virtual/reserved/IME key as a condition for sending.
    internal static bool Blocked(Func<int, bool> physicalDown, Func<int, bool> asyncDown, int targetKey)
    {
        for (int key = 1; key < 255; key++)
            if (key != 0xFC && physicalDown(key)) return true;
        // Keys held before monitoring began still protect modifier semantics and
        // a concurrently held target/mouse button. Never synthesize their release.
        foreach (int key in new[] { 1, 2, 4, 5, 6, 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 })
            if (asyncDown(key)) return true;
        return asyncDown(targetKey);
    }
}
