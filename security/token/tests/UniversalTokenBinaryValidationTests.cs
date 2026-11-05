using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class UniversalTokenBinaryValidationTests
{
    [Test]
    public void ValidateBytesReturnsUnifiedAuthenticatedCwtMetadata()
    {
        TokenKey key = CoseKey(32, 1, CoseAlgorithms.HmacSha256);
        CoseHeader header = AlgorithmHeader(CoseAlgorithms.HmacSha256);
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 1, 2, 3 }));
        header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/example+cwt"));
        CwtProtectionLayer layer = CwtProtectionLayer.CreateMac0(header, key);
        CwtClaims claims = Claims();
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult written = CborWebToken.Create(
            claims,
            layer,
            true,
            policy);
        TokenKeyResolutionContext observed = null;
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            observed = context;
            return KeySuccess(key);
        });

        TokenValidationResult result = UniversalToken.ValidateBytes(
            written.Bytes,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.Cwt);
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(result.JwtClaims, Is.Null);
            Assert.That(result.CwtClaims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo("alice"));
            Assert.That(result.Headers, Has.Length.EqualTo(1));
            Assert.That(result.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(result.Algorithm, Is.EqualTo("HMAC 256/256"));
            Assert.That(result.Algorithms,
                Is.EqualTo(new[] { "HMAC 256/256" }));
            Assert.That(result.KeyId, Is.EqualTo("AQID"));
            Assert.That(result.KeyIds, Is.EqualTo(new[] { "AQID" }));
            Assert.That(result.TokenType,
                Is.EqualTo("application/example+cwt"));
            Assert.That(result.Issuer, Is.EqualTo("issuer"));
            Assert.That(result.Subject, Is.EqualTo("alice"));
            Assert.That(result.Audiences, Is.EqualTo(new[] { "api" }));
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
            Assert.That(observed.Kind, Is.EqualTo(TokenKind.Cwt));
            Assert.That(observed.Operation,
                Is.EqualTo(TokenKeyOperations.MacVerify));
            Assert.That(observed.KeyIdBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidateBytesAuthenticatesNestedCoseProtectionLayers()
    {
        TokenKey macKey = CoseKey(32, 1, CoseAlgorithms.HmacSha256);
        TokenKey encryptionKey = CoseKey(
            16,
            40,
            CoseAlgorithms.Aes128Gcm);
        CoseHeader macHeader = AlgorithmHeader(CoseAlgorithms.HmacSha256);
        macHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 4 }));
        CoseHeader encryptionHeader = AlgorithmHeader(
            CoseAlgorithms.Aes128Gcm);
        encryptionHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 5 }));
        CwtProtectionLayer inner = CwtProtectionLayer.CreateMac0(
            macHeader,
            macKey);
        CwtProtectionLayer outer = CwtProtectionLayer.CreateEncrypt0(
            encryptionHeader,
            encryptionKey);
        TokenPolicy policy = Policy("HMAC 256/256", "A128GCM");
        TokenBytesResult written = CborWebToken.CreateNested(
            Claims(),
            new CwtProtectionLayer[] { inner, outer },
            false,
            policy);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
            context.Operation == TokenKeyOperations.Decrypt
                ? KeySuccess(encryptionKey)
                : KeySuccess(macKey));

        TokenValidationResult result = UniversalToken.ValidateBytes(
            written.Bytes,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            AssertSuccess(result, TokenKind.Cose);
            Assert.That(result.InnerKind, Is.EqualTo(TokenKind.Cose));
            Assert.That(result.Headers, Has.Length.EqualTo(2));
            Assert.That(result.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(result.Headers[1].Role,
                Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(result.Algorithm, Is.EqualTo("HMAC 256/256"));
            Assert.That(result.EncryptionAlgorithm, Is.EqualTo("A128GCM"));
            Assert.That(result.KeyIds, Is.EqualTo(new[] { "BQ", "BA" }));
            Assert.That(result.TokenType, Is.EqualTo("application/cwt"));
            Assert.That(result.SignatureValidated, Is.True);
            Assert.That(result.EncryptionValidated, Is.True);
            Assert.That(result.Nested, Is.True);
        });

        macKey.ReleaseOwnedMaterial();
        encryptionKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidateBytesFallsBackToUtf8TextValidation()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = "jwt-key";
        TokenPolicy policy = Policy(JoseAlgorithms.HmacSha256);
        JoseHeader header = JoseHeader.Read(
            "{\"alg\":\"HS256\",\"kid\":\"jwt-key\"}").Header;
        TokenTextResult written = JsonWebSignature.Sign(
            Encoding.UTF8.GetBytes("{\"sub\":\"text\"}"),
            header,
            key,
            policy);

        TokenValidationResult result = UniversalToken.ValidateBytes(
            Encoding.UTF8.GetBytes(written.Text),
            new TokenKeyResolver(context => KeySuccess(key)),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Subject, Is.EqualTo("text"));
            Assert.That(result.SignatureValidated, Is.True);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidateBytesMapsGeneralCoseParticipantMetadata()
    {
        using ECDsa firstNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey first = EcdsaKeyImporter.Import(firstNative, true).Key;
        TokenKey second = EcdsaKeyImporter.Import(secondNative, true).Key;
        CoseHeader firstHeader = AlgorithmHeader(
            CoseAlgorithms.EcdsaP256Sha256);
        firstHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 6 }));
        CoseHeader secondHeader = AlgorithmHeader(
            CoseAlgorithms.EcdsaP256Sha256);
        secondHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 7 }));
        CwtProtectionLayer sign = CwtProtectionLayer.CreateSign(
            new CoseHeader(),
            new CoseHeader[] { firstHeader, secondHeader },
            new TokenKey[] { first, second });
        TokenKey encryption = CoseKey(16, 80, CoseAlgorithms.Direct);
        CoseHeader encryptionHeader = AlgorithmHeader(
            CoseAlgorithms.Aes128Gcm);
        CoseHeader recipientHeader = new CoseHeader();
        recipientHeader.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Integer(CoseAlgorithms.Direct));
        recipientHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 8 }));
        CwtProtectionLayer encrypt = CwtProtectionLayer.CreateEncrypt(
            encryptionHeader,
            new CoseHeader[] { recipientHeader },
            new TokenKey[] { encryption },
            null);
        TokenPolicy policy = Policy("ESP256", "A128GCM", "direct");
        TokenBytesResult signed = CborWebToken.Create(
            Claims(),
            sign,
            false,
            policy);
        TokenBytesResult encrypted = CborWebToken.Create(
            Claims(),
            encrypt,
            false,
            policy);
        Assert.That(encrypted.Success, Is.True,
            encrypted.Error == null ? "no error" : encrypted.Error.Code);
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            if (context.KeyIdBytes != null
                && context.KeyIdBytes.Length == 1
                && context.KeyIdBytes[0] == 6)
            {
                return KeySuccess(first);
            }

            if (context.KeyIdBytes != null
                && context.KeyIdBytes.Length == 1
                && context.KeyIdBytes[0] == 7)
            {
                return KeySuccess(second);
            }

            return KeySuccess(encryption);
        });

        TokenValidationResult signedResult = UniversalToken.ValidateBytes(
            signed.Bytes,
            resolver,
            policy);
        TokenValidationResult encryptedResult = UniversalToken.ValidateBytes(
            encrypted.Bytes,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signedResult, TokenKind.Cose);
            Assert.That(signedResult.Headers, Has.Length.EqualTo(3));
            Assert.That(signedResult.Headers[0].Role,
                Is.EqualTo(TokenHeaderRole.Message));
            Assert.That(signedResult.Headers[1].Role,
                Is.EqualTo(TokenHeaderRole.Signature));
            Assert.That(signedResult.Algorithms,
                Is.EqualTo(new[] { "ESP256", "ESP256" }));
            Assert.That(signedResult.KeyIds,
                Is.EqualTo(new[] { "Bg", "Bw" }));
            Assert.That(signedResult.SignatureValidated, Is.True);
            AssertSuccess(encryptedResult, TokenKind.Cose);
            Assert.That(encryptedResult.Headers, Has.Length.EqualTo(2));
            Assert.That(encryptedResult.KeyManagementAlgorithm,
                Is.EqualTo("direct"));
            Assert.That(encryptedResult.EncryptionAlgorithm,
                Is.EqualTo("A128GCM"));
            Assert.That(encryptedResult.KeyId, Is.EqualTo("CA"));
            Assert.That(encryptedResult.EncryptionValidated, Is.True);
        });

        encryption.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidateBytesRejectsTamperingUnknownInputAndNullArguments()
    {
        TokenKey key = CoseKey(32, 1, CoseAlgorithms.HmacSha256);
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult written = CborWebToken.Create(
            Claims(),
            key,
            policy);
        byte[] tampered = written.Bytes.ToArray();
        tampered[tampered.Length - 1] ^= 1;
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(key));

        TokenValidationResult changed = UniversalToken.ValidateBytes(
            tampered,
            resolver,
            policy);
        TokenValidationResult unknown = UniversalToken.ValidateBytes(
            new byte[] { 255 },
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(changed);
            AssertFailure(unknown, "unknown_token");
            Assert.That(
                () => UniversalToken.ValidateBytes(null, resolver, policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.ValidateBytes(
                    Array.Empty<byte>(),
                    null,
                    policy),
                Throws.ArgumentNullException);
            Assert.That(
                () => UniversalToken.ValidateBytes(
                    Array.Empty<byte>(),
                    resolver,
                    null),
                Throws.ArgumentNullException);
        });

        key.ReleaseOwnedMaterial();
    }

    static CwtClaims Claims()
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        claims.Set(CwtClaimLabels.Subject, Text("alice"));
        claims.Set(CwtClaimLabels.Audience, Text("api"));
        claims.Set(CwtClaimLabels.ExpirationTime, Unsigned(4102444800));
        claims.Set(CwtClaimLabels.NotBefore, Unsigned(0));
        claims.Set(CwtClaimLabels.IssuedAt, Unsigned(1));
        return claims;
    }

    static TokenKey CoseKey(int length, int seed, int algorithm)
    {
        return SymmetricKeyImporterOperations.ImportCose(
            Sequence(length, seed),
            algorithm).Key;
    }

    static byte[] Sequence(int count, int seed)
    {
        return Enumerable.Range(seed, count)
            .Select(value => (byte)value)
            .ToArray();
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

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static void AssertSuccess(TokenValidationResult result, TokenKind kind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                result.Error == null ? "no error" : result.Error.Code);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.CwtClaims, Is.Not.Null);
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
