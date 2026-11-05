using System.Collections.Concurrent;
using System.Threading;
using rtp;

namespace rtp.Tests.Session;

public class RtpSessionConcurrencyTests
{
    [Test]
    public void ConcurrentOutboundCallsCommitUniquePacketsAndStatistics()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(
            configuration,
            clock,
            null,
            null,
            null);
        ConcurrentBag<ushort> sequenceNumbers =
            new ConcurrentBag<ushort>();

        Parallel.For(0, 100, packetIndex =>
        {
            RtpSessionUpdate update = session.PrepareOutboundRtpPacket(
                CreatePacket(0U, 0, (uint)packetIndex),
                CreateContext(1L, packetIndex));
            sequenceNumbers.Add(
                update.OutboundPackets.Single().SequenceNumber);
        });

        RtpSourceSnapshot local = session.GetSessionSnapshot().Sources.Single(
            value => value.IsLocal);
        Assert.Multiple(() =>
        {
            Assert.That(sequenceNumbers, Has.Count.EqualTo(100));
            Assert.That(sequenceNumbers.Distinct().Count(), Is.EqualTo(100));
            Assert.That(local.TransmissionStatistics?.SentPacketCount,
                Is.EqualTo(100L));
            Assert.That(local.TransmissionStatistics?.SentOctetCount,
                Is.EqualTo(300L));
            Assert.That(session.outboundStream.packetHistory.entries,
                Has.Count.EqualTo(100));
        });
    }

    [Test]
    public void ConcurrentSnapshotsAndResetsReturnCompleteStateGraphs()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        RtpSession session = CreateSession(
            configuration,
            clock,
            null,
            null,
            null);
        ConcurrentBag<RtpSessionSnapshot> snapshots =
            new ConcurrentBag<RtpSessionSnapshot>();

        Parallel.For(0, 100, operationIndex =>
        {
            if (operationIndex % 10 == 0)
            {
                session.ResetSession();
            }
            else
            {
                snapshots.Add(session.GetSessionSnapshot());
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(snapshots, Is.Not.Empty);
            Assert.That(snapshots.All(
                value => value.Configuration.CanonicalName ==
                    "local@example"),
                Is.True);
            Assert.That(snapshots.All(
                value => value.LocalSourceIdentifiers.Length == 1),
                Is.True);
            Assert.That(snapshots.All(
                value => value.Sources.Length == 1 &&
                    value.Sources[0].IsLocal),
                Is.True);
            Assert.That(session.GetSessionSnapshot().MemberCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplicationHooksRunOutsideSessionAndComponentLocks()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        FixedClock clock = new FixedClock();
        LockObservingPlugin plugin = new LockObservingPlugin();
        RtpSession session = CreateSession(
            configuration,
            clock,
            plugin,
            plugin,
            plugin);
        plugin.Session = session;

        RtpSessionUpdate outbound = session.PrepareOutboundRtpPacket(
            CreatePacket(0U, 0, 0U),
            CreateContext(0L, 0));
        session.ProcessInboundRtpDatagram(
            new RtpPacketCodec(configuration).EncodeRtpPacketToArray(
                CreatePacket(500U, 10, 1000U)),
            CreateContext(1L, 1));
        uint localSourceIdentifier = session.GetSessionSnapshot()
            .LocalSourceIdentifiers.Single();
        ushort sentSequenceNumber = outbound.OutboundPackets.Single()
            .SequenceNumber;
        session.ProcessInboundRtcpDatagram(
            CreateNackDatagram(
                configuration,
                localSourceIdentifier,
                sentSequenceNumber),
            CreateContext(1L, 1));

        Assert.Multiple(() =>
        {
            Assert.That(plugin.PacketizerCallCount, Is.EqualTo(1));
            Assert.That(plugin.DepacketizerCallCount, Is.EqualTo(1));
            Assert.That(plugin.RepairEncoderCallCount, Is.EqualTo(1));
            Assert.That(plugin.SessionLockObserved, Is.False);
            Assert.That(plugin.OutboundLockObserved, Is.False);
            Assert.That(plugin.MediaLockObserved, Is.False);
            Assert.That(plugin.SnapshotCallCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ConcurrentInboundSourcesRemainIndependentAndComplete()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock clock = new FixedClock();
        CountingDepacketizer depacketizer = new CountingDepacketizer();
        RtpSession session = CreateSession(
            configuration,
            clock,
            null,
            depacketizer,
            null);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        ConcurrentBag<RtpSessionUpdate> updates =
            new ConcurrentBag<RtpSessionUpdate>();

        Parallel.For(0, 50, sourceIndex =>
        {
            uint sourceIdentifier = (uint)(1000 + sourceIndex);
            byte[] datagram = codec.EncodeRtpPacketToArray(
                CreatePacket(sourceIdentifier, 10, 1000U));
            updates.Add(session.ProcessInboundRtpDatagram(
                datagram,
                CreateContext(1L, sourceIndex)));
        });

        RtpSessionSnapshot snapshot = session.GetSessionSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(updates, Has.Count.EqualTo(50));
            Assert.That(updates.All(
                value => value.AcceptedPackets.Length == 1),
                Is.True);
            Assert.That(updates.SelectMany(
                value => value.AcceptedPackets)
                .Select(value => value.SynchronizationSource)
                .Distinct()
                .Count(),
                Is.EqualTo(50));
            Assert.That(snapshot.Sources.Count(value => !value.IsLocal),
                Is.EqualTo(50));
            Assert.That(depacketizer.CallCount, Is.EqualTo(50));
        });
    }

    static RtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock,
        IRtpPayloadPacketizer? packetizer,
        IRtpPayloadDepacketizer? depacketizer,
        IRtpRepairEncoder? repairEncoder)
    {
        return new RtpSession(
            configuration,
            clock,
            new FixedRandomSource(),
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            packetizer,
            depacketizer,
            repairEncoder,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 128;
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

    static RtpPacketContext CreateContext(
        long operationTimeTicks,
        int pathIndex)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = $"path-{pathIndex}";
        context.ClockRate = 90000;
        context.MediaClockTimestamp = operationTimeTicks;
        return context;
    }

    static byte[] CreateNackDatagram(
        RtpSessionConfiguration configuration,
        uint mediaSourceIdentifier,
        ushort sequenceNumber)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = sequenceNumber;
        RtcpGenericNack nack = new RtcpGenericNack();
        nack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.SenderSourceIdentifier = 500U;
        feedback.MediaSourceIdentifier = mediaSourceIdentifier;
        feedback.GenericNack = nack;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { packet };
        return new RtcpPacketCodec(configuration)
            .EncodeRtcpCompoundPacketToArray(compound);
    }

    private class FixedClock : IRtpClock
    {
        public long GetMonotonicTimeTicks()
        {
            return 1L;
        }

        public long GetUtcTimeTicks()
        {
            return 1L;
        }

        public ulong GetNtpTimestamp()
        {
            return 1UL;
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

    private class LockObservingPlugin :
        IRtpPayloadPacketizer,
        IRtpPayloadDepacketizer,
        IRtpRepairEncoder
    {
        public RtpSession? Session;
        public int PacketizerCallCount;
        public int DepacketizerCallCount;
        public int RepairEncoderCallCount;
        public int SnapshotCallCount;
        public bool SessionLockObserved;
        public bool OutboundLockObserved;
        public bool MediaLockObserved;

        public RtpPacketizationResult PacketizeRtpPayload(
            ReadOnlySpan<byte> mediaData,
            int maximumPayloadSize)
        {
            ObserveLocks();
            PacketizerCallCount = PacketizerCallCount + 1;
            RtpPacketizationResult result = new RtpPacketizationResult();
            result.Success = true;
            result.ConsumedCount = mediaData.Length;
            result.Payloads = new byte[][] { mediaData.ToArray() };
            result.Markers = new bool[] { true };
            return result;
        }

        public RtpDepacketizationResult DepacketizeRtpPayload(
            RtpPacket[] packets)
        {
            ObserveLocks();
            RtpSession? currentSession = Session;
            if (currentSession != null &&
                currentSession.ingress.inboundMediaProcessor != null &&
                Monitor.IsEntered(
                    currentSession.ingress.inboundMediaProcessor.syncRoot))
            {
                MediaLockObserved = true;
            }

            DepacketizerCallCount = DepacketizerCallCount + 1;
            RtpDepacketizationResult result = new RtpDepacketizationResult();
            result.Success = true;
            result.ConsumedPacketCount = packets.Length;
            result.Payload = (byte[])packets[0].Payload.Clone();
            return result;
        }

        public RtpRepairResult EncodeRtpRepair(
            RtpRepairRequest request,
            RtpPacket[] sourcePackets)
        {
            ObserveLocks();
            RepairEncoderCallCount = RepairEncoderCallCount + 1;
            RtpRepairResult result = new RtpRepairResult();
            result.Success = true;
            result.Kind = request.Kind;
            return result;
        }

        public void ObserveLocks()
        {
            RtpSession? currentSession = Session;
            if (currentSession == null)
            {
                return;
            }

            if (Monitor.IsEntered(currentSession.syncRoot))
            {
                SessionLockObserved = true;
            }

            if (Monitor.IsEntered(currentSession.outboundStream.syncRoot))
            {
                OutboundLockObserved = true;
            }

            currentSession.GetSessionSnapshot();
            SnapshotCallCount = SnapshotCallCount + 1;
        }
    }

    private class CountingDepacketizer : IRtpPayloadDepacketizer
    {
        public int CallCount;

        public RtpDepacketizationResult DepacketizeRtpPayload(
            RtpPacket[] packets)
        {
            Interlocked.Increment(ref CallCount);
            RtpDepacketizationResult result = new RtpDepacketizationResult();
            result.Success = true;
            result.ConsumedPacketCount = packets.Length;
            result.Payload = (byte[])packets[0].Payload.Clone();
            return result;
        }
    }
}
