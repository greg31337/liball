using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterRasterOperationTests
{
    [Test]
    public void WriteScanline_WritesRowsInAnyOrderAndReencodesCompressedStrip()
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(3, 2);
        configuration.Layout.RowsPerStrip = 2;
        configuration.Compression = TiffCompression.PackBits;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics secondRow = writer.WriteScanline(imageIndex, 1, new byte[] { 4, 5, 6 });
        TiffDiagnostics firstRow = writer.WriteScanline(imageIndex, 0, new byte[] { 1, 2, 3 });
        TiffDiagnostics flush = writer.Flush();
        byte[] raster = ReadSourceRaster(writer.GetFileBytes(), 6);

        Assert.That(secondRow.Count(), Is.EqualTo(0));
        Assert.That(firstRow.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void WriteRegion_DeinterleavesRowMajorPixelsIntoSeparateTiles()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 2);
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffRegion region = CreateRegion(0, 0, 3, 2);
        byte[] source = new byte[]
        {
            1, 11, 21, 2, 12, 22, 3, 13, 23,
            4, 14, 24, 5, 15, 25, 6, 16, 26
        };

        TiffDiagnostics write = writer.WriteRegion(imageIndex, region, source);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        byte[] raster = new byte[source.Length];
        TiffDiagnostics read = file.GetImage(0).ReadSourceRasterToBuffer(raster);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(file.GetImage(0).GetTileCount(), Is.EqualTo(6));
        Assert.That(raster, Is.EqualTo(source));
    }

    [Test]
    public void WriteSamplePlane_InterleavesPlanesIntoContiguousStrips()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(2, 2);
        configuration.Layout.RowsPerStrip = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics blue = writer.WriteSamplePlane(imageIndex, 2, new byte[] { 21, 22, 23, 24 });
        TiffDiagnostics red = writer.WriteSamplePlane(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        TiffDiagnostics green = writer.WriteSamplePlane(imageIndex, 1, new byte[] { 11, 12, 13, 14 });
        TiffDiagnostics flush = writer.Flush();
        byte[] raster = ReadSourceRaster(writer.GetFileBytes(), 12);

        Assert.That(blue.Count(), Is.EqualTo(0));
        Assert.That(red.Count(), Is.EqualTo(0));
        Assert.That(green.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[] { 1, 11, 21, 2, 12, 22, 3, 13, 23, 4, 14, 24 }));
    }

    [Test]
    public void SampleRegionAndSampleScanline_UpdateOnlySelectedChannels()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 2);
        configuration.Layout.RowsPerStrip = 2;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        writer.WriteRegion(imageIndex, CreateRegion(0, 0, 3, 2), new byte[18]);

        TiffDiagnostics green = writer.WriteSampleRegion(imageIndex, CreateRegion(1, 0, 2, 2), 1, new byte[] { 7, 8, 9, 10 });
        TiffDiagnostics blue = writer.WriteSampleScanline(imageIndex, 1, 2, new byte[] { 11, 12, 13 });
        TiffDiagnostics flush = writer.Flush();
        byte[] raster = ReadSourceRaster(writer.GetFileBytes(), 18);

        Assert.That(green.Count(), Is.EqualTo(0));
        Assert.That(blue.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[]
        {
            0, 0, 0, 0, 7, 0, 0, 8, 0,
            0, 0, 11, 0, 9, 12, 0, 10, 13
        }));
    }

    [Test]
    public void WriteRegion_LeavesUnrelatedConfiguredTilesSparse()
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(4, 4);
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        configuration.Layout.AllowSparseSegments = true;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteRegion(imageIndex, CreateRegion(0, 0, 2, 2), new byte[] { 1, 2, 3, 4 });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        TiffTileLayoutResult layout = image.GetTileLayout();
        byte[] raster = new byte[16];
        TiffDiagnostics read = image.ReadSourceRasterToBuffer(raster);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(layout.Layout.Segments[0].IsSparse, Is.False);
        Assert.That(layout.Layout.Segments[1].IsSparse, Is.True);
        Assert.That(layout.Layout.Segments[2].IsSparse, Is.True);
        Assert.That(layout.Layout.Segments[3].IsSparse, Is.True);
        Assert.That(raster, Is.EqualTo(new byte[]
        {
            1, 2, 0, 0,
            3, 4, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        }));
    }

    [Test]
    public void RasterOperations_RejectLegacyVolumetricPackedAndMismatchedRequests()
    {
        TiffWriter legacyWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int legacyIndex = legacyWriter.AddImage(new TiffImageDefinition());
        TiffDiagnostics legacy = legacyWriter.WriteScanline(legacyIndex, 0, new byte[0]);

        TiffWriterImageConfiguration volumeConfiguration = CreateGrayConfiguration(2, 2);
        volumeConfiguration.Depth = 2;
        volumeConfiguration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        volumeConfiguration.Layout.TileWidth = 2;
        volumeConfiguration.Layout.TileLength = 2;
        volumeConfiguration.Layout.TileDepth = 1;
        TiffWriter volumeWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int volumeIndex = volumeWriter.AddConfiguredImage(volumeConfiguration);
        TiffDiagnostics volume = volumeWriter.WriteRegion(volumeIndex, CreateRegion(0, 0, 2, 2), new byte[4]);

        TiffWriterImageConfiguration packedConfiguration = CreateGrayConfiguration(8, 1);
        packedConfiguration.PixelFormat.BitsPerSample = new int[] { 1 };
        TiffWriter packedWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int packedIndex = packedWriter.AddConfiguredImage(packedConfiguration);
        TiffDiagnostics packed = packedWriter.WriteScanline(packedIndex, 0, new byte[] { 170 });

        TiffWriter mismatchWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int mismatchIndex = mismatchWriter.AddConfiguredImage(CreateGrayConfiguration(2, 2));
        TiffDiagnostics mismatch = mismatchWriter.WriteSamplePlane(mismatchIndex, 0, new byte[] { 1, 2, 3 });
        TiffDiagnostics bounds = mismatchWriter.WriteRegion(mismatchIndex, CreateRegion(1, 1, 2, 2), new byte[4]);

        Assert.That(legacy.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(volume.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(packed.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(mismatch.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(bounds.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
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
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.SamplesPerPixel = 3;
        configuration.PixelFormat.BitsPerSample = new int[] { 8 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        return configuration;
    }

    static TiffRegion CreateRegion(ulong column, ulong row, ulong width, ulong length)
    {
        TiffRegion region = new TiffRegion();
        region.Column = column;
        region.Row = row;
        region.Width = width;
        region.Length = length;
        return region;
    }

    static byte[] ReadSourceRaster(byte[] fileBytes, int byteCount)
    {
        TiffImage image = TiffFile.OpenRead(fileBytes).GetImage(0);
        byte[] raster = new byte[byteCount];
        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(raster);
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        return raster;
    }
}
