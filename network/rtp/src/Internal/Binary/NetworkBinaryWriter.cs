using System;

namespace rtp;

internal static class NetworkBinaryWriter
{
    internal static void WriteByte(Span<byte> destination, int offset, byte value)
    {
        ValidateRange(destination, offset, 1);
        destination[offset] = value;
    }

    internal static void WriteUInt16(Span<byte> destination, int offset, ushort value)
    {
        ValidateRange(destination, offset, 2);
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)(value & 255U);
    }

    internal static void WriteUInt24(Span<byte> destination, int offset, uint value)
    {
        if (value > 16777215U)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        ValidateRange(destination, offset, 3);
        destination[offset] = (byte)(value >> 16);
        destination[offset + 1] = (byte)((value >> 8) & 255U);
        destination[offset + 2] = (byte)(value & 255U);
    }

    internal static void WriteInt24(Span<byte> destination, int offset, int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        ValidateRange(destination, offset, 3);
        int encoded = value;
        if (encoded < 0)
        {
            encoded = encoded + 16777216;
        }

        destination[offset] = (byte)(encoded >> 16);
        destination[offset + 1] = (byte)((encoded >> 8) & 255);
        destination[offset + 2] = (byte)(encoded & 255);
    }

    internal static void WriteUInt32(Span<byte> destination, int offset, uint value)
    {
        ValidateRange(destination, offset, 4);
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)((value >> 16) & 255U);
        destination[offset + 2] = (byte)((value >> 8) & 255U);
        destination[offset + 3] = (byte)(value & 255U);
    }

    internal static void WriteUInt64(Span<byte> destination, int offset, ulong value)
    {
        ValidateRange(destination, offset, 8);
        destination[offset] = (byte)(value >> 56);
        destination[offset + 1] = (byte)((value >> 48) & 255UL);
        destination[offset + 2] = (byte)((value >> 40) & 255UL);
        destination[offset + 3] = (byte)((value >> 32) & 255UL);
        destination[offset + 4] = (byte)((value >> 24) & 255UL);
        destination[offset + 5] = (byte)((value >> 16) & 255UL);
        destination[offset + 6] = (byte)((value >> 8) & 255UL);
        destination[offset + 7] = (byte)(value & 255UL);
    }

    internal static void WriteBytes(Span<byte> destination, int offset, ReadOnlySpan<byte> source)
    {
        ValidateRange(destination, offset, source.Length);
        source.CopyTo(destination.Slice(offset, source.Length));
    }

    static void ValidateRange(Span<byte> destination, int offset, int count)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (offset > destination.Length || count > destination.Length - offset)
        {
            throw new ArgumentException("The destination does not contain the requested bytes.");
        }
    }
}
