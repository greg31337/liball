using websocket;

namespace websocket.Tests;

public class GenericExtensionNegotiationTests
{
    [Test]
    public void ClientHandlerCreatesOfferAndValidatesSelectedConfiguration()
    {
        var extension = new ConfigurableNegotiationExtension("x-mode", "fast", "slow");
        extension.ClaimsRsv1 = true;
        extension.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [extension];

        var created = handshake.CreateExtendedConnectRequest(options);
        var response = Response("x-mode; mode=slow");
        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(created.Request, "sec-websocket-extensions"),
                Is.EqualTo("x-mode; mode=fast"));
            Assert.That(extension.CreateOfferCount, Is.EqualTo(1));
            Assert.That(extension.ValidatedResponses, Has.Count.EqualTo(1));
            Assert.That(extension.ValidatedResponses[0].Parameters[0].Value,
                Is.EqualTo("slow"));
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("slow"));
            Assert.That(result.Extensions[0].ClaimsRsv1, Is.True);
            Assert.That(result.Extensions[0].ClaimedOpcodes,
                Is.EqualTo(new[] { WebSocketOpcode.ReservedData3 }));
        });
    }

    [Test]
    public void GenericNegotiationCompletesAcrossAnHttp1ClientServerHandshake()
    {
        var clientExtension = new ConfigurableNegotiationExtension(
            "x-mode",
            "fast",
            "fast");
        var serverExtension = new ConfigurableNegotiationExtension(
            "x-mode",
            "",
            "fast");
        IWebSocketClientHandshake client = new WebSocketClientHandshake();
        IWebSocketServerHandshake server = new WebSocketServerHandshake();
        var clientOptions = new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat",
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            ExtensionHandlers = [clientExtension]
        };
        var serverOptions = new WebSocketServerHandshakeOptions
        {
            ExtensionHandlers = [serverExtension]
        };

        var request = client.CreateHttp1Request(clientOptions);
        var serverResult = server.EvaluateHttp1Request(request.Request, serverOptions);
        var clientResult = client.ValidateHttp1Response(serverResult.Response);

        Assert.Multiple(() =>
        {
            Assert.That(serverResult.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(clientResult.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(serverResult.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(clientResult.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(serverExtension.AcceptedOffers, Has.Count.EqualTo(1));
            Assert.That(clientExtension.ValidatedResponses, Has.Count.EqualTo(1));
        });
    }

    [TestCase("duplicate")]
    [TestCase("unoffered")]
    [TestCase("incompatible")]
    public void ClientRejectsDuplicateUnofferedOrIncompatibleSelections(string mutation)
    {
        var extension = new ConfigurableNegotiationExtension("x-mode", "fast", "slow");
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [extension];
        handshake.CreateExtendedConnectRequest(options);
        WebSocketHttpResponse response;
        if (mutation == "duplicate")
        {
            response = Response("x-mode; mode=slow, x-mode; mode=slow");
        }
        else if (mutation == "unoffered")
        {
            response = Response("other; mode=slow");
        }
        else
        {
            response = Response("x-mode; mode=other");
        }

        var result = handshake.ValidateExtendedConnectResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-response-extensions-invalid"));
            Assert.Throws<WebSocketStateException>(() =>
                handshake.ValidateExtendedConnectResponse(response));
        });
    }

    [Test]
    public void ClientPreservesServerSelectedExtensionOrder()
    {
        var first = new ConfigurableNegotiationExtension("x-first", "one", "one");
        var second = new ConfigurableNegotiationExtension("x-second", "two", "two");
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [first, second];
        handshake.CreateExtendedConnectRequest(options);

        var result = handshake.ValidateExtendedConnectResponse(
            Response("x-second; mode=two, x-first; mode=one"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "x-second", "x-first" }));
            Assert.That(first.ValidatedResponses, Has.Count.EqualTo(1));
            Assert.That(second.ValidatedResponses, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ClientRejectsResponseParametersReportedInvalidByHandler()
    {
        var extension = new ConfigurableNegotiationExtension("x-mode", "fast", "slow");
        extension.RejectDuplicateParameters = true;
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [extension];
        handshake.CreateExtendedConnectRequest(options);

        var result = handshake.ValidateExtendedConnectResponse(
            Response("x-mode; mode=slow; mode=slow"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-response-extensions-invalid"));
            Assert.That(extension.ValidatedResponses[0].Parameters,
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ServerAcceptsHandshakeWhenEveryExtensionIsDeclined()
    {
        var extension = new ConfigurableNegotiationExtension("x-mode", "", "good");
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = ServerOptions();
        options.ExtensionHandlers = [extension];

        var result = handshake.EvaluateExtendedConnectRequest(
            Request("x-mode; mode=bad, other; mode=good"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions, Is.Empty);
            Assert.That(result.Response.Fields.Any(field =>
                field.Name == "sec-websocket-extensions"), Is.False);
            Assert.That(extension.AcceptedOffers, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ServerSelectsHandlersByServerOrderAndAlternativesByClientOrder()
    {
        var yExtension = new ConfigurableNegotiationExtension("y-mode", "", "good");
        yExtension.ClaimsRsv2 = true;
        yExtension.ClaimsRsv3 = true;
        var xExtension = new ConfigurableNegotiationExtension("x-mode", "", "fast");
        xExtension.ClaimedOpcodes = [WebSocketOpcode.ReservedData4];
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = ServerOptions();
        options.ExtensionHandlers = [yExtension, xExtension];
        var request = Request(
            "x-mode; mode=bad, x-mode; mode=fast, y-mode; mode=good");

        var result = handshake.EvaluateExtendedConnectRequest(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "y-mode", "x-mode" }));
            Assert.That(result.Extensions[0].ClaimsRsv2, Is.True);
            Assert.That(result.Extensions[0].ClaimsRsv3, Is.True);
            Assert.That(result.Extensions[1].ClaimedOpcodes,
                Is.EqualTo(new[] { WebSocketOpcode.ReservedData4 }));
            Assert.That(Value(result.Response, "sec-websocket-extensions"),
                Is.EqualTo("y-mode; mode=good, x-mode; mode=fast"));
            Assert.That(yExtension.AcceptedOffers.Select(offer => offer.Name),
                Is.EqualTo(new[] { "x-mode", "x-mode", "y-mode" }));
            Assert.That(xExtension.AcceptedOffers.Select(offer =>
                offer.Parameters[0].Value),
                Is.EqualTo(new[] { "bad", "fast" }));
        });
    }

    [Test]
    public void ServerEmitsAtMostOneSelectionForDuplicateHandlerNames()
    {
        var first = new ConfigurableNegotiationExtension("x-mode", "", "fast");
        var second = new ConfigurableNegotiationExtension("x-mode", "", "fast");
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = ServerOptions();
        options.ExtensionHandlers = [first, second];

        var result = handshake.EvaluateExtendedConnectRequest(
            Request("x-mode; mode=fast"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Response.Fields, Has.Length.EqualTo(1));
            Assert.That(Value(result.Response, "sec-websocket-extensions"),
                Is.EqualTo("x-mode; mode=fast"));
        });
    }

    [Test]
    public void ServerRejectsMalformedParametersReportedByHandler()
    {
        var extension = new ConfigurableNegotiationExtension("x-mode", "", "fast");
        extension.RejectDuplicateParameters = true;
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = ServerOptions();
        options.ExtensionHandlers = [extension];

        var result = handshake.EvaluateExtendedConnectRequest(
            Request("x-mode; mode=fast; mode=slow"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-extensions-invalid"));
            Assert.That(extension.AcceptedOffers[0].Parameters, Has.Length.EqualTo(2));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ServerRejectsNegotiatedRsvOrOpcodeOwnershipConflicts(
        bool rsvConflict)
    {
        var first = new ConfigurableNegotiationExtension("x-first", "", "one");
        var second = new ConfigurableNegotiationExtension("x-second", "", "two");
        if (rsvConflict)
        {
            first.ClaimsRsv1 = true;
            second.ClaimsRsv1 = true;
        }
        else
        {
            first.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
            second.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
        }

        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = ServerOptions();
        options.ExtensionHandlers = [first, second];

        var result = handshake.EvaluateExtendedConnectRequest(
            Request("x-first; mode=one, x-second; mode=two"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-extensions-invalid"));
        });
    }

    [Test]
    public void ClientRejectsConflictingClaimsInSelectedExtensions()
    {
        var first = new ConfigurableNegotiationExtension("x-first", "one", "one");
        var second = new ConfigurableNegotiationExtension("x-second", "two", "two");
        first.ClaimsRsv2 = true;
        second.ClaimsRsv2 = true;
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [first, second];
        handshake.CreateExtendedConnectRequest(options);

        var result = handshake.ValidateExtendedConnectResponse(
            Response("x-first; mode=one, x-second; mode=two"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extended-connect-response-extensions-invalid"));
        });
    }

    [Test]
    public void ClientRejectsInvalidGeneratedOfferBeforeRetainingState()
    {
        var extension = new ConfigurableNegotiationExtension("bad name", "fast", "fast");
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        var options = ClientOptions();
        options.ExtensionHandlers = [extension];

        Assert.Throws<WebSocketHandshakeException>(() =>
            handshake.CreateExtendedConnectRequest(options));
        Assert.Throws<WebSocketStateException>(() =>
            handshake.ValidateExtendedConnectResponse(Response("x-mode")));
    }

    [Test]
    public void NegotiatedConnectionAppliesOutboundAndInboundTransformsInOrder()
    {
        var calls = new List<string>();
        var first = new ConfigurableNegotiationExtension("x-first", "", "one");
        first.TransformMarker = 1;
        first.TransformCalls = calls;
        var second = new ConfigurableNegotiationExtension("x-second", "", "two");
        second.TransformMarker = 2;
        second.TransformCalls = calls;
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var handshakeOptions = ServerOptions();
        handshakeOptions.ExtensionHandlers = [first, second];
        WebSocketHandshakeResult result = handshake.EvaluateExtendedConnectRequest(
            Request("x-first; mode=one, x-second; mode=two"),
            handshakeOptions);
        var template = new WebSocketConnectionOptions
        {
            Extensions = [first, second]
        };
        WebSocketConnectionOptions connectionOptions =
            handshake.CreateServerConnectionOptions(result, template);
        var connection = new WebSocketConnection(connectionOptions, result);

        WebSocketParseStatus sent = connection.SendWholeMessage(
            new WebSocketSendRequest
            {
                MessageType = WebSocketMessageType.Binary,
                Payload = [9],
                PayloadCount = 1
            });
        byte[] output = DrainAll(connection);
        WebSocketInboundFramePumpResult received = connection.ReceiveConnectionBytes(
            [0x82, 0x83, 1, 2, 3, 4, 8, 3, 1]);
        WebSocketEvent? message = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(output, Is.EqualTo(new byte[] { 0x82, 0x03, 9, 1, 2 }));
            Assert.That(received.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.Payload, Is.EqualTo(new byte[] { 9 }));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "out-x-first",
                "out-x-second",
                "in-x-second",
                "in-x-first"
            }));
        });
    }

    static WebSocketClientHandshakeOptions ClientOptions()
    {
        return new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat",
            Transport = WebSocketHandshakeTransport.Http3ExtendedConnect,
            PeerEnabledExtendedConnect = true
        };
    }

    static WebSocketServerHandshakeOptions ServerOptions()
    {
        return new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [WebSocketHandshakeTransport.Http3ExtendedConnect],
            ExtendedConnectEnabled = true
        };
    }

    static WebSocketHttpRequest Request(string extensionValue)
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http3ExtendedConnect,
            Method = "CONNECT",
            HasScheme = true,
            Scheme = "https",
            HasAuthority = true,
            Authority = "example.com",
            HasPath = true,
            Path = "/chat",
            HasProtocol = true,
            Protocol = "websocket",
            Fields =
            [
                Field("sec-websocket-version", "13"),
                Field("sec-websocket-extensions", extensionValue)
            ]
        };
    }

    static WebSocketHttpResponse Response(string extensionValue)
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http3ExtendedConnect,
            StatusCode = 200,
            Fields = [Field("sec-websocket-extensions", extensionValue)]
        };
    }

    static string Value(WebSocketHttpRequest request, string name)
    {
        return request.Fields.Single(field => field.Name == name).Value;
    }

    static string Value(WebSocketHttpResponse response, string name)
    {
        return response.Fields.Single(field => field.Name == name).Value;
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }
}

