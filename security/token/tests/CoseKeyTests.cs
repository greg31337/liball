using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseKeyTests
{
    [Test]
    public void ReadParsesSymmetricKeyAndCommonMetadata()
    {
        byte[] secret = Enumerable.Repeat((byte)7, 32).ToArray();
        byte[] kid = new byte[] { 1, 2, 3 };
        byte[] encoded = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.KeyId, Bytes(kid)),
            Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.HmacSha256)),
            Entry(CoseKeyLabels.KeyOperations, Array(
                Int(CoseKeyLabels.OperationMacCreate),
                Int(CoseKeyLabels.OperationMacVerify))),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(secret))));

        CoseKeyResult result = CoseKey.Read(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeSymmetric));
            Assert.That(result.Key.KeyIdBytes, Is.EqualTo(kid));
            Assert.That(result.Key.HasCoseAlgorithm, Is.True);
            Assert.That(result.Key.CoseAlgorithm, Is.EqualTo(CoseAlgorithms.HmacSha256));
            Assert.That(result.Key.CoseKeyOperations, Is.EqualTo(new int[]
            {
                CoseKeyLabels.OperationMacCreate,
                CoseKeyLabels.OperationMacVerify
            }));
            Assert.That(result.Key.SymmetricKey, Is.EqualTo(secret));
            Assert.That(result.Key.SymmetricKey, Is.Not.SameAs(secret));
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadParsesRsaPublicAndPrivateKeys()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters publicParameters = rsa.ExportParameters(false);
        RSAParameters privateParameters = rsa.ExportParameters(true);
        byte[] publicKey = Encode(RsaMap(publicParameters, false));
        byte[] privateKey = Encode(RsaMap(privateParameters, true));

        CoseKeyResult publicResult = CoseKey.Read(publicKey);
        CoseKeyResult privateResult = CoseKey.Read(privateKey);

        Assert.Multiple(() =>
        {
            Assert.That(publicResult.Success, Is.True);
            Assert.That(publicResult.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(publicResult.Key.CoseAlgorithm, Is.EqualTo(CoseAlgorithms.RsaSha256));
            Assert.That(publicResult.Key.NativePublicMaterial, Is.True);
            Assert.That(publicResult.Key.NativePrivateMaterial, Is.False);
            Assert.That(publicResult.Key.OwnsNativeKey, Is.True);
            Assert.That(privateResult.Success, Is.True,
                privateResult.Error == null
                    ? "no error"
                    : privateResult.Error.Code + ": " + privateResult.Error.Message);
            Assert.That(privateResult.Key.NativePrivateMaterial, Is.True);
            Assert.That(privateResult.Key.Rsa.ExportParameters(false).Modulus,
                Is.EqualTo(publicParameters.Modulus));
        });

        publicResult.Key.ReleaseOwnedMaterial();
        privateResult.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadNormalizesFixedWidthRsaIntegerComponents()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters parameters = rsa.ExportParameters(true);
        parameters.Modulus = PrefixZero(parameters.Modulus);
        parameters.Exponent = PrefixZero(parameters.Exponent);
        parameters.D = PrefixZero(parameters.D);
        parameters.P = PrefixZero(parameters.P);
        parameters.Q = PrefixZero(parameters.Q);
        parameters.DP = PrefixZero(parameters.DP);
        parameters.DQ = PrefixZero(parameters.DQ);
        parameters.InverseQ = PrefixZero(parameters.InverseQ);

        CoseKeyResult result = CoseKey.Read(Encode(RsaMap(parameters, true)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.True);
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadParsesEc2PublicAndPrivateKeys()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters publicParameters = ecdsa.ExportParameters(false);
        ECParameters privateParameters = ecdsa.ExportParameters(true);
        byte[] publicKey = Encode(Ec2Map(publicParameters, false));
        byte[] privateKey = Encode(Ec2Map(privateParameters, true));

        CoseKeyResult publicResult = CoseKey.Read(publicKey);
        CoseKeyResult privateResult = CoseKey.Read(privateKey);

        Assert.Multiple(() =>
        {
            Assert.That(publicResult.Success, Is.True);
            Assert.That(publicResult.Key.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(publicResult.Key.CoseCurve, Is.EqualTo(CoseKeyLabels.CurveP256));
            Assert.That(publicResult.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(publicResult.Key.NativePrivateMaterial, Is.False);
            Assert.That(privateResult.Success, Is.True);
            Assert.That(privateResult.Key.NativePrivateMaterial, Is.True);
            Assert.That(privateResult.Key.Ecdsa.ExportParameters(false).Q.X,
                Is.EqualTo(publicParameters.Q.X));
        });

        publicResult.Key.ReleaseOwnedMaterial();
        privateResult.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadParsesCompressedEc2PointAndEcdhAlgorithm()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters parameters = ecdsa.ExportParameters(false);
        bool odd = (parameters.Q.Y[^1] & 1) == 1;
        CborItem y = new CborItem();
        y.Kind = TokenCborValueKind.Boolean;
        y.BooleanValue = odd;
        byte[] encoded = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeEc2)),
            Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.EcdhEsHkdfSha256)),
            Entry(CoseKeyLabels.Curve, Int(CoseKeyLabels.CurveP256)),
            Entry(CoseKeyLabels.X, Bytes(parameters.Q.X)),
            Entry(CoseKeyLabels.Y, y)));

        CoseKeyResult result = CoseKey.Read(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdhEsHkdfSha256));
            Assert.That(result.Key.Ecdsa.ExportParameters(false).Q.Y,
                Is.EqualTo(parameters.Q.Y));
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [TestCase(CoseKeyLabels.CurveEd25519, CoseAlgorithms.Ed25519, 32)]
    [TestCase(CoseKeyLabels.CurveEd448, CoseAlgorithms.Ed448, 57)]
    [TestCase(CoseKeyLabels.CurveX25519, CoseAlgorithms.EcdhEsHkdfSha256, 32)]
    [TestCase(CoseKeyLabels.CurveX448, CoseAlgorithms.EcdhEsHkdfSha512, 56)]
    public void ReadParsesKnownOkpForms(int curve, int algorithm, int size)
    {
        byte[] publicBytes = Enumerable.Repeat((byte)4, size).ToArray();
        byte[] privateBytes = Enumerable.Repeat((byte)9, size).ToArray();
        byte[] encoded = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeOkp)),
            Entry(CoseKeyLabels.Algorithm, Int(algorithm)),
            Entry(CoseKeyLabels.Curve, Int(curve)),
            Entry(CoseKeyLabels.X, Bytes(publicBytes)),
            Entry(CoseKeyLabels.PrivateKey, Bytes(privateBytes))));

        CoseKeyResult result = CoseKey.Read(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.OctetKeyPair));
            Assert.That(result.Key.CoseCurve, Is.EqualTo(curve));
            Assert.That(result.Key.CoseAlgorithm, Is.EqualTo(algorithm));
            Assert.That(result.Key.OkpPublicKey, Is.EqualTo(publicBytes));
            Assert.That(result.Key.OkpPrivateKey, Is.EqualTo(privateBytes));
            Assert.That(result.Key.NativePublicMaterial, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.True);
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadAcceptsRegisteredCoseKeyTag()
    {
        CborItem map = Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32])));
        CborItem tagged = new CborItem();
        tagged.Kind = TokenCborValueKind.Tag;
        tagged.TagValue = 101;
        tagged.TaggedValue = map;

        CoseKeyResult result = CoseKey.Read(Encode(tagged));

        Assert.That(result.Success, Is.True);
        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRejectsMalformedRootTagAndTrailingData()
    {
        CborItem validMap = Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32])));
        byte[] encoded = Encode(validMap);
        CborItem wrongTag = new CborItem();
        wrongTag.Kind = TokenCborValueKind.Tag;
        wrongTag.TagValue = 18;
        wrongTag.TaggedValue = validMap;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseKey.Read(new byte[] { 255 }), "invalid_cbor");
            AssertFailure(CoseKey.Read(Encode(Array())), "cose_key_not_map");
            AssertFailure(CoseKey.Read(Encode(wrongTag)), "invalid_cose_key_tag");
            AssertFailure(CoseKey.Read(encoded.Concat(new byte[] { 0 }).ToArray()),
                "trailing_cose_key_data");
        });
    }

    [Test]
    public void ReadRejectsMissingUnsupportedAndWronglyTypedParameters()
    {
        byte[] missingType = Encode(Map(
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32]))));
        byte[] unsupportedType = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(99))));
        byte[] textLabel = Encode(Map(new CborMapEntry(
            Text("kty"),
            Int(CoseKeyLabels.KeyTypeSymmetric))));
        byte[] unknownExtension = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32])),
            new CborMapEntry(Text("application-extension"), Text("value"))));
        byte[] wrongMaterial = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.SymmetricKey, Text("secret"))));
        CoseKeyResult extensionResult = CoseKey.Read(unknownExtension);

        Assert.Multiple(() =>
        {
            AssertFailure(CoseKey.Read(missingType), "missing_cose_key_parameter");
            AssertFailure(CoseKey.Read(unsupportedType), "unsupported_key_type");
            AssertFailure(CoseKey.Read(textLabel), "missing_cose_key_parameter");
            AssertFailure(CoseKey.Read(wrongMaterial), "invalid_cose_key_parameter");
            Assert.That(extensionResult.Success, Is.True);
        });

        extensionResult.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadPropagatesDuplicateMapLabelDetection()
    {
        byte[] encoded = new byte[] { 162, 1, 4, 1, 4 };

        CoseKeyResult result = CoseKey.Read(encoded);

        AssertFailure(result, "duplicate_cbor_map_key");
    }

    [Test]
    public void ReadRejectsInvalidOperations()
    {
        CborMapEntry type = Entry(
            CoseKeyLabels.KeyType,
            Int(CoseKeyLabels.KeyTypeSymmetric));
        CborMapEntry material = Entry(
            CoseKeyLabels.SymmetricKey,
            Bytes(new byte[32]));
        byte[] wrongType = Encode(Map(type, material,
            Entry(CoseKeyLabels.KeyOperations, Int(1))));
        byte[] wrongItem = Encode(Map(type, material,
            Entry(CoseKeyLabels.KeyOperations, Array(Text("sign")))));
        byte[] unknown = Encode(Map(type, material,
            Entry(CoseKeyLabels.KeyOperations, Array(Int(99)))));
        byte[] duplicate = Encode(Map(type, material,
            Entry(CoseKeyLabels.KeyOperations, Array(Int(9), Int(9)))));

        Assert.Multiple(() =>
        {
            AssertFailure(CoseKey.Read(wrongType), "invalid_cose_key_operations");
            AssertFailure(CoseKey.Read(wrongItem), "invalid_cose_key_operation");
            AssertFailure(CoseKey.Read(unknown), "unknown_key_operation");
            AssertFailure(CoseKey.Read(duplicate), "duplicate_key_operation");
        });
    }

    [Test]
    public void ReadRejectsAlgorithmTypeAndKeyTypeMismatches()
    {
        byte[] textAlgorithm = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.Algorithm, Text("HS256")),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32]))));
        byte[] rsaAlgorithm = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.RsaSha256)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32]))));
        byte[] wrongOkp = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeOkp)),
            Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.Ed25519)),
            Entry(CoseKeyLabels.Curve, Int(CoseKeyLabels.CurveX25519)),
            Entry(CoseKeyLabels.X, Bytes(new byte[32]))));

        Assert.Multiple(() =>
        {
            AssertFailure(CoseKey.Read(textAlgorithm), "invalid_cose_key_parameter");
            AssertFailure(CoseKey.Read(rsaAlgorithm), "invalid_key_algorithm");
            AssertFailure(CoseKey.Read(wrongOkp), "invalid_key_algorithm");
        });
    }

    [Test]
    public void ReadCompatibilityOverloadControlsPolymorphicEcdsaLabels()
    {
        byte[] encoded = Convert.FromHexString(
            "A72358206C1382765AEC5358F117733D281C1C7BDC39884D04A45A1E6C67C858BC206C1922582060F7F1A780D8A783BFB7A2DD6B2796E8128DBBCEF9D3D168DB9529971A36E7B9215820143329CCE7868E416927599CF65A34F3CE2FFDA55A7ECA69ED8919A394D42F0F2001010202524173796D6D657472696345434453413235360326");

        CoseKeyResult conservative = CoseKey.Read(encoded);
        CoseKeyResult compatible = CoseKey.Read(encoded, true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True,
                compatible.Error == null
                    ? "no error"
                    : compatible.Error.Code);
            Assert.That(compatible.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha256));
            Assert.That(compatible.Key.CoseCurve,
                Is.EqualTo(CoseKeyLabels.CurveP256));
        });

        compatible.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRejectsIncompleteOrInvalidKeyMaterial()
    {
        byte[] shortSymmetric = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[8]))));
        byte[] incompleteRsa = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeRsa)),
            Entry(CoseKeyLabels.RsaModulus, Bytes(new byte[256]))));
        byte[] invalidEc = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeEc2)),
            Entry(CoseKeyLabels.Curve, Int(CoseKeyLabels.CurveP256)),
            Entry(CoseKeyLabels.X, Bytes(new byte[32])),
            Entry(CoseKeyLabels.Y, Bytes(new byte[32]))));
        byte[] shortOkp = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeOkp)),
            Entry(CoseKeyLabels.Curve, Int(CoseKeyLabels.CurveEd25519)),
            Entry(CoseKeyLabels.X, Bytes(new byte[31]))));

        Assert.Multiple(() =>
        {
            AssertFailure(CoseKey.Read(shortSymmetric), "weak_key");
            AssertFailure(CoseKey.Read(incompleteRsa), "missing_cose_key_parameter");
            AssertFailure(CoseKey.Read(invalidEc), "invalid_ec_point");
            AssertFailure(CoseKey.Read(shortOkp), "invalid_okp_key");
        });
    }

    [Test]
    public void ReadRejectsNullInput()
    {
        Assert.That(() => CoseKey.Read(null), Throws.ArgumentNullException);
    }

    [Test]
    public void WriteProducesExactDeterministicSymmetricEncoding()
    {
        byte[] secret = new byte[32];
        TokenKey key = SymmetricKeyImporterOperations.Import(secret).Key;
        key.KeyIdBytes = new byte[] { 1, 2 };
        key.CoseAlgorithm = CoseAlgorithms.HmacSha256;
        key.HasCoseAlgorithm = true;
        key.CoseKeyOperations = new int[]
        {
            CoseKeyLabels.OperationMacCreate,
            CoseKeyLabels.OperationMacVerify
        };

        TokenBytesResult result = CoseKey.Write(key);

        byte[] expected = new byte[]
        {
            0xA5,
            0x01, 0x04,
            0x02, 0x42, 0x01, 0x02,
            0x03, 0x05,
            0x04, 0x82, 0x09, 0x0A,
            0x20, 0x58, 0x20,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(expected));
            Assert.That(key.SymmetricKey, Is.EqualTo(secret));
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void BaseInitializationVectorRoundTripsAsCommonKeyParameter()
    {
        byte[] baseInitializationVector = Convert.FromHexString(
            "89F52F65A1C580930000000000");
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        key.BaseInitializationVector = baseInitializationVector;

        TokenBytesResult written = CoseKey.Write(key);
        CoseKeyResult parsed = CoseKey.Read(written.Bytes);
        CborItem map = CborReader.Create(written.Bytes).Read().Item;

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.BaseInitializationVector,
                Is.EqualTo(baseInitializationVector));
            Assert.That(MapValue(
                map,
                CoseKeyLabels.BaseInitializationVector).Bytes,
                Is.EqualTo(baseInitializationVector));
        });

        key.ReleaseOwnedMaterial();
        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRejectsWronglyTypedBaseInitializationVector()
    {
        byte[] encoded = Encode(Map(
            Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeSymmetric)),
            Entry(CoseKeyLabels.BaseInitializationVector, Int(1)),
            Entry(CoseKeyLabels.SymmetricKey, Bytes(new byte[32]))));

        CoseKeyResult result = CoseKey.Read(encoded);

        AssertFailure(result, "invalid_cose_key_parameter");
    }

    [Test]
    public void WriteAndReadRoundTripRsaPublicAndPrivateKeys()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey publicKey = RsaKeyImporterOperations.Import(rsa, false).Key;
        publicKey.CoseAlgorithm = CoseAlgorithms.RsaSha256;
        publicKey.HasCoseAlgorithm = true;
        TokenKey privateKey = RsaKeyImporterOperations.Import(rsa, true).Key;
        privateKey.CoseAlgorithm = CoseAlgorithms.RsaPssSha256;
        privateKey.HasCoseAlgorithm = true;

        TokenBytesResult publicBytes = CoseKey.Write(publicKey);
        TokenBytesResult privateBytes = CoseKey.Write(privateKey);
        CoseKeyResult publicRoundTrip = CoseKey.Read(publicBytes.Bytes);
        CoseKeyResult privateRoundTrip = CoseKey.Read(privateBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(publicBytes.Success, Is.True);
            Assert.That(privateBytes.Success, Is.True);
            Assert.That(publicRoundTrip.Success, Is.True);
            Assert.That(publicRoundTrip.Key.NativePrivateMaterial, Is.False);
            Assert.That(privateRoundTrip.Success, Is.True);
            Assert.That(privateRoundTrip.Key.NativePrivateMaterial, Is.True);
            Assert.That(privateRoundTrip.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.RsaPssSha256));
            Assert.That(rsa.ExportParameters(false).Modulus, Is.Not.Empty);
        });

        publicRoundTrip.Key.ReleaseOwnedMaterial();
        privateRoundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteAndReadRoundTripEc2PublicAndPrivateKeys()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey publicKey = EcdsaKeyImporter.Import(ecdsa, false).Key;
        publicKey.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        publicKey.HasCoseAlgorithm = true;
        TokenKey privateKey = EcdsaKeyImporter.Import(ecdsa, true).Key;
        privateKey.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        privateKey.HasCoseAlgorithm = true;

        CoseKeyResult publicRoundTrip = CoseKey.Read(CoseKey.Write(publicKey).Bytes);
        CoseKeyResult privateRoundTrip = CoseKey.Read(CoseKey.Write(privateKey).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(publicRoundTrip.Success, Is.True);
            Assert.That(publicRoundTrip.Key.NativePrivateMaterial, Is.False);
            Assert.That(privateRoundTrip.Success, Is.True);
            Assert.That(privateRoundTrip.Key.NativePrivateMaterial, Is.True);
            Assert.That(privateRoundTrip.Key.CoseCurve,
                Is.EqualTo(CoseKeyLabels.CurveP256));
            Assert.That(ecdsa.ExportParameters(false).Q.X, Is.Not.Empty);
        });

        publicRoundTrip.Key.ReleaseOwnedMaterial();
        privateRoundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteSupportsEc2KeyAgreementAlgorithms()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, false).Key;
        key.CoseAlgorithm = CoseAlgorithms.EcdhEsHkdfSha256;
        key.HasCoseAlgorithm = true;

        TokenBytesResult written = CoseKey.Write(key);
        CoseKeyResult parsed = CoseKey.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.EcdhEsHkdfSha256));
            Assert.That(parsed.Key.CoseCurve, Is.EqualTo(CoseKeyLabels.CurveP256));
        });

        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteAndReadRoundTripOkpPrivateKey()
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.OctetKeyPair;
        key.CoseCurve = CoseKeyLabels.CurveEd25519;
        key.CoseAlgorithm = CoseAlgorithms.Ed25519;
        key.HasCoseAlgorithm = true;
        key.OkpPublicKey = Enumerable.Repeat((byte)2, 32).ToArray();
        key.OkpPrivateKey = Enumerable.Repeat((byte)8, 32).ToArray();
        byte[] retainedPrivate = key.OkpPrivateKey.ToArray();

        TokenBytesResult written = CoseKey.Write(key);
        CoseKeyResult roundTrip = CoseKey.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Key.OkpPublicKey, Is.EqualTo(key.OkpPublicKey));
            Assert.That(roundTrip.Key.OkpPrivateKey, Is.EqualTo(retainedPrivate));
            Assert.That(key.OkpPrivateKey, Is.EqualTo(retainedPrivate));
        });

        key.ReleaseOwnedMaterial();
        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteMapsJoseTypeAndAlgorithmMetadata()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        key.CoseKeyType = 0;
        key.Algorithm = JoseAlgorithms.HmacSha256;

        TokenBytesResult written = CoseKey.Write(key);
        CoseKeyResult parsed = CoseKey.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(parsed.Key.CoseKeyType,
                Is.EqualTo(CoseKeyLabels.KeyTypeSymmetric));
            Assert.That(parsed.Key.CoseAlgorithm,
                Is.EqualTo(CoseAlgorithms.HmacSha256));
            Assert.That(parsed.Key.HasCoseAlgorithm, Is.True);
        });

        key.ReleaseOwnedMaterial();
        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WritePreservesAbsentOptionalMetadata()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;

        TokenBytesResult written = CoseKey.Write(key);
        CborItem map = CborReader.Create(written.Bytes).Read().Item;

        Assert.Multiple(() =>
        {
            Assert.That(map.MapValue, Has.Length.EqualTo(2));
            Assert.That(MapValue(map, CoseKeyLabels.KeyId), Is.Null);
            Assert.That(MapValue(map, CoseKeyLabels.Algorithm), Is.Null);
            Assert.That(MapValue(map, CoseKeyLabels.KeyOperations), Is.Null);
        });

        key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteRejectsConflictingTypeMaterialAndAlgorithms()
    {
        TokenKey conflictingType = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        conflictingType.CoseKeyType = CoseKeyLabels.KeyTypeRsa;
        TokenKey conflictingMaterial = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        conflictingMaterial.OkpPublicKey = new byte[32];
        TokenKey conflictingAlgorithm = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        conflictingAlgorithm.Algorithm = JoseAlgorithms.HmacSha256;
        conflictingAlgorithm.CoseAlgorithm = CoseAlgorithms.HmacSha512;
        conflictingAlgorithm.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            AssertWriteFailure(CoseKey.Write(conflictingType), "conflicting_key_type");
            AssertWriteFailure(CoseKey.Write(conflictingMaterial),
                "conflicting_key_material");
            AssertWriteFailure(CoseKey.Write(conflictingAlgorithm),
                "conflicting_key_algorithm");
        });

        conflictingType.ReleaseOwnedMaterial();
        conflictingMaterial.ReleaseOwnedMaterial();
        conflictingAlgorithm.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteRejectsMissingInvalidAndUnsupportedValues()
    {
        TokenKey missing = new TokenKey();
        TokenKey unsupportedType = new TokenKey();
        unsupportedType.CoseKeyType = 99;
        TokenKey unknownAlgorithm = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        unknownAlgorithm.CoseAlgorithm = 9999;
        unknownAlgorithm.HasCoseAlgorithm = true;
        TokenKey duplicateOperation = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        duplicateOperation.CoseKeyOperations = new int[] { 9, 9 };
        TokenKey invalidOkp = new TokenKey();
        invalidOkp.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        invalidOkp.CoseCurve = CoseKeyLabels.CurveEd25519;
        invalidOkp.OkpPublicKey = new byte[31];

        Assert.Multiple(() =>
        {
            AssertWriteFailure(CoseKey.Write(missing), "missing_key_material");
            AssertWriteFailure(CoseKey.Write(unsupportedType), "unsupported_key_type");
            AssertWriteFailure(CoseKey.Write(unknownAlgorithm), "unknown_algorithm");
            AssertWriteFailure(CoseKey.Write(duplicateOperation),
                "duplicate_key_operation");
            AssertWriteFailure(CoseKey.Write(invalidOkp), "invalid_okp_key");
            Assert.That(() => CoseKey.Write(null), Throws.ArgumentNullException);
        });

        unknownAlgorithm.ReleaseOwnedMaterial();
        duplicateOperation.ReleaseOwnedMaterial();
    }

    static CborItem RsaMap(RSAParameters parameters, bool includePrivate)
    {
        List<CborMapEntry> entries = new List<CborMapEntry>();
        entries.Add(Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeRsa)));
        entries.Add(Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.RsaSha256)));
        entries.Add(Entry(CoseKeyLabels.RsaModulus, Bytes(parameters.Modulus)));
        entries.Add(Entry(CoseKeyLabels.RsaPublicExponent, Bytes(parameters.Exponent)));
        if (includePrivate)
        {
            entries.Add(Entry(CoseKeyLabels.RsaPrivateExponent, Bytes(parameters.D)));
            entries.Add(Entry(CoseKeyLabels.RsaFirstPrimeFactor, Bytes(parameters.P)));
            entries.Add(Entry(CoseKeyLabels.RsaSecondPrimeFactor, Bytes(parameters.Q)));
            entries.Add(Entry(CoseKeyLabels.RsaFirstFactorCrtExponent, Bytes(parameters.DP)));
            entries.Add(Entry(CoseKeyLabels.RsaSecondFactorCrtExponent, Bytes(parameters.DQ)));
            entries.Add(Entry(CoseKeyLabels.RsaCrtCoefficient, Bytes(parameters.InverseQ)));
        }

        return Map(entries.ToArray());
    }

    static CborItem Ec2Map(ECParameters parameters, bool includePrivate)
    {
        List<CborMapEntry> entries = new List<CborMapEntry>();
        entries.Add(Entry(CoseKeyLabels.KeyType, Int(CoseKeyLabels.KeyTypeEc2)));
        entries.Add(Entry(CoseKeyLabels.Algorithm, Int(CoseAlgorithms.EcdsaP256Sha256)));
        entries.Add(Entry(CoseKeyLabels.Curve, Int(CoseKeyLabels.CurveP256)));
        entries.Add(Entry(CoseKeyLabels.X, Bytes(parameters.Q.X)));
        entries.Add(Entry(CoseKeyLabels.Y, Bytes(parameters.Q.Y)));
        if (includePrivate)
        {
            entries.Add(Entry(CoseKeyLabels.PrivateKey, Bytes(parameters.D)));
        }

        return Map(entries.ToArray());
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult result = writer.WriteItem(item);
        Assert.That(result.Success, Is.True);
        return writer.ToArray();
    }

    static byte[] PrefixZero(byte[] value)
    {
        byte[] result = new byte[value.Length + 1];
        value.CopyTo(result, 1);
        return result;
    }

    static CborItem MapValue(CborItem map, int label)
    {
        foreach (CborMapEntry entry in map.MapValue)
        {
            int value;
            if (entry.Key.Kind == TokenCborValueKind.UnsignedInteger)
            {
                value = (int)entry.Key.IntegerArgument;
            }
            else
            {
                value = -1 - (int)entry.Key.IntegerArgument;
            }

            if (value == label)
            {
                return entry.Value;
            }
        }

        return null;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
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
            item.IntegerArgument = (ulong)(-(value + 1));
        }

        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static void AssertFailure(CoseKeyResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    static void AssertWriteFailure(TokenBytesResult result, string code)
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
