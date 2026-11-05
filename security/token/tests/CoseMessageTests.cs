using token;

namespace token.Tests;

public class CoseMessageTests
{
    [TestCase(CoseMessageKind.Encrypt0, 16UL)]
    [TestCase(CoseMessageKind.Mac0, 17UL)]
    [TestCase(CoseMessageKind.Sign1, 18UL)]
    [TestCase(CoseMessageKind.Encrypt, 96UL)]
    [TestCase(CoseMessageKind.Mac, 97UL)]
    [TestCase(CoseMessageKind.Sign, 98UL)]
    public void RegisteredTagsIdentifyEveryCoseMessageKind(
        CoseMessageKind kind,
        ulong tag)
    {
        CborItem message = Message(kind);

        CoseMessageResult result = CoseMessage.Read(
            Encode(Tag(tag, message)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.Tagged, Is.True);
            Assert.That(result.Tag, Is.EqualTo(tag));
            Assert.That(result.Item.Kind, Is.EqualTo(TokenCborValueKind.Array));
            Assert.That(result.Item.ArrayValue,
                Has.Length.EqualTo(message.ArrayValue.Length));
        });
    }

    [TestCase(CoseMessageKind.Encrypt0)]
    [TestCase(CoseMessageKind.Mac0)]
    [TestCase(CoseMessageKind.Sign1)]
    [TestCase(CoseMessageKind.Encrypt)]
    [TestCase(CoseMessageKind.Mac)]
    [TestCase(CoseMessageKind.Sign)]
    public void ContextIdentifiesEveryUntaggedCoseMessageKind(
        CoseMessageKind kind)
    {
        CoseMessageResult result = CoseMessage.Read(
            Encode(Message(kind)),
            kind);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.Tagged, Is.False);
            Assert.That(result.Tag, Is.Zero);
            Assert.That(result.Item, Is.Not.Null);
        });
    }

    [Test]
    public void UntaggedMessageWithoutContextIsRejected()
    {
        CoseMessageResult result = CoseMessage.Read(
            Encode(Message(CoseMessageKind.Sign1)));

        AssertFailure(
            result,
            "untagged_cose_message_type_required",
            0);
    }

    [Test]
    public void TaggedMessageMustMatchContext()
    {
        CoseMessageResult result = CoseMessage.Read(
            Encode(Tag(18, Message(CoseMessageKind.Sign1))),
            CoseMessageKind.Mac0);

        AssertFailure(result, "unexpected_cose_message_type", 0);
    }

    [Test]
    public void UnknownCborTagIsRejected()
    {
        CoseMessageResult result = CoseMessage.Read(
            Encode(Tag(999, Message(CoseMessageKind.Sign1))));

        AssertFailure(result, "unknown_cose_message_tag", 0);
    }

    [Test]
    public void MessageMustHaveTheExpectedTopLevelShape()
    {
        CoseMessageResult wrongLength = CoseMessage.Read(
            Encode(Array(Bytes(new byte[0]), Map(), Bytes(new byte[0]))),
            CoseMessageKind.Sign1);
        CoseMessageResult wrongProtected = CoseMessage.Read(
            Encode(Array(Map(), Map(), Bytes(new byte[0]), Bytes(new byte[1]))),
            CoseMessageKind.Sign1);
        CoseMessageResult wrongContent = CoseMessage.Read(
            Encode(Array(Bytes(new byte[0]), Map(), Boolean(true), Bytes(new byte[1]))),
            CoseMessageKind.Sign1);
        CoseMessageResult emptyRecipients = CoseMessage.Read(
            Encode(Array(Bytes(new byte[0]), Map(), Null(), Array())),
            CoseMessageKind.Encrypt);
        CoseMessageResult wrongMac = CoseMessage.Read(
            Encode(Array(
                Bytes(new byte[0]),
                Map(),
                Null(),
                Boolean(true),
                Array(Null()))),
            CoseMessageKind.Mac);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongLength, "invalid_cose_message_structure", 3);
            AssertFailure(wrongProtected, "invalid_cose_message_structure", 0);
            AssertFailure(wrongContent, "invalid_cose_message_structure", 2);
            AssertFailure(emptyRecipients, "invalid_cose_message_structure", 3);
            AssertFailure(wrongMac, "invalid_cose_message_structure", 3);
        });
    }

    [Test]
    public void TaggedValueMustBeAMessageArray()
    {
        CoseMessageResult result = CoseMessage.Read(
            Encode(Tag(16, Map())));

        AssertFailure(result, "cose_message_not_array", 0);
    }

    [Test]
    public void InvalidAndTrailingCborReturnStableFailures()
    {
        byte[] valid = Encode(Tag(16, Message(CoseMessageKind.Encrypt0)));
        byte[] trailing = valid.Concat(new byte[] { 0 }).ToArray();

        CoseMessageResult invalidResult = CoseMessage.Read(new byte[] { 0x9F });
        CoseMessageResult trailingResult = CoseMessage.Read(trailing);

        Assert.Multiple(() =>
        {
            AssertFailure(invalidResult, "unexpected_end_of_cbor", 1);
            AssertFailure(
                trailingResult,
                "trailing_cose_message_data",
                valid.Length);
        });
    }

    [Test]
    public void SizeDepthAndPolicyLimitsAreEnforced()
    {
        byte[] encoded = Encode(
            Tag(16, Message(CoseMessageKind.Encrypt0)));
        TokenPolicy size = new TokenPolicy();
        size.MaximumTokenSizeInBytes = encoded.Length - 1;
        TokenPolicy depth = new TokenPolicy();
        depth.MaximumCborDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        CoseMessageResult sizeResult = CoseMessage.Read(encoded, size);
        CoseMessageResult depthResult = CoseMessage.Read(encoded, depth);
        CoseMessageResult invalidResult = CoseMessage.Read(encoded, invalid);

        Assert.Multiple(() =>
        {
            AssertFailure(sizeResult, "cbor_size_exceeded", 6);
            AssertFailure(depthResult, "cbor_depth_exceeded", 1);
            AssertFailure(invalidResult, "invalid_policy", -1);
        });
    }

    [Test]
    public void PublicOperationsRejectInvalidArguments()
    {
        byte[] message = Encode(
            Tag(16, Message(CoseMessageKind.Encrypt0)));

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseMessage.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMessage.Read(message, (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMessage.Read(
                    message,
                    CoseMessageKind.Encrypt0,
                    null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMessage.Read(
                    message,
                    (CoseMessageKind)999),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ResultDefaultsToNoDetectedMessage()
    {
        CoseMessageResult result = new CoseMessageResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(CoseMessageKind.Unknown));
            Assert.That(result.Tagged, Is.False);
            Assert.That(result.Tag, Is.Zero);
            Assert.That(result.Item, Is.Null);
        });
    }

    static CborItem Message(CoseMessageKind kind)
    {
        CborItem protectedBucket = Bytes(new byte[0]);
        CborItem unprotectedBucket = Map();
        CborItem content = Bytes(new byte[] { 1, 2 });
        if (kind == CoseMessageKind.Encrypt0)
        {
            return Array(protectedBucket, unprotectedBucket, content);
        }

        if (kind == CoseMessageKind.Sign1
            || kind == CoseMessageKind.Mac0)
        {
            return Array(
                protectedBucket,
                unprotectedBucket,
                content,
                Bytes(new byte[] { 3, 4 }));
        }

        if (kind == CoseMessageKind.Mac)
        {
            return Array(
                protectedBucket,
                unprotectedBucket,
                content,
                Bytes(new byte[] { 3, 4 }),
                Array(Null()));
        }

        return Array(
            protectedBucket,
            unprotectedBucket,
            content,
            Array(Null()));
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteItem(item);
        Assert.That(written.Success, Is.True);
        return writer.ToArray();
    }

    static CborItem Tag(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Null()
    {
        return new CborItem();
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

    static void AssertFailure(
        CoseMessageResult result,
        string code,
        int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
        Assert.That(result.Kind, Is.EqualTo(CoseMessageKind.Unknown));
        Assert.That(result.Item, Is.Null);
    }
}
