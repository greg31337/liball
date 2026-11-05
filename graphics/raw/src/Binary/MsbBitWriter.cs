namespace raw;

internal class MsbBitWriter
{
    internal BitWriterCore Core;

    internal MsbBitWriter(System.Memory<byte> destination)
    {
        Core = new BitWriterCore(destination, RawBitOrder.MostSignificantBitFirst);
    }

    internal static MsbBitWriter CreateStream(System.IO.Stream destination, long maximumBytes)
    {
        MsbBitWriter writer = new MsbBitWriter(default(System.Memory<byte>));
        writer.Core = BitWriterCore.CreateStream(destination, maximumBytes, RawBitOrder.MostSignificantBitFirst);
        return writer;
    }

    internal void WriteBits(uint value, int count)
    {
        Core.WriteBits(value, count);
    }

    internal void AlignToByte(int paddingBit)
    {
        Core.AlignToByte(paddingBit);
    }

    internal long Finish(int paddingBit)
    {
        return Core.Finish(paddingBit);
    }

    internal long GetBitPosition()
    {
        return Core.GetBitPosition();
    }

    internal long GetBytesWritten()
    {
        return Core.GetBytesWritten();
    }

    internal long GetRemainingBitCapacity()
    {
        return Core.GetRemainingBitCapacity();
    }
}

internal class BitWriterCore
{
    internal System.IO.Stream? Stream;
    internal System.Memory<byte> Memory;
    internal RawBitOrder BitOrder;
    internal ulong Buffer;
    internal int BufferedBitCount;
    internal long BitPosition;
    internal long BytesWritten;
    internal long CapacityBits;
    internal bool UsesMemory;

    internal BitWriterCore(System.Memory<byte> destination, RawBitOrder bitOrder)
    {
        BitUtility.ValidateBitOrder(bitOrder);
        Stream = null;
        Memory = destination;
        BitOrder = bitOrder;
        Buffer = 0;
        BufferedBitCount = 0;
        BitPosition = 0;
        BytesWritten = 0;
        CapacityBits = CheckedMath.Multiply(destination.Length, 8);
        UsesMemory = true;
    }

    internal static BitWriterCore CreateStream(System.IO.Stream destination, long maximumBytes, RawBitOrder bitOrder)
    {
        BitUtility.ValidateBitOrder(bitOrder);
        if (destination == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An output stream is required.", -1);
        }
        if (!destination.CanWrite)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The output stream must be writable.", -1);
        }
        if (maximumBytes < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The output byte limit cannot be negative.", -1);
        }

        BitWriterCore writer = new BitWriterCore(default(System.Memory<byte>), bitOrder);
        writer.Stream = destination;
        writer.Memory = default(System.Memory<byte>);
        writer.CapacityBits = CheckedMath.Multiply(maximumBytes, 8);
        writer.UsesMemory = false;
        return writer;
    }

    internal void WriteBits(uint value, int count)
    {
        BitUtility.ValidateBitCount(count);
        if (count < 32 && value > BitUtility.Mask(count))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The value does not fit in the requested bit count.", -1);
        }

        long nextPosition = CheckedMath.Add(BitPosition, count);
        if (nextPosition > CapacityBits)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The bit write exceeds the bounded output capacity.", BytesWritten);
        }
        if (count == 0)
        {
            return;
        }

        ulong maskedValue = value & BitUtility.Mask(count);
        if (BitOrder == RawBitOrder.MostSignificantBitFirst)
        {
            Buffer = (Buffer << count) | maskedValue;
        }
        else
        {
            Buffer |= maskedValue << BufferedBitCount;
        }
        BufferedBitCount += count;
        BitPosition = nextPosition;
        FlushWholeBytes();
    }

    internal void AlignToByte(int paddingBit)
    {
        BitUtility.ValidatePaddingBit(paddingBit);
        int remainder = (int)(BitPosition % 8);
        if (remainder == 0)
        {
            return;
        }
        int count = 8 - remainder;
        uint value = paddingBit == 0 ? 0U : (uint)BitUtility.Mask(count);
        WriteBits(value, count);
    }

    internal long Finish(int paddingBit)
    {
        AlignToByte(paddingBit);
        return BytesWritten;
    }

    internal long GetBitPosition()
    {
        return BitPosition;
    }

    internal long GetBytesWritten()
    {
        return BytesWritten;
    }

    internal long GetRemainingBitCapacity()
    {
        return CapacityBits - BitPosition;
    }

    internal void FlushWholeBytes()
    {
        while (BufferedBitCount >= 8)
        {
            byte value;
            if (BitOrder == RawBitOrder.MostSignificantBitFirst)
            {
                value = (byte)(Buffer >> (BufferedBitCount - 8));
                BufferedBitCount -= 8;
                Buffer &= BitUtility.Mask(BufferedBitCount);
            }
            else
            {
                value = (byte)(Buffer & 255UL);
                Buffer >>= 8;
                BufferedBitCount -= 8;
            }
            WriteByte(value);
        }
    }

    internal void WriteByte(byte value)
    {
        if (UsesMemory)
        {
            int index = CheckedMath.ToInt32(BytesWritten);
            Memory.Span[index] = value;
        }
        else
        {
            (Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).WriteByte(value);
        }
        BytesWritten = CheckedMath.Add(BytesWritten, 1);
    }
}
