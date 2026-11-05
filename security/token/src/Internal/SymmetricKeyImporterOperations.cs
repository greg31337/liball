using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Imports and validates symmetric key material for JOSE and COSE algorithms.
/// </summary>
internal static class SymmetricKeyImporterOperations
{
    /// <summary>The default minimum symmetric key size in bytes.</summary>
    internal const int MinimumKeySizeInBytes = SymmetricKeyImporter.MinimumKeySizeInBytes;

    /// <summary>Imports symmetric key bytes without binding them to an algorithm.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <returns>A result containing an independent copy of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyBytes</c> is null.</exception>
    internal static TokenKeyResult Import(byte[] keyBytes)
    {
        if (keyBytes == null)
        {
            throw new ArgumentNullException("keyBytes");
        }

        TokenResult validation = ValidateLength(keyBytes.Length, MinimumKeySizeInBytes, false);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return Success(keyBytes, null, 0, false);
    }

    /// <summary>Imports symmetric key bytes for a JOSE algorithm using conservative policy.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>A result containing an independent copy of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    internal static TokenKeyResult ImportJose(byte[] keyBytes, string algorithm)
    {
        return ImportJose(keyBytes, algorithm, false);
    }

    /// <summary>Imports symmetric key bytes for a JOSE algorithm with explicit compatibility control.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <param name="allowCompatibility">Whether compatibility-only algorithms may be imported.</param>
    /// <returns>A result containing an independent copy of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either reference argument is null.</exception>
    internal static TokenKeyResult ImportJose(
        byte[] keyBytes,
        string algorithm,
        bool allowCompatibility)
    {
        if (keyBytes == null)
        {
            throw new ArgumentNullException("keyBytes");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        SymmetricKeyRequirement requirement = GetJoseRequirement(algorithm, allowCompatibility);
        if (requirement.Error != null)
        {
            return Failure(requirement.Error);
        }

        TokenResult validation = ValidateLength(
            keyBytes.Length,
            requirement.SizeInBytes,
            requirement.ExactSize);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return Success(keyBytes, algorithm, 0, false);
    }

    /// <summary>Imports symmetric key bytes for a COSE algorithm using conservative policy.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>A result containing an independent copy of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyBytes</c> is null.</exception>
    internal static TokenKeyResult ImportCose(byte[] keyBytes, int algorithm)
    {
        return ImportCose(keyBytes, algorithm, false);
    }

    /// <summary>Imports symmetric key bytes for a COSE algorithm with explicit compatibility control.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="allowCompatibility">Whether compatibility-only algorithms may be imported.</param>
    /// <returns>A result containing an independent copy of the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyBytes</c> is null.</exception>
    internal static TokenKeyResult ImportCose(
        byte[] keyBytes,
        int algorithm,
        bool allowCompatibility)
    {
        if (keyBytes == null)
        {
            throw new ArgumentNullException("keyBytes");
        }

        SymmetricKeyRequirement requirement = GetCoseRequirement(algorithm, allowCompatibility);
        if (requirement.Error != null)
        {
            return Failure(requirement.Error);
        }

        TokenResult validation = ValidateLength(
            keyBytes.Length,
            requirement.SizeInBytes,
            requirement.ExactSize);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return Success(keyBytes, null, algorithm, true);
    }

    /// <summary>Validates a symmetric key container using conservative policy.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(TokenKey key)
    {
        return Validate(key, false);
    }

    /// <summary>Validates a symmetric key container with explicit compatibility control.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <param name="allowCompatibility">Whether compatibility-only algorithms may be accepted.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(TokenKey key, bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key.KeyType != null
            && !string.Equals(key.KeyType, TokenKeyTypes.Symmetric, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "invalid_key_type",
                "The key type is not a symmetric octet-sequence key.");
        }

        if (key.CoseKeyType != 0 && key.CoseKeyType != CoseKeyLabels.KeyTypeSymmetric)
        {
            return ValidationFailure(
                "invalid_key_type",
                "The COSE key type is not Symmetric.");
        }

        if (key.SymmetricKey == null || key.SymmetricKey.Length == 0)
        {
            return ValidationFailure(
                "missing_key_material",
                "Symmetric key material is required.");
        }

