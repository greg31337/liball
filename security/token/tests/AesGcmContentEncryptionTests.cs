using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class AesGcmContentEncryptionTests
{
    [Test]
    public void EncryptAndDecryptMatchRfc7520Section56Vector()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "You can trust us to stick with you through thick and "
            + "thin–to the bitter end. And you can trust us to "
            + "keep any secret of yours–closer than you keep it "
            + "yourself. But you cannot trust us to let you face trouble "
            + "alone, and go off without a word. We are your friends, Frodo.");
        byte[] keyBytes = Decode("XctOhJAkA-pD9Lh7ZgW_2A");
        byte[] initializationVector = Decode("refa467QzzKx6QAB");
        byte[] additionalAuthenticatedData = Encoding.ASCII.GetBytes(
            "eyJhbGciOiJkaXIiLCJraWQiOiI3N2M3ZTJiOC02ZTEzLTQ1Y2YtODY3Mi02MT"
            + "diNWI0NTI0M2EiLCJlbmMiOiJBMTI4R0NNIn0");
        byte[] expectedCiphertext = Decode(
            "JW_i_f52hww_ELQPGaYyeAB6HYGcR559l9TYnSovc23XJoBcW29rHP8yZOZG7Y"
            + "hLpT1bjFuvZPjQS-m0IFtVcXkZXdH_lr_FrdYt9HRUYkshtrMmIUAyGmUnd9zM"
            + "DB2n0cRDIHAzFVeJUDxkUwVAE7_YGRPdcqMyiBoCO-FBdE-Nceb4h3-FtBP-c_"
            + "BIwCPTjb9o0SbdcdREEMJMyZBH8ySWMVi1gPD9yxi-aQpGbSv_F9N4IZAxscj5"
            + "g-NJsUPbjk29-s7LJAGb15wEBtXphVCgyy53CoIKLHHeJHXex45Uz9aKZSRSIn"
            + "ZI-wjsY0yu3cT4_aQ3i1o-tiE-F8Ios61EKgyIQ4CWao8PFMj8TTnp");
        byte[] expectedTag = Decode("vbb32Xvllea2OtmHAdccRQ");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            JoseAlgorithms.Aes128Gcm).Key;

        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                plaintext,
                initializationVector,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = AesGcmContentEncryption.Decrypt(
            expectedCiphertext,
            initializationVector,
            expectedTag,
            additionalAuthenticatedData,
            key,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, plaintext.Length);
            Assert.That(encrypted.Ciphertext, Is.EqualTo(expectedCiphertext));
            Assert.That(encrypted.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(encrypted.InitializationVector,
                Is.Not.SameAs(initializationVector));
            Assert.That(encrypted.AuthenticationTag, Is.EqualTo(expectedTag));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Error, Is.Null);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(JoseAlgorithms.Aes256Gcm, 32)]
    public void GeneratedEncryptionInteroperatesWithBuiltInAesGcm(
        string algorithm,
        int keySize)
    {
        byte[] keyBytes = Sequence(keySize, 3);
        byte[] plaintext = Sequence(47, 29);
        byte[] additionalAuthenticatedData = Sequence(23, 91);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            algorithm).Key;

        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                algorithm);
        byte[] platformPlaintext = new byte[plaintext.Length];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Decrypt(
                encrypted.InitializationVector,
                encrypted.Ciphertext,
                encrypted.AuthenticationTag,
                platformPlaintext,
                additionalAuthenticatedData);
        }

        byte[] platformInitializationVector = Sequence(12, 121);
        byte[] platformCiphertext = new byte[plaintext.Length];
        byte[] platformTag = new byte[16];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Encrypt(
                platformInitializationVector,
                plaintext,
                platformCiphertext,
                platformTag,
                additionalAuthenticatedData);
        }

        TokenBytesResult decrypted = AesGcmContentEncryption.Decrypt(
            platformCiphertext,
            platformInitializationVector,
            platformTag,
            additionalAuthenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, plaintext.Length);
            Assert.That(platformPlaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void GeneratedInitializationVectorsAreFresh()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.Aes256Gcm).Key;
        byte[] plaintext = Sequence(32, 2);
        byte[] additionalAuthenticatedData = Sequence(10, 3);

        AesGcmContentEncryptionResult first =
            AesGcmContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes256Gcm);
        AesGcmContentEncryptionResult second =
            AesGcmContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(first.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(second.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(first.InitializationVector,
                Is.Not.EqualTo(second.InitializationVector));
            Assert.That(first.Ciphertext, Is.Not.EqualTo(second.Ciphertext));
            Assert.That(first.AuthenticationTag,
                Is.Not.EqualTo(second.AuthenticationTag));
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(24, 4),
            JoseAlgorithms.Aes192Gcm).Key;
        byte[] plaintext = Container(Sequence(19, 31));
        byte[] initializationVector = Container(Sequence(12, 57));
        byte[] additionalAuthenticatedData = Container(Sequence(7, 89));

        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                plaintext.AsSpan(1, 19),
                initializationVector.AsSpan(1, 12),
                additionalAuthenticatedData.AsSpan(1, 7),
                key,
                JoseAlgorithms.Aes192Gcm);
        byte[] ciphertext = Container(encrypted.Ciphertext);
        byte[] tag = Container(encrypted.AuthenticationTag);
        TokenBytesResult decrypted = AesGcmContentEncryption.Decrypt(
            ciphertext.AsSpan(1, encrypted.Ciphertext.Length),
            initializationVector.AsSpan(1, 12),
            tag.AsSpan(1, 16),
            additionalAuthenticatedData.AsSpan(1, 7),
            key,
            JoseAlgorithms.Aes192Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext[1..^1]));
            Assert.That(encrypted.InitializationVector,
                Is.EqualTo(initializationVector[1..^1]));
            Assert.That(plaintext[0], Is.Zero);
            Assert.That(plaintext[^1], Is.Zero);
            Assert.That(additionalAuthenticatedData[0], Is.Zero);
            Assert.That(additionalAuthenticatedData[^1], Is.Zero);
        });
    }

    [Test]
    public void EmptyPlaintextIsAuthenticatedAndRoundTrips()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            new byte[16],
            JoseAlgorithms.Aes128Gcm).Key;

        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                new byte[12],
                Encoding.ASCII.GetBytes("protected"),
                key,
                JoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = AesGcmContentEncryption.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            Encoding.ASCII.GetBytes("protected"),
            key,
            JoseAlgorithms.Aes128Gcm);

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
        byte[] keyBytes = Sequence(16, 17);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            JoseAlgorithms.Aes128Gcm).Key;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, 18),
            JoseAlgorithms.Aes128Gcm).Key;
        byte[] additionalAuthenticatedData = Sequence(8, 44);
        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                Sequence(21, 61),
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128Gcm);
        byte[] changedCiphertext = encrypted.Ciphertext.ToArray();
        changedCiphertext[0] ^= 1;
        byte[] changedInitializationVector =
            encrypted.InitializationVector.ToArray();
        changedInitializationVector[0] ^= 1;
        byte[] changedTag = encrypted.AuthenticationTag.ToArray();
        changedTag[0] ^= 1;
        byte[] changedAdditionalAuthenticatedData =
            additionalAuthenticatedData.ToArray();
        changedAdditionalAuthenticatedData[0] ^= 1;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(Decrypt(
                changedCiphertext,
                encrypted,
                additionalAuthenticatedData,
                key), "decryption_failed");
            AssertBytesFailure(AesGcmContentEncryption.Decrypt(
                encrypted.Ciphertext,
                changedInitializationVector,
                encrypted.AuthenticationTag,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128Gcm), "decryption_failed");
            AssertBytesFailure(AesGcmContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                changedTag,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128Gcm), "decryption_failed");
            AssertBytesFailure(Decrypt(
                encrypted.Ciphertext,
                encrypted,
                changedAdditionalAuthenticatedData,
                key), "decryption_failed");
            AssertBytesFailure(Decrypt(
                encrypted.Ciphertext,
                encrypted,
                additionalAuthenticatedData,
                wrongKey), "decryption_failed");
        });
    }

    [Test]
    public void InvalidIvAndTagLengthsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            new byte[16],
            JoseAlgorithms.Aes128Gcm).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                new byte[11],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128Gcm), "invalid_initialization_vector");
            AssertBytesFailure(AesGcmContentEncryption.Decrypt(
                Array.Empty<byte>(),
                new byte[11],
                new byte[16],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128Gcm), "decryption_failed");
            AssertBytesFailure(AesGcmContentEncryption.Decrypt(
                Array.Empty<byte>(),
                new byte[12],
                new byte[15],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128Gcm), "decryption_failed");
        });
    }

    [Test]
    public void JoseAndEquivalentCoseAlgorithmBindingsAreAccepted()
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
        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                Sequence(9, 2),
                Array.Empty<byte>(),
                joseKey,
                JoseAlgorithms.Aes256Gcm);

        TokenBytesResult decrypted = AesGcmContentEncryption.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            Array.Empty<byte>(),
            coseKey,
            JoseAlgorithms.Aes256Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.Success, Is.True);
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Sequence(9, 2),
                Array.Empty<byte>(),
                conflicting,
                JoseAlgorithms.Aes256Gcm), "conflicting_key_algorithm");
        });
    }

    [Test]
    public void UseAndOperationMetadataAreEnforced()
    {
        TokenKey encryptOnly = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, 1),
            JoseAlgorithms.Aes128Gcm).Key;
        encryptOnly.KeyOperations = new string[] { TokenKeyOperations.Encrypt };
        TokenKey decryptOnly = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            CoseAlgorithms.Aes128Gcm).Key;
        decryptOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationDecrypt };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportJose(
            Sequence(16, 1),
            JoseAlgorithms.Aes128Gcm).Key;
        signatureUse.Use = TokenKeyUses.Signature;
        AesGcmContentEncryptionResult encrypted =
            AesGcmContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                encryptOnly,
                JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                decryptOnly,
                JoseAlgorithms.Aes128Gcm), "key_operation_not_permitted");
            Assert.That(AesGcmContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                Array.Empty<byte>(),
                decryptOnly,
                JoseAlgorithms.Aes128Gcm).Success, Is.True);
            AssertBytesFailure(AesGcmContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                Array.Empty<byte>(),
                encryptOnly,
                JoseAlgorithms.Aes128Gcm), "key_operation_not_permitted");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                signatureUse,
                JoseAlgorithms.Aes128Gcm), "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void InvalidKeysAndAlgorithmsReturnStableFailures()
    {
        TokenKey wrongSize = SymmetricKeyImporterOperations.Import(
            new byte[17]).Key;
        TokenKey wrongType = new TokenKey();
        wrongType.KeyType = TokenKeyTypes.Rsa;
        wrongType.SymmetricKey = new byte[16];
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.Symmetric;
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[16]).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                wrongSize,
                JoseAlgorithms.Aes128Gcm), "invalid_key_size");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                wrongType,
                JoseAlgorithms.Aes128Gcm), "invalid_key_type");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                missing,
                JoseAlgorithms.Aes128Gcm), "missing_key_material");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                "a128gcm"), "unknown_algorithm");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128KeyWrap), "invalid_algorithm");
            AssertEncryptionFailure(AesGcmContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128Cbc), "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            new byte[16],
            JoseAlgorithms.Aes128Gcm).Key;
        byte[] empty = Array.Empty<byte>();
        byte[] initializationVector = new byte[12];
        byte[] tag = new byte[16];

        Assert.Multiple(() =>
        {
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                (byte[])null, empty, key, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                empty, (byte[])null, key, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                empty, empty, null, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                empty, empty, key, null), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                empty, (byte[])null, empty, key, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Encrypt(
                empty,
                initializationVector,
                null,
                key,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                null,
                initializationVector,
                tag,
                empty,
                key,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                empty,
                null,
                tag,
                empty,
                key,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                empty,
                initializationVector,
                null,
                empty,
                key,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                empty,
                initializationVector,
                tag,
                null,
                key,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                empty,
                initializationVector,
                tag,
                empty,
                null,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => AesGcmContentEncryption.Decrypt(
                empty,
                initializationVector,
                tag,
                empty,
                key,
                null), Throws.ArgumentNullException);
        });
    }

    static TokenBytesResult Decrypt(
        byte[] ciphertext,
        AesGcmContentEncryptionResult encrypted,
        byte[] additionalAuthenticatedData,
        TokenKey key)
    {
        return AesGcmContentEncryption.Decrypt(
            ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            additionalAuthenticatedData,
            key,
            JoseAlgorithms.Aes128Gcm);
    }

    static byte[] Decode(string text)
    {
        TokenBytesResult result = Base64Url.Decode(text);
        Assert.That(result.Success, Is.True);
        return result.Bytes;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }

    static byte[] Container(byte[] value)
    {
        byte[] result = new byte[value.Length + 2];
        value.CopyTo(result, 1);
        return result;
    }

    static void AssertEncryptionSuccess(
        AesGcmContentEncryptionResult result,
        int ciphertextSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Ciphertext,
                Has.Length.EqualTo(ciphertextSize));
            Assert.That(result.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(result.AuthenticationTag, Has.Length.EqualTo(16));
        });
    }

    static void AssertEncryptionFailure(
        AesGcmContentEncryptionResult result,
        string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Ciphertext, Is.Null);
            Assert.That(result.InitializationVector, Is.Null);
            Assert.That(result.AuthenticationTag, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
