namespace odf;

/// <summary>
/// Describes one file entry declared by an OpenDocument package manifest.
/// </summary>
public class OdfManifestEntry
{
    /// <summary>
    /// The declared full package path.
    /// </summary>
    public string FullPath = "";
    /// <summary>
    /// The declared entry media type.
    /// </summary>
    public string MediaType = "";
    /// <summary>
    /// The optional entry version lexical value.
    /// </summary>
    public string Version = "";
    /// <summary>
    /// The optional preferred view mode lexical value.
    /// </summary>
    public string PreferredViewMode = "";
    /// <summary>
    /// The optional expanded size when it fits a signed 64-bit integer.
    /// </summary>
    public long Size;
    /// <summary>
    /// The exact optional size lexical value.
    /// </summary>
    public string SizeLexical = "";
    /// <summary>
    /// Whether a size attribute was present.
    /// </summary>
    public bool HasSize;
    /// <summary>
    /// Whether the size lexical value is a supported non-negative integer.
    /// </summary>
    public bool SizeIsValid;
    /// <summary>
    /// The optional standard encryption-data subtree.
    /// </summary>
    public OdfManifestNode? EncryptionData;
    /// <summary>
    /// The typed encryption metadata, or <c>null</c> for an unencrypted entry.
    /// </summary>
    public OdfEncryptionInfo? Encryption;
    /// <summary>
    /// Attributes not defined for a standard manifest file entry.
    /// </summary>
    public OdfAttribute[] ForeignAttributes = [];
    /// <summary>
    /// Non-standard direct child elements retained from the file entry.
    /// </summary>
    public OdfManifestNode[] ForeignElements = [];
    /// <summary>
    /// The complete namespace-aware source element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
}
