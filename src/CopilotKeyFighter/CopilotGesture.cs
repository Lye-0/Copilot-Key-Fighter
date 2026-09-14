// Pure state machine: never consumes any modifier, even if release order changes.
internal sealed class CopilotGesture
{
    bool f23Down;
    bool captured;
    internal (bool Suppress, bool Trigger) Process(uint key, bool down, bool leftWin, bool leftShift, bool otherModifier)
    {
        if (key != 0x86) return (false, false);
        if (!down)
        {
            bool suppress = captured;
            f23Down = captured = false;
            return (suppress, false);
        }
        if (f23Down) return (captured, false);
        f23Down = true;
        captured = leftWin && leftShift && !otherModifier;
        return (captured, captured);
    }
}
