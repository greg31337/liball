using System;

namespace rtp;

internal class RtpOutboundStream
{
    internal object syncRoot = new object();
    internal RtpLocalSourceState sourceState;
    internal RtpOutboundPacketBuilder packetBuilder;
    internal IRtpPayloadPacketizer? payloadPacketizer;
    internal RtpTransmissionStatistics transmissionStatistics;
    internal RtpPacketHistory packetHistory;
    internal bool strictValidation;
    internal int maximumRtpPacketSize;

    internal RtpOutboundStream(
        RtpSessionConfiguration configuration,
        RtpLocalSourceState sourceState,
        IRtpPayloadTypeRegistry payloadTypeRegistry,
        IRtpHeaderExtensionRegistry headerExtensionRegistry,
        IRtpPayloadPacketizer? payloadPacketizer,
        IRtpClock clock)
    {
        if (configuration == null)
        {
            throw new RtpConfigurationException(
                "An RTP session configuration is required for outbound processing.");
        }

        if (sourceState == null)
        {
            throw new RtpConfigurationException(
                "A local RTP source state is required for outbound processing.");
        }

        if (clock == null)
        {
            throw new RtpConfigurationException(
                "An RTP clock is required for outbound processing.");
        }

        packetBuilder = new RtpOutboundPacketBuilder(
            payloadTypeRegistry,
            headerExtensionRegistry);
        this.sourceState = sourceState;
        this.payloadPacketizer = payloadPacketizer;
        strictValidation = configuration.StrictValidation &&
            configuration.TransportMode == RtpSessionTransportMode.Multiplexed;
        maximumRtpPacketSize = configuration.MaximumRtpPacketSize;
        transmissionStatistics = new RtpTransmissionStatistics(
            sourceState.sourceIdentifier,
            clock);
        RtpSessionConfiguration historyConfiguration = configuration.Copy();
        historyConfiguration.StrictValidation = strictValidation;
        packetHistory = new RtpPacketHistory(historyConfiguration);
        lock (sourceState.syncRoot)
        {
            sourceState.transmissionStatistics = transmissionStatistics;
            sourceState.packetHistory = packetHistory;
        }
    }

    internal RtpSessionUpdate PrepareOutboundRtpPacket(
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        lock (syncRoot)
        {
            ValidatePreparedPacketBeforeMutation(packetTemplate, context);
            RtpPacket prepared = packetBuilder.BuildOutboundPacket(
                sourceState,
                packetTemplate,
                context);
            bool historyCapacityFailure =
                !RecordPreparedPacket(prepared, context) &&
                packetHistory.maximumPacketCount > 0;
            return CreateOutboundUpdate(
                new RtpPacket[] { prepared },
                context,
                null,
                historyCapacityFailure);
        }
    }

    internal RtpSessionUpdate PacketizeOutboundRtpPayload(
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        if (payloadPacketizer == null)
        {
            return CreateRecoverableOutboundUpdate(
                "An RTP payload packetizer is not configured.",
                context,
                null);
        }

        int maximumPayloadSize;
        lock (syncRoot)
        {
            maximumPayloadSize = GetMaximumPayloadSize(
                packetTemplate,
                context);
        }

        if (maximumPayloadSize < 0)
        {
            return CreateRecoverableOutboundUpdate(
                "The outbound RTP headers exceed the configured packet-size limit.",
                context,
                null);
        }

        RtpPacketizationResult result = payloadPacketizer.PacketizeRtpPayload(
            packetTemplate.Payload,
            maximumPayloadSize);
        string? resultError = GetPacketizationResultError(
            result,
            packetTemplate.Payload.Length,
            maximumPayloadSize);
        if (resultError != null)
        {
            return CreateRecoverableOutboundUpdate(
                resultError,
                context,
                CreateSafePacketizationResultCopy(result));
        }

        if (!result.Success)
        {
            string failureMessage = "The RTP payload packetizer did not produce packets.";
            if (result.ErrorMessage != null && result.ErrorMessage.Length > 0)
            {
                failureMessage = result.ErrorMessage;
            }

            return CreateRecoverableOutboundUpdate(
                failureMessage,
                context,
                result.Copy());
        }

        RtpPacketizationResult ownedResult = result.Copy();
        lock (syncRoot)
        {
            ValidatePacketizedPacketsBeforeMutation(
                packetTemplate,
                context,
                ownedResult);
            RtpPacket[] preparedPackets =
                new RtpPacket[ownedResult.Payloads.Length];
            int packetIndex = 0;
            while (packetIndex < ownedResult.Payloads.Length)
            {
                RtpPacket fragmentTemplate = CreateFragmentTemplate(
                    packetTemplate,
                    ownedResult.Payloads[packetIndex],
                    ownedResult.Markers[packetIndex]);
                preparedPackets[packetIndex] = packetBuilder.BuildOutboundPacket(
                    sourceState,
                    fragmentTemplate,
                    context);
                packetIndex = packetIndex + 1;
            }

            packetIndex = 0;
            bool historyCapacityFailure = false;
            while (packetIndex < preparedPackets.Length)
            {
                if (!RecordPreparedPacket(
                    preparedPackets[packetIndex],
                    context) &&
                    packetHistory.maximumPacketCount > 0)
                {
                    historyCapacityFailure = true;
                }

                packetIndex = packetIndex + 1;
            }

            return CreateOutboundUpdate(
                preparedPackets,
                context,
                ownedResult,
                historyCapacityFailure);
        }
    }

