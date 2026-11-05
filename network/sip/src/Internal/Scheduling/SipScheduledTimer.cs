namespace sip;

internal class SipScheduledTimer
{
    internal long Id;
    internal string? OwnerId;
    internal string? Name;
    internal long DeadlineMilliseconds = -1;
    internal long Sequence;
}
