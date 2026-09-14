using Xunit;

public class SafetyTests
{
    [Fact]
    public void CopilotChordModifiersArrivingAfterF23DoNotCancelDelete()
    {
        var gesture = new CopilotGesture();
        Assert.Equal((true,true), gesture.Process(0x86, true, true, true, false));
        Assert.True(gesture.IsCaptured);
        foreach (uint key in new uint[] {0x5B, 0xA0, 0x10})
            Assert.False(KeyboardHook.CancelsPending(key, true, false, gesture.IsCaptured));
        foreach (uint key in new uint[] {0x41, 0xA1, 0xA2, 0x5C})
            Assert.True(KeyboardHook.CancelsPending(key, true, false, gesture.IsCaptured));
        Assert.Equal((true,false), gesture.Process(0x86, false, false, false, false));
        Assert.False(gesture.IsCaptured);
        Assert.True(KeyboardHook.CancelsPending(0xA0, true, false, gesture.IsCaptured));
        Assert.False(ReleaseGate.Blocked(_ => false, _ => false, 0x2E));
        Assert.Equal(new[] {new KeyEvent(0x2E,false),new KeyEvent(0x2E,true)}, KeySequence.Create(new(0x2E)));
    }
    [Fact]
    public void RepeatedHeldModifiersAreNotNewUserActions()
    {
        foreach(uint key in new uint[] {0x5B, 0xA0, 0x41})
        {
            Assert.False(KeyboardHook.CancelsPending(key, true, true));
            Assert.True(KeyboardHook.CancelsPending(key, true, false));
            Assert.False(KeyboardHook.CancelsPending(key, false, true));
        }
        Assert.False(KeyboardHook.CancelsPending(0x86, true, false));
    }
    [Fact]
    public void ReleaseGateUsesPairedPhysicalF23InsteadOfReservedAsyncStates()
    {
        Assert.False(ReleaseGate.Blocked(_ => false, k => k is 0x86 or 0xFC or 0xE5, 0x2E));
        Assert.True(ReleaseGate.Blocked(k => k == 0x86, _ => false, 0x2E));
        Assert.True(ReleaseGate.Blocked(k => k == 0x41, _ => false, 0x2E));
        foreach (int key in new[] { 1, 2, 4, 5, 6, 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x2E })
            Assert.True(ReleaseGate.Blocked(_ => false, k => k == key, 0x2E));
        var g = new CopilotGesture();
        Assert.Equal((true,true), g.Process(0x86,true,true,true,false));
        Assert.Equal((true,false), g.Process(0x86,false,false,false,false));
        Assert.False(ReleaseGate.Blocked(_ => false, _ => false, 0x2E));
    }
    [Fact]
    public void PendingGestureNeverSendsAfterInterferenceOrExpiry()
    {
        Assert.Equal(PendingDecision.Wait, PendingGesture.Evaluate(1, 100, true, true, true, true, true));
        Assert.Equal(PendingDecision.Send, PendingGesture.Evaluate(1, 100, true, true, true, true, false));
        for (int bits = 0; bits < 15; bits++)
            Assert.Equal(PendingDecision.Cancel, PendingGesture.Evaluate(1, 100, (bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0, (bits & 8) != 0, false));
        Assert.Equal(PendingDecision.Cancel, PendingGesture.Evaluate(100, 100, true, true, true, true, false));
    }
    [Fact]
    public void EveryShortcutIsBalancedAndEveryPartialSendHasRecovery()
    {
        for (int mods = 0; mods < 16; mods++)
        {
            var input = KeySequence.Create(new(0x41, (mods & 1) != 0, (mods & 2) != 0, (mods & 4) != 0, (mods & 8) != 0));
            for (int sent = 0; sent <= input.Length; sent++)
            {
                var held = new HashSet<int>();
                foreach (var e in input.Take(sent).Concat(KeySequence.Recovery(input, sent)))
                {
                    if (e.Up) Assert.True(held.Remove(e.Key)); else Assert.True(held.Add(e.Key));
                }
                Assert.Empty(held);
            }
        }
    }
    [Theory]
    [InlineData(0x5B)] [InlineData(0x10)] [InlineData(0xA2)] [InlineData(0x86)] [InlineData(0xFC)] [InlineData(0)] [InlineData(0xFFFF)]
    public void UnsafeOrRecursiveKeysAreRejected(int key) => Assert.Throws<ArgumentException>(() => new KeyBinding(key).Validate());
    [Fact]
    public void SecureAttentionIsRejected() => Assert.Throws<ArgumentException>(() => new KeyBinding(0x2E, Control:true, Alt:true).Validate());
    [Theory]
    [InlineData(0x2E)] [InlineData(0x25)] [InlineData(0x6F)] [InlineData(0xB3)]
    public void ExtendedKeysAreEncoded(int key) => Assert.True(KeySender.IsExtended(key));
    [Fact]
    public void CapturedReleaseStillSuppressedAfterDisable()
    {
        var g = new CopilotGesture();
        Assert.Equal((true, true), g.Process(0x86, true, true, true, false));
        Assert.Equal((true, false), g.Process(0x86, false, false, false, false));
        Assert.Equal((false, false), g.Process(0x86, true, false, true, false));
    }
    [Fact]
    public void InvalidSettingsFailOpenAndOriginalRemainsAvailable()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CKF-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var store = new SettingsStore(directory);
            File.WriteAllText(store.FilePath, "{ broken");
            var result = store.Load();
            Assert.False(result.Settings.Enabled); Assert.NotNull(result.Error);
            Assert.Equal("{ broken", File.ReadAllText(store.FilePath));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void SettingsRoundTripAndInvalidSaveDoesNotOverwrite()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CKF-tests-" + Guid.NewGuid());
        try
        {
            var store = new SettingsStore(directory);
            var valid = new Settings(Enabled:true, Action:ActionKind.Keyboard, Binding:new(0x41, Control:true));
            store.Save(valid);
            Assert.Equal(valid, store.Load().Settings);
            Assert.Throws<ArgumentException>(() => store.Save(valid with { Binding = new(0x86) }));
            Assert.Equal(valid, store.Load().Settings);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void ExeDoesNotRequireAnAumid()
    {
        new Settings(Enabled:true, Target:new(TargetKind.Executable, "Example", @"C:\Apps\Example.exe")).Validate(false);
        Assert.Throws<ArgumentException>(() => new Settings(Enabled:true, Target:new(TargetKind.Packaged, "Example", @"C:\Apps\Example.exe")).Validate(false));
    }
}
