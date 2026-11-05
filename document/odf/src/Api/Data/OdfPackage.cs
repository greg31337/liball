namespace odf;

/// <summary>
/// Represents a loaded editable OpenDocument package.
/// </summary>
public class OdfPackage
{
    /// <summary>
    /// Package entries in their current output order.
    /// </summary>
    public OdfPackageEntry[] Entries = [];
    /// <summary>
    /// The package manifest, or <c>null</c> when no manifest was loaded.
    /// </summary>
    public OdfManifest? Manifest;
    /// <summary>
    /// Diagnostics collected while reading the package.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// The detected or inferred package media type.
    /// </summary>
    public string MediaType = "";
    /// <summary>
    /// The media type declared by the root manifest entry.
    /// </summary>
    public string ManifestMediaType = "";
    /// <summary>
    /// The detected OpenDocument kind.
    /// </summary>
    public OdfDocumentKind DocumentKind;
    /// <summary>
    /// Whether the source package uses ZIP64 structures.
    /// </summary>
    public bool UsesZip64;
    /// <summary>
    /// Whether package inspection completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    internal ZipEntryIndex? EntryIndex;
}
