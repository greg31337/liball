using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Encrypts and decrypts COSE content using registered AES-CCM algorithms.</summary>
internal static class CoseAesCcm
{
    const int MaximumShortLengthPayload = 65535;

    /// <summary>Encrypts plaintext using a generated unique nonce.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
    /// <returns>The ciphertext, generated nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseAesCcmResult Encrypt(
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
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
    /// <returns>The ciphertext, generated nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static CoseAesCcmResult Encrypt(
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

        TokenResult lengthResult = ValidatePlaintextLength(
            plaintext.Length,
            algorithm);
        if (!lengthResult.Success)
        {
            return Failure(lengthResult.Error);
        }

        byte[] initializationVector = RandomBytes.Generate(
            GetInitializationVectorSize(algorithm));
        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            key,
            algorithm);
    }

    /// <summary>Encrypts plaintext using a caller-supplied unique nonce.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="initializationVector">The unique nonce.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
    /// <returns>The ciphertext, copied nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseAesCcmResult Encrypt(
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
    /// <param name="initializationVector">The unique nonce.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
    /// <returns>The ciphertext, copied nonce, and authentication tag, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static CoseAesCcmResult Encrypt(
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

        if (initializationVector.Length
            != GetInitializationVectorSize(algorithm))
        {
            return Failure(
                "invalid_initialization_vector",
                "The nonce length does not match the COSE AES-CCM algorithm.");
        }

        TokenResult lengthResult = ValidatePlaintextLength(
            plaintext.Length,
            algorithm);
        if (!lengthResult.Success)
        {
            return Failure(lengthResult.Error);
        }

        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            key,
            algorithm);
    }

    /// <summary>Authenticates and decrypts COSE AES-CCM ciphertext.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric decryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
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

    /// <summary>Authenticates and decrypts COSE AES-CCM byte spans.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The encoded COSE Enc_structure bytes.</param>
    /// <param name="key">The symmetric decryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES-CCM algorithm label.</param>
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

        if (initializationVector.Length
                != GetInitializationVectorSize(algorithm)
            || authenticationTag.Length
                != GetAuthenticationTagSize(algorithm)
            || !IsValidPayloadLength(ciphertext.Length, algorithm))
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

    static CoseAesCcmResult EncryptValidated(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey key,
        int algorithm)
    {
        byte[] plaintextCopy = plaintext.ToArray();
        byte[] initializationVectorCopy =
            initializationVector.ToArray();
        byte[] authenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] ciphertext = new byte[plaintextCopy.Length];
        byte[] authenticationTag = new byte[
            GetAuthenticationTagSize(algorithm)];
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
                "The AES-CCM provider could not encrypt the COSE content.");
        }

        CoseAesCcmResult result = new CoseAesCcmResult();
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
        using (AesCcm aes = new AesCcm((key ?? throw new InvalidOperationException("A required value is null."))))
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
        using (AesCcm aes = new AesCcm((key ?? throw new InvalidOperationException("A required value is null."))))
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
                "The symmetric key does not match the COSE AES-CCM algorithm size.");
        }

        if (key.Algorithm != null)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(
                key.Algorithm);
            if (!RegistryLookup.Contains(labels, algorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The JOSE key algorithm does not match the requested COSE AES-CCM algorithm.");
            }
        }

        if (key.HasCoseAlgorithm
            && key.CoseAlgorithm != algorithm)
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match the requested AES-CCM algorithm.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit COSE AES-CCM encryption or decryption.");
        }

        if (!PermitsTextOperation(key.KeyOperations, encrypting))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-CCM operation is not permitted by the key metadata.");
        }

        if (!PermitsCoseOperation(
            key.CoseKeyOperations,
            encrypting))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The requested AES-CCM operation is not permitted by the COSE key metadata.");
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

        if (!IsAesCcmAlgorithm(algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an AES-CCM content-encryption algorithm.");
        }

        if (!AesCcm.IsSupported)
        {
            return ValidationFailure(
                "unsupported_algorithm",
                "AES-CCM is not supported by this platform.");
        }

        return ValidationSuccess();
    }

    static bool IsAesCcmAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_256
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm16_128_256
            || algorithm == CoseAlgorithms.AesCcm64_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_256;
    }

    static int GetKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_128)
        {
            return 16;
        }

        return 32;
    }

    static int GetInitializationVectorSize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm16_128_256)
        {
            return 13;
        }

        return 7;
    }

    static int GetAuthenticationTagSize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_256)
        {
            return 8;
        }

        return 16;
    }

    static TokenResult ValidatePlaintextLength(
        int length,
        int algorithm)
    {
        if (!IsValidPayloadLength(length, algorithm))
        {
            return ValidationFailure(
                "plaintext_too_large",
                "The plaintext is too large for the COSE AES-CCM nonce size.");
        }

        return ValidationSuccess();
    }

    static bool IsValidPayloadLength(int length, int algorithm)
    {
        return GetInitializationVectorSize(algorithm) != 13
            || length <= MaximumShortLengthPayload;
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
            "The COSE AES-CCM content could not be authenticated and decrypted.");
    }

    static CoseAesCcmResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseAesCcmResult Failure(TokenError? error)
    {
        CoseAesCcmResult result = new CoseAesCcmResult();
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
