using System;
using System.Buffers.Binary;
using System.IO;

namespace tiff;

internal class TiffBinaryWriter
{
    internal static void WriteByte(Span<byte> destination, byte value)
    {
        RequireLength(destination, 1);
        destination[0] = value;
    }

    internal static void WriteSByte(Span<byte> destination, sbyte value)
    {
        RequireLength(destination, 1);
        if (value < 0)
        {
            destination[0] = (byte)(value + 256);
            return;
        }

        destination[0] = (byte)value;
    }

    internal static void WriteUInt16(Span<byte> destination, ushort value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 2);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteInt16(Span<byte> destination, short value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 2);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteInt16LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteInt16BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteUInt32(Span<byte> destination, uint value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 4);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteInt32(Span<byte> destination, int value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 4);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteInt32LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteUInt64(Span<byte> destination, ulong value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 8);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteInt64(Span<byte> destination, long value, TiffByteOrder byteOrder)
    {
        RequireLength(destination, 8);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteInt64LittleEndian(destination, value);
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            BinaryPrimitives.WriteInt64BigEndian(destination, value);
            return;
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static void WriteSingle(Span<byte> destination, float value, TiffByteOrder byteOrder)
    {
        uint bits = BitConverter.SingleToUInt32Bits(value);
        WriteUInt32(destination, bits, byteOrder);
    }

    internal static void WriteDouble(Span<byte> destination, double value, TiffByteOrder byteOrder)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        WriteUInt64(destination, bits, byteOrder);
    }

    internal static void WriteByte(Stream stream, byte value)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        stream.WriteByte(value);
    }

    internal static void WriteSByte(Stream stream, sbyte value)
    {
        byte[] buffer = new byte[1];
        WriteSByte(buffer, value);
        WriteBytes(stream, buffer);
    }

    internal static void WriteUInt16(Stream stream, ushort value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[2];
        WriteUInt16(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteInt16(Stream stream, short value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[2];
        WriteInt16(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteUInt32(Stream stream, uint value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[4];
        WriteUInt32(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteInt32(Stream stream, int value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[4];
        WriteInt32(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteUInt64(Stream stream, ulong value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[8];
        WriteUInt64(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteInt64(Stream stream, long value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[8];
        WriteInt64(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteSingle(Stream stream, float value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[4];
        WriteSingle(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteDouble(Stream stream, double value, TiffByteOrder byteOrder)
    {
        byte[] buffer = new byte[8];
        WriteDouble(buffer, value, byteOrder);
        WriteBytes(stream, buffer);
    }

    internal static void WriteBytes(Stream stream, ReadOnlySpan<byte> source)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        stream.Write(source);
    }

    internal static void RequireLength(Span<byte> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException("The destination buffer is shorter than the requested TIFF primitive.");
        }
    }
}
