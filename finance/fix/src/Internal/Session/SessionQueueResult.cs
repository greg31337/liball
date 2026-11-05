namespace fix;

internal class SessionQueueResult
{
    internal bool Accepted;
    internal bool Disconnect;
    internal int SequenceNumber;
    internal string Code = "";
    internal string Text = "";
}
