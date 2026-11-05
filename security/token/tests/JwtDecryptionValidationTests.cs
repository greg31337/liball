using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtDecryptionValidationTests
{
    [Test]
    public void ValidateAuthenticatesEncryptedClaimsAndPopulatesResolverContext()
    {
        TokenKey key = DirectKey("encryption-key", 1);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JsonWebToken.CreateEncrypted(
            JwtClaims.Read(
                "{\"iss\":\"https://issuer.example\",\"aud\":\"api\",\"sub\":\"subject\"}")
                .Claims,
            key,
            JoseHeader.Read(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"typ\":\"at+jwt\",\"kid\":\"encryption-key\"}")
                .Header,
            creationPolicy).Text;
        TokenKeyResolutionContext observed = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            observed = context;
            return KeySuccess(key);
        });

        JwtValidationResult result = JsonWebToken.Validate(
            token,
            resolver,
            ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.Claims.Get(JwtClaimNames.Issuer).StringValue,
                Is.EqualTo("https://issuer.example"));
            Assert.That(result.KeyManagementAlgorithm, Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(result.EncryptionAlgorithm, Is.EqualTo(JoseAlgorithms.Aes128Gcm));
            Assert.That(result.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(result.EncryptionKeyId, Is.EqualTo("encryption-key"));
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.SignatureValidated, Is.False);
            Assert.That(result.Nested, Is.False);
            Assert.That(observed.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(observed.Algorithm, Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(observed.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes128Gcm));
            Assert.That(observed.KeyId, Is.EqualTo("encryption-key"));
            Assert.That(observed.TokenType, Is.EqualTo("at+jwt"));
            Assert.That(observed.Issuer, Is.Null);
            Assert.That(observed.Audiences, Is.Empty);
            Assert.That(observed.Operation, Is.EqualTo(TokenKeyOperations.Decrypt));
            Assert.That(observed.ProtectedHeader, Is.Not.Null);
            Assert.That(observed.UnverifiedClaims, Is.Null);
        });
    }

    [Test]
    public void ValidateEncryptedSupportsRsaKeyUnwrapping()
    {
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaOaepSha256,
            false).Key;
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            privateRsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        publicKey.KeyId = "rsa-key";
        privateKey.KeyId = "rsa-key";
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        string token = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"sub\":\"rsa-subject\"}").Claims,
            publicKey,
            JoseAlgorithms.Aes256Gcm,
            creationPolicy).Text;
        string operation = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            operation = context.Operation;
            return KeySuccess(privateKey);
        });

        JwtValidationResult result = JsonWebToken.ValidateEncrypted(
            token,
            resolver,
            ValidationPolicy(
                JoseAlgorithms.RsaOaepSha256,
                JoseAlgorithms.Aes256Gcm));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("rsa-subject"));
            Assert.That(result.KeyManagementAlgorithm,
                Is.EqualTo(JoseAlgorithms.RsaOaepSha256));
            Assert.That(operation, Is.EqualTo(TokenKeyOperations.UnwrapKey));
        });
    }

    [Test]
    public void ValidateNestedResolvesAndAuthenticatesBothLayers()
    {
        TokenKey signingKey = HmacKey("signing-key", 20);
        TokenKey encryptionKey = DirectKey("encryption-key", 60);
        TokenPolicy creationPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read(
                "{\"iss\":\"issuer\",\"aud\":[\"api\",\"admin\"],\"sub\":\"nested\"}")
                .Claims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            creationPolicy).Text;
        List<TokenKeyResolutionContext> contexts = new List<TokenKeyResolutionContext>();
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            contexts.Add(context);
            return context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryptionKey)
                : KeySuccess(signingKey);
        });

        JwtValidationResult result = JsonWebToken.Validate(
            token,
            resolver,
            ValidationPolicy(
                JoseAlgorithms.HmacSha256,
                JoseAlgorithms.Direct,
                JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("nested"));
            Assert.That(result.ProtectedHeader.Get(JoseHeaderNames.ContentType).StringValue,
                Is.EqualTo("JWT"));
            Assert.That(result.InnerProtectedHeader.Get(JoseHeaderNames.Algorithm).StringValue,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.KeyManagementAlgorithm, Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(result.EncryptionAlgorithm, Is.EqualTo(JoseAlgorithms.Aes128Gcm));
            Assert.That(result.SigningKeyId, Is.EqualTo("signing-key"));
            Assert.That(result.EncryptionKeyId, Is.EqualTo("encryption-key"));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
            Assert.That(contexts, Has.Count.EqualTo(2));
            Assert.That(contexts[0].Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(contexts[0].UnverifiedClaims, Is.Null);
            Assert.That(contexts[1].Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(contexts[1].Issuer, Is.EqualTo("issuer"));
            Assert.That(contexts[1].Audiences, Is.EqualTo(new[] { "api", "admin" }));
        });
    }

    [Test]
    public void NestedValidationRejectsDisabledAndMismatchedInnerContent()
    {
        TokenKey signingKey = HmacKey(null, 30);
        TokenKey encryptionKey = DirectKey(null, 80);
        TokenPolicy tokenPolicy = TokenPolicyFor(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string nestedToken = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read("{}").Claims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            tokenPolicy).Text;
        string mismatched = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes("{}"),
            JoseHeader.Read(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\"JWT\"}").Header,
            encryptionKey,
            tokenPolicy).Text;
        TokenKeyResolver resolver = Resolver(signingKey, encryptionKey);
        JwtValidationPolicy disabled = ValidationPolicy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        disabled.AllowNestedTokens = false;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.Validate(nestedToken, resolver, disabled),
                "nested_tokens_not_allowed");
            AssertFailure(
                JsonWebToken.Validate(
                    mismatched,
                    resolver,
                    ValidationPolicy(
                        JoseAlgorithms.HmacSha256,
                        JoseAlgorithms.Direct,
                        JoseAlgorithms.Aes128Gcm)),
                "nested_content_type_mismatch");
        });
    }

    [Test]
    public void TamperedCiphertextNeverExposesClaimsAsValidated()
    {
        TokenKey key = DirectKey(null, 100);
        TokenPolicy tokenPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"sub\":\"secret\"}").Claims,
            key,
            JoseAlgorithms.Aes128Gcm,
            tokenPolicy).Text;
        string[] segments = token.Split('.');
        char first = segments[3][0];
        segments[3] = (first == 'A' ? "B" : "A") + segments[3].Substring(1);
        string tampered = string.Join('.', segments);

        JwtValidationResult result = JsonWebToken.Validate(
            tampered,
            new TokenKeyResolver(context => KeySuccess(key)),
            ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm));

        AssertFailure(result, "decryption_failed");
    }

    [Test]
    public void MalformedAuthenticatedPlaintextDoesNotBecomeClaims()
    {
        TokenKey key = DirectKey(null, 120);
        TokenPolicy tokenPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes("[]"),
            JoseHeader.Read("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header,
            key,
            tokenPolicy).Text;
        int calls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            calls += 1;
            return KeySuccess(key);
        });

        JwtValidationResult result = JsonWebToken.ValidateEncrypted(
            token,
            resolver,
            ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            AssertFailure(result, "json_root_not_object");
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void DecryptionPropagatesResolverPolicyAndKeyFailures()
    {
        TokenKey key = DirectKey(null, 140);
        TokenPolicy tokenPolicy = TokenPolicyFor(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        string token = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{}").Claims,
            key,
            JoseAlgorithms.Aes128Gcm,
            tokenPolicy).Text;
        JwtValidationPolicy disabled = ValidationPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        disabled.AllowEncryptedTokens = false;
        disabled.AllowNestedTokens = false;
        TokenKey restricted = DirectKey(null, 140);
        restricted.KeyOperations = new[] { TokenKeyOperations.Encrypt };
        JwtValidationPolicy denied = ValidationPolicy();

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.ValidateEncrypted(
                    token,
                    new TokenKeyResolver(context => KeySuccess(key)),
                    disabled),
                "encrypted_tokens_not_allowed");
            AssertFailure(
                JsonWebToken.ValidateEncrypted(
                    token,
                    new TokenKeyResolver(context => KeyFailure("decrypt_key_missing")),
                    ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "decrypt_key_missing");
            AssertFailure(
                JsonWebToken.ValidateEncrypted(
                    token,
                    new TokenKeyResolver(context => KeySuccess(restricted)),
                    ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "key_operation_not_permitted");
            AssertFailure(
                JsonWebToken.ValidateEncrypted(
                    token,
                    new TokenKeyResolver(context => KeySuccess(key)),
                    denied),
                "algorithm_not_allowed");
        });
    }

    [Test]
    public void ValidateDispatchRejectsMalformedCompactShapeWithoutResolution()
    {
        int calls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            calls += 1;
            return KeyFailure("unused");
        });

        JwtValidationResult result = JsonWebToken.Validate(
            "one.two",
            resolver,
            ValidationPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm));

        Assert.Multiple(() =>
        {
            AssertFailure(result, "invalid_compact_token");
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            KeyFailure("unused"));
        JwtValidationPolicy policy = ValidationPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => JwtValidator.Validate(null, resolver, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.Validate("token", null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.Validate("token", resolver, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.ValidateEncrypted(null, resolver, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.ValidateEncrypted("token", null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtValidator.ValidateEncrypted("token", resolver, null),
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

    static TokenKey DirectKey(string keyId, int start)
    {
        byte[] bytes = Enumerable.Range(start, 16)
            .Select(value => (byte)value)
            .ToArray();
        TokenKey key = SymmetricKeyImporterOperations.Import(bytes).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        key.KeyId = keyId;
        return key;
    }

    static TokenKeyResolver Resolver(TokenKey signingKey, TokenKey encryptionKey)
    {
        return new TokenKeyResolver(context =>
            context.Kind == TokenKind.JweCompact
                ? KeySuccess(encryptionKey)
                : KeySuccess(signingKey));
    }

    static TokenPolicy TokenPolicyFor(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JwtValidationPolicy ValidationPolicy(params string[] algorithms)
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
        Assert.That(result.EncryptionValidated, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
