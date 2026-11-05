using rtp;

namespace rtp.Tests.Session;

public class RtpOutboundPacketBuilderTests
{
    [Test]
    public void BuilderAppliesRandomizedSourceStateAndAdvancesAcrossWrap()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(96, 48000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(0x10203040U, ushort.MaxValue, 0xFFFFFF00U));
        RtpPacket firstTemplate = CreatePacketTemplate(96, false, new byte[] { 1, 2 });
        RtpPacket secondTemplate = CreatePacketTemplate(96, true, new byte[] { 3 });
        RtpPacketContext firstContext = CreateContext(48000, 1000);
        RtpPacketContext secondContext = CreateContext(48000, 1960);

        RtpPacket first = builder.BuildOutboundPacket(state, firstTemplate, firstContext);
        RtpPacket second = builder.BuildOutboundPacket(state, secondTemplate, secondContext);

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(RtpProtocolConstants.Version));
            Assert.That(first.SynchronizationSource, Is.EqualTo(0x10203040U));
            Assert.That(second.SynchronizationSource, Is.EqualTo(0x10203040U));
            Assert.That(first.SequenceNumber, Is.EqualTo(ushort.MaxValue));
            Assert.That(second.SequenceNumber, Is.EqualTo(0));
            Assert.That(first.Timestamp, Is.EqualTo(0xFFFFFF00U));
            Assert.That(second.Timestamp, Is.EqualTo(0x000002C0U));
            Assert.That(first.Marker, Is.False);
            Assert.That(second.Marker, Is.True);
        });
    }

    [Test]
    public void PayloadFragmentsCanShareTimestampWhileSequenceAdvances()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(96, 90000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(7, 20, 500));
        RtpPacketContext context = CreateContext(90000, 3000);

        RtpPacket first = builder.BuildOutboundPacket(
            state,
            CreatePacketTemplate(96, false, new byte[] { 1 }),
            context);
        RtpPacket second = builder.BuildOutboundPacket(
            state,
            CreatePacketTemplate(96, true, new byte[] { 2 }),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(first.SequenceNumber, Is.EqualTo(20));
            Assert.That(second.SequenceNumber, Is.EqualTo(21));
            Assert.That(first.Timestamp, Is.EqualTo(500));
            Assert.That(second.Timestamp, Is.EqualTo(500));
        });
    }

    [Test]
    public void BuilderSelectsTheNegotiatedHeaderExtensionFormat()
    {
        RtpPayloadTypeRegistry payloadRegistry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        payloadRegistry.SetPayloadTypeDefinition(CreatePayloadDefinition(96, 90000));
        RtpHeaderExtensionRegistry extensionRegistry = new RtpHeaderExtensionRegistry();
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = 2;
        mapping.Uri = RtpHeaderExtensionUris.MediaIdentifier;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.TwoByte;
        extensionRegistry.SetHeaderExtensionMapping(mapping);
        RtpOutboundPacketBuilder builder = new RtpOutboundPacketBuilder(
            payloadRegistry,
            extensionRegistry);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(7, 8, 9));
        RtpPacket template = CreatePacketTemplate(96, false, new byte[] { 1 });
        template.HasExtension = true;
        template.HeaderExtension = CreateAutomaticExtension(2, new byte[] { 65 });

        RtpPacket prepared = builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(90000, 0));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.HasExtension, Is.True);
            Assert.That(prepared.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(prepared.HeaderExtension?.Profile,
                Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte));
            Assert.That(prepared.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(2));
            Assert.That(prepared.HeaderExtension?.Elements[0].Data,
                Is.EqualTo(new byte[] { 65 }));
        });
    }

    [Test]
    public void FailedMappingDoesNotAdvanceLocalSourceState()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(96, 90000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(100, 200, 300));
        RtpPacket invalidTemplate = CreatePacketTemplate(96, false, new byte[] { 1 });
        invalidTemplate.HasExtension = true;
        invalidTemplate.HeaderExtension = CreateAutomaticExtension(
            9,
            new byte[] { 7 });

        Assert.Throws<RtpSessionException>(() => builder.BuildOutboundPacket(
            state,
            invalidTemplate,
            CreateContext(90000, 0)));
        RtpPacket prepared = builder.BuildOutboundPacket(
            state,
            CreatePacketTemplate(96, false, new byte[] { 2 }),
            CreateContext(90000, 0));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.SequenceNumber, Is.EqualTo(200));
            Assert.That(prepared.Timestamp, Is.EqualTo(300));
        });
    }

    [Test]
    public void FailedClockValidationDoesNotAdvanceLocalSourceState()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(96, 48000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(100, 200, 300));
        RtpPacket template = CreatePacketTemplate(96, false, new byte[] { 1 });

        Assert.Throws<RtpSessionException>(() => builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(8000, 0)));
        RtpPacket prepared = builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(48000, 0));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.SequenceNumber, Is.EqualTo(200));
            Assert.That(prepared.Timestamp, Is.EqualTo(300));
        });
    }

    [Test]
    public void BuilderReturnsAnIndependentPacketAndRejectsBackwardMediaTime()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(96, 48000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(1, 2, 3));
        RtpPacket template = CreatePacketTemplate(96, true, new byte[] { 10, 11 });
        template.ContributingSources = new uint[] { 12 };
        RtpPacket first = builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(48000, 100));
        template.Payload[0] = 99;
        template.ContributingSources[0] = 99;

        Assert.Throws<RtpSessionException>(() => builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(48000, 99)));
        RtpPacket second = builder.BuildOutboundPacket(
            state,
            template,
            CreateContext(48000, 101));

        Assert.Multiple(() =>
        {
            Assert.That(first.Payload, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(first.ContributingSources, Is.EqualTo(new uint[] { 12 }));
            Assert.That(second.SequenceNumber, Is.EqualTo(3));
            Assert.That(second.Timestamp, Is.EqualTo(4));
        });
    }

    [Test]
    public void SeparateTransportPreservesMarkerForPayloadTypesThatOnlyConflictInMux()
    {
        RtpOutboundPacketBuilder builder = CreateBuilder(72, 90000);
        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(1, 2, 3));

        RtpPacket prepared = builder.BuildOutboundPacket(
            state,
            CreatePacketTemplate(72, true, new byte[] { 1 }),
            CreateContext(90000, 0));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.PayloadType, Is.EqualTo(72));
            Assert.That(prepared.Marker, Is.True);
        });
    }

    [Test]
    public void BuilderAndInputValidationRejectEveryMalformedDependencyAndTemplate()
    {
        RtpPayloadTypeRegistry payloadRegistry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpHeaderExtensionRegistry extensionRegistry =
            new RtpHeaderExtensionRegistry();
        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpOutboundPacketBuilder(null!, extensionRegistry));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpOutboundPacketBuilder(payloadRegistry, null!));
        });

        RtpLocalSourceState state = new RtpLocalSourceState(
            new FixedRandomSource(1U, 2, 3U));
        RtpPacket packet = CreatePacketTemplate(96, false, new byte[] { 1 });
        RtpPacketContext context = CreateContext(90000, 0L);

        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            null!, packet, context), Is.Not.Null);
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, null!, context), Is.Not.Null);
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, null!), Is.Not.Null);

        context.ClockRate = 0;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        context.ClockRate = 90000;
        context.ArrivalTimeTicks = -1L;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        context.ArrivalTimeTicks = 0L;

        packet.PayloadType = 128;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.PayloadType = 96;
        packet.ContributingSources = null!;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.ContributingSources = new uint[16];
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.ContributingSources = [];
        packet.Payload = null!;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.Payload = [];
        packet.Padding = null!;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.Padding = [];

        packet.HasExtension = true;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.HasExtension = false;
        packet.HeaderExtension = new RtpHeaderExtension();
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Not.Null);
        packet.HeaderExtension = null;
        Assert.That(RtpOutboundPacketBuilder.GetInputValidationError(
            state, packet, context), Is.Null);
    }

    [Test]
    public void HeaderExtensionPreparationRejectsMalformedRawAndMappingSnapshots()
    {
        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(null!, []));

        RtpHeaderExtension raw = new RtpHeaderExtension();
        raw.Profile = 0x1000;
        raw.Data = null!;
        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(raw, []));
        raw.Data = [];
        raw.Elements = null!;
        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(raw, []));
        raw.Elements = new RtpHeaderExtensionElement[] { null! };
        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(raw, []));
        raw.Elements = new RtpHeaderExtensionElement[]
        {
            new RtpHeaderExtensionElement()
        };
        raw.Elements[0].Data = null!;
        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.PrepareMappedExtension(raw, []));
        raw.Elements[0].Data = new byte[] { 1 };
        Assert.That(
            RtpOutboundPacketBuilder.PrepareMappedExtension(raw, []).Data,
            Is.Empty);

        Assert.Throws<RtpSessionException>(() =>
            RtpOutboundPacketBuilder.FindMapping(null!, 1));
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = 2;
        Assert.That(RtpOutboundPacketBuilder.FindMapping(
            new RtpHeaderExtensionMapping[] { null!, mapping }, 2),
            Is.SameAs(mapping));
        Assert.That(RtpOutboundPacketBuilder.FindMapping(
            new RtpHeaderExtensionMapping[] { mapping }, 3),
            Is.Null);
    }

    static RtpOutboundPacketBuilder CreateBuilder(byte payloadType, int clockRate)
    {
        RtpPayloadTypeRegistry payloadRegistry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        payloadRegistry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(payloadType, clockRate));
        return new RtpOutboundPacketBuilder(
            payloadRegistry,
            new RtpHeaderExtensionRegistry());
    }

    static RtpPayloadTypeDefinition CreatePayloadDefinition(
        byte payloadType,
        int clockRate)
    {
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = payloadType;
        definition.ClockRate = clockRate;
        definition.MediaIdentity = "test";
        definition.ChannelCount = 1;
        return definition;
    }

    static RtpPacket CreatePacketTemplate(
        byte payloadType,
        bool marker,
        byte[] payload)
    {
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = payloadType;
        packet.Marker = marker;
        packet.Payload = payload;
        return packet;
    }

    static RtpPacketContext CreateContext(int clockRate, long mediaClockTimestamp)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ClockRate = clockRate;
        context.MediaClockTimestamp = mediaClockTimestamp;
        return context;
    }

    static RtpHeaderExtension CreateAutomaticExtension(int identifier, byte[] data)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = new RtpHeaderExtensionElement[] { element };
        return extension;
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
}
