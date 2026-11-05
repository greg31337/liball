using System;
namespace token;

/// <summary>Routes representation-neutral creation requests to format-specific engines.</summary>
internal static class UniversalTokenCreator
{
    internal static UniversalTokenCreationResult CreateToken(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (request.JoseProtectedHeaders == null
            || request.JoseUnprotectedHeaders == null
            || request.CoseParticipantHeaders == null
            || request.Keys == null
            || request.CwtProtectionLayers == null
            || request.ExternalAuthenticatedData == null)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "The universal token creation request is incomplete.");
        }

        TokenResult entries = ValidateRequestEntries(request);
        if (!entries.Success)
        {
            return FailureError(entries.Error);
        }

        if (request.Kind == TokenKind.JwsCompact)
        {
            return CreateCompactSignature(request, policy);
        }

        if (request.Kind == TokenKind.JweCompact)
        {
            return CreateCompactEncryption(request, policy);
        }

        if (request.Kind == TokenKind.JwsJson)
        {
            return CreateJsonSignature(request, policy);
        }

        if (request.Kind == TokenKind.JweJson)
        {
            return CreateJsonEncryption(request, policy);
        }

        if (request.Kind == TokenKind.Cose)
        {
            return CreateCoseMessage(request, policy);
        }

        if (request.Kind == TokenKind.Cwt)
        {
            return CreateCwtMessage(request, policy);
        }

        if (request.Kind == TokenKind.SdJwt)
        {
            return FailureCode(
                "unsupported_token_creation",
                "Universal SD-JWT creation is not supported.");
        }

        return FailureCode(
            "unsupported_token_creation",
            "The requested token representation cannot be created.");
    }

    static TokenResult ValidateRequestEntries(
        UniversalTokenCreationRequest request)
    {
        int index = 0;
        while (index < request.JoseProtectedHeaders.Length)
        {
            if (request.JoseProtectedHeaders[index] == null)
            {
                return InvalidRequestEntries(
                    "JOSE protected header entries cannot be null.");
            }

            index += 1;
        }

        index = 0;
        while (index < request.CoseParticipantHeaders.Length)
        {
            if (request.CoseParticipantHeaders[index] == null)
            {
                return InvalidRequestEntries(
                    "COSE participant header entries cannot be null.");
            }

            index += 1;
        }

        index = 0;
        while (index < request.Keys.Length)
        {
            if (request.Keys[index] == null)
            {
                return InvalidRequestEntries(
                    "Token creation key entries cannot be null.");
            }

            index += 1;
        }

        index = 0;
        while (index < request.CwtProtectionLayers.Length)
        {
            if (request.CwtProtectionLayers[index] == null)
            {
                return InvalidRequestEntries(
                    "CWT protection layer entries cannot be null.");
            }

            index += 1;
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult InvalidRequestEntries(string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(
            "invalid_universal_creation_request",
            message,
            -1);
        return result;
    }

    static UniversalTokenCreationResult CreateCompactSignature(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Nested)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "A compact JWS request cannot select nested creation.");
        }

        if (request.Keys.Length != 1)
        {
            return MissingKeys("Compact JWS creation requires one signing key.");
        }

        TokenTextResult created;
        if (request.JwtClaims != null
            && request.JoseProtectedHeaders.Length == 0
            && !request.Detached)
        {
            created = new JwtTokenService().CreateSignedJwt(
                request.JwtClaims,
                request.Keys[0],
                policy);
        }
        else
        {
            TokenBytesResult payload = GetJosePayload(request, policy);
            if (!payload.Success)
            {
                return FailureError(payload.Error);
            }

            if (request.JoseProtectedHeaders.Length != 1)
            {
                return MissingHeaders(
                    "Compact JWS creation requires one protected header.");
            }

            JoseTokenService service = new JoseTokenService();
            created = request.Detached
                ? service.SignDetachedJwsCompact(
                    (payload.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                    request.JoseProtectedHeaders[0],
                    request.Keys[0],
                    policy)
                : service.SignJwsCompact(
                    (payload.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                    request.JoseProtectedHeaders[0],
                    request.Keys[0],
                    policy);
        }

        return FromTextResult(
            created,
            request.Kind,
            request.Detached ? GetDetachedPayload(request, policy) : null);
    }

    static UniversalTokenCreationResult CreateCompactEncryption(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Detached)
        {
            return FailureCode(
                "unsupported_detached_creation",
                "Compact JWE creation does not support detached ciphertext.");
        }

        if (request.Nested)
        {
            return CreateNestedCompactJwt(request, policy);
        }

        if (request.Keys.Length != 1)
        {
            return MissingKeys("Compact JWE creation requires one encryption key.");
        }

        TokenTextResult created;
        if (request.JwtClaims != null
            && request.JoseProtectedHeaders.Length == 0
            && request.ContentEncryptionAlgorithm != null)
        {
            created = new JwtTokenService().CreateEncryptedJwt(
                request.JwtClaims,
                request.Keys[0],
                request.ContentEncryptionAlgorithm,
                policy);
        }
        else
        {
            TokenBytesResult payload = GetJosePayload(request, policy);
            if (!payload.Success)
            {
                return FailureError(payload.Error);
            }

            if (request.JoseProtectedHeaders.Length != 1)
            {
                return MissingHeaders(
                    "Compact JWE creation requires one protected header.");
            }

            created = new JoseTokenService().EncryptJweCompact(
                (payload.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                request.JoseProtectedHeaders[0],
                request.Keys[0],
                policy);
        }

        return FromTextResult(created, request.Kind, null);
    }

    static UniversalTokenCreationResult CreateNestedCompactJwt(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.JwtClaims == null)
        {
            return FailureCode(
                "missing_creation_claims",
                "Nested compact JWT creation requires JWT claims.");
        }

        if (request.Keys.Length != 2)
        {
            return MissingKeys(
                "Nested compact JWT creation requires signing and encryption keys.");
        }

        TokenTextResult created;
        JwtTokenService service = new JwtTokenService();
        if (request.JoseProtectedHeaders.Length == 2)
        {
            created = service.CreateSignedThenEncryptedJwtWithHeaders(
                request.JwtClaims,
                request.Keys[0],
                request.Keys[1],
                request.JoseProtectedHeaders[0],
                request.JoseProtectedHeaders[1],
                policy);
        }
        else if (request.JoseProtectedHeaders.Length == 0
            && request.ContentEncryptionAlgorithm != null)
        {
            created = service.CreateSignedThenEncryptedJwt(
                request.JwtClaims,
                request.Keys[0],
                request.Keys[1],
                request.ContentEncryptionAlgorithm,
                policy);
        }
        else
        {
            return MissingHeaders(
                "Nested compact JWT creation requires two protected headers or a content-encryption algorithm.");
        }

        return FromTextResult(created, request.Kind, null);
    }

    static UniversalTokenCreationResult CreateJsonSignature(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Nested)
        {
            return FailureCode(
                "unsupported_nested_creation",
                "Universal nested JWS JSON creation is not supported.");
        }

        if (request.Keys.Length == 0
            || request.JoseProtectedHeaders.Length != request.Keys.Length)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "JWS JSON protected headers and signing keys must be nonempty and correspond by index.");
        }

        if (request.JoseUnprotectedHeaders.Length != 0
            && request.JoseUnprotectedHeaders.Length != request.Keys.Length)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "JWS JSON unprotected headers must correspond to signing keys.");
        }

        TokenBytesResult payload = GetJosePayload(request, policy);
        if (!payload.Success)
        {
            return FailureError(payload.Error);
        }

        byte[] requiredPayload = payload.Bytes
            ?? throw new InvalidOperationException("A required value is null.");
        JoseTokenService service = new JoseTokenService();
        TokenTextResult created;
        if (request.Keys.Length == 1)
        {
            JoseHeader? unprotected = request.JoseUnprotectedHeaders.Length == 0
                ? null
                : request.JoseUnprotectedHeaders[0];
            created = request.Detached
                ? service.SignDetachedJwsFlattened(
                    requiredPayload,
                    request.JoseProtectedHeaders[0],
                    unprotected,
                    request.Keys[0],
                    policy)
                : service.SignJwsFlattened(
                    requiredPayload,
                    request.JoseProtectedHeaders[0],
                    unprotected,
                    request.Keys[0],
                    policy);
        }
        else
        {
            JoseHeader?[]? unprotected =
                request.JoseUnprotectedHeaders.Length == 0
                ? null
                : request.JoseUnprotectedHeaders;
            created = request.Detached
                ? service.SignDetachedJwsGeneral(
                    requiredPayload,
                    request.JoseProtectedHeaders,
                    unprotected,
                    request.Keys,
                    policy)
                : service.SignJwsGeneral(
                    requiredPayload,
                    request.JoseProtectedHeaders,
                    unprotected,
                    request.Keys,
                    policy);
        }

        return FromTextResult(
            created,
            request.Kind,
            request.Detached ? requiredPayload : null);
    }

    static UniversalTokenCreationResult CreateJsonEncryption(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Detached)
        {
            return FailureCode(
                "unsupported_detached_creation",
                "JWE JSON creation does not support detached ciphertext.");
        }

        if (request.Nested)
        {
            return FailureCode(
                "unsupported_nested_creation",
                "Universal nested JWE JSON creation is not supported.");
        }

        if (request.Keys.Length == 0
            || request.JoseProtectedHeaders.Length != 1)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "JWE JSON creation requires one shared protected header and at least one recipient key.");
        }

        if (request.JoseUnprotectedHeaders.Length != 0
            && request.JoseUnprotectedHeaders.Length != request.Keys.Length)
        {
            return FailureCode(
                "invalid_universal_creation_request",
                "JWE JSON recipient headers must correspond to recipient keys.");
        }

        TokenBytesResult payload = GetJosePayload(request, policy);
        if (!payload.Success)
        {
            return FailureError(payload.Error);
        }

        byte[] requiredPayload = payload.Bytes
            ?? throw new InvalidOperationException("A required value is null.");
        JoseTokenService service = new JoseTokenService();
        TokenTextResult created;
        if (request.Keys.Length == 1)
        {
            JoseHeader? recipient = request.JoseUnprotectedHeaders.Length == 0
                ? null
                : request.JoseUnprotectedHeaders[0];
            created = request.ExternalAuthenticatedData.Length == 0
                ? service.EncryptJweFlattened(
                    requiredPayload,
                    request.JoseProtectedHeaders[0],
                    request.JoseSharedUnprotectedHeader,
                    recipient,
                    request.Keys[0],
                    policy)
                : service.EncryptJweFlattenedWithAuthenticatedData(
                    requiredPayload,
                    request.ExternalAuthenticatedData,
                    request.JoseProtectedHeaders[0],
                    request.JoseSharedUnprotectedHeader,
                    recipient,
                    request.Keys[0],
                    policy);
        }
        else
        {
            JoseHeader?[] recipients = request.JoseUnprotectedHeaders.Length == 0
                ? EmptyJoseHeaders(request.Keys.Length)
                : request.JoseUnprotectedHeaders;
            created = request.ExternalAuthenticatedData.Length == 0
                ? service.EncryptJweGeneral(
                    requiredPayload,
                    request.JoseProtectedHeaders[0],
                    request.JoseSharedUnprotectedHeader,
                    recipients,
                    request.Keys,
                    policy)
                : service.EncryptJweGeneralWithAuthenticatedData(
                    requiredPayload,
                    request.ExternalAuthenticatedData,
                    request.JoseProtectedHeaders[0],
                    request.JoseSharedUnprotectedHeader,
                    recipients,
                    request.Keys,
                    policy);
        }

        return FromTextResult(created, request.Kind, null);
    }

    static UniversalTokenCreationResult CreateCoseMessage(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Nested)
        {
            return FailureCode(
                "unsupported_nested_creation",
                "Raw COSE nesting must be constructed explicitly as payload bytes.");
        }

        if (request.Payload == null)
        {
            return FailureCode(
                "missing_creation_payload",
                "COSE creation requires payload or plaintext bytes.");
        }

        if (request.CoseHeader == null)
        {
            return MissingHeaders("COSE creation requires a message header.");
        }

        if (request.Keys.Length == 0)
        {
            return MissingKeys("COSE creation requires at least one key.");
        }

        if (request.CoseKind == CoseMessageKind.Sign1)
        {
            return CreateCoseSign1Message(request, policy);
        }

        if (request.CoseKind == CoseMessageKind.Sign)
        {
            return CreateCoseSignMessage(request, policy);
        }

        if (request.CoseKind == CoseMessageKind.Mac0)
        {
            return CreateCoseMac0Message(request, policy);
        }

        if (request.CoseKind == CoseMessageKind.Mac)
        {
            return CreateCoseMacMessage(request, policy);
        }

        if (request.CoseKind == CoseMessageKind.Encrypt0)
        {
            return CreateCoseEncrypt0Message(request, policy);
        }

        if (request.CoseKind == CoseMessageKind.Encrypt)
        {
            return CreateCoseEncryptMessage(request, policy);
        }

        return FailureCode(
            "unsupported_cose_creation_kind",
            "The requested COSE message kind cannot be created.");
    }

    static UniversalTokenCreationResult CreateCoseSign1Message(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Keys.Length != 1)
        {
            return MissingKeys("COSE_Sign1 creation requires one key.");
        }

        CoseSigningService service = new CoseSigningService();
        TokenBytesResult created = request.Detached
            ? service.CreateDetachedCoseSign1(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.Keys[0],
                request.Tagged,
                request.AllowCompatibility,
                policy)
            : service.CreateCoseSign1(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.Keys[0],
                request.Tagged,
                request.AllowCompatibility,
                policy);
        return FromBytesResult(
            created,
            request.Kind,
            request.Detached ? request.Payload : null);
    }

    static UniversalTokenCreationResult CreateCoseSignMessage(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        CoseSigningService service = new CoseSigningService();
        TokenBytesResult created = request.Detached
            ? service.CreateDetachedCoseSign(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.CoseParticipantHeaders,
                request.Keys,
                request.Tagged,
                request.AllowCompatibility,
                policy)
            : service.CreateCoseSign(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.CoseParticipantHeaders,
                request.Keys,
                request.Tagged,
                request.AllowCompatibility,
                policy);
        return FromBytesResult(
            created,
            request.Kind,
            request.Detached ? request.Payload : null);
    }

    static UniversalTokenCreationResult CreateCoseMac0Message(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Keys.Length != 1)
        {
            return MissingKeys("COSE_Mac0 creation requires one key.");
        }

        CoseEncryptionService service = new CoseEncryptionService();
        TokenBytesResult created = request.Detached
            ? service.CreateDetachedCoseMac0(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.Keys[0],
                request.Tagged,
                policy)
            : service.CreateCoseMac0(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.Keys[0],
                request.Tagged,
                policy);
        return FromBytesResult(
            created,
            request.Kind,
            request.Detached ? request.Payload : null);
    }

    static UniversalTokenCreationResult CreateCoseMacMessage(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        CoseEncryptionService service = new CoseEncryptionService();
        TokenBytesResult created = request.Detached
            ? service.CreateDetachedCoseMac(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.CoseParticipantHeaders,
                request.Keys,
                request.SenderKeys,
                request.Tagged,
                policy)
            : service.CreateCoseMac(
                (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                request.ExternalAuthenticatedData,
                (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                request.CoseParticipantHeaders,
                request.Keys,
                request.SenderKeys,
                request.Tagged,
                policy);
        return FromBytesResult(
            created,
            request.Kind,
            request.Detached ? request.Payload : null);
    }

    static UniversalTokenCreationResult CreateCoseEncrypt0Message(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Keys.Length != 1)
        {
            return MissingKeys("COSE_Encrypt0 creation requires one key.");
        }

        CoseEncryptionService service = new CoseEncryptionService();
        if (request.Detached)
        {
            CoseDetachedEncryptionResult detached =
                service.EncryptDetachedCoseEncrypt0(
                    (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                    request.ExternalAuthenticatedData,
                    (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                    request.Keys[0],
                    request.Tagged,
                    policy);
            return FromDetachedEncryption(detached, request.Kind);
        }

        TokenBytesResult created = service.EncryptCoseEncrypt0(
            (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
            request.ExternalAuthenticatedData,
            (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
            request.Keys[0],
            request.Tagged,
            policy);
        return FromBytesResult(created, request.Kind, null);
    }

    static UniversalTokenCreationResult CreateCoseEncryptMessage(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        CoseEncryptionService service = new CoseEncryptionService();
        if (request.Detached)
        {
            CoseDetachedEncryptionResult detached =
                service.EncryptDetachedCoseEncrypt(
                    (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
                    request.ExternalAuthenticatedData,
                    (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
                    request.CoseParticipantHeaders,
                    request.Keys,
                    request.SenderKeys,
                    request.Tagged,
                    policy);
            return FromDetachedEncryption(detached, request.Kind);
        }

        TokenBytesResult created = service.EncryptCoseEncrypt(
            (request.Payload ?? throw new InvalidOperationException("A required value is null.")),
            request.ExternalAuthenticatedData,
            (request.CoseHeader ?? throw new InvalidOperationException("A required value is null.")),
            request.CoseParticipantHeaders,
            request.Keys,
            request.SenderKeys,
            request.Tagged,
            policy);
        return FromBytesResult(created, request.Kind, null);
    }

    static UniversalTokenCreationResult CreateCwtMessage(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.Detached)
        {
            return FailureCode(
                "unsupported_detached_creation",
                "CWT creation does not support detached claims.");
        }

        if (request.CwtClaims == null)
        {
            return FailureCode(
                "missing_creation_claims",
                "CWT creation requires a CWT Claims Set.");
        }

        if (request.CwtProtectionLayers.Length == 0)
        {
            return FailureCode(
                "missing_cwt_protection",
                "CWT creation requires at least one protection layer.");
        }

        CwtTokenService service = new CwtTokenService();
        TokenBytesResult created = request.Tagged
            ? service.CreateTaggedCwt(
                request.CwtClaims,
                request.CwtProtectionLayers,
                policy)
            : service.CreateCwt(
                request.CwtClaims,
                request.CwtProtectionLayers,
                policy);
        return FromBytesResult(created, request.Kind, null);
    }

    static TokenBytesResult GetJosePayload(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        if (request.JwtClaims != null)
        {
            return request.JwtClaims.WriteUtf8WithPolicy(policy);
        }

        TokenBytesResult result = new TokenBytesResult();
        if (request.Payload == null)
        {
            result.Error = new TokenError(
                "missing_creation_payload",
                "JOSE creation requires payload bytes or JWT claims.",
                -1);
            return result;
        }

        result.Success = true;
        result.Bytes = request.Payload.AsSpan().ToArray();
        return result;
    }

    static byte[]? GetDetachedPayload(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        TokenBytesResult payload = GetJosePayload(request, policy);
        return payload.Success ? payload.Bytes : null;
    }

    static JoseHeader?[] EmptyJoseHeaders(int length)
    {
        return new JoseHeader?[length];
    }

    static UniversalTokenCreationResult FromTextResult(
        TokenTextResult source,
        TokenKind kind,
        byte[]? detachedContent)
    {
        if (!source.Success)
        {
            return FailureError(source.Error);
        }

        UniversalTokenCreationResult result =
            new UniversalTokenCreationResult();
        result.Success = true;
        result.Kind = kind;
        result.Text = source.Text;
        result.DetachedContent = detachedContent == null
            ? null
            : detachedContent.AsSpan().ToArray();
        return result;
    }

    static UniversalTokenCreationResult FromBytesResult(
        TokenBytesResult source,
        TokenKind kind,
        byte[]? detachedContent)
    {
        if (!source.Success)
        {
            return FailureError(source.Error);
        }

        UniversalTokenCreationResult result =
            new UniversalTokenCreationResult();
        result.Success = true;
        result.Kind = kind;
        result.Bytes = source.Bytes;
        result.DetachedContent = detachedContent == null
            ? null
            : detachedContent.AsSpan().ToArray();
        return result;
    }

    static UniversalTokenCreationResult FromDetachedEncryption(
        CoseDetachedEncryptionResult source,
        TokenKind kind)
    {
        if (!source.Success)
        {
            return FailureError(source.Error);
        }

        UniversalTokenCreationResult result =
            new UniversalTokenCreationResult();
        result.Success = true;
        result.Kind = kind;
        result.Bytes = source.Bytes;
        result.DetachedContent = source.Ciphertext;
        return result;
    }

    static UniversalTokenCreationResult MissingHeaders(string message)
    {
        return FailureCode("missing_creation_header", message);
    }

    static UniversalTokenCreationResult MissingKeys(string message)
    {
        return FailureCode("missing_creation_key", message);
    }

    static UniversalTokenCreationResult FailureCode(
        string code,
        string message)
    {
        return FailureError(new TokenError(code, message, -1));
    }

    static UniversalTokenCreationResult FailureError(TokenError? error)
    {
        UniversalTokenCreationResult result =
            new UniversalTokenCreationResult();
        result.Error = error;
        return result;
    }
}
