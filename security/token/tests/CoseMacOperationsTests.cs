using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseMacOperationsTests
{
    [Test]
    public void DetachedPayloadAndExternalAuthenticatedDataAreBound()
    {
        byte[] payload = Encoding.UTF8.GetBytes("detached mac payload");
        byte[] externalData = Encoding.UTF8.GetBytes("mac aad");
        TokenKey key = Symmetric(Sequence(32, 3), CoseAlgorithms.Direct);
        TokenPolicy policy = Policy("HMAC 256/256", "direct");

        TokenBytesResult created = CoseMacOperations.CreateDetached(
            payload,
            externalData,
            MacHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Direct, 1)
            },
            new TokenKey[] { key },
            null,
            true,
            policy);
        CoseMacResult parsed = CoseMac.Read(created.Bytes);
        CoseMacResult verified = CoseMacOperations.VerifyDetached(
            created.Bytes,
            payload,
            externalData,
            0,
            key,
            null,
            policy);
        CoseMacResult wrongData = CoseMacOperations.VerifyDetached(
            created.Bytes,
            payload,
            new byte[] { 8 },
            0,
            key,
            null,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.DetachedPayload, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            AssertVerified(verified, payload);
            AssertFailure(wrongData, "mac_mismatch");
        });
    }

    [Test]
    public void MultipleAesKeyWrapRecipientsVerifyIndividuallyAndTogether()
    {
        byte[] payload = Encoding.UTF8.GetBytes(
            "one authenticated payload for three recipients");
        int[] algorithms = new int[]
        {
            CoseAlgorithms.Aes128KeyWrap,
            CoseAlgorithms.Aes192KeyWrap,
            CoseAlgorithms.Aes256KeyWrap
        };
        TokenKey[] keys = new TokenKey[]
        {
            Symmetric(Sequence(16, 1), algorithms[0]),
            Symmetric(Sequence(24, 21), algorithms[1]),
            Symmetric(Sequence(32, 51), algorithms[2])
        };
        TokenPolicy policy = Policy(
            "HMAC 256/256", "A128KW", "A192KW", "A256KW");

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeaders(algorithms),
            keys,
            policy);
        CoseMacResult parsed = CoseMac.Read(created.Bytes);
        CoseMacResult selected = CoseMacOperations.Verify(
            created.Bytes,
            1,
            keys[1],
            policy);
        CoseMacResult all = CoseMacOperations.Verify(
            created.Bytes,
            keys,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Tag, Has.Length.EqualTo(32));
            Assert.That(parsed.Message.TagVerified, Is.False);
            Assert.That(parsed.Message.Recipients, Has.Length.EqualTo(3));
            Assert.That(parsed.Message.Recipients.All(
                value => value.EncryptedKey.Length == 40), Is.True);
            AssertVerified(selected, payload);
            Assert.That(selected.Message.Recipients[0].Decrypted, Is.False);
            Assert.That(selected.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(selected.Message.Recipients[2].Decrypted, Is.False);
            AssertVerified(all, payload);
            Assert.That(all.Message.Recipients.All(
                value => value.Decrypted), Is.True);
        });
    }

    [TestCase(CoseAlgorithms.HmacSha256Truncated64, 32, 8)]
    [TestCase(CoseAlgorithms.HmacSha256, 32, 32)]
    [TestCase(CoseAlgorithms.HmacSha384, 48, 48)]
    [TestCase(CoseAlgorithms.HmacSha512, 64, 64)]
    public void EveryHmacAlgorithmRoundTripsWithDirectDistribution(
        int algorithm,
        int keySize,
        int tagSize)
    {
        TokenKey key = Symmetric(
            Sequence(keySize, 4),
            CoseAlgorithms.Direct);
        TokenPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(algorithm),
            "direct");
        byte[] payload = Sequence(129, 8);

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(algorithm),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Direct, 1)
            },
            new TokenKey[] { key },
            false,
            policy);
        CoseMacResult parsed = CoseMac.Read(created.Bytes);
        CoseMacResult verified = CoseMacOperations.Verify(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.Tagged, Is.False);
            Assert.That(parsed.Message.Tag, Has.Length.EqualTo(tagSize));
            Assert.That(parsed.Message.Recipients[0].EncryptedKey, Is.Empty);
            AssertVerified(verified, payload);
        });
    }

    [Test]
    public void DirectRecipientMustBeAlone()
    {
        TokenPolicy policy = Policy(
            "HMAC 256/256", "direct", "A128KW");
        TokenBytesResult result = CoseMacOperations.Create(
            new byte[] { 1 },
            MacHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Direct, 1),
                RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 2)
            },
            new TokenKey[]
            {
                Symmetric(Sequence(32, 1), CoseAlgorithms.Direct),
                Symmetric(Sequence(16, 40),
                    CoseAlgorithms.Aes128KeyWrap)
            },
            policy);

        AssertBytesFailure(
            result,
            "direct_recipient_must_be_only_recipient");
    }

    [Test]
    public void DirectHkdfUsesRecipientContext()
    {
        int recipientAlgorithm = CoseAlgorithms.DirectHkdfSha256;
        TokenKey key = Symmetric(Sequence(32, 60), recipientAlgorithm);
        key.KeyOperations = new string[] { TokenKeyOperations.DeriveKey };
        CoseHeader recipient = RecipientHeader(recipientAlgorithm, 1);
        recipient.SetUnprotected(
            CoseHeaderLabels.Salt,
            Bytes(Sequence(16, 3)));
        recipient.SetUnprotected(
            CoseHeaderLabels.PartyVIdentity,
            Bytes(Encoding.ASCII.GetBytes("recipient")));
        TokenPolicy policy = Policy(
            "HMAC 256/256",
            "direct+HKDF-SHA-256");
        byte[] payload = Sequence(90, 1);

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[] { recipient },
            new TokenKey[] { key },
            policy);
        CoseMacResult verified = CoseMacOperations.Verify(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertVerified(verified, payload);
        });
    }

    [Test]
    public void EphemeralEcdhAndAesKeyWrapRecipientsInteroperate()
    {
        using ECDsa recipientNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        int ecdhAlgorithm = CoseAlgorithms.EcdhEsAes128KeyWrap;
        int aesAlgorithm = CoseAlgorithms.Aes128KeyWrap;
        TokenKey recipientPrivate = EcPrivate(recipientNative);
        TokenKey recipientPublic = EcPublic(recipientPublicNative);
        TokenKey aesKey = Symmetric(Sequence(16, 90), aesAlgorithm);
        TokenPolicy policy = Policy(
            "HMAC 256/256", "ECDH-ES + A128KW", "A128KW");
        byte[] payload = Encoding.ASCII.GetBytes("ecdh recipient");

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeaders(new int[] { ecdhAlgorithm, aesAlgorithm }),
            new TokenKey[] { recipientPublic, aesKey },
            policy);
        CoseMacResult parsed = CoseMac.Read(created.Bytes);
        CoseMacResult ecdh = CoseMacOperations.Verify(
            created.Bytes,
            0,
            recipientPrivate,
            policy);
        CoseMacResult aes = CoseMacOperations.Verify(
            created.Bytes,
            1,
            aesKey,
            policy);
        CoseMacResult all = CoseMacOperations.Verify(
            created.Bytes,
            new TokenKey[] { recipientPrivate, aesKey },
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.Recipients[0].Header.Get(
                CoseHeaderLabels.EphemeralKey).Kind,
                Is.EqualTo(TokenCborValueKind.Map));
            Assert.That(parsed.Message.Recipients[0].EncryptedKey,
                Has.Length.EqualTo(40));
            AssertVerified(ecdh, payload);
            AssertVerified(aes, payload);
            AssertVerified(all, payload);
        });
    }

    [Test]
    public void StaticEcdhKeyWrapUsesExplicitSenderKeys()
    {
        using ECDsa recipientNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        using ECDsa senderNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa senderPublicNative = PublicPart(senderNative);
        int algorithm = CoseAlgorithms.EcdhSsAes128KeyWrap;
        CoseHeader recipient = RecipientHeader(algorithm, 1);
        recipient.SetUnprotected(
            CoseHeaderLabels.Salt,
            Bytes(Sequence(16, 70)));
        TokenPolicy policy = Policy(
            "HMAC 256/256", "ECDH-SS + A128KW");
        byte[] payload = new byte[] { 8, 6, 7, 5, 3, 0, 9 };

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(CoseAlgorithms.HmacSha256),
            new CoseHeader[] { recipient },
            new TokenKey[] { EcPublic(recipientPublicNative) },
            new TokenKey[] { EcPrivate(senderNative) },
            true,
            policy);
        CoseMacResult missingSender = CoseMacOperations.Verify(
            created.Bytes,
            0,
            EcPrivate(recipientNative),
            policy);
        CoseMacResult verified = CoseMacOperations.Verify(
            created.Bytes,
            0,
            EcPrivate(recipientNative),
            EcPublic(senderPublicNative),
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertFailure(missingSender, "missing_sender_key");
            AssertVerified(verified, payload);
            Assert.That(verified.Message.Recipients[0].Header.Get(
                CoseHeaderLabels.EphemeralKey), Is.Null);
        });
    }

    [Test]
    public void GeneratedMessageUsesExactMacStructure()
    {
        int macAlgorithm = CoseAlgorithms.HmacSha256;
        int recipientAlgorithm = CoseAlgorithms.Aes128KeyWrap;
        TokenKey recipientKey = Symmetric(
            Sequence(16, 20),
            recipientAlgorithm);
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        byte[] payload = Encoding.ASCII.GetBytes("authenticated bytes");

        TokenBytesResult created = CoseMacOperations.Create(
            payload,
            MacHeader(macAlgorithm),
            new CoseHeader[]
            {
                RecipientHeader(recipientAlgorithm, 1)
            },
            new TokenKey[] { recipientKey },
            policy);
        CoseMac message = CoseMac.Read(created.Bytes).Message;
        TokenBytesResult unwrapped = CoseAesKeyWrap.Unwrap(
            message.Recipients[0].EncryptedKey,
            recipientKey,
            recipientAlgorithm);
        TokenKey macKey = Symmetric(unwrapped.Bytes, macAlgorithm);
        TokenBytesResult structure = CoseMacStructure.CreateMac(
            message.Header.ProtectedBytes,
            payload);
        TokenResult verified = CoseHmacAlgorithms.Verify(
            structure.Bytes,
            message.Tag,
            macKey,
            macAlgorithm);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(verified.Success, Is.True);
        });
    }

    [Test]
    public void PayloadTagAndProtectedHeaderTamperingFailClosed()
    {
        int recipientAlgorithm = CoseAlgorithms.Aes128KeyWrap;
        TokenKey key = Symmetric(Sequence(16, 30), recipientAlgorithm);
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        TokenBytesResult created = Create(key, policy);

        CoseMac payload = CoseMac.Read(created.Bytes).Message;
        payload.Payload[0] ^= 1;
        CoseMac tag = CoseMac.Read(created.Bytes).Message;
        tag.Tag[^1] ^= 1;
        CoseMac header = CoseMac.Read(created.Bytes).Message;
        header.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cwt"));

        CoseMacResult payloadResult = CoseMacOperations.Verify(
            payload.Write().Bytes, 0, key, policy);
        CoseMacResult tagResult = CoseMacOperations.Verify(
            tag.Write().Bytes, 0, key, policy);
        CoseMacResult headerResult = CoseMacOperations.Verify(
            header.Write().Bytes, 0, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(payloadResult, "mac_mismatch");
            AssertFailure(tagResult, "mac_mismatch");
            AssertFailure(headerResult, "mac_mismatch");
        });
    }

    [Test]
    public void MacAndRecipientAlgorithmsAreValidated()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        CoseHeader missingMac = new CoseHeader();
        CoseHeader unprotectedMac = new CoseHeader();
        unprotectedMac.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.HmacSha256));
        CoseHeader invalidMac = MacHeader(CoseAlgorithms.Aes128Gcm);
        CoseHeader missingRecipient = new CoseHeader();
        CoseHeader invalidRecipient = RecipientHeader(
            CoseAlgorithms.HmacSha256,
            1);

        TokenBytesResult missingMacResult = Create(
            missingMac,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("HMAC 256/256", "A128KW"));
        TokenBytesResult unprotectedMacResult = Create(
            unprotectedMac,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("HMAC 256/256", "A128KW"));
        TokenBytesResult invalidMacResult = Create(
            invalidMac,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("A128GCM", "A128KW"));
        TokenBytesResult missingRecipientResult = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            missingRecipient,
            key,
            Policy("HMAC 256/256", "A128KW"));
        TokenBytesResult invalidRecipientResult = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            invalidRecipient,
            key,
            Policy("HMAC 256/256"));
        TokenBytesResult denied = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("HMAC 384/384", "A128KW"));

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingMacResult,
                "missing_cose_algorithm");
            AssertBytesFailure(unprotectedMacResult,
                "cose_algorithm_not_protected");
            AssertBytesFailure(invalidMacResult, "invalid_algorithm");
            AssertBytesFailure(missingRecipientResult,
                "missing_cose_algorithm");
            AssertBytesFailure(invalidRecipientResult,
                "invalid_algorithm");
            AssertBytesFailure(denied, "algorithm_not_allowed");
        });
    }

    [Test]
    public void RecipientSelectionCountsAndMessageModesAreValidated()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        TokenBytesResult created = Create(key, policy);
        CoseMac detached = CoseMac.Read(created.Bytes).Message;
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseMac nested = CoseMac.Read(created.Bytes).Message;
        nested.Recipients[0].Recipients =
            new CoseRecipient[] { new CoseRecipient() };
        CoseMac absentKey = CoseMac.Read(created.Bytes).Message;
        absentKey.Recipients[0].EncryptedKeyAbsent = true;
        absentKey.Recipients[0].EncryptedKey = null;

        CoseMacResult low = CoseMacOperations.Verify(
            created.Bytes, -1, key, policy);
        CoseMacResult high = CoseMacOperations.Verify(
            created.Bytes, 1, key, policy);
        CoseMacResult count = CoseMacOperations.Verify(
            created.Bytes, System.Array.Empty<TokenKey>(), policy);
        CoseMacResult missing = CoseMacOperations.Verify(
            created.Bytes, new TokenKey[] { null }, policy);
        CoseMacResult detachedResult = CoseMacOperations.Verify(
            detached.Write().Bytes, 0, key, policy);
        CoseMacResult nestedResult = CoseMacOperations.Verify(
            nested.Write().Bytes, 0, key, policy);
        CoseMacResult absentKeyResult = CoseMacOperations.Verify(
            absentKey.Write().Bytes, 0, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(low, "recipient_index_out_of_range");
            AssertFailure(high, "recipient_index_out_of_range");
            AssertFailure(count, "recipient_count_mismatch");
            AssertFailure(missing, "invalid_key");
            AssertFailure(detachedResult, "missing_payload");
            AssertFailure(nestedResult,
                "nested_recipient_processing_not_supported");
            AssertFailure(absentKeyResult, "invalid_encrypted_key");
        });
    }

    [Test]
    public void WrongKeysAndRecipientKeyOperationsAreEnforced()
    {
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenKey deniedCreate = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        deniedCreate.KeyOperations =
            new string[] { TokenKeyOperations.UnwrapKey };
        TokenBytesResult deniedCreateResult = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            deniedCreate,
            policy);
        TokenBytesResult created = Create(key, policy);
        TokenKey wrong = Symmetric(
            Sequence(16, 90),
            CoseAlgorithms.Aes128KeyWrap);
        CoseMacResult wrongResult = CoseMacOperations.Verify(
            created.Bytes,
            0,
            wrong,
            policy);
        key.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationWrapKey };
        CoseMacResult deniedVerify = CoseMacOperations.Verify(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(deniedCreateResult,
                "key_operation_not_permitted");
            AssertFailure(wrongResult, "unwrap_failed");
            AssertFailure(deniedVerify,
                "key_operation_not_permitted");
        });
    }

    [Test]
    public void CriticalHeadersSizeAndPolicyLimitsAreEnforced()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");
        CoseHeader critical = MacHeader(CoseAlgorithms.HmacSha256);
        critical.SetProtectedByTextLabel("private", Boolean(true));
        critical.SetProtected(
            CoseHeaderLabels.Critical,
            Array(Text("private")));
        TokenBytesResult criticalResult = Create(
            critical,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        TokenBytesResult created = Create(key, policy);
        TokenPolicy small = Policy("HMAC 256/256", "A128KW");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        TokenPolicy invalid = Policy("HMAC 256/256", "A128KW");
        invalid.AllowedAlgorithms = null;

        TokenBytesResult smallCreate = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            small);
        CoseMacResult smallVerify = CoseMacOperations.Verify(
            created.Bytes, 0, key, small);
        TokenBytesResult invalidCreate = Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            invalid);
        CoseMacResult invalidVerify = CoseMacOperations.Verify(
            created.Bytes, 0, key, invalid);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(criticalResult,
                "unknown_cose_critical_header");
            AssertBytesFailure(smallCreate, "buffer_limit_exceeded");
            AssertFailure(smallVerify, "cbor_size_exceeded");
            AssertBytesFailure(invalidCreate, "invalid_policy");
            AssertFailure(invalidVerify, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseHeader header = MacHeader(CoseAlgorithms.HmacSha256);
        CoseHeader[] recipients = new CoseHeader[]
        {
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1)
        };
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenKey[] keys = new TokenKey[] { key };
        TokenPolicy policy = Policy("HMAC 256/256", "A128KW");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseMacOperations.Create(
                null, header, recipients, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Create(
                new byte[0], null, recipients, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Create(
                new byte[0], header, null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Create(
                new byte[0], header, recipients, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Create(
                new byte[0], header, recipients, keys, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Verify(
                null, 0, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Verify(
                new byte[0], 0, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Verify(
                new byte[0], 0, key, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Verify(
                null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMacOperations.Verify(
                new byte[0], (TokenKey[])null, policy),
                Throws.ArgumentNullException);
        });
    }

    static TokenBytesResult Create(TokenKey key, TokenPolicy policy)
    {
        return Create(
            MacHeader(CoseAlgorithms.HmacSha256),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
    }

    static TokenBytesResult Create(
        CoseHeader mac,
        CoseHeader recipient,
        TokenKey key,
        TokenPolicy policy)
    {
        return CoseMacOperations.Create(
            new byte[] { 1, 2, 3 },
            mac,
            new CoseHeader[] { recipient },
            new TokenKey[] { key },
            policy);
    }

    static CoseHeader MacHeader(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        return header;
    }

    static CoseHeader[] RecipientHeaders(int[] algorithms)
    {
        CoseHeader[] result = new CoseHeader[algorithms.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = RecipientHeader(
                algorithms[index],
                (byte)(index + 1));
            index += 1;
        }

        return result;
    }

    static CoseHeader RecipientHeader(int algorithm, byte keyId)
    {
        CoseHeader header = new CoseHeader();
        header.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { keyId }));
        return header;
    }

    static TokenKey Symmetric(byte[] material, int algorithm)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            material,
            algorithm).Key;
        key.Use = algorithm == CoseAlgorithms.HmacSha256
            ? TokenKeyUses.Signature
            : TokenKeyUses.Encryption;
        return key;
    }

    static TokenKey EcPrivate(ECDsa native)
    {
        TokenKey key = EcdsaKeyImporter.Import(native, true).Key;
        key.Use = TokenKeyUses.Encryption;
        key.KeyOperations = new string[] { TokenKeyOperations.DeriveKey };
        return key;
    }

    static TokenKey EcPublic(ECDsa native)
    {
        TokenKey key = EcdsaKeyImporter.Import(native, false).Key;
        key.Use = TokenKeyUses.Encryption;
        return key;
    }

    static ECDsa PublicPart(ECDsa source)
    {
        return ECDsa.Create(source.ExportParameters(false));
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
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

    static void AssertVerified(CoseMacResult result, byte[] payload)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Message.TagVerified, Is.True);
        Assert.That(result.Message.Payload, Is.EqualTo(payload));
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.Not.Null);
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }

    static void AssertFailure(CoseMacResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
