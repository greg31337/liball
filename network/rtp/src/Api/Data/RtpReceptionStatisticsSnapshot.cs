namespace rtp;

/// <summary>
/// Contains an independent snapshot of RTP reception statistics for one source.
/// </summary>
public class RtpReceptionStatisticsSnapshot
{
    /// <summary>The synchronization source described by the snapshot.</summary>
    public uint SourceIdentifier;

    /// <summary>The first validated extended RTP sequence number.</summary>
    public uint BaseExtendedSequenceNumber;

    /// <summary>The greatest validated extended RTP sequence number observed.</summary>
    public uint HighestExtendedSequenceNumber;

    /// <summary>The number of packets expected in the current source interval.</summary>
    public long ExpectedPacketCount;

    /// <summary>The number of valid RTP packets received.</summary>
    public long ReceivedPacketCount;

    /// <summary>The number of media payload octets received.</summary>
    public long ReceivedOctetCount;

    /// <summary>The number of duplicate packets observed.</summary>
    public long DuplicatePacketCount;

    /// <summary>The number of reordered packets observed.</summary>
    public long ReorderedPacketCount;

    /// <summary>The number of packets discarded before acceptance.</summary>
    public long DiscardedPacketCount;

    /// <summary>The number of packets arriving after their release deadline.</summary>
    public long LatePacketCount;

    /// <summary>The number of packets presented by repair mechanisms.</summary>
    public long RepairedPacketCount;

    /// <summary>The number of missing packets successfully recovered.</summary>
    public long RecoveredPacketCount;

    /// <summary>The signed cumulative packet loss reported by RTCP.</summary>
    public int CumulativePacketsLost;

    /// <summary>The eight-bit fraction lost during the latest reporting interval.</summary>
    public byte FractionLost;

    /// <summary>The RFC 3550 interarrival jitter estimate in RTP timestamp units.</summary>
    public uint InterarrivalJitter;

    /// <summary>The middle 32 bits of the most recently received sender-report timestamp.</summary>
    public uint LastSenderReport;

    /// <summary>The delay since the most recent sender report in compact-NTP units.</summary>
    public uint DelaySinceLastSenderReport;

    /// <summary>Whether sustained reception conditions indicate a circuit-breaker timeout.</summary>
    public bool TimedOut;

    internal RtpReceptionStatisticsSnapshot Copy()
    {
        RtpReceptionStatisticsSnapshot copy = new RtpReceptionStatisticsSnapshot();
        copy.SourceIdentifier = SourceIdentifier;
        copy.BaseExtendedSequenceNumber = BaseExtendedSequenceNumber;
        copy.HighestExtendedSequenceNumber = HighestExtendedSequenceNumber;
        copy.ExpectedPacketCount = ExpectedPacketCount;
        copy.ReceivedPacketCount = ReceivedPacketCount;
        copy.ReceivedOctetCount = ReceivedOctetCount;
        copy.DuplicatePacketCount = DuplicatePacketCount;
        copy.ReorderedPacketCount = ReorderedPacketCount;
        copy.DiscardedPacketCount = DiscardedPacketCount;
        copy.LatePacketCount = LatePacketCount;
        copy.RepairedPacketCount = RepairedPacketCount;
        copy.RecoveredPacketCount = RecoveredPacketCount;
        copy.CumulativePacketsLost = CumulativePacketsLost;
        copy.FractionLost = FractionLost;
        copy.InterarrivalJitter = InterarrivalJitter;
        copy.LastSenderReport = LastSenderReport;
        copy.DelaySinceLastSenderReport = DelaySinceLastSenderReport;
        copy.TimedOut = TimedOut;
        return copy;
    }
}
