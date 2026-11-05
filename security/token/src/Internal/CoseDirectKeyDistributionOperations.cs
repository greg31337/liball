using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Applies COSE direct and direct-with-HKDF content key distribution.</summary>
internal static class CoseDirectKeyDistributionOperations
{
    /// <summary>The maximum derived-key size accepted by this implementation.</summary>
    internal const int MaximumKeySizeInBits = CoseDirectKeyDistribution.MaximumKeySizeInBits;

    /// <summary>Obtains a content key for COSE encryption.</summary>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="algorithm">The COSE direct recipient algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when PartyU provides a nonce.</param>
    /// <param name="parameters">The content algorithm and COSE KDF context inputs.</param>
    /// <returns>The copied or derived content key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>parameters</c> is null.</exception>
    internal static CoseDirectKeyDistributionResult Encrypt(
        TokenKey key,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        return Distribute(
            key,
            algorithm,
            salt,
            parameters,
            true);
    }

    /// <summary>Obtains a content key for COSE decryption.</summary>
    /// <param name="encryptedKey">The COSE recipient ciphertext, which must be empty.</param>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="algorithm">The COSE direct recipient algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when PartyU provides a nonce.</param>
    /// <param name="parameters">The content algorithm and COSE KDF context inputs.</param>
    /// <returns>The copied or derived content key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument other than <c>salt</c> is null.</exception>
    internal static CoseDirectKeyDistributionResult Decrypt(
        byte[] encryptedKey,
        TokenKey key,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        return Decrypt(
            encryptedKey.AsSpan(),
            key,
            algorithm,
            salt,
            parameters);
    }

    /// <summary>Obtains a content key using a COSE recipient ciphertext span.</summary>
    /// <param name="encryptedKey">The COSE recipient ciphertext, which must be empty.</param>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="algorithm">The COSE direct recipient algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when PartyU provides a nonce.</param>
    /// <param name="parameters">The content algorithm and COSE KDF context inputs.</param>
    /// <returns>The copied or derived content key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>parameters</c> is null.</exception>
    internal static CoseDirectKeyDistributionResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey key,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (encryptedKey.Length != 0)
        {
            return Failure(
                "invalid_encrypted_key",
                "A COSE direct recipient ciphertext must be empty.");
        }

