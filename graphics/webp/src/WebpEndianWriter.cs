using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpEndianWriter
{
    internal static void WriteByte(Span<byte> data, int offset, byte value)
    {
        RequireAvailable(data.Length, offset, 1);
        data[offset] = value;
    }

    internal static void WriteUInt16(Span<byte> data, int offset, ushort value)
    {
        RequireAvailable(data.Length, offset, 2);

        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    internal static void WriteUInt24(Span<byte> data, int offset, uint value)
    {
        if (value > 0xFFFFFFU)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        RequireAvailable(data.Length, offset, 3);
        WriteUInt24Unchecked(data, offset, value);
    }

    internal static void WriteInt24(Span<byte> data, int offset, int value)
    {
        uint unsignedValue = EncodeInt24(value);

        RequireAvailable(data.Length, offset, 3);
        WriteUInt24Unchecked(data, offset, unsignedValue);
    }

    internal static void WriteUInt32(Span<byte> data, int offset, uint value)
    {
        RequireAvailable(data.Length, offset, 4);

        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    internal static void WriteBytes(Span<byte> data, int offset, ReadOnlySpan<byte> payload)
    {
        RequireAvailable(data.Length, offset, payload.Length);
        payload.CopyTo(data.Slice(offset, payload.Length));
    }

    internal static void WritePaddingByte(Span<byte> data, int offset)
    {
        WriteByte(data, offset, 0);
    }

    internal static void AppendByte(List<byte> data, byte value)
    {
        ArgumentNullException.ThrowIfNull(data);
        data.Add(value);
    }

    internal static void AppendUInt16(List<byte> data, ushort value)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Add((byte)(value & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
    }

    internal static void AppendUInt24(List<byte> data, uint value)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (value > 0xFFFFFFU)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        AppendUInt24Unchecked(data, value);
    }

    internal static void AppendInt24(List<byte> data, int value)
    {
        ArgumentNullException.ThrowIfNull(data);

        uint unsignedValue = EncodeInt24(value);
        AppendUInt24Unchecked(data, unsignedValue);
    }

    internal static void AppendUInt32(List<byte> data, uint value)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Add((byte)(value & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)((value >> 16) & 0xFF));
        data.Add((byte)((value >> 24) & 0xFF));
    }

    internal static void AppendBytes(List<byte> data, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(payload);

        int index = 0;
        while (index < payload.Length)
        {
            data.Add(payload[index]);
            index = index + 1;
        }
    }

    internal static void AppendPadding(List<byte> data, int payloadLength)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (payloadLength < 0)
        {
            throw new ArgumentOutOfRangeException("payloadLength");
        }

        if ((payloadLength & 1) != 0)
        {
            data.Add(0);
        }
    }

    internal static void AppendChunkPayload(List<byte> data, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        AppendBytes(data, payload);
        AppendPadding(data, payload.Length);
    }

    internal static void WriteUInt24Unchecked(Span<byte> data, int offset, uint value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    internal static void AppendUInt24Unchecked(List<byte> data, uint value)
    {
        data.Add((byte)(value & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)((value >> 16) & 0xFF));
    }

    internal static uint EncodeInt24(int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (value < 0)
        {
            return (uint)(value + 0x1000000);
        }

        return (uint)value;
    }

    internal static void RequireAvailable(int dataLength, int offset, int length)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (offset > dataLength)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length > dataLength - offset)
        {
            throw new ArgumentException("requested range extends beyond the output buffer", "length");
        }
    }
}
