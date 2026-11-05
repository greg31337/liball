namespace whois;

/// <summary>
/// Contains one ordered generic field derived from a WHOIS response line.
/// </summary>
public class WhoisField
{
    /// <summary>
    /// Contains the zero-based field index in the parsed response.
    /// </summary>
    public int Index = -1;

    /// <summary>
    /// Contains the zero-based containing record index, or <c>-1</c> when ungrouped.
    /// </summary>
    public int RecordIndex = -1;

    /// <summary>
    /// Contains the one-based source line number.
    /// </summary>
    public int SourceLine = 0;

    /// <summary>
    /// Contains the field name with original casing preserved.
    /// </summary>
    public string OriginalName = string.Empty;

    /// <summary>
    /// Contains the case-normalized field name used for lookup.
    /// </summary>
    public string CanonicalName = string.Empty;

    /// <summary>
    /// Contains the field value as parsed from its source line.
    /// </summary>
    public string RawValue = string.Empty;

    /// <summary>
    /// Contains the continuation-aware derived field value.
    /// </summary>
    public string Value = string.Empty;
}
