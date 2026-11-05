namespace uri;

/// <summary>
/// Selects strict RFC grammar or the advisory RFC 8288 recovery algorithm.
/// </summary>
public enum WebLinkParseMode
{
    /// <summary>Reject the complete field input at the first syntax failure.</summary>
    Strict = 0,

    /// <summary>Apply the RFC 8288 Appendix B recovery algorithm.</summary>
    Recovery = 1
}
