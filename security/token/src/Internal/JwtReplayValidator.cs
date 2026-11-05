using System;
namespace token;

/// <summary>
/// Invokes application-provided replay detection for authenticated JWT identifiers.
/// </summary>
internal static class JwtReplayValidator
{
    /// <summary>Validates a JWT identifier and its expiration using the configured replay callback.</summary>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The replay validation configuration.</param>
    /// <returns>A successful result when replay validation is disabled or passes, or stable failure details.</returns>
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

        if (policy.ReplayValidator == null)
        {
            return Success();
        }

        JsonValue? identifier = claims.Get(JwtClaimNames.JwtId);
        if (identifier == null)
        {
            return Failure(
                "missing_jwt_id",
                "JWT replay validation requires the jti claim.");
        }

        if (identifier.Kind != TokenJsonValueKind.String
            || identifier.StringValue == null
            || identifier.StringValue.Length == 0
            || !Utf8Codec.Encode(identifier.StringValue).Success)
        {
            return Failure(
                "invalid_jwt_id",
                "The JWT jti claim must be a nonempty valid string.");
        }

        JsonValue? expirationValue = claims.Get(JwtClaimNames.ExpirationTime);
        if (expirationValue == null)
        {
            return Failure(
                "missing_expiration_time",
                "JWT replay validation requires the exp claim.");
        }

        JwtNumericDateResult expiration = JwtNumericDate.ToDateTimeOffset(
            expirationValue);
        if (!expiration.Success)
        {
            return Failure(
                "invalid_expiration_time",
                "The JWT exp claim must be a supported NumericDate.");
        }

        TokenResult replay = policy.ReplayValidator(
            identifier.StringValue,
            expiration.DateTime);
        if (replay == null)
        {
            return Failure(
                "invalid_replay_validation_result",
                "The JWT replay validator returned no result.");
        }

        if (replay.Success && replay.Error != null)
        {
            return Failure(
                "invalid_replay_validation_result",
                "The JWT replay validator returned contradictory success details.");
        }

        if (!replay.Success && replay.Error == null)
        {
            return Failure(
                "token_replay_detected",
                "The JWT replay validator rejected the token.");
        }

        return replay;
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
