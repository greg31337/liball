namespace odf;

/// <summary>
/// Describes an SVG view box in an ODF local coordinate system.
/// </summary>
public class OdfSvgViewBox
{
    /// <summary>The preserved lexical view-box value.</summary>
    public string LexicalValue = "";
    /// <summary>The left coordinate.</summary>
    public long Left;
    /// <summary>The top coordinate.</summary>
    public long Top;
    /// <summary>The view-box width.</summary>
    public long Width;
    /// <summary>The view-box height.</summary>
    public long Height;
    /// <summary>Whether a view box was specified.</summary>
    public bool IsSpecified;
    /// <summary>Whether the specified view box is valid.</summary>
    public bool IsValid;
    internal string OriginalLexicalValue = "";
    internal string OriginalCanonicalValue = "";
}
