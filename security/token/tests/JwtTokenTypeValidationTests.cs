using token;

namespace token.Tests;

public class JwtTokenTypeValidationTests
{
    [Test]
    public void DisabledTokenTypeValidationAcceptsMissingAndUnexpectedTypes()
    {
        JwtValidationPolicy policy = Policy();

        TokenResult missing = JwtTokenTypeValidator.Validate(
            Header("{}"),
            policy);
        TokenResult unexpected = JwtTokenTypeValidator.Validate(
            Header("{\"typ\":\"unexpected\"}"),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(unexpected.Success, Is.True);
        });
    }

    [TestCase("JWT", "application/jwt")]
    [TestCase("APPLICATION/JWT", "jwt")]
    [TestCase("at+jwt", "application/at+jwt")]
    [TestCase("logout+jwt", "application/logout+jwt")]
    [TestCase("secevent+jwt", "application/secevent+jwt")]
    [TestCase("dpop+jwt", "application/dpop+jwt")]
    [TestCase("id_token", "id_token")]
    [TestCase("custom-token", "custom-token")]
    public void EnabledValidationAcceptsCommonAliasesAndCustomTypes(
        string actual,
        string expected)
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "other", expected };

        TokenResult result = JwtTokenTypeValidator.Validate(
            Header("{\"typ\":\"" + actual + "\"}"),
            policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void EnabledValidationRequiresTypeAndMatchesCustomTypesExactly()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "Custom-Type" };

        TokenResult missing = JwtTokenTypeValidator.Validate(
            Header("{}"),
            policy);
        TokenResult wrongCase = JwtTokenTypeValidator.Validate(
            Header("{\"typ\":\"custom-type\"}"),
            policy);
        TokenResult mismatch = JwtTokenTypeValidator.Validate(
            Header("{\"typ\":\"different\"}"),
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, "missing_token_type");
            AssertFailure(wrongCase, "token_type_mismatch");
            AssertFailure(mismatch, "token_type_mismatch");
        });
    }

    [Test]
    public void TokenTypeValidationRejectsMalformedMutableValues()
    {
        JwtValidationPolicy policy = Policy();
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "JWT" };
        JoseHeader empty = new JoseHeader();
        empty.Set(JoseHeaderNames.Type, StringValue(""));
        JoseHeader wrongKind = new JoseHeader();
        wrongKind.Set(JoseHeaderNames.Type, BooleanValue());
        JoseHeader invalidText = new JoseHeader();
        invalidText.Set(JoseHeaderNames.Type, StringValue("\uD800"));
        JwtValidationPolicy invalidPolicy = Policy();
        invalidPolicy.ValidateTokenType = true;
        invalidPolicy.ValidTokenTypes = new[] { "\uD800" };

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtTokenTypeValidator.Validate(empty, policy),
                "invalid_token_type");
            AssertFailure(
                JwtTokenTypeValidator.Validate(wrongKind, policy),
                "invalid_token_type");
            AssertFailure(
                JwtTokenTypeValidator.Validate(invalidText, policy),
                "invalid_token_type");
            AssertFailure(
                JwtTokenTypeValidator.Validate(Header("{\"typ\":\"JWT\"}"), invalidPolicy),
                "invalid_policy");
        });
    }

    [Test]
    public void SignedAndEncryptedValidationApplyTokenTypePolicy()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string signed = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"sub\":\"signed\"}").Claims,
            signingKey,
            Header("{\"typ\":\"at+jwt\"}"),
            creationPolicy).Text;
        string encrypted = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"sub\":\"encrypted\"}").Claims,
            encryptionKey,
            Header("{\"enc\":\"A128GCM\",\"typ\":\"dpop+jwt\"}"),
            creationPolicy).Text;
        JwtValidationPolicy signedPolicy = Policy(JoseAlgorithms.HmacSha256);
        signedPolicy.ValidateTokenType = true;
        signedPolicy.ValidTokenTypes = new[] { JoseMediaTypes.AccessTokenJwt };
        JwtValidationPolicy encryptedPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        encryptedPolicy.ValidateTokenType = true;
        encryptedPolicy.ValidTokenTypes = new[] { JoseMediaTypes.DpopJwt };
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);

        JwtValidationResult signedResult = JsonWebToken.Validate(
            signed,
            resolver,
            signedPolicy);
        JwtValidationResult encryptedResult = JsonWebToken.Validate(
            encrypted,
            resolver,
            encryptedPolicy);
        encryptedPolicy.ValidTokenTypes = new[] { "id_token" };
        JwtValidationResult rejected = JsonWebToken.Validate(
            encrypted,
            resolver,
            encryptedPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(signedResult.Success, Is.True);
            Assert.That(signedResult.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(encryptedResult.Success, Is.True);
            Assert.That(encryptedResult.TokenType, Is.EqualTo("dpop+jwt"));
            AssertValidationFailure(rejected, "token_type_mismatch");
        });
    }

    [Test]
    public void NestedValidationAppliesPolicyToInnerClaimsBearingType()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JoseHeader outer = Header("{\"enc\":\"A128GCM\"}");
        string accepted = JwtCreator.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"sub\":\"nested\"}").Claims,
            signingKey,
            encryptionKey,
            Header("{\"typ\":\"id_token\"}"),
            outer,
            creationPolicy).Text;
        string rejected = JwtCreator.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"sub\":\"nested\"}").Claims,
            signingKey,
            encryptionKey,
            Header("{\"typ\":\"logout+jwt\"}"),
            outer,
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "id_token" };
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);

        JwtValidationResult acceptedResult = JsonWebToken.Validate(
            accepted,
            resolver,
            policy);
        JwtValidationResult rejectedResult = JsonWebToken.Validate(
            rejected,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedResult.Success, Is.True);
            Assert.That(
                acceptedResult.InnerProtectedHeader.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("id_token"));
            AssertValidationFailure(rejectedResult, "token_type_mismatch");
        });
    }

    [Test]
    public void TokenTypeMismatchPreventsReplayCallbackInvocation()
    {
        int calls = 0;
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"jti\":\"id-1\",\"exp\":2000}").Claims,
            key,
            Header("{\"typ\":\"JWT\"}"),
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.ValidateTokenType = true;
        policy.ValidTokenTypes = new[] { "at+jwt" };
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
            AssertValidationFailure(result, "token_type_mismatch");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void TokenTypeValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy missingTypes = Policy();
        missingTypes.ValidateTokenType = true;
        JwtValidationPolicy duplicateTypes = Policy();
        duplicateTypes.ValidateTokenType = true;
        duplicateTypes.ValidTokenTypes = new[] { "JWT", "JWT" };
        JoseHeader header = Header("{\"typ\":\"JWT\"}");

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtTokenTypeValidator.Validate(header, missingTypes),
                "invalid_policy");
            AssertFailure(
                JwtTokenTypeValidator.Validate(header, duplicateTypes),
                "invalid_policy");
            Assert.That(
                () => JwtTokenTypeValidator.Validate(null, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtTokenTypeValidator.Validate(header, null),
                Throws.ArgumentNullException);
        });
    }

    static JoseHeader Header(string json)
    {
        return JoseHeader.Read(json).Header;
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

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
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
