using System;
namespace token;

/// <summary>
/// Authenticates compact JWTs and returns claims only after cryptographic validation.
/// </summary>
internal static class JwtValidator
{
    /// <summary>Validates a signed or encrypted compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects cryptographic keys.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
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

        JoseCompactScanResult scanned = JoseCompact.Scan(token);
        if (!scanned.Success)
        {
            return Failure(scanned.Error);
        }

        if ((scanned.Segments ?? throw new InvalidOperationException("A required value is null.")).SegmentCount == 3)
        {
            return ValidateSigned(token, resolver, policy);
        }

        return ValidateEncrypted(token, resolver, policy);
    }

    /// <summary>Validates an encrypted compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects decryption and nested verification keys.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult ValidateEncrypted(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
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

        if (!policy.AllowEncryptedTokens)
        {
            return Failure(
                "encrypted_tokens_not_allowed",
                "Encrypted JWT validation is disabled by policy.");
        }

        JweCompactResult parsed = JsonWebEncryption.Parse(
            token,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        TokenKeyResolutionContext context = CreateContext(parsed.Token);
        TokenKeyResult resolved = TokenKeyResolution.Resolve(
            resolver,
            context);
        if (!resolved.Success)
        {
            return Failure(resolved.Error);
        }

        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            token,
            (resolved.Key ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!decrypted.Success)
        {
            return Failure(decrypted.Error);
        }

        string? contentType = ReadHeaderString(
            (decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader,
            JoseHeaderNames.ContentType);
        if (IsJwtContentType(contentType))
        {
            return ValidateNested(decrypted.Token, resolver, policy);
        }

        JwtClaimsResult claims = JwtClaims.Read(
            (decrypted.Token.Plaintext ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!claims.Success)
        {
            return Failure(claims.Error);
        }

        TokenResult tokenTypeValidation = JwtTokenTypeValidator.Validate(
            (decrypted.Token.ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!tokenTypeValidation.Success)
        {
            return Failure(tokenTypeValidation.Error);
        }

        TokenResult replicationValidation =
            JwtHeaderClaimReplicationValidator.Validate(
                decrypted.Token.ProtectedHeader,
                (claims.Claims ?? throw new InvalidOperationException("A required value is null.")),
                policy);
        if (!replicationValidation.Success)
        {
            return Failure(replicationValidation.Error);
        }

        TokenResult requiredClaimValidation = JwtRequiredClaimValidator.Validate(
            claims.Claims,
            policy);
        if (!requiredClaimValidation.Success)
        {
            return Failure(requiredClaimValidation.Error);
        }

        TokenResult issuerValidation = JwtIssuerValidator.Validate(
            claims.Claims,
            policy);
        if (!issuerValidation.Success)
        {
            return Failure(issuerValidation.Error);
        }

        TokenResult audienceValidation = JwtAudienceValidator.Validate(
            claims.Claims,
            policy);
        if (!audienceValidation.Success)
        {
            return Failure(audienceValidation.Error);
        }

        TokenResult applicationClaimValidation =
            JwtApplicationClaimValidator.Validate(
                claims.Claims,
                policy);
        if (!applicationClaimValidation.Success)
        {
            return Failure(applicationClaimValidation.Error);
        }

        TokenResult lifetimeValidation = JwtLifetimeValidator.Validate(
            claims.Claims,
            policy);
        if (!lifetimeValidation.Success)
        {
            return Failure(lifetimeValidation.Error);
        }

        TokenResult replayValidation = JwtReplayValidator.Validate(
            claims.Claims,
            policy);
        if (!replayValidation.Success)
        {
            return Failure(replayValidation.Error);
        }

        JwtValidationResult result = new JwtValidationResult();
        result.Kind = TokenKind.JweCompact;
        result.Claims = claims.Claims;
        result.ProtectedHeader = decrypted.Token.ProtectedHeader;
        result.KeyManagementAlgorithm = decrypted.Token.Algorithm;
        result.EncryptionAlgorithm = decrypted.Token.EncryptionAlgorithm;
        result.TokenType = ReadHeaderString(
            decrypted.Token.ProtectedHeader,
            JoseHeaderNames.Type);
        result.EncryptionKeyId = decrypted.Token.KeyId;
        result.EncryptionValidated = true;
        result.Success = true;
        return result;
    }

    /// <summary>Validates a signed compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects a verification key.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult ValidateSigned(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
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

        if (!policy.AllowSignedTokens)
        {
            return Failure(
                "signed_tokens_not_allowed",
                "Signed JWT validation is disabled by policy.");
        }

        JwsCompactResult parsed = JsonWebSignature.Parse(
            token,
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        if ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured)
        {
            return Failure(
                "jwt_signature_required",
                "Signed JWT validation requires a cryptographic signature or MAC.");
        }

        if (!parsed.Token.PayloadBase64UrlEncoded)
        {
            return Failure(
                "invalid_jwt_payload_encoding",
                "A compact JWT claim set must use base64url payload encoding.");
        }

        JwtClaimsResult unverifiedClaims = JwtClaims.Read(
            (parsed.Token.ParsedPayload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!unverifiedClaims.Success)
        {
            return Failure(unverifiedClaims.Error);
        }

        TokenKeyResolutionContext context = CreateContext(
            parsed.Token,
            unverifiedClaims.Claims);
        TokenKeyResult resolved = TokenKeyResolution.Resolve(
            resolver,
            context);
        if (!resolved.Success)
        {
            return Failure(resolved.Error);
        }

        JwsCompactResult verified = JsonWebSignature.Verify(
            token,
            (resolved.Key ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        JwtClaimsResult authenticatedClaims = JwtClaims.Read(
            ((verified.Token ?? throw new InvalidOperationException("A required value is null.")).Payload ?? throw new InvalidOperationException("A required value is null.")),
            policy.TokenPolicy);
        if (!authenticatedClaims.Success)
        {
            return Failure(authenticatedClaims.Error);
        }

        TokenResult tokenTypeValidation = JwtTokenTypeValidator.Validate(
            (verified.Token.ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!tokenTypeValidation.Success)
        {
            return Failure(tokenTypeValidation.Error);
        }

        TokenResult requiredClaimValidation = JwtRequiredClaimValidator.Validate(
            (authenticatedClaims.Claims ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!requiredClaimValidation.Success)
        {
            return Failure(requiredClaimValidation.Error);
        }

        TokenResult issuerValidation = JwtIssuerValidator.Validate(
            authenticatedClaims.Claims,
            policy);
        if (!issuerValidation.Success)
        {
            return Failure(issuerValidation.Error);
        }

        TokenResult audienceValidation = JwtAudienceValidator.Validate(
            authenticatedClaims.Claims,
            policy);
        if (!audienceValidation.Success)
        {
            return Failure(audienceValidation.Error);
        }

        TokenResult applicationClaimValidation =
            JwtApplicationClaimValidator.Validate(
                authenticatedClaims.Claims,
                policy);
        if (!applicationClaimValidation.Success)
        {
            return Failure(applicationClaimValidation.Error);
        }

        TokenResult lifetimeValidation = JwtLifetimeValidator.Validate(
            authenticatedClaims.Claims,
            policy);
        if (!lifetimeValidation.Success)
        {
            return Failure(lifetimeValidation.Error);
        }

        TokenResult replayValidation = JwtReplayValidator.Validate(
            authenticatedClaims.Claims,
            policy);
        if (!replayValidation.Success)
        {
            return Failure(replayValidation.Error);
        }

        JwtValidationResult result = new JwtValidationResult();
        result.Kind = TokenKind.JwsCompact;
        result.Claims = authenticatedClaims.Claims;
        result.ProtectedHeader = verified.Token.ProtectedHeader;
        result.Algorithm = verified.Token.Algorithm;
        result.TokenType = ReadHeaderString(
            verified.Token.ProtectedHeader,
            JoseHeaderNames.Type);
        result.SigningKeyId = verified.Token.KeyId;
        result.SignatureValidated = true;
        result.Success = true;
        return result;
    }

    static TokenKeyResolutionContext CreateContext(
        JwsCompactToken token,
        JwtClaims? claims)
    {
        TokenKeyResolutionContext context = new TokenKeyResolutionContext();
        context.Kind = TokenKind.JwsCompact;
        context.Algorithm = token.Algorithm;
        context.KeyId = token.KeyId;
        context.TokenType = ReadHeaderString(
            token.ProtectedHeader,
            JoseHeaderNames.Type);
        context.Issuer = ReadClaimString(claims, JwtClaimNames.Issuer);
        context.Audiences = ReadAudiences(claims);
        context.Operation = TokenKeyOperations.Verify;
        context.ProtectedHeader = token.ProtectedHeader;
        context.UnverifiedClaims = claims;
        return context;
    }

    static TokenKeyResolutionContext CreateContext(JweCompactToken? token)
    {
        TokenKeyResolutionContext context = new TokenKeyResolutionContext();
        context.Kind = TokenKind.JweCompact;
        context.Algorithm = (token ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        context.EncryptionAlgorithm = token.EncryptionAlgorithm;
        context.KeyId = token.KeyId;
        context.TokenType = ReadHeaderString(
            token.ProtectedHeader,
            JoseHeaderNames.Type);
        context.Operation = DecryptionOperation(token.Algorithm);
        context.ProtectedHeader = token.ProtectedHeader;
        return context;
    }

    static JwtValidationResult ValidateNested(
        JweCompactToken outer,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
    {
        if (!policy.AllowNestedTokens || !policy.AllowSignedTokens)
        {
            return Failure(
                "nested_tokens_not_allowed",
                "Nested JWT validation is disabled by policy.");
        }

        TokenTextResult innerText = Utf8Codec.Decode((outer.Plaintext ?? throw new InvalidOperationException("A required value is null.")));
        if (!innerText.Success)
        {
            return Failure(
                "invalid_nested_token_encoding",
                "The nested JWT must be valid UTF-8 compact token text.");
        }

        JoseCompactScanResult shape = JoseCompact.Scan((innerText.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!shape.Success
            || (shape.Segments ?? throw new InvalidOperationException("A required value is null.")).SegmentCount != 3)
        {
            return Failure(
                "nested_content_type_mismatch",
                "The outer JWT content type requires an inner signed compact JWT.");
        }

        JwtValidationResult inner = ValidateSigned(
            innerText.Text,
            resolver,
            WithoutReplayValidation(policy));
        if (!inner.Success)
        {
            return inner;
        }

        TokenResult replicationValidation =
            JwtHeaderClaimReplicationValidator.Validate(
                (outer.ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")),
                (inner.Claims ?? throw new InvalidOperationException("A required value is null.")),
                policy);
        if (!replicationValidation.Success)
        {
            return Failure(replicationValidation.Error);
        }

        TokenResult replayValidation = JwtReplayValidator.Validate(
            inner.Claims,
            policy);
        if (!replayValidation.Success)
        {
            return Failure(replayValidation.Error);
        }

        JwtValidationResult result = new JwtValidationResult();
        result.Kind = TokenKind.JweCompact;
        result.InnerKind = TokenKind.JwsCompact;
        result.Claims = inner.Claims;
        result.ProtectedHeader = outer.ProtectedHeader;
        result.InnerProtectedHeader = inner.ProtectedHeader;
        result.Algorithm = inner.Algorithm;
        result.KeyManagementAlgorithm = outer.Algorithm;
        result.EncryptionAlgorithm = outer.EncryptionAlgorithm;
        result.TokenType = ReadHeaderString(
            outer.ProtectedHeader,
            JoseHeaderNames.Type);
        result.SigningKeyId = inner.SigningKeyId;
        result.EncryptionKeyId = outer.KeyId;
        result.SignatureValidated = true;
        result.EncryptionValidated = true;
        result.Nested = true;
        result.Success = true;
        return result;
    }

    static JwtValidationPolicy WithoutReplayValidation(
        JwtValidationPolicy policy)
    {
        if (policy.ReplayValidator == null)
        {
            return policy;
        }

        JwtValidationPolicy result = policy.Copy();
        result.ReplayValidator = null;
        return result;
    }

    static bool IsJwtContentType(string? contentType)
    {
        string? normalized = JoseMediaTypesOperations.Normalize(contentType);
        return string.Equals(
            normalized,
            JoseMediaTypes.Jwt,
            StringComparison.Ordinal)
            || normalized != null
                && normalized.EndsWith("+jwt", StringComparison.OrdinalIgnoreCase);
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

    static string? ReadHeaderString(JoseHeader? header, string name)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(name);
        return value == null ? null : value.StringValue;
    }

    static string? ReadClaimString(JwtClaims? claims, string name)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(name);
        return value == null ? null : value.StringValue;
    }

    static string[] ReadAudiences(JwtClaims? claims)
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

    static JwtValidationResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JwtValidationResult Failure(TokenError? error)
    {
        JwtValidationResult result = new JwtValidationResult();
        result.Error = error;
        return result;
    }
}
