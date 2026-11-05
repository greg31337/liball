namespace rtp;

/// <summary>
/// Identifies the rule set used to validate and encode an RTCP packet sequence.
/// </summary>
public enum RtcpCompoundMode
{
    /// <summary>The packet sequence follows the full RFC 3550 compound rules.</summary>
    Full = 0,

    /// <summary>The packet sequence is a minimal RFC 4585 feedback compound.</summary>
    MinimalFeedback = 1,

    /// <summary>The packet sequence uses negotiated RFC 5506 reduced-size rules.</summary>
    ReducedSize = 2
}
