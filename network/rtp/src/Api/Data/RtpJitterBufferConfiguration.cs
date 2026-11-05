namespace rtp;

/// <summary>
/// Contains bounded RTP reordering, loss, and NACK configuration.
/// </summary>
public class RtpJitterBufferConfiguration
{
    /// <summary>The maximum number of packets retained by the reorder buffer.</summary>
    public int Capacity = 256;

    /// <summary>The maximum time a sequence gap remains open in monotonic ticks.</summary>
    public long MaximumLatencyTicks;

    /// <summary>The largest acceptable forward sequence jump.</summary>
    public int MaximumDropout = RtpProtocolConstants.DefaultMaximumDropout;

    /// <summary>The largest accepted sequence misordering distance.</summary>
    public int MaximumMisorder = RtpProtocolConstants.DefaultMaximumMisorder;

    /// <summary>Whether packets arriving after loss finalization can still be released.</summary>
    public bool ReleaseLatePackets;

    /// <summary>Whether a confirmed sequence discontinuity resets buffered state.</summary>
    public bool ResetOnDiscontinuity = true;

    /// <summary>The delay before the first NACK request in monotonic ticks.</summary>
    public long NackDelayTicks;

    /// <summary>The interval between NACK retries in monotonic ticks.</summary>
    public long NackRetryIntervalTicks;

    /// <summary>The maximum number of NACK transmissions for one missing packet.</summary>
    public int MaximumNackRetryCount;

    /// <summary>The maximum duration of NACK retry processing in monotonic ticks.</summary>
    public long MaximumNackRetryPeriodTicks;

    internal RtpJitterBufferConfiguration Copy()
    {
        RtpJitterBufferConfiguration copy = new RtpJitterBufferConfiguration();
        copy.Capacity = Capacity;
        copy.MaximumLatencyTicks = MaximumLatencyTicks;
        copy.MaximumDropout = MaximumDropout;
        copy.MaximumMisorder = MaximumMisorder;
        copy.ReleaseLatePackets = ReleaseLatePackets;
        copy.ResetOnDiscontinuity = ResetOnDiscontinuity;
        copy.NackDelayTicks = NackDelayTicks;
        copy.NackRetryIntervalTicks = NackRetryIntervalTicks;
        copy.MaximumNackRetryCount = MaximumNackRetryCount;
        copy.MaximumNackRetryPeriodTicks = MaximumNackRetryPeriodTicks;
        return copy;
    }
}
