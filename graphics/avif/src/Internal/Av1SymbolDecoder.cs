using System;

namespace avif;

// AV1 specification sections 8.2 and 8.3.
internal class Av1Cdf
{
    internal ushort[] Values;
    internal int Offset;
    internal int SymbolCount;

    internal Av1Cdf(ushort[] values, int offset, int symbolCount)
    {
        Values = values;
        Offset = offset;
        SymbolCount = symbolCount;
    }

    internal bool IsValid()
    {
        if (Values == null || Offset < 0 || SymbolCount < 2 || SymbolCount > 16
            || Offset > Values.Length - SymbolCount - 1)
        {
            return false;
        }

        int previous = 0;
        int index = 0;
        while (index < SymbolCount)
        {
            int value = Values[Offset + index];
            if (value < previous || value > Av1SyntaxConstants.CdfProbabilityTop)
            {
                return false;
            }

            previous = value;
            index++;
        }

        return previous == Av1SyntaxConstants.CdfProbabilityTop
            && Values[Offset + SymbolCount] <= Av1SyntaxConstants.CdfCountTop;
    }

    internal int GetValue(int index)
    {
        return Values[Offset + index];
    }

    internal void SetValue(int index, int value)
    {
        Values[Offset + index] = (ushort)value;
    }
}

internal class Av1SymbolDecoder
{
    static readonly ushort[] booleanCdfValues = new ushort[]
    {
        1 << 14,
        1 << 15,
        0
    };

    static readonly Av1Cdf booleanCdf = new Av1Cdf(booleanCdfValues, 0, 2);

    internal byte[] Source;
    internal int SourceOffset;
    internal int SourceSize;
    internal long BitPosition;
    internal int SymbolRange;
    internal uint SymbolValue;
    internal long SymbolMaxBits;
    internal bool DisableCdfUpdate;
    internal bool Exited;
    internal AvifResultCode Code;

    internal Av1SymbolDecoder(
        byte[] source,
        int sourceOffset,
        int sourceSize,
        bool disableCdfUpdate)
    {
        Source = source;
        SourceOffset = sourceOffset;
        SourceSize = sourceSize;
        DisableCdfUpdate = disableCdfUpdate;
        SymbolRange = 1 << 15;
        if (source == null || sourceOffset < 0 || sourceSize < 0
            || sourceOffset > (source == null ? 0 : source.Length))
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }

        if (sourceSize > (source ?? throw new InvalidOperationException("Invalid internal state.")).Length - sourceOffset)
        {
            Code = AvifResultCode.TruncatedData;
            return;
        }

