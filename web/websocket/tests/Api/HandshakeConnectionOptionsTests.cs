using websocket;

namespace websocket.Tests;

public class HandshakeConnectionOptionsTests
{
    [Test]
    public void ServerTransferFreezesNegotiationOrderConfigurationAndSecurityLimits()
    {
        IWebSocketServerHandshake handshaker = new WebSocketServerHandshake();
        var firstExtension = new DataTestExtension();
        var secondExtension = new DataTestExtension();
        var template = ServerTemplate(firstExtension, secondExtension);
        var handshake = AcceptedHandshake();

        var options = handshaker.CreateServerConnectionOptions(handshake, template);

        handshake.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        handshake.SelectedSubprotocol = "mutated";
        handshake.Extensions[0].Name = "mutated";
        handshake.Extensions[0].AcceptedConfiguration.Name = "mutated";
        handshake.Extensions[0].AcceptedConfiguration.Parameters[0].Value = "mutated";
        handshake.Extensions[0].ClaimedOpcodes[0] = WebSocketOpcode.ReservedData7;
        handshake.Extensions[0].Error.Identifier = "mutated";
        handshake.Extensions = [];
        template.Extensions[0] = new DataTestExtension();
        template.NegotiatedExtensions = [];
        template.PerMessageDeflate.ServerMaxWindowBits = 8;
        template.Heartbeat.TokenLength = 1;
        template.MaximumFramePayloadLength = 1;

        Assert.Multiple(() =>
        {
            Assert.That(options.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(options.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(options.HasSelectedSubprotocol, Is.True);
            Assert.That(options.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(options.Extensions, Is.Not.SameAs(template.Extensions));
            Assert.That(options.Extensions, Is.EqualTo(new[]
            {
                firstExtension,
                secondExtension
            }));
            Assert.That(options.NegotiatedExtensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "x-first", "x-second" }));
            Assert.That(options.NegotiatedExtensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("one"));
            Assert.That(options.NegotiatedExtensions[0].ClaimedOpcodes,
                Is.EqualTo(new[] { WebSocketOpcode.ReservedData3 }));
            Assert.That(options.NegotiatedExtensions[0].Error.Identifier, Is.Empty);
            Assert.That(options.PerMessageDeflate, Is.Not.SameAs(template.PerMessageDeflate));
            Assert.That(options.PerMessageDeflate.ServerMaxWindowBits, Is.EqualTo(12));
            Assert.That(options.Heartbeat, Is.Not.SameAs(template.Heartbeat));
            Assert.That(options.Heartbeat.TokenLength, Is.EqualTo(8));
            Assert.That(options.MaximumFramePayloadLength, Is.EqualTo(64));
            Assert.That(options.MaximumMessagePayloadLength, Is.EqualTo(128));
            Assert.That(options.MaximumDecompressedMessagePayloadLength, Is.EqualTo(256));
            Assert.That(options.MaximumFragmentCount, Is.EqualTo(2));
            Assert.That(options.MaximumBufferedInputBytes, Is.EqualTo(64));
            Assert.That(options.MaximumBufferedOutputBytes, Is.EqualTo(128));
            Assert.That(options.MaximumQueuedEvents, Is.EqualTo(3));
            Assert.That(options.AutomaticFramePayloadLength, Is.EqualTo(64));
            Assert.That(options.CloseTimeoutMilliseconds, Is.EqualTo(900));
        });
    }

    [Test]
    public void ClientTransferForcesClientRoleAndRetainsCollaboratorOwnership()
    {
        IWebSocketClientHandshake handshaker = new WebSocketClientHandshake();
        var maskSource = new DataTestMaskKeySource();
        var template = new WebSocketConnectionOptions();
        template.Role = WebSocketRole.Server;
        template.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        template.HasMaskKeySource = true;
        template.MaskKeySource = maskSource;
        var handshakeOptions = new WebSocketClientHandshakeOptions();
        handshakeOptions.Uri = "wss://example.com/chat";
        handshakeOptions.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        handshakeOptions.PeerEnabledExtendedConnect = true;
        handshaker.CreateExtendedConnectRequest(handshakeOptions);
        var response = new WebSocketHttpResponse();
        response.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        response.StatusCode = 200;
        var handshake = handshaker.ValidateExtendedConnectResponse(response);

        var options = handshaker.CreateClientConnectionOptions(handshake, template);

        Assert.Multiple(() =>
        {
            Assert.That(options.Role, Is.EqualTo(WebSocketRole.Client));
            Assert.That(options.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(options.HasMaskKeySource, Is.True);
            Assert.That(options.MaskKeySource, Is.SameAs(maskSource));
            Assert.That(options.Extensions, Is.Not.SameAs(template.Extensions));
            Assert.That(options.NegotiatedExtensions, Is.Not.SameAs(handshake.Extensions));
        });
    }

    [Test]
    public void TransferRejectsNonAcceptedHandshakesAndUnmatchedExtensions()
    {
        IWebSocketClientHandshake client = new WebSocketClientHandshake();
        IWebSocketServerHandshake server = new WebSocketServerHandshake();
        var incomplete = new WebSocketHandshakeResult();
        var rejected = new WebSocketHandshakeResult();
        rejected.Outcome = WebSocketHandshakeOutcome.Rejected;
        var accepted = new WebSocketHandshakeResult();
        accepted.Outcome = WebSocketHandshakeOutcome.Accepted;
        accepted.Extensions = [Negotiation("x-test", "value")];
        var conflicting = new WebSocketHandshakeResult();
        conflicting.Outcome = WebSocketHandshakeOutcome.Accepted;
        var firstConflict = Negotiation("x-first", "one");
        firstConflict.ClaimsRsv3 = true;
        var secondConflict = Negotiation("x-second", "two");
        secondConflict.ClaimsRsv3 = true;
        conflicting.Extensions = [firstConflict, secondConflict];
        var conflictingTemplate = new WebSocketConnectionOptions();
        conflictingTemplate.Extensions =
            [new DataTestExtension(), new DataTestExtension()];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                client.CreateClientConnectionOptions(
                    incomplete,
                    new WebSocketConnectionOptions()));
            Assert.Throws<WebSocketStateException>(() =>
                server.CreateServerConnectionOptions(
                    rejected,
                    new WebSocketConnectionOptions()));
            Assert.Throws<WebSocketStateException>(() =>
                client.CreateClientConnectionOptions(
                    accepted,
                    new WebSocketConnectionOptions()));
            Assert.Throws<WebSocketStateException>(() =>
                client.CreateClientConnectionOptions(
                    conflicting,
                    conflictingTemplate));
        });
    }

    [Test]
    public void TransferValidatesCopiedConnectionLimits()
    {
        IWebSocketClientHandshake handshaker = new WebSocketClientHandshake();
        var handshake = new WebSocketHandshakeResult();
        handshake.Outcome = WebSocketHandshakeOutcome.Accepted;
        var template = new WebSocketConnectionOptions();
        template.MaximumMessagePayloadLength = 1;

        Assert.Throws<WebSocketLimitException>(() =>
            handshaker.CreateClientConnectionOptions(handshake, template));
    }

    static WebSocketConnectionOptions ServerTemplate(
        IWebSocketExtension first,
        IWebSocketExtension second)
    {
        var template = new WebSocketConnectionOptions();
        template.Role = WebSocketRole.Client;
        template.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        template.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        template.HasSelectedSubprotocol = true;
        template.SelectedSubprotocol = "template";
        template.Extensions = [first, second];
        template.NegotiatedExtensions =
        [
            Negotiation("ignored-first", "ignored"),
            Negotiation("ignored-second", "ignored")
        ];
        template.HasPerMessageDeflate = true;
        template.PerMessageDeflate.OfferServerMaxWindowBits = true;
        template.PerMessageDeflate.HasServerMaxWindowBits = true;
        template.PerMessageDeflate.ServerMaxWindowBits = 12;
        template.MaximumFramePayloadLength = 64;
        template.MaximumMessagePayloadLength = 128;
        template.MaximumDecompressedMessagePayloadLength = 256;
        template.MaximumFragmentCount = 2;
        template.MaximumBufferedInputBytes = 64;
        template.MaximumBufferedOutputBytes = 128;
        template.MaximumQueuedEvents = 3;
        template.AutomaticFramePayloadLength = 64;
        template.AutomaticPong = false;
        template.AutomaticCloseResponse = false;
        template.CloseTimeoutMilliseconds = 900;
        template.Heartbeat.Enabled = true;
        template.Heartbeat.IdleIntervalMilliseconds = 300;
        template.Heartbeat.PongTimeoutMilliseconds = 100;
        template.Heartbeat.TokenLength = 8;
        return template;
    }

    static WebSocketHandshakeResult AcceptedHandshake()
    {
        var first = Negotiation("x-first", "one");
        first.ClaimsRsv1 = true;
        first.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
        var second = Negotiation("x-second", "two");
        second.ClaimsRsv2 = true;
        var handshake = new WebSocketHandshakeResult();
        handshake.Outcome = WebSocketHandshakeOutcome.Accepted;
        handshake.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        handshake.HasSelectedSubprotocol = true;
        handshake.SelectedSubprotocol = "chat";
        handshake.Extensions = [first, second];
        return handshake;
    }

    static WebSocketExtensionNegotiation Negotiation(string name, string value)
    {
        var parameter = new WebSocketExtensionParameter();
        parameter.Name = "mode";
        parameter.HasValue = true;
        parameter.Value = value;
        var offer = new WebSocketExtensionOffer();
        offer.Name = name;
        offer.Parameters = [parameter];
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Name = name;
        negotiation.Status = WebSocketExtensionStatus.Negotiated;
        negotiation.HasAcceptedConfiguration = true;
        negotiation.AcceptedConfiguration = offer;
        return negotiation;
    }
}
