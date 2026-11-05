using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CwtValidatorTests
{
    [Test]
    public void ValidatesSignedMacAndEncryptedSinglePartyCwts()
    {
        CwtClaims claims = Claims();
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey signing = EcdsaKeyImporter.Import(native, true).Key;
        signing.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        signing.HasCoseAlgorithm = true;
        TokenKey mac = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenKey encryption = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        CwtValidationPolicy policy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM");

        TokenBytesResult signed = CborWebToken.Create(
            claims,
            signing,
            policy.TokenPolicy);
        TokenBytesResult maced = CborWebToken.Create(
            claims,
            mac,
            policy.TokenPolicy);
        TokenBytesResult encrypted = CborWebToken.Create(
            claims,
            encryption,
            policy.TokenPolicy);

        CwtValidationResult signedResult = CborWebToken.Validate(
            signed.Bytes,
            Resolver(signing, mac, encryption),
            policy);
        CwtValidationResult macResult = CborWebToken.Validate(
            maced.Bytes,
            Resolver(signing, mac, encryption),
            policy);
        CwtValidationResult encryptedResult = CborWebToken.Validate(
            encrypted.Bytes,
            Resolver(signing, mac, encryption),
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signedResult, CoseMessageKind.Sign1);
            AssertSuccess(macResult, CoseMessageKind.Mac0);
            AssertSuccess(encryptedResult, CoseMessageKind.Encrypt0);
            Assert.That(signedResult.Claims.Get(CwtClaimLabels.Issuer).Text,
                Is.EqualTo("issuer"));
            Assert.That(macResult.Claims.Get(CwtClaimLabels.Subject).Text,
                Is.EqualTo("subject"));
            Assert.That(encryptedResult.Headers[0].Algorithm,
                Is.EqualTo(CoseAlgorithms.Aes128Gcm));
        });

        mac.ReleaseOwnedMaterial();
        encryption.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidatesMultiSignerMacAndEncryptionForms()
    {
        CwtClaims claims = Claims();
        using ECDsa firstNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey first = EcdsaKeyImporter.Import(firstNative, true).Key;
        TokenKey second = EcdsaKeyImporter.Import(secondNative, true).Key;
        CoseHeader firstHeader = AlgorithmHeader(
            CoseAlgorithms.EcdsaP256Sha256);
        firstHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 1 }));
        CoseHeader secondHeader = AlgorithmHeader(
            CoseAlgorithms.EcdsaP256Sha256);
        secondHeader.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 2 }));
        CwtProtectionLayer sign = CwtProtectionLayer.CreateSign(
            new CoseHeader(),
            new CoseHeader[] { firstHeader, secondHeader },
            new TokenKey[] { first, second });

        TokenKey macKey = Symmetric(32, CoseAlgorithms.Direct);
        CwtProtectionLayer mac = CwtProtectionLayer.CreateMac(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[] { RecipientHeader(CoseAlgorithms.Direct, 3) },
            new TokenKey[] { macKey },
            null);
        TokenKey encryptionKey = Symmetric(16, CoseAlgorithms.Direct);
        CwtProtectionLayer encrypt = CwtProtectionLayer.CreateEncrypt(
            AlgorithmHeader(CoseAlgorithms.Aes128Gcm),
            new CoseHeader[] { RecipientHeader(CoseAlgorithms.Direct, 4) },
            new TokenKey[] { encryptionKey },
            null);
        CwtValidationPolicy policy = Policy(
            "ESP256",
            "HMAC 256/256",
            "A128GCM",
            "direct");

        TokenBytesResult signed = CborWebToken.Create(
            claims,
            sign,
            false,
            policy.TokenPolicy);
        TokenBytesResult maced = CborWebToken.Create(
            claims,
            mac,
            false,
            policy.TokenPolicy);
        TokenBytesResult encrypted = CborWebToken.Create(
            claims,
            encrypt,
            false,
            policy.TokenPolicy);

        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            if (context.KeyIdBytes != null && context.KeyIdBytes.Length == 1)
            {
                if (context.KeyIdBytes[0] == 1)
                {
                    return KeySuccess(first);
                }

                if (context.KeyIdBytes[0] == 2)
                {
                    return KeySuccess(second);
                }

                if (context.KeyIdBytes[0] == 3)
                {
                    return KeySuccess(macKey);
                }

                if (context.KeyIdBytes[0] == 4)
                {
                    return KeySuccess(encryptionKey);
                }
            }

            return KeyFailure("key_not_found");
        });

        CwtValidationResult signedResult = CborWebToken.Validate(
            signed.Bytes,
            resolver,
            policy);
        CwtValidationResult macResult = CborWebToken.Validate(
            maced.Bytes,
            resolver,
            policy);
        CwtValidationResult encryptedResult = CborWebToken.Validate(
            encrypted.Bytes,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(signedResult, CoseMessageKind.Sign);
            AssertSuccess(macResult, CoseMessageKind.Mac);
            AssertSuccess(encryptedResult, CoseMessageKind.Encrypt);
        });

        macKey.ReleaseOwnedMaterial();
        encryptionKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void ValidatesTaggedNestedCwtWithPerLayerExternalData()
    {
        CwtClaims claims = Claims();
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey signing = EcdsaKeyImporter.Import(native, true).Key;
        TokenKey encryption = Symmetric(16, CoseAlgorithms.Aes128Gcm);
        byte[] innerData = new byte[] { 1, 2 };
        byte[] outerData = new byte[] { 3, 4 };
        CwtProtectionLayer inner = CwtProtectionLayer.CreateSign1(
            AlgorithmHeader(CoseAlgorithms.EcdsaP256Sha256),
            signing);
        inner.ExternalAuthenticatedData = innerData;
        CwtProtectionLayer outer = CwtProtectionLayer.CreateEncrypt0(
            AlgorithmHeader(CoseAlgorithms.Aes128Gcm),
            encryption);
        outer.ExternalAuthenticatedData = outerData;
        CwtValidationPolicy policy = Policy("ESP256", "A128GCM");
        policy.RequireCwtTag = true;
        policy.LayerExternalAuthenticatedData = new byte[][]
        {
            outerData,
            innerData
        };

        TokenBytesResult token = CborWebToken.CreateNested(
            claims,
            new CwtProtectionLayer[] { inner, outer },
            true,
            policy.TokenPolicy);
        List<string> operations = new List<string>();
        TokenKeyResolver resolver = new TokenKeyResolver(context =>
        {
            operations.Add(context.Operation);
            Assert.That(context.Kind, Is.EqualTo(TokenKind.Cwt));
            Assert.That(context.CoseHeader, Is.Not.Null);
            return context.Operation == TokenKeyOperations.Decrypt
                ? KeySuccess(encryption)
                : KeySuccess(signing);
        });

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            resolver,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                result.Error == null ? "no error" : result.Error.Code);
            Assert.That(result.CwtTagged, Is.True);
            Assert.That(result.Nested, Is.True);
            Assert.That(result.ProtectionKinds, Is.EqualTo(new[]
            {
                CoseMessageKind.Encrypt0,
                CoseMessageKind.Sign1
            }));
            Assert.That(operations, Is.EqualTo(new[]
            {
                TokenKeyOperations.Decrypt,
                TokenKeyOperations.Verify
            }));
        });

        encryption.ReleaseOwnedMaterial();
    }

    [Test]
    public void RejectsTamperingWrongExternalDataAndResolverFailure()
    {
        TokenKey key = Symmetric(32, CoseAlgorithms.HmacSha256);
        CwtProtectionLayer layer = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        layer.ExternalAuthenticatedData = new byte[] { 7 };
        CwtValidationPolicy creationPolicy = Policy("HMAC 256/256");
        TokenBytesResult token = CborWebToken.Create(
            Claims(),
            layer,
            false,
            creationPolicy.TokenPolicy);
        byte[] tampered = token.Bytes.ToArray();
        tampered[tampered.Length - 1] ^= 1;
        CwtValidationPolicy correct = Policy("HMAC 256/256");
        correct.LayerExternalAuthenticatedData = new byte[][]
        {
            new byte[] { 7 }
        };
        CwtValidationPolicy wrongData = Policy("HMAC 256/256");
        wrongData.LayerExternalAuthenticatedData = new byte[][]
        {
            new byte[] { 8 }
        };

        CwtValidationResult changed = CborWebToken.Validate(
            tampered,
            Resolver(key),
            correct);
        CwtValidationResult wrong = CborWebToken.Validate(
            token.Bytes,
            Resolver(key),
            wrongData);
        CwtValidationResult unresolved = CborWebToken.Validate(
            token.Bytes,
            new TokenKeyResolver(context => KeyFailure("key_not_found")),
            correct);

        Assert.Multiple(() =>
        {
            AssertFailure(changed, "mac_mismatch");
            AssertFailure(wrong, "mac_mismatch");
            AssertFailure(unresolved, "key_not_found");
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void AppliesProtectionTagNestingAndClaimPolicies()
    {
        TokenKey key = Symmetric(32, CoseAlgorithms.HmacSha256);
        CwtProtectionLayer first = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtProtectionLayer second = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        TokenPolicy creationPolicy = Policy("HMAC 256/256").TokenPolicy;
        TokenBytesResult one = CborWebToken.Create(
            Claims(),
            first,
            false,
            creationPolicy);
        TokenBytesResult nested = CborWebToken.CreateNested(
            Claims(),
            new CwtProtectionLayer[] { first, second },
            false,
            creationPolicy);
        CwtClaims expiredClaims = Claims();
        expiredClaims.Set(CwtClaimLabels.ExpirationTime, Unsigned(1));
        TokenBytesResult expired = CborWebToken.Create(
            expiredClaims,
            first,
            false,
            creationPolicy);

        CwtValidationPolicy macDisabled = Policy("HMAC 256/256");
        macDisabled.AllowMacTokens = false;
        CwtValidationPolicy tagRequired = Policy("HMAC 256/256");
        tagRequired.RequireCwtTag = true;
        CwtValidationPolicy nestingDisabled = Policy("HMAC 256/256");
        nestingDisabled.AllowNestedTokens = false;
        CwtValidationPolicy expiredPolicy = Policy("HMAC 256/256");
        expiredPolicy.TokenPolicy.ClockSkewSeconds = 0;
        expiredPolicy.Clock = new TokenClock(() =>
            DateTimeOffset.FromUnixTimeSeconds(10));

        CwtValidationResult disabled = CborWebToken.Validate(
            one.Bytes,
            Resolver(key),
            macDisabled);
        CwtValidationResult missingTag = CborWebToken.Validate(
            one.Bytes,
            Resolver(key),
            tagRequired);
        CwtValidationResult disallowedNested = CborWebToken.Validate(
            nested.Bytes,
            Resolver(key),
            nestingDisabled);
        CwtValidationResult expiredResult = CborWebToken.Validate(
            expired.Bytes,
            Resolver(key),
            expiredPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(disabled, "maced_cwt_not_allowed");
            AssertFailure(missingTag, "missing_cwt_tag");
            AssertFailure(disallowedNested, "nested_cwt_not_allowed");
            AssertFailure(expiredResult, "token_expired");
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void RejectsExcessiveLayersAndInvalidTagContent()
    {
        TokenKey key = Symmetric(32, CoseAlgorithms.HmacSha256);
        CwtProtectionLayer first = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtProtectionLayer second = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtProtectionLayer third = CwtProtectionLayer.CreateMac0(
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            key);
        CwtValidationPolicy policy = Policy("HMAC 256/256");
        policy.MaximumProtectionLayers = 2;
        TokenBytesResult nested = CborWebToken.CreateNested(
            Claims(),
            new CwtProtectionLayer[] { first, second, third },
            false,
            policy.TokenPolicy);
        CwtValidationResult layers = CborWebToken.Validate(
            nested.Bytes,
            Resolver(key),
            policy);
        CwtValidationResult tag = CborWebToken.Validate(
            Convert.FromHexString("D83DA0"),
            Resolver(key),
            Policy("HMAC 256/256"));

        Assert.Multiple(() =>
        {
            AssertFailure(layers, "cwt_protection_layer_limit_exceeded");
            AssertFailure(tag, "invalid_cwt_tag_content");
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void RejectsAnExposedSymmetricConfirmationKey()
    {
        CwtClaims claims = Claims();
        TokenKey confirmation = Symmetric(32, CoseAlgorithms.HmacSha256);
        TokenResult set = CwtConfirmation.SetProofOfPossessionKey(
            claims,
            confirmation);
        TokenKey macKey = Symmetric(32, CoseAlgorithms.HmacSha256);
        CwtValidationPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult claimsBytes = claims.WriteWithPolicy(policy.TokenPolicy);
        TokenBytesResult token = CoseMac0Operations.Create(
            claimsBytes.Bytes,
            AlgorithmHeader(CoseAlgorithms.HmacSha256),
            macKey,
            policy.TokenPolicy);

        CwtValidationResult result = CborWebToken.Validate(
            token.Bytes,
            Resolver(macKey),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(set.Success, Is.True);
            AssertFailure(
                result,
                "symmetric_confirmation_key_not_encrypted");
        });

        confirmation.ReleaseOwnedMaterial();
        macKey.ReleaseOwnedMaterial();
    }

    static CwtClaims Claims()
    {
        CwtClaims claims = new CwtClaims();
        claims.Set(CwtClaimLabels.Issuer, Text("issuer"));
        claims.Set(CwtClaimLabels.Subject, Text("subject"));
        return claims;
    }

    static CoseHeader AlgorithmHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(algorithm));
        return header;
    }

    static CoseHeader RecipientHeader(int algorithm, byte keyId)
    {
        CoseHeader header = new CoseHeader();
        header.SetUnprotected(CoseHeaderLabels.Algorithm, Int(algorithm));
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { keyId }));
        return header;
    }

    static TokenKey Symmetric(int length, int algorithm)
    {
        byte[] material = new byte[length];
        int index = 0;
        while (index < material.Length)
        {
            material[index] = (byte)(index + 1);
            index += 1;
        }

        return SymmetricKeyImporterOperations.ImportCose(material, algorithm).Key;
    }

    static CwtValidationPolicy Policy(params string[] algorithms)
    {
        CwtValidationPolicy policy = new CwtValidationPolicy();
        policy.TokenPolicy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static TokenKeyResolver Resolver(params TokenKey[] keys)
    {
        return new TokenKeyResolver(context =>
        {
            int index = 0;
            while (index < keys.Length)
            {
                TokenKey key = keys[index];
                if (context.CoseAlgorithm == key.CoseAlgorithm)
                {
                    return KeySuccess(key);
                }

                index += 1;
            }

            return KeyFailure("key_not_found");
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
        result.Error = new TokenError(code, code, -1);
        return result;
    }

    static CborItem Int(int value)
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

    static void AssertSuccess(
        CwtValidationResult result,
        CoseMessageKind kind)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Claims, Is.Not.Null);
        Assert.That(result.ProtectionKinds, Is.EqualTo(new[] { kind }));
        Assert.That(result.Nested, Is.False);
    }

    static void AssertFailure(CwtValidationResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Claims, Is.Null);
    }
}
