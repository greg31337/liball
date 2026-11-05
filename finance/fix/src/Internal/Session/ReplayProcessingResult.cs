using System.Collections.Generic;

namespace fix;

internal class ReplayProcessingResult
{
    internal bool Accepted;
    internal bool Disconnect;
    internal bool StoreCorrupt;
    internal bool UsedUnavailableGapFill;
    internal int BeginSeqNo;
    internal int EndSeqNo;
    internal int ReplayedApplicationCount;
    internal int SkippedApplicationCount;
    internal int AdministrativeSequenceCount;
    internal int UnavailableSequenceCount;
    internal int GapFillCount;
    internal int NextSenderSeqNum;
    internal string Code = "";
    internal string Text = "";
    internal List<ReplayMessageResult> Messages = [];
}
