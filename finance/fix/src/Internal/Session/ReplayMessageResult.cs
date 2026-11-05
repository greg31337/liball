namespace fix;

internal class ReplayMessageResult
{
    internal bool Success;
    internal bool IsGapFill;
    internal int BeginSeqNo;
    internal int EndSeqNo;
    internal int OriginalSequenceNumber;
    internal string MsgType = "";
    internal FixMessage? Message;
    internal byte[] Bytes = [];
    internal byte[] OriginalSendingTimeValue = [];
    internal ApplicationDecision? ApplicationDecision;
    internal string Code = "";
    internal string Text = "";
}
