namespace odf;

/// <summary>
/// Describes the content-encryption algorithm declared for a package entry.
/// </summary>
public class OdfEncryptionAlgorithm
{
    /// <summary>
    /// The algorithm name or IRI exactly as declared.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The recognized encryption method.
    /// </summary>
    public OdfEncryptionMethod Method;
    /// <summary>
    /// The initialization-vector bytes when valid.
    /// </summary>
    public byte[] InitializationVector = [];
    /// <summary>
    /// The initialization-vector base64 lexical value.
    /// </summary>
    public string InitializationVectorLexical = "";
    /// <summary>
    /// Whether the initialization-vector lexical value is valid base64.
    /// </summary>
    public bool InitializationVectorIsValid;
    /// <summary>
    /// The preserved algorithm element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether all required algorithm values are present and valid.
    /// </summary>
    public bool IsValid;
}
