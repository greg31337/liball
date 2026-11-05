using System;
namespace token;

/// <summary>Validates registered RFC 8392 CWT claims and policy constraints.</summary>
internal static class CwtRegisteredClaimValidator
{
    const ulong MaximumPositiveSeconds = 253402300799UL;
    const ulong MaximumNegativeArgument = 62135596799UL;

    /// <summary>Validates registered claim types, required claims, values, and lifetimes.</summary>
    /// <param name="claims">The authenticated CWT Claims Set.</param>
    /// <param name="policy">The registered-claim validation policy.</param>
    /// <returns>A successful result when all requested checks pass.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        CwtClaims claims,
        CwtValidationPolicy policy)
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

        if (claims.Values == null)
        {
            return Failure(
                "invalid_cwt_claims",
                "A CWT Claims Set requires claim entries.");
        }

        TokenResult types = ValidateTypes(claims);
        if (!types.Success)
        {
            return types;
        }

        TokenResult required = ValidateRequired(claims, policy);
        if (!required.Success)
        {
            return required;
        }

        TokenResult issuer = ValidateIssuer(claims, policy);
        if (!issuer.Success)
        {
            return issuer;
        }

        TokenResult audience = ValidateAudience(claims, policy);
        if (!audience.Success)
        {
            return audience;
        }

        return ValidateLifetime(claims, policy);
    }

    static TokenResult ValidateTypes(CwtClaims claims)
    {
        CborItem? issuer = claims.Get(CwtClaimLabels.Issuer);
        if (issuer != null && !IsText(issuer))
        {
            return InvalidType("iss", "a text string");
        }

        CborItem? subject = claims.Get(CwtClaimLabels.Subject);
        if (subject != null && !IsText(subject))
        {
            return InvalidType("sub", "a text string");
        }

        CborItem? audience = claims.Get(CwtClaimLabels.Audience);
        if (audience != null && !IsText(audience))
        {
            return InvalidType("aud", "a text string");
        }

        TokenResult expiration = ValidateNumericType(
            claims.Get(CwtClaimLabels.ExpirationTime),
            "exp");
        if (!expiration.Success)
        {
            return expiration;
        }

        TokenResult notBefore = ValidateNumericType(
            claims.Get(CwtClaimLabels.NotBefore),
            "nbf");
        if (!notBefore.Success)
        {
            return notBefore;
        }

        TokenResult issuedAt = ValidateNumericType(
            claims.Get(CwtClaimLabels.IssuedAt),
            "iat");
        if (!issuedAt.Success)
        {
            return issuedAt;
        }

        CborItem? identifier = claims.Get(CwtClaimLabels.CwtId);
        if (identifier != null
            && (identifier.Kind != TokenCborValueKind.ByteString
                || identifier.Bytes == null))
        {
            return InvalidType("cti", "a byte string");
        }

        return Success();
    }

    static TokenResult ValidateNumericType(CborItem? value, string name)
    {
        if (value == null)
        {
            return Success();
        }

        if (value.Kind != TokenCborValueKind.UnsignedInteger
            && value.Kind != TokenCborValueKind.NegativeInteger
            && value.Kind != TokenCborValueKind.FloatingPoint)
        {
            return InvalidType(name, "an untagged integer or floating-point number");
        }

        if (value.Kind == TokenCborValueKind.FloatingPoint
            && value.FloatingPointWidth != 2
            && value.FloatingPointWidth != 4
            && value.FloatingPointWidth != 8)
        {
            return InvalidType(
                name,
                "an integer or a two-, four-, or eight-byte floating-point number");
        }

        CwtNumericDateResult converted = ReadNumericDate(value);
        if (!converted.Success)
        {
            return Failure(
                "invalid_" + NumericDateName(name),
                "The CWT " + name + " claim is outside the supported NumericDate range.");
        }

        return Success();
    }

    static TokenResult ValidateRequired(
        CwtClaims claims,
        CwtValidationPolicy policy)
    {
        int index = 0;
        while (index < (policy.RequiredClaims ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            int label = policy.RequiredClaims[index];
            if (claims.Get(label) == null)
            {
                return Failure(
                    "missing_required_claim",
                    "The CWT is missing an integer claim required by policy: "
                        + label.ToString() + ".");
            }

            index += 1;
        }

        index = 0;
        while (index < (policy.RequiredTextClaims ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            string label = policy.RequiredTextClaims[index];
            if (claims.GetByTextLabel(label) == null)
            {
                return Failure(
                    "missing_required_claim",
                    "The CWT is missing a text claim required by policy: "
                        + label + ".");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateIssuer(
        CwtClaims claims,
        CwtValidationPolicy policy)
    {
        if (!policy.ValidateIssuer)
        {
            return Success();
        }

        CborItem? issuer = claims.Get(CwtClaimLabels.Issuer);
        if (issuer == null)
        {
            return Failure(
                "missing_issuer",
                "CWT issuer validation requires the iss claim.");
        }

        return Matches(
            issuer.Text,
            policy.ValidIssuers,
            "issuer_mismatch",
            "The CWT issuer is not allowed by policy.");
    }

    static TokenResult ValidateAudience(
        CwtClaims claims,
        CwtValidationPolicy policy)
    {
        if (!policy.ValidateAudience)
        {
            return Success();
        }

        CborItem? audience = claims.Get(CwtClaimLabels.Audience);
        if (audience == null)
        {
            return Failure(
                "missing_audience",
                "CWT audience validation requires the aud claim.");
        }

        return Matches(
            audience.Text,
            policy.ValidAudiences,
            "audience_mismatch",
            "The CWT audience is not allowed by policy.");
    }

    static TokenResult Matches(
        string? actualText,
        string[]? expectedTexts,
        string mismatchCode,
        string mismatchMessage)
    {
        TokenBytesResult actual = Utf8Codec.Encode((actualText ?? throw new InvalidOperationException("A required value is null.")));
        if (!actual.Success)
        {
            return Failure(
                "invalid_claim_type",
                "A registered CWT text claim contains invalid text.");
        }

        bool matched = false;
        int index = 0;
        while (index < (expectedTexts ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            TokenBytesResult expected = Utf8Codec.Encode(
                expectedTexts[index]);
            if (!expected.Success)
            {
                return Failure(
                    "invalid_policy",
                    "A configured CWT value contains invalid text.");
            }

            bool equal = ConstantTime.AreEqual((actual.Bytes ?? throw new InvalidOperationException("A required value is null.")), (expected.Bytes ?? throw new InvalidOperationException("A required value is null.")));
            matched = matched || equal;
            index += 1;
        }

        return matched
            ? Success()
            : Failure(mismatchCode, mismatchMessage);
    }

    static TokenResult ValidateLifetime(
        CwtClaims claims,
        CwtValidationPolicy policy)
    {
        CborItem? expirationValue = claims.Get(
            CwtClaimLabels.ExpirationTime);
        CborItem? notBeforeValue = claims.Get(CwtClaimLabels.NotBefore);
        CborItem? issuedAtValue = claims.Get(CwtClaimLabels.IssuedAt);
        if (policy.RequireExpirationTime && expirationValue == null)
        {
            return Failure(
                "missing_expiration_time",
                "CWT lifetime validation requires the exp claim.");
        }

        CwtNumericDateResult? expiration = null;
        if (policy.ValidateExpirationTime && expirationValue != null)
        {
            expiration = ReadNumericDate(expirationValue);
        }

        CwtNumericDateResult? notBefore = null;
        if (policy.ValidateNotBefore && notBeforeValue != null)
        {
            notBefore = ReadNumericDate(notBeforeValue);
        }

        CwtNumericDateResult? issuedAt = null;
        if (policy.ValidateIssuedAt && issuedAtValue != null)
        {
            issuedAt = ReadNumericDate(issuedAtValue);
        }

        if (policy.MaximumTokenAgeSeconds > 0 && issuedAt == null)
        {
            return Failure(
                "missing_issued_at",
                "Maximum CWT age validation requires the iat claim.");
        }

        if (expiration == null && notBefore == null && issuedAt == null)
        {
            return Success();
        }

        decimal current = JwtNumericDate.FromClock((policy.Clock ?? throw new InvalidOperationException("A required value is null."))).Seconds;
        decimal skew = (policy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).ClockSkewSeconds;
        if (expiration != null
            && current >= expiration.Seconds + skew)
        {
            return Failure("token_expired", "The CWT has expired.");
        }

        if (notBefore != null
            && current + skew < notBefore.Seconds)
        {
            return Failure(
                "token_not_yet_valid",
                "The CWT cannot be used before its nbf time.");
        }

        if (issuedAt != null
            && current + skew < issuedAt.Seconds)
        {
            return Failure(
                "issued_at_in_future",
                "The CWT iat claim is later than the allowed current time.");
        }

        if (policy.MaximumTokenAgeSeconds > 0
            && current > (issuedAt ?? throw new InvalidOperationException("A required value is null.")).Seconds
                + policy.MaximumTokenAgeSeconds
                + skew)
        {
            return Failure(
                "token_too_old",
                "The CWT exceeds the maximum token age allowed by policy.");
        }

        return Success();
    }

    static CwtNumericDateResult ReadNumericDate(CborItem value)
    {
        decimal seconds;
        if (value.Kind == TokenCborValueKind.UnsignedInteger)
        {
            if (value.IntegerArgument > MaximumPositiveSeconds)
            {
                return NumericFailure();
            }

            seconds = value.IntegerArgument;
        }
        else if (value.Kind == TokenCborValueKind.NegativeInteger)
        {
            if (value.IntegerArgument > MaximumNegativeArgument)
            {
                return NumericFailure();
            }

            seconds = -1m - value.IntegerArgument;
        }
        else if (value.Kind == TokenCborValueKind.FloatingPoint)
        {
            double represented = RepresentedFloatingPointValue(value);
            if (!double.IsFinite(represented)
                || represented < -62135596800d
                || represented > 253402300799.9999999d)
            {
                return NumericFailure();
            }

            seconds = (decimal)represented;
        }
        else
        {
            return NumericFailure();
        }

        JwtNumericDateResult converted =
            JwtNumericDate.ToDateTimeOffset(seconds);
        if (!converted.Success)
        {
            return NumericFailure();
        }

        CwtNumericDateResult result = new CwtNumericDateResult();
        result.Success = true;
        result.Seconds = converted.Seconds;
        return result;
    }

    static double RepresentedFloatingPointValue(CborItem value)
    {
        if (value.FloatingPointWidth == 2)
        {
            return (double)(Half)value.FloatingPointValue;
        }

        if (value.FloatingPointWidth == 4)
        {
            return (double)(float)value.FloatingPointValue;
        }

        return value.FloatingPointValue;
    }

    static string NumericDateName(string name)
    {
        if (string.Equals(name, "exp", StringComparison.Ordinal))
        {
            return "expiration_time";
        }

        if (string.Equals(name, "nbf", StringComparison.Ordinal))
        {
            return "not_before";
        }

        return "issued_at";
    }

    static bool IsText(CborItem value)
    {
        if (value.Kind != TokenCborValueKind.TextString
            || value.Text == null)
        {
            return false;
        }

        return Utf8Codec.Encode(value.Text).Success;
    }

    static TokenResult InvalidType(string name, string expected)
    {
        return Failure(
            "invalid_claim_type",
            "The CWT " + name + " claim must be " + expected + ".");
    }

    static CwtNumericDateResult NumericFailure()
    {
        return new CwtNumericDateResult();
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
