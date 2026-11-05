using System.Text;
using token;

namespace token.Tests;

public class ResourceLimitNegativeTests
{
    [Test]
    public void OversizedJwtAndCwtAreRejectedBeforeKeyResolution()
    {
        TokenKey joseKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        TokenPolicy joseCreationPolicy = Policy(
            JoseAlgorithms.HmacSha256);
        TokenTextResult jwt = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"subject\"}"),
            JoseHeader.Read("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")
                .Header,
            joseKey,
            joseCreationPolicy);
        TokenKey coseKey = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 40),
            CoseAlgorithms.HmacSha256).Key;
        TokenPolicy coseCreationPolicy = Policy("HMAC 256/256");
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        TokenBytesResult cwt = CborWebToken.Create(
            claims,
            coseKey,
            coseCreationPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(joseKey);
        });
        TokenPolicy joseLimited = Policy(JoseAlgorithms.HmacSha256);
        joseLimited.MaximumTokenSizeInBytes =
            Encoding.UTF8.GetByteCount(jwt.Text) - 1;
        TokenPolicy coseLimited = Policy("HMAC 256/256");
        coseLimited.MaximumTokenSizeInBytes = cwt.Bytes.Length - 1;

        JwtValidationResult jwtResult = JwtValidator.ValidateSigned(
            jwt.Text,
            resolver,
            JwtValidationPolicy.Create(joseLimited));
        CwtValidationResult cwtResult = CborWebToken.Validate(
            cwt.Bytes,
            resolver,
            CwtValidationPolicy.Create(coseLimited));

        Assert.Multiple(() =>
        {
            Assert.That(jwt.Success, Is.True);
            Assert.That(cwt.Success, Is.True);
            AssertJwtFailure(jwtResult, "token_too_large");
            AssertCwtFailure(cwtResult, "cbor_size_exceeded");
            Assert.That(resolverCalls, Is.Zero);
        });

        joseKey.ReleaseOwnedMaterial();
        coseKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void ExcessivelyNestedJwtClaimsAreRejectedBeforeKeyResolution()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        TokenPolicy creationPolicy = Policy(JoseAlgorithms.HmacSha256);
        TokenTextResult token = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes(
                "{\"private\":{\"nested\":{\"value\":true}}}"),
            JoseHeader.Read("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")
                .Header,
            key,
            creationPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });
        TokenPolicy limited = Policy(JoseAlgorithms.HmacSha256);
        limited.MaximumJsonDepth = 2;

        JwtValidationResult result = JwtValidator.ValidateSigned(
            token.Text,
            resolver,
            JwtValidationPolicy.Create(limited));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            AssertJwtFailure(result, "json_depth_exceeded");
            Assert.That(resolverCalls, Is.Zero);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ExcessiveCwtProtectionLayersAreRejectedWithoutClaims()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256).Key;
        TokenPolicy tokenPolicy = Policy("HMAC 256/256");
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        CwtProtectionLayer first = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtProtectionLayer second = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtProtectionLayer third = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        TokenBytesResult token = CborWebToken.CreateNested(
            claims,
            new[] { first, second, third },
            false,
            tokenPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });
        CwtValidationPolicy validationPolicy = CwtValidationPolicy.Create(
            tokenPolicy);
        validationPolicy.MaximumProtectionLayers = 2;

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            resolver,
            validationPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            AssertCwtFailure(
                result,
                "cwt_protection_layer_limit_exceeded");
            Assert.That(resolverCalls, Is.EqualTo(2));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void CompressedJwtExpansionIsRejectedWithoutClaims()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, 1),
            JoseAlgorithms.Direct).Key;
        TokenPolicy creationPolicy = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        creationPolicy.AllowCompression = true;
        creationPolicy.MaximumCompressionRatio = 1000;
        string claims = "{\"sub\":\"subject\",\"data\":\""
            + new string('A', 4096) + "\"}";
        TokenTextResult token = JsonWebEncryption.Encrypt(
            Encoding.UTF8.GetBytes(claims),
            JoseHeader.Read(
                "{\"alg\":\"dir\",\"enc\":\"A128GCM\","
                + "\"zip\":\"DEF\",\"typ\":\"JWT\"}").Header,
            key,
            creationPolicy);
        int resolverCalls = 0;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            resolverCalls += 1;
            return KeySuccess(key);
        });
        TokenPolicy limited = Policy(
            JoseAlgorithms.Direct,
            JoseAlgorithms.Aes128Gcm);
        limited.AllowCompression = true;
        limited.MaximumDecompressedSizeInBytes = 8192;
        limited.MaximumCompressionRatio = 2;
        limited.MaximumTokenSizeInBytes = 8192;

        JwtValidationResult result = JwtValidator.ValidateEncrypted(
            token.Text,
            resolver,
            JwtValidationPolicy.Create(limited));

        Assert.Multiple(() =>
        {
            Assert.That(token.Success, Is.True);
            Assert.That(token.Text.Length, Is.LessThan(claims.Length));
            AssertJwtFailure(result, "decompression_limit_exceeded");
            Assert.That(resolverCalls, Is.EqualTo(1));
        });

        key.ReleaseOwnedMaterial();
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

    static CoseHeader AlgorithmHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Integer(algorithm));
        return header;
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

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
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
