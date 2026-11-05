using token;

namespace token.Tests;

public class JoseRegistryTests
{
    [Test]
    public void HeaderRegistryProvidesNamesAndUsageLocations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JoseHeaderNames.Algorithm, Is.EqualTo("alg"));
            Assert.That(JoseHeaderNames.X509Sha256Thumbprint, Is.EqualTo("x5t#S256"));
            Assert.That(JoseHeaderNamesOperations.IsRegistered(JoseHeaderNames.PeerTrustChain), Is.True);
            Assert.That(JoseHeaderNamesOperations.IsForJws(JoseHeaderNames.Base64UrlEncodePayload), Is.True);
            Assert.That(JoseHeaderNamesOperations.IsForJwe(JoseHeaderNames.Base64UrlEncodePayload), Is.False);
            Assert.That(JoseHeaderNamesOperations.IsForJwe(JoseHeaderNames.EncryptionAlgorithm), Is.True);
            Assert.That(JoseHeaderNamesOperations.IsForJws(JoseHeaderNames.EncryptionAlgorithm), Is.False);
            Assert.That(JoseHeaderNamesOperations.IsForJws(JoseHeaderNames.Algorithm), Is.True);
            Assert.That(JoseHeaderNamesOperations.IsForJwe(JoseHeaderNames.Algorithm), Is.True);
        });
    }

    [Test]
    public void HeaderAndAlgorithmNamesAreCaseSensitive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JoseHeaderNamesOperations.IsRegistered("ALG"), Is.False);
            Assert.That(JoseAlgorithmsOperations.IsRegistered("hs256"), Is.False);
            Assert.That(JoseCompressionAlgorithmsOperations.IsRegistered("def"), Is.False);
            Assert.That(JwtClaimNamesOperations.IsRegistered("ISS"), Is.False);
            Assert.That(JoseHeaderNamesOperations.IsRegistered(null), Is.False);
            Assert.That(JoseAlgorithmsOperations.IsRegistered(null), Is.False);
        });
    }

    [Test]
    public void AlgorithmRegistryClassifiesEveryJoseUsageCategory()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JoseAlgorithmsOperations.IsSignature(JoseAlgorithms.HmacSha256), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsSignature(JoseAlgorithms.Ed25519), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsSignature(JoseAlgorithms.MlDsa87), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsKeyManagement(JoseAlgorithms.RsaOaepSha512), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsKeyManagement(JoseAlgorithms.EcdhEsAes256KeyWrap), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsContentEncryption(JoseAlgorithms.Aes256Gcm), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsJwkOnly(JoseAlgorithms.HmacSha1), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsRegistered(JoseAlgorithms.HmacSha1), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsSignature(JoseAlgorithms.Aes256Gcm), Is.False);
            Assert.That(JoseAlgorithmsOperations.IsRegistered("not-registered"), Is.False);
        });
    }

    [Test]
    public void CompressionRegistryContainsDeflate()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JoseCompressionAlgorithms.Deflate, Is.EqualTo("DEF"));
            Assert.That(JoseCompressionAlgorithmsOperations.IsRegistered("DEF"), Is.True);
            Assert.That(JoseCompressionAlgorithmsOperations.IsRegistered("GZIP"), Is.False);
        });
    }

    [Test]
    public void MediaTypeRegistryNormalizesApplicationSubtypeAbbreviations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JoseMediaTypesOperations.IsRegistered("APPLICATION/JWT"), Is.True);
            Assert.That(JoseMediaTypesOperations.IsRegistered(JoseMediaTypes.DpopJwt), Is.True);
            Assert.That(JoseMediaTypesOperations.Normalize("JWT"), Is.EqualTo(JoseMediaTypes.Jwt));
            Assert.That(JoseMediaTypesOperations.Normalize("at+jwt"), Is.EqualTo(JoseMediaTypes.AccessTokenJwt));
            Assert.That(JoseMediaTypesOperations.Normalize("APPLICATION/JOSE+JSON"), Is.EqualTo(JoseMediaTypes.JoseJson));
            Assert.That(JoseMediaTypesOperations.Normalize("unknown+jwt"), Is.Null);
            Assert.That(JoseMediaTypesOperations.Normalize(null), Is.Null);
        });
    }

    [Test]
    public void ClaimRegistryContainsCoreProfileAndCurrentExtensionClaims()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JwtClaimNames.Issuer, Is.EqualTo("iss"));
            Assert.That(JwtClaimNamesOperations.IsRegistered(JwtClaimNames.JwtId), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered(JwtClaimNames.AuthenticationMethodsReferences), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered(JwtClaimNames.Scope), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered("authorization_details"), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered("status_list"), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered("private_claim"), Is.False);
            Assert.That(JwtClaimNamesOperations.IsRegistered(null), Is.False);
        });
    }

    [Test]
    public void RegistryArraysAreDefensiveCopies()
    {
        string[] headers = JoseHeaderNamesOperations.GetAll();
        string[] algorithms = JoseAlgorithmsOperations.GetAll();
        string[] compression = JoseCompressionAlgorithmsOperations.GetAll();
        string[] mediaTypes = JoseMediaTypesOperations.GetAll();
        string[] claims = JwtClaimNamesOperations.GetAll();

        Assert.Multiple(() =>
        {
            Assert.That(headers, Has.Length.EqualTo(33).And.Unique);
            Assert.That(algorithms, Has.Length.EqualTo(53).And.Unique);
            Assert.That(compression, Has.Length.EqualTo(1).And.Unique);
            Assert.That(mediaTypes, Has.Length.EqualTo(29).And.Unique);
            Assert.That(claims, Has.Length.EqualTo(163).And.Unique);
        });

        headers[0] = "changed";
        algorithms[0] = "changed";
        compression[0] = "changed";
        mediaTypes[0] = "changed";
        claims[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(JoseHeaderNamesOperations.IsRegistered("changed"), Is.False);
            Assert.That(JoseAlgorithmsOperations.IsRegistered("changed"), Is.False);
            Assert.That(JoseCompressionAlgorithmsOperations.IsRegistered("changed"), Is.False);
            Assert.That(JoseMediaTypesOperations.IsRegistered("changed"), Is.False);
            Assert.That(JwtClaimNamesOperations.IsRegistered("changed"), Is.False);
            Assert.That(JoseHeaderNamesOperations.IsRegistered(JoseHeaderNames.Algorithm), Is.True);
            Assert.That(JoseAlgorithmsOperations.IsRegistered(JoseAlgorithms.HmacSha256), Is.True);
            Assert.That(JoseCompressionAlgorithmsOperations.IsRegistered(JoseCompressionAlgorithms.Deflate), Is.True);
            Assert.That(JoseMediaTypesOperations.IsRegistered(JoseMediaTypes.AccessTokenJwt), Is.True);
            Assert.That(JwtClaimNamesOperations.IsRegistered(JwtClaimNames.Issuer), Is.True);
        });
    }
}
