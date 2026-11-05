using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtEncryptedCreatorTests
{
    [Test]
    public void CreateEncryptedBuildsAStandardJwtAndDecryptsItsClaims()
    {
        JwtClaims claims = JwtClaims.Read(
            "{\"iss\":\"https://issuer.example\",\"sub\":\"subject\",\"exp\":1700000000}")
            .Claims;
        TokenKey key = DirectKey("key-1");
        TokenPolicy policy = EncryptionPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        TokenTextResult created = JsonWebToken.CreateEncrypted(
            claims,
            key,
            JoseAlgorithms.Aes128Gcm,
            policy);
        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            created.Text,
            key,
            policy);
        string[] segments = created.Text.Split('.');
        string headerJson = Decode(segments[0]);
        string claimsJson = Encoding.UTF8.GetString(decrypted.Token.Plaintext);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Error, Is.Null);
            Assert.That(segments, Has.Length.EqualTo(5));
            Assert.That(headerJson, Is.EqualTo(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"typ\":\"JWT\",\"kid\":\"key-1\"}"));
            Assert.That(segments[1], Is.Empty);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Token.AuthenticationTagVerified, Is.True);
            Assert.That(claimsJson, Is.EqualTo(
                "{\"iss\":\"https://issuer.example\",\"sub\":\"subject\",\"exp\":1700000000}"));
            Assert.That(JwtClaims.Read(decrypted.Token.Plaintext).Success, Is.True);
        });
    }

    [Test]
    public void ExplicitHeaderIsCopiedPreservedAndCompletedWithoutMutation()
    {
        JwtClaims claims = JwtClaims.Read("{\"aud\":\"api\"}").Claims;
        TokenKey key = DirectKey("key-default");
        JoseHeader header = JoseHeader.Read(
            "{\"enc\":\"A128GCM\",\"typ\":\"at+jwt\",\"kid\":\"key-explicit\",\"private\":true}")
            .Header;

        TokenTextResult created = JwtCreator.CreateEncrypted(
            claims,
            key,
            header,
            EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm));
        string protectedJson = Decode(created.Text.Split('.')[0]);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(protectedJson, Is.EqualTo(
                "{\"enc\":\"A128GCM\",\"typ\":\"at+jwt\",\"kid\":\"key-explicit\",\"private\":true,\"alg\":\"dir\"}"));
            Assert.That(header.Get(JoseHeaderNames.Algorithm), Is.Null);
            Assert.That(header.Parameters.Members, Has.Length.EqualTo(4));
            Assert.That(header.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("at+jwt"));
            Assert.That(header.Get(JoseHeaderNames.KeyId).StringValue,
                Is.EqualTo("key-explicit"));
        });
    }

    [Test]
    public void ExplicitAlgorithmSupportsKeysWithoutAlgorithmMetadata()
    {
        TokenKey key = DirectKey(null);
        key.Algorithm = null;
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header;
        TokenPolicy policy = EncryptionPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        TokenTextResult created = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"jti\":\"id-1\"}").Claims,
            key,
            header,
            policy);
        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            created.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(Decode(created.Text.Split('.')[0]), Is.EqualTo(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"typ\":\"JWT\"}"));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(decrypted.Token.Plaintext),
                Is.EqualTo("{\"jti\":\"id-1\"}"));
        });
    }

    [Test]
    public void CreateEncryptedSupportsRsaOaepThroughTheJwtFacade()
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
        TokenPolicy policy = EncryptionPolicy(
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        TokenTextResult created = JsonWebToken.CreateEncrypted(
            JwtClaims.Read("{\"sub\":\"subject\"}").Claims,
            publicKey,
            JoseAlgorithms.Aes256Gcm,
            policy);
        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            created.Text,
            privateKey,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Text.Split('.')[1], Is.Not.Empty);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(decrypted.Token.Plaintext),
                Is.EqualTo("{\"sub\":\"subject\"}"));
            Assert.That(decrypted.Token.Algorithm,
                Is.EqualTo(JoseAlgorithms.RsaOaepSha256));
            Assert.That(decrypted.Token.EncryptionAlgorithm,
                Is.EqualTo(JoseAlgorithms.Aes256Gcm));
            Assert.That(decrypted.Token.KeyId, Is.EqualTo("rsa-key"));
        });
    }

    [Test]
    public void CreateEncryptedSupportsPolicyGatedCompression()
    {
        string repeated = new string('a', 500);
        JwtClaims claims = JwtClaims.Read(
            "{\"sub\":\"subject\",\"private\":\"" + repeated + "\"}").Claims;
        TokenKey key = DirectKey(null);
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\",\"zip\":\"DEF\"}").Header;
        TokenPolicy policy = EncryptionPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        policy.AllowCompression = true;

        TokenTextResult created = JwtCreator.CreateEncrypted(
            claims,
            key,
            header,
            policy);
        JweCompactResult decrypted = JsonWebEncryption.Decrypt(
            created.Text,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(JwtClaims.Read(decrypted.Token.Plaintext).Claims
                .Get("private").StringValue, Is.EqualTo(repeated));
        });
    }

    [Test]
    public void CreateEncryptedPropagatesClaimHeaderAlgorithmPolicyAndKeyFailures()
    {
        JwtClaims invalidClaims = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember(JwtClaimNames.ExpirationTime, StringValue("tomorrow"))
        }));
        JoseHeader invalidHeader = new JoseHeader();
        invalidHeader.Parameters = null;
        TokenKey key = DirectKey("key-1");
        TokenKey missingAlgorithm = DirectKey(null);
        missingAlgorithm.Algorithm = null;
        TokenKey restricted = DirectKey(null);
        restricted.KeyOperations = new[] { TokenKeyOperations.Decrypt };
        JoseHeader complete = JoseHeader.Read(
            "{\"alg\":\"dir\",\"enc\":\"A128GCM\"}").Header;
        JoseHeader missingEncryption = JoseHeader.Read("{\"alg\":\"dir\"}").Header;
        TokenPolicy denied = new TokenPolicy();
        denied.AllowedAlgorithms = Array.Empty<string>();
        TokenPolicy small = EncryptionPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        small.MaximumTokenSizeInBytes = 10;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    invalidClaims,
                    key,
                    JoseAlgorithms.Aes128Gcm,
                    EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "invalid_claim_type");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{}").Claims,
                    key,
                    invalidHeader,
                    EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "invalid_header");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{}").Claims,
                    missingAlgorithm,
                    JoseAlgorithms.Aes128Gcm,
                    EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "missing_algorithm");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{}").Claims,
                    key,
                    missingEncryption,
                    EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "missing_encryption_algorithm");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{}").Claims,
                    key,
                    complete,
                    denied),
                "algorithm_not_allowed");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{}").Claims,
                    restricted,
                    complete,
                    EncryptionPolicy(JoseAlgorithms.Direct, JoseAlgorithms.Aes128Gcm)),
                "key_operation_not_permitted");
            AssertFailure(
                JsonWebToken.CreateEncrypted(
                    JwtClaims.Read("{\"sub\":\"subject\"}").Claims,
                    key,
                    complete,
                    small),
                "token_too_large");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        JwtClaims claims = new JwtClaims();
        TokenKey key = DirectKey(null);
        JoseHeader header = new JoseHeader();
        TokenPolicy policy = EncryptionPolicy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => JwtCreator.CreateEncrypted(
                    null,
                    key,
                    JoseAlgorithms.Aes128Gcm,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtCreator.CreateEncrypted(
                    claims,
                    null,
                    JoseAlgorithms.Aes128Gcm,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtCreator.CreateEncrypted(claims, key, (string)null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtCreator.CreateEncrypted(
                    claims,
                    key,
                    JoseAlgorithms.Aes128Gcm,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JsonWebToken.CreateEncrypted(
                    claims,
                    key,
                    (JoseHeader)null,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JsonWebToken.CreateEncrypted(claims, key, header, null),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey DirectKey(string keyId)
    {
        byte[] bytes = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.Import(bytes).Key;
        key.Algorithm = JoseAlgorithms.Direct;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy EncryptionPolicy(string algorithm, string encryption)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[] { algorithm, encryption };
        return policy;
    }

    static string Decode(string segment)
    {
        return Encoding.UTF8.GetString(Base64Url.Decode(segment).Bytes);
    }

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static void AssertFailure(TokenTextResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
