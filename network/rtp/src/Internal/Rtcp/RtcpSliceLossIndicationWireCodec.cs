using System;

namespace rtp;

internal static class RtcpSliceLossIndicationWireCodec
{
    const int EntrySize = 4;
    const int MacroblockMaximum = 8191;
    const byte PictureIdentifierMaximum = 63;

    internal static string? GetSliceLossIndicationValidationError(
        RtcpSliceLossIndication? sliceLossIndication)
    {
        if (sliceLossIndication == null)
        {
            return "The slice loss indication body is required.";
        }

        if (sliceLossIndication.Entries == null ||
            sliceLossIndication.Entries.Length == 0)
        {
            return "A slice loss indication must contain at least one entry.";
        }

        int entryIndex = 0;
        while (entryIndex < sliceLossIndication.Entries.Length)
        {
            RtcpSliceLossEntry? entry = sliceLossIndication.Entries[entryIndex];
            if (entry == null)
            {
                return "A slice loss indication entry is required.";
            }

            string? entryError = GetSliceLossEntryValidationError(entry);
            if (entryError != null)
            {
                return entryError;
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static string? GetSliceLossIndicationDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
        {
            return "A slice loss indication must contain at least one entry.";
        }

        if (source.Length % EntrySize != 0)
        {
            return "A slice loss indication must contain complete four-byte entries.";
        }

        int offset = 0;
        while (offset < source.Length)
        {
            uint packedEntry = NetworkBinaryReader.ReadUInt32(source, offset);
            int firstMacroblock = (int)(packedEntry >> 19);
            int macroblockCount = (int)((packedEntry >> 6) & MacroblockMaximum);
            if (firstMacroblock == 0 || macroblockCount == 0)
            {
                return "Slice loss macroblock fields must be positive.";
            }

            offset = offset + EntrySize;
        }

        return null;
    }

    internal static long CalculateSliceLossIndicationSize(
        RtcpSliceLossIndication sliceLossIndication)
    {
        return (long)sliceLossIndication.Entries.Length * EntrySize;
    }

    internal static RtcpSliceLossIndication DecodeSliceLossIndication(
        ReadOnlySpan<byte> source)
    {
        RtcpSliceLossIndication sliceLossIndication =
            new RtcpSliceLossIndication();
        sliceLossIndication.Entries =
            new RtcpSliceLossEntry[source.Length / EntrySize];
        int entryIndex = 0;
        while (entryIndex < sliceLossIndication.Entries.Length)
        {
            uint packedEntry = NetworkBinaryReader.ReadUInt32(
                source,
                entryIndex * EntrySize);
            RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
            entry.FirstMacroblock = (int)(packedEntry >> 19);
            entry.MacroblockCount =
                (int)((packedEntry >> 6) & MacroblockMaximum);
            entry.PictureIdentifier =
                (byte)(packedEntry & PictureIdentifierMaximum);
            sliceLossIndication.Entries[entryIndex] = entry;
            entryIndex = entryIndex + 1;
        }

        return sliceLossIndication;
    }

    internal static void EncodeSliceLossIndication(
        RtcpSliceLossIndication sliceLossIndication,
        Span<byte> destination)
    {
        int entryIndex = 0;
        while (entryIndex < sliceLossIndication.Entries.Length)
        {
            RtcpSliceLossEntry entry = sliceLossIndication.Entries[entryIndex];
            uint packedEntry = ((uint)entry.FirstMacroblock << 19) |
                ((uint)entry.MacroblockCount << 6) |
                entry.PictureIdentifier;
            NetworkBinaryWriter.WriteUInt32(
                destination,
                entryIndex * EntrySize,
                packedEntry);
            entryIndex = entryIndex + 1;
        }
    }

    internal static string? GetSliceLossEntryValidationError(
        RtcpSliceLossEntry entry)
    {
        if (entry.FirstMacroblock < 1 ||
            entry.FirstMacroblock > MacroblockMaximum)
        {
            return "The first slice loss macroblock must fit a positive 13-bit value.";
        }

        if (entry.MacroblockCount < 1 ||
            entry.MacroblockCount > MacroblockMaximum)
        {
            return "The slice loss macroblock count must fit a positive 13-bit value.";
        }

        if (entry.PictureIdentifier > PictureIdentifierMaximum)
        {
            return "The slice loss picture identifier must fit six bits.";
        }

        return null;
    }
}
