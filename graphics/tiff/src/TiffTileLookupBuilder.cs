namespace tiff;

internal class TiffTileLookupBuilder
{
    internal static TiffTileLookupResult Lookup(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, int sampleIndex)
    {
        return LookupVolume(info, tileOffsets, tileByteCounts, column, row, 0, sampleIndex);
    }

    internal static TiffTileLookupResult LookupVolume(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, ulong depth, int sampleIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!TiffStrileLayout.ValidateTileInfo(info, diagnostics))
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateDepthAndSample(info, column, row, depth, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        return LookupByIndex(info, tileOffsets, tileByteCounts, tileIndex, 0, diagnostics);
    }

    internal static TiffTileLookupResult LookupByIndex(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong tileIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return LookupByIndex(info, tileOffsets, tileByteCounts, tileIndex, 0, diagnostics);
    }

    internal static TiffTileLookupResult LookupLevel(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, int levelIndex, ulong column, ulong row, int sampleIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateLevelArrays(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, diagnostics))
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffTileLookupResult result = Lookup(levelInfos[levelIndex], tileOffsetsByLevel[levelIndex], tileByteCountsByLevel[levelIndex], column, row, sampleIndex);
        diagnostics.Merge(result.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffTileLookup lookup = result.Lookup.Copy();
        lookup.PyramidLevelIndex = levelIndex;
        return TiffTileLookupResult.Create(lookup, diagnostics);
    }

    internal static TiffTileLookupResult LookupLevelFromBaseCoordinate(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, int levelIndex, ulong baseColumn, ulong baseRow, int sampleIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateLevelArrays(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, diagnostics))
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffImageInfo baseInfo = levelInfos[0];
        TiffImageInfo levelInfo = levelInfos[levelIndex];
        if (baseInfo.Width == 0 || baseInfo.Length == 0 || levelInfo.Width == 0 || levelInfo.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Level dimensions must be positive for base-coordinate tile lookup.");
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        if (baseColumn >= baseInfo.Width || baseRow >= baseInfo.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Base coordinate is outside the base image dimensions.");
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        ulong levelColumn = ScaleBaseCoordinate(baseColumn, baseInfo.Width, levelInfo.Width, diagnostics);
        ulong levelRow = ScaleBaseCoordinate(baseRow, baseInfo.Length, levelInfo.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        return LookupLevel(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, levelColumn, levelRow, sampleIndex);
    }

    internal static int FindBestLevel(TiffImageInfo[] levelInfos, ulong requestedWidth, ulong requestedLength, TiffDiagnostics diagnostics)
    {
        if (levelInfos == null || levelInfos.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "At least one level is required for tile level lookup.");
            return -1;
        }

        if (requestedWidth == 0 || requestedLength == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Requested level dimensions must be positive.");
            return -1;
        }

        int bestIndex = -1;
        bool hasCoveringLevel = false;
        int index = 0;
        while (index < levelInfos.Length)
        {
            if (levelInfos[index] != null && levelInfos[index].Width > 0 && levelInfos[index].Length > 0)
            {
                bool coversRequest = levelInfos[index].Width >= requestedWidth && levelInfos[index].Length >= requestedLength;
                if (coversRequest)
                {
                    if (!hasCoveringLevel || IsSmallerLevel(levelInfos[index], levelInfos[bestIndex]))
                    {
                        bestIndex = index;
                    }

                    hasCoveringLevel = true;
                }
                else if (!hasCoveringLevel && (bestIndex < 0 || IsLargerLevel(levelInfos[index], levelInfos[bestIndex])))
                {
                    bestIndex = index;
                }
            }

            index = index + 1;
        }

        if (bestIndex < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "No level has positive dimensions for tile level lookup.");
        }

        return bestIndex;
    }

