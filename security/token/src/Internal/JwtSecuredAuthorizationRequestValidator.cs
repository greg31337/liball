using System;
namespace token;

/// <summary>
/// Authenticates and validates RFC 9101 JWT-secured authorization Request Objects.
/// </summary>
internal static class JwtSecuredAuthorizationRequestValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.ClientId,
        "response_type"
    };

    /// <summary>Validates a signed or signed-and-encrypted Request Object.</summary>
    /// <param name="requestObject">The compact Request Object JWT.</param>
    /// <param name="resolver">The resolver for client verification and server decryption keys.</param>
    /// <param name="policy">The client, authorization server, and JWT validation context.</param>
    /// <returns>Authenticated authorization request claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string requestObject,
        ITokenKeyResolver resolver,
        JwtSecuredAuthorizationRequestValidationPolicy policy)
    {
        if (requestObject == null)
        {
            throw new ArgumentNullException("requestObject");
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

        JwtValidationPolicy profile = ProfilePolicy(policy);
        JwtValidationResult validated = JwtValidator.Validate(
            requestObject,
            resolver,
            profile);
        if (!validated.Success)
        {
            return validated;
        }

        if (!validated.SignatureValidated)
        {
            return Failure(
                "authorization_request_signature_required",
                "A JWT-secured authorization request must be signed.");
        }

        JoseHeader? signingHeader = validated.Nested
            ? validated.InnerProtectedHeader
            : validated.ProtectedHeader;
        TokenResult type = ValidateType(signingHeader, policy.RequireExplicitType);
        if (!type.Success)
        {
            return Failure(type.Error);
        }

        TokenResult issuerAudience = ValidateOptionalIssuerAndAudience(
            validated.Claims,
            policy);
        if (!issuerAudience.Success)
        {
            return Failure(issuerAudience.Error);
        }

        TokenResult parameters = ValidateParameters(
            validated.Claims,
            policy.ClientId);
        if (!parameters.Success)
        {
            return Failure(parameters.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(
        JwtSecuredAuthorizationRequestValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = policy.RequireIssuerAndAudience;
        result.ValidIssuers = new string[] { (policy.ClientId ?? throw new InvalidOperationException("A required value is null.")) };
        result.ValidateAudience = policy.RequireIssuerAndAudience;
        result.ValidAudiences = new string[]
        {
            (policy.AuthorizationServerIssuer ?? throw new InvalidOperationException("A required value is null."))
        };
        result.ValidateTokenType = false;
        result.AllowSignedTokens = true;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = MergeRequiredClaims(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims,
            policy.RequireIssuerAndAudience);
        return result;
    }

    static TokenResult ValidateType(JoseHeader? header, bool required)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Type);
        if (value == null)
        {
            if (required)
            {
                return TokenFailure(
                    "missing_authorization_request_type",
                    "The Request Object requires an explicit protected typ parameter.");
            }

            return TokenSuccess();
        }

        if (value.Kind != TokenJsonValueKind.String
            || !string.Equals(
                JoseMediaTypesOperations.Normalize((value.StringValue ?? throw new InvalidOperationException("A required value is null."))),
                JoseMediaTypes.AuthorizationRequestJwt,
                StringComparison.Ordinal))
        {
            return TokenFailure(
                "invalid_authorization_request_type",
                "The Request Object protected typ parameter must identify oauth-authz-req+jwt.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateOptionalIssuerAndAudience(
        JwtClaims? claims,
        JwtSecuredAuthorizationRequestValidationPolicy policy)
    {
        if (policy.RequireIssuerAndAudience)
        {
            return TokenSuccess();
        }

        JsonValue? issuer = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.Issuer);
        if (issuer != null
            && (!ValidString(issuer)
                || !EqualText(issuer.StringValue, policy.ClientId)))
        {
            return TokenFailure(
                "authorization_request_issuer_mismatch",
                "The Request Object issuer does not match the expected client.");
        }

        JsonValue? audience = claims.Get(JwtClaimNames.Audience);
        if (audience != null
            && !MatchesAudience(
                audience,
                policy.AuthorizationServerIssuer))
        {
            return TokenFailure(
                "authorization_request_audience_mismatch",
                "The Request Object audience does not identify the authorization server.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateParameters(JwtClaims? claims, string? clientId)
    {
        JsonValue? objectClient = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.ClientId);
        if (!ValidString(objectClient))
        {
            return TokenFailure(
                "invalid_authorization_request_client_id",
                "The Request Object client_id parameter must be a nonempty string.");
        }

        if (!EqualText((objectClient ?? throw new InvalidOperationException("A required value is null.")).StringValue, clientId))
        {
            return TokenFailure(
                "authorization_request_client_id_mismatch",
                "The Request Object client_id does not match the outer authorization request.");
        }

        JsonValue? responseType = claims.Get("response_type");
        if (!ValidString(responseType))
        {
            return TokenFailure(
                "invalid_authorization_request_response_type",
                "The Request Object response_type parameter must be a nonempty string.");
        }

        if (claims.Get("request") != null
            || claims.Get("request_uri") != null)
        {
            return TokenFailure(
                "nested_authorization_request_parameter",
                "A Request Object must not contain request or request_uri parameters.");
        }

        return TokenSuccess();
    }

    static bool MatchesAudience(JsonValue value, string? expected)
    {
        if (value.Kind == TokenJsonValueKind.String)
        {
            return EqualText(value.StringValue, expected);
        }

        if (value.Kind != TokenJsonValueKind.Array
            || value.ArrayValue == null
            || value.ArrayValue.Length == 0)
        {
            return false;
        }

        bool matched = false;
        int index = 0;
        while (index < value.ArrayValue.Length)
        {
            JsonValue item = value.ArrayValue[index];
            if (!ValidString(item))
            {
                return false;
            }

            bool equal = EqualText(item.StringValue, expected);
            matched = matched || equal;
            index += 1;
        }

        return matched;
    }

    static string[] MergeRequiredClaims(
        string[]? configured,
        bool requireIssuerAndAudience)
    {
        int profileCount = requiredClaims.Length;
        if (requireIssuerAndAudience)
        {
            profileCount += 2;
        }

        string[] profile = new string[profileCount];
        int count = 0;
        profile[count] = JwtClaimNames.ClientId;
        count += 1;
        profile[count] = "response_type";
        count += 1;
        if (requireIssuerAndAudience)
        {
            profile[count] = JwtClaimNames.Issuer;
            count += 1;
            profile[count] = JwtClaimNames.Audience;
        }

        return JwtAuthorizationProfileHelpers.MergeClaims(
            configured,
            profile);
    }

    static bool ValidString(JsonValue? value)
    {
        return JwtAuthorizationProfileHelpers.ValidString(value);
    }

    static bool EqualText(string? left, string? right)
    {
        return JwtAuthorizationProfileHelpers.EqualText(left, right);
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
