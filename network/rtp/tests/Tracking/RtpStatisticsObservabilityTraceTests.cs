using rtp;

namespace rtp.Tests.Tracking;

public class RtpStatisticsObservabilityTraceTests
{
    const uint LocalSourceIdentifier = 0x10203040U;
    const uint RemoteSourceIdentifier = 0x50607080U;
    const ulong FirstNtpTimestamp = 0x0000123456780000UL;
    const ulong SecondNtpTimestamp = 0x00008765ABCD0000UL;
    const uint FirstCompactTimestamp = 0x12345678U;
    const uint SecondCompactTimestamp = 0x8765ABCDU;

    [Test]
    public void SenderCountersAndEligibilityFollowFakeClockActivity()
    {
        ManualClock clock = new ManualClock();
        RtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RtpPacket first = CreatePacket(LocalSourceIdentifier, 1, 1000U, 3);
        first.Padding = new byte[] { 0, 0, 0, 4 };
        statistics.RecordSentRtpPacket(first, CreateContext(10L, 90000));
        statistics.RecordSentRtpPacket(
            CreatePacket(LocalSourceIdentifier, 2, 2000U, 5),
            CreateContext(20L, 90000));
        RtpTransmissionStatisticsSnapshot beforeReport =
            statistics.GetTransmissionStatisticsSnapshot();

        clock.MonotonicTimeTicks = 20L;
        RtcpSenderInformation firstReport =
            statistics.BuildRtcpSenderInformation(FirstNtpTimestamp);
        statistics.SetTransmissionTimeoutState(true);
        RtpTransmissionStatisticsSnapshot timedOut =
            statistics.GetTransmissionStatisticsSnapshot();
        statistics.RecordSentRtpPacket(
            CreatePacket(LocalSourceIdentifier, 3, 3000U, 7),
            CreateContext(30L, 90000));
        RtpTransmissionStatisticsSnapshot resumed =
            statistics.GetTransmissionStatisticsSnapshot();
        clock.MonotonicTimeTicks = 30L;
        RtcpSenderInformation secondReport =
            statistics.BuildRtcpSenderInformation(SecondNtpTimestamp);

        Assert.Multiple(() =>
        {
            Assert.That(beforeReport.SentPacketCount, Is.EqualTo(2L));
            Assert.That(beforeReport.SentOctetCount, Is.EqualTo(8L));
            Assert.That(beforeReport.IsActiveSender, Is.True);
            Assert.That(beforeReport.SenderReportEligible, Is.True);
            Assert.That(firstReport.PacketCount, Is.EqualTo(2U));
            Assert.That(firstReport.OctetCount, Is.EqualTo(8U));
            Assert.That(firstReport.RtpTimestamp, Is.EqualTo(2000U));
            Assert.That(timedOut.TimedOut, Is.True);
            Assert.That(resumed.TimedOut, Is.False);
            Assert.That(resumed.IsActiveSender, Is.True);
            Assert.That(resumed.SentPacketCount, Is.EqualTo(3L));
            Assert.That(resumed.SentOctetCount, Is.EqualTo(15L));
            Assert.That(secondReport.PacketCount, Is.EqualTo(3U));
            Assert.That(secondReport.OctetCount, Is.EqualTo(15U));
            Assert.That(secondReport.RtpTimestamp, Is.EqualTo(3000U));
        });
    }

