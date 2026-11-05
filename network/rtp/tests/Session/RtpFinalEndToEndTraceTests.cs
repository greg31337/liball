using System.Text;
using rtp;

namespace rtp.Tests.Session;

public class RtpFinalEndToEndTraceTests
{
    [Test]
    public void WrapReorderLossSuppressionAndRetransmissionRecoveryRemainCoherent()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.ReducedSize);
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.RtcpScheduler.ReducedSizeEnabled = true;
        configuration.JitterBuffer.NackDelayTicks = 5L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 10L;
        configuration.JitterBuffer.MaximumNackRetryCount = 1;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        configuration.JitterBuffer.MaximumLatencyTicks = 1_000_000L;
        TraceClock clock = new TraceClock();
        RtpSession session = CreateSession(configuration, clock, null);
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);

        RtpSessionUpdate beforeWrap = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(500U, 65534, 1000U),
            0L,
            "remote-media");
        RtpSessionUpdate afterWrap = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(500U, 0, 3000U),
            10_000L,
            "remote-media");
        RtpSessionUpdate reordered = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(500U, 65535, 2000U),
            20_000L,
            "remote-media");
        RtpSessionUpdate waitingGap = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(500U, 2, 5000U),
            30_000L,
            "remote-media");
        RtpSessionUpdate gap = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(500U, 3, 6000U),
            30_005L,
            "remote-media");

        RtcpFeedbackPacket observedNack = CreateGenericNack(
            700U,
            500U,
            1);
        RtcpPacket feedbackPacket = new RtcpPacket();
        feedbackPacket.Type = RtcpPacketType.TransportFeedback;
        feedbackPacket.Feedback = observedNack;
        RtcpCompoundPacket feedbackCompound = new RtcpCompoundPacket();
        feedbackCompound.Packets = new RtcpPacket[] { feedbackPacket };
        clock.MonotonicTimeTicks = 30_006L;
        RtpSessionUpdate observation = session.ProcessInboundRtcpDatagram(
            rtcpCodec.EncodeRtcpCompoundPacketToArray(feedbackCompound),
            CreateContext(30_006L, "remote-control", 1));
        RtpSessionUpdate suppressed = session.PollSession();

        RtpRepairResult retransmission = new RtpRepairResult();
        retransmission.Success = true;
        retransmission.Kind = RtpRepairKind.Retransmission;
        retransmission.RepairedPackets = new RtpPacket[]
        {
            CreatePacket(500U, 1, 4000U)
        };
        retransmission.Data = new byte[] { 1 };
        clock.MonotonicTimeTicks = 30_007L;
        RtpSessionUpdate recovered = session.ApplySessionRepairResult(
            retransmission,
            CreateContext(30_007L, "remote-media", 90000));
        RtpSourceSnapshot remote = recovered.Snapshot!.Sources.Single(
            value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(beforeWrap.ReleasedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 65534 }));
            Assert.That(afterWrap.ReleasedPackets, Is.Empty);
            Assert.That(reordered.AcceptedPackets.Single().SequenceNumber,
                Is.EqualTo(65535));
            Assert.That(reordered.ReleasedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 65535, 0 }));
            Assert.That(waitingGap.ReleasedPackets, Is.Empty);
            Assert.That(waitingGap.FeedbackRequests, Is.Empty);
            Assert.That(gap.ReleasedPackets, Is.Empty);
            Assert.That(gap.RepairRequests.Single().SequenceNumbers,
                Is.EqualTo(new ushort[] { 1 }));
            Assert.That(gap.FeedbackRequests.Single().Feedback?.GenericNack?
                .Entries.Single().PacketIdentifier, Is.EqualTo(1));
            Assert.That(observation.Events.Any(
                value => value.Type == RtpSessionEventType.Report), Is.True);
            Assert.That(suppressed.FeedbackDecisions.Single().ShouldSend,
                Is.False);
            Assert.That(suppressed.FeedbackDecisions[0].Suppressed, Is.True);
            Assert.That(suppressed.OutboundControlPackets, Is.Empty);
            Assert.That(recovered.AcceptedPackets.Single().SequenceNumber,
                Is.EqualTo(1));
            Assert.That(recovered.ReleasedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 1, 2, 3 }));
            Assert.That(recovered.RepairResults.Single().Kind,
                Is.EqualTo(RtpRepairKind.Retransmission));
            Assert.That(remote.ReceptionStatistics?.ReceivedPacketCount,
                Is.EqualTo(6L));
            Assert.That(remote.ReceptionStatistics?.ReorderedPacketCount,
                Is.EqualTo(2L));
            Assert.That(remote.ReceptionStatistics?.RepairedPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.RecoveredPacketCount,
                Is.EqualTo(1L));
            Assert.That(remote.ReceptionStatistics?.InterarrivalJitter,
                Is.GreaterThan(0U));
        });
    }

    [Test]
    public void CollisionFecTimeoutRemoteGoodbyeAndLocalGoodbyeCompleteOneTrace()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.Full);
        configuration.RtcpScheduler.SenderTimeoutTicks = 5L;
        configuration.RtcpScheduler.MemberTimeoutTicks = 10L;
        TraceClock clock = new TraceClock();
        TraceFecDecoder decoder = new TraceFecDecoder();
        decoder.RepairedPacket = CreatePacket(500U, 10, 1000U);
        RtpSession session = CreateSession(configuration, clock, decoder);
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);

        RtpSessionUpdate outbound = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(0L, "local-media", 90000));
        uint originalLocalSource = outbound.OutboundPackets.Single()
            .SynchronizationSource;
        RtpSessionUpdate collision = ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(originalLocalSource, 50, 5000U),
            1L,
            "foreign-media");
        uint replacementLocalSource = session.GetSessionSnapshot()
            .LocalSourceIdentifiers.Single();

        RtpRepairRequest fecRequest = new RtpRepairRequest();
        fecRequest.Kind = RtpRepairKind.ForwardErrorCorrection;
        fecRequest.SourceIdentifier = 500U;
        fecRequest.SequenceNumbers = new ushort[] { 10 };
        fecRequest.RequestTimeTicks = 2L;
        fecRequest.DeadlineTicks = 12L;
        fecRequest.Data = new byte[] { 9 };
        RtpRepairResult fecResult = session.ingress.repairCoordinator
            .DecodeRepair(fecRequest, new byte[] { 7, 8 });
        clock.MonotonicTimeTicks = 2L;
        RtpSessionUpdate fecRecovered = session.ApplySessionRepairResult(
            fecResult,
            CreateContext(2L, "remote-peer", 90000));

        ProcessRtp(
            session,
            rtpCodec,
            clock,
            CreatePacket(600U, 20, 2000U),
            3L,
            "other-media");
        clock.MonotonicTimeTicks = 4L;
        RtpSessionUpdate remoteGoodbye = session.ProcessInboundRtcpDatagram(
            rtcpCodec.EncodeRtcpCompoundPacketToArray(
                CreateGoodbyeCompound(500U, "remote@example")),
            CreateContext(4L, "remote-peer", 1));

        clock.MonotonicTimeTicks = 100L;
        RtpSessionUpdate timeout = session.PollSession();
        RtpSessionUpdate localGoodbye = session.RequestSessionGoodbye(
            Encoding.UTF8.GetBytes("trace complete"));

        Assert.Multiple(() =>
        {
            Assert.That(originalLocalSource, Is.EqualTo(100U));
            Assert.That(replacementLocalSource, Is.EqualTo(101U));
            Assert.That(collision.Events.Single(
                value => value.Type == RtpSessionEventType.Collision)
                .ControlPacket?.Goodbye?.Sources,
                Is.EqualTo(new uint[] { originalLocalSource }));
            Assert.That(decoder.DecodeCallCount, Is.EqualTo(1));
            Assert.That(decoder.ReceivedKind,
                Is.EqualTo(RtpRepairKind.ForwardErrorCorrection));
            Assert.That(decoder.ReceivedData, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(fecRecovered.AcceptedPackets.Single().SequenceNumber,
                Is.EqualTo(10));
            Assert.That(fecRecovered.RepairResults.Single().Kind,
                Is.EqualTo(RtpRepairKind.ForwardErrorCorrection));
            Assert.That(fecRecovered.Snapshot!.Sources.Single(
                value => value.SourceIdentifier == 500U)
                .ReceptionStatistics?.RecoveredPacketCount,
                Is.EqualTo(1L));
            Assert.That(remoteGoodbye.Events.Any(value =>
                value.Type == RtpSessionEventType.Goodbye &&
                value.SourceIdentifier == 500U), Is.True);
            Assert.That(timeout.Events.Any(value =>
                value.Type == RtpSessionEventType.Timeout &&
                value.SourceIdentifier == 600U), Is.True);
            Assert.That(timeout.Snapshot?.Sources.Any(value =>
                value.SourceIdentifier == 600U), Is.False);
            Assert.That(timeout.Snapshot?.Sources.Single(value =>
                value.SourceIdentifier == 500U).Status,
                Is.EqualTo(RtpSourceStatus.Goodbye));
            Assert.That(localGoodbye.ControlScheduleDecisions.Single()
                .IsGoodbye, Is.True);
            Assert.That(localGoodbye.OutboundControlPackets.Single().Packets
                .Last().Goodbye?.Sources,
                Is.EqualTo(new uint[] { replacementLocalSource }));
            Assert.That(localGoodbye.OutboundControlPackets[0].Packets.Last()
                .Goodbye?.Reason,
                Is.EqualTo(Encoding.UTF8.GetBytes("trace complete")));
        });
    }

    static RtpSession CreateSession(
        RtpSessionConfiguration configuration,
        TraceClock clock,
        TraceFecDecoder? decoder)
    {
        return new RtpSession(
            configuration,
            clock,
            new TraceRandomSource(),
            CreatePayloadRegistry(configuration.TransportMode),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            decoder);
    }

    static RtpSessionConfiguration CreateConfiguration(
        RtcpCompoundMode compoundMode)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.RtcpCompoundMode = compoundMode;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 16;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry(
        RtpSessionTransportMode transportMode)
    {
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(transportMode);
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = 96;
        definition.ClockRate = 90000;
        definition.MediaIdentity = "video";
        definition.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(definition);
        return registry;
    }

    static RtpSessionUpdate ProcessRtp(
        IRtpSession session,
        IRtpPacketCodec codec,
        TraceClock clock,
        RtpPacket packet,
        long arrivalTimeTicks,
        string transportIdentity)
    {
        clock.MonotonicTimeTicks = arrivalTimeTicks;
        return session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(packet),
            CreateContext(arrivalTimeTicks, transportIdentity, 90000));
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
        packet.Payload = new byte[] { (byte)sequenceNumber };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long operationTimeTicks,
        string transportIdentity,
        int clockRate)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = operationTimeTicks;
        return context;
    }

    static RtcpFeedbackPacket CreateGenericNack(
        uint senderSourceIdentifier,
        uint mediaSourceIdentifier,
        ushort sequenceNumber)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = sequenceNumber;
        RtcpGenericNack nack = new RtcpGenericNack();
        nack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.SenderSourceIdentifier = senderSourceIdentifier;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.GenericNack = nack;
        return feedback;
    }

    static RtcpCompoundPacket CreateGoodbyeCompound(
        uint sourceIdentifier,
        string canonicalNameText)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket reportPacket = new RtcpPacket();
        reportPacket.Type = RtcpPacketType.ReceiverReport;
        reportPacket.ReceiverReport = receiverReport;

        RtcpSourceDescriptionItem canonicalName =
            new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = Encoding.UTF8.GetBytes(canonicalNameText);
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket descriptionPacket = new RtcpPacket();
        descriptionPacket.Type = RtcpPacketType.SourceDescription;
        descriptionPacket.SourceDescription = description;

        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { sourceIdentifier };
        RtcpPacket goodbyePacket = new RtcpPacket();
        goodbyePacket.Type = RtcpPacketType.Goodbye;
        goodbyePacket.Goodbye = goodbye;

        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            reportPacket,
            descriptionPacket,
            goodbyePacket
        };
        return compound;
    }

    private class TraceClock : IRtpClock
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

    private class TraceRandomSource : IRtpRandomSource
    {
        uint nextSourceIdentifier = 100U;

        public uint CreateSynchronizationSource()
        {
            uint result = nextSourceIdentifier;
            nextSourceIdentifier = nextSourceIdentifier + 1U;
            return result;
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

    private class TraceFecDecoder : IRtpRepairDecoder
    {
        public int DecodeCallCount;
        public RtpRepairKind ReceivedKind;
        public byte[] ReceivedData = [];
        public RtpPacket RepairedPacket = new RtpPacket();

        public RtpRepairResult DecodeRtpRepair(
            RtpRepairRequest request,
            ReadOnlySpan<byte> repairData)
        {
            DecodeCallCount = DecodeCallCount + 1;
            ReceivedKind = request.Kind;
            ReceivedData = repairData.ToArray();
            RtpRepairResult result = new RtpRepairResult();
            result.Success = true;
            result.Kind = request.Kind;
            result.RepairedPackets = new RtpPacket[]
            {
                RepairedPacket.Copy()
            };
            result.Data = repairData.ToArray();
            return result;
        }
    }
}
