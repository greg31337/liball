using System.Collections.Generic;

namespace avif;

internal static class Av1CdfUpdater
{
    internal static AvifResultCode Update(Av1Cdf cdf, int symbol)
    {
        if (cdf == null || !cdf.IsValid()
            || symbol < 0 || symbol >= cdf.SymbolCount)
        {
            return AvifResultCode.InvalidArgument;
        }

        int count = cdf.GetValue(cdf.SymbolCount);
        int rate = 3 + (count > 15 ? 1 : 0) + (count > 31 ? 1 : 0)
            + Minimum(FloorLog2((uint)cdf.SymbolCount), 2);
        int target = 0;
        int index = 0;
        while (index < cdf.SymbolCount - 1)
        {
            if (index == symbol)
            {
                target = Av1SyntaxConstants.CdfProbabilityTop;
            }

            int value = cdf.GetValue(index);
            if (target < value)
            {
                value -= (value - target) >> rate;
            }
            else
            {
                value += (target - value) >> rate;
            }

            cdf.SetValue(index, value);
            index++;
        }

        if (count < Av1SyntaxConstants.CdfCountTop)
        {
            cdf.SetValue(cdf.SymbolCount, count + 1);
        }

        return AvifResultCode.Success;
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

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }
}

internal class Av1SymbolEncoderStep
{
    internal int Lower;
    internal int Upper;
    internal int RenormalizationBits;

    internal Av1SymbolEncoderStep(int lower, int upper, int renormalizationBits)
    {
        Lower = lower;
        Upper = upper;
        RenormalizationBits = renormalizationBits;
    }
}

// AV1 specification sections 8.2 and 8.3.
internal class Av1SymbolEncoder
{
    static readonly ushort[] booleanCdfValues = new ushort[]
    {
        1 << 14,
        1 << 15,
        0
    };

    static readonly Av1Cdf booleanCdf = new Av1Cdf(booleanCdfValues, 0, 2);

    internal int SymbolRange;
    internal bool DisableCdfUpdate;
    internal bool Exited;
    internal AvifResultCode Code;
    List<Av1SymbolEncoderStep> steps;
    byte[] output;
    int renormalizationBitCount;

    internal Av1SymbolEncoder(bool disableCdfUpdate)
    {
        DisableCdfUpdate = disableCdfUpdate;
        SymbolRange = 1 << 15;
        Code = AvifResultCode.Success;
        steps = new List<Av1SymbolEncoderStep>();
        output = System.Array.Empty<byte>();
    }

    internal AvifResultCode WriteBoolean(int value)
    {
        if (value != 0 && value != 1)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        EncodeSymbol(booleanCdf, value, false);
        return Code;
    }

    internal AvifResultCode WriteLiteral(ulong value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 64
            || (bitCount < 64 && value >> bitCount != 0))
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        int index = bitCount - 1;
        while (index >= 0 && Code == AvifResultCode.Success)
        {
            WriteBoolean((int)(value >> index & 1));
            index--;
        }

