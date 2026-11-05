namespace rtp;

/// <summary>
/// Contains RTCP interval and feedback scheduling configuration.
/// </summary>
public class RtcpSchedulerConfiguration
{
    /// <summary>The total session bandwidth in bits per second.</summary>
    public double SessionBandwidthBitsPerSecond;

    /// <summary>The fraction of session bandwidth allocated to RTCP.</summary>
    public double RtcpBandwidthFraction = RtcpProtocolConstants.RecommendedBandwidthFraction;

    /// <summary>The RTCP bandwidth fraction reserved for active senders.</summary>
    public double SenderBandwidthFraction = RtcpProtocolConstants.SenderBandwidthFraction;

    /// <summary>The RTCP bandwidth fraction reserved for receivers.</summary>
    public double ReceiverBandwidthFraction = RtcpProtocolConstants.ReceiverBandwidthFraction;

    /// <summary>The minimum regular RTCP interval in seconds.</summary>
    public double MinimumIntervalSeconds = RtcpProtocolConstants.MinimumIntervalSeconds;

    /// <summary>The minimum initial RTCP interval in seconds.</summary>
    public double InitialMinimumIntervalSeconds =
        RtcpProtocolConstants.InitialMinimumIntervalSeconds;

    /// <summary>The initial average compound RTCP packet size in bytes.</summary>
    public int InitialAveragePacketSize = 128;

    /// <summary>The minimum interval between AVPF feedback transmissions in ticks.</summary>
    public long MinimumFeedbackIntervalTicks;

    /// <summary>The interval for retaining received feedback for suppression.</summary>
    public long FeedbackRetentionTicks =
        RtcpProtocolConstants.MinimumFeedbackRetentionTicks;

    /// <summary>The inactivity interval after which sender status expires.</summary>
    public long SenderTimeoutTicks;

    /// <summary>The inactivity interval after which session membership expires.</summary>
    public long MemberTimeoutTicks;

    /// <summary>Whether AVPF early and immediate feedback scheduling is enabled.</summary>
    public bool FeedbackEnabled;

    /// <summary>Whether negotiated reduced-size RTCP output is enabled.</summary>
    public bool ReducedSizeEnabled;

    internal RtcpSchedulerConfiguration Copy()
    {
        RtcpSchedulerConfiguration copy = new RtcpSchedulerConfiguration();
        copy.SessionBandwidthBitsPerSecond = SessionBandwidthBitsPerSecond;
        copy.RtcpBandwidthFraction = RtcpBandwidthFraction;
        copy.SenderBandwidthFraction = SenderBandwidthFraction;
        copy.ReceiverBandwidthFraction = ReceiverBandwidthFraction;
        copy.MinimumIntervalSeconds = MinimumIntervalSeconds;
        copy.InitialMinimumIntervalSeconds = InitialMinimumIntervalSeconds;
        copy.InitialAveragePacketSize = InitialAveragePacketSize;
        copy.MinimumFeedbackIntervalTicks = MinimumFeedbackIntervalTicks;
        copy.FeedbackRetentionTicks = FeedbackRetentionTicks;
        copy.SenderTimeoutTicks = SenderTimeoutTicks;
        copy.MemberTimeoutTicks = MemberTimeoutTicks;
        copy.FeedbackEnabled = FeedbackEnabled;
        copy.ReducedSizeEnabled = ReducedSizeEnabled;
        return copy;
    }
}
