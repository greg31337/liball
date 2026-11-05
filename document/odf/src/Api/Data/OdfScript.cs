namespace odf;

/// <summary>
/// Describes script content for inspection and preservation without execution.
/// </summary>
public class OdfScript
{
    /// <summary>How the script is stored.</summary>
    public OdfScriptKind Kind;
    /// <summary>The script language declared by an embedded script.</summary>
    public string Language = "";
    /// <summary>The direct character data of an embedded script.</summary>
    public string Text = "";
    /// <summary>The package path for a package script entry.</summary>
    public string PackagePath = "";
    /// <summary>The declared or inferred package-entry media type.</summary>
    public string MediaType = "";
    /// <summary>The package-entry bytes when available without decryption.</summary>
    public byte[] Data = [];
    /// <summary>Whether package-entry bytes are available.</summary>
    public bool IsContentAvailable;
    /// <summary>Whether the package entry is encrypted.</summary>
    public bool IsEncrypted;
    /// <summary>All qualified embedded-script attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>All embedded-script child nodes, including extension content.</summary>
    public OdfNode[] ChildNodes = [];
    /// <summary>The XML part containing an embedded script.</summary>
    public string PartPath = "";
    /// <summary>The active-content security classification.</summary>
    public OdfActiveContentClassification Classification;
    /// <summary>Whether this description represents active content.</summary>
    public bool IsActiveContent;
    /// <summary>The underlying office:script element.</summary>
    public OdfNode? Node;
    internal string OriginalLanguage = "";
    internal string OriginalText = "";
    internal string OriginalPackagePath = "";
    internal string OriginalMediaType = "";
    internal byte[] OriginalData = [];
}
