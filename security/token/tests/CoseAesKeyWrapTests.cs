using token;

namespace token.Tests;

public class CoseAesKeyWrapTests
{
    [TestCase(
        CoseAlgorithms.Aes128KeyWrap,
        "000102030405060708090A0B0C0D0E0F",
        "1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5")]
    [TestCase(
        CoseAlgorithms.Aes192KeyWrap,
        "000102030405060708090A0B0C0D0E0F1011121314151617",
        "96778B25AE6CA435F92B5B97C050AED2468AB8A17AD84E5D")]
    [TestCase(
        CoseAlgorithms.Aes256KeyWrap,
        "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
        "64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7")]
    public void WrapAndUnwrapMatchRfc3394Vectors(
        int algorithm,
        string keyHex,
        string wrappedHex)
    {
        byte[] keyData = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF");
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Convert.FromHexString(keyHex),
            algorithm).Key;

        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            keyData,
            key,
            algorithm);
        TokenBytesResult unwrapped = CoseAesKeyWrap.Unwrap(
            Convert.FromHexString(wrappedHex),
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertSuccess(
                wrapped,
                Convert.FromHexString(wrappedHex));
            AssertSuccess(unwrapped, keyData);
        });
    }

    [Test]
    public void WrapAndUnwrapSupportMoreThanTwoInputBlocks()
    {
        byte[] keyEncryptionKey = Convert.FromHexString(
            "000102030405060708090A0B0C0D0E0F1011121314151617");
        byte[] keyData = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF0001020304050607");
        byte[] expected = Convert.FromHexString(
            "031D33264E15D33268F24EC260743EDCE1C6C7DDEE725A936BA814915C6762D2");
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            keyEncryptionKey,
            CoseAlgorithms.Aes192KeyWrap).Key;

        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            keyData,
            key,
            CoseAlgorithms.Aes192KeyWrap);
        TokenBytesResult unwrapped = CoseAesKeyWrap.Unwrap(
            wrapped.Bytes,
            key,
            CoseAlgorithms.Aes192KeyWrap);

        Assert.Multiple(() =>
        {
            AssertSuccess(wrapped, expected);
            AssertSuccess(unwrapped, keyData);
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 7),
            CoseAlgorithms.Aes256KeyWrap).Key;
        byte[] keyData = Container(Bytes(32, 5), 99, 88);

        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            keyData.AsSpan(1, 32),
            key,
            CoseAlgorithms.Aes256KeyWrap);
        byte[] wrappedContainer = Container(
            wrapped.Bytes,
            77,
            66);
        TokenBytesResult unwrapped = CoseAesKeyWrap.Unwrap(
            wrappedContainer.AsSpan(1, wrapped.Bytes.Length),
            key,
            CoseAlgorithms.Aes256KeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            AssertSuccess(unwrapped, keyData[1..^1]);
            Assert.That(keyData[0], Is.EqualTo(99));
            Assert.That(keyData[^1], Is.EqualTo(88));
            Assert.That(wrappedContainer[0], Is.EqualTo(77));
            Assert.That(wrappedContainer[^1], Is.EqualTo(66));
        });
    }

    [Test]
    public void WrapRequiresAtLeastTwoCompleteBlocks()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 1),
            CoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseAesKeyWrap.Wrap(
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(8, 1),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(15, 1),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(17, 1),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
        });
    }

    [Test]
    public void UnwrapFailuresDoNotRevealCause()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 2),
            CoseAlgorithms.Aes128KeyWrap).Key;
        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            Bytes(32, 3),
            key,
            CoseAlgorithms.Aes128KeyWrap);
        byte[] first = Changed(wrapped.Bytes, 0);
        byte[] middle = Changed(
            wrapped.Bytes,
            wrapped.Bytes.Length / 2);
        byte[] last = Changed(
            wrapped.Bytes,
            wrapped.Bytes.Length - 1);
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 4),
            CoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseAesKeyWrap.Unwrap(
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                Bytes(16, 1),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                Bytes(25, 1),
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                first,
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                middle,
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                last,
                key,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                wrapped.Bytes,
                wrongKey,
                CoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
        });
    }

    [TestCase(CoseAlgorithms.Aes128KeyWrap, 15)]
    [TestCase(CoseAlgorithms.Aes128KeyWrap, 17)]
    [TestCase(CoseAlgorithms.Aes192KeyWrap, 16)]
    [TestCase(CoseAlgorithms.Aes192KeyWrap, 32)]
    [TestCase(CoseAlgorithms.Aes256KeyWrap, 24)]
    [TestCase(CoseAlgorithms.Aes256KeyWrap, 40)]
    public void KeyEncryptionKeyMustExactlyMatchAlgorithmSize(
        int algorithm,
        int keySize)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        key.SymmetricKey = Bytes(keySize, 1);

        TokenBytesResult result = CoseAesKeyWrap.Wrap(
            Bytes(16, 2),
            key,
            algorithm);

        string expected = keySize
            < SymmetricKeyImporter.MinimumKeySizeInBytes
            ? "weak_key"
            : "invalid_key_size";
        AssertFailure(result, expected);
    }

    [Test]
    public void EquivalentJoseAndCoseKeyMetadataAreAccepted()
    {
        TokenKey jose = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256KeyWrap).Key;
        TokenKey cose = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 2),
            CoseAlgorithms.Aes256KeyWrap).Key;
        TokenKey both = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 3),
            JoseAlgorithms.Aes256KeyWrap).Key;
        both.CoseAlgorithm = CoseAlgorithms.Aes256KeyWrap;
        both.HasCoseAlgorithm = true;

        Assert.Multiple(() =>
        {
            Assert.That(CoseAesKeyWrap.Wrap(
                Bytes(16, 4),
                jose,
                CoseAlgorithms.Aes256KeyWrap).Success,
                Is.True);
            Assert.That(CoseAesKeyWrap.Wrap(
                Bytes(16, 4),
                cose,
                CoseAlgorithms.Aes256KeyWrap).Success,
                Is.True);
            Assert.That(CoseAesKeyWrap.Wrap(
                Bytes(16, 4),
                both,
                CoseAlgorithms.Aes256KeyWrap).Success,
                Is.True);
        });
    }

    [Test]
    public void ConflictingKeyMetadataAndWrongTypeAreRejected()
    {
        TokenKey joseConflict = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256Gcm).Key;
        TokenKey coseConflict = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.Aes256Gcm).Key;
        TokenKey wrongType = SymmetricKeyImporterOperations.ImportCose(
            Bytes(32, 1),
            CoseAlgorithms.Aes256KeyWrap).Key;
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                joseConflict,
                CoseAlgorithms.Aes256KeyWrap),
                "conflicting_key_algorithm");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                coseConflict,
                CoseAlgorithms.Aes256KeyWrap),
                "conflicting_key_algorithm");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                wrongType,
                CoseAlgorithms.Aes256KeyWrap),
                "invalid_key_type");
        });
    }

    [Test]
    public void UseAndOperationsAreEnforcedPerDirection()
    {
        TokenKey unrestricted = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 5),
            CoseAlgorithms.Aes128KeyWrap).Key;
        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            Bytes(16, 6),
            unrestricted,
            CoseAlgorithms.Aes128KeyWrap);
        TokenKey unwrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 5),
            CoseAlgorithms.Aes128KeyWrap).Key;
        unwrapOnly.KeyOperations =
            new string[] { TokenKeyOperations.UnwrapKey };
        TokenKey wrapOnly = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 5),
            CoseAlgorithms.Aes128KeyWrap).Key;
        wrapOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationWrapKey };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 5),
            CoseAlgorithms.Aes128KeyWrap).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        Assert.Multiple(() =>
        {
            Assert.That(CoseAesKeyWrap.Unwrap(
                wrapped.Bytes,
                unwrapOnly,
                CoseAlgorithms.Aes128KeyWrap).Success,
                Is.True);
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 6),
                unwrapOnly,
                CoseAlgorithms.Aes128KeyWrap),
                "key_operation_not_permitted");
            AssertFailure(CoseAesKeyWrap.Unwrap(
                wrapped.Bytes,
                wrapOnly,
                CoseAlgorithms.Aes128KeyWrap),
                "key_operation_not_permitted");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 6),
                signatureUse,
                CoseAlgorithms.Aes128KeyWrap),
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void UnsupportedAndNonKeyWrapAlgorithmsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(
            Bytes(32, 1)).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                999999),
                "unknown_algorithm");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                CoseAlgorithms.Aes256Gcm),
                "invalid_algorithm");
            AssertFailure(CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                key,
                CoseAlgorithms.RsaSha1),
                "unsupported_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Bytes(16, 1),
            CoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseAesKeyWrap.Wrap(
                null,
                key,
                CoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesKeyWrap.Wrap(
                Bytes(16, 1),
                null,
                CoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesKeyWrap.Unwrap(
                null,
                key,
                CoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(() => CoseAesKeyWrap.Unwrap(
                Bytes(24, 1),
                null,
                CoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static byte[] Container(
        byte[] value,
        byte first,
        byte last)
    {
        byte[] result = new byte[value.Length + 2];
        result[0] = first;
        value.CopyTo(result, 1);
        result[^1] = last;
        return result;
    }

    static byte[] Changed(byte[] value, int index)
    {
        byte[] result = value.ToArray();
        result[index] ^= 1;
        return result;
    }

    static void AssertSuccess(
        TokenBytesResult result,
        byte[] expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    static void AssertFailure(
        TokenBytesResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(-1));
    }
}
