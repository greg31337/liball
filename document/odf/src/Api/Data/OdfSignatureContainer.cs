namespace odf;

/// <summary>
/// Describes an OpenDocument digital-signature container file.
/// </summary>
public class OdfSignatureContainer
{
    /// <summary>
    /// The package path of the signature container.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The declared ODF digital-signature version.
    /// </summary>
    public string Version = "";
    /// <summary>
    /// Signatures in source order.
    /// </summary>
    public OdfSignature[] Signatures = [];
    /// <summary>
    /// The exact original container bytes.
    /// </summary>
    public byte[] OriginalBytes = [];
    /// <summary>
    /// The preserved document-element XML.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Diagnostics produced while parsing the container.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// Whether the container and all contained signatures are structurally valid.
    /// </summary>
    public bool IsValid;
}
