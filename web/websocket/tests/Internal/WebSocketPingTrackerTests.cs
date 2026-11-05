using websocket;

namespace websocket.Tests;

public class WebSocketPingTrackerTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void ConstructorRejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<WebSocketLimitException>(() =>
            new WebSocketPingTracker(capacity));
    }

    [Test]
    public void MatchesMultipleOutstandingTokensOutOfOrderWithLatency()
    {
        var tracker = new WebSocketPingTracker(3);
        WebSocketPingTrackingResult first = tracker.TrackPingToken(
            [1],
            0,
            1,
            100);
        WebSocketPingTrackingResult second = tracker.TrackPingToken(
            [2],
            0,
            1,
            120);

        WebSocketPingTrackingResult secondMatch = tracker.MatchPongToken(
            [2],
            0,
            1,
            155);
        WebSocketPingTrackingResult firstMatch = tracker.MatchPongToken(
            [1],
            0,
            1,
            200);

        Assert.Multiple(() =>
        {
            Assert.That(first.OutstandingCount, Is.EqualTo(1));
            Assert.That(second.OutstandingCount, Is.EqualTo(2));
            Assert.That(secondMatch.Matched, Is.True);
            Assert.That(secondMatch.OutstandingCount, Is.EqualTo(1));
            Assert.That(secondMatch.AcknowledgementEvent.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(secondMatch.AcknowledgementEvent.Payload,
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(
                secondMatch.AcknowledgementEvent.HasPingElapsedMilliseconds,
                Is.True);
            Assert.That(
                secondMatch.AcknowledgementEvent.PingElapsedMilliseconds,
                Is.EqualTo(35));
            Assert.That(firstMatch.Matched, Is.True);
            Assert.That(
                firstMatch.AcknowledgementEvent.PingElapsedMilliseconds,
                Is.EqualTo(100));
            Assert.That(tracker.GetOutstandingPingCount(), Is.Zero);
        });
    }

    [Test]
    public void UnmatchedPongIsBenignAndLeavesOutstandingTokens()
    {
        var tracker = new WebSocketPingTracker(2);
        tracker.TrackPingToken([1], 0, 1, 10);

        WebSocketPingTrackingResult unmatched = tracker.MatchPongToken(
            [9],
            0,
            1,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(unmatched.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(unmatched.Accepted, Is.True);
            Assert.That(unmatched.Matched, Is.False);
            Assert.That(unmatched.HasAcknowledgementEvent, Is.False);
            Assert.That(unmatched.HasError, Is.False);
            Assert.That(unmatched.OutstandingCount, Is.EqualTo(1));
            Assert.That(tracker.ContainsPingToken([1], 0, 1), Is.True);
        });
    }

    [Test]
    public void DuplicateOutstandingTokenIsRejectedWithoutReplacingSendTime()
    {
        var tracker = new WebSocketPingTracker(2);
        tracker.TrackPingToken([4, 5], 0, 2, 100);

        WebSocketPingTrackingResult duplicate = tracker.TrackPingToken(
            [4, 5],
            0,
            2,
            150);
        WebSocketPingTrackingResult matched = tracker.MatchPongToken(
            [4, 5],
            0,
            2,
            180);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("ping-token-duplicate"));
            Assert.That(
                matched.AcknowledgementEvent.PingElapsedMilliseconds,
                Is.EqualTo(80));
            Assert.That(tracker.GetOutstandingPingCount(), Is.Zero);
        });
    }

    [Test]
    public void TrackedTokenAndAcknowledgementAreIndependentCopies()
    {
        var tracker = new WebSocketPingTracker(2);
        var source = new byte[] { 0, 7, 8, 0 };
        tracker.TrackPingToken(source, 1, 2, 10);
        source[1] = 90;

        WebSocketPingTrackingResult matched = tracker.MatchPongToken(
            [7, 8],
            0,
            2,
            20);
        byte[] eventPayload = matched.AcknowledgementEvent.Payload;
        source[2] = 91;

        Assert.Multiple(() =>
        {
            Assert.That(matched.Matched, Is.True);
            Assert.That(eventPayload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(eventPayload, Is.Not.SameAs(source));
        });
    }

    [Test]
    public void CapacityIsReclaimedAfterAcknowledgement()
    {
        var tracker = new WebSocketPingTracker(1);
        tracker.TrackPingToken([1], 0, 1, 0);

        WebSocketPingTrackingResult full = tracker.TrackPingToken(
            [2],
            0,
            1,
            0);
        tracker.MatchPongToken([1], 0, 1, 1);
        WebSocketPingTrackingResult recovered = tracker.TrackPingToken(
            [2],
            0,
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(full.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(full.Error.Identifier,
                Is.EqualTo("ping-tracking-capacity-exceeded"));
            Assert.That(recovered.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(tracker.GetOutstandingPingCapacity(), Is.EqualTo(1));
            Assert.That(tracker.GetOutstandingPingCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void EmptyTokenCanBeTrackedAndAcknowledgedOnce()
    {
        var tracker = new WebSocketPingTracker(2);
        WebSocketPingTrackingResult tracked = tracker.TrackPingToken(
            [],
            0,
            0,
            5);
        WebSocketPingTrackingResult duplicate = tracker.TrackPingToken(
            [],
            0,
            0,
            6);
        WebSocketPingTrackingResult matched = tracker.MatchPongToken(
            [],
            0,
            0,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(tracked.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("ping-token-duplicate"));
            Assert.That(matched.Matched, Is.True);
            Assert.That(matched.AcknowledgementEvent.Payload, Is.Empty);
            Assert.That(
                matched.AcknowledgementEvent.PingElapsedMilliseconds,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void RegressedPongTimeDoesNotRemoveTheToken()
    {
        var tracker = new WebSocketPingTracker(1);
        tracker.TrackPingToken([3], 0, 1, 20);

        WebSocketPingTrackingResult regressed = tracker.MatchPongToken(
            [3],
            0,
            1,
            19);
        WebSocketPingTrackingResult recovered = tracker.MatchPongToken(
            [3],
            0,
            1,
            25);

        Assert.Multiple(() =>
        {
            Assert.That(regressed.Error.Identifier,
                Is.EqualTo("ping-elapsed-time-regressed"));
            Assert.That(recovered.Matched, Is.True);
            Assert.That(
                recovered.AcknowledgementEvent.PingElapsedMilliseconds,
                Is.EqualTo(5));
        });
    }

    [Test]
    public void RemovalAndClearProvideTerminalLifecycleCleanup()
    {
        var tracker = new WebSocketPingTracker(3);
        tracker.TrackPingToken([1], 0, 1, 1);
        tracker.TrackPingToken([2], 0, 1, 2);

        bool removed = tracker.RemovePingToken([1], 0, 1);
        tracker.ClearOutstandingPings();
        WebSocketPingTrackingResult reused = tracker.TrackPingToken(
            [2],
            0,
            1,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(reused.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(tracker.GetOutstandingPingCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidTokenRangesAndTimesAreRejectedWithoutMutation()
    {
        var tracker = new WebSocketPingTracker(2);

        WebSocketPingTrackingResult range = tracker.TrackPingToken(
            [1],
            1,
            1,
            0);
        WebSocketPingTrackingResult tooLong = tracker.TrackPingToken(
            new byte[126],
            0,
            126,
            0);
        WebSocketPingTrackingResult negative = tracker.TrackPingToken(
            [1],
            0,
            1,
            -1);

        Assert.Multiple(() =>
        {
            Assert.That(range.Error.Identifier,
                Is.EqualTo("ping-token-range-invalid"));
            Assert.That(tooLong.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(negative.Error.Identifier,
                Is.EqualTo("ping-sent-time-invalid"));
            Assert.That(tracker.GetOutstandingPingCount(), Is.Zero);
        });
    }

    [Test]
    public void HandlerTurnsMatchingPongIntoAcknowledgementEvent()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketPingPongHandler handler =
            consumer.GetStreamingPingPongHandler();
        handler.SetPingElapsedMilliseconds(100);
        WebSocketControlSendResult sent = handler.SendPing(
            [3, 4],
            0,
            2);
        handler.SetPingElapsedMilliseconds(145);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult received = pump.PumpInboundFrames(
            [0x8A, 0x02, 3, 4]);
        WebSocketEvent acknowledged = RequiredEvent(
            consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(received.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(acknowledged.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(acknowledged.Payload,
                Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(acknowledged.PingElapsedMilliseconds,
                Is.EqualTo(45));
            Assert.That(handler.GetOutstandingTrackedPingCount(), Is.Zero);
            Assert.That(consumer.GetOwnedPayloadCount(), Is.Zero);
        });
    }

    [Test]
    public void HandlerLeavesMismatchedPongUnsolicitedAndOutstanding()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketPingPongHandler handler =
            consumer.GetStreamingPingPongHandler();
        handler.SendTrackedPing([1], 0, 1, 10);
        handler.SetPingElapsedMilliseconds(20);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        pump.PumpInboundFrames([0x8A, 0x01, 2]);
        WebSocketEvent pong = RequiredEvent(consumer.ReadStreamingEvent());

        Assert.Multiple(() =>
        {
            Assert.That(pong.Type, Is.EqualTo(WebSocketEventType.Pong));
            Assert.That(pong.HasPingElapsedMilliseconds, Is.False);
            Assert.That(handler.GetOutstandingTrackedPingCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void HandlerRejectsDuplicateWithoutQueueingAnotherPing()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketPingPongHandler handler =
            consumer.GetStreamingPingPongHandler();

        handler.SetPingElapsedMilliseconds(10);
        WebSocketControlSendResult first = handler.SendPing(
            [5],
            0,
            1);
        int firstPending = consumer.GetStreamingSendScheduler()
            .GetPendingByteCount();
        handler.SetPingElapsedMilliseconds(11);
        WebSocketControlSendResult duplicate = handler.SendPing(
            [5],
            0,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("ping-token-duplicate"));
            Assert.That(consumer.GetStreamingSendScheduler()
                .GetPendingByteCount(), Is.EqualTo(firstPending));
            Assert.That(handler.GetOutstandingTrackedPingCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void OutputBackpressureRollsBackTrackingForRetry()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        var scheduler = new WebSocketSendScheduler(8);
        var handler = new WebSocketPingPongHandler(
            options,
            scheduler,
            new WebSocketEventQueue(4));
        scheduler.TryQueueDataFrame([9, 9, 9, 9, 9, 9]);

        WebSocketControlSendResult blocked = handler.SendTrackedPing(
            [1],
            0,
            1,
            10);
        var drained = new byte[6];
        scheduler.WritePendingBytes(drained);
        WebSocketControlSendResult retried = handler.SendTrackedPing(
            [1],
            0,
            1,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.Accepted, Is.False);
            Assert.That(retried.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(handler.GetOutstandingTrackedPingCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void CloseAndFailureCleanupRemoveOutstandingTokens()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        var handler = new WebSocketPingPongHandler(
            options,
            new WebSocketSendScheduler(256),
            new WebSocketEventQueue(4));
        handler.SendTrackedPing([1], 0, 1, 1);

        handler.ClearTrackedPingState();
        WebSocketControlSendResult reused = handler.SendTrackedPing(
            [1],
            0,
            1,
            2);
        WebSocketFrameHeader invalid = Header(
            WebSocketOpcode.Pong,
            126);
        handler.GetInboundControlCapacity(invalid, true, 256);

        Assert.Multiple(() =>
        {
            Assert.That(reused.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(handler.GetOutstandingTrackedPingCount(), Is.Zero);
        });
    }

    [Test]
    public void ReceiverFailureClearsOutstandingTokens()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Streaming);
        WebSocketStreamingReceiveConsumer consumer = Consumer(options);
        WebSocketPingPongHandler handler =
            consumer.GetStreamingPingPongHandler();
        handler.SendPing([1], 0, 1);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        WebSocketInboundFramePumpResult failed = pump.PumpInboundFrames(
            [0x81, 0x01, 0xC0]);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(handler.GetOutstandingTrackedPingCount(), Is.Zero);
        });
    }

    [Test]
    public void ReassembledModeForwardsAcknowledgementEvents()
    {
        WebSocketConnectionOptions options = Options(
            WebSocketMessageDeliveryMode.Reassembled);
        var consumer = new WebSocketReassembledReceiveConsumer(
            options,
            EmptyPipeline());
        WebSocketPingPongHandler handler =
            consumer.GetReassembledPingPongHandler();
        handler.SendTrackedPing([6], 0, 1, 30);
        handler.SetPingElapsedMilliseconds(38);
        WebSocketInboundFramePump pump = Pump(options, consumer);

        pump.PumpInboundFrames([0x8A, 0x01, 6]);
        WebSocketEvent acknowledged = RequiredEvent(
            consumer.ReadReassembledEvent());

        Assert.Multiple(() =>
        {
            Assert.That(acknowledged.Type,
                Is.EqualTo(WebSocketEventType.PingAcknowledged));
            Assert.That(acknowledged.PingElapsedMilliseconds,
                Is.EqualTo(8));
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
        WebSocketMessageDeliveryMode deliveryMode)
    {
        return new WebSocketConnectionOptions
        {
            Role = WebSocketRole.Client,
            DeliveryMode = deliveryMode,
            AutomaticPong = true,
            MaximumFramePayloadLength = 128,
            MaximumMessagePayloadLength = 128,
            MaximumDecompressedMessagePayloadLength = 128,
            AutomaticFramePayloadLength = 32,
            MaximumFragmentCount = 4,
            MaximumBufferedInputBytes = 256,
            MaximumBufferedOutputBytes = 256,
            MaximumQueuedEvents = 4
        };
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

    static WebSocketEvent RequiredEvent(WebSocketEvent? connectionEvent)
    {
        Assert.That(connectionEvent, Is.Not.Null);
        return connectionEvent!;
    }
}
