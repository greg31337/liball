using System;
namespace token;

/// <summary>
/// Provides registered JOSE header parameter names and usage lookups.
/// </summary>
internal static class JoseHeaderNamesOperations
{
    /// <summary>The algorithm header parameter.</summary>
    internal const string Algorithm = JoseHeaderNames.Algorithm;

    /// <summary>The JWK Set URL header parameter.</summary>
    internal const string JwkSetUrl = JoseHeaderNames.JwkSetUrl;

    /// <summary>The JSON Web Key header parameter.</summary>
    internal const string Jwk = JoseHeaderNames.Jwk;

    /// <summary>The key identifier header parameter.</summary>
    internal const string KeyId = JoseHeaderNames.KeyId;

    /// <summary>The X.509 URL header parameter.</summary>
    internal const string X509Url = JoseHeaderNames.X509Url;

    /// <summary>The X.509 certificate chain header parameter.</summary>
    internal const string X509CertificateChain = JoseHeaderNames.X509CertificateChain;

    /// <summary>The X.509 SHA-1 thumbprint header parameter.</summary>
    internal const string X509Sha1Thumbprint = JoseHeaderNames.X509Sha1Thumbprint;

    /// <summary>The X.509 SHA-256 thumbprint header parameter.</summary>
    internal const string X509Sha256Thumbprint = JoseHeaderNames.X509Sha256Thumbprint;

    /// <summary>The type header parameter.</summary>
    internal const string Type = JoseHeaderNames.Type;

    /// <summary>The content type header parameter.</summary>
    internal const string ContentType = JoseHeaderNames.ContentType;

    /// <summary>The critical header parameter list.</summary>
    internal const string Critical = JoseHeaderNames.Critical;

    /// <summary>The JWE content encryption algorithm header parameter.</summary>
    internal const string EncryptionAlgorithm = JoseHeaderNames.EncryptionAlgorithm;

    /// <summary>The JWE compression algorithm header parameter.</summary>
    internal const string CompressionAlgorithm = JoseHeaderNames.CompressionAlgorithm;

    /// <summary>The ephemeral public key header parameter.</summary>
    internal const string EphemeralPublicKey = JoseHeaderNames.EphemeralPublicKey;

    /// <summary>The agreement PartyUInfo header parameter.</summary>
    internal const string AgreementPartyUInfo = JoseHeaderNames.AgreementPartyUInfo;

    /// <summary>The agreement PartyVInfo header parameter.</summary>
    internal const string AgreementPartyVInfo = JoseHeaderNames.AgreementPartyVInfo;

    /// <summary>The initialization vector header parameter.</summary>
    internal const string InitializationVector = JoseHeaderNames.InitializationVector;

    /// <summary>The authentication tag header parameter.</summary>
    internal const string AuthenticationTag = JoseHeaderNames.AuthenticationTag;

    /// <summary>The PBES2 salt input header parameter.</summary>
    internal const string Pbes2SaltInput = JoseHeaderNames.Pbes2SaltInput;

    /// <summary>The PBES2 iteration count header parameter.</summary>
    internal const string Pbes2Count = JoseHeaderNames.Pbes2Count;

    /// <summary>The replicated issuer header parameter.</summary>
    internal const string Issuer = JoseHeaderNames.Issuer;

    /// <summary>The replicated subject header parameter.</summary>
    internal const string Subject = JoseHeaderNames.Subject;

    /// <summary>The replicated audience header parameter.</summary>
    internal const string Audience = JoseHeaderNames.Audience;

    /// <summary>The unencoded-payload option header parameter.</summary>
    internal const string Base64UrlEncodePayload = JoseHeaderNames.Base64UrlEncodePayload;

    /// <summary>The PASSporT extension identifier header parameter.</summary>
    internal const string PassportType = JoseHeaderNames.PassportType;

    /// <summary>The URL header parameter.</summary>
    internal const string Url = JoseHeaderNames.Url;

    /// <summary>The nonce header parameter.</summary>
    internal const string Nonce = JoseHeaderNames.Nonce;

