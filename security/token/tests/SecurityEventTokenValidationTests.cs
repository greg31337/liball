using System.Text;
using token;

namespace token.Tests;

public class SecurityEventTokenValidationTests
{
    const string EventType = "https://schemas.example/events/account-disabled";

    [Test]
    public void SignedSecurityEventTokenAuthenticatesEnvelopeAndEvents()
    {
        TokenKey key = HmacKey(1);
        string token = Signed(
            CompleteClaims(
                "\"toe\":950,\"txn\":\"transaction-1\","
                    + "\"events\":{\"" + EventType
                    + "\":{\"reason\":\"fraud\"}}"),
            key,
            "secevent+jwt");

        JwtValidationResult result = SecurityEventTokenValidator.Validate(
            token,
            Resolver(key),
            Policy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.TokenType, Is.EqualTo("secevent+jwt"));
            Assert.That(result.Claims.Get(JwtClaimNames.TransactionId).StringValue,
                Is.EqualTo("transaction-1"));
            JsonObject events = result.Claims.Get(
                JwtClaimNames.SecurityEvents).ObjectValue;
            Assert.That(events.Members, Has.Length.EqualTo(1));
            Assert.That(events.Get(EventType).ObjectValue.Get("reason").StringValue,
                Is.EqualTo("fraud"));
        });
    }

    [TestCase("{\"iat\":900,\"jti\":\"set-1\",\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}", "iss")]
    [TestCase("{\"iss\":\"issuer\",\"jti\":\"set-1\",\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}", "iat")]
    [TestCase("{\"iss\":\"issuer\",\"iat\":900,\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}", "jti")]
    [TestCase("{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\",\"events\":{\"https://example/event\":{}}}", "aud")]
    [TestCase("{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\",\"aud\":\"feed-1\"}", "events")]
    public void SecurityEventTokenRequiresProfileClaims(
        string claims,
        string missing)
    {
        TokenKey key = HmacKey(1);
        JwtValidationResult result = SecurityEventTokenValidator.Validate(
            Signed(claims, key, null),
            Resolver(key),
            Policy());

        AssertFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void AudienceMayBeOptionalButPresentValuesRemainValidated()
    {
        TokenKey key = HmacKey(1);
        SecurityEventTokenValidationPolicy optional = Policy();
        optional.RequireAudience = false;
        string missing = Signed(
            "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
                + "\"events\":{\"https://example/event\":{}}}",
            key,
            null);
        string wrong = Signed(
            "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
                + "\"aud\":\"other\",\"events\":{\"https://example/event\":{}}}",
            key,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(
                SecurityEventTokenValidator.Validate(
                    missing,
                    Resolver(key),
                    optional).Success,
                Is.True);
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    wrong,
                    Resolver(key),
                    Policy()),
                "audience_mismatch");
        });
    }

    [Test]
    public void IssuerAudienceAndIssuedAtAreValidated()
    {
        TokenKey key = HmacKey(1);
        string issuer = Signed(
            "{\"iss\":\"other\",\"iat\":900,\"jti\":\"set-1\","
                + "\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}",
            key,
            null);
        string future = Signed(
            "{\"iss\":\"issuer\",\"iat\":1101,\"jti\":\"set-1\","
                + "\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}",
            key,
            null);

        Assert.Multiple(() =>
        {
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    issuer,
                    Resolver(key),
                    Policy()),
                "issuer_mismatch");
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    future,
                    Resolver(key),
                    Policy()),
                "issued_at_in_future");
        });
    }

    [Test]
    public void ExplicitTypeIsOptionalButValidatedWhenPresent()
    {
        TokenKey key = HmacKey(1);
        string untyped = Signed(ValidClaims(), key, null);
        string full = Signed(
            ValidClaims(),
            key,
            "APPLICATION/SECEVENT+JWT");
        string wrong = Signed(ValidClaims(), key, "JWT");
        SecurityEventTokenValidationPolicy required = Policy();
        required.RequireExplicitType = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                SecurityEventTokenValidator.Validate(
                    untyped,
                    Resolver(key),
                    Policy()).Success,
                Is.True);
            Assert.That(
                SecurityEventTokenValidator.Validate(
                    full,
                    Resolver(key),
                    Policy()).Success,
                Is.True);
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    wrong,
                    Resolver(key),
                    Policy()),
                "invalid_security_event_type");
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    untyped,
                    Resolver(key),
                    required),
                "missing_security_event_type");
        });
    }

    [TestCase("{}", "invalid_security_events")]
    [TestCase("{\"not a uri\":{}}", "invalid_security_event")]
    [TestCase("{\"https://example/event\":true}", "invalid_security_event")]
    public void EventsClaimRequiresUriKeysAndObjectPayloads(
        string events,
        string code)
    {
        TokenKey key = HmacKey(1);
        string claims = "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
            + "\"aud\":\"feed-1\",\"events\":" + events + "}";

        AssertFailure(
            SecurityEventTokenValidator.Validate(
                Signed(claims, key, null),
                Resolver(key),
                Policy()),
            code);
    }

    [Test]
    public void AllowedEventTypesAndPayloadCallbackAreEnforced()
    {
        TokenKey key = HmacKey(1);
        string acceptedToken = Signed(
            ValidClaims(EventType, "{\"subject\":\"user-1\"}"),
            key,
            null);
        string unknownToken = Signed(
            ValidClaims("https://schemas.example/events/unknown", "{}"),
            key,
            null);
        SecurityEventTokenValidationPolicy policy = Policy();
        policy.ValidEventTypes = new[] { EventType };
        string observedType = null;
        string observedSubject = null;
        policy.EventValidator = (eventType, payload) =>
        {
            observedType = eventType;
            observedSubject = payload.Get("subject").StringValue;
            return Success();
        };

        JwtValidationResult accepted = SecurityEventTokenValidator.Validate(
            acceptedToken,
            Resolver(key),
            policy);
        JwtValidationResult unknown = SecurityEventTokenValidator.Validate(
            unknownToken,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(observedType, Is.EqualTo(EventType));
            Assert.That(observedSubject, Is.EqualTo("user-1"));
            AssertFailure(unknown, "unsupported_security_event");
        });
    }

    [Test]
    public void EventValidatorFailuresAreHandledStably()
    {
        TokenKey key = HmacKey(1);
        string token = Signed(ValidClaims(), key, null);
        SecurityEventTokenValidationPolicy custom = Policy();
        custom.EventValidator = (eventType, payload) =>
            TokenFailure("invalid_profile_event");
        SecurityEventTokenValidationPolicy noDetails = Policy();
        noDetails.EventValidator = (eventType, payload) => new TokenResult();
        SecurityEventTokenValidationPolicy noResult = Policy();
        noResult.EventValidator = (eventType, payload) => null;
        SecurityEventTokenValidationPolicy contradictory = Policy();
        contradictory.EventValidator = (eventType, payload) =>
            ContradictorySuccess();

        Assert.Multiple(() =>
        {
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    token,
                    Resolver(key),
                    custom),
                "invalid_profile_event");
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    token,
                    Resolver(key),
                    noDetails),
                "security_event_rejected");
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    token,
                    Resolver(key),
                    noResult),
                "invalid_security_event_validation_result");
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    token,
                    Resolver(key),
                    contradictory),
                "invalid_security_event_validation_result");
        });
    }

    [TestCase("\"toe\":\"yesterday\",", "invalid_claim_type")]
    [TestCase("\"txn\":1,", "invalid_claim_type")]
    [TestCase("\"jti\":\"\",", "invalid_security_event_jwt_id")]
    public void SetSpecificClaimsAreTypeAndValueChecked(
        string replacement,
        string code)
    {
        TokenKey key = HmacKey(1);
        string claims;
        if (replacement.StartsWith("\"jti\"", StringComparison.Ordinal))
        {
            claims = "{\"iss\":\"issuer\",\"iat\":900," + replacement
                + "\"aud\":\"feed-1\",\"events\":{\"https://example/event\":{}}}";
        }
        else
        {
            claims = "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
                + "\"aud\":\"feed-1\"," + replacement
                + "\"events\":{\"https://example/event\":{}}}";
        }

        AssertFailure(
            SecurityEventTokenValidator.Validate(
                Signed(claims, key, null),
                Resolver(key),
                Policy()),
            code);
    }

    [Test]
    public void SignatureRequirementCanAllowAuthenticatedEncryptionOnly()
    {
        TokenKey signing = HmacKey(1);
        TokenKey encryption = DirectKey(90);
        TokenPolicy algorithms = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string encrypted = JsonWebToken.CreateEncrypted(
            Claims(ValidClaims()),
            encryption,
            Header("{\"enc\":\"A128GCM\",\"typ\":\"secevent+jwt\"}"),
            algorithms).Text;
        string nested = JsonWebToken.CreateSignedThenEncrypted(
            Claims(ValidClaims()),
            signing,
            encryption,
            Header("{\"typ\":\"secevent+jwt\"}"),
            Header("{\"enc\":\"A128GCM\"}"),
            algorithms).Text;
        TokenKeyResolver resolver = Resolver(signing, encryption);
        SecurityEventTokenValidationPolicy encryptedPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        encryptedPolicy.RequireSignature = false;

        JwtValidationResult encryptedResult = SecurityEventTokenValidator.Validate(
            encrypted,
            resolver,
            encryptedPolicy);
        JwtValidationResult nestedResult = SecurityEventTokenValidator.Validate(
            nested,
            resolver,
            Policy(
                JoseAlgorithms.HmacSha256,
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            AssertFailure(
                SecurityEventTokenValidator.Validate(
                    encrypted,
                    resolver,
                    Policy(
                        JoseAlgorithms.Direct,
                        JoseAlgorithms.Aes128Gcm)),
                "security_event_signature_required");
            Assert.That(encryptedResult.Success, Is.True);
            Assert.That(encryptedResult.EncryptionValidated, Is.True);
            Assert.That(encryptedResult.SignatureValidated, Is.False);
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Nested, Is.True);
            Assert.That(nestedResult.SignatureValidated, Is.True);
        });
    }

    [Test]
    public void ReplayCallbackUsesExpirationOrUnboundedRetention()
    {
        TokenKey key = HmacKey(1);
        List<string> identifiers = new List<string>();
        List<DateTimeOffset> expirations = new List<DateTimeOffset>();
        SecurityEventTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            identifiers.Add(identifier);
            expirations.Add(expiration);
            return Success();
        };
        string noExpiration = Signed(ValidClaims(), key, null);
        string expiration = Signed(
            CompleteClaims(
                "\"exp\":2000,\"events\":{\"https://example/event\":{}}"),
            key,
            null);

        JwtValidationResult first = SecurityEventTokenValidator.Validate(
            noExpiration,
            Resolver(key),
            policy);
        JwtValidationResult second = SecurityEventTokenValidator.Validate(
            expiration,
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(identifiers, Is.EqualTo(new[] { "set-1", "set-1" }));
            Assert.That(expirations[0], Is.EqualTo(DateTimeOffset.MaxValue));
            Assert.That(expirations[1],
                Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(2000)));
        });
    }

    [Test]
    public void ReplayRejectionDoesNotExposeAuthenticatedClaims()
    {
        TokenKey key = HmacKey(1);
        SecurityEventTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
            new TokenResult();

        AssertFailure(
            SecurityEventTokenValidator.Validate(
                Signed(ValidClaims(), key, null),
                Resolver(key),
                policy),
            "security_event_replay_detected");
    }

    [Test]
    public void ReplayCallbackRejectsContradictorySuccessDetails()
    {
        TokenKey key = HmacKey(1);
        SecurityEventTokenValidationPolicy policy = Policy();
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
            ContradictorySuccess();

        AssertFailure(
            SecurityEventTokenValidator.Validate(
                Signed(ValidClaims(), key, null),
                Resolver(key),
                policy),
            "invalid_replay_validation_result");
    }

    [Test]
    public void PolicyRequiresIssuerAudienceAndValidEventTypes()
    {
        SecurityEventTokenValidationPolicy issuer = Policy();
        issuer.JwtPolicy.ValidIssuers = Array.Empty<string>();
        SecurityEventTokenValidationPolicy audience = Policy();
        audience.JwtPolicy.ValidAudiences = Array.Empty<string>();
        SecurityEventTokenValidationPolicy eventType = Policy();
        eventType.ValidEventTypes = new[] { "relative" };
        SecurityEventTokenValidationPolicy duplicate = Policy();
        duplicate.ValidEventTypes = new[] { EventType, EventType };

        Assert.Multiple(() =>
        {
            AssertPolicyFailure(issuer);
            AssertPolicyFailure(audience);
            AssertPolicyFailure(eventType);
            AssertPolicyFailure(duplicate);
        });
    }

    [Test]
    public void ValidatorRequiresArguments()
    {
        TokenKey key = HmacKey(1);
        string token = Signed(ValidClaims(), key, null);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => SecurityEventTokenValidator.Validate(
                    null,
                    Resolver(key),
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => SecurityEventTokenValidator.Validate(
                    token,
                    null,
                    Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => SecurityEventTokenValidator.Validate(
                    token,
                    Resolver(key),
                    null),
                Throws.ArgumentNullException);
        });
    }

    static string ValidClaims()
    {
        return ValidClaims("https://example/event", "{}");
    }

    static string ValidClaims(string eventType, string payload)
    {
        return CompleteClaims(
            "\"events\":{\"" + eventType + "\":" + payload + "}");
    }

    static string CompleteClaims(string members)
    {
        return "{\"iss\":\"issuer\",\"iat\":900,\"jti\":\"set-1\","
            + "\"aud\":\"feed-1\"," + members + "}";
    }

    static string Signed(string claims, TokenKey key, string type)
    {
        string typeMember = type == null
            ? string.Empty
            : ",\"typ\":\"" + type + "\"";
        JoseHeader header = Header(
            "{\"alg\":\"HS256\"" + typeMember + "}");
        return JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(claims),
            header,
            key,
            TokenPolicyFor(JoseAlgorithms.HmacSha256)).Text;
    }

    static JwtClaims Claims(string json)
    {
        return JwtClaims.Read(json).Claims;
    }

    static JoseHeader Header(string json)
    {
        return JoseHeader.Read(json).Header;
    }

    static SecurityEventTokenValidationPolicy Policy(
        params string[] algorithms)
    {
        string[] allowed = algorithms.Length == 0
            ? new[] { JoseAlgorithms.HmacSha256 }
            : algorithms;
        TokenPolicy tokenPolicy = TokenPolicyFor(allowed);
        tokenPolicy.ClockSkewSeconds = 0;
        SecurityEventTokenValidationPolicy policy =
            new SecurityEventTokenValidationPolicy();
        policy.JwtPolicy = JwtValidationPolicy.Create(tokenPolicy);
        policy.JwtPolicy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1100));
        policy.JwtPolicy.ValidIssuers = new[] { "issuer" };
        policy.JwtPolicy.ValidAudiences = new[] { "feed-1" };
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

    static TokenResult TokenFailure(string code)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static TokenResult ContradictorySuccess()
    {
        TokenResult result = TokenFailure("unexpected_success_error");
        result.Success = true;
        return result;
    }

    static void AssertPolicyFailure(SecurityEventTokenValidationPolicy policy)
    {
        TokenResult result = policy.Validate();
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("invalid_security_event_policy"));
    }

    static void AssertFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
