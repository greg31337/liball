using System;

namespace tiff;

internal class TiffStripRegionReader
{
    internal static TiffRegionReadResult ReadRegion(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong column, ulong row, ulong width, ulong length)
    {
        return ReadRegion(directory, fileBytes, byteOrder, column, row, width, length, 0, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong column, ulong row, ulong width, ulong length, int sampleIndex)
    {
        return ReadRegion(directory, fileBytes, byteOrder, column, row, width, length, sampleIndex, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a strip region.");
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, TiffCompression.Unknown, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to read a strip region.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to read a strip region.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        return ReadRegion(infoResult.Info, stripOffsets, stripByteCounts, fileBytes, column, row, width, length, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length)
    {
        return ReadRegion(info, stripOffsets, stripByteCounts, fileBytes, column, row, width, length, 0, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex)
    {
        return ReadRegion(info, stripOffsets, stripByteCounts, fileBytes, column, row, width, length, sampleIndex, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadRegion(info, stripOffsets, stripByteCounts, fileBytes, column, row, width, length, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!ValidateRegion(info, column, row, width, length, sampleIndex, diagnostics))
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong bytesPerPixel = CalculateBytesPerPixel(info, sampleIndex, diagnostics);
        ulong rowByteCount = CalculateRegionRowByteCount(width, bytesPerPixel, diagnostics);
        ulong totalByteCount = CalculateRegionTotalByteCount(rowByteCount, length, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        byte[] bytes = new byte[(int)totalByteCount];
        CopyRegionRows(info, stripOffsets, stripByteCounts, fileBytes, column, row, length, sampleIndex, bytesPerPixel, rowByteCount, bytes, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), bytes, diagnostics);
    }

    internal static void CopyRegionRows(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong length, int sampleIndex, ulong bytesPerPixel, ulong rowByteCount, byte[] destination, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong columnByteOffset = CalculateColumnByteOffset(column, bytesPerPixel, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        ulong rowIndex = 0;
        while (rowIndex < length)
        {
            ulong currentRow = row + rowIndex;
            TiffScanlineReadResult scanline = TiffScanlineReader.ReadScanline(info, stripOffsets, stripByteCounts, fileBytes, currentRow, sampleIndex, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            ulong destinationOffset = rowIndex * rowByteCount;
            CopyRegionRow(scanline.Bytes, columnByteOffset, rowByteCount, destination, destinationOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void CopyRegionRow(byte[] scanlineBytes, ulong sourceOffset, ulong rowByteCount, byte[] destination, ulong destinationOffset, TiffDiagnostics diagnostics)
    {
        if (scanlineBytes == null || destination == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Scanline and destination bytes are required to copy a region row.");
            return;
        }

        if (sourceOffset > int.MaxValue || destinationOffset > int.MaxValue || rowByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region row byte range exceeds the supported array range.");
            return;
        }

        if (sourceOffset > ulong.MaxValue - rowByteCount || destinationOffset > ulong.MaxValue - rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region row byte range end offset overflows the supported range.");
            return;
        }

        if (sourceOffset + rowByteCount > (ulong)scanlineBytes.Length || destinationOffset + rowByteCount > (ulong)destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Region row byte range is outside the available buffer.");
            return;
        }

        TiffRawStripWriter.CopyBytes(scanlineBytes.AsSpan((int)sourceOffset, (int)rowByteCount), destination.AsSpan((int)destinationOffset, (int)rowByteCount));
    }

    internal static bool ValidateRegion(TiffImageInfo info, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!TiffScanlineReader.ValidateStrippedImage(info, diagnostics))
        {
            return false;
        }

        if (width == 0 || length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Region dimensions must be positive.");
            return false;
        }

        if (!TiffStrileLayout.ValidateRowAndSample(info, row, sampleIndex, diagnostics))
        {
            return false;
        }

        if (column >= info.Width)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region column is outside the image width.");
            return false;
        }

        if (width > info.Width - column || length > info.Length - row)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region extends outside the image bounds.");
            return false;
        }

        return true;
    }

    internal static ulong CalculateBytesPerPixel(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        ulong bitsPerPixel = 0;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            if (info.BitsPerSample == null || sampleIndex >= info.BitsPerSample.Length)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain a value for the requested region sample.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            bitsPerPixel = (ulong)info.BitsPerSample[sampleIndex];
        }
        else
        {
            bitsPerPixel = TiffStrileLayout.SumBitsPerPixel(info.BitsPerSample, diagnostics);
        }

        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (bitsPerPixel % 8 != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Region reading currently requires byte-aligned pixels.");
            return 0;
        }

        return bitsPerPixel / 8;
    }

    internal static ulong CalculateRegionRowByteCount(ulong width, ulong bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if (bytesPerPixel != 0 && width > ulong.MaxValue / bytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region row byte count overflows the supported range.");
            return 0;
        }

        return width * bytesPerPixel;
    }

    internal static ulong CalculateRegionTotalByteCount(ulong rowByteCount, ulong length, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (rowByteCount != 0 && length > ulong.MaxValue / rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region byte count overflows the supported range.");
            return 0;
        }

        ulong totalByteCount = rowByteCount * length;
        if (totalByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Region byte count is too large to allocate.");
            return totalByteCount;
        }

        if (!actualLimits.AllowsAllocation((long)totalByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Region byte count exceeds the configured allocation limit.");
            return totalByteCount;
        }

        return totalByteCount;
    }

    internal static ulong CalculateColumnByteOffset(ulong column, ulong bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if (bytesPerPixel != 0 && column > ulong.MaxValue / bytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region column byte offset overflows the supported range.");
            return 0;
        }

        return column * bytesPerPixel;
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
