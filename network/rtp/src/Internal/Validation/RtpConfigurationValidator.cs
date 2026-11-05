using System;
using System.Text;

namespace rtp;

internal static class RtpConfigurationValidator
{
    internal static void ValidateRtpPacketCodecConfiguration(
        RtpSessionConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException("RTP packet codec configuration is required.");
        }

        if (configuration.MaximumRtpPacketSize < RtpProtocolConstants.FixedHeaderSize ||
            configuration.MaximumRtpPacketSize > 65535)
        {
            throw new RtpConfigurationException(
                "MaximumRtpPacketSize must be between the RTP header size and 65535 bytes.");
        }
    }

    internal static void ValidateRtpSequenceTrackerConfiguration(
        RtpSessionConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException(
                "RTP sequence tracker configuration is required.");
        }

        if (configuration.MinimumSequential <= 0 ||
            configuration.MinimumSequential >= RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MinimumSequential must fit within the RTP sequence-number space.");
        }

        if (configuration.JitterBuffer == null)
        {
            throw new RtpConfigurationException(
                "Jitter-buffer sequence thresholds are required.");
        }

        if (configuration.JitterBuffer.MaximumDropout <= 0 ||
            configuration.JitterBuffer.MaximumDropout >=
            RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumDropout must fit within the RTP sequence-number space.");
        }

        if (configuration.JitterBuffer.MaximumMisorder < 0 ||
            configuration.JitterBuffer.MaximumMisorder >=
            RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumMisorder must fit within the RTP sequence-number space.");
        }

        if (configuration.JitterBuffer.MaximumDropout +
            configuration.JitterBuffer.MaximumMisorder >=
            RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumDropout and MaximumMisorder leave no restart-detection range.");
        }
    }

    internal static void ValidateRtpPacketHistoryConfiguration(
        RtpSessionConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException(
                "RTP packet history configuration is required.");
        }

        if (configuration.MaximumRtpPacketSize <
            RtpProtocolConstants.FixedHeaderSize ||
            configuration.MaximumRtpPacketSize > 65535)
        {
            throw new RtpConfigurationException(
                "MaximumRtpPacketSize must be between the RTP header size and 65535 bytes.");
        }

        if (configuration.MaximumPacketHistoryCount < 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryCount cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryBytes < 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryBytes cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryAgeTicks < 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryAgeTicks cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryCount > 0 &&
            configuration.MaximumPacketHistoryBytes == 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryBytes must be positive when packet history is enabled.");
        }
    }

    internal static void ValidateRtpSourceTableConfiguration(
        RtpSessionConfiguration configuration)
    {
        ValidateRtpPacketCodecConfiguration(configuration);
        ValidateRtpSequenceTrackerConfiguration(configuration);
        if (configuration.MaximumSourceCount <= 0)
        {
            throw new RtpConfigurationException(
                "MaximumSourceCount must be positive.");
        }

        if (configuration.RtcpScheduler == null)
        {
            throw new RtpConfigurationException(
                "RTCP scheduler configuration is required.");
        }

        if (configuration.RtcpScheduler.SenderTimeoutTicks < 0L)
        {
            throw new RtpConfigurationException(
                "SenderTimeoutTicks cannot be negative.");
        }

        if (configuration.RtcpScheduler.MemberTimeoutTicks < 0L)
        {
            throw new RtpConfigurationException(
                "MemberTimeoutTicks cannot be negative.");
        }

        if (configuration.RtcpScheduler.MemberTimeoutTicks > 0L &&
            configuration.RtcpScheduler.SenderTimeoutTicks >
            configuration.RtcpScheduler.MemberTimeoutTicks)
        {
            throw new RtpConfigurationException(
                "SenderTimeoutTicks cannot exceed MemberTimeoutTicks.");
        }
    }

    internal static void ValidateRtcpPacketCodecConfiguration(
        RtpSessionConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException("RTCP packet codec configuration is required.");
        }

        if (configuration.MaximumRtcpCompoundPacketSize < RtcpProtocolConstants.HeaderSize ||
            configuration.MaximumRtcpCompoundPacketSize >
            RtcpProtocolConstants.MaximumPacketSize)
        {
            throw new RtpConfigurationException(
                "MaximumRtcpCompoundPacketSize is outside the RTCP wire limits.");
        }

        if (configuration.MaximumRtcpCompoundPacketCount <= 0 ||
            configuration.MaximumRtcpCompoundPacketCount >
            RtcpProtocolConstants.MaximumCompoundPacketCount)
        {
            throw new RtpConfigurationException(
                "MaximumRtcpCompoundPacketCount is outside the RTCP wire limits.");
        }

        ValidateRtcpCompoundMode(configuration.RtcpCompoundMode);
    }

    internal static void ValidateSessionConfiguration(RtpSessionConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException("Session configuration is required.");
        }

        if (configuration.TransportMode != RtpSessionTransportMode.Separate &&
            configuration.TransportMode != RtpSessionTransportMode.Multiplexed)
        {
            throw new RtpConfigurationException("TransportMode is not recognized.");
        }

        if (configuration.MaximumRtpPacketSize < RtpProtocolConstants.FixedHeaderSize ||
            configuration.MaximumRtpPacketSize > 65535)
        {
            throw new RtpConfigurationException(
                "MaximumRtpPacketSize must be between the RTP header size and 65535 bytes.");
        }

        if (configuration.MaximumRtcpCompoundPacketSize < RtcpProtocolConstants.HeaderSize ||
            configuration.MaximumRtcpCompoundPacketSize > RtcpProtocolConstants.MaximumPacketSize)
        {
            throw new RtpConfigurationException(
                "MaximumRtcpCompoundPacketSize is outside the RTCP wire limits.");
        }

        if (configuration.MaximumRtcpCompoundPacketCount <= 0 ||
            configuration.MaximumRtcpCompoundPacketCount >
            RtcpProtocolConstants.MaximumCompoundPacketCount)
        {
            throw new RtpConfigurationException(
                "MaximumRtcpCompoundPacketCount is outside the RTCP wire limits.");
        }

        ValidateRtcpCompoundMode(configuration.RtcpCompoundMode);

        if (configuration.MaximumSourceCount <= 0)
        {
            throw new RtpConfigurationException("MaximumSourceCount must be positive.");
        }

        if (configuration.MaximumPacketHistoryCount < 0)
        {
            throw new RtpConfigurationException("MaximumPacketHistoryCount cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryBytes < 0)
        {
            throw new RtpConfigurationException("MaximumPacketHistoryBytes cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryAgeTicks < 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryAgeTicks cannot be negative.");
        }

        if (configuration.MaximumPacketHistoryCount > 0 &&
            configuration.MaximumPacketHistoryBytes == 0)
        {
            throw new RtpConfigurationException(
                "MaximumPacketHistoryBytes must be positive when packet history is enabled.");
        }

        if (configuration.MinimumSequential <= 0 ||
            configuration.MinimumSequential >= RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MinimumSequential must fit within the RTP sequence-number space.");
        }

        if (configuration.CanonicalName == null || configuration.CanonicalName.Length == 0)
        {
            throw new RtpConfigurationException("CanonicalName is required.");
        }

        if (Encoding.UTF8.GetByteCount(configuration.CanonicalName) >
            RtcpProtocolConstants.MaximumTextLength)
        {
            throw new RtpConfigurationException(
                "CanonicalName exceeds the RTCP SDES text limit.");
        }

        ValidateOptionalSourceDescriptionItems(
            configuration.OptionalSourceDescriptionItems);

        if (configuration.RtcpScheduler == null)
        {
            throw new RtpConfigurationException("RtcpScheduler configuration is required.");
        }

        if (configuration.JitterBuffer == null)
        {
            throw new RtpConfigurationException("JitterBuffer configuration is required.");
        }

        ValidateRtcpSchedulerConfiguration(configuration.RtcpScheduler);
        ValidateRtpJitterBufferConfiguration(configuration.JitterBuffer);
    }

    internal static void ValidateOptionalSourceDescriptionItems(
        RtcpSourceDescriptionItem[] items)
    {
        if (items == null)
        {
            throw new RtpConfigurationException(
                "OptionalSourceDescriptionItems is required.");
        }

        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            RtcpSourceDescriptionItem? item = items[itemIndex];
            string? validationError =
                RtcpSourceDescriptionWireCodec.GetSourceDescriptionItemValidationError(item);
            if (validationError != null)
            {
                throw new RtpConfigurationException(validationError);
            }

            if (item.Type == RtcpSdesItemType.CanonicalName)
            {
                throw new RtpConfigurationException(
                    "CanonicalName is mandatory and cannot be configured as an optional item.");
            }

            itemIndex = itemIndex + 1;
        }
    }

    internal static void ValidateRtcpSchedulerConfiguration(
        RtcpSchedulerConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException("RTCP scheduler configuration is required.");
        }

        ValidatePositiveFiniteValue(
            configuration.SessionBandwidthBitsPerSecond,
            "SessionBandwidthBitsPerSecond");
        ValidateFractionValue(configuration.RtcpBandwidthFraction, "RtcpBandwidthFraction");
        ValidateFractionValue(configuration.SenderBandwidthFraction, "SenderBandwidthFraction");
        ValidateFractionValue(
            configuration.ReceiverBandwidthFraction,
            "ReceiverBandwidthFraction");
        if (Math.Abs(
            configuration.SenderBandwidthFraction +
            configuration.ReceiverBandwidthFraction - 1.0) > 0.000001)
        {
            throw new RtpConfigurationException(
                "SenderBandwidthFraction and ReceiverBandwidthFraction must total one.");
        }

        ValidatePositiveFiniteValue(
            configuration.MinimumIntervalSeconds,
            "MinimumIntervalSeconds");
        ValidatePositiveFiniteValue(
            configuration.InitialMinimumIntervalSeconds,
            "InitialMinimumIntervalSeconds");
        if (configuration.InitialMinimumIntervalSeconds > configuration.MinimumIntervalSeconds)
        {
            throw new RtpConfigurationException(
                "InitialMinimumIntervalSeconds cannot exceed MinimumIntervalSeconds.");
        }

        if (configuration.InitialAveragePacketSize <= 0)
        {
            throw new RtpConfigurationException("InitialAveragePacketSize must be positive.");
        }

        if (configuration.MinimumFeedbackIntervalTicks < 0)
        {
            throw new RtpConfigurationException(
                "MinimumFeedbackIntervalTicks cannot be negative.");
        }

        if (configuration.FeedbackRetentionTicks <
            RtcpProtocolConstants.MinimumFeedbackRetentionTicks)
        {
            throw new RtpConfigurationException(
                "FeedbackRetentionTicks must be at least two seconds.");
        }

        if (configuration.SenderTimeoutTicks < 0)
        {
            throw new RtpConfigurationException("SenderTimeoutTicks cannot be negative.");
        }

        if (configuration.MemberTimeoutTicks < 0)
        {
            throw new RtpConfigurationException("MemberTimeoutTicks cannot be negative.");
        }

        if (configuration.MemberTimeoutTicks > 0 &&
            configuration.SenderTimeoutTicks > configuration.MemberTimeoutTicks)
        {
            throw new RtpConfigurationException(
                "SenderTimeoutTicks cannot exceed MemberTimeoutTicks.");
        }
    }

    internal static void ValidateRtcpCompoundMode(RtcpCompoundMode compoundMode)
    {
        if (compoundMode != RtcpCompoundMode.Full &&
            compoundMode != RtcpCompoundMode.MinimalFeedback &&
            compoundMode != RtcpCompoundMode.ReducedSize)
        {
            throw new RtpConfigurationException("RtcpCompoundMode is not recognized.");
        }
    }

    internal static void ValidateRtpJitterBufferConfiguration(
        RtpJitterBufferConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException("Jitter-buffer configuration is required.");
        }

        if (configuration.Capacity <= 0)
        {
            throw new RtpConfigurationException("Jitter-buffer Capacity must be positive.");
        }

        if (configuration.MaximumLatencyTicks < 0)
        {
            throw new RtpConfigurationException("MaximumLatencyTicks cannot be negative.");
        }

        if (configuration.MaximumDropout <= 0 ||
            configuration.MaximumDropout >= RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumDropout must fit within the RTP sequence-number space.");
        }

        if (configuration.MaximumMisorder < 0 ||
            configuration.MaximumMisorder >= RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumMisorder must fit within the RTP sequence-number space.");
        }

        if (configuration.MaximumDropout + configuration.MaximumMisorder >=
            RtpProtocolConstants.SequenceNumberModulus)
        {
            throw new RtpConfigurationException(
                "MaximumDropout and MaximumMisorder leave no restart-detection range.");
        }

        if (configuration.NackDelayTicks < 0 ||
            configuration.NackRetryIntervalTicks < 0 ||
            configuration.MaximumNackRetryCount < 0 ||
            configuration.MaximumNackRetryPeriodTicks < 0)
        {
            throw new RtpConfigurationException("NACK timing and retry values cannot be negative.");
        }

        if (configuration.MaximumNackRetryCount == 0 &&
            (configuration.NackDelayTicks != 0 ||
            configuration.NackRetryIntervalTicks != 0 ||
            configuration.MaximumNackRetryPeriodTicks != 0))
        {
            throw new RtpConfigurationException(
                "NACK timing must be zero when NACK retries are disabled.");
        }

        if (configuration.MaximumNackRetryCount > 0 &&
            configuration.NackRetryIntervalTicks <= 0)
        {
            throw new RtpConfigurationException(
                "NackRetryIntervalTicks must be positive when NACK retries are enabled.");
        }

        if (configuration.MaximumNackRetryCount > 0 &&
            configuration.MaximumNackRetryPeriodTicks < configuration.NackRetryIntervalTicks)
        {
            throw new RtpConfigurationException(
                "MaximumNackRetryPeriodTicks must include at least one retry interval.");
        }

        if (configuration.MaximumNackRetryCount > 0 &&
            configuration.MaximumNackRetryPeriodTicks < configuration.NackDelayTicks)
        {
            throw new RtpConfigurationException(
                "MaximumNackRetryPeriodTicks cannot end before the initial NACK delay.");
        }

        if (configuration.MaximumLatencyTicks > 0 &&
            configuration.NackDelayTicks > configuration.MaximumLatencyTicks)
        {
            throw new RtpConfigurationException(
                "NackDelayTicks cannot exceed MaximumLatencyTicks.");
        }
    }

    internal static void ValidatePayloadTypeDefinition(RtpPayloadTypeDefinition definition)
    {
        if (definition == null)
        {
            throw new RtpConfigurationException("Payload-type definition is required.");
        }

        if (definition.PayloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            throw new RtpConfigurationException("PayloadType exceeds the seven-bit RTP field.");
        }

        if (definition.ClockRate <= 0)
        {
            throw new RtpConfigurationException("ClockRate must be positive.");
        }

        if (definition.MediaIdentity == null || definition.MediaIdentity.Length == 0)
        {
            throw new RtpConfigurationException("MediaIdentity is required.");
        }

        if (definition.ChannelCount <= 0)
        {
            throw new RtpConfigurationException("ChannelCount must be positive.");
        }

        if (definition.IsRetransmission && !definition.HasAssociatedPayloadType)
        {
            throw new RtpConfigurationException(
                "An RTX payload type must identify its associated media payload type.");
        }

        if (!definition.IsRetransmission && definition.HasAssociatedPayloadType)
        {
            throw new RtpConfigurationException(
                "Only an RTX payload type can identify an associated media payload type.");
        }

        if (definition.HasAssociatedPayloadType &&
            definition.AssociatedPayloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            throw new RtpConfigurationException(
                "AssociatedPayloadType exceeds the seven-bit RTP field.");
        }

        if (definition.HasAssociatedPayloadType &&
            definition.AssociatedPayloadType == definition.PayloadType)
        {
            throw new RtpConfigurationException(
                "An RTX payload type cannot be associated with itself.");
        }
    }

    internal static void ValidatePayloadTypeDefinitions(
        RtpPayloadTypeDefinition[] definitions,
        RtpSessionTransportMode transportMode)
    {
        if (definitions == null)
        {
            throw new RtpConfigurationException("Payload-type definitions are required.");
        }

        if (transportMode != RtpSessionTransportMode.Separate &&
            transportMode != RtpSessionTransportMode.Multiplexed)
        {
            throw new RtpConfigurationException("The payload-type transport mode is not recognized.");
        }

        int definitionIndex = 0;
        while (definitionIndex < definitions.Length)
        {
            ValidatePayloadTypeDefinition(definitions[definitionIndex]);
            if (transportMode == RtpSessionTransportMode.Multiplexed)
            {
                ValidateMultiplexedPayloadType(definitions[definitionIndex]);
            }

            definitionIndex = definitionIndex + 1;
        }

        definitionIndex = 0;
        while (definitionIndex < definitions.Length)
        {
            int duplicateIndex = definitionIndex + 1;
            while (duplicateIndex < definitions.Length)
            {
                if (definitions[definitionIndex].PayloadType ==
                    definitions[duplicateIndex].PayloadType)
                {
                    throw new RtpConfigurationException(
                        "Payload-type definitions contain a duplicate PayloadType.");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            definitionIndex = definitionIndex + 1;
        }

        int associationIndex = 0;
        while (associationIndex < definitions.Length)
        {
            if (definitions[associationIndex].HasAssociatedPayloadType)
            {
                bool associationFound = false;
                int mediaIndex = 0;
                while (mediaIndex < definitions.Length)
                {
                    if (definitions[mediaIndex].PayloadType ==
                        definitions[associationIndex].AssociatedPayloadType &&
                        !definitions[mediaIndex].IsRetransmission)
                    {
                        associationFound = true;
                    }

                    mediaIndex = mediaIndex + 1;
                }

                if (!associationFound)
                {
                    throw new RtpConfigurationException(
                        "An RTX association must reference a registered media payload type.");
                }

                int associatedMediaIndex = 0;
                while (associatedMediaIndex < definitions.Length)
                {
                    if (definitions[associatedMediaIndex].PayloadType ==
                        definitions[associationIndex].AssociatedPayloadType &&
                        definitions[associatedMediaIndex].ClockRate !=
                        definitions[associationIndex].ClockRate)
                    {
                        throw new RtpConfigurationException(
                            "An RTX payload type must use its associated media clock rate.");
                    }

                    associatedMediaIndex = associatedMediaIndex + 1;
                }
            }

            associationIndex = associationIndex + 1;
        }
    }

    internal static void ValidateMultiplexedPayloadType(RtpPayloadTypeDefinition definition)
    {
        if (definition.PayloadType >=
            RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMinimum &&
            definition.PayloadType <=
            RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMaximum)
        {
            throw new RtpConfigurationException(
                "PayloadType is forbidden when RTP and RTCP are multiplexed.");
        }
    }

    internal static void ValidateHeaderExtensionMapping(RtpHeaderExtensionMapping mapping)
    {
        if (mapping == null)
        {
            throw new RtpConfigurationException("Header-extension mapping is required.");
        }

        if (mapping.Identifier <= 0 || mapping.Identifier > 255)
        {
            throw new RtpConfigurationException(
                "Header-extension Identifier must be between 1 and 255.");
        }

        if (mapping.Uri == null || mapping.Uri.Length == 0)
        {
            throw new RtpConfigurationException("Header-extension Uri is required.");
        }

        if (mapping.Attributes == null)
        {
            throw new RtpConfigurationException("Header-extension Attributes cannot be null.");
        }

        if (!mapping.SupportsOneByte && !mapping.SupportsTwoByte)
        {
            throw new RtpConfigurationException(
                "A header-extension mapping must support at least one RFC 8285 format.");
        }

        if (mapping.SupportsOneByte && mapping.Identifier > 14)
        {
            throw new RtpConfigurationException(
                "One-byte header-extension identifiers must be between 1 and 14.");
        }

        if (mapping.PreferredFormat == RtpHeaderExtensionFormat.Raw)
        {
            throw new RtpConfigurationException(
                "An RFC 8285 mapping cannot prefer the raw extension format.");
        }

        if (mapping.PreferredFormat == RtpHeaderExtensionFormat.OneByte &&
            !mapping.SupportsOneByte)
        {
            throw new RtpConfigurationException(
                "PreferredFormat requires one-byte format support.");
        }

        if (mapping.PreferredFormat == RtpHeaderExtensionFormat.TwoByte &&
            !mapping.SupportsTwoByte)
        {
            throw new RtpConfigurationException(
                "PreferredFormat requires two-byte format support.");
        }

        if (mapping.PreferredFormat != RtpHeaderExtensionFormat.OneByte &&
            mapping.PreferredFormat != RtpHeaderExtensionFormat.TwoByte)
        {
            throw new RtpConfigurationException("PreferredFormat is not recognized.");
        }
    }

    internal static void ValidateHeaderExtensionMappings(RtpHeaderExtensionMapping[] mappings)
    {
        if (mappings == null)
        {
            throw new RtpConfigurationException("Header-extension mappings are required.");
        }

        int mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            ValidateHeaderExtensionMapping(mappings[mappingIndex]);
            mappingIndex = mappingIndex + 1;
        }

        mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            int duplicateIndex = mappingIndex + 1;
            while (duplicateIndex < mappings.Length)
            {
                if (mappings[mappingIndex].Identifier == mappings[duplicateIndex].Identifier)
                {
                    throw new RtpConfigurationException(
                        "Header-extension mappings contain a duplicate Identifier.");
                }

                if (string.Equals(
                    mappings[mappingIndex].Uri,
                    mappings[duplicateIndex].Uri,
                    StringComparison.Ordinal))
                {
                    throw new RtpConfigurationException(
                        "Header-extension mappings contain a duplicate Uri.");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            mappingIndex = mappingIndex + 1;
        }
    }

    internal static void ValidatePositiveFiniteValue(double value, string fieldName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
        {
            throw new RtpConfigurationException($"{fieldName} must be positive and finite.");
        }
    }

    internal static void ValidateFractionValue(double value, string fieldName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value <= 0.0 || value > 1.0)
        {
            throw new RtpConfigurationException($"{fieldName} must be greater than zero and at most one.");
        }
    }
}
