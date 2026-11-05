using rtp;

namespace rtp.Tests.Session;

public class RtpInboundMediaProcessorTests
{
    [Test]
    public void MarkedReleasedGroupIsDepacketizedWithSessionTimingDecisions()
    {
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            true,
            2,
            new byte[] { 10, 11 });
        depacketizer.Result.Timestamp = 1U;
        depacketizer.Result.Marker = false;
        depacketizer.MutateInput = true;
        RtpInboundMediaProcessor processor =
            new RtpInboundMediaProcessor(depacketizer);
        RtpJitterBufferUpdate firstUpdate = CreateJitterUpdate(
            RtpJitterBufferStatus.Released,
            new RtpPacket[] { CreatePacket(100U, 10, 900U, false, 1) },
            []);

        RtpSessionEvent[] firstEvents = processor.ProcessReleasedRtpPackets(
            firstUpdate,
            CreateContext(1L),
            CreateSourceSnapshot(100U, false));
        RtpJitterBufferUpdate secondUpdate = CreateJitterUpdate(
            RtpJitterBufferStatus.Released,
            new RtpPacket[] { CreatePacket(100U, 11, 900U, true, 2) },
            []);
        RtpSessionEvent[] secondEvents = processor.ProcessReleasedRtpPackets(
            secondUpdate,
            CreateContext(2L),
            CreateSourceSnapshot(100U, false));
        byte inputAfterHook = firstUpdate.ReleasedPackets[0].Payload[0];
        depacketizer.Result.Payload[0] = 99;
        firstUpdate.ReleasedPackets[0].Payload[0] = 98;

