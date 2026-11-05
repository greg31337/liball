using System.Collections.Generic;

namespace fix;

internal class SequenceResetProcessingResult
{
    internal bool Accepted;
    internal bool Ignored;
    internal bool Rejected;
    internal bool Disconnect;
    internal bool Queued;
    internal bool SequenceAdvanced;
    internal bool RecoveryCompleted;
    internal bool SequenceResetPending;
    internal bool PersistenceFailed;
    internal int MessageSequenceNumber;
    internal int NewSequenceNumber;
    internal int NextTargetSeqNum;
    internal int DroppedQueueCount;
    internal int QueueCount;
    internal string Code = "";
    internal string Text = "";
    internal List<QueuedSessionMessage> ReleasedMessages = [];
    internal QueuedSessionMessage? PendingSequenceReset;
    internal ResendRequestActionResult ResendAction = new ResendRequestActionResult();
}
