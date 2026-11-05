namespace odf;

/// <summary>
/// Describes an OpenDocument version and conformance class.
/// </summary>
public class OdfVersionInfo
{
    /// <summary>
    /// The original version lexical value, including unsupported values retained for preservation.
    /// </summary>
    public string LexicalValue = "";
    /// <summary>
    /// The recognized document version.
    /// </summary>
    public OdfVersion Version;
    /// <summary>
    /// The parsed major version number.
    /// </summary>
    public int Major;
    /// <summary>
    /// The parsed minor version number.
    /// </summary>
    public int Minor;
    /// <summary>
    /// Whether the version lexical value is syntactically valid.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Whether the parsed version is newer than the latest version known by this SDK.
    /// </summary>
    public bool IsFuture;
    /// <summary>
    /// The original conformance-class lexical value.
    /// </summary>
    public string ConformanceLexicalValue = "";
    /// <summary>
    /// The document conformance class.
    /// </summary>
    public OdfConformanceClass ConformanceClass;
    /// <summary>
    /// The producer or generator string used for compatibility classification.
    /// </summary>
    public string Producer = "";
    /// <summary>
    /// The selected standard or extended producer profile identifier.
    /// </summary>
    public string ProducerProfile = "";
}
