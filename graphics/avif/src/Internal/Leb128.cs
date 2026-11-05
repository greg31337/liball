using System;

namespace avif;

internal static class Leb128
{
    internal const int MaxBytes = 8;
    internal const ulong MaxValue = 72057594037927935;

    internal static ulong ReadUnsigned(ReadOnlySpan<byte> source, ByteReader reader)
    {
        if (reader == null)
        {
            return 0;
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return 0;
        }

        int initialPosition = reader.Position;
        ulong value = 0;
        int index = 0;
        while (index < MaxBytes)
        {
            if (!reader.CanRead(source, index + 1))
            {
                reader.Position = initialPosition;
                return 0;
            }

            byte encoded = source[initialPosition + index];
            ulong payload = (uint)(encoded & 127);
            value |= payload << (index * 7);
            if ((encoded & 128) == 0)
            {
                int byteCount = index + 1;
                if (GetEncodedLength(value) != byteCount)
                {
                    reader.Code = AvifResultCode.InvalidData;
                    reader.Position = initialPosition;
                    return 0;
                }

                reader.Position = initialPosition + index + 1;
                return value;
            }

            index++;
        }

        reader.Code = AvifResultCode.InvalidData;
        reader.Position = initialPosition;
        return 0;
    }

    internal static AvifResultCode WriteUnsigned(
        Span<byte> destination,
        ByteWriter writer,
        ulong value)
    {
        if (writer == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (writer.Code != AvifResultCode.Success)
        {
            return writer.Code;
        }

        if (value > MaxValue)
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return writer.Code;
        }

        int byteCount = GetEncodedLength(value);

        if (!writer.CanWrite(destination, byteCount))
        {
            return writer.Code;
        }

        ulong remaining = value;
        int index = 0;
        while (index < byteCount)
        {
            byte encoded = (byte)(remaining & 127);
            remaining >>= 7;
            if (index != byteCount - 1)
            {
                encoded = (byte)(encoded | 128);
            }

            destination[writer.Position + index] = encoded;
            index++;
        }

        writer.Position += byteCount;
        return writer.Code;
    }

    internal static int GetEncodedLength(ulong value)
    {
        int byteCount = 1;
        ulong remaining = value >> 7;
        while (remaining != 0)
        {
            byteCount++;
            remaining >>= 7;
        }

        return byteCount;
    }
}