    internal int GetMaximumPayloadSize(
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        RtpPacket sizingPacket = CreateValidatedPreviewPacket(
            packetTemplate,
            context,
            [],
            false);
        int headerAndPaddingSize = RtpPacketEncoder.GetEncodedSize(
            sizingPacket,
            strictValidation,
            int.MaxValue);
        return maximumRtpPacketSize - headerAndPaddingSize;
    }

    internal void ValidatePreparedPacketBeforeMutation(
        RtpPacket packetTemplate,
        RtpPacketContext context)
    {
        RtpPacket preview = CreateValidatedPreviewPacket(
            packetTemplate,
            context,
            packetTemplate.Payload,
            packetTemplate.Marker);
        RtpPacketEncoder.GetEncodedSize(
            preview,
            strictValidation,
            maximumRtpPacketSize);
        ValidateAccountingChronology(context);
    }

    internal void ValidatePacketizedPacketsBeforeMutation(
        RtpPacket packetTemplate,
        RtpPacketContext context,
        RtpPacketizationResult result)
    {
        int packetIndex = 0;
        while (packetIndex < result.Payloads.Length)
        {
            RtpPacket preview = CreateValidatedPreviewPacket(
                packetTemplate,
                context,
                result.Payloads[packetIndex],
                result.Markers[packetIndex]);
            RtpPacketEncoder.GetEncodedSize(
                preview,
                strictValidation,
                maximumRtpPacketSize);
            packetIndex = packetIndex + 1;
        }

        ValidateAccountingChronology(context);
    }

    internal RtpPacket CreateValidatedPreviewPacket(
        RtpPacket packetTemplate,
        RtpPacketContext context,
        byte[] payload,
        bool marker)
    {
        string? inputError = RtpOutboundPacketBuilder.GetInputValidationError(
            sourceState,
            packetTemplate,
            context);
        if (inputError != null)
        {
            throw new RtpSessionException(inputError);
        }

        RtpPayloadTypeDefinition? payloadDefinition = packetBuilder
            .payloadTypeRegistry.FindPayloadTypeDefinition(
                packetTemplate.PayloadType);
        if (payloadDefinition == null)
        {
            throw new RtpSessionException(
                "The outbound RTP payload type is not registered.");
        }

        RtpConfigurationValidator.ValidatePayloadTypeDefinition(
            payloadDefinition);
        if (payloadDefinition.ClockRate != context.ClockRate)
        {
            throw new RtpSessionException(
                "The outbound media clock rate does not match its payload type.");
        }

        RtpHeaderExtension? preparedExtension = null;
        if (packetTemplate.HeaderExtension != null)
        {
            preparedExtension = RtpOutboundPacketBuilder.PrepareMappedExtension(
                packetTemplate.HeaderExtension,
                packetBuilder.headerExtensionRegistry
                    .GetHeaderExtensionMappings());
        }

        RtpPacket preview = RtpOutboundPacketBuilder.CreatePacketCopy(
            packetTemplate,
            preparedExtension);
        preview.Marker = marker;
        preview.Payload = (byte[])payload.Clone();
        lock (sourceState.syncRoot)
        {
            preview.SequenceNumber = sourceState.nextSequenceNumber;
            preview.Timestamp = RtpOutboundPacketBuilder.CalculateTimestamp(
                sourceState,
                context);
            preview.SynchronizationSource = sourceState.sourceIdentifier;
        }

        return preview;
    }

