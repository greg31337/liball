using websocket;

namespace websocket.Tests;

public class WebSocketRandomizedLoopbackTests
{
    [TestCase(
        0x10203040U,
        WebSocketMessageDeliveryMode.Reassembled,
        WebSocketMessageDeliveryMode.Reassembled,
        18,
        19,
        1,
        2,
        1,
        1)]
    [TestCase(
        0x89abcdefU,
        WebSocketMessageDeliveryMode.Streaming,
        WebSocketMessageDeliveryMode.Streaming,
        23,
        21,
        1,
        1,
        3,
        2)]
    [TestCase(
        0x13579bdfU,
        WebSocketMessageDeliveryMode.Streaming,
        WebSocketMessageDeliveryMode.Reassembled,
        32,
        18,
        2,
        1,
        7,
        5)]
    [TestCase(
        0xfedcba98U,
        WebSocketMessageDeliveryMode.Reassembled,
        WebSocketMessageDeliveryMode.Streaming,
        20,
        35,
        1,
        3,
        13,
        8)]
    public void SeededBidirectionalSequencesConvergeWithoutLoss(
        uint seed,
        WebSocketMessageDeliveryMode clientMode,
        WebSocketMessageDeliveryMode serverMode,
        int clientOutputCapacity,
        int serverOutputCapacity,
        int clientEventCapacity,
        int serverEventCapacity,
        int transportCapacity,
        int maximumChunkCount)
    {
        var random = new LoopbackDeterministicRandom(seed);
        var clientMasks = new RandomizedLoopbackMaskKeySource(seed);
        WebSocketConnectionOptions clientOptions = Options(
            WebSocketRole.Client,
            clientMode,
            clientOutputCapacity,
            clientEventCapacity,
            clientMasks);
        WebSocketConnectionOptions serverOptions = Options(
            WebSocketRole.Server,
            serverMode,
            serverOutputCapacity,
            serverEventCapacity,
            null);
        var pair = new RandomizedLoopbackPair
        {
            Client = Connection(clientOptions),
            Server = Connection(serverOptions),
            ClientOptions = clientOptions,
            ServerOptions = serverOptions,
            ClientCollector = new RandomizedLoopbackCollector(clientMode),
            ServerCollector = new RandomizedLoopbackCollector(serverMode)
        };
        var transport = new WebSocketInMemoryLoopbackTransport(
            pair.Client,
            pair.Server,
            transportCapacity,
            maximumChunkCount);
        int expectedClientControlFrameCount = 0;
        int actionIndex = 0;
        while (actionIndex < 36)
        {
            bool fromClient = random.NextBoolean();
            int action = random.NextInt(10);
            if (action < 5)
            {
                SendRandomWholeMessage(
                    pair,
                    transport,
                    random,
                    fromClient,
                    actionIndex,
                    transportCapacity);
            }
            else if (action < 8)
            {
                SendRandomStreamingMessage(
                    pair,
                    transport,
                    random,
                    fromClient,
                    actionIndex,
                    transportCapacity);
            }
            else
            {
                bool ping = action == 8;
                SendRandomControl(
                    pair,
                    transport,
                    random,
                    fromClient,
                    ping,
                    actionIndex,
                    transportCapacity);
                if (fromClient || (ping && !fromClient))
                {
                    expectedClientControlFrameCount++;
                }
            }

            actionIndex++;
        }

        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
        bool clientStartsClose = random.NextBoolean();
        WebSocketConnection closingEndpoint = clientStartsClose
            ? pair.Client
            : pair.Server;
        WebSocketLocalCloseResult close = closingEndpoint.StartConnectionClose(
            new WebSocketCloseInfo
            {
                HasCode = true,
                Code = 1000
            });
        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
        transport.EndClientLoopbackOutput();
        transport.EndServerLoopbackOutput();
        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
        CollectEvents(pair);

        WebSocketConnectionInfo clientInformation =
            pair.Client.GetConnectionInfo();
        WebSocketConnectionInfo serverInformation =
            pair.Server.GetConnectionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(close.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(pair.ClientCollector.Errors, Is.Empty);
            Assert.That(pair.ServerCollector.Errors, Is.Empty);
            AssertMessages(
                pair.ClientExpectedMessages,
                pair.ClientCollector.Messages);
            AssertMessages(
                pair.ServerExpectedMessages,
                pair.ServerCollector.Messages);
            Assert.That(clientInformation.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(serverInformation.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(clientInformation.CleanClose, Is.True);
            Assert.That(serverInformation.CleanClose, Is.True);
            Assert.That(clientInformation.HasLocalClose, Is.True);
            Assert.That(clientInformation.HasRemoteClose, Is.True);
            Assert.That(serverInformation.HasLocalClose, Is.True);
            Assert.That(serverInformation.HasRemoteClose, Is.True);
            Assert.That(clientInformation.PendingOutputBytes, Is.Zero);
            Assert.That(serverInformation.PendingOutputBytes, Is.Zero);
            Assert.That(clientInformation.PendingEventCount, Is.Zero);
            Assert.That(serverInformation.PendingEventCount, Is.Zero);
            Assert.That(
                transport.GetClientToServerQueuedByteCount(), Is.Zero);
            Assert.That(
                transport.GetServerToClientQueuedByteCount(), Is.Zero);
            Assert.That(
                transport.GetMaximumObservedClientToServerByteCount(),
                Is.LessThanOrEqualTo(transportCapacity));
            Assert.That(
                transport.GetMaximumObservedServerToClientByteCount(),
                Is.LessThanOrEqualTo(transportCapacity));
            Assert.That(clientMasks.CallCount,
                Is.EqualTo(
                    clientInformation.SentFrameCount +
                    expectedClientControlFrameCount +
                    1));
        });
    }

    static void SendRandomWholeMessage(
        RandomizedLoopbackPair pair,
        WebSocketInMemoryLoopbackTransport transport,
        LoopbackDeterministicRandom random,
        bool fromClient,
        int actionIndex,
        int transportCapacity)
    {
        WebSocketConnection sender = fromClient ? pair.Client : pair.Server;
        WebSocketMessageType messageType = random.NextBoolean()
            ? WebSocketMessageType.Text
            : WebSocketMessageType.Binary;
        byte[] payload = Payload(random, messageType, random.NextInt(33));
        WebSocketSendRequest request = new WebSocketSendRequest
        {
            MessageType = messageType,
            Payload = payload,
            PayloadCount = payload.Length
        };
        int scheduledLimit = fromClient
            ? pair.ClientOptions.AutomaticFramePayloadLength
            : pair.ServerOptions.AutomaticFramePayloadLength;
        if (random.NextBoolean())
        {
            request.HasFramePayloadLength = true;
            request.FramePayloadLength = 1 + random.NextInt(scheduledLimit);
        }

        AddExpectedMessage(pair, fromClient, actionIndex, messageType, payload);
        WebSocketParseStatus status = sender.SendWholeMessage(request);
        while (status == WebSocketParseStatus.NeedOutputCapacity)
        {
            PumpUntilQuiescent(
                pair,
                transport,
                random,
                transportCapacity);
            status = sender.SendWholeMessage(request);
        }

        Assert.That(status, Is.EqualTo(WebSocketParseStatus.Complete));
        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
    }

    static void SendRandomStreamingMessage(
        RandomizedLoopbackPair pair,
        WebSocketInMemoryLoopbackTransport transport,
        LoopbackDeterministicRandom random,
        bool fromClient,
        int actionIndex,
        int transportCapacity)
    {
        WebSocketConnection sender = fromClient ? pair.Client : pair.Server;
        WebSocketMessageType messageType = random.NextBoolean()
            ? WebSocketMessageType.Text
            : WebSocketMessageType.Binary;
        int scheduledLimit = fromClient
            ? pair.ClientOptions.AutomaticFramePayloadLength
            : pair.ServerOptions.AutomaticFramePayloadLength;
        int fragmentCount = 1 + random.NextInt(4);
        var chunks = new List<byte[]>();
        var completePayload = new List<byte>();
        int index = 0;
        while (index < fragmentCount)
        {
            int payloadCount = random.NextInt(scheduledLimit + 1);
            byte[] payload = Payload(random, messageType, payloadCount);
            chunks.Add(payload);
            completePayload.AddRange(payload);
            index++;
        }

        AddExpectedMessage(
            pair,
            fromClient,
            actionIndex,
            messageType,
            completePayload.ToArray());
        index = 0;
        while (index < chunks.Count)
        {
            WebSocketSendChunk chunk = new WebSocketSendChunk
            {
                First = index == 0,
                Final = index == chunks.Count - 1,
                MessageType = messageType,
                Payload = chunks[index],
                PayloadCount = chunks[index].Length
            };
            WebSocketParseStatus status = sender.SendMessageChunk(chunk);
            while (status == WebSocketParseStatus.NeedOutputCapacity)
            {
                PumpUntilQuiescent(
                    pair,
                    transport,
                    random,
                    transportCapacity);
                status = sender.SendMessageChunk(chunk);
            }

            Assert.That(status, Is.EqualTo(WebSocketParseStatus.Complete));
            if (random.NextBoolean())
            {
                PumpUntilQuiescent(
                    pair,
                    transport,
                    random,
                    transportCapacity);
            }

            index++;
        }

        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
    }

    static void SendRandomControl(
        RandomizedLoopbackPair pair,
        WebSocketInMemoryLoopbackTransport transport,
        LoopbackDeterministicRandom random,
        bool fromClient,
        bool ping,
        int actionIndex,
        int transportCapacity)
    {
        WebSocketConnection sender = fromClient ? pair.Client : pair.Server;
        int payloadCount = random.NextInt(6);
        byte[] payload = new byte[payloadCount];
        if (payload.Length != 0)
        {
            payload[0] = (byte)actionIndex;
            int index = 1;
            while (index < payload.Length)
            {
                payload[index] = random.NextByte();
                index++;
            }
        }

        WebSocketControlSendResult result = ping
            ? sender.SendConnectionPing(payload, 0, payload.Length)
            : sender.SendConnectionPong(payload, 0, payload.Length);
        while (result.Status == WebSocketParseStatus.NeedOutputCapacity)
        {
            PumpUntilQuiescent(
                pair,
                transport,
                random,
                transportCapacity);
            result = ping
                ? sender.SendConnectionPing(payload, 0, payload.Length)
                : sender.SendConnectionPong(payload, 0, payload.Length);
        }

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        PumpUntilQuiescent(
            pair,
            transport,
            random,
            transportCapacity);
    }

    static void PumpUntilQuiescent(
        RandomizedLoopbackPair pair,
        WebSocketInMemoryLoopbackTransport transport,
        LoopbackDeterministicRandom random,
        int transportCapacity)
    {
        int attempt = 0;
        while (attempt < 2000)
        {
            int maximumSteps = 1 + random.NextInt(7);
            WebSocketLoopbackPumpResult result =
                transport.PumpLoopbackTraffic(maximumSteps);
            Assert.That(
                result.HasError,
                Is.False,
                result.Error.Identifier);
            Assert.That(
                transport.GetClientToServerQueuedByteCount(),
                Is.LessThanOrEqualTo(transportCapacity));
            Assert.That(
                transport.GetServerToClientQueuedByteCount(),
                Is.LessThanOrEqualTo(transportCapacity));
            AssertConnectionBounds(pair.Client, pair.ClientOptions);
            AssertConnectionBounds(pair.Server, pair.ServerOptions);
            CollectEvents(pair);
            if (result.Quiescent)
            {
                return;
            }

            Assert.That(
                result.Blocked || result.StepLimitReached || result.Progress,
                Is.True);
            attempt++;
        }

        Assert.Fail("The deterministic loopback sequence did not quiesce.");
    }

    static void CollectEvents(RandomizedLoopbackPair pair)
    {
        CollectConnectionEvents(pair.Client, pair.ClientCollector);
        CollectConnectionEvents(pair.Server, pair.ServerCollector);
    }

    static void CollectConnectionEvents(
        WebSocketConnection connection,
        RandomizedLoopbackCollector collector)
    {
        WebSocketEvent? connectionEvent = connection.ReadNextEvent();
        while (connectionEvent != null)
        {
            collector.AddEvent(connectionEvent);
            connectionEvent = connection.ReadNextEvent();
        }
    }

    static void AssertConnectionBounds(
        WebSocketConnection connection,
        WebSocketConnectionOptions options)
    {
        WebSocketConnectionInfo information = connection.GetConnectionInfo();
        Assert.That(information.PendingOutputBytes,
            Is.LessThanOrEqualTo(options.MaximumBufferedOutputBytes));
        Assert.That(information.PendingEventCount,
            Is.LessThanOrEqualTo(options.MaximumQueuedEvents));
    }

    static void AddExpectedMessage(
        RandomizedLoopbackPair pair,
        bool fromClient,
        int actionIndex,
        WebSocketMessageType messageType,
        byte[] payload)
    {
        var expected = new RandomizedLoopbackMessage
        {
            ActionIndex = actionIndex,
            MessageType = messageType,
            Payload = payload.ToArray()
        };
        if (fromClient)
        {
            pair.ServerExpectedMessages.Add(expected);
        }
        else
        {
            pair.ClientExpectedMessages.Add(expected);
        }
    }

    static byte[] Payload(
        LoopbackDeterministicRandom random,
        WebSocketMessageType messageType,
        int count)
    {
        byte[] payload = new byte[count];
        int index = 0;
        while (index < payload.Length)
        {
            if (messageType == WebSocketMessageType.Text)
            {
                payload[index] = (byte)('a' + random.NextInt(26));
            }
            else
            {
                payload[index] = random.NextByte();
            }

            index++;
        }

        return payload;
    }

    static WebSocketConnectionOptions Options(
        WebSocketRole role,
        WebSocketMessageDeliveryMode deliveryMode,
        int outputCapacity,
        int eventCapacity,
        IWebSocketMaskKeySource? maskKeySource)
    {
        int automaticFramePayloadLength = outputCapacity -
            WebSocketProtocolConstants.MaximumFrameHeaderLength;
        if (automaticFramePayloadLength > 8)
        {
            automaticFramePayloadLength = 8;
        }

        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = deliveryMode,
            MaximumFramePayloadLength = 8,
            MaximumMessagePayloadLength = 32,
            MaximumDecompressedMessagePayloadLength = 32,
            MaximumFragmentCount = 32,
            MaximumBufferedInputBytes = 48,
            MaximumBufferedOutputBytes = outputCapacity,
            MaximumQueuedEvents = eventCapacity,
            AutomaticFramePayloadLength = automaticFramePayloadLength,
            AutomaticPong = true,
            AutomaticCloseResponse = true,
            CloseTimeoutMilliseconds = 1000
        };
        if (role == WebSocketRole.Client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = maskKeySource;
        }

        return options;
    }

