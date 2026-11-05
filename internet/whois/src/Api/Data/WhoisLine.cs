namespace whois;

/// <summary>
/// Contains a derived line from a decoded WHOIS response.
/// </summary>
public class WhoisLine
{
    /// <summary>
    /// Contains the one-based source line number.
    /// </summary>
    public int LineNumber = 0;

    /// <summary>
    /// Contains the exact line content without its newline sequence.
    /// </summary>
    public string RawContent = string.Empty;

    /// <summary>
    /// Contains the normalized line content used for generic parsing.
    /// </summary>
    public string NormalizedContent = string.Empty;

    /// <summary>
    /// Describes the generic line classification.
    /// </summary>
    public WhoisLineKind Kind = WhoisLineKind.Blank;

    /// <summary>
    /// Contains the linked ordered field index, or <c>-1</c> when no field is linked.
    /// </summary>
    public int FieldIndex = -1;
}
