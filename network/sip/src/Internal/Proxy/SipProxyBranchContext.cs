namespace sip;

internal class SipProxyBranchContext
{
    internal string? Branch;
    internal string? SentByHost;
    internal int SentByPort = -1;
    internal SipTransportMessage? ReceivedRequest;
    internal SipProxyTarget? Target;
    internal string? CorrelationId;
    internal string? OperationId;
    internal SipMessage? OutboundRequest;
    internal SipMessage? FinalResponse;
    internal long TimerCDeadlineMilliseconds = -1;
    internal int TimerCIntervalMilliseconds;
    internal bool Stateful;
    internal bool Completed;
    internal bool TimedOut;
    internal bool ProvisionalReceived;
    internal bool CancellationPending;
    internal bool CancelSent;
    internal bool SelectedFinal;
    internal string? AuthenticatedIdentity;
}