    internal static TiffTileLookupResult LookupBestLevel(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, ulong requestedWidth, ulong requestedLength, ulong column, ulong row, int sampleIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int levelIndex = FindBestLevel(levelInfos, requestedWidth, requestedLength, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffTileLookupResult result = LookupLevel(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, column, row, sampleIndex);
        diagnostics.Merge(result.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        return TiffTileLookupResult.Create(result.Lookup, diagnostics);
    }

    internal static TiffTileLookupResult LookupByIndex(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong tileIndex, int pyramidLevelIndex, TiffDiagnostics diagnostics)
    {
        if (!TiffStrileLayout.ValidateTileInfo(info, diagnostics))
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        if (tileCount == 0 || tileIndex >= tileCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, tileOffsets, tileByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        ulong tileIndexInPlane = TiffStrileLayout.CalculateTilePlaneIndex(info, tileIndex, diagnostics);
        int sampleIndex = TiffStrileLayout.CalculateTileSampleIndex(info, tileIndex, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = TiffStrileLayout.CalculateTilesDown(info, diagnostics);
        ulong tilesPerDepth = tilesAcross * tilesDown;
        ulong tileIndexInDepth = tileIndexInPlane % tilesPerDepth;
        ulong tileColumn = tileIndexInDepth % tilesAcross;
        ulong tileRow = tileIndexInDepth / tilesAcross;
        ulong tileDepthIndex = tileIndexInPlane / tilesPerDepth;
        ulong firstColumn = Multiply(tileColumn, info.TileWidth, "Tile first column overflows the supported range.", diagnostics);
        ulong firstRow = Multiply(tileRow, info.TileLength, "Tile first row overflows the supported range.", diagnostics);
        ulong firstDepth = Multiply(tileDepthIndex, info.TileDepth, "Tile first depth overflows the supported range.", diagnostics);
        ulong width = TiffStrileLayout.CalculateTileWidthInImage(info, tileIndex, diagnostics);
        ulong length = TiffStrileLayout.CalculateTileLengthInImage(info, tileIndex, diagnostics);
        ulong depth = TiffStrileLayout.CalculateTileDepthInImage(info, tileIndex, diagnostics);
        ulong storageByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
        ulong imageByteCount = TiffStrileLayout.CalculateTileImageByteCount(info, tileIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffTileLookup lookup = TiffTileLookup.Create(info.ImageIndex, pyramidLevelIndex, tileIndex, tileIndexInPlane, tileColumn, tileRow, tileDepthIndex, sampleIndex, firstColumn, firstRow, firstDepth, width, length, depth, storageByteCount, imageByteCount, ranges[(int)tileIndex]);
        return TiffTileLookupResult.Create(lookup, diagnostics);
    }

    internal static bool ValidateLevelArrays(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, int levelIndex, TiffDiagnostics diagnostics)
    {
        if (levelInfos == null || tileOffsetsByLevel == null || tileByteCountsByLevel == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Level infos, tile offsets, and tile byte counts are required for level tile lookup.");
            return false;
        }

        if (levelInfos.Length != tileOffsetsByLevel.Length || levelInfos.Length != tileByteCountsByLevel.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "Tile level lookup arrays must have matching level counts.");
            return false;
        }

        if (levelIndex < 0 || levelIndex >= levelInfos.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Pyramid level index is outside the available level range.");
            return false;
        }

        return true;
    }

    internal static ulong ScaleBaseCoordinate(ulong baseCoordinate, ulong baseSize, ulong levelSize, TiffDiagnostics diagnostics)
    {
        if (baseSize == 0 || levelSize == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Coordinate scale dimensions must be positive.");
            return 0;
        }

        if (baseCoordinate > ulong.MaxValue / levelSize)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Base coordinate scaling overflows the supported range.");
            return 0;
        }

        ulong scaled = (baseCoordinate * levelSize) / baseSize;
        if (scaled >= levelSize)
        {
            return levelSize - 1;
        }

        return scaled;
    }

    internal static ulong Multiply(ulong left, ulong right, string message, TiffDiagnostics diagnostics)
    {
        if (left != 0 && right > ulong.MaxValue / left)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, message);
            return 0;
        }

        return left * right;
    }

    internal static bool IsSmallerLevel(TiffImageInfo candidate, TiffImageInfo current)
    {
        if (current == null)
        {
            return true;
        }

        if (candidate.Width < current.Width)
        {
            return true;
        }

        return candidate.Width == current.Width && candidate.Length < current.Length;
    }

    internal static bool IsLargerLevel(TiffImageInfo candidate, TiffImageInfo current)
    {
        if (current == null)
        {
            return true;
        }

        if (candidate.Width > current.Width)
        {
            return true;
        }

        return candidate.Width == current.Width && candidate.Length > current.Length;
    }
}
