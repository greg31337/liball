using System.Text;
using token;

namespace token.Tests;

public class CoseSign1Tests
{
    const string WorkingGroupVector =
        "D28441A0A201260442313154546869732069732074686520636F6E74656E742E584087DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F";

    [Test]
    public void ReadMatchesCoseWorkingGroupSign1Vector()
    {
        CoseSign1Result result = CoseSign1.Read(
            Convert.FromHexString(WorkingGroupVector));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message.Tagged, Is.True);
            Assert.That(result.Message.DetachedPayload, Is.False);
            Assert.That(result.Message.SignatureVerified, Is.False);
            Assert.That(Encoding.UTF8.GetString(result.Message.Payload),
                Is.EqualTo("This is the content."));
            Assert.That(result.Message.Header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA0 }));
            Assert.That(result.Message.Header.ProtectedParameters, Is.Empty);
            Assert.That(result.Message.Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha256));
            Assert.That(result.Message.Header.HasAlgorithm, Is.True);
            Assert.That(result.Message.Header.KeyId,
                Is.EqualTo(Encoding.ASCII.GetBytes("11")));
            Assert.That(result.Message.Signature, Has.Length.EqualTo(64));
            Assert.That(result.Message.Signature,
                Is.EqualTo(Convert.FromHexString(
                    "87DB0D2E5571843B78AC33ECB2830DF7B6E0A4D5B7376DE336B23C591C90C425317E56127FBE04370097CE347087B233BF722B64072BEB4486BDA4031D27244F")));
        });
    }

    [Test]
    public void ParsedVectorWritesWithoutChangingSignedHeaderBytes()
    {
        byte[] vector = Convert.FromHexString(WorkingGroupVector);
        CoseSign1Result parsed = CoseSign1.Read(vector);

        TokenBytesResult written = parsed.Message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(written.Bytes, Is.EqualTo(vector));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseSign1 message = new CoseSign1();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.EcdsaSha256));
        message.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 9 }));
        message.Payload = new byte[] { 1 };
        message.Signature = new byte[] { 2, 3 };

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D28443A10126A10441094101420203")));
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10126")));
            Assert.That(message.Tagged, Is.True);
        });
    }

    [Test]
    public void CustomHeadersAndLargeValuesRoundTrip()
    {
        CoseSign1 message = Message();
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
        message.Signature = Sequence(132, 20);

        TokenBytesResult written = message.Write();
        CoseSign1Result parsed = CoseSign1.Read(written.Bytes);

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
            Assert.That(parsed.Message.Payload, Is.EqualTo(message.Payload));
            Assert.That(parsed.Message.Signature,
                Is.EqualTo(message.Signature));
        });
    }

    [Test]
    public void DetachedAndEmptyAttachedPayloadsRemainDistinct()
    {
        CoseSign1 detached = Message();
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseSign1 attached = Message();
        attached.Payload = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseSign1Result detachedResult = CoseSign1.Read(detachedBytes.Bytes);
        CoseSign1Result attachedResult = CoseSign1.Read(attachedBytes.Bytes);
        CborItem detachedItem = Decode(detachedBytes.Bytes);
        CborItem attachedItem = Decode(attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(detachedItem.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(attachedItem.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Success, Is.True);
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
        CoseSign1 message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes[0], Is.EqualTo(0xD2));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseSign1.Read(untagged.Bytes).Message.Tagged,
                Is.False);
            Assert.That(CoseSign1.Read(tagged.Bytes).Message.Tagged,
                Is.True);
        });
    }

    [Test]
    public void HeaderChangesRegenerateProtectedBytes()
    {
        CoseSign1Result parsed = CoseSign1.Read(
            Convert.FromHexString(WorkingGroupVector));
        parsed.Message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("text/plain"));

        TokenBytesResult written = parsed.Message.Write();
        CoseSign1Result reparsed = CoseSign1.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(written.Bytes,
                Is.Not.EqualTo(Convert.FromHexString(WorkingGroupVector)));
            Assert.That(reparsed.Message.Header.ContentTypeText,
                Is.EqualTo("text/plain"));
            Assert.That(reparsed.Message.Header.ProtectedBytes,
                Is.Not.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void ParsingCopiesPayloadSignatureAndProtectedBytes()
    {
        byte[] encoded = Convert.FromHexString(WorkingGroupVector);
        CoseSign1Result parsed = CoseSign1.Read(encoded);
        byte[] payload = parsed.Message.Payload.ToArray();
        byte[] signature = parsed.Message.Signature.ToArray();
        byte[] protectedBytes = parsed.Message.Header.ProtectedBytes.ToArray();

        System.Array.Fill(encoded, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Message.Payload, Is.EqualTo(payload));
            Assert.That(parsed.Message.Signature, Is.EqualTo(signature));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
        });
    }

    [Test]
    public void WritingDoesNotMutateCallerInputs()
    {
        byte[] payload = Sequence(40, 10);
        byte[] signature = Sequence(64, 80);
        CoseSign1 message = Message();
        message.Payload = payload;
        message.Signature = signature;
        byte[] payloadCopy = payload.ToArray();
        byte[] signatureCopy = signature.ToArray();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(payload, Is.EqualTo(payloadCopy));
            Assert.That(signature, Is.EqualTo(signatureCopy));
        });
    }

    [Test]
    public void InvalidMessageAndHeaderStructuresAreRejected()
    {
        CoseSign1Result wrongLength = CoseSign1.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }))));
        CoseSign1Result wrongProtected = CoseSign1.Read(Encode(Array(
            Map(),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseSign1Result badProtectedMap = CoseSign1.Read(Encode(Array(
            Bytes(new byte[] { 1 }),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseSign1Result wrongPayload = CoseSign1.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Boolean(false),
            Bytes(new byte[] { 2 }))));
        CoseSign1Result wrongSignature = CoseSign1.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Boolean(false))));

        Assert.Multiple(() =>
        {
            AssertFailure(
                wrongLength,
                "invalid_cose_message_structure",
                3);
            AssertFailure(
                wrongProtected,
                "invalid_cose_message_structure",
                0);
            AssertFailure(
                badProtectedMap,
                "invalid_cose_protected_header",
                -1);
            AssertFailure(
                wrongPayload,
                "invalid_cose_message_structure",
                2);
            AssertFailure(
                wrongSignature,
                "invalid_cose_message_structure",
                3);
        });
    }

    [Test]
    public void WrongTagTrailingDataAndInvalidCborAreRejected()
    {
        byte[] valid = Message().Write().Bytes;
        CoseSign1Result wrongTag = CoseSign1.Read(Encode(
            Tag(98, Structure())));
        CoseSign1Result trailing = CoseSign1.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseSign1Result invalid = CoseSign1.Read(new byte[] { 0x9F });

        Assert.Multiple(() =>
        {
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
    public void DuplicateHeaderLabelsAreRejectedWhileWriting()
    {
        CoseSign1 message = Message();
        message.Header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(-7))
        };
        message.Header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(-7))
        };

        TokenBytesResult result = message.Write();

        AssertBytesFailure(result, "duplicate_cose_header_label");
    }

    [Test]
    public void InvalidModelsReturnStableFailures()
    {
        CoseSign1 missingHeader = Message();
        missingHeader.Header = null;
        CoseSign1 missingPayload = Message();
        missingPayload.Payload = null;
        CoseSign1 conflictingDetached = Message();
        conflictingDetached.DetachedPayload = true;
        CoseSign1 missingSignature = Message();
        missingSignature.Signature = null;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                missingHeader.Write(),
                "invalid_cose_sign1_header");
            AssertBytesFailure(
                missingPayload.Write(),
                "invalid_cose_sign1_payload");
            AssertBytesFailure(
                conflictingDetached.Write(),
                "invalid_cose_sign1_payload");
            AssertBytesFailure(
                missingSignature.Write(),
                "invalid_cose_sign1_signature");
        });
    }

    [Test]
    public void ReadAndWriteEnforceSizeDepthAndPolicyLimits()
    {
        CoseSign1 message = Message();
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
        CoseSign1Result exactRead = CoseSign1.Read(
            unrestricted.Bytes,
            exact);
        CoseSign1Result smallRead = CoseSign1.Read(
            unrestricted.Bytes,
            small);
        CoseSign1Result shallowRead = CoseSign1.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseSign1Result invalidRead = CoseSign1.Read(
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
        CoseSign1 message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseSign1.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1.Write(message, (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1.Write(message, true, null),
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
        CoseSign1 message = new CoseSign1();
        CoseSign1Result result = new CoseSign1Result();

        Assert.Multiple(() =>
        {
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Payload, Is.Empty);
            Assert.That(message.DetachedPayload, Is.False);
            Assert.That(message.Signature, Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.SignatureVerified, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }

    static CoseSign1 Message()
    {
        CoseSign1 message = new CoseSign1();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.EcdsaP256Sha256));
        message.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 7 }));
        message.Payload = new byte[] { 1, 2, 3 };
        message.Signature = new byte[] { 8, 9 };
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
        CoseSign1Result result,
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
