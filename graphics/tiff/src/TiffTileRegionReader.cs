using System;

namespace tiff;

internal class TiffTileRegionReader
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
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a tile region.");
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, TiffCompression.Unknown, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? tileOffsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        if (tileOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileOffsets is required to read a tile region.", directory.ImageIndex, TiffTagId.TileOffsets);
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? tileByteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (tileByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileByteCounts is required to read a tile region.", directory.ImageIndex, TiffTagId.TileByteCounts);
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        ulong[] tileOffsets = TiffImageInfoExtractor.ReadUnsignedArray(tileOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] tileByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(tileByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        return ReadRegion(infoResult.Info, tileOffsets, tileByteCounts, fileBytes, column, row, width, length, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length)
    {
        return ReadRegion(info, tileOffsets, tileByteCounts, fileBytes, column, row, width, length, 0, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex)
    {
        return ReadRegion(info, tileOffsets, tileByteCounts, fileBytes, column, row, width, length, sampleIndex, new TiffLimits());
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadRegion(info, tileOffsets, tileByteCounts, fileBytes, column, row, width, length, sampleIndex, actualLimits, diagnostics);
    }

    internal static TiffRegionReadResult ReadRegion(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!ValidateRegion(info, column, row, width, length, sampleIndex, diagnostics))
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, 0, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(info, sampleIndex, diagnostics);
        ulong rowByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(width, bytesPerPixel, diagnostics);
        ulong totalByteCount = TiffStripRegionReader.CalculateRegionTotalByteCount(rowByteCount, length, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        byte[] bytes = new byte[(int)totalByteCount];
        CopyRegionRows(info, tileOffsets, tileByteCounts, fileBytes, column, row, width, length, sampleIndex, bytesPerPixel, rowByteCount, bytes, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        return TiffRegionReadResult.Create(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, GetCompression(info), bytes, diagnostics);
    }

    internal static void CopyRegionRows(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, ulong length, int sampleIndex, ulong bytesPerPixel, ulong rowByteCount, byte[] destination, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong rowIndex = 0;
        while (rowIndex < length)
        {
            CopyRegionRowTiles(info, tileOffsets, tileByteCounts, fileBytes, column, row + rowIndex, width, sampleIndex, bytesPerPixel, rowIndex * rowByteCount, destination, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void CopyRegionRowTiles(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong column, ulong row, ulong width, int sampleIndex, ulong bytesPerPixel, ulong rowDestinationOffset, byte[] destination, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong currentColumn = column;
        ulong remainingWidth = width;
        ulong destinationOffset = rowDestinationOffset;
        while (remainingWidth > 0)
        {
            ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateAndSample(info, currentColumn, row, sampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            TiffDecodedTileReadResult tileResult = TiffDecodedTileReader.ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            ulong columnInTile = CalculateColumnInTile(info, currentColumn, tileIndex, diagnostics);
            ulong rowInTile = CalculateRowInTile(info, row, tileIndex, diagnostics);
            ulong copyPixelCount = CalculateCopyPixelCount(info, columnInTile, remainingWidth, diagnostics);
            ulong sourceOffset = CalculateTileSourceOffset(info, columnInTile, rowInTile, bytesPerPixel, diagnostics);
            ulong copyByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(copyPixelCount, bytesPerPixel, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            TiffStripRegionReader.CopyRegionRow(tileResult.Bytes, sourceOffset, copyByteCount, destination, destinationOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            currentColumn = currentColumn + copyPixelCount;
            remainingWidth = remainingWidth - copyPixelCount;
            destinationOffset = destinationOffset + copyByteCount;
        }
    }

    internal static bool ValidateRegion(TiffImageInfo info, ulong column, ulong row, ulong width, ulong length, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateTiledImage(info, diagnostics))
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

    internal static bool ValidateTiledImage(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!TiffStrileLayout.ValidateTileInfo(info, diagnostics))
        {
            return false;
        }

        if (!info.IsTiled)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Tile region reading requires tiled image data.");
            return false;
        }

        return true;
    }

    internal static ulong CalculateColumnInTile(TiffImageInfo info, ulong column, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilePlaneIndex = TiffStrileLayout.CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        ulong tileColumn = tilePlaneIndex % tilesAcross;
        if (tileColumn > ulong.MaxValue / info.TileWidth)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile column offset overflows the supported range.");
            return 0;
        }

        ulong firstColumn = tileColumn * info.TileWidth;
        if (column < firstColumn)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region column is outside the decoded tile.");
            return 0;
        }

        return column - firstColumn;
    }

    internal static ulong CalculateRowInTile(TiffImageInfo info, ulong row, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilePlaneIndex = TiffStrileLayout.CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        ulong tileRow = tilePlaneIndex / tilesAcross;
        if (tileRow > ulong.MaxValue / info.TileLength)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile row offset overflows the supported range.");
            return 0;
        }

        ulong firstRow = tileRow * info.TileLength;
        if (row < firstRow)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region row is outside the decoded tile.");
            return 0;
        }

        return row - firstRow;
    }

    internal static ulong CalculateCopyPixelCount(TiffImageInfo info, ulong columnInTile, ulong remainingWidth, TiffDiagnostics diagnostics)
    {
        if (columnInTile >= info.TileWidth)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region column is outside the tile width.");
            return 0;
        }

        ulong availableWidth = info.TileWidth - columnInTile;
        if (remainingWidth < availableWidth)
        {
            return remainingWidth;
        }

        return availableWidth;
    }

    internal static ulong CalculateTileSourceOffset(TiffImageInfo info, ulong columnInTile, ulong rowInTile, ulong bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if (rowInTile >= info.TileLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Region row is outside the tile length.");
            return 0;
        }

        if (rowInTile > ulong.MaxValue / info.TileWidth)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile row byte offset overflows the supported range.");
            return 0;
        }

        ulong pixelOffset = rowInTile * info.TileWidth;
        if (pixelOffset > ulong.MaxValue - columnInTile)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile pixel offset overflows the supported range.");
            return 0;
        }

        pixelOffset = pixelOffset + columnInTile;
        if (bytesPerPixel != 0 && pixelOffset > ulong.MaxValue / bytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile source byte offset overflows the supported range.");
            return 0;
        }

        return pixelOffset * bytesPerPixel;
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
