namespace cbor;

/// <summary>Describes an exact half-open range in CDDL source text.</summary>
public class CddlSourceSpan
{
    /// <summary>The optional logical source name.</summary>
    public string? SourceName;
    /// <summary>The zero-based UTF-16 index where the range begins.</summary>
    public int Start;
    /// <summary>The range length in UTF-16 code units.</summary>
    public int Length;
    /// <summary>The one-based line where the range begins.</summary>
    public int StartLine = 1;
    /// <summary>The one-based column where the range begins.</summary>
    public int StartColumn = 1;
    /// <summary>The one-based line at the exclusive end of the range.</summary>
    public int EndLine = 1;
    /// <summary>The one-based column at the exclusive end of the range.</summary>
    public int EndColumn = 1;
}
