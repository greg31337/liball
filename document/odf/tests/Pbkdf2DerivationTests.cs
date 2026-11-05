using System;
using System.Text;

namespace odf.Tests;

public class Pbkdf2DerivationTests
{
    [TestCase(
        "password",
        "salt",
        1,
        20,
        "0C60C80F961F0E71F3A9B524AF6012062FE037A6")]
    [TestCase(
        "password",
        "salt",
        2,
        20,
        "EA6C014DC72D6F8CCD1ED92ACE1D41F0D8DE8957")]
    [TestCase(
        "password",
        "salt",
        4096,
        20,
        "4B007901B765489ABEAD49D926F721D065A429C1")]
    [TestCase(
        "passwordPASSWORDpassword",
        "saltSALTsaltSALTsaltSALTsaltSALTsalt",
        4096,
        25,
        "3D2EEC4FE41C849B80C8D83662C0E44A8B291A964CF2F07038")]
    public void DerivesPublishedRfc6070HmacSha1Vectors(
        string password,
        string salt,
        int iterationCount,
        int keySize,
        string expectedHex)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        using SecurityBuffer derived = Pbkdf2Derivation.DeriveHmacSha1(
            passwordBytes,
            saltBytes,
            iterationCount,
            keySize);

        Assert.That(Convert.ToHexString(derived.GetOperationBytes()), Is.EqualTo(expectedHex));
        SecurityBuffer.ClearBytes(passwordBytes);
        SecurityBuffer.ClearBytes(saltBytes);
    }

    [Test]
    public void DerivesRfc6070VectorContainingZeroBytes()
    {
        byte[] password = [112, 97, 115, 115, 0, 119, 111, 114, 100];
        byte[] salt = [115, 97, 0, 108, 116];
        using SecurityBuffer derived = Pbkdf2Derivation.DeriveHmacSha1(
            password,
            salt,
            4096,
            16);

        Assert.That(
            Convert.ToHexString(derived.GetOperationBytes()),
            Is.EqualTo("56FA6AA75548099DCC37D7F03425E0C3"));
        SecurityBuffer.ClearBytes(password);
        SecurityBuffer.ClearBytes(salt);
    }

    [Test]
    public void GeneratesVersionSpecificShaStartKeys()
    {
        OdfStartKeyGeneration legacySettings = new OdfStartKeyGeneration();
        OdfStartKeyGeneration modernSettings = new OdfStartKeyGeneration();
        modernSettings.Name = OdfAlgorithmIdentifiers.Sha256Odf;
        modernSettings.HasKeySize = true;
        modernSettings.KeySizeIsValid = true;
        modernSettings.KeySize = 32;

        using SecurityBuffer legacy = Pbkdf2Derivation.CreateStartKey(
            "password",
            OdfVersion.Version11,
            legacySettings);
        using SecurityBuffer modern = Pbkdf2Derivation.CreateStartKey(
            "password",
            OdfVersion.Version14,
            modernSettings);

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(legacy.GetOperationBytes()),
                Is.EqualTo("5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8"));
            Assert.That(
                Convert.ToHexString(modern.GetOperationBytes()),
                Is.EqualTo("5E884898DA28047151D0E56F8DC6292773603D0D6AABB" +
                    "DD62A11EF721D1542D8"));
        });
    }

    [Test]
    public void UsesTwentyByteDefaultForSha256StartKey()
    {
        OdfStartKeyGeneration settings = new OdfStartKeyGeneration();
        settings.Name = OdfAlgorithmIdentifiers.Sha256Odf;

        using SecurityBuffer startKey = Pbkdf2Derivation.CreateStartKey(
            "password",
            OdfVersion.Version12,
            settings);

        Assert.That(
            Convert.ToHexString(startKey.GetOperationBytes()),
            Is.EqualTo("5E884898DA28047151D0E56F8DC6292773603D0D"));
    }

    [Test]
    public void DerivesOdfEntryKeyFromHashedPasswordAndDeclaredParameters()
    {
        OdfKeyDerivation settings = CreatePbkdf2Settings(
            Encoding.ASCII.GetBytes("salt-value-12345"),
            1024,
            16);

        using SecurityBuffer startKey = Pbkdf2Derivation.CreateStartKey(
            "password",
            OdfVersion.Version12,
            new OdfStartKeyGeneration());
        using SecurityBuffer entryKey = Pbkdf2Derivation.DeriveEntryKey(
            startKey,
            OdfVersion.Version12,
            settings,
            OdfEncryptionMethod.BlowfishCfb);

        Assert.That(
            Convert.ToHexString(entryKey.GetOperationBytes()),
            Is.EqualTo("1BB2F34D50397D2EEE907ACAAC14F18B"));
        SecurityBuffer.ClearBytes(settings.Salt);
    }

    [Test]
    public void AppliesOdfDefaultIterationCountAndEntryKeySize()
    {
        OdfKeyDerivation settings = new OdfKeyDerivation();
        settings.Name = OdfAlgorithmIdentifiers.Pbkdf2;
        settings.Salt = Encoding.ASCII.GetBytes("salt-value-12345");
        settings.SaltIsValid = true;

        using SecurityBuffer startKey = Pbkdf2Derivation.CreateStartKey(
            "password",
            OdfVersion.Version13,
            new OdfStartKeyGeneration());
        using SecurityBuffer entryKey = Pbkdf2Derivation.DeriveEntryKey(
            startKey,
            OdfVersion.Version13,
            settings,
            OdfEncryptionMethod.Aes128Cbc);

        Assert.Multiple(() =>
        {
            Assert.That(entryKey.GetLength(), Is.EqualTo(16));
            Assert.That(
                Convert.ToHexString(entryKey.GetOperationBytes()),
                Is.EqualTo("1BB2F34D50397D2EEE907ACAAC14F18B"));
        });
        SecurityBuffer.ClearBytes(settings.Salt);
    }

    [Test]
    public void RejectsStartKeyAlgorithmsAndSizesOutsideTheirOdfVersion()
    {
        OdfStartKeyGeneration sha256 = new OdfStartKeyGeneration();
        sha256.Name = OdfAlgorithmIdentifiers.Sha256Odf;
        OdfStartKeyGeneration oversized = new OdfStartKeyGeneration();
        oversized.Name = OdfAlgorithmIdentifiers.Sha1Name;
        oversized.HasKeySize = true;
        oversized.KeySizeIsValid = true;
        oversized.KeySize = 21;

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.CreateStartKey("password", OdfVersion.Version11, sha256));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.CreateStartKey("password", OdfVersion.Version14, oversized));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.CreateStartKey(
                    "password",
                    OdfVersion.Future,
                    new OdfStartKeyGeneration()));
        });
    }

    [Test]
    public void EnforcesVersionAndEncryptionMethodKeySizes()
    {
        OdfKeyDerivation aes128 = CreatePbkdf2Settings([1, 2, 3], 1, 16);
        OdfKeyDerivation aes256WrongSize = CreatePbkdf2Settings([1, 2, 3], 1, 16);
        OdfKeyDerivation aes256 = CreatePbkdf2Settings([1, 2, 3], 1, 32);
        using SecurityBuffer startKey = new SecurityBuffer([1, 2, 3, 4]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version11,
                    aes128,
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    aes256WrongSize,
                    OdfEncryptionMethod.Aes256Cbc));
        });

        using SecurityBuffer result = Pbkdf2Derivation.DeriveEntryKey(
            startKey,
            OdfVersion.Version14,
            aes256,
            OdfEncryptionMethod.Aes256Cbc);
        Assert.That(result.GetLength(), Is.EqualTo(32));
        SecurityBuffer.ClearBytes(aes128.Salt);
        SecurityBuffer.ClearBytes(aes256WrongSize.Salt);
        SecurityBuffer.ClearBytes(aes256.Salt);
    }

    [Test]
    public void RejectsDerivedBlowfishKeysOutsideTheCipherLimits()
    {
        OdfKeyDerivation tooShort = CreatePbkdf2Settings([1], 1, 3);
        OdfKeyDerivation tooLong = CreatePbkdf2Settings([1], 1, 57);
        using SecurityBuffer startKey = new SecurityBuffer([1, 2, 3, 4]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    tooShort,
                    OdfEncryptionMethod.BlowfishCfb));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    tooLong,
                    OdfEncryptionMethod.BlowfishCfb));
        });
        SecurityBuffer.ClearBytes(tooShort.Salt);
        SecurityBuffer.ClearBytes(tooLong.Salt);
    }

    [Test]
    public void RejectsInvalidPbkdf2Metadata()
    {
        OdfKeyDerivation invalidSalt = CreatePbkdf2Settings([], 1, 16);
        OdfKeyDerivation invalidIterations = CreatePbkdf2Settings([1], 0, 16);
        OdfKeyDerivation pgp = CreatePbkdf2Settings([1], 1, 16);
        pgp.IsPgp = true;
        using SecurityBuffer startKey = new SecurityBuffer([1]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    invalidSalt,
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    invalidIterations,
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                Pbkdf2Derivation.DeriveEntryKey(
                    startKey,
                    OdfVersion.Version14,
                    pgp,
                    OdfEncryptionMethod.Aes128Cbc));
        });
        SecurityBuffer.ClearBytes(invalidIterations.Salt);
        SecurityBuffer.ClearBytes(pgp.Salt);
    }

    static OdfKeyDerivation CreatePbkdf2Settings(
        byte[] salt,
        int iterationCount,
        int keySize)
    {
        OdfKeyDerivation result = new OdfKeyDerivation();
        result.Name = OdfAlgorithmIdentifiers.Pbkdf2Name;
        result.Salt = salt;
        result.SaltIsValid = true;
        result.HasIterationCount = true;
        result.IterationCountIsValid = true;
        result.IterationCount = iterationCount;
        result.HasKeySize = true;
        result.KeySizeIsValid = true;
        result.KeySize = keySize;
        return result;
    }
}
