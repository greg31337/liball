using System;
namespace token;

/// <summary>
/// Authenticates and validates OAuth client assertion JWTs according to RFC 7523.
/// </summary>
internal static class ClientAssertionJwtValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.Issuer,
        JwtClaimNames.Subject,
        JwtClaimNames.Audience,
        JwtClaimNames.ExpirationTime
    };

    /// <summary>Validates a signed or signed-and-encrypted client assertion JWT.</summary>
    /// <param name="assertion">The compact client assertion JWT.</param>
    /// <param name="resolver">The resolver for client verification and server decryption keys.</param>
    /// <param name="policy">The expected client, authorization server, and JWT validation context.</param>
    /// <returns>Authenticated client assertion claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string assertion,
        ITokenKeyResolver resolver,
        ClientAssertionJwtValidationPolicy policy)
    {
        if (assertion == null)
        {
            throw new ArgumentNullException("assertion");
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
            assertion,
            resolver,
            ProfilePolicy(policy));
        if (!validated.Success)
        {
            return validated;
        }

        if (!validated.SignatureValidated)
        {
            return Failure(
                "client_assertion_signature_required",
                "A client assertion JWT must be signed or MAC protected.");
        }

        TokenResult subject = ValidateSubject(
            validated.Claims,
            policy.ClientId);
        if (!subject.Success)
        {
            return Failure(subject.Error);
        }

        TokenResult identifier = ValidateJwtId(
            validated.Claims,
            policy.RequireJwtId);
        if (!identifier.Success)
        {
            return Failure(identifier.Error);
        }

        TokenResult lifetime = ValidateMaximumLifetime(
            validated.Claims,
            policy);
        if (!lifetime.Success)
        {
            return Failure(lifetime.Error);
        }

        TokenResult replay = JwtReplayValidator.Validate(
            (validated.Claims ?? throw new InvalidOperationException("A required value is null.")),
            policy.JwtPolicy);
        if (!replay.Success)
        {
            return Failure(replay.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(
        ClientAssertionJwtValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = true;
        result.ValidIssuers = new string[] { (policy.ClientId ?? throw new InvalidOperationException("A required value is null.")) };
        result.ValidateAudience = true;
        result.ValidAudiences = new string[]
        {
            (policy.AuthorizationServerAudience ?? throw new InvalidOperationException("A required value is null."))
        };
        result.ValidateExpirationTime = true;
        result.ValidateNotBefore = true;
        result.ValidateIssuedAt = true;
        result.RequireExpirationTime = true;
        result.AllowSignedTokens = true;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = RequiredClaims(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims,
            policy.RequireJwtId);
        result.ReplayValidator = null;
        return result;
    }

    static string[] RequiredClaims(string[]? configured, bool requireJwtId)
    {
        if (!requireJwtId)
        {
            return JwtAuthorizationProfileHelpers.MergeClaims(
                configured,
                requiredClaims);
        }

        string[] profile = new string[requiredClaims.Length + 1];
        int index = 0;
        while (index < requiredClaims.Length)
        {
            profile[index] = requiredClaims[index];
            index += 1;
        }

        profile[index] = JwtClaimNames.JwtId;
        return JwtAuthorizationProfileHelpers.MergeClaims(
            configured,
            profile);
    }

    static TokenResult ValidateSubject(JwtClaims? claims, string? clientId)
    {
        JsonValue? subject = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.Subject);
        if (!JwtAuthorizationProfileHelpers.ValidString(subject))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_client_assertion_subject",
                "The client assertion sub claim must be a nonempty string.");
        }

        if (!JwtAuthorizationProfileHelpers.EqualText(
            (subject ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            clientId))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "client_assertion_subject_mismatch",
                "The client assertion subject does not match the expected client.");
        }

        return JwtAuthorizationProfileHelpers.Success();
    }

    static TokenResult ValidateJwtId(JwtClaims? claims, bool required)
    {
        JsonValue? identifier = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.JwtId);
        if (identifier == null)
        {
            if (required)
            {
                return JwtAuthorizationProfileHelpers.Failure(
                    "missing_client_assertion_jwt_id",
                    "The client assertion requires the jti claim.");
            }

            return JwtAuthorizationProfileHelpers.Success();
        }

        if (!JwtAuthorizationProfileHelpers.ValidString(identifier))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_client_assertion_jwt_id",
                "The client assertion jti claim must be a nonempty string.");
        }

        return JwtAuthorizationProfileHelpers.Success();
    }

    static TokenResult ValidateMaximumLifetime(
        JwtClaims? claims,
        ClientAssertionJwtValidationPolicy policy)
    {
        JwtNumericDateResult expiration = JwtNumericDate.ToDateTimeOffset(
            ((claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.ExpirationTime) ?? throw new InvalidOperationException("A required value is null.")));
        if (!expiration.Success)
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_expiration_time",
                "The client assertion exp claim must be a supported NumericDate.");
        }

        decimal current = JwtNumericDate.FromClock(
            (policy.JwtPolicy.Clock ?? throw new InvalidOperationException("A required value is null."))).Seconds;
        decimal maximum = current
            + policy.MaximumAssertionLifetimeSeconds
            + (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).ClockSkewSeconds;
        if (expiration.Seconds > maximum)
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "client_assertion_lifetime_too_long",
                "The client assertion expiration exceeds the configured lifetime.");
        }

        return JwtAuthorizationProfileHelpers.Success();
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