        Code = AvifResultCode.Success;
        int numBits = sourceSize < 2 ? sourceSize * 8 : 15;
        uint buffer = ReadInputBits(numBits);
        uint paddedBuffer = buffer << (15 - numBits);
        SymbolValue = ((1U << 15) - 1) ^ paddedBuffer;
        SymbolMaxBits = (long)sourceSize * 8 - 15;
    }

    internal int ReadBoolean()
    {
        return DecodeSymbol(booleanCdf, false);
    }

    internal ulong ReadLiteral(int bitCount)
    {
        if (bitCount < 0 || bitCount > 64)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return 0;
        }

        ulong value = 0;
        int index = 0;
        while (index < bitCount && Code == AvifResultCode.Success)
        {
            value = value * 2 + (uint)ReadBoolean();
            index++;
        }

        return value;
    }

    internal uint ReadNs(uint valueCount)
    {
        if (valueCount == 0)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return 0;
        }

        if (valueCount == 1 || Code != AvifResultCode.Success)
        {
            return 0;
        }

        int width = FloorLog2(valueCount) + 1;
        ulong threshold = (1UL << width) - valueCount;
        ulong prefix = ReadLiteral(width - 1);
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }

        if (prefix < threshold)
        {
            return (uint)prefix;
        }

        return (uint)((prefix << 1) - threshold + (uint)ReadBoolean());
    }

    internal int ReadSymbol(ushort[] cdf)
    {
        if (cdf == null)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return 0;
        }

        return DecodeSymbol(new Av1Cdf(cdf, 0, cdf.Length - 1), true);
    }

    internal int ReadCdfSymbol(Av1Cdf? cdf)
    {
        return DecodeSymbol(cdf, true);
    }

    internal AvifResultCode Exit()
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (Exited)
        {
            SetCode(AvifResultCode.InvalidState);
            return Code;
        }

        if (SymbolMaxBits < -14)
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        int bufferedBits = SymbolMaxBits < 0 ? (int)(SymbolMaxBits + 15) : 15;
        long trailingBitPosition = BitPosition - bufferedBits;
        long paddingEndPosition = BitPosition + (SymbolMaxBits > 0 ? SymbolMaxBits : 0);
        if (trailingBitPosition < 0
            || trailingBitPosition >= paddingEndPosition
            || paddingEndPosition != (long)SourceSize * 8
            || ReadSourceBit(trailingBitPosition) != 1)
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        long position = trailingBitPosition + 1;
        while (position < paddingEndPosition)
        {
            if (ReadSourceBit(position) != 0)
            {
                SetCode(AvifResultCode.InvalidAv1Bitstream);
                return Code;
            }

            position++;
        }

        BitPosition = paddingEndPosition;
        Exited = true;
        return Code;
    }

    int DecodeSymbol(Av1Cdf? cdf, bool updateCdf)
    {
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }

        if (Exited)
        {
            SetCode(AvifResultCode.InvalidState);
            return 0;
        }

        if (cdf == null || !cdf.IsValid())
        {
            SetCode(AvifResultCode.InvalidArgument);
            return 0;
        }

        int current = SymbolRange;
        int previous = current;
        int symbol = 0;
        while (symbol < cdf.SymbolCount)
        {
            previous = current;
            int frequency = Av1SyntaxConstants.CdfProbabilityTop - cdf.GetValue(symbol);
            current = ((SymbolRange >> 8)
                * (frequency >> Av1SyntaxConstants.EntropyCoderProbabilityShift))
                >> (7 - Av1SyntaxConstants.EntropyCoderProbabilityShift);
            current += Av1SyntaxConstants.EntropyCoderMinimumProbability
                * (cdf.SymbolCount - symbol - 1);
            if (SymbolValue >= (uint)current)
            {
                break;
            }

            symbol++;
        }

        if (symbol >= cdf.SymbolCount || previous <= current)
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return 0;
        }

        SymbolRange = previous - current;
        SymbolValue -= (uint)current;
        int bits = 15 - FloorLog2((uint)SymbolRange);
        SymbolRange <<= bits;
        int numBits = SymbolMaxBits <= 0
            ? 0
            : (SymbolMaxBits < bits ? (int)SymbolMaxBits : bits);
        uint newData = ReadInputBits(numBits);
        uint paddedData = newData << (bits - numBits);
        SymbolValue = paddedData ^ (((SymbolValue + 1) << bits) - 1);
        SymbolMaxBits -= bits;

        if (updateCdf && !DisableCdfUpdate)
        {
            Av1CdfUpdater.Update(cdf, symbol);
        }

        return symbol;
    }

    uint ReadInputBits(int bitCount)
    {
        uint value = 0;
        int index = 0;
        while (index < bitCount)
        {
            value = value * 2 + (uint)ReadSourceBit(BitPosition);
            BitPosition++;
            index++;
        }

        return value;
    }

    int ReadSourceBit(long position)
    {
        int byteIndex = SourceOffset + (int)(position >> 3);
        int shift = 7 - (int)(position & 7);
        return (Source[byteIndex] >> shift) & 1;
    }

    void SetCode(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }
    }

    static int FloorLog2(uint value)
    {
        int result = -1;
        while (value != 0)
        {
            result++;
            value >>= 1;
        }

        return result;
    }

}
