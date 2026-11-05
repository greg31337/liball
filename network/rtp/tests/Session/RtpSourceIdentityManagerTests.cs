using rtp;

namespace rtp.Tests.Session;

public class RtpSourceIdentityManagerTests
{
    [Test]
    public void LocalCollisionCreatesGoodbyeAndResetsWithUniqueIdentity()
    {
        ScriptRandomSource localRandom = new ScriptRandomSource(
            new uint[] { 100U, 200U, 300U, 200U, 400U },
            new ushort[] { 10, 20, 30 },
            new uint[] { 1000U, 2000U, 3000U });
        RtpLocalSourceTable localTable = new RtpLocalSourceTable(
            CreateConfiguration(8),
            localRandom);
        RtpLocalSourceState collidingSource =
            localTable.CreateLocalSourceState();
        localTable.CreateLocalSourceState();
        RtpOutboundPacketBuilder builder = CreateBuilder();
        builder.BuildOutboundPacket(
            collidingSource,
            CreatePacket(96),
            CreateContext(10L, "local-path", 8000, 100L));

        RtpSourceTable remoteTable = CreateRemoteTable();
        remoteTable.RecordRemoteRtcpActivity(
            300U,
            CreateContext(1L, "remote-path", 0, 0L));
        RtpSourceIdentityManager manager = new RtpSourceIdentityManager(
            localTable,
            remoteTable);

        RtpSourceIdentityUpdate update =
            manager.InspectIncomingSourceIdentity(
                100U,
                CreateContext(11L, "other-path", 8000, 101L));
        RtpSourceSnapshot? resetSnapshot =
            localTable.FindLocalSourceSnapshot(400U);
        RtpPacket restarted = builder.BuildOutboundPacket(
            collidingSource,
            CreatePacket(96),
            CreateContext(12L, "new-local-path", 8000, 500L));

        Assert.Multiple(() =>
        {
            Assert.That(update.collisionDetected, Is.True);
            Assert.That(update.loopDetected, Is.False);
            Assert.That(update.previousSourceIdentifier, Is.EqualTo(100U));
            Assert.That(update.currentSourceIdentifier, Is.EqualTo(400U));
            Assert.That(update.goodbye?.Sources,
                Is.EqualTo(new uint[] { 100U }));
            Assert.That(update.goodbye?.HasReason, Is.False);
            Assert.That(update.sourceSnapshot?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(update.sourceSnapshot?.IsSender, Is.False);
            Assert.That(resetSnapshot?.PayloadType, Is.Zero);
            Assert.That(resetSnapshot?.ClockRate, Is.Zero);
            Assert.That(resetSnapshot?.MediaIdentity, Is.Empty);
            Assert.That(resetSnapshot?.LastRtpActivityTicks, Is.Zero);
            Assert.That(resetSnapshot?.TransportIdentity, Is.Empty);
            Assert.That(localTable.GetLocalSourceIdentifiers(),
                Is.EqualTo(new uint[] { 200U, 400U }));
            Assert.That(localTable.FindLocalSourceState(400U),
                Is.SameAs(collidingSource));
            Assert.That(restarted.SynchronizationSource, Is.EqualTo(400U));
            Assert.That(restarted.SequenceNumber, Is.EqualTo(30));
            Assert.That(restarted.Timestamp, Is.EqualTo(3000U));
        });
    }

    [Test]
    public void OwnPacketOnRecordedTransportIsDetectedAsALoop()
    {
        ScriptRandomSource localRandom = new ScriptRandomSource(
            new uint[] { 100U, 200U },
            new ushort[] { 10, 20 },
            new uint[] { 1000U, 2000U });
        RtpLocalSourceTable localTable = new RtpLocalSourceTable(
            CreateConfiguration(4),
            localRandom);
        RtpLocalSourceState source = localTable.CreateLocalSourceState();
        CreateBuilder().BuildOutboundPacket(
            source,
            CreatePacket(96),
            CreateContext(10L, "loop-path", 8000, 100L));
        RtpSourceIdentityManager manager = new RtpSourceIdentityManager(
            localTable,
            CreateRemoteTable());

        RtpSourceIdentityUpdate update =
            manager.InspectIncomingSourceIdentity(
                100U,
                CreateContext(11L, "loop-path", 8000, 101L));
        RtpSourceSnapshot? snapshot =
            localTable.FindLocalSourceSnapshot(100U);

        Assert.Multiple(() =>
        {
            Assert.That(update.collisionDetected, Is.False);
            Assert.That(update.loopDetected, Is.True);
            Assert.That(update.goodbye, Is.Null);
            Assert.That(update.currentSourceIdentifier, Is.EqualTo(100U));
            Assert.That(update.sourceSnapshot?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(snapshot?.IsSender, Is.True);
            Assert.That(snapshot?.TransportIdentity, Is.EqualTo("loop-path"));
            Assert.That(localRandom.sourceIdentifierCallCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RemoteSourceOnAnotherTransportIsMarkedAsCollision()
    {
        RtpLocalSourceTable localTable = new RtpLocalSourceTable(
            CreateConfiguration(4),
            new ScriptRandomSource(
                new uint[] { 500U },
                new ushort[] { 1 },
                new uint[] { 1U }));
        RtpSourceTable remoteTable = CreateRemoteTable();
        remoteTable.RecordRemoteRtcpActivity(
            300U,
            CreateContext(5L, "first-path", 0, 0L));
        RtpSourceIdentityManager manager = new RtpSourceIdentityManager(
            localTable,
            remoteTable);

        RtpSourceIdentityUpdate sameTransport =
            manager.InspectIncomingSourceIdentity(
                300U,
                CreateContext(6L, "first-path", 0, 0L));
        RtpSourceIdentityUpdate collision =
            manager.InspectIncomingSourceIdentity(
                300U,
                CreateContext(6L, "second-path", 0, 0L));
        RtpSourceIdentityUpdate unknown =
            manager.InspectIncomingSourceIdentity(
                999U,
                CreateContext(6L, "third-path", 0, 0L));
        RtpSourceSnapshot? snapshot =
            remoteTable.FindRemoteSourceSnapshot(300U);
        RtpSourceState? state = remoteTable.FindSourceState(300U);

        Assert.Multiple(() =>
        {
            Assert.That(sameTransport.collisionDetected, Is.False);
            Assert.That(sameTransport.loopDetected, Is.False);
            Assert.That(collision.collisionDetected, Is.True);
            Assert.That(collision.loopDetected, Is.False);
            Assert.That(collision.goodbye, Is.Null);
            Assert.That(collision.sourceSnapshot?.Status,
                Is.EqualTo(RtpSourceStatus.Collision));
            Assert.That(snapshot?.Status,
                Is.EqualTo(RtpSourceStatus.Collision));
            Assert.That(snapshot?.TransportIdentity,
                Is.EqualTo("first-path"));
            Assert.That(state?.collisionTransportIdentity,
                Is.EqualTo("second-path"));
            Assert.That(unknown.collisionDetected, Is.False);
            Assert.That(unknown.sourceSnapshot, Is.Null);
        });
    }

    [Test]
    public void FailedReselectionAndInvalidIdentityLeaveSourcesUnchanged()
    {
        uint[] repeatedIdentifiers = new uint[33];
        int identifierIndex = 0;
        while (identifierIndex < repeatedIdentifiers.Length)
        {
            repeatedIdentifiers[identifierIndex] = 100U;
            identifierIndex = identifierIndex + 1;
        }

        RtpLocalSourceTable localTable = new RtpLocalSourceTable(
            CreateConfiguration(4),
            new ScriptRandomSource(
                repeatedIdentifiers,
                new ushort[] { 10 },
                new uint[] { 1000U }));
        RtpLocalSourceState source = localTable.CreateLocalSourceState();
        RtpSourceIdentityManager manager = new RtpSourceIdentityManager(
            localTable,
            CreateRemoteTable());

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpSessionException>(() =>
                manager.InspectIncomingSourceIdentity(
                    100U,
                    CreateContext(1L, "collision", 0, 0L)));
            Assert.Throws<RtpConfigurationException>(() =>
                manager.InspectIncomingSourceIdentity(
                    100U,
                    CreateContext(1L, "", 0, 0L)));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpSourceIdentityManager(null!, CreateRemoteTable()));
            Assert.That(localTable.GetLocalSourceIdentifiers(),
                Is.EqualTo(new uint[] { 100U }));
            Assert.That(localTable.FindLocalSourceState(100U),
                Is.SameAs(source));
            Assert.That(source.nextSequenceNumber, Is.EqualTo(10));
            Assert.That(source.initialTimestamp, Is.EqualTo(1000U));
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

    static RtpSourceTable CreateRemoteTable()
    {
        return new RtpSourceTable(
            CreateConfiguration(8),
            new ScriptRandomSource(
                new uint[] { 1U },
                new ushort[] { 1 },
                new uint[] { 1U }));
    }

    static RtpOutboundPacketBuilder CreateBuilder()
    {
        RtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
        definition.PayloadType = 96;
        definition.ClockRate = 8000;
        definition.MediaIdentity = "audio";
        definition.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(definition);
        return new RtpOutboundPacketBuilder(
            registry,
            new RtpHeaderExtensionRegistry());
    }

    static RtpPacket CreatePacket(byte payloadType)
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = payloadType;
        packet.Payload = new byte[] { 1 };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        string transportIdentity,
        int clockRate,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    private class ScriptRandomSource : IRtpRandomSource
    {
        uint[] sourceIdentifiers;
        ushort[] sequenceNumbers;
        uint[] timestamps;
        int sourceIdentifierIndex;
        int sequenceNumberIndex;
        int timestampIndex;
        internal int sourceIdentifierCallCount;

        public ScriptRandomSource(
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
            uint value = sourceIdentifiers[sourceIdentifierIndex];
            sourceIdentifierIndex = sourceIdentifierIndex + 1;
            sourceIdentifierCallCount = sourceIdentifierCallCount + 1;
            return value;
        }

        public ushort CreateSequenceNumber()
        {
            ushort value = sequenceNumbers[sequenceNumberIndex];
            sequenceNumberIndex = sequenceNumberIndex + 1;
            return value;
        }

        public uint CreateTimestamp()
        {
            uint value = timestamps[timestampIndex];
            timestampIndex = timestampIndex + 1;
            return value;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }
}
