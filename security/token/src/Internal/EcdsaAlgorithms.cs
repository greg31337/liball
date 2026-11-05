using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Creates and verifies JWS ECDSA signatures for ES256, ES384, and ES512.
/// </summary>
internal static class EcdsaAlgorithms
{
    /// <summary>Creates a fixed-width JOSE ECDSA signature over a byte array.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The ECDSA signing key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE ECDSA algorithm name.</param>
    /// <returns>A result containing the raw <c>r || s</c> signature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Sign(byte[] data, TokenKey key, string algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        return Sign(data.AsSpan(), key, algorithm);
    }

    /// <summary>Creates a fixed-width JOSE ECDSA signature over a byte span.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The ECDSA signing key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE ECDSA algorithm name.</param>
    /// <returns>A result containing the raw <c>r || s</c> signature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>algorithm</c> is null.</exception>
    internal static TokenBytesResult Sign(
        ReadOnlySpan<byte> data,
        TokenKey key,
        string algorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult validation = Validate(key, algorithm, true);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        byte[] der = (key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).SignData(
            data,
            GetHashAlgorithm(algorithm),
            DSASignatureFormat.Rfc3279DerSequence);
        TokenBytesResult raw = EcdsaSignatureCodec.DerToRaw(
            der,
            GetComponentWidth(algorithm));
        CryptographicOperations.ZeroMemory(der);
        return raw;
    }

    /// <summary>Verifies a fixed-width JOSE ECDSA signature over a byte array.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The raw <c>r || s</c> signature to verify.</param>
    /// <param name="key">The ECDSA verification key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE ECDSA algorithm name.</param>
    /// <returns>A successful result when the signature is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Verify(
        byte[] data,
        byte[] signature,
        TokenKey key,
        string algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        return Verify(data.AsSpan(), signature.AsSpan(), key, algorithm);
    }

    /// <summary>Verifies a fixed-width JOSE ECDSA signature over byte spans.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The raw <c>r || s</c> signature to verify.</param>
    /// <param name="key">The ECDSA verification key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE ECDSA algorithm name.</param>
    /// <returns>A successful result when the signature is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>algorithm</c> is null.</exception>
    internal static TokenResult Verify(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> signature,
        TokenKey key,
        string algorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult validation = Validate(key, algorithm, false);
        if (!validation.Success)
        {
            return validation;
        }

        TokenBytesResult der = EcdsaSignatureCodec.RawToDer(
            signature,
            GetComponentWidth(algorithm));
        if (!der.Success)
        {
            return Failure(
                "signature_mismatch",
                "The ECDSA signature does not match the signed data.");
        }

        bool verified = (key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).VerifyData(
            data,
            der.Bytes,
            GetHashAlgorithm(algorithm),
            DSASignatureFormat.Rfc3279DerSequence);
        CryptographicOperations.ZeroMemory(der.Bytes);
        if (!verified)
        {
            return Failure(
                "signature_mismatch",
                "The ECDSA signature does not match the signed data.");
        }

        return Success();
    }

    static TokenResult Validate(TokenKey key, string algorithm, bool signing)
    {
        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return algorithmValidation;
        }

        TokenResult keyValidation = EcdsaKeyImporter.Validate(key, signing, false);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if ((key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).KeySize != GetCurveSize(algorithm))
        {
            return Failure(
                "algorithm_curve_mismatch",
                "The ECDSA curve does not match the requested JOSE algorithm.");
        }

        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return Failure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested ECDSA algorithm.");
        }

        if (key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return Failure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm does not match the requested ECDSA algorithm.");
            }
        }

        if (key.Use != null
            && !string.Equals(key.Use, TokenKeyUses.Signature, StringComparison.Ordinal))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The key use does not permit ECDSA signing or verification.");
        }

        string operation = signing
            ? TokenKeyOperations.Sign
            : TokenKeyOperations.Verify;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(key.KeyOperations, operation, false))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested ECDSA operation is not permitted by the key metadata.");
        }

        int coseOperation = signing
            ? CoseKeyLabels.OperationSign
            : CoseKeyLabels.OperationVerify;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(key.CoseKeyOperations, coseOperation))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested ECDSA operation is not permitted by the COSE key metadata.");
        }

        return Success();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha512, StringComparison.Ordinal))
        {
            return Success();
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return Failure(
            "invalid_algorithm",
            "The JOSE algorithm is not an implemented ECDSA signing algorithm.");
    }

    static HashAlgorithmName GetHashAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA256;
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static int GetComponentWidth(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal))
        {
            return 32;
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal))
        {
            return 48;
        }

        return 66;
    }

    static int GetCurveSize(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal))
        {
            return 256;
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal))
        {
            return 384;
        }

        return 521;
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
