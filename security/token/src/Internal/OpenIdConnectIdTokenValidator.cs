using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Authenticates and validates OpenID Connect ID Tokens.
/// </summary>
internal static class OpenIdConnectIdTokenValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.Issuer,
        JwtClaimNames.Subject,
        JwtClaimNames.Audience,
        JwtClaimNames.ExpirationTime,
        JwtClaimNames.IssuedAt
    };

    /// <summary>Authenticates and validates an OpenID Connect ID Token.</summary>
    /// <param name="token">The compact ID Token text.</param>
    /// <param name="resolver">The resolver that selects verification and decryption keys.</param>
    /// <param name="policy">The ID Token and authentication-request validation context.</param>
    /// <returns>A result containing authenticated ID Token claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string token,
        ITokenKeyResolver resolver,
        OpenIdConnectIdTokenValidationPolicy policy)
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
            return ValidationFailure(policyResult.Error);
        }

        JwtValidationPolicy profile = ProfilePolicy(policy);
        JwtValidationResult validated = JwtValidator.Validate(
            token,
            resolver,
            profile);
        if (!validated.Success)
        {
            return validated;
        }

        if (!validated.SignatureValidated)
        {
            return ValidationFailure(
                "openid_id_token_signature_required",
                "An OpenID Connect ID Token must be signed.");
        }

        TokenResult type = ValidateType(validated);
        if (!type.Success)
        {
            return ValidationFailure(type.Error);
        }

        TokenResult audience = ValidateAudience(
            validated.Claims,
            policy.ClientId,
            policy.JwtPolicy.ValidAudiences);
        if (!audience.Success)
        {
            return ValidationFailure(audience.Error);
        }

        TokenResult nonce = ValidateNonce(
            validated.Claims,
            policy.Nonce);
        if (!nonce.Success)
        {
            return ValidationFailure(nonce.Error);
        }

        TokenResult authenticationTime = ValidateAuthenticationTime(
            validated.Claims,
            policy);
        if (!authenticationTime.Success)
        {
            return ValidationFailure(authenticationTime.Error);
        }

        TokenResult accessTokenHash = ValidateHashClaim(
            (validated.Claims ?? throw new InvalidOperationException("A required value is null.")),
            JwtClaimNames.AccessTokenHash,
            policy.AccessToken,
            validated.Algorithm);
        if (!accessTokenHash.Success)
        {
            return ValidationFailure(accessTokenHash.Error);
        }

        TokenResult codeHash = ValidateHashClaim(
            validated.Claims,
            JwtClaimNames.CodeHash,
            policy.AuthorizationCode,
            validated.Algorithm);
        if (!codeHash.Success)
        {
            return ValidationFailure(codeHash.Error);
        }

        TokenResult replay = JwtReplayValidator.Validate(
            validated.Claims,
            policy.JwtPolicy);
        if (!replay.Success)
        {
            return ValidationFailure(replay.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(
        OpenIdConnectIdTokenValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = true;
        result.ValidateAudience = true;
        result.ValidAudiences = TrustedAudiences(
            policy.ClientId,
            policy.JwtPolicy.ValidAudiences);
        result.ValidateExpirationTime = true;
        result.ValidateIssuedAt = true;
        result.RequireExpirationTime = true;
        result.ValidateTokenType = false;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = MergeRequiredClaims(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims);
        result.ReplayValidator = null;
        return result;
    }

    static TokenResult ValidateType(JwtValidationResult token)
    {
        JoseHeader? header = token.Nested
            ? token.InnerProtectedHeader
            : token.ProtectedHeader;
        JsonValue? type = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Type);
        if (type == null)
        {
            return Success();
        }

        string? normalized = JoseMediaTypesOperations.Normalize((type.StringValue ?? throw new InvalidOperationException("A required value is null.")));
        if (string.Equals(
            normalized,
            JoseMediaTypes.Jwt,
            StringComparison.Ordinal)
            || string.Equals(
                type.StringValue,
                "id_token",
                StringComparison.Ordinal))
        {
            return Success();
        }

        return Failure(
            "openid_id_token_type_mismatch",
            "The JWT protected typ value does not identify an ID Token.");
    }

    static TokenResult ValidateAudience(
        JwtClaims? claims,
        string? clientId,
        string[]? trustedAudiences)
    {
        JsonValue? audience = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.Audience);
        string[]? values = AudienceValues(audience);
        if (values == null || values.Length == 0)
        {
            return Failure(
                "invalid_openid_id_token_audience",
                "The OpenID Connect ID Token aud claim is invalid.");
        }

        bool clientPresent = false;
        int index = 0;
        while (index < values.Length)
        {
            bool currentClient = EqualText(values[index], clientId);
            clientPresent = clientPresent || currentClient;
            if (!MatchesAnyAudience(
                values[index],
                clientId,
                trustedAudiences))
            {
                return Failure(
                    "untrusted_openid_id_token_audience",
                    "The OpenID Connect ID Token contains an untrusted audience.");
            }

            index += 1;
        }

        if (!clientPresent)
        {
            return Failure(
                "openid_id_token_audience_mismatch",
                "The OpenID Connect ID Token audience does not include the client.");
        }

        JsonValue? authorizedParty = claims.Get(JwtClaimNames.AuthorizedParty);
        if (values.Length > 1 && authorizedParty == null)
        {
            return Failure(
                "missing_authorized_party",
                "A multi-audience OpenID Connect ID Token requires the azp claim.");
        }

        if (authorizedParty != null
            && !EqualText(authorizedParty.StringValue, clientId))
        {
            return Failure(
                "authorized_party_mismatch",
                "The OpenID Connect ID Token azp claim does not match the client.");
        }

        return Success();
    }

    static string[]? AudienceValues(JsonValue? value)
    {
        if ((value ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenJsonValueKind.String)
        {
            return new string[] { (value.StringValue ?? throw new InvalidOperationException("A required value is null.")) };
        }

        if (value.Kind != TokenJsonValueKind.Array
            || value.ArrayValue == null)
        {
            return null;
        }

        string[] result = new string[value.ArrayValue.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (value.ArrayValue[index].StringValue ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return result;
    }

    static bool MatchesAnyAudience(
        string value,
        string? clientId,
        string[]? trustedAudiences)
    {
        bool matched = EqualText(value, clientId);
        int index = 0;
        while (index < (trustedAudiences ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            bool current = EqualText(value, trustedAudiences[index]);
            matched = matched || current;
            index += 1;
        }

        return matched;
    }

    static TokenResult ValidateNonce(JwtClaims? claims, string? expected)
    {
        if (expected == null)
        {
            return Success();
        }

        JsonValue? nonce = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.Nonce);
        if (nonce == null)
        {
            return Failure(
                "missing_nonce",
                "The OpenID Connect ID Token is missing the requested nonce.");
        }

        if (!EqualText(nonce.StringValue, expected))
        {
            return Failure(
                "nonce_mismatch",
                "The OpenID Connect ID Token nonce does not match the request.");
        }

        return Success();
    }

    static TokenResult ValidateAuthenticationTime(
        JwtClaims? claims,
        OpenIdConnectIdTokenValidationPolicy policy)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.AuthenticationTime);
        if (value == null)
        {
            if (policy.MaximumAuthenticationAgeSeconds > 0)
            {
                return Failure(
                    "missing_authentication_time",
                    "The OpenID Connect ID Token is missing the required auth_time claim.");
            }

            return Success();
        }

        JwtNumericDateResult authenticationTime =
            JwtNumericDate.ToDateTimeOffset(value);
        if (!authenticationTime.Success)
        {
            return Failure(
                "invalid_authentication_time",
                "The OpenID Connect ID Token auth_time claim is invalid.");
        }

        if (policy.MaximumAuthenticationAgeSeconds == 0)
        {
            return Success();
        }

        decimal current = JwtNumericDate.FromClock(
            (policy.JwtPolicy.Clock ?? throw new InvalidOperationException("A required value is null."))).Seconds;
        decimal skew = (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).ClockSkewSeconds;
        if (current + skew < authenticationTime.Seconds)
        {
            return Failure(
                "authentication_time_in_future",
                "The OpenID Connect ID Token auth_time is in the future.");
        }

        if (current > authenticationTime.Seconds
            + policy.MaximumAuthenticationAgeSeconds
            + skew)
        {
            return Failure(
                "authentication_too_old",
                "The OpenID Connect authentication exceeds the maximum age.");
        }

        return Success();
    }

    static TokenResult ValidateHashClaim(
        JwtClaims claims,
        string claimName,
        string? artifact,
        string? algorithm)
    {
        if (artifact == null)
        {
            return Success();
        }

        JsonValue? claim = claims.Get(claimName);
        if (claim == null)
        {
            return Failure(
                "missing_openid_token_hash",
                "The OpenID Connect ID Token is missing the " + claimName + " claim.");
        }

        TokenTextResult calculated = CalculateHash(artifact, algorithm);
        if (!calculated.Success)
        {
            return TokenFailure(calculated.Error);
        }

        if (!EqualText(claim.StringValue, calculated.Text))
        {
            return Failure(
                "openid_token_hash_mismatch",
                "The OpenID Connect ID Token " + claimName + " claim does not match.");
        }

        return Success();
    }

    static TokenTextResult CalculateHash(string value, string? algorithm)
    {
        byte[] bytes = AsciiBytes(value);
        byte[]? digest = null;
        if (algorithm != null
            && algorithm.EndsWith("256", StringComparison.Ordinal))
        {
            digest = SHA256.HashData(bytes);
        }
        else if (algorithm != null
            && algorithm.EndsWith("384", StringComparison.Ordinal))
        {
            digest = SHA384.HashData(bytes);
        }
        else if (algorithm != null
            && algorithm.EndsWith("512", StringComparison.Ordinal))
        {
            digest = SHA512.HashData(bytes);
        }

        if (digest == null)
        {
            TokenTextResult failure = new TokenTextResult();
            failure.Error = new TokenError(
                "unsupported_openid_token_hash_algorithm",
                "The ID Token signing algorithm cannot calculate token hashes.",
                -1);
            return failure;
        }

        byte[] left = new byte[digest.Length / 2];
        Array.Copy(digest, left, left.Length);
        return Base64Url.Encode(left);
    }

    static byte[] AsciiBytes(string value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = (byte)value[index];
            index += 1;
        }

        return result;
    }

    static bool EqualText(string? left, string? right)
    {
        TokenBytesResult leftBytes = Utf8Codec.Encode((left ?? throw new InvalidOperationException("A required value is null.")));
        TokenBytesResult rightBytes = Utf8Codec.Encode((right ?? throw new InvalidOperationException("A required value is null.")));
        return leftBytes.Success
            && rightBytes.Success
            && ConstantTime.AreEqual((leftBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")), (rightBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    static string[] TrustedAudiences(string? clientId, string[]? configured)
    {
        if (Contains(configured, (configured ?? throw new InvalidOperationException("A required value is null.")).Length, clientId))
        {
            return configured.AsSpan().ToArray();
        }

        string[] result = new string[configured.Length + 1];
        int index = 0;
        while (index < configured.Length)
        {
            result[index] = configured[index];
            index += 1;
        }

        result[configured.Length] = (clientId ?? throw new InvalidOperationException("A required value is null."));
        return result;
    }

    static string[] MergeRequiredClaims(string[]? configured)
    {
        string[] merged = new string[(configured ?? throw new InvalidOperationException("A required value is null.")).Length + requiredClaims.Length];
        int count = 0;
        int configuredIndex = 0;
        while (configuredIndex < configured.Length)
        {
            merged[count] = configured[configuredIndex];
            count += 1;
            configuredIndex += 1;
        }

        int requiredIndex = 0;
        while (requiredIndex < requiredClaims.Length)
        {
            string name = requiredClaims[requiredIndex];
            if (!Contains(merged, count, name))
            {
                merged[count] = name;
                count += 1;
            }

            requiredIndex += 1;
        }

        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            result[index] = merged[index];
            index += 1;
        }

        return result;
    }

    static bool Contains(string[]? values, int count, string? expected)
    {
        int index = 0;
        while (index < count)
        {
            if (string.Equals(
                (values ?? throw new InvalidOperationException("A required value is null."))[index],
                expected,
                StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenResult TokenFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static JwtValidationResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }

    static JwtValidationResult ValidationFailure(TokenError? error)
    {
        JwtValidationResult result = new JwtValidationResult();
        result.Error = error;
        return result;
    }
}