    /// <summary>The signature validation token header parameter.</summary>
    internal const string SignatureValidationToken = JoseHeaderNames.SignatureValidationToken;

    /// <summary>The IHE SubmissionSet identifier header parameter.</summary>
    internal const string IheSubmissionSetId = JoseHeaderNames.IheSubmissionSetId;

    /// <summary>The embedded JWT header parameter.</summary>
    internal const string Jwt = JoseHeaderNames.Jwt;

    /// <summary>The OAuth client identifier header parameter.</summary>
    internal const string ClientId = JoseHeaderNames.ClientId;

    /// <summary>The OpenID Federation trust chain header parameter.</summary>
    internal const string TrustChain = JoseHeaderNames.TrustChain;

    /// <summary>The OpenID Federation peer trust chain header parameter.</summary>
    internal const string PeerTrustChain = JoseHeaderNames.PeerTrustChain;

    static readonly string[] all = new string[]
    {
        Algorithm, JwkSetUrl, Jwk, KeyId, X509Url, X509CertificateChain,
        X509Sha1Thumbprint, X509Sha256Thumbprint, Type, ContentType, Critical,
        EncryptionAlgorithm, CompressionAlgorithm, EphemeralPublicKey,
        AgreementPartyUInfo, AgreementPartyVInfo, InitializationVector,
        AuthenticationTag, Pbes2SaltInput, Pbes2Count, Issuer, Subject, Audience,
        Base64UrlEncodePayload, PassportType, Url, Nonce, SignatureValidationToken,
        IheSubmissionSetId, Jwt, ClientId, TrustChain, PeerTrustChain
    };

    static readonly string[] jws = new string[]
    {
        Algorithm, JwkSetUrl, Jwk, KeyId, X509Url, X509CertificateChain,
        X509Sha1Thumbprint, X509Sha256Thumbprint, Type, ContentType, Critical,
        Base64UrlEncodePayload, PassportType, Url, Nonce, SignatureValidationToken,
        IheSubmissionSetId, Jwt, ClientId, TrustChain, PeerTrustChain
    };

    static readonly string[] jwe = new string[]
    {
        Algorithm, EncryptionAlgorithm, CompressionAlgorithm, JwkSetUrl, Jwk, KeyId,
        X509Url, X509CertificateChain, X509Sha1Thumbprint, X509Sha256Thumbprint,
        Type, ContentType, Critical, EphemeralPublicKey, AgreementPartyUInfo,
        AgreementPartyVInfo, InitializationVector, AuthenticationTag, Pbes2SaltInput,
        Pbes2Count, Issuer, Subject, Audience, Url, Nonce
    };

    /// <summary>
    /// Determines whether a name is registered as a JOSE header parameter.
    /// </summary>
    /// <param name="name">The case-sensitive header parameter name.</param>
    /// <returns><c>true</c> when the name is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(string name)
    {
        return RegistryLookup.Contains(all, name, false);
    }

    /// <summary>
    /// Determines whether a registered header parameter is used with JWS.
    /// </summary>
    /// <param name="name">The case-sensitive header parameter name.</param>
    /// <returns><c>true</c> when the parameter is registered for JWS; otherwise, <c>false</c>.</returns>
    internal static bool IsForJws(string name)
    {
        return RegistryLookup.Contains(jws, name, false);
    }

    /// <summary>
    /// Determines whether a registered header parameter is used with JWE.
    /// </summary>
    /// <param name="name">The case-sensitive header parameter name.</param>
    /// <returns><c>true</c> when the parameter is registered for JWE; otherwise, <c>false</c>.</returns>
    internal static bool IsForJwe(string name)
    {
        return RegistryLookup.Contains(jwe, name, false);
    }

    /// <summary>
    /// Returns all registered JOSE header parameter names known by this library.
    /// </summary>
    /// <returns>A new array containing the registered names.</returns>
    internal static string[] GetAll()
    {
        return RegistryLookup.Copy(all);
    }
}
