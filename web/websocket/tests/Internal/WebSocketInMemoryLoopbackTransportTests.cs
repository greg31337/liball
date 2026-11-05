using websocket;

namespace websocket.Tests;

public class WebSocketInMemoryLoopbackTransportTests
{
    [Test]
    public void BoundedChunksCarryMaskedAndUnmaskedMessagesBothWays()
    {
        LoopbackTestPair pair = Pair(
            WebSocketMessageDeliveryMode.Reassembled,
            WebSocketMessageDeliveryMode.Reassembled,
            8,
            8);
        WebSocketInMemoryLoopbackTransport transport =
            new WebSocketInMemoryLoopbackTransport(
                pair.Client,
                pair.Server,
                3,
                2);
        pair.Client.SendWholeMessage(Request(
            WebSocketMessageType.Text,
            [0x68, 0x65, 0x6C, 0x6C, 0x6F]));
        pair.Server.SendWholeMessage(Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4]));

        WebSocketLoopbackPumpResult pumped =
            transport.PumpLoopbackTraffic(100);
        WebSocketEvent serverMessage = RequiredEvent(
            pair.Server.ReadNextEvent());
        WebSocketEvent clientMessage = RequiredEvent(
            pair.Client.ReadNextEvent());
        WebSocketConnectionInfo clientInformation =
            pair.Client.GetConnectionInfo();
        WebSocketConnectionInfo serverInformation =
            pair.Server.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(pumped.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(pumped.Quiescent, Is.True);
            Assert.That(pumped.Blocked, Is.False);
            Assert.That(pumped.StepLimitReached, Is.False);
            Assert.That(pumped.ClientToServerByteCount,
                Is.GreaterThan(5));
            Assert.That(pumped.ServerToClientByteCount,
                Is.GreaterThan(4));
            Assert.That(pumped.ServerReceivedFrameCount, Is.EqualTo(3));
            Assert.That(pumped.ClientReceivedFrameCount, Is.EqualTo(2));
            Assert.That(
                transport.GetMaximumObservedClientToServerByteCount(),
                Is.LessThanOrEqualTo(3));
            Assert.That(
                transport.GetMaximumObservedServerToClientByteCount(),
                Is.LessThanOrEqualTo(3));
            Assert.That(serverMessage.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(serverMessage.Payload,
                Is.EqualTo(new byte[]
                {
                    0x68, 0x65, 0x6C, 0x6C, 0x6F
                }));
            Assert.That(clientMessage.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(clientMessage.Payload,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(clientInformation.ReceivedMessageCount,
                Is.EqualTo(1));
            Assert.That(serverInformation.ReceivedMessageCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void EventBackpressureResumesWithoutLosingTransportBytes()
    {
        LoopbackTestPair pair = Pair(
            WebSocketMessageDeliveryMode.Reassembled,
            WebSocketMessageDeliveryMode.Reassembled,
            8,
            1);
        WebSocketInMemoryLoopbackTransport transport =
            new WebSocketInMemoryLoopbackTransport(
                pair.Client,
                pair.Server,
                32,
                32);
        pair.Client.SendWholeMessage(Request(
            WebSocketMessageType.Binary,
            [1]));
        pair.Client.SendWholeMessage(Request(
            WebSocketMessageType.Binary,
            [2]));

        WebSocketLoopbackPumpResult blocked =
            transport.PumpLoopbackTraffic(20);
        WebSocketEvent first = RequiredEvent(
            pair.Server.ReadNextEvent());
        WebSocketLoopbackPumpResult resumed =
            transport.PumpLoopbackTraffic(20);
        WebSocketEvent second = RequiredEvent(
            pair.Server.ReadNextEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.Blocked, Is.True);
            Assert.That(first.Payload,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.Quiescent, Is.True);
            Assert.That(second.Payload,
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(
                transport.GetClientToServerQueuedByteCount(),
                Is.Zero);
            Assert.That(pair.Server.GetConnectionInfo()
                .ReceivedMessageCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void SharedTimeAndDeferredEofCompleteAHeartbeatAndCleanClose()
    {
        LoopbackTestPair pair = Pair(
            WebSocketMessageDeliveryMode.Streaming,
            WebSocketMessageDeliveryMode.Streaming,
            8,
            8);
        EnableHeartbeat(pair.ClientOptions);
        EnableHeartbeat(pair.ServerOptions);
        pair = CreatePair(pair.ClientOptions, pair.ServerOptions);
        WebSocketInMemoryLoopbackTransport transport =
            new WebSocketInMemoryLoopbackTransport(
                pair.Client,
                pair.Server,
                5,
                3);

        WebSocketParseStatus advanced =
            transport.AdvanceLoopbackTime(100);
        WebSocketLoopbackPumpResult heartbeat =
            transport.PumpLoopbackTraffic(100);
        DrainEvents(pair.Client);
        DrainEvents(pair.Server);
        pair.Client.StartConnectionClose(new WebSocketCloseInfo
        {
            HasCode = true,
            Code = 1000,
            HasReason = true,
            Reason = "done"
        });
        WebSocketLoopbackPumpResult closing =
            transport.PumpLoopbackTraffic(100);
        transport.EndClientLoopbackOutput();
        transport.EndServerLoopbackOutput();
        WebSocketLoopbackPumpResult ended =
            transport.PumpLoopbackTraffic(20);
        WebSocketConnectionInfo clientInformation =
            pair.Client.GetConnectionInfo();
        WebSocketConnectionInfo serverInformation =
            pair.Server.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(advanced,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(heartbeat.Quiescent, Is.True);
            Assert.That(pair.Client.GetConnectionInfo()
                .HasLastPingRoundTripMilliseconds,
                Is.True);
            Assert.That(pair.Server.GetConnectionInfo()
                .HasLastPingRoundTripMilliseconds,
                Is.True);
            Assert.That(closing.Quiescent, Is.True);
            Assert.That(ended.Quiescent, Is.True);
            Assert.That(transport.HasDeliveredClientOutputEnd(), Is.True);
            Assert.That(transport.HasDeliveredServerOutputEnd(), Is.True);
            Assert.That(clientInformation.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(serverInformation.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(clientInformation.CleanClose, Is.True);
            Assert.That(serverInformation.CleanClose, Is.True);
            Assert.That(clientInformation.ElapsedMilliseconds,
                Is.EqualTo(100));
            Assert.That(serverInformation.ElapsedMilliseconds,
                Is.EqualTo(100));
        });
    }

    [Test]
    public void ResetClearsTransportAndConnectionOutputAndFailsBothEnds()
    {
        LoopbackTestPair pair = Pair(
            WebSocketMessageDeliveryMode.Reassembled,
            WebSocketMessageDeliveryMode.Reassembled,
            8,
            8);
        WebSocketInMemoryLoopbackTransport transport =
            new WebSocketInMemoryLoopbackTransport(
                pair.Client,
                pair.Server,
                4,
                2);
        pair.Client.SendWholeMessage(Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4]));
        transport.PumpLoopbackTraffic(1);

        WebSocketParseStatus reset = transport.ResetLoopbackTransport();
        WebSocketConnectionInfo clientInformation =
            pair.Client.GetConnectionInfo();
        WebSocketConnectionInfo serverInformation =
            pair.Server.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(reset,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(
                transport.GetClientToServerQueuedByteCount(),
                Is.Zero);
            Assert.That(
                transport.GetServerToClientQueuedByteCount(),
                Is.Zero);
            Assert.That(clientInformation.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(serverInformation.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(clientInformation.TransportReset, Is.True);
            Assert.That(serverInformation.TransportReset, Is.True);
            Assert.That(clientInformation.PendingOutputBytes, Is.Zero);
            Assert.That(serverInformation.PendingOutputBytes, Is.Zero);
            Assert.That(clientInformation.Error.Identifier,
                Is.EqualTo("connection-transport-reset"));
            Assert.That(serverInformation.Error.Identifier,
                Is.EqualTo("connection-transport-reset"));
        });
    }

    static LoopbackTestPair Pair(
        WebSocketMessageDeliveryMode clientMode,
        WebSocketMessageDeliveryMode serverMode,
        int clientEventCapacity,
        int serverEventCapacity)
    {
        WebSocketConnectionOptions clientOptions = Options(
            WebSocketRole.Client,
            clientMode,
            clientEventCapacity);
        WebSocketConnectionOptions serverOptions = Options(
            WebSocketRole.Server,
            serverMode,
            serverEventCapacity);
        return CreatePair(clientOptions, serverOptions);
    }

    static LoopbackTestPair CreatePair(
        WebSocketConnectionOptions clientOptions,
        WebSocketConnectionOptions serverOptions)
    {
        WebSocketHandshakeResult handshake = Handshake();
        return new LoopbackTestPair
        {
            ClientOptions = clientOptions,
            ServerOptions = serverOptions,
            Client = new WebSocketConnection(
                clientOptions,
                handshake),
            Server = new WebSocketConnection(
                serverOptions,
                handshake)
        };
    }

    static WebSocketConnectionOptions Options(
        WebSocketRole role,
        WebSocketMessageDeliveryMode deliveryMode,
        int eventCapacity)
    {
        WebSocketConnectionOptions options =
            new WebSocketConnectionOptions
            {
                Role = role,
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                DeliveryMode = deliveryMode,
                MaximumFramePayloadLength = 8,
                MaximumMessagePayloadLength = 32,
                MaximumDecompressedMessagePayloadLength = 32,
                MaximumFragmentCount = 16,
                MaximumBufferedInputBytes = 64,
                MaximumBufferedOutputBytes = 64,
                MaximumQueuedEvents = eventCapacity,
                AutomaticFramePayloadLength = 2,
                CloseTimeoutMilliseconds = 1000
            };
        if (role == WebSocketRole.Client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = new LoopbackMaskKeySource();
        }

        return options;
    }

    static WebSocketHandshakeResult Handshake()
    {
        return new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            HasRequest = true,
            Request = new WebSocketHttpRequest
            {
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                Method = "GET"
            },
            HasResponse = true,
            Response = new WebSocketHttpResponse
            {
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                StatusCode = 101
            }
        };
    }

    static WebSocketSendRequest Request(
        WebSocketMessageType messageType,
        byte[] payload)
    {
        return new WebSocketSendRequest
        {
            MessageType = messageType,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static void EnableHeartbeat(WebSocketConnectionOptions options)
    {
        options.Heartbeat.Enabled = true;
        options.Heartbeat.IdleIntervalMilliseconds = 100;
        options.Heartbeat.PongTimeoutMilliseconds = 50;
        options.Heartbeat.TokenLength = 4;
    }

    static List<WebSocketEvent> DrainEvents(
        WebSocketConnection connection)
    {
        List<WebSocketEvent> events = [];
        WebSocketEvent? connectionEvent = connection.ReadNextEvent();
        while (connectionEvent != null)
        {
            events.Add(connectionEvent);
            connectionEvent = connection.ReadNextEvent();
        }

        return events;
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}

internal class LoopbackTestPair
{
    internal WebSocketConnectionOptions ClientOptions =
        new WebSocketConnectionOptions();
    internal WebSocketConnectionOptions ServerOptions =
        new WebSocketConnectionOptions();
    internal WebSocketConnection Client = null!;
    internal WebSocketConnection Server = null!;
}

internal class LoopbackMaskKeySource : IWebSocketMaskKeySource
{
    uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey = NextKey + 0x01010101U;
        return key;
    }
}
