namespace odf;

/// <summary>
/// Represents a parsed OpenDocument package manifest.
/// </summary>
public class OdfManifest
{
    /// <summary>
    /// The manifest version lexical value.
    /// </summary>
    public string Version = "";
    /// <summary>
    /// File entries in source order.
    /// </summary>
    public OdfManifestEntry[] Entries = [];
    /// <summary>
    /// Standard encrypted-key subtrees in source order.
    /// </summary>
    public OdfManifestNode[] EncryptedKeys = [];
    /// <summary>
    /// Typed encrypted-key descriptions in source order.
    /// </summary>
    public OdfEncryptedKey[] EncryptedKeyModels = [];
    /// <summary>
    /// Attributes not defined for the standard manifest root.
    /// </summary>
    public OdfAttribute[] ForeignAttributes = [];
    /// <summary>
    /// Non-standard direct child elements retained from the manifest root.
    /// </summary>
    public OdfManifestNode[] ForeignElements = [];
    /// <summary>
    /// The complete namespace-aware root element.
    /// </summary>
    public OdfManifestNode Root = new OdfManifestNode();
    /// <summary>
    /// The original XML text supplied to the parser.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Diagnostics produced while interpreting known manifest values.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// Whether the document has a manifest root and valid required known values.
    /// </summary>
    public bool IsValid;
}
