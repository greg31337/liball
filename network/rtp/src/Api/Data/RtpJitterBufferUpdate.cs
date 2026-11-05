namespace rtp;

/// <summary>
/// Contains the result of a bounded RTP jitter-buffer operation.
/// </summary>
public class RtpJitterBufferUpdate
{
    /// <summary>The primary jitter-buffer operation outcome.</summary>
    public RtpJitterBufferStatus Status;

    /// <summary>The packets released in extended-sequence order.</summary>
    public RtpPacket[] ReleasedPackets = [];

    /// <summary>The repaired packets accepted during the operation.</summary>
    public RtpPacket[] RecoveredPackets = [];

    /// <summary>The sequence numbers finalized as lost.</summary>
    public ushort[] LostSequenceNumbers = [];

    /// <summary>The repair requests generated for current sequence gaps.</summary>
    public RtpRepairRequest[] RepairRequests = [];

    /// <summary>The RTCP feedback requests generated for missing packets.</summary>
    public RtpFeedbackRequest[] FeedbackRequests = [];

    /// <summary>The number of packets retained after the operation.</summary>
    public int BufferedPacketCount;

    /// <summary>The number of payload octets retained after the operation.</summary>
    public int BufferedOctetCount;

    /// <summary>The cumulative number of packets released since reset.</summary>
    public long ReleasedPacketCount;

    /// <summary>The cumulative number of duplicate packets observed since reset.</summary>
    public long DuplicatePacketCount;

    /// <summary>The cumulative number of late packets observed since reset.</summary>
    public long LatePacketCount;

    /// <summary>The cumulative number of packets finalized as lost since reset.</summary>
    public long LostPacketCount;

    /// <summary>The cumulative number of capacity overflows since reset.</summary>
    public long OverflowPacketCount;

    /// <summary>The cumulative number of repaired packets recovered since reset.</summary>
    public long RecoveredPacketCount;

    internal RtpJitterBufferUpdate Copy()
    {
        RtpJitterBufferUpdate copy = new RtpJitterBufferUpdate();
        copy.Status = Status;
        copy.ReleasedPackets = new RtpPacket[ReleasedPackets.Length];
        int packetIndex = 0;
        while (packetIndex < ReleasedPackets.Length)
        {
            copy.ReleasedPackets[packetIndex] = ReleasedPackets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        copy.RecoveredPackets = new RtpPacket[RecoveredPackets.Length];
        int recoveredIndex = 0;
        while (recoveredIndex < RecoveredPackets.Length)
        {
            copy.RecoveredPackets[recoveredIndex] =
                RecoveredPackets[recoveredIndex].Copy();
            recoveredIndex = recoveredIndex + 1;
        }

        copy.LostSequenceNumbers = (ushort[])LostSequenceNumbers.Clone();
        copy.RepairRequests = new RtpRepairRequest[RepairRequests.Length];
        int requestIndex = 0;
        while (requestIndex < RepairRequests.Length)
        {
            copy.RepairRequests[requestIndex] = RepairRequests[requestIndex].Copy();
            requestIndex = requestIndex + 1;
        }

        copy.FeedbackRequests =
            new RtpFeedbackRequest[FeedbackRequests.Length];
        int feedbackIndex = 0;
        while (feedbackIndex < FeedbackRequests.Length)
        {
            copy.FeedbackRequests[feedbackIndex] =
                FeedbackRequests[feedbackIndex].Copy();
            feedbackIndex = feedbackIndex + 1;
        }

        copy.BufferedPacketCount = BufferedPacketCount;
        copy.BufferedOctetCount = BufferedOctetCount;
        copy.ReleasedPacketCount = ReleasedPacketCount;
        copy.DuplicatePacketCount = DuplicatePacketCount;
        copy.LatePacketCount = LatePacketCount;
        copy.LostPacketCount = LostPacketCount;
        copy.OverflowPacketCount = OverflowPacketCount;
        copy.RecoveredPacketCount = RecoveredPacketCount;
        return copy;
    }
}
