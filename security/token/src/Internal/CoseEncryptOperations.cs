using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Creates and decrypts RFC 9052 COSE_Encrypt messages.</summary>
internal static class CoseEncryptOperations
{
    /// <summary>Encrypts plaintext for one or more recipients.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="header">The content headers, including a protected content algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient keys in recipient order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The tagged encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] plaintext,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Create(
            plaintext,
            header,
            recipientHeaders,
            keys,
            null,
            true,
            policy);
    }

    /// <summary>Encrypts plaintext with explicit registered-tag control.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="header">The content headers, including a protected content algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient keys in recipient order.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 96.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] plaintext,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        bool tagged,
        TokenPolicy policy)
    {
        return Create(
            plaintext,
            header,
            recipientHeaders,
            keys,
            null,
            tagged,
            policy);
    }

    /// <summary>Encrypts plaintext with optional static ECDH sender keys and explicit tag control.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="header">The content headers, including a protected content algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient public or symmetric keys in recipient order.</param>
    /// <param name="senderKeys">Static sender private keys in recipient order, or <c>null</c> for ephemeral ECDH.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 96.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] plaintext,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        CoseDetachedEncryptionResult result = CreateCore(
            plaintext,
            Array.Empty<byte>(),
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            false,
            policy);
        return result.Success
            ? BytesSuccess(result.Bytes)
            : BytesFailure(result.Error);
    }

    /// <summary>Encrypts attached plaintext with external authenticated data.</summary>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        CoseDetachedEncryptionResult result = CreateCore(
            plaintext,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            false,
            policy);
        return result.Success
            ? BytesSuccess(result.Bytes)
            : BytesFailure(result.Error);
    }

    /// <summary>Encrypts plaintext and transports the resulting ciphertext separately.</summary>
    internal static CoseDetachedEncryptionResult CreateDetached(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            plaintext,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            true,
            policy);
    }

    static CoseDetachedEncryptionResult CreateCore(
        byte[] plaintext,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        bool detached,
        TokenPolicy policy)
    {
        ValidateCreateReferences(
            plaintext,
            header,
            recipientHeaders,
            keys,
            policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }
        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return DetachedFailure(policyResult.Error);
        }

        if (recipientHeaders.Length == 0
            || recipientHeaders.Length != keys.Length
            || senderKeys != null
                && senderKeys.Length != keys.Length)
        {
            return DetachedFailure(
                "recipient_count_mismatch",
                "Recipient headers and keys must have the same nonzero length.");
        }

        TokenResult contentHeader = ValidateContentHeader(header, policy);
        if (!contentHeader.Success)
        {
            return DetachedFailure(contentHeader.Error);
        }

        TokenBytesResult bodyProtected = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!bodyProtected.Success)
        {
            return DetachedFailure(bodyProtected.Error);
        }

        int contentAlgorithm = header.Algorithm;
        int keySize = GetContentKeySize(contentAlgorithm);
        byte[]? contentKey = null;
        CoseRecipient[] recipients =
            new CoseRecipient[recipientHeaders.Length];
        int index = 0;
        while (index < recipientHeaders.Length)
        {
            if (recipientHeaders[index] == null)
            {
                Clear(contentKey);
                return DetachedFailure(
                    "invalid_cose_recipient_header",
                    "A COSE recipient header is missing.");
            }

            if (keys[index] == null)
            {
                Clear(contentKey);
                return DetachedFailure(
                    "invalid_key",
                    "A COSE recipient key is missing.");
            }

            TokenResult recipientHeader = ValidateRecipientHeader(
                recipientHeaders[index],
                policy);
            if (!recipientHeader.Success)
            {
                Clear(contentKey);
                return DetachedFailure(recipientHeader.Error);
            }

            int recipientAlgorithm = recipientHeaders[index].Algorithm;
            if (IsDirectRecipient(recipientAlgorithm)
                && recipientHeaders.Length != 1)
            {
                Clear(contentKey);
                return DetachedFailure(
                    "direct_recipient_must_be_only_recipient",
                    "Direct key distribution requires exactly one recipient.");
            }

            TokenBytesResult recipientProtected =
                CoseSign.GetProtectedBytes(
                    recipientHeaders[index],
                    policy);
            if (!recipientProtected.Success)
            {
                Clear(contentKey);
                return DetachedFailure(recipientProtected.Error);
            }

            CoseKdfContextParameters context = Context(
                recipientHeaders[index],
                recipientProtected.Bytes,
                recipientAlgorithm,
                contentAlgorithm,
                keySize);
            byte[]? salt = HeaderBytes(
                recipientHeaders[index],
                CoseHeaderLabels.Salt);
            CoseRecipient recipient = new CoseRecipient();
            recipient.Header = recipientHeaders[index];
            TokenError? managedError = null;
            if (IsDirectDistribution(recipientAlgorithm))
            {
                CoseDirectKeyDistributionResult managed =
                    CoseDirectKeyDistributionOperations.Encrypt(
                        keys[index],
                        recipientAlgorithm,
                        salt,
                        context);
                if (managed.Success)
                {
                    contentKey = managed.ContentKey;
                    recipient.EncryptedKey = managed.EncryptedKey;
                }
                else
                {
                    managedError = managed.Error;
                }
            }
            else
            {
                if (contentKey == null)
                {
                    contentKey = RandomBytes.Generate(keySize);
                }

                if (IsAesKeyWrap(recipientAlgorithm))
                {
                    TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
                        contentKey,
                        keys[index],
                        recipientAlgorithm);
                    if (wrapped.Success)
                    {
                        recipient.EncryptedKey = wrapped.Bytes;
                    }
                    else
                    {
                        managedError = wrapped.Error;
                    }
                }
                else
                {
                    TokenKey? senderKey = senderKeys == null
                        ? null
                        : senderKeys[index];
                    CoseEcdhResult ecdh = IsEcdhKeyWrap(
                        recipientAlgorithm)
                        ? CoseEcdhAlgorithms.Wrap(
                            contentKey,
                            keys[index],
                            senderKey,
                            recipientAlgorithm,
                            salt,
                            context)
                        : CoseEcdhAlgorithms.Derive(
                            keys[index],
                            senderKey,
                            recipientAlgorithm,
                            salt,
                            context);
                    if (ecdh.Success)
                    {
                        contentKey = ReplaceContentKey(
                            contentKey,
                            ecdh.ContentKey,
                            IsEcdhDirect(recipientAlgorithm));
                        recipient.EncryptedKey = ecdh.EncryptedKey;
                        TokenResult sender = AddSenderPublicKey(
                            recipient.Header,
                            ecdh.SenderPublicKey);
                        if (!sender.Success)
                        {
                            managedError = sender.Error;
                        }
                    }
                    else
                    {
                        managedError = ecdh.Error;
                    }
                }
            }

            if (managedError != null)
            {
                Clear(contentKey);
                return DetachedFailure(managedError);
            }

            recipients[index] = recipient;
            index += 1;
        }

        TokenBytesResult authenticatedData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt,
            (bodyProtected.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            policy);
        if (!authenticatedData.Success)
        {
            Clear(contentKey);
            return DetachedFailure(authenticatedData.Error);
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.ImportCose(
            (contentKey ?? throw new InvalidOperationException("A required value is null.")),
            contentAlgorithm);
        if (!imported.Success)
        {
            Clear(contentKey);
            return DetachedFailure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Use = TokenKeyUses.Encryption;
        byte[]? initializationVector = header.InitializationVector;
        CoseContentEncryptionResult encrypted = EncryptContent(
            plaintext,
            initializationVector,
            authenticatedData.Bytes,
            imported.Key,
            contentAlgorithm);
        imported.Key.ReleaseOwnedMaterial();
        Clear(contentKey);
        if (!encrypted.Success)
        {
            return DetachedFailure(encrypted.Error);
        }

        if (header.InitializationVector == null)
        {
            header.SetUnprotected(
                CoseHeaderLabels.InitializationVector,
                BytesItem(encrypted.InitializationVector));
        }

        CoseEncrypt message = new CoseEncrypt();
        message.Header = header;
        message.Header.ProtectedBytes = Copy(bodyProtected.Bytes);
        message.Ciphertext = detached ? null : encrypted.Ciphertext;
        message.DetachedCiphertext = detached;
        message.Recipients = recipients;
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

    /// <summary>Decrypts one selected recipient and authenticates the shared ciphertext.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt message.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message with authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseEncryptResult Decrypt(
        byte[] cbor,
        int recipientIndex,
        TokenKey key,
        TokenPolicy policy)
    {
        return Decrypt(cbor, recipientIndex, key, null, policy);
    }

    /// <summary>Decrypts one selected recipient with an optional static ECDH sender public key.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt message.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient private or symmetric key.</param>
    /// <param name="senderKey">The static ECDH sender public key, or <c>null</c> for ephemeral ECDH.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message with authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference argument is null.</exception>
    internal static CoseEncryptResult Decrypt(
        byte[] cbor,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return DecryptSelectedCore(
            cbor,
            null,
            Array.Empty<byte>(),
            recipientIndex,
            key,
            senderKey,
            false,
            policy);
    }

    /// <summary>Decrypts one recipient of attached ciphertext with external authenticated data.</summary>
    internal static CoseEncryptResult DecryptWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return DecryptSelectedCore(
            cbor,
            null,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            false,
            policy);
    }

    /// <summary>Decrypts one recipient using separately transported ciphertext.</summary>
    internal static CoseEncryptResult DecryptDetached(
        byte[] cbor,
        byte[] ciphertext,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return DecryptSelectedCore(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            true,
            policy);
    }

    static CoseEncryptResult DecryptSelectedCore(
        byte[] cbor,
        byte[]? detachedCiphertext,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        bool detached,
        TokenPolicy policy)
    {
        ValidateDecryptReferences(cbor, key, policy);
        ValidateDetachedReferences(
            detachedCiphertext,
            externalAuthenticatedData,
            detached);
        CoseEncryptResult parsed = ParseForDecryption(
            cbor,
            detached,
            policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (recipientIndex < 0
            || recipientIndex >= (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            return Failure(
                "recipient_index_out_of_range",
                "The selected COSE recipient index is outside the recipients array.");
        }

        TokenBytesResult contentKey = RecoverContentKey(
            parsed.Message,
            parsed.Message.Recipients[recipientIndex],
            key,
            senderKey,
            policy);
        if (!contentKey.Success)
        {
            return Failure(contentKey.Error);
        }

        TokenBytesResult plaintext = DecryptContent(
            parsed.Message,
            contentKey.Bytes,
            detached ? detachedCiphertext : parsed.Message.Ciphertext,
            externalAuthenticatedData,
            policy);
        Clear(contentKey.Bytes);
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
        parsed.Message.Recipients[recipientIndex].Decrypted = true;
        return parsed;
    }

    /// <summary>Decrypts and authenticates every top-level recipient with matching keys.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt message.</param>
    /// <param name="keys">Recipient keys corresponding to serialization order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message with plaintext only when every recipient succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncryptResult Decrypt(
        byte[] cbor,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Decrypt(cbor, keys, null, policy);
    }

    /// <summary>Decrypts every recipient with optional static ECDH sender public keys.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt message.</param>
    /// <param name="keys">Recipient keys corresponding to serialization order.</param>
    /// <param name="senderKeys">Static sender public keys in recipient order, or <c>null</c>.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message with plaintext only when every recipient succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static CoseEncryptResult Decrypt(
        byte[] cbor,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return DecryptAllCore(
            cbor,
            null,
            Array.Empty<byte>(),
            keys,
            senderKeys,
            false,
            policy);
    }

    /// <summary>Decrypts all recipients of attached ciphertext with external authenticated data.</summary>
    internal static CoseEncryptResult DecryptWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return DecryptAllCore(
            cbor,
            null,
            externalAuthenticatedData,
            keys,
            senderKeys,
            false,
            policy);
    }

    /// <summary>Decrypts all recipients using separately transported ciphertext.</summary>
    internal static CoseEncryptResult DecryptDetached(
        byte[] cbor,
        byte[] ciphertext,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return DecryptAllCore(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            keys,
            senderKeys,
            true,
            policy);
    }

    static CoseEncryptResult DecryptAllCore(
        byte[] cbor,
        byte[]? detachedCiphertext,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool detached,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        ValidateDetachedReferences(
            detachedCiphertext,
            externalAuthenticatedData,
            detached);
        CoseEncryptResult parsed = ParseForDecryption(
            cbor,
            detached,
            policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (keys.Length != (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length
            || senderKeys != null
                && senderKeys.Length != keys.Length)
        {
            return Failure(
                "recipient_count_mismatch",
                "The key count must match the COSE recipient count.");
        }

        byte[]? contentKey = null;
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                Clear(contentKey);
                return Failure(
                    "invalid_key",
                    "A COSE recipient key is missing.");
            }

            TokenBytesResult recovered = RecoverContentKey(
                parsed.Message,
                parsed.Message.Recipients[index],
                keys[index],
                senderKeys == null ? null : senderKeys[index],
                policy);
            if (!recovered.Success)
            {
                Clear(contentKey);
                return Failure(recovered.Error);
            }

            if (contentKey == null)
            {
                contentKey = recovered.Bytes;
            }
            else
            {
                bool same = CryptographicOperations.FixedTimeEquals(
                    contentKey,
                    recovered.Bytes);
                Clear(recovered.Bytes);
                if (!same)
                {
                    Clear(contentKey);
                    return Failure(
                        "recipient_content_key_mismatch",
                        "COSE recipients did not recover the same content key.");
                }
            }

            index += 1;
        }

        TokenBytesResult plaintext = DecryptContent(
            parsed.Message,
            contentKey,
            detached ? detachedCiphertext : parsed.Message.Ciphertext,
            externalAuthenticatedData,
            policy);
        Clear(contentKey);
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
        index = 0;
        while (index < parsed.Message.Recipients.Length)
        {
            parsed.Message.Recipients[index].Decrypted = true;
            index += 1;
        }

        return parsed;
    }

    static CoseEncryptResult ParseForDecryption(
        byte[] cbor,
        bool detached,
        TokenPolicy policy)
    {
        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseEncryptResult parsed = CoseEncrypt.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedCiphertext && !detached)
        {
            return Failure(
                "missing_ciphertext",
                "Attached COSE_Encrypt decryption requires ciphertext bytes.");
        }

        if (!parsed.Message.DetachedCiphertext && detached)
        {
            return Failure(
                "unexpected_attached_ciphertext",
                "Detached COSE_Encrypt decryption requires a null ciphertext field.");
        }

        TokenResult contentHeader = ValidateContentHeader(
            parsed.Message.Header,
            policy);
        if (!contentHeader.Success)
        {
            return Failure(contentHeader.Error);
        }

        int index = 0;
        while (index < parsed.Message.Recipients.Length)
        {
            CoseRecipient recipient = parsed.Message.Recipients[index];
            if (recipient.Recipients.Length != 0)
            {
                return Failure(
                    "nested_recipient_processing_not_supported",
                    "Cryptographic processing of nested COSE recipients is not supported.");
            }

            if (recipient.EncryptedKeyAbsent)
            {
                return Failure(
                    "invalid_encrypted_key",
                    "A cryptographically processed COSE recipient requires an encrypted-key byte string.");
            }

            TokenResult recipientHeader = ValidateRecipientHeader(
                recipient.Header,
                policy);
            if (!recipientHeader.Success)
            {
                return Failure(recipientHeader.Error);
            }

            if (IsDirectRecipient((recipient.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm)
                && parsed.Message.Recipients.Length != 1)
            {
                return Failure(
                    "direct_recipient_must_be_only_recipient",
                    "Direct key distribution requires exactly one recipient.");
            }

            index += 1;
        }

        return parsed;
    }

    static TokenBytesResult RecoverContentKey(
        CoseEncrypt message,
        CoseRecipient recipient,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        int recipientAlgorithm = (recipient.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        int contentAlgorithm = (message.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        int keySize = GetContentKeySize(contentAlgorithm);
        CoseKdfContextParameters context = Context(
            recipient.Header,
            recipient.Header.ProtectedBytes,
            recipientAlgorithm,
            contentAlgorithm,
            keySize);
        byte[]? salt = HeaderBytes(
            recipient.Header,
            CoseHeaderLabels.Salt);
        if (IsDirectDistribution(recipientAlgorithm))
        {
            CoseDirectKeyDistributionResult direct =
                CoseDirectKeyDistributionOperations.Decrypt(
                    (recipient.EncryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                    key,
                    recipientAlgorithm,
                    salt,
                    context);
            return direct.Success
                ? BytesSuccess(direct.ContentKey)
                : BytesFailure(direct.Error);
        }

        if (IsAesKeyWrap(recipientAlgorithm))
        {
            return CoseAesKeyWrap.Unwrap(
                (recipient.EncryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                recipientAlgorithm);
        }

        TokenKey? actualSender = senderKey;
        bool releaseSender = false;
        if (IsEcdhEphemeral(recipientAlgorithm))
        {
            CoseKeyResult parsedSender = ReadSenderPublicKey(
                recipient.Header,
                policy);
            if (!parsedSender.Success)
            {
                return BytesFailure(parsedSender.Error);
            }

            actualSender = parsedSender.Key;
            releaseSender = true;
        }
        else if (actualSender == null)
        {
            return BytesFailure(
                "missing_sender_key",
                "Static-static ECDH decryption requires the sender public key.");
        }

        CoseEcdhResult ecdh = CoseEcdhAlgorithms.Decrypt(
            (recipient.EncryptedKey ?? throw new InvalidOperationException("A required value is null.")),
            key,
            (actualSender ?? throw new InvalidOperationException("A required value is null.")),
            recipientAlgorithm,
            salt,
            context);
        if (releaseSender)
        {
            actualSender.ReleaseOwnedMaterial();
        }

        return ecdh.Success
            ? BytesSuccess(ecdh.ContentKey)
            : BytesFailure(ecdh.Error);
    }

    static TokenBytesResult DecryptContent(
        CoseEncrypt message,
        byte[]? contentKey,
        byte[]? combinedCiphertext,
        byte[] externalAuthenticatedData,
        TokenPolicy policy)
    {
        int algorithm = (message.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        int tagSize = GetAuthenticationTagSize(algorithm);
        if ((combinedCiphertext ?? throw new InvalidOperationException("A required value is null.")).Length < tagSize
            || message.Header.InitializationVector == null)
        {
            return BytesFailure(
                "decryption_failed",
                "The COSE content could not be authenticated and decrypted.");
        }

        int ciphertextLength = combinedCiphertext.Length - tagSize;
        byte[] ciphertext = new byte[ciphertextLength];
        byte[] tag = new byte[tagSize];
        combinedCiphertext.AsSpan(0, ciphertextLength).CopyTo(ciphertext);
        combinedCiphertext.AsSpan(ciphertextLength).CopyTo(tag);
        TokenBytesResult authenticatedData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt,
            message.Header.ProtectedBytes,
            externalAuthenticatedData,
            policy);
        if (!authenticatedData.Success)
        {
            Clear(ciphertext);
            Clear(tag);
            return authenticatedData;
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.ImportCose(
            (contentKey ?? throw new InvalidOperationException("A required value is null.")),
            algorithm);
        if (!imported.Success)
        {
            Clear(ciphertext);
            Clear(tag);
            return BytesFailure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Use = TokenKeyUses.Encryption;
        TokenBytesResult result = IsAesGcm(algorithm)
            ? CoseAesGcm.Decrypt(
                ciphertext,
                message.Header.InitializationVector,
                tag,
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                imported.Key,
                algorithm)
            : CoseAesCcm.Decrypt(
                ciphertext,
                message.Header.InitializationVector,
                tag,
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                imported.Key,
                algorithm);
        imported.Key.ReleaseOwnedMaterial();
        Clear(ciphertext);
        Clear(tag);
        return result;
    }

    static CoseContentEncryptionResult EncryptContent(
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
                ? ContentSuccess(
                    Join(encrypted.Ciphertext, encrypted.AuthenticationTag),
                    encrypted.InitializationVector)
                : ContentFailure(encrypted.Error);
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
            ? ContentSuccess(
                Join(ccm.Ciphertext, ccm.AuthenticationTag),
                ccm.InitializationVector)
            : ContentFailure(ccm.Error);
    }

    static TokenResult ValidateContentHeader(
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
                "COSE_Encrypt requires a content encryption algorithm.");
        }

        if (header.GetProtected(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE content encryption algorithm must be protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE content encryption algorithm must use an integer label.");
        }

        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(
                header.Algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsContentAlgorithm(header.Algorithm))
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

        if (header.PartialInitializationVector != null)
        {
            return ValidationFailure(
                "partial_initialization_vector_not_supported",
                "COSE_Encrypt operations require a full initialization vector.");
        }

        return ValidationSuccess();
    }

    internal static TokenResult ValidateRecipientHeader(
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
                "A COSE recipient requires an algorithm header.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE recipient algorithm must use an integer label.");
        }

        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(
                header.Algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsRecipientAlgorithm(header.Algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented recipient algorithm.");
        }

        if (!IsAllowed(header.Algorithm, policy.AllowedAlgorithms))
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The COSE recipient algorithm is not allowed by policy.");
        }

        return ValidationSuccess();
    }

    internal static CoseKdfContextParameters Context(
        CoseHeader header,
        byte[]? protectedBytes,
        int recipientAlgorithm,
        int contentAlgorithm,
        int contentKeySize)
    {
        int targetAlgorithm = IsEcdhKeyWrap(recipientAlgorithm)
            ? EcdhKeyWrapAlgorithm(recipientAlgorithm)
            : contentAlgorithm;
        CoseKdfContextParameters result =
            new CoseKdfContextParameters(
                targetAlgorithm,
                (ulong)contentKeySize * 8UL);
        if (IsEcdhKeyWrap(recipientAlgorithm))
        {
            result.KeyDataLength =
                (ulong)GetKeyWrapSize(targetAlgorithm) * 8UL;
        }

        result.ProtectedParameters = Copy(protectedBytes);
        result.PartyUInfo.Identity = HeaderBytes(
            header,
            CoseHeaderLabels.PartyUIdentity);
        result.PartyUInfo.Nonce = header.Get(
            CoseHeaderLabels.PartyUNonce);
        result.PartyUInfo.Other = HeaderBytes(
            header,
            CoseHeaderLabels.PartyUOther);
        result.PartyVInfo.Identity = HeaderBytes(
            header,
            CoseHeaderLabels.PartyVIdentity);
        result.PartyVInfo.Nonce = header.Get(
            CoseHeaderLabels.PartyVNonce);
        result.PartyVInfo.Other = HeaderBytes(
            header,
            CoseHeaderLabels.PartyVOther);
        return result;
    }

    internal static TokenResult AddSenderPublicKey(
        CoseHeader header,
        TokenKey? senderPublicKey)
    {
        if (senderPublicKey == null)
        {
            return ValidationSuccess();
        }

        TokenBytesResult encoded = CoseKey.Write(senderPublicKey);
        senderPublicKey.ReleaseOwnedMaterial();
        if (!encoded.Success)
        {
            return ValidationFailure(
                (encoded.Error ?? throw new InvalidOperationException("A required value is null.")).Code,
                encoded.Error.Message);
        }

        CborReader reader = CborReader.Create((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        CborReadResult decoded = reader.Read();
        if (!decoded.Success
            || reader.GetRemainingLength() != 0
            || (decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Map)
        {
            return ValidationFailure(
                "invalid_sender_key",
                "The generated ECDH sender key could not be encoded.");
        }

        if (header.Get(CoseHeaderLabels.EphemeralKey) != null)
        {
            return ValidationFailure(
                "duplicate_ephemeral_key",
                "The ECDH recipient already contains an ephemeral key.");
        }

        header.SetUnprotected(
            CoseHeaderLabels.EphemeralKey,
            decoded.Item);
        return ValidationSuccess();
    }

    internal static CoseKeyResult ReadSenderPublicKey(
        CoseHeader header,
        TokenPolicy policy)
    {
        CborItem? item = header.Get(CoseHeaderLabels.EphemeralKey);
        if (item == null || item.Kind != TokenCborValueKind.Map)
        {
            return CoseKeyFailure(
                "missing_ephemeral_key",
                "An ephemeral ECDH recipient requires an ephemeral public key.");
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(item);
        if (!written.Success)
        {
            return CoseKeyFailure(written.Error);
        }

        return CoseKey.Read(writer.ToArray());
    }

    internal static byte[]? HeaderBytes(CoseHeader header, int label)
    {
        CborItem? value = header.Get(label);
        if (value == null
            || value.Kind != TokenCborValueKind.ByteString)
        {
            return null;
        }

        return Copy(value.Bytes);
    }

    internal static byte[]? ReplaceContentKey(
        byte[] current,
        byte[]? replacement,
        bool replace)
    {
        if (!replace)
        {
            Clear(replacement);
            return current;
        }

        Clear(current);
        return replacement;
    }

    static bool IsContentAlgorithm(int algorithm)
    {
        return IsAesGcm(algorithm) || IsAesCcm(algorithm);
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

    static bool IsRecipientAlgorithm(int algorithm)
    {
        return IsDirectDistribution(algorithm)
            || IsAesKeyWrap(algorithm)
            || IsEcdhDirect(algorithm)
            || IsEcdhKeyWrap(algorithm);
    }

    internal static bool IsDirectRecipient(int algorithm)
    {
        return IsDirectDistribution(algorithm)
            || IsEcdhDirect(algorithm);
    }

    internal static bool IsDirectDistribution(int algorithm)
    {
        return algorithm == CoseAlgorithms.Direct
            || algorithm == CoseAlgorithms.DirectHkdfSha256
            || algorithm == CoseAlgorithms.DirectHkdfSha512
            || algorithm == CoseAlgorithms.DirectHkdfAes128
            || algorithm == CoseAlgorithms.DirectHkdfAes256;
    }

    internal static bool IsAesKeyWrap(int algorithm)
    {
        return algorithm == CoseAlgorithms.Aes128KeyWrap
            || algorithm == CoseAlgorithms.Aes192KeyWrap
            || algorithm == CoseAlgorithms.Aes256KeyWrap;
    }

    internal static bool IsEcdhDirect(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512;
    }

    internal static bool IsEcdhKeyWrap(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    internal static bool IsEcdhEphemeral(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap;
    }

    static int EcdhKeyWrapAlgorithm(int algorithm)
    {
        if (algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap)
        {
            return CoseAlgorithms.Aes128KeyWrap;
        }

        if (algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap)
        {
            return CoseAlgorithms.Aes192KeyWrap;
        }

        return CoseAlgorithms.Aes256KeyWrap;
    }

    static int GetContentKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.Aes128Gcm
            || algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_128)
        {
            return 16;
        }

        if (algorithm == CoseAlgorithms.Aes192Gcm)
        {
            return 24;
        }

        return 32;
    }

    static int GetKeyWrapSize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.Aes128KeyWrap)
        {
            return 16;
        }

        if (algorithm == CoseAlgorithms.Aes192KeyWrap)
        {
            return 24;
        }

        return 32;
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

    internal static bool IsAllowed(int algorithm, string[]? allowed)
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
                "The COSE_Encrypt cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] plaintext,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
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

        if (recipientHeaders == null)
        {
            throw new ArgumentNullException("recipientHeaders");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
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

    static void ValidateDetachedReferences(
        byte[]? detachedCiphertext,
        byte[] externalAuthenticatedData,
        bool detached)
    {
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedCiphertext == null)
        {
            throw new ArgumentNullException("detachedCiphertext");
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

    static TokenBytesResult BytesSuccess(byte[]? value)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = value;
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

    static CoseDetachedEncryptionResult DetachedFailure(
        string code,
        string message)
    {
        return DetachedFailure(new TokenError(code, message, -1));
    }

    static CoseDetachedEncryptionResult DetachedFailure(TokenError? error)
    {
        CoseDetachedEncryptionResult result =
            new CoseDetachedEncryptionResult();
        result.Error = error;
        return result;
    }

    static CoseContentEncryptionResult ContentSuccess(
        byte[] ciphertext,
        byte[]? initializationVector)
    {
        CoseContentEncryptionResult result =
            new CoseContentEncryptionResult();
        result.Success = true;
        result.Ciphertext = ciphertext;
        result.InitializationVector = initializationVector;
        return result;
    }

    static CoseContentEncryptionResult ContentFailure(TokenError? error)
    {
        CoseContentEncryptionResult result =
            new CoseContentEncryptionResult();
        result.Error = error;
        return result;
    }

    static CoseEncryptResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseEncryptResult Failure(TokenError? error)
    {
        CoseEncryptResult result = new CoseEncryptResult();
        result.Error = error;
        return result;
    }

    static CoseKeyResult CoseKeyFailure(string code, string message)
    {
        return CoseKeyFailure(new TokenError(code, message, -1));
    }

    static CoseKeyResult CoseKeyFailure(TokenError? error)
    {
        CoseKeyResult result = new CoseKeyResult();
        result.Error = error;
        return result;
    }
}
