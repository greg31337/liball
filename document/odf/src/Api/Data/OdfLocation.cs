namespace odf;

/// <summary>
/// Identifies a package, XML, or text location when available.
/// </summary>
public class OdfLocation
{
    /// <summary>
    /// The package part path, or an empty string when not applicable.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The node path within the XML part, or an empty string when unavailable.
    /// </summary>
    public string NodePath = "";
    /// <summary>
    /// The XML ID associated with the location, or an empty string.
    /// </summary>
    public string XmlId = "";
    /// <summary>
    /// The one-based line number, or -1 when unavailable.
    /// </summary>
    public int Line = -1;
    /// <summary>
    /// The one-based column number, or -1 when unavailable.
    /// </summary>
    public int Column = -1;
    /// <summary>
    /// The byte offset in the package part, or -1 when unavailable.
    /// </summary>
    public long ByteOffset = -1;
    /// <summary>
    /// The character offset in a text or formula value, or -1 when unavailable.
    /// </summary>
    public int TextOffset = -1;
    /// <summary>
    /// The number of characters covered by the location.
    /// </summary>
    public int TextLength;
}
