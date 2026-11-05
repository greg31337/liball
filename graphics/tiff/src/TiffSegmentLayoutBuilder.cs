namespace tiff;

internal class TiffSegmentLayoutBuilder
{
    internal static TiffStripLayoutResult CreateStripLayout(TiffImageInfo info, ulong[] offsets, ulong[] storedByteCounts)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffStripLayout layout = new TiffStripLayout();
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to create a strip layout.");
            return TiffStripLayoutResult.Create(layout, diagnostics);
        }

        if (info.IsTiled)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "A tiled image does not have a strip layout.");
            return TiffStripLayoutResult.Create(layout, diagnostics);
        }

        ulong stripsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(info, diagnostics);
        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, offsets, storedByteCounts, diagnostics);
        if (diagnostics.HasErrors() || stripCount > int.MaxValue)
        {
            return TiffStripLayoutResult.Create(layout, diagnostics);
        }

        TiffStripSegment[] segments = new TiffStripSegment[(int)stripCount];
        int index = 0;
        while (index < segments.Length)
        {
            ulong stripIndex = (ulong)index;
            ulong indexInPlane = TiffStrileLayout.CalculateStripIndexInPlane(info, stripIndex, diagnostics);
            ulong rowCount = TiffStrileLayout.CalculateRowsInStrip(info, stripIndex, diagnostics);
            ulong decodedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
            TiffStripSegment segment = new TiffStripSegment();
            segment.StripIndex = stripIndex;
            segment.SourceStripIndex = stripIndex;
            segment.StripIndexInPlane = indexInPlane;
            segment.SampleIndex = TiffStrileLayout.CalculateStripSampleIndex(info, stripIndex, diagnostics);
            segment.FirstRow = indexInPlane * info.RowsPerStrip;
            segment.RowCount = rowCount;
            segment.Width = info.Width;
            segment.DecodedByteCount = decodedByteCount;
            segment.ImageByteCount = decodedByteCount;
            segment.StoredByteCount = ranges[index].ByteCount;
            segment.ByteRange = ranges[index].Copy();
            segment.IsPartial = rowCount < info.RowsPerStrip;
            segment.IsSparse = ranges[index].IsSparse;
            segment.IsChopped = false;
            segments[index] = segment;
            index = index + 1;
        }

        layout.ImageIndex = info.ImageIndex;
        layout.PlanarConfiguration = info.PlanarConfiguration;
        layout.StripsPerPlane = stripsPerPlane;
        layout.SourceStripCount = stripCount;
        layout.StripCount = stripCount;
        layout.Segments = segments;
        layout.IsDataContiguous = AreStripSegmentsContiguous(segments);
        return TiffStripLayoutResult.Create(layout, diagnostics);
    }

    internal static TiffStripLayoutResult CreateChoppedStripLayout(TiffImageInfo info, ulong[] offsets, ulong[] storedByteCounts, ulong targetDecodedByteCount)
    {
        return CreateChoppedStripLayout(info, offsets, storedByteCounts, targetDecodedByteCount, new TiffLimits());
    }

    internal static TiffStripLayoutResult CreateChoppedStripLayout(TiffImageInfo info, ulong[] offsets, ulong[] storedByteCounts, ulong targetDecodedByteCount, TiffLimits limits)
    {
        TiffStripLayoutResult sourceResult = CreateStripLayout(info, offsets, storedByteCounts);
        if (!sourceResult.Success)
        {
            return sourceResult;
        }

        if (targetDecodedByteCount == 0)
        {
            sourceResult.Diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Chopped-strip target byte count must be positive.");
            return TiffStripLayoutResult.Create(sourceResult.Layout, sourceResult.Diagnostics);
        }

        TiffStripLayout sourceLayout = sourceResult.Layout;
        if (info.Compression != TiffCompression.None || sourceLayout.StripsPerPlane != 1)
        {
            return sourceResult;
        }

        ulong largestDecodedByteCount = 0;
        ulong largestRowByteCount = 0;
        int sourceIndex = 0;
        while (sourceIndex < sourceLayout.Segments.Length)
        {
            TiffStripSegment sourceSegment = sourceLayout.Segments[sourceIndex];
            if (sourceSegment.DecodedByteCount > largestDecodedByteCount)
            {
                largestDecodedByteCount = sourceSegment.DecodedByteCount;
            }

            if (sourceSegment.RowCount != 0)
            {
                ulong rowByteCount = sourceSegment.DecodedByteCount / sourceSegment.RowCount;
                if (rowByteCount > largestRowByteCount)
                {
                    largestRowByteCount = rowByteCount;
                }
            }

            sourceIndex = sourceIndex + 1;
        }

        if (largestDecodedByteCount <= targetDecodedByteCount || largestRowByteCount == 0)
        {
            return sourceResult;
        }

        ulong rowsPerChoppedStrip = targetDecodedByteCount / largestRowByteCount;
        if (rowsPerChoppedStrip == 0)
        {
            rowsPerChoppedStrip = 1;
        }

        ulong stripsPerPlane = TiffStrileLayout.DivideRoundUp(info.Length, rowsPerChoppedStrip);
        ulong planeCount = 1;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            planeCount = (ulong)info.SamplesPerPixel;
        }

        if (stripsPerPlane > ulong.MaxValue / planeCount)
        {
            sourceResult.Diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Chopped strip count overflows the supported range.");
            return TiffStripLayoutResult.Create(sourceLayout, sourceResult.Diagnostics);
        }

        ulong stripCount = stripsPerPlane * planeCount;
        if (stripCount > int.MaxValue)
        {
            sourceResult.Diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Chopped strip count does not fit in a supported array length.");
            return TiffStripLayoutResult.Create(sourceLayout, sourceResult.Diagnostics);
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (stripCount > (ulong)(long.MaxValue / 8) || !actualLimits.AllowsAllocation((long)(stripCount * 8)))
        {
            sourceResult.Diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Chopped strip layout exceeds the configured allocation limit.");
            return TiffStripLayoutResult.Create(sourceLayout, sourceResult.Diagnostics);
        }

        TiffStripSegment[] segments = new TiffStripSegment[(int)stripCount];
        ulong planeIndex = 0;
        while (planeIndex < planeCount)
        {
            TiffStripSegment sourceSegment = sourceLayout.Segments[(int)planeIndex];
            ulong rowByteCount = sourceSegment.DecodedByteCount / sourceSegment.RowCount;
            ulong consumedByteCount = 0;
            ulong choppedIndexInPlane = 0;
            while (choppedIndexInPlane < stripsPerPlane)
            {
                ulong firstRow = choppedIndexInPlane * rowsPerChoppedStrip;
                ulong remainingRows = info.Length - firstRow;
                ulong rowCount = rowsPerChoppedStrip;
                if (remainingRows < rowCount)
                {
                    rowCount = remainingRows;
                }

                ulong decodedByteCount = TiffStrileLayout.MultiplyLayoutValue(rowByteCount, rowCount, "Chopped strip byte count overflows the supported range.", sourceResult.Diagnostics);
                ulong stripIndex = planeIndex * stripsPerPlane + choppedIndexInPlane;
                TiffByteRange range = CreateChoppedRange(sourceSegment, consumedByteCount, decodedByteCount, sourceResult.Diagnostics);
                TiffStripSegment segment = new TiffStripSegment();
                segment.StripIndex = stripIndex;
                segment.SourceStripIndex = sourceSegment.SourceStripIndex;
                segment.StripIndexInPlane = choppedIndexInPlane;
                segment.SampleIndex = sourceSegment.SampleIndex;
                segment.FirstRow = firstRow;
                segment.RowCount = rowCount;
                segment.Width = info.Width;
                segment.DecodedByteCount = decodedByteCount;
                segment.ImageByteCount = decodedByteCount;
                segment.StoredByteCount = range.ByteCount;
                segment.ByteRange = range;
                segment.IsPartial = rowCount < rowsPerChoppedStrip;
                segment.IsSparse = range.IsSparse;
                segment.IsChopped = true;
                segments[(int)stripIndex] = segment;
                consumedByteCount = consumedByteCount + decodedByteCount;
                choppedIndexInPlane = choppedIndexInPlane + 1;
            }

            planeIndex = planeIndex + 1;
        }

        if (sourceResult.Diagnostics.HasErrors())
        {
            return TiffStripLayoutResult.Create(sourceLayout, sourceResult.Diagnostics);
        }

        TiffStripLayout layout = new TiffStripLayout();
        layout.ImageIndex = info.ImageIndex;
        layout.PlanarConfiguration = info.PlanarConfiguration;
        layout.StripsPerPlane = stripsPerPlane;
        layout.SourceStripCount = sourceLayout.SourceStripCount;
        layout.StripCount = stripCount;
        layout.IsChopped = true;
        layout.Segments = segments;
        layout.IsDataContiguous = AreStripSegmentsContiguous(segments);
        return TiffStripLayoutResult.Create(layout, sourceResult.Diagnostics);
    }

    internal static TiffTileLayoutResult CreateTileLayout(TiffImageInfo info, ulong[] offsets, ulong[] storedByteCounts)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTileLayout layout = new TiffTileLayout();
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to create a tile layout.");
            return TiffTileLayoutResult.Create(layout, diagnostics);
        }

        if (!info.IsTiled)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "A stripped image does not have a tile layout.");
            return TiffTileLayoutResult.Create(layout, diagnostics);
        }

        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = TiffStrileLayout.CalculateTilesDown(info, diagnostics);
        ulong tilesDeep = TiffStrileLayout.CalculateTilesDeep(info, diagnostics);
        ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, offsets, storedByteCounts, diagnostics);
        if (diagnostics.HasErrors() || tileCount > int.MaxValue)
        {
            return TiffTileLayoutResult.Create(layout, diagnostics);
        }

        ulong tilesPerDepth = tilesAcross * tilesDown;
        TiffTileSegment[] segments = new TiffTileSegment[(int)tileCount];
        int index = 0;
        while (index < segments.Length)
        {
            ulong tileIndex = (ulong)index;
            ulong indexInPlane = TiffStrileLayout.CalculateTilePlaneIndex(info, tileIndex, diagnostics);
            ulong indexInDepth = indexInPlane % tilesPerDepth;
            ulong tileColumn = indexInDepth % tilesAcross;
            ulong tileRow = indexInDepth / tilesAcross;
            ulong tileDepthIndex = indexInPlane / tilesPerDepth;
            ulong width = TiffStrileLayout.CalculateTileWidthInImage(info, tileIndex, diagnostics);
            ulong length = TiffStrileLayout.CalculateTileLengthInImage(info, tileIndex, diagnostics);
            ulong depth = TiffStrileLayout.CalculateTileDepthInImage(info, tileIndex, diagnostics);
            TiffTileSegment segment = new TiffTileSegment();
            segment.TileIndex = tileIndex;
            segment.TileIndexInPlane = indexInPlane;
            segment.SampleIndex = TiffStrileLayout.CalculateTileSampleIndex(info, tileIndex, diagnostics);
            segment.TileColumn = tileColumn;
            segment.TileRow = tileRow;
            segment.TileDepthIndex = tileDepthIndex;
            segment.FirstColumn = tileColumn * info.TileWidth;
            segment.FirstRow = tileRow * info.TileLength;
            segment.FirstDepth = tileDepthIndex * info.TileDepth;
            segment.Width = width;
            segment.Length = length;
            segment.Depth = depth;
            segment.DecodedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
            segment.ImageByteCount = TiffStrileLayout.CalculateTileImageByteCount(info, tileIndex, diagnostics);
            segment.StoredByteCount = ranges[index].ByteCount;
            segment.ByteRange = ranges[index].Copy();
            segment.IsPartial = width < info.TileWidth || length < info.TileLength || depth < info.TileDepth;
            segment.IsSparse = ranges[index].IsSparse;
            segments[index] = segment;
            index = index + 1;
        }

        layout.ImageIndex = info.ImageIndex;
        layout.PlanarConfiguration = info.PlanarConfiguration;
        layout.TilesAcross = tilesAcross;
        layout.TilesDown = tilesDown;
        layout.TilesDeep = tilesDeep;
        layout.TilesPerPlane = tilesPerPlane;
        layout.TileCount = tileCount;
        layout.Segments = segments;
        layout.IsDataContiguous = AreTileSegmentsContiguous(segments);
        return TiffTileLayoutResult.Create(layout, diagnostics);
    }

    internal static TiffByteRange CreateChoppedRange(TiffStripSegment sourceSegment, ulong consumedByteCount, ulong decodedByteCount, TiffDiagnostics diagnostics)
    {
        if (sourceSegment.IsSparse)
        {
            return new TiffByteRange();
        }

        if (consumedByteCount > sourceSegment.StoredByteCount || decodedByteCount > sourceSegment.StoredByteCount - consumedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "A source strip is shorter than its chopped decoded layout.");
            return new TiffByteRange();
        }

        if (sourceSegment.ByteRange.Offset > ulong.MaxValue - consumedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Chopped strip offset overflows the supported range.");
            return new TiffByteRange();
        }

        return TiffStrileLayout.CreateByteRange(sourceSegment.ByteRange.Offset + consumedByteCount, decodedByteCount, diagnostics);
    }

    internal static bool AreStripSegmentsContiguous(TiffStripSegment[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == null || segments[index].IsSparse)
            {
                return false;
            }

            if (index > 0 && segments[index - 1].ByteRange.EndOffset != segments[index].ByteRange.Offset)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreTileSegmentsContiguous(TiffTileSegment[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == null || segments[index].IsSparse)
            {
                return false;
            }

            if (index > 0 && segments[index - 1].ByteRange.EndOffset != segments[index].ByteRange.Offset)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
