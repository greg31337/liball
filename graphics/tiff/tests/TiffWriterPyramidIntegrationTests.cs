using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterPyramidIntegrationTests
{
    [Test]
    public void AddPyramidLevel_WritesOrderedClassicLevelsWithIndependentLayoutsAndCompression()
    {
        ITiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        TiffWriterImageConfiguration baseConfiguration = CreateGrayConfiguration(8, 8, 8);
        TiffWriterImageConfiguration firstConfiguration = CreateGrayConfiguration(4, 4, 8);
        firstConfiguration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        firstConfiguration.Layout.TileWidth = 2;
        firstConfiguration.Layout.TileLength = 2;
        firstConfiguration.Layout.TileDepth = 1;
        firstConfiguration.Compression = TiffCompression.PackBits;
        TiffWriterImageConfiguration secondConfiguration = CreateGrayConfiguration(2, 2, 16);
        secondConfiguration.Layout.RowsPerStrip = 1;
        secondConfiguration.Compression = TiffCompression.Deflate;
        int imageIndex = writer.AddConfiguredImage(baseConfiguration);
        int firstIndex = writer.AddPyramidLevel(imageIndex, firstConfiguration);
        int secondIndex = writer.AddPyramidLevel(imageIndex, secondConfiguration);
        byte[] firstRgba = CreateGrayRgba8(16);
        ushort[] secondRgba = CreateGrayRgba16();

        TiffDiagnostics baseWrite = writer.WriteSourceRaster(imageIndex, CreateBytes(64));
        TiffDiagnostics firstWrite = writer.WriteSubImageRgba8(imageIndex, firstIndex, firstRgba);
        TiffDiagnostics secondWrite = writer.WriteSubImageRgba16(imageIndex, secondIndex, secondRgba);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffImage first = image.GetPyramidLevel(1);
        TiffImage second = image.GetPyramidLevel(2);
        byte[] firstSource = new byte[16];
        TiffDiagnostics firstRead = first.ReadSourceRasterToBuffer(firstSource);
        TiffRgba16ReadResult secondRead = second.ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(firstIndex, Is.EqualTo(0));
        Assert.That(secondIndex, Is.EqualTo(1));
        Assert.That(baseWrite.Count(), Is.EqualTo(0));
        Assert.That(firstWrite.Count(), Is.EqualTo(0));
        Assert.That(secondWrite.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(image.GetDirectory().FindEntry(TiffTagId.SubIFDs)!.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(image.GetPyramidLevelCount(), Is.EqualTo(3));
        Assert.That(first.GetInfo().Width, Is.EqualTo(4));
        Assert.That(first.GetInfo().Length, Is.EqualTo(4));
        Assert.That(first.GetInfo().IsReducedResolution, Is.True);
        Assert.That(first.GetInfo().IsTiled, Is.True);
        Assert.That(first.GetInfo().TileWidth, Is.EqualTo(2));
        Assert.That(first.GetInfo().Compression, Is.EqualTo(TiffCompression.PackBits));
        Assert.That(firstRead.Count(), Is.EqualTo(0));
        Assert.That(firstSource, Is.EqualTo(CreateGrayValues(16)));
        Assert.That(second.GetInfo().Width, Is.EqualTo(2));
        Assert.That(second.GetInfo().Length, Is.EqualTo(2));
        Assert.That(second.GetInfo().IsReducedResolution, Is.True);
        Assert.That(second.GetInfo().IsTiled, Is.False);
        Assert.That(second.GetInfo().RowsPerStrip, Is.EqualTo(1));
        Assert.That(second.GetInfo().Compression, Is.EqualTo(TiffCompression.Deflate));
        Assert.That(secondRead.Success, Is.True);
        Assert.That(secondRead.Samples, Is.EqualTo(secondRgba));
    }

    [Test]
    public void AddPyramidLevel_WritesBigTiffSubIfdOffsetsAndSourceRaster()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.BigTiff;
        ITiffWriter writer = new TiffFactory().CreateWriter(options);
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(2, 2, 8));
        int levelIndex = writer.AddPyramidLevel(imageIndex, CreateGrayConfiguration(1, 1, 8));

        writer.WriteSourceRaster(imageIndex, new byte[] { 1, 2, 3, 4 });
        TiffDiagnostics levelWrite = writer.WriteSubImageSourceRaster(imageIndex, levelIndex, new byte[] { 99 });
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffImage level = image.GetPyramidLevel(1);
        byte[] source = new byte[1];
        TiffDiagnostics read = level.ReadSourceRasterToBuffer(source);

        Assert.That(levelWrite.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(image.GetDirectory().FindEntry(TiffTagId.SubIFDs)!.DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(level.GetInfo().NewSubfileType & 1, Is.EqualTo(1));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(source, Is.EqualTo(new byte[] { 99 }));
    }

    [Test]
    public void AddConfiguredSubImage_WritesAChildWithoutClassifyingItAsAnOverview()
    {
        ITiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(2, 2, 8));
        TiffWriterImageConfiguration childConfiguration = CreateGrayConfiguration(2, 2, 8);
        childConfiguration.Compression = TiffCompression.Lzw;
        int childIndex = writer.AddConfiguredSubImage(imageIndex, childConfiguration);

        writer.WriteSourceRaster(imageIndex, new byte[] { 1, 2, 3, 4 });
        TiffDiagnostics childWrite = writer.WriteSubImageSourceRaster(imageIndex, childIndex, new byte[] { 7, 8, 9, 10 });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        TiffImage child = image.GetSubImage(childIndex);
        byte[] source = new byte[4];
        TiffDiagnostics read = child.ReadSourceRasterToBuffer(source);

        Assert.That(childWrite.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(image.GetSubImageCount(), Is.EqualTo(1));
        Assert.That(image.GetPyramidLevelCount(), Is.EqualTo(1));
        Assert.That(child.GetInfo().IsReducedResolution, Is.False);
        Assert.That(child.GetInfo().Compression, Is.EqualTo(TiffCompression.Lzw));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(source, Is.EqualTo(new byte[] { 7, 8, 9, 10 }));
    }

    [Test]
    public void AddPyramidLevel_RejectsUnconfiguredBasesAndUnorderedDimensions()
    {
        ITiffWriter rawWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int rawIndex = rawWriter.AddImage(new TiffImageDefinition());
        int rawLevel = rawWriter.AddPyramidLevel(rawIndex, CreateGrayConfiguration(1, 1, 8));

        ITiffWriter equalWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int equalBase = equalWriter.AddConfiguredImage(CreateGrayConfiguration(4, 4, 8));
        int equalLevel = equalWriter.AddPyramidLevel(equalBase, CreateGrayConfiguration(4, 4, 8));

        ITiffWriter unorderedWriter = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int unorderedBase = unorderedWriter.AddConfiguredImage(CreateGrayConfiguration(8, 8, 8));
        int firstLevel = unorderedWriter.AddPyramidLevel(unorderedBase, CreateGrayConfiguration(4, 4, 8));
        int unorderedLevel = unorderedWriter.AddPyramidLevel(unorderedBase, CreateGrayConfiguration(5, 2, 8));

        Assert.That(rawLevel, Is.EqualTo(-1));
        Assert.That(rawWriter.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(equalLevel, Is.EqualTo(-1));
        Assert.That(equalWriter.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(firstLevel, Is.EqualTo(0));
        Assert.That(unorderedLevel, Is.EqualTo(-1));
        Assert.That(unorderedWriter.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffWriterImageConfiguration CreateGrayConfiguration(int width, int length, int bitsPerSample)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.BitsPerSample = new int[] { bitsPerSample };
        return configuration;
    }

    static byte[] CreateBytes(int count)
    {
        byte[] values = new byte[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = (byte)(index + 1);
            index = index + 1;
        }

        return values;
    }

    static byte[] CreateGrayValues(int count)
    {
        byte[] values = new byte[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = (byte)(index + 10);
            index = index + 1;
        }

        return values;
    }

    static byte[] CreateGrayRgba8(int pixelCount)
    {
        byte[] gray = CreateGrayValues(pixelCount);
        byte[] rgba = new byte[pixelCount * 4];
        int index = 0;
        while (index < pixelCount)
        {
            rgba[index * 4] = gray[index];
            rgba[index * 4 + 1] = gray[index];
            rgba[index * 4 + 2] = gray[index];
            rgba[index * 4 + 3] = 255;
            index = index + 1;
        }

        return rgba;
    }

    static ushort[] CreateGrayRgba16()
    {
        ushort[] values = new ushort[16];
        int index = 0;
        while (index < 4)
        {
            ushort gray = (ushort)((index + 1) * 1000);
            values[index * 4] = gray;
            values[index * 4 + 1] = gray;
            values[index * 4 + 2] = gray;
            values[index * 4 + 3] = 65535;
            index = index + 1;
        }

        return values;
    }
}
