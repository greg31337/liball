namespace odf;

/// <summary>
/// Describes X.509 material retained from an XML Signature KeyInfo element.
/// </summary>
public class OdfX509Data
{
    /// <summary>
    /// Issuer distinguished names in source order.
    /// </summary>
    public string[] IssuerNames = [];
    /// <summary>
    /// Issuer certificate serial-number lexical values corresponding to issuer names.
    /// </summary>
    public string[] SerialNumbers = [];
    /// <summary>
    /// Subject distinguished names in source order.
    /// </summary>
    public string[] SubjectNames = [];
    /// <summary>
    /// Decoded subject key identifiers.
    /// </summary>
    public byte[][] SubjectKeyIdentifiers = [];
    /// <summary>
    /// Subject key identifier base64 lexical texts.
    /// </summary>
    public string[] SubjectKeyIdentifierLexicalValues = [];
    /// <summary>
    /// Validity flags corresponding to subject key identifiers.
    /// </summary>
    public bool[] SubjectKeyIdentifierIsValid = [];
    /// <summary>
    /// Decoded DER certificates.
    /// </summary>
    public byte[][] Certificates = [];
    /// <summary>
    /// Certificate base64 lexical texts.
    /// </summary>
    public string[] CertificateLexicalValues = [];
    /// <summary>
    /// Validity flags corresponding to certificates.
    /// </summary>
    public bool[] CertificateIsValid = [];
    /// <summary>
    /// Decoded certificate-revocation lists.
    /// </summary>
    public byte[][] CertificateRevocationLists = [];
    /// <summary>
    /// Certificate-revocation-list base64 lexical texts.
    /// </summary>
    public string[] CertificateRevocationListLexicalValues = [];
    /// <summary>
    /// Validity flags corresponding to certificate-revocation lists.
    /// </summary>
    public bool[] CertificateRevocationListIsValid = [];
    /// <summary>
    /// XML Signature 1.1 X509Digest algorithm IRIs.
    /// </summary>
    public string[] DigestAlgorithms = [];
    /// <summary>
    /// Decoded XML Signature 1.1 X509Digest values.
    /// </summary>
    public byte[][] Digests = [];
    /// <summary>
    /// XML Signature 1.1 X509Digest base64 lexical texts.
    /// </summary>
    public string[] DigestLexicalValues = [];
    /// <summary>
    /// Validity flags corresponding to X509Digest values.
    /// </summary>
    public bool[] DigestIsValid = [];
    /// <summary>
    /// The preserved X509Data subtree.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Whether all retained binary values have valid non-empty base64 forms.
    /// </summary>
    public bool IsValid;
}
