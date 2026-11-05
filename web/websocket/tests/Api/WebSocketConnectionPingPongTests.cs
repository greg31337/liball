using websocket;

namespace websocket.Tests;

public class WebSocketConnectionPingPongTests
{
    [TestCase(WebSocketRole.Client)]
    [TestCase(WebSocketRole.Server)]
    public void ExplicitPingAndPongReachThePeerInCallOrder(
        WebSocketRole senderRole)
    {
        var keys = new PingPongMaskKeySource();
        WebSocketConnection sender = Connection(
            senderRole,
            false,
            256,
            125,
            keys);
        WebSocketConnection receiver = Connection(
            OtherRole(senderRole),
            false,
            256,
            125,
            null);

        WebSocketControlSendResult ping = sender.SendConnectionPing(
            [0, 1, 2, 3],
            1,
            2);
        WebSocketControlSendResult pong = sender.SendConnectionPong(
            [],
            0,
            0);
        byte[] wire = DrainAll(sender);
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? pingEvent = receiver.ReadNextEvent();
        WebSocketEvent? pongEvent = receiver.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(ping.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping.Accepted, Is.True);
            Assert.That(pong.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(2));
            Assert.That(pingEvent, Is.Not.Null);
            Assert.That(pingEvent!.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(EventPayload(pingEvent), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(pongEvent, Is.Not.Null);
            Assert.That(pongEvent!.Type, Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(EventPayload(pongEvent), Is.Empty);
            Assert.That(keys.CallCount,
                Is.EqualTo(senderRole == WebSocketRole.Client ? 2 : 0));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ReceivedPingIsSurfacedAndAutomaticPongHonorsConfiguration(
        bool automaticPong)
    {
        var keys = new PingPongMaskKeySource();
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            automaticPong,
            256,
            125,
            keys);

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes([0x89, 0x03, 9, 8, 7]);
        WebSocketEvent? ping = connection.ReadNextEvent();
        byte[] output = DrainAll(connection);

        Assert.Multiple(() =>
        {
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping, Is.Not.Null);
            Assert.That(ping!.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(EventPayload(ping), Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(output.Length,
                Is.EqualTo(automaticPong ? 9 : 0));
            Assert.That(keys.CallCount, Is.EqualTo(automaticPong ? 1 : 0));
        });

        if (automaticPong)
        {
            WebSocketConnection peer = Connection(
                WebSocketRole.Server,
                false,
                256,
                125,
                null);
            WebSocketInboundFramePumpResult pongReceived =
                peer.ReceiveConnectionBytes(output);
            WebSocketEvent? pong = peer.ReadNextEvent();
            Assert.Multiple(() =>
            {
                Assert.That(pongReceived.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(pong, Is.Not.Null);
                Assert.That(pong!.Type, Is.EqualTo(WebSocketEventType.Pong));
                Assert.That(EventPayload(pong),
                    Is.EqualTo(new byte[] { 9, 8, 7 }));
            });
        }
    }

    [Test]
    public void PingStaysInterleavedBetweenStreamingMessageFragments()
    {
        WebSocketConnection sender = Connection(
            WebSocketRole.Server,
            false,
            128,
            16,
            null);
        WebSocketConnection receiver = Connection(
            WebSocketRole.Client,
            false,
            128,
            16,
            null,
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketSendChunk first = TextChunk(true, false, [65]);
        WebSocketSendChunk final = TextChunk(false, true, [66]);

        WebSocketParseStatus firstStatus = sender.SendMessageChunk(first);
        byte[] prefix = DrainCount(sender, 1);
        WebSocketControlSendResult pingStatus = sender.SendConnectionPing(
            [9],
            0,
            1);
        WebSocketParseStatus finalStatus = sender.SendMessageChunk(final);
        byte[] wire = Combine(prefix, DrainAll(sender));
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        List<WebSocketEvent> events = DrainEvents(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(firstStatus, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(pingStatus.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finalStatus, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(3));
            Assert.That(events.Select(item => item.Type), Is.EqualTo(new[]
            {
                WebSocketEventType.MessageStart,
                WebSocketEventType.MessageData,
                WebSocketEventType.Ping,
                WebSocketEventType.MessageData,
                WebSocketEventType.MessageEnd
            }));
            Assert.That(EventPayload(events[1]), Is.EqualTo(new byte[] { 65 }));
            Assert.That(EventPayload(events[2]), Is.EqualTo(new byte[] { 9 }));
            Assert.That(EventPayload(events[3]), Is.EqualTo(new byte[] { 66 }));
        });
    }

    [Test]
    public void MatchingPongAcknowledgesOnceWithLatencyWhileOthersStayUnsolicited()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            false,
            128,
            16,
            new PingPongMaskKeySource());
        byte[] token = [7, 8];

        WebSocketParseStatus initialTime = connection.AdvanceConnectionTime(100);
        WebSocketControlSendResult ping = connection.SendConnectionPing(
            token,
            0,
            token.Length);
        WebSocketControlSendResult duplicate = connection.SendConnectionPing(
            token,
            0,
            token.Length);
        DrainAll(connection);
        WebSocketParseStatus mismatchTime = connection.AdvanceConnectionTime(150);
        WebSocketInboundFramePumpResult mismatched =
            connection.ReceiveConnectionBytes([0x8a, 0x01, 9]);
        WebSocketEvent? unsolicited = connection.ReadNextEvent();
        bool retained = connection.PingPongHandler.ContainsTrackedPingToken(
            token,
            0,
            token.Length);
        WebSocketParseStatus matchTime = connection.AdvanceConnectionTime(175);
        WebSocketInboundFramePumpResult matched =
            connection.ReceiveConnectionBytes([0x8a, 0x02, 7, 8]);
        WebSocketEvent? acknowledged = connection.ReadNextEvent();
        WebSocketControlSendResult reused = connection.SendConnectionPing(
            token,
            0,
            token.Length);

        Assert.Multiple(() =>
        {
            Assert.That(initialTime, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(duplicate.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("ping-token-duplicate"));
            Assert.That(mismatchTime, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(mismatched.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(unsolicited, Is.Not.Null);
            Assert.That(unsolicited!.Type, Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(EventPayload(unsolicited), Is.EqualTo(new byte[] { 9 }));
            Assert.That(retained, Is.True);
            Assert.That(matchTime, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(matched.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(acknowledged, Is.Not.Null);
            Assert.That(acknowledged!.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(EventPayload(acknowledged), Is.EqualTo(token));
            Assert.That(acknowledged.HasPingElapsedMilliseconds, Is.True);
            Assert.That(acknowledged.PingElapsedMilliseconds, Is.EqualTo(75));
            Assert.That(connection.PingPongHandler.ContainsTrackedPingToken(
                    token,
                    0,
                    token.Length),
                Is.True);
            Assert.That(reused.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void ExplicitControlPayloadAndOutputLimitsRejectWithoutStalling()
    {
        WebSocketConnection roomy = Connection(
            WebSocketRole.Client,
            false,
            256,
            125,
            new PingPongMaskKeySource());
        byte[] maximumPayload = new byte[125];
        WebSocketControlSendResult maximum = roomy.SendConnectionPing(
            maximumPayload,
            0,
            maximumPayload.Length);
        WebSocketControlSendResult oversized = roomy.SendConnectionPong(
            new byte[126],
            0,
            126);

        WebSocketConnection bounded = Connection(
            WebSocketRole.Client,
            false,
            128,
            125,
            new PingPongMaskKeySource());
        WebSocketControlSendResult cannotFit = bounded.SendConnectionPing(
            maximumPayload,
            0,
            maximumPayload.Length);
        WebSocketControlSendResult recovered = bounded.SendConnectionPing(
            [1],
            0,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(maximum.EncodedCount, Is.EqualTo(131));
            Assert.That(oversized.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(oversized.Error.Identifier,
                Is.EqualTo("control-send-payload-limit-exceeded"));
            Assert.That(cannotFit.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(cannotFit.Error.Identifier,
                Is.EqualTo("control-send-buffer-limit-exceeded"));
            Assert.That(bounded.GetPendingConnectionWriteCount(), Is.EqualTo(7));
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(bounded.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    [Test]
    public void ExplicitAndAutomaticPongBackpressureResumesAfterDrain()
    {
        WebSocketConnection explicitConnection = Connection(
            WebSocketRole.Client,
            false,
            18,
            4,
            new PingPongMaskKeySource());
        WebSocketControlSendResult first = explicitConnection.SendConnectionPing(
            [1, 2, 3, 4],
            0,
            4);
        WebSocketControlSendResult blocked = explicitConnection.SendConnectionPong(
            [5, 6, 7],
            0,
            3);
        DrainAll(explicitConnection);
        WebSocketControlSendResult retried = explicitConnection.SendConnectionPong(
            [5, 6, 7],
            0,
            3);

        WebSocketConnection automaticConnection = Connection(
            WebSocketRole.Client,
            true,
            18,
            4,
            new PingPongMaskKeySource());
        WebSocketParseStatus data = automaticConnection.SendWholeMessage(
            new WebSocketSendRequest
            {
                MessageType = WebSocketMessageType.Binary,
                Payload = [0, 1, 2, 3],
                PayloadCount = 4
            });
        byte[] pingWire = [0x89, 0x03, 9, 8, 7];
        WebSocketInboundFramePumpResult autoBlocked =
            automaticConnection.ReceiveConnectionBytes(pingWire);
        DrainAll(automaticConnection);
        WebSocketInboundFramePumpResult autoResumed =
            automaticConnection.ReceiveConnectionBytes(
                pingWire.AsSpan(autoBlocked.ConsumedCount));
        WebSocketEvent? ping = automaticConnection.ReadNextEvent();
        byte[] automaticPong = DrainAll(automaticConnection);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(retried.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(data, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(autoBlocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(autoBlocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(autoResumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(autoResumed.ConsumedCount, Is.EqualTo(3));
            Assert.That(ping, Is.Not.Null);
            Assert.That(ping!.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(automaticPong, Has.Length.EqualTo(9));
        });
    }

    [Test]
    public void AutomaticPongLargerThanTotalOutputCapacityFailsAsALimit()
    {
        WebSocketConnection connection = Connection(
            WebSocketRole.Client,
            true,
            128,
            125,
            new PingPongMaskKeySource());
        byte[] wire = new byte[127];
        wire[0] = 0x89;
        wire[1] = 125;

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? error = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("control-send-buffer-limit-exceeded"));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(connection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
        });
    }

    static WebSocketConnection Connection(
        WebSocketRole role,
        bool automaticPong,
        int outputCapacity,
        int maximumFramePayloadLength,
        IWebSocketMaskKeySource? maskKeySource,
        WebSocketMessageDeliveryMode deliveryMode =
            WebSocketMessageDeliveryMode.Reassembled)
    {
        int automaticFramePayloadLength = maximumFramePayloadLength;
        int scheduledLimit = outputCapacity -
            WebSocketProtocolConstants.MaximumFrameHeaderLength;
        if (automaticFramePayloadLength > scheduledLimit)
        {
            automaticFramePayloadLength = scheduledLimit;
        }

        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = maximumFramePayloadLength,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            MaximumFragmentCount = 32,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = outputCapacity,
            MaximumQueuedEvents = 32,
            AutomaticFramePayloadLength = automaticFramePayloadLength,
            AutomaticPong = automaticPong,
            CloseTimeoutMilliseconds = 1000
        };
        if (role == WebSocketRole.Client && maskKeySource != null)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = maskKeySource;
        }

        return new WebSocketConnection(
            options,
            new WebSocketHandshakeResult
            {
                Outcome = WebSocketHandshakeOutcome.Accepted,
                Transport = WebSocketHandshakeTransport.Http1Upgrade
            });
    }

    static WebSocketRole OtherRole(WebSocketRole role)
    {
        if (role == WebSocketRole.Client)
        {
            return WebSocketRole.Server;
        }

        return WebSocketRole.Client;
    }

    static WebSocketSendChunk TextChunk(
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

    static byte[] DrainCount(WebSocketConnection connection, int count)
    {
        byte[] output = new byte[count];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(count));
        return output;
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        return DrainCount(
            connection,
            connection.GetPendingConnectionWriteCount());
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

    static byte[] EventPayload(WebSocketEvent item)
    {
        return item.Payload
            .AsSpan(item.PayloadOffset, item.PayloadCount)
            .ToArray();
    }

    static byte[] Combine(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }

        return result;
    }
}

internal class PingPongMaskKeySource : IWebSocketMaskKeySource
{
    internal int CallCount;
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        CallCount++;
        return key;
    }
}
