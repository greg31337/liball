using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Wraps and unwraps JWE content-encryption keys using AES-GCM key encryption.
/// </summary>
internal static class AesGcmKeyWrap
{
    const int InitializationVectorSize = 12;
    const int AuthenticationTagSize = 16;

    /// <summary>Wraps key material using A128GCMKW, A192GCMKW, or A256GCMKW.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES-GCM key-wrap algorithm name.</param>
    /// <returns>A result containing the encrypted key, generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static AesGcmKeyWrapResult Wrap(
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

    /// <summary>Wraps a key-material span using A128GCMKW, A192GCMKW, or A256GCMKW.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES-GCM key-wrap algorithm name.</param>
    /// <returns>A result containing the encrypted key, generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> or <c>algorithm</c> is null.</exception>
    internal static AesGcmKeyWrapResult Wrap(
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

        if (keyMaterial.Length == 0)
        {
            return Failure(
                "invalid_key_size",
                "AES-GCM Key Wrap input must contain key material.");
        }

        byte[] plaintext = keyMaterial.ToArray();
        byte[] encryptedKey = new byte[plaintext.Length];
        byte[] initializationVector = RandomBytes.Generate(InitializationVectorSize);
        byte[] authenticationTag = new byte[AuthenticationTagSize];
        Task<bool> operation = Task.Run(() => EncryptCore(
            plaintext,
            encryptedKey,
            initializationVector,
            authenticationTag,
            keyEncryptionKey.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(plaintext);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(encryptedKey);
            CryptographicOperations.ZeroMemory(initializationVector);
            CryptographicOperations.ZeroMemory(authenticationTag);
            return Failure(
                "encryption_failed",
                "The AES-GCM provider could not wrap the key material.");
        }

        AesGcmKeyWrapResult result = new AesGcmKeyWrapResult();
        result.Success = true;
        result.EncryptedKey = encryptedKey;
        result.InitializationVector = initializationVector;
        result.AuthenticationTag = authenticationTag;
        return result;
    }

    /// <summary>Unwraps key material using A128GCMKW, A192GCMKW, or A256GCMKW.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value.</param>
    /// <param name="initializationVector">The decoded JWE <c>iv</c> header value.</param>
    /// <param name="authenticationTag">The decoded JWE <c>tag</c> header value.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES-GCM key-wrap algorithm name.</param>
    /// <returns>A result containing the authenticated unwrapped key material.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Unwrap(
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] authenticationTag,
        TokenKey keyEncryptionKey,
        string algorithm)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        if (initializationVector == null)
        {
            throw new ArgumentNullException("initializationVector");
        }

        if (authenticationTag == null)
        {
            throw new ArgumentNullException("authenticationTag");
        }

        return Unwrap(
            encryptedKey.AsSpan(),
            initializationVector.AsSpan(),
            authenticationTag.AsSpan(),
            keyEncryptionKey,
            algorithm);
    }

    /// <summary>Unwraps key material from JWE encrypted-key, IV, and tag spans.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value.</param>
    /// <param name="initializationVector">The decoded JWE <c>iv</c> header value.</param>
    /// <param name="authenticationTag">The decoded JWE <c>tag</c> header value.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE AES-GCM key-wrap algorithm name.</param>
    /// <returns>A result containing the authenticated unwrapped key material.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> or <c>algorithm</c> is null.</exception>
    internal static TokenBytesResult Unwrap(
        ReadOnlySpan<byte> encryptedKey,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> authenticationTag,
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
            return BytesFailure(validation.Error);
        }

        if (encryptedKey.Length == 0
            || initializationVector.Length != InitializationVectorSize
            || authenticationTag.Length != AuthenticationTagSize)
        {
            return UnwrapFailure();
        }

        byte[] encryptedCopy = encryptedKey.ToArray();
        byte[] initializationVectorCopy = initializationVector.ToArray();
        byte[] authenticationTagCopy = authenticationTag.ToArray();
        byte[] plaintext = new byte[encryptedCopy.Length];
        Task<bool> operation = Task.Run(() => DecryptCore(
            encryptedCopy,
            plaintext,
            initializationVectorCopy,
            authenticationTagCopy,
            keyEncryptionKey.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(encryptedCopy);
        CryptographicOperations.ZeroMemory(initializationVectorCopy);
        CryptographicOperations.ZeroMemory(authenticationTagCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(plaintext);
            return UnwrapFailure();
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = plaintext;
        return result;
    }

    static bool EncryptCore(
        byte[] plaintext,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[]? key)
    {
        using (AesGcm aes = new AesGcm((key ?? throw new InvalidOperationException("A required value is null.")), AuthenticationTagSize))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                encryptedKey,
                authenticationTag);
        }

        return true;
    }

    static bool DecryptCore(
        byte[] encryptedKey,
        byte[] plaintext,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[]? key)
    {
        using (AesGcm aes = new AesGcm((key ?? throw new InvalidOperationException("A required value is null.")), AuthenticationTagSize))
        {
            aes.Decrypt(
                initializationVector,
                encryptedKey,
                authenticationTag,
                plaintext);
        }

        return true;
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
                "The key algorithm does not match the requested AES-GCM Key Wrap algorithm.");
        }

        if (keyEncryptionKey.HasCoseAlgorithm)
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "COSE does not define an algorithm equivalent to JWE AES-GCM Key Wrap.");
        }

        if (keyEncryptionKey.Use != null
            && !string.Equals(
                keyEncryptionKey.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit AES-GCM key wrapping or unwrapping.");
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
                "The requested AES-GCM key operation is not permitted by the key metadata.");
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
                "The requested AES-GCM key operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(
                algorithm,
                JoseAlgorithms.Aes128GcmKeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes192GcmKeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes256GcmKeyWrap,
                StringComparison.Ordinal))
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
            "The JOSE algorithm is not an AES-GCM Key Wrap algorithm.");
    }

    static int GetKeySize(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes128GcmKeyWrap,
            StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192GcmKeyWrap,
            StringComparison.Ordinal))
        {
            return 24;
        }

        return 32;
    }

    static void Wait(Task<bool> operation)
    {
        while (!operation.IsCompleted)
        {
            Thread.Yield();
        }
    }

    static void ObserveFailure(Task<bool> operation)
    {
        AggregateException? failure = operation.Exception;
        if (failure != null)
        {
            failure.Flatten();
        }
    }

    static TokenBytesResult UnwrapFailure()
    {
        return BytesFailure(
            "unwrap_failed",
            "The encrypted key could not be authenticated and unwrapped.");
    }

    static AesGcmKeyWrapResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static AesGcmKeyWrapResult Failure(TokenError? error)
    {
        AesGcmKeyWrapResult result = new AesGcmKeyWrapResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        return BytesFailure(new TokenError(code, message, -1));
    }

    static TokenBytesResult BytesFailure(TokenError? error)
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
