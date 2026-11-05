using token;

namespace token.Tests;

public class AesKeyWrapTests
{
    [TestCase(
        JoseAlgorithms.Aes128KeyWrap,
        "000102030405060708090A0B0C0D0E0F",
        "1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5")]
    [TestCase(
        JoseAlgorithms.Aes192KeyWrap,
        "000102030405060708090A0B0C0D0E0F1011121314151617",
        "96778B25AE6CA435F92B5B97C050AED2468AB8A17AD84E5D")]
    [TestCase(
        JoseAlgorithms.Aes256KeyWrap,
        "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
        "64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7")]
    public void WrapAndUnwrapMatchRfc3394Vectors(
        string algorithm,
        string keyHex,
        string wrappedHex)
    {
        byte[] keyData = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Convert.FromHexString(keyHex),
            algorithm).Key;

        TokenBytesResult wrapped = AesKeyWrap.Wrap(keyData, key, algorithm);
        TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
            Convert.FromHexString(wrappedHex),
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            Assert.That(wrapped.Error, Is.Null);
            Assert.That(wrapped.Bytes, Is.EqualTo(Convert.FromHexString(wrappedHex)));
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(unwrapped.Error, Is.Null);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyData));
        });
    }

    [Test]
    public void WrapMatchesRfc3394MultiBlockVector()
    {
        byte[] kek = Convert.FromHexString(
            "000102030405060708090A0B0C0D0E0F1011121314151617");
        byte[] keyData = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF0001020304050607");
        byte[] expected = Convert.FromHexString(
            "031D33264E15D33268F24EC260743EDCE1C6C7DDEE725A936BA814915C6762D2");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            kek,
            JoseAlgorithms.Aes192KeyWrap).Key;

        TokenBytesResult wrapped = AesKeyWrap.Wrap(
            keyData,
            key,
            JoseAlgorithms.Aes192KeyWrap);
        TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
            wrapped.Bytes,
            key,
            JoseAlgorithms.Aes192KeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Bytes, Is.EqualTo(expected));
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyData));
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 7),
            JoseAlgorithms.Aes256KeyWrap).Key;
        byte[] keyData = new byte[34];
        keyData[0] = 99;
        keyData[33] = 88;
        Array.Fill(keyData, (byte)5, 1, 32);

        TokenBytesResult wrapped = AesKeyWrap.Wrap(
            keyData.AsSpan(1, 32),
            key,
            JoseAlgorithms.Aes256KeyWrap);
        byte[] container = new byte[wrapped.Bytes.Length + 2];
        wrapped.Bytes.CopyTo(container, 1);
        TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
            container.AsSpan(1, wrapped.Bytes.Length),
            key,
            JoseAlgorithms.Aes256KeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyData.AsSpan(1, 32).ToArray()));
            Assert.That(keyData[0], Is.EqualTo(99));
            Assert.That(keyData[33], Is.EqualTo(88));
        });
    }

    [Test]
    public void WrapRequiresAtLeastTwoCompleteBlocks()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 1),
            JoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                AesKeyWrap.Wrap(Array.Empty<byte>(), key, JoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertBytesFailure(
                AesKeyWrap.Wrap(Bytes(8, 1), key, JoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertBytesFailure(
                AesKeyWrap.Wrap(Bytes(15, 1), key, JoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
            AssertBytesFailure(
                AesKeyWrap.Wrap(Bytes(17, 1), key, JoseAlgorithms.Aes128KeyWrap),
                "invalid_key_size");
        });
    }

    [Test]
    public void UnwrapUsesUniformFailureForMalformedOrUnauthenticatedInput()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 2),
            JoseAlgorithms.Aes128KeyWrap).Key;
        TokenBytesResult wrapped = AesKeyWrap.Wrap(
            Bytes(32, 3),
            key,
            JoseAlgorithms.Aes128KeyWrap);
        byte[] first = (byte[])wrapped.Bytes.Clone();
        first[0] ^= 1;
        byte[] middle = (byte[])wrapped.Bytes.Clone();
        middle[middle.Length / 2] ^= 1;
        byte[] last = (byte[])wrapped.Bytes.Clone();
        last[last.Length - 1] ^= 1;
        TokenKey wrongKey = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 4),
            JoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                AesKeyWrap.Unwrap(Array.Empty<byte>(), key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(Bytes(16, 1), key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(Bytes(25, 1), key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(first, key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(middle, key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(last, key, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(wrapped.Bytes, wrongKey, JoseAlgorithms.Aes128KeyWrap),
                "unwrap_failed");
        });
    }

    [TestCase(JoseAlgorithms.Aes128KeyWrap, 15)]
    [TestCase(JoseAlgorithms.Aes128KeyWrap, 17)]
    [TestCase(JoseAlgorithms.Aes192KeyWrap, 16)]
    [TestCase(JoseAlgorithms.Aes192KeyWrap, 32)]
    [TestCase(JoseAlgorithms.Aes256KeyWrap, 24)]
    [TestCase(JoseAlgorithms.Aes256KeyWrap, 40)]
    public void KeyEncryptionKeyMustExactlyMatchAlgorithmSize(
        string algorithm,
        int keySize)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = Bytes(keySize, 1);

        TokenBytesResult result = AesKeyWrap.Wrap(Bytes(16, 2), key, algorithm);

        string expected = keySize < SymmetricKeyImporter.MinimumKeySizeInBytes
            ? "weak_key"
            : "invalid_key_size";
        AssertBytesFailure(result, expected);
    }

    [Test]
    public void JoseAndEquivalentCoseKeyMetadataAreAccepted()
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
            Assert.That(AesKeyWrap.Wrap(
                Bytes(16, 4),
                jose,
                JoseAlgorithms.Aes256KeyWrap).Success, Is.True);
            Assert.That(AesKeyWrap.Wrap(
                Bytes(16, 4),
                cose,
                JoseAlgorithms.Aes256KeyWrap).Success, Is.True);
            Assert.That(AesKeyWrap.Wrap(
                Bytes(16, 4),
                both,
                JoseAlgorithms.Aes256KeyWrap).Success, Is.True);
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
        TokenKey wrongType = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 1),
            JoseAlgorithms.Aes256KeyWrap).Key;
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 1),
                    joseConflict,
                    JoseAlgorithms.Aes256KeyWrap),
                "conflicting_key_algorithm");
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 1),
                    coseConflict,
                    JoseAlgorithms.Aes256KeyWrap),
                "conflicting_key_algorithm");
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 1),
                    wrongType,
                    JoseAlgorithms.Aes256KeyWrap),
                "invalid_key_type");
        });
    }

    [Test]
    public void UseAndOperationsAreEnforcedPerDirection()
    {
        TokenKey unrestricted = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 5),
            JoseAlgorithms.Aes128KeyWrap).Key;
        TokenBytesResult wrapped = AesKeyWrap.Wrap(
            Bytes(16, 6),
            unrestricted,
            JoseAlgorithms.Aes128KeyWrap);
        TokenKey unwrapOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 5),
            JoseAlgorithms.Aes128KeyWrap).Key;
        unwrapOnly.KeyOperations = new string[] { TokenKeyOperations.UnwrapKey };
        TokenKey wrapOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 5),
            JoseAlgorithms.Aes128KeyWrap).Key;
        wrapOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationWrapKey };
        TokenKey signatureUse = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 5),
            JoseAlgorithms.Aes128KeyWrap).Key;
        signatureUse.Use = TokenKeyUses.Signature;

        Assert.Multiple(() =>
        {
            Assert.That(AesKeyWrap.Unwrap(
                wrapped.Bytes,
                unwrapOnly,
                JoseAlgorithms.Aes128KeyWrap).Success, Is.True);
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 6),
                    unwrapOnly,
                    JoseAlgorithms.Aes128KeyWrap),
                "key_operation_not_permitted");
            AssertBytesFailure(
                AesKeyWrap.Unwrap(
                    wrapped.Bytes,
                    wrapOnly,
                    JoseAlgorithms.Aes128KeyWrap),
                "key_operation_not_permitted");
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 6),
                    signatureUse,
                    JoseAlgorithms.Aes128KeyWrap),
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void UnsupportedAlgorithmsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(
            Bytes(32, 1)).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                AesKeyWrap.Wrap(Bytes(16, 1), key, "a256kw"),
                "unknown_algorithm");
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 1),
                    key,
                    JoseAlgorithms.Aes256GcmKeyWrap),
                "invalid_algorithm");
            AssertBytesFailure(
                AesKeyWrap.Wrap(
                    Bytes(16, 1),
                    key,
                    JoseAlgorithms.Aes256Cbc),
                "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(16, 1),
            JoseAlgorithms.Aes128KeyWrap).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => AesKeyWrap.Wrap(null, key, JoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(
                () => AesKeyWrap.Wrap(Bytes(16, 1), null, JoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(
                () => AesKeyWrap.Wrap(Bytes(16, 1), key, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => AesKeyWrap.Unwrap(null, key, JoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(
                () => AesKeyWrap.Unwrap(Bytes(24, 1), null, JoseAlgorithms.Aes128KeyWrap),
                Throws.ArgumentNullException);
            Assert.That(
                () => AesKeyWrap.Unwrap(Bytes(24, 1), key, null),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
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
