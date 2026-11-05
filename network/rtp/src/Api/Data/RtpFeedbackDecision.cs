namespace rtp;

/// <summary>
/// Contains an RTCP feedback scheduling decision.
/// </summary>
public class RtpFeedbackDecision
{
    /// <summary>Whether feedback should be transmitted by this decision.</summary>
    public bool ShouldSend;

    /// <summary>Whether equivalent feedback suppressed the pending request.</summary>
    public bool Suppressed;

    /// <summary>Whether feedback should be included with a regular RTCP transmission.</summary>
    public bool Piggyback;

    /// <summary>The selected feedback emission mode.</summary>
    public RtcpFeedbackTimingMode TimingMode;

    /// <summary>The scheduled monotonic feedback transmission time.</summary>
    public long ScheduledTimeTicks;

    /// <summary>The feedback request associated with the decision.</summary>
    public RtpFeedbackRequest? Request;

    /// <summary>The compound RTCP packet to transmit when feedback is ready.</summary>
    public RtcpCompoundPacket? CompoundPacket;

    internal RtpFeedbackDecision Copy()
    {
        RtpFeedbackDecision copy = new RtpFeedbackDecision();
        copy.ShouldSend = ShouldSend;
        copy.Suppressed = Suppressed;
        copy.Piggyback = Piggyback;
        copy.TimingMode = TimingMode;
        copy.ScheduledTimeTicks = ScheduledTimeTicks;
        if (Request != null)
        {
            copy.Request = Request.Copy();
        }

        if (CompoundPacket != null)
        {
            copy.CompoundPacket = CompoundPacket.Copy();
        }

        return copy;
    }
}
