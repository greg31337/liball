using System;

namespace tiff;

internal class TiffConfiguredRasterWriter
{
    internal static TiffDiagnostics Write(TiffWriterImageState state, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        return WriteWithCodec(state, region, sampleIndex, source, byteOrder, limits, null);
    }

    internal static TiffDiagnostics WriteWithCodec(TiffWriterImageState state, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, TiffByteOrder byteOrder, TiffLimits limits, ITiffCompressionCodec? compressionCodec)
    {
        return WriteCore(state, region, sampleIndex, source, byteOrder, limits, compressionCodec, null);
    }

    internal static TiffDiagnostics WriteWithOperation(TiffWriterImageState state, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, TiffByteOrder byteOrder, TiffLimits limits, ITiffCompressionCodec? compressionCodec, TiffOperationMonitor monitor)
    {
        return WriteCore(state, region, sampleIndex, source, byteOrder, limits, compressionCodec, monitor);
    }

    internal static TiffDiagnostics WriteCore(TiffWriterImageState state, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, TiffByteOrder byteOrder, TiffLimits limits, ITiffCompressionCodec? compressionCodec, TiffOperationMonitor? monitor)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRequest(state, region, sampleIndex, source, diagnostics))
        {
            if (monitor != null)
            {
                monitor.Start(0);
            }

            return diagnostics;
        }

        TiffImageInfo info = state.ConfiguredInfo;
        int[] sampleByteCounts = TiffSourceRasterWriter.CalculateByteAlignedSampleByteCounts(info, diagnostics);
        int bytesPerPixel = CalculateBytesPerPixel(sampleByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            if (monitor != null)
            {
                monitor.Start(0);
            }

            return diagnostics;
        }

        ValidateSourceByteCount(region, sampleIndex, source, sampleByteCounts, bytesPerPixel, diagnostics);
        if (diagnostics.HasErrors())
        {
            if (monitor != null)
            {
                monitor.Start(0);
            }

            return diagnostics;
        }

        long totalUnits = state.ConfiguredSegmentCount;
        if (monitor != null)
        {
            monitor.Start(totalUnits);
        }

        if (info.IsTiled)
        {
            WriteTiles(state, region, sampleIndex, source, sampleByteCounts, bytesPerPixel, byteOrder, limits, diagnostics, compressionCodec, monitor, totalUnits);
        }
        else
        {
            WriteStrips(state, region, sampleIndex, source, sampleByteCounts, bytesPerPixel, byteOrder, limits, diagnostics, compressionCodec, monitor, totalUnits);
        }

        if (monitor != null && !diagnostics.HasErrors())
        {
            monitor.Complete(totalUnits);
        }

        return diagnostics;
    }

    internal static bool ValidateRequest(TiffWriterImageState state, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (state == null || !state.IsConfigured)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Scanline, region, and sample-plane writing requires an image added with AddConfiguredImage.");
            return false;
        }

        TiffImageInfo info = state.ConfiguredInfo;
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Configured image information is unavailable.");
            return false;
        }

        if (info.ImageDepth != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Two-dimensional writer operations require ImageDepth to be one.", info.ImageIndex, TiffTagId.ImageDepth);
        }

        if (region == null || region.Width == 0 || region.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Writer region dimensions must be positive.");
            return false;
        }

        if (region.Column >= info.Width || region.Row >= info.Length || region.Width > info.Width - region.Column || region.Length > info.Length - region.Row)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Writer region extends outside the configured image bounds.");
        }

        if (sampleIndex < -1 || sampleIndex >= info.SamplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Writer sample index is outside the configured source sample range.");
        }

        return !diagnostics.HasErrors();
    }

    internal static int CalculateBytesPerPixel(int[] sampleByteCounts, TiffDiagnostics diagnostics)
    {
        int bytesPerPixel = 0;
        int index = 0;
        while (index < sampleByteCounts.Length)
        {
            if (sampleByteCounts[index] > int.MaxValue - bytesPerPixel)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Configured source pixel byte count overflows the supported range.");
                return 0;
            }

            bytesPerPixel = bytesPerPixel + sampleByteCounts[index];
            index = index + 1;
        }

        return bytesPerPixel;
    }

    internal static void ValidateSourceByteCount(TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        ulong pixelByteCount = (ulong)bytesPerPixel;
        if (sampleIndex >= 0)
        {
            pixelByteCount = (ulong)sampleByteCounts[sampleIndex];
        }

        if (region.Width > ulong.MaxValue / region.Length || region.Width * region.Length > ulong.MaxValue / pixelByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Writer source byte count overflows the supported range.");
            return;
        }

        ulong expectedByteCount = region.Width * region.Length * pixelByteCount;
        if ((ulong)source.Length != expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Writer source byte count does not match the requested region and sample ordering.");
        }
    }

    internal static void WriteStrips(TiffWriterImageState state, TiffRegion region, int selectedSample, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec, TiffOperationMonitor? monitor, long totalUnits)
    {
        TiffImageInfo info = state.ConfiguredInfo;
        int stripIndex = 0;
        while (stripIndex < state.ConfiguredSegmentCount)
        {
            if (monitor != null)
            {
                monitor.CheckCancellation();
            }

            if (!StripIntersects(info, stripIndex, region, selectedSample))
            {
                stripIndex = stripIndex + 1;
                ReportWriteAdvance(stripIndex, state.ConfiguredSegmentCount, monitor, totalUnits);
                continue;
            }

            byte[] decoded = GetDecodedStrip(state, stripIndex, byteOrder, limits, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return;
            }

            bool changed = CopyStripRegion(info, stripIndex, region, selectedSample, source, sampleByteCounts, bytesPerPixel, decoded);
            if (changed)
            {
                state.DecodedStrips[stripIndex] = decoded;
                EncodeStrip(state, stripIndex, decoded, byteOrder, limits, diagnostics, compressionCodec);
                if (diagnostics.HasErrors())
                {
                    return;
                }
            }

            stripIndex = stripIndex + 1;
            ReportWriteAdvance(stripIndex, state.ConfiguredSegmentCount, monitor, totalUnits);
        }
    }

    internal static void WriteTiles(TiffWriterImageState state, TiffRegion region, int selectedSample, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec, TiffOperationMonitor? monitor, long totalUnits)
    {
        TiffImageInfo info = state.ConfiguredInfo;
        int tileIndex = 0;
        while (tileIndex < state.ConfiguredSegmentCount)
        {
            if (monitor != null)
            {
                monitor.CheckCancellation();
            }

            if (!TileIntersects(info, tileIndex, region, selectedSample, diagnostics))
            {
                if (diagnostics.HasErrors())
                {
                    return;
                }

                tileIndex = tileIndex + 1;
                ReportWriteAdvance(tileIndex, state.ConfiguredSegmentCount, monitor, totalUnits);
                continue;
            }

            byte[] decoded = GetDecodedTile(state, tileIndex, byteOrder, limits, diagnostics, compressionCodec);
            if (diagnostics.HasErrors())
            {
                return;
            }

            bool changed = CopyTileRegion(info, tileIndex, region, selectedSample, source, sampleByteCounts, bytesPerPixel, decoded, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            if (changed)
            {
                state.DecodedTiles[tileIndex] = decoded;
                EncodeTile(state, tileIndex, decoded, byteOrder, limits, diagnostics, compressionCodec);
                if (diagnostics.HasErrors())
                {
                    return;
                }
            }

            tileIndex = tileIndex + 1;
            ReportWriteAdvance(tileIndex, state.ConfiguredSegmentCount, monitor, totalUnits);
        }
    }

    internal static void ReportWriteAdvance(int completedUnits, int segmentCount, TiffOperationMonitor? monitor, long totalUnits)
    {
        if (monitor != null && completedUnits < segmentCount)
        {
            monitor.Advance(completedUnits, totalUnits);
        }
    }

    internal static bool StripIntersects(TiffImageInfo info, int stripIndex, TiffRegion region, int selectedSample)
    {
        ulong stripsPerPlane = TiffStrileLayout.DivideRoundUp(info.Length, info.RowsPerStrip);
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate && selectedSample >= 0 && (int)((ulong)stripIndex / stripsPerPlane) != selectedSample)
        {
            return false;
        }

        ulong stripIndexInPlane = (ulong)stripIndex % stripsPerPlane;
        ulong firstRow = stripIndexInPlane * info.RowsPerStrip;
        ulong lastRow = Minimum(firstRow + info.RowsPerStrip, info.Length);
        return Maximum(firstRow, region.Row) < Minimum(lastRow, region.Row + region.Length);
    }

    internal static bool TileIntersects(TiffImageInfo info, int tileIndex, TiffRegion region, int selectedSample, TiffDiagnostics diagnostics)
    {
        ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate && selectedSample >= 0 && (int)((ulong)tileIndex / tilesPerPlane) != selectedSample)
        {
            return false;
        }

        ulong tileIndexInPlane = (ulong)tileIndex % tilesPerPlane;
        ulong firstColumn = (tileIndexInPlane % tilesAcross) * info.TileWidth;
        ulong firstRow = (tileIndexInPlane / tilesAcross) * info.TileLength;
        ulong lastColumn = Minimum(firstColumn + info.TileWidth, info.Width);
        ulong lastRow = Minimum(firstRow + info.TileLength, info.Length);
        bool columnsIntersect = Maximum(firstColumn, region.Column) < Minimum(lastColumn, region.Column + region.Width);
        bool rowsIntersect = Maximum(firstRow, region.Row) < Minimum(lastRow, region.Row + region.Length);
        return columnsIntersect && rowsIntersect;
    }

    internal static byte[] GetDecodedStrip(TiffWriterImageState state, int stripIndex, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong expectedByteCount = TiffStrileLayout.CalculateStripByteCount(state.ConfiguredInfo, (ulong)stripIndex, diagnostics);
        if (!ValidateDecodedAllocation(expectedByteCount, limits, diagnostics))
        {
            return new byte[0];
        }

        if (stripIndex < state.DecodedStrips.Length && state.DecodedStrips[stripIndex].Length == (int)expectedByteCount)
        {
            return state.DecodedStrips[stripIndex];
        }

        byte[] rawBytes = state.Definition.RawStrips[stripIndex];
        if (rawBytes.Length > 0)
        {
            return DecodeSegment(state.ConfiguredInfo, false, stripIndex, expectedByteCount, rawBytes, byteOrder, limits, diagnostics, compressionCodec);
        }

        return new byte[(int)expectedByteCount];
    }

    internal static byte[] GetDecodedTile(TiffWriterImageState state, int tileIndex, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        ulong expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(state.ConfiguredInfo, (ulong)tileIndex, diagnostics);
        if (!ValidateDecodedAllocation(expectedByteCount, limits, diagnostics))
        {
            return new byte[0];
        }

        if (tileIndex < state.DecodedTiles.Length && state.DecodedTiles[tileIndex].Length == (int)expectedByteCount)
        {
            return state.DecodedTiles[tileIndex];
        }

        byte[] rawBytes = state.Definition.RawTiles[tileIndex];
        if (rawBytes.Length > 0)
        {
            return DecodeSegment(state.ConfiguredInfo, true, tileIndex, expectedByteCount, rawBytes, byteOrder, limits, diagnostics, compressionCodec);
        }

        return new byte[(int)expectedByteCount];
    }

    internal static bool ValidateDecodedAllocation(ulong byteCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (byteCount > int.MaxValue || !limits.AllowsAllocation((long)byteCount) || !limits.AllowsDecodedBytes((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Configured decoded segment exceeds the writer limits.");
            return false;
        }

        return !diagnostics.HasErrors();
    }

    internal static byte[] DecodeSegment(TiffImageInfo info, bool isTile, int segmentIndex, ulong expectedByteCount, byte[] rawBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        TiffCodecContext context = TiffCodecContext.Create(info, isTile, (ulong)segmentIndex, expectedByteCount, limits);
        context.ByteOrder = byteOrder;
        TiffCodecResult result = TiffCodec.Decode(context, rawBytes, compressionCodec);
        diagnostics.Merge(result.Diagnostics);
        if (!result.Success)
        {
            return new byte[0];
        }

        return result.Bytes;
    }

    internal static void EncodeStrip(TiffWriterImageState state, int stripIndex, byte[] decoded, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        byte[] encoded = EncodeSegment(state.ConfiguredInfo, false, stripIndex, decoded, byteOrder, limits, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return;
        }

        if (state.AllowsSparseSegments && TiffSparseSegment.IsAllZero(decoded))
        {
            encoded = new byte[0];
        }

        state.Definition.SetRawStrip(stripIndex, encoded);
    }

    internal static void EncodeTile(TiffWriterImageState state, int tileIndex, byte[] decoded, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        byte[] encoded = EncodeSegment(state.ConfiguredInfo, true, tileIndex, decoded, byteOrder, limits, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return;
        }

        if (state.AllowsSparseSegments && TiffSparseSegment.IsAllZero(decoded))
        {
            encoded = new byte[0];
        }

        state.Definition.SetRawTile(tileIndex, encoded);
    }

    internal static byte[] EncodeSegment(TiffImageInfo info, bool isTile, int segmentIndex, byte[] decoded, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        TiffCodecContext context = TiffCodecContext.Create(info, isTile, (ulong)segmentIndex, (ulong)decoded.Length, limits);
        context.ByteOrder = byteOrder;
        TiffCodecResult result = TiffCodec.Encode(context, decoded, compressionCodec);
        diagnostics.Merge(result.Diagnostics);
        if (!result.Success)
        {
            return new byte[0];
        }

        return result.Bytes;
    }

    internal static bool CopyStripRegion(TiffImageInfo info, int stripIndex, TiffRegion region, int selectedSample, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, byte[] decoded)
    {
        ulong stripsPerPlane = TiffStrileLayout.DivideRoundUp(info.Length, info.RowsPerStrip);
        ulong stripIndexInPlane = (ulong)stripIndex % stripsPerPlane;
        int storageSample = -1;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            storageSample = (int)((ulong)stripIndex / stripsPerPlane);
            if (selectedSample >= 0 && storageSample != selectedSample)
            {
                return false;
            }
        }

        ulong firstRow = stripIndexInPlane * info.RowsPerStrip;
        ulong lastRow = firstRow + info.RowsPerStrip;
        if (lastRow > info.Length)
        {
            lastRow = info.Length;
        }

        ulong copyFirstRow = Maximum(firstRow, region.Row);
        ulong copyLastRow = Minimum(lastRow, region.Row + region.Length);
        if (copyFirstRow >= copyLastRow)
        {
            return false;
        }

        CopyRows(info, region, selectedSample, storageSample, copyFirstRow, copyLastRow, firstRow, 0, info.Width, region.Column, region.Column + region.Width, source, sampleByteCounts, bytesPerPixel, decoded);
        return true;
    }

    internal static bool CopyTileRegion(TiffImageInfo info, int tileIndex, TiffRegion region, int selectedSample, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, byte[] decoded, TiffDiagnostics diagnostics)
    {
        ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        ulong tileIndexInPlane = (ulong)tileIndex % tilesPerPlane;
        int storageSample = -1;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            storageSample = (int)((ulong)tileIndex / tilesPerPlane);
            if (selectedSample >= 0 && storageSample != selectedSample)
            {
                return false;
            }
        }

        ulong tileColumn = tileIndexInPlane % tilesAcross;
        ulong tileRow = tileIndexInPlane / tilesAcross;
        ulong firstColumn = tileColumn * info.TileWidth;
        ulong firstRow = tileRow * info.TileLength;
        ulong lastColumn = Minimum(firstColumn + info.TileWidth, info.Width);
        ulong lastRow = Minimum(firstRow + info.TileLength, info.Length);
        ulong copyFirstColumn = Maximum(firstColumn, region.Column);
        ulong copyLastColumn = Minimum(lastColumn, region.Column + region.Width);
        ulong copyFirstRow = Maximum(firstRow, region.Row);
        ulong copyLastRow = Minimum(lastRow, region.Row + region.Length);
        if (copyFirstColumn >= copyLastColumn || copyFirstRow >= copyLastRow)
        {
            return false;
        }

        CopyRows(info, region, selectedSample, storageSample, copyFirstRow, copyLastRow, firstRow, firstColumn, info.TileWidth, copyFirstColumn, copyLastColumn, source, sampleByteCounts, bytesPerPixel, decoded);
        return true;
    }

    internal static void CopyRows(TiffImageInfo info, TiffRegion region, int selectedSample, int storageSample, ulong copyFirstRow, ulong copyLastRow, ulong segmentFirstRow, ulong segmentFirstColumn, ulong segmentWidth, ulong copyFirstColumn, ulong copyLastColumn, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, byte[] decoded)
    {
        int destinationPixelByteCount = bytesPerPixel;
        int destinationChannelOffset = 0;
        if (storageSample >= 0)
        {
            destinationPixelByteCount = sampleByteCounts[storageSample];
        }
        else if (selectedSample >= 0)
        {
            destinationChannelOffset = TiffSourceRasterWriter.CalculateChannelOffset(sampleByteCounts, selectedSample);
        }

        int sourcePixelByteCount = bytesPerPixel;
        if (selectedSample >= 0)
        {
            sourcePixelByteCount = sampleByteCounts[selectedSample];
        }

        ulong row = copyFirstRow;
        while (row < copyLastRow)
        {
            ulong column = copyFirstColumn;
            while (column < copyLastColumn)
            {
                ulong sourcePixelIndex = (row - region.Row) * region.Width + column - region.Column;
                ulong destinationPixelIndex = (row - segmentFirstRow) * segmentWidth + column - segmentFirstColumn;

                int sourceOffset = (int)(sourcePixelIndex * (ulong)sourcePixelByteCount);
                int destinationOffset = (int)(destinationPixelIndex * (ulong)destinationPixelByteCount) + destinationChannelOffset;
                if (storageSample >= 0 && selectedSample < 0)
                {
                    sourceOffset = (int)(sourcePixelIndex * (ulong)bytesPerPixel) + TiffSourceRasterWriter.CalculateChannelOffset(sampleByteCounts, storageSample);
                }

                int copyByteCount = destinationPixelByteCount;
                if (storageSample < 0 && selectedSample < 0)
                {
                    copyByteCount = bytesPerPixel;
                }
                else if (storageSample < 0)
                {
                    copyByteCount = sampleByteCounts[selectedSample];
                }

                CopyBytes(source, sourceOffset, decoded, destinationOffset, copyByteCount);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void CopyBytes(ReadOnlySpan<byte> source, int sourceOffset, byte[] destination, int destinationOffset, int byteCount)
    {
        int index = 0;
        while (index < byteCount)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index = index + 1;
        }
    }

    internal static ulong Minimum(ulong left, ulong right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
    }

    internal static ulong Maximum(ulong left, ulong right)
    {
        if (left > right)
        {
            return left;
        }

        return right;
    }
}
