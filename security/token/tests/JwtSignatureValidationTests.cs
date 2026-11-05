using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtSignatureValidationTests
{
    [Test]
    public void ValidateAuthenticatesClaimsAndPopulatesResolverContext()
    {
        TokenKey key = HmacKey("key-1", 1);
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        JwtClaims claims = JwtClaims.Read(
            "{\"iss\":\"https://issuer.example\",\"aud\":[\"api\",\"admin\"],"
            + "\"sub\":\"subject\",\"private\":true}").Claims;
        TokenTextResult token = JsonWebToken.CreateSigned(
            claims,
            key,
            JoseHeader.Read(
                "{\"alg\":\"HS256\",\"typ\":\"at+jwt\",\"kid\":\"key-1\"}")
                .Header,
            creationPolicy);
        RecordingKeyResolver resolver = new RecordingKeyResolver(key);

        JwtValidationResult result = JsonWebToken.Validate(
            token.Text,
            resolver,
            ValidationPolicy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("subject"));
            Assert.That(result.ProtectedHeader.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("at+jwt"));
            Assert.That(result.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(result.SigningKeyId, Is.EqualTo("key-1"));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.False);
            Assert.That(result.Nested, Is.False);
            Assert.That(resolver.Context, Is.Not.Null);
            Assert.That(resolver.Context.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(resolver.Context.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(resolver.Context.EncryptionAlgorithm, Is.Null);
            Assert.That(resolver.Context.KeyId, Is.EqualTo("key-1"));
            Assert.That(resolver.Context.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(resolver.Context.Issuer, Is.EqualTo("https://issuer.example"));
            Assert.That(resolver.Context.Audiences, Is.EqualTo(new[] { "api", "admin" }));
            Assert.That(resolver.Context.Operation, Is.EqualTo(TokenKeyOperations.Verify));
            Assert.That(resolver.Context.ProtectedHeader, Is.Not.Null);
            Assert.That(resolver.Context.UnverifiedClaims.Get("private").BooleanValue, Is.True);
        });
    }

    [Test]
    public void ValidateAcceptsJsonWebKeySetResolver()
    {
        TokenKey key = HmacKey("set-key", 2);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"sub\":\"from-set\"}").Claims,
            key,
            tokenPolicy).Text;
        JsonWebKeySet set = new JsonWebKeySet(new TokenKey[] { key });
        JsonWebKeySetResolver resolver = new JsonWebKeySetResolver(set);

        JwtValidationResult result = JsonWebToken.Validate(
            token,
            resolver,
            ValidationPolicy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(
                result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("from-set"));
        });
    }

    [Test]
    public void ResolverContextNormalizesSingleAndMissingAudienceForms()
    {
        TokenKey key = HmacKey(null, 2);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        TokenTextResult singleToken = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"aud\":\"api\"}").Claims,
            key,
            tokenPolicy);
        TokenTextResult missingToken = JsonWebToken.CreateSigned(
            JwtClaims.Read("{}").Claims,
            key,
            tokenPolicy);
        List<string[]> audiences = new List<string[]>();
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            audiences.Add(context.Audiences);
            return KeySuccess(key);
        });
        JwtValidationPolicy policy = ValidationPolicy(JoseAlgorithms.HmacSha256);

        JwtValidationResult single = JwtValidator.ValidateSigned(
            singleToken.Text,
            resolver,
            policy);
        JwtValidationResult missing = JwtValidator.ValidateSigned(
            missingToken.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(single.Success, Is.True);
            Assert.That(missing.Success, Is.True);
            Assert.That(audiences[0], Is.EqualTo(new[] { "api" }));
            Assert.That(audiences[1], Is.Empty);
            Assert.That(audiences[0], Is.Not.SameAs(audiences[1]));
        });
    }

    [Test]
    public void ValidateSupportsRsaKeysSelectedByProtectedMetadata()
    {
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            privateRsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaSha256,
            false).Key;
        privateKey.KeyId = "rsa-1";
        publicKey.KeyId = "rsa-1";
        TokenPolicy creationPolicy = TokenPolicyFor(JoseAlgorithms.RsaSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"sub\":\"rsa-subject\"}").Claims,
            privateKey,
            creationPolicy).Text;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            if (context.KeyId == "rsa-1"
                && context.Algorithm == JoseAlgorithms.RsaSha256)
            {
                return KeySuccess(publicKey);
            }

            return KeyFailure("key_not_found");
        });

        JwtValidationResult result = JsonWebToken.ValidateSigned(
            token,
            resolver,
            ValidationPolicy(JoseAlgorithms.RsaSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("rsa-subject"));
        });
    }

    [Test]
    public void TamperedSignatureNeverExposesClaimsAsValidated()
    {
        TokenKey key = HmacKey(null, 3);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"sub\":\"original\"}").Claims,
            key,
            tokenPolicy).Text;
        string[] segments = token.Split('.');
        segments[1] = Base64Url.Encode(
            Encoding.UTF8.GetBytes("{\"sub\":\"tampered\"}")).Text;
        string tampered = string.Join('.', segments);

        JwtValidationResult result = JsonWebToken.Validate(
            tampered,
            new TokenKeyResolver(context => KeySuccess(key)),
            ValidationPolicy(JoseAlgorithms.HmacSha256));

        AssertFailure(result, "signature_mismatch");
    }

    [Test]
    public void ValidateRejectsMalformedClaimsBeforeCallingResolver()
    {
        TokenKey key = HmacKey(null, 4);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("[]"),
            JoseHeader.Read("{\"alg\":\"HS256\",\"typ\":\"JWT\"}").Header,
            key,
            tokenPolicy).Text;
        int calls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            calls += 1;
            return KeySuccess(key);
        });

        JwtValidationResult result = JsonWebToken.ValidateSigned(
            token,
            resolver,
            ValidationPolicy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            AssertFailure(result, "json_root_not_object");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void ValidateRejectsUnsecuredAndUnencodedJwtPayloadsBeforeResolution()
    {
        TokenKey key = HmacKey(null, 5);
        TokenPolicy unsecuredPolicy = TokenPolicyFor(JoseAlgorithms.None);
        unsecuredPolicy.AllowUnsecuredTokens = true;
        string unsecured = JsonWebSignature.CreateUnsecured(
            Encoding.UTF8.GetBytes("{}"),
            JoseHeader.Read("{\"alg\":\"none\"}").Header,
            unsecuredPolicy).Text;
        TokenPolicy unencodedPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string unencoded = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{}"),
            JoseHeader.Read(
                "{\"alg\":\"HS256\",\"b64\":false,\"crit\":[\"b64\"]}").Header,
            key,
            unencodedPolicy).Text;
        int calls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            calls += 1;
            return KeySuccess(key);
        });

        JwtValidationResult unsecuredResult = JsonWebToken.ValidateSigned(
            unsecured,
            resolver,
            ValidationPolicy(JoseAlgorithms.None));
        JwtValidationResult unencodedResult = JsonWebToken.ValidateSigned(
            unencoded,
            resolver,
            ValidationPolicy(JoseAlgorithms.HmacSha256));

        Assert.Multiple(() =>
        {
            AssertFailure(unsecuredResult, "jwt_signature_required");
            AssertFailure(unencodedResult, "invalid_jwt_payload_encoding");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void ResolverFailuresAndInvalidResultsAreReturnedStably()
    {
        TokenKey key = HmacKey(null, 6);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{}").Claims,
            key,
            tokenPolicy).Text;
        JwtValidationPolicy policy = ValidationPolicy(JoseAlgorithms.HmacSha256);
        TokenKeyResult successWithoutKey = new TokenKeyResult();
        successWithoutKey.Success = true;
        TokenKeyResult failureWithoutError = new TokenKeyResult();
        TokenKeyResult successWithError = KeySuccess(key);
        successWithError.Error = new TokenError(
            "contradictory_success",
            "A successful result cannot contain an error.",
            -1);
        TokenKeyResult failureWithKey = KeyFailure(
            "contradictory_failure");
        failureWithKey.Key = key;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.Validate(token, TokenKeyResolver.CreateEmpty(), policy),
                "key_resolver_not_configured");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new NullKeyResolver(),
                    policy),
                "invalid_key_resolution_result");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => successWithoutKey),
                    policy),
                "invalid_key_resolution_result");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => failureWithoutError),
                    policy),
                "key_resolution_failed");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => successWithError),
                    policy),
                "invalid_key_resolution_result");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => failureWithKey),
                    policy),
                "invalid_key_resolution_result");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeyFailure("custom_key_failure")),
                    policy),
                "custom_key_failure");
        });
    }

    [Test]
    public void ValidationEnforcesPolicyAndResolvedKeyRestrictions()
    {
        TokenKey key = HmacKey(null, 7);
        TokenPolicy tokenPolicy = TokenPolicyFor(JoseAlgorithms.HmacSha256);
        string token = JsonWebToken.CreateSigned(
            JwtClaims.Read("{}").Claims,
            key,
            tokenPolicy).Text;
        JwtValidationPolicy disabled = ValidationPolicy(JoseAlgorithms.HmacSha256);
        disabled.AllowSignedTokens = false;
        disabled.AllowNestedTokens = false;
        TokenKey wrong = HmacKey(null, 40);
        TokenKey restricted = HmacKey(null, 7);
        restricted.KeyOperations = new[] { TokenKeyOperations.Sign };
        JwtValidationPolicy denied = ValidationPolicy();
        JwtValidationPolicy invalid = ValidationPolicy(JoseAlgorithms.HmacSha256);
        invalid.TokenPolicy.MaximumJsonDepth = 0;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeySuccess(key)),
                    disabled),
                "signed_tokens_not_allowed");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeySuccess(wrong)),
                    ValidationPolicy(JoseAlgorithms.HmacSha256)),
                "signature_mismatch");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeySuccess(restricted)),
                    ValidationPolicy(JoseAlgorithms.HmacSha256)),
                "key_operation_not_permitted");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeySuccess(key)),
                    denied),
                "algorithm_not_allowed");
            AssertFailure(
                JsonWebToken.Validate(
                    token,
                    new TokenKeyResolver(context => KeySuccess(key)),
                    invalid),
                "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            KeyFailure("unused"));
        JwtValidationPolicy policy = ValidationPolicy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => JwtValidator.ValidateSigned(null, resolver, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.ValidateSigned("token", null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.ValidateSigned("token", resolver, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => new TokenKeyResolver(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => resolver.Resolve(null),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey HmacKey(string keyId, int start)
    {
        byte[] bytes = Enumerable.Range(start, 32)
            .Select(value => (byte)value)
            .ToArray();
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            bytes,
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JwtValidationPolicy ValidationPolicy(params string[] algorithms)
    {
        TokenPolicy tokenPolicy = TokenPolicyFor(algorithms);
        return JwtValidationPolicy.Create(tokenPolicy);
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static TokenKeyResult KeyFailure(string code)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, "Key resolution failed.", -1);
        return result;
    }

    static void AssertFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    class RecordingKeyResolver : ITokenKeyResolver
    {
        TokenKey key;
        public TokenKeyResolutionContext Context;

        public RecordingKeyResolver(TokenKey key)
        {
            this.key = key;
        }

        public TokenKeyResult Resolve(TokenKeyResolutionContext context)
        {
            Context = context;
            return KeySuccess(key);
        }
    }

    class NullKeyResolver : ITokenKeyResolver
    {
        public TokenKeyResult Resolve(TokenKeyResolutionContext context)
        {
            return null;
        }
    }
}
