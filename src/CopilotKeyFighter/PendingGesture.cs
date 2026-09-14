internal enum PendingDecision { Wait, Send, Cancel }
internal static class PendingGesture
{
    internal static PendingDecision Evaluate(long now, long deadline, bool sameGeneration, bool enabled, bool sameInputSequence, bool sameWindow, bool keysDown)
    {
        if (now >= deadline || !sameGeneration || !enabled || !sameInputSequence || !sameWindow) return PendingDecision.Cancel;
        return keysDown ? PendingDecision.Wait : PendingDecision.Send;
    }
}
