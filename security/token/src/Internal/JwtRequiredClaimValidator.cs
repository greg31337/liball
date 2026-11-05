using System;
namespace token;

/// <summary>
/// Validates that authenticated JWT claims include every policy-required name.
/// </summary>
internal static class JwtRequiredClaimValidator
{
    /// <summary>Validates required claim presence using exact, case-sensitive names.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The required-claim validation configuration.</param>
    /// <returns>A successful result when every required claim is present, or stable failure details.</returns>
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

        int index = 0;
        while (index < ((policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            string name = (policy.TokenPolicy.RequiredClaims ?? throw new InvalidOperationException("A required value is null."))[index];
            if (claims.Get(name) == null)
            {
                return Failure(
                    "missing_required_claim",
                    "The JWT is missing a claim required by policy: " + name + ".");
            }

            index += 1;
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
