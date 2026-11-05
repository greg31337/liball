using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseEcdhAlgorithmsTests
{
    [Test]
    public void DirectAgreementMatchesCoseWorkingGroupVector()
    {
        using ECDsa recipientNative = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64Url.Decode(
                    "Ze2loSV3wrroKUN_4zhwGhCqo3Xhu1td4QjeQ5wIVR0").Bytes,
                Y = Base64Url.Decode(
                    "HlLtdXARY_f55A3fnzQbPcm6hgr34Mp8p-nuzQCE0Zw").Bytes
            },
            D = Base64Url.Decode(
                "r_kHyZ-a06rmxM3yESK84r1otSg-aQcVStkRhA-iCM8").Bytes
        });
        using ECDsa senderNative = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Convert.FromHexString(
                    "98F50A4FF6C05861C8860D13A638EA56C3F5AD7590BBFBF054E1C7B4D91D6280"),
                Y = Convert.FromHexString(
                    "F01400B089867804B8E9FC96C3932161F1934F4223069170D924B7E03BF822BB")
            }
        });
        TokenKey recipient = PrivateKey(recipientNative);
        TokenKey sender = PublicKey(senderNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A1013818");
        using ECDiffieHellman recipientAgreement =
            ECDiffieHellman.Create(recipientNative.ExportParameters(true));
        using ECDiffieHellman senderAgreement =
            ECDiffieHellman.Create(senderNative.ExportParameters(false));
        byte[] secret = recipientAgreement.DeriveRawSecretAgreement(
            senderAgreement.PublicKey);
        TokenBytesResult context = CoseKdfContext.Create(parameters);

        CoseEcdhResult result = CoseEcdhAlgorithms.Decrypt(
            Array.Empty<byte>(),
            recipient,
            sender,
            CoseAlgorithms.EcdhEsHkdfSha256,
            null,
            parameters);

        Assert.Multiple(() =>
        {
            Assert.That(secret, Is.EqualTo(Convert.FromHexString(
                "4B31712E096E5F20B4ECF9790FD8CC7C8B7E2C8AD90BDA81CB224F62C0E7B9A6")));
            Assert.That(context.Bytes, Is.EqualTo(Convert.FromHexString(
                "840183F6F6F683F6F6F682188044A1013818")));
            AssertSuccess(result, Convert.FromHexString(
                "56074D506729CA40C4B4FE50C6439893"));
            Assert.That(result.EncryptedKey, Is.Empty);
        });
    }

    [TestCase(CoseAlgorithms.EcdhEsHkdfSha256, 256, 128)]
    [TestCase(CoseAlgorithms.EcdhEsHkdfSha512, 384, 256)]
    [TestCase(CoseAlgorithms.EcdhSsHkdfSha256, 521, 192)]
    [TestCase(CoseAlgorithms.EcdhSsHkdfSha512, 256, 256)]
    public void DirectModesRoundTrip(
        int algorithm,
        int curveSize,
        int keySize)
    {
        using ECDsa recipientNative = CreateCurve(curveSize);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = CreateCurve(curveSize);
        using ECDsa senderPublicNative = PublicPart(senderNative);
        TokenKey recipientPrivate = PrivateKey(recipientNative);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        TokenKey senderPrivate = PrivateKey(senderNative);
        TokenKey senderPublic = PublicKey(senderPublicNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes256Gcm,
            (ulong)keySize,
            "A10103");
        byte[] salt = IsStatic(algorithm)
            ? new byte[] { 1, 2, 3, 4 }
            : null;

        CoseEcdhResult encrypted = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            IsStatic(algorithm) ? senderPrivate : null,
            algorithm,
            salt,
            parameters);
        CoseEcdhResult decrypted = CoseEcdhAlgorithms.Decrypt(
            encrypted.EncryptedKey,
            recipientPrivate,
            IsStatic(algorithm) ? senderPublic : encrypted.SenderPublicKey,
            algorithm,
            salt,
            parameters);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            AssertSuccess(decrypted, encrypted.ContentKey);
            Assert.That(encrypted.ContentKey, Has.Length.EqualTo(keySize / 8));
            Assert.That(encrypted.EncryptedKey, Is.Empty);
            Assert.That(encrypted.SenderPublicKey == null,
                Is.EqualTo(IsStatic(algorithm)));
        });

        if (encrypted.SenderPublicKey != null)
        {
            encrypted.SenderPublicKey.ReleaseOwnedMaterial();
        }
    }

    [TestCase(CoseAlgorithms.EcdhEsAes128KeyWrap, CoseAlgorithms.Aes128KeyWrap, 128)]
    [TestCase(CoseAlgorithms.EcdhEsAes192KeyWrap, CoseAlgorithms.Aes192KeyWrap, 192)]
    [TestCase(CoseAlgorithms.EcdhEsAes256KeyWrap, CoseAlgorithms.Aes256KeyWrap, 256)]
    [TestCase(CoseAlgorithms.EcdhSsAes128KeyWrap, CoseAlgorithms.Aes128KeyWrap, 128)]
    [TestCase(CoseAlgorithms.EcdhSsAes192KeyWrap, CoseAlgorithms.Aes192KeyWrap, 192)]
    [TestCase(CoseAlgorithms.EcdhSsAes256KeyWrap, CoseAlgorithms.Aes256KeyWrap, 256)]
    public void KeyWrapModesRoundTrip(
        int algorithm,
        int keyWrapAlgorithm,
        int keySize)
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa senderPublicNative = PublicPart(senderNative);
        TokenKey recipientPrivate = PrivateKey(recipientNative);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        TokenKey senderPrivate = PrivateKey(senderNative);
        TokenKey senderPublic = PublicKey(senderPublicNative);
        CoseKdfContextParameters parameters = Context(
            keyWrapAlgorithm,
            (ulong)keySize,
            "A101381C");
        byte[] salt = IsStatic(algorithm)
            ? new byte[] { 8, 6, 7, 5, 3, 0, 9 }
            : null;
        byte[] contentKey = Sequence(32, 17);

        CoseEcdhResult encrypted = CoseEcdhAlgorithms.Wrap(
            contentKey,
            recipientPublic,
            IsStatic(algorithm) ? senderPrivate : null,
            algorithm,
            salt,
            parameters);
        CoseEcdhResult decrypted = CoseEcdhAlgorithms.Decrypt(
            encrypted.EncryptedKey,
            recipientPrivate,
            IsStatic(algorithm) ? senderPublic : encrypted.SenderPublicKey,
            algorithm,
            salt,
            parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(encrypted, contentKey);
            AssertSuccess(decrypted, contentKey);
            Assert.That(encrypted.ContentKey, Is.Not.SameAs(contentKey));
            Assert.That(encrypted.EncryptedKey, Has.Length.EqualTo(40));
            Assert.That(decrypted.EncryptedKey,
                Is.EqualTo(encrypted.EncryptedKey));
        });

        if (encrypted.SenderPublicKey != null)
        {
            encrypted.SenderPublicKey.ReleaseOwnedMaterial();
        }
    }

    [Test]
    public void EphemeralAgreementCreatesAFreshPublicKeyEachTime()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");

        CoseEcdhResult first = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            null,
            CoseAlgorithms.EcdhEsHkdfSha256,
            null,
            parameters);
        CoseEcdhResult second = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            null,
            CoseAlgorithms.EcdhEsHkdfSha256,
            null,
            parameters);
        ECParameters firstPublic = first.SenderPublicKey.Ecdsa.ExportParameters(false);
        ECParameters secondPublic = second.SenderPublicKey.Ecdsa.ExportParameters(false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.ContentKey, Is.Not.EqualTo(first.ContentKey));
            Assert.That(secondPublic.Q.X, Is.Not.EqualTo(firstPublic.Q.X));
        });

        first.SenderPublicKey.ReleaseOwnedMaterial();
        second.SenderPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void StaticAgreementIsDeterministicAndBindsSaltAndContext()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        TokenKey senderPrivate = PrivateKey(senderNative);
        CoseKdfContextParameters firstContext = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        CoseKdfContextParameters secondContext = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        secondContext.PartyVInfo.Identity = new byte[] { 9 };

        CoseEcdhResult first = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 1 },
            firstContext);
        CoseEcdhResult repeated = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 1 },
            firstContext);
        CoseEcdhResult changedSalt = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 2 },
            firstContext);
        CoseEcdhResult changedContext = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 1 },
            secondContext);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.ContentKey, Is.EqualTo(first.ContentKey));
            Assert.That(changedSalt.ContentKey, Is.Not.EqualTo(first.ContentKey));
            Assert.That(changedContext.ContentKey, Is.Not.EqualTo(first.ContentKey));
            Assert.That(first.SenderPublicKey, Is.Null);
        });
    }

    [Test]
    public void StaticAgreementRequiresSaltOrPartyUNonce()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        TokenKey senderPrivate = PrivateKey(senderNative);
        CoseKdfContextParameters missing = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        CoseKdfContextParameters nonce = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        nonce.PartyUInfo.Nonce = Bytes(new byte[] { 4, 2 });

        CoseEcdhResult failed = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            null,
            missing);
        CoseEcdhResult succeeded = CoseEcdhAlgorithms.Derive(
            recipientPublic,
            senderPrivate,
            CoseAlgorithms.EcdhSsHkdfSha256,
            null,
            nonce);

        AssertFailure(failed, "missing_kdf_uniqueness");
        Assert.That(succeeded.Success, Is.True);
    }

    [Test]
    public void WrapSpanUsesOnlySelectedContentKeySlice()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        TokenKey recipientPrivate = PrivateKey(recipientNative);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128KeyWrap,
            128,
            "");
        byte[] container = new byte[34];
        container[0] = 99;
        Sequence(32, 3).CopyTo(container, 1);
        container[33] = 88;

        CoseEcdhResult encrypted = CoseEcdhAlgorithms.Wrap(
            container.AsSpan(1, 32),
            recipientPublic,
            null,
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            null,
            parameters);
        byte[] wrappedContainer = new byte[encrypted.EncryptedKey.Length + 2];
        wrappedContainer[0] = 77;
        encrypted.EncryptedKey.CopyTo(wrappedContainer, 1);
        wrappedContainer[^1] = 66;
        CoseEcdhResult decrypted = CoseEcdhAlgorithms.Decrypt(
            wrappedContainer.AsSpan(1, encrypted.EncryptedKey.Length),
            recipientPrivate,
            encrypted.SenderPublicKey,
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            null,
            parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(decrypted, container[1..^1]);
            Assert.That(container[0], Is.EqualTo(99));
            Assert.That(container[^1], Is.EqualTo(88));
            Assert.That(wrappedContainer[0], Is.EqualTo(77));
            Assert.That(wrappedContainer[^1], Is.EqualTo(66));
        });
        encrypted.SenderPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void TamperedWrappedKeyReturnsUniformFailure()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        TokenKey recipientPrivate = PrivateKey(recipientNative);
        TokenKey recipientPublic = PublicKey(recipientPublicNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128KeyWrap,
            128,
            "");
        CoseEcdhResult encrypted = CoseEcdhAlgorithms.Wrap(
            Sequence(16, 1),
            recipientPublic,
            null,
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            null,
            parameters);
        byte[] tampered = encrypted.EncryptedKey.ToArray();
        tampered[0] ^= 1;

        CoseEcdhResult result = CoseEcdhAlgorithms.Decrypt(
            tampered,
            recipientPrivate,
            encrypted.SenderPublicKey,
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            null,
            parameters);

        AssertFailure(result, "unwrap_failed");
        encrypted.SenderPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void ContextAndRecipientCiphertextAreValidated()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        TokenKey privateKey = PrivateKey(native);
        TokenKey publicKey = PublicKey(publicNative);
        CoseKdfContextParameters direct = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        CoseKdfContextParameters wrongWrapAlgorithm = Context(
            CoseAlgorithms.Aes256KeyWrap,
            128,
            "");
        CoseKdfContextParameters wrongWrapSize = Context(
            CoseAlgorithms.Aes128KeyWrap,
            192,
            "");

        Assert.Multiple(() =>
        {
            AssertFailure(CoseEcdhAlgorithms.Decrypt(
                new byte[] { 1 }, privateKey, publicKey,
                CoseAlgorithms.EcdhEsHkdfSha256, null, direct),
                "invalid_encrypted_key");
            AssertFailure(CoseEcdhAlgorithms.Wrap(
                Sequence(16, 1), publicKey, null,
                CoseAlgorithms.EcdhEsAes128KeyWrap, null,
                wrongWrapAlgorithm),
                "invalid_cose_kdf_context");
            AssertFailure(CoseEcdhAlgorithms.Wrap(
                Sequence(16, 1), publicKey, null,
                CoseAlgorithms.EcdhEsAes128KeyWrap, null,
                wrongWrapSize),
                "invalid_cose_kdf_context");
            AssertFailure(CoseEcdhAlgorithms.Wrap(
                Sequence(15, 1), publicKey, null,
                CoseAlgorithms.EcdhEsAes128KeyWrap, null,
                Context(CoseAlgorithms.Aes128KeyWrap, 128, "")),
                "invalid_key_size");
        });
    }

    [Test]
    public void KeyMetadataAndMaterialAreValidated()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey recipient = PublicKey(recipientPublicNative);
        TokenKey sender = PrivateKey(senderNative);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        recipient.CoseKeyOperations = new int[] { CoseKeyLabels.OperationDeriveKey };
        CoseEcdhResult publicOperations = CoseEcdhAlgorithms.Derive(
            recipient, null, CoseAlgorithms.EcdhEsHkdfSha256,
            null, parameters);
        recipient.CoseKeyOperations = Array.Empty<int>();
        sender.KeyOperations = new string[] { TokenKeyOperations.Sign };
        CoseEcdhResult privateOperations = CoseEcdhAlgorithms.Derive(
            recipient, sender, CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 1 }, parameters);
        recipient.Use = TokenKeyUses.Signature;
        CoseEcdhResult wrongUse = CoseEcdhAlgorithms.Derive(
            recipient, null, CoseAlgorithms.EcdhEsHkdfSha256,
            null, parameters);
        TokenKey rawOkp = new TokenKey();
        rawOkp.KeyType = TokenKeyTypes.OctetKeyPair;
        rawOkp.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        rawOkp.OkpPublicKey = Sequence(32, 1);
        CoseEcdhResult unsupported = CoseEcdhAlgorithms.Derive(
            rawOkp, null, CoseAlgorithms.EcdhEsHkdfSha256,
            null, parameters);

        Assert.Multiple(() =>
        {
            AssertFailure(publicOperations, "key_operation_not_permitted");
            AssertFailure(privateOperations, "key_operation_not_permitted");
            AssertFailure(wrongUse, "key_use_algorithm_mismatch");
            AssertFailure(unsupported, "invalid_key_type");
        });
    }

    [Test]
    public void MismatchedCurvesFailWithoutProviderExceptions()
    {
        using ECDsa recipientNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey recipient = PublicKey(recipientPublicNative);
        TokenKey sender = PrivateKey(senderNative);

        CoseEcdhResult result = CoseEcdhAlgorithms.Derive(
            recipient,
            sender,
            CoseAlgorithms.EcdhSsHkdfSha256,
            new byte[] { 1 },
            Context(CoseAlgorithms.Aes128Gcm, 128, ""));

        AssertFailure(result, "curve_mismatch");
    }

    [TestCase(CoseAlgorithms.EcdhEsHkdfSha256)]
    [TestCase(CoseAlgorithms.EcdhEsHkdfSha512)]
    [TestCase(CoseAlgorithms.EcdhSsHkdfSha256)]
    [TestCase(CoseAlgorithms.EcdhSsHkdfSha512)]
    [TestCase(CoseAlgorithms.EcdhEsAes128KeyWrap)]
    [TestCase(CoseAlgorithms.EcdhEsAes192KeyWrap)]
    [TestCase(CoseAlgorithms.EcdhEsAes256KeyWrap)]
    [TestCase(CoseAlgorithms.EcdhSsAes128KeyWrap)]
    [TestCase(CoseAlgorithms.EcdhSsAes192KeyWrap)]
    [TestCase(CoseAlgorithms.EcdhSsAes256KeyWrap)]
    public void EcdsaImporterAcceptsEveryEcdhLabel(int algorithm)
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        TokenKeyResult result = EcdsaKeyImporter.ImportCose(
            native,
            algorithm,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.CoseAlgorithm, Is.EqualTo(algorithm));
        });
    }

    [Test]
    public void NullArgumentsThrowAndDefaultResultHasNoOutputs()
    {
        CoseEcdhResult result = new CoseEcdhResult();

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseEcdhAlgorithms.Derive(
                null, null, CoseAlgorithms.EcdhEsHkdfSha256,
                null, new CoseKdfContextParameters(1, 128)),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEcdhAlgorithms.Wrap(
                (byte[])null, new TokenKey(), null,
                CoseAlgorithms.EcdhEsAes128KeyWrap, null,
                new CoseKdfContextParameters(-3, 128)),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEcdhAlgorithms.Decrypt(
                (byte[])null, new TokenKey(), new TokenKey(),
                CoseAlgorithms.EcdhEsHkdfSha256, null,
                new CoseKdfContextParameters(1, 128)),
                Throws.ArgumentNullException);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentKey, Is.Null);
            Assert.That(result.EncryptedKey, Is.Null);
            Assert.That(result.SenderPublicKey, Is.Null);
        });
    }

    static ECDsa CreateCurve(int size)
    {
        if (size == 384)
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        if (size == 521)
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP521);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    static ECDsa PublicPart(ECDsa source)
    {
        return ECDsa.Create(source.ExportParameters(false));
    }

    static TokenKey PrivateKey(ECDsa native)
    {
        TokenKey key = EcdsaKeyImporter.Import(native, true).Key;
        key.Use = TokenKeyUses.Encryption;
        key.KeyOperations = new string[] { TokenKeyOperations.DeriveKey };
        return key;
    }

    static TokenKey PublicKey(ECDsa native)
    {
        TokenKey key = EcdsaKeyImporter.Import(native, false).Key;
        key.Use = TokenKeyUses.Encryption;
        return key;
    }

    static CoseKdfContextParameters Context(
        int algorithm,
        ulong keySize,
        string protectedHex)
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(algorithm, keySize);
        parameters.ProtectedParameters = protectedHex.Length == 0
            ? Array.Empty<byte>()
            : Convert.FromHexString(protectedHex);
        return parameters;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static bool IsStatic(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] value = new byte[length];
        int index = 0;
        while (index < length)
        {
            value[index] = (byte)(start + index);
            index += 1;
        }

        return value;
    }

    static void AssertSuccess(CoseEcdhResult result, byte[] expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.ContentKey, Is.EqualTo(expected));
        Assert.That(result.EncryptedKey, Is.Not.Null);
    }

    static void AssertFailure(CoseEcdhResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.ContentKey, Is.Null);
        Assert.That(result.EncryptedKey, Is.Null);
        Assert.That(result.SenderPublicKey, Is.Null);
    }
}
