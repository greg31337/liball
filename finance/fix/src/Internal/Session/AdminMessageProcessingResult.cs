namespace fix;

internal class AdminMessageProcessingResult
{
    internal bool Accepted;
    internal bool Disconnect;
    internal bool RouteToLogonProcessor;
    internal bool RouteToLogoutProcessor;
    internal bool SendHeartbeat;
    internal int SequenceNumber;
    internal string MsgType = "";
    internal string Code = "";
    internal string Text = "";
    internal FixMessage? OutboundMessage;
    internal ApplicationDecision? ApplicationDecision;
    internal SessionTimerActionResult? TimerAction;
    internal ReplayProcessingResult? ReplayResult;
    internal SequenceResetProcessingResult? SequenceResetResult;
}
