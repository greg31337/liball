using rtp;

namespace rtp.Tests.Session;

public class RtpOutboundStreamTests
{
    [Test]
    public void PacketizerFragmentsShareTimestampAndFeedStatisticsAndHistory()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(32);
        RtpPayloadTypeRegistry payloadRegistry = CreatePayloadRegistry(96, 90000);
        RtpHeaderExtensionRegistry extensionRegistry =
            CreateHeaderExtensionRegistry();
        FixedPacketizer packetizer = new FixedPacketizer();
        packetizer.Result = CreatePacketizationResult(
            new byte[][]
            {
                new byte[] { 10, 11, 12 },
                new byte[] { 13, 14 }
            },
            new bool[] { false, true },
            5);
        RtpLocalSourceState source = new RtpLocalSourceState(
            new FixedRandomSource(0x10203040U, ushort.MaxValue, 700U));
        RtpOutboundStream stream = new RtpOutboundStream(
            configuration,
            source,
            payloadRegistry,
            extensionRegistry,
            packetizer,
            new FixedClock());
        RtpPacket template = CreatePacket(96, new byte[] { 1, 2, 3, 4, 5 });
        template.ContributingSources = new uint[] { 22U };
        template.HasPadding = true;
        template.Padding = new byte[] { 0, 2 };
        template.HasExtension = true;
        template.HeaderExtension = CreateAutomaticExtension();
        RtpPacketContext context = CreateContext(50L, 90000, 1000L);

        RtpSessionUpdate update = stream.PacketizeOutboundRtpPayload(
            template,
            context);
        RtpTransmissionStatisticsSnapshot statistics = stream
            .transmissionStatistics.GetTransmissionStatisticsSnapshot();
        RtpPacket? firstHistory = stream.packetHistory.FindRtpHistoryPacket(
            0x10203040U,
            ushort.MaxValue);
        RtpPacket? secondHistory = stream.packetHistory.FindRtpHistoryPacket(
            0x10203040U,
            0);
        packetizer.Result.Payloads[0][0] = 99;
        template.Payload[0] = 98;
        update.Events[0].PacketizationResult!.Payloads[0][0] = 97;

