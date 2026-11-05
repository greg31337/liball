namespace odf;

/// <summary>
/// Describes audio, video, plugin, applet, or floating-frame content without
/// loading or executing it.
/// </summary>
public class OdfMediaResource
{
    /// <summary>The media-resource kind.</summary>
    public OdfMediaResourceKind Kind;
    /// <summary>The resource location kind.</summary>
    public OdfResourceSourceKind SourceKind;
    /// <summary>The preserved resource reference.</summary>
    public string Href = "";
    /// <summary>The resolved canonical package path.</summary>
    public string PackagePath = "";
    /// <summary>The declared media type.</summary>
    public string MediaType = "";
    /// <summary>The preserved XLink type.</summary>
    public string XLinkType = "";
    /// <summary>The preserved XLink show behavior.</summary>
    public string Show = "";
    /// <summary>The preserved XLink activation behavior.</summary>
    public string Actuate = "";
    /// <summary>The applet code name.</summary>
    public string Code = "";
    /// <summary>The serialized applet object name.</summary>
    public string ObjectName = "";
    /// <summary>The applet archive list.</summary>
    public string Archive = "";
    /// <summary>The preserved applet scripting permission.</summary>
    public string MayScriptValue = "";
    /// <summary>Whether the applet declares that scripting may be used.</summary>
    public bool MayScript;
    /// <summary>Whether the applet scripting permission is absent or valid.</summary>
    public bool IsMayScriptValid;
    /// <summary>The floating-frame name.</summary>
    public string FrameName = "";
    /// <summary>Whether the resource reference is absent or syntactically valid.</summary>
    public bool IsReferenceValid;
    /// <summary>Whether the media type is absent or syntactically valid.</summary>
    public bool IsMediaTypeValid;
    /// <summary>Whether a referenced package entry exists.</summary>
    public bool PackageEntryExists;
    /// <summary>Whether referenced package bytes are available without decryption.</summary>
    public bool IsContentAvailable;
    /// <summary>Whether the referenced package entry is encrypted.</summary>
    public bool IsEncrypted;
    /// <summary>Whether the description can represent executable active content.</summary>
    public bool IsActiveContent;
    /// <summary>The plugin or applet parameters, preserved without interpretation.</summary>
    public OdfMediaParameter[] Parameters = [];
    /// <summary>The drawing object represented by this resource.</summary>
    public OdfDrawingObject? Drawing;
    /// <summary>The underlying media, applet, or floating-frame element.</summary>
    public OdfNode? Node;
    internal string OriginalHref = "";
    internal string OriginalMediaType = "";
    internal string OriginalXLinkType = "";
    internal string OriginalShow = "";
    internal string OriginalActuate = "";
    internal string OriginalCode = "";
    internal string OriginalObjectName = "";
    internal string OriginalArchive = "";
    internal string OriginalMayScriptValue = "";
    internal string OriginalFrameName = "";
    internal OdfNode[] OriginalParameterNodes = [];
}
