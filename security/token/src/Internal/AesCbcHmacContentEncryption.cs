using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Encrypts and decrypts JWE content using AES-CBC-HMAC-SHA2 algorithms.
/// </summary>
internal static class AesCbcHmacContentEncryption
{
    const int InitializationVectorSize = 16;

    /// <summary>Encrypts plaintext using a generated 128-bit initialization vector.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing ciphertext, the generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static AesCbcHmacContentEncryptionResult Encrypt(
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

    /// <summary>Encrypts plaintext and AAD spans using a generated 128-bit IV.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing ciphertext, the generated IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static AesCbcHmacContentEncryptionResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        ValidateReferences(contentEncryptionKey, algorithm);
        TokenResult validation = Validate(
            contentEncryptionKey,
            algorithm,
            true);
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
            contentEncryptionKey,
            algorithm);
    }

    /// <summary>Encrypts plaintext using a caller-supplied 128-bit initialization vector.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="initializationVector">The random 128-bit initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing ciphertext, a copied IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static AesCbcHmacContentEncryptionResult Encrypt(
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

    /// <summary>Encrypts plaintext, initialization-vector, and AAD spans.</summary>
    /// <param name="plaintext">The plaintext content.</param>
    /// <param name="initializationVector">The random 128-bit initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The JWE Additional Authenticated Data bytes.</param>
    /// <param name="contentEncryptionKey">The content-encryption key and its metadata.</param>
    /// <param name="algorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing ciphertext, a copied IV, and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static AesCbcHmacContentEncryptionResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        ValidateReferences(contentEncryptionKey, algorithm);
        TokenResult validation = Validate(
            contentEncryptionKey,
            algorithm,
            true);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (initializationVector.Length != InitializationVectorSize)
        {
            return Failure(
                "invalid_initialization_vector",
                "JWE AES-CBC-HMAC-SHA2 requires a 128-bit initialization vector.");
        }

        return EncryptValidated(
            plaintext,
            initializationVector,
            additionalAuthenticatedData,
            contentEncryptionKey,
            algorithm);
    }

    /// <summary>Authenticates and decrypts JWE AES-CBC-HMAC-SHA2 content.</summary>
    /// <param name="ciphertext">The JWE ciphertext.</param>
    /// <param name="initializationVector">The decoded 128-bit JWE initialization vector.</param>
    /// <param name="authenticationTag">The decoded JWE authentication tag.</param>
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
    /// <param name="initializationVector">The decoded 128-bit JWE initialization vector.</param>
    /// <param name="authenticationTag">The decoded JWE authentication tag.</param>
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
        TokenResult validation = Validate(
            contentEncryptionKey,
            algorithm,
            false);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        if (ciphertext.Length == 0
            || ciphertext.Length % InitializationVectorSize != 0
            || initializationVector.Length != InitializationVectorSize
            || authenticationTag.Length != GetTagSize(algorithm))
        {
            return DecryptionFailure();
        }

        int halfKeySize = GetKeySize(algorithm) / 2;
        byte[] macKey = contentEncryptionKey.SymmetricKey
            .AsSpan(0, halfKeySize).ToArray();
        byte[] encryptionKey = contentEncryptionKey.SymmetricKey
            .AsSpan(halfKeySize, halfKeySize).ToArray();
        byte[] ciphertextCopy = ciphertext.ToArray();
        byte[] initializationVectorCopy = initializationVector.ToArray();
        byte[] additionalAuthenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        byte[] expectedTag = ComputeAuthenticationTag(
            additionalAuthenticatedDataCopy,
            initializationVectorCopy,
            ciphertextCopy,
            macKey,
            algorithm);
        CryptographicOperations.ZeroMemory(macKey);
        CryptographicOperations.ZeroMemory(
            additionalAuthenticatedDataCopy);
        bool authenticated = ConstantTime.AreEqual(
            authenticationTag,
            expectedTag);
        CryptographicOperations.ZeroMemory(expectedTag);
        if (!authenticated)
        {
            CryptographicOperations.ZeroMemory(encryptionKey);
            CryptographicOperations.ZeroMemory(ciphertextCopy);
            CryptographicOperations.ZeroMemory(initializationVectorCopy);
            return DecryptionFailure();
        }

        Task<byte[]> operation = Task.Run(() => DecryptCore(
            ciphertextCopy,
            initializationVectorCopy,
            encryptionKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(encryptionKey);
        CryptographicOperations.ZeroMemory(ciphertextCopy);
        CryptographicOperations.ZeroMemory(initializationVectorCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            return DecryptionFailure();
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = operation.Result;
        return result;
    }

    static AesCbcHmacContentEncryptionResult EncryptValidated(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string algorithm)
    {
        int halfKeySize = GetKeySize(algorithm) / 2;
        byte[] macKey = contentEncryptionKey.SymmetricKey
            .AsSpan(0, halfKeySize).ToArray();
        byte[] encryptionKey = contentEncryptionKey.SymmetricKey
            .AsSpan(halfKeySize, halfKeySize).ToArray();
        byte[] plaintextCopy = plaintext.ToArray();
        byte[] initializationVectorCopy = initializationVector.ToArray();
        byte[] additionalAuthenticatedDataCopy =
            additionalAuthenticatedData.ToArray();
        Task<byte[]> operation = Task.Run(() => EncryptCore(
            plaintextCopy,
            initializationVectorCopy,
            encryptionKey));
        Wait(operation);
        CryptographicOperations.ZeroMemory(encryptionKey);
        CryptographicOperations.ZeroMemory(plaintextCopy);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            CryptographicOperations.ZeroMemory(macKey);
            CryptographicOperations.ZeroMemory(initializationVectorCopy);
            CryptographicOperations.ZeroMemory(
                additionalAuthenticatedDataCopy);
            return Failure(
                "encryption_failed",
                "The AES-CBC provider could not encrypt the JWE content.");
        }

        byte[] ciphertext = operation.Result;
        byte[] authenticationTag = ComputeAuthenticationTag(
            additionalAuthenticatedDataCopy,
            initializationVectorCopy,
            ciphertext,
            macKey,
            algorithm);
        CryptographicOperations.ZeroMemory(macKey);
        CryptographicOperations.ZeroMemory(
            additionalAuthenticatedDataCopy);
        AesCbcHmacContentEncryptionResult result =
            new AesCbcHmacContentEncryptionResult();
        result.Success = true;
        result.Ciphertext = ciphertext;
        result.InitializationVector = initializationVectorCopy;
        result.AuthenticationTag = authenticationTag;
        return result;
    }

    static byte[] EncryptCore(
        byte[] plaintext,
        byte[] initializationVector,
        byte[] encryptionKey)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            return aes.EncryptCbc(
                plaintext,
                initializationVector,
                PaddingMode.PKCS7);
        }
    }

    static byte[] DecryptCore(
        byte[] ciphertext,
        byte[] initializationVector,
        byte[] encryptionKey)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            return aes.DecryptCbc(
                ciphertext,
                initializationVector,
                PaddingMode.PKCS7);
        }
    }

    static byte[] ComputeAuthenticationTag(
        byte[] additionalAuthenticatedData,
        byte[] initializationVector,
        byte[] ciphertext,
        byte[] macKey,
        string algorithm)
    {
        byte[] additionalDataLength = new byte[8];
        ulong bitLength = (ulong)additionalAuthenticatedData.Length * 8;
        int index = 7;
        while (index >= 0)
        {
            additionalDataLength[index] = (byte)(bitLength & 255);
            bitLength >>= 8;
            index -= 1;
        }

        byte[] input = new byte[
            additionalAuthenticatedData.Length
            + initializationVector.Length
            + ciphertext.Length
            + additionalDataLength.Length];
        int position = 0;
        additionalAuthenticatedData.CopyTo(input, position);
        position += additionalAuthenticatedData.Length;
        initializationVector.CopyTo(input, position);
        position += initializationVector.Length;
        ciphertext.CopyTo(input, position);
        position += ciphertext.Length;
        additionalDataLength.CopyTo(input, position);
        byte[] completeTag;
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes128CbcHmacSha256,
            StringComparison.Ordinal))
        {
            completeTag = HMACSHA256.HashData(macKey, input);
        }
        else if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192CbcHmacSha384,
            StringComparison.Ordinal))
        {
            completeTag = HMACSHA384.HashData(macKey, input);
        }
        else
        {
            completeTag = HMACSHA512.HashData(macKey, input);
        }

        byte[] authenticationTag = completeTag
            .AsSpan(0, GetTagSize(algorithm)).ToArray();
        CryptographicOperations.ZeroMemory(additionalDataLength);
        CryptographicOperations.ZeroMemory(input);
        CryptographicOperations.ZeroMemory(completeTag);
        return authenticationTag;
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
                "The content-encryption key does not match the AES-CBC-HMAC-SHA2 algorithm size.");
        }

        if (contentEncryptionKey.Algorithm != null
            && !string.Equals(
                contentEncryptionKey.Algorithm,
                algorithm,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match the requested AES-CBC-HMAC-SHA2 algorithm.");
        }

        if (contentEncryptionKey.HasCoseAlgorithm)
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "COSE does not define an algorithm equivalent to JWE AES-CBC-HMAC-SHA2.");
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
                "The requested content operation is not permitted by the key metadata.");
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
                "The requested content operation is not permitted by the COSE key metadata.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(
                algorithm,
                JoseAlgorithms.Aes128CbcHmacSha256,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes192CbcHmacSha384,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes256CbcHmacSha512,
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
            "The JOSE algorithm is not AES-CBC-HMAC-SHA2 content encryption.");
    }

    static int GetKeySize(string algorithm)
    {
        if (string.Equals(
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

        return 64;
    }

    static int GetTagSize(string algorithm)
    {
        return GetKeySize(algorithm) / 2;
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

    static TokenBytesResult DecryptionFailure()
    {
        return BytesFailure(
            "decryption_failed",
            "The JWE content could not be authenticated and decrypted.");
    }

    static AesCbcHmacContentEncryptionResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static AesCbcHmacContentEncryptionResult Failure(TokenError? error)
    {
        AesCbcHmacContentEncryptionResult result =
            new AesCbcHmacContentEncryptionResult();
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