    internal static string? GetPacketizationResultError(
        RtpPacketizationResult result,
        int mediaDataLength,
        int maximumPayloadSize)
    {
        if (result == null)
        {
            return "The RTP payload packetizer returned no result.";
        }

        if (result.ConsumedCount < 0 ||
            result.ConsumedCount > mediaDataLength)
        {
            return "The RTP payload packetizer returned an invalid consumed byte count.";
        }

        if (result.Payloads == null || result.Markers == null)
        {
            return "The RTP payload packetizer returned missing fragment arrays.";
        }

        if (result.Payloads.Length != result.Markers.Length)
        {
            return "The RTP payload packetizer returned mismatched fragment and marker counts.";
        }

        if (result.Success &&
            result.ConsumedCount > 0 &&
            result.Payloads.Length == 0)
        {
            return "The RTP payload packetizer consumed media without producing a fragment.";
        }

        int payloadIndex = 0;
        while (payloadIndex < result.Payloads.Length)
        {
            if (result.Payloads[payloadIndex] == null)
            {
                return "The RTP payload packetizer returned a missing fragment.";
            }

            if (result.Payloads[payloadIndex].Length > maximumPayloadSize)
            {
                return "An RTP payload packetizer fragment exceeds the maximum payload size.";
            }

            payloadIndex = payloadIndex + 1;
        }

        return null;
    }

    internal static RtpPacketizationResult? CreateSafePacketizationResultCopy(
        RtpPacketizationResult result)
    {
        if (result == null || result.Payloads == null || result.Markers == null)
        {
            return null;
        }

        int payloadIndex = 0;
        while (payloadIndex < result.Payloads.Length)
        {
            if (result.Payloads[payloadIndex] == null)
            {
                return null;
            }

            payloadIndex = payloadIndex + 1;
        }

        return result.Copy();
    }

    internal static RtpPacket CreateFragmentTemplate(
        RtpPacket packetTemplate,
        byte[] payload,
        bool marker)
    {
        RtpPacket fragment = packetTemplate.Copy();
        fragment.Marker = marker;
        fragment.Payload = (byte[])payload.Clone();
        return fragment;
    }

    internal void ValidateAccountingChronology(RtpPacketContext context)
    {
        lock (sourceState.syncRoot)
        {
            if (sourceState.sourceIdentifier !=
                transmissionStatistics.sourceIdentifier ||
                sourceState.transmissionStatistics != transmissionStatistics ||
                sourceState.packetHistory != packetHistory)
            {
                throw new RtpSessionException(
                    "The outbound RTP stream no longer owns the local source state.");
            }
        }

        lock (transmissionStatistics.syncRoot)
        {
            if (transmissionStatistics.senderReportEligible &&
                context.ArrivalTimeTicks < transmissionStatistics.lastSendTimeTicks)
            {
                throw new RtpSessionException(
                    "The RTP packet send time cannot move backward.");
            }
        }

        lock (packetHistory.syncRoot)
        {
            packetHistory.ValidateHistoryChronology(context.ArrivalTimeTicks);
        }
    }

    internal bool RecordPreparedPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        transmissionStatistics.RecordSentRtpPacket(packet, context);
        return packetHistory.TryStoreRtpHistoryPacket(packet, context);
    }

    internal RtpSessionUpdate CreateOutboundUpdate(
        RtpPacket[] packets,
        RtpPacketContext context,
        RtpPacketizationResult? packetizationResult,
        bool historyCapacityFailure)
    {
        RtpSourceSnapshot sourceSnapshot = sourceState.CreateLocalSourceSnapshot();
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Source;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SourceIdentifier = sourceSnapshot.SourceIdentifier;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Source = sourceSnapshot;
        if (packets.Length == 1)
        {
            sessionEvent.Packet = packets[0].Copy();
        }

        if (packetizationResult != null)
        {
            sessionEvent.PacketizationResult = packetizationResult.Copy();
        }

        RtpSessionUpdate update = new RtpSessionUpdate();
        if (historyCapacityFailure)
        {
            RtpSessionEvent capacityEvent = new RtpSessionEvent();
            capacityEvent.Type = RtpSessionEventType.RecoverableError;
            capacityEvent.PacketKind = RtpPacketKind.Rtp;
            capacityEvent.SourceIdentifier =
                sourceSnapshot.SourceIdentifier;
            capacityEvent.PacketContext = context.Copy();
            capacityEvent.Source = sourceSnapshot.Copy();
            capacityEvent.Message =
                "An outbound RTP packet could not be retained within the configured history limits.";
            update.Events = new RtpSessionEvent[]
            {
                sessionEvent,
                capacityEvent
            };
        }
        else
        {
            update.Events = new RtpSessionEvent[] { sessionEvent };
        }

        update.OutboundPackets = new RtpPacket[packets.Length];
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            update.OutboundPackets[packetIndex] = packets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        return update;
    }

    internal RtpSessionUpdate CreateRecoverableOutboundUpdate(
        string message,
        RtpPacketContext context,
        RtpPacketizationResult? packetizationResult)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.RecoverableError;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.Message = message;
        if (context != null)
        {
            sessionEvent.PacketContext = context.Copy();
        }

        if (packetizationResult != null)
        {
            sessionEvent.PacketizationResult = packetizationResult.Copy();
        }

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = new RtpSessionEvent[] { sessionEvent };
        return update;
    }
}
