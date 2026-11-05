using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseAesGcmTests
{
    [Test]
    public void EncryptAndDecryptMatchNistGcmVector()
    {
        byte[] keyBytes = new byte[16];
        byte[] initializationVector = new byte[12];
        byte[] plaintext = new byte[16];
        byte[] expectedCiphertext = Convert.FromHexString(
            "0388DACE60B6A392F328C2B971B2FE78");
        byte[] expectedTag = Convert.FromHexString(
            "AB6E47D42CEC13BDF53A67B21257BDDF");
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            CoseAlgorithms.Aes128Gcm).Key;

        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            plaintext,
            initializationVector,
            Array.Empty<byte>(),
            key,
            CoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            expectedCiphertext,
            initializationVector,
            expectedTag,
            Array.Empty<byte>(),
            key,
            CoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, plaintext.Length);
            Assert.That(encrypted.Ciphertext,
                Is.EqualTo(expectedCiphertext));
            Assert.That(encrypted.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(encrypted.AuthenticationTag,
                Is.EqualTo(expectedTag));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(CoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(CoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(CoseAlgorithms.Aes256Gcm, 32)]
    public void EveryCoseAesGcmAlgorithmInteroperatesWithBuiltInAesGcm(
        int algorithm,
        int keySize)
    {
        byte[] keyBytes = Sequence(keySize, 1);
        byte[] plaintext = Sequence(37, 31);
        byte[] initializationVector = Sequence(12, 71);
        byte[] authenticatedData = Sequence(19, 101);
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            algorithm).Key;

        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            plaintext,
            initializationVector,
            authenticatedData,
            key,
            algorithm);
        byte[] builtInPlaintext = new byte[plaintext.Length];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Decrypt(
                initializationVector,
                encrypted.Ciphertext,
                encrypted.AuthenticationTag,
                builtInPlaintext,
                authenticatedData);
        }

        byte[] platformCiphertext = new byte[plaintext.Length];
        byte[] platformTag = new byte[16];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                platformCiphertext,
                platformTag,
                authenticatedData);
        }

        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            platformCiphertext,
            initializationVector,
            platformTag,
            authenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, plaintext.Length);
            Assert.That(builtInPlaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void GeneratedInitializationVectorsAreFresh()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.Aes256Gcm).Key;
        byte[] plaintext = Sequence(32, 2);
        byte[] authenticatedData = Sequence(10, 3);

        CoseAesGcmResult first = CoseAesGcm.Encrypt(
            plaintext,
            authenticatedData,
            key,
            CoseAlgorithms.Aes256Gcm);
        CoseAesGcmResult second = CoseAesGcm.Encrypt(
            plaintext,
            authenticatedData,
            key,
            CoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(first.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(second.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(first.InitializationVector,
                Is.Not.EqualTo(second.InitializationVector));
            Assert.That(first.Ciphertext,
                Is.Not.EqualTo(second.Ciphertext));
            Assert.That(first.AuthenticationTag,
                Is.Not.EqualTo(second.AuthenticationTag));
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(24, 4),
            CoseAlgorithms.Aes192Gcm).Key;
        byte[] plaintext = Container(Sequence(19, 31));
        byte[] initializationVector =
            Container(Sequence(12, 57));
        byte[] authenticatedData = Container(Sequence(7, 89));

        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            plaintext.AsSpan(1, 19),
            initializationVector.AsSpan(1, 12),
            authenticatedData.AsSpan(1, 7),
            key,
            CoseAlgorithms.Aes192Gcm);
        byte[] ciphertext = Container(encrypted.Ciphertext);
        byte[] tag = Container(encrypted.AuthenticationTag);
        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            ciphertext.AsSpan(1, encrypted.Ciphertext.Length),
            initializationVector.AsSpan(1, 12),
            tag.AsSpan(1, 16),
            authenticatedData.AsSpan(1, 7),
            key,
            CoseAlgorithms.Aes192Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes,
                Is.EqualTo(plaintext[1..^1]));
            Assert.That(encrypted.InitializationVector,
                Is.EqualTo(initializationVector[1..^1]));
            Assert.That(plaintext[0], Is.Zero);
            Assert.That(plaintext[^1], Is.Zero);
            Assert.That(authenticatedData[0], Is.Zero);
            Assert.That(authenticatedData[^1], Is.Zero);
        });
    }

    [Test]
    public void EmptyPlaintextIsAuthenticatedAndRoundTrips()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            CoseAlgorithms.Aes128Gcm).Key;
        byte[] authenticatedData =
            Convert.FromHexString("8368456E6372797074304040");

        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            Array.Empty<byte>(),
            new byte[12],
            authenticatedData,
            key,
            CoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            authenticatedData,
            key,
            CoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, 0);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.Empty);
        });
    }

    [Test]
    public void AuthenticationFailuresAreIndistinguishable()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 17),
            CoseAlgorithms.Aes128Gcm).Key;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 18),
            CoseAlgorithms.Aes128Gcm).Key;
        byte[] authenticatedData = Sequence(8, 44);
        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            Sequence(21, 61),
            authenticatedData,
            key,
            CoseAlgorithms.Aes128Gcm);
        byte[] changedCiphertext = encrypted.Ciphertext.ToArray();
        changedCiphertext[0] ^= 1;
        byte[] changedIv = encrypted.InitializationVector.ToArray();
        changedIv[0] ^= 1;
        byte[] changedTag = encrypted.AuthenticationTag.ToArray();
        changedTag[0] ^= 1;
        byte[] changedAad = authenticatedData.ToArray();
        changedAad[0] ^= 1;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(Decrypt(
                changedCiphertext,
                encrypted,
                authenticatedData,
                key), "decryption_failed");
            AssertBytesFailure(CoseAesGcm.Decrypt(
                encrypted.Ciphertext,
                changedIv,
                encrypted.AuthenticationTag,
                authenticatedData,
                key,
                CoseAlgorithms.Aes128Gcm), "decryption_failed");
            AssertBytesFailure(CoseAesGcm.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                changedTag,
                authenticatedData,
                key,
                CoseAlgorithms.Aes128Gcm), "decryption_failed");
            AssertBytesFailure(Decrypt(
                encrypted.Ciphertext,
                encrypted,
                changedAad,
                key), "decryption_failed");
            AssertBytesFailure(Decrypt(
                encrypted.Ciphertext,
                encrypted,
                authenticatedData,
                wrongKey), "decryption_failed");
        });
    }

    [Test]
    public void InvalidNonceAndTagLengthsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            CoseAlgorithms.Aes128Gcm).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(CoseAesGcm.Encrypt(
                Array.Empty<byte>(),
                new byte[11],
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Aes128Gcm),
                "invalid_initialization_vector");
            AssertBytesFailure(CoseAesGcm.Decrypt(
                Array.Empty<byte>(),
                new byte[11],
                new byte[16],
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Aes128Gcm),
                "decryption_failed");
            AssertBytesFailure(CoseAesGcm.Decrypt(
                Array.Empty<byte>(),
                new byte[12],
                new byte[15],
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Aes128Gcm),
                "decryption_failed");
        });
    }

    [Test]
    public void JoseAndCoseAlgorithmBindingsAreEnforced()
    {
        byte[] keyBytes = Sequence(32, 8);
        TokenKey joseKey = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            JoseAlgorithms.Aes256Gcm).Key;
        TokenKey coseKey = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            CoseAlgorithms.Aes256Gcm).Key;
        TokenKey conflicting = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            CoseAlgorithms.Aes256KeyWrap).Key;
        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            Sequence(9, 2),
            Array.Empty<byte>(),
            joseKey,
            CoseAlgorithms.Aes256Gcm);

        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            Array.Empty<byte>(),
            coseKey,
            CoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.Success, Is.True);
            AssertEncryptionFailure(CoseAesGcm.Encrypt(
                Sequence(9, 2),
                Array.Empty<byte>(),
                conflicting,
                CoseAlgorithms.Aes256Gcm),
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void EncryptDecryptAndKeyWrapOperationsAreAccepted()
    {
        byte[] data = Sequence(5, 7);
        TokenKey wrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm).Key;
        wrapOnly.KeyOperations =
            new string[] { TokenKeyOperations.WrapKey };
        TokenKey unwrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm).Key;
        unwrapOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationUnwrapKey };
        TokenKey denied = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm).Key;
        denied.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationDecrypt };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            data,
            Array.Empty<byte>(),
            wrapOnly,
            CoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = CoseAesGcm.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            Array.Empty<byte>(),
            unwrapOnly,
            CoseAlgorithms.Aes128Gcm);
        CoseAesGcmResult deniedResult = CoseAesGcm.Encrypt(
            data,
            Array.Empty<byte>(),
            denied,
            CoseAlgorithms.Aes128Gcm);
        CoseAesGcmResult useResult = CoseAesGcm.Encrypt(
            data,
            Array.Empty<byte>(),
            signatureUse,
            CoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(data));
            AssertEncryptionFailure(
                deniedResult,
                "key_operation_not_permitted");
            AssertEncryptionFailure(
                useResult,
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void WrongKeySizesAndAlgorithmsReturnStableFailures()
    {
        TokenKey shortKey = SymmetricKeyImporterOperations.Import(
            Sequence(16, 2)).Key;
        TokenKey valid = SymmetricKeyImporterOperations.Import(
            Sequence(32, 3)).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(CoseAesGcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                shortKey,
                CoseAlgorithms.Aes256Gcm),
                "invalid_key_size");
            AssertEncryptionFailure(CoseAesGcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                valid,
                999999),
                "unknown_algorithm");
            AssertEncryptionFailure(CoseAesGcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                valid,
                CoseAlgorithms.HmacSha256),
                "invalid_algorithm");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        byte[] empty = Array.Empty<byte>();
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            CoseAlgorithms.Aes128Gcm).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseAesGcm.Encrypt(
                    null,
                    empty,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Encrypt(
                    empty,
                    null,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Encrypt(
                    empty,
                    empty,
                    null,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Encrypt(
                    empty,
                    null,
                    empty,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Decrypt(
                    null,
                    new byte[12],
                    new byte[16],
                    empty,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Decrypt(
                    empty,
                    null,
                    new byte[16],
                    empty,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Decrypt(
                    empty,
                    new byte[12],
                    null,
                    empty,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Decrypt(
                    empty,
                    new byte[12],
                    new byte[16],
                    null,
                    key,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseAesGcm.Decrypt(
                    empty,
                    new byte[12],
                    new byte[16],
                    empty,
                    null,
                    CoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ResultDefaultsToNoEncryptionOutput()
    {
        CoseAesGcmResult result = new CoseAesGcmResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Ciphertext, Is.Null);
            Assert.That(result.InitializationVector, Is.Null);
            Assert.That(result.AuthenticationTag, Is.Null);
        });
    }

    static TokenBytesResult Decrypt(
        byte[] ciphertext,
        CoseAesGcmResult encrypted,
        byte[] authenticatedData,
        TokenKey key)
    {
        return CoseAesGcm.Decrypt(
            ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            authenticatedData,
            key,
            CoseAlgorithms.Aes128Gcm);
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        return bytes;
    }

    static byte[] Container(byte[] value)
    {
        byte[] result = new byte[value.Length + 2];
        value.CopyTo(result, 1);
        return result;
    }

    static void AssertEncryptionSuccess(
        CoseAesGcmResult result,
        int ciphertextLength)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Ciphertext,
            Has.Length.EqualTo(ciphertextLength));
        Assert.That(result.InitializationVector,
            Has.Length.EqualTo(12));
        Assert.That(result.AuthenticationTag,
            Has.Length.EqualTo(16));
    }

    static void AssertEncryptionFailure(
        CoseAesGcmResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Ciphertext, Is.Null);
        Assert.That(result.InitializationVector, Is.Null);
        Assert.That(result.AuthenticationTag, Is.Null);
    }

    static void AssertBytesFailure(
        TokenBytesResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }
}
