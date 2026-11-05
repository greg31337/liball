using websocket;

namespace websocket.Tests;

public class WebSocketProtocolFixtureTests
{
    [TestCaseSource(typeof(WebSocketProtocolFixtures), nameof(WebSocketProtocolFixtures.RfcExamples))]
    public void RfcWireExamplesDecodeThroughTheConnection(ConnectionWireFixture fixture)
    {
        AssertConnectionFixture(fixture);
    }

    [Test]
    public void VerifiedKeyErratumAcceptsCorrectedPadBitsAndRejectsReportedText()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();

        WebSocketHandshakeResult corrected = handshake.EvaluateHttp1Request(
            Request(WebSocketProtocolFixtures.CorrectedErrataKey),
            new WebSocketServerHandshakeOptions());
        WebSocketHandshakeResult reported = handshake.EvaluateHttp1Request(
            Request(WebSocketProtocolFixtures.NonCanonicalErrataKey),
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                corrected.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted),
                WebSocketProtocolFixtures.KeyErrataProvenance);
            Assert.That(
                corrected.ClientKey,
                Is.EqualTo(WebSocketProtocolFixtures.CorrectedErrataKey),
                WebSocketProtocolFixtures.KeyErrataProvenance);
            Assert.That(
                reported.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Failed),
                WebSocketProtocolFixtures.KeyErrataProvenance);
            Assert.That(
                reported.Error.Identifier,
                Is.EqualTo("websocket-request-key-invalid"),
                WebSocketProtocolFixtures.KeyErrataProvenance);
        });
    }

    [Test]
    public void VerifiedExtensionErratumCombinesRepeatedResponseFields()
    {
        IWebSocketExtensionHeaderCodec codec =
            new WebSocketExtensionHeaderCodec(4, 4, 8, 64, 256);
        WebSocketHeaderField[] repeated =
        [
            Field(
                "Sec-WebSocket-Extensions",
                "permessage-deflate; server_no_context_takeover"),
            Field("Sec-WebSocket-Extensions", "x-mode; level=7")
        ];
        WebSocketHeaderField[] combined =
        [
            Field(
                "Sec-WebSocket-Extensions",
                "permessage-deflate; server_no_context_takeover, x-mode; level=7")
        ];

        WebSocketExtensionOffer[] repeatedOffers =
            codec.ParseExtensionFields(repeated);
        WebSocketExtensionOffer[] combinedOffers =
            codec.ParseExtensionFields(combined);
        char[] repeatedWire =
            new char[codec.MeasureExtensionFields(repeatedOffers)];
        char[] combinedWire =
            new char[codec.MeasureExtensionFields(combinedOffers)];
        codec.WriteExtensionFields(repeatedOffers, repeatedWire);
        codec.WriteExtensionFields(combinedOffers, combinedWire);

        Assert.Multiple(() =>
        {
            Assert.That(
                repeatedOffers.Select(item => item.Name),
                Is.EqualTo(new[] { "permessage-deflate", "x-mode" }),
                WebSocketProtocolFixtures.RepeatedExtensionsErrataProvenance);
            Assert.That(
                repeatedWire,
                Is.EqualTo(combinedWire),
                WebSocketProtocolFixtures.RepeatedExtensionsErrataProvenance);
        });
    }

    [TestCaseSource(typeof(WebSocketProtocolFixtures), nameof(WebSocketProtocolFixtures.IanaCloseCodes))]
    public void IanaAssignedCloseCodesDecodeAsKnown(IanaCloseCodeFixture fixture)
    {
        IWebSocketCloseCodec codec = new WebSocketCloseCodec();
        byte[] payload =
        [
            (byte)((fixture.Code >> 8) & 255),
            (byte)(fixture.Code & 255)
        ];

        WebSocketCloseReadResult result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True, fixture.Provenance);
            Assert.That(result.HasError, Is.False, fixture.Provenance);
            Assert.That(result.Close.HasCode, Is.True, fixture.Provenance);
            Assert.That(result.Close.Code, Is.EqualTo(fixture.Code), fixture.Provenance);
            Assert.That(result.Close.HasKnownCode, Is.True, fixture.Provenance);
            Assert.That(result.Close.KnownCode, Is.EqualTo(fixture.KnownCode), fixture.Provenance);
        });
    }

    [TestCaseSource(typeof(WebSocketProtocolFixtures), nameof(WebSocketProtocolFixtures.MalformedInputs))]
    public void AdversarialMalformedFramesAreRejected(MalformedWireFixture fixture)
    {
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            fixture.LocalRole,
            1024,
            []);

        WebSocketFrameReadResult result = codec.ReadFrame(fixture.Wire);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure),
                fixture.Provenance);
            Assert.That(result.HasError, Is.True, fixture.Provenance);
            Assert.That(
                result.Error.Identifier,
                Is.EqualTo(fixture.ErrorIdentifier),
                fixture.Provenance);
            Assert.That(
                result.Error.CloseCode,
                Is.EqualTo((int)WebSocketCloseCode.ProtocolError),
                fixture.Provenance);
        });
    }

    [TestCaseSource(typeof(WebSocketProtocolFixtures), nameof(WebSocketProtocolFixtures.LibraryInterop))]
    public void SurveyedLibraryWirePatternsInteroperate(ConnectionWireFixture fixture)
    {
        AssertConnectionFixture(fixture);
    }

    static void AssertConnectionFixture(ConnectionWireFixture fixture)
    {
        WebSocketConnection connection = Connection(fixture.LocalRole);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(fixture.Wire);
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketEvent[] messages = events
            .Where(item => item.Type == WebSocketEventType.MessageData)
            .ToArray();
        WebSocketEvent[] pings = events
            .Where(item => item.Type == WebSocketEventType.Ping)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete),
                fixture.Provenance);
            Assert.That(result.ConsumedCount, Is.EqualTo(fixture.Wire.Length), fixture.Provenance);
            Assert.That(result.HasError, Is.False, fixture.Provenance);
            Assert.That(messages.Length, Is.EqualTo(fixture.HasMessage ? 1 : 0), fixture.Provenance);
            Assert.That(pings.Length, Is.EqualTo(fixture.HasPing ? 1 : 0), fixture.Provenance);
        });

        if (fixture.HasMessage)
        {
            Assert.Multiple(() =>
            {
                Assert.That(messages[0].MessageType, Is.EqualTo(fixture.MessageType), fixture.Provenance);
                Assert.That(EventPayload(messages[0]), Is.EqualTo(fixture.MessagePayload), fixture.Provenance);
                Assert.That(messages[0].First, Is.True, fixture.Provenance);
                Assert.That(messages[0].Final, Is.True, fixture.Provenance);
            });
        }

        if (fixture.HasPing)
        {
            Assert.That(EventPayload(pings[0]), Is.EqualTo(fixture.PingPayload), fixture.Provenance);
        }
    }

    static WebSocketConnection Connection(WebSocketRole role)
    {
        WebSocketConnectionOptions options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            MaximumFramePayloadLength = 1024,
            MaximumMessagePayloadLength = 4096,
            MaximumDecompressedMessagePayloadLength = 4096,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 4096,
            MaximumBufferedOutputBytes = 4096,
            MaximumQueuedEvents = 32,
            AutomaticFramePayloadLength = 1024,
            AutomaticPong = false,
            CloseTimeoutMilliseconds = 1000
        };
        WebSocketHandshakeResult handshake = new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
        return new WebSocketConnection(options, handshake);
    }

    static WebSocketHttpRequest Request(string key)
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/chat",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1",
            Fields =
            [
                Field("Host", "example.com"),
                Field("Upgrade", "websocket"),
                Field("Connection", "Upgrade"),
                Field("Sec-WebSocket-Key", key),
                Field("Sec-WebSocket-Version", "13")
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

    static List<WebSocketEvent> DrainEvents(WebSocketConnection connection)
    {
        List<WebSocketEvent> events = [];
        WebSocketEvent? item = connection.ReadNextEvent();
        while (item != null)
        {
            events.Add(item);
            item = connection.ReadNextEvent();
        }

        return events;
    }

    static byte[] EventPayload(WebSocketEvent item)
    {
        return item.Payload
            .AsSpan(item.PayloadOffset, item.PayloadCount)
            .ToArray();
    }
}
