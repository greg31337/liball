namespace whois;

/// <summary>
/// Contains an ordered generic group of field-bearing WHOIS response lines.
/// </summary>
public class WhoisRecord
{
    /// <summary>
    /// Contains the zero-based record index in the parsed response.
    /// </summary>
    public int Index = -1;

    /// <summary>
    /// Contains the record fields in source order.
    /// </summary>
    public WhoisField[] Fields = System.Array.Empty<WhoisField>();

    /// <summary>
    /// Contains the record lines in source order.
    /// </summary>
    public WhoisLine[] Lines = System.Array.Empty<WhoisLine>();
}
