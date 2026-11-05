using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class AesCbcHmacContentEncryptionTests
{
    [TestCase(
        JoseAlgorithms.Aes128CbcHmacSha256,
        32,
        "c80edfa32ddf39d5ef00c0b468834279"
        + "a2e46a1b8049f792f76bfe54b903a9c9"
        + "a94ac9b47ad2655c5f10f9aef71427e2"
        + "fc6f9b3f399a221489f16362c7032336"
        + "09d45ac69864e3321cf82935ac4096c8"
        + "6e133314c54019e8ca7980dfa4b9cf1b"
        + "384c486f3a54c51078158ee5d79de59f"
        + "bd34d848b3d69550a67646344427ade5"
        + "4b8851ffb598f7f80074b9473c82e2db",
        "652c3fa36b0a7c5b3219fab3a30bc1c4")]
    [TestCase(
        JoseAlgorithms.Aes192CbcHmacSha384,
        48,
        "ea65da6b59e61edb419be62d19712ae5"
        + "d303eeb50052d0dfd6697f77224c8edb"
        + "000d279bdc14c1072654bd30944230c6"
        + "57bed4ca0c9f4a8466f22b226d174621"
        + "4bf8cfc2400add9f5126e479663fc90b"
        + "3bed787a2f0ffcbf3904be2a641d5c21"
        + "05bfe591bae23b1d7449e532eef60a9a"
        + "c8bb6c6b01d35d49787bcd57ef484927"
        + "f280adc91ac0c4e79c7b11efc60054e3",
        "8490ac0e58949bfe51875d733f93ac2075168039ccc733d7")]
    [TestCase(
        JoseAlgorithms.Aes256CbcHmacSha512,
        64,
        "4affaaadb78c31c5da4b1b590d10ffbd"
        + "3dd8d5d302423526912da037ecbcc7bd"
        + "822c301dd67c373bccb584ad3e9279c2"
        + "e6d12a1374b77f077553df829410446b"
        + "36ebd97066296ae6427ea75c2e0846a1"
        + "1a09ccf5370dc80bfecbad28c73f09b3"
        + "a3b75e662a2594410ae496b2e2e6609e"
        + "31e6e02cc837f053d21f37ff4f51950b"
        + "be2638d09dd7a4930930806d0703b1f6",
        "4dd3b4c088a7f45c216839645b2012bf"
        + "2e6269a8c56a816dbc1b267761955bc5")]
    public void EncryptAndDecryptMatchRfc7518AppendixBVectors(
        string algorithm,
        int keySize,
        string expectedCiphertextHex,
        string expectedTagHex)
    {
        byte[] keyBytes = Sequence(keySize, 0);
        byte[] plaintext = Encoding.ASCII.GetBytes(
            "A cipher system must not be required to be secret, and it must "
            + "be able to fall into the hands of the enemy without inconvenience");
        byte[] initializationVector = Convert.FromHexString(
            "1af38c2dc2b96ffdd86694092341bc04");
        byte[] additionalAuthenticatedData = Encoding.ASCII.GetBytes(
            "The second principle of Auguste Kerckhoffs");
        byte[] expectedCiphertext = Convert.FromHexString(
            expectedCiphertextHex);
        byte[] expectedTag = Convert.FromHexString(expectedTagHex);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            algorithm).Key;

        AesCbcHmacContentEncryptionResult encrypted =
            AesCbcHmacContentEncryption.Encrypt(
                plaintext,
                initializationVector,
                additionalAuthenticatedData,
                key,
                algorithm);
        TokenBytesResult decrypted = AesCbcHmacContentEncryption.Decrypt(
            expectedCiphertext,
            initializationVector,
            expectedTag,
            additionalAuthenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(
                encrypted,
                expectedCiphertext.Length,
                keySize / 2);
            Assert.That(encrypted.Ciphertext,
                Is.EqualTo(expectedCiphertext));
            Assert.That(encrypted.InitializationVector,
                Is.EqualTo(initializationVector));
            Assert.That(encrypted.InitializationVector,
                Is.Not.SameAs(initializationVector));
            Assert.That(encrypted.AuthenticationTag,
                Is.EqualTo(expectedTag));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Error, Is.Null);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(0, 16)]
    [TestCase(1, 16)]
    [TestCase(15, 16)]
    [TestCase(16, 32)]
    [TestCase(17, 32)]
    [TestCase(64, 80)]
    public void GeneratedIvAndPkcs7PaddingRoundTrip(
        int plaintextSize,
        int ciphertextSize)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(48, 10),
            JoseAlgorithms.Aes192CbcHmacSha384).Key;
        byte[] plaintext = Sequence(plaintextSize, 55);
        byte[] additionalAuthenticatedData = Sequence(13, 91);

        AesCbcHmacContentEncryptionResult encrypted =
            AesCbcHmacContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes192CbcHmacSha384);
        TokenBytesResult decrypted = AesCbcHmacContentEncryption.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            additionalAuthenticatedData,
            key,
            JoseAlgorithms.Aes192CbcHmacSha384);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, ciphertextSize, 24);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void GeneratedInitializationVectorsAreFresh()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Sequence(64, 2),
            JoseAlgorithms.Aes256CbcHmacSha512).Key;
        byte[] plaintext = Sequence(32, 4);
        byte[] additionalAuthenticatedData = Sequence(10, 6);

        AesCbcHmacContentEncryptionResult first =
            AesCbcHmacContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes256CbcHmacSha512);
        AesCbcHmacContentEncryptionResult second =
            AesCbcHmacContentEncryption.Encrypt(
                plaintext,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes256CbcHmacSha512);

        Assert.Multiple(() =>
        {
            Assert.That(first.InitializationVector, Has.Length.EqualTo(16));
            Assert.That(second.InitializationVector, Has.Length.EqualTo(16));
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
            Sequence(32, 7),
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        byte[] plaintext = Container(Sequence(19, 31));
        byte[] initializationVector = Container(Sequence(16, 57));
        byte[] additionalAuthenticatedData = Container(Sequence(7, 89));

        AesCbcHmacContentEncryptionResult encrypted =
            AesCbcHmacContentEncryption.Encrypt(
                plaintext.AsSpan(1, 19),
                initializationVector.AsSpan(1, 16),
                additionalAuthenticatedData.AsSpan(1, 7),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256);
        byte[] ciphertext = Container(encrypted.Ciphertext);
        byte[] tag = Container(encrypted.AuthenticationTag);
        TokenBytesResult decrypted = AesCbcHmacContentEncryption.Decrypt(
            ciphertext.AsSpan(1, encrypted.Ciphertext.Length),
            initializationVector.AsSpan(1, 16),
            tag.AsSpan(1, 16),
            additionalAuthenticatedData.AsSpan(1, 7),
            key,
            JoseAlgorithms.Aes128CbcHmacSha256);

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
    public void AuthenticationAndPaddingFailuresAreIndistinguishable()
    {
        byte[] keyBytes = Sequence(32, 17);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 18),
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        byte[] additionalAuthenticatedData = Sequence(8, 44);
        AesCbcHmacContentEncryptionResult encrypted =
            AesCbcHmacContentEncryption.Encrypt(
                Sequence(21, 61),
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256);
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
        byte[] invalidPaddingCiphertext;
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes[16..];
            invalidPaddingCiphertext = aes.EncryptCbc(
                new byte[16],
                encrypted.InitializationVector,
                PaddingMode.None);
        }

        byte[] validTagForInvalidPadding = ComputeTag(
            additionalAuthenticatedData,
            encrypted.InitializationVector,
            invalidPaddingCiphertext,
            keyBytes[..16]);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(Decrypt(
                changedCiphertext,
                encrypted,
                additionalAuthenticatedData,
                key), "decryption_failed");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                encrypted.Ciphertext,
                changedInitializationVector,
                encrypted.AuthenticationTag,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                changedTag,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
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
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                invalidPaddingCiphertext,
                encrypted.InitializationVector,
                validTagForInvalidPadding,
                additionalAuthenticatedData,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
        });
    }

    [Test]
    public void MalformedIvCiphertextAndTagReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            new byte[32],
            JoseAlgorithms.Aes128CbcHmacSha256).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                new byte[15],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "invalid_initialization_vector");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                Array.Empty<byte>(),
                new byte[16],
                new byte[16],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                new byte[15],
                new byte[16],
                new byte[16],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                new byte[16],
                new byte[15],
                new byte[16],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                new byte[16],
                new byte[16],
                new byte[15],
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128CbcHmacSha256), "decryption_failed");
        });
    }

    [Test]
    public void KeyMetadataIsEnforced()
    {
        TokenKey encryptOnly = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        encryptOnly.KeyOperations = new string[] { TokenKeyOperations.Encrypt };
        TokenKey decryptOnly = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        decryptOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationDecrypt };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 1),
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        signatureUse.Use = TokenKeyUses.Signature;
        TokenKey coseAlgorithm = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 1),
            CoseAlgorithms.HmacSha256).Key;
        AesCbcHmacContentEncryptionResult encrypted =
            AesCbcHmacContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                encryptOnly,
                JoseAlgorithms.Aes128CbcHmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                decryptOnly,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "key_operation_not_permitted");
            Assert.That(AesCbcHmacContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                Array.Empty<byte>(),
                decryptOnly,
                JoseAlgorithms.Aes128CbcHmacSha256).Success, Is.True);
            AssertBytesFailure(AesCbcHmacContentEncryption.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                Array.Empty<byte>(),
                encryptOnly,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "key_operation_not_permitted");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                signatureUse,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "key_use_algorithm_mismatch");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Sequence(5, 7),
                Array.Empty<byte>(),
                coseAlgorithm,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void InvalidKeysAndAlgorithmsReturnStableFailures()
    {
        TokenKey wrongSize = SymmetricKeyImporterOperations.Import(
            new byte[33]).Key;
        TokenKey wrongType = new TokenKey();
        wrongType.KeyType = TokenKeyTypes.Rsa;
        wrongType.SymmetricKey = new byte[32];
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.Symmetric;
        TokenKey key = SymmetricKeyImporterOperations.Import(new byte[32]).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                wrongSize,
                JoseAlgorithms.Aes128CbcHmacSha256), "invalid_key_size");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                wrongType,
                JoseAlgorithms.Aes128CbcHmacSha256), "invalid_key_type");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                missing,
                JoseAlgorithms.Aes128CbcHmacSha256),
                "missing_key_material");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                "a128cbc-hs256"), "unknown_algorithm");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128Gcm), "invalid_algorithm");
            AssertEncryptionFailure(AesCbcHmacContentEncryption.Encrypt(
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
            new byte[32],
            JoseAlgorithms.Aes128CbcHmacSha256).Key;
        byte[] empty = Array.Empty<byte>();
        byte[] initializationVector = new byte[16];
        byte[] tag = new byte[16];

        Assert.Multiple(() =>
        {
            Assert.That(() => AesCbcHmacContentEncryption.Encrypt(
                (byte[])null,
                empty,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Encrypt(
                empty,
                (byte[])null,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Encrypt(
                empty,
                initializationVector,
                null,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Encrypt(
                empty,
                empty,
                null,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Encrypt(
                empty,
                empty,
                key,
                null), Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
                null,
                initializationVector,
                tag,
                empty,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
                empty,
                null,
                tag,
                empty,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
                empty,
                initializationVector,
                null,
                empty,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
                empty,
                initializationVector,
                tag,
                null,
                key,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
                empty,
                initializationVector,
                tag,
                empty,
                null,
                JoseAlgorithms.Aes128CbcHmacSha256),
                Throws.ArgumentNullException);
            Assert.That(() => AesCbcHmacContentEncryption.Decrypt(
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
        AesCbcHmacContentEncryptionResult encrypted,
        byte[] additionalAuthenticatedData,
        TokenKey key)
    {
        return AesCbcHmacContentEncryption.Decrypt(
            ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            additionalAuthenticatedData,
            key,
            JoseAlgorithms.Aes128CbcHmacSha256);
    }

    static byte[] ComputeTag(
        byte[] additionalAuthenticatedData,
        byte[] initializationVector,
        byte[] ciphertext,
        byte[] macKey)
    {
        byte[] bitLength = new byte[8];
        ulong value = (ulong)additionalAuthenticatedData.Length * 8;
        int index = 7;
        while (index >= 0)
        {
            bitLength[index] = (byte)value;
            value >>= 8;
            index -= 1;
        }

        byte[] input = additionalAuthenticatedData
            .Concat(initializationVector)
            .Concat(ciphertext)
            .Concat(bitLength)
            .ToArray();
        return HMACSHA256.HashData(macKey, input)[..16];
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
        AesCbcHmacContentEncryptionResult result,
        int ciphertextSize,
        int tagSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Ciphertext,
                Has.Length.EqualTo(ciphertextSize));
            Assert.That(result.InitializationVector, Has.Length.EqualTo(16));
            Assert.That(result.AuthenticationTag,
                Has.Length.EqualTo(tagSize));
        });
    }

    static void AssertEncryptionFailure(
        AesCbcHmacContentEncryptionResult result,
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
