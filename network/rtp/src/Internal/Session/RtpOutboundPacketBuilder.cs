using System;

namespace rtp;

internal class RtpOutboundPacketBuilder
{
    internal IRtpPayloadTypeRegistry payloadTypeRegistry;
    internal IRtpHeaderExtensionRegistry headerExtensionRegistry;

    internal RtpOutboundPacketBuilder(
        IRtpPayloadTypeRegistry payloadTypeRegistry,
        IRtpHeaderExtensionRegistry headerExtensionRegistry)
    {
        if (payloadTypeRegistry == null)
        {
            throw new RtpConfigurationException(
                "An RTP payload-type registry is required.");
        }

        if (headerExtensionRegistry == null)
        {
            throw new RtpConfigurationException(
                "An RTP header-extension registry is required.");
        }

        this.payloadTypeRegistry = payloadTypeRegistry;
        this.headerExtensionRegistry = headerExtensionRegistry;
    }

    internal RtpPacket BuildOutboundPacket(
        RtpLocalSourceState sourceState,
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        string? inputError = GetInputValidationError(sourceState, packetTemplate, context);
        if (inputError != null)
        {
            throw new RtpSessionException(inputError);
        }

        RtpPayloadTypeDefinition? payloadDefinition =
            payloadTypeRegistry.FindPayloadTypeDefinition(packetTemplate.PayloadType);
        if (payloadDefinition == null)
        {
            throw new RtpSessionException(
                "The outbound RTP payload type is not registered.");
        }

        RtpConfigurationValidator.ValidatePayloadTypeDefinition(payloadDefinition);
        if (payloadDefinition.ClockRate != context.ClockRate)
        {
            throw new RtpSessionException(
                "The outbound media clock rate does not match its payload type.");
        }

        RtpHeaderExtension? preparedExtension = null;
        if (packetTemplate.HeaderExtension != null)
        {
            RtpHeaderExtensionMapping[] mappings =
                headerExtensionRegistry.GetHeaderExtensionMappings();
            preparedExtension = PrepareMappedExtension(
                packetTemplate.HeaderExtension,
                mappings);
        }

        RtpPacket prepared = CreatePacketCopy(packetTemplate, preparedExtension);
        lock (sourceState.syncRoot)
        {
            uint timestamp = CalculateTimestamp(sourceState, context);
            prepared.SequenceNumber = sourceState.nextSequenceNumber;
            prepared.Timestamp = timestamp;
            prepared.SynchronizationSource = sourceState.sourceIdentifier;

            string? packetError = RtpPacketEncoder.GetPacketValidationError(prepared, false);
            if (packetError != null)
            {
                throw new RtpSessionException(packetError);
            }

            AdvanceSourceState(
                sourceState,
                context,
                timestamp,
                payloadDefinition);
        }

        return prepared;
    }

    internal static string? GetInputValidationError(
        RtpLocalSourceState sourceState,
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        if (sourceState == null)
        {
            return "The local RTP source state is required.";
        }

        if (packetTemplate == null)
        {
            return "The outbound RTP packet template is required.";
        }

        if (context == null)
        {
            return "The outbound RTP packet context is required.";
        }

        if (context.ClockRate <= 0)
        {
            return "The outbound RTP media clock rate must be positive.";
        }

        if (context.ArrivalTimeTicks < 0)
        {
            return "The outbound RTP send time cannot be negative.";
        }

        if (packetTemplate.PayloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            return "The outbound RTP payload type exceeds seven bits.";
        }

        if (packetTemplate.ContributingSources == null)
        {
            return "The outbound RTP contributing-source array is required.";
        }

        if (packetTemplate.ContributingSources.Length >
            RtpProtocolConstants.MaximumCsrcCount)
        {
            return "The outbound RTP contributing-source count exceeds 15.";
        }

        if (packetTemplate.Payload == null)
        {
            return "The outbound RTP payload array is required.";
        }

        if (packetTemplate.Padding == null)
        {
            return "The outbound RTP padding array is required.";
        }

        if (packetTemplate.HasExtension && packetTemplate.HeaderExtension == null)
        {
            return "The outbound RTP extension flag requires a header extension.";
        }

        if (!packetTemplate.HasExtension && packetTemplate.HeaderExtension != null)
        {
            return "An outbound RTP header extension requires the extension flag.";
        }

        return null;
    }

