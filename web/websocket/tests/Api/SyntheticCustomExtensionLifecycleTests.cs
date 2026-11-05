using websocket;

namespace websocket.Tests;

public class SyntheticCustomExtensionLifecycleTests
{
    [TestCase(WebSocketHandshakeTransport.Http1Upgrade)]
    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void NegotiatesAndTransformsIncrementalMessagesAcrossEveryTransport(
        WebSocketHandshakeTransport transport)
    {
        SyntheticExtensionPair pair = CreatePair(transport, false);

        WebSocketParseStatus first = pair.ClientConnection.SendMessageChunk(
            Chunk(true, false, [1, 2]));
        WebSocketParseStatus final = pair.ClientConnection.SendMessageChunk(
            Chunk(false, true, [3]));
        byte[] clientWire = DrainAll(pair.ClientConnection);
        WebSocketInboundFramePumpResult serverReceive =
            pair.ServerConnection.ReceiveConnectionBytes(clientWire);
        WebSocketEvent? serverMessage = pair.ServerConnection.ReadNextEvent();

        WebSocketParseStatus reply = pair.ServerConnection.SendWholeMessage(
            Request([4, 5]));
        byte[] serverWire = DrainAll(pair.ServerConnection);
        WebSocketInboundFramePumpResult clientReceive =
            pair.ClientConnection.ReceiveConnectionBytes(serverWire);
        WebSocketEvent? clientMessage = pair.ClientConnection.ReadNextEvent();

        IWebSocketFrameCodec claimedFrameCodec = new WebSocketFrameCodec(
            WebSocketRole.Server,
            16,
            pair.ServerHandshake.Extensions);
        WebSocketFrameReadResult claimedFrame = claimedFrameCodec.ReadFrame(
            [0xa3, 0x80, 1, 2, 3, 4]);

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientHandshake.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(pair.ServerHandshake.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(pair.ClientHandshake.Transport, Is.EqualTo(transport));
            Assert.That(pair.ServerHandshake.Transport, Is.EqualTo(transport));
            Assert.That(pair.ClientHandshake.Extensions.Select(item => item.Name),
                Is.EqualTo(new[] { "x-synthetic", "x-after" }));
            Assert.That(pair.ClientHandshake.Extensions[0].ClaimsRsv2, Is.True);
            Assert.That(pair.ClientHandshake.Extensions[0].ClaimedOpcodes,
                Is.EqualTo(new[] { WebSocketOpcode.ReservedData3 }));
            Assert.That(first, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(reply, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(clientWire[0] & 0x20, Is.EqualTo(0x20));
            Assert.That(serverReceive.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(serverReceive.FrameCount, Is.EqualTo(2));
            Assert.That(serverMessage, Is.Not.Null);
            Assert.That(serverMessage!.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(clientReceive.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(clientMessage, Is.Not.Null);
            Assert.That(clientMessage!.Payload, Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(claimedFrame.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(claimedFrame.Header.Rsv2, Is.True);
            Assert.That(claimedFrame.Header.Opcode,
                Is.EqualTo(WebSocketOpcode.ReservedData3));
        });

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientFirst.OutboundFirstChunks,
                Is.EqualTo(new[] { true, false }));
            Assert.That(pair.ClientFirst.OutboundFinalChunks,
                Is.EqualTo(new[] { false, true }));
            Assert.That(pair.ServerFirst.InboundFirstChunks,
                Is.EqualTo(new[] { true, false }));
            Assert.That(pair.ServerFirst.InboundFinalChunks,
                Is.EqualTo(new[] { false, true }));
            Assert.That(pair.ServerFirst.InboundRsv2,
                Is.EqualTo(new[] { true, false }));
            Assert.That(pair.ServerSecond.InboundRsv2,
                Is.EqualTo(new[] { false, false }));
            Assert.That(pair.ClientFirst.OutboundCompleteCount, Is.EqualTo(1));
            Assert.That(pair.ServerFirst.InboundCompleteCount, Is.EqualTo(1));
            Assert.That(pair.ServerFirst.OutboundCompleteCount, Is.EqualTo(1));
            Assert.That(pair.ClientFirst.InboundCompleteCount, Is.EqualTo(1));
            Assert.That(pair.Trace, Is.EqualTo(new[]
            {
                "out-client-first",
                "out-client-second",
                "out-client-first",
                "out-client-second",
                "in-server-second",
                "in-server-first",
                "in-server-second",
                "in-server-first",
                "out-server-first",
                "out-server-second",
                "in-client-second",
                "in-client-first"
            }));
        });

        pair.ClientConnection.AbortConnectionTransport();
        pair.ServerConnection.AbortConnectionTransport();
        pair.ClientConnection.AbortConnectionTransport();
        pair.ServerConnection.AbortConnectionTransport();

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientFirst.ResetCount, Is.EqualTo(1));
            Assert.That(pair.ClientSecond.ResetCount, Is.EqualTo(1));
            Assert.That(pair.ServerFirst.ResetCount, Is.EqualTo(1));
            Assert.That(pair.ServerSecond.ResetCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void AFailingExtensionStopsItsPipelineWithoutAffectingAnotherConnection()
    {
        SyntheticExtensionPair failing = CreatePair(
            WebSocketHandshakeTransport.Http1Upgrade,
            true);
        SyntheticExtensionPair healthy = CreatePair(
            WebSocketHandshakeTransport.Http1Upgrade,
            false);

        WebSocketParseStatus failed = failing.ClientConnection.SendWholeMessage(
            Request([7]));
        WebSocketParseStatus repeated = failing.ClientConnection.SendWholeMessage(
            Request([8]));
        WebSocketParseStatus succeeded = healthy.ClientConnection.SendWholeMessage(
            Request([9]));
        byte[] healthyWire = DrainAll(healthy.ClientConnection);
        WebSocketInboundFramePumpResult received =
            healthy.ServerConnection.ReceiveConnectionBytes(healthyWire);
        WebSocketEvent? message = healthy.ServerConnection.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failing.ClientConnection.GetConnectionInfo().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(failing.ClientFirst.OutboundCallCount, Is.EqualTo(1));
            Assert.That(failing.ClientSecond.OutboundCallCount, Is.Zero);
            Assert.That(succeeded, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.Payload, Is.EqualTo(new byte[] { 9 }));
            Assert.That(healthy.ClientFirst.OutboundCallCount, Is.EqualTo(1));
            Assert.That(healthy.ClientSecond.OutboundCallCount, Is.EqualTo(1));
            Assert.That(healthy.ServerFirst.InboundCallCount, Is.EqualTo(1));
            Assert.That(healthy.ServerSecond.InboundCallCount, Is.EqualTo(1));
            Assert.That(healthy.ClientFirst.ResetCount, Is.Zero);
            Assert.That(healthy.ServerFirst.ResetCount, Is.Zero);
        });

        failing.ClientConnection.AbortConnectionTransport();

        Assert.Multiple(() =>
        {
            Assert.That(failing.ClientFirst.ResetCount, Is.EqualTo(1));
            Assert.That(failing.ClientSecond.ResetCount, Is.EqualTo(1));
            Assert.That(healthy.ClientFirst.ResetCount, Is.Zero);
            Assert.That(healthy.ServerFirst.ResetCount, Is.Zero);
        });
    }

    static SyntheticExtensionPair CreatePair(
        WebSocketHandshakeTransport transport,
        bool failClientFirst)
    {
        var trace = new List<string>();
        var clientFirst = new SyntheticLifecycleExtension(
            "x-synthetic",
            "client-first",
            0xa1,
            trace);
        clientFirst.ClaimsRsv2 = true;
        clientFirst.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
        clientFirst.FailOutbound = failClientFirst;
        var clientSecond = new SyntheticLifecycleExtension(
            "x-after",
            "client-second",
            0xb2,
            trace);
        var serverFirst = new SyntheticLifecycleExtension(
            "x-synthetic",
            "server-first",
            0xa1,
            trace);
        serverFirst.ClaimsRsv2 = true;
        serverFirst.ClaimedOpcodes = [WebSocketOpcode.ReservedData3];
        var serverSecond = new SyntheticLifecycleExtension(
            "x-after",
            "server-second",
            0xb2,
            trace);
        IWebSocketExtension[] clientExtensions = [clientFirst, clientSecond];
        IWebSocketExtension[] serverExtensions = [serverFirst, serverSecond];
        IWebSocketClientHandshake clientHandshake = new WebSocketClientHandshake();
        IWebSocketServerHandshake serverHandshake = new WebSocketServerHandshake();
        var clientOptions = new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/lifecycle",
            Transport = transport,
            PeerEnabledExtendedConnect = true,
            ExtensionHandlers = clientExtensions
        };
        var serverOptions = new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [transport],
            ExtendedConnectEnabled = true,
            ExtensionHandlers = serverExtensions
        };

        WebSocketHandshakeResult created;
        WebSocketHandshakeResult serverResult;
        WebSocketHandshakeResult clientResult;
        if (transport == WebSocketHandshakeTransport.Http1Upgrade)
        {
            created = clientHandshake.CreateHttp1Request(clientOptions);
            serverResult = serverHandshake.EvaluateHttp1Request(
                created.Request,
                serverOptions);
            clientResult = clientHandshake.ValidateHttp1Response(
                serverResult.Response);
        }
        else
        {
            created = clientHandshake.CreateExtendedConnectRequest(clientOptions);
            serverResult = serverHandshake.EvaluateExtendedConnectRequest(
                created.Request,
                serverOptions);
            clientResult = clientHandshake.ValidateExtendedConnectResponse(
                serverResult.Response);
        }

        var clientTemplate = new WebSocketConnectionOptions
        {
            Extensions = clientExtensions,
            HasMaskKeySource = true,
            MaskKeySource = new SyntheticMaskKeySource(),
            MaximumFramePayloadLength = 64,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 128,
            AutomaticFramePayloadLength = 64
        };
        var serverTemplate = new WebSocketConnectionOptions
        {
            Extensions = serverExtensions,
            MaximumFramePayloadLength = 64,
            MaximumMessagePayloadLength = 64,
            MaximumDecompressedMessagePayloadLength = 64,
            MaximumBufferedInputBytes = 64,
            MaximumBufferedOutputBytes = 128,
            AutomaticFramePayloadLength = 64
        };
        WebSocketConnectionOptions clientConnectionOptions =
            clientHandshake.CreateClientConnectionOptions(
                clientResult,
                clientTemplate);
        WebSocketConnectionOptions serverConnectionOptions =
            serverHandshake.CreateServerConnectionOptions(
                serverResult,
                serverTemplate);

        return new SyntheticExtensionPair
        {
            ClientHandshake = clientResult,
            ServerHandshake = serverResult,
            ClientConnection = new WebSocketConnection(
                clientConnectionOptions,
                clientResult),
            ServerConnection = new WebSocketConnection(
                serverConnectionOptions,
                serverResult),
            ClientFirst = clientFirst,
            ClientSecond = clientSecond,
            ServerFirst = serverFirst,
            ServerSecond = serverSecond,
            Trace = trace
        };
    }

