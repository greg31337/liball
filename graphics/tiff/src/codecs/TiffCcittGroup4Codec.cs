using System;

namespace tiff;

internal class TiffCcittTransitionLine
{
    internal int[] Values;
    internal int Count;

    internal TiffCcittTransitionLine(int capacity)
    {
        Values = new int[capacity];
        Count = 0;
    }
}

internal class TiffCcittModeResult
{
    internal bool Success;
    internal int Mode;
    internal int Delta;
    internal int BitOffset;
    internal TiffDiagnostics Diagnostics;

    internal TiffCcittModeResult()
    {
        Success = false;
        Mode = 0;
        Delta = 0;
        BitOffset = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffCcittModeResult Create(int mode, int delta, int bitOffset, TiffDiagnostics diagnostics)
    {
        TiffCcittModeResult value = new TiffCcittModeResult();
        value.Initialize(mode, delta, bitOffset, diagnostics);
        return value;
    }

    internal void Initialize(int mode, int delta, int bitOffset, TiffDiagnostics diagnostics)
    {
        Success = !diagnostics.HasErrors();
        Mode = mode;
        Delta = delta;
        BitOffset = bitOffset;
        Diagnostics = diagnostics;
    }
}

internal class TiffCcittGroup4Codec
{
    internal const int ModePass = 1;
    internal const int ModeHorizontal = 2;
    internal const int ModeVertical = 3;
    internal const int UncompressedOption = 2;
    internal const int SupportedOptionsMask = 2;

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

        int rowCount = TiffCcittModifiedHuffmanCodec.CalculateRowCount(context, diagnostics);
        int rowByteCount = TiffCcittModifiedHuffmanCodec.CalculateRowByteCount(context, diagnostics);
        int width = CalculateWidth(context, diagnostics);
        ValidateTransitionAllocation(width, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        long decodedByteCount = CalculateDecodedByteCount(context, rowCount, rowByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[(int)decodedByteCount];
        TiffCcittTransitionLine referenceLine = CreateWhiteLine(width);
        int bitOffset = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            bitOffset = DecodeRow(context, source, destination, rowIndex, rowByteCount, width, referenceLine, bitOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            referenceLine = BuildTransitionsFromRow(destination, rowIndex * rowByteCount, width, context.Info.Photometric);
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

        int rowCount = TiffCcittModifiedHuffmanCodec.CalculateRowCount(context, diagnostics);
        int rowByteCount = TiffCcittModifiedHuffmanCodec.CalculateRowByteCount(context, diagnostics);
        int width = CalculateWidth(context, diagnostics);
        ValidateTransitionAllocation(width, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        long decodedByteCount = CalculateDecodedByteCount(context, rowCount, rowByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (source.Length != (int)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 source byte count does not match the expected decoded byte count.");
            return new byte[0];
        }

        int maximumEncodedByteCount = CalculateMaximumEncodedByteCount(source.Length, rowCount, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[maximumEncodedByteCount];
        TiffCcittTransitionLine referenceLine = CreateWhiteLine(width);
        int bitOffset = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            TiffCcittTransitionLine currentLine = BuildTransitionsFromRow(source, rowIndex * rowByteCount, width, context.Info.Photometric);
            bitOffset = EncodeRow(destination, bitOffset, width, currentLine, referenceLine, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            referenceLine = currentLine;
            rowIndex = rowIndex + 1;
        }

        bitOffset = WriteEndOfFacsimileBlock(destination, bitOffset);
        return TiffCcittModifiedHuffmanCodec.TrimEncodedBytes(destination, TiffCcittModifiedHuffmanCodec.CalculateByteCountFromBits(bitOffset));
    }

    internal static bool ValidateDecodeContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, diagnostics))
        {
            return false;
        }

        if (!TiffCcittModifiedHuffmanCodec.ValidateImageInfo(context, diagnostics))
        {
            return false;
        }

        return ValidateOptions(context, diagnostics);
    }

    internal static bool ValidateDecodeContext(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, source, diagnostics))
        {
            return false;
        }

        if (!TiffCcittModifiedHuffmanCodec.ValidateImageInfo(context, diagnostics))
        {
            return false;
        }

        return ValidateOptions(context, diagnostics);
    }

