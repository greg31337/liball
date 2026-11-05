namespace raw;

internal static class EndianReader
{
    internal static byte ReadUInt8(System.ReadOnlySpan<byte> source)
    {
        RequireLength(source, 1);
        return source[0];
    }

    internal static sbyte ReadInt8(System.ReadOnlySpan<byte> source)
    {
        RequireLength(source, 1);
        return BitUtility.ToInt8Bits(source[0]);
    }

    internal static ushort ReadUInt16(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 2);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static short ReadInt16(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 2);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 4);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static int ReadInt32(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 4);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static ulong ReadUInt64(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 8);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static long ReadInt64(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        RequireLength(source, 8);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(source);
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            return System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(source);
        }
        throw InvalidByteOrder();
    }

    internal static System.Half ReadHalf(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        ushort bits = ReadUInt16(source, byteOrder);
        return System.BitConverter.UInt16BitsToHalf(bits);
    }

    internal static float ReadSingle(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        uint bits = ReadUInt32(source, byteOrder);
        return System.BitConverter.UInt32BitsToSingle(bits);
    }

    internal static double ReadDouble(System.ReadOnlySpan<byte> source, RawByteOrder byteOrder)
    {
        ulong bits = ReadUInt64(source, byteOrder);
        return System.BitConverter.UInt64BitsToDouble(bits);
    }

    internal static void RequireLength(System.ReadOnlySpan<byte> source, int requiredLength)
    {
        if (source.Length < requiredLength)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input span is too short for the requested primitive.", 0);
        }
    }

    internal static RawFormatException InvalidByteOrder()
    {
        return new RawFormatException(RawDiagnosticCode.InvalidArgument, "The byte order is not defined.", -1);
    }
}
