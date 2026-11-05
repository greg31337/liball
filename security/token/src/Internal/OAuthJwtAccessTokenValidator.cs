using System;
namespace token;

/// <summary>
/// Validates JWT access tokens according to the OAuth 2.0 JWT access-token profile.
/// </summary>
internal static class OAuthJwtAccessTokenValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.Issuer,
        JwtClaimNames.Subject,
        JwtClaimNames.Audience,
        JwtClaimNames.ExpirationTime,
        JwtClaimNames.IssuedAt,
        JwtClaimNames.JwtId,
        JwtClaimNames.ClientId
    };

    /// <summary>Authenticates and validates an RFC 9068 JWT access token.</summary>
    /// <param name="token">The compact JWT access token text.</param>
    /// <param name="resolver">The resolver that selects verification and decryption keys.</param>
    /// <param name="policy">The expected issuer, audience, algorithms, clock, and optional callbacks.</param>
    /// <returns>A result containing authenticated access-token claims or stable failure details.</returns>
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

        TokenResult policyResult = policy.Validate();
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        if ((policy.ValidIssuers ?? throw new InvalidOperationException("A required value is null.")).Length == 0
            || (policy.ValidAudiences ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure(
                "invalid_oauth_access_token_policy",
                "OAuth JWT access-token validation requires an expected issuer and audience.");
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
            return Failure(
                "oauth_access_token_signature_required",
                "An OAuth JWT access token must be signed.");
        }

        TokenResult replay = JwtReplayValidator.Validate(
            (validated.Claims ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!replay.Success)
        {
            return Failure(replay.Error);
        }

        return validated;
    }

    static JwtValidationPolicy ProfilePolicy(JwtValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.Copy();
        result.ValidateIssuer = true;
        result.ValidateAudience = true;
        result.ValidateExpirationTime = true;
        result.ValidateIssuedAt = true;
        result.RequireExpirationTime = true;
        result.ValidateTokenType = true;
        result.ValidTokenTypes = new string[]
        {
            JoseMediaTypes.AccessTokenJwt
        };
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = MergeRequiredClaims(
            (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims);
        result.ReplayValidator = null;
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

    static bool Contains(string[] values, int count, string expected)
    {
        int index = 0;
        while (index < count)
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