internal class ConfigurableNegotiationExtension : IWebSocketExtension
{
    internal string Name;
    internal string ClientOfferMode;
    internal string AcceptedMode;
    internal bool ClaimsRsv1;
    internal bool ClaimsRsv2;
    internal bool ClaimsRsv3;
    internal WebSocketOpcode[] ClaimedOpcodes = [];
    internal bool RejectDuplicateParameters;
    internal byte TransformMarker;
    internal List<string> TransformCalls = [];
    internal int CreateOfferCount;
    internal List<WebSocketExtensionOffer> AcceptedOffers = [];
    internal List<WebSocketExtensionOffer> ValidatedResponses = [];

    internal ConfigurableNegotiationExtension(
        string name,
        string clientOfferMode,
        string acceptedMode)
    {
        Name = name;
        ClientOfferMode = clientOfferMode;
        AcceptedMode = acceptedMode;
    }

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        CreateOfferCount++;
        return Offer(Name, ClientOfferMode);
    }

    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(
        WebSocketExtensionOffer offer)
    {
        AcceptedOffers.Add(offer);
        if (offer.Name != Name)
        {
            return Declined(Name);
        }

        if (RejectDuplicateParameters && HasDuplicateParameter(offer))
        {
            var invalid = new WebSocketExtensionNegotiation();
            invalid.Name = Name;
            invalid.Status = WebSocketExtensionStatus.Invalid;
            invalid.HasError = true;
            invalid.Error.Kind = WebSocketErrorKind.Handshake;
            invalid.Error.Identifier = "test-duplicate-parameter";
            return invalid;
        }

        if (Mode(offer) != AcceptedMode)
        {
            return Declined(Name);
        }

        return Negotiated(offer);
    }

    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(
        WebSocketExtensionOffer response)
    {
        ValidatedResponses.Add(response);
        if (RejectDuplicateParameters && HasDuplicateParameter(response))
        {
            return Invalid(Name);
        }

        if (response.Name != Name || Mode(response) != AcceptedMode)
        {
            return Declined(Name);
        }

        return Negotiated(response);
    }

    public WebSocketTransformResult TransformOutboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        TransformCalls.Add("out-" + Name);
        if (TransformMarker == 0)
        {
            return Transform(source);
        }

        byte[] output = new byte[source.Length + 1];
        source.CopyTo(output);
        output[source.Length] = TransformMarker;
        return Transform(source.Length, output);
    }

    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        TransformCalls.Add("in-" + Name);
        if (TransformMarker == 0)
        {
            return Transform(source);
        }

        if (source.Length == 0 || source[source.Length - 1] != TransformMarker)
        {
            var failure = new WebSocketTransformResult();
            failure.Status = WebSocketParseStatus.ProtocolFailure;
            failure.HasError = true;
            failure.Error.Kind = WebSocketErrorKind.Protocol;
            failure.Error.Identifier = "test-transform-marker-missing";
            return failure;
        }

        return Transform(source.Length, source.Slice(0, source.Length - 1).ToArray());
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
    }

    public void ResetExtensionState()
    {
    }

    WebSocketExtensionNegotiation Negotiated(WebSocketExtensionOffer configuration)
    {
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Name = Name;
        negotiation.Status = WebSocketExtensionStatus.Negotiated;
        negotiation.HasAcceptedConfiguration = true;
        negotiation.AcceptedConfiguration = Copy(configuration);
        negotiation.ClaimsRsv1 = ClaimsRsv1;
        negotiation.ClaimsRsv2 = ClaimsRsv2;
        negotiation.ClaimsRsv3 = ClaimsRsv3;
        negotiation.ClaimedOpcodes = ClaimedOpcodes.ToArray();
        return negotiation;
    }

    static WebSocketExtensionNegotiation Declined(string name)
    {
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Name = name;
        negotiation.Status = WebSocketExtensionStatus.Declined;
        return negotiation;
    }

    static WebSocketExtensionNegotiation Invalid(string name)
    {
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Name = name;
        negotiation.Status = WebSocketExtensionStatus.Invalid;
        return negotiation;
    }

    static WebSocketExtensionOffer Offer(string name, string mode)
    {
        var offer = new WebSocketExtensionOffer();
        offer.Name = name;
        if (mode.Length != 0)
        {
            offer.Parameters =
            [
                new WebSocketExtensionParameter
                {
                    Name = "mode",
                    HasValue = true,
                    Value = mode
                }
            ];
        }

        return offer;
    }

    static string Mode(WebSocketExtensionOffer offer)
    {
        foreach (var parameter in offer.Parameters)
        {
            if (parameter.Name == "mode" && parameter.HasValue)
            {
                return parameter.Value;
            }
        }

        return "";
    }

    static bool HasDuplicateParameter(WebSocketExtensionOffer offer)
    {
        for (var first = 0; first < offer.Parameters.Length; first++)
        {
            for (var second = first + 1; second < offer.Parameters.Length; second++)
            {
                if (offer.Parameters[first].Name == offer.Parameters[second].Name)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static WebSocketExtensionOffer Copy(WebSocketExtensionOffer source)
    {
        return new WebSocketExtensionOffer
        {
            Name = source.Name,
            Parameters = source.Parameters.Select(parameter =>
                new WebSocketExtensionParameter
                {
                    Name = parameter.Name,
                    HasValue = parameter.HasValue,
                    Value = parameter.Value
                }).ToArray()
        };
    }

    static WebSocketTransformResult Transform(ReadOnlySpan<byte> source)
    {
        return Transform(source.Length, source.ToArray());
    }

    static WebSocketTransformResult Transform(int consumedCount, byte[] output)
    {
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = consumedCount,
            ProducedCount = output.Length,
            Output = output
        };
    }
}
