using websocket;

namespace websocket.Tests;

public class WebSocketConnectionTerminationTests
{
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http1Upgrade)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http1Upgrade)]
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http3ExtendedConnect)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void CleanInputEndFinalizesBothCloseFactsAndOneTransportEvent(
        WebSocketRole role,
        WebSocketHandshakeTransport transport)
    {
        WebSocketConnection connection = Connection(role, transport, 1000);
        WebSocketLocalCloseResult local = connection.StartConnectionClose(
            Close(1000, "local"));
        byte[] sent = DrainAll(connection);
        WebSocketInboundFramePumpResult remote =
            connection.ReceiveConnectionBytes(InboundFrame(
                role,
                WebSocketOpcode.Close,
                [0x03, 0xe9, 0x70, 0x65, 0x65, 0x72]));

        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionTransitionResult repeated =
            connection.EndConnectionInput();
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketConnectionInfo first = connection.GetConnectionInfo();
        first.LocalClose.Reason = "changed";
        first.RemoteClose.Reason = "changed";
        WebSocketConnectionInfo second = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(local.CloseQueued, Is.True);
            Assert.That(CloseCode(role, sent), Is.EqualTo(1000));
            Assert.That(remote.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ended.Status, Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(ended.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(repeated.Changed, Is.False);
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.Close,
                    WebSocketEventType.TransportEnd
                }));
            Assert.That(second.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(second.Transport, Is.EqualTo(transport));
            Assert.That(second.TransportEnded, Is.True);
            Assert.That(second.TransportReset, Is.False);
            Assert.That(second.CleanClose, Is.True);
            Assert.That(second.ShouldEndTransport, Is.True);
            Assert.That(second.HasError, Is.False);
            Assert.That(second.HasLocalClose, Is.True);
            Assert.That(second.LocalClose.Code, Is.EqualTo(1000));
            Assert.That(second.LocalClose.Reason, Is.EqualTo("local"));
            Assert.That(second.LocalClose.Sent, Is.True);
            Assert.That(second.LocalClose.Clean, Is.True);
            Assert.That(second.HasRemoteClose, Is.True);
            Assert.That(second.RemoteClose.Code, Is.EqualTo(1001));
            Assert.That(second.RemoteClose.Reason, Is.EqualTo("peer"));
            Assert.That(second.RemoteClose.Received, Is.True);
            Assert.That(second.RemoteClose.Clean, Is.True);
        });
    }

    [TestCase(
        WebSocketHandshakeTransport.Http1Upgrade,
        "connection-transport-ended-before-close")]
    [TestCase(
        WebSocketHandshakeTransport.Http2ExtendedConnect,
        "connection-http2-end-stream-before-close")]
    [TestCase(
        WebSocketHandshakeTransport.Http3ExtendedConnect,
        "connection-http3-fin-before-close")]
    public void PrematureInputEndRecordsAbnormalClosureWithoutSending1006(
        WebSocketHandshakeTransport transport,
        string identifier)
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            transport,
            1000);

        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionTransitionResult repeated =
            connection.EndConnectionInput();
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            AssertTerminalFailure(ended, identifier);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.Error,
                    WebSocketEventType.TransportEnd
                }));
            Assert.That(events[0].Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(information.TransportEnded, Is.True);
            Assert.That(information.TransportReset, Is.False);
            Assert.That(information.CleanClose, Is.False);
            Assert.That(information.HasLocalClose, Is.False);
            Assert.That(information.HasRemoteClose, Is.False);
            Assert.That(information.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    [TestCase(
        WebSocketHandshakeTransport.Http1Upgrade,
        "connection-transport-reset")]
    [TestCase(
        WebSocketHandshakeTransport.Http2ExtendedConnect,
        "connection-http2-stream-reset")]
    [TestCase(
        WebSocketHandshakeTransport.Http3ExtendedConnect,
        "connection-http3-stream-reset")]
    public void StreamResetRecordsTransportFactsAndNeverQueuesAClose(
        WebSocketHandshakeTransport transport,
        string identifier)
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Server,
            transport,
            1000);
        connection.SendWholeMessage(Request([1, 2, 3]));

        WebSocketConnectionTransitionResult reset =
            connection.ResetConnectionTransport();
        WebSocketConnectionTransitionResult repeated =
            connection.ResetConnectionTransport();
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            AssertTerminalFailure(reset, identifier);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.Error,
                    WebSocketEventType.TransportEnd
                }));
            Assert.That(information.TransportEnded, Is.True);
            Assert.That(information.TransportReset, Is.True);
            Assert.That(information.ShouldEndTransport, Is.False);
            Assert.That(information.HasLocalClose, Is.False);
        });
    }

    [TestCase(WebSocketHandshakeTransport.Http1Upgrade)]
    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void AbortIsTerminalIdempotentAndTransportIndependent(
        WebSocketHandshakeTransport transport)
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            transport,
            1000);
        connection.SendConnectionPing([1, 2, 3], 0, 3);

        WebSocketConnectionTransitionResult aborted =
            connection.AbortConnectionTransport();
        WebSocketConnectionTransitionResult repeated =
            connection.AbortConnectionTransport();
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            AssertTerminalFailure(aborted, "connection-aborted");
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("connection-aborted"));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.Error,
                    WebSocketEventType.TransportEnd
                }));
            Assert.That(information.Transport, Is.EqualTo(transport));
            Assert.That(information.TransportEnded, Is.True);
            Assert.That(information.TransportReset, Is.False);
            Assert.That(information.ShouldEndTransport, Is.False);
            Assert.That(information.HasLocalClose, Is.False);
        });
    }

    [Test]
    public void CloseTimeoutUsesExactBoundaryAndDoesNotTransmitPseudoCode()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            WebSocketHandshakeTransport.Http1Upgrade,
            100);
        Assert.That(connection.AdvanceConnectionTime(50),
            Is.EqualTo(WebSocketParseStatus.Complete));
        connection.StartConnectionClose(new WebSocketCloseInfo());
        byte[] requestedClose = DrainAll(connection);

        WebSocketParseStatus before = connection.AdvanceConnectionTime(149);
        WebSocketParseStatus boundary = connection.AdvanceConnectionTime(150);
        byte[] afterTimeout = DrainAll(connection);
        WebSocketEvent? error = connection.ReadNextEvent();
        WebSocketConnectionInfo timedOut = connection.GetConnectionInfo();
        WebSocketConnectionTransitionResult aborted =
            connection.AbortConnectionTransport();
        WebSocketEvent? transportEnd = connection.ReadNextEvent();
        WebSocketConnectionInfo terminal = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(ClosePayloadLength(
                WebSocketRole.Client,
                requestedClose), Is.Zero);
            Assert.That(before, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(boundary,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(afterTimeout, Is.Empty);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(error.Error.Identifier,
                Is.EqualTo("connection-close-timeout"));
            Assert.That(error.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(timedOut.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(timedOut.ElapsedMilliseconds, Is.EqualTo(150));
            Assert.That(timedOut.TransportEnded, Is.False);
            Assert.That(timedOut.ShouldEndTransport, Is.True);
            Assert.That(timedOut.HasLocalClose, Is.True);
            Assert.That(timedOut.LocalClose.HasCode, Is.False);
            Assert.That(timedOut.LocalClose.Sent, Is.True);
            Assert.That(aborted.Error.Identifier,
                Is.EqualTo("connection-close-timeout"));
            Assert.That(transportEnd, Is.Not.Null);
            Assert.That(transportEnd!.Type,
                Is.EqualTo(WebSocketEventType.TransportEnd));
            Assert.That(terminal.TransportEnded, Is.True);
            Assert.That(terminal.Error.Identifier,
                Is.EqualTo("connection-close-timeout"));
        });
    }

    [Test]
    public void PrematureEndRetainsExistingLocalCloseFacts()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Server,
            WebSocketHandshakeTransport.Http1Upgrade,
            1000);
        connection.StartConnectionClose(Close(1001, "away"));
        byte[] localClose = DrainAll(connection);

        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionInfo first = connection.GetConnectionInfo();
        first.LocalClose.Reason = "changed";
        first.Error.Identifier = "changed";
        WebSocketConnectionInfo second = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(CloseCode(WebSocketRole.Server, localClose),
                Is.EqualTo(1001));
            AssertTerminalFailure(
                ended,
                "connection-transport-ended-before-close");
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(second.HasLocalClose, Is.True);
            Assert.That(second.LocalClose.Code, Is.EqualTo(1001));
            Assert.That(second.LocalClose.Reason, Is.EqualTo("away"));
            Assert.That(second.LocalClose.Sent, Is.True);
            Assert.That(second.LocalClose.Clean, Is.False);
            Assert.That(second.HasRemoteClose, Is.False);
            Assert.That(second.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
        });
    }

    [Test]
    public void FirstFailureAndFailedStateRemainPermanentAcrossLaterCalls()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Server,
            WebSocketHandshakeTransport.Http1Upgrade,
            1000);
        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        DrainEvents(connection);

        WebSocketInboundFramePumpResult receive =
            connection.ReceiveConnectionBytes(InboundFrame(
                WebSocketRole.Server,
                WebSocketOpcode.Binary,
                [1]));
        WebSocketParseStatus whole =
            connection.SendWholeMessage(Request([2]));
        WebSocketParseStatus chunk = connection.SendMessageChunk(
            Chunk([3]));
        WebSocketControlSendResult ping =
            connection.SendConnectionPing([4], 0, 1);
        WebSocketControlSendResult pong =
            connection.SendConnectionPong([5], 0, 1);
        WebSocketLocalCloseResult close =
            connection.StartConnectionClose(Close(1000, "later"));
        WebSocketParseStatus time = connection.AdvanceConnectionTime(1);
        WebSocketConnectionTransitionResult reset =
            connection.ResetConnectionTransport();
        WebSocketConnectionTransitionResult abort =
            connection.AbortConnectionTransport();
        WebSocketConnectionTransitionResult endAgain =
            connection.EndConnectionInput();
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            AssertTerminalFailure(
                ended,
                "connection-transport-ended-before-close");
            Assert.That(receive.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(receive.ConsumedCount, Is.Zero);
            Assert.That(receive.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(whole,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(chunk,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(ping.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(pong.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(close.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(time,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(reset.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(abort.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(endAgain.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(information.Error.Identifier,
                Is.EqualTo("connection-transport-ended-before-close"));
            Assert.That(information.TransportReset, Is.True);
            Assert.That(information.HasLocalClose, Is.False);
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(connection.ReadNextEvent(), Is.Null);
        });
    }

    [Test]
    public void ProtocolFailureRemainsPrimaryWhenTransportThenEnds()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Server,
            WebSocketHandshakeTransport.Http1Upgrade,
            1000);

        WebSocketInboundFramePumpResult failed =
            connection.ReceiveConnectionBytes([0x82, 0x00]);
        byte[] protocolClose = DrainAll(connection);
        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("frame-mask-required"));
            Assert.That(CloseCode(WebSocketRole.Server, protocolClose),
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(ended.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(ended.Error.Identifier,
                Is.EqualTo("frame-mask-required"));
            Assert.That(information.Error.Identifier,
                Is.EqualTo("frame-mask-required"));
            Assert.That(information.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(information.TransportEnded, Is.True);
            Assert.That(information.HasLocalClose, Is.True);
            Assert.That(information.LocalClose.Code,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(information.HasRemoteClose, Is.False);
        });
    }

    static WebSocketConnection Connection(
        WebSocketRole role,
        WebSocketHandshakeTransport transport,
        int closeTimeoutMilliseconds)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = transport,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            MaximumFramePayloadLength = 125,
            MaximumMessagePayloadLength = 256,
            MaximumDecompressedMessagePayloadLength = 256,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = 64,
            AutomaticCloseResponse = true,
            CloseTimeoutMilliseconds = closeTimeoutMilliseconds
        };
        if (role == WebSocketRole.Client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = new TerminationMaskKeySource();
        }

        return new WebSocketConnection(
            options,
            new WebSocketHandshakeResult
            {
                Outcome = WebSocketHandshakeOutcome.Accepted,
                Transport = transport
            });
    }

    static WebSocketCloseInfo Close(int code, string reason)
    {
        return new WebSocketCloseInfo
        {
            HasCode = true,
            Code = code,
            HasReason = reason.Length != 0,
            Reason = reason
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

    static WebSocketSendChunk Chunk(byte[] payload)
    {
        return new WebSocketSendChunk
        {
            First = true,
            Final = true,
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] InboundFrame(
        WebSocketRole receiverRole,
        WebSocketOpcode opcode,
        byte[] payload)
    {
        bool masked = receiverRole == WebSocketRole.Server;
        int headerLength = masked ? 6 : 2;
        byte[] frame = new byte[headerLength + payload.Length];
        frame[0] = (byte)(0x80 | (int)opcode);
        frame[1] = (byte)((masked ? 0x80 : 0) | payload.Length);
        if (masked)
        {
            frame[2] = 1;
            frame[3] = 2;
            frame[4] = 3;
            frame[5] = 4;
            int index = 0;
            while (index < payload.Length)
            {
                frame[6 + index] =
                    (byte)(payload[index] ^ frame[2 + index % 4]);
                index++;
            }
        }
        else
        {
            payload.CopyTo(frame, 2);
        }

        return frame;
    }

    static int CloseCode(WebSocketRole senderRole, byte[] wire)
    {
        byte[] payload = DecodeClosePayload(senderRole, wire);
        Assert.That(payload, Has.Length.GreaterThanOrEqualTo(2));
        return (payload[0] << 8) | payload[1];
    }

    static int ClosePayloadLength(WebSocketRole senderRole, byte[] wire)
    {
        return DecodeClosePayload(senderRole, wire).Length;
    }

    static byte[] DecodeClosePayload(
        WebSocketRole senderRole,
        byte[] wire)
    {
        WebSocketRole receiverRole = senderRole == WebSocketRole.Client
            ? WebSocketRole.Server
            : WebSocketRole.Client;
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            receiverRole,
            125,
            []);
        WebSocketFrameReadResult frame = codec.ReadFrame(wire);
        Assert.That(frame.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        Assert.That(frame.Header.Opcode, Is.EqualTo(WebSocketOpcode.Close));
        Assert.That(frame.ConsumedCount, Is.EqualTo(wire.Length));
        return frame.Payload
            .AsSpan(frame.PayloadOffset, frame.PayloadCount)
            .ToArray();
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static List<WebSocketEvent> DrainEvents(WebSocketConnection connection)
    {
        var events = new List<WebSocketEvent>();
        WebSocketEvent? item = connection.ReadNextEvent();
        while (item != null)
        {
            events.Add(item);
            item = connection.ReadNextEvent();
        }

        return events;
    }

    static void AssertTerminalFailure(
        WebSocketConnectionTransitionResult result,
        string identifier)
    {
        Assert.That(result.Status,
            Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        Assert.That(result.State, Is.EqualTo(WebSocketConnectionState.Failed));
        Assert.That(result.HasError, Is.True);
        Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
        Assert.That(result.Error.HasCloseCode, Is.True);
        Assert.That(result.Error.CloseCode,
            Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
        Assert.That(result.Error.HasKnownCloseCode, Is.True);
        Assert.That(result.Error.KnownCloseCode,
            Is.EqualTo(WebSocketCloseCode.AbnormalClosure));
        Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
    }
}

internal class TerminationMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        return key;
    }
}
