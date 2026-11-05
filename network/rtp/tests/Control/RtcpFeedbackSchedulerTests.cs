using rtp;

namespace rtp.Tests.Control;

public class RtcpFeedbackSchedulerTests
{
    [Test]
    public void PointToPointImmediateFeedbackHonorsTrrIntAndBandwidthCredit()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.MinimumFeedbackIntervalTicks =
            2L * TimeSpan.TicksPerSecond;
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            configuration,
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            2,
            1,
            10L * TimeSpan.TicksPerSecond);
        RtpFeedbackRequest immediateRequest = CreateRequest(
            RtcpFeedbackTimingMode.Immediate,
            TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(immediateRequest);
        immediateRequest.Feedback!.MediaSourceIdentifier = 99U;

        RtpFeedbackDecision immediate = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);

        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            TimeSpan.TicksPerSecond));
        RtpFeedbackDecision regular = scheduler.PollRtcpFeedback(
            snapshot,
            10L * TimeSpan.TicksPerSecond);
        snapshot.NextRtcpSendTimeTicks = 20L * TimeSpan.TicksPerSecond;
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Immediate,
            10L * TimeSpan.TicksPerSecond));
        RtpFeedbackDecision throttled = scheduler.PollRtcpFeedback(
            snapshot,
            10L * TimeSpan.TicksPerSecond);
        RtpFeedbackDecision replenished = scheduler.PollRtcpFeedback(
            snapshot,
            12L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(immediate.ShouldSend, Is.True);
            Assert.That(immediate.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(immediate.Piggyback, Is.False);
            Assert.That(immediate.ScheduledTimeTicks,
                Is.EqualTo(TimeSpan.TicksPerSecond));
            Assert.That(immediate.Request?.Feedback?.MediaSourceIdentifier,
                Is.Zero);
            Assert.That(regular.ShouldSend, Is.True);
            Assert.That(regular.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Regular));
            Assert.That(regular.Piggyback, Is.True);
            Assert.That(throttled.ShouldSend, Is.False);
            Assert.That(throttled.ScheduledTimeTicks,
                Is.EqualTo(12L * TimeSpan.TicksPerSecond));
            Assert.That(replenished.ShouldSend, Is.True);
            Assert.That(replenished.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(scheduler.feedbackByteCredit, Is.Zero);
            Assert.That(scheduler.allowEarly, Is.False);
            Assert.That(randomSource.UnitIntervalCallCount, Is.Zero);
        });
    }

    [Test]
    public void MultipartyEarlyFeedbackIsDitheredWithinTheRegularDeadline()
    {
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            4,
            1,
            10L * TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Early,
            TimeSpan.TicksPerSecond));

        RtpFeedbackDecision waiting = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision ready = scheduler.PollRtcpFeedback(
            snapshot,
            waiting.ScheduledTimeTicks);
        RtpFeedbackDecision regularBoundary = scheduler.PollRtcpFeedback(
            snapshot,
            10L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.ShouldSend, Is.False);
            Assert.That(waiting.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(waiting.Piggyback, Is.False);
            Assert.That(waiting.ScheduledTimeTicks,
                Is.EqualTo(22500000L));
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(ready.ScheduledTimeTicks,
                Is.EqualTo(waiting.ScheduledTimeTicks));
            Assert.That(randomSource.UnitIntervalCallCount, Is.EqualTo(1));
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(regularBoundary.ShouldSend, Is.False);
            Assert.That(scheduler.allowEarly, Is.True);
        });
    }

    [Test]
    public void EarlyAllowanceResetsAfterRegularFeedbackPiggyback()
    {
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            4,
            1,
            10L * TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Early,
            0L));

        RtpFeedbackDecision firstEarly = scheduler.PollRtcpFeedback(
            snapshot,
            0L);
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Early,
            TimeSpan.TicksPerSecond));
        RtpFeedbackDecision waitingForRegular = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision regular = scheduler.PollRtcpFeedback(
            snapshot,
            10L * TimeSpan.TicksPerSecond);

        snapshot.NextRtcpSendTimeTicks = 20L * TimeSpan.TicksPerSecond;
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Early,
            10L * TimeSpan.TicksPerSecond));
        RtpFeedbackDecision secondEarly = scheduler.PollRtcpFeedback(
            snapshot,
            10L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(firstEarly.ShouldSend, Is.True);
            Assert.That(firstEarly.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(firstEarly.Piggyback, Is.False);
            Assert.That(waitingForRegular.ShouldSend, Is.False);
            Assert.That(waitingForRegular.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Regular));
            Assert.That(waitingForRegular.Piggyback, Is.True);
            Assert.That(waitingForRegular.ScheduledTimeTicks,
                Is.EqualTo(10L * TimeSpan.TicksPerSecond));
            Assert.That(regular.ShouldSend, Is.True);
            Assert.That(regular.Piggyback, Is.True);
            Assert.That(secondEarly.ShouldSend, Is.True);
            Assert.That(secondEarly.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(secondEarly.Piggyback, Is.False);
            Assert.That(randomSource.UnitIntervalCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void FeedbackNearARegularDeadlineIsPiggybackedWithoutDithering()
    {
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            4,
            1,
            2L * TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Immediate,
            TimeSpan.TicksPerSecond));

        RtpFeedbackDecision waiting = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision ready = scheduler.PollRtcpFeedback(
            snapshot,
            2L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.ShouldSend, Is.False);
            Assert.That(waiting.Piggyback, Is.True);
            Assert.That(waiting.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Regular));
            Assert.That(waiting.ScheduledTimeTicks,
                Is.EqualTo(2L * TimeSpan.TicksPerSecond));
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(randomSource.UnitIntervalCallCount, Is.Zero);
            Assert.That(scheduler.allowEarly, Is.True);
        });
    }

    [Test]
    public void ExhaustedFeedbackBandwidthFallsBackToTheRegularReport()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.SessionBandwidthBitsPerSecond = 800.0;
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            configuration,
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            2,
            1,
            TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Immediate,
            0L));
        RtpFeedbackDecision first = scheduler.PollRtcpFeedback(snapshot, 0L);
        scheduler.PollRtcpFeedback(snapshot, TimeSpan.TicksPerSecond);
        snapshot.NextRtcpSendTimeTicks = 10L * TimeSpan.TicksPerSecond;
        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Early,
            TimeSpan.TicksPerSecond));

        RtpFeedbackDecision limited = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(first.ShouldSend, Is.True);
            Assert.That(scheduler.feedbackByteCredit, Is.EqualTo(2.5));
            Assert.That(limited.ShouldSend, Is.False);
            Assert.That(limited.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Regular));
            Assert.That(limited.Piggyback, Is.True);
            Assert.That(limited.ScheduledTimeTicks,
                Is.EqualTo(10L * TimeSpan.TicksPerSecond));
        });
    }

    [Test]
    public void InvalidRequestsAndResetDoNotRetainFeedbackWork()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            configuration,
            randomSource);
        RtpFeedbackRequest missingPacket = new RtpFeedbackRequest();
        RtpFeedbackRequest invalidTimes = CreateRequest(
            RtcpFeedbackTimingMode.Early,
            10L);
        invalidTimes.ExpirationTimeTicks = 9L;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                new RtcpFeedbackScheduler(configuration, null!));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(missingPacket));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(invalidTimes));
        });

        scheduler.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            10L));
        scheduler.ResetRtcpFeedbackScheduler(20L);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(scheduler.hasScheduledFeedback, Is.False);
            Assert.That(scheduler.allowEarly, Is.True);
            Assert.That(scheduler.feedbackByteCredit,
                Is.EqualTo(configuration.InitialAveragePacketSize));
        });
    }

    [Test]
    public void ReceivedGenericNackSupersetSuppressesPendingFeedback()
    {
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            new FixedFeedbackRandomSource());
        RtcpFeedbackPacket received = CreateGenericNackFeedback(
            77U,
            100,
            1);
        received.SenderSourceIdentifier = 900U;
        scheduler.ObserveRtcpFeedback(
            received,
            TimeSpan.TicksPerSecond);
        RtpFeedbackRequest request = CreateGenericNackRequest(
            77U,
            101,
            0,
            RtcpFeedbackTimingMode.Early,
            2L * TimeSpan.TicksPerSecond);
        request.Feedback!.SenderSourceIdentifier = 901U;
        scheduler.QueueRtcpFeedback(request);

        RtpFeedbackDecision decision = scheduler.PollRtcpFeedback(
            CreateSnapshot(4, 1, 10L * TimeSpan.TicksPerSecond),
            2L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldSend, Is.False);
            Assert.That(decision.Suppressed, Is.True);
            Assert.That(decision.Piggyback, Is.False);
            Assert.That(decision.Request?.Feedback?.MediaSourceIdentifier,
                Is.EqualTo(77U));
            Assert.That(scheduler.pendingRequests, Is.Empty);
        });
    }

    [Test]
    public void EquivalentFeedbackObservedDuringDitherSuppressesScheduledRequest()
    {
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            randomSource);
        RtpSessionSnapshot snapshot = CreateSnapshot(
            4,
            1,
            10L * TimeSpan.TicksPerSecond);
        scheduler.QueueRtcpFeedback(CreateGenericNackRequest(
            77U,
            100,
            1,
            RtcpFeedbackTimingMode.Early,
            TimeSpan.TicksPerSecond));

        RtpFeedbackDecision scheduled = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        scheduler.ObserveRtcpFeedback(
            CreateGenericNackFeedback(77U, 100, 1),
            15L * TimeSpan.TicksPerSecond / 10L);
        RtpFeedbackDecision suppressed = scheduler.PollRtcpFeedback(
            snapshot,
            15L * TimeSpan.TicksPerSecond / 10L);

        Assert.Multiple(() =>
        {
            Assert.That(scheduled.ShouldSend, Is.False);
            Assert.That(scheduled.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(scheduled.ScheduledTimeTicks,
                Is.EqualTo(22500000L));
            Assert.That(suppressed.ShouldSend, Is.False);
            Assert.That(suppressed.Suppressed, Is.True);
            Assert.That(suppressed.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(suppressed.ScheduledTimeTicks,
                Is.EqualTo(15L * TimeSpan.TicksPerSecond / 10L));
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(scheduler.hasScheduledFeedback, Is.False);
            Assert.That(randomSource.UnitIntervalCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReducedSizeFeedbackRequiresAvpfAndReducedSizeNegotiation()
    {
        RtcpSchedulerConfiguration negotiatedConfiguration =
            CreateConfiguration();
        negotiatedConfiguration.ReducedSizeEnabled = true;
        RtcpFeedbackScheduler negotiated = new RtcpFeedbackScheduler(
            negotiatedConfiguration,
            new FixedFeedbackRandomSource());
        negotiated.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.ReducedSize,
            TimeSpan.TicksPerSecond));

        RtcpSchedulerConfiguration unnegotiatedConfiguration =
            CreateConfiguration();
        RtcpFeedbackScheduler unnegotiated = new RtcpFeedbackScheduler(
            unnegotiatedConfiguration,
            new FixedFeedbackRandomSource());
        unnegotiated.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.ReducedSize,
            TimeSpan.TicksPerSecond));

        RtcpSchedulerConfiguration disabledConfiguration =
            CreateConfiguration();
        disabledConfiguration.FeedbackEnabled = false;
        disabledConfiguration.ReducedSizeEnabled = true;
        RtcpFeedbackScheduler disabled = new RtcpFeedbackScheduler(
            disabledConfiguration,
            new FixedFeedbackRandomSource());
        disabled.QueueRtcpFeedback(CreateRequest(
            RtcpFeedbackTimingMode.ReducedSize,
            TimeSpan.TicksPerSecond));

        RtpSessionSnapshot snapshot = CreateSnapshot(
            2,
            1,
            10L * TimeSpan.TicksPerSecond);
        RtpFeedbackDecision reducedSize = negotiated.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision early = unnegotiated.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision regular = disabled.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(reducedSize.ShouldSend, Is.True);
            Assert.That(reducedSize.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.ReducedSize));
            Assert.That(reducedSize.Piggyback, Is.False);
            Assert.That(early.ShouldSend, Is.True);
            Assert.That(early.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(early.Piggyback, Is.False);
            Assert.That(regular.ShouldSend, Is.False);
            Assert.That(regular.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Regular));
            Assert.That(regular.Piggyback, Is.True);
            Assert.That(regular.ScheduledTimeTicks,
                Is.EqualTo(10L * TimeSpan.TicksPerSecond));
        });
    }

    [Test]
    public void PartialGenericNackDoesNotSuppressAdditionalLoss()
    {
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            new FixedFeedbackRandomSource());
        scheduler.ObserveRtcpFeedback(
            CreateGenericNackFeedback(77U, 100, 0),
            0L);
        scheduler.QueueRtcpFeedback(CreateGenericNackRequest(
            77U,
            100,
            1,
            RtcpFeedbackTimingMode.Immediate,
            0L));

        RtpFeedbackDecision decision = scheduler.PollRtcpFeedback(
            CreateSnapshot(2, 1, 10L * TimeSpan.TicksPerSecond),
            0L);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldSend, Is.True);
            Assert.That(decision.Suppressed, Is.False);
            Assert.That(decision.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
        });
    }

    [Test]
    public void GenericNackRequestsCoalesceAndKeepPiggybackScheduling()
    {
        FixedFeedbackRandomSource randomSource =
            new FixedFeedbackRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            randomSource);
        RtpFeedbackRequest first = CreateGenericNackRequest(
            77U,
            100,
            0,
            RtcpFeedbackTimingMode.Regular,
            2L * TimeSpan.TicksPerSecond);
        first.ExpirationTimeTicks = 4L * TimeSpan.TicksPerSecond;
        RtpFeedbackRequest second = CreateGenericNackRequest(
            77U,
            105,
            0,
            RtcpFeedbackTimingMode.Immediate,
            TimeSpan.TicksPerSecond);
        second.ExpirationTimeTicks = 6L * TimeSpan.TicksPerSecond;
        second.AllowSuppression = false;
        scheduler.QueueRtcpFeedback(first);
        scheduler.QueueRtcpFeedback(second);

        RtpSessionSnapshot snapshot = CreateSnapshot(
            4,
            1,
            2L * TimeSpan.TicksPerSecond);
        RtpFeedbackDecision waiting = scheduler.PollRtcpFeedback(
            snapshot,
            TimeSpan.TicksPerSecond);
        RtpFeedbackDecision ready = scheduler.PollRtcpFeedback(
            snapshot,
            2L * TimeSpan.TicksPerSecond);
        ushort[] lostSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                ready.Request!.Feedback!.GenericNack!);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.ShouldSend, Is.False);
            Assert.That(waiting.Piggyback, Is.True);
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(ready.Piggyback, Is.True);
            Assert.That(ready.Request?.EarliestSendTimeTicks,
                Is.EqualTo(TimeSpan.TicksPerSecond));
            Assert.That(ready.Request?.ExpirationTimeTicks,
                Is.EqualTo(6L * TimeSpan.TicksPerSecond));
            Assert.That(ready.Request?.PreferredTimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(ready.Request?.AllowSuppression, Is.False);
            Assert.That(lostSequenceNumbers,
                Is.EquivalentTo(new ushort[] { 100, 105 }));
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(randomSource.UnitIntervalCallCount, Is.Zero);
        });
    }

    [Test]
    public void FeedbackThatCannotMeetItsExpirationIsDiscarded()
    {
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            new FixedFeedbackRandomSource());
        RtpFeedbackRequest request = CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            TimeSpan.TicksPerSecond);
        request.ExpirationTimeTicks = 2L * TimeSpan.TicksPerSecond;
        scheduler.QueueRtcpFeedback(request);

        RtpFeedbackDecision decision = scheduler.PollRtcpFeedback(
            CreateSnapshot(4, 1, 10L * TimeSpan.TicksPerSecond),
            TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldSend, Is.False);
            Assert.That(decision.Suppressed, Is.False);
            Assert.That(decision.Piggyback, Is.False);
            Assert.That(decision.ScheduledTimeTicks,
                Is.EqualTo(2L * TimeSpan.TicksPerSecond));
            Assert.That(decision.Request, Is.Not.Null);
            Assert.That(scheduler.pendingRequests, Is.Empty);
        });
    }

    [Test]
    public void FeedbackRetentionCannotBeConfiguredBelowTwoSeconds()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.FeedbackRetentionTicks =
            RtcpProtocolConstants.MinimumFeedbackRetentionTicks - 1L;

        Assert.Throws<RtpConfigurationException>(() =>
            new RtcpFeedbackScheduler(
                configuration,
                new FixedFeedbackRandomSource()));
    }

    [Test]
    public void FeedbackOperationsRejectEveryInvalidRequestSnapshotAndTime()
    {
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            CreateConfiguration(),
            new FixedFeedbackRandomSource());
        RtpFeedbackRequest negativeEarliest = CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            -1L);
        RtpFeedbackRequest negativeExpiration = CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            0L);
        negativeExpiration.ExpirationTimeTicks = -1L;
        RtpFeedbackRequest invalidMode = CreateRequest(
            (RtcpFeedbackTimingMode)99,
            0L);
        RtpSessionSnapshot noSources = CreateSnapshot(1, 0, 0L);
        noSources.Sources = null!;
        RtpSessionSnapshot noMembers = CreateSnapshot(0, 0, 0L);
        RtpSessionSnapshot negativeSenders = CreateSnapshot(1, -1, 0L);
        RtpSessionSnapshot tooManySenders = CreateSnapshot(1, 2, 0L);
        RtpSessionSnapshot negativeDeadline = CreateSnapshot(1, 0, -1L);
        RtpSessionSnapshot nullSource = CreateSnapshot(1, 0, 0L);
        nullSource.Sources = new RtpSourceSnapshot[] { null! };
        RtpSessionSnapshot valid = CreateSnapshot(1, 0, 0L);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(negativeEarliest));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(negativeExpiration));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.QueueRtcpFeedback(invalidMode));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ObserveRtcpFeedback(null!, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ObserveRtcpFeedback(
                    CreateRequest(RtcpFeedbackTimingMode.Regular, 0L).Feedback!,
                    -1L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(null!, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(noSources, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(noMembers, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(negativeSenders, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(tooManySenders, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(negativeDeadline, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(nullSource, 0L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.PollRtcpFeedback(valid, -1L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ResetRtcpFeedbackScheduler(-1L));
        });
    }

    [Test]
    public void FeedbackSchedulerHelpersCoverTimingMergingAndBandwidthEdges()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            configuration,
            new FixedFeedbackRandomSource());

        scheduler.ScheduleRegularFeedback(5L, 6L);
        Assert.That(scheduler.scheduledFeedbackTimeTicks, Is.EqualTo(6L));
        Assert.Multiple(() =>
        {
            Assert.That(scheduler.GetFeedbackTimingPriority(
                RtcpFeedbackTimingMode.Immediate), Is.EqualTo(3));
            Assert.That(scheduler.GetFeedbackTimingPriority(
                RtcpFeedbackTimingMode.ReducedSize), Is.EqualTo(2));
            Assert.That(scheduler.GetFeedbackTimingPriority(
                RtcpFeedbackTimingMode.Early), Is.EqualTo(1));
            Assert.That(scheduler.GetFeedbackTimingPriority(
                RtcpFeedbackTimingMode.Regular), Is.Zero);
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.AddFeedbackTime(long.MaxValue, 1L));
        });

        RtpFeedbackRequest missingFeedback = new RtpFeedbackRequest();
        Assert.Throws<RtpSessionException>(() =>
            scheduler.GetRequiredRequestFeedback(missingFeedback));
        scheduler.hasSchedulerTime = true;
        scheduler.schedulerTimeTicks = 10L;
        Assert.Throws<RtpSessionException>(() =>
            scheduler.ValidateFeedbackChronology(9L));
        scheduler.bandwidthUpdateTimeTicks = 10L;
        Assert.Throws<RtpSessionException>(() => scheduler
            .UpdateFeedbackBandwidthCredit(CreateSnapshot(1, 0, 20L), 9L));

        RtpFeedbackRequest pending = CreateRequest(
            RtcpFeedbackTimingMode.Regular,
            10L);
        pending.ExpirationTimeTicks = 20L;
        pending.AllowSuppression = true;
        RtpFeedbackRequest incoming = CreateRequest(
            RtcpFeedbackTimingMode.Early,
            5L);
        incoming.ExpirationTimeTicks = 30L;
        incoming.AllowSuppression = false;
        scheduler.MergeFeedbackRequest(pending, incoming);
        Assert.Multiple(() =>
        {
            Assert.That(pending.EarliestSendTimeTicks, Is.EqualTo(5L));
            Assert.That(pending.ExpirationTimeTicks, Is.EqualTo(30L));
            Assert.That(pending.PreferredTimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(pending.AllowSuppression, Is.False);
        });

        incoming.ExpirationTimeTicks = 0L;
        scheduler.MergeFeedbackRequest(pending, incoming);
        Assert.That(pending.ExpirationTimeTicks, Is.Zero);

        scheduler.observedFeedback.Add(incoming.Feedback!.Copy());
        scheduler.observedFeedbackArrivalTimes.Add(0L);
        scheduler.PruneObservedFeedback(
            configuration.FeedbackRetentionTicks + 1L);
        Assert.That(scheduler.observedFeedback, Is.Empty);

        RtpSessionSnapshot snapshot = CreateSnapshot(3, 1, 0L);
        snapshot.AverageRtcpPacketSize = 0;
        Assert.That(scheduler.GetFeedbackAveragePacketSize(snapshot),
            Is.EqualTo(configuration.InitialAveragePacketSize));
        snapshot.AverageRtcpPacketSize = 99;
        Assert.That(scheduler.GetFeedbackAveragePacketSize(snapshot),
            Is.EqualTo(99));

        RtpSourceSnapshot receiver = new RtpSourceSnapshot();
        receiver.IsLocal = true;
        snapshot.Sources = new RtpSourceSnapshot[] { receiver };
        Assert.That(scheduler.HasActiveLocalSender(snapshot), Is.False);
        receiver.IsSender = true;
        Assert.That(scheduler.HasActiveLocalSender(snapshot), Is.True);
    }

    private static RtcpSchedulerConfiguration CreateConfiguration()
    {
        RtcpSchedulerConfiguration configuration =
            new RtcpSchedulerConfiguration();
        configuration.SessionBandwidthBitsPerSecond = 80000.0;
        configuration.FeedbackEnabled = true;
        return configuration;
    }

    private static RtpSessionSnapshot CreateSnapshot(
        int memberCount,
        int senderCount,
        long nextRegularTimeTicks)
    {
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = memberCount;
        snapshot.SenderCount = senderCount;
        snapshot.AverageRtcpPacketSize = 100;
        snapshot.NextRtcpSendTimeTicks = nextRegularTimeTicks;
        return snapshot;
    }

    private static RtpFeedbackRequest CreateRequest(
        RtcpFeedbackTimingMode timingMode,
        long earliestSendTimeTicks)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.PictureLossIndication = new RtcpPictureLossIndication();
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = feedback;
        request.EarliestSendTimeTicks = earliestSendTimeTicks;
        request.ExpirationTimeTicks = 30L * TimeSpan.TicksPerSecond;
        request.PreferredTimingMode = timingMode;
        return request;
    }

    private static RtpFeedbackRequest CreateGenericNackRequest(
        uint mediaSourceIdentifier,
        ushort packetIdentifier,
        ushort lostPacketBitmask,
        RtcpFeedbackTimingMode timingMode,
        long earliestSendTimeTicks)
    {
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = CreateGenericNackFeedback(
            mediaSourceIdentifier,
            packetIdentifier,
            lostPacketBitmask);
        request.EarliestSendTimeTicks = earliestSendTimeTicks;
        request.ExpirationTimeTicks = 30L * TimeSpan.TicksPerSecond;
        request.PreferredTimingMode = timingMode;
        return request;
    }

    private static RtcpFeedbackPacket CreateGenericNackFeedback(
        uint mediaSourceIdentifier,
        ushort packetIdentifier,
        ushort lostPacketBitmask)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = packetIdentifier;
        entry.LostPacketBitmask = lostPacketBitmask;
        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.GenericNack = genericNack;
        return feedback;
    }

    private class FixedFeedbackRandomSource : IRtpRandomSource
    {
        public double UnitIntervalValue;
        public int UnitIntervalCallCount;

        public uint CreateSynchronizationSource()
        {
            return 1U;
        }

        public ushort CreateSequenceNumber()
        {
            return 1;
        }

        public uint CreateTimestamp()
        {
            return 1U;
        }

        public double CreateUnitIntervalValue()
        {
            UnitIntervalCallCount = UnitIntervalCallCount + 1;
            return UnitIntervalValue;
        }
    }
}
