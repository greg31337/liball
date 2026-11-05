using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Imports and validates native RSA keys for JOSE and COSE algorithms.
/// </summary>
internal static class RsaKeyImporterOperations
{
    /// <summary>The default minimum accepted RSA modulus size in bits.</summary>
    internal const int MinimumKeySizeInBits = RsaKeyImporter.MinimumKeySizeInBits;

    /// <summary>Imports a native RSA key for public operations.</summary>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <returns>A result containing a key container that references <c>rsa</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rsa</c> is null.</exception>
    internal static TokenKeyResult Import(RSA rsa)
    {
        return Import(rsa, false);
    }

    /// <summary>Imports a native RSA key with an explicit private-material declaration.</summary>
    /// <remarks>
    /// The returned container does not own or dispose <c>rsa</c>. The declaration is required
    /// because native RSA implementations do not provide a portable exception-free private-key probe.
    /// </remarks>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing a key container that references <c>rsa</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rsa</c> is null.</exception>
    internal static TokenKeyResult Import(RSA rsa, bool privateMaterial)
    {
        if (rsa == null)
        {
            throw new ArgumentNullException("rsa");
        }

        TokenResult size = ValidateSize(rsa.KeySize);
        if (!size.Success)
        {
            return Failure(size.Error);
        }

        return Success(rsa, privateMaterial, null, 0, false);
    }

    /// <summary>Imports an RSA key for a JOSE algorithm using conservative policy.</summary>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either reference argument is null.</exception>
    internal static TokenKeyResult ImportJose(
        RSA rsa,
        string algorithm,
        bool privateMaterial)
    {
        return ImportJose(rsa, algorithm, privateMaterial, false);
    }

    /// <summary>Imports an RSA key for a JOSE algorithm with explicit compatibility control.</summary>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <param name="allowCompatibility">Whether compatibility-only RSA algorithms may be imported.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either reference argument is null.</exception>
    internal static TokenKeyResult ImportJose(
        RSA rsa,
        string algorithm,
        bool privateMaterial,
        bool allowCompatibility)
    {
        if (rsa == null)
        {
            throw new ArgumentNullException("rsa");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult algorithmValidation = ValidateJoseAlgorithm(
            algorithm,
            allowCompatibility);
        if (!algorithmValidation.Success)
        {
            return Failure(algorithmValidation.Error);
        }

        TokenResult size = ValidateSize(rsa.KeySize);
        if (!size.Success)
        {
            return Failure(size.Error);
        }

        return Success(rsa, privateMaterial, algorithm, 0, false);
    }

    /// <summary>Imports an RSA key for a COSE algorithm using conservative policy.</summary>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rsa</c> is null.</exception>
    internal static TokenKeyResult ImportCose(RSA rsa, int algorithm, bool privateMaterial)
    {
        return ImportCose(rsa, algorithm, privateMaterial, false);
    }

    /// <summary>Imports an RSA key for a COSE algorithm with explicit compatibility control.</summary>
    /// <param name="rsa">The caller-owned RSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <param name="allowCompatibility">Whether compatibility-only RSA algorithms may be imported.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rsa</c> is null.</exception>
    internal static TokenKeyResult ImportCose(
        RSA rsa,
        int algorithm,
        bool privateMaterial,
        bool allowCompatibility)
    {
        if (rsa == null)
        {
            throw new ArgumentNullException("rsa");
        }

        TokenResult algorithmValidation = ValidateCoseAlgorithm(
            algorithm,
            allowCompatibility);
        if (!algorithmValidation.Success)
        {
            return Failure(algorithmValidation.Error);
        }

        TokenResult size = ValidateSize(rsa.KeySize);
        if (!size.Success)
        {
            return Failure(size.Error);
        }

        return Success(rsa, privateMaterial, null, algorithm, true);
    }

    /// <summary>Validates an RSA key container for public operations using conservative policy.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(TokenKey key)
    {
        return Validate(key, false, false);
    }

    /// <summary>Validates an RSA key container with material and compatibility requirements.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <param name="requirePrivateMaterial">Whether private-operation capability is required.</param>
    /// <param name="allowCompatibility">Whether compatibility-only RSA algorithms may be accepted.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(
        TokenKey key,
        bool requirePrivateMaterial,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key.KeyType != null
            && !string.Equals(key.KeyType, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            return FailureResult(
                "invalid_key_type",
                "The key type is not RSA.");
        }

        if (key.CoseKeyType != 0 && key.CoseKeyType != CoseKeyLabels.KeyTypeRsa)
        {
            return FailureResult(
                "invalid_key_type",
                "The COSE key type is not RSA.");
        }

        if (key.Rsa == null)
        {
            return FailureResult(
                "missing_key_material",
                "A native RSA key is required.");
        }

        TokenResult size = ValidateSize(key.Rsa.KeySize);
        if (!size.Success)
        {
            return size;
        }

        if (requirePrivateMaterial && !key.NativePrivateMaterial)
        {
            return FailureResult(
                "missing_private_key",
                "The RSA key is not declared to contain private material.");
        }

        if (key.Algorithm != null)
        {
            TokenResult joseValidation = ValidateJoseAlgorithm(
                key.Algorithm,
                allowCompatibility);
            if (!joseValidation.Success)
            {
                return joseValidation;
            }
        }

        if (key.HasCoseAlgorithm)
        {
            TokenResult coseValidation = ValidateCoseAlgorithm(
                key.CoseAlgorithm,
                allowCompatibility);
            if (!coseValidation.Success)
            {
                return coseValidation;
            }
        }

        if (key.Algorithm != null && key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(key.Algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return FailureResult(
                    "conflicting_key_algorithm",
                    "The JOSE and COSE algorithm identifiers are not equivalent.");
            }
        }

        return SuccessResult();
    }

