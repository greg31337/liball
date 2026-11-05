using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMultiPageRoundTripTests
{
    [Test]
    public void ClassicWriter_RoundTripsMultiplePagesInOrder()
    {
        TiffWriter writer = TiffWriter.Create();
        int firstImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2, 0, 2)));
        int secondImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 1, 1, 2)));

        writer.WriteRawStrip(firstImageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.WriteRawStrip(secondImageIndex, 0, new byte[] { 5, 6, 7 });
        TiffDiagnostics flushDiagnostics = writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffDirectory[] directories = file.GetDirectories();
        TiffValidationResult validation = TiffValidator.Validate(fileBytes);

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(file.GetImageCount(), Is.EqualTo(2));
        Assert.That(directories[0].NextIfdOffset, Is.EqualTo(directories[1].Offset));
        Assert.That(directories[1].NextIfdOffset, Is.EqualTo(0));
        AssertPage(file.GetImage(0), 2, 2, 0, new byte[] { 1, 2, 3, 4 });
        AssertPage(file.GetImage(1), 3, 1, 1, new byte[] { 5, 6, 7 });
        Assert.That(validation.Success, Is.True);
    }

    [Test]
    public void BigTiffWriter_RoundTripsMultiplePagesInOrder()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.BigTiff;
        TiffWriter writer = TiffWriter.Create(options);
        int firstImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 2, 0, 2)));
        int secondImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 1, 1, 2)));

        writer.WriteRawStrip(firstImageIndex, 0, new byte[] { 9, 8 });
        writer.WriteRawStrip(secondImageIndex, 0, new byte[] { 7, 6 });
        TiffDiagnostics flushDiagnostics = writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffDirectory[] directories = file.GetDirectories();

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(file.GetImageCount(), Is.EqualTo(2));
        Assert.That(directories[0].NextIfdOffset, Is.EqualTo(directories[1].Offset));
        Assert.That(directories[1].NextIfdOffset, Is.EqualTo(0));
        AssertPage(file.GetImage(0), 1, 2, 0, new byte[] { 9, 8 });
        AssertPage(file.GetImage(1), 2, 1, 1, new byte[] { 7, 6 });
    }

    static void AssertPage(TiffImage image, ulong width, ulong length, ulong pageIndex, byte[] expectedBytes)
    {
        TiffImageInfo info = image.GetInfo();
        TiffTagDefaultResult pageNumber = image.GetTagDefaulted(TiffTagId.PageNumber);
        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(image.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(width));
        Assert.That(info.Length, Is.EqualTo(length));
        Assert.That(pageNumber.Found, Is.True);
        Assert.That(pageNumber.UsedDefault, Is.False);
        Assert.That(pageNumber.Value.UnsignedValues, Is.EqualTo(new ulong[] { pageIndex, 2 }));
        Assert.That(image.GetStripCount(), Is.EqualTo(1));
        Assert.That(strip.Success, Is.True);
        Assert.That(strip.Bytes, Is.EqualTo(expectedBytes));
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, ulong pageIndex, ulong pageCount)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedArray(TiffTagId.PageNumber, TiffDataType.Short, new ulong[] { pageIndex, pageCount })
        };
    }
}
