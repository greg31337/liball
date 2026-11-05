using System;
using System.Collections.Generic;

namespace rtp;

internal class RtpSeparatePortIngress
{
    internal IRtpPacketCodec rtpPacketCodec;
    internal IRtcpPacketCodec rtcpPacketCodec;
    internal IRtpPayloadTypeRegistry payloadTypeRegistry;
    internal IRtpJitterBuffer jitterBuffer;
    internal RtpLocalSourceTable localSourceTable;
    internal RtpSourceTable remoteSourceTable;
    internal RtpSourceIdentityManager sourceIdentityManager;
    internal IRtpRtcpDemultiplexer demultiplexer;
    internal RtpSessionTransportMode transportMode;
    internal RtpInboundMediaProcessor? inboundMediaProcessor;
    internal RtcpFeedbackScheduler? feedbackScheduler;
    internal RtpRepairCoordinator repairCoordinator;
    internal RtcpSchedulerConfiguration feedbackConfiguration;
    internal IRtpRandomSource feedbackRandomSource;

    internal RtpSeparatePortIngress(
        RtpSessionConfiguration configuration,
        IRtpRandomSource randomSource,
        IRtpPayloadTypeRegistry payloadTypeRegistry)
    {
        if (payloadTypeRegistry == null)
        {
            throw new RtpConfigurationException(
                "An RTP ingress payload-type registry is required.");
        }

        rtpPacketCodec = new RtpPacketCodec(configuration);
        rtcpPacketCodec = new RtcpPacketCodec(configuration);
        jitterBuffer = new RtpJitterBuffer(configuration.JitterBuffer);
        localSourceTable = new RtpLocalSourceTable(
            configuration,
            randomSource);
        remoteSourceTable = new RtpSourceTable(
            configuration,
            randomSource);
        remoteSourceTable.ConfigureRemotePayloadTypeRegistry(
            payloadTypeRegistry);
        sourceIdentityManager = new RtpSourceIdentityManager(
            localSourceTable,
            remoteSourceTable);
        demultiplexer = new RtpRtcpDemultiplexer();
        repairCoordinator = new RtpRepairCoordinator(null, null);
        feedbackConfiguration = configuration.RtcpScheduler.Copy();
        feedbackRandomSource = randomSource;
        transportMode = configuration.TransportMode;
        this.payloadTypeRegistry = payloadTypeRegistry;
    }

    internal void ConfigurePayloadDepacketizer(
        IRtpPayloadDepacketizer payloadDepacketizer)
    {
        inboundMediaProcessor = new RtpInboundMediaProcessor(
            payloadDepacketizer);
    }

    internal void ConfigureRepairHooks(
        IRtpRepairEncoder? repairEncoder,
        IRtpRepairDecoder? repairDecoder)
    {
        repairCoordinator = new RtpRepairCoordinator(
            repairEncoder,
            repairDecoder);
    }

