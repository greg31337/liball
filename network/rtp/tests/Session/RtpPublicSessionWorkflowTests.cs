using System.Text;
using rtp;

namespace rtp.Tests.Session;

public class RtpPublicSessionWorkflowTests
{
    [Test]
    public void SendOnlySeparateWorkflowSupportsFeedbackReportsGoodbyeAndReset()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Separate);
        configuration.RtcpScheduler.FeedbackEnabled = true;
        FixedClock clock = new FixedClock();
        IRtpSession session = CreateSession(configuration, clock);

        RtpSessionUpdate video = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U, 96),
            CreateContext(0L, "video-send", 90000, 1000L));
        clock.MonotonicTimeTicks = 1L;
        RtpSessionUpdate audio = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U, 0),
            CreateContext(1L, "audio-send", 8000, 160L));
        uint localSourceIdentifier = video.OutboundPackets.Single()
            .SynchronizationSource;

        RtpSessionUpdate queuedFeedback = session.RequestSessionFeedback(
            CreatePictureLossRequest(700U));
        RtpSessionUpdate feedback = session.PollSession();
        long reportTime = session.GetSessionSnapshot()
            .NextRtcpSendTimeTicks;
        clock.MonotonicTimeTicks = reportTime;
        RtpSessionUpdate report = session.PollSession();
        RtpSessionSnapshot active = session.GetSessionSnapshot();

        RtpSessionUpdate goodbye = session.RequestSessionGoodbye(
            Encoding.UTF8.GetBytes("complete"));
        session.ResetSession();
        RtpSessionSnapshot reset = session.GetSessionSnapshot();
        RtpSessionUpdate reused = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U, 96),
            CreateContext(reportTime, "video-send", 90000, 2000L));

        Assert.Multiple(() =>
        {
            Assert.That(video.OutboundPackets, Has.Length.EqualTo(1));
            Assert.That(audio.OutboundPackets, Has.Length.EqualTo(1));
            Assert.That(audio.OutboundPackets[0].SynchronizationSource,
                Is.EqualTo(localSourceIdentifier));
            Assert.That(audio.OutboundPackets[0].SequenceNumber,
                Is.EqualTo((ushort)(video.OutboundPackets[0]
                    .SequenceNumber + 1)));
            Assert.That(queuedFeedback.FeedbackRequests,
                Has.Length.EqualTo(1));
            Assert.That(feedback.FeedbackDecisions.Single().ShouldSend,
                Is.True);
            Assert.That(feedback.OutboundControlPackets.Single().Packets.Any(
                value => value.Type == RtcpPacketType.PayloadFeedback),
                Is.True);
            Assert.That(report.OutboundControlPackets.Single().Packets[0].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(active.Sources, Has.Length.EqualTo(1));
            Assert.That(active.Sources[0].IsSender, Is.True);
            Assert.That(active.Sources[0].ClockRate, Is.EqualTo(8000));
            Assert.That(active.Sources[0].TransmissionStatistics?
                .SentPacketCount, Is.EqualTo(2L));
            Assert.That(goodbye.ControlScheduleDecisions.Single().IsGoodbye,
                Is.True);
            Assert.That(goodbye.OutboundControlPackets.Single().Packets.Last()
                .Goodbye?.Reason, Is.EqualTo(Encoding.UTF8.GetBytes("complete")));
            Assert.That(reset.Sources, Has.Length.EqualTo(1));
            Assert.That(reset.Sources[0].IsSender, Is.False);
            Assert.That(reset.LocalSourceIdentifiers.Single(),
                Is.Not.EqualTo(localSourceIdentifier));
            Assert.That(reused.OutboundPackets, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ReceiveOnlyMuxWorkflowTracksMultipleSourcesAndClockRates()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Multiplexed);
        FixedClock clock = new FixedClock();
        IRtpSession session = CreateSession(configuration, clock);
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);

        RtpSessionUpdate video = session.ProcessInboundMultiplexedDatagram(
            rtpCodec.EncodeRtpPacketToArray(
                CreatePacket(500U, 10, 9000U, 96)),
            CreateContext(0L, "mux-peer", 1, 0L));
        RtpSessionUpdate audio = session.ProcessInboundMultiplexedDatagram(
            rtpCodec.EncodeRtpPacketToArray(
                CreatePacket(600U, 20, 800U, 0)),
            CreateContext(1L, "mux-peer", 1, 1L));
        RtpSessionUpdate inboundReport =
            session.ProcessInboundMultiplexedDatagram(
                rtcpCodec.EncodeRtcpCompoundPacketToArray(
                    CreateReceiverReportCompound(500U, [])),
                CreateContext(2L, "mux-peer", 1, 2L));
        clock.MonotonicTimeTicks = 2L;
        RtpSessionSnapshot received = session.GetSessionSnapshot();

        clock.MonotonicTimeTicks = received.NextRtcpSendTimeTicks;
        RtpSessionUpdate report = session.PollSession();
        RtpSourceSnapshot remoteVideo = received.Sources.Single(
            value => value.SourceIdentifier == 500U);
        RtpSourceSnapshot remoteAudio = received.Sources.Single(
            value => value.SourceIdentifier == 600U);

        Assert.Multiple(() =>
        {
            Assert.That(video.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(audio.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(inboundReport.Events.Select(value => value.PacketKind),
                Is.All.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(received.Sources, Has.Length.EqualTo(3));
            Assert.That(received.SenderCount, Is.EqualTo(2));
            Assert.That(received.Sources.Single(value => value.IsLocal)
                .IsSender, Is.False);
            Assert.That(remoteVideo.ClockRate, Is.EqualTo(90000));
            Assert.That(remoteVideo.MediaIdentity, Is.EqualTo("video"));
            Assert.That(remoteVideo.CanonicalName,
                Is.EqualTo("remote@example"));
            Assert.That(remoteAudio.ClockRate, Is.EqualTo(8000));
            Assert.That(remoteAudio.MediaIdentity, Is.EqualTo("audio"));
            Assert.That(report.OutboundControlPackets.Single().Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(report.OutboundControlPackets[0].Packets[0]
                .ReceiverReport?.ReceptionReports.Select(
                    value => value.SourceIdentifier),
                Is.EquivalentTo(new uint[] { 500U, 600U }));
        });
    }

    [Test]
    public void BidirectionalSeparateWorkflowRepairsLossAndProcessesReports()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Separate);
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.JitterBuffer.MaximumLatencyTicks = 100L;
        configuration.JitterBuffer.NackDelayTicks = 5L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 10L;
        configuration.JitterBuffer.MaximumNackRetryCount = 1;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        FixedClock clock = new FixedClock();
        IRtpSession session = CreateSession(configuration, clock);
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);

        RtpSessionUpdate outbound = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U, 96),
            CreateContext(0L, "local-rtp", 90000, 1000L));
        uint localSourceIdentifier = outbound.OutboundPackets.Single()
            .SynchronizationSource;
        session.ProcessInboundRtpDatagram(
            rtpCodec.EncodeRtpPacketToArray(
                CreatePacket(500U, 10, 1000U, 96)),
            CreateContext(0L, "separate-peer", 90000, 0L));
        RtpSessionUpdate gap = session.ProcessInboundRtpDatagram(
            rtpCodec.EncodeRtpPacketToArray(
                CreatePacket(500U, 12, 3000U, 96)),
            CreateContext(1L, "separate-peer", 90000, 90L));

        clock.MonotonicTimeTicks = 6L;
        RtpSessionUpdate loss = session.PollSession();
        RtpRepairResult repairResult = new RtpRepairResult();
        repairResult.Success = true;
        repairResult.Kind = RtpRepairKind.Retransmission;
        repairResult.RepairedPackets = new RtpPacket[]
        {
            CreatePacket(500U, 11, 2000U, 96)
        };
        RtpSessionUpdate repaired = session.ApplySessionRepairResult(
            repairResult,
            CreateContext(6L, "separate-peer", 90000, 540L));

        RtcpReceptionReport receptionReport = new RtcpReceptionReport();
        receptionReport.SourceIdentifier = localSourceIdentifier;
        receptionReport.FractionLost = 3;
        receptionReport.CumulativePacketsLost = 1;
        clock.MonotonicTimeTicks = 7L;
        RtpSessionUpdate inboundReport = session.ProcessInboundRtcpDatagram(
            rtcpCodec.EncodeRtcpCompoundPacketToArray(
                CreateReceiverReportCompound(
                    500U,
                    new RtcpReceptionReport[] { receptionReport })),
            CreateContext(7L, "separate-peer", 1, 7L));
        RtpSessionSnapshot final = session.GetSessionSnapshot();
        RtpSourceSnapshot local = final.Sources.Single(
            value => value.IsLocal);
        RtpSourceSnapshot remote = final.Sources.Single(
            value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(outbound.OutboundPackets, Has.Length.EqualTo(1));
            Assert.That(gap.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(gap.ReleasedPackets, Is.Empty);
            Assert.That(loss.RepairRequests.Single().SequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(loss.FeedbackDecisions.Single().ShouldSend, Is.True);
            Assert.That(loss.OutboundControlPackets.Single().Packets.Any(
                value => value.Type == RtcpPacketType.TransportFeedback),
                Is.True);
            Assert.That(repaired.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(repaired.ReleasedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 11, 12 }));
            Assert.That(repaired.RepairResults.Single().Success, Is.True);
            Assert.That(inboundReport.Events.Any(
                value => value.Type == RtpSessionEventType.Report), Is.True);
            Assert.That(local.IsSender, Is.True);
            Assert.That(local.RemoteReports.Single().ReporterSourceIdentifier,
                Is.EqualTo(500U));
            Assert.That(local.RemoteReports[0].FractionLost, Is.EqualTo(3));
            Assert.That(remote.ReceptionStatistics?.ReceivedPacketCount,
                Is.EqualTo(3L));
            Assert.That(remote.ReceptionStatistics?.RepairedPacketCount,
                Is.EqualTo(1L));
        });
    }

    static IRtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock)
    {
        return new RtpSession(
            configuration,
            clock,
            new IncrementingRandomSource(),
            CreatePayloadRegistry(configuration.TransportMode),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration(
        RtpSessionTransportMode transportMode)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = transportMode;
        configuration.RtcpCompoundMode = RtcpCompoundMode.Full;
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
        RtpPayloadTypeDefinition video = new RtpPayloadTypeDefinition();
        video.PayloadType = 96;
        video.ClockRate = 90000;
        video.MediaIdentity = "video";
        video.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(video);
        RtpPayloadTypeDefinition audio = new RtpPayloadTypeDefinition();
        audio.PayloadType = 0;
        audio.ClockRate = 8000;
        audio.MediaIdentity = "audio";
        audio.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(audio);
        return registry;
    }

    static RtpPacket CreatePacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp,
        byte payloadType)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = payloadType;
        packet.Marker = true;
        packet.Payload = new byte[] { 1, 2, 3 };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long operationTimeTicks,
        string transportIdentity,
        int clockRate,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    static RtpFeedbackRequest CreatePictureLossRequest(
        uint mediaSourceIdentifier)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat =
            RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        RtcpPictureLossIndication pictureLoss =
            new RtcpPictureLossIndication();
        pictureLoss.Requested = true;
        feedback.PictureLossIndication = pictureLoss;
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = feedback;
        request.PreferredTimingMode = RtcpFeedbackTimingMode.Immediate;
        request.ExpirationTimeTicks = 100L;
        return request;
    }

    static RtcpCompoundPacket CreateReceiverReportCompound(
        uint sourceIdentifier,
        RtcpReceptionReport[] reports)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource = sourceIdentifier;
        receiverReport.ReceptionReports = reports;
        RtcpPacket reportPacket = new RtcpPacket();
        reportPacket.Type = RtcpPacketType.ReceiverReport;
        reportPacket.ReceiverReport = receiverReport;

        RtcpSourceDescriptionItem canonicalName =
            new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = Encoding.UTF8.GetBytes("remote@example");
        RtcpSourceDescriptionChunk chunk =
            new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket descriptionPacket = new RtcpPacket();
        descriptionPacket.Type = RtcpPacketType.SourceDescription;
        descriptionPacket.SourceDescription = description;

        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            reportPacket,
            descriptionPacket
        };
        return compound;
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
        uint nextSourceIdentifier = 100U;

        public uint CreateSynchronizationSource()
        {
            uint sourceIdentifier = nextSourceIdentifier;
            nextSourceIdentifier = nextSourceIdentifier + 1U;
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
}
