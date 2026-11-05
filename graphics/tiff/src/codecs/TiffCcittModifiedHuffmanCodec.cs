using System;

namespace tiff;

internal class TiffCcittRunResult
{
    internal bool Success;
    internal int RunLength;
    internal int BitOffset;
    internal bool IsTerminating;
    internal TiffDiagnostics Diagnostics;

    internal TiffCcittRunResult()
    {
        Success = false;
        RunLength = 0;
        BitOffset = 0;
        IsTerminating = false;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffCcittRunResult Create(int runLength, int bitOffset, bool isTerminating, TiffDiagnostics diagnostics)
    {
        TiffCcittRunResult value = new TiffCcittRunResult();
        value.Initialize(runLength, bitOffset, isTerminating, diagnostics);
        return value;
    }

    internal void Initialize(int runLength, int bitOffset, bool isTerminating, TiffDiagnostics diagnostics)
    {
        Success = !diagnostics.HasErrors();
        RunLength = runLength;
        BitOffset = bitOffset;
        IsTerminating = isTerminating;
        Diagnostics = diagnostics;
    }
}

internal class TiffCcittModifiedHuffmanCodec
{
    internal static readonly int[] WhiteCodeLengths = new int[] { 8, 6, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 5, 5, 6, 7, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 6, 9, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12 };
    internal static readonly int[] WhiteCodes = new int[] { 53, 7, 7, 8, 11, 12, 14, 15, 19, 20, 7, 8, 8, 3, 52, 53, 42, 43, 39, 12, 8, 23, 3, 4, 40, 43, 19, 36, 24, 2, 3, 26, 27, 18, 19, 20, 21, 22, 23, 40, 41, 42, 43, 44, 45, 4, 5, 10, 11, 82, 83, 84, 85, 36, 37, 88, 89, 90, 91, 74, 75, 50, 51, 52, 27, 18, 23, 55, 54, 55, 100, 101, 104, 103, 204, 205, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 152, 153, 154, 24, 155, 8, 12, 13, 18, 19, 20, 21, 22, 23, 28, 29, 30, 31 };
    internal static readonly int[] WhiteRunLengths = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 128, 192, 256, 320, 384, 448, 512, 576, 640, 704, 768, 832, 896, 960, 1024, 1088, 1152, 1216, 1280, 1344, 1408, 1472, 1536, 1600, 1664, 1728, 1792, 1856, 1920, 1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560 };
    internal static readonly int[] BlackCodeLengths = new int[] { 10, 3, 2, 2, 3, 4, 4, 5, 6, 6, 7, 7, 7, 8, 8, 9, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 10, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12 };
    internal static readonly int[] BlackCodes = new int[] { 55, 2, 3, 2, 3, 3, 2, 3, 5, 4, 4, 5, 7, 4, 7, 24, 23, 24, 8, 103, 104, 108, 55, 40, 23, 24, 202, 203, 204, 205, 104, 105, 106, 107, 210, 211, 212, 213, 214, 215, 108, 109, 218, 219, 84, 85, 86, 87, 100, 101, 82, 83, 36, 55, 56, 39, 40, 88, 89, 43, 44, 90, 102, 103, 15, 200, 201, 91, 51, 52, 53, 108, 109, 74, 75, 76, 77, 114, 115, 116, 117, 118, 119, 82, 83, 84, 85, 90, 91, 100, 101, 8, 12, 13, 18, 19, 20, 21, 22, 23, 28, 29, 30, 31 };
    internal static readonly int[] BlackRunLengths = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 128, 192, 256, 320, 384, 448, 512, 576, 640, 704, 768, 832, 896, 960, 1024, 1088, 1152, 1216, 1280, 1344, 1408, 1472, 1536, 1600, 1664, 1728, 1792, 1856, 1920, 1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560 };

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
        if (!ValidateDecodeContext(context, source, diagnostics))
        {
            return new byte[0];
        }

        byte[] destination = new byte[(int)context.ExpectedDecodedByteCount];
        int rowCount = CalculateRowCount(context, diagnostics);
        int rowByteCount = CalculateRowByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        long decodedByteCount = (long)rowCount * (long)rowByteCount;
        if (decodedByteCount > int.MaxValue || context.ExpectedDecodedByteCount != (ulong)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman decoded byte count does not match the expected row layout.");
            return new byte[0];
        }

        int bitOffset = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            bitOffset = DecodeRow(context, source, destination, rowIndex, rowByteCount, bitOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            rowIndex = rowIndex + 1;
        }

        return destination;
    }

    internal static byte[] EncodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!ValidateEncodeContext(context, diagnostics))
        {
            return new byte[0];
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int rowByteCount = CalculateRowByteCount(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        long decodedByteCount = (long)rowCount * (long)rowByteCount;
        if (decodedByteCount > int.MaxValue || context.ExpectedDecodedByteCount != (ulong)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman decoded byte count does not match the expected row layout.");
            return new byte[0];
        }

        if (source.Length != (int)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman source byte count does not match the expected decoded byte count.");
            return new byte[0];
        }

        int maximumEncodedByteCount = CalculateMaximumEncodedByteCount(source.Length, rowCount, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[maximumEncodedByteCount];
        int bitOffset = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            bitOffset = EncodeRow(context, source, destination, rowIndex, rowByteCount, bitOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            rowIndex = rowIndex + 1;
        }

        return TrimEncodedBytes(destination, CalculateByteCountFromBits(bitOffset));
    }

    internal static bool ValidateContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, diagnostics))
        {
            return false;
        }

        return ValidateImageInfo(context, diagnostics);
    }

    internal static bool ValidateDecodeContext(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, source, diagnostics))
        {
            return false;
        }

        return ValidateImageInfo(context, diagnostics);
    }

    internal static bool ValidateEncodeContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateEncodeContext(context, diagnostics))
        {
            return false;
        }

        return ValidateImageInfo(context, diagnostics);
    }

    internal static bool ValidateImageInfo(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.SamplesPerPixel != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CCITT Modified Huffman requires one sample per pixel.", context.Info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        if (context.Info.BitsPerSample == null || context.Info.BitsPerSample.Length == 0 || context.Info.BitsPerSample[0] != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CCITT Modified Huffman requires one bit per sample.", context.Info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        if (context.Info.Photometric != TiffPhotometric.WhiteIsZero && context.Info.Photometric != TiffPhotometric.BlackIsZero)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "CCITT Modified Huffman requires WhiteIsZero or BlackIsZero photometric interpretation.");
            return false;
        }

        return true;
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, int rowCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long maximumEncodedByteCount = (long)sourceLength * 13 + (long)rowCount * 2 + 8;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Modified Huffman encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Modified Huffman encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static int CalculateRowCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong rowCount = context.Info.Length;
        if (context.IsTile)
        {
            rowCount = context.Info.TileLength;
        }
        else
        {
            rowCount = TiffStrileLayout.CalculateRowsInStrip(context.Info, context.SegmentIndex, diagnostics);
        }

        if (rowCount == 0 || rowCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "CCITT Modified Huffman row count is invalid.");
            return 0;
        }

        return (int)rowCount;
    }

    internal static int CalculateRowByteCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong width = context.Info.Width;
        if (context.IsTile)
        {
            width = context.Info.TileWidth;
        }

        if (width == 0 || width > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "CCITT Modified Huffman row width is invalid.");
            return 0;
        }

        ulong rowByteCount = (width + 7) / 8;
        if (rowByteCount == 0 || rowByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "CCITT Modified Huffman row byte count exceeds the supported range.");
            return 0;
        }

        return (int)rowByteCount;
    }

    internal static int DecodeRow(TiffCodecContext context, ReadOnlySpan<byte> source, byte[] destination, int rowIndex, int rowByteCount, int bitOffset, TiffDiagnostics diagnostics)
    {
        int width = (int)context.Info.Width;
        if (context.IsTile)
        {
            width = (int)context.Info.TileWidth;
        }

        int pixel = 0;
        bool isWhite = true;
        while (pixel < width)
        {
            TiffCcittRunResult runResult = ReadRun(source, bitOffset, isWhite);
            diagnostics.Merge(runResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return bitOffset;
            }

            if (runResult.RunLength > width - pixel)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman run exceeds the row width.");
                return bitOffset;
            }

            WriteRun(destination, rowIndex * rowByteCount, pixel, runResult.RunLength, CalculateOutputBit(context.Info.Photometric, isWhite));
            pixel = pixel + runResult.RunLength;
            bitOffset = runResult.BitOffset;
            isWhite = !isWhite;
        }

        return bitOffset;
    }

    internal static int EncodeRow(TiffCodecContext context, ReadOnlySpan<byte> source, byte[] destination, int rowIndex, int rowByteCount, int bitOffset, TiffDiagnostics diagnostics)
    {
        int width = (int)context.Info.Width;
        if (context.IsTile)
        {
            width = (int)context.Info.TileWidth;
        }

        int rowByteOffset = rowIndex * rowByteCount;
        int pixel = 0;
        bool isWhite = true;
        while (pixel < width)
        {
            int runLength = CountRunPixels(source, rowByteOffset, pixel, width, isWhite, context.Info.Photometric);
            bitOffset = WriteEncodedRun(destination, bitOffset, runLength, isWhite, diagnostics);
            if (diagnostics.HasErrors())
            {
                return bitOffset;
            }

            pixel = pixel + runLength;
            isWhite = !isWhite;
        }

        return bitOffset;
    }

    internal static int CalculateOutputBit(TiffPhotometric photometric, bool isWhite)
    {
        if (photometric == TiffPhotometric.WhiteIsZero)
        {
            if (isWhite)
            {
                return 0;
            }

            return 1;
        }

        if (isWhite)
        {
            return 1;
        }

        return 0;
    }

    internal static bool IsWhitePixel(TiffPhotometric photometric, int bit)
    {
        if (photometric == TiffPhotometric.WhiteIsZero)
        {
            return bit == 0;
        }

        return bit != 0;
    }

    internal static int CountRunPixels(ReadOnlySpan<byte> source, int rowByteOffset, int startPixel, int width, bool isWhite, TiffPhotometric photometric)
    {
        int count = 0;
        while (startPixel + count < width)
        {
            int bit = ReadPackedBit(source, rowByteOffset, startPixel + count);
            if (IsWhitePixel(photometric, bit) != isWhite)
            {
                return count;
            }

            count = count + 1;
        }

        return count;
    }

    internal static int ReadPackedBit(ReadOnlySpan<byte> source, int rowByteOffset, int pixel)
    {
        int byteIndex = rowByteOffset + pixel / 8;
        int shift = 7 - pixel % 8;
        return (source[byteIndex] >> shift) & 1;
    }

    internal static int WriteEncodedRun(byte[] destination, int bitOffset, int runLength, bool isWhite, TiffDiagnostics diagnostics)
    {
        int remainingRunLength = runLength;
        while (remainingRunLength >= 2560)
        {
            bitOffset = WriteRunCode(destination, bitOffset, 2560, isWhite, diagnostics);
            if (diagnostics.HasErrors())
            {
                return bitOffset;
            }

            remainingRunLength = remainingRunLength - 2560;
        }

        if (remainingRunLength >= 64)
        {
            int makeupRunLength = FindLargestMakeupRun(isWhite, remainingRunLength);
            bitOffset = WriteRunCode(destination, bitOffset, makeupRunLength, isWhite, diagnostics);
            if (diagnostics.HasErrors())
            {
                return bitOffset;
            }

            remainingRunLength = remainingRunLength - makeupRunLength;
        }

        return WriteRunCode(destination, bitOffset, remainingRunLength, isWhite, diagnostics);
    }

    internal static int WriteRunCode(byte[] destination, int bitOffset, int runLength, bool isWhite, TiffDiagnostics diagnostics)
    {
        int codeLength = FindCodeLength(isWhite, runLength);
        int code = FindCode(isWhite, runLength);
        if (codeLength < 0 || code < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman run length cannot be encoded.");
            return bitOffset;
        }

        return WriteBits(destination, bitOffset, codeLength, code);
    }

    internal static int FindLargestMakeupRun(bool isWhite, int runLength)
    {
        int[] runs = BlackRunLengths;
        if (isWhite)
        {
            runs = WhiteRunLengths;
        }

        int bestRunLength = 0;
        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index] >= 64 && runs[index] <= runLength && runs[index] > bestRunLength)
            {
                bestRunLength = runs[index];
            }

            index = index + 1;
        }

        return bestRunLength;
    }

    internal static int FindCodeLength(bool isWhite, int runLength)
    {
        int[] lengths = BlackCodeLengths;
        int[] runs = BlackRunLengths;
        if (isWhite)
        {
            lengths = WhiteCodeLengths;
            runs = WhiteRunLengths;
        }

        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index] == runLength)
            {
                return lengths[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindCode(bool isWhite, int runLength)
    {
        int[] codes = BlackCodes;
        int[] runs = BlackRunLengths;
        if (isWhite)
        {
            codes = WhiteCodes;
            runs = WhiteRunLengths;
        }

        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index] == runLength)
            {
                return codes[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal static TiffCcittRunResult ReadRun(ReadOnlySpan<byte> source, int bitOffset, bool isWhite)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int totalRunLength = 0;
        bool isTerminating = false;
        while (!isTerminating)
        {
            TiffCcittRunResult codeResult = ReadCode(source, bitOffset, isWhite);
            diagnostics.Merge(codeResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffCcittRunResult.Create(0, bitOffset, false, diagnostics);
            }

            totalRunLength = totalRunLength + codeResult.RunLength;
            bitOffset = codeResult.BitOffset;
            isTerminating = codeResult.IsTerminating;
        }

        return TiffCcittRunResult.Create(totalRunLength, bitOffset, true, diagnostics);
    }

    internal static TiffCcittRunResult ReadCode(ReadOnlySpan<byte> source, int bitOffset, bool isWhite)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int codeLength = 1;
        while (codeLength <= 13)
        {
            if (bitOffset + codeLength > source.Length * 8)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "CCITT Modified Huffman code is truncated.");
                return TiffCcittRunResult.Create(0, bitOffset, false, diagnostics);
            }

            int code = ReadBits(source, bitOffset, codeLength);
            int runLength = FindRunLength(isWhite, codeLength, code);
            if (runLength >= 0)
            {
                return TiffCcittRunResult.Create(runLength, bitOffset + codeLength, runLength < 64, diagnostics);
            }

            codeLength = codeLength + 1;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Modified Huffman code is invalid.");
        return TiffCcittRunResult.Create(0, bitOffset, false, diagnostics);
    }

    internal static int FindRunLength(bool isWhite, int codeLength, int code)
    {
        int[] lengths = BlackCodeLengths;
        int[] codes = BlackCodes;
        int[] runs = BlackRunLengths;
        if (isWhite)
        {
            lengths = WhiteCodeLengths;
            codes = WhiteCodes;
            runs = WhiteRunLengths;
        }

        int index = 0;
        while (index < lengths.Length)
        {
            if (lengths[index] == codeLength && codes[index] == code)
            {
                return runs[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int ReadBits(ReadOnlySpan<byte> source, int bitOffset, int bitCount)
    {
        int value = 0;
        int bitIndex = 0;
        while (bitIndex < bitCount)
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

    internal static int WriteBits(byte[] destination, int bitOffset, int bitCount, int code)
    {
        int bitIndex = bitCount - 1;
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

    internal static int CalculateByteCountFromBits(int bitCount)
    {
        return (bitCount + 7) / 8;
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

    internal static void WriteRun(byte[] destination, int rowByteOffset, int startPixel, int runLength, int bitValue)
    {
        if (bitValue == 0)
        {
            return;
        }

        int pixel = 0;
        while (pixel < runLength)
        {
            int outputPixel = startPixel + pixel;
            int byteIndex = rowByteOffset + outputPixel / 8;
            int shift = 7 - outputPixel % 8;
            destination[byteIndex] = (byte)(destination[byteIndex] | (1 << shift));
            pixel = pixel + 1;
        }
    }
}
