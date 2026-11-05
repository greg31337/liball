using System;
namespace token;

/// <summary>
/// Validates authenticated JWT audience claims in string or array form.
/// </summary>
internal static class JwtAudienceValidator
{
    /// <summary>Validates the audience claim according to a JWT validation policy.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The audience validation configuration.</param>
    /// <returns>A successful result when any token audience is allowed, or stable failure details.</returns>
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

        if (!policy.ValidateAudience)
        {
            return Success();
        }

        JsonValue? audience = claims.Get(JwtClaimNames.Audience);
        if (audience == null)
        {
            return Failure(
                "missing_audience",
                "JWT audience validation requires the aud claim.");
        }

        if (audience.Kind == TokenJsonValueKind.String
            && audience.StringValue != null)
        {
            return ValidateValues(
                new string[] { audience.StringValue },
                policy.ValidAudiences);
        }

        if (audience.Kind != TokenJsonValueKind.Array
            || audience.ArrayValue == null
            || audience.ArrayValue.Length == 0)
        {
            return Failure(
                "invalid_audience",
                "The JWT aud claim must be a string or a nonempty string array.");
        }

        string[] values = new string[audience.ArrayValue.Length];
        int index = 0;
        while (index < values.Length)
        {
            JsonValue item = audience.ArrayValue[index];
            if (item == null
                || item.Kind != TokenJsonValueKind.String
                || item.StringValue == null)
            {
                return Failure(
                    "invalid_audience",
                    "Every JWT aud array item must be a string.");
            }

            values[index] = item.StringValue;
            index += 1;
        }

        return ValidateValues(values, policy.ValidAudiences);
    }

    static TokenResult ValidateValues(
        string[] actualValues,
        string[]? expectedValues)
    {
        bool matched = false;
        int actualIndex = 0;
        while (actualIndex < actualValues.Length)
        {
            TokenBytesResult actual = Utf8Codec.Encode(
                actualValues[actualIndex]);
            if (!actual.Success)
            {
                return Failure(
                    "invalid_audience",
                    "A JWT aud value contains invalid text.");
            }

            int expectedIndex = 0;
            while (expectedIndex < (expectedValues ?? throw new InvalidOperationException("A required value is null.")).Length)
            {
                TokenBytesResult expected = Utf8Codec.Encode(
                    expectedValues[expectedIndex]);
                if (!expected.Success)
                {
                    return Failure(
                        "invalid_policy",
                        "A configured JWT audience contains invalid text.");
                }

                bool equal = ConstantTime.AreEqual(
                    (actual.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                    (expected.Bytes ?? throw new InvalidOperationException("A required value is null.")));
                matched = matched || equal;
                expectedIndex += 1;
            }

            actualIndex += 1;
        }

        if (!matched)
        {
            return Failure(
                "audience_mismatch",
                "The JWT audience is not allowed by policy.");
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
