namespace fix;

internal class SessionSequenceActionResult
{
    internal bool Accepted;
    internal bool Consumed;
    internal bool Queued;
    internal bool Duplicate;
    internal bool RejectMessage;
    internal bool Disconnect;
    internal bool PersistenceFailed;
    internal int SequenceNumber;
    internal int ExpectedSequenceNumber;
    internal int NextSenderSeqNum;
    internal int NextTargetSeqNum;
    internal int QueueCount;
    internal MessageDisposition Disposition;
    internal SessionEventKind EventKind;
    internal string Code = "";
    internal string Text = "";
}
