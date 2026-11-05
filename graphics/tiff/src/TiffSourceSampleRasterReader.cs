using System;

namespace tiff;

internal class TiffSourceSampleRasterReader
{
    internal static TiffRasterReadInfo CreateRasterInfo(TiffImageInfo info)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRasterInfo(info, diagnostics))
        {
            return TiffRasterReadInfo.Create(info, -1, 0, new int[0], new TiffSampleFormat[0], false, 0, 0, 0, 0, TiffByteOrder.Unknown, diagnostics);
        }

        int[] bitsPerSample = TiffImageInfo.CopyIntArray(info.BitsPerSample);
        TiffSampleFormat[] sampleFormats = TiffImageInfo.CopySampleFormats(info.SampleFormats);
        ulong bitsPerPixel = CalculateBitsPerPixel(bitsPerSample, diagnostics);
        bool isPacked = ContainsPackedSamples(bitsPerSample);
        ulong rowBitCount = CalculateRowBitCount(info.Width, bitsPerPixel, diagnostics);
        ulong rowByteCount = TiffStrileLayout.DivideRoundUp(rowBitCount, 8);
        ulong totalByteCount = CalculateTotalByteCount(rowByteCount, info.Length, diagnostics);
        TiffByteOrder byteOrder = GetHostByteOrder(isPacked);
        return TiffRasterReadInfo.Create(info, -1, info.SamplesPerPixel, bitsPerSample, sampleFormats, isPacked, bitsPerPixel, rowBitCount, rowByteCount, totalByteCount, byteOrder, diagnostics);
    }

    internal static TiffRasterReadInfo CreatePlaneInfo(TiffImageInfo info, int sampleIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRasterInfo(info, diagnostics))
        {
            return TiffRasterReadInfo.Create(info, sampleIndex, 0, new int[0], new TiffSampleFormat[0], false, 0, 0, 0, 0, TiffByteOrder.Unknown, diagnostics);
        }

        if (sampleIndex < 0 || sampleIndex >= info.SamplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample index is outside the SamplesPerPixel range.");
            return TiffRasterReadInfo.Create(info, sampleIndex, 0, new int[0], new TiffSampleFormat[0], false, 0, 0, 0, 0, TiffByteOrder.Unknown, diagnostics);
        }

        int[] bitsPerSample = new int[] { info.BitsPerSample[sampleIndex] };
        TiffSampleFormat[] sampleFormats = new TiffSampleFormat[] { info.SampleFormats[sampleIndex] };
        ulong bitsPerPixel = (ulong)bitsPerSample[0];
        bool isPacked = bitsPerSample[0] % 8 != 0;
        ulong rowBitCount = CalculateRowBitCount(info.Width, bitsPerPixel, diagnostics);
        ulong rowByteCount = TiffStrileLayout.DivideRoundUp(rowBitCount, 8);
        ulong totalByteCount = CalculateTotalByteCount(rowByteCount, info.Length, diagnostics);
        TiffByteOrder byteOrder = GetHostByteOrder(isPacked);
        return TiffRasterReadInfo.Create(info, sampleIndex, 1, bitsPerSample, sampleFormats, isPacked, bitsPerPixel, rowBitCount, rowByteCount, totalByteCount, byteOrder, diagnostics);
    }

    internal static bool ValidateRasterInfo(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to describe source samples.");
            return false;
        }

        if (info.Width == 0 || info.Length == 0 || info.SamplesPerPixel <= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image dimensions and SamplesPerPixel must be positive.");
            return false;
        }

        if (info.BitsPerSample == null || info.BitsPerSample.Length != info.SamplesPerPixel)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample count must match SamplesPerPixel.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        if (info.SampleFormats == null || info.SampleFormats.Length != info.SamplesPerPixel)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "SampleFormat count must match SamplesPerPixel.", info.ImageIndex, TiffTagId.SampleFormat);
            return false;
        }

        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            if (info.BitsPerSample[sampleIndex] <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be positive.", info.ImageIndex, TiffTagId.BitsPerSample);
                return false;
            }

            sampleIndex = sampleIndex + 1;
        }

        if (info.IsTiled)
        {
            return TiffTileRegionReader.ValidateTiledImage(info, diagnostics);
        }

        return TiffStrileLayout.ValidateStripInfo(info, diagnostics);
    }

    internal static ulong CalculateBitsPerPixel(int[] bitsPerSample, TiffDiagnostics diagnostics)
    {
        ulong bitsPerPixel = 0;
        int sampleIndex = 0;
        while (sampleIndex < bitsPerSample.Length)
        {
            if (bitsPerSample[sampleIndex] <= 0 || bitsPerPixel > ulong.MaxValue - (ulong)bitsPerSample[sampleIndex])
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Source raster pixel bit count overflows the supported range.");
                return 0;
            }

            bitsPerPixel = bitsPerPixel + (ulong)bitsPerSample[sampleIndex];
            sampleIndex = sampleIndex + 1;
        }

        return bitsPerPixel;
    }

    internal static bool ContainsPackedSamples(int[] bitsPerSample)
    {
        int sampleIndex = 0;
        while (sampleIndex < bitsPerSample.Length)
        {
            if (bitsPerSample[sampleIndex] % 8 != 0)
            {
                return true;
            }

            sampleIndex = sampleIndex + 1;
        }

        return false;
    }

    internal static ulong CalculateRowBitCount(ulong width, ulong bitsPerPixel, TiffDiagnostics diagnostics)
    {
        if (bitsPerPixel != 0 && width > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Source raster row bit count overflows the supported range.");
            return 0;
        }

        return width * bitsPerPixel;
    }

    internal static ulong CalculateTotalByteCount(ulong rowByteCount, ulong length, TiffDiagnostics diagnostics)
    {
        if (rowByteCount != 0 && length > ulong.MaxValue / rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Source raster byte count overflows the supported range.");
            return 0;
        }

        ulong totalByteCount = rowByteCount * length;
        if (totalByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Source raster byte count exceeds the supported caller-buffer range.");
        }

        return totalByteCount;
    }

    internal static TiffByteOrder GetHostByteOrder(bool isPacked)
    {
        if (isPacked)
        {
            return TiffByteOrder.Unknown;
        }

        if (BitConverter.IsLittleEndian)
        {
            return TiffByteOrder.LittleEndian;
        }

        return TiffByteOrder.BigEndian;
    }

    internal static TiffDiagnostics ReadRaster(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, Span<byte> destination)
    {
        return ReadRasterWithCodec(info, offsets, byteCounts, source, byteOrder, limits, destination, null);
    }

    internal static TiffDiagnostics ReadRasterWithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, Span<byte> destination, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRasterReadInfo readInfo = CreateRasterInfo(info);
        diagnostics.Merge(readInfo.Diagnostics);
        if (!AllowsDecodedRead(readInfo, limits, diagnostics))
        {
            return diagnostics;
        }

        if (!ValidateReadBuffers(info, offsets, byteCounts, readInfo, destination, diagnostics))
        {
            return diagnostics;
        }

        if (!readInfo.IsPacked)
        {
            TiffRegionReadOptions options = new TiffRegionReadOptions();
            options.Region.Width = info.Width;
            options.Region.Length = info.Length;
            options.Output = TiffRegionOutput.Normalized;
            TiffDiagnostics regionDiagnostics = TiffSourceRegionReader.ReadRegionWithCodec(info, offsets, byteCounts, source, byteOrder, limits, options, destination, compressionCodec);
            diagnostics.Merge(regionDiagnostics);
            return diagnostics;
        }

        destination.Slice(0, (int)readInfo.TotalByteCount).Clear();
        ulong destinationSampleBitOffset = 0;
        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            CopySamplePlane(info, offsets, byteCounts, source, byteOrder, limits, sampleIndex, readInfo.BitsPerPixel, destinationSampleBitOffset, readInfo.RowByteCount, destination, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            destinationSampleBitOffset = destinationSampleBitOffset + (ulong)info.BitsPerSample[sampleIndex];
            sampleIndex = sampleIndex + 1;
        }

        return diagnostics;
    }

    internal static TiffDiagnostics ReadPlane(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int sampleIndex, Span<byte> destination)
    {
        return ReadPlaneWithCodec(info, offsets, byteCounts, source, byteOrder, limits, sampleIndex, destination, null);
    }

    internal static TiffDiagnostics ReadPlaneWithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int sampleIndex, Span<byte> destination, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRasterReadInfo readInfo = CreatePlaneInfo(info, sampleIndex);
        diagnostics.Merge(readInfo.Diagnostics);
        if (!AllowsDecodedRead(readInfo, limits, diagnostics))
        {
            return diagnostics;
        }

        if (!ValidateReadBuffers(info, offsets, byteCounts, readInfo, destination, diagnostics))
        {
            return diagnostics;
        }

        destination.Slice(0, (int)readInfo.TotalByteCount).Clear();
        CopySamplePlane(info, offsets, byteCounts, source, byteOrder, limits, sampleIndex, readInfo.BitsPerPixel, 0, readInfo.RowByteCount, destination, diagnostics, compressionCodec);
        return diagnostics;
    }

    internal static bool AllowsDecodedRead(TiffRasterReadInfo readInfo, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (readInfo.TotalByteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)readInfo.TotalByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Decoded raster byte count exceeds the configured decoded-size limit.");
            return false;
        }

        return true;
    }

    internal static bool ValidateReadBuffers(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, TiffRasterReadInfo readInfo, Span<byte> destination, TiffDiagnostics diagnostics)
    {
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (readInfo.TotalByteCount > (ulong)destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Destination is shorter than the decoded source samples.");
            return false;
        }

        ulong segmentCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        if (info.IsTiled)
        {
            segmentCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        }

        return TiffSourceScanlineReader.ValidateSegmentArrays(offsets, byteCounts, segmentCount, info.IsTiled, diagnostics);
    }

    internal static void CopySamplePlane(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int sampleIndex, ulong outputPixelBitCount, ulong destinationSampleBitOffset, ulong destinationRowByteCount, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong sourcePixelBitCount = CalculateBitsPerPixel(info.BitsPerSample, diagnostics);
        ulong sourceSampleBitOffset = CalculateSourceSampleBitOffset(info, sampleIndex, diagnostics);
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            sourcePixelBitCount = (ulong)info.BitsPerSample[sampleIndex];
            sourceSampleBitOffset = 0;
        }

        if (diagnostics.HasErrors())
        {
            return;
        }

        ulong row = 0;
        while (row < info.Length)
        {
            if (info.IsTiled)
            {
                CopyTiledSampleRow(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, sourcePixelBitCount, sourceSampleBitOffset, outputPixelBitCount, destinationSampleBitOffset, destinationRowByteCount, destination, diagnostics, compressionCodec);
            }
            else
            {
                CopyStrippedSampleRow(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, sourcePixelBitCount, sourceSampleBitOffset, outputPixelBitCount, destinationSampleBitOffset, destinationRowByteCount, destination, diagnostics, compressionCodec);
            }

            if (diagnostics.HasErrors())
            {
                return;
            }

            row = row + 1;
        }
    }

    internal static ulong CalculateSourceSampleBitOffset(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        ulong bitOffset = 0;
        int index = 0;
        while (index < sampleIndex)
        {
            if (bitOffset > ulong.MaxValue - (ulong)info.BitsPerSample[index])
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Source sample bit offset overflows the supported range.");
                return 0;
            }

            bitOffset = bitOffset + (ulong)info.BitsPerSample[index];
            index = index + 1;
        }

        return bitOffset;
    }

    internal static void CopyStrippedSampleRow(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, ulong sourcePixelBitCount, ulong sourceSampleBitOffset, ulong outputPixelBitCount, ulong destinationSampleBitOffset, ulong destinationRowByteCount, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong stripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(info, row, sampleIndex, diagnostics);
        ulong decodedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        ulong sourceRowBitCount = CalculateRowBitCount(info.Width, sourcePixelBitCount, diagnostics);
        ulong sourceRowByteCount = TiffStrileLayout.DivideRoundUp(sourceRowBitCount, 8);
        ulong rowInStrip = row % info.RowsPerStrip;
        if (diagnostics.HasErrors())
        {
            return;
        }

        ulong sourceBitOffset = rowInStrip * sourceRowByteCount * 8 + sourceSampleBitOffset;
        ulong destinationBitOffset = row * destinationRowByteCount * 8 + destinationSampleBitOffset;
        TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)stripIndex], byteCounts[(int)stripIndex], diagnostics);
        CopySegmentSampleBits(info, range, false, stripIndex, decodedByteCount, sourceBitOffset, info.Width, sourcePixelBitCount, (ulong)info.BitsPerSample[sampleIndex], destinationBitOffset, outputPixelBitCount, source, byteOrder, limits, destination, diagnostics, compressionCodec);
    }

    internal static void CopyTiledSampleRow(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, ulong sourcePixelBitCount, ulong sourceSampleBitOffset, ulong outputPixelBitCount, ulong destinationSampleBitOffset, ulong destinationRowByteCount, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong currentColumn = 0;
        ulong remainingWidth = info.Width;
        while (remainingWidth > 0)
        {
            ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateDepthAndSample(info, currentColumn, row, 0, sampleIndex, diagnostics);
            ulong columnInTile = currentColumn % info.TileWidth;
            ulong rowInTile = row % info.TileLength;
            ulong copyPixelCount = TiffTileRegionReader.CalculateCopyPixelCount(info, columnInTile, remainingWidth, diagnostics);
            ulong decodedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
            ulong sourceRowBitCount = CalculateRowBitCount(info.TileWidth, sourcePixelBitCount, diagnostics);
            ulong sourceRowByteCount = TiffStrileLayout.DivideRoundUp(sourceRowBitCount, 8);
            if (diagnostics.HasErrors())
            {
                return;
            }

            ulong sourceBitOffset = rowInTile * sourceRowByteCount * 8 + columnInTile * sourcePixelBitCount + sourceSampleBitOffset;
            ulong destinationBitOffset = row * destinationRowByteCount * 8 + currentColumn * outputPixelBitCount + destinationSampleBitOffset;
            TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)tileIndex], byteCounts[(int)tileIndex], diagnostics);
            CopySegmentSampleBits(info, range, true, tileIndex, decodedByteCount, sourceBitOffset, copyPixelCount, sourcePixelBitCount, (ulong)info.BitsPerSample[sampleIndex], destinationBitOffset, outputPixelBitCount, source, byteOrder, limits, destination, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return;
            }

            currentColumn = currentColumn + copyPixelCount;
            remainingWidth = remainingWidth - copyPixelCount;
        }
    }

    internal static void CopySegmentSampleBits(TiffImageInfo info, TiffByteRange range, bool isTile, ulong segmentIndex, ulong decodedByteCount, ulong sourceBitOffset, ulong pixelCount, ulong sourcePixelBitCount, ulong sampleBitCount, ulong destinationBitOffset, ulong outputPixelBitCount, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (range.IsSparse)
        {
            return;
        }

        byte[] decodedBytes = TiffSourceScanlineReader.DecodeSourceSegment(info, range, isTile, segmentIndex, decodedByteCount, source, byteOrder, limits, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return;
        }

        ulong pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            ulong pixelSourceBitOffset = sourceBitOffset + pixelIndex * sourcePixelBitCount;
            ulong pixelDestinationBitOffset = destinationBitOffset + pixelIndex * outputPixelBitCount;
            CopySampleBits(decodedBytes, pixelSourceBitOffset, destination, pixelDestinationBitOffset, sampleBitCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            pixelIndex = pixelIndex + 1;
        }
    }

    internal static void CopySampleBits(byte[] source, ulong sourceBitOffset, Span<byte> destination, ulong destinationBitOffset, ulong bitCount, TiffDiagnostics diagnostics)
    {
        if (source == null || sourceBitOffset > ulong.MaxValue - bitCount || destinationBitOffset > ulong.MaxValue - bitCount || sourceBitOffset + bitCount > (ulong)source.Length * 8 || destinationBitOffset + bitCount > (ulong)destination.Length * 8)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sample bit range is outside the decoded source or caller buffer.");
            return;
        }

        if (sourceBitOffset % 8 == 0 && destinationBitOffset % 8 == 0 && bitCount % 8 == 0)
        {
            source.AsSpan((int)(sourceBitOffset / 8), (int)(bitCount / 8)).CopyTo(destination.Slice((int)(destinationBitOffset / 8), (int)(bitCount / 8)));
            return;
        }

        ulong bitIndex = 0;
        while (bitIndex < bitCount)
        {
            ulong sourcePosition = sourceBitOffset + bitIndex;
            ulong destinationPosition = destinationBitOffset + bitIndex;
            int sourceBit = (source[(int)(sourcePosition / 8)] >> (7 - (int)(sourcePosition % 8))) & 1;
            if (sourceBit != 0)
            {
                int destinationByteIndex = (int)(destinationPosition / 8);
                int destinationBitInByte = 7 - (int)(destinationPosition % 8);
                destination[destinationByteIndex] = (byte)(destination[destinationByteIndex] | (byte)(1 << destinationBitInByte));
            }

            bitIndex = bitIndex + 1;
        }
    }
}
