using token;

namespace token.Tests;

public class SymmetricKeyImporterTests
{
    [Test]
    public void GenericImportRequiresAtLeast128BitsAndCopiesInput()
    {
        byte[] source = Bytes(16, 7);
        TokenKeyResult result = SymmetricKeyImporterOperations.Import(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key, Is.Not.Null);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeSymmetric));
            Assert.That(result.Key.SymmetricKey, Is.EqualTo(source));
            Assert.That(result.Key.SymmetricKey, Is.Not.SameAs(source));
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.True);
        });

        source[0] = 99;
        Assert.That(result.Key.SymmetricKey[0], Is.EqualTo(7));
    }

    [Test]
    public void GenericImportRejectsWeakKey()
    {
        TokenKeyResult result = SymmetricKeyImporterOperations.Import(Bytes(15, 1));

        AssertFailure(result, "weak_key");
    }

    [TestCase(JoseAlgorithms.HmacSha256, 31, false)]
    [TestCase(JoseAlgorithms.HmacSha256, 32, true)]
    [TestCase(JoseAlgorithms.HmacSha256, 64, true)]
    [TestCase(JoseAlgorithms.HmacSha384, 47, false)]
    [TestCase(JoseAlgorithms.HmacSha384, 48, true)]
    [TestCase(JoseAlgorithms.HmacSha512, 63, false)]
    [TestCase(JoseAlgorithms.HmacSha512, 64, true)]
    public void JoseHmacImportEnforcesHashSizedMinimum(
        string algorithm,
        int length,
        bool success)
    {
        TokenKeyResult result = SymmetricKeyImporterOperations.ImportJose(
            Bytes(length, 2),
            algorithm);

        Assert.That(result.Success, Is.EqualTo(success));
        if (success)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Error, Is.Null);
                Assert.That(result.Key.Algorithm, Is.EqualTo(algorithm));
                Assert.That(result.Key.HasCoseAlgorithm, Is.False);
            });
        }
        else
        {
            AssertFailure(result, "weak_key");
        }
    }

    [TestCase(JoseAlgorithms.Aes128KeyWrap, 16)]
    [TestCase(JoseAlgorithms.Aes192GcmKeyWrap, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    [TestCase(JoseAlgorithms.Aes192CbcHmacSha384, 48)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void JoseAesImportRequiresExactAlgorithmSize(string algorithm, int length)
    {
        TokenKeyResult exact = SymmetricKeyImporterOperations.ImportJose(Bytes(length, 3), algorithm);
        TokenKeyResult shortKey = SymmetricKeyImporterOperations.ImportJose(Bytes(length - 1, 3), algorithm);
        TokenKeyResult longKey = SymmetricKeyImporterOperations.ImportJose(Bytes(length + 1, 3), algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(shortKey.Success, Is.False);
            Assert.That(shortKey.Error.Code, Is.EqualTo("invalid_key_size"));
            Assert.That(longKey.Success, Is.False);
            Assert.That(longKey.Error.Code, Is.EqualTo("invalid_key_size"));
        });
    }

    [TestCase(CoseAlgorithms.HmacSha256, 32)]
    [TestCase(CoseAlgorithms.HmacSha384, 48)]
    [TestCase(CoseAlgorithms.HmacSha512, 64)]
    public void CoseHmacImportEnforcesMinimumSize(int algorithm, int length)
    {
        TokenKeyResult exact = SymmetricKeyImporterOperations.ImportCose(Bytes(length, 4), algorithm);
        TokenKeyResult longer = SymmetricKeyImporterOperations.ImportCose(Bytes(length + 8, 4), algorithm);
        TokenKeyResult shortKey = SymmetricKeyImporterOperations.ImportCose(Bytes(length - 1, 4), algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(longer.Success, Is.True);
            Assert.That(shortKey.Success, Is.False);
            Assert.That(shortKey.Error.Code, Is.EqualTo("weak_key"));
            Assert.That(exact.Key.CoseAlgorithm, Is.EqualTo(algorithm));
            Assert.That(exact.Key.HasCoseAlgorithm, Is.True);
            Assert.That(exact.Key.Algorithm, Is.Null);
        });
    }

    [TestCase(CoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(CoseAlgorithms.Aes192KeyWrap, 24)]
    [TestCase(CoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(CoseAlgorithms.AesCcm16_64_128, 16)]
    [TestCase(CoseAlgorithms.AesCcm64_128_256, 32)]
    [TestCase(CoseAlgorithms.AesMac128_128, 16)]
    [TestCase(CoseAlgorithms.AesMac256_64, 32)]
    [TestCase(CoseAlgorithms.ChaCha20Poly1305, 32)]
    public void CoseContentAndMacAlgorithmsRequireExactSize(int algorithm, int length)
    {
        TokenKeyResult exact = SymmetricKeyImporterOperations.ImportCose(Bytes(length, 5), algorithm);
        TokenKeyResult wrong = SymmetricKeyImporterOperations.ImportCose(Bytes(length + 1, 5), algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(wrong.Success, Is.False);
            Assert.That(wrong.Error.Code, Is.EqualTo("invalid_key_size"));
        });
    }

    [TestCase(CoseAlgorithms.DirectHkdfAes128, 16)]
    [TestCase(CoseAlgorithms.DirectHkdfAes256, 32)]
    [TestCase(CoseAlgorithms.DirectHkdfSha256, 32)]
    [TestCase(CoseAlgorithms.DirectHkdfSha512, 64)]
    public void CoseDirectDerivationUsesMinimumSharedSecretSize(int algorithm, int length)
    {
        TokenKeyResult exact = SymmetricKeyImporterOperations.ImportCose(Bytes(length, 6), algorithm);
        TokenKeyResult longer = SymmetricKeyImporterOperations.ImportCose(Bytes(length + 1, 6), algorithm);
        TokenKeyResult shortKey = SymmetricKeyImporterOperations.ImportCose(Bytes(length - 1, 6), algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(longer.Success, Is.True);
            Assert.That(shortKey.Success, Is.False);
            Assert.That(shortKey.Error.Code, Is.EqualTo("weak_key"));
        });
    }

    [Test]
    public void DeprecatedCoseRawAesRequiresCompatibilityOptIn()
    {
        TokenKeyResult conservative = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 8),
            CoseAlgorithms.Aes128Cbc);
        TokenKeyResult compatible = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 8),
            CoseAlgorithms.Aes128Cbc,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
            Assert.That(compatible.Key.CoseAlgorithm, Is.EqualTo(CoseAlgorithms.Aes128Cbc));
        });
    }

    [Test]
    public void ProhibitedJoseAlgorithmsCannotBeImportedWithCompatibilityOptIn()
    {
        TokenKeyResult result = SymmetricKeyImporterOperations.ImportJose(
            Bytes(20, 9),
            JoseAlgorithms.HmacSha1,
            true);

        AssertFailure(result, "prohibited_algorithm");
    }

    [Test]
    public void UnknownAndNonSymmetricAlgorithmsAreRejected()
    {
        TokenKeyResult unknownJose = SymmetricKeyImporterOperations.ImportJose(Bytes(32, 1), "unknown");
        TokenKeyResult asymmetricJose = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.RsaSha256);
        TokenKeyResult compatibilityAsymmetricJose = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.RsaPkcs1);
        TokenKeyResult unknownCose = SymmetricKeyImporterOperations.ImportCose(Bytes(32, 1), 999999);
        TokenKeyResult asymmetricCose = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.EcdsaP256Sha256);
        TokenKeyResult compatibilityAsymmetricCose = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.EcdsaSha256);

        Assert.Multiple(() =>
        {
            AssertFailure(unknownJose, "unknown_algorithm");
            AssertFailure(asymmetricJose, "invalid_key_algorithm");
            AssertFailure(compatibilityAsymmetricJose, "invalid_key_algorithm");
            AssertFailure(unknownCose, "unknown_algorithm");
            AssertFailure(asymmetricCose, "invalid_key_algorithm");
            AssertFailure(compatibilityAsymmetricCose, "invalid_key_algorithm");
        });
    }

    [Test]
    public void ValidationRejectsMissingMaterialAndWrongTypes()
    {
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.Symmetric;
        TokenKey wrongJoseType = new TokenKey();
        wrongJoseType.KeyType = TokenKeyTypes.Rsa;
        wrongJoseType.SymmetricKey = Bytes(32, 1);
        TokenKey wrongCoseType = new TokenKey();
        wrongCoseType.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        wrongCoseType.SymmetricKey = Bytes(32, 1);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(SymmetricKeyImporterOperations.Validate(missing), "missing_key_material");
            AssertValidationFailure(SymmetricKeyImporterOperations.Validate(wrongJoseType), "invalid_key_type");
            AssertValidationFailure(SymmetricKeyImporterOperations.Validate(wrongCoseType), "invalid_key_type");
        });
    }

    [Test]
    public void ValidationChecksBothAlgorithmIdentifiersForAgreement()
    {
        TokenKey valid = new TokenKey();
        valid.KeyType = TokenKeyTypes.Symmetric;
        valid.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        valid.Algorithm = JoseAlgorithms.Aes256Gcm;
        valid.CoseAlgorithm = CoseAlgorithms.Aes256Gcm;
        valid.HasCoseAlgorithm = true;
        valid.SymmetricKey = Bytes(32, 1);
        TokenKey conflicting = new TokenKey();
        conflicting.KeyType = TokenKeyTypes.Symmetric;
        conflicting.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        conflicting.Algorithm = JoseAlgorithms.Aes256Gcm;
        conflicting.CoseAlgorithm = CoseAlgorithms.Aes256KeyWrap;
        conflicting.HasCoseAlgorithm = true;
        conflicting.SymmetricKey = Bytes(32, 1);

        Assert.Multiple(() =>
        {
            Assert.That(SymmetricKeyImporterOperations.Validate(valid).Success, Is.True);
            AssertValidationFailure(
                SymmetricKeyImporterOperations.Validate(conflicting),
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void ValidationAppliesGenericMinimumWhenAlgorithmIsAbsent()
    {
        TokenKey weak = new TokenKey();
        weak.SymmetricKey = Bytes(15, 1);
        TokenKey sufficient = new TokenKey();
        sufficient.SymmetricKey = Bytes(16, 1);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(SymmetricKeyImporterOperations.Validate(weak), "weak_key");
            Assert.That(SymmetricKeyImporterOperations.Validate(sufficient).Success, Is.True);
        });
    }

    [Test]
    public void ClearZeroesImportedCopyWithoutChangingCallerInput()
    {
        byte[] source = Bytes(32, 11);
        TokenKeyResult imported = SymmetricKeyImporterOperations.Import(source);
        byte[] owned = imported.Key.SymmetricKey;

        SymmetricKeyImporterOperations.Clear(imported.Key);

        Assert.Multiple(() =>
        {
            Assert.That(owned, Is.All.Zero);
            Assert.That(imported.Key.SymmetricKey, Is.Null);
            Assert.That(source, Is.All.EqualTo(11));
            Assert.That(imported.Key.ContainsPrivateMaterial(), Is.False);
        });
    }

    [Test]
    public void NullArgumentsAreProgrammerErrors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => SymmetricKeyImporterOperations.Import(null), Throws.ArgumentNullException);
            Assert.That(() => SymmetricKeyImporterOperations.ImportJose(null, JoseAlgorithms.HmacSha256), Throws.ArgumentNullException);
            Assert.That(() => SymmetricKeyImporterOperations.ImportJose(Bytes(32, 1), null), Throws.ArgumentNullException);
            Assert.That(() => SymmetricKeyImporterOperations.ImportCose(null, CoseAlgorithms.HmacSha256), Throws.ArgumentNullException);
            Assert.That(() => SymmetricKeyImporterOperations.Validate(null), Throws.ArgumentNullException);
            Assert.That(() => SymmetricKeyImporterOperations.Clear(null), Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
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
