using token;

namespace token.Tests;

public class JwtRequiredClaimValidationTests
{
    [Test]
    public void EmptyRequiredClaimListAcceptsAnyClaimSet()
    {
        JwtValidationPolicy policy = Policy();

        TokenResult result = JwtRequiredClaimValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void ValidationAcceptsEveryRequiredRegisteredAndPrivateClaim()
    {
        JwtValidationPolicy policy = Policy();
        policy.TokenPolicy.RequiredClaims = new[]
        {
            JwtClaimNames.Subject,
            "private-null",
            "private-object"
        };

        TokenResult result = JwtRequiredClaimValidator.Validate(
            JwtClaims.Read(
                "{\"sub\":\"subject\",\"private-null\":null,"
                + "\"private-object\":{}}").Claims,
            policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void ValidationRequiresExactCaseSensitiveClaimPresence()
    {
        JwtValidationPolicy policy = Policy();
        policy.TokenPolicy.RequiredClaims = new[] { "first", "Subject" };

        TokenResult firstMissing = JwtRequiredClaimValidator.Validate(
            JwtClaims.Read("{\"Subject\":\"value\"}").Claims,
            policy);
        TokenResult wrongCase = JwtRequiredClaimValidator.Validate(
            JwtClaims.Read("{\"first\":true,\"subject\":\"value\"}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(firstMissing, "missing_required_claim");
            Assert.That(firstMissing.Error.Message, Does.Contain("first"));
            AssertFailure(wrongCase, "missing_required_claim");
            Assert.That(wrongCase.Error.Message, Does.Contain("Subject"));
        });
    }

    [Test]
    public void SignedEncryptedAndNestedValidationApplyRequiredClaims()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims completeClaims = JwtClaims.Read(
            "{\"sub\":\"subject\",\"private\":true}").Claims;
        JwtClaims incompleteClaims = JwtClaims.Read(
            "{\"sub\":\"subject\"}").Claims;
        string signedAccepted = JsonWebToken.CreateSigned(
            completeClaims,
            signingKey,
            creationPolicy).Text;
        string signedRejected = JsonWebToken.CreateSigned(
            incompleteClaims,
            signingKey,
            creationPolicy).Text;
        string encryptedAccepted = JsonWebToken.CreateEncrypted(
            completeClaims,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string encryptedRejected = JsonWebToken.CreateEncrypted(
            incompleteClaims,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string nestedAccepted = JsonWebToken.CreateSignedThenEncrypted(
            completeClaims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string nestedRejected = JsonWebToken.CreateSignedThenEncrypted(
            incompleteClaims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.TokenPolicy.RequiredClaims = new[]
        {
            JwtClaimNames.Subject,
            "private"
        };
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);

        JwtValidationResult signedSuccess = JsonWebToken.Validate(
            signedAccepted,
            resolver,
            policy);
        JwtValidationResult signedFailure = JsonWebToken.Validate(
            signedRejected,
            resolver,
            policy);
        JwtValidationResult encryptedSuccess = JsonWebToken.Validate(
            encryptedAccepted,
            resolver,
            policy);
        JwtValidationResult encryptedFailure = JsonWebToken.Validate(
            encryptedRejected,
            resolver,
            policy);
        JwtValidationResult nestedSuccess = JsonWebToken.Validate(
            nestedAccepted,
            resolver,
            policy);
        JwtValidationResult nestedFailure = JsonWebToken.Validate(
            nestedRejected,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(signedSuccess.Success, Is.True);
            AssertValidationFailure(signedFailure, "missing_required_claim");
            Assert.That(encryptedSuccess.Success, Is.True);
            AssertValidationFailure(encryptedFailure, "missing_required_claim");
            Assert.That(nestedSuccess.Success, Is.True);
            AssertValidationFailure(nestedFailure, "missing_required_claim");
        });
    }

    [Test]
    public void MissingRequiredClaimPreventsReplayCallbackInvocation()
    {
        int calls = 0;
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"jti\":\"id-1\",\"exp\":2000}").Claims,
            key,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.TokenPolicy.RequiredClaims = new[] { JwtClaimNames.Subject };
        policy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult result = JsonWebToken.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(result, "missing_required_claim");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void RequiredClaimValidationRejectsInvalidPolicyAndArguments()
    {
        JwtValidationPolicy nullNames = Policy();
        nullNames.TokenPolicy.RequiredClaims = null;
        JwtValidationPolicy emptyName = Policy();
        emptyName.TokenPolicy.RequiredClaims = new[] { "" };
        JwtValidationPolicy duplicate = Policy();
        duplicate.TokenPolicy.RequiredClaims = new[] { "sub", "sub" };
        JwtClaims claims = JwtClaims.Read("{\"sub\":\"subject\"}").Claims;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtRequiredClaimValidator.Validate(claims, nullNames),
                "invalid_policy");
            AssertFailure(
                JwtRequiredClaimValidator.Validate(claims, emptyName),
                "invalid_policy");
            AssertFailure(
                JwtRequiredClaimValidator.Validate(claims, duplicate),
                "invalid_policy");
            Assert.That(
                () => JwtRequiredClaimValidator.Validate(null, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtRequiredClaimValidator.Validate(claims, null),
                Throws.ArgumentNullException);
        });
    }

    static JwtValidationPolicy Policy(params string[] algorithms)
    {
        return JwtValidationPolicy.Create(TokenPolicyFor(algorithms));
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

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResolver Resolver(TokenKey signingKey, TokenKey encryptionKey)
    {
        return new TokenKeyResolver(context =>
            context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryptionKey)
                : KeySuccess(signingKey));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
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