    static WebSocketSendChunk Chunk(bool first, bool final, byte[] payload)
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

    static WebSocketSendRequest Request(byte[] payload)
    {
        return new WebSocketSendRequest
        {
            MessageType = WebSocketMessageType.Binary,
            Payload = payload,
            PayloadCount = payload.Length
        };
    }

    static byte[] DrainAll(WebSocketConnection connection)
    {
        byte[] output = new byte[connection.GetPendingConnectionWriteCount()];
        int written = connection.WriteConnectionBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }
}

internal class SyntheticExtensionPair
{
    internal WebSocketHandshakeResult ClientHandshake = new WebSocketHandshakeResult();
    internal WebSocketHandshakeResult ServerHandshake = new WebSocketHandshakeResult();
    internal WebSocketConnection ClientConnection = null!;
    internal WebSocketConnection ServerConnection = null!;
    internal SyntheticLifecycleExtension ClientFirst = null!;
    internal SyntheticLifecycleExtension ClientSecond = null!;
    internal SyntheticLifecycleExtension ServerFirst = null!;
    internal SyntheticLifecycleExtension ServerSecond = null!;
    internal List<string> Trace = [];
}

internal class SyntheticLifecycleExtension : IWebSocketExtension
{
    internal string Name;
    internal string Label;
    internal byte Marker;
    internal List<string> Trace;
    internal bool ClaimsRsv2;
    internal WebSocketOpcode[] ClaimedOpcodes = [];
    internal bool FailOutbound;
    internal int OutboundCallCount;
    internal int InboundCallCount;
    internal int OutboundCompleteCount;
    internal int InboundCompleteCount;
    internal int ResetCount;
    internal List<bool> OutboundFirstChunks = [];
    internal List<bool> OutboundFinalChunks = [];
    internal List<bool> InboundFirstChunks = [];
    internal List<bool> InboundFinalChunks = [];
    internal List<bool> InboundRsv2 = [];

