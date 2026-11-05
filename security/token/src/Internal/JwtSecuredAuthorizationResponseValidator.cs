using System;
namespace token;

/// <summary>
/// Authenticates and validates JWT-secured OAuth authorization responses.
/// </summary>
internal static class JwtSecuredAuthorizationResponseValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.Issuer,
        JwtClaimNames.Audience,
        JwtClaimNames.ExpirationTime
    };

    static readonly string[] successParameters = new string[]
    {
        "code",
        "access_token",
        "id_token"
    };

    static readonly string[] stringParameters = new string[]
    {
        "code",
        "state",
        "access_token",
        "token_type",
        "id_token",
        "scope",
        "error",
        "error_description",
        "error_uri"
    };

    /// <summary>Validates a signed or signed-and-encrypted JARM response.</summary>
    /// <param name="response">The compact authorization response JWT.</param>
    /// <param name="resolver">The resolver for server verification and client decryption keys.</param>
    /// <param name="policy">The expected issuer, client, state, and JWT validation context.</param>
    /// <returns>Authenticated authorization response claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string response,
        ITokenKeyResolver resolver,
        JwtSecuredAuthorizationResponseValidationPolicy policy)
    {
        if (response == null)
        {
            throw new ArgumentNullException("response");
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
            response,
            resolver,
            ProfilePolicy(policy));
        if (!validated.Success)
        {
            return validated;
        }

        if (!validated.SignatureValidated)
        {
            return Failure(
                "authorization_response_signature_required",
                "A JWT-secured authorization response must be signed.");
        }

        TokenResult lifetime = ValidateMaximumLifetime(
            validated.Claims,
            policy);
        if (!lifetime.Success)
        {
            return Failure(lifetime.Error);
        }

        TokenResult state = ValidateState(validated.Claims, policy.State);
        if (!state.Success)
        {
            return Failure(state.Error);
        }

        TokenResult parameters = ValidateParameters(validated.Claims);
        if (!parameters.Success)
        {
            return Failure(parameters.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(
        JwtSecuredAuthorizationResponseValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = true;
        result.ValidIssuers = new string[] { (policy.Issuer ?? throw new InvalidOperationException("A required value is null.")) };
        result.ValidateAudience = true;
        result.ValidAudiences = new string[] { (policy.ClientId ?? throw new InvalidOperationException("A required value is null.")) };
        result.ValidateExpirationTime = true;
        result.RequireExpirationTime = true;
        result.ValidateTokenType = false;
        result.AllowSignedTokens = true;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims =
            JwtAuthorizationProfileHelpers.MergeClaims(
                (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims,
                requiredClaims);
        return result;
    }

    static TokenResult ValidateMaximumLifetime(
        JwtClaims? claims,
        JwtSecuredAuthorizationResponseValidationPolicy policy)
    {
        JwtNumericDateResult expiration = JwtNumericDate.ToDateTimeOffset(
            ((claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.ExpirationTime) ?? throw new InvalidOperationException("A required value is null.")));
        if (!expiration.Success)
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_expiration_time",
                "The authorization response exp claim must be a supported NumericDate.");
        }

        decimal current = JwtNumericDate.FromClock(
            (policy.JwtPolicy.Clock ?? throw new InvalidOperationException("A required value is null."))).Seconds;
        decimal maximum = current
            + policy.MaximumResponseLifetimeSeconds
            + (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).ClockSkewSeconds;
        if (expiration.Seconds > maximum)
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "authorization_response_lifetime_too_long",
                "The authorization response expiration exceeds the configured lifetime.");
        }

        return JwtAuthorizationProfileHelpers.Success();
    }

    static TokenResult ValidateState(JwtClaims? claims, string? expected)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get("state");
        if (value != null
            && !JwtAuthorizationProfileHelpers.ValidString(value))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_authorization_response_state",
                "The authorization response state parameter must be a nonempty string.");
        }

        if (expected == null)
        {
            return JwtAuthorizationProfileHelpers.Success();
        }

        if (value == null)
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "missing_authorization_response_state",
                "The authorization response is missing the requested state.");
        }

        if (!JwtAuthorizationProfileHelpers.EqualText(
            value.StringValue,
            expected))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "authorization_response_state_mismatch",
                "The authorization response state does not match the request.");
        }

        return JwtAuthorizationProfileHelpers.Success();
    }

    static TokenResult ValidateParameters(JwtClaims? claims)
    {
        int index = 0;
        while (index < stringParameters.Length)
        {
            JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(stringParameters[index]);
            if (value != null
                && !JwtAuthorizationProfileHelpers.ValidString(value))
            {
                return JwtAuthorizationProfileHelpers.Failure(
                    "invalid_authorization_response_parameter",
                    "An authorization response string parameter has an invalid value: "
                        + stringParameters[index] + ".");
            }

            index += 1;
        }

        JsonValue? expiresIn = (claims ?? throw new InvalidOperationException("A required value is null.")).Get("expires_in");
        if (expiresIn != null
            && (expiresIn.Kind != TokenJsonValueKind.Number
                || expiresIn.NumberText == null))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "invalid_authorization_response_parameter",
                "The authorization response expires_in parameter must be a JSON number.");
        }

        JsonValue? error = claims.Get("error");
        if (error != null && HasSuccessParameter(claims))
        {
            return JwtAuthorizationProfileHelpers.Failure(
                "mixed_authorization_response",
                "An authorization response must not contain both success and error parameters.");
        }

        return JwtAuthorizationProfileHelpers.Success();
    }

    static bool HasSuccessParameter(JwtClaims claims)
    {
        int index = 0;
        while (index < successParameters.Length)
        {
            if (claims.Get(successParameters[index]) != null)
            {
                return true;
            }

            index += 1;
        }

        return false;
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
