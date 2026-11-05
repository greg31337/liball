using rtp;

namespace rtp.Tests.Session;

public class RtpSessionPollingTests
{
    [Test]
    public void DueRegularReportIsBuiltRecordedAndReturnedWithSnapshot()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        FixedRandomSource randomSource = new FixedRandomSource();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            randomSource,
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        long dueTime = poller.controlScheduler.nextReportTimeTicks;
        clock.MonotonicTimeTicks = dueTime;
        clock.NtpTimestamp = 0x0102030405060708UL;

        RtpSessionUpdate update = poller.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(update.OutboundControlPackets,
                Has.Length.EqualTo(1));
            Assert.That(update.OutboundControlPackets[0].Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(update.OutboundControlPackets[0].Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(update.ControlScheduleDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(update.ControlScheduleDecisions[0].CompoundPacket,
                Is.Not.Null);
            Assert.That(update.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Report));
            Assert.That(update.Snapshot?.LocalSourceIdentifiers,
                Is.EqualTo(new uint[] { 100U }));
            Assert.That(update.Snapshot?.NextRtcpSendTimeTicks,
                Is.GreaterThan(dueTime));
            Assert.That(poller.controlScheduler.averagePacketSize,
                Is.Not.EqualTo(
                    configuration.RtcpScheduler.InitialAveragePacketSize));
        });
    }

    [Test]
    public void DueReportRollsRemoteReceptionIntervalIntoReportBlocks()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpPayloadTypeRegistry payloadRegistry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            payloadRegistry);
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(CreateRtpPacket(500U, 10, 1000U)),
            CreateContext(0L));
        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(CreateRtpPacket(500U, 12, 3000U)),
            CreateContext(1L));
        clock.MonotonicTimeTicks = 1L;
        poller.PollSession();
        clock.MonotonicTimeTicks =
            poller.controlScheduler.nextReportTimeTicks;

        RtpSessionUpdate update = poller.PollSession();
        RtcpReceiverReport report = update.OutboundControlPackets.Single()
            .Packets[0].ReceiverReport!;
        RtpSourceState source = ingress.remoteSourceTable
            .FindSourceState(500U)!;

        Assert.Multiple(() =>
        {
            Assert.That(update.ControlScheduleDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(report.ReceptionReports, Has.Length.EqualTo(1));
            Assert.That(report.ReceptionReports[0].SourceIdentifier,
                Is.EqualTo(500U));
            Assert.That(report.ReceptionReports[0].FractionLost,
                Is.GreaterThan(0));
            Assert.That(source.receptionStatistics.expectedPacketCountPrior,
                Is.GreaterThan(0L));
            Assert.That(update.Snapshot?.Sources.Single(
                value => value.SourceIdentifier == 500U)
                .ReceptionStatistics?.FractionLost,
                Is.EqualTo(report.ReceptionReports[0].FractionLost));
        });
    }

    [Test]
    public void ImmediateFeedbackIsEmittedBeforeTheRegularDeadline()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpScheduler.FeedbackEnabled = true;
        FixedClock clock = new FixedClock();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        RtpFeedbackRequest request = CreateFeedbackRequest(
            500U,
            25,
            0L,
            100L);
        poller.QueueSessionFeedbackRequest(request);
        request.Feedback!.MediaSourceIdentifier = 999U;

        RtpSessionUpdate update = poller.PollSession();
        RtcpCompoundPacket compound = update.OutboundControlPackets.Single();
        RtcpPacket feedbackPacket = compound.Packets.Single(
            value => value.Type == RtcpPacketType.TransportFeedback);

        Assert.Multiple(() =>
        {
            Assert.That(update.FeedbackDecisions, Has.Length.EqualTo(1));
            Assert.That(update.FeedbackDecisions[0].ShouldSend, Is.True);
            Assert.That(update.FeedbackDecisions[0].Piggyback, Is.False);
            Assert.That(update.FeedbackDecisions[0].TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(update.ControlScheduleDecisions[0].ShouldSend,
                Is.False);
            Assert.That(feedbackPacket.Feedback?.MediaSourceIdentifier,
                Is.EqualTo(500U));
            Assert.That(update.Events.Single().FeedbackDecision?.ShouldSend,
                Is.True);
        });
    }

    [Test]
    public void RegularFeedbackPiggybacksWithoutDuplicatingTheCompound()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpScheduler.FeedbackEnabled = true;
        FixedClock clock = new FixedClock();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        RtpFeedbackRequest request = CreateFeedbackRequest(
            500U,
            25,
            0L,
            0L);
        request.PreferredTimingMode = RtcpFeedbackTimingMode.Regular;
        poller.QueueSessionFeedbackRequest(request);
        clock.MonotonicTimeTicks =
            poller.controlScheduler.nextReportTimeTicks;

        RtpSessionUpdate update = poller.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(update.FeedbackDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(update.FeedbackDecisions[0].Piggyback, Is.True);
            Assert.That(update.ControlScheduleDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(update.FeedbackDecisions[0].CompoundPacket,
                Is.Not.Null);
            Assert.That(update.ControlScheduleDecisions[0].CompoundPacket,
                Is.Not.Null);
            Assert.That(update.OutboundControlPackets,
                Has.Length.EqualTo(1));
            Assert.That(update.OutboundControlPackets[0].Packets.Any(
                value => value.Type == RtcpPacketType.TransportFeedback),
                Is.True);
        });
    }

    [Test]
    public void PollProducesDelayedNackRepairWorkAndFeedback()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.JitterBuffer.MaximumLatencyTicks = 100L;
        configuration.JitterBuffer.NackDelayTicks = 5L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 10L;
        configuration.JitterBuffer.MaximumNackRetryCount = 1;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        FixedClock clock = new FixedClock();
        RtpPayloadTypeRegistry payloadRegistry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            payloadRegistry);
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(CreateRtpPacket(500U, 10, 1000U)),
            CreateContext(0L));
        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(CreateRtpPacket(500U, 12, 3000U)),
            CreateContext(1L));
        clock.MonotonicTimeTicks = 6L;

        RtpSessionUpdate update = poller.PollSession();
        RtpSessionUpdate repeated = poller.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(update.RepairRequests, Has.Length.EqualTo(1));
            Assert.That(update.RepairRequests[0].Kind,
                Is.EqualTo(RtpRepairKind.Retransmission));
            Assert.That(update.RepairRequests[0].SourceIdentifier,
                Is.EqualTo(500U));
            Assert.That(update.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(update.FeedbackRequests, Has.Length.EqualTo(1));
            Assert.That(update.FeedbackDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(update.OutboundControlPackets,
                Has.Length.EqualTo(1));
            Assert.That(update.Events.Any(
                value => value.Type == RtpSessionEventType.Repair),
                Is.True);
            Assert.That(repeated.RepairRequests, Is.Empty);
            Assert.That(repeated.FeedbackRequests, Is.Empty);
        });
    }

    [Test]
    public void PollExpiresRemoteSourcesAndReturnsTimeoutEvents()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpScheduler.SenderTimeoutTicks = 5L;
        configuration.RtcpScheduler.MemberTimeoutTicks = 10L;
        FixedClock clock = new FixedClock();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        ingress.remoteSourceTable.RecordRemoteRtcpActivity(
            500U,
            CreateContext(0L));
        clock.MonotonicTimeTicks = 10L;

        RtpSessionUpdate update = poller.PollSession();
        RtpSessionEvent timeout = update.Events.Single(
            value => value.Type == RtpSessionEventType.Timeout);

        Assert.Multiple(() =>
        {
            Assert.That(timeout.SourceIdentifier, Is.EqualTo(500U));
            Assert.That(timeout.SourceStatus,
                Is.EqualTo(RtpSourceStatus.Inactive));
            Assert.That(timeout.Source?.Status,
                Is.EqualTo(RtpSourceStatus.Inactive));
            Assert.That(ingress.remoteSourceTable.GetRemoteSourceSnapshots(),
                Is.Empty);
            Assert.That(update.Snapshot?.MemberCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void QueuedControlEventsAndRepairWorkAreOwnedAndDrainedOnce()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        RtcpCompoundPacket compound = new RtcpReportBuilder()
            .BuildRegularRtcpReport(
                poller.CreateSessionSnapshot(0L),
                0UL);
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Source;
        sessionEvent.Message = "queued";
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = RtpRepairKind.ForwardErrorCorrection;
        request.SourceIdentifier = 500U;
        request.SequenceNumbers = new ushort[] { 10 };
        request.RequestTimeTicks = 0L;
        request.DeadlineTicks = 10L;
        RtpRepairResult result = new RtpRepairResult();
        result.Success = true;
        result.Kind = RtpRepairKind.ForwardErrorCorrection;
        result.Data = new byte[] { 1 };
        poller.QueueOutboundControlPacket(compound);
        poller.QueueSessionEvent(sessionEvent);
        poller.QueueSessionRepairRequest(request);
        poller.QueueSessionRepairResult(result);
        compound.Packets = [];
        sessionEvent.Message = "changed";
        request.SequenceNumbers[0] = 99;
        result.Data[0] = 99;

        RtpSessionUpdate first = poller.PollSession();
        RtpSessionUpdate second = poller.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(first.OutboundControlPackets,
                Has.Length.EqualTo(1));
            Assert.That(first.OutboundControlPackets[0].Packets,
                Has.Length.EqualTo(2));
            Assert.That(first.Events.Single().Message, Is.EqualTo("queued"));
            Assert.That(first.RepairRequests.Single().SequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(first.RepairResults.Single().Data,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(second.OutboundControlPackets, Is.Empty);
            Assert.That(second.Events, Is.Empty);
            Assert.That(second.RepairRequests, Is.Empty);
            Assert.That(second.RepairResults, Is.Empty);
        });
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 16;
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

    static RtpPacket CreateRtpPacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Payload = new byte[] { 1, 2, 3 };
        return packet;
    }

    static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = "remote-path";
        context.ClockRate = 90000;
        return context;
    }

    static RtpFeedbackRequest CreateFeedbackRequest(
        uint mediaSourceIdentifier,
        ushort sequenceNumber,
        long earliestSendTimeTicks,
        long expirationTimeTicks)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = sequenceNumber;
        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.GenericNack = genericNack;
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = feedback;
        request.EarliestSendTimeTicks = earliestSendTimeTicks;
        request.ExpirationTimeTicks = expirationTimeTicks;
        request.PreferredTimingMode = RtcpFeedbackTimingMode.Immediate;
        return request;
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 100U;
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

    private class FixedClock : IRtpClock
    {
        public long MonotonicTimeTicks;
        public ulong NtpTimestamp;

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
            return NtpTimestamp;
        }
    }
}
