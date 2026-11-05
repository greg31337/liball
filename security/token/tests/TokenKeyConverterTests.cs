using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class TokenKeyConverterTests
{
    [Test]
    public void JwkToCoseKeyMapsIdentifierAlgorithmAndOperations()
    {
        string secret = B64(Enumerable.Repeat((byte)7, 32).ToArray());
        string jwk = "{\"kty\":\"oct\",\"k\":\"" + secret
            + "\",\"kid\":\"clé-一\",\"alg\":\"HS256\","
            + "\"key_ops\":[\"sign\",\"verify\"]}";

        TokenBytesResult converted = TokenKeyConverter.JwkToCoseKey(jwk);
        CoseKeyResult parsed = CoseKey.Read(converted.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Error, Is.Null);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.KeyIdBytes, Is.EqualTo(Encoding.UTF8.GetBytes("clé-一")));
            Assert.That(parsed.Key.CoseAlgorithm, Is.EqualTo(CoseAlgorithms.HmacSha256));
            Assert.That(parsed.Key.CoseKeyOperations, Is.EqualTo(new int[]
            {
                CoseKeyLabels.OperationSign,
                CoseKeyLabels.OperationVerify
            }));
        });

        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void CoseKeyToJwkMapsIdentifierAlgorithmAndOperations()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        key.KeyIdBytes = Encoding.UTF8.GetBytes("remote-key");
        key.CoseAlgorithm = CoseAlgorithms.HmacSha256;
        key.HasCoseAlgorithm = true;
        key.CoseKeyOperations = new int[]
        {
            CoseKeyLabels.OperationSign,
            CoseKeyLabels.OperationVerify
        };
        TokenBytesResult cose = CoseKey.Write(key);

        TokenTextResult converted = TokenKeyConverter.CoseKeyToJwk(cose.Bytes);
        JwkResult parsed = JsonWebKey.Read(converted.Text);

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Error, Is.Null);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.KeyId, Is.EqualTo("remote-key"));
            Assert.That(parsed.Key.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(parsed.Key.KeyOperations, Is.EqualTo(new string[]
            {
                TokenKeyOperations.Sign,
                TokenKeyOperations.Verify
            }));
        });

        key.ReleaseOwnedMaterial();
        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void EverySharedKeyOperationMapsInBothDirections()
    {
        string[] operations = new string[]
        {
            TokenKeyOperations.Sign,
            TokenKeyOperations.Verify,
            TokenKeyOperations.Encrypt,
            TokenKeyOperations.Decrypt,
            TokenKeyOperations.WrapKey,
            TokenKeyOperations.UnwrapKey,
            TokenKeyOperations.DeriveKey,
            TokenKeyOperations.DeriveBits
        };
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        key.KeyOperations = operations;

        TokenBytesResult cose = TokenKeyConverter.ToCoseKey(key);
        CoseKeyResult parsedCose = CoseKey.Read(cose.Bytes);
        TokenTextResult jwk = TokenKeyConverter.CoseKeyToJwk(cose.Bytes);
        JwkResult parsedJwk = JsonWebKey.Read(jwk.Text);

        Assert.Multiple(() =>
        {
            Assert.That(parsedCose.Key.CoseKeyOperations,
                Is.EqualTo(Enumerable.Range(1, 8).ToArray()));
            Assert.That(parsedJwk.Key.KeyOperations, Is.EqualTo(operations));
        });

        key.ReleaseOwnedMaterial();
        parsedCose.Key.ReleaseOwnedMaterial();
        parsedJwk.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void PrivateRsaSurvivesJwkCoseJwkConversion()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        key.Algorithm = JoseAlgorithms.RsaSha256;
        key.KeyId = "rsa-private";
        TokenTextResult original = JsonWebKey.Write(key);
        JwkResult originalParsed = JsonWebKey.Read(original.Text);
        TokenBytesResult cose = TokenKeyConverter.JwkToCoseKey(original.Text);
        TokenTextResult converted = TokenKeyConverter.CoseKeyToJwk(cose.Bytes);
        JwkResult roundTrip = JsonWebKey.Read(converted.Text);
        TokenBytesResult originalThumbprint = JsonWebKey.Thumbprint(originalParsed.Key);
        TokenBytesResult roundTripThumbprint = JsonWebKey.Thumbprint(roundTrip.Key);

        Assert.Multiple(() =>
        {
            Assert.That(cose.Success, Is.True);
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Text, Does.Contain("\"d\":"));
            Assert.That(roundTrip.Key.KeyId, Is.EqualTo("rsa-private"));
            Assert.That(roundTrip.Key.Algorithm, Is.EqualTo(JoseAlgorithms.RsaSha256));
            Assert.That(roundTrip.Key.NativePrivateMaterial, Is.True);
            Assert.That(roundTripThumbprint.Bytes, Is.EqualTo(originalThumbprint.Bytes));
        });

        originalParsed.Key.ReleaseOwnedMaterial();
        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void EcAndOkpKeysConvertAcrossBothFormats()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ec = EcdsaKeyImporter.Import(ecdsa, true).Key;
        ec.Algorithm = JoseAlgorithms.EcdsaSha256;
        TokenBytesResult ecCose = TokenKeyConverter.ToCoseKey(ec);
        TokenTextResult ecJwk = TokenKeyConverter.CoseKeyToJwk(ecCose.Bytes);

        TokenKey okp = new TokenKey();
        okp.KeyType = TokenKeyTypes.OctetKeyPair;
        okp.Curve = "Ed25519";
        okp.Algorithm = JoseAlgorithms.Ed25519;
        okp.OkpPublicKey = Enumerable.Repeat((byte)3, 32).ToArray();
        okp.OkpPrivateKey = Enumerable.Repeat((byte)4, 32).ToArray();
        TokenBytesResult okpCose = TokenKeyConverter.ToCoseKey(okp);
        TokenTextResult okpJwk = TokenKeyConverter.CoseKeyToJwk(okpCose.Bytes);
        JwkResult parsedEc = JsonWebKey.Read(ecJwk.Text);
        JwkResult parsedOkp = JsonWebKey.Read(okpJwk.Text);

        Assert.Multiple(() =>
        {
            Assert.That(parsedEc.Success, Is.True);
            Assert.That(parsedEc.Key.NativePrivateMaterial, Is.True);
            Assert.That(parsedEc.Key.Curve, Is.EqualTo("P-256"));
            Assert.That(parsedOkp.Success, Is.True);
            Assert.That(parsedOkp.Key.OkpPrivateKey, Is.EqualTo(okp.OkpPrivateKey));
            Assert.That(parsedOkp.Key.Curve, Is.EqualTo("Ed25519"));
        });

        parsedEc.Key.ReleaseOwnedMaterial();
        parsedOkp.Key.ReleaseOwnedMaterial();
        okp.ReleaseOwnedMaterial();
    }

    [Test]
    public void DirectConversionsDoNotMutateCallerMetadataOrSecretMaterial()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)9, 32).ToArray()).Key;
        key.KeyId = "jwk-id";
        key.KeyOperations = new string[] { TokenKeyOperations.Sign };
        key.BaseInitializationVector = Convert.FromHexString(
            "000102030405060708090A0B");
        byte[] retainedSecret = key.SymmetricKey.ToArray();

        TokenBytesResult cose = TokenKeyConverter.ToCoseKey(key);
        CoseKeyResult parsedCose = CoseKey.Read(cose.Bytes);
        TokenTextResult jwk = TokenKeyConverter.ToJwk(key);

        Assert.Multiple(() =>
        {
            Assert.That(cose.Success, Is.True);
            Assert.That(jwk.Success, Is.True);
            Assert.That(key.KeyIdBytes, Is.Null);
            Assert.That(key.CoseKeyOperations, Is.Empty);
            Assert.That(parsedCose.Key.BaseInitializationVector,
                Is.EqualTo(key.BaseInitializationVector));
            Assert.That(key.SymmetricKey, Is.EqualTo(retainedSecret));
        });

        key.ReleaseOwnedMaterial();
        parsedCose.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ConversionRejectsInvalidUtf8AndCoseOnlyOperations()
    {
        TokenKey invalidId = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        invalidId.KeyIdBytes = new byte[] { 0xFF };
        TokenKey macOperation = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        macOperation.CoseKeyOperations = new int[]
        {
            CoseKeyLabels.OperationMacCreate
        };

        Assert.Multiple(() =>
        {
            AssertTextFailure(TokenKeyConverter.ToJwk(invalidId), "invalid_utf8");
            AssertTextFailure(TokenKeyConverter.ToJwk(macOperation),
                "unmapped_key_operation");
        });

        invalidId.ReleaseOwnedMaterial();
        macOperation.ReleaseOwnedMaterial();
    }

    [Test]
    public void ConversionRejectsConflictingDualFormatMetadata()
    {
        TokenKey identifiers = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        identifiers.KeyId = "one";
        identifiers.KeyIdBytes = Encoding.UTF8.GetBytes("two");
        TokenKey operations = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        operations.KeyOperations = new string[] { TokenKeyOperations.Sign };
        operations.CoseKeyOperations = new int[] { CoseKeyLabels.OperationVerify };

        Assert.Multiple(() =>
        {
            AssertBytesFailure(TokenKeyConverter.ToCoseKey(identifiers),
                "conflicting_key_id");
            AssertTextFailure(TokenKeyConverter.ToJwk(identifiers),
                "conflicting_key_id");
            AssertBytesFailure(TokenKeyConverter.ToCoseKey(operations),
                "conflicting_key_operations");
            AssertTextFailure(TokenKeyConverter.ToJwk(operations),
                "conflicting_key_operations");
        });

        identifiers.ReleaseOwnedMaterial();
        operations.ReleaseOwnedMaterial();
    }

    [Test]
    public void CrossFormatConversionPropagatesParseAndMappingFailures()
    {
        TokenBytesResult invalidJwk = TokenKeyConverter.JwkToCoseKey("{}");
        TokenTextResult invalidCose = TokenKeyConverter.CoseKeyToJwk(new byte[] { 0x80 });
        TokenKey ecdh = new TokenKey();
        ecdh.KeyType = TokenKeyTypes.OctetKeyPair;
        ecdh.CoseCurve = CoseKeyLabels.CurveX25519;
        ecdh.CoseAlgorithm = CoseAlgorithms.EcdhEsHkdfSha256;
        ecdh.HasCoseAlgorithm = true;
        ecdh.OkpPublicKey = new byte[32];
        TokenBytesResult encoded = CoseKey.Write(ecdh);
        TokenTextResult unmapped = TokenKeyConverter.CoseKeyToJwk(encoded.Bytes);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(invalidJwk, "missing_jwk_member");
            AssertTextFailure(invalidCose, "cose_key_not_map");
            AssertTextFailure(unmapped, "unmapped_algorithm");
        });
    }

    [Test]
    public void ConversionApisRejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => TokenKeyConverter.ToJwk(null), Throws.ArgumentNullException);
            Assert.That(() => TokenKeyConverter.ToCoseKey(null), Throws.ArgumentNullException);
            Assert.That(() => TokenKeyConverter.JwkToCoseKey(null),
                Throws.ArgumentNullException);
            Assert.That(() => TokenKeyConverter.CoseKeyToJwk(null),
                Throws.ArgumentNullException);
        });
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    static void AssertTextFailure(TokenTextResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }
}
