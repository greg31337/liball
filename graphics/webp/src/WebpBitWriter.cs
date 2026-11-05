using System;
using System.Collections.Generic;

namespace webp;

internal class WebpBitWriter
{
    internal List<byte> Data;
    internal int BitPosition;
    internal WebpBitOrder BitOrder;

    internal WebpBitWriter(WebpBitOrder bitOrder)
    {
        Data = new List<byte>();
        BitPosition = 0;
        BitOrder = bitOrder;
    }

    internal int ByteLength()
    {
        return Data.Count;
    }

    internal bool IsByteAligned()
    {
        return (BitPosition & 7) == 0;
    }

    internal void AlignToByte()
    {
        while (!IsByteAligned())
        {
            WriteBit(0);
        }
    }

    internal void WriteBit(int bit)
    {
        if (bit != 0 && bit != 1)
        {
            throw new ArgumentOutOfRangeException("bit");
        }

        int byteIndex = BitPosition / 8;
        if (byteIndex == Data.Count)
        {
            Data.Add(0);
        }

        if (bit != 0)
        {
            int bitOffset = BitPosition & 7;
            if (BitOrder == WebpBitOrder.MostSignificantBitFirst)
            {
                bitOffset = 7 - bitOffset;
            }

            Data[byteIndex] = (byte)(Data[byteIndex] | (1 << bitOffset));
        }

        BitPosition = BitPosition + 1;
    }

    internal void WriteBits(uint value, int bitCount)
    {
        RequireBitCount(bitCount);

        if (BitOrder == WebpBitOrder.LeastSignificantBitFirst)
        {
            int index = 0;
            while (index < bitCount)
            {
                WriteBit((int)((value >> index) & 1U));
                index = index + 1;
            }
        }
        else
        {
            int index = bitCount - 1;
            while (index >= 0)
            {
                WriteBit((int)((value >> index) & 1U));
                index = index - 1;
            }
        }
    }

    internal byte[] ToArray()
    {
        return Data.ToArray();
    }

    internal static void RequireBitCount(int bitCount)
    {
        if (bitCount < 0 || bitCount > 32)
        {
            throw new ArgumentOutOfRangeException("bitCount");
        }
    }
}
