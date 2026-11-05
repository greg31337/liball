using rtp;

namespace rtp.Tests.Api;

public class RtpSessionModelTests
{
    [Test]
    public void SessionModelsProvideUsableCollectionAndConfigurationDefaults()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        RtpJitterBufferUpdate jitter = new RtpJitterBufferUpdate();
        RtpRepairRequest repairRequest = new RtpRepairRequest();
        RtpRepairResult repairResult = new RtpRepairResult();
        RtpPacketizationResult packetization = new RtpPacketizationResult();
        RtpDepacketizationResult depacketization = new RtpDepacketizationResult();
        RtpSessionUpdate update = new RtpSessionUpdate();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.StrictValidation, Is.True);
            Assert.That(configuration.PreservePadding, Is.True);
            Assert.That(configuration.MinimumSequential,
                Is.EqualTo(RtpProtocolConstants.DefaultMinimumSequential));
            Assert.That(configuration.RtcpScheduler, Is.Not.Null);
            Assert.That(configuration.JitterBuffer, Is.Not.Null);
            Assert.That(configuration.JitterBuffer.MaximumDropout,
                Is.EqualTo(RtpProtocolConstants.DefaultMaximumDropout));
            Assert.That(configuration.JitterBuffer.MaximumMisorder,
                Is.EqualTo(RtpProtocolConstants.DefaultMaximumMisorder));
            Assert.That(configuration.RtcpScheduler.RtcpBandwidthFraction,
                Is.EqualTo(RtcpProtocolConstants.RecommendedBandwidthFraction));
            Assert.That(source.RemoteReports, Is.Empty);
            Assert.That(snapshot.Configuration, Is.Not.Null);
            Assert.That(snapshot.Sources, Is.Empty);
            Assert.That(snapshot.LocalSourceIdentifiers, Is.Empty);
            Assert.That(jitter.ReleasedPackets, Is.Empty);
            Assert.That(jitter.LostSequenceNumbers, Is.Empty);
            Assert.That(jitter.RepairRequests, Is.Empty);
            Assert.That(jitter.FeedbackRequests, Is.Empty);
            Assert.That(repairRequest.SequenceNumbers, Is.Empty);
            Assert.That(repairRequest.Data, Is.Empty);
            Assert.That(repairResult.RepairedPackets, Is.Empty);
            Assert.That(repairResult.UnrepairedSequenceNumbers, Is.Empty);
            Assert.That(repairResult.Data, Is.Empty);
            Assert.That(packetization.Payloads, Is.Empty);
            Assert.That(packetization.Markers, Is.Empty);
            Assert.That(depacketization.Payload, Is.Empty);
            Assert.That(update.Events, Is.Empty);
            Assert.That(update.AcceptedPackets, Is.Empty);
            Assert.That(update.ReleasedPackets, Is.Empty);
            Assert.That(update.OutboundPackets, Is.Empty);
            Assert.That(update.OutboundControlPackets, Is.Empty);
            Assert.That(update.FeedbackRequests, Is.Empty);
            Assert.That(update.FeedbackDecisions, Is.Empty);
            Assert.That(update.ControlScheduleDecisions, Is.Empty);
            Assert.That(update.RepairRequests, Is.Empty);
            Assert.That(update.RepairResults, Is.Empty);
        });
    }

    [Test]
    public void ConfigurationCopyIsolatesNestedSchedulerAndJitterSettings()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Multiplexed;
        configuration.StrictValidation = false;
        configuration.PreservePadding = false;
        configuration.RtcpCompoundMode = RtcpCompoundMode.MinimalFeedback;
        configuration.MaximumRtpPacketSize = 1400;
        configuration.MaximumRtcpCompoundPacketSize = 4096;
        configuration.MaximumSourceCount = 200;
        configuration.MaximumPacketHistoryCount = 100;
        configuration.MaximumPacketHistoryBytes = 100000;
        configuration.MaximumPacketHistoryAgeTicks = 3000L;
        configuration.MinimumSequential = 3;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 1000000.0;
        configuration.RtcpScheduler.SenderBandwidthFraction = 0.2;
        configuration.RtcpScheduler.MinimumFeedbackIntervalTicks = 500L;
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.RtcpScheduler.ReducedSizeEnabled = true;
        configuration.JitterBuffer.Capacity = 64;
        configuration.JitterBuffer.MaximumLatencyTicks = 2000L;
        configuration.JitterBuffer.ReleaseLatePackets = true;
        configuration.JitterBuffer.NackDelayTicks = 100L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 250L;
        configuration.JitterBuffer.MaximumNackRetryCount = 4;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 2000L;

        RtpSessionConfiguration copy = configuration.Copy();
        configuration.CanonicalName = "changed";
        configuration.RtcpScheduler.SenderBandwidthFraction = 0.9;
        configuration.JitterBuffer.Capacity = 1;

        Assert.Multiple(() =>
        {
            Assert.That(copy.TransportMode, Is.EqualTo(RtpSessionTransportMode.Multiplexed));
            Assert.That(copy.StrictValidation, Is.False);
            Assert.That(copy.PreservePadding, Is.False);
            Assert.That(copy.RtcpCompoundMode,
                Is.EqualTo(RtcpCompoundMode.MinimalFeedback));
            Assert.That(copy.MaximumRtpPacketSize, Is.EqualTo(1400));
            Assert.That(copy.MaximumRtcpCompoundPacketSize, Is.EqualTo(4096));
            Assert.That(copy.MaximumSourceCount, Is.EqualTo(200));
            Assert.That(copy.MaximumPacketHistoryCount, Is.EqualTo(100));
            Assert.That(copy.MaximumPacketHistoryBytes, Is.EqualTo(100000));
            Assert.That(copy.MaximumPacketHistoryAgeTicks, Is.EqualTo(3000L));
            Assert.That(copy.MinimumSequential, Is.EqualTo(3));
            Assert.That(copy.CanonicalName, Is.EqualTo("local@example"));
            Assert.That(copy.RtcpScheduler, Is.Not.SameAs(configuration.RtcpScheduler));
            Assert.That(copy.RtcpScheduler.SessionBandwidthBitsPerSecond,
                Is.EqualTo(1000000.0));
            Assert.That(copy.RtcpScheduler.SenderBandwidthFraction, Is.EqualTo(0.2));
            Assert.That(copy.RtcpScheduler.MinimumFeedbackIntervalTicks, Is.EqualTo(500L));
            Assert.That(copy.RtcpScheduler.FeedbackEnabled, Is.True);
            Assert.That(copy.RtcpScheduler.ReducedSizeEnabled, Is.True);
            Assert.That(copy.JitterBuffer, Is.Not.SameAs(configuration.JitterBuffer));
            Assert.That(copy.JitterBuffer.Capacity, Is.EqualTo(64));
            Assert.That(copy.JitterBuffer.MaximumLatencyTicks, Is.EqualTo(2000L));
            Assert.That(copy.JitterBuffer.ReleaseLatePackets, Is.True);
            Assert.That(copy.JitterBuffer.NackDelayTicks, Is.EqualTo(100L));
            Assert.That(copy.JitterBuffer.NackRetryIntervalTicks, Is.EqualTo(250L));
            Assert.That(copy.JitterBuffer.MaximumNackRetryCount, Is.EqualTo(4));
            Assert.That(copy.JitterBuffer.MaximumNackRetryPeriodTicks, Is.EqualTo(2000L));
        });
    }

    [Test]
    public void SessionSnapshotCopyIsolatesTrackingAndStatisticsGraphs()
    {
        RtpSequenceUpdate sequence = new RtpSequenceUpdate();
        sequence.Status = RtpSequenceStatus.Reordered;
        sequence.SequenceNumber = 2;
        sequence.ExtendedSequenceNumber = 65538U;
        sequence.BaseExtendedSequenceNumber = 65530U;
        sequence.HighestExtendedSequenceNumber = 65540U;
        sequence.ProbationRemaining = 0;
        sequence.Accepted = true;
        RtpTimestampUpdate timestamp = new RtpTimestampUpdate();
        timestamp.Timestamp = 3U;
        timestamp.ExtendedTimestamp = 4294967299L;
        timestamp.Delta = -10L;
        timestamp.ClockRate = 90000;
        timestamp.Accepted = true;
        timestamp.IsReordered = true;
        timestamp.ClockRateChanged = true;
        RtpClockCorrelation correlation = new RtpClockCorrelation();
        correlation.IsValid = true;
        correlation.SourceIdentifier = 0x11111111U;
        correlation.RtpTimestamp = 3U;
        correlation.ExtendedRtpTimestamp = 4294967299L;
        correlation.NtpTimestamp = 0x0123456789ABCDEFUL;
        correlation.ClockRate = 90000;
        correlation.PresentationTimeTicks = 123456L;
        RtpReceptionStatisticsSnapshot reception = new RtpReceptionStatisticsSnapshot();
        reception.SourceIdentifier = 0x11111111U;
        reception.ExpectedPacketCount = 100L;
        reception.ReceivedPacketCount = 95L;
        reception.ReceivedOctetCount = 9500L;
        reception.DuplicatePacketCount = 2L;
        reception.ReorderedPacketCount = 3L;
        reception.RepairedPacketCount = 4L;
        reception.RecoveredPacketCount = 3L;
        reception.CumulativePacketsLost = 5;
        reception.FractionLost = 12;
        reception.InterarrivalJitter = 44U;
        reception.TimedOut = true;
        RtpTransmissionStatisticsSnapshot transmission =
            new RtpTransmissionStatisticsSnapshot();
        transmission.SourceIdentifier = 0x22222222U;
        transmission.SentPacketCount = 80L;
        transmission.SentOctetCount = 8000L;
        transmission.LastRtpTimestamp = 900U;
        transmission.LastSendTimeTicks = 700L;
        transmission.IsActiveSender = true;
        transmission.SenderReportEligible = true;
        RtpRemoteReportSnapshot remote = new RtpRemoteReportSnapshot();
        remote.ReporterSourceIdentifier = 0x33333333U;
        remote.ReportedSourceIdentifier = 0x22222222U;
        remote.FractionLost = 9;
        remote.CumulativePacketsLost = -2;
        remote.ExtendedHighestSequenceNumber = 1000U;
        remote.InterarrivalJitter = 20U;
        remote.HasRoundTripTime = true;
        remote.RoundTripTimeTicks = 42L;
        remote.ArrivalTimeTicks = 800L;
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        source.SourceIdentifier = 0x11111111U;
        source.Status = RtpSourceStatus.ActiveSender;
        source.IsSender = true;
        source.CanonicalName = "remote@example";
        source.TransportIdentity = "path-a";
        source.PayloadType = 96;
        source.ClockRate = 90000;
        source.SequenceUpdate = sequence;
        source.TimestampUpdate = timestamp;
        source.ClockCorrelation = correlation;
        source.ReceptionStatistics = reception;
        source.TransmissionStatistics = transmission;
        source.RemoteReports = new RtpRemoteReportSnapshot[] { remote };
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.Configuration.CanonicalName = "local@example";
        snapshot.Sources = new RtpSourceSnapshot[] { source };
        snapshot.LocalSourceIdentifiers = new uint[] { 0x22222222U };
        snapshot.MemberCount = 3;
        snapshot.SenderCount = 2;
        snapshot.AverageRtcpPacketSize = 128;
        snapshot.SnapshotTimeTicks = 900L;
        snapshot.NextRtcpSendTimeTicks = 1000L;

        RtpSessionSnapshot copy = snapshot.Copy();
        sequence.ExtendedSequenceNumber = 0U;
        timestamp.ExtendedTimestamp = 0L;
        correlation.NtpTimestamp = 0UL;
        reception.ReceivedPacketCount = 0L;
        transmission.SentPacketCount = 0L;
        remote.RoundTripTimeTicks = 0L;
        snapshot.LocalSourceIdentifiers[0] = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Sources, Is.Not.SameAs(snapshot.Sources));
            Assert.That(copy.Sources[0], Is.Not.SameAs(source));
            Assert.That(copy.Sources[0].Status, Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(copy.Sources[0].SequenceUpdate?.Status,
                Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(copy.Sources[0].SequenceUpdate?.ExtendedSequenceNumber,
                Is.EqualTo(65538U));
            Assert.That(copy.Sources[0].TimestampUpdate?.ExtendedTimestamp,
                Is.EqualTo(4294967299L));
            Assert.That(copy.Sources[0].TimestampUpdate?.Delta, Is.EqualTo(-10L));
            Assert.That(copy.Sources[0].ClockCorrelation?.NtpTimestamp,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(copy.Sources[0].ClockCorrelation?.PresentationTimeTicks,
                Is.EqualTo(123456L));
            Assert.That(copy.Sources[0].ReceptionStatistics?.ReceivedPacketCount,
                Is.EqualTo(95L));
            Assert.That(copy.Sources[0].ReceptionStatistics?.CumulativePacketsLost,
                Is.EqualTo(5));
            Assert.That(copy.Sources[0].TransmissionStatistics?.SentPacketCount,
                Is.EqualTo(80L));
            Assert.That(copy.Sources[0].TransmissionStatistics?.SenderReportEligible,
                Is.True);
            Assert.That(copy.Sources[0].RemoteReports[0].RoundTripTimeTicks,
                Is.EqualTo(42L));
            Assert.That(copy.Sources[0].RemoteReports,
                Is.Not.SameAs(source.RemoteReports));
            Assert.That(copy.Configuration, Is.Not.SameAs(snapshot.Configuration));
            Assert.That(copy.Configuration.CanonicalName, Is.EqualTo("local@example"));
            Assert.That(copy.LocalSourceIdentifiers, Is.EqualTo(new uint[] { 0x22222222U }));
            Assert.That(copy.MemberCount, Is.EqualTo(3));
            Assert.That(copy.SenderCount, Is.EqualTo(2));
            Assert.That(copy.AverageRtcpPacketSize, Is.EqualTo(128));
            Assert.That(copy.SnapshotTimeTicks, Is.EqualTo(900L));
            Assert.That(copy.NextRtcpSendTimeTicks, Is.EqualTo(1000L));
        });
    }

    [Test]
    public void SchedulingCopiesOwnFeedbackAndControlPackets()
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.RawFeedbackControlInformation = new byte[] { 1, 2, 3, 4 };
        RtpFeedbackRequest request = new RtpFeedbackRequest();
        request.Feedback = feedback;
        request.EarliestSendTimeTicks = 10L;
        request.ExpirationTimeTicks = 20L;
        request.PreferredTimingMode = RtcpFeedbackTimingMode.Immediate;
        request.AllowSuppression = false;
        RtcpPacket controlPacket = new RtcpPacket();
        controlPacket.Padding = new byte[] { 0, 2 };
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { controlPacket };
        RtpFeedbackDecision feedbackDecision = new RtpFeedbackDecision();
        feedbackDecision.ShouldSend = true;
        feedbackDecision.Piggyback = true;
        feedbackDecision.TimingMode = RtcpFeedbackTimingMode.Early;
        feedbackDecision.ScheduledTimeTicks = 15L;
        feedbackDecision.Request = request;
        feedbackDecision.CompoundPacket = compound;
        RtcpScheduleDecision schedule = new RtcpScheduleDecision();
        schedule.ShouldSend = true;
        schedule.IsInitial = true;
        schedule.IsSender = true;
        schedule.ScheduledTimeTicks = 15L;
        schedule.DelayTicks = 5L;
        schedule.MemberCount = 4;
        schedule.SenderCount = 2;
        schedule.CompoundPacket = compound;

        RtpFeedbackDecision feedbackCopy = feedbackDecision.Copy();
        RtcpScheduleDecision scheduleCopy = schedule.Copy();
        feedback.RawFeedbackControlInformation[0] = 99;
        controlPacket.Padding[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(feedbackCopy.ShouldSend, Is.True);
            Assert.That(feedbackCopy.Piggyback, Is.True);
            Assert.That(feedbackCopy.TimingMode, Is.EqualTo(RtcpFeedbackTimingMode.Early));
            Assert.That(feedbackCopy.ScheduledTimeTicks, Is.EqualTo(15L));
            Assert.That(feedbackCopy.Request, Is.Not.SameAs(request));
            Assert.That(feedbackCopy.Request?.EarliestSendTimeTicks, Is.EqualTo(10L));
            Assert.That(feedbackCopy.Request?.ExpirationTimeTicks, Is.EqualTo(20L));
            Assert.That(feedbackCopy.Request?.AllowSuppression, Is.False);
            Assert.That(feedbackCopy.Request?.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(feedbackCopy.CompoundPacket?.Packets[0].Padding,
                Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(scheduleCopy.ShouldSend, Is.True);
            Assert.That(scheduleCopy.IsInitial, Is.True);
            Assert.That(scheduleCopy.IsSender, Is.True);
            Assert.That(scheduleCopy.ScheduledTimeTicks, Is.EqualTo(15L));
            Assert.That(scheduleCopy.DelayTicks, Is.EqualTo(5L));
            Assert.That(scheduleCopy.MemberCount, Is.EqualTo(4));
            Assert.That(scheduleCopy.SenderCount, Is.EqualTo(2));
            Assert.That(scheduleCopy.CompoundPacket, Is.Not.SameAs(compound));
            Assert.That(scheduleCopy.CompoundPacket?.Packets[0].Padding,
                Is.EqualTo(new byte[] { 0, 2 }));
        });
    }

    [Test]
    public void BufferRepairAndPayloadResultCopiesOwnPacketAndByteData()
    {
        RtpPacket packet = new RtpPacket();
        packet.SequenceNumber = 100;
        packet.Payload = new byte[] { 10, 11 };
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = RtpRepairKind.Retransmission;
        request.SourceIdentifier = 0x12345678U;
        request.SequenceNumbers = new ushort[] { 98, 99 };
        request.AttemptCount = 2;
        request.RequestTimeTicks = 1000L;
        request.DeadlineTicks = 2000L;
        request.Data = new byte[] { 1, 2 };
        RtpJitterBufferUpdate jitter = new RtpJitterBufferUpdate();
        jitter.Status = RtpJitterBufferStatus.Released;
        jitter.ReleasedPackets = new RtpPacket[] { packet };
        jitter.RecoveredPackets = new RtpPacket[] { packet };
        jitter.LostSequenceNumbers = new ushort[] { 98 };
        jitter.RepairRequests = new RtpRepairRequest[] { request };
        jitter.BufferedPacketCount = 4;
        jitter.BufferedOctetCount = 800;
        jitter.ReleasedPacketCount = 10L;
        jitter.DuplicatePacketCount = 2L;
        jitter.LatePacketCount = 3L;
        jitter.LostPacketCount = 4L;
        jitter.OverflowPacketCount = 5L;
        jitter.RecoveredPacketCount = 6L;
        RtpRepairResult repair = new RtpRepairResult();
        repair.Success = true;
        repair.Kind = RtpRepairKind.Retransmission;
        repair.RepairedPackets = new RtpPacket[] { packet };
        repair.UnrepairedSequenceNumbers = new ushort[] { 99 };
        repair.Data = new byte[] { 3, 4 };
        repair.ErrorMessage = "partial";
        RtpPacketizationResult packetization = new RtpPacketizationResult();
        packetization.Success = true;
        packetization.ConsumedCount = 2;
        packetization.Payloads = new byte[][] { new byte[] { 10, 11 } };
        packetization.Markers = new bool[] { true };
        RtpDepacketizationResult depacketization = new RtpDepacketizationResult();
        depacketization.Success = true;
        depacketization.ConsumedPacketCount = 1;
        depacketization.Payload = new byte[] { 10, 11 };
        depacketization.Timestamp = 200U;
        depacketization.Marker = true;

        RtpJitterBufferUpdate jitterCopy = jitter.Copy();
        RtpRepairResult repairCopy = repair.Copy();
        RtpPacketizationResult packetizationCopy = packetization.Copy();
        RtpDepacketizationResult depacketizationCopy = depacketization.Copy();
        packet.Payload[0] = 99;
        request.SequenceNumbers[0] = 0;
        request.Data[0] = 0;
        jitter.LostSequenceNumbers[0] = 0;
        repair.UnrepairedSequenceNumbers[0] = 0;
        repair.Data[0] = 0;
        packetization.Payloads[0][0] = 0;
        packetization.Markers[0] = false;
        depacketization.Payload[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(jitterCopy.Status, Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(jitterCopy.ReleasedPackets[0].Payload,
                Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(jitterCopy.RecoveredPackets[0].Payload,
                Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(jitterCopy.RecoveredPackets[0],
                Is.Not.SameAs(jitterCopy.ReleasedPackets[0]));
            Assert.That(jitterCopy.LostSequenceNumbers, Is.EqualTo(new ushort[] { 98 }));
            Assert.That(jitterCopy.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 98, 99 }));
            Assert.That(jitterCopy.RepairRequests[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(jitterCopy.BufferedPacketCount, Is.EqualTo(4));
            Assert.That(jitterCopy.BufferedOctetCount, Is.EqualTo(800));
            Assert.That(jitterCopy.ReleasedPacketCount, Is.EqualTo(10L));
            Assert.That(jitterCopy.DuplicatePacketCount, Is.EqualTo(2L));
            Assert.That(jitterCopy.LatePacketCount, Is.EqualTo(3L));
            Assert.That(jitterCopy.LostPacketCount, Is.EqualTo(4L));
            Assert.That(jitterCopy.OverflowPacketCount, Is.EqualTo(5L));
            Assert.That(jitterCopy.RecoveredPacketCount, Is.EqualTo(6L));
            Assert.That(repairCopy.Success, Is.True);
            Assert.That(repairCopy.RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(repairCopy.UnrepairedSequenceNumbers,
                Is.EqualTo(new ushort[] { 99 }));
            Assert.That(repairCopy.Data, Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(repairCopy.ErrorMessage, Is.EqualTo("partial"));
            Assert.That(packetizationCopy.ConsumedCount, Is.EqualTo(2));
            Assert.That(packetizationCopy.Payloads[0],
                Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(packetizationCopy.Markers, Is.EqualTo(new bool[] { true }));
            Assert.That(depacketizationCopy.ConsumedPacketCount, Is.EqualTo(1));
            Assert.That(depacketizationCopy.Payload, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(depacketizationCopy.Timestamp, Is.EqualTo(200U));
            Assert.That(depacketizationCopy.Marker, Is.True);
        });
    }

    [Test]
    public void SessionUpdateCopyIsolatesAggregateEventsAndOutboundWork()
    {
        RtpPacket packet = new RtpPacket();
        packet.Payload = new byte[] { 7, 8 };
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = 100L;
        context.TransportIdentity = "path-b";
        context.IsRepaired = true;
        context.IsLate = true;
        context.ClockRate = 48000;
        context.MediaClockTimestamp = 480L;
        RtpSequenceUpdate sequence = new RtpSequenceUpdate();
        sequence.Status = RtpSequenceStatus.Accepted;
        sequence.Accepted = true;
        RtpTimestampUpdate timestamp = new RtpTimestampUpdate();
        timestamp.Accepted = true;
        RtpClockCorrelation correlation = new RtpClockCorrelation();
        correlation.IsValid = true;
        RtpRepairRequest request = new RtpRepairRequest();
        request.SequenceNumbers = new ushort[] { 10 };
        RtpRepairResult repair = new RtpRepairResult();
        repair.RepairedPackets = new RtpPacket[] { packet };
        RtpJitterBufferUpdate jitter = new RtpJitterBufferUpdate();
        jitter.ReleasedPackets = new RtpPacket[] { packet };
        jitter.RepairRequests = new RtpRepairRequest[] { request };
        RtpPacketizationResult packetization = new RtpPacketizationResult();
        packetization.Payloads = new byte[][] { new byte[] { 7, 8 } };
        packetization.Markers = new bool[] { true };
        RtpDepacketizationResult depacketization = new RtpDepacketizationResult();
        depacketization.Payload = new byte[] { 7, 8 };
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        source.SourceIdentifier = 0xABCDEF01U;
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SequenceStatus = RtpSequenceStatus.Accepted;
        sessionEvent.SourceStatus = RtpSourceStatus.ActiveSender;
        sessionEvent.JitterBufferStatus = RtpJitterBufferStatus.Released;
        sessionEvent.RepairKind = RtpRepairKind.Retransmission;
        sessionEvent.SourceIdentifier = 0xABCDEF01U;
        sessionEvent.Message = "recovered";
        sessionEvent.PacketContext = context;
        sessionEvent.Packet = packet;
        sessionEvent.HeaderExtension = new RtpHeaderExtension();
        sessionEvent.HeaderExtension.Data = new byte[] { 9 };
        sessionEvent.Configuration = new RtpSessionConfiguration();
        sessionEvent.Configuration.CanonicalName = "event@example";
        sessionEvent.Source = source;
        sessionEvent.SequenceUpdate = sequence;
        sessionEvent.TimestampUpdate = timestamp;
        sessionEvent.ClockCorrelation = correlation;
        sessionEvent.JitterBufferUpdate = jitter;
        sessionEvent.RepairRequest = request;
        sessionEvent.RepairResult = repair;
        sessionEvent.PacketizationResult = packetization;
        sessionEvent.DepacketizationResult = depacketization;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { new RtcpPacket() };
        compound.Packets[0].Padding = new byte[] { 0, 2 };
        sessionEvent.ControlPacket = compound.Packets[0];
        RtpFeedbackRequest feedbackRequest = new RtpFeedbackRequest();
        feedbackRequest.Feedback = new RtcpFeedbackPacket();
        RtpFeedbackDecision feedbackDecision = new RtpFeedbackDecision();
        feedbackDecision.Request = feedbackRequest;
        sessionEvent.FeedbackDecision = feedbackDecision;
        RtcpScheduleDecision schedule = new RtcpScheduleDecision();
        schedule.CompoundPacket = compound;
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.Sources = new RtpSourceSnapshot[] { source };
        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = new RtpSessionEvent[] { sessionEvent };
        update.AcceptedPackets = new RtpPacket[] { packet };
        update.ReleasedPackets = new RtpPacket[] { packet };
        update.OutboundPackets = new RtpPacket[] { packet };
        update.OutboundControlPackets = new RtcpCompoundPacket[] { compound };
        update.FeedbackRequests = new RtpFeedbackRequest[] { feedbackRequest };
        update.FeedbackDecisions = new RtpFeedbackDecision[] { feedbackDecision };
        update.ControlScheduleDecisions = new RtcpScheduleDecision[] { schedule };
        update.RepairRequests = new RtpRepairRequest[] { request };
        update.RepairResults = new RtpRepairResult[] { repair };
        update.Snapshot = snapshot;

        RtpSessionUpdate copy = update.Copy();
        packet.Payload[0] = 99;
        context.TransportIdentity = "changed";
        sessionEvent.HeaderExtension.Data[0] = 0;
        compound.Packets[0].Padding[0] = 99;
        request.SequenceNumbers[0] = 0;
        depacketization.Payload[0] = 0;
        source.SourceIdentifier = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Events, Is.Not.SameAs(update.Events));
            Assert.That(copy.Events[0].Type, Is.EqualTo(RtpSessionEventType.Repair));
            Assert.That(copy.Events[0].SourceIdentifier, Is.EqualTo(0xABCDEF01U));
            Assert.That(copy.Events[0].Message, Is.EqualTo("recovered"));
            Assert.That(copy.Events[0].PacketContext?.TransportIdentity, Is.EqualTo("path-b"));
            Assert.That(copy.Events[0].PacketContext?.IsRepaired, Is.True);
            Assert.That(copy.Events[0].PacketContext?.IsLate, Is.True);
            Assert.That(copy.Events[0].PacketContext?.MediaClockTimestamp, Is.EqualTo(480L));
            Assert.That(copy.Events[0].Packet?.Payload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.Events[0].HeaderExtension?.Data, Is.EqualTo(new byte[] { 9 }));
            Assert.That(copy.Events[0].ControlPacket?.Padding, Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(copy.Events[0].FeedbackDecision?.Request, Is.Not.Null);
            Assert.That(copy.Events[0].Configuration?.CanonicalName,
                Is.EqualTo("event@example"));
            Assert.That(copy.Events[0].Source?.SourceIdentifier, Is.EqualTo(0xABCDEF01U));
            Assert.That(copy.Events[0].SequenceUpdate?.Accepted, Is.True);
            Assert.That(copy.Events[0].TimestampUpdate?.Accepted, Is.True);
            Assert.That(copy.Events[0].ClockCorrelation?.IsValid, Is.True);
            Assert.That(copy.Events[0].JitterBufferUpdate?.ReleasedPackets[0].Payload,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.Events[0].RepairRequest?.SequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(copy.Events[0].RepairResult?.RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.Events[0].PacketizationResult?.Payloads[0],
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.Events[0].DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.AcceptedPackets[0].Payload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.ReleasedPackets[0].Payload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.OutboundPackets[0].Payload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.OutboundControlPackets[0], Is.Not.SameAs(compound));
            Assert.That(copy.FeedbackRequests[0], Is.Not.SameAs(feedbackRequest));
            Assert.That(copy.FeedbackDecisions[0], Is.Not.SameAs(feedbackDecision));
            Assert.That(copy.ControlScheduleDecisions[0], Is.Not.SameAs(schedule));
            Assert.That(copy.RepairRequests[0].SequenceNumbers,
                Is.EqualTo(new ushort[] { 10 }));
            Assert.That(copy.RepairResults[0].RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.Snapshot?.Sources[0].SourceIdentifier, Is.EqualTo(0xABCDEF01U));
        });
    }
}
