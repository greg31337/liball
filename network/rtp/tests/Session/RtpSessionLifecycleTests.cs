using System.Text;
using rtp;

namespace rtp.Tests.Session;

public class RtpSessionLifecycleTests
{
    [Test]
    public void SmallSessionGoodbyeIsImmediateOwnedAndClosesMediaWork()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        byte[] reason = Encoding.UTF8.GetBytes("finished");
        session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(0L));

        RtpSessionUpdate update = session.RequestSessionGoodbye(reason);
        reason[0] = (byte)'X';
        RtcpCompoundPacket compound =
            update.OutboundControlPackets.Single();
        RtcpGoodbye goodbye = compound.Packets.Single(
            value => value.Type == RtcpPacketType.Goodbye).Goodbye!;

        Assert.Multiple(() =>
        {
            Assert.That(update.ControlScheduleDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(update.ControlScheduleDecisions[0].IsGoodbye,
                Is.True);
            Assert.That(update.ControlScheduleDecisions[0].CompoundPacket,
                Is.Not.Null);
            Assert.That(compound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(compound.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(goodbye.Sources,
                Is.EqualTo(update.Snapshot?.LocalSourceIdentifiers));
            Assert.That(goodbye.Reason,
                Is.EqualTo(Encoding.UTF8.GetBytes("finished")));
            Assert.That(update.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Goodbye));
            Assert.That(session.PollSession().ControlScheduleDecisions,
                Is.Empty);
            Assert.Throws<RtpSessionException>(() =>
                session.PrepareOutboundRtpPacket(
                    CreatePacket(0U, 0, 0U),
                    CreateContext(0L)));
        });
    }

    [Test]
    public void LargeSessionGoodbyeUsesBackoffAndCompletesOnPoll()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        int sourceIndex = 0;
        while (sourceIndex < 50)
        {
            session.ingress.remoteSourceTable.RecordRemoteRtcpActivity(
                (uint)(1000 + sourceIndex),
                CreateContext(0L));
            sourceIndex = sourceIndex + 1;
        }

        RtpSessionUpdate pending = session.RequestSessionGoodbye([]);
        clock.MonotonicTimeTicks = pending.ControlScheduleDecisions[0]
            .ScheduledTimeTicks;

        RtpSessionUpdate completed = session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(pending.ControlScheduleDecisions.Single().ShouldSend,
                Is.False);
            Assert.That(pending.OutboundControlPackets, Is.Empty);
            Assert.That(completed.ControlScheduleDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(completed.ControlScheduleDecisions[0].IsGoodbye,
                Is.True);
            Assert.That(completed.OutboundControlPackets,
                Has.Length.EqualTo(1));
            Assert.That(completed.OutboundControlPackets[0].Packets.Last().Type,
                Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(completed.Events.Any(
                value => value.Type == RtpSessionEventType.Goodbye),
                Is.True);
            Assert.That(session.poller.goodbyeCompleted, Is.True);
        });
    }

    [Test]
    public void InvalidGoodbyeDoesNotChangeSessionLifecycle()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);

        Assert.Throws<RtpConfigurationException>(() =>
            session.RequestSessionGoodbye(new byte[] { 0xC3, 0x28 }));
        RtpSessionUpdate outbound = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(1L));

        Assert.Multiple(() =>
        {
            Assert.That(session.poller.goodbyeRequested, Is.False);
            Assert.That(session.poller.goodbyeCompleted, Is.False);
            Assert.That(outbound.OutboundPackets, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ResetClearsSourcesFeedbackAndSenderStateThenAllowsReuse()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpScheduler.FeedbackEnabled = true;
        FixedClock clock = new FixedClock();
        IncrementingRandomSource randomSource =
            new IncrementingRandomSource();
        RtpSession session = CreateSession(
            configuration,
            clock,
            randomSource);
        uint originalLocalSource = session.GetSessionSnapshot()
            .LocalSourceIdentifiers.Single();
        session.ProcessInboundRtpDatagram(
            new RtpPacketCodec(configuration).EncodeRtpPacketToArray(
                CreatePacket(500U, 10, 1000U)),
            CreateContext(0L));
        session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(1L));
        session.RequestSessionFeedback(CreateFeedbackRequest(500U));

        session.ResetSession();
        RtpSessionSnapshot reset = session.GetSessionSnapshot();
        RtpSessionUpdate poll = session.PollSession();
        RtpSessionUpdate reused = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(1L));

        Assert.Multiple(() =>
        {
            Assert.That(reset.Sources, Has.Length.EqualTo(1));
            Assert.That(reset.Sources[0].IsLocal, Is.True);
            Assert.That(reset.Sources[0].IsSender, Is.False);
            Assert.That(reset.LocalSourceIdentifiers.Single(),
                Is.Not.EqualTo(originalLocalSource));
            Assert.That(reset.MemberCount, Is.EqualTo(1));
            Assert.That(reset.SenderCount, Is.Zero);
            Assert.That(poll.FeedbackDecisions, Is.Empty);
            Assert.That(reused.OutboundPackets, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CollisionReselectionLoopAndResetPreserveSourceIdentity()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        IncrementingRandomSource randomSource =
            new IncrementingRandomSource();
        RtpPayloadTypeRegistry payloadRegistry =
            CreatePayloadRegistry();
        RtpSession session = new RtpSession(
            configuration,
            clock,
            randomSource,
            payloadRegistry,
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            null);
        RtpSessionUpdate outbound = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(0L));
        uint originalLocalSource = outbound.OutboundPackets.Single()
            .SynchronizationSource;
        RtpPacket collidingPacket = CreatePacket(
            originalLocalSource,
            50,
            5000U);
        RtpPacketContext remoteContext = CreateContext(1L);
        remoteContext.TransportIdentity = "remote-path";
        clock.MonotonicTimeTicks = 1L;

        RtpSessionUpdate collision = session.ProcessInboundRtpDatagram(
            new RtpPacketCodec(configuration).EncodeRtpPacketToArray(
                collidingPacket),
            remoteContext);
        RtpSessionEvent collisionEvent = collision.Events.Single(
            value => value.Type == RtpSessionEventType.Collision);
        RtpSessionSnapshot afterCollision = session.GetSessionSnapshot();
        uint replacementLocalSource = afterCollision.LocalSourceIdentifiers
            .Single();
        RtpLocalSourceState replacementState = session.ingress
            .localSourceTable.FindLocalSourceState(
                replacementLocalSource)!;
        RtpPacketContext loopSendContext = CreateContext(2L);
        loopSendContext.TransportIdentity = "new-local-path";
        RtpPacket loopPacket = new RtpOutboundPacketBuilder(
            payloadRegistry,
            new RtpHeaderExtensionRegistry()).BuildOutboundPacket(
                replacementState,
                CreatePacket(0U, 0, 0U),
                loopSendContext);
        clock.MonotonicTimeTicks = 2L;
        RtpSessionUpdate loop = session.ProcessInboundRtpDatagram(
            new RtpPacketCodec(configuration).EncodeRtpPacketToArray(
                loopPacket),
            loopSendContext);
        RtpSourceSnapshot? loopRemoteSource = session.ingress
            .remoteSourceTable.FindRemoteSourceSnapshot(
                replacementLocalSource);

        session.ResetSession();
        RtpSessionSnapshot reset = session.GetSessionSnapshot();
        RtpPacketContext postResetContext = CreateContext(3L);
        postResetContext.TransportIdentity = "remote-path";
        clock.MonotonicTimeTicks = 3L;
        RtpSessionUpdate postReset = session.ProcessInboundRtpDatagram(
            new RtpPacketCodec(configuration).EncodeRtpPacketToArray(
                CreatePacket(originalLocalSource, 51, 6000U)),
            postResetContext);
        RtpSessionSnapshot final = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(originalLocalSource, Is.EqualTo(100U));
            Assert.That(collision.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(collisionEvent.SourceIdentifier,
                Is.EqualTo(originalLocalSource));
            Assert.That(collisionEvent.Message, Does.Contain("collision"));
            Assert.That(collisionEvent.ControlPacket?.Type,
                Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(collisionEvent.ControlPacket?.Goodbye?.Sources,
                Is.EqualTo(new uint[] { originalLocalSource }));
            Assert.That(replacementLocalSource, Is.EqualTo(101U));
            Assert.That(afterCollision.LocalSourceIdentifiers,
                Is.EqualTo(new uint[] { replacementLocalSource }));
            Assert.That(afterCollision.Sources.Select(
                value => value.SourceIdentifier),
                Is.EqualTo(new uint[]
                {
                    replacementLocalSource,
                    originalLocalSource
                }));
            Assert.That(loop.AcceptedPackets, Is.Empty);
            Assert.That(loop.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Collision));
            Assert.That(loop.Events[0].Message, Does.Contain("loop"));
            Assert.That(loopRemoteSource, Is.Null);
            Assert.That(reset.Sources, Has.Length.EqualTo(1));
            Assert.That(reset.LocalSourceIdentifiers,
                Is.EqualTo(new uint[] { 102U }));
            Assert.That(reset.Sources[0].IsLocal, Is.True);
            Assert.That(postReset.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(postReset.Events,
                Has.None.Matches<RtpSessionEvent>(
                    value => value.Type == RtpSessionEventType.Collision));
            Assert.That(final.LocalSourceIdentifiers,
                Is.EqualTo(new uint[] { 102U }));
            Assert.That(final.Sources.Single(
                value => !value.IsLocal).SourceIdentifier,
                Is.EqualTo(originalLocalSource));
        });
    }

    [Test]
    public void ConfigurationReplacementIsAtomicAndReturnsOwnedState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        session.ingress.remoteSourceTable.RecordRemoteRtcpActivity(
            500U,
            CreateContext(0L));
        RtpSessionSnapshot before = session.GetSessionSnapshot();
        RtpSessionConfiguration invalid = CreateConfiguration();
        invalid.RtcpScheduler.SessionBandwidthBitsPerSecond = 0.0;

        Assert.Throws<RtpConfigurationException>(() =>
            session.UpdateSessionConfiguration(invalid));
        RtpSessionSnapshot afterInvalid = session.GetSessionSnapshot();

        RtpSessionConfiguration replacement = CreateConfiguration();
        replacement.CanonicalName = "replacement@example";
        replacement.JitterBuffer.Capacity = 8;
        RtpSessionUpdate changed = session.UpdateSessionConfiguration(
            replacement);
        replacement.CanonicalName = "mutated@example";
        replacement.JitterBuffer.Capacity = 1;
        RtpSessionSnapshot afterValid = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(afterInvalid.LocalSourceIdentifiers,
                Is.EqualTo(before.LocalSourceIdentifiers));
            Assert.That(afterInvalid.Sources.Any(
                value => value.SourceIdentifier == 500U),
                Is.True);
            Assert.That(changed.Events.Single().Configuration?.CanonicalName,
                Is.EqualTo("replacement@example"));
            Assert.That(changed.Snapshot?.Configuration.JitterBuffer.Capacity,
                Is.EqualTo(8));
            Assert.That(afterValid.Configuration.CanonicalName,
                Is.EqualTo("replacement@example"));
            Assert.That(afterValid.Configuration.JitterBuffer.Capacity,
                Is.EqualTo(8));
            Assert.That(afterValid.Sources, Has.Length.EqualTo(1));
            Assert.That(afterValid.MemberCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SnapshotGraphsAreIndependentFromLiveSessionState()
    {
        RtpSession session = CreateSession(
            CreateConfiguration(),
            new FixedClock());
        RtpSessionSnapshot first = session.GetSessionSnapshot();
        uint localSourceIdentifier = first.LocalSourceIdentifiers[0];
        first.Configuration.CanonicalName = "changed";
        first.LocalSourceIdentifiers[0] = 0U;
        first.Sources[0].CanonicalName = "changed";

        RtpSessionSnapshot second = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(second.Configuration.CanonicalName,
                Is.EqualTo("local@example"));
            Assert.That(second.LocalSourceIdentifiers[0],
                Is.EqualTo(localSourceIdentifier));
            Assert.That(second.Sources[0].CanonicalName,
                Is.EqualTo("local@example"));
            Assert.That(second.Sources, Is.Not.SameAs(first.Sources));
        });
    }

    [Test]
    public void FlushReturnsQueuedWorkAndCurrentSnapshotOnce()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        RtpSessionEvent queuedEvent = new RtpSessionEvent();
        queuedEvent.Type = RtpSessionEventType.Source;
        queuedEvent.Message = "flush";
        RtpRepairRequest queuedRepair = new RtpRepairRequest();
        queuedRepair.Kind = RtpRepairKind.ForwardErrorCorrection;
        queuedRepair.SourceIdentifier = 500U;
        queuedRepair.SequenceNumbers = new ushort[] { 10 };
        queuedRepair.RequestTimeTicks = 0L;
        queuedRepair.DeadlineTicks = 10L;
        session.poller.QueueSessionEvent(queuedEvent);
        session.poller.QueueSessionRepairRequest(queuedRepair);
        queuedEvent.Message = "changed";
        queuedRepair.SequenceNumbers[0] = 99;

        RtpSessionUpdate first = session.FlushSession();
        RtpSessionUpdate second = session.FlushSession();

        Assert.Multiple(() =>
        {
            Assert.That(first.Events.Single().Message, Is.EqualTo("flush"));
            Assert.That(first.RepairRequests.Single().SequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(first.Snapshot, Is.Not.Null);
            Assert.That(second.Events, Is.Empty);
            Assert.That(second.RepairRequests, Is.Empty);
        });
    }

    [Test]
    public void RepairedPacketsReenterIngressAndUpdateOwnedStatistics()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        RtpRepairResult repairResult = new RtpRepairResult();
        repairResult.Success = true;
        repairResult.Kind = RtpRepairKind.ForwardErrorCorrection;
        repairResult.RepairedPackets = new RtpPacket[]
        {
            CreatePacket(500U, 10, 1000U)
        };
        repairResult.Data = new byte[] { 1 };
        clock.MonotonicTimeTicks = 1L;

        RtpSessionUpdate update = session.ApplySessionRepairResult(
            repairResult,
            CreateContext(1L));
        repairResult.RepairedPackets[0].Payload[0] = 99;
        repairResult.Data[0] = 99;
        RtpSourceSnapshot remote = update.Snapshot!.Sources.Single(
            value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(update.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(update.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(update.RepairResults.Single().Data,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(update.AcceptedPackets[0].Payload,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(update.Events.Last().Type,
                Is.EqualTo(RtpSessionEventType.Repair));
            Assert.That(remote.ReceptionStatistics?.RepairedPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.RecoveredPacketCount,
                Is.EqualTo(1L));
        });
    }

    [TestCase(RtpRepairKind.Retransmission)]
    [TestCase(RtpRepairKind.Redundancy)]
    [TestCase(RtpRepairKind.ForwardErrorCorrection)]
    public void RepairHookSuccessReentersOnceAndFailureIsIsolated(
        RtpRepairKind kind)
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        TraceRepairDecoder decoder = new TraceRepairDecoder();
        decoder.RepairedPacket = CreatePacket(500U, 10, 1000U);
        RtpSession session = new RtpSession(
            configuration,
            clock,
            new IncrementingRandomSource(),
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            decoder);
        RtpRepairRequest successfulRequest = CreateRepairRequest(
            kind,
            10,
            1L);

        clock.MonotonicTimeTicks = 1L;
        RtpRepairResult successfulResult = session.ingress.repairCoordinator
            .DecodeRepair(successfulRequest, new byte[] { 1, 2 });
        RtpSessionUpdate successful = session.ApplySessionRepairResult(
            successfulResult,
            CreateContext(1L));

        decoder.Success = false;
        decoder.RepairedPacket = null;
        RtpRepairRequest failedRequest = CreateRepairRequest(
            kind,
            11,
            2L);
        clock.MonotonicTimeTicks = 2L;
        RtpRepairResult failedResult = session.ingress.repairCoordinator
            .DecodeRepair(failedRequest, new byte[] { 3, 4 });
        RtpSessionUpdate failed = session.ApplySessionRepairResult(
            failedResult,
            CreateContext(2L));
        RtpSourceSnapshot remote = failed.Snapshot!.Sources.Single(
            value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(decoder.DecodeCallCount, Is.EqualTo(2));
            Assert.That(decoder.RequestKinds,
                Is.EqualTo(new RtpRepairKind[] { kind, kind }));
            Assert.That(decoder.RequestedSequenceNumbers,
                Is.EqualTo(new ushort[] { 10, 11 }));
            Assert.That(successfulResult.Success, Is.True);
            Assert.That(successfulResult.Kind, Is.EqualTo(kind));
            Assert.That(successful.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(successful.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(successful.RepairResults.Single().Kind,
                Is.EqualTo(kind));
            Assert.That(successful.Events.Last().RepairKind,
                Is.EqualTo(kind));
            Assert.That(failedResult.Success, Is.False);
            Assert.That(failedResult.Kind, Is.EqualTo(kind));
            Assert.That(failedResult.UnrepairedSequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(failed.AcceptedPackets, Is.Empty);
            Assert.That(failed.ReleasedPackets, Is.Empty);
            Assert.That(failed.RepairResults.Single().Success, Is.False);
            Assert.That(failed.Events.Single().Message,
                Is.EqualTo("simulated repair failure"));
            Assert.That(remote.ReceptionStatistics?.ReceivedPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.RepairedPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.RecoveredPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.DiscardedPacketCount,
                Is.Zero);
        });
    }

    static RtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock)
    {
        return CreateSession(
            configuration,
            clock,
            new IncrementingRandomSource());
    }

    static RtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock,
        IRtpRandomSource randomSource)
    {
        return new RtpSession(
            configuration,
            clock,
            randomSource,
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 64;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
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

    static RtpPacket CreatePacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Marker = true;
        packet.Payload = new byte[] { 1, 2, 3 };
        return packet;
    }

    static RtpPacketContext CreateContext(long operationTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = "path";
        context.ClockRate = 90000;
        context.MediaClockTimestamp = operationTimeTicks;
        return context;
    }

    static RtpFeedbackRequest CreateFeedbackRequest(
        uint mediaSourceIdentifier)
    {
        RtcpPictureLossIndication pictureLoss =
            new RtcpPictureLossIndication();
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat =
            RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.PictureLossIndication = pictureLoss;
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = feedback;
        request.PreferredTimingMode = RtcpFeedbackTimingMode.Immediate;
        request.ExpirationTimeTicks = 100L;
        return request;
    }

    static RtpRepairRequest CreateRepairRequest(
        RtpRepairKind kind,
        ushort sequenceNumber,
        long requestTimeTicks)
    {
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = kind;
        request.SourceIdentifier = 500U;
        request.SequenceNumbers = new ushort[] { sequenceNumber };
        request.AttemptCount = 1;
        request.RequestTimeTicks = requestTimeTicks;
        request.DeadlineTicks = requestTimeTicks + 10L;
        return request;
    }

    private class FixedClock : IRtpClock
    {
        public long MonotonicTimeTicks;

        public long GetMonotonicTimeTicks()
        {
            return MonotonicTimeTicks;
        }

        public long GetUtcTimeTicks()
        {
            return MonotonicTimeTicks;
        }

        public ulong GetNtpTimestamp()
        {
            return (ulong)MonotonicTimeTicks;
        }
    }

    private class IncrementingRandomSource : IRtpRandomSource
    {
        public uint NextSourceIdentifier = 100U;

        public uint CreateSynchronizationSource()
        {
            uint sourceIdentifier = NextSourceIdentifier;
            NextSourceIdentifier = NextSourceIdentifier + 1U;
            return sourceIdentifier;
        }

        public ushort CreateSequenceNumber()
        {
            return 10;
        }

        public uint CreateTimestamp()
        {
            return 1000U;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }

    private class TraceRepairDecoder : IRtpRepairDecoder
    {
        public bool Success = true;
        public int DecodeCallCount;
        public RtpPacket? RepairedPacket;
        public List<RtpRepairKind> RequestKinds =
            new List<RtpRepairKind>();
        public List<ushort> RequestedSequenceNumbers =
            new List<ushort>();

        public RtpRepairResult DecodeRtpRepair(
            RtpRepairRequest request,
            ReadOnlySpan<byte> repairData)
        {
            DecodeCallCount = DecodeCallCount + 1;
            RequestKinds.Add(request.Kind);
            RequestedSequenceNumbers.Add(request.SequenceNumbers[0]);
            RtpRepairResult result = new RtpRepairResult();
            result.Success = Success;
            result.Kind = request.Kind;
            result.Data = repairData.ToArray();
            if (Success && RepairedPacket != null)
            {
                result.RepairedPackets =
                    new RtpPacket[] { RepairedPacket.Copy() };
            }
            else
            {
                result.UnrepairedSequenceNumbers =
                    (ushort[])request.SequenceNumbers.Clone();
                result.ErrorMessage = "simulated repair failure";
            }

            return result;
        }
    }
}
