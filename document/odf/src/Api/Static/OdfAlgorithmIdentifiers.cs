namespace odf;

/// <summary>
/// Provides standard identifiers used by ODF encryption and XML signatures.
/// </summary>
public static class OdfAlgorithmIdentifiers
{
    /// <summary>
    /// The legacy Blowfish CFB algorithm name.
    /// </summary>
    public const string BlowfishCfbName = "Blowfish CFB";
    /// <summary>
    /// The ODF Blowfish CFB algorithm IRI.
    /// </summary>
    public const string BlowfishCfb = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#blowfish";
    /// <summary>
    /// The AES-128 CBC algorithm IRI.
    /// </summary>
    public const string Aes128Cbc = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";
    /// <summary>
    /// The AES-192 CBC algorithm IRI.
    /// </summary>
    public const string Aes192Cbc = "http://www.w3.org/2001/04/xmlenc#aes192-cbc";
    /// <summary>
    /// The AES-256 CBC algorithm IRI.
    /// </summary>
    public const string Aes256Cbc = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
    /// <summary>
    /// The legacy PBKDF2 algorithm name.
    /// </summary>
    public const string Pbkdf2Name = "PBKDF2";
    /// <summary>
    /// The ODF PBKDF2 algorithm IRI.
    /// </summary>
    public const string Pbkdf2 = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#pbkdf2";
    /// <summary>
    /// The legacy SHA-1 start-key name.
    /// </summary>
    public const string Sha1Name = "SHA1";
    /// <summary>
    /// The XML Signature SHA-1 digest IRI.
    /// </summary>
    public const string Sha1 = "http://www.w3.org/2000/09/xmldsig#sha1";
    /// <summary>
    /// The SHA-256 digest IRI specified by ODF package encryption.
    /// </summary>
    public const string Sha256Odf = "http://www.w3.org/2000/09/xmldsig#sha256";
    /// <summary>
    /// The XML Encryption SHA-256 digest IRI.
    /// </summary>
    public const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
    /// <summary>
    /// The XML Signature SHA-384 digest IRI.
    /// </summary>
    public const string Sha384 = "http://www.w3.org/2001/04/xmldsig-more#sha384";
    /// <summary>
    /// The XML Encryption SHA-512 digest IRI.
    /// </summary>
    public const string Sha512 = "http://www.w3.org/2001/04/xmlenc#sha512";
    /// <summary>
    /// The legacy SHA-1 first-kilobyte checksum name.
    /// </summary>
    public const string Sha1OneKilobyteName = "SHA1/1K";
    /// <summary>
    /// The ODF SHA-1 first-kilobyte checksum IRI.
    /// </summary>
    public const string Sha1OneKilobyte = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#sha1-1k";
    /// <summary>
    /// The ODF SHA-256 first-kilobyte checksum IRI.
    /// </summary>
    public const string Sha256OneKilobyte = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#sha256-1k";
    /// <summary>
    /// The Canonical XML 1.0 algorithm IRI.
    /// </summary>
    public const string CanonicalXml10 = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    /// <summary>
    /// The Canonical XML 1.0 with-comments algorithm IRI.
    /// </summary>
    public const string CanonicalXml10WithComments = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
    /// <summary>
    /// The Canonical XML 1.1 algorithm IRI.
    /// </summary>
    public const string CanonicalXml11 = "http://www.w3.org/2006/12/xml-c14n11";
    /// <summary>
    /// The Canonical XML 1.1 with-comments algorithm IRI.
    /// </summary>
    public const string CanonicalXml11WithComments = "http://www.w3.org/2006/12/xml-c14n11#WithComments";
    /// <summary>
    /// The Exclusive Canonical XML 1.0 algorithm IRI.
    /// </summary>
    public const string ExclusiveCanonicalXml10 = "http://www.w3.org/2001/10/xml-exc-c14n#";
    /// <summary>
    /// The Exclusive Canonical XML 1.0 with-comments algorithm IRI.
    /// </summary>
    public const string ExclusiveCanonicalXml10WithComments = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
    /// <summary>
    /// The DSA with SHA-1 signature algorithm IRI.
    /// </summary>
    public const string DsaSha1 = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
    /// <summary>
    /// The RSA with SHA-1 signature algorithm IRI.
    /// </summary>
    public const string RsaSha1 = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
    /// <summary>
    /// The HMAC with SHA-1 signature algorithm IRI.
    /// </summary>
    public const string HmacSha1 = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";
    /// <summary>
    /// The RSA with SHA-256 signature algorithm IRI.
    /// </summary>
    public const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    /// <summary>
    /// The RSA with SHA-384 signature algorithm IRI.
    /// </summary>
    public const string RsaSha384 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
    /// <summary>
    /// The RSA with SHA-512 signature algorithm IRI.
    /// </summary>
    public const string RsaSha512 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
    /// <summary>
    /// The ECDSA with SHA-256 signature algorithm IRI.
    /// </summary>
    public const string EcdsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
    /// <summary>
    /// The enveloped-signature transform IRI.
    /// </summary>
    public const string EnvelopedSignatureTransform = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
    /// <summary>
    /// The Base64 transform IRI.
    /// </summary>
    public const string Base64Transform = "http://www.w3.org/2000/09/xmldsig#base64";
}
