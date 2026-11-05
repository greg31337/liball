using System;
namespace token;

/// <summary>
/// Validates an authenticated JWT protected-header type against a policy allowlist.
/// </summary>
internal static class JwtTokenTypeValidator
{
    /// <summary>Validates the protected <c>typ</c> value using registered media-type aliases.</summary>
    /// <param name="header">The authenticated JWT protected header.</param>
    /// <param name="policy">The token-type validation configuration.</param>
    /// <returns>A successful result when type validation is disabled or matches, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        JoseHeader header,
        JwtValidationPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
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

        if (!policy.ValidateTokenType)
        {
            return Success();
        }

        JsonValue? type = header.Get(JoseHeaderNames.Type);
        if (type == null)
        {
            return Failure(
                "missing_token_type",
                "JWT token-type validation requires the protected typ parameter.");
        }

        if (type.Kind != TokenJsonValueKind.String
            || type.StringValue == null
            || type.StringValue.Length == 0
            || !Utf8Codec.Encode(type.StringValue).Success)
        {
            return Failure(
                "invalid_token_type",
                "The JWT protected typ parameter must be a nonempty valid string.");
        }

        string? normalizedActual = JoseMediaTypesOperations.Normalize(type.StringValue);
        int index = 0;
        while (index < (policy.ValidTokenTypes ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            string expected = policy.ValidTokenTypes[index];
            if (!Utf8Codec.Encode(expected).Success)
            {
                return Failure(
                    "invalid_policy",
                    "A configured JWT token type contains invalid text.");
            }

            string? normalizedExpected = JoseMediaTypesOperations.Normalize(expected);
            bool registeredMatch = normalizedActual != null
                && normalizedExpected != null
                && string.Equals(
                    normalizedActual,
                    normalizedExpected,
                    StringComparison.Ordinal);
            if (registeredMatch
                || string.Equals(
                    type.StringValue,
                    expected,
                    StringComparison.Ordinal))
            {
                return Success();
            }

            index += 1;
        }

        return Failure(
            "token_type_mismatch",
            "The JWT protected typ value is not allowed by policy.");
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
