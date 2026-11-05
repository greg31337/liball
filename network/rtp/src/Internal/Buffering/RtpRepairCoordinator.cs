using System;

namespace rtp;

internal class RtpRepairCoordinator
{
    internal const int MaximumPacketSize = 65535;

    internal IRtpRepairEncoder? repairEncoder;
    internal IRtpRepairDecoder? repairDecoder;

    internal RtpRepairCoordinator(
        IRtpRepairEncoder? repairEncoder,
        IRtpRepairDecoder? repairDecoder)
    {
        this.repairEncoder = repairEncoder;
        this.repairDecoder = repairDecoder;
    }

    internal RtpRepairResult EncodeRepair(
        RtpRepairRequest request,
        RtpPacket[] sourcePackets)
    {
        RtpRepairRequest requestCopy = CopyValidatedRequest(request);
        RtpPacket[] packetCopies = CopyValidatedSourcePackets(sourcePackets);
        if (repairEncoder == null)
        {
            return CreateUnavailableEncoderResult(requestCopy);
        }

        RtpRepairResult result = repairEncoder.EncodeRtpRepair(
            requestCopy,
            packetCopies);
        return CopyValidatedResult(result, request.Kind);
    }

    internal RtpRepairResult DecodeRepair(
        RtpRepairRequest request,
        ReadOnlySpan<byte> repairData)
    {
        RtpRepairRequest requestCopy = CopyValidatedRequest(request);
        if (repairDecoder == null)
        {
            return CreateUnavailableDecoderResult(requestCopy);
        }

        RtpRepairResult result = repairDecoder.DecodeRtpRepair(
            requestCopy,
            repairData);
        return CopyValidatedResult(result, request.Kind);
    }