    internal SyntheticLifecycleExtension(
        string name,
        string label,
        byte marker,
        List<string> trace)
    {
        Name = name;
        Label = label;
        Marker = marker;
        Trace = trace;
    }

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = Name };
    }

    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(
        WebSocketExtensionOffer offer)
    {
        return Negotiate(offer);
    }

    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(
        WebSocketExtensionOffer response)
    {
        return Negotiate(response);
    }

    public WebSocketTransformResult TransformOutboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        OutboundCallCount++;
        OutboundFirstChunks.Add(context.FirstChunk);
        OutboundFinalChunks.Add(context.FinalChunk);
        Trace.Add("out-" + Label);
        if (FailOutbound)
        {
            return new WebSocketTransformResult
            {
                Status = WebSocketParseStatus.ProtocolFailure,
                HasError = true,
                Error = new WebSocketError
                {
                    Kind = WebSocketErrorKind.Protocol,
                    Identifier = "synthetic-transform-failure",
                    Diagnostic = "The synthetic extension rejected outbound data."
                }
            };
        }

        byte[] output = new byte[source.Length + 1];
        source.CopyTo(output);
        output[source.Length] = Marker;
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output,
            SetRsv2 = ClaimsRsv2 && context.FirstFragment
        };
    }

    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        InboundCallCount++;
        InboundFirstChunks.Add(context.FirstChunk);
        InboundFinalChunks.Add(context.FinalChunk);
        InboundRsv2.Add(context.Rsv2);
        Trace.Add("in-" + Label);
        if (source.Length == 0 || source[source.Length - 1] != Marker)
        {
            return new WebSocketTransformResult
            {
                Status = WebSocketParseStatus.ProtocolFailure,
                HasError = true,
                Error = new WebSocketError
                {
                    Kind = WebSocketErrorKind.Protocol,
                    Identifier = "synthetic-marker-missing",
                    Diagnostic = "The synthetic extension marker was missing."
                }
            };
        }

        byte[] output = source.Slice(0, source.Length - 1).ToArray();
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output
        };
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
        if (context.Outbound)
        {
            OutboundCompleteCount++;
        }
        else
        {
            InboundCompleteCount++;
        }
    }

    public void ResetExtensionState()
    {
        ResetCount++;
    }

    WebSocketExtensionNegotiation Negotiate(WebSocketExtensionOffer offer)
    {
        var result = new WebSocketExtensionNegotiation();
        result.Name = Name;
        if (offer.Name != Name)
        {
            result.Status = WebSocketExtensionStatus.Declined;
            return result;
        }

        result.Status = WebSocketExtensionStatus.Negotiated;
        result.HasAcceptedConfiguration = true;
        result.AcceptedConfiguration = new WebSocketExtensionOffer
        {
            Name = Name
        };
        result.ClaimsRsv2 = ClaimsRsv2;
        result.ClaimedOpcodes = ClaimedOpcodes.ToArray();
        return result;
    }
}

internal class SyntheticMaskKeySource : IWebSocketMaskKeySource
{
    internal uint NextKey = 0x01020304U;

    public uint CreateMaskingKey()
    {
        uint result = NextKey;
        NextKey = NextKey + 0x01010101U;
        return result;
    }
}
