using System;

namespace odf.Tests;

public class PackageEncryptionTests
{
    [Test]
    public void CompressesAndChecksumsBeforeAesEncryption()
    {
        byte[] plaintext = new byte[4096];
        Array.Fill(plaintext, (byte)65);
        byte[] keyBytes = CreateSequence(16, 3);
        using SecurityBuffer key = new SecurityBuffer(keyBytes);

        PackageEncryptionResult encrypted = PackageEncryption.EncryptAesEntry(
            plaintext,
            key,
            OdfEncryptionMethod.Aes128Cbc,
            OdfAlgorithmIdentifiers.Sha256OneKilobyte,
            OdfVersion.Version14);
        using SecurityBuffer compressed = AesCbcCipher.DecryptPadded(
            encrypted.EncryptedBytes,
            key,
            encrypted.InitializationVector,
            OdfEncryptionMethod.Aes128Cbc);
        byte[] decrypted = PackageEncryption.DecryptAesEntry(
            encrypted.EncryptedBytes,
            key,
            encrypted.InitializationVector,
            OdfEncryptionMethod.Aes128Cbc,
            OdfAlgorithmIdentifiers.Sha256OneKilobyte,
            encrypted.Checksum,
            plaintext.Length,
            8192,
            OdfVersion.Version14);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.InitializationVector.Length, Is.EqualTo(16));
            Assert.That(encrypted.Checksum.Length, Is.EqualTo(32));
            Assert.That(encrypted.UncompressedSize, Is.EqualTo(plaintext.Length));
            Assert.That(encrypted.CompressedSize, Is.LessThan(plaintext.Length));
            Assert.That(compressed.GetLength(), Is.EqualTo(encrypted.CompressedSize));
            Assert.That(compressed.GetOperationBytes(), Is.Not.EqualTo(plaintext));
            Assert.That(
                PackageChecksum.VerifyChecksum(
                    compressed.GetOperationBytes(),
                    OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                    encrypted.Checksum),
                Is.True);
            Assert.That(encrypted.EncryptedBytes.Length % 16, Is.Zero);
            Assert.That(decrypted, Is.EqualTo(plaintext));
        });
        SecurityBuffer.ClearBytes(keyBytes);
        SecurityBuffer.ClearBytes(decrypted);
    }

    [TestCase(OdfEncryptionMethod.Aes128Cbc, 16, OdfVersion.Version12)]
    [TestCase(OdfEncryptionMethod.Aes192Cbc, 24, OdfVersion.Version13)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, 32, OdfVersion.Version14)]
    public void RoundTripsSupportedOdfAesVariants(
        OdfEncryptionMethod encryptionMethod,
        int keySize,
        OdfVersion version)
    {
        byte[] plaintext = CreateSequence(333, 29);
        byte[] keyBytes = CreateSequence(keySize, 71);
        using SecurityBuffer key = new SecurityBuffer(keyBytes);

        PackageEncryptionResult encrypted = PackageEncryption.EncryptAesEntry(
            plaintext,
            key,
            encryptionMethod,
            OdfAlgorithmIdentifiers.Sha1OneKilobyte,
            version);
        byte[] decrypted = PackageEncryption.DecryptAesEntry(
            encrypted.EncryptedBytes,
            key,
            encrypted.InitializationVector,
            encryptionMethod,
            OdfAlgorithmIdentifiers.Sha1OneKilobyte,
            encrypted.Checksum,
            plaintext.Length,
            1000,
            version);

        Assert.That(decrypted, Is.EqualTo(plaintext));
        SecurityBuffer.ClearBytes(keyBytes);
        SecurityBuffer.ClearBytes(decrypted);
    }

    [Test]
    public void RejectsWrongChecksumSizeAndExpansionLimit()
    {
        byte[] plaintext = new byte[2048];
        Array.Fill(plaintext, (byte)90);
        using SecurityBuffer key = new SecurityBuffer(new byte[16]);
        PackageEncryptionResult encrypted = PackageEncryption.EncryptAesEntry(
            plaintext,
            key,
            OdfEncryptionMethod.Aes128Cbc,
            OdfAlgorithmIdentifiers.Sha256OneKilobyte,
            OdfVersion.Version14);
        byte[] wrongChecksum = (byte[])encrypted.Checksum.Clone();
        wrongChecksum[0] ^= 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                PackageEncryption.DecryptAesEntry(
                    encrypted.EncryptedBytes,
                    key,
                    encrypted.InitializationVector,
                    OdfEncryptionMethod.Aes128Cbc,
                    OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                    wrongChecksum,
                    plaintext.Length,
                    4096,
                    OdfVersion.Version14));
            Assert.Throws<OdfSecurityException>(() =>
                PackageEncryption.DecryptAesEntry(
                    encrypted.EncryptedBytes,
                    key,
                    encrypted.InitializationVector,
                    OdfEncryptionMethod.Aes128Cbc,
                    OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                    encrypted.Checksum,
                    plaintext.Length,
                    100,
                    OdfVersion.Version14));
            Assert.Throws<OdfSecurityException>(() =>
                PackageEncryption.DecryptAesEntry(
                    encrypted.EncryptedBytes,
                    key,
                    encrypted.InitializationVector,
                    OdfEncryptionMethod.Aes128Cbc,
                    OdfAlgorithmIdentifiers.Sha256OneKilobyte,
                    encrypted.Checksum,
                    plaintext.Length - 1,
                    4096,
                    OdfVersion.Version14));
        });
    }

    [Test]
    public void RejectsAesForLegacyAndUnknownOdfVersions()
    {
        using SecurityBuffer key = new SecurityBuffer(new byte[16]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                PackageEncryption.EncryptAesEntry(
                    [1],
                    key,
                    OdfEncryptionMethod.Aes128Cbc,
                    OdfAlgorithmIdentifiers.Sha1OneKilobyte,
                    OdfVersion.Version11));
            Assert.Throws<OdfSecurityException>(() =>
                PackageEncryption.EncryptAesEntry(
                    [1],
                    key,
                    OdfEncryptionMethod.Aes128Cbc,
                    OdfAlgorithmIdentifiers.Sha1OneKilobyte,
                    OdfVersion.Future));
        });
    }

    [Test]
    public void GeneratesASeparateInitializationVectorForEveryEntry()
    {
        using SecurityBuffer key = new SecurityBuffer(new byte[16]);

        PackageEncryptionResult first = PackageEncryption.EncryptAesEntry(
            [1, 2, 3],
            key,
            OdfEncryptionMethod.Aes128Cbc,
            OdfAlgorithmIdentifiers.Sha1OneKilobyte,
            OdfVersion.Version14);
        PackageEncryptionResult second = PackageEncryption.EncryptAesEntry(
            [1, 2, 3],
            key,
            OdfEncryptionMethod.Aes128Cbc,
            OdfAlgorithmIdentifiers.Sha1OneKilobyte,
            OdfVersion.Version14);

        Assert.That(second.InitializationVector, Is.Not.EqualTo(first.InitializationVector));
    }

    static byte[] CreateSequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)((start + index) & 255);
            index++;
        }
        return result;
    }
}
