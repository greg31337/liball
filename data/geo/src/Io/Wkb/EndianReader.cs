using System;
using System.Buffers.Binary;
using System.IO;

namespace geo;

internal static class EndianReader
{
    internal static bool HasBytes(ReadOnlySpan<byte> source, int offset, int count)
    {
        if (offset < 0 || count < 0) return false;
        if (count > source.Length) return false;
        return offset <= source.Length - count;
    }

    internal static byte ReadByteFromSpan(ReadOnlySpan<byte> source, int offset)
    {
        ValidateReadRange(source, offset, 1);
        return source[offset];
    }

    internal static uint ReadUInt32FromSpan(ReadOnlySpan<byte> source, int offset, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateReadRange(source, offset, 4);
        ReadOnlySpan<byte> value = source.Slice(offset, 4);
        if (byteOrder == ByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(value);
        }
        return BinaryPrimitives.ReadUInt32BigEndian(value);
    }

    internal static double ReadDoubleFromSpan(ReadOnlySpan<byte> source, int offset, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateReadRange(source, offset, 8);
        ReadOnlySpan<byte> value = source.Slice(offset, 8);
        long bits;
        if (byteOrder == ByteOrder.LittleEndian)
        {
            bits = BinaryPrimitives.ReadInt64LittleEndian(value);
        }
        else
        {
            bits = BinaryPrimitives.ReadInt64BigEndian(value);
        }
        return BitConverter.Int64BitsToDouble(bits);
    }

    internal static byte ReadByteFromArray(byte[] source, int offset)
    {
        ValidateSourceArray(source);
        return ReadByteFromSpan(source, offset);
    }

    internal static uint ReadUInt32FromArray(byte[] source, int offset, ByteOrder byteOrder)
    {
        ValidateSourceArray(source);
        return ReadUInt32FromSpan(source, offset, byteOrder);
    }

    internal static double ReadDoubleFromArray(byte[] source, int offset, ByteOrder byteOrder)
    {
        ValidateSourceArray(source);
        return ReadDoubleFromSpan(source, offset, byteOrder);
    }

    internal static byte ReadByteFromStream(Stream stream)
    {
        byte[] bytes = ReadExactBytesFromStream(stream, 1);
        return bytes[0];
    }

    internal static uint ReadUInt32FromStream(Stream stream, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        byte[] bytes = ReadExactBytesFromStream(stream, 4);
        return ReadUInt32FromSpan(bytes, 0, byteOrder);
    }

    internal static double ReadDoubleFromStream(Stream stream, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        byte[] bytes = ReadExactBytesFromStream(stream, 8);
        return ReadDoubleFromSpan(bytes, 0, byteOrder);
    }

    internal static byte[] ReadExactBytesFromStream(Stream stream, int count)
    {
        if (stream == null)
        {
            throw new GeometryParseException("WKB input stream is null.");
        }
        if (!stream.CanRead)
        {
            throw new GeometryParseException("WKB input stream is not readable.");
        }
        if (count < 0 || count > 8)
        {
            throw new GeometryParseException("A WKB primitive read must request between zero and eight bytes.");
        }

        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
            {
                throw new GeometryParseException(
                    $"WKB input ended after {offset} of {count} required primitive bytes.");
            }
            offset += read;
        }
        return buffer;
    }

    internal static void ValidateReadRange(ReadOnlySpan<byte> source, int offset, int count)
    {
        if (!HasBytes(source, offset, count))
        {
            throw new GeometryParseException(
                $"WKB input does not contain {count} bytes at byte offset {offset}.");
        }
    }

    internal static void ValidateByteOrder(ByteOrder byteOrder)
    {
        if (byteOrder != ByteOrder.BigEndian && byteOrder != ByteOrder.LittleEndian)
        {
            throw new GeometryParseException("WKB byte order must be big-endian or little-endian.");
        }
    }

    internal static void ValidateSourceArray(byte[] source)
    {
        if (source == null)
        {
            throw new GeometryParseException("WKB input byte array is null.");
        }
    }
}
