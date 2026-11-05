using token;

namespace token.Tests;

public class JwtHeaderClaimReplicationValidationTests
{
    [Test]
    public void MissingReplicasAndDisabledValidationAcceptClaims()
    {
        JwtValidationPolicy enabled = Policy();
        JwtValidationPolicy disabled = Policy();
        disabled.ValidateHeaderClaimReplication = false;

        TokenResult missing = JwtHeaderClaimReplicationValidator.Validate(
            Header("{}"),
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\"}"),
            enabled);
        TokenResult mismatched = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"iss\":\"other\",\"sub\":\"other\",\"aud\":\"other\"}"),
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\"}"),
            disabled);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(mismatched.Success, Is.True);
        });
    }

    [Test]
    public void MatchingIssuerSubjectAndAudienceReplicasAreAccepted()
    {
        JoseHeader header = Header(
            "{\"iss\":\"issuer-é😀\",\"sub\":\"subject\","
            + "\"aud\":[\"api\",\"admin\"]}");
        JwtClaims claims = Claims(
            "{\"iss\":\"issuer-é😀\",\"sub\":\"subject\","
            + "\"aud\":[\"api\",\"admin\"]}");

        TokenResult result = JwtHeaderClaimReplicationValidator.Validate(
            header,
            claims,
            Policy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void ReplicatedClaimRequiresPayloadClaim()
    {
        TokenResult issuer = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"iss\":\"issuer\"}"),
            Claims("{}"),
            Policy());
        TokenResult subject = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"sub\":\"subject\"}"),
            Claims("{}"),
            Policy());
        TokenResult audience = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"aud\":\"api\"}"),
            Claims("{}"),
            Policy());

        Assert.Multiple(() =>
        {
            AssertFailure(issuer, "missing_replicated_claim");
            Assert.That(issuer.Error.Message, Does.Contain("iss"));
            AssertFailure(subject, "missing_replicated_claim");
            Assert.That(subject.Error.Message, Does.Contain("sub"));
            AssertFailure(audience, "missing_replicated_claim");
            Assert.That(audience.Error.Message, Does.Contain("aud"));
        });
    }

    [Test]
    public void ReplicasRequireExactCaseShapeOrderAndValueEquality()
    {
        TokenResult issuerCase = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"iss\":\"Issuer\"}"),
            Claims("{\"iss\":\"issuer\"}"),
            Policy());
        TokenResult audienceShape = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"aud\":\"api\"}"),
            Claims("{\"aud\":[\"api\"]}"),
            Policy());
        TokenResult audienceOrder = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"aud\":[\"api\",\"admin\"]}"),
            Claims("{\"aud\":[\"admin\",\"api\"]}"),
            Policy());
        TokenResult subjectValue = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"sub\":\"one\"}"),
            Claims("{\"sub\":\"two\"}"),
            Policy());

        Assert.Multiple(() =>
        {
            AssertFailure(issuerCase, "replicated_claim_mismatch");
            AssertFailure(audienceShape, "replicated_claim_mismatch");
            AssertFailure(audienceOrder, "replicated_claim_mismatch");
            AssertFailure(subjectValue, "replicated_claim_mismatch");
        });
    }

    [Test]
    public void ReplicationValidationRejectsMalformedMutableValues()
    {
        JoseHeader invalidHeader = new JoseHeader();
        invalidHeader.Set(JoseHeaderNames.Issuer, BooleanValue());
        JwtClaims validClaims = Claims("{\"iss\":\"issuer\"}");
        JwtClaims invalidClaims = new JwtClaims();
        invalidClaims.Set(JwtClaimNames.Audience, ArrayValue(BooleanValue()));

        TokenResult headerResult = JwtHeaderClaimReplicationValidator.Validate(
            invalidHeader,
            validClaims,
            Policy());
        TokenResult claimResult = JwtHeaderClaimReplicationValidator.Validate(
            Header("{\"aud\":[\"api\"]}"),
            invalidClaims,
            Policy());

        Assert.Multiple(() =>
        {
            AssertFailure(headerResult, "invalid_replicated_claim");
            AssertFailure(claimResult, "invalid_replicated_claim");
        });
    }

    [Test]
    public void EncryptedValidationAppliesReplicationPolicy()
    {
        TokenKey key = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims claims = Claims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":[\"api\",\"admin\"]}");
        string accepted = JsonWebToken.CreateEncrypted(
            claims,
            key,
            Header("{\"enc\":\"A128GCM\",\"iss\":\"issuer\","
                + "\"sub\":\"subject\",\"aud\":[\"api\",\"admin\"]}"),
            creationPolicy).Text;
        string rejected = JsonWebToken.CreateEncrypted(
            claims,
            key,
            Header("{\"enc\":\"A128GCM\",\"iss\":\"other\"}"),
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        JwtValidationResult acceptedResult = JsonWebToken.Validate(
            accepted,
            resolver,
            policy);
        JwtValidationResult rejectedResult = JsonWebToken.Validate(
            rejected,
            resolver,
            policy);
        policy.ValidateHeaderClaimReplication = false;
        JwtValidationResult disabledResult = JsonWebToken.Validate(
            rejected,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedResult.Success, Is.True);
            AssertValidationFailure(rejectedResult, "replicated_claim_mismatch");
            Assert.That(disabledResult.Success, Is.True);
        });
    }

    [Test]
    public void NestedValidationChecksOuterReplicasBeforeReplayCallback()
    {
        int calls = 0;
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims claims = Claims(
            "{\"iss\":\"issuer\",\"jti\":\"id-1\",\"exp\":2000}");
        string accepted = JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            Header("{}"),
            Header("{\"enc\":\"A128GCM\",\"iss\":\"issuer\"}"),
            creationPolicy).Text;
        string rejected = JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            Header("{}"),
            Header("{\"enc\":\"A128GCM\",\"iss\":\"other\"}"),
            creationPolicy).Text;
        JwtValidationPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);

        JwtValidationResult rejectedResult = JsonWebToken.Validate(
            rejected,
            resolver,
            policy);
        JwtValidationResult acceptedResult = JsonWebToken.Validate(
            accepted,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(rejectedResult, "replicated_claim_mismatch");
            Assert.That(acceptedResult.Success, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReplicationValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy invalidPolicy = Policy();
        invalidPolicy.ValidAudiences = null;
        JoseHeader header = Header("{}");
        JwtClaims claims = Claims("{}");

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtHeaderClaimReplicationValidator.Validate(
                    header,
                    claims,
                    invalidPolicy),
                "invalid_policy");
            Assert.That(
                () => JwtHeaderClaimReplicationValidator.Validate(
                    null,
                    claims,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtHeaderClaimReplicationValidator.Validate(
                    header,
                    null,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtHeaderClaimReplicationValidator.Validate(
                    header,
                    claims,
                    null),
                Throws.ArgumentNullException);
        });
    }

    static JoseHeader Header(string json)
    {
        return JoseHeader.Read(json).Header;
    }

    static JwtClaims Claims(string json)
    {
        return JwtClaims.Read(json).Claims;
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

    static JsonValue BooleanValue()
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Boolean;
        result.BooleanValue = true;
        return result;
    }

    static JsonValue ArrayValue(JsonValue value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Array;
        result.ArrayValue = new[] { value };
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
