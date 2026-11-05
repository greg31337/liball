using System;

namespace uuid;

internal class BigEndianTools
{
    internal const long MaxUInt48 = 281474976710655;

    internal static ushort ReadUInt16(byte[]? bytes, int offset)
    {
        byte[] buffer = ValidateRead(bytes, offset, 2);
        int value = (buffer[offset] << 8) | buffer[offset + 1];
        return (ushort)value;
    }

    internal static int ReadUInt24(byte[]? bytes, int offset)
    {
        byte[] buffer = ValidateRead(bytes, offset, 3);
        return (buffer[offset] << 16) | (buffer[offset + 1] << 8) | buffer[offset + 2];
    }

    internal static uint ReadUInt32(byte[]? bytes, int offset)
    {
        byte[] buffer = ValidateRead(bytes, offset, 4);
        uint value = ((uint)buffer[offset] << 24)
            | ((uint)buffer[offset + 1] << 16)
            | ((uint)buffer[offset + 2] << 8)
            | buffer[offset + 3];
        return value;
    }

    internal static long ReadUInt48(byte[]? bytes, int offset)
    {
        byte[] buffer = ValidateRead(bytes, offset, 6);
        long value = ((long)buffer[offset] << 40)
            | ((long)buffer[offset + 1] << 32)
            | ((long)buffer[offset + 2] << 24)
            | ((long)buffer[offset + 3] << 16)
            | ((long)buffer[offset + 4] << 8)
            | buffer[offset + 5];
        return value;
    }

    internal static ulong ReadUInt64(byte[]? bytes, int offset)
    {
        byte[] buffer = ValidateRead(bytes, offset, 8);
        ulong value = ((ulong)buffer[offset] << 56)
            | ((ulong)buffer[offset + 1] << 48)
            | ((ulong)buffer[offset + 2] << 40)
            | ((ulong)buffer[offset + 3] << 32)
            | ((ulong)buffer[offset + 4] << 24)
            | ((ulong)buffer[offset + 5] << 16)
            | ((ulong)buffer[offset + 6] << 8)
            | buffer[offset + 7];
        return value;
    }

    internal static void WriteUInt16(byte[]? bytes, int offset, int value)
    {
        byte[] buffer = ValidateWrite(bytes, offset, 2);
        ValidateRange(value, 65535);
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)(value & 255);
    }

    internal static void WriteUInt24(byte[]? bytes, int offset, int value)
    {
        byte[] buffer = ValidateWrite(bytes, offset, 3);
        ValidateRange(value, 16777215);
        buffer[offset] = (byte)(value >> 16);
        buffer[offset + 1] = (byte)((value >> 8) & 255);
        buffer[offset + 2] = (byte)(value & 255);
    }

    internal static void WriteUInt32(byte[]? bytes, int offset, uint value)
    {
        byte[] buffer = ValidateWrite(bytes, offset, 4);
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)((value >> 16) & 255);
        buffer[offset + 2] = (byte)((value >> 8) & 255);
        buffer[offset + 3] = (byte)(value & 255);
    }

    internal static void WriteUInt48(byte[]? bytes, int offset, long value)
    {
        byte[] buffer = ValidateWrite(bytes, offset, 6);
        ValidateRange(value, MaxUInt48);
        buffer[offset] = (byte)(value >> 40);
        buffer[offset + 1] = (byte)((value >> 32) & 255);
        buffer[offset + 2] = (byte)((value >> 24) & 255);
        buffer[offset + 3] = (byte)((value >> 16) & 255);
        buffer[offset + 4] = (byte)((value >> 8) & 255);
        buffer[offset + 5] = (byte)(value & 255);
    }

    internal static void WriteUInt64(byte[]? bytes, int offset, ulong value)
    {
        byte[] buffer = ValidateWrite(bytes, offset, 8);
        buffer[offset] = (byte)(value >> 56);
        buffer[offset + 1] = (byte)((value >> 48) & 255);
        buffer[offset + 2] = (byte)((value >> 40) & 255);
        buffer[offset + 3] = (byte)((value >> 32) & 255);
        buffer[offset + 4] = (byte)((value >> 24) & 255);
        buffer[offset + 5] = (byte)((value >> 16) & 255);
        buffer[offset + 6] = (byte)((value >> 8) & 255);
        buffer[offset + 7] = (byte)(value & 255);
    }

    internal static byte[] ValidateRead(byte[]? bytes, int offset, int width)
    {
        return ValidateBuffer(bytes, offset, width);
    }

    internal static byte[] ValidateWrite(byte[]? bytes, int offset, int width)
    {
        return ValidateBuffer(bytes, offset, width);
    }

    internal static byte[] ValidateBuffer(byte[]? bytes, int offset, int width)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (bytes.Length - offset < width)
        {
            throw new ArgumentException("byte array is too short for requested offset and width", "bytes");
        }

        return bytes;
    }

    internal static void ValidateRange(long value, long maxValue)
    {
        if (value < 0 || value > maxValue)
        {
            throw new ArgumentOutOfRangeException("value");
        }
    }
}
