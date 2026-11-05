using rtp;

namespace rtp.Tests.Buffering;

public class RtpRepairCoordinatorTests
{
    [TestCase(RtpRepairKind.Retransmission)]
    [TestCase(RtpRepairKind.Redundancy)]
    [TestCase(RtpRepairKind.ForwardErrorCorrection)]
    [TestCase(RtpRepairKind.ApplicationDefined)]
    public void EncoderDispatchesEveryRepairKindWithOwnedInputs(
        RtpRepairKind kind)
    {
        TestRepairHook hook = new TestRepairHook();
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(hook, hook);
        RtpRepairRequest request = CreateRequest(kind);
        RtpPacket sourcePacket = CreatePacket(10, 1);

        RtpRepairResult result = coordinator.EncodeRepair(
            request,
            new RtpPacket[] { sourcePacket });
        hook.EncodedRequest!.SequenceNumbers[0] = 99;
        hook.EncodedPackets![0].Payload[0] = 99;
        hook.EncoderResult!.Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(hook.EncodeCallCount, Is.EqualTo(1));
            Assert.That(hook.EncodedRequest.Kind, Is.EqualTo(kind));
            Assert.That(request.SequenceNumbers, Is.EqualTo(new ushort[] { 10 }));
            Assert.That(sourcePacket.Payload, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.Data, Is.EqualTo(new byte[] { 7, 8 }));
        });
    }

    [TestCase(RtpRepairKind.Retransmission)]
    [TestCase(RtpRepairKind.Redundancy)]
    [TestCase(RtpRepairKind.ForwardErrorCorrection)]
    [TestCase(RtpRepairKind.ApplicationDefined)]
    public void DecoderDispatchesEveryRepairKindWithOwnedResults(
        RtpRepairKind kind)
    {
        TestRepairHook hook = new TestRepairHook();
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(hook, hook);
        RtpRepairRequest request = CreateRequest(kind);
        byte[] repairData = new byte[] { 3, 4, 5 };

        RtpRepairResult result = coordinator.DecodeRepair(request, repairData);
        hook.DecodedRequest!.Data[0] = 99;
        hook.DecoderResult!.RepairedPackets[0].Payload[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(hook.DecodeCallCount, Is.EqualTo(1));
            Assert.That(hook.DecodedRequest.Kind, Is.EqualTo(kind));
            Assert.That(request.Data, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 3, 4, 5 }));
        });
    }

    [Test]
    public void MissingHooksReturnRecoverableFailures()
    {
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(null, null);
        RtpRepairRequest request = CreateRequest(
            RtpRepairKind.ApplicationDefined);

        RtpRepairResult encoded = coordinator.EncodeRepair(
            request,
            new RtpPacket[] { CreatePacket(10, 1) });
        RtpRepairResult decoded = coordinator.DecodeRepair(
            request,
            new byte[] { 1 });
        request.SequenceNumbers[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Kind,
                Is.EqualTo(RtpRepairKind.ApplicationDefined));
            Assert.That(encoded.UnrepairedSequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(encoded.ErrorMessage,
                Is.EqualTo("No RTP repair encoder is available."));
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.UnrepairedSequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(decoded.ErrorMessage,
                Is.EqualTo("No RTP repair decoder is available."));
        });
    }

    [Test]
    public void InvalidRequestsAndHookResultsAreRejected()
    {
        TestRepairHook hook = new TestRepairHook();
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(hook, hook);
        RtpRepairRequest invalidTime = CreateRequest(
            RtpRepairKind.Retransmission);
        invalidTime.DeadlineTicks = invalidTime.RequestTimeTicks - 1;
        RtpRepairRequest invalidKind = CreateRequest(
            (RtpRepairKind)99);

        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.EncodeRepair(
                invalidTime,
                new RtpPacket[] { CreatePacket(10, 1) }));
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.DecodeRepair(invalidKind, new byte[] { 1 }));
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.EncodeRepair(
                CreateRequest(RtpRepairKind.Retransmission),
                null!));

        hook.ReturnedKind = RtpRepairKind.Redundancy;
        Assert.Throws<RtpSessionException>(() =>
            coordinator.EncodeRepair(
                CreateRequest(RtpRepairKind.Retransmission),
                new RtpPacket[] { CreatePacket(10, 1) }));

        hook.ReturnNull = true;
        Assert.Throws<RtpSessionException>(() =>
            coordinator.DecodeRepair(
                CreateRequest(RtpRepairKind.Redundancy),
                new byte[] { 1 }));
    }

    [Test]
    public void RepairedRecoveredDuplicateAndLateOutcomesRemainDistinct()
    {
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(null, null);
        TestSequenceTracker sequenceTracker = new TestSequenceTracker();
        RtpReceptionStatistics statistics =
            new RtpReceptionStatistics(100U);
        RtpJitterBuffer jitterBuffer = new RtpJitterBuffer(
            new RtpJitterBufferConfiguration());

        sequenceTracker.NextUpdate = CreateSequenceUpdate(
            10,
            10U,
            10U,
            RtpSequenceStatus.Accepted,
            true);
        RtpPacketContext acceptedContext = CreateContext(0L);
        RtpSessionEvent accepted = coordinator.ProcessRepairedPacket(
            CreatePacket(10, 1),
            acceptedContext,
            RtpRepairKind.Retransmission,
            sequenceTracker,
            statistics,
            jitterBuffer);

        sequenceTracker.NextUpdate = CreateSequenceUpdate(
            10,
            10U,
            10U,
            RtpSequenceStatus.Duplicate,
            false);
        RtpSessionEvent duplicate = coordinator.ProcessRepairedPacket(
            CreatePacket(10, 2),
            CreateContext(1L),
            RtpRepairKind.Redundancy,
            sequenceTracker,
            statistics,
            jitterBuffer);

        sequenceTracker.NextUpdate = CreateSequenceUpdate(
            9,
            10U,
            10U,
            RtpSequenceStatus.Reordered,
            true);
        RtpPacketContext lateContext = CreateContext(2L);
        RtpSessionEvent late = coordinator.ProcessRepairedPacket(
            CreatePacket(9, 3),
            lateContext,
            RtpRepairKind.ForwardErrorCorrection,
            sequenceTracker,
            statistics,
            jitterBuffer);

        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(2L);

        Assert.Multiple(() =>
        {
            Assert.That(sequenceTracker.ProcessCallCount, Is.EqualTo(3));
            Assert.That(snapshot.ReceivedPacketCount, Is.EqualTo(3));
            Assert.That(snapshot.RepairedPacketCount, Is.EqualTo(3));
            Assert.That(snapshot.RecoveredPacketCount, Is.EqualTo(2));
            Assert.That(snapshot.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(snapshot.ReorderedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.LatePacketCount, Is.EqualTo(1));
            Assert.That(accepted.JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(duplicate.SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(duplicate.JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Duplicate));
            Assert.That(late.PacketContext?.IsRepaired, Is.True);
            Assert.That(late.PacketContext?.IsLate, Is.True);
            Assert.That(late.JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Late));
            Assert.That(late.RepairKind,
                Is.EqualTo(RtpRepairKind.ForwardErrorCorrection));
            Assert.That(acceptedContext.IsRepaired, Is.False);
            Assert.That(lateContext.IsLate, Is.False);
            Assert.That(jitterBuffer.recoveredPacketCount, Is.EqualTo(1));
            Assert.That(jitterBuffer.duplicatePacketCount, Is.EqualTo(1));
            Assert.That(jitterBuffer.latePacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectedRepairIsRecordedOnceWithoutEnteringJitterBuffer()
    {
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(null, null);
        TestSequenceTracker sequenceTracker = new TestSequenceTracker();
        sequenceTracker.NextUpdate = CreateSequenceUpdate(
            30,
            0U,
            0U,
            RtpSequenceStatus.LargeJump,
            false);
        RtpReceptionStatistics statistics =
            new RtpReceptionStatistics(100U);
        RtpJitterBuffer jitterBuffer = new RtpJitterBuffer(
            new RtpJitterBufferConfiguration());

        RtpSessionEvent discarded = coordinator.ProcessRepairedPacket(
            CreatePacket(30, 1),
            CreateContext(0L),
            RtpRepairKind.ApplicationDefined,
            sequenceTracker,
            statistics,
            jitterBuffer);
        RtpReceptionStatisticsSnapshot snapshot =
            statistics.GetReceptionStatisticsSnapshot(0L);

        Assert.Multiple(() =>
        {
            Assert.That(sequenceTracker.ProcessCallCount, Is.EqualTo(1));
            Assert.That(snapshot.ReceivedPacketCount, Is.Zero);
            Assert.That(snapshot.DiscardedPacketCount, Is.EqualTo(1));
            Assert.That(snapshot.RepairedPacketCount, Is.EqualTo(1));
            Assert.That(jitterBuffer.sourcePacketHeaps, Is.Empty);
            Assert.That(discarded.SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.LargeJump));
            Assert.That(discarded.JitterBufferUpdate, Is.Null);
            Assert.That(discarded.Message, Is.Not.Empty);
        });
    }

    [Test]
    public void InvalidRepairIsRejectedBeforeSequenceOrStatisticsMutation()
    {
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(null, null);
        TestSequenceTracker sequenceTracker = new TestSequenceTracker();
        sequenceTracker.NextUpdate = CreateSequenceUpdate(
            10,
            10U,
            10U,
            RtpSequenceStatus.Accepted,
            true);
        RtpReceptionStatistics statistics =
            new RtpReceptionStatistics(100U);
        RtpJitterBuffer jitterBuffer = new RtpJitterBuffer(
            new RtpJitterBufferConfiguration());
        RtpPacket invalid = CreatePacket(10, 1);
        invalid.Version = 1;

        Assert.Throws<RtpPacketFormatException>(() =>
            coordinator.ProcessRepairedPacket(
                invalid,
                CreateContext(0L),
                RtpRepairKind.Retransmission,
                sequenceTracker,
                statistics,
                jitterBuffer));

        Assert.Multiple(() =>
        {
            Assert.That(sequenceTracker.ProcessCallCount, Is.Zero);
            Assert.That(statistics.repairedPacketCount, Is.Zero);
            Assert.That(statistics.receivedPacketCount, Is.Zero);
            Assert.That(jitterBuffer.sourcePacketHeaps, Is.Empty);
        });
    }

    [Test]
    public void RepairValidationRejectsEveryMalformedRequestResultAndDependency()
    {
        RtpRepairCoordinator coordinator = new RtpRepairCoordinator(null, null);
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(null!));

        RtpRepairRequest request = CreateRequest(RtpRepairKind.Retransmission);
        request.SequenceNumbers = null!;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(request));
        request = CreateRequest(RtpRepairKind.Retransmission);
        request.Data = null!;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(request));
        request = CreateRequest(RtpRepairKind.Retransmission);
        request.AttemptCount = -1;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(request));
        request = CreateRequest(RtpRepairKind.Retransmission);
        request.RequestTimeTicks = -1L;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(request));
        request = CreateRequest(RtpRepairKind.Retransmission);
        request.DeadlineTicks = -1L;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.CopyValidatedRequest(request));

        Assert.Throws<RtpSessionException>(() =>
            coordinator.CopyValidatedResult(null!, RtpRepairKind.Retransmission));
        RtpRepairResult result = new RtpRepairResult();
        result.Kind = RtpRepairKind.Retransmission;
        result.RepairedPackets = null!;
        Assert.Throws<RtpSessionException>(() =>
            coordinator.CopyValidatedResult(result, RtpRepairKind.Retransmission));
        result = new RtpRepairResult();
        result.Kind = RtpRepairKind.Retransmission;
        result.UnrepairedSequenceNumbers = null!;
        Assert.Throws<RtpSessionException>(() =>
            coordinator.CopyValidatedResult(result, RtpRepairKind.Retransmission));
        result = new RtpRepairResult();
        result.Kind = RtpRepairKind.Retransmission;
        result.Data = null!;
        Assert.Throws<RtpSessionException>(() =>
            coordinator.CopyValidatedResult(result, RtpRepairKind.Retransmission));

        TestSequenceTracker tracker = new TestSequenceTracker();
        RtpReceptionStatistics statistics = new RtpReceptionStatistics(100U);
        RtpJitterBuffer jitterBuffer = new RtpJitterBuffer(
            new RtpJitterBufferConfiguration());
        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() => coordinator
                .ValidateRepairedPacketDependencies(null!, statistics, jitterBuffer));
            Assert.Throws<RtpConfigurationException>(() => coordinator
                .ValidateRepairedPacketDependencies(tracker, null!, jitterBuffer));
            Assert.Throws<RtpConfigurationException>(() => coordinator
                .ValidateRepairedPacketDependencies(tracker, statistics, null!));
        });

        RtpPacket packet = CreatePacket(10, 1);
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.ValidateRepairedPacketInput(packet, null!));
        RtpPacketContext context = CreateContext(-1L);
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.ValidateRepairedPacketInput(packet, context));
        context = CreateContext(0L);
        context.ClockRate = 0;
        Assert.Throws<RtpConfigurationException>(() =>
            coordinator.ValidateRepairedPacketInput(packet, context));
        Assert.Throws<RtpSessionException>(() =>
            coordinator.ValidateRepairedSequenceUpdate(packet, null!));
        RtpSequenceUpdate update = CreateSequenceUpdate(
            11, 11U, 11U, RtpSequenceStatus.Accepted, true);
        Assert.Throws<RtpSessionException>(() =>
            coordinator.ValidateRepairedSequenceUpdate(packet, update));
    }

    static RtpRepairRequest CreateRequest(RtpRepairKind kind)
    {
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = kind;
        request.SourceIdentifier = 100U;
        request.SequenceNumbers = new ushort[] { 10 };
        request.AttemptCount = 1;
        request.RequestTimeTicks = 20L;
        request.DeadlineTicks = 30L;
        request.Data = new byte[] { 1, 2 };
        return request;
    }

    static RtpPacket CreatePacket(ushort sequenceNumber, byte payload)
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = 96;
        packet.SequenceNumber = sequenceNumber;
        packet.SynchronizationSource = 100U;
        packet.Payload = new byte[] { payload };
        return packet;
    }

    static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.ClockRate = 8000;
        context.MediaClockTimestamp = arrivalTimeTicks;
        return context;
    }

    static RtpSequenceUpdate CreateSequenceUpdate(
        ushort sequenceNumber,
        uint baseExtendedSequenceNumber,
        uint highestExtendedSequenceNumber,
        RtpSequenceStatus status,
        bool accepted)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = status;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = sequenceNumber;
        update.BaseExtendedSequenceNumber = baseExtendedSequenceNumber;
        update.HighestExtendedSequenceNumber = highestExtendedSequenceNumber;
        update.Accepted = accepted;
        return update;
    }

    private class TestSequenceTracker : IRtpSequenceTracker
    {
        public int ProcessCallCount;
        public RtpSequenceUpdate NextUpdate = new RtpSequenceUpdate();

        public RtpSequenceUpdate ProcessRtpSequenceNumber(
            ushort sequenceNumber)
        {
            ProcessCallCount = ProcessCallCount + 1;
            return NextUpdate.Copy();
        }

        public ushort AdvanceRtpSequenceNumber()
        {
            return 0;
        }

        public RtpSequenceUpdate GetRtpSequenceState()
        {
            return NextUpdate.Copy();
        }

        public RtpSequenceUpdate ResetRtpSequenceTracker(
            ushort sequenceNumber)
        {
            return NextUpdate.Copy();
        }
    }

    private class TestRepairHook : IRtpRepairEncoder, IRtpRepairDecoder
    {
        public int EncodeCallCount;
        public int DecodeCallCount;
        public RtpRepairRequest? EncodedRequest;
        public RtpPacket[]? EncodedPackets;
        public RtpRepairRequest? DecodedRequest;
        public RtpRepairResult? EncoderResult;
        public RtpRepairResult? DecoderResult;
        public RtpRepairKind ReturnedKind;
        public bool ReturnNull;

        public RtpRepairResult EncodeRtpRepair(
            RtpRepairRequest request,
            RtpPacket[] sourcePackets)
        {
            EncodeCallCount = EncodeCallCount + 1;
            EncodedRequest = request;
            EncodedPackets = sourcePackets;
            if (ReturnNull)
            {
                return null!;
            }

            EncoderResult = new RtpRepairResult();
            EncoderResult.Success = true;
            EncoderResult.Kind = GetReturnedKind(request.Kind);
            EncoderResult.Data = new byte[] { 7, 8 };
            return EncoderResult;
        }

        public RtpRepairResult DecodeRtpRepair(
            RtpRepairRequest request,
            ReadOnlySpan<byte> repairData)
        {
            DecodeCallCount = DecodeCallCount + 1;
            DecodedRequest = request;
            if (ReturnNull)
            {
                return null!;
            }

            DecoderResult = new RtpRepairResult();
            DecoderResult.Success = true;
            DecoderResult.Kind = GetReturnedKind(request.Kind);
            RtpPacket repairedPacket = new RtpPacket();
            repairedPacket.Payload = repairData.ToArray();
            DecoderResult.RepairedPackets =
                new RtpPacket[] { repairedPacket };
            return DecoderResult;
        }

        RtpRepairKind GetReturnedKind(RtpRepairKind requestedKind)
        {
            if (ReturnedKind == RtpRepairKind.Retransmission)
            {
                return requestedKind;
            }

            return ReturnedKind;
        }
    }
}
