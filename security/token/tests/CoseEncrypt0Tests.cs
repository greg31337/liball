using token;

namespace token.Tests;

public class CoseEncrypt0Tests
{
    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseEncrypt0 message = Message();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D08343A10101A10542010243030405")));
            Assert.That(message.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10101")));
            Assert.That(message.Tagged, Is.True);
        });
    }

    [Test]
    public void ParsedMessagePreservesExactProtectedHeaderBytes()
    {
        CborItem protectedMap = Map(
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 9 })),
            Entry(CoseHeaderLabels.Algorithm, Int(1)));
        byte[] protectedBytes = Encode(protectedMap);
        byte[] encoded = Encode(Tag(16, Array(
            Bytes(protectedBytes),
            Map(),
            Bytes(new byte[] { 1 }))));

        CoseEncrypt0Result parsed = CoseEncrypt0.Read(encoded);
        TokenBytesResult rewritten = parsed.Message.Write();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            AssertSuccess(rewritten);
            Assert.That(rewritten.Bytes, Is.EqualTo(encoded));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
            Assert.That(parsed.Message.Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.Aes128Gcm));
            Assert.That(parsed.Message.Header.KeyId,
                Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void CustomHeadersAndLargeCiphertextRoundTrip()
    {
        CoseEncrypt0 message = Message();
        message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cwt"));
        message.Header.SetProtected(-70000, Array(
            Unsigned(1),
            Boolean(true)));
        message.Header.SetUnprotectedByTextLabel(
            "urn:example:hint",
            Text("secondary"));
        message.Ciphertext = Sequence(300, 4);

        TokenBytesResult written = message.Write();
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(written.Bytes);

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
            Assert.That(parsed.Message.Ciphertext,
                Is.EqualTo(message.Ciphertext));
        });
    }

    [Test]
    public void DetachedAndEmptyCiphertextRemainDistinct()
    {
        CoseEncrypt0 detached = Message();
        detached.DetachedCiphertext = true;
        detached.Ciphertext = null;
        CoseEncrypt0 attached = Message();
        attached.Ciphertext = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseEncrypt0Result detachedResult = CoseEncrypt0.Read(
            detachedBytes.Bytes);
        CoseEncrypt0Result attachedResult = CoseEncrypt0.Read(
            attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(detachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(Decode(attachedBytes.Bytes).ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Message.DetachedCiphertext, Is.True);
            Assert.That(detachedResult.Message.Ciphertext, Is.Null);
            Assert.That(detachedResult.Message.Tagged, Is.False);
            Assert.That(attachedResult.Message.DetachedCiphertext, Is.False);
            Assert.That(attachedResult.Message.Ciphertext, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTagControlOverridesAndUpdatesTheModel()
    {
        CoseEncrypt0 message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x83));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes[0], Is.EqualTo(0xD0));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseEncrypt0.Read(untagged.Bytes).Message.Tagged,
                Is.False);
            Assert.That(CoseEncrypt0.Read(tagged.Bytes).Message.Tagged,
                Is.True);
        });
    }

    [Test]
    public void HeaderChangesRegenerateProtectedBytes()
    {
        CoseEncrypt0 message = Message();
        byte[] encoded = message.Write().Bytes;
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(encoded);
        byte[] originalProtected = parsed.Message.Header.ProtectedBytes
            .ToArray();
        parsed.Message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cbor"));

        TokenBytesResult written = parsed.Message.Write();
        CoseEncrypt0Result reparsed = CoseEncrypt0.Read(written.Bytes);

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
    public void ParsingCopiesCiphertextAndProtectedBytes()
    {
        byte[] encoded = Message().Write().Bytes;
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(encoded);
        byte[] ciphertext = parsed.Message.Ciphertext.ToArray();
        byte[] protectedBytes = parsed.Message.Header.ProtectedBytes
            .ToArray();

        System.Array.Fill(encoded, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Message.Ciphertext,
                Is.EqualTo(ciphertext));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(protectedBytes));
        });
    }

    [Test]
    public void WritingDoesNotMutateCallerCiphertext()
    {
        byte[] ciphertext = Sequence(80, 10);
        byte[] original = ciphertext.ToArray();
        CoseEncrypt0 message = Message();
        message.Ciphertext = ciphertext;

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(ciphertext, Is.EqualTo(original));
        });
    }

    [Test]
    public void InvalidMessageAndHeaderStructuresAreRejected()
    {
        CoseEncrypt0Result wrongLength = CoseEncrypt0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Bytes(new byte[] { 2 }))));
        CoseEncrypt0Result wrongProtected = CoseEncrypt0.Read(Encode(Array(
            Map(),
            Map(),
            Bytes(new byte[] { 1 }))));
        CoseEncrypt0Result badProtectedMap = CoseEncrypt0.Read(Encode(Array(
            Bytes(new byte[] { 1 }),
            Map(),
            Bytes(new byte[] { 1 }))));
        CoseEncrypt0Result wrongUnprotected = CoseEncrypt0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Array(),
            Bytes(new byte[] { 1 }))));
        CoseEncrypt0Result wrongCiphertext = CoseEncrypt0.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Boolean(false))));

        Assert.Multiple(() =>
        {
            AssertFailure(
                wrongLength,
                "invalid_cose_message_structure",
                4);
            AssertFailure(
                wrongProtected,
                "invalid_cose_message_structure",
                0);
            AssertFailure(
                badProtectedMap,
                "invalid_cose_protected_header",
                -1);
            AssertFailure(
                wrongUnprotected,
                "invalid_cose_message_structure",
                1);
            AssertFailure(
                wrongCiphertext,
                "invalid_cose_message_structure",
                2);
        });
    }

    [Test]
    public void WrongTagTrailingDataAndInvalidCborAreRejected()
    {
        byte[] valid = Message().Write().Bytes;
        CoseEncrypt0Result wrongTag = CoseEncrypt0.Read(Encode(
            Tag(18, Structure())));
        CoseEncrypt0Result unknownTag = CoseEncrypt0.Read(Encode(
            Tag(999, Structure())));
        CoseEncrypt0Result trailing = CoseEncrypt0.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseEncrypt0Result invalid = CoseEncrypt0.Read(
            new byte[] { 0x9F });

        Assert.Multiple(() =>
        {
            AssertFailure(
                wrongTag,
                "unexpected_cose_message_type",
                0);
            AssertFailure(
                unknownTag,
                "unknown_cose_message_tag",
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
        CoseEncrypt0 message = Message();
        message.Header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(1))
        };
        message.Header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(1))
        };

        TokenBytesResult result = message.Write();

        AssertBytesFailure(result, "duplicate_cose_header_label");
    }

    [Test]
    public void InvalidModelsReturnStableFailures()
    {
        CoseEncrypt0 missingHeader = Message();
        missingHeader.Header = null;
        CoseEncrypt0 missingCiphertext = Message();
        missingCiphertext.Ciphertext = null;
        CoseEncrypt0 conflictingDetached = Message();
        conflictingDetached.DetachedCiphertext = true;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                missingHeader.Write(),
                "invalid_cose_encrypt0_header");
            AssertBytesFailure(
                missingCiphertext.Write(),
                "invalid_cose_encrypt0_ciphertext");
            AssertBytesFailure(
                conflictingDetached.Write(),
                "invalid_cose_encrypt0_ciphertext");
        });
    }

    [Test]
    public void FailedWriteDoesNotCommitTagOrProtectedBytes()
    {
        CoseEncrypt0 message = Message();
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
        CoseEncrypt0 message = Message();
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
        CoseEncrypt0Result exactRead = CoseEncrypt0.Read(
            unrestricted.Bytes,
            exact);
        CoseEncrypt0Result smallRead = CoseEncrypt0.Read(
            unrestricted.Bytes,
            small);
        CoseEncrypt0Result shallowRead = CoseEncrypt0.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseEncrypt0Result invalidRead = CoseEncrypt0.Read(
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
        CoseEncrypt0 message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseEncrypt0.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0.Write(
                message,
                (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0.Write(message, true, null),
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
        CoseEncrypt0 message = new CoseEncrypt0();
        CoseEncrypt0Result result = new CoseEncrypt0Result();

        Assert.Multiple(() =>
        {
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Ciphertext, Is.Empty);
            Assert.That(message.DetachedCiphertext, Is.False);
            Assert.That(message.Tagged, Is.True);
            Assert.That(message.Plaintext, Is.Null);
            Assert.That(message.PlaintextAvailable, Is.False);
            Assert.That(message.AuthenticationTagVerified, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }

    static CoseEncrypt0 Message()
    {
        CoseEncrypt0 message = new CoseEncrypt0();
        message.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.Aes128Gcm));
        message.Header.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(new byte[] { 1, 2 }));
        message.Ciphertext = new byte[] { 3, 4, 5 };
        return message;
    }

    static CborItem Structure()
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }));
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
        CoseEncrypt0Result result,
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