    static WebSocketConnection Connection(WebSocketConnectionOptions options)
    {
        return new WebSocketConnection(
            options,
            new WebSocketHandshakeResult
            {
                Outcome = WebSocketHandshakeOutcome.Accepted,
                Transport = WebSocketHandshakeTransport.Http1Upgrade
            });
    }

    static void AssertMessages(
        List<RandomizedLoopbackMessage> expected,
        List<RandomizedLoopbackMessage> actual)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count));
        int index = 0;
        while (index < expected.Count && index < actual.Count)
        {
            Assert.That(actual[index].MessageType,
                Is.EqualTo(expected[index].MessageType),
                "message type at " + index);
            Assert.That(actual[index].Payload,
                Is.EqualTo(expected[index].Payload),
                "message payload at " + index);
            index++;
        }
    }
}

internal class RandomizedLoopbackPair
{
    internal WebSocketConnection Client = null!;
    internal WebSocketConnection Server = null!;
    internal WebSocketConnectionOptions ClientOptions =
        new WebSocketConnectionOptions();
    internal WebSocketConnectionOptions ServerOptions =
        new WebSocketConnectionOptions();
    internal RandomizedLoopbackCollector ClientCollector =
        new RandomizedLoopbackCollector(
            WebSocketMessageDeliveryMode.Reassembled);
    internal RandomizedLoopbackCollector ServerCollector =
        new RandomizedLoopbackCollector(
            WebSocketMessageDeliveryMode.Reassembled);
    internal List<RandomizedLoopbackMessage> ClientExpectedMessages = [];
    internal List<RandomizedLoopbackMessage> ServerExpectedMessages = [];
}

