using System;
using System.Collections.Generic;
using System.Globalization;

namespace token;

/// <summary>Provides representation-neutral token inspection operations.</summary>
internal static class UniversalToken
{
    const ulong CwtTag = 61;

    /// <summary>Inspects headers from a supported text token without exposing payload content.</summary>
    /// <remarks>The returned headers are parsed but are not cryptographically authenticated.</remarks>
    /// <param name="text">The token text to inspect.</param>
    /// <param name="policy">The token size, JSON depth, and JOSE header policy.</param>
    /// <returns>The token kind and unauthenticated headers, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenInspectionResult Inspect(
        string text,
        TokenPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenInspectionResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return policyResult;
        }

        TokenBytesResult encoded = Utf8Codec.Encode(text);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The token exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        if (text.IndexOf('~') >= 0)
        {
            return InspectSelectiveDisclosure(text, policy);
        }

        JoseCompactParseResult compact = JoseCompact.Parse(text);
        if (compact.Success)
        {
            return InspectCompact(text, (compact.Serialization ?? throw new InvalidOperationException("A required value is null.")).Kind, policy);
        }

        return InspectJson(text, policy);
    }

    /// <summary>Inspects headers from a supported binary token without exposing payload content.</summary>
    /// <remarks>
    /// Tagged COSE and CWT are inspected as binary tokens. Valid UTF-8 input is
    /// inspected as a text token when it is not a supported binary form.
    /// Returned headers are not cryptographically authenticated.
    /// </remarks>
    /// <param name="bytes">The encoded token bytes to inspect.</param>
    /// <param name="policy">The token size, CBOR depth, and header policy.</param>
    /// <returns>The token kind and unauthenticated headers, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenInspectionResult InspectBytes(
        byte[] bytes,
        TokenPolicy policy)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenInspectionResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return policyResult;
        }

        if (bytes.Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The token exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        TokenInspectionResult binary = InspectCose(bytes, policy);
        if (binary.Success || IsTerminalBinaryFailure(binary.Error))
        {
            return binary;
        }

        TokenTextResult text = Utf8Codec.Decode(bytes);
        if (text.Success)
        {
            return Inspect((text.Text ?? throw new InvalidOperationException("A required value is null.")), policy);
        }

        return binary;
    }

    /// <summary>Authenticates and validates a supported text token.</summary>
    /// <remarks>
    /// Compact JWT, JWS JSON, JWE JSON, and SD-JWT representations are
    /// supported. Detached payloads require a representation-specific API.
    /// </remarks>
    /// <param name="text">The token text to validate.</param>
    /// <param name="resolver">The resolver used to select cryptographic keys.</param>
    /// <param name="policy">The algorithm, claim, and resource policy.</param>
    /// <returns>Authenticated token metadata and claims, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenValidationResult Validate(
        string text,
        ITokenKeyResolver resolver,
        TokenPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (resolver == null)
        {
            throw new ArgumentNullException("resolver");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JwtValidationPolicy jwtPolicy = JwtValidationPolicy.Create(policy);
        TokenResult policyResult = jwtPolicy.Validate();
        if (!policyResult.Success || policy.MaximumCborDepth <= 0)
        {
            return policyResult.Success
                ? ValidationFailure(
                    "invalid_policy",
                    "The universal token policy contains an invalid CBOR depth limit.",
                    TokenValidationDiagnosticStage.Policy,
                    TokenKind.Unknown)
                : ValidationFailure(
                    policyResult.Error,
                    TokenValidationDiagnosticStage.Policy,
                    TokenKind.Unknown);
        }

        TokenInspectionResult inspected = Inspect(text, policy);
        if (!inspected.Success)
        {
            return ValidationFailure(
                inspected.Error,
                DiagnosticStage(
                    inspected.Error,
                    TokenValidationDiagnosticStage.Inspection),
                TokenKind.Unknown);
        }

        if (inspected.Kind == TokenKind.JwsCompact
            || inspected.Kind == TokenKind.JweCompact)
        {
            JwtValidationResult validated = JwtValidator.Validate(
                text,
                resolver,
                jwtPolicy);
            if (!validated.Success)
            {
                return ValidationFailure(
                    validated.Error,
                    DiagnosticStage(
                        validated.Error,
                        TokenValidationDiagnosticStage.Validation),
                    inspected.Kind);
            }

            return FromCompactJwt(validated, inspected.Headers);
        }

        if (inspected.Kind == TokenKind.SdJwt)
        {
            return ValidateSelectiveDisclosure(
                text,
                resolver,
                jwtPolicy,
                inspected.Headers);
        }

        if (inspected.Kind == TokenKind.JwsJson)
        {
            return ValidateJsonSignature(
                text,
                resolver,
                jwtPolicy,
                inspected.Headers);
        }

        if (inspected.Kind == TokenKind.JweJson)
        {
            return ValidateJsonEncryption(
                text,
                resolver,
                jwtPolicy,
                inspected.Headers);
        }

        return ValidationFailure(
            "unsupported_token_kind",
            "The text token representation is not supported for validation.",
            TokenValidationDiagnosticStage.Unsupported,
            inspected.Kind);
    }

    /// <summary>Authenticates and validates a supported binary token.</summary>
    /// <remarks>
    /// Tagged COSE and CWT representations are validated as protected CWTs.
    /// Valid UTF-8 input falls back to universal text-token validation.
    /// </remarks>
    /// <param name="bytes">The encoded token bytes to validate.</param>
    /// <param name="resolver">The resolver used to select cryptographic keys.</param>
    /// <param name="policy">The algorithm, claim, and resource policy.</param>
    /// <returns>Authenticated token metadata and claims, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenValidationResult ValidateBytes(
        byte[] bytes,
        ITokenKeyResolver resolver,
        TokenPolicy policy)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (resolver == null)
        {
            throw new ArgumentNullException("resolver");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenInspectionResult inspected = InspectBytes(bytes, policy);
        if (!inspected.Success)
        {
            return ValidationFailure(
                inspected.Error,
                DiagnosticStage(
                    inspected.Error,
                    TokenValidationDiagnosticStage.Inspection),
                TokenKind.Unknown);
        }

        if (inspected.Kind != TokenKind.Cose
            && inspected.Kind != TokenKind.Cwt)
        {
            TokenTextResult text = Utf8Codec.Decode(bytes);
            if (!text.Success)
            {
                return ValidationFailure(
                    text.Error,
                    TokenValidationDiagnosticStage.Parsing,
                    inspected.Kind);
            }

            return Validate((text.Text ?? throw new InvalidOperationException("A required value is null.")), resolver, policy);
        }

        CwtValidationPolicy cwtPolicy = CwtValidationPolicy.Create(policy);
        CwtValidationResult validated = CwtValidator.Validate(
            bytes,
            resolver,
            cwtPolicy);
        if (!validated.Success)
        {
            return ValidationFailure(
                validated.Error,
                DiagnosticStage(
                    validated.Error,
                    TokenValidationDiagnosticStage.Validation),
                inspected.Kind);
        }

        return FromCwt(
            inspected.Kind,
            validated,
            inspected.Headers);
    }

    static TokenValidationResult ValidateSelectiveDisclosure(
        string text,
        ITokenKeyResolver resolver,
        JwtValidationPolicy jwtPolicy,
        TokenHeaderInspection[] headers)
    {
        SelectiveDisclosureJwtValidationPolicy policy =
            new SelectiveDisclosureJwtValidationPolicy();
        policy.JwtPolicy = jwtPolicy;
        if (policy.MaximumDisclosureSizeInBytes
            > (jwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumTokenSizeInBytes)
        {
            policy.MaximumDisclosureSizeInBytes =
                jwtPolicy.TokenPolicy.MaximumTokenSizeInBytes;
        }

        SelectiveDisclosureJwtResult validated =
            SelectiveDisclosureJwt.Validate(text, resolver, policy);
        if (!validated.Success)
        {
            return ValidationFailure(
                validated.Error,
                TokenValidationDiagnosticStage.Validation,
                TokenKind.SdJwt);
        }

        if (validated.KeyBindingJwt != null)
        {
            return ValidationFailure(
                "unvalidated_sd_jwt_key_binding",
                "Universal SD-JWT validation cannot authenticate a key-binding JWT without holder-key context.",
                TokenValidationDiagnosticStage.Unsupported,
                TokenKind.SdJwt);
        }

        TokenValidationResult result = FromJwtClaims(
            TokenKind.SdJwt,
            validated.Claims,
            headers);
        result.InnerKind = TokenKind.JwsCompact;
        result.Algorithm = (validated.IssuerJwt ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        result.Algorithms = ValueArray(result.Algorithm);
        result.KeyId = validated.IssuerJwt.SigningKeyId;
        result.KeyIds = ValueArray(result.KeyId);
        result.TokenType = validated.IssuerJwt.TokenType;
        result.SignatureValidated = true;
        result.Nested = true;
        result.Success = true;
        return result;
    }

    static TokenValidationResult ValidateJsonSignature(
        string text,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy,
        TokenHeaderInspection[] headers)
    {
        JsonObjectResult root = JsonObject.Read(
            text,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumJsonDepth);
        if ((root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("signatures") != null)
        {
            return ValidateGeneralSignature(
                text,
                resolver,
                policy,
                headers);
        }

        JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
            text,
            policy.TokenPolicy);
        if (!parsed.Success)
        {
            return ValidationFailure(
                parsed.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        if ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload)
        {
            return ValidationFailure(
                "detached_payload",
                "Universal validation requires an attached JWS payload.",
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        if (parsed.Token.Unsecured)
        {
            return ValidationFailure(
                "jwt_signature_required",
                "Universal JWS validation requires a cryptographic signature.",
                TokenValidationDiagnosticStage.Authentication,
                TokenKind.JwsJson);
        }

        JwtClaimsResult unverified = JwtClaims.Read(
            (parsed.Token.ParsedPayload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!unverified.Success)
        {
            return ValidationFailure(
                unverified.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        TokenKeyResolutionContext context = SignatureContext(
            parsed.Token.ProtectedHeader,
            parsed.Token.Algorithm,
            parsed.Token.KeyId,
            unverified.Claims,
            TokenKind.JwsJson);
        TokenKeyResult resolved = TokenKeyResolution.Resolve(
            resolver,
            context);
        if (!resolved.Success)
        {
            return ValidationFailure(
                resolved.Error,
                TokenValidationDiagnosticStage.KeyResolution,
                TokenKind.JwsJson);
        }

        JwsFlattenedResult verified = JsonWebSignatureFlattened.Verify(
            text,
            (resolved.Key ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!verified.Success)
        {
            return ValidationFailure(
                verified.Error,
                TokenValidationDiagnosticStage.Authentication,
                TokenKind.JwsJson);
        }

        JwtClaimsResult claims = JwtClaims.Read(
            ((verified.Token ?? throw new InvalidOperationException("A required value is null.")).Payload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!claims.Success)
        {
            return ValidationFailure(
                claims.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JwsJson);
        }

        TokenResult claimsResult = ValidateJwtClaims(
            claims.Claims,
            verified.Token.ProtectedHeader,
            policy,
            false);
        if (!claimsResult.Success)
        {
            return ValidationFailure(
                claimsResult.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JwsJson);
        }

        TokenValidationResult result = FromJwtClaims(
            TokenKind.JwsJson,
            claims.Claims,
            headers);
        result.Algorithm = verified.Token.Algorithm;
        result.Algorithms = ValueArray(result.Algorithm);
        result.KeyId = verified.Token.KeyId;
        result.KeyIds = ValueArray(result.KeyId);
        result.TokenType = HeaderString(
            verified.Token.ProtectedHeader,
            JoseHeaderNames.Type);
        result.SignatureValidated = true;
        result.Success = true;
        return result;
    }

    static TokenValidationResult ValidateGeneralSignature(
        string text,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy,
        TokenHeaderInspection[] headers)
    {
        JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(
            text,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return ValidationFailure(
                parsed.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        if ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload)
        {
            return ValidationFailure(
                "detached_payload",
                "Universal validation requires an attached JWS payload.",
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        JwtClaimsResult unverified = JwtClaims.Read(
            (parsed.Token.ParsedPayload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!unverified.Success)
        {
            return ValidationFailure(
                unverified.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JwsJson);
        }

        TokenKey[] keys = new TokenKey[parsed.Token.Signatures.Length];
        string[] algorithms = new string[parsed.Token.Signatures.Length];
        string[] keyIds = new string[parsed.Token.Signatures.Length];
        int index = 0;
        while (index < parsed.Token.Signatures.Length)
        {
            JwsGeneralSignature signature = parsed.Token.Signatures[index];
            if (signature.Unsecured)
            {
                return ValidationFailure(
                    "jwt_signature_required",
                    "Every general JWS entry requires a cryptographic signature.",
                    TokenValidationDiagnosticStage.Authentication,
                    TokenKind.JwsJson,
                    index);
            }

            TokenKeyResolutionContext context = SignatureContext(
                signature.ProtectedHeader,
                signature.Algorithm,
                signature.KeyId,
                unverified.Claims,
                TokenKind.JwsJson);
            TokenKeyResult resolved = TokenKeyResolution.Resolve(
                resolver,
                context);
            if (!resolved.Success)
            {
                return ValidationFailure(
                    resolved.Error,
                    TokenValidationDiagnosticStage.KeyResolution,
                    TokenKind.JwsJson,
                    index);
            }

            TokenResult type = JwtTokenTypeValidator.Validate(
                (signature.ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")),
                policy);
            if (!type.Success)
            {
                return ValidationFailure(
                    type.Error,
                    TokenValidationDiagnosticStage.Claims,
                    TokenKind.JwsJson,
                    index);
            }

            keys[index] = (resolved.Key ?? throw new InvalidOperationException("A required value is null."));
            algorithms[index] = (signature.Algorithm ?? throw new InvalidOperationException("A required value is null."));
            keyIds[index] = (signature.KeyId ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        JwsGeneralResult verified = JsonWebSignatureGeneral.Verify(
            text,
            keys,
            policy.TokenPolicy);
        if (!verified.Success)
        {
            return ValidationFailure(
                verified.Error,
                TokenValidationDiagnosticStage.Authentication,
                TokenKind.JwsJson);
        }

        JwtClaimsResult claims = JwtClaims.Read(
            ((verified.Token ?? throw new InvalidOperationException("A required value is null.")).Payload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!claims.Success)
        {
            return ValidationFailure(
                claims.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JwsJson);
        }

        TokenResult claimsResult = ValidateJwtClaims(
            claims.Claims,
            null,
            policy,
            false);
        if (!claimsResult.Success)
        {
            return ValidationFailure(
                claimsResult.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JwsJson);
        }

        TokenValidationResult result = FromJwtClaims(
            TokenKind.JwsJson,
            claims.Claims,
            headers);
        result.Algorithm = algorithms.Length == 0 ? null : algorithms[0];
        result.Algorithms = algorithms;
        result.KeyId = keyIds.Length == 0 ? null : keyIds[0];
        result.KeyIds = keyIds;
        result.TokenType = algorithms.Length == 0
            ? null
            : HeaderString(
                verified.Token.Signatures[0].ProtectedHeader,
                JoseHeaderNames.Type);
        result.SignatureValidated = true;
        result.Success = true;
        return result;
    }

    static TokenValidationResult ValidateJsonEncryption(
        string text,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy,
        TokenHeaderInspection[] headers)
    {
        JsonObjectResult root = JsonObject.Read(
            text,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumJsonDepth);
        if ((root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("recipients") != null)
        {
            return ValidateGeneralEncryption(
                text,
                resolver,
                policy,
                headers);
        }

        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            text,
            policy.TokenPolicy);
        if (!parsed.Success)
        {
            return ValidationFailure(
                parsed.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JweJson);
        }

        TokenKeyResolutionContext context = EncryptionContext(
            (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            parsed.Token.Algorithm,
            parsed.Token.EncryptionAlgorithm,
            parsed.Token.KeyId,
            TokenKind.JweJson);
        TokenKeyResult resolved = TokenKeyResolution.Resolve(
            resolver,
            context);
        if (!resolved.Success)
        {
            return ValidationFailure(
                resolved.Error,
                TokenValidationDiagnosticStage.KeyResolution,
                TokenKind.JweJson);
        }

        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            text,
            (resolved.Key ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!decrypted.Success)
        {
            return ValidationFailure(
                decrypted.Error,
                TokenValidationDiagnosticStage.Authentication,
                TokenKind.JweJson);
        }

        return ValidatedJsonEncryption(
            (decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).Plaintext,
            decrypted.Token.ProtectedHeader,
            decrypted.Token.Algorithm,
            decrypted.Token.EncryptionAlgorithm,
            decrypted.Token.KeyId,
            policy,
            headers);
    }

    static TokenValidationResult ValidateGeneralEncryption(
        string text,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy,
        TokenHeaderInspection[] headers)
    {
        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(
            text,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return ValidationFailure(
                parsed.Error,
                TokenValidationDiagnosticStage.Parsing,
                TokenKind.JweJson);
        }

        List<TokenValidationDiagnostic> diagnostics =
            new List<TokenValidationDiagnostic>();
        TokenError? lastError = null;
        int index = 0;
        while (index < (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            JweFlattenedResult recipient = JsonWebEncryptionFlattened.Parse(
                (parsed.Token.Recipients[index].FlattenedJson ?? throw new InvalidOperationException("A required value is null.")),
                policy.TokenPolicy);
            if (!recipient.Success)
            {
                return ValidationFailure(
                    recipient.Error,
                    TokenValidationDiagnosticStage.Parsing,
                    TokenKind.JweJson,
                    index);
            }

            TokenKeyResolutionContext context = EncryptionContext(
                (recipient.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
                recipient.Token.Algorithm,
                recipient.Token.EncryptionAlgorithm,
                recipient.Token.KeyId,
                TokenKind.JweJson);
            TokenKeyResult resolved = TokenKeyResolution.Resolve(
                resolver,
                context);
            if (resolved.Success)
            {
                JweGeneralResult decrypted = JsonWebEncryptionGeneral.Decrypt(
                    text,
                    index,
                    (resolved.Key ?? throw new InvalidOperationException("A required value is null.")),
                    policy.TokenPolicy);
                if (decrypted.Success)
                {
                    return ValidatedJsonEncryption(
                        (decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).Plaintext,
                        decrypted.Token.ProtectedHeader,
                        recipient.Token.Algorithm,
                        decrypted.Token.EncryptionAlgorithm,
                        recipient.Token.KeyId,
                        policy,
                        headers);
                }

                lastError = decrypted.Error;
                diagnostics.Add(Diagnostic(
                    decrypted.Error,
                    TokenValidationDiagnosticStage.Authentication,
                    TokenKind.JweJson,
                    index));
            }
            else
            {
                lastError = resolved.Error;
                diagnostics.Add(Diagnostic(
                    resolved.Error,
                    TokenValidationDiagnosticStage.KeyResolution,
                    TokenKind.JweJson,
                    index));
            }

            index += 1;
        }

        return lastError == null
            ? ValidationFailure(
                "key_resolution_failed",
                "No JWE recipient could be resolved and decrypted.",
                TokenValidationDiagnosticStage.KeyResolution,
                TokenKind.JweJson)
            : ValidationFailure(lastError, diagnostics);
    }

    static TokenValidationResult ValidatedJsonEncryption(
        byte[]? plaintext,
        JoseHeader? protectedHeader,
        string? algorithm,
        string? encryptionAlgorithm,
        string? keyId,
        JwtValidationPolicy policy,
        TokenHeaderInspection[] headers)
    {
        JwtClaimsResult claims = JwtClaims.Read(
            (plaintext ?? throw new InvalidOperationException("A required value is null.")),
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!claims.Success)
        {
            return ValidationFailure(
                claims.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JweJson);
        }

        TokenResult claimsResult = ValidateJwtClaims(
            claims.Claims,
            protectedHeader,
            policy,
            true);
        if (!claimsResult.Success)
        {
            return ValidationFailure(
                claimsResult.Error,
                TokenValidationDiagnosticStage.Claims,
                TokenKind.JweJson);
        }

        TokenValidationResult result = FromJwtClaims(
            TokenKind.JweJson,
            claims.Claims,
            headers);
        result.KeyManagementAlgorithm = algorithm;
        result.EncryptionAlgorithm = encryptionAlgorithm;
        result.KeyId = keyId;
        result.KeyIds = ValueArray(keyId);
        result.TokenType = HeaderString(
            protectedHeader,
            JoseHeaderNames.Type);
        result.EncryptionValidated = true;
        result.Success = true;
        return result;
    }

    static TokenInspectionResult InspectCompact(
        string text,
        TokenKind kind,
        TokenPolicy policy)
    {
        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        if (kind == TokenKind.JwsCompact)
        {
            JwsCompactResult signature = JsonWebSignature.Parse(text, policy);
            if (!signature.Success)
            {
                return Failure(signature.Error);
            }

            AddJose(
                headers,
                (signature.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected);
            TokenInspectionResult result = Success(
                TokenKind.JwsCompact,
                headers);
            AddJwtClaims(
                result,
                signature.Token.ParsedPayload,
                false,
                policy);
            return result;
        }

        JweCompactResult encryption = JsonWebEncryption.Parse(text, policy);
        if (!encryption.Success)
        {
            return Failure(encryption.Error);
        }

        AddJose(
            headers,
            (encryption.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            TokenHeaderRole.Message,
            TokenHeaderProtection.Protected);
        TokenInspectionResult encryptedResult = Success(
            TokenKind.JweCompact,
            headers);
        SetEncryptedClaimsError(encryptedResult, policy);
        return encryptedResult;
    }

    static TokenInspectionResult InspectJson(
        string text,
        TokenPolicy policy)
    {
        JsonObjectResult root = JsonObject.Read(
            text,
            policy.MaximumJsonDepth);
        if (!root.Success)
        {
            if (root.Error != null
                && string.Equals(
                    root.Error.Code,
                    "json_depth_exceeded",
                    StringComparison.Ordinal))
            {
                return Failure(root.Error);
            }

            return Failure(
                "unknown_token",
                "The text is not a supported token representation.",
                -1);
        }

        if ((root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("signatures") != null)
        {
            return InspectGeneralSignature(text, policy);
        }

        if (root.Object.Get("signature") != null)
        {
            return InspectFlattenedSignature(text, policy);
        }

        if (root.Object.Get("recipients") != null)
        {
            return InspectGeneralEncryption(text, policy);
        }

        if (root.Object.Get("ciphertext") != null)
        {
            return InspectFlattenedEncryption(text, policy);
        }

        return Failure(
            "unknown_token",
            "The JSON object is not a supported JOSE serialization.",
            -1);
    }

    static TokenInspectionResult InspectFlattenedSignature(
        string text,
        TokenPolicy policy)
    {
        JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
            text,
            policy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        AddJose(
            headers,
            (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            TokenHeaderRole.Signature,
            TokenHeaderProtection.Protected);
        AddJose(
            headers,
            parsed.Token.UnprotectedHeader,
            TokenHeaderRole.Signature,
            TokenHeaderProtection.Unprotected);
        TokenInspectionResult result = Success(TokenKind.JwsJson, headers);
        AddJwtClaims(
            result,
            parsed.Token.ParsedPayload,
            parsed.Token.DetachedPayload,
            policy);
        return result;
    }

    static TokenInspectionResult InspectGeneralSignature(
        string text,
        TokenPolicy policy)
    {
        JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(text, policy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        int index = 0;
        while (index < (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Signatures.Length)
        {
            JwsGeneralSignature signature = parsed.Token.Signatures[index];
            AddJose(
                headers,
                signature.ProtectedHeader,
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Protected);
            AddJose(
                headers,
                signature.UnprotectedHeader,
                TokenHeaderRole.Signature,
                TokenHeaderProtection.Unprotected);
            index += 1;
        }

        TokenInspectionResult result = Success(TokenKind.JwsJson, headers);
        AddJwtClaims(
            result,
            parsed.Token.ParsedPayload,
            parsed.Token.DetachedPayload,
            policy);
        return result;
    }

    static TokenInspectionResult InspectFlattenedEncryption(
        string text,
        TokenPolicy policy)
    {
        JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
            text,
            policy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        AddJose(
            headers,
            (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            TokenHeaderRole.Message,
            TokenHeaderProtection.Protected);
        AddJose(
            headers,
            parsed.Token.SharedUnprotectedHeader,
            TokenHeaderRole.Message,
            TokenHeaderProtection.Unprotected);
        AddJose(
            headers,
            parsed.Token.RecipientHeader,
            TokenHeaderRole.Recipient,
            TokenHeaderProtection.Unprotected);
        TokenInspectionResult result = Success(TokenKind.JweJson, headers);
        SetEncryptedClaimsError(result, policy);
        return result;
    }

    static TokenInspectionResult InspectGeneralEncryption(
        string text,
        TokenPolicy policy)
    {
        JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(text, policy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        AddJose(
            headers,
            (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            TokenHeaderRole.Message,
            TokenHeaderProtection.Protected);
        AddJose(
            headers,
            parsed.Token.SharedUnprotectedHeader,
            TokenHeaderRole.Message,
            TokenHeaderProtection.Unprotected);
        int index = 0;
        while (index < parsed.Token.Recipients.Length)
        {
            AddJose(
                headers,
                parsed.Token.Recipients[index].Header,
                TokenHeaderRole.Recipient,
                TokenHeaderProtection.Unprotected);
            index += 1;
        }

        TokenInspectionResult result = Success(TokenKind.JweJson, headers);
        SetEncryptedClaimsError(result, policy);
        return result;
    }

    static TokenInspectionResult InspectSelectiveDisclosure(
        string text,
        TokenPolicy policy)
    {
        SelectiveDisclosureJwtValidationPolicy selectivePolicy =
            new SelectiveDisclosureJwtValidationPolicy();
        selectivePolicy.JwtPolicy = JwtValidationPolicy.Create(policy);
        if (selectivePolicy.MaximumDisclosureSizeInBytes
            > policy.MaximumTokenSizeInBytes)
        {
            selectivePolicy.MaximumDisclosureSizeInBytes =
                policy.MaximumTokenSizeInBytes;
        }

        SelectiveDisclosureJwtResult parsed = SelectiveDisclosureJwt.Parse(
            text,
            selectivePolicy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        JwsCompactResult issuer = JsonWebSignature.Parse(
            (parsed.IssuerSignedJwt ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!issuer.Success)
        {
            return Failure(issuer.Error);
        }

        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        AddJose(
            headers,
            (issuer.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            TokenHeaderRole.Signature,
            TokenHeaderProtection.Protected);
        if (parsed.KeyBindingJwt != null)
        {
            JwsCompactResult binding = JsonWebSignature.Parse(
                parsed.KeyBindingJwt,
                policy);
            if (!binding.Success)
            {
                return Failure(binding.Error);
            }

            AddJose(
                headers,
                (binding.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
                TokenHeaderRole.KeyBinding,
                TokenHeaderProtection.Protected);
        }

        TokenInspectionResult result = Success(TokenKind.SdJwt, headers);
        AddJwtClaims(
            result,
            issuer.Token.ParsedPayload,
            false,
            policy);
        return result;
    }

    static TokenInspectionResult InspectCose(
        byte[] bytes,
        TokenPolicy policy)
    {
        CborReader reader = CborReader.Create(
            bytes,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            if (IsTerminalBinaryFailure(decoded.Error))
            {
                return Failure(decoded.Error);
            }

            return Failure(
                "unknown_token",
                "The bytes are not a supported binary token representation.",
                -1);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "unknown_token",
                "The bytes are not a supported binary token representation.",
                -1);
        }

        TokenKind tokenKind = TokenKind.Cose;
        byte[] messageBytes = bytes;
        if ((decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Tag
            && decoded.Item.TagValue == CwtTag)
        {
            if (decoded.Item.TaggedValue == null)
            {
                return Failure(
                    "invalid_cwt",
                    "The CWT tag does not wrap a token.",
                    0);
            }

            CborWriter writer = CborWriter.Create(
                policy.MaximumTokenSizeInBytes,
                policy.MaximumCborDepth);
            TokenResult written = writer.WriteDeterministicItem(
                decoded.Item.TaggedValue);
            if (!written.Success)
            {
                return Failure(written.Error);
            }

            messageBytes = writer.ToArray();
            tokenKind = TokenKind.Cwt;
        }

        CoseMessageResult message = CoseMessage.Read(messageBytes, policy);
        if (!message.Success)
        {
            if (tokenKind == TokenKind.Cwt)
            {
                return Failure(
                    "invalid_cwt",
                    "The CWT tag must wrap a valid tagged COSE message.",
                    0);
            }

            return Failure(message.Error);
        }

        return InspectCoseMessage(
            messageBytes,
            message.Kind,
            tokenKind,
            policy);
    }

    static TokenInspectionResult InspectCoseMessage(
        byte[] bytes,
        CoseMessageKind messageKind,
        TokenKind tokenKind,
        TokenPolicy policy)
    {
        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        byte[]? claimsPayload = null;
        bool detached = false;
        bool encrypted = false;
        if (messageKind == CoseMessageKind.Sign1)
        {
            CoseSign1Result parsed = CoseSign1.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            AddCose(headers, (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header, TokenHeaderRole.Signature);
            claimsPayload = parsed.Message.Payload;
            detached = parsed.Message.DetachedPayload;
        }
        else if (messageKind == CoseMessageKind.Mac0)
        {
            CoseMac0Result parsed = CoseMac0.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            AddCose(headers, (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header, TokenHeaderRole.Signature);
            claimsPayload = parsed.Message.Payload;
            detached = parsed.Message.DetachedPayload;
        }
        else if (messageKind == CoseMessageKind.Encrypt0)
        {
            CoseEncrypt0Result parsed = CoseEncrypt0.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            AddCose(headers, (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Header, TokenHeaderRole.Message);
            encrypted = true;
        }
        else if (messageKind == CoseMessageKind.Sign)
        {
            CoseSignResult parsed = CoseSign.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            CoseSign message = parsed.Message
                ?? throw new InvalidOperationException("A required value is null.");
            AddCose(headers, message.Header, TokenHeaderRole.Message);
            int index = 0;
            while (index < message.Signatures.Length)
            {
                AddCose(
                    headers,
                    message.Signatures[index].Header,
                    TokenHeaderRole.Signature);
                index += 1;
            }

            claimsPayload = message.Payload;
            detached = message.DetachedPayload;
        }
        else if (messageKind == CoseMessageKind.Mac)
        {
            CoseMacResult parsed = CoseMac.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            CoseMac message = parsed.Message
                ?? throw new InvalidOperationException("A required value is null.");
            AddCose(headers, message.Header, TokenHeaderRole.Message);
            AddRecipients(headers, message.Recipients);
            claimsPayload = message.Payload;
            detached = message.DetachedPayload;
        }
        else
        {
            CoseEncryptResult parsed = CoseEncrypt.Read(bytes, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            CoseEncrypt message = parsed.Message
                ?? throw new InvalidOperationException("A required value is null.");
            AddCose(headers, message.Header, TokenHeaderRole.Message);
            AddRecipients(headers, message.Recipients);
            encrypted = true;
        }

        TokenInspectionResult result = Success(tokenKind, headers);
        if (encrypted)
        {
            SetEncryptedClaimsError(result, policy);
        }
        else
        {
            AddCwtClaims(result, claimsPayload, detached, policy, 0);
        }

        return result;
    }

    static void AddJwtClaims(
        TokenInspectionResult result,
        byte[]? payload,
        bool detached,
        TokenPolicy policy)
    {
        if (!policy.AllowUnauthenticatedDecode)
        {
            return;
        }

        if (detached || payload == null)
        {
            result.ClaimsError = new TokenError(
                "detached_payload",
                "Claims cannot be inspected without the detached payload.",
                -1);
            return;
        }

        JwtClaimsResult claims = JwtClaims.Read(payload, policy);
        if (!claims.Success)
        {
            result.ClaimsError = claims.Error;
            return;
        }

        result.JwtClaims = claims.Claims;
        result.ClaimsAvailable = true;
    }

    static void AddCwtClaims(
        TokenInspectionResult result,
        byte[]? payload,
        bool detached,
        TokenPolicy policy,
        int depth)
    {
        if (!policy.AllowUnauthenticatedDecode)
        {
            return;
        }

        if (detached || payload == null)
        {
            result.ClaimsError = new TokenError(
                "detached_payload",
                "Claims cannot be inspected without the detached payload.",
                -1);
            return;
        }

        CwtClaimsResult claims = CwtClaims.Read(payload, policy);
        if (claims.Success)
        {
            result.CwtClaims = claims.Claims;
            result.ClaimsAvailable = true;
            return;
        }

        if (depth >= policy.MaximumCborDepth)
        {
            result.ClaimsError = new TokenError(
                "cbor_depth_exceeded",
                "Nested claims inspection exceeds the configured CBOR depth limit.",
                depth);
            return;
        }

        byte[]? nestedBytes = CoseBytes(payload, policy);
        if (nestedBytes == null)
        {
            result.ClaimsError = claims.Error;
            return;
        }

        CoseMessageResult nested = CoseMessage.Read(nestedBytes, policy);
        if (!nested.Success)
        {
            result.ClaimsError = claims.Error;
            return;
        }

        AddNestedCoseClaims(
            result,
            nestedBytes,
            nested.Kind,
            policy,
            depth + 1);
    }

    static void AddNestedCoseClaims(
        TokenInspectionResult result,
        byte[] bytes,
        CoseMessageKind kind,
        TokenPolicy policy,
        int depth)
    {
        if (kind == CoseMessageKind.Encrypt0
            || kind == CoseMessageKind.Encrypt)
        {
            SetEncryptedClaimsError(result, policy);
            return;
        }

        byte[]? payload = null;
        bool detached = false;
        if (kind == CoseMessageKind.Sign1)
        {
            CoseSign1Result parsed = CoseSign1.Read(bytes, policy);
            if (!parsed.Success)
            {
                result.ClaimsError = parsed.Error;
                return;
            }

            payload = (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Payload;
            detached = parsed.Message.DetachedPayload;
        }
        else if (kind == CoseMessageKind.Mac0)
        {
            CoseMac0Result parsed = CoseMac0.Read(bytes, policy);
            if (!parsed.Success)
            {
                result.ClaimsError = parsed.Error;
                return;
            }

            payload = (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Payload;
            detached = parsed.Message.DetachedPayload;
        }
        else if (kind == CoseMessageKind.Sign)
        {
            CoseSignResult parsed = CoseSign.Read(bytes, policy);
            if (!parsed.Success)
            {
                result.ClaimsError = parsed.Error;
                return;
            }

            payload = (parsed.Message ?? throw new InvalidOperationException("A required value is null.")).Payload;
            detached = parsed.Message.DetachedPayload;
        }
        else
        {
            CoseMacResult parsed = CoseMac.Read(bytes, policy);
            if (!parsed.Success)
            {
                result.ClaimsError = parsed.Error;
                return;
            }

            CoseMac message = parsed.Message
                ?? throw new InvalidOperationException("A required value is null.");
            payload = message.Payload;
            detached = message.DetachedPayload;
        }

        AddCwtClaims(result, payload, detached, policy, depth);
    }

    static byte[]? CoseBytes(byte[] bytes, TokenPolicy policy)
    {
        CborReader reader = CborReader.Create(
            bytes,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success || reader.GetRemainingLength() != 0)
        {
            return null;
        }

        if ((decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Tag
            || decoded.Item.TagValue != CwtTag)
        {
            return bytes;
        }

        if (decoded.Item.TaggedValue == null)
        {
            return null;
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(
            decoded.Item.TaggedValue);
        return written.Success ? writer.ToArray() : null;
    }

    static void SetEncryptedClaimsError(
        TokenInspectionResult result,
        TokenPolicy policy)
    {
        if (!policy.AllowUnauthenticatedDecode)
        {
            return;
        }

        result.ClaimsError = new TokenError(
            "encrypted_payload",
            "Claims inspection is not available for encrypted payloads.",
            -1);
    }

    static void AddRecipients(
        List<TokenHeaderInspection> headers,
        CoseRecipient[] recipients)
    {
        int index = 0;
        while (index < recipients.Length)
        {
            AddCose(
                headers,
                recipients[index].Header,
                TokenHeaderRole.Recipient);
            AddRecipients(headers, recipients[index].Recipients);
            index += 1;
        }
    }

    static void AddJose(
        List<TokenHeaderInspection> headers,
        JoseHeader? header,
        TokenHeaderRole role,
        TokenHeaderProtection protection)
    {
        if (header == null)
        {
            return;
        }

        TokenHeaderInspection entry = new TokenHeaderInspection();
        entry.Role = role;
        entry.Protection = protection;
        entry.JoseHeader = header;
        headers.Add(entry);
    }

    static void AddCose(
        List<TokenHeaderInspection> headers,
        CoseHeader? header,
        TokenHeaderRole role)
    {
        TokenHeaderInspection entry = new TokenHeaderInspection();
        entry.Role = role;
        entry.Protection = TokenHeaderProtection.Combined;
        entry.CoseHeader = header;
        headers.Add(entry);
    }

    static TokenValidationResult FromCompactJwt(
        JwtValidationResult validated,
        TokenHeaderInspection[] headers)
    {
        TokenValidationResult result = FromJwtClaims(
            validated.Kind,
            validated.Claims,
            CompactHeaders(validated, headers));
        result.InnerKind = validated.InnerKind;
        result.Algorithm = validated.Algorithm;
        result.Algorithms = ValueArray(validated.Algorithm);
        result.KeyManagementAlgorithm = validated.KeyManagementAlgorithm;
        result.EncryptionAlgorithm = validated.EncryptionAlgorithm;
        result.KeyId = validated.SigningKeyId == null
            ? validated.EncryptionKeyId
            : validated.SigningKeyId;
        result.KeyIds = CompactKeyIds(
            validated.SigningKeyId,
            validated.EncryptionKeyId);
        result.TokenType = validated.TokenType;
        result.SignatureValidated = validated.SignatureValidated;
        result.EncryptionValidated = validated.EncryptionValidated;
        result.Nested = validated.Nested;
        result.Success = true;
        return result;
    }

    static TokenValidationResult FromJwtClaims(
        TokenKind kind,
        JwtClaims? claims,
        TokenHeaderInspection[] headers)
    {
        TokenValidationResult result = new TokenValidationResult();
        result.Kind = kind;
        result.JwtClaims = claims;
        result.Headers = headers;
        result.Issuer = ClaimString(claims, JwtClaimNames.Issuer);
        result.Subject = ClaimString(claims, JwtClaimNames.Subject);
        result.Audiences = ClaimAudiences(claims);
        SetTime(
            result,
            (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.ExpirationTime),
            JwtClaimNames.ExpirationTime);
        SetTime(
            result,
            claims.Get(JwtClaimNames.NotBefore),
            JwtClaimNames.NotBefore);
        SetTime(
            result,
            claims.Get(JwtClaimNames.IssuedAt),
            JwtClaimNames.IssuedAt);
        return result;
    }

    static TokenValidationResult FromCwt(
        TokenKind kind,
        CwtValidationResult validated,
        TokenHeaderInspection[] outerHeaders)
    {
        TokenValidationResult result = new TokenValidationResult();
        List<string> algorithms = new List<string>();
        int index = 0;
        while (index < validated.Headers.Length)
        {
            CoseHeader header = validated.Headers[index];
            CoseMessageKind protection = validated.ProtectionKinds[index];
            bool authentication = IsCoseAuthentication(protection);

            string? algorithm = CoseAlgorithm(header);
            if (authentication && algorithm != null)
            {
                algorithms.Add(algorithm);
            }
            else if (!authentication
                && result.EncryptionAlgorithm == null)
            {
                result.EncryptionAlgorithm = algorithm;
            }

            if (index == 0
                && protection == CoseMessageKind.Sign
                && algorithm == null)
            {
                AddCoseSignatureAlgorithms(
                    algorithms,
                    outerHeaders);
            }

            if (index == 0
                && protection == CoseMessageKind.Encrypt)
            {
                result.KeyManagementAlgorithm =
                    CoseRecipientAlgorithm(outerHeaders);
            }

            if (result.TokenType == null)
            {
                result.TokenType = CoseContentType(header);
            }

            if (authentication)
            {
                result.SignatureValidated = true;
            }
            else
            {
                result.EncryptionValidated = true;
            }

            index += 1;
        }

        result.Kind = kind;
        result.InnerKind = validated.Nested
            ? TokenKind.Cose
            : TokenKind.Unknown;
        result.CwtClaims = validated.Claims;
        result.Headers = CwtHeaders(validated, outerHeaders);
        result.Algorithms = algorithms.ToArray();
        result.Algorithm = result.Algorithms.Length == 0
            ? null
            : result.Algorithms[0];
        result.KeyIds = CoseKeyIds(result.Headers);
        result.KeyId = result.KeyIds.Length == 0
            ? null
            : result.KeyIds[0];
        result.Issuer = CwtClaimString(
            validated.Claims,
            CwtClaimLabels.Issuer);
        result.Subject = CwtClaimString(
            validated.Claims,
            CwtClaimLabels.Subject);
        result.Audiences = CwtAudiences(validated.Claims);
        SetCwtTime(
            result,
            (validated.Claims ?? throw new InvalidOperationException("A required value is null.")).Get(CwtClaimLabels.ExpirationTime),
            CwtClaimLabels.ExpirationTime);
        SetCwtTime(
            result,
            validated.Claims.Get(CwtClaimLabels.NotBefore),
            CwtClaimLabels.NotBefore);
        SetCwtTime(
            result,
            validated.Claims.Get(CwtClaimLabels.IssuedAt),
            CwtClaimLabels.IssuedAt);
        result.Nested = validated.Nested;
        result.Success = true;
        return result;
    }

    static TokenHeaderInspection[] CwtHeaders(
        CwtValidationResult validated,
        TokenHeaderInspection[] outerHeaders)
    {
        List<TokenHeaderInspection> headers =
            new List<TokenHeaderInspection>();
        int index = 0;
        while (index < outerHeaders.Length)
        {
            headers.Add(outerHeaders[index]);
            index += 1;
        }

        index = 1;
        while (index < validated.Headers.Length)
        {
            CoseMessageKind kind = validated.ProtectionKinds[index];
            TokenHeaderRole role = kind == CoseMessageKind.Sign1
                    || kind == CoseMessageKind.Mac0
                ? TokenHeaderRole.Signature
                : TokenHeaderRole.Message;
            AddCose(headers, validated.Headers[index], role);
            index += 1;
        }

        return headers.ToArray();
    }

    static void AddCoseSignatureAlgorithms(
        List<string> algorithms,
        TokenHeaderInspection[] headers)
    {
        int index = 0;
        while (index < headers.Length)
        {
            TokenHeaderInspection entry = headers[index];
            if (entry.Role == TokenHeaderRole.Signature
                && entry.CoseHeader != null)
            {
                string? algorithm = CoseAlgorithm(entry.CoseHeader);
                if (algorithm != null)
                {
                    algorithms.Add(algorithm);
                }
            }

            index += 1;
        }
    }

    static string? CoseRecipientAlgorithm(
        TokenHeaderInspection[] headers)
    {
        int index = 0;
        while (index < headers.Length)
        {
            TokenHeaderInspection entry = headers[index];
            if (entry.Role == TokenHeaderRole.Recipient
                && entry.CoseHeader != null)
            {
                string? algorithm = CoseAlgorithm(entry.CoseHeader);
                if (algorithm != null)
                {
                    return algorithm;
                }
            }

            index += 1;
        }

        return null;
    }

    static string[] CoseKeyIds(TokenHeaderInspection[] headers)
    {
        List<string> keyIds = new List<string>();
        int index = 0;
        while (index < headers.Length)
        {
            CoseHeader? header = headers[index].CoseHeader;
            if (header != null)
            {
                string? keyId = CoseKeyId(header);
                if (keyId != null)
                {
                    keyIds.Add(keyId);
                }
            }

            index += 1;
        }

        return keyIds.ToArray();
    }

    static bool IsCoseAuthentication(CoseMessageKind kind)
    {
        return kind == CoseMessageKind.Sign1
            || kind == CoseMessageKind.Sign
            || kind == CoseMessageKind.Mac0
            || kind == CoseMessageKind.Mac;
    }

    static string? CoseAlgorithm(CoseHeader header)
    {
        if (header.AlgorithmText != null)
        {
            return header.AlgorithmText;
        }

        if (!header.HasAlgorithm)
        {
            return null;
        }

        string? name = CoseAlgorithmsOperations.GetName(header.Algorithm);
        return name == null
            ? header.Algorithm.ToString(CultureInfo.InvariantCulture)
            : name;
    }

    static string? CoseKeyId(CoseHeader header)
    {
        if (header.KeyId == null)
        {
            return null;
        }

        TokenTextResult encoded = Base64Url.Encode(header.KeyId);
        return encoded.Success ? encoded.Text : null;
    }

    static string? CoseContentType(CoseHeader header)
    {
        if (header.ContentTypeText != null)
        {
            return header.ContentTypeText;
        }

        return header.HasContentTypeNumber
            ? header.ContentTypeNumber.ToString(
                CultureInfo.InvariantCulture)
            : null;
    }

    static string? CwtClaimString(CwtClaims? claims, int label)
    {
        CborItem? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(label);
        return value == null ? null : value.Text;
    }

    static string[] CwtAudiences(CwtClaims? claims)
    {
        string? audience = CwtClaimString(
            claims,
            CwtClaimLabels.Audience);
        return ValueArray(audience);
    }

    static void SetCwtTime(
        TokenValidationResult result,
        CborItem? value,
        int label)
    {
        if (value == null)
        {
            return;
        }

        decimal seconds;
        if (value.Kind == TokenCborValueKind.UnsignedInteger)
        {
            seconds = value.IntegerArgument;
        }
        else if (value.Kind == TokenCborValueKind.NegativeInteger)
        {
            seconds = -1m - value.IntegerArgument;
        }
        else
        {
            double represented = value.FloatingPointValue;
            if (value.FloatingPointWidth == 2)
            {
                represented = (double)(Half)value.FloatingPointValue;
            }
            else if (value.FloatingPointWidth == 4)
            {
                represented = (double)(float)value.FloatingPointValue;
            }

            seconds = (decimal)represented;
        }

        JwtNumericDateResult converted =
            JwtNumericDate.ToDateTimeOffset(seconds);
        if (!converted.Success)
        {
            return;
        }

        if (label == CwtClaimLabels.ExpirationTime)
        {
            result.ExpirationTime = converted.DateTime;
            result.HasExpirationTime = true;
        }
        else if (label == CwtClaimLabels.NotBefore)
        {
            result.NotBefore = converted.DateTime;
            result.HasNotBefore = true;
        }
        else
        {
            result.IssuedAt = converted.DateTime;
            result.HasIssuedAt = true;
        }
    }

    static TokenResult ValidateJwtClaims(
        JwtClaims? claims,
        JoseHeader? protectedHeader,
        JwtValidationPolicy policy,
        bool encrypted)
    {
        if (protectedHeader != null)
        {
            TokenResult type = JwtTokenTypeValidator.Validate(
                protectedHeader,
                policy);
            if (!type.Success)
            {
                return type;
            }

            if (encrypted)
            {
                TokenResult replication =
                    JwtHeaderClaimReplicationValidator.Validate(
                        protectedHeader,
                        (claims ?? throw new InvalidOperationException("A required value is null.")),
                        policy);
                if (!replication.Success)
                {
                    return replication;
                }
            }
        }

        TokenResult required = JwtRequiredClaimValidator.Validate(
            (claims ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!required.Success)
        {
            return required;
        }

        TokenResult issuer = JwtIssuerValidator.Validate(claims, policy);
        if (!issuer.Success)
        {
            return issuer;
        }

        TokenResult audience = JwtAudienceValidator.Validate(claims, policy);
        if (!audience.Success)
        {
            return audience;
        }

        TokenResult lifetime = JwtLifetimeValidator.Validate(claims, policy);
        if (!lifetime.Success)
        {
            return lifetime;
        }

        return JwtReplayValidator.Validate(claims, policy);
    }

    static TokenKeyResolutionContext SignatureContext(
        JoseHeader? header,
        string? algorithm,
        string? keyId,
        JwtClaims? claims,
        TokenKind kind)
    {
        TokenKeyResolutionContext context =
            new TokenKeyResolutionContext();
        context.Kind = kind;
        context.Algorithm = algorithm;
        context.KeyId = keyId;
        context.TokenType = HeaderString(header, JoseHeaderNames.Type);
        context.Issuer = ClaimString(claims, JwtClaimNames.Issuer);
        context.Audiences = ClaimAudiences(claims);
        context.Operation = TokenKeyOperations.Verify;
        context.ProtectedHeader = header;
        context.UnverifiedClaims = claims;
        return context;
    }

    static TokenKeyResolutionContext EncryptionContext(
        JoseHeader? header,
        string? algorithm,
        string? encryptionAlgorithm,
        string? keyId,
        TokenKind kind)
    {
        TokenKeyResolutionContext context =
            new TokenKeyResolutionContext();
        context.Kind = kind;
        context.Algorithm = algorithm;
        context.EncryptionAlgorithm = encryptionAlgorithm;
        context.KeyId = keyId;
        context.TokenType = HeaderString(header, JoseHeaderNames.Type);
        context.Operation = DecryptionOperation(algorithm);
        context.ProtectedHeader = header;
        return context;
    }

    static string DecryptionOperation(string? algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Direct,
            StringComparison.Ordinal))
        {
            return TokenKeyOperations.Decrypt;
        }

        if (string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEs,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes256KeyWrap,
                StringComparison.Ordinal))
        {
            return TokenKeyOperations.DeriveKey;
        }

        return TokenKeyOperations.UnwrapKey;
    }

    static string? HeaderString(JoseHeader? header, string name)
    {
        if (header == null)
        {
            return null;
        }

        JsonValue? value = header.Get(name);
        return value == null ? null : value.StringValue;
    }

    static string? ClaimString(JwtClaims? claims, string name)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(name);
        return value == null ? null : value.StringValue;
    }

    static string[] ClaimAudiences(JwtClaims? claims)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.Audience);
        if (value == null)
        {
            return Array.Empty<string>();
        }

        if (value.Kind == TokenJsonValueKind.String)
        {
            return new string[] { (value.StringValue ?? throw new InvalidOperationException("A required value is null.")) };
        }

        string[] audiences = new string[(value.ArrayValue ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < audiences.Length)
        {
            audiences[index] = (value.ArrayValue[index].StringValue ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return audiences;
    }

    static void SetTime(
        TokenValidationResult result,
        JsonValue? value,
        string name)
    {
        if (value == null)
        {
            return;
        }

        JwtNumericDateResult converted = JwtNumericDate.ToDateTimeOffset(
            value);
        if (!converted.Success)
        {
            return;
        }

        if (string.Equals(
            name,
            JwtClaimNames.ExpirationTime,
            StringComparison.Ordinal))
        {
            result.ExpirationTime = converted.DateTime;
            result.HasExpirationTime = true;
        }
        else if (string.Equals(
            name,
            JwtClaimNames.NotBefore,
            StringComparison.Ordinal))
        {
            result.NotBefore = converted.DateTime;
            result.HasNotBefore = true;
        }
        else
        {
            result.IssuedAt = converted.DateTime;
            result.HasIssuedAt = true;
        }
    }

    static string[] ValueArray(string? value)
    {
        return value == null
            ? Array.Empty<string>()
            : new string[] { value };
    }

    static string[] CompactKeyIds(string? signing, string? encryption)
    {
        if (signing == null)
        {
            return ValueArray(encryption);
        }

        if (encryption == null)
        {
            return ValueArray(signing);
        }

        return new string[] { signing, encryption };
    }

    static TokenHeaderInspection[] CompactHeaders(
        JwtValidationResult validated,
        TokenHeaderInspection[] headers)
    {
        if (validated.InnerProtectedHeader == null)
        {
            return headers;
        }

        TokenHeaderInspection[] result =
            new TokenHeaderInspection[headers.Length + 1];
        headers.AsSpan().CopyTo(result);
        TokenHeaderInspection inner = new TokenHeaderInspection();
        inner.Role = TokenHeaderRole.Signature;
        inner.Protection = TokenHeaderProtection.Protected;
        inner.JoseHeader = validated.InnerProtectedHeader;
        result[result.Length - 1] = inner;
        return result;
    }

    static TokenValidationResult ValidationFailure(
        string code,
        string message,
        TokenValidationDiagnosticStage stage,
        TokenKind kind)
    {
        return ValidationFailure(
            new TokenError(code, message, -1),
            stage,
            kind);
    }

    static TokenValidationResult ValidationFailure(
        string code,
        string message,
        TokenValidationDiagnosticStage stage,
        TokenKind kind,
        int participantIndex)
    {
        return ValidationFailure(
            new TokenError(code, message, -1),
            stage,
            kind,
            participantIndex);
    }

    static TokenValidationResult ValidationFailure(
        TokenError? error,
        TokenValidationDiagnosticStage stage,
        TokenKind kind)
    {
        return ValidationFailure(error, stage, kind, -1);
    }

    static TokenValidationResult ValidationFailure(
        TokenError? error,
        TokenValidationDiagnosticStage stage,
        TokenKind kind,
        int participantIndex)
    {
        TokenValidationResult result = new TokenValidationResult();
        result.Error = error;
        result.Diagnostics = new TokenValidationDiagnostic[]
        {
            Diagnostic(error, stage, kind, participantIndex)
        };
        return result;
    }

    static TokenValidationResult ValidationFailure(
        TokenError error,
        List<TokenValidationDiagnostic> diagnostics)
    {
        TokenValidationResult result = new TokenValidationResult();
        result.Error = error;
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    static TokenValidationDiagnostic Diagnostic(
        TokenError? error,
        TokenValidationDiagnosticStage stage,
        TokenKind kind,
        int participantIndex)
    {
        TokenValidationDiagnostic result =
            new TokenValidationDiagnostic();
        result.Stage = stage;
        result.Kind = kind;
        result.ParticipantIndex = participantIndex;
        result.Error = error;
        return result;
    }

    static TokenValidationDiagnosticStage DiagnosticStage(
        TokenError? error,
        TokenValidationDiagnosticStage fallback)
    {
        if (error != null
            && string.Equals(
                error.Code,
                "invalid_policy",
                StringComparison.Ordinal))
        {
            return TokenValidationDiagnosticStage.Policy;
        }

        return fallback;
    }

    static bool IsTerminalBinaryFailure(TokenError? error)
    {
        if (error == null)
        {
            return false;
        }

        return string.Equals(
                error.Code,
                "invalid_cwt",
                StringComparison.Ordinal)
            || string.Equals(
                error.Code,
                "cbor_depth_exceeded",
                StringComparison.Ordinal)
            || string.Equals(
                error.Code,
                "cbor_size_exceeded",
                StringComparison.Ordinal);
    }

    static TokenInspectionResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumJsonDepth <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null
            || policy.UnderstoodCriticalHeaders == null)
        {
            return Failure(
                "invalid_policy",
                "The token inspection policy contains an invalid limit or header setting.",
                -1);
        }

        TokenInspectionResult result = new TokenInspectionResult();
        result.Success = true;
        return result;
    }

    static TokenInspectionResult Success(
        TokenKind kind,
        List<TokenHeaderInspection> headers)
    {
        TokenInspectionResult result = new TokenInspectionResult();
        result.Success = true;
        result.Kind = kind;
        result.Headers = headers.ToArray();
        return result;
    }

    static TokenInspectionResult Failure(TokenError? error)
    {
        TokenInspectionResult result = new TokenInspectionResult();
        result.Error = error;
        return result;
    }

    static TokenInspectionResult Failure(
        string code,
        string message,
        int position)
    {
        return Failure(new TokenError(code, message, position));
    }
}
