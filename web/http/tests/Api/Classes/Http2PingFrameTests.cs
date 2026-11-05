namespace http.Tests;

public class Http2PingFrameTests
{
    [TestCase(false, (byte)0xa0)]
    [TestCase(true, (byte)0xa1)]
    public void ParsingPreservesAcknowledgementAndCorrelationData(
        bool acknowledgement,
        byte flags)
    {
        byte[] correlationData =
            [0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef];
        byte[] wire = BuildWire(flags, 0, correlationData);
        wire[5] = 0x80;

        Http2FrameResult result =
            new Http2FrameCodec().ParseHttp2Frame(wire);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(17));
            Assert.That(result.RequiredSize, Is.EqualTo(17));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Ping));
            Assert.That(result.Frame.StreamId, Is.Zero);
            Assert.That(result.Frame.ReservedBit, Is.True);
            Assert.That(result.Frame.Acknowledgement,
                Is.EqualTo(acknowledgement));
            Assert.That(result.Frame.OpaqueData,
                Is.EqualTo(correlationData));
            Assert.That(result.Frame.Payload, Is.EqualTo(correlationData));
            Assert.That(result.Frame.Flags, Is.EqualTo(flags));
        });
    }

    [TestCase(0)]
    [TestCase(7)]
    [TestCase(9)]
    public void ParsingRejectsEveryNonEightBytePayload(int payloadLength)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, new byte[payloadLength]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-ping-frame-size"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectNonzeroStream()
    {
        byte[] correlationData =
            [0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef];
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 1, correlationData));
        Http2Frame frame = CreatePingFrame(correlationData);
        frame.StreamId = 1;

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-ping-stream-nonzero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-ping-stream-nonzero"));
        });
    }

    [TestCase(false, (byte)0xfe)]
    [TestCase(true, (byte)0xff)]
    public void SerializationEmitsCanonicalFrameAndPreservesUnknownFlags(
        bool acknowledgement,
        byte expectedFlags)
    {
        byte[] correlationData =
            [0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef];
        Http2Frame frame = CreatePingFrame(correlationData);
        frame.Flags = 0xff;
        frame.Acknowledgement = acknowledgement;
        frame.ReservedBit = true;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x08, 0x06, expectedFlags,
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            }));
            Assert.That(serialized.Written, Is.EqualTo(17));
            Assert.That(serialized.RequiredSize, Is.EqualTo(17));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.Acknowledgement,
                Is.EqualTo(acknowledgement));
            Assert.That(serialized.Frame.OpaqueData,
                Is.EqualTo(correlationData));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.Acknowledgement,
                Is.EqualTo(acknowledgement));
            Assert.That(parsed.Frame.OpaqueData,
                Is.EqualTo(correlationData));
        });
    }

    [TestCase(0)]
    [TestCase(7)]
    [TestCase(9)]
    public void SerializationRejectsNonEightByteCorrelationData(int length)
    {
        Http2Frame frame = CreatePingFrame(new byte[length]);

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http2-ping-data"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        byte[] correlationData =
            [0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef];
        Http2Frame frame = CreatePingFrame(correlationData);
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 7;
        limited.peerMaximumFrameSize = 7;
        byte[] exact = new byte[17];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 16).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult rejected = limited.SerializeHttp2Frame(frame);
        Http2FrameResult written = codec.WriteHttp2Frame(frame, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(frame, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(17));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact[9..17], Is.EqualTo(correlationData));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(17));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 16).ToArray()));
        });
    }

    static Http2Frame CreatePingFrame(byte[] correlationData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.OpaqueData = correlationData;
        return frame;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x06;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
