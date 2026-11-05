using websocket;

namespace websocket.Tests;

public class WebSocketConnectionTests
{
    [Test]
    public void PublicSnapshotsDeepCopyRetainedOptionAndHandshakeData()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        options.Heartbeat.Enabled = true;
        options.Heartbeat.TokenLength = 4;
        WebSocketHeaderField requestField = new WebSocketHeaderField
        {
            Name = "Host",
            Value = "example.test"
        };
        byte[] responseBody = [1, 2, 3];
        WebSocketHandshakeResult handshake = Handshake();
        handshake.Request.Fields = [requestField];
        handshake.Response.HasBody = true;
        handshake.Response.Body = responseBody;
        WebSocketConnection connection = new WebSocketConnection(
            options,
            handshake);

        options.Heartbeat.TokenLength = 1;
        requestField.Value = "mutated-input";
        responseBody[0] = 99;
        WebSocketConnectionInfo first = connection.GetConnectionInfo();
        first.Options.Heartbeat.TokenLength = 2;
        first.Handshake.Request.Fields[0].Value = "mutated-snapshot";
        first.Handshake.Response.Body[0] = 88;
        WebSocketConnectionInfo second = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(second.Options.Heartbeat.TokenLength, Is.EqualTo(4));
            Assert.That(second.Handshake.Request.Fields[0].Value,
                Is.EqualTo("example.test"));
            Assert.That(second.Handshake.Response.Body,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(second.Options, Is.Not.SameAs(first.Options));
            Assert.That(second.Options.Heartbeat,
                Is.Not.SameAs(first.Options.Heartbeat));
            Assert.That(second.Handshake, Is.Not.SameAs(first.Handshake));
            Assert.That(second.Handshake.Request.Fields,
                Is.Not.SameAs(first.Handshake.Request.Fields));
            Assert.That(second.Handshake.Request.Fields[0],
                Is.Not.SameAs(first.Handshake.Request.Fields[0]));
            Assert.That(second.Handshake.Response.Body,
                Is.Not.SameAs(first.Handshake.Response.Body));
        });
    }

    [Test]
    public void WholeMessageSendResumesAfterOutputBackpressure()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Reassembled,
            20);
        options.MaximumFramePayloadLength = 6;
        options.MaximumMessagePayloadLength = 18;
        options.MaximumDecompressedMessagePayloadLength = 18;
        options.MaximumFragmentCount = 3;
        options.AutomaticFramePayloadLength = 6;
        WebSocketHandshakeResult handshake = Handshake();
        handshake.HasSelectedSubprotocol = true;
        handshake.SelectedSubprotocol = "chat";
        WebSocketConnection connection = new WebSocketConnection(
            options,
            handshake);
        byte[] payload =
            [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17];
        WebSocketSendRequest request = Request(payload);

        WebSocketParseStatus blocked =
            connection.SendWholeMessage(request);
        WebSocketParseStatus competing = connection.SendWholeMessage(
            Request([99]));
        WebSocketConnectionInfo blockedInfo =
            connection.GetConnectionInfo();
        byte[] prefix = DrainCount(connection, 8);
        WebSocketParseStatus resumed =
            connection.SendWholeMessage(request);
        payload[6] = 99;
        options.AutomaticFramePayloadLength = 1;
        handshake.SelectedSubprotocol = "changed";
        byte[] suffix = DrainAll(connection);
        WebSocketConnectionInfo completedInfo =
            connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(blocked,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(competing,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blockedInfo.PendingOutputBytes, Is.EqualTo(16));
            Assert.That(blockedInfo.SentFrameCount, Is.EqualTo(2));
            Assert.That(blockedInfo.SentMessageCount, Is.Zero);
            Assert.That(prefix,
                Is.EqualTo(new byte[]
                {
                    0x02, 0x06, 0, 1, 2, 3, 4, 5
                }));
            Assert.That(resumed,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(suffix,
                Is.EqualTo(new byte[]
                {
                    0x00, 0x06, 6, 7, 8, 9, 10, 11,
                    0x80, 0x06, 12, 13, 14, 15, 16, 17
                }));
            Assert.That(completedInfo.PendingOutputBytes, Is.Zero);
            Assert.That(completedInfo.SentFrameCount, Is.EqualTo(3));
            Assert.That(completedInfo.SentMessageCount, Is.EqualTo(1));
            Assert.That(completedInfo.SelectedSubprotocol,
                Is.EqualTo("chat"));
            Assert.That(completedInfo.Options.AutomaticFramePayloadLength,
                Is.EqualTo(6));
            Assert.That(completedInfo.Handshake.SelectedSubprotocol,
                Is.EqualTo("chat"));
        });
    }

    [Test]
    public void StreamingSendRetainsLifecycleAndCountsCompletedMessage()
    {
        WebSocketConnection connection = new WebSocketConnection(
            Options(
                WebSocketRole.Server,
                WebSocketMessageDeliveryMode.Streaming,
                64),
            Handshake());
        WebSocketSendChunk first = Chunk(true, false, [65]);
        WebSocketSendChunk final = Chunk(false, true, [66]);

        WebSocketParseStatus firstStatus =
            connection.SendMessageChunk(first);
        WebSocketParseStatus overlap = connection.SendWholeMessage(
            Request([9]));
        WebSocketParseStatus finalStatus =
            connection.SendMessageChunk(final);
        byte[] wire = DrainAll(connection);
        WebSocketConnectionInfo information =
            connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(firstStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(overlap,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(finalStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(wire,
                Is.EqualTo(new byte[]
                {
                    0x01, 0x01, 65,
                    0x80, 0x01, 66
                }));
            Assert.That(information.SentFrameCount, Is.EqualTo(2));
            Assert.That(information.SentMessageCount, Is.EqualTo(1));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    [Test]
    public void ReassembledReceiveProducesOneOwnedEventAndUpdatesCounters()
    {
        WebSocketConnection connection = new WebSocketConnection(
            Options(
                WebSocketRole.Client,
                WebSocketMessageDeliveryMode.Reassembled,
                64),
            Handshake());

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(
                [0x81, 0x02, 0x68, 0x69]);
        WebSocketConnectionInfo beforeRead =
            connection.GetConnectionInfo();
        WebSocketEvent? message = connection.ReadNextEvent();
        WebSocketConnectionInfo afterRead =
            connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.ConsumedCount, Is.EqualTo(4));
            Assert.That(received.FrameCount, Is.EqualTo(1));
            Assert.That(beforeRead.ReceivedFrameCount, Is.EqualTo(1));
            Assert.That(beforeRead.ReceivedMessageCount, Is.EqualTo(1));
            Assert.That(beforeRead.PendingEventCount, Is.EqualTo(1));
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.Type,
                Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(message.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(message.Payload,
                Is.EqualTo(new byte[] { 0x68, 0x69 }));
            Assert.That(message.First, Is.True);
            Assert.That(message.Final, Is.True);
            Assert.That(afterRead.PendingEventCount, Is.Zero);
        });
    }

    [Test]
    public void HeartbeatAndCloseUseTheSameComposedConnectionState()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        options.Heartbeat.Enabled = true;
        options.Heartbeat.IdleIntervalMilliseconds = 100;
        options.Heartbeat.PongTimeoutMilliseconds = 50;
        options.Heartbeat.TokenLength = 4;
        WebSocketConnection connection = new WebSocketConnection(
            options,
            Handshake());

        WebSocketParseStatus advanced =
            connection.AdvanceConnectionTime(100);
        WebSocketConnectionInfo heartbeat = connection.GetConnectionInfo();
        byte[] ping = DrainAll(connection);
        WebSocketLocalCloseResult started = connection.StartConnectionClose(
            new WebSocketCloseInfo
            {
                HasCode = true,
                Code = 1000,
                HasReason = true,
                Reason = "done"
            });
        byte[] localClose = DrainAll(connection);
        WebSocketInboundFramePumpResult remoteClose =
            connection.ReceiveConnectionBytes(
                [0x88, 0x82, 1, 2, 3, 4, 0x02, 0xEA]);
        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionTransitionResult repeatedEnd =
            connection.EndConnectionInput();
        WebSocketEvent? closeEvent = connection.ReadNextEvent();
        WebSocketEvent? transportEndEvent = connection.ReadNextEvent();
        WebSocketEvent? afterTransportEnd = connection.ReadNextEvent();
        WebSocketConnectionInfo closed = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(advanced,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(heartbeat.HeartbeatPingOutstanding, Is.True);
            Assert.That(heartbeat.HeartbeatPingSentMilliseconds,
                Is.EqualTo(100));
            Assert.That(ping[0], Is.EqualTo(0x89));
            Assert.That(started.CloseQueued, Is.True);
            Assert.That(localClose[0], Is.EqualTo(0x88));
            Assert.That(remoteClose.FrameCount, Is.EqualTo(1));
            Assert.That(ended.Status,
                Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(ended.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(repeatedEnd.Status,
                Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Type,
                Is.EqualTo(WebSocketEventType.Close));
            Assert.That(transportEndEvent, Is.Not.Null);
            Assert.That(transportEndEvent!.Type,
                Is.EqualTo(WebSocketEventType.TransportEnd));
            Assert.That(transportEndEvent.First, Is.True);
            Assert.That(transportEndEvent.Final, Is.True);
            Assert.That(afterTransportEnd, Is.Null);
            Assert.That(closed.CleanClose, Is.True);
            Assert.That(closed.HasLocalClose, Is.True);
            Assert.That(closed.LocalClose.Sent, Is.True);
            Assert.That(closed.HasRemoteClose, Is.True);
            Assert.That(closed.RemoteClose.Received, Is.True);
            Assert.That(closed.ReceivedFrameCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OutboundExtensionFailureEntersFailureClosingAndQueuesError()
    {
        ConnectionFailingExtension extension =
            new ConnectionFailingExtension();
        WebSocketExtensionNegotiation negotiation =
            new WebSocketExtensionNegotiation
            {
                Name = "x-fail",
                Status = WebSocketExtensionStatus.Negotiated,
                HasAcceptedConfiguration = true,
                AcceptedConfiguration = new WebSocketExtensionOffer
                {
                    Name = "x-fail"
                }
            };
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        WebSocketHandshakeResult handshake = Handshake();
        handshake.Extensions = [negotiation];
        WebSocketConnection connection = new WebSocketConnection(
            options,
            handshake);

        WebSocketParseStatus status = connection.SendWholeMessage(
            Request([1]));
        WebSocketConnectionInfo information =
            connection.GetConnectionInfo();
        WebSocketEvent? error = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(information.HasError, Is.True);
            Assert.That(information.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Compression));
            Assert.That(information.Error.Identifier,
                Is.EqualTo("connection-extension-failed"));
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(error.Error.Identifier,
                Is.EqualTo("connection-extension-failed"));
        });
    }

    [Test]
    public void ConstructorRequiresAnAcceptedHandshake()
    {
        WebSocketHandshakeResult incomplete = Handshake();
        incomplete.Outcome = WebSocketHandshakeOutcome.Incomplete;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketConnection(
                    null!,
                    incomplete));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketConnection(
                    Options(
                        WebSocketRole.Server,
                        WebSocketMessageDeliveryMode.Streaming,
                        64),
                    null!));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketConnection(
                    Options(
                        WebSocketRole.Server,
                        WebSocketMessageDeliveryMode.Streaming,
                        64),
                    incomplete));
        });
    }

    [Test]
    public void ConstructorExposesDocumentedLimitAndExtensionExceptions()
    {
        WebSocketConnectionOptions invalidLimit = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        invalidLimit.MaximumFramePayloadLength = 0;
        WebSocketConnectionOptions invalidExtension = Options(
            WebSocketRole.Server,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        invalidExtension.HasPerMessageDeflate = true;
        invalidExtension.PerMessageDeflate.CompressionThreshold = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketConnection(invalidLimit, Handshake()));
            Assert.Throws<WebSocketExtensionException>(() =>
                new WebSocketConnection(invalidExtension, Handshake()));
        });
    }

    [Test]
    public void CallerSendRejectionsReturnStableStatusesWithoutFailingConnection()
    {
        WebSocketConnection connection = new WebSocketConnection(
            Options(
                WebSocketRole.Server,
                WebSocketMessageDeliveryMode.Streaming,
                64),
            Handshake());
        WebSocketSendRequest oversized = Request(new byte[33]);
        WebSocketSendRequest invalidType = Request([]);
        invalidType.MessageType = (WebSocketMessageType)99;

        WebSocketParseStatus missing = connection.SendWholeMessage(null!);
        WebSocketParseStatus invalid = connection.SendWholeMessage(invalidType);
        WebSocketParseStatus limited = connection.SendWholeMessage(oversized);
        WebSocketParseStatus accepted = connection.SendWholeMessage(Request([1]));
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(missing,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(limited,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(accepted, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(information.HasError, Is.False);
        });
    }

    [Test]
    public void ClientWireVectorUsesInjectedMaskingKeySource()
    {
        ConnectionMaskKeySource source = new ConnectionMaskKeySource();
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            WebSocketMessageDeliveryMode.Streaming,
            64);
        options.HasMaskKeySource = true;
        options.MaskKeySource = source;
        WebSocketConnection connection = new WebSocketConnection(
            options,
            Handshake());

        WebSocketParseStatus status =
            connection.SendWholeMessage(Request([0x48, 0x69]));
        byte[] wire = DrainAll(connection);

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(source.CallCount, Is.EqualTo(1));
            Assert.That(wire,
                Is.EqualTo(new byte[]
                {
                    0x82, 0x82,
                    0x11, 0x22, 0x33, 0x44,
                    0x59, 0x4b
                }));
        });
    }

    static WebSocketConnectionOptions Options(
        WebSocketRole role,
        WebSocketMessageDeliveryMode deliveryMode,
        int outputCapacity)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = 16,
            MaximumMessagePayloadLength = 32,
            MaximumDecompressedMessagePayloadLength = 32,
            MaximumFragmentCount = 8,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = outputCapacity,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 4,
            CloseTimeoutMilliseconds = 1000
        };
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

    static WebSocketSendRequest Request(byte[] payload)
    {
        return new WebSocketSendRequest
        {
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static WebSocketSendChunk Chunk(
        bool first,
        bool final,
        byte[] payload)
    {
        return new WebSocketSendChunk
        {
            First = first,
            Final = final,
            MessageType = WebSocketMessageType.Text,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] DrainCount(
        WebSocketConnection connection,
        int count)
    {
        byte[] output = new byte[count];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        return DrainCount(
            connection,
            connection.GetPendingConnectionWriteCount());
    }
}

internal class ConnectionMaskKeySource : IWebSocketMaskKeySource
{
    internal int CallCount;
    internal uint NextKey = 0x11223344U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey = NextKey + 0x01010101U;
        CallCount = CallCount + 1;
        return key;
    }
}

internal class ConnectionFailingExtension : IWebSocketExtension
{
    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = "x-fail" };
    }

    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(
        WebSocketExtensionOffer offer)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(
        WebSocketExtensionOffer response)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketTransformResult TransformOutboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.ProtocolFailure,
            HasError = true,
            Error = new WebSocketError
            {
                Kind = WebSocketErrorKind.Compression,
                Identifier = "connection-extension-failed",
                Diagnostic = "The test extension rejected outbound data."
            }
        };
    }

    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = source.Length,
            Output = source.ToArray()
        };
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
    }

    public void ResetExtensionState()
    {
    }
}
