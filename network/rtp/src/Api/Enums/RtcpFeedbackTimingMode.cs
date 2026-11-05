namespace rtp;

/// <summary>
/// Identifies when and how RTCP feedback is emitted.
/// </summary>
public enum RtcpFeedbackTimingMode
{
    /// <summary>Feedback is sent with the next regular RTCP report.</summary>
    Regular = 0,

    /// <summary>Feedback is sent in an early RTCP packet.</summary>
    Early = 1,

    /// <summary>Feedback is eligible for immediate transmission.</summary>
    Immediate = 2,

    /// <summary>Feedback is sent as negotiated reduced-size RTCP.</summary>
    ReducedSize = 3
}
