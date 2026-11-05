using rtp;

namespace rtp.Tests.Tracking;

public class RtpReceptionStatisticsRfcTraceTests
{
    const uint SourceIdentifier = 0x55667788U;

    [Test]
    public void EmptySourceProducesAZeroedReceptionReport()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(0L);
        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(0L);

        Assert.Multiple(() =>
        {
            Assert.That(report.SourceIdentifier, Is.EqualTo(SourceIdentifier));
            Assert.That(report.FractionLost, Is.Zero);
            Assert.That(report.CumulativePacketsLost, Is.Zero);
            Assert.That(report.ExtendedHighestSequenceNumber, Is.Zero);
            Assert.That(report.InterarrivalJitter, Is.Zero);
            Assert.That(snapshot.ExpectedPacketCount, Is.Zero);
            Assert.That(snapshot.ReceivedPacketCount, Is.Zero);
            Assert.That(snapshot.CumulativePacketsLost, Is.Zero);
            Assert.That(snapshot.FractionLost, Is.Zero);
        });
    }

    [Test]
    public void BurstLossLateRecoveryAndDuplicatesUseIndependentReportIntervals()
    {
        IRtpSequenceTracker sequenceTracker = CreateSequenceTracker();
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordPacket(sequenceTracker, statistics, 100, 1L);
        RecordPacket(sequenceTracker, statistics, 105, 2L);
        RtcpReceptionReport burstLoss =
            statistics.BuildRtcpReceptionReport(2L);

        RtpSequenceUpdate late101 =
            RecordPacket(sequenceTracker, statistics, 101, 3L);
        RecordPacket(sequenceTracker, statistics, 103, 4L);
        RecordPacket(sequenceTracker, statistics, 102, 5L);
        RecordPacket(sequenceTracker, statistics, 104, 6L);
        RtcpReceptionReport recovered =
            statistics.BuildRtcpReceptionReport(6L);

        RecordPacket(sequenceTracker, statistics, 110, 7L);
        RtcpReceptionReport nextBurst =
            statistics.BuildRtcpReceptionReport(7L);
        RtpSequenceUpdate duplicate110 =
            RecordPacket(sequenceTracker, statistics, 110, 8L);
        RtcpReceptionReport duplicateInterval =
            statistics.BuildRtcpReceptionReport(8L);
        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(8L);

        Assert.Multiple(() =>
        {
            Assert.That(burstLoss.FractionLost, Is.EqualTo(170));
            Assert.That(burstLoss.CumulativePacketsLost, Is.EqualTo(4));
            Assert.That(late101.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(recovered.FractionLost, Is.Zero);
            Assert.That(recovered.CumulativePacketsLost, Is.Zero);
            Assert.That(nextBurst.FractionLost, Is.EqualTo(204));
            Assert.That(nextBurst.CumulativePacketsLost, Is.EqualTo(4));
            Assert.That(duplicate110.Status,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(duplicateInterval.FractionLost, Is.Zero);
            Assert.That(duplicateInterval.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(snapshot.ExpectedPacketCount, Is.EqualTo(11L));
            Assert.That(snapshot.ReceivedPacketCount, Is.EqualTo(8L));
            Assert.That(snapshot.ReorderedPacketCount, Is.EqualTo(4L));
            Assert.That(snapshot.DuplicatePacketCount, Is.EqualTo(1L));
        });
    }

    [Test]
    public void SequenceWrapLateArrivalAndDuplicateAdjustSignedLoss()
    {
        IRtpSequenceTracker sequenceTracker = CreateSequenceTracker();
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordPacket(sequenceTracker, statistics, 65534, 1L);
        RecordPacket(sequenceTracker, statistics, 65535, 2L);
        RtpSequenceUpdate wrapped =
            RecordPacket(sequenceTracker, statistics, 0, 3L);
        RecordPacket(sequenceTracker, statistics, 2, 4L);
        RtcpReceptionReport missingAcrossWrap =
            statistics.BuildRtcpReceptionReport(4L);

        RtpSequenceUpdate reordered =
            RecordPacket(sequenceTracker, statistics, 1, 5L);
        RtcpReceptionReport recovered =
            statistics.BuildRtcpReceptionReport(5L);
        RtpSequenceUpdate duplicate =
            RecordPacket(sequenceTracker, statistics, 2, 6L);
        RtcpReceptionReport negativeLoss =
            statistics.BuildRtcpReceptionReport(6L);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.ExtendedSequenceNumber, Is.EqualTo(65536U));
            Assert.That(missingAcrossWrap.ExtendedHighestSequenceNumber,
                Is.EqualTo(65538U));
            Assert.That(missingAcrossWrap.FractionLost, Is.EqualTo(51));
            Assert.That(missingAcrossWrap.CumulativePacketsLost, Is.EqualTo(1));
            Assert.That(reordered.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(recovered.FractionLost, Is.Zero);
            Assert.That(recovered.CumulativePacketsLost, Is.Zero);
            Assert.That(duplicate.Status, Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(negativeLoss.FractionLost, Is.Zero);
            Assert.That(negativeLoss.CumulativePacketsLost, Is.EqualTo(-1));
        });
    }

    [Test]
    public void RfcIntegerJitterTraceMatchesOneSixteenthFilter()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordTimedPacket(statistics, 1, 0U, 0L, 0L, 8000);
        uint first = statistics
            .GetReceptionStatisticsSnapshot(0L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 2, 160U, 160L, 160L, 8000);
        uint noVariation = statistics
            .GetReceptionStatisticsSnapshot(160L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 3, 320U, 340L, 340L, 8000);
        uint firstVariation = statistics
            .GetReceptionStatisticsSnapshot(340L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 4, 480U, 480L, 480L, 8000);
        uint secondVariation = statistics
            .GetReceptionStatisticsSnapshot(480L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 5, 640U, 680L, 680L, 8000);
        uint thirdVariation = statistics
            .GetReceptionStatisticsSnapshot(680L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 6, 800U, 800L, 800L, 8000);
        RtcpReceptionReport report = statistics.BuildRtcpReceptionReport(800L);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Zero);
            Assert.That(noVariation, Is.Zero);
            Assert.That(firstVariation, Is.EqualTo(1U));
            Assert.That(secondVariation, Is.EqualTo(2U));
            Assert.That(thirdVariation, Is.EqualTo(4U));
            Assert.That(report.InterarrivalJitter, Is.EqualTo(7U));
        });
    }

    [Test]
    public void JitterUsesArrivalOrderForReorderedAndDuplicatePackets()
    {
        IRtpSequenceTracker sequenceTracker = CreateSequenceTracker();
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordTimedSequencePacket(
            sequenceTracker,
            statistics,
            10,
            1000U,
            1000L,
            1L);
        RecordTimedSequencePacket(
            sequenceTracker,
            statistics,
            12,
            1320U,
            1320L,
            2L);
        RtpSequenceUpdate reordered = RecordTimedSequencePacket(
            sequenceTracker,
            statistics,
            11,
            1160U,
            1480L,
            3L);
        uint afterReorder = statistics
            .GetReceptionStatisticsSnapshot(3L)
            .InterarrivalJitter;
        RtpSequenceUpdate duplicate = RecordTimedSequencePacket(
            sequenceTracker,
            statistics,
            11,
            1160U,
            1640L,
            4L);
        uint afterDuplicate = statistics
            .GetReceptionStatisticsSnapshot(4L)
            .InterarrivalJitter;

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Status, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(afterReorder, Is.EqualTo(20U));
            Assert.That(duplicate.Status, Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(afterDuplicate, Is.EqualTo(28U));
        });
    }

    [Test]
    public void TimestampWrapAndClockRateChangeHaveSeparateJitterDomains()
    {
        IRtpReceptionStatistics statistics =
            new RtpReceptionStatistics(SourceIdentifier);

        RecordTimedPacket(
            statistics,
            1,
            0xFFFFFFF0U,
            1000L,
            1L,
            8000);
        RecordTimedPacket(statistics, 2, 0x00000010U, 1032L, 2L, 8000);
        uint afterWrap = statistics
            .GetReceptionStatisticsSnapshot(2L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 3, 0x00000030U, 1080L, 3L, 8000);
        uint beforeRateChange = statistics
            .GetReceptionStatisticsSnapshot(3L)
            .InterarrivalJitter;

        RecordTimedPacket(statistics, 4, 10000U, 500000L, 4L, 90000);
        uint afterRateChange = statistics
            .GetReceptionStatisticsSnapshot(4L)
            .InterarrivalJitter;
        RecordTimedPacket(statistics, 5, 10900U, 500450L, 5L, 90000);
        uint newRateVariation = statistics
            .GetReceptionStatisticsSnapshot(5L)
            .InterarrivalJitter;

        Assert.Multiple(() =>
        {
            Assert.That(afterWrap, Is.Zero);
            Assert.That(beforeRateChange, Is.EqualTo(1U));
            Assert.That(afterRateChange, Is.Zero);
            Assert.That(newRateVariation, Is.EqualTo(28U));
        });
    }

    [Test]
    public void SignedTwentyFourBitLossEdgesArePreservedAndClamped()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtpReceptionStatistics.ClampCumulativePacketLoss(
                8_388_607L), Is.EqualTo(8_388_607));
            Assert.That(RtpReceptionStatistics.ClampCumulativePacketLoss(
                8_388_608L), Is.EqualTo(8_388_607));
            Assert.That(RtpReceptionStatistics.ClampCumulativePacketLoss(
                -8_388_608L), Is.EqualTo(-8_388_608));
            Assert.That(RtpReceptionStatistics.ClampCumulativePacketLoss(
                -8_388_609L), Is.EqualTo(-8_388_608));
        });
    }

    static IRtpSequenceTracker CreateSequenceTracker()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = 1;
        configuration.JitterBuffer.MaximumDropout = 3000;
        configuration.JitterBuffer.MaximumMisorder = 100;
        return new RtpSequenceTracker(
            configuration,
            new FixedSequenceRandomSource());
    }

    static RtpSequenceUpdate RecordPacket(
        IRtpSequenceTracker sequenceTracker,
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        long arrivalTimeTicks)
    {
        RtpSequenceUpdate update =
            sequenceTracker.ProcessRtpSequenceNumber(sequenceNumber);
        RtpPacket packet = CreatePacket(sequenceNumber, 0U);
        RtpPacketContext context = CreateContext(
            arrivalTimeTicks,
            0L,
            8000);
        statistics.RecordReceivedRtpPacket(packet, context, update);
        return update;
    }

    static void RecordTimedPacket(
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint rtpTimestamp,
        long mediaClockTimestamp,
        long arrivalTimeTicks,
        int clockRate)
    {
        RtpPacket packet = CreatePacket(sequenceNumber, rtpTimestamp);
        RtpPacketContext context = CreateContext(
            arrivalTimeTicks,
            mediaClockTimestamp,
            clockRate);
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = RtpSequenceStatus.Accepted;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = sequenceNumber;
        update.BaseExtendedSequenceNumber = 1U;
        update.HighestExtendedSequenceNumber = sequenceNumber;
        update.Accepted = true;
        statistics.RecordReceivedRtpPacket(packet, context, update);
    }

    static RtpSequenceUpdate RecordTimedSequencePacket(
        IRtpSequenceTracker sequenceTracker,
        IRtpReceptionStatistics statistics,
        ushort sequenceNumber,
        uint rtpTimestamp,
        long mediaClockTimestamp,
        long arrivalTimeTicks)
    {
        RtpSequenceUpdate update =
            sequenceTracker.ProcessRtpSequenceNumber(sequenceNumber);
        RtpPacket packet = CreatePacket(sequenceNumber, rtpTimestamp);
        RtpPacketContext context = CreateContext(
            arrivalTimeTicks,
            mediaClockTimestamp,
            8000);
        statistics.RecordReceivedRtpPacket(packet, context, update);
        return update;
    }

    static RtpPacket CreatePacket(ushort sequenceNumber, uint timestamp)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = SourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.Payload = new byte[] { 1 };
        return packet;
    }

    static RtpPacketContext CreateContext(
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
