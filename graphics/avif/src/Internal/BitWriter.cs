using System;

namespace avif;

internal class BitWriter
{
    internal long BitPosition;
    internal long BitLimit;
    internal AvifResultCode Code;

    internal static BitWriter Create(int byteLength)
    {
        long bitLimit = byteLength < 0 ? 0 : (long)byteLength * 8;
        return new BitWriter(byteLength, bitLimit);
    }

    internal BitWriter(int byteLength, long bitLimit)
    {
        BitPosition = 0;
        long availableBits = byteLength < 0 ? 0 : (long)byteLength * 8;
        if (byteLength < 0 || bitLimit < 0 || bitLimit > availableBits)
        {
            BitLimit = 0;
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            BitLimit = bitLimit;
            Code = AvifResultCode.Success;
        }
    }

    internal long RemainingBits()
    {
        return BitLimit - BitPosition;
    }

    internal bool IsByteAligned()
    {
        return BitPosition % 8 == 0;
    }

    internal AvifResultCode WriteBit(Span<byte> destination, int value)
    {
        if (value != 0 && value != 1)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWriteBits(destination, 1))
        {
            WriteBitAt(destination, BitPosition, value);
            BitPosition++;
        }

        return Code;
    }

    internal AvifResultCode WriteBits(Span<byte> destination, ulong value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 64)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (bitCount < 64 && value >> bitCount != 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (!CanWriteBits(destination, bitCount))
        {
            return Code;
        }

        int index = 0;
        while (index < bitCount)
        {
            int shift = bitCount - index - 1;
            int bit = (int)(value >> shift & 1);
            WriteBitAt(destination, BitPosition + index, bit);
            index++;
        }

        BitPosition += bitCount;
        return Code;
    }

    internal AvifResultCode WriteSignedBits(Span<byte> destination, long value, int bitCount)
    {
        if (bitCount <= 0 || bitCount > 64)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (bitCount < 64)
        {
            long minimum = -(1L << (bitCount - 1));
            long maximum = (1L << (bitCount - 1)) - 1;
            if (value < minimum || value > maximum)
            {
                if (Code == AvifResultCode.Success)
                {
                    Code = AvifResultCode.InvalidArgument;
                }

                return Code;
            }

            ulong mask = (1UL << bitCount) - 1;
            return WriteBits(destination,
                IntegerConversion.ToUInt64(value) & mask,
                bitCount);
        }

        return WriteBits(destination, IntegerConversion.ToUInt64(value), bitCount);
    }

    internal AvifResultCode WriteUvlc(Span<byte> destination, uint value)
    {
        int leadingZeros;
        ulong codeNumber;
        if (value == uint.MaxValue)
        {
            leadingZeros = 32;
            codeNumber = 1;
        }
        else
        {
            codeNumber = (ulong)value + 1;
            leadingZeros = 0;
            ulong remaining = codeNumber;
            while (remaining > 1)
            {
                leadingZeros++;
                remaining >>= 1;
            }
        }

        int totalBitCount = leadingZeros == 32 ? 33 : leadingZeros * 2 + 1;
        if (!CanWriteBits(destination, totalBitCount))
        {
            return Code;
        }

        int index = 0;
        while (index < leadingZeros)
        {
            WriteBitAt(destination, BitPosition + index, 0);
            index++;
        }

        if (leadingZeros == 32)
        {
            WriteBitAt(destination, BitPosition + 32, 1);
        }
        else
        {
            int codeBitCount = leadingZeros + 1;
            index = 0;
            while (index < codeBitCount)
            {
                int shift = codeBitCount - index - 1;
                int bit = (int)(codeNumber >> shift & 1);
                WriteBitAt(destination, BitPosition + leadingZeros + index, bit);
                index++;
            }
        }

        BitPosition += totalBitCount;
        return Code;
    }

    internal AvifResultCode WriteNs(Span<byte> destination, uint value, uint valueCount)
    {
        if (valueCount == 0 || value >= valueCount)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        int width = 0;
        uint remaining = valueCount;
        while (remaining != 0)
        {
            width++;
            remaining >>= 1;
        }

        ulong threshold = (1UL << width) - valueCount;
        if (value < threshold)
        {
            return WriteBits(destination, value, width - 1);
        }

        ulong encoded = value + threshold;
        if (!CanWriteBits(destination, width))
        {
            return Code;
        }

        WriteBits(destination, encoded >> 1, width - 1);
        WriteBit(destination, (int)(encoded & 1));
        return Code;
    }

    internal AvifResultCode AlignToByte(Span<byte> destination, int fillBit)
    {
        if (fillBit != 0 && fillBit != 1)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        int padding = (int)((8 - BitPosition % 8) % 8);
        if (!CanWriteBits(destination, padding))
        {
            return Code;
        }

        int index = 0;
        while (index < padding)
        {
            WriteBitAt(destination, BitPosition + index, fillBit);
            index++;
        }

        BitPosition += padding;
        return Code;
    }

    internal AvifResultCode WriteTrailingBits(Span<byte> destination)
    {
        int zeroBitCount = (int)((8 - (BitPosition + 1) % 8) % 8);
        int totalBitCount = zeroBitCount + 1;
        if (!CanWriteBits(destination, totalBitCount))
        {
            return Code;
        }

        WriteBitAt(destination, BitPosition, 1);
        int index = 1;
        while (index < totalBitCount)
        {
            WriteBitAt(destination, BitPosition + index, 0);
            index++;
        }

        BitPosition += totalBitCount;
        return Code;
    }

    internal bool CanWriteBits(Span<byte> destination, int bitCount)
    {
        if (Code != AvifResultCode.Success)
        {
            return false;
        }

        if (bitCount < 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        if (BitPosition > BitLimit || bitCount > BitLimit - BitPosition)
        {
            Code = AvifResultCode.BufferTooSmall;
            return false;
        }

        long finalPosition = BitPosition + bitCount;
        long requiredBytes = finalPosition / 8;
        if (finalPosition % 8 != 0)
        {
            requiredBytes++;
        }

        if (requiredBytes > destination.Length)
        {
            Code = AvifResultCode.BufferTooSmall;
            return false;
        }

        return true;
    }

    internal void WriteBitAt(Span<byte> destination, long position, int value)
    {
        int byteIndex = (int)(position / 8);
        int shift = 7 - (int)(position % 8);
        byte mask = (byte)(1 << shift);
        if (value == 0)
        {
            destination[byteIndex] = (byte)(destination[byteIndex] & ~mask);
        }
        else
        {
            destination[byteIndex] = (byte)(destination[byteIndex] | mask);
        }
    }
}
