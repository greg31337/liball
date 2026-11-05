namespace fix;

internal class SessionStateTransitionResult
{
    internal bool Accepted;
    internal bool Changed;
    internal SessionState PreviousState;
    internal SessionState State;
    internal SessionEventKind EventKind;
    internal string Code = "";
    internal string Text = "";
}
