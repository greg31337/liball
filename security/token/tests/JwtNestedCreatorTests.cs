using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtNestedCreatorTests
{
    [Test]
    public void CreateSignedThenEncryptedProtectsAndAuthenticatesBothLayers()
    {
        JwtClaims claims = JwtClaims.Read(
            "{\"iss\":\"https://issuer.example\",\"sub\":\"subject\",\"exp\":1700000000}")
            .Claims;
        TokenKey signingKey = HmacKey("signing-key");
        TokenKey encryptionKey = DirectKey("encryption-key");
        TokenPolicy policy = NestedPolicy();

        TokenTextResult created = JsonWebToken.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            JoseAlgorithms.Aes128Gcm,
            policy);
        JweCompactResult outer = JsonWebEncryption.Decrypt(
            created.Text,
            encryptionKey,
            policy);
        string innerText = Encoding.UTF8.GetString(outer.Token.Plaintext);
        JwsCompactResult inner = JsonWebSignature.Verify(
            innerText,
            signingKey,
            policy);
        JwtClaimsResult verifiedClaims = JwtClaims.Read(inner.Token.Payload);
        NestedJoseResult nested = NestedJose.Read(outer, policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Error, Is.Null);
            Assert.That(created.Text.Split('.'), Has.Length.EqualTo(5));
            Assert.That(outer.Success, Is.True);
            Assert.That(outer.Token.AuthenticationTagVerified, Is.True);
            Assert.That(outer.Token.ProtectedHeader.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("JWT"));
            Assert.That(outer.Token.ProtectedHeader.Get(JoseHeaderNames.ContentType).StringValue,
                Is.EqualTo("JWT"));
            Assert.That(outer.Token.KeyId, Is.EqualTo("encryption-key"));
            Assert.That(innerText.Split('.'), Has.Length.EqualTo(3));
            Assert.That(inner.Success, Is.True);
            Assert.That(inner.Token.SignatureVerified, Is.True);
            Assert.That(inner.Token.KeyId, Is.EqualTo("signing-key"));
            Assert.That(verifiedClaims.Success, Is.True);
            Assert.That(verifiedClaims.Claims.Get(JwtClaimNames.Subject).StringValue,
                Is.EqualTo("subject"));
            Assert.That(nested.Success, Is.True);
            Assert.That(nested.Token.Kind, Is.EqualTo(TokenKind.JwsCompact));
        });
    }

    [Test]
    public void ExplicitHeadersAreCopiedAndPreservedWithoutMutation()
    {
        JoseHeader signingHeader = JoseHeader.Read(
            "{\"typ\":\"at+jwt\",\"private-sign\":true}").Header;
        JoseHeader encryptionHeader = JoseHeader.Read(
            "{\"enc\":\"A128GCM\",\"cty\":\"application/jwt\",\"private-encrypt\":true}")
            .Header;
        TokenKey signingKey = HmacKey("sign-default");
        TokenKey encryptionKey = DirectKey("encrypt-default");
        TokenPolicy policy = NestedPolicy();

        TokenTextResult created = JwtCreator.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"aud\":\"api\"}").Claims,
            signingKey,
            encryptionKey,
            signingHeader,
            encryptionHeader,
            policy);
        JweCompactResult outer = JsonWebEncryption.Decrypt(
            created.Text,
            encryptionKey,
            policy);
        JwsCompactResult inner = JsonWebSignature.Verify(
            Encoding.UTF8.GetString(outer.Token.Plaintext),
            signingKey,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(Decode(created.Text.Split('.')[0]), Is.EqualTo(
                "{\"enc\":\"A128GCM\",\"cty\":\"application/jwt\",\"private-encrypt\":true,\"alg\":\"dir\",\"typ\":\"JWT\",\"kid\":\"encrypt-default\"}"));
            Assert.That(Decode(Encoding.UTF8.GetString(outer.Token.Plaintext)
                .Split('.')[0]), Is.EqualTo(
                "{\"typ\":\"at+jwt\",\"private-sign\":true,\"alg\":\"HS256\",\"kid\":\"sign-default\"}"));
            Assert.That(inner.Success, Is.True);
            Assert.That(signingHeader.Get(JoseHeaderNames.Algorithm), Is.Null);
            Assert.That(signingHeader.Get(JoseHeaderNames.KeyId), Is.Null);
            Assert.That(signingHeader.Parameters.Members, Has.Length.EqualTo(2));
            Assert.That(encryptionHeader.Get(JoseHeaderNames.Algorithm), Is.Null);
            Assert.That(encryptionHeader.Get(JoseHeaderNames.Type), Is.Null);
            Assert.That(encryptionHeader.Get(JoseHeaderNames.KeyId), Is.Null);
            Assert.That(encryptionHeader.Parameters.Members, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void ExplicitAlgorithmsSupportKeysWithoutAlgorithmMetadata()
    {
        TokenKey signingKey = HmacKey(null);
        signingKey.Algorithm = null;
        TokenKey encryptionKey = DirectKey(null);
        encryptionKey.Algorithm = null;
        JoseHeader signingHeader = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;
        JoseHeader encryptionHeader = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header;
        TokenPolicy policy = NestedPolicy();

        TokenTextResult created = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"jti\":\"id-1\"}").Claims,
            signingKey,
            encryptionKey,
            signingHeader,
            encryptionHeader,
            policy);
        JweCompactResult outer = JsonWebEncryption.Decrypt(
            created.Text,
            encryptionKey,
            policy);
        JwsCompactResult inner = JsonWebSignature.Verify(
            Encoding.UTF8.GetString(outer.Token.Plaintext),
            signingKey,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(outer.Success, Is.True);
            Assert.That(inner.Success, Is.True);
            Assert.That(inner.Token.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(outer.Token.Algorithm, Is.EqualTo(JoseAlgorithms.Direct));
        });
    }

    [Test]
    public void NestedCreationSupportsRsaForBothLayers()
    {
        using RSA signingRsa = RSA.Create(2048);
        using RSA encryptionPrivate = RSA.Create(2048);
        using RSA encryptionPublic = RSA.Create();
        encryptionPublic.ImportParameters(encryptionPrivate.ExportParameters(false));
        TokenKey signingKey = RsaKeyImporterOperations.ImportJose(
            signingRsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        TokenKey encryptionPublicKey = RsaKeyImporterOperations.ImportJose(
            encryptionPublic,
            JoseAlgorithms.RsaOaepSha256,
            false).Key;
        TokenKey encryptionPrivateKey = RsaKeyImporterOperations.ImportJose(
            encryptionPrivate,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.RsaSha256,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm
        };

        TokenTextResult created = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read("{\"sub\":\"rsa-subject\"}").Claims,
            signingKey,
            encryptionPublicKey,
            JoseAlgorithms.Aes256Gcm,
            policy);
        JweCompactResult outer = JsonWebEncryption.Decrypt(
            created.Text,
            encryptionPrivateKey,
            policy);
        JwsCompactResult inner = JsonWebSignature.Verify(
            Encoding.UTF8.GetString(outer.Token.Plaintext),
            signingKey,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(outer.Success, Is.True);
            Assert.That(inner.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(inner.Token.Payload),
                Is.EqualTo("{\"sub\":\"rsa-subject\"}"));
        });
    }

    [TestCase("JOSE")]
    [TestCase("at+jwt")]
    [TestCase("application/example")]
    public void NestedCreationRejectsOuterContentTypesThatDoNotIdentifyJwt(
        string contentType)
    {
        JoseHeader encryptionHeader = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"cty\":\""
            + contentType + "\"}").Header;

        TokenTextResult result = JsonWebToken.CreateSignedThenEncrypted(
            JwtClaims.Read("{}").Claims,
            HmacKey(null),
            DirectKey(null),
            JoseHeader.Read("{\"alg\":\"HS256\"}").Header,
            encryptionHeader,
            NestedPolicy());

        AssertFailure(result, "invalid_nested_content_type");
    }

    [Test]
    public void NestedCreationPropagatesInnerAndOuterFailures()
    {
        JwtClaims claims = JwtClaims.Read("{\"sub\":\"subject\"}").Claims;
        TokenKey signingKey = HmacKey(null);
        TokenKey encryptionKey = DirectKey(null);
        TokenPolicy missingSigning = new TokenPolicy();
        missingSigning.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm
        };
        TokenPolicy missingEncryption = new TokenPolicy();
        missingEncryption.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct
        };
        JoseHeader signingHeader = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;
        JoseHeader encryptionHeader = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header;
        TokenTextResult valid = JsonWebToken.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            signingHeader,
            encryptionHeader,
            NestedPolicy());
        TokenPolicy small = NestedPolicy();
        small.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(valid.Text) - 1;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.CreateSignedThenEncrypted(
                    claims,
                    signingKey,
                    encryptionKey,
                    signingHeader,
                    encryptionHeader,
                    missingSigning),
                "algorithm_not_allowed");
            AssertFailure(
                JsonWebToken.CreateSignedThenEncrypted(
                    claims,
                    signingKey,
                    encryptionKey,
                    signingHeader,
                    encryptionHeader,
                    missingEncryption),
                "algorithm_not_allowed");
            AssertFailure(
                JsonWebToken.CreateSignedThenEncrypted(
                    claims,
                    signingKey,
                    encryptionKey,
                    signingHeader,
                    encryptionHeader,
                    small),
                "token_too_large");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        JwtClaims claims = new JwtClaims();
        TokenKey signingKey = HmacKey(null);
        TokenKey encryptionKey = DirectKey(null);
        JoseHeader signingHeader = new JoseHeader();
        JoseHeader encryptionHeader = JoseHeader.Read(
            "{\"enc\":\"A128GCM\"}").Header;
        TokenPolicy policy = NestedPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                null, signingKey, encryptionKey, signingHeader, encryptionHeader, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                claims, null, encryptionKey, signingHeader, encryptionHeader, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                claims, signingKey, null, signingHeader, encryptionHeader, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                claims, signingKey, encryptionKey, null, encryptionHeader, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                claims, signingKey, encryptionKey, signingHeader, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JwtCreator.CreateSignedThenEncrypted(
                claims, signingKey, encryptionKey, signingHeader, encryptionHeader, null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebToken.CreateSignedThenEncrypted(
                claims, signingKey, encryptionKey, (string)null, policy),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey HmacKey(string keyId)
    {
        byte[] bytes = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            bytes,
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = keyId;
        return key;
    }

    static TokenKey DirectKey(string keyId)
    {
        byte[] bytes = Enumerable.Range(65, 16).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.Import(bytes).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy NestedPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm
        };
        return policy;
    }

    static string Decode(string segment)
    {
        return Encoding.UTF8.GetString(Base64Url.Decode(segment).Bytes);
    }

    static void AssertFailure(TokenTextResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
