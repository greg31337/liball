using websocket;

namespace websocket.Tests;

public class PublicApiCoverageCompletionTests
{
    [Test]
    public void OriginPolicyRejectsMalformedFieldsAndUnknownTransport()
    {
        IWebSocketOriginPolicy policy = new DefaultWebSocketOriginPolicy([]);
        WebSocketHttpRequest malformedFields = Http1OriginRequest();
        malformedFields.Fields = [null!];
        WebSocketHttpRequest unknownTransport = Http1OriginRequest();
        unknownTransport.Transport = (WebSocketHandshakeTransport)99;

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(malformedFields), Is.False);
            Assert.That(policy.IsOriginAllowed(unknownTransport), Is.False);
        });
    }

    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void OriginPolicyReadsBothExtendedConnectAuthorities(
        WebSocketHandshakeTransport transport)
    {
        IWebSocketOriginPolicy policy = new DefaultWebSocketOriginPolicy([]);
        WebSocketHttpRequest request = new WebSocketHttpRequest
        {
            Transport = transport,
            HasScheme = true,
            Scheme = "https",
            HasAuthority = true,
            Authority = "example.com",
            Fields = [Field("origin", "https://example.com")]
        };

        Assert.That(policy.IsOriginAllowed(request), Is.True);
    }

    [Test]
    public void CloseMeasurementRejectsNullInformation()
    {
        IWebSocketCloseCodec codec = new WebSocketCloseCodec();

        Assert.Throws<WebSocketProtocolException>(() =>
            codec.MeasureClosePayload(null!));
    }

    [Test]
    public void ExtensionCodecRejectsNullCollectionsAndMalformedFieldMetadata()
    {
        IWebSocketExtensionHeaderCodec codec =
            new WebSocketExtensionHeaderCodec(4, 4, 8, 64, 256);
        WebSocketHeaderField malformed = new WebSocketHeaderField();
        malformed.Name = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketExtensionException>(() =>
                codec.ParseExtensionFields(null!));
            Assert.Throws<WebSocketExtensionException>(() =>
                codec.ParseExtensionFields([malformed]));
            Assert.Throws<WebSocketExtensionException>(() =>
                codec.MeasureExtensionFields(null!));
        });
    }

    [Test]
    public void HttpWriterExceptionFactoryMapsUnexpectedStatusToState()
    {
        Assert.That(
            WebSocketHttp1Codec.CreateWriteException(
                WebSocketParseStatus.NeedInput),
            Is.TypeOf<WebSocketStateException>());
    }

    [Test]
    public void FrameMeasurementRejectsNullRequestAtPublicBoundary()
    {
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            WebSocketRole.Client,
            125,
            []);

        Assert.Throws<WebSocketStateException>(() => codec.MeasureFrame(null!));
    }

    [Test]
    public void FrameWriteRejectsMutationDuringPartialHeaderOutput()
    {
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            WebSocketRole.Server,
            125,
            []);
        WebSocketFrameWriteRequest request = new WebSocketFrameWriteRequest
        {
            Header = new WebSocketFrameHeader
            {
                Final = true,
                Opcode = WebSocketOpcode.Binary,
                PayloadLength = 2
            },
            Payload = [1, 2],
            PayloadCount = 2
        };
        int firstCount = codec.WriteFrame(request, new byte[1]);
        request.Header.Final = false;

        Assert.Multiple(() =>
        {
            Assert.That(firstCount, Is.EqualTo(1));
            Assert.Throws<WebSocketStateException>(() =>
                codec.WriteFrame(request, new byte[8]));
        });
    }

    [Test]
    public void OppositeSendKindsObservePendingBackpressure()
    {
        WebSocketConnection wholePending = Connection(
            WebSocketMessageDeliveryMode.Reassembled);
        WebSocketSendRequest largeWhole = Whole(new byte[20]);
        WebSocketParseStatus wholeStatus =
            wholePending.SendWholeMessage(largeWhole);
        WebSocketParseStatus chunkWhileWholePending =
            wholePending.SendMessageChunk(Chunk(new byte[1]));

        WebSocketConnection streamingPending = Connection(
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketParseStatus prefill =
            streamingPending.SendWholeMessage(Whole(new byte[7]));
        WebSocketSendChunk largeChunk = Chunk(new byte[2]);
        WebSocketParseStatus streamingStatus =
            streamingPending.SendMessageChunk(largeChunk);
        WebSocketParseStatus wholeWhileStreamingPending =
            streamingPending.SendWholeMessage(Whole(new byte[1]));

        Assert.Multiple(() =>
        {
            Assert.That(wholeStatus,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(chunkWhileWholePending,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(prefill, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(streamingStatus,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(wholeWhileStreamingPending,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
        });
    }

    [TestCase(WebSocketMessageDeliveryMode.Reassembled)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming)]
    public void PendingEventCountSupportsBothDeliveryModes(
        WebSocketMessageDeliveryMode deliveryMode)
    {
        WebSocketConnection connection = Connection(deliveryMode);

        Assert.That(connection.GetPendingConnectionEventCount(), Is.Zero);
    }

    static WebSocketHttpRequest Http1OriginRequest()
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Fields =
            [
                Field("host", "example.com"),
                Field("origin", "http://example.com")
            ]
        };
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static WebSocketConnection Connection(
        WebSocketMessageDeliveryMode deliveryMode)
    {
        WebSocketConnectionOptions options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Server,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = 11,
            MaximumMessagePayloadLength = 20,
            MaximumDecompressedMessagePayloadLength = 20,
            MaximumFragmentCount = 10,
            MaximumBufferedInputBytes = 32,
            MaximumBufferedOutputBytes = 16,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 2,
            CloseTimeoutMilliseconds = 1000
        };
        WebSocketHandshakeResult handshake = new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
        return new WebSocketConnection(options, handshake);
    }

    static WebSocketSendRequest Whole(byte[] payload)
    {
        return new WebSocketSendRequest
        {
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static WebSocketSendChunk Chunk(byte[] payload)
    {
        return new WebSocketSendChunk
        {
            First = true,
            Final = true,
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }
}
