using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Encrypts and decrypts COSE content using registered AES-GCM algorithms.</summary>
internal static class CoseAesGcm
{
    const int InitializationVectorSize = 12;
    const int AuthenticationTagSize = 16;

    /// <summary>Encrypts plaintext using a generated unique 96-bit nonce.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The ciphertext, generated nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseAesGcmResult Encrypt(
        byte[] plaintext,
        byte[] additionalAuthenticatedData,
        TokenKey key,
        int algorithm)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException(
                "additionalAuthenticatedData");
        }

        return Encrypt(
            plaintext.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            key,
            algorithm);
    }

    /// <summary>Encrypts plaintext and authenticated-data spans using a generated nonce.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The ciphertext, generated nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static CoseAesGcmResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> additionalAuthenticatedData,
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
            return Failure(validation.Error);
        }

        byte[] initializationVector = RandomBytes.Generate(
            InitializationVectorSize);
        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            key);
    }

    /// <summary>Encrypts plaintext using a caller-supplied unique 96-bit nonce.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="initializationVector">The unique 96-bit nonce.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The ciphertext, copied nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseAesGcmResult Encrypt(
        byte[] plaintext,
        byte[] initializationVector,
        byte[] additionalAuthenticatedData,
        TokenKey key,
        int algorithm)
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
            throw new ArgumentNullException(
                "additionalAuthenticatedData");
        }

        return Encrypt(
            plaintext.AsSpan(),
            initializationVector.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            key,
            algorithm);
    }

    /// <summary>Encrypts plaintext, nonce, and authenticated-data spans.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="initializationVector">The unique 96-bit nonce.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The ciphertext, copied nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static CoseAesGcmResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
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
            return Failure(validation.Error);
        }

        if (initializationVector.Length != InitializationVectorSize)
        {
            return Failure(
                "invalid_initialization_vector",
                "COSE AES-GCM requires a 96-bit nonce.");
        }

        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            key);
    }

    /// <summary>Authenticates and decrypts COSE AES-GCM ciphertext.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="initializationVector">The 96-bit nonce.</param>
    /// <param name="authenticationTag">The 128-bit authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric decryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Decrypt(
        byte[] ciphertext,
        byte[] initializationVector,
        byte[] authenticationTag,
        byte[] additionalAuthenticatedData,
        TokenKey key,
        int algorithm)
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
            throw new ArgumentNullException(
                "additionalAuthenticatedData");
        }

        return Decrypt(
            ciphertext.AsSpan(),
            initializationVector.AsSpan(),
            authenticationTag.AsSpan(),
            additionalAuthenticatedData.AsSpan(),
            key,
            algorithm);
    }

    /// <summary>Authenticates and decrypts COSE AES-GCM byte spans.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="initializationVector">The 96-bit nonce.</param>
    /// <param name="authenticationTag">The 128-bit authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric decryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-GCM algorithm label.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult Decrypt(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> authenticationTag,
        ReadOnlySpan<byte> additionalAuthenticatedData,
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
            return BytesFailure(validation.Error);
        }

        if (initializationVector.Length != InitializationVectorSize
            || authenticationTag.Length != AuthenticationTagSize)
        {
            return DecryptionFailure();
        }

        byte[] ciphertextCopy = ciphertext.ToArray();
        byte[] initializationVectorCopy =
            initializationVector.ToArray();
        byte[] authenticationTagCopy = authenticationTag.ToArray();
        byte[] authenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] plaintext = new byte[ciphertextCopy.Length];
        Task<bool> operation = Task.Run(() => DecryptCore(
            ciphertextCopy,
            plaintext,
            initializationVectorCopy,
            authenticationTagCopy,
            authenticatedDataCopy,
            key.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(ciphertextCopy);
        CryptographicOperations.ZeroMemory(initializationVectorCopy);
        CryptographicOperations.ZeroMemory(authenticationTagCopy);
        CryptographicOperations.ZeroMemory(authenticatedDataCopy);
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

    static CoseAesGcmResult EncryptValidated(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey key)
    {
        byte[] plaintextCopy = plaintext.ToArray();
        byte[] initializationVectorCopy =
            initializationVector.ToArray();
        byte[] authenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] ciphertext = new byte[plaintextCopy.Length];
        byte[] authenticationTag = new byte[AuthenticationTagSize];
        Task<bool> operation = Task.Run(() => EncryptCore(
            plaintextCopy,
            ciphertext,
            initializationVectorCopy,
            authenticationTag,
            authenticatedDataCopy,
            key.SymmetricKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(plaintextCopy);
        CryptographicOperations.ZeroMemory(authenticatedDataCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(initializationVectorCopy);
            CryptographicOperations.ZeroMemory(authenticationTag);
            return Failure(
                "encryption_failed",
                "The AES-GCM provider could not encrypt the COSE content.");
        }

        CoseAesGcmResult result = new CoseAesGcmResult();
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
        TokenKey key,
        int algorithm,
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

        if ((key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")).Length != GetKeySize(algorithm))
        {
            return ValidationFailure(
                "invalid_key_size",
                "The symmetric key does not match the COSE AES-GCM algorithm size.");
        }

        if (key.Algorithm != null)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(
                key.Algorithm);
            if (!RegistryLookup.Contains(labels, algorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The JOSE key algorithm does not match the requested COSE AES-GCM algorithm.");
            }
        }

        if (key.HasCoseAlgorithm
            && key.CoseAlgorithm != algorithm)
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match the requested AES-GCM algorithm.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit COSE AES-GCM encryption or decryption.");
        }

        if (!PermitsTextOperation(key.KeyOperations, encrypting))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-GCM operation is not permitted by the key metadata.");
        }

        if (!PermitsCoseOperation(
            key.CoseKeyOperations,
            encrypting))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-GCM operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static bool PermitsTextOperation(
        string[] operations,
        bool encrypting)
    {
        if (operations == null || operations.Length == 0)
        {
            return true;
        }

        string primary = encrypting
            ? TokenKeyOperations.Encrypt
            : TokenKeyOperations.Decrypt;
        string keyOperation = encrypting
            ? TokenKeyOperations.WrapKey
            : TokenKeyOperations.UnwrapKey;
        return RegistryLookup.Contains(operations, primary, false)
            || RegistryLookup.Contains(
                operations,
                keyOperation,
                false);
    }

    static bool PermitsCoseOperation(
        int[]? operations,
        bool encrypting)
    {
        if (operations == null || operations.Length == 0)
        {
            return true;
        }

        int primary = encrypting
            ? CoseKeyLabels.OperationEncrypt
            : CoseKeyLabels.OperationDecrypt;
        int keyOperation = encrypting
            ? CoseKeyLabels.OperationWrapKey
            : CoseKeyLabels.OperationUnwrapKey;
        return RegistryLookup.Contains(operations, primary)
            || RegistryLookup.Contains(operations, keyOperation);
    }

    static TokenResult ValidateAlgorithm(int algorithm)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (algorithm != CoseAlgorithms.Aes128Gcm
            && algorithm != CoseAlgorithms.Aes192Gcm
            && algorithm != CoseAlgorithms.Aes256Gcm)
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an AES-GCM content-encryption algorithm.");
        }

        return ValidationSuccess();
    }

    static int GetKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.Aes128Gcm)
        {
            return 16;
        }

        if (algorithm == CoseAlgorithms.Aes192Gcm)
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

    static TokenBytesResult DecryptionFailure()
    {
        return BytesFailure(
            "decryption_failed",
            "The COSE AES-GCM content could not be authenticated and decrypted.");
    }

    static CoseAesGcmResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseAesGcmResult Failure(TokenError? error)
    {
        CoseAesGcmResult result = new CoseAesGcmResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(
        string code,
        string message)
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

    static TokenResult ValidationFailure(
        string code,
        string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
