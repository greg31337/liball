namespace rtp;

internal static class RtcpFeedbackSemanticComparer
{
    internal static bool IsFeedbackSuperset(
        RtcpFeedbackPacket received,
        RtcpFeedbackPacket requested)
    {
        if (!HasMatchingFeedbackEnvelope(received, requested))
        {
            return false;
        }

        byte format = RtcpFeedbackWireCodec.GetFeedbackWireFormat(requested);
        if (requested.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            format == RtcpFeedbackFormats.TransportGenericNack)
        {
            return IsGenericNackSuperset(received, requested);
        }

        if (requested.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            format == RtcpFeedbackFormats.PayloadSliceLossIndication)
        {
            return IsSliceLossSuperset(received, requested);
        }

        if (requested.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            format == RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            return IsFullIntraRequestSuperset(received, requested);
        }

        if (!HasUnderstoodFeedbackSemantics(requested, format))
        {
            return false;
        }

        return HasSameEncodedFeedback(received, requested);
    }

    internal static bool CanCoalesceFeedback(
        RtcpFeedbackPacket first,
        RtcpFeedbackPacket second)
    {
        if (!HasMatchingFeedbackEnvelope(first, second))
        {
            return false;
        }

        byte format = RtcpFeedbackWireCodec.GetFeedbackWireFormat(first);
        if (first.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            format == RtcpFeedbackFormats.TransportGenericNack)
        {
            return first.GenericNack != null && second.GenericNack != null;
        }

        return HasSameEncodedFeedback(first, second);
    }

