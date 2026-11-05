namespace odf;

/// <summary>
/// Controls document serialization and output ownership.
/// </summary>
public class OdfSaveOptions
{
    /// <summary>
    /// Explicit synchronous cancellation state.
    /// </summary>
    public OdfCancellationState Cancellation = new OdfCancellationState();
    /// <summary>
    /// Whether a caller-supplied output stream remains open after the operation.
    /// </summary>
    public bool LeaveOutputOpen = true;
    /// <summary>
    /// Whether unknown and foreign content is retained.
    /// </summary>
    public bool PreserveUnknownContent = true;
    /// <summary>
    /// Whether preserved lexical values are preferred over canonical formatting.
    /// </summary>
    public bool PreserveLexicalValues = true;
    /// <summary>
    /// Whether original package timestamps are retained when possible.
    /// </summary>
    public bool PreserveTimestamps = true;
    /// <summary>
    /// Whether output ordering, names, timestamps, and serialization are deterministic.
    /// </summary>
    public bool Deterministic;
    /// <summary>
    /// Whether serialized XML uses readable indentation.
    /// </summary>
    public bool IndentXml;
    /// <summary>
    /// Whether manifest file entries are ordered by package path when serialized.
    /// </summary>
    public bool SortManifestEntries;
    /// <summary>
    /// The compatibility profile used for output, or an empty string for the default preservation behavior.
    /// </summary>
    public string CompatibilityProfile = "";
    /// <summary>
    /// The preferred package-entry compression method.
    /// </summary>
    public OdfCompressionMethod CompressionMethod = OdfCompressionMethod.Deflated;
    /// <summary>
    /// The target ODF version, or <see cref="OdfVersion.Unknown"/> to preserve the document version.
    /// </summary>
    public OdfVersion TargetVersion;
    /// <summary>
    /// The target conformance class, or <see cref="OdfConformanceClass.Unknown"/> to preserve it.
    /// </summary>
    public OdfConformanceClass TargetConformanceClass;
}
