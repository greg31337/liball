using websocket;

namespace websocket.Tests;

public class Http2ClientHandshakeTests
{
    [Test]
    public void PeerSettingGatesHttp2ExtendedConnectConstruction()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var disabled = Options();
        disabled.PeerEnabledExtendedConnect = false;

        Assert.Throws<WebSocketHandshakeException>(() =>
            handshake.CreateExtendedConnectRequest(disabled));
        Assert.Throws<WebSocketStateException>(() =>
            handshake.ValidateExtendedConnectResponse(Response(200)));
    }

    [Test]
    public void CreatesNormalizedHttp2ExtendedConnectRequestWithoutHttp1Fields()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.Uri = "WSS://BÜCHER.example:443/a/../chat?q=naïve";

        var result = handshake.CreateExtendedConnectRequest(options);
        var request = result.Request;

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.HasClientKey, Is.False);
            Assert.That(request.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(request.Method, Is.EqualTo("CONNECT"));
            Assert.That(request.HasRequestTarget, Is.False);
            Assert.That(request.RequestTarget, Is.Empty);
            Assert.That(request.HasHttpVersion, Is.False);
            Assert.That(request.HttpVersion, Is.Empty);
            Assert.That(request.HasScheme, Is.True);
            Assert.That(request.Scheme, Is.EqualTo("https"));
            Assert.That(request.HasAuthority, Is.True);
            Assert.That(request.Authority, Is.EqualTo("xn--bcher-kva.example:443"));
            Assert.That(request.HasPath, Is.True);
            Assert.That(request.Path, Is.EqualTo("/chat?q=na%C3%AFve"));
            Assert.That(request.HasProtocol, Is.True);
            Assert.That(request.Protocol, Is.EqualTo("websocket"));
            Assert.That(request.Fields, Has.Length.EqualTo(1));
            Assert.That(request.Fields[0].Name, Is.EqualTo("sec-websocket-version"));
            Assert.That(request.Fields[0].Value, Is.EqualTo("13"));
            Assert.That(request.Fields.Any(field => field.Name == "host"), Is.False);
            Assert.That(request.Fields.Any(field => field.Name == "connection"), Is.False);
            Assert.That(request.Fields.Any(field => field.Name == "upgrade"), Is.False);
            Assert.That(request.Fields.Any(field => field.Name == "sec-websocket-key"), Is.False);
            Assert.That(ExtendedConnectValidator.ValidateRequest(
                request,
                WebSocketHandshakeTransport.Http2ExtendedConnect).Valid, Is.True);
        });
    }

    [Test]
    public void AddsNormalizedOptionalWebSocketAndApplicationFields()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.HasOrigin = true;
        options.Origin = "HTTPS://EXAMPLE.com:443";
        options.Subprotocols = ["chat", "superchat"];
        options.Extensions = [Offer("x-mode", Parameter("speed", "fast"))];
        options.HasUserAgent = true;
        options.UserAgent = "websocket-tests/1.0";
        options.ExtraFields =
        [
            Field("cookie", "session=abc"),
            Field("authorization", "Bearer token")
        ];

        var result = handshake.CreateExtendedConnectRequest(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "sec-websocket-version",
                "origin",
                "sec-websocket-protocol",
                "sec-websocket-extensions",
                "user-agent",
                "cookie",
                "authorization"
            }));
            Assert.That(Value(result.Request, "origin"), Is.EqualTo("https://example.com"));
            Assert.That(Value(result.Request, "sec-websocket-protocol"),
                Is.EqualTo("chat, superchat"));
            Assert.That(Value(result.Request, "sec-websocket-extensions"),
                Is.EqualTo("x-mode; speed=fast"));
            Assert.That(Value(result.Request, "user-agent"),
                Is.EqualTo("websocket-tests/1.0"));
        });
    }

    [TestCase("Origin")]
    [TestCase("x-Test")]
    [TestCase("host")]
    [TestCase("connection")]
    [TestCase("upgrade")]
    [TestCase("sec-websocket-key")]
    [TestCase("sec-websocket-accept")]
    [TestCase("sec-websocket-version")]
    [TestCase("sec-websocket-protocol")]
    [TestCase("sec-websocket-extensions")]
    public void RejectsUnsafeUppercaseForbiddenOrCriticalExtraFields(string fieldName)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.ExtraFields = [Field(fieldName, "value")];

        Assert.Throws<WebSocketHandshakeException>(() =>
            handshake.CreateExtendedConnectRequest(options));
    }

    [TestCase(200)]
    [TestCase(201)]
    [TestCase(204)]
    [TestCase(299)]
    public void AcceptsAnyValid2xxResponseWithCompleteMetadata(int statusCode)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var created = handshake.CreateExtendedConnectRequest(Options());
        var response = Response(statusCode);

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request, Is.Not.SameAs(created.Request));
            Assert.That(result.Request.Path, Is.EqualTo("/chat"));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response, Is.Not.SameAs(response));
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.HasClientKey, Is.False);
            Assert.That(result.HasSelectedSubprotocol, Is.False);
            Assert.That(result.Extensions, Is.Empty);
            Assert.That(result.HasError, Is.False);
        });
    }

    [Test]
    public void ReturnsSelectedSubprotocolAndNegotiatedExtension()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.Subprotocols = ["chat", "superchat"];
        options.Extensions =
        [
            Offer("x-mode", Parameter("speed", "fast")),
            Offer("x-mode", Parameter("speed", "slow"))
        ];
        handshake.CreateExtendedConnectRequest(options);
        var response = Response(200);
        response.Fields =
        [
            Field("sec-websocket-protocol", "superchat"),
            Field("sec-websocket-extensions", "x-mode; speed=slow")
        ];

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.HasSelectedSubprotocol, Is.True);
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("superchat"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(result.Extensions[0].Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("slow"));
        });
    }

    [Test]
    public void RetainedOffersAndRequestAreIndependentFromCallerMutation()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var protocol = new[] { "chat" };
        var parameter = Parameter("speed", "fast");
        var extension = Offer("x-mode", parameter);
        var options = Options();
        options.Subprotocols = protocol;
        options.Extensions = [extension];
        var created = handshake.CreateExtendedConnectRequest(options);

        protocol[0] = "mutated";
        extension.Name = "mutated";
        parameter.Value = "slow";
        created.Request.Path = "/mutated";
        created.Request.Fields[0].Name = "mutated";
        var response = Response(200);
        response.Fields =
        [
            Field("sec-websocket-protocol", "chat"),
            Field("sec-websocket-extensions", "x-mode; speed=fast")
        ];

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(result.Request.Path, Is.EqualTo("/chat"));
            Assert.That(result.Request.Fields[0].Name, Is.EqualTo("sec-websocket-version"));
        });
    }

    [Test]
    public void InformationalResponseDoesNotConsumePendingRequest()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(Options());

        var informational = handshake.ValidateExtendedConnectResponse(Response(103));
        var accepted = handshake.ValidateExtendedConnectResponse(Response(200));

        Assert.Multiple(() =>
        {
            Assert.That(informational.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(informational.HasRequest, Is.True);
            Assert.That(informational.HasResponse, Is.True);
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
        });
    }

    [Test]
    public void Non2xxResponseIsADeepCopiedTerminalRejection()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(Options());
        var body = new byte[] { 1, 2, 3 };
        var response = Response(404);
        response.Fields = [Field("content-type", "application/octet-stream")];
        response.HasBody = true;
        response.Body = body;

        var result = handshake.ValidateExtendedConnectResponse(response);
        response.Fields[0].Value = "mutated";
        body[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(404));
            Assert.That(result.Response.Fields[0].Value,
                Is.EqualTo("application/octet-stream"));
            Assert.That(result.Response.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.HasError, Is.False);
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(response));
        });
    }

    [TestCase(300)]
    [TestCase(399)]
    [TestCase(400)]
    [TestCase(499)]
    [TestCase(500)]
    [TestCase(599)]
    public void EveryFinalNon2xxStatusCategoryIsATerminalRejection(int statusCode)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(Options());

        var result = handshake.ValidateExtendedConnectResponse(Response(statusCode));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.HasError, Is.False);
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(Response(statusCode)));
        });
    }

    [Test]
    public void AcceptedHttp2ConnectionsMapEndStreamAndResetStreamDistinctly()
    {
        WebSocketHandshakeResult endHandshake = AcceptedHandshake();
        WebSocketHandshakeResult resetHandshake = AcceptedHandshake();
        WebSocketConnection endConnection = Connection(endHandshake);
        WebSocketConnection resetConnection = Connection(resetHandshake);

        WebSocketConnectionTransitionResult ended = endConnection.EndConnectionInput();
        WebSocketConnectionTransitionResult reset = resetConnection.ResetConnectionTransport();
        WebSocketConnectionInfo endedInfo = endConnection.GetConnectionInfo();
        WebSocketConnectionInfo resetInfo = resetConnection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(ended.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(ended.State, Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(ended.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
            Assert.That(ended.Error.Identifier,
                Is.EqualTo("connection-http2-end-stream-before-close"));
            Assert.That(endedInfo.Options.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(endedInfo.TransportEnded, Is.True);
            Assert.That(endedInfo.TransportReset, Is.False);
            Assert.That(reset.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(reset.State, Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(reset.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
            Assert.That(reset.Error.Identifier,
                Is.EqualTo("connection-http2-stream-reset"));
            Assert.That(resetInfo.Options.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(resetInfo.TransportEnded, Is.True);
            Assert.That(resetInfo.TransportReset, Is.True);
        });
    }

    [TestCase("wrong-transport", "extended-connect-response-transport-invalid")]
    [TestCase("uppercase-field", "extended-connect-field-name-not-lowercase")]
    [TestCase("forbidden-field", "extended-connect-field-forbidden")]
    [TestCase("status-101", "extended-connect-response-status-invalid")]
    public void InvalidResponseFailsWithStableIdentifierAndConsumesState(
        string mutation,
        string identifier)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(Options());
        var response = Response(200);
        MutateResponse(response, mutation);

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.HasRequest, Is.True);
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(response));
        });
    }

    [TestCase("unoffered-protocol", "extended-connect-response-subprotocol-invalid")]
    [TestCase("multiple-protocols", "extended-connect-response-subprotocol-invalid")]
    [TestCase("unoffered-extension", "extended-connect-response-extensions-invalid")]
    [TestCase("repeated-extension", "extended-connect-response-extensions-invalid")]
    public void RejectsInvalidNegotiationSelections(string mutation, string identifier)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.Subprotocols = ["chat"];
        options.Extensions = [Offer("x-test")];
        handshake.CreateExtendedConnectRequest(options);
        var response = Response(200);
        MutateNegotiation(response, mutation);

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    static WebSocketClientHandshakeOptions Options()
    {
        return new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat",
            Transport = WebSocketHandshakeTransport.Http2ExtendedConnect,
            PeerEnabledExtendedConnect = true
        };
    }

    static WebSocketHandshakeResult AcceptedHandshake()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        handshake.CreateExtendedConnectRequest(Options());
        return handshake.ValidateExtendedConnectResponse(Response(200));
    }

    static WebSocketConnection Connection(WebSocketHandshakeResult handshake)
    {
        IWebSocketClientHandshake handshaker = new WebSocketClientHandshake();
        WebSocketConnectionOptions options = handshaker.CreateClientConnectionOptions(
            handshake,
            new WebSocketConnectionOptions());
        return new WebSocketConnection(options, handshake);
    }

    static WebSocketHttpResponse Response(int statusCode)
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http2ExtendedConnect,
            StatusCode = statusCode
        };
    }

    static void MutateResponse(WebSocketHttpResponse response, string mutation)
    {
        if (mutation == "wrong-transport")
        {
            response.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        }
        else if (mutation == "uppercase-field")
        {
            response.Fields = [Field("X-Test", "value")];
        }
        else if (mutation == "forbidden-field")
        {
            response.Fields = [Field("upgrade", "websocket")];
        }
        else
        {
            response.StatusCode = 101;
        }
    }

    static void MutateNegotiation(WebSocketHttpResponse response, string mutation)
    {
        if (mutation == "unoffered-protocol")
        {
            response.Fields = [Field("sec-websocket-protocol", "other")];
        }
        else if (mutation == "multiple-protocols")
        {
            response.Fields = [Field("sec-websocket-protocol", "chat, other")];
        }
        else if (mutation == "unoffered-extension")
        {
            response.Fields = [Field("sec-websocket-extensions", "other")];
        }
        else
        {
            response.Fields =
            [
                Field("sec-websocket-extensions", "x-test"),
                Field("sec-websocket-extensions", "x-test")
            ];
        }
    }

    static string Value(WebSocketHttpRequest request, string fieldName)
    {
        return request.Fields.Single(field => string.Equals(
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
}
