using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseEncryptOperationsTests
{
    [Test]
    public void DetachedCiphertextAndExternalAuthenticatedDataAreBound()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("detached recipient ciphertext");
        byte[] externalData = Encoding.UTF8.GetBytes("encrypt aad");
        TokenKey key = Symmetric(Sequence(16, 4), CoseAlgorithms.Direct);
        TokenPolicy policy = Policy("A128GCM", "direct");

        CoseDetachedEncryptionResult created =
            CoseEncryptOperations.CreateDetached(
                plaintext,
                externalData,
                ContentHeader(),
                new CoseHeader[]
                {
                    RecipientHeader(CoseAlgorithms.Direct, 1)
                },
                new TokenKey[] { key },
                null,
                true,
                policy);
        CoseEncryptResult parsed = CoseEncrypt.Read(created.Bytes);
        CoseEncryptResult decrypted = CoseEncryptOperations.DecryptDetached(
            created.Bytes,
            created.Ciphertext,
            externalData,
            0,
            key,
            null,
            policy);
        CoseEncryptResult wrongData = CoseEncryptOperations.DecryptDetached(
            created.Bytes,
            created.Ciphertext,
            new byte[] { 6 },
            0,
            key,
            null,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Bytes, Is.Not.Null);
            Assert.That(created.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            Assert.That(parsed.Message.DetachedCiphertext, Is.True);
            Assert.That(parsed.Message.Ciphertext, Is.Null);
            AssertVerified(decrypted, plaintext);
            Assert.That(decrypted.Message.Ciphertext,
                Is.EqualTo(created.Ciphertext));
            AssertFailure(wrongData, "decryption_failed");
        });
    }

    [Test]
    public void MultipleAesKeyWrapRecipientsDecryptIndividuallyAndTogether()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "one encrypted payload for three recipients");
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
            "A128GCM", "A128KW", "A192KW", "A256KW");

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            RecipientHeaders(algorithms),
            keys,
            policy);
        CoseEncryptResult parsed = CoseEncrypt.Read(created.Bytes);
        CoseEncryptResult selected = CoseEncryptOperations.Decrypt(
            created.Bytes,
            1,
            keys[1],
            policy);
        CoseEncryptResult all = CoseEncryptOperations.Decrypt(
            created.Bytes,
            keys,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.PlaintextAvailable, Is.False);
            Assert.That(parsed.Message.AuthenticationTagVerified, Is.False);
            Assert.That(parsed.Message.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            Assert.That(parsed.Message.Header.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(parsed.Message.Recipients, Has.Length.EqualTo(3));
            Assert.That(parsed.Message.Recipients.All(
                value => value.EncryptedKey.Length == 24), Is.True);
            AssertVerified(selected, plaintext);
            Assert.That(selected.Message.Recipients[0].Decrypted, Is.False);
            Assert.That(selected.Message.Recipients[1].Decrypted, Is.True);
            Assert.That(selected.Message.Recipients[2].Decrypted, Is.False);
            AssertVerified(all, plaintext);
            Assert.That(all.Message.Recipients.All(
                value => value.Decrypted), Is.True);
        });
    }

    [Test]
    public void DirectRecipientRoundTripsAndMustBeAlone()
    {
        byte[] plaintext = new byte[] { 1, 2, 3 };
        TokenKey directKey = Symmetric(
            Sequence(16, 4),
            CoseAlgorithms.Direct);
        TokenPolicy policy = Policy("A128GCM", "direct");
        CoseHeader directHeader = RecipientHeader(
            CoseAlgorithms.Direct,
            1);

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[] { directHeader },
            new TokenKey[] { directKey },
            policy);
        CoseEncryptResult parsed = CoseEncrypt.Read(created.Bytes);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            directKey,
            policy);
        TokenBytesResult multiple = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Direct, 1),
                RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 2)
            },
            new TokenKey[]
            {
                directKey,
                Symmetric(Sequence(16, 40),
                    CoseAlgorithms.Aes128KeyWrap)
            },
            Policy("A128GCM", "direct", "A128KW"));

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.Recipients[0].EncryptedKey, Is.Empty);
            AssertVerified(decrypted, plaintext);
            AssertBytesFailure(
                multiple,
                "direct_recipient_must_be_only_recipient");
        });
    }

    [Test]
    public void DirectHkdfUsesRecipientContext()
    {
        byte[] plaintext = Sequence(90, 1);
        int algorithm = CoseAlgorithms.DirectHkdfSha256;
        TokenKey key = Symmetric(Sequence(32, 60), algorithm);
        key.KeyOperations = new string[] { TokenKeyOperations.DeriveKey };
        CoseHeader recipient = RecipientHeader(algorithm, 1);
        recipient.SetUnprotected(
            CoseHeaderLabels.Salt,
            Bytes(Sequence(16, 3)));
        recipient.SetUnprotected(
            CoseHeaderLabels.PartyVIdentity,
            Bytes(Encoding.ASCII.GetBytes("recipient")));
        TokenPolicy policy = Policy(
            "A128GCM",
            "direct+HKDF-SHA-256");

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[] { recipient },
            new TokenKey[] { key },
            policy);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertVerified(decrypted, plaintext);
        });
    }

    [Test]
    public void EphemeralEcdhKeyWrapInteroperatesWithAesKeyWrapRecipient()
    {
        using ECDsa recipientNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        int ecdhAlgorithm = CoseAlgorithms.EcdhEsAes128KeyWrap;
        int aesAlgorithm = CoseAlgorithms.Aes128KeyWrap;
        TokenKey recipientPrivate = EcPrivate(recipientNative);
        TokenKey recipientPublic = EcPublic(recipientPublicNative);
        TokenKey aesKey = Symmetric(Sequence(16, 90), aesAlgorithm);
        byte[] plaintext = Encoding.ASCII.GetBytes("ecdh recipient");
        TokenPolicy policy = Policy(
            "A128GCM", "ECDH-ES + A128KW", "A128KW");

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            RecipientHeaders(new int[] { ecdhAlgorithm, aesAlgorithm }),
            new TokenKey[] { recipientPublic, aesKey },
            policy);
        CoseEncryptResult parsed = CoseEncrypt.Read(created.Bytes);
        CoseEncryptResult ecdh = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            recipientPrivate,
            policy);
        CoseEncryptResult aes = CoseEncryptOperations.Decrypt(
            created.Bytes,
            1,
            aesKey,
            policy);
        CoseEncryptResult all = CoseEncryptOperations.Decrypt(
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
                Has.Length.EqualTo(24));
            AssertVerified(ecdh, plaintext);
            AssertVerified(aes, plaintext);
            AssertVerified(all, plaintext);
        });
    }

    [Test]
    public void EphemeralDirectEcdhRoundTripsAsOnlyRecipient()
    {
        using ECDsa recipientNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = PublicPart(recipientNative);
        int algorithm = CoseAlgorithms.EcdhEsHkdfSha256;
        byte[] plaintext = Sequence(33, 5);
        TokenPolicy policy = Policy(
            "A128GCM", "ECDH-ES + HKDF-256");

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[] { RecipientHeader(algorithm, 1) },
            new TokenKey[] { EcPublic(recipientPublicNative) },
            policy);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            EcPrivate(recipientNative),
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertVerified(decrypted, plaintext);
            Assert.That(decrypted.Message.Recipients[0].EncryptedKey,
                Is.Empty);
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
            "A128GCM", "ECDH-SS + A128KW");
        byte[] plaintext = new byte[] { 8, 6, 7, 5, 3, 0, 9 };

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[] { recipient },
            new TokenKey[] { EcPublic(recipientPublicNative) },
            new TokenKey[] { EcPrivate(senderNative) },
            true,
            policy);
        CoseEncryptResult missingSender = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            EcPrivate(recipientNative),
            policy);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            EcPrivate(recipientNative),
            EcPublic(senderPublicNative),
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertFailure(missingSender, "missing_sender_key");
            AssertVerified(decrypted, plaintext);
            Assert.That(decrypted.Message.Recipients[0].Header.Get(
                CoseHeaderLabels.EphemeralKey), Is.Null);
        });
    }

    [Test]
    public void CiphertextProtectedHeaderAndEncryptedKeyTamperingFails()
    {
        byte[] plaintext = Sequence(24, 1);
        TokenKey key = Symmetric(
            Sequence(16, 40),
            CoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy("A128GCM", "A128KW");
        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            ContentHeader(),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1)
            },
            new TokenKey[] { key },
            policy);
        CoseEncrypt ciphertext = CoseEncrypt.Read(created.Bytes).Message;
        ciphertext.Ciphertext[0] ^= 1;
        CoseEncrypt protectedHeader =
            CoseEncrypt.Read(created.Bytes).Message;
        protectedHeader.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("changed"));
        CoseEncrypt encryptedKey = CoseEncrypt.Read(created.Bytes).Message;
        encryptedKey.Recipients[0].EncryptedKey[0] ^= 1;

        CoseEncryptResult ciphertextResult =
            CoseEncryptOperations.Decrypt(
                ciphertext.Write().Bytes, 0, key, policy);
        CoseEncryptResult headerResult = CoseEncryptOperations.Decrypt(
            protectedHeader.Write().Bytes, 0, key, policy);
        CoseEncryptResult keyResult = CoseEncryptOperations.Decrypt(
            encryptedKey.Write().Bytes, 0, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(ciphertextResult, "decryption_failed");
            AssertFailure(headerResult, "decryption_failed");
            AssertFailure(keyResult, "unwrap_failed");
        });
    }

    [Test]
    public void ContentAndRecipientAlgorithmsAndPolicyAreEnforced()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        CoseHeader missingContent = new CoseHeader();
        CoseHeader unprotectedContent = new CoseHeader();
        unprotectedContent.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Aes128Gcm));
        CoseHeader missingRecipient = new CoseHeader();
        CoseHeader invalidRecipient = RecipientHeader(
            CoseAlgorithms.HmacSha256,
            1);

        TokenBytesResult missingContentResult = Create(
            missingContent,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("A128GCM", "A128KW"));
        TokenBytesResult unprotectedContentResult = Create(
            unprotectedContent,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("A128GCM", "A128KW"));
        TokenBytesResult missingRecipientResult = Create(
            ContentHeader(),
            missingRecipient,
            key,
            Policy("A128GCM", "A128KW"));
        TokenBytesResult invalidRecipientResult = Create(
            ContentHeader(),
            invalidRecipient,
            key,
            Policy("A128GCM", "HMAC 256/256"));
        TokenBytesResult denied = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            Policy("A256GCM", "A128KW"));

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                missingContentResult,
                "missing_cose_algorithm");
            AssertBytesFailure(
                unprotectedContentResult,
                "cose_algorithm_not_protected");
            AssertBytesFailure(
                missingRecipientResult,
                "missing_cose_algorithm");
            AssertBytesFailure(
                invalidRecipientResult,
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
        TokenPolicy policy = Policy("A128GCM", "A128KW");
        TokenBytesResult created = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        CoseEncrypt detached = CoseEncrypt.Read(created.Bytes).Message;
        detached.DetachedCiphertext = true;
        detached.Ciphertext = null;
        CoseEncrypt nested = CoseEncrypt.Read(created.Bytes).Message;
        nested.Recipients[0].Recipients =
            new CoseRecipient[] { new CoseRecipient() };
        CoseEncrypt absentKey = CoseEncrypt.Read(created.Bytes).Message;
        absentKey.Recipients[0].EncryptedKeyAbsent = true;
        absentKey.Recipients[0].EncryptedKey = null;
        CoseEncrypt directMultiple = CoseEncrypt.Read(created.Bytes).Message;
        directMultiple.Recipients[0].Header.Remove(
            CoseHeaderLabels.Algorithm);
        directMultiple.Recipients[0].Header.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Direct));
        CoseRecipient secondDirect = new CoseRecipient();
        secondDirect.Header.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Direct));
        directMultiple.Recipients = new CoseRecipient[]
        {
            directMultiple.Recipients[0],
            secondDirect
        };

        CoseEncryptResult low = CoseEncryptOperations.Decrypt(
            created.Bytes, -1, key, policy);
        CoseEncryptResult high = CoseEncryptOperations.Decrypt(
            created.Bytes, 1, key, policy);
        CoseEncryptResult count = CoseEncryptOperations.Decrypt(
            created.Bytes, System.Array.Empty<TokenKey>(), policy);
        CoseEncryptResult missing = CoseEncryptOperations.Decrypt(
            created.Bytes, new TokenKey[] { null }, policy);
        CoseEncryptResult detachedResult = CoseEncryptOperations.Decrypt(
            detached.Write().Bytes, 0, key, policy);
        CoseEncryptResult nestedResult = CoseEncryptOperations.Decrypt(
            nested.Write().Bytes, 0, key, policy);
        CoseEncryptResult absentKeyResult = CoseEncryptOperations.Decrypt(
            absentKey.Write().Bytes, 0, key, policy);
        CoseEncryptResult directMultipleResult =
            CoseEncryptOperations.Decrypt(
                directMultiple.Write().Bytes,
                0,
                key,
                Policy("A128GCM", "direct"));

        Assert.Multiple(() =>
        {
            AssertFailure(low, "recipient_index_out_of_range");
            AssertFailure(high, "recipient_index_out_of_range");
            AssertFailure(count, "recipient_count_mismatch");
            AssertFailure(missing, "invalid_key");
            AssertFailure(detachedResult, "missing_ciphertext");
            AssertFailure(
                nestedResult,
                "nested_recipient_processing_not_supported");
            AssertFailure(absentKeyResult, "invalid_encrypted_key");
            AssertFailure(
                directMultipleResult,
                "direct_recipient_must_be_only_recipient");
        });
    }

    [Test]
    public void AesCcmContentRoundTripsWhenPlatformSupportIsAvailable()
    {
        if (!AesCcm.IsSupported)
        {
            Assert.Ignore("AES-CCM is unavailable on this platform.");
        }

        int contentAlgorithm = CoseAlgorithms.AesCcm16_128_128;
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        CoseHeader content = new CoseHeader();
        content.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(contentAlgorithm));
        TokenPolicy policy = Policy(
            CoseAlgorithmsOperations.GetName(contentAlgorithm),
            "A128KW");
        byte[] plaintext = Sequence(120, 9);

        TokenBytesResult created = CoseEncryptOperations.Create(
            plaintext,
            content,
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1)
            },
            new TokenKey[] { key },
            policy);
        CoseEncryptResult decrypted = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            AssertVerified(decrypted, plaintext);
            Assert.That(decrypted.Message.Header.InitializationVector,
                Has.Length.EqualTo(13));
        });
    }

    [Test]
    public void GeneratedNoncesAreFreshAndExplicitTagIsHonored()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy("A128GCM", "A128KW");

        TokenBytesResult first = CoseEncryptOperations.Create(
            System.Array.Empty<byte>(),
            ContentHeader(),
            new CoseHeader[]
            {
                RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1)
            },
            new TokenKey[] { key },
            false,
            policy);
        TokenBytesResult second = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        CoseEncrypt firstModel = CoseEncrypt.Read(first.Bytes).Message;
        CoseEncrypt secondModel = CoseEncrypt.Read(second.Bytes).Message;

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes[0], Is.EqualTo(0x84));
            Assert.That(firstModel.Tagged, Is.False);
            Assert.That(firstModel.Ciphertext, Has.Length.EqualTo(16));
            Assert.That(firstModel.Header.InitializationVector,
                Is.Not.EqualTo(secondModel.Header.InitializationVector));
        });
    }

    [Test]
    public void KeyMetadataAndWrongKeysFailClosed()
    {
        TokenPolicy policy = Policy("A128GCM", "A128KW");
        TokenKey denied = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        denied.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationUnwrapKey };
        TokenBytesResult deniedCreate = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            denied,
            policy);
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult created = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        TokenKey wrong = Symmetric(
            Sequence(16, 90),
            CoseAlgorithms.Aes128KeyWrap);
        CoseEncryptResult wrongResult = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            wrong,
            policy);
        key.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationWrapKey };
        CoseEncryptResult deniedDecrypt = CoseEncryptOperations.Decrypt(
            created.Bytes,
            0,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                deniedCreate,
                "key_operation_not_permitted");
            AssertFailure(wrongResult, "unwrap_failed");
            AssertFailure(
                deniedDecrypt,
                "key_operation_not_permitted");
        });
    }

    [Test]
    public void CriticalHeadersSizeAndPolicyLimitsAreEnforced()
    {
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenPolicy policy = Policy("A128GCM", "A128KW");
        CoseHeader critical = ContentHeader();
        SetUnknownCritical(critical);
        TokenBytesResult criticalResult = Create(
            critical,
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        TokenBytesResult created = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            policy);
        TokenPolicy small = Policy("A128GCM", "A128KW");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        TokenPolicy invalid = Policy("A128GCM", "A128KW");
        invalid.AllowedAlgorithms = null;

        TokenBytesResult smallCreate = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            small);
        CoseEncryptResult smallDecrypt = CoseEncryptOperations.Decrypt(
            created.Bytes, 0, key, small);
        TokenBytesResult invalidCreate = Create(
            ContentHeader(),
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1),
            key,
            invalid);
        CoseEncryptResult invalidDecrypt = CoseEncryptOperations.Decrypt(
            created.Bytes, 0, key, invalid);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                criticalResult,
                "unknown_cose_critical_header");
            AssertBytesFailure(smallCreate, "buffer_limit_exceeded");
            AssertFailure(smallDecrypt, "cbor_size_exceeded");
            AssertBytesFailure(invalidCreate, "invalid_policy");
            AssertFailure(invalidDecrypt, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseHeader header = ContentHeader();
        CoseHeader[] recipients = new CoseHeader[]
        {
            RecipientHeader(CoseAlgorithms.Aes128KeyWrap, 1)
        };
        TokenKey key = Symmetric(
            Sequence(16, 1),
            CoseAlgorithms.Aes128KeyWrap);
        TokenKey[] keys = new TokenKey[] { key };
        TokenPolicy policy = Policy("A128GCM", "A128KW");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseEncryptOperations.Create(
                null, header, recipients, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Create(
                new byte[0], null, recipients, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Create(
                new byte[0], header, null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Create(
                new byte[0], header, recipients, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Create(
                new byte[0], header, recipients, keys, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Decrypt(
                null, 0, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Decrypt(
                new byte[0], 0, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Decrypt(
                new byte[0], 0, key, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Decrypt(
                null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncryptOperations.Decrypt(
                new byte[0], (TokenKey[])null, policy),
                Throws.ArgumentNullException);
        });
    }

    static TokenBytesResult Create(
        CoseHeader content,
        CoseHeader recipient,
        TokenKey key,
        TokenPolicy policy)
    {
        return CoseEncryptOperations.Create(
            new byte[] { 1, 2, 3 },
            content,
            new CoseHeader[] { recipient },
            new TokenKey[] { key },
            policy);
    }

    static CoseHeader ContentHeader()
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Aes128Gcm));
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
        key.Use = TokenKeyUses.Encryption;
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

    static void SetUnknownCritical(CoseHeader header)
    {
        header.SetProtectedByTextLabel("private", Boolean(true));
        header.SetProtected(
            CoseHeaderLabels.Critical,
            Array(Text("private")));
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

    static void AssertVerified(CoseEncryptResult result, byte[] plaintext)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Message.PlaintextAvailable, Is.True);
        Assert.That(result.Message.AuthenticationTagVerified, Is.True);
        Assert.That(result.Message.Plaintext, Is.EqualTo(plaintext));
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

    static void AssertFailure(CoseEncryptResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
