using System.Text;
using token;

namespace token.Tests;

public class ClientAssertionJwtValidationTests
{
    [Test]
    public void SignedAssertionAuthenticatesClientAndAuthorizationServer()
    {
        TokenKey key = HmacKey(1);
        JwtValidationResult result = ClientAssertionJwtValidator.Validate(
            Signed(ValidClaims(), key),
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.Claims.Get(JwtClaimNames.Issuer).StringValue,
                Is.EqualTo("client-1"));
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("client-1"));
        });
    }

    [TestCase("{\"sub\":\"client-1\",\"aud\":\"https://as.example/token\",\"exp\":1200}", "iss")]
    [TestCase("{\"iss\":\"client-1\",\"aud\":\"https://as.example/token\",\"exp\":1200}", "sub")]
    [TestCase("{\"iss\":\"client-1\",\"sub\":\"client-1\",\"exp\":1200}", "aud")]
    [TestCase("{\"iss\":\"client-1\",\"sub\":\"client-1\",\"aud\":\"https://as.example/token\"}", "exp")]
    public void AssertionRequiresRfc7523Claims(string claims, string missing)
    {
        TokenKey key = HmacKey(1);
        JwtValidationResult result = ClientAssertionJwtValidator.Validate(
            Signed(claims, key),
            Resolver(key),
            Policy());

        AssertFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void AssertionBindsIssuerSubjectAndAudience()
    {
        TokenKey key = HmacKey(1);
        string issuer = Signed(
            Claims("other", "client-1", "https://as.example/token", 1200),
            key);
        string subject = Signed(
            Claims("client-1", "other", "https://as.example/token", 1200),
            key);
        string audience = Signed(
            Claims("client-1", "client-1", "https://other.example", 1200),
            key);

        Assert.Multiple(() =>
        {
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    issuer,
                    Resolver(key),
                    Policy()),
                "issuer_mismatch");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    subject,
                    Resolver(key),
                    Policy()),
                "client_assertion_subject_mismatch");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    audience,
                    Resolver(key),
                    Policy()),
                "audience_mismatch");
        });
    }

    [Test]
    public void AudienceArrayMayIdentifyAuthorizationServer()
    {
        TokenKey key = HmacKey(1);
        string claims = "{\"iss\":\"client-1\",\"sub\":\"client-1\","
            + "\"aud\":[\"https://other.example\",\"https://as.example/token\"],"
            + "\"exp\":1200}";

        Assert.That(
            ClientAssertionJwtValidator.Validate(
                Signed(claims, key),
                Resolver(key),
                Policy()).Success,
            Is.True);
    }

    [Test]
    public void AssertionEnforcesTimeWindowAndMaximumLifetime()
    {
        TokenKey key = HmacKey(1);
        string expired = Signed(
            Claims("client-1", "client-1", "https://as.example/token", 1000),
            key);
        string futureIssued = Signed(
            ValidClaims("\"iat\":1001,"),
            key);
        string notYetValid = Signed(
            ValidClaims("\"nbf\":1001,"),
            key);
        string tooLong = Signed(
            Claims("client-1", "client-1", "https://as.example/token", 1301),
            key);

        Assert.Multiple(() =>
        {
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    expired,
                    Resolver(key),
                    Policy()),
                "token_expired");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    futureIssued,
                    Resolver(key),
                    Policy()),
                "issued_at_in_future");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    notYetValid,
                    Resolver(key),
                    Policy()),
                "token_not_yet_valid");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    tooLong,
                    Resolver(key),
                    Policy()),
                "client_assertion_lifetime_too_long");
        });
    }

    [Test]
    public void JwtIdIsOptionalUnlessProfileRequiresIt()
    {
        TokenKey key = HmacKey(1);
        string noIdentifier = Signed(ValidClaims(), key);
        string emptyIdentifier = Signed(ValidClaims("\"jti\":\"\","), key);
        ClientAssertionJwtValidationPolicy required = Policy();
        required.RequireJwtId = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                ClientAssertionJwtValidator.Validate(
                    noIdentifier,
                    Resolver(key),
                    Policy()).Success,
                Is.True);
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    noIdentifier,
                    Resolver(key),
                    required),
                "missing_required_claim");
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    emptyIdentifier,
                    Resolver(key),
                    Policy()),
                "invalid_client_assertion_jwt_id");
        });
    }

    [Test]
    public void ReplayValidationRunsAfterAssertionProfileChecks()
    {
        TokenKey key = HmacKey(1);
        List<string> identifiers = new List<string>();
        List<DateTimeOffset> expirations = new List<DateTimeOffset>();
        ClientAssertionJwtValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            identifiers.Add(identifier);
            expirations.Add(expiration);
            return Success();
        };
        string valid = Signed(ValidClaims("\"jti\":\"assertion-1\","), key);
        string wrongSubject = Signed(
            "{\"iss\":\"client-1\",\"sub\":\"other\","
                + "\"aud\":\"https://as.example/token\",\"exp\":1200,"
                + "\"jti\":\"assertion-2\"}",
            key);

        JwtValidationResult accepted = ClientAssertionJwtValidator.Validate(
            valid,
            Resolver(key),
            policy);
        JwtValidationResult rejected = ClientAssertionJwtValidator.Validate(
            wrongSubject,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(identifiers, Is.EqualTo(new[] { "assertion-1" }));
            Assert.That(expirations, Is.EqualTo(new[]
            {
                DateTimeOffset.UnixEpoch.AddSeconds(1200)
            }));
            AssertFailure(rejected, "client_assertion_subject_mismatch");
        });
    }

    [Test]
    public void ReplayRejectionDoesNotExposeAuthenticatedClaims()
    {
        TokenKey key = HmacKey(1);
        ClientAssertionJwtValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
            new TokenResult();

        AssertFailure(
            ClientAssertionJwtValidator.Validate(
                Signed(ValidClaims("\"jti\":\"assertion-1\","), key),
                Resolver(key),
                policy),
            "token_replay_detected");
    }

    [Test]
    public void AssertionMustBeSignedButMayBeNestedAndEncrypted()
    {
        TokenKey signing = HmacKey(1);
        TokenKey encryption = DirectKey(90);
        TokenPolicy algorithms = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string encrypted = JsonWebToken.CreateEncrypted(
            JwtClaims.Read(ValidClaims()).Claims,
            encryption,
            JoseAlgorithms.Aes128Gcm,
            algorithms).Text;
        string nested = JwtCreator.CreateSignedThenEncrypted(
            JwtClaims.Read(ValidClaims()).Claims,
            signing,
            encryption,
            JoseHeader.Read("{\"alg\":\"HS256\"}").Header,
            JoseHeader.Read("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header,
            algorithms).Text;
        TokenKeyResolver resolver = Resolver(signing, encryption);
        ClientAssertionJwtValidationPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        JwtValidationResult nestedResult = ClientAssertionJwtValidator.Validate(
            nested,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(
                ClientAssertionJwtValidator.Validate(
                    encrypted,
                    resolver,
                    policy),
                "client_assertion_signature_required");
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Nested, Is.True);
            Assert.That(nestedResult.SignatureValidated, Is.True);
            Assert.That(nestedResult.EncryptionValidated, Is.True);
        });
    }

    [Test]
    public void PolicyRequiresClientAudienceAndBoundedLifetime()
    {
        ClientAssertionJwtValidationPolicy client = Policy();
        client.ClientId = null;
        ClientAssertionJwtValidationPolicy audience = Policy();
        audience.AuthorizationServerAudience = string.Empty;
        ClientAssertionJwtValidationPolicy lifetime = Policy();
        lifetime.MaximumAssertionLifetimeSeconds = 0;
        ClientAssertionJwtValidationPolicy excessive = Policy();
        excessive.MaximumAssertionLifetimeSeconds = 86401;

        Assert.Multiple(() =>
        {
            AssertPolicyFailure(client);
            AssertPolicyFailure(audience);
            AssertPolicyFailure(lifetime);
            AssertPolicyFailure(excessive);
        });
    }

    [Test]
    public void ValidatorRequiresArguments()
    {
        TokenKey key = HmacKey(1);
        string assertion = Signed(ValidClaims(), key);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ClientAssertionJwtValidator.Validate(
                    null,
                    Resolver(key),
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => ClientAssertionJwtValidator.Validate(
                    assertion,
                    null,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => ClientAssertionJwtValidator.Validate(
                    assertion,
                    Resolver(key),
                    null),
                Throws.ArgumentNullException);
        });
    }

    static string ValidClaims()
    {
        return Claims(
            "client-1",
            "client-1",
            "https://as.example/token",
            1200);
    }

    static string ValidClaims(string prefix)
    {
        return "{" + prefix + "\"iss\":\"client-1\","
            + "\"sub\":\"client-1\","
            + "\"aud\":\"https://as.example/token\",\"exp\":1200}";
    }

    static string Claims(
        string issuer,
        string subject,
        string audience,
        long expiration)
    {
        return "{\"iss\":\"" + issuer + "\",\"sub\":\"" + subject
            + "\",\"aud\":\"" + audience + "\",\"exp\":"
            + expiration + "}";
    }

    static string Signed(string claims, TokenKey key)
    {
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(claims),
            JoseHeader.Read("{\"alg\":\"HS256\"}").Header,
            key,
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
    }

    static ClientAssertionJwtValidationPolicy Policy(
        params string[] algorithms)
    {
        string[] allowed = algorithms.Length == 0
            ? new[] { JoseAlgorithms.HmacSha256 }
            : algorithms;
        TokenPolicy tokenPolicy = TokenPolicyFor(allowed);
        tokenPolicy.ClockSkewSeconds = 0;
        ClientAssertionJwtValidationPolicy policy =
            new ClientAssertionJwtValidationPolicy();
        policy.JwtPolicy = JwtValidationPolicy.Create(tokenPolicy);
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1000));
        policy.ClientId = "client-1";
        policy.AuthorizationServerAudience = "https://as.example/token";
        return policy;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKey HmacKey(int start)
    {
        return SymmetricKeyImporterOperations.ImportJose(
            Enumerable.Range(start, 32).Select(value => (byte)value).ToArray(),
            JoseAlgorithms.HmacSha256).Key;
    }

    static TokenKey DirectKey(int start)
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(
            Enumerable.Range(start, 16).Select(value => (byte)value).ToArray()).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        return key;
    }

    static TokenKeyResolver Resolver(TokenKey signing)
    {
        return new TokenKeyResolver(context => KeySuccess(signing));
    }

    static TokenKeyResolver Resolver(TokenKey signing, TokenKey encryption)
    {
        return new TokenKeyResolver(context =>
            context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryption)
                : KeySuccess(signing));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static void AssertFailure(JwtValidationResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Claims, Is.Null);
        });
    }

    static void AssertPolicyFailure(ClientAssertionJwtValidationPolicy policy)
    {
        TokenResult result = policy.Validate();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code,
                Is.EqualTo("invalid_client_assertion_policy"));
        });
    }
}
