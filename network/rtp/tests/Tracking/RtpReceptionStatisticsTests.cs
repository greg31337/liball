using rtp;

namespace rtp.Tests.Tracking;

public class RtpReceptionStatisticsTests
{
    const uint SourceIdentifier = 0x10203040U;

    [Test]
    public void ReceivedPacketsDriveRfcExpectedReceivedAndCumulativeLossCounts()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordPacket(statistics, 100, 100U, 100U,
            RtpSequenceStatus.Accepted, true, 3, false);
        RecordPacket(statistics, 104, 100U, 104U,
            RtpSequenceStatus.Accepted, true, 5, false);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(10);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.SourceIdentifier, Is.EqualTo(SourceIdentifier));
            Assert.That(snapshot.BaseExtendedSequenceNumber, Is.EqualTo(100U));
            Assert.That(snapshot.HighestExtendedSequenceNumber, Is.EqualTo(104U));
            Assert.That(snapshot.ExpectedPacketCount, Is.EqualTo(5));
            Assert.That(snapshot.ReceivedPacketCount, Is.EqualTo(2));
            Assert.That(snapshot.ReceivedOctetCount, Is.EqualTo(8));
            Assert.That(snapshot.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(snapshot.FractionLost, Is.Zero);
        });
    }

    [Test]
    public void ReportsCommitIndependentExpectedAndReceivedIntervals()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordPacket(statistics, 100, 100U, 100U,
            RtpSequenceStatus.Accepted, true, 1, false);
        RecordPacket(statistics, 104, 100U, 104U,
            RtpSequenceStatus.Accepted, true, 1, false);

        RtcpReceptionReport first = statistics.BuildRtcpReceptionReport(20);
        RecordPacket(statistics, 102, 100U, 104U,
            RtpSequenceStatus.Reordered, true, 1, false);
        RtcpReceptionReport noNewExpected =
            statistics.BuildRtcpReceptionReport(30);
        RecordPacket(statistics, 108, 100U, 108U,
            RtpSequenceStatus.Accepted, true, 1, false);
        RtcpReceptionReport next = statistics.BuildRtcpReceptionReport(40);

        Assert.Multiple(() =>
        {
            Assert.That(first.FractionLost, Is.EqualTo(153));
            Assert.That(first.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(first.ExtendedHighestSequenceNumber, Is.EqualTo(104U));
            Assert.That(noNewExpected.FractionLost, Is.Zero);
            Assert.That(noNewExpected.CumulativePacketsLost, Is.EqualTo(2));
            Assert.That(next.FractionLost, Is.EqualTo(192));
            Assert.That(next.CumulativePacketsLost, Is.EqualTo(5));
        });
    }

    [Test]
    public void CompleteReportBlocksRollIntervalsOnlyAfterSuccessfulGeneration()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RtcpSenderReport senderReport = new RtcpSenderReport();
        senderReport.SenderSynchronizationSource = SourceIdentifier;
        senderReport.SenderInformation.NtpTimestamp = 0x0123456789ABCDEFUL;
        statistics.RecordReceivedSenderReport(senderReport, 10_000_000L);
        RecordTimedPacket(
            statistics,
            100,
            100U,
            100U,
            1000U,
            10_000_000L,
            1000L,
            8000);
        RecordTimedPacket(
            statistics,
            104,
            100U,
            104U,
            1160U,
            20_000_000L,
            1320L,
            8000);

        Assert.Throws<RtpSessionException>(() =>
            statistics.BuildRtcpReceptionReport(19_999_999L));
        RtcpReceptionReport first =
            statistics.BuildRtcpReceptionReport(35_000_000L);
        RecordTimedPacket(
            statistics,
            108,
            100U,
            108U,
            1320U,
            30_000_000L,
            1480L,
            8000);
        Assert.Throws<RtpSessionException>(() =>
            statistics.BuildRtcpReceptionReport(34_000_000L));
        RtcpReceptionReport second =
            statistics.BuildRtcpReceptionReport(36_000_000L);

        Assert.Multiple(() =>
        {
            Assert.That(first.SourceIdentifier, Is.EqualTo(SourceIdentifier));
            Assert.That(first.FractionLost, Is.EqualTo(153));
            Assert.That(first.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(first.ExtendedHighestSequenceNumber, Is.EqualTo(104U));
            Assert.That(first.InterarrivalJitter, Is.EqualTo(10U));
            Assert.That(first.LastSenderReport, Is.EqualTo(0x456789ABU));
            Assert.That(first.DelaySinceLastSenderReport,
                Is.EqualTo(0x00028000U));
            Assert.That(second.FractionLost, Is.EqualTo(192));
            Assert.That(second.CumulativePacketsLost, Is.EqualTo(6));
            Assert.That(second.ExtendedHighestSequenceNumber, Is.EqualTo(108U));
        });

        statistics.ResetReceptionStatistics();
        Assert.DoesNotThrow(() => statistics.BuildRtcpReceptionReport(0L));
    }

    [Test]
    public void EveryValidArrivalCountsForRfcLossIncludingDuplicates()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordPacket(statistics, 20, 20U, 20U,
            RtpSequenceStatus.Accepted, true, 2, false);
        statistics.BuildRtcpReceptionReport(1);

        RecordPacket(statistics, 20, 20U, 20U,
            RtpSequenceStatus.Duplicate, false, 4, true);
        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(2);
        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(2);

        Assert.Multiple(() =>
        {
            Assert.That(report.FractionLost, Is.Zero);
            Assert.That(report.CumulativePacketsLost, Is.EqualTo(-1));
            Assert.That(snapshot.ExpectedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.ReceivedPacketCount, Is.EqualTo(2));
            Assert.That(snapshot.ReceivedOctetCount, Is.EqualTo(6));
            Assert.That(snapshot.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(snapshot.RepairedPacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ObservablePacketOutcomesRemainDistinctAndVisible()
    {
        RtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        statistics.SetReceptionTimeoutState(true);
        RecordObservablePacket(
            statistics,
            1,
            1U,
            1U,
            RtpSequenceStatus.Accepted,
            true,
            true,
            false);
        bool clearedByActivity = !statistics
            .GetReceptionStatisticsSnapshot(0L)
            .TimedOut;
        RecordObservablePacket(
            statistics,
            3,
            1U,
            3U,
            RtpSequenceStatus.Accepted,
            true,
            false,
            false);
        RecordObservablePacket(
            statistics,
            2,
            1U,
            3U,
            RtpSequenceStatus.Reordered,
            true,
            true,
            true);
        RecordObservablePacket(
            statistics,
            2,
            1U,
            3U,
            RtpSequenceStatus.Duplicate,
            false,
            true,
            true);
        RtpPacketContext discarded = CreateTimedContext(0L, 0L, 8000);
        discarded.IsRepaired = true;
        discarded.IsLate = true;
        statistics.RecordDiscardedRtpPacket(discarded);
        statistics.SetReceptionTimeoutState(true);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(0L);

        Assert.Multiple(() =>
        {
            Assert.That(clearedByActivity, Is.True);
            Assert.That(snapshot.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(snapshot.ReorderedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.DiscardedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.LatePacketCount, Is.EqualTo(3));
            Assert.That(snapshot.RepairedPacketCount, Is.EqualTo(4));
            Assert.That(snapshot.RecoveredPacketCount, Is.EqualTo(2));
            Assert.That(snapshot.TimedOut, Is.True);
        });

        statistics.ResetReceptionStatistics();
        RtpReceptionStatisticsSnapshot reset =
            statistics.GetReceptionStatisticsSnapshot(0L);
        Assert.Multiple(() =>
        {
            Assert.That(reset.DuplicatePacketCount, Is.Zero);
            Assert.That(reset.ReorderedPacketCount, Is.Zero);
            Assert.That(reset.DiscardedPacketCount, Is.Zero);
            Assert.That(reset.LatePacketCount, Is.Zero);
            Assert.That(reset.RepairedPacketCount, Is.Zero);
            Assert.That(reset.RecoveredPacketCount, Is.Zero);
            Assert.That(reset.TimedOut, Is.False);
        });
    }

    [Test]
    public void SignedCumulativeLossAndFractionValuesAreClampedToWireRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                RtpReceptionStatistics.ClampCumulativePacketLoss(8_388_608L),
                Is.EqualTo(RtcpProtocolConstants.CumulativeLossMaximum));
            Assert.That(
                RtpReceptionStatistics.ClampCumulativePacketLoss(-8_388_609L),
                Is.EqualTo(RtcpProtocolConstants.CumulativeLossMinimum));
            Assert.That(
                RtpReceptionStatistics.ClampCumulativePacketLoss(-12L),
                Is.EqualTo(-12));
            Assert.That(
                RtpReceptionStatistics.CalculateFractionLost(4L, 0L),
                Is.EqualTo(RtcpProtocolConstants.FractionLostMaximum));
            Assert.That(
                RtpReceptionStatistics.CalculateFractionLost(4L, 1L),
                Is.EqualTo(192));
            Assert.That(
                RtpReceptionStatistics.CalculateFractionLost(0L, 1L),
                Is.Zero);
        });
    }

    [Test]
    public void RestartAndExplicitResetBeginFreshAccountingIntervals()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordPacket(statistics, 100, 100U, 104U,
            RtpSequenceStatus.Accepted, true, 3, false);
        statistics.BuildRtcpReceptionReport(1);

        RecordPacket(statistics, 200, 200U, 200U,
            RtpSequenceStatus.Restarted, true, 7, false);
        RtcpReceptionReport restarted = statistics.BuildRtcpReceptionReport(2);
        RtpReceptionStatisticsSnapshot restartSnapshot =
            statistics.GetReceptionStatisticsSnapshot(2);
        statistics.RecordDiscardedRtpPacket(new RtpPacketContext());
        statistics.ResetReceptionStatistics();
        RtpReceptionStatisticsSnapshot reset =
            statistics.GetReceptionStatisticsSnapshot(3);

        Assert.Multiple(() =>
        {
            Assert.That(restarted.FractionLost, Is.Zero);
            Assert.That(restarted.CumulativePacketsLost, Is.Zero);
            Assert.That(restartSnapshot.BaseExtendedSequenceNumber, Is.EqualTo(200U));
            Assert.That(restartSnapshot.ExpectedPacketCount, Is.EqualTo(1));
            Assert.That(restartSnapshot.ReceivedPacketCount, Is.EqualTo(1));
            Assert.That(restartSnapshot.ReceivedOctetCount, Is.EqualTo(7));
            Assert.That(reset.BaseExtendedSequenceNumber, Is.Zero);
            Assert.That(reset.ExpectedPacketCount, Is.Zero);
            Assert.That(reset.ReceivedPacketCount, Is.Zero);
            Assert.That(reset.DiscardedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void InterarrivalJitterUsesRfcOneSixteenthFiltering()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordTimedPacket(statistics, 1, 1U, 1U, 1000U, 10L, 1000L, 8000);
        RecordTimedPacket(statistics, 2, 1U, 2U, 1160U, 20L, 1160L, 8000);
        RecordTimedPacket(statistics, 3, 1U, 3U, 1320U, 30L, 1480L, 8000);

        RtpReceptionStatisticsSnapshot tenUnitEstimate =
            statistics.GetReceptionStatisticsSnapshot(30L);
        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(30L);
        RecordTimedPacket(statistics, 4, 1U, 4U, 1480U, 40L, 1640L, 8000);
        RtpReceptionStatisticsSnapshot decayed =
            statistics.GetReceptionStatisticsSnapshot(40L);

        Assert.Multiple(() =>
        {
            Assert.That(tenUnitEstimate.InterarrivalJitter, Is.EqualTo(10U));
            Assert.That(report.InterarrivalJitter, Is.EqualTo(10U));
            Assert.That(decayed.InterarrivalJitter, Is.EqualTo(9U));
        });
    }

    [Test]
    public void TimestampWraparoundDoesNotCreateArtificialJitter()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordTimedPacket(
            statistics,
            1,
            1U,
            1U,
            uint.MaxValue - 79U,
            10L,
            1000L,
            8000);
        RecordTimedPacket(statistics, 2, 1U, 2U, 0U, 20L, 1080L, 8000);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(20L);

        Assert.That(snapshot.InterarrivalJitter, Is.Zero);
    }

    [Test]
    public void ClockRateChangesStartANewJitterDomain()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordTimedPacket(statistics, 1, 1U, 1U, 0U, 0L, 0L, 8000);
        RecordTimedPacket(statistics, 2, 1U, 2U, 80U, 10L, 160L, 8000);
        uint beforeChange = statistics
            .GetReceptionStatisticsSnapshot(10L)
            .InterarrivalJitter;

        RecordTimedPacket(statistics, 3, 1U, 3U, 1000U, 20L, 10_000L, 16000);
        uint changed = statistics
            .GetReceptionStatisticsSnapshot(20L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 4, 1U, 4U, 1160U, 30L, 10_192L, 16000);
        uint afterChange = statistics
            .GetReceptionStatisticsSnapshot(30L)
            .InterarrivalJitter;

        Assert.Multiple(() =>
        {
            Assert.That(beforeChange, Is.EqualTo(5U));
            Assert.That(changed, Is.Zero);
            Assert.That(afterChange, Is.EqualTo(2U));
        });
    }

    [Test]
    public void InvalidArrivalContextDoesNotChangeReceptionState()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordTimedPacket(statistics, 10, 10U, 10U, 100U, 10L, 1000L, 8000);
        RtpPacket packet = CreatePacket(11, 1);
        packet.Timestamp = 180U;
        RtpSequenceUpdate update = CreateUpdate(
            11, 10U, 11U, RtpSequenceStatus.Accepted, true);
        RtpPacketContext backwardArrival = CreateTimedContext(9L, 1080L, 8000);
        RtpPacketContext backwardMediaClock = CreateTimedContext(11L, 999L, 8000);
        RtpPacketContext negativeArrival = CreateTimedContext(-1L, 1080L, 8000);
        RtpPacketContext invalidClockRate = CreateTimedContext(11L, 1080L, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpSessionException>(() => statistics
                .RecordReceivedRtpPacket(packet, backwardArrival, update));
            Assert.Throws<RtpSessionException>(() => statistics
                .RecordReceivedRtpPacket(packet, backwardMediaClock, update));
            Assert.Throws<RtpConfigurationException>(() => statistics
                .RecordReceivedRtpPacket(packet, negativeArrival, update));
            Assert.Throws<RtpConfigurationException>(() => statistics
                .RecordReceivedRtpPacket(packet, invalidClockRate, update));
        });

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(11L);
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ReceivedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.HighestExtendedSequenceNumber, Is.EqualTo(10U));
            Assert.That(snapshot.InterarrivalJitter, Is.Zero);
        });
    }

    [Test]
    public void InvalidInputsFailBeforeAccountingStateChanges()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RtpPacket packet = CreatePacket(10, 1);
        RtpPacketContext context = new RtpPacketContext();
        context.ClockRate = 8000;
        RtpSequenceUpdate update = CreateUpdate(
            10, 10U, 10U, RtpSequenceStatus.Accepted, true);
        RtpPacket wrongSource = CreatePacket(10, 1);
        wrongSource.SynchronizationSource = SourceIdentifier + 1U;
        RtpSequenceUpdate wrongSequence = CreateUpdate(
            11, 10U, 11U, RtpSequenceStatus.Accepted, true);
        RtpSequenceUpdate probation = CreateUpdate(
            10, 10U, 10U, RtpSequenceStatus.Probation, false);
        RtpSequenceUpdate invalidRestart = CreateUpdate(
            10, 11U, 10U, RtpSequenceStatus.Restarted, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedRtpPacket(null!, context, update));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedRtpPacket(packet, null!, update));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedRtpPacket(packet, context, null!));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedRtpPacket(wrongSource, context, update));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedRtpPacket(packet, context, wrongSequence));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedRtpPacket(packet, context, probation));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedRtpPacket(packet, context, invalidRestart));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.GetReceptionStatisticsSnapshot(-1));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.BuildRtcpReceptionReport(-1));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordDiscardedRtpPacket(null!));
        });

        Assert.That(
            statistics.GetReceptionStatisticsSnapshot(0).ReceivedPacketCount,
            Is.Zero);
    }

    [Test]
    public void SenderReportInputsAreValidatedWithoutChangingPacketCounts()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RtcpSenderReport report = new RtcpSenderReport();
        report.SenderSynchronizationSource = SourceIdentifier;
        statistics.RecordReceivedSenderReport(report, 10);
        RtcpSenderReport wrongSource = new RtcpSenderReport();
        wrongSource.SenderSynchronizationSource = SourceIdentifier + 1U;
        RtcpSenderReport missingInformation = new RtcpSenderReport();
        missingInformation.SenderSynchronizationSource = SourceIdentifier;
        missingInformation.SenderInformation = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedSenderReport(null!, 10));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedSenderReport(missingInformation, 10));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedSenderReport(wrongSource, 10));
            Assert.Throws<RtpConfigurationException>(() =>
                statistics.RecordReceivedSenderReport(report, -1));
            Assert.That(
                statistics.GetReceptionStatisticsSnapshot(10).ReceivedPacketCount,
                Is.Zero);
        });
    }

    [Test]
    public void SenderReportsProduceLsrAndSaturatingDlsrValues()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RtcpSenderReport senderReport = new RtcpSenderReport();
        senderReport.SenderSynchronizationSource = SourceIdentifier;
        senderReport.SenderInformation.NtpTimestamp = 0x0123456789ABCDEFUL;
        statistics.RecordReceivedSenderReport(senderReport, 10_000_000L);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(35_000_000L);
        RtcpReceptionReport report =
            statistics.BuildRtcpReceptionReport(35_000_000L);
        RtpReceptionStatisticsSnapshot saturated =
            statistics.GetReceptionStatisticsSnapshot(655_370_000_000L);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.LastSenderReport, Is.EqualTo(0x456789ABU));
            Assert.That(snapshot.DelaySinceLastSenderReport,
                Is.EqualTo(0x00028000U));
            Assert.That(report.LastSenderReport, Is.EqualTo(0x456789ABU));
            Assert.That(report.DelaySinceLastSenderReport,
                Is.EqualTo(0x00028000U));
            Assert.That(saturated.DelaySinceLastSenderReport,
                Is.EqualTo(uint.MaxValue));
            Assert.Throws<RtpSessionException>(() =>
                statistics.GetReceptionStatisticsSnapshot(9_999_999L));
            Assert.Throws<RtpSessionException>(() =>
                statistics.RecordReceivedSenderReport(senderReport, 9_999_999L));
        });

        RtpReceptionStatisticsSnapshot preserved =
            statistics.GetReceptionStatisticsSnapshot(35_000_000L);
        Assert.That(preserved.LastSenderReport, Is.EqualTo(0x456789ABU));

        statistics.ResetReceptionStatistics();
        RtpReceptionStatisticsSnapshot reset =
            statistics.GetReceptionStatisticsSnapshot(0L);
        Assert.Multiple(() =>
        {
            Assert.That(reset.LastSenderReport, Is.Zero);
            Assert.That(reset.DelaySinceLastSenderReport, Is.Zero);
        });
    }

    [Test]
    public void ReturnedSnapshotsAndReportsCannotMutateAccountingState()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);
        RecordPacket(statistics, 7, 7U, 7U,
            RtpSequenceStatus.Accepted, true, 2, false);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(0);
        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(0);
        snapshot.ReceivedPacketCount = 99;
        report.CumulativePacketsLost = 99;

        RtpReceptionStatisticsSnapshot fresh =
            statistics.GetReceptionStatisticsSnapshot(0);
        RtcpReceptionReport freshReport =
            statistics.BuildRtcpReceptionReport(0);

        Assert.Multiple(() =>
        {
            Assert.That(fresh, Is.Not.SameAs(snapshot));
            Assert.That(fresh.ReceivedPacketCount, Is.EqualTo(1));
            Assert.That(freshReport, Is.Not.SameAs(report));
            Assert.That(freshReport.CumulativePacketsLost, Is.Zero);
        });
    }

    private static void RecordPacket(
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint baseSequenceNumber,
        uint highestSequenceNumber,
        RtpSequenceStatus status,
        bool accepted,
        int payloadLength,
        bool repaired)
    {
        RtpPacket packet = CreatePacket(sequenceNumber, payloadLength);
        RtpPacketContext context = new RtpPacketContext();
        context.IsRepaired = repaired;
        context.ClockRate = 8000;
        RtpSequenceUpdate update = CreateUpdate(
            sequenceNumber,
            baseSequenceNumber,
            highestSequenceNumber,
            status,
            accepted);
        statistics.RecordReceivedRtpPacket(packet, context, update);
    }

    private static void RecordTimedPacket(
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint baseSequenceNumber,
        uint highestSequenceNumber,
        uint rtpTimestamp,
        long arrivalTimeTicks,
        long mediaClockTimestamp,
        int clockRate)
    {
        RtpPacket packet = CreatePacket(sequenceNumber, 1);
        packet.Timestamp = rtpTimestamp;
        RtpPacketContext context = CreateTimedContext(
            arrivalTimeTicks,
            mediaClockTimestamp,
            clockRate);
        RtpSequenceUpdate update = CreateUpdate(
            sequenceNumber,
            baseSequenceNumber,
            highestSequenceNumber,
            RtpSequenceStatus.Accepted,
            true);
        statistics.RecordReceivedRtpPacket(packet, context, update);
    }

    private static void RecordObservablePacket(
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint baseSequenceNumber,
        uint highestSequenceNumber,
        RtpSequenceStatus status,
        bool accepted,
        bool repaired,
        bool late)
    {
        RtpPacket packet = CreatePacket(sequenceNumber, 1);
        RtpPacketContext context = CreateTimedContext(0L, 0L, 8000);
        context.IsRepaired = repaired;
        context.IsLate = late;
        RtpSequenceUpdate update = CreateUpdate(
            sequenceNumber,
            baseSequenceNumber,
            highestSequenceNumber,
            status,
            accepted);
        statistics.RecordReceivedRtpPacket(packet, context, update);
    }

    private static RtpPacketContext CreateTimedContext(
        long arrivalTimeTicks,
        long mediaClockTimestamp,
        int clockRate)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.MediaClockTimestamp = mediaClockTimestamp;
        context.ClockRate = clockRate;
        return context;
    }

    private static RtpPacket CreatePacket(
        ushort sequenceNumber,
        int payloadLength)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = SourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Payload = new byte[payloadLength];
        return packet;
    }

    private static RtpSequenceUpdate CreateUpdate(
        ushort sequenceNumber,
        uint baseSequenceNumber,
        uint highestSequenceNumber,
        RtpSequenceStatus status,
        bool accepted)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = status;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = highestSequenceNumber;
        update.BaseExtendedSequenceNumber = baseSequenceNumber;
        update.HighestExtendedSequenceNumber = highestSequenceNumber;
        update.Accepted = accepted;
        return update;
    }
}
