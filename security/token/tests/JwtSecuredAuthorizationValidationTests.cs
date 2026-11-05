using System.Text;
using token;

namespace token.Tests;

public class JwtSecuredAuthorizationValidationTests
{
    [Test]
    public void SignedRequestObjectAuthenticatesProfileParameters()
    {
        TokenKey key = HmacKey(1);
        string token = Signed(
            RequestClaims(),
            key,
            "oauth-authz-req+jwt");

        JwtValidationResult result =
            JwtSecuredAuthorizationRequestValidator.Validate(
                token,
                Resolver(key),
                RequestPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.TokenType, Is.EqualTo("oauth-authz-req+jwt"));
            Assert.That(result.Claims.Get(JwtClaimNames.ClientId).StringValue,
                Is.EqualTo("client-1"));
            Assert.That(result.Claims.Get("response_type").StringValue,
                Is.EqualTo("code"));
        });
    }

    [TestCase("{\"aud\":\"https://as.example\",\"client_id\":\"client-1\",\"response_type\":\"code\"}", "iss")]
    [TestCase("{\"iss\":\"client-1\",\"client_id\":\"client-1\",\"response_type\":\"code\"}", "aud")]
    [TestCase("{\"iss\":\"client-1\",\"aud\":\"https://as.example\",\"response_type\":\"code\"}", "client_id")]
    [TestCase("{\"iss\":\"client-1\",\"aud\":\"https://as.example\",\"client_id\":\"client-1\"}", "response_type")]
    public void RequestObjectRequiresSecureProfileClaims(
        string json,
        string missing)
    {
        TokenKey key = HmacKey(1);
        JwtValidationResult result =
            JwtSecuredAuthorizationRequestValidator.Validate(
                Signed(json, key, null),
                Resolver(key),
                RequestPolicy());

        AssertFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void RequestObjectBindsClientAndAuthorizationServer()
    {
        TokenKey key = HmacKey(1);
        string wrongIssuer = Signed(
            "{\"iss\":\"other\",\"aud\":\"https://as.example\","
                + "\"client_id\":\"client-1\",\"response_type\":\"code\"}",
            key,
            null);
        string wrongAudience = Signed(
            "{\"iss\":\"client-1\",\"aud\":\"https://other.example\","
                + "\"client_id\":\"client-1\",\"response_type\":\"code\"}",
            key,
            null);
        string wrongClient = Signed(
            "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
                + "\"client_id\":\"other\",\"response_type\":\"code\"}",
            key,
            null);

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    wrongIssuer,
                    Resolver(key),
                    RequestPolicy()),
                "issuer_mismatch");
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    wrongAudience,
                    Resolver(key),
                    RequestPolicy()),
                "audience_mismatch");
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    wrongClient,
                    Resolver(key),
                    RequestPolicy()),
                "authorization_request_client_id_mismatch");
        });
    }

    [Test]
    public void RequestObjectExplicitTypeIsOptionalButValidatedWhenPresent()
    {
        TokenKey key = HmacKey(1);
        string untyped = Signed(RequestClaims(), key, null);
        string fullType = Signed(
            RequestClaims(),
            key,
            "APPLICATION/OAUTH-AUTHZ-REQ+JWT");
        string wrongType = Signed(RequestClaims(), key, "JWT");
        JwtSecuredAuthorizationRequestValidationPolicy required =
            RequestPolicy();
        required.RequireExplicitType = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    untyped,
                    Resolver(key),
                    RequestPolicy()).Success,
                Is.True);
            Assert.That(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    fullType,
                    Resolver(key),
                    RequestPolicy()).Success,
                Is.True);
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    wrongType,
                    Resolver(key),
                    RequestPolicy()),
                "invalid_authorization_request_type");
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    untyped,
                    Resolver(key),
                    required),
                "missing_authorization_request_type");
        });
    }

    [TestCase("request")]
    [TestCase("request_uri")]
    public void RequestObjectRejectsNestedRequestParameters(string parameter)
    {
        TokenKey key = HmacKey(1);
        string claims = "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
            + "\"client_id\":\"client-1\",\"response_type\":\"code\",\""
            + parameter + "\":\"forbidden\"}";

        AssertFailure(
            JwtSecuredAuthorizationRequestValidator.Validate(
                Signed(claims, key, null),
                Resolver(key),
                RequestPolicy()),
            "nested_authorization_request_parameter");
    }

    [Test]
    public void RequestObjectValidatesParameterTypes()
    {
        TokenKey key = HmacKey(1);
        string clientNumber = Signed(
            "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
                + "\"client_id\":1,\"response_type\":\"code\"}",
            key,
            null);
        string emptyResponseType = Signed(
            "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
                + "\"client_id\":\"client-1\",\"response_type\":\"\"}",
            key,
            null);

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    clientNumber,
                    Resolver(key),
                    RequestPolicy()),
                "invalid_claim_type");
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    emptyResponseType,
                    Resolver(key),
                    RequestPolicy()),
                "invalid_authorization_request_response_type");
        });
    }

    [Test]
    public void CompatibilityPolicyAllowsMissingIssuerAndAudienceButChecksPresentValues()
    {
        TokenKey key = HmacKey(1);
        JwtSecuredAuthorizationRequestValidationPolicy policy = RequestPolicy();
        policy.RequireIssuerAndAudience = false;
        string missing = Signed(
            "{\"client_id\":\"client-1\",\"response_type\":\"code\"}",
            key,
            null);
        string wrong = Signed(
            "{\"iss\":\"other\",\"client_id\":\"client-1\","
                + "\"response_type\":\"code\"}",
            key,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    missing,
                    Resolver(key),
                    policy).Success,
                Is.True);
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    wrong,
                    Resolver(key),
                    policy),
                "authorization_request_issuer_mismatch");
        });
    }

    [Test]
    public void RequestObjectMustBeSignedButMayBeNestedAndEncrypted()
    {
        TokenKey signing = HmacKey(1);
        TokenKey encryption = DirectKey(90);
        string direct = JsonWebToken.CreateEncrypted(
            Claims(RequestClaims()),
            encryption,
            JoseAlgorithms.Aes128Gcm,
            TokenPolicyFor(
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm)).Text;
        string nested = JwtCreator.CreateSignedThenEncrypted(
            Claims(RequestClaims()),
            signing,
            encryption,
            Header("{\"typ\":\"oauth-authz-req+jwt\"}"),
            Header("{\"enc\":\"A128GCM\"}"),
            TokenPolicyFor(
                JoseAlgorithms.HmacSha256,
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm)).Text;
        TokenKeyResolver resolver = Resolver(signing, encryption);

        JwtValidationResult nestedResult =
            JwtSecuredAuthorizationRequestValidator.Validate(
                nested,
                resolver,
                RequestPolicy(
                    JoseAlgorithms.HmacSha256,
                    JoseAlgorithms.Direct,
                    JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationRequestValidator.Validate(
                    direct,
                    resolver,
                    RequestPolicy(
                        JoseAlgorithms.Direct,
                        JoseAlgorithms.Aes128Gcm)),
                "authorization_request_signature_required");
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Nested, Is.True);
            Assert.That(nestedResult.SignatureValidated, Is.True);
            Assert.That(nestedResult.EncryptionValidated, Is.True);
        });
    }

    [Test]
    public void SignedAuthorizationResponseAuthenticatesBeforeExposingParameters()
    {
        TokenKey key = HmacKey(1);
        string token = Signed(ResponseClaims(), key, null);

        JwtValidationResult result =
            JwtSecuredAuthorizationResponseValidator.Validate(
                token,
                Resolver(key),
                ResponsePolicy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.Claims.Get("code").StringValue,
                Is.EqualTo("authorization-code"));
            Assert.That(result.Claims.Get("state").StringValue,
                Is.EqualTo("request-state"));
        });
    }

    [TestCase("{\"aud\":\"client-1\",\"exp\":1500,\"code\":\"code\"}", "iss")]
    [TestCase("{\"iss\":\"https://as.example\",\"exp\":1500,\"code\":\"code\"}", "aud")]
    [TestCase("{\"iss\":\"https://as.example\",\"aud\":\"client-1\",\"code\":\"code\"}", "exp")]
    public void AuthorizationResponseRequiresSecurityClaims(
        string json,
        string missing)
    {
        TokenKey key = HmacKey(1);
        JwtValidationResult result =
            JwtSecuredAuthorizationResponseValidator.Validate(
                Signed(json, key, null),
                Resolver(key),
                ResponsePolicy());

        AssertFailure(result, "missing_required_claim");
        Assert.That(result.Error.Message, Does.Contain(missing));
    }

    [Test]
    public void AuthorizationResponseEnforcesIssuerAudienceAndExpiration()
    {
        TokenKey key = HmacKey(1);
        string issuer = Signed(
            "{\"iss\":\"https://other.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"code\"}",
            key,
            null);
        string audience = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"other\","
                + "\"exp\":1500,\"code\":\"code\"}",
            key,
            null);
        string expired = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1000,\"code\":\"code\"}",
            key,
            null);
        string tooLong = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1701,\"code\":\"code\"}",
            key,
            null);
        TokenKeyResolver resolver = Resolver(key);

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    issuer,
                    resolver,
                    ResponsePolicy()),
                "issuer_mismatch");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    audience,
                    resolver,
                    ResponsePolicy()),
                "audience_mismatch");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    expired,
                    resolver,
                    ResponsePolicy()),
                "token_expired");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    tooLong,
                    resolver,
                    ResponsePolicy()),
                "authorization_response_lifetime_too_long");
        });
    }

    [Test]
    public void AuthorizationResponseValidatesRequestedState()
    {
        TokenKey key = HmacKey(1);
        string missing = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"code\"}",
            key,
            null);
        string wrong = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"code\",\"state\":\"wrong\"}",
            key,
            null);

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    missing,
                    Resolver(key),
                    ResponsePolicy()),
                "missing_authorization_response_state");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    wrong,
                    Resolver(key),
                    ResponsePolicy()),
                "authorization_response_state_mismatch");
        });
    }

    [Test]
    public void AuthorizationResponseRejectsInvalidOrMixedParameters()
    {
        TokenKey key = HmacKey(1);
        string invalidCode = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":1}",
            key,
            null);
        string invalidExpires = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"access_token\":\"token\","
                + "\"expires_in\":\"3600\"}",
            key,
            null);
        string mixed = Signed(
            "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
                + "\"exp\":1500,\"code\":\"code\",\"error\":\"access_denied\"}",
            key,
            null);
        JwtSecuredAuthorizationResponseValidationPolicy policy =
            ResponsePolicy();
        policy.State = null;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    invalidCode,
                    Resolver(key),
                    policy),
                "invalid_authorization_response_parameter");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    invalidExpires,
                    Resolver(key),
                    policy),
                "invalid_authorization_response_parameter");
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    mixed,
                    Resolver(key),
                    policy),
                "mixed_authorization_response");
        });
    }

    [Test]
    public void AuthorizationResponseMustBeSignedButMayBeNestedAndEncrypted()
    {
        TokenKey signing = HmacKey(1);
        TokenKey encryption = DirectKey(90);
        string direct = JsonWebToken.CreateEncrypted(
            Claims(ResponseClaims()),
            encryption,
            JoseAlgorithms.Aes128Gcm,
            TokenPolicyFor(
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm)).Text;
        string nested = JwtCreator.CreateSignedThenEncrypted(
            Claims(ResponseClaims()),
            signing,
            encryption,
            JoseAlgorithms.Aes128Gcm,
            TokenPolicyFor(
                JoseAlgorithms.HmacSha256,
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm)).Text;
        TokenKeyResolver resolver = Resolver(signing, encryption);

        JwtValidationResult nestedResult =
            JwtSecuredAuthorizationResponseValidator.Validate(
                nested,
                resolver,
                ResponsePolicy(
                    JoseAlgorithms.HmacSha256,
                    JoseAlgorithms.Direct,
                    JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtSecuredAuthorizationResponseValidator.Validate(
                    direct,
                    resolver,
                    ResponsePolicy(
                        JoseAlgorithms.Direct,
                        JoseAlgorithms.Aes128Gcm)),
                "authorization_response_signature_required");
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Nested, Is.True);
            Assert.That(nestedResult.SignatureValidated, Is.True);
            Assert.That(nestedResult.EncryptionValidated, Is.True);
        });
    }

    [Test]
    public void AuthorizationProfilesPreserveCallerPolicyAndInvokeReplayOnce()
    {
        int calls = 0;
        TokenKey key = HmacKey(1);
        JwtSecuredAuthorizationResponseValidationPolicy policy =
            ResponsePolicy();
        policy.State = null;
        policy.JwtPolicy.TokenPolicy.RequiredClaims = new[] { "tenant" };
        policy.JwtPolicy.ReplayValidator = (identifier, expiration) =>
        {
            calls += 1;
            return Success();
        };
        string claims = "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
            + "\"exp\":1500,\"code\":\"code\",\"tenant\":\"one\","
            + "\"jti\":\"response-1\"}";

        JwtValidationResult result =
            JwtSecuredAuthorizationResponseValidator.Validate(
                Signed(claims, key, null),
                Resolver(key),
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(policy.JwtPolicy.TokenPolicy.RequiredClaims,
                Is.EqualTo(new[] { "tenant" }));
            Assert.That(policy.JwtPolicy.ValidateIssuer, Is.False);
            Assert.That(policy.JwtPolicy.ValidateAudience, Is.False);
        });
    }

    [Test]
    public void AuthorizationProfilePoliciesAndValidatorsRequireContext()
    {
        JwtSecuredAuthorizationRequestValidationPolicy request =
            RequestPolicy();
        request.ClientId = null;
        JwtSecuredAuthorizationResponseValidationPolicy response =
            ResponsePolicy();
        response.MaximumResponseLifetimeSeconds = 0;
        TokenKey key = HmacKey(1);
        string token = Signed(RequestClaims(), key, null);

        Assert.Multiple(() =>
        {
            AssertTokenFailure(
                request.Validate(),
                "invalid_authorization_request_policy");
            AssertTokenFailure(
                response.Validate(),
                "invalid_authorization_response_policy");
            Assert.That(
                () => JwtSecuredAuthorizationRequestValidator.Validate(
                    null,
                    Resolver(key),
                    RequestPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtSecuredAuthorizationRequestValidator.Validate(
                    token,
                    null,
                    RequestPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtSecuredAuthorizationRequestValidator.Validate(
                    token,
                    Resolver(key),
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtSecuredAuthorizationResponseValidator.Validate(
                    null,
                    Resolver(key),
                    ResponsePolicy()),
                Throws.ArgumentNullException);
        });
    }

    static string RequestClaims()
    {
        return "{\"iss\":\"client-1\",\"aud\":\"https://as.example\","
            + "\"client_id\":\"client-1\",\"response_type\":\"code\","
            + "\"redirect_uri\":\"https://client.example/callback\"}";
    }

    static string ResponseClaims()
    {
        return "{\"iss\":\"https://as.example\",\"aud\":\"client-1\","
            + "\"exp\":1500,\"code\":\"authorization-code\","
            + "\"state\":\"request-state\"}";
    }

    static string Signed(string claims, TokenKey key, string type)
    {
        JoseHeader header = type == null
            ? Header("{\"alg\":\"HS256\"}")
            : Header("{\"alg\":\"HS256\",\"typ\":\"" + type + "\"}");
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

    static JwtSecuredAuthorizationRequestValidationPolicy RequestPolicy(
        params string[] algorithms)
    {
        JwtSecuredAuthorizationRequestValidationPolicy policy =
            new JwtSecuredAuthorizationRequestValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(algorithms);
        policy.ClientId = "client-1";
        policy.AuthorizationServerIssuer = "https://as.example";
        return policy;
    }

    static JwtSecuredAuthorizationResponseValidationPolicy ResponsePolicy(
        params string[] algorithms)
    {
        JwtSecuredAuthorizationResponseValidationPolicy policy =
            new JwtSecuredAuthorizationResponseValidationPolicy();
        policy.JwtPolicy = ValidationPolicy(algorithms);
        policy.Issuer = "https://as.example";
        policy.ClientId = "client-1";
        policy.State = "request-state";
        return policy;
    }

    static JwtValidationPolicy ValidationPolicy(params string[] algorithms)
    {
        string[] allowed = algorithms.Length == 0
            ? new[] { JoseAlgorithms.HmacSha256 }
            : algorithms;
        TokenPolicy tokenPolicy = TokenPolicyFor(allowed);
        tokenPolicy.ClockSkewSeconds = 0;
        JwtValidationPolicy policy = JwtValidationPolicy.Create(tokenPolicy);
        policy.Clock = new TokenClock(
            () => DateTimeOffset.UnixEpoch.AddSeconds(1100));
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
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertTokenFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
