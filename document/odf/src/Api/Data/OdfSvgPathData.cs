namespace odf;

/// <summary>
/// Describes parsed SVG path data while retaining its source spelling.
/// </summary>
public class OdfSvgPathData
{
    /// <summary>The preserved lexical path data.</summary>
    public string LexicalValue = "";
    /// <summary>The parsed path command segments.</summary>
    public OdfSvgPathCommand[] Commands = [];
    /// <summary>Whether path data was specified.</summary>
    public bool IsSpecified;
    /// <summary>Whether the specified path data is valid.</summary>
    public bool IsValid;
    internal string OriginalLexicalValue = "";
    internal string OriginalCanonicalValue = "";
}
