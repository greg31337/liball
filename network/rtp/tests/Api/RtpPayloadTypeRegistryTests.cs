using rtp;

namespace rtp.Tests.Api;

public class RtpPayloadTypeRegistryTests
{
    [Test]
    public void RegistryStoresAndReturnsIndependentDefinitionCopies()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition = CreateMediaDefinition(
            96,
            90000,
            "H264",
            1);

        registry.SetPayloadTypeDefinition(definition);
        definition.ClockRate = 8000;
        definition.MediaIdentity = "changed";
        RtpPayloadTypeDefinition? firstCopy = registry.FindPayloadTypeDefinition(96);
        if (firstCopy != null)
        {
            firstCopy.ChannelCount = 4;
            firstCopy.MediaIdentity = "mutated-copy";
        }

        RtpPayloadTypeDefinition? freshCopy = registry.FindPayloadTypeDefinition(96);

        Assert.Multiple(() =>
        {
            Assert.That(freshCopy?.PayloadType, Is.EqualTo(96));
            Assert.That(freshCopy?.ClockRate, Is.EqualTo(90000));
            Assert.That(freshCopy?.MediaIdentity, Is.EqualTo("H264"));
            Assert.That(freshCopy?.ChannelCount, Is.EqualTo(1));
            Assert.That(freshCopy, Is.Not.SameAs(firstCopy));
        });
    }

    [Test]
    public void RegistryReturnsPayloadOrderedIndependentSnapshots()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(120, 90000, "VP9", 1));
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(8, 8000, "PCMA", 1));
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(96, 48000, "opus", 2));

        RtpPayloadTypeDefinition[] snapshot = registry.GetPayloadTypeDefinitions();
        snapshot[0].PayloadType = 40;
        snapshot[1].MediaIdentity = "changed";
        RtpPayloadTypeDefinition[] freshSnapshot = registry.GetPayloadTypeDefinitions();

        Assert.Multiple(() =>
        {
            Assert.That(freshSnapshot.Length, Is.EqualTo(3));
            Assert.That(freshSnapshot[0].PayloadType, Is.EqualTo(8));
            Assert.That(freshSnapshot[1].PayloadType, Is.EqualTo(96));
            Assert.That(freshSnapshot[2].PayloadType, Is.EqualTo(120));
            Assert.That(freshSnapshot[1].MediaIdentity, Is.EqualTo("opus"));
            Assert.That(freshSnapshot, Is.Not.SameAs(snapshot));
            Assert.That(freshSnapshot[0], Is.Not.SameAs(snapshot[0]));
        });
    }

    [Test]
    public void SignalingUpdateReplacesDefinitionByPayloadType()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(96, 90000, "H264", 1));
        RtpPayloadTypeDefinition replacement =
            CreateMediaDefinition(96, 90000, "VP8", 1);

        registry.SetPayloadTypeDefinition(replacement);

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetPayloadTypeDefinitions().Length, Is.EqualTo(1));
            Assert.That(registry.FindPayloadTypeDefinition(96)?.MediaIdentity,
                Is.EqualTo("VP8"));
        });
    }

    [Test]
    public void MultiplexedRegistryRejectsRtcpConflictingAssignmentsAtomically()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Multiplexed);
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(63, 8000, "safe-low", 1));
        registry.SetPayloadTypeDefinition(CreateMediaDefinition(96, 90000, "safe-high", 1));

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(
                    CreateMediaDefinition(64, 8000, "conflict-low", 1)));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(
                    CreateMediaDefinition(95, 8000, "conflict-high", 1)));
            Assert.That(registry.GetPayloadTypeDefinitions().Length, Is.EqualTo(2));
            Assert.That(registry.FindPayloadTypeDefinition(63), Is.Not.Null);
            Assert.That(registry.FindPayloadTypeDefinition(96), Is.Not.Null);
        });
    }

    [Test]
    public void RtxAssociationsRequireRegisteredMediaAndMatchingClockRates()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition media =
            CreateMediaDefinition(96, 90000, "H264", 1);
        RtpPayloadTypeDefinition retransmission = CreateRtxDefinition(97, 96, 90000);
        registry.SetPayloadTypeDefinition(media);
        registry.SetPayloadTypeDefinition(retransmission);

        Assert.Multiple(() =>
        {
            Assert.That(registry.FindPayloadTypeDefinition(97)?.IsRetransmission, Is.True);
            Assert.That(registry.FindPayloadTypeDefinition(97)?.AssociatedPayloadType,
                Is.EqualTo(96));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(CreateRtxDefinition(98, 99, 90000)));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(CreateRtxDefinition(98, 96, 8000)));
            Assert.That(registry.FindPayloadTypeDefinition(98), Is.Null);
        });
    }

    [Test]
    public void InvalidUpdatesAndReferencedMediaRemovalPreserveRegistryState()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(
            CreateMediaDefinition(96, 90000, "H264", 1));
        registry.SetPayloadTypeDefinition(CreateRtxDefinition(97, 96, 90000));
        RtpPayloadTypeDefinition incompatibleReplacement =
            CreateMediaDefinition(96, 8000, "PCMU", 1);
        RtpPayloadTypeDefinition invalid =
            CreateMediaDefinition(98, 0, "invalid", 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(incompatibleReplacement));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(invalid));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.RemovePayloadTypeDefinition(96));
            Assert.That(registry.FindPayloadTypeDefinition(96)?.ClockRate,
                Is.EqualTo(90000));
            Assert.That(registry.FindPayloadTypeDefinition(97), Is.Not.Null);
        });
    }

    [Test]
    public void RegistryReportsRemovalAndValidatesConstructorAndLookupInputs()
    {
        IRtpPayloadTypeRegistry registry =
            new RtpPayloadTypeRegistry(RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(
            CreateMediaDefinition(96, 90000, "H264", 1));

        bool removed = registry.RemovePayloadTypeDefinition(96);
        bool absent = registry.RemovePayloadTypeDefinition(96);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(absent, Is.False);
            Assert.That(registry.GetPayloadTypeDefinitions(), Is.Empty);
            Assert.That(registry.FindPayloadTypeDefinition(10), Is.Null);
            Assert.Throws<RtpConfigurationException>(() =>
                registry.FindPayloadTypeDefinition(128));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.RemovePayloadTypeDefinition(255));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpPayloadTypeRegistry((RtpSessionTransportMode)99));
            Assert.Throws<RtpConfigurationException>(() =>
                registry.SetPayloadTypeDefinition(null!));
        });
    }

    static RtpPayloadTypeDefinition CreateMediaDefinition(
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

    static RtpPayloadTypeDefinition CreateRtxDefinition(
        byte payloadType,
        byte associatedPayloadType,
        int clockRate)
    {
        RtpPayloadTypeDefinition definition = CreateMediaDefinition(
            payloadType,
            clockRate,
            "rtx",
            1);
        definition.IsRetransmission = true;
        definition.HasAssociatedPayloadType = true;
        definition.AssociatedPayloadType = associatedPayloadType;
        return definition;
    }
}
