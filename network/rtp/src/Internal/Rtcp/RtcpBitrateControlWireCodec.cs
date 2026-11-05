using System;

namespace rtp;

internal static class RtcpBitrateControlWireCodec
{
    const int EntrySize = 8;
    const byte ExponentMaximum = 63;
    const uint MantissaMaximum = 131071U;
    const ushort OverheadMaximum = 511;

    internal static string? GetBitrateControlValidationError(
        RtcpBitrateControl? bitrateControl,
        bool isNotification,
        uint mediaSourceIdentifier)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of TMMBR and TMMBN must be zero.";
        }

        if (bitrateControl == null)
        {
            return "The bitrate-control feedback body is required.";
        }

        if (bitrateControl.IsNotification != isNotification)
        {
            return "The TMMBR or TMMBN discriminator does not match its typed body.";
        }

        if (bitrateControl.Entries == null)
        {
            return "The bitrate-control entry array is required.";
        }

        if (!isNotification && bitrateControl.Entries.Length == 0)
        {
            return "A TMMBR request must contain at least one entry.";
        }

        int entryIndex = 0;
        while (entryIndex < bitrateControl.Entries.Length)
        {
            RtcpBitrateControlEntry? entry = bitrateControl.Entries[entryIndex];
            if (entry == null)
            {
                return "A bitrate-control entry is required.";
            }

            if (entry.Exponent > ExponentMaximum)
            {
                return "The maximum bitrate exponent must fit six bits.";
            }

            if (entry.Mantissa > MantissaMaximum)
            {
                return "The maximum bitrate mantissa must fit seventeen bits.";
            }

            if (entry.Overhead > OverheadMaximum)
            {
                return "The measured packet overhead must fit nine bits.";
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    internal static string? GetBitrateControlDecodeError(
        ReadOnlySpan<byte> source,
        bool isNotification,
        uint mediaSourceIdentifier,
        int lengthInWordsMinusOne)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "The common media-source SSRC of TMMBR and TMMBN must be zero.";
        }

        if (!isNotification && source.Length == 0)
        {
            return "A TMMBR request must contain at least one entry.";
        }

        if (source.Length % EntrySize != 0)
        {
            return "TMMBR and TMMBN must contain complete eight-byte entries.";
        }

        int expectedLength = 2 + source.Length /
            RtcpProtocolConstants.LengthWordSize;
        if (lengthInWordsMinusOne != expectedLength)
        {
            return "A TMMBR or TMMBN length must be 2 plus two words per entry.";
        }

        return null;
    }

    internal static long CalculateBitrateControlSize(
        RtcpBitrateControl bitrateControl)
    {
        return (long)bitrateControl.Entries.Length * EntrySize;
    }

    internal static RtcpBitrateControl DecodeBitrateControl(
        ReadOnlySpan<byte> source,
        bool isNotification)
    {
        RtcpBitrateControl bitrateControl = new RtcpBitrateControl();
        bitrateControl.IsNotification = isNotification;
        bitrateControl.Entries =
            new RtcpBitrateControlEntry[source.Length / EntrySize];
        int entryIndex = 0;
        while (entryIndex < bitrateControl.Entries.Length)
        {
            int offset = entryIndex * EntrySize;
            uint packedTuple = NetworkBinaryReader.ReadUInt32(source, offset + 4);
            RtcpBitrateControlEntry entry = new RtcpBitrateControlEntry();
            entry.SourceIdentifier = NetworkBinaryReader.ReadUInt32(source, offset);
            entry.Exponent = (byte)(packedTuple >> 26);
            entry.Mantissa = (packedTuple >> 9) & MantissaMaximum;
            entry.Overhead = (ushort)(packedTuple & OverheadMaximum);
            bitrateControl.Entries[entryIndex] = entry;
            entryIndex = entryIndex + 1;
        }

        return bitrateControl;
    }

    internal static void EncodeBitrateControl(
        RtcpBitrateControl bitrateControl,
        Span<byte> destination)
    {
        int entryIndex = 0;
        while (entryIndex < bitrateControl.Entries.Length)
        {
            int offset = entryIndex * EntrySize;
            RtcpBitrateControlEntry entry = bitrateControl.Entries[entryIndex];
            uint packedTuple = ((uint)entry.Exponent << 26) |
                (entry.Mantissa << 9) |
                entry.Overhead;
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset,
                entry.SourceIdentifier);
            NetworkBinaryWriter.WriteUInt32(destination, offset + 4, packedTuple);
            entryIndex = entryIndex + 1;
        }
    }
}
