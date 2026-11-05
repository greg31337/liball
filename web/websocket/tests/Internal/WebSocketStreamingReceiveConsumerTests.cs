using websocket;

namespace websocket.Tests;

public class WebSocketStreamingReceiveConsumerTests
{
    [Test]
    public void OneSlotQueueResumesStartDataAndEndWithoutLosingTheChunk()
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(1, 64);
        byte[] payload = [72, 105];
        WebSocketInboundFrameChunk chunk = Chunk(
            Header(WebSocketOpcode.Text, true, payload.Length),
            true,
            true,
            payload);

        WebSocketInboundFrameConsumeResult startBlocked =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketEvent start = RequiredEvent(consumer.ReadStreamingEvent());
        WebSocketInboundFrameConsumeResult dataBlocked =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketEvent data = RequiredEvent(consumer.ReadStreamingEvent());
        WebSocketInboundFrameConsumeResult completed =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketEvent end = RequiredEvent(consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(startBlocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(startBlocked.Accepted, Is.False);
            Assert.That(start.Type,
                Is.EqualTo(WebSocketEventType.MessageStart));
            Assert.That(start.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(start.First, Is.True);
            Assert.That(dataBlocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(data.Type,
                Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(data.Payload, Is.Not.SameAs(payload));
            Assert.That(data.Payload, Is.EqualTo(payload));
            Assert.That(data.PayloadOffset, Is.Zero);
            Assert.That(data.PayloadCount, Is.EqualTo(2));
            Assert.That(data.First, Is.True);
            Assert.That(data.Final, Is.True);
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.Accepted, Is.True);
            Assert.That(end.Type,
                Is.EqualTo(WebSocketEventType.MessageEnd));
            Assert.That(end.Final, Is.True);
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void FramePumpStreamsFragmentedTextAcrossInterleavedControlFrames()
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(12, 64);
        var codec = new WebSocketFrameCodec(
            WebSocketRole.Client,
            128,
            []);
        var pump = new WebSocketInboundFramePump(codec, consumer);
        byte[] source = Combine(
            WireFrame(WebSocketOpcode.Text, false, [65]),
            WireFrame(WebSocketOpcode.Ping, true, [9]),
            WireFrame(
                WebSocketOpcode.Continuation,
                true,
                [0xE2, 0x82, 0xAC]));

        WebSocketInboundFramePumpResult result =
            pump.PumpInboundFrames(source);
        List<WebSocketEvent> events = DrainEvents(consumer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(result.FrameCount, Is.EqualTo(3));
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.Ping,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageEnd
                }));
            Assert.That(events[0].MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(events[1].MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(events[3].MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(events[4].MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(events[1].Payload, Is.EqualTo(new byte[] { 65 }));
            Assert.That(events[1].First, Is.True);
            Assert.That(events[1].Final, Is.False);
            Assert.That(events[2].Payload, Is.EqualTo(new byte[] { 9 }));
            Assert.That(events[2].HasMessageType, Is.False);
            Assert.That(events[3].Payload,
                Is.EqualTo(new byte[] { 0xE2, 0x82, 0xAC }));
            Assert.That(events[3].First, Is.False);
            Assert.That(events[3].Final, Is.True);
        });
    }

    [TestCase(new byte[] { 0xC0 }, "stream-text-utf8-invalid")]
    [TestCase(new byte[] { 0xE2 }, "stream-text-utf8-incomplete")]
    public void InvalidTextQueuesNoApplicationEvents(
        byte[] payload,
        string identifier)
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(4, 64);
        WebSocketInboundFrameChunk chunk = Chunk(
            Header(WebSocketOpcode.Text, true, payload.Length),
            true,
            true,
            payload);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(errorEvent.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void ValidUtf8SequenceMaySpanFrameChunksAndFragments()
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(8, 64);
        WebSocketFrameHeader firstHeader = Header(
            WebSocketOpcode.Text,
            false,
            1);
        WebSocketFrameHeader finalHeader = Header(
            WebSocketOpcode.Continuation,
            true,
            2);

        WebSocketInboundFrameConsumeResult first =
            consumer.ConsumeInboundFrameChunk(
                Chunk(firstHeader, true, true, [0xE2]));
        WebSocketInboundFrameConsumeResult final =
            consumer.ConsumeInboundFrameChunk(
                Chunk(finalHeader, true, true, [0x82, 0xAC]));
        List<WebSocketEvent> events = DrainEvents(consumer);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageEnd
                }));
            Assert.That(events[1].Payload,
                Is.EqualTo(new byte[] { 0xE2 }));
            Assert.That(events[2].Payload,
                Is.EqualTo(new byte[] { 0x82, 0xAC }));
        });
    }

    [Test]
    public void TransformedTextIsValidatedBeforeAnyApplicationEventIsQueued()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.Output = [0xC0];
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(4, 64);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [negotiation]);
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            pipeline);
        WebSocketInboundFrameChunk chunk = Chunk(
            Header(WebSocketOpcode.Text, true, 1),
            true,
            true,
            [1]);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("stream-text-utf8-invalid"));
            Assert.That(extension.InboundCallCount, Is.EqualTo(1));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
        });
    }

    [Test]
    public void TransformedOutputUsesExactOwnedArraysAndPayloadBackpressure()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.FillAvailableOutput = true;
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(4, 14);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [negotiation]);
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            pipeline);
        WebSocketInboundFrameChunk chunk = Chunk(
            Header(WebSocketOpcode.Binary, true, 1),
            true,
            true,
            [7]);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(chunk);
        WebSocketInboundFrameCapacity blocked =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Binary, true, 1),
                true);
        WebSocketEvent start = RequiredEvent(consumer.ReadStreamingEvent());
        WebSocketEvent data = RequiredEvent(consumer.ReadStreamingEvent());
        WebSocketInboundFrameCapacity resumed =
            consumer.GetInboundFrameCapacity(
                Header(WebSocketOpcode.Binary, true, 1),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(start.Type,
                Is.EqualTo(WebSocketEventType.MessageStart));
            Assert.That(data.Type,
                Is.EqualTo(WebSocketEventType.MessageData));
            Assert.That(data.PayloadCount, Is.EqualTo(14));
            Assert.That(data.Payload.Length, Is.EqualTo(14));
            Assert.That(data.Payload.All(value => value == 90), Is.True);
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.MaximumPayloadCount, Is.EqualTo(14));
        });
    }

    [Test]
    public void PartialTransformOutputResumesTheSameChunkAfterEventsDrain()
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
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            pipeline);
        WebSocketInboundFrameChunk chunk = Chunk(
            Header(WebSocketOpcode.Binary, true, 1),
            true,
            true,
            [7]);

        WebSocketInboundFrameConsumeResult paused =
            consumer.ConsumeInboundFrameChunk(chunk);
        List<WebSocketEvent> firstEvents = DrainEvents(consumer);
        WebSocketInboundFrameConsumeResult completed =
            consumer.ConsumeInboundFrameChunk(chunk);
        List<WebSocketEvent> finalEvents = DrainEvents(consumer);

        Assert.Multiple(() =>
        {
            Assert.That(paused.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(firstEvents.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData
                }));
            Assert.That(firstEvents[1].Payload,
                Is.EqualTo(new byte[] { 65, 66 }));
            Assert.That(firstEvents[1].First, Is.True);
            Assert.That(firstEvents[1].Final, Is.False);
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finalEvents.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageEnd
                }));
            Assert.That(finalEvents[0].Payload,
                Is.EqualTo(new byte[] { 67 }));
            Assert.That(finalEvents[0].First, Is.False);
            Assert.That(finalEvents[0].Final, Is.True);
            Assert.That(extension.InboundCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void MessageTrackingFailureIsReturnedWithoutApplicationEvents()
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(8, 64);
        WebSocketInboundFrameChunk first = Chunk(
            Header(WebSocketOpcode.Text, false, 1),
            true,
            true,
            [65]);
        WebSocketInboundFrameChunk overlap = Chunk(
            Header(WebSocketOpcode.Binary, true, 1),
            true,
            true,
            [1]);
        consumer.ConsumeInboundFrameChunk(first);
        DrainEvents(consumer);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(overlap);
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("message-fragment-overlap"));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
        });
    }

    [Test]
    public void DecompressedMessageLimitFailsBeforeExpandedBytesAreQueued()
    {
        var extension = new StreamingReceiveTestExtension();
        extension.Output = [1, 2, 3];
        WebSocketExtensionNegotiation negotiation = Negotiation();
        WebSocketConnectionOptions options = Options(4, 64);
        options.MaximumDecompressedMessagePayloadLength = 2;
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [negotiation]);
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            pipeline);

        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(
                    Header(WebSocketOpcode.Binary, true, 1),
                    true,
                    true,
                    [9]));
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("stream-decompressed-payload-limit-exceeded"));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(errorEvent.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
        });
    }

    [Test]
    public void ResetClearsFailureQueuedEventsAndPendingOwnership()
    {
        WebSocketStreamingReceiveConsumer consumer = CreateConsumer(1, 64);
        WebSocketInboundFrameChunk invalid = Chunk(
            Header(WebSocketOpcode.Text, true, 1),
            true,
            true,
            [0xC0]);
        consumer.ConsumeInboundFrameChunk(invalid);

        consumer.ResetInboundFrameConsumer();
        WebSocketInboundFrameChunk valid = Chunk(
            Header(WebSocketOpcode.Binary, true, 1),
            true,
            true,
            [8]);
        WebSocketInboundFrameConsumeResult recovered =
            consumer.ConsumeInboundFrameChunk(valid);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(recovered.HasError, Is.False);
            Assert.That(consumer.GetStreamingEventCount(), Is.EqualTo(1));
            Assert.That(consumer.GetOwnedPayloadCount(), Is.EqualTo(1));
            Assert.That(RequiredEvent(consumer.ReadStreamingEvent()).Type,
                Is.EqualTo(WebSocketEventType.MessageStart));
        });
    }

    [Test]
    public void ConstructorRequiresStreamingOptionsAndPipeline()
    {
        WebSocketConnectionOptions reassembled = Options(4, 64);
        reassembled.DeliveryMode = WebSocketMessageDeliveryMode.Reassembled;
        WebSocketConnectionOptions streaming = Options(4, 64);

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketStreamingReceiveConsumer(
                    reassembled,
                    new WebSocketExtensionPipeline([], [])));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketStreamingReceiveConsumer(streaming, null));
        });
    }

    static WebSocketStreamingReceiveConsumer CreateConsumer(
        int eventCount,
        int ownedPayloadBytes)
    {
        WebSocketConnectionOptions options = Options(
            eventCount,
            ownedPayloadBytes);
        return new WebSocketStreamingReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
    }

    static WebSocketConnectionOptions Options(
        int eventCount,
        int ownedPayloadBytes)
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 256,
            MaximumDecompressedMessagePayloadLength = 256,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = ownedPayloadBytes,
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
        bool first,
        bool end,
        byte[] payload)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = header,
            FirstChunk = first,
            EndOfFrame = end,
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

    static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        byte[] combined = new byte[
            first.Length + second.Length + third.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        third.CopyTo(combined, first.Length + second.Length);
        return combined;
    }

    static List<WebSocketEvent> DrainEvents(
        WebSocketStreamingReceiveConsumer consumer)
    {
        var events = new List<WebSocketEvent>();
        WebSocketEvent? connectionEvent = consumer.ReadStreamingEvent();
        while (connectionEvent != null)
        {
            events.Add(connectionEvent);
            connectionEvent = consumer.ReadStreamingEvent();
        }

        return events;
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}

internal class StreamingReceiveTestExtension : IWebSocketExtension
{
    internal byte[] Output = [65];
    internal bool FillAvailableOutput;
    internal bool SplitOutput;
    internal int InboundCallCount;
    internal int LastInboundMaximumOutputCount;

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = "x-stream-test" };
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
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = source.Length,
            Output = source.ToArray()
        };
    }

    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        InboundCallCount++;
        LastInboundMaximumOutputCount =
            context.MaximumBufferedOutputBytes;
        byte[] output = Output.ToArray();
        if (FillAvailableOutput)
        {
            output = new byte[context.MaximumBufferedOutputBytes];
            Array.Fill(output, (byte)90);
        }

        if (SplitOutput)
        {
            output = [67];
            if (InboundCallCount == 1)
            {
                output = [65, 66];
            }
        }

        var result = new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output
        };
        if (SplitOutput && InboundCallCount == 1)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
    }

    public void ResetExtensionState()
    {
    }
}