        return Distribute(
            key,
            algorithm,
            salt,
            parameters,
            false);
    }

    static CoseDirectKeyDistributionResult Distribute(
        TokenKey key,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters,
        bool encrypting)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            salt,
            parameters,
            encrypting);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        TokenBytesResult context = CoseKdfContext.Create(parameters);
        if (!context.Success)
        {
            return Failure(context.Error);
        }

        byte[] contentKey;
        if (algorithm == CoseAlgorithms.Direct)
        {
            contentKey = Copy(key.SymmetricKey);
        }
        else
        {
            int outputLength = (int)(parameters.KeyDataLength / 8UL);
            contentKey = Derive(
                key.SymmetricKey,
                salt,
                context.Bytes,
                outputLength,
                algorithm);
        }

        CryptographicOperations.ZeroMemory(context.Bytes);
        CoseDirectKeyDistributionResult result =
            new CoseDirectKeyDistributionResult();
        result.Success = true;
        result.ContentKey = contentKey;
        result.EncryptedKey = Array.Empty<byte>();
        return result;
    }

    static TokenResult Validate(
        TokenKey key,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters,
        bool encrypting)
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

        TokenResult sizeResult = ValidateOutputSize(
            parameters.KeyDataLength,
            algorithm);
        if (!sizeResult.Success)
        {
            return sizeResult;
        }

        if (!MatchesKeyAlgorithm(key, algorithm))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the COSE direct recipient algorithm.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit COSE direct key distribution.");
        }

        if (algorithm == CoseAlgorithms.Direct)
        {
            if (parameters.ProtectedParameters == null
                || parameters.ProtectedParameters.Length != 0)
            {
                return ValidationFailure(
                    "invalid_protected_headers",
                    "The protected recipient headers must be empty for direct key distribution.");
            }

            if ((ulong)(key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length * 8UL
                != parameters.KeyDataLength)
            {
                return ValidationFailure(
                    "invalid_key_size",
                    "The direct key must exactly match the requested content key size.");
            }

            return ValidateDirectOperation(key, encrypting);
        }

        if (salt == null && !HasPartyUNonce(parameters))
        {
            return ValidationFailure(
                "missing_kdf_uniqueness",
                "Direct HKDF requires a salt or PartyU nonce.");
        }

        byte[] symmetricKey = key.SymmetricKey
            ?? throw new InvalidOperationException("A required value is null.");
        if (algorithm == CoseAlgorithms.DirectHkdfAes128
            && symmetricKey.Length != 16
            || algorithm == CoseAlgorithms.DirectHkdfAes256
                && symmetricKey.Length != 32)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The shared secret does not match the direct HKDF AES key size.");
        }

        return ValidateDeriveOperation(key);
    }

    static TokenResult ValidateAlgorithm(int algorithm)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (algorithm != CoseAlgorithms.Direct
            && algorithm != CoseAlgorithms.DirectHkdfSha256
            && algorithm != CoseAlgorithms.DirectHkdfSha512
            && algorithm != CoseAlgorithms.DirectHkdfAes128
            && algorithm != CoseAlgorithms.DirectHkdfAes256)
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not a direct key distribution algorithm.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateOutputSize(
        ulong sizeInBits,
        int algorithm)
    {
        if (sizeInBits == 0
            || sizeInBits % 8UL != 0
            || sizeInBits > (ulong)MaximumKeySizeInBits)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The requested content key size is outside the permitted range.");
        }

        ulong sizeInBytes = sizeInBits / 8UL;
        if (algorithm != CoseAlgorithms.Direct
            && sizeInBytes > (ulong)(GetPrfSize(algorithm) * 255))
        {
            return ValidationFailure(
                "invalid_key_size",
                "The requested content key exceeds the HKDF expansion limit.");
        }

        return ValidationSuccess();
    }

    static bool MatchesKeyAlgorithm(TokenKey key, int algorithm)
    {
        if (key.Algorithm != null)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(
                key.Algorithm);
            if (!RegistryLookup.Contains(labels, algorithm))
            {
                return false;
            }
        }

        return !key.HasCoseAlgorithm
            || key.CoseAlgorithm == algorithm;
    }

    static TokenResult ValidateDirectOperation(
        TokenKey key,
        bool encrypting)
    {
        string operation = encrypting
            ? TokenKeyOperations.Encrypt
            : TokenKeyOperations.Decrypt;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                operation,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested direct key operation is not permitted by the key metadata.");
        }

        int coseOperation = encrypting
            ? CoseKeyLabels.OperationEncrypt
            : CoseKeyLabels.OperationDecrypt;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                coseOperation))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested direct key operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateDeriveOperation(TokenKey key)
    {
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                TokenKeyOperations.DeriveKey,
                false)
            && !RegistryLookup.Contains(
                key.KeyOperations,
                TokenKeyOperations.DeriveBits,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "Direct HKDF is not permitted by the key metadata.");
        }

        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                CoseKeyLabels.OperationDeriveKey)
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                CoseKeyLabels.OperationDeriveBits))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "Direct HKDF is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static bool HasPartyUNonce(CoseKdfContextParameters parameters)
    {
        if (parameters.PartyUInfo == null
            || parameters.PartyUInfo.Nonce == null)
        {
            return false;
        }

        return parameters.PartyUInfo.Nonce.Kind
            != TokenCborValueKind.Null;
    }

    static byte[] Derive(
        byte[]? secret,
        byte[]? salt,
        byte[]? context,
        int outputLength,
        int algorithm)
    {
        if (algorithm == CoseAlgorithms.DirectHkdfSha256)
        {
            return Hkdf(
                HashAlgorithmName.SHA256,
                secret,
                salt,
                context,
                outputLength);
        }

        if (algorithm == CoseAlgorithms.DirectHkdfSha512)
        {
            return Hkdf(
                HashAlgorithmName.SHA512,
                secret,
                salt,
                context,
                outputLength);
        }

        return AesHkdf(secret, context, outputLength);
    }

    static byte[] Hkdf(
        HashAlgorithmName hashAlgorithm,
        byte[]? secret,
        byte[]? salt,
        byte[]? context,
        int outputLength)
    {
        byte[] actualSalt = salt == null
            ? Array.Empty<byte>()
            : salt;
        return HKDF.DeriveKey(
            hashAlgorithm,
            (secret ?? throw new InvalidOperationException("A required value is null.")),
            outputLength,
            actualSalt,
            context);
    }

    static byte[] AesHkdf(
        byte[]? secret,
        byte[]? context,
        int outputLength)
    {
        byte[] output = new byte[outputLength];
        byte[] previous = Array.Empty<byte>();
        int outputOffset = 0;
        int counter = 1;
        while (outputOffset < output.Length)
        {
            int inputLength = previous.Length + (context ?? throw new InvalidOperationException("A required value is null.")).Length + 1;
            int paddedLength = inputLength;
            int remainder = paddedLength % 16;
            if (remainder != 0)
            {
                paddedLength += 16 - remainder;
            }

            byte[] input = new byte[paddedLength];
            previous.CopyTo(input, 0);
            context.CopyTo(input, previous.Length);
            input[inputLength - 1] = (byte)counter;
            byte[] block = AesCbcMac(secret, input);
            CryptographicOperations.ZeroMemory(input);
            if (previous.Length != 0)
            {
                CryptographicOperations.ZeroMemory(previous);
            }

            previous = block;
            int remaining = output.Length - outputOffset;
            int count = remaining < block.Length
                ? remaining
                : block.Length;
            block.AsSpan(0, count).CopyTo(
                output.AsSpan(outputOffset));
            outputOffset += count;
            counter += 1;
        }

        CryptographicOperations.ZeroMemory(previous);
        return output;
    }

    static byte[] AesCbcMac(byte[]? key, byte[] input)
    {
        byte[] initializationVector = new byte[16];
        byte[] encrypted;
        using (Aes aes = Aes.Create())
        {
            aes.Key = (key ?? throw new InvalidOperationException("A required value is null."));
            encrypted = aes.EncryptCbc(
                input,
                initializationVector,
                PaddingMode.None);
        }

        byte[] result = new byte[16];
        encrypted.AsSpan(
            encrypted.Length - 16,
            16).CopyTo(result);
        CryptographicOperations.ZeroMemory(initializationVector);
        CryptographicOperations.ZeroMemory(encrypted);
        return result;
    }

    static int GetPrfSize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.DirectHkdfSha256)
        {
            return 32;
        }

        if (algorithm == CoseAlgorithms.DirectHkdfSha512)
        {
            return 64;
        }

        return 16;
    }

    static byte[] Copy(byte[]? value)
    {
        byte[] result = new byte[(value ?? throw new InvalidOperationException("A required value is null.")).Length];
        value.CopyTo(result, 0);
        return result;
    }

    static CoseDirectKeyDistributionResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseDirectKeyDistributionResult Failure(TokenError? error)
    {
        CoseDirectKeyDistributionResult result =
            new CoseDirectKeyDistributionResult();
        result.Error = error;
        return result;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(
        string code,
        string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
