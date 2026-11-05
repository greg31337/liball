using System;

namespace tiff;

internal class TiffSourceScanlineReader
{
    internal static TiffDiagnostics ReadScanline(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, Span<byte> destination)
    {
        return ReadScanlineWithCodec(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, destination, null);
    }

    internal static TiffDiagnostics ReadScanlineWithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, Span<byte> destination, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read a scanline.");
            return diagnostics;
        }

        if (info.IsTiled)
        {
            ReadTiledScanline(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, destination, diagnostics, compressionCodec);
        }
        else
        {
            ReadStrippedScanline(info, offsets, byteCounts, source, byteOrder, limits, row, sampleIndex, destination, diagnostics, compressionCodec);
        }

        return diagnostics;
    }

    internal static ulong CalculateScanlineByteCount(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to calculate a scanline size.");
            return 0;
        }

        if (info.Width == 0 || info.SamplesPerPixel <= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image width and SamplesPerPixel must be positive.");
            return 0;
        }

        if (sampleIndex < 0 || sampleIndex >= info.SamplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample index is outside the SamplesPerPixel range.");
            return 0;
        }

        ulong bitsPerPixel = CalculateScanlineBitsPerPixel(info, sampleIndex, diagnostics);
        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (info.Width > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline bit count overflows the supported range.");
            return 0;
        }

        return TiffStrileLayout.CalculateByteCountForRows(info.Width * bitsPerPixel, 1, "Scanline byte count overflows the supported range.", diagnostics);
    }

    internal static ulong CalculateScanlineBitsPerPixel(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain at least one value.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            if (sampleIndex >= info.BitsPerSample.Length)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain a value for the requested sample plane.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            if (info.BitsPerSample[sampleIndex] <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be positive.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            return (ulong)info.BitsPerSample[sampleIndex];
        }

        return TiffStrileLayout.SumBitsPerPixel(info.BitsPerSample, diagnostics);
    }

    internal static void ReadStrippedScanline(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffStrileLayout.ValidateStripInfo(info, diagnostics) || !TiffStrileLayout.ValidateRowAndSample(info, row, sampleIndex, diagnostics))
        {
            return;
        }

        ulong scanlineByteCount = CalculateScanlineByteCount(info, sampleIndex, diagnostics);
        if (!AllowsDecodedByteCount(scanlineByteCount, limits, diagnostics))
        {
            return;
        }

        if (!ValidateDestinationBuffer(destination, scanlineByteCount, diagnostics))
        {
            return;
        }

        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        if (!ValidateSegmentArrays(offsets, byteCounts, stripCount, false, diagnostics))
        {
            return;
        }

        ulong stripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(info, row, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)stripIndex], byteCounts[(int)stripIndex], diagnostics);
        ulong sourceOffset = TiffScanlineReader.CalculateSourceOffset(info, row, stripIndex, scanlineByteCount, diagnostics);
        ulong decodedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        Span<byte> scanlineDestination = destination.Slice(0, (int)scanlineByteCount);
        if (range.IsSparse)
        {
            scanlineDestination.Clear();
            return;
        }

        if (CanReadSourceBytesDirectlyWithCodec(info, compressionCodec) && range.ByteCount == decodedByteCount)
        {
            ReadDirectSegmentPortion(range, sourceOffset, scanlineDestination, source, diagnostics);
            return;
        }

        byte[] decodedBytes = DecodeSourceSegment(info, range, false, stripIndex, decodedByteCount, source, byteOrder, limits, diagnostics, compressionCodec);
        CopyDecodedSegmentPortion(decodedBytes, sourceOffset, scanlineDestination, diagnostics);
    }

    internal static void ReadTiledScanline(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, ulong row, int sampleIndex, Span<byte> destination, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffTileRegionReader.ValidateTiledImage(info, diagnostics) || !TiffStrileLayout.ValidateRowAndSample(info, row, sampleIndex, diagnostics))
        {
            return;
        }

        ulong bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(info, sampleIndex, diagnostics);
        ulong scanlineByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(info.Width, bytesPerPixel, diagnostics);
        if (!AllowsDecodedByteCount(scanlineByteCount, limits, diagnostics))
        {
            return;
        }

        if (!ValidateDestinationBuffer(destination, scanlineByteCount, diagnostics))
        {
            return;
        }

        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        if (!ValidateSegmentArrays(offsets, byteCounts, tileCount, true, diagnostics))
        {
            return;
        }

        ulong currentColumn = 0;
        ulong destinationOffset = 0;
        while (currentColumn < info.Width)
        {
            ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateDepthAndSample(info, currentColumn, row, 0, sampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            ulong columnInTile = currentColumn % info.TileWidth;
            ulong rowInTile = row % info.TileLength;
            ulong remainingWidth = info.Width - currentColumn;
            ulong copyPixelCount = TiffTileRegionReader.CalculateCopyPixelCount(info, columnInTile, remainingWidth, diagnostics);
            ulong copyByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(copyPixelCount, bytesPerPixel, diagnostics);
            ulong sourceOffset = TiffTileRegionReader.CalculateTileSourceOffset(info, columnInTile, rowInTile, bytesPerPixel, diagnostics);
            ulong decodedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            TiffByteRange range = TiffStrileLayout.CreateByteRange(offsets[(int)tileIndex], byteCounts[(int)tileIndex], diagnostics);
            Span<byte> tileDestination = destination.Slice((int)destinationOffset, (int)copyByteCount);
            if (range.IsSparse)
            {
                tileDestination.Clear();
            }
            else if (CanReadSourceBytesDirectlyWithCodec(info, compressionCodec) && range.ByteCount == decodedByteCount)
            {
                ReadDirectSegmentPortion(range, sourceOffset, tileDestination, source, diagnostics);
            }
            else
            {
                byte[] decodedBytes = DecodeSourceSegment(info, range, true, tileIndex, decodedByteCount, source, byteOrder, limits, diagnostics, compressionCodec);
                CopyDecodedSegmentPortion(decodedBytes, sourceOffset, tileDestination, diagnostics);
            }

            if (diagnostics.HasErrors())
            {
                return;
            }

            currentColumn = currentColumn + copyPixelCount;
            destinationOffset = destinationOffset + copyByteCount;
        }
    }

    internal static bool ValidateDestinationBuffer(Span<byte> destination, ulong byteCount, TiffDiagnostics diagnostics)
    {
        if (byteCount > int.MaxValue || byteCount > (ulong)destination.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Destination is shorter than the decoded scanline.");
            return false;
        }

        return !diagnostics.HasErrors();
    }

    internal static bool ValidateSegmentArrays(ulong[] offsets, ulong[] byteCounts, ulong segmentCount, bool isTile, TiffDiagnostics diagnostics)
    {
        if (segmentCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Segment count exceeds the supported array range.");
            return false;
        }

        int count = (int)segmentCount;
        if (offsets == null || offsets.Length != count)
        {
            int tagId = TiffTagId.StripOffsets;
            if (isTile)
            {
                tagId = TiffTagId.TileOffsets;
            }

            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Segment offset count does not match the calculated segment count.", TiffDiagnostic.NoImageIndex, tagId);
            return false;
        }

        if (byteCounts == null || byteCounts.Length != count)
        {
            int tagId = TiffTagId.StripByteCounts;
            if (isTile)
            {
                tagId = TiffTagId.TileByteCounts;
            }

            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Segment byte-count count does not match the calculated segment count.", TiffDiagnostic.NoImageIndex, tagId);
            return false;
        }

        return !diagnostics.HasErrors();
    }

    internal static bool CanReadSourceBytesDirectly(TiffImageInfo info)
    {
        if (info.Compression != TiffCompression.None || info.Predictor != 1 || info.FillOrder != 1 || info.BitsPerSample == null)
        {
            return false;
        }

        int index = 0;
        while (index < info.BitsPerSample.Length)
        {
            if (info.BitsPerSample[index] <= 0 || info.BitsPerSample[index] > 8)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AllowsDecodedByteCount(ulong byteCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (byteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Decoded scanline byte count exceeds the configured decoded-size limit.");
            return false;
        }

        return true;
    }

    internal static bool CanReadSourceBytesDirectlyWithCodec(TiffImageInfo info, ITiffCompressionCodec? compressionCodec)
    {
        if (compressionCodec != null && compressionCodec.CanDecode(info.Compression))
        {
            return false;
        }

        return CanReadSourceBytesDirectly(info);
    }

    internal static void ReadDirectSegmentPortion(TiffByteRange range, ulong sourceOffset, Span<byte> destination, ITiffRandomAccessSource source, TiffDiagnostics diagnostics)
    {
        ulong byteCount = (ulong)destination.Length;
        if (sourceOffset > range.ByteCount || byteCount > range.ByteCount - sourceOffset)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Scanline byte range is outside the stored segment.");
            return;
        }

        if (range.Offset > ulong.MaxValue - sourceOffset || range.Offset + sourceOffset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Scanline source offset exceeds the supported range.");
            return;
        }

        TiffRandomAccessSourceReader.ReadToBuffer(source, (long)(range.Offset + sourceOffset), destination, diagnostics);
    }

    internal static byte[] DecodeSourceSegment(TiffImageInfo info, TiffByteRange range, bool isTile, ulong segmentIndex, ulong decodedByteCount, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (range == null || range.Offset > long.MaxValue || range.ByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Encoded segment byte range exceeds the supported range.");
            return new byte[0];
        }

        if (!actualLimits.AllowsAllocation((long)range.ByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Encoded segment byte count exceeds the configured allocation limit.");
            return new byte[0];
        }

        if (decodedByteCount > int.MaxValue || decodedByteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)decodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.CodecExpansionLimitExceeded, "Decoded segment byte count exceeds the configured decoded-size limit.");
            return new byte[0];
        }

        if (source == null || !source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            return new byte[0];
        }

        if (!TiffMath.IsByteRangeWithinLength((long)range.Offset, (int)range.ByteCount, source.GetLength()))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "The encoded segment byte range is outside the TIFF source.");
            return new byte[0];
        }

        using (TiffSegmentBuffer rawBuffer = new TiffSegmentBuffer((int)range.ByteCount, actualLimits))
        {
            if (!TiffRandomAccessSourceReader.ReadToBuffer(source, (long)range.Offset, rawBuffer.GetSpan(), diagnostics))
            {
                return new byte[0];
            }

            TiffCodecContext context = TiffCodecContext.Create(info, isTile, segmentIndex, decodedByteCount, actualLimits);
            context.ByteOrder = byteOrder;
            TiffCodecResult result = TiffCodec.Decode(context, rawBuffer.GetReadOnlySpan(), compressionCodec);
            diagnostics.Merge(result.Diagnostics);
            return result.Bytes;
        }
    }

    internal static void CopyDecodedSegmentPortion(byte[] decodedBytes, ulong sourceOffset, Span<byte> destination, TiffDiagnostics diagnostics)
    {
        if (decodedBytes == null || sourceOffset > int.MaxValue || sourceOffset > ulong.MaxValue - (ulong)destination.Length || sourceOffset + (ulong)destination.Length > (ulong)decodedBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Scanline byte range is outside the decoded segment.");
            return;
        }

        decodedBytes.AsSpan((int)sourceOffset, destination.Length).CopyTo(destination);
    }
}
