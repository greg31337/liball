using System;
namespace token;

/// <summary>
/// Validates authenticated JWT lifetime claims using a policy-provided clock.
/// </summary>
internal static class JwtLifetimeValidator
{
    /// <summary>Validates expiration, not-before, issued-at, and maximum-age constraints.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The lifetime validation configuration.</param>
    /// <returns>A successful result when lifetime validation passes, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        JwtClaims claims,
        JwtValidationPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = policy.Validate();
        if (!policyResult.Success)
        {
            return policyResult;
        }

        JsonValue? expirationValue = claims.Get(JwtClaimNames.ExpirationTime);
        JsonValue? notBeforeValue = claims.Get(JwtClaimNames.NotBefore);
        JsonValue? issuedAtValue = claims.Get(JwtClaimNames.IssuedAt);

        if (policy.RequireExpirationTime && expirationValue == null)
        {
            return Failure(
                "missing_expiration_time",
                "JWT lifetime validation requires the exp claim.");
        }

        JwtNumericDateResult? expiration = null;
        if (policy.ValidateExpirationTime && expirationValue != null)
        {
            expiration = JwtNumericDate.ToDateTimeOffset(expirationValue);
            if (!expiration.Success)
            {
                return Failure(
                    "invalid_expiration_time",
                    "The JWT exp claim must be a supported NumericDate.");
            }
        }

        JwtNumericDateResult? notBefore = null;
        if (policy.ValidateNotBefore && notBeforeValue != null)
        {
            notBefore = JwtNumericDate.ToDateTimeOffset(notBeforeValue);
            if (!notBefore.Success)
            {
                return Failure(
                    "invalid_not_before",
                    "The JWT nbf claim must be a supported NumericDate.");
            }
        }

        JwtNumericDateResult? issuedAt = null;
        if (policy.ValidateIssuedAt && issuedAtValue != null)
        {
            issuedAt = JwtNumericDate.ToDateTimeOffset(issuedAtValue);
            if (!issuedAt.Success)
            {
                return Failure(
                    "invalid_issued_at",
                    "The JWT iat claim must be a supported NumericDate.");
            }
        }

        if (policy.MaximumTokenAgeSeconds > 0 && issuedAt == null)
        {
            return Failure(
                "missing_issued_at",
                "Maximum token age validation requires the iat claim.");
        }

        if (expiration == null && notBefore == null && issuedAt == null)
        {
            return Success();
        }

        decimal current = JwtNumericDate.FromClock((policy.Clock ?? throw new InvalidOperationException("A required value is null."))).Seconds;
        decimal skew = (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).ClockSkewSeconds;

        if (expiration != null && current >= expiration.Seconds + skew)
        {
            return Failure(
                "token_expired",
                "The JWT has expired.");
        }

        if (notBefore != null && current + skew < notBefore.Seconds)
        {
            return Failure(
                "token_not_yet_valid",
                "The JWT cannot be used before its nbf time.");
        }

        if (issuedAt != null && current + skew < issuedAt.Seconds)
        {
            return Failure(
                "issued_at_in_future",
                "The JWT iat claim is later than the allowed current time.");
        }

        if (policy.MaximumTokenAgeSeconds > 0
            && current > (issuedAt ?? throw new InvalidOperationException("A required value is null.")).Seconds
                + policy.MaximumTokenAgeSeconds
                + skew)
        {
            return Failure(
                "token_too_old",
                "The JWT exceeds the maximum token age allowed by policy.");
        }

        return Success();
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
}
