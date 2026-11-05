using System.Text;
using token;

namespace token.Tests;

public class JwtReplayValidationTests
{
    [Test]
    public void MissingCallbackDisablesReplayValidation()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);

        TokenResult result = JwtReplayValidator.Validate(
            JwtClaims.Read("{}").Claims,
            policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void CallbackReceivesExactIdentifierAndExpiration()
    {
        string identifier = null;
        DateTimeOffset expiration = DateTimeOffset.MinValue;
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.ReplayValidator = (value, expires) =>
        {
            identifier = value;
            expiration = expires;
            return Success();
        };

        TokenResult result = JwtReplayValidator.Validate(
            JwtClaims.Read("{\"jti\":\"id-é😀\",\"exp\":1700.25}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(identifier, Is.EqualTo("id-é😀"));
            Assert.That(
                expiration,
                Is.EqualTo(DateTimeOffset.UnixEpoch.AddMilliseconds(1700250)));
            Assert.That(expiration.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void EnabledReplayValidationRequiresIdentifierAndExpiration()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.ReplayValidator = (identifier, expiration) => Success();

        TokenResult missingIdentifier = JwtReplayValidator.Validate(
            JwtClaims.Read("{\"exp\":2000}").Claims,
            policy);
        TokenResult missingExpiration = JwtReplayValidator.Validate(
            JwtClaims.Read("{\"jti\":\"id-1\"}").Claims,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(missingIdentifier, "missing_jwt_id");
            AssertFailure(missingExpiration, "missing_expiration_time");
        });
    }

    [Test]
    public void ReplayValidationRejectsMalformedMutableClaims()
    {
        JwtValidationPolicy policy = PolicyAt(1000m);
        policy.ReplayValidator = (identifier, expiration) => Success();
        JwtClaims emptyIdentifier = ClaimsWith(StringValue(""), NumberValue("2000"));
        JwtClaims wrongIdentifier = ClaimsWith(BooleanValue(), NumberValue("2000"));
        JwtClaims invalidText = ClaimsWith(StringValue("\uD800"), NumberValue("2000"));
        JwtClaims invalidExpiration = ClaimsWith(StringValue("id-1"), NumberValue("1e100"));

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtReplayValidator.Validate(emptyIdentifier, policy),
                "invalid_jwt_id");
            AssertFailure(
                JwtReplayValidator.Validate(wrongIdentifier, policy),
                "invalid_jwt_id");
            AssertFailure(
                JwtReplayValidator.Validate(invalidText, policy),
                "invalid_jwt_id");
            AssertFailure(
                JwtReplayValidator.Validate(invalidExpiration, policy),
                "invalid_expiration_time");
        });
    }

    [Test]
    public void CallbackFailuresAndInvalidResultsAreReturnedStably()
    {
        JwtClaims claims = JwtClaims.Read(
            "{\"jti\":\"id-1\",\"exp\":2000}").Claims;
        JwtValidationPolicy custom = PolicyAt(1000m);
        custom.ReplayValidator = (identifier, expiration) =>
            Failure("custom_replay_rejection");
        JwtValidationPolicy noDetails = PolicyAt(1000m);
        noDetails.ReplayValidator = (identifier, expiration) => new TokenResult();
        JwtValidationPolicy noResult = PolicyAt(1000m);
        noResult.ReplayValidator = (identifier, expiration) => null;
        JwtValidationPolicy contradictory = PolicyAt(1000m);
        contradictory.ReplayValidator = (identifier, expiration) =>
            ContradictorySuccess();

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtReplayValidator.Validate(claims, custom),
                "custom_replay_rejection");
            AssertFailure(
                JwtReplayValidator.Validate(claims, noDetails),
                "token_replay_detected");
            AssertFailure(
                JwtReplayValidator.Validate(claims, noResult),
                "invalid_replay_validation_result");
            AssertFailure(
                JwtReplayValidator.Validate(claims, contradictory),
                "invalid_replay_validation_result");
        });
    }

    [Test]
    public void SignedEncryptedAndNestedValidationInvokeCallbackOncePerToken()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string signed = JsonWebToken.CreateSigned(
            ReplayClaims("signed"),
            signingKey,
            creationPolicy).Text;
        string encrypted = JsonWebToken.CreateEncrypted(
            ReplayClaims("encrypted"),
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        string nested = JsonWebToken.CreateSignedThenEncrypted(
            ReplayClaims("nested"),
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        JwtValidationPolicy policy = PolicyAt(
            1000m,
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        List<string> identifiers = new List<string>();
        List<DateTimeOffset> expirations = new List<DateTimeOffset>();
        policy.ReplayValidator = (identifier, expiration) =>
        {
            identifiers.Add(identifier);
            expirations.Add(expiration);
            return Success();
        };
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);

        JwtValidationResult signedResult = JsonWebToken.Validate(signed, resolver, policy);
        JwtValidationResult encryptedResult = JsonWebToken.Validate(encrypted, resolver, policy);
        JwtValidationResult nestedResult = JsonWebToken.Validate(nested, resolver, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signedResult.Success, Is.True);
            Assert.That(encryptedResult.Success, Is.True);
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(identifiers, Is.EqualTo(new[] { "signed", "encrypted", "nested" }));
            Assert.That(expirations, Has.All.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(2000)));
        });
    }

    [Test]
    public void CallbackRejectionDoesNotExposeAuthenticatedTokenData()
    {
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            ReplayClaims("replayed"),
            key,
            creationPolicy).Text;
        JwtValidationPolicy policy = PolicyAt(1000m, JoseAlgorithms.HmacSha256);
        policy.ReplayValidator = (identifier, expiration) =>
            Failure("replayed_identifier");

        JwtValidationResult result = JsonWebToken.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        AssertValidationFailure(result, "replayed_identifier");
    }

    [Test]
    public void ReplayCallbackRunsOnlyAfterSignatureAuthentication()
    {
        int calls = 0;
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            ReplayClaims("original"),
            key,
            creationPolicy).Text;
        string[] segments = token.Split('.');
        segments[1] = Base64Url.Encode(
            Encoding.UTF8.GetBytes("{\"jti\":\"tampered\",\"exp\":2000}")).Text;
        string tampered = string.Join('.', segments);
        JwtValidationPolicy policy = PolicyAt(1000m, JoseAlgorithms.HmacSha256);
        policy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult result = JsonWebToken.Validate(
            tampered,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void ReplayValidationRequiresValidPolicyAndArguments()
    {
        JwtValidationPolicy invalidPolicy = PolicyAt(1000m);
        invalidPolicy.Clock = null;
        JwtClaims claims = ReplayClaims("id-1");

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtReplayValidator.Validate(claims, invalidPolicy),
                "invalid_policy");
            Assert.That(
                () => JwtReplayValidator.Validate(null, PolicyAt(1000m)),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtReplayValidator.Validate(claims, null),
                Throws.ArgumentNullException);
        });
    }

    static JwtClaims ReplayClaims(string identifier)
    {
        return JwtClaims.Read(
            "{\"jti\":\"" + identifier + "\",\"exp\":2000}").Claims;
    }

    static JwtClaims ClaimsWith(JsonValue identifier, JsonValue expiration)
    {
        JwtClaims claims = new JwtClaims();
        claims.Set(JwtClaimNames.JwtId, identifier);
        claims.Set(JwtClaimNames.ExpirationTime, expiration);
        return claims;
    }

    static JwtValidationPolicy PolicyAt(decimal seconds, params string[] algorithms)
    {
        JwtValidationPolicy policy = JwtValidationPolicy.Create(
            TokenPolicyFor(algorithms));
        policy.TokenPolicy.ClockSkewSeconds = 0;
        DateTimeOffset instant = JwtNumericDate.ToDateTimeOffset(seconds).DateTime;
        policy.Clock = new TokenClock(() => instant);
        return policy;
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

    static TokenResult Failure(string code)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static TokenResult ContradictorySuccess()
    {
        TokenResult result = Failure("unexpected_success_error");
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
