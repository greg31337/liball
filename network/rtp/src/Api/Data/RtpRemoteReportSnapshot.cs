namespace rtp;

/// <summary>
/// Contains an independent snapshot of a remote RTCP reception report.
/// </summary>
public class RtpRemoteReportSnapshot
{
    /// <summary>The synchronization source that sent the report.</summary>
    public uint ReporterSourceIdentifier;

    /// <summary>The local synchronization source described by the report.</summary>
    public uint ReportedSourceIdentifier;

    /// <summary>The eight-bit fraction lost reported by the remote source.</summary>
    public byte FractionLost;

    /// <summary>The signed cumulative packet loss reported by the remote source.</summary>
    public int CumulativePacketsLost;

    /// <summary>The extended highest sequence number reported by the remote source.</summary>
    public uint ExtendedHighestSequenceNumber;

    /// <summary>The interarrival jitter reported by the remote source.</summary>
    public uint InterarrivalJitter;

    /// <summary>The middle 32 bits of the sender-report timestamp echoed by the report.</summary>
    public uint LastSenderReport;

    /// <summary>The remote delay since that sender report in compact-NTP units.</summary>
    public uint DelaySinceLastSenderReport;

    /// <summary>Whether a valid round-trip-time sample was calculated.</summary>
    public bool HasRoundTripTime;

    /// <summary>The calculated round-trip time in caller-defined monotonic ticks.</summary>
    public long RoundTripTimeTicks;

    /// <summary>The monotonic arrival time of the remote report.</summary>
    public long ArrivalTimeTicks;

    internal RtpRemoteReportSnapshot Copy()
    {
        RtpRemoteReportSnapshot copy = new RtpRemoteReportSnapshot();
        copy.ReporterSourceIdentifier = ReporterSourceIdentifier;
        copy.ReportedSourceIdentifier = ReportedSourceIdentifier;
        copy.FractionLost = FractionLost;
        copy.CumulativePacketsLost = CumulativePacketsLost;
        copy.ExtendedHighestSequenceNumber = ExtendedHighestSequenceNumber;
        copy.InterarrivalJitter = InterarrivalJitter;
        copy.LastSenderReport = LastSenderReport;
        copy.DelaySinceLastSenderReport = DelaySinceLastSenderReport;
        copy.HasRoundTripTime = HasRoundTripTime;
        copy.RoundTripTimeTicks = RoundTripTimeTicks;
        copy.ArrivalTimeTicks = ArrivalTimeTicks;
        return copy;
    }
}
