using rtp;

namespace rtp.Tests.Session;

public class RtpRtcpIngressProcessingTests
{
    [Test]
    public void ReceptionReportUpdatesLocalTransmissionStateAndRoundTripTime()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.Full);
        FixedRandomSource randomSource = new FixedRandomSource();
        RtpPayloadTypeRegistry payloadRegistry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            randomSource,
            payloadRegistry);
        RtpLocalSourceState localSource =
            ingress.localSourceTable.CreateLocalSourceState();
        FixedClock clock = new FixedClock();
        clock.MonotonicTimeTicks = 10_000_000L;
        RtpOutboundStream outbound = new RtpOutboundStream(
            configuration,
            localSource,
            payloadRegistry,
            new RtpHeaderExtensionRegistry(),
            null,
            clock);
        outbound.PrepareOutboundRtpPacket(
            CreateOutboundPacket(),
            CreateRtpContext(10_000_000L));
        outbound.transmissionStatistics.BuildRtcpSenderInformation(
            0x0000123456780000UL);
        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = localSource.sourceIdentifier;
        report.FractionLost = 4;
        report.CumulativePacketsLost = 2;
        report.LastSenderReport = 0x12345678U;
        report.DelaySinceLastSenderReport = 0x00008000U;
        RtcpCompoundPacket compound = CreateReceiverReportCompound(
            300U,
            new RtcpReceptionReport[] { report },
            false);

        RtpSessionUpdate update = ingress.ProcessSeparateRtcpDatagram(
            new RtcpPacketCodec(configuration)
                .EncodeRtcpCompoundPacketToArray(compound),
            CreateRtcpContext(30_000_000L, "remote-report"));
        RtpSourceSnapshot localSnapshot =
            localSource.CreateLocalSourceSnapshot();
        RtpSourceSnapshot? remoteSnapshot =
            ingress.remoteSourceTable.FindRemoteSourceSnapshot(300U);

        Assert.Multiple(() =>
        {
            Assert.That(update.Events, Has.Length.EqualTo(2));
            Assert.That(update.Events[0].Type,
                Is.EqualTo(RtpSessionEventType.Report));
            Assert.That(localSnapshot.RemoteReports, Has.Length.EqualTo(1));
            Assert.That(localSnapshot.RemoteReports[0].ReporterSourceIdentifier,
                Is.EqualTo(300U));
            Assert.That(localSnapshot.RemoteReports[0].FractionLost,
                Is.EqualTo(4));
            Assert.That(localSnapshot.RemoteReports[0].HasRoundTripTime,
                Is.True);
            Assert.That(localSnapshot.RemoteReports[0].RoundTripTimeTicks,
                Is.EqualTo(15_000_000L));
            Assert.That(remoteSnapshot?.CanonicalName,
                Is.EqualTo("remote@example"));
            Assert.That(remoteSnapshot?.LastRtcpActivityTicks,
                Is.EqualTo(30_000_000L));
        });
    }

    [Test]
    public void SourceDescriptionAndGoodbyeUpdateRemoteLifecycleState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.Full);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtcpCompoundPacket compound = CreateReceiverReportCompound(
            500U,
            [],
            true);

        RtpSessionUpdate update = ingress.ProcessSeparateRtcpDatagram(
            new RtcpPacketCodec(configuration)
                .EncodeRtcpCompoundPacketToArray(compound),
            CreateRtcpContext(40L, "remote-goodbye"));
        RtpSourceState? source =
            ingress.remoteSourceTable.FindSourceState(500U);

        Assert.Multiple(() =>
        {
            Assert.That(update.Events.Select(value => value.Type),
                Is.EqualTo(new RtpSessionEventType[]
                {
                    RtpSessionEventType.Report,
                    RtpSessionEventType.Report,
                    RtpSessionEventType.Goodbye
                }));
            Assert.That(update.Events[1].Source?.CanonicalName,
                Is.EqualTo("remote@example"));
            Assert.That(update.Events[2].SourceStatus,
                Is.EqualTo(RtpSourceStatus.Goodbye));
            Assert.That(source?.status, Is.EqualTo(RtpSourceStatus.Goodbye));
            Assert.That(source?.canonicalName, Is.EqualTo("remote@example"));
            Assert.That(source?.goodbyeReason,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ExtendedReportIsRetainedWithReporterActivity()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.ReducedSize);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        block.ReceiverReferenceTime = 0x0102030405060708UL;
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = 700U;
        report.Blocks = new RtcpExtendedReportBlock[] { block };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = report;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { packet };

        RtpSessionUpdate update = ingress.ProcessSeparateRtcpDatagram(
            new RtcpPacketCodec(configuration)
                .EncodeRtcpCompoundPacketToArray(compound),
            CreateRtcpContext(50L, "remote-xr"));
        block.ReceiverReferenceTime = 1UL;
        RtpSourceState? source =
            ingress.remoteSourceTable.FindSourceState(700U);

        Assert.Multiple(() =>
        {
            Assert.That(update.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Report));
            Assert.That(update.Events[0].ControlPacket?.ExtendedReport?
                .Blocks[0].ReceiverReferenceTime,
                Is.EqualTo(0x0102030405060708UL));
            Assert.That(source?.extendedReportBlocks,
                Has.Length.EqualTo(1));
            Assert.That(source?.extendedReportBlocks[0].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.ReceiverReferenceTime));
            Assert.That(source?.extendedReportBlocks[0].ReceiverReferenceTime,
                Is.EqualTo(0x0102030405060708UL));
            Assert.That(source?.lastExtendedReportTimeTicks, Is.EqualTo(50L));
            Assert.That(source?.lastRtcpActivityTicks, Is.EqualTo(50L));
        });
    }

    [Test]
    public void GenericNackSuppressesPendingFeedbackAndUsesPacketHistoryForRepair()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.ReducedSize);
        FixedRandomSource randomSource = new FixedRandomSource();
        RtpPayloadTypeRegistry payloadRegistry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            randomSource,
            payloadRegistry);
        RtpLocalSourceState localSource =
            ingress.localSourceTable.CreateLocalSourceState();
        RtpOutboundStream outbound = new RtpOutboundStream(
            configuration,
            localSource,
            payloadRegistry,
            new RtpHeaderExtensionRegistry(),
            null,
            new FixedClock());
        outbound.PrepareOutboundRtpPacket(
            CreateOutboundPacket(),
            CreateRtpContext(10L));
        RecordingRepairEncoder repairEncoder =
            new RecordingRepairEncoder();
        ingress.ConfigureRepairHooks(repairEncoder, null);
        RtcpFeedbackPacket feedback = CreateGenericNackFeedback(
            300U,
            localSource.sourceIdentifier,
            50,
            1);
        RtpFeedbackRequest pendingRequest = new RtpFeedbackRequest();
        pendingRequest.Feedback = feedback.Copy();
        pendingRequest.Feedback.SenderSourceIdentifier = 999U;
        pendingRequest.EarliestSendTimeTicks = 20L;
        pendingRequest.ExpirationTimeTicks = 100L;
        pendingRequest.PreferredTimingMode =
            RtcpFeedbackTimingMode.Immediate;
        ingress.GetIngressFeedbackScheduler().QueueRtcpFeedback(
            pendingRequest);
        RtcpPacket feedbackPacket = new RtcpPacket();
        feedbackPacket.Type = RtcpPacketType.TransportFeedback;
        feedbackPacket.Feedback = feedback;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { feedbackPacket };

        RtpSessionUpdate update = ingress.ProcessSeparateRtcpDatagram(
            new RtcpPacketCodec(configuration)
                .EncodeRtcpCompoundPacketToArray(compound),
            CreateRtcpContext(20L, "remote-feedback"));
        RtpFeedbackDecision decision = ingress.GetIngressFeedbackScheduler()
            .PollRtcpFeedback(CreateSessionSnapshot(), 20L);
        repairEncoder.Result.Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldSend, Is.False);
            Assert.That(decision.Suppressed, Is.True);
            Assert.That(update.RepairRequests, Has.Length.EqualTo(1));
            Assert.That(update.RepairRequests[0].Kind,
                Is.EqualTo(RtpRepairKind.Retransmission));
            Assert.That(update.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 50, 51 }));
            Assert.That(update.RepairResults, Has.Length.EqualTo(1));
            Assert.That(update.RepairResults[0].Data,
                Is.EqualTo(new byte[] { 50 }));
            Assert.That(repairEncoder.SourcePackets, Has.Length.EqualTo(1));
            Assert.That(repairEncoder.SourcePackets[0].SequenceNumber,
                Is.EqualTo(50));
            Assert.That(repairEncoder.Request?.SequenceNumbers,
                Is.EqualTo(new ushort[] { 50, 51 }));
            Assert.That(update.Events.Select(value => value.Type),
                Is.EqualTo(new RtpSessionEventType[]
                {
                    RtpSessionEventType.Repair,
                    RtpSessionEventType.Report
                }));
            Assert.That(update.Events[0].RepairResult?.Data,
                Is.EqualTo(new byte[] { 50 }));
        });
    }

    [Test]
    public void IngressQueuesJitterBufferFeedbackForLaterSuppression()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.Full);
        configuration.JitterBuffer.MaximumLatencyTicks = 100L;
        configuration.JitterBuffer.NackDelayTicks = 0L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 10L;
        configuration.JitterBuffer.MaximumNackRetryCount = 1;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        RtpPayloadTypeRegistry registry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            registry);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        RtpPacket first = CreateInboundPacket(800U, 10, 1000U);
        RtpPacket gap = CreateInboundPacket(800U, 12, 3000U);

        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(first),
            CreateRtcpContext(0L, "remote-media"));
        RtpSessionUpdate update = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(gap),
            CreateRtcpContext(1L, "remote-media"));
        update.FeedbackRequests[0].Feedback!.MediaSourceIdentifier = 1U;

        Assert.Multiple(() =>
        {
            Assert.That(update.FeedbackRequests, Has.Length.EqualTo(1));
            Assert.That(update.FeedbackRequests[0].Feedback?.GenericNack?
                .Entries[0].PacketIdentifier, Is.EqualTo(11));
            Assert.That(ingress.feedbackScheduler?.pendingRequests,
                Has.Count.EqualTo(1));
            Assert.That(ingress.feedbackScheduler?.pendingRequests[0]
                .Feedback?.MediaSourceIdentifier, Is.EqualTo(800U));
        });
    }

    static RtpSessionConfiguration CreateConfiguration(
        RtcpCompoundMode compoundMode)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.RtcpCompoundMode = compoundMode;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 16;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.RtcpScheduler.MinimumFeedbackIntervalTicks = 0L;
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

    static RtpPacket CreateOutboundPacket()
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = 96;
        packet.Marker = true;
        packet.Payload = new byte[] { 9 };
        return packet;
    }

    static RtpPacket CreateInboundPacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp)
    {
        RtpPacket packet = CreateOutboundPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        return packet;
    }

    static RtpPacketContext CreateRtpContext(long sendTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = sendTimeTicks;
        context.TransportIdentity = "local-path";
        context.ClockRate = 90000;
        context.MediaClockTimestamp = 1000L;
        return context;
    }

    static RtpPacketContext CreateRtcpContext(
        long arrivalTimeTicks,
        string transportIdentity)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        return context;
    }

    static RtcpCompoundPacket CreateReceiverReportCompound(
        uint reporterSourceIdentifier,
        RtcpReceptionReport[] reports,
        bool includeGoodbye)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource =
            reporterSourceIdentifier;
        receiverReport.ReceptionReports = reports;
        RtcpPacket reportPacket = new RtcpPacket();
        reportPacket.Type = RtcpPacketType.ReceiverReport;
        reportPacket.ReceiverReport = receiverReport;
        RtcpPacket descriptionPacket = CreateSourceDescriptionPacket(
            reporterSourceIdentifier);
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        if (!includeGoodbye)
        {
            compound.Packets = new RtcpPacket[]
            {
                reportPacket,
                descriptionPacket
            };
            return compound;
        }

        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { reporterSourceIdentifier };
        goodbye.HasReason = true;
        goodbye.Reason = new byte[] { 1, 2, 3 };
        RtcpPacket goodbyePacket = new RtcpPacket();
        goodbyePacket.Type = RtcpPacketType.Goodbye;
        goodbyePacket.Goodbye = goodbye;
        compound.Packets = new RtcpPacket[]
        {
            reportPacket,
            descriptionPacket,
            goodbyePacket
        };
        return compound;
    }

    static RtcpPacket CreateSourceDescriptionPacket(
        uint sourceIdentifier)
    {
        RtcpSourceDescriptionItem canonicalName =
            new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = System.Text.Encoding.UTF8.GetBytes(
            "remote@example");
        RtcpSourceDescriptionChunk chunk =
            new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static RtcpFeedbackPacket CreateGenericNackFeedback(
        uint senderSourceIdentifier,
        uint mediaSourceIdentifier,
        ushort sequenceNumber,
        ushort lostPacketBitmask)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = sequenceNumber;
        entry.LostPacketBitmask = lostPacketBitmask;
        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.SenderSourceIdentifier = senderSourceIdentifier;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.GenericNack = genericNack;
        return feedback;
    }

    static RtpSessionSnapshot CreateSessionSnapshot()
    {
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.MemberCount = 2;
        snapshot.SenderCount = 1;
        snapshot.NextRtcpSendTimeTicks = 100L;
        snapshot.AverageRtcpPacketSize = 100;
        return snapshot;
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 100U;
        }

        public ushort CreateSequenceNumber()
        {
            return 50;
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

    private class FixedClock : IRtpClock
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
            return 0UL;
        }
    }

    private class RecordingRepairEncoder : IRtpRepairEncoder
    {
        public RtpRepairResult Result = new RtpRepairResult();
        public RtpRepairRequest? Request;
        public RtpPacket[] SourcePackets = [];

        public RtpRepairResult EncodeRtpRepair(
            RtpRepairRequest request,
            RtpPacket[] sourcePackets)
        {
            Request = request.Copy();
            SourcePackets = new RtpPacket[sourcePackets.Length];
            int packetIndex = 0;
            while (packetIndex < sourcePackets.Length)
            {
                SourcePackets[packetIndex] = sourcePackets[packetIndex].Copy();
                packetIndex = packetIndex + 1;
            }

            Result = new RtpRepairResult();
            Result.Success = true;
            Result.Kind = request.Kind;
            Result.Data = new byte[] { (byte)request.SequenceNumbers[0] };
            return Result;
        }
    }
}
