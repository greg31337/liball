using websocket;

namespace websocket.Tests;

public class WebSocketRemoteCloseHandlerTests
{
    [Test]
    public void ValidCloseIsRecordedSurfacedAndEchoedWithClientMasking()
    {
        WebSocketConnectionOptions options = Options(true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketSendScheduler scheduler =
            consumer.GetStreamingSendScheduler();
        scheduler.TryQueueDataFrame([9, 8, 7]);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(
            WireFrame(
                WebSocketOpcode.Close,
                true,
                [0x03, 0xE8, 0x62, 0x79, 0x65]));
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadStreamingEvent());
        WebSocketCloseInfo remote = consumer
            .GetStreamingRemoteCloseHandler()
            .GetRemoteCloseInfo();
        closeEvent.Close.Reason = "mutated";
        WebSocketCloseInfo retained = consumer
            .GetStreamingRemoteCloseHandler()
            .GetRemoteCloseInfo();
        byte[] echo = Drain(scheduler);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FrameCount, Is.EqualTo(1));
            Assert.That(closeEvent.Type,
                Is.EqualTo(WebSocketEventType.Close));
            Assert.That(closeEvent.HasClose, Is.True);
            Assert.That(closeEvent.First, Is.True);
            Assert.That(closeEvent.Final, Is.True);
            Assert.That(remote.HasCode, Is.True);
            Assert.That(remote.Code, Is.EqualTo(1000));
            Assert.That(remote.HasKnownCode, Is.True);
            Assert.That(remote.KnownCode,
                Is.EqualTo(WebSocketCloseCode.NormalClosure));
            Assert.That(remote.Reason, Is.EqualTo("bye"));
            Assert.That(remote.Received, Is.True);
            Assert.That(remote.InitiatedLocally, Is.False);
            Assert.That(retained.Reason, Is.EqualTo("bye"));
            Assert.That(echo,
                Is.EqualTo(new byte[]
                {
                    0x88, 0x85,
                    0x01, 0x02, 0x03, 0x04,
                    0x02, 0xEA, 0x61, 0x7D, 0x64
                }));
            Assert.That(echo, Does.Not.Contain(9));
            Assert.That(keys.CallCount, Is.EqualTo(1));
            Assert.That(scheduler.GetDataSendsStopped(), Is.True);
            Assert.That(consumer.GetStreamingStateMachine().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(consumer.GetStreamingStateMachine()
                .RemoteCloseReceived, Is.True);
            Assert.That(consumer.GetStreamingLocalCloseCoordinator()
                .GetLocalCloseInfo().InitiatedLocally, Is.False);
        });
    }

    [Test]
    public void EmptyCloseProducesAnEmptyAutomaticResponse()
    {
        WebSocketConnectionOptions options = Options(true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);

        WebSocketInboundFramePumpResult result = Pump(options, consumer)
            .PumpInboundFrames(WireFrame(WebSocketOpcode.Close, true, []));
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(closeEvent.Close.HasCode, Is.False);
            Assert.That(closeEvent.Close.Received, Is.True);
            Assert.That(Drain(consumer.GetStreamingSendScheduler()),
                Is.EqualTo(new byte[]
                {
                    0x88, 0x80,
                    0x01, 0x02, 0x03, 0x04
                }));
        });
    }

    [Test]
    public void DisabledAutomaticResponseStillStopsDataAndHeartbeat()
    {
        WebSocketConnectionOptions options = Options(false);
        options.Heartbeat.Enabled = true;
        options.Heartbeat.IdleIntervalMilliseconds = 100;
        options.Heartbeat.PongTimeoutMilliseconds = 50;
        options.Heartbeat.TokenLength = 4;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketHeartbeatManager heartbeat =
            consumer.GetStreamingHeartbeatManager();
        heartbeat.AdvanceHeartbeatTime(100);

        Pump(options, consumer).PumpInboundFrames(
            WireFrame(WebSocketOpcode.Close, true, [0x03, 0xE8]));
        Drain(consumer.GetStreamingSendScheduler());
        heartbeat.AdvanceHeartbeatTime(1000);

        Assert.Multiple(() =>
        {
            Assert.That(heartbeat.HasOutstandingHeartbeatPing(), Is.False);
            Assert.That(consumer.GetStreamingPingPongHandler()
                .GetOutstandingTrackedPingCount(), Is.Zero);
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetPendingByteCount(), Is.Zero);
            Assert.That(consumer.GetStreamingSendScheduler()
                .TryQueueDataFrame([1]), Is.False);
            Assert.That(consumer.GetStreamingLocalCloseCoordinator()
                .HasLocalCloseInfo(), Is.False);
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .HasRemoteCloseInfo(), Is.True);
        });
    }

    [Test]
    public void ExistingLocalClosePreventsASecondAutomaticCloseFrame()
    {
        WebSocketConnectionOptions options = Options(true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketLocalCloseCoordinator local =
            consumer.GetStreamingLocalCloseCoordinator();
        local.StartLocalClose(Close(1001, "away"));
        int localCount = consumer.GetStreamingSendScheduler()
            .GetPendingByteCount();

        Pump(options, consumer).PumpInboundFrames(
            WireFrame(WebSocketOpcode.Close, true, [0x03, 0xE8]));

        Assert.Multiple(() =>
        {
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetPendingByteCount(), Is.EqualTo(localCount));
            Assert.That(keys.CallCount, Is.EqualTo(1));
            Assert.That(local.GetLocalCloseInfo().Code, Is.EqualTo(1001));
            Assert.That(local.GetLocalCloseInfo().InitiatedLocally, Is.True);
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .GetRemoteCloseInfo().Code, Is.EqualTo(1000));
            Assert.That(consumer.GetStreamingStateMachine()
                .RemoteCloseReceived, Is.True);
        });
    }

    [TestCase(
        new byte[] { 0x88, 0x01, 0x03 },
        WebSocketErrorKind.Protocol,
        1002,
        "close-payload-one-byte")]
    [TestCase(
        new byte[] { 0x88, 0x02, 0x03, 0xEE },
        WebSocketErrorKind.Protocol,
        1002,
        "close-code-invalid")]
    [TestCase(
        new byte[] { 0x88, 0x03, 0x03, 0xE8, 0xC0 },
        WebSocketErrorKind.Utf8,
        1007,
        "close-reason-invalid-utf8")]
    public void InvalidClosePayloadQueuesFailureCloseWithoutRemoteFacts(
        byte[] wire,
        WebSocketErrorKind kind,
        int closeCode,
        string identifier)
    {
        WebSocketConnectionOptions options = Options(true);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);

        WebSocketInboundFramePumpResult result = Pump(options, consumer)
            .PumpInboundFrames(wire);
        WebSocketEvent errorEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(kind));
            Assert.That(result.Error.CloseCode, Is.EqualTo(closeCode));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(errorEvent.Type,
                Is.EqualTo(WebSocketEventType.Error));
            Assert.That(errorEvent.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(consumer.GetStreamingEventCount(), Is.Zero);
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetPendingByteCount(), Is.EqualTo(8));
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .HasRemoteCloseInfo(), Is.False);
            Assert.That(consumer.GetStreamingStateMachine().State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(consumer.GetStreamingStateMachine().HasFailure,
                Is.True);
            Assert.That(consumer.GetStreamingLocalCloseCoordinator()
                .GetLocalCloseInfo().Code, Is.EqualTo(closeCode));
        });
    }

    [Test]
    public void StreamingCloseCancelsFragmentAndSuppressesLaterData()
    {
        WebSocketConnectionOptions options = Options(true);
        options.HasMaskKeySource = true;
        options.MaskKeySource = new WholeWriterMaskKeySource();
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        byte[] wire = Combine(
            WireFrame(WebSocketOpcode.Text, false, [65]),
            WireFrame(WebSocketOpcode.Close, true, []),
            WireFrame(WebSocketOpcode.Continuation, true, [66]),
            WireFrame(WebSocketOpcode.Binary, true, [67]));

        WebSocketInboundFramePumpResult result = Pump(options, consumer)
            .PumpInboundFrames(wire);
        List<WebSocketEvent> events = DrainEvents(consumer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FrameCount, Is.EqualTo(4));
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.Close
                }));
            Assert.That(events[1].Payload,
                Is.EqualTo(new byte[] { 65 }));
            Assert.That(consumer.GetApplicationDataSuppressed(), Is.True);
            Assert.That(consumer.GetStreamingStateMachine().HasFailure,
                Is.False);
        });
    }

    [Test]
    public void ReassembledCloseDiscardsPartialMessageAndLaterData()
    {
        WebSocketConnectionOptions options = Options(true);
        options.DeliveryMode = WebSocketMessageDeliveryMode.Reassembled;
        options.HasMaskKeySource = true;
        options.MaskKeySource = new WholeWriterMaskKeySource();
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
        byte[] wire = Combine(
            WireFrame(WebSocketOpcode.Binary, false, [1, 2]),
            WireFrame(WebSocketOpcode.Close, true, [0x03, 0xE8]),
            WireFrame(WebSocketOpcode.Continuation, true, [3]),
            WireFrame(WebSocketOpcode.Text, true, [65]));

        WebSocketInboundFramePumpResult result = Pump(options, consumer)
            .PumpInboundFrames(wire);
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FrameCount, Is.EqualTo(4));
            Assert.That(closeEvent.Type,
                Is.EqualTo(WebSocketEventType.Close));
            Assert.That(closeEvent.Close.Code, Is.EqualTo(1000));
            Assert.That(consumer.ReadReassembledEvent(), Is.Null);
            Assert.That(consumer.GetBufferedMessageByteCount(), Is.Zero);
            Assert.That(consumer.GetBufferedMessageByteCapacity(), Is.Zero);
        });
    }

    [Test]
    public void DuplicateRemoteCloseDoesNotReplaceFactsEventOrEcho()
    {
        WebSocketConnectionOptions options = Options(true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        byte[] wire = Combine(
            WireFrame(WebSocketOpcode.Close, true, [0x03, 0xE8]),
            WireFrame(WebSocketOpcode.Close, true, [0x03, 0xE9]));

        WebSocketInboundFramePumpResult result = Pump(options, consumer)
            .PumpInboundFrames(wire);
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameCount, Is.EqualTo(2));
            Assert.That(closeEvent.Close.Code, Is.EqualTo(1000));
            Assert.That(consumer.ReadStreamingEvent(), Is.Null);
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .GetRemoteCloseInfo().Code, Is.EqualTo(1000));
            Assert.That(keys.CallCount, Is.EqualTo(1));
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetPendingCloseByteCount(), Is.EqualTo(8));
        });
    }

    [Test]
    public void CloseEventCapacityBackpressureResumesWithoutReparsing()
    {
        WebSocketConnectionOptions options = Options(true);
        options.AutomaticPong = false;
        options.MaximumQueuedEvents = 1;
        options.HasMaskKeySource = true;
        options.MaskKeySource = new WholeWriterMaskKeySource();
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        byte[] ping = WireFrame(WebSocketOpcode.Ping, true, [9]);
        byte[] close = WireFrame(
            WebSocketOpcode.Close,
            true,
            [0x03, 0xE8]);
        byte[] wire = Combine(ping, close);

        WebSocketInboundFramePumpResult blocked =
            pump.PumpInboundFrames(wire);
        WebSocketEvent pingEvent = RequiredEvent(
            consumer.ReadStreamingEvent());
        WebSocketInboundFramePumpResult resumed = pump.PumpInboundFrames(
            wire.AsSpan(blocked.ConsumedCount));
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.ConsumedCount,
                Is.EqualTo(ping.Length + 2));
            Assert.That(pingEvent.Type,
                Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(closeEvent.Type,
                Is.EqualTo(WebSocketEventType.Close));
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .GetRemoteCloseInfo().Code, Is.EqualTo(1000));
        });
    }

    [Test]
    public void EchoOutputBackpressureRetainsCompletedCloseForRetry()
    {
        WebSocketConnectionOptions options = Options(true);
        options.HasMaskKeySource = true;
        options.MaskKeySource = new WholeWriterMaskKeySource();
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketSendScheduler scheduler =
            consumer.GetStreamingSendScheduler();
        scheduler.TryQueueControlFrame(new byte[128]);
        var prefix = new byte[1];
        scheduler.WritePendingBytes(prefix);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        byte[] close = WireFrame(
            WebSocketOpcode.Close,
            true,
            [0x03, 0xE8]);

        WebSocketInboundFramePumpResult blocked =
            pump.PumpInboundFrames(close);
        var prior = new byte[127];
        scheduler.WritePendingBytes(prior);
        WebSocketInboundFramePumpResult resumed =
            pump.PumpInboundFrames(ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(close.Length));
            Assert.That(blocked.FrameCount, Is.Zero);
            Assert.That(consumer.GetApplicationDataSuppressed(), Is.True);
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.FrameCount, Is.EqualTo(1));
            Assert.That(consumer.GetStreamingEventCount(), Is.EqualTo(1));
            Assert.That(scheduler.GetPendingCloseByteCount(), Is.EqualTo(8));
        });
    }

    [Test]
    public void ClosePayloadCanArriveAcrossInputCalls()
    {
        WebSocketConnectionOptions options = Options(false);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        byte[] wire = WireFrame(
            WebSocketOpcode.Close,
            true,
            [0x03, 0xE8, 0x62, 0x79, 0x65]);

        WebSocketInboundFramePumpResult partial = pump.PumpInboundFrames(
            wire.AsSpan(0, 4));
        WebSocketInboundFramePumpResult completed = pump.PumpInboundFrames(
            wire.AsSpan(4));
        WebSocketEvent closeEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status,
                Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(partial.ConsumedCount, Is.EqualTo(4));
            Assert.That(completed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(closeEvent.Close.Reason, Is.EqualTo("bye"));
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void ResetAfterCloseRestoresApplicationDeliveryAndOpenState()
    {
        WebSocketConnectionOptions options = Options(false);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        Pump(options, consumer).PumpInboundFrames(
            WireFrame(WebSocketOpcode.Close, true, []));

        consumer.ResetInboundFrameConsumer();
        WebSocketInboundFrameConsumeResult result =
            consumer.ConsumeInboundFrameChunk(
                Chunk(WebSocketOpcode.Binary, true, [7]));
        List<WebSocketEvent> events = DrainEvents(consumer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(events.Select(item => item.Type),
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageEnd
                }));
            Assert.That(consumer.GetApplicationDataSuppressed(), Is.False);
            Assert.That(consumer.GetStreamingStateMachine().State,
                Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(consumer.GetStreamingRemoteCloseHandler()
                .HasRemoteCloseInfo(), Is.False);
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetDataSendsStopped(), Is.False);
        });
    }

    static WebSocketConnectionOptions Options(bool automaticClose)
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            AutomaticCloseResponse = automaticClose,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 256,
            MaximumDecompressedMessagePayloadLength = 256,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 128,
            MaximumBufferedOutputBytes = 128,
            MaximumQueuedEvents = 16,
            AutomaticFramePayloadLength = 64
        };
    }

    static WebSocketStreamingReceiveConsumer Consumer(
        WebSocketConnectionOptions options)
    {
        return new WebSocketStreamingReceiveConsumer(
            options,
            new WebSocketExtensionPipeline([], []));
    }

    static WebSocketInboundFramePump Pump(
        WebSocketConnectionOptions options,
        IWebSocketInboundFrameConsumer consumer)
    {
        var codec = new WebSocketFrameCodec(
            options.Role,
            options.MaximumFramePayloadLength,
            options.NegotiatedExtensions);
        return new WebSocketInboundFramePump(codec, consumer);
    }

    static WebSocketCloseInfo Close(int code, string reason)
    {
        return new WebSocketCloseInfo
        {
            HasCode = true,
            Code = code,
            HasReason = reason.Length != 0,
            Reason = reason
        };
    }

    static WebSocketInboundFrameChunk Chunk(
        WebSocketOpcode opcode,
        bool final,
        byte[] payload)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = new WebSocketFrameHeader
            {
                Final = final,
                Opcode = opcode,
                PayloadLength = payload.Length
            },
            FirstChunk = true,
            EndOfFrame = true,
            Payload = payload,
            PayloadOffset = 0,
            PayloadCount = payload.Length
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

    static byte[] Combine(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] combined = new byte[length];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            array.CopyTo(combined, offset);
            offset += array.Length;
        }

        return combined;
    }

    static byte[] Drain(WebSocketSendScheduler scheduler)
    {
        byte[] output = new byte[scheduler.GetPendingByteCount()];
        int written = scheduler.WritePendingBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static List<WebSocketEvent> DrainEvents(
        WebSocketStreamingReceiveConsumer consumer)
    {
        List<WebSocketEvent> events = [];
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
