using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicImageLayoutApiTests
{
    [Test]
    public void StrippedContiguousPartialEdge_EncodedScanlineRegionRasterAndPlaneReadsAgree()
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(4, 3);
        configuration.Layout.RowsPerStrip = 2;
        configuration.Compression = TiffCompression.PackBits;
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        byte[] firstStrip = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] secondStrip = new byte[] { 9, 10, 11, 12 };

        TiffRawStripWriteResult firstWrite = writer.WriteEncodedStrip(imageIndex, 0, firstStrip);
        TiffRawStripWriteResult secondWrite = writer.WriteEncodedStrip(imageIndex, 1, secondStrip);
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        TiffRawStripReadResult raw = image.ReadRawStrip(0);
        TiffRawStripReadResult decoded = image.ReadEncodedStrip(0);
        byte[] decodedBuffer = new byte[4];
        TiffDiagnostics decodedBufferRead = image.ReadEncodedStripToBuffer(1, decodedBuffer);
        byte[] scanline = new byte[4];
        TiffDiagnostics scanlineRead = image.ReadScanlineToBuffer(2, scanline);
        TiffRegionReadOptions regionOptions = CreateRegionOptions(1, 1, 2, 2, TiffRegionOutput.Source, 0);
        byte[] region = new byte[4];
        TiffDiagnostics regionRead = image.ReadRegionToBuffer(regionOptions, region);
        byte[] raster = new byte[12];
        byte[] plane = new byte[12];
        TiffDiagnostics rasterRead = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics planeRead = image.ReadSamplePlaneToBuffer(0, plane);
        TiffStripLayoutResult layout = image.GetStripLayout();

        Assert.That(firstWrite.Success, Is.True);
        Assert.That(secondWrite.Success, Is.True);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(raw.Success, Is.True);
        Assert.That(raw.Bytes, Is.Not.EqualTo(firstStrip));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(firstStrip));
        Assert.That(decodedBufferRead.HasErrors(), Is.False);
        Assert.That(decodedBuffer, Is.EqualTo(secondStrip));
        Assert.That(scanlineRead.HasErrors(), Is.False);
        Assert.That(scanline, Is.EqualTo(secondStrip));
        Assert.That(regionRead.HasErrors(), Is.False);
        Assert.That(region, Is.EqualTo(new byte[] { 6, 7, 10, 11 }));
        Assert.That(rasterRead.HasErrors(), Is.False);
        Assert.That(raster, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
        Assert.That(planeRead.HasErrors(), Is.False);
        Assert.That(plane, Is.EqualTo(raster));
        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.StripCount, Is.EqualTo(2));
        Assert.That(layout.Layout.Segments[1].IsPartial, Is.True);
        Assert.That(layout.Layout.Segments[1].RowCount, Is.EqualTo(1));
    }

    [Test]
    public void TiledSeparatePartialEdges_SourceRasterWriteSupportsEveryNormalizedReadView()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 3);
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        configuration.Compression = TiffCompression.Deflate;
        byte[] source = new byte[]
        {
            1, 11, 21, 2, 12, 22, 3, 13, 23,
            4, 14, 24, 5, 15, 25, 6, 16, 26,
            7, 17, 27, 8, 18, 28, 9, 19, 29
        };
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        TiffRawTileReadResult rawEdge = image.ReadRawTile(1);
        TiffRawTileReadResult decodedEdge = image.ReadEncodedTile(1);
        byte[] decodedBuffer = new byte[4];
        TiffDiagnostics decodedBufferRead = image.ReadEncodedTileToBuffer(1, decodedBuffer);
        byte[] raster = new byte[source.Length];
        byte[] greenPlane = new byte[9];
        byte[] blueScanline = new byte[3];
        TiffDiagnostics rasterRead = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics planeRead = image.ReadSamplePlaneToBuffer(1, greenPlane);
        TiffDiagnostics scanlineRead = image.ReadSampleScanlineToBuffer(2, 2, blueScanline);
        TiffRegionReadOptions regionOptions = CreateRegionOptions(1, 1, 2, 2, TiffRegionOutput.Normalized, 0);
        byte[] region = new byte[12];
        TiffDiagnostics regionRead = image.ReadRegionToBuffer(regionOptions, region);
        TiffTileLayoutResult layout = image.GetTileLayout();

        Assert.That(write.HasErrors(), Is.False);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(image.GetTileCount(), Is.EqualTo(12));
        Assert.That(rawEdge.Success, Is.True);
        Assert.That(rawEdge.Bytes, Is.Not.EqualTo(new byte[] { 3, 0, 6, 0 }));
        Assert.That(decodedEdge.Success, Is.True);
        Assert.That(decodedEdge.Bytes, Is.EqualTo(new byte[] { 3, 0, 6, 0 }));
        Assert.That(decodedBufferRead.HasErrors(), Is.False);
        Assert.That(decodedBuffer, Is.EqualTo(decodedEdge.Bytes));
        Assert.That(rasterRead.HasErrors(), Is.False);
        Assert.That(raster, Is.EqualTo(source));
        Assert.That(planeRead.HasErrors(), Is.False);
        Assert.That(greenPlane, Is.EqualTo(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19 }));
        Assert.That(scanlineRead.HasErrors(), Is.False);
        Assert.That(blueScanline, Is.EqualTo(new byte[] { 27, 28, 29 }));
        Assert.That(regionRead.HasErrors(), Is.False);
        Assert.That(region, Is.EqualTo(new byte[] { 5, 15, 25, 6, 16, 26, 8, 18, 28, 9, 19, 29 }));
        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.Segments[1].IsPartial, Is.True);
        Assert.That(layout.Layout.Segments[3].Width, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[3].Length, Is.EqualTo(1));
    }

    [Test]
    public void ScanlineRegionAndSamplePlaneWrites_ComposeCompressedSeparateStrips()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 2);
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.RowsPerStrip = 1;
        configuration.Compression = TiffCompression.Deflate;
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffRegion secondRowRegion = new TiffRegion();
        secondRowRegion.Width = 3;
        secondRowRegion.Length = 1;
        secondRowRegion.Row = 1;

        TiffDiagnostics scanlineWrite = writer.WriteScanline(imageIndex, 0, new byte[] { 1, 11, 21, 2, 12, 22, 3, 13, 23 });
        TiffDiagnostics regionWrite = writer.WriteRegion(imageIndex, secondRowRegion, new byte[] { 4, 14, 24, 5, 15, 25, 6, 16, 26 });
        TiffDiagnostics planeWrite = writer.WriteSamplePlane(imageIndex, 2, new byte[] { 31, 32, 33, 34, 35, 36 });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        byte[] raster = new byte[18];
        byte[] bluePlane = new byte[6];
        TiffDiagnostics rasterRead = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics planeRead = image.ReadSamplePlaneToBuffer(2, bluePlane);
        TiffRawStripReadResult rawRed = image.ReadRawStrip(0);
        TiffRawStripReadResult decodedRed = image.ReadEncodedStrip(0);

        Assert.That(scanlineWrite.HasErrors(), Is.False);
        Assert.That(regionWrite.HasErrors(), Is.False);
        Assert.That(planeWrite.HasErrors(), Is.False);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(rasterRead.HasErrors(), Is.False);
        Assert.That(raster, Is.EqualTo(new byte[]
        {
            1, 11, 31, 2, 12, 32, 3, 13, 33,
            4, 14, 34, 5, 15, 35, 6, 16, 36
        }));
        Assert.That(planeRead.HasErrors(), Is.False);
        Assert.That(bluePlane, Is.EqualTo(new byte[] { 31, 32, 33, 34, 35, 36 }));
        Assert.That(rawRed.Bytes, Is.Not.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(decodedRed.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void SparseRawStripAndTileWrites_ZeroFillAllDecodedReadPaths()
    {
        TiffWriterImageConfiguration stripConfiguration = CreateGrayConfiguration(4, 2);
        stripConfiguration.Layout.RowsPerStrip = 1;
        stripConfiguration.Layout.AllowSparseSegments = true;
        TiffWriterImageConfiguration tileConfiguration = CreateGrayConfiguration(4, 1);
        tileConfiguration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        tileConfiguration.Layout.TileWidth = 2;
        tileConfiguration.Layout.TileLength = 1;
        tileConfiguration.Layout.TileDepth = 1;
        tileConfiguration.Layout.AllowSparseSegments = true;
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int stripImageIndex = writer.AddConfiguredImage(stripConfiguration);
        int tileImageIndex = writer.AddConfiguredImage(tileConfiguration);

        TiffRawStripWriteResult sparseStripWrite = writer.WriteRawStrip(stripImageIndex, 0, new byte[0]);
        TiffRawStripWriteResult storedStripWrite = writer.WriteRawStrip(stripImageIndex, 1, new byte[] { 7, 8, 9, 10 });
        TiffRawTileWriteResult sparseTileWrite = writer.WriteRawTile(tileImageIndex, 0, new byte[0]);
        TiffRawTileWriteResult storedTileWrite = writer.WriteRawTile(tileImageIndex, 1, new byte[] { 11, 12 });
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        TiffImage stripImage = file.GetImage(0);
        TiffImage tileImage = file.GetImage(1);
        TiffRawStripReadResult rawStrip = stripImage.ReadRawStrip(0);
        TiffRawStripReadResult decodedStrip = stripImage.ReadEncodedStrip(0);
        TiffRawTileReadResult rawTile = tileImage.ReadRawTile(0);
        TiffRawTileReadResult decodedTile = tileImage.ReadEncodedTile(0);
        byte[] stripRaster = new byte[8];
        TiffDiagnostics stripRasterRead = stripImage.ReadSourceRasterToBuffer(stripRaster);
        TiffRegionReadOptions tileRegionOptions = CreateRegionOptions(1, 0, 2, 1, TiffRegionOutput.Source, 0);
        byte[] tileRegion = new byte[2];
        TiffDiagnostics tileRegionRead = tileImage.ReadRegionToBuffer(tileRegionOptions, tileRegion);

        Assert.That(sparseStripWrite.Success, Is.True);
        Assert.That(storedStripWrite.Success, Is.True);
        Assert.That(sparseTileWrite.Success, Is.True);
        Assert.That(storedTileWrite.Success, Is.True);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(rawStrip.IsSparse, Is.True);
        Assert.That(rawStrip.Bytes, Is.Empty);
        Assert.That(decodedStrip.Success, Is.True);
        Assert.That(decodedStrip.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(rawTile.IsSparse, Is.True);
        Assert.That(rawTile.Bytes, Is.Empty);
        Assert.That(decodedTile.Success, Is.True);
        Assert.That(decodedTile.Bytes, Is.EqualTo(new byte[] { 0, 0 }));
        Assert.That(stripRasterRead.HasErrors(), Is.False);
        Assert.That(stripRaster, Is.EqualTo(new byte[] { 0, 0, 0, 0, 7, 8, 9, 10 }));
        Assert.That(tileRegionRead.HasErrors(), Is.False);
        Assert.That(tileRegion, Is.EqualTo(new byte[] { 0, 11 }));
    }

    [Test]
    public void ChoppedSingleStripLayout_PreservesPhysicalAndHighLevelReads()
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(4, 4);
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        Assert.That(writer.WriteRawStrip(imageIndex, 0, source).Success, Is.True);
        Assert.That(writer.Flush().HasErrors(), Is.False);
        TiffReadOptions options = new TiffReadOptions();
        options.MemoryPolicy.ChoppedStripByteCount = 4;
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), options).GetImage(0);
        TiffStripLayoutResult layout = image.GetStripLayout();
        TiffRawStripReadResult physical = image.ReadRawStrip(0);
        byte[] scanline = new byte[4];
        byte[] raster = new byte[16];
        TiffDiagnostics scanlineRead = image.ReadScanlineToBuffer(2, scanline);
        TiffDiagnostics rasterRead = image.ReadSourceRasterToBuffer(raster);

        Assert.That(image.GetStripCount(), Is.EqualTo(1));
        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.IsChopped, Is.True);
        Assert.That(layout.Layout.SourceStripCount, Is.EqualTo(1));
        Assert.That(layout.Layout.StripCount, Is.EqualTo(4));
        Assert.That(layout.Layout.Segments[2].FirstRow, Is.EqualTo(2));
        Assert.That(layout.Layout.Segments[2].ByteRange.Offset, Is.EqualTo(layout.Layout.Segments[0].ByteRange.Offset + 8));
        Assert.That(physical.Bytes, Is.EqualTo(source));
        Assert.That(scanlineRead.HasErrors(), Is.False);
        Assert.That(scanline, Is.EqualTo(new byte[] { 9, 10, 11, 12 }));
        Assert.That(rasterRead.HasErrors(), Is.False);
        Assert.That(raster, Is.EqualTo(source));
    }

    [Test]
    public void VolumetricSeparateTiles_EncodeDecodeLookupAndExposePartialDepthEdges()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 3);
        configuration.Depth = 3;
        configuration.PixelFormat.SamplesPerPixel = 2;
        configuration.PixelFormat.Photometric = TiffPhotometric.BlackIsZero;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 2;
        configuration.Layout.AllowSparseSegments = true;
        configuration.Compression = TiffCompression.PackBits;
        byte[] firstPlaneEdge = new byte[] { 41, 0, 0, 0, 0, 0, 0, 0 };
        byte[] secondPlaneEdge = new byte[] { 51, 0, 0, 0, 0, 0, 0, 0 };
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffRawTileWriteResult firstWrite = writer.WriteEncodedTile(imageIndex, 7, firstPlaneEdge);
        TiffRawTileWriteResult secondWrite = writer.WriteEncodedTile(imageIndex, 15, secondPlaneEdge);
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        TiffTileLayoutResult layout = image.GetTileLayout();
        TiffTileLookupResult lookup = image.LookupPyramidTile(0, 2, 2, 2, 1);
        TiffRawTileReadResult raw = image.ReadRawTile(15);
        TiffRawTileReadResult decoded = image.ReadEncodedTile(15);
        TiffRawTileReadResult sparseDecoded = image.ReadEncodedTile(0);

        Assert.That(firstWrite.Success, Is.True);
        Assert.That(secondWrite.Success, Is.True);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(image.GetTileCount(), Is.EqualTo(16));
        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.TilesAcross, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesDown, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesDeep, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesPerPlane, Is.EqualTo(8));
        Assert.That(layout.Layout.Segments[15].SampleIndex, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[15].Width, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[15].Length, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[15].Depth, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[15].IsPartial, Is.True);
        Assert.That(lookup.Success, Is.True);
        Assert.That(lookup.Lookup.TileIndex, Is.EqualTo(15));
        Assert.That(raw.Bytes, Is.Not.EqualTo(secondPlaneEdge));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(secondPlaneEdge));
        Assert.That(sparseDecoded.Success, Is.True);
        Assert.That(sparseDecoded.Bytes, Is.EqualTo(new byte[8]));
    }

    static TiffWriterImageConfiguration CreateGrayConfiguration(int width, int length)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        return configuration;
    }

    static TiffWriterImageConfiguration CreateRgbConfiguration(int width, int length)
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(width, length);
        configuration.PixelFormat.SamplesPerPixel = 3;
        configuration.PixelFormat.BitsPerSample = new int[] { 8 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        return configuration;
    }

    static TiffRegionReadOptions CreateRegionOptions(ulong column, ulong row, ulong width, ulong length, TiffRegionOutput output, int sampleIndex)
    {
        TiffRegionReadOptions options = new TiffRegionReadOptions();
        options.Region.Column = column;
        options.Region.Row = row;
        options.Region.Width = width;
        options.Region.Length = length;
        options.Output = output;
        options.SampleIndex = sampleIndex;
        return options;
    }
}
