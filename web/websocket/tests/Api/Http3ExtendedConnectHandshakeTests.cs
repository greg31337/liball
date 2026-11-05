using websocket;

namespace websocket.Tests;

public class Http3ExtendedConnectHandshakeTests
{
    [Test]
    public void ClientRequiresTheIndependentlyAdvertisedHttp3Setting()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var http2 = ClientOptions(WebSocketHandshakeTransport.Http2ExtendedConnect);
        handshake.CreateExtendedConnectRequest(http2);
        handshake.ValidateExtendedConnectResponse(Response(
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            200));
        var http3 = ClientOptions(WebSocketHandshakeTransport.Http3ExtendedConnect);
        http3.PeerEnabledExtendedConnect = false;

        Assert.Throws<WebSocketHandshakeException>(() =>
            handshake.CreateExtendedConnectRequest(http3));

        http3.PeerEnabledExtendedConnect = true;
        var created = handshake.CreateExtendedConnectRequest(http3);

        Assert.Multiple(() =>
        {
            Assert.That(created.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(created.Request.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
        });
    }

    [Test]
    public void ClientCreatesAndCompletesHttp3UsingSharedNegotiationRules()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions(WebSocketHandshakeTransport.Http3ExtendedConnect);
        options.Uri = "WSS://BÜCHER.example:443/a/../chat?q=naïve";
        options.Subprotocols = ["chat", "superchat"];
        options.Extensions = [Offer("x-mode", Parameter("speed", "fast"))];

        var created = handshake.CreateExtendedConnectRequest(options);
        var response = Response(WebSocketHandshakeTransport.Http3ExtendedConnect, 299);
        response.Fields =
        [
            Field("sec-websocket-protocol", "superchat"),
            Field("sec-websocket-extensions", "x-mode; speed=fast")
        ];
        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(created.Request.Method, Is.EqualTo("CONNECT"));
            Assert.That(created.Request.Scheme, Is.EqualTo("https"));
            Assert.That(created.Request.Authority,
                Is.EqualTo("xn--bcher-kva.example:443"));
            Assert.That(created.Request.Path, Is.EqualTo("/chat?q=na%C3%AFve"));
            Assert.That(created.Request.Protocol, Is.EqualTo("websocket"));
            Assert.That(created.Request.HasRequestTarget, Is.False);
            Assert.That(created.Request.HasHttpVersion, Is.False);
            Assert.That(created.Request.Fields.Any(field =>
                field.Name == "sec-websocket-key"), Is.False);
            Assert.That(ExtendedConnectValidator.ValidateRequest(
                created.Request,
                WebSocketHandshakeTransport.Http3ExtendedConnect).Valid, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(result.Response.StatusCode, Is.EqualTo(299));
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("superchat"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
        });
    }

    [Test]
    public void ClientRejectsAnHttp2ResponseForAPendingHttp3Request()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(ClientOptions(
            WebSocketHandshakeTransport.Http3ExtendedConnect));
        var wrongTransport = Response(
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            200);

