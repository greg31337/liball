namespace odf;

/// <summary>
/// Describes an embedded OpenDocument, OLE, MathML, or chart object.
/// </summary>
public class OdfEmbeddedObject
{
    /// <summary>The embedded-object kind.</summary>
    public OdfEmbeddedObjectKind Kind;
    /// <summary>The resource location kind.</summary>
    public OdfResourceSourceKind SourceKind;
    /// <summary>The preserved object reference.</summary>
    public string Href = "";
    /// <summary>The resolved package entry or subdocument-root path.</summary>
    public string PackagePath = "";
    /// <summary>The declared or detected media type.</summary>
    public string MediaType = "";
    /// <summary>The embedded OpenDocument kind.</summary>
    public OdfDocumentKind DocumentKind;
    /// <summary>The OLE class identifier.</summary>
    public string ClassId = "";
    /// <summary>The ranges for which the object requests update notification.</summary>
    public string NotifyOnUpdateOfRanges = "";
    /// <summary>The preserved XLink type.</summary>
    public string XLinkType = "";
    /// <summary>The preserved XLink show behavior.</summary>
    public string Show = "";
    /// <summary>The preserved XLink activation behavior.</summary>
    public string Actuate = "";
    /// <summary>Whether the reference is absent or syntactically valid.</summary>
    public bool IsReferenceValid;
    /// <summary>Whether the referenced package object exists.</summary>
    public bool PackageEntryExists;
    /// <summary>Whether object bytes are available without decryption.</summary>
    public bool IsContentAvailable;
    /// <summary>Whether the package object is encrypted.</summary>
    public bool IsEncrypted;
    /// <summary>Whether this object uses a directory-form subdocument.</summary>
    public bool IsDirectoryPackage;
    /// <summary>Whether this object uses a nested ZIP package.</summary>
    public bool IsArchivePackage;
    /// <summary>Whether the object structure passed bounded inspection.</summary>
    public bool IsValid;
    /// <summary>Locally available bytes for a single-entry or inline object.</summary>
    public byte[] Data = [];
    /// <summary>Package entries belonging to a directory or archive subdocument.</summary>
    public OdfPackageEntry[] PackageEntries = [];
    /// <summary>Nested OpenDocument subdocuments.</summary>
    public OdfEmbeddedObject[] Children = [];
    /// <summary>The sibling replacement image, when present.</summary>
    public OdfImage? ReplacementImage;
    /// <summary>The embedded chart description, when this is a chart object.</summary>
    public OdfChart? Chart;
    /// <summary>The inline MathML root, when present.</summary>
    public OdfNode? MathNode;
    /// <summary>The inline OLE binary-data element, when present.</summary>
    public OdfNode? BinaryDataNode;
    /// <summary>The drawing object represented by this embedded object.</summary>
    public OdfDrawingObject? Drawing;
    /// <summary>The underlying draw object element.</summary>
    public OdfNode? Node;
    internal string OriginalHref = "";
    internal string OriginalClassId = "";
    internal string OriginalNotifyOnUpdateOfRanges = "";
    internal string OriginalXLinkType = "";
    internal string OriginalShow = "";
    internal string OriginalActuate = "";
    internal byte[] OriginalData = [];
}
