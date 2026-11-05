namespace rtp;

internal static class RtcpCompoundValidator
{
    internal static string? GetValidationError(
        RtcpCompoundPacket compoundPacket,
        RtcpCompoundMode compoundMode)
    {
        if (compoundMode == RtcpCompoundMode.Full)
        {
            return GetRfc3550ValidationError(compoundPacket);
        }

        if (compoundMode == RtcpCompoundMode.MinimalFeedback)
        {
            return GetMinimalFeedbackValidationError(compoundPacket);
        }

        if (compoundMode == RtcpCompoundMode.ReducedSize)
        {
            return GetFinalPaddingValidationError(compoundPacket);
        }

        return "The RTCP compound mode is not recognized.";
    }

    internal static string? GetRfc3550ValidationError(RtcpCompoundPacket compoundPacket)
    {
        if (compoundPacket.Packets.Length < 2)
        {
            return "An RFC 3550 compound packet must contain at least two packets.";
        }

        RtcpPacket firstPacket = compoundPacket.Packets[0];
        if (firstPacket.Type != RtcpPacketType.SenderReport &&
            firstPacket.Type != RtcpPacketType.ReceiverReport)
        {
            return "An RFC 3550 compound packet must begin with a sender or receiver report.";
        }

        bool hasCanonicalName = false;
        int phase = 0;
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            RtcpPacket packet = compoundPacket.Packets[packetIndex];
            if (packetIndex < compoundPacket.Packets.Length - 1 &&
                packet.Header.HasPadding)
            {
                return "Only the final packet in an RTCP compound packet may be padded.";
            }

            if (packetIndex > 0)
            {
                if (packet.Type == RtcpPacketType.ReceiverReport ||
                    packet.Type == RtcpPacketType.SenderReport)
                {
                    if (phase != 0)
                    {
                        return "Additional reports must precede source descriptions.";
                    }
                }
                else if (packet.Type == RtcpPacketType.SourceDescription)
                {
                    if (phase > 1)
                    {
                        return "Source descriptions must precede other RTCP packet types.";
                    }

                    phase = 1;
                }
                else if (packet.Type == RtcpPacketType.Goodbye)
                {
                    phase = 3;
                }
                else
                {
                    if (phase == 3)
                    {
                        return "RTCP goodbye packets must be at the end of a compound packet.";
                    }

                    phase = 2;
                }
            }

            if (packet.Type == RtcpPacketType.SourceDescription &&
                ContainsCanonicalName(packet.SourceDescription))
            {
                hasCanonicalName = true;
            }

            packetIndex = packetIndex + 1;
        }

        if (!hasCanonicalName)
        {
            return "An RFC 3550 compound packet must contain an SDES CNAME item.";
        }

        return null;
    }

    internal static string? GetMinimalFeedbackValidationError(
        RtcpCompoundPacket compoundPacket)
    {
        if (compoundPacket.Packets.Length < 3)
        {
            return "A minimal AVPF compound must contain a report, CNAME, and feedback.";
        }

        RtcpPacket reportPacket = compoundPacket.Packets[0];
        if (reportPacket.Type != RtcpPacketType.SenderReport &&
            reportPacket.Type != RtcpPacketType.ReceiverReport)
        {
            return "A minimal AVPF compound must begin with exactly one sender or receiver report.";
        }

        RtcpPacket descriptionPacket = compoundPacket.Packets[1];
        if (descriptionPacket.Type != RtcpPacketType.SourceDescription ||
            !ContainsOnlyCanonicalName(descriptionPacket.SourceDescription))
        {
            return "A minimal AVPF compound must contain exactly one CNAME-only SDES packet after its report.";
        }

        RtcpSourceDescription? description = descriptionPacket.SourceDescription;
        if (description == null)
        {
            return "A minimal AVPF compound must contain a source description.";
        }

        uint reportSource = 0;
        if (reportPacket.Type == RtcpPacketType.SenderReport)
        {
            RtcpSenderReport? senderReport = reportPacket.SenderReport;
            if (senderReport == null)
            {
                return "A minimal AVPF compound sender report is required.";
            }

            reportSource = senderReport.SenderSynchronizationSource;
        }
        else
        {
            RtcpReceiverReport? receiverReport = reportPacket.ReceiverReport;
            if (receiverReport == null)
            {
                return "A minimal AVPF compound receiver report is required.";
            }

            reportSource = receiverReport.SenderSynchronizationSource;
        }

        if (description.Chunks[0].SourceIdentifier != reportSource)
        {
            return "A minimal AVPF compound CNAME must identify the reporting source.";
        }

        int packetIndex = 2;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            RtcpPacket packet = compoundPacket.Packets[packetIndex];
            if (packet.Type != RtcpPacketType.TransportFeedback &&
                packet.Type != RtcpPacketType.PayloadFeedback)
            {
                return "A minimal AVPF compound may contain only feedback after its SDES packet.";
            }

            packetIndex = packetIndex + 1;
        }

        return GetFinalPaddingValidationError(compoundPacket);
    }

    internal static string? GetFinalPaddingValidationError(
        RtcpCompoundPacket compoundPacket)
    {
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length - 1)
        {
            if (compoundPacket.Packets[packetIndex].Header.HasPadding)
            {
                return "Only the final packet in an RTCP packet sequence may be padded.";
            }

            packetIndex = packetIndex + 1;
        }

        return null;
    }

    internal static bool ContainsCanonicalName(RtcpSourceDescription? description)
    {
        if (description == null)
        {
            return false;
        }

        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtcpSourceDescriptionChunk chunk = description.Chunks[chunkIndex];
            int itemIndex = 0;
            while (itemIndex < chunk.Items.Length)
            {
                if (chunk.Items[itemIndex].Type == RtcpSdesItemType.CanonicalName)
                {
                    return true;
                }

                itemIndex = itemIndex + 1;
            }

            chunkIndex = chunkIndex + 1;
        }

        return false;
    }

    internal static bool ContainsOnlyCanonicalName(RtcpSourceDescription? description)
    {
        if (description == null || description.Chunks.Length != 1)
        {
            return false;
        }

        RtcpSourceDescriptionChunk chunk = description.Chunks[0];
        return chunk.Items.Length == 1 &&
            chunk.Items[0].Type == RtcpSdesItemType.CanonicalName;
    }
}
