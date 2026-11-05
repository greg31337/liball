using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Creates and verifies RFC 9053 COSE HMAC authentication tags.</summary>
internal static class CoseHmacAlgorithms
{
    /// <summary>Creates a COSE HMAC authentication tag over a byte array.</summary>
    /// <param name="data">The bytes to authenticate.</param>
    /// <param name="key">The symmetric MAC key and its metadata.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>The complete or registered truncated tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] data,
        TokenKey key,
        int algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        return Create(data.AsSpan(), key, algorithm);
    }

    /// <summary>Creates a COSE HMAC authentication tag over a byte span.</summary>
    /// <param name="data">The bytes to authenticate.</param>
    /// <param name="key">The symmetric MAC key and its metadata.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>The complete or registered truncated tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult Create(
        ReadOnlySpan<byte> data,
        TokenKey key,
        int algorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenResult validation = Validate(key, algorithm, true);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        byte[] complete = Compute(data, key.SymmetricKey, algorithm);
        int tagLength = GetTagLength(algorithm);
        byte[] tag;
        if (complete.Length == tagLength)
        {
            tag = complete;
        }
        else
        {
            tag = new byte[tagLength];
            complete.AsSpan(0, tagLength).CopyTo(tag);
            CryptographicOperations.ZeroMemory(complete);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = tag;
        return result;
    }

    /// <summary>Verifies a COSE HMAC authentication tag over a byte array.</summary>
    /// <param name="data">The authenticated bytes.</param>
    /// <param name="tag">The complete or registered truncated tag.</param>
    /// <param name="key">The symmetric MAC key and its metadata.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>A successful result when the tag is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenResult Verify(
        byte[] data,
        byte[] tag,
        TokenKey key,
        int algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (tag == null)
        {
            throw new ArgumentNullException("tag");
        }

        return Verify(
            data.AsSpan(),
            tag.AsSpan(),
            key,
            algorithm);
    }

    /// <summary>Verifies a COSE HMAC authentication tag over byte spans.</summary>
    /// <param name="data">The authenticated bytes.</param>
    /// <param name="tag">The complete or registered truncated tag.</param>
    /// <param name="key">The symmetric MAC key and its metadata.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>A successful result when the tag is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Verify(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> tag,
        TokenKey key,
        int algorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenResult validation = Validate(key, algorithm, false);
        if (!validation.Success)
        {
            return validation;
        }

        byte[] complete = Compute(data, key.SymmetricKey, algorithm);
        int tagLength = GetTagLength(algorithm);
        bool equal = tag.Length == tagLength
            && ConstantTime.AreEqual(
                tag,
                complete.AsSpan(0, tagLength));
        CryptographicOperations.ZeroMemory(complete);
        if (!equal)
        {
            return Failure(
                "mac_mismatch",
                "The COSE HMAC tag does not match the authenticated data.");
        }

        return Success();
    }

    static TokenResult Validate(
        TokenKey key,
        int algorithm,
        bool creating)
    {
        TokenResult algorithmResult = ValidateAlgorithm(algorithm);
        if (!algorithmResult.Success)
        {
            return algorithmResult;
        }

        TokenResult keyResult = SymmetricKeyImporterOperations.Validate(key);
        if (!keyResult.Success)
        {
            return keyResult;
        }

        if (key.Algorithm != null)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(
                key.Algorithm);
            if (!RegistryLookup.Contains(labels, algorithm))
            {
                return Failure(
                    "conflicting_key_algorithm",
                    "The JOSE key algorithm does not match the requested COSE HMAC algorithm.");
            }
        }

        if (key.HasCoseAlgorithm
            && key.CoseAlgorithm != algorithm)
        {
            return Failure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match the requested HMAC algorithm.");
        }

        int minimumSize = GetMinimumKeySize(algorithm);
        if ((key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length < minimumSize)
        {
            return Failure(
                "weak_key",
                "The symmetric key is shorter than the HMAC hash output size.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Signature,
                StringComparison.Ordinal))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The key use does not permit HMAC creation or verification.");
        }

        string operation = creating
            ? TokenKeyOperations.MacCreate
            : TokenKeyOperations.MacVerify;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                operation,
                false))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested HMAC operation is not permitted by the key metadata.");
        }

        int coseOperation = creating
            ? CoseKeyLabels.OperationMacCreate
            : CoseKeyLabels.OperationMacVerify;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                coseOperation))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested HMAC operation is not permitted by the COSE key metadata.");
        }

        return Success();
    }

    static TokenResult ValidateAlgorithm(int algorithm)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (algorithm != CoseAlgorithms.HmacSha256Truncated64
            && algorithm != CoseAlgorithms.HmacSha256
            && algorithm != CoseAlgorithms.HmacSha384
            && algorithm != CoseAlgorithms.HmacSha512)
        {
            return Failure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented HMAC algorithm.");
        }

        return Success();
    }

    static byte[] Compute(
        ReadOnlySpan<byte> data,
        byte[]? key,
        int algorithm)
    {
        if (algorithm == CoseAlgorithms.HmacSha256Truncated64
            || algorithm == CoseAlgorithms.HmacSha256)
        {
            return HMACSHA256.HashData(key, data);
        }

        if (algorithm == CoseAlgorithms.HmacSha384)
        {
            return HMACSHA384.HashData(key, data);
        }

        return HMACSHA512.HashData(key, data);
    }

    static int GetTagLength(int algorithm)
    {
        if (algorithm == CoseAlgorithms.HmacSha256Truncated64)
        {
            return 8;
        }

        return GetMinimumKeySize(algorithm);
    }

    static int GetMinimumKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.HmacSha256Truncated64
            || algorithm == CoseAlgorithms.HmacSha256)
        {
            return 32;
        }

        if (algorithm == CoseAlgorithms.HmacSha384)
        {
            return 48;
        }

        return 64;
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