    internal static bool ValidateEncodeContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateEncodeContext(context, diagnostics))
        {
            return false;
        }

        if (!TiffCcittModifiedHuffmanCodec.ValidateImageInfo(context, diagnostics))
        {
            return false;
        }

        return ValidateOptions(context, diagnostics);
    }

    internal static bool ValidateOptions(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        int options = context.Info.T6Options;
        if ((options & 1) != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CCITT Group 4 T6Options bit 0 must be zero.", context.Info.ImageIndex, TiffTagId.T6Options);
            return false;
        }

        if ((options & UncompressedOption) != 0)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedCompression, "CCITT Group 4 uncompressed mode is not supported.");
            return false;
        }

        if ((options & ~SupportedOptionsMask) != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CCITT Group 4 T6Options contains unsupported reserved bits.", context.Info.ImageIndex, TiffTagId.T6Options);
            return false;
        }

        return true;
    }

    internal static int CalculateWidth(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong width = context.Info.Width;
        if (context.IsTile)
        {
            width = context.Info.TileWidth;
        }

        if (width == 0 || width > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "CCITT Group 4 row width is invalid.");
            return 0;
        }

        return (int)width;
    }

    internal static void ValidateTransitionAllocation(int width, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long byteCount = ((long)width + 1) * 4 * 2;
        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Group 4 transition tables exceed the configured allocation limit.");
        }
    }

    internal static long CalculateDecodedByteCount(TiffCodecContext context, int rowCount, int rowByteCount, TiffDiagnostics diagnostics)
    {
        long decodedByteCount = (long)rowCount * (long)rowByteCount;
        if (decodedByteCount > int.MaxValue || context.ExpectedDecodedByteCount != (ulong)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 decoded byte count does not match the expected row layout.");
            return 0;
        }

        return decodedByteCount;
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, int rowCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long maximumEncodedByteCount = (long)sourceLength * 20 + (long)rowCount * 4 + 8;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Group 4 encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Group 4 encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static int DecodeRow(TiffCodecContext context, ReadOnlySpan<byte> source, byte[] destination, int rowIndex, int rowByteCount, int width, TiffCcittTransitionLine referenceLine, int bitOffset, TiffDiagnostics diagnostics)
    {
        int rowByteOffset = rowIndex * rowByteCount;
        int a0 = 0;
        bool isWhite = true;
        bool includeEqual = true;
        while (a0 < width)
        {
            TiffCcittModeResult modeResult = ReadMode(source, bitOffset);
            diagnostics.Merge(modeResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return bitOffset;
            }

            bitOffset = modeResult.BitOffset;
            if (modeResult.Mode == ModePass)
            {
                int b1 = FindChangingElement(referenceLine, a0, !isWhite, includeEqual);
                int b2 = FindNextTransition(referenceLine, b1);
                if (b2 <= a0)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 pass mode did not advance across the row.");
                    return bitOffset;
                }

                TiffCcittModifiedHuffmanCodec.WriteRun(destination, rowByteOffset, a0, b2 - a0, TiffCcittModifiedHuffmanCodec.CalculateOutputBit(context.Info.Photometric, isWhite));
                a0 = b2;
                includeEqual = false;
            }
            else if (modeResult.Mode == ModeHorizontal)
            {
                TiffCcittRunResult firstRun = TiffCcittModifiedHuffmanCodec.ReadRun(source, bitOffset, isWhite);
                diagnostics.Merge(firstRun.Diagnostics);
                if (diagnostics.HasErrors())
                {
                    return bitOffset;
                }

                bitOffset = firstRun.BitOffset;
                TiffCcittRunResult secondRun = TiffCcittModifiedHuffmanCodec.ReadRun(source, bitOffset, !isWhite);
                diagnostics.Merge(secondRun.Diagnostics);
                if (diagnostics.HasErrors())
                {
                    return bitOffset;
                }

                bitOffset = secondRun.BitOffset;
                int a1 = a0 + firstRun.RunLength;
                int a2 = a1 + secondRun.RunLength;
                if (a1 > width || a2 > width || a2 <= a0)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 horizontal run exceeds the row width.");
                    return bitOffset;
                }

                TiffCcittModifiedHuffmanCodec.WriteRun(destination, rowByteOffset, a0, firstRun.RunLength, TiffCcittModifiedHuffmanCodec.CalculateOutputBit(context.Info.Photometric, isWhite));
                TiffCcittModifiedHuffmanCodec.WriteRun(destination, rowByteOffset, a1, secondRun.RunLength, TiffCcittModifiedHuffmanCodec.CalculateOutputBit(context.Info.Photometric, !isWhite));
                a0 = a2;
                includeEqual = false;
            }
            else
            {
                int b1 = FindChangingElement(referenceLine, a0, !isWhite, includeEqual);
                int a1 = b1 + modeResult.Delta;
                if (a1 < a0 || a1 > width)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 vertical mode points outside the row.");
                    return bitOffset;
                }

                TiffCcittModifiedHuffmanCodec.WriteRun(destination, rowByteOffset, a0, a1 - a0, TiffCcittModifiedHuffmanCodec.CalculateOutputBit(context.Info.Photometric, isWhite));
                a0 = a1;
                isWhite = !isWhite;
                includeEqual = false;
            }
        }

        return bitOffset;
    }

    internal static int EncodeRow(byte[] destination, int bitOffset, int width, TiffCcittTransitionLine currentLine, TiffCcittTransitionLine referenceLine, TiffDiagnostics diagnostics)
    {
        int a0 = 0;
        bool isWhite = true;
        bool includeEqual = true;
        while (a0 < width)
        {
            int b1 = FindChangingElement(referenceLine, a0, !isWhite, includeEqual);
            int b2 = FindNextTransition(referenceLine, b1);
            int a1 = FindChangingElement(currentLine, a0, !isWhite, includeEqual);
            if (b2 < a1)
            {
                bitOffset = WriteMode(destination, bitOffset, ModePass, 0);
                a0 = b2;
                includeEqual = false;
            }
            else if (a1 - b1 >= -3 && a1 - b1 <= 3)
            {
                bitOffset = WriteMode(destination, bitOffset, ModeVertical, a1 - b1);
                a0 = a1;
                isWhite = !isWhite;
                includeEqual = false;
            }
            else
            {
                int a2 = FindNextTransition(currentLine, a1);
                int firstRunLength = a1 - a0;
                int secondRunLength = a2 - a1;
                bitOffset = WriteMode(destination, bitOffset, ModeHorizontal, 0);
                bitOffset = TiffCcittModifiedHuffmanCodec.WriteEncodedRun(destination, bitOffset, firstRunLength, isWhite, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return bitOffset;
                }

                bitOffset = TiffCcittModifiedHuffmanCodec.WriteEncodedRun(destination, bitOffset, secondRunLength, !isWhite, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return bitOffset;
                }

                a0 = a2;
                includeEqual = false;
            }
        }

        return bitOffset;
    }

    internal static TiffCcittModeResult ReadMode(ReadOnlySpan<byte> source, int bitOffset)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int codeLength = 1;
        while (codeLength <= 7)
        {
            if (bitOffset + codeLength > source.Length * 8)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "CCITT Group 4 mode code is truncated.");
                return TiffCcittModeResult.Create(0, 0, bitOffset, diagnostics);
            }

            int code = TiffCcittModifiedHuffmanCodec.ReadBits(source, bitOffset, codeLength);
            TiffCcittModeResult result = FindMode(codeLength, code, bitOffset + codeLength, diagnostics);
            if (result.Success)
            {
                return result;
            }

            codeLength = codeLength + 1;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 4 mode code is invalid.");
        return TiffCcittModeResult.Create(0, 0, bitOffset, diagnostics);
    }

    internal static TiffCcittModeResult FindMode(int codeLength, int code, int bitOffset, TiffDiagnostics diagnostics)
    {
        if (codeLength == 1 && code == 1)
        {
            return TiffCcittModeResult.Create(ModeVertical, 0, bitOffset, diagnostics);
        }

        if (codeLength == 3 && code == 1)
        {
            return TiffCcittModeResult.Create(ModeHorizontal, 0, bitOffset, diagnostics);
        }

        if (codeLength == 3 && code == 3)
        {
            return TiffCcittModeResult.Create(ModeVertical, 1, bitOffset, diagnostics);
        }

        if (codeLength == 3 && code == 2)
        {
            return TiffCcittModeResult.Create(ModeVertical, -1, bitOffset, diagnostics);
        }

        if (codeLength == 4 && code == 1)
        {
            return TiffCcittModeResult.Create(ModePass, 0, bitOffset, diagnostics);
        }

        if (codeLength == 6 && code == 3)
        {
            return TiffCcittModeResult.Create(ModeVertical, 2, bitOffset, diagnostics);
        }

        if (codeLength == 6 && code == 2)
        {
            return TiffCcittModeResult.Create(ModeVertical, -2, bitOffset, diagnostics);
        }

        if (codeLength == 7 && code == 3)
        {
            return TiffCcittModeResult.Create(ModeVertical, 3, bitOffset, diagnostics);
        }

        if (codeLength == 7 && code == 2)
        {
            return TiffCcittModeResult.Create(ModeVertical, -3, bitOffset, diagnostics);
        }

        return new TiffCcittModeResult();
    }

    internal static int WriteMode(byte[] destination, int bitOffset, int mode, int delta)
    {
        if (mode == ModePass)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 4, 1);
        }

        if (mode == ModeHorizontal)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 3, 1);
        }

        if (delta == 0)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 1, 1);
        }

        if (delta == 1)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 3, 3);
        }

        if (delta == -1)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 3, 2);
        }

        if (delta == 2)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 6, 3);
        }

        if (delta == -2)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 6, 2);
        }

        if (delta == 3)
        {
            return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 7, 3);
        }

        return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 7, 2);
    }

    internal static int WriteEndOfFacsimileBlock(byte[] destination, int bitOffset)
    {
        bitOffset = TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 12, 1);
        return TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, 12, 1);
    }

    internal static TiffCcittTransitionLine CreateWhiteLine(int width)
    {
        TiffCcittTransitionLine line = new TiffCcittTransitionLine(width + 1);
        line.Values[0] = width;
        line.Count = 1;
        return line;
    }

    internal static TiffCcittTransitionLine BuildTransitionsFromRow(ReadOnlySpan<byte> source, int rowByteOffset, int width, TiffPhotometric photometric)
    {
        TiffCcittTransitionLine line = new TiffCcittTransitionLine(width + 1);
        bool previousWhite = true;
        int pixel = 0;
        if (width > 0)
        {
            int bit = TiffCcittModifiedHuffmanCodec.ReadPackedBit(source, rowByteOffset, 0);
            previousWhite = TiffCcittModifiedHuffmanCodec.IsWhitePixel(photometric, bit);
            if (!previousWhite)
            {
                AddTransition(line, 0);
            }

            pixel = 1;
        }

        while (pixel < width)
        {
            int bit = TiffCcittModifiedHuffmanCodec.ReadPackedBit(source, rowByteOffset, pixel);
            bool isWhite = TiffCcittModifiedHuffmanCodec.IsWhitePixel(photometric, bit);
            if (isWhite != previousWhite)
            {
                AddTransition(line, pixel);
                previousWhite = isWhite;
            }

            pixel = pixel + 1;
        }

        AddTransition(line, width);
        return line;
    }

    internal static void AddTransition(TiffCcittTransitionLine line, int value)
    {
        line.Values[line.Count] = value;
        line.Count = line.Count + 1;
    }

    internal static int FindChangingElement(TiffCcittTransitionLine line, int position, bool targetWhite, bool includeEqual)
    {
        bool isWhite = true;
        int index = 0;
        while (index < line.Count)
        {
            int transition = line.Values[index];
            isWhite = !isWhite;
            bool isAfterPosition = transition > position;
            if (includeEqual && transition == position)
            {
                isAfterPosition = true;
            }

            if (isAfterPosition && isWhite == targetWhite)
            {
                return transition;
            }

            index = index + 1;
        }

        return line.Values[line.Count - 1];
    }

    internal static int FindNextTransition(TiffCcittTransitionLine line, int position)
    {
        int index = 0;
        while (index < line.Count)
        {
            if (line.Values[index] > position)
            {
                return line.Values[index];
            }

            index = index + 1;
        }

        return line.Values[line.Count - 1];
    }
}
