using websocket;

namespace websocket.Tests;

public class PerMessageDeflateExtensionTests
{
    [Test]
    public void ConstructorMapsInvalidOptionsToExtensionException()
    {
        PerMessageDeflateOptions invalid = new PerMessageDeflateOptions();
        invalid.CompressionThreshold = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketExtensionException>(() =>
                new PerMessageDeflateExtension(null!));
            Assert.Throws<WebSocketExtensionException>(() =>
                new PerMessageDeflateExtension(invalid));
        });
    }

    [Test]
    public void ClientAndServerNegotiateAndRetainCopiedConfiguration()
    {
        var clientOptions = new PerMessageDeflateOptions();
        clientOptions.OfferClientMaxWindowBits = true;
        clientOptions.HasClientMaxWindowBits = true;
        clientOptions.ClientMaxWindowBits = 12;
        var client = new PerMessageDeflateExtension(clientOptions);
        WebSocketExtensionOffer offer = client.CreateClientExtensionOffer();
        clientOptions.ClientMaxWindowBits = 8;
        offer.Parameters[0].Value = "9";

        WebSocketExtensionOffer retainedOffer =
            client.CreateClientExtensionOffer();
        var serverOptions = new PerMessageDeflateOptions();
        serverOptions.OfferClientMaxWindowBits = true;
        serverOptions.HasClientMaxWindowBits = true;
        serverOptions.ClientMaxWindowBits = 11;
        var server = new PerMessageDeflateExtension(serverOptions);
        WebSocketExtensionNegotiation accepted =
            server.AcceptClientExtensionOffer(retainedOffer);
        WebSocketExtensionNegotiation validated =
            client.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(retainedOffer.Parameters[0].Value, Is.EqualTo("12"));
            Assert.That(accepted.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(accepted.ClaimsRsv1, Is.True);
            Assert.That(accepted.AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("11"));
            Assert.That(validated.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(validated.ClaimsRsv1, Is.True);
        });
    }

    [Test]
    public void CompressedTextRoundTripsWithSingleByteOutputCapacity()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        byte[] source = "hello \u20ac per-message deflate"u8.ToArray();
        WebSocketExtensionContext outbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Text,
            1,
            1000);
        outbound.RequestCompression = true;
        bool rsv1;

        byte[] compressed = TransformAll(
            client,
            outbound,
            source,
            true,
            out rsv1);
        WebSocketExtensionContext inbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Text,
            1,
            1000);
        inbound.Rsv1 = rsv1;
        bool ignored;
        byte[] restored = TransformAll(
            server,
            inbound,
            compressed,
            false,
            out ignored);

        Assert.Multiple(() =>
        {
            Assert.That(rsv1, Is.True);
            Assert.That(compressed, Is.Not.EqualTo(source));
            Assert.That(restored, Is.EqualTo(source));
        });
    }

    [Test]
    public void CompressionOptOutAndThresholdCopyPayloadWithoutRsv1()
    {
        PerMessageDeflateOptions options = CompressionOptions(10, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        byte[] source = [1, 2, 3, 4, 5];
        WebSocketExtensionContext outbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            2,
            1000);
        outbound.RequestCompression = true;
        bool rsv1;

        byte[] wire = TransformAll(
            client,
            outbound,
            source,
            true,
            out rsv1);
        WebSocketExtensionContext inbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            2,
            1000);
        inbound.Rsv1 = false;
        bool ignored;
        byte[] restored = TransformAll(
            server,
            inbound,
            wire,
            false,
            out ignored);

        Assert.Multiple(() =>
        {
            Assert.That(rsv1, Is.False);
            Assert.That(wire, Is.EqualTo(source));
            Assert.That(restored, Is.EqualTo(source));
        });
    }

    [Test]
    public void CompressionStateSpansMessageChunksAndFragments()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        byte[] firstSource = "first \u20ac "u8.ToArray();
        byte[] secondSource = "second"u8.ToArray();
        WebSocketExtensionContext firstOutbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Text,
            2,
            1000);
        firstOutbound.RequestCompression = true;
        firstOutbound.FinalChunk = false;
        firstOutbound.FinalFragment = false;
        bool firstRsv1;
        byte[] firstWire = TransformAll(
            client,
            firstOutbound,
            firstSource,
            true,
            out firstRsv1);
        WebSocketExtensionContext secondOutbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Text,
            2,
            1000);
        secondOutbound.RequestCompression = true;
        secondOutbound.FirstChunk = false;
        secondOutbound.FirstFragment = false;
        bool secondRsv1;
        byte[] secondWire = TransformAll(
            client,
            secondOutbound,
            secondSource,
            true,
            out secondRsv1);

        WebSocketExtensionContext firstInbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Text,
            2,
            1000);
        firstInbound.Rsv1 = firstRsv1;
        firstInbound.FinalChunk = false;
        firstInbound.FinalFragment = false;
        bool ignored;
        byte[] firstRestored = TransformAll(
            server,
            firstInbound,
            firstWire,
            false,
            out ignored);
        WebSocketExtensionContext secondInbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Text,
            2,
            1000);
        secondInbound.FirstChunk = false;
        secondInbound.FirstFragment = false;
        secondInbound.Rsv1 = false;
        byte[] secondRestored = TransformAll(
            server,
            secondInbound,
            secondWire,
            false,
            out ignored);
        byte[] restored = new byte[firstRestored.Length + secondRestored.Length];
        firstRestored.CopyTo(restored, 0);
        secondRestored.CopyTo(restored, firstRestored.Length);
        byte[] expected = new byte[firstSource.Length + secondSource.Length];
        firstSource.CopyTo(expected, 0);
        secondSource.CopyTo(expected, firstSource.Length);

        Assert.Multiple(() =>
        {
            Assert.That(firstRsv1, Is.True);
            Assert.That(secondRsv1, Is.False);
            Assert.That(restored, Is.EqualTo(expected));
        });
    }

    [Test]
    public void DecompressedMessageLimitMapsToMessageTooBig()
    {
        PerMessageDeflateOptions clientOptions = CompressionOptions(0, 1000);
        PerMessageDeflateOptions serverOptions = CompressionOptions(0, 4);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(clientOptions, serverOptions, out client, out server);
        WebSocketExtensionContext outbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            1000);
        outbound.RequestCompression = true;
        bool rsv1;
        byte[] compressed = TransformAll(
            client,
            outbound,
            [1, 2, 3, 4, 5],
            true,
            out rsv1);
        WebSocketExtensionContext inbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            8,
            4);
        inbound.Rsv1 = rsv1;

        WebSocketTransformResult result =
            server.TransformInboundChunk(inbound, compressed);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(result.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.MessageTooBig));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
        });
    }

    [Test]
    public void InvalidCompressedTextMapsToInvalidPayloadData()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        WebSocketExtensionContext outbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            1000);
        outbound.RequestCompression = true;
        bool rsv1;
        byte[] compressed = TransformAll(
            client,
            outbound,
            [0xff],
            true,
            out rsv1);
        WebSocketExtensionContext inbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Text,
            8,
            1000);
        inbound.Rsv1 = rsv1;

        WebSocketTransformResult result =
            server.TransformInboundChunk(inbound, compressed);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(result.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.InvalidFramePayloadData));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("permessage-deflate-text-utf8-invalid"));
        });
    }

    [Test]
    public void ResetClearsNegotiationAndTransformState()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        var extension = new PerMessageDeflateExtension(options);
        extension.CreateClientExtensionOffer();
        WebSocketExtensionOffer response = new WebSocketExtensionOffer();
        response.Name = "permessage-deflate";
        extension.ValidateServerExtensionResponse(response);
        extension.ResetExtensionState();
        WebSocketExtensionContext context = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            1000);
        context.RequestCompression = true;

        WebSocketTransformResult result =
            extension.TransformOutboundChunk(context, [1]);
        WebSocketExtensionNegotiation validation =
            extension.ValidateServerExtensionResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("permessage-deflate-transform-context-invalid"));
            Assert.That(validation.Status,
                Is.EqualTo(WebSocketExtensionStatus.Invalid));
        });
    }

    [Test]
    public void InvalidOffersAndResponsesClearNegotiatedState()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension server =
            new PerMessageDeflateExtension(options);
        WebSocketExtensionNegotiation declined =
            server.AcceptClientExtensionOffer(new WebSocketExtensionOffer
            {
                Name = "other"
            });
        PerMessageDeflateExtension client =
            new PerMessageDeflateExtension(options);
        client.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation invalid =
            client.ValidateServerExtensionResponse(new WebSocketExtensionOffer
            {
                Name = "other"
            });

        Assert.Multiple(() =>
        {
            Assert.That(declined.Status,
                Is.EqualTo(WebSocketExtensionStatus.Declined));
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketExtensionStatus.Invalid));
        });
    }

    [Test]
    public void TransformRejectsOrphanContinuationsAndChangedLimits()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        WebSocketExtensionContext orphanOutbound = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            1000);
        orphanOutbound.FirstChunk = false;
        orphanOutbound.FirstFragment = false;
        WebSocketTransformResult outboundFailure =
            client.TransformOutboundChunk(orphanOutbound, [1]);
        WebSocketExtensionContext orphanInbound = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            8,
            1000);
        orphanInbound.FirstChunk = false;
        orphanInbound.FirstFragment = false;
        orphanInbound.Rsv1 = true;
        WebSocketTransformResult inboundFailure =
            server.TransformInboundChunk(orphanInbound, [1]);

        PerMessageDeflateExtension initializedClient;
        PerMessageDeflateExtension initializedServer;
        Negotiate(
            options,
            options,
            out initializedClient,
            out initializedServer);
        WebSocketExtensionContext initialized = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            1000);
        initialized.RequestCompression = false;
        WebSocketTransformResult initializedResult =
            initializedClient.TransformOutboundChunk(initialized, [1]);
        initializedClient.CompleteExtensionMessage(initialized);
        WebSocketExtensionContext changed = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            8,
            999);
        changed.RequestCompression = false;
        WebSocketTransformResult changedFailure =
            initializedClient.TransformOutboundChunk(changed, [2]);

        Assert.Multiple(() =>
        {
            Assert.That(outboundFailure.HasError, Is.True);
            Assert.That(outboundFailure.Error.Identifier,
                Is.EqualTo("permessage-deflate-outgoing-state-invalid"));
            Assert.That(inboundFailure.HasError, Is.True);
            Assert.That(inboundFailure.Error.Identifier,
                Is.EqualTo("permessage-deflate-continuation-rsv1-invalid"));
            Assert.That(initializedResult.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(changedFailure.HasError, Is.True);
            Assert.That(changedFailure.Error.Identifier,
                Is.EqualTo("permessage-deflate-transform-limits-changed"));
        });
    }

    [Test]
    public void TransformHandlesNonFinalThresholdAndCapsLargeOutputBuffers()
    {
        PerMessageDeflateOptions options = CompressionOptions(10, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        WebSocketExtensionContext context = Context(
            WebSocketRole.Client,
            true,
            WebSocketMessageType.Binary,
            9000,
            1000);
        context.RequestCompression = true;
        context.FinalChunk = false;
        context.FinalFragment = false;

        WebSocketTransformResult result =
            client.TransformOutboundChunk(context, [1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Output, Has.Length.EqualTo(8192));
            Assert.That(result.SetRsv1, Is.True);
        });
    }

    [Test]
    public void InboundTransformRejectsNullContextAndMalformedCompressedBytes()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension client;
        PerMessageDeflateExtension server;
        Negotiate(options, options, out client, out server);
        WebSocketTransformResult invalidContext =
            server.TransformInboundChunk(null!, []);
        WebSocketExtensionContext malformedContext = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            16,
            1000);
        malformedContext.Rsv1 = true;
        WebSocketTransformResult malformed =
            server.TransformInboundChunk(malformedContext, [0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(invalidContext.HasError, Is.True);
            Assert.That(invalidContext.Error.Identifier,
                Is.EqualTo("permessage-deflate-transform-context-invalid"));
            Assert.That(malformed.HasError, Is.True);
            Assert.That(malformed.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Compression));
        });
    }

    [Test]
    public void InboundTransformRejectsUnexpectedContinuationAndTruncatedEmptyStream()
    {
        PerMessageDeflateOptions options = CompressionOptions(0, 1000);
        PerMessageDeflateExtension firstClient;
        PerMessageDeflateExtension continuationServer;
        Negotiate(options, options, out firstClient, out continuationServer);
        WebSocketExtensionContext continuation = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            16,
            1000);
        continuation.FirstChunk = false;
        continuation.FirstFragment = false;
        WebSocketTransformResult unexpected =
            continuationServer.TransformInboundChunk(continuation, []);

        PerMessageDeflateExtension secondClient;
        PerMessageDeflateExtension truncatedServer;
        Negotiate(options, options, out secondClient, out truncatedServer);
        WebSocketExtensionContext truncated = Context(
            WebSocketRole.Server,
            false,
            WebSocketMessageType.Binary,
            16,
            1000);
        truncated.Rsv1 = true;
        WebSocketTransformResult incomplete =
            truncatedServer.TransformInboundChunk(truncated, []);

        Assert.Multiple(() =>
        {
            Assert.That(unexpected.HasError, Is.True);
            Assert.That(unexpected.Error.Identifier,
                Is.EqualTo("permessage-deflate-continuation-unexpected"));
            Assert.That(incomplete.HasError, Is.True);
            Assert.That(incomplete.Error.Identifier,
                Is.EqualTo("deflate-input-truncated"));
        });
    }

    static PerMessageDeflateOptions CompressionOptions(int threshold, int limit)
    {
        var options = new PerMessageDeflateOptions();
        options.CompressionThreshold = threshold;
        options.MaximumDecompressedMessagePayloadLength = limit;
        return options;
    }

    static void Negotiate(
        PerMessageDeflateOptions clientOptions,
        PerMessageDeflateOptions serverOptions,
        out PerMessageDeflateExtension client,
        out PerMessageDeflateExtension server)
    {
        client = new PerMessageDeflateExtension(clientOptions);
        server = new PerMessageDeflateExtension(serverOptions);
        WebSocketExtensionOffer offer = client.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation accepted =
            server.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            client.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        Assert.That(accepted.Status,
            Is.EqualTo(WebSocketExtensionStatus.Negotiated));
        Assert.That(validated.Status,
            Is.EqualTo(WebSocketExtensionStatus.Negotiated));
    }

    static WebSocketExtensionContext Context(
        WebSocketRole role,
        bool outbound,
        WebSocketMessageType messageType,
        int outputLimit,
        int decompressedLimit)
    {
        var context = new WebSocketExtensionContext();
        context.Role = role;
        context.Outbound = outbound;
        context.MessageType = messageType;
        context.FirstChunk = true;
        context.FinalChunk = true;
        context.FirstFragment = true;
        context.FinalFragment = true;
        context.MaximumBufferedOutputBytes = outputLimit;
        context.MaximumDecompressedMessagePayloadLength = decompressedLimit;
        return context;
    }

    static byte[] TransformAll(
        PerMessageDeflateExtension extension,
        WebSocketExtensionContext context,
        byte[] source,
        bool outbound,
        out bool rsv1)
    {
        var output = new List<byte>();
        int sourceOffset = 0;
        int callCount = 0;
        rsv1 = false;
        while (true)
        {
            WebSocketTransformResult result;
            if (outbound)
            {
                result = extension.TransformOutboundChunk(
                    context,
                    source.AsSpan(sourceOffset));
            }
            else
            {
                result = extension.TransformInboundChunk(
                    context,
                    source.AsSpan(sourceOffset));
            }

            Assert.That(result.HasError, Is.False, result.Error.Identifier);
            Assert.That(result.ProducedCount,
                Is.LessThanOrEqualTo(context.MaximumBufferedOutputBytes));
            sourceOffset = sourceOffset + result.ConsumedCount;
            int outputIndex = 0;
            while (outputIndex < result.ProducedCount)
            {
                output.Add(result.Output[outputIndex]);
                outputIndex++;
            }

            rsv1 = rsv1 || result.SetRsv1;
            if (result.Status == WebSocketParseStatus.Complete)
            {
                Assert.That(sourceOffset, Is.EqualTo(source.Length));
                extension.CompleteExtensionMessage(context);
                return output.ToArray();
            }

            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            callCount++;
            Assert.That(callCount, Is.LessThan(10000));
        }
    }
}
