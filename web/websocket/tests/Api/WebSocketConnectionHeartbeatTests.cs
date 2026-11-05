using websocket;

namespace websocket.Tests;

public class WebSocketConnectionHeartbeatTests
{
    [Test]
    public void IdlePingAppearsOnlyAtTheExactBoundaryAcrossPartialAdvances()
    {
        WebSocketConnection connection = Connection(true, 100, 50, 4);

        WebSocketParseStatus zero = connection.AdvanceConnectionTime(0);
        WebSocketParseStatus first = connection.AdvanceConnectionTime(25);
        WebSocketParseStatus repeated = connection.AdvanceConnectionTime(25);
        WebSocketParseStatus below = connection.AdvanceConnectionTime(99);
        WebSocketConnectionInfo before = connection.GetConnectionInfo();
        WebSocketParseStatus boundary = connection.AdvanceConnectionTime(100);
        WebSocketConnectionInfo scheduled = connection.GetConnectionInfo();
        byte[] wire = DrainAll(connection);
        WebSocketFrameReadResult ping = DecodeClientFrame(wire);
        WebSocketParseStatus sameBoundary =
            connection.AdvanceConnectionTime(100);

        Assert.Multiple(() =>
        {
            Assert.That(zero, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(below, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(before.ElapsedMilliseconds, Is.EqualTo(99));
            Assert.That(before.PendingOutputBytes, Is.Zero);
            Assert.That(before.HeartbeatPingOutstanding, Is.False);
            Assert.That(boundary, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(scheduled.ElapsedMilliseconds, Is.EqualTo(100));
            Assert.That(scheduled.HeartbeatPingOutstanding, Is.True);
            Assert.That(scheduled.HasHeartbeatPingSentMilliseconds, Is.True);
            Assert.That(scheduled.HeartbeatPingSentMilliseconds, Is.EqualTo(100));
            Assert.That(ping.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping.Header.Opcode, Is.EqualTo(WebSocketOpcode.Ping));
            Assert.That(ping.PayloadCount, Is.EqualTo(4));
            Assert.That(sameBoundary, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
        });
    }

    [Test]
    public void MatchingPongRecordsRoundTripAndRestartsTheIdleInterval()
    {
        WebSocketConnection connection = Connection(true, 100, 50, 4);
        connection.AdvanceConnectionTime(100);
        WebSocketFrameReadResult ping = DecodeClientFrame(
            DrainAll(connection));
        byte[] token = EventPayload(ping);

        WebSocketParseStatus advanced = connection.AdvanceConnectionTime(125);
        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(
                ControlFrame(WebSocketOpcode.Pong, token));
        WebSocketEvent? acknowledged = connection.ReadNextEvent();
        WebSocketConnectionInfo matched = connection.GetConnectionInfo();
        WebSocketParseStatus belowNext =
            connection.AdvanceConnectionTime(224);
        int belowNextPending = connection.GetPendingConnectionWriteCount();
        WebSocketParseStatus nextBoundary =
            connection.AdvanceConnectionTime(225);
        WebSocketConnectionInfo rescheduled = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(advanced, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(acknowledged, Is.Not.Null);
            Assert.That(acknowledged!.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(EventPayload(acknowledged), Is.EqualTo(token));
            Assert.That(acknowledged.HasPingElapsedMilliseconds, Is.True);
            Assert.That(acknowledged.PingElapsedMilliseconds, Is.EqualTo(25));
            Assert.That(matched.HeartbeatPingOutstanding, Is.False);
            Assert.That(matched.HasLastPingRoundTripMilliseconds, Is.True);
            Assert.That(matched.LastPingRoundTripMilliseconds, Is.EqualTo(25));
            Assert.That(belowNext, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(belowNextPending, Is.Zero);
            Assert.That(nextBoundary, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connection.GetPendingConnectionWriteCount(),
                Is.GreaterThan(0));
            Assert.That(rescheduled.HeartbeatPingOutstanding, Is.True);
            Assert.That(rescheduled.HeartbeatPingSentMilliseconds,
                Is.EqualTo(225));
        });
    }

    [Test]
    public void MissingPongFailsAtTheExactDeadlineAndCleansTracking()
    {
        WebSocketConnection connection = Connection(true, 100, 50, 4);
        connection.AdvanceConnectionTime(100);
        DrainAll(connection);

        WebSocketParseStatus below = connection.AdvanceConnectionTime(149);
        WebSocketConnectionInfo beforeDeadline =
            connection.GetConnectionInfo();
        WebSocketParseStatus deadline = connection.AdvanceConnectionTime(150);
        WebSocketConnectionInfo failed = connection.GetConnectionInfo();
        WebSocketEvent? error = connection.ReadNextEvent();
        WebSocketParseStatus repeated = connection.AdvanceConnectionTime(200);

        Assert.Multiple(() =>
        {
            Assert.That(below, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(beforeDeadline.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(beforeDeadline.HeartbeatPingOutstanding, Is.True);
            Assert.That(deadline,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.State, Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(failed.HasError, Is.True);
            Assert.That(failed.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Transport));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("heartbeat-pong-timeout"));
            Assert.That(failed.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(failed.HeartbeatPingOutstanding, Is.False);
            Assert.That(connection.PingPongHandler.GetOutstandingTrackedPingCount(),
                Is.Zero);
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(error.Error.Identifier,
                Is.EqualTo("heartbeat-pong-timeout"));
            Assert.That(repeated,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [Test]
    public void DisabledHeartbeatTracksElapsedTimeWithoutScheduling()
    {
        WebSocketConnection connection = Connection(false, 100, 50, 4);

        WebSocketParseStatus first = connection.AdvanceConnectionTime(100);
        WebSocketParseStatus later = connection.AdvanceConnectionTime(1000000);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(later, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(information.ElapsedMilliseconds, Is.EqualTo(1000000));
            Assert.That(information.HeartbeatPingOutstanding, Is.False);
            Assert.That(information.HasHeartbeatPingSentMilliseconds, Is.False);
            Assert.That(information.HasLastPingRoundTripMilliseconds, Is.False);
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(connection.PingPongHandler.GetOutstandingTrackedPingCount(),
                Is.Zero);
        });
    }

    [Test]
    public void LocalClosingStopsHeartbeatAndClearsOutstandingTracking()
    {
        WebSocketConnection connection = Connection(true, 100, 50, 4);
        connection.AdvanceConnectionTime(100);
        DrainAll(connection);
        WebSocketConnectionInfo beforeClose = connection.GetConnectionInfo();

        WebSocketLocalCloseResult close = connection.StartConnectionClose(
            new WebSocketCloseInfo
            {
                HasCode = true,
                Code = WebSocketCloseCodes.NormalClosure
            });
        WebSocketConnectionInfo closing = connection.GetConnectionInfo();
        byte[] closeWire = DrainAll(connection);
        WebSocketParseStatus advanced = connection.AdvanceConnectionTime(150);
        WebSocketControlSendResult pingRejected = connection.SendConnectionPing(
            [1],
            0,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(beforeClose.HeartbeatPingOutstanding, Is.True);
            Assert.That(close.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(close.CloseQueued, Is.True);
            Assert.That(closing.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(closing.HeartbeatPingOutstanding, Is.False);
            Assert.That(connection.PingPongHandler.GetOutstandingTrackedPingCount(),
                Is.Zero);
            Assert.That(closeWire[0] & 0x0f,
                Is.EqualTo((int)WebSocketOpcode.Close));
            Assert.That(advanced, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(connection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(pingRejected.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(pingRejected.Error.Identifier,
                Is.EqualTo("connection-control-state-invalid"));
        });
    }

    [Test]
    public void RemoteClosingStopsHeartbeatAndClearsOutstandingTracking()
    {
        WebSocketConnection connection = Connection(true, 100, 50, 4);
        connection.AdvanceConnectionTime(100);
        DrainAll(connection);

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(
                ControlFrame(WebSocketOpcode.Close, [0x03, 0xe8]));
        WebSocketEvent? close = connection.ReadNextEvent();
        WebSocketConnectionInfo closing = connection.GetConnectionInfo();
        DrainAll(connection);
        WebSocketParseStatus advanced = connection.AdvanceConnectionTime(149);

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(close, Is.Not.Null);
            Assert.That(close!.Type, Is.EqualTo(WebSocketEventType.Close));
            Assert.That(closing.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(closing.HeartbeatPingOutstanding, Is.False);
            Assert.That(connection.PingPongHandler.GetOutstandingTrackedPingCount(),
                Is.Zero);
            Assert.That(advanced, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
        });
    }

    static WebSocketConnection Connection(
        bool enabled,
        int idleMilliseconds,
        int timeoutMilliseconds,
        int tokenLength)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = 32,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = 4,
            AutomaticCloseResponse = true,
            CloseTimeoutMilliseconds = 1000,
            HasMaskKeySource = true,
            MaskKeySource = new HeartbeatMaskKeySource(),
            Heartbeat = new WebSocketHeartbeatOptions
            {
                Enabled = enabled,
                IdleIntervalMilliseconds = idleMilliseconds,
                PongTimeoutMilliseconds = timeoutMilliseconds,
                TokenLength = tokenLength
            }
        };
        return new WebSocketConnection(
            options,
            new WebSocketHandshakeResult
            {
                Outcome = WebSocketHandshakeOutcome.Accepted,
                Transport = WebSocketHandshakeTransport.Http1Upgrade
            });
    }

    static WebSocketFrameReadResult DecodeClientFrame(byte[] wire)
    {
        IWebSocketFrameCodec codec = new WebSocketFrameCodec(
            WebSocketRole.Server,
            125,
            []);
        return codec.ReadFrame(wire);
    }

    static byte[] ControlFrame(WebSocketOpcode opcode, byte[] payload)
    {
        byte[] wire = new byte[payload.Length + 2];
        wire[0] = (byte)(0x80 | (int)opcode);
        wire[1] = (byte)payload.Length;
        payload.CopyTo(wire, 2);
        return wire;
    }

    static byte[] EventPayload(WebSocketFrameReadResult frame)
    {
        return frame.Payload
            .AsSpan(frame.PayloadOffset, frame.PayloadCount)
            .ToArray();
    }

    static byte[] EventPayload(WebSocketEvent connectionEvent)
    {
        return connectionEvent.Payload
            .AsSpan(
                connectionEvent.PayloadOffset,
                connectionEvent.PayloadCount)
            .ToArray();
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }
}

internal class HeartbeatMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        return key;
    }
}