    internal static RtpHeaderExtension PrepareMappedExtension(
        RtpHeaderExtension extension,
        RtpHeaderExtensionMapping[] mappings)
    {
        if (extension == null)
        {
            throw new RtpSessionException("The RTP header extension is required.");
        }

        if (!RtpHeaderExtensionWireCodec.UsesElementEncoding(extension))
        {
            if (extension.Data == null)
            {
                throw new RtpSessionException(
                    "The RTP header-extension data array is required.");
            }

            if (extension.Elements == null)
            {
                throw new RtpSessionException(
                    "The RTP header-extension element array is required.");
            }

            int rawElementIndex = 0;
            while (rawElementIndex < extension.Elements.Length)
            {
                if (extension.Elements[rawElementIndex] == null ||
                    extension.Elements[rawElementIndex].Data == null)
                {
                    throw new RtpSessionException(
                        "Every RTP header-extension element must contain data.");
                }

                rawElementIndex = rawElementIndex + 1;
            }

            return extension.Copy();
        }

        string? extensionError =
            RtpHeaderExtensionWireCodec.GetEnvelopeValidationError(extension);
        if (extensionError != null)
        {
            throw new RtpSessionException(extensionError);
        }

        RtpHeaderExtension prepared =
            RtpHeaderExtensionWireCodec.GetSelectedEncodingExtension(extension).Copy();
        bool allSupportOneByte = true;
        bool allSupportTwoByte = true;
        bool prefersTwoByte = false;
        int elementIndex = 0;
        while (elementIndex < prepared.Elements.Length)
        {
            RtpHeaderExtensionMapping? mapping = FindMapping(
                mappings,
                prepared.Elements[elementIndex].Identifier);
            if (mapping == null)
            {
                throw new RtpSessionException(
                    "An outbound RTP header-extension element is not registered.");
            }

            if (!mapping.SupportsOneByte)
            {
                allSupportOneByte = false;
            }

            if (!mapping.SupportsTwoByte)
            {
                allSupportTwoByte = false;
            }

            if (mapping.PreferredFormat == RtpHeaderExtensionFormat.TwoByte)
            {
                prefersTwoByte = true;
            }

            elementIndex = elementIndex + 1;
        }

        if (extension.Format == RtpHeaderExtensionFormat.OneByte &&
            !allSupportOneByte)
        {
            throw new RtpSessionException(
                "A mapped extension does not support the requested one-byte format.");
        }

        if (extension.Format == RtpHeaderExtensionFormat.TwoByte &&
            !allSupportTwoByte)
        {
            throw new RtpSessionException(
                "A mapped extension does not support the requested two-byte format.");
        }

        if (extension.Format == RtpHeaderExtensionFormat.Raw)
        {
            if (prepared.Format == RtpHeaderExtensionFormat.OneByte &&
                allSupportOneByte && (!prefersTwoByte || !allSupportTwoByte))
            {
                return prepared;
            }

            if (allSupportTwoByte)
            {
                prepared.Format = RtpHeaderExtensionFormat.TwoByte;
                prepared.Profile = (ushort)(RtpHeaderExtensionProfiles.TwoByte |
                    prepared.ApplicationBits);
                return prepared;
            }

            throw new RtpSessionException(
                "No negotiated format can encode the RTP header-extension elements.");
        }

        return prepared;
    }

