using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicWriterRoundTripTests
{
    [TestCase("RawStrip")]
    [TestCase("EncodedStrip")]
    [TestCase("RawTile")]
    [TestCase("EncodedTile")]
    [TestCase("Scanline")]
    [TestCase("Region")]
    [TestCase("SamplePlane")]
    [TestCase("SourceRaster")]
    public void RasterWriteOperations_RoundTripThroughPublicImageApi(string operation)
    {
        byte[] expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(4, 2, 8, TiffSampleFormat.UnsignedInteger);
        configuration.Layout.RowsPerStrip = 1;
        if (operation == "RawTile" || operation == "EncodedTile")
        {
            configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
            configuration.Layout.TileWidth = 2;
            configuration.Layout.TileLength = 1;
            configuration.Layout.TileDepth = 1;
        }

        if (operation == "EncodedStrip")
        {
            configuration.Compression = TiffCompression.PackBits;
        }

        if (operation == "EncodedTile")
        {
            configuration.Compression = TiffCompression.Deflate;
        }

        ITiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        WriteRasterOperation(writer, imageIndex, operation, expected);

        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        ITiffImage image = file.GetImage(0);
        byte[] actual = new byte[expected.Length];
        TiffDiagnostics read = image.ReadSourceRasterToBuffer(actual);

        Assert.That(flush.HasErrors(), Is.False, operation + ": " + flush);
        Assert.That(file.Success, Is.True, operation + ": " + file.GetDiagnostics());
        Assert.That(read.HasErrors(), Is.False, operation + ": " + read);
        Assert.That(actual, Is.EqualTo(expected), operation);
        if (operation == "EncodedStrip")
        {
            Assert.That(image.ReadEncodedStrip(0).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(image.ReadRawStrip(0).Bytes, Is.Not.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }

        if (operation == "EncodedTile")
        {
            Assert.That(image.ReadEncodedTile(0).Bytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(image.ReadRawTile(0).Bytes, Is.Not.EqualTo(new byte[] { 1, 2 }));
        }
    }

    [TestCase(TiffByteOrder.LittleEndian, TiffCompression.None, 1, 8, TiffSampleFormat.UnsignedInteger)]
    [TestCase(TiffByteOrder.BigEndian, TiffCompression.PackBits, 1, 16, TiffSampleFormat.UnsignedInteger)]
    [TestCase(TiffByteOrder.LittleEndian, TiffCompression.Lzw, 2, 32, TiffSampleFormat.UnsignedInteger)]
    [TestCase(TiffByteOrder.BigEndian, TiffCompression.Deflate, 2, 64, TiffSampleFormat.UnsignedInteger)]
    [TestCase(TiffByteOrder.BigEndian, TiffCompression.Deflate, 3, 32, TiffSampleFormat.IeeeFloatingPoint)]
    public void SourceRaster_RoundTripsByteOrderCompressionAndPredictorCombinations(TiffByteOrder byteOrder, TiffCompression compression, int predictor, int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        byte[] source = CreateSourceSamples(bitsPerSample, sampleFormat);
        TiffWriteOptions options = new TiffWriteOptions();
        options.ByteOrder = byteOrder;
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(3, 1, bitsPerSample, sampleFormat);
        configuration.Compression = compression;
        configuration.Predictor = predictor;
        ITiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        ITiffImage image = file.GetImage(0);
        byte[] actual = new byte[source.Length];
        TiffDiagnostics read = image.ReadSourceRasterToBuffer(actual);
        TiffRgba8ReadResult rgba = image.ReadRgba8(new TiffRgbaReadOptions());

        Assert.That(write.HasErrors(), Is.False, write.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(byteOrder));
        Assert.That(image.GetInfo().Compression, Is.EqualTo(compression));
        Assert.That(image.GetInfo().Predictor, Is.EqualTo(predictor));
        Assert.That(read.HasErrors(), Is.False, read.ToString());
        Assert.That(actual, Is.EqualTo(source));
        Assert.That(rgba.Success, Is.True, rgba.Diagnostics.ToString());
    }

    [Test]
    public void Rgba8AndRgba16_RoundTripSeparateTilesAndPredictorStrips()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.ByteOrder = TiffByteOrder.BigEndian;
        ITiffWriter writer = TiffApi.Factory.CreateWriter(options);
        TiffWriterImageConfiguration rgba8Configuration = CreateRgbaConfiguration(2, 2, 8);
        rgba8Configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        rgba8Configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        rgba8Configuration.Layout.TileWidth = 2;
        rgba8Configuration.Layout.TileLength = 1;
        rgba8Configuration.Layout.TileDepth = 1;
        rgba8Configuration.Compression = TiffCompression.PackBits;
        TiffWriterImageConfiguration rgba16Configuration = CreateRgbaConfiguration(2, 1, 16);
        rgba16Configuration.Layout.RowsPerStrip = 1;
        rgba16Configuration.Compression = TiffCompression.Deflate;
        rgba16Configuration.Predictor = 2;
        int rgba8Index = writer.AddConfiguredImage(rgba8Configuration);
        int rgba16Index = writer.AddConfiguredImage(rgba16Configuration);
        byte[] expected8 = new byte[]
        {
            1, 2, 3, 64,
            4, 5, 6, 128,
            7, 8, 9, 192,
            10, 11, 12, 255
        };
        ushort[] expected16 = new ushort[]
        {
            1000, 2000, 3000, 4000,
            5000, 6000, 7000, 8000
        };

        TiffDiagnostics write8 = writer.WriteRgba8(rgba8Index, expected8);
        TiffDiagnostics write16 = writer.WriteRgba16(rgba16Index, expected16);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        TiffRgba8ReadResult actual8 = file.GetImage(0).ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba16ReadResult actual16 = file.GetImage(1).ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(write8.HasErrors(), Is.False, write8.ToString());
        Assert.That(write16.HasErrors(), Is.False, write16.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(file.GetImage(0).GetInfo().PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(file.GetImage(0).GetInfo().IsTiled, Is.True);
        Assert.That(file.GetImage(1).GetInfo().Predictor, Is.EqualTo(2));
        Assert.That(actual8.Success, Is.True, actual8.Diagnostics.ToString());
        Assert.That(actual8.Bytes, Is.EqualTo(expected8));
        Assert.That(actual16.Success, Is.True, actual16.Diagnostics.ToString());
        Assert.That(actual16.Samples, Is.EqualTo(expected16));
    }

    [TestCase(TiffFileKind.Classic, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.Classic, TiffByteOrder.BigEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.LittleEndian)]
    [TestCase(TiffFileKind.BigTiff, TiffByteOrder.BigEndian)]
    public void ContainerTrees_RoundTripMultiPageSubIfdAndPyramidWorkflows(TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        options.ByteOrder = byteOrder;
        options.AutoUpgradeToBigTiff = true;
        ITiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int baseIndex = writer.AddConfiguredImage(CreateGrayConfiguration(4, 4, 8, TiffSampleFormat.UnsignedInteger));
        int pageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(2, 1, 8, TiffSampleFormat.UnsignedInteger));
        int childIndex = writer.AddConfiguredSubImage(baseIndex, CreateGrayConfiguration(4, 4, 8, TiffSampleFormat.UnsignedInteger));
        int overviewIndex = writer.AddPyramidLevel(baseIndex, CreateGrayConfiguration(2, 2, 8, TiffSampleFormat.UnsignedInteger));
        byte[] baseSource = CreateSequentialBytes(16, 1);
        byte[] pageSource = new byte[] { 90, 91 };
        byte[] childSource = CreateSequentialBytes(16, 20);
        byte[] overviewSource = new byte[] { 70, 71, 72, 73 };

        TiffDiagnostics baseWrite = writer.WriteSourceRaster(baseIndex, baseSource);
        TiffDiagnostics pageWrite = writer.WriteSourceRaster(pageIndex, pageSource);
        TiffDiagnostics childWrite = writer.WriteSubImageSourceRaster(baseIndex, childIndex, childSource);
        TiffDiagnostics overviewWrite = writer.WriteSubImageSourceRaster(baseIndex, overviewIndex, overviewSource);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        ITiffImage baseImage = file.GetImage(0);
        ITiffImage pageImage = file.GetImage(1);
        ITiffImage childImage = baseImage.GetSubImage(childIndex);
        ITiffImage overviewImage = baseImage.GetPyramidLevel(1);

        Assert.That(baseWrite.HasErrors(), Is.False, baseWrite.ToString());
        Assert.That(pageWrite.HasErrors(), Is.False, pageWrite.ToString());
        Assert.That(childWrite.HasErrors(), Is.False, childWrite.ToString());
        Assert.That(overviewWrite.HasErrors(), Is.False, overviewWrite.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(writer.GetOptions().AutoUpgradeToBigTiff, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(fileKind));
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(byteOrder));
        Assert.That(file.GetImageCount(), Is.EqualTo(2));
        Assert.That(baseImage.GetSubImageCount(), Is.EqualTo(2));
        Assert.That(baseImage.GetPyramidLevelCount(), Is.EqualTo(2));
        AssertSourceRaster(baseImage, baseSource);
        AssertSourceRaster(pageImage, pageSource);
        AssertSourceRaster(childImage, childSource);
        AssertSourceRaster(overviewImage, overviewSource);
        Assert.That(overviewImage.GetInfo().IsReducedResolution, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AutoUpgradeSetting_RoundTripsSmallClassicWorkflowWithoutChangingContainer(bool autoUpgrade)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.Classic;
        options.AutoUpgradeToBigTiff = autoUpgrade;
        ITiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1, 8, TiffSampleFormat.UnsignedInteger));

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, new byte[] { 123 });
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());

        Assert.That(write.HasErrors(), Is.False, write.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(writer.GetOptions().AutoUpgradeToBigTiff, Is.EqualTo(autoUpgrade));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        AssertSourceRaster(file.GetImage(0), new byte[] { 123 });
    }

    static void WriteRasterOperation(ITiffWriter writer, int imageIndex, string operation, byte[] source)
    {
        if (operation == "RawStrip" || operation == "EncodedStrip")
        {
            int stripIndex = 0;
            while (stripIndex < 2)
            {
                ReadOnlySpan<byte> strip = source.AsSpan(stripIndex * 4, 4);
                TiffRawStripWriteResult result = operation == "RawStrip"
                    ? writer.WriteRawStrip(imageIndex, stripIndex, strip)
                    : writer.WriteEncodedStrip(imageIndex, stripIndex, strip);
                Assert.That(result.Success, Is.True, operation + " " + stripIndex + ": " + result.Diagnostics);
                stripIndex = stripIndex + 1;
            }

            return;
        }

        if (operation == "RawTile" || operation == "EncodedTile")
        {
            int tileIndex = 0;
            while (tileIndex < 4)
            {
                ReadOnlySpan<byte> tile = source.AsSpan(tileIndex * 2, 2);
                TiffRawTileWriteResult result = operation == "RawTile"
                    ? writer.WriteRawTile(imageIndex, tileIndex, tile)
                    : writer.WriteEncodedTile(imageIndex, tileIndex, tile);
                Assert.That(result.Success, Is.True, operation + " " + tileIndex + ": " + result.Diagnostics);
                tileIndex = tileIndex + 1;
            }

            return;
        }

        if (operation == "Scanline")
        {
            Assert.That(writer.WriteScanline(imageIndex, 1, source.AsSpan(4, 4)).HasErrors(), Is.False);
            Assert.That(writer.WriteScanline(imageIndex, 0, source.AsSpan(0, 4)).HasErrors(), Is.False);
            return;
        }

        if (operation == "Region")
        {
            TiffRegion region = new TiffRegion();
            region.Width = 4;
            region.Length = 2;
            Assert.That(writer.WriteRegion(imageIndex, region, source).HasErrors(), Is.False);
            return;
        }

        if (operation == "SamplePlane")
        {
            Assert.That(writer.WriteSamplePlane(imageIndex, 0, source).HasErrors(), Is.False);
            return;
        }

        Assert.That(operation, Is.EqualTo("SourceRaster"));
        Assert.That(writer.WriteSourceRaster(imageIndex, source).HasErrors(), Is.False);
    }

    static TiffWriterImageConfiguration CreateGrayConfiguration(int width, int length, int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.BitsPerSample = new int[] { bitsPerSample };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { sampleFormat };
        return configuration;
    }

    static TiffWriterImageConfiguration CreateRgbaConfiguration(int width, int length, int bitsPerSample)
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(width, length, bitsPerSample, TiffSampleFormat.UnsignedInteger);
        configuration.PixelFormat.SamplesPerPixel = 4;
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        return configuration;
    }

    static byte[] CreateSourceSamples(int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            float[] values = new float[] { 0.25f, 0.5f, 0.75f };
            byte[] bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        if (bitsPerSample == 8)
        {
            return new byte[] { 10, 20, 30 };
        }

        if (bitsPerSample == 16)
        {
            ushort[] values = new ushort[] { 1000, 2000, 3000 };
            byte[] bytes = new byte[values.Length * 2];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        if (bitsPerSample == 32)
        {
            uint[] values = new uint[] { 100000, 200000, 300000 };
            byte[] bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        ulong[] wideValues = new ulong[] { 10000000000, 20000000000, 30000000000 };
        byte[] wideBytes = new byte[wideValues.Length * 8];
        Buffer.BlockCopy(wideValues, 0, wideBytes, 0, wideBytes.Length);
        return wideBytes;
    }

    static byte[] CreateSequentialBytes(int count, int first)
    {
        byte[] values = new byte[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = (byte)(first + index);
            index = index + 1;
        }

        return values;
    }

    static void AssertSourceRaster(ITiffImage image, byte[] expected)
    {
        byte[] actual = new byte[expected.Length];
        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(actual);
        Assert.That(diagnostics.HasErrors(), Is.False, diagnostics.ToString());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
