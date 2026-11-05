using System;
namespace token;

/// <summary>
/// Authenticates and validates RFC 8417 Security Event Tokens.
/// </summary>
internal static class SecurityEventTokenValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.Issuer,
        JwtClaimNames.IssuedAt,
        JwtClaimNames.JwtId,
        JwtClaimNames.SecurityEvents
    };

    /// <summary>Validates a signed, encrypted, or nested Security Event Token.</summary>
    /// <param name="token">The compact Security Event Token.</param>
    /// <param name="resolver">The resolver for issuer verification and recipient decryption keys.</param>
    /// <param name="policy">The issuer, audience, event profile, and JWT validation context.</param>
    /// <returns>Authenticated SET claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string token,
        ITokenKeyResolver resolver,
        SecurityEventTokenValidationPolicy policy)
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

        JwtValidationResult validated = JwtValidator.Validate(
            token,
            resolver,
            ProfilePolicy(policy));
        if (!validated.Success)
        {
            return validated;
        }

        if (policy.RequireSignature && !validated.SignatureValidated)
        {
            return Failure(
                "security_event_signature_required",
                "The Security Event Token policy requires a JWS signature.");
        }

        JoseHeader? typeHeader = validated.Nested
            ? validated.InnerProtectedHeader
            : validated.ProtectedHeader;
        TokenResult type = ValidateType(
            typeHeader,
            policy.RequireExplicitType);
        if (!type.Success)
        {
            return Failure(type.Error);
        }

        TokenResult claims = ValidateSetClaims(validated.Claims, policy);
        if (!claims.Success)
        {
            return Failure(claims.Error);
        }

        TokenResult replay = ValidateReplay(validated.Claims, policy.JwtPolicy);
        if (!replay.Success)
        {
            return Failure(replay.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(
        SecurityEventTokenValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = true;
        result.ValidateAudience = policy.RequireAudience;
        result.ValidateIssuedAt = true;
        result.ValidateTokenType = false;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = MergeRequiredClaims(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims,
            policy.RequireAudience);
        result.ReplayValidator = null;
        return result;
    }

    static string[] MergeRequiredClaims(
        string[]? configured,
        bool requireAudience)
    {
        if (!requireAudience)
        {
            return JwtAuthorizationProfileHelpers.MergeClaims(
                configured,
                requiredClaims);
        }

        string[] withAudience = new string[requiredClaims.Length + 1];
        requiredClaims.AsSpan().CopyTo(withAudience);
        withAudience[withAudience.Length - 1] = JwtClaimNames.Audience;
        return JwtAuthorizationProfileHelpers.MergeClaims(
            configured,
            withAudience);
    }

    static TokenResult ValidateType(JoseHeader? header, bool required)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Type);
        if (value == null)
        {
            if (required)
            {
                return TokenFailure(
                    "missing_security_event_type",
                    "The Security Event Token requires an explicit protected typ parameter.");
            }

            return TokenSuccess();
        }

        if (value.Kind != TokenJsonValueKind.String
            || !string.Equals(
                JoseMediaTypesOperations.Normalize((value.StringValue ?? throw new InvalidOperationException("A required value is null."))),
                JoseMediaTypes.SecurityEventJwt,
                StringComparison.Ordinal))
        {
            return TokenFailure(
                "invalid_security_event_type",
                "The Security Event Token protected typ must identify secevent+jwt.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateSetClaims(
        JwtClaims? claims,
        SecurityEventTokenValidationPolicy policy)
    {
        JsonValue? identifier = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.JwtId);
        if (!JwtAuthorizationProfileHelpers.ValidString(identifier))
        {
            return TokenFailure(
                "invalid_security_event_jwt_id",
                "The Security Event Token jti claim must be a nonempty string.");
        }

        JsonValue? transaction = claims.Get(JwtClaimNames.TransactionId);
        if (transaction != null
            && !JwtAuthorizationProfileHelpers.ValidString(transaction))
        {
            return TokenFailure(
                "invalid_security_event_transaction_id",
                "The Security Event Token txn claim must be a nonempty string.");
        }

        JsonValue? timeOfEvent = claims.Get(JwtClaimNames.TimeOfEvent);
        if (timeOfEvent != null)
        {
            JwtNumericDateResult converted = JwtNumericDate.ToDateTimeOffset(
                timeOfEvent);
            if (!converted.Success)
            {
                return TokenFailure(
                    "invalid_security_event_time",
                    "The Security Event Token toe claim must be a supported NumericDate.");
            }
        }

        return ValidateEvents(
            claims.Get(JwtClaimNames.SecurityEvents),
            policy);
    }

    static TokenResult ValidateEvents(
        JsonValue? events,
        SecurityEventTokenValidationPolicy policy)
    {
        if ((events ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenJsonValueKind.Object
            || events.ObjectValue == null
            || events.ObjectValue.Members == null
            || events.ObjectValue.Members.Length == 0)
        {
            return TokenFailure(
                "invalid_security_events",
                "The SET events claim must be a nonempty JSON object.");
        }

        int index = 0;
        while (index < events.ObjectValue.Members.Length)
        {
            JsonMember member = events.ObjectValue.Members[index];
            if (member == null
                || !ValidEventType(member.Name)
                || member.Value == null
                || member.Value.Kind != TokenJsonValueKind.Object
                || member.Value.ObjectValue == null)
            {
                return TokenFailure(
                    "invalid_security_event",
                    "Every SET event must use a URI identifier and JSON object payload.");
            }

            if (policy.ValidEventTypes.Length > 0
                && !Contains(policy.ValidEventTypes, member.Name))
            {
                return TokenFailure(
                    "unsupported_security_event",
                    "The SET contains an event type not accepted by policy.");
            }

            if (policy.EventValidator != null)
            {
                TokenResult validation = policy.EventValidator(
                    (member.Name ?? throw new InvalidOperationException("A required value is null.")),
                    member.Value.ObjectValue);
                if (validation == null)
                {
                    return TokenFailure(
                        "invalid_security_event_validation_result",
                        "The SET event validator returned no result.");
                }

                if (validation.Success && validation.Error != null)
                {
                    return TokenFailure(
                        "invalid_security_event_validation_result",
                        "The SET event validator returned contradictory success details.");
                }

                if (!validation.Success && validation.Error == null)
                {
                    return TokenFailure(
                        "security_event_rejected",
                        "The SET event validator rejected an event.");
                }

                if (!validation.Success)
                {
                    return validation;
                }
            }

            index += 1;
        }

        return TokenSuccess();
    }

    static TokenResult ValidateReplay(
        JwtClaims? claims,
        JwtValidationPolicy policy)
    {
        if (policy.ReplayValidator == null)
        {
            return TokenSuccess();
        }

        DateTimeOffset expiration = DateTimeOffset.MaxValue;
        JsonValue? expirationValue = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.ExpirationTime);
        if (expirationValue != null)
        {
            JwtNumericDateResult converted = JwtNumericDate.ToDateTimeOffset(
                expirationValue);
            if (!converted.Success)
            {
                return TokenFailure(
                    "invalid_expiration_time",
                    "The Security Event Token exp claim must be a supported NumericDate.");
            }

            expiration = converted.DateTime;
        }

        string? identifier = (claims.Get(JwtClaimNames.JwtId) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        TokenResult replay = policy.ReplayValidator((identifier ?? throw new InvalidOperationException("A required value is null.")), expiration);
        if (replay == null)
        {
            return TokenFailure(
                "invalid_replay_validation_result",
                "The Security Event Token replay validator returned no result.");
        }

        if (replay.Success && replay.Error != null)
        {
            return TokenFailure(
                "invalid_replay_validation_result",
                "The Security Event Token replay validator returned contradictory success details.");
        }

        if (!replay.Success && replay.Error == null)
        {
            return TokenFailure(
                "security_event_replay_detected",
                "The Security Event Token replay validator rejected the token.");
        }

        return replay;
    }

    static bool ValidEventType(string? value)
    {
        return value != null
            && value.Length > 0
            && Utf8Codec.Encode(value).Success
            && Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }

    static bool Contains(string[] values, string? expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(
                values[index],
                expected,
                StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static TokenResult TokenSuccess()
    {
        return JwtAuthorizationProfileHelpers.Success();
    }

    static TokenResult TokenFailure(string code, string message)
    {
        return JwtAuthorizationProfileHelpers.Failure(code, message);
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
