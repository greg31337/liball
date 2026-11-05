namespace asn1;

/// <summary>
/// Identifies an exact position within a named source unit.
/// </summary>
/// <remarks>
/// Offset is zero-based and counts UTF-16 code units. Line and column are one-based. Column counts Unicode scalar values. A CRLF pair is one line break; lone CR and LF characters are also line breaks.
/// </remarks>
public class AsnSourcePosition
{
    /// <summary>
    /// Contains the source-unit name.
    /// </summary>
    public string SourceName = "";

    /// <summary>
    /// Contains the zero-based UTF-16 code-unit offset.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Contains the one-based line number.
    /// </summary>
    public int Line = 1;

    /// <summary>
    /// Contains the one-based Unicode-scalar column number.
    /// </summary>
    public int Column = 1;
}
