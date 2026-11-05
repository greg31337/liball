using System;
namespace token;

/// <summary>Creates and verifies RFC 9052 COSE_Mac0 messages.</summary>
internal static class CoseMac0Operations
{
    /// <summary>Creates a tagged COSE_Mac0 message.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The headers, including a protected HMAC algorithm.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The tagged MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            header,
            key,
            true,
            false,
            policy);
    }

    /// <summary>Creates a COSE_Mac0 message with explicit registered-tag control.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The headers, including a protected HMAC algorithm.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            header,
            key,
            tagged,
            false,
            policy);
    }

    /// <summary>Creates an attached COSE_Mac0 message with external authenticated data.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The headers, including a protected HMAC algorithm.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            false,
            policy);
    }

    /// <summary>Creates a COSE_Mac0 message whose authenticated payload is detached.</summary>
    /// <param name="payload">The detached payload bytes to authenticate.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The headers, including a protected HMAC algorithm.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The MAC message with a null payload field or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateDetached(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            true,
            policy);
    }

    static TokenBytesResult CreateCore(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool detached,
        TokenPolicy policy)
    {
        ValidateCreateReferences(payload, header, key, policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        TokenResult headerResult = ValidateHeader(header, policy);
        if (!headerResult.Success)
        {
            return BytesFailure(headerResult.Error);
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!protectedBytes.Success)
        {
            return protectedBytes;
        }

        TokenBytesResult authenticated = CoseMacStructure.CreateMac0(
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            payload,
            policy);
        if (!authenticated.Success)
        {
            return authenticated;
        }

        TokenBytesResult tag = CoseHmacAlgorithms.Create(
            (authenticated.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            key,
            header.Algorithm);
        if (!tag.Success)
        {
            return tag;
        }

        CoseMac0 message = new CoseMac0();
        message.Header = header;
        message.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        message.Payload = detached ? null : payload;
        message.DetachedPayload = detached;
        message.Tag = tag.Bytes;
        message.Tagged = tagged;
        return message.WriteWithTagAndPolicy(tagged, policy);
    }

    /// <summary>Verifies an attached COSE_Mac0 message.</summary>
    /// <param name="cbor">The tagged or contextually untagged message bytes.</param>
    /// <param name="key">The symmetric MAC verification key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMac0Result Verify(
        byte[] cbor,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            Array.Empty<byte>(),
            key,
            false,
            policy);
    }

    /// <summary>Verifies an attached COSE_Mac0 message with external authenticated data.</summary>
    /// <param name="cbor">The tagged or contextually untagged message bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The symmetric MAC verification key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMac0Result VerifyWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            externalAuthenticatedData,
            key,
            false,
            policy);
    }

    /// <summary>Verifies a COSE_Mac0 message against detached payload and external authenticated data.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The externally supplied detached payload.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The symmetric MAC verification key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The parsed verified message containing the supplied payload or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMac0Result VerifyDetached(
        byte[] cbor,
        byte[] payload,
        byte[] externalAuthenticatedData,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            payload,
            externalAuthenticatedData,
            key,
            true,
            policy);
    }

    static CoseMac0Result VerifyCore(
        byte[] cbor,
        byte[]? detachedPayload,
        byte[] externalAuthenticatedData,
        TokenKey key,
        bool detached,
        TokenPolicy policy)
    {
        ValidateVerifyReferences(cbor, key, policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedPayload == null)
        {
            throw new ArgumentNullException("detachedPayload");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseMac0Result parsed = CoseMac0.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload && !detached)
        {
            return Failure(
                "missing_payload",
                "Attached COSE_Mac0 verification requires payload bytes.");
        }

        if (!parsed.Message.DetachedPayload && detached)
        {
            return Failure(
                "unexpected_attached_payload",
                "Detached COSE_Mac0 verification requires a null payload field.");
        }

        TokenResult headerResult = ValidateHeader(
            parsed.Message.Header,
            policy);
        if (!headerResult.Success)
        {
            return Failure(headerResult.Error);
        }

        TokenBytesResult authenticated = CoseMacStructure.CreateMac0(
            (parsed.Message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            externalAuthenticatedData,
            ((detached ? detachedPayload : parsed.Message.Payload)
                ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!authenticated.Success)
        {
            return Failure(authenticated.Error);
        }

        TokenResult verified = CoseHmacAlgorithms.Verify(
            (authenticated.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            (parsed.Message.Tag ?? throw new InvalidOperationException("A required value is null.")),
            key,
            parsed.Message.Header.Algorithm);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        if (detached)
        {
            parsed.Message.Payload = Copy(detachedPayload);
        }

        parsed.Message.TagVerified = true;
        return parsed;
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
                "COSE_Mac0 requires a message authentication algorithm.");
        }

        if (header.GetProtected(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE_Mac0 algorithm must be protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE_Mac0 algorithm must use an integer label.");
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

    static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Mac0 cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
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

        if (key == null)
        {
            throw new ArgumentNullException("key");
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

    static byte[] Copy(byte[]? value)
    {
        return value.AsSpan().ToArray();
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

    static CoseMac0Result Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseMac0Result Failure(TokenError? error)
    {
        CoseMac0Result result = new CoseMac0Result();
        result.Error = error;
        return result;
    }
}
