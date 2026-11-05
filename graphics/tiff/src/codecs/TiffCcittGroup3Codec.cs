using System;

namespace tiff;

internal class TiffCcittGroup3Codec
{
    internal const int EndOfLineCodeLength = 12;
    internal const int EndOfLineCode = 1;
    internal const int TwoDimensionalOption = 1;
    internal const int UncompressedOption = 2;
    internal const int ByteAlignedEolOption = 4;
    internal const int SupportedOptionsMask = 7;

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
        int bitOffset = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            bitOffset = ReadEndOfLine(source, bitOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            bitOffset = TiffCcittModifiedHuffmanCodec.DecodeRow(context, source, destination, rowIndex, rowByteCount, bitOffset, diagnostics);
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

        int rowCount = TiffCcittModifiedHuffmanCodec.CalculateRowCount(context, diagnostics);
        int rowByteCount = TiffCcittModifiedHuffmanCodec.CalculateRowByteCount(context, diagnostics);
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
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 3 source byte count does not match the expected decoded byte count.");
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
            bitOffset = WriteFillBeforeEndOfLine(bitOffset, context.Info.T4Options);
            bitOffset = TiffCcittModifiedHuffmanCodec.WriteBits(destination, bitOffset, EndOfLineCodeLength, EndOfLineCode);
            bitOffset = TiffCcittModifiedHuffmanCodec.EncodeRow(context, source, destination, rowIndex, rowByteCount, bitOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            rowIndex = rowIndex + 1;
        }

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
        int options = context.Info.T4Options;
        if ((options & TwoDimensionalOption) != 0)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedCompression, "CCITT Group 3 two-dimensional MR coding is not supported.");
            return false;
        }

        if ((options & UncompressedOption) != 0)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedCompression, "CCITT Group 3 uncompressed mode is not supported.");
            return false;
        }

        if ((options & ~SupportedOptionsMask) != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CCITT Group 3 T4Options contains unsupported reserved bits.", context.Info.ImageIndex, TiffTagId.T4Options);
            return false;
        }

        return true;
    }

    internal static long CalculateDecodedByteCount(TiffCodecContext context, int rowCount, int rowByteCount, TiffDiagnostics diagnostics)
    {
        long decodedByteCount = (long)rowCount * (long)rowByteCount;
        if (decodedByteCount > int.MaxValue || context.ExpectedDecodedByteCount != (ulong)decodedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 3 decoded byte count does not match the expected row layout.");
            return 0;
        }

        return decodedByteCount;
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, int rowCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long maximumEncodedByteCount = (long)sourceLength * 13 + (long)rowCount * 4 + 8;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Group 3 encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CCITT Group 3 encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static int ReadEndOfLine(ReadOnlySpan<byte> source, int bitOffset, TiffDiagnostics diagnostics)
    {
        int totalBitCount = source.Length * 8;
        while (bitOffset + EndOfLineCodeLength <= totalBitCount)
        {
            int code = TiffCcittModifiedHuffmanCodec.ReadBits(source, bitOffset, EndOfLineCodeLength);
            if (code == EndOfLineCode)
            {
                return bitOffset + EndOfLineCodeLength;
            }

            int bit = TiffCcittModifiedHuffmanCodec.ReadBits(source, bitOffset, 1);
            if (bit != 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CCITT Group 3 row does not begin with an EOL code.");
                return bitOffset;
            }

            bitOffset = bitOffset + 1;
        }

        diagnostics.AddError(TiffErrorCode.TruncatedStream, "CCITT Group 3 EOL code is truncated.");
        return bitOffset;
    }

    internal static int WriteFillBeforeEndOfLine(int bitOffset, int options)
    {
        if ((options & ByteAlignedEolOption) == 0)
        {
            return bitOffset;
        }

        while ((bitOffset + EndOfLineCodeLength) % 8 != 0)
        {
            bitOffset = bitOffset + 1;
        }

        return bitOffset;
    }
}