        Assert.Multiple(() =>
        {
            Assert.That(firstEvents, Is.Empty);
            Assert.That(depacketizer.CallCount, Is.EqualTo(1));
            Assert.That(depacketizer.ReceivedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 10, 11 }));
            Assert.That(inputAfterHook, Is.EqualTo(1));
            Assert.That(firstUpdate.ReleasedPackets[0].Payload,
                Is.EqualTo(new byte[] { 98 }));
            Assert.That(secondEvents, Has.Length.EqualTo(1));
            Assert.That(secondEvents[0].Type,
                Is.EqualTo(RtpSessionEventType.Source));
            Assert.That(secondEvents[0].DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(secondEvents[0].DepacketizationResult?.Timestamp,
                Is.EqualTo(900U));
            Assert.That(secondEvents[0].DepacketizationResult?.Marker,
                Is.True);
            Assert.That(processor.mediaGroups[0].packets, Is.Empty);
        });
    }

    [Test]
    public void LossDiscardsIncompleteMediaBeforeProcessingReleasedPackets()
    {
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            true,
            1,
            new byte[] { 12 });
        RtpInboundMediaProcessor processor =
            new RtpInboundMediaProcessor(depacketizer);
        processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 10, 500U, false, 10)
                },
                []),
            CreateContext(1L),
            CreateSourceSnapshot(100U, false));

        RtpSessionEvent[] events = processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Lost,
                new RtpPacket[]
                {
                    CreatePacket(100U, 12, 500U, true, 12)
                },
                new ushort[] { 11 }),
            CreateContext(2L),
            CreateSourceSnapshot(100U, false));

        Assert.Multiple(() =>
        {
            Assert.That(events.Select(value => value.Type),
                Is.EqualTo(new RtpSessionEventType[]
                {
                    RtpSessionEventType.Loss,
                    RtpSessionEventType.Source
                }));
            Assert.That(events[0].JitterBufferUpdate?.LostSequenceNumbers,
                Is.EqualTo(new ushort[] { 11 }));
            Assert.That(events[0].Message, Does.Contain("lost"));
            Assert.That(depacketizer.ReceivedPackets, Has.Length.EqualTo(1));
            Assert.That(depacketizer.ReceivedPackets[0].SequenceNumber,
                Is.EqualTo(12));
            Assert.That(events[1].DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 12 }));
        });
    }

    [Test]
    public void TimestampDiscontinuityDropsThePriorGroupAndIsIndicated()
    {
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            true,
            1,
            new byte[] { 2 });
        RtpInboundMediaProcessor processor =
            new RtpInboundMediaProcessor(depacketizer);
        processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 1, 100U, false, 1)
                },
                []),
            CreateContext(1L),
            CreateSourceSnapshot(100U, false));

        RtpSessionEvent[] events = processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 2, 200U, true, 2)
                },
                []),
            CreateContext(2L),
            CreateSourceSnapshot(100U, true));

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Length.EqualTo(2));
            Assert.That(events[0].Type,
                Is.EqualTo(RtpSessionEventType.Source));
            Assert.That(events[0].Message, Does.Contain("discontinuity"));
            Assert.That(events[0].TimestampUpdate?.IsDiscontinuity, Is.True);
            Assert.That(depacketizer.ReceivedPackets, Has.Length.EqualTo(1));
            Assert.That(depacketizer.ReceivedPackets[0].SequenceNumber,
                Is.EqualTo(2));
            Assert.That(events[1].DepacketizationResult?.Timestamp,
                Is.EqualTo(200U));
        });
    }

    [Test]
    public void TimestampChangeBeforeMarkerProducesAnImplicitDiscontinuity()
    {
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            true,
            1,
            new byte[] { 2 });
        RtpInboundMediaProcessor processor =
            new RtpInboundMediaProcessor(depacketizer);
        processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 1, 100U, false, 1)
                },
                []),
            CreateContext(1L),
            CreateSourceSnapshot(100U, false));

        RtpSessionEvent[] events = processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 2, 200U, true, 2)
                },
                []),
            CreateContext(2L),
            CreateSourceSnapshot(100U, false));

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Length.EqualTo(2));
            Assert.That(events[0].Message, Does.Contain("timestamp changed"));
            Assert.That(depacketizer.ReceivedPackets, Has.Length.EqualTo(1));
            Assert.That(depacketizer.ReceivedPackets[0].SequenceNumber,
                Is.EqualTo(2));
            Assert.That(events[1].DepacketizationResult?.Timestamp,
                Is.EqualTo(200U));
        });
    }

    [Test]
    public void DeclaredDepacketizerFailureIsRecoverableAndIndependentlyOwned()
    {
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            false,
            0,
            new byte[] { 7 });
        depacketizer.Result.ErrorMessage = "need another packet";
        RtpInboundMediaProcessor processor =
            new RtpInboundMediaProcessor(depacketizer);

        RtpSessionEvent[] events = processor.ProcessReleasedRtpPackets(
            CreateJitterUpdate(
                RtpJitterBufferStatus.Released,
                new RtpPacket[]
                {
                    CreatePacket(100U, 1, 100U, true, 1)
                },
                []),
            CreateContext(1L),
            CreateSourceSnapshot(100U, false));
        depacketizer.Result.Payload[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(events.Single().Type,
                Is.EqualTo(RtpSessionEventType.RecoverableError));
            Assert.That(events[0].Message, Is.EqualTo("need another packet"));
            Assert.That(events[0].DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 7 }));
            Assert.That(processor.mediaGroups[0].packets, Is.Empty);
        });
    }

    [Test]
    public void IngressInvokesDepacketizerOnlyForReleasedCompleteMedia()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 4;
        RtpPayloadTypeRegistry registry = CreatePayloadRegistry();
        RtpSeparatePortIngress ingress = new RtpSeparatePortIngress(
            configuration,
            new FixedRandomSource(),
            registry);
        RecordingDepacketizer depacketizer = new RecordingDepacketizer();
        depacketizer.Result = CreateDepacketizationResult(
            true,
            2,
            new byte[] { 1, 2 });
        ingress.ConfigurePayloadDepacketizer(depacketizer);
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        RtpPacket first = CreatePacket(300U, 20, 1000U, false, 1);
        first.PayloadType = 96;
        RtpPacket second = CreatePacket(300U, 21, 1000U, true, 2);
        second.PayloadType = 96;

        RtpSessionUpdate firstUpdate = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(first),
            CreateContext(1L));
        RtpSessionUpdate secondUpdate = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(second),
            CreateContext(2L));
        RtpSessionUpdate duplicate = ingress.ProcessSeparateRtpDatagram(
            codec.EncodeRtpPacketToArray(second),
            CreateContext(3L));

        Assert.Multiple(() =>
        {
            Assert.That(firstUpdate.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(firstUpdate.Events, Has.Length.EqualTo(1));
            Assert.That(secondUpdate.ReleasedPackets, Has.Length.EqualTo(1));
            Assert.That(secondUpdate.Events, Has.Length.EqualTo(2));
            Assert.That(secondUpdate.Events[1].DepacketizationResult?.Payload,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(secondUpdate.Events[1].DepacketizationResult?.Timestamp,
                Is.EqualTo(1000U));
            Assert.That(duplicate.ReleasedPackets, Is.Empty);
            Assert.That(duplicate.Events, Has.Length.EqualTo(1));
            Assert.That(depacketizer.CallCount, Is.EqualTo(1));
            Assert.That(depacketizer.ReceivedPackets.Select(
                value => value.SequenceNumber),
                Is.EqualTo(new ushort[] { 20, 21 }));
        });
    }

    [Test]
    public void MediaProcessorRejectsEveryMalformedDependencyInputAndHookResult()
    {
        Assert.Throws<RtpConfigurationException>(() =>
            new RtpInboundMediaProcessor(null!));

        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            null!, 1), Is.Not.Null);
        RtpDepacketizationResult result = CreateDepacketizationResult(
            false,
            -1,
            []);
        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            result, 1), Is.Not.Null);
        result.ConsumedPacketCount = 2;
        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            result, 1), Is.Not.Null);
        result.ConsumedPacketCount = 1;
        result.Payload = null!;
        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            result, 1), Is.Not.Null);
        result.Payload = [];
        result.Success = true;
        result.ConsumedPacketCount = 0;
        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            result, 1), Is.Not.Null);
        result.Success = false;
        Assert.That(RtpInboundMediaProcessor.GetDepacketizationResultError(
            result, 1), Is.Null);

        Assert.That(RtpInboundMediaProcessor.CreateSafeDepacketizationResultCopy(
            null!), Is.Null);
        result.Payload = null!;
        Assert.That(RtpInboundMediaProcessor.CreateSafeDepacketizationResultCopy(
            result), Is.Null);
        result.Payload = new byte[] { 1 };
        Assert.That(RtpInboundMediaProcessor.CreateSafeDepacketizationResultCopy(
            result), Is.Not.SameAs(result));

        RtpJitterBufferUpdate update = CreateJitterUpdate(
            RtpJitterBufferStatus.Released,
            [],
            []);
        RtpPacketContext context = CreateContext(0L);
        RtpSourceSnapshot source = CreateSourceSnapshot(1U, false);
        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpInboundMediaProcessor.ValidateMediaInput(null!, context, source));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpInboundMediaProcessor.ValidateMediaInput(update, null!, source));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpInboundMediaProcessor.ValidateMediaInput(update, context, null!));
        });
        update.ReleasedPackets = null!;
        Assert.Throws<RtpConfigurationException>(() =>
            RtpInboundMediaProcessor.ValidateMediaInput(update, context, source));
        update.ReleasedPackets = [];
        update.LostSequenceNumbers = null!;
        Assert.Throws<RtpConfigurationException>(() =>
            RtpInboundMediaProcessor.ValidateMediaInput(update, context, source));
    }

    static RtpJitterBufferUpdate CreateJitterUpdate(
        RtpJitterBufferStatus status,
        RtpPacket[] releasedPackets,
        ushort[] lostSequenceNumbers)
    {
        RtpJitterBufferUpdate update = new RtpJitterBufferUpdate();
        update.Status = status;
        update.ReleasedPackets = releasedPackets;
        update.LostSequenceNumbers = lostSequenceNumbers;
        return update;
    }

    static RtpSourceSnapshot CreateSourceSnapshot(
        uint sourceIdentifier,
        bool isDiscontinuity)
    {
        RtpTimestampUpdate timestampUpdate = new RtpTimestampUpdate();
        timestampUpdate.Accepted = true;
        timestampUpdate.IsDiscontinuity = isDiscontinuity;
        RtpSourceSnapshot snapshot = new RtpSourceSnapshot();
        snapshot.SourceIdentifier = sourceIdentifier;
        snapshot.Status = RtpSourceStatus.ActiveSender;
        snapshot.TimestampUpdate = timestampUpdate;
        return snapshot;
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
        packet.Marker = marker;
        packet.Payload = new byte[] { payloadValue };
        return packet;
    }

    static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = "remote";
        context.ClockRate = 90000;
        context.MediaClockTimestamp = arrivalTimeTicks;
        return context;
    }

    static RtpDepacketizationResult CreateDepacketizationResult(
        bool success,
        int consumedPacketCount,
        byte[] payload)
    {
        RtpDepacketizationResult result = new RtpDepacketizationResult();
        result.Success = success;
        result.ConsumedPacketCount = consumedPacketCount;
        result.Payload = payload;
        return result;
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

    private class RecordingDepacketizer : IRtpPayloadDepacketizer
    {
        public RtpDepacketizationResult Result =
            new RtpDepacketizationResult();
        public RtpPacket[] ReceivedPackets = [];
        public int CallCount;
        public bool MutateInput;

        public RtpDepacketizationResult DepacketizeRtpPayload(
            RtpPacket[] packets)
        {
            CallCount = CallCount + 1;
            ReceivedPackets = new RtpPacket[packets.Length];
            int packetIndex = 0;
            while (packetIndex < packets.Length)
            {
                ReceivedPackets[packetIndex] = packets[packetIndex].Copy();
                packetIndex = packetIndex + 1;
            }

            if (MutateInput && packets.Length > 0)
            {
                packets[0].Payload[0] = 77;
            }

            return Result;
        }
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 1U;
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
