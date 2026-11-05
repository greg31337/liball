namespace odf;

/// <summary>
/// Describes a parsed <c>draw:points</c> value.
/// </summary>
public class OdfSvgPointList
{
    /// <summary>The preserved lexical point list.</summary>
    public string LexicalValue = "";
    /// <summary>The points in document order.</summary>
    public OdfSvgPoint[] Points = [];
    /// <summary>Whether a point list was specified.</summary>
    public bool IsSpecified;
    /// <summary>Whether the specified point list is valid.</summary>
    public bool IsValid;
    internal string OriginalLexicalValue = "";
    internal string OriginalCanonicalValue = "";
}
