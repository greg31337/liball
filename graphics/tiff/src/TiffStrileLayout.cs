namespace tiff;

internal class TiffStrileLayout
{
    internal static ulong CalculateStripsPerPlane(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateStripInfo(info, diagnostics))
        {
            return 0;
        }

        return DivideRoundUp(info.Length, info.RowsPerStrip);
    }

    internal static ulong CalculateStripCount(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
        if (stripsPerPlane == 0)
        {
            return 0;
        }

        ulong planeCount = 1;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            planeCount = (ulong)info.SamplesPerPixel;
        }

        if (planeCount != 0 && stripsPerPlane > ulong.MaxValue / planeCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip count overflows the supported range.");
            return 0;
        }

        return stripsPerPlane * planeCount;
    }

    internal static ulong CalculateRowsInStrip(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
        ulong stripCount = CalculateStripCount(info, diagnostics);
        if (stripCount == 0)
        {
            return 0;
        }

        if (stripIndex >= stripCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return 0;
        }

        ulong planeStripIndex = stripIndex % stripsPerPlane;
        ulong firstRow = planeStripIndex * info.RowsPerStrip;
        ulong remainingRows = info.Length - firstRow;
        if (remainingRows < info.RowsPerStrip)
        {
            return remainingRows;
        }

        return info.RowsPerStrip;
    }

    internal static ulong CalculateStripByteCount(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong rows = CalculateRowsInStrip(info, stripIndex, diagnostics);
        if (rows == 0)
        {
            return 0;
        }

        ulong bitsPerRow = CalculateStripBitsPerRow(info, stripIndex, diagnostics);
        if (bitsPerRow == 0)
        {
            return 0;
        }

        return CalculateByteCountForRows(bitsPerRow, rows, "Strip byte count overflows the supported range.", diagnostics);
    }

    internal static ulong CalculateStripBitsPerRow(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateStripInfo(info, diagnostics))
        {
            return 0;
        }

        ulong stripCount = CalculateStripCount(info, diagnostics);
        if (stripCount == 0)
        {
            return 0;
        }

        if (stripIndex >= stripCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return 0;
        }

        ulong bitsPerPixel = 0;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
            ulong sampleIndexValue = stripIndex / stripsPerPlane;
            if (sampleIndexValue > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip plane index exceeds the supported range.");
                return 0;
            }

            int sampleIndex = (int)sampleIndexValue;
            if (sampleIndex >= info.BitsPerSample.Length)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain a value for the requested plane.");
                return 0;
            }

            bitsPerPixel = (ulong)info.BitsPerSample[sampleIndex];
        }
        else
        {
            bitsPerPixel = SumBitsPerPixel(info.BitsPerSample, diagnostics);
        }

        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (info.Width > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip row bit count overflows the supported range.");
            return 0;
        }

        return info.Width * bitsPerPixel;
    }

    internal static TiffByteRange[] CalculateStripByteRanges(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, TiffDiagnostics diagnostics)
    {
        ulong stripCount = CalculateStripCount(info, diagnostics);
        if (stripCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip count does not fit in a supported array length.");
            return new TiffByteRange[0];
        }

        int count = (int)stripCount;
        if (stripOffsets == null || stripOffsets.Length != count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "StripOffsets count does not match the calculated strip count.", TiffDiagnostic.NoImageIndex, TiffTagId.StripOffsets);
            return new TiffByteRange[0];
        }

        if (stripByteCounts == null || stripByteCounts.Length != count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "StripByteCounts count does not match the calculated strip count.", TiffDiagnostic.NoImageIndex, TiffTagId.StripByteCounts);
            return new TiffByteRange[0];
        }

        TiffByteRange[] ranges = new TiffByteRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = CreateByteRange(stripOffsets[index], stripByteCounts[index], diagnostics);
            index = index + 1;
        }

        return ranges;
    }

    internal static ulong CalculateTilesAcross(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateTileInfo(info, diagnostics))
        {
            return 0;
        }

        return DivideRoundUp(info.Width, info.TileWidth);
    }

    internal static ulong CalculateTilesDown(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateTileInfo(info, diagnostics))
        {
            return 0;
        }

        return DivideRoundUp(info.Length, info.TileLength);
    }

    internal static ulong CalculateTilesDeep(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateTileInfo(info, diagnostics))
        {
            return 0;
        }

        return DivideRoundUp(info.ImageDepth, info.TileDepth);
    }

    internal static ulong CalculateTilesPerPlane(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        ulong tilesAcross = CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = CalculateTilesDown(info, diagnostics);
        ulong tilesDeep = CalculateTilesDeep(info, diagnostics);
        if (tilesAcross == 0 || tilesDown == 0 || tilesDeep == 0)
        {
            return 0;
        }

        if (tilesAcross > ulong.MaxValue / tilesDown)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile count per plane overflows the supported range.");
            return 0;
        }

        ulong tilesPerDepth = tilesAcross * tilesDown;
        if (tilesPerDepth > ulong.MaxValue / tilesDeep)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Volumetric tile count per plane overflows the supported range.");
            return 0;
        }

        return tilesPerDepth * tilesDeep;
    }

    internal static ulong CalculateTileCount(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        ulong tilesPerPlane = CalculateTilesPerPlane(info, diagnostics);
        if (tilesPerPlane == 0)
        {
            return 0;
        }

        ulong planeCount = 1;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            planeCount = (ulong)info.SamplesPerPixel;
        }

        if (planeCount != 0 && tilesPerPlane > ulong.MaxValue / planeCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile count overflows the supported range.");
            return 0;
        }

        return tilesPerPlane * planeCount;
    }

    internal static ulong CalculateTileWidthInImage(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilePlaneIndex = CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        if (tilePlaneIndex == ulong.MaxValue)
        {
            return 0;
        }

        ulong tilesAcross = CalculateTilesAcross(info, diagnostics);
        ulong tileColumn = tilePlaneIndex % tilesAcross;
        ulong firstColumn = tileColumn * info.TileWidth;
        ulong remainingWidth = info.Width - firstColumn;
        if (remainingWidth < info.TileWidth)
        {
            return remainingWidth;
        }

        return info.TileWidth;
    }

    internal static ulong CalculateTileLengthInImage(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilePlaneIndex = CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        if (tilePlaneIndex == ulong.MaxValue)
        {
            return 0;
        }

        ulong tilesAcross = CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = CalculateTilesDown(info, diagnostics);
        ulong tileRow = (tilePlaneIndex / tilesAcross) % tilesDown;
        ulong firstRow = tileRow * info.TileLength;
        ulong remainingLength = info.Length - firstRow;
        if (remainingLength < info.TileLength)
        {
            return remainingLength;
        }

        return info.TileLength;
    }

    internal static ulong CalculateTileDepthInImage(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilePlaneIndex = CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        if (tilePlaneIndex == ulong.MaxValue)
        {
            return 0;
        }

        ulong tilesAcross = CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = CalculateTilesDown(info, diagnostics);
        if (tilesAcross == 0 || tilesDown == 0 || tilesAcross > ulong.MaxValue / tilesDown)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile depth-plane size overflows the supported range.");
            return 0;
        }

        ulong tilesPerDepth = tilesAcross * tilesDown;
        ulong tileDepthIndex = tilePlaneIndex / tilesPerDepth;
        ulong firstDepth = tileDepthIndex * info.TileDepth;
        ulong remainingDepth = info.ImageDepth - firstDepth;
        if (remainingDepth < info.TileDepth)
        {
            return remainingDepth;
        }

        return info.TileDepth;
    }

    internal static ulong CalculateTileStorageByteCount(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong bitsPerRow = CalculateTileBitsPerStorageRow(info, tileIndex, diagnostics);
        if (bitsPerRow == 0)
        {
            return 0;
        }

        ulong planeByteCount = CalculateByteCountForRows(bitsPerRow, info.TileLength, "Tile storage byte count overflows the supported range.", diagnostics);
        return MultiplyLayoutValue(planeByteCount, info.TileDepth, "Volumetric tile storage byte count overflows the supported range.", diagnostics);
    }

    internal static ulong CalculateTileImageByteCount(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tileLength = CalculateTileLengthInImage(info, tileIndex, diagnostics);
        ulong tileDepth = CalculateTileDepthInImage(info, tileIndex, diagnostics);
        if (tileLength == 0 || tileDepth == 0)
        {
            return 0;
        }

        ulong bitsPerRow = CalculateTileBitsPerImageRow(info, tileIndex, diagnostics);
        if (bitsPerRow == 0)
        {
            return 0;
        }

        ulong planeByteCount = CalculateByteCountForRows(bitsPerRow, tileLength, "Tile image byte count overflows the supported range.", diagnostics);
        return MultiplyLayoutValue(planeByteCount, tileDepth, "Volumetric tile image byte count overflows the supported range.", diagnostics);
    }

    internal static ulong CalculateTileBitsPerStorageRow(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong bitsPerPixel = CalculateTileBitsPerPixel(info, tileIndex, diagnostics);
        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (info.TileWidth > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile storage row bit count overflows the supported range.");
            return 0;
        }

        return info.TileWidth * bitsPerPixel;
    }

    internal static ulong CalculateTileBitsPerImageRow(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tileWidth = CalculateTileWidthInImage(info, tileIndex, diagnostics);
        if (tileWidth == 0)
        {
            return 0;
        }

        ulong bitsPerPixel = CalculateTileBitsPerPixel(info, tileIndex, diagnostics);
        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (tileWidth > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile image row bit count overflows the supported range.");
            return 0;
        }

        return tileWidth * bitsPerPixel;
    }

    internal static TiffByteRange[] CalculateTileByteRanges(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, TiffDiagnostics diagnostics)
    {
        ulong tileCount = CalculateTileCount(info, diagnostics);
        if (tileCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile count does not fit in a supported array length.");
            return new TiffByteRange[0];
        }

        int count = (int)tileCount;
        if (tileOffsets == null || tileOffsets.Length != count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "TileOffsets count does not match the calculated tile count.", TiffDiagnostic.NoImageIndex, TiffTagId.TileOffsets);
            return new TiffByteRange[0];
        }

        if (tileByteCounts == null || tileByteCounts.Length != count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "TileByteCounts count does not match the calculated tile count.", TiffDiagnostic.NoImageIndex, TiffTagId.TileByteCounts);
            return new TiffByteRange[0];
        }

        TiffByteRange[] ranges = new TiffByteRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = CreateByteRange(tileOffsets[index], tileByteCounts[index], diagnostics);
            index = index + 1;
        }

        return ranges;
    }

    internal static ulong CalculateSampleBufferIndex(TiffImageInfo info, ulong column, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for sample index calculations.");
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return CalculateSeparateSampleBufferIndex(info, column, row, sampleIndex, diagnostics);
        }

        return CalculateContiguousSampleBufferIndex(info, column, row, sampleIndex, diagnostics);
    }

    internal static ulong CalculateContiguousSampleBufferIndex(TiffImageInfo info, ulong column, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateSamplePosition(info, column, row, sampleIndex, diagnostics))
        {
            return 0;
        }

        ulong pixelIndex = CalculatePixelIndex(info, column, row, diagnostics);
        if (pixelIndex > ulong.MaxValue / (ulong)info.SamplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Contiguous sample buffer index overflows the supported range.");
            return 0;
        }

        ulong firstSample = pixelIndex * (ulong)info.SamplesPerPixel;
        if (firstSample > ulong.MaxValue - (ulong)sampleIndex)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Contiguous sample buffer index overflows the supported range.");
            return 0;
        }

        return firstSample + (ulong)sampleIndex;
    }

    internal static ulong CalculateSeparateSampleBufferIndex(TiffImageInfo info, ulong column, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateSamplePosition(info, column, row, sampleIndex, diagnostics))
        {
            return 0;
        }

        ulong planeSize = CalculatePixelCount(info, diagnostics);
        if (planeSize == 0)
        {
            return 0;
        }

        if (sampleIndex != 0 && planeSize > ulong.MaxValue / (ulong)sampleIndex)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate sample plane offset overflows the supported range.");
            return 0;
        }

        ulong planeOffset = planeSize * (ulong)sampleIndex;
        ulong pixelIndex = CalculatePixelIndex(info, column, row, diagnostics);
        if (planeOffset > ulong.MaxValue - pixelIndex)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate sample buffer index overflows the supported range.");
            return 0;
        }

        return planeOffset + pixelIndex;
    }

    internal static ulong CalculateStripIndexForRowAndSample(TiffImageInfo info, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateStripInfo(info, diagnostics))
        {
            return 0;
        }

        if (!ValidateRowAndSample(info, row, sampleIndex, diagnostics))
        {
            return 0;
        }

        ulong stripIndexInPlane = row / info.RowsPerStrip;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
            if (sampleIndex != 0 && stripsPerPlane > ulong.MaxValue / (ulong)sampleIndex)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate strip plane offset overflows the supported range.");
                return 0;
            }

            ulong planeOffset = stripsPerPlane * (ulong)sampleIndex;
            if (planeOffset > ulong.MaxValue - stripIndexInPlane)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate strip index overflows the supported range.");
                return 0;
            }

            return planeOffset + stripIndexInPlane;
        }

        return stripIndexInPlane;
    }

    internal static ulong CalculateTileIndexForCoordinateAndSample(TiffImageInfo info, ulong column, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        return CalculateTileIndexForCoordinateDepthAndSample(info, column, row, 0, sampleIndex, diagnostics);
    }

    internal static ulong CalculateTileIndexForCoordinateDepthAndSample(TiffImageInfo info, ulong column, ulong row, ulong depth, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateTileInfo(info, diagnostics))
        {
            return 0;
        }

        if (!ValidateSamplePosition(info, column, row, sampleIndex, diagnostics))
        {
            return 0;
        }

        if (depth >= info.ImageDepth)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Depth coordinate is outside the image depth.");
            return 0;
        }

        ulong tilesAcross = CalculateTilesAcross(info, diagnostics);
        ulong tileColumn = column / info.TileWidth;
        ulong tileRow = row / info.TileLength;
        ulong tileDepthIndex = depth / info.TileDepth;
        if (tileRow > ulong.MaxValue / tilesAcross)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile row offset overflows the supported range.");
            return 0;
        }

        ulong tileIndexInPlane = tileRow * tilesAcross;
        if (tileIndexInPlane > ulong.MaxValue - tileColumn)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile index in plane overflows the supported range.");
            return 0;
        }

        tileIndexInPlane = tileIndexInPlane + tileColumn;
        ulong tilesDown = CalculateTilesDown(info, diagnostics);
        if (tilesAcross > ulong.MaxValue / tilesDown)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile depth-plane size overflows the supported range.");
            return 0;
        }

        ulong tilesPerDepth = tilesAcross * tilesDown;
        if (tileDepthIndex != 0 && tilesPerDepth > ulong.MaxValue / tileDepthIndex)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile depth offset overflows the supported range.");
            return 0;
        }

        ulong depthOffset = tilesPerDepth * tileDepthIndex;
        if (depthOffset > ulong.MaxValue - tileIndexInPlane)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Volumetric tile index overflows the supported range.");
            return 0;
        }

        tileIndexInPlane = depthOffset + tileIndexInPlane;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong tilesPerPlane = CalculateTilesPerPlane(info, diagnostics);
            if (sampleIndex != 0 && tilesPerPlane > ulong.MaxValue / (ulong)sampleIndex)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate tile plane offset overflows the supported range.");
                return 0;
            }

            ulong planeOffset = tilesPerPlane * (ulong)sampleIndex;
            if (planeOffset > ulong.MaxValue - tileIndexInPlane)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Separate tile index overflows the supported range.");
                return 0;
            }

            return planeOffset + tileIndexInPlane;
        }

        return tileIndexInPlane;
    }

    internal static int CalculateStripSampleIndex(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong stripCount = CalculateStripCount(info, diagnostics);
        if (stripCount == 0)
        {
            return 0;
        }

        if (stripIndex >= stripCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
            ulong sampleIndex = stripIndex / stripsPerPlane;
            if (sampleIndex > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip sample index exceeds the supported range.");
                return 0;
            }

            return (int)sampleIndex;
        }

        return 0;
    }

    internal static ulong CalculateStripIndexInPlane(TiffImageInfo info, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong stripsPerPlane = CalculateStripsPerPlane(info, diagnostics);
        ulong stripCount = CalculateStripCount(info, diagnostics);
        if (stripsPerPlane == 0 || stripCount == 0)
        {
            return ulong.MaxValue;
        }

        if (stripIndex >= stripCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return ulong.MaxValue;
        }

        return stripIndex % stripsPerPlane;
    }

    internal static int CalculateTileSampleIndex(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tileCount = CalculateTileCount(info, diagnostics);
        if (tileCount == 0)
        {
            return 0;
        }

        if (tileIndex >= tileCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong tilesPerPlane = CalculateTilesPerPlane(info, diagnostics);
            ulong sampleIndex = tileIndex / tilesPerPlane;
            if (sampleIndex > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile sample index exceeds the supported range.");
                return 0;
            }

            return (int)sampleIndex;
        }

        return 0;
    }

    internal static TiffByteRange CreateByteRange(ulong offset, ulong byteCount, TiffDiagnostics diagnostics)
    {
        bool sparse = offset == 0 || byteCount == 0;
        if (offset > ulong.MaxValue - byteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Strip byte range end offset overflows the supported range.");
            return TiffByteRange.Create(offset, byteCount, 0, sparse);
        }

        return TiffByteRange.Create(offset, byteCount, offset + byteCount, sparse);
    }

    internal static bool ValidateStripInfo(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for strip layout calculations.");
            return false;
        }

        if (info.Width == 0 || info.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image dimensions must be positive for strip layout calculations.");
            return false;
        }

        if (info.RowsPerStrip == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "RowsPerStrip must be positive.", info.ImageIndex, TiffTagId.RowsPerStrip);
            return false;
        }

        if (info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain at least one value.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        return true;
    }

    internal static bool ValidateSampleInfo(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for sample index calculations.");
            return false;
        }

        if (info.Width == 0 || info.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image dimensions must be positive for sample index calculations.");
            return false;
        }

        if (info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        return true;
    }

    internal static bool ValidateSamplePosition(TiffImageInfo info, ulong column, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateSampleInfo(info, diagnostics))
        {
            return false;
        }

        if (column >= info.Width)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Column index is outside the image width.");
            return false;
        }

        return ValidateRowAndSample(info, row, sampleIndex, diagnostics);
    }

    internal static bool ValidateRowAndSample(TiffImageInfo info, ulong row, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateSampleInfo(info, diagnostics))
        {
            return false;
        }

        if (row >= info.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Row index is outside the image length.");
            return false;
        }

        if (sampleIndex < 0 || sampleIndex >= info.SamplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample index is outside the SamplesPerPixel range.");
            return false;
        }

        return true;
    }

    internal static bool ValidateTileInfo(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for tile layout calculations.");
            return false;
        }

        if (info.Width == 0 || info.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image dimensions must be positive for tile layout calculations.");
            return false;
        }

        if (info.TileWidth == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "TileWidth must be positive.", info.ImageIndex, TiffTagId.TileWidth);
            return false;
        }

        if (info.TileLength == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "TileLength must be positive.", info.ImageIndex, TiffTagId.TileLength);
            return false;
        }

        if (info.ImageDepth == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageDepth must be positive.", info.ImageIndex, TiffTagId.ImageDepth);
            return false;
        }

        if (info.TileDepth == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "TileDepth must be positive.", info.ImageIndex, TiffTagId.TileDepth);
            return false;
        }

        if (info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain at least one value.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        return true;
    }

    internal static ulong CalculatePixelCount(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateSampleInfo(info, diagnostics))
        {
            return 0;
        }

        if (info.Width > ulong.MaxValue / info.Length)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Image pixel count overflows the supported range.");
            return 0;
        }

        return info.Width * info.Length;
    }

    internal static ulong CalculatePixelIndex(TiffImageInfo info, ulong column, ulong row, TiffDiagnostics diagnostics)
    {
        if (!ValidateSamplePosition(info, column, row, 0, diagnostics))
        {
            return 0;
        }

        if (row > ulong.MaxValue / info.Width)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Pixel row offset overflows the supported range.");
            return 0;
        }

        ulong rowOffset = row * info.Width;
        if (rowOffset > ulong.MaxValue - column)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Pixel index overflows the supported range.");
            return 0;
        }

        return rowOffset + column;
    }

    internal static ulong CalculateTilePlaneIndex(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tilesPerPlane = CalculateTilesPerPlane(info, diagnostics);
        ulong tileCount = CalculateTileCount(info, diagnostics);
        if (tilesPerPlane == 0 || tileCount == 0)
        {
            return ulong.MaxValue;
        }

        if (tileIndex >= tileCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return ulong.MaxValue;
        }

        return tileIndex % tilesPerPlane;
    }

    internal static ulong CalculateTileBitsPerPixel(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        if (!ValidateTileInfo(info, diagnostics))
        {
            return 0;
        }

        ulong tileCount = CalculateTileCount(info, diagnostics);
        if (tileCount == 0)
        {
            return 0;
        }

        if (tileIndex >= tileCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong tilesPerPlane = CalculateTilesPerPlane(info, diagnostics);
            ulong sampleIndexValue = tileIndex / tilesPerPlane;
            if (sampleIndexValue > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Tile plane index exceeds the supported range.");
                return 0;
            }

            int sampleIndex = (int)sampleIndexValue;
            if (sampleIndex >= info.BitsPerSample.Length)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain a value for the requested tile plane.");
                return 0;
            }

            return (ulong)info.BitsPerSample[sampleIndex];
        }

        return SumBitsPerPixel(info.BitsPerSample, diagnostics);
    }

    internal static ulong SumBitsPerPixel(int[] bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (bitsPerSample == null || bitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain at least one value.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        ulong sum = 0;
        int index = 0;
        while (index < bitsPerSample.Length)
        {
            if (bitsPerSample[index] <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be positive.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            ulong value = (ulong)bitsPerSample[index];
            if (sum > ulong.MaxValue - value)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "BitsPerSample sum overflows the supported range.");
                return 0;
            }

            sum = sum + value;
            index = index + 1;
        }

        return sum;
    }

    internal static ulong DivideRoundUp(ulong value, ulong divisor)
    {
        if (divisor == 0)
        {
            return 0;
        }

        ulong result = value / divisor;
        if (value % divisor != 0)
        {
            result = result + 1;
        }

        return result;
    }

    internal static ulong CalculateByteCountForRows(ulong bitsPerRow, ulong rowCount, string overflowMessage, TiffDiagnostics diagnostics)
    {
        if (rowCount == 0)
        {
            return 0;
        }

        ulong bytesPerRow = DivideRoundUp(bitsPerRow, 8);
        if (bytesPerRow > ulong.MaxValue / rowCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, overflowMessage);
            return 0;
        }

        return bytesPerRow * rowCount;
    }

    internal static ulong MultiplyLayoutValue(ulong first, ulong second, string overflowMessage, TiffDiagnostics diagnostics)
    {
        if (first != 0 && second > ulong.MaxValue / first)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, overflowMessage);
            return 0;
        }

        return first * second;
    }
}