    internal static RtcpFeedbackPacket MergeCoalescedFeedback(
        RtcpFeedbackPacket first,
        RtcpFeedbackPacket second)
    {
        RtcpFeedbackPacket merged = first.Copy();
        byte format = RtcpFeedbackWireCodec.GetFeedbackWireFormat(first);
        if (first.PacketType != RtcpFeedbackPacketType.TransportLayer ||
            format != RtcpFeedbackFormats.TransportGenericNack)
        {
            return merged;
        }

        ushort[] firstSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                RtcpFeedbackWireCodec.GetRequiredGenericNack(first));
        ushort[] secondSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                RtcpFeedbackWireCodec.GetRequiredGenericNack(second));
        bool[] included = new bool[RtpProtocolConstants.SequenceNumberModulus];
        int sequenceCount = 0;
        int sequenceIndex = 0;
        while (sequenceIndex < firstSequenceNumbers.Length)
        {
            int sequenceNumber = firstSequenceNumbers[sequenceIndex];
            if (!included[sequenceNumber])
            {
                included[sequenceNumber] = true;
                sequenceCount = sequenceCount + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        sequenceIndex = 0;
        while (sequenceIndex < secondSequenceNumbers.Length)
        {
            int sequenceNumber = secondSequenceNumbers[sequenceIndex];
            if (!included[sequenceNumber])
            {
                included[sequenceNumber] = true;
                sequenceCount = sequenceCount + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        ushort[] combinedSequenceNumbers = new ushort[sequenceCount];
        int combinedIndex = 0;
        int candidate = 0;
        while (candidate < included.Length)
        {
            if (included[candidate])
            {
                combinedSequenceNumbers[combinedIndex] = (ushort)candidate;
                combinedIndex = combinedIndex + 1;
            }

            candidate = candidate + 1;
        }

        merged.GenericNack =
            RtcpGenericNackWireCodec.PackLostSequenceNumbers(
                combinedSequenceNumbers);
        return merged;
    }

    internal static bool HasMatchingFeedbackEnvelope(
        RtcpFeedbackPacket first,
        RtcpFeedbackPacket second)
    {
        return first.PacketType == second.PacketType &&
            first.MediaSourceIdentifier == second.MediaSourceIdentifier &&
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(first) ==
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(second);
    }

    internal static bool HasUnderstoodFeedbackSemantics(
        RtcpFeedbackPacket feedback,
        byte format)
    {
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer)
        {
            return format == RtcpFeedbackFormats.TransportGenericNack ||
                format == RtcpFeedbackFormats.
                    TransportTemporaryMaximumBitrateRequest ||
                format == RtcpFeedbackFormats.
                    TransportTemporaryMaximumBitrateNotification ||
                format == RtcpFeedbackFormats.
                    TransportRapidResynchronizationRequest ||
                format == RtcpFeedbackFormats.
                    TransportCongestionControlFeedback;
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific)
        {
            return format == RtcpFeedbackFormats.PayloadPictureLossIndication ||
                format == RtcpFeedbackFormats.PayloadSliceLossIndication ||
                format == RtcpFeedbackFormats.
                    PayloadReferencePictureSelectionIndication ||
                format == RtcpFeedbackFormats.PayloadFullIntraRequest ||
                format == RtcpFeedbackFormats.
                    PayloadTemporalSpatialTradeoffRequest ||
                format == RtcpFeedbackFormats.
                    PayloadTemporalSpatialTradeoffNotification;
        }

        return false;
    }

    internal static bool HasSameEncodedFeedback(
        RtcpFeedbackPacket first,
        RtcpFeedbackPacket second)
    {
        if (!HasMatchingFeedbackEnvelope(first, second))
        {
            return false;
        }

        long firstSize = RtcpFeedbackWireCodec.CalculateFeedbackBodySize(first);
        long secondSize = RtcpFeedbackWireCodec.CalculateFeedbackBodySize(second);
        if (firstSize != secondSize || firstSize > int.MaxValue)
        {
            return false;
        }

        byte[] firstBytes = new byte[(int)firstSize];
        byte[] secondBytes = new byte[(int)secondSize];
        RtcpFeedbackWireCodec.EncodeFeedback(first, firstBytes);
        RtcpFeedbackWireCodec.EncodeFeedback(second, secondBytes);
        int byteIndex = RtcpProtocolConstants.SourceIdentifierSize;
        while (byteIndex < firstBytes.Length)
        {
            if (firstBytes[byteIndex] != secondBytes[byteIndex])
            {
                return false;
            }

            byteIndex = byteIndex + 1;
        }

        return true;
    }

    internal static bool IsGenericNackSuperset(
        RtcpFeedbackPacket received,
        RtcpFeedbackPacket requested)
    {
        if (received.GenericNack == null || requested.GenericNack == null)
        {
            return false;
        }

        ushort[] receivedSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                received.GenericNack);
        ushort[] requestedSequenceNumbers =
            RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
                requested.GenericNack);
        bool[] receivedSet =
            new bool[RtpProtocolConstants.SequenceNumberModulus];
        int sequenceIndex = 0;
        while (sequenceIndex < receivedSequenceNumbers.Length)
        {
            receivedSet[receivedSequenceNumbers[sequenceIndex]] = true;
            sequenceIndex = sequenceIndex + 1;
        }

        sequenceIndex = 0;
        while (sequenceIndex < requestedSequenceNumbers.Length)
        {
            if (!receivedSet[requestedSequenceNumbers[sequenceIndex]])
            {
                return false;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return true;
    }

    internal static bool IsSliceLossSuperset(
        RtcpFeedbackPacket received,
        RtcpFeedbackPacket requested)
    {
        if (received.SliceLossIndication == null ||
            requested.SliceLossIndication == null)
        {
            return false;
        }

        RtcpSliceLossEntry[] receivedEntries =
            received.SliceLossIndication.Entries;
        RtcpSliceLossEntry[] requestedEntries =
            requested.SliceLossIndication.Entries;
        int requestedIndex = 0;
        while (requestedIndex < requestedEntries.Length)
        {
            RtcpSliceLossEntry requestedEntry = requestedEntries[requestedIndex];
            bool found = false;
            int receivedIndex = 0;
            while (receivedIndex < receivedEntries.Length)
            {
                RtcpSliceLossEntry receivedEntry = receivedEntries[receivedIndex];
                if (receivedEntry.FirstMacroblock == requestedEntry.FirstMacroblock &&
                    receivedEntry.MacroblockCount == requestedEntry.MacroblockCount &&
                    receivedEntry.PictureIdentifier == requestedEntry.PictureIdentifier)
                {
                    found = true;
                }

                receivedIndex = receivedIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            requestedIndex = requestedIndex + 1;
        }

        return true;
    }

    internal static bool IsFullIntraRequestSuperset(
        RtcpFeedbackPacket received,
        RtcpFeedbackPacket requested)
    {
        if (received.FullIntraRequest == null ||
            requested.FullIntraRequest == null)
        {
            return false;
        }

        RtcpFullIntraRequestEntry[] receivedEntries =
            received.FullIntraRequest.Entries;
        RtcpFullIntraRequestEntry[] requestedEntries =
            requested.FullIntraRequest.Entries;
        int requestedIndex = 0;
        while (requestedIndex < requestedEntries.Length)
        {
            uint requestedSourceIdentifier =
                requestedEntries[requestedIndex].SourceIdentifier;
            bool found = false;
            int receivedIndex = 0;
            while (receivedIndex < receivedEntries.Length)
            {
                if (receivedEntries[receivedIndex].SourceIdentifier ==
                    requestedSourceIdentifier)
                {
                    found = true;
                }

                receivedIndex = receivedIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            requestedIndex = requestedIndex + 1;
        }

        return true;
    }
}
