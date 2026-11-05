using System;
using System.Buffers.Binary;

namespace diameter;

internal static class NetworkByteOrder
{
    internal static uint ReadUInt24(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 3);
        return ((uint)source[0] << 16) | ((uint)source[1] << 8) | source[2];
    }

    internal static int ReadInt24(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 3);
        int value = (source[0] << 16) | (source[1] << 8) | source[2];
        if ((value & 8388608) != 0)
        {
            value = value - 16777216;
        }

        return value;
    }

    internal static uint ReadUInt32(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 4);
        return BinaryPrimitives.ReadUInt32BigEndian(source);
    }

    internal static int ReadInt32(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 4);
        return BinaryPrimitives.ReadInt32BigEndian(source);
    }

    internal static ulong ReadUInt64(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 8);
        return BinaryPrimitives.ReadUInt64BigEndian(source);
    }

    internal static long ReadInt64(ReadOnlySpan<byte> source)
    {
        ValidateReadLength(source, 8);
        return BinaryPrimitives.ReadInt64BigEndian(source);
    }

    internal static void WriteUInt24(Span<byte> destination, uint value)
    {
        ValidateWriteLength(destination, 3);
        if (value > 16777215U)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        destination[0] = (byte)(value >> 16);
        destination[1] = (byte)((value >> 8) & 255U);
        destination[2] = (byte)(value & 255U);
    }

    internal static void WriteInt24(Span<byte> destination, int value)
    {
        ValidateWriteLength(destination, 3);
        if (value < -8388608 || value > 8388607)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        int encoded = value;
        if (encoded < 0)
        {
            encoded = encoded + 16777216;
        }

        destination[0] = (byte)(encoded >> 16);
        destination[1] = (byte)((encoded >> 8) & 255);
        destination[2] = (byte)(encoded & 255);
    }

    internal static void WriteUInt32(Span<byte> destination, uint value)
    {
        ValidateWriteLength(destination, 4);
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
    }

    internal static void WriteInt32(Span<byte> destination, int value)
    {
        ValidateWriteLength(destination, 4);
        BinaryPrimitives.WriteInt32BigEndian(destination, value);
    }

    internal static void WriteUInt64(Span<byte> destination, ulong value)
    {
        ValidateWriteLength(destination, 8);
        BinaryPrimitives.WriteUInt64BigEndian(destination, value);
    }

    internal static void WriteInt64(Span<byte> destination, long value)
    {
        ValidateWriteLength(destination, 8);
        BinaryPrimitives.WriteInt64BigEndian(destination, value);
    }

    internal static void ValidateReadLength(ReadOnlySpan<byte> source, int requiredLength)
    {
        if (source.Length < requiredLength)
        {
            throw new ArgumentException("The source does not contain enough octets.");
        }
    }

    internal static void ValidateWriteLength(Span<byte> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException("The destination does not contain enough octets.");
        }
    }
}
