using System;

namespace odf.Tests;

public class AesCbcCipherTests
{
    [Test]
    public void MatchesNistAes128CbcFirstBlockVector()
    {
        byte[] keyBytes = Convert.FromHexString(
            "2B7E151628AED2A6ABF7158809CF4F3C");
        byte[] initializationVector = Convert.FromHexString(
            "000102030405060708090A0B0C0D0E0F");
        byte[] plaintextBytes = Convert.FromHexString(
            "6BC1BEE22E409F96E93D7E117393172A");
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using SecurityBuffer plaintext = new SecurityBuffer(plaintextBytes);

        byte[] encrypted = AesCbcCipher.EncryptPadded(
            plaintext,
            key,
            initializationVector,
            OdfEncryptionMethod.Aes128Cbc);
        using SecurityBuffer decrypted = AesCbcCipher.DecryptPadded(
            encrypted,
            key,
            initializationVector,
            OdfEncryptionMethod.Aes128Cbc);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Length, Is.EqualTo(32));
            Assert.That(
                Convert.ToHexString(encrypted, 0, 16),
                Is.EqualTo("7649ABAC8119B246CEE98E9B12E9197D"));
            Assert.That(decrypted.GetOperationBytes(), Is.EqualTo(plaintextBytes));
        });
        SecurityBuffer.ClearBytes(keyBytes);
        SecurityBuffer.ClearBytes(plaintextBytes);
    }

    [TestCase(OdfEncryptionMethod.Aes128Cbc, 16, 0)]
    [TestCase(OdfEncryptionMethod.Aes128Cbc, 16, 15)]
    [TestCase(OdfEncryptionMethod.Aes192Cbc, 24, 16)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, 32, 31)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, 32, 33)]
    public void AppliesW3cPaddingAndRoundTripsEveryAesKeySize(
        OdfEncryptionMethod encryptionMethod,
        int keySize,
        int plaintextSize)
    {
        byte[] keyBytes = CreateSequence(keySize, 17);
        byte[] initializationVector = CreateSequence(16, 49);
        byte[] originalVector = (byte[])initializationVector.Clone();
        byte[] plaintextBytes = CreateSequence(plaintextSize, 81);
        byte[] originalPlaintext = (byte[])plaintextBytes.Clone();
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using SecurityBuffer plaintext = new SecurityBuffer(plaintextBytes);

        byte[] encrypted = AesCbcCipher.EncryptPadded(
            plaintext,
            key,
            initializationVector,
            encryptionMethod);
        using SecurityBuffer decrypted = AesCbcCipher.DecryptPadded(
            encrypted,
            key,
            initializationVector,
            encryptionMethod);

        int expectedLength = (plaintextSize / 16 + 1) * 16;
        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Length, Is.EqualTo(expectedLength));
            Assert.That(decrypted.GetOperationBytes(), Is.EqualTo(originalPlaintext));
            Assert.That(plaintextBytes, Is.EqualTo(originalPlaintext));
            Assert.That(initializationVector, Is.EqualTo(originalVector));
        });
        SecurityBuffer.ClearBytes(keyBytes);
        SecurityBuffer.ClearBytes(plaintextBytes);
    }

    [Test]
    public void RejectsInvalidW3cPadding()
    {
        byte[] initializationVector = new byte[16];
        using SecurityBuffer key = new SecurityBuffer(new byte[16]);
        using SecurityBuffer plaintext = new SecurityBuffer([]);
        byte[] encrypted = AesCbcCipher.EncryptPadded(
            plaintext,
            key,
            initializationVector,
            OdfEncryptionMethod.Aes128Cbc);
        byte[] changedVector = (byte[])initializationVector.Clone();
        changedVector[15] = 1;

        Assert.Throws<OdfSecurityException>(() =>
            AesCbcCipher.DecryptPadded(
                encrypted,
                key,
                changedVector,
                OdfEncryptionMethod.Aes128Cbc));
    }

    [Test]
    public void RejectsInvalidAesParametersAndCiphertextLengths()
    {
        using SecurityBuffer key16 = new SecurityBuffer(new byte[16]);
        using SecurityBuffer key24 = new SecurityBuffer(new byte[24]);
        using SecurityBuffer plaintext = new SecurityBuffer([1]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() =>
                AesCbcCipher.EncryptPadded(
                    plaintext,
                    key16,
                    new byte[15],
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                AesCbcCipher.EncryptPadded(
                    plaintext,
                    key24,
                    new byte[16],
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                AesCbcCipher.EncryptPadded(
                    plaintext,
                    key16,
                    new byte[16],
                    OdfEncryptionMethod.BlowfishCfb));
            Assert.Throws<OdfSecurityException>(() =>
                AesCbcCipher.DecryptPadded(
                    [],
                    key16,
                    new byte[16],
                    OdfEncryptionMethod.Aes128Cbc));
            Assert.Throws<OdfSecurityException>(() =>
                AesCbcCipher.DecryptPadded(
                    new byte[17],
                    key16,
                    new byte[16],
                    OdfEncryptionMethod.Aes128Cbc));
        });
    }

    [Test]
    public void GeneratesIndependentSixteenByteInitializationVectors()
    {
        byte[] first = AesCbcCipher.CreateInitializationVector();
        byte[] second = AesCbcCipher.CreateInitializationVector();

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.EqualTo(16));
            Assert.That(second.Length, Is.EqualTo(16));
            Assert.That(second, Is.Not.EqualTo(first));
        });
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
