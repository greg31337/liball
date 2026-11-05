using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using token;

namespace token.Tests;

public class JwkTests
{
    [Test]
    public void ReadSymmetricKeyParsesCommonMetadata()
    {
        byte[] secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        byte[] sha256 = Enumerable.Repeat((byte)7, 32).ToArray();
        string json = "{\"kty\":\"oct\",\"k\":\"" + B64(secret)
            + "\",\"kid\":\"key-1\",\"alg\":\"HS256\",\"use\":\"sig\""
            + ",\"key_ops\":[\"sign\",\"verify\"],\"x5t#S256\":\""
            + B64(sha256) + "\"}";

        JwkResult result = JsonWebKey.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeSymmetric));
            Assert.That(result.Key.SymmetricKey, Is.EqualTo(secret));
            Assert.That(result.Key.KeyId, Is.EqualTo("key-1"));
            Assert.That(result.Key.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Key.Use, Is.EqualTo(TokenKeyUses.Signature));
            Assert.That(result.Key.KeyOperations, Is.EqualTo(new[] { "sign", "verify" }));
            Assert.That(result.Key.CertificateSha256Thumbprint, Is.EqualTo(sha256));
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRsaPublicKeyCreatesOwnedNativeKey()
    {
        using RSA source = RSA.Create(2048);
        RSAParameters parameters = source.ExportParameters(false);
        string json = RsaJson(parameters, false, "RS256");

        JwkResult result = JsonWebKey.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeRsa));
            Assert.That(result.Key.Algorithm, Is.EqualTo(JoseAlgorithms.RsaSha256));
            Assert.That(result.Key.OwnsNativeKey, Is.True);
            Assert.That(result.Key.NativePublicMaterial, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.False);
            Assert.That(result.Key.Rsa.ExportParameters(false).Modulus, Is.EqualTo(parameters.Modulus));
        });

        result.Key.ReleaseOwnedMaterial();
        Assert.That(result.Key.Rsa, Is.Null);
    }

    [Test]
    public void ReadRsaPrivateKeyValidatesAndImportsCrtMaterial()
    {
        using RSA source = RSA.Create(2048);
        RSAParameters parameters = source.ExportParameters(true);

        JwkResult result = JsonWebKey.Read(RsaJson(parameters, true, null));

        byte[] data = RandomNumberGenerator.GetBytes(32);
        byte[] signature = result.Key.Rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.True);
            Assert.That(source.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1), Is.True);
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRsaPrivateKeyRejectsIncompleteAndConflictingCrtMaterial()
    {
        using RSA source = RSA.Create(2048);
        RSAParameters parameters = source.ExportParameters(true);
        string incomplete = "{\"kty\":\"RSA\",\"n\":\"" + UInt(parameters.Modulus)
            + "\",\"e\":\"" + UInt(parameters.Exponent) + "\",\"d\":\""
            + UInt(parameters.D) + "\"}";
        byte[] wrongDp = parameters.DP.ToArray();
        wrongDp[wrongDp.Length - 1] ^= 1;
        parameters.DP = wrongDp;

        JwkResult incompleteResult = JsonWebKey.Read(incomplete);
        JwkResult conflictingResult = JsonWebKey.Read(RsaJson(parameters, true, null));

        Assert.Multiple(() =>
        {
            AssertFailure(incompleteResult, "missing_jwk_member");
            AssertFailure(conflictingResult, "conflicting_key_material");
        });
    }

    [TestCase("P-256", "ES256")]
    [TestCase("P-384", "ES384")]
    [TestCase("P-521", "ES512")]
    public void ReadEcPublicKeyCreatesNamedCurveKey(string curveName, string algorithm)
    {
        using ECDsa source = CreateEc(curveName);
        ECParameters parameters = source.ExportParameters(false);
        string json = EcJson(curveName, parameters, false, algorithm);

        JwkResult result = JsonWebKey.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(result.Key.Curve, Is.EqualTo(curveName));
            Assert.That(result.Key.Algorithm, Is.EqualTo(algorithm));
            Assert.That(result.Key.OwnsNativeKey, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.False);
            Assert.That(result.Key.Ecdsa.ExportParameters(false).Q.X, Is.EqualTo(parameters.Q.X));
            Assert.That(result.Key.Ecdsa.ExportParameters(false).Q.Y, Is.EqualTo(parameters.Q.Y));
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadEcPrivateKeyChecksScalarAgainstPublicPoint()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters parameters = source.ExportParameters(true);
        JwkResult valid = JsonWebKey.Read(EcJson("P-256", parameters, true, "ES256"));
        byte[] wrongD = parameters.D.ToArray();
        wrongD[wrongD.Length - 1] ^= 1;
        parameters.D = wrongD;
        JwkResult conflict = JsonWebKey.Read(EcJson("P-256", parameters, true, "ES256"));

        byte[] data = RandomNumberGenerator.GetBytes(20);
        byte[] signature = valid.Key.Ecdsa.SignData(data, HashAlgorithmName.SHA256);
        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Key.NativePrivateMaterial, Is.True);
            Assert.That(source.VerifyData(data, signature, HashAlgorithmName.SHA256), Is.True);
            AssertFailure(conflict, "conflicting_key_material");
        });

        valid.Key.ReleaseOwnedMaterial();
    }

    [TestCase("X25519", 32, "ECDH-ES", CoseKeyLabels.CurveX25519)]
    [TestCase("X448", 56, "ECDH-ES+A256KW", CoseKeyLabels.CurveX448)]
    [TestCase("Ed25519", 32, "Ed25519", CoseKeyLabels.CurveEd25519)]
    [TestCase("Ed448", 57, "Ed448", CoseKeyLabels.CurveEd448)]
    public void ReadKnownOkpForms(
        string curve,
        int size,
        string algorithm,
        int coseCurve)
    {
        byte[] publicKey = Enumerable.Repeat((byte)3, size).ToArray();
        byte[] privateKey = Enumerable.Repeat((byte)4, size).ToArray();
        string json = "{\"kty\":\"OKP\",\"crv\":\"" + curve
            + "\",\"x\":\"" + B64(publicKey) + "\",\"d\":\""
            + B64(privateKey) + "\",\"alg\":\"" + algorithm + "\"}";

        JwkResult result = JsonWebKey.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeOkp));
            Assert.That(result.Key.Curve, Is.EqualTo(curve));
            Assert.That(result.Key.CoseCurve, Is.EqualTo(coseCurve));
            Assert.That(result.Key.OkpPublicKey, Is.EqualTo(publicKey));
            Assert.That(result.Key.OkpPrivateKey, Is.EqualTo(privateKey));
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.True);
        });

        result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadUtf8UsesStrictJsonAndRejectsDuplicateMembers()
    {
        byte[] valid = Encoding.UTF8.GetBytes(
            "{\"kty\":\"oct\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\"}");
        JwkResult parsed = JsonWebKey.Read(valid);
        JwkResult duplicate = JsonWebKey.Read(
            "{\"kty\":\"oct\",\"kty\":\"RSA\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\"}");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            AssertFailure(duplicate, "duplicate_json_member");
        });

        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadRejectsMissingWrongTypeAndInvalidBase64urlMembers()
    {
        JwkResult missingType = JsonWebKey.Read("{}");
        JwkResult wrongType = JsonWebKey.Read("{\"kty\":1}");
        JwkResult missingMaterial = JsonWebKey.Read("{\"kty\":\"oct\"}");
        JwkResult invalidEncoding = JsonWebKey.Read("{\"kty\":\"oct\",\"k\":\"AA==\"}");
        JwkResult wrongOperations = JsonWebKey.Read(
            "{\"kty\":\"oct\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\",\"key_ops\":[1]}");

        Assert.Multiple(() =>
        {
            AssertFailure(missingType, "missing_jwk_member");
            AssertFailure(wrongType, "invalid_jwk_member");
            AssertFailure(missingMaterial, "missing_jwk_member");
            AssertFailure(invalidEncoding, "invalid_base64url");
            AssertFailure(wrongOperations, "invalid_jwk_member");
        });
    }

    [Test]
    public void ReadValidatesCertificateThumbprintWidths()
    {
        string key = "{\"kty\":\"oct\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\"";
        JwkResult sha1 = JsonWebKey.Read(key + ",\"x5t\":\"AA\"}");
        JwkResult sha256 = JsonWebKey.Read(key + ",\"x5t#S256\":\"AA\"}");

        Assert.Multiple(() =>
        {
            AssertFailure(sha1, "invalid_certificate_thumbprint");
            AssertFailure(sha256, "invalid_certificate_thumbprint");
        });
    }

    [Test]
    public void ReadRejectsUnsupportedTypesCurvesAndAlgorithmConfusion()
    {
        JwkResult type = JsonWebKey.Read("{\"kty\":\"magic\"}");
        JwkResult curve = JsonWebKey.Read(
            "{\"kty\":\"OKP\",\"crv\":\"magic\",\"x\":\"AA\"}");
        JwkResult algorithm = JsonWebKey.Read(
            "{\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"alg\":\"RS256\"}");

        Assert.Multiple(() =>
        {
            AssertFailure(type, "unsupported_key_type");
            AssertFailure(curve, "unsupported_curve");
            AssertFailure(algorithm, "invalid_key_algorithm");
        });
    }

    [TestCase("A128CBC", 16)]
    [TestCase("A192CTR", 24)]
    [TestCase("A256CBC", 32)]
    [TestCase("HS1", 20)]
    public void ReadRejectsProhibitedJwkOnlySymmetricAlgorithms(string algorithm, int size)
    {
        string json = "{\"kty\":\"oct\",\"k\":\""
            + B64(new byte[size]) + "\",\"alg\":\"" + algorithm + "\"}";

        JwkResult result = JsonWebKey.Read(json);

        AssertFailure(result, "prohibited_algorithm");
    }

    [Test]
    public void ReadRejectsProhibitedJwkOnlyRsaSha1Metadata()
    {
        using RSA rsa = RSA.Create(2048);
        JwkResult result = JsonWebKey.Read(
            RsaJson(rsa.ExportParameters(false), false, JoseAlgorithms.RsaSha1));

        AssertFailure(result, "prohibited_algorithm");
    }

    [Test]
    public void ReadRejectsWeakAndNonCanonicalRsaPublicValues()
    {
        using RSA weak = RSA.Create(1024);
        RSAParameters parameters = weak.ExportParameters(false);
        JwkResult weakResult = JsonWebKey.Read(RsaJson(parameters, false, null));
        string nonCanonical = "{\"kty\":\"RSA\",\"n\":\"AAE\",\"e\":\"Aw\"}";
        JwkResult nonCanonicalResult = JsonWebKey.Read(nonCanonical);

        Assert.Multiple(() =>
        {
            AssertFailure(weakResult, "weak_key");
            AssertFailure(nonCanonicalResult, "invalid_unsigned_integer");
        });
    }

    [Test]
    public void ReadRejectsInvalidEcPointAndOkpLength()
    {
        string ec = "{\"kty\":\"EC\",\"crv\":\"P-256\",\"x\":\""
            + B64(new byte[32]) + "\",\"y\":\"" + B64(new byte[32]) + "\"}";
        string okp = "{\"kty\":\"OKP\",\"crv\":\"X25519\",\"x\":\"AA\"}";

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebKey.Read(ec), "invalid_ec_point");
            AssertFailure(JsonWebKey.Read(okp), "invalid_okp_key");
        });
    }

    [Test]
    public void ReleaseOwnedMaterialZeroesParsedSecrets()
    {
        JwkResult symmetric = JsonWebKey.Read(
            "{\"kty\":\"oct\",\"k\":\"AAAAAAAAAAAAAAAAAAAAAA\"}");
        JwkResult okp = JsonWebKey.Read(
            "{\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"d\":\"" + B64(Enumerable.Repeat((byte)9, 32).ToArray()) + "\"}");
        byte[] symmetricBytes = symmetric.Key.SymmetricKey;
        byte[] privateBytes = okp.Key.OkpPrivateKey;

        symmetric.Key.ReleaseOwnedMaterial();
        okp.Key.ReleaseOwnedMaterial();

        Assert.Multiple(() =>
        {
            Assert.That(symmetricBytes, Is.All.Zero);
            Assert.That(privateBytes, Is.All.Zero);
            Assert.That(symmetric.Key.SymmetricKey, Is.Null);
            Assert.That(okp.Key.OkpPrivateKey, Is.Null);
            Assert.That(okp.Key.ContainsPrivateMaterial(), Is.False);
        });
    }

    [Test]
    public void WriteSymmetricKeyProducesDeterministicEscapedJson()
    {
        byte[] secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            secret,
            JoseAlgorithms.HmacSha256).Key;
        key.KeyId = "key\"\\\n";
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[] { TokenKeyOperations.Sign, TokenKeyOperations.Verify };

        TokenTextResult first = JsonWebKey.Write(key);
        TokenTextResult second = JsonWebKey.Write(key);
        JwkResult roundTrip = JsonWebKey.Read(first.Text);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Error, Is.Null);
            Assert.That(second.Text, Is.EqualTo(first.Text));
            Assert.That(first.Text, Is.EqualTo(
                "{\"alg\":\"HS256\",\"k\":\"" + B64(secret)
                + "\",\"key_ops\":[\"sign\",\"verify\"],\"kid\":\"key\\\"\\\\\\n\""
                + ",\"kty\":\"oct\",\"use\":\"sig\"}"));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Key.SymmetricKey, Is.EqualTo(secret));
            Assert.That(roundTrip.Key.KeyId, Is.EqualTo(key.KeyId));
        });

        roundTrip.Key.ReleaseOwnedMaterial();
        SymmetricKeyImporterOperations.Clear(key);
    }

    [Test]
    public void WriteRsaPublicKeyUsesMinimalUnsignedIntegers()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters expected = rsa.ExportParameters(false);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            false).Key;
        key.KeyId = "rsa-public";

        TokenTextResult written = JsonWebKey.Write(key);
        JsonObjectResult json = JsonObject.Read(written.Text);
        JwkResult roundTrip = JsonWebKey.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(json.Object.Get("d"), Is.Null);
            Assert.That(json.Object.Get("n").StringValue, Does.Not.StartWith("AA"));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(
                roundTrip.Key.Rsa.ExportParameters(false).Modulus,
                Is.EqualTo(expected.Modulus));
            Assert.That(
                roundTrip.Key.Rsa.ExportParameters(false).Exponent,
                Is.EqualTo(expected.Exponent));
        });

        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteRsaPrivateKeyRoundTripsAllCrtMembers()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;

        TokenTextResult written = JsonWebKey.Write(key);
        JsonObjectResult json = JsonObject.Read(written.Text);
        JwkResult roundTrip = JsonWebKey.Read(written.Text);
        byte[] data = RandomNumberGenerator.GetBytes(16);
        byte[] signature = roundTrip.Key.Rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(json.Object.Get("d"), Is.Not.Null);
            Assert.That(json.Object.Get("p"), Is.Not.Null);
            Assert.That(json.Object.Get("q"), Is.Not.Null);
            Assert.That(json.Object.Get("dp"), Is.Not.Null);
            Assert.That(json.Object.Get("dq"), Is.Not.Null);
            Assert.That(json.Object.Get("qi"), Is.Not.Null);
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1), Is.True);
        });

        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [TestCase("P-256", "ES256")]
    [TestCase("P-384", "ES384")]
    [TestCase("P-521", "ES512")]
    public void WriteEcPrivateKeyRoundTripsNamedCurve(string curve, string algorithm)
    {
        using ECDsa ecdsa = CreateEc(curve);
        TokenKey key = EcdsaKeyImporter.ImportJose(ecdsa, algorithm, true).Key;

        TokenTextResult written = JsonWebKey.Write(key);
        JwkResult roundTrip = JsonWebKey.Read(written.Text);
        byte[] data = RandomNumberGenerator.GetBytes(18);
        byte[] signature = roundTrip.Key.Ecdsa.SignData(data, HashAlgorithmName.SHA256);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Does.Contain("\"d\":"));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Key.Curve, Is.EqualTo(curve));
            Assert.That(ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256), Is.True);
        });

        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [TestCase("X25519", 32, CoseKeyLabels.CurveX25519)]
    [TestCase("X448", 56, CoseKeyLabels.CurveX448)]
    [TestCase("Ed25519", 32, CoseKeyLabels.CurveEd25519)]
    [TestCase("Ed448", 57, CoseKeyLabels.CurveEd448)]
    public void WriteOkpKeyRoundTripsKnownForms(string curve, int size, int coseCurve)
    {
        TokenKey key = new TokenKey();
        key.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        key.CoseCurve = coseCurve;
        key.OkpPublicKey = Enumerable.Repeat((byte)5, size).ToArray();
        key.OkpPrivateKey = Enumerable.Repeat((byte)6, size).ToArray();

        TokenTextResult written = JsonWebKey.Write(key);
        JwkResult roundTrip = JsonWebKey.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Key.Curve, Is.EqualTo(curve));
            Assert.That(roundTrip.Key.OkpPublicKey, Is.EqualTo(key.OkpPublicKey));
            Assert.That(roundTrip.Key.OkpPrivateKey, Is.EqualTo(key.OkpPrivateKey));
        });

        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteMapsCoseTypeAndAlgorithmToJoseNames()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = new TokenKey();
        key.CoseKeyType = CoseKeyLabels.KeyTypeRsa;
        key.CoseAlgorithm = CoseAlgorithms.RsaSha256;
        key.HasCoseAlgorithm = true;
        key.Rsa = rsa;
        key.NativePublicMaterial = true;

        TokenTextResult result = JsonWebKey.Write(key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Does.Contain("\"alg\":\"RS256\""));
            Assert.That(result.Text, Does.Contain("\"kty\":\"RSA\""));
        });
    }

    [Test]
    public void WriteCertificateBackedKeyIncludesPublicMaterialAndX509Metadata()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(rsa);
        TokenKeyResult imported = X509KeyHelpers.Import(
            certificate,
            new X509Certificate2[] { certificate });
        imported.Key.KeyId = "certificate-key";

        TokenTextResult written = JsonWebKey.Write(imported.Key);
        JsonObjectResult json = JsonObject.Read(written.Text);
        JwkResult roundTrip = JsonWebKey.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(json.Object.Get("x5c").Kind, Is.EqualTo(TokenJsonValueKind.Array));
            Assert.That(json.Object.Get("x5c").ArrayValue, Has.Length.EqualTo(1));
            Assert.That(
                json.Object.Get("x5c").ArrayValue[0].StringValue,
                Is.EqualTo(Convert.ToBase64String(certificate.RawData)));
            Assert.That(
                json.Object.Get("x5t").StringValue,
                Is.EqualTo(B64(certificate.GetCertHash(HashAlgorithmName.SHA1))));
            Assert.That(
                json.Object.Get("x5t#S256").StringValue,
                Is.EqualTo(B64(certificate.GetCertHash(HashAlgorithmName.SHA256))));
            Assert.That(json.Object.Get("d"), Is.Null);
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
        });

        roundTrip.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteRejectsMissingConflictingAndInvalidMetadata()
    {
        TokenKey missing = new TokenKey();
        TokenKey conflict = new TokenKey();
        conflict.KeyType = TokenKeyTypes.Symmetric;
        conflict.SymmetricKey = new byte[32];
        conflict.OkpPublicKey = new byte[32];
        TokenKey badOperations = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        badOperations.KeyOperations = new string[] { null };
        TokenKey badThumbprint = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        badThumbprint.CertificateSha1Thumbprint = new byte[3];
        TokenKey badString = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        badString.KeyId = "\ud800";

        TokenTextResult missingResult = JsonWebKey.Write(missing);
        TokenTextResult conflictResult = JsonWebKey.Write(conflict);
        TokenTextResult operationsResult = JsonWebKey.Write(badOperations);
        TokenTextResult thumbprintResult = JsonWebKey.Write(badThumbprint);
        TokenTextResult stringResult = JsonWebKey.Write(badString);

        Assert.Multiple(() =>
        {
            AssertWriteFailure(missingResult, "missing_key_material");
            AssertWriteFailure(conflictResult, "conflicting_key_material");
            AssertWriteFailure(operationsResult, "unknown_key_operation");
            AssertWriteFailure(thumbprintResult, "invalid_certificate_thumbprint");
            AssertWriteFailure(stringResult, "invalid_json_string");
        });

        SymmetricKeyImporterOperations.Clear(badOperations);
        SymmetricKeyImporterOperations.Clear(badThumbprint);
        SymmetricKeyImporterOperations.Clear(badString);
    }

    [Test]
    public void WriteRejectsAlgorithmAndCurveConfusion()
    {
        TokenKey wrongFamily = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        wrongFamily.Algorithm = JoseAlgorithms.RsaSha256;
        using RSA rsa = RSA.Create(2048);
        TokenKey conflictingAlgorithms = RsaKeyImporterOperations.Import(rsa).Key;
        conflictingAlgorithms.Algorithm = JoseAlgorithms.RsaSha256;
        conflictingAlgorithms.CoseAlgorithm = CoseAlgorithms.RsaPssSha256;
        conflictingAlgorithms.HasCoseAlgorithm = true;
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey wrongCurve = EcdsaKeyImporter.Import(ecdsa).Key;
        wrongCurve.Curve = "P-384";

        TokenTextResult familyResult = JsonWebKey.Write(wrongFamily);
        TokenTextResult algorithmResult = JsonWebKey.Write(conflictingAlgorithms);
        TokenTextResult curveResult = JsonWebKey.Write(wrongCurve);

        Assert.Multiple(() =>
        {
            AssertWriteFailure(familyResult, "invalid_key_algorithm");
            AssertWriteFailure(algorithmResult, "conflicting_key_algorithm");
            AssertWriteFailure(curveResult, "conflicting_key_curve");
        });

        SymmetricKeyImporterOperations.Clear(wrongFamily);
    }

    [Test]
    public void WriteRejectsNullKey()
    {
        Assert.That(() => JsonWebKey.Write(null), Throws.ArgumentNullException);
    }

    [Test]
    public void WritePublicRsaStripsEveryPrivateMemberWithoutMutatingKey()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        key.KeyId = "rsa-private";

        TokenTextResult result = JsonWebKey.WritePublic(key);
        JsonObjectResult json = JsonObject.Read(result.Text);
        JwkResult parsed = JsonWebKey.Read(result.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(json.Object.Get("n"), Is.Not.Null);
            Assert.That(json.Object.Get("e"), Is.Not.Null);
            Assert.That(json.Object.Get("d"), Is.Null);
            Assert.That(json.Object.Get("p"), Is.Null);
            Assert.That(json.Object.Get("q"), Is.Null);
            Assert.That(json.Object.Get("dp"), Is.Null);
            Assert.That(json.Object.Get("dq"), Is.Null);
            Assert.That(json.Object.Get("qi"), Is.Null);
            Assert.That(json.Object.Get("kid").StringValue, Is.EqualTo("rsa-private"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.NativePrivateMaterial, Is.False);
            Assert.That(key.NativePrivateMaterial, Is.True);
            Assert.That(key.Rsa, Is.SameAs(rsa));
        });

        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WritePublicEcAndOkpStripPrivateMembers()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ec = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        TokenKey okp = new TokenKey();
        okp.KeyType = TokenKeyTypes.OctetKeyPair;
        okp.Curve = "Ed25519";
        okp.OkpPublicKey = Enumerable.Repeat((byte)4, 32).ToArray();
        okp.OkpPrivateKey = Enumerable.Repeat((byte)5, 32).ToArray();

        TokenTextResult ecResult = JsonWebKey.WritePublic(ec);
        TokenTextResult okpResult = JsonWebKey.WritePublic(okp);
        JsonObjectResult ecJson = JsonObject.Read(ecResult.Text);
        JsonObjectResult okpJson = JsonObject.Read(okpResult.Text);

        Assert.Multiple(() =>
        {
            Assert.That(ecResult.Success, Is.True);
            Assert.That(ecJson.Object.Get("x"), Is.Not.Null);
            Assert.That(ecJson.Object.Get("y"), Is.Not.Null);
            Assert.That(ecJson.Object.Get("d"), Is.Null);
            Assert.That(okpResult.Success, Is.True);
            Assert.That(okpJson.Object.Get("x"), Is.Not.Null);
            Assert.That(okpJson.Object.Get("d"), Is.Null);
            Assert.That(okp.OkpPrivateKey, Is.Not.Null);
        });
    }

    [Test]
    public void WritePublicRejectsSymmetricKeys()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;

        TokenTextResult result = JsonWebKey.WritePublic(key);

        AssertWriteFailure(result, "public_projection_unavailable");
        SymmetricKeyImporterOperations.Clear(key);
    }

    [Test]
    public void WritePrivateRequiresAndRetainsPrivateMaterial()
    {
        using RSA privateRsa = RSA.Create(2048);
        RSAParameters publicParameters = privateRsa.ExportParameters(false);
        using RSA publicRsa = RSA.Create(publicParameters);
        TokenKey privateKey = RsaKeyImporterOperations.Import(privateRsa, true).Key;
        TokenKey publicKey = RsaKeyImporterOperations.Import(publicRsa).Key;
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(new byte[32]).Key;

        TokenTextResult privateResult = JsonWebKey.WritePrivate(privateKey);
        TokenTextResult publicResult = JsonWebKey.WritePrivate(publicKey);
        TokenTextResult symmetricResult = JsonWebKey.WritePrivate(symmetric);
        JsonObjectResult privateJson = JsonObject.Read(privateResult.Text);
        JsonObjectResult symmetricJson = JsonObject.Read(symmetricResult.Text);

        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Success, Is.True);
            Assert.That(privateJson.Object.Get("d"), Is.Not.Null);
            AssertWriteFailure(publicResult, "private_projection_unavailable");
            Assert.That(symmetricResult.Success, Is.True);
            Assert.That(symmetricJson.Object.Get("k"), Is.Not.Null);
        });

        SymmetricKeyImporterOperations.Clear(symmetric);
    }

    [Test]
    public void WritePrivateRejectsPublicOnlyEcAndOkpKeys()
    {
        using ECDsa privateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEc = ECDsa.Create(privateEc.ExportParameters(false));
        TokenKey ec = EcdsaKeyImporter.Import(publicEc).Key;
        TokenKey okp = new TokenKey();
        okp.KeyType = TokenKeyTypes.OctetKeyPair;
        okp.Curve = "X25519";
        okp.OkpPublicKey = new byte[32];

        TokenTextResult ecResult = JsonWebKey.WritePrivate(ec);
        TokenTextResult okpResult = JsonWebKey.WritePrivate(okp);

        Assert.Multiple(() =>
        {
            AssertWriteFailure(ecResult, "private_projection_unavailable");
            AssertWriteFailure(okpResult, "private_projection_unavailable");
        });
    }

    [Test]
    public void WritePrivateEcAndOkpRetainPrivateMembers()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ec = EcdsaKeyImporter.Import(ecdsa, true).Key;
        TokenKey okp = new TokenKey();
        okp.KeyType = TokenKeyTypes.OctetKeyPair;
        okp.Curve = "Ed25519";
        okp.OkpPublicKey = new byte[32];
        okp.OkpPrivateKey = Enumerable.Repeat((byte)8, 32).ToArray();

        TokenTextResult ecResult = JsonWebKey.WritePrivate(ec);
        TokenTextResult okpResult = JsonWebKey.WritePrivate(okp);
        JsonObjectResult ecJson = JsonObject.Read(ecResult.Text);
        JsonObjectResult okpJson = JsonObject.Read(okpResult.Text);

        Assert.Multiple(() =>
        {
            Assert.That(ecResult.Success, Is.True);
            Assert.That(ecJson.Object.Get("d"), Is.Not.Null);
            Assert.That(okpResult.Success, Is.True);
            Assert.That(okpJson.Object.Get("d"), Is.Not.Null);
        });
    }

    [Test]
    public void WriteMinimalRetainsPrivateKeyParametersAndOmitsOptionalMetadata()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        key.KeyId = "minimal-key";
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[] { TokenKeyOperations.Sign };
        key.CertificateSha1Thumbprint = new byte[20];
        key.CertificateSha256Thumbprint = new byte[32];

        TokenTextResult result = JsonWebKey.WriteMinimal(key);
        JsonObjectResult json = JsonObject.Read(result.Text);
        JwkResult parsed = JsonWebKey.Read(result.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(json.Object.Members.Select(member => member.Name), Is.EquivalentTo(
                new string[] { "kty", "n", "e", "d", "p", "q", "dp", "dq", "qi" }));
            Assert.That(json.Object.Get("alg"), Is.Null);
            Assert.That(json.Object.Get("kid"), Is.Null);
            Assert.That(json.Object.Get("use"), Is.Null);
            Assert.That(json.Object.Get("key_ops"), Is.Null);
            Assert.That(json.Object.Get("x5t"), Is.Null);
            Assert.That(json.Object.Get("x5t#S256"), Is.Null);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Key.NativePrivateMaterial, Is.True);
        });

        parsed.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriteMinimalCertificateKeyOmitsX509Metadata()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(rsa);
        TokenKey key = X509KeyHelpers.Import(
            certificate,
            new X509Certificate2[] { certificate }).Key;
        key.KeyId = "certificate";

        TokenTextResult result = JsonWebKey.WriteMinimal(key);
        JsonObjectResult json = JsonObject.Read(result.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(json.Object.Get("kty").StringValue, Is.EqualTo("RSA"));
            Assert.That(json.Object.Get("n"), Is.Not.Null);
            Assert.That(json.Object.Get("e"), Is.Not.Null);
            Assert.That(json.Object.Get("x5c"), Is.Null);
            Assert.That(json.Object.Get("x5t"), Is.Null);
            Assert.That(json.Object.Get("x5t#S256"), Is.Null);
            Assert.That(json.Object.Get("kid"), Is.Null);
        });
    }

    [Test]
    public void ProjectionMethodsRejectNullKeys()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebKey.WritePublic(null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKey.WritePrivate(null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKey.WriteMinimal(null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ReadRejectsUnknownDuplicateAndConflictingOperationMetadata()
    {
        string prefix = "{\"kty\":\"oct\",\"k\":\""
            + B64(new byte[32]) + "\"";
        JwkResult unknownUse = JsonWebKey.Read(prefix + ",\"use\":\"other\"}");
        JwkResult unknownOperation = JsonWebKey.Read(
            prefix + ",\"key_ops\":[\"macCreate\"]}");
        JwkResult duplicate = JsonWebKey.Read(
            prefix + ",\"key_ops\":[\"sign\",\"sign\"]}");
        JwkResult useConflict = JsonWebKey.Read(
            prefix + ",\"use\":\"sig\",\"key_ops\":[\"encrypt\"]}");
        JwkResult algorithmConflict = JsonWebKey.Read(
            prefix + ",\"alg\":\"HS256\",\"key_ops\":[\"encrypt\"]}");
        JwkResult useAlgorithmConflict = JsonWebKey.Read(
            prefix + ",\"alg\":\"HS256\",\"use\":\"enc\"}");
        JwkResult useTypeConflict = JsonWebKey.Read(
            "{\"kty\":\"OKP\",\"crv\":\"X25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"use\":\"sig\"}");

        Assert.Multiple(() =>
        {
            AssertFailure(unknownUse, "unknown_key_use");
            AssertFailure(unknownOperation, "unknown_key_operation");
            AssertFailure(duplicate, "duplicate_key_operation");
            AssertFailure(useConflict, "key_use_operation_mismatch");
            AssertFailure(algorithmConflict, "key_algorithm_operation_mismatch");
            AssertFailure(useAlgorithmConflict, "key_use_algorithm_mismatch");
            AssertFailure(useTypeConflict, "key_use_type_mismatch");
        });
    }

    [Test]
    public void ReadEnforcesKeyTypeAndMaterialCapabilities()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters rsaPublic = rsa.ExportParameters(false);
        string rsaJson = RsaJson(rsaPublic, false, JoseAlgorithms.RsaSha256);
        JwkResult unavailable = JsonWebKey.Read(
            AddJsonMembers(rsaJson, "\"key_ops\":[\"sign\"]"));
        JwkResult validPublic = JsonWebKey.Read(
            AddJsonMembers(rsaJson, "\"use\":\"sig\",\"key_ops\":[\"verify\"]"));

        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string ecJson = EcJson("P-256", ecdsa.ExportParameters(false), false, null);
        JwkResult wrongType = JsonWebKey.Read(
            AddJsonMembers(ecJson, "\"key_ops\":[\"encrypt\"]"));

        Assert.Multiple(() =>
        {
            AssertFailure(unavailable, "key_operation_unavailable");
            Assert.That(validPublic.Success, Is.True);
            Assert.That(validPublic.Key.KeyOperations, Is.EqualTo(new[] { "verify" }));
            AssertFailure(wrongType, "key_type_operation_mismatch");
        });

        validPublic.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ReadAcceptsValidSignatureEncryptionAndDerivationOperations()
    {
        string symmetric = "{\"kty\":\"oct\",\"k\":\""
            + B64(new byte[32])
            + "\",\"alg\":\"A256GCM\",\"use\":\"enc\""
            + ",\"key_ops\":[\"encrypt\",\"decrypt\"]}";
        string x25519 = "{\"kty\":\"OKP\",\"crv\":\"X25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"d\":\"" + B64(new byte[32])
            + "\",\"alg\":\"ECDH-ES\",\"use\":\"enc\""
            + ",\"key_ops\":[\"deriveKey\",\"deriveBits\"]}";
        string ed25519 = "{\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\""
            + B64(new byte[32]) + "\",\"d\":\"" + B64(new byte[32])
            + "\",\"alg\":\"Ed25519\",\"use\":\"sig\""
            + ",\"key_ops\":[\"sign\",\"verify\"]}";

        JwkResult symmetricResult = JsonWebKey.Read(symmetric);
        JwkResult x25519Result = JsonWebKey.Read(x25519);
        JwkResult ed25519Result = JsonWebKey.Read(ed25519);

        Assert.Multiple(() =>
        {
            Assert.That(symmetricResult.Success, Is.True);
            Assert.That(x25519Result.Success, Is.True);
            Assert.That(ed25519Result.Success, Is.True);
        });

        symmetricResult.Key.ReleaseOwnedMaterial();
        x25519Result.Key.ReleaseOwnedMaterial();
        ed25519Result.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void WriterRejectsInvalidUseAndOperationMetadata()
    {
        TokenKey unknownUse = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        unknownUse.Use = "other";
        TokenKey duplicate = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        duplicate.KeyOperations = new string[] { "encrypt", "encrypt" };
        using RSA rsa = RSA.Create(2048);
        TokenKey publicRsa = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            false).Key;
        publicRsa.KeyOperations = new string[] { TokenKeyOperations.Sign };

        TokenTextResult useResult = JsonWebKey.Write(unknownUse);
        TokenTextResult duplicateResult = JsonWebKey.Write(duplicate);
        TokenTextResult capabilityResult = JsonWebKey.Write(publicRsa);

        Assert.Multiple(() =>
        {
            AssertWriteFailure(useResult, "unknown_key_use");
            AssertWriteFailure(duplicateResult, "duplicate_key_operation");
            AssertWriteFailure(capabilityResult, "key_operation_unavailable");
        });

        SymmetricKeyImporterOperations.Clear(unknownUse);
        SymmetricKeyImporterOperations.Clear(duplicate);
    }

    [Test]
    public void PublicProjectionRejectsOperationsThatNeedPrivateMaterial()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[] { TokenKeyOperations.Sign };

        TokenTextResult result = JsonWebKey.WritePublic(key);

        AssertWriteFailure(result, "key_operation_unavailable");
    }

    [Test]
    public void ValidateUseAndOperationsMapsCoseAlgorithmMetadata()
    {
        TokenKey valid = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        valid.CoseAlgorithm = CoseAlgorithms.Aes256Gcm;
        valid.HasCoseAlgorithm = true;
        valid.Use = TokenKeyUses.Encryption;
        valid.KeyOperations = new string[]
        {
            TokenKeyOperations.Encrypt,
            TokenKeyOperations.Decrypt
        };
        TokenKey invalid = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        invalid.CoseAlgorithm = CoseAlgorithms.Aes256Gcm;
        invalid.HasCoseAlgorithm = true;
        invalid.KeyOperations = new string[] { TokenKeyOperations.Sign };

        TokenResult validResult = JsonWebKey.ValidateUseAndOperations(valid);
        TokenResult invalidResult = JsonWebKey.ValidateUseAndOperations(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(
                invalidResult.Error.Code,
                Is.EqualTo("key_algorithm_operation_mismatch"));
            Assert.That(
                () => JsonWebKey.ValidateUseAndOperations(null),
                Throws.ArgumentNullException);
        });

        SymmetricKeyImporterOperations.Clear(valid);
        SymmetricKeyImporterOperations.Clear(invalid);
    }

    [Test]
    public void ReadRejectsNullInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebKey.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebKey.Read((byte[])null), Throws.ArgumentNullException);
        });
    }

    static string RsaJson(RSAParameters parameters, bool includePrivate, string algorithm)
    {
        string json = "{\"kty\":\"RSA\",\"n\":\"" + UInt(parameters.Modulus)
            + "\",\"e\":\"" + UInt(parameters.Exponent) + "\"";
        if (includePrivate)
        {
            json += ",\"d\":\"" + UInt(parameters.D) + "\",\"p\":\""
                + UInt(parameters.P) + "\",\"q\":\"" + UInt(parameters.Q)
                + "\",\"dp\":\"" + UInt(parameters.DP) + "\",\"dq\":\""
                + UInt(parameters.DQ) + "\",\"qi\":\"" + UInt(parameters.InverseQ) + "\"";
        }

        if (algorithm != null)
        {
            json += ",\"alg\":\"" + algorithm + "\"";
        }

        return json + "}";
    }

    static string EcJson(
        string curve,
        ECParameters parameters,
        bool includePrivate,
        string algorithm)
    {
        string json = "{\"kty\":\"EC\",\"crv\":\"" + curve + "\",\"x\":\""
            + B64(parameters.Q.X) + "\",\"y\":\"" + B64(parameters.Q.Y) + "\"";
        if (includePrivate)
        {
            json += ",\"d\":\"" + B64(parameters.D) + "\"";
        }

        if (algorithm != null)
        {
            json += ",\"alg\":\"" + algorithm + "\"";
        }

        return json + "}";
    }

    static ECDsa CreateEc(string curve)
    {
        if (curve == "P-256")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        if (curve == "P-384")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP521);
    }

    static string UInt(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length - 1 && bytes[index] == 0)
        {
            index++;
        }

        return B64(bytes.AsSpan(index).ToArray());
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    static string AddJsonMembers(string json, string members)
    {
        return json.Substring(0, json.Length - 1) + "," + members + "}";
    }

    static X509Certificate2 CreateRsaCertificate(RSA rsa)
    {
        CertificateRequest request = new CertificateRequest(
            "CN=jwk-write-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            rsa,
            RSASignaturePadding.Pkcs1);
        return request.Create(
            request.SubjectName,
            generator,
            start,
            start.AddDays(2),
            new byte[] { 1 });
    }

    static void AssertFailure(JwkResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    static void AssertWriteFailure(TokenTextResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }
}
