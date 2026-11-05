using websocket;

namespace websocket.Tests;

public class WebSocketConnectionLimitBackpressureTests
{
    [TestCase("frame", -1)]
    [TestCase("frame", 0)]
    [TestCase("frame", 1)]
    [TestCase("message", -1)]
    [TestCase("message", 0)]
    [TestCase("message", 1)]
    [TestCase("fragments", -1)]
    [TestCase("fragments", 0)]
    [TestCase("fragments", 1)]
    public void ReceiveLimitsRejectOnlyAboveTheirExactBoundary(
        string limit,
        int delta)
    {
        WebSocketConnectionOptions options = Options();
        byte[] wire;
        string identifier;
        if (limit == "frame")
        {
            options.MaximumFramePayloadLength = 4;
            options.AutomaticFramePayloadLength = 4;
            wire = Frame(
                WebSocketOpcode.Binary,
                true,
                new byte[4 + delta]);
            identifier = "frame-payload-too-large";
        }
        else if (limit == "message")
        {
            options.MaximumFramePayloadLength = 4;
            options.MaximumMessagePayloadLength = 6;
            options.AutomaticFramePayloadLength = 3;
            wire = FragmentedMessage(6 + delta, 4);
            identifier = "stream-message-payload-limit-exceeded";
        }
        else
        {
            options.MaximumFramePayloadLength = 4;
            options.MaximumMessagePayloadLength = 8;
            options.MaximumFragmentCount = 2;
            options.AutomaticFramePayloadLength = 4;
            wire = MessageWithFragmentCount(2 + delta);
            identifier = "message-fragment-limit-exceeded";
        }

        WebSocketConnection connection = Connection(options);
        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? firstEvent = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            if (delta <= 0)
            {
                Assert.That(result.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(result.ConsumedCount, Is.EqualTo(wire.Length));
                Assert.That(firstEvent, Is.Not.Null);
                Assert.That(firstEvent!.Type,
                    Is.EqualTo(WebSocketEventType.MessageData));
                Assert.That(connection.GetConnectionInfo().State,
                    Is.EqualTo(WebSocketConnectionState.Open));
            }
            else
            {
                Assert.That(result.Status,
                    Is.EqualTo(WebSocketParseStatus.LimitRejected));
                Assert.That(result.Error.Kind,
                    Is.EqualTo(WebSocketErrorKind.Limit));
                Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
                Assert.That(result.Error.CloseCode,
                    Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
                Assert.That(firstEvent, Is.Not.Null);
                Assert.That(firstEvent!.Type,
                    Is.EqualTo(WebSocketEventType.Error));
            }
        });
    }

    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void BufferedInputLimitRetainsAtMostItsExactCapacity(int count)
    {
        WebSocketConnectionOptions options = Options();
        options.MaximumFramePayloadLength = 32;
        options.MaximumMessagePayloadLength = 32;
        options.MaximumDecompressedMessagePayloadLength = 32;
        options.MaximumBufferedInputBytes = 14;
        options.AutomaticFramePayloadLength = 16;
        WebSocketConnection connection = Connection(options);
        byte[] wire = Frame(
            WebSocketOpcode.Binary,
            true,
            Enumerable.Range(0, count).Select(value => (byte)value).ToArray());

        WebSocketInboundFramePumpResult result =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? connectionEvent = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            if (count <= 14)
            {
                Assert.That(result.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(connectionEvent, Is.Not.Null);
                Assert.That(EventPayload(connectionEvent!),
                    Has.Length.EqualTo(count));
                Assert.That(connection.GetConnectionInfo().State,
                    Is.EqualTo(WebSocketConnectionState.Open));
            }
            else
            {
                Assert.That(result.Status,
                    Is.EqualTo(WebSocketParseStatus.LimitRejected));
                Assert.That(result.Error.Identifier,
                    Is.EqualTo("reassembly-input-buffer-limit-exceeded"));
                Assert.That(connectionEvent, Is.Not.Null);
                Assert.That(connectionEvent!.Type,
                    Is.EqualTo(WebSocketEventType.Error));
            }
        });
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void DecompressedMessageLimitRejectsOnlyExpandedOutputAboveBoundary(
        int count)
    {
        CompressedLimitPair pair = CompressedPair(4);
        byte[] payload = Enumerable.Repeat((byte)65, count).ToArray();
        WebSocketSendRequest request = Request(payload);
        request.Compress = true;

        WebSocketParseStatus sent = pair.Sender.SendWholeMessage(request);
        byte[] wire = DrainAll(pair.Sender);
        WebSocketInboundFramePumpResult received =
            pair.Receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? connectionEvent = pair.Receiver.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.EqualTo(WebSocketParseStatus.Complete));
            if (count <= 4)
            {
                Assert.That(received.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(connectionEvent, Is.Not.Null);
                Assert.That(EventPayload(connectionEvent!), Is.EqualTo(payload));
            }
            else
            {
                Assert.That(received.Status,
                    Is.EqualTo(WebSocketParseStatus.LimitRejected));
                Assert.That(received.Error.Kind,
                    Is.EqualTo(WebSocketErrorKind.Limit));
                Assert.That(received.Error.CloseCode,
                    Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
                Assert.That(connectionEvent, Is.Not.Null);
                Assert.That(connectionEvent!.Type,
                    Is.EqualTo(WebSocketEventType.Error));
            }
        });
    }

    [Test]
    public void BufferedOutputBelowEqualAndAboveCapacityDrainsWithoutDataLoss()
    {
        WebSocketConnectionOptions options = Options();
        options.Role = WebSocketRole.Server;
        options.MaximumFramePayloadLength = 12;
        options.MaximumMessagePayloadLength = 36;
        options.MaximumDecompressedMessagePayloadLength = 36;
        options.MaximumFragmentCount = 3;
        options.MaximumBufferedOutputBytes = 28;
        options.AutomaticFramePayloadLength = 12;

        WebSocketConnection below = Connection(options);
        WebSocketParseStatus belowStatus =
            below.SendWholeMessage(Request(new byte[11]));
        int belowPending = below.GetPendingConnectionWriteCount();

        WebSocketConnection equal = Connection(options);
        WebSocketParseStatus equalStatus =
            equal.SendWholeMessage(Request(new byte[24]));
        int equalPending = equal.GetPendingConnectionWriteCount();

        WebSocketConnection above = Connection(options);
        byte[] payload = Enumerable.Range(0, 36)
            .Select(value => (byte)value)
            .ToArray();
        WebSocketSendRequest aboveRequest = Request(payload);
        WebSocketParseStatus blocked = above.SendWholeMessage(aboveRequest);
        byte[] prefix = DrainAll(above);
        WebSocketParseStatus resumed = above.SendWholeMessage(aboveRequest);
        byte[] suffix = DrainAll(above);
        byte[] wire = Combine(prefix, suffix);
        WebSocketConnectionOptions receiverOptions = Options();
        receiverOptions.MaximumFramePayloadLength = 12;
        receiverOptions.MaximumMessagePayloadLength = 36;
        receiverOptions.MaximumDecompressedMessagePayloadLength = 36;
        receiverOptions.MaximumFragmentCount = 3;
        receiverOptions.AutomaticFramePayloadLength = 12;
        WebSocketConnection receiver = Connection(receiverOptions);
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? message = receiver.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(belowStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(belowPending, Is.EqualTo(13));
            Assert.That(equalStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(equalPending, Is.EqualTo(28));
            Assert.That(blocked,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(prefix, Has.Length.EqualTo(28));
            Assert.That(resumed,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(suffix, Has.Length.EqualTo(14));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(3));
            Assert.That(message, Is.Not.Null);
            Assert.That(EventPayload(message!), Is.EqualTo(payload));
            Assert.That(above.GetConnectionInfo().SentMessageCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void EventCapacityDrainsAndResumesEveryMessageWithoutLoss()
    {
        WebSocketConnectionOptions options = Options();
        options.MaximumQueuedEvents = 1;
        WebSocketConnection connection = Connection(options);
        byte[] wire = Combine(
            Frame(WebSocketOpcode.Binary, true, [1]),
            Frame(WebSocketOpcode.Binary, true, [2]),
            Frame(WebSocketOpcode.Binary, true, [3]));

        WebSocketInboundFramePumpResult first =
            connection.ReceiveConnectionBytes(wire);
        WebSocketEvent? firstEvent = connection.ReadNextEvent();
        WebSocketInboundFramePumpResult second =
            connection.ReceiveConnectionBytes(
                wire.AsSpan(first.ConsumedCount));
        WebSocketEvent? secondEvent = connection.ReadNextEvent();
        WebSocketInboundFramePumpResult third =
            connection.ReceiveConnectionBytes(
                wire.AsSpan(first.ConsumedCount + second.ConsumedCount));
        WebSocketEvent? thirdEvent = connection.ReadNextEvent();
        WebSocketConnectionInfo information = connection.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(first.FrameCount, Is.EqualTo(1));
            Assert.That(first.ConsumedCount, Is.EqualTo(5));
            Assert.That(second.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(second.FrameCount, Is.EqualTo(1));
            Assert.That(second.ConsumedCount, Is.EqualTo(3));
            Assert.That(third.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(third.FrameCount, Is.EqualTo(1));
            Assert.That(third.ConsumedCount, Is.EqualTo(1));
            Assert.That(EventPayload(firstEvent!), Is.EqualTo(new byte[] { 1 }));
            Assert.That(EventPayload(secondEvent!), Is.EqualTo(new byte[] { 2 }));
            Assert.That(EventPayload(thirdEvent!), Is.EqualTo(new byte[] { 3 }));
            Assert.That(information.ReceivedFrameCount, Is.EqualTo(3));
            Assert.That(information.ReceivedMessageCount, Is.EqualTo(3));
            Assert.That(information.PendingEventCount, Is.Zero);
        });
    }

    [Test]
    public void AutomaticControlsSurviveCombinedOutputAndEventBackpressure()
    {
        WebSocketConnectionOptions options = Options();
        options.Role = WebSocketRole.Client;
        options.MaximumBufferedOutputBytes = 20;
        options.AutomaticFramePayloadLength = 6;
        options.MaximumQueuedEvents = 1;
        options.AutomaticPong = true;
        options.HasMaskKeySource = true;
        options.MaskKeySource = new LimitMaskKeySource();
        WebSocketConnection connection = Connection(options);
        connection.SendConnectionPing(new byte[14], 0, 14);
        var priorPrefix = new byte[1];
        connection.WriteConnectionBytes(priorPrefix);
        byte[] wire = Combine(
            Frame(WebSocketOpcode.Ping, true, [1]),
            Frame(WebSocketOpcode.Ping, true, [2]));

        WebSocketInboundFramePumpResult outputBlocked =
            connection.ReceiveConnectionBytes(wire);
        byte[] priorSuffix = DrainAll(connection);
        WebSocketInboundFramePumpResult eventBlocked =
            connection.ReceiveConnectionBytes(
                wire.AsSpan(outputBlocked.ConsumedCount));
        WebSocketEvent? firstPing = connection.ReadNextEvent();
        byte[] firstPong = DrainAll(connection);
        int consumed = outputBlocked.ConsumedCount +
            eventBlocked.ConsumedCount;
        WebSocketInboundFramePumpResult completed =
            connection.ReceiveConnectionBytes(wire.AsSpan(consumed));
        WebSocketEvent? secondPing = connection.ReadNextEvent();
        byte[] secondPong = DrainAll(connection);

        Assert.Multiple(() =>
        {
            Assert.That(outputBlocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(outputBlocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(priorSuffix, Has.Length.EqualTo(19));
            Assert.That(eventBlocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(eventBlocked.FrameCount, Is.EqualTo(1));
            Assert.That(eventBlocked.ConsumedCount, Is.EqualTo(3));
            Assert.That(EventPayload(firstPing!), Is.EqualTo(new byte[] { 1 }));
            Assert.That(ControlPayload(
                WebSocketRole.Client,
                firstPong,
                WebSocketOpcode.Pong), Is.EqualTo(new byte[] { 1 }));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.FrameCount, Is.EqualTo(1));
            Assert.That(completed.ConsumedCount, Is.EqualTo(1));
            Assert.That(EventPayload(secondPing!), Is.EqualTo(new byte[] { 2 }));
            Assert.That(ControlPayload(
                WebSocketRole.Client,
                secondPong,
                WebSocketOpcode.Pong), Is.EqualTo(new byte[] { 2 }));
            Assert.That(connection.GetConnectionInfo().ReceivedFrameCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void OutboundFrameMessageAndFragmentLimitsAllowCorrectedRetries()
    {
        WebSocketConnectionOptions frameOptions = Options();
        frameOptions.MaximumFramePayloadLength = 4;
        frameOptions.AutomaticFramePayloadLength = 4;
        WebSocketConnection frameConnection = Connection(frameOptions);
        WebSocketSendRequest invalidFrame = Request([1]);
        invalidFrame.HasFramePayloadLength = true;
        invalidFrame.FramePayloadLength = 5;
        WebSocketSendRequest validFrame = Request([1, 2, 3, 4]);
        validFrame.HasFramePayloadLength = true;
        validFrame.FramePayloadLength = 4;

        WebSocketParseStatus frameRejected =
            frameConnection.SendWholeMessage(invalidFrame);
        WebSocketParseStatus frameAccepted =
            frameConnection.SendWholeMessage(validFrame);

        WebSocketConnectionOptions messageOptions = Options();
        messageOptions.MaximumFramePayloadLength = 4;
        messageOptions.MaximumMessagePayloadLength = 8;
        messageOptions.AutomaticFramePayloadLength = 4;
        WebSocketConnection messageConnection = Connection(messageOptions);
        WebSocketParseStatus messageRejected =
            messageConnection.SendWholeMessage(Request(new byte[9]));
        WebSocketParseStatus messageAccepted =
            messageConnection.SendWholeMessage(Request(new byte[8]));

        WebSocketConnectionOptions fragmentOptions = Options();
        fragmentOptions.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        fragmentOptions.MaximumFramePayloadLength = 4;
        fragmentOptions.MaximumMessagePayloadLength = 8;
        fragmentOptions.MaximumFragmentCount = 2;
        fragmentOptions.AutomaticFramePayloadLength = 4;
        WebSocketConnection fragmentConnection = Connection(fragmentOptions);
        WebSocketParseStatus first = fragmentConnection.SendMessageChunk(
            Chunk(true, false, [1]));
        WebSocketParseStatus impossibleSecond =
            fragmentConnection.SendMessageChunk(Chunk(false, false, [2]));
        WebSocketParseStatus correctedSecond =
            fragmentConnection.SendMessageChunk(Chunk(false, true, [2]));

        Assert.Multiple(() =>
        {
            Assert.That(frameRejected,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(frameAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(messageRejected,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(messageAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(impossibleSecond,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(correctedSecond,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(frameConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(messageConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(fragmentConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    static WebSocketConnectionOptions Options()
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            MaximumFramePayloadLength = 16,
            MaximumMessagePayloadLength = 32,
            MaximumDecompressedMessagePayloadLength = 32,
            MaximumFragmentCount = 8,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 64,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 8,
            CloseTimeoutMilliseconds = 1000
        };
    }

    static WebSocketConnection Connection(WebSocketConnectionOptions options)
    {
        return new WebSocketConnection(
            options,
            new WebSocketHandshakeResult
            {
                Outcome = WebSocketHandshakeOutcome.Accepted,
                Transport = options.Transport
            });
    }

    static CompressedLimitPair CompressedPair(int decompressedLimit)
    {
        var senderCompression = new PerMessageDeflateOptions
        {
            CompressionThreshold = 0,
            MaximumDecompressedMessagePayloadLength = 64
        };
        var receiverCompression = new PerMessageDeflateOptions
        {
            CompressionThreshold = 0,
            MaximumDecompressedMessagePayloadLength = 64
        };
        var senderExtension = new PerMessageDeflateExtension(senderCompression);
        var receiverExtension = new PerMessageDeflateExtension(receiverCompression);
        WebSocketExtensionOffer offer =
            senderExtension.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation accepted =
            receiverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            senderExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);

        WebSocketConnectionOptions senderOptions = Options();
        senderOptions.Extensions = [senderExtension];
        senderOptions.NegotiatedExtensions = [validated];
        senderOptions.HasPerMessageDeflate = true;
        senderOptions.PerMessageDeflate = senderCompression;
        senderOptions.MaximumMessagePayloadLength = 64;
        senderOptions.MaximumDecompressedMessagePayloadLength = 64;
        senderOptions.HasMaskKeySource = true;
        senderOptions.MaskKeySource = new LimitMaskKeySource();

        WebSocketConnectionOptions receiverOptions = Options();
        receiverOptions.Role = WebSocketRole.Server;
        receiverOptions.Extensions = [receiverExtension];
        receiverOptions.NegotiatedExtensions = [accepted];
        receiverOptions.HasPerMessageDeflate = true;
        receiverOptions.PerMessageDeflate = receiverCompression;
        receiverOptions.MaximumMessagePayloadLength = 64;
        receiverOptions.MaximumDecompressedMessagePayloadLength =
            decompressedLimit;

        return new CompressedLimitPair
        {
            Sender = new WebSocketConnection(
                senderOptions,
                Handshake([validated])),
            Receiver = new WebSocketConnection(
                receiverOptions,
                Handshake([accepted]))
        };
    }

    static WebSocketHandshakeResult Handshake(
        WebSocketExtensionNegotiation[] extensions)
    {
        return new WebSocketHandshakeResult
        {
            Outcome = WebSocketHandshakeOutcome.Accepted,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Extensions = extensions
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
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] FragmentedMessage(int payloadCount, int firstCount)
    {
        byte[] payload = Enumerable.Range(0, payloadCount)
            .Select(value => (byte)value)
            .ToArray();
        return Combine(
            Frame(
                WebSocketOpcode.Binary,
                false,
                payload.AsSpan(0, firstCount).ToArray()),
            Frame(
                WebSocketOpcode.Continuation,
                true,
                payload.AsSpan(firstCount).ToArray()));
    }

    static byte[] MessageWithFragmentCount(int count)
    {
        var frames = new List<byte[]>();
        int index = 0;
        while (index < count)
        {
            WebSocketOpcode opcode = index == 0
                ? WebSocketOpcode.Binary
                : WebSocketOpcode.Continuation;
            frames.Add(Frame(opcode, index == count - 1, [(byte)index]));
            index++;
        }

        return Combine(frames.ToArray());
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
        byte[] output = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            value.CopyTo(output, offset);
            offset += value.Length;
        }

        return output;
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static byte[] EventPayload(WebSocketEvent connectionEvent)
    {
        return connectionEvent.Payload
            .AsSpan(
                connectionEvent.PayloadOffset,
                connectionEvent.PayloadCount)
            .ToArray();
    }

    static byte[] ControlPayload(
        WebSocketRole senderRole,
        byte[] wire,
        WebSocketOpcode opcode)
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
        Assert.That(frame.Header.Opcode, Is.EqualTo(opcode));
        Assert.That(frame.ConsumedCount, Is.EqualTo(wire.Length));
        return frame.Payload
            .AsSpan(frame.PayloadOffset, frame.PayloadCount)
            .ToArray();
    }
}

internal class CompressedLimitPair
{
    internal WebSocketConnection Sender = null!;
    internal WebSocketConnection Receiver = null!;
}

internal class LimitMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x11111111U;
        return key;
    }
}
