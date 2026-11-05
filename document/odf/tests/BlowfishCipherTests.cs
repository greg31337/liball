using System;

namespace odf.Tests;

public class BlowfishCipherTests
{
    [TestCase(
        "0000000000000000",
        "0000000000000000",
        "4EF997456198DD78")]
    [TestCase(
        "FFFFFFFFFFFFFFFF",
        "FFFFFFFFFFFFFFFF",
        "51866FD5B85ECB8A")]
    [TestCase(
        "3000000000000000",
        "1000000000000001",
        "7D856F9A613063F2")]
    [TestCase(
        "1111111111111111",
        "1111111111111111",
        "2466DD878B963C9D")]
    public void MatchesPublishedSchneierBlockVectors(
        string keyHex,
        string plaintextHex,
        string ciphertextHex)
    {
        byte[] keyBytes = Convert.FromHexString(keyHex);
        byte[] plaintext = Convert.FromHexString(plaintextHex);
        byte[] expected = Convert.FromHexString(ciphertextHex);
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using BlowfishCipher cipher = new BlowfishCipher(key);

        byte[] encrypted = cipher.EncryptBlock(plaintext);
        byte[] decrypted = cipher.DecryptBlock(expected);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted, Is.EqualTo(expected));
            Assert.That(decrypted, Is.EqualTo(plaintext));
        });
        SecurityBuffer.ClearBytes(keyBytes);
    }

    [Test]
    public void MatchesPublishedBouncyCastleCfb8Vector()
    {
        byte[] keyBytes = Convert.FromHexString(
            "AAFD12F659CAE63489B479E5076DDEC2");
        byte[] initializationVector = Convert.FromHexString(
            "AAFD12F659CAE634");
        byte[] plaintext = Convert.FromHexString(
            "000102030405060708090A0B0C0D0E0F" +
            "FF0102030405060708090A0B0C0D0E0F");
        byte[] expected = Convert.FromHexString(
            "6CA63AAADA9188D2410C07513CC0736B" +
            "9888770768C25A5BEFC776BEEA5BDC4C");
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using BlowfishCipher cipher = new BlowfishCipher(key);

        byte[] encrypted = cipher.EncryptCfb8(plaintext, initializationVector);
        byte[] decrypted = cipher.DecryptCfb8(expected, initializationVector);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted, Is.EqualTo(expected));
            Assert.That(decrypted, Is.EqualTo(plaintext));
        });
        SecurityBuffer.ClearBytes(keyBytes);
    }

    [Test]
    public void EncryptsPartialBlocksWithoutPaddingOrMutatingInputs()
    {
        byte[] keyBytes = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF");
        byte[] initializationVector = Convert.FromHexString(
            "0123456789ABCDEF");
        byte[] originalVector = (byte[])initializationVector.Clone();
        byte[] plaintext = Convert.FromHexString("000102030405060708090A0B0C0D0E0F10");
        byte[] originalPlaintext = (byte[])plaintext.Clone();
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using BlowfishCipher cipher = new BlowfishCipher(key);

        byte[] encrypted = cipher.EncryptCfb8(plaintext, initializationVector);
        byte[] decrypted = cipher.DecryptCfb8(encrypted, initializationVector);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Length, Is.EqualTo(plaintext.Length));
            Assert.That(decrypted, Is.EqualTo(originalPlaintext));
            Assert.That(plaintext, Is.EqualTo(originalPlaintext));
            Assert.That(initializationVector, Is.EqualTo(originalVector));
        });
        SecurityBuffer.ClearBytes(keyBytes);
    }

    [Test]
    public void SupportsEmptyEntryContent()
    {
        byte[] keyBytes = [1, 2, 3, 4];
        using SecurityBuffer key = new SecurityBuffer(keyBytes);
        using BlowfishCipher cipher = new BlowfishCipher(key);

        Assert.Multiple(() =>
        {
            Assert.That(cipher.EncryptCfb8([], new byte[8]), Is.Empty);
            Assert.That(cipher.DecryptCfb8([], new byte[8]), Is.Empty);
        });
        SecurityBuffer.ClearBytes(keyBytes);
    }

    [Test]
    public void RejectsInvalidKeyAndInitializationVectorLengths()
    {
        using SecurityBuffer shortKey = new SecurityBuffer(new byte[3]);
        using SecurityBuffer longKey = new SecurityBuffer(new byte[57]);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() => new BlowfishCipher(shortKey));
            Assert.Throws<OdfSecurityException>(() => new BlowfishCipher(longKey));
        });

        using SecurityBuffer validKey = new SecurityBuffer(new byte[16]);
        using BlowfishCipher cipher = new BlowfishCipher(validKey);
        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() => cipher.EncryptCfb8([1], new byte[7]));
            Assert.Throws<OdfSecurityException>(() => cipher.DecryptCfb8([1], new byte[9]));
        });
    }

    [Test]
    public void ClearingCipherRejectsFurtherOperations()
    {
        using SecurityBuffer key = new SecurityBuffer(new byte[16]);
        BlowfishCipher cipher = new BlowfishCipher(key);

        cipher.Dispose();
        cipher.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<ObjectDisposedException>(() => cipher.EncryptBlock(new byte[8]));
            Assert.Throws<ObjectDisposedException>(() => cipher.DecryptBlock(new byte[8]));
            Assert.Throws<ObjectDisposedException>(() => cipher.EncryptCfb8([], new byte[8]));
            Assert.Throws<ObjectDisposedException>(() => cipher.DecryptCfb8([], new byte[8]));
        });
    }
}