        Assert.Multiple(() =>
        {
            Assert.That(packetizer.MaximumPayloadSize, Is.EqualTo(6));
            Assert.That(packetizer.MediaData,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(update.OutboundPackets, Has.Length.EqualTo(2));
            Assert.That(update.OutboundPackets.Select(value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { ushort.MaxValue, 0 }));
            Assert.That(update.OutboundPackets.Select(value => value.Timestamp),
                Is.EqualTo(new uint[] { 700U, 700U }));
            Assert.That(update.OutboundPackets.Select(value => value.Marker),
                Is.EqualTo(new bool[] { false, true }));
            Assert.That(update.OutboundPackets[0].Payload,
                Is.EqualTo(new byte[] { 10, 11, 12 }));
            Assert.That(update.OutboundPackets[1].Payload,
                Is.EqualTo(new byte[] { 13, 14 }));
            Assert.That(update.OutboundPackets[0].HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(update.OutboundPackets[1].HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(statistics.SentPacketCount, Is.EqualTo(2L));
            Assert.That(statistics.SentOctetCount, Is.EqualTo(5L));
            Assert.That(statistics.LastRtpTimestamp, Is.EqualTo(700U));
            Assert.That(firstHistory?.Payload,
                Is.EqualTo(new byte[] { 10, 11, 12 }));
            Assert.That(secondHistory?.Payload,
                Is.EqualTo(new byte[] { 13, 14 }));
            Assert.That(update.Events, Has.Length.EqualTo(1));
            Assert.That(update.Events[0].Type,
                Is.EqualTo(RtpSessionEventType.Source));
            Assert.That(update.Events[0].PacketizationResult?.ConsumedCount,
                Is.EqualTo(5));
            Assert.That(update.Events[0].Source?.TransmissionStatistics?
                .SentPacketCount, Is.EqualTo(2L));
        });
    }

    [Test]
    public void DirectPreparationPreservesSeparatePortMarkerAndTracksThePacket()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(64);
        RtpLocalSourceState source = new RtpLocalSourceState(
            new FixedRandomSource(7U, 20, 500U));
        RtpOutboundStream stream = new RtpOutboundStream(
            configuration,
            source,
            CreatePayloadRegistry(72, 90000),
            new RtpHeaderExtensionRegistry(),
            null,
            new FixedClock());
        RtpPacket packet = CreatePacket(72, new byte[] { 8, 9 });
        packet.Marker = true;

        RtpSessionUpdate update = stream.PrepareOutboundRtpPacket(
            packet,
            CreateContext(9L, 90000, 100L));
        RtpTransmissionStatisticsSnapshot statistics = source
            .CreateLocalSourceSnapshot().TransmissionStatistics!;

        Assert.Multiple(() =>
        {
            Assert.That(update.OutboundPackets, Has.Length.EqualTo(1));
            Assert.That(update.OutboundPackets[0].Marker, Is.True);
            Assert.That(update.OutboundPackets[0].SequenceNumber,
                Is.EqualTo(20));
            Assert.That(update.Events[0].Packet?.Payload,
                Is.EqualTo(new byte[] { 8, 9 }));
            Assert.That(statistics.SentPacketCount, Is.EqualTo(1L));
            Assert.That(statistics.SentOctetCount, Is.EqualTo(2L));
            Assert.That(stream.packetHistory.FindRtpHistoryPacket(7U, 20),
                Is.Not.Null);
        });
    }

    [Test]
    public void PacketizerFailureIsRecoverableAndDoesNotAdvanceState()
    {
        FixedPacketizer packetizer = new FixedPacketizer();
        packetizer.Result = new RtpPacketizationResult();
        packetizer.Result.ErrorMessage = "needs more media";
        RtpLocalSourceState source = new RtpLocalSourceState(
            new FixedRandomSource(50U, 60, 70U));
        RtpOutboundStream stream = new RtpOutboundStream(
            CreateConfiguration(64),
            source,
            CreatePayloadRegistry(96, 48000),
            new RtpHeaderExtensionRegistry(),
            packetizer,
            new FixedClock());

        RtpSessionUpdate failed = stream.PacketizeOutboundRtpPayload(
            CreatePacket(96, new byte[] { 1, 2 }),
            CreateContext(1L, 48000, 10L));
        packetizer.Result = CreatePacketizationResult(
            new byte[][] { new byte[] { 1, 2 } },
            new bool[] { true },
            2);
        RtpSessionUpdate succeeded = stream.PacketizeOutboundRtpPayload(
            CreatePacket(96, new byte[] { 1, 2 }),
            CreateContext(2L, 48000, 10L));

        Assert.Multiple(() =>
        {
            Assert.That(failed.OutboundPackets, Is.Empty);
            Assert.That(failed.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(failed.Events[0].Message, Is.EqualTo("needs more media"));
            Assert.That(failed.Events[0].PacketizationResult?.Success,
                Is.False);
            Assert.That(succeeded.OutboundPackets[0].SequenceNumber,
                Is.EqualTo(60));
            Assert.That(stream.transmissionStatistics
                .GetTransmissionStatisticsSnapshot().SentPacketCount,
                Is.EqualTo(1L));
            Assert.That(stream.packetHistory.FindRtpHistoryPacket(50U, 60),
                Is.Not.Null);
        });
    }

    [Test]
    public void InvalidPacketizerOutputIsRejectedBeforeAnyPacketIsPrepared()
    {
        FixedPacketizer packetizer = new FixedPacketizer();
        packetizer.Result = CreatePacketizationResult(
            new byte[][]
            {
                new byte[21],
                new byte[] { 1 }
            },
            new bool[] { false, true },
            2);
        RtpLocalSourceState source = new RtpLocalSourceState(
            new FixedRandomSource(80U, 90, 100U));
        RtpOutboundStream stream = new RtpOutboundStream(
            CreateConfiguration(32),
            source,
            CreatePayloadRegistry(96, 90000),
            new RtpHeaderExtensionRegistry(),
            packetizer,
            new FixedClock());

        RtpSessionUpdate invalid = stream.PacketizeOutboundRtpPayload(
            CreatePacket(96, new byte[] { 1, 2 }),
            CreateContext(1L, 90000, 20L));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.OutboundPackets, Is.Empty);
            Assert.That(invalid.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(invalid.Events[0].Message,
                Does.Contain("exceeds"));
            Assert.That(source.nextSequenceNumber, Is.EqualTo(90));
            Assert.That(source.isSender, Is.False);
            Assert.That(stream.transmissionStatistics
                .GetTransmissionStatisticsSnapshot().SentPacketCount,
                Is.Zero);
            Assert.That(stream.packetHistory.FindRtpHistoryPacket(80U, 90),
                Is.Null);
        });
    }

    [Test]
    public void ReselectedSourceRejectsStaleOutboundStreamBeforeMutation()
    {
        FixedPacketizer packetizer = new FixedPacketizer();
        packetizer.Result = CreatePacketizationResult(
            new byte[][] { new byte[] { 1 } },
            new bool[] { true },
            1);
        RtpLocalSourceState source = new RtpLocalSourceState(
            new FixedRandomSource(10U, 20, 30U));
        RtpOutboundStream stream = new RtpOutboundStream(
            CreateConfiguration(64),
            source,
            CreatePayloadRegistry(96, 90000),
            new RtpHeaderExtensionRegistry(),
            packetizer,
            new FixedClock());
        source.ResetLocalSourceIdentity(11U, 21, 31U);

        Assert.Throws<RtpSessionException>(() =>
            stream.PacketizeOutboundRtpPayload(
                CreatePacket(96, new byte[] { 1 }),
                CreateContext(1L, 90000, 1L)));
        Assert.Multiple(() =>
        {
            Assert.That(source.nextSequenceNumber, Is.EqualTo(21));
            Assert.That(source.isSender, Is.False);
            Assert.That(stream.transmissionStatistics
                .GetTransmissionStatisticsSnapshot().SentPacketCount,
                Is.Zero);
            Assert.That(stream.packetHistory.FindRtpHistoryPacket(10U, 20),
                Is.Null);
        });
    }

