using websocket;

namespace websocket.Tests;

public class WebSocketPingPongHandlerTests
{
    [Test]
    public void ServerQueuesExplicitPingAndUnsolicitedPongInCallOrder()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            true);
        var scheduler = new WebSocketSendScheduler(32);
        var events = new WebSocketEventQueue(4);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        var source = new byte[] { 0, 1, 2, 3, 0 };

        WebSocketControlSendResult ping = handler.SendPing(
            source,
            1,
            3);
        WebSocketControlSendResult pong = handler.SendPong([], 0, 0);
        var wire = new byte[7];
        int written = scheduler.WritePendingBytes(wire);

        Assert.Multiple(() =>
        {
            Assert.That(ping.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(ping.Accepted, Is.True);
            Assert.That(ping.EncodedCount, Is.EqualTo(5));
            Assert.That(pong.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(pong.EncodedCount, Is.EqualTo(2));
            Assert.That(written, Is.EqualTo(7));
            Assert.That(
                wire,
                Is.EqualTo(new byte[]
                {
                    0x89, 0x03, 1, 2, 3,
                    0x8A, 0x00
                }));
        });
    }

    [Test]
    public void ClientUsesFreshMaskingKeysForExplicitControlFrames()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        var scheduler = new WebSocketSendScheduler(32);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            new WebSocketEventQueue(4));

        handler.SendPing([1, 2], 0, 2);
        handler.SendPong([], 0, 0);
        var wire = new byte[14];
        int written = scheduler.WritePendingBytes(wire);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(14));
            Assert.That(keys.CallCount, Is.EqualTo(2));
            Assert.That(
                wire,
                Is.EqualTo(new byte[]
                {
                    0x89, 0x82,
                    0x01, 0x02, 0x03, 0x04,
                    0x00, 0x00,
                    0x8A, 0x80,
                    0x11, 0x12, 0x13, 0x14
                }));
        });
    }

    [Test]
    public void ExplicitControlPayloadBoundaryIsEnforcedWithoutQueueMutation()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            true);
        var scheduler = new WebSocketSendScheduler(256);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            new WebSocketEventQueue(4));

        WebSocketControlSendResult maximum = handler.SendPing(
            new byte[125],
            0,
            125);
        WebSocketControlSendResult oversized = handler.SendPong(
            new byte[126],
            0,
            126);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(maximum.EncodedCount, Is.EqualTo(127));
            Assert.That(oversized.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(oversized.Accepted, Is.False);
            Assert.That(oversized.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(oversized.Error.Identifier,
                Is.EqualTo("control-send-payload-limit-exceeded"));
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(127));
        });
    }

    [Test]
    public void InvalidExplicitPayloadRangeDoesNotLatchTheHandler()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            true);
        var scheduler = new WebSocketSendScheduler(16);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            new WebSocketEventQueue(4));

        WebSocketControlSendResult invalid = handler.SendPing(
            [1, 2],
            1,
            2);
        WebSocketControlSendResult recovered = handler.SendPong(
            [3],
            0,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error.Identifier,
                Is.EqualTo("control-send-payload-range-invalid"));
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ExplicitSendCanRetryAfterOutputCapacityIsDrained()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Server,
            true);
        var scheduler = new WebSocketSendScheduler(5);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            new WebSocketEventQueue(4));
        handler.SendPing([1, 2, 3], 0, 3);

        WebSocketControlSendResult blocked = handler.SendPong([], 0, 0);
        var first = new byte[5];
        scheduler.WritePendingBytes(first);
        WebSocketControlSendResult retried = handler.SendPong([], 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(blocked.HasError, Is.False);
            Assert.That(retried.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ReceivedPingEmitsEventAndQueuesAnExactAutomaticPong()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(
            [0x89, 0x03, 9, 8, 7]);
        WebSocketEvent connectionEvent = RequiredEvent(
            consumer.ReadStreamingEvent());
        WebSocketSendScheduler scheduler =
            consumer.GetStreamingSendScheduler();
        var pong = new byte[9];
        int written = scheduler.WritePendingBytes(pong);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(connectionEvent.Payload,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(connectionEvent.First, Is.True);
            Assert.That(connectionEvent.Final, Is.True);
            Assert.That(written, Is.EqualTo(9));
            Assert.That(
                pong,
                Is.EqualTo(new byte[]
                {
                    0x8A, 0x83,
                    0x01, 0x02, 0x03, 0x04,
                    0x08, 0x0A, 0x04
                }));
        });
    }

    [Test]
    public void ReceivedUnsolicitedPongIsSurfacedWithoutAResponse()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(
            [0x8A, 0x02, 7, 8]);
        WebSocketEvent connectionEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(connectionEvent.Payload,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(
                consumer.GetStreamingSendScheduler().GetPendingByteCount(),
                Is.Zero);
        });
    }

    [Test]
    public void AutomaticPongCanBeDisabledWithoutSuppressingPingEvents()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            false);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(
            [0x89, 0x01, 5]);
        WebSocketEvent connectionEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(connectionEvent.Payload,
                Is.EqualTo(new byte[] { 5 }));
            Assert.That(
                consumer.GetStreamingSendScheduler().GetPendingByteCount(),
                Is.Zero);
        });
    }

    [Test]
    public void PingEventKeepsItsPositionBetweenFragmentEvents()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        byte[] wire = Combine(
            [0x01, 0x01, 65],
            [0x89, 0x01, 9],
            [0x80, 0x01, 66]);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(wire);
        List<WebSocketEventType> eventTypes = [];
        WebSocketEvent? connectionEvent = consumer.ReadStreamingEvent();
        while (connectionEvent != null)
        {
            eventTypes.Add(connectionEvent.Type);
            connectionEvent = consumer.ReadStreamingEvent();
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(
                eventTypes,
                Is.EqualTo(new[]
                {
                    WebSocketEventType.MessageStart,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.Ping,
                    WebSocketEventType.MessageData,
                    WebSocketEventType.MessageEnd
                }));
        });
    }

    [Test]
    public void AutomaticPongBackpressureStopsBeforePingPayloadConsumption()
    {
        WebSocketConnectionOptions options = SmallOutputOptions();
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketSendScheduler scheduler =
            consumer.GetStreamingSendScheduler();
        scheduler.TryQueueDataFrame(new byte[10]);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        var frame = new byte[] { 0x89, 0x01, 7 };

        WebSocketInboundFramePumpResult blocked =
            pump.PumpInboundFrames(frame);
        var queued = new byte[10];
        scheduler.WritePendingBytes(queued);
        WebSocketInboundFramePumpResult resumed =
            pump.PumpInboundFrames(frame.AsSpan(blocked.ConsumedCount));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(resumed.ConsumedCount, Is.EqualTo(1));
            Assert.That(consumer.GetStreamingEventCount(), Is.EqualTo(1));
            Assert.That(scheduler.GetPendingControlByteCount(),
                Is.EqualTo(7));
        });
    }

    [Test]
    public void AutomaticPongRetainsNoUnaccountedFrameWhileOutputIsBlocked()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        var keys = new WholeWriterMaskKeySource();
        options.HasMaskKeySource = true;
        options.MaskKeySource = keys;
        var scheduler = new WebSocketSendScheduler(8);
        var events = new WebSocketEventQueue(1);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            events);
        WebSocketFrameHeader header = Header(WebSocketOpcode.Ping, 1);
        WebSocketInboundFrameCapacity capacity =
            handler.GetInboundControlCapacity(header, true, 256);
        scheduler.TryQueueControlFrame(new byte[8]);
        var chunk = new WebSocketInboundFrameChunk
        {
            Header = header,
            FirstChunk = true,
            EndOfFrame = true,
            Payload = [7],
            PayloadCount = 1
        };

        WebSocketInboundFrameConsumeResult blocked =
            handler.ConsumeInboundControlChunk(chunk);
        int keysWhileBlocked = keys.CallCount;
        var prior = new byte[8];
        scheduler.WritePendingBytes(prior);
        WebSocketInboundFrameConsumeResult resumed =
            handler.ConsumeInboundControlChunk(chunk);
        var pong = new byte[7];
        scheduler.WritePendingBytes(pong);

        Assert.Multiple(() =>
        {
            Assert.That(capacity.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(keysWhileBlocked, Is.Zero);
            Assert.That(handler.GetOwnedControlPayloadCount(), Is.EqualTo(1));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(keys.CallCount, Is.EqualTo(1));
            Assert.That(events.GetEventCount(), Is.EqualTo(1));
            Assert.That(pong,
                Is.EqualTo(new byte[]
                {
                    0x8A, 0x81,
                    0x01, 0x02, 0x03, 0x04,
                    0x06
                }));
        });
    }

    [Test]
    public void EventBackpressureRetainsTheNextControlFrameForRetry()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            false);
        options.MaximumQueuedEvents = 1;
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketInboundFramePump pump = Pump(options, consumer);
        pump.PumpInboundFrames([0x89, 0x01, 7]);
        var second = new byte[] { 0x8A, 0x01, 8 };

        WebSocketInboundFramePumpResult blocked =
            pump.PumpInboundFrames(second);
        WebSocketEvent first = RequiredEvent(consumer.ReadStreamingEvent());
        WebSocketInboundFramePumpResult resumed =
            pump.PumpInboundFrames(second.AsSpan(blocked.ConsumedCount));
        WebSocketEvent secondEvent = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedEventCapacity));
            Assert.That(blocked.ConsumedCount, Is.EqualTo(2));
            Assert.That(first.Type, Is.EqualTo(WebSocketEventType.Ping));
            Assert.That(resumed.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(secondEvent.Type,
                Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(secondEvent.Payload,
                Is.EqualTo(new byte[] { 8 }));
        });
    }

    [Test]
    public void ReassembledModeAlsoSurfacesControlEvents()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            false);
        options.DeliveryMode = WebSocketMessageDeliveryMode.Reassembled;
        WebSocketExtensionPipeline pipeline = EmptyPipeline();
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            pipeline);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult result = pump.PumpInboundFrames(
            [0x8A, 0x02, 4, 5]);
        WebSocketEvent connectionEvent = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(connectionEvent.Type,
                Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(connectionEvent.Payload,
                Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void InvalidInboundControlLengthIsRejectedDeterministically()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            false);
        var handler = new WebSocketPingPongHandler(
            options,
            new WebSocketSendScheduler(256),
            new WebSocketEventQueue(4));
        WebSocketFrameHeader header = Header(
            WebSocketOpcode.Ping,
            126);

        WebSocketInboundFrameCapacity failed =
            handler.GetInboundControlCapacity(header, true, 256);
        WebSocketInboundFrameCapacity repeated =
            handler.GetInboundControlCapacity(
                Header(WebSocketOpcode.Pong, 0),
                true,
                256);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.HasError, Is.True);
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("control-frame-header-invalid"));
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("control-frame-header-invalid"));
        });
    }

    static WebSocketStreamingReceiveConsumer Consumer(
        WebSocketConnectionOptions options)
    {
        return new WebSocketStreamingReceiveConsumer(
            options,
            EmptyPipeline());
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

    static WebSocketConnectionOptions Options(
        WebSocketRole role,
        bool automaticPong)
    {
        return new WebSocketConnectionOptions
        {
            Role = role,
            DeliveryMode = WebSocketMessageDeliveryMode.Streaming,
            AutomaticPong = automaticPong,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            AutomaticFramePayloadLength = 32,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 16
        };
    }

    static WebSocketConnectionOptions SmallOutputOptions()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketRole.Client,
            true);
        options.MaximumFramePayloadLength = 8;
        options.MaximumMessagePayloadLength = 8;
        options.MaximumDecompressedMessagePayloadLength = 8;
        options.AutomaticFramePayloadLength = 1;
        options.MaximumFragmentCount = 8;
        options.MaximumBufferedOutputBytes = 15;
        return options;
    }

    static WebSocketFrameHeader Header(
        WebSocketOpcode opcode,
        long payloadLength)
    {
        return new WebSocketFrameHeader
        {
            Final = true,
            Opcode = opcode,
            PayloadLength = payloadLength
        };
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

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}
