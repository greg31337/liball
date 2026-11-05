using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class OpenIdConnectIdTokenValidationTests
{
    [Test]
    public void SignedIdTokenWithRequiredClaimsIsAccepted()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(RequiredClaims(), key, "JWT");

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.TokenType, Is.EqualTo("JWT"));
            Assert.That(
                result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("subject"));
        });
    }

    [TestCase("{\"sub\":\"subject\",\"aud\":\"client-1\",\"exp\":2000,\"iat\":900}", "iss")]
    [TestCase("{\"iss\":\"issuer\",\"aud\":\"client-1\",\"exp\":2000,\"iat\":900}", "sub")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"exp\":2000,\"iat\":900}", "aud")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"client-1\",\"iat\":900}", "exp")]
    [TestCase("{\"iss\":\"issuer\",\"sub\":\"subject\",\"aud\":\"client-1\",\"exp\":2000}", "iat")]
    public void IdTokenRequiresCoreClaims(string json, string missing)
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(Claims(json), key, "JWT");

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            Policy());

        AssertValidationFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void IdTokenEnforcesIssuerClientAudienceAndLifetime()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        TokenKeyResolver resolver = Resolver(key);
        string issuer = SignedToken(
            Claims("{\"iss\":\"other\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string audience = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"other\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string expired = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":1000,\"iat\":900}"),
            key,
            "JWT");
        string futureIssued = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":1001}"),
            key,
            "JWT");

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(issuer, resolver, policy),
                "issuer_mismatch");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(audience, resolver, policy),
                "audience_mismatch");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(expired, resolver, policy),
                "token_expired");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    futureIssued,
                    resolver,
                    policy),
                "issued_at_in_future");
        });
    }

    [Test]
    public void MultipleAudiencesRequireMatchingAuthorizedPartyAndTrust()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.ValidAudiences = new[] { "trusted-api" };
        TokenKeyResolver resolver = Resolver(key);
        string accepted = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":[\"client-1\",\"trusted-api\"],"
                + "\"azp\":\"client-1\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string missingAzp = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":[\"client-1\",\"trusted-api\"],"
                + "\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string wrongAzp = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":[\"client-1\",\"trusted-api\"],"
                + "\"azp\":\"other\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string untrusted = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":[\"client-1\",\"unknown\"],"
                + "\"azp\":\"client-1\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");
        string clientMissing = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"trusted-api\",\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");

        Assert.Multiple(() =>
        {
            Assert.That(
                OpenIdConnectIdTokenValidator.Validate(
                    accepted,
                    resolver,
                    policy).Success,
                Is.True);
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    missingAzp,
                    resolver,
                    policy),
                "missing_authorized_party");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    wrongAzp,
                    resolver,
                    policy),
                "authorized_party_mismatch");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    untrusted,
                    resolver,
                    policy),
                "untrusted_openid_id_token_audience");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    clientMissing,
                    resolver,
                    policy),
                "openid_id_token_audience_mismatch");
        });
    }

    [Test]
    public void AuthorizedPartyIsValidatedForSingleAudienceWhenPresent()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"azp\":\"other\","
                + "\"exp\":2000,\"iat\":900}"),
            key,
            "JWT");

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            Policy());

        AssertValidationFailure(result, "authorized_party_mismatch");
    }

    [Test]
    public void ExpectedNonceIsRequiredAndComparedExactly()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.Nonce = "nonce-é😀";
        string accepted = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900,"
                + "\"nonce\":\"nonce-é😀\"}"),
            key,
            "JWT");
        string missing = SignedToken(RequiredClaims(), key, "JWT");
        string mismatch = SignedToken(
            Claims("{\"iss\":\"issuer\",\"sub\":\"subject\","
                + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900,"
                + "\"nonce\":\"Nonce-é😀\"}"),
            key,
            "JWT");

        Assert.Multiple(() =>
        {
            Assert.That(
                OpenIdConnectIdTokenValidator.Validate(
                    accepted,
                    Resolver(key),
                    policy).Success,
                Is.True);
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    missing,
                    Resolver(key),
                    policy),
                "missing_nonce");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    mismatch,
                    Resolver(key),
                    policy),
                "nonce_mismatch");
        });
    }

    [Test]
    public void MaximumAuthenticationAgeRequiresAndValidatesAuthenticationTime()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.MaximumAuthenticationAgeSeconds = 100;
        string missing = SignedToken(RequiredClaims(), key, "JWT");
        string boundary = SignedToken(
            ClaimsWith("\"auth_time\":900"),
            key,
            "JWT");
        string old = SignedToken(
            ClaimsWith("\"auth_time\":899.9999999"),
            key,
            "JWT");
        string future = SignedToken(
            ClaimsWith("\"auth_time\":1000.0000001"),
            key,
            "JWT");

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    missing,
                    Resolver(key),
                    policy),
                "missing_authentication_time");
            Assert.That(
                OpenIdConnectIdTokenValidator.Validate(
                    boundary,
                    Resolver(key),
                    policy).Success,
                Is.True);
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    old,
                    Resolver(key),
                    policy),
                "authentication_too_old");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    future,
                    Resolver(key),
                    policy),
                "authentication_time_in_future");
        });
    }

    [Test]
    public void AuthenticationAgeHonorsClockSkew()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.MaximumAuthenticationAgeSeconds = 100;
        policy.JwtPolicy.TokenPolicy.ClockSkewSeconds = 5;
        string oldBoundary = SignedToken(
            ClaimsWith("\"auth_time\":895"),
            key,
            "JWT");
        string futureBoundary = SignedToken(
            ClaimsWith("\"auth_time\":1005"),
            key,
            "JWT");

        Assert.Multiple(() =>
        {
            Assert.That(
                OpenIdConnectIdTokenValidator.Validate(
                    oldBoundary,
                    Resolver(key),
                    policy).Success,
                Is.True);
            Assert.That(
                OpenIdConnectIdTokenValidator.Validate(
                    futureBoundary,
                    Resolver(key),
                    policy).Success,
                Is.True);
        });
    }

    [Test]
    public void AccessTokenAndCodeHashesAreValidated()
    {
        TokenKey key = HmacKey(1);
        string accessToken = "access-token";
        string code = "code-1";
        JwtClaims claims = ClaimsWith(
            "\"at_hash\":\"" + HalfHash(accessToken) + "\","
            + "\"c_hash\":\"" + HalfHash(code) + "\"");
        string token = SignedToken(claims, key, "id_token");
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.AccessToken = accessToken;
        policy.AuthorizationCode = code;

        JwtValidationResult accepted = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            policy);
        policy.AccessToken = "different";
        JwtValidationResult mismatch = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            AssertValidationFailure(mismatch, "openid_token_hash_mismatch");
        });
    }

    [Test]
    public void RequestedTokenHashClaimsAreRequired()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy accessPolicy = Policy();
        accessPolicy.AccessToken = "access-token";
        OpenIdConnectIdTokenValidationPolicy codePolicy = Policy();
        codePolicy.AuthorizationCode = "code-1";
        string token = SignedToken(RequiredClaims(), key, "JWT");

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    token,
                    Resolver(key),
                    accessPolicy),
                "missing_openid_token_hash");
            AssertValidationFailure(
                OpenIdConnectIdTokenValidator.Validate(
                    token,
                    Resolver(key),
                    codePolicy),
                "missing_openid_token_hash");
        });
    }

    [TestCase(null, true)]
    [TestCase("JWT", true)]
    [TestCase("application/jwt", true)]
    [TestCase("id_token", true)]
    [TestCase("at+jwt", false)]
    public void IdTokenTypeIsOptionalButRejectsCrossProfileTypes(
        string? type,
        bool success)
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(RequiredClaims(), key, type);

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(key),
            Policy());

        if (success)
        {
            Assert.That(result.Success, Is.True);
        }
        else
        {
            AssertValidationFailure(result, "openid_id_token_type_mismatch");
        }
    }

    [Test]
    public void DirectEncryptedIdTokenIsRejectedBeforeReplayCallback()
    {
        int calls = 0;
        TokenKey key = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        JwtClaims claims = ClaimsWith("\"jti\":\"id-1\"");
        string token = JsonWebToken.CreateEncrypted(
            claims,
            key,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        OpenIdConnectIdTokenValidationPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(result, "openid_id_token_signature_required");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void NestedSignedEncryptedIdTokenIsAccepted()
    {
        TokenKey signingKey = HmacKey(1);
        TokenKey encryptionKey = DirectKey(90);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JwtCreator.CreateSignedThenEncrypted(
            RequiredClaims(),
            signingKey,
            encryptionKey,
            Header("{\"typ\":\"id_token\"}"),
            Header("{\"enc\":\"A128GCM\"}"),
            creationPolicy).Text;

        JwtValidationResult result = OpenIdConnectIdTokenValidator.Validate(
            token,
            Resolver(signingKey, encryptionKey),
            Policy(
                JoseAlgorithms.HmacSha256,
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
        });
    }

    [Test]
    public void ReplayCallbackRunsOnlyAfterIdTokenProfileChecks()
    {
        int calls = 0;
        TokenKey key = HmacKey(1);
        JwtClaims claims = ClaimsWith("\"jti\":\"id-1\"");
        string accepted = SignedToken(claims, key, "JWT");
        string rejected = SignedToken(claims, key, "at+jwt");
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };

        JwtValidationResult rejectedResult = OpenIdConnectIdTokenValidator.Validate(
            rejected,
            Resolver(key),
            policy);
        JwtValidationResult acceptedResult = OpenIdConnectIdTokenValidator.Validate(
            accepted,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                rejectedResult,
                "openid_id_token_type_mismatch");
            Assert.That(acceptedResult.Success, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProfileMergesRequiredClaimsWithoutMutatingCallerPolicy()
    {
        TokenKey key = HmacKey(1);
        OpenIdConnectIdTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.TokenPolicy.RequiredClaims = new[] { "tenant" };
        string missing = SignedToken(RequiredClaims(), key, "JWT");
        string complete = SignedToken(
            ClaimsWith("\"tenant\":\"one\""),
            key,
            "JWT");

        JwtValidationResult missingResult = OpenIdConnectIdTokenValidator.Validate(
            missing,
            Resolver(key),
            policy);
        JwtValidationResult completeResult = OpenIdConnectIdTokenValidator.Validate(
            complete,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(missingResult, "missing_required_claim");
            Assert.That(completeResult.Success, Is.True);
            Assert.That(
                policy.JwtPolicy.TokenPolicy.RequiredClaims,
                Is.EqualTo(new[] { "tenant" }));
            Assert.That(policy.JwtPolicy.ValidateIssuer, Is.False);
            Assert.That(policy.JwtPolicy.ValidateAudience, Is.False);
        });
    }

    [Test]
    public void IdTokenPolicyRequiresClientIssuerAndValidRequestContext()
    {
        OpenIdConnectIdTokenValidationPolicy missingClient = Policy();
        missingClient.ClientId = null;
        OpenIdConnectIdTokenValidationPolicy missingIssuer = Policy();
        missingIssuer.JwtPolicy.ValidIssuers = Array.Empty<string>();
        OpenIdConnectIdTokenValidationPolicy invalidAge = Policy();
        invalidAge.MaximumAuthenticationAgeSeconds = -1;
        OpenIdConnectIdTokenValidationPolicy invalidNonce = Policy();
        invalidNonce.Nonce = "";
        OpenIdConnectIdTokenValidationPolicy invalidArtifact = Policy();
        invalidArtifact.AccessToken = "é";

        Assert.Multiple(() =>
        {
            AssertFailure(missingClient.Validate(), "invalid_openid_id_token_policy");
            AssertFailure(missingIssuer.Validate(), "invalid_openid_id_token_policy");
            AssertFailure(invalidAge.Validate(), "invalid_openid_id_token_policy");
            AssertFailure(invalidNonce.Validate(), "invalid_openid_id_token_policy");
            AssertFailure(invalidArtifact.Validate(), "invalid_openid_id_token_policy");
        });
    }

    [Test]
    public void IdTokenValidationRequiresValidArguments()
    {
        TokenKey key = HmacKey(1);
        string token = SignedToken(RequiredClaims(), key, "JWT");
        TokenKeyResolver resolver = Resolver(key);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => OpenIdConnectIdTokenValidator.Validate(
                    null,
                    resolver,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => OpenIdConnectIdTokenValidator.Validate(
                    token,
                    null,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => OpenIdConnectIdTokenValidator.Validate(
                    token,
                    resolver,
                    null),
                Throws.ArgumentNullException);
        });
    }

    static JwtClaims RequiredClaims()
    {
        return Claims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900}");
    }

    static JwtClaims ClaimsWith(string members)
    {
        return Claims(
            "{\"iss\":\"issuer\",\"sub\":\"subject\","
            + "\"aud\":\"client-1\",\"exp\":2000,\"iat\":900,"
            + members + "}");
    }

    static JwtClaims Claims(string json)
    {
        return JwtClaims.Read(json).Claims;
    }

    static string SignedToken(JwtClaims claims, TokenKey key, string? type)
    {
        if (type == null)
        {
            JoseHeader untyped = Header("{\"alg\":\"HS256\"}");
            return JsonWebSignature.Sign(
                Encoding.UTF8.GetBytes(claims.Write().Text),
                untyped,
                key,
                TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
        }

        JoseHeader header = Header("{\"typ\":\"" + type + "\"}");
        return JsonWebToken.CreateSigned(
            claims,
            key,
            header,
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
    }

    static OpenIdConnectIdTokenValidationPolicy Policy(
        params string[] algorithms)
    {
        string[] allowed = algorithms.Length == 0
            ? new[] { JoseAlgorithms.HmacSha256 }
            : algorithms;
        OpenIdConnectIdTokenValidationPolicy policy =
            new OpenIdConnectIdTokenValidationPolicy();
        policy.JwtPolicy = JwtValidationPolicy.Create(TokenPolicyFor(allowed));
        policy.JwtPolicy.TokenPolicy.ClockSkewSeconds = 0;
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.JwtPolicy.ValidIssuers = new[] { "issuer" };
        policy.ClientId = "client-1";
        return policy;
    }

    static string HalfHash(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.ASCII.GetBytes(value));
        return Base64Url.Encode(digest[..(digest.Length / 2)]).Text;
    }

    static JoseHeader Header(string json)
    {
        return JoseHeader.Read(json).Header;
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

    static TokenKeyResolver Resolver(TokenKey signingKey)
    {
        return new TokenKeyResolver(context => KeySuccess(signingKey));
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
