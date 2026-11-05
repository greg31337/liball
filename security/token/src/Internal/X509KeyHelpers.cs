using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>
/// Provides X.509 certificate key and thumbprint helpers.
/// </summary>
internal static class X509KeyHelpers
{
    const string RsaPublicKeyOid = "1.2.840.113549.1.1.1";
    const string RsaPssPublicKeyOid = "1.2.840.113549.1.1.10";
    const string EcPublicKeyOid = "1.2.840.10045.2.1";

    /// <summary>
    /// Imports an RSA or ECDSA certificate as a normalized key.
    /// </summary>
    /// <remarks>
    /// The returned key references the caller-owned certificate and does not retain temporary
    /// native public-key handles. Certificate lifetime and trust are checked separately by
    /// <see cref="Validate(TokenKey)"/>.
    /// </remarks>
    /// <param name="certificate">The caller-owned end-entity certificate.</param>
    /// <returns>A result containing the certificate-backed key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenKeyResult Import(X509Certificate2 certificate)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException("certificate");
        }

        return ImportCore(certificate, System.Array.Empty<X509Certificate2>());
    }

    /// <summary>
    /// Imports an RSA or ECDSA certificate with its leaf-first certificate chain.
    /// </summary>
    /// <remarks>
    /// The first chain element must be the imported certificate. The returned key references
    /// the caller-owned certificates and owns only an independent copy of the chain array.
    /// </remarks>
    /// <param name="certificate">The caller-owned end-entity certificate.</param>
    /// <param name="certificateChain">The caller-owned chain ordered from leaf to trust anchor.</param>
    /// <returns>A result containing the certificate-backed key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    internal static TokenKeyResult Import(
        X509Certificate2 certificate,
        X509Certificate2[] certificateChain)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException("certificate");
        }

        if (certificateChain == null)
        {
            throw new ArgumentNullException("certificateChain");
        }

        if (certificateChain.Length == 0)
        {
            return ImportFailure(
                "missing_certificate_chain",
                "The certificate chain is empty.");
        }

        int index = 0;
        while (index < certificateChain.Length)
        {
            if (certificateChain[index] == null)
            {
                return ImportFailure(
                    "invalid_certificate_chain",
                    "The certificate chain contains a null certificate.");
            }

            index++;
        }

        if (!ConstantTime.AreEqual(certificate.RawData, certificateChain[0].RawData))
        {
            return ImportFailure(
                "certificate_chain_mismatch",
                "The first certificate in the chain is not the imported certificate.");
        }

        X509Certificate2[] copy = new X509Certificate2[certificateChain.Length];
        System.Array.Copy(certificateChain, copy, certificateChain.Length);
        return ImportCore(certificate, copy);
    }

    /// <summary>
    /// Validates a certificate-backed key at the current UTC time.
    /// </summary>
    /// <remarks>
    /// A present certificate chain is validated with its last element as the explicit trust anchor.
    /// </remarks>
    /// <param name="key">The certificate-backed key to validate.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        bool validateChain = key.CertificateChain != null && key.CertificateChain.Length > 0;
        return Validate(key, DateTimeOffset.UtcNow, validateChain);
    }

    /// <summary>
    /// Validates a certificate-backed key at a specified time with optional chain validation.
    /// </summary>
    /// <param name="key">The certificate-backed key to validate.</param>
    /// <param name="validationTime">The instant at which certificate validity is evaluated.</param>
    /// <param name="validateChain">Whether an explicit leaf-first chain is required and validated.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(
        TokenKey key,
        DateTimeOffset validationTime,
        bool validateChain)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key.Certificate == null)
        {
            return ValidationFailure(
                "missing_certificate",
                "An end-entity certificate is required.");
        }

        X509CertificateKeyInfo info = Inspect(key.Certificate);
        if (info.Error != null)
        {
            return ValidationFailure(info.Error);
        }

        TokenResult metadata = ValidateMetadata(key, info);
        if (!metadata.Success)
        {
            return metadata;
        }

        TokenResult algorithms = ValidateAlgorithms(key, info);
        if (!algorithms.Success)
        {
            return algorithms;
        }

        TokenResult certificateValidation = Validate(key.Certificate, validationTime);
        if (!certificateValidation.Success)
        {
            return certificateValidation;
        }

        if (key.CertificateSha1Thumbprint != null)
        {
            byte[] actualSha1 = key.Certificate.GetCertHash(HashAlgorithmName.SHA1);
            if (!ConstantTime.AreEqual(key.CertificateSha1Thumbprint, actualSha1))
            {
                return ValidationFailure(
                    "certificate_thumbprint_mismatch",
                    "The SHA-1 thumbprint does not match the certificate.");
            }
        }

        if (key.CertificateSha256Thumbprint != null)
        {
            byte[] actualSha256 = key.Certificate.GetCertHash(HashAlgorithmName.SHA256);
            if (!ConstantTime.AreEqual(key.CertificateSha256Thumbprint, actualSha256))
            {
                return ValidationFailure(
                    "certificate_thumbprint_mismatch",
                    "The SHA-256 thumbprint does not match the certificate.");
            }
        }

        if (validateChain)
        {
            if (key.CertificateChain == null || key.CertificateChain.Length == 0)
            {
                return ValidationFailure(
                    "missing_certificate_chain",
                    "Certificate chain validation was requested without a chain.");
            }

            if (key.CertificateChain[0] == null
                || !ConstantTime.AreEqual(
                    key.Certificate.RawData,
                    key.CertificateChain[0].RawData))
            {
                return ValidationFailure(
                    "certificate_chain_mismatch",
                    "The first certificate in the chain is not the imported certificate.");
            }

            return ValidateChain(key.CertificateChain, validationTime);
        }

        return ValidationSuccess();
    }

    /// <summary>
    /// Validates certificate lifetime and supported public-key policy at a specified time.
    /// </summary>
    /// <param name="certificate">The certificate to validate.</param>
    /// <param name="validationTime">The instant at which certificate validity is evaluated.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenResult Validate(
        X509Certificate2 certificate,
        DateTimeOffset validationTime)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException("certificate");
        }

        X509CertificateKeyInfo info = Inspect(certificate);
        if (info.Error != null)
        {
            return ValidationFailure(info.Error);
        }

        DateTime utc = validationTime.UtcDateTime;
        if (utc < certificate.NotBefore.ToUniversalTime())
        {
            return ValidationFailure(
                "certificate_not_yet_valid",
                "The certificate is not yet valid at the requested time.");
        }

        if (utc > certificate.NotAfter.ToUniversalTime())
        {
            return ValidationFailure(
                "certificate_expired",
                "The certificate is expired at the requested time.");
        }

        return ValidationSuccess();
    }

    /// <summary>
    /// Validates an explicit leaf-first certificate chain at a specified time.
    /// </summary>
    /// <remarks>
    /// Validation is deterministic and offline: revocation checks and certificate downloads are
    /// disabled, the final supplied certificate is the custom trust anchor, and the built path
    /// must exactly match the supplied order.
    /// </remarks>
    /// <param name="certificateChain">The chain ordered from leaf to trust anchor.</param>
    /// <param name="validationTime">The instant at which the chain is evaluated.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificateChain</c> is null.</exception>
    internal static TokenResult ValidateChain(
        X509Certificate2[] certificateChain,
        DateTimeOffset validationTime)
    {
        if (certificateChain == null)
        {
            throw new ArgumentNullException("certificateChain");
        }

        if (certificateChain.Length == 0)
        {
            return ValidationFailure(
                "missing_certificate_chain",
                "The certificate chain is empty.");
        }

        int index = 0;
        while (index < certificateChain.Length)
        {
            if (certificateChain[index] == null)
            {
                return ValidationFailure(
                    "invalid_certificate_chain",
                    "The certificate chain contains a null certificate.");
            }

            TokenResult certificateValidation = Validate(
                certificateChain[index],
                validationTime);
            if (!certificateValidation.Success)
            {
                return certificateValidation;
            }

            index++;
        }

        using (X509Chain chain = new X509Chain())
        {
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.VerificationTime = validationTime.UtcDateTime;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(
                certificateChain[certificateChain.Length - 1]);

            index = 1;
            while (index < certificateChain.Length - 1)
            {
                chain.ChainPolicy.ExtraStore.Add(certificateChain[index]);
                index++;
            }

            if (!chain.Build(certificateChain[0]))
            {
                return ValidationFailure(
                    "invalid_certificate_chain",
                    "The certificate chain could not be validated.");
            }

            if (chain.ChainElements.Count != certificateChain.Length)
            {
                return ValidationFailure(
                    "certificate_chain_mismatch",
                    "The validated certificate path does not match the supplied chain.");
            }

            index = 0;
            while (index < certificateChain.Length)
            {
                X509Certificate2 built = chain.ChainElements[index].Certificate;
                if (!ConstantTime.AreEqual(built.RawData, certificateChain[index].RawData))
                {
                    return ValidationFailure(
                        "certificate_chain_mismatch",
                        "The validated certificate path does not match the supplied chain.");
                }

                index++;
            }
        }

        return ValidationSuccess();
    }

    /// <summary>
    /// Calculates the SHA-1 thumbprint used by the JOSE <c>x5t</c> parameter.
    /// </summary>
    /// <remarks>
    /// SHA-1 is exposed only for the standards-defined certificate identifier and is not suitable for signing.
    /// </remarks>
    /// <param name="certificate">The certificate whose DER encoding is hashed.</param>
    /// <returns>A successful result containing the 20-byte SHA-1 thumbprint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenBytesResult CalculateSha1Thumbprint(X509Certificate2 certificate)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException("certificate");
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = certificate.GetCertHash(HashAlgorithmName.SHA1);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Calculates the SHA-256 thumbprint used by the JOSE <c>x5t#S256</c> parameter.
    /// </summary>
    /// <param name="certificate">The certificate whose DER encoding is hashed.</param>
    /// <returns>A successful result containing the 32-byte SHA-256 thumbprint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenBytesResult CalculateSha256Thumbprint(X509Certificate2 certificate)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException("certificate");
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = certificate.GetCertHash(HashAlgorithmName.SHA256);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Calculates the unpadded base64url SHA-1 thumbprint used by the JOSE <c>x5t</c> parameter.
    /// </summary>
    /// <remarks>
    /// SHA-1 is exposed only for the standards-defined certificate identifier and is not suitable for signing.
    /// </remarks>
    /// <param name="certificate">The certificate whose DER encoding is hashed.</param>
    /// <returns>A successful result containing the base64url thumbprint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenTextResult CalculateSha1ThumbprintBase64Url(X509Certificate2 certificate)
    {
        TokenBytesResult thumbprint = CalculateSha1Thumbprint(certificate);
        return Base64Url.Encode((thumbprint.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>
    /// Calculates the unpadded base64url SHA-256 thumbprint used by the JOSE <c>x5t#S256</c> parameter.
    /// </summary>
    /// <param name="certificate">The certificate whose DER encoding is hashed.</param>
    /// <returns>A successful result containing the base64url thumbprint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>certificate</c> is null.</exception>
    internal static TokenTextResult CalculateSha256ThumbprintBase64Url(X509Certificate2 certificate)
    {
        TokenBytesResult thumbprint = CalculateSha256Thumbprint(certificate);
        return Base64Url.Encode((thumbprint.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenKeyResult ImportCore(
        X509Certificate2 certificate,
        X509Certificate2[] certificateChain)
    {
        X509CertificateKeyInfo info = Inspect(certificate);
        if (info.Error != null)
        {
            return ImportFailure(info.Error);
        }

        TokenKey key = new TokenKey();
        key.KeyType = info.KeyType;
        key.CoseKeyType = info.CoseKeyType;
        key.Curve = info.Curve;
        key.CoseCurve = info.CoseCurve;
        key.Certificate = certificate;
        key.CertificateChain = certificateChain;
        key.CertificateSha1Thumbprint = certificate.GetCertHash(HashAlgorithmName.SHA1);
        key.CertificateSha256Thumbprint = certificate.GetCertHash(HashAlgorithmName.SHA256);
        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = certificate.HasPrivateKey;

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static X509CertificateKeyInfo Inspect(X509Certificate2 certificate)
    {
        string? oid = certificate.PublicKey.Oid.Value;
        if (string.Equals(oid, RsaPublicKeyOid, StringComparison.Ordinal)
            || string.Equals(oid, RsaPssPublicKeyOid, StringComparison.Ordinal))
        {
            using (RSA? rsa = certificate.GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    return InspectionFailure(
                        "invalid_certificate_key",
                        "The RSA certificate public key could not be imported.");
                }

                TokenKeyResult imported = RsaKeyImporterOperations.Import(rsa);
                if (!imported.Success)
                {
                    return InspectionFailure(imported.Error);
                }
            }

            X509CertificateKeyInfo rsaInfo = new X509CertificateKeyInfo();
            rsaInfo.KeyType = TokenKeyTypes.Rsa;
            rsaInfo.CoseKeyType = CoseKeyLabels.KeyTypeRsa;
            return rsaInfo;
        }

        if (string.Equals(oid, EcPublicKeyOid, StringComparison.Ordinal))
        {
            using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
            {
                if (ecdsa == null)
                {
                    return InspectionFailure(
                        "invalid_certificate_key",
                        "The ECDSA certificate public key could not be imported.");
                }

                TokenKeyResult imported = EcdsaKeyImporter.Import(ecdsa);
                if (!imported.Success)
                {
                    return InspectionFailure(imported.Error);
                }

                X509CertificateKeyInfo ecInfo = new X509CertificateKeyInfo();
                ecInfo.KeyType = (imported.Key ?? throw new InvalidOperationException("A required value is null.")).KeyType;
                ecInfo.CoseKeyType = imported.Key.CoseKeyType;
                ecInfo.Curve = imported.Key.Curve;
                ecInfo.CoseCurve = imported.Key.CoseCurve;
                return ecInfo;
            }
        }

        return InspectionFailure(
            "unsupported_certificate_key",
            "The certificate public-key algorithm is not supported.");
    }

    static TokenResult ValidateMetadata(TokenKey key, X509CertificateKeyInfo info)
    {
        if (key.KeyType != null
            && !string.Equals(key.KeyType, info.KeyType, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_type",
                "The key type does not match the certificate public key.");
        }

        if (key.CoseKeyType != 0 && key.CoseKeyType != info.CoseKeyType)
        {
            return ValidationFailure(
                "conflicting_key_type",
                "The COSE key type does not match the certificate public key.");
        }

        if (key.Curve != null
            && !string.Equals(key.Curve, info.Curve, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_curve",
                "The curve name does not match the certificate public key.");
        }

        if (key.CoseCurve != 0 && key.CoseCurve != info.CoseCurve)
        {
            return ValidationFailure(
                "conflicting_key_curve",
                "The COSE curve label does not match the certificate public key.");
        }

        if (key.NativePrivateMaterial && !(key.Certificate ?? throw new InvalidOperationException("A required value is null.")).HasPrivateKey)
        {
            return ValidationFailure(
                "missing_private_key",
                "The key declares private material that the certificate does not contain.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithms(TokenKey key, X509CertificateKeyInfo info)
    {
        if (key.Algorithm == null && !key.HasCoseAlgorithm)
        {
            return ValidationSuccess();
        }

        if (string.Equals(info.KeyType, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            using (RSA? rsa = (key.Certificate ?? throw new InvalidOperationException("A required value is null.")).GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    return ValidationFailure(
                        "invalid_certificate_key",
                        "The RSA certificate public key could not be imported.");
                }

                TokenKeyResult imported = RsaKeyImporterOperations.Import(rsa);
                if (!imported.Success)
                {
                    return ValidationFailure(imported.Error);
                }

                (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = key.Algorithm;
                imported.Key.CoseAlgorithm = key.CoseAlgorithm;
                imported.Key.HasCoseAlgorithm = key.HasCoseAlgorithm;
                return RsaKeyImporterOperations.Validate(imported.Key);
            }
        }

        using (ECDsa? ecdsa = (key.Certificate ?? throw new InvalidOperationException("A required value is null.")).GetECDsaPublicKey())
        {
            if (ecdsa == null)
            {
                return ValidationFailure(
                    "invalid_certificate_key",
                    "The ECDSA certificate public key could not be imported.");
            }

            TokenKeyResult imported = EcdsaKeyImporter.Import(ecdsa);
            if (!imported.Success)
            {
                return ValidationFailure(imported.Error);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = key.Algorithm;
            imported.Key.CoseAlgorithm = key.CoseAlgorithm;
            imported.Key.HasCoseAlgorithm = key.HasCoseAlgorithm;
            return EcdsaKeyImporter.Validate(imported.Key);
        }
    }

    static TokenKeyResult ImportFailure(string code, string message)
    {
        return ImportFailure(new TokenError(code, message, -1));
    }

    static TokenKeyResult ImportFailure(TokenError error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }

    static TokenResult ValidationFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static X509CertificateKeyInfo InspectionFailure(string code, string message)
    {
        return InspectionFailure(new TokenError(code, message, -1));
    }

    static X509CertificateKeyInfo InspectionFailure(TokenError? error)
    {
        X509CertificateKeyInfo info = new X509CertificateKeyInfo();
        info.Error = error;
        return info;
    }
}
