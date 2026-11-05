using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffInspectorTests
{
    [Test]
    public void OpenMetadata_ReturnsMetadataAndBlocksRawImageReads()
    {
        byte[] fileBytes = CreateClassicFileBytes();

        TiffFile file = TiffFile.OpenMetadata(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(file.Success, Is.True);
        Assert.That(file.Options.ReadImageData, Is.False);
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Length, Is.EqualTo(2));
        Assert.That(image.GetTag(TiffTagId.ImageDescription), Is.Not.Null);
        Assert.That(strip.Success, Is.False);
        Assert.That(strip.Bytes.Length, Is.EqualTo(0));
        Assert.That(image.GetDiagnostics().ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void Inspect_BytesReturnsHeaderImagesTagsAndInfo()
    {
        byte[] fileBytes = CreateClassicFileBytes();

        TiffInspectionResult result = TiffInspector.Inspect(fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(result.FileByteCount, Is.EqualTo(fileBytes.Length));
        Assert.That(result.ImageCount(), Is.EqualTo(1));
        Assert.That(result.Images[0].ImageIndex, Is.EqualTo(0));
        Assert.That(result.Images[0].Info.Width, Is.EqualTo(2));
        Assert.That(result.Images[0].Tags.Length, Is.EqualTo(8));
        Assert.That(result.Images[0].SubImageCount, Is.EqualTo(0));
    }

    [Test]
    public void Inspect_PathReadsMetadataOnlySummary()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllBytes(path, CreateClassicFileBytes());

            TiffInspectionResult result = TiffInspector.Inspect(path);

            Assert.That(result.Success, Is.True);
            Assert.That(result.ImageCount(), Is.EqualTo(1));
            Assert.That(result.Images[0].Info.Length, Is.EqualTo(2));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void Inspect_StreamRespectsLeaveOpen()
    {
        MemoryStream stream = new MemoryStream(CreateClassicFileBytes());
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffInspectionResult result = TiffInspector.Inspect(stream, options);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.CanRead, Is.True);
        Assert.That(result.Images[0].Info.Width, Is.EqualTo(2));
    }

    [Test]
    public void Inspect_StreamClosesByDefaultAfterInspection()
    {
        MemoryStream stream = new MemoryStream(CreateClassicFileBytes());

        TiffInspectionResult result = TiffInspector.Inspect(stream);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.CanRead, Is.False);
    }

    [Test]
    public void Inspect_InvalidHeaderReportsDiagnostics()
    {
        TiffInspectionResult result = TiffInspector.Inspect(new byte[] { 0, 0, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateStripTags()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "metadata")
        };
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        DeleteIfExists(path);
        return path;
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
