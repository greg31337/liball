using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Encrypts and decrypts JWE content using A128GCM, A192GCM, or A256GCM.
/// </summary>
internal static class AesGcmContentEncryption
{
    const int InitializationVectorSize = 12;
    const int AuthenticationTagSize = 16;

    /// <summary>Encrypts plaintext using a generated 96-bit initialization vector.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the ciphertext, generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static AesGcmContentEncryptionResult Encrypt(
        byte[] plaintext,
        byte[] additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException("additionalAuthenticatedData");
        }

        return Encrypt(
            plaintext.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            contentEncryptionKey,
            algorithm);
    }

    /// <summary>Encrypts plaintext and AAD spans using a generated 96-bit initialization vector.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the ciphertext, generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static AesGcmContentEncryptionResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        ValidateReferences(contentEncryptionKey, algorithm);
        TokenResult validation = Validate(contentEncryptionKey, algorithm, true);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        byte[] initializationVector = RandomBytes.Generate(
            InitializationVectorSize);
        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            contentEncryptionKey);
    }

    /// <summary>Encrypts plaintext using a caller-supplied 96-bit initialization vector.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="initializationVector">The unique 96-bit initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the ciphertext, copied IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static AesGcmContentEncryptionResult Encrypt(
        byte[] plaintext,
        byte[] initializationVector,
        byte[] additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        if (initializationVector == null)
        {
            throw new ArgumentNullException("initializationVector");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException("additionalAuthenticatedData");
        }

        return Encrypt(
            plaintext.AsSpan(),
            initializationVector.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            contentEncryptionKey,
            algorithm);
    }

    /// <summary>Encrypts plaintext, IV, and AAD spans.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="initializationVector">The unique 96-bit initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the ciphertext, copied IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static AesGcmContentEncryptionResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        ValidateReferences(contentEncryptionKey, algorithm);
        TokenResult validation = Validate(contentEncryptionKey, algorithm, true);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (initializationVector.Length != InitializationVectorSize)
        {
            return Failure(
                "invalid_initialization_vector",
                "JWE AES-GCM requires a 96-bit initialization vector.");
        }

        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            contentEncryptionKey);
    }

    /// <summary>Authenticates and decrypts JWE AES-GCM content.</summary>
    /// <param name="ciphertext">The JWE ciphertext.</param>
    /// <param name="initializationVector">The decoded 96-bit JWE initialization vector.</param>
    /// <param name="authenticationTag">The decoded 128-bit JWE authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Decrypt(
        byte[] ciphertext,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[] additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        if (ciphertext == null)
        {
            throw new ArgumentNullException("ciphertext");
        }

        if (initializationVector == null)
        {
            throw new ArgumentNullException("initializationVector");
        }

        if (authenticationTag == null)
        {
            throw new ArgumentNullException("authenticationTag");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException("additionalAuthenticatedData");
        }

        return Decrypt(
            ciphertext.AsSpan(),
            initializationVector.AsSpan(),
            authenticationTag.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            contentEncryptionKey,
            algorithm);
    }

    /// <summary>Authenticates and decrypts ciphertext, IV, tag, and AAD spans.</summary>
    /// <param name="ciphertext">The JWE ciphertext.</param>
    /// <param name="initializationVector">The decoded 96-bit JWE initialization vector.</param>
    /// <param name="authenticationTag">The decoded 128-bit JWE authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Decrypt(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> authenticationTag,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        ValidateReferences(contentEncryptionKey, algorithm);
        TokenResult validation = Validate(contentEncryptionKey, algorithm, false);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        if (initializationVector.Length != InitializationVectorSize
            || authenticationTag.Length != AuthenticationTagSize)
        {
            return DecryptionFailure();
        }

        byte[] ciphertextCopy = ciphertext.ToArray();
        byte[] initializationVectorCopy = initializationVector.ToArray();
        byte[] authenticationTagCopy = authenticationTag.ToArray();
        byte[] additionalAuthenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] plaintext = new byte[ciphertextCopy.Length];
        Task<bool> operation = Task.Run(() => DecryptCore(
            ciphertextCopy,
            plaintext,
            initializationVectorCopy,
            authenticationTagCopy,
            additionalAuthenticatedDataCopy,
            contentEncryptionKey.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(ciphertextCopy);
        CryptographicOperations.ZeroMemory(initializationVectorCopy);
        CryptographicOperations.ZeroMemory(authenticationTagCopy);
        CryptographicOperations.ZeroMemory(additionalAuthenticatedDataCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(plaintext);
            return DecryptionFailure();
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = plaintext;
        return result;
    }

    static AesGcmContentEncryptionResult EncryptValidated(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey)
    {
        byte[] plaintextCopy = plaintext.ToArray();
        byte[] initializationVectorCopy = initializationVector.ToArray();
        byte[] additionalAuthenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] ciphertext = new byte[plaintextCopy.Length];
        byte[] authenticationTag = new byte[AuthenticationTagSize];
        Task<bool> operation = Task.Run(() => EncryptCore(
            plaintextCopy,
            ciphertext,
            initializationVectorCopy,
            authenticationTag,
            additionalAuthenticatedDataCopy,
            contentEncryptionKey.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(plaintextCopy);
        CryptographicOperations.ZeroMemory(additionalAuthenticatedDataCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(initializationVectorCopy);
            CryptographicOperations.ZeroMemory(authenticationTag);
            return Failure(
                "encryption_failed",
                "The AES-GCM provider could not encrypt the JWE content.");
        }

        AesGcmContentEncryptionResult result =
            new AesGcmContentEncryptionResult();
        result.Success = true;
        result.Ciphertext = ciphertext;
        result.InitializationVector = initializationVectorCopy;
        result.AuthenticationTag = authenticationTag;
        return result;
    }

    static bool EncryptCore(
        byte[] plaintext,
        byte[] ciphertext,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[] additionalAuthenticatedData,
        byte[]? key)
    {
        using (AesGcm aes = new AesGcm((key ?? throw new InvalidOperationException("A required value is null.")), AuthenticationTagSize))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                ciphertext,
                authenticationTag,
                additionalAuthenticatedData);
        }

        return true;
    }

    static bool DecryptCore(
        byte[] ciphertext,
        byte[] plaintext,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[] additionalAuthenticatedData,
        byte[]? key)
    {
        using (AesGcm aes = new AesGcm((key ?? throw new InvalidOperationException("A required value is null.")), AuthenticationTagSize))
        {
            aes.Decrypt(
                initializationVector,
                ciphertext,
                authenticationTag,
                plaintext,
                additionalAuthenticatedData);
        }

        return true;
    }

    static TokenResult Validate(
        TokenKey contentEncryptionKey,
        string algorithm,
        bool encrypting)
    {
        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return algorithmValidation;
        }

        TokenResult keyValidation = SymmetricKeyImporterOperations.Validate(
            contentEncryptionKey);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if ((contentEncryptionKey.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length != GetKeySize(algorithm))
        {
            return ValidationFailure(
                "invalid_key_size",
                "The content-encryption key does not match the AES-GCM algorithm size.");
        }

        if (contentEncryptionKey.Algorithm != null
            && !string.Equals(
                contentEncryptionKey.Algorithm,
                algorithm,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested AES-GCM content algorithm.");
        }

        if (contentEncryptionKey.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(
                mapped,
                contentEncryptionKey.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm is not equivalent to the JWE AES-GCM algorithm.");
            }
        }

        if (contentEncryptionKey.Use != null
            && !string.Equals(
                contentEncryptionKey.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit JWE content encryption or decryption.");
        }

        string operation = encrypting
            ? TokenKeyOperations.Encrypt
            : TokenKeyOperations.Decrypt;
        if (contentEncryptionKey.KeyOperations != null
            && contentEncryptionKey.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                contentEncryptionKey.KeyOperations,
                operation,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-GCM content operation is not permitted by the key metadata.");
        }

        int coseOperation = encrypting
            ? CoseKeyLabels.OperationEncrypt
            : CoseKeyLabels.OperationDecrypt;
        if (contentEncryptionKey.CoseKeyOperations != null
            && contentEncryptionKey.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                contentEncryptionKey.CoseKeyOperations,
                coseOperation))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-GCM content operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(
                algorithm,
                JoseAlgorithms.Aes128Gcm,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes192Gcm,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes256Gcm,
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
            "The JOSE algorithm is not an AES-GCM content-encryption algorithm.");
    }

    static int GetKeySize(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes128Gcm,
            StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192Gcm,
            StringComparison.Ordinal))
        {
            return 24;
        }

        return 32;
    }

    static void ValidateReferences(TokenKey key, string algorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("contentEncryptionKey");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }
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

    static TokenBytesResult DecryptionFailure()
    {
        return BytesFailure(
            "decryption_failed",
            "The JWE AES-GCM content could not be authenticated and decrypted.");
    }

    static AesGcmContentEncryptionResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static AesGcmContentEncryptionResult Failure(TokenError? error)
    {
        AesGcmContentEncryptionResult result =
            new AesGcmContentEncryptionResult();
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
