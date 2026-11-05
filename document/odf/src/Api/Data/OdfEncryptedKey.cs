namespace odf;

/// <summary>
/// Describes one encrypted package key transported to an OpenPGP recipient.
/// </summary>
public class OdfEncryptedKey
{
    /// <summary>
    /// The optional OpenPGP encryption algorithm IRI.
    /// </summary>
    public string PgpAlgorithm = "";
    /// <summary>
    /// Whether an encryption-method element was present.
    /// </summary>
    public bool HasPgpAlgorithm;
    /// <summary>
    /// The preserved optional encryption-method element.
    /// </summary>
    public OdfManifestNode? EncryptionMethodNode;
    /// <summary>
    /// The preserved required keyinfo element, or <c>null</c> when absent.
    /// </summary>
    public OdfManifestNode? KeyInfoNode;
    /// <summary>
    /// The recipient OpenPGP data.
    /// </summary>
    public OdfPgpData PgpData = new OdfPgpData();
    /// <summary>
    /// The encrypted key value.
    /// </summary>
    public OdfCipherData CipherData = new OdfCipherData();
    /// <summary>
    /// The preserved encrypted-key element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether the required OpenPGP and cipher values are present and valid.
    /// </summary>
    public bool IsValid;
}
