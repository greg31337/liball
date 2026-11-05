using websocket;

namespace websocket.Tests;

public class WebSocketStreamingMessageWriterTests
{
    [Test]
    public void EncodesOneFramePerAcceptedStreamingChunk()
    {
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));

        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(
                Chunk(true, false, WebSocketMessageType.Text, [65]));
        WebSocketStreamingMessageWriteResult middle =
            writer.EncodeMessageChunk(
                Chunk(false, false, WebSocketMessageType.Binary, [66]));
        WebSocketStreamingMessageWriteResult final =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [67]));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.EncodedFrame,
                Is.EqualTo(new byte[] { 0x01, 0x01, 65 }));
            Assert.That(first.MessageStarted, Is.True);
            Assert.That(first.MessageEnded, Is.False);
            Assert.That(first.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(first.FragmentCount, Is.EqualTo(1));
            Assert.That(middle.EncodedFrame,
                Is.EqualTo(new byte[] { 0x00, 0x01, 66 }));
            Assert.That(middle.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(middle.FragmentCount, Is.EqualTo(2));
            Assert.That(final.EncodedFrame,
                Is.EqualTo(new byte[] { 0x80, 0x01, 67 }));
            Assert.That(final.MessageEnded, Is.True);
            Assert.That(final.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(final.FragmentCount, Is.EqualTo(3));
            Assert.That(writer.HasActiveStreamingMessage(), Is.False);
            Assert.That(writer.GetActiveStreamingFragmentCount(), Is.Zero);
            Assert.That(writer.GetActiveStreamingPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void OrphanAndOverlapAreRejectedWithoutChangingActiveMessage()
    {
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));
        WebSocketStreamingMessageWriteResult orphan =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [1]));
        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(
                Chunk(true, false, WebSocketMessageType.Binary, [2]));
        WebSocketStreamingMessageWriteResult overlap =
            writer.EncodeMessageChunk(
                Chunk(true, true, WebSocketMessageType.Text, [65]));
        WebSocketSendChunk continuation = Chunk(
            false,
            true,
            (WebSocketMessageType)9,
            [3]);
        continuation.Compress = true;
        WebSocketStreamingMessageWriteResult recovered =
            writer.EncodeMessageChunk(continuation);

        Assert.Multiple(() =>
        {
            Assert.That(orphan.Error.Identifier,
                Is.EqualTo("stream-send-continuation-unexpected"));
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(overlap.Error.Identifier,
                Is.EqualTo("stream-send-message-overlap"));
            Assert.That(overlap.Accepted, Is.False);
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(recovered.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(recovered.FragmentCount, Is.EqualTo(2));
            Assert.That(recovered.EncodedFrame,
                Is.EqualTo(new byte[] { 0x80, 0x01, 3 }));
        });
    }

    [Test]
    public void Utf8MaySpanChunksAndInvalidContinuationCanBeCorrected()
    {
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));
        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(
                Chunk(true, false, WebSocketMessageType.Text, [0xE2]));
        WebSocketStreamingMessageWriteResult invalid =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Text, [65]));

        WebSocketStreamingMessageWriteResult corrected =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Text, [0x82, 0xAC]));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(invalid.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(invalid.Error.Identifier,
                Is.EqualTo("stream-send-text-utf8-invalid"));
            Assert.That(writer.HasActiveStreamingMessage(), Is.False);
            Assert.That(corrected.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(corrected.FragmentCount, Is.EqualTo(2));
            Assert.That(corrected.EncodedFrame,
                Is.EqualTo(new byte[] { 0x80, 0x02, 0x82, 0xAC }));
        });
    }

    [Test]
    public void IncompleteFinalTextChunkCanBeRetried()
    {
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));
        writer.EncodeMessageChunk(
            Chunk(true, false, WebSocketMessageType.Text, [0xF0]));
        WebSocketStreamingMessageWriteResult incomplete =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Text, [0x90]));

        WebSocketStreamingMessageWriteResult completed =
            writer.EncodeMessageChunk(
                Chunk(
                    false,
                    true,
                    WebSocketMessageType.Text,
                    [0x90, 0x80, 0x80]));

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Error.Identifier,
                Is.EqualTo("stream-send-text-utf8-invalid"));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.EncodedFrame,
                Is.EqualTo(new byte[]
                {
                    0x80, 0x03, 0x90, 0x80, 0x80
                }));
        });
    }

    [Test]
    public void MessageAndFragmentLimitsRejectBeforeChangingActiveState()
    {
        WebSocketConnectionOptions messageOptions = Options(
            WebSocketRole.Server);
        messageOptions.MaximumFramePayloadLength = 2;
        messageOptions.MaximumMessagePayloadLength = 2;
        messageOptions.AutomaticFramePayloadLength = 1;
        messageOptions.MaximumFragmentCount = 2;
        WebSocketStreamingMessageWriter messageWriter = CreateWriter(
            messageOptions);
        messageWriter.EncodeMessageChunk(
            Chunk(true, false, WebSocketMessageType.Binary, [1]));
        WebSocketStreamingMessageWriteResult tooLarge =
            messageWriter.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [2, 3]));
        WebSocketStreamingMessageWriteResult smaller =
            messageWriter.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [2]));

        WebSocketConnectionOptions fragmentOptions = Options(
            WebSocketRole.Server);
        fragmentOptions.MaximumFramePayloadLength = 2;
        fragmentOptions.MaximumMessagePayloadLength = 2;
        fragmentOptions.AutomaticFramePayloadLength = 1;
        fragmentOptions.MaximumFragmentCount = 2;
        WebSocketStreamingMessageWriter fragmentWriter = CreateWriter(
            fragmentOptions);
        fragmentWriter.EncodeMessageChunk(
            Chunk(true, false, WebSocketMessageType.Binary, [1]));
        WebSocketStreamingMessageWriteResult cannotLeaveOpen =
            fragmentWriter.EncodeMessageChunk(
                Chunk(false, false, WebSocketMessageType.Binary, []));
        WebSocketStreamingMessageWriteResult closesAtLimit =
            fragmentWriter.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [2]));

        Assert.Multiple(() =>
        {
            Assert.That(tooLarge.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(tooLarge.Error.Identifier,
                Is.EqualTo("stream-send-message-payload-limit-exceeded"));
            Assert.That(smaller.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(smaller.FragmentCount, Is.EqualTo(2));
            Assert.That(cannotLeaveOpen.Error.Identifier,
                Is.EqualTo("stream-send-fragment-limit-exceeded"));
            Assert.That(fragmentWriter.HasActiveStreamingMessage(), Is.False);
            Assert.That(closesAtLimit.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void CompressionChoiceAndTypeAreRetainedAcrossContinuations()
    {
        var extension = new StreamingWriterTestExtension();
        WebSocketExtensionNegotiation negotiation = Negotiation(true);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.HasPerMessageDeflate = true;
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketStreamingMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        WebSocketSendChunk firstChunk = Chunk(
            true,
            false,
            WebSocketMessageType.Text,
            [65]);
        firstChunk.Compress = true;
        WebSocketSendChunk finalChunk = Chunk(
            false,
            true,
            WebSocketMessageType.Binary,
            [66]);
        finalChunk.Compress = false;

        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(firstChunk);
        WebSocketStreamingMessageWriteResult final =
            writer.EncodeMessageChunk(finalChunk);

        Assert.Multiple(() =>
        {
            Assert.That(first.EncodedFrame[0], Is.EqualTo(0x41));
            Assert.That(final.EncodedFrame[0], Is.EqualTo(0x80));
            Assert.That(final.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(extension.ObservedCompression,
                Is.EqualTo(new[] { true, true }));
            Assert.That(extension.ObservedMessageTypes,
                Is.EqualTo(new[]
                {
                    WebSocketMessageType.Text,
                    WebSocketMessageType.Text
                }));
            Assert.That(extension.ObservedFirst,
                Is.EqualTo(new[] { true, false }));
            Assert.That(extension.ObservedFinal,
                Is.EqualTo(new[] { false, true }));
            Assert.That(extension.CompleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NegotiatedPerMessageDeflateRoundTripsStreamingFragments()
    {
        var compressionOptions = new PerMessageDeflateOptions();
        compressionOptions.CompressionThreshold = 0;
        compressionOptions.MaximumDecompressedMessagePayloadLength = 32;
        var clientExtension = new PerMessageDeflateExtension(
            compressionOptions);
        var serverExtension = new PerMessageDeflateExtension(
            compressionOptions);
        WebSocketExtensionOffer offer =
            clientExtension.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation accepted =
            serverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            clientExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        WebSocketConnectionOptions clientOptions = Options(
            WebSocketRole.Client);
        clientOptions.MaximumFramePayloadLength = 32;
        clientOptions.MaximumMessagePayloadLength = 64;
        clientOptions.MaximumDecompressedMessagePayloadLength = 64;
        clientOptions.MaximumFragmentCount = 8;
        clientOptions.AutomaticFramePayloadLength = 8;
        clientOptions.HasPerMessageDeflate = true;
        clientOptions.PerMessageDeflate = compressionOptions;
        clientOptions.Extensions = [clientExtension];
        clientOptions.NegotiatedExtensions = [validated];
        clientOptions.HasMaskKeySource = true;
        clientOptions.MaskKeySource = new WholeWriterMaskKeySource();
        var writer = new WebSocketStreamingMessageWriter(
            clientOptions,
            new WebSocketExtensionPipeline(
                [clientExtension],
                [validated]));
        WebSocketSendChunk firstChunk = Chunk(
            true,
            false,
            WebSocketMessageType.Text,
            "hello "u8.ToArray());
        firstChunk.Compress = true;
        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(firstChunk);
        WebSocketStreamingMessageWriteResult final =
            writer.EncodeMessageChunk(
                Chunk(
                    false,
                    true,
                    WebSocketMessageType.Binary,
                    "world"u8.ToArray()));

        WebSocketConnectionOptions serverOptions = Options(
            WebSocketRole.Server);
        serverOptions.DeliveryMode = WebSocketMessageDeliveryMode.Reassembled;
        serverOptions.MaximumFramePayloadLength = 32;
        serverOptions.MaximumMessagePayloadLength = 64;
        serverOptions.MaximumDecompressedMessagePayloadLength = 64;
        serverOptions.MaximumFragmentCount = 8;
        serverOptions.AutomaticFramePayloadLength = 8;
        serverOptions.HasPerMessageDeflate = true;
        serverOptions.PerMessageDeflate = compressionOptions;
        serverOptions.Extensions = [serverExtension];
        serverOptions.NegotiatedExtensions = [accepted];
        var receiver = new WebSocketReassembledReceiveConsumer(
            serverOptions,
            new WebSocketExtensionPipeline(
                [serverExtension],
                [accepted]));
        var codec = new WebSocketFrameCodec(
            WebSocketRole.Server,
            32,
            [accepted]);
        var pump = new WebSocketInboundFramePump(codec, receiver);
        byte[] wire = Combine(first.EncodedFrame, final.EncodedFrame);

        WebSocketInboundFramePumpResult pumped =
            pump.PumpInboundFrames(wire);
        WebSocketEvent restored = RequiredEvent(
            receiver.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.EncodedFrame[0] & 64, Is.EqualTo(64));
            Assert.That(final.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.EncodedFrame[0] & 64, Is.Zero);
            Assert.That(pumped.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(restored.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(restored.Payload,
                Is.EqualTo("hello world"u8.ToArray()));
        });
    }

    [Test]
    public void ClientUsesFreshMaskForEveryStreamingFragment()
    {
        var source = new WholeWriterMaskKeySource();
        WebSocketConnectionOptions options = Options(WebSocketRole.Client);
        options.HasMaskKeySource = true;
        options.MaskKeySource = source;
        WebSocketStreamingMessageWriter writer = CreateWriter(options);

        WebSocketStreamingMessageWriteResult first =
            writer.EncodeMessageChunk(
                Chunk(true, false, WebSocketMessageType.Binary, [10]));
        WebSocketStreamingMessageWriteResult final =
            writer.EncodeMessageChunk(
                Chunk(false, true, WebSocketMessageType.Binary, [20]));

        Assert.Multiple(() =>
        {
            Assert.That(source.CallCount, Is.EqualTo(2));
            Assert.That(first.EncodedFrame,
                Is.EqualTo(new byte[]
                {
                    0x02, 0x81, 1, 2, 3, 4, 11
                }));
            Assert.That(final.EncodedFrame,
                Is.EqualTo(new byte[]
                {
                    0x80, 0x81, 17, 18, 19, 20, 5
                }));
        });
    }

    [Test]
    public void OversizedTransformedFragmentLatchesWriterState()
    {
        var extension = new StreamingWriterTestExtension();
        extension.Output = new byte[17];
        WebSocketExtensionNegotiation negotiation = Negotiation(false);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketStreamingMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));

        WebSocketStreamingMessageWriteResult failed =
            writer.EncodeMessageChunk(
                Chunk(true, true, WebSocketMessageType.Binary, [1]));
        extension.Output = [2];
        WebSocketStreamingMessageWriteResult latched =
            writer.EncodeMessageChunk(
                Chunk(true, true, WebSocketMessageType.Binary, [2]));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("stream-send-frame-payload-limit-exceeded"));
            Assert.That(latched.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(latched.Error.Identifier,
                Is.EqualTo("stream-send-frame-payload-limit-exceeded"));
            Assert.That(extension.OutboundCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidRequestAndUnavailableCompressionDoNotLatchWriter()
    {
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));
        WebSocketSendChunk badRange = Chunk(
            true,
            true,
            WebSocketMessageType.Binary,
            [1]);
        badRange.PayloadOffset = 2;
        WebSocketSendChunk compressed = Chunk(
            true,
            true,
            WebSocketMessageType.Binary,
            [1]);
        compressed.Compress = true;

        WebSocketStreamingMessageWriteResult range =
            writer.EncodeMessageChunk(badRange);
        WebSocketStreamingMessageWriteResult unavailable =
            writer.EncodeMessageChunk(compressed);
        WebSocketStreamingMessageWriteResult recovered =
            writer.EncodeMessageChunk(
                Chunk(true, true, WebSocketMessageType.Binary, [2]));

        Assert.Multiple(() =>
        {
            Assert.That(range.Error.Identifier,
                Is.EqualTo("stream-send-payload-range-invalid"));
            Assert.That(unavailable.Error.Identifier,
                Is.EqualTo("stream-send-compression-unavailable"));
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(recovered.EncodedFrame,
                Is.EqualTo(new byte[] { 0x82, 0x01, 2 }));
        });
    }

    [Test]
    public void EncodedFragmentDoesNotRetainCallerPayload()
    {
        byte[] payload = [1, 2];
        WebSocketSendChunk chunk = Chunk(
            true,
            true,
            WebSocketMessageType.Binary,
            payload);
        WebSocketStreamingMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));

        WebSocketStreamingMessageWriteResult result =
            writer.EncodeMessageChunk(chunk);
        payload[0] = 9;
        chunk.PayloadCount = 0;

        Assert.That(result.EncodedFrame,
            Is.EqualTo(new byte[] { 0x82, 0x02, 1, 2 }));
    }

    [Test]
    public void ConstructorRequiresAnExtensionPipeline()
    {
        Assert.Throws<WebSocketStateException>(() =>
            new WebSocketStreamingMessageWriter(
                Options(WebSocketRole.Server),
                null));
    }

    static WebSocketStreamingMessageWriter CreateWriter(
        WebSocketConnectionOptions options)
    {
        return new WebSocketStreamingMessageWriter(
            options,
            new WebSocketExtensionPipeline([], []));
    }

    static WebSocketConnectionOptions Options(WebSocketRole role)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            MaximumFramePayloadLength = 16,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumFragmentCount = 8,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 64,
            AutomaticFramePayloadLength = 8
        };
    }

    static WebSocketSendChunk Chunk(
        bool first,
        bool final,
        WebSocketMessageType messageType,
        byte[] payload)
    {
        return new WebSocketSendChunk
        {
            First = first,
            Final = final,
            MessageType = messageType,
            Payload = payload,
            PayloadOffset = 0,
            PayloadCount = payload.Length
        };
    }

    static WebSocketExtensionNegotiation Negotiation(bool rsv1)
    {
        return new WebSocketExtensionNegotiation
        {
            Name = "x-stream-writer",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-stream-writer"
            },
            ClaimsRsv1 = rsv1
        };
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

internal class StreamingWriterTestExtension : IWebSocketExtension
{
    internal byte[] Output = [];
    internal int OutboundCallCount;
    internal int CompleteCount;
    internal List<bool> ObservedCompression = [];
    internal List<WebSocketMessageType> ObservedMessageTypes = [];
    internal List<bool> ObservedFirst = [];
    internal List<bool> ObservedFinal = [];

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = "x-stream-writer" };
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
        OutboundCallCount++;
        ObservedCompression.Add(context.RequestCompression);
        ObservedMessageTypes.Add(context.MessageType);
        ObservedFirst.Add(context.FirstChunk);
        ObservedFinal.Add(context.FinalChunk);
        byte[] output = source.ToArray();
        if (Output.Length != 0)
        {
            output = Output.ToArray();
        }

        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output,
            SetRsv1 = context.FirstChunk && context.RequestCompression
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
        CompleteCount++;
    }

    public void ResetExtensionState()
    {
    }
}
