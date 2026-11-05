using token;

namespace token.Tests;

public class CoseEncryptTests
{
    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseEncrypt message = Message();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D8608443A10101A10542010243030405818340A20122044109420607")));
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10101")));
            Assert.That(message.Recipients[0].Header.ProtectedBytes,
                Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.Plaintext, Is.Null);
            Assert.That(message.PlaintextAvailable, Is.False);
            Assert.That(message.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void NestedRecipientsAndNullEncryptedKeysRoundTrip()
    {
        CoseEncrypt message = Message();
        CoseRecipient child = Recipient(
            CoseAlgorithms.EcdhEsHkdfSha256,
            System.Array.Empty<byte>());
        child.Header.SetProtectedByTextLabel(
            "level",
            Unsigned(2));
        CoseRecipient absent = Recipient(
            CoseAlgorithms.Direct,
            System.Array.Empty<byte>());
        absent.EncryptedKeyAbsent = true;
        absent.EncryptedKey = null;
        message.Recipients[0].Recipients =
            new CoseRecipient[] { child, absent };
        message.Recipients = new CoseRecipient[]
        {
            message.Recipients[0],
            Recipient(CoseAlgorithms.Aes256KeyWrap, Sequence(40, 20))
        };
        message.Ciphertext = Sequence(300, 3);

        TokenBytesResult written = message.Write();
        CoseEncryptResult parsed = CoseEncrypt.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Ciphertext,
                Is.EqualTo(message.Ciphertext));
            Assert.That(parsed.Message.Recipients, Has.Length.EqualTo(2));
            Assert.That(parsed.Message.Recipients[0].Recipients,
                Has.Length.EqualTo(2));
            Assert.That(parsed.Message.Recipients[0].Recipients[0]
                .Header.GetProtectedByTextLabel("level").IntegerArgument,
                Is.EqualTo(2));
            Assert.That(parsed.Message.Recipients[0].Recipients[1]
                .EncryptedKeyAbsent, Is.True);
            Assert.That(parsed.Message.Recipients[0].Recipients[1]
                .EncryptedKey, Is.Null);
            Assert.That(parsed.Message.Recipients[1].EncryptedKey,
                Is.EqualTo(Sequence(40, 20)));
        });
    }

    [Test]
    public void ParsedMessagePreservesExactProtectedHeaderBytes()
    {
        CborItem bodyProtected = Map(
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 9 })),
            Entry(CoseHeaderLabels.Algorithm, Int(1)));
        CborItem recipientProtected = Map(
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 8 })),
            Entry(CoseHeaderLabels.Algorithm, Int(-3)));
        byte[] encoded = Encode(Tag(96, Array(
            Bytes(Encode(bodyProtected)),
            Map(),
            Bytes(new byte[] { 1 }),
            Array(Array(
                Bytes(Encode(recipientProtected)),
                Map(),
                Bytes(new byte[] { 2 }))))));

        CoseEncryptResult parsed = CoseEncrypt.Read(encoded);
        TokenBytesResult rewritten = parsed.Message.Write();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            AssertSuccess(rewritten);
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(Encode(bodyProtected)));
            Assert.That(parsed.Message.Recipients[0].Header.ProtectedBytes,
                Is.EqualTo(Encode(recipientProtected)));
        });
    }

    [Test]
    public void HeaderChangesRegenerateProtectedBytesAtEveryLayer()
    {
        CoseEncrypt message = Message();
        message.Recipients[0].Header.SetProtectedByTextLabel(
            "purpose",
            Text("initial"));
        byte[] original = message.Write().Bytes;
        CoseEncryptResult parsed = CoseEncrypt.Read(original);
        byte[] bodyBytes = parsed.Message.Header.ProtectedBytes.ToArray();
        byte[] recipientBytes = parsed.Message.Recipients[0]
            .Header.ProtectedBytes.ToArray();
        parsed.Message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cwt"));
        parsed.Message.Recipients[0].Header.SetProtectedByTextLabel(
            "purpose",
            Text("updated"));

        TokenBytesResult written = parsed.Message.Write();
        CoseEncryptResult reparsed = CoseEncrypt.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(reparsed.Message.Header.ProtectedBytes,
                Is.Not.EqualTo(bodyBytes));
            Assert.That(reparsed.Message.Recipients[0]
                .Header.ProtectedBytes,
                Is.Not.EqualTo(recipientBytes));
            Assert.That(reparsed.Message.Header.ContentTypeText,
                Is.EqualTo("application/cwt"));
            Assert.That(reparsed.Message.Recipients[0].Header
                .GetProtectedByTextLabel("purpose").Text,
                Is.EqualTo("updated"));
        });
    }

    [Test]
    public void DetachedAndEmptyCiphertextRemainDistinct()
    {
        CoseEncrypt detached = Message();
        detached.DetachedCiphertext = true;
        detached.Ciphertext = null;
        CoseEncrypt attached = Message();
        attached.Ciphertext = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseEncryptResult detachedResult = CoseEncrypt.Read(
            detachedBytes.Bytes);
        CoseEncryptResult attachedResult = CoseEncrypt.Read(
            attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(detachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(Decode(attachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Message.DetachedCiphertext, Is.True);
            Assert.That(detachedResult.Message.Ciphertext, Is.Null);
            Assert.That(attachedResult.Message.DetachedCiphertext, Is.False);
            Assert.That(attachedResult.Message.Ciphertext, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTagControlOverridesAndUpdatesTheModel()
    {
        CoseEncrypt message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes.Take(2),
                Is.EqualTo(new byte[] { 0xD8, 0x60 }));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseEncrypt.Read(untagged.Bytes).Message.Tagged,
                Is.False);
            Assert.That(CoseEncrypt.Read(tagged.Bytes).Message.Tagged,
                Is.True);
        });
    }

    [Test]
    public void ParsingCopiesAllByteStringFields()
    {
        byte[] encoded = Message().Write().Bytes;
        CoseEncryptResult parsed = CoseEncrypt.Read(encoded);
        byte[] protectedBytes = parsed.Message.Header.ProtectedBytes.ToArray();
        byte[] ciphertext = parsed.Message.Ciphertext.ToArray();
        byte[] encryptedKey = parsed.Message.Recipients[0]
            .EncryptedKey.ToArray();

        System.Array.Fill(encoded, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
            Assert.That(parsed.Message.Ciphertext, Is.EqualTo(ciphertext));
            Assert.That(parsed.Message.Recipients[0].EncryptedKey,
                Is.EqualTo(encryptedKey));
        });
    }

    [Test]
    public void InvalidRecipientStructuresAreRejected()
    {
        CoseEncryptResult tooShort = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map()));
        CoseEncryptResult tooLong = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array(RecipientItem()),
            Unsigned(1)));
        CoseEncryptResult wrongHeaders = ReadWithRecipient(Array(
            Map(),
            Map(),
            Bytes(System.Array.Empty<byte>())));
        CoseEncryptResult wrongKey = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Boolean(false)));
        CoseEncryptResult emptyChildren = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array()));
        CoseEncryptResult invalidChild = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array(Boolean(false))));

        Assert.Multiple(() =>
        {
            AssertFailure(tooShort, "invalid_cose_recipient_structure", 0);
            AssertFailure(tooLong, "invalid_cose_recipient_structure", 0);
            AssertFailure(wrongHeaders, "invalid_cose_protected_header", -1);
            AssertFailure(wrongKey, "invalid_cose_recipient_structure", 0);
            AssertFailure(emptyChildren, "invalid_cose_recipient_structure", 0);
            AssertFailure(invalidChild, "invalid_cose_recipient_structure", 0);
        });
    }

    [Test]
    public void InvalidMessageStructuresAndTagsAreRejected()
    {
        CoseEncryptResult emptyRecipients = CoseEncrypt.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Array())));
        CoseEncryptResult wrongTag = CoseEncrypt.Read(Encode(
            Tag(98, Structure(RecipientItem()))));
        byte[] valid = Message().Write().Bytes;
        CoseEncryptResult trailing = CoseEncrypt.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseEncryptResult invalid = CoseEncrypt.Read(
            new byte[] { 0x9F });

        Assert.Multiple(() =>
        {
            AssertFailure(
                emptyRecipients,
                "invalid_cose_message_structure",
                3);
            AssertFailure(
                wrongTag,
                "unexpected_cose_message_type",
                0);
            AssertFailure(
                trailing,
                "trailing_cose_message_data",
                valid.Length);
            AssertFailure(invalid, "unexpected_end_of_cbor", 1);
        });
    }

    [Test]
    public void InvalidModelsAndCyclesReturnStableFailures()
    {
        CoseEncrypt missingHeader = Message();
        missingHeader.Header = null;
        CoseEncrypt missingCiphertext = Message();
        missingCiphertext.Ciphertext = null;
        CoseEncrypt conflictingDetached = Message();
        conflictingDetached.DetachedCiphertext = true;
        CoseEncrypt missingRecipients = Message();
        missingRecipients.Recipients = null;
        CoseEncrypt emptyRecipients = Message();
        emptyRecipients.Recipients = System.Array.Empty<CoseRecipient>();
        CoseEncrypt missingRecipient = Message();
        missingRecipient.Recipients = new CoseRecipient[] { null };
        CoseEncrypt missingKey = Message();
        missingKey.Recipients[0].EncryptedKey = null;
        CoseEncrypt conflictingKey = Message();
        conflictingKey.Recipients[0].EncryptedKeyAbsent = true;
        CoseEncrypt missingChildren = Message();
        missingChildren.Recipients[0].Recipients = null;
        CoseEncrypt cyclic = Message();
        cyclic.Recipients[0].Recipients =
            new CoseRecipient[] { cyclic.Recipients[0] };

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingHeader.Write(),
                "invalid_cose_encrypt_header");
            AssertBytesFailure(missingCiphertext.Write(),
                "invalid_cose_encrypt_ciphertext");
            AssertBytesFailure(conflictingDetached.Write(),
                "invalid_cose_encrypt_ciphertext");
            AssertBytesFailure(missingRecipients.Write(),
                "invalid_cose_encrypt_recipients");
            AssertBytesFailure(emptyRecipients.Write(),
                "invalid_cose_encrypt_recipients");
            AssertBytesFailure(missingRecipient.Write(),
                "invalid_cose_recipient");
            AssertBytesFailure(missingKey.Write(),
                "invalid_cose_recipient_encrypted_key");
            AssertBytesFailure(conflictingKey.Write(),
                "invalid_cose_recipient_encrypted_key");
            AssertBytesFailure(missingChildren.Write(),
                "invalid_cose_recipient_children");
            AssertBytesFailure(cyclic.Write(),
                "cyclic_cose_recipient");
        });
    }

    [Test]
    public void DuplicateHeaderLabelsAreRejectedAtEveryLayer()
    {
        CoseEncrypt body = Message();
        SetDuplicateAlgorithm(body.Header);
        CoseEncrypt recipient = Message();
        SetDuplicateAlgorithm(recipient.Recipients[0].Header);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(body.Write(),
                "duplicate_cose_header_label");
            AssertBytesFailure(recipient.Write(),
                "duplicate_cose_header_label");
        });
    }

    [Test]
    public void FailedWriteDoesNotCommitTagOrProtectedBytes()
    {
        CoseEncrypt message = Message();
        message.Tagged = true;
        byte[] bodyBefore = message.Header.ProtectedBytes.ToArray();
        byte[] recipientBefore = message.Recipients[0]
            .Header.ProtectedBytes.ToArray();
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = 1;

        TokenBytesResult result = message.WriteWithTagAndPolicy(false, small);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(result, "buffer_limit_exceeded");
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(bodyBefore));
            Assert.That(message.Recipients[0].Header.ProtectedBytes,
                Is.EqualTo(recipientBefore));
        });
    }

    [Test]
    public void ReadAndWriteEnforceSizeDepthAndPolicyLimits()
    {
        CoseEncrypt message = Message();
        TokenBytesResult unrestricted = message.Write();
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 2;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactWrite = message.WriteWithPolicy(exact);
        TokenBytesResult smallWrite = message.WriteWithPolicy(small);
        CoseEncryptResult exactRead = CoseEncrypt.Read(
            unrestricted.Bytes,
            exact);
        CoseEncryptResult smallRead = CoseEncrypt.Read(
            unrestricted.Bytes,
            small);
        CoseEncryptResult shallowRead = CoseEncrypt.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseEncryptResult invalidRead = CoseEncrypt.Read(
            unrestricted.Bytes,
            invalid);

        Assert.Multiple(() =>
        {
            AssertSuccess(exactWrite);
            Assert.That(exactRead.Success, Is.True);
            AssertBytesFailure(smallWrite, "buffer_limit_exceeded");
            Assert.That(smallRead.Error.Code,
                Is.EqualTo("cbor_size_exceeded"));
            Assert.That(shallowRead.Error.Code,
                Is.EqualTo("cbor_depth_exceeded"));
            AssertBytesFailure(invalidWrite, "invalid_policy");
            AssertFailure(invalidRead, "invalid_policy", -1);
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseEncrypt message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseEncrypt.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt.Write(
                message,
                (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt.Write(message, true, null),
                Throws.ArgumentNullException);
            Assert.That(() => message.WriteWithPolicy((TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => message.WriteWithTagAndPolicy(true, null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ModelsHaveSafeDefaults()
    {
        CoseEncrypt message = new CoseEncrypt();
        CoseEncryptResult result = new CoseEncryptResult();
        CoseRecipient recipient = new CoseRecipient();

        Assert.Multiple(() =>
        {
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Ciphertext, Is.Empty);
            Assert.That(message.DetachedCiphertext, Is.False);
            Assert.That(message.Recipients, Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
            Assert.That(recipient.Header, Is.Not.Null);
            Assert.That(recipient.EncryptedKey, Is.Empty);
            Assert.That(recipient.EncryptedKeyAbsent, Is.False);
            Assert.That(recipient.Recipients, Is.Empty);
            Assert.That(recipient.Decrypted, Is.False);
        });
    }

    static CoseEncrypt Message()
    {
        CoseEncrypt message = new CoseEncrypt();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Aes128Gcm));
        message.Header.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(new byte[] { 1, 2 }));
        message.Ciphertext = new byte[] { 3, 4, 5 };
        message.Recipients = new CoseRecipient[]
        {
            Recipient(CoseAlgorithms.Aes128KeyWrap, new byte[] { 6, 7 })
        };
        return message;
    }

    static CoseRecipient Recipient(int algorithm, byte[] encryptedKey)
    {
        CoseRecipient recipient = new CoseRecipient();
        recipient.Header.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        recipient.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 9 }));
        recipient.EncryptedKey = encryptedKey;
        return recipient;
    }

    static CoseEncryptResult ReadWithRecipient(CborItem recipient)
    {
        return CoseEncrypt.Read(Encode(Structure(recipient)));
    }

    static CborItem Structure(CborItem recipient)
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Array(recipient));
    }

    static CborItem RecipientItem()
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()));
    }

    static void SetDuplicateAlgorithm(CoseHeader header)
    {
        header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(1))
        };
        header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(1))
        };
    }

    static CborItem Decode(byte[] encoded)
    {
        CborReader reader = CborReader.Create(encoded);
        CborReadResult result = reader.Read();
        Assert.That(result.Success, Is.True);
        Assert.That(reader.GetRemainingLength(), Is.Zero);
        return result.Item;
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult result = writer.WriteItem(item);
        Assert.That(result.Success, Is.True);
        return writer.ToArray();
    }

    static CborMapEntry Entry(int key, CborItem value)
    {
        return new CborMapEntry(Int(key), value);
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

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
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

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static CborItem Tag(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
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

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True);
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

    static void AssertFailure(
        CoseEncryptResult result,
        string code,
        int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
        Assert.That(result.Message, Is.Null);
    }
}
