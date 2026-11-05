namespace fix;

internal class LogoutActionResult
{
    internal bool Accepted;
    internal bool SendLogout;
    internal bool CloseTransport;
    internal bool ImmediateClose;
    internal bool AwaitingPeerLogout;
    internal bool SimultaneousLogout;
    internal bool TimedOut;
    internal SessionState State;
    internal FixMessage? OutboundMessage;
    internal string Code = "";
    internal string Text = "";
    internal string PeerText = "";
    internal ApplicationDecision? AdministrativeDecision;
    internal ApplicationDecision? LogoutDecision;
    internal System.Collections.Generic.List<ValidationIssue> Issues = [];
}
