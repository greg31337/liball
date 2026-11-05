using System;

namespace rtp;

internal static class NetworkBinaryReader
{
    internal static byte ReadByte(ReadOnlySpan<byte> source, int offset)
    {
        ValidateRange(source, offset, 1);
        return source[offset];
    }

    internal static ushort ReadUInt16(ReadOnlySpan<byte> source, int offset)
    {
        ValidateRange(source, offset, 2);
        return (ushort)(((uint)source[offset] << 8) |
            (uint)source[offset + 1]);
    }

    internal static uint ReadUInt24(ReadOnlySpan<byte> source, int offset)
    {
        ValidateRange(source, offset, 3);
        return ((uint)source[offset] << 16) |
            ((uint)source[offset + 1] << 8) |
            (uint)source[offset + 2];
    }

    internal static int ReadInt24(ReadOnlySpan<byte> source, int offset)
    {
        int encoded = (int)ReadUInt24(source, offset);
        if ((encoded & 8388608) != 0)
        {
            return encoded - 16777216;
        }

        return encoded;
    }

    internal static uint ReadUInt32(ReadOnlySpan<byte> source, int offset)
    {
        ValidateRange(source, offset, 4);
        return ((uint)source[offset] << 24) |
            ((uint)source[offset + 1] << 16) |
            ((uint)source[offset + 2] << 8) |
            (uint)source[offset + 3];
    }

    internal static ulong ReadUInt64(ReadOnlySpan<byte> source, int offset)
    {
        ValidateRange(source, offset, 8);
        return ((ulong)source[offset] << 56) |
            ((ulong)source[offset + 1] << 48) |
            ((ulong)source[offset + 2] << 40) |
            ((ulong)source[offset + 3] << 32) |
            ((ulong)source[offset + 4] << 24) |
            ((ulong)source[offset + 5] << 16) |
            ((ulong)source[offset + 6] << 8) |
            (ulong)source[offset + 7];
    }

    internal static ReadOnlySpan<byte> ReadSlice(ReadOnlySpan<byte> source, int offset, int count)
    {
        ValidateRange(source, offset, count);
        return source.Slice(offset, count);
    }

    static void ValidateRange(ReadOnlySpan<byte> source, int offset, int count)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (offset > source.Length || count > source.Length - offset)
        {
            throw new ArgumentException("The source does not contain the requested bytes.");
        }
    }
}
