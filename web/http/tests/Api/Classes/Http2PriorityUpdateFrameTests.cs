namespace http.Tests;

public class Http2PriorityUpdateFrameTests
{
    [Test]
    public void ParsingExtractsRequestIdAndStructuredPriority()
    {
        byte[] fieldValue =
            "u=1, i, custom=(1 \"two\");active, bytes=:YWJj:, display=%\"caf%c3%a9\""u8
                .ToArray();
        byte[] wire = BuildWire(
            0xa5, 0, BuildPayload(3, true, fieldValue));
        wire[5] = 0x80;

        Http2FrameResult result =
            new Http2FrameCodec().ParseHttp2Frame(wire);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + 4 +
                    fieldValue.Length));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.PriorityUpdate));
            Assert.That(result.Frame.StreamId, Is.Zero);
            Assert.That(result.Frame.ReservedBit, Is.True);
            Assert.That(result.Frame.HasPrioritizedElementId, Is.True);
            Assert.That(result.Frame.PrioritizedElementReservedBit, Is.True);
            Assert.That(result.Frame.PrioritizedElementId, Is.EqualTo(3));
            Assert.That(result.Frame.PrioritizedElementIsPush, Is.False);
            Assert.That(result.Frame.PriorityFieldValue,
                Is.EqualTo(fieldValue));
            Assert.That(result.Frame.Priority, Is.Not.Null);
            Assert.That(result.Frame.Priority!.HasUrgency, Is.True);
            Assert.That(result.Frame.Priority.Urgency, Is.EqualTo(1));
            Assert.That(result.Frame.Priority.HasIncremental, Is.True);
            Assert.That(result.Frame.Priority.Incremental, Is.True);
            Assert.That(result.Frame.Priority.UnknownParameters,
                Has.Count.EqualTo(3));
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
        });
    }

    [TestCase(3, false)]
    [TestCase(4, true)]
    public void ParsingClassifiesRequestAndPushStreamIdentifiers(
        int elementId,
        bool isPush)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0,
                BuildPayload(elementId, false, Array.Empty<byte>())));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.PrioritizedElementId,
                Is.EqualTo(elementId));
            Assert.That(result.Frame.PrioritizedElementIsPush,
                Is.EqualTo(isPush));
            Assert.That(result.Frame.PriorityFieldValue, Is.Empty);
            Assert.That(result.Frame.Priority, Is.Not.Null);
            Assert.That(result.Frame.Priority!.Urgency, Is.EqualTo(3));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    public void ParsingRejectsPayloadWithoutCompleteElementId(int length)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, new byte[length]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-priority-update-frame-size"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.Zero);
        });
    }

    [Test]
    public void ParsingAndSerializationRejectNonzeroFrameStream()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 1,
                BuildPayload(3, false, "u=1"u8.ToArray())));
        Http2Frame frame = CreatePriorityUpdateFrame(
            3, "u=1"u8.ToArray());
        frame.StreamId = 1;

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-priority-update-stream-nonzero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-priority-update-stream-nonzero"));
        });
    }

    [Test]
    public void ParsingRejectsZeroElementAsConnectionProtocolError()
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0,
                BuildPayload(0, true, "u=1"u8.ToArray())));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-priority-update-element-zero"));
            Assert.That(result.ErrorPosition,
                Is.EqualTo(Http2Constants.FrameHeaderSize));
            Assert.That(result.Frame!.HasPrioritizedElementId, Is.True);
            Assert.That(result.Frame.PrioritizedElementReservedBit, Is.True);
            Assert.That(result.Frame.PrioritizedElementId, Is.Zero);
        });
    }

    [Test]
    public void ParsingRejectsMalformedOrNonAsciiFieldValues()
    {
        byte[][] malformed =
        [
            "u="u8.ToArray(),
            "U=1"u8.ToArray(),
            "u=1,"u8.ToArray(),
            "display=%\"%c3%28\""u8.ToArray(),
            new byte[] { 0x75, 0x3d, 0xff }
        ];
        Http2FrameCodec codec = new Http2FrameCodec();

        Assert.Multiple(() =>
        {
            foreach (byte[] fieldValue in malformed)
            {
                Http2FrameResult result = codec.ParseHttp2Frame(
                    BuildWire(0, 0, BuildPayload(3, false, fieldValue)));
                Assert.That(result.Status,
                    Is.EqualTo(HttpOperationStatus.Rejected));
                Assert.That(result.ErrorCode,
                    Is.EqualTo("http2-priority-update-field-value"));
                Assert.That(result.ErrorPosition,
                    Is.EqualTo(Http2Constants.FrameHeaderSize + 4));
            }
        });
    }

    [Test]
    public void SerializationPreservesValidatedFieldValueAndClearsReservedBits()
    {
        byte[] fieldValue = "u=1, i"u8.ToArray();
        Http2Frame frame = CreatePriorityUpdateFrame(4, fieldValue);
        frame.Flags = 0xa5;
        frame.ReservedBit = true;
        frame.PrioritizedElementReservedBit = true;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x0a, 0x10, 0xa5,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x04,
                0x75, 0x3d, 0x31, 0x2c, 0x20, 0x69
            }));
            Assert.That(serialized.Written, Is.EqualTo(19));
            Assert.That(serialized.RequiredSize, Is.EqualTo(19));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.PrioritizedElementReservedBit,
                Is.False);
            Assert.That(serialized.Frame.PrioritizedElementIsPush, Is.True);
            Assert.That(serialized.Frame.PriorityFieldValue,
                Is.EqualTo(fieldValue));
            Assert.That(serialized.Frame.Priority, Is.Not.Null);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.PrioritizedElementId, Is.EqualTo(4));
            Assert.That(parsed.Frame.Priority!.Urgency, Is.EqualTo(1));
        });
    }

    [Test]
    public void SerializationRejectsInconsistentElementOrFieldMetadata()
    {
        Http2Frame missingElement = CreatePriorityUpdateFrame(
            3, "u=1"u8.ToArray());
        missingElement.HasPrioritizedElementId = false;
        Http2Frame zeroElement = CreatePriorityUpdateFrame(
            0, "u=1"u8.ToArray());
        Http2Frame wrongKind = CreatePriorityUpdateFrame(
            4, "u=1"u8.ToArray());
        wrongKind.PrioritizedElementIsPush = false;
        Http2Frame malformedField = CreatePriorityUpdateFrame(
            3, "u="u8.ToArray());
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult missingResult =
            codec.SerializeHttp2Frame(missingElement);
        Http2FrameResult zeroResult = codec.SerializeHttp2Frame(zeroElement);
        Http2FrameResult kindResult = codec.SerializeHttp2Frame(wrongKind);
        Http2FrameResult fieldResult =
            codec.SerializeHttp2Frame(malformedField);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("invalid-http2-priority-update-element"));
            Assert.That(zeroResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(zeroResult.ErrorCode,
                Is.EqualTo("invalid-http2-priority-update-element"));
            Assert.That(kindResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(kindResult.ErrorCode,
                Is.EqualTo("invalid-http2-priority-update-element"));
            Assert.That(fieldResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(fieldResult.ErrorCode,
                Is.EqualTo("invalid-http2-priority-update-field-value"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame exactLimit = CreatePriorityUpdateFrame(
            3, "u=1"u8.ToArray());
        Http2Frame tooLarge = CreatePriorityUpdateFrame(
            3, "u=1, i"u8.ToArray());
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 7;
        limited.peerMaximumFrameSize = 7;
        byte[] exact = new byte[19];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 18).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult accepted = limited.SerializeHttp2Frame(exactLimit);
        Http2FrameResult rejected = limited.SerializeHttp2Frame(tooLarge);
        Http2FrameResult written = codec.WriteHttp2Frame(tooLarge, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(tooLarge, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.Frame!.Length, Is.EqualTo(7));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(19));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact[13..19], Is.EqualTo("u=1, i"u8.ToArray()));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(19));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 18).ToArray()));
        });
    }

    static Http2Frame CreatePriorityUpdateFrame(
        int elementId,
        byte[] fieldValue)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = elementId;
        frame.PrioritizedElementIsPush = elementId > 0 && elementId % 2 == 0;
        frame.PriorityFieldValue = fieldValue;
        return frame;
    }

    static byte[] BuildPayload(
        int elementId,
        bool reservedBit,
        byte[] fieldValue)
    {
        byte[] payload = new byte[4 + fieldValue.Length];
        payload[0] = (byte)(elementId >> 24);
        if (reservedBit)
        {
            payload[0] = (byte)(payload[0] | 0x80);
        }

        payload[1] = (byte)(elementId >> 16);
        payload[2] = (byte)(elementId >> 8);
        payload[3] = (byte)elementId;
        fieldValue.CopyTo(payload, 4);
        return payload;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x10;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
