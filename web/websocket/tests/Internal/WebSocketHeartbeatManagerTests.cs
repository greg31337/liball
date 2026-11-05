using websocket;

namespace websocket.Tests;

public class WebSocketHeartbeatManagerTests
{
    [Test]
    public void DisabledHeartbeatNeverQueuesAutomaticPing()
    {
        HeartbeatTestContext context = Context(false, 100, 50, 4, 256);

        WebSocketHeartbeatAdvanceResult advanced =
            context.Manager.AdvanceHeartbeatTime(100000);

        Assert.Multiple(() =>
        {
            Assert.That(advanced.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(advanced.PingQueued, Is.False);
            Assert.That(context.Scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.False);
        });
    }

    [Test]
    public void PingIsQueuedOnlyWhenIdleBoundaryIsAdvancedExplicitly()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        Assert.That(context.Scheduler.GetPendingByteCount(), Is.Zero);

        WebSocketHeartbeatAdvanceResult below =
            context.Manager.AdvanceHeartbeatTime(99);
        WebSocketHeartbeatAdvanceResult boundary =
            context.Manager.AdvanceHeartbeatTime(100);
        var wire = new byte[6];
        int written = context.Scheduler.WritePendingBytes(wire);

        Assert.Multiple(() =>
        {
            Assert.That(below.PingQueued, Is.False);
            Assert.That(boundary.PingQueued, Is.True);
            Assert.That(boundary.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(written, Is.EqualTo(6));
            Assert.That(wire[0], Is.EqualTo(0x89));
            Assert.That(wire[1], Is.EqualTo(0x04));
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.True);
            Assert.That(
                context.Manager.HasHeartbeatPingSentMilliseconds(),
                Is.True);
            Assert.That(
                context.Manager.GetHeartbeatPingSentMilliseconds(),
                Is.EqualTo(100));
        });
    }

    [Test]
    public void RecordedActivityRestartsTheIdleInterval()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(90);
        context.Manager.RecordHeartbeatActivity();

        WebSocketHeartbeatAdvanceResult originalBoundary =
            context.Manager.AdvanceHeartbeatTime(100);
        WebSocketHeartbeatAdvanceResult belowRestartedBoundary =
            context.Manager.AdvanceHeartbeatTime(189);
        WebSocketHeartbeatAdvanceResult restartedBoundary =
            context.Manager.AdvanceHeartbeatTime(190);

