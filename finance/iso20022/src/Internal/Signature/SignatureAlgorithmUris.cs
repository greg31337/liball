namespace iso20022;

internal static class SignatureAlgorithmUris
{
    internal const string ExclusiveCanonicalization =
        "http://www.w3.org/2001/10/xml-exc-c14n#";
    internal const string ExclusiveCanonicalizationWithComments =
        "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
    internal const string EnvelopedSignature =
        "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
    internal const string Sha1 =
        "http://www.w3.org/2000/09/xmldsig#sha1";
    internal const string Sha256 =
        "http://www.w3.org/2001/04/xmlenc#sha256";
    internal const string Sha384 =
        "http://www.w3.org/2001/04/xmldsig-more#sha384";
    internal const string Sha512 =
        "http://www.w3.org/2001/04/xmlenc#sha512";
    internal const string RsaSha1 =
        "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
    internal const string RsaSha256 =
        "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    internal const string RsaSha384 =
        "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
    internal const string RsaSha512 =
        "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
}
