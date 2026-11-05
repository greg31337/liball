namespace odf;

/// <summary>
/// Describes per-entry key derivation for package encryption.
/// </summary>
public class OdfKeyDerivation
{
    /// <summary>
    /// The derivation algorithm name or IRI exactly as declared.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The salt bytes when the lexical value is valid base64.
    /// </summary>
    public byte[] Salt = [];
    /// <summary>
    /// The salt base64 lexical value.
    /// </summary>
    public string SaltLexical = "";
    /// <summary>
    /// Whether the salt lexical value is valid base64.
    /// </summary>
    public bool SaltIsValid;
    /// <summary>
    /// The iteration count when valid and supported.
    /// </summary>
    public int IterationCount;
    /// <summary>
    /// The iteration-count lexical value.
    /// </summary>
    public string IterationCountLexical = "";
    /// <summary>
    /// Whether an iteration count was declared.
    /// </summary>
    public bool HasIterationCount;
    /// <summary>
    /// Whether the iteration-count lexical value is a supported non-negative integer.
    /// </summary>
    public bool IterationCountIsValid;
    /// <summary>
    /// The derived key size in bytes when valid and supported.
    /// </summary>
    public int KeySize;
    /// <summary>
    /// The key-size lexical value.
    /// </summary>
    public string KeySizeLexical = "";
    /// <summary>
    /// Whether a derived key size was declared.
    /// </summary>
    public bool HasKeySize;
    /// <summary>
    /// Whether the derived key-size lexical value is a supported non-negative integer.
    /// </summary>
    public bool KeySizeIsValid;
    /// <summary>
    /// Whether the derivation name declares public-key OpenPGP transport.
    /// </summary>
    public bool IsPgp;
    /// <summary>
    /// The preserved key-derivation element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether all required derivation values are present and valid.
    /// </summary>
    public bool IsValid;
}