        var result = handshake.ValidateExtendedConnectResponse(wrongTransport);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-response-transport-invalid"));
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(wrongTransport));
        });
    }

    [Test]
    public void ServerTreatsHttp2AndHttp3SettingsIndependently()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var http2Only = ServerOptions(WebSocketHandshakeTransport.Http2ExtendedConnect);
        var http3Only = ServerOptions(WebSocketHandshakeTransport.Http3ExtendedConnect);
        var both = new WebSocketServerHandshakeOptions
        {
            SupportedTransports =
            [
                WebSocketHandshakeTransport.Http2ExtendedConnect,
                WebSocketHandshakeTransport.Http3ExtendedConnect
            ],
            ExtendedConnectEnabled = true
        };

        var disabled = handshake.EvaluateExtendedConnectRequest(
            Request(WebSocketHandshakeTransport.Http3ExtendedConnect),
            http2Only);
        var http2Disabled = handshake.EvaluateExtendedConnectRequest(
            Request(WebSocketHandshakeTransport.Http2ExtendedConnect),
            http3Only);
        var enabled = handshake.EvaluateExtendedConnectRequest(
            Request(WebSocketHandshakeTransport.Http3ExtendedConnect),
            http3Only);
        var bothHttp2 = handshake.EvaluateExtendedConnectRequest(
            Request(WebSocketHandshakeTransport.Http2ExtendedConnect),
            both);
        var bothHttp3 = handshake.EvaluateExtendedConnectRequest(
            Request(WebSocketHandshakeTransport.Http3ExtendedConnect),
            both);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(disabled.Response.StatusCode, Is.EqualTo(501));
            Assert.That(disabled.Error.Identifier,
                Is.EqualTo("extended-connect-not-supported"));
            Assert.That(disabled.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(http2Disabled.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(http2Disabled.Response.StatusCode, Is.EqualTo(501));
            Assert.That(http2Disabled.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(enabled.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(enabled.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(enabled.Response.StatusCode, Is.EqualTo(200));
            Assert.That(bothHttp2.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(bothHttp3.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
        });
    }

    [TestCase(100, WebSocketHandshakeOutcome.Incomplete)]
    [TestCase(103, WebSocketHandshakeOutcome.Incomplete)]
    [TestCase(199, WebSocketHandshakeOutcome.Incomplete)]
    [TestCase(200, WebSocketHandshakeOutcome.Accepted)]
    [TestCase(204, WebSocketHandshakeOutcome.Accepted)]
    [TestCase(299, WebSocketHandshakeOutcome.Accepted)]
    [TestCase(300, WebSocketHandshakeOutcome.Rejected)]
    [TestCase(404, WebSocketHandshakeOutcome.Rejected)]
    [TestCase(599, WebSocketHandshakeOutcome.Rejected)]
    public void ClientAppliesRfc9220ResponseStatusClasses(
        int statusCode,
        WebSocketHandshakeOutcome expectedOutcome)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(ClientOptions(
            WebSocketHandshakeTransport.Http3ExtendedConnect));

        var result = handshake.ValidateExtendedConnectResponse(Response(
            WebSocketHandshakeTransport.Http3ExtendedConnect,
            statusCode));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(expectedOutcome));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.HasError, Is.False);
        });
    }

    [Test]
    public void Status101IsForbiddenForRfc9220AndConsumesPendingState()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(ClientOptions(
            WebSocketHandshakeTransport.Http3ExtendedConnect));
        var response = Response(WebSocketHandshakeTransport.Http3ExtendedConnect, 101);

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-response-status-invalid"));
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(response));
        });
    }

    [Test]
    public void ServerNegotiatesHttp3SubprotocolAndExtension()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = Request(WebSocketHandshakeTransport.Http3ExtendedConnect);
        request.Fields =
        [
            Field("sec-websocket-version", "13"),
            Field("sec-websocket-protocol", "video, chat"),
            Field("sec-websocket-extensions", "x-mode; speed=fast")
        ];
        var options = ServerOptions(WebSocketHandshakeTransport.Http3ExtendedConnect);
        options.SupportedSubprotocols = ["chat", "video"];
        options.SupportedExtensions = [Offer("x-mode", Parameter("speed", "fast"))];

        var result = handshake.EvaluateExtendedConnectRequest(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("video"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(Value(result.Response, "sec-websocket-protocol"),
                Is.EqualTo("video"));
            Assert.That(Value(result.Response, "sec-websocket-extensions"),
                Is.EqualTo("x-mode; speed=fast"));
            Assert.That(ExtendedConnectValidator.ValidateResponse(
                result.Response,
                WebSocketHandshakeTransport.Http3ExtendedConnect).Valid, Is.True);
        });
    }

    [Test]
    public void ServerReturns501ForAnUnsupportedHttp3Protocol()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = Request(WebSocketHandshakeTransport.Http3ExtendedConnect);
        request.Protocol = "connect-udp";

        var result = handshake.EvaluateExtendedConnectRequest(
            request,
            ServerOptions(WebSocketHandshakeTransport.Http3ExtendedConnect));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(501));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-protocol-unsupported"));
            Assert.That(result.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
        });
    }

    [TestCase("GET", "extended-connect-method-invalid")]
    [TestCase("CONNECT", "extended-connect-field-name-not-lowercase")]
    public void ServerUsesSharedHttp3ValidationFailures(string method, string identifier)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = Request(WebSocketHandshakeTransport.Http3ExtendedConnect);
        request.Method = method;
        if (method == "CONNECT")
        {
            request.Fields = [Field("Sec-WebSocket-Version", "13")];
        }

        var result = handshake.EvaluateExtendedConnectRequest(
            request,
            ServerOptions(WebSocketHandshakeTransport.Http3ExtendedConnect));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
        });
    }

    [Test]
    public void ServerAdvertisesVersion13ForUnsupportedHttp3Version()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = Request(WebSocketHandshakeTransport.Http3ExtendedConnect);
        request.Fields[0].Value = "8";

        var result = handshake.EvaluateExtendedConnectRequest(
            request,
            ServerOptions(WebSocketHandshakeTransport.Http3ExtendedConnect));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(426));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-version-unsupported"));
            Assert.That(Value(result.Response, "sec-websocket-version"), Is.EqualTo("13"));
        });
    }

    [Test]
    public void AcceptedHttp3ConnectionMapsCleanFinAndStreamReset()
    {
        WebSocketHandshakeResult cleanHandshake = AcceptedServerHandshake(
            WebSocketHandshakeTransport.Http3ExtendedConnect);
        WebSocketHandshakeResult resetHandshake = AcceptedServerHandshake(
            WebSocketHandshakeTransport.Http3ExtendedConnect);
        WebSocketConnection clean = ServerConnection(cleanHandshake);
        WebSocketConnection reset = ServerConnection(resetHandshake);

        WebSocketLocalCloseResult started = clean.StartConnectionClose(
            new WebSocketCloseInfo
            {
                HasCode = true,
                Code = 1000
            });
        byte[] localClose = DrainAll(clean);
        WebSocketInboundFramePumpResult remoteClose = clean.ReceiveConnectionBytes(
            [0x88, 0x82, 1, 2, 3, 4, 0x02, 0xEA]);
        WebSocketConnectionTransitionResult finished = clean.EndConnectionInput();
        WebSocketConnectionTransitionResult resetResult = reset.ResetConnectionTransport();
        WebSocketConnectionInfo cleanInfo = clean.GetConnectionInfo();
        WebSocketConnectionInfo resetInfo = reset.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(started.CloseQueued, Is.True);
            Assert.That(localClose[0], Is.EqualTo(0x88));
            Assert.That(remoteClose.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finished.Status, Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(finished.State, Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(cleanInfo.Options.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(cleanInfo.CleanClose, Is.True);
            Assert.That(cleanInfo.TransportEnded, Is.True);
            Assert.That(cleanInfo.TransportReset, Is.False);
            Assert.That(cleanInfo.HasError, Is.False);
            Assert.That(resetResult.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(resetResult.Error.Identifier,
                Is.EqualTo("connection-http3-stream-reset"));
            Assert.That(resetInfo.TransportEnded, Is.True);
            Assert.That(resetInfo.TransportReset, Is.True);
            Assert.That(resetInfo.CleanClose, Is.False);
        });
    }

    [Test]
    public void Http2AndHttp3DataUseEquivalentWebSocketFramingSemantics()
    {
        WebSocketConnection http2 = ServerConnection(AcceptedServerHandshake(
            WebSocketHandshakeTransport.Http2ExtendedConnect));
        WebSocketConnection http3 = ServerConnection(AcceptedServerHandshake(
            WebSocketHandshakeTransport.Http3ExtendedConnect));
        var send2 = new WebSocketSendRequest
        {
            MessageType = WebSocketMessageType.Binary,
            Payload = [1, 2, 3],
            PayloadCount = 3
        };
        var send3 = new WebSocketSendRequest
        {
            MessageType = WebSocketMessageType.Binary,
            Payload = [1, 2, 3],
            PayloadCount = 3
        };

        WebSocketParseStatus sent2 = http2.SendWholeMessage(send2);
        WebSocketParseStatus sent3 = http3.SendWholeMessage(send3);
        byte[] wire2 = DrainAll(http2);
        byte[] wire3 = DrainAll(http3);
        byte[] maskedText = [0x81, 0x82, 1, 2, 3, 4, 0x69, 0x6B];
        WebSocketInboundFramePumpResult received2 =
            http2.ReceiveConnectionBytes(maskedText);
        WebSocketInboundFramePumpResult received3 =
            http3.ReceiveConnectionBytes(maskedText);
        WebSocketEvent? event2 = http2.ReadNextEvent();
        WebSocketEvent? event3 = http3.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(sent2, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(sent3, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(wire2, Is.EqualTo(new byte[] { 0x82, 0x03, 1, 2, 3 }));
            Assert.That(wire3, Is.EqualTo(wire2));
            Assert.That(received2.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received3.Status, Is.EqualTo(received2.Status));
            Assert.That(received3.ConsumedCount, Is.EqualTo(received2.ConsumedCount));
            Assert.That(event2, Is.Not.Null);
            Assert.That(event3, Is.Not.Null);
            Assert.That(event2!.Type, Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(event3!.Type, Is.EqualTo(event2.Type));
            Assert.That(event3.MessageType, Is.EqualTo(event2.MessageType));
            Assert.That(event3.Payload, Is.EqualTo(event2.Payload));
            Assert.That(event3.Payload, Is.EqualTo(new byte[] { 0x68, 0x69 }));
        });
    }

    static WebSocketClientHandshakeOptions ClientOptions(
        WebSocketHandshakeTransport transport)
    {
        return new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat",
            Transport = transport,
            PeerEnabledExtendedConnect = true
        };
    }

    static WebSocketServerHandshakeOptions ServerOptions(
        WebSocketHandshakeTransport transport)
    {
        return new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [transport],
            ExtendedConnectEnabled = true
        };
    }

    static WebSocketHttpRequest Request(WebSocketHandshakeTransport transport)
    {
        return new WebSocketHttpRequest
        {
            Transport = transport,
            Method = "CONNECT",
            HasScheme = true,
            Scheme = "https",
            HasAuthority = true,
            Authority = "example.com:443",
            HasPath = true,
            Path = "/chat",
            HasProtocol = true,
            Protocol = "websocket",
            Fields = [Field("sec-websocket-version", "13")]
        };
    }

    static WebSocketHttpResponse Response(
        WebSocketHandshakeTransport transport,
        int statusCode)
    {
        return new WebSocketHttpResponse
        {
            Transport = transport,
            StatusCode = statusCode
        };
    }

    static string Value(WebSocketHttpResponse response, string fieldName)
    {
        return response.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value;
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static WebSocketExtensionOffer Offer(
        string name,
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = name,
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Parameter(string name, string value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = true,
            Value = value
        };
    }

    static WebSocketHandshakeResult AcceptedServerHandshake(
        WebSocketHandshakeTransport transport)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        return handshake.EvaluateExtendedConnectRequest(
            Request(transport),
            ServerOptions(transport));
    }

    static WebSocketConnection ServerConnection(WebSocketHandshakeResult handshake)
    {
        IWebSocketServerHandshake handshaker = new WebSocketServerHandshake();
        WebSocketConnectionOptions options = handshaker.CreateServerConnectionOptions(
            handshake,
            new WebSocketConnectionOptions());
        return new WebSocketConnection(options, handshake);
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }
}
