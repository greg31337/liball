using token;

namespace token.Tests;

public class JwtLifetimeValidationTests
{
    [Test]
    public void ExpirationUsesExclusiveBoundaryAndClockSkew()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);

        TokenResult future = Validate("{\"exp\":1000.0000001}", policy);
        TokenResult boundary = Validate("{\"exp\":1000}", policy);
        policy.TokenPolicy.ClockSkewSeconds = 5;
        TokenResult withinSkew = Validate("{\"exp\":996}", policy);
        TokenResult skewBoundary = Validate("{\"exp\":995}", policy);

        Assert.Multiple(() =>
        {
            Assert.That(future.Success, Is.True);
            AssertFailure(boundary, "token_expired");
            Assert.That(withinSkew.Success, Is.True);
            AssertFailure(skewBoundary, "token_expired");
        });
    }

    [Test]
    public void RequiredExpirationRejectsMissingClaim()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.RequireExpirationTime = true;

        TokenResult result = Validate("{}", policy);

        AssertFailure(result, "missing_expiration_time");
    }

    [Test]
    public void NotBeforeUsesInclusiveClockSkewBoundary()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);

        TokenResult boundary = Validate("{\"nbf\":1000}", policy);
        TokenResult future = Validate("{\"nbf\":1000.0000001}", policy);
        policy.TokenPolicy.ClockSkewSeconds = 5;
        TokenResult skewBoundary = Validate("{\"nbf\":1005}", policy);
        TokenResult beyondSkew = Validate("{\"nbf\":1005.0000001}", policy);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Success, Is.True);
            AssertFailure(future, "token_not_yet_valid");
            Assert.That(skewBoundary.Success, Is.True);
            AssertFailure(beyondSkew, "token_not_yet_valid");
        });
    }

    [Test]
    public void IssuedAtRejectsFutureValuesBeyondClockSkew()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);

        TokenResult boundary = Validate("{\"iat\":1000}", policy);
        TokenResult future = Validate("{\"iat\":1000.0000001}", policy);
        policy.TokenPolicy.ClockSkewSeconds = 5;
        TokenResult skewBoundary = Validate("{\"iat\":1005}", policy);
        TokenResult beyondSkew = Validate("{\"iat\":1005.0000001}", policy);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Success, Is.True);
            AssertFailure(future, "issued_at_in_future");
            Assert.That(skewBoundary.Success, Is.True);
            AssertFailure(beyondSkew, "issued_at_in_future");
        });
    }

    [Test]
    public void MaximumAgeRequiresIssuedAtAndUsesInclusiveBoundary()
    {
        JwtValidationPolicy policy = PolicyAt(1100m);
        policy.MaximumTokenAgeSeconds = 100;

        TokenResult missing = Validate("{}", policy);
        TokenResult boundary = Validate("{\"iat\":1000}", policy);
        policy.Clock = ClockAt(1100.0000001m);
        TokenResult tooOld = Validate("{\"iat\":1000}", policy);
        policy.TokenPolicy.ClockSkewSeconds = 5;
        policy.Clock = ClockAt(1105m);
        TokenResult skewBoundary = Validate("{\"iat\":1000}", policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, "missing_issued_at");
            Assert.That(boundary.Success, Is.True);
            AssertFailure(tooOld, "token_too_old");
            Assert.That(skewBoundary.Success, Is.True);
        });
    }

    [Test]
    public void LifetimeValidationSupportsPreEpochAndFractionalNumericDates()
    {
        JwtValidationPolicy policy = PolicyAt(-50.5m);

        TokenResult result = Validate(
            "{\"exp\":-40.25,\"nbf\":-60.75,\"iat\":-70.125}",
            policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void LifetimeValidationReadsClockOnceForAllEnabledChecks()
    {
        int reads = 0;
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.MaximumTokenAgeSeconds = 500;
        policy.Clock = new TokenClock(() =>
        {
            reads += 1;
            return DateTimeOffset.UnixEpoch.AddSeconds(1000);
        });

        TokenResult result = Validate(
            "{\"exp\":1200,\"nbf\":800,\"iat\":900}",
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(reads, Is.EqualTo(1));
        });
    }

    [Test]
    public void DisabledLifetimeChecksIgnoreClaimsWithoutReadingClock()
    {
        int reads = 0;
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.ValidateExpirationTime = false;
        policy.ValidateNotBefore = false;
        policy.ValidateIssuedAt = false;
        policy.Clock = new TokenClock(() =>
        {
            reads += 1;
            return DateTimeOffset.UnixEpoch;
        });
        JwtClaims claims = new JwtClaims();
        claims.Set(JwtClaimNames.ExpirationTime, BooleanValue());
        claims.Set(JwtClaimNames.NotBefore, BooleanValue());
        claims.Set(JwtClaimNames.IssuedAt, BooleanValue());

        TokenResult result = JwtLifetimeValidator.Validate(claims, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(reads, Is.Zero);
        });
    }

    [Test]
    public void LifetimeValidationRejectsMalformedMutableNumericDates()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);
        JwtClaims expiration = new JwtClaims();
        expiration.Set(JwtClaimNames.ExpirationTime, NumberValue("1e100"));
        JwtClaims notBefore = new JwtClaims();
        notBefore.Set(JwtClaimNames.NotBefore, NumberValue("invalid"));
        JwtClaims issuedAt = new JwtClaims();
        issuedAt.Set(JwtClaimNames.IssuedAt, BooleanValue());

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtLifetimeValidator.Validate(expiration, policy),
                "invalid_expiration_time");
            AssertFailure(
                JwtLifetimeValidator.Validate(notBefore, policy),
                "invalid_not_before");
            AssertFailure(
                JwtLifetimeValidator.Validate(issuedAt, policy),
                "invalid_issued_at");
        });
    }

    [Test]
    public void SignedEncryptedAndNestedValidationApplyLifetimePolicy()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims expiredClaims = JwtClaims.Read("{\"exp\":1000}").Claims;
        JwtClaims earlyClaims = JwtClaims.Read("{\"nbf\":1001}").Claims;
        JwtClaims futureClaims = JwtClaims.Read("{\"iat\":1001}").Claims;
        string signed = JsonWebToken.CreateSigned(
            expiredClaims,
            signingKey,
            creationPolicy).Text;
        string encrypted = JsonWebToken.CreateEncrypted(
            earlyClaims,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string nested = JsonWebToken.CreateSignedThenEncrypted(
            futureClaims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        JwtValidationPolicy policy = PolicyAt(
            1000m,
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryptionKey)
                : KeySuccess(signingKey));

        JwtValidationResult signedResult = JsonWebToken.Validate(
            signed,
            resolver,
            policy);
        JwtValidationResult encryptedResult = JsonWebToken.Validate(
            encrypted,
            resolver,
            policy);
        JwtValidationResult nestedResult = JsonWebToken.Validate(
            nested,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(signedResult, "token_expired");
            AssertValidationFailure(encryptedResult, "token_not_yet_valid");
            AssertValidationFailure(nestedResult, "issued_at_in_future");
        });
    }

    [Test]
    public void LifetimeValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy invalidPolicy = PolicyAt(1000m);
        invalidPolicy.TokenPolicy.ClockSkewSeconds = -1;
        JwtClaims claims = JwtClaims.Read("{}").Claims;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtLifetimeValidator.Validate(claims, invalidPolicy),
                "invalid_policy");
            Assert.That(
                () => JwtLifetimeValidator.Validate(null, PolicyAt(1000m)),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtLifetimeValidator.Validate(claims, null),
                Throws.ArgumentNullException);
        });
    }

    static TokenResult Validate(string json, JwtValidationPolicy policy)
    {
        return JwtLifetimeValidator.Validate(JwtClaims.Read(json).Claims, policy);
    }

    static JwtValidationPolicy PolicyAt(decimal seconds, params string[] algorithms)
    {
        JwtValidationPolicy policy = JwtValidationPolicy.Create(
            TokenPolicyFor(algorithms));
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = ClockAt(seconds);
        return policy;
    }

    static ITokenClock ClockAt(decimal seconds)
    {
        DateTimeOffset instant = JwtNumericDate.ToDateTimeOffset(seconds).DateTime;
        return new FixedClock(instant);
    }

    static TokenKey HmacKey(int start)
    {
        byte[] bytes = Enumerable.Range(start, 32)
            .Select(value => (byte)value)
            .ToArray();
        return SymmetricKeyImporterOperations.ImportJose(
            bytes,
            JoseAlgorithms.HmacSha256).Key;
    }

    static TokenKey DirectKey(int start)
    {
        byte[] bytes = Enumerable.Range(start, 16)
            .Select(value => (byte)value)
            .ToArray();
        TokenKey key = SymmetricKeyImporterOperations.Import(bytes).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        return key;
    }

    class FixedClock : ITokenClock
    {
        DateTimeOffset instant;

        public FixedClock(DateTimeOffset instant)
        {
            this.instant = instant;
        }

        public DateTimeOffset GetUtcNow()
        {
            return instant;
        }
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static JsonValue NumberValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = value;
        return result;
    }

    static JsonValue BooleanValue()
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Boolean;
        result.BooleanValue = true;
        return result;
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertValidationFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
