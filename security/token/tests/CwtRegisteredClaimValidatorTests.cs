using token;

namespace token.Tests;

public class CwtRegisteredClaimValidatorTests
{
    [Test]
    public void ValidatesEveryRegisteredClaimAndConfiguredValues()
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        claims.Set(CwtClaimLabels.Audience, Text("api"));
        claims.Set(CwtClaimLabels.ExpirationTime, Unsigned(1100));
        claims.Set(CwtClaimLabels.NotBefore, Floating(900.5));
        claims.Set(CwtClaimLabels.IssuedAt, Unsigned(950));
        claims.Set(CwtClaimLabels.CwtId, Bytes(new byte[] { 1, 2 }));
        claims.SetByTextLabel("tenant", Text("one"));
        CwtValidationPolicy policy = Policy(1000);
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "other", "issuer" };
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api" };
        policy.RequiredClaims = new[]
        {
            CwtClaimLabels.Subject,
            CwtClaimLabels.CwtId
        };
        policy.RequiredTextClaims = new[] { "tenant" };

        TokenResult result = CwtRegisteredClaimValidator.Validate(
            claims,
            policy);

        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void RejectsInvalidRegisteredClaimTypes()
    {
        Assert.Multiple(() =>
        {
            AssertInvalidType(CwtClaimLabels.Issuer, Unsigned(1));
            AssertInvalidType(CwtClaimLabels.Subject, Boolean(true));
            AssertInvalidType(CwtClaimLabels.Audience,
                Array(Text("api")));
            AssertInvalidType(CwtClaimLabels.ExpirationTime, Text("1000"));
            AssertInvalidType(CwtClaimLabels.NotBefore, Tagged(1, Unsigned(1)));
            AssertInvalidType(CwtClaimLabels.IssuedAt, Boolean(false));
            AssertInvalidType(CwtClaimLabels.CwtId, Text("identifier"));
            AssertInvalidType(CwtClaimLabels.Issuer, Text("\uD800"));
            CborItem invalidFloat = Floating(1000);
            invalidFloat.FloatingPointWidth = 0;
            AssertInvalidType(CwtClaimLabels.ExpirationTime, invalidFloat);
        });
    }

    [Test]
    public void RejectsNonFiniteAndOutOfRangeNumericDates()
    {
        CwtClaims infinity = Claims(
            CwtClaimLabels.ExpirationTime,
            Floating(double.PositiveInfinity));
        CwtClaims tooLarge = Claims(
            CwtClaimLabels.NotBefore,
            Unsigned(253402300800UL));
        CborItem negative = new CborItem();
        negative.Kind = TokenCborValueKind.NegativeInteger;
        negative.IntegerArgument = 62135596800UL;
        CwtClaims tooNegative = Claims(
            CwtClaimLabels.IssuedAt,
            negative);

        TokenResult infinityResult = CwtRegisteredClaimValidator.Validate(
            infinity,
            Policy(1000));
        TokenResult largeResult = CwtRegisteredClaimValidator.Validate(
            tooLarge,
            Policy(1000));
        TokenResult negativeResult = CwtRegisteredClaimValidator.Validate(
            tooNegative,
            Policy(1000));

        Assert.Multiple(() =>
        {
            AssertFailure(infinityResult, "invalid_expiration_time");
            AssertFailure(largeResult, "invalid_not_before");
            AssertFailure(negativeResult, "invalid_issued_at");
        });
    }

    [Test]
    public void IssuerAndAudienceMustBePresentAndMatchExactly()
    {
        CwtValidationPolicy issuerPolicy = Policy(1000);
        issuerPolicy.ValidateIssuer = true;
        issuerPolicy.ValidIssuers = new[] { "issuer" };
        CwtValidationPolicy audiencePolicy = Policy(1000);
        audiencePolicy.ValidateAudience = true;
        audiencePolicy.ValidAudiences = new[] { "api" };

        TokenResult missingIssuer = CwtRegisteredClaimValidator.Validate(
            new CwtClaims(),
            issuerPolicy);
        TokenResult wrongIssuer = CwtRegisteredClaimValidator.Validate(
            Claims(CwtClaimLabels.Issuer, Text("Issuer")),
            issuerPolicy);
        TokenResult missingAudience = CwtRegisteredClaimValidator.Validate(
            new CwtClaims(),
            audiencePolicy);
        TokenResult wrongAudience = CwtRegisteredClaimValidator.Validate(
            Claims(CwtClaimLabels.Audience, Text("API")),
            audiencePolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(missingIssuer, "missing_issuer");
            AssertFailure(wrongIssuer, "issuer_mismatch");
            AssertFailure(missingAudience, "missing_audience");
            AssertFailure(wrongAudience, "audience_mismatch");
        });
    }

    [Test]
    public void LifetimeValidationEnforcesExpirationNotBeforeAndIssuedAt()
    {
        CwtValidationPolicy noSkew = Policy(1000);
        CwtValidationPolicy skew = Policy(1000);
        skew.TokenPolicy.ClockSkewSeconds = 5;

        TokenResult expired = ValidateTime(
            CwtClaimLabels.ExpirationTime,
            Unsigned(1000),
            noSkew);
        TokenResult expirationSkew = ValidateTime(
            CwtClaimLabels.ExpirationTime,
            Unsigned(996),
            skew);
        TokenResult tooEarly = ValidateTime(
            CwtClaimLabels.NotBefore,
            Unsigned(1001),
            noSkew);
        TokenResult notBeforeSkew = ValidateTime(
            CwtClaimLabels.NotBefore,
            Unsigned(1005),
            skew);
        TokenResult futureIssuedAt = ValidateTime(
            CwtClaimLabels.IssuedAt,
            Floating(1000.5),
            noSkew);
        TokenResult issuedAtSkew = ValidateTime(
            CwtClaimLabels.IssuedAt,
            Unsigned(1005),
            skew);

        Assert.Multiple(() =>
        {
            AssertFailure(expired, "token_expired");
            Assert.That(expirationSkew.Success, Is.True);
            AssertFailure(tooEarly, "token_not_yet_valid");
            Assert.That(notBeforeSkew.Success, Is.True);
            AssertFailure(futureIssuedAt, "issued_at_in_future");
            Assert.That(issuedAtSkew.Success, Is.True);
        });
    }

    [Test]
    public void RequiredExpirationAndMaximumAgeAreEnforced()
    {
        CwtValidationPolicy expirationPolicy = Policy(1000);
        expirationPolicy.RequireExpirationTime = true;
        CwtValidationPolicy agePolicy = Policy(1000);
        agePolicy.MaximumTokenAgeSeconds = 100;
        CwtValidationPolicy skewPolicy = Policy(1000);
        skewPolicy.MaximumTokenAgeSeconds = 100;
        skewPolicy.TokenPolicy.ClockSkewSeconds = 5;

        TokenResult missingExpiration =
            CwtRegisteredClaimValidator.Validate(
                new CwtClaims(),
                expirationPolicy);
        TokenResult missingIssuedAt =
            CwtRegisteredClaimValidator.Validate(
                new CwtClaims(),
                agePolicy);
        TokenResult tooOld = ValidateTime(
            CwtClaimLabels.IssuedAt,
            Unsigned(899),
            agePolicy);
        TokenResult withinSkew = ValidateTime(
            CwtClaimLabels.IssuedAt,
            Unsigned(895),
            skewPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(missingExpiration, "missing_expiration_time");
            AssertFailure(missingIssuedAt, "missing_issued_at");
            AssertFailure(tooOld, "token_too_old");
            Assert.That(withinSkew.Success, Is.True);
        });
    }

    [Test]
    public void RequiredIntegerAndTextClaimsAreEnforced()
    {
        CwtValidationPolicy integerPolicy = Policy(1000);
        integerPolicy.RequiredClaims = new[] { CwtClaimLabels.Subject };
        CwtValidationPolicy textPolicy = Policy(1000);
        textPolicy.RequiredTextClaims = new[] { "tenant" };

        TokenResult integerResult = CwtRegisteredClaimValidator.Validate(
            new CwtClaims(),
            integerPolicy);
        TokenResult textResult = CwtRegisteredClaimValidator.Validate(
            new CwtClaims(),
            textPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(integerResult, "missing_required_claim");
            AssertFailure(textResult, "missing_required_claim");
        });
    }

    [Test]
    public void PolicyValidationRejectsContradictionsAndCopyIsIndependent()
    {
        CwtValidationPolicy source = Policy(1000);
        source.ValidIssuers = new[] { "issuer" };
        source.RequiredClaims = new[] { CwtClaimLabels.Subject };
        source.RequiredTextClaims = new[] { "tenant" };
        source.MaximumProtectionLayers = 4;
        source.RequireCwtTag = true;
        source.AllowCompatibilitySignatures = true;
        source.LayerExternalAuthenticatedData = new byte[][]
        {
            new byte[] { 1, 2 }
        };
        CwtValidationPolicy copy = source.Copy();
        copy.ValidIssuers[0] = "changed";
        copy.RequiredClaims[0] = CwtClaimLabels.Audience;
        copy.RequiredTextClaims[0] = "changed";
        copy.TokenPolicy.ClockSkewSeconds = 9;
        copy.MaximumProtectionLayers = 3;
        copy.RequireCwtTag = false;
        copy.LayerExternalAuthenticatedData[0][0] = 9;
        CwtValidationPolicy contradiction = Policy(1000);
        contradiction.RequireExpirationTime = true;
        contradiction.ValidateExpirationTime = false;
        CwtValidationPolicy duplicate = Policy(1000);
        duplicate.RequiredClaims = new[] { 1, 1 };
        CwtValidationPolicy noIssuer = Policy(1000);
        noIssuer.ValidateIssuer = true;

        Assert.Multiple(() =>
        {
            Assert.That(source.ValidIssuers[0], Is.EqualTo("issuer"));
            Assert.That(source.RequiredClaims[0],
                Is.EqualTo(CwtClaimLabels.Subject));
            Assert.That(source.RequiredTextClaims[0], Is.EqualTo("tenant"));
            Assert.That(source.TokenPolicy.ClockSkewSeconds, Is.Zero);
            Assert.That(source.MaximumProtectionLayers, Is.EqualTo(4));
            Assert.That(source.RequireCwtTag, Is.True);
            Assert.That(copy.AllowCompatibilitySignatures, Is.True);
            Assert.That(source.LayerExternalAuthenticatedData[0],
                Is.EqualTo(new byte[] { 1, 2 }));
            AssertFailure(contradiction.Validate(), "invalid_policy");
            AssertFailure(duplicate.Validate(), "invalid_policy");
            AssertFailure(noIssuer.Validate(), "invalid_policy");
        });
    }

    static void AssertInvalidType(int label, CborItem value)
    {
        TokenResult result = CwtRegisteredClaimValidator.Validate(
            Claims(label, value),
            Policy(1000));
        AssertFailure(result, "invalid_claim_type");
    }

    static TokenResult ValidateTime(
        int label,
        CborItem value,
        CwtValidationPolicy policy)
    {
        return CwtRegisteredClaimValidator.Validate(
            Claims(label, value),
            policy);
    }

    static CwtClaims Claims(int label, CborItem value)
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(label, value);
        return claims;
    }

    static CwtValidationPolicy Policy(long currentSeconds)
    {
        CwtValidationPolicy policy = new CwtValidationPolicy();
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(currentSeconds));
        return policy;
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Floating(double value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.FloatingPoint;
        item.FloatingPointValue = value;
        item.FloatingPointWidth = 8;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static CborItem Tagged(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
