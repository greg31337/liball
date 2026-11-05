using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPathConvenienceTests
{
    [Test]
    public void OpenRead_PathReadsFileBytes()
    {
        string path = CreateTempPath();
        try
        {
            byte[] fileBytes = CreateClassicFileBytes();
            File.WriteAllBytes(path, fileBytes);

            TiffFile file = TiffFile.OpenRead(path);

            Assert.That(file.Success, Is.True);
            Assert.That(file.GetImageCount(), Is.EqualTo(1));
            Assert.That(file.GetImage(0).GetInfo().Width, Is.EqualTo(2));
            Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void OpenRead_PathUsesOptions()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllBytes(path, CreateClassicFileBytes());
            TiffReadOptions options = new TiffReadOptions();
            options.Limits.MaximumIfdCount = 0;

            TiffFile file = TiffFile.OpenRead(path, options);

            Assert.That(file.Success, Is.False);
            Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void OpenRead_PathReportsMissingFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "missing-" + Path.GetRandomFileName() + ".tif");

        TiffFile file = TiffFile.OpenRead(path);

        Assert.That(file.Success, Is.False);
        Assert.That(file.IsOpen, Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void Create_PathFlushWritesFile()
    {
        string path = CreateTempPath();
        try
        {
            TiffWriter writer = TiffWriter.Create(path);
            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));

            writer.WriteRawStrip(imageIndex, 0, new byte[] { 5, 6, 7, 8 });
            TiffDiagnostics diagnostics = writer.Flush();
            TiffFile file = TiffFile.OpenRead(path);

            Assert.That(diagnostics.Count(), Is.EqualTo(0));
            Assert.That(writer.Success, Is.True);
            Assert.That(writer.GetPath(), Is.EqualTo(path));
            Assert.That(File.Exists(path), Is.True);
            Assert.That(file.Success, Is.True);
            Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void Create_PathReportsBlankPath()
    {
        TiffWriter writer = TiffWriter.Create("");

        Assert.That(writer.Success, Is.False);
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void Close_PathFlushesFile()
    {
        string path = CreateTempPath();
        try
        {
            TiffWriter writer = TiffWriter.Create(path);
            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1)));

            writer.WriteRawStrip(imageIndex, 0, new byte[] { 42 });
            writer.Close();
            TiffFile file = TiffFile.OpenRead(path);

            Assert.That(writer.IsOpen, Is.False);
            Assert.That(file.Success, Is.True);
            Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 42 }));
        }
        finally
        {
            DeleteIfExists(path);
        }
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

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
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
}
