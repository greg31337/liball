namespace pop3;

internal class Pop3ServerPendingResponse
{
    internal string? Keyword;
    internal string? ErrorText;
    internal bool HandlingStarted;
    internal bool ResponseStarted;
    internal bool IntakeBarrier;
}
