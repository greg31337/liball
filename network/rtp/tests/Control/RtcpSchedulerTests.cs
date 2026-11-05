using rtp;

namespace rtp.Tests.Control;

public class RtcpSchedulerTests
{
    [Test]
    public void PollReportsDueStateAndPerformsTimerReconsideration()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        RtpSessionSnapshot snapshot = CreateScheduleSnapshot(1, 0, 128);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, false, 0L, 0L)
        };
        long initialDueTime = scheduler.nextReportTimeTicks;

        RtcpScheduleDecision early = scheduler.PollRtcpSchedule(
            snapshot,
            initialDueTime - 1L);
        RtcpScheduleDecision initialReady = scheduler.PollRtcpSchedule(
            snapshot,
            initialDueTime);
        long secondInitialDueTime = scheduler.nextReportTimeTicks;
        RtcpScheduleDecision reconsidered = scheduler.PollRtcpSchedule(
            snapshot,
            secondInitialDueTime);
        long regularDueTime = scheduler.nextReportTimeTicks;
        RtcpScheduleDecision regularReady = scheduler.PollRtcpSchedule(
            snapshot,
            regularDueTime);

        Assert.Multiple(() =>
        {
            Assert.That(early.ShouldSend, Is.False);
            Assert.That(early.IsInitial, Is.True);
            Assert.That(early.ScheduledTimeTicks, Is.EqualTo(initialDueTime));
            Assert.That(early.DelayTicks, Is.EqualTo(1L));
            Assert.That(initialReady.ShouldSend, Is.True);
            Assert.That(initialReady.IsInitial, Is.True);
            Assert.That(initialReady.IsGoodbye, Is.False);
            Assert.That(initialReady.ScheduledTimeTicks,
                Is.EqualTo(initialDueTime));
            Assert.That(initialReady.DelayTicks, Is.Zero);
            Assert.That(initialReady.CompoundPacket, Is.Null);
            Assert.That(reconsidered.ShouldSend, Is.False);
            Assert.That(reconsidered.IsInitial, Is.False);
            Assert.That(reconsidered.ScheduledTimeTicks,
                Is.GreaterThan(secondInitialDueTime));
            Assert.That(regularReady.ShouldSend, Is.True);
            Assert.That(regularReady.IsInitial, Is.False);
            Assert.That(regularReady.MemberCount, Is.EqualTo(1));
            Assert.That(regularReady.SenderCount, Is.Zero);
            Assert.That(randomSource.UnitIntervalCallCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void PollIdentifiesAnActiveLocalSenderFromTheSnapshot()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        long dueTime = scheduler.nextReportTimeTicks;
        RtpSessionSnapshot snapshot = CreateScheduleSnapshot(1, 1, 128);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, true, dueTime, 0L)
        };

        RtcpScheduleDecision decision = scheduler.PollRtcpSchedule(
            snapshot,
            dueTime);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldSend, Is.True);
            Assert.That(decision.IsSender, Is.True);
            Assert.That(decision.MemberCount, Is.EqualTo(1));
            Assert.That(decision.SenderCount, Is.EqualTo(1));
            Assert.That(scheduler.localSenderActive, Is.True);
        });
    }

    [Test]
    public void InactivityTimeoutsExpireSendersAndRemoteMembers()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.SenderTimeoutTicks = 50L;
        configuration.MemberTimeoutTicks = 100L;
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(configuration, 0L);
        scheduler.ReconsiderRtcpSchedule(4, 2, 0L);
        long beforeNextTime = scheduler.nextReportTimeTicks;
        long beforePreviousTime = scheduler.previousReportTimeTicks;
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = 4;
        snapshot.SenderCount = 2;
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, true, 75L, 0L),
            CreateSource(false, true, 40L, 90L),
            CreateSource(false, false, 0L, 0L),
            CreateSource(false, false, 1L, 1L)
        };

        scheduler.ApplyInactivityTimeouts(snapshot, 100L);
        long expectedNextTime = (long)Math.Round(
            100.0 + (beforeNextTime - 100.0) * 0.75);
        long expectedPreviousTime = (long)Math.Round(
            100.0 + (beforePreviousTime - 100.0) * 0.75);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.memberCount, Is.EqualTo(3));
            Assert.That(scheduler.senderCount, Is.EqualTo(1));
            Assert.That(scheduler.previousMemberCount, Is.EqualTo(3));
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(expectedNextTime));
            Assert.That(scheduler.previousReportTimeTicks,
                Is.EqualTo(expectedPreviousTime));
        });
    }

    [Test]
    public void ZeroTimeoutConfigurationUsesRfcIntervalMultipliers()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        RtpSessionSnapshot snapshot = CreateScheduleSnapshot(2, 1, 128);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, false, 0L, 0L),
            CreateSource(false, true, 0L, 0L)
        };

        scheduler.ApplyInactivityTimeouts(
            snapshot,
            6L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.memberCount, Is.EqualTo(2));
            Assert.That(scheduler.senderCount, Is.Zero);
        });

        scheduler.ApplyInactivityTimeouts(
            snapshot,
            13L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.memberCount, Is.EqualTo(1));
            Assert.That(scheduler.senderCount, Is.Zero);
        });
    }

    [Test]
    public void SmallSessionGoodbyeIsImmediateAndLargeSessionUsesBackoff()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler smallScheduler = new RtcpScheduler(randomSource);
        smallScheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        RtpSessionSnapshot smallSnapshot = CreateScheduleSnapshot(50, 1, 128);
        smallSnapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, true, 0L, 0L)
        };

        RtcpScheduleDecision immediate = smallScheduler.ScheduleRtcpGoodbye(
            smallSnapshot,
            100L);

        RtcpScheduler largeScheduler = new RtcpScheduler(randomSource);
        largeScheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        RtpSessionSnapshot largeSnapshot = CreateScheduleSnapshot(51, 1, 128);
        RtcpScheduleDecision pending = largeScheduler.ScheduleRtcpGoodbye(
            largeSnapshot,
            100L);
        long firstBackoffTime = pending.ScheduledTimeTicks;
        largeScheduler.ReconsiderRtcpSchedule(2, 0, 100L);
        long reconsideredTime = largeScheduler.nextReportTimeTicks;
        RtcpScheduleDecision stillPending = largeScheduler.ScheduleRtcpGoodbye(
            largeSnapshot,
            firstBackoffTime);
        RtcpScheduleDecision ready = largeScheduler.ScheduleRtcpGoodbye(
            largeSnapshot,
            reconsideredTime);

        Assert.Multiple(() =>
        {
            Assert.That(immediate.ShouldSend, Is.True);
            Assert.That(immediate.IsGoodbye, Is.True);
            Assert.That(immediate.IsSender, Is.True);
            Assert.That(immediate.ScheduledTimeTicks, Is.EqualTo(100L));
            Assert.That(immediate.DelayTicks, Is.Zero);
            Assert.That(immediate.MemberCount, Is.EqualTo(50));
            Assert.That(pending.ShouldSend, Is.False);
            Assert.That(pending.IsGoodbye, Is.True);
            Assert.That(pending.IsInitial, Is.True);
            Assert.That(pending.MemberCount, Is.EqualTo(1));
            Assert.That(pending.SenderCount, Is.Zero);
            Assert.That(pending.DelayTicks, Is.GreaterThan(0L));
            Assert.That(pending.CompoundPacket, Is.Null);
            Assert.That(reconsideredTime,
                Is.EqualTo(100L + 2L * (firstBackoffTime - 100L)));
            Assert.That(stillPending.ShouldSend, Is.False);
            Assert.That(stillPending.ScheduledTimeTicks,
                Is.EqualTo(reconsideredTime));
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(ready.DelayTicks, Is.Zero);
            Assert.That(largeScheduler.goodbyePending, Is.False);
        });
    }

    [Test]
    public void SchedulerTracksPacketSizeEwmaAndResetRestoresInitialState()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);

        scheduler.InitializeRtcpSchedule(configuration, 100L);
        configuration.InitialAveragePacketSize = 999;
        scheduler.RecordRtcpPacketSize(288);
        scheduler.RecordRtcpPacketSize(106);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.averagePacketSize, Is.EqualTo(136.0));
            Assert.That(scheduler.configuration?.InitialAveragePacketSize,
                Is.EqualTo(128));
            Assert.That(scheduler.memberCount, Is.EqualTo(1));
            Assert.That(scheduler.senderCount, Is.Zero);
            Assert.That(scheduler.isInitial, Is.True);
        });

        scheduler.ResetRtcpScheduler(500L);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.averagePacketSize, Is.EqualTo(128.0));
            Assert.That(scheduler.previousReportTimeTicks, Is.EqualTo(500L));
            Assert.That(scheduler.nextReportTimeTicks, Is.GreaterThan(500L));
            Assert.That(scheduler.previousMemberCount, Is.EqualTo(1));
            Assert.That(scheduler.isInitial, Is.True);
        });
    }

    [Test]
    public void MembershipGrowthAndShrinkReconsiderBothTimerAnchors()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 100L);
        long initialNextTime = scheduler.nextReportTimeTicks;

        scheduler.ReconsiderRtcpSchedule(4, 1, 200L);
        long expectedGrowthNext = 200L + 4L * (initialNextTime - 200L);
        long expectedGrowthPrevious = 200L + 4L * (100L - 200L);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.memberCount, Is.EqualTo(4));
            Assert.That(scheduler.senderCount, Is.EqualTo(1));
            Assert.That(scheduler.previousMemberCount, Is.EqualTo(4));
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(expectedGrowthNext));
            Assert.That(scheduler.previousReportTimeTicks,
                Is.EqualTo(expectedGrowthPrevious));
        });

        scheduler.ReconsiderRtcpSchedule(2, 0, 300L);
        long expectedShrinkNext = (long)Math.Round(
            300.0 + (expectedGrowthNext - 300.0) * 0.5);
        long expectedShrinkPrevious = (long)Math.Round(
            300.0 + (expectedGrowthPrevious - 300.0) * 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.memberCount, Is.EqualTo(2));
            Assert.That(scheduler.senderCount, Is.Zero);
            Assert.That(scheduler.previousMemberCount, Is.EqualTo(2));
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(expectedShrinkNext));
            Assert.That(scheduler.previousReportTimeTicks,
                Is.EqualTo(expectedShrinkPrevious));
        });

        scheduler.ReconsiderRtcpSchedule(2, 1, 400L);
        Assert.Multiple(() =>
        {
            Assert.That(scheduler.senderCount, Is.EqualTo(1));
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(expectedShrinkNext));
            Assert.That(scheduler.previousReportTimeTicks,
                Is.EqualTo(expectedShrinkPrevious));
        });
    }

    [Test]
    public void SchedulerStateChangesRejectInvalidInputWithoutPartialMutation()
    {
        FixedRandomSource randomSource = new FixedRandomSource();
        randomSource.UnitIntervalValue = 0.5;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);

        Assert.Throws<RtpConfigurationException>(() =>
            new RtcpScheduler(null!));
        Assert.Throws<RtpSessionException>(() =>
            scheduler.RecordRtcpPacketSize(100));
        Assert.Throws<RtpConfigurationException>(() =>
            scheduler.InitializeRtcpSchedule(CreateConfiguration(), -1L));

        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 100L);
        long scheduledTime = scheduler.nextReportTimeTicks;
        RtpSessionSnapshot futureActivity =
            CreateScheduleSnapshot(1, 0, 128);
        futureActivity.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, false, 101L, 0L)
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.RecordRtcpPacketSize(0));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ReconsiderRtcpSchedule(0, 0, 200L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ReconsiderRtcpSchedule(2, 3, 200L));
            Assert.Throws<RtpSessionException>(() =>
                scheduler.ReconsiderRtcpSchedule(2, 0, 99L));
            Assert.Throws<RtpSessionException>(() =>
                scheduler.ApplyInactivityTimeouts(futureActivity, 100L));
            Assert.That(scheduler.memberCount, Is.EqualTo(1));
            Assert.That(scheduler.senderCount, Is.Zero);
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(scheduledTime));
            Assert.That(scheduler.averagePacketSize, Is.EqualTo(128.0));
        });
    }

    [Test]
    public void SchedulerValidationAndArithmeticHelpersCoverEveryRepresentableEdge()
    {
        RtcpScheduler scheduler = new RtcpScheduler(new FixedRandomSource());
        RtpSessionSnapshot noSources = CreateScheduleSnapshot(1, 0, 128);
        noSources.Sources = null!;
        RtpSessionSnapshot nullSource = CreateScheduleSnapshot(1, 0, 128);
        nullSource.Sources = new RtpSourceSnapshot[] { null! };
        RtpSourceSnapshot negativeRtp = CreateSource(false, false, -1L, 0L);
        RtpSourceSnapshot negativeRtcp = CreateSource(false, false, 0L, -1L);
        RtpSourceSnapshot futureRtp = CreateSource(false, false, 2L, 0L);
        RtpSourceSnapshot futureRtcp = CreateSource(false, false, 0L, 2L);
        RtpSourceSnapshot active = CreateSource(true, true, 0L, 0L);
        RtpSessionSnapshot localSender = CreateScheduleSnapshot(1, 1, 128);
        localSender.Sources = new RtpSourceSnapshot[] { active };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateScheduleSnapshot(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateScheduleSnapshot(noSources));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateScheduleSnapshot(nullSource));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateMemberAndSenderCounts(1, -1));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateMemberAndSenderCounts(1, 2));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateNonnegativeTime(-1L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateSourceActivity(negativeRtp, 1L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ValidateSourceActivity(negativeRtcp, 1L));
            Assert.Throws<RtpSessionException>(() =>
                scheduler.ValidateSourceActivity(futureRtp, 1L));
            Assert.Throws<RtpSessionException>(() =>
                scheduler.ValidateSourceActivity(futureRtcp, 1L));
            Assert.That(scheduler.MultiplyTimeoutInterval(long.MaxValue, 2),
                Is.EqualTo(long.MaxValue));
            Assert.That(scheduler.MultiplyTimeoutInterval(10L, 2), Is.EqualTo(20L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ConvertIntervalToTicks(double.MaxValue));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.AddIntervalToTime(long.MaxValue, 1L));
            Assert.Throws<RtpConfigurationException>(() =>
                scheduler.ScaleTimeAroundCurrent(long.MaxValue, 0L, double.MaxValue));
            Assert.That(scheduler.HasLocalSender(localSender), Is.True);
            Assert.That(scheduler.IsActiveMember(active), Is.True);
            Assert.That(scheduler.IsActiveSender(active), Is.True);
        });
    }

    [Test]
    public void SenderAndReceiverBandwidthSharesApplyBelowTheSenderThreshold()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();

        double senderInterval = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            10,
            true,
            100,
            false,
            0.5);
        double receiverInterval = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            10,
            false,
            100,
            false,
            0.5);

        Assert.Multiple(() =>
        {
            Assert.That(senderInterval,
                Is.EqualTo(8.0 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
            Assert.That(receiverInterval,
                Is.EqualTo(24.0 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
        });
    }

    [Test]
    public void ExcessSenderPopulationUsesTheUndividedRtcpBandwidth()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();

        double senderInterval = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            26,
            true,
            100,
            false,
            0.5);
        double receiverInterval = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            26,
            false,
            100,
            false,
            0.5);

        double expected = 20.0 / RtcpProtocolConstants.IntervalCompensation;
        Assert.Multiple(() =>
        {
            Assert.That(senderInterval, Is.EqualTo(expected).Within(0.000000001));
            Assert.That(receiverInterval, Is.EqualTo(expected).Within(0.000000001));
        });
    }

    [Test]
    public void InitialMinimumRandomizationAndCompensationFollowRfcOrder()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();

        double regularLow = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            1,
            0,
            false,
            100,
            false,
            0.0);
        double regularHigh = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            1,
            0,
            false,
            100,
            false,
            1.0);
        double initialMiddle = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            1,
            0,
            false,
            100,
            true,
            0.5);

        Assert.Multiple(() =>
        {
            Assert.That(regularLow,
                Is.EqualTo(2.5 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
            Assert.That(regularHigh,
                Is.EqualTo(7.5 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
            Assert.That(initialMiddle,
                Is.EqualTo(2.5 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
        });
    }

    [Test]
    public void IntervalCalculationRejectsInconsistentOrUnusableInputs()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 0, 0, false, 100, false, 0.5));
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 2, 3, false, 100, false, 0.5));
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 2, 0, true, 100, false, 0.5));
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 2, 0, false, 0, false, 0.5));
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 2, 0, false, 100, false, -0.1));
            Assert.Throws<RtpConfigurationException>(() =>
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    configuration, 2, 0, false, 100, false, double.NaN));
        });
    }

    private static RtcpSchedulerConfiguration CreateConfiguration()
    {
        RtcpSchedulerConfiguration configuration =
            new RtcpSchedulerConfiguration();
        configuration.SessionBandwidthBitsPerSecond = 80000.0;
        return configuration;
    }

    private static RtpSessionSnapshot CreateScheduleSnapshot(
        int memberCount,
        int senderCount,
        int averagePacketSize)
    {
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = memberCount;
        snapshot.SenderCount = senderCount;
        snapshot.AverageRtcpPacketSize = averagePacketSize;
        return snapshot;
    }

    private static RtpSourceSnapshot CreateSource(
        bool isLocal,
        bool isSender,
        long lastRtpActivityTicks,
        long lastRtcpActivityTicks)
    {
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        source.IsLocal = isLocal;
        source.IsSender = isSender;
        if (isSender)
        {
            source.Status = RtpSourceStatus.ActiveSender;
        }
        else
        {
            source.Status = RtpSourceStatus.ActiveMember;
        }

        source.LastRtpActivityTicks = lastRtpActivityTicks;
        source.LastRtcpActivityTicks = lastRtcpActivityTicks;
        return source;
    }

    private class FixedRandomSource : IRtpRandomSource
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