    internal RtpSessionUpdate ProcessMultiplexedDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(datagram);
        if (transportMode != RtpSessionTransportMode.Multiplexed)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Unknown,
                "Multiplexed RTP ingress is not enabled for this session.",
                context);
        }

        if (classification.Kind == RtpPacketKind.Rtp)
        {
            return ProcessSeparateRtpDatagram(datagram, context);
        }

        if (classification.Kind == RtpPacketKind.Rtcp)
        {
            return ProcessSeparateRtcpDatagram(datagram, context);
        }

        string errorMessage =
            "The multiplexed datagram could not be classified.";
        if (classification.ErrorMessage != null)
        {
            errorMessage = classification.ErrorMessage;
        }

        return CreateRecoverableIngressUpdate(
            RtpPacketKind.Unknown,
            errorMessage,
            context);
    }

    internal RtpSessionUpdate ProcessSeparateRtpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        string? contextError = GetIngressContextError(context);
        if (contextError != null)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Rtp,
                contextError,
                context);
        }

        RtpCodecResult decodeResult =
            rtpPacketCodec.TryDecodeRtpPacket(datagram);
        if (!decodeResult.Success || decodeResult.Packet == null)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Rtp,
                GetRtpDecodeError(decodeResult),
                context);
        }

        RtpPacket packet = decodeResult.Packet;
        RtpPayloadTypeDefinition? payloadDefinition =
            payloadTypeRegistry.FindPayloadTypeDefinition(
                packet.PayloadType);
        if (payloadDefinition == null)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Rtp,
                "The inbound RTP payload type is not registered.",
                context);
        }

        if (!CanRetainRtpPacketSources(
            new RtpPacket[] { packet }))
        {
            return CreateRemoteSourceCapacityIngressUpdate(
                RtpPacketKind.Rtp,
                context);
        }

        RtpPacketContext mappedContext = context.Copy();
        mappedContext.ClockRate = payloadDefinition.ClockRate;
        List<RtpSessionEvent> events = new List<RtpSessionEvent>();
        RtpSourceIdentityUpdate identityUpdate =
            sourceIdentityManager.InspectIncomingSourceIdentity(
                packet.SynchronizationSource,
                mappedContext);
        if (identityUpdate.collisionDetected ||
            identityUpdate.loopDetected)
        {
            events.Add(CreateCollisionIngressEvent(
                identityUpdate,
                RtpPacketKind.Rtp,
                mappedContext,
                packet,
                null));
            if (identityUpdate.loopDetected ||
                identityUpdate.goodbye == null)
            {
                RtpSessionUpdate collisionUpdate = new RtpSessionUpdate();
                collisionUpdate.Events = events.ToArray();
                return collisionUpdate;
            }
        }

        RtpSequenceUpdate sequenceUpdate =
            remoteSourceTable.RecordMappedRemoteRtpPacket(
                packet,
                mappedContext);
        RtpJitterBufferUpdate? jitterUpdate = null;
        if (sequenceUpdate.Accepted ||
            sequenceUpdate.Status == RtpSequenceStatus.Duplicate)
        {
            jitterUpdate = jitterBuffer.TryAddRtpJitterBufferPacket(
                packet,
                mappedContext,
                sequenceUpdate);
            if (jitterUpdate.Status == RtpJitterBufferStatus.Late)
            {
                mappedContext.IsLate = true;
            }
        }

        RtpSourceSnapshot sourceSnapshot =
            remoteSourceTable.RecordRemoteRtpReception(
                packet,
                mappedContext,
                sequenceUpdate);
        events.Add(CreateRtpSourceIngressEvent(
            packet,
            mappedContext,
            sequenceUpdate,
            jitterUpdate,
            sourceSnapshot));
        if (jitterUpdate != null && inboundMediaProcessor != null)
        {
            RtpSessionEvent[] mediaEvents = inboundMediaProcessor
                .ProcessReleasedRtpPackets(
                    jitterUpdate,
                    mappedContext,
                    sourceSnapshot);
            int mediaEventIndex = 0;
            while (mediaEventIndex < mediaEvents.Length)
            {
                events.Add(mediaEvents[mediaEventIndex]);
                mediaEventIndex = mediaEventIndex + 1;
            }
        }
        else if (jitterUpdate != null &&
            jitterUpdate.LostSequenceNumbers.Length > 0)
        {
            events.Add(CreateJitterLossIngressEvent(
                packet,
                mappedContext,
                sourceSnapshot,
                jitterUpdate));
        }

        if (jitterUpdate != null &&
            jitterUpdate.Status == RtpJitterBufferStatus.Overflow)
        {
            events.Add(CreateJitterCapacityIngressEvent(
                packet,
                mappedContext,
                sourceSnapshot,
                jitterUpdate));
        }

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        if (sequenceUpdate.Accepted)
        {
            update.AcceptedPackets = new RtpPacket[] { packet.Copy() };
        }

        if (jitterUpdate != null)
        {
            QueueIngressFeedbackRequests(jitterUpdate.FeedbackRequests);
            update.ReleasedPackets = jitterUpdate.ReleasedPackets;
            update.FeedbackRequests = jitterUpdate.FeedbackRequests;
            update.RepairRequests = jitterUpdate.RepairRequests;
        }

        return update;
    }

    internal RtpSessionUpdate ProcessSeparateRtcpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context)
    {
        string? contextError = GetIngressContextError(context);
        if (contextError != null)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Rtcp,
                contextError,
                context);
        }

        RtcpCodecResult decodeResult =
            rtcpPacketCodec.TryDecodeRtcpCompoundPacket(datagram);
        if (!decodeResult.Success || decodeResult.CompoundPacket == null)
        {
            return CreateRecoverableIngressUpdate(
                RtpPacketKind.Rtcp,
                GetRtcpDecodeError(decodeResult),
                context);
        }

        if (!CanRetainRtcpPacketSources(decodeResult.CompoundPacket))
        {
            return CreateRemoteSourceCapacityIngressUpdate(
                RtpPacketKind.Rtcp,
                context);
        }

        List<RtpSessionEvent> events = new List<RtpSessionEvent>();
        List<RtpRepairRequest> repairRequests =
            new List<RtpRepairRequest>();
        List<RtpRepairResult> repairResults =
            new List<RtpRepairResult>();
        int packetIndex = 0;
        while (packetIndex < decodeResult.CompoundPacket.Packets.Length)
        {
            RtcpPacket packet =
                decodeResult.CompoundPacket.Packets[packetIndex];
            ProcessDecodedRtcpPacket(
                packet,
                context,
                events,
                repairRequests,
                repairResults);

            packetIndex = packetIndex + 1;
        }

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        update.RepairRequests = repairRequests.ToArray();
        update.RepairResults = repairResults.ToArray();
        return update;
    }

    internal void QueueIngressFeedbackRequests(
        RtpFeedbackRequest[] feedbackRequests)
    {
        if (feedbackRequests.Length == 0)
        {
            return;
        }

        RtcpFeedbackScheduler scheduler = GetIngressFeedbackScheduler();
        int requestIndex = 0;
        while (requestIndex < feedbackRequests.Length)
        {
            scheduler.QueueRtcpFeedback(
                feedbackRequests[requestIndex]);
            requestIndex = requestIndex + 1;
        }
    }

    internal RtcpFeedbackScheduler GetIngressFeedbackScheduler()
    {
        if (feedbackScheduler == null)
        {
            feedbackScheduler = new RtcpFeedbackScheduler(
                feedbackConfiguration,
                feedbackRandomSource);
        }

        return feedbackScheduler;
    }

    internal void ProcessDecodedRtcpPacket(
        RtcpPacket packet,
        RtpPacketContext context,
        List<RtpSessionEvent> events,
        List<RtpRepairRequest> repairRequests,
        List<RtpRepairResult> repairResults)
    {
        if (packet.Type == RtcpPacketType.SourceDescription &&
            packet.SourceDescription != null)
        {
            ProcessRtcpSourceDescription(
                packet,
                context,
                events);
            return;
        }

        if (packet.Type == RtcpPacketType.Goodbye &&
            packet.Goodbye != null)
        {
            ProcessRtcpGoodbye(packet, context, events);
            return;
        }

        RtpSourceSnapshot? sourceSnapshot = null;
        if (HasControlSenderSource(packet))
        {
            uint sourceIdentifier = GetControlSenderSource(packet);
            if (!CanProcessRtcpSourceIdentity(
                sourceIdentifier,
                packet,
                context,
                events))
            {
                return;
            }

            sourceSnapshot = ProcessRtcpSenderPacketState(
                packet,
                context,
                sourceIdentifier);
            ProcessRemoteReceptionReports(
                packet,
                sourceIdentifier,
                context.ArrivalTimeTicks);
            if (packet.Feedback != null)
            {
                ProcessRtcpFeedback(
                    packet,
                    context,
                    events,
                    repairRequests,
                    repairResults);
            }
        }

        events.Add(CreateRtcpReportIngressEvent(
            packet,
            context,
            sourceSnapshot));
    }

    internal bool CanProcessRtcpSourceIdentity(
        uint sourceIdentifier,
        RtcpPacket packet,
        RtpPacketContext context,
        List<RtpSessionEvent> events)
    {
        RtpSourceIdentityUpdate identityUpdate =
            sourceIdentityManager.InspectIncomingSourceIdentity(
                sourceIdentifier,
                context);
        if (identityUpdate.collisionDetected || identityUpdate.loopDetected)
        {
            events.Add(CreateCollisionIngressEvent(
                identityUpdate,
                RtpPacketKind.Rtcp,
                context,
                null,
                packet));
        }

        return !identityUpdate.loopDetected &&
            (!identityUpdate.collisionDetected ||
            identityUpdate.goodbye != null);
    }

    internal RtpSourceSnapshot? ProcessRtcpSenderPacketState(
        RtcpPacket packet,
        RtpPacketContext context,
        uint sourceIdentifier)
    {
        if (packet.Type == RtcpPacketType.ExtendedReport &&
            packet.ExtendedReport != null)
        {
            return remoteSourceTable.RecordRemoteExtendedReport(
                packet.ExtendedReport,
                context);
        }

        remoteSourceTable.RecordRemoteRtcpActivity(
            sourceIdentifier,
            context);
        RtpSourceState? source = remoteSourceTable.FindSourceState(
            sourceIdentifier);
        if (source != null &&
            packet.Type == RtcpPacketType.SenderReport &&
            packet.SenderReport != null)
        {
            source.receptionStatistics.RecordReceivedSenderReport(
                packet.SenderReport,
                context.ArrivalTimeTicks);
        }

        if (source == null)
        {
            return null;
        }

        return source.CreateSourceStatisticsSnapshot(
            context.ArrivalTimeTicks);
    }

    internal void ProcessRemoteReceptionReports(
        RtcpPacket packet,
        uint reporterSourceIdentifier,
        long arrivalTimeTicks)
    {
        RtcpReceptionReport[] reports = GetRtcpReceptionReports(packet);
        int reportIndex = 0;
        while (reportIndex < reports.Length)
        {
            RtcpReceptionReport report = reports[reportIndex];
            RtpLocalSourceState? localSource =
                localSourceTable.FindLocalSourceState(
                    report.SourceIdentifier);
            if (localSource != null &&
                localSource.transmissionStatistics != null)
            {
                localSource.transmissionStatistics
                    .RecordRemoteReceptionReport(
                        reporterSourceIdentifier,
                        report,
                        arrivalTimeTicks);
            }

            reportIndex = reportIndex + 1;
        }
    }

    internal RtcpReceptionReport[] GetRtcpReceptionReports(
        RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport &&
            packet.SenderReport != null)
        {
            return packet.SenderReport.ReceptionReports;
        }

        if (packet.Type == RtcpPacketType.ReceiverReport &&
            packet.ReceiverReport != null)
        {
            return packet.ReceiverReport.ReceptionReports;
        }

        return [];
    }

    internal void ProcessRtcpFeedback(
        RtcpPacket packet,
        RtpPacketContext context,
        List<RtpSessionEvent> events,
        List<RtpRepairRequest> repairRequests,
        List<RtpRepairResult> repairResults)
    {
        RtcpFeedbackPacket? feedback = packet.Feedback;
        if (feedback == null)
        {
            return;
        }

        GetIngressFeedbackScheduler().ObserveRtcpFeedback(
            feedback,
            context.ArrivalTimeTicks);
        if (feedback.PacketType != RtcpFeedbackPacketType.TransportLayer ||
            feedback.GenericNack == null)
        {
            return;
        }

        RtpLocalSourceState? localSource =
            localSourceTable.FindLocalSourceState(
                feedback.MediaSourceIdentifier);
        if (localSource == null)
        {
            return;
        }

        ushort[] sequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                feedback.GenericNack);
        if (sequenceNumbers.Length == 0)
        {
            return;
        }

        RtpRepairRequest request = CreateRtcpRepairRequest(
            feedback.MediaSourceIdentifier,
            sequenceNumbers,
            context.ArrivalTimeTicks);
        repairRequests.Add(request);
        RtpRepairResult? result = null;
        if (repairCoordinator.repairEncoder != null)
        {
            RtpPacket[] sourcePackets = GetRtcpRepairSourcePackets(
                localSource,
                sequenceNumbers);
            result = repairCoordinator.EncodeRepair(
                request,
                sourcePackets);
            repairResults.Add(result);
        }

        events.Add(CreateRtcpRepairIngressEvent(
            packet,
            context,
            request,
            result));
    }

    internal RtpRepairRequest CreateRtcpRepairRequest(
        uint sourceIdentifier,
        ushort[] sequenceNumbers,
        long requestTimeTicks)
    {
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = RtpRepairKind.Retransmission;
        request.SourceIdentifier = sourceIdentifier;
        request.SequenceNumbers = (ushort[])sequenceNumbers.Clone();
        request.RequestTimeTicks = requestTimeTicks;
        request.DeadlineTicks = requestTimeTicks;
        return request;
    }

    internal RtpPacket[] GetRtcpRepairSourcePackets(
        RtpLocalSourceState localSource,
        ushort[] sequenceNumbers)
    {
        List<RtpPacket> packets = new List<RtpPacket>();
        if (localSource.packetHistory == null)
        {
            return packets.ToArray();
        }

        int sequenceIndex = 0;
        while (sequenceIndex < sequenceNumbers.Length)
        {
            RtpPacket? packet = localSource.packetHistory
                .FindRtpHistoryPacket(
                    localSource.sourceIdentifier,
                    sequenceNumbers[sequenceIndex]);
            if (packet != null)
            {
                packets.Add(packet);
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return packets.ToArray();
    }

    internal void ProcessRtcpSourceDescription(
        RtcpPacket packet,
        RtpPacketContext context,
        List<RtpSessionEvent> events)
    {
        RtcpSourceDescription? description = packet.SourceDescription;
        if (description == null)
        {
            return;
        }

        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtcpSourceDescriptionChunk chunk =
                description.Chunks[chunkIndex];
            if (CanProcessRtcpSourceIdentity(
                chunk.SourceIdentifier,
                packet,
                context,
                events))
            {
                RtcpSourceDescription singleDescription =
                    new RtcpSourceDescription();
                singleDescription.Chunks =
                    new RtcpSourceDescriptionChunk[] { chunk.Copy() };
                RtpSourceSnapshot[] snapshots = remoteSourceTable
                    .RecordRemoteSourceDescription(
                        singleDescription,
                        context);
                events.Add(CreateRtcpSourceIngressEvent(
                    packet,
                    context,
                    snapshots[0]));
            }

            chunkIndex = chunkIndex + 1;
        }

        if (description.Chunks.Length == 0)
        {
            events.Add(CreateRtcpReportIngressEvent(
                packet,
                context,
                null));
        }
    }

    internal void ProcessRtcpGoodbye(
        RtcpPacket packet,
        RtpPacketContext context,
        List<RtpSessionEvent> events)
    {
        RtcpGoodbye? goodbye = packet.Goodbye;
        if (goodbye == null)
        {
            return;
        }

        int sourceIndex = 0;
        while (sourceIndex < goodbye.Sources.Length)
        {
            uint sourceIdentifier = goodbye.Sources[sourceIndex];
            if (CanProcessRtcpSourceIdentity(
                sourceIdentifier,
                packet,
                context,
                events))
            {
                RtcpGoodbye singleGoodbye = new RtcpGoodbye();
                singleGoodbye.Sources = new uint[] { sourceIdentifier };
                singleGoodbye.HasReason = goodbye.HasReason;
                singleGoodbye.Reason = (byte[])goodbye.Reason.Clone();
                RtpSourceSnapshot[] snapshots = remoteSourceTable
                    .RecordRemoteGoodbye(singleGoodbye, context);
                RtpSourceSnapshot? snapshot = null;
                if (snapshots.Length > 0)
                {
                    snapshot = snapshots[0];
                }

                events.Add(CreateRtcpGoodbyeIngressEvent(
                    packet,
                    context,
                    sourceIdentifier,
                    snapshot));
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal string? GetIngressContextError(RtpPacketContext context)
    {
        if (context == null)
        {
            return "An inbound RTP packet context is required.";
        }

        if (context.TransportIdentity == null ||
            context.TransportIdentity.Length == 0)
        {
            return "An inbound RTP transport identity is required.";
        }

        if (context.ArrivalTimeTicks < 0)
        {
            return "An inbound RTP arrival time cannot be negative.";
        }

        return null;
    }

    internal string GetRtpDecodeError(RtpCodecResult result)
    {
        if (result.ErrorMessage != null)
        {
            return result.ErrorMessage;
        }

        return "The inbound RTP datagram could not be decoded.";
    }

    internal string GetRtcpDecodeError(RtcpCodecResult result)
    {
        if (result.ErrorMessage != null)
        {
            return result.ErrorMessage;
        }

        return "The inbound RTCP datagram could not be decoded.";
    }

    internal RtpSessionUpdate CreateRecoverableIngressUpdate(
        RtpPacketKind packetKind,
        string message,
        RtpPacketContext? context)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.RecoverableError;
        sessionEvent.PacketKind = packetKind;
        sessionEvent.Message = message;
        if (context != null)
        {
            sessionEvent.PacketContext = context.Copy();
        }

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = new RtpSessionEvent[] { sessionEvent };
        return update;
    }

    internal RtpSessionUpdate CreateRemoteSourceCapacityIngressUpdate(
        RtpPacketKind packetKind,
        RtpPacketContext context)
    {
        return CreateRecoverableIngressUpdate(
            packetKind,
            "The RTP remote source table capacity has been reached.",
            context);
    }

    internal bool CanRetainRtpPacketSources(RtpPacket[] packets)
    {
        if (packets == null)
        {
            throw new RtpConfigurationException(
                "The inbound RTP packet array is required.");
        }

        List<uint> sourceIdentifiers = new List<uint>();
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            RtpPacket packet = packets[packetIndex];
            AppendUniqueSourceIdentifier(
                sourceIdentifiers,
                packet.SynchronizationSource);
            int sourceIndex = 0;
            while (sourceIndex < packet.ContributingSources.Length)
            {
                AppendUniqueSourceIdentifier(
                    sourceIdentifiers,
                    packet.ContributingSources[sourceIndex]);
                sourceIndex = sourceIndex + 1;
            }

            packetIndex = packetIndex + 1;
        }

        return remoteSourceTable.CanRetainRemoteSourceIdentifiers(
            sourceIdentifiers.ToArray());
    }

    internal bool CanRetainRtcpPacketSources(
        RtcpCompoundPacket compoundPacket)
    {
        List<uint> sourceIdentifiers = new List<uint>();
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            RtcpPacket packet = compoundPacket.Packets[packetIndex];
            if (packet.Type == RtcpPacketType.SourceDescription &&
                packet.SourceDescription != null)
            {
                int chunkIndex = 0;
                while (chunkIndex <
                    packet.SourceDescription.Chunks.Length)
                {
                    AppendUniqueSourceIdentifier(
                        sourceIdentifiers,
                        packet.SourceDescription.Chunks[chunkIndex]
                            .SourceIdentifier);
                    chunkIndex = chunkIndex + 1;
                }
            }
            else if (HasControlSenderSource(packet))
            {
                AppendUniqueSourceIdentifier(
                    sourceIdentifiers,
                    GetControlSenderSource(packet));
            }

            packetIndex = packetIndex + 1;
        }

        return remoteSourceTable.CanRetainRemoteSourceIdentifiers(
            sourceIdentifiers.ToArray());
    }

    internal void AppendUniqueSourceIdentifier(
        List<uint> sourceIdentifiers,
        uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sourceIdentifiers.Count)
        {
            if (sourceIdentifiers[sourceIndex] == sourceIdentifier)
            {
                return;
            }

            sourceIndex = sourceIndex + 1;
        }

        sourceIdentifiers.Add(sourceIdentifier);
    }

    internal RtpSessionEvent CreateCollisionIngressEvent(
        RtpSourceIdentityUpdate identityUpdate,
        RtpPacketKind packetKind,
        RtpPacketContext context,
        RtpPacket? packet,
        RtcpPacket? controlPacket)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Collision;
        sessionEvent.PacketKind = packetKind;
        sessionEvent.SourceIdentifier =
            identityUpdate.previousSourceIdentifier;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Source = identityUpdate.sourceSnapshot;
        if (packet != null)
        {
            sessionEvent.Packet = packet.Copy();
        }

        if (identityUpdate.loopDetected)
        {
            sessionEvent.Message =
                "An RTP transport loop was detected.";
        }
        else
        {
            sessionEvent.Message = "An RTP SSRC collision was detected.";
        }

        if (identityUpdate.goodbye != null)
        {
            RtcpPacket goodbyePacket = new RtcpPacket();
            goodbyePacket.Type = RtcpPacketType.Goodbye;
            goodbyePacket.Goodbye = identityUpdate.goodbye.Copy();
            sessionEvent.ControlPacket = goodbyePacket;
        }
        else if (controlPacket != null)
        {
            sessionEvent.ControlPacket = controlPacket.Copy();
        }

        return sessionEvent;
    }

    internal RtpSessionEvent CreateRtpSourceIngressEvent(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate,
        RtpJitterBufferUpdate? jitterUpdate,
        RtpSourceSnapshot sourceSnapshot)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Source;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SequenceStatus = sequenceUpdate.Status;
        sessionEvent.SourceStatus = sourceSnapshot.Status;
        sessionEvent.SourceIdentifier = packet.SynchronizationSource;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Packet = packet.Copy();
        sessionEvent.Source = sourceSnapshot;
        sessionEvent.SequenceUpdate = sequenceUpdate.Copy();
        sessionEvent.TimestampUpdate = sourceSnapshot.TimestampUpdate;
        if (jitterUpdate != null)
        {
            sessionEvent.JitterBufferStatus = jitterUpdate.Status;
            sessionEvent.JitterBufferUpdate = jitterUpdate.Copy();
        }

        return sessionEvent;
    }

    internal RtpSessionEvent CreateJitterLossIngressEvent(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot,
        RtpJitterBufferUpdate jitterUpdate)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Loss;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SourceIdentifier = packet.SynchronizationSource;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Source = sourceSnapshot;
        sessionEvent.JitterBufferStatus = jitterUpdate.Status;
        sessionEvent.JitterBufferUpdate = jitterUpdate.Copy();
        sessionEvent.Message = "One or more RTP packets were finalized as lost.";
        return sessionEvent;
    }

    internal RtpSessionEvent CreateJitterCapacityIngressEvent(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot,
        RtpJitterBufferUpdate jitterUpdate)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.RecoverableError;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SourceIdentifier = packet.SynchronizationSource;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Packet = packet.Copy();
        sessionEvent.Source = sourceSnapshot.Copy();
        sessionEvent.JitterBufferStatus =
            RtpJitterBufferStatus.Overflow;
        sessionEvent.JitterBufferUpdate = jitterUpdate.Copy();
        sessionEvent.Message =
            "The RTP jitter buffer source capacity has been reached.";
        return sessionEvent;
    }

    internal RtpSessionEvent CreateRtcpReportIngressEvent(
        RtcpPacket packet,
        RtpPacketContext context,
        RtpSourceSnapshot? sourceSnapshot)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Report;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.ControlPacket = packet.Copy();
        if (sourceSnapshot != null)
        {
            sessionEvent.SourceIdentifier =
                sourceSnapshot.SourceIdentifier;
            sessionEvent.SourceStatus = sourceSnapshot.Status;
            sessionEvent.Source = sourceSnapshot;
        }

        return sessionEvent;
    }

    internal RtpSessionEvent CreateRtcpSourceIngressEvent(
        RtcpPacket packet,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Report;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.SourceIdentifier = sourceSnapshot.SourceIdentifier;
        sessionEvent.SourceStatus = sourceSnapshot.Status;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.ControlPacket = packet.Copy();
        sessionEvent.Source = sourceSnapshot.Copy();
        return sessionEvent;
    }

    internal RtpSessionEvent CreateRtcpGoodbyeIngressEvent(
        RtcpPacket packet,
        RtpPacketContext context,
        uint sourceIdentifier,
        RtpSourceSnapshot? sourceSnapshot)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Goodbye;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.SourceIdentifier = sourceIdentifier;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.ControlPacket = packet.Copy();
        sessionEvent.Message = "An RTP source sent an RTCP goodbye.";
        if (sourceSnapshot != null)
        {
            sessionEvent.SourceStatus = sourceSnapshot.Status;
            sessionEvent.Source = sourceSnapshot.Copy();
        }

        return sessionEvent;
    }

    internal RtpSessionEvent CreateRtcpRepairIngressEvent(
        RtcpPacket packet,
        RtpPacketContext context,
        RtpRepairRequest request,
        RtpRepairResult? result)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.RepairKind = request.Kind;
        sessionEvent.SourceIdentifier = request.SourceIdentifier;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.ControlPacket = packet.Copy();
        sessionEvent.RepairRequest = request.Copy();
        if (result != null)
        {
            sessionEvent.RepairResult = result.Copy();
        }

        return sessionEvent;
    }

    internal bool HasControlSenderSource(RtcpPacket packet)
    {
        return (packet.Type == RtcpPacketType.SenderReport &&
            packet.SenderReport != null) ||
            (packet.Type == RtcpPacketType.ReceiverReport &&
            packet.ReceiverReport != null) ||
            (packet.Type == RtcpPacketType.ApplicationDefined &&
            packet.ApplicationDefined != null) ||
            (packet.Feedback != null) ||
            (packet.Type == RtcpPacketType.ExtendedReport &&
            packet.ExtendedReport != null);
    }

    internal uint GetControlSenderSource(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport &&
            packet.SenderReport != null)
        {
            return packet.SenderReport.SenderSynchronizationSource;
        }

        if (packet.Type == RtcpPacketType.ReceiverReport &&
            packet.ReceiverReport != null)
        {
            return packet.ReceiverReport.SenderSynchronizationSource;
        }

        if (packet.Type == RtcpPacketType.ApplicationDefined &&
            packet.ApplicationDefined != null)
        {
            return packet.ApplicationDefined.SourceIdentifier;
        }

        if (packet.Feedback != null)
        {
            return packet.Feedback.SenderSourceIdentifier;
        }

        if (packet.Type == RtcpPacketType.ExtendedReport &&
            packet.ExtendedReport != null)
        {
            return packet.ExtendedReport.SenderSynchronizationSource;
        }

        throw new RtpSessionException(
            "The RTCP packet has no sender source identifier.");
    }
}
