using System;
namespace token;

/// <summary>Invokes application-provided subject and claim validation callbacks.</summary>
internal static class JwtApplicationClaimValidator
{
    /// <summary>Validates authenticated claims with configured application callbacks.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The callback configuration.</param>
    /// <returns>A successful result when every configured callback accepts the claims.</returns>
    internal static TokenResult Validate(JwtClaims claims, JwtValidationPolicy policy)
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

        if (policy.SubjectValidator != null)
        {
            TokenResult subject = policy.SubjectValidator(claims);
            if (subject == null)
            {
                return Failure(
                    "invalid_subject_validation_result",
                    "The JWT subject validator returned no result.");
            }

            if (subject.Success && subject.Error != null)
            {
                return Failure(
                    "invalid_subject_validation_result",
                    "The JWT subject validator returned contradictory success details.");
            }

            if (!subject.Success && subject.Error == null)
            {
                return Failure(
                    "subject_rejected",
                    "The JWT subject validator rejected the token.");
            }

            if (!subject.Success)
            {
                return subject;
            }
        }

        if (policy.ClaimValidator == null)
        {
            return Success();
        }

        int index = 0;
        while (index < claims.Values.Members.Length)
        {
            JsonMember member = claims.Values.Members[index];
            TokenResult claim = policy.ClaimValidator(
                (member.Name ?? throw new InvalidOperationException("A required value is null.")),
                (member.Value ?? throw new InvalidOperationException("A required value is null.")));
            if (claim == null)
            {
                return Failure(
                    "invalid_claim_validation_result",
                    "The JWT claim validator returned no result.");
            }

            if (claim.Success && claim.Error != null)
            {
                return Failure(
                    "invalid_claim_validation_result",
                    "The JWT claim validator returned contradictory success details.");
            }

            if (!claim.Success && claim.Error == null)
            {
                return Failure(
                    "claim_rejected",
                    "The JWT claim validator rejected a claim.");
            }

            if (!claim.Success)
            {
                return claim;
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
