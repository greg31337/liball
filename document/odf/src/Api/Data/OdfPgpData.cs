namespace odf;

/// <summary>
/// Describes OpenPGP recipient identification stored in a package manifest.
/// </summary>
public class OdfPgpData
{
    /// <summary>
    /// The decoded OpenPGP key identifier bytes.
    /// </summary>
    public byte[] KeyId = [];
    /// <summary>
    /// The OpenPGP key identifier base64 lexical value.
    /// </summary>
    public string KeyIdLexical = "";
    /// <summary>
    /// Whether the key identifier lexical value is valid base64.
    /// </summary>
    public bool KeyIdIsValid;
    /// <summary>
    /// The preserved required PGPKeyID element, or <c>null</c> when absent.
    /// </summary>
    public OdfManifestNode? KeyIdNode;
    /// <summary>
    /// The optional decoded OpenPGP key packet bytes.
    /// </summary>
    public byte[] KeyPacket = [];
    /// <summary>
    /// The optional OpenPGP key packet base64 lexical value.
    /// </summary>
    public string KeyPacketLexical = "";
    /// <summary>
    /// Whether a key packet was present.
    /// </summary>
    public bool HasKeyPacket;
    /// <summary>
    /// Whether the key packet lexical value is valid base64.
    /// </summary>
    public bool KeyPacketIsValid;
    /// <summary>
    /// The preserved optional PGPKeyPacket element.
    /// </summary>
    public OdfManifestNode? KeyPacketNode;
    /// <summary>
    /// The preserved PGPData element.
    /// </summary>
    public OdfManifestNode Node = new OdfManifestNode();
    /// <summary>
    /// Whether the required recipient values are present and valid.
    /// </summary>
    public bool IsValid;
}
