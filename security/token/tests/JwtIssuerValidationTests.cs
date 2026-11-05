using token;

namespace token.Tests;

public class JwtIssuerValidationTests
{
    [Test]
    public void DisabledIssuerValidationAcceptsMissingAndUnexpectedIssuers()
    {
        JwtValidationPolicy policy = Policy();

        TokenResult missing = JwtIssuerValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);
        TokenResult unexpected = JwtIssuerValidator.Validate(
            JwtClaims.Read("{\"iss\":\"unexpected\"}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(unexpected.Success, Is.True);
        });
    }

    [Test]
    public void EnabledIssuerValidationMatchesAnyConfiguredIssuerExactly()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[]
        {
            "https://issuer-one.example",
            "https://issuer.example/é😀"
        };

        TokenResult first = JwtIssuerValidator.Validate(
            JwtClaims.Read("{\"iss\":\"https://issuer-one.example\"}").Claims,
            policy);
        TokenResult second = JwtIssuerValidator.Validate(
            JwtClaims.Read("{\"iss\":\"https://issuer.example/é😀\"}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Error, Is.Null);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Error, Is.Null);
        });
    }

    [Test]
    public void EnabledIssuerValidationRequiresIssuerAndUsesOrdinalComparison()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "https://Issuer.Example/path" };

        TokenResult missing = JwtIssuerValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);
        TokenResult wrongCase = JwtIssuerValidator.Validate(
            JwtClaims.Read("{\"iss\":\"https://issuer.example/path\"}").Claims,
            policy);
        TokenResult suffix = JwtIssuerValidator.Validate(
            JwtClaims.Read("{\"iss\":\"https://Issuer.Example/path/\"}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, "missing_issuer");
            AssertFailure(wrongCase, "issuer_mismatch");
            AssertFailure(suffix, "issuer_mismatch");
        });
    }

    [Test]
    public void IssuerValidationRejectsInvalidMutableClaimAndPolicyText()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "issuer" };
        JwtClaims invalidClaim = new JwtClaims();
        JsonValue issuer = new JsonValue();
        issuer.Kind = TokenJsonValueKind.String;
        issuer.StringValue = "\uD800";
        invalidClaim.Set(JwtClaimNames.Issuer, issuer);
        JwtValidationPolicy invalidPolicy = Policy();
        invalidPolicy.ValidateIssuer = true;
        invalidPolicy.ValidIssuers = new[] { "\uD800" };

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtIssuerValidator.Validate(invalidClaim, policy),
                "invalid_issuer");
            AssertFailure(
                JwtIssuerValidator.Validate(
                    JwtClaims.Read("{\"iss\":\"issuer\"}").Claims,
                    invalidPolicy),
                "invalid_policy");
        });
    }

    [Test]
    public void SignedJwtValidationAppliesIssuerPolicyAfterAuthentication()
    {
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256);
        string acceptedToken = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"iss\":\"issuer\",\"sub\":\"subject\"}").Claims,
            key,
            creationPolicy).Text;
        string rejectedToken = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"iss\":\"other\",\"sub\":\"subject\"}").Claims,
            key,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "issuer" };
        TokenKeyResolver resolver = new TokenKeyResolver(context => KeySuccess(key));

        JwtValidationResult accepted = JsonWebToken.Validate(
            acceptedToken,
            resolver,
            policy);
        JwtValidationResult rejected = JsonWebToken.Validate(
            rejectedToken,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.SignatureValidated, Is.True);
            Assert.That(accepted.Claims.Get(JwtClaimNames.Issuer).StringValue,
                Is.EqualTo("issuer"));
            AssertValidationFailure(rejected, "issuer_mismatch");
        });
    }

    [Test]
    public void EncryptedAndNestedJwtValidationApplyIssuerPolicy()
    {
        TokenKey signingKey = HmacKey(40);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims acceptedClaims = JwtClaims.Read(
            "{\"iss\":\"issuer\",\"sub\":\"subject\"}").Claims;
        JwtClaims rejectedClaims = JwtClaims.Read(
            "{\"iss\":\"other\",\"sub\":\"subject\"}").Claims;
        string encrypted = JsonWebToken.CreateEncrypted(
            acceptedClaims,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string nested = JsonWebToken.CreateSignedThenEncrypted(
            acceptedClaims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string rejectedNested = JsonWebToken.CreateSignedThenEncrypted(
            rejectedClaims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.ValidateIssuer = true;
        policy.ValidIssuers = new[] { "issuer" };
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryptionKey)
                : KeySuccess(signingKey));

        JwtValidationResult encryptedResult = JsonWebToken.Validate(
            encrypted,
            resolver,
            policy);
        JwtValidationResult nestedResult = JsonWebToken.Validate(
            nested,
            resolver,
            policy);
        JwtValidationResult rejectedResult = JsonWebToken.Validate(
            rejectedNested,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(encryptedResult.Success, Is.True);
            Assert.That(encryptedResult.EncryptionValidated, Is.True);
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.SignatureValidated, Is.True);
            Assert.That(nestedResult.EncryptionValidated, Is.True);
            AssertValidationFailure(rejectedResult, "issuer_mismatch");
        });
    }

    [Test]
    public void IssuerValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy missingIssuers = Policy();
        missingIssuers.ValidateIssuer = true;
        JwtValidationPolicy duplicateIssuers = Policy();
        duplicateIssuers.ValidateIssuer = true;
        duplicateIssuers.ValidIssuers = new[] { "issuer", "issuer" };
        JwtClaims claims = JwtClaims.Read("{\"iss\":\"issuer\"}").Claims;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtIssuerValidator.Validate(claims, missingIssuers),
                "invalid_policy");
            AssertFailure(
                JwtIssuerValidator.Validate(claims, duplicateIssuers),
                "invalid_policy");
            Assert.That(
                () => JwtIssuerValidator.Validate(null, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtIssuerValidator.Validate(claims, null),
                Throws.ArgumentNullException);
        });
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

    static JwtValidationPolicy Policy(params string[] algorithms)
    {
        return JwtValidationPolicy.Create(TokenPolicyFor(algorithms));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
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
