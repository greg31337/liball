using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicSegmentLayoutTests
{
    [Test]
    public void CreateStripLayout_ExposesCoordinatesPlanesDecodedAndStoredSizes()
    {
        TiffImageInfo info = CreateStripInfo(10, 5, 2, new int[] { 8, 16 }, 3, TiffPlanarConfiguration.Separate);
        info.Compression = TiffCompression.Lzw;
        ulong[] offsets = new ulong[] { 100, 110, 120, 130 };
        ulong[] byteCounts = new ulong[] { 10, 10, 10, 10 };

        TiffStripLayoutResult result = TiffApi.Codec.CreateStripLayout(info, offsets, byteCounts);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.StripsPerPlane, Is.EqualTo(2));
        Assert.That(result.Layout.SourceStripCount, Is.EqualTo(4));
        Assert.That(result.Layout.StripCount, Is.EqualTo(4));
        Assert.That(result.Layout.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(result.Layout.IsDataContiguous, Is.True);
        Assert.That(result.Layout.IsChopped, Is.False);
        Assert.That(result.Layout.Segments[0].SampleIndex, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[0].FirstRow, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[0].RowCount, Is.EqualTo(3));
        Assert.That(result.Layout.Segments[0].DecodedByteCount, Is.EqualTo(30));
        Assert.That(result.Layout.Segments[0].StoredByteCount, Is.EqualTo(10));
        Assert.That(result.Layout.Segments[1].IsPartial, Is.True);
        Assert.That(result.Layout.Segments[2].SampleIndex, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[2].StripIndexInPlane, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[2].DecodedByteCount, Is.EqualTo(60));
        Assert.That(result.Layout.Segments[3].DecodedByteCount, Is.EqualTo(40));
    }

    [Test]
    public void CreateStripLayout_RecognizesSparseAndNoncontiguousSegments()
    {
        TiffImageInfo info = CreateStripInfo(10, 10, 1, new int[] { 8 }, 4, TiffPlanarConfiguration.Contiguous);

        TiffStripLayoutResult result = TiffApi.Codec.CreateStripLayout(info, new ulong[] { 100, 0, 300 }, new ulong[] { 40, 0, 20 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.IsDataContiguous, Is.False);
        Assert.That(result.Layout.Segments[1].IsSparse, Is.True);
        Assert.That(result.Layout.Segments[1].DecodedByteCount, Is.EqualTo(40));
        Assert.That(result.Layout.Segments[1].StoredByteCount, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[2].FirstRow, Is.EqualTo(8));
        Assert.That(result.Layout.Segments[2].RowCount, Is.EqualTo(2));
    }

    [Test]
    public void CreateChoppedStripLayout_SplitsLargeUncompressedStripIntoVirtualRanges()
    {
        TiffImageInfo info = CreateStripInfo(10, 10, 3, new int[] { 8, 8, 8 }, 10, TiffPlanarConfiguration.Contiguous);
        info.Compression = TiffCompression.None;

        TiffStripLayoutResult result = TiffApi.Codec.CreateChoppedStripLayout(info, new ulong[] { 100 }, new ulong[] { 300 }, 90);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.IsChopped, Is.True);
        Assert.That(result.Layout.SourceStripCount, Is.EqualTo(1));
        Assert.That(result.Layout.StripsPerPlane, Is.EqualTo(4));
        Assert.That(result.Layout.StripCount, Is.EqualTo(4));
        Assert.That(result.Layout.IsDataContiguous, Is.True);
        Assert.That(result.Layout.Segments[0].SourceStripIndex, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[0].RowCount, Is.EqualTo(3));
        Assert.That(result.Layout.Segments[0].DecodedByteCount, Is.EqualTo(90));
        Assert.That(result.Layout.Segments[0].ByteRange.Offset, Is.EqualTo(100));
        Assert.That(result.Layout.Segments[2].ByteRange.Offset, Is.EqualTo(280));
        Assert.That(result.Layout.Segments[3].FirstRow, Is.EqualTo(9));
        Assert.That(result.Layout.Segments[3].RowCount, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[3].StoredByteCount, Is.EqualTo(30));
        Assert.That(result.Layout.Segments[3].IsPartial, Is.True);
        Assert.That(result.Layout.Segments[3].IsChopped, Is.True);
    }

    [Test]
    public void CreateChoppedStripLayout_LeavesCompressedAndAlreadySegmentedLayoutsPhysical()
    {
        TiffImageInfo compressed = CreateStripInfo(10, 10, 1, new int[] { 8 }, 10, TiffPlanarConfiguration.Contiguous);
        compressed.Compression = TiffCompression.Lzw;
        TiffImageInfo segmented = CreateStripInfo(10, 10, 1, new int[] { 8 }, 5, TiffPlanarConfiguration.Contiguous);

        TiffStripLayoutResult compressedResult = TiffApi.Codec.CreateChoppedStripLayout(compressed, new ulong[] { 100 }, new ulong[] { 20 }, 10);
        TiffStripLayoutResult segmentedResult = TiffApi.Codec.CreateChoppedStripLayout(segmented, new ulong[] { 100, 150 }, new ulong[] { 50, 50 }, 10);

        Assert.That(compressedResult.Success, Is.True);
        Assert.That(compressedResult.Layout.IsChopped, Is.False);
        Assert.That(compressedResult.Layout.StripCount, Is.EqualTo(1));
        Assert.That(segmentedResult.Success, Is.True);
        Assert.That(segmentedResult.Layout.IsChopped, Is.False);
        Assert.That(segmentedResult.Layout.StripCount, Is.EqualTo(2));
    }

    [Test]
    public void CreateTileLayout_HandlesVolumetricEdgesAndPlanarSeparateSamples()
    {
        TiffImageInfo info = CreateTileInfo(5, 3, 3, 2, new int[] { 8, 16 }, 4, 2, 2, TiffPlanarConfiguration.Separate);
        ulong[] offsets = CreateSequence(16, 100, 5);
        ulong[] byteCounts = CreateFilled(16, 5);

        TiffTileLayoutResult result = TiffApi.Codec.CreateTileLayout(info, offsets, byteCounts);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.TilesAcross, Is.EqualTo(2));
        Assert.That(result.Layout.TilesDown, Is.EqualTo(2));
        Assert.That(result.Layout.TilesDeep, Is.EqualTo(2));
        Assert.That(result.Layout.TilesPerPlane, Is.EqualTo(8));
        Assert.That(result.Layout.TileCount, Is.EqualTo(16));
        Assert.That(result.Layout.IsDataContiguous, Is.True);
        Assert.That(result.Layout.Segments[7].TileColumn, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].TileRow, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].TileDepthIndex, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].FirstColumn, Is.EqualTo(4));
        Assert.That(result.Layout.Segments[7].FirstRow, Is.EqualTo(2));
        Assert.That(result.Layout.Segments[7].FirstDepth, Is.EqualTo(2));
        Assert.That(result.Layout.Segments[7].Width, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].Length, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].Depth, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].DecodedByteCount, Is.EqualTo(16));
        Assert.That(result.Layout.Segments[7].ImageByteCount, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].IsPartial, Is.True);
        Assert.That(result.Layout.Segments[15].SampleIndex, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[15].DecodedByteCount, Is.EqualTo(32));
        Assert.That(result.Layout.Segments[15].ImageByteCount, Is.EqualTo(2));
        Assert.That(result.Layout.Segments[15].StoredByteCount, Is.EqualTo(5));
    }

    [Test]
    public void LookupVolumeTile_MapsDepthAndSamplePlaneToTile()
    {
        TiffImageInfo info = CreateTileInfo(5, 3, 3, 2, new int[] { 8, 16 }, 4, 2, 2, TiffPlanarConfiguration.Separate);

        TiffTileLookupResult result = TiffApi.Codec.LookupVolumeTile(info, CreateSequence(16, 100, 5), CreateFilled(16, 5), 4, 2, 2, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Lookup.TileIndex, Is.EqualTo(15));
        Assert.That(result.Lookup.TileIndexInPlane, Is.EqualTo(7));
        Assert.That(result.Lookup.SampleIndex, Is.EqualTo(1));
        Assert.That(result.Lookup.TileColumn, Is.EqualTo(1));
        Assert.That(result.Lookup.TileRow, Is.EqualTo(1));
        Assert.That(result.Lookup.TileDepthIndex, Is.EqualTo(1));
        Assert.That(result.Lookup.FirstDepth, Is.EqualTo(2));
        Assert.That(result.Lookup.Depth, Is.EqualTo(1));
        Assert.That(result.Lookup.StorageByteCount, Is.EqualTo(32));
        Assert.That(result.Lookup.ImageByteCount, Is.EqualTo(2));
    }

    [Test]
    public void CreateTileLayout_RecognizesSparseTiles()
    {
        TiffImageInfo info = CreateTileInfo(5, 3, 1, 1, new int[] { 8 }, 4, 2, 1, TiffPlanarConfiguration.Contiguous);
        ulong[] offsets = new ulong[] { 100, 0, 108, 112 };
        ulong[] byteCounts = new ulong[] { 4, 0, 4, 4 };

        TiffTileLayoutResult result = TiffApi.Codec.CreateTileLayout(info, offsets, byteCounts);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.IsDataContiguous, Is.False);
        Assert.That(result.Layout.Segments[1].IsSparse, Is.True);
        Assert.That(result.Layout.Segments[1].StoredByteCount, Is.EqualTo(0));
        Assert.That(result.Layout.Segments[1].DecodedByteCount, Is.EqualTo(8));
        Assert.That(result.Layout.Segments[1].ImageByteCount, Is.EqualTo(2));
    }

    [Test]
    public void ImageLayoutHelpers_LoadPhysicalRangesFromImageTags()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 5),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 3),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripOffsets, TiffDataType.Long, new ulong[] { 100, 130 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripByteCounts, TiffDataType.Long, new ulong[] { 30, 20 })
        };
        TiffImage image = TiffFile.OpenRead(CreateClassicTiffFile(values)).GetImage(0);

        TiffStripLayoutResult physical = image.GetStripLayout();
        TiffStripLayoutResult chopped = image.GetChoppedStripLayout(10);

        Assert.That(physical.Success, Is.True);
        Assert.That(physical.Layout.StripCount, Is.EqualTo(2));
        Assert.That(physical.Layout.Segments[1].FirstRow, Is.EqualTo(3));
        Assert.That(physical.Layout.Segments[1].StoredByteCount, Is.EqualTo(20));
        Assert.That(chopped.Success, Is.True);
        Assert.That(chopped.Layout.IsChopped, Is.False);
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void ImageTileLayoutHelper_LoadsVolumetricRangesFromImageTags()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 5),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 3),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, 4),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageDepth, TiffDataType.Long, 3),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileDepth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedArray(TiffTagId.TileOffsets, TiffDataType.Long, CreateSequence(8, 100, 5)),
            TiffTagValue.CreateUnsignedArray(TiffTagId.TileByteCounts, TiffDataType.Long, CreateFilled(8, 5))
        };
        TiffImage image = TiffFile.OpenRead(CreateClassicTiffFile(values)).GetImage(0);

        TiffTileLayoutResult result = image.GetTileLayout();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Layout.TileCount, Is.EqualTo(8));
        Assert.That(result.Layout.TilesDeep, Is.EqualTo(2));
        Assert.That(result.Layout.Segments[7].FirstDepth, Is.EqualTo(2));
        Assert.That(result.Layout.Segments[7].Depth, Is.EqualTo(1));
        Assert.That(result.Layout.Segments[7].StoredByteCount, Is.EqualTo(5));
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void LayoutHelpers_ReportInvalidArrayCountsAndChopTargets()
    {
        TiffImageInfo stripInfo = CreateStripInfo(10, 10, 1, new int[] { 8 }, 10, TiffPlanarConfiguration.Contiguous);
        TiffImageInfo tileInfo = CreateTileInfo(5, 3, 1, 1, new int[] { 8 }, 4, 2, 1, TiffPlanarConfiguration.Contiguous);

        TiffStripLayoutResult chopResult = TiffApi.Codec.CreateChoppedStripLayout(stripInfo, new ulong[] { 100 }, new ulong[] { 100 }, 0);
        TiffTileLayoutResult tileResult = TiffApi.Codec.CreateTileLayout(tileInfo, new ulong[] { 100 }, new ulong[] { 10 });
        TiffTileLookupResult lookupResult = TiffApi.Codec.LookupVolumeTile(tileInfo, new ulong[] { 100, 110, 120, 130 }, new ulong[] { 10, 10, 10, 10 }, 0, 0, 1, 0);

        Assert.That(chopResult.Success, Is.False);
        Assert.That(chopResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(tileResult.Success, Is.False);
        Assert.That(tileResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(lookupResult.Success, Is.False);
        Assert.That(lookupResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
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
        info.IsTiled = false;
        return info;
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, ulong imageDepth, int samplesPerPixel, int[] bitsPerSample, ulong tileWidth, ulong tileLength, ulong tileDepth, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.ImageDepth = imageDepth;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
        info.TileDepth = tileDepth;
        info.PlanarConfiguration = planarConfiguration;
        info.IsTiled = true;
        return info;
    }

    static ulong[] CreateSequence(int count, ulong start, ulong step)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = start + (ulong)index * step;
            index = index + 1;
        }

        return values;
    }

    static ulong[] CreateFilled(int count, ulong value)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
    }

    static byte[] CreateClassicTiffFile(TiffTagValue[] values)
    {
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        TiffDiagnostics encodeDiagnostics = new TiffDiagnostics();
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(values, TiffByteOrder.LittleEndian, encodeDiagnostics);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, valueBytes, TiffByteOrder.LittleEndian, 8, 0);
        Assert.That(encodeDiagnostics.HasErrors(), Is.False);
        Assert.That(writeDiagnostics.HasErrors(), Is.False);
        return fileBytes;
    }
}
