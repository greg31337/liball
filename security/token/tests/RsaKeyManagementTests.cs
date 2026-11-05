using System.Security.Cryptography;
using token;

namespace token.Tests;

public class RsaKeyManagementTests
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

    [TestCase(JoseAlgorithms.RsaOaep)]
    [TestCase(JoseAlgorithms.RsaOaepSha256)]
    [TestCase(JoseAlgorithms.RsaOaepSha384)]
    [TestCase(JoseAlgorithms.RsaOaepSha512)]
    public void WrapAndDecryptInteroperateWithBuiltInRsa(string algorithm)
    {
        byte[] contentEncryptionKey = Bytes(32, 0x5a);
        TokenKey key = RsaKeyImporterOperations.ImportJose(rsa, algorithm, true).Key;

        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            key,
            algorithm,
            JoseAlgorithms.Aes256Gcm);
        byte[] platformDecrypted = rsa.Decrypt(
            wrapped.EncryptedKey,
            Padding(algorithm));
        byte[] platformEncrypted = rsa.Encrypt(
            contentEncryptionKey,
            Padding(algorithm));
        RsaKeyManagementResult decrypted = RsaKeyManagement.Decrypt(
            platformEncrypted,
            key,
            algorithm,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            AssertSuccess(wrapped, contentEncryptionKey, rsa.KeySize / 8);
            Assert.That(platformDecrypted, Is.EqualTo(contentEncryptionKey));
            AssertSuccess(decrypted, contentEncryptionKey, rsa.KeySize / 8);
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
    public void EncryptGeneratesCorrectRandomContentEncryptionKey(
        string contentEncryptionAlgorithm,
        int keySize)
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;

        RsaKeyManagementResult first = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaOaepSha256,
            contentEncryptionAlgorithm);
        RsaKeyManagementResult second = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaOaepSha256,
            contentEncryptionAlgorithm);
        RsaKeyManagementResult decrypted = RsaKeyManagement.Decrypt(
            first.EncryptedKey,
            key,
            JoseAlgorithms.RsaOaepSha256,
            contentEncryptionAlgorithm);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.ContentEncryptionKey, Has.Length.EqualTo(keySize));
            Assert.That(first.ContentEncryptionKey, Is.Not.EqualTo(second.ContentEncryptionKey));
            Assert.That(first.EncryptedKey, Has.Length.EqualTo(rsa.KeySize / 8));
            Assert.That(decrypted.ContentEncryptionKey,
                Is.EqualTo(first.ContentEncryptionKey));
        });
    }

    [Test]
    public void WrapCopiesCallerContentEncryptionKey()
    {
        byte[] contentEncryptionKey = Bytes(32, 7);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;

        RsaKeyManagementResult result = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        result.ContentEncryptionKey[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.ContentEncryptionKey, Is.Not.SameAs(contentEncryptionKey));
            Assert.That(contentEncryptionKey[0], Is.EqualTo(7));
            Assert.That(rsa.Decrypt(
                result.EncryptedKey,
                RSAEncryptionPadding.OaepSHA256)[0], Is.EqualTo(7));
        });
    }

    [Test]
    public void PublicKeyEncryptsButPrivateCapabilityIsRequiredToDecrypt()
    {
        byte[] contentEncryptionKey = Bytes(32, 4);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaOaepSha256,
            false).Key;
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;

        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            publicKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult publicDecrypt = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            publicKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult privateDecrypt = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            privateKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            AssertFailure(publicDecrypt, "missing_private_key");
            Assert.That(privateDecrypt.Success, Is.True);
            Assert.That(privateDecrypt.ContentEncryptionKey,
                Is.EqualTo(contentEncryptionKey));
        });
    }

    [Test]
    public void SpanDecryptUsesOnlySelectedEncryptedKeySlice()
    {
        byte[] contentEncryptionKey = Bytes(32, 6);
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        byte[] container = new byte[wrapped.EncryptedKey.Length + 2];
        wrapped.EncryptedKey.CopyTo(container, 1);

        RsaKeyManagementResult result = RsaKeyManagement.Decrypt(
            container.AsSpan(1, wrapped.EncryptedKey.Length),
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        Assert.That(result.ContentEncryptionKey, Is.EqualTo(contentEncryptionKey));
    }

    [Test]
    public void InvalidCiphertextsAndRecoveredSizesUseUniformFailure()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        byte[] contentEncryptionKey = Bytes(32, 8);
        byte[] encrypted = rsa.Encrypt(
            contentEncryptionKey,
            RSAEncryptionPadding.OaepSHA256);
        byte[] altered = (byte[])encrypted.Clone();
        altered[0] ^= 1;
        byte[] wrongSizeKey = rsa.Encrypt(
            Bytes(24, 8),
            RSAEncryptionPadding.OaepSHA256);

        RsaKeyManagementResult changed = RsaKeyManagement.Decrypt(
            altered,
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult truncated = RsaKeyManagement.Decrypt(
            encrypted[..^1],
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult wrongPadding = RsaKeyManagement.Decrypt(
            encrypted,
            key,
            JoseAlgorithms.RsaOaepSha384,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult wrongRecoveredSize = RsaKeyManagement.Decrypt(
            wrongSizeKey,
            key,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            AssertFailure(changed, "decryption_failed");
            AssertFailure(truncated, "decryption_failed");
            AssertFailure(wrongPadding, "decryption_failed");
            AssertFailure(wrongRecoveredSize, "decryption_failed");
        });
    }

    [Test]
    public void WrapRejectsContentEncryptionKeyWithWrongSize()
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(
                RsaKeyManagement.Wrap(
                    Bytes(31, 1),
                    key,
                    JoseAlgorithms.RsaOaepSha256,
                    JoseAlgorithms.Aes256Gcm),
                "invalid_key_size");
            AssertFailure(
                RsaKeyManagement.Wrap(
                    Bytes(33, 1),
                    key,
                    JoseAlgorithms.RsaOaepSha256,
                    JoseAlgorithms.Aes256Gcm),
                "invalid_key_size");
        });
    }

    [Test]
    public void AlgorithmAndKeyMetadataAreBound()
    {
        TokenKey joseMismatch = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha384,
            true).Key;
        TokenKey coseMismatch = RsaKeyImporterOperations.ImportCose(
            rsa,
            CoseAlgorithms.RsaOaep,
            true).Key;
        TokenKey wrongType = RsaKeyImporterOperations.Import(rsa, true).Key;
        wrongType.KeyType = TokenKeyTypes.Symmetric;

        Assert.Multiple(() =>
        {
            AssertFailure(
                RsaKeyManagement.Encrypt(
                    joseMismatch,
                    JoseAlgorithms.RsaOaepSha256,
                    JoseAlgorithms.Aes256Gcm),
                "conflicting_key_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(
                    coseMismatch,
                    JoseAlgorithms.RsaOaepSha256,
                    JoseAlgorithms.Aes256Gcm),
                "conflicting_key_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(
                    wrongType,
                    JoseAlgorithms.RsaOaepSha256,
                    JoseAlgorithms.Aes256Gcm),
                "invalid_key_type");
        });
    }

    [Test]
    public void UseAndOperationsAreEnforcedPerDirection()
    {
        byte[] contentEncryptionKey = Bytes(32, 2);
        TokenKey unrestricted = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        RsaKeyManagementResult wrapped = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            unrestricted,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        TokenKey unwrapOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        unwrapOnly.KeyOperations = new string[] { TokenKeyOperations.UnwrapKey };
        TokenKey wrapOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        wrapOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationWrapKey };
        TokenKey signatureUse = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaOaepSha256,
            true).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        RsaKeyManagementResult allowedUnwrap = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            unwrapOnly,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult deniedWrap = RsaKeyManagement.Wrap(
            contentEncryptionKey,
            unwrapOnly,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult deniedUnwrap = RsaKeyManagement.Decrypt(
            wrapped.EncryptedKey,
            wrapOnly,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);
        RsaKeyManagementResult deniedUse = RsaKeyManagement.Encrypt(
            signatureUse,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(allowedUnwrap.Success, Is.True);
            AssertFailure(deniedWrap, "key_operation_not_permitted");
            AssertFailure(deniedUnwrap, "key_operation_not_permitted");
            AssertFailure(deniedUse, "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void UnsupportedAlgorithmsReturnStableFailures()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(
                RsaKeyManagement.Encrypt(key, "rsa-oaep-256", JoseAlgorithms.Aes256Gcm),
                "unknown_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaPkcs1, JoseAlgorithms.Aes256Gcm),
                "compatibility_required");
            AssertFailure(
                RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaSha1, JoseAlgorithms.Aes256Gcm),
                "prohibited_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaOaepSha256, "a256gcm"),
                "unknown_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaOaepSha256, JoseAlgorithms.HmacSha256),
                "invalid_algorithm");
            AssertFailure(
                RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaOaepSha256, JoseAlgorithms.Aes256Cbc),
                "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RsaKeyManagement.Encrypt(null, JoseAlgorithms.RsaOaep, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Encrypt(key, null, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Encrypt(key, JoseAlgorithms.RsaOaep, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Wrap(null, key, JoseAlgorithms.RsaOaep, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Decrypt((byte[])null, key, JoseAlgorithms.RsaOaep, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Decrypt(new byte[256], null, JoseAlgorithms.RsaOaep, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Decrypt(new byte[256], key, null, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaKeyManagement.Decrypt(new byte[256], key, JoseAlgorithms.RsaOaep, null),
                Throws.ArgumentNullException);
        });
    }

    static RSAEncryptionPadding Padding(string algorithm)
    {
        if (algorithm == JoseAlgorithms.RsaOaep)
        {
            return RSAEncryptionPadding.OaepSHA1;
        }

        if (algorithm == JoseAlgorithms.RsaOaepSha256)
        {
            return RSAEncryptionPadding.OaepSHA256;
        }

        if (algorithm == JoseAlgorithms.RsaOaepSha384)
        {
            return RSAEncryptionPadding.OaepSHA384;
        }

        return RSAEncryptionPadding.OaepSHA512;
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static void AssertSuccess(
        RsaKeyManagementResult result,
        byte[] expectedContentEncryptionKey,
        int encryptedKeySize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentEncryptionKey,
                Is.EqualTo(expectedContentEncryptionKey));
            Assert.That(result.EncryptedKey, Has.Length.EqualTo(encryptedKeySize));
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
