namespace http.Tests;

public class Http2PushPromiseFrameTests
{
    [Test]
    public void ParsingExtractsPromisedIdentifierFragmentFlagsAndPadding()
    {
        byte[] payload =
        [
            0x02,
            0x87, 0x65, 0x43, 0x20,
            0x82, 0x84,
            0xaa, 0xbb
        ];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0x0c, 0x01234567, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(18));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.PushPromise));
            Assert.That(result.Frame.EndHeaders, Is.True);
            Assert.That(result.Frame.IsPadded, Is.True);
            Assert.That(result.Frame.PaddingLength, Is.EqualTo(2));
            Assert.That(result.Frame.HasPromisedStreamId, Is.True);
            Assert.That(result.Frame.PromisedStreamReservedBit, Is.True);
            Assert.That(result.Frame.PromisedStreamId,
                Is.EqualTo(0x07654320));
            Assert.That(result.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82, 0x84 }));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParsingPermitsEmptyFragmentAndZeroOrNonzeroPadding()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult unpadded = codec.ParseHttp2Frame(
            BuildWire(0x00, 1, new byte[] { 0x00, 0x00, 0x00, 0x02 }));
        Http2FrameResult zeroPadding = codec.ParseHttp2Frame(
            BuildWire(0x08, 1,
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x02 }));
        Http2FrameResult padded = codec.ParseHttp2Frame(
            BuildWire(0x08, 1,
                new byte[] { 0x02, 0x00, 0x00, 0x00, 0x02, 0xaa, 0xbb }));

        Assert.Multiple(() =>
        {
            Assert.That(unpadded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(unpadded.Frame!.HeaderBlockFragment, Is.Empty);
            Assert.That(zeroPadding.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(zeroPadding.Frame!.HeaderBlockFragment, Is.Empty);
            Assert.That(zeroPadding.Frame.PaddingLength, Is.Zero);
            Assert.That(padded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(padded.Frame!.HeaderBlockFragment, Is.Empty);
            Assert.That(padded.Frame.PaddingLength, Is.EqualTo(2));
        });
    }

    [Test]
    public void ParsingRejectsMalformedOptionalSections()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult missingPadLength = codec.ParseHttp2Frame(
            BuildWire(0x08, 1, Array.Empty<byte>()));
        Http2FrameResult excessivePadding = codec.ParseHttp2Frame(
            BuildWire(0x08, 1, new byte[] { 0x02, 0xaa }));
        Http2FrameResult truncatedIdentifier = codec.ParseHttp2Frame(
            BuildWire(0x00, 1, new byte[] { 0x00, 0x00, 0x02 }));
        Http2FrameResult paddedTruncatedIdentifier = codec.ParseHttp2Frame(
            BuildWire(0x08, 1,
                new byte[] { 0x00, 0x00, 0x00, 0x02 }));

        Assert.Multiple(() =>
        {
            Assert.That(missingPadLength.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingPadLength.ErrorCode,
                Is.EqualTo("http2-push-promise-pad-length-missing"));
            Assert.That(excessivePadding.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(excessivePadding.ErrorCode,
                Is.EqualTo("http2-push-promise-padding-invalid"));
            Assert.That(truncatedIdentifier.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(truncatedIdentifier.ErrorCode,
                Is.EqualTo("http2-push-promise-id-truncated"));
            Assert.That(paddedTruncatedIdentifier.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(paddedTruncatedIdentifier.ErrorCode,
                Is.EqualTo("http2-push-promise-id-truncated"));
        });
    }

    [Test]
    public void ParsingAndSerializationRejectStreamZero()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 0, new byte[] { 0x00, 0x00, 0x00, 0x02 }));
        Http2Frame frame = CreatePushPromiseFrame(
            0, 2, Array.Empty<byte>());

        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-push-promise-stream-zero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-push-promise-stream-zero"));
        });
    }

    [TestCase(0, "http2-push-promise-id-zero")]
    [TestCase(3, "http2-push-promise-self")]
    public void ParsingRejectsInvalidPromisedIdentifier(
        int promisedStreamId,
        string expectedErrorCode)
    {
        byte[] payload =
        [
            (byte)(promisedStreamId >> 24),
            (byte)(promisedStreamId >> 16),
            (byte)(promisedStreamId >> 8),
            (byte)promisedStreamId
        ];

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildWire(0, 3, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(expectedErrorCode));
            Assert.That(result.Frame!.HasPromisedStreamId, Is.True);
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(-1)]
    public void SerializationRejectsInvalidPromisedIdentifier(
        int promisedStreamId)
    {
        Http2Frame frame = CreatePushPromiseFrame(
            3, promisedStreamId, Array.Empty<byte>());

        Http2FrameResult result =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http2-push-promise-id"));
        });
    }

    [Test]
    public void SerializationEmitsCanonicalPayloadAndClearsReservedBits()
    {
        Http2Frame frame = CreatePushPromiseFrame(
            0x01234567, 0x07654320, new byte[] { 0x82, 0x84 });
        frame.Flags = 0xa1;
        frame.EndHeaders = true;
        frame.IsPadded = true;
        frame.PaddingLength = 2;
        frame.ReservedBit = true;
        frame.PromisedStreamReservedBit = true;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x09, 0x05, 0xad,
                0x01, 0x23, 0x45, 0x67,
                0x02,
                0x07, 0x65, 0x43, 0x20,
                0x82, 0x84,
                0x00, 0x00
            }));
            Assert.That(serialized.Written, Is.EqualTo(18));
            Assert.That(serialized.RequiredSize, Is.EqualTo(18));
            Assert.That(serialized.Frame!.ReservedBit, Is.False);
            Assert.That(serialized.Frame.PromisedStreamReservedBit, Is.False);
            Assert.That(serialized.Frame.EndHeaders, Is.True);
            Assert.That(serialized.Frame.HeaderBlockFragment,
                Is.EqualTo(new byte[] { 0x82, 0x84 }));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.PromisedStreamId,
                Is.EqualTo(0x07654320));
            Assert.That(parsed.Frame.PromisedStreamReservedBit, Is.False);
        });
    }

    [Test]
    public void SerializationValidatesMetadataAndWholePayloadSize()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.configuredMaximumFrameSize = 7;
        codec.peerMaximumFrameSize = 7;
        Http2Frame exact = CreatePushPromiseFrame(
            1, 2, Array.Empty<byte>());
        exact.IsPadded = true;
        exact.PaddingLength = 2;
        Http2Frame tooLarge = CreatePushPromiseFrame(
            1, 2, Array.Empty<byte>());
        tooLarge.IsPadded = true;
        tooLarge.PaddingLength = 3;
        Http2Frame missingIdentifier = CreatePushPromiseFrame(
            1, 2, Array.Empty<byte>());
        missingIdentifier.HasPromisedStreamId = false;
        Http2Frame inconsistentPadding = CreatePushPromiseFrame(
            1, 2, Array.Empty<byte>());
        inconsistentPadding.PaddingLength = 1;

        Http2FrameResult exactResult = codec.SerializeHttp2Frame(exact);
        Http2FrameResult largeResult = codec.SerializeHttp2Frame(tooLarge);
        Http2FrameResult missingResult =
            codec.SerializeHttp2Frame(missingIdentifier);
        Http2FrameResult paddingResult =
            codec.SerializeHttp2Frame(inconsistentPadding);

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exactResult.Frame!.Length, Is.EqualTo(7));
            Assert.That(largeResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(largeResult.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(missingResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("invalid-http2-push-promise-id"));
            Assert.That(paddingResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(paddingResult.ErrorCode,
                Is.EqualTo("invalid-http2-push-promise-frame"));
        });
    }

    [Test]
    public void BoundedWriteIsAllOrNothing()
    {
        Http2Frame frame = CreatePushPromiseFrame(
            1, 2, new byte[] { 0x82, 0x84 });
        frame.EndHeaders = true;
        byte[] exact = new byte[15];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 14).ToArray();
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult written = codec.WriteHttp2Frame(frame, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(frame, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(15));
            Assert.That(exact, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x06, 0x05, 0x04,
                0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x02,
                0x82, 0x84
            }));
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(15));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 14).ToArray()));
        });
    }

    static Http2Frame CreatePushPromiseFrame(
        int streamId,
        int promisedStreamId,
        byte[] fragment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = streamId;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = promisedStreamId;
        frame.HeaderBlockFragment = fragment;
        return frame;
    }

    static byte[] BuildWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x05;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
