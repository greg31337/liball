using System.Security.Cryptography;
using token;

namespace token.Tests;

public class EcdsaKeyImporterTests
{
    [TestCase("P-256", CoseKeyLabels.CurveP256)]
    [TestCase("P-384", CoseKeyLabels.CurveP384)]
    [TestCase("P-521", CoseKeyLabels.CurveP521)]
    public void ImportCreatesNormalizedCurveContainer(string curveName, int coseCurve)
    {
        using ECDsa ecdsa = CreateCurve(curveName);
        TokenKeyResult result = EcdsaKeyImporter.Import(ecdsa, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeEc2));
            Assert.That(result.Key.Curve, Is.EqualTo(curveName));
            Assert.That(result.Key.CoseCurve, Is.EqualTo(coseCurve));
            Assert.That(result.Key.Ecdsa, Is.SameAs(ecdsa));
            Assert.That(result.Key.NativePublicMaterial, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.True);
            Assert.That(result.Key.ContainsPublicMaterial(), Is.True);
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.True);
        });
    }

    [Test]
    public void ImportDoesNotTakeOwnershipOfNativeKey()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKeyResult result = EcdsaKeyImporter.Import(ecdsa, true);
        byte[] data = new byte[] { 1, 2, 3 };
        byte[] signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);

        Assert.That(result.Key.Ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256), Is.True);
    }

    [TestCase(JoseAlgorithms.EcdsaSha256, "P-256", CoseKeyLabels.CurveP256)]
    [TestCase(JoseAlgorithms.EcdsaSha384, "P-384", CoseKeyLabels.CurveP384)]
    [TestCase(JoseAlgorithms.EcdsaSha512, "P-521", CoseKeyLabels.CurveP521)]
    public void JoseImportRequiresAlgorithmCurveMatch(
        string algorithm,
        string curveName,
        int coseCurve)
    {
        using ECDsa ecdsa = CreateCurve(curveName);
        TokenKeyResult result = EcdsaKeyImporter.ImportJose(ecdsa, algorithm, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.Algorithm, Is.EqualTo(algorithm));
            Assert.That(result.Key.CoseCurve, Is.EqualTo(coseCurve));
            Assert.That(result.Key.HasCoseAlgorithm, Is.False);
        });
    }

    [Test]
    public void JoseImportRejectsMismatchedCurve()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKeyResult result = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true);

        AssertFailure(result, "algorithm_curve_mismatch");
    }

    [TestCase(CoseAlgorithms.EcdsaP256Sha256, "P-256")]
    [TestCase(CoseAlgorithms.EcdsaP384Sha384, "P-384")]
    [TestCase(CoseAlgorithms.EcdsaP521Sha512, "P-521")]
    public void CoseImportAcceptsFullySpecifiedAlgorithms(int algorithm, string curveName)
    {
        using ECDsa ecdsa = CreateCurve(curveName);
        TokenKeyResult result = EcdsaKeyImporter.ImportCose(ecdsa, algorithm, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.CoseAlgorithm, Is.EqualTo(algorithm));
            Assert.That(result.Key.HasCoseAlgorithm, Is.True);
            Assert.That(result.Key.Algorithm, Is.Null);
        });
    }

    [TestCase(CoseAlgorithms.EcdsaSha256, "P-256")]
    [TestCase(CoseAlgorithms.EcdsaSha384, "P-384")]
    [TestCase(CoseAlgorithms.EcdsaSha512, "P-521")]
    public void DeprecatedCoseAlgorithmsRequireCompatibilityOptIn(
        int algorithm,
        string curveName)
    {
        using ECDsa ecdsa = CreateCurve(curveName);
        TokenKeyResult conservative = EcdsaKeyImporter.ImportCose(
            ecdsa,
            algorithm,
            true);
        TokenKeyResult compatible = EcdsaKeyImporter.ImportCose(
            ecdsa,
            algorithm,
            true,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
            Assert.That(compatible.Key.CoseAlgorithm, Is.EqualTo(algorithm));
        });
    }

    [Test]
    public void CoseImportRejectsMismatchedCurve()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKeyResult result = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP384Sha384,
            true);

        AssertFailure(result, "algorithm_curve_mismatch");
    }

    [Test]
    public void UnknownAndNonEcdsaAlgorithmsAreRejected()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKeyResult unknownJose = EcdsaKeyImporter.ImportJose(ecdsa, "unknown", false);
        TokenKeyResult rsaJose = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.RsaSha256,
            false);
        TokenKeyResult unknownCose = EcdsaKeyImporter.ImportCose(ecdsa, 999999, false);
        TokenKeyResult rsaCose = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.RsaSha256,
            false);

        Assert.Multiple(() =>
        {
            AssertFailure(unknownJose, "unknown_algorithm");
            AssertFailure(rsaJose, "invalid_key_algorithm");
            AssertFailure(unknownCose, "unknown_algorithm");
            AssertFailure(rsaCose, "invalid_key_algorithm");
        });
    }

    [Test]
    public void ValidationRejectsMissingMaterialAndWrongTypes()
    {
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.EllipticCurve;
        TokenKey wrongJoseType = new TokenKey();
        wrongJoseType.KeyType = TokenKeyTypes.Rsa;
        TokenKey wrongCoseType = new TokenKey();
        wrongCoseType.CoseKeyType = CoseKeyLabels.KeyTypeRsa;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(EcdsaKeyImporter.Validate(missing), "missing_key_material");
            AssertValidationFailure(EcdsaKeyImporter.Validate(wrongJoseType), "invalid_key_type");
            AssertValidationFailure(EcdsaKeyImporter.Validate(wrongCoseType), "invalid_key_type");
        });
    }

    [Test]
    public void ValidationEnforcesPrivateMaterialRequirement()
    {
        using ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters publicParameters = privateKey.ExportParameters(false);
        using ECDsa publicKey = ECDsa.Create(publicParameters);
        TokenKeyResult imported = EcdsaKeyImporter.Import(publicKey);

        Assert.Multiple(() =>
        {
            Assert.That(EcdsaKeyImporter.Validate(imported.Key).Success, Is.True);
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(imported.Key, true, false),
                "missing_private_key");
        });
    }

    [Test]
    public void ValidationUsesNativeCurveAsAuthoritative()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey wrongName = EcdsaKeyImporter.Import(ecdsa).Key;
        wrongName.Curve = "P-384";
        TokenKey wrongLabel = EcdsaKeyImporter.Import(ecdsa).Key;
        wrongLabel.CoseCurve = CoseKeyLabels.CurveP384;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(wrongName),
                "conflicting_key_curve");
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(wrongLabel),
                "conflicting_key_curve");
        });
    }

    [Test]
    public void ValidationChecksAlgorithmsAgainstCurveAndEachOther()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey matching = EcdsaKeyImporter.Import(ecdsa, true).Key;
        matching.Algorithm = JoseAlgorithms.EcdsaSha256;
        matching.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        matching.HasCoseAlgorithm = true;
        TokenKey wrongCurve = EcdsaKeyImporter.Import(ecdsa, true).Key;
        wrongCurve.Algorithm = JoseAlgorithms.EcdsaSha384;
        TokenKey conflicting = EcdsaKeyImporter.Import(ecdsa, true).Key;
        conflicting.Algorithm = JoseAlgorithms.EcdsaSha256;
        conflicting.CoseAlgorithm = CoseAlgorithms.EcdsaSha256;
        conflicting.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            Assert.That(EcdsaKeyImporter.Validate(matching, true, false).Success, Is.True);
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(wrongCurve, true, false),
                "algorithm_curve_mismatch");
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(conflicting, true, false),
                "compatibility_required");
            Assert.That(EcdsaKeyImporter.Validate(conflicting, true, true).Success, Is.True);
        });
    }

    [Test]
    public void ValidationRejectsAlgorithmFromAnotherKeyFamily()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey jose = EcdsaKeyImporter.Import(ecdsa).Key;
        jose.Algorithm = JoseAlgorithms.HmacSha256;
        TokenKey cose = EcdsaKeyImporter.Import(ecdsa).Key;
        cose.CoseAlgorithm = CoseAlgorithms.RsaPssSha256;
        cose.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(jose),
                "invalid_key_algorithm");
            AssertValidationFailure(
                EcdsaKeyImporter.Validate(cose),
                "invalid_key_algorithm");
        });
    }

    [Test]
    public void NullArgumentsAreProgrammerErrors()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        Assert.Multiple(() =>
        {
            Assert.That(() => EcdsaKeyImporter.Import(null), Throws.ArgumentNullException);
            Assert.That(() => EcdsaKeyImporter.ImportJose(null, JoseAlgorithms.EcdsaSha256, false), Throws.ArgumentNullException);
            Assert.That(() => EcdsaKeyImporter.ImportJose(ecdsa, null, false), Throws.ArgumentNullException);
            Assert.That(() => EcdsaKeyImporter.ImportCose(null, CoseAlgorithms.EcdsaP256Sha256, false), Throws.ArgumentNullException);
            Assert.That(() => EcdsaKeyImporter.Validate(null), Throws.ArgumentNullException);
        });
    }

    static ECDsa CreateCurve(string name)
    {
        if (name == "P-256")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        if (name == "P-384")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP521);
    }

    static void AssertFailure(TokenKeyResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }

    static void AssertValidationFailure(TokenResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
