using System;

namespace tiff;

internal class TiffScanlineReader
{
    internal static TiffScanlineReadResult ReadScanline(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong row)
    {
        return ReadScanline(directory, fileBytes, byteOrder, row, 0, new TiffLimits());
    }

    internal static TiffScanlineReadResult ReadScanline(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong row, int sampleIndex)
    {
        return ReadScanline(directory, fileBytes, byteOrder, row, sampleIndex, new TiffLimits());
    }

    internal static TiffScanlineReadResult ReadScanline(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong row, int sampleIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a scanline.");
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, TiffCompression.Unknown, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        if (!ValidateStrippedImage(infoResult.Info, diagnostics))
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to read a scanline.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to read a scanline.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        return ReadScanline(infoResult.Info, stripOffsets, stripByteCounts, fileBytes, row, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffScanlineReadResult ReadScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong row)
    {
        return ReadScanline(info, stripOffsets, stripByteCounts, fileBytes, row, 0, new TiffLimits());
    }

    internal static TiffScanlineReadResult ReadScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong row, int sampleIndex)
    {
        return ReadScanline(info, stripOffsets, stripByteCounts, fileBytes, row, sampleIndex, new TiffLimits());
    }

    internal static TiffScanlineReadResult ReadScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong row, int sampleIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadScanline(info, stripOffsets, stripByteCounts, fileBytes, row, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffScanlineReadResult ReadScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong row, int sampleIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!ValidateStrippedImage(info, diagnostics))
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, 0, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong stripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(info, row, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, stripIndex, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong scanlineByteCount = CalculateScanlineByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        TiffDecodedStripReadResult stripResult = TiffDecodedStripReader.ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        ulong sourceOffset = CalculateSourceOffset(info, row, stripIndex, scanlineByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineReadResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        byte[] bytes = CopyScanline(stripResult.Bytes, sourceOffset, scanlineByteCount, diagnostics);
        return TiffScanlineReadResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), bytes, diagnostics);
    }

    internal static ulong CalculateScanlineByteCount(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong bitsPerRow = TiffStrileLayout.CalculateStripBitsPerRow(info, stripIndex, diagnostics);
        if (bitsPerRow == 0)
        {
            return 0;
        }

        return TiffStrileLayout.CalculateByteCountForRows(bitsPerRow, 1, "Scanline byte count overflows the supported range.", diagnostics);
    }

    internal static ulong CalculateSourceOffset(TiffImageInfo info, ulong row, ulong stripIndex, ulong scanlineByteCount, TiffDiagnostics diagnostics)
    {
        ulong stripIndexInPlane = TiffStrileLayout.CalculateStripIndexInPlane(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (stripIndexInPlane > ulong.MaxValue / info.RowsPerStrip)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline strip row offset overflows the supported range.");
            return 0;
        }

        ulong firstRow = stripIndexInPlane * info.RowsPerStrip;
        if (row < firstRow)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Scanline row is outside the decoded strip.");
            return 0;
        }

        ulong rowInStrip = row - firstRow;
        if (scanlineByteCount != 0 && rowInStrip > ulong.MaxValue / scanlineByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline byte offset overflows the supported range.");
            return 0;
        }

        return rowInStrip * scanlineByteCount;
    }

    internal static byte[] CopyScanline(byte[] stripBytes, ulong sourceOffset, ulong scanlineByteCount, TiffDiagnostics diagnostics)
    {
        if (stripBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Decoded strip bytes are required to copy a scanline.");
            return new byte[0];
        }

        if (scanlineByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Scanline byte count is too large to allocate.");
            return new byte[0];
        }

        if (sourceOffset > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Scanline byte offset exceeds the supported array range.");
            return new byte[0];
        }

        if (sourceOffset > ulong.MaxValue - scanlineByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline byte range end offset overflows the supported range.");
            return new byte[0];
        }

        ulong endOffset = sourceOffset + scanlineByteCount;
        if (endOffset > (ulong)stripBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Scanline byte range is outside the decoded strip.");
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(stripBytes.AsSpan((int)sourceOffset, (int)scanlineByteCount));
    }

    internal static bool ValidateStrippedImage(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read a scanline.");
            return false;
        }

        if (info.IsTiled)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Scanline reading requires stripped image data.");
            return false;
        }

        return true;
    }

    internal static TiffCompression GetCompression(TiffImageInfo info)
    {
        if (info == null)
        {
            return TiffCompression.Unknown;
        }

        return info.Compression;
    }
}
