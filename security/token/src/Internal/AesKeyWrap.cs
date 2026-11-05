using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Wraps and unwraps key material with the RFC 3394 AES Key Wrap algorithm.
/// </summary>
internal static class AesKeyWrap
{
    /// <summary>Wraps key material using A128KW, A192KW, or A256KW.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES Key Wrap algorithm name.</param>
    /// <returns>A result containing the wrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Wrap(
        byte[] keyMaterial,
        TokenKey keyEncryptionKey,
        string algorithm)
    {
        if (keyMaterial == null)
        {
            throw new ArgumentNullException("keyMaterial");
        }

        return Wrap(keyMaterial.AsSpan(), keyEncryptionKey, algorithm);
    }

    /// <summary>Wraps a key-material span using A128KW, A192KW, or A256KW.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES Key Wrap algorithm name.</param>
    /// <returns>A result containing the wrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> or <c>algorithm</c> is null.</exception>
    internal static TokenBytesResult Wrap(
        ReadOnlySpan<byte> keyMaterial,
        TokenKey keyEncryptionKey,
        string algorithm)
    {
        if (keyEncryptionKey == null)
        {
            throw new ArgumentNullException("keyEncryptionKey");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult validation = Validate(keyEncryptionKey, algorithm, true);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (keyMaterial.Length < 16 || keyMaterial.Length % 8 != 0)
        {
            return Failure(
                "invalid_key_size",
                "AES Key Wrap input must contain at least two 64-bit blocks.");
        }

        int blockCount = keyMaterial.Length / 8;
        byte[] result = new byte[keyMaterial.Length + 8];
        Span<byte> accumulator = result.AsSpan(0, 8);
        accumulator.Fill(166);
        keyMaterial.CopyTo(result.AsSpan(8));
        byte[] block = new byte[16];

        using (Aes aes = Aes.Create())
        {
            aes.Key = (keyEncryptionKey.SymmetricKey ?? throw new InvalidOperationException("A required value is null."));
            int round = 0;
            while (round < 6)
            {
                int index = 1;
                while (index <= blockCount)
                {
                    accumulator.CopyTo(block.AsSpan(0, 8));
                    result.AsSpan(index * 8, 8).CopyTo(block.AsSpan(8, 8));
                    byte[] encrypted = aes.EncryptEcb(block, PaddingMode.None);
                    encrypted.AsSpan(0, 8).CopyTo(accumulator);
                    XorCounter(accumulator, (long)blockCount * round + index);
                    encrypted.AsSpan(8, 8).CopyTo(result.AsSpan(index * 8, 8));
                    CryptographicOperations.ZeroMemory(encrypted);
                    index += 1;
                }

                round += 1;
            }
        }

        CryptographicOperations.ZeroMemory(block);
        TokenBytesResult wrapped = new TokenBytesResult();
        wrapped.Success = true;
        wrapped.Bytes = result;
        return wrapped;
    }

    /// <summary>Unwraps key material using A128KW, A192KW, or A256KW.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES Key Wrap algorithm name.</param>
    /// <returns>A result containing the unwrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Unwrap(
        byte[] wrappedKey,
        TokenKey keyEncryptionKey,
        string algorithm)
    {
        if (wrappedKey == null)
        {
            throw new ArgumentNullException("wrappedKey");
        }

        return Unwrap(wrappedKey.AsSpan(), keyEncryptionKey, algorithm);
    }

    /// <summary>Unwraps a key-material span using A128KW, A192KW, or A256KW.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES Key Wrap algorithm name.</param>
    /// <returns>A result containing the unwrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> or <c>algorithm</c> is null.</exception>
    internal static TokenBytesResult Unwrap(
        ReadOnlySpan<byte> wrappedKey,
        TokenKey keyEncryptionKey,
        string algorithm)
    {
        if (keyEncryptionKey == null)
        {
            throw new ArgumentNullException("keyEncryptionKey");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult validation = Validate(keyEncryptionKey, algorithm, false);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (wrappedKey.Length < 24 || wrappedKey.Length % 8 != 0)
        {
            return UnwrapFailure();
        }

        int blockCount = wrappedKey.Length / 8 - 1;
        byte[] accumulator = wrappedKey.Slice(0, 8).ToArray();
        byte[] result = wrappedKey.Slice(8).ToArray();
        byte[] block = new byte[16];

        using (Aes aes = Aes.Create())
        {
            aes.Key = (keyEncryptionKey.SymmetricKey ?? throw new InvalidOperationException("A required value is null."));
            int round = 5;
            while (round >= 0)
            {
                int index = blockCount;
                while (index >= 1)
                {
                    accumulator.CopyTo(block, 0);
                    XorCounter(block.AsSpan(0, 8), (long)blockCount * round + index);
                    result.AsSpan((index - 1) * 8, 8).CopyTo(block.AsSpan(8, 8));
                    byte[] decrypted = aes.DecryptEcb(block, PaddingMode.None);
                    decrypted.AsSpan(0, 8).CopyTo(accumulator);
                    decrypted.AsSpan(8, 8).CopyTo(result.AsSpan((index - 1) * 8, 8));
                    CryptographicOperations.ZeroMemory(decrypted);
                    index -= 1;
                }

                round -= 1;
            }
        }

        CryptographicOperations.ZeroMemory(block);
        byte[] expected = new byte[8];
        expected.AsSpan().Fill(166);
        bool valid = ConstantTime.AreEqual(accumulator, expected);
        CryptographicOperations.ZeroMemory(accumulator);
        CryptographicOperations.ZeroMemory(expected);
        if (!valid)
        {
            CryptographicOperations.ZeroMemory(result);
            return UnwrapFailure();
        }

        TokenBytesResult unwrapped = new TokenBytesResult();
        unwrapped.Success = true;
        unwrapped.Bytes = result;
        return unwrapped;
    }

    static TokenResult Validate(
        TokenKey keyEncryptionKey,
        string algorithm,
        bool wrapping)
    {
        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return algorithmValidation;
        }

        TokenResult keyValidation = SymmetricKeyImporterOperations.Validate(keyEncryptionKey);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        int keySize = GetKeySize(algorithm);
        if ((keyEncryptionKey.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length != keySize)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The AES key-encryption key does not match the algorithm size.");
        }

        if (keyEncryptionKey.Algorithm != null
            && !string.Equals(
                keyEncryptionKey.Algorithm,
                algorithm,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested AES Key Wrap algorithm.");
        }

        if (keyEncryptionKey.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, keyEncryptionKey.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm does not match the requested AES Key Wrap algorithm.");
            }
        }

        if (keyEncryptionKey.Use != null
            && !string.Equals(
                keyEncryptionKey.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit AES key wrapping or unwrapping.");
        }

        string operation = wrapping
            ? TokenKeyOperations.WrapKey
            : TokenKeyOperations.UnwrapKey;
        if (keyEncryptionKey.KeyOperations != null
            && keyEncryptionKey.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                keyEncryptionKey.KeyOperations,
                operation,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES key operation is not permitted by the key metadata.");
        }

        int coseOperation = wrapping
            ? CoseKeyLabels.OperationWrapKey
            : CoseKeyLabels.OperationUnwrapKey;
        if (keyEncryptionKey.CoseKeyOperations != null
            && keyEncryptionKey.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                keyEncryptionKey.CoseKeyOperations,
                coseOperation))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES key operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Aes128KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256KeyWrap, StringComparison.Ordinal))
        {
            return ValidationSuccess();
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return ValidationFailure(
            "invalid_algorithm",
            "The JOSE algorithm is not an AES Key Wrap algorithm.");
    }

    static int GetKeySize(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Aes128KeyWrap, StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes192KeyWrap, StringComparison.Ordinal))
        {
            return 24;
        }

        return 32;
    }

    static void XorCounter(Span<byte> accumulator, long counter)
    {
        int index = 7;
        while (counter != 0)
        {
            accumulator[index] ^= (byte)(counter & 255);
            counter >>= 8;
            index -= 1;
        }
    }

    static TokenBytesResult UnwrapFailure()
    {
        return Failure(
            "unwrap_failed",
            "The wrapped key could not be authenticated and unwrapped.");
    }

    static TokenBytesResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
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
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
