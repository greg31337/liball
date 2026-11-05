namespace rtp;

/// <summary>
/// Describes the result of a jitter-buffer operation.
/// </summary>
public enum RtpJitterBufferStatus
{
    /// <summary>The packet was retained while awaiting release.</summary>
    Buffered = 0,

    /// <summary>One or more packets were released.</summary>
    Released = 1,

    /// <summary>The packet duplicated one already observed.</summary>
    Duplicate = 2,

    /// <summary>The packet arrived after its release deadline.</summary>
    Late = 3,

    /// <summary>A sequence gap was finalized as loss.</summary>
    Lost = 4,

    /// <summary>The configured buffer capacity was exceeded.</summary>
    Overflow = 5,

    /// <summary>The buffer reset after a discontinuity or explicit request.</summary>
    Reset = 6
}
