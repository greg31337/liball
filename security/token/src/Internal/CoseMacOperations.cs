using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Creates and verifies RFC 9052 COSE_Mac messages.</summary>
internal static class CoseMacOperations
{
    /// <summary>Creates a tagged MAC message for one or more recipients.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The MAC headers, including a protected HMAC algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient keys in recipient order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The tagged MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Create(
            payload,
            header,
            recipientHeaders,
            keys,
            null,
            true,
            policy);
    }

    /// <summary>Creates a MAC message with explicit registered-tag control.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The MAC headers, including a protected HMAC algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient keys in recipient order.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 97.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        bool tagged,
        TokenPolicy policy)
    {
        return Create(
            payload,
            header,
            recipientHeaders,
            keys,
            null,
            tagged,
            policy);
    }

    /// <summary>Creates a MAC message with optional static ECDH sender keys.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The MAC headers, including a protected HMAC algorithm.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient public or symmetric keys in recipient order.</param>
    /// <param name="senderKeys">Static sender private keys in recipient order, or <c>null</c> for ephemeral ECDH.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 97.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            false,
            policy);
    }

    /// <summary>Creates an attached COSE_Mac message with external authenticated data.</summary>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            false,
            policy);
    }

    /// <summary>Creates a COSE_Mac message whose payload is transported separately.</summary>
    internal static TokenBytesResult CreateDetached(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            true,
            policy);
    }

    static TokenBytesResult CreateCore(
        byte[] payload,
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
            payload,
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
            return BytesFailure(policyResult.Error);
        }

        if (recipientHeaders.Length == 0
            || recipientHeaders.Length != keys.Length
            || senderKeys != null
                && senderKeys.Length != keys.Length)
        {
            return BytesFailure(
                "recipient_count_mismatch",
                "Recipient headers and keys must have the same nonzero length.");
        }

        TokenResult macHeader = ValidateMacHeader(header, policy);
        if (!macHeader.Success)
        {
            return BytesFailure(macHeader.Error);
        }

        TokenBytesResult bodyProtected = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!bodyProtected.Success)
        {
            return bodyProtected;
        }

        int macAlgorithm = header.Algorithm;
        int keySize = GetMacKeySize(macAlgorithm);
        byte[]? macKey = null;
        CoseRecipient[] recipients =
            new CoseRecipient[recipientHeaders.Length];
        int index = 0;
        while (index < recipientHeaders.Length)
        {
            if (recipientHeaders[index] == null)
            {
                Clear(macKey);
                return BytesFailure(
                    "invalid_cose_recipient_header",
                    "A COSE recipient header is missing.");
            }

            if (keys[index] == null)
            {
                Clear(macKey);
                return BytesFailure(
                    "invalid_key",
                    "A COSE recipient key is missing.");
            }

            TokenResult recipientHeader =
                CoseEncryptOperations.ValidateRecipientHeader(
                    recipientHeaders[index],
                    policy);
            if (!recipientHeader.Success)
            {
                Clear(macKey);
                return BytesFailure(recipientHeader.Error);
            }

            int recipientAlgorithm = recipientHeaders[index].Algorithm;
            if (CoseEncryptOperations.IsDirectRecipient(
                    recipientAlgorithm)
                && recipientHeaders.Length != 1)
            {
                Clear(macKey);
                return BytesFailure(
                    "direct_recipient_must_be_only_recipient",
                    "Direct key distribution requires exactly one recipient.");
            }

            TokenBytesResult recipientProtected =
                CoseSign.GetProtectedBytes(
                    recipientHeaders[index],
                    policy);
            if (!recipientProtected.Success)
            {
                Clear(macKey);
                return recipientProtected;
            }

            CoseKdfContextParameters context =
                CoseEncryptOperations.Context(
                    recipientHeaders[index],
                    recipientProtected.Bytes,
                    recipientAlgorithm,
                    macAlgorithm,
                    keySize);
            byte[]? salt = CoseEncryptOperations.HeaderBytes(
                recipientHeaders[index],
                CoseHeaderLabels.Salt);
            CoseRecipient recipient = new CoseRecipient();
            recipient.Header = recipientHeaders[index];
            TokenError? managedError = null;
            if (CoseEncryptOperations.IsDirectDistribution(
                recipientAlgorithm))
            {
                CoseDirectKeyDistributionResult managed =
                    CoseDirectKeyDistributionOperations.Encrypt(
                        keys[index],
                        recipientAlgorithm,
                        salt,
                        context);
                if (managed.Success)
                {
                    macKey = managed.ContentKey;
                    recipient.EncryptedKey = managed.EncryptedKey;
                }
                else
                {
                    managedError = managed.Error;
                }
            }
            else
            {
                if (macKey == null)
                {
                    macKey = RandomBytes.Generate(keySize);
                }

                if (CoseEncryptOperations.IsAesKeyWrap(
                    recipientAlgorithm))
                {
                    TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
                        macKey,
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
                    CoseEcdhResult ecdh =
                        CoseEncryptOperations.IsEcdhKeyWrap(
                            recipientAlgorithm)
                        ? CoseEcdhAlgorithms.Wrap(
                            macKey,
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
                        macKey = CoseEncryptOperations.ReplaceContentKey(
                            macKey,
                            ecdh.ContentKey,
                            CoseEncryptOperations.IsEcdhDirect(
                                recipientAlgorithm));
                        recipient.EncryptedKey = ecdh.EncryptedKey;
                        TokenResult sender =
                            CoseEncryptOperations.AddSenderPublicKey(
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
                Clear(macKey);
                return BytesFailure(managedError);
            }

            recipients[index] = recipient;
            index += 1;
        }

        TokenBytesResult authenticated = CoseMacStructure.CreateMac(
            (bodyProtected.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            payload,
            policy);
        if (!authenticated.Success)
        {
            Clear(macKey);
            return authenticated;
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.ImportCose(
            (macKey ?? throw new InvalidOperationException("A required value is null.")),
            macAlgorithm);
        Clear(macKey);
        if (!imported.Success)
        {
            return BytesFailure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Use = TokenKeyUses.Signature;
        TokenBytesResult tag = CoseHmacAlgorithms.Create(
            (authenticated.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            imported.Key,
            macAlgorithm);
        imported.Key.ReleaseOwnedMaterial();
        if (!tag.Success)
        {
            return tag;
        }

        CoseMac message = new CoseMac();
        message.Header = header;
        message.Header.ProtectedBytes = Copy(bodyProtected.Bytes);
        message.Payload = detached ? null : payload;
        message.DetachedPayload = detached;
        message.Tag = tag.Bytes;
        message.Recipients = recipients;
        message.Tagged = tagged;
        return message.WriteWithTagAndPolicy(tagged, policy);
    }

    /// <summary>Verifies one selected recipient and the message authentication code.</summary>
    /// <param name="cbor">The encoded COSE_Mac message.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static CoseMacResult Verify(
        byte[] cbor,
        int recipientIndex,
        TokenKey key,
        TokenPolicy policy)
    {
        return Verify(cbor, recipientIndex, key, null, policy);
    }

    /// <summary>Verifies one selected recipient with an optional static ECDH sender public key.</summary>
    /// <param name="cbor">The encoded COSE_Mac message.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="senderKey">The static ECDH sender public key, or <c>null</c>.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference argument is null.</exception>
    internal static CoseMacResult Verify(
        byte[] cbor,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return VerifySelectedCore(
            cbor,
            null,
            Array.Empty<byte>(),
            recipientIndex,
            key,
            senderKey,
            false,
            policy);
    }

    /// <summary>Verifies one recipient of an attached COSE_Mac message with external authenticated data.</summary>
    internal static CoseMacResult VerifyWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return VerifySelectedCore(
            cbor,
            null,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            false,
            policy);
    }

    /// <summary>Verifies one recipient using a separately transported payload.</summary>
    internal static CoseMacResult VerifyDetached(
        byte[] cbor,
        byte[] payload,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        return VerifySelectedCore(
            cbor,
            payload,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            true,
            policy);
    }

    static CoseMacResult VerifySelectedCore(
        byte[] cbor,
        byte[]? detachedPayload,
        byte[] externalAuthenticatedData,
        int recipientIndex,
        TokenKey key,
        TokenKey? senderKey,
        bool detached,
        TokenPolicy policy)
    {
        ValidateVerifyReferences(cbor, key, policy);
        ValidateDetachedReferences(
            detachedPayload,
            externalAuthenticatedData,
            detached);
        CoseMacResult parsed = ParseForVerification(
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

        TokenBytesResult macKey = RecoverMacKey(
            parsed.Message,
            parsed.Message.Recipients[recipientIndex],
            key,
            senderKey,
            policy);
        if (!macKey.Success)
        {
            return Failure(macKey.Error);
        }

        TokenResult verified = VerifyTag(
            parsed.Message,
            macKey.Bytes,
            detached ? detachedPayload : parsed.Message.Payload,
            externalAuthenticatedData,
            policy);
        Clear(macKey.Bytes);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        if (detached)
        {
            parsed.Message.Payload = Copy(detachedPayload);
        }

        parsed.Message.TagVerified = true;
        parsed.Message.Recipients[recipientIndex].Decrypted = true;
        return parsed;
    }

    /// <summary>Verifies every top-level recipient with matching keys.</summary>
    /// <param name="cbor">The encoded COSE_Mac message.</param>
    /// <param name="keys">Recipient keys corresponding to serialization order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message only when every recipient and the MAC verify.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMacResult Verify(
        byte[] cbor,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Verify(cbor, keys, null, policy);
    }

    /// <summary>Verifies every recipient with optional static ECDH sender public keys.</summary>
    /// <param name="cbor">The encoded COSE_Mac message.</param>
    /// <param name="keys">Recipient keys corresponding to serialization order.</param>
    /// <param name="senderKeys">Static sender public keys in recipient order, or <c>null</c>.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed message only when every recipient and the MAC verify.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static CoseMacResult Verify(
        byte[] cbor,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return VerifyAllCore(
            cbor,
            null,
            Array.Empty<byte>(),
            keys,
            senderKeys,
            false,
            policy);
    }

    /// <summary>Verifies every recipient of an attached message with external authenticated data.</summary>
    internal static CoseMacResult VerifyWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return VerifyAllCore(
            cbor,
            null,
            externalAuthenticatedData,
            keys,
            senderKeys,
            false,
            policy);
    }

    /// <summary>Verifies every recipient using a separately transported payload.</summary>
    internal static CoseMacResult VerifyDetached(
        byte[] cbor,
        byte[] payload,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        TokenKey[]? senderKeys,
        TokenPolicy policy)
    {
        return VerifyAllCore(
            cbor,
            payload,
            externalAuthenticatedData,
            keys,
            senderKeys,
            true,
            policy);
    }

    static CoseMacResult VerifyAllCore(
        byte[] cbor,
        byte[]? detachedPayload,
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
            detachedPayload,
            externalAuthenticatedData,
            detached);
        CoseMacResult parsed = ParseForVerification(
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

        byte[]? macKey = null;
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                Clear(macKey);
                return Failure(
                    "invalid_key",
                    "A COSE recipient key is missing.");
            }

            TokenBytesResult recovered = RecoverMacKey(
                parsed.Message,
                parsed.Message.Recipients[index],
                keys[index],
                senderKeys == null ? null : senderKeys[index],
                policy);
            if (!recovered.Success)
            {
                Clear(macKey);
                return Failure(recovered.Error);
            }

            if (macKey == null)
            {
                macKey = recovered.Bytes;
            }
            else
            {
                bool same = CryptographicOperations.FixedTimeEquals(
                    macKey,
                    recovered.Bytes);
                Clear(recovered.Bytes);
                if (!same)
                {
                    Clear(macKey);
                    return Failure(
                        "recipient_content_key_mismatch",
                        "COSE recipients did not recover the same MAC key.");
                }
            }

            index += 1;
        }

        TokenResult verified = VerifyTag(
            parsed.Message,
            macKey,
            detached ? detachedPayload : parsed.Message.Payload,
            externalAuthenticatedData,
            policy);
        Clear(macKey);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        if (detached)
        {
            parsed.Message.Payload = Copy(detachedPayload);
        }

        parsed.Message.TagVerified = true;
        index = 0;
        while (index < parsed.Message.Recipients.Length)
        {
            parsed.Message.Recipients[index].Decrypted = true;
            index += 1;
        }

        return parsed;
    }

    static CoseMacResult ParseForVerification(
        byte[] cbor,
        bool detached,
        TokenPolicy policy)
    {
        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseMacResult parsed = CoseMac.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload && !detached)
        {
            return Failure(
                "missing_payload",
                "Attached COSE_Mac verification requires payload bytes.");
        }

        if (!parsed.Message.DetachedPayload && detached)
        {
            return Failure(
                "unexpected_attached_payload",
                "Detached COSE_Mac verification requires a null payload field.");
        }

        TokenResult macHeader = ValidateMacHeader(
            parsed.Message.Header,
            policy);
        if (!macHeader.Success)
        {
            return Failure(macHeader.Error);
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

            TokenResult recipientHeader =
                CoseEncryptOperations.ValidateRecipientHeader(
                    recipient.Header,
                    policy);
            if (!recipientHeader.Success)
            {
                return Failure(recipientHeader.Error);
            }

            if (CoseEncryptOperations.IsDirectRecipient(
                    (recipient.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm)
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

    static TokenBytesResult RecoverMacKey(
        CoseMac message,
        CoseRecipient recipient,
        TokenKey key,
        TokenKey? senderKey,
        TokenPolicy policy)
    {
        int recipientAlgorithm = (recipient.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        int macAlgorithm = (message.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        int keySize = GetMacKeySize(macAlgorithm);
        CoseKdfContextParameters context =
            CoseEncryptOperations.Context(
                recipient.Header,
                recipient.Header.ProtectedBytes,
                recipientAlgorithm,
                macAlgorithm,
                keySize);
        byte[]? salt = CoseEncryptOperations.HeaderBytes(
            recipient.Header,
            CoseHeaderLabels.Salt);
        if (CoseEncryptOperations.IsDirectDistribution(
            recipientAlgorithm))
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

        if (CoseEncryptOperations.IsAesKeyWrap(recipientAlgorithm))
        {
            return CoseAesKeyWrap.Unwrap(
                (recipient.EncryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                recipientAlgorithm);
        }

        TokenKey? actualSender = senderKey;
        bool releaseSender = false;
        if (CoseEncryptOperations.IsEcdhEphemeral(
            recipientAlgorithm))
        {
            CoseKeyResult parsedSender =
                CoseEncryptOperations.ReadSenderPublicKey(
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
                "Static-static ECDH verification requires the sender public key.");
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

    static TokenResult VerifyTag(
        CoseMac message,
        byte[]? macKey,
        byte[]? payload,
        byte[] externalAuthenticatedData,
        TokenPolicy policy)
    {
        TokenBytesResult authenticated = CoseMacStructure.CreateMac(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            externalAuthenticatedData,
            (payload ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!authenticated.Success)
        {
            return ValidationFailure(authenticated.Error);
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.ImportCose(
            (macKey ?? throw new InvalidOperationException("A required value is null.")),
            message.Header.Algorithm);
        if (!imported.Success)
        {
            return ValidationFailure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Use = TokenKeyUses.Signature;
        TokenResult result = CoseHmacAlgorithms.Verify(
            (authenticated.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            (message.Tag ?? throw new InvalidOperationException("A required value is null.")),
            imported.Key,
            message.Header.Algorithm);
        imported.Key.ReleaseOwnedMaterial();
        return result;
    }

    static TokenResult ValidateMacHeader(
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
                "COSE_Mac requires a message authentication algorithm.");
        }

        if (header.GetProtected(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE_Mac algorithm must be protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE_Mac algorithm must use an integer label.");
        }

        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(
                header.Algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsHmac(header.Algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented MAC algorithm.");
        }

        if (!CoseEncryptOperations.IsAllowed(
            header.Algorithm,
            policy.AllowedAlgorithms))
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The COSE MAC algorithm is not allowed by policy.");
        }

        return ValidationSuccess();
    }

    static bool IsHmac(int algorithm)
    {
        return algorithm == CoseAlgorithms.HmacSha256Truncated64
            || algorithm == CoseAlgorithms.HmacSha256
            || algorithm == CoseAlgorithms.HmacSha384
            || algorithm == CoseAlgorithms.HmacSha512;
    }

    static int GetMacKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.HmacSha384)
        {
            return 48;
        }

        if (algorithm == CoseAlgorithms.HmacSha512)
        {
            return 64;
        }

        return 32;
    }

    static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Mac cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] payload,
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
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

    static void ValidateVerifyReferences(
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
        byte[]? detachedPayload,
        byte[] externalAuthenticatedData,
        bool detached)
    {
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedPayload == null)
        {
            throw new ArgumentNullException("detachedPayload");
        }
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

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }

    static TokenResult ValidationFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
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

    static CoseMacResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseMacResult Failure(TokenError? error)
    {
        CoseMacResult result = new CoseMacResult();
        result.Error = error;
        return result;
    }
}
