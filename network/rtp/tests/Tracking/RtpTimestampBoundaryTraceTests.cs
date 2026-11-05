using rtp;

namespace rtp.Tests.Tracking;

public class RtpTimestampBoundaryTraceTests
{
    [Test]
    public void ExtendedTimestampTraceCrossesTwoWrapCycles()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();

        RtpTimestampUpdate initial =
            tracker.ProcessRtpTimestamp(0xFFFFFF00U, 90000);
        RtpTimestampUpdate firstWrap =
            tracker.ProcessRtpTimestamp(0x00000100U, 90000);
        RtpTimestampUpdate firstCycleHigh =
            tracker.ProcessRtpTimestamp(0x70000100U, 90000);
        RtpTimestampUpdate beforeSecondWrap =
            tracker.ProcessRtpTimestamp(0xE0000100U, 90000);
        RtpTimestampUpdate secondWrap =
            tracker.ProcessRtpTimestamp(0x50000100U, 90000);

        Assert.Multiple(() =>
        {
            Assert.That(initial.ExtendedTimestamp, Is.EqualTo(4294967040L));
            Assert.That(firstWrap.ExtendedTimestamp, Is.EqualTo(4294967552L));
            Assert.That(firstWrap.Delta, Is.EqualTo(512L));
            Assert.That(firstCycleHigh.ExtendedTimestamp, Is.EqualTo(6174015744L));
            Assert.That(beforeSecondWrap.ExtendedTimestamp,
                Is.EqualTo(8053063936L));
            Assert.That(secondWrap.ExtendedTimestamp, Is.EqualTo(9932112128L));
            Assert.That(secondWrap.Delta, Is.EqualTo(1879048192L));
            Assert.That(secondWrap.IsForward, Is.True);
            Assert.That(secondWrap.IsDiscontinuity, Is.False);
        });
    }

    [Test]
    public void BackwardReorderAcrossWrapDoesNotMoveTimelineHead()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0xFFFFFF00U, 48000);
        tracker.ProcessRtpTimestamp(0x00000100U, 48000);
        tracker.ProcessRtpTimestamp(0x00000300U, 48000);

        RtpTimestampUpdate reordered =
            tracker.ProcessRtpTimestamp(0xFFFFFF80U, 48000);
        RtpTimestampUpdate resumed =
            tracker.ProcessRtpTimestamp(0x00000400U, 48000);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.ExtendedTimestamp, Is.EqualTo(4294967168L));
            Assert.That(reordered.Delta, Is.EqualTo(-896L));
            Assert.That(reordered.Accepted, Is.True);
            Assert.That(reordered.IsReordered, Is.True);
            Assert.That(reordered.IsForward, Is.False);
            Assert.That(resumed.ExtendedTimestamp, Is.EqualTo(4294968320L));
            Assert.That(resumed.Delta, Is.EqualTo(256L));
            Assert.That(resumed.IsForward, Is.True);
        });
    }

    [Test]
    public void BothHalfRangeDirectionsResetAndInvalidateCorrelation()
    {
        long presentationTicks =
            new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc).Ticks;
        IRtpTimestampTracker forwardTracker = new RtpTimestampTracker();
        forwardTracker.ProcessRtpTimestamp(100U, 90000);
        forwardTracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                1U,
                100U,
                RtpNtpTime.FromUtcTicks(presentationTicks)),
            90000,
            presentationTicks);

        RtpTimestampUpdate forwardHalfRange =
            forwardTracker.ProcessRtpTimestamp(0x80000064U, 90000);
        RtpClockCorrelation invalidated =
            forwardTracker.GetRtpClockCorrelation();
        IRtpTimestampTracker backwardTracker = new RtpTimestampTracker();
        backwardTracker.ProcessRtpTimestamp(0x80000064U, 90000);
        RtpTimestampUpdate backwardHalfRange =
            backwardTracker.ProcessRtpTimestamp(100U, 90000);

        Assert.Multiple(() =>
        {
            Assert.That(forwardHalfRange.IsDiscontinuity, Is.True);
            Assert.That(forwardHalfRange.ExtendedTimestamp,
                Is.EqualTo(2147483748L));
            Assert.That(forwardHalfRange.Delta, Is.Zero);
            Assert.That(forwardHalfRange.ClockRateChanged, Is.False);
            Assert.That(invalidated.IsValid, Is.False);
            Assert.That(backwardHalfRange.IsDiscontinuity, Is.True);
            Assert.That(backwardHalfRange.ExtendedTimestamp, Is.EqualTo(100L));
            Assert.That(backwardHalfRange.Delta, Is.Zero);
            Assert.That(backwardHalfRange.ClockRateChanged, Is.False);
        });
    }

    [Test]
    public void ClockRateMatrixMapsOneSecondToACommonPresentationDelta()
    {
        int[] clockRates = new int[] { 8000, 11025, 44100, 48000, 90000 };
        long presentationTicks =
            new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc).Ticks;
        int index = 0;

        while (index < clockRates.Length)
        {
            int clockRate = clockRates[index];
            IRtpTimestampTracker tracker = new RtpTimestampTracker();
            tracker.ProcessRtpTimestamp(100000U, clockRate);
            RtpClockCorrelation senderReport = tracker.CorrelateRtcpSenderReport(
                CreateSenderReport(
                    (uint)(index + 1),
                    100000U,
                    RtpNtpTime.FromUtcTicks(presentationTicks)),
                clockRate,
                presentationTicks);
            RtpTimestampUpdate update = tracker.ProcessRtpTimestamp(
                100000U + (uint)clockRate,
                clockRate);
            RtpClockCorrelation converted =
                tracker.ConvertRtpTimestampToPresentationTime(update);

            Assert.Multiple(() =>
            {
                Assert.That(senderReport.IsValid, Is.True, $"rate {clockRate}");
                Assert.That(update.Delta, Is.EqualTo((long)clockRate),
                    $"rate {clockRate}");
                Assert.That(converted.IsValid, Is.True, $"rate {clockRate}");
                Assert.That(converted.PresentationTimeTicks,
                    Is.EqualTo(presentationTicks + TimeSpan.TicksPerSecond),
                    $"rate {clockRate}");
            });
            index = index + 1;
        }
    }

    [Test]
    public void ClockRateChangeStartsANewDomainAndRequiresANewSenderReport()
    {
        long firstPresentationTicks =
            new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc).Ticks;
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0xFFFFFFF0U, 48000);
        tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                10U,
                0xFFFFFFF0U,
                RtpNtpTime.FromUtcTicks(firstPresentationTicks)),
            48000,
            firstPresentationTicks);

        RtpTimestampUpdate changed =
            tracker.ProcessRtpTimestamp(0x00000010U, 90000);
        RtpClockCorrelation stale =
            tracker.ConvertRtpTimestampToPresentationTime(changed);
        long secondPresentationTicks =
            firstPresentationTicks + TimeSpan.TicksPerSecond;
        RtpClockCorrelation replacement = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                10U,
                0x00000010U,
                RtpNtpTime.FromUtcTicks(secondPresentationTicks)),
            90000,
            secondPresentationTicks);
        RtpTimestampUpdate continued =
            tracker.ProcessRtpTimestamp(90016U, 90000);
        RtpClockCorrelation converted =
            tracker.ConvertRtpTimestampToPresentationTime(continued);

        Assert.Multiple(() =>
        {
            Assert.That(changed.ExtendedTimestamp, Is.EqualTo(16L));
            Assert.That(changed.Delta, Is.Zero);
            Assert.That(changed.ClockRateChanged, Is.True);
            Assert.That(changed.IsDiscontinuity, Is.True);
            Assert.That(stale.IsValid, Is.False);
            Assert.That(replacement.IsValid, Is.True);
            Assert.That(replacement.ClockRate, Is.EqualTo(90000));
            Assert.That(continued.Delta, Is.EqualTo(90000L));
            Assert.That(converted.IsValid, Is.True);
            Assert.That(converted.PresentationTimeTicks,
                Is.EqualTo(secondPresentationTicks + TimeSpan.TicksPerSecond));
        });
    }

    [Test]
    public void SenderReportBehindWrappedHeadUsesTheMatchingExtendedCycle()
    {
        long presentationTicks =
            new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc).Ticks;
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0xFFFFFF00U, 48000);
        tracker.ProcessRtpTimestamp(0x00000100U, 48000);

        RtpClockCorrelation senderReport = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                20U,
                0x00000080U,
                RtpNtpTime.FromUtcTicks(presentationTicks)),
            48000,
            presentationTicks);
        RtpTimestampUpdate packet =
            tracker.ProcessRtpTimestamp(0x00000180U, 48000);
        RtpClockCorrelation converted =
            tracker.ConvertRtpTimestampToPresentationTime(packet);

        Assert.Multiple(() =>
        {
            Assert.That(senderReport.IsValid, Is.True);
            Assert.That(senderReport.ExtendedRtpTimestamp,
                Is.EqualTo(4294967424L));
            Assert.That(packet.ExtendedTimestamp, Is.EqualTo(4294967680L));
            Assert.That(converted.IsValid, Is.True);
            Assert.That(converted.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + 53333L));
        });
    }

    [Test]
    public void PresentationConversionTruncatesBothFractionalDirectionsTowardZero()
    {
        long presentationTicks =
            new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc).Ticks;
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(44100U, 44100);
        tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                30U,
                44100U,
                RtpNtpTime.FromUtcTicks(presentationTicks)),
            44100,
            presentationTicks);

        RtpTimestampUpdate before = tracker.ProcessRtpTimestamp(44099U, 44100);
        RtpClockCorrelation beforeTime =
            tracker.ConvertRtpTimestampToPresentationTime(before);
        RtpTimestampUpdate after = tracker.ProcessRtpTimestamp(44101U, 44100);
        RtpClockCorrelation afterTime =
            tracker.ConvertRtpTimestampToPresentationTime(after);
        RtpTimestampUpdate halfSecond =
            tracker.ProcessRtpTimestamp(66150U, 44100);
        RtpClockCorrelation halfSecondTime =
            tracker.ConvertRtpTimestampToPresentationTime(halfSecond);

        Assert.Multiple(() =>
        {
            Assert.That(before.IsReordered, Is.True);
            Assert.That(beforeTime.IsValid, Is.True);
            Assert.That(beforeTime.PresentationTimeTicks,
                Is.EqualTo(presentationTicks - 226L));
            Assert.That(afterTime.IsValid, Is.True);
            Assert.That(afterTime.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + 226L));
            Assert.That(halfSecondTime.IsValid, Is.True);
            Assert.That(halfSecondTime.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + TimeSpan.TicksPerSecond / 2));
        });
    }

    [Test]
    public void LeapSecondBoundaryReportsAreRejectedWithoutBreakingSafeTimeline()
    {
        long safeTicks =
            new DateTime(2016, 12, 31, 23, 59, 58, DateTimeKind.Utc).Ticks;
        long lastSecondTicks =
            new DateTime(2016, 12, 31, 23, 59, 59, DateTimeKind.Utc).Ticks;
        long firstSecondTicks =
            new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        long afterBoundaryTicks =
            new DateTime(2017, 1, 1, 0, 0, 1, DateTimeKind.Utc).Ticks;
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(1000U, 8000);
        RtpClockCorrelation safe = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(40U, 1000U, RtpNtpTime.FromUtcTicks(safeTicks)),
            8000,
            safeTicks);

        RtpClockCorrelation rejectedLast = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                40U,
                9000U,
                RtpNtpTime.FromUtcTicks(lastSecondTicks)),
            8000,
            lastSecondTicks);
        RtpTimestampUpdate atFirstSecond =
            tracker.ProcessRtpTimestamp(17000U, 8000);
        RtpClockCorrelation convertedFirstSecond =
            tracker.ConvertRtpTimestampToPresentationTime(atFirstSecond);
        RtpClockCorrelation rejectedFirst = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                40U,
                17000U,
                RtpNtpTime.FromUtcTicks(firstSecondTicks)),
            8000,
            firstSecondTicks);
        RtpTimestampUpdate afterBoundary =
            tracker.ProcessRtpTimestamp(25000U, 8000);
        RtpClockCorrelation convertedAfterBoundary =
            tracker.ConvertRtpTimestampToPresentationTime(afterBoundary);
        RtpClockCorrelation replacement = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                40U,
                25000U,
                RtpNtpTime.FromUtcTicks(afterBoundaryTicks)),
            8000,
            afterBoundaryTicks);

        Assert.Multiple(() =>
        {
            Assert.That(safe.IsValid, Is.True);
            Assert.That(rejectedLast.IsValid, Is.False);
            Assert.That(convertedFirstSecond.IsValid, Is.True);
            Assert.That(convertedFirstSecond.PresentationTimeTicks,
                Is.EqualTo(firstSecondTicks));
            Assert.That(rejectedFirst.IsValid, Is.False);
            Assert.That(convertedAfterBoundary.IsValid, Is.True);
            Assert.That(convertedAfterBoundary.PresentationTimeTicks,
                Is.EqualTo(afterBoundaryTicks));
            Assert.That(replacement.IsValid, Is.True);
            Assert.That(replacement.PresentationTimeTicks,
                Is.EqualTo(afterBoundaryTicks));
        });
    }

    static RtcpSenderReport CreateSenderReport(
        uint sourceIdentifier,
        uint rtpTimestamp,
        ulong ntpTimestamp)
    {
        RtcpSenderReport report = new RtcpSenderReport();
        report.SenderSynchronizationSource = sourceIdentifier;
        report.SenderInformation.NtpTimestamp = ntpTimestamp;
        report.SenderInformation.RtpTimestamp = rtpTimestamp;
        return report;
    }
}
