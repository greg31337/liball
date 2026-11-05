using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtcpGenericNackWireCodec
{
    internal static RtcpGenericNack PackLostSequenceNumbers(
        ushort[] lostSequenceNumbers)
    {
        if (lostSequenceNumbers == null || lostSequenceNumbers.Length == 0)
        {
            throw new RtcpPacketFormatException(
                "At least one lost RTP sequence number is required for Generic NACK.");
        }

        bool[] lost = new bool[RtpProtocolConstants.SequenceNumberModulus];
        int lostCount = 0;
        int sequenceIndex = 0;
        while (sequenceIndex < lostSequenceNumbers.Length)
        {
            int sequenceNumber = lostSequenceNumbers[sequenceIndex];
            if (!lost[sequenceNumber])
            {
                lost[sequenceNumber] = true;
                lostCount = lostCount + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        ushort[] ordered = CreateOrderedSequenceNumbers(lost, lostCount);
        return CreateCanonicalGenericNack(ordered);
    }

    internal static ushort[] ExpandLostSequenceNumbers(RtcpGenericNack genericNack)
    {
        string? validationError = GetGenericNackValidationError(genericNack);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        bool[] lost = new bool[RtpProtocolConstants.SequenceNumberModulus];
        int lostCount = 0;
        int entryIndex = 0;
        while (entryIndex < genericNack.Entries.Length)
        {
            RtcpGenericNackEntry entry = genericNack.Entries[entryIndex];
            int packetIdentifier = entry.PacketIdentifier;
            if (!lost[packetIdentifier])
            {
                lost[packetIdentifier] = true;
                lostCount = lostCount + 1;
            }

            int bitIndex = 0;
            while (bitIndex < 16)
            {
                if ((entry.LostPacketBitmask & (1U << bitIndex)) != 0)
                {
                    int sequenceNumber = (packetIdentifier + bitIndex + 1) & 65535;
                    if (!lost[sequenceNumber])
                    {
                        lost[sequenceNumber] = true;
                        lostCount = lostCount + 1;
                    }
                }

                bitIndex = bitIndex + 1;
            }

            entryIndex = entryIndex + 1;
        }

        return CreateOrderedSequenceNumbers(lost, lostCount);
    }

    internal static string? GetGenericNackValidationError(RtcpGenericNack? genericNack)
    {
        if (genericNack == null)
        {
            return "The Generic NACK body is required.";
        }

        if (genericNack.Entries == null || genericNack.Entries.Length == 0)
        {
            return "Generic NACK must contain at least one PID and BLP entry.";
        }

        int entryIndex = 0;
        while (entryIndex < genericNack.Entries.Length)
        {
            if (genericNack.Entries[entryIndex] == null)
            {
                return "A Generic NACK PID and BLP entry is required.";
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static string? GetGenericNackDecodeError(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
        {
            return "Generic NACK feedback must contain at least one PID and BLP entry.";
        }

        if (source.Length % 4 != 0)
        {
            return "Generic NACK feedback must contain complete four-byte entries.";
        }

        return null;
    }

    internal static int CalculateGenericNackSize(RtcpGenericNack genericNack)
    {
        RtcpGenericNack canonical = CanonicalizeGenericNack(genericNack);
        return canonical.Entries.Length * 4;
    }

    internal static RtcpGenericNack DecodeGenericNack(ReadOnlySpan<byte> source)
    {
        RtcpGenericNack decoded = new RtcpGenericNack();
        decoded.Entries = new RtcpGenericNackEntry[source.Length / 4];
        int entryIndex = 0;
        while (entryIndex < decoded.Entries.Length)
        {
            int offset = entryIndex * 4;
            RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
            entry.PacketIdentifier = NetworkBinaryReader.ReadUInt16(source, offset);
            entry.LostPacketBitmask = NetworkBinaryReader.ReadUInt16(source, offset + 2);
            decoded.Entries[entryIndex] = entry;
            entryIndex = entryIndex + 1;
        }

        return CanonicalizeGenericNack(decoded);
    }

    internal static void EncodeGenericNack(
        RtcpGenericNack genericNack,
        Span<byte> destination)
    {
        RtcpGenericNack canonical = CanonicalizeGenericNack(genericNack);
        int entryIndex = 0;
        while (entryIndex < canonical.Entries.Length)
        {
            int offset = entryIndex * 4;
            RtcpGenericNackEntry entry = canonical.Entries[entryIndex];
            NetworkBinaryWriter.WriteUInt16(
                destination,
                offset,
                entry.PacketIdentifier);
            NetworkBinaryWriter.WriteUInt16(
                destination,
                offset + 2,
                entry.LostPacketBitmask);
            entryIndex = entryIndex + 1;
        }
    }

    internal static RtcpGenericNack CanonicalizeGenericNack(
        RtcpGenericNack genericNack)
    {
        ushort[] sequenceNumbers = ExpandLostSequenceNumbers(genericNack);
        return PackLostSequenceNumbers(sequenceNumbers);
    }

    internal static ushort[] CreateOrderedSequenceNumbers(bool[] lost, int lostCount)
    {
        ushort[] numericOrder = new ushort[lostCount];
        int numericIndex = 0;
        int sequenceNumber = 0;
        while (sequenceNumber < lost.Length)
        {
            if (lost[sequenceNumber])
            {
                numericOrder[numericIndex] = (ushort)sequenceNumber;
                numericIndex = numericIndex + 1;
            }

            sequenceNumber = sequenceNumber + 1;
        }

        if (numericOrder.Length < 2)
        {
            return numericOrder;
        }

        int startIndex = 0;
        int largestGap = 0;
        int index = 0;
        while (index < numericOrder.Length)
        {
            int nextIndex = (index + 1) % numericOrder.Length;
            int gap = (numericOrder[nextIndex] - numericOrder[index] +
                RtpProtocolConstants.SequenceNumberModulus) & 65535;
            if (gap > largestGap ||
                (gap == largestGap &&
                numericOrder[nextIndex] < numericOrder[startIndex]))
            {
                largestGap = gap;
                startIndex = nextIndex;
            }

            index = index + 1;
        }

        ushort[] ordered = new ushort[numericOrder.Length];
        index = 0;
        while (index < ordered.Length)
        {
            ordered[index] = numericOrder[(startIndex + index) % numericOrder.Length];
            index = index + 1;
        }

        return ordered;
    }

    internal static RtcpGenericNack CreateCanonicalGenericNack(
        ushort[] orderedSequenceNumbers)
    {
        List<RtcpGenericNackEntry> entries = new List<RtcpGenericNackEntry>();
        int sequenceIndex = 0;
        while (sequenceIndex < orderedSequenceNumbers.Length)
        {
            int packetIdentifier = orderedSequenceNumbers[sequenceIndex];
            ushort bitmask = 0;
            sequenceIndex = sequenceIndex + 1;
            while (sequenceIndex < orderedSequenceNumbers.Length)
            {
                int distance = (orderedSequenceNumbers[sequenceIndex] - packetIdentifier +
                    RtpProtocolConstants.SequenceNumberModulus) & 65535;
                if (distance > 16)
                {
                    break;
                }

                bitmask = (ushort)(bitmask | (1U << (distance - 1)));
                sequenceIndex = sequenceIndex + 1;
            }

            RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
            entry.PacketIdentifier = (ushort)packetIdentifier;
            entry.LostPacketBitmask = bitmask;
            entries.Add(entry);
        }

        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = entries.ToArray();
        return genericNack;
    }
}
