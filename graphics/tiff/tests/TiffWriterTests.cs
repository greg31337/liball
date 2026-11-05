using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterTests
{
    [Test]
    public void Flush_WritesClassicImageWithRawStripAndRoundTrips()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2)));

        TiffRawStripWriteResult writeResult = writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
        TiffDiagnostics flushDiagnostics = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(image.GetInfo().Width, Is.EqualTo(3));
        Assert.That(image.GetInfo().Length, Is.EqualTo(2));
        Assert.That(image.GetStripCount(), Is.EqualTo(1));
        Assert.That(image.GetTag(TiffTagId.StripOffsets), Is.Not.Null);
        Assert.That(image.GetTag(TiffTagId.StripByteCounts), Is.Not.Null);
        Assert.That(strip.Success, Is.True);
        Assert.That(strip.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void WriteTag_ReplacesImageTagBeforeFlush()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2)));

        writer.WriteTag(imageIndex, TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 4));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImage(0).GetInfo().Width, Is.EqualTo(4));
        Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public void AddSubImage_WritesSubIfdPage()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(8, 8)));

        int subImageIndex = writer.AddSubImage(imageIndex, TiffImageDefinition.Create(CreateStripTags(4, 4)));
        writer.WriteRawStrip(imageIndex, 0, CreateBytes(64));
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffImage subImage = image.GetSubImage(subImageIndex);

        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(image.GetSubImageCount(), Is.EqualTo(1));
        Assert.That(subImage.Success, Is.True);
        Assert.That(subImage.GetInfo().Width, Is.EqualTo(4));
        Assert.That(subImage.GetInfo().Length, Is.EqualTo(4));
    }

    [Test]
    public void Flush_WritesBigTiffWhenConfigured()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.BigTiff;
        TiffWriter writer = TiffWriter.Create(options);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 9, 8, 7, 6 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
    }

    [Test]
    public void Flush_AppliesBaselineDefaultsForMinimalSingleStripImage()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateMinimalStripTags(8, 1)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 170 });
        TiffDiagnostics diagnostics = writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffValidationResult validation = TiffValidator.Validate(fileBytes);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffTagDefaultResult compression = image.GetTagDefaulted(TiffTagId.Compression);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(validation.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(image.GetTag(TiffTagId.BitsPerSample), Is.Not.Null);
        Assert.That(image.GetTag(TiffTagId.RowsPerStrip), Is.Not.Null);
        Assert.That(info.BitsPerSample, Is.EqualTo(new int[] { 1 }));
        Assert.That(info.RowsPerStrip, Is.EqualTo(uint.MaxValue));
        Assert.That(info.SamplesPerPixel, Is.EqualTo(1));
        Assert.That(info.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(compression.Found, Is.True);
        Assert.That(compression.UsedDefault, Is.True);
    }

    [Test]
    public void Flush_ValidateBeforeWriteReportsMissingNonDefaultedBaselineTag()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateSizeOnlyTags(8, 1)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });
        TiffDiagnostics diagnostics = writer.Flush();

        Assert.That(writer.Success, Is.False);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(ContainsDiagnosticForTag(diagnostics, TiffTagId.PhotometricInterpretation), Is.True);
    }

    [Test]
    public void Flush_DoesNotDefaultRowsPerStripForMultipleStrips()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateMinimalStripTags(8, 2)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });
        writer.WriteRawStrip(imageIndex, 1, new byte[] { 2 });
        TiffDiagnostics diagnostics = writer.Flush();

        Assert.That(writer.Success, Is.False);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(ContainsDiagnosticForTag(diagnostics, TiffTagId.RowsPerStrip), Is.True);
    }

    [Test]
    public void SelectFileKindForWrite_UpgradesClassicWhenAutoUpgradeIsEnabled()
    {
        ulong oversizedClassicByteCount = (ulong)uint.MaxValue + 1UL;

        TiffFileKind selected = TiffWriter.SelectFileKindForWrite(TiffFileKind.Classic, true, oversizedClassicByteCount);

        Assert.That(TiffWriter.ClassicOffsetsWouldOverflow((ulong)uint.MaxValue), Is.False);
        Assert.That(TiffWriter.ClassicOffsetsWouldOverflow(oversizedClassicByteCount), Is.True);
        Assert.That(selected, Is.EqualTo(TiffFileKind.BigTiff));
    }

    [Test]
    public void SelectFileKindForWrite_KeepsClassicWhenAutoUpgradeIsDisabled()
    {
        ulong oversizedClassicByteCount = (ulong)uint.MaxValue + 1UL;

        TiffFileKind selected = TiffWriter.SelectFileKindForWrite(TiffFileKind.Classic, false, oversizedClassicByteCount);

        Assert.That(selected, Is.EqualTo(TiffFileKind.Classic));
    }

    [Test]
    public void SelectFileKindForWrite_KeepsExplicitBigTiff()
    {
        TiffFileKind selected = TiffWriter.SelectFileKindForWrite(TiffFileKind.BigTiff, true, 8);

        Assert.That(selected, Is.EqualTo(TiffFileKind.BigTiff));
    }

    [Test]
    public void ValidateFileKindCapacity_ReportsClassicOverflow()
    {
        TiffWriter writer = TiffWriter.Create();

        bool valid = writer.ValidateFileKindCapacity(TiffFileKind.Classic, (ulong)uint.MaxValue + 1UL);

        Assert.That(valid, Is.False);
        Assert.That(writer.Success, Is.False);
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void Flush_WritesHeaderOnlyFileWhenNoImagesWereAdded()
    {
        TiffWriter writer = TiffWriter.Create();

        TiffDiagnostics diagnostics = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.GetFileByteCount(), Is.EqualTo(8));
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImageCount(), Is.EqualTo(0));
        Assert.That(file.GetHeader().FirstIfdOffset, Is.EqualTo(0));
    }

    [Test]
    public void Writer_ReportsInvalidIndexes()
    {
        TiffWriter writer = TiffWriter.Create();

        int subImageIndex = writer.AddSubImage(3, new TiffImageDefinition());
        TiffRawStripWriteResult stripResult = writer.WriteRawStrip(3, 0, new byte[] { 1 });
        TiffRawTileWriteResult tileResult = writer.WriteRawTile(0, -1, new byte[] { 1 });

        Assert.That(subImageIndex, Is.EqualTo(-1));
        Assert.That(stripResult.Success, Is.False);
        Assert.That(tileResult.Success, Is.False);
        Assert.That(writer.Success, Is.False);
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
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

    static TiffTagValue[] CreateSizeOnlyTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length)
        };
    }

    static TiffTagValue[] CreateMinimalStripTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.WhiteIsZero)
        };
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
    }

    static byte[] CreateBytes(int count)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index = index + 1;
        }

        return bytes;
    }
}
