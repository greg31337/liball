namespace odf;

/// <summary>
/// Describes an encrypted key value stored in a package manifest.
/// </summary>
public class OdfCipherData
{
    /// <summary>
    /// The decoded cipher value bytes.
    /// </summary>
    public byte[] Value = [];
    /// <summary>
    /// The cipher value base64 lexical value.
    /// </summary>
    public string ValueLexical = "";
    /// <summary>
    /// Whether the cipher value lexical value is valid base64.
    /// </summary>
    public bool ValueIsValid;
    /// <summary>
    /// The preserved required CipherValue element, or <c>null</c> when absent.
    /// </summary>
    public OdfManifestNode? ValueNode;
    /// <summary>
    /// The preserved CipherData element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether a valid CipherValue child was present.
    /// </summary>
    public bool IsValid;
}
