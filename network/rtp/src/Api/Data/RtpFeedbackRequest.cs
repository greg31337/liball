namespace rtp;

/// <summary>
/// Contains an application or stream request to schedule RTCP feedback.
/// </summary>
public class RtpFeedbackRequest
{
    /// <summary>The feedback packet requested for transmission.</summary>
    public RtcpFeedbackPacket? Feedback;

    /// <summary>The earliest acceptable monotonic transmission time.</summary>
    public long EarliestSendTimeTicks;

    /// <summary>The monotonic time after which the request is no longer useful.</summary>
    public long ExpirationTimeTicks;

    /// <summary>The preferred feedback timing mode.</summary>
    public RtcpFeedbackTimingMode PreferredTimingMode;

    /// <summary>Whether equivalent received feedback may suppress this request.</summary>
    public bool AllowSuppression = true;

    internal RtpFeedbackRequest Copy()
    {
        RtpFeedbackRequest copy = new RtpFeedbackRequest();
        if (Feedback != null)
        {
            copy.Feedback = Feedback.Copy();
        }

        copy.EarliestSendTimeTicks = EarliestSendTimeTicks;
        copy.ExpirationTimeTicks = ExpirationTimeTicks;
        copy.PreferredTimingMode = PreferredTimingMode;
        copy.AllowSuppression = AllowSuppression;
        return copy;
    }
}
