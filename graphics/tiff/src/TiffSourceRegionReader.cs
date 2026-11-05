using System;

namespace tiff;

internal class TiffSourceRegionReader
{
    internal static TiffRegionReadInfo CreateReadInfo(TiffImageInfo info, TiffRegionReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRegionReadOptions actualOptions = NormalizeRegionOptions(options);
        TiffRegion region = actualOptions.Region;
        if (!ValidateRegionRequest(info, actualOptions, diagnostics))
        {
            return TiffRegionReadInfo.Create(region, actualOptions.Output, actualOptions.SampleIndex, 0, new int[0], new TiffSampleFormat[0], TiffPlanarConfiguration.Contiguous, 0, 0, diagnostics);
        }

        int samplesPerPixel = info.SamplesPerPixel;
        int[] bitsPerSample = TiffImageInfo.CopyIntArray(info.BitsPerSample);
        TiffSampleFormat[] sampleFormats = TiffImageInfo.CopySampleFormats(info.SampleFormats);
        TiffPlanarConfiguration planarConfiguration = TiffPlanarConfiguration.Contiguous;
        ulong bytesPerPixel = 0;
        int resultSampleIndex = 0;
        if (actualOptions.Output == TiffRegionOutput.Source)
        {
            if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
            {
                resultSampleIndex = actualOptions.SampleIndex;
            }

            bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(info, resultSampleIndex, diagnostics);
            planarConfiguration = info.PlanarConfiguration;
            if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
            {
                samplesPerPixel = 1;
                bitsPerSample = CreateSelectedBits(info, resultSampleIndex, diagnostics);
                sampleFormats = CreateSelectedFormats(info, resultSampleIndex, diagnostics);
            }
        }
        else
        {
            bytesPerPixel = CalculateNormalizedBytesPerPixel(info, diagnostics);
        }

        ulong rowByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(region.Width, bytesPerPixel, diagnostics);
        ulong totalByteCount = CalculateCallerBufferByteCount(rowByteCount, region.Length, diagnostics);
        return TiffRegionReadInfo.Create(region, actualOptions.Output, resultSampleIndex, samplesPerPixel, bitsPerSample, sampleFormats, planarConfiguration, rowByteCount, totalByteCount, diagnostics);
    }

    internal static TiffDiagnostics ReadRegion(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRegionReadOptions options, Span<byte> destination)
    {
        return ReadRegionWithCodec(info, offsets, byteCounts, source, byteOrder, limits, options, destination, null);
    }

    internal static TiffDiagnostics ReadRegionWithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRegionReadOptions options, Span<byte> destination, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRegionReadInfo readInfo = CreateReadInfo(info, options);
        diagnostics.Merge(readInfo.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (readInfo.TotalByteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)readInfo.TotalByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Decoded region byte count exceeds the configured decoded-size limit.");
            return diagnostics;
        }

        if (readInfo.TotalByteCount > (ulong)destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Destination is shorter than the decoded region.");
            return diagnostics;
        }

        ulong segmentCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        if (info.IsTiled)
        {
            segmentCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        }

        if (!TiffSourceScanlineReader.ValidateSegmentArrays(offsets, byteCounts, segmentCount, info.IsTiled, diagnostics))
        {
            return diagnostics;
        }

        TiffRegionReadOptions actualOptions = NormalizeRegionOptions(options);
        if (actualOptions.Output == TiffRegionOutput.Normalized && info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ReadNormalizedSeparateRegion(info, offsets, byteCounts, source, byteOrder, limits, actualOptions.Region, readInfo, destination, diagnostics, compressionCodec);
        }
        else
        {
            ReadSourceLayoutRegion(info, offsets, byteCounts, source, byteOrder, limits, actualOptions.Region, readInfo.SampleIndex, readInfo, destination, diagnostics, compressionCodec);
        }

