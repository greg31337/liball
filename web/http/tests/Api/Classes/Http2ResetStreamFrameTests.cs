namespace http.Tests;

public class Http2ResetStreamFrameTests
{
    [TestCase(0x00u, Http2ErrorCode.NoError)]
    [TestCase(0x01u, Http2ErrorCode.ProtocolError)]
    [TestCase(0x02u, Http2ErrorCode.InternalError)]
    [TestCase(0x03u, Http2ErrorCode.FlowControlError)]
    [TestCase(0x04u, Http2ErrorCode.SettingsTimeout)]
    [TestCase(0x05u, Http2ErrorCode.StreamClosed)]
    [TestCase(0x06u, Http2ErrorCode.FrameSizeError)]
    [TestCase(0x07u, Http2ErrorCode.RefusedStream)]
    [TestCase(0x08u, Http2ErrorCode.Cancel)]
    [TestCase(0x09u, Http2ErrorCode.CompressionError)]
    [TestCase(0x0au, Http2ErrorCode.ConnectError)]
    [TestCase(0x0bu, Http2ErrorCode.EnhanceYourCalm)]
    [TestCase(0x0cu, Http2ErrorCode.InadequateSecurity)]
    [TestCase(0x0du, Http2ErrorCode.Http11Required)]
    public void ParsingMapsEveryRegisteredErrorCode(
        uint wireCode,
        Http2ErrorCode expectedCode)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildResetWire(0xa5, 3, ErrorBytes(wireCode)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(13));
            Assert.That(result.RequiredSize, Is.EqualTo(13));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.RstStream));
            Assert.That(result.Frame.HasErrorCode, Is.True);
            Assert.That(result.Frame.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.Frame.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(result.Frame.ErrorCodeValue, Is.EqualTo(wireCode));
            Assert.That(result.Frame.IsKnownErrorCode, Is.True);
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
        });
    }

    [Test]
    public void UnknownErrorCodeIsPreservedAcrossRoundTrip()
    {
        uint unknownCode = 0xf1234567u;
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult parsed = codec.ParseHttp2Frame(
            BuildResetWire(0x5a, 0x01234567, ErrorBytes(unknownCode)));

        Http2FrameResult serialized =
            codec.SerializeHttp2Frame(parsed.Frame!);
        Http2FrameResult reparsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.ErrorCode,
                Is.EqualTo(Http2ErrorCode.Unknown));
            Assert.That(parsed.Frame.ErrorCodeValue, Is.EqualTo(unknownCode));
            Assert.That(parsed.Frame.IsKnownErrorCode, Is.False);
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x04, 0x03, 0x5a,
                0x01, 0x23, 0x45, 0x67,
                0xf1, 0x23, 0x45, 0x67
            }));
            Assert.That(serialized.Frame!.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(reparsed.Frame!.ErrorCodeValue,
                Is.EqualTo(unknownCode));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(5)]
    public void ParsingRejectsEveryNonFourBytePayloadLength(int payloadLength)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildResetWire(0, 1, new byte[payloadLength]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-rst-stream-frame-size"));
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectStreamZero()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildResetWire(0, 0, ErrorBytes(0x08u)));
        Http2Frame frame = CreateResetFrame(0, 0x08u, Http2ErrorCode.Cancel);

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-rst-stream-stream-zero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-rst-stream-stream-zero"));
        });
    }

    [Test]
    public void SerializationEmitsCanonicalHeaderAndStreamErrorMetadata()
    {
        Http2Frame frame = CreateResetFrame(
            0x01234567, 0x08u, Http2ErrorCode.Cancel);
        frame.Flags = 0xa5;
        frame.ReservedBit = true;

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x04, 0x03, 0xa5,
                0x01, 0x23, 0x45, 0x67,
                0x00, 0x00, 0x00, 0x08
            }));
            Assert.That(result.Written, Is.EqualTo(13));
            Assert.That(result.RequiredSize, Is.EqualTo(13));
            Assert.That(result.Frame!.ReservedBit, Is.False);
            Assert.That(result.Frame.ErrorScope,
                Is.EqualTo(Http2ErrorScope.Stream));
            Assert.That(result.Frame.ErrorCode, Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(result.Frame.ErrorCodeValue, Is.EqualTo(0x08u));
            Assert.That(result.Frame.IsKnownErrorCode, Is.True);
        });
    }

    [Test]
    public void SerializationRejectsMissingOrInconsistentErrorMetadata()
    {
        Http2Frame missing = CreateResetFrame(
            3, 0x08u, Http2ErrorCode.Cancel);
        missing.HasErrorCode = false;
        Http2Frame mismatchedCode = CreateResetFrame(
            3, 0x08u, Http2ErrorCode.ProtocolError);
        Http2Frame mismatchedKnown = CreateResetFrame(
            3, 0xf0000001u, Http2ErrorCode.Unknown);
        mismatchedKnown.IsKnownErrorCode = true;
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult missingResult = codec.SerializeHttp2Frame(missing);
        Http2FrameResult codeResult = codec.SerializeHttp2Frame(mismatchedCode);
        Http2FrameResult knownResult =
            codec.SerializeHttp2Frame(mismatchedKnown);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(codeResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(knownResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(knownResult.ErrorCode,
                Is.EqualTo("invalid-http2-rst-stream-error-code"));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame frame = CreateResetFrame(
            3, 0x08u, Http2ErrorCode.Cancel);
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 3;
        limited.peerMaximumFrameSize = 3;
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] exact = new byte[13];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 12).ToArray();

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
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(13));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 12).ToArray()));
        });
    }

    static Http2Frame CreateResetFrame(
        int streamId,
        uint errorCodeValue,
        Http2ErrorCode errorCode)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = streamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCode = errorCode;
        frame.ErrorCodeValue = errorCodeValue;
        frame.IsKnownErrorCode = errorCode != Http2ErrorCode.Unknown;
        return frame;
    }

    static byte[] ErrorBytes(uint errorCode)
    {
        return
        [
            (byte)(errorCode >> 24),
            (byte)(errorCode >> 16),
            (byte)(errorCode >> 8),
            (byte)errorCode
        ];
    }

    static byte[] BuildResetWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x03;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