    [Test]
    public void PacketizerResultValidationRejectsEveryMalformedShape()
    {
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            null!, 1, 10), Is.Not.Null);
        RtpPacketizationResult result = CreatePacketizationResult([], [], -1);
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.ConsumedCount = 2;
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.ConsumedCount = 0;
        result.Payloads = null!;
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.Payloads = [];
        result.Markers = null!;
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.Payloads = new byte[][] { [] };
        result.Markers = [];
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.Payloads = [];
        result.Markers = [];
        result.Success = true;
        result.ConsumedCount = 1;
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.ConsumedCount = 0;
        result.Payloads = new byte[][] { null! };
        result.Markers = new bool[] { false };
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.Payloads = new byte[][] { new byte[11] };
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Not.Null);
        result.Payloads = new byte[][] { new byte[10] };
        Assert.That(RtpOutboundStream.GetPacketizationResultError(
            result, 1, 10), Is.Null);

        Assert.That(RtpOutboundStream.CreateSafePacketizationResultCopy(
            null!), Is.Null);
        result.Payloads = null!;
        Assert.That(RtpOutboundStream.CreateSafePacketizationResultCopy(
            result), Is.Null);
        result.Payloads = [];
        result.Markers = null!;
        Assert.That(RtpOutboundStream.CreateSafePacketizationResultCopy(
            result), Is.Null);
        result.Payloads = new byte[][] { null! };
        result.Markers = new bool[] { false };
        Assert.That(RtpOutboundStream.CreateSafePacketizationResultCopy(
            result), Is.Null);
        result.Payloads = new byte[][] { new byte[] { 1 } };
        Assert.That(RtpOutboundStream.CreateSafePacketizationResultCopy(
            result), Is.Not.SameAs(result));
    }

    static RtpSessionConfiguration CreateConfiguration(int maximumPacketSize)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.StrictValidation = true;
        configuration.MaximumRtpPacketSize = maximumPacketSize;
        configuration.MaximumPacketHistoryCount = 8;
        configuration.MaximumPacketHistoryBytes = 1024;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry(
        byte payloadType,
        int clockRate)
    {
        RtpPayloadTypeRegistry registry = new RtpPayloadTypeRegistry(
            RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = payloadType;
        definition.ClockRate = clockRate;
        definition.MediaIdentity = "test";
        definition.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(definition);
        return registry;
    }

    static RtpHeaderExtensionRegistry CreateHeaderExtensionRegistry()
    {
        RtpHeaderExtensionRegistry registry = new RtpHeaderExtensionRegistry();
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = 2;
        mapping.Uri = RtpHeaderExtensionUris.MediaIdentifier;
        mapping.SupportsOneByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        registry.SetHeaderExtensionMapping(mapping);
        return registry;
    }

    static RtpHeaderExtension CreateAutomaticExtension()
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = 2;
        element.Data = new byte[] { 7 };
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = new RtpHeaderExtensionElement[] { element };
        return extension;
    }

    static RtpPacket CreatePacket(byte payloadType, byte[] payload)
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = payloadType;
        packet.Payload = payload;
        return packet;
    }

    static RtpPacketContext CreateContext(
        long sendTimeTicks,
        int clockRate,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = sendTimeTicks;
        context.TransportIdentity = "send-path";
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    static RtpPacketizationResult CreatePacketizationResult(
        byte[][] payloads,
        bool[] markers,
        int consumedCount)
    {
        RtpPacketizationResult result = new RtpPacketizationResult();
        result.Success = true;
        result.ConsumedCount = consumedCount;
        result.Payloads = payloads;
        result.Markers = markers;
        return result;
    }

    private class FixedPacketizer : IRtpPayloadPacketizer
    {
        public RtpPacketizationResult Result = new RtpPacketizationResult();
        public byte[] MediaData = [];
        public int MaximumPayloadSize;

        public RtpPacketizationResult PacketizeRtpPayload(
            ReadOnlySpan<byte> mediaData,
            int maximumPayloadSize)
        {
            MediaData = mediaData.ToArray();
            MaximumPayloadSize = maximumPayloadSize;
            return Result;
        }
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        uint sourceIdentifier;
        ushort sequenceNumber;
        uint timestamp;

        public FixedRandomSource(
            uint sourceIdentifier,
            ushort sequenceNumber,
            uint timestamp)
        {
            this.sourceIdentifier = sourceIdentifier;
            this.sequenceNumber = sequenceNumber;
            this.timestamp = timestamp;
        }

        public uint CreateSynchronizationSource()
        {
            return sourceIdentifier;
        }

        public ushort CreateSequenceNumber()
        {
            return sequenceNumber;
        }

        public uint CreateTimestamp()
        {
            return timestamp;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }

    private class FixedClock : IRtpClock
    {
        public long GetMonotonicTimeTicks()
        {
            return 0L;
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
}
