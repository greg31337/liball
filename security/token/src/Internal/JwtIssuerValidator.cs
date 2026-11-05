using System;
namespace token;

/// <summary>
/// Validates an authenticated JWT issuer claim against an exact allowlist.
/// </summary>
internal static class JwtIssuerValidator
{
    /// <summary>Validates the issuer claim according to a JWT validation policy.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The issuer validation configuration.</param>
    /// <returns>A successful result when issuer validation passes, or stable failure details.</returns>
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

        if (!policy.ValidateIssuer)
        {
            return Success();
        }

        JsonValue? issuer = claims.Get(JwtClaimNames.Issuer);
        if (issuer == null)
        {
            return Failure(
                "missing_issuer",
                "JWT issuer validation requires the iss claim.");
        }

        if (issuer.Kind != TokenJsonValueKind.String
            || issuer.StringValue == null)
        {
            return Failure(
                "invalid_issuer",
                "The JWT iss claim must be a string.");
        }

        TokenBytesResult actual = Utf8Codec.Encode(issuer.StringValue);
        if (!actual.Success)
        {
            return Failure(
                "invalid_issuer",
                "The JWT iss claim contains invalid text.");
        }

        bool matched = false;
        int index = 0;
        while (index < (policy.ValidIssuers ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            TokenBytesResult expected = Utf8Codec.Encode(
                policy.ValidIssuers[index]);
            if (!expected.Success)
            {
                return Failure(
                    "invalid_policy",
                    "A configured JWT issuer contains invalid text.");
            }

            bool equal = ConstantTime.AreEqual(
                (actual.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                (expected.Bytes ?? throw new InvalidOperationException("A required value is null.")));
            matched = matched || equal;
            index += 1;
        }

        if (!matched)
        {
            return Failure(
                "issuer_mismatch",
                "The JWT issuer is not allowed by policy.");
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
