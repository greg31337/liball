using System;

namespace webp;

internal class WebpBitReader
{
    internal byte[] Data;
    internal int BitLength;
    internal int BitPosition;
    internal WebpBitOrder BitOrder;

    internal WebpBitReader(byte[] data, int offset, int length, WebpBitOrder bitOrder)
    {
        ArgumentNullException.ThrowIfNull(data);

        Data = WebpEndianReader.ReadSlice(data, offset, length).ToArray();
        BitLength = Data.Length * 8;
        BitPosition = 0;
        BitOrder = bitOrder;
    }

    internal static WebpBitReader FromBytes(byte[] data, WebpBitOrder bitOrder)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new WebpBitReader(data, 0, data.Length, bitOrder);
    }

    internal int BitsRemaining()
    {
        return BitLength - BitPosition;
    }

    internal bool HasBits(int bitCount)
    {
        RequireBitCount(bitCount);
        return bitCount <= BitsRemaining();
    }

    internal int BytePosition()
    {
        return BitPosition / 8;
    }

    internal bool IsByteAligned()
    {
        return (BitPosition & 7) == 0;
    }

    internal void AlignToByte()
    {
        int remainder = BitPosition & 7;
        if (remainder != 0)
        {
            BitPosition = BitPosition + 8 - remainder;
        }
    }

    internal int ReadBit()
    {
        RequireAvailableBits(1);

        int byteIndex = BitPosition / 8;
        int bitOffset = BitPosition & 7;
        if (BitOrder == WebpBitOrder.MostSignificantBitFirst)
        {
            bitOffset = 7 - bitOffset;
        }

        int bit = (Data[byteIndex] >> bitOffset) & 1;
        BitPosition = BitPosition + 1;
        return bit;
    }

    internal uint ReadBits(int bitCount)
    {
        RequireAvailableBits(bitCount);

        uint value = 0;
        int index = 0;
        while (index < bitCount)
        {
            int bit = ReadBit();
            if (BitOrder == WebpBitOrder.LeastSignificantBitFirst)
            {
                value = value | ((uint)bit << index);
            }
            else
            {
                value = (value << 1) | (uint)bit;
            }

            index = index + 1;
        }

        return value;
    }

    internal static void RequireBitCount(int bitCount)
    {
        if (bitCount < 0 || bitCount > 32)
        {
            throw new ArgumentOutOfRangeException("bitCount");
        }
    }

    internal void RequireAvailableBits(int bitCount)
    {
        RequireBitCount(bitCount);
        if (bitCount > BitsRemaining())
        {
            throw new ArgumentException("requested bits extend beyond the input buffer", "bitCount");
        }
    }
}
