using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Creates and verifies RSA signatures for supported JOSE algorithms.
/// </summary>
internal static class RsaAlgorithms
{
    /// <summary>Creates an RS256, RS384, RS512, PS256, PS384, or PS512 signature over a byte array.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The RSA signing key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE RSA algorithm name.</param>
    /// <returns>A result containing the signature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Sign(byte[] data, TokenKey key, string algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        return Sign(data.AsSpan(), key, algorithm);
    }

    /// <summary>Creates an RS256, RS384, RS512, PS256, PS384, or PS512 signature over a byte span.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The RSA signing key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE RSA algorithm name.</param>
    /// <returns>A result containing the signature or stable failure details.</returns>
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

        byte[] signature = (key.Rsa ?? throw new InvalidOperationException("A required value is null.")).SignData(
            data,
            GetHashAlgorithm(algorithm),
            GetSignaturePadding(algorithm));
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = signature;
        return result;
    }

    /// <summary>Verifies an RS256, RS384, RS512, PS256, PS384, or PS512 signature over a byte array.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The RSA signature to verify.</param>
    /// <param name="key">The RSA verification key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE RSA algorithm name.</param>
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

    /// <summary>Verifies an RS256, RS384, RS512, PS256, PS384, or PS512 signature over byte spans.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The RSA signature to verify.</param>
    /// <param name="key">The RSA verification key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JOSE RSA algorithm name.</param>
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

        bool verified = (key.Rsa ?? throw new InvalidOperationException("A required value is null.")).VerifyData(
            data,
            signature,
            GetHashAlgorithm(algorithm),
            GetSignaturePadding(algorithm));
        if (!verified)
        {
            return Failure(
                "signature_mismatch",
                "The RSA signature does not match the signed data.");
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

        TokenResult keyValidation = RsaKeyImporterOperations.Validate(key, signing, false);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return Failure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested RSA algorithm.");
        }

        if (key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return Failure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm does not match the requested RSA algorithm.");
            }
        }

        if (key.Use != null
            && !string.Equals(key.Use, TokenKeyUses.Signature, StringComparison.Ordinal))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The key use does not permit RSA signing or verification.");
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
                "The requested RSA operation is not permitted by the key metadata.");
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
                "The requested RSA operation is not permitted by the COSE key metadata.");
        }

        return Success();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal))
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
            "The JOSE algorithm is not an implemented RSA signing algorithm.");
    }

    static HashAlgorithmName GetHashAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA256;
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static RSASignaturePadding GetSignaturePadding(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal))
        {
            return RSASignaturePadding.Pss;
        }

        return RSASignaturePadding.Pkcs1;
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
