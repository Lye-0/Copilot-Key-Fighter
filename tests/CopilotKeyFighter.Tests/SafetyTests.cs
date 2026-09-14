using Xunit;

public class SafetyTests
{
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