        return diagnostics;
    }

    internal static TiffRegionReadOptions NormalizeRegionOptions(TiffRegionReadOptions options)
    {
        if (options == null)
        {
            return new TiffRegionReadOptions();
        }

        return options.Copy();
    }

    internal static bool ValidateRegionRequest(TiffImageInfo info, TiffRegionReadOptions options, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read a region.");
            return false;
        }

        if (options == null || options.Region == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Region read options and bounds are required.");
            return false;
        }

        if (options.Output != TiffRegionOutput.Source && options.Output != TiffRegionOutput.Normalized)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Region output layout is not supported.");
            return false;
        }

        TiffRegion region = options.Region;
        if (region.Width == 0 || region.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Region dimensions must be positive.");
            return false;
        }

        int validationSampleIndex = 0;
        if (options.Output == TiffRegionOutput.Source && info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            validationSampleIndex = options.SampleIndex;
        }

        if (!TiffStrileLayout.ValidateRowAndSample(info, region.Row, validationSampleIndex, diagnostics))
        {
            return false;
        }

        if (region.Column >= info.Width)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region column is outside the image width.");
            return false;
        }

        if (region.Width > info.Width - region.Column || region.Length > info.Length - region.Row)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region extends outside the image bounds.");
            return false;
        }

        if (info.IsTiled)
        {
            return TiffTileRegionReader.ValidateTiledImage(info, diagnostics);
        }

        return TiffStrileLayout.ValidateStripInfo(info, diagnostics);
    }

    internal static int[] CreateSelectedBits(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || sampleIndex < 0 || sampleIndex >= info.BitsPerSample.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain the selected region sample.", info.ImageIndex, TiffTagId.BitsPerSample);
            return new int[0];
        }

        return new int[] { info.BitsPerSample[sampleIndex] };
    }

    internal static TiffSampleFormat[] CreateSelectedFormats(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.SampleFormats == null || sampleIndex < 0 || sampleIndex >= info.SampleFormats.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "SampleFormat does not contain the selected region sample.", info.ImageIndex, TiffTagId.SampleFormat);
            return new TiffSampleFormat[0];
        }

        return new TiffSampleFormat[] { info.SampleFormats[sampleIndex] };
    }

    internal static ulong CalculateNormalizedBytesPerPixel(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || info.BitsPerSample.Length != info.SamplesPerPixel)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample count must match SamplesPerPixel for normalized region output.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        ulong byteCount = 0;
        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            int bits = info.BitsPerSample[sampleIndex];
            if (bits <= 0 || bits % 8 != 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Normalized region output currently requires byte-aligned source samples.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            ulong sampleByteCount = (ulong)(bits / 8);
            if (byteCount > ulong.MaxValue - sampleByteCount)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Normalized region pixel byte count overflows the supported range.");
                return 0;
            }

            byteCount = byteCount + sampleByteCount;
            sampleIndex = sampleIndex + 1;
        }

        return byteCount;
    }

    internal static ulong CalculateCallerBufferByteCount(ulong rowByteCount, ulong length, TiffDiagnostics diagnostics)
    {
        if (rowByteCount != 0 && length > ulong.MaxValue / rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region byte count overflows the supported range.");
            return 0;
        }

        ulong totalByteCount = rowByteCount * length;
        if (totalByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Region byte count exceeds the supported caller-buffer range.");
        }

        return totalByteCount;
    }

    internal static void ReadSourceLayoutRegion(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRegion region, int sampleIndex, TiffRegionReadInfo readInfo, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong bytesPerPixel = readInfo.RowByteCount / region.Width;
        ulong rowIndex = 0;
        while (rowIndex < region.Length)
        {
            ulong destinationOffset = rowIndex * readInfo.RowByteCount;
            Span<byte> rowDestination = destination.Slice((int)destinationOffset, (int)readInfo.RowByteCount);
            ReadSampleRowWithStride(info, offsets, byteCounts, source, byteOrder, limits, region.Row + rowIndex, sampleIndex, region.Column, region.Width, bytesPerPixel, 0, rowDestination, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void ReadNormalizedSeparateRegion(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRegion region, TiffRegionReadInfo readInfo, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong outputPixelByteCount = readInfo.RowByteCount / region.Width;
        ulong rowIndex = 0;
        while (rowIndex < region.Length)
        {
            ulong rowDestinationOffset = rowIndex * readInfo.RowByteCount;
            Span<byte> rowDestination = destination.Slice((int)rowDestinationOffset, (int)readInfo.RowByteCount);
            ulong channelOffset = 0;
            int sampleIndex = 0;
            while (sampleIndex < info.SamplesPerPixel)
            {
                ulong sampleByteCount = (ulong)(info.BitsPerSample[sampleIndex] / 8);
                ReadSampleRowWithStride(info, offsets, byteCounts, source, byteOrder, limits, region.Row + rowIndex, sampleIndex, region.Column, region.Width, outputPixelByteCount, channelOffset, rowDestination, diagnostics, compressionCodec);
                if (diagnostics.HasErrors())
                {
                    return;
                }

                channelOffset = channelOffset + sampleByteCount;
                sampleIndex = sampleIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void ReadSampleRowWithStride(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, ulong column, ulong width, ulong outputPixelByteCount, ulong channelOffset, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong sourcePixelByteCount = TiffStripRegionReader.CalculateBytesPerPixel(info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        if (info.IsTiled)
        {
            ReadTiledSampleRow(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, column, width, sourcePixelByteCount, outputPixelByteCount, channelOffset, destination, diagnostics, compressionCodec);
        }
        else
        {
            ReadStrippedSampleRow(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, column, width, sourcePixelByteCount, outputPixelByteCount, channelOffset, destination, diagnostics, compressionCodec);
        }
    }

    internal static void ReadStrippedSampleRow(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, ulong column, ulong width, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong stripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(info, row, sampleIndex, diagnostics);
        ulong scanlineByteCount = TiffSourceScanlineReader.CalculateScanlineByteCount(info, sampleIndex, diagnostics);
        ulong rowSourceOffset = TiffScanlineReader.CalculateSourceOffset(info, row, stripIndex, scanlineByteCount, diagnostics);
        ulong columnByteOffset = TiffStripRegionReader.CalculateColumnByteOffset(column, sourcePixelByteCount, diagnostics);
        ulong decodedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        if (rowSourceOffset > ulong.MaxValue - columnByteOffset)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Region source offset overflows the supported range.");
            return;
        }

        ulong sourceOffset = rowSourceOffset + columnByteOffset;
        TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)stripIndex], byteCounts[(int)stripIndex], diagnostics);
        CopySegmentPixels(info, range, false, stripIndex, decodedByteCount, sourceOffset, width, sourcePixelByteCount, outputPixelByteCount, channelOffset, 0, source, byteOrder, limits, destination, diagnostics, compressionCodec);
    }

    internal static void ReadTiledSampleRow(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, ulong column, ulong width, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong currentColumn = column;
        ulong remainingWidth = width;
        ulong outputPixelOffset = 0;
        while (remainingWidth > 0)
        {
            ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateDepthAndSample(info, currentColumn, row, 0, sampleIndex, diagnostics);
            ulong columnInTile = currentColumn % info.TileWidth;
            ulong rowInTile = row % info.TileLength;
            ulong copyPixelCount = TiffTileRegionReader.CalculateCopyPixelCount(info, columnInTile, remainingWidth, diagnostics);
            ulong sourceOffset = TiffTileRegionReader.CalculateTileSourceOffset(info, columnInTile, rowInTile, sourcePixelByteCount, diagnostics);
            ulong decodedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)tileIndex], byteCounts[(int)tileIndex], diagnostics);
            CopySegmentPixels(info, range, true, tileIndex, decodedByteCount, sourceOffset, copyPixelCount, sourcePixelByteCount, outputPixelByteCount, channelOffset, outputPixelOffset, source, byteOrder, limits, destination, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return;
            }

            currentColumn = currentColumn + copyPixelCount;
            remainingWidth = remainingWidth - copyPixelCount;
            outputPixelOffset = outputPixelOffset + copyPixelCount;
        }
    }

    internal static void CopySegmentPixels(TiffImageInfo info, TiffByteRange range, bool isTile, ulong segmentIndex, ulong decodedByteCount, ulong sourceOffset, ulong pixelCount, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, ulong outputPixelOffset, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (range.IsSparse)
        {
            ClearOutputPixels(pixelCount, sourcePixelByteCount, outputPixelByteCount, channelOffset, outputPixelOffset, destination);
            return;
        }

        bool contiguousOutput = outputPixelByteCount == sourcePixelByteCount && channelOffset == 0;
        if (TiffSourceScanlineReader.CanReadSourceBytesDirectlyWithCodec(info, compressionCodec) && range.ByteCount == decodedByteCount)
        {
            if (contiguousOutput)
            {
                ulong destinationOffset = outputPixelOffset * outputPixelByteCount;
                ulong byteCount = pixelCount * sourcePixelByteCount;
                TiffSourceScanlineReader.ReadDirectSegmentPortion(range, sourceOffset, destination.Slice((int)destinationOffset, (int)byteCount), source, diagnostics);
                return;
            }

            ReadDirectPixels(range, sourceOffset, pixelCount, sourcePixelByteCount, outputPixelByteCount, channelOffset, outputPixelOffset, source, destination, diagnostics);
            return;
        }

        byte[] decodedBytes = TiffSourceScanlineReader.DecodeSourceSegment(info, range, isTile, segmentIndex, decodedByteCount, source, byteOrder, limits, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return;
        }

        CopyDecodedPixels(decodedBytes, sourceOffset, pixelCount, sourcePixelByteCount, outputPixelByteCount, channelOffset, outputPixelOffset, destination, diagnostics);
    }

    internal static void ReadDirectPixels(TiffByteRange range, ulong sourceOffset, ulong pixelCount, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, ulong outputPixelOffset, ITiffRandomAccessSource source, Span<byte> destination, TiffDiagnostics diagnostics)
    {
        ulong pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            ulong pixelSourceOffset = sourceOffset + pixelIndex * sourcePixelByteCount;
            ulong destinationOffset = (outputPixelOffset + pixelIndex) * outputPixelByteCount + channelOffset;
            TiffSourceScanlineReader.ReadDirectSegmentPortion(range, pixelSourceOffset, destination.Slice((int)destinationOffset, (int)sourcePixelByteCount), source, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            pixelIndex = pixelIndex + 1;
        }
    }

    internal static void CopyDecodedPixels(byte[] decodedBytes, ulong sourceOffset, ulong pixelCount, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, ulong outputPixelOffset, Span<byte> destination, TiffDiagnostics diagnostics)
    {
        if (decodedBytes == null || sourceOffset > (ulong)decodedBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Region byte range is outside the decoded segment.");
            return;
        }

        ulong pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            ulong pixelSourceOffset = sourceOffset + pixelIndex * sourcePixelByteCount;
            ulong destinationOffset = (outputPixelOffset + pixelIndex) * outputPixelByteCount + channelOffset;
            if (pixelSourceOffset > int.MaxValue || pixelSourceOffset > (ulong)decodedBytes.Length || sourcePixelByteCount > (ulong)decodedBytes.Length - pixelSourceOffset)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Region byte range is outside the decoded segment.");
                return;
            }

            decodedBytes.AsSpan((int)pixelSourceOffset, (int)sourcePixelByteCount).CopyTo(destination.Slice((int)destinationOffset, (int)sourcePixelByteCount));
            pixelIndex = pixelIndex + 1;
        }
    }

    internal static void ClearOutputPixels(ulong pixelCount, ulong sourcePixelByteCount, ulong outputPixelByteCount, ulong channelOffset, ulong outputPixelOffset, Span<byte> destination)
    {
        ulong pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            ulong destinationOffset = (outputPixelOffset + pixelIndex) * outputPixelByteCount + channelOffset;
            destination.Slice((int)destinationOffset, (int)sourcePixelByteCount).Clear();
            pixelIndex = pixelIndex + 1;
        }
    }
}
