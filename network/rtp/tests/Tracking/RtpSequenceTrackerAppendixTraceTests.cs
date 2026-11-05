using rtp;

namespace rtp.Tests.Tracking;

public class RtpSequenceTrackerAppendixTraceTests
{
    [Test]
    public void AppendixTraceCoversProbationDuplicatesReorderAndDropout()
    {
        IRtpSequenceTracker tracker = CreateTracker(3, 3000, 100);

        RtpSequenceUpdate first = tracker.ProcessRtpSequenceNumber(1000);
        RtpSequenceUpdate repeatedProbation =
            tracker.ProcessRtpSequenceNumber(1000);
        RtpSequenceUpdate second = tracker.ProcessRtpSequenceNumber(1001);
        RtpSequenceUpdate probationGap =
            tracker.ProcessRtpSequenceNumber(1003);
        RtpSequenceUpdate fourth = tracker.ProcessRtpSequenceNumber(1004);
        RtpSequenceUpdate validated = tracker.ProcessRtpSequenceNumber(1005);
        RtpSequenceUpdate duplicate = tracker.ProcessRtpSequenceNumber(1005);
        RtpSequenceUpdate acceptedGap = tracker.ProcessRtpSequenceNumber(1008);
        RtpSequenceUpdate reordered = tracker.ProcessRtpSequenceNumber(1007);
        RtpSequenceUpdate repeatedReorder =
            tracker.ProcessRtpSequenceNumber(1007);
        RtpSequenceUpdate lastBeforeDropout =
            tracker.ProcessRtpSequenceNumber(4007);
        RtpSequenceUpdate dropout = tracker.ProcessRtpSequenceNumber(7007);
        RtpSequenceUpdate resumed = tracker.ProcessRtpSequenceNumber(4008);
        RtpSequenceUpdate restartCandidate =
            tracker.ProcessRtpSequenceNumber(30000);
        RtpSequenceUpdate restarted = tracker.ProcessRtpSequenceNumber(30001);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(first.ProbationRemaining, Is.EqualTo(2));
            Assert.That(repeatedProbation.Status,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(repeatedProbation.ProbationRemaining, Is.EqualTo(2));
            Assert.That(second.ProbationRemaining, Is.EqualTo(1));
            Assert.That(probationGap.ProbationRemaining, Is.EqualTo(2));
            Assert.That(fourth.ProbationRemaining, Is.EqualTo(1));
            Assert.That(validated.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(validated.BaseExtendedSequenceNumber, Is.EqualTo(1005U));
            Assert.That(duplicate.Status, Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(acceptedGap.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(acceptedGap.ExtendedSequenceNumber, Is.EqualTo(1008U));
            Assert.That(reordered.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(reordered.ExtendedSequenceNumber, Is.EqualTo(1007U));
            Assert.That(reordered.Accepted, Is.True);
            Assert.That(repeatedReorder.Status,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(lastBeforeDropout.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(lastBeforeDropout.ExtendedSequenceNumber,
                Is.EqualTo(4007U));
            Assert.That(dropout.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(dropout.Accepted, Is.False);
            Assert.That(dropout.HighestExtendedSequenceNumber,
                Is.EqualTo(4007U));
            Assert.That(resumed.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(resumed.ExtendedSequenceNumber, Is.EqualTo(4008U));
            Assert.That(restartCandidate.Status,
                Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(restarted.Status, Is.EqualTo(RtpSequenceStatus.Restarted));
            Assert.That(restarted.BaseExtendedSequenceNumber, Is.EqualTo(30001U));
            Assert.That(restarted.HighestExtendedSequenceNumber,
                Is.EqualTo(30001U));
            Assert.That(restarted.Accepted, Is.True);
        });
    }

    [Test]
    public void ExtendedSequenceTraceCrossesTwoWrapCycles()
    {
        IRtpSequenceTracker tracker = CreateTracker(1, 3000, 100);
        RtpSequenceUpdate initial = tracker.ProcessRtpSequenceNumber(65534);
        RtpSequenceUpdate beforeFirstWrap =
            tracker.ProcessRtpSequenceNumber(65535);
        RtpSequenceUpdate firstWrap = tracker.ProcessRtpSequenceNumber(0);
        uint expectedExtended = 65536U;
        RtpSequenceUpdate step = firstWrap;
        while (expectedExtended + 2000U < 131071U)
        {
            expectedExtended = expectedExtended + 2000U;
            step = tracker.ProcessRtpSequenceNumber(
                (ushort)(expectedExtended & 65535U));
            Assert.Multiple(() =>
            {
                Assert.That(step.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
                Assert.That(step.ExtendedSequenceNumber,
                    Is.EqualTo(expectedExtended));
            });
        }

        RtpSequenceUpdate beforeSecondWrap =
            tracker.ProcessRtpSequenceNumber(65535);
        RtpSequenceUpdate secondWrap = tracker.ProcessRtpSequenceNumber(0);
        RtpSequenceUpdate previousCycleDuplicate =
            tracker.ProcessRtpSequenceNumber(65535);
        RtpSequenceUpdate previousCycleReorder =
            tracker.ProcessRtpSequenceNumber(65534);
        RtpSequenceUpdate repeatedPreviousCycleReorder =
            tracker.ProcessRtpSequenceNumber(65534);

        Assert.Multiple(() =>
        {
            Assert.That(initial.BaseExtendedSequenceNumber, Is.EqualTo(65534U));
            Assert.That(beforeFirstWrap.ExtendedSequenceNumber,
                Is.EqualTo(65535U));
            Assert.That(firstWrap.ExtendedSequenceNumber, Is.EqualTo(65536U));
            Assert.That(beforeSecondWrap.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(beforeSecondWrap.ExtendedSequenceNumber,
                Is.EqualTo(131071U));
            Assert.That(secondWrap.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(secondWrap.ExtendedSequenceNumber, Is.EqualTo(131072U));
            Assert.That(secondWrap.HighestExtendedSequenceNumber,
                Is.EqualTo(131072U));
            Assert.That(previousCycleDuplicate.Status,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(previousCycleDuplicate.ExtendedSequenceNumber,
                Is.EqualTo(131071U));
            Assert.That(previousCycleReorder.Status,
                Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(previousCycleReorder.ExtendedSequenceNumber,
                Is.EqualTo(131070U));
            Assert.That(repeatedPreviousCycleReorder.Status,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(repeatedPreviousCycleReorder.HighestExtendedSequenceNumber,
                Is.EqualTo(131072U));
        });
    }

    [Test]
    public void RestartConfirmationSequenceWrapsFromMaximumToZero()
    {
        IRtpSequenceTracker tracker = CreateTracker(1, 3000, 100);
        tracker.ProcessRtpSequenceNumber(100);

        RtpSequenceUpdate candidate =
            tracker.ProcessRtpSequenceNumber(ushort.MaxValue);
        RtpSequenceUpdate confirmation = tracker.ProcessRtpSequenceNumber(0);
        RtpSequenceUpdate next = tracker.ProcessRtpSequenceNumber(1);

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Status, Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(candidate.Accepted, Is.False);
            Assert.That(candidate.HighestExtendedSequenceNumber, Is.EqualTo(100U));
            Assert.That(confirmation.Status,
                Is.EqualTo(RtpSequenceStatus.Restarted));
            Assert.That(confirmation.SequenceNumber, Is.Zero);
            Assert.That(confirmation.ExtendedSequenceNumber, Is.Zero);
            Assert.That(confirmation.BaseExtendedSequenceNumber, Is.Zero);
            Assert.That(confirmation.HighestExtendedSequenceNumber, Is.Zero);
            Assert.That(next.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(next.ExtendedSequenceNumber, Is.EqualTo(1U));
        });
    }

    [Test]
    public void ResetClearsCyclesObservedPacketsAndPendingRestart()
    {
        IRtpSequenceTracker tracker = CreateTracker(3, 3000, 100);
        tracker.ProcessRtpSequenceNumber(65534);
        tracker.ProcessRtpSequenceNumber(65535);
        tracker.ProcessRtpSequenceNumber(0);
        tracker.ProcessRtpSequenceNumber(5000);

        RtpSequenceUpdate reset =
            tracker.ResetRtpSequenceTracker(ushort.MaxValue);
        RtpSequenceUpdate probation = tracker.ProcessRtpSequenceNumber(0);
        RtpSequenceUpdate accepted = tracker.ProcessRtpSequenceNumber(1);
        RtpSequenceUpdate oldPendingConfirmation =
            tracker.ProcessRtpSequenceNumber(5001);
        RtpSequenceUpdate oldCyclePacket =
            tracker.ProcessRtpSequenceNumber(ushort.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(reset.ProbationRemaining, Is.EqualTo(2));
            Assert.That(reset.BaseExtendedSequenceNumber, Is.Zero);
            Assert.That(reset.HighestExtendedSequenceNumber, Is.Zero);
            Assert.That(probation.Status,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(probation.ProbationRemaining, Is.EqualTo(1));
            Assert.That(accepted.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(accepted.BaseExtendedSequenceNumber, Is.EqualTo(1U));
            Assert.That(accepted.ExtendedSequenceNumber, Is.EqualTo(1U));
            Assert.That(oldPendingConfirmation.Status,
                Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(oldCyclePacket.Status,
                Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(oldCyclePacket.ExtendedSequenceNumber,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(oldCyclePacket.Accepted, Is.True);
        });
    }

    [Test]
    public void ConfiguredDropoutAndMisorderEdgesUseExclusiveBoundaries()
    {
        IRtpSequenceTracker tracker = CreateTracker(1, 10, 4);
        tracker.ProcessRtpSequenceNumber(100);

        RtpSequenceUpdate lastForward = tracker.ProcessRtpSequenceNumber(109);
        RtpSequenceUpdate dropoutBoundary =
            tracker.ProcessRtpSequenceNumber(119);
        RtpSequenceUpdate resumed = tracker.ProcessRtpSequenceNumber(110);
        RtpSequenceUpdate lastReordered =
            tracker.ProcessRtpSequenceNumber(107);
        RtpSequenceUpdate misorderBoundary =
            tracker.ProcessRtpSequenceNumber(106);

        Assert.Multiple(() =>
        {
            Assert.That(lastForward.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(lastForward.ExtendedSequenceNumber, Is.EqualTo(109U));
            Assert.That(dropoutBoundary.Status,
                Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(dropoutBoundary.Accepted, Is.False);
            Assert.That(resumed.Status, Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(lastReordered.Status,
                Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(lastReordered.ExtendedSequenceNumber, Is.EqualTo(107U));
            Assert.That(misorderBoundary.Status,
                Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(misorderBoundary.Accepted, Is.False);
            Assert.That(misorderBoundary.HighestExtendedSequenceNumber,
                Is.EqualTo(110U));
        });
    }

    static IRtpSequenceTracker CreateTracker(
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
            new FixedSequenceRandomSource());
    }

    private class FixedSequenceRandomSource : IRtpRandomSource
    {
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
            return 0.5;
        }
    }
}
