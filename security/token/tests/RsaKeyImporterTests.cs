using System.Security.Cryptography;
using token;

namespace token.Tests;

public class RsaKeyImporterTests
{
    [Test]
    public void ImportCreatesNormalizedContainerWithoutTakingOwnership()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult result = RsaKeyImporterOperations.Import(rsa);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key, Is.Not.Null);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeRsa));
            Assert.That(result.Key.Rsa, Is.SameAs(rsa));
            Assert.That(result.Key.NativePublicMaterial, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.False);
            Assert.That(result.Key.ContainsPublicMaterial(), Is.True);
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.False);
        });

        byte[] data = new byte[] { 1, 2, 3 };
        byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.That(result.Key.Rsa.VerifyData(
            data,
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1), Is.True);
    }

    [Test]
    public void ImportPreservesExplicitPrivateCapabilityDeclaration()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult result = RsaKeyImporterOperations.Import(rsa, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.True);
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.True);
            Assert.That(RsaKeyImporterOperations.Validate(result.Key, true, false).Success, Is.True);
        });
    }

    [Test]
    public void ImportRejectsRsaKeysBelow2048Bits()
    {
        using RSA rsa = RSA.Create(1024);
        TokenKeyResult result = RsaKeyImporterOperations.Import(rsa);

        AssertFailure(result, "weak_key");
    }

    [Test]
    public void JoseImportAcceptsEveryRsaAlgorithm()
    {
        string[] algorithms = new string[]
        {
            JoseAlgorithms.RsaSha256, JoseAlgorithms.RsaSha384,
            JoseAlgorithms.RsaSha512, JoseAlgorithms.RsaPssSha256,
            JoseAlgorithms.RsaPssSha384, JoseAlgorithms.RsaPssSha512,
            JoseAlgorithms.RsaOaep, JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.RsaOaepSha384, JoseAlgorithms.RsaOaepSha512
        };
        using RSA rsa = RSA.Create(2048);

        foreach (string algorithm in algorithms)
        {
            TokenKeyResult result = RsaKeyImporterOperations.ImportJose(rsa, algorithm, false);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, algorithm);
                Assert.That(result.Key.Algorithm, Is.EqualTo(algorithm));
                Assert.That(result.Key.HasCoseAlgorithm, Is.False);
                Assert.That(result.Key.Rsa, Is.SameAs(rsa));
            });
        }
    }

    [Test]
    public void CoseImportAcceptsEveryCurrentRsaAlgorithm()
    {
        int[] algorithms = new int[]
        {
            CoseAlgorithms.RsaSha256, CoseAlgorithms.RsaSha384,
            CoseAlgorithms.RsaSha512, CoseAlgorithms.RsaPssSha256,
            CoseAlgorithms.RsaPssSha384, CoseAlgorithms.RsaPssSha512,
            CoseAlgorithms.RsaOaep, CoseAlgorithms.RsaOaepSha256,
            CoseAlgorithms.RsaOaepSha512
        };
        using RSA rsa = RSA.Create(2048);

        foreach (int algorithm in algorithms)
        {
            TokenKeyResult result = RsaKeyImporterOperations.ImportCose(rsa, algorithm, true);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, algorithm.ToString());
                Assert.That(result.Key.CoseAlgorithm, Is.EqualTo(algorithm));
                Assert.That(result.Key.HasCoseAlgorithm, Is.True);
                Assert.That(result.Key.Algorithm, Is.Null);
                Assert.That(result.Key.NativePrivateMaterial, Is.True);
            });
        }
    }

    [Test]
    public void LegacyRsaEncryptionRequiresCompatibilityOptIn()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult conservative = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true);
        TokenKeyResult compatible = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
            Assert.That(compatible.Key.Algorithm, Is.EqualTo(JoseAlgorithms.RsaPkcs1));
        });
    }

    [Test]
    public void DeprecatedCoseRsaSha1RequiresCompatibilityOptIn()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult conservative = RsaKeyImporterOperations.ImportCose(
            rsa,
            CoseAlgorithms.RsaSha1,
            true);
        TokenKeyResult compatible = RsaKeyImporterOperations.ImportCose(
            rsa,
            CoseAlgorithms.RsaSha1,
            true,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
            Assert.That(compatible.Key.CoseAlgorithm, Is.EqualTo(CoseAlgorithms.RsaSha1));
        });
    }

    [Test]
    public void ProhibitedJoseRsaSha1CannotBeImportedWithOptIn()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult result = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha1,
            true,
            true);

        AssertFailure(result, "prohibited_algorithm");
    }

    [Test]
    public void UnknownAndNonRsaAlgorithmsAreRejected()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKeyResult unknownJose = RsaKeyImporterOperations.ImportJose(rsa, "unknown", false);
        TokenKeyResult symmetricJose = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.HmacSha256,
            false);
        TokenKeyResult unknownCose = RsaKeyImporterOperations.ImportCose(rsa, 999999, false);
        TokenKeyResult symmetricCose = RsaKeyImporterOperations.ImportCose(
            rsa,
            CoseAlgorithms.Aes256Gcm,
            false);

        Assert.Multiple(() =>
        {
            AssertFailure(unknownJose, "unknown_algorithm");
            AssertFailure(symmetricJose, "invalid_key_algorithm");
            AssertFailure(unknownCose, "unknown_algorithm");
            AssertFailure(symmetricCose, "invalid_key_algorithm");
        });
    }

    [Test]
    public void ValidationRejectsMissingMaterialWrongTypesAndWeakKeys()
    {
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.Rsa;
        TokenKey wrongJoseType = new TokenKey();
        wrongJoseType.KeyType = TokenKeyTypes.Symmetric;
        TokenKey wrongCoseType = new TokenKey();
        wrongCoseType.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        using RSA weakRsa = RSA.Create(1024);
        TokenKey weak = new TokenKey();
        weak.Rsa = weakRsa;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(RsaKeyImporterOperations.Validate(missing), "missing_key_material");
            AssertValidationFailure(RsaKeyImporterOperations.Validate(wrongJoseType), "invalid_key_type");
            AssertValidationFailure(RsaKeyImporterOperations.Validate(wrongCoseType), "invalid_key_type");
            AssertValidationFailure(RsaKeyImporterOperations.Validate(weak), "weak_key");
        });
    }

    [Test]
    public void ValidationEnforcesPrivateMaterialRequirement()
    {
        using RSA privateRsa = RSA.Create(2048);
        RSAParameters publicParameters = privateRsa.ExportParameters(false);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(publicParameters);
        TokenKeyResult imported = RsaKeyImporterOperations.Import(publicRsa);

        Assert.Multiple(() =>
        {
            Assert.That(RsaKeyImporterOperations.Validate(imported.Key).Success, Is.True);
            AssertValidationFailure(
                RsaKeyImporterOperations.Validate(imported.Key, true, false),
                "missing_private_key");
        });
    }

    [Test]
    public void ValidationChecksBothAlgorithmIdentifiersForAgreement()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey matching = RsaKeyImporterOperations.Import(rsa, true).Key;
        matching.Algorithm = JoseAlgorithms.RsaPssSha256;
        matching.CoseAlgorithm = CoseAlgorithms.RsaPssSha256;
        matching.HasCoseAlgorithm = true;
        TokenKey conflicting = RsaKeyImporterOperations.Import(rsa, true).Key;
        conflicting.Algorithm = JoseAlgorithms.RsaPssSha256;
        conflicting.CoseAlgorithm = CoseAlgorithms.RsaSha256;
        conflicting.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            Assert.That(RsaKeyImporterOperations.Validate(matching, true, false).Success, Is.True);
            AssertValidationFailure(
                RsaKeyImporterOperations.Validate(conflicting, true, false),
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void ValidationRejectsAlgorithmFromAnotherKeyFamily()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey jose = RsaKeyImporterOperations.Import(rsa).Key;
        jose.Algorithm = JoseAlgorithms.EcdsaSha256;
        TokenKey cose = RsaKeyImporterOperations.Import(rsa).Key;
        cose.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        cose.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                RsaKeyImporterOperations.Validate(jose),
                "invalid_key_algorithm");
            AssertValidationFailure(
                RsaKeyImporterOperations.Validate(cose),
                "invalid_key_algorithm");
        });
    }

    [Test]
    public void NullArgumentsAreProgrammerErrors()
    {
        using RSA rsa = RSA.Create(2048);

        Assert.Multiple(() =>
        {
            Assert.That(() => RsaKeyImporterOperations.Import(null), Throws.ArgumentNullException);
            Assert.That(() => RsaKeyImporterOperations.ImportJose(null, JoseAlgorithms.RsaSha256, false), Throws.ArgumentNullException);
            Assert.That(() => RsaKeyImporterOperations.ImportJose(rsa, null, false), Throws.ArgumentNullException);
            Assert.That(() => RsaKeyImporterOperations.ImportCose(null, CoseAlgorithms.RsaSha256, false), Throws.ArgumentNullException);
            Assert.That(() => RsaKeyImporterOperations.Validate(null), Throws.ArgumentNullException);
        });
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
