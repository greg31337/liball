namespace rtp;

/// <summary>
/// Contains transport and processing configuration for an RTP session.
/// </summary>
public class RtpSessionConfiguration
{
    /// <summary>The RTP and RTCP transport arrangement.</summary>
    public RtpSessionTransportMode TransportMode;

    /// <summary>Whether malformed or nonconforming input is rejected strictly.</summary>
    public bool StrictValidation = true;

    /// <summary>Whether received RTP and RTCP padding bytes are preserved.</summary>
    public bool PreservePadding = true;

    /// <summary>The rule set used for RTCP compound validation and encoding.</summary>
    public RtcpCompoundMode RtcpCompoundMode;

    /// <summary>The largest accepted RTP packet size in bytes.</summary>
    public int MaximumRtpPacketSize = 65535;

    /// <summary>The largest accepted compound RTCP packet size in bytes.</summary>
    public int MaximumRtcpCompoundPacketSize = RtcpProtocolConstants.MaximumPacketSize;

    /// <summary>The largest accepted number of packets in one RTCP compound packet.</summary>
    public int MaximumRtcpCompoundPacketCount =
        RtcpProtocolConstants.MaximumCompoundPacketCount;

    /// <summary>The largest number of remote and contributing sources retained.</summary>
    public int MaximumSourceCount = 1024;

    /// <summary>The largest number of sent packets retained for retransmission.</summary>
    public int MaximumPacketHistoryCount = 512;

    /// <summary>The largest aggregate sent-packet history size in bytes.</summary>
    public int MaximumPacketHistoryBytes = 4194304;

    /// <summary>The maximum sent-packet history age in monotonic ticks.</summary>
    public long MaximumPacketHistoryAgeTicks;

    /// <summary>The number of sequential packets required to validate a new source.</summary>
    public int MinimumSequential = RtpProtocolConstants.DefaultMinimumSequential;

    /// <summary>The local canonical name included in RTCP source descriptions.</summary>
    public string CanonicalName = "";

    /// <summary>
    /// The optional local source-description items rotated through regular RTCP reports.
    /// </summary>
    public RtcpSourceDescriptionItem[] OptionalSourceDescriptionItems = [];

    /// <summary>The RTCP scheduling configuration.</summary>
    public RtcpSchedulerConfiguration RtcpScheduler = new RtcpSchedulerConfiguration();

    /// <summary>The RTP jitter-buffer and loss-detection configuration.</summary>
    public RtpJitterBufferConfiguration JitterBuffer = new RtpJitterBufferConfiguration();

    internal RtpSessionConfiguration Copy()
    {
        RtpSessionConfiguration copy = new RtpSessionConfiguration();
        copy.TransportMode = TransportMode;
        copy.StrictValidation = StrictValidation;
        copy.PreservePadding = PreservePadding;
        copy.RtcpCompoundMode = RtcpCompoundMode;
        copy.MaximumRtpPacketSize = MaximumRtpPacketSize;
        copy.MaximumRtcpCompoundPacketSize = MaximumRtcpCompoundPacketSize;
        copy.MaximumRtcpCompoundPacketCount = MaximumRtcpCompoundPacketCount;
        copy.MaximumSourceCount = MaximumSourceCount;
        copy.MaximumPacketHistoryCount = MaximumPacketHistoryCount;
        copy.MaximumPacketHistoryBytes = MaximumPacketHistoryBytes;
        copy.MaximumPacketHistoryAgeTicks = MaximumPacketHistoryAgeTicks;
        copy.MinimumSequential = MinimumSequential;
        copy.CanonicalName = CanonicalName;
        copy.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[OptionalSourceDescriptionItems.Length];
        int sourceDescriptionItemIndex = 0;
        while (sourceDescriptionItemIndex < OptionalSourceDescriptionItems.Length)
        {
            copy.OptionalSourceDescriptionItems[sourceDescriptionItemIndex] =
                OptionalSourceDescriptionItems[sourceDescriptionItemIndex].Copy();
            sourceDescriptionItemIndex = sourceDescriptionItemIndex + 1;
        }

        copy.RtcpScheduler = RtcpScheduler.Copy();
        copy.JitterBuffer = JitterBuffer.Copy();
        return copy;
    }
}
