namespace sip;

/// <summary>
/// Holds one ordered query header embedded in a SIP URI.
/// </summary>
public class SipUriHeader
{
    /// <summary>The decoded header name.</summary>
    public string? Name;

    /// <summary>The decoded header value.</summary>
    public string? Value;

    /// <summary>The original query component when preservation was requested.</summary>
    public string? OriginalText;

    internal string? ComparisonNameSource;
    internal string? ComparisonNameValue;
    internal string? ComparisonValueSource;
    internal string? ComparisonValueValue;
}
