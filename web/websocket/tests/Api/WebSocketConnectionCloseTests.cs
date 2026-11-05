using websocket;

namespace websocket.Tests;

public class WebSocketConnectionCloseTests
{
    [TestCase(WebSocketRole.Client, false)]
    [TestCase(WebSocketRole.Client, true)]
    [TestCase(WebSocketRole.Server, false)]
    [TestCase(WebSocketRole.Server, true)]
    public void LocalCloseWritesExactlyOneEmptyOrValidFrame(
        WebSocketRole role,
        bool withDetails)
    {
        WebSocketConnection connection = Connection(role);
        WebSocketCloseInfo request = withDetails
            ? Close(1000, "done")
            : new WebSocketCloseInfo();

        WebSocketLocalCloseResult started =
            connection.StartConnectionClose(request);
        WebSocketLocalCloseResult duplicateBeforeWrite =
            connection.StartConnectionClose(request);
        byte[] wire = DrainAll(connection);
        WebSocketLocalCloseResult duplicateAfterWrite =
            connection.StartConnectionClose(request);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();
        DecodedConnectionClose decoded = DecodeClose(role, wire);

        Assert.Multiple(() =>
        {
            Assert.That(started.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(started.CloseQueued, Is.True);
            Assert.That(duplicateBeforeWrite.Changed, Is.False);
            Assert.That(duplicateBeforeWrite.CloseQueued, Is.False);
            Assert.That(duplicateAfterWrite.CloseSent, Is.True);
            Assert.That(connection.GetPendingConnectionWriteCount(), Is.Zero);
            Assert.That(decoded.Masked,
                Is.EqualTo(role == WebSocketRole.Client));
            Assert.That(decoded.Payload,
                Is.EqualTo(withDetails
                    ? new byte[] { 0x03, 0xe8, 0x64, 0x6f, 0x6e, 0x65 }
                    : Array.Empty<byte>()));
            Assert.That(information.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(information.HasLocalClose, Is.True);
            Assert.That(information.LocalClose.Sent, Is.True);
            Assert.That(information.LocalClose.InitiatedLocally, Is.True);
            Assert.That(information.LocalClose.HasCode, Is.EqualTo(withDetails));
        });
    }

    [Test]
    public void InvalidLocalCloseCanBeCorrectedWithoutChangingOpenState()
    {
        WebSocketConnection connection = Connection(WebSocketRole.Server);
        WebSocketCloseInfo invalid = Close(1006, "reserved");

        WebSocketLocalCloseResult rejected =
            connection.StartConnectionClose(invalid);
        WebSocketConnectionInfo afterRejection = connection.GetConnectionInfo();
        WebSocketLocalCloseResult accepted =
            connection.StartConnectionClose(Close(1000, "valid"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Error.Identifier,
                Is.EqualTo("local-close-code-invalid"));
            Assert.That(afterRejection.State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(afterRejection.HasLocalClose, Is.False);
            Assert.That(connection.GetPendingConnectionWriteCount(),
                Is.GreaterThan(0));
            Assert.That(accepted.CloseQueued, Is.True);
        });
    }

    [TestCase(WebSocketRole.Client, false)]
    [TestCase(WebSocketRole.Client, true)]
    [TestCase(WebSocketRole.Server, false)]
    [TestCase(WebSocketRole.Server, true)]
    public void RemoteCloseIsSurfacedAndEchoedExactlyOnce(
        WebSocketRole role,
        bool withDetails)
    {
        WebSocketConnection connection = Connection(role);
        byte[] payload = withDetails
            ? new byte[] { 0x03, 0xe9, 0x61, 0x77, 0x61, 0x79 }
            : [];
        byte[] first = InboundFrame(role, WebSocketOpcode.Close, true, payload);
        byte[] duplicate = InboundFrame(
            role,
            WebSocketOpcode.Close,
            true,
            [0x03, 0xe8]);

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(Combine(first, duplicate));
        WebSocketEvent? closeEvent = connection.ReadNextEvent();
        WebSocketEvent? extraEvent = connection.ReadNextEvent();
        byte[] echo = DrainAll(connection);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();
        DecodedConnectionClose decoded = DecodeClose(role, echo);

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(2));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Type, Is.EqualTo(WebSocketEventType.Close));
            Assert.That(closeEvent.Close.HasCode, Is.EqualTo(withDetails));
            Assert.That(closeEvent.Close.Code,
                Is.EqualTo(withDetails ? 1001 : 0));
            Assert.That(extraEvent, Is.Null);
            Assert.That(decoded.Payload, Is.EqualTo(payload));
            Assert.That(decoded.Masked,
                Is.EqualTo(role == WebSocketRole.Client));
            Assert.That(information.HasRemoteClose, Is.True);
            Assert.That(information.RemoteClose.Received, Is.True);
            Assert.That(information.HasLocalClose, Is.True);
            Assert.That(information.LocalClose.InitiatedLocally, Is.False);
            Assert.That(information.LocalClose.Sent, Is.True);
        });
    }

    [TestCase(new byte[] { 0x03 }, "close-payload-one-byte", 1002)]
    [TestCase(new byte[] { 0x03, 0xee }, "close-code-invalid", 1002)]
    [TestCase(
        new byte[] { 0x03, 0xe8, 0xc0 },
        "close-reason-invalid-utf8",
        1007)]
    public void InvalidRemoteCloseFailsWithoutRecordingRemoteClose(
        byte[] payload,
        string identifier,
        int failureCode)
    {
        WebSocketConnection connection = Connection(WebSocketRole.Client);
        byte[] wire = InboundFrame(
            WebSocketRole.Client,
            WebSocketOpcode.Close,
            true,
            payload);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? error = connection.ReadNextEvent();
        WebSocketConnectionInfo information = connection.GetConnectionInfo();
        DecodedConnectionClose failure = DecodeClose(
            WebSocketRole.Client,
            DrainAll(connection));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.CloseCode, Is.EqualTo(failureCode));
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(information.HasRemoteClose, Is.False);
            Assert.That(information.HasLocalClose, Is.True);
            Assert.That(failure.CloseCode, Is.EqualTo(failureCode));
        });
    }

    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void SimultaneousCloseKeepsLocalDetailsAndCompletesCleanly(
        WebSocketRole role)
    {
        WebSocketConnection connection = Connection(role);
        WebSocketCloseInfo local = Close(1000, "local");
        byte[] remote = InboundFrame(
            role,
            WebSocketOpcode.Close,
            true,
            [0x03, 0xe9, 0x72, 0x65, 0x6d, 0x6f, 0x74, 0x65]);

        WebSocketLocalCloseResult started =
            connection.StartConnectionClose(local);
        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(remote);
        WebSocketEvent? closeEvent = connection.ReadNextEvent();
        DecodedConnectionClose sent = DecodeClose(role, DrainAll(connection));
        WebSocketConnectionTransitionResult ended =
            connection.EndConnectionInput();
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(started.CloseQueued, Is.True);
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Close.Code, Is.EqualTo(1001));
            Assert.That(sent.CloseCode, Is.EqualTo(1000));
            Assert.That(sent.Reason, Is.EqualTo("local"));
            Assert.That(ended.Status, Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(ended.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(information.CleanClose, Is.True);
            Assert.That(information.LocalClose.Code, Is.EqualTo(1000));
            Assert.That(information.LocalClose.InitiatedLocally, Is.True);
            Assert.That(information.RemoteClose.Code, Is.EqualTo(1001));
        });
    }

    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void LocalCloseRetriesAfterExistingOutputIsDrained(
        WebSocketRole role)
    {
        WebSocketConnection connection = Connection(role, 20);
        int pingPayloadCount = role == WebSocketRole.Client ? 14 : 18;
        WebSocketControlSendResult ping = connection.SendConnectionPing(
            new byte[pingPayloadCount],
            0,
            pingPayloadCount);
        var prefix = new byte[1];
        connection.WriteConnectionBytes(prefix);

        WebSocketLocalCloseResult blocked =
            connection.StartConnectionClose(new WebSocketCloseInfo());
        WebSocketConnectionState stateWhileBlocked =
            connection.GetConnectionInfo().State;
        byte[] priorOutput = DrainAll(connection);
        WebSocketLocalCloseResult retried =
            connection.StartConnectionClose(new WebSocketCloseInfo());
        DecodedConnectionClose close = DecodeClose(role, DrainAll(connection));

        Assert.Multiple(() =>
        {
            Assert.That(ping.Accepted, Is.True);
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(stateWhileBlocked,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(priorOutput, Has.Length.EqualTo(19));
            Assert.That(retried.CloseQueued, Is.True);
            Assert.That(close.Payload, Is.Empty);
        });
    }

    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void RemoteEchoResumesAfterExistingOutputIsDrained(
        WebSocketRole role)
    {
        WebSocketConnection connection = Connection(role, 20);
        int pingPayloadCount = role == WebSocketRole.Client ? 14 : 18;
        connection.SendConnectionPing(
            new byte[pingPayloadCount],
            0,
            pingPayloadCount);
        var prefix = new byte[1];
        connection.WriteConnectionBytes(prefix);
        byte[] remote = InboundFrame(
            role,
            WebSocketOpcode.Close,
            true,
            []);

        WebSocketInboundFramePumpResult blocked =
            connection.ReceiveConnectionBytes(remote);
        byte[] priorOutput = DrainAll(connection);
        WebSocketInboundFramePumpResult resumed =
            connection.ReceiveConnectionBytes(ReadOnlySpan<byte>.Empty);
        WebSocketEvent? closeEvent = connection.ReadNextEvent();
        DecodedConnectionClose echo = DecodeClose(role, DrainAll(connection));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(remote.Length));
            Assert.That(blocked.FrameCount, Is.Zero);
            Assert.That(priorOutput, Has.Length.EqualTo(19));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.FrameCount, Is.EqualTo(1));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Type, Is.EqualTo(WebSocketEventType.Close));
            Assert.That(echo.Payload, Is.Empty);
        });
    }

    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void CloseDuringFragmentationCancelsAndSuppressesApplicationData(
        WebSocketRole role)
    {
        WebSocketConnection outbound = Connection(
            role,
            128,
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketParseStatus firstSend = outbound.SendMessageChunk(
            Chunk(true, false, [1, 2]));
        WebSocketLocalCloseResult localClose =
            outbound.StartConnectionClose(new WebSocketCloseInfo());
        byte[] outboundWire = DrainAll(outbound);
        WebSocketParseStatus laterSend = outbound.SendMessageChunk(
            Chunk(false, true, [3]));

        WebSocketConnection inbound = Connection(
            role,
            128,
            WebSocketMessageDeliveryMode.Reassembled);
        byte[] inboundWire = Combine(
            InboundFrame(role, WebSocketOpcode.Binary, false, [1, 2]),
            InboundFrame(role, WebSocketOpcode.Close, true, []),
            InboundFrame(role, WebSocketOpcode.Continuation, true, [3]),
            InboundFrame(role, WebSocketOpcode.Text, true, [4]));
        WebSocketInboundFramePumpResult received =
            inbound.ReceiveConnectionBytes(inboundWire);
        WebSocketEvent? closeEvent = inbound.ReadNextEvent();
        WebSocketEvent? applicationEvent = inbound.ReadNextEvent();
        WebSocketConnectionInfo information = inbound.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(firstSend,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(localClose.CloseQueued, Is.True);
            Assert.That(DecodeClose(role, outboundWire).Payload, Is.Empty);
            Assert.That(laterSend,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(4));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Type, Is.EqualTo(WebSocketEventType.Close));
            Assert.That(applicationEvent, Is.Null);
            Assert.That(information.ReceivedMessageCount, Is.Zero);
            Assert.That(information.HasError, Is.False);
        });
    }

    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void PingAfterRemoteCloseIsIgnored(WebSocketRole role)
    {
        WebSocketConnection connection = Connection(role);
        byte[] wire = Combine(
            InboundFrame(role, WebSocketOpcode.Close, true, []),
            InboundFrame(role, WebSocketOpcode.Ping, true, [4, 5]));

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? closeEvent = connection.ReadNextEvent();
        WebSocketEvent? pingEvent = connection.ReadNextEvent();
        byte[] output = DrainAll(connection);

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(2));
            Assert.That(closeEvent, Is.Not.Null);
            Assert.That(closeEvent!.Type, Is.EqualTo(WebSocketEventType.Close));
            Assert.That(pingEvent, Is.Null);
            Assert.That(output, Has.Length.EqualTo(
                role == WebSocketRole.Client ? 6 : 2));
            Assert.That(DecodeClose(role, output).Payload, Is.Empty);
        });
    }

    static WebSocketConnection Connection(
        WebSocketRole role,
        int outputCapacity = 128,
        WebSocketMessageDeliveryMode deliveryMode =
            WebSocketMessageDeliveryMode.Reassembled)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = 125,
            MaximumMessagePayloadLength = 256,
            MaximumDecompressedMessagePayloadLength = 256,
            MaximumFragmentCount = 256,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = outputCapacity,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = outputCapacity < 78
                ? outputCapacity - 14
                : 64,
            AutomaticCloseResponse = true,
            CloseTimeoutMilliseconds = 1000
        };
        if (role == WebSocketRole.Client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = new ConnectionCloseMaskKeySource();
        }

        return new WebSocketConnection(options, Handshake());
    }

    static WebSocketHandshakeResult Handshake()
    {
        return new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
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

    static WebSocketSendChunk Chunk(bool first, bool final, byte[] payload)
    {
        return new WebSocketSendChunk
        {
            First = first,
            Final = final,
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] InboundFrame(
        WebSocketRole receiverRole,
        WebSocketOpcode opcode,
        bool final,
        byte[] payload)
    {
        bool masked = receiverRole == WebSocketRole.Server;
        int headerLength = masked ? 6 : 2;
        byte[] frame = new byte[headerLength + payload.Length];
        frame[0] = (byte)((final ? 0x80 : 0) | (int)opcode);
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

    static byte[] Combine(params byte[][] values)
    {
        byte[] combined = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            value.CopyTo(combined, offset);
            offset += value.Length;
        }

        return combined;
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static DecodedConnectionClose DecodeClose(
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
        byte[] payload = frame.Payload
            .AsSpan(frame.PayloadOffset, frame.PayloadCount)
            .ToArray();
        var decoded = new DecodedConnectionClose
        {
            Masked = frame.Header.Masked,
            Payload = payload
        };
        if (payload.Length >= 2)
        {
            decoded.CloseCode = (payload[0] << 8) | payload[1];
            decoded.Reason = System.Text.Encoding.UTF8.GetString(
                payload.AsSpan(2));
        }

        Assert.That(frame.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        Assert.That(frame.Header.Opcode, Is.EqualTo(WebSocketOpcode.Close));
        Assert.That(frame.ConsumedCount, Is.EqualTo(wire.Length));
        return decoded;
    }
}

internal class DecodedConnectionClose
{
    internal bool Masked;
    internal byte[] Payload = [];
    internal int CloseCode;
    internal string Reason = "";
}

internal class ConnectionCloseMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        return key;
    }
}