internal class RandomizedLoopbackMessage
{
    internal int ActionIndex;
    internal WebSocketMessageType MessageType;
    internal byte[] Payload = [];
}

internal class RandomizedLoopbackCollector
{
    internal WebSocketMessageDeliveryMode DeliveryMode;
    internal List<RandomizedLoopbackMessage> Messages = [];
    internal List<WebSocketError> Errors = [];
    internal bool MessageActive;
    internal WebSocketMessageType ActiveMessageType;
    internal List<byte> ActivePayload = [];

    internal RandomizedLoopbackCollector(
        WebSocketMessageDeliveryMode deliveryMode)
    {
        DeliveryMode = deliveryMode;
    }

    internal void AddEvent(WebSocketEvent connectionEvent)
    {
        if (connectionEvent.Type == WebSocketEventType.Error)
        {
            Errors.Add(connectionEvent.Error);
            return;
        }

        if (DeliveryMode == WebSocketMessageDeliveryMode.Reassembled &&
            connectionEvent.Type == WebSocketEventType.MessageData)
        {
            Messages.Add(new RandomizedLoopbackMessage
            {
                MessageType = connectionEvent.MessageType,
                Payload = EventPayload(connectionEvent)
            });
            return;
        }

        if (DeliveryMode != WebSocketMessageDeliveryMode.Streaming)
        {
            return;
        }

        if (connectionEvent.Type == WebSocketEventType.MessageStart)
        {
            Assert.That(MessageActive, Is.False);
            MessageActive = true;
            ActiveMessageType = connectionEvent.MessageType;
            ActivePayload = [];
        }
        else if (connectionEvent.Type == WebSocketEventType.MessageData)
        {
            Assert.That(MessageActive, Is.True);
            ActivePayload.AddRange(EventPayload(connectionEvent));
        }
        else if (connectionEvent.Type == WebSocketEventType.MessageEnd)
        {
            Assert.That(MessageActive, Is.True);
            Messages.Add(new RandomizedLoopbackMessage
            {
                MessageType = ActiveMessageType,
                Payload = ActivePayload.ToArray()
            });
            MessageActive = false;
            ActivePayload = [];
        }
    }

    static byte[] EventPayload(WebSocketEvent connectionEvent)
    {
        return connectionEvent.Payload
            .AsSpan(
                connectionEvent.PayloadOffset,
                connectionEvent.PayloadCount)
            .ToArray();
    }
}

internal class LoopbackDeterministicRandom
{
    internal uint State;

    internal LoopbackDeterministicRandom(uint seed)
    {
        State = seed;
    }

    internal uint NextUInt()
    {
        State = State * 1664525U + 1013904223U;
        return State;
    }

    internal int NextInt(int exclusiveMaximum)
    {
        Assert.That(exclusiveMaximum, Is.GreaterThan(0));
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }

    internal bool NextBoolean()
    {
        return (NextUInt() & 1U) != 0;
    }

    internal byte NextByte()
    {
        return (byte)(NextUInt() >> 24);
    }
}

internal class RandomizedLoopbackMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey;
    internal int CallCount;

    internal RandomizedLoopbackMaskKeySource(uint seed)
    {
        NextKey = seed | 1U;
    }

    public uint CreateMaskingKey()
    {
        uint key = NextKey;
        NextKey += 0x01010101U;
        CallCount++;
        return key;
    }
}
