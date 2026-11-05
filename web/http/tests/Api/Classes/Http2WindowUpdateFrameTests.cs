namespace http.Tests;

public class Http2WindowUpdateFrameTests
{
    [TestCase(0)]
    [TestCase(0x01234567)]
    public void ParsingPreservesConnectionAndStreamTargets(int streamId)
    {
        byte[] payload = [0x00, 0x00, 0x04, 0x00];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0xa5, streamId, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(13));
            Assert.That(result.RequiredSize, Is.EqualTo(13));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.WindowUpdate));
            Assert.That(result.Frame.StreamId, Is.EqualTo(streamId));
            Assert.That(result.Frame.HasWindowIncrement, Is.True);
            Assert.That(result.Frame.WindowIncrement, Is.EqualTo(1024));
            Assert.That(result.Frame.WindowIncrementReservedBit, Is.False);
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParsingMasksReservedBitWithoutSignedOverflow()
    {
        byte[] wire = BuildWire(
            0x5a,
            1,
            new byte[] { 0xff, 0xff, 0xff, 0xff });
        wire[5] = 0x80;
        wire[8] = 0x01;

        Http2FrameResult result =
            new Http2FrameCodec().ParseHttp2Frame(wire);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.ReservedBit, Is.True);
            Assert.That(result.Frame.StreamId, Is.EqualTo(1));
            Assert.That(result.Frame.WindowIncrementReservedBit, Is.True);
            Assert.That(result.Frame.WindowIncrement,
                Is.EqualTo(Http2Constants.MaximumWindowSize));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(5)]
    public void ParsingRejectsEveryNonFourBytePayload(int payloadLength)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, new byte[payloadLength]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-window-update-frame-size"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    public void ParsingRejectsZeroIncrementForEitherTarget(int streamId)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, streamId,
                new byte[] { 0x80, 0x00, 0x00, 0x00 }));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-window-update-zero-increment"));
            Assert.That(result.ErrorPosition,
                Is.EqualTo(Http2Constants.FrameHeaderSize));
            Assert.That(result.Frame!.StreamId, Is.EqualTo(streamId));
            Assert.That(result.Frame.HasWindowIncrement, Is.True);
            Assert.That(result.Frame.WindowIncrementReservedBit, Is.True);
            Assert.That(result.Frame.WindowIncrement, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(0x01234567)]
    public void SerializationEmitsCanonicalTargetAndIncrement(int streamId)
    {
        Http2Frame frame = CreateWindowUpdateFrame(
            streamId, Http2Constants.MaximumWindowSize);
        frame.Flags = 0xa5;
        frame.ReservedBit = true;
        frame.WindowIncrementReservedBit = true;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes[0..5], Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x04, 0x08, 0xa5
            }));
            Assert.That(serialized.Bytes[5..9], Is.EqualTo(new byte[]
            {
                (byte)(streamId >> 24),
                (byte)(streamId >> 16),
                (byte)(streamId >> 8),
                (byte)streamId
            }));
            Assert.That(serialized.Bytes[9..13], Is.EqualTo(new byte[]
            {
                0x7f, 0xff, 0xff, 0xff
            }));
            Assert.That(serialized.Written, Is.EqualTo(13));
            Assert.That(serialized.RequiredSize, Is.EqualTo(13));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.WindowIncrementReservedBit, Is.False);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.StreamId, Is.EqualTo(streamId));
            Assert.That(parsed.Frame.WindowIncrement,
                Is.EqualTo(Http2Constants.MaximumWindowSize));
        });
    }

    [Test]
    public void SerializationRejectsInvalidTargetOrIncrementMetadata()
    {
        Http2Frame invalidTarget = CreateWindowUpdateFrame(-1, 1);
        Http2Frame missingIncrement = CreateWindowUpdateFrame(0, 1);
        missingIncrement.HasWindowIncrement = false;
        Http2Frame zeroIncrement = CreateWindowUpdateFrame(0, 0);
        Http2Frame negativeIncrement = CreateWindowUpdateFrame(0, -1);
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult targetResult =
            codec.SerializeHttp2Frame(invalidTarget);
        Http2FrameResult missingResult =
            codec.SerializeHttp2Frame(missingIncrement);
        Http2FrameResult zeroResult =
            codec.SerializeHttp2Frame(zeroIncrement);
        Http2FrameResult negativeResult =
            codec.SerializeHttp2Frame(negativeIncrement);

        Assert.Multiple(() =>
        {
            Assert.That(targetResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(targetResult.ErrorCode,
                Is.EqualTo("invalid-http2-window-update-stream"));
            Assert.That(missingResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("invalid-http2-window-update-increment"));
            Assert.That(zeroResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(zeroResult.ErrorCode,
                Is.EqualTo("invalid-http2-window-update-increment"));
            Assert.That(negativeResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(negativeResult.ErrorCode,
                Is.EqualTo("invalid-http2-window-update-increment"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame frame = CreateWindowUpdateFrame(3, 1024);
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 3;
        limited.peerMaximumFrameSize = 3;
        byte[] exact = new byte[13];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 12).ToArray();
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
            Assert.That(written.Written, Is.EqualTo(13));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x04, 0x08, 0x00,
                0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x04, 0x00
            }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(13));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 12).ToArray()));
        });
    }

    static Http2Frame CreateWindowUpdateFrame(int streamId, int increment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = streamId;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = increment;
        return frame;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x08;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
