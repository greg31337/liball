using rtp;

namespace rtp.Tests.Validation;

public class ConfigurationValidationTests
{
    [Test]
    public void ValidSessionConfigurationIsReturnedAsAnIsolatedCopy()
    {
        RtpSessionConfiguration configuration = CreateValidSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Multiplexed;
        configuration.RtcpCompoundMode = RtcpCompoundMode.MinimalFeedback;
        configuration.RtcpScheduler.FeedbackEnabled = true;
        configuration.RtcpScheduler.MinimumFeedbackIntervalTicks = 100L;
        configuration.JitterBuffer.MaximumLatencyTicks = 1000L;
        configuration.JitterBuffer.NackDelayTicks = 100L;
        configuration.JitterBuffer.NackRetryIntervalTicks = 200L;
        configuration.JitterBuffer.MaximumNackRetryCount = 3;
        configuration.JitterBuffer.MaximumNackRetryPeriodTicks = 900L;
        RtcpSourceDescriptionItem optionalName = new RtcpSourceDescriptionItem();
        optionalName.Type = RtcpSdesItemType.Name;
        optionalName.Value = new byte[] { 1, 2, 3 };
        configuration.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[] { optionalName };

        RtpSessionConfiguration copy =
            RtpApiCopy.CopyValidatedSessionConfiguration(configuration);
        configuration.CanonicalName = "changed";
        optionalName.Value[0] = 9;
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 1.0;
        configuration.JitterBuffer.Capacity = 1;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(configuration));
            Assert.That(copy.CanonicalName, Is.EqualTo("local@example"));
            Assert.That(copy.OptionalSourceDescriptionItems,
                Is.Not.SameAs(configuration.OptionalSourceDescriptionItems));
            Assert.That(copy.OptionalSourceDescriptionItems[0],
                Is.Not.SameAs(optionalName));
            Assert.That(copy.OptionalSourceDescriptionItems[0].Value,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(copy.TransportMode, Is.EqualTo(RtpSessionTransportMode.Multiplexed));
            Assert.That(copy.RtcpCompoundMode,
                Is.EqualTo(RtcpCompoundMode.MinimalFeedback));
            Assert.That(copy.RtcpScheduler, Is.Not.SameAs(configuration.RtcpScheduler));
            Assert.That(copy.RtcpScheduler.SessionBandwidthBitsPerSecond,
                Is.EqualTo(1000000.0));
            Assert.That(copy.RtcpScheduler.MinimumFeedbackIntervalTicks, Is.EqualTo(100L));
            Assert.That(copy.JitterBuffer, Is.Not.SameAs(configuration.JitterBuffer));
            Assert.That(copy.JitterBuffer.Capacity, Is.EqualTo(256));
            Assert.That(copy.JitterBuffer.MaximumNackRetryCount, Is.EqualTo(3));
            Assert.That(copy.JitterBuffer.MaximumNackRetryPeriodTicks, Is.EqualTo(900L));
        });
    }

    [Test]
    public void SessionValidationRejectsInvalidRootAndNestedCombinations()
    {
        RtpSessionConfiguration smallPacket = CreateValidSessionConfiguration();
        smallPacket.MaximumRtpPacketSize = RtpProtocolConstants.FixedHeaderSize - 1;
        RtpSessionConfiguration emptyCanonicalName = CreateValidSessionConfiguration();
        emptyCanonicalName.CanonicalName = "";
        RtpSessionConfiguration invalidTransport = CreateValidSessionConfiguration();
        invalidTransport.TransportMode = (RtpSessionTransportMode)99;
        RtpSessionConfiguration invalidCompoundMode = CreateValidSessionConfiguration();
        invalidCompoundMode.RtcpCompoundMode = (RtcpCompoundMode)99;
        RtpSessionConfiguration noBandwidth = CreateValidSessionConfiguration();
        noBandwidth.RtcpScheduler.SessionBandwidthBitsPerSecond = 0.0;
        RtpSessionConfiguration invalidSplit = CreateValidSessionConfiguration();
        invalidSplit.RtcpScheduler.SenderBandwidthFraction = 0.5;
        RtpSessionConfiguration invalidIntervals = CreateValidSessionConfiguration();
        invalidIntervals.RtcpScheduler.InitialMinimumIntervalSeconds = 6.0;
        RtpSessionConfiguration noCapacity = CreateValidSessionConfiguration();
        noCapacity.JitterBuffer.Capacity = 0;
        RtpSessionConfiguration noRestartRange = CreateValidSessionConfiguration();
        noRestartRange.JitterBuffer.MaximumDropout = 65000;
        noRestartRange.JitterBuffer.MaximumMisorder = 536;
        RtpSessionConfiguration incompleteNack = CreateValidSessionConfiguration();
        incompleteNack.JitterBuffer.MaximumNackRetryCount = 1;
        incompleteNack.JitterBuffer.MaximumNackRetryPeriodTicks = 100L;
        RtpSessionConfiguration optionalCanonicalName =
            CreateValidSessionConfiguration();
        RtcpSourceDescriptionItem canonicalNameItem =
            new RtcpSourceDescriptionItem();
        canonicalNameItem.Type = RtcpSdesItemType.CanonicalName;
        optionalCanonicalName.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[] { canonicalNameItem };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(smallPacket));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(emptyCanonicalName));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(invalidTransport));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(invalidCompoundMode));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(noBandwidth));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(invalidSplit));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(invalidIntervals));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(noCapacity));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(noRestartRange));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(incompleteNack));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateSessionConfiguration(
                    optionalCanonicalName));
        });
    }

    [Test]
    public void PayloadDefinitionsValidateAssociationsMuxSafetyAndCopyIsolation()
    {
        RtpPayloadTypeDefinition media = CreateMediaPayloadType(96, "video", 90000);
        RtpPayloadTypeDefinition retransmission = CreateMediaPayloadType(97, "rtx", 90000);
        retransmission.IsRetransmission = true;
        retransmission.HasAssociatedPayloadType = true;
        retransmission.AssociatedPayloadType = 96;
        RtpPayloadTypeDefinition[] definitions =
            new RtpPayloadTypeDefinition[] { media, retransmission };

        RtpPayloadTypeDefinition[] copies =
            RtpApiCopy.CopyValidatedPayloadTypeDefinitions(
                definitions,
                RtpSessionTransportMode.Multiplexed);
        RtpPayloadTypeDefinition singleCopy =
            RtpApiCopy.CopyValidatedPayloadTypeDefinition(media);
        media.MediaIdentity = "changed";
        retransmission.AssociatedPayloadType = 98;

        RtpPayloadTypeDefinition duplicate = CreateMediaPayloadType(96, "duplicate", 8000);
        RtpPayloadTypeDefinition missingAssociation =
            CreateMediaPayloadType(98, "rtx", 90000);
        missingAssociation.IsRetransmission = true;
        missingAssociation.HasAssociatedPayloadType = true;
        missingAssociation.AssociatedPayloadType = 99;
        RtpPayloadTypeDefinition forbidden = CreateMediaPayloadType(80, "audio", 8000);

        Assert.Multiple(() =>
        {
            Assert.That(copies, Is.Not.SameAs(definitions));
            Assert.That(copies[0], Is.Not.SameAs(media));
            Assert.That(copies[0].MediaIdentity, Is.EqualTo("video"));
            Assert.That(copies[1].AssociatedPayloadType, Is.EqualTo(96));
            Assert.That(singleCopy, Is.Not.SameAs(media));
            Assert.That(singleCopy.MediaIdentity, Is.EqualTo("video"));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    new RtpPayloadTypeDefinition[] { media, duplicate },
                    RtpSessionTransportMode.Separate));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    new RtpPayloadTypeDefinition[] { media, missingAssociation },
                    RtpSessionTransportMode.Separate));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    new RtpPayloadTypeDefinition[] { forbidden },
                    RtpSessionTransportMode.Multiplexed));
        });
    }

    [Test]
    public void HeaderExtensionMappingsValidateFormatsUniquenessAndCopyIsolation()
    {
        RtpHeaderExtensionMapping audioLevel = CreateMapping(
            1,
            RtpHeaderExtensionUris.SsrcAudioLevel);
        RtpHeaderExtensionMapping streamIdentifier = CreateMapping(
            2,
            RtpHeaderExtensionUris.MediaIdentifier);
        RtpHeaderExtensionMapping[] mappings =
            new RtpHeaderExtensionMapping[] { audioLevel, streamIdentifier };

        RtpHeaderExtensionMapping[] copies =
            RtpApiCopy.CopyValidatedHeaderExtensionMappings(mappings);
        RtpHeaderExtensionMapping singleCopy =
            RtpApiCopy.CopyValidatedHeaderExtensionMapping(audioLevel);
        audioLevel.Uri = "changed";
        streamIdentifier.Attributes = "changed";

        RtpHeaderExtensionMapping invalidOneByte = CreateMapping(15, "urn:test:wide");
        RtpHeaderExtensionMapping duplicateIdentifier = CreateMapping(1, "urn:test:other");
        RtpHeaderExtensionMapping duplicateUri = CreateMapping(
            3,
            "changed");
        RtpHeaderExtensionMapping unsupportedPreference = CreateMapping(4, "urn:test:two");
        unsupportedPreference.SupportsOneByte = false;

        Assert.Multiple(() =>
        {
            Assert.That(copies, Is.Not.SameAs(mappings));
            Assert.That(copies[0], Is.Not.SameAs(audioLevel));
            Assert.That(copies[0].Uri, Is.EqualTo(RtpHeaderExtensionUris.SsrcAudioLevel));
            Assert.That(copies[1].Attributes, Is.EqualTo(""));
            Assert.That(singleCopy, Is.Not.SameAs(audioLevel));
            Assert.That(singleCopy.Uri, Is.EqualTo(RtpHeaderExtensionUris.SsrcAudioLevel));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMapping(invalidOneByte));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMappings(
                    new RtpHeaderExtensionMapping[] { audioLevel, duplicateIdentifier }));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMappings(
                    new RtpHeaderExtensionMapping[] { audioLevel, duplicateUri }));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMapping(unsupportedPreference));
        });
    }

    [Test]
    public void ComponentConfigurationsRejectEveryReachableInvalidBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtpPacketCodecConfiguration(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtpSequenceTrackerConfiguration(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtpPacketHistoryConfiguration(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtcpPacketCodecConfiguration(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtcpSchedulerConfiguration(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtpJitterBufferConfiguration(null!));
        });

        Action<RtpSessionConfiguration>[] sequenceMutations =
        [
            value => value.MinimumSequential = 65536,
            value => value.JitterBuffer = null!,
            value => value.JitterBuffer.MaximumDropout = 0,
            value => value.JitterBuffer.MaximumDropout = 65536,
            value => value.JitterBuffer.MaximumMisorder = -1,
            value => value.JitterBuffer.MaximumMisorder = 65536,
            value =>
            {
                value.JitterBuffer.MaximumDropout = 65000;
                value.JitterBuffer.MaximumMisorder = 536;
            }
        ];
        AssertInvalidConfigurations(
            sequenceMutations,
            RtpConfigurationValidator.ValidateRtpSequenceTrackerConfiguration);

        Action<RtpSessionConfiguration>[] historyMutations =
        [
            value => value.MaximumRtpPacketSize = 65536,
            value => value.MaximumPacketHistoryCount = -1,
            value => value.MaximumPacketHistoryBytes = -1,
            value => value.MaximumPacketHistoryAgeTicks = -1,
            value =>
            {
                value.MaximumPacketHistoryCount = 1;
                value.MaximumPacketHistoryBytes = 0;
            }
        ];
        AssertInvalidConfigurations(
            historyMutations,
            RtpConfigurationValidator.ValidateRtpPacketHistoryConfiguration);

        Action<RtpSessionConfiguration>[] sourceMutations =
        [
            value => value.MaximumSourceCount = 0,
            value => value.RtcpScheduler = null!,
            value => value.RtcpScheduler.SenderTimeoutTicks = -1,
            value => value.RtcpScheduler.MemberTimeoutTicks = -1,
            value =>
            {
                value.RtcpScheduler.SenderTimeoutTicks = 2;
                value.RtcpScheduler.MemberTimeoutTicks = 1;
            }
        ];
        AssertInvalidConfigurations(
            sourceMutations,
            RtpConfigurationValidator.ValidateRtpSourceTableConfiguration);

        Action<RtpSessionConfiguration>[] rtcpMutations =
        [
            value => value.MaximumRtcpCompoundPacketSize =
                RtcpProtocolConstants.MaximumPacketSize + 1,
            value => value.MaximumRtcpCompoundPacketCount = 0,
            value => value.MaximumRtcpCompoundPacketCount =
                RtcpProtocolConstants.MaximumCompoundPacketCount + 1,
            value => value.RtcpCompoundMode = (RtcpCompoundMode)99
        ];
        AssertInvalidConfigurations(
            rtcpMutations,
            RtpConfigurationValidator.ValidateRtcpPacketCodecConfiguration);
    }

    [Test]
    public void SessionConfigurationRejectsEveryReachableRootFailure()
    {
        Action<RtpSessionConfiguration>[] mutations =
        [
            value => value.MaximumRtpPacketSize = 65536,
            value => value.MaximumRtcpCompoundPacketSize = 3,
            value => value.MaximumRtcpCompoundPacketSize =
                RtcpProtocolConstants.MaximumPacketSize + 1,
            value => value.MaximumRtcpCompoundPacketCount = 0,
            value => value.MaximumRtcpCompoundPacketCount =
                RtcpProtocolConstants.MaximumCompoundPacketCount + 1,
            value => value.MaximumSourceCount = 0,
            value => value.MaximumPacketHistoryCount = -1,
            value => value.MaximumPacketHistoryBytes = -1,
            value => value.MaximumPacketHistoryAgeTicks = -1,
            value =>
            {
                value.MaximumPacketHistoryCount = 1;
                value.MaximumPacketHistoryBytes = 0;
            },
            value => value.MinimumSequential = 65536,
            value => value.CanonicalName = null!,
            value => value.CanonicalName = new string('x', 256),
            value => value.OptionalSourceDescriptionItems = null!,
            value => value.OptionalSourceDescriptionItems =
                new RtcpSourceDescriptionItem[] { null! },
            value => value.RtcpScheduler = null!,
            value => value.JitterBuffer = null!
        ];

        Assert.Throws<RtpConfigurationException>(() =>
            RtpConfigurationValidator.ValidateSessionConfiguration(null!));
        AssertInvalidConfigurations(
            mutations,
            RtpConfigurationValidator.ValidateSessionConfiguration);
    }

    [Test]
    public void SchedulerConfigurationRejectsEveryReachableInvalidField()
    {
        Action<RtcpSchedulerConfiguration>[] mutations =
        [
            value => value.SessionBandwidthBitsPerSecond = double.NaN,
            value => value.SessionBandwidthBitsPerSecond = double.PositiveInfinity,
            value => value.RtcpBandwidthFraction = 0.0,
            value => value.RtcpBandwidthFraction = 1.1,
            value => value.SenderBandwidthFraction = double.NaN,
            value => value.ReceiverBandwidthFraction = double.NegativeInfinity,
            value => value.MinimumIntervalSeconds = 0.0,
            value => value.InitialMinimumIntervalSeconds = double.NaN,
            value => value.InitialAveragePacketSize = 0,
            value => value.MinimumFeedbackIntervalTicks = -1,
            value => value.FeedbackRetentionTicks =
                RtcpProtocolConstants.MinimumFeedbackRetentionTicks - 1,
            value => value.SenderTimeoutTicks = -1,
            value => value.MemberTimeoutTicks = -1,
            value =>
            {
                value.SenderTimeoutTicks = 2;
                value.MemberTimeoutTicks = 1;
            }
        ];

        int index = 0;
        while (index < mutations.Length)
        {
            RtcpSchedulerConfiguration configuration =
                CreateValidSessionConfiguration().RtcpScheduler;
            mutations[index](configuration);
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtcpSchedulerConfiguration(
                    configuration),
                $"mutation {index}");
            index = index + 1;
        }
    }

    [Test]
    public void JitterBufferConfigurationRejectsEveryReachableInvalidField()
    {
        Action<RtpJitterBufferConfiguration>[] mutations =
        [
            value => value.MaximumLatencyTicks = -1,
            value => value.MaximumDropout = 65536,
            value => value.MaximumMisorder = 65536,
            value => value.NackDelayTicks = -1,
            value => value.NackRetryIntervalTicks = -1,
            value => value.MaximumNackRetryCount = -1,
            value => value.MaximumNackRetryPeriodTicks = -1,
            value =>
            {
                value.MaximumNackRetryCount = 0;
                value.NackDelayTicks = 1;
            },
            value =>
            {
                value.MaximumNackRetryCount = 1;
                value.NackRetryIntervalTicks = 0;
            },
            value =>
            {
                value.MaximumNackRetryCount = 1;
                value.NackRetryIntervalTicks = 2;
                value.MaximumNackRetryPeriodTicks = 1;
            },
            value =>
            {
                value.MaximumNackRetryCount = 1;
                value.NackDelayTicks = 3;
                value.NackRetryIntervalTicks = 1;
                value.MaximumNackRetryPeriodTicks = 2;
            },
            value =>
            {
                value.MaximumLatencyTicks = 1;
                value.MaximumNackRetryCount = 1;
                value.NackDelayTicks = 2;
                value.NackRetryIntervalTicks = 1;
                value.MaximumNackRetryPeriodTicks = 2;
            }
        ];

        int index = 0;
        while (index < mutations.Length)
        {
            RtpJitterBufferConfiguration configuration =
                new RtpJitterBufferConfiguration();
            mutations[index](configuration);
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateRtpJitterBufferConfiguration(
                    configuration),
                $"mutation {index}");
            index = index + 1;
        }
    }

    [Test]
    public void PayloadAndHeaderMappingValidationRejectsEveryReachableShape()
    {
        RtpPayloadTypeDefinition valid = CreateMediaPayloadType(96, "video", 90000);
        Action<RtpPayloadTypeDefinition>[] payloadMutations =
        [
            value => value.ClockRate = 0,
            value => value.MediaIdentity = null!,
            value => value.ChannelCount = 0,
            value => value.IsRetransmission = true,
            value => value.HasAssociatedPayloadType = true,
            value =>
            {
                value.IsRetransmission = true;
                value.HasAssociatedPayloadType = true;
                value.AssociatedPayloadType = value.PayloadType;
            }
        ];
        int index = 0;
        while (index < payloadMutations.Length)
        {
            RtpPayloadTypeDefinition definition =
                CreateMediaPayloadType(96, "video", 90000);
            payloadMutations[index](definition);
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinition(definition),
                $"payload mutation {index}");
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinition(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    null!,
                    RtpSessionTransportMode.Separate));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    new RtpPayloadTypeDefinition[] { valid },
                    (RtpSessionTransportMode)99));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                    new RtpPayloadTypeDefinition[] { null! },
                    RtpSessionTransportMode.Separate));
        });

        Action<RtpHeaderExtensionMapping>[] mappingMutations =
        [
            value => value.Identifier = 0,
            value => value.Identifier = 256,
            value => value.Uri = null!,
            value => value.Attributes = null!,
            value =>
            {
                value.SupportsOneByte = false;
                value.SupportsTwoByte = false;
            },
            value => value.PreferredFormat = RtpHeaderExtensionFormat.Raw,
            value =>
            {
                value.SupportsOneByte = false;
                value.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
            },
            value =>
            {
                value.SupportsTwoByte = false;
                value.PreferredFormat = RtpHeaderExtensionFormat.TwoByte;
            },
            value => value.PreferredFormat = (RtpHeaderExtensionFormat)99
        ];
        index = 0;
        while (index < mappingMutations.Length)
        {
            RtpHeaderExtensionMapping mapping = CreateMapping(1, "urn:test");
            mappingMutations[index](mapping);
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMapping(mapping),
                $"mapping mutation {index}");
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMapping(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpConfigurationValidator.ValidateHeaderExtensionMappings(null!));
        });
    }

    private static void AssertInvalidConfigurations(
        Action<RtpSessionConfiguration>[] mutations,
        Action<RtpSessionConfiguration> validator)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            RtpSessionConfiguration configuration =
                CreateValidSessionConfiguration();
            mutations[index](configuration);
            Assert.Throws<RtpConfigurationException>(() => validator(configuration),
                $"mutation {index}");
            index = index + 1;
        }
    }

    private static RtpSessionConfiguration CreateValidSessionConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.CanonicalName = "local@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 1000000.0;
        return configuration;
    }

    private static RtpPayloadTypeDefinition CreateMediaPayloadType(
        byte payloadType,
        string mediaIdentity,
        int clockRate)
    {
        RtpPayloadTypeDefinition definition = new RtpPayloadTypeDefinition();
        definition.PayloadType = payloadType;
        definition.MediaIdentity = mediaIdentity;
        definition.ClockRate = clockRate;
        definition.ChannelCount = 1;
        return definition;
    }

    private static RtpHeaderExtensionMapping CreateMapping(int identifier, string uri)
    {
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = identifier;
        mapping.Uri = uri;
        mapping.SupportsOneByte = true;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        return mapping;
    }
}
