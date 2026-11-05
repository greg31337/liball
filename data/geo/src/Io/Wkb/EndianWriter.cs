using System;
using System.Buffers.Binary;
using System.IO;

namespace geo;

internal static class EndianWriter
{
    internal static bool HasBytes(Span<byte> destination, int offset, int count)
    {
        if (offset < 0 || count < 0) return false;
        if (count > destination.Length) return false;
        return offset <= destination.Length - count;
    }

    internal static void WriteByteToSpan(Span<byte> destination, int offset, byte value)
    {
        ValidateWriteRange(destination, offset, 1);
        destination[offset] = value;
    }

    internal static void WriteUInt32ToSpan(Span<byte> destination, int offset, uint value, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateWriteRange(destination, offset, 4);
        Span<byte> target = destination.Slice(offset, 4);
        if (byteOrder == ByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(target, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(target, value);
        }
    }

    internal static void WriteDoubleToSpan(Span<byte> destination, int offset, double value, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateWriteRange(destination, offset, 8);
        Span<byte> target = destination.Slice(offset, 8);
        long bits = BitConverter.DoubleToInt64Bits(value);
        if (byteOrder == ByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteInt64LittleEndian(target, bits);
        }
        else
        {
            BinaryPrimitives.WriteInt64BigEndian(target, bits);
        }
    }

    internal static void WriteByteToArray(byte[] destination, int offset, byte value)
    {
        ValidateDestinationArray(destination);
        WriteByteToSpan(destination, offset, value);
    }

    internal static void WriteUInt32ToArray(byte[] destination, int offset, uint value, ByteOrder byteOrder)
    {
        ValidateDestinationArray(destination);
        WriteUInt32ToSpan(destination, offset, value, byteOrder);
    }

    internal static void WriteDoubleToArray(byte[] destination, int offset, double value, ByteOrder byteOrder)
    {
        ValidateDestinationArray(destination);
        WriteDoubleToSpan(destination, offset, value, byteOrder);
    }

    internal static void WriteByteToStream(Stream stream, byte value)
    {
        ValidateStream(stream);
        stream.WriteByte(value);
    }

    internal static void WriteUInt32ToStream(Stream stream, uint value, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateStream(stream);
        byte[] bytes = new byte[4];
        WriteUInt32ToSpan(bytes, 0, value, byteOrder);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void WriteDoubleToStream(Stream stream, double value, ByteOrder byteOrder)
    {
        ValidateByteOrder(byteOrder);
        ValidateStream(stream);
        byte[] bytes = new byte[8];
        WriteDoubleToSpan(bytes, 0, value, byteOrder);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void ValidateWriteRange(Span<byte> destination, int offset, int count)
    {
        if (!HasBytes(destination, offset, count))
        {
            throw new InvalidGeometryException(
                $"WKB destination does not contain {count} bytes at byte offset {offset}.");
        }
    }

    internal static void ValidateByteOrder(ByteOrder byteOrder)
    {
        if (byteOrder != ByteOrder.BigEndian && byteOrder != ByteOrder.LittleEndian)
        {
            throw new InvalidGeometryException("WKB byte order must be big-endian or little-endian.");
        }
    }

    internal static void ValidateDestinationArray(byte[] destination)
    {
        if (destination == null)
        {
            throw new InvalidGeometryException("WKB destination byte array is null.");
        }
    }

    internal static void ValidateStream(Stream stream)
    {
        if (stream == null)
        {
            throw new InvalidGeometryException("WKB output stream is null.");
        }
        if (!stream.CanWrite)
        {
            throw new InvalidGeometryException("WKB output stream is not writable.");
        }
    }
}
