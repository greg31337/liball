using System.Text;
using rtp;

namespace rtp.Tests.Control;

public class RtcpReportBuilderCoverageTraceTests
{
    const uint FirstLocalSource = 0x10101010U;
    const uint SecondLocalSource = 0x20202020U;
    const uint ThirdLocalSource = 0x30303030U;
    const uint RemoteSourceBase = 1000U;
    const ulong NtpTimestamp = 0x0102030405060708UL;

    [Test]
    public void MixedLocalStreamsBuildIndependentSenderAndReceiverReports()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(
            new RtpSourceSnapshot[]
            {
                CreateLocalSource(
                    FirstLocalSource,
                    true,
                    4294967297L,
                    4294967298L,
                    111U),
                CreateLocalSource(
                    SecondLocalSource,
                    false,
                    0L,
                    0L,
                    222U),
                CreateLocalSource(
                    ThirdLocalSource,
                    true,
                    7L,
                    11L,
                    333U),
                CreateRemoteSource(RemoteSourceBase)
            });
        snapshot.LocalSourceIdentifiers = new uint[]
        {
            FirstLocalSource,
            SecondLocalSource,
            ThirdLocalSource
        };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket compound = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpSourceDescription description =
            compound.Packets[3].SourceDescription!;

        Assert.Multiple(() =>
        {
            Assert.That(compound.Packets, Has.Length.EqualTo(4));
            Assert.That(compound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(compound.Packets[0].SenderReport?
                .SenderSynchronizationSource, Is.EqualTo(FirstLocalSource));
            Assert.That(compound.Packets[0].SenderReport?
                .SenderInformation.NtpTimestamp, Is.EqualTo(NtpTimestamp));
            Assert.That(compound.Packets[0].SenderReport?
                .SenderInformation.RtpTimestamp, Is.EqualTo(111U));
            Assert.That(compound.Packets[0].SenderReport?
                .SenderInformation.PacketCount, Is.EqualTo(1U));
            Assert.That(compound.Packets[0].SenderReport?
                .SenderInformation.OctetCount, Is.EqualTo(2U));
            Assert.That(compound.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(compound.Packets[1].ReceiverReport?
                .SenderSynchronizationSource, Is.EqualTo(SecondLocalSource));
            Assert.That(compound.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(compound.Packets[2].SenderReport?
                .SenderSynchronizationSource, Is.EqualTo(ThirdLocalSource));
            Assert.That(compound.Packets[2].SenderReport?
                .SenderInformation.PacketCount, Is.EqualTo(7U));
            Assert.That(compound.Packets[2].SenderReport?
                .SenderInformation.OctetCount, Is.EqualTo(11U));
            Assert.That(compound.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase));
            Assert.That(compound.Packets[1].ReceiverReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase));
            Assert.That(compound.Packets[2].SenderReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase));
            Assert.That(compound.Packets[3].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(description.Chunks, Has.Length.EqualTo(3));
            Assert.That(description.Chunks.Select(
                value => value.SourceIdentifier),
                Is.EqualTo(snapshot.LocalSourceIdentifiers));
            Assert.That(description.Chunks.All(
                value => value.Items[0].Type ==
                    RtcpSdesItemType.CanonicalName), Is.True);
            Assert.That(description.Chunks.All(
                value => Encoding.UTF8.GetString(value.Items[0].Value) ==
                    "local@example"), Is.True);
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(snapshot.Configuration);
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(compound));
    }

    [Test]
    public void SixtyFiveRemoteSourcesSplitAndRotateForEveryLocalStream()
    {
        RtpSessionSnapshot snapshot = CreateManySourceSnapshot(65);
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket first = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpCompoundPacket second = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpCompoundPacket third = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packets, Has.Length.EqualTo(7));
            Assert.That(first.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(first.Packets[0].SenderReport?.ReceptionReports,
                Has.Length.EqualTo(31));
            Assert.That(first.Packets[1].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(31));
            Assert.That(first.Packets[2].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(3));
            Assert.That(first.Packets[3].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(first.Packets[3].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(31));
            Assert.That(first.Packets[4].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(31));
            Assert.That(first.Packets[5].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(3));
            Assert.That(first.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase));
            Assert.That(first.Packets[3].ReceiverReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase));
            Assert.That(second.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase + 31U));
            Assert.That(second.Packets[2].ReceiverReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase + 28U));
            Assert.That(second.Packets[3].ReceiverReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase + 31U));
            Assert.That(third.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier, Is.EqualTo(RemoteSourceBase + 62U));
            Assert.That(third.Packets[6].SourceDescription?.Chunks,
                Has.Length.EqualTo(2));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(snapshot.Configuration);
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(first));
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(second));
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(third));
    }

    [Test]
    public void OptionalDescriptionsRotateAtExactAccumulatedByteCredit()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(
            new RtpSourceSnapshot[]
            {
                CreateLocalSource(
                    FirstLocalSource,
                    false,
                    0L,
                    0L,
                    0U)
            });
        snapshot.LocalSourceIdentifiers = new uint[] { FirstLocalSource };
        snapshot.Configuration.CanonicalName = "c";
        snapshot.AverageRtcpPacketSize = 20;
        snapshot.Configuration.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[]
            {
                CreateDescriptionItem(RtcpSdesItemType.Name, "n"),
                CreateDescriptionItem(RtcpSdesItemType.Email, "e")
            };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket first = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpCompoundPacket second = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpCompoundPacket third = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpSourceDescriptionItem[] firstItems = first.Packets[1]
            .SourceDescription!.Chunks[0].Items;
        RtcpSourceDescriptionItem[] secondItems = second.Packets[1]
            .SourceDescription!.Chunks[0].Items;
        RtcpSourceDescriptionItem[] thirdItems = third.Packets[1]
            .SourceDescription!.Chunks[0].Items;

        Assert.Multiple(() =>
        {
            Assert.That(firstItems, Has.Length.EqualTo(2));
            Assert.That(secondItems, Has.Length.EqualTo(2));
            Assert.That(thirdItems, Has.Length.EqualTo(2));
            Assert.That(firstItems[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(secondItems[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(thirdItems[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(firstItems[1].Type, Is.EqualTo(RtcpSdesItemType.Name));
            Assert.That(secondItems[1].Type, Is.EqualTo(RtcpSdesItemType.Email));
            Assert.That(thirdItems[1].Type, Is.EqualTo(RtcpSdesItemType.Name));
        });
    }

    [Test]
    public void ScheduledReportsRollReceptionIntervalsBeforeConstruction()
    {
        RtpSessionConfiguration configuration = CreateSessionConfiguration();
        ManualClock clock = new ManualClock();
        clock.MonotonicTimeTicks = 0L;
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            CreatePayloadRegistry());
        RtpSessionPoller poller = new RtpSessionPoller(
            configuration,
            clock,
            ingress);
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RecordRemotePacket(ingress, 10, 1000U, 0L, 1000L);
        RecordRemotePacket(ingress, 14, 1160U, 1L, 1160L);

        ingress.remoteSourceTable.AdvanceRemoteReceptionReportIntervals(1L);
        RtpSessionSnapshot firstSnapshot = poller.CreateSessionSnapshot(1L);
        RtcpCompoundPacket firstCompound = builder.BuildRegularRtcpReport(
            firstSnapshot,
            NtpTimestamp);
        RtcpReceptionReport firstReport =
            GetOnlyReceptionReport(firstCompound);

        RecordRemotePacket(
            ingress,
            11,
            1320U,
            2L,
            1320L);
        RecordRemotePacket(
            ingress,
            13,
            1480U,
            3L,
            1480L);
        RecordRemotePacket(
            ingress,
            12,
            1640U,
            4L,
            1640L);
        RecordRemotePacket(
            ingress,
            18,
            1800U,
            5L,
            1800L);
        ingress.remoteSourceTable.AdvanceRemoteReceptionReportIntervals(5L);
        RtpSessionSnapshot secondSnapshot = poller.CreateSessionSnapshot(5L);
        RtcpCompoundPacket secondCompound = builder.BuildRegularRtcpReport(
            secondSnapshot,
            NtpTimestamp);
        RtcpReceptionReport secondReport =
            GetOnlyReceptionReport(secondCompound);

        Assert.Multiple(() =>
        {
            Assert.That(firstReport.SourceIdentifier,
                Is.EqualTo(RemoteSourceBase));
            Assert.That(firstReport.FractionLost, Is.EqualTo(153));
            Assert.That(firstReport.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(firstReport.ExtendedHighestSequenceNumber,
                Is.EqualTo(14U));
            Assert.That(secondReport.FractionLost, Is.Zero);
            Assert.That(secondReport.CumulativePacketsLost, Is.EqualTo(3));
            Assert.That(secondReport.ExtendedHighestSequenceNumber,
                Is.EqualTo(18U));
        });
    }

    static RtpSessionSnapshot CreateManySourceSnapshot(int remoteCount)
    {
        RtpSourceSnapshot firstLocal = CreateLocalSource(
            FirstLocalSource,
            true,
            5L,
            10L,
            500U);
        RtpSourceSnapshot secondLocal = CreateLocalSource(
            SecondLocalSource,
            false,
            0L,
            0L,
            0U);
        RtpSourceSnapshot[] sources =
            new RtpSourceSnapshot[remoteCount + 2];
        sources[0] = firstLocal;
        sources[1] = secondLocal;
        int remoteIndex = 0;
        while (remoteIndex < remoteCount)
        {
            sources[remoteIndex + 2] = CreateRemoteSource(
                RemoteSourceBase + (uint)remoteIndex);
            remoteIndex = remoteIndex + 1;
        }

        RtpSessionSnapshot snapshot = CreateSnapshot(sources);
        snapshot.LocalSourceIdentifiers = new uint[]
        {
            FirstLocalSource,
            SecondLocalSource
        };
        return snapshot;
    }

    static RtpSessionSnapshot CreateSnapshot(RtpSourceSnapshot[] sources)
    {
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.Configuration.CanonicalName = "local@example";
        snapshot.Sources = sources;
        return snapshot;
    }

    static RtpSourceSnapshot CreateLocalSource(
        uint sourceIdentifier,
        bool activeSender,
        long sentPacketCount,
        long sentOctetCount,
        uint lastRtpTimestamp)
    {
        RtpTransmissionStatisticsSnapshot transmission =
            new RtpTransmissionStatisticsSnapshot();
        transmission.SourceIdentifier = sourceIdentifier;
        transmission.IsActiveSender = activeSender;
        transmission.SenderReportEligible = activeSender;
        transmission.SentPacketCount = sentPacketCount;
        transmission.SentOctetCount = sentOctetCount;
        transmission.LastRtpTimestamp = lastRtpTimestamp;
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        source.SourceIdentifier = sourceIdentifier;
        source.IsLocal = true;
        source.TransmissionStatistics = transmission;
        return source;
    }

    static RtpSourceSnapshot CreateRemoteSource(uint sourceIdentifier)
    {
        RtpReceptionStatisticsSnapshot reception =
            new RtpReceptionStatisticsSnapshot();
        reception.SourceIdentifier = sourceIdentifier;
        reception.FractionLost = 64;
        reception.CumulativePacketsLost = 2;
        reception.HighestExtendedSequenceNumber = sourceIdentifier;
        reception.InterarrivalJitter = 10U;
        reception.LastSenderReport = 0x11223344U;
        reception.DelaySinceLastSenderReport = 0x00008000U;
        RtpSourceSnapshot source = new RtpSourceSnapshot();
        source.SourceIdentifier = sourceIdentifier;
        source.ReceptionStatistics = reception;
        return source;
    }

    static RtcpSourceDescriptionItem CreateDescriptionItem(
        RtcpSdesItemType type,
        string value)
    {
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        item.Type = type;
        item.Value = Encoding.UTF8.GetBytes(value);
        return item;
    }

    static RtcpReceptionReport GetOnlyReceptionReport(
        RtcpCompoundPacket compound)
    {
        RtcpPacket reportPacket = compound.Packets[0];
        return reportPacket.ReceiverReport!.ReceptionReports.Single();
    }

    static void RecordRemotePacket(
        RtpSeparatePortIngress ingress,
        ushort sequenceNumber,
        uint timestamp,
        long arrivalTimeTicks,
        long mediaClockTimestamp)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = RemoteSourceBase;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Payload = new byte[] { 1 };
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.MediaClockTimestamp = mediaClockTimestamp;
        context.ClockRate = 90000;
        context.TransportIdentity = "remote-path";
        byte[] datagram = ingress.rtpPacketCodec.EncodeRtpPacketToArray(packet);
        ingress.ProcessSeparateRtpDatagram(datagram, context);
    }

    static RtpSessionConfiguration CreateSessionConfiguration()
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

    private class ManualClock : IRtpClock
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
            return NtpTimestamp;
        }
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return FirstLocalSource;
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
}
