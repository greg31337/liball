namespace raw;

internal static class EndianWriter
{
    internal static void WriteUInt8(System.Span<byte> destination, byte value)
    {
        RequireLength(destination, 1);
        destination[0] = value;
    }

    internal static void WriteInt8(System.Span<byte> destination, sbyte value)
    {
        RequireLength(destination, 1);
        destination[0] = BitUtility.ToUInt8Bits(value);
    }

    internal static void WriteUInt16(System.Span<byte> destination, ushort value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 2);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteInt16(System.Span<byte> destination, short value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 2);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt16BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteUInt32(System.Span<byte> destination, uint value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 4);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteInt32(System.Span<byte> destination, int value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 4);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteUInt64(System.Span<byte> destination, ulong value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 8);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteInt64(System.Span<byte> destination, long value, RawByteOrder byteOrder)
    {
        RequireLength(destination, 8);
        if (byteOrder == RawByteOrder.LittleEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(destination, value);
            return;
        }
        if (byteOrder == RawByteOrder.BigEndian)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(destination, value);
            return;
        }
        throw InvalidByteOrder();
    }

    internal static void WriteHalf(System.Span<byte> destination, System.Half value, RawByteOrder byteOrder)
    {
        WriteUInt16(destination, System.BitConverter.HalfToUInt16Bits(value), byteOrder);
    }

    internal static void WriteSingle(System.Span<byte> destination, float value, RawByteOrder byteOrder)
    {
        WriteUInt32(destination, System.BitConverter.SingleToUInt32Bits(value), byteOrder);
    }

    internal static void WriteDouble(System.Span<byte> destination, double value, RawByteOrder byteOrder)
    {
        WriteUInt64(destination, System.BitConverter.DoubleToUInt64Bits(value), byteOrder);
    }

    internal static void RequireLength(System.Span<byte> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The destination span is too short for the requested primitive.", -1);
        }
    }

    internal static RawFormatException InvalidByteOrder()
    {
        return new RawFormatException(RawDiagnosticCode.InvalidArgument, "The byte order is not defined.", -1);
    }
}