    static TokenResult ValidateJoseAlgorithm(string algorithm, bool allowCompatibility)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return FailureResult(
                "unknown_algorithm",
                "The JOSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return FailureResult(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited and cannot be used for RSA key import.");
        }

        bool isRsa = string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaep, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha512, StringComparison.Ordinal);
        if (!isRsa)
        {
            return FailureResult(
                "invalid_key_algorithm",
                "The JOSE algorithm does not use an RSA key.");
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal)
            && !allowCompatibility)
        {
            return FailureResult(
                "compatibility_required",
                "RSA1_5 requires explicit compatibility opt-in.");
        }

        return SuccessResult();
    }

    static TokenResult ValidateCoseAlgorithm(int algorithm, bool allowCompatibility)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(algorithm);
        if (!metadata.Known)
        {
            return FailureResult(
                "unknown_algorithm",
                "The COSE algorithm is not registered.");
        }

        bool isRsa = algorithm == CoseAlgorithms.RsaSha1
            || algorithm == CoseAlgorithms.RsaSha256
            || algorithm == CoseAlgorithms.RsaSha384
            || algorithm == CoseAlgorithms.RsaSha512
            || algorithm == CoseAlgorithms.RsaPssSha256
            || algorithm == CoseAlgorithms.RsaPssSha384
            || algorithm == CoseAlgorithms.RsaPssSha512
            || algorithm == CoseAlgorithms.RsaOaep
            || algorithm == CoseAlgorithms.RsaOaepSha256
            || algorithm == CoseAlgorithms.RsaOaepSha512;
        if (!isRsa)
        {
            return FailureResult(
                "invalid_key_algorithm",
                "The COSE algorithm does not use an RSA key.");
        }

        if (algorithm == CoseAlgorithms.RsaSha1 && !allowCompatibility)
        {
            return FailureResult(
                "compatibility_required",
                "RS1 requires explicit compatibility opt-in.");
        }

        return SuccessResult();
    }

    static TokenResult ValidateSize(int keySize)
    {
        if (keySize < MinimumKeySizeInBits)
        {
            return FailureResult(
                "weak_key",
                "The RSA modulus is shorter than the required minimum size.");
        }

        return SuccessResult();
    }

    static TokenKeyResult Success(
        RSA rsa,
        bool privateMaterial,
        string? joseAlgorithm,
        int coseAlgorithm,
        bool hasCoseAlgorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Rsa;
        key.CoseKeyType = CoseKeyLabels.KeyTypeRsa;
        key.Algorithm = joseAlgorithm;
        key.CoseAlgorithm = coseAlgorithm;
        key.HasCoseAlgorithm = hasCoseAlgorithm;
        key.Rsa = rsa;
        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = privateMaterial;

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static TokenKeyResult Failure(TokenError? error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    static TokenResult SuccessResult()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult FailureResult(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
