using rtp;

namespace rtp.Tests.Tracking;

public class RtpTransmissionStatisticsTests
{
    const uint LocalSourceIdentifier = 0x10203040U;
    const ulong SenderReportNtpTimestamp = 0x0000123456780000UL;
    const uint CompactSenderReportTimestamp = 0x12345678U;

    [Test]
    public void MatchingRemoteReportProducesRoundTripTimeAndCopiedFields()
    {
        FixedClock clock = new FixedClock(10_000_000L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RecordSentPacket(statistics, 10_000_000L, 1000U, 3);
        RtcpSenderInformation information =
            statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp);
        RtcpReceptionReport report = CreateReport(
            CompactSenderReportTimestamp,
            0x00008000U);
        report.FractionLost = 17;
        report.CumulativePacketsLost = 4;
        report.ExtendedHighestSequenceNumber = 500U;
        report.InterarrivalJitter = 25U;

        statistics.RecordRemoteReceptionReport(0xAABBCCDDU, report, 30_000_000L);
        report.CumulativePacketsLost = 99;
        RtpRemoteReportSnapshot[] snapshots =
            statistics.GetRemoteReportSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(information.NtpTimestamp,
                Is.EqualTo(SenderReportNtpTimestamp));
            Assert.That(information.RtpTimestamp, Is.EqualTo(1000U));
            Assert.That(information.PacketCount, Is.EqualTo(1U));
            Assert.That(information.OctetCount, Is.EqualTo(3U));
            Assert.That(snapshots, Has.Length.EqualTo(1));
            Assert.That(snapshots[0].ReporterSourceIdentifier,
                Is.EqualTo(0xAABBCCDDU));
            Assert.That(snapshots[0].ReportedSourceIdentifier,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(snapshots[0].FractionLost, Is.EqualTo(17));
            Assert.That(snapshots[0].CumulativePacketsLost, Is.EqualTo(4));
            Assert.That(snapshots[0].ExtendedHighestSequenceNumber,
                Is.EqualTo(500U));
            Assert.That(snapshots[0].InterarrivalJitter, Is.EqualTo(25U));
            Assert.That(snapshots[0].LastSenderReport,
                Is.EqualTo(CompactSenderReportTimestamp));
            Assert.That(snapshots[0].DelaySinceLastSenderReport,
                Is.EqualTo(0x00008000U));
            Assert.That(snapshots[0].HasRoundTripTime, Is.True);
            Assert.That(snapshots[0].RoundTripTimeTicks,
                Is.EqualTo(15_000_000L));
            Assert.That(snapshots[0].ArrivalTimeTicks,
                Is.EqualTo(30_000_000L));
        });

