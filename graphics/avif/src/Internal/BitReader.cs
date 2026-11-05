using System;

namespace avif;

internal class BitReader
{
    internal long BitPosition;
    internal long BitLimit;
    internal AvifResultCode Code;

    internal static BitReader Create(int byteLength)
    {
        long bitLimit = byteLength < 0 ? 0 : (long)byteLength * 8;
        return new BitReader(byteLength, bitLimit);
    }

    internal BitReader(int byteLength, long bitLimit)
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

    internal int ReadBit(ReadOnlySpan<byte> source)
    {
        if (!CanReadBits(source, 1))
        {
            return 0;
        }

        int value = ReadBitAt(source, BitPosition);
        BitPosition++;
        return value;
    }

    internal ulong ReadBits(ReadOnlySpan<byte> source, int bitCount)
    {
        if (bitCount < 0 || bitCount > 64)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return 0;
        }

        if (!CanReadBits(source, bitCount))
        {
            return 0;
        }

        ulong value = ReadBitsAt(source, BitPosition, bitCount);
        BitPosition += bitCount;
        return value;
    }

    internal long ReadSignedBits(ReadOnlySpan<byte> source, int bitCount)
    {
        if (bitCount <= 0 || bitCount > 64)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return 0;
        }

        ulong value = ReadBits(source, bitCount);
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }

        if (bitCount == 64)
        {
            return IntegerConversion.ToInt64(value);
        }

        ulong signBit = 1UL << (bitCount - 1);
        if ((value & signBit) != 0)
        {
            value |= ulong.MaxValue << bitCount;
        }

        return IntegerConversion.ToInt64(value);
    }

    internal uint ReadUvlc(ReadOnlySpan<byte> source)
    {
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }

        long cursor = BitPosition;
        int leadingZeros = 0;
        while (true)
        {
            if (!CanReadBitsAt(source, cursor, 1))
            {
                return 0;
            }

            int bit = ReadBitAt(source, cursor);
            cursor++;
            if (bit == 1)
            {
                break;
            }

            leadingZeros++;
            if (leadingZeros > 32)
            {
                Code = AvifResultCode.InvalidData;
                return 0;
            }
        }

        if (leadingZeros == 32)
        {
            BitPosition = cursor;
            return uint.MaxValue;
        }

        if (!CanReadBitsAt(source, cursor, leadingZeros))
        {
            return 0;
        }

        ulong suffix = ReadBitsAt(source, cursor, leadingZeros);
        ulong value = ((1UL << leadingZeros) - 1) + suffix;
        BitPosition = cursor + leadingZeros;
        return (uint)value;
    }

    internal uint ReadNs(ReadOnlySpan<byte> source, uint valueCount)
    {
        if (valueCount == 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return 0;
        }

        if (Code != AvifResultCode.Success)
        {
            return 0;
        }

        int width = 0;
        uint remaining = valueCount;
        while (remaining != 0)
        {
            width++;
            remaining >>= 1;
        }

        ulong threshold = (1UL << width) - valueCount;
        long initialPosition = BitPosition;
        ulong prefix = ReadBits(source, width - 1);
        if (Code != AvifResultCode.Success)
        {
            BitPosition = initialPosition;
            return 0;
        }

        if (prefix < threshold)
        {
            return (uint)prefix;
        }

        int extra = ReadBit(source);
        if (Code != AvifResultCode.Success)
        {
            BitPosition = initialPosition;
            return 0;
        }

        return (uint)((prefix << 1) - threshold + (uint)extra);
    }

    internal AvifResultCode AlignToByte(ReadOnlySpan<byte> source, bool requireZeroBits)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        int padding = (int)((8 - BitPosition % 8) % 8);
        if (!CanReadBits(source, padding))
        {
            return Code;
        }

        if (requireZeroBits)
        {
            int index = 0;
            while (index < padding)
            {
                if (ReadBitAt(source, BitPosition + index) != 0)
                {
                    Code = AvifResultCode.InvalidData;
                    return Code;
                }

                index++;
            }
        }

        BitPosition += padding;
        return Code;
    }

    internal AvifResultCode ReadTrailingBits(ReadOnlySpan<byte> source)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        int zeroBitCount = (int)((8 - (BitPosition + 1) % 8) % 8);
        int totalBitCount = zeroBitCount + 1;
        if (!CanReadBits(source, totalBitCount))
        {
            return Code;
        }

        if (BitPosition + totalBitCount != BitLimit)
        {
            Code = AvifResultCode.InvalidData;
            return Code;
        }

        if (ReadBitAt(source, BitPosition) != 1)
        {
            Code = AvifResultCode.InvalidData;
            return Code;
        }

        int index = 1;
        while (index < totalBitCount)
        {
            if (ReadBitAt(source, BitPosition + index) != 0)
            {
                Code = AvifResultCode.InvalidData;
                return Code;
            }

            index++;
        }

        BitPosition += totalBitCount;
        return Code;
    }

    internal bool CanReadBits(ReadOnlySpan<byte> source, int bitCount)
    {
        return CanReadBitsAt(source, BitPosition, bitCount);
    }

    internal bool CanReadBitsAt(ReadOnlySpan<byte> source, long position, int bitCount)
    {
        if (Code != AvifResultCode.Success)
        {
            return false;
        }

        if (position < 0 || bitCount < 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        if (position > BitLimit || bitCount > BitLimit - position)
        {
            Code = AvifResultCode.TruncatedData;
            return false;
        }

        long finalPosition = position + bitCount;
        long requiredBytes = finalPosition / 8;
        if (finalPosition % 8 != 0)
        {
            requiredBytes++;
        }

        if (requiredBytes > source.Length)
        {
            Code = AvifResultCode.TruncatedData;
            return false;
        }

        return true;
    }

    internal int ReadBitAt(ReadOnlySpan<byte> source, long position)
    {
        int byteIndex = (int)(position / 8);
        int shift = 7 - (int)(position % 8);
        return source[byteIndex] >> shift & 1;
    }

    internal ulong ReadBitsAt(ReadOnlySpan<byte> source, long position, int bitCount)
    {
        ulong value = 0;
        int index = 0;
        while (index < bitCount)
        {
            value = value << 1 | (uint)ReadBitAt(source, position + index);
            index++;
        }

        return value;
    }
}
