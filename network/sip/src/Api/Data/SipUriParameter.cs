namespace sip;

/// <summary>
/// Holds one ordered URI, name-address, media-type, or disposition parameter.
/// </summary>
public class SipUriParameter
{
    /// <summary>The parameter name with its original spelling.</summary>
    public string? Name;

    /// <summary>The decoded parameter value.</summary>
    public string? Value;

    /// <summary>Indicates whether an equals sign and value were supplied.</summary>
    public bool HasValue;

    /// <summary>The original parameter text when preservation was requested.</summary>
    public string? OriginalText;

    internal string? ComparisonNameSource;
    internal string? ComparisonNameValue;
    internal string? ComparisonValueSource;
    internal string? ComparisonValueValue;
    internal bool Quoted;
}