        Assert.Multiple(() =>
        {
            Assert.That(originalBoundary.PingQueued, Is.False);
            Assert.That(belowRestartedBoundary.PingQueued, Is.False);
            Assert.That(restartedBoundary.PingQueued, Is.True);
            Assert.That(
                context.Manager.GetLastHeartbeatActivityMilliseconds(),
                Is.EqualTo(90));
        });
    }

    [Test]
    public void OutputBackpressureCanRetryAtTheSameElapsedTime()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 6);
        context.Scheduler.TryQueueDataFrame([9]);

        WebSocketHeartbeatAdvanceResult blocked =
            context.Manager.AdvanceHeartbeatTime(100);
        var prior = new byte[1];
        context.Scheduler.WritePendingBytes(prior);
        WebSocketHeartbeatAdvanceResult retried =
            context.Manager.AdvanceHeartbeatTime(100);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.PingQueued, Is.False);
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.True);
            Assert.That(retried.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(retried.Changed, Is.False);
            Assert.That(retried.PingQueued, Is.True);
            Assert.That(context.Scheduler.GetPendingByteCount(),
                Is.EqualTo(6));
        });
    }

    [Test]
    public void PongDeadlineFailsAtTheExactBoundaryAndLatches()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(100);

        WebSocketHeartbeatAdvanceResult below =
            context.Manager.AdvanceHeartbeatTime(149);
        WebSocketHeartbeatAdvanceResult boundary =
            context.Manager.AdvanceHeartbeatTime(150);
        WebSocketHeartbeatAdvanceResult repeated =
            context.Manager.AdvanceHeartbeatTime(200);

        Assert.Multiple(() =>
        {
            Assert.That(below.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(boundary.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(boundary.TimedOut, Is.True);
            Assert.That(boundary.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Transport));
            Assert.That(boundary.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(boundary.Error.Identifier,
                Is.EqualTo("heartbeat-pong-timeout"));
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("heartbeat-pong-timeout"));
            Assert.That(context.Manager.HasHeartbeatFailed(), Is.True);
            Assert.That(context.Handler.GetOutstandingTrackedPingCount(),
                Is.Zero);
        });
    }

    [Test]
    public void ActivityWhilePingIsOutstandingDoesNotMovePongDeadline()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(100);
        context.Manager.AdvanceHeartbeatTime(120);
        context.Manager.RecordHeartbeatActivity();

        WebSocketHeartbeatAdvanceResult timedOut =
            context.Manager.AdvanceHeartbeatTime(150);

        Assert.Multiple(() =>
        {
            Assert.That(timedOut.TimedOut, Is.True);
            Assert.That(timedOut.Error.Identifier,
                Is.EqualTo("heartbeat-pong-timeout"));
        });
    }

    [Test]
    public void MatchingPongClearsDeadlineAndRecordsRoundTrip()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(100);
        byte[] wire = Drain(context.Scheduler);
        byte[] token = wire.AsSpan(2, 4).ToArray();
        context.Manager.AdvanceHeartbeatTime(125);

        WebSocketInboundFrameConsumeResult consumed =
            context.Handler.ConsumeInboundControlChunk(
                Chunk(WebSocketOpcode.Pong, token));
        bool synchronized =
            context.Manager.SynchronizeHeartbeatAcknowledgement();
        WebSocketEvent acknowledged = RequiredEvent(
            context.Events.DequeueEvent());
        WebSocketHeartbeatAdvanceResult belowNextIdle =
            context.Manager.AdvanceHeartbeatTime(224);
        WebSocketHeartbeatAdvanceResult nextIdle =
            context.Manager.AdvanceHeartbeatTime(225);

        Assert.Multiple(() =>
        {
            Assert.That(consumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(synchronized, Is.True);
            Assert.That(acknowledged.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(acknowledged.PingElapsedMilliseconds,
                Is.EqualTo(25));
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.True);
            Assert.That(
                context.Manager.HasHeartbeatRoundTripMilliseconds(),
                Is.True);
            Assert.That(
                context.Manager.GetHeartbeatRoundTripMilliseconds(),
                Is.EqualTo(25));
            Assert.That(belowNextIdle.PingQueued, Is.False);
            Assert.That(nextIdle.PingQueued, Is.True);
        });
    }

    [Test]
    public void MonotonicTimeRejectionDoesNotMutateHeartbeatState()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(60);

        WebSocketHeartbeatAdvanceResult regressed =
            context.Manager.AdvanceHeartbeatTime(59);
        WebSocketHeartbeatAdvanceResult recovered =
            context.Manager.AdvanceHeartbeatTime(100);

        Assert.Multiple(() =>
        {
            Assert.That(regressed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(regressed.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(regressed.Error.Identifier,
                Is.EqualTo("heartbeat-elapsed-time-invalid"));
            Assert.That(recovered.PingQueued, Is.True);
            Assert.That(
                context.Manager.GetHeartbeatElapsedMilliseconds(),
                Is.EqualTo(100));
        });
    }

    [Test]
    public void StopClearsOutstandingTokenAndSuppressesFutureHeartbeat()
    {
        HeartbeatTestContext context = Context(true, 100, 50, 4, 256);
        context.Manager.AdvanceHeartbeatTime(100);
        context.Handler.SendPing([9], 0, 1);

        context.Manager.StopHeartbeat();
        WebSocketHeartbeatAdvanceResult advanced =
            context.Manager.AdvanceHeartbeatTime(1000);

        Assert.Multiple(() =>
        {
            Assert.That(advanced.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(advanced.PingQueued, Is.False);
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.False);
            Assert.That(context.Handler.GetOutstandingTrackedPingCount(),
                Is.Zero);
        });
    }

    [Test]
    public void ResetReturnsTimingAndRoundTripFactsToInitialState()
    {
        HeartbeatTestContext context = Context(true, 10, 50, 2, 256);
        context.Manager.AdvanceHeartbeatTime(10);
        byte[] wire = Drain(context.Scheduler);
        byte[] token = wire.AsSpan(2, 2).ToArray();
        context.Manager.AdvanceHeartbeatTime(15);
        context.Handler.ConsumeInboundControlChunk(
            Chunk(WebSocketOpcode.Pong, token));
        context.Manager.SynchronizeHeartbeatAcknowledgement();

        context.Manager.ResetHeartbeatState();

        Assert.Multiple(() =>
        {
            Assert.That(context.Manager.GetHeartbeatElapsedMilliseconds(),
                Is.Zero);
            Assert.That(
                context.Manager.GetLastHeartbeatActivityMilliseconds(),
                Is.Zero);
            Assert.That(context.Manager.HasOutstandingHeartbeatPing(),
                Is.False);
            Assert.That(
                context.Manager.HasHeartbeatRoundTripMilliseconds(),
                Is.False);
            Assert.That(context.Manager.HasHeartbeatFailed(), Is.False);
        });

        WebSocketHeartbeatAdvanceResult recovered =
            context.Manager.AdvanceHeartbeatTime(10);
        Assert.That(recovered.PingQueued, Is.True);
    }

    [Test]
    public void ConfiguredMaximumTokenLengthFitsOneControlFrame()
    {
        HeartbeatTestContext context = Context(true, 1, 50, 125, 256);

        WebSocketHeartbeatAdvanceResult advanced =
            context.Manager.AdvanceHeartbeatTime(1);
        byte[] wire = Drain(context.Scheduler);

        Assert.Multiple(() =>
        {
            Assert.That(advanced.PingQueued, Is.True);
            Assert.That(wire.Length, Is.EqualTo(127));
            Assert.That(wire[0], Is.EqualTo(0x89));
            Assert.That(wire[1], Is.EqualTo(125));
        });
    }

    [Test]
    public void StreamingConsumerSynchronizesHeartbeatOnPongReceipt()
    {
        WebSocketConnectionOptions options = Options(
            true,
            100,
            50,
            4);
        options.Role = WebSocketRole.Client;
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            EmptyPipeline());
        WebSocketHeartbeatManager manager =
            consumer.GetStreamingHeartbeatManager();
        manager.AdvanceHeartbeatTime(100);
        byte[] ping = Drain(consumer.GetStreamingSendScheduler());
        byte[] token = DecodeMaskedPayload(ping);
        manager.AdvanceHeartbeatTime(120);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        byte[] pong = ServerControlFrame(WebSocketOpcode.Pong, token);

        WebSocketInboundFramePumpResult result =
            pump.PumpInboundFrames(pong);
        WebSocketEvent acknowledged = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(acknowledged.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(manager.HasOutstandingHeartbeatPing(), Is.False);
            Assert.That(manager.GetHeartbeatRoundTripMilliseconds(),
                Is.EqualTo(20));
        });
    }

    [Test]
    public void StreamingDataReceiptResetsHeartbeatIdleTime()
    {
        WebSocketConnectionOptions options = Options(
            true,
            100,
            50,
            4);
        options.Role = WebSocketRole.Client;
        var consumer = new WebSocketStreamingReceiveConsumer(
            options,
            EmptyPipeline());
        WebSocketHeartbeatManager manager =
            consumer.GetStreamingHeartbeatManager();
        manager.AdvanceHeartbeatTime(90);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        pump.PumpInboundFrames([0x82, 0x01, 7]);

        WebSocketHeartbeatAdvanceResult originalBoundary =
            manager.AdvanceHeartbeatTime(100);
        WebSocketHeartbeatAdvanceResult restartedBoundary =
            manager.AdvanceHeartbeatTime(190);

        Assert.Multiple(() =>
        {
            Assert.That(originalBoundary.PingQueued, Is.False);
            Assert.That(restartedBoundary.PingQueued, Is.True);
            Assert.That(manager.GetLastHeartbeatActivityMilliseconds(),
                Is.EqualTo(90));
        });
    }

    static HeartbeatTestContext Context(
        bool enabled,
        int idleMilliseconds,
        int timeoutMilliseconds,
        int tokenLength,
        int outputCapacity)
    {
        WebSocketConnectionOptions options = Options(
            enabled,
            idleMilliseconds,
            timeoutMilliseconds,
            tokenLength);
        var scheduler = new WebSocketSendScheduler(outputCapacity);
        var events = new WebSocketEventQueue(8);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        var manager = new WebSocketHeartbeatManager(options, handler);
        return new HeartbeatTestContext
        {
            Options = options,
            Scheduler = scheduler,
            Events = events,
            Handler = handler,
            Manager = manager
        };
    }

    static WebSocketConnectionOptions Options(
        bool enabled,
        int idleMilliseconds,
        int timeoutMilliseconds,
        int tokenLength)
    {
        WebSocketConnectionOptions options = new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Server,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            AutomaticFramePayloadLength = 32,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 8
        };
        options.Heartbeat.Enabled = enabled;
        options.Heartbeat.IdleIntervalMilliseconds = idleMilliseconds;
        options.Heartbeat.PongTimeoutMilliseconds = timeoutMilliseconds;
        options.Heartbeat.TokenLength = tokenLength;
        return options;
    }

    static WebSocketInboundFrameChunk Chunk(
        WebSocketOpcode opcode,
        byte[] payload)
    {
        return new WebSocketInboundFrameChunk
        {
            Header = new WebSocketFrameHeader
            {
                Final = true,
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

    static byte[] Drain(WebSocketSendScheduler scheduler)
    {
        byte[] output = new byte[scheduler.GetPendingByteCount()];
        int written = scheduler.WritePendingBytes(output);
        Assert.That(written, Is.EqualTo(output.Length));
        return output;
    }

    static byte[] DecodeMaskedPayload(byte[] frame)
    {
        int payloadCount = frame[1] & 127;
        byte[] payload = new byte[payloadCount];
        int index = 0;
        while (index < payloadCount)
        {
            payload[index] = (byte)(
                frame[6 + index] ^ frame[2 + index % 4]);
            index++;
        }

        return payload;
    }

    static byte[] ServerControlFrame(
        WebSocketOpcode opcode,
        byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 2];
        frame[0] = (byte)(128 | (int)opcode);
        frame[1] = (byte)payload.Length;
        payload.CopyTo(frame, 2);
        return frame;
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

    static WebSocketExtensionPipeline EmptyPipeline()
    {
        return new WebSocketExtensionPipeline([], []);
    }

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}

internal class HeartbeatTestContext
{
    internal WebSocketConnectionOptions Options =
        new WebSocketConnectionOptions();
    internal WebSocketSendScheduler Scheduler =
        new WebSocketSendScheduler(1);
    internal WebSocketEventQueue Events = new WebSocketEventQueue(1);
    internal WebSocketPingPongHandler Handler = null!;
    internal WebSocketHeartbeatManager Manager = null!;
}
