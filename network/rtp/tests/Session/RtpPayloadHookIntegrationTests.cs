using rtp;

namespace rtp.Tests.Session;

public class RtpPayloadHookIntegrationTests
{
    [Test]
    public void PacketizedMediaRoundTripsWithMarkersTimestampsAndStatistics()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock senderClock = new FixedClock();
        FixedClock receiverClock = new FixedClock();
        RecordingPacketizer packetizer = new RecordingPacketizer();
        packetizer.Payloads = new byte[][]
        {
            new byte[] { 1, 2 },
            new byte[] { 3, 4, 5 }
        };
        packetizer.Markers = new bool[] { false, true };
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        IRtpSession sender = CreateSession(
            configuration,
            senderClock,
            100U,
            packetizer,
            null);
        IRtpSession receiver = CreateSession(
            configuration,
            receiverClock,
            200U,
            null,
            depacketizer);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        RtpPacket media = CreatePacket(0U, 0, 0U, false, 0);
        media.Payload = new byte[] { 1, 2, 3, 4, 5 };

        senderClock.MonotonicTimeTicks = 1L;
        RtpSessionUpdate sent = sender.PrepareOutboundRtpPacket(
            media,
            CreateContext(1L, "sender", 1000L));
        receiverClock.MonotonicTimeTicks = 1L;
        RtpSessionUpdate first = receiver.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(sent.OutboundPackets[0]),
            CreateContext(1L, "sender", 1000L));
        receiverClock.MonotonicTimeTicks = 2L;
        RtpSessionUpdate second = receiver.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(sent.OutboundPackets[1]),
            CreateContext(2L, "sender", 1001L));

        RtpDepacketizationResult assembled = second.Events.Single(
            value => value.DepacketizationResult != null)
            .DepacketizationResult!;
        RtpSourceSnapshot sentSource = sender.GetSessionSnapshot()
            .Sources.Single(value => value.IsLocal);
        RtpSourceSnapshot receivedSource = receiver.GetSessionSnapshot()
            .Sources.Single(value => value.SourceIdentifier == 100U);

        Assert.Multiple(() =>
        {
            Assert.That(packetizer.CallCount, Is.EqualTo(1));
            Assert.That(packetizer.MediaData,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(packetizer.MaximumPayloadSize,
                Is.GreaterThanOrEqualTo(5));
            Assert.That(sent.OutboundPackets, Has.Length.EqualTo(2));
            Assert.That(sent.OutboundPackets.Select(value => value.Marker),
                Is.EqualTo(new bool[] { false, true }));
            Assert.That(sent.OutboundPackets.Select(value => value.Timestamp),
                Is.EqualTo(new uint[] { 1000U, 1000U }));
            Assert.That(sent.OutboundPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 10, 11 }));
            Assert.That(first.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(first.Events.All(
                value => value.DepacketizationResult == null), Is.True);
            Assert.That(depacketizer.SequenceBatches,
                Is.EqualTo(new ushort[][]
                {
                    new ushort[] { 10, 11 }
                }));
            Assert.That(assembled.Payload,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(assembled.Timestamp, Is.EqualTo(1000U));
            Assert.That(assembled.Marker, Is.True);
            Assert.That(sentSource.TransmissionStatistics?.SentPacketCount,
                Is.EqualTo(2L));
            Assert.That(sentSource.TransmissionStatistics?.SentOctetCount,
                Is.EqualTo(5L));
            Assert.That(receivedSource.ReceptionStatistics?
                .ReceivedPacketCount, Is.EqualTo(2L));
            Assert.That(receivedSource.ReceptionStatistics?
                .ReceivedOctetCount, Is.EqualTo(5L));
        });
    }

    [Test]
    public void LossAndTimestampDiscontinuityDiscardIncompleteMediaGroups()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.JitterBuffer.MaximumLatencyTicks = 5L;
        FixedClock clock = new FixedClock();
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        IRtpSession receiver = CreateSession(
            configuration,
            clock,
            200U,
            null,
            depacketizer);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);

        ProcessPacket(receiver, codec, clock,
            CreatePacket(500U, 10, 100U, false, 10), 0L);
        ProcessPacket(receiver, codec, clock,
            CreatePacket(500U, 12, 100U, true, 12), 1L);
        RtpSessionUpdate loss = ProcessPacket(
            receiver,
            codec,
            clock,
            CreatePacket(500U, 13, 200U, true, 13),
            6L);
        ProcessPacket(receiver, codec, clock,
            CreatePacket(500U, 14, 300U, false, 14), 7L);
        RtpSessionUpdate discontinuity = ProcessPacket(
            receiver,
            codec,
            clock,
            CreatePacket(500U, 15, 400U, true, 15),
            8L);
        RtpSourceSnapshot source = receiver.GetSessionSnapshot()
            .Sources.Single(value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(loss.Events.Any(
                value => value.Type == RtpSessionEventType.Loss &&
                    value.JitterBufferUpdate != null &&
                    value.JitterBufferUpdate.LostSequenceNumbers.Contains(
                        (ushort)11)), Is.True);
            Assert.That(depacketizer.SequenceBatches,
                Is.EqualTo(new ushort[][]
                {
                    new ushort[] { 12 },
                    new ushort[] { 13 },
                    new ushort[] { 15 }
                }));
            Assert.That(depacketizer.PayloadBatches,
                Is.EqualTo(new byte[][]
                {
                    new byte[] { 12 },
                    new byte[] { 13 },
                    new byte[] { 15 }
                }));
            Assert.That(discontinuity.Events.Any(
                value => value.Message != null &&
                    value.Message.Contains("timestamp changed")),
                Is.True);
            Assert.That(discontinuity.Events.Single(
                value => value.DepacketizationResult != null)
                .DepacketizationResult?.Timestamp, Is.EqualTo(400U));
            Assert.That(source.ReceptionStatistics?.ReceivedPacketCount,
                Is.EqualTo(5L));
            Assert.That(source.ReceptionStatistics?.ReceivedOctetCount,
                Is.EqualTo(5L));
            Assert.That(source.ReceptionStatistics?.CumulativePacketsLost,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void DeclaredHookFailuresAreRecoverableAndLaterMediaSucceeds()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        FixedClock senderClock = new FixedClock();
        RecordingPacketizer packetizer = new RecordingPacketizer();
        packetizer.Fail = true;
        IRtpSession sender = CreateSession(
            configuration,
            senderClock,
            100U,
            packetizer,
            null);
        RtpPacket outboundMedia = CreatePacket(0U, 0, 0U, true, 9);

        RtpSessionUpdate failedSend = sender.PrepareOutboundRtpPacket(
            outboundMedia,
            CreateContext(0L, "sender", 1000L));
        packetizer.Fail = false;
        packetizer.Payloads = new byte[][] { new byte[] { 9 } };
        packetizer.Markers = new bool[] { true };
        RtpSessionUpdate successfulSend = sender.PrepareOutboundRtpPacket(
            outboundMedia,
            CreateContext(0L, "sender", 1000L));
        RtpSourceSnapshot senderSource = sender.GetSessionSnapshot()
            .Sources.Single(value => value.IsLocal);

        FixedClock receiverClock = new FixedClock();
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Fail = true;
        IRtpSession receiver = CreateSession(
            configuration,
            receiverClock,
            200U,
            null,
            depacketizer);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        RtpSessionUpdate failedReceive = ProcessPacket(
            receiver,
            codec,
            receiverClock,
            CreatePacket(500U, 20, 200U, true, 20),
            0L);
        depacketizer.Fail = false;
        RtpSessionUpdate successfulReceive = ProcessPacket(
            receiver,
            codec,
            receiverClock,
            CreatePacket(500U, 21, 300U, true, 21),
            1L);
        RtpSourceSnapshot receiverSource = receiver.GetSessionSnapshot()
            .Sources.Single(value => value.SourceIdentifier == 500U);

        Assert.Multiple(() =>
        {
            Assert.That(failedSend.OutboundPackets, Is.Empty);
            Assert.That(failedSend.Events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(failedSend.Events[0].Message,
                Is.EqualTo("packetizer unavailable"));
            Assert.That(successfulSend.OutboundPackets.Single()
                .SequenceNumber, Is.EqualTo(10));
            Assert.That(senderSource.TransmissionStatistics?.SentPacketCount,
                Is.EqualTo(1L));
            Assert.That(senderSource.TransmissionStatistics?.SentOctetCount,
                Is.EqualTo(1L));
            Assert.That(failedReceive.AcceptedPackets,
                Has.Length.EqualTo(1));
            Assert.That(failedReceive.Events.Any(
                value => value.Type == RtpSessionEventType.RecoverableError &&
                    value.Message == "depacketizer unavailable"), Is.True);
            Assert.That(successfulReceive.Events.Single(
                value => value.DepacketizationResult != null)
                .DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 21 }));
            Assert.That(depacketizer.CallCount, Is.EqualTo(2));
            Assert.That(receiverSource.ReceptionStatistics?
                .ReceivedPacketCount, Is.EqualTo(2L));
            Assert.That(receiverSource.ReceptionStatistics?
                .ReceivedOctetCount, Is.EqualTo(2L));
            Assert.That(receiverSource.ReceptionStatistics?
                .DiscardedPacketCount, Is.Zero);
        });
    }

    static RtpSessionUpdate ProcessPacket(
        IRtpSession session,
        IRtpPacketCodec codec,
        FixedClock clock,
        RtpPacket packet,
        long arrivalTimeTicks)
    {
        clock.MonotonicTimeTicks = arrivalTimeTicks;
        return session.ProcessInboundRtpDatagram(
            codec.EncodeRtpPacketToArray(packet),
            CreateContext(
                arrivalTimeTicks,
                "remote",
                arrivalTimeTicks * 90L));
    }

    static IRtpSession CreateSession(
        RtpSessionConfiguration configuration,
        FixedClock clock,
        uint localSourceIdentifier,
        IRtpPayloadPacketizer? packetizer,
        IRtpPayloadDepacketizer? depacketizer)
    {
        return new RtpSession(
            configuration,
            clock,
            new FixedRandomSource(localSourceIdentifier),
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            packetizer,
            depacketizer,
            null,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 8;
        configuration.MaximumRtpPacketSize = 64;
        configuration.CanonicalName = "integration@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
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
        uint timestamp,
        bool marker,
        byte payloadValue)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = timestamp;
        packet.PayloadType = 96;
        packet.Marker = marker;
        packet.Payload = new byte[] { payloadValue };
        return packet;
    }

    static RtpPacketContext CreateContext(
        long operationTimeTicks,
        string transportIdentity,
        long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = operationTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = 90000;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    private class RecordingPacketizer : IRtpPayloadPacketizer
    {
        public bool Fail;
        public int CallCount;
        public byte[] MediaData = [];
        public int MaximumPayloadSize;
        public byte[][] Payloads = [];
        public bool[] Markers = [];

        public RtpPacketizationResult PacketizeRtpPayload(
            ReadOnlySpan<byte> mediaData,
            int maximumPayloadSize)
        {
            CallCount = CallCount + 1;
            MediaData = mediaData.ToArray();
            MaximumPayloadSize = maximumPayloadSize;
            RtpPacketizationResult result = new RtpPacketizationResult();
            if (Fail)
            {
                result.ErrorMessage = "packetizer unavailable";
                return result;
            }

            result.Success = true;
            result.ConsumedCount = mediaData.Length;
            result.Payloads = Payloads;
            result.Markers = Markers;
            return result;
        }
    }

    private class RecordingDepacketizer : IRtpPayloadDepacketizer
    {
        public bool Fail;
        public int CallCount;
        public List<ushort[]> SequenceBatches = new List<ushort[]>();
        public List<byte[]> PayloadBatches = new List<byte[]>();

        public RtpDepacketizationResult DepacketizeRtpPayload(
            RtpPacket[] packets)
        {
            CallCount = CallCount + 1;
            ushort[] sequences = new ushort[packets.Length];
            int payloadLength = 0;
            int packetIndex = 0;
            while (packetIndex < packets.Length)
            {
                sequences[packetIndex] = packets[packetIndex].SequenceNumber;
                payloadLength = payloadLength +
                    packets[packetIndex].Payload.Length;
                packetIndex = packetIndex + 1;
            }

            byte[] payload = new byte[payloadLength];
            int payloadOffset = 0;
            packetIndex = 0;
            while (packetIndex < packets.Length)
            {
                packets[packetIndex].Payload.CopyTo(payload, payloadOffset);
                payloadOffset = payloadOffset +
                    packets[packetIndex].Payload.Length;
                packetIndex = packetIndex + 1;
            }

            SequenceBatches.Add(sequences);
            PayloadBatches.Add(payload);
            RtpDepacketizationResult result =
                new RtpDepacketizationResult();
            if (Fail)
            {
                result.ErrorMessage = "depacketizer unavailable";
                return result;
            }

            result.Success = true;
            result.ConsumedPacketCount = packets.Length;
            result.Payload = payload;
            result.Timestamp = 999U;
            return result;
        }
    }

    private class FixedClock : IRtpClock
    {
        public long MonotonicTimeTicks;

        public long GetMonotonicTimeTicks()
        {
            return MonotonicTimeTicks;
        }

        public long GetUtcTimeTicks()
        {
            return MonotonicTimeTicks;
        }

        public ulong GetNtpTimestamp()
        {
            return (ulong)MonotonicTimeTicks;
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
