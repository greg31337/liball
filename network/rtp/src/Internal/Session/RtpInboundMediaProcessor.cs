using System.Collections.Generic;

namespace rtp;

internal class RtpInboundMediaProcessor
{
    internal object syncRoot = new object();
    internal IRtpPayloadDepacketizer payloadDepacketizer;
    internal List<RtpInboundMediaGroup> mediaGroups =
        new List<RtpInboundMediaGroup>();

    internal RtpInboundMediaProcessor(
        IRtpPayloadDepacketizer payloadDepacketizer)
    {
        if (payloadDepacketizer == null)
        {
            throw new RtpConfigurationException(
                "An RTP payload depacketizer is required.");
        }

        this.payloadDepacketizer = payloadDepacketizer;
    }

    internal RtpSessionEvent[] ProcessReleasedRtpPackets(
        RtpJitterBufferUpdate jitterUpdate,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot)
    {
        ValidateMediaInput(jitterUpdate, context, sourceSnapshot);
        List<RtpSessionEvent> events = new List<RtpSessionEvent>();
        bool hasLoss = jitterUpdate.LostSequenceNumbers.Length > 0;
        bool hasDiscontinuity = jitterUpdate.Status ==
            RtpJitterBufferStatus.Reset;
        if (jitterUpdate.ReleasedPackets.Length > 0 &&
            sourceSnapshot.TimestampUpdate != null &&
            sourceSnapshot.TimestampUpdate.IsDiscontinuity)
        {
            hasDiscontinuity = true;
        }

        if (hasLoss)
        {
            ClearMediaGroup(sourceSnapshot.SourceIdentifier);
            events.Add(CreateMediaIndicationEvent(
                RtpSessionEventType.Loss,
                "One or more RTP packets were lost before media assembly.",
                jitterUpdate,
                context,
                sourceSnapshot));
        }

        if (hasDiscontinuity)
        {
            ClearMediaGroup(sourceSnapshot.SourceIdentifier);
            events.Add(CreateMediaIndicationEvent(
                RtpSessionEventType.Source,
                "The RTP media timeline was reset after a discontinuity.",
                jitterUpdate,
                context,
                sourceSnapshot));
        }

        int packetIndex = 0;
        while (packetIndex < jitterUpdate.ReleasedPackets.Length)
        {
            RtpPacket packet = jitterUpdate.ReleasedPackets[packetIndex];
            if (packet.SynchronizationSource != sourceSnapshot.SourceIdentifier)
            {
                events.Add(CreateMediaIndicationEvent(
                    RtpSessionEventType.RecoverableError,
                    "A released RTP packet belongs to a different source.",
                    jitterUpdate,
                    context,
                    sourceSnapshot));
            }
            else
            {
                AppendReleasedPacket(
                    packet,
                    jitterUpdate,
                    context,
                    sourceSnapshot,
                    events);
            }

            packetIndex = packetIndex + 1;
        }

        return events.ToArray();
    }

    internal void AppendReleasedPacket(
        RtpPacket packet,
        RtpJitterBufferUpdate jitterUpdate,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot,
        List<RtpSessionEvent> events)
    {
        RtpPacket[] hookPackets = [];
        uint mediaTimestamp = packet.Timestamp;
        lock (syncRoot)
        {
            RtpInboundMediaGroup group = GetOrCreateMediaGroup(
                packet.SynchronizationSource,
                packet.Timestamp);
            if (group.packets.Count == 0)
            {
                group.timestamp = packet.Timestamp;
            }
            else if (group.timestamp != packet.Timestamp)
            {
                events.Add(CreateMediaIndicationEvent(
                    RtpSessionEventType.Source,
                    "The RTP timestamp changed before the previous media unit completed.",
                    jitterUpdate,
                    context,
                    sourceSnapshot));
                group.packets.Clear();
                group.timestamp = packet.Timestamp;
            }

            group.packets.Add(packet.Copy());
            if (packet.Marker)
            {
                hookPackets = CopyMediaGroupPackets(group);
                mediaTimestamp = group.timestamp;
                group.packets.Clear();
            }
        }

        if (!packet.Marker)
        {
            return;
        }

        RtpDepacketizationResult result =
            payloadDepacketizer.DepacketizeRtpPayload(hookPackets);
        string? resultError = GetDepacketizationResultError(
            result,
            hookPackets.Length);
        if (resultError != null)
        {
            events.Add(CreateDepacketizationEvent(
                RtpSessionEventType.RecoverableError,
                resultError,
                jitterUpdate,
                context,
                sourceSnapshot,
                packet,
                CreateSafeDepacketizationResultCopy(result)));
            return;
        }

        RtpDepacketizationResult ownedResult = result.Copy();
        if (!ownedResult.Success)
        {
            string failureMessage =
                "The RTP payload depacketizer did not produce media.";
            if (ownedResult.ErrorMessage != null &&
                ownedResult.ErrorMessage.Length > 0)
            {
                failureMessage = ownedResult.ErrorMessage;
            }

            events.Add(CreateDepacketizationEvent(
                RtpSessionEventType.RecoverableError,
                failureMessage,
                jitterUpdate,
                context,
                sourceSnapshot,
                packet,
                ownedResult));
            return;
        }

        ownedResult.Timestamp = mediaTimestamp;
        ownedResult.Marker = packet.Marker;
        events.Add(CreateDepacketizationEvent(
            RtpSessionEventType.Source,
            "",
            jitterUpdate,
            context,
            sourceSnapshot,
            packet,
            ownedResult));
    }

