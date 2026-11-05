using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Applies RSA-OAEP and compatibility-gated RSA1_5 JWE key management using built-in .NET RSA primitives.
/// </summary>
internal static class RsaKeyManagement
{
    /// <summary>Generates and encrypts a content-encryption key.</summary>
    /// <param name="key">The RSA public key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA-OAEP algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the generated key and RSA-encrypted key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static RsaKeyManagementResult Encrypt(
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Encrypt(key, algorithm, contentEncryptionAlgorithm, false);
    }

    /// <summary>Generates and encrypts a content-encryption key with explicit legacy compatibility control.</summary>
    /// <param name="key">The RSA public key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 key encryption is explicitly allowed.</param>
    /// <returns>A result containing the generated key and RSA-encrypted key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static RsaKeyManagementResult Encrypt(
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            contentEncryptionAlgorithm,
            false,
            true,
            allowCompatibility);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        byte[] contentEncryptionKey = RandomBytes.Generate(
            GetContentEncryptionKeySize(contentEncryptionAlgorithm));
        RsaKeyManagementResult result = EncryptCore(
            contentEncryptionKey,
            key,
            algorithm);
        if (!result.Success)
        {
            CryptographicOperations.ZeroMemory(contentEncryptionKey);
            return result;
        }

        result.ContentEncryptionKey = contentEncryptionKey;
        return result;
    }

    /// <summary>Encrypts a caller-supplied content-encryption key.</summary>
    /// <param name="contentEncryptionKey">The content-encryption key to encrypt.</param>
    /// <param name="key">The RSA public key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA-OAEP algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing independent content-key and encrypted-key arrays.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static RsaKeyManagementResult Wrap(
        byte[] contentEncryptionKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Wrap(
            contentEncryptionKey,
            key,
            algorithm,
            contentEncryptionAlgorithm,
            false);
    }

    /// <summary>Encrypts a caller-supplied content-encryption key with explicit legacy compatibility control.</summary>
    /// <param name="contentEncryptionKey">The content-encryption key to encrypt.</param>
    /// <param name="key">The RSA public key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 key encryption is explicitly allowed.</param>
    /// <returns>A result containing independent content-key and encrypted-key arrays.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static RsaKeyManagementResult Wrap(
        byte[] contentEncryptionKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool allowCompatibility)
    {
        if (contentEncryptionKey == null)
        {
            throw new ArgumentNullException("contentEncryptionKey");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            contentEncryptionAlgorithm,
            false,
            true,
            allowCompatibility);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        int expectedSize = GetContentEncryptionKeySize(contentEncryptionAlgorithm);
        if (contentEncryptionKey.Length != expectedSize)
        {
            return Failure(
                "invalid_key_size",
                "The content-encryption key does not match the JWE enc algorithm.");
        }

        byte[] copy = new byte[contentEncryptionKey.Length];
        contentEncryptionKey.CopyTo(copy, 0);
        RsaKeyManagementResult result = EncryptCore(copy, key, algorithm);
        if (!result.Success)
        {
            CryptographicOperations.ZeroMemory(copy);
            return result;
        }

        result.ContentEncryptionKey = copy;
        return result;
    }

    /// <summary>Decrypts an RSA-encrypted content-encryption key.</summary>
    /// <param name="encryptedKey">The RSA-encrypted JWE content-encryption key.</param>
    /// <param name="key">The RSA private key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA-OAEP algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the decrypted content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static RsaKeyManagementResult Decrypt(
        byte[] encryptedKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Decrypt(
            encryptedKey,
            key,
            algorithm,
            contentEncryptionAlgorithm,
            false);
    }

    /// <summary>Decrypts an RSA-encrypted content-encryption key with explicit legacy compatibility control.</summary>
    /// <param name="encryptedKey">The RSA-encrypted JWE content-encryption key.</param>
    /// <param name="key">The RSA private key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 key decryption is explicitly allowed.</param>
    /// <returns>A result containing the decrypted content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static RsaKeyManagementResult Decrypt(
        byte[] encryptedKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool allowCompatibility)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        return Decrypt(
            encryptedKey.AsSpan(),
            key,
            algorithm,
            contentEncryptionAlgorithm,
            allowCompatibility);
    }

    /// <summary>Decrypts an RSA-encrypted content-encryption key span.</summary>
    /// <param name="encryptedKey">The RSA-encrypted JWE content-encryption key.</param>
    /// <param name="key">The RSA private key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA-OAEP algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the decrypted content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c>, <c>algorithm</c>, or <c>contentEncryptionAlgorithm</c> is null.</exception>
    internal static RsaKeyManagementResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Decrypt(
            encryptedKey,
            key,
            algorithm,
            contentEncryptionAlgorithm,
            false);
    }

    /// <summary>Decrypts an RSA-encrypted content-encryption key span with explicit legacy compatibility control.</summary>
    /// <param name="encryptedKey">The RSA-encrypted JWE content-encryption key.</param>
    /// <param name="key">The RSA private key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE RSA key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 key decryption is explicitly allowed.</param>
    /// <returns>A result containing the decrypted content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c>, <c>algorithm</c>, or <c>contentEncryptionAlgorithm</c> is null.</exception>
    internal static RsaKeyManagementResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            contentEncryptionAlgorithm,
            true,
            false,
            allowCompatibility);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (encryptedKey.Length != (key.Rsa ?? throw new InvalidOperationException("A required value is null.")).KeySize / 8)
        {
            return DecryptionFailure();
        }

        byte[] encryptedCopy = encryptedKey.ToArray();
        Task<byte[]> operation = Task.Run(() => key.Rsa.Decrypt(
            encryptedCopy,
            GetPadding(algorithm)));
        Wait(operation);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(encryptedCopy);
            return DecryptionFailure();
        }

        byte[] contentEncryptionKey = operation.Result;
        int expectedSize = GetContentEncryptionKeySize(contentEncryptionAlgorithm);
        if (contentEncryptionKey.Length != expectedSize)
        {
            CryptographicOperations.ZeroMemory(contentEncryptionKey);
            return DecryptionFailure();
        }

        RsaKeyManagementResult result = new RsaKeyManagementResult();
        result.Success = true;
        result.ContentEncryptionKey = contentEncryptionKey;
        result.EncryptedKey = encryptedCopy;
        return result;
    }

    static RsaKeyManagementResult EncryptCore(
        byte[] contentEncryptionKey,
        TokenKey key,
        string algorithm)
    {
        Task<byte[]> operation = Task.Run(() => (key.Rsa ?? throw new InvalidOperationException("A required value is null.")).Encrypt(
            contentEncryptionKey,
            GetPadding(algorithm)));
        Wait(operation);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            return Failure(
                "encryption_failed",
                "The RSA provider could not encrypt the content-encryption key.");
        }

        RsaKeyManagementResult result = new RsaKeyManagementResult();
        result.Success = true;
        result.EncryptedKey = operation.Result;
        return result;
    }

    static TokenResult Validate(
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool requirePrivateMaterial,
        bool wrapping,
        bool allowCompatibility)
    {
        TokenResult algorithmValidation = ValidateKeyManagementAlgorithm(
            algorithm,
            allowCompatibility);
        if (!algorithmValidation.Success)
        {
            return algorithmValidation;
        }

        TokenResult contentValidation = ValidateContentEncryptionAlgorithm(
            contentEncryptionAlgorithm);
        if (!contentValidation.Success)
        {
            return contentValidation;
        }

        TokenResult keyValidation = RsaKeyImporterOperations.Validate(
            key,
            requirePrivateMaterial,
            allowCompatibility);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested RSA key-management algorithm.");
        }

        if (key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm does not match the requested RSA key-management algorithm.");
            }
        }

        if (key.Use != null
            && !string.Equals(key.Use, TokenKeyUses.Encryption, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit JWE key management.");
        }

        string operation = wrapping
            ? TokenKeyOperations.WrapKey
            : TokenKeyOperations.UnwrapKey;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(key.KeyOperations, operation, false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested RSA key operation is not permitted by the key metadata.");
        }

        int coseOperation = wrapping
            ? CoseKeyLabels.OperationWrapKey
            : CoseKeyLabels.OperationUnwrapKey;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(key.CoseKeyOperations, coseOperation))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested RSA key operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateKeyManagementAlgorithm(
        string algorithm,
        bool allowCompatibility)
    {
        if (string.Equals(algorithm, JoseAlgorithms.RsaOaep, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha512, StringComparison.Ordinal))
        {
            return ValidationSuccess();
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal))
        {
            if (allowCompatibility)
            {
                return ValidationSuccess();
            }

            return ValidationFailure(
                "compatibility_required",
                "RSA1_5 key management requires explicit compatibility opt-in.");
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return ValidationFailure(
            "invalid_algorithm",
            "The JOSE algorithm is not a supported RSA key-management algorithm.");
    }

    static TokenResult ValidateContentEncryptionAlgorithm(string algorithm)
    {
        if (GetContentEncryptionKeySize(algorithm) != 0)
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
            "The JOSE algorithm is not a supported content-encryption algorithm.");
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

    static RSAEncryptionPadding GetPadding(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal))
        {
            return RSAEncryptionPadding.Pkcs1;
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaOaep, StringComparison.Ordinal))
        {
            return RSAEncryptionPadding.OaepSHA1;
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaOaepSha256, StringComparison.Ordinal))
        {
            return RSAEncryptionPadding.OaepSHA256;
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaOaepSha384, StringComparison.Ordinal))
        {
            return RSAEncryptionPadding.OaepSHA384;
        }

        return RSAEncryptionPadding.OaepSHA512;
    }

    static void Wait(Task<byte[]> operation)
    {
        while (!operation.IsCompleted)
        {
            Thread.Yield();
        }
    }

    static void ObserveFailure(Task<byte[]> operation)
    {
        AggregateException? failure = operation.Exception;
        if (failure != null)
        {
            failure.Flatten();
        }
    }

    static RsaKeyManagementResult DecryptionFailure()
    {
        return Failure(
            "decryption_failed",
            "The RSA-encrypted content-encryption key could not be decrypted.");
    }

    static RsaKeyManagementResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static RsaKeyManagementResult Failure(TokenError? error)
    {
        RsaKeyManagementResult result = new RsaKeyManagementResult();
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