        snapshots[0].RoundTripTimeTicks = 1L;
        Assert.That(
            statistics.GetRemoteReportSnapshots()[0].RoundTripTimeTicks,
            Is.EqualTo(15_000_000L));
    }

    [Test]
    public void MissingAndNegativeRttObservationsAreNotAcceptedAsSamples()
    {
        FixedClock clock = new FixedClock(10_000_000L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RecordSentPacket(statistics, 10_000_000L, 1000U, 1);
        statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp);

        statistics.RecordRemoteReceptionReport(
            1U,
            CreateReport(0U, 0U),
            20_000_000L);
        statistics.RecordRemoteReceptionReport(
            2U,
            CreateReport(0x11111111U, 0U),
            20_000_000L);
        statistics.RecordRemoteReceptionReport(
            3U,
            CreateReport(CompactSenderReportTimestamp, 0x00008000U),
            12_000_000L);

        RtpRemoteReportSnapshot[] snapshots =
            statistics.GetRemoteReportSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Has.Length.EqualTo(3));
            Assert.That(snapshots[0].HasRoundTripTime, Is.False);
            Assert.That(snapshots[1].HasRoundTripTime, Is.False);
            Assert.That(snapshots[2].HasRoundTripTime, Is.False);
            Assert.That(snapshots[0].RoundTripTimeTicks, Is.Zero);
            Assert.That(snapshots[1].RoundTripTimeTicks, Is.Zero);
            Assert.That(snapshots[2].RoundTripTimeTicks, Is.Zero);
        });
    }

    [Test]
    public void InvalidRemoteReportInputPreservesTheLatestReporterSnapshot()
    {
        FixedClock clock = new FixedClock(1L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RtcpReceptionReport report = CreateReport(0U, 0U);
        statistics.RecordRemoteReceptionReport(7U, report, 20L);
        RtcpReceptionReport wrongSource = CreateReport(0U, 0U);
        wrongSource.SourceIdentifier = LocalSourceIdentifier + 1U;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() => statistics
                .RecordRemoteReceptionReport(7U, null!, 21L));
            Assert.Throws<RtpConfigurationException>(() => statistics
                .RecordRemoteReceptionReport(7U, report, -1L));
            Assert.Throws<RtpSessionException>(() => statistics
                .RecordRemoteReceptionReport(7U, wrongSource, 21L));
            Assert.Throws<RtpSessionException>(() => statistics
                .RecordRemoteReceptionReport(7U, report, 19L));
        });

        RtpRemoteReportSnapshot[] snapshots =
            statistics.GetRemoteReportSnapshots();
        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Has.Length.EqualTo(1));
            Assert.That(snapshots[0].ReporterSourceIdentifier, Is.EqualTo(7U));
            Assert.That(snapshots[0].ArrivalTimeTicks, Is.EqualTo(20L));
        });
    }

    [Test]
    public void ResetRemovesRemoteReportsAndSenderReportTimingHistory()
    {
        FixedClock clock = new FixedClock(10_000_000L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RecordSentPacket(statistics, 10_000_000L, 1000U, 1);
        statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp);
        RtcpReceptionReport report = CreateReport(
            CompactSenderReportTimestamp,
            0U);
        statistics.RecordRemoteReceptionReport(1U, report, 20_000_000L);

        statistics.ResetTransmissionStatistics();
        statistics.RecordRemoteReceptionReport(1U, report, 30_000_000L);
        RtpRemoteReportSnapshot[] snapshots =
            statistics.GetRemoteReportSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Has.Length.EqualTo(1));
            Assert.That(snapshots[0].HasRoundTripTime, Is.False);
            Assert.That(snapshots[0].RoundTripTimeTicks, Is.Zero);
            Assert.That(
                statistics.GetTransmissionStatisticsSnapshot().SentPacketCount,
                Is.Zero);
        });
    }

    [Test]
    public void SentPacketsTrackPayloadCountersTimingAndSenderEligibility()
    {
        FixedClock clock = new FixedClock(30L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RecordSentPacket(statistics, 10L, 100U, 3);
        RtpPacket secondPacket = CreateSentPacket(200U, 5);
        secondPacket.Padding = new byte[] { 0, 0, 0, 4 };
        RtpPacketContext secondContext = CreateSentContext(20L);
        statistics.RecordSentRtpPacket(secondPacket, secondContext);

        RtpTransmissionStatisticsSnapshot active =
            statistics.GetTransmissionStatisticsSnapshot();
        RtcpSenderInformation information =
            statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp);
        RtpTransmissionStatisticsSnapshot reported =
            statistics.GetTransmissionStatisticsSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(active.SentPacketCount, Is.EqualTo(2));
            Assert.That(active.SentOctetCount, Is.EqualTo(8));
            Assert.That(active.LastRtpTimestamp, Is.EqualTo(200U));
            Assert.That(active.LastSendTimeTicks, Is.EqualTo(20L));
            Assert.That(active.IsActiveSender, Is.True);
            Assert.That(active.SenderReportEligible, Is.True);
            Assert.That(information.RtpTimestamp, Is.EqualTo(200U));
            Assert.That(information.PacketCount, Is.EqualTo(2U));
            Assert.That(information.OctetCount, Is.EqualTo(8U));
            Assert.That(reported.IsActiveSender, Is.False);
            Assert.That(reported.SenderReportEligible, Is.True);
        });

        active.SentPacketCount = 99L;
        Assert.That(
            statistics.GetTransmissionStatisticsSnapshot().SentPacketCount,
            Is.EqualTo(2));
    }

    [Test]
    public void ClockAndSentPacketInputsAreValidatedWithoutPartialCounts()
    {
        Assert.Throws<RtpConfigurationException>(() =>
            new RtpTransmissionStatistics(LocalSourceIdentifier, null!));
        FixedClock clock = new FixedClock(-1L);
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        Assert.Throws<RtpConfigurationException>(() =>
            statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp));
        Assert.Throws<RtpSessionException>(() =>
            new RtpTransmissionStatistics(
                LocalSourceIdentifier,
                new FixedClock(0L))
                .BuildRtcpSenderInformation(SenderReportNtpTimestamp));
        RtpPacket packet = CreateSentPacket(100U, 2);
        RtpPacketContext context = CreateSentContext(10L);
        statistics.RecordSentRtpPacket(packet, context);
        clock.monotonicTimeTicks = 10L;
        statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp);
        clock.monotonicTimeTicks = 9L;
        Assert.Throws<RtpSessionException>(() =>
            statistics.BuildRtcpSenderInformation(SenderReportNtpTimestamp));

        RtpPacket wrongSource = CreateSentPacket(200U, 1);
        wrongSource.SynchronizationSource = LocalSourceIdentifier + 1U;
        RtpPacket missingPayload = CreateSentPacket(200U, 1);
        missingPayload.Payload = null!;
        RtpPacketContext negativeTime = CreateSentContext(-1L);
        RtpPacketContext invalidClockRate = CreateSentContext(11L);
        invalidClockRate.ClockRate = 0;
        RtpPacketContext backwardTime = CreateSentContext(9L);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordSentRtpPacket(null!, context));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordSentRtpPacket(packet, null!));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordSentRtpPacket(wrongSource, context));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordSentRtpPacket(missingPayload, context));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordSentRtpPacket(packet, negativeTime));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordSentRtpPacket(packet, invalidClockRate));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordSentRtpPacket(packet, backwardTime));
            Assert.That(
                statistics.GetTransmissionStatisticsSnapshot().SentPacketCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void SenderInformationUsesModuloThirtyTwoBitCounters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                RtpTransmissionStatistics.GetRfcSenderCount(4294967296L),
                Is.Zero);
            Assert.That(
                RtpTransmissionStatistics.GetRfcSenderCount(4294967297L),
                Is.EqualTo(1U));
            Assert.That(
                RtpTransmissionStatistics.AddSaturatedTransmissionCount(
                    long.MaxValue,
                    1),
                Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void TransmissionTimeoutIsObservableAndClearedByActivityOrReset()
    {
        FixedClock clock = new FixedClock(10L);
        RtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        statistics.SetTransmissionTimeoutState(true);
        bool initiallyTimedOut = statistics
            .GetTransmissionStatisticsSnapshot()
            .TimedOut;
        RecordSentPacket(statistics, 10L, 100U, 1);
        bool clearedByActivity = !statistics
            .GetTransmissionStatisticsSnapshot()
            .TimedOut;
        statistics.SetTransmissionTimeoutState(true);
        statistics.ResetTransmissionStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(initiallyTimedOut, Is.True);
            Assert.That(clearedByActivity, Is.True);
            Assert.That(
                statistics.GetTransmissionStatisticsSnapshot().TimedOut,
                Is.False);
        });
    }

    private static RtcpReceptionReport CreateReport(
        uint lastSenderReport,
        uint delaySinceLastSenderReport)
    {
        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = LocalSourceIdentifier;
        report.LastSenderReport = lastSenderReport;
        report.DelaySinceLastSenderReport = delaySinceLastSenderReport;
        return report;
    }

    private static void RecordSentPacket(
        IRtpTransmissionStatistics statistics,
        long sendTimeTicks,
        uint rtpTimestamp,
        int payloadLength)
    {
        statistics.RecordSentRtpPacket(
            CreateSentPacket(rtpTimestamp, payloadLength),
            CreateSentContext(sendTimeTicks));
    }

    private static RtpPacket CreateSentPacket(
        uint rtpTimestamp,
        int payloadLength)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = LocalSourceIdentifier;
        packet.Timestamp = rtpTimestamp;
        packet.Payload = new byte[payloadLength];
        return packet;
    }

    private static RtpPacketContext CreateSentContext(long sendTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = sendTimeTicks;
        context.ClockRate = 90000;
        return context;
    }

    private class FixedClock : IRtpClock
    {
        internal long monotonicTimeTicks;

        public FixedClock(long monotonicTimeTicks)
        {
            this.monotonicTimeTicks = monotonicTimeTicks;
        }

        public long GetMonotonicTimeTicks()
        {
            return monotonicTimeTicks;
        }

        public long GetUtcTimeTicks()
        {
            return 0L;
        }

        public ulong GetNtpTimestamp()
        {
            return 0UL;
        }
    }
}
