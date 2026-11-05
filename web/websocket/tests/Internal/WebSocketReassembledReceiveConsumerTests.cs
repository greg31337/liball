using websocket;

namespace websocket.Tests;

public class WebSocketReassembledReceiveConsumerTests
{
    [Test]
    public void UnfragmentedMessageProducesOneOwnedCompleteEvent()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(2, 64);
        byte[] payload = [1, 2, 3];

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Binary, true, payload.Length),
                    payload));
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Accepted, Is.True);
            Assert.That(message.Type,
                Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(message.HasMessageType, Is.True);
            Assert.That(message.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(message.Payload, Is.Not.SameAs(payload));
            Assert.That(message.Payload, Is.EqualTo(payload));
            Assert.That(message.PayloadOffset, Is.Zero);
            Assert.That(message.PayloadCount, Is.EqualTo(3));
            Assert.That(message.First, Is.True);
            Assert.That(message.Final, Is.True);
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCapacity(), Is.Zero);
        });
    }

    [Test]
    public void FragmentedMessageIsJoinedAcrossInterleavedControls()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(2, 64);

        WebSocketInboundFrameConsumeResult first =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Text, false, 2), [65, 66]));
        WebSocketInboundFrameConsumeResult control =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Ping, true, 1), [9]));
        WebSocketInboundFrameConsumeResult final =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Continuation, true, 3),
                    [67, 68, 69]));
        WebSocketEvent ping = RequiredEvent(
            consumer.ReadReassembledEvent());
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(control.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping.Type,
                Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(ping.Payload, Is.EqualTo(new byte[] { 9 }));
            Assert.That(message.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(message.Payload,
                Is.EqualTo(new byte[] { 65, 66, 67, 68, 69 }));
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
        });
    }

    [Test]
    public void EmptyMessageProducesOneEmptyCompleteEvent()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(1, 64);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Text, true, 0), []));
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(message.Type,
                Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(message.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(message.Payload, Is.Empty);
            Assert.That(message.PayloadCount, Is.Zero);
            Assert.That(message.First, Is.True);
            Assert.That(message.Final, Is.True);
        });
    }

    [TestCase(new byte[] { 0xC0 }, "stream-text-utf8-invalid")]
    [TestCase(new byte[] { 0xE2 }, "stream-text-utf8-incomplete")]
    public void InvalidTextProducesNoPartialOrCompleteMessage(
        byte[] payload,
        string identifier)
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(2, 64);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Text, true, payload.Length),
                    payload));
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(errorEvent.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCapacity(), Is.Zero);
        });
    }

    [Test]
    public void TransformedChunksAreReassembledOnlyAfterTransformCompletion()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.SplitOutput = true;
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(2, 64);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [negotiation]);
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            pipeline);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Binary, true, 1), [7]));
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.InboundCallCount, Is.EqualTo(2));
            Assert.That(message.Payload,
                Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(message.PayloadCount, Is.EqualTo(3));
            Assert.That(message.First, Is.True);
            Assert.That(message.Final, Is.True);
        });
    }

    [Test]
    public void InvalidTransformedTextIsNeverExposedAsAWholeMessage()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.Output = [0xC0];
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(2, 64);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Text, true, 1), [1]));
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
        });
    }

    [Test]
    public void FullEventQueueStopsBeforeTheNextFinalFramePayload()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(1, 64);
        var codec = new WebSocketFrameCodec(
            WebSocketRole.Client,
            128,
            []);
        var pump = new WebSocketInboundFramePump(codec, consumer);
        byte[] first = WireFrame(WebSocketOpcode.Binary, true, [1]);
        byte[] second = WireFrame(WebSocketOpcode.Text, true, [65]);
        byte[] source = Combine(first, second);

        WebSocketInboundFramePumpResult blocked =
            pump.PumpInboundFrames(source);
        WebSocketEvent firstMessage = RequiredEvent(
            consumer.ReadReassembledEvent());
        WebSocketInboundFramePumpResult resumed = pump.PumpInboundFrames(
            source.AsSpan(blocked.ConsumedCount));
        WebSocketEvent secondMessage = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(first.Length + 2));
            Assert.That(blocked.FrameCount, Is.EqualTo(1));
            Assert.That(firstMessage.Payload, Is.EqualTo(new byte[] { 1 }));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.ConsumedCount,
                Is.EqualTo(second.Length - 2));
            Assert.That(resumed.FrameCount, Is.EqualTo(1));
            Assert.That(secondMessage.Payload,
                Is.EqualTo(new byte[] { 65 }));
        });
    }

    [Test]
    public void FullPublicEventQueueStopsBeforeTheNextControlPayload()
    {
        WebSocketConnectionOptions options = Options(1, 64);
        options.AutomaticPong = false;
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Ping, true, 1), [1]));

        WebSocketInboundFrameCapacity blocked =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Pong, true, 1),
                true);
        WebSocketEvent first = RequiredEvent(
            consumer.ReadReassembledEvent());
        WebSocketInboundFrameCapacity resumed =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Pong, true, 1),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.MaximumPayloadCount, Is.Zero);
            Assert.That(first.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.MaximumPayloadCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DirectConsumeRetainsOneCompletedMessageUntilQueueCapacityReturns()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(1, 64);
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Binary, true, 1), [1]));
        WebSocketInboundFrameChunk secondChunk = Chunk(
            Header(WebSocketOpcode.Text, true, 1),
            [65]);

        WebSocketInboundFrameConsumeResult blocked =
            consumer.ConsumeInboundFrameChunk(secondChunk);
        WebSocketEvent first = RequiredEvent(
            consumer.ReadReassembledEvent());
        WebSocketInboundFrameConsumeResult resumed =
            consumer.ConsumeInboundFrameChunk(secondChunk);
        WebSocketEvent second = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(first.Payload, Is.EqualTo(new byte[] { 1 }));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.Accepted, Is.True);
            Assert.That(second.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(second.Payload, Is.EqualTo(new byte[] { 65 }));
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
        });
    }

    [Test]
    public void ControlAndMessageBytesShareOneResumableInputBudget()
    {
        WebSocketConnectionOptions options = Options(2, 14);
        options.AutomaticPong = false;
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
        consumer.ConsumeInboundFrameChunk(
            Chunk(
                Header(WebSocketOpcode.Binary, false, 10),
                new byte[10]));
        consumer.ConsumeInboundFrameChunk(
            Chunk(
                Header(WebSocketOpcode.Ping, true, 4),
                [1, 2, 3, 4]));

        WebSocketInboundFrameCapacity blocked =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Continuation, true, 1),
                true);
        int ownedWhileBlocked = consumer.GetOwnedPayloadCount();
        WebSocketEvent ping = RequiredEvent(
            consumer.ReadReassembledEvent());
        WebSocketInboundFrameCapacity resumed =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Continuation, true, 1),
                true);
        WebSocketInboundFrameConsumeResult completed =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Continuation, true, 1),
                    [9]));
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(ownedWhileBlocked, Is.EqualTo(14));
            Assert.That(ping.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.MaximumPayloadCount, Is.EqualTo(4));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(message.PayloadCount, Is.EqualTo(11));
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void ExtensionExpansionResumesWithTheRemainingConnectionBudget()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.Output = new byte[10];
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(2, 14);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Binary, false, 1), [1]));
        extension.FillAvailableOutput = true;

        WebSocketInboundFrameConsumeResult completed =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Continuation, true, 1),
                    [2]));
        int ownedBeforeDrain = consumer.GetOwnedPayloadCount();
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.LastInboundMaximumOutputCount,
                Is.EqualTo(4));
            Assert.That(ownedBeforeDrain, Is.EqualTo(14));
            Assert.That(message.PayloadCount, Is.EqualTo(14));
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void UndeliverableWholeMessageInputLimitFailsWithoutGrowing()
    {
        WebSocketReassembledReceiveConsumer consumer =
            CreateConsumer(2, 14);
        consumer.ConsumeInboundFrameChunk(
            Chunk(
                Header(WebSocketOpcode.Binary, false, 14),
                new byte[14]));

        WebSocketInboundFrameCapacity failed =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Continuation, true, 1),
                true);
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(failed.HasError, Is.True);
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("reassembly-input-buffer-limit-exceeded"));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void ReassemblyBufferRejectsOversizeBeforeGrowingOrChangingData()
    {
        var buffer = new WebSocketReassemblyBuffer(4);
        bool initial = buffer.AppendMessageBytes([1, 2], 0, 2);
        int capacityBefore = buffer.GetMessageByteCapacity();

        bool excessive = buffer.AppendMessageBytes([3, 4, 5], 0, 3);
        byte[] retained = buffer.TakeMessageBytes();

        Assert.Multiple(() =>
        {
            Assert.That(initial, Is.True);
            Assert.That(capacityBefore, Is.EqualTo(2));
            Assert.That(excessive, Is.False);
            Assert.That(retained, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(buffer.GetMessageByteCount(), Is.Zero);
            Assert.That(buffer.GetMessageByteCapacity(), Is.Zero);
        });
    }

    [Test]
    public void RawMessageLimitClearsPreviouslyBufferedFragments()
    {
        WebSocketConnectionOptions options = Options(2, 64);
        options.MaximumFramePayloadLength = 2;
        options.MaximumMessagePayloadLength = 2;
        options.MaximumDecompressedMessagePayloadLength = 4;
        options.MaximumFragmentCount = 2;
        options.AutomaticFramePayloadLength = 1;
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Binary, false, 2), [1, 2]));

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Continuation, true, 1), [3]));
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("stream-message-payload-limit-exceeded"));
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCapacity(), Is.Zero);
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(errorEvent.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(consumer.GetReassembledEventCount(), Is.Zero);
        });
    }

    [Test]
    public void ResetClearsFailureBufferAndQueuedWholeMessages()
    {
        WebSocketReassembledReceiveConsumer consumer = CreateConsumer(2, 64);
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Binary, true, 1), [1]));
        consumer.ConsumeInboundFrameChunk(
            Chunk(Header(WebSocketOpcode.Continuation, true, 1), [2]));

        consumer.ResetInboundFrameConsumer();
        WebSocketInboundFrameConsumeResult recovered =
            consumer.ConsumeInboundFrameChunk(
                Chunk(Header(WebSocketOpcode.Text, true, 1), [65]));
        WebSocketEvent message = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(message.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(message.Payload, Is.EqualTo(new byte[] { 65 }));
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
        });
    }

    [Test]
    public void ConstructorRequiresReassembledOptionsAndPipeline()
    {
        WebSocketConnectionOptions streaming = Options(2, 64);
        streaming.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        WebSocketConnectionOptions reassembled = Options(2, 64);

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketReassembledReceiveConsumer(
                    streaming,
                    new WebSocketExtensionPipeline([], [])));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketReassembledReceiveConsumer(
                    reassembled,
                    null));
        });
    }

    static WebSocketReassembledReceiveConsumer CreateConsumer(
        int eventCount,
        int bufferedInputBytes)
    {
        return new WebSocketReassembledReceiveConsumer(
            Options(eventCount, bufferedInputBytes),
            new WebSocketExtensionPipeline([], []));
    }

    static WebSocketConnectionOptions Options(
        int eventCount,
        int bufferedInputBytes)
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 256,
            MaximumDecompressedMessagePayloadLength = 256,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = bufferedInputBytes,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = eventCount,
            AutomaticFramePayloadLength = 64
        };
    }

    static WebSocketExtensionNegotiation Negotiation()
    {
        return new WebSocketExtensionNegotiation
        {
            Name = "x-stream-test",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-stream-test"
            }
        };
    }

    static WebSocketInboundFrameChunk Chunk(
        WebSocketFrameHeader header,
        byte[] payload)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = header,
            FirstChunk = true,
            EndOfFrame = true,
            Payload = payload,
            PayloadOffset = 0,
            PayloadCount = payload.Length
        };
    }

    static WebSocketFrameHeader Header(
        WebSocketOpcode opcode,
        bool final,
        long payloadLength)
    {
        return new WebSocketFrameHeader
        {
            Final = final,
            Opcode = opcode,
            PayloadLength = payloadLength
        };
    }

    static byte[] WireFrame(
        WebSocketOpcode opcode,
        bool final,
        byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 2];
        int first = (int)opcode;
        if (final)
        {
            first = first | 128;
        }

        frame[0] = (byte)first;
        frame[1] = (byte)payload.Length;
        payload.CopyTo(frame, 2);
        return frame;
    }

    static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}
