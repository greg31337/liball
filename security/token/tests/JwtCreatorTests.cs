using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JwtCreatorTests
{
    [Test]
    public void CreateSignedBuildsAStandardJwtAndValidSignature()
    {
        JwtClaims claims = JwtClaims.Read(
            "{\"iss\":\"https://issuer.example\",\"sub\":\"subject\",\"exp\":1700000000}")
            .Claims;
        TokenKey key = SigningKey("key-1");
        TokenPolicy policy = SigningPolicy();

        TokenTextResult created = JsonWebToken.CreateSigned(claims, key, policy);
        JwsCompactResult verified = JsonWebSignature.Verify(created.Text, key, policy);
        string[] segments = created.Text.Split('.');
        string headerJson = Decode(segments[0]);
        string claimsJson = Decode(segments[1]);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Error, Is.Null);
            Assert.That(segments, Has.Length.EqualTo(3));
            Assert.That(headerJson,
                Is.EqualTo("{\"alg\":\"HS256\",\"typ\":\"JWT\",\"kid\":\"key-1\"}"));
            Assert.That(claimsJson,
                Is.EqualTo("{\"iss\":\"https://issuer.example\",\"sub\":\"subject\",\"exp\":1700000000}"));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.SignatureVerified, Is.True);
            Assert.That(Encoding.UTF8.GetString(verified.Token.Payload),
                Is.EqualTo(claimsJson));
        });
    }

    [Test]
    public void ExplicitHeaderIsCopiedPreservedAndCompletedWithoutMutation()
    {
        JwtClaims claims = JwtClaims.Read("{\"aud\":\"api\"}").Claims;
        TokenKey key = SigningKey("key-default");
        JoseHeader header = JoseHeader.Read(
            "{\"typ\":\"at+jwt\",\"kid\":\"key-explicit\",\"private\":true}")
            .Header;

        TokenTextResult created = JwtCreator.CreateSigned(
            claims,
            key,
            header,
            SigningPolicy());
        string headerJson = Decode(created.Text.Split('.')[0]);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(headerJson, Is.EqualTo(
                "{\"typ\":\"at+jwt\",\"kid\":\"key-explicit\",\"private\":true,\"alg\":\"HS256\"}"));
            Assert.That(header.Get(JoseHeaderNames.Algorithm), Is.Null);
            Assert.That(header.Parameters.Members, Has.Length.EqualTo(3));
            Assert.That(header.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("at+jwt"));
            Assert.That(header.Get(JoseHeaderNames.KeyId).StringValue,
                Is.EqualTo("key-explicit"));
        });
    }

    [Test]
    public void ExplicitAlgorithmSupportsKeysWithoutAlgorithmMetadata()
    {
        JwtClaims claims = JwtClaims.Read("{\"jti\":\"id-1\"}").Claims;
        TokenKey key = SigningKey(null);
        key.Algorithm = null;
        JoseHeader header = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;

        TokenTextResult created = JsonWebToken.CreateSigned(
            claims,
            key,
            header,
            SigningPolicy());
        JwsCompactResult verified = JsonWebSignature.Verify(
            created.Text,
            key,
            SigningPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(Decode(created.Text.Split('.')[0]),
                Is.EqualTo("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            Assert.That(verified.Success, Is.True);
        });
    }

    [Test]
    public void CreateSignedSupportsRsaSigningThroughTheJwtFacade()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        key.Algorithm = JoseAlgorithms.RsaSha256;
        key.KeyId = "rsa-key";
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[] { JoseAlgorithms.RsaSha256 };

        TokenTextResult created = JsonWebToken.CreateSigned(
            JwtClaims.Read("{\"sub\":\"subject\"}").Claims,
            key,
            policy);
        JwsCompactResult verified = JsonWebSignature.Verify(created.Text, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.Algorithm, Is.EqualTo(JoseAlgorithms.RsaSha256));
            Assert.That(verified.Token.KeyId, Is.EqualTo("rsa-key"));
        });
    }

    [Test]
    public void CreateSignedRejectsUnencodedJwtPayloads()
    {
        JwtClaims claims = JwtClaims.Read("{\"sub\":\"subject\"}").Claims;
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"HS256\",\"b64\":false,\"crit\":[\"b64\"]}")
            .Header;

        TokenTextResult result = JsonWebToken.CreateSigned(
            claims,
            SigningKey(null),
            header,
            SigningPolicy());

        AssertFailure(result, "unencoded_jwt_payload");
    }

    [Test]
    public void CreateSignedPropagatesClaimHeaderPolicyAndKeyFailures()
    {
        JwtClaims invalidClaims = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember(JwtClaimNames.ExpirationTime, StringValue("tomorrow"))
        }));
        JoseHeader invalidHeader = new JoseHeader();
        invalidHeader.Parameters = null;
        TokenKey key = SigningKey("key-1");
        TokenKey missingAlgorithm = SigningKey(null);
        missingAlgorithm.Algorithm = null;
        byte[] conflictingBytes = Enumerable.Range(1, 48)
            .Select(value => (byte)value)
            .ToArray();
        TokenKey conflictingAlgorithm = SymmetricKeyImporterOperations.ImportJose(
            conflictingBytes,
            JoseAlgorithms.HmacSha384).Key;
        JoseHeader sha256 = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;
        TokenPolicy denied = new TokenPolicy();
        denied.AllowedAlgorithms = Array.Empty<string>();
        TokenPolicy small = SigningPolicy();
        small.MaximumTokenSizeInBytes = 10;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JsonWebToken.CreateSigned(invalidClaims, key, SigningPolicy()),
                "invalid_claim_type");
            AssertFailure(
                JsonWebToken.CreateSigned(
                    JwtClaims.Read("{}").Claims,
                    key,
                    invalidHeader,
                    SigningPolicy()),
                "invalid_header");
            AssertFailure(
                JsonWebToken.CreateSigned(
                    JwtClaims.Read("{}").Claims,
                    missingAlgorithm,
                    SigningPolicy()),
                "missing_algorithm");
            AssertFailure(
                JsonWebToken.CreateSigned(
                    JwtClaims.Read("{}").Claims,
                    key,
                    denied),
                "algorithm_not_allowed");
            AssertFailure(
                JsonWebToken.CreateSigned(
                    JwtClaims.Read("{}").Claims,
                    conflictingAlgorithm,
                    sha256,
                    SigningPolicy()),
                "conflicting_key_algorithm");
            AssertFailure(
                JsonWebToken.CreateSigned(
                    JwtClaims.Read("{\"sub\":\"subject\"}").Claims,
                    key,
                    small),
                "token_too_large");
        });
    }

    [Test]
    public void CreateSignedHonorsKeyOperationRestrictions()
    {
        TokenKey key = SigningKey(null);
        key.KeyOperations = new[] { TokenKeyOperations.Verify };
        JoseHeader header = JoseHeader.Read("{\"alg\":\"HS256\"}").Header;

        TokenTextResult result = JsonWebToken.CreateSigned(
            JwtClaims.Read("{}").Claims,
            key,
            header,
            SigningPolicy());

        AssertFailure(result, "key_operation_not_permitted");
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        JwtClaims claims = new JwtClaims();
        TokenKey key = SigningKey(null);
        JoseHeader header = new JoseHeader();
        TokenPolicy policy = SigningPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => JwtCreator.CreateSigned(null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtCreator.CreateSigned(claims, null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtCreator.CreateSigned(claims, key, (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JsonWebToken.CreateSigned(claims, key, null, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => JsonWebToken.CreateSigned(claims, key, header, null),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey SigningKey(string keyId)
    {
        byte[] bytes = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            bytes,
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = keyId;
        return key;
    }

    static TokenPolicy SigningPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.HmacSha384
        };
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