    internal RtpSessionEvent ProcessRepairedPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpRepairKind repairKind,
        IRtpSequenceTracker sequenceTracker,
        IRtpReceptionStatistics receptionStatistics,
        IRtpJitterBuffer jitterBuffer)
    {
        ValidateRepairKind(repairKind);
        ValidateRepairedPacketDependencies(
            sequenceTracker,
            receptionStatistics,
            jitterBuffer);
        ValidateRepairedPacketInput(packet, context);

        RtpPacket packetCopy = packet.Copy();
        RtpPacketContext contextCopy = context.Copy();
        contextCopy.IsRepaired = true;
        RtpSequenceUpdate sequenceUpdate =
            sequenceTracker.ProcessRtpSequenceNumber(
                packetCopy.SequenceNumber);
        ValidateRepairedSequenceUpdate(packetCopy, sequenceUpdate);

        RtpJitterBufferUpdate? jitterBufferUpdate = null;
        if (IsRecordableRepairedSequence(sequenceUpdate))
        {
            jitterBufferUpdate = jitterBuffer.TryAddRtpJitterBufferPacket(
                packetCopy,
                contextCopy,
                sequenceUpdate);
            if (jitterBufferUpdate.Status == RtpJitterBufferStatus.Late)
            {
                contextCopy.IsLate = true;
            }

            receptionStatistics.RecordReceivedRtpPacket(
                packetCopy,
                contextCopy,
                sequenceUpdate);
        }
        else
        {
            receptionStatistics.RecordDiscardedRtpPacket(contextCopy);
        }

        return CreateRepairedPacketEvent(
            packetCopy,
            contextCopy,
            repairKind,
            sequenceUpdate,
            jitterBufferUpdate);
    }

    internal RtpRepairRequest CopyValidatedRequest(RtpRepairRequest request)
    {
        if (request == null)
        {
            throw new RtpConfigurationException(
                "An RTP repair request is required.");
        }

        ValidateRepairKind(request.Kind);
        if (request.SequenceNumbers == null || request.Data == null)
        {
            throw new RtpConfigurationException(
                "RTP repair request arrays are required.");
        }

        if (request.AttemptCount < 0)
        {
            throw new RtpConfigurationException(
                "The RTP repair attempt count cannot be negative.");
        }

        if (request.RequestTimeTicks < 0 || request.DeadlineTicks < 0)
        {
            throw new RtpConfigurationException(
                "RTP repair times cannot be negative.");
        }

        if (request.DeadlineTicks < request.RequestTimeTicks)
        {
            throw new RtpConfigurationException(
                "The RTP repair deadline cannot precede its request time.");
        }

        return request.Copy();
    }

    internal RtpPacket[] CopyValidatedSourcePackets(RtpPacket[] sourcePackets)
    {
        if (sourcePackets == null)
        {
            throw new RtpConfigurationException(
                "RTP repair source packets are required.");
        }

        RtpPacket[] copies = new RtpPacket[sourcePackets.Length];
        int packetIndex = 0;
        while (packetIndex < sourcePackets.Length)
        {
            RtpPacketEncoder.GetEncodedSize(
                sourcePackets[packetIndex],
                false,
                MaximumPacketSize);
            copies[packetIndex] = sourcePackets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        return copies;
    }

    internal RtpRepairResult CopyValidatedResult(
        RtpRepairResult result,
        RtpRepairKind requestedKind)
    {
        if (result == null)
        {
            throw new RtpSessionException(
                "The RTP repair hook returned no result.");
        }

        ValidateRepairKind(result.Kind);
        if (result.Kind != requestedKind)
        {
            throw new RtpSessionException(
                "The RTP repair result kind does not match its request.");
        }

        if (result.RepairedPackets == null ||
            result.UnrepairedSequenceNumbers == null ||
            result.Data == null)
        {
            throw new RtpSessionException(
                "RTP repair result arrays are required.");
        }

        int packetIndex = 0;
        while (packetIndex < result.RepairedPackets.Length)
        {
            RtpPacketEncoder.GetEncodedSize(
                result.RepairedPackets[packetIndex],
                false,
                MaximumPacketSize);
            packetIndex = packetIndex + 1;
        }

        return result.Copy();
    }

    internal void ValidateRepairedPacketDependencies(
        IRtpSequenceTracker sequenceTracker,
        IRtpReceptionStatistics receptionStatistics,
        IRtpJitterBuffer jitterBuffer)
    {
        if (sequenceTracker == null ||
            receptionStatistics == null ||
            jitterBuffer == null)
        {
            throw new RtpConfigurationException(
                "RTP repaired-packet processing dependencies are required.");
        }
    }

    internal void ValidateRepairedPacketInput(
        RtpPacket packet,
        RtpPacketContext context)
    {
        RtpPacketEncoder.GetEncodedSize(
            packet,
            true,
            MaximumPacketSize);
        if (context == null)
        {
            throw new RtpConfigurationException(
                "An RTP repaired-packet context is required.");
        }

        if (context.ArrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The repaired RTP packet arrival time cannot be negative.");
        }

        if (context.ClockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The repaired RTP packet clock rate must be positive.");
        }
    }

    internal void ValidateRepairedSequenceUpdate(
        RtpPacket packet,
        RtpSequenceUpdate sequenceUpdate)
    {
        if (sequenceUpdate == null ||
            sequenceUpdate.SequenceNumber != packet.SequenceNumber)
        {
            throw new RtpSessionException(
                "RTP sequence processing did not identify the repaired packet.");
        }
    }

    internal bool IsRecordableRepairedSequence(
        RtpSequenceUpdate sequenceUpdate)
    {
        return sequenceUpdate.Status == RtpSequenceStatus.Accepted ||
            sequenceUpdate.Status == RtpSequenceStatus.Duplicate ||
            sequenceUpdate.Status == RtpSequenceStatus.Reordered ||
            sequenceUpdate.Status == RtpSequenceStatus.Restarted;
    }

    internal RtpSessionEvent CreateRepairedPacketEvent(
        RtpPacket packet,
        RtpPacketContext context,
        RtpRepairKind repairKind,
        RtpSequenceUpdate sequenceUpdate,
        RtpJitterBufferUpdate? jitterBufferUpdate)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.RepairKind = repairKind;
        sessionEvent.SourceIdentifier = packet.SynchronizationSource;
        sessionEvent.SequenceStatus = sequenceUpdate.Status;
        sessionEvent.Packet = packet.Copy();
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.SequenceUpdate = sequenceUpdate.Copy();
        if (jitterBufferUpdate != null)
        {
            sessionEvent.JitterBufferStatus = jitterBufferUpdate.Status;
            sessionEvent.JitterBufferUpdate = jitterBufferUpdate.Copy();
        }
        else
        {
            sessionEvent.Message =
                "The repaired RTP packet was discarded by sequence processing.";
        }

        return sessionEvent;
    }

    internal RtpRepairResult CreateUnavailableEncoderResult(
        RtpRepairRequest request)
    {
        RtpRepairResult result = CreateUnavailableResult(request);
        result.ErrorMessage = "No RTP repair encoder is available.";
        return result;
    }

    internal RtpRepairResult CreateUnavailableDecoderResult(
        RtpRepairRequest request)
    {
        RtpRepairResult result = CreateUnavailableResult(request);
        result.ErrorMessage = "No RTP repair decoder is available.";
        return result;
    }

    internal RtpRepairResult CreateUnavailableResult(
        RtpRepairRequest request)
    {
        RtpRepairResult result = new RtpRepairResult();
        result.Kind = request.Kind;
        result.UnrepairedSequenceNumbers =
            (ushort[])request.SequenceNumbers.Clone();
        return result;
    }

    internal void ValidateRepairKind(RtpRepairKind kind)
    {
        switch (kind)
        {
            case RtpRepairKind.Retransmission:
                break;
            case RtpRepairKind.Redundancy:
                break;
            case RtpRepairKind.ForwardErrorCorrection:
                break;
            case RtpRepairKind.ApplicationDefined:
                break;
            default:
                throw new RtpConfigurationException(
                    "The RTP repair kind is not supported.");
        }
    }
}
