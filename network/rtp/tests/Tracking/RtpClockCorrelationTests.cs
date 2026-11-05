using rtp;

namespace rtp.Tests.Tracking;

public class RtpClockCorrelationTests
{
    [Test]
    public void SenderReportCreatesAnEraAwareIndependentCorrelation()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(1000U, 90000);
        long presentationTicks =
            new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc).Ticks;
        RtcpSenderReport senderReport = CreateSenderReport(
            0x10203040U,
            1000U,
            RtpNtpTime.FromUtcTicks(presentationTicks));

        RtpClockCorrelation correlation = tracker.CorrelateRtcpSenderReport(
            senderReport,
            90000,
            presentationTicks);
        correlation.SourceIdentifier = 0U;
        correlation.PresentationTimeTicks = 0L;
        RtpClockCorrelation snapshot = tracker.GetRtpClockCorrelation();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(correlation));
            Assert.That(snapshot.IsValid, Is.True);
            Assert.That(snapshot.SourceIdentifier, Is.EqualTo(0x10203040U));
            Assert.That(snapshot.RtpTimestamp, Is.EqualTo(1000U));
            Assert.That(snapshot.ExtendedRtpTimestamp, Is.EqualTo(1000L));
            Assert.That(snapshot.NtpTimestamp,
                Is.EqualTo(senderReport.SenderInformation.NtpTimestamp));
            Assert.That(snapshot.ClockRate, Is.EqualTo(90000));
            Assert.That(snapshot.PresentationTimeTicks, Is.EqualTo(presentationTicks));
        });
    }

    [Test]
    public void SenderReportCanEstablishTheTimelineBeforeTheFirstRtpPacket()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        long presentationTicks =
            new DateTime(2025, 4, 10, 10, 0, 0, DateTimeKind.Utc).Ticks;
        RtcpSenderReport senderReport = CreateSenderReport(
            7U,
            0xFFFFFF00U,
            RtpNtpTime.FromUtcTicks(presentationTicks));

        RtpClockCorrelation correlation = tracker.CorrelateRtcpSenderReport(
            senderReport,
            48000,
            presentationTicks);
        RtpTimestampUpdate packet = tracker.ProcessRtpTimestamp(0x000000E0U, 48000);
        RtpClockCorrelation converted =
            tracker.ConvertRtpTimestampToPresentationTime(packet);

        Assert.Multiple(() =>
        {
            Assert.That(correlation.IsValid, Is.True);
            Assert.That(packet.ExtendedTimestamp, Is.EqualTo(4294967520L));
            Assert.That(packet.Delta, Is.EqualTo(480L));
            Assert.That(converted.IsValid, Is.True);
            Assert.That(converted.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + TimeSpan.TicksPerSecond / 100));
        });
    }

    [Test]
    public void SeparateClockRatesMapOntoOnePresentationTimeline()
    {
        long commonPresentationTicks =
            new DateTime(2025, 5, 20, 8, 30, 0, DateTimeKind.Utc).Ticks;
        ulong commonNtpTimestamp = RtpNtpTime.FromUtcTicks(commonPresentationTicks);
        IRtpTimestampTracker audio = new RtpTimestampTracker();
        IRtpTimestampTracker video = new RtpTimestampTracker();
        audio.ProcessRtpTimestamp(8000U, 8000);
        video.ProcessRtpTimestamp(90000U, 90000);
        audio.CorrelateRtcpSenderReport(
            CreateSenderReport(1U, 8000U, commonNtpTimestamp),
            8000,
            commonPresentationTicks);
        video.CorrelateRtcpSenderReport(
            CreateSenderReport(2U, 90000U, commonNtpTimestamp),
            90000,
            commonPresentationTicks);

        RtpTimestampUpdate audioPacket = audio.ProcessRtpTimestamp(12000U, 8000);
        RtpTimestampUpdate videoPacket = video.ProcessRtpTimestamp(135000U, 90000);
        RtpClockCorrelation audioTime =
            audio.ConvertRtpTimestampToPresentationTime(audioPacket);
        RtpClockCorrelation videoTime =
            video.ConvertRtpTimestampToPresentationTime(videoPacket);

        Assert.Multiple(() =>
        {
            Assert.That(audioTime.IsValid, Is.True);
            Assert.That(videoTime.IsValid, Is.True);
            Assert.That(audioTime.SourceIdentifier, Is.EqualTo(1U));
            Assert.That(videoTime.SourceIdentifier, Is.EqualTo(2U));
            Assert.That(audioTime.PresentationTimeTicks,
                Is.EqualTo(commonPresentationTicks + TimeSpan.TicksPerSecond / 2));
            Assert.That(videoTime.PresentationTimeTicks,
                Is.EqualTo(audioTime.PresentationTimeTicks));
            Assert.That(videoTime.NtpTimestamp, Is.EqualTo(audioTime.NtpTimestamp));
        });
    }

    [Test]
    public void WrappedAndReorderedTimestampsProjectAroundTheSameAnchor()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        long presentationTicks =
            new DateTime(2025, 7, 10, 9, 0, 0, DateTimeKind.Utc).Ticks;
        tracker.ProcessRtpTimestamp(0xFFFFFFF0U, 48000);
        tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                3U,
                0xFFFFFFF0U,
                RtpNtpTime.FromUtcTicks(presentationTicks)),
            48000,
            presentationTicks);

        RtpTimestampUpdate wrapped = tracker.ProcessRtpTimestamp(0x00000020U, 48000);
        RtpClockCorrelation wrappedTime =
            tracker.ConvertRtpTimestampToPresentationTime(wrapped);
        RtpTimestampUpdate reordered = tracker.ProcessRtpTimestamp(0x00000010U, 48000);
        RtpClockCorrelation reorderedTime =
            tracker.ConvertRtpTimestampToPresentationTime(reordered);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.ExtendedTimestamp, Is.EqualTo(4294967328L));
            Assert.That(wrappedTime.IsValid, Is.True);
            Assert.That(wrappedTime.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + 10000L));
            Assert.That(reordered.IsReordered, Is.True);
            Assert.That(reordered.ExtendedTimestamp, Is.EqualTo(4294967312L));
            Assert.That(reorderedTime.IsValid, Is.True);
            Assert.That(reorderedTime.PresentationTimeTicks,
                Is.EqualTo(presentationTicks + 6666L));
        });
    }

    [Test]
    public void ClockDomainChangesInvalidatePriorCorrelationUntilANewReport()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        long firstPresentationTicks =
            new DateTime(2025, 8, 10, 9, 0, 0, DateTimeKind.Utc).Ticks;
        tracker.ProcessRtpTimestamp(800U, 8000);
        tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                4U,
                800U,
                RtpNtpTime.FromUtcTicks(firstPresentationTicks)),
            8000,
            firstPresentationTicks);

        RtpTimestampUpdate transition = tracker.ProcessRtpTimestamp(1600U, 16000);
        RtpClockCorrelation invalidated = tracker.GetRtpClockCorrelation();
        RtpClockCorrelation rejectedTransition =
            tracker.ConvertRtpTimestampToPresentationTime(transition);
        long secondPresentationTicks = firstPresentationTicks + TimeSpan.TicksPerSecond;
        RtpClockCorrelation replacement = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(
                4U,
                1600U,
                RtpNtpTime.FromUtcTicks(secondPresentationTicks)),
            16000,
            secondPresentationTicks);
        RtpTimestampUpdate continued = tracker.ProcessRtpTimestamp(1760U, 16000);
        RtpClockCorrelation converted =
            tracker.ConvertRtpTimestampToPresentationTime(continued);
        tracker.ResetRtpTimestampTracker(2000U, 16000);
        RtpClockCorrelation resetCorrelation = tracker.GetRtpClockCorrelation();

        Assert.Multiple(() =>
        {
            Assert.That(transition.ClockRateChanged, Is.True);
            Assert.That(invalidated.IsValid, Is.False);
            Assert.That(rejectedTransition.IsValid, Is.False);
            Assert.That(replacement.IsValid, Is.True);
            Assert.That(replacement.ClockRate, Is.EqualTo(16000));
            Assert.That(converted.IsValid, Is.True);
            Assert.That(converted.PresentationTimeTicks,
                Is.EqualTo(secondPresentationTicks + TimeSpan.TicksPerSecond / 100));
            Assert.That(resetCorrelation.IsValid, Is.False);
        });
    }

    [Test]
    public void UnsafeOrUnavailableSenderReportTimeDoesNotReplaceSafeCorrelation()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(1000U, 90000);
        long safeTicks =
            new DateTime(2025, 9, 10, 10, 0, 0, DateTimeKind.Utc).Ticks;
        RtpClockCorrelation safe = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(5U, 1000U, RtpNtpTime.FromUtcTicks(safeTicks)),
            90000,
            safeTicks);
        long unsafeTicks =
            new DateTime(2025, 9, 30, 23, 59, 59, 500, DateTimeKind.Utc).Ticks;

        RtpClockCorrelation unsafeResult = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(5U, 2000U, RtpNtpTime.FromUtcTicks(unsafeTicks)),
            90000,
            unsafeTicks);
        RtpClockCorrelation unavailable = tracker.CorrelateRtcpSenderReport(
            CreateSenderReport(5U, 3000U, 0UL),
            90000,
            safeTicks);
        RtpClockCorrelation retained = tracker.GetRtpClockCorrelation();

        Assert.Multiple(() =>
        {
            Assert.That(safe.IsValid, Is.True);
            Assert.That(unsafeResult.IsValid, Is.False);
            Assert.That(unavailable.IsValid, Is.False);
            Assert.That(retained.IsValid, Is.True);
            Assert.That(retained.RtpTimestamp, Is.EqualTo(1000U));
            Assert.That(retained.PresentationTimeTicks, Is.EqualTo(safeTicks));
        });
    }

    [Test]
    public void CorrelationRejectsInvalidInputsAndMismatchedDomains()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(100U, 8000);
        long presentationTicks =
            new DateTime(2025, 10, 10, 10, 0, 0, DateTimeKind.Utc).Ticks;
        RtcpSenderReport report = CreateSenderReport(
            6U,
            100U,
            RtpNtpTime.FromUtcTicks(presentationTicks));

        RtpClockCorrelation mismatched = tracker.CorrelateRtcpSenderReport(
            report,
            16000,
            presentationTicks);
        RtpTimestampUpdate incompatibleUpdate = new RtpTimestampUpdate();
        incompatibleUpdate.Accepted = true;
        incompatibleUpdate.ClockRate = 16000;
        RtpClockCorrelation incompatible =
            tracker.ConvertRtpTimestampToPresentationTime(incompatibleUpdate);

        Assert.Multiple(() =>
        {
            Assert.That(mismatched.IsValid, Is.False);
            Assert.That(incompatible.IsValid, Is.False);
            Assert.Throws<RtpConfigurationException>(() =>
                tracker.CorrelateRtcpSenderReport(null!, 8000, presentationTicks));
            Assert.Throws<RtpConfigurationException>(() =>
                tracker.CorrelateRtcpSenderReport(report, 0, presentationTicks));
            Assert.Throws<RtpConfigurationException>(() =>
                tracker.CorrelateRtcpSenderReport(
                    report,
                    8000,
                    RtpNtpTime.NtpEpochTicks - 1L));
            Assert.Throws<RtpConfigurationException>(() =>
                tracker.ConvertRtpTimestampToPresentationTime(null!));
        });
    }

    private static RtcpSenderReport CreateSenderReport(
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