        return Code;
    }

    internal AvifResultCode WriteNs(uint value, uint valueCount)
    {
        if (valueCount == 0 || value >= valueCount)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        if (valueCount == 1)
        {
            return Code;
        }

        int width = FloorLog2(valueCount) + 1;
        ulong threshold = (1UL << width) - valueCount;
        if (value < threshold)
        {
            return WriteLiteral(value, width - 1);
        }

        ulong encoded = value + threshold;
        WriteLiteral(encoded >> 1, width - 1);
        WriteBoolean((int)(encoded & 1));
        return Code;
    }

    internal AvifResultCode WriteSymbol(ushort[] cdf, int symbol)
    {
        if (cdf == null)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        EncodeSymbol(new Av1Cdf(cdf, 0, cdf.Length - 1), symbol, true);
        return Code;
    }

    internal AvifResultCode WriteCdfSymbol(Av1Cdf cdf, int symbol)
    {
        EncodeSymbol(cdf, symbol, true);
        return Code;
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

        if (steps.Count == 0)
        {
            output = new byte[] { 128 };
            Exited = true;
            return Code;
        }

        int byteCount = (renormalizationBitCount + 8) / 8;
        int finalValue = 0;
        while (finalValue < SymbolRange)
        {
            byte[]? candidate = CreateCandidate(finalValue, byteCount);
            if (candidate != null)
            {
                output = candidate;
                Exited = true;
                return Code;
            }

            finalValue++;
        }

        SetCode(AvifResultCode.InvalidState);
        return Code;
    }

    internal byte[] GetOutput()
    {
        return (byte[])output.Clone();
    }

    void EncodeSymbol(Av1Cdf cdf, int symbol, bool updateCdf)
    {
        if (Code != AvifResultCode.Success)
        {
            return;
        }

        if (Exited)
        {
            SetCode(AvifResultCode.InvalidState);
            return;
        }

        if (cdf == null || !cdf.IsValid()
            || symbol < 0 || symbol >= cdf.SymbolCount)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return;
        }

        int current = SymbolRange;
        int previous = current;
        int index = 0;
        while (index <= symbol)
        {
            previous = current;
            int frequency = Av1SyntaxConstants.CdfProbabilityTop
                - cdf.GetValue(index);
            current = ((SymbolRange >> 8)
                * (frequency >> Av1SyntaxConstants.EntropyCoderProbabilityShift))
                >> (7 - Av1SyntaxConstants.EntropyCoderProbabilityShift);
            current += Av1SyntaxConstants.EntropyCoderMinimumProbability
                * (cdf.SymbolCount - index - 1);
            index++;
        }

        int symbolRange = previous - current;
        if (symbolRange <= 0)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return;
        }

        int bits = 15 - FloorLog2((uint)symbolRange);
        if (renormalizationBitCount > int.MaxValue - bits)
        {
            SetCode(AvifResultCode.LimitExceeded);
            return;
        }

        steps.Add(new Av1SymbolEncoderStep(current, previous, bits));
        renormalizationBitCount += bits;
        SymbolRange = symbolRange << bits;
        if (updateCdf && !DisableCdfUpdate)
        {
            SetCode(Av1CdfUpdater.Update(cdf, symbol));
        }
    }

    byte[]? CreateCandidate(int finalValue, int byteCount)
    {
        int[] chunks = new int[steps.Count];
        int value = finalValue;
        int stepIndex = steps.Count - 1;
        while (stepIndex >= 0)
        {
            Av1SymbolEncoderStep step = steps[stepIndex];
            int scale = 1 << step.RenormalizationBits;
            int quotient = value >> step.RenormalizationBits;
            int remainder = value & (scale - 1);
            value = step.Lower + quotient;
            if (value < step.Lower || value >= step.Upper)
            {
                return null;
            }

            chunks[stepIndex] = scale - 1 - remainder;
            stepIndex--;
        }

        int conceptualBitCount = 15 + renormalizationBitCount;
        byte[] conceptual = new byte[(conceptualBitCount + 7) / 8];
        int initialBits = (1 << 15) - 1 - value;
        WriteBits(conceptual, 0, initialBits, 15);
        int bitPosition = 15;
        stepIndex = 0;
        while (stepIndex < steps.Count)
        {
            int bits = steps[stepIndex].RenormalizationBits;
            WriteBits(conceptual, bitPosition, chunks[stepIndex], bits);
            bitPosition += bits;
            stepIndex++;
        }

        if (ReadBit(conceptual, renormalizationBitCount) != 1)
        {
            return null;
        }

        bitPosition = renormalizationBitCount + 1;
        while (bitPosition < conceptualBitCount)
        {
            if (ReadBit(conceptual, bitPosition) != 0)
            {
                return null;
            }

            bitPosition++;
        }

        byte[] candidate = new byte[byteCount];
        bitPosition = 0;
        while (bitPosition < byteCount * 8)
        {
            if (ReadBit(conceptual, bitPosition) != 0)
            {
                WriteBit(candidate, bitPosition, 1);
            }

            bitPosition++;
        }

        return candidate;
    }

    static void WriteBits(byte[] destination, int position, int value, int bitCount)
    {
        int index = 0;
        while (index < bitCount)
        {
            int shift = bitCount - index - 1;
            WriteBit(destination, position + index, value >> shift & 1);
            index++;
        }
    }

    static void WriteBit(byte[] destination, int position, int value)
    {
        if (value != 0)
        {
            destination[position >> 3] = (byte)(destination[position >> 3]
                | 1 << (7 - (position & 7)));
        }
    }

    static int ReadBit(byte[] source, int position)
    {
        return source[position >> 3] >> (7 - (position & 7)) & 1;
    }

    void SetCode(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success && code != AvifResultCode.Success)
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
