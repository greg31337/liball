using token;

namespace token.Tests;

public class DirectKeyManagementTests
{
    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    [TestCase(JoseAlgorithms.Aes128CbcHmacSha256, 32)]
    [TestCase(JoseAlgorithms.Aes192CbcHmacSha384, 48)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void EncryptionAndDecryptionReturnExactCopiedCek(
        string contentEncryptionAlgorithm,
        int keySize)
    {
        byte[] secret = Bytes(keySize, 0x5a);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            secret,
            JoseAlgorithms.Direct).Key;

        DirectKeyManagementResult encryption = DirectKeyManagement.Encrypt(
            key,
            contentEncryptionAlgorithm);
        DirectKeyManagementResult decryption = DirectKeyManagement.Decrypt(
            Array.Empty<byte>(),
            key,
            contentEncryptionAlgorithm);

        Assert.Multiple(() =>
        {
            AssertSuccess(encryption, secret);
            AssertSuccess(decryption, secret);
            Assert.That(encryption.ContentEncryptionKey, Is.Not.SameAs(key.SymmetricKey));
            Assert.That(decryption.ContentEncryptionKey, Is.Not.SameAs(key.SymmetricKey));
            Assert.That(decryption.ContentEncryptionKey,
                Is.Not.SameAs(encryption.ContentEncryptionKey));
        });

        encryption.ContentEncryptionKey[0] = 0;
        Assert.Multiple(() =>
        {
            Assert.That(key.SymmetricKey[0], Is.EqualTo(0x5a));
            Assert.That(decryption.ContentEncryptionKey[0], Is.EqualTo(0x5a));
            Assert.That(secret[0], Is.EqualTo(0x5a));
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 15)]
    [TestCase(JoseAlgorithms.Aes128Gcm, 17)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 31)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 33)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 63)]
    [TestCase(JoseAlgorithms.Aes256CbcHmacSha512, 65)]
    public void DirectKeyMustExactlyMatchContentEncryptionSize(
        string contentEncryptionAlgorithm,
        int keySize)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.Algorithm = JoseAlgorithms.Direct;
        key.SymmetricKey = Bytes(keySize, 3);

        DirectKeyManagementResult result = DirectKeyManagement.Encrypt(
            key,
            contentEncryptionAlgorithm);

        string expected = keySize < SymmetricKeyImporter.MinimumKeySizeInBytes
            ? "weak_key"
            : "invalid_key_size";
        AssertFailure(result, expected);
    }

    [Test]
    public void DecryptionRequiresAnEmptyEncryptedKeyValue()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 4),
            JoseAlgorithms.Direct).Key;
        byte[] wrapped = new byte[] { 9, 8, 7 };

        DirectKeyManagementResult arrayResult = DirectKeyManagement.Decrypt(
            new byte[] { 1 },
            key,
            JoseAlgorithms.Aes256Gcm);
        DirectKeyManagementResult spanResult = DirectKeyManagement.Decrypt(
            wrapped.AsSpan(1, 1),
            key,
            JoseAlgorithms.Aes256Gcm);
        DirectKeyManagementResult emptySlice = DirectKeyManagement.Decrypt(
            wrapped.AsSpan(1, 0),
            key,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            AssertFailure(arrayResult, "invalid_encrypted_key");
            AssertFailure(spanResult, "invalid_encrypted_key");
            Assert.That(emptySlice.Success, Is.True);
        });
    }

    [Test]
    public void DirectAndEquivalentCoseMetadataAreAccepted()
    {
        TokenKey jose = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 6),
            JoseAlgorithms.Direct).Key;
        TokenKey cose = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 7),
            CoseAlgorithms.Direct).Key;
        TokenKey both = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 8),
            JoseAlgorithms.Direct).Key;
        both.CoseAlgorithm = CoseAlgorithms.Direct;
        both.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            Assert.That(DirectKeyManagement.Encrypt(
                jose,
                JoseAlgorithms.Aes256Gcm).Success, Is.True);
            Assert.That(DirectKeyManagement.Encrypt(
                cose,
                JoseAlgorithms.Aes256Gcm).Success, Is.True);
            Assert.That(DirectKeyManagement.Encrypt(
                both,
                JoseAlgorithms.Aes256Gcm).Success, Is.True);
        });
    }

    [Test]
    public void MatchingContentAlgorithmsAreAcceptedAndConflictsAreRejected()
    {
        TokenKey joseContent = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256Gcm).Key;
        TokenKey coseContent = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.Aes256Gcm).Key;
        TokenKey joseConflict = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256KeyWrap).Key;
        TokenKey coseConflict = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.Aes256KeyWrap).Key;
        TokenKey wrongType = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Direct).Key;
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            Assert.That(
                DirectKeyManagement.Encrypt(
                    joseContent,
                    JoseAlgorithms.Aes256Gcm).Success,
                Is.True);
            Assert.That(
                DirectKeyManagement.Encrypt(
                    coseContent,
                    JoseAlgorithms.Aes256Gcm).Success,
                Is.True);
            AssertFailure(
                DirectKeyManagement.Encrypt(
                    joseConflict,
                    JoseAlgorithms.Aes256Gcm),
                "conflicting_key_algorithm");
            AssertFailure(
                DirectKeyManagement.Encrypt(
                    coseConflict,
                    JoseAlgorithms.Aes256Gcm),
                "conflicting_key_algorithm");
            AssertFailure(
                DirectKeyManagement.Encrypt(
                    wrongType,
                    JoseAlgorithms.Aes256Gcm),
                "invalid_key_type");
        });
    }

    [Test]
    public void UseAndOperationsAreEnforcedPerDirection()
    {
        TokenKey decryptOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 2),
            JoseAlgorithms.Direct).Key;
        decryptOnly.KeyOperations = new string[] { TokenKeyOperations.Decrypt };
        TokenKey encryptOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 2),
            JoseAlgorithms.Direct).Key;
        encryptOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationEncrypt };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 2),
            JoseAlgorithms.Direct).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        DirectKeyManagementResult allowedDecrypt = DirectKeyManagement.Decrypt(
            Array.Empty<byte>(),
            decryptOnly,
            JoseAlgorithms.Aes256Gcm);
        DirectKeyManagementResult deniedEncrypt = DirectKeyManagement.Encrypt(
            decryptOnly,
            JoseAlgorithms.Aes256Gcm);
        DirectKeyManagementResult deniedDecrypt = DirectKeyManagement.Decrypt(
            Array.Empty<byte>(),
            encryptOnly,
            JoseAlgorithms.Aes256Gcm);
        DirectKeyManagementResult deniedUse = DirectKeyManagement.Encrypt(
            signatureUse,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(allowedDecrypt.Success, Is.True);
            AssertFailure(deniedEncrypt, "key_operation_not_permitted");
            AssertFailure(deniedDecrypt, "key_operation_not_permitted");
            AssertFailure(deniedUse, "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void UnsupportedContentEncryptionAlgorithmsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 5),
            JoseAlgorithms.Direct).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(
                DirectKeyManagement.Encrypt(key, "a256gcm"),
                "unknown_algorithm");
            AssertFailure(
                DirectKeyManagement.Encrypt(key, JoseAlgorithms.HmacSha256),
                "invalid_algorithm");
            AssertFailure(
                DirectKeyManagement.Encrypt(key, JoseAlgorithms.Aes256Cbc),
                "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 5),
            JoseAlgorithms.Direct).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DirectKeyManagement.Encrypt(null, JoseAlgorithms.Aes256Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => DirectKeyManagement.Encrypt(key, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => DirectKeyManagement.Decrypt(
                    (byte[])null,
                    key,
                    JoseAlgorithms.Aes256Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => DirectKeyManagement.Decrypt(
                    Array.Empty<byte>(),
                    null,
                    JoseAlgorithms.Aes256Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => DirectKeyManagement.Decrypt(
                    Array.Empty<byte>(),
                    key,
                    null),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static void AssertSuccess(DirectKeyManagementResult result, byte[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentEncryptionKey, Is.EqualTo(expected));
            Assert.That(result.EncryptedKey, Is.Not.Null);
            Assert.That(result.EncryptedKey, Is.Empty);
        });
    }

    static void AssertFailure(DirectKeyManagementResult result, string code)
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
