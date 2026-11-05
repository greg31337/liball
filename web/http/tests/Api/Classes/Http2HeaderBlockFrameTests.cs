namespace http.Tests;

public class Http2HeaderBlockFrameTests
{
    [Test]
    public void HeadersParsingExtractsFlagsPriorityFragmentAndPadding()
    {
        byte[] payload =
        [
            0x02,
            0x80, 0x00, 0x00, 0x03, 0xff,
            0x82, 0x84,
            0xaa, 0xbb
        ];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x01, 0x2d, 5, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(19));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Headers));
            Assert.That(result.Frame.EndStream, Is.True);
            Assert.That(result.Frame.EndHeaders, Is.True);
            Assert.That(result.Frame.IsPadded, Is.True);
            Assert.That(result.Frame.PaddingLength, Is.EqualTo(2));
            Assert.That(result.Frame.HasPriority, Is.True);
            Assert.That(result.Frame.ExclusiveDependency, Is.True);
            Assert.That(result.Frame.StreamDependency, Is.EqualTo(3));
            Assert.That(result.Frame.Weight, Is.EqualTo(256));
            Assert.That(result.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82, 0x84 }));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void HeadersParsingPermitsEmptyFragmentAndZeroPadding()
    {
        Http2FrameResult empty = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x01, 0x08, 1, new byte[] { 0x00 }));
        Http2FrameResult priorityOnly = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x01, 0x20, 3,
                new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Frame!.HeaderBlockFragment, Is.Empty);
            Assert.That(empty.Frame.PaddingLength, Is.Zero);
            Assert.That(priorityOnly.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(priorityOnly.Frame!.HeaderBlockFragment, Is.Empty);
            Assert.That(priorityOnly.Frame.Weight, Is.EqualTo(1));
        });
    }

    [Test]
    public void ContinuationParsingUsesEntirePayloadAsFragment()
    {
        byte[] payload = [0x82, 0x84, 0xff];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x09, 0x85, 7, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Continuation));
            Assert.That(result.Frame.EndHeaders, Is.True);
            Assert.That(result.Frame.EndStream, Is.False);
            Assert.That(result.Frame.IsPadded, Is.False);
            Assert.That(result.Frame.HasPriority, Is.False);
            Assert.That(result.Frame.HeaderBlockFragment, Is.EqualTo(payload));
            Assert.That(result.Frame.Flags, Is.EqualTo(0x85));
        });
    }

    [Test]
    public void MalformedHeadersOptionalSectionsAreRejected()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult missingPadLength = codec.ParseHttp2Frame(
            BuildWire(0x01, 0x08, 1, Array.Empty<byte>()));
        Http2FrameResult excessivePadding = codec.ParseHttp2Frame(
            BuildWire(0x01, 0x08, 1, new byte[] { 0x02, 0xaa }));
        Http2FrameResult shortPriority = codec.ParseHttp2Frame(
            BuildWire(0x01, 0x20, 1,
                new byte[] { 0x00, 0x00, 0x00, 0x01 }));

        Assert.Multiple(() =>
        {
            Assert.That(missingPadLength.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingPadLength.ErrorCode,
                Is.EqualTo("http2-headers-pad-length-missing"));
            Assert.That(excessivePadding.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(excessivePadding.ErrorCode,
                Is.EqualTo("http2-headers-padding-invalid"));
            Assert.That(shortPriority.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(shortPriority.ErrorCode,
                Is.EqualTo("http2-headers-priority-truncated"));
        });
    }

    [TestCase((byte)0x01, Http2FrameType.Headers)]
    [TestCase((byte)0x09, Http2FrameType.Continuation)]
    public void HeaderBlockFramesRejectStreamZero(
        byte typeCode,
        Http2FrameType frameType)
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(typeCode, 0, 0, Array.Empty<byte>()));
        Http2Frame frame = CreateFrame(frameType, 0, Array.Empty<byte>());
        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-header-block-stream-zero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-header-block-stream-zero"));
        });
    }

    [Test]
    public void HeadersRejectsSelfDependencyDuringParsingAndSerialization()
    {
        byte[] priority = [0x00, 0x00, 0x00, 0x03, 0x0f];
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x01, 0x20, 3, priority));
        Http2Frame frame = CreateFrame(
            Http2FrameType.Headers, 3, Array.Empty<byte>());
        frame.HasPriority = true;
        frame.StreamDependency = 3;
        frame.Weight = 16;

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-headers-self-dependency"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("invalid-http2-headers-priority"));
        });
    }

    [Test]
    public void HeadersSerializationEmitsCanonicalOptionalSections()
    {
        Http2Frame frame = CreateFrame(
            Http2FrameType.Headers, 5, new byte[] { 0x82, 0x84 });
        frame.Flags = 0x42;
        frame.EndStream = true;
        frame.EndHeaders = true;
        frame.IsPadded = true;
        frame.PaddingLength = 2;
        frame.HasPriority = true;
        frame.ExclusiveDependency = true;
        frame.StreamDependency = 3;
        frame.Weight = 256;
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
                0x00, 0x00, 0x0a, 0x01, 0x6f,
                0x00, 0x00, 0x00, 0x05,
                0x02,
                0x80, 0x00, 0x00, 0x03, 0xff,
                0x82, 0x84,
                0x00, 0x00
            }));
            Assert.That(serialized.RequiredSize, Is.EqualTo(19));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82, 0x84 }));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.ExclusiveDependency, Is.True);
            Assert.That(parsed.Frame.StreamDependency, Is.EqualTo(3));
            Assert.That(parsed.Frame.Weight, Is.EqualTo(256));
        });
    }

    [Test]
    public void ContinuationSerializationPreservesUnknownFlags()
    {
        Http2Frame frame = CreateFrame(
            Http2FrameType.Continuation, 7, new byte[] { 0x82, 0x84 });
        frame.Flags = 0x81;
        frame.EndHeaders = true;

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x02, 0x09, 0x85,
                0x00, 0x00, 0x00, 0x07,
                0x82, 0x84
            }));
            Assert.That(result.Frame!.EndHeaders, Is.True);
            Assert.That(result.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82, 0x84 }));
        });
    }

    [Test]
    public void SerializationHandlesMultiOctetLengthAndStreamIdentifier()
    {
        Http2Frame frame = CreateFrame(
            Http2FrameType.Continuation,
            0x01234567,
            Enumerable.Repeat((byte)0x82, 300).ToArray());

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes[0..3],
                Is.EqualTo(new byte[] { 0x00, 0x01, 0x2c }));
            Assert.That(result.Bytes[5..9],
                Is.EqualTo(new byte[] { 0x01, 0x23, 0x45, 0x67 }));
            Assert.That(result.RequiredSize, Is.EqualTo(309));
        });
    }

    [Test]
    public void SerializationValidatesMetadataAndWholePayloadSize()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.configuredMaximumFrameSize = 7;
        codec.peerMaximumFrameSize = 7;
        Http2Frame exact = CreateFrame(
            Http2FrameType.Headers, 1, Array.Empty<byte>());
        exact.IsPadded = true;
        exact.PaddingLength = 1;
        exact.HasPriority = true;
        exact.Weight = 1;
        Http2Frame tooLarge = CreateFrame(
            Http2FrameType.Headers, 1, new byte[] { 0x82 });
        tooLarge.IsPadded = true;
        tooLarge.PaddingLength = 1;
        tooLarge.HasPriority = true;
        tooLarge.Weight = 1;
        Http2Frame badContinuation = CreateFrame(
            Http2FrameType.Continuation, 1, Array.Empty<byte>());
        badContinuation.IsPadded = true;

        Http2FrameResult exactResult = codec.SerializeHttp2Frame(exact);
        Http2FrameResult largeResult = codec.SerializeHttp2Frame(tooLarge);
        Http2FrameResult continuationResult =
            codec.SerializeHttp2Frame(badContinuation);

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exactResult.Frame!.Length, Is.EqualTo(7));
            Assert.That(largeResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(largeResult.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(continuationResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(continuationResult.ErrorCode,
                Is.EqualTo("invalid-http2-continuation-frame"));
        });
    }

    [Test]
    public void BoundedWriteSupportsHeadersWithoutPartialOutput()
    {
        Http2Frame frame = CreateFrame(
            Http2FrameType.Headers, 1, new byte[] { 0x82, 0x84 });
        frame.EndHeaders = true;
        byte[] exact = new byte[11];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 10).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult written = codec.WriteHttp2Frame(frame, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(frame, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(11));
            Assert.That(exact, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x02, 0x01, 0x04,
                0x00, 0x00, 0x00, 0x01,
                0x82, 0x84
            }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(11));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 10).ToArray()));
        });
    }

    static Http2Frame CreateFrame(
        Http2FrameType frameType,
        int streamId,
        byte[] fragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = frameType;
        frame.StreamId = streamId;
        frame.HeaderBlockFragment = fragment;
        return frame;
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
