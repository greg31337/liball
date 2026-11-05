using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtcpFullIntraRequestWireCodec
{
    const int EntrySize = 8;

    internal static string? GetFullIntraRequestValidationError(
        RtcpFullIntraRequest? fullIntraRequest,
        uint mediaSourceIdentifier)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of a full intra request must be zero.";
        }

        if (fullIntraRequest == null)
        {
            return "The full intra request body is required.";
        }

        if (fullIntraRequest.Entries == null ||
            fullIntraRequest.Entries.Length == 0)
        {
            return "A full intra request must contain at least one entry.";
        }

        HashSet<uint> sourceIdentifiers = new HashSet<uint>();
        int entryIndex = 0;
        while (entryIndex < fullIntraRequest.Entries.Length)
        {
            RtcpFullIntraRequestEntry? entry =
                fullIntraRequest.Entries[entryIndex];
            if (entry == null)
            {
                return "A full intra request entry is required.";
            }

            if (!sourceIdentifiers.Add(entry.SourceIdentifier))
            {
                return "A full intra request cannot repeat a target SSRC.";
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static string? GetFullIntraRequestDecodeError(
        ReadOnlySpan<byte> source,
        uint mediaSourceIdentifier,
        int lengthInWordsMinusOne)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of a full intra request must be zero.";
        }

        if (source.Length == 0)
        {
            return "A full intra request must contain at least one entry.";
        }

        if (source.Length % EntrySize != 0)
        {
            return "A full intra request must contain complete eight-byte entries.";
        }

        int expectedLength = 2 + source.Length /
            RtcpProtocolConstants.LengthWordSize;
        if (lengthInWordsMinusOne != expectedLength)
        {
            return "A full intra request length must be 2 plus two words per entry.";
        }

        HashSet<uint> sourceIdentifiers = new HashSet<uint>();
        int offset = 0;
        while (offset < source.Length)
        {
            uint sourceIdentifier = NetworkBinaryReader.ReadUInt32(source, offset);
            if (!sourceIdentifiers.Add(sourceIdentifier))
            {
                return "A full intra request cannot repeat a target SSRC.";
            }

            offset = offset + EntrySize;
        }

        return null;
    }

    internal static long CalculateFullIntraRequestSize(
        RtcpFullIntraRequest fullIntraRequest)
    {
        return (long)fullIntraRequest.Entries.Length * EntrySize;
    }

    internal static RtcpFullIntraRequest DecodeFullIntraRequest(
        ReadOnlySpan<byte> source)
    {
        RtcpFullIntraRequest fullIntraRequest = new RtcpFullIntraRequest();
        fullIntraRequest.Entries =
            new RtcpFullIntraRequestEntry[source.Length / EntrySize];
        int entryIndex = 0;
        while (entryIndex < fullIntraRequest.Entries.Length)
        {
            int offset = entryIndex * EntrySize;
            RtcpFullIntraRequestEntry entry = new RtcpFullIntraRequestEntry();
            entry.SourceIdentifier = NetworkBinaryReader.ReadUInt32(source, offset);
            entry.SequenceNumber = source[offset + 4];
            fullIntraRequest.Entries[entryIndex] = entry;
            entryIndex = entryIndex + 1;
        }

        return fullIntraRequest;
    }

    internal static void EncodeFullIntraRequest(
        RtcpFullIntraRequest fullIntraRequest,
        Span<byte> destination)
    {
        int entryIndex = 0;
        while (entryIndex < fullIntraRequest.Entries.Length)
        {
            int offset = entryIndex * EntrySize;
            RtcpFullIntraRequestEntry entry =
                fullIntraRequest.Entries[entryIndex];
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset,
                entry.SourceIdentifier);
            destination[offset + 4] = entry.SequenceNumber;
            destination[offset + 5] = 0;
            destination[offset + 6] = 0;
            destination[offset + 7] = 0;
            entryIndex = entryIndex + 1;
        }
    }

    internal static byte AdvanceFullIntraRequestSequenceNumber(
        byte sequenceNumber)
    {
        if (sequenceNumber == byte.MaxValue)
        {
            return 0;
        }

        return (byte)(sequenceNumber + 1);
    }
}
