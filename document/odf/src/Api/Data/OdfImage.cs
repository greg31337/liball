namespace odf;

/// <summary>
/// Describes a package, external, or inline-base64 drawing image.
/// </summary>
public class OdfImage
{
    /// <summary>The image source kind.</summary>
    public OdfImageSourceKind SourceKind;
    /// <summary>The preserved image reference.</summary>
    public string Href = "";
    /// <summary>The resolved canonical package path.</summary>
    public string PackagePath = "";
    /// <summary>The declared image media type.</summary>
    public string DeclaredMediaType = "";
    /// <summary>The safely detected image media type.</summary>
    public string DetectedMediaType = "";
    /// <summary>The safely detected image format.</summary>
    public OdfImageFormat DetectedFormat;
    /// <summary>The image bytes when locally available.</summary>
    public byte[] Data = [];
    /// <summary>The preserved inline base64 text.</summary>
    public string Base64Value = "";
    /// <summary>The preserved XLink type.</summary>
    public string XLinkType = "";
    /// <summary>The preserved XLink show behavior.</summary>
    public string Show = "";
    /// <summary>The preserved XLink activation behavior.</summary>
    public string Actuate = "";
    /// <summary>Whether the local source and its encoding are valid.</summary>
    public bool IsValid;
    /// <summary>Whether declared and detected media types disagree.</summary>
    public bool HasMediaTypeMismatch;
    /// <summary>The drawing object represented by this image.</summary>
    public OdfDrawingObject? Drawing;
    /// <summary>The underlying image element.</summary>
    public OdfNode? Node;
    /// <summary>The inline binary-data element, when present.</summary>
    public OdfNode? BinaryDataNode;
    internal string OriginalHref = "";
    internal string OriginalDeclaredMediaType = "";
    internal string OriginalBase64Value = "";
    internal string OriginalXLinkType = "";
    internal string OriginalShow = "";
    internal string OriginalActuate = "";
    internal byte[] OriginalData = [];
}
