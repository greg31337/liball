namespace odf;

/// <summary>
/// Describes password start-key generation for package encryption.
/// </summary>
public class OdfStartKeyGeneration
{
    /// <summary>
    /// The digest algorithm name or IRI exactly as declared.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The declared key size in bytes when valid and supported.
    /// </summary>
    public int KeySize;
    /// <summary>
    /// The key-size lexical value.
    /// </summary>
    public string KeySizeLexical = "";
    /// <summary>
    /// Whether a key size was declared.
    /// </summary>
    public bool HasKeySize;
    /// <summary>
    /// Whether the key-size lexical value is a supported non-negative integer.
    /// </summary>
    public bool KeySizeIsValid;
    /// <summary>
    /// The preserved start-key-generation element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether all required start-key values are present and valid.
    /// </summary>
    public bool IsValid;
}
