using rtp;

namespace rtp.Tests.Session;

public class RtpMultiSourceStateTests
{
    [Test]
    public void LocalSourcesKeepIndependentSequenceTimestampAndMediaState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(2);
        SequenceRandomSource randomSource = new SequenceRandomSource(
            new uint[] { 200U, 100U },
            new ushort[] { 20, 10 },
            new uint[] { 2000U, 1000U });
        RtpLocalSourceTable table = new RtpLocalSourceTable(
            configuration,
            randomSource);
        RtpLocalSourceState videoSource = table.CreateLocalSourceState();
        RtpLocalSourceState audioSource = table.CreateLocalSourceState();
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(96, 90000, "video", 1));
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(0, 8000, "audio", 1));
        RtpOutboundPacketBuilder builder = new RtpOutboundPacketBuilder(
            registry,
            new RtpHeaderExtensionRegistry());

        RtpPacket firstVideo = builder.BuildOutboundPacket(
            videoSource,
            CreatePacket(96, 1U),
            CreateContext(10L, 90000, 500L));
        RtpPacket audio = builder.BuildOutboundPacket(
            audioSource,
            CreatePacket(0, 2U),
            CreateContext(11L, 8000, 50L));
        RtpPacket secondVideo = builder.BuildOutboundPacket(
            videoSource,
            CreatePacket(96, 3U),
            CreateContext(12L, 90000, 501L));
        RtpSourceSnapshot[] snapshots = table.GetLocalSourceSnapshots();
        snapshots[0].MediaIdentity = "changed";
        RtpSourceSnapshot? freshAudio =
            table.FindLocalSourceSnapshot(100U);

        Assert.Multiple(() =>
        {
            Assert.That(firstVideo.SynchronizationSource, Is.EqualTo(200U));
            Assert.That(firstVideo.SequenceNumber, Is.EqualTo(20));
            Assert.That(firstVideo.Timestamp, Is.EqualTo(2000U));
            Assert.That(secondVideo.SequenceNumber, Is.EqualTo(21));
            Assert.That(secondVideo.Timestamp, Is.EqualTo(2001U));
            Assert.That(audio.SynchronizationSource, Is.EqualTo(100U));
            Assert.That(audio.SequenceNumber, Is.EqualTo(10));
            Assert.That(audio.Timestamp, Is.EqualTo(1000U));
            Assert.That(snapshots.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U }));
            Assert.That(snapshots.Select(value => value.IsLocal),
                Is.EqualTo(new bool[] { true, true }));
            Assert.That(snapshots.Select(value => value.IsSender),
                Is.EqualTo(new bool[] { true, true }));
            Assert.That(freshAudio?.CanonicalName,
                Is.EqualTo("endpoint@example"));
            Assert.That(freshAudio?.PayloadType, Is.EqualTo(0));
            Assert.That(freshAudio?.ClockRate, Is.EqualTo(8000));
            Assert.That(freshAudio?.MediaIdentity, Is.EqualTo("audio"));
            Assert.That(freshAudio?.LastRtpActivityTicks, Is.EqualTo(11L));
            Assert.That(table.GetLocalSourceIdentifiers(),
                Is.EqualTo(new uint[] { 100U, 200U }));
            Assert.Throws<RtpBufferCapacityException>(() =>
                table.CreateLocalSourceState());
        });

        Assert.That(table.RemoveLocalSourceState(100U), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(table.FindLocalSourceState(100U), Is.Null);
            Assert.That(table.GetLocalSourceIdentifiers(),
                Is.EqualTo(new uint[] { 200U }));
            Assert.That(table.RemoveLocalSourceState(100U), Is.False);
        });
    }

    [Test]
    public void DuplicateLocalIdentifiersAreRejectedWithoutChangingTheTable()
    {
        RtpLocalSourceTable table = new RtpLocalSourceTable(
            CreateConfiguration(3),
            new SequenceRandomSource(
                new uint[] { 100U, 100U },
                new ushort[] { 1, 2 },
                new uint[] { 10U, 20U }));

        RtpLocalSourceState first = table.CreateLocalSourceState();

        Assert.Multiple(() =>
        {
            Assert.That(first.sourceIdentifier, Is.EqualTo(100U));
            Assert.Throws<RtpSessionException>(() =>
                table.CreateLocalSourceState());
            Assert.That(table.GetLocalSourceIdentifiers(),
                Is.EqualTo(new uint[] { 100U }));
        });

        table.ClearLocalSourceStates();
        Assert.That(table.GetLocalSourceSnapshots(), Is.Empty);
    }

    [Test]
    public void RemoteStreamsResolveCurrentMappingsAndIsolateClockChanges()
    {
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(96, 90000, "H264", 1));
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(0, 8000, "PCMU", 1));
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(4),
            new SequenceRandomSource(
                new uint[] { 1U },
                new ushort[] { 1 },
                new uint[] { 1U }));
        table.ConfigureRemotePayloadTypeRegistry(registry);
        RtpPacketContext videoContext = CreateContext(1L, 12345, 900L);

        table.RecordMappedRemoteRtpPacket(
            CreateRemotePacket(200U, 1, 9000U, 96),
            videoContext);
        table.RecordMappedRemoteRtpPacket(
            CreateRemotePacket(100U, 1, 800U, 0),
            CreateContext(1L, 12345, 80L));
        RtpSourceSnapshot[] initial = table.GetRemoteSourceSnapshots();

        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(96, 48000, "VP8", 2));
        table.RecordMappedRemoteRtpPacket(
            CreateRemotePacket(200U, 2, 100U, 96),
            CreateContext(2L, 1, 100L));
        RtpSourceSnapshot? remapped =
            table.FindRemoteSourceSnapshot(200U);
        RtpSourceSnapshot? unaffected =
            table.FindRemoteSourceSnapshot(100U);

        table.RecordMappedRemoteRtpPacket(
            CreateRemotePacket(200U, 3, 200U, 0),
            CreateContext(3L, 1, 200L));
        RtpSourceSnapshot? changedMedia =
            table.FindRemoteSourceSnapshot(200U);

        Assert.Multiple(() =>
        {
            Assert.That(videoContext.ClockRate, Is.EqualTo(12345));
            Assert.That(initial.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U }));
            Assert.That(initial[0].MediaIdentity, Is.EqualTo("PCMU"));
            Assert.That(initial[0].ClockRate, Is.EqualTo(8000));
            Assert.That(initial[1].MediaIdentity, Is.EqualTo("H264"));
            Assert.That(initial[1].ClockRate, Is.EqualTo(90000));
            Assert.That(remapped?.PayloadType, Is.EqualTo(96));
            Assert.That(remapped?.ClockRate, Is.EqualTo(48000));
            Assert.That(remapped?.MediaIdentity, Is.EqualTo("VP8"));
            Assert.That(remapped?.ChannelCount, Is.EqualTo(2));
            Assert.That(remapped?.TimestampUpdate?.ClockRateChanged, Is.True);
            Assert.That(remapped?.TimestampUpdate?.IsDiscontinuity, Is.True);
            Assert.That(unaffected?.ClockRate, Is.EqualTo(8000));
            Assert.That(unaffected?.TimestampUpdate?.ClockRateChanged,
                Is.False);
            Assert.That(changedMedia?.PayloadType, Is.EqualTo(0));
            Assert.That(changedMedia?.ClockRate, Is.EqualTo(8000));
            Assert.That(changedMedia?.MediaIdentity, Is.EqualTo("PCMU"));
            Assert.That(changedMedia?.TimestampUpdate?.ClockRateChanged,
                Is.True);
        });

        Assert.Throws<RtpSessionException>(() =>
            table.RecordMappedRemoteRtpPacket(
                CreateRemotePacket(300U, 1, 1U, 97),
                CreateContext(4L, 90000, 1L)));
        Assert.That(table.FindRemoteSourceSnapshot(300U), Is.Null);
    }

    [Test]
    public void MappedRemoteInputRequiresARegistryAndKnownPayloadType()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(2),
            new SequenceRandomSource(
                new uint[] { 1U },
                new ushort[] { 1 },
                new uint[] { 1U }));

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                table.ConfigureRemotePayloadTypeRegistry(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                table.RecordMappedRemoteRtpPacket(
                    CreateRemotePacket(100U, 1, 1U, 96),
                    CreateContext(1L, 90000, 1L)));
            Assert.That(table.GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    static RtpSessionConfiguration CreateConfiguration(int maximumSourceCount)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MaximumSourceCount = maximumSourceCount;
        configuration.MinimumSequential = 1;
        configuration.CanonicalName = "endpoint@example";
        return configuration;
    }

    static RtpPayloadTypeDefinition CreatePayloadDefinition(
        byte payloadType,
        int clockRate,
        string mediaIdentity,
        int channelCount)
    {
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = payloadType;
        definition.ClockRate = clockRate;
        definition.MediaIdentity = mediaIdentity;
        definition.ChannelCount = channelCount;
        return definition;
    }

    static RtpPacket CreatePacket(byte payloadType, uint payloadValue)
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = payloadType;
        packet.Payload = new byte[] { (byte)payloadValue };
        return packet;
    }

    static RtpPacket CreateRemotePacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        uint timestamp,
        byte payloadType)
    {
        RtpPacket packet = CreatePacket(payloadType, 1U);
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        return packet;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        int clockRate,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = "path";
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    private class SequenceRandomSource : IRtpRandomSource
    {
        uint[] sourceIdentifiers;
        ushort[] sequenceNumbers;
        uint[] timestamps;
        int sourceIndex;

        public SequenceRandomSource(
            uint[] sourceIdentifiers,
            ushort[] sequenceNumbers,
            uint[] timestamps)
        {
            this.sourceIdentifiers = sourceIdentifiers;
            this.sequenceNumbers = sequenceNumbers;
            this.timestamps = timestamps;
        }

        public uint CreateSynchronizationSource()
        {
            uint value = sourceIdentifiers[sourceIndex];
            sourceIndex = sourceIndex + 1;
            return value;
        }

        public ushort CreateSequenceNumber()
        {
            int valueIndex = sourceIndex - 1;
            if (valueIndex < 0)
            {
                valueIndex = 0;
            }

            return sequenceNumbers[valueIndex];
        }

        public uint CreateTimestamp()
        {
            int valueIndex = sourceIndex - 1;
            if (valueIndex < 0)
            {
                valueIndex = 0;
            }

            return timestamps[valueIndex];
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }
}
