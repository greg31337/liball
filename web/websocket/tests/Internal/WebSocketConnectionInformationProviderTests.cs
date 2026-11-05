using websocket;

namespace websocket.Tests;

public class WebSocketConnectionInformationProviderTests
{
    [Test]
    public void SnapshotCopiesNegotiationPendingCountsCountersAndHeartbeat()
    {
        WebSocketConnectionOptions options = Options(true);
        WebSocketExtensionNegotiation negotiation = Negotiation();
        IWebSocketExtension handler = new DataTestExtension();
        options.Extensions = [handler];
        options.NegotiatedExtensions = [negotiation];
        options.HasSelectedSubprotocol = true;
        options.SelectedSubprotocol = "chat";
        WebSocketHandshakeResult handshake = Handshake(negotiation);
        SnapshotTestContext context = Context(options, handshake);
        context.State.AdvanceElapsedMilliseconds(100);
        context.Heartbeat.AdvanceHeartbeatTime(100);
        context.Scheduler.TryQueueDataFrame([9, 8, 7]);
        context.Events.TryEnqueueEvent(new WebSocketEvent
        {
            Type = WebSocketEventType.MessageData
        });
        context.Provider.RecordInboundProgress(2, 1);
        context.Provider.RecordOutboundProgress(3, 2);

        options.SelectedSubprotocol = "changed-input";
        options.Heartbeat.TokenLength = 9;
        options.Extensions = [];
        negotiation.AcceptedConfiguration.Parameters[0].Value =
            "changed-input";
        handshake.Request.Fields[0].Value = "changed-input";
        handshake.Response.Body[0] = 99;
        WebSocketConnectionInfo first =
            context.Provider.GetConnectionInformation();

        first.Options.SelectedSubprotocol = "changed-snapshot";
        first.Options.Heartbeat.TokenLength = 10;
        first.Options.Extensions = [];
        first.Options.NegotiatedExtensions[0]
            .AcceptedConfiguration.Parameters[0].Value =
                "changed-snapshot";
        first.Extensions[0].AcceptedConfiguration.Parameters[0].Value =
            "changed-snapshot";
        first.Handshake.Request.Fields[0].Value = "changed-snapshot";
        first.Handshake.Response.Body[0] = 88;
        WebSocketConnectionInfo second =
            context.Provider.GetConnectionInformation();

        Assert.Multiple(() =>
        {
            Assert.That(second.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(second.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(second.DeliveryMode,
                Is.EqualTo(WebSocketMessageDeliveryMode.Streaming));
            Assert.That(second.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(second.HasSelectedSubprotocol, Is.True);
            Assert.That(second.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(second.PendingOutputBytes, Is.EqualTo(9));
            Assert.That(second.PendingEventCount, Is.EqualTo(1));
            Assert.That(second.ReceivedFrameCount, Is.EqualTo(2));
            Assert.That(second.SentFrameCount, Is.EqualTo(3));
            Assert.That(second.ReceivedMessageCount, Is.EqualTo(1));
            Assert.That(second.SentMessageCount, Is.EqualTo(2));
            Assert.That(second.ElapsedMilliseconds, Is.EqualTo(100));
            Assert.That(second.HeartbeatPingOutstanding, Is.True);
            Assert.That(second.HasHeartbeatPingSentMilliseconds, Is.True);
            Assert.That(second.HeartbeatPingSentMilliseconds,
                Is.EqualTo(100));
            Assert.That(second.HasLastPingRoundTripMilliseconds, Is.False);
            Assert.That(second.Options.Heartbeat.TokenLength, Is.EqualTo(4));
            Assert.That(second.Options.Extensions, Has.Length.EqualTo(1));
            Assert.That(second.Options.Extensions[0], Is.SameAs(handler));
            Assert.That(second.Options.NegotiatedExtensions[0]
                .AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(second.Extensions[0]
                .AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(second.Handshake.Request.Fields[0].Value,
                Is.EqualTo("example.test"));
            Assert.That(second.Handshake.Response.Body,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(second.Options, Is.Not.SameAs(first.Options));
            Assert.That(second.Extensions, Is.Not.SameAs(first.Extensions));
            Assert.That(second.Extensions[0],
                Is.Not.SameAs(first.Extensions[0]));
            Assert.That(second.Handshake, Is.Not.SameAs(first.Handshake));
            Assert.That(second.Handshake.Request.Fields,
                Is.Not.SameAs(first.Handshake.Request.Fields));
            Assert.That(second.Handshake.Response.Body,
                Is.Not.SameAs(first.Handshake.Response.Body));
        });
    }

    [Test]
    public void CleanCloseSnapshotCopiesLocalAndRemoteCloseFacts()
    {
        WebSocketConnectionOptions options = Options(false);
        SnapshotTestContext context = Context(
            options,
            Handshake(null));
        WebSocketCloseInfo requested = new WebSocketCloseInfo
        {
            HasCode = true,
            Code = 1000,
            HasReason = true,
            Reason = "local"
        };
        context.LocalClose.StartLocalClose(requested);
        Drain(context.Scheduler);
        context.LocalClose.SynchronizeLocalCloseSent();
        context.RemoteClose.ConsumeInboundCloseChunk(CloseChunk(
            [0x03, 0xE9, 0x70, 0x65, 0x65, 0x72]));
        context.State.EndTransport();

        WebSocketConnectionInfo first =
            context.Provider.GetConnectionInformation();
        first.LocalClose.Reason = "changed";
        first.RemoteClose.Reason = "changed";
        WebSocketConnectionInfo second =
            context.Provider.GetConnectionInformation();

        Assert.Multiple(() =>
        {
            Assert.That(second.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(second.CleanClose, Is.True);
            Assert.That(second.TransportEnded, Is.True);
            Assert.That(second.ShouldEndTransport, Is.True);
            Assert.That(second.HasError, Is.False);
            Assert.That(second.PendingOutputBytes, Is.Zero);
            Assert.That(second.PendingEventCount, Is.EqualTo(1));
            Assert.That(second.HasLocalClose, Is.True);
            Assert.That(second.LocalClose.Code, Is.EqualTo(1000));
            Assert.That(second.LocalClose.Reason, Is.EqualTo("local"));
            Assert.That(second.LocalClose.Sent, Is.True);
            Assert.That(second.LocalClose.InitiatedLocally, Is.True);
            Assert.That(second.LocalClose.Clean, Is.True);
            Assert.That(second.HasRemoteClose, Is.True);
            Assert.That(second.RemoteClose.Code, Is.EqualTo(1001));
            Assert.That(second.RemoteClose.Reason, Is.EqualTo("peer"));
            Assert.That(second.RemoteClose.Received, Is.True);
            Assert.That(second.RemoteClose.InitiatedLocally, Is.False);
            Assert.That(second.RemoteClose.Clean, Is.True);
            Assert.That(second.LocalClose,
                Is.Not.SameAs(first.LocalClose));
            Assert.That(second.RemoteClose,
                Is.Not.SameAs(first.RemoteClose));
        });
    }

    [Test]
    public void TerminalErrorIsCopiedAndCounterResetIsIndependent()
    {
        WebSocketConnectionOptions options = Options(false);
        SnapshotTestContext context = Context(
            options,
            Handshake(null));
        context.Provider.RecordInboundProgress(4, 2);
        context.Provider.RecordOutboundProgress(5, 3);
        Assert.Throws<WebSocketStateException>(() =>
            context.Provider.RecordInboundProgress(1, -1));
        Assert.Throws<WebSocketStateException>(() =>
            context.Provider.RecordOutboundProgress(-1, 1));
        context.State.EndTransport();

        WebSocketConnectionInfo first =
            context.Provider.GetConnectionInformation();
        first.Error.Identifier = "changed";
        context.Provider.ResetConnectionInformation();
        WebSocketConnectionInfo second =
            context.Provider.GetConnectionInformation();

        Assert.Multiple(() =>
        {
            Assert.That(first.ReceivedFrameCount, Is.EqualTo(4));
            Assert.That(first.ReceivedMessageCount, Is.EqualTo(2));
            Assert.That(first.SentFrameCount, Is.EqualTo(5));
            Assert.That(first.SentMessageCount, Is.EqualTo(3));
            Assert.That(second.ReceivedFrameCount, Is.Zero);
            Assert.That(second.ReceivedMessageCount, Is.Zero);
            Assert.That(second.SentFrameCount, Is.Zero);
            Assert.That(second.SentMessageCount, Is.Zero);
            Assert.That(second.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(second.HasError, Is.True);
            Assert.That(second.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Transport));
            Assert.That(second.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(second.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(second.Error, Is.Not.SameAs(first.Error));
        });
    }

    static SnapshotTestContext Context(
        WebSocketConnectionOptions options,
        WebSocketHandshakeResult handshake)
    {
        WebSocketSendScheduler scheduler =
            new WebSocketSendScheduler(options.MaximumBufferedOutputBytes);
        WebSocketEventQueue events =
            new WebSocketEventQueue(options.MaximumQueuedEvents);
        WebSocketPingPongHandler pingPong = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        WebSocketHeartbeatManager heartbeat =
            new WebSocketHeartbeatManager(options, pingPong);
        WebSocketConnectionStateMachine state =
            new WebSocketConnectionStateMachine(
                options.Role,
                options.Transport,
                options.CloseTimeoutMilliseconds);
        state.OpenConnection();
        WebSocketLocalCloseCoordinator localClose =
            new WebSocketLocalCloseCoordinator(
                options,
                scheduler,
                state,
                heartbeat);
        WebSocketRemoteCloseHandler remoteClose =
            new WebSocketRemoteCloseHandler(
                options,
                scheduler,
                events,
                state,
                localClose,
                heartbeat);
        WebSocketConnectionInformationProvider provider =
            new WebSocketConnectionInformationProvider(
                options,
                handshake,
                state,
                scheduler,
                events,
                heartbeat,
                localClose,
                remoteClose);
        return new SnapshotTestContext
        {
            Scheduler = scheduler,
            Events = events,
            Heartbeat = heartbeat,
            State = state,
            LocalClose = localClose,
            RemoteClose = remoteClose,
            Provider = provider
        };
    }

    static WebSocketConnectionOptions Options(bool heartbeatEnabled)
    {
        WebSocketConnectionOptions options =
            new WebSocketConnectionOptions
            {
                Role = WebSocketRole.Server,
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
                MaximumFramePayloadLength = 128,
                MaximumMessagePayloadLength = 256,
                MaximumDecompressedMessagePayloadLength = 256,
                MaximumFragmentCount = 8,
                MaximumBufferedInputBytes = 256,
                MaximumBufferedOutputBytes = 256,
                MaximumQueuedEvents = 8,
                AutomaticFramePayloadLength = 32,
                CloseTimeoutMilliseconds = 1000
            };
        options.Heartbeat.Enabled = heartbeatEnabled;
        options.Heartbeat.IdleIntervalMilliseconds = 100;
        options.Heartbeat.PongTimeoutMilliseconds = 50;
        options.Heartbeat.TokenLength = 4;
        return options;
    }

    static WebSocketExtensionNegotiation Negotiation()
    {
        return new WebSocketExtensionNegotiation
        {
            Name = "x-snapshot",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-snapshot",
                Parameters =
                [
                    new WebSocketExtensionParameter
                    {
                        Name = "mode",
                        HasValue = true,
                        Value = "fast"
                    }
                ]
            },
            ClaimsRsv1 = true,
            ClaimedOpcodes = [WebSocketOpcode.ReservedData3]
        };
    }

    static WebSocketHandshakeResult Handshake(
        WebSocketExtensionNegotiation? negotiation)
    {
        WebSocketExtensionNegotiation[] extensions = [];
        if (negotiation != null)
        {
            extensions = [negotiation];
        }

        return new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            HasRequest = true,
            Request = new WebSocketHttpRequest
            {
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                Method = "GET",
                HasRequestTarget = true,
                RequestTarget = "/chat",
                HasHttpVersion = true,
                HttpVersion = "HTTP/1.1",
                Fields =
                [
                    new WebSocketHeaderField
                    {
                        Name = "Host",
                        Value = "example.test"
                    }
                ]
            },
            HasResponse = true,
            Response = new WebSocketHttpResponse
            {
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                StatusCode = 101,
                HasReasonPhrase = true,
                ReasonPhrase = "Switching Protocols",
                HasBody = true,
                Body = [1, 2, 3]
            },
            HasSelectedSubprotocol = negotiation != null,
            SelectedSubprotocol = negotiation == null ? "" : "chat",
            Extensions = extensions,
            HasClientKey = true,
            ClientKey = "client-key"
        };
    }

    static WebSocketInboundFrameChunk CloseChunk(byte[] payload)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = new WebSocketFrameHeader
            {
                Final = true,
                Opcode = WebSocketOpcode.Close,
                PayloadLength = payload.Length
            },
            FirstChunk = true,
            EndOfFrame = true,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] Drain(WebSocketSendScheduler scheduler)
    {
        byte[] output = new byte[scheduler.GetPendingByteCount()];
        int written = scheduler.WritePendingBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }
}

internal class SnapshotTestContext
{
    internal WebSocketSendScheduler Scheduler =
        new WebSocketSendScheduler(1);
    internal WebSocketEventQueue Events = new WebSocketEventQueue(1);
    internal WebSocketHeartbeatManager Heartbeat = null!;
    internal WebSocketConnectionStateMachine State = null!;
    internal WebSocketLocalCloseCoordinator LocalClose = null!;
    internal WebSocketRemoteCloseHandler RemoteClose = null!;
    internal WebSocketConnectionInformationProvider Provider = null!;
}
