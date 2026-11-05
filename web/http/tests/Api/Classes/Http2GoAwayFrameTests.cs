namespace http.Tests;

public class Http2GoAwayFrameTests
{
    [Test]
    public void ParsingExtractsLastStreamErrorAndOpaqueDebugData()
    {
        byte[] payload =
        [
            0x87, 0x65, 0x43, 0x21,
            0x00, 0x00, 0x00, 0x08,
            0xde, 0xad, 0xbe, 0xef
        ];
        byte[] wire = BuildWire(0xa5, 0, payload);
        wire[5] = 0x80;

        Http2FrameResult result =
            new Http2FrameCodec().ParseHttp2Frame(wire);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(21));
            Assert.That(result.RequiredSize, Is.EqualTo(21));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.GoAway));
            Assert.That(result.Frame.StreamId, Is.Zero);
            Assert.That(result.Frame.ReservedBit, Is.True);
            Assert.That(result.Frame.HasLastStreamId, Is.True);
            Assert.That(result.Frame.LastStreamReservedBit, Is.True);
            Assert.That(result.Frame.LastStreamId,
                Is.EqualTo(0x07654321));
            Assert.That(result.Frame.HasErrorCode, Is.True);
            Assert.That(result.Frame.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.Frame.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(result.Frame.ErrorCodeValue, Is.EqualTo(0x08u));
            Assert.That(result.Frame.IsKnownErrorCode, Is.True);
            Assert.That(result.Frame.DebugData,
                Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
        });
    }

    [Test]
    public void EmptyDebugDataAndUnknownErrorCodeRoundTripExactly()
    {
        uint unknownCode = 0xf1234567u;
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x00,
            0xf1, 0x23, 0x45, 0x67
        ];
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult parsed = codec.ParseHttp2Frame(
            BuildWire(0x5a, 0, payload));

        Http2FrameResult serialized =
            codec.SerializeHttp2Frame(parsed.Frame!);
        Http2FrameResult reparsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.LastStreamId, Is.Zero);
            Assert.That(parsed.Frame.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Unknown));
            Assert.That(parsed.Frame.ErrorCodeValue, Is.EqualTo(unknownCode));
            Assert.That(parsed.Frame.IsKnownErrorCode, Is.False);
            Assert.That(parsed.Frame.DebugData, Is.Empty);
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes,
                Is.EqualTo(BuildWire(0x5a, 0, payload)));
            Assert.That(reparsed.Frame!.ErrorCodeValue,
                Is.EqualTo(unknownCode));
        });
    }

    [TestCase(0)]
    [TestCase(7)]
    public void ParsingRejectsPayloadShorterThanEightBytes(int payloadLength)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, new byte[payloadLength]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-goaway-frame-size"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectNonzeroStream()
    {
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x03,
            0x00, 0x00, 0x00, 0x00
        ];
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 1, payload));
        Http2Frame frame = CreateGoAwayFrame(
            3, 0x00u, Http2ErrorCode.NoError, Array.Empty<byte>());
        frame.StreamId = 1;

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-goaway-stream-nonzero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-goaway-stream-nonzero"));
        });
    }

    [Test]
    public void SerializationEmitsCanonicalIdentifiersAndPreservesFlags()
    {
        Http2Frame frame = CreateGoAwayFrame(
            0x07654321,
            0xf1234567u,
            Http2ErrorCode.Unknown,
            new byte[] { 0xde, 0xad, 0xbe, 0xef });
        frame.Flags = 0xa5;
        frame.ReservedBit = true;
        frame.LastStreamReservedBit = true;

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x0c, 0x07, 0xa5,
                0x00, 0x00, 0x00, 0x00,
                0x07, 0x65, 0x43, 0x21,
                0xf1, 0x23, 0x45, 0x67,
                0xde, 0xad, 0xbe, 0xef
            }));
            Assert.That(result.Written, Is.EqualTo(21));
            Assert.That(result.RequiredSize, Is.EqualTo(21));
            Assert.That(result.Frame!.ReservedBit, Is.False);
            Assert.That(result.Frame.LastStreamReservedBit, Is.False);
            Assert.That(result.Frame.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.Frame.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Unknown));
            Assert.That(result.Frame.ErrorCodeValue,
                Is.EqualTo(0xf1234567u));
            Assert.That(result.Frame.DebugData,
                Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
        });
    }

    [Test]
    public void SerializationRejectsMissingOrInconsistentMetadata()
    {
        Http2Frame missingLastStream = CreateGoAwayFrame(
            3, 0x00u, Http2ErrorCode.NoError, Array.Empty<byte>());
        missingLastStream.HasLastStreamId = false;
        Http2Frame invalidLastStream = CreateGoAwayFrame(
            -1, 0x00u, Http2ErrorCode.NoError, Array.Empty<byte>());
        Http2Frame missingError = CreateGoAwayFrame(
            3, 0x08u, Http2ErrorCode.Cancel, Array.Empty<byte>());
        missingError.HasErrorCode = false;
        Http2Frame mismatchedError = CreateGoAwayFrame(
            3, 0x08u, Http2ErrorCode.ProtocolError, Array.Empty<byte>());
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult missingLastResult =
            codec.SerializeHttp2Frame(missingLastStream);
        Http2FrameResult invalidLastResult =
            codec.SerializeHttp2Frame(invalidLastStream);
        Http2FrameResult missingErrorResult =
            codec.SerializeHttp2Frame(missingError);
        Http2FrameResult mismatchedErrorResult =
            codec.SerializeHttp2Frame(mismatchedError);

        Assert.Multiple(() =>
        {
            Assert.That(missingLastResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingLastResult.ErrorCode,
                Is.EqualTo("invalid-http2-goaway-last-stream-id"));
            Assert.That(invalidLastResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalidLastResult.ErrorCode,
                Is.EqualTo("invalid-http2-goaway-last-stream-id"));
            Assert.That(missingErrorResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingErrorResult.ErrorCode,
                Is.EqualTo("invalid-http2-goaway-error-code"));
            Assert.That(mismatchedErrorResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(mismatchedErrorResult.ErrorCode,
                Is.EqualTo("invalid-http2-goaway-error-code"));
        });
    }

    [Test]
    public void SerializationBoundsDebugDataAndWritesAllOrNothing()
    {
        Http2Frame exactLimit = CreateGoAwayFrame(
            3, 0x00u, Http2ErrorCode.NoError, new byte[] { 0xaa });
        Http2Frame tooLarge = CreateGoAwayFrame(
            3,
            0x00u,
            Http2ErrorCode.NoError,
            new byte[] { 0xaa, 0xbb });
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 9;
        limited.peerMaximumFrameSize = 9;
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
            Assert.That(accepted.Frame!.Length, Is.EqualTo(9));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(19));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(exact[17..19],
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(19));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 18).ToArray()));
        });
    }

    static Http2Frame CreateGoAwayFrame(
        int lastStreamId,
        uint errorCodeValue,
        Http2ErrorCode errorCode,
        byte[] debugData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = lastStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = errorCode;
        frame.ErrorCodeValue = errorCodeValue;
        frame.IsKnownErrorCode = errorCode != Http2ErrorCode.Unknown;
        frame.DebugData = debugData;
        return frame;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x07;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
