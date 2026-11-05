using System;
namespace token;

/// <summary>Creates signed, MACed, encrypted, and nested CBOR Web Tokens.</summary>
internal static class CborWebToken
{
    const ulong CwtTag = 61;
    const string CwtMediaType = "application/cwt";

    /// <summary>Validates a signed, MACed, encrypted, or nested CWT.</summary>
    /// <param name="token">The encoded CWT.</param>
    /// <param name="resolver">The key resolver used for every COSE layer.</param>
    /// <param name="policy">The protection, nesting, and registered-claim policy.</param>
    /// <returns>The authenticated claims and protection metadata, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtValidationResult Validate(
        byte[] token,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        return CwtValidator.Validate(token, resolver, policy);
    }

    /// <summary>Creates a tagged single-party CWT selected from the key's COSE algorithm.</summary>
    /// <remarks>
    /// Implemented signature algorithms select COSE_Sign1, HMAC algorithms select COSE_Mac0,
    /// and implemented content-encryption algorithms select COSE_Encrypt0.
    /// </remarks>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="key">A key with a COSE algorithm assignment.</param>
    /// <param name="policy">The size, algorithm, and CBOR safety policy.</param>
    /// <returns>The tagged COSE message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        CwtClaims claims,
        TokenKey key,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (!key.HasCoseAlgorithm)
        {
            return Failure(
                "missing_cwt_algorithm",
                "Automatic CWT creation requires a COSE algorithm on the key.");
        }

        CoseHeader header = AlgorithmHeader(key.CoseAlgorithm);
        CwtProtectionLayer layer;
        if (IsSignatureAlgorithm(key.CoseAlgorithm))
        {
            layer = CwtProtectionLayer.CreateSign1(header, key);
        }
        else if (IsMacAlgorithm(key.CoseAlgorithm))
        {
            layer = CwtProtectionLayer.CreateMac0(header, key);
        }
        else if (IsContentEncryptionAlgorithm(key.CoseAlgorithm))
        {
            layer = CwtProtectionLayer.CreateEncrypt0(header, key);
        }
        else
        {
            return Failure(
                "invalid_cwt_algorithm",
                "The key algorithm cannot create a single-party CWT.");
        }

        return Create(claims, layer, false, policy);
    }

    /// <summary>Creates a CWT with one COSE protection layer.</summary>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="layer">The COSE protection description.</param>
    /// <param name="cwtTagged">Whether to prepend CBOR tag 61.</param>
    /// <param name="policy">The size, algorithm, and CBOR safety policy.</param>
    /// <returns>The protected CWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        CwtClaims claims,
        CwtProtectionLayer layer,
        bool cwtTagged,
        TokenPolicy policy)
    {
        if (layer == null)
        {
            throw new ArgumentNullException("layer");
        }

        return CreateNested(
            claims,
            new CwtProtectionLayer[] { layer },
            cwtTagged,
            policy);
    }

    /// <summary>Creates a nested CWT by applying protection layers from innermost to outermost.</summary>
    /// <remarks>
    /// Every COSE layer is tagged so a nested consumer can identify the next message kind.
    /// For outer layers, a missing content type is added as the protected
    /// <c>application/cwt</c> value. Existing content types are preserved.
    /// </remarks>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="layers">One or more layers in innermost-to-outermost order.</param>
    /// <param name="cwtTagged">Whether to prepend CBOR tag 61 to the complete CWT.</param>
    /// <param name="policy">The size, algorithm, and CBOR safety policy.</param>
    /// <returns>The protected CWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateNested(
        CwtClaims claims,
        CwtProtectionLayer[] layers,
        bool cwtTagged,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (layers == null)
        {
            throw new ArgumentNullException("layers");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (layers.Length == 0)
        {
            return Failure(
                "missing_cwt_protection",
                "A CWT requires at least one COSE protection layer.");
        }

        TokenResult confirmation = CwtConfirmation.Validate(claims);
        if (!confirmation.Success)
        {
            return Failure(confirmation.Error);
        }

        TokenResult confidentiality = ValidateConfirmationConfidentiality(
            claims,
            layers);
        if (!confidentiality.Success)
        {
            return Failure(confidentiality.Error);
        }

        TokenBytesResult claimsBytes = claims.WriteWithPolicy(policy);
        if (!claimsBytes.Success)
        {
            return claimsBytes;
        }

        byte[]? message = claimsBytes.Bytes;
        int index = 0;
        while (index < layers.Length)
        {
            CwtProtectionLayer layer = layers[index];
            if (layer == null)
            {
                return Failure(
                    "invalid_cwt_protection_layer",
                    "A CWT protection layer is missing.");
            }

            if (index > 0)
            {
                TokenResult contentType = EnsureNestedContentType(layer);
                if (!contentType.Success)
                {
                    return Failure(contentType.Error);
                }
            }

            TokenBytesResult protectedMessage = ApplyLayer(
                message,
                layer,
                policy);
            if (!protectedMessage.Success)
            {
                return protectedMessage;
            }

            message = protectedMessage.Bytes;
            index += 1;
        }

        if (!cwtTagged)
        {
            return BytesSuccess(message);
        }

        return AddCwtTag(message, policy);
    }

    static TokenBytesResult ApplyLayer(
        byte[]? payload,
        CwtProtectionLayer layer,
        TokenPolicy policy)
    {
        TokenResult model = ValidateLayer(layer);
        if (!model.Success)
        {
            return Failure(model.Error);
        }

        byte[] requiredPayload = payload
            ?? throw new InvalidOperationException("A required value is null.");
        byte[] external = layer.ExternalAuthenticatedData;
        if (layer.Kind == CwtProtectionKind.Sign1)
        {
            return CoseSign1Operations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.Keys[0],
                true,
                layer.AllowCompatibility,
                policy);
        }

        if (layer.Kind == CwtProtectionKind.Sign)
        {
            return CoseSignOperations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.ParticipantHeaders,
                layer.Keys,
                true,
                layer.AllowCompatibility,
                policy);
        }

        if (layer.Kind == CwtProtectionKind.Mac0)
        {
            return CoseMac0Operations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.Keys[0],
                true,
                policy);
        }

        if (layer.Kind == CwtProtectionKind.Mac)
        {
            return CoseMacOperations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.ParticipantHeaders,
                layer.Keys,
                layer.SenderKeys,
                true,
                policy);
        }

        if (layer.Kind == CwtProtectionKind.Encrypt0)
        {
            return CoseEncrypt0Operations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.Keys[0],
                true,
                policy);
        }

        if (layer.Kind == CwtProtectionKind.Encrypt)
        {
            return CoseEncryptOperations.CreateWithExternalAuthenticatedData(
                requiredPayload,
                external,
                layer.Header,
                layer.ParticipantHeaders,
                layer.Keys,
                layer.SenderKeys,
                true,
                policy);
        }

        return Failure(
            "invalid_cwt_protection_kind",
            "The CWT protection kind is not supported.");
    }

    static TokenResult ValidateLayer(CwtProtectionLayer layer)
    {
        if (layer.Header == null
            || layer.ParticipantHeaders == null
            || layer.Keys == null
            || layer.ExternalAuthenticatedData == null)
        {
            return ResultFailure(
                "invalid_cwt_protection_layer",
                "A CWT protection layer contains a missing required value.");
        }

        if (layer.Kind == CwtProtectionKind.Sign1
            || layer.Kind == CwtProtectionKind.Mac0
            || layer.Kind == CwtProtectionKind.Encrypt0)
        {
            if (layer.Keys.Length != 1 || layer.Keys[0] == null)
            {
                return ResultFailure(
                    "invalid_cwt_protection_keys",
                    "A single-party CWT protection layer requires exactly one key.");
            }

            return ResultSuccess();
        }

        if (layer.Kind == CwtProtectionKind.Sign
            || layer.Kind == CwtProtectionKind.Mac
            || layer.Kind == CwtProtectionKind.Encrypt)
        {
            if (layer.Keys.Length == 0
                || layer.ParticipantHeaders.Length != layer.Keys.Length)
            {
                return ResultFailure(
                    "invalid_cwt_protection_participants",
                    "A multi-party CWT layer requires corresponding nonempty headers and keys.");
            }

            int index = 0;
            while (index < layer.Keys.Length)
            {
                if (layer.Keys[index] == null
                    || layer.ParticipantHeaders[index] == null)
                {
                    return ResultFailure(
                        "invalid_cwt_protection_participants",
                        "A multi-party CWT layer contains a missing header or key.");
                }

                index += 1;
            }

            if (layer.SenderKeys != null
                && layer.SenderKeys.Length != layer.Keys.Length)
            {
                return ResultFailure(
                    "invalid_cwt_sender_keys",
                    "Static sender keys must correspond to recipient keys.");
            }

            return ResultSuccess();
        }

        return ResultFailure(
            "invalid_cwt_protection_kind",
            "The CWT protection kind is not supported.");
    }

    static TokenResult EnsureNestedContentType(CwtProtectionLayer layer)
    {
        if (layer.Header == null)
        {
            return ResultFailure(
                "invalid_cwt_protection_layer",
                "A nested CWT protection layer requires a header.");
        }

        CborItem? existing = layer.Header.Get(CoseHeaderLabels.ContentType);
        if (existing != null)
        {
            if (existing.Kind == TokenCborValueKind.TextString
                && string.Equals(
                    existing.Text,
                    CwtMediaType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ResultSuccess();
            }

            if (existing.Kind == TokenCborValueKind.UnsignedInteger
                && existing.IntegerArgument == CwtTag)
            {
                return ResultSuccess();
            }

            return ResultFailure(
                "invalid_nested_cwt_content_type",
                "An outer nested CWT layer must identify application/cwt content.");
        }

        CborItem contentType = new CborItem();
        contentType.Kind = TokenCborValueKind.TextString;
        contentType.Text = CwtMediaType;
        layer.Header.SetProtected(CoseHeaderLabels.ContentType, contentType);
        return ResultSuccess();
    }

    static TokenResult ValidateConfirmationConfidentiality(
        CwtClaims claims,
        CwtProtectionLayer[] layers)
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
        while (layerIndex < layers.Length)
        {
            if (layers[layerIndex] != null
                && (layers[layerIndex].Kind == CwtProtectionKind.Encrypt0
                    || layers[layerIndex].Kind == CwtProtectionKind.Encrypt))
            {
                return ResultSuccess();
            }

            layerIndex += 1;
        }

        return ResultFailure(
            "symmetric_confirmation_key_not_encrypted",
            "A directly embedded symmetric confirmation key requires an encrypted CWT layer.");
    }

    static TokenBytesResult AddCwtTag(byte[]? message, TokenPolicy policy)
    {
        CborReader reader = CborReader.Create(
            (message ?? throw new InvalidOperationException("A required value is null.")),
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0
            || (decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Tag)
        {
            return Failure(
                "invalid_cwt_cose_message",
                "The CWT tag must wrap exactly one tagged COSE message.");
        }

        CborItem tagged = new CborItem();
        tagged.Kind = TokenCborValueKind.Tag;
        tagged.TagValue = CwtTag;
        tagged.TaggedValue = decoded.Item;
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(tagged);
        if (!written.Success)
        {
            return Failure(written.Error);
        }

        return BytesSuccess(writer.ToArray());
    }

    static CoseHeader AlgorithmHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        CborItem value = new CborItem();
        if (algorithm >= 0)
        {
            value.Kind = TokenCborValueKind.UnsignedInteger;
            value.IntegerArgument = (ulong)algorithm;
        }
        else
        {
            value.Kind = TokenCborValueKind.NegativeInteger;
            value.IntegerArgument = (ulong)(-(long)algorithm - 1L);
        }

        header.SetProtected(CoseHeaderLabels.Algorithm, value);
        return header;
    }

    static bool IsSignatureAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP256Sha256
            || algorithm == CoseAlgorithms.EcdsaP384Sha384
            || algorithm == CoseAlgorithms.EcdsaP521Sha512
            || algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256
            || algorithm == CoseAlgorithms.EcdsaSha256
            || algorithm == CoseAlgorithms.EcdsaSha384
            || algorithm == CoseAlgorithms.EcdsaSha512
            || algorithm == CoseAlgorithms.RsaSha256
            || algorithm == CoseAlgorithms.RsaSha384
            || algorithm == CoseAlgorithms.RsaSha512
            || algorithm == CoseAlgorithms.RsaPssSha256
            || algorithm == CoseAlgorithms.RsaPssSha384
            || algorithm == CoseAlgorithms.RsaPssSha512;
    }

    static bool IsMacAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.HmacSha256Truncated64
            || algorithm == CoseAlgorithms.HmacSha256
            || algorithm == CoseAlgorithms.HmacSha384
            || algorithm == CoseAlgorithms.HmacSha512;
    }

    static bool IsContentEncryptionAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.Aes128Gcm
            || algorithm == CoseAlgorithms.Aes192Gcm
            || algorithm == CoseAlgorithms.Aes256Gcm
            || algorithm == CoseAlgorithms.AesCcm16_64_128
            || algorithm == CoseAlgorithms.AesCcm16_64_256
            || algorithm == CoseAlgorithms.AesCcm64_64_128
            || algorithm == CoseAlgorithms.AesCcm64_64_256
            || algorithm == CoseAlgorithms.AesCcm16_128_128
            || algorithm == CoseAlgorithms.AesCcm16_128_256
            || algorithm == CoseAlgorithms.AesCcm64_128_128
            || algorithm == CoseAlgorithms.AesCcm64_128_256;
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

    static TokenBytesResult BytesSuccess(byte[]? bytes)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    static TokenBytesResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
