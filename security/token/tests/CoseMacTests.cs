using token;

namespace token.Tests;

public class CoseMacTests
{
    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseMac message = Message();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D8618543A10104A104410943010203480405060708090A0B818340A2012204410C420D0E")));
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10104")));
            Assert.That(message.Recipients[0].Header.ProtectedBytes,
                Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.TagVerified, Is.False);
        });
    }

    [Test]
    public void NestedRecipientsAndNullEncryptedKeysRoundTrip()
    {
        CoseMac message = Message();
        CoseRecipient child = Recipient(
            CoseAlgorithms.EcdhEsHkdfSha256,
            System.Array.Empty<byte>());
        child.Header.SetProtectedByTextLabel("level", Unsigned(2));
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
        message.Payload = Sequence(300, 3);
        message.Tag = Sequence(64, 80);

        TokenBytesResult written = message.Write();
        CoseMacResult parsed = CoseMac.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.EqualTo(message.Payload));
            Assert.That(parsed.Message.Tag, Is.EqualTo(message.Tag));
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
            Entry(CoseHeaderLabels.Algorithm, Int(5)));
        CborItem recipientProtected = Map(
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 8 })),
            Entry(CoseHeaderLabels.Algorithm, Int(-3)));
        byte[] encoded = Encode(Tag(97, Array(
            Bytes(Encode(bodyProtected)),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }),
            Array(Array(
                Bytes(Encode(recipientProtected)),
                Map(),
                Bytes(new byte[] { 3 }))))));

        CoseMacResult parsed = CoseMac.Read(encoded);
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
        CoseMac message = Message();
        message.Recipients[0].Header.SetProtectedByTextLabel(
            "purpose",
            Text("initial"));
        byte[] original = message.Write().Bytes;
        CoseMacResult parsed = CoseMac.Read(original);
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
        CoseMacResult reparsed = CoseMac.Read(written.Bytes);

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
    public void DetachedAndEmptyPayloadRemainDistinct()
    {
        CoseMac detached = Message();
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseMac attached = Message();
        attached.Payload = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseMacResult detachedResult = CoseMac.Read(detachedBytes.Bytes);
        CoseMacResult attachedResult = CoseMac.Read(attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(detachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(Decode(attachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Message.DetachedPayload, Is.True);
            Assert.That(detachedResult.Message.Payload, Is.Null);
            Assert.That(attachedResult.Message.DetachedPayload, Is.False);
            Assert.That(attachedResult.Message.Payload, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTagControlOverridesAndUpdatesTheModel()
    {
        CoseMac message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x85));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes.Take(2),
                Is.EqualTo(new byte[] { 0xD8, 0x61 }));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseMac.Read(untagged.Bytes).Message.Tagged,
                Is.False);
            Assert.That(CoseMac.Read(tagged.Bytes).Message.Tagged,
                Is.True);
        });
    }

    [Test]
    public void ParsingCopiesAllByteStringFields()
    {
        byte[] encoded = Message().Write().Bytes;
        CoseMacResult parsed = CoseMac.Read(encoded);
        byte[] protectedBytes = parsed.Message.Header.ProtectedBytes.ToArray();
        byte[] payload = parsed.Message.Payload.ToArray();
        byte[] tag = parsed.Message.Tag.ToArray();
        byte[] encryptedKey = parsed.Message.Recipients[0]
            .EncryptedKey.ToArray();

        System.Array.Fill(encoded, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
            Assert.That(parsed.Message.Payload, Is.EqualTo(payload));
            Assert.That(parsed.Message.Tag, Is.EqualTo(tag));
            Assert.That(parsed.Message.Recipients[0].EncryptedKey,
                Is.EqualTo(encryptedKey));
        });
    }

    [Test]
    public void InvalidRecipientStructuresAreRejected()
    {
        CoseMacResult tooShort = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map()));
        CoseMacResult tooLong = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array(RecipientItem()),
            Unsigned(1)));
        CoseMacResult wrongHeaders = ReadWithRecipient(Array(
            Map(),
            Map(),
            Bytes(System.Array.Empty<byte>())));
        CoseMacResult wrongKey = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Boolean(false)));
        CoseMacResult emptyChildren = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array()));
        CoseMacResult invalidChild = ReadWithRecipient(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(System.Array.Empty<byte>()),
            Array(Boolean(false))));

        Assert.Multiple(() =>
        {
            AssertFailure(tooShort,
                "invalid_cose_recipient_structure", 0);
            AssertFailure(tooLong,
                "invalid_cose_recipient_structure", 0);
            AssertFailure(wrongHeaders,
                "invalid_cose_protected_header", -1);
            AssertFailure(wrongKey,
                "invalid_cose_recipient_structure", 0);
            AssertFailure(emptyChildren,
                "invalid_cose_recipient_structure", 0);
            AssertFailure(invalidChild,
                "invalid_cose_recipient_structure", 0);
        });
    }

    [Test]
    public void InvalidMessageStructuresAndTagsAreRejected()
    {
        CoseMacResult emptyRecipients = CoseMac.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }),
            Array())));
        CoseMacResult wrongMac = CoseMac.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Boolean(false),
            Array(RecipientItem()))));
        CoseMacResult wrongTag = CoseMac.Read(Encode(
            Tag(98, Structure(RecipientItem()))));
        byte[] valid = Message().Write().Bytes;
        CoseMacResult trailing = CoseMac.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseMacResult invalid = CoseMac.Read(new byte[] { 0x9F });

        Assert.Multiple(() =>
        {
            AssertFailure(emptyRecipients,
                "invalid_cose_message_structure", 4);
            AssertFailure(wrongMac,
                "invalid_cose_message_structure", 3);
            AssertFailure(wrongTag,
                "unexpected_cose_message_type", 0);
            AssertFailure(trailing,
                "trailing_cose_message_data", valid.Length);
            AssertFailure(invalid, "unexpected_end_of_cbor", 1);
        });
    }

    [Test]
    public void InvalidModelsAndCyclesReturnStableFailures()
    {
        CoseMac missingHeader = Message();
        missingHeader.Header = null;
        CoseMac missingPayload = Message();
        missingPayload.Payload = null;
        CoseMac conflictingDetached = Message();
        conflictingDetached.DetachedPayload = true;
        CoseMac missingTag = Message();
        missingTag.Tag = null;
        CoseMac missingRecipients = Message();
        missingRecipients.Recipients = null;
        CoseMac emptyRecipients = Message();
        emptyRecipients.Recipients = System.Array.Empty<CoseRecipient>();
        CoseMac missingRecipient = Message();
        missingRecipient.Recipients = new CoseRecipient[] { null };
        CoseMac missingKey = Message();
        missingKey.Recipients[0].EncryptedKey = null;
        CoseMac conflictingKey = Message();
        conflictingKey.Recipients[0].EncryptedKeyAbsent = true;
        CoseMac missingChildren = Message();
        missingChildren.Recipients[0].Recipients = null;
        CoseMac cyclic = Message();
        cyclic.Recipients[0].Recipients =
            new CoseRecipient[] { cyclic.Recipients[0] };

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingHeader.Write(),
                "invalid_cose_mac_header");
            AssertBytesFailure(missingPayload.Write(),
                "invalid_cose_mac_payload");
            AssertBytesFailure(conflictingDetached.Write(),
                "invalid_cose_mac_payload");
            AssertBytesFailure(missingTag.Write(),
                "invalid_cose_mac_tag");
            AssertBytesFailure(missingRecipients.Write(),
                "invalid_cose_mac_recipients");
            AssertBytesFailure(emptyRecipients.Write(),
                "invalid_cose_mac_recipients");
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
        CoseMac body = Message();
        SetDuplicateAlgorithm(body.Header);
        CoseMac recipient = Message();
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
    public void FailedWriteDoesNotCommitTagStateOrProtectedBytes()
    {
        CoseMac message = Message();
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
        CoseMac message = Message();
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
        CoseMacResult exactRead = CoseMac.Read(
            unrestricted.Bytes,
            exact);
        CoseMacResult smallRead = CoseMac.Read(
            unrestricted.Bytes,
            small);
        CoseMacResult shallowRead = CoseMac.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseMacResult invalidRead = CoseMac.Read(
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
        CoseMac message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseMac.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac.Write(
                message,
                (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac.Write(message, true, null),
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
        CoseMac message = new CoseMac();
        CoseMacResult result = new CoseMacResult();

        Assert.Multiple(() =>
        {
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Payload, Is.Empty);
            Assert.That(message.DetachedPayload, Is.False);
            Assert.That(message.Tag, Is.Empty);
            Assert.That(message.Recipients, Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.TagVerified, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }

    static CoseMac Message()
    {
        CoseMac message = new CoseMac();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.HmacSha256Truncated64));
        message.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 9 }));
        message.Payload = new byte[] { 1, 2, 3 };
        message.Tag = new byte[] { 4, 5, 6, 7, 8, 9, 10, 11 };
        message.Recipients = new CoseRecipient[]
        {
            Recipient(CoseAlgorithms.Aes128KeyWrap,
                new byte[] { 13, 14 })
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
            Bytes(new byte[] { 12 }));
        recipient.EncryptedKey = encryptedKey;
        return recipient;
    }

    static CoseMacResult ReadWithRecipient(CborItem recipient)
    {
        return CoseMac.Read(Encode(Structure(recipient)));
    }

    static CborItem Structure(CborItem recipient)
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }),
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
            Entry(CoseHeaderLabels.Algorithm, Int(5))
        };
        header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(5))
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
        CoseMacResult result,
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
