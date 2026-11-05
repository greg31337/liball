namespace odf;

/// <summary>
/// Describes the default behavior of hyperlinks in a document.
/// </summary>
public class OdfMetadataHyperlinkBehavior
{
    /// <summary>Whether hyperlink-behavior metadata is present.</summary>
    public bool IsPresent;
    /// <summary>The default target frame name.</summary>
    public string TargetFrameName = "";
    /// <summary>The XLink show token.</summary>
    public string Show = "";
    internal OdfNode? Node;
}
