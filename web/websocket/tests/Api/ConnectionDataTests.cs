using websocket;

namespace websocket.Tests;

public class ConnectionDataTests
{
    [Test]
    public void HeartbeatDefaultsAreBoundedAndDisabled()
    {
        var heartbeat = new WebSocketHeartbeatOptions();

        Assert.Multiple(() =>
        {
            Assert.That(heartbeat.Enabled, Is.False);
            Assert.That(heartbeat.IdleIntervalMilliseconds, Is.EqualTo(WebSocketDefaults.HeartbeatIdleIntervalMilliseconds));
            Assert.That(heartbeat.PongTimeoutMilliseconds, Is.EqualTo(WebSocketDefaults.HeartbeatPongTimeoutMilliseconds));
            Assert.That(heartbeat.TokenLength, Is.EqualTo(WebSocketDefaults.HeartbeatTokenLength));
            Assert.That(heartbeat.TokenLength, Is.LessThanOrEqualTo(WebSocketProtocolConstants.ControlPayloadLengthMaximum));
        });

        heartbeat.Enabled = true;
        heartbeat.IdleIntervalMilliseconds = 250;
        heartbeat.PongTimeoutMilliseconds = 75;
        heartbeat.TokenLength = 12;

        Assert.Multiple(() =>
        {
            Assert.That(heartbeat.Enabled, Is.True);
            Assert.That(heartbeat.IdleIntervalMilliseconds, Is.EqualTo(250));
            Assert.That(heartbeat.PongTimeoutMilliseconds, Is.EqualTo(75));
            Assert.That(heartbeat.TokenLength, Is.EqualTo(12));
        });
    }