    internal RtpInboundMediaGroup GetOrCreateMediaGroup(
        uint sourceIdentifier,
        uint timestamp)
    {
        lock (syncRoot)
        {
            int groupIndex = FindMediaGroupIndex(sourceIdentifier);
            if (groupIndex >= 0)
            {
                return mediaGroups[groupIndex];
            }

            RtpInboundMediaGroup group = new RtpInboundMediaGroup();
            group.sourceIdentifier = sourceIdentifier;
            group.timestamp = timestamp;
            mediaGroups.Add(group);
            return group;
        }
    }

    internal void ClearMediaGroup(uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            int groupIndex = FindMediaGroupIndex(sourceIdentifier);
            if (groupIndex >= 0)
            {
                mediaGroups[groupIndex].packets.Clear();
            }
        }
    }

    internal int FindMediaGroupIndex(uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            int groupIndex = 0;
            while (groupIndex < mediaGroups.Count)
            {
                if (mediaGroups[groupIndex].sourceIdentifier == sourceIdentifier)
                {
                    return groupIndex;
                }

                groupIndex = groupIndex + 1;
            }

            return -1;
        }
    }

    internal static RtpPacket[] CopyMediaGroupPackets(
        RtpInboundMediaGroup group)
    {
        RtpPacket[] copies = new RtpPacket[group.packets.Count];
        int packetIndex = 0;
        while (packetIndex < group.packets.Count)
        {
            copies[packetIndex] = group.packets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        return copies;
    }

    internal static string? GetDepacketizationResultError(
        RtpDepacketizationResult result,
        int packetCount)
    {
        if (result == null)
        {
            return "The RTP payload depacketizer returned no result.";
        }

        if (result.ConsumedPacketCount < 0 ||
            result.ConsumedPacketCount > packetCount)
        {
            return "The RTP payload depacketizer returned an invalid consumed packet count.";
        }

        if (result.Payload == null)
        {
            return "The RTP payload depacketizer returned no media payload array.";
        }

        if (result.Success && result.ConsumedPacketCount != packetCount)
        {
            return "The RTP payload depacketizer did not consume the complete media group.";
        }

        return null;
    }

    internal static RtpDepacketizationResult?
        CreateSafeDepacketizationResultCopy(
            RtpDepacketizationResult result)
    {
        if (result == null || result.Payload == null)
        {
            return null;
        }

        return result.Copy();
    }

    internal static void ValidateMediaInput(
        RtpJitterBufferUpdate jitterUpdate,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot)
    {
        if (jitterUpdate == null)
        {
            throw new RtpConfigurationException(
                "An RTP jitter-buffer update is required for media assembly.");
        }

        if (context == null)
        {
            throw new RtpConfigurationException(
                "An RTP packet context is required for media assembly.");
        }

        if (sourceSnapshot == null)
        {
            throw new RtpConfigurationException(
                "An RTP source snapshot is required for media assembly.");
        }

        if (jitterUpdate.ReleasedPackets == null ||
            jitterUpdate.LostSequenceNumbers == null)
        {
            throw new RtpConfigurationException(
                "The RTP jitter-buffer media arrays are required.");
        }
    }

    internal static RtpSessionEvent CreateMediaIndicationEvent(
        RtpSessionEventType eventType,
        string message,
        RtpJitterBufferUpdate jitterUpdate,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = eventType;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.SourceIdentifier = sourceSnapshot.SourceIdentifier;
        sessionEvent.SourceStatus = sourceSnapshot.Status;
        sessionEvent.JitterBufferStatus = jitterUpdate.Status;
        sessionEvent.Message = message;
        sessionEvent.PacketContext = context.Copy();
        sessionEvent.Source = sourceSnapshot.Copy();
        sessionEvent.JitterBufferUpdate = jitterUpdate.Copy();
        if (sourceSnapshot.TimestampUpdate != null)
        {
            sessionEvent.TimestampUpdate =
                sourceSnapshot.TimestampUpdate.Copy();
        }

        return sessionEvent;
    }

    internal static RtpSessionEvent CreateDepacketizationEvent(
        RtpSessionEventType eventType,
        string message,
        RtpJitterBufferUpdate jitterUpdate,
        RtpPacketContext context,
        RtpSourceSnapshot sourceSnapshot,
        RtpPacket packet,
        RtpDepacketizationResult? result)
    {
        RtpSessionEvent sessionEvent = CreateMediaIndicationEvent(
            eventType,
            message,
            jitterUpdate,
            context,
            sourceSnapshot);
        sessionEvent.Packet = packet.Copy();
        if (result != null)
        {
            sessionEvent.DepacketizationResult = result.Copy();
        }

        return sessionEvent;
    }
}
