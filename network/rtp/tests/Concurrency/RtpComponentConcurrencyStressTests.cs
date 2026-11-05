using System.Collections.Concurrent;
using System.Threading;
using rtp;

namespace rtp.Tests.Concurrency;

public class RtpComponentConcurrencyStressTests
{
    [Test]
    public void SharedCodecsRemainDeterministicUnderConcurrentUse()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);
        ConcurrentQueue<string> errors = new ConcurrentQueue<string>();
        ManualResetEventSlim startGate = new ManualResetEventSlim(false);
        Task[] tasks = new Task[8];
        int workerIndex = 0;
        while (workerIndex < tasks.Length)
        {
            int currentWorker = workerIndex;
            tasks[workerIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int operationIndex = 0;
                while (operationIndex < 200)
                {
                    int value = currentWorker * 200 + operationIndex;
                    RtpPacket packet = CreatePacket(
                        (uint)(1000 + value),
                        (ushort)value,
                        (uint)(9000 + value));
                    packet.Payload = new byte[] { (byte)(value % 251) };
                    RtpPacket decoded = rtpCodec.DecodeRtpPacket(
                        rtpCodec.EncodeRtpPacketToArray(packet));
                    if (decoded.SynchronizationSource !=
                            packet.SynchronizationSource ||
                        decoded.SequenceNumber != packet.SequenceNumber ||
                        decoded.Timestamp != packet.Timestamp ||
                        !decoded.Payload.SequenceEqual(packet.Payload))
                    {
                        errors.Enqueue("RTP round trip was inconsistent.");
                    }

                    RtcpCompoundPacket control =
                        CreateReceiverReportCompound((uint)(2000 + value));
                    RtcpCompoundPacket decodedControl = rtcpCodec
                        .DecodeRtcpCompoundPacket(
                            rtcpCodec.EncodeRtcpCompoundPacketToArray(
                                control));
                    if (decodedControl.Packets.Length != 1 ||
                        decodedControl.Packets[0].ReceiverReport == null ||
                        decodedControl.Packets[0].ReceiverReport!
                            .SenderSynchronizationSource !=
                            (uint)(2000 + value))
                    {
                        errors.Enqueue("RTCP round trip was inconsistent.");
                    }

                    operationIndex = operationIndex + 1;
                }
            });
            workerIndex = workerIndex + 1;
        }

        startGate.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "Codec workers deadlocked.");
            Assert.That(errors, Is.Empty);
        });
    }

    [Test]
    public void RegistriesPublishOnlyCompleteOrderedSnapshots()
    {
        RtpPayloadTypeRegistry payloadRegistry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpHeaderExtensionRegistry extensionRegistry =
            new RtpHeaderExtensionRegistry();
        ConcurrentQueue<string> errors = new ConcurrentQueue<string>();
        ManualResetEventSlim startGate = new ManualResetEventSlim(false);
        Task[] tasks = new Task[8];
        int writerIndex = 0;
        while (writerIndex < 4)
        {
            int currentWriter = writerIndex;
            tasks[writerIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int itemIndex = currentWriter;
                while (itemIndex < 64)
                {
                    payloadRegistry.SetPayloadTypeDefinition(
                        CreatePayloadDefinition(itemIndex));
                    extensionRegistry.SetHeaderExtensionMapping(
                        CreateExtensionMapping(itemIndex));
                    itemIndex = itemIndex + 4;
                }
            });
            writerIndex = writerIndex + 1;
        }

        int readerIndex = 4;
        while (readerIndex < tasks.Length)
        {
            tasks[readerIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int snapshotIndex = 0;
                while (snapshotIndex < 500)
                {
                    if (!IsCompletePayloadSnapshot(
                        payloadRegistry.GetPayloadTypeDefinitions()))
                    {
                        errors.Enqueue(
                            "A payload registry snapshot was torn.");
                    }

                    if (!IsCompleteExtensionSnapshot(
                        extensionRegistry.GetHeaderExtensionMappings()))
                    {
                        errors.Enqueue(
                            "An extension registry snapshot was torn.");
                    }

                    snapshotIndex = snapshotIndex + 1;
                }
            });
            readerIndex = readerIndex + 1;
        }

        startGate.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
        RtpPayloadTypeDefinition[] definitions =
            payloadRegistry.GetPayloadTypeDefinitions();
        RtpHeaderExtensionMapping[] mappings =
            extensionRegistry.GetHeaderExtensionMappings();

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "Registry workers deadlocked.");
            Assert.That(errors, Is.Empty);
            Assert.That(definitions, Has.Length.EqualTo(64));
            Assert.That(mappings, Has.Length.EqualTo(64));
            Assert.That(IsCompletePayloadSnapshot(definitions), Is.True);
            Assert.That(IsCompleteExtensionSnapshot(mappings), Is.True);
        });
    }

    [Test]
    public void TrackersAndSchedulerRemainCoherentUnderContention()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        RtpSequenceTracker sequenceTracker = new RtpSequenceTracker(
            configuration,
            new FixedRandomSource(100U));
        RtpTimestampTracker timestampTracker = new RtpTimestampTracker();
        RtcpScheduler scheduler = new RtcpScheduler(
            new FixedRandomSource(100U));
        scheduler.InitializeRtcpSchedule(
            configuration.RtcpScheduler,
            0L);
        RtpSessionSnapshot scheduleSnapshot = new RtpSessionSnapshot();
        scheduleSnapshot.MemberCount = 1;
        scheduleSnapshot.AverageRtcpPacketSize = 128;
        ConcurrentBag<ushort> outboundSequences =
            new ConcurrentBag<ushort>();
        ConcurrentBag<RtpTimestampUpdate> timestampUpdates =
            new ConcurrentBag<RtpTimestampUpdate>();
        ConcurrentBag<RtcpScheduleDecision> scheduleDecisions =
            new ConcurrentBag<RtcpScheduleDecision>();
        ManualResetEventSlim startGate = new ManualResetEventSlim(false);
        Task[] tasks = new Task[10];
        int taskIndex = 0;
        while (taskIndex < 4)
        {
            tasks[taskIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int operationIndex = 0;
                while (operationIndex < 250)
                {
                    outboundSequences.Add(
                        sequenceTracker.AdvanceRtpSequenceNumber());
                    operationIndex = operationIndex + 1;
                }
            });
            taskIndex = taskIndex + 1;
        }

        while (taskIndex < 8)
        {
            int currentTask = taskIndex;
            tasks[taskIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int operationIndex = 0;
                while (operationIndex < 250)
                {
                    uint timestamp = (uint)(
                        (currentTask - 4) * 250 + operationIndex);
                    timestampUpdates.Add(
                        timestampTracker.ProcessRtpTimestamp(
                            timestamp,
                            90000));
                    RtpTimestampUpdate snapshot = timestampTracker
                        .GetRtpTimestampState();
                    if (snapshot.Accepted)
                    {
                        timestampUpdates.Add(snapshot);
                    }

                    operationIndex = operationIndex + 1;
                }
            });
            taskIndex = taskIndex + 1;
        }

        while (taskIndex < tasks.Length)
        {
            tasks[taskIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int operationIndex = 0;
                while (operationIndex < 500)
                {
                    int memberCount = 1 + operationIndex % 8;
                    scheduler.RecordRtcpPacketSize(
                        80 + operationIndex % 40);
                    scheduler.ReconsiderRtcpSchedule(
                        memberCount,
                        operationIndex % (memberCount + 1),
                        0L);
                    scheduleDecisions.Add(scheduler.PollRtcpSchedule(
                        scheduleSnapshot,
                        0L));
                    operationIndex = operationIndex + 1;
                }
            });
            taskIndex = taskIndex + 1;
        }

        startGate.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True,
                "Tracker or scheduler workers deadlocked.");
            Assert.That(outboundSequences, Has.Count.EqualTo(1000));
            Assert.That(outboundSequences.Distinct().Count(),
                Is.EqualTo(1000));
            Assert.That(timestampUpdates, Is.Not.Empty);
            Assert.That(timestampUpdates.All(value =>
                value.Accepted &&
                value.ClockRate == 90000 &&
                value.ExtendedTimestamp == value.Timestamp), Is.True);
            Assert.That(scheduleDecisions, Has.Count.EqualTo(1000));
            Assert.That(scheduleDecisions.All(value =>
                value.MemberCount >= 1 &&
                value.SenderCount >= 0 &&
                value.SenderCount <= value.MemberCount &&
                value.ScheduledTimeTicks >= 0L &&
                value.DelayTicks >= 0L), Is.True);
        });
    }

    [Test]
    public void BufferAndHistoryConcurrentWritersDoNotDuplicateReleases()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.JitterBuffer.Capacity = 256;
        configuration.MaximumPacketHistoryCount = 256;
        configuration.MaximumPacketHistoryBytes = 65536;
        RtpJitterBuffer jitterBuffer = new RtpJitterBuffer(
            configuration.JitterBuffer);
        RtpPacketHistory history = new RtpPacketHistory(configuration);
        ConcurrentBag<ushort> releasedSequences =
            new ConcurrentBag<ushort>();
        ConcurrentQueue<ushort> missingHistory =
            new ConcurrentQueue<ushort>();
        ManualResetEventSlim startGate = new ManualResetEventSlim(false);
        Task[] tasks = new Task[8];
        int taskIndex = 0;
        while (taskIndex < tasks.Length)
        {
            int currentTask = taskIndex;
            tasks[taskIndex] = Task.Run(() =>
            {
                startGate.Wait();
                int packetIndex = currentTask;
                while (packetIndex < 200)
                {
                    ushort sequenceNumber = (ushort)packetIndex;
                    RtpPacket packet = CreatePacket(
                        700U,
                        sequenceNumber,
                        (uint)(1000 + packetIndex));
                    RtpJitterBufferUpdate update = jitterBuffer
                        .TryAddRtpJitterBufferPacket(
                            packet,
                            CreateContext(1L, "shared"),
                            CreateSequenceUpdate(sequenceNumber, 199U));
                    int releasedIndex = 0;
                    while (releasedIndex < update.ReleasedPackets.Length)
                    {
                        releasedSequences.Add(update.ReleasedPackets[
                            releasedIndex].SequenceNumber);
                        releasedIndex = releasedIndex + 1;
                    }

                    if (!history.TryStoreRtpHistoryPacket(
                        packet,
                        CreateContext(1L, "shared")))
                    {
                        missingHistory.Enqueue(sequenceNumber);
                    }

                    packetIndex = packetIndex + tasks.Length;
                }
            });
            taskIndex = taskIndex + 1;
        }

        startGate.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
        RtpJitterBufferUpdate flushed = jitterBuffer.FlushRtpJitterBuffer();
        int flushIndex = 0;
        while (flushIndex < flushed.ReleasedPackets.Length)
        {
            releasedSequences.Add(
                flushed.ReleasedPackets[flushIndex].SequenceNumber);
            flushIndex = flushIndex + 1;
        }

        int historyIndex = 0;
        while (historyIndex < 200)
        {
            if (history.FindRtpHistoryPacket(
                700U,
                (ushort)historyIndex) == null)
            {
                missingHistory.Enqueue((ushort)historyIndex);
            }

            historyIndex = historyIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True,
                "Buffer or history workers deadlocked.");
            Assert.That(missingHistory, Is.Empty);
            Assert.That(releasedSequences, Has.Count.EqualTo(200));
            Assert.That(releasedSequences.Distinct().Count(),
                Is.EqualTo(200));
            Assert.That(releasedSequences.OrderBy(value => value),
                Is.EqualTo(Enumerable.Range(0, 200).Select(
                    value => (ushort)value)));
        });
    }

    [Test]
    public void SessionStressCompletesWithCoherentSnapshotsAndUnlockedHooks()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.MaximumSourceCount = 256;
        FixedClock clock = new FixedClock();
        LockCheckingMediaHooks hooks = new LockCheckingMediaHooks();
        RtpSession session = new RtpSession(
            configuration,
            clock,
            new FixedRandomSource(100U),
            CreateSessionPayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            hooks,
            hooks,
            null,
            null);
        hooks.Session = session;
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        ConcurrentBag<ushort> outboundSequences =
            new ConcurrentBag<ushort>();
        ConcurrentBag<RtpSessionSnapshot> snapshots =
            new ConcurrentBag<RtpSessionSnapshot>();
        ManualResetEventSlim startGate = new ManualResetEventSlim(false);

        Task[] tasks = new Task[]
        {
            Task.Run(() =>
            {
                startGate.Wait();
                Parallel.For(0, 100, packetIndex =>
                {
                    RtpSessionUpdate update =
                        session.PrepareOutboundRtpPacket(
                            CreatePacket(0U, 0, (uint)packetIndex),
                            CreateContext(1L, "local"));
                    outboundSequences.Add(
                        update.OutboundPackets.Single().SequenceNumber);
                });
            }),
            Task.Run(() =>
            {
                startGate.Wait();
                Parallel.For(0, 100, sourceIndex =>
                {
                    RtpPacket packet = CreatePacket(
                        (uint)(1000 + sourceIndex),
                        10,
                        1000U);
                    session.ProcessInboundRtpDatagram(
                        codec.EncodeRtpPacketToArray(packet),
                        CreateContext(1L, $"remote-{sourceIndex}"));
                });
            }),
            Task.Run(() =>
            {
                startGate.Wait();
                int snapshotIndex = 0;
                while (snapshotIndex < 1000)
                {
                    snapshots.Add(session.GetSessionSnapshot());
                    snapshotIndex = snapshotIndex + 1;
                }
            })
        };

        startGate.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
        RtpSessionSnapshot final = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "Session workers deadlocked.");
            Assert.That(snapshots, Is.Not.Empty);
            Assert.That(snapshots.All(IsCoherentSessionSnapshot), Is.True);
            Assert.That(IsCoherentSessionSnapshot(final), Is.True);
            Assert.That(final.Sources.Count(value => !value.IsLocal),
                Is.EqualTo(100));
            Assert.That(outboundSequences, Has.Count.EqualTo(100));
            Assert.That(outboundSequences.Distinct().Count(),
                Is.EqualTo(100));
            Assert.That(hooks.PacketizerCallCount, Is.EqualTo(100));
            Assert.That(hooks.DepacketizerCallCount, Is.EqualTo(100));
            Assert.That(hooks.LockObserved, Is.EqualTo(0));
        });
    }

    static bool IsCompletePayloadSnapshot(
        RtpPayloadTypeDefinition[] definitions)
    {
        int definitionIndex = 0;
        while (definitionIndex < definitions.Length)
        {
            RtpPayloadTypeDefinition definition =
                definitions[definitionIndex];
            int value = definition.PayloadType - 64;
            if (value < 0 || value >= 64 ||
                definition.ClockRate != 8000 + value ||
                definition.MediaIdentity != $"media-{value}" ||
                definition.ChannelCount != 1 ||
                (definitionIndex > 0 && definitions[definitionIndex - 1]
                    .PayloadType >= definition.PayloadType))
            {
                return false;
            }

            definitionIndex = definitionIndex + 1;
        }

        return true;
    }

    static bool IsCompleteExtensionSnapshot(
        RtpHeaderExtensionMapping[] mappings)
    {
        int mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            RtpHeaderExtensionMapping mapping = mappings[mappingIndex];
            int value = mapping.Identifier - 1;
            if (value < 0 || value >= 64 ||
                mapping.Uri != $"urn:stress:{value}" ||
                !mapping.SupportsTwoByte ||
                mapping.PreferredFormat !=
                    RtpHeaderExtensionFormat.TwoByte ||
                (mappingIndex > 0 && mappings[mappingIndex - 1]
                    .Identifier >= mapping.Identifier))
            {
                return false;
            }

            mappingIndex = mappingIndex + 1;
        }

        return true;
    }

    static bool IsCoherentSessionSnapshot(RtpSessionSnapshot snapshot)
    {
        return snapshot.LocalSourceIdentifiers.Length == 1 &&
            snapshot.Sources.Count(value => value.IsLocal) == 1 &&
            snapshot.Sources.Select(value => value.SourceIdentifier)
                .Distinct().Count() == snapshot.Sources.Length &&
            snapshot.MemberCount == snapshot.Sources.Length &&
            snapshot.SenderCount == snapshot.Sources.Count(
                value => value.IsSender);
    }

    static RtpPayloadTypeDefinition CreatePayloadDefinition(int value)
    {
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
        definition.PayloadType = (byte)(64 + value);
        definition.ClockRate = 8000 + value;
        definition.MediaIdentity = $"media-{value}";
        definition.ChannelCount = 1;
        return definition;
    }

    static RtpHeaderExtensionMapping CreateExtensionMapping(int value)
    {
        RtpHeaderExtensionMapping mapping =
            new RtpHeaderExtensionMapping();
        mapping.Identifier = value + 1;
        mapping.Uri = $"urn:stress:{value}";
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.TwoByte;
        return mapping;
    }

    static RtpSequenceUpdate CreateSequenceUpdate(
        ushort sequenceNumber,
        uint highestSequenceNumber)
    {
        RtpSequenceUpdate update = new RtpSequenceUpdate();
        update.Status = RtpSequenceStatus.Accepted;
        update.SequenceNumber = sequenceNumber;
        update.ExtendedSequenceNumber = sequenceNumber;
        update.BaseExtendedSequenceNumber = 0U;
        update.HighestExtendedSequenceNumber = highestSequenceNumber;
        update.Accepted = true;
        return update;
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 128;
        configuration.MaximumPacketHistoryCount = 512;
        configuration.MaximumPacketHistoryBytes = 65536;
        configuration.CanonicalName = "stress@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreateSessionPayloadRegistry()
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
        packet.Payload = new byte[] { 1 };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long operationTimeTicks,
        string transportIdentity)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = 90000;
        context.MediaClockTimestamp = operationTimeTicks;
        return context;
    }

    static RtcpCompoundPacket CreateReceiverReportCompound(
        uint sourceIdentifier)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = receiverReport;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { packet };
        return compound;
    }

    private class LockCheckingMediaHooks :
        IRtpPayloadPacketizer,
        IRtpPayloadDepacketizer
    {
        public RtpSession? Session;
        public int PacketizerCallCount;
        public int DepacketizerCallCount;
        public int LockObserved;

        public RtpPacketizationResult PacketizeRtpPayload(
            ReadOnlySpan<byte> mediaData,
            int maximumPayloadSize)
        {
            ObserveLocks();
            Interlocked.Increment(ref PacketizerCallCount);
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
            RtpSession? session = Session;
            if (session != null &&
                session.ingress.inboundMediaProcessor != null &&
                Monitor.IsEntered(
                    session.ingress.inboundMediaProcessor.syncRoot))
            {
                Interlocked.Exchange(ref LockObserved, 1);
            }

            Interlocked.Increment(ref DepacketizerCallCount);
            RtpDepacketizationResult result =
                new RtpDepacketizationResult();
            result.Success = true;
            result.ConsumedPacketCount = packets.Length;
            result.Payload = packets[0].Payload.ToArray();
            return result;
        }

        public void ObserveLocks()
        {
            RtpSession? session = Session;
            if (session == null)
            {
                return;
            }

            if (Monitor.IsEntered(session.syncRoot) ||
                Monitor.IsEntered(session.outboundStream.syncRoot))
            {
                Interlocked.Exchange(ref LockObserved, 1);
            }
        }
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
        uint sourceIdentifier;

        public FixedRandomSource(uint sourceIdentifier)
        {
            this.sourceIdentifier = sourceIdentifier;
        }

        public uint CreateSynchronizationSource()
        {
            return sourceIdentifier;
        }

        public ushort CreateSequenceNumber()
        {
            return 0;
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
