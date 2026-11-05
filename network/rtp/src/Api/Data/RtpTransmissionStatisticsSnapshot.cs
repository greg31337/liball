namespace rtp;

/// <summary>
/// Contains an independent snapshot of RTP transmission statistics for one source.
/// </summary>
public class RtpTransmissionStatisticsSnapshot
{
    /// <summary>The synchronization source described by the snapshot.</summary>
    public uint SourceIdentifier;

    /// <summary>The number of RTP packets sent.</summary>
    public long SentPacketCount;

    /// <summary>The number of media payload octets sent.</summary>
    public long SentOctetCount;

    /// <summary>The RTP timestamp of the most recently sent packet.</summary>
    public uint LastRtpTimestamp;

    /// <summary>The monotonic send time of the most recently sent packet.</summary>
    public long LastSendTimeTicks;

    /// <summary>Whether the source has sent RTP during the current reporting interval.</summary>
    public bool IsActiveSender;

    /// <summary>Whether the source has enough timing data to produce sender information.</summary>
    public bool SenderReportEligible;

    /// <summary>Whether sustained transmission conditions indicate a circuit-breaker timeout.</summary>
    public bool TimedOut;

    internal RtpTransmissionStatisticsSnapshot Copy()
    {
        RtpTransmissionStatisticsSnapshot copy = new RtpTransmissionStatisticsSnapshot();
        copy.SourceIdentifier = SourceIdentifier;
        copy.SentPacketCount = SentPacketCount;
        copy.SentOctetCount = SentOctetCount;
        copy.LastRtpTimestamp = LastRtpTimestamp;
        copy.LastSendTimeTicks = LastSendTimeTicks;
        copy.IsActiveSender = IsActiveSender;
        copy.SenderReportEligible = SenderReportEligible;
        copy.TimedOut = TimedOut;
        return copy;
    }
}
