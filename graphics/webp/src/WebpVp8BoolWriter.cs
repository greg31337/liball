using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8BoolWriter
{
    internal List<byte> Bytes;
    internal int Range;
    internal long Value;
    internal int Run;
    internal int PendingBits;

    internal WebpVp8BoolWriter()
    {
        Bytes = new List<byte>();
        Range = 254;
        Value = 0;
        Run = 0;
        PendingBits = -8;
    }

    internal void WriteBool(int probability, int bit)
    {
        ValidateProbability(probability);

        int split = (Range * probability) >> 8;
        if (bit != 0)
        {
            Value = Value + split + 1;
            Range = Range - split - 1;
        }
        else
        {
            Range = split;
        }

        if (Range < 127)
        {
            int shift = FindNormalizationShift(Range);
            Range = ((Range + 1) << shift) - 1;
            Value = Value << shift;
            PendingBits = PendingBits + shift;
            if (PendingBits > 0)
            {
                Flush();
            }
        }
    }

    internal void WriteBit(int bit)
    {
        WriteBool(128, bit);
    }

    internal void WriteLiteral(int value, int bitCount)
    {
        ValidateLiteralBitCount(bitCount);

        int index = bitCount - 1;
        while (index >= 0)
        {
            WriteBit((value >> index) & 1);
            index = index - 1;
        }
    }

    internal byte[] ToBytes()
    {
        int bitCount = 9 - PendingBits;
        while (bitCount > 0)
        {
            WriteBit(0);
            bitCount = bitCount - 1;
        }

        PendingBits = 0;
        Flush();
        return Bytes.ToArray();
    }

    internal void Flush()
    {
        int shift = 8 + PendingBits;
        long bits = Value >> shift;
        Value = Value - (bits << shift);
        PendingBits = PendingBits - 8;
        if ((bits & 255) != 255)
        {
            if ((bits & 256) != 0 && Bytes.Count > 0)
            {
                Bytes[Bytes.Count - 1] = (byte)(Bytes[Bytes.Count - 1] + 1);
            }

            byte pending = (bits & 256) != 0 ? (byte)0 : (byte)255;
            while (Run > 0)
            {
                Bytes.Add(pending);
                Run = Run - 1;
            }

            Bytes.Add((byte)(bits & 255));
        }
        else
        {
            Run = Run + 1;
        }
    }

    internal static int FindNormalizationShift(int range)
    {
        int shift = 0;
        while (((range + 1) << shift) < 128)
        {
            shift = shift + 1;
        }

        return shift;
    }

    internal static void ValidateProbability(int probability)
    {
        if (probability < 1 || probability > 255)
        {
            throw new ArgumentOutOfRangeException("probability");
        }
    }

    internal static void ValidateLiteralBitCount(int bitCount)
    {
        if (bitCount < 0 || bitCount > 31)
        {
            throw new ArgumentOutOfRangeException("bitCount");
        }
    }
}
