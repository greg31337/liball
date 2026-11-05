using System.Security.Cryptography;
using token;

namespace token.Tests;

public class AesGcmKeyWrapTests
{
    [Test]
    public void UnwrapMatchesNistAes128GcmVector()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            new byte[16],
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        byte[] encryptedKey = Convert.FromHexString(
            "0388DACE60B6A392F328C2B971B2FE78");
        byte[] initializationVector = new byte[12];
        byte[] authenticationTag = Convert.FromHexString(
            "AB6E47D42CEC13BDF53A67B21257BDDF");

        TokenBytesResult result = AesGcmKeyWrap.Unwrap(
            encryptedKey,
            initializationVector,
            authenticationTag,
            key,
            JoseAlgorithms.Aes128GcmKeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(new byte[16]));
        });
    }

    [TestCase(JoseAlgorithms.Aes128GcmKeyWrap, 16)]
    [TestCase(JoseAlgorithms.Aes192GcmKeyWrap, 24)]
    [TestCase(JoseAlgorithms.Aes256GcmKeyWrap, 32)]
    public void WrapAndUnwrapInteroperateWithBuiltInAesGcm(
        string algorithm,
        int keySize)
    {
        byte[] keyBytes = Sequence(keySize, 1);
        byte[] keyMaterial = Sequence(32, 31);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            keyBytes,
            algorithm).Key;

        AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
            keyMaterial,
            key,
            algorithm);
        byte[] platformPlaintext = new byte[keyMaterial.Length];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Decrypt(
                wrapped.InitializationVector,
                wrapped.EncryptedKey,
                wrapped.AuthenticationTag,
                platformPlaintext);
        }

        byte[] platformIv = Sequence(12, 71);
        byte[] platformCiphertext = new byte[keyMaterial.Length];
        byte[] platformTag = new byte[16];
        using (AesGcm aes = new AesGcm(keyBytes, 16))
        {
            aes.Encrypt(
                platformIv,
                keyMaterial,
                platformCiphertext,
                platformTag);
        }

        TokenBytesResult unwrapped = AesGcmKeyWrap.Unwrap(
            platformCiphertext,
            platformIv,
            platformTag,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertWrapSuccess(wrapped, keyMaterial.Length);
            Assert.That(platformPlaintext, Is.EqualTo(keyMaterial));
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(unwrapped.Error, Is.Null);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial));
        });
    }

    [Test]
    public void WrapGeneratesFreshNinetySixBitInitializationVectors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256GcmKeyWrap).Key;

        AesGcmKeyWrapResult first = AesGcmKeyWrap.Wrap(
            Bytes(32, 2),
            key,
            JoseAlgorithms.Aes256GcmKeyWrap);
        AesGcmKeyWrapResult second = AesGcmKeyWrap.Wrap(
            Bytes(32, 2),
            key,
            JoseAlgorithms.Aes256GcmKeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(first.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(second.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(first.InitializationVector,
                Is.Not.EqualTo(second.InitializationVector));
            Assert.That(first.EncryptedKey, Is.Not.EqualTo(second.EncryptedKey));
            Assert.That(first.AuthenticationTag,
                Is.Not.EqualTo(second.AuthenticationTag));
        });
    }

    [Test]
    public void GcmWrapDoesNotRequireCompleteAesBlocks()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 3),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        byte[] keyMaterial = Sequence(17, 9);

        AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
            keyMaterial,
            key,
            JoseAlgorithms.Aes128GcmKeyWrap);
        TokenBytesResult unwrapped = AesGcmKeyWrap.Unwrap(
            wrapped.EncryptedKey,
            wrapped.InitializationVector,
            wrapped.AuthenticationTag,
            key,
            JoseAlgorithms.Aes128GcmKeyWrap);

        Assert.Multiple(() =>
        {
            AssertWrapSuccess(wrapped, 17);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial));
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(24, 4),
            JoseAlgorithms.Aes192GcmKeyWrap).Key;
        byte[] keyContainer = new byte[34];
        Array.Fill(keyContainer, (byte)8, 1, 32);

        AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
            keyContainer.AsSpan(1, 32),
            key,
            JoseAlgorithms.Aes192GcmKeyWrap);
        byte[] encryptedContainer = Container(wrapped.EncryptedKey);
        byte[] ivContainer = Container(wrapped.InitializationVector);
        byte[] tagContainer = Container(wrapped.AuthenticationTag);
        TokenBytesResult unwrapped = AesGcmKeyWrap.Unwrap(
            encryptedContainer.AsSpan(1, wrapped.EncryptedKey.Length),
            ivContainer.AsSpan(1, wrapped.InitializationVector.Length),
            tagContainer.AsSpan(1, wrapped.AuthenticationTag.Length),
            key,
            JoseAlgorithms.Aes192GcmKeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(unwrapped.Bytes, Is.EqualTo(Bytes(32, 8)));
            Assert.That(keyContainer[0], Is.Zero);
            Assert.That(keyContainer[33], Is.Zero);
        });
    }

    [Test]
    public void WrapRejectsEmptyKeyMaterial()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 1),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;

        AssertWrapFailure(
            AesGcmKeyWrap.Wrap(
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.Aes128GcmKeyWrap),
            "invalid_key_size");
    }

    [Test]
    public void UnwrapUsesUniformFailureForMalformedOrUnauthenticatedInput()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 5),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
            Bytes(32, 6),
            key,
            JoseAlgorithms.Aes128GcmKeyWrap);
        byte[] changedEncryptedKey = (byte[])wrapped.EncryptedKey.Clone();
        changedEncryptedKey[0] ^= 1;
        byte[] changedIv = (byte[])wrapped.InitializationVector.Clone();
        changedIv[0] ^= 1;
        byte[] changedTag = (byte[])wrapped.AuthenticationTag.Clone();
        changedTag[0] ^= 1;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 7),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                Array.Empty<byte>(),
                wrapped.InitializationVector,
                wrapped.AuthenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                Bytes(11, 1),
                wrapped.AuthenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                wrapped.InitializationVector,
                Bytes(15, 1),
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                changedEncryptedKey,
                wrapped.InitializationVector,
                wrapped.AuthenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                changedIv,
                wrapped.AuthenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                wrapped.InitializationVector,
                changedTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                wrapped.InitializationVector,
                wrapped.AuthenticationTag,
                wrongKey,
                JoseAlgorithms.Aes128GcmKeyWrap), "unwrap_failed");
        });
    }

    [TestCase(JoseAlgorithms.Aes128GcmKeyWrap, 15)]
    [TestCase(JoseAlgorithms.Aes128GcmKeyWrap, 17)]
    [TestCase(JoseAlgorithms.Aes192GcmKeyWrap, 16)]
    [TestCase(JoseAlgorithms.Aes192GcmKeyWrap, 32)]
    [TestCase(JoseAlgorithms.Aes256GcmKeyWrap, 24)]
    [TestCase(JoseAlgorithms.Aes256GcmKeyWrap, 40)]
    public void KeyEncryptionKeyMustExactlyMatchAlgorithmSize(
        string algorithm,
        int keySize)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = Bytes(keySize, 1);

        AesGcmKeyWrapResult result = AesGcmKeyWrap.Wrap(
            Bytes(16, 2),
            key,
            algorithm);

        string expected = keySize < SymmetricKeyImporter.MinimumKeySizeInBytes
            ? "weak_key"
            : "invalid_key_size";
        AssertWrapFailure(result, expected);
    }

    [Test]
    public void JoseAndUnboundKeyMetadataAreAccepted()
    {
        TokenKey jose = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256GcmKeyWrap).Key;
        TokenKey unbound = SymmetricKeyImporterOperations.Import(Bytes(32, 2)).Key;

        Assert.Multiple(() =>
        {
            Assert.That(AesGcmKeyWrap.Wrap(
                Bytes(16, 3),
                jose,
                JoseAlgorithms.Aes256GcmKeyWrap).Success, Is.True);
            Assert.That(AesGcmKeyWrap.Wrap(
                Bytes(16, 3),
                unbound,
                JoseAlgorithms.Aes256GcmKeyWrap).Success, Is.True);
        });
    }

    [Test]
    public void ConflictingMetadataAndWrongKeyTypeAreRejected()
    {
        TokenKey joseConflict = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256Gcm).Key;
        TokenKey coseConflict = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.Aes256Gcm).Key;
        TokenKey wrongType = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256GcmKeyWrap).Key;
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 2),
                joseConflict,
                JoseAlgorithms.Aes256GcmKeyWrap), "conflicting_key_algorithm");
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 2),
                coseConflict,
                JoseAlgorithms.Aes256GcmKeyWrap), "conflicting_key_algorithm");
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 2),
                wrongType,
                JoseAlgorithms.Aes256GcmKeyWrap), "invalid_key_type");
        });
    }

    [Test]
    public void UseAndOperationsAreEnforcedPerDirection()
    {
        TokenKey unrestricted = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 8),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
            Bytes(16, 9),
            unrestricted,
            JoseAlgorithms.Aes128GcmKeyWrap);
        TokenKey unwrapOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 8),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        unwrapOnly.KeyOperations = new string[] { TokenKeyOperations.UnwrapKey };
        TokenKey wrapOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 8),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        wrapOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationWrapKey };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 8),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        Assert.Multiple(() =>
        {
            Assert.That(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                wrapped.InitializationVector,
                wrapped.AuthenticationTag,
                unwrapOnly,
                JoseAlgorithms.Aes128GcmKeyWrap).Success, Is.True);
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 9),
                unwrapOnly,
                JoseAlgorithms.Aes128GcmKeyWrap), "key_operation_not_permitted");
            AssertBytesFailure(AesGcmKeyWrap.Unwrap(
                wrapped.EncryptedKey,
                wrapped.InitializationVector,
                wrapped.AuthenticationTag,
                wrapOnly,
                JoseAlgorithms.Aes128GcmKeyWrap), "key_operation_not_permitted");
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 9),
                signatureUse,
                JoseAlgorithms.Aes128GcmKeyWrap), "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void UnsupportedAlgorithmsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Bytes(32, 1)).Key;

        Assert.Multiple(() =>
        {
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                "a256gcmkw"), "unknown_algorithm");
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                JoseAlgorithms.Aes256KeyWrap), "invalid_algorithm");
            AssertWrapFailure(AesGcmKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                JoseAlgorithms.Aes256Cbc), "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 1),
            JoseAlgorithms.Aes128GcmKeyWrap).Key;
        byte[] encryptedKey = Bytes(16, 2);
        byte[] initializationVector = Bytes(12, 3);
        byte[] authenticationTag = Bytes(16, 4);

        Assert.Multiple(() =>
        {
            Assert.That(() => AesGcmKeyWrap.Wrap(
                null,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Wrap(
                Bytes(16, 1),
                null,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                null), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Unwrap(
                (byte[])null,
                initializationVector,
                authenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Unwrap(
                encryptedKey,
                null,
                authenticationTag,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Unwrap(
                encryptedKey,
                initializationVector,
                null,
                key,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Unwrap(
                encryptedKey,
                initializationVector,
                authenticationTag,
                null,
                JoseAlgorithms.Aes128GcmKeyWrap), Throws.ArgumentNullException);
            Assert.That(() => AesGcmKeyWrap.Unwrap(
                encryptedKey,
                initializationVector,
                authenticationTag,
                key,
                null), Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        for (int index = 0; index < length; index += 1)
        {
            bytes[index] = (byte)(start + index);
        }

        return bytes;
    }

    static byte[] Container(byte[] value)
    {
        byte[] container = new byte[value.Length + 2];
        value.CopyTo(container, 1);
        return container;
    }

    static void AssertWrapSuccess(AesGcmKeyWrapResult result, int keySize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.EncryptedKey, Has.Length.EqualTo(keySize));
            Assert.That(result.InitializationVector, Has.Length.EqualTo(12));
            Assert.That(result.AuthenticationTag, Has.Length.EqualTo(16));
        });
    }

    static void AssertWrapFailure(AesGcmKeyWrapResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.EncryptedKey, Is.Null);
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
