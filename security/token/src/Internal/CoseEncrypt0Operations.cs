using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Creates and decrypts RFC 9052 COSE_Encrypt0 messages.</summary>
internal static class CoseEncrypt0Operations
{
    /// <summary>Encrypts plaintext into a tagged COSE_Encrypt0 message.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="header">The content headers, including a protected content algorithm.</param>
    /// <param name="key">The symmetric content-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The tagged encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] plaintext,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        return Create(
            plaintext,
            header,
            key,
            true,
            policy);
    }

    /// <summary>Encrypts plaintext with explicit registered-tag control.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="header">The content headers, including a protected content algorithm.</param>
    /// <param name="key">The symmetric content-encryption key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] plaintext,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        CoseDetachedEncryptionResult result = CreateCore(
            plaintext,
            Array.Empty<byte>(),
            header,
            key,
            tagged,
            false,
            policy);
        return result.Success
            ? BytesSuccess(result.Bytes)
            : BytesFailure(result.Error);
    }

    /// <summary>Encrypts attached plaintext with external authenticated data.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The content headers.</param>
    /// <param name="key">The symmetric encryption key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        CoseDetachedEncryptionResult result = CreateCore(
            plaintext,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            false,
            policy);
        return result.Success
            ? BytesSuccess(result.Bytes)
            : BytesFailure(result.Error);
    }

    /// <summary>Encrypts plaintext and transports the resulting ciphertext separately.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The content headers.</param>
    /// <param name="key">The symmetric encryption key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The encoded message and detached ciphertext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseDetachedEncryptionResult CreateDetached(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            plaintext,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            true,
            policy);
    }

    static CoseDetachedEncryptionResult CreateCore(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool detached,
        TokenPolicy policy)
    {
        ValidateCreateReferences(plaintext, header, key, policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return DetachedFailure(policyResult.Error);
        }

        TokenResult headerResult = ValidateHeader(header, policy);
        if (!headerResult.Success)
        {
            return DetachedFailure(headerResult.Error);
        }

        TokenBytesResult initializationVector = ResolveInitializationVector(
            header,
            key,
            header.Algorithm);
        if (!initializationVector.Success)
        {
            return DetachedFailure(initializationVector.Error);
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!protectedBytes.Success)
        {
            return DetachedFailure(protectedBytes.Error);
        }

        TokenBytesResult authenticatedData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            policy);
        if (!authenticatedData.Success)
        {
            return DetachedFailure(authenticatedData.Error);
        }

        CoseEncrypt0EncryptionResult encrypted = EncryptContent(
            plaintext,
            initializationVector.Bytes,
            authenticatedData.Bytes,
            key,
            header.Algorithm);
        if (!encrypted.Success)
        {
            return DetachedFailure(encrypted.Error);
        }

        if (header.InitializationVector == null
            && header.PartialInitializationVector == null)
        {
            header.SetUnprotected(
                CoseHeaderLabels.InitializationVector,
                BytesItem(encrypted.InitializationVector));
        }

        CoseEncrypt0 message = new CoseEncrypt0();
        message.Header = header;
        message.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        message.Ciphertext = detached ? null : encrypted.Ciphertext;
        message.DetachedCiphertext = detached;
        message.Tagged = tagged;
        TokenBytesResult written = message.WriteWithTagAndPolicy(tagged, policy);
        if (!written.Success)
        {
            Clear(encrypted.Ciphertext);
            return DetachedFailure(written.Error);
        }

        CoseDetachedEncryptionResult result =
            new CoseDetachedEncryptionResult();
        result.Success = true;
        result.Bytes = written.Bytes;
        result.Ciphertext = detached
            ? encrypted.Ciphertext
            : null;
        return result;
    }

    /// <summary>Authenticates and decrypts an attached COSE_Encrypt0 message.</summary>
    /// <param name="cbor">The tagged or contextually untagged message bytes.</param>
    /// <param name="key">The symmetric content-decryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message with authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncrypt0Result Decrypt(
        byte[] cbor,
        TokenKey key,
        TokenPolicy policy)
    {
        return DecryptCore(
            cbor,
            null,
            Array.Empty<byte>(),
            key,
            false,
            policy);
    }

    /// <summary>Decrypts attached ciphertext with external authenticated data.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The symmetric decryption key.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncrypt0Result DecryptWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey key,
        TokenPolicy policy)
    {
        return DecryptCore(
            cbor,
            null,
            externalAuthenticatedData,
            key,
            false,
            policy);
    }

    /// <summary>Decrypts separately transported ciphertext with external authenticated data.</summary>
    /// <param name="cbor">The encoded message containing a null ciphertext field.</param>
    /// <param name="ciphertext">The detached ciphertext and authentication tag.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The symmetric decryption key.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncrypt0Result DecryptDetached(
        byte[] cbor,
        byte[] ciphertext,
        byte[] externalAuthenticatedData,
        TokenKey key,
        TokenPolicy policy)
    {
        return DecryptCore(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            key,
            true,
            policy);
    }

    static CoseEncrypt0Result DecryptCore(
        byte[] cbor,
        byte[]? detachedCiphertext,
        byte[] externalAuthenticatedData,
        TokenKey key,
        bool detached,
        TokenPolicy policy)
    {
        ValidateDecryptReferences(cbor, key, policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedCiphertext == null)
        {
            throw new ArgumentNullException("detachedCiphertext");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseEncrypt0Result parsed = CoseEncrypt0.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedCiphertext && !detached)
        {
            return Failure(
                "missing_ciphertext",
                "Attached COSE_Encrypt0 decryption requires ciphertext bytes.");
        }

        if (!parsed.Message.DetachedCiphertext && detached)
        {
            return Failure(
                "unexpected_attached_ciphertext",
                "Detached COSE_Encrypt0 decryption requires a null ciphertext field.");
        }

        TokenResult headerResult = ValidateHeader(
            parsed.Message.Header,
            policy);
        if (!headerResult.Success)
        {
            return Failure(headerResult.Error);
        }

        TokenBytesResult initializationVector = ResolveInitializationVector(
            parsed.Message.Header,
            key,
            (parsed.Message.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm);
        if (!initializationVector.Success)
        {
            return Failure(initializationVector.Error);
        }

        int algorithm = parsed.Message.Header.Algorithm;
        int tagSize = GetAuthenticationTagSize(algorithm);
        byte[]? combined = detached
            ? detachedCiphertext
            : parsed.Message.Ciphertext;
        if ((combined ?? throw new InvalidOperationException("A required value is null.")).Length < tagSize
            || initializationVector.Bytes == null)
        {
            return Failure(
                "decryption_failed",
                "The COSE_Encrypt0 content could not be authenticated and decrypted.");
        }

        int ciphertextLength = combined.Length - tagSize;
        byte[] ciphertext = new byte[ciphertextLength];
        byte[] authenticationTag = new byte[tagSize];
        combined.AsSpan(0, ciphertextLength).CopyTo(
            ciphertext);
        combined.AsSpan(ciphertextLength).CopyTo(
            authenticationTag);
        TokenBytesResult authenticatedData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            parsed.Message.Header.ProtectedBytes,
            externalAuthenticatedData,
            policy);
        if (!authenticatedData.Success)
        {
            Clear(ciphertext);
            Clear(authenticationTag);
            return Failure(authenticatedData.Error);
        }

        TokenBytesResult plaintext = IsAesGcm(algorithm)
            ? CoseAesGcm.Decrypt(
                ciphertext,
                initializationVector.Bytes,
                authenticationTag,
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm)
            : CoseAesCcm.Decrypt(
                ciphertext,
                initializationVector.Bytes,
                authenticationTag,
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm);
        Clear(ciphertext);
        Clear(authenticationTag);
        if (!plaintext.Success)
        {
            return Failure(plaintext.Error);
        }

        if (detached)
        {
            parsed.Message.Ciphertext = Copy(detachedCiphertext);
        }

        parsed.Message.Plaintext = plaintext.Bytes;
        parsed.Message.PlaintextAvailable = true;
        parsed.Message.AuthenticationTagVerified = true;
        return parsed;
    }

    static CoseEncrypt0EncryptionResult EncryptContent(
        byte[] plaintext,
        byte[]? initializationVector,
        byte[]? authenticatedData,
        TokenKey key,
        int algorithm)
    {
        if (IsAesGcm(algorithm))
        {
            CoseAesGcmResult encrypted = initializationVector == null
                ? CoseAesGcm.Encrypt(
                    plaintext,
                    (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    key,
                    algorithm)
                : CoseAesGcm.Encrypt(
                    plaintext,
                    initializationVector,
                    (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    key,
                    algorithm);
            return encrypted.Success
                ? EncryptionSuccess(
                    Join(encrypted.Ciphertext, encrypted.AuthenticationTag),
                    encrypted.InitializationVector)
                : EncryptionFailure(encrypted.Error);
        }

        CoseAesCcmResult ccm = initializationVector == null
            ? CoseAesCcm.Encrypt(
                plaintext,
                (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm)
            : CoseAesCcm.Encrypt(
                plaintext,
                initializationVector,
                (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm);
        return ccm.Success
            ? EncryptionSuccess(
                Join(ccm.Ciphertext, ccm.AuthenticationTag),
                ccm.InitializationVector)
            : EncryptionFailure(ccm.Error);
    }

    static TokenResult ValidateHeader(
        CoseHeader? header,
        TokenPolicy policy)
    {
        TokenResult critical = CoseCriticalHeaderValidator.Validate((header ?? throw new InvalidOperationException("A required value is null.")));
        if (!critical.Success)
        {
            return critical;
        }

        if (header.Get(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "missing_cose_algorithm",
                "COSE_Encrypt0 requires a content encryption algorithm.");
        }

        if (header.GetProtected(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE_Encrypt0 content encryption algorithm must be protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE_Encrypt0 content encryption algorithm must use an integer label.");
        }

        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(
                header.Algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsAesGcm(header.Algorithm)
            && !IsAesCcm(header.Algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented content encryption algorithm.");
        }

        if (!IsAllowed(header.Algorithm, policy.AllowedAlgorithms))
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The COSE content encryption algorithm is not allowed by policy.");
        }

        if (header.InitializationVector != null
            && header.PartialInitializationVector != null)
        {
            return ValidationFailure(
                "conflicting_initialization_vectors",
                "A COSE header cannot contain both full and partial initialization vectors.");
        }

        return ValidationSuccess();
    }

    static TokenBytesResult ResolveInitializationVector(
        CoseHeader? header,
        TokenKey key,
        int algorithm)
    {
        if ((header ?? throw new InvalidOperationException("A required value is null.")).PartialInitializationVector == null)
        {
            return BytesSuccess(header.InitializationVector);
        }

        if (key.BaseInitializationVector == null)
        {
            return BytesFailure(new TokenError(
                "missing_base_initialization_vector",
                "A COSE Partial IV requires a Base IV associated with the key.",
                -1));
        }

        int size = GetInitializationVectorSize(algorithm);
        if (key.BaseInitializationVector.Length != size)
        {
            return BytesFailure(new TokenError(
                "invalid_base_initialization_vector",
                "The COSE Base IV does not have the size required by the algorithm.",
                -1));
        }

        if ((header.PartialInitializationVector ?? throw new InvalidOperationException("A required value is null.")).Length > size)
        {
            return BytesFailure(new TokenError(
                "invalid_partial_initialization_vector",
                "The COSE Partial IV is longer than the algorithm initialization vector.",
                -1));
        }

        byte[] result = Copy(key.BaseInitializationVector);
        int source = header.PartialInitializationVector.Length - 1;
        int target = result.Length - 1;
        while (source >= 0)
        {
            result[target] ^= header.PartialInitializationVector[source];
            source--;
            target--;
        }

        return BytesSuccess(result);
    }

    static bool IsAesGcm(int algorithm)
    {
        return algorithm == CoseAlgorithms.Aes128Gcm
            || algorithm == CoseAlgorithms.Aes192Gcm
            || algorithm == CoseAlgorithms.Aes256Gcm;
    }

    static bool IsAesCcm(int algorithm)
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

    static int GetAuthenticationTagSize(int algorithm)
    {
        if (IsAesGcm(algorithm)
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm16_128_256
            || algorithm == CoseAlgorithms.AesCcm64_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_256)
        {
            return 16;
        }

        return 8;
    }

    static int GetInitializationVectorSize(int algorithm)
    {
        if (IsAesGcm(algorithm))
        {
            return 12;
        }

        if (algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm16_128_256)
        {
            return 13;
        }

        return 7;
    }

    static bool IsAllowed(int algorithm, string[]? allowed)
    {
        string? coseName = CoseAlgorithmsOperations.GetName(algorithm);
        if (RegistryLookup.Contains(allowed, coseName, false))
        {
            return true;
        }

        AlgorithmMappingResult mapping =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        return mapping.Mapped
            && RegistryLookup.Contains(
                allowed,
                mapping.JoseName,
                false);
    }

    static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Encrypt0 cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] plaintext,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    static void ValidateDecryptReferences(
        byte[] cbor,
        TokenKey key,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    static byte[] Join(byte[]? first, byte[]? second)
    {
        byte[] result = new byte[(first ?? throw new InvalidOperationException("A required value is null.")).Length + (second ?? throw new InvalidOperationException("A required value is null.")).Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        Clear(first);
        Clear(second);
        return result;
    }

    static byte[] Copy(byte[]? value)
    {
        return value.AsSpan().ToArray();
    }

    static void Clear(byte[]? value)
    {
        if (value != null)
        {
            CryptographicOperations.ZeroMemory(value);
        }
    }

    static CborItem BytesItem(byte[]? value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
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

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesSuccess(byte[]? bytes)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    static CoseDetachedEncryptionResult DetachedFailure(TokenError? error)
    {
        CoseDetachedEncryptionResult result =
            new CoseDetachedEncryptionResult();
        result.Error = error;
        return result;
    }

    static CoseEncrypt0EncryptionResult EncryptionSuccess(
        byte[] ciphertext,
        byte[]? initializationVector)
    {
        CoseEncrypt0EncryptionResult result =
            new CoseEncrypt0EncryptionResult();
        result.Success = true;
        result.Ciphertext = ciphertext;
        result.InitializationVector = initializationVector;
        return result;
    }

    static CoseEncrypt0EncryptionResult EncryptionFailure(
        TokenError? error)
    {
        CoseEncrypt0EncryptionResult result =
            new CoseEncrypt0EncryptionResult();
        result.Error = error;
        return result;
    }

    static CoseEncrypt0Result Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseEncrypt0Result Failure(TokenError? error)
    {
        CoseEncrypt0Result result = new CoseEncrypt0Result();
        result.Error = error;
        return result;
    }
}
