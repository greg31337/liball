using rtp;

namespace rtp.Tests.Session;

public class RtpSessionLimitTests
{
    [Test]
    public void PacketSizeLimitsReturnRecoverableUpdatesWithoutSourceState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        configuration.MaximumRtpPacketSize = 16;
        configuration.MaximumRtcpCompoundPacketSize = 8;
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        RtpSessionConfiguration encodingConfiguration =
            CreateConfiguration();
        encodingConfiguration.RtcpCompoundMode =
            RtcpCompoundMode.ReducedSize;
        RtpPacket oversizedPacket = CreatePacket(200U, 10, 1000U);
        oversizedPacket.Payload = new byte[] { 1, 2, 3, 4, 5 };
        byte[] oversizedRtp = new RtpPacketCodec(
            encodingConfiguration).EncodeRtpPacketToArray(
                oversizedPacket);
        RtcpCompoundPacket oversizedCompound = new RtcpCompoundPacket();
        oversizedCompound.Packets = new RtcpPacket[]
        {
            CreateReceiverReportPacket(300U),
            CreateReceiverReportPacket(400U)
        };
        byte[] oversizedRtcp = new RtcpPacketCodec(
            encodingConfiguration).EncodeRtcpCompoundPacketToArray(
                oversizedCompound);

        RtpSessionUpdate rtpUpdate = session.ProcessInboundRtpDatagram(
            oversizedRtp,
            CreateContext(1L, "rtp"));
        RtpSessionUpdate rtcpUpdate = session.ProcessInboundRtcpDatagram(
            oversizedRtcp,
            CreateContext(1L, "rtcp"));
        clock.TimeTicks = 1L;
        RtpSessionSnapshot snapshot = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(rtpUpdate.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(rtpUpdate.Events[0].Message,
                Does.Contain("maximum"));
            Assert.That(rtcpUpdate.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(rtcpUpdate.Events[0].Message,
                Does.Contain("limit"));
            Assert.That(snapshot.Sources.Count(value => !value.IsLocal),
                Is.Zero);
        });
    }

    [Test]
    public void RtpSourceCapacityIsPreflightedForPrimaryAndContributors()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.MaximumSourceCount = 1;
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        RtpPacket tooManySources = CreatePacket(200U, 10, 1000U);
        tooManySources.ContributingSources = new uint[] { 300U };
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);

        RtpSessionUpdate failed = session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(tooManySources),
            CreateContext(1L, "remote"));
        RtpSourceSnapshot[] afterFailure = session.ingress
            .remoteSourceTable.GetRemoteSourceSnapshots();
        RtpSessionUpdate accepted = session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 10, 1000U)),
            CreateContext(2L, "remote"));
        clock.TimeTicks = 2L;
        RtpSessionSnapshot snapshot = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(failed.Events[0].Message,
                Does.Contain("source table capacity"));
            Assert.That(failed.AcceptedPackets, Is.Empty);
            Assert.That(afterFailure, Is.Empty);
            Assert.That(accepted.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(snapshot.Sources.Count(value => !value.IsLocal),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RtcpSourceCapacityRejectsWholeCompoundBeforeMutation()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        configuration.MaximumSourceCount = 1;
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            CreateReceiverReportPacket(300U),
            CreateReceiverReportPacket(400U)
        };
        IRtcpPacketCodec codec = new RtcpPacketCodec(configuration);

        RtpSessionUpdate failed = session.ProcessInboundRtcpDatagram(
            codec.EncodeRtcpCompoundPacketToArray(compound),
            CreateContext(1L, "control"));
        RtpSourceSnapshot[] afterFailure = session.ingress
            .remoteSourceTable.GetRemoteSourceSnapshots();
        RtcpCompoundPacket validCompound = new RtcpCompoundPacket();
        validCompound.Packets = new RtcpPacket[]
        {
            CreateReceiverReportPacket(300U)
        };
        RtpSessionUpdate accepted = session.ProcessInboundRtcpDatagram(
            codec.EncodeRtcpCompoundPacketToArray(validCompound),
            CreateContext(2L, "control"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(failed.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(afterFailure, Is.Empty);
            Assert.That(accepted.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Report));
            Assert.That(session.ingress.remoteSourceTable
                .GetRemoteSourceSnapshots(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void RepairBatchCapacityFailureDoesNotApplyEarlierPackets()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.MaximumSourceCount = 1;
        FixedClock clock = new FixedClock();
        clock.TimeTicks = 1L;
        RtpSession session = CreateSession(configuration, clock);
        RtpRepairResult result = new RtpRepairResult();
        result.Success = true;
        result.Kind = RtpRepairKind.ForwardErrorCorrection;
        result.RepairedPackets = new RtpPacket[]
        {
            CreatePacket(200U, 10, 1000U),
            CreatePacket(300U, 10, 1000U)
        };

        RtpSessionUpdate update = session.ApplySessionRepairResult(
            result,
            CreateContext(1L, "repair"));

        Assert.Multiple(() =>
        {
            Assert.That(update.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(update.AcceptedPackets, Is.Empty);
            Assert.That(update.RepairResults, Has.Length.EqualTo(1));
            Assert.That(update.Snapshot?.Sources.Count(
                value => !value.IsLocal), Is.Zero);
            Assert.That(session.ingress.remoteSourceTable
                .GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    [Test]
    public void JitterOverflowReturnsRecoverableEventAndRemainsBounded()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.JitterBuffer.Capacity = 1;
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);

        session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 10, 1000U)),
            CreateContext(1L, "remote"));
        session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 12, 3000U)),
            CreateContext(2L, "remote"));
        RtpSessionUpdate overflow = session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 13, 4000U)),
            CreateContext(3L, "remote"));
        RtpSessionEvent capacityEvent = overflow.Events.Single(
            value => value.Type == RtpSessionEventType.RecoverableError);

        Assert.Multiple(() =>
        {
            Assert.That(overflow.AcceptedPackets, Has.Length.EqualTo(1));
            Assert.That(capacityEvent.JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Overflow));
            Assert.That(capacityEvent.JitterBufferUpdate?
                .BufferedPacketCount, Is.EqualTo(1));
            Assert.That(capacityEvent.JitterBufferUpdate?
                .OverflowPacketCount, Is.EqualTo(1L));
            Assert.That(capacityEvent.Message, Does.Contain("capacity"));
        });
    }

    [Test]
    public void HistoryCapacityFailureIsReportedWithoutDroppingOutboundPacket()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.MaximumPacketHistoryCount = 1;
        configuration.MaximumPacketHistoryBytes = 12;
        FixedClock clock = new FixedClock();
        clock.TimeTicks = 1L;
        RtpSession session = CreateSession(configuration, clock);

        RtpSessionUpdate update = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(1L, "local"));
        uint localSourceIdentifier = update.OutboundPackets.Single()
            .SynchronizationSource;
        RtpSessionEvent capacityEvent = update.Events.Single(
            value => value.Type == RtpSessionEventType.RecoverableError);

        Assert.Multiple(() =>
        {
            Assert.That(update.OutboundPackets, Has.Length.EqualTo(1));
            Assert.That(capacityEvent.Message,
                Does.Contain("history limits"));
            Assert.That(session.outboundStream.packetHistory
                .FindRtpHistoryPacket(
                    localSourceIdentifier,
                    update.OutboundPackets[0].SequenceNumber),
                Is.Null);
            Assert.That(session.GetSessionSnapshot().Sources.Single(
                value => value.IsLocal).TransmissionStatistics?
                .SentPacketCount, Is.EqualTo(1L));
        });
    }

    [Test]
    public void FeedbackRetryLimitIsHonoredAcrossSessionIngress()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.JitterBuffer.MaximumLatencyTicks = 100L;
        configuration.JitterBuffer.NackDelayTicks = 0L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 10L;
        configuration.JitterBuffer.MaximumNackRetryCount = 1;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(configuration, clock);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);

        session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 10, 1000U)),
            CreateContext(0L, "remote"));
        RtpSessionUpdate firstGap = session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreatePacket(200U, 12, 3000U)),
            CreateContext(1L, "remote"));
        RtpSessionUpdate afterRetryLimit =
            session.ProcessInboundRtpDatagram(
                codec.EncodeRtpPacketToArray(
                    CreatePacket(200U, 13, 4000U)),
                CreateContext(2L, "remote"));

        Assert.Multiple(() =>
        {
            Assert.That(firstGap.FeedbackRequests,
                Has.Length.EqualTo(1));
            Assert.That(firstGap.RepairRequests[0].AttemptCount,
                Is.EqualTo(1));
            Assert.That(afterRetryLimit.FeedbackRequests, Is.Empty);
            Assert.That(session.ingress.feedbackScheduler?
                .pendingRequests, Has.Count.EqualTo(1));
        });
    }

    static RtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock)
    {
        return new RtpSession(
            configuration,
            clock,
            new FixedRandomSource(),
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration =
            new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.RtcpCompoundMode = RtcpCompoundMode.Full;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 16;
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond =
            64000.0;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry()
    {
        RtpPayloadTypeRegistry registry = new RtpPayloadTypeRegistry(
            RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
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

    static RtcpPacket CreateReceiverReportPacket(uint sourceIdentifier)
    {
        RtcpReceiverReport report = new RtcpReceiverReport();
        report.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = report;
        return packet;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        string transportIdentity)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = 90000;
        context.MediaClockTimestamp = arrivalTimeTicks;
        return context;
    }

    private class FixedClock : IRtpClock
    {
        public long TimeTicks;

        public long GetMonotonicTimeTicks()
        {
            return TimeTicks;
        }

        public long GetUtcTimeTicks()
        {
            return TimeTicks;
        }

        public ulong GetNtpTimestamp()
        {
            return (ulong)TimeTicks;
        }
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
}
