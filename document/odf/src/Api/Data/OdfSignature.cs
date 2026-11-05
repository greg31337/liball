using System.Xml;

namespace odf;

/// <summary>
/// Describes one XML signature stored in an OpenDocument package.
/// </summary>
public class OdfSignature
{
    /// <summary>
    /// The optional XML Signature identifier.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The canonicalization algorithm declared by SignedInfo.
    /// </summary>
    public string CanonicalizationMethod = "";
    /// <summary>
    /// The exclusive-canonicalization inclusive-prefix list.
    /// </summary>
    public string CanonicalizationInclusivePrefixes = "";
    /// <summary>
    /// The signature algorithm declared by SignedInfo.
    /// </summary>
    public string SignatureMethod = "";
    /// <summary>
    /// References signed directly by SignedInfo.
    /// </summary>
    public OdfSignatureReference[] References = [];
    /// <summary>
    /// References retained from XML Signature Manifest objects.
    /// </summary>
    public OdfSignatureReference[] ManifestReferences = [];
    /// <summary>
    /// The optional SignatureValue identifier.
    /// </summary>
    public string SignatureValueId = "";
    /// <summary>
    /// The decoded signature value.
    /// </summary>
    public byte[] SignatureValue = [];
    /// <summary>
    /// The signature value base64 lexical text.
    /// </summary>
    public string SignatureValueLexical = "";
    /// <summary>
    /// Whether the signature value is valid non-empty base64.
    /// </summary>
    public bool SignatureValueIsValid;
    /// <summary>
    /// Key names retained from KeyInfo.
    /// </summary>
    public string[] KeyNames = [];
    /// <summary>
    /// Parsed X509Data groups retained from KeyInfo.
    /// </summary>
    public OdfX509Data[] X509Data = [];
    /// <summary>
    /// Decoded XML Signature 1.1 DER-encoded key values.
    /// </summary>
    public byte[][] DerEncodedKeyValues = [];
    /// <summary>
    /// DER-encoded key value base64 lexical texts.
    /// </summary>
    public string[] DerEncodedKeyValueLexicalValues = [];
    /// <summary>
    /// Validity flags corresponding to DER-encoded key values.
    /// </summary>
    public bool[] DerEncodedKeyValueIsValid = [];
    /// <summary>
    /// XML Signature 1.1 KeyInfoReference URI values.
    /// </summary>
    public string[] KeyInfoReferenceUris = [];
    /// <summary>
    /// Parsed XAdES qualifying-properties groups.
    /// </summary>
    public OdfXadesProperties[] XadesProperties = [];
    /// <summary>
    /// Preserved XML Signature Object subtrees.
    /// </summary>
    public string[] ObjectXml = [];
    /// <summary>
    /// The preserved SignedInfo subtree.
    /// </summary>
    public string SignedInfoXml = "";
    /// <summary>
    /// The preserved KeyInfo subtree, or an empty string.
    /// </summary>
    public string KeyInfoXml = "";
    /// <summary>
    /// The preserved complete Signature subtree.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Diagnostics produced while parsing this signature.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// The signature status.
    /// </summary>
    public OdfSignatureStatus Status;
    /// <summary>
    /// Whether every directly signed and manifest reference digest matched.
    /// </summary>
    public bool ReferencesAreValid;
    /// <summary>
    /// Whether the embedded signing-certificate material was usable and consistent.
    /// </summary>
    public bool CertificateMaterialIsValid;
    /// <summary>
    /// The subject name of the certificate used for verification.
    /// </summary>
    public string SigningCertificateSubject = "";
    /// <summary>
    /// The hexadecimal thumbprint of the certificate used for verification.
    /// </summary>
    public string SigningCertificateThumbprint = "";
    /// <summary>
    /// Whether the cryptographic signature value matched canonicalized SignedInfo.
    /// </summary>
    public bool SignatureValueMatches;
    /// <summary>
    /// Whether all required signature structures and lexical values are valid.
    /// </summary>
    public bool IsValid;
    internal XmlElement? SignatureElement;
    internal XmlElement? SignedInfoElement;
}
