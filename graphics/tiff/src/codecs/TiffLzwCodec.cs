using System;

namespace tiff;

internal class TiffLzwCodec
{
    internal const int ClearCode = 256;
    internal const int EndOfInformationCode = 257;
    internal const int FirstDictionaryCode = 258;
    internal const int MaximumCodeCount = 4096;
    internal const int MaximumCodeSize = 12;

    internal static TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = DecodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = EncodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static byte[] DecodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, source, diagnostics))
        {
            return new byte[0];
        }

        byte[] destination = new byte[(int)context.ExpectedDecodedByteCount];
        int[] prefixes = new int[MaximumCodeCount];
        byte[] suffixes = new byte[MaximumCodeCount];
        byte[] stack = new byte[MaximumCodeCount];
        InitializeDictionary(prefixes, suffixes);

        int bitOffset = 0;
        int codeSize = 9;
        int nextCode = FirstDictionaryCode;
        int previousCode = -1;
        byte previousFirstByte = 0;
        int destinationIndex = 0;
        bool sawClearCode = false;
        bool sawEndOfInformationCode = false;

        while (!sawEndOfInformationCode)
        {
            int code = ReadCode(source, bitOffset, codeSize);
            if (code < 0)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "LZW stream ended before the end-of-information code was reached.");
                return new byte[0];
            }

            bitOffset = bitOffset + codeSize;
            if (!sawClearCode && code != ClearCode)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW stream must begin with a clear code.");
                return new byte[0];
            }

            if (code == ClearCode)
            {
                InitializeDictionary(prefixes, suffixes);
                codeSize = 9;
                nextCode = FirstDictionaryCode;
                previousCode = -1;
                previousFirstByte = 0;
                sawClearCode = true;
            }
            else if (code == EndOfInformationCode)
            {
                sawEndOfInformationCode = true;
            }
            else
            {
                byte currentFirstByte = FindCurrentFirstByte(code, nextCode, previousCode, previousFirstByte, prefixes, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new byte[0];
                }

                if (previousCode >= 0)
                {
                    AddDictionaryEntry(prefixes, suffixes, previousCode, currentFirstByte, nextCode);
                    nextCode = AdvanceNextCode(nextCode);
                    codeSize = AdvanceCodeSize(codeSize, nextCode);
                }

                destinationIndex = WriteCodeBytes(code, nextCode, previousCode, previousFirstByte, prefixes, suffixes, destination, destinationIndex, stack, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new byte[0];
                }

                previousCode = code;
                previousFirstByte = currentFirstByte;
            }
        }

        if (destinationIndex != destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "LZW stream ended before the expected decoded byte count was reached.");
            return new byte[0];
        }

        return destination;
    }

    internal static byte[] EncodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateEncodeContext(context, diagnostics))
        {
            return new byte[0];
        }

        if (context.ExpectedDecodedByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW decoded byte count does not match the expected byte count.");
            return new byte[0];
        }

        int maximumEncodedByteCount = CalculateMaximumEncodedByteCount(source.Length, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        int[] prefixes = new int[MaximumCodeCount];
        byte[] suffixes = new byte[MaximumCodeCount];
        InitializeDictionary(prefixes, suffixes);

        byte[] destination = new byte[maximumEncodedByteCount];
        int bitOffset = 0;
        int codeSize = 9;
        int nextCode = FirstDictionaryCode;
        bitOffset = WriteCodeBits(destination, bitOffset, codeSize, ClearCode);

        if (source.Length > 0)
        {
            int currentCode = source[0];
            int sourceIndex = 1;
            while (sourceIndex < source.Length)
            {
                byte suffixByte = source[sourceIndex];
                int dictionaryCode = FindDictionaryCode(prefixes, suffixes, currentCode, suffixByte, nextCode);
                if (dictionaryCode >= 0)
                {
                    currentCode = dictionaryCode;
                }
                else
                {
                    bitOffset = WriteCodeBits(destination, bitOffset, codeSize, currentCode);
                    AddDictionaryEntry(prefixes, suffixes, currentCode, suffixByte, nextCode);
                    nextCode = AdvanceNextCode(nextCode);
                    codeSize = AdvanceEncodeCodeSize(codeSize, nextCode);
                    currentCode = suffixByte;
                }

                sourceIndex = sourceIndex + 1;
            }

            bitOffset = WriteCodeBits(destination, bitOffset, codeSize, currentCode);
        }

        bitOffset = WriteCodeBits(destination, bitOffset, codeSize, EndOfInformationCode);
        return TrimEncodedBytes(destination, CalculateByteCountFromBits(bitOffset));
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long maximumBitCount = ((long)sourceLength + 2) * MaximumCodeSize;
        long maximumEncodedByteCount = (maximumBitCount + 7) / 8;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "LZW encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "LZW encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static void InitializeDictionary(int[] prefixes, byte[] suffixes)
    {
        int index = 0;
        while (index < MaximumCodeCount)
        {
            prefixes[index] = -1;
            suffixes[index] = 0;
            index = index + 1;
        }

        index = 0;
        while (index < 256)
        {
            suffixes[index] = (byte)index;
            index = index + 1;
        }
    }

    internal static int ReadCode(ReadOnlySpan<byte> source, int bitOffset, int codeSize)
    {
        if (bitOffset + codeSize > source.Length * 8)
        {
            return -1;
        }

        int value = 0;
        int bitIndex = 0;
        while (bitIndex < codeSize)
        {
            int absoluteBitIndex = bitOffset + bitIndex;
            int sourceByte = source[absoluteBitIndex / 8];
            int shift = 7 - absoluteBitIndex % 8;
            int bit = (sourceByte >> shift) & 1;
            value = value * 2 + bit;
            bitIndex = bitIndex + 1;
        }

        return value;
    }

    internal static int WriteCodeBits(byte[] destination, int bitOffset, int codeSize, int code)
    {
        int bitIndex = codeSize - 1;
        while (bitIndex >= 0)
        {
            int bit = (code >> bitIndex) & 1;
            if (bit != 0)
            {
                int destinationByteIndex = bitOffset / 8;
                int shift = 7 - bitOffset % 8;
                destination[destinationByteIndex] = (byte)(destination[destinationByteIndex] | (1 << shift));
            }

            bitOffset = bitOffset + 1;
            bitIndex = bitIndex - 1;
        }

        return bitOffset;
    }

    internal static int FindDictionaryCode(int[] prefixes, byte[] suffixes, int prefixCode, byte suffixByte, int nextCode)
    {
        int code = FirstDictionaryCode;
        while (code < nextCode && code < MaximumCodeCount)
        {
            if (prefixes[code] == prefixCode && suffixes[code] == suffixByte)
            {
                return code;
            }

            code = code + 1;
        }

        return -1;
    }

    internal static int CalculateByteCountFromBits(int bitCount)
    {
        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        return byteCount;
    }

    internal static byte[] TrimEncodedBytes(byte[] source, int length)
    {
        byte[] trimmed = new byte[length];
        int index = 0;
        while (index < length)
        {
            trimmed[index] = source[index];
            index = index + 1;
        }

        return trimmed;
    }

    internal static byte FindCurrentFirstByte(int code, int nextCode, int previousCode, byte previousFirstByte, int[] prefixes, TiffDiagnostics diagnostics)
    {
        if (code == nextCode && previousCode >= 0)
        {
            return previousFirstByte;
        }

        if (!IsKnownCode(code, nextCode))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW code is not present in the current dictionary.");
            return 0;
        }

        return FindFirstByte(code, prefixes, diagnostics);
    }

    internal static bool IsKnownCode(int code, int nextCode)
    {
        if (code >= 0 && code < 256)
        {
            return true;
        }

        if (code >= FirstDictionaryCode && code < nextCode && code < MaximumCodeCount)
        {
            return true;
        }

        return false;
    }

    internal static byte FindFirstByte(int code, int[] prefixes, TiffDiagnostics diagnostics)
    {
        int stepCount = 0;
        while (code >= 256)
        {
            if (code < 0 || code >= MaximumCodeCount || prefixes[code] < 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary entry is invalid.");
                return 0;
            }

            code = prefixes[code];
            stepCount = stepCount + 1;
            if (stepCount > MaximumCodeCount)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary chain is cyclic.");
                return 0;
            }
        }

        return (byte)code;
    }

    internal static void AddDictionaryEntry(int[] prefixes, byte[] suffixes, int prefixCode, byte suffixByte, int nextCode)
    {
        if (nextCode >= MaximumCodeCount)
        {
            return;
        }

        prefixes[nextCode] = prefixCode;
        suffixes[nextCode] = suffixByte;
    }

    internal static int AdvanceNextCode(int nextCode)
    {
        if (nextCode < MaximumCodeCount)
        {
            return nextCode + 1;
        }

        return nextCode;
    }

    internal static int AdvanceCodeSize(int codeSize, int nextCode)
    {
        if (codeSize < MaximumCodeSize && nextCode == (1 << codeSize))
        {
            return codeSize + 1;
        }

        return codeSize;
    }

    internal static int AdvanceEncodeCodeSize(int codeSize, int nextCode)
    {
        if (codeSize < MaximumCodeSize && nextCode == (1 << codeSize) + 1)
        {
            return codeSize + 1;
        }

        return codeSize;
    }

    internal static int WriteCodeBytes(int code, int nextCode, int previousCode, byte previousFirstByte, int[] prefixes, byte[] suffixes, byte[] destination, int destinationIndex, byte[] stack, TiffDiagnostics diagnostics)
    {
        if (code == nextCode - 1 && previousCode >= 0 && !IsKnownCode(code, nextCode - 1))
        {
            return WriteSpecialCodeBytes(previousCode, previousFirstByte, prefixes, suffixes, destination, destinationIndex, stack, diagnostics);
        }

        return WriteKnownCodeBytes(code, prefixes, suffixes, destination, destinationIndex, stack, diagnostics);
    }

    internal static int WriteSpecialCodeBytes(int previousCode, byte previousFirstByte, int[] prefixes, byte[] suffixes, byte[] destination, int destinationIndex, byte[] stack, TiffDiagnostics diagnostics)
    {
        int nextDestinationIndex = WriteKnownCodeBytes(previousCode, prefixes, suffixes, destination, destinationIndex, stack, diagnostics);
        if (diagnostics.HasErrors())
        {
            return destinationIndex;
        }

        if (nextDestinationIndex >= destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW code expands beyond the expected decoded byte count.");
            return destinationIndex;
        }

        destination[nextDestinationIndex] = previousFirstByte;
        return nextDestinationIndex + 1;
    }

    internal static int WriteKnownCodeBytes(int code, int[] prefixes, byte[] suffixes, byte[] destination, int destinationIndex, byte[] stack, TiffDiagnostics diagnostics)
    {
        int stackLength = BuildCodeStack(code, prefixes, suffixes, stack, diagnostics);
        if (diagnostics.HasErrors())
        {
            return destinationIndex;
        }

        if (destinationIndex > destination.Length - stackLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW code expands beyond the expected decoded byte count.");
            return destinationIndex;
        }

        int stackIndex = stackLength - 1;
        while (stackIndex >= 0)
        {
            destination[destinationIndex] = stack[stackIndex];
            destinationIndex = destinationIndex + 1;
            stackIndex = stackIndex - 1;
        }

        return destinationIndex;
    }

    internal static int BuildCodeStack(int code, int[] prefixes, byte[] suffixes, byte[] stack, TiffDiagnostics diagnostics)
    {
        int stackLength = 0;
        int stepCount = 0;
        while (code >= 256)
        {
            if (code < 0 || code >= MaximumCodeCount || prefixes[code] < 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary entry is invalid.");
                return 0;
            }

            if (stackLength >= stack.Length)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary entry is too long.");
                return 0;
            }

            stack[stackLength] = suffixes[code];
            stackLength = stackLength + 1;
            code = prefixes[code];
            stepCount = stepCount + 1;
            if (stepCount > MaximumCodeCount)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary chain is cyclic.");
                return 0;
            }
        }

        if (stackLength >= stack.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LZW dictionary entry is too long.");
            return 0;
        }

        stack[stackLength] = (byte)code;
        return stackLength + 1;
    }
}
