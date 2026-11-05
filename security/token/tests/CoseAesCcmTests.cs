using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseAesCcmTests
{
    [Test]
    public void EncryptAndDecryptMatchRfc3610PacketVectorOne()
    {
        RequireAesCcm();
        byte[] keyBytes = Convert.FromHexString(
            "C0C1C2C3C4C5C6C7C8C9CACBCCCDCECF");
        byte[] initializationVector = Convert.FromHexString(
            "00000003020100A0A1A2A3A4A5");
        byte[] authenticatedData = Convert.FromHexString(
            "0001020304050607");
        byte[] plaintext = Convert.FromHexString(
            "08090A0B0C0D0E0F101112131415161718191A1B1C1D1E");
        byte[] expectedCiphertext = Convert.FromHexString(
            "588C979A61C663D2F066D0C2C0F989806D5F6B61DAC384");
        byte[] expectedTag = Convert.FromHexString(
            "17E8D12CFDF926E0");
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            CoseAlgorithms.AesCcm16_64_128).Key;

        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            plaintext,
            initializationVector,
            authenticatedData,
            key,
            CoseAlgorithms.AesCcm16_64_128);
        TokenBytesResult decrypted = CoseAesCcm.Decrypt(
            expectedCiphertext,
            initializationVector,
            expectedTag,
            authenticatedData,
            key,
            CoseAlgorithms.AesCcm16_64_128);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(
                encrypted,
                plaintext.Length,
                13,
                8);
            Assert.That(encrypted.Ciphertext,
                Is.EqualTo(expectedCiphertext));
            Assert.That(encrypted.AuthenticationTag,
                Is.EqualTo(expectedTag));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(CoseAlgorithms.AesCcm16_64_128, 16, 13, 8)]
    [TestCase(CoseAlgorithms.AesCcm16_64_256, 32, 13, 8)]
    [TestCase(CoseAlgorithms.AesCcm64_64_128, 16, 7, 8)]
    [TestCase(CoseAlgorithms.AesCcm64_64_256, 32, 7, 8)]
    [TestCase(CoseAlgorithms.AesCcm16_128_128, 16, 13, 16)]
    [TestCase(CoseAlgorithms.AesCcm16_128_256, 32, 13, 16)]
    [TestCase(CoseAlgorithms.AesCcm64_128_128, 16, 7, 16)]
    [TestCase(CoseAlgorithms.AesCcm64_128_256, 32, 7, 16)]
    public void EveryCoseAesCcmAlgorithmInteroperatesWithBuiltInAesCcm(
        int algorithm,
        int keySize,
        int initializationVectorSize,
        int authenticationTagSize)
    {
        RequireAesCcm();
        byte[] keyBytes = Sequence(keySize, 1);
        byte[] plaintext = Sequence(37, 31);
        byte[] initializationVector = Sequence(
            initializationVectorSize,
            71);
        byte[] authenticatedData = Sequence(19, 101);
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            keyBytes,
            algorithm).Key;

        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            plaintext,
            initializationVector,
            authenticatedData,
            key,
            algorithm);
        byte[] builtInPlaintext = new byte[plaintext.Length];
        using (AesCcm aes = new AesCcm(keyBytes))
        {
            aes.Decrypt(
                initializationVector,
                encrypted.Ciphertext,
                encrypted.AuthenticationTag,
                builtInPlaintext,
                authenticatedData);
        }

        byte[] platformCiphertext = new byte[plaintext.Length];
        byte[] platformTag = new byte[authenticationTagSize];
        using (AesCcm aes = new AesCcm(keyBytes))
        {
            aes.Encrypt(
                initializationVector,
                plaintext,
                platformCiphertext,
                platformTag,
                authenticatedData);
        }

        TokenBytesResult decrypted = CoseAesCcm.Decrypt(
            platformCiphertext,
            initializationVector,
            platformTag,
            authenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(
                encrypted,
                plaintext.Length,
                initializationVectorSize,
                authenticationTagSize);
            Assert.That(builtInPlaintext, Is.EqualTo(plaintext));
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [TestCase(CoseAlgorithms.AesCcm16_64_128, 13, 8)]
    [TestCase(CoseAlgorithms.AesCcm64_128_128, 7, 16)]
    public void GeneratedInitializationVectorsAreFreshAndSizedForAlgorithm(
        int algorithm,
        int initializationVectorSize,
        int authenticationTagSize)
    {
        RequireAesCcm();
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            algorithm).Key;
        byte[] plaintext = Sequence(32, 2);
        byte[] authenticatedData = Sequence(10, 3);

        CoseAesCcmResult first = CoseAesCcm.Encrypt(
            plaintext,
            authenticatedData,
            key,
            algorithm);
        CoseAesCcmResult second = CoseAesCcm.Encrypt(
            plaintext,
            authenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(
                first,
                plaintext.Length,
                initializationVectorSize,
                authenticationTagSize);
            AssertEncryptionSuccess(
                second,
                plaintext.Length,
                initializationVectorSize,
                authenticationTagSize);
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
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm64_128_256;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 4),
            algorithm).Key;
        byte[] plaintext = Container(Sequence(19, 31));
        byte[] initializationVector = Container(Sequence(7, 57));
        byte[] authenticatedData = Container(Sequence(7, 89));

        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            plaintext.AsSpan(1, 19),
            initializationVector.AsSpan(1, 7),
            authenticatedData.AsSpan(1, 7),
            key,
            algorithm);
        byte[] ciphertext = Container(encrypted.Ciphertext);
        byte[] tag = Container(encrypted.AuthenticationTag);
        TokenBytesResult decrypted = CoseAesCcm.Decrypt(
            ciphertext.AsSpan(1, encrypted.Ciphertext.Length),
            initializationVector.AsSpan(1, 7),
            tag.AsSpan(1, 16),
            authenticatedData.AsSpan(1, 7),
            key,
            algorithm);

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
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm16_128_128;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            algorithm).Key;
        byte[] authenticatedData =
            Convert.FromHexString("8368456E6372797074304040");

        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            Array.Empty<byte>(),
            new byte[13],
            authenticatedData,
            key,
            algorithm);
        TokenBytesResult decrypted = CoseAesCcm.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            authenticatedData,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(encrypted, 0, 13, 16);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.Empty);
        });
    }

    [Test]
    public void AuthenticationFailuresAreIndistinguishable()
    {
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm16_64_128;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 17),
            algorithm).Key;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 18),
            algorithm).Key;
        byte[] authenticatedData = Sequence(8, 44);
        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            Sequence(21, 61),
            authenticatedData,
            key,
            algorithm);
        byte[] changedCiphertext = Changed(encrypted.Ciphertext);
        byte[] changedIv = Changed(encrypted.InitializationVector);
        byte[] changedTag = Changed(encrypted.AuthenticationTag);
        byte[] changedAad = Changed(authenticatedData);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(CoseAesCcm.Decrypt(
                changedCiphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                authenticatedData,
                key,
                algorithm), "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                encrypted.Ciphertext,
                changedIv,
                encrypted.AuthenticationTag,
                authenticatedData,
                key,
                algorithm), "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                changedTag,
                authenticatedData,
                key,
                algorithm), "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                changedAad,
                key,
                algorithm), "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                encrypted.Ciphertext,
                encrypted.InitializationVector,
                encrypted.AuthenticationTag,
                authenticatedData,
                wrongKey,
                algorithm), "decryption_failed");
        });
    }

    [Test]
    public void InvalidNonceTagAndPayloadLengthsReturnStableFailures()
    {
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm16_64_128;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            algorithm).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                new byte[12],
                Array.Empty<byte>(),
                key,
                algorithm),
                "invalid_initialization_vector");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                new byte[65536],
                new byte[13],
                Array.Empty<byte>(),
                key,
                algorithm),
                "plaintext_too_large");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                Array.Empty<byte>(),
                new byte[12],
                new byte[8],
                Array.Empty<byte>(),
                key,
                algorithm),
                "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                Array.Empty<byte>(),
                new byte[13],
                new byte[7],
                Array.Empty<byte>(),
                key,
                algorithm),
                "decryption_failed");
            AssertBytesFailure(CoseAesCcm.Decrypt(
                new byte[65536],
                new byte[13],
                new byte[8],
                Array.Empty<byte>(),
                key,
                algorithm),
                "decryption_failed");
        });
    }

    [Test]
    public void AlgorithmAndKeyMetadataAreEnforced()
    {
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm64_128_256;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 8),
            algorithm).Key;
        TokenKey conflictingCose = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 8),
            CoseAlgorithms.Aes256Gcm).Key;
        TokenKey conflictingJose = SymmetricKeyImporterOperations.ImportJose(
            Sequence(32, 8),
            JoseAlgorithms.Aes256Gcm).Key;
        TokenKey wrongUse = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 8),
            algorithm).Key;
        wrongUse.Use = TokenKeyUses.Signature;
        TokenKey denied = SymmetricKeyImporterOperations.ImportCose(
            Sequence(32, 8),
            algorithm).Key;
        denied.KeyOperations =
            new string[] { TokenKeyOperations.Decrypt };

        Assert.Multiple(() =>
        {
            AssertEncryptionSuccess(CoseAesCcm.Encrypt(
                Sequence(9, 2),
                Array.Empty<byte>(),
                key,
                algorithm), 9, 7, 16);
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                conflictingCose,
                algorithm),
                "conflicting_key_algorithm");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                conflictingJose,
                algorithm),
                "conflicting_key_algorithm");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                wrongUse,
                algorithm),
                "key_use_algorithm_mismatch");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                denied,
                algorithm),
                "key_operation_not_permitted");
        });
    }

    [Test]
    public void KeyWrapOperationsAreAcceptedForEncryptAndDecrypt()
    {
        RequireAesCcm();
        int algorithm = CoseAlgorithms.AesCcm64_64_128;
        byte[] data = Sequence(5, 7);
        TokenKey wrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            algorithm).Key;
        wrapOnly.KeyOperations =
            new string[] { TokenKeyOperations.WrapKey };
        TokenKey unwrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Sequence(16, 1),
            algorithm).Key;
        unwrapOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationUnwrapKey };

        CoseAesCcmResult encrypted = CoseAesCcm.Encrypt(
            data,
            Array.Empty<byte>(),
            wrapOnly,
            algorithm);
        TokenBytesResult decrypted = CoseAesCcm.Decrypt(
            encrypted.Ciphertext,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            Array.Empty<byte>(),
            unwrapOnly,
            algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Bytes, Is.EqualTo(data));
        });
    }

    [Test]
    public void WrongKeySizesAndAlgorithmsReturnStableFailures()
    {
        RequireAesCcm();
        TokenKey shortKey = SymmetricKeyImporterOperations.Import(
            Sequence(16, 2)).Key;
        TokenKey valid = SymmetricKeyImporterOperations.Import(
            Sequence(32, 3)).Key;

        Assert.Multiple(() =>
        {
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                shortKey,
                CoseAlgorithms.AesCcm64_128_256),
                "invalid_key_size");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                valid,
                999999),
                "unknown_algorithm");
            AssertEncryptionFailure(CoseAesCcm.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                valid,
                CoseAlgorithms.HmacSha256),
                "invalid_algorithm");
        });
    }

    [Test]
    public void UnavailableProviderReturnsUnsupportedAlgorithm()
    {
        if (AesCcm.IsSupported)
        {
            Assert.Pass();
        }

        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            CoseAlgorithms.AesCcm16_64_128).Key;
        CoseAesCcmResult result = CoseAesCcm.Encrypt(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            key,
            CoseAlgorithms.AesCcm16_64_128);

        AssertEncryptionFailure(result, "unsupported_algorithm");
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        byte[] empty = Array.Empty<byte>();
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            new byte[16],
            CoseAlgorithms.AesCcm16_64_128).Key;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseAesCcm.Encrypt(
                null,
                empty,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Encrypt(
                empty,
                null,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Encrypt(
                empty,
                empty,
                null,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Encrypt(
                empty,
                null,
                empty,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Decrypt(
                null,
                new byte[13],
                new byte[8],
                empty,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Decrypt(
                empty,
                null,
                new byte[8],
                empty,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Decrypt(
                empty,
                new byte[13],
                null,
                empty,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Decrypt(
                empty,
                new byte[13],
                new byte[8],
                null,
                key,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesCcm.Decrypt(
                empty,
                new byte[13],
                new byte[8],
                empty,
                null,
                CoseAlgorithms.AesCcm16_64_128),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ResultDefaultsToNoEncryptionOutput()
    {
        CoseAesCcmResult result = new CoseAesCcmResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Ciphertext, Is.Null);
            Assert.That(result.InitializationVector, Is.Null);
            Assert.That(result.AuthenticationTag, Is.Null);
        });
    }

    static void RequireAesCcm()
    {
        if (!AesCcm.IsSupported)
        {
            Assert.Ignore("AES-CCM is not supported by this platform.");
        }
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

    static byte[] Changed(byte[] value)
    {
        byte[] result = value.ToArray();
        result[0] ^= 1;
        return result;
    }

    static void AssertEncryptionSuccess(
        CoseAesCcmResult result,
        int ciphertextLength,
        int initializationVectorSize,
        int authenticationTagSize)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Ciphertext,
            Has.Length.EqualTo(ciphertextLength));
        Assert.That(result.InitializationVector,
            Has.Length.EqualTo(initializationVectorSize));
        Assert.That(result.AuthenticationTag,
            Has.Length.EqualTo(authenticationTagSize));
    }

    static void AssertEncryptionFailure(
        CoseAesCcmResult result,
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
