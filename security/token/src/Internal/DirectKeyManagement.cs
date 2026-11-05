using System;
namespace token;

/// <summary>
/// Applies JWE <c>dir</c> key management using a pre-shared content-encryption key.
/// </summary>
internal static class DirectKeyManagement
{
    /// <summary>Obtains the content-encryption key for JWE encryption.</summary>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing a copied content-encryption key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static DirectKeyManagementResult Encrypt(
        TokenKey key,
        string contentEncryptionAlgorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        TokenResult validation = Validate(
            key,
            contentEncryptionAlgorithm,
            true);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return Success(key.SymmetricKey);
    }

    /// <summary>Obtains the content-encryption key for JWE decryption.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value, which must be empty for <c>dir</c>.</param>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing a copied content-encryption key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static DirectKeyManagementResult Decrypt(
        byte[] encryptedKey,
        TokenKey key,
        string contentEncryptionAlgorithm)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        return Decrypt(
            encryptedKey.AsSpan(),
            key,
            contentEncryptionAlgorithm);
    }

    /// <summary>Obtains the content-encryption key using a JWE encrypted-key span.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value, which must be empty for <c>dir</c>.</param>
    /// <param name="key">The pre-shared symmetric key and its metadata.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing a copied content-encryption key and an empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>contentEncryptionAlgorithm</c> is null.</exception>
    internal static DirectKeyManagementResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey key,
        string contentEncryptionAlgorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        if (encryptedKey.Length != 0)
        {
            return Failure(
                "invalid_encrypted_key",
                "The JWE encrypted-key value must be empty for direct key management.");
        }

        TokenResult validation = Validate(
            key,
            contentEncryptionAlgorithm,
            false);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return Success(key.SymmetricKey);
    }

    static TokenResult Validate(
        TokenKey key,
        string contentEncryptionAlgorithm,
        bool encrypting)
    {
        int keySize = GetContentEncryptionKeySize(contentEncryptionAlgorithm);
        if (keySize == 0)
        {
            TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
                contentEncryptionAlgorithm);
            if (!availability.Success)
            {
                return availability;
            }

            return ValidationFailure(
                "invalid_algorithm",
                "The JOSE algorithm is not a supported content-encryption algorithm.");
        }

        TokenResult keyValidation = SymmetricKeyImporterOperations.Validate(key);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if (key.Algorithm != null
            && !string.Equals(
                key.Algorithm,
                JoseAlgorithms.Direct,
                StringComparison.Ordinal)
            && !string.Equals(
                key.Algorithm,
                contentEncryptionAlgorithm,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match JWE direct key management.");
        }

        if (key.HasCoseAlgorithm
            && key.CoseAlgorithm != CoseAlgorithms.Direct
            && !CoseMatchesContentEncryption(
                key.CoseAlgorithm,
                contentEncryptionAlgorithm))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match direct key management.");
        }

        if ((key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length != keySize)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The direct key must exactly match the content-encryption key size.");
        }

        if (key.Use != null
            && !string.Equals(key.Use, TokenKeyUses.Encryption, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit JWE encryption or decryption.");
        }

        string operation = encrypting
            ? TokenKeyOperations.Encrypt
            : TokenKeyOperations.Decrypt;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(key.KeyOperations, operation, false))
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
            && !RegistryLookup.Contains(key.CoseKeyOperations, coseOperation))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested direct key operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static bool CoseMatchesContentEncryption(
        int coseAlgorithm,
        string contentEncryptionAlgorithm)
    {
        int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(
            contentEncryptionAlgorithm);
        return RegistryLookup.Contains(labels, coseAlgorithm);
    }

    static int GetContentEncryptionKeySize(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Aes128Gcm, StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes192Gcm, StringComparison.Ordinal))
        {
            return 24;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes256Gcm, StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes128CbcHmacSha256,
                StringComparison.Ordinal))
        {
            return 32;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192CbcHmacSha384,
            StringComparison.Ordinal))
        {
            return 48;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes256CbcHmacSha512,
            StringComparison.Ordinal))
        {
            return 64;
        }

        return 0;
    }

    static DirectKeyManagementResult Success(byte[]? keyBytes)
    {
        DirectKeyManagementResult result = new DirectKeyManagementResult();
        result.ContentEncryptionKey = new byte[(keyBytes ?? throw new InvalidOperationException("A required value is null.")).Length];
        keyBytes.CopyTo(result.ContentEncryptionKey, 0);
        result.EncryptedKey = Array.Empty<byte>();
        result.Success = true;
        return result;
    }

    static DirectKeyManagementResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static DirectKeyManagementResult Failure(TokenError? error)
    {
        DirectKeyManagementResult result = new DirectKeyManagementResult();
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
