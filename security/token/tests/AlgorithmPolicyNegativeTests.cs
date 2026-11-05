using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class AlgorithmPolicyNegativeTests
{
    [Test]
    public void JwtAndCwtValidationRejectWeakResolverKeys()
    {
        TokenKey strong = SymmetricKeyImporterOperations.Import(
            Sequence(32, 1)).Key;
        TokenKey weak = SymmetricKeyImporterOperations.Import(
            Sequence(16, 80)).Key;
        TokenPolicy josePolicy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult jwt = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"subject\"}"),
            JoseHeader.Read("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")
                .Header,
            strong,
            josePolicy);
        TokenKey coseStrong = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256).Key;
        TokenPolicy cosePolicy = Policy("HMAC 256/256");
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        TokenBytesResult cwt = CborWebToken.Create(
            claims,
            coseStrong,
            cosePolicy);

        JwtValidationResult jwtResult = JwtValidator.ValidateSigned(
            jwt.Text,
            Resolver(weak),
            JwtValidationPolicy.Create(josePolicy));
        CwtValidationResult cwtResult = CborWebToken.Validate(
            cwt.Bytes,
            Resolver(weak),
            CwtValidationPolicy.Create(cosePolicy));

        Assert.Multiple(() =>
        {
            Assert.That(jwt.Success, Is.True);
            Assert.That(cwt.Success, Is.True);
            AssertJwtFailure(jwtResult, "weak_key");
            AssertCwtFailure(cwtResult, "weak_key");
        });

        strong.ReleaseOwnedMaterial();
        weak.ReleaseOwnedMaterial();
        coseStrong.ReleaseOwnedMaterial();
    }

    [Test]
    public void ProhibitedJoseAlgorithmsCannotBeEnabledByCompatibility()
    {
        TokenKeyResult hmac = SymmetricKeyImporterOperations.ImportJose(
            Sequence(20, 1),
            JoseAlgorithms.HmacSha1,
            true);
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult signature = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha1,
            true,
            true);

        Assert.Multiple(() =>
        {
            AssertKeyFailure(hmac, "prohibited_algorithm");
            AssertKeyFailure(signature, "prohibited_algorithm");
        });
    }

    [Test]
    public void JwtValidationRequiresExplicitLegacyRsaEncryptionOptIn()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;
        TokenPolicy creationPolicy = Policy(
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm);
        creationPolicy.AllowLegacyRsaPkcs1Encryption = true;
        TokenTextResult token = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes("{\"sub\":\"subject\"}"),
            JoseHeader.Read(
                "{\"alg\":\"RSA1_5\",\"enc\":\"A128GCM\","
                + "\"typ\":\"JWT\"}").Header,
            key,
            creationPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });
        TokenPolicy conservativePolicy = Policy(
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm);

        JwtValidationResult result = JwtValidator.ValidateEncrypted(
            token.Text,
            resolver,
            JwtValidationPolicy.Create(conservativePolicy));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            AssertJwtFailure(result, "compatibility_required");
            Assert.That(resolverCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void CwtValidationRequiresExplicitLegacyEcdsaOptIn()
    {
        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ec,
            CoseAlgorithms.EcdsaSha256,
            true,
            true).Key;
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Integer(CoseAlgorithms.EcdsaSha256));
        CwtProtectionLayer layer = CwtProtectionLayer.CreateSign1(
            header,
            key);
        layer.AllowCompatibility = true;
        TokenPolicy tokenPolicy = Policy(
            CoseAlgorithmsOperations.GetName(CoseAlgorithms.EcdsaSha256));
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        TokenBytesResult token = CborWebToken.Create(
            claims,
            layer,
            false,
            tokenPolicy);

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            Resolver(key),
            CwtValidationPolicy.Create(tokenPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            AssertCwtFailure(result, "compatibility_required");
        });
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        return bytes;
    }

    static CborItem Integer(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static TokenKeyResolver Resolver(TokenKey key)
    {
        return new TokenKeyResolver(context => KeySuccess(key));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static void AssertKeyFailure(TokenKeyResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Key, Is.Null);
        AssertError(result.Error, code);
    }

    static void AssertJwtFailure(JwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.SignatureValidated, Is.False);
        Assert.That(result.EncryptionValidated, Is.False);
        AssertError(result.Error, code);
    }

    static void AssertCwtFailure(CwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
        Assert.That(result.Headers, Is.Empty);
        AssertError(result.Error, code);
    }

    static void AssertError(TokenError error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo(code));
    }
}
