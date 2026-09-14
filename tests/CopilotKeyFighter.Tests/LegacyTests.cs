using Xunit;
public class LegacyTests { [Fact] public void Original2576Assertions() {
int checks = 0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
// Every modifier release order, repeated down events, and unrelated key events.
foreach (bool releaseWinFirst in new[] { false, true })
{
    var g = new CopilotGesture();
    Check(g.Process(0x5B, true, false, false, false) == (false, false), "Win down passes");
    Check(g.Process(0xA0, true, true, false, false) == (false, false), "Shift down passes");
    Check(g.Process(0x86, true, true, true, false) == (true, true), "Copilot first down");
    for (int n = 0; n < 20; n++) Check(g.Process(0x86, true, true, true, false) == (true, false), "Repeat does not trigger");
    if (releaseWinFirst)
    {
        Check(g.Process(0x5B, false, true, true, false) == (false, false), "Win up before F23 passes");
        Check(g.Process(0xA0, false, false, true, false) == (false, false), "Shift up before F23 passes");
    }
    Check(g.Process(0x86, false, !releaseWinFirst, !releaseWinFirst, false) == (true, false), "Captured up stays suppressed");
    Check(g.Process(0xA0, false, true, true, false) == (false, false), "Shift up passes");
    Check(g.Process(0x5B, false, true, false, false) == (false, false), "Win up passes");
}
for (uint key = 0; key < 256; key++)
{
    if (key == 0x86) continue;
    var g = new CopilotGesture();
    Check(g.Process(key, true, true, true, false) == (false, false), $"Other key {key} down");
    Check(g.Process(key, false, true, true, false) == (false, false), $"Other key {key} up");
}
foreach (var mods in new[] { (false,false,false), (true,false,false), (false,true,false), (true,true,true) })
{
    var g = new CopilotGesture();
    Check(g.Process(0x86, true, mods.Item1, mods.Item2, mods.Item3) == (false,false), "Unmatched F23 down");
    Check(g.Process(0x86, true, true, true, false) == (false,false), "Modifiers added during held F23 do not capture");
    Check(g.Process(0x86, false, true, true, false) == (false,false), "Unmatched F23 up");
}
var repeated = new CopilotGesture();
for (int n = 0; n < 1000; n++)
{
    Check(repeated.Process(0x86, true, true, true, false) == (true,true), "Repeated complete gesture down");
    Check(repeated.Process(0x86, false, false, false, false) == (true,false), "Repeated complete gesture up");
}
Console.WriteLine($"PASS: {checks} assertions");

}}
