using System;
namespace token;

/// <summary>
/// Provides registered JOSE header parameter names.
/// </summary>
public static class JoseHeaderNames
{
    /// <summary>The algorithm header parameter.</summary>
    public const string Algorithm = "alg";

    /// <summary>The JWK Set URL header parameter.</summary>
    public const string JwkSetUrl = "jku";

    /// <summary>The JSON Web Key header parameter.</summary>
    public const string Jwk = "jwk";

    /// <summary>The key identifier header parameter.</summary>
    public const string KeyId = "kid";

    /// <summary>The X.509 URL header parameter.</summary>
    public const string X509Url = "x5u";

    /// <summary>The X.509 certificate chain header parameter.</summary>
    public const string X509CertificateChain = "x5c";

    /// <summary>The X.509 SHA-1 thumbprint header parameter.</summary>
    public const string X509Sha1Thumbprint = "x5t";

    /// <summary>The X.509 SHA-256 thumbprint header parameter.</summary>
    public const string X509Sha256Thumbprint = "x5t#S256";

    /// <summary>The type header parameter.</summary>
    public const string Type = "typ";

    /// <summary>The content type header parameter.</summary>
    public const string ContentType = "cty";

    /// <summary>The critical header parameter list.</summary>
    public const string Critical = "crit";

    /// <summary>The JWE content encryption algorithm header parameter.</summary>
    public const string EncryptionAlgorithm = "enc";

    /// <summary>The JWE compression algorithm header parameter.</summary>
    public const string CompressionAlgorithm = "zip";

    /// <summary>The ephemeral public key header parameter.</summary>
    public const string EphemeralPublicKey = "epk";

    /// <summary>The agreement PartyUInfo header parameter.</summary>
    public const string AgreementPartyUInfo = "apu";

    /// <summary>The agreement PartyVInfo header parameter.</summary>
    public const string AgreementPartyVInfo = "apv";

    /// <summary>The initialization vector header parameter.</summary>
    public const string InitializationVector = "iv";

    /// <summary>The authentication tag header parameter.</summary>
    public const string AuthenticationTag = "tag";

    /// <summary>The PBES2 salt input header parameter.</summary>
    public const string Pbes2SaltInput = "p2s";

    /// <summary>The PBES2 iteration count header parameter.</summary>
    public const string Pbes2Count = "p2c";

    /// <summary>The replicated issuer header parameter.</summary>
    public const string Issuer = "iss";

    /// <summary>The replicated subject header parameter.</summary>
    public const string Subject = "sub";

    /// <summary>The replicated audience header parameter.</summary>
    public const string Audience = "aud";

    /// <summary>The unencoded-payload option header parameter.</summary>
    public const string Base64UrlEncodePayload = "b64";

    /// <summary>The PASSporT extension identifier header parameter.</summary>
    public const string PassportType = "ppt";

    /// <summary>The URL header parameter.</summary>
    public const string Url = "url";

    /// <summary>The nonce header parameter.</summary>
    public const string Nonce = "nonce";

    /// <summary>The signature validation token header parameter.</summary>
    public const string SignatureValidationToken = "svt";

    /// <summary>The IHE SubmissionSet identifier header parameter.</summary>
    public const string IheSubmissionSetId = "iheSSId";

    /// <summary>The embedded JWT header parameter.</summary>
    public const string Jwt = "jwt";

    /// <summary>The OAuth client identifier header parameter.</summary>
    public const string ClientId = "client_id";

    /// <summary>The OpenID Federation trust chain header parameter.</summary>
    public const string TrustChain = "trust_chain";

    /// <summary>The OpenID Federation peer trust chain header parameter.</summary>
    public const string PeerTrustChain = "peer_trust_chain";
}
