using System;

namespace webp;

internal class WebpVp8BoolDecoder
{
    internal byte[] Data;
    internal int InputOffset;
    internal uint Range;
    internal uint Value;
    internal int BitCount;
    internal bool IsTruncated;

    internal WebpVp8BoolDecoder(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 2)
        {
            throw new ArgumentException("VP8 boolean decoder requires at least two bytes", "data");
        }

        Data = WebpUnknownChunk.CopyBytes(data);
        Value = ((uint)Data[0] << 8) | Data[1];
        InputOffset = 2;
        Range = 255;
        BitCount = 0;
        IsTruncated = false;
    }

    internal int BytePosition()
    {
        return InputOffset;
    }

    internal int DecodeBool(int probability)
    {
        ValidateProbability(probability);

        uint split = 1U + (((Range - 1U) * (uint)probability) >> 8);
        uint largeSplit = split << 8;
        int value = 0;
        if (Value >= largeSplit)
        {
            value = 1;
            Range = Range - split;
            Value = Value - largeSplit;
        }
        else
        {
            Range = split;
        }

        Renormalize();
        return value;
    }

    internal int DecodeBit()
    {
        return DecodeBool(128);
    }

    internal uint DecodeLiteral(int bitCount)
    {
        ValidateLiteralBitCount(bitCount);

        uint value = 0;
        int index = 0;
        while (index < bitCount)
        {
            value = (value << 1) | (uint)DecodeBit();
            index = index + 1;
        }

        return value;
    }

    internal int DecodeSignedLiteral(int bitCount)
    {
        ValidateLiteralBitCount(bitCount);
        if (bitCount == 0)
        {
            return 0;
        }

        int value = 0;
        if (DecodeBit() != 0)
        {
            value = -1;
        }

        int index = 1;
        while (index < bitCount)
        {
            value = (value << 1) + DecodeBit();
            index = index + 1;
        }

        return value;
    }

    internal int DecodeTree(int[] tree, byte[] probabilities)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(probabilities);
        ValidateTree(tree, probabilities);

        int treeIndex = 0;
        while (true)
        {
            int probabilityIndex = treeIndex >> 1;
            int bit = DecodeBool(probabilities[probabilityIndex]);
            treeIndex = tree[treeIndex + bit];
            if (treeIndex <= 0)
            {
                return -treeIndex;
            }
        }
    }

    internal void Renormalize()
    {
        while (Range < 128)
        {
            Value = Value << 1;
            Range = Range << 1;
            BitCount = BitCount + 1;
            if (BitCount == 8)
            {
                BitCount = 0;
                Value = Value | ReadNextByte();
            }
        }
    }

    internal uint ReadNextByte()
    {
        if (InputOffset >= Data.Length)
        {
            IsTruncated = true;
            return 0;
        }

        uint value = Data[InputOffset];
        InputOffset = InputOffset + 1;
        return value;
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
        if (bitCount < 0 || bitCount > 32)
        {
            throw new ArgumentOutOfRangeException("bitCount");
        }
    }

    internal static void ValidateTree(int[] tree, byte[] probabilities)
    {
        if (tree.Length < 2 || (tree.Length & 1) != 0)
        {
            throw new ArgumentException("VP8 bool tree must contain branch pairs", "tree");
        }

        if (probabilities.Length * 2 < tree.Length)
        {
            throw new ArgumentException("VP8 bool tree probabilities must cover every interior node", "probabilities");
        }
    }
}
