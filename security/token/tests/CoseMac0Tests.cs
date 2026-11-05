using token;

namespace token.Tests;

public class CoseMac0Tests
{
    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseMac0 message = Message();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D18443A10104A10442010243030405420607")));
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10104")));
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.TagVerified, Is.False);
        });
    }

    [Test]
    public void ParsedMessagePreservesExactProtectedHeaderBytes()
    {
        CborItem protectedMap = Map(
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 9 })),
            Entry(CoseHeaderLabels.Algorithm, Int(5)));
        byte[] protectedBytes = Encode(protectedMap);
        byte[] encoded = Encode(Tag(17, Array(
            Bytes(protectedBytes),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));

        CoseMac0Result parsed = CoseMac0.Read(encoded);
        TokenBytesResult rewritten = parsed.Message.Write();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            AssertSuccess(rewritten);
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
            Assert.That(parsed.Message.Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.HmacSha256));
            Assert.That(parsed.Message.Header.KeyId,
                Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void CustomHeadersLargePayloadAndTagRoundTrip()
    {
        CoseMac0 message = Message();
        message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cwt"));
        message.Header.SetProtected(-70000, Array(
            Unsigned(1),
            Boolean(true)));
        message.Header.SetUnprotectedByTextLabel(
            "urn:example:hint",
            Text("secondary"));
        message.Payload = Sequence(300, 4);
        message.Tag = Sequence(64, 50);

        TokenBytesResult written = message.Write();
        CoseMac0Result parsed = CoseMac0.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Header.ContentTypeText,
                Is.EqualTo("application/cwt"));
            Assert.That(parsed.Message.Header.GetProtected(-70000)
                .ArrayValue[1].BooleanValue, Is.True);
            Assert.That(parsed.Message.Header.GetUnprotectedByTextLabel(
                "urn:example:hint").Text, Is.EqualTo("secondary"));
            Assert.That(parsed.Message.Payload,
                Is.EqualTo(message.Payload));
            Assert.That(parsed.Message.Tag, Is.EqualTo(message.Tag));
        });
    }

    [Test]
    public void DetachedAndEmptyPayloadRemainDistinct()
    {
        CoseMac0 detached = Message();
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseMac0 attached = Message();
        attached.Payload = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseMac0Result detachedResult = CoseMac0.Read(
            detachedBytes.Bytes);
        CoseMac0Result attachedResult = CoseMac0.Read(
            attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(detachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(Decode(attachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Message.DetachedPayload, Is.True);
            Assert.That(detachedResult.Message.Payload, Is.Null);
            Assert.That(detachedResult.Message.Tagged, Is.False);
            Assert.That(attachedResult.Message.DetachedPayload, Is.False);
            Assert.That(attachedResult.Message.Payload, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTagControlOverridesAndUpdatesTheModel()
    {
        CoseMac0 message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes[0], Is.EqualTo(0xD1));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseMac0.Read(untagged.Bytes).Message.Tagged,
                Is.False);
            Assert.That(CoseMac0.Read(tagged.Bytes).Message.Tagged,
                Is.True);
        });
    }

    [Test]
    public void HeaderChangesRegenerateProtectedBytes()
    {
        CoseMac0 message = Message();
        byte[] encoded = message.Write().Bytes;
        CoseMac0Result parsed = CoseMac0.Read(encoded);
        byte[] originalProtected = parsed.Message.Header.ProtectedBytes
            .ToArray();
        parsed.Message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cbor"));

        TokenBytesResult written = parsed.Message.Write();
        CoseMac0Result reparsed = CoseMac0.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(written.Bytes, Is.Not.EqualTo(encoded));
            Assert.That(reparsed.Message.Header.ProtectedBytes,
                Is.Not.EqualTo(originalProtected));
            Assert.That(reparsed.Message.Header.ContentTypeText,
                Is.EqualTo("application/cbor"));
        });
    }

    [Test]
    public void ParsingCopiesPayloadTagAndProtectedBytes()
    {
        byte[] encoded = Message().Write().Bytes;
        CoseMac0Result parsed = CoseMac0.Read(encoded);
        byte[] payload = parsed.Message.Payload.ToArray();
        byte[] tag = parsed.Message.Tag.ToArray();
        byte[] protectedBytes = parsed.Message.Header.ProtectedBytes
            .ToArray();

        System.Array.Fill(encoded, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Message.Payload, Is.EqualTo(payload));
            Assert.That(parsed.Message.Tag, Is.EqualTo(tag));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
        });
    }

    [Test]
    public void WritingDoesNotMutateCallerPayloadOrTag()
    {
        byte[] payload = Sequence(80, 10);
        byte[] tag = Sequence(32, 90);
        byte[] originalPayload = payload.ToArray();
        byte[] originalTag = tag.ToArray();
        CoseMac0 message = Message();
        message.Payload = payload;
        message.Tag = tag;

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(payload, Is.EqualTo(originalPayload));
            Assert.That(tag, Is.EqualTo(originalTag));
        });
    }

    [Test]
    public void InvalidMessageAndHeaderStructuresAreRejected()
    {
        CoseMac0Result wrongLength = CoseMac0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }))));
        CoseMac0Result wrongProtected = CoseMac0.Read(Encode(Array(
            Map(),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseMac0Result badProtectedMap = CoseMac0.Read(Encode(Array(
            Bytes(new byte[] { 1 }),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseMac0Result wrongUnprotected = CoseMac0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Array(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseMac0Result wrongPayload = CoseMac0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Boolean(false),
            Bytes(new byte[] { 2 }))));
        CoseMac0Result wrongTagValue = CoseMac0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Boolean(false))));

        Assert.Multiple(() =>
        {
            AssertFailure(wrongLength,
                "invalid_cose_message_structure", 3);
            AssertFailure(wrongProtected,
                "invalid_cose_message_structure", 0);
            AssertFailure(badProtectedMap,
                "invalid_cose_protected_header", -1);
            AssertFailure(wrongUnprotected,
                "invalid_cose_message_structure", 1);
            AssertFailure(wrongPayload,
                "invalid_cose_message_structure", 2);
            AssertFailure(wrongTagValue,
                "invalid_cose_message_structure", 3);
        });
    }

    [Test]
    public void WrongTagTrailingDataAndInvalidCborAreRejected()
    {
        byte[] valid = Message().Write().Bytes;
        CoseMac0Result wrongTag = CoseMac0.Read(Encode(
            Tag(18, Structure())));
        CoseMac0Result unknownTag = CoseMac0.Read(Encode(
            Tag(999, Structure())));
        CoseMac0Result trailing = CoseMac0.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseMac0Result invalid = CoseMac0.Read(new byte[] { 0x9F });

        Assert.Multiple(() =>
        {
            AssertFailure(wrongTag,
                "unexpected_cose_message_type", 0);
            AssertFailure(unknownTag,
                "unknown_cose_message_tag", 0);
            AssertFailure(trailing,
                "trailing_cose_message_data", valid.Length);
            AssertFailure(invalid, "unexpected_end_of_cbor", 1);
        });
    }

    [Test]
    public void DuplicateHeaderLabelsAreRejectedWhileWriting()
    {
        CoseMac0 message = Message();
        message.Header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(4))
        };
        message.Header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(4))
        };

        TokenBytesResult result = message.Write();

        AssertBytesFailure(result, "duplicate_cose_header_label");
    }

    [Test]
    public void InvalidModelsReturnStableFailures()
    {
        CoseMac0 missingHeader = Message();
        missingHeader.Header = null;
        CoseMac0 missingPayload = Message();
        missingPayload.Payload = null;
        CoseMac0 conflictingDetached = Message();
        conflictingDetached.DetachedPayload = true;
        CoseMac0 missingTag = Message();
        missingTag.Tag = null;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingHeader.Write(),
                "invalid_cose_mac0_header");
            AssertBytesFailure(missingPayload.Write(),
                "invalid_cose_mac0_payload");
            AssertBytesFailure(conflictingDetached.Write(),
                "invalid_cose_mac0_payload");
            AssertBytesFailure(missingTag.Write(),
                "invalid_cose_mac0_tag");
        });
    }

    [Test]
    public void FailedWriteDoesNotCommitTagStateOrProtectedBytes()
    {
        CoseMac0 message = Message();
        byte[] before = message.Header.ProtectedBytes.ToArray();
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = 1;

        TokenBytesResult result = message.WriteWithTagAndPolicy(false, small);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(result, "buffer_limit_exceeded");
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(before));
        });
    }

    [Test]
    public void ReadAndWriteEnforceSizeDepthAndPolicyLimits()
    {
        CoseMac0 message = Message();
        TokenBytesResult unrestricted = message.Write();
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactWrite = message.WriteWithPolicy(exact);
        TokenBytesResult smallWrite = message.WriteWithPolicy(small);
        CoseMac0Result exactRead = CoseMac0.Read(
            unrestricted.Bytes,
            exact);
        CoseMac0Result smallRead = CoseMac0.Read(
            unrestricted.Bytes,
            small);
        CoseMac0Result shallowRead = CoseMac0.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseMac0Result invalidRead = CoseMac0.Read(
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
        CoseMac0 message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseMac0.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0.Write(
                message,
                (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0.Write(message, true, null),
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
        CoseMac0 message = new CoseMac0();
        CoseMac0Result result = new CoseMac0Result();

        Assert.Multiple(() =>
        {
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Payload, Is.Empty);
            Assert.That(message.DetachedPayload, Is.False);
            Assert.That(message.Tag, Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.TagVerified, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }

    static CoseMac0 Message()
    {
        CoseMac0 message = new CoseMac0();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.HmacSha256Truncated64));
        message.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 1, 2 }));
        message.Payload = new byte[] { 3, 4, 5 };
        message.Tag = new byte[] { 6, 7 };
        return message;
    }

    static CborItem Structure()
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }));
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
        CoseMac0Result result,
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
