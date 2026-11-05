using rtp;

namespace rtp.Tests.Tracking;

public class RtpSequenceTrackerTests
{
    [Test]
    public void DefaultProbationAcceptsTheSecondSequentialPacket()
    {
        IRtpSequenceTracker tracker = CreateTracker(2);

        RtpSequenceUpdate initial = tracker.GetRtpSequenceState();
        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(100);
        RtpSequenceUpdate second = tracker.ProcessRtpSequenceNumber(101);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(initial.ProbationRemaining, Is.EqualTo(2));
            Assert.That(initial.Accepted, Is.False);
            Assert.That(first.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(first.SequenceNumber, Is.EqualTo(100));
            Assert.That(first.ProbationRemaining, Is.EqualTo(1));
            Assert.That(first.Accepted, Is.False);
            Assert.That(second.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(second.SequenceNumber, Is.EqualTo(101));
            Assert.That(second.BaseExtendedSequenceNumber, Is.EqualTo(101U));
            Assert.That(second.HighestExtendedSequenceNumber, Is.EqualTo(101U));
            Assert.That(second.ProbationRemaining, Is.Zero);
            Assert.That(second.Accepted, Is.True);
        });
    }

    [Test]
    public void GapRestartsTheConfiguredSequentialRunFromTheNewestPacket()
    {
        IRtpSequenceTracker tracker = CreateTracker(3);

        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(10);
        RtpSequenceUpdate second = tracker.ProcessRtpSequenceNumber(11);
        RtpSequenceUpdate gap = tracker.ProcessRtpSequenceNumber(13);
        RtpSequenceUpdate afterGap = tracker.ProcessRtpSequenceNumber(14);
        RtpSequenceUpdate accepted = tracker.ProcessRtpSequenceNumber(15);

        Assert.Multiple(() =>
        {
            Assert.That(first.ProbationRemaining, Is.EqualTo(2));
            Assert.That(second.ProbationRemaining, Is.EqualTo(1));
            Assert.That(gap.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(gap.ProbationRemaining, Is.EqualTo(2));
            Assert.That(afterGap.ProbationRemaining, Is.EqualTo(1));
            Assert.That(accepted.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(accepted.BaseExtendedSequenceNumber, Is.EqualTo(15U));
            Assert.That(accepted.Accepted, Is.True);
        });
    }

    [Test]
    public void ConsecutiveProbationRecognizesSixteenBitWraparound()
    {
        IRtpSequenceTracker tracker = CreateTracker(2);

        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(ushort.MaxValue);
        RtpSequenceUpdate wrapped = tracker.ProcessRtpSequenceNumber(0);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(first.ProbationRemaining, Is.EqualTo(1));
            Assert.That(wrapped.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(wrapped.SequenceNumber, Is.Zero);
            Assert.That(wrapped.ProbationRemaining, Is.Zero);
            Assert.That(wrapped.Accepted, Is.True);
        });
    }

    [Test]
    public void AcceptedPacketsUseExtendedSequenceNumbersAcrossWraparound()
    {
        IRtpSequenceTracker tracker = CreateTracker(1);

        RtpSequenceUpdate baseline = tracker.ProcessRtpSequenceNumber(65533);
        RtpSequenceUpdate beforeWrap = tracker.ProcessRtpSequenceNumber(65535);
        RtpSequenceUpdate late = tracker.ProcessRtpSequenceNumber(65534);
        RtpSequenceUpdate wrapped = tracker.ProcessRtpSequenceNumber(0);
        RtpSequenceUpdate previousCycleDuplicate =
            tracker.ProcessRtpSequenceNumber(65535);
        RtpSequenceUpdate afterWrap = tracker.ProcessRtpSequenceNumber(1);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.BaseExtendedSequenceNumber, Is.EqualTo(65533U));
            Assert.That(beforeWrap.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(beforeWrap.ExtendedSequenceNumber, Is.EqualTo(65535U));
            Assert.That(late.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(late.ExtendedSequenceNumber, Is.EqualTo(65534U));
            Assert.That(late.Accepted, Is.True);
            Assert.That(wrapped.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(wrapped.ExtendedSequenceNumber, Is.EqualTo(65536U));
            Assert.That(wrapped.HighestExtendedSequenceNumber, Is.EqualTo(65536U));
            Assert.That(previousCycleDuplicate.Status,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(previousCycleDuplicate.ExtendedSequenceNumber, Is.EqualTo(65535U));
            Assert.That(previousCycleDuplicate.Accepted, Is.False);
            Assert.That(afterWrap.ExtendedSequenceNumber, Is.EqualTo(65537U));
            Assert.That(afterWrap.HighestExtendedSequenceNumber, Is.EqualTo(65537U));
        });
    }

    [Test]
    public void ReorderedPacketsAreAcceptedOnceAndThenClassifiedAsDuplicates()
    {
        IRtpSequenceTracker tracker = CreateTracker(1);
        tracker.ProcessRtpSequenceNumber(100);
        RtpSequenceUpdate forwardGap = tracker.ProcessRtpSequenceNumber(103);

        RtpSequenceUpdate reordered = tracker.ProcessRtpSequenceNumber(102);
        RtpSequenceUpdate duplicateReordered = tracker.ProcessRtpSequenceNumber(102);
        RtpSequenceUpdate duplicateHighest = tracker.ProcessRtpSequenceNumber(103);
        RtpSequenceUpdate state = tracker.GetRtpSequenceState();

        Assert.Multiple(() =>
        {
            Assert.That(forwardGap.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(forwardGap.HighestExtendedSequenceNumber, Is.EqualTo(103U));
            Assert.That(reordered.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(reordered.ExtendedSequenceNumber, Is.EqualTo(102U));
            Assert.That(reordered.HighestExtendedSequenceNumber, Is.EqualTo(103U));
            Assert.That(reordered.Accepted, Is.True);
            Assert.That(duplicateReordered.Status, Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(duplicateReordered.Accepted, Is.False);
            Assert.That(duplicateHighest.Status, Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(duplicateHighest.Accepted, Is.False);
            Assert.That(state.HighestExtendedSequenceNumber, Is.EqualTo(103U));
        });
    }

    [Test]
    public void ConfiguredThresholdBoundariesAndJumpConfirmationFollowRfcRules()
    {
        IRtpSequenceTracker tracker = CreateThresholdTracker(1, 3, 2);
        tracker.ProcessRtpSequenceNumber(100);

        RtpSequenceUpdate lastAllowedForward = tracker.ProcessRtpSequenceNumber(102);
        RtpSequenceUpdate jump = tracker.ProcessRtpSequenceNumber(105);
        RtpSequenceUpdate restarted = tracker.ProcessRtpSequenceNumber(106);
        RtpSequenceUpdate forwardAfterRestart = tracker.ProcessRtpSequenceNumber(108);
        RtpSequenceUpdate lastAllowedReorder = tracker.ProcessRtpSequenceNumber(107);
        RtpSequenceUpdate reorderBoundary = tracker.ProcessRtpSequenceNumber(106);

        Assert.Multiple(() =>
        {
            Assert.That(lastAllowedForward.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(lastAllowedForward.HighestExtendedSequenceNumber, Is.EqualTo(102U));
            Assert.That(jump.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(jump.Accepted, Is.False);
            Assert.That(jump.HighestExtendedSequenceNumber, Is.EqualTo(102U));
            Assert.That(restarted.Status, Is.EqualTo(RtpSequenceStatus.Restarted));
            Assert.That(restarted.Accepted, Is.True);
            Assert.That(restarted.BaseExtendedSequenceNumber, Is.EqualTo(106U));
            Assert.That(restarted.HighestExtendedSequenceNumber, Is.EqualTo(106U));
            Assert.That(forwardAfterRestart.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(lastAllowedReorder.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(lastAllowedReorder.Accepted, Is.True);
            Assert.That(reorderBoundary.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(reorderBoundary.Accepted, Is.False);
            Assert.That(reorderBoundary.HighestExtendedSequenceNumber, Is.EqualTo(108U));
        });
    }

    [Test]
    public void UnconfirmedLargeJumpsDoNotMutateValidatedSequenceState()
    {
        IRtpSequenceTracker tracker = CreateThresholdTracker(1, 10, 4);
        tracker.ProcessRtpSequenceNumber(400);
        tracker.ProcessRtpSequenceNumber(405);

        RtpSequenceUpdate firstJump = tracker.ProcessRtpSequenceNumber(20000);
        RtpSequenceUpdate secondJump = tracker.ProcessRtpSequenceNumber(30000);
        RtpSequenceUpdate resumed = tracker.ProcessRtpSequenceNumber(406);

        Assert.Multiple(() =>
        {
            Assert.That(firstJump.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(firstJump.BaseExtendedSequenceNumber, Is.EqualTo(400U));
            Assert.That(firstJump.HighestExtendedSequenceNumber, Is.EqualTo(405U));
            Assert.That(secondJump.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(secondJump.HighestExtendedSequenceNumber, Is.EqualTo(405U));
            Assert.That(resumed.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(resumed.ExtendedSequenceNumber, Is.EqualTo(406U));
            Assert.That(resumed.HighestExtendedSequenceNumber, Is.EqualTo(406U));
        });
    }

    [Test]
    public void OnePacketConfigurationAcceptsTheFirstPacket()
    {
        IRtpSequenceTracker tracker = CreateTracker(1);

        RtpSequenceUpdate update = tracker.ProcessRtpSequenceNumber(77);

        Assert.Multiple(() =>
        {
            Assert.That(update.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(update.SequenceNumber, Is.EqualTo(77));
            Assert.That(update.BaseExtendedSequenceNumber, Is.EqualTo(77U));
            Assert.That(update.ProbationRemaining, Is.Zero);
            Assert.That(update.Accepted, Is.True);
        });
    }

    [Test]
    public void ResetStartsFreshProbationAndDefinesOutboundSequence()
    {
        IRtpSequenceTracker tracker = CreateTracker(2);
        tracker.ProcessRtpSequenceNumber(10);
        tracker.ProcessRtpSequenceNumber(11);

        RtpSequenceUpdate reset = tracker.ResetRtpSequenceTracker(200);
        RtpSequenceUpdate accepted = tracker.ProcessRtpSequenceNumber(201);
        ushort firstOutbound = tracker.AdvanceRtpSequenceNumber();
        ushort secondOutbound = tracker.AdvanceRtpSequenceNumber();

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(reset.SequenceNumber, Is.EqualTo(200));
            Assert.That(reset.ProbationRemaining, Is.EqualTo(1));
            Assert.That(reset.Accepted, Is.False);
            Assert.That(accepted.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(accepted.BaseExtendedSequenceNumber, Is.EqualTo(201U));
            Assert.That(firstOutbound, Is.EqualTo(200));
            Assert.That(secondOutbound, Is.EqualTo(201));
        });
    }

    [Test]
    public void ConfigurationIsValidatedAndCopiedAtConstruction()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = 2;
        configuration.JitterBuffer.MaximumDropout = 3;
        configuration.JitterBuffer.MaximumMisorder = 2;
        IRtpSequenceTracker tracker = new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource(0));
        configuration.MinimumSequential = 4;
        configuration.JitterBuffer.MaximumDropout = 3000;
        configuration.JitterBuffer.MaximumMisorder = 100;
        RtpSessionConfiguration zero = new RtpSessionConfiguration();
        zero.MinimumSequential = 0;
        RtpSessionConfiguration modulus = new RtpSessionConfiguration();
        modulus.MinimumSequential = RtpProtocolConstants.SequenceNumberModulus;
        RtpSessionConfiguration noThresholds = new RtpSessionConfiguration();
        noThresholds.JitterBuffer = null!;
        RtpSessionConfiguration noDropout = new RtpSessionConfiguration();
        noDropout.JitterBuffer.MaximumDropout = 0;
        RtpSessionConfiguration negativeMisorder = new RtpSessionConfiguration();
        negativeMisorder.JitterBuffer.MaximumMisorder = -1;
        RtpSessionConfiguration noRestartRange = new RtpSessionConfiguration();
        noRestartRange.JitterBuffer.MaximumDropout = 65500;
        noRestartRange.JitterBuffer.MaximumMisorder = 36;

        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(300);
        RtpSequenceUpdate accepted = tracker.ProcessRtpSequenceNumber(301);
        RtpSequenceUpdate copiedThresholdJump = tracker.ProcessRtpSequenceNumber(304);

        Assert.Multiple(() =>
        {
            Assert.That(first.ProbationRemaining, Is.EqualTo(1));
            Assert.That(accepted.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(copiedThresholdJump.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.Throws<RtpConfigurationException>(() => new RtpSequenceTracker(
                zero,
                new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() => new RtpSequenceTracker(
                modulus,
                new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpSequenceTracker(
                    noThresholds,
                    new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpSequenceTracker(
                    noDropout,
                    new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpSequenceTracker(
                    negativeMisorder,
                    new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpSequenceTracker(
                    noRestartRange,
                    new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() => new RtpSequenceTracker(
                null!,
                new FixedSequenceRandomSource(0)));
            Assert.Throws<RtpConfigurationException>(() => new RtpSequenceTracker(
                new RtpSessionConfiguration(),
                null!));
        });
    }

    [Test]
    public void OutboundSequenceUsesTheInjectedRandomStartAndWraps()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        FixedSequenceRandomSource randomSource =
            new FixedSequenceRandomSource(ushort.MaxValue);
        IRtpSequenceTracker tracker = new RtpSequenceTracker(configuration, randomSource);

        ushort first = tracker.AdvanceRtpSequenceNumber();
        ushort wrapped = tracker.AdvanceRtpSequenceNumber();
        ushort afterWrap = tracker.AdvanceRtpSequenceNumber();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(ushort.MaxValue));
            Assert.That(wrapped, Is.Zero);
            Assert.That(afterWrap, Is.EqualTo(1));
            Assert.That(randomSource.sequenceRequestCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitResetRestartsOutboundSequenceWithoutRequestingNewRandomness()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        FixedSequenceRandomSource randomSource = new FixedSequenceRandomSource(700);
        IRtpSequenceTracker tracker = new RtpSequenceTracker(configuration, randomSource);
        tracker.AdvanceRtpSequenceNumber();
        tracker.AdvanceRtpSequenceNumber();

        tracker.ResetRtpSequenceTracker(900);
        ushort restarted = tracker.AdvanceRtpSequenceNumber();
        ushort advanced = tracker.AdvanceRtpSequenceNumber();

        Assert.Multiple(() =>
        {
            Assert.That(restarted, Is.EqualTo(900));
            Assert.That(advanced, Is.EqualTo(901));
            Assert.That(randomSource.sequenceRequestCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void TrackerInstancesKeepLocalSenderAndReceiveStateIndependent()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        IRtpSequenceTracker first = new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource(1000));
        IRtpSequenceTracker second = new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource(5000));
        first.ProcessRtpSequenceNumber(40);
        first.ProcessRtpSequenceNumber(41);

        ushort firstInitial = first.AdvanceRtpSequenceNumber();
        ushort secondInitial = second.AdvanceRtpSequenceNumber();
        ushort secondNext = second.AdvanceRtpSequenceNumber();
        ushort firstNext = first.AdvanceRtpSequenceNumber();
        RtpSequenceUpdate firstReceiveState = first.GetRtpSequenceState();
        RtpSequenceUpdate secondReceiveState = second.GetRtpSequenceState();

        Assert.Multiple(() =>
        {
            Assert.That(firstInitial, Is.EqualTo(1000));
            Assert.That(firstNext, Is.EqualTo(1001));
            Assert.That(secondInitial, Is.EqualTo(5000));
            Assert.That(secondNext, Is.EqualTo(5001));
            Assert.That(firstReceiveState.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(firstReceiveState.SequenceNumber, Is.EqualTo(41));
            Assert.That(secondReceiveState.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(secondReceiveState.ProbationRemaining, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReturnedUpdatesAreIndependentSnapshots()
    {
        IRtpSequenceTracker tracker = CreateTracker(2);
        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(40);
        first.Status = RtpSequenceStatus.Invalid;
        first.ProbationRemaining = 99;
        RtpSequenceUpdate fresh = tracker.GetRtpSequenceState();

        Assert.Multiple(() =>
        {
            Assert.That(fresh, Is.Not.SameAs(first));
            Assert.That(fresh.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(fresh.SequenceNumber, Is.EqualTo(40));
            Assert.That(fresh.ProbationRemaining, Is.EqualTo(1));
        });
    }

    private static IRtpSequenceTracker CreateTracker(int minimumSequential)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = minimumSequential;
        return new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource(0));
    }

    private static IRtpSequenceTracker CreateThresholdTracker(
        int minimumSequential,
        int maximumDropout,
        int maximumMisorder)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = minimumSequential;
        configuration.JitterBuffer.MaximumDropout = maximumDropout;
        configuration.JitterBuffer.MaximumMisorder = maximumMisorder;
        return new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource(0));
    }

    private class FixedSequenceRandomSource : IRtpRandomSource
    {
        ushort sequenceNumber;
        internal int sequenceRequestCount;

        public FixedSequenceRandomSource(ushort sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }

        public uint CreateSynchronizationSource()
        {
            return 0;
        }

        public ushort CreateSequenceNumber()
        {
            sequenceRequestCount = sequenceRequestCount + 1;
            return sequenceNumber;
        }

        public uint CreateTimestamp()
        {
            return 0;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }
}
