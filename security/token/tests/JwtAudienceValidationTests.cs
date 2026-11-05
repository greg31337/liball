using token;

namespace token.Tests;

public class JwtAudienceValidationTests
{
    [Test]
    public void DisabledAudienceValidationAcceptsMissingAndUnexpectedAudiences()
    {
        JwtValidationPolicy policy = Policy();

        TokenResult missing = JwtAudienceValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);
        TokenResult unexpected = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":[\"unexpected\"]}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(unexpected.Success, Is.True);
        });
    }

    [Test]
    public void EnabledAudienceValidationAcceptsStringAndArrayForms()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api", "urn:audience:é😀" };

        TokenResult single = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":\"api\"}").Claims,
            policy);
        TokenResult arrayFirst = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":[\"api\",\"other\"]}").Claims,
            policy);
        TokenResult arraySecond = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":[\"other\",\"urn:audience:é😀\"]}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(single.Success, Is.True);
            Assert.That(single.Error, Is.Null);
            Assert.That(arrayFirst.Success, Is.True);
            Assert.That(arraySecond.Success, Is.True);
        });
    }

    [Test]
    public void EnabledAudienceValidationRequiresAudienceAndUsesOrdinalComparison()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "Api" };

        TokenResult missing = JwtAudienceValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);
        TokenResult wrongCase = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":\"api\"}").Claims,
            policy);
        TokenResult noIntersection = JwtAudienceValidator.Validate(
            JwtClaims.Read("{\"aud\":[\"one\",\"two\"]}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, "missing_audience");
            AssertFailure(wrongCase, "audience_mismatch");
            AssertFailure(noIntersection, "audience_mismatch");
        });
    }

    [Test]
    public void AudienceValidationRejectsEmptyAndMalformedMutableClaims()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api" };
        JwtClaims empty = new JwtClaims();
        JsonValue emptyValue = new JsonValue();
        emptyValue.Kind = TokenJsonValueKind.Array;
        emptyValue.ArrayValue = Array.Empty<JsonValue>();
        empty.Set(JwtClaimNames.Audience, emptyValue);
        JwtClaims nonString = new JwtClaims();
        JsonValue nonStringValue = new JsonValue();
        nonStringValue.Kind = TokenJsonValueKind.Array;
        nonStringValue.ArrayValue = new[] { BooleanValue(true) };
        nonString.Set(JwtClaimNames.Audience, nonStringValue);
        JwtClaims invalidText = new JwtClaims();
        invalidText.Set(JwtClaimNames.Audience, StringValue("\uD800"));
        JwtValidationPolicy invalidPolicy = Policy();
        invalidPolicy.ValidateAudience = true;
        invalidPolicy.ValidAudiences = new[] { "\uD800" };

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtAudienceValidator.Validate(empty, policy),
                "invalid_audience");
            AssertFailure(
                JwtAudienceValidator.Validate(nonString, policy),
                "invalid_audience");
            AssertFailure(
                JwtAudienceValidator.Validate(invalidText, policy),
                "invalid_audience");
            AssertFailure(
                JwtAudienceValidator.Validate(
                    JwtClaims.Read("{\"aud\":\"api\"}").Claims,
                    invalidPolicy),
                "invalid_policy");
        });
    }

    [Test]
    public void SignedJwtValidationAppliesAudiencePolicyAfterAuthentication()
    {
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string single = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"aud\":\"api\",\"sub\":\"subject\"}").Claims,
            key,
            creationPolicy).Text;
        string multiple = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"aud\":[\"other\",\"api\"]}").Claims,
            key,
            creationPolicy).Text;
        string rejected = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"aud\":\"other\"}").Claims,
            key,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api" };
        TokenKeyResolver resolver = new TokenKeyResolver(context => KeySuccess(key));

        JwtValidationResult singleResult = JsonWebToken.Validate(single, resolver, policy);
        JwtValidationResult multipleResult = JsonWebToken.Validate(multiple, resolver, policy);
        JwtValidationResult rejectedResult = JsonWebToken.Validate(rejected, resolver, policy);

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Success, Is.True);
            Assert.That(multipleResult.Success, Is.True);
            AssertValidationFailure(rejectedResult, "audience_mismatch");
        });
    }

    [Test]
    public void EncryptedAndNestedJwtValidationApplyAudiencePolicy()
    {
        TokenKey signingKey = HmacKey(40);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims acceptedClaims = JwtClaims.Read(
            "{\"aud\":[\"other\",\"api\"],\"sub\":\"subject\"}").Claims;
        JwtClaims rejectedClaims = JwtClaims.Read(
            "{\"aud\":\"other\",\"sub\":\"subject\"}").Claims;
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
        policy.ValidateAudience = true;
        policy.ValidAudiences = new[] { "api" };
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
            Assert.That(nestedResult.Success, Is.True);
            AssertValidationFailure(rejectedResult, "audience_mismatch");
        });
    }

    [Test]
    public void AudienceValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy missingAudiences = Policy();
        missingAudiences.ValidateAudience = true;
        JwtValidationPolicy duplicateAudiences = Policy();
        duplicateAudiences.ValidateAudience = true;
        duplicateAudiences.ValidAudiences = new[] { "api", "api" };
        JwtClaims claims = JwtClaims.Read("{\"aud\":\"api\"}").Claims;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtAudienceValidator.Validate(claims, missingAudiences),
                "invalid_policy");
            AssertFailure(
                JwtAudienceValidator.Validate(claims, duplicateAudiences),
                "invalid_policy");
            Assert.That(
                () => JwtAudienceValidator.Validate(null, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtAudienceValidator.Validate(claims, null),
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

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static JsonValue BooleanValue(bool value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Boolean;
        result.BooleanValue = value;
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
