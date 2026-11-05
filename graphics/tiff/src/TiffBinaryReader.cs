using System;
using System.Buffers.Binary;
using System.IO;

namespace tiff;

internal class TiffBinaryReader
{
    internal static byte ReadByte(ReadOnlySpan<byte> source)
    {
        RequireLength(source, 1);
        return source[0];
    }

    internal static sbyte ReadSByte(ReadOnlySpan<byte> source)
    {
        RequireLength(source, 1);
        if (source[0] > sbyte.MaxValue)
        {
            return (sbyte)(source[0] - 256);
        }

        return (sbyte)source[0];
    }

    internal static ushort ReadUInt16(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 2);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static short ReadInt16(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 2);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadInt16BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static uint ReadUInt32(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 4);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static int ReadInt32(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 4);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadInt32BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static ulong ReadUInt64(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 8);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static long ReadInt64(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        RequireLength(source, 8);
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(source);
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            return BinaryPrimitives.ReadInt64BigEndian(source);
        }

        throw new ArgumentException("Unsupported TIFF byte order.");
    }

    internal static float ReadSingle(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        uint bits = ReadUInt32(source, byteOrder);
        return BitConverter.UInt32BitsToSingle(bits);
    }

    internal static double ReadDouble(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        ulong bits = ReadUInt64(source, byteOrder);
        return BitConverter.UInt64BitsToDouble(bits);
    }

    internal static byte ReadByte(Stream stream)
    {
        byte[] buffer = ReadBytes(stream, 1);
        return ReadByte(buffer);
    }

    internal static sbyte ReadSByte(Stream stream)
    {
        byte[] buffer = ReadBytes(stream, 1);
        return ReadSByte(buffer);
    }

    internal static ushort ReadUInt16(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 2);
        return ReadUInt16(buffer, byteOrder);
    }

    internal static short ReadInt16(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 2);
        return ReadInt16(buffer, byteOrder);
    }

    internal static uint ReadUInt32(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 4);
        return ReadUInt32(buffer, byteOrder);
    }

    internal static int ReadInt32(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 4);
        return ReadInt32(buffer, byteOrder);
    }

    internal static ulong ReadUInt64(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 8);
        return ReadUInt64(buffer, byteOrder);
    }

    internal static long ReadInt64(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 8);
        return ReadInt64(buffer, byteOrder);
    }

    internal static float ReadSingle(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 4);
        return ReadSingle(buffer, byteOrder);
    }

    internal static double ReadDouble(Stream stream, TiffByteOrder byteOrder)
    {
        byte[] buffer = ReadBytes(stream, 8);
        return ReadDouble(buffer, byteOrder);
    }

    internal static byte[] ReadBytes(Stream stream, int count)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        byte[] buffer = new byte[count];
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(buffer, total, count - total);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of TIFF stream.");
            }

            total = total + read;
        }

        return buffer;
    }

    internal static void RequireLength(ReadOnlySpan<byte> source, int requiredLength)
    {
        if (source.Length < requiredLength)
        {
            throw new ArgumentException("The source buffer is shorter than the requested TIFF primitive.");
        }
    }
}
