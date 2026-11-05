namespace odf;

/// <summary>
/// Describes an entry encryption method.
/// </summary>
public class OdfEncryptionInfo
{
    /// <summary>
    /// The encryption method.
    /// </summary>
    public OdfEncryptionMethod Method;
    /// <summary>
    /// The checksum algorithm name or IRI exactly as declared.
    /// </summary>
    public string ChecksumType = "";
    /// <summary>
    /// The checksum bytes when the lexical value is valid base64.
    /// </summary>
    public byte[] Checksum = [];
    /// <summary>
    /// The checksum base64 lexical value.
    /// </summary>
    public string ChecksumLexical = "";
    /// <summary>
    /// Whether the checksum lexical value is valid base64.
    /// </summary>
    public bool ChecksumIsValid;
    /// <summary>
    /// The encrypted entry expanded size copied from its manifest file entry.
    /// </summary>
    public long Size;
    /// <summary>
    /// The expanded-size lexical value copied from its manifest file entry.
    /// </summary>
    public string SizeLexical = "";
    /// <summary>
    /// Whether an expanded size was declared for the encrypted entry.
    /// </summary>
    public bool HasSize;
    /// <summary>
    /// Whether the expanded-size lexical value is valid.
    /// </summary>
    public bool SizeIsValid;
    /// <summary>
    /// The content-encryption algorithm description.
    /// </summary>
    public OdfEncryptionAlgorithm Algorithm = new OdfEncryptionAlgorithm();
    /// <summary>
    /// The optional password start-key generation description.
    /// </summary>
    public OdfStartKeyGeneration StartKeyGeneration = new OdfStartKeyGeneration();
    /// <summary>
    /// Whether a start-key-generation element was present.
    /// </summary>
    public bool HasStartKeyGeneration;
    /// <summary>
    /// The per-entry key derivation description.
    /// </summary>
    public OdfKeyDerivation KeyDerivation = new OdfKeyDerivation();
    /// <summary>
    /// The preserved encryption-data element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether the required encryption metadata has valid lexical values.
    /// </summary>
    public bool IsValid;
}
