using websocket;

namespace websocket.Tests;

public class WebSocketConnectionReceiveTests
{
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Text, false)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Text, true)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Binary, false)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Binary, true)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Text, false)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Text, true)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Binary, false)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Binary, true)]
    public void ReceivesTextAndBinaryMessagesWithAndWithoutFragmentation(
        WebSocketMessageDeliveryMode deliveryMode,
        WebSocketMessageType messageType,
        bool fragmented)
    {
        WebSocketConnection connection = Connection(deliveryMode);
        byte[] payload = messageType == WebSocketMessageType.Text
            ? new byte[] { 0x41, 0xe2, 0x82, 0xac, 0x42 }
            : new byte[] { 0x00, 0x7f, 0x80, 0xff, 0x01 };
        byte[] wire = MessageWire(messageType, fragmented, payload);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        List<WebSocketEvent> events = DrainEvents(connection);
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(wire.Length));
            Assert.That(result.FrameCount, Is.EqualTo(fragmented ? 2 : 1));
            Assert.That(result.ChunkCount, Is.EqualTo(fragmented ? 2 : 1));
            Assert.That(information.ReceivedFrameCount,
                Is.EqualTo(fragmented ? 2 : 1));
            Assert.That(information.ReceivedMessageCount, Is.EqualTo(1));
        });
        AssertMessage(events, deliveryMode, messageType, payload, fragmented);
    }

    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Text, false)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Text, true)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Binary, false)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, WebSocketMessageType.Binary, true)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Text, false)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Text, true)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Binary, false)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, WebSocketMessageType.Binary, true)]
    public void ReceivesZeroLengthMessagesInBothDeliveryModes(
        WebSocketMessageDeliveryMode deliveryMode,
        WebSocketMessageType messageType,
        bool fragmented)
    {
        WebSocketConnection connection = Connection(deliveryMode);
        byte[] wire = MessageWire(messageType, fragmented, []);

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        List<WebSocketEvent> events = DrainEvents(connection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(wire.Length));
            Assert.That(result.FrameCount, Is.EqualTo(fragmented ? 2 : 1));
            Assert.That(result.ChunkCount, Is.EqualTo(fragmented ? 2 : 1));
            Assert.That(connection.GetConnectionInfo().ReceivedMessageCount,
                Is.EqualTo(1));
        });
        AssertMessage(events, deliveryMode, messageType, [], fragmented);
    }

    [TestCase(WebSocketMessageDeliveryMode.Reassembled, false)]
    [TestCase(WebSocketMessageDeliveryMode.Reassembled, true)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, false)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming, true)]
    public void RetainsReceiveStateAtEveryTwoCallWireBoundary(
        WebSocketMessageDeliveryMode deliveryMode,
        bool fragmented)
    {
        byte[] payload = [0x41, 0xe2, 0x82, 0xac, 0x42, 0x43];
        byte[] wire = MessageWire(
            WebSocketMessageType.Text,
            fragmented,
            payload);

        int split = 0;
        while (split <= wire.Length)
        {
            WebSocketConnection connection = Connection(deliveryMode);

            WebSocketInboundFramePumpResult first =
                connection.ReceiveConnectionBytes(wire.AsSpan(0, split));
            WebSocketInboundFramePumpResult second =
                connection.ReceiveConnectionBytes(wire.AsSpan(split));
            List<WebSocketEvent> events = DrainEvents(connection);
            WebSocketConnectionInfo information = connection.GetConnectionInfo();

            Assert.Multiple(() =>
            {
                Assert.That(first.HasError, Is.False, "first split " + split);
                Assert.That(second.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete),
                    "second split " + split);
                Assert.That(first.ConsumedCount,
                    Is.EqualTo(split),
                    "first split " + split);
                Assert.That(second.ConsumedCount,
                    Is.EqualTo(wire.Length - split),
                    "second split " + split);
                Assert.That(first.FrameCount + second.FrameCount,
                    Is.EqualTo(fragmented ? 2 : 1),
                    "frame count split " + split);
                Assert.That(information.ReceivedFrameCount,
                    Is.EqualTo(fragmented ? 2 : 1),
                    "snapshot frame count split " + split);
                Assert.That(information.ReceivedMessageCount,
                    Is.EqualTo(1),
                    "message count split " + split);
            });
            AssertMessages(
                events,
                deliveryMode,
                [
                    new ExpectedConnectionMessage(
                        WebSocketMessageType.Text,
                        payload)
                ]);
            split++;
        }
    }

    [TestCase(WebSocketMessageDeliveryMode.Reassembled)]
    [TestCase(WebSocketMessageDeliveryMode.Streaming)]
    public void ConsumesManyFramesAndRetainsOnlyThePartialTrailingFrame(
        WebSocketMessageDeliveryMode deliveryMode)
    {
        WebSocketConnection connection = Connection(deliveryMode);
        byte[] firstMessage = MessageWire(
            WebSocketMessageType.Text,
            true,
            [0x48, 0x69]);
        byte[] secondMessage = MessageWire(
            WebSocketMessageType.Binary,
            false,
            []);
        byte[] thirdMessage = MessageWire(
            WebSocketMessageType.Binary,
            false,
            [7, 8, 9]);
        byte[] initial = Combine(
            firstMessage,
            secondMessage,
            thirdMessage.AsSpan(0, 1).ToArray());

        WebSocketInboundFramePumpResult partial =
            connection.ReceiveConnectionBytes(initial);
        List<WebSocketEvent> initialEvents = DrainEvents(connection);
        WebSocketConnectionInfo partialInformation =
            connection.GetConnectionInfo();
        WebSocketInboundFramePumpResult completed =
            connection.ReceiveConnectionBytes(thirdMessage.AsSpan(1));
        List<WebSocketEvent> finalEvents = DrainEvents(connection);
        WebSocketConnectionInfo completedInformation =
            connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(partial.ConsumedCount, Is.EqualTo(initial.Length));
            Assert.That(partial.FrameCount, Is.EqualTo(3));
            Assert.That(partialInformation.ReceivedFrameCount, Is.EqualTo(3));
            Assert.That(partialInformation.ReceivedMessageCount, Is.EqualTo(2));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.ConsumedCount,
                Is.EqualTo(thirdMessage.Length - 1));
            Assert.That(completed.FrameCount, Is.EqualTo(1));
            Assert.That(completedInformation.ReceivedFrameCount, Is.EqualTo(4));
            Assert.That(completedInformation.ReceivedMessageCount, Is.EqualTo(3));
        });
        AssertMessages(
            initialEvents,
            deliveryMode,
            [
                new ExpectedConnectionMessage(
                    WebSocketMessageType.Text,
                    [0x48, 0x69]),
                new ExpectedConnectionMessage(
                    WebSocketMessageType.Binary,
                    [])
            ]);
        AssertMessages(
            finalEvents,
            deliveryMode,
            [
                new ExpectedConnectionMessage(
                    WebSocketMessageType.Binary,
                    [7, 8, 9])
            ]);
    }

    static WebSocketConnection Connection(
        WebSocketMessageDeliveryMode deliveryMode)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = 32,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumFragmentCount = 8,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = 32,
            AutomaticFramePayloadLength = 32,
            CloseTimeoutMilliseconds = 1000
        };
        var handshake = new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
        return new WebSocketConnection(options, handshake);
    }

    static byte[] MessageWire(
        WebSocketMessageType messageType,
        bool fragmented,
        byte[] payload)
    {
        WebSocketOpcode opcode = messageType == WebSocketMessageType.Text
            ? WebSocketOpcode.Text
            : WebSocketOpcode.Binary;
        if (!fragmented)
        {
            return Frame(opcode, true, payload);
        }

        int firstCount = payload.Length / 2;
        if (payload.Length != 0 && firstCount == 0)
        {
            firstCount = 1;
        }

        return Combine(
            Frame(opcode, false, payload.AsSpan(0, firstCount).ToArray()),
            Frame(
                WebSocketOpcode.Continuation,
                true,
                payload.AsSpan(firstCount).ToArray()));
    }

    static byte[] Frame(
        WebSocketOpcode opcode,
        bool final,
        byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 2];
        frame[0] = (byte)((final ? 0x80 : 0) | (int)opcode);
        frame[1] = (byte)payload.Length;
        payload.CopyTo(frame, 2);
        return frame;
    }

    static byte[] Combine(params byte[][] values)
    {
        int length = values.Sum(value => value.Length);
        byte[] combined = new byte[length];
        int offset = 0;
        foreach (byte[] value in values)
        {
            value.CopyTo(combined, offset);
            offset += value.Length;
        }

        return combined;
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

    static void AssertMessage(
        List<WebSocketEvent> events,
        WebSocketMessageDeliveryMode deliveryMode,
        WebSocketMessageType messageType,
        byte[] payload,
        bool fragmented)
    {
        AssertMessages(
            events,
            deliveryMode,
            [new ExpectedConnectionMessage(messageType, payload)]);
        if (deliveryMode == WebSocketMessageDeliveryMode.Streaming &&
            payload.Length != 0)
        {
            WebSocketEvent[] data = events
                .Where(item => item.Type == WebSocketEventType.MessageData)
                .ToArray();
            Assert.That(data, Has.Length.EqualTo(fragmented ? 2 : 1));
            Assert.That(data[0].First, Is.True);
            Assert.That(data[data.Length - 1].Final, Is.True);
        }
    }

    static void AssertMessages(
        List<WebSocketEvent> events,
        WebSocketMessageDeliveryMode deliveryMode,
        ExpectedConnectionMessage[] expected)
    {
        if (deliveryMode == WebSocketMessageDeliveryMode.Reassembled)
        {
            Assert.That(events, Has.Count.EqualTo(expected.Length));
            int index = 0;
            while (index < expected.Length)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(events[index].Type,
                        Is.EqualTo(WebSocketEventType.MessageData));
                    Assert.That(events[index].MessageType,
                        Is.EqualTo(expected[index].MessageType));
                    Assert.That(EventPayload(events[index]),
                        Is.EqualTo(expected[index].Payload));
                    Assert.That(events[index].First, Is.True);
                    Assert.That(events[index].Final, Is.True);
                });
                index++;
            }

            return;
        }

        int eventIndex = 0;
        int expectedIndex = 0;
        while (expectedIndex < expected.Length)
        {
            Assert.That(events[eventIndex].Type,
                Is.EqualTo(WebSocketEventType.MessageStart));
            Assert.That(events[eventIndex].MessageType,
                Is.EqualTo(expected[expectedIndex].MessageType));
            eventIndex++;
            var payload = new List<byte>();
            while (eventIndex < events.Count &&
                events[eventIndex].Type == WebSocketEventType.MessageData)
            {
                payload.AddRange(EventPayload(events[eventIndex]));
                eventIndex++;
            }

            Assert.That(payload, Is.EqualTo(expected[expectedIndex].Payload));
            Assert.That(events[eventIndex].Type,
                Is.EqualTo(WebSocketEventType.MessageEnd));
            Assert.That(events[eventIndex].MessageType,
                Is.EqualTo(expected[expectedIndex].MessageType));
            eventIndex++;
            expectedIndex++;
        }

        Assert.That(eventIndex, Is.EqualTo(events.Count));
    }

    static byte[] EventPayload(WebSocketEvent item)
    {
        return item.Payload
            .AsSpan(item.PayloadOffset, item.PayloadCount)
            .ToArray();
    }
}

internal class ExpectedConnectionMessage
{
    internal WebSocketMessageType MessageType;
    internal byte[] Payload;

    internal ExpectedConnectionMessage(
        WebSocketMessageType messageType,
        byte[] payload)
    {
        MessageType = messageType;
        Payload = payload;
    }
}
