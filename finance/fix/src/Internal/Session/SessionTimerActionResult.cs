namespace fix;

internal class SessionTimerActionResult
{
    internal bool Accepted;
    internal bool SendHeartbeat;
    internal bool SendTestRequest;
    internal bool Disconnect;
    internal bool MatchedTestRequest;
    internal bool TestRequestPending;
    internal string TestRequestId = "";
    internal SessionEventKind EventKind;
    internal string Code = "";
    internal string Text = "";
}
