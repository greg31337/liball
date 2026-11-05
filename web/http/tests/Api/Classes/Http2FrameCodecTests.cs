namespace http.Tests;

public class Http2FrameCodecTests
{
    [Test]
    public void CompleteHeaderWithNoPayloadParsesExactFields()
    {
        IHttp2FrameCodec codec = new Http2FrameCodec();
        byte[] input = BuildHeader(0, 0x00, 0x01, 1, false);

        Http2FrameResult result = codec.ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(9));
            Assert.That(result.RequiredSize, Is.EqualTo(9));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.Length, Is.Zero);
            Assert.That(result.Frame.FrameType, Is.EqualTo(Http2FrameType.Data));
            Assert.That(result.Frame.TypeCode, Is.Zero);
            Assert.That(result.Frame.IsKnownType, Is.True);
            Assert.That(result.Frame.Flags, Is.EqualTo(0x01));
            Assert.That(result.Frame.StreamId, Is.EqualTo(1));
            Assert.That(result.Frame.ReservedBit, Is.False);
            Assert.That(result.Frame.Payload, Is.Empty);
        });
    }

    [Test]
    public void EveryPartialHeaderNeedsTheExactNineByteHeader()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] header = BuildHeader(0, 0x04, 0, 0, false);

        for (int length = 0; length < Http2Constants.FrameHeaderSize; length++)
        {
            Http2FrameResult result = codec.ParseHttp2Frame(
                header.AsSpan(0, length).ToArray());

            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput),
                $"Partial header length {length} was not incremental.");
            Assert.That(result.Consumed, Is.Zero);
            Assert.That(result.RequiredSize,
                Is.EqualTo(Http2Constants.FrameHeaderSize));
            Assert.That(result.Frame, Is.Null);
        }
    }

    [Test]
    public void TwentyFourBitLengthReportsExactIncompletePayloadRequirement()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        codec.configuredMaximumFrameSize = Http2Constants.MaximumFrameSize;
        codec.peerMaximumFrameSize = Http2Constants.MaximumFrameSize;
        byte[] input = BuildHeader(0x010203, 0x01, 0x24, 3, false);

        Http2FrameResult result = codec.ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(result.Consumed, Is.Zero);
            Assert.That(result.RequiredSize, Is.EqualTo(0x010203 + 9));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.Length, Is.EqualTo(0x010203));
            Assert.That(result.Frame.FrameType,
                Is.EqualTo(Http2FrameType.Headers));
            Assert.That(result.Frame.Flags, Is.EqualTo(0x24));
            Assert.That(result.Frame.StreamId, Is.EqualTo(3));
            Assert.That(result.Frame.Payload, Is.Empty);
        });
    }

    [Test]
    public void CompletePayloadIsOwnedAndSurplusInputIsNotConsumed()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] header = BuildHeader(2, 0xfe, 0xa5, 5, true);
        byte[] input = new byte[12];
        header.CopyTo(input, 0);
        input[9] = 0xaa;
        input[10] = 0xbb;
        input[11] = 0xcc;

        Http2FrameResult result = codec.ParseHttp2Frame(input);
        input[9] = 0x00;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(11));
            Assert.That(result.RequiredSize, Is.EqualTo(11));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.Length, Is.EqualTo(2));
            Assert.That(result.Frame.FrameType,
                Is.EqualTo(Http2FrameType.Unknown));
            Assert.That(result.Frame.TypeCode, Is.EqualTo(0xfe));
            Assert.That(result.Frame.IsKnownType, Is.False);
            Assert.That(result.Frame.Flags, Is.EqualTo(0xa5));
            Assert.That(result.Frame.ReservedBit, Is.True);
            Assert.That(result.Frame.StreamId, Is.EqualTo(5));
            Assert.That(result.Frame.Payload,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        });
    }

    [Test]
    public void IncompletePayloadKeepsTheWholeFrameUnconsumed()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] input = new byte[11];
        BuildHeader(3, 0x09, 0x04, 7, false).CopyTo(input, 0);
        input[9] = 0x82;
        input[10] = 0x84;

        Http2FrameResult result = codec.ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(result.Consumed, Is.Zero);
            Assert.That(result.RequiredSize, Is.EqualTo(12));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.FrameType,
                Is.EqualTo(Http2FrameType.Continuation));
            Assert.That(result.Frame.Payload, Is.Empty);
        });
    }

    [Test]
    public void ReservedBitIsPreservedAndMaskedFromTheStreamIdentifier()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] input = new byte[13];
        BuildHeader(
            4, 0x08, 0, Http2Constants.MaximumStreamIdentifier, true)
            .CopyTo(input, 0);
        input[12] = 1;

        Http2FrameResult result = codec.ParseHttp2Frame(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.ReservedBit, Is.True);
            Assert.That(result.Frame.StreamId,
                Is.EqualTo(Http2Constants.MaximumStreamIdentifier));
            Assert.That(result.Frame.FrameType,
                Is.EqualTo(Http2FrameType.WindowUpdate));
            Assert.That(result.Frame.WindowIncrement, Is.EqualTo(1));
        });
    }

    [TestCase(0x00, Http2FrameType.Data)]
    [TestCase(0x04, Http2FrameType.Settings)]
    [TestCase(0x09, Http2FrameType.Continuation)]
    [TestCase(0x0c, Http2FrameType.Origin)]
    [TestCase(0x10, Http2FrameType.PriorityUpdate)]
    [TestCase(0x0a, Http2FrameType.Unknown)]
    public void RegisteredAndUnknownTypeCodesAreClassifiedExactly(
        int typeCode,
        Http2FrameType expected)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildHeader(0, (byte)typeCode, 0, 0, false));

        Assert.That(result.Frame, Is.Not.Null);
        Assert.That(result.Frame!.FrameType, Is.EqualTo(expected));
        Assert.That(result.Frame.TypeCode, Is.EqualTo((byte)typeCode));
        Assert.That(result.Frame.IsKnownType,
            Is.EqualTo(expected != Http2FrameType.Unknown));
    }

    [Test]
    public void DefaultMaximumAcceptsTheBoundaryAndRejectsTheNextLength()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult boundary = codec.ParseHttp2Frame(BuildHeader(
            Http2Constants.DefaultMaximumFrameSize,
            0x00,
            0,
            1,
            false));
        Http2FrameResult exceeded = codec.ParseHttp2Frame(BuildHeader(
            Http2Constants.DefaultMaximumFrameSize + 1,
            0x00,
            0,
            1,
            false));

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Status,
                Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(boundary.RequiredSize,
                Is.EqualTo(Http2Constants.DefaultMaximumFrameSize + 9));
            Assert.That(exceeded.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(exceeded.Consumed, Is.EqualTo(9));
            Assert.That(exceeded.RequiredSize,
                Is.EqualTo(Http2Constants.DefaultMaximumFrameSize + 10));
            Assert.That(exceeded.HasErrorPosition, Is.True);
            Assert.That(exceeded.ErrorPosition, Is.Zero);
            Assert.That(exceeded.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
        });
    }

    [Test]
    public void PeerAndConfiguredMaximumsAreBothEnforced()
    {
        Http2FrameCodec configured = new Http2FrameCodec();
        configured.configuredMaximumFrameSize = 17000;
        configured.peerMaximumFrameSize = 20000;
        Http2FrameCodec peer = new Http2FrameCodec();
        peer.configuredMaximumFrameSize = 20000;
        peer.peerMaximumFrameSize = 17000;

        Http2FrameResult configuredResult = configured.ParseHttp2Frame(
            BuildHeader(18000, 0x00, 0, 1, false));
        Http2FrameResult peerResult = peer.ParseHttp2Frame(
            BuildHeader(18000, 0x00, 0, 1, false));

        Assert.Multiple(() =>
        {
            Assert.That(configuredResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(configuredResult.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(peerResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(peerResult.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
        });
    }

    static byte[] BuildHeader(
        int length,
        byte typeCode,
        byte flags,
        int streamId,
        bool reservedBit)
    {
        byte[] result = new byte[9];
        result[0] = (byte)(length >> 16);
        result[1] = (byte)(length >> 8);
        result[2] = (byte)length;
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
        return result;
    }
}
