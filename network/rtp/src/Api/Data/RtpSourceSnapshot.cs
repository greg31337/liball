namespace rtp;

/// <summary>
/// Contains an independent snapshot of one local or remote RTP source.
/// </summary>
public class RtpSourceSnapshot
{
    /// <summary>The synchronization source identifier.</summary>
    public uint SourceIdentifier;

    /// <summary>The current source lifecycle state.</summary>
    public RtpSourceStatus Status;

    /// <summary>Whether the source is owned by the local session.</summary>
    public bool IsLocal;

    /// <summary>Whether the source is currently an RTP sender.</summary>
    public bool IsSender;

    /// <summary>The source canonical name when known.</summary>
    public string CanonicalName = "";

    /// <summary>The most recently observed transport identity token.</summary>
    public string TransportIdentity = "";

    /// <summary>The last RTP payload type observed for the source.</summary>
    public byte PayloadType;

    /// <summary>The active RTP timestamp clock rate.</summary>
    public int ClockRate;

    /// <summary>The signaling-supplied media identity for the active payload type.</summary>
    public string MediaIdentity = "";

    /// <summary>The signaling-supplied channel count for the active payload type.</summary>
    public int ChannelCount;

    /// <summary>The monotonic time of the source's most recent RTP activity.</summary>
    public long LastRtpActivityTicks;

    /// <summary>The monotonic time of the source's most recent RTCP activity.</summary>
    public long LastRtcpActivityTicks;

    /// <summary>The latest sequence-number update when available.</summary>
    public RtpSequenceUpdate? SequenceUpdate;

    /// <summary>The latest timestamp update when available.</summary>
    public RtpTimestampUpdate? TimestampUpdate;

    /// <summary>The latest sender-report clock correlation when available.</summary>
    public RtpClockCorrelation? ClockCorrelation;

    /// <summary>The source reception statistics when it is received locally.</summary>
    public RtpReceptionStatisticsSnapshot? ReceptionStatistics;

    /// <summary>The source transmission statistics when it is sent locally.</summary>
    public RtpTransmissionStatisticsSnapshot? TransmissionStatistics;

    /// <summary>The latest reception reports received from remote participants.</summary>
    public RtpRemoteReportSnapshot[] RemoteReports = [];

    internal RtpSourceSnapshot Copy()
    {
        RtpSourceSnapshot copy = new RtpSourceSnapshot();
        copy.SourceIdentifier = SourceIdentifier;
        copy.Status = Status;
        copy.IsLocal = IsLocal;
        copy.IsSender = IsSender;
        copy.CanonicalName = CanonicalName;
        copy.TransportIdentity = TransportIdentity;
        copy.PayloadType = PayloadType;
        copy.ClockRate = ClockRate;
        copy.MediaIdentity = MediaIdentity;
        copy.ChannelCount = ChannelCount;
        copy.LastRtpActivityTicks = LastRtpActivityTicks;
        copy.LastRtcpActivityTicks = LastRtcpActivityTicks;
        if (SequenceUpdate != null)
        {
            copy.SequenceUpdate = SequenceUpdate.Copy();
        }

        if (TimestampUpdate != null)
        {
            copy.TimestampUpdate = TimestampUpdate.Copy();
        }

        if (ClockCorrelation != null)
        {
            copy.ClockCorrelation = ClockCorrelation.Copy();
        }

        if (ReceptionStatistics != null)
        {
            copy.ReceptionStatistics = ReceptionStatistics.Copy();
        }

        if (TransmissionStatistics != null)
        {
            copy.TransmissionStatistics = TransmissionStatistics.Copy();
        }

        copy.RemoteReports = new RtpRemoteReportSnapshot[RemoteReports.Length];
        int reportIndex = 0;
        while (reportIndex < RemoteReports.Length)
        {
            copy.RemoteReports[reportIndex] = RemoteReports[reportIndex].Copy();
            reportIndex = reportIndex + 1;
        }

        return copy;
    }
}
