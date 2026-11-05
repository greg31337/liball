using rtp;

namespace rtp.Tests.Buffering;

public class RtpJitterBufferTests
{
    [Test]
    public void PacketsAreRetainedPerSourceInExtendedSequenceOrder()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(3));
        RtpPacket first = CreatePacket(10U, 65537U, new byte[] { 1, 2 });
        RtpPacketContext firstContext = CreateContext(10L);
        RtpSequenceUpdate firstSequence = CreateSequenceUpdate(65537U);

        buffer.AddRtpJitterBufferPacket(first, firstContext, firstSequence);
        first.Payload[0] = 99;
        firstContext.ArrivalTimeTicks = 99L;
        firstSequence.ExtendedSequenceNumber = 99U;
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65535U, new byte[] { 3 }),
            CreateContext(20L),
            CreateSequenceUpdate(65535U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65536U, new byte[] { 4, 5, 6 }),
            CreateContext(30L),
            CreateSequenceUpdate(65536U));
        RtpJitterBufferUpdate update = buffer.AddRtpJitterBufferPacket(
            CreatePacket(20U, 5U, new byte[] { 7 }),
            CreateContext(40L),
            CreateSequenceUpdate(5U));

        RtpPacketHeap firstSource = buffer.FindSourcePacketHeap(10U)!;
        RtpPacketHeap secondSource = buffer.FindSourcePacketHeap(20U)!;
        Assert.Multiple(() =>
        {
            Assert.That(buffer.sourcePacketHeaps, Has.Count.EqualTo(2));
            Assert.That(firstSource.Entries, Has.Count.EqualTo(3));
            Assert.That(firstSource.Entries[0].SequenceUpdate
                .ExtendedSequenceNumber, Is.EqualTo(65535U));
            Assert.That(firstSource.Entries[1].SequenceUpdate
                .ExtendedSequenceNumber, Is.EqualTo(65536U));
            Assert.That(firstSource.Entries[2].SequenceUpdate
                .ExtendedSequenceNumber, Is.EqualTo(65537U));
            Assert.That(firstSource.Entries[2].Packet.Payload,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(firstSource.Entries[2].Context.ArrivalTimeTicks,
                Is.EqualTo(10L));
            Assert.That(secondSource.Entries, Has.Count.EqualTo(1));
            Assert.That(update.Status, Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(update.BufferedPacketCount, Is.EqualTo(4));
            Assert.That(update.BufferedOctetCount, Is.EqualTo(7));
        });
    }

    [Test]
    public void DuplicateExtendedSequencesAreNotRetainedTwice()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(2));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 1 }),
            CreateContext(1L),
            CreateSequenceUpdate(10U));

        RtpJitterBufferUpdate repeated = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 2 }),
            CreateContext(2L),
            CreateSequenceUpdate(10U));
        RtpSequenceUpdate trackerDuplicate = CreateSequenceUpdate(10U);
        trackerDuplicate.Status = RtpSequenceStatus.Duplicate;
        trackerDuplicate.Accepted = false;
        RtpJitterBufferUpdate rejectedByTracker =
            buffer.TryAddRtpJitterBufferPacket(
                CreatePacket(10U, 10U, new byte[] { 3 }),
                CreateContext(3L),
                trackerDuplicate);

        RtpPacketHeap source = buffer.FindSourcePacketHeap(10U)!;
        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status,
                Is.EqualTo(RtpJitterBufferStatus.Duplicate));
            Assert.That(rejectedByTracker.Status,
                Is.EqualTo(RtpJitterBufferStatus.Duplicate));
            Assert.That(repeated.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(rejectedByTracker.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(source.Entries, Has.Count.EqualTo(1));
            Assert.That(source.Entries[0].Packet.Payload,
                Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void CapacityOverflowRetainsTheLowestSequenceCandidates()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(2));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 20U, new byte[] { 1, 2 }),
            CreateContext(1L),
            CreateSequenceUpdate(20U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 30U, new byte[] { 3, 4, 5 }),
            CreateContext(2L),
            CreateSequenceUpdate(30U));

        Assert.Throws<RtpBufferCapacityException>(() =>
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(10U, 10U, new byte[] { 6 }),
                CreateContext(3L),
                CreateSequenceUpdate(10U)));
        RtpPacketHeap source = buffer.FindSourcePacketHeap(10U)!;
        Assert.That(source.Entries[0].SequenceUpdate.ExtendedSequenceNumber,
            Is.EqualTo(20U));
        Assert.That(source.Entries[1].SequenceUpdate.ExtendedSequenceNumber,
            Is.EqualTo(30U));

        RtpJitterBufferUpdate earlier = buffer.TryAddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 6 }),
            CreateContext(3L),
            CreateSequenceUpdate(10U));
        RtpJitterBufferUpdate later = buffer.TryAddRtpJitterBufferPacket(
            CreatePacket(10U, 40U, new byte[] { 7, 8, 9, 10 }),
            CreateContext(4L),
            CreateSequenceUpdate(40U));

        Assert.Multiple(() =>
        {
            Assert.That(earlier.Status,
                Is.EqualTo(RtpJitterBufferStatus.Overflow));
            Assert.That(later.Status,
                Is.EqualTo(RtpJitterBufferStatus.Overflow));
            Assert.That(source.Entries, Has.Count.EqualTo(2));
            Assert.That(source.Entries[0].SequenceUpdate
                .ExtendedSequenceNumber, Is.EqualTo(10U));
            Assert.That(source.Entries[1].SequenceUpdate
                .ExtendedSequenceNumber, Is.EqualTo(20U));
            Assert.That(source.BufferedOctetCount, Is.EqualTo(3));
            Assert.That(later.BufferedPacketCount, Is.EqualTo(2));
            Assert.That(later.BufferedOctetCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ConfigurationAndPacketValidationPrecedeStateMutation()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(2);
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        configuration.Capacity = 1;
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 1U, new byte[] { 1 }),
            CreateContext(1L),
            CreateSequenceUpdate(1U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 2U, new byte[] { 2 }),
            CreateContext(2L),
            CreateSequenceUpdate(2U));
        RtpPacket invalidPacket = CreatePacket(
            10U,
            3U,
            new byte[] { 3 });
        invalidPacket.Version = 1;
        RtpSequenceUpdate mismatchedSequence = CreateSequenceUpdate(4U);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                buffer.AddRtpJitterBufferPacket(
                    invalidPacket,
                    CreateContext(3L),
                    CreateSequenceUpdate(3U)));
            Assert.Throws<RtpConfigurationException>(() =>
                buffer.TryAddRtpJitterBufferPacket(
                    CreatePacket(10U, 3U, new byte[] { 3 }),
                    CreateContext(3L),
                    mismatchedSequence));
            Assert.That(buffer.FindSourcePacketHeap(10U)?.Entries,
                Has.Count.EqualTo(2));
        });

        RtpJitterBufferUpdate reset = buffer.ResetRtpJitterBuffer();

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(RtpJitterBufferStatus.Reset));
            Assert.That(reset.BufferedPacketCount, Is.Zero);
            Assert.That(reset.BufferedOctetCount, Is.Zero);
            Assert.That(buffer.sourcePacketHeaps, Is.Empty);
        });
    }

    [Test]
    public void ContiguousPacketsReleaseImmediatelyInExtendedSequenceOrder()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(5));

        RtpJitterBufferUpdate first = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        RtpJitterBufferUpdate gap = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(102U, 100U));
        RtpJitterBufferUpdate filled = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 101U, new byte[] { 101 }),
            CreateContext(2L),
            CreateSequenceUpdateWithBase(101U, 100U));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(first.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(first.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(100));
            Assert.That(gap.Status, Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(gap.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(filled.Status,
                Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(filled.ReleasedPackets, Has.Length.EqualTo(2));
            Assert.That(filled.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(101));
            Assert.That(filled.ReleasedPackets[1].SequenceNumber,
                Is.EqualTo(102));
            Assert.That(filled.BufferedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void ContiguousReleaseCrossesTheSequenceNumberWrap()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(4));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65535U, new byte[] { 1 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(65535U, 65535U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65537U, new byte[] { 3 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(65537U, 65535U));

        RtpJitterBufferUpdate update = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65536U, new byte[] { 2 }),
            CreateContext(2L),
            CreateSequenceUpdateWithBase(65536U, 65535U));

        Assert.Multiple(() =>
        {
            Assert.That(update.ReleasedPackets, Has.Length.EqualTo(2));
            Assert.That(update.ReleasedPackets[0].SequenceNumber, Is.Zero);
            Assert.That(update.ReleasedPackets[1].SequenceNumber, Is.EqualTo(1));
            Assert.That(buffer.FindSourcePacketHeap(10U)?
                .NextReleaseExtendedSequenceNumber,
                Is.EqualTo(65538U));
        });
    }

    [Test]
    public void PollExpiresAGapAndClassifiesSubsequentPacketsAsLate()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(5);
        configuration.MaximumLatencyTicks = 10L;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 1 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 103U, new byte[] { 4 }),
            CreateContext(5L),
            CreateSequenceUpdateWithBase(103U, 100U));

        RtpJitterBufferUpdate waiting = buffer.PollRtpJitterBuffer(14L);
        RtpJitterBufferUpdate expired = buffer.PollRtpJitterBuffer(15L);
        RtpJitterBufferUpdate late = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 101U, new byte[] { 2 }),
            CreateContext(16L),
            CreateSequenceUpdateWithBase(101U, 100U));

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(waiting.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(expired.Status,
                Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(expired.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 101, 102 }));
            Assert.That(expired.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(expired.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(103));
            Assert.That(late.Status, Is.EqualTo(RtpJitterBufferStatus.Late));
            Assert.That(late.ReleasedPackets, Is.Empty);
            Assert.That(late.BufferedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void LatePacketPolicyCanReleaseFinalizedPackets()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(3);
        configuration.MaximumLatencyTicks = 1L;
        configuration.ReleaseLatePackets = true;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 10 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(10U, 10U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 12 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(12U, 10U));
        buffer.PollRtpJitterBuffer(2L);

        RtpJitterBufferUpdate late = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 11U, new byte[] { 11 }),
            CreateContext(3L),
            CreateSequenceUpdateWithBase(11U, 10U));

        Assert.Multiple(() =>
        {
            Assert.That(late.Status, Is.EqualTo(RtpJitterBufferStatus.Late));
            Assert.That(late.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(late.ReleasedPackets[0].Payload,
                Is.EqualTo(new byte[] { 11 }));
            Assert.That(late.BufferedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void RestartDiscontinuityDropsPendingStateAndStartsANewRun()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(4));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(102U, 100U));
        RtpSequenceUpdate restartSequence =
            CreateSequenceUpdateWithBase(50U, 50U);
        restartSequence.Status = RtpSequenceStatus.Restarted;

        RtpJitterBufferUpdate reset = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 50U, new byte[] { 50 }),
            CreateContext(2L),
            restartSequence);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Status, Is.EqualTo(RtpJitterBufferStatus.Reset));
            Assert.That(reset.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(reset.ReleasedPackets[0].SequenceNumber, Is.EqualTo(50));
            Assert.That(reset.BufferedPacketCount, Is.Zero);
            Assert.That(buffer.FindSourcePacketHeap(10U)?
                .NextReleaseExtendedSequenceNumber,
                Is.EqualTo(51U));
        });
    }

    [Test]
    public void DrainFinalizesEveryGapWhileFlushLeavesOpenGapsPending()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(5));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 103U, new byte[] { 103 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(103U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 105U, new byte[] { 105 }),
            CreateContext(2L),
            CreateSequenceUpdateWithBase(105U, 100U));

        RtpJitterBufferUpdate flushed = buffer.FlushRtpJitterBuffer();
        RtpJitterBufferUpdate drained = buffer.DrainRtpJitterBuffer();

        Assert.Multiple(() =>
        {
            Assert.That(flushed.Status,
                Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(flushed.ReleasedPackets, Is.Empty);
            Assert.That(flushed.BufferedPacketCount, Is.EqualTo(2));
            Assert.That(drained.Status,
                Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(drained.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 101, 102, 104 }));
            Assert.That(drained.ReleasedPackets, Has.Length.EqualTo(2));
            Assert.That(drained.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(103));
            Assert.That(drained.ReleasedPackets[1].SequenceNumber,
                Is.EqualTo(105));
            Assert.That(drained.BufferedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void ThrowingCapacityAddDoesNotAdvanceTimeOrExpireAGap()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(1);
        configuration.MaximumLatencyTicks = 1L;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 12 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(12U, 10U));

        Assert.Throws<RtpBufferCapacityException>(() =>
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(10U, 13U, new byte[] { 13 }),
                CreateContext(1L),
                CreateSequenceUpdateWithBase(13U, 10U)));
        RtpJitterBufferUpdate unchanged = buffer.PollRtpJitterBuffer(0L);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Status,
                Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(unchanged.ReleasedPackets, Is.Empty);
            Assert.That(unchanged.LostSequenceNumbers, Is.Empty);
            Assert.That(unchanged.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(unchanged.OverflowPacketCount, Is.Zero);
            Assert.That(buffer.FindSourcePacketHeap(10U)?.Entries[0]
                .SequenceUpdate.ExtendedSequenceNumber, Is.EqualTo(12U));
        });
    }

    [Test]
    public void OutcomeCountersAndOccupancyAreCumulativeSnapshots()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(2);
        configuration.MaximumLatencyTicks = 10L;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        RtpJitterBufferUpdate first = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 10 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(10U, 10U));
        RtpJitterBufferUpdate buffered = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 12, 12 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(12U, 10U));
        RtpJitterBufferUpdate duplicate = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 0 }),
            CreateContext(2L),
            CreateSequenceUpdateWithBase(12U, 10U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 13U, new byte[] { 13, 13, 13 }),
            CreateContext(3L),
            CreateSequenceUpdateWithBase(13U, 10U));
        RtpJitterBufferUpdate overflow =
            buffer.TryAddRtpJitterBufferPacket(
                CreatePacket(10U, 14U, new byte[] { 14 }),
                CreateContext(4L),
                CreateSequenceUpdateWithBase(14U, 10U));
        RtpJitterBufferUpdate expired = buffer.PollRtpJitterBuffer(11L);
        RtpJitterBufferUpdate late = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 11U, new byte[] { 11 }),
            CreateContext(12L),
            CreateSequenceUpdateWithBase(11U, 10U));

        Assert.Multiple(() =>
        {
            Assert.That(first.ReleasedPacketCount, Is.EqualTo(1));
            Assert.That(first.DuplicatePacketCount, Is.Zero);
            Assert.That(buffered.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(buffered.BufferedOctetCount, Is.EqualTo(2));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(duplicate.BufferedPacketCount, Is.EqualTo(1));
            Assert.That(overflow.Status,
                Is.EqualTo(RtpJitterBufferStatus.Overflow));
            Assert.That(overflow.OverflowPacketCount, Is.EqualTo(1));
            Assert.That(overflow.BufferedPacketCount, Is.EqualTo(2));
            Assert.That(overflow.BufferedOctetCount, Is.EqualTo(5));
            Assert.That(expired.ReleasedPacketCount, Is.EqualTo(3));
            Assert.That(expired.LostPacketCount, Is.EqualTo(1));
            Assert.That(expired.BufferedPacketCount, Is.Zero);
            Assert.That(late.ReleasedPacketCount, Is.EqualTo(3));
            Assert.That(late.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(late.LatePacketCount, Is.EqualTo(1));
            Assert.That(late.LostPacketCount, Is.EqualTo(1));
            Assert.That(late.OverflowPacketCount, Is.EqualTo(1));
            Assert.That(late.RecoveredPacketCount, Is.Zero);
        });
    }

    [Test]
    public void RepairedPacketEventsAreIndependentAndResetWithCounters()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(3));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(102U, 100U));
        RtpPacketContext repairedContext = CreateContext(2L);
        repairedContext.IsRepaired = true;

        RtpJitterBufferUpdate recovered = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 101U, new byte[] { 101 }),
            repairedContext,
            CreateSequenceUpdateWithBase(101U, 100U));
        recovered.ReleasedPackets[0].Payload[0] = 0;
        bool recoveredEventIsIndependent =
            recovered.RecoveredPackets[0].Payload[0] == 101;
        recovered.RecoveredPackets[0].Payload[0] = 0;
        recovered.RecoveredPacketCount = 99L;
        RtpJitterBufferUpdate fresh = buffer.PollRtpJitterBuffer(2L);
        RtpSequenceUpdate duplicateSequence =
            CreateSequenceUpdateWithBase(101U, 100U);
        duplicateSequence.Status = RtpSequenceStatus.Duplicate;
        duplicateSequence.Accepted = false;
        RtpJitterBufferUpdate duplicate =
            buffer.TryAddRtpJitterBufferPacket(
                CreatePacket(10U, 101U, new byte[] { 1 }),
                CreateContext(3L),
                duplicateSequence);
        RtpJitterBufferUpdate reset = buffer.ResetRtpJitterBuffer();

        Assert.Multiple(() =>
        {
            Assert.That(recovered.ReleasedPackets, Has.Length.EqualTo(2));
            Assert.That(recovered.RecoveredPackets, Has.Length.EqualTo(1));
            Assert.That(recoveredEventIsIndependent, Is.True);
            Assert.That(recovered.ReleasedPackets[0],
                Is.Not.SameAs(recovered.RecoveredPackets[0]));
            Assert.That(fresh.ReleasedPackets, Is.Empty);
            Assert.That(fresh.RecoveredPackets, Is.Empty);
            Assert.That(fresh.ReleasedPacketCount, Is.EqualTo(3));
            Assert.That(fresh.RecoveredPacketCount, Is.EqualTo(1));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(duplicate.RecoveredPacketCount, Is.EqualTo(1));
            Assert.That(duplicate.RecoveredPackets, Is.Empty);
            Assert.That(reset.ReleasedPacketCount, Is.Zero);
            Assert.That(reset.DuplicatePacketCount, Is.Zero);
            Assert.That(reset.LatePacketCount, Is.Zero);
            Assert.That(reset.LostPacketCount, Is.Zero);
            Assert.That(reset.OverflowPacketCount, Is.Zero);
            Assert.That(reset.RecoveredPacketCount, Is.Zero);
        });
    }

    [Test]
    public void RepairedPacketAfterLossFinalizationIsReleasedAsLateRecovery()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(3);
        configuration.MaximumLatencyTicks = 2L;
        configuration.ReleaseLatePackets = true;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 10 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(10U, 10U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 12 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(12U, 10U));
        RtpJitterBufferUpdate expired = buffer.PollRtpJitterBuffer(3L);
        RtpPacketContext repairedContext = CreateContext(4L);
        repairedContext.IsRepaired = true;

        RtpJitterBufferUpdate recovered = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 11U, new byte[] { 11 }),
            repairedContext,
            CreateSequenceUpdateWithBase(11U, 10U));

        Assert.Multiple(() =>
        {
            Assert.That(expired.Status,
                Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(expired.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(expired.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(expired.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(12));
            Assert.That(recovered.Status,
                Is.EqualTo(RtpJitterBufferStatus.Late));
            Assert.That(recovered.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(recovered.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(11));
            Assert.That(recovered.RecoveredPackets, Has.Length.EqualTo(1));
            Assert.That(recovered.RecoveredPackets[0].SequenceNumber,
                Is.EqualTo(11));
            Assert.That(recovered.ReleasedPackets[0],
                Is.Not.SameAs(recovered.RecoveredPackets[0]));
            Assert.That(recovered.ReleasedPacketCount, Is.EqualTo(3));
            Assert.That(recovered.LatePacketCount, Is.EqualTo(1));
            Assert.That(recovered.LostPacketCount, Is.EqualTo(1));
            Assert.That(recovered.RecoveredPacketCount, Is.EqualTo(1));
            Assert.That(recovered.BufferedPacketCount, Is.Zero);
        });
    }

    [Test]
    public void SourceGapExpiryDuplicateAndDiscontinuityRemainIndependent()
    {
        RtpJitterBufferConfiguration configuration = CreateConfiguration(4);
        configuration.MaximumLatencyTicks = 5L;
        RtpJitterBuffer buffer = new RtpJitterBuffer(configuration);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(102U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(20U, 200U, new byte[] { 200 }),
            CreateContext(2L),
            CreateSequenceUpdateWithBase(200U, 200U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(20U, 201U, new byte[] { 201 }),
            CreateContext(3L),
            CreateSequenceUpdateWithBase(201U, 200U));
        RtpSequenceUpdate duplicateSequence =
            CreateSequenceUpdateWithBase(201U, 200U);
        duplicateSequence.Status = RtpSequenceStatus.Duplicate;
        duplicateSequence.Accepted = false;
        RtpJitterBufferUpdate duplicate = buffer.AddRtpJitterBufferPacket(
            CreatePacket(20U, 201U, new byte[] { 0 }),
            CreateContext(4L),
            duplicateSequence);

        RtpJitterBufferUpdate expired = buffer.PollRtpJitterBuffer(6L);
        RtpJitterBufferUpdate secondSourceContinues =
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(20U, 202U, new byte[] { 202 }),
                CreateContext(7L),
                CreateSequenceUpdateWithBase(202U, 200U));
        RtpSequenceUpdate restartSequence =
            CreateSequenceUpdateWithBase(50U, 50U);
        restartSequence.Status = RtpSequenceStatus.Restarted;
        RtpJitterBufferUpdate firstSourceRestart =
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(10U, 50U, new byte[] { 50 }),
                CreateContext(8L),
                restartSequence);
        RtpJitterBufferUpdate afterOtherSourceRestart =
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(20U, 203U, new byte[] { 203 }),
                CreateContext(9L),
                CreateSequenceUpdateWithBase(203U, 200U));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Status,
                Is.EqualTo(RtpJitterBufferStatus.Duplicate));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(expired.Status,
                Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(expired.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 101 }));
            Assert.That(expired.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(expired.ReleasedPackets[0].SynchronizationSource,
                Is.EqualTo(10U));
            Assert.That(expired.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(102));
            Assert.That(secondSourceContinues.Status,
                Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(secondSourceContinues.ReleasedPackets[0]
                .SynchronizationSource, Is.EqualTo(20U));
            Assert.That(secondSourceContinues.ReleasedPackets[0]
                .SequenceNumber, Is.EqualTo(202));
            Assert.That(firstSourceRestart.Status,
                Is.EqualTo(RtpJitterBufferStatus.Reset));
            Assert.That(firstSourceRestart.ReleasedPackets[0]
                .SynchronizationSource, Is.EqualTo(10U));
            Assert.That(afterOtherSourceRestart.Status,
                Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(afterOtherSourceRestart.ReleasedPackets[0]
                .SynchronizationSource, Is.EqualTo(20U));
            Assert.That(afterOtherSourceRestart.ReleasedPackets[0]
                .SequenceNumber, Is.EqualTo(203));
            Assert.That(buffer.FindSourcePacketHeap(10U)?
                .NextReleaseExtendedSequenceNumber, Is.EqualTo(51U));
            Assert.That(buffer.FindSourcePacketHeap(20U)?
                .NextReleaseExtendedSequenceNumber, Is.EqualTo(204U));
            Assert.That(afterOtherSourceRestart.ReleasedPacketCount,
                Is.EqualTo(7));
            Assert.That(afterOtherSourceRestart.DuplicatePacketCount,
                Is.EqualTo(1));
            Assert.That(afterOtherSourceRestart.LostPacketCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void NackDelayProvidesReorderGraceAndArrivalCancelsRequests()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(
            CreateNackConfiguration(5, 5L, 10L, 3, 50L, 100L));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        RtpJitterBufferUpdate gap = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 103U, new byte[] { 103 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(103U, 100U));
        RtpJitterBufferUpdate waiting = buffer.PollRtpJitterBuffer(5L);
        RtpJitterBufferUpdate reordered = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 101U, new byte[] { 101 }),
            CreateContext(5L),
            CreateSequenceUpdateWithBase(101U, 100U));
        RtpJitterBufferUpdate due = buffer.PollRtpJitterBuffer(6L);
        RtpJitterBufferUpdate filled = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(7L),
            CreateSequenceUpdateWithBase(102U, 100U));
        RtpJitterBufferUpdate afterRetryTime =
            buffer.PollRtpJitterBuffer(16L);

        Assert.Multiple(() =>
        {
            Assert.That(gap.RepairRequests, Is.Empty);
            Assert.That(waiting.RepairRequests, Is.Empty);
            Assert.That(reordered.RepairRequests, Is.Empty);
            Assert.That(due.RepairRequests, Has.Length.EqualTo(1));
            Assert.That(due.RepairRequests[0].Kind,
                Is.EqualTo(RtpRepairKind.Retransmission));
            Assert.That(due.RepairRequests[0].SourceIdentifier,
                Is.EqualTo(10U));
            Assert.That(due.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 102 }));
            Assert.That(due.RepairRequests[0].AttemptCount, Is.EqualTo(1));
            Assert.That(due.RepairRequests[0].RequestTimeTicks,
                Is.EqualTo(6L));
            Assert.That(due.RepairRequests[0].DeadlineTicks,
                Is.EqualTo(51L));
            Assert.That(filled.ReleasedPackets, Has.Length.EqualTo(2));
            Assert.That(filled.RepairRequests, Is.Empty);
            Assert.That(afterRetryTime.RepairRequests, Is.Empty);
            Assert.That(buffer.FindSourcePacketHeap(10U)?.MissingPackets,
                Is.Empty);
        });
    }

    [Test]
    public void NackRetriesRespectCadenceCountAndRetryPeriod()
    {
        RtpJitterBuffer countLimited = new RtpJitterBuffer(
            CreateNackConfiguration(3, 2L, 3L, 3, 20L, 100L));
        countLimited.AddRtpJitterBufferPacket(
            CreatePacket(10U, 10U, new byte[] { 10 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(10U, 10U));
        countLimited.AddRtpJitterBufferPacket(
            CreatePacket(10U, 12U, new byte[] { 12 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(12U, 10U));

        RtpJitterBufferUpdate beforeDelay =
            countLimited.PollRtpJitterBuffer(2L);
        RtpJitterBufferUpdate first =
            countLimited.PollRtpJitterBuffer(3L);
        first.RepairRequests[0].SequenceNumbers[0] = 0;
        RtpJitterBufferUpdate beforeRetry =
            countLimited.PollRtpJitterBuffer(5L);
        RtpJitterBufferUpdate second =
            countLimited.PollRtpJitterBuffer(6L);
        RtpJitterBufferUpdate third =
            countLimited.PollRtpJitterBuffer(9L);
        RtpJitterBufferUpdate afterCount =
            countLimited.PollRtpJitterBuffer(12L);

        RtpJitterBuffer periodLimited = new RtpJitterBuffer(
            CreateNackConfiguration(3, 2L, 3L, 10, 7L, 100L));
        periodLimited.AddRtpJitterBufferPacket(
            CreatePacket(20U, 20U, new byte[] { 20 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(20U, 20U));
        periodLimited.AddRtpJitterBufferPacket(
            CreatePacket(20U, 22U, new byte[] { 22 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(22U, 20U));
        RtpJitterBufferUpdate periodFirst =
            periodLimited.PollRtpJitterBuffer(3L);
        RtpJitterBufferUpdate periodSecond =
            periodLimited.PollRtpJitterBuffer(6L);
        RtpJitterBufferUpdate atPeriodEnd =
            periodLimited.PollRtpJitterBuffer(8L);
        RtpJitterBufferUpdate afterPeriod =
            periodLimited.PollRtpJitterBuffer(9L);

        Assert.Multiple(() =>
        {
            Assert.That(beforeDelay.RepairRequests, Is.Empty);
            Assert.That(first.RepairRequests[0].AttemptCount, Is.EqualTo(1));
            Assert.That(beforeRetry.RepairRequests, Is.Empty);
            Assert.That(second.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(second.RepairRequests[0].AttemptCount, Is.EqualTo(2));
            Assert.That(third.RepairRequests[0].AttemptCount, Is.EqualTo(3));
            Assert.That(afterCount.RepairRequests, Is.Empty);
            Assert.That(periodFirst.RepairRequests[0].AttemptCount,
                Is.EqualTo(1));
            Assert.That(periodSecond.RepairRequests[0].AttemptCount,
                Is.EqualTo(2));
            Assert.That(atPeriodEnd.RepairRequests, Is.Empty);
            Assert.That(afterPeriod.RepairRequests, Is.Empty);
        });
    }

    [Test]
    public void MissingPacketRequestsPreserveSourceAndSequenceWrapOrder()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(
            CreateNackConfiguration(4, 0L, 2L, 2, 10L, 100L));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65534U, new byte[] { 1 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(65534U, 65534U));

        RtpJitterBufferUpdate gap = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65537U, new byte[] { 4 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(65537U, 65534U));

        Assert.Multiple(() =>
        {
            Assert.That(gap.RepairRequests, Has.Length.EqualTo(2));
            Assert.That(gap.RepairRequests[0].SourceIdentifier,
                Is.EqualTo(10U));
            Assert.That(gap.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 65535 }));
            Assert.That(gap.RepairRequests[1].SequenceNumbers,
                Is.EqualTo(new ushort[] { 0 }));
            Assert.That(gap.RepairRequests[0].DeadlineTicks,
                Is.EqualTo(11L));
        });
    }

    [Test]
    public void LossAndDiscontinuityCancelPendingMissingPacketState()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(
            CreateNackConfiguration(4, 1L, 1L, 5, 20L, 3L));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 102U, new byte[] { 102 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(102U, 100U));
        RtpJitterBufferUpdate request = buffer.PollRtpJitterBuffer(2L);
        RtpJitterBufferUpdate lost = buffer.PollRtpJitterBuffer(4L);
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 105U, new byte[] { 105 }),
            CreateContext(5L),
            CreateSequenceUpdateWithBase(105U, 100U));
        RtpSequenceUpdate restartSequence =
            CreateSequenceUpdateWithBase(50U, 50U);
        restartSequence.Status = RtpSequenceStatus.Restarted;
        RtpJitterBufferUpdate restart = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 50U, new byte[] { 50 }),
            CreateContext(6L),
            restartSequence);
        RtpJitterBufferUpdate afterRestart =
            buffer.PollRtpJitterBuffer(20L);

        Assert.Multiple(() =>
        {
            Assert.That(request.RepairRequests, Has.Length.EqualTo(1));
            Assert.That(lost.Status, Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(lost.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 101 }));
            Assert.That(lost.RepairRequests, Is.Empty);
            Assert.That(restart.Status,
                Is.EqualTo(RtpJitterBufferStatus.Reset));
            Assert.That(restart.RepairRequests, Is.Empty);
            Assert.That(afterRestart.RepairRequests, Is.Empty);
            Assert.That(buffer.FindSourcePacketHeap(10U)?.MissingPackets,
                Is.Empty);
        });
    }

    [Test]
    public void PartialRecoveryRetriesCoalesceSuppressAndFinalizeRemainingLoss()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(
            CreateNackConfiguration(6, 2L, 3L, 3, 20L, 15L));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 100U, new byte[] { 100 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(100U, 100U));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 104U, new byte[] { 104 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(104U, 100U));

        RtpJitterBufferUpdate beforeDelay =
            buffer.PollRtpJitterBuffer(2L);
        RtpJitterBufferUpdate firstRequests =
            buffer.PollRtpJitterBuffer(3L);
        RtpJitterBufferUpdate partialRecovery =
            buffer.AddRtpJitterBufferPacket(
                CreatePacket(10U, 102U, new byte[] { 102 }),
                CreateContext(4L),
                CreateSequenceUpdateWithBase(102U, 100U));
        RtpJitterBufferUpdate beforeRetry =
            buffer.PollRtpJitterBuffer(5L);
        RtpJitterBufferUpdate retry =
            buffer.PollRtpJitterBuffer(6L);

        RtcpSchedulerConfiguration schedulerConfiguration =
            new RtcpSchedulerConfiguration();
        schedulerConfiguration.SessionBandwidthBitsPerSecond = 80000.0;
        schedulerConfiguration.FeedbackEnabled = true;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            schedulerConfiguration,
            new SecureRtpRandomSource());
        int feedbackIndex = 0;
        while (feedbackIndex < retry.FeedbackRequests.Length)
        {
            scheduler.QueueRtcpFeedback(retry.FeedbackRequests[feedbackIndex]);
            feedbackIndex = feedbackIndex + 1;
        }

        int coalescedRequestCount = scheduler.pendingRequests.Count;
        RtcpFeedbackPacket equivalentFeedback =
            scheduler.pendingRequests[0].Feedback!.Copy();
        ushort[] retriedSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                equivalentFeedback.GenericNack!);
        scheduler.ObserveRtcpFeedback(equivalentFeedback, 6L);
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = 2;
        snapshot.SenderCount = 1;
        snapshot.AverageRtcpPacketSize = 100;
        snapshot.NextRtcpSendTimeTicks = 100L;
        RtpFeedbackDecision suppressed =
            scheduler.PollRtcpFeedback(snapshot, 6L);

        RtpJitterBufferUpdate finalized =
            buffer.PollRtpJitterBuffer(16L);
        RtpJitterBufferUpdate afterFinalization =
            buffer.PollRtpJitterBuffer(20L);

        Assert.Multiple(() =>
        {
            Assert.That(beforeDelay.RepairRequests, Is.Empty);
            Assert.That(firstRequests.RepairRequests, Has.Length.EqualTo(3));
            Assert.That(firstRequests.RepairRequests.Select(
                value => value.SequenceNumbers[0]),
                Is.EqualTo(new ushort[] { 101, 102, 103 }));
            Assert.That(firstRequests.RepairRequests.Select(
                value => value.AttemptCount),
                Is.EqualTo(new int[] { 1, 1, 1 }));
            Assert.That(partialRecovery.RepairRequests, Is.Empty);
            Assert.That(beforeRetry.RepairRequests, Is.Empty);
            Assert.That(retry.RepairRequests, Has.Length.EqualTo(2));
            Assert.That(retry.RepairRequests.Select(
                value => value.SequenceNumbers[0]),
                Is.EqualTo(new ushort[] { 101, 103 }));
            Assert.That(retry.RepairRequests.Select(
                value => value.AttemptCount),
                Is.EqualTo(new int[] { 2, 2 }));
            Assert.That(coalescedRequestCount, Is.EqualTo(1));
            Assert.That(retriedSequenceNumbers,
                Is.EqualTo(new ushort[] { 101, 103 }));
            Assert.That(suppressed.ShouldSend, Is.False);
            Assert.That(suppressed.Suppressed, Is.True);
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(finalized.Status,
                Is.EqualTo(RtpJitterBufferStatus.Lost));
            Assert.That(finalized.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 101, 103 }));
            Assert.That(finalized.ReleasedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 102, 104 }));
            Assert.That(finalized.RepairRequests, Is.Empty);
            Assert.That(finalized.FeedbackRequests, Is.Empty);
            Assert.That(afterFinalization.RepairRequests, Is.Empty);
            Assert.That(afterFinalization.FeedbackRequests, Is.Empty);
            Assert.That(buffer.FindSourcePacketHeap(10U)?.MissingPackets,
                Is.Empty);
        });
    }

    [Test]
    public void GenericNackFeedbackIsCanonicalAndSchedulerCompatible()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(
            CreateNackConfiguration(4, 0L, 2L, 2, 10L, 100L));
        buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65534U, new byte[] { 1 }),
            CreateContext(0L),
            CreateSequenceUpdateWithBase(65534U, 65534U));
        RtpJitterBufferUpdate gap = buffer.AddRtpJitterBufferPacket(
            CreatePacket(10U, 65537U, new byte[] { 4 }),
            CreateContext(1L),
            CreateSequenceUpdateWithBase(65537U, 65534U));
        RtpJitterBufferUpdate copiedGap = gap.Copy();
        gap.FeedbackRequests[0].Feedback!.GenericNack!.Entries[0]
            .PacketIdentifier = 7;

        RtcpSchedulerConfiguration schedulerConfiguration =
            new RtcpSchedulerConfiguration();
        schedulerConfiguration.SessionBandwidthBitsPerSecond = 80000.0;
        schedulerConfiguration.FeedbackEnabled = true;
        RtcpFeedbackScheduler scheduler = new RtcpFeedbackScheduler(
            schedulerConfiguration,
            new SecureRtpRandomSource());
        scheduler.QueueRtcpFeedback(copiedGap.FeedbackRequests[0]);
        scheduler.QueueRtcpFeedback(copiedGap.FeedbackRequests[1]);
        int coalescedRequestCount = scheduler.pendingRequests.Count;
        RtcpFeedbackPacket mergedFeedback =
            scheduler.pendingRequests[0].Feedback!.Copy();
        ushort[] mergedSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                mergedFeedback.GenericNack!);
        scheduler.ObserveRtcpFeedback(mergedFeedback, 1L);
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = 2;
        snapshot.SenderCount = 1;
        snapshot.AverageRtcpPacketSize = 100;
        snapshot.NextRtcpSendTimeTicks = 100L;
        RtpFeedbackDecision decision =
            scheduler.PollRtcpFeedback(snapshot, 1L);

        Assert.Multiple(() =>
        {
            Assert.That(gap.RepairRequests, Has.Length.EqualTo(2));
            Assert.That(gap.FeedbackRequests, Has.Length.EqualTo(2));
            Assert.That(copiedGap.FeedbackRequests[0].Feedback?.PacketType,
                Is.EqualTo(RtcpFeedbackPacketType.TransportLayer));
            Assert.That(copiedGap.FeedbackRequests[0].Feedback?
                .TransportFormat,
                Is.EqualTo(RtcpTransportFeedbackFormat.GenericNack));
            Assert.That(copiedGap.FeedbackRequests[0].Feedback?
                .MediaSourceIdentifier, Is.EqualTo(10U));
            Assert.That(copiedGap.FeedbackRequests[0].Feedback?
                .GenericNack?.Entries[0].PacketIdentifier,
                Is.EqualTo(65535));
            Assert.That(copiedGap.FeedbackRequests[0]
                .EarliestSendTimeTicks, Is.EqualTo(1L));
            Assert.That(copiedGap.FeedbackRequests[0]
                .ExpirationTimeTicks, Is.EqualTo(11L));
            Assert.That(copiedGap.FeedbackRequests[0]
                .PreferredTimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(copiedGap.FeedbackRequests[0].AllowSuppression,
                Is.True);
            Assert.That(coalescedRequestCount, Is.EqualTo(1));
            Assert.That(scheduler.pendingRequests, Is.Empty);
            Assert.That(mergedSequenceNumbers,
                Is.EqualTo(new ushort[] { 65535, 0 }));
            Assert.That(mergedFeedback.GenericNack?.Entries,
                Has.Length.EqualTo(1));
            Assert.That(mergedFeedback.GenericNack?.Entries[0]
                .LostPacketBitmask, Is.EqualTo(1));
            Assert.That(decision.ShouldSend, Is.False);
            Assert.That(decision.Suppressed, Is.True);
        });
    }

    [Test]
    public void JitterBufferRejectsEveryMalformedPacketContextAndSequenceUpdate()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(2));
        RtpPacket packet = CreatePacket(10U, 10U, new byte[] { 1 });
        RtpPacketContext context = CreateContext(0L);
        RtpSequenceUpdate update = CreateSequenceUpdate(10U);

        Assert.Throws<RtpPacketFormatException>(() => buffer
            .AddRtpJitterBufferPacket(null!, context, update));
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, null!, update));
        context.ArrivalTimeTicks = -1L;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, update));
        context.ArrivalTimeTicks = 0L;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, null!));

        update.SequenceNumber = 11;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, update));
        update.SequenceNumber = 10;
        update.ExtendedSequenceNumber = 11U;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, update));
        update.ExtendedSequenceNumber = 10U;
        update.Accepted = false;
        update.Status = RtpSequenceStatus.LargeJump;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, update));
        update.Accepted = true;
        update.Status = RtpSequenceStatus.Duplicate;
        Assert.Throws<RtpConfigurationException>(() => buffer
            .AddRtpJitterBufferPacket(packet, context, update));

        Assert.Throws<RtpConfigurationException>(() =>
            buffer.ValidateJitterBufferTime(-1L));
        buffer.hasCurrentTime = true;
        buffer.currentTimeTicks = 2L;
        Assert.Throws<RtpSessionException>(() =>
            buffer.ValidateJitterBufferChronology(1L));
    }

    [Test]
    public void JitterBufferReleaseHelpersCoverAbsentExhaustedAndMaximumSequences()
    {
        RtpJitterBuffer buffer = new RtpJitterBuffer(CreateConfiguration(2));
        RtpPacketHeap heap = new RtpPacketHeap();
        RtpSequenceUpdate update = CreateSequenceUpdate(10U);

        Assert.That(buffer.IsPacketBeforeReleaseSequence(heap, update), Is.False);
        Assert.That(buffer.IsPacketAtReleaseSequence(heap, update), Is.False);
        heap.HasReleaseSequence = true;
        heap.ReleaseSequenceExhausted = true;
        Assert.That(buffer.IsPacketBeforeReleaseSequence(heap, update), Is.True);
        Assert.That(buffer.IsPacketAtReleaseSequence(heap, update), Is.False);

        heap.ReleaseSequenceExhausted = false;
        heap.NextReleaseExtendedSequenceNumber = uint.MaxValue;
        buffer.AdvanceSourceReleaseSequence(heap);
        Assert.That(heap.ReleaseSequenceExhausted, Is.True);
        Assert.That(buffer.AddSaturatedJitterBufferCount(long.MaxValue, 1L),
            Is.EqualTo(long.MaxValue));
    }

    private static RtpJitterBufferConfiguration CreateConfiguration(int capacity)
    {
        RtpJitterBufferConfiguration configuration =
            new RtpJitterBufferConfiguration();
        configuration.Capacity = capacity;
        return configuration;
    }

    private static RtpJitterBufferConfiguration CreateNackConfiguration(
        int capacity,
        long delayTicks,
        long retryIntervalTicks,
        int maximumRetryCount,
        long maximumRetryPeriodTicks,
        long maximumLatencyTicks)
    {
        RtpJitterBufferConfiguration configuration =
            CreateConfiguration(capacity);
        configuration.NackDelayTicks = delayTicks;
        configuration.NackRetryIntervalTicks = retryIntervalTicks;
        configuration.MaximumNackRetryCount = maximumRetryCount;
        configuration.MaximumNackRetryPeriodTicks = maximumRetryPeriodTicks;
        configuration.MaximumLatencyTicks = maximumLatencyTicks;
        return configuration;
    }

    private static RtpPacket CreatePacket(
        uint sourceIdentifier,
        uint extendedSequenceNumber,
        byte[] payload)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = (ushort)extendedSequenceNumber;
        packet.PayloadType = 96;
        packet.Payload = payload;
        return packet;
    }

    private static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        return context;
    }

    private static RtpSequenceUpdate CreateSequenceUpdate(
        uint extendedSequenceNumber)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = RtpSequenceStatus.Accepted;
        update.SequenceNumber = (ushort)extendedSequenceNumber;
        update.ExtendedSequenceNumber = extendedSequenceNumber;
        update.Accepted = true;
        return update;
    }

    private static RtpSequenceUpdate CreateSequenceUpdateWithBase(
        uint extendedSequenceNumber,
        uint baseExtendedSequenceNumber)
    {
        RtpSequenceUpdate update = CreateSequenceUpdate(extendedSequenceNumber);
        update.BaseExtendedSequenceNumber = baseExtendedSequenceNumber;
        if (extendedSequenceNumber > baseExtendedSequenceNumber)
        {
            update.HighestExtendedSequenceNumber = extendedSequenceNumber;
        }
        else
        {
            update.HighestExtendedSequenceNumber = baseExtendedSequenceNumber;
        }

        return update;
    }
}
