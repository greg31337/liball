using rtp;

namespace rtp.Tests.Session;

public class RtpSeparatePortIngressTests
{
    [Test]
    public void RtpIngressUpdatesSourceStatisticsAndReleasesAcceptedPackets()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(1);
        RtpPayloadTypeRegistry registry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            registry);
        RtpPacket packet = CreateRtpPacket(100U, 10, 9000U, 96);
        byte[] datagram = new RtpPacketCodec(configuration)
            .EncodeRtpPacketToArray(packet);
        RtpPacketContext context = CreateContext(1L, "remote-a", 900L);
        context.ClockRate = 1;

        RtpSessionUpdate accepted = ingress.ProcessSeparateRtpDatagram(
            datagram,
            context);
        RtpSessionUpdate duplicate = ingress.ProcessSeparateRtpDatagram(
            datagram,
            CreateContext(2L, "remote-a", 901L));
        RtpSourceSnapshot? snapshot =
            ingress.remoteSourceTable.FindRemoteSourceSnapshot(100U);
        RtpSourceState? state =
            ingress.remoteSourceTable.FindSourceState(100U);
        RtpReceptionStatisticsSnapshot statistics =
            state!.receptionStatistics.GetReceptionStatisticsSnapshot(2L);

        Assert.Multiple(() =>
        {
            Assert.That(context.ClockRate, Is.EqualTo(1));
            Assert.That(accepted.AcceptedPackets.Length, Is.EqualTo(1));
            Assert.That(accepted.ReleasedPackets.Length, Is.EqualTo(1));
            Assert.That(accepted.ReleasedPackets[0].SequenceNumber,
                Is.EqualTo(10));
            Assert.That(accepted.Events.Length, Is.EqualTo(1));
            Assert.That(accepted.Events[0].Type,
                Is.EqualTo(RtpSessionEventType.Source));
            Assert.That(accepted.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Rtp));
            Assert.That(accepted.Events[0].SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(accepted.Events[0].JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(accepted.Events[0].Source?.ReceptionStatistics?
                .ReceivedPacketCount, Is.EqualTo(1L));
            Assert.That(duplicate.AcceptedPackets, Is.Empty);
            Assert.That(duplicate.ReleasedPackets, Is.Empty);
            Assert.That(duplicate.Events[0].SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.Duplicate));
            Assert.That(snapshot?.ClockRate, Is.EqualTo(90000));
            Assert.That(snapshot?.MediaIdentity, Is.EqualTo("video"));
            Assert.That(statistics.ReceivedPacketCount, Is.EqualTo(2L));
            Assert.That(statistics.DuplicatePacketCount, Is.EqualTo(1L));
        });
    }

    [Test]
    public void ProbationAndMalformedOrUnmappedRtpDoNotEnterTheJitterBuffer()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(2);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        byte[] firstDatagram = codec.EncodeRtpPacketToArray(
            CreateRtpPacket(100U, 10, 1000U, 96));
        byte[] secondDatagram = codec.EncodeRtpPacketToArray(
            CreateRtpPacket(100U, 11, 2000U, 96));
        byte[] unmappedDatagram = codec.EncodeRtpPacketToArray(
            CreateRtpPacket(200U, 1, 1U, 97));

        RtpSessionUpdate probation = ingress.ProcessSeparateRtpDatagram(
            firstDatagram,
            CreateContext(1L, "remote", 10L));
        RtpSessionUpdate accepted = ingress.ProcessSeparateRtpDatagram(
            secondDatagram,
            CreateContext(2L, "remote", 20L));
        RtpSessionUpdate malformed = ingress.ProcessSeparateRtpDatagram(
            new byte[] { 0x80, 0x60 },
            CreateContext(3L, "remote", 30L));
        RtpSessionUpdate unmapped = ingress.ProcessSeparateRtpDatagram(
            unmappedDatagram,
            CreateContext(3L, "other", 30L));
        RtpSourceState? state =
            ingress.remoteSourceTable.FindSourceState(100U);
        RtpReceptionStatisticsSnapshot statistics =
            state!.receptionStatistics.GetReceptionStatisticsSnapshot(3L);

        Assert.Multiple(() =>
        {
            Assert.That(probation.AcceptedPackets, Is.Empty);
            Assert.That(probation.ReleasedPackets, Is.Empty);
            Assert.That(probation.Events[0].SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(accepted.AcceptedPackets.Length, Is.EqualTo(1));
            Assert.That(accepted.ReleasedPackets.Length, Is.EqualTo(1));
            Assert.That(statistics.ReceivedPacketCount, Is.EqualTo(1L));
            Assert.That(statistics.DiscardedPacketCount, Is.EqualTo(1L));
            Assert.That(malformed.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(malformed.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Rtp));
            Assert.That(unmapped.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(unmapped.Events[0].Message,
                Does.Contain("not registered"));
            Assert.That(ingress.remoteSourceTable
                .FindRemoteSourceSnapshot(200U), Is.Null);
        });
    }

    [Test]
    public void RtpIngressSurfacesGapLossAndOrderedJitterRelease()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(1);
        configuration.JitterBuffer.MaximumLatencyTicks = 5L;
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);

        ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreateRtpPacket(100U, 10, 1000U, 96)),
            CreateContext(1L, "remote", 10L));
        RtpSessionUpdate buffered = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreateRtpPacket(100U, 12, 3000U, 96)),
            CreateContext(2L, "remote", 20L));
        RtpSessionUpdate expired = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(
                CreateRtpPacket(100U, 13, 4000U, 96)),
            CreateContext(7L, "remote", 70L));

        Assert.Multiple(() =>
        {
            Assert.That(buffered.ReleasedPackets, Is.Empty);
            Assert.That(buffered.Events[0].JitterBufferStatus,
                Is.EqualTo(RtpJitterBufferStatus.Buffered));
            Assert.That(expired.ReleasedPackets
                .Select(value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 12, 13 }));
            Assert.That(expired.Events.Select(value => value.Type),
                Is.EqualTo(new RtpSessionEventType[]
                {
                    RtpSessionEventType.Source,
                    RtpSessionEventType.Loss
                }));
            Assert.That(expired.Events[1].JitterBufferUpdate?
                .LostSequenceNumbers, Is.EqualTo(new ushort[] { 11 }));
        });
    }

    [Test]
    public void RtcpIngressUpdatesSenderActivityAndSenderReportTiming()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(1);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtcpCompoundPacket compound = CreateSenderReportCompound(300U);
        byte[] datagram = new RtcpPacketCodec(configuration)
            .EncodeRtcpCompoundPacketToArray(compound);

        RtpSessionUpdate update = ingress.ProcessSeparateRtcpDatagram(
            datagram,
            CreateContext(5L, "control", 0L));
        RtpSourceState? source =
            ingress.remoteSourceTable.FindSourceState(300U);
        RtpReceptionStatisticsSnapshot statistics = source!
            .receptionStatistics.GetReceptionStatisticsSnapshot(5L);

        Assert.Multiple(() =>
        {
            Assert.That(update.Events.Length, Is.EqualTo(2));
            Assert.That(update.Events.Select(value => value.Type),
                Is.EqualTo(new RtpSessionEventType[]
                {
                    RtpSessionEventType.Report,
                    RtpSessionEventType.Report
                }));
            Assert.That(update.Events.Select(value => value.PacketKind),
                Is.EqualTo(new RtpPacketKind[]
                {
                    RtpPacketKind.Rtcp,
                    RtpPacketKind.Rtcp
                }));
            Assert.That(update.Events[0].ControlPacket?.Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(update.Events[0].Source?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(update.Events[0].Source?.LastRtcpActivityTicks,
                Is.EqualTo(5L));
            Assert.That(update.Events[1].ControlPacket?.Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(statistics.LastSenderReport,
                Is.EqualTo(0x56780000U));
        });
    }

    [Test]
    public void InvalidRtcpAndLoopedRtpProduceEventsWithoutRemoteMutation()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(1);
        RtpPayloadTypeRegistry registry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            registry);
        RtpLocalSourceState localSource =
            ingress.localSourceTable.CreateLocalSourceState();
        RtpOutboundPacketBuilder builder = new RtpOutboundPacketBuilder(
            registry,
            new RtpHeaderExtensionRegistry());
        RtpPacket outbound = builder.BuildOutboundPacket(
            localSource,
            CreateRtpPacket(0U, 0, 0U, 96),
            CreateContext(1L, "loop-path", 10L));
        byte[] loopedDatagram = new RtpPacketCodec(configuration)
            .EncodeRtpPacketToArray(outbound);

        RtpSessionUpdate invalidControl =
            ingress.ProcessSeparateRtcpDatagram(
                new byte[] { 0x80, 200 },
                CreateContext(2L, "control", 0L));
        RtpSessionUpdate loop = ingress.ProcessSeparateRtpDatagram(
            loopedDatagram,
            CreateContext(2L, "loop-path", 20L));
        RtpSessionUpdate invalidContext =
            ingress.ProcessSeparateRtpDatagram(
                loopedDatagram,
                CreateContext(-1L, "loop-path", 20L));

        Assert.Multiple(() =>
        {
            Assert.That(invalidControl.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(invalidControl.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(loop.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.Collision));
            Assert.That(loop.Events[0].Message, Does.Contain("loop"));
            Assert.That(loop.AcceptedPackets, Is.Empty);
            Assert.That(invalidContext.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(ingress.remoteSourceTable
                .GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    static RtpSessionConfiguration CreateConfiguration(int minimumSequential)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = minimumSequential;
        configuration.MaximumSourceCount = 16;
        configuration.CanonicalName = "local@example";
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry()
    {
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
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
        uint timestamp,
        byte payloadType)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = payloadType;
        packet.Payload = new byte[] { 1, 2, 3 };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        string transportIdentity,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = 90000;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    static RtcpCompoundPacket CreateSenderReportCompound(
        uint sourceIdentifier)
    {
        RtcpSenderReport senderReport = new RtcpSenderReport();
        senderReport.SenderSynchronizationSource = sourceIdentifier;
        senderReport.SenderInformation.NtpTimestamp =
            0x1234567800000000UL;
        RtcpPacket senderPacket = new RtcpPacket();
        senderPacket.Type = RtcpPacketType.SenderReport;
        senderPacket.SenderReport = senderReport;

        RtcpSourceDescriptionItem canonicalName =
            new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = new byte[] { (byte)'x' };
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
            senderPacket,
            descriptionPacket
        };
        return compound;
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
