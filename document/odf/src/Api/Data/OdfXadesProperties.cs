namespace odf;

/// <summary>
/// Describes one XAdES qualifying-properties subtree.
/// </summary>
public class OdfXadesProperties
{
    /// <summary>
    /// The XAdES namespace IRI used by the subtree.
    /// </summary>
    public string NamespaceName = "";
    /// <summary>
    /// The QualifyingProperties Target reference.
    /// </summary>
    public string Target = "";
    /// <summary>
    /// The optional QualifyingProperties identifier.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The optional SignedProperties identifier.
    /// </summary>
    public string SignedPropertiesId = "";
    /// <summary>
    /// The signing-time lexical value, or an empty string.
    /// </summary>
    public string SigningTime = "";
    /// <summary>
    /// Whether SignedProperties was present.
    /// </summary>
    public bool HasSignedProperties;
    /// <summary>
    /// Whether UnsignedProperties was present.
    /// </summary>
    public bool HasUnsignedProperties;
    /// <summary>
    /// Whether a SigningCertificate property was present.
    /// </summary>
    public bool HasSigningCertificate;
    /// <summary>
    /// Whether a SigningCertificateV2 property was present.
    /// </summary>
    public bool HasSigningCertificateV2;
    /// <summary>
    /// The preserved SignedProperties subtree.
    /// </summary>
    public string SignedPropertiesXml = "";
    /// <summary>
    /// The preserved SignedSignatureProperties subtree.
    /// </summary>
    public string SignedSignaturePropertiesXml = "";
    /// <summary>
    /// The preserved SignedDataObjectProperties subtree.
    /// </summary>
    public string SignedDataObjectPropertiesXml = "";
    /// <summary>
    /// The preserved UnsignedProperties subtree.
    /// </summary>
    public string UnsignedPropertiesXml = "";
    /// <summary>
    /// The preserved UnsignedSignatureProperties subtree.
    /// </summary>
    public string UnsignedSignaturePropertiesXml = "";
    /// <summary>
    /// The preserved UnsignedDataObjectProperties subtree.
    /// </summary>
    public string UnsignedDataObjectPropertiesXml = "";
    /// <summary>
    /// The preserved complete QualifyingProperties subtree.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Whether required qualifying-properties metadata is present.
    /// </summary>
    public bool IsValid;
}
