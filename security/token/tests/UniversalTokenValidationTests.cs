using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class UniversalTokenValidationTests
{
    [Test]
    public void ValidateCompactJwtReturnsUnifiedAuthenticatedMetadata()
    {
        TokenKey key = HmacKey("signing-key", 1);
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256);
        JwtClaims claims = JwtClaims.Read(
            "{\"iss\":\"issuer\",\"sub\":\"alice\","
            + "\"aud\":[\"api\",\"admin\"],\"exp\":4102444800,"
            + "\"nbf\":0,\"iat\":1}").Claims;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"typ\":\"JWT\",\"kid\":\"signing-key\"}");
        TokenTextResult written = JsonWebToken.CreateSigned(
            claims,
            key,
            header,
            policy);
        TokenKeyResolutionContext observed = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            observed = context;
            return KeySuccess(key);
        });

        TokenValidationResult result = UniversalToken.Validate(
            written.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.JwsCompact);
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.JwtClaims.Get("sub").StringValue,
                Is.EqualTo("alice"));
            Assert.That(result.CwtClaims, Is.Null);
            Assert.That(result.Headers, Has.Length.EqualTo(1));
            Assert.That(result.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Algorithms,
                Is.EqualTo(new[] { JoseAlgorithms.HmacSha256 }));
            Assert.That(result.KeyId, Is.EqualTo("signing-key"));
            Assert.That(result.KeyIds, Is.EqualTo(new[] { "signing-key" }));
            Assert.That(result.TokenType, Is.EqualTo("JWT"));
            Assert.That(result.Issuer, Is.EqualTo("issuer"));
            Assert.That(result.Subject, Is.EqualTo("alice"));
            Assert.That(result.Audiences, Is.EqualTo(new[] { "api", "admin" }));
            Assert.That(result.HasExpirationTime, Is.True);
            Assert.That(result.ExpirationTime,
                Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(4102444800)));
            Assert.That(result.HasNotBefore, Is.True);
            Assert.That(result.NotBefore, Is.EqualTo(DateTimeOffset.UnixEpoch));
            Assert.That(result.HasIssuedAt, Is.True);
            Assert.That(result.IssuedAt,
                Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(1)));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.False);
            Assert.That(result.Nested, Is.False);
            Assert.That(observed.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(observed.Operation, Is.EqualTo(TokenKeyOperations.Verify));
            Assert.That(observed.UnverifiedClaims.Get("sub").StringValue,
                Is.EqualTo("alice"));
        });
    }

    [Test]
    public void ValidateCompactJweAuthenticatesEncryptionAndClaims()
    {
        TokenKey key = DirectKey("encryption-key", 20);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult written = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"sub\":\"encrypted\"}").Claims,
            key,
            JoseAlgorithms.Aes128Gcm,
            policy);
        TokenKeyResolutionContext observed = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            observed = context;
            return KeySuccess(key);
        });

        TokenValidationResult result = UniversalToken.Validate(
            written.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.JweCompact);
            Assert.That(result.Subject, Is.EqualTo("encrypted"));
            Assert.That(result.KeyManagementAlgorithm,
                Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(result.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes128Gcm));
            Assert.That(result.KeyId, Is.EqualTo("encryption-key"));
            Assert.That(result.SignatureValidated, Is.False);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(observed.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(observed.Operation, Is.EqualTo(TokenKeyOperations.Decrypt));
        });
    }

    [Test]
    public void ValidateFlattenedAndGeneralJwsAuthenticatesAllSignatures()
    {
        TokenKey first = HmacKey("first", 1);
        TokenKey second = HmacKey("second", 40);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        byte[] claims = Encoding.UTF8.GetBytes("{\"sub\":\"json-signed\"}");
        TokenTextResult flattened = JsonWebSignatureFlattened.Sign(
            claims,
            Header("{\"alg\":\"HS256\",\"kid\":\"first\"}"),
            null,
            first,
            policy);
        TokenTextResult general = JsonWebSignatureGeneral.Sign(
            claims,
            new JoseHeader[]
            {
                Header("{\"alg\":\"HS256\",\"kid\":\"first\"}"),
                Header("{\"alg\":\"HS256\",\"kid\":\"second\"}")
            },
            null,
            new TokenKey[] { first, second },
            policy);
        TokenKeyResolver resolver = Resolver(first, second);

        TokenValidationResult flattenedResult = UniversalToken.Validate(
            flattened.Text,
            resolver,
            policy);
        TokenValidationResult generalResult = UniversalToken.Validate(
            general.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattened.Success, Is.True);
            Assert.That(general.Success, Is.True);
            AssertSuccess(flattenedResult, TokenKind.JwsJson);
            Assert.That(flattenedResult.Subject, Is.EqualTo("json-signed"));
            Assert.That(flattenedResult.KeyIds, Is.EqualTo(new[] { "first" }));
            AssertSuccess(generalResult, TokenKind.JwsJson);
            Assert.That(generalResult.Subject, Is.EqualTo("json-signed"));
            Assert.That(generalResult.KeyIds,
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(generalResult.Algorithms,
                Is.EqualTo(new[]
                {
                    JoseAlgorithms.HmacSha256,
                    JoseAlgorithms.HmacSha256
                }));
            Assert.That(generalResult.Headers, Has.Length.EqualTo(2));
            Assert.That(generalResult.SignatureValidated, Is.True);
        });
    }

    [Test]
    public void ValidateFlattenedAndGeneralJweAuthenticatesSelectedRecipient()
    {
        TokenKey key = DirectKey("recipient", 80);
        TokenPolicy policy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        byte[] claims = Encoding.UTF8.GetBytes("{\"sub\":\"json-encrypted\"}");
        JoseHeader protectedHeader = Header("{\"enc\":\"A128GCM\"}");
        JoseHeader recipientHeader = Header(
            "{\"alg\":\"dir\",\"kid\":\"recipient\"}");
        TokenTextResult flattened = JsonWebEncryptionFlattened.Encrypt(
            claims,
            protectedHeader,
            null,
            recipientHeader,
            key,
            policy);
        TokenTextResult general = JsonWebEncryptionGeneral.Encrypt(
            claims,
            protectedHeader,
            null,
            new JoseHeader[] { recipientHeader },
            new TokenKey[] { key },
            policy);
        TokenKeyResolver resolver = Resolver(key);

        TokenValidationResult flattenedResult = UniversalToken.Validate(
            flattened.Text,
            resolver,
            policy);
        TokenValidationResult generalResult = UniversalToken.Validate(
            general.Text,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(flattened.Success, Is.True);
            Assert.That(general.Success, Is.True);
            AssertSuccess(flattenedResult, TokenKind.JweJson);
            Assert.That(flattenedResult.Subject, Is.EqualTo("json-encrypted"));
            Assert.That(flattenedResult.EncryptionValidated, Is.True);
            AssertSuccess(generalResult, TokenKind.JweJson);
            Assert.That(generalResult.Subject, Is.EqualTo("json-encrypted"));
            Assert.That(generalResult.KeyId, Is.EqualTo("recipient"));
            Assert.That(generalResult.Headers, Has.Length.EqualTo(2));
            Assert.That(generalResult.EncryptionValidated, Is.True);
        });
    }

    [Test]
    public void ValidateSdJwtAuthenticatesIssuerAndProcessesDisclosures()
    {
        TokenKey key = HmacKey("issuer", 1);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        string disclosure = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "[\"salt\",\"role\",\"admin\"]")).Text;
        string digest = Base64Url.Encode(SHA256.HashData(
            Encoding.ASCII.GetBytes(disclosure))).Text;
        TokenTextResult issuer = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(
                "{\"sub\":\"selective\",\"_sd\":[\""
                + digest + "\"]}"),
            Header("{\"alg\":\"HS256\",\"typ\":\"example+sd-jwt\",\"kid\":\"issuer\"}"),
            key,
            policy);

        TokenValidationResult result = UniversalToken.Validate(
            issuer.Text + "~" + disclosure + "~",
            Resolver(key),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(issuer.Success, Is.True);
            AssertSuccess(result, TokenKind.SdJwt);
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Subject, Is.EqualTo("selective"));
            Assert.That(result.JwtClaims.Get("role").StringValue,
                Is.EqualTo("admin"));
            Assert.That(result.JwtClaims.Get("_sd"), Is.Null);
            Assert.That(result.KeyId, Is.EqualTo("issuer"));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.Nested, Is.True);
        });
    }

    [Test]
    public void ValidateNestedCompactJwtReturnsOuterAndInnerMetadata()
    {
        TokenKey signing = HmacKey("signing", 1);
        TokenKey encryption = DirectKey("encryption", 80);
        TokenPolicy policy = Policy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        TokenTextResult written = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"sub\":\"nested\"}").Claims,
            signing,
            encryption,
            JoseAlgorithms.Aes128Gcm,
            policy);

        TokenValidationResult result = UniversalToken.Validate(
            written.Text,
            Resolver(signing, encryption),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.JweCompact);
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Subject, Is.EqualTo("nested"));
            Assert.That(result.Headers, Has.Length.EqualTo(2));
            Assert.That(result.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(result.Headers[1].Role,
                Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(result.Algorithm,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.KeyManagementAlgorithm,
                Is.EqualTo(JoseAlgorithms.Direct));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
        });
    }

    [Test]
    public void ValidationRejectsTamperingUnknownDetachedAndMissingClaims()
    {
        TokenKey key = HmacKey("key", 1);
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}"),
            Header("{\"alg\":\"HS256\",\"kid\":\"key\"}"),
            key,
            policy);
        string tampered = signed.Text.Substring(0, signed.Text.Length - 1)
            + (signed.Text.EndsWith("A", StringComparison.Ordinal) ? "B" : "A");
        TokenTextResult detached = JsonWebSignatureFlattened.SignDetached(
            Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}"),
            Header("{\"alg\":\"HS256\",\"kid\":\"key\"}"),
            null,
            key,
            policy);
        TokenPolicy required = Policy(JoseAlgorithms.HmacSha256);
        required.RequiredClaims = new[] { "iss" };

        TokenValidationResult badSignature = UniversalToken.Validate(
            tampered,
            Resolver(key),
            policy);
        TokenValidationResult unknown = UniversalToken.Validate(
            "opaque",
            Resolver(key),
            policy);
        TokenValidationResult detachedResult = UniversalToken.Validate(
            detached.Text,
            Resolver(key),
            policy);
        TokenValidationResult missing = UniversalToken.Validate(
            signed.Text,
            Resolver(key),
            required);

        Assert.Multiple(() =>
        {
            AssertFailure(badSignature);
            AssertFailure(unknown);
            AssertFailure(detachedResult, "detached_payload");
            AssertFailure(missing, "missing_required_claim");
        });
    }

    [Test]
    public void ValidateRejectsNullArguments()
    {
        TokenPolicy policy = new TokenPolicy();
        TokenKeyResolver resolver = Resolver(HmacKey("key", 1));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => UniversalToken.Validate(null, resolver, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.Validate("token", null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.Validate("token", resolver, null),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey HmacKey(string keyId, int seed)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, seed),
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = keyId;
        return key;
    }

    static TokenKey DirectKey(string keyId, int seed)
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(16, seed)).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        key.KeyId = keyId;
        return key;
    }

    static TokenKeyResolver Resolver(params TokenKey[] keys)
    {
        return new TokenKeyResolver(context =>
        {
            TokenKey key = keys.FirstOrDefault(value => string.Equals(
                value.KeyId,
                context.KeyId,
                StringComparison.Ordinal));
            return key == null
                ? KeyFailure("key_not_found")
                : KeySuccess(key);
        });
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

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static byte[] Sequence(int count, int seed)
    {
        return Enumerable.Range(seed, count)
            .Select(value => (byte)value)
            .ToArray();
    }

    static void AssertSuccess(TokenValidationResult result, TokenKind kind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.JwtClaims, Is.Not.Null);
        });
    }

    static void AssertFailure(TokenValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.JwtClaims, Is.Null);
            Assert.That(result.CwtClaims, Is.Null);
        });
    }

    static void AssertFailure(TokenValidationResult result, string code)
    {
        AssertFailure(result);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