    [Test]
    public void ConnectionOptionsUseFiniteDefaultsAndSafeAutomaticControlHandling()
    {
        var first = new WebSocketConnectionOptions();
        var second = new WebSocketConnectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Role, Is.EqualTo(WebSocketRole.Client));
            Assert.That(first.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(first.DeliveryMode, Is.EqualTo(WebSocketMessageDeliveryMode.Reassembled));
            Assert.That(first.HasSelectedSubprotocol, Is.False);
            Assert.That(first.SelectedSubprotocol, Is.Empty);
            Assert.That(first.Extensions, Is.Empty);
            Assert.That(first.NegotiatedExtensions, Is.Empty);
            Assert.That(first.HasPerMessageDeflate, Is.False);
            Assert.That(first.MaximumFramePayloadLength, Is.EqualTo(WebSocketDefaults.MaximumFramePayloadLength));
            Assert.That(first.MaximumMessagePayloadLength, Is.EqualTo(WebSocketDefaults.MaximumMessagePayloadLength));
            Assert.That(first.MaximumDecompressedMessagePayloadLength, Is.EqualTo(WebSocketDefaults.MaximumDecompressedMessagePayloadLength));
            Assert.That(first.MaximumFragmentCount, Is.EqualTo(WebSocketDefaults.MaximumFragmentCount));
            Assert.That(first.MaximumBufferedInputBytes, Is.EqualTo(WebSocketDefaults.MaximumBufferedInputBytes));
            Assert.That(first.MaximumBufferedOutputBytes, Is.EqualTo(WebSocketDefaults.MaximumBufferedOutputBytes));
            Assert.That(first.MaximumQueuedEvents, Is.EqualTo(WebSocketDefaults.MaximumQueuedEvents));
            Assert.That(first.AutomaticFramePayloadLength, Is.EqualTo(WebSocketDefaults.AutomaticFramePayloadLength));
            Assert.That(first.AutomaticPong, Is.True);
            Assert.That(first.AutomaticCloseResponse, Is.True);
            Assert.That(first.CloseTimeoutMilliseconds, Is.EqualTo(WebSocketDefaults.CloseTimeoutMilliseconds));
            Assert.That(first.HasMaskKeySource, Is.False);
            Assert.That(first.MaskKeySource, Is.Null);
            Assert.That(first.PerMessageDeflate, Is.Not.SameAs(second.PerMessageDeflate));
            Assert.That(first.Heartbeat, Is.Not.SameAs(second.Heartbeat));
        });
    }

    [Test]
    public void ConnectionOptionsCarryNegotiationAndCollaboratorOwnership()
    {
        var extension = new DataTestExtension();
        var extensions = new IWebSocketExtension[] { extension };
        var maskSource = new DataTestMaskKeySource();
        var options = new WebSocketConnectionOptions();
        options.Role = WebSocketRole.Server;
        options.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        options.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        options.HasSelectedSubprotocol = true;
        options.SelectedSubprotocol = "chat";
        options.Extensions = extensions;
        options.NegotiatedExtensions =
        [
            new WebSocketExtensionNegotiation
            {
                Name = "x-test",
                Status = WebSocketExtensionStatus.Negotiated,
                HasAcceptedConfiguration = true,
                AcceptedConfiguration = new WebSocketExtensionOffer { Name = "x-test" }
            }
        ];
        options.HasPerMessageDeflate = true;
        options.PerMessageDeflate.OfferClientNoContextTakeover = true;
        options.MaximumFramePayloadLength = 1024;
        options.MaximumMessagePayloadLength = 2048;
        options.MaximumDecompressedMessagePayloadLength = 4096;
        options.MaximumFragmentCount = 16;
        options.MaximumBufferedInputBytes = 8192;
        options.MaximumBufferedOutputBytes = 16384;
        options.MaximumQueuedEvents = 32;
        options.AutomaticFramePayloadLength = 512;
        options.AutomaticPong = false;
        options.AutomaticCloseResponse = false;
        options.CloseTimeoutMilliseconds = 750;
        options.Heartbeat.Enabled = true;
        options.HasMaskKeySource = true;
        options.MaskKeySource = maskSource;

        Assert.Multiple(() =>
        {
            Assert.That(options.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(options.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(options.DeliveryMode, Is.EqualTo(WebSocketMessageDeliveryMode.Streaming));
            Assert.That(options.HasSelectedSubprotocol, Is.True);
            Assert.That(options.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(options.Extensions, Is.SameAs(extensions));
            Assert.That(options.Extensions[0], Is.SameAs(extension));
            Assert.That(options.NegotiatedExtensions[0].Name, Is.EqualTo("x-test"));
            Assert.That(options.HasPerMessageDeflate, Is.True);
            Assert.That(options.PerMessageDeflate.OfferClientNoContextTakeover, Is.True);
            Assert.That(options.MaximumFramePayloadLength, Is.EqualTo(1024));
            Assert.That(options.MaximumMessagePayloadLength, Is.EqualTo(2048));
            Assert.That(options.MaximumDecompressedMessagePayloadLength, Is.EqualTo(4096));
            Assert.That(options.MaximumFragmentCount, Is.EqualTo(16));
            Assert.That(options.MaximumBufferedInputBytes, Is.EqualTo(8192));
            Assert.That(options.MaximumBufferedOutputBytes, Is.EqualTo(16384));
            Assert.That(options.MaximumQueuedEvents, Is.EqualTo(32));
            Assert.That(options.AutomaticFramePayloadLength, Is.EqualTo(512));
            Assert.That(options.AutomaticPong, Is.False);
            Assert.That(options.AutomaticCloseResponse, Is.False);
            Assert.That(options.CloseTimeoutMilliseconds, Is.EqualTo(750));
            Assert.That(options.Heartbeat.Enabled, Is.True);
            Assert.That(options.HasMaskKeySource, Is.True);
            Assert.That(options.MaskKeySource, Is.SameAs(maskSource));
        });
    }

    [Test]
    public void ConnectionInfoDefaultsHaveNoOptionalOrTerminalFacts()
    {
        var first = new WebSocketConnectionInfo();
        var second = new WebSocketConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(first.Role, Is.EqualTo(WebSocketRole.Client));
            Assert.That(first.State, Is.EqualTo(WebSocketConnectionState.Connecting));
            Assert.That(first.DeliveryMode, Is.EqualTo(WebSocketMessageDeliveryMode.Reassembled));
            Assert.That(first.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(first.HasSelectedSubprotocol, Is.False);
            Assert.That(first.SelectedSubprotocol, Is.Empty);
            Assert.That(first.Extensions, Is.Empty);
            Assert.That(first.PendingOutputBytes, Is.Zero);
            Assert.That(first.PendingEventCount, Is.Zero);
            Assert.That(first.ReceivedFrameCount, Is.Zero);
            Assert.That(first.SentFrameCount, Is.Zero);
            Assert.That(first.ReceivedMessageCount, Is.Zero);
            Assert.That(first.SentMessageCount, Is.Zero);
            Assert.That(first.ElapsedMilliseconds, Is.Zero);
            Assert.That(first.HeartbeatPingOutstanding, Is.False);
            Assert.That(first.HasHeartbeatPingSentMilliseconds, Is.False);
            Assert.That(first.HeartbeatPingSentMilliseconds, Is.Zero);
            Assert.That(first.HasLastPingRoundTripMilliseconds, Is.False);
            Assert.That(first.LastPingRoundTripMilliseconds, Is.Zero);
            Assert.That(first.HasLocalClose, Is.False);
            Assert.That(first.HasRemoteClose, Is.False);
            Assert.That(first.CleanClose, Is.False);
            Assert.That(first.TransportEnded, Is.False);
            Assert.That(first.TransportReset, Is.False);
            Assert.That(first.ShouldEndTransport, Is.False);
            Assert.That(first.HasError, Is.False);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
            Assert.That(first.Handshake, Is.Not.SameAs(second.Handshake));
            Assert.That(first.LocalClose, Is.Not.SameAs(second.LocalClose));
            Assert.That(first.RemoteClose, Is.Not.SameAs(second.RemoteClose));
            Assert.That(first.Error, Is.Not.SameAs(second.Error));
        });
    }

    [Test]
    public void ConnectionInfoCarriesCountersHeartbeatCloseAndTerminalFacts()
    {
        var info = new WebSocketConnectionInfo();
        info.Role = WebSocketRole.Server;
        info.State = WebSocketConnectionState.Closed;
        info.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        info.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        info.HasSelectedSubprotocol = true;
        info.SelectedSubprotocol = "chat";
        info.Extensions =
        [
            new WebSocketExtensionNegotiation
            {
                Name = "x-test"
            }
        ];
        info.Handshake.Outcome = WebSocketHandshakeOutcome.Accepted;
        info.Options.Role = WebSocketRole.Server;
        info.PendingOutputBytes = 12;
        info.PendingEventCount = 2;
        info.ReceivedFrameCount = 100;
        info.SentFrameCount = 90;
        info.ReceivedMessageCount = 40;
        info.SentMessageCount = 30;
        info.ElapsedMilliseconds = 5000;
        info.HeartbeatPingOutstanding = true;
        info.HasHeartbeatPingSentMilliseconds = true;
        info.HeartbeatPingSentMilliseconds = 4900;
        info.HasLastPingRoundTripMilliseconds = true;
        info.LastPingRoundTripMilliseconds = 25;
        info.HasLocalClose = true;
        info.LocalClose.HasCode = true;
        info.LocalClose.Code = 1000;
        info.HasRemoteClose = true;
        info.RemoteClose.HasCode = true;
        info.RemoteClose.Code = 1000;
        info.CleanClose = true;
        info.TransportEnded = true;
        info.TransportReset = true;
        info.ShouldEndTransport = true;
        info.HasError = true;
        info.Error.Kind = WebSocketErrorKind.Transport;

        Assert.Multiple(() =>
        {
            Assert.That(info.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(info.State, Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(info.DeliveryMode, Is.EqualTo(WebSocketMessageDeliveryMode.Streaming));
            Assert.That(info.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(info.HasSelectedSubprotocol, Is.True);
            Assert.That(info.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(info.Extensions[0].Name, Is.EqualTo("x-test"));
            Assert.That(info.Handshake.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(info.Options.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(info.PendingOutputBytes, Is.EqualTo(12));
            Assert.That(info.PendingEventCount, Is.EqualTo(2));
            Assert.That(info.ReceivedFrameCount, Is.EqualTo(100));
            Assert.That(info.SentFrameCount, Is.EqualTo(90));
            Assert.That(info.ReceivedMessageCount, Is.EqualTo(40));
            Assert.That(info.SentMessageCount, Is.EqualTo(30));
            Assert.That(info.ElapsedMilliseconds, Is.EqualTo(5000));
            Assert.That(info.HeartbeatPingOutstanding, Is.True);
            Assert.That(info.HasHeartbeatPingSentMilliseconds, Is.True);
            Assert.That(info.HeartbeatPingSentMilliseconds, Is.EqualTo(4900));
            Assert.That(info.HasLastPingRoundTripMilliseconds, Is.True);
            Assert.That(info.LastPingRoundTripMilliseconds, Is.EqualTo(25));
            Assert.That(info.HasLocalClose, Is.True);
            Assert.That(info.LocalClose.Code, Is.EqualTo(1000));
            Assert.That(info.HasRemoteClose, Is.True);
            Assert.That(info.RemoteClose.Code, Is.EqualTo(1000));
            Assert.That(info.CleanClose, Is.True);
            Assert.That(info.TransportEnded, Is.True);
            Assert.That(info.TransportReset, Is.True);
            Assert.That(info.ShouldEndTransport, Is.True);
            Assert.That(info.HasError, Is.True);
            Assert.That(info.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
        });
    }
}

internal class DataTestExtension : IWebSocketExtension
{
    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer();
    }

    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(WebSocketExtensionOffer offer)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(WebSocketExtensionOffer response)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketTransformResult TransformOutboundChunk(WebSocketExtensionContext context, ReadOnlySpan<byte> source)
    {
        var result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = source.Length;
        result.ProducedCount = source.Length;
        result.Output = source.ToArray();
        return result;
    }

    public WebSocketTransformResult TransformInboundChunk(WebSocketExtensionContext context, ReadOnlySpan<byte> source)
    {
        var result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = source.Length;
        result.ProducedCount = source.Length;
        result.Output = source.ToArray();
        return result;
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
    }

    public void ResetExtensionState()
    {
    }
}
