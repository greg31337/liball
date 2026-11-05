using System.Security.Cryptography;
using token;

namespace token.Tests;

public class RsaPkcs1KeyManagementTests
{
    RSA rsa;

    [OneTimeSetUp]
    public void CreateKey()
    {
        rsa = RSA.Create(2048);
    }

    [OneTimeTearDown]
    public void DisposeKey()
    {
        rsa.Dispose();
    }

    [Test]
    public void Rsa15IsRejectedByEveryConservativeEntryPoint()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        byte[] contentEncryptionKey = Bytes(32, 1);
        byte[] encryptedKey = rsa.Encrypt(
            contentEncryptionKey,
            RSAEncryptionPadding.Pkcs1);

        Assert.Multiple(() =>
        {
            AssertFailure(
                RsaKeyManagement.Encrypt(
                    key,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm),
                "compatibility_required");
            AssertFailure(
                RsaKeyManagement.Wrap(
                    contentEncryptionKey,
                    key,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm),
                "compatibility_required");
            AssertFailure(
                RsaKeyManagement.Decrypt(
                    encryptedKey,
                    key,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm),
                "compatibility_required");
            AssertFailure(
                RsaKeyManagement.Decrypt(
                    encryptedKey.AsSpan(),
                    key,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm),
                "compatibility_required");
        });
    }

    [Test]
    public void OptedInWrapAndDecryptInteroperateWithBuiltInRsa()
    {
        byte[] contentEncryptionKey = Bytes(32, 0x5a);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;

        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        byte[] platformDecrypted = rsa.Decrypt(
            wrapped.EncryptedKey,
            RSAEncryptionPadding.Pkcs1);
        byte[] platformEncrypted = rsa.Encrypt(
            contentEncryptionKey,
            RSAEncryptionPadding.Pkcs1);
        RsaKeyManagementResult decrypted = RsaKeyManagement.Decrypt(
            platformEncrypted,
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);

        Assert.Multiple(() =>
        {
            AssertSuccess(wrapped, contentEncryptionKey);
            Assert.That(platformDecrypted, Is.EqualTo(contentEncryptionKey));
            AssertSuccess(decrypted, contentEncryptionKey);
            Assert.That(decrypted.EncryptedKey, Is.EqualTo(platformEncrypted));
            Assert.That(decrypted.EncryptedKey, Is.Not.SameAs(platformEncrypted));
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    [TestCase(JoseAlgorithms.Aes192CbcHmacSha384, 48)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void OptedInEncryptionGeneratesAndRecoversCorrectCekSize(
        string contentEncryptionAlgorithm,
        int keySize)
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;

        RsaKeyManagementResult encrypted = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaPkcs1,
            contentEncryptionAlgorithm,
            true);
        RsaKeyManagementResult decrypted = RsaKeyManagement.Decrypt(
            encrypted.EncryptedKey,
            key,
            JoseAlgorithms.RsaPkcs1,
            contentEncryptionAlgorithm,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.ContentEncryptionKey, Has.Length.EqualTo(keySize));
            Assert.That(encrypted.EncryptedKey, Has.Length.EqualTo(rsa.KeySize / 8));
            Assert.That(decrypted.ContentEncryptionKey,
                Is.EqualTo(encrypted.ContentEncryptionKey));
        });
    }

    [Test]
    public void OptInDoesNotPermitRsa15KeyMetadataToLeakIntoConservativeCalls()
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;

        RsaKeyManagementResult conservative = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm);
        RsaKeyManagementResult compatible = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
        });
    }

    [Test]
    public void PublicKeyCanWrapButPrivateCapabilityIsRequiredToDecrypt()
    {
        byte[] contentEncryptionKey = Bytes(32, 4);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaPkcs1,
            false,
            true).Key;
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;

        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            publicKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        RsaKeyManagementResult denied = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            publicKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        RsaKeyManagementResult allowed = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            privateKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            AssertFailure(denied, "missing_private_key");
            Assert.That(allowed.ContentEncryptionKey,
                Is.EqualTo(contentEncryptionKey));
        });
    }

    [Test]
    public void InvalidRsa15CiphertextsAndRecoveredSizesUseUniformFailure()
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;
        byte[] valid = rsa.Encrypt(Bytes(32, 8), RSAEncryptionPadding.Pkcs1);
        byte[] altered = (byte[])valid.Clone();
        altered[0] ^= 1;
        byte[] wrongSize = rsa.Encrypt(Bytes(24, 8), RSAEncryptionPadding.Pkcs1);
        byte[] wrongPadding = rsa.Encrypt(
            Bytes(32, 8),
            RSAEncryptionPadding.OaepSHA256);

        RsaKeyManagementResult changed = RsaKeyManagement.Decrypt(
            altered,
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        RsaKeyManagementResult truncated = RsaKeyManagement.Decrypt(
            valid[..^1],
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        RsaKeyManagementResult recoveredSize = RsaKeyManagement.Decrypt(
            wrongSize,
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);
        RsaKeyManagementResult padding = RsaKeyManagement.Decrypt(
            wrongPadding,
            key,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes256Gcm,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(changed, "decryption_failed");
            AssertFailure(truncated, "decryption_failed");
            AssertFailure(recoveredSize, "decryption_failed");
            AssertFailure(padding, "decryption_failed");
        });
    }

    [Test]
    public void CompatibilityOptInStillEnforcesMetadataAndCekSize()
    {
        TokenKey unwrapOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;
        unwrapOnly.KeyOperations = new string[] { TokenKeyOperations.UnwrapKey };
        TokenKey signatureUse = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPkcs1,
            true,
            true).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        Assert.Multiple(() =>
        {
            AssertFailure(
                RsaKeyManagement.Wrap(
                    Bytes(32, 2),
                    unwrapOnly,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm,
                    true),
                "key_operation_not_permitted");
            AssertFailure(
                RsaKeyManagement.Encrypt(
                    signatureUse,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm,
                    true),
                "key_use_algorithm_mismatch");
            AssertFailure(
                RsaKeyManagement.Wrap(
                    Bytes(31, 2),
                    RsaKeyImporterOperations.Import(rsa, true).Key,
                    JoseAlgorithms.RsaPkcs1,
                    JoseAlgorithms.Aes256Gcm,
                    true),
                "invalid_key_size");
        });
    }

    [Test]
    public void CompatibilityOptInDoesNotEnableOtherUnsupportedAlgorithms()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;

        AssertFailure(
            RsaKeyManagement.Encrypt(
                key,
                JoseAlgorithms.RsaSha256,
                JoseAlgorithms.Aes256Gcm,
                true),
            "invalid_algorithm");
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static void AssertSuccess(
        RsaKeyManagementResult result,
        byte[] expectedContentEncryptionKey)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentEncryptionKey,
                Is.EqualTo(expectedContentEncryptionKey));
            Assert.That(result.EncryptedKey, Has.Length.EqualTo(256));
        });
    }

    static void AssertFailure(RsaKeyManagementResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ContentEncryptionKey, Is.Null);
            Assert.That(result.EncryptedKey, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