    internal static RtpHeaderExtensionMapping? FindMapping(
        RtpHeaderExtensionMapping[] mappings,
        int identifier)
    {
        if (mappings == null)
        {
            throw new RtpSessionException(
                "The RTP header-extension mapping snapshot is required.");
        }

        int mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            if (mappings[mappingIndex] != null &&
                mappings[mappingIndex].Identifier == identifier)
            {
                return mappings[mappingIndex];
            }

            mappingIndex = mappingIndex + 1;
        }

        return null;
    }

    internal static RtpPacket CreatePacketCopy(
        RtpPacket packetTemplate,
        RtpHeaderExtension? preparedExtension)
    {
        RtpPacket prepared = new RtpPacket();
        prepared.Version = RtpProtocolConstants.Version;
        prepared.HasPadding = packetTemplate.HasPadding;
        prepared.HasExtension = preparedExtension != null;
        prepared.Marker = packetTemplate.Marker;
        prepared.PayloadType = packetTemplate.PayloadType;
        prepared.ContributingSources =
            (uint[])packetTemplate.ContributingSources.Clone();
        prepared.HeaderExtension = preparedExtension;
        prepared.Payload = (byte[])packetTemplate.Payload.Clone();
        prepared.Padding = (byte[])packetTemplate.Padding.Clone();
        return prepared;
    }

    internal static uint CalculateTimestamp(
        RtpLocalSourceState sourceState,
        RtpPacketContext context)
    {
        if (sourceState.isSender &&
            context.ArrivalTimeTicks < sourceState.lastRtpActivityTicks)
        {
            throw new RtpSessionException(
                "The outbound RTP send time cannot move backward.");
        }

        if (!sourceState.hasMediaClockOrigin)
        {
            return sourceState.initialTimestamp;
        }

        if (sourceState.clockRate != context.ClockRate)
        {
            return sourceState.lastTimestamp;
        }

        if (context.MediaClockTimestamp < sourceState.lastMediaClockTimestamp)
        {
            throw new RtpSessionException(
                "The outbound media clock cannot move backward.");
        }

        long originRemainder = sourceState.mediaClockOrigin % 4294967296L;
        if (originRemainder < 0)
        {
            originRemainder = originRemainder + 4294967296L;
        }

        long currentRemainder = context.MediaClockTimestamp % 4294967296L;
        if (currentRemainder < 0)
        {
            currentRemainder = currentRemainder + 4294967296L;
        }

        long deltaRemainder = currentRemainder - originRemainder;
        if (deltaRemainder < 0)
        {
            deltaRemainder = deltaRemainder + 4294967296L;
        }

        ulong wideTimestamp =
            (ulong)sourceState.initialTimestamp + (ulong)deltaRemainder;
        return (uint)(wideTimestamp % 4294967296UL);
    }

    internal static void AdvanceSourceState(
        RtpLocalSourceState sourceState,
        RtpPacketContext context,
        uint timestamp,
        RtpPayloadTypeDefinition payloadDefinition)
    {
        if (sourceState.nextSequenceNumber == ushort.MaxValue)
        {
            sourceState.nextSequenceNumber = 0;
        }
        else
        {
            sourceState.nextSequenceNumber =
                (ushort)(sourceState.nextSequenceNumber + 1);
        }

        if (!sourceState.hasMediaClockOrigin || sourceState.clockRate != context.ClockRate)
        {
            sourceState.hasMediaClockOrigin = true;
            sourceState.mediaClockOrigin = context.MediaClockTimestamp;
            sourceState.initialTimestamp = timestamp;
            sourceState.clockRate = context.ClockRate;
        }

        sourceState.lastMediaClockTimestamp = context.MediaClockTimestamp;
        sourceState.lastTimestamp = timestamp;
        sourceState.isSender = true;
        sourceState.payloadType = payloadDefinition.PayloadType;
        sourceState.mediaIdentity = payloadDefinition.MediaIdentity;
        sourceState.channelCount = payloadDefinition.ChannelCount;
        sourceState.lastRtpActivityTicks = context.ArrivalTimeTicks;
        sourceState.transportIdentity = context.TransportIdentity;
    }
}
