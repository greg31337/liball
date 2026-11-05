namespace http.Tests;

public class Http2PriorityFrameTests
{
    [TestCase((byte)0x00, 1)]
    [TestCase((byte)0xff, 256)]
    public void ParsingMapsWireWeightAcrossItsFullRange(
        byte wireWeight,
        int expectedWeight)
    {
        byte[] payload = [0x80, 0x00, 0x00, 0x03, wireWeight];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildPriorityWire(0xa5, 5, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(14));
            Assert.That(result.RequiredSize, Is.EqualTo(14));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Priority));
            Assert.That(result.Frame.HasPriority, Is.True);
            Assert.That(result.Frame.ExclusiveDependency, Is.True);
            Assert.That(result.Frame.StreamDependency, Is.EqualTo(3));
            Assert.That(result.Frame.Weight, Is.EqualTo(expectedWeight));
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
            Assert.That(result.Frame.PriorityIgnored, Is.False);
        });
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(6)]
    public void ParsingRejectsEveryNonFiveBytePayloadLength(int payloadLength)
    {
        byte[] payload = new byte[payloadLength];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildPriorityWire(0, 1, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-priority-frame-size"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectStreamZero()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildPriorityWire(0, 0,
                new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }));
        Http2Frame frame = CreatePriorityFrame(0, 1, 1);

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-priority-stream-zero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-priority-stream-zero"));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectSelfDependency()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildPriorityWire(0, 3,
                new byte[] { 0x80, 0x00, 0x00, 0x03, 0x0f }));
        Http2Frame frame = CreatePriorityFrame(3, 3, 16);

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-priority-self-dependency"));
            Assert.That(parsed.Frame!.ExclusiveDependency, Is.True);
            Assert.That(parsed.Frame.Weight, Is.EqualTo(16));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("invalid-http2-priority-frame"));
        });
    }

    [Test]
    public void NegotiatedIgnorePolicyPreservesDecodedMetadata()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.ignoreRfc7540Priorities = true;

        Http2FrameResult priority = codec.ParseHttp2Frame(
            BuildPriorityWire(0, 5,
                new byte[] { 0x00, 0x00, 0x00, 0x03, 0x1f }));
        Http2FrameResult headers = codec.ParseHttp2Frame(
            BuildWire(0x01, 0x20, 5,
                new byte[] { 0x00, 0x00, 0x00, 0x03, 0x1f, 0x82 }));
        Http2FrameResult serializedHeaders =
            codec.SerializeHttp2Frame(headers.Frame!);

        Assert.Multiple(() =>
        {
            Assert.That(priority.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(priority.Frame!.PriorityIgnored, Is.True);
            Assert.That(priority.Frame.StreamDependency, Is.EqualTo(3));
            Assert.That(priority.Frame.Weight, Is.EqualTo(32));
            Assert.That(headers.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(headers.Frame!.PriorityIgnored, Is.True);
            Assert.That(headers.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82 }));
            Assert.That(serializedHeaders.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serializedHeaders.Frame!.PriorityIgnored, Is.True);
        });
    }

    [Test]
    public void SerializationEmitsExactCanonicalPayloadAndPreservesFlags()
    {
        Http2Frame frame = CreatePriorityFrame(0x01234567, 0x07654321, 256);
        frame.ExclusiveDependency = true;
        frame.Flags = 0xa5;
        frame.ReservedBit = true;
        frame.PriorityIgnored = true;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x05, 0x02, 0xa5,
                0x01, 0x23, 0x45, 0x67,
                0x87, 0x65, 0x43, 0x21, 0xff
            }));
            Assert.That(serialized.Written, Is.EqualTo(14));
            Assert.That(serialized.RequiredSize, Is.EqualTo(14));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.HasPriority, Is.True);
            Assert.That(serialized.Frame.PriorityIgnored, Is.True);
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.StreamDependency,
                Is.EqualTo(0x07654321));
            Assert.That(parsed.Frame.Weight, Is.EqualTo(256));
        });
    }

    [TestCase(-1, 1)]
    [TestCase(0x7fffffff, 0)]
    [TestCase(1, 0)]
    [TestCase(1, 257)]
    public void SerializationRejectsInvalidDependencyOrWeight(
        int dependency,
        int weight)
    {
        Http2Frame frame = CreatePriorityFrame(3, dependency, weight);

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http2-priority-frame"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame frame = CreatePriorityFrame(3, 1, 16);
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 4;
        limited.peerMaximumFrameSize = 4;
        byte[] exact = new byte[14];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 13).ToArray();
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
            Assert.That(written.Written, Is.EqualTo(14));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.RequiredSize, Is.EqualTo(14));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 13).ToArray()));
        });
    }

    static Http2Frame CreatePriorityFrame(
        int streamId,
        int dependency,
        int weight)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Priority;
        frame.StreamId = streamId;
        frame.StreamDependency = dependency;
        frame.Weight = weight;
        return frame;
    }

    static byte[] BuildPriorityWire(byte flags, int streamId, byte[] payload)
    {
        return BuildWire(0x02, flags, streamId, payload);
    }

    static byte[] BuildWire(
        byte typeCode,
        byte flags,
        int streamId,
        byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = typeCode;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
