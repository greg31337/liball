namespace fix;

internal class ResendRequestActionResult
{
    internal bool Accepted;
    internal bool GenerateResendRequest;
    internal bool GapActive;
    internal bool Redundant;
    internal int BeginSeqNo;
    internal int EndSeqNo;
    internal int GapBeginSeqNo;
    internal int GapEndSeqNo;
    internal int HighestReceivedSeqNo;
    internal string Code = "";
    internal string Text = "";
}
