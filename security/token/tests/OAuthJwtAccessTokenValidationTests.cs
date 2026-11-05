using token;

namespace token.Tests;

public class OAuthJwtAccessTokenValidationTests
{
    [Test]
    public void SignedProfileTokenWithRequiredClaimsIsAccepted()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(AccessTokenClaims(), key);
        JwtValidationPolicy policy = ProfilePolicy(JoseAlgorithms.HmacSha256);

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(
                result.Claims.Get(JwtClaimNames.ClientId).StringValue,
                Is.EqualTo("client-1"));
        });
    }

    [TestCase("{\"sub\":\"subject\",\"aud\":\"api\",\"exp\":2000,\"iat\":900,\"jti\":\"id-1\",\"client_id\":\"client-1\"}", "iss")]
    [TestCase("{\"iss\":\"issuer\",\"aud\":\"api\",\"exp\":2000,\"iat\":900,\"jti\":\"id-1\",\"client_id\":\"client-1\"}", "sub")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"exp\":2000,\"iat\":900,\"jti\":\"id-1\",\"client_id\":\"client-1\"}", "aud")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\",\"iat\":900,\"jti\":\"id-1\",\"client_id\":\"client-1\"}", "exp")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\",\"exp\":2000,\"jti\":\"id-1\",\"client_id\":\"client-1\"}", "iat")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\",\"exp\":2000,\"iat\":900,\"client_id\":\"client-1\"}", "jti")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\",\"exp\":2000,\"iat\":900,\"jti\":\"id-1\"}", "client_id")]
    public void ProfileRequiresEveryMandatoryClaim(string json, string missing)
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(Claims(json), key);

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            ProfilePolicy(JoseAlgorithms.HmacSha256));

        AssertValidationFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void ProfileRequiresAccessTokenType()
    {
        TokenKey key = HmacKey(1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string jwt = JsonWebToken.CreateSigned(
            AccessTokenClaims(),
            key,
            Header("{\"typ\":\"JWT\"}"),
            creationPolicy).Text;
        string idToken = JsonWebToken.CreateSigned(
            AccessTokenClaims(),
            key,
            Header("{\"typ\":\"id_token\"}"),
            creationPolicy).Text;
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));
        JwtValidationPolicy policy = ProfilePolicy(JoseAlgorithms.HmacSha256);

        JwtValidationResult jwtResult = OAuthJwtAccessTokenValidator.Validate(
            jwt,
            resolver,
            policy);
        JwtValidationResult idTokenResult = OAuthJwtAccessTokenValidator.Validate(
            idToken,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(jwtResult, "token_type_mismatch");
            AssertValidationFailure(idTokenResult, "token_type_mismatch");
        });
    }

    [Test]
    public void FullApplicationAccessTokenTypeAliasIsAccepted()
    {
        TokenKey key = HmacKey(1);
        string token = JsonWebToken.CreateSigned(
            AccessTokenClaims(),
            key,
            Header("{\"typ\":\"APPLICATION/AT+JWT\"}"),
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            ProfilePolicy(JoseAlgorithms.HmacSha256));

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ProfileEnforcesExpectedIssuerAudienceAndLifetime()
    {
        TokenKey key = HmacKey(1);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));
        JwtValidationPolicy policy = ProfilePolicy(JoseAlgorithms.HmacSha256);
        string issuerMismatch = SignedToken(
            Claims("{\"iss\":\"other\",\"sub\":\"subject\",\"aud\":\"api\","
                + "\"exp\":2000,\"iat\":900,\"jti\":\"id-1\","
                + "\"client_id\":\"client-1\"}"),
            key);
        string audienceMismatch = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"other\","
                + "\"exp\":2000,\"iat\":900,\"jti\":\"id-1\","
                + "\"client_id\":\"client-1\"}"),
            key);
        string expired = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\","
                + "\"exp\":1000,\"iat\":900,\"jti\":\"id-1\","
                + "\"client_id\":\"client-1\"}"),
            key);
        string futureIssued = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\","
                + "\"exp\":2000,\"iat\":1001,\"jti\":\"id-1\","
                + "\"client_id\":\"client-1\"}"),
            key);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    issuerMismatch,
                    resolver,
                    policy),
                "issuer_mismatch");
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    audienceMismatch,
                    resolver,
                    policy),
                "audience_mismatch");
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(expired, resolver, policy),
                "token_expired");
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    futureIssued,
                    resolver,
                    policy),
                "issued_at_in_future");
        });
    }

    [Test]
    public void ProfileRequiresConfiguredIssuerAndAudience()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(AccessTokenClaims(), key);
        JwtValidationPolicy missingIssuer = Policy(JoseAlgorithms.HmacSha256);
        missingIssuer.ValidAudiences = new[] { "api" };
        JwtValidationPolicy missingAudience = Policy(JoseAlgorithms.HmacSha256);
        missingAudience.ValidIssuers = new[] { "issuer" };
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    token,
                    resolver,
                    missingIssuer),
                "invalid_oauth_access_token_policy");
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    token,
                    resolver,
                    missingAudience),
                "invalid_oauth_access_token_policy");
        });
    }

    [Test]
    public void DirectEncryptedTokenIsRejectedWithoutInvokingReplayCallback()
    {
        int calls = 0;
        TokenKey key = DirectKey(90);
        string token = JsonWebToken.CreateEncrypted(
            AccessTokenClaims(),
            key,
            Header("{\"enc\":\"A128GCM\",\"typ\":\"at+jwt\"}"),
            TokenPolicyFor(
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm)).Text;
        JwtValidationPolicy policy = ProfilePolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                result,
                "oauth_access_token_signature_required");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void NestedSignedAndEncryptedProfileTokenIsAcceptedAndReplayedOnce()
    {
        int calls = 0;
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JwtCreator.CreateSignedThenEncrypted(
            AccessTokenClaims(),
            signingKey,
            encryptionKey,
            Header("{\"typ\":\"at+jwt\"}"),
            Header("{\"enc\":\"A128GCM\"}"),
            creationPolicy).Text;
        JwtValidationPolicy policy = ProfilePolicy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            token,
            Resolver(signingKey, encryptionKey),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProfileMergesCallerRequiredClaimsWithoutMutatingPolicy()
    {
        TokenKey key = HmacKey(1);
        JwtValidationPolicy policy = ProfilePolicy(JoseAlgorithms.HmacSha256);
        policy.ValidateIssuer = false;
        policy.ValidateAudience = false;
        policy.ValidateTokenType = false;
        policy.RequireExpirationTime = false;
        policy.TokenPolicy.RequiredClaims = new[] { "tenant" };
        string missing = SignedToken(AccessTokenClaims(), key);
        JwtClaims completeClaims = Claims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\","
            + "\"exp\":2000,\"iat\":900,\"jti\":\"id-1\","
            + "\"client_id\":\"client-1\",\"tenant\":\"one\"}");
        string complete = SignedToken(completeClaims, key);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        JwtValidationResult missingResult = OAuthJwtAccessTokenValidator.Validate(
            missing,
            resolver,
            policy);
        JwtValidationResult completeResult = OAuthJwtAccessTokenValidator.Validate(
            complete,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(missingResult, "missing_required_claim");
            Assert.That(completeResult.Success, Is.True);
            Assert.That(policy.ValidateIssuer, Is.False);
            Assert.That(policy.ValidateAudience, Is.False);
            Assert.That(policy.ValidateTokenType, Is.False);
            Assert.That(policy.RequireExpirationTime, Is.False);
            Assert.That(policy.TokenPolicy.RequiredClaims, Is.EqualTo(new[] { "tenant" }));
        });
    }

    [Test]
    public void ProfileReturnsReplayFailureAfterAllProfileChecks()
    {
        TokenKey key = HmacKey(1);
        JwtValidationPolicy policy = ProfilePolicy(JoseAlgorithms.HmacSha256);
        policy.ReplayValidator = (identifier, expiration) =>
            Failure("replayed_access_token");

        JwtValidationResult result = OAuthJwtAccessTokenValidator.Validate(
            SignedToken(AccessTokenClaims(), key),
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        AssertValidationFailure(result, "replayed_access_token");
    }

    [Test]
    public void ProfileValidationRequiresValidArgumentsAndPolicy()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(AccessTokenClaims(), key);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));
        JwtValidationPolicy invalidPolicy = ProfilePolicy(
            JoseAlgorithms.HmacSha256);
        invalidPolicy.Clock = null;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OAuthJwtAccessTokenValidator.Validate(
                    token,
                    resolver,
                    invalidPolicy),
                "invalid_policy");
            Assert.That(
                () => OAuthJwtAccessTokenValidator.Validate(
                    null,
                    resolver,
                    ProfilePolicy(JoseAlgorithms.HmacSha256)),
                Throws.ArgumentNullException);
            Assert.That(
                () => OAuthJwtAccessTokenValidator.Validate(
                    token,
                    null,
                    ProfilePolicy(JoseAlgorithms.HmacSha256)),
                Throws.ArgumentNullException);
            Assert.That(
                () => OAuthJwtAccessTokenValidator.Validate(
                    token,
                    resolver,
                    null),
                Throws.ArgumentNullException);
        });
    }

    static JwtClaims AccessTokenClaims()
    {
        return Claims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"api\","
            + "\"exp\":2000,\"iat\":900,\"jti\":\"id-1\","
            + "\"client_id\":\"client-1\",\"scope\":\"read write\"}");
    }

    static JwtClaims Claims(string json)
    {
        return JwtClaims.Read(json).Claims;
    }

    static string SignedToken(JwtClaims claims, TokenKey key)
    {
        return JsonWebToken.CreateSigned(
            claims,
            key,
            Header("{\"typ\":\"at+jwt\"}"),
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
    }

    static JoseHeader Header(string json)
    {
        return JoseHeader.Read(json).Header;
    }

    static JwtValidationPolicy ProfilePolicy(params string[] algorithms)
    {
        JwtValidationPolicy policy = Policy(algorithms);
        policy.TokenPolicy.ClockSkewSeconds = 0;
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.ValidIssuers = new[] { "issuer" };
        policy.ValidAudiences = new[] { "api" };
        return policy;
    }

    static JwtValidationPolicy Policy(params string[] algorithms)
    {
        return JwtValidationPolicy.Create(TokenPolicyFor(algorithms));
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
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
