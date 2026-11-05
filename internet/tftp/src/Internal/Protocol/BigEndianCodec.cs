using System;

namespace tftp;

internal static class BigEndianCodec
{
    internal static ushort ReadUInt16(ReadOnlySpan<byte> source, int offset)
    {
        if (offset < 0)
        {
            throw new TftpProtocolException("The read offset cannot be negative.", false, 0);
        }

        if (offset > source.Length - 2)
        {
            throw new TftpProtocolException(
                "Two bytes are required to read an unsigned 16-bit value.",
                true,
                offset);
        }

        int value = (source[offset] << 8) | source[offset + 1];
        return (ushort)value;
    }

    internal static void WriteUInt16(Span<byte> destination, int offset, ushort value)
    {
        if (offset < 0)
        {
            throw new TftpProtocolException("The write offset cannot be negative.", false, 0);
        }

        if (offset > destination.Length - 2)
        {
            throw new TftpProtocolException(
                "Two destination bytes are required to write an unsigned 16-bit value.",
                true,
                offset);
        }

        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)(value & 255);
    }
}
