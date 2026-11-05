using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffStrileLayoutTests
{
    [Test]
    public void CalculateStripLayout_HandlesContiguousRgbImage()
    {
        TiffImageInfo info = CreateInfo(10, 10, 3, new int[] { 8, 8, 8 }, 4, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong stripsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(info, diagnostics);
        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        ulong firstRows = TiffStrileLayout.CalculateRowsInStrip(info, 0, diagnostics);
        ulong lastRows = TiffStrileLayout.CalculateRowsInStrip(info, 2, diagnostics);
        ulong firstByteCount = TiffStrileLayout.CalculateStripByteCount(info, 0, diagnostics);
        ulong lastByteCount = TiffStrileLayout.CalculateStripByteCount(info, 2, diagnostics);

        Assert.That(stripsPerPlane, Is.EqualTo(3));
        Assert.That(stripCount, Is.EqualTo(3));
        Assert.That(firstRows, Is.EqualTo(4));
        Assert.That(lastRows, Is.EqualTo(2));
        Assert.That(firstByteCount, Is.EqualTo(120));
        Assert.That(lastByteCount, Is.EqualTo(60));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateStripByteCount_RoundsSubByteRowsUp()
    {
        TiffImageInfo info = CreateInfo(13, 5, 1, new int[] { 1 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong firstByteCount = TiffStrileLayout.CalculateStripByteCount(info, 0, diagnostics);
        ulong secondByteCount = TiffStrileLayout.CalculateStripByteCount(info, 1, diagnostics);
        ulong lastByteCount = TiffStrileLayout.CalculateStripByteCount(info, 2, diagnostics);

        Assert.That(firstByteCount, Is.EqualTo(4));
        Assert.That(secondByteCount, Is.EqualTo(4));
        Assert.That(lastByteCount, Is.EqualTo(2));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateStripLayout_HandlesPlanarSeparateImages()
    {
        TiffImageInfo info = CreateInfo(10, 10, 3, new int[] { 8, 16, 8 }, 4, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong stripsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(info, diagnostics);
        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        ulong firstPlaneByteCount = TiffStrileLayout.CalculateStripByteCount(info, 0, diagnostics);
        ulong secondPlaneByteCount = TiffStrileLayout.CalculateStripByteCount(info, 3, diagnostics);
        ulong lastPlaneByteCount = TiffStrileLayout.CalculateStripByteCount(info, 8, diagnostics);

        Assert.That(stripsPerPlane, Is.EqualTo(3));
        Assert.That(stripCount, Is.EqualTo(9));
        Assert.That(firstPlaneByteCount, Is.EqualTo(40));
        Assert.That(secondPlaneByteCount, Is.EqualTo(80));
        Assert.That(lastPlaneByteCount, Is.EqualTo(20));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateStripByteRanges_MapsOffsetsAndCounts()
    {
        TiffImageInfo info = CreateInfo(10, 10, 3, new int[] { 8, 8, 8 }, 4, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();
        ulong[] offsets = new ulong[] { 100, 0, 150 };
        ulong[] byteCounts = new ulong[] { 120, 0, 60 };

        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, offsets, byteCounts, diagnostics);

        Assert.That(ranges.Length, Is.EqualTo(3));
        Assert.That(ranges[0].Offset, Is.EqualTo(100));
        Assert.That(ranges[0].ByteCount, Is.EqualTo(120));
        Assert.That(ranges[0].EndOffset, Is.EqualTo(220));
        Assert.That(ranges[0].IsSparse, Is.False);
        Assert.That(ranges[1].Offset, Is.EqualTo(0));
        Assert.That(ranges[1].ByteCount, Is.EqualTo(0));
        Assert.That(ranges[1].EndOffset, Is.EqualTo(0));
        Assert.That(ranges[1].IsSparse, Is.True);
        Assert.That(ranges[2].Offset, Is.EqualTo(150));
        Assert.That(ranges[2].ByteCount, Is.EqualTo(60));
        Assert.That(ranges[2].EndOffset, Is.EqualTo(210));
        Assert.That(ranges[2].IsSparse, Is.False);
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateStripByteRanges_ReportsMismatchedArrayCounts()
    {
        TiffImageInfo info = CreateInfo(10, 10, 3, new int[] { 8, 8, 8 }, 4, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();
        ulong[] offsets = new ulong[] { 100, 220 };
        ulong[] byteCounts = new ulong[] { 120, 120, 60 };

        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, offsets, byteCounts, diagnostics);

        Assert.That(ranges.Length, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.StripOffsets));
    }

    [Test]
    public void CalculateStripCount_ReportsInvalidStripInputs()
    {
        TiffImageInfo info = CreateInfo(10, 10, 1, new int[] { 8 }, 0, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);

        Assert.That(stripCount, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.RowsPerStrip));
    }

    [Test]
    public void CreateByteRange_ReportsEndOffsetOverflow()
    {
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        TiffByteRange range = TiffStrileLayout.CreateByteRange(ulong.MaxValue, 1, diagnostics);

        Assert.That(range.Offset, Is.EqualTo(ulong.MaxValue));
        Assert.That(range.ByteCount, Is.EqualTo(1));
        Assert.That(range.EndOffset, Is.EqualTo(0));
        Assert.That(range.IsSparse, Is.False);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void CalculateStripBitsPerRow_RejectsOutOfRangeStripIndex()
    {
        TiffImageInfo info = CreateInfo(10, 10, 1, new int[] { 8 }, 4, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong bitsPerRow = TiffStrileLayout.CalculateStripBitsPerRow(info, 3, diagnostics);

        Assert.That(bitsPerRow, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void CalculateTileLayout_HandlesContiguousRgbEdgeTiles()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        ulong tilesDown = TiffStrileLayout.CalculateTilesDown(info, diagnostics);
        ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        ulong firstTileStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 0, diagnostics);
        ulong firstTileImageBytes = TiffStrileLayout.CalculateTileImageByteCount(info, 0, diagnostics);
        ulong edgeTileWidth = TiffStrileLayout.CalculateTileWidthInImage(info, 8, diagnostics);
        ulong edgeTileLength = TiffStrileLayout.CalculateTileLengthInImage(info, 8, diagnostics);
        ulong edgeTileStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 8, diagnostics);
        ulong edgeTileImageBytes = TiffStrileLayout.CalculateTileImageByteCount(info, 8, diagnostics);

        Assert.That(tilesAcross, Is.EqualTo(3));
        Assert.That(tilesDown, Is.EqualTo(3));
        Assert.That(tilesPerPlane, Is.EqualTo(9));
        Assert.That(tileCount, Is.EqualTo(9));
        Assert.That(firstTileStorageBytes, Is.EqualTo(150));
        Assert.That(firstTileImageBytes, Is.EqualTo(150));
        Assert.That(edgeTileWidth, Is.EqualTo(5));
        Assert.That(edgeTileLength, Is.EqualTo(2));
        Assert.That(edgeTileStorageBytes, Is.EqualTo(150));
        Assert.That(edgeTileImageBytes, Is.EqualTo(30));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateTileByteCounts_RoundSubByteRowsUp()
    {
        TiffImageInfo info = CreateTileInfo(13, 2, 1, new int[] { 1 }, 10, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong firstTileStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 0, diagnostics);
        ulong firstTileImageBytes = TiffStrileLayout.CalculateTileImageByteCount(info, 0, diagnostics);
        ulong edgeTileStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 1, diagnostics);
        ulong edgeTileImageBytes = TiffStrileLayout.CalculateTileImageByteCount(info, 1, diagnostics);

        Assert.That(firstTileStorageBytes, Is.EqualTo(4));
        Assert.That(firstTileImageBytes, Is.EqualTo(4));
        Assert.That(edgeTileStorageBytes, Is.EqualTo(4));
        Assert.That(edgeTileImageBytes, Is.EqualTo(2));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateTileLayout_HandlesPlanarSeparateImages()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 16, 8 }, 10, 5, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        ulong firstPlaneStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 0, diagnostics);
        ulong secondPlaneStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 9, diagnostics);
        ulong lastPlaneStorageBytes = TiffStrileLayout.CalculateTileStorageByteCount(info, 26, diagnostics);
        ulong lastPlaneImageBytes = TiffStrileLayout.CalculateTileImageByteCount(info, 26, diagnostics);

        Assert.That(tilesPerPlane, Is.EqualTo(9));
        Assert.That(tileCount, Is.EqualTo(27));
        Assert.That(firstPlaneStorageBytes, Is.EqualTo(50));
        Assert.That(secondPlaneStorageBytes, Is.EqualTo(100));
        Assert.That(lastPlaneStorageBytes, Is.EqualTo(50));
        Assert.That(lastPlaneImageBytes, Is.EqualTo(10));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateTileByteRanges_MapsOffsetsAndCounts()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();
        ulong[] offsets = new ulong[] { 100, 250, 400, 550, 0, 700, 850, 1000, 1150 };
        ulong[] byteCounts = new ulong[] { 150, 150, 150, 150, 0, 150, 150, 150, 30 };

        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, offsets, byteCounts, diagnostics);

        Assert.That(ranges.Length, Is.EqualTo(9));
        Assert.That(ranges[0].Offset, Is.EqualTo(100));
        Assert.That(ranges[0].ByteCount, Is.EqualTo(150));
        Assert.That(ranges[0].EndOffset, Is.EqualTo(250));
        Assert.That(ranges[0].IsSparse, Is.False);
        Assert.That(ranges[4].Offset, Is.EqualTo(0));
        Assert.That(ranges[4].ByteCount, Is.EqualTo(0));
        Assert.That(ranges[4].IsSparse, Is.True);
        Assert.That(ranges[8].EndOffset, Is.EqualTo(1180));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateTileByteRanges_ReportsMismatchedArrayCounts()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();
        ulong[] offsets = new ulong[] { 100, 250 };
        ulong[] byteCounts = new ulong[] { 150, 150, 150, 150, 150, 150, 150, 150, 30 };

        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, offsets, byteCounts, diagnostics);

        Assert.That(ranges.Length, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileOffsets));
    }

    [Test]
    public void CalculateTileCount_ReportsInvalidTileInputs()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 1, new int[] { 8 }, 0, 5, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);

        Assert.That(tileCount, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileWidth));
    }

    [Test]
    public void CalculateTileImageByteCount_RejectsOutOfRangeTileIndex()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 1, new int[] { 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong byteCount = TiffStrileLayout.CalculateTileImageByteCount(info, 9, diagnostics);

        Assert.That(byteCount, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void CalculateSampleBufferIndex_UsesContiguousLayout()
    {
        TiffImageInfo info = CreateInfo(4, 3, 3, new int[] { 8, 8, 8 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong sampleIndex = TiffStrileLayout.CalculateSampleBufferIndex(info, 2, 1, 1, diagnostics);
        ulong contiguousSampleIndex = TiffStrileLayout.CalculateContiguousSampleBufferIndex(info, 2, 1, 1, diagnostics);

        Assert.That(sampleIndex, Is.EqualTo(19));
        Assert.That(contiguousSampleIndex, Is.EqualTo(19));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateSampleBufferIndex_UsesSeparatePlaneLayout()
    {
        TiffImageInfo info = CreateInfo(4, 3, 3, new int[] { 8, 8, 8 }, 2, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong sampleIndex = TiffStrileLayout.CalculateSampleBufferIndex(info, 2, 1, 1, diagnostics);
        ulong separateSampleIndex = TiffStrileLayout.CalculateSeparateSampleBufferIndex(info, 2, 1, 1, diagnostics);

        Assert.That(sampleIndex, Is.EqualTo(18));
        Assert.That(separateSampleIndex, Is.EqualTo(18));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateStripIndexes_MapRowsAndSamplesForBothPlanarModes()
    {
        TiffImageInfo contiguousInfo = CreateInfo(10, 10, 3, new int[] { 8, 8, 8 }, 4, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo separateInfo = CreateInfo(10, 10, 3, new int[] { 8, 8, 8 }, 4, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong contiguousStripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(contiguousInfo, 7, 2, diagnostics);
        ulong separateStripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(separateInfo, 7, 2, diagnostics);
        int contiguousSampleIndex = TiffStrileLayout.CalculateStripSampleIndex(contiguousInfo, 1, diagnostics);
        int separateSampleIndex = TiffStrileLayout.CalculateStripSampleIndex(separateInfo, 7, diagnostics);
        ulong separateIndexInPlane = TiffStrileLayout.CalculateStripIndexInPlane(separateInfo, 7, diagnostics);

        Assert.That(contiguousStripIndex, Is.EqualTo(1));
        Assert.That(separateStripIndex, Is.EqualTo(7));
        Assert.That(contiguousSampleIndex, Is.EqualTo(0));
        Assert.That(separateSampleIndex, Is.EqualTo(2));
        Assert.That(separateIndexInPlane, Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateTileIndexes_MapCoordinatesAndSamplesForBothPlanarModes()
    {
        TiffImageInfo contiguousInfo = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo separateInfo = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong contiguousTileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateAndSample(contiguousInfo, 24, 11, 2, diagnostics);
        ulong separateTileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateAndSample(separateInfo, 24, 11, 2, diagnostics);
        int contiguousSampleIndex = TiffStrileLayout.CalculateTileSampleIndex(contiguousInfo, 8, diagnostics);
        int separateSampleIndex = TiffStrileLayout.CalculateTileSampleIndex(separateInfo, 26, diagnostics);
        ulong separateIndexInPlane = TiffStrileLayout.CalculateTilePlaneIndex(separateInfo, 26, diagnostics);

        Assert.That(contiguousTileIndex, Is.EqualTo(8));
        Assert.That(separateTileIndex, Is.EqualTo(26));
        Assert.That(contiguousSampleIndex, Is.EqualTo(0));
        Assert.That(separateSampleIndex, Is.EqualTo(2));
        Assert.That(separateIndexInPlane, Is.EqualTo(8));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CalculateSampleBufferIndex_RejectsInvalidCoordinatesAndSamples()
    {
        TiffImageInfo info = CreateInfo(4, 3, 3, new int[] { 8, 8, 8 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong columnIndex = TiffStrileLayout.CalculateSampleBufferIndex(info, 4, 1, 1, diagnostics);
        ulong sampleIndex = TiffStrileLayout.CalculateSampleBufferIndex(info, 2, 1, 3, diagnostics);

        Assert.That(columnIndex, Is.EqualTo(0));
        Assert.That(sampleIndex, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void CalculateSeparateSampleBufferIndex_ReportsPlaneOffsetOverflow()
    {
        TiffImageInfo info = CreateInfo(ulong.MaxValue, 2, 2, new int[] { 8, 8 }, 2, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong sampleIndex = TiffStrileLayout.CalculateSeparateSampleBufferIndex(info, 0, 0, 1, diagnostics);

        Assert.That(sampleIndex, Is.EqualTo(0));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong rowsPerStrip, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.RowsPerStrip = rowsPerStrip;
        info.PlanarConfiguration = planarConfiguration;
        return info;
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong tileWidth, ulong tileLength, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
        info.PlanarConfiguration = planarConfiguration;
        return info;
    }
}
