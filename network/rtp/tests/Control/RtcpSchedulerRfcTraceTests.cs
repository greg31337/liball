using rtp;

namespace rtp.Tests.Control;

public class RtcpSchedulerRfcTraceTests
{
    [Test]
    public void BandwidthMatrixCoversSenderThresholdAndRandomBounds()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();

        double belowSender =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                24,
                true,
                100,
                false);
        double belowReceiver =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                24,
                false,
                100,
                false);
        double thresholdSender =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                25,
                true,
                100,
                false);
        double thresholdReceiver =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                25,
                false,
                100,
                false);
        double aboveSender =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                26,
                true,
                100,
                false);
        double aboveReceiver =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                100,
                26,
                false,
                100,
                false);
        double randomLow = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            25,
            true,
            100,
            false,
            0.0);
        double randomMiddle = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            25,
            true,
            100,
            false,
            0.5);
        double randomHigh = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configuration,
            100,
            25,
            true,
            100,
            false,
            1.0);

        Assert.Multiple(() =>
        {
            Assert.That(belowSender, Is.EqualTo(19.2).Within(0.000000001));
            Assert.That(belowReceiver,
                Is.EqualTo(20.266666666666666).Within(0.000000001));
            Assert.That(thresholdSender, Is.EqualTo(20.0).Within(0.000000001));
            Assert.That(thresholdReceiver,
                Is.EqualTo(20.0).Within(0.000000001));
            Assert.That(aboveSender, Is.EqualTo(20.0).Within(0.000000001));
            Assert.That(aboveReceiver, Is.EqualTo(20.0).Within(0.000000001));
            Assert.That(randomLow,
                Is.EqualTo(10.0 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
            Assert.That(randomMiddle,
                Is.EqualTo(20.0 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
            Assert.That(randomHigh,
                Is.EqualTo(30.0 / RtcpProtocolConstants.IntervalCompensation)
                    .Within(0.000000001));
        });
    }

    [Test]
    public void QueuedRandomTraceTransitionsFromInitialToRegularIntervals()
    {
        SequenceRandomSource randomSource = new SequenceRandomSource(
            new double[] { 0.0, 0.0, 1.0, 1.0, 1.0, 0.5 });
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        long startTime = 1000L;
        scheduler.InitializeRtcpSchedule(configuration, startTime);
        RtpSessionSnapshot snapshot = CreateSnapshot(1, 0);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, false, 0L, 0L)
        };
        long expectedInitialLow = ExpectedIntervalTicks(1.25);
        long initialDueTime = startTime + expectedInitialLow;

        RtcpScheduleDecision initial = scheduler.PollRtcpSchedule(
            snapshot,
            initialDueTime);
        long expectedInitialHigh = ExpectedIntervalTicks(3.75);
        long transitionDueTime = initialDueTime + expectedInitialHigh;
        RtcpScheduleDecision transition = scheduler.PollRtcpSchedule(
            snapshot,
            transitionDueTime);
        long expectedRegularHigh = ExpectedIntervalTicks(7.5);
        long regularDueTime = initialDueTime + expectedRegularHigh;
        RtcpScheduleDecision regular = scheduler.PollRtcpSchedule(
            snapshot,
            regularDueTime);
        long expectedRegularMiddle = ExpectedIntervalTicks(5.0);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.isInitial, Is.False);
            Assert.That(initial.ShouldSend, Is.True);
            Assert.That(initial.IsInitial, Is.True);
            Assert.That(initial.ScheduledTimeTicks, Is.EqualTo(initialDueTime));
            Assert.That(transition.ShouldSend, Is.False);
            Assert.That(transition.IsInitial, Is.False);
            Assert.That(transition.ScheduledTimeTicks,
                Is.EqualTo(regularDueTime));
            Assert.That(regular.ShouldSend, Is.True);
            Assert.That(regular.IsInitial, Is.False);
            Assert.That(regular.ScheduledTimeTicks, Is.EqualTo(regularDueTime));
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(regularDueTime + expectedRegularMiddle));
            Assert.That(randomSource.CallCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void AveragePacketSizeEwmaChangesTheReconsideredInterval()
    {
        SequenceRandomSource randomSource = new SequenceRandomSource(
            new double[] { 0.5 });
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.MinimumIntervalSeconds = 0.001;
        configuration.InitialMinimumIntervalSeconds = 0.001;
        configuration.InitialAveragePacketSize = 160;
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(configuration, 0L);
        scheduler.RecordRtcpPacketSize(320);
        scheduler.ReconsiderRtcpSchedule(10, 0, 0L);
        RtpSessionSnapshot snapshot = CreateSnapshot(10, 0);
        long priorDueTime = scheduler.nextReportTimeTicks;

        RtcpScheduleDecision reconsidered = scheduler.PollRtcpSchedule(
            snapshot,
            priorDueTime);
        long expectedInterval = ExpectedIntervalTicks(
            4.533333333333333);
        RtcpScheduleDecision ready = scheduler.PollRtcpSchedule(
            snapshot,
            expectedInterval);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.averagePacketSize, Is.EqualTo(170.0));
            Assert.That(reconsidered.ShouldSend, Is.False);
            Assert.That(reconsidered.ScheduledTimeTicks,
                Is.EqualTo(expectedInterval));
            Assert.That(expectedInterval, Is.GreaterThan(priorDueTime));
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(ready.IsInitial, Is.True);
            Assert.That(scheduler.nextReportTimeTicks,
                Is.EqualTo(expectedInterval + expectedInterval));
        });
    }

    [Test]
    public void MembershipGrowthAndShrinkScaleBothTimerAnchors()
    {
        SequenceRandomSource randomSource = new SequenceRandomSource(
            new double[] { 0.5 });
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(CreateConfiguration(), 100L);
        long initialNext = scheduler.nextReportTimeTicks;

        scheduler.ReconsiderRtcpSchedule(8, 2, 200L);
        long growthNext = (long)Math.Round(
            200.0 + (initialNext - 200.0) * 8.0);
        long growthPrevious = (long)Math.Round(
            200.0 + (100.0 - 200.0) * 8.0);
        scheduler.ReconsiderRtcpSchedule(2, 1, 300L);
        long shrinkNext = (long)Math.Round(
            300.0 + (growthNext - 300.0) * 0.25);
        long shrinkPrevious = (long)Math.Round(
            300.0 + (growthPrevious - 300.0) * 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(growthNext, Is.GreaterThan(initialNext));
            Assert.That(scheduler.memberCount, Is.EqualTo(2));
            Assert.That(scheduler.senderCount, Is.EqualTo(1));
            Assert.That(scheduler.previousMemberCount, Is.EqualTo(2));
            Assert.That(scheduler.nextReportTimeTicks, Is.EqualTo(shrinkNext));
            Assert.That(scheduler.previousReportTimeTicks,
                Is.EqualTo(shrinkPrevious));
            Assert.That(shrinkNext, Is.LessThan(growthNext));
        });
    }

    [Test]
    public void SenderAndMemberTimeoutsUseInclusiveConfiguredBoundaries()
    {
        RtcpSchedulerConfiguration configuration = CreateConfiguration();
        configuration.SenderTimeoutTicks = 10L;
        configuration.MemberTimeoutTicks = 25L;
        SequenceRandomSource randomSource = new SequenceRandomSource(
            new double[] { 0.5 });
        RtcpScheduler scheduler = new RtcpScheduler(randomSource);
        scheduler.InitializeRtcpSchedule(configuration, 0L);
        RtpSessionSnapshot snapshot = CreateSnapshot(3, 2);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, true, 95L, 95L),
            CreateSource(false, true, 90L, 90L),
            CreateSource(false, false, 81L, 80L)
        };

        scheduler.ApplyInactivityTimeouts(snapshot, 99L);
        int earlyMembers = scheduler.memberCount;
        int earlySenders = scheduler.senderCount;
        scheduler.ApplyInactivityTimeouts(snapshot, 100L);
        int remoteSenderExpired = scheduler.senderCount;
        scheduler.ApplyInactivityTimeouts(snapshot, 105L);
        int allSendersExpired = scheduler.senderCount;
        int membersBeforeBoundary = scheduler.memberCount;
        scheduler.ApplyInactivityTimeouts(snapshot, 106L);

        Assert.Multiple(() =>
        {
            Assert.That(earlyMembers, Is.EqualTo(3));
            Assert.That(earlySenders, Is.EqualTo(2));
            Assert.That(remoteSenderExpired, Is.EqualTo(1));
            Assert.That(allSendersExpired, Is.Zero);
            Assert.That(membersBeforeBoundary, Is.EqualTo(3));
            Assert.That(scheduler.memberCount, Is.EqualTo(2));
            Assert.That(scheduler.senderCount, Is.Zero);
            Assert.That(scheduler.localSenderActive, Is.False);
        });
    }

    [Test]
    public void GoodbyeUsesImmediateBoundaryAndReconsideredLargeSessionBackoff()
    {
        RtpSessionSnapshot immediateSnapshot = CreateSnapshot(50, 1);
        immediateSnapshot.AverageRtcpPacketSize = 256;
        immediateSnapshot.Sources = new RtpSourceSnapshot[]
        {
            CreateSource(true, true, 0L, 0L)
        };
        RtcpScheduler immediateScheduler = new RtcpScheduler(
            new SequenceRandomSource(new double[] { 0.5 }));
        immediateScheduler.InitializeRtcpSchedule(
            CreateConfiguration(),
            0L);
        RtcpScheduleDecision immediate = immediateScheduler
            .ScheduleRtcpGoodbye(immediateSnapshot, 100L);

        RtpSessionSnapshot backoffSnapshot = CreateSnapshot(51, 1);
        backoffSnapshot.AverageRtcpPacketSize = 256;
        backoffSnapshot.Sources = immediateSnapshot.Sources;
        RtcpScheduler backoffScheduler = new RtcpScheduler(
            new SequenceRandomSource(new double[] { 0.5, 0.0 }));
        backoffScheduler.InitializeRtcpSchedule(CreateConfiguration(), 0L);
        RtcpScheduleDecision pending = backoffScheduler.ScheduleRtcpGoodbye(
            backoffSnapshot,
            100L);
        long originalDueTime = pending.ScheduledTimeTicks;
        backoffScheduler.ReconsiderRtcpSchedule(4, 0, 101L);
        long grownDueTime = (long)Math.Round(
            101.0 + (originalDueTime - 101.0) * 4.0);
        backoffScheduler.ReconsiderRtcpSchedule(1, 0, 102L);
        long shrunkDueTime = (long)Math.Round(
            102.0 + (grownDueTime - 102.0) * 0.25);
        RtcpScheduleDecision justBefore = backoffScheduler
            .ScheduleRtcpGoodbye(backoffSnapshot, shrunkDueTime - 1L);
        RtcpScheduleDecision ready = backoffScheduler
            .ScheduleRtcpGoodbye(backoffSnapshot, shrunkDueTime);

        Assert.Multiple(() =>
        {
            Assert.That(immediate.ShouldSend, Is.True);
            Assert.That(immediate.IsGoodbye, Is.True);
            Assert.That(immediate.IsSender, Is.True);
            Assert.That(immediate.ScheduledTimeTicks, Is.EqualTo(100L));
            Assert.That(pending.ShouldSend, Is.False);
            Assert.That(pending.IsGoodbye, Is.True);
            Assert.That(pending.MemberCount, Is.EqualTo(1));
            Assert.That(grownDueTime, Is.GreaterThan(originalDueTime));
            Assert.That(shrunkDueTime, Is.LessThan(grownDueTime));
            Assert.That(justBefore.ShouldSend, Is.False);
            Assert.That(justBefore.DelayTicks, Is.EqualTo(1L));
            Assert.That(ready.ShouldSend, Is.True);
            Assert.That(ready.DelayTicks, Is.Zero);
            Assert.That(backoffScheduler.goodbyePending, Is.False);
        });
    }

    static long ExpectedIntervalTicks(double uncompensatedSeconds)
    {
        return (long)Math.Ceiling(
            uncompensatedSeconds /
            RtcpProtocolConstants.IntervalCompensation *
            TimeSpan.TicksPerSecond);
    }

    static RtcpSchedulerConfiguration CreateConfiguration()
    {
        RtcpSchedulerConfiguration configuration =
            new RtcpSchedulerConfiguration();
        configuration.SessionBandwidthBitsPerSecond = 80000.0;
        return configuration;
    }

    static RtpSessionSnapshot CreateSnapshot(int memberCount, int senderCount)
    {
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = memberCount;
        snapshot.SenderCount = senderCount;
        snapshot.AverageRtcpPacketSize = 128;
        return snapshot;
    }

    static RtpSourceSnapshot CreateSource(
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

    private class SequenceRandomSource : IRtpRandomSource
    {
        internal double[] values;
        internal int index;
        public int CallCount;

        public SequenceRandomSource(double[] values)
        {
            this.values = values;
        }

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
            int selectedIndex = index;
            if (selectedIndex >= values.Length)
            {
                selectedIndex = values.Length - 1;
            }

            index = index + 1;
            CallCount = CallCount + 1;
            return values[selectedIndex];
        }
    }
}
