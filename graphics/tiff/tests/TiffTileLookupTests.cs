using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTileLookupTests
{
    [Test]
    public void LookupTile_MapsCoordinateToEdgeTileAndByteRange()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);
        ulong[] offsets = CreateSequence(9, 100, 10);
        ulong[] byteCounts = CreateSequence(9, 10, 1);

        TiffTileLookupResult result = TiffCodec.LookupTile(info, offsets, byteCounts, 24, 11, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Lookup.ImageIndex, Is.EqualTo(4));
        Assert.That(result.Lookup.TileIndex, Is.EqualTo(8));
        Assert.That(result.Lookup.TileIndexInPlane, Is.EqualTo(8));
        Assert.That(result.Lookup.TileColumn, Is.EqualTo(2));
        Assert.That(result.Lookup.TileRow, Is.EqualTo(2));
        Assert.That(result.Lookup.FirstColumn, Is.EqualTo(20));
        Assert.That(result.Lookup.FirstRow, Is.EqualTo(10));
        Assert.That(result.Lookup.Width, Is.EqualTo(5));
        Assert.That(result.Lookup.Length, Is.EqualTo(2));
        Assert.That(result.Lookup.StorageByteCount, Is.EqualTo(150));
        Assert.That(result.Lookup.ImageByteCount, Is.EqualTo(30));
        Assert.That(result.Lookup.ByteRange.Offset, Is.EqualTo(180));
        Assert.That(result.Lookup.ByteRange.ByteCount, Is.EqualTo(18));
        Assert.That(result.Lookup.ByteRange.EndOffset, Is.EqualTo(198));
        Assert.That(result.Lookup.IsSparse, Is.False);
    }

    [Test]
    public void LookupTile_HandlesSeparateSamplePlanes()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 16, 8 }, 10, 5, TiffPlanarConfiguration.Separate);
        ulong[] offsets = CreateSequence(27, 100, 10);
        ulong[] byteCounts = CreateSequence(27, 1, 1);

        TiffTileLookupResult result = TiffCodec.LookupTile(info, offsets, byteCounts, 24, 11, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Lookup.TileIndex, Is.EqualTo(26));
        Assert.That(result.Lookup.TileIndexInPlane, Is.EqualTo(8));
        Assert.That(result.Lookup.SampleIndex, Is.EqualTo(2));
        Assert.That(result.Lookup.StorageByteCount, Is.EqualTo(50));
        Assert.That(result.Lookup.ImageByteCount, Is.EqualTo(10));
    }

    [Test]
    public void FindBestTileLevel_SelectsSmallestLevelCoveringRequestedSize()
    {
        TiffImageInfo[] levels = new TiffImageInfo[]
        {
            CreateTileInfo(1024, 768, 1, new int[] { 8 }, 256, 256, TiffPlanarConfiguration.Contiguous),
            CreateTileInfo(512, 384, 1, new int[] { 8 }, 128, 128, TiffPlanarConfiguration.Contiguous),
            CreateTileInfo(256, 192, 1, new int[] { 8 }, 64, 64, TiffPlanarConfiguration.Contiguous)
        };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        int mediumLevel = TiffCodec.FindBestTileLevel(levels, 300, 200, diagnostics);
        int smallestLevel = TiffCodec.FindBestTileLevel(levels, 128, 96, diagnostics);
        int baseLevel = TiffCodec.FindBestTileLevel(levels, 2000, 1500, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(mediumLevel, Is.EqualTo(1));
        Assert.That(smallestLevel, Is.EqualTo(2));
        Assert.That(baseLevel, Is.EqualTo(0));
    }

    [Test]
    public void LookupTileLevelFromBaseCoordinate_MapsBaseCoordinateToOverview()
    {
        TiffImageInfo[] levels = new TiffImageInfo[]
        {
            CreateTileInfo(1000, 1000, 1, new int[] { 8 }, 256, 256, TiffPlanarConfiguration.Contiguous),
            CreateTileInfo(250, 250, 1, new int[] { 8 }, 128, 128, TiffPlanarConfiguration.Contiguous)
        };
        ulong[][] offsets = new ulong[][]
        {
            CreateSequence(16, 100, 10),
            CreateSequence(4, 1000, 20)
        };
        ulong[][] byteCounts = new ulong[][]
        {
            CreateSequence(16, 10, 1),
            CreateSequence(4, 50, 1)
        };

        TiffTileLookupResult result = TiffCodec.LookupTileLevelFromBaseCoordinate(levels, offsets, byteCounts, 1, 999, 999, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Lookup.PyramidLevelIndex, Is.EqualTo(1));
        Assert.That(result.Lookup.TileIndex, Is.EqualTo(3));
        Assert.That(result.Lookup.TileColumn, Is.EqualTo(1));
        Assert.That(result.Lookup.TileRow, Is.EqualTo(1));
        Assert.That(result.Lookup.FirstColumn, Is.EqualTo(128));
        Assert.That(result.Lookup.FirstRow, Is.EqualTo(128));
        Assert.That(result.Lookup.Width, Is.EqualTo(122));
        Assert.That(result.Lookup.Length, Is.EqualTo(122));
        Assert.That(result.Lookup.ByteRange.Offset, Is.EqualTo(1060));
    }

    [Test]
    public void LookupTile_ReportsMismatchedByteCountArray()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 3, new int[] { 8, 8, 8 }, 10, 5, TiffPlanarConfiguration.Contiguous);

        TiffTileLookupResult result = TiffCodec.LookupTile(info, CreateSequence(9, 100, 10), CreateSequence(8, 1, 1), 24, 11, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileByteCounts));
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong tileWidth, ulong tileLength, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.ImageIndex = 4;
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

    static ulong[] CreateSequence(int count, ulong start, ulong step)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = start + ((ulong)index * step);
            index = index + 1;
        }

        return values;
    }
}
