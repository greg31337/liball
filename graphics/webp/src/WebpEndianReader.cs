using System;

namespace webp;

internal static class WebpEndianReader
{
    internal static byte ReadByte(ReadOnlySpan<byte> data, int offset)
    {
        RequireAvailable(data, offset, 1);
        return data[offset];
    }

    internal static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
        RequireAvailable(data, offset, 2);

        int value = data[offset];
        value = value | (data[offset + 1] << 8);
        return (ushort)value;
    }

    internal static uint ReadUInt24(ReadOnlySpan<byte> data, int offset)
    {
        RequireAvailable(data, offset, 3);

        uint value = data[offset];
        value = value | ((uint)data[offset + 1] << 8);
        value = value | ((uint)data[offset + 2] << 16);
        return value;
    }

    internal static int ReadInt24(ReadOnlySpan<byte> data, int offset)
    {
        uint value = ReadUInt24(data, offset);
        if ((value & 0x800000U) != 0)
        {
            return (int)value - 0x1000000;
        }

        return (int)value;
    }

    internal static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        RequireAvailable(data, offset, 4);

        uint value = data[offset];
        value = value | ((uint)data[offset + 1] << 8);
        value = value | ((uint)data[offset + 2] << 16);
        value = value | ((uint)data[offset + 3] << 24);
        return value;
    }

    internal static ReadOnlySpan<byte> ReadSlice(ReadOnlySpan<byte> data, int offset, int length)
    {
        RequireAvailable(data, offset, length);
        return data.Slice(offset, length);
    }

    internal static void RequireAvailable(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length > data.Length - offset)
        {
            throw new ArgumentException("requested range extends beyond the input buffer", "length");
        }
    }
}
