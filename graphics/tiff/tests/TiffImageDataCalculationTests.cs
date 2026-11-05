using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageDataCalculationTests
{
    [Test]
    public void ScanlineCalculations_HandleContiguousAndSeparateRows()
    {
        TiffImageInfo contiguous = CreateStripInfo(4, 4, 3, new int[] { 8, 8, 8 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo separate = CreateStripInfo(4, 4, 2, new int[] { 8, 16 }, 2, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong contiguousStripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(contiguous, 1, 2, diagnostics);
        ulong contiguousByteCount = TiffScanlineReader.CalculateScanlineByteCount(contiguous, contiguousStripIndex, diagnostics);
        ulong separateStripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(separate, 3, 1, diagnostics);
        ulong separateByteCount = TiffScanlineReader.CalculateScanlineByteCount(separate, separateStripIndex, diagnostics);
        ulong separateSourceOffset = TiffScanlineReader.CalculateSourceOffset(separate, 3, separateStripIndex, separateByteCount, diagnostics);

        Assert.That(contiguousStripIndex, Is.EqualTo(0));
        Assert.That(contiguousByteCount, Is.EqualTo(12));
        Assert.That(separateStripIndex, Is.EqualTo(3));
        Assert.That(separateByteCount, Is.EqualTo(8));
        Assert.That(separateSourceOffset, Is.EqualTo(8));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void StripRegionCalculations_ComputeByteAlignedRowsAndTotals()
    {
        TiffImageInfo info = CreateStripInfo(10, 6, 3, new int[] { 8, 8, 8 }, 3, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(info, 0, diagnostics);
        ulong rowByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(2, bytesPerPixel, diagnostics);
        ulong totalByteCount = TiffStripRegionReader.CalculateRegionTotalByteCount(rowByteCount, 3, new TiffLimits(), diagnostics);
        ulong columnByteOffset = TiffStripRegionReader.CalculateColumnByteOffset(4, bytesPerPixel, diagnostics);

        Assert.That(bytesPerPixel, Is.EqualTo(3));
        Assert.That(rowByteCount, Is.EqualTo(6));
        Assert.That(totalByteCount, Is.EqualTo(18));
        Assert.That(columnByteOffset, Is.EqualTo(12));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void StripRegionCalculations_HandleSeparateSampleByteWidth()
    {
        TiffImageInfo info = CreateStripInfo(10, 6, 2, new int[] { 8, 16 }, 3, TiffPlanarConfiguration.Separate);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(info, 1, diagnostics);
        ulong rowByteCount = TiffStripRegionReader.CalculateRegionRowByteCount(4, bytesPerPixel, diagnostics);

        Assert.That(bytesPerPixel, Is.EqualTo(2));
        Assert.That(rowByteCount, Is.EqualTo(8));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void RegionCalculations_RejectSubBytePixelsAndAllocationOverflow()
    {
        TiffImageInfo subByte = CreateStripInfo(8, 1, 1, new int[] { 1 }, 1, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics subByteDiagnostics = TiffDiagnostics.Create();
        TiffDiagnostics allocationDiagnostics = TiffDiagnostics.Create();
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 5;

        ulong bytesPerPixel = TiffStripRegionReader.CalculateBytesPerPixel(subByte, 0, subByteDiagnostics);
        ulong totalByteCount = TiffStripRegionReader.CalculateRegionTotalByteCount(6, 1, limits, allocationDiagnostics);

        Assert.That(bytesPerPixel, Is.EqualTo(0));
        Assert.That(subByteDiagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(totalByteCount, Is.EqualTo(6));
        Assert.That(allocationDiagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void TileRegionCalculations_MapImageCoordinatesToTileStorageOffsets()
    {
        TiffImageInfo info = CreateTileInfo(5, 4, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        ulong tileIndex = TiffStrileLayout.CalculateTileIndexForCoordinateAndSample(info, 4, 3, 0, diagnostics);
        ulong columnInTile = TiffTileRegionReader.CalculateColumnInTile(info, 4, tileIndex, diagnostics);
        ulong rowInTile = TiffTileRegionReader.CalculateRowInTile(info, 3, tileIndex, diagnostics);
        ulong copyPixelCount = TiffTileRegionReader.CalculateCopyPixelCount(info, columnInTile, 2, diagnostics);
        ulong sourceOffset = TiffTileRegionReader.CalculateTileSourceOffset(info, columnInTile, rowInTile, 1, diagnostics);

        Assert.That(tileIndex, Is.EqualTo(3));
        Assert.That(columnInTile, Is.EqualTo(1));
        Assert.That(rowInTile, Is.EqualTo(1));
        Assert.That(copyPixelCount, Is.EqualTo(2));
        Assert.That(sourceOffset, Is.EqualTo(4));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void TileRegionCalculations_ValidateBoundsAndTiledLayout()
    {
        TiffImageInfo tiled = CreateTileInfo(5, 4, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo stripped = CreateStripInfo(5, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics boundsDiagnostics = TiffDiagnostics.Create();
        TiffDiagnostics layoutDiagnostics = TiffDiagnostics.Create();

        bool boundsValid = TiffTileRegionReader.ValidateRegion(tiled, 4, 0, 2, 1, 0, boundsDiagnostics);
        bool layoutValid = TiffTileRegionReader.ValidateRegion(stripped, 0, 0, 1, 1, 0, layoutDiagnostics);

        Assert.That(boundsValid, Is.False);
        Assert.That(boundsDiagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(layoutValid, Is.False);
        Assert.That(layoutDiagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void StripAndTileRanges_MarkEitherZeroOffsetOrZeroByteCountSparse()
    {
        TiffImageInfo stripInfo = CreateStripInfo(4, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo tileInfo = CreateTileInfo(4, 4, 1, new int[] { 8 }, 2, 2, TiffPlanarConfiguration.Contiguous);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();

        TiffByteRange[] stripRanges = TiffStrileLayout.CalculateStripByteRanges(stripInfo, new ulong[] { 20, 30 }, new ulong[] { 0, 8 }, diagnostics);
        TiffByteRange[] tileRanges = TiffStrileLayout.CalculateTileByteRanges(tileInfo, new ulong[] { 20, 0, 40, 44 }, new ulong[] { 4, 4, 0, 4 }, diagnostics);

        Assert.That(stripRanges[0].IsSparse, Is.True);
        Assert.That(stripRanges[1].IsSparse, Is.False);
        Assert.That(tileRanges[0].IsSparse, Is.False);
        Assert.That(tileRanges[1].IsSparse, Is.True);
        Assert.That(tileRanges[2].IsSparse, Is.True);
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    static TiffImageInfo CreateStripInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong rowsPerStrip, TiffPlanarConfiguration planarConfiguration)
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