    [Test]
    public void SenderReportRoundTripProducesExactLsrDlsrAndRtt()
    {
        ManualClock clock = new ManualClock();
        RtpTransmissionStatistics sender =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        RtpReceptionStatistics receiver =
            new RtpReceptionStatistics(LocalSourceIdentifier);
        sender.RecordSentRtpPacket(
            CreatePacket(LocalSourceIdentifier, 1, 90000U, 4),
            CreateContext(5_000_000L, 90000));
        clock.MonotonicTimeTicks = 10_000_000L;
        RtcpSenderInformation information =
            sender.BuildRtcpSenderInformation(FirstNtpTimestamp);
        RtcpSenderReport senderReport = new RtcpSenderReport();
        senderReport.SenderSynchronizationSource = LocalSourceIdentifier;
        senderReport.SenderInformation = information;

        receiver.RecordReceivedSenderReport(senderReport, 12_000_000L);
        RtpReceptionStatisticsSnapshot halfway =
            receiver.GetReceptionStatisticsSnapshot(17_000_000L);
        RtcpReceptionReport receptionReport =
            receiver.BuildRtcpReceptionReport(22_000_000L);
        sender.RecordRemoteReceptionReport(
            RemoteSourceIdentifier,
            receptionReport,
            30_000_000L);
        RtpRemoteReportSnapshot remote =
            sender.GetRemoteReportSnapshots().Single();

        Assert.Multiple(() =>
        {
            Assert.That(halfway.LastSenderReport,
                Is.EqualTo(FirstCompactTimestamp));
            Assert.That(halfway.DelaySinceLastSenderReport,
                Is.EqualTo(0x00008000U));
            Assert.That(receptionReport.LastSenderReport,
                Is.EqualTo(FirstCompactTimestamp));
            Assert.That(receptionReport.DelaySinceLastSenderReport,
                Is.EqualTo(0x00010000U));
            Assert.That(remote.ReporterSourceIdentifier,
                Is.EqualTo(RemoteSourceIdentifier));
            Assert.That(remote.ReportedSourceIdentifier,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(remote.HasRoundTripTime, Is.True);
            Assert.That(remote.RoundTripTimeTicks,
                Is.EqualTo(10_000_000L));
        });
    }

    [Test]
    public void RemoteReportersUpdateIndependentlyWithRttFromMatchingReports()
    {
        ManualClock clock = new ManualClock();
        IRtpTransmissionStatistics statistics =
            new RtpTransmissionStatistics(LocalSourceIdentifier, clock);
        statistics.RecordSentRtpPacket(
            CreatePacket(LocalSourceIdentifier, 1, 1000U, 1),
            CreateContext(0L, 90000));
        clock.MonotonicTimeTicks = 10_000_000L;
        statistics.BuildRtcpSenderInformation(FirstNtpTimestamp);
        clock.MonotonicTimeTicks = 20_000_000L;
        statistics.BuildRtcpSenderInformation(SecondNtpTimestamp);

        RtcpReceptionReport firstReporter = CreateReceptionReport(
            FirstCompactTimestamp,
            0x00010000U,
            10);
        statistics.RecordRemoteReceptionReport(
            1U,
            firstReporter,
            40_000_000L);
        RtcpReceptionReport secondReporter = CreateReceptionReport(
            SecondCompactTimestamp,
            0x00020000U,
            20);
        statistics.RecordRemoteReceptionReport(
            2U,
            secondReporter,
            50_000_000L);
        RtcpReceptionReport replacement = CreateReceptionReport(
            SecondCompactTimestamp,
            0x00010000U,
            30);
        statistics.RecordRemoteReceptionReport(
            1U,
            replacement,
            60_000_000L);
        replacement.FractionLost = 99;

        RtpRemoteReportSnapshot[] reports =
            statistics.GetRemoteReportSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(reports, Has.Length.EqualTo(2));
            Assert.That(reports[0].ReporterSourceIdentifier, Is.EqualTo(1U));
            Assert.That(reports[0].FractionLost, Is.EqualTo(30));
            Assert.That(reports[0].LastSenderReport,
                Is.EqualTo(SecondCompactTimestamp));
            Assert.That(reports[0].HasRoundTripTime, Is.True);
            Assert.That(reports[0].RoundTripTimeTicks,
                Is.EqualTo(30_000_000L));
            Assert.That(reports[1].ReporterSourceIdentifier, Is.EqualTo(2U));
            Assert.That(reports[1].FractionLost, Is.EqualTo(20));
            Assert.That(reports[1].HasRoundTripTime, Is.True);
            Assert.That(reports[1].RoundTripTimeTicks,
                Is.EqualTo(10_000_000L));
        });
    }

    [Test]
    public void RepairRecoveryLossAndTimeoutRemainObservableWithoutStoppingTraffic()
    {
        RtpReceptionStatistics statistics =
            new RtpReceptionStatistics(RemoteSourceIdentifier);
        RecordReceivedPacket(
            statistics,
            1,
            1U,
            RtpSequenceStatus.Accepted,
            true,
            true,
            false,
            1L);
        RecordReceivedPacket(
            statistics,
            3,
            3U,
            RtpSequenceStatus.Accepted,
            true,
            false,
            false,
            2L);
        RecordReceivedPacket(
            statistics,
            2,
            3U,
            RtpSequenceStatus.Reordered,
            true,
            true,
            true,
            3L);
        RecordReceivedPacket(
            statistics,
            2,
            3U,
            RtpSequenceStatus.Duplicate,
            false,
            true,
            true,
            4L);
        RtpPacketContext discarded = CreateContext(5L, 8000);
        discarded.IsRepaired = true;
        discarded.IsLate = true;
        statistics.RecordDiscardedRtpPacket(discarded);
        statistics.SetReceptionTimeoutState(true);

        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(5L);
        RtpReceptionStatisticsSnapshot warning =
            statistics.GetReceptionStatisticsSnapshot(5L);
        RecordReceivedPacket(
            statistics,
            4,
            4U,
            RtpSequenceStatus.Accepted,
            true,
            false,
            false,
            6L);
        RtpReceptionStatisticsSnapshot resumed =
            statistics.GetReceptionStatisticsSnapshot(6L);

        Assert.Multiple(() =>
        {
            Assert.That(report.CumulativePacketsLost, Is.EqualTo(-1));
            Assert.That(report.FractionLost, Is.Zero);
            Assert.That(warning.RepairedPacketCount, Is.EqualTo(4L));
            Assert.That(warning.RecoveredPacketCount, Is.EqualTo(2L));
            Assert.That(warning.ReorderedPacketCount, Is.EqualTo(1L));
            Assert.That(warning.DuplicatePacketCount, Is.EqualTo(1L));
            Assert.That(warning.DiscardedPacketCount, Is.EqualTo(1L));
            Assert.That(warning.LatePacketCount, Is.EqualTo(3L));
            Assert.That(warning.TimedOut, Is.True);
            Assert.That(resumed.TimedOut, Is.False);
            Assert.That(resumed.ReceivedPacketCount, Is.EqualTo(5L));
            Assert.That(resumed.CumulativePacketsLost, Is.EqualTo(-1));
        });
    }

    [Test]
    public void FakeClockExposesExactSenderAndMemberTimeoutBoundaries()
    {
        RtpSessionConfiguration configuration = CreateSessionConfiguration();
        ManualClock clock = new ManualClock();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        ingress.remoteSourceTable.RecordRemoteRtpPacket(
            CreatePacket(RemoteSourceIdentifier, 1, 1000U, 1),
            CreateContext(0L, 90000));

        clock.MonotonicTimeTicks = 4L;
        RtpSessionUpdate beforeSenderTimeout = poller.PollSession();
        clock.MonotonicTimeTicks = 5L;
        RtpSessionUpdate senderTimeout = poller.PollSession();
        clock.MonotonicTimeTicks = 9L;
        RtpSessionUpdate beforeMemberTimeout = poller.PollSession();
        clock.MonotonicTimeTicks = 10L;
        RtpSessionUpdate memberTimeout = poller.PollSession();
        RtpSessionEvent senderEvent = senderTimeout.Events.Single(
            value => value.Type == RtpSessionEventType.Timeout);
        RtpSessionEvent memberEvent = memberTimeout.Events.Single(
            value => value.Type == RtpSessionEventType.Timeout);

        Assert.Multiple(() =>
        {
            Assert.That(beforeSenderTimeout.Events.Any(
                value => value.Type == RtpSessionEventType.Timeout), Is.False);
            Assert.That(senderEvent.SourceIdentifier,
                Is.EqualTo(RemoteSourceIdentifier));
            Assert.That(senderEvent.SourceStatus,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(senderEvent.Source?.IsSender, Is.False);
            Assert.That(beforeMemberTimeout.Events.Any(
                value => value.Type == RtpSessionEventType.Timeout), Is.False);
            Assert.That(memberEvent.SourceIdentifier,
                Is.EqualTo(RemoteSourceIdentifier));
            Assert.That(memberEvent.SourceStatus,
                Is.EqualTo(RtpSourceStatus.Inactive));
            Assert.That(memberEvent.Message, Does.Contain("membership expired"));
            Assert.That(ingress.remoteSourceTable.GetRemoteSourceSnapshots(),
                Is.Empty);
        });
    }

    static RtcpReceptionReport CreateReceptionReport(
        uint lastSenderReport,
        uint delaySinceLastSenderReport,
        byte fractionLost)
    {
        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = LocalSourceIdentifier;
        report.FractionLost = fractionLost;
        report.CumulativePacketsLost = fractionLost;
        report.ExtendedHighestSequenceNumber = fractionLost;
        report.InterarrivalJitter = fractionLost;
        report.LastSenderReport = lastSenderReport;
        report.DelaySinceLastSenderReport = delaySinceLastSenderReport;
        return report;
    }

    static void RecordReceivedPacket(
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint highestExtendedSequenceNumber,
        RtpSequenceStatus status,
        bool accepted,
        bool repaired,
        bool late,
        long arrivalTimeTicks)
    {
        RtpPacket packet = CreatePacket(
            RemoteSourceIdentifier,
            sequenceNumber,
            (uint)(sequenceNumber * 160),
            1);
        RtpPacketContext context = CreateContext(arrivalTimeTicks, 8000);
        context.MediaClockTimestamp = arrivalTimeTicks * 160L;
        context.IsRepaired = repaired;
        context.IsLate = late;
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = status;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = sequenceNumber;
        update.BaseExtendedSequenceNumber = 1U;
        update.HighestExtendedSequenceNumber = highestExtendedSequenceNumber;
        update.Accepted = accepted;
        statistics.RecordReceivedRtpPacket(packet, context, update);
    }

    static RtpPacket CreatePacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp,
        int payloadLength)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Payload = new byte[payloadLength];
        return packet;
    }

    static RtpPacketContext CreateContext(long operationTimeTicks, int clockRate)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.MediaClockTimestamp = operationTimeTicks * clockRate;
        context.ClockRate = clockRate;
        context.TransportIdentity = "remote-path";
        return context;
    }

    static RtpSessionConfiguration CreateSessionConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 4;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
        configuration.RtcpScheduler.SenderTimeoutTicks = 5L;
        configuration.RtcpScheduler.MemberTimeoutTicks = 10L;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry()
    {
        RtpPayloadTypeRegistry registry = new RtpPayloadTypeRegistry(
            RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = 96;
        definition.ClockRate = 90000;
        definition.MediaIdentity = "video";
        definition.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(definition);
        return registry;
    }

    private class ManualClock : IRtpClock
    {
        public long MonotonicTimeTicks;

        public long GetMonotonicTimeTicks()
        {
            return MonotonicTimeTicks;
        }

        public long GetUtcTimeTicks()
        {
            return 0L;
        }

        public ulong GetNtpTimestamp()
        {
            return FirstNtpTimestamp;
        }
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return LocalSourceIdentifier;
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
