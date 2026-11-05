using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtcpTemporalSpatialTradeoffWireCodec
{
    const int EntrySize = 8;
    const byte TradeoffIndexMaximum = 31;

    internal static string? GetTemporalSpatialTradeoffValidationError(
        RtcpTemporalSpatialTradeoff[]? tradeoffs,
        bool isNotification,
        uint mediaSourceIdentifier)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of TSTR and TSTN must be zero.";
        }

        if (tradeoffs == null || tradeoffs.Length == 0)
        {
            return "TSTR and TSTN must contain at least one entry.";
        }

        HashSet<uint> sourceIdentifiers = new HashSet<uint>();
        byte notificationIndex = 0;
        int entryIndex = 0;
        while (entryIndex < tradeoffs.Length)
        {
            RtcpTemporalSpatialTradeoff? tradeoff = tradeoffs[entryIndex];
            if (tradeoff == null)
            {
                return "A temporal-spatial trade-off entry is required.";
            }

            if (tradeoff.IsNotification != isNotification)
            {
                return "The TSTR or TSTN discriminator does not match its entry.";
            }

            if (tradeoff.TradeoffIndex > TradeoffIndexMaximum)
            {
                return "The temporal-spatial trade-off index must fit five bits.";
            }

            if (!sourceIdentifiers.Add(tradeoff.SourceIdentifier))
            {
                return "TSTR and TSTN cannot repeat an entry SSRC.";
            }

            if (entryIndex == 0)
            {
                notificationIndex = tradeoff.TradeoffIndex;
            }
            else if (isNotification &&
                tradeoff.TradeoffIndex != notificationIndex)
            {
                return "Every TSTN entry must contain the same trade-off index.";
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static string? GetTemporalSpatialTradeoffDecodeError(
        ReadOnlySpan<byte> source,
        bool isNotification,
        uint mediaSourceIdentifier,
        int lengthInWordsMinusOne)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of TSTR and TSTN must be zero.";
        }

        if (source.Length == 0)
        {
            return "TSTR and TSTN must contain at least one entry.";
        }

        if (source.Length % EntrySize != 0)
        {
            return "TSTR and TSTN must contain complete eight-byte entries.";
        }

        int expectedLength = 2 + source.Length /
            RtcpProtocolConstants.LengthWordSize;
        if (lengthInWordsMinusOne != expectedLength)
        {
            return "A TSTR or TSTN length must be 2 plus two words per entry.";
        }

        HashSet<uint> sourceIdentifiers = new HashSet<uint>();
        byte notificationIndex = 0;
        int entryIndex = 0;
        while (entryIndex < source.Length / EntrySize)
        {
            int offset = entryIndex * EntrySize;
            uint sourceIdentifier = NetworkBinaryReader.ReadUInt32(source, offset);
            if (!sourceIdentifiers.Add(sourceIdentifier))
            {
                return "TSTR and TSTN cannot repeat an entry SSRC.";
            }

            byte tradeoffIndex = (byte)(source[offset + 7] &
                TradeoffIndexMaximum);
            if (entryIndex == 0)
            {
                notificationIndex = tradeoffIndex;
            }
            else if (isNotification && tradeoffIndex != notificationIndex)
            {
                return "Every TSTN entry must contain the same trade-off index.";
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static long CalculateTemporalSpatialTradeoffSize(
        RtcpTemporalSpatialTradeoff[] tradeoffs)
    {
        return (long)tradeoffs.Length * EntrySize;
    }

    internal static RtcpTemporalSpatialTradeoff[] DecodeTemporalSpatialTradeoffs(
        ReadOnlySpan<byte> source,
        bool isNotification)
    {
        RtcpTemporalSpatialTradeoff[] tradeoffs =
            new RtcpTemporalSpatialTradeoff[source.Length / EntrySize];
        int entryIndex = 0;
        while (entryIndex < tradeoffs.Length)
        {
            int offset = entryIndex * EntrySize;
            RtcpTemporalSpatialTradeoff tradeoff =
                new RtcpTemporalSpatialTradeoff();
            tradeoff.IsNotification = isNotification;
            tradeoff.SourceIdentifier = NetworkBinaryReader.ReadUInt32(
                source,
                offset);
            tradeoff.SequenceNumber = source[offset + 4];
            tradeoff.TradeoffIndex = (byte)(source[offset + 7] &
                TradeoffIndexMaximum);
            tradeoffs[entryIndex] = tradeoff;
            entryIndex = entryIndex + 1;
        }

        return tradeoffs;
    }

    internal static void EncodeTemporalSpatialTradeoffs(
        RtcpTemporalSpatialTradeoff[] tradeoffs,
        Span<byte> destination)
    {
        int entryIndex = 0;
        while (entryIndex < tradeoffs.Length)
        {
            int offset = entryIndex * EntrySize;
            RtcpTemporalSpatialTradeoff tradeoff = tradeoffs[entryIndex];
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset,
                tradeoff.SourceIdentifier);
            destination[offset + 4] = tradeoff.SequenceNumber;
            destination[offset + 5] = 0;
            destination[offset + 6] = 0;
            destination[offset + 7] = tradeoff.TradeoffIndex;
            entryIndex = entryIndex + 1;
        }
    }
}