        SymmetricKeyRequirement? joseRequirement = null;
        if (key.Algorithm != null)
        {
            joseRequirement = GetJoseRequirement(key.Algorithm, allowCompatibility);
            if (joseRequirement.Error != null)
            {
                return ValidationFailure(joseRequirement.Error);
            }

            TokenResult joseLength = ValidateLength(
                key.SymmetricKey.Length,
                joseRequirement.SizeInBytes,
                joseRequirement.ExactSize);
            if (!joseLength.Success)
            {
                return joseLength;
            }
        }

        SymmetricKeyRequirement? coseRequirement = null;
        if (key.HasCoseAlgorithm)
        {
            coseRequirement = GetCoseRequirement(key.CoseAlgorithm, allowCompatibility);
            if (coseRequirement.Error != null)
            {
                return ValidationFailure(coseRequirement.Error);
            }

            TokenResult coseLength = ValidateLength(
                key.SymmetricKey.Length,
                coseRequirement.SizeInBytes,
                coseRequirement.ExactSize);
            if (!coseLength.Success)
            {
                return coseLength;
            }
        }

        if (key.Algorithm != null && key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(key.Algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The JOSE and COSE algorithm identifiers are not equivalent.");
            }
        }

        if (joseRequirement == null && coseRequirement == null)
        {
            return ValidateLength(
                key.SymmetricKey.Length,
                MinimumKeySizeInBytes,
                false);
        }

