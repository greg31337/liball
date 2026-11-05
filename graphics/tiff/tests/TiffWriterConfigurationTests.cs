using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterConfigurationTests
{
    [Test]
    public void AddConfiguredImage_GeneratesStripTagsAndRoundTripsPixelConfiguration()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 2;
        configuration.Depth = 1;
        configuration.ByteOrder = TiffByteOrder.BigEndian;
        configuration.Compression = TiffCompression.Lzw;
        configuration.Predictor = 2;
        configuration.PixelFormat.SamplesPerPixel = 4;
        configuration.PixelFormat.BitsPerSample = new int[] { 8 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        configuration.Layout.Kind = TiffWriterLayoutKind.Strips;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        configuration.Layout.RowsPerStrip = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());

        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffRawStripWriteResult firstWrite = writer.WriteEncodedStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        TiffRawStripWriteResult secondWrite = writer.WriteEncodedStrip(imageIndex, 1, new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 });
        TiffDiagnostics flushDiagnostics = writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffValidationResult validation = TiffValidator.Validate(fileBytes);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();

        Assert.That(imageIndex, Is.EqualTo(0));
        Assert.That(firstWrite.Success, Is.True);
        Assert.That(secondWrite.Success, Is.True);
        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(validation.Success, Is.True);
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Length, Is.EqualTo(2));
        Assert.That(info.ImageDepth, Is.EqualTo(1));
        Assert.That(info.SamplesPerPixel, Is.EqualTo(4));
        Assert.That(info.BitsPerSample, Is.EqualTo(new int[] { 8, 8, 8, 8 }));
        Assert.That(info.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger }));
        Assert.That(info.Photometric, Is.EqualTo(TiffPhotometric.Rgb));
        Assert.That(info.ExtraSamples, Is.EqualTo(new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha }));
        Assert.That(info.Compression, Is.EqualTo(TiffCompression.Lzw));
        Assert.That(info.Predictor, Is.EqualTo(2));
        Assert.That(info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Contiguous));
        Assert.That(info.RowsPerStrip, Is.EqualTo(1));
        Assert.That(image.GetStripCount(), Is.EqualTo(2));
        Assert.That(image.GetTag(TiffTagId.StripOffsets), Is.Not.Null);
        Assert.That(image.GetTag(TiffTagId.StripByteCounts), Is.Not.Null);
    }

    [Test]
    public void AddConfiguredImage_ComputesVolumetricSeparateTileLayoutAndAllowsSparseTiles()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 5;
        configuration.Length = 3;
        configuration.Depth = 2;
        configuration.PixelFormat.SamplesPerPixel = 2;
        configuration.PixelFormat.BitsPerSample = new int[] { 16 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.BlackIsZero;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Layout.TileWidth = 4;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        configuration.Layout.AllowSparseSegments = true;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());

        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffRawTileWriteResult writeResult = writer.WriteRawTile(imageIndex, 0, new byte[] { 9, 8 });
        TiffDiagnostics flushDiagnostics = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffTileLayoutResult layout = image.GetTileLayout();

        Assert.That(imageIndex, Is.EqualTo(0));
        Assert.That(writeResult.Success, Is.True);
        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(file.Success, Is.True);
        Assert.That(info.ImageDepth, Is.EqualTo(2));
        Assert.That(info.TileWidth, Is.EqualTo(4));
        Assert.That(info.TileLength, Is.EqualTo(2));
        Assert.That(info.TileDepth, Is.EqualTo(1));
        Assert.That(info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(image.GetTileCount(), Is.EqualTo(16));
        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.TilesAcross, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesDown, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesDeep, Is.EqualTo(2));
        Assert.That(layout.Layout.TilesPerPlane, Is.EqualTo(8));
        Assert.That(layout.Layout.Segments[0].IsSparse, Is.False);
        Assert.That(layout.Layout.Segments[1].IsSparse, Is.True);
    }

    [Test]
    public void AddConfiguredImage_UsesImageLengthForDefaultStripHeight()
    {
        TiffWriterImageConfiguration configuration = CreateGrayscaleStripConfiguration();
        configuration.Length = 3;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());

        int imageIndex = writer.AddConfiguredImage(configuration);
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
        TiffDiagnostics diagnostics = writer.Flush();
        TiffImageInfo info = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0).GetInfo();

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(info.RowsPerStrip, Is.EqualTo(3));
    }

    [Test]
    public void Flush_RejectsUnwrittenConfiguredSegmentsWhenSparseStorageIsDisabled()
    {
        TiffWriterImageConfiguration configuration = CreateGrayscaleStripConfiguration();
        configuration.Layout.RowsPerStrip = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());

        int imageIndex = writer.AddConfiguredImage(configuration);
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2 });
        TiffDiagnostics diagnostics = writer.Flush();

        Assert.That(imageIndex, Is.EqualTo(0));
        Assert.That(writer.GetSuccess(), Is.False);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(ContainsDiagnosticForTag(diagnostics, TiffTagId.StripByteCounts), Is.True);
    }

    [Test]
    public void AddConfiguredImage_RejectsInvalidRequiredConfigurationBeforeAddingImage()
    {
        TiffWriterImageConfiguration configuration = CreateGrayscaleStripConfiguration();
        configuration.Width = 0;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());

        int imageIndex = writer.AddConfiguredImage(configuration);

        Assert.That(imageIndex, Is.EqualTo(-1));
        Assert.That(writer.GetSuccess(), Is.False);
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(ContainsDiagnosticForTag(writer.GetDiagnostics(), TiffTagId.ImageWidth), Is.True);
    }

    [Test]
    public void ConfiguredWriter_RejectsMixedByteOrdersInOneFile()
    {
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        TiffWriterImageConfiguration first = CreateGrayscaleStripConfiguration();
        first.ByteOrder = TiffByteOrder.BigEndian;
        TiffWriterImageConfiguration second = CreateGrayscaleStripConfiguration();
        second.ByteOrder = TiffByteOrder.LittleEndian;

        int firstIndex = writer.AddConfiguredImage(first);
        int secondIndex = writer.AddConfiguredImage(second);

        Assert.That(firstIndex, Is.EqualTo(0));
        Assert.That(secondIndex, Is.EqualTo(-1));
        Assert.That(writer.GetByteOrder(), Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void ConfiguredWriter_RejectsWrongOutOfRangeAndUnpermittedSparseSegments()
    {
        TiffWriter wrongKindWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int wrongKindImage = wrongKindWriter.AddConfiguredImage(CreateGrayscaleStripConfiguration());
        TiffWriter outOfRangeWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int outOfRangeImage = outOfRangeWriter.AddConfiguredImage(CreateGrayscaleStripConfiguration());
        TiffWriter emptyWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int emptyImage = emptyWriter.AddConfiguredImage(CreateGrayscaleStripConfiguration());

        TiffRawTileWriteResult wrongKind = wrongKindWriter.WriteRawTile(wrongKindImage, 0, new byte[] { 1 });
        TiffRawStripWriteResult outOfRange = outOfRangeWriter.WriteRawStrip(outOfRangeImage, 1, new byte[] { 1 });
        TiffRawStripWriteResult empty = emptyWriter.WriteRawStrip(emptyImage, 0, new byte[0]);

        Assert.That(wrongKind.Success, Is.False);
        Assert.That(wrongKind.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(outOfRange.Success, Is.False);
        Assert.That(outOfRange.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(empty.Success, Is.False);
        Assert.That(empty.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffWriterImageConfiguration CreateGrayscaleStripConfiguration()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 2;
        return configuration;
    }

    static bool ContainsDiagnosticForTag(TiffDiagnostics diagnostics, int tagId)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).TagId == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
