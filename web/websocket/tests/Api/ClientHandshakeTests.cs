using websocket;

namespace websocket.Tests;

public class ClientHandshakeTests
{
    [Test]
    public void RepeatedPublicRequestCreationUsesFreshCanonicalClientKeys()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        HashSet<string> keys = [];

        for (int index = 0; index < 32; index++)
        {
            WebSocketHandshakeResult result =
                handshake.CreateHttp1Request(Options());
            byte[] nonce =
                new byte[WebSocketProtocolConstants.ClientNonceLength];
            Base64DecodeResult decoding =
                Base64Codec.DecodeClientNonce(result.ClientKey, nonce);

            Assert.Multiple(() =>
            {
                Assert.That(result.Outcome,
                    Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
                Assert.That(result.HasClientKey, Is.True);
                Assert.That(result.ClientKey, Has.Length.EqualTo(24));
                Assert.That(result.ClientKey, Does.EndWith("=="));
                Assert.That(Value(result.Request, "Sec-WebSocket-Key"),
                    Is.EqualTo(result.ClientKey));
                Assert.That(decoding.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(decoding.ConsumedCount,
                    Is.EqualTo(result.ClientKey.Length));
                Assert.That(decoding.ProducedCount,
                    Is.EqualTo(WebSocketProtocolConstants.ClientNonceLength));
            });

            keys.Add(result.ClientKey);
        }

        Assert.That(keys.Count, Is.EqualTo(32));
    }

    [Test]
    public void CreatesHttp1RequestThroughThePublicContract()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.HasOrigin = true;
        options.Origin = "https://example.com";
        options.Subprotocols = ["chat"];
        options.ExtraFields = [Field("Cookie", "session=abc")];

        var result = handshake.CreateHttp1Request(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.Request.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(Value(result.Request, "Host"), Is.EqualTo("example.com"));
            Assert.That(Value(result.Request, "Origin"), Is.EqualTo("https://example.com"));
            Assert.That(Value(result.Request, "Sec-WebSocket-Protocol"), Is.EqualTo("chat"));
            Assert.That(Value(result.Request, "Cookie"), Is.EqualTo("session=abc"));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.Not.Empty);
            Assert.That(Http1Writer.MeasureRequest(result.Request).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void AcceptedResultContainsCompleteNegotiationMetadata()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.Subprotocols = ["chat", "superchat"];
        options.Extensions = [Offer("x-mode", Parameter("speed", "fast"))];
        var created = handshake.CreateHttp1Request(options);
        var response = Success(created.ClientKey);
        response.Fields = response.Fields.Concat(new[]
        {
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("Sec-WebSocket-Extensions", "x-mode; speed=fast")
        }).ToArray();

        var result = handshake.ValidateHttp1Response(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(Value(result.Request, "Host"), Is.EqualTo("example.com"));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response, Is.Not.SameAs(response));
            Assert.That(result.Response.StatusCode, Is.EqualTo(101));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.EqualTo(created.ClientKey));
            Assert.That(result.HasSelectedSubprotocol, Is.True);
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(result.Extensions[0].Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(result.HasError, Is.False);
        });
    }

    [Test]
    public void RetainedRequestIsIndependentFromTheConstructionResult()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var created = handshake.CreateHttp1Request(Options());
        var clientKey = created.ClientKey;
        created.Request.RequestTarget = "/mutated";
        created.Request.Fields[0].Name = "X-Mutated";
        created.Request.Fields[0].Value = "mutated.example";

        var accepted = handshake.ValidateHttp1Response(Success(clientKey));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Request, Is.Not.SameAs(created.Request));
            Assert.That(accepted.Request.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(accepted.Request.Fields[0].Name, Is.EqualTo("Host"));
            Assert.That(accepted.Request.Fields[0].Value, Is.EqualTo("example.com"));
        });
    }

    [Test]
    public void InformationalResponseRetainsTheRequestForTheFinalResponse()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var created = handshake.CreateHttp1Request(Options());
        var informationalResponse = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 103,
            HasReasonPhrase = true,
            ReasonPhrase = "Early Hints",
            Fields = [Field("Link", "</style.css>; rel=preload")]
        };

        var informational = handshake.ValidateHttp1Response(informationalResponse);
        informational.Request.RequestTarget = "/mutated";
        var accepted = handshake.ValidateHttp1Response(Success(created.ClientKey));

        Assert.Multiple(() =>
        {
            Assert.That(informational.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(informational.HasRequest, Is.True);
            Assert.That(informational.HasResponse, Is.True);
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(accepted.Request.RequestTarget, Is.EqualTo("/chat?room=blue"));
        });
    }

    [Test]
    public void FinalRejectionAndValidationFailureIncludeBothHandshakeMessages()
    {
        IWebSocketClientHandshake rejectedHandshake = new WebSocketClientHandshake();
        rejectedHandshake.CreateHttp1Request(Options());
        var rejection = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 401,
            HasReasonPhrase = true,
            ReasonPhrase = "Unauthorized",
            Fields = [Field("WWW-Authenticate", "Basic realm=test")]
        };
        var rejected = rejectedHandshake.ValidateHttp1Response(rejection);

        IWebSocketClientHandshake failedHandshake = new WebSocketClientHandshake();
        var created = failedHandshake.CreateHttp1Request(Options());
        var failureResponse = Success(created.ClientKey);
        ValueField(failureResponse, "Sec-WebSocket-Accept").Value =
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var failed = failedHandshake.ValidateHttp1Response(failureResponse);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(rejected.HasRequest, Is.True);
            Assert.That(rejected.HasResponse, Is.True);
            Assert.That(failed.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(failed.HasRequest, Is.True);
            Assert.That(failed.HasResponse, Is.True);
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("websocket-response-accept-invalid"));
            Assert.Throws<WebSocketStateException>(() =>
                rejectedHandshake.ValidateHttp1Response(rejection));
            Assert.Throws<WebSocketStateException>(() =>
                failedHandshake.ValidateHttp1Response(failureResponse));
        });
    }

    [Test]
    public void FailedRecreationClearsTheEarlierPendingRequest()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var created = handshake.CreateHttp1Request(Options());
        var invalid = Options();
        invalid.Uri = "http://example.com/";

        Assert.Throws<WebSocketHandshakeException>(() => handshake.CreateHttp1Request(invalid));
        Assert.Throws<WebSocketStateException>(() =>
            handshake.ValidateHttp1Response(Success(created.ClientKey)));
    }

    [Test]
    public void Http3ExtendedConnectRequiresItsIndependentlyAdvertisedSetting()
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = Options();
        options.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        options.PeerEnabledExtendedConnect = false;
        var response = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http3ExtendedConnect,
            StatusCode = 200
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                handshake.CreateExtendedConnectRequest(options));
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(response));
        });
    }

    static WebSocketClientHandshakeOptions Options()
    {
        return new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat?room=blue",
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
    }

    static WebSocketHttpResponse Success(string clientKey)
    {
        var accept = WebSocketHandshakeCryptography.ComputeAccept(clientKey);
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 101,
            HasReasonPhrase = true,
            ReasonPhrase = "Switching Protocols",
            Fields =
            [
                Field("Upgrade", "websocket"),
                Field("Connection", "Upgrade"),
                Field("Sec-WebSocket-Accept", accept.Value)
            ]
        };
    }

    static string Value(WebSocketHttpRequest request, string fieldName)
    {
        return request.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value;
    }

    static WebSocketHeaderField ValueField(WebSocketHttpResponse response, string fieldName)
    {
        return response.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase));
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
