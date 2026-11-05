using System;
using System.Security.Cryptography.X509Certificates;
namespace token;

/// <summary>
/// Provides X.509 key import, offline chain validation, and certificate thumbprints.
/// </summary>
public interface ITokenX509Service
{
    /// <summary>Imports the public key and metadata from an RSA or ECDSA certificate.</summary>
    /// <param name="certificate">The caller-owned certificate.</param>
    /// <returns>A certificate-backed key, or stable failure details.</returns>
    TokenKeyResult ImportCertificate(X509Certificate2 certificate);

    /// <summary>Imports a certificate-backed key with an explicit leaf-first chain.</summary>
    /// <param name="certificate">The caller-owned leaf certificate.</param>
    /// <param name="certificateChain">The caller-owned chain from leaf to trust anchor.</param>
    /// <returns>A certificate-backed key, or stable failure details.</returns>
    TokenKeyResult ImportCertificateChain(X509Certificate2 certificate, X509Certificate2[] certificateChain);

    /// <summary>Validates certificate lifetime and supported public-key policy at an instant.</summary>
    /// <param name="certificate">The certificate to validate.</param>
    /// <param name="validationTime">The validation instant.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ValidateCertificate(X509Certificate2 certificate, DateTimeOffset validationTime);

    /// <summary>Validates an explicit leaf-first chain offline against its final trust anchor.</summary>
    /// <param name="certificateChain">The chain from leaf to trust anchor.</param>
    /// <param name="validationTime">The validation instant.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ValidateCertificateChain(X509Certificate2[] certificateChain, DateTimeOffset validationTime);

    /// <summary>Calculates the standards-defined SHA-1 certificate thumbprint.</summary>
    /// <param name="certificate">The certificate whose DER bytes are hashed.</param>
    /// <returns>The 20-byte thumbprint.</returns>
    TokenBytesResult CalculateCertificateSha1Thumbprint(X509Certificate2 certificate);

    /// <summary>Calculates the SHA-256 certificate thumbprint.</summary>
    /// <param name="certificate">The certificate whose DER bytes are hashed.</param>
    /// <returns>The 32-byte thumbprint.</returns>
    TokenBytesResult CalculateCertificateSha256Thumbprint(X509Certificate2 certificate);

    /// <summary>Calculates the unpadded base64url SHA-1 certificate thumbprint.</summary>
    /// <param name="certificate">The certificate whose DER bytes are hashed.</param>
    /// <returns>The base64url thumbprint.</returns>
    TokenTextResult CalculateCertificateSha1ThumbprintBase64Url(X509Certificate2 certificate);

    /// <summary>Calculates the unpadded base64url SHA-256 certificate thumbprint.</summary>
    /// <param name="certificate">The certificate whose DER bytes are hashed.</param>
    /// <returns>The base64url thumbprint.</returns>
    TokenTextResult CalculateCertificateSha256ThumbprintBase64Url(X509Certificate2 certificate);
}