        return ValidationSuccess();
    }

    /// <summary>Zeroes and releases symmetric material held by a key container.</summary>
    /// <param name="key">The key whose symmetric bytes are cleared.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static void Clear(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key.SymmetricKey != null)
        {
            CryptographicOperations.ZeroMemory(key.SymmetricKey);
            key.SymmetricKey = null;
        }
    }

    static SymmetricKeyRequirement GetJoseRequirement(
        string algorithm,
        bool allowCompatibility)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return RequirementFailure(
                "unknown_algorithm",
                "The JOSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return RequirementFailure(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited and cannot be used for key import.");
        }

        if (string.Equals(algorithm, JoseAlgorithms.HmacSha256, StringComparison.Ordinal))
        {
            return Requirement(32, false);
        }

        if (string.Equals(algorithm, JoseAlgorithms.HmacSha384, StringComparison.Ordinal))
        {
            return Requirement(48, false);
        }

        if (string.Equals(algorithm, JoseAlgorithms.HmacSha512, StringComparison.Ordinal))
        {
            return Requirement(64, false);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes128KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128Gcm, StringComparison.Ordinal))
        {
            return Requirement(16, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes192KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192Gcm, StringComparison.Ordinal))
        {
            return Requirement(24, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes256KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256Gcm, StringComparison.Ordinal))
        {
            return Requirement(32, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes128CbcHmacSha256, StringComparison.Ordinal))
        {
            return Requirement(32, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes192CbcHmacSha384, StringComparison.Ordinal))
        {
            return Requirement(48, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes256CbcHmacSha512, StringComparison.Ordinal))
        {
            return Requirement(64, true);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            return Requirement(MinimumKeySizeInBytes, false);
        }

        if (string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
                StringComparison.Ordinal))
        {
            return Requirement(MinimumKeySizeInBytes, false);
        }

        return RequirementFailure(
            "invalid_key_algorithm",
            "The JOSE algorithm does not directly use a symmetric key.");
    }

    static SymmetricKeyRequirement GetCoseRequirement(int algorithm, bool allowCompatibility)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(algorithm);
        if (!metadata.Known)
        {
            return RequirementFailure(
                "unknown_algorithm",
                "The COSE algorithm is not registered.");
        }

        bool compatibilitySymmetric = algorithm == CoseAlgorithms.Aes128Ctr
            || algorithm == CoseAlgorithms.Aes192Ctr
            || algorithm == CoseAlgorithms.Aes256Ctr
            || algorithm == CoseAlgorithms.Aes128Cbc
            || algorithm == CoseAlgorithms.Aes192Cbc
            || algorithm == CoseAlgorithms.Aes256Cbc;
        if (compatibilitySymmetric && !allowCompatibility)
        {
            return RequirementFailure(
                "compatibility_required",
                "The COSE algorithm requires explicit compatibility opt-in.");
        }

        if (algorithm == CoseAlgorithms.HmacSha256Truncated64
            || algorithm == CoseAlgorithms.HmacSha256)
        {
            return Requirement(32, false);
        }

        if (algorithm == CoseAlgorithms.HmacSha384)
        {
            return Requirement(48, false);
        }

        if (algorithm == CoseAlgorithms.HmacSha512)
        {
            return Requirement(64, false);
        }

        if (algorithm == CoseAlgorithms.Aes128KeyWrap
            || algorithm == CoseAlgorithms.Aes128Gcm
            || algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_128
            || algorithm == CoseAlgorithms.AesMac128_64
            || algorithm == CoseAlgorithms.AesMac128_128
            || algorithm == CoseAlgorithms.Aes128Ctr
            || algorithm == CoseAlgorithms.Aes128Cbc)
        {
            return Requirement(16, true);
        }

        if (algorithm == CoseAlgorithms.Aes192KeyWrap
            || algorithm == CoseAlgorithms.Aes192Gcm
            || algorithm == CoseAlgorithms.Aes192Ctr
            || algorithm == CoseAlgorithms.Aes192Cbc)
        {
            return Requirement(24, true);
        }

        if (algorithm == CoseAlgorithms.Aes256KeyWrap
            || algorithm == CoseAlgorithms.Aes256Gcm
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm64_64_256
            || algorithm == CoseAlgorithms.AesCcm16_128_256
            || algorithm == CoseAlgorithms.AesCcm64_128_256
            || algorithm == CoseAlgorithms.AesMac256_64
            || algorithm == CoseAlgorithms.AesMac256_128
            || algorithm == CoseAlgorithms.ChaCha20Poly1305
            || algorithm == CoseAlgorithms.Aes256Ctr
            || algorithm == CoseAlgorithms.Aes256Cbc)
        {
            return Requirement(32, true);
        }

        if (algorithm == CoseAlgorithms.Direct)
        {
            return Requirement(MinimumKeySizeInBytes, false);
        }

        if (algorithm == CoseAlgorithms.DirectHkdfAes128)
        {
            return Requirement(16, false);
        }

        if (algorithm == CoseAlgorithms.DirectHkdfAes256
            || algorithm == CoseAlgorithms.DirectHkdfSha256)
        {
            return Requirement(32, false);
        }

        if (algorithm == CoseAlgorithms.DirectHkdfSha512)
        {
            return Requirement(64, false);
        }

        return RequirementFailure(
            "invalid_key_algorithm",
            "The COSE algorithm does not directly use a symmetric key.");
    }

    static TokenResult ValidateLength(int actual, int required, bool exact)
    {
        if (exact && actual != required)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The symmetric key must have the exact size required by the algorithm.");
        }

        if (!exact && actual < required)
        {
            return ValidationFailure(
                "weak_key",
                "The symmetric key is shorter than the required minimum size.");
        }

        return ValidationSuccess();
    }

    static TokenKeyResult Success(
        byte[] keyBytes,
        string? joseAlgorithm,
        int coseAlgorithm,
        bool hasCoseAlgorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        key.Algorithm = joseAlgorithm;
        key.CoseAlgorithm = coseAlgorithm;
        key.HasCoseAlgorithm = hasCoseAlgorithm;
        key.SymmetricKey = new byte[keyBytes.Length];
        keyBytes.CopyTo(key.SymmetricKey, 0);

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

    static TokenResult ValidationFailure(TokenError error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static SymmetricKeyRequirement Requirement(int sizeInBytes, bool exactSize)
    {
        SymmetricKeyRequirement requirement = new SymmetricKeyRequirement();
        requirement.SizeInBytes = sizeInBytes;
        requirement.ExactSize = exactSize;
        return requirement;
    }

    static SymmetricKeyRequirement RequirementFailure(string code, string message)
    {
        SymmetricKeyRequirement requirement = new SymmetricKeyRequirement();
        requirement.Error = new TokenError(code, message, -1);
        return requirement;
    }
}
