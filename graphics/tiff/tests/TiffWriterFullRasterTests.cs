using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterFullRasterTests
{
    [Test]
    public void WriteSourceRaster_DeinterleavesAndCompressesSeparateTiles()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(3, 2, 8);
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        configuration.Compression = TiffCompression.Deflate;
        configuration.Predictor = 2;
        byte[] source = new byte[]
        {
            1, 11, 21, 2, 12, 22, 3, 13, 23,
            4, 14, 24, 5, 15, 25, 6, 16, 26
        };
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        byte[] raster = ReadSourceRaster(image, source.Length);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(image.GetInfo().Compression, Is.EqualTo(TiffCompression.Deflate));
        Assert.That(image.GetInfo().Predictor, Is.EqualTo(2));
        Assert.That(image.GetInfo().PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(raster, Is.EqualTo(source));
    }

    [Test]
    public void WriteRgba8_ConvertsGrayscaleAndAssociatedAlphaSamples()
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(2, 1, 8);
        configuration.PixelFormat.SamplesPerPixel = 2;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteRgba8(imageIndex, new byte[]
        {
            200, 200, 200, 128,
            50, 50, 50, 255
        });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        byte[] source = ReadSourceRaster(image, 4);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(image.GetInfo().ExtraSamples, Is.EqualTo(new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha }));
        Assert.That(source, Is.EqualTo(new byte[] { 100, 128, 50, 255 }));
    }

    [Test]
    public void WriteRgba8_ConvertsIntoCmykSeparateTileLayout()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 1;
        configuration.PixelFormat.SamplesPerPixel = 4;
        configuration.PixelFormat.BitsPerSample = new int[] { 8 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Cmyk;
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 1;
        configuration.Layout.TileDepth = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteRgba8(imageIndex, new byte[]
        {
            0, 255, 255, 255,
            255, 255, 255, 255
        });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        byte[] source = ReadSourceRaster(image, 8);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(image.GetInfo().Photometric, Is.EqualTo(TiffPhotometric.Cmyk));
        Assert.That(image.GetInfo().PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(source, Is.EqualTo(new byte[] { 255, 0, 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void WriteRgba16_RoundTripsBigEndianSixteenBitRgba()
    {
        TiffWriterImageConfiguration configuration = CreateRgbConfiguration(2, 1, 16);
        configuration.ByteOrder = TiffByteOrder.BigEndian;
        configuration.PixelFormat.SamplesPerPixel = 4;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        configuration.Compression = TiffCompression.PackBits;
        ushort[] rgba = new ushort[]
        {
            65535, 0, 32768, 40000,
            1000, 2000, 3000, 4000
        };
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteRgba16(imageIndex, rgba);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffRgba16ReadResult read = file.GetImage(0).ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Samples, Is.EqualTo(rgba));
    }

    [Test]
    public void FullRasterMethods_ReportInvalidDestinationFormatAndSourceCounts()
    {
        TiffWriter legacyWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int legacyIndex = legacyWriter.AddImage(new TiffImageDefinition());
        TiffDiagnostics legacy = legacyWriter.WriteRgba8(legacyIndex, new byte[4]);

        TiffWriter sourceWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int sourceIndex = sourceWriter.AddConfiguredImage(CreateGrayConfiguration(2, 2, 8));
        TiffDiagnostics sourceCount = sourceWriter.WriteSourceRaster(sourceIndex, new byte[3]);
        TiffDiagnostics rgba8Count = sourceWriter.WriteRgba8(sourceIndex, new byte[15]);
        TiffDiagnostics rgba16Count = sourceWriter.WriteRgba16(sourceIndex, new ushort[15]);

        TiffWriterImageConfiguration unsupportedConfiguration = new TiffWriterImageConfiguration();
        unsupportedConfiguration.Width = 1;
        unsupportedConfiguration.Length = 1;
        unsupportedConfiguration.PixelFormat.SamplesPerPixel = 3;
        unsupportedConfiguration.PixelFormat.BitsPerSample = new int[] { 8 };
        unsupportedConfiguration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        unsupportedConfiguration.PixelFormat.Photometric = TiffPhotometric.CieLab;
        TiffWriter unsupportedWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int unsupportedIndex = unsupportedWriter.AddConfiguredImage(unsupportedConfiguration);
        TiffDiagnostics unsupported = unsupportedWriter.WriteRgba8(unsupportedIndex, new byte[] { 1, 2, 3, 255 });

        Assert.That(legacy.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(sourceCount.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(rgba8Count.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(rgba16Count.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(unsupported.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    static TiffWriterImageConfiguration CreateGrayConfiguration(int width, int length, int bitsPerSample)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.BitsPerSample = new int[] { bitsPerSample };
        return configuration;
    }

    static TiffWriterImageConfiguration CreateRgbConfiguration(int width, int length, int bitsPerSample)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.SamplesPerPixel = 3;
        configuration.PixelFormat.BitsPerSample = new int[] { bitsPerSample };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        return configuration;
    }

    static byte[] ReadSourceRaster(TiffImage image, int byteCount)
    {
        byte[] destination = new byte[byteCount];
        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(destination);
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        return destination;
    }
}
