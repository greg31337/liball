using System;
namespace token;

/// <summary>Authenticates COSE-protected CWTs and validates their registered claims.</summary>
internal static class CwtValidator
{
    const ulong CwtTag = 61;

    /// <summary>Validates a signed, MACed, encrypted, or nested CWT.</summary>
    /// <remarks>
    /// Key-resolution metadata is untrusted until the current COSE operation succeeds.
    /// External authenticated data is consumed in outermost-to-innermost order.
    /// Detached COSE content is not a self-contained CWT and is rejected.
    /// </remarks>
    /// <param name="token">The encoded CWT.</param>
    /// <param name="resolver">The key resolver used for every protection layer.</param>
    /// <param name="policy">The protection, nesting, and registered-claim policy.</param>
    /// <returns>The authenticated claims and protection metadata, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtValidationResult Validate(
        byte[] token,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (resolver == null)
        {
            throw new ArgumentNullException("resolver");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = policy.Validate();
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CwtEnvelopeResult envelope = ReadEnvelope(token, policy);
        if (!envelope.Success)
        {
            return Failure(envelope.Error);
        }

        CoseMessageKind[] kinds =
            new CoseMessageKind[policy.MaximumProtectionLayers];
        CoseHeader[] headers =
            new CoseHeader[policy.MaximumProtectionLayers];
        CoseHeader[][] layerHeaders =
            new CoseHeader[policy.MaximumProtectionLayers][];
        byte[]? message = envelope.Message;
        int layerIndex = 0;
        while (layerIndex < policy.MaximumProtectionLayers)
        {
            CoseMessageResult detected = CoseMessage.Read(
                (message ?? throw new InvalidOperationException("A required value is null.")),
                (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
            if (!detected.Success)
            {
                return Failure(detected.Error);
            }

            TokenResult allowed = ValidateKind(detected.Kind, policy);
            if (!allowed.Success)
            {
                return Failure(allowed.Error);
            }

            byte[] external = GetExternalData(policy, layerIndex);
            CwtLayerValidationResult layer = ValidateLayer(
                message,
                detected.Kind,
                external,
                resolver,
                policy);
            if (!layer.Success)
            {
                return Failure(layer.Error);
            }

            if (layer.Payload == null)
            {
                return Failure(
                    "detached_cwt_content_not_supported",
                    "A self-contained CWT cannot contain detached COSE content.");
            }

            kinds[layerIndex] = detected.Kind;
            headers[layerIndex] = (layer.Header ?? throw new InvalidOperationException("A required value is null."));
            layerHeaders[layerIndex] = layer.RelatedHeaders;
            layerIndex += 1;

            if (IsTaggedCoseMessage(layer.Payload))
            {
                if (!policy.AllowNestedTokens)
                {
                    return Failure(
                        "nested_cwt_not_allowed",
                        "Nested CWT validation is disabled by policy.");
                }

                if (layerIndex >= policy.MaximumProtectionLayers)
                {
                    return Failure(
                        "cwt_protection_layer_limit_exceeded",
                        "The CWT exceeds the configured protection-layer limit.");
                }

                message = layer.Payload;
                continue;
            }

            CwtClaimsResult claims = CwtClaims.Read(
                layer.Payload,
                policy.TokenPolicy);
            if (!claims.Success)
            {
                return Failure(claims.Error);
            }

            TokenResult confirmation = CwtConfirmation.Validate((claims.Claims ?? throw new InvalidOperationException("A required value is null.")));
            if (!confirmation.Success)
            {
                return Failure(confirmation.Error);
            }

            TokenResult confidentiality = ValidateConfirmationConfidentiality(
                claims.Claims,
                kinds,
                layerIndex);
            if (!confidentiality.Success)
            {
                return Failure(confidentiality.Error);
            }

            TokenResult headerClaims = ValidateHeaderClaims(
                layerHeaders,
                layerIndex,
                claims.Claims,
                policy.TokenPolicy);
            if (!headerClaims.Success)
            {
                return Failure(headerClaims.Error);
            }

            TokenResult registered = CwtRegisteredClaimValidator.Validate(
                claims.Claims,
                policy);
            if (!registered.Success)
            {
                return Failure(registered.Error);
            }

            CwtValidationResult result = new CwtValidationResult();
            result.Success = true;
            result.Claims = claims.Claims;
            result.ProtectionKinds = CopyKinds(kinds, layerIndex);
            result.Headers = CopyHeaders(headers, layerIndex);
            result.CwtTagged = envelope.Tagged;
            result.Nested = layerIndex > 1;
            return result;
        }

        return Failure(
            "cwt_protection_layer_limit_exceeded",
            "The CWT exceeds the configured protection-layer limit.");
    }

    static CwtEnvelopeResult ReadEnvelope(
        byte[] token,
        CwtValidationPolicy policy)
    {
        CwtEnvelopeResult result = new CwtEnvelopeResult();
        CborReader reader = CborReader.Create(
            token,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumTokenSizeInBytes,
            policy.TokenPolicy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            result.Error = decoded.Error;
            return result;
        }

        if (reader.GetRemainingLength() != 0)
        {
            result.Error = new TokenError(
                "trailing_cwt_data",
                "A CWT must contain exactly one CBOR item.",
                reader.GetPosition());
            return result;
        }

        CborItem? item = decoded.Item;
        if ((item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Tag && item.TagValue == CwtTag)
        {
            result.Tagged = true;
            item = item.TaggedValue;
            if (!IsCoseTag(item))
            {
                result.Error = new TokenError(
                    "invalid_cwt_tag_content",
                    "CBOR tag 61 must directly wrap a tagged COSE message.",
                    -1);
                return result;
            }
        }
        else if (policy.RequireCwtTag)
        {
            result.Error = new TokenError(
                "missing_cwt_tag",
                "The CWT policy requires CBOR tag 61.",
                -1);
            return result;
        }

        TokenBytesResult encoded = Encode(item, policy.TokenPolicy);
        if (!encoded.Success)
        {
            result.Error = encoded.Error;
            return result;
        }

        result.Success = true;
        result.Message = encoded.Bytes;
        return result;
    }

    static CwtLayerValidationResult ValidateLayer(
        byte[] message,
        CoseMessageKind kind,
        byte[] external,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        if (kind == CoseMessageKind.Sign1)
        {
            return ValidateSign1(message, external, resolver, policy);
        }

        if (kind == CoseMessageKind.Sign)
        {
            return ValidateSign(message, external, resolver, policy);
        }

        if (kind == CoseMessageKind.Mac0)
        {
            return ValidateMac0(
                message,
                external,
                resolver,
                policy.TokenPolicy);
        }

        if (kind == CoseMessageKind.Mac)
        {
            return ValidateMac(
                message,
                external,
                resolver,
                policy.TokenPolicy);
        }

        if (kind == CoseMessageKind.Encrypt0)
        {
            return ValidateEncrypt0(
                message,
                external,
                resolver,
                policy.TokenPolicy);
        }

        if (kind == CoseMessageKind.Encrypt)
        {
            return ValidateEncrypt(
                message,
                external,
                resolver,
                policy.TokenPolicy);
        }

        return LayerFailure(
            "invalid_cwt_protection_kind",
            "The CWT protection kind is not supported.");
    }

    static CwtLayerValidationResult ValidateSign1(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        CoseSign1Result parsed = CoseSign1.Read(
            message,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenKeyResult key = ResolveKey(
            resolver,
            (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header,
            parsed.Message.Header,
            TokenKeyOperations.Verify,
            false);
        if (!key.Success)
        {
            return LayerFailure(key.Error);
        }

        CoseSign1Result verified =
            CoseSign1Operations.VerifyWithExternalAuthenticatedData(
                message,
                external,
                (key.Key ?? throw new InvalidOperationException("A required value is null.")),
                policy.AllowCompatibilitySignatures,
                policy.TokenPolicy);
        if (!verified.Success)
        {
            return LayerFailure(verified.Error);
        }

        return LayerSuccess(
            (verified.Message ?? throw new InvalidOperationException("A required value is null.")).Payload,
            verified.Message.Header,
            Headers(verified.Message.Header));
    }

    static CwtLayerValidationResult ValidateSign(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        CoseSignResult parsed = CoseSign.Read(
            message,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenKey[] keys = new TokenKey[(parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Signatures.Length];
        int index = 0;
        while (index < keys.Length)
        {
            CoseHeader? signer = parsed.Message.Signatures[index].Header;
            TokenKeyResult key = ResolveKey(
                resolver,
                signer,
                parsed.Message.Header,
                TokenKeyOperations.Verify,
                false);
            if (!key.Success)
            {
                return LayerFailure(key.Error);
            }

            keys[index] = (key.Key ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CoseSignResult verified =
            CoseSignOperations.VerifyWithExternalAuthenticatedData(
                message,
                external,
                keys,
                policy.AllowCompatibilitySignatures,
                policy.TokenPolicy);
        if (!verified.Success)
        {
            return LayerFailure(verified.Error);
        }

        return LayerSuccess(
            (verified.Message ?? throw new InvalidOperationException("A required value is null.")).Payload,
            verified.Message.Header,
            Headers(
                verified.Message.Header,
                verified.Message.Signatures));
    }

    static CwtLayerValidationResult ValidateMac0(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        TokenPolicy? policy)
    {
        CoseMac0Result parsed = CoseMac0.Read(message, (policy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenKeyResult key = ResolveKey(
            resolver,
            (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header,
            parsed.Message.Header,
            TokenKeyOperations.MacVerify,
            false);
        if (!key.Success)
        {
            return LayerFailure(key.Error);
        }

        CoseMac0Result verified =
            CoseMac0Operations.VerifyWithExternalAuthenticatedData(
                message,
                external,
                (key.Key ?? throw new InvalidOperationException("A required value is null.")),
                policy);
        if (!verified.Success)
        {
            return LayerFailure(verified.Error);
        }

        return LayerSuccess(
            (verified.Message ?? throw new InvalidOperationException("A required value is null.")).Payload,
            verified.Message.Header,
            Headers(verified.Message.Header));
    }

    static CwtLayerValidationResult ValidateMac(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        TokenPolicy? policy)
    {
        CoseMacResult parsed = CoseMac.Read(message, (policy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenError? lastError = null;
        int index = 0;
        while (index < (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            CoseHeader? recipient = parsed.Message.Recipients[index].Header;
            TokenKeyResult key = ResolveKey(
                resolver,
                recipient,
                parsed.Message.Header,
                TokenKeyOperations.MacVerify,
                false);
            if (key.Success)
            {
                CoseMacResult verified =
                    CoseMacOperations.VerifyWithExternalAuthenticatedData(
                        message,
                        external,
                        index,
                        (key.Key ?? throw new InvalidOperationException("A required value is null.")),
                        null,
                        policy);
                if (verified.Success)
                {
                    return LayerSuccess(
                        (verified.Message ?? throw new InvalidOperationException("A required value is null.")).Payload,
                        verified.Message.Header,
                        Headers(
                            verified.Message.Header,
                            verified.Message.Recipients));
                }

                lastError = verified.Error;
            }
            else
            {
                lastError = key.Error;
            }

            index += 1;
        }

        return lastError == null
            ? LayerFailure(
                "cwt_key_resolution_failed",
                "No CWT MAC recipient key could be resolved.")
            : LayerFailure(lastError);
    }

    static CwtLayerValidationResult ValidateEncrypt0(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        TokenPolicy? policy)
    {
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(message, (policy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenKeyResult key = ResolveKey(
            resolver,
            (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header,
            parsed.Message.Header,
            TokenKeyOperations.Decrypt,
            true);
        if (!key.Success)
        {
            return LayerFailure(key.Error);
        }

        CoseEncrypt0Result decrypted =
            CoseEncrypt0Operations.DecryptWithExternalAuthenticatedData(
                message,
                external,
                (key.Key ?? throw new InvalidOperationException("A required value is null.")),
                policy);
        if (!decrypted.Success)
        {
            return LayerFailure(decrypted.Error);
        }

        return LayerSuccess(
            (decrypted.Message ?? throw new InvalidOperationException("A required value is null.")).Plaintext,
            decrypted.Message.Header,
            Headers(decrypted.Message.Header));
    }

    static CwtLayerValidationResult ValidateEncrypt(
        byte[] message,
        byte[] external,
        ITokenKeyResolver resolver,
        TokenPolicy? policy)
    {
        CoseEncryptResult parsed = CoseEncrypt.Read(message, (policy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return LayerFailure(parsed.Error);
        }

        TokenError? lastError = null;
        int index = 0;
        while (index < (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            CoseHeader? recipient = parsed.Message.Recipients[index].Header;
            TokenKeyResult key = ResolveKey(
                resolver,
                recipient,
                parsed.Message.Header,
                TokenKeyOperations.Decrypt,
                true);
            if (key.Success)
            {
                CoseEncryptResult decrypted =
                    CoseEncryptOperations.DecryptWithExternalAuthenticatedData(
                        message,
                        external,
                        index,
                        (key.Key ?? throw new InvalidOperationException("A required value is null.")),
                        null,
                        policy);
                if (decrypted.Success)
                {
                    return LayerSuccess(
                        (decrypted.Message ?? throw new InvalidOperationException("A required value is null.")).Plaintext,
                        decrypted.Message.Header,
                        Headers(
                            decrypted.Message.Header,
                            decrypted.Message.Recipients));
                }

                lastError = decrypted.Error;
            }
            else
            {
                lastError = key.Error;
            }

            index += 1;
        }

        return lastError == null
            ? LayerFailure(
                "cwt_key_resolution_failed",
                "No CWT encryption recipient key could be resolved.")
            : LayerFailure(lastError);
    }

    static TokenKeyResult ResolveKey(
        ITokenKeyResolver resolver,
        CoseHeader? selectionHeader,
        CoseHeader? bodyHeader,
        string operation,
        bool encrypted)
    {
        TokenKeyResolutionContext context =
            new TokenKeyResolutionContext();
        context.Kind = TokenKind.Cwt;
        context.Operation = operation;
        context.CoseHeader = selectionHeader;
        CoseHeader? algorithmHeader = (selectionHeader ?? throw new InvalidOperationException("A required value is null.")).HasAlgorithm
            ? selectionHeader
            : bodyHeader;
        context.CoseAlgorithm = (algorithmHeader ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        context.HasCoseAlgorithm = algorithmHeader.HasAlgorithm;
        if (algorithmHeader.HasAlgorithm)
        {
            context.Algorithm = CoseAlgorithmsOperations.GetName(
                algorithmHeader.Algorithm);
        }

        if (encrypted && (bodyHeader ?? throw new InvalidOperationException("A required value is null.")).HasAlgorithm)
        {
            context.EncryptionAlgorithm = CoseAlgorithmsOperations.GetName(
                bodyHeader.Algorithm);
        }

        byte[]? keyId = selectionHeader.KeyId == null
            ? (bodyHeader ?? throw new InvalidOperationException("A required value is null.")).KeyId
            : selectionHeader.KeyId;
        context.KeyIdBytes = keyId == null
            ? null
            : keyId.AsSpan().ToArray();
        CoseCwtClaimsResult headerClaims = CoseCwtClaims.Read(
            selectionHeader);
        if (!headerClaims.Present && selectionHeader != bodyHeader)
        {
            headerClaims = CoseCwtClaims.Read((bodyHeader ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (headerClaims.Success && headerClaims.Present)
        {
            context.UnverifiedCwtClaims = headerClaims.Claims;
        }

        return TokenKeyResolution.Resolve(resolver, context);
    }

    static TokenResult ValidateKind(
        CoseMessageKind kind,
        CwtValidationPolicy policy)
    {
        if ((kind == CoseMessageKind.Sign1
                || kind == CoseMessageKind.Sign)
            && !policy.AllowSignedTokens)
        {
            return ResultFailure(
                "signed_cwt_not_allowed",
                "Signed CWT validation is disabled by policy.");
        }

        if ((kind == CoseMessageKind.Mac0
                || kind == CoseMessageKind.Mac)
            && !policy.AllowMacTokens)
        {
            return ResultFailure(
                "maced_cwt_not_allowed",
                "MACed CWT validation is disabled by policy.");
        }

        if ((kind == CoseMessageKind.Encrypt0
                || kind == CoseMessageKind.Encrypt)
            && !policy.AllowEncryptedTokens)
        {
            return ResultFailure(
                "encrypted_cwt_not_allowed",
                "Encrypted CWT validation is disabled by policy.");
        }

        return ResultSuccess();
    }

    static TokenResult ValidateConfirmationConfidentiality(
        CwtClaims claims,
        CoseMessageKind[] kinds,
        int length)
    {
        CborItem? confirmation = claims.Get(CwtClaimLabels.Confirmation);
        if (confirmation == null
            || confirmation.Kind != TokenCborValueKind.Map
            || confirmation.MapValue == null)
        {
            return ResultSuccess();
        }

        CborItem? directKey = null;
        int entryIndex = 0;
        while (entryIndex < confirmation.MapValue.Length)
        {
            CborMapEntry entry = confirmation.MapValue[entryIndex];
            if ((entry.Key ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.UnsignedInteger
                && entry.Key.IntegerArgument
                    == (ulong)CwtConfirmationMethodLabels.CoseKey)
            {
                directKey = entry.Value;
                break;
            }

            entryIndex += 1;
        }

        if (directKey == null)
        {
            return ResultSuccess();
        }

        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteDeterministicItem(directKey);
        if (!written.Success)
        {
            return written;
        }

        CoseKeyResult parsed = CoseKey.Read(writer.ToArray());
        if (!parsed.Success)
        {
            return ResultFailure(
                "invalid_confirmation_key",
                "The CWT confirmation COSE_Key is invalid.");
        }

        bool symmetric = (parsed.Key ?? throw new InvalidOperationException("A required value is null.")).SymmetricKey != null
            || parsed.Key.CoseKeyType == CoseKeyLabels.KeyTypeSymmetric
            || string.Equals(
                parsed.Key.KeyType,
                TokenKeyTypes.Symmetric,
                StringComparison.Ordinal);
        parsed.Key.ReleaseOwnedMaterial();
        if (!symmetric)
        {
            return ResultSuccess();
        }

        int layerIndex = 0;
        while (layerIndex < length)
        {
            if (kinds[layerIndex] == CoseMessageKind.Encrypt0
                || kinds[layerIndex] == CoseMessageKind.Encrypt)
            {
                return ResultSuccess();
            }

            layerIndex += 1;
        }

        return ResultFailure(
            "symmetric_confirmation_key_not_encrypted",
            "A directly embedded symmetric confirmation key requires an encrypted CWT layer.");
    }

    static TokenResult ValidateHeaderClaims(
        CoseHeader[][] layerHeaders,
        int length,
        CwtClaims payloadClaims,
        TokenPolicy policy)
    {
        int index = 0;
        while (index < length)
        {
            CoseHeader[] headers = layerHeaders[index];
            int headerIndex = 0;
            while (headerIndex < headers.Length)
            {
                TokenResult result = CoseCwtClaims.ValidateAgainstPayload(
                    headers[headerIndex],
                    payloadClaims,
                    policy);
                if (!result.Success)
                {
                    return result;
                }

                headerIndex += 1;
            }

            index += 1;
        }

        return ResultSuccess();
    }

    static CoseHeader[] Headers(CoseHeader? header)
    {
        return new CoseHeader[] { (header ?? throw new InvalidOperationException("A required value is null.")) };
    }

    static CoseHeader[] Headers(
        CoseHeader? header,
        CoseSignature[] signatures)
    {
        CoseHeader[] result = new CoseHeader[signatures.Length + 1];
        result[0] = (header ?? throw new InvalidOperationException("A required value is null."));
        int index = 0;
        while (index < signatures.Length)
        {
            result[index + 1] = (signatures[index].Header ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return result;
    }

    static CoseHeader[] Headers(
        CoseHeader? header,
        CoseRecipient[] recipients)
    {
        CoseHeader[] result = new CoseHeader[recipients.Length + 1];
        result[0] = (header ?? throw new InvalidOperationException("A required value is null."));
        int index = 0;
        while (index < recipients.Length)
        {
            result[index + 1] = (recipients[index].Header ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return result;
    }

    static byte[] GetExternalData(
        CwtValidationPolicy policy,
        int layerIndex)
    {
        if (layerIndex >= (policy.LayerExternalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            return Array.Empty<byte>();
        }

        return policy.LayerExternalAuthenticatedData[layerIndex]
            ?? throw new InvalidOperationException("A required value is null.");
    }

    static bool IsTaggedCoseMessage(byte[] payload)
    {
        CborReader reader = CborReader.Create(payload);
        CborReadResult decoded = reader.Read();
        return decoded.Success
            && reader.GetRemainingLength() == 0
            && IsCoseTag(decoded.Item);
    }

    static bool IsCoseTag(CborItem? item)
    {
        if (item == null || item.Kind != TokenCborValueKind.Tag)
        {
            return false;
        }

        return item.TagValue == (ulong)CoseMessageKind.Sign1
            || item.TagValue == (ulong)CoseMessageKind.Sign
            || item.TagValue == (ulong)CoseMessageKind.Mac0
            || item.TagValue == (ulong)CoseMessageKind.Mac
            || item.TagValue == (ulong)CoseMessageKind.Encrypt0
            || item.TagValue == (ulong)CoseMessageKind.Encrypt;
    }

    static TokenBytesResult Encode(CborItem? item, TokenPolicy policy)
    {
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem((item ?? throw new InvalidOperationException("A required value is null.")));
        if (!written.Success)
        {
            TokenBytesResult failure = new TokenBytesResult();
            failure.Error = written.Error;
            return failure;
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static CoseMessageKind[] CopyKinds(
        CoseMessageKind[] values,
        int length)
    {
        CoseMessageKind[] result = new CoseMessageKind[length];
        values.AsSpan(0, length).CopyTo(result);
        return result;
    }

    static CoseHeader[] CopyHeaders(CoseHeader[] values, int length)
    {
        CoseHeader[] result = new CoseHeader[length];
        values.AsSpan(0, length).CopyTo(result);
        return result;
    }

    static CwtLayerValidationResult LayerSuccess(
        byte[]? payload,
        CoseHeader? header,
        CoseHeader[] relatedHeaders)
    {
        CwtLayerValidationResult result =
            new CwtLayerValidationResult();
        result.Success = true;
        result.Payload = payload;
        result.Header = header;
        result.RelatedHeaders = relatedHeaders;
        return result;
    }

    static CwtLayerValidationResult LayerFailure(
        string code,
        string message)
    {
        return LayerFailure(new TokenError(code, message, -1));
    }

    static CwtLayerValidationResult LayerFailure(TokenError? error)
    {
        CwtLayerValidationResult result =
            new CwtLayerValidationResult();
        result.Error = error;
        return result;
    }

    static TokenResult ResultSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ResultFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static CwtValidationResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CwtValidationResult Failure(TokenError? error)
    {
        CwtValidationResult result = new CwtValidationResult();
        result.Error = error;
        return result;
    }
}
