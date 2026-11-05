using websocket;

namespace websocket.Tests;

public class WebSocketConnectionSendTests
{
    [TestCase(WebSocketRole.Client, false)]
    [TestCase(WebSocketRole.Client, true)]
    [TestCase(WebSocketRole.Server, false)]
    [TestCase(WebSocketRole.Server, true)]
    public void WholeMessagesRoundTripWithAutomaticOrRequestedFragmentation(
        WebSocketRole senderRole,
        bool requestedFrameSize)
    {
        var maskSource = new SendTestMaskKeySource();
        WebSocketConnection sender = Connection(
            senderRole,
            128,
            3,
            maskSource);
        WebSocketConnection receiver = Connection(
            OtherRole(senderRole),
            128,
            3,
            null);
        byte[] payload = [0, 1, 2, 3, 4, 5, 6, 7];
        WebSocketSendRequest request = Request(
            WebSocketMessageType.Binary,
            payload);
        int framePayloadLength = 3;
        if (requestedFrameSize)
        {
            request.HasFramePayloadLength = true;
            request.FramePayloadLength = 2;
            framePayloadLength = 2;
        }

        WebSocketParseStatus sent = sender.SendWholeMessage(request);
        byte[] wire = DrainBounded(sender, [1, 2, 5, 3]);
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? message = receiver.ReadNextEvent();
        List<ObservedSendFrame> frames = ObserveFrames(wire);
        int expectedFrameCount =
            (payload.Length + framePayloadLength - 1) / framePayloadLength;

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(expectedFrameCount));
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.MessageType,
                Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(EventPayload(message), Is.EqualTo(payload));
            Assert.That(frames, Has.Count.EqualTo(expectedFrameCount));
            Assert.That(frames[0].Opcode, Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(frames.Skip(1).Select(item => item.Opcode),
                Is.All.EqualTo(WebSocketOpcode.Continuation));
            Assert.That(frames.Take(frames.Count - 1).Select(item => item.Final),
                Is.All.False);
            Assert.That(frames[frames.Count - 1].Final, Is.True);
            Assert.That(frames.Select(item => item.Masked),
                Is.All.EqualTo(senderRole == WebSocketRole.Client));
            Assert.That(sender.GetConnectionInfo().SentFrameCount,
                Is.EqualTo(expectedFrameCount));
            Assert.That(sender.GetConnectionInfo().SentMessageCount,
                Is.EqualTo(1));
            Assert.That(maskSource.CallCount,
                Is.EqualTo(senderRole == WebSocketRole.Client
                    ? expectedFrameCount
                    : 0));
        });
    }

    [Test]
    public void ChunkedClientSendResumesEachPendingFragmentAfterBoundedDrains()
    {
        var maskSource = new SendTestMaskKeySource();
        WebSocketConnection sender = Connection(
            WebSocketRole.Client,
            18,
            4,
            maskSource);
        WebSocketConnection receiver = Connection(
            WebSocketRole.Server,
            128,
            4,
            null);
        WebSocketSendChunk first = Chunk(true, false, [0, 1, 2, 3]);
        WebSocketSendChunk middle = Chunk(false, false, [4, 5, 6, 7]);
        WebSocketSendChunk final = Chunk(false, true, [8, 9, 10]);

        WebSocketParseStatus firstStatus = sender.SendMessageChunk(first);
        WebSocketParseStatus middleBlocked = sender.SendMessageChunk(middle);
        WebSocketParseStatus competing = sender.SendMessageChunk(
            Chunk(false, false, [99]));
        byte[] firstWire = DrainBounded(sender, [1, 3, 2]);
        WebSocketParseStatus middleStatus = sender.SendMessageChunk(middle);
        WebSocketParseStatus finalBlocked = sender.SendMessageChunk(final);
        byte[] middleWire = DrainBounded(sender, [2, 1, 4]);
        WebSocketParseStatus finalStatus = sender.SendMessageChunk(final);
        byte[] finalWire = DrainBounded(sender, [1]);
        byte[] wire = Combine(firstWire, middleWire, finalWire);
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? message = receiver.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(firstStatus, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(middleBlocked,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(competing,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(middleStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finalBlocked,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(finalStatus, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.FrameCount, Is.EqualTo(3));
            Assert.That(message, Is.Not.Null);
            Assert.That(EventPayload(message!),
                Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.That(sender.GetConnectionInfo().SentFrameCount, Is.EqualTo(3));
            Assert.That(sender.GetConnectionInfo().SentMessageCount, Is.EqualTo(1));
            Assert.That(maskSource.CallCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void TextValidationRejectsBadWholeAndFinalChunksButAllowsRetry()
    {
        WebSocketConnection sender = Connection(
            WebSocketRole.Server,
            128,
            8,
            null);
        WebSocketConnection receiver = Connection(
            WebSocketRole.Client,
            128,
            8,
            null);
        WebSocketSendRequest invalidWhole = Request(
            WebSocketMessageType.Text,
            [0xc0]);
        WebSocketSendRequest validWhole = Request(
            WebSocketMessageType.Text,
            "ok"u8.ToArray());
        WebSocketSendChunk first = TextChunk(true, false, [0xe2]);
        WebSocketSendChunk invalidFinal = TextChunk(false, true, [0x28]);
        WebSocketSendChunk validFinal = TextChunk(
            false,
            true,
            [0x82, 0xac]);

        WebSocketParseStatus wholeRejected =
            sender.SendWholeMessage(invalidWhole);
        WebSocketParseStatus wholeAccepted =
            sender.SendWholeMessage(validWhole);
        WebSocketParseStatus firstAccepted = sender.SendMessageChunk(first);
        WebSocketParseStatus finalRejected =
            sender.SendMessageChunk(invalidFinal);
        WebSocketParseStatus finalAccepted = sender.SendMessageChunk(validFinal);
        byte[] wire = DrainBounded(sender, [2, 1, 3]);
        WebSocketInboundFramePumpResult received =
            receiver.ReceiveConnectionBytes(wire);
        WebSocketEvent? wholeMessage = receiver.ReadNextEvent();
        WebSocketEvent? streamedMessage = receiver.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(wholeRejected,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(wholeAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(firstAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finalRejected,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(finalAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(wholeMessage, Is.Not.Null);
            Assert.That(EventPayload(wholeMessage!), Is.EqualTo("ok"u8.ToArray()));
            Assert.That(streamedMessage, Is.Not.Null);
            Assert.That(EventPayload(streamedMessage!),
                Is.EqualTo(new byte[] { 0xe2, 0x82, 0xac }));
            Assert.That(sender.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(sender.GetConnectionInfo().SentMessageCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void PerMessageDeflateSelectionChangesRsv1AndRoundTripsBothMessages()
    {
        CompressedConnectionPair pair = CompressedPair();
        byte[] payload = "compress me compress me compress me"u8.ToArray();
        WebSocketSendRequest plainRequest = Request(
            WebSocketMessageType.Text,
            payload);
        WebSocketSendRequest compressedRequest = Request(
            WebSocketMessageType.Text,
            payload);
        compressedRequest.Compress = true;

        WebSocketParseStatus plainStatus =
            pair.Client.SendWholeMessage(plainRequest);
        byte[] plainWire = DrainBounded(pair.Client, [3, 1, 5]);
        WebSocketInboundFramePumpResult plainReceived =
            pair.Server.ReceiveConnectionBytes(plainWire);
        WebSocketEvent? plainMessage = pair.Server.ReadNextEvent();
        WebSocketParseStatus compressedStatus =
            pair.Client.SendWholeMessage(compressedRequest);
        byte[] compressedWire = DrainBounded(pair.Client, [1, 2, 4]);
        WebSocketInboundFramePumpResult compressedReceived =
            pair.Server.ReceiveConnectionBytes(compressedWire);
        WebSocketEvent? compressedMessage = pair.Server.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(plainStatus, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(compressedStatus,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(plainWire[0] & 0x40, Is.Zero);
            Assert.That(compressedWire[0] & 0x40, Is.EqualTo(0x40));
            Assert.That(compressedWire.Length, Is.LessThan(plainWire.Length));
            Assert.That(plainReceived.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(compressedReceived.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(plainMessage, Is.Not.Null);
            Assert.That(EventPayload(plainMessage!), Is.EqualTo(payload));
            Assert.That(compressedMessage, Is.Not.Null);
            Assert.That(EventPayload(compressedMessage!), Is.EqualTo(payload));
            Assert.That(pair.Client.GetConnectionInfo().SentMessageCount,
                Is.EqualTo(2));
            Assert.That(pair.Server.GetConnectionInfo().ReceivedMessageCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void OversizedRequestedFramesAndChunksRejectBeforePendingForever()
    {
        WebSocketConnection wholeConnection = Connection(
            WebSocketRole.Client,
            18,
            4,
            new SendTestMaskKeySource());
        WebSocketSendRequest oversizedRequest = Request(
            WebSocketMessageType.Binary,
            new byte[17]);
        oversizedRequest.HasFramePayloadLength = true;
        oversizedRequest.FramePayloadLength = 17;
        WebSocketSendRequest validRequest = Request(
            WebSocketMessageType.Binary,
            [1, 2, 3, 4]);

        WebSocketParseStatus wholeRejected =
            wholeConnection.SendWholeMessage(oversizedRequest);
        WebSocketParseStatus wholeAccepted =
            wholeConnection.SendWholeMessage(validRequest);

        WebSocketConnection chunkConnection = Connection(
            WebSocketRole.Client,
            18,
            4,
            new SendTestMaskKeySource());
        WebSocketParseStatus chunkRejected = chunkConnection.SendMessageChunk(
            Chunk(true, true, new byte[17]));
        WebSocketParseStatus chunkAccepted = chunkConnection.SendMessageChunk(
            Chunk(true, true, [5, 6, 7, 8]));

        Assert.Multiple(() =>
        {
            Assert.That(wholeRejected,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(wholeAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(wholeConnection.GetPendingConnectionWriteCount(),
                Is.GreaterThan(0));
            Assert.That(wholeConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(chunkRejected,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(chunkAccepted,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(chunkConnection.GetPendingConnectionWriteCount(),
                Is.GreaterThan(0));
            Assert.That(chunkConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    [Test]
    public void ExpandedStreamingFragmentThatCannotBeScheduledFailsDeterministically()
    {
        var trace = new List<string>();
        var extension = new SyntheticLifecycleExtension(
            "x-expand",
            "expand",
            0xaa,
            trace);
        WebSocketExtensionNegotiation negotiation =
            extension.AcceptClientExtensionOffer(
                new WebSocketExtensionOffer { Name = "x-expand" });
        var options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Server,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            Extensions = [extension],
            MaximumFramePayloadLength = 32,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumFragmentCount = 16,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 18,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 4,
            CloseTimeoutMilliseconds = 1000
        };
        var connection = new WebSocketConnection(
            options,
            Handshake([negotiation]));

        WebSocketParseStatus status = connection.SendMessageChunk(
            Chunk(true, true, [1, 2, 3, 4]));
        WebSocketEvent? error = connection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(connection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(connection.SendMessageChunk(
                    Chunk(true, true, [1])),
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Type, Is.EqualTo(WebSocketEventType.Error));
            Assert.That(error.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(error.Error.Identifier,
                Is.EqualTo("stream-send-frame-payload-limit-exceeded"));
            Assert.That(error.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
        });
    }

    static WebSocketConnection Connection(
        WebSocketRole role,
        int outputCapacity,
        int automaticFramePayloadLength,
        IWebSocketMaskKeySource? maskSource)
    {
        var options = new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            MaximumFramePayloadLength = 32,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 128,
            MaximumFragmentCount = 32,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = outputCapacity,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = automaticFramePayloadLength,
            CloseTimeoutMilliseconds = 1000
        };
        if (role == WebSocketRole.Client && maskSource != null)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = maskSource;
        }

        return new WebSocketConnection(options, Handshake([]));
    }

    static CompressedConnectionPair CompressedPair()
    {
        var compressionOptions = new PerMessageDeflateOptions
        {
            CompressionThreshold = 0,
            MaximumDecompressedMessagePayloadLength = 128
        };
        var clientExtension = new PerMessageDeflateExtension(compressionOptions);
        var serverExtension = new PerMessageDeflateExtension(compressionOptions);
        WebSocketExtensionOffer offer =
            clientExtension.CreateClientExtensionOffer();
        WebSocketExtensionNegotiation accepted =
            serverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            clientExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        var clientOptions = CompressionConnectionOptions(
            WebSocketRole.Client,
            compressionOptions,
            clientExtension);
        clientOptions.HasMaskKeySource = true;
        clientOptions.MaskKeySource = new SendTestMaskKeySource();
        var serverOptions = CompressionConnectionOptions(
            WebSocketRole.Server,
            compressionOptions,
            serverExtension);

        return new CompressedConnectionPair
        {
            Client = new WebSocketConnection(
                clientOptions,
                Handshake([validated])),
            Server = new WebSocketConnection(
                serverOptions,
                Handshake([accepted]))
        };
    }

    static WebSocketConnectionOptions CompressionConnectionOptions(
        WebSocketRole role,
        PerMessageDeflateOptions compressionOptions,
        IWebSocketExtension extension)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            Extensions = [extension],
            HasPerMessageDeflate = true,
            PerMessageDeflate = compressionOptions,
            MaximumFramePayloadLength = 64,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            MaximumFragmentCount = 32,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 8,
            AutomaticFramePayloadLength = 64,
            CloseTimeoutMilliseconds = 1000
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

    static WebSocketRole OtherRole(WebSocketRole role)
    {
        if (role == WebSocketRole.Client)
        {
            return WebSocketRole.Server;
        }

        return WebSocketRole.Client;
    }

    static WebSocketSendRequest Request(
        WebSocketMessageType messageType,
        byte[] payload)
    {
        return new WebSocketSendRequest
        {
            MessageType = messageType,
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

    static WebSocketSendChunk TextChunk(
        bool first,
        bool final,
        byte[] payload)
    {
        WebSocketSendChunk chunk = Chunk(first, final, payload);
        chunk.MessageType = WebSocketMessageType.Text;
        return chunk;
    }

    static byte[] DrainBounded(WebSocketConnection connection, int[] sizes)
    {
        var output = new List<byte>();
        int index = 0;
        while (connection.GetPendingConnectionWriteCount() != 0)
        {
            byte[] destination = new byte[sizes[index % sizes.Length]];
            int written = connection.WriteConnectionBytes(destination);
            output.AddRange(destination.AsSpan(0, written).ToArray());
            index++;
        }

        return output.ToArray();
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

    static byte[] EventPayload(WebSocketEvent item)
    {
        return item.Payload
            .AsSpan(item.PayloadOffset, item.PayloadCount)
            .ToArray();
    }

    static List<ObservedSendFrame> ObserveFrames(byte[] wire)
    {
        var frames = new List<ObservedSendFrame>();
        int offset = 0;
        while (offset < wire.Length)
        {
            int payloadLength = wire[offset + 1] & 0x7f;
            bool masked = (wire[offset + 1] & 0x80) != 0;
            int headerLength = masked ? 6 : 2;
            frames.Add(new ObservedSendFrame
            {
                Final = (wire[offset] & 0x80) != 0,
                Opcode = (WebSocketOpcode)(wire[offset] & 0x0f),
                Masked = masked,
                PayloadLength = payloadLength
            });
            offset += headerLength + payloadLength;
        }

        return frames;
    }
}

internal class ObservedSendFrame
{
    internal bool Final;
    internal WebSocketOpcode Opcode;
    internal bool Masked;
    internal int PayloadLength;
}

internal class CompressedConnectionPair
{
    internal WebSocketConnection Client = null!;
    internal WebSocketConnection Server = null!;
}

internal class SendTestMaskKeySource : IWebSocketMaskKeySource
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
