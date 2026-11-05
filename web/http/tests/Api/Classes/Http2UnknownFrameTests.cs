namespace http.Tests;

public class Http2UnknownFrameTests
{
    [TestCase((byte)0x0a)]
    [TestCase((byte)0x0b)]
    [TestCase((byte)0x0d)]
    [TestCase((byte)0xfe)]
    [TestCase((byte)0xff)]
    public void ParsingSafelyReturnsEveryUnregisteredType(byte typeCode)
    {
        byte[] input = BuildWire(
            typeCode,
            0xa5,
            5,
            true,
            new byte[] { 0xaa, 0xbb },
            1);

        Http2FrameResult result =
            new Http2FrameCodec().ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(11));
            Assert.That(result.RequiredSize, Is.EqualTo(11));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Unknown));
            Assert.That(result.Frame.TypeCode, Is.EqualTo(typeCode));
            Assert.That(result.Frame.IsKnownType, Is.False);
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
            Assert.That(result.Frame.ReservedBit, Is.True);
            Assert.That(result.Frame.StreamId, Is.EqualTo(5));
            Assert.That(result.Frame.Payload,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        });
    }

    [Test]
    public void ParsedUnknownFrameRoundTripsWithCanonicalReservedBit()
    {
        byte[] payload = [0x00, 0x7f, 0x80, 0xff];
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult parsed = codec.ParseHttp2Frame(BuildWire(
            0xfe,
            0xa5,
            Http2Constants.MaximumStreamIdentifier,
            true,
            payload,
            0));

        Http2FrameResult serialized =
            codec.SerializeHttp2Frame(parsed.Frame!);
        Http2FrameResult reparsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x04, 0xfe, 0xa5,
                0x7f, 0xff, 0xff, 0xff,
                0x00, 0x7f, 0x80, 0xff
            }));
            Assert.That(serialized.Written, Is.EqualTo(13));
            Assert.That(serialized.RequiredSize, Is.EqualTo(13));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.TypeCode, Is.EqualTo(0xfe));
            Assert.That(serialized.Frame.IsKnownType, Is.False);
            Assert.That(serialized.Frame.Payload, Is.EqualTo(payload));
            Assert.That(reparsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reparsed.Frame!.TypeCode, Is.EqualTo(0xfe));
            Assert.That(reparsed.Frame.Flags, Is.EqualTo(0xa5));
            Assert.That(reparsed.Frame.StreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(reparsed.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void SerializationAllowsEmptyConnectionFrame()
    {
        Http2Frame frame = CreateUnknownFrame(
            0x0a, 0, Array.Empty<byte>());

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x00, 0x0a, 0x00,
                0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(result.Frame!.Length, Is.Zero);
            Assert.That(result.Frame.Payload, Is.Empty);
        });
    }

    [Test]
    public void SerializationEncodesMultiOctetPayloadLength()
    {
        byte[] payload = Enumerable.Repeat((byte)0xaa, 300).ToArray();
        Http2Frame frame = CreateUnknownFrame(0xfe, 0x01234567, payload);

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes[0..3],
                Is.EqualTo(new byte[] { 0x00, 0x01, 0x2c }));
            Assert.That(result.Bytes[5..9],
                Is.EqualTo(new byte[] { 0x01, 0x23, 0x45, 0x67 }));
            Assert.That(result.Bytes[9..], Is.EqualTo(payload));
            Assert.That(result.RequiredSize, Is.EqualTo(309));
        });
    }

    [Test]
    public void SerializationRejectsInconsistentTypeAndStreamMetadata()
    {
        Http2Frame registeredType = CreateUnknownFrame(
            0x06, 0, Array.Empty<byte>());
        Http2Frame markedKnown = CreateUnknownFrame(
            0xfe, 0, Array.Empty<byte>());
        markedKnown.IsKnownType = true;
        Http2Frame invalidStream = CreateUnknownFrame(
            0xfe, -1, Array.Empty<byte>());
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult registeredResult =
            codec.SerializeHttp2Frame(registeredType);
        Http2FrameResult knownResult = codec.SerializeHttp2Frame(markedKnown);
        Http2FrameResult streamResult =
            codec.SerializeHttp2Frame(invalidStream);

        Assert.Multiple(() =>
        {
            Assert.That(registeredResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(registeredResult.ErrorCode,
                Is.EqualTo("invalid-http2-unknown-frame-type"));
            Assert.That(knownResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(knownResult.ErrorCode,
                Is.EqualTo("invalid-http2-unknown-frame-type"));
            Assert.That(streamResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(streamResult.ErrorCode,
                Is.EqualTo("invalid-http2-unknown-frame-stream"));
        });
    }

    [Test]
    public void SerializationHonorsPayloadLimitAndBoundedOutput()
    {
        Http2Frame exactLimit = CreateUnknownFrame(
            0xfe, 3, new byte[] { 0xaa, 0xbb });
        Http2Frame tooLarge = CreateUnknownFrame(
            0xfe, 3, new byte[] { 0xaa, 0xbb, 0xcc });
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 2;
        limited.peerMaximumFrameSize = 2;
        byte[] exact = new byte[12];
        byte[] shortOutput = Enumerable.Repeat((byte)0xdd, 11).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult accepted = limited.SerializeHttp2Frame(exactLimit);
        Http2FrameResult rejected = limited.SerializeHttp2Frame(tooLarge);
        Http2FrameResult written = codec.WriteHttp2Frame(tooLarge, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(tooLarge, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(accepted.Frame!.Length, Is.EqualTo(2));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(12));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x03, 0xfe, 0x00,
                0x00, 0x00, 0x00, 0x03,
                0xaa, 0xbb, 0xcc
            }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(12));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xdd, 11).ToArray()));
        });
    }

    static Http2Frame CreateUnknownFrame(
        byte typeCode,
        int streamId,
        byte[] payload)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Unknown;
        frame.TypeCode = typeCode;
        frame.IsKnownType = false;
        frame.StreamId = streamId;
        frame.Payload = payload;
        return frame;
    }

    static byte[] BuildWire(
        byte typeCode,
        byte flags,
        int streamId,
        bool reservedBit,
        byte[] payload,
        int surplusLength)
    {
        byte[] result = new byte[
            Http2Constants.FrameHeaderSize + payload.Length + surplusLength];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = typeCode;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        if (reservedBit)
        {
            result[5] = (byte)(result[5] | 0x80);
        }

        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
