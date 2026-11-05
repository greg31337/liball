using websocket;

namespace websocket.Tests;

public class WebSocketWholeMessageWriterTests
{
    [Test]
    public void ServerFragmentsBinaryMessageAtAutomaticFrameSize()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.AutomaticFramePayloadLength = 2;
        WebSocketWholeMessageWriter writer = CreateWriter(options);
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4, 5]);

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.FrameCount, Is.EqualTo(3));
            Assert.That(result.EncodedCount, Is.EqualTo(11));
            Assert.That(result.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x02, 0x02, 1, 2 }));
            Assert.That(result.EncodedFrames[1],
                Is.EqualTo(new byte[] { 0x00, 0x02, 3, 4 }));
            Assert.That(result.EncodedFrames[2],
                Is.EqualTo(new byte[] { 0x80, 0x01, 5 }));
        });
    }

    [Test]
    public void PerRequestFrameSizeOverridesTheAutomaticSize()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.AutomaticFramePayloadLength = 3;
        WebSocketWholeMessageWriter writer = CreateWriter(options);
        WebSocketSendRequest automatic = Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4, 5]);
        WebSocketSendRequest overridden = Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4, 5]);
        overridden.HasFramePayloadLength = true;
        overridden.FramePayloadLength = 2;

        WebSocketWholeMessageWriteResult first =
            writer.EncodeWholeMessage(automatic);
        WebSocketWholeMessageWriteResult second =
            writer.EncodeWholeMessage(overridden);

        Assert.Multiple(() =>
        {
            Assert.That(first.FrameCount, Is.EqualTo(2));
            Assert.That(FramePayloadCounts(first), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(second.FrameCount, Is.EqualTo(3));
            Assert.That(FramePayloadCounts(second),
                Is.EqualTo(new[] { 2, 2, 1 }));
        });
    }

    [Test]
    public void EmptyTextMessageUsesOneFinalTextFrame()
    {
        WebSocketWholeMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Text, []));

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameCount, Is.EqualTo(1));
            Assert.That(result.EncodedCount, Is.EqualTo(2));
            Assert.That(result.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x81, 0x00 }));
        });
    }

    [Test]
    public void InvalidTextIsRejectedBeforeExtensionsAndDoesNotLatchWriter()
    {
        var extension = new WholeWriterTestExtension();
        WebSocketExtensionNegotiation negotiation = Negotiation(false);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));

        WebSocketWholeMessageWriteResult invalid =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Text, [0xC0]));
        WebSocketWholeMessageWriteResult valid =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Text, [65]));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(invalid.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(invalid.Error.Identifier,
                Is.EqualTo("send-text-utf8-invalid"));
            Assert.That(extension.OutboundCallCount, Is.EqualTo(1));
            Assert.That(valid.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(valid.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x81, 0x01, 65 }));
        });
    }

    [Test]
    public void OptionalCompressionTransformsBeforeFragmentingAndSetsRsvOnce()
    {
        var extension = new WholeWriterTestExtension();
        extension.CompressedOutput = [70, 71, 72];
        WebSocketExtensionNegotiation negotiation = Negotiation(true);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.HasPerMessageDeflate = true;
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        WebSocketSendRequest compressed = Request(
            WebSocketMessageType.Text,
            [65, 66]);
        compressed.Compress = true;
        compressed.HasFramePayloadLength = true;
        compressed.FramePayloadLength = 2;
        WebSocketSendRequest plain = Request(
            WebSocketMessageType.Binary,
            [1]);

        WebSocketWholeMessageWriteResult transformed =
            writer.EncodeWholeMessage(compressed);
        WebSocketWholeMessageWriteResult untransformed =
            writer.EncodeWholeMessage(plain);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.FrameCount, Is.EqualTo(2));
            Assert.That(transformed.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x41, 0x02, 70, 71 }));
            Assert.That(transformed.EncodedFrames[1],
                Is.EqualTo(new byte[] { 0x80, 0x01, 72 }));
            Assert.That(untransformed.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x82, 0x01, 1 }));
            Assert.That(extension.ObservedCompression,
                Is.EqualTo(new[] { true, false }));
            Assert.That(extension.CompleteCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void PartialExtensionOutputIsCollectedBeforeWireFragmentation()
    {
        var extension = new WholeWriterTestExtension();
        extension.SplitOutput = true;
        WebSocketExtensionNegotiation negotiation = Negotiation(false);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Binary,
            [9]);
        request.HasFramePayloadLength = true;
        request.FramePayloadLength = 2;

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.OutboundCallCount, Is.EqualTo(2));
            Assert.That(result.FrameCount, Is.EqualTo(2));
            Assert.That(result.EncodedFrames[0],
                Is.EqualTo(new byte[] { 0x02, 0x02, 10, 11 }));
            Assert.That(result.EncodedFrames[1],
                Is.EqualTo(new byte[] { 0x80, 0x01, 12 }));
        });
    }

    [Test]
    public void NegotiatedPerMessageDeflateRoundTripsThroughTheReceivePipeline()
    {
        var compressionOptions = new PerMessageDeflateOptions();
        compressionOptions.CompressionThreshold = 0;
        compressionOptions.MaximumDecompressedMessagePayloadLength = 16;
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
        clientOptions.HasPerMessageDeflate = true;
        clientOptions.PerMessageDeflate = compressionOptions;
        clientOptions.Extensions = [clientExtension];
        clientOptions.NegotiatedExtensions = [validated];
        clientOptions.HasMaskKeySource = true;
        clientOptions.MaskKeySource = new WholeWriterMaskKeySource();
        clientOptions.AutomaticFramePayloadLength = 4;
        var writer = new WebSocketWholeMessageWriter(
            clientOptions,
            new WebSocketExtensionPipeline(
                [clientExtension],
                [validated]));
        byte[] original = "hello hello"u8.ToArray();
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Text,
            original);
        request.Compress = true;

        WebSocketWholeMessageWriteResult encoded =
            writer.EncodeWholeMessage(request);
        WebSocketConnectionOptions serverOptions = Options(
            WebSocketRole.Server);
        serverOptions.DeliveryMode = WebSocketMessageDeliveryMode.Reassembled;
        serverOptions.HasPerMessageDeflate = true;
        serverOptions.PerMessageDeflate = compressionOptions;
        serverOptions.Extensions = [serverExtension];
        serverOptions.NegotiatedExtensions = [accepted];
        var receiver = new WebSocketReassembledReceiveConsumer(
            serverOptions,
            new WebSocketExtensionPipeline(
                [serverExtension],
                [accepted]));
        var frameCodec = new WebSocketFrameCodec(
            WebSocketRole.Server,
            serverOptions.MaximumFramePayloadLength,
            [accepted]);
        var pump = new WebSocketInboundFramePump(frameCodec, receiver);

        WebSocketInboundFramePumpResult pumped =
            pump.PumpInboundFrames(CombineFrames(encoded.EncodedFrames));
        WebSocketEvent restored = RequiredEvent(
            receiver.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encoded.EncodedFrames[0][0] & 64, Is.EqualTo(64));
            Assert.That(pumped.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(pumped.FrameCount, Is.EqualTo(encoded.FrameCount));
            Assert.That(restored.MessageType,
                Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(restored.Payload, Is.EqualTo(original));
        });
    }

    [Test]
    public void ClientUsesAFreshMaskingKeyForEveryFragment()
    {
        var maskSource = new WholeWriterMaskKeySource();
        WebSocketConnectionOptions options = Options(WebSocketRole.Client);
        options.HasMaskKeySource = true;
        options.MaskKeySource = maskSource;
        options.AutomaticFramePayloadLength = 2;
        WebSocketWholeMessageWriter writer = CreateWriter(options);

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Binary, [10, 20, 30, 40, 50]));

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameCount, Is.EqualTo(3));
            Assert.That(maskSource.CallCount, Is.EqualTo(3));
            Assert.That(result.EncodedFrames[0],
                Is.EqualTo(new byte[]
                {
                    0x02, 0x82, 1, 2, 3, 4, 11, 22
                }));
            Assert.That(result.EncodedFrames[1],
                Is.EqualTo(new byte[]
                {
                    0x00, 0x82, 17, 18, 19, 20, 15, 58
                }));
            Assert.That(result.EncodedFrames[2],
                Is.EqualTo(new byte[]
                {
                    0x80, 0x81, 33, 34, 35, 36, 19
                }));
        });
    }

    [Test]
    public void ClientWithoutCustomSourceStillWritesMaskedFrames()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Client);
        WebSocketWholeMessageWriter writer = CreateWriter(options);

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Binary, [1]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.EncodedFrames[0][1], Is.EqualTo(0x81));
            Assert.That(result.EncodedFrames[0].Length, Is.EqualTo(7));
        });
    }

    [Test]
    public void RequestValidationRejectsBadRangesSentinelsAndCompression()
    {
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        WebSocketWholeMessageWriter writer = CreateWriter(options);
        WebSocketSendRequest badRange = Request(
            WebSocketMessageType.Binary,
            [1]);
        badRange.PayloadOffset = 2;
        WebSocketSendRequest badType = Request(
            (WebSocketMessageType)9,
            [1]);
        WebSocketSendRequest badSentinel = Request(
            WebSocketMessageType.Binary,
            [1]);
        badSentinel.FramePayloadLength = 1;
        WebSocketSendRequest compression = Request(
            WebSocketMessageType.Binary,
            [1]);
        compression.Compress = true;

        WebSocketWholeMessageWriteResult range =
            writer.EncodeWholeMessage(badRange);
        WebSocketWholeMessageWriteResult type =
            writer.EncodeWholeMessage(badType);
        WebSocketWholeMessageWriteResult sentinel =
            writer.EncodeWholeMessage(badSentinel);
        WebSocketWholeMessageWriteResult unavailable =
            writer.EncodeWholeMessage(compression);

        Assert.Multiple(() =>
        {
            Assert.That(range.Error.Identifier,
                Is.EqualTo("send-payload-range-invalid"));
            Assert.That(type.Error.Identifier,
                Is.EqualTo("send-message-type-invalid"));
            Assert.That(sentinel.Error.Identifier,
                Is.EqualTo("send-frame-payload-sentinel-invalid"));
            Assert.That(unavailable.Error.Identifier,
                Is.EqualTo("send-compression-unavailable"));
            Assert.That(range.EncodedFrames, Is.Empty);
            Assert.That(type.Accepted, Is.False);
            Assert.That(sentinel.Accepted, Is.False);
            Assert.That(unavailable.Accepted, Is.False);
        });
    }

    [Test]
    public void PayloadAndFrameLimitsAreCheckedBeforeExtensionCalls()
    {
        var extension = new WholeWriterTestExtension();
        WebSocketExtensionNegotiation negotiation = Negotiation(false);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.MaximumFramePayloadLength = 4;
        options.MaximumMessagePayloadLength = 4;
        options.AutomaticFramePayloadLength = 2;
        options.MaximumFragmentCount = 2;
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        WebSocketSendRequest tooLarge = Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4, 5]);
        WebSocketSendRequest badFrameSize = Request(
            WebSocketMessageType.Binary,
            [1]);
        badFrameSize.HasFramePayloadLength = true;
        badFrameSize.FramePayloadLength = 5;

        WebSocketWholeMessageWriteResult messageLimit =
            writer.EncodeWholeMessage(tooLarge);
        WebSocketWholeMessageWriteResult frameLimit =
            writer.EncodeWholeMessage(badFrameSize);

        Assert.Multiple(() =>
        {
            Assert.That(messageLimit.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(messageLimit.Error.Identifier,
                Is.EqualTo("send-message-payload-limit-exceeded"));
            Assert.That(frameLimit.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(frameLimit.Error.Identifier,
                Is.EqualTo("send-frame-payload-length-invalid"));
            Assert.That(extension.OutboundCallCount, Is.Zero);
        });
    }

    [Test]
    public void ExcessiveTransformedOutputLatchesTheWriter()
    {
        var extension = new WholeWriterTestExtension();
        extension.CompressedOutput = new byte[17];
        WebSocketExtensionNegotiation negotiation = Negotiation(true);
        WebSocketConnectionOptions options = Options(WebSocketRole.Server);
        options.HasPerMessageDeflate = true;
        options.AutomaticFramePayloadLength = 2;
        options.MaximumFragmentCount = 8;
        options.Extensions = [extension];
        options.NegotiatedExtensions = [negotiation];
        var writer = new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline(
                [extension],
                [negotiation]));
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Binary,
            [1]);
        request.Compress = true;

        WebSocketWholeMessageWriteResult failed =
            writer.EncodeWholeMessage(request);
        WebSocketWholeMessageWriteResult latched =
            writer.EncodeWholeMessage(
                Request(WebSocketMessageType.Binary, [2]));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("send-fragment-limit-exceeded"));
            Assert.That(latched.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(latched.Error.Identifier,
                Is.EqualTo("send-fragment-limit-exceeded"));
            Assert.That(extension.OutboundCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void EncodedFramesDoNotRetainTheCallerPayload()
    {
        byte[] payload = [1, 2];
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Binary,
            payload);
        WebSocketWholeMessageWriter writer = CreateWriter(
            Options(WebSocketRole.Server));

        WebSocketWholeMessageWriteResult result =
            writer.EncodeWholeMessage(request);
        payload[0] = 9;
        request.PayloadOffset = 1;
        request.PayloadCount = 0;

        Assert.That(result.EncodedFrames[0],
            Is.EqualTo(new byte[] { 0x82, 0x02, 1, 2 }));
    }

    static WebSocketWholeMessageWriter CreateWriter(
        WebSocketConnectionOptions options)
    {
        return new WebSocketWholeMessageWriter(
            options,
            new WebSocketExtensionPipeline([], []));
    }

    static WebSocketConnectionOptions Options(WebSocketRole role)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            MaximumFramePayloadLength = 8,
            MaximumMessagePayloadLength = 16,
            MaximumDecompressedMessagePayloadLength = 16,
            MaximumFragmentCount = 8,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 64,
            AutomaticFramePayloadLength = 3
        };
    }

    static WebSocketSendRequest Request(
        WebSocketMessageType messageType,
        byte[] payload)
    {
        return new WebSocketSendRequest
        {
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
            Name = "x-whole-writer",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-whole-writer"
            },
            ClaimsRsv1 = rsv1
        };
    }

    static int[] FramePayloadCounts(WebSocketWholeMessageWriteResult result)
    {
        int[] counts = new int[result.FrameCount];
        int index = 0;
        while (index < counts.Length)
        {
            counts[index] = result.EncodedFrames[index][1] & 127;
            index++;
        }

        return counts;
    }

    static byte[] CombineFrames(byte[][] frames)
    {
        int totalCount = 0;
        int index = 0;
        while (index < frames.Length)
        {
            totalCount = totalCount + frames[index].Length;
            index++;
        }

        byte[] combined = new byte[totalCount];
        int offset = 0;
        index = 0;
        while (index < frames.Length)
        {
            frames[index].CopyTo(combined, offset);
            offset = offset + frames[index].Length;
            index++;
        }

        return combined;
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}

internal class WholeWriterTestExtension : IWebSocketExtension
{
    internal byte[] CompressedOutput = [];
    internal bool SplitOutput;
    internal int OutboundCallCount;
    internal int CompleteCount;
    internal List<bool> ObservedCompression = [];

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = "x-whole-writer" };
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
        if (OutboundCallCount == 1 || !SplitOutput)
        {
            ObservedCompression.Add(context.RequestCompression);
        }

        byte[] output = source.ToArray();
        if (context.RequestCompression)
        {
            output = CompressedOutput.ToArray();
        }

        if (SplitOutput)
        {
            output = [12];
            if (OutboundCallCount == 1)
            {
                output = [10, 11];
            }
        }

        var result = new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output,
            SetRsv1 = context.RequestCompression
        };
        if (SplitOutput && OutboundCallCount == 1)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
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

internal class WholeWriterMaskKeySource : IWebSocketMaskKeySource
{
    internal int CallCount;

    public uint CreateMaskingKey()
    {
        CallCount++;
        if (CallCount == 1)
        {
            return 0x01020304U;
        }

        if (CallCount == 2)
        {
            return 0x11121314U;
        }

        return 0x21222324U;
    }
}
