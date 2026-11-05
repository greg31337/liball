namespace rtp;

/// <summary>
/// Contains one RFC 3550 reception report block.
/// </summary>
public class RtcpReceptionReport
{
    /// <summary>The SSRC of the RTP source being reported.</summary>
    public uint SourceIdentifier;

    /// <summary>The fraction lost since the previous report.</summary>
    public byte FractionLost;

    /// <summary>The signed cumulative number of packets lost.</summary>
    public int CumulativePacketsLost;

    /// <summary>The extended highest sequence number received.</summary>
    public uint ExtendedHighestSequenceNumber;

    /// <summary>The estimated interarrival jitter.</summary>
    public uint InterarrivalJitter;

    /// <summary>The middle 32 bits of the last sender report NTP timestamp.</summary>
    public uint LastSenderReport;

    /// <summary>The delay since the last sender report in compact-NTP units.</summary>
    public uint DelaySinceLastSenderReport;

    internal RtcpReceptionReport Copy()
    {
        RtcpReceptionReport copy = new RtcpReceptionReport();
        copy.SourceIdentifier = SourceIdentifier;
        copy.FractionLost = FractionLost;
        copy.CumulativePacketsLost = CumulativePacketsLost;
        copy.ExtendedHighestSequenceNumber = ExtendedHighestSequenceNumber;
        copy.InterarrivalJitter = InterarrivalJitter;
        copy.LastSenderReport = LastSenderReport;
        copy.DelaySinceLastSenderReport = DelaySinceLastSenderReport;
        return copy;
    }
}
