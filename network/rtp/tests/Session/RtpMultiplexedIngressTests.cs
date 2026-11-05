using rtp;

namespace rtp.Tests.Session;

public class RtpMultiplexedIngressTests
{
    [Test]
    public void MultiplexedRtpUsesTheSharedMediaIngressState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Multiplexed);
        RtpPayloadTypeRegistry registry = CreatePayloadRegistry(
            RtpSessionTransportMode.Multiplexed);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            registry);
        byte[] datagram = new RtpPacketCodec(configuration)
            .EncodeRtpPacketToArray(
                CreateRtpPacket(100U, 1, 9000U));

        RtpSessionUpdate update = ingress.ProcessMultiplexedDatagram(
            datagram,
            CreateContext(1L, "mux-path"));
        RtpSourceState? state =
            ingress.remoteSourceTable.FindSourceState(100U);
        RtpReceptionStatisticsSnapshot statistics = state!
            .receptionStatistics.GetReceptionStatisticsSnapshot(1L);

        Assert.Multiple(() =>
        {
            Assert.That(update.AcceptedPackets.Length, Is.EqualTo(1));
            Assert.That(update.ReleasedPackets.Length, Is.EqualTo(1));
            Assert.That(update.Events.Single().PacketKind,
                Is.EqualTo(RtpPacketKind.Rtp));
            Assert.That(update.Events[0].SequenceStatus,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(update.Events[0].Source?.MediaIdentity,
                Is.EqualTo("video"));
            Assert.That(statistics.ReceivedPacketCount, Is.EqualTo(1L));
        });
    }

    [Test]
    public void MultiplexedRtcpUsesTheSharedControlIngressState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Multiplexed);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry(RtpSessionTransportMode.Multiplexed));
        byte[] datagram = new RtcpPacketCodec(configuration)
            .EncodeRtcpCompoundPacketToArray(
                CreateReceiverReportCompound(200U));

        RtpSessionUpdate update = ingress.ProcessMultiplexedDatagram(
            datagram,
            CreateContext(2L, "mux-control"));
        RtpSourceSnapshot? source =
            ingress.remoteSourceTable.FindRemoteSourceSnapshot(200U);

        Assert.Multiple(() =>
        {
            Assert.That(update.Events.Length, Is.EqualTo(2));
            Assert.That(update.Events.Select(value => value.PacketKind),
                Is.EqualTo(new RtpPacketKind[]
                {
                    RtpPacketKind.Rtcp,
                    RtpPacketKind.Rtcp
                }));
            Assert.That(update.Events[0].ControlPacket?.Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(source?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(source?.TransportIdentity,
                Is.EqualTo("mux-control"));
        });
    }

    [Test]
    public void UnknownMuxDatagramsReturnRecoverableEventsWithoutMutation()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtpSessionTransportMode.Multiplexed);
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry(RtpSessionTransportMode.Multiplexed));
        RtpPacket forbiddenPacket = CreateRtpPacket(100U, 1, 1U);
        forbiddenPacket.PayloadType = 72;
        RtpSessionConfiguration separateConfiguration = CreateConfiguration(
            RtpSessionTransportMode.Separate);
        byte[] forbiddenDatagram = new RtpPacketCodec(separateConfiguration)
            .EncodeRtpPacketToArray(forbiddenPacket);

        RtpSessionUpdate forbidden = ingress.ProcessMultiplexedDatagram(
            forbiddenDatagram,
            CreateContext(1L, "mux"));
        RtpSessionUpdate truncated = ingress.ProcessMultiplexedDatagram(
            new byte[] { 0x80 },
            CreateContext(2L, "mux"));

        Assert.Multiple(() =>
        {
            Assert.That(forbidden.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(forbidden.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Unknown));
            Assert.That(forbidden.Events[0].Message,
                Does.Contain("forbidden"));
            Assert.That(truncated.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(truncated.Events[0].PacketKind,
                Is.EqualTo(RtpPacketKind.Unknown));
            Assert.That(ingress.remoteSourceTable
                .GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    [Test]
    public void MuxClassificationRunsOnceAndSeparateModeRejectsRouting()
    {
        RtpSessionConfiguration muxConfiguration = CreateConfiguration(
            RtpSessionTransportMode.Multiplexed);
        RtpSeparatePortIngress muxIngress = new RtpSeparatePortIngress(
            muxConfiguration,
            new FixedRandomSource(),
            CreatePayloadRegistry(RtpSessionTransportMode.Multiplexed));
        CountingDemultiplexer counting = new CountingDemultiplexer();
        counting.kind = RtpPacketKind.Rtp;
        muxIngress.demultiplexer = counting;
        byte[] datagram = new RtpPacketCodec(muxConfiguration)
            .EncodeRtpPacketToArray(
                CreateRtpPacket(100U, 1, 1U));

        RtpSessionUpdate routed = muxIngress.ProcessMultiplexedDatagram(
            datagram,
            CreateContext(1L, "mux"));

        RtpSessionConfiguration separateConfiguration = CreateConfiguration(
            RtpSessionTransportMode.Separate);
        RtpSeparatePortIngress separateIngress = new RtpSeparatePortIngress(
            separateConfiguration,
            new FixedRandomSource(),
            CreatePayloadRegistry(RtpSessionTransportMode.Separate));
        CountingDemultiplexer disabledCounting =
            new CountingDemultiplexer();
        disabledCounting.kind = RtpPacketKind.Rtp;
        separateIngress.demultiplexer = disabledCounting;
        RtpSessionUpdate disabled =
            separateIngress.ProcessMultiplexedDatagram(
                datagram,
                CreateContext(1L, "mux"));

        Assert.Multiple(() =>
        {
            Assert.That(routed.AcceptedPackets.Length, Is.EqualTo(1));
            Assert.That(counting.callCount, Is.EqualTo(1));
            Assert.That(disabled.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(disabled.Events[0].Message,
                Does.Contain("not enabled"));
            Assert.That(disabledCounting.callCount, Is.EqualTo(1));
            Assert.That(separateIngress.remoteSourceTable
                .GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    static RtpSessionConfiguration CreateConfiguration(
        RtpSessionTransportMode transportMode)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = transportMode;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 16;
        configuration.CanonicalName = "local@example";
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry(
        RtpSessionTransportMode transportMode)
    {
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(transportMode);
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
        uint timestamp)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Payload = new byte[] { 1 };
        return packet;
    }

    static RtcpCompoundPacket CreateReceiverReportCompound(
        uint sourceIdentifier)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket receiverPacket = new RtcpPacket();
        receiverPacket.Type = RtcpPacketType.ReceiverReport;
        receiverPacket.ReceiverReport = receiverReport;

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
            receiverPacket,
            descriptionPacket
        };
        return compound;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        string transportIdentity)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.MediaClockTimestamp = arrivalTimeTicks * 90L;
        return context;
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 500U;
        }

        public ushort CreateSequenceNumber()
        {
            return 1;
        }

        public uint CreateTimestamp()
        {
            return 1U;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }

    private class CountingDemultiplexer : IRtpRtcpDemultiplexer
    {
        internal int callCount;
        internal RtpPacketKind kind;

        public RtpDatagramClassification ClassifyDatagram(
            ReadOnlySpan<byte> datagram)
        {
            callCount = callCount + 1;
            RtpDatagramClassification classification =
                new RtpDatagramClassification();
            classification.Kind = kind;
            return classification;
        }
    }
}
