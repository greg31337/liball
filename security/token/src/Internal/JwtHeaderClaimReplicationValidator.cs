using System;
namespace token;

/// <summary>
/// Validates replicated encrypted-JWT protected-header claims against payload claims.
/// </summary>
internal static class JwtHeaderClaimReplicationValidator
{
    /// <summary>Validates replicated <c>iss</c>, <c>sub</c>, and <c>aud</c> values.</summary>
    /// <param name="header">The authenticated JWE protected header.</param>
    /// <param name="claims">The authenticated JWT claims.</param>
    /// <param name="policy">The header-claim replication configuration.</param>
    /// <returns>A successful result when validation is disabled or all replicas match, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        JoseHeader header,
        JwtClaims claims,
        JwtValidationPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

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

        if (!policy.ValidateHeaderClaimReplication)
        {
            return Success();
        }

        TokenResult issuer = ValidateClaim(
            header,
            claims,
            JoseHeaderNames.Issuer,
            JwtClaimNames.Issuer);
        if (!issuer.Success)
        {
            return issuer;
        }

        TokenResult subject = ValidateClaim(
            header,
            claims,
            JoseHeaderNames.Subject,
            JwtClaimNames.Subject);
        if (!subject.Success)
        {
            return subject;
        }

        return ValidateClaim(
            header,
            claims,
            JoseHeaderNames.Audience,
            JwtClaimNames.Audience);
    }

    static TokenResult ValidateClaim(
        JoseHeader header,
        JwtClaims claims,
        string headerName,
        string claimName)
    {
        JsonValue? replica = header.Get(headerName);
        if (replica == null)
        {
            return Success();
        }

        JsonValue? claim = claims.Get(claimName);
        if (claim == null)
        {
            return Failure(
                "missing_replicated_claim",
                "The JWT protected header replicates a claim missing from the payload: "
                    + claimName + ".");
        }

        TokenBytesResult replicaBytes = CanonicalBytes(replica);
        TokenBytesResult claimBytes = CanonicalBytes(claim);
        if (!replicaBytes.Success || !claimBytes.Success)
        {
            return Failure(
                "invalid_replicated_claim",
                "A replicated JWT header or payload claim is invalid: "
                    + claimName + ".");
        }

        if (!ConstantTime.AreEqual((replicaBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")), (claimBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))))
        {
            return Failure(
                "replicated_claim_mismatch",
                "A replicated JWT protected-header claim does not match the payload: "
                    + claimName + ".");
        }

        return Success();
    }

    static TokenBytesResult CanonicalBytes(JsonValue value)
    {
        if (value == null)
        {
            return BytesFailure();
        }

        if (value.Kind == TokenJsonValueKind.String)
        {
            if (value.StringValue == null)
            {
                return BytesFailure();
            }

            TokenTextResult written = JsonWriter.WriteStringValue(
                value.StringValue);
            if (!written.Success)
            {
                return BytesFailure();
            }

            return Utf8Codec.Encode((written.Text ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (value.Kind != TokenJsonValueKind.Array
            || value.ArrayValue == null)
        {
            return BytesFailure();
        }

        string text = "[";
        int index = 0;
        while (index < value.ArrayValue.Length)
        {
            JsonValue item = value.ArrayValue[index];
            if (item == null
                || item.Kind != TokenJsonValueKind.String
                || item.StringValue == null)
            {
                return BytesFailure();
            }

            TokenTextResult written = JsonWriter.WriteStringValue(
                item.StringValue);
            if (!written.Success)
            {
                return BytesFailure();
            }

            if (index > 0)
            {
                text += ",";
            }

            text += written.Text;
            index += 1;
        }

        text += "]";
        return Utf8Codec.Encode(text);
    }

    static TokenBytesResult BytesFailure()
    {
        return new TokenBytesResult();
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
