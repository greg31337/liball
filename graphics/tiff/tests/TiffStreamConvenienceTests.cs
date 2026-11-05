using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffStreamConvenienceTests
{
    [Test]
    public void OpenRead_StreamReadsFileBytes()
    {
        byte[] fileBytes = CreateClassicFileBytes(new byte[] { 1, 2, 3, 4 });
        MemoryStream stream = new MemoryStream(fileBytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffFile file = TiffFile.OpenRead(stream, options);

        Assert.That(file.Success, Is.True);
        Assert.That(stream.CanRead, Is.True);
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
        Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void OpenRead_StreamRemainsOpenUntilFileClosesByDefault()
    {
        MemoryStream stream = new MemoryStream(CreateClassicFileBytes(new byte[] { 1, 2, 3, 4 }));

        TiffFile file = TiffFile.OpenRead(stream);

        Assert.That(file.Success, Is.True);
        Assert.That(stream.CanRead, Is.True);

        file.Close();

        Assert.That(stream.CanRead, Is.False);
    }

    [Test]
    public void OpenRead_StreamReportsNullStream()
    {
        Stream stream = null!;

        TiffFile file = TiffFile.OpenRead(stream);

        Assert.That(file.Success, Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void Create_StreamFlushWritesBytes()
    {
        MemoryStream stream = new MemoryStream();
        TiffWriteOptions options = new TiffWriteOptions();
        options.LeaveOpen = true;
        TiffWriter writer = TiffWriter.Create(stream, options);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 5, 6, 7, 8 });
        TiffDiagnostics diagnostics = writer.Flush();
        TiffFile file = TiffFile.OpenRead(stream.ToArray());

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(stream.Length, Is.EqualTo(writer.GetFileByteCount()));
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
    }

    [Test]
    public void Close_StreamDisposesByDefault()
    {
        MemoryStream stream = new MemoryStream();
        TiffWriter writer = TiffWriter.Create(stream);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 9 });
        writer.Close();

        Assert.That(writer.IsOpen, Is.False);
        Assert.That(stream.CanWrite, Is.False);
    }

    [Test]
    public void Close_StreamRespectsLeaveOpen()
    {
        MemoryStream stream = new MemoryStream();
        TiffWriteOptions options = new TiffWriteOptions();
        options.LeaveOpen = true;
        TiffWriter writer = TiffWriter.Create(stream, options);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1)));

        writer.WriteRawStrip(imageIndex, 0, new byte[] { 42 });
        writer.Close();
        TiffFile file = TiffFile.OpenRead(stream.ToArray());

        Assert.That(writer.IsOpen, Is.False);
        Assert.That(stream.CanWrite, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImage(0).ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 42 }));
    }

    [Test]
    public void Create_StreamReportsNullStream()
    {
        Stream stream = null!;

        TiffWriter writer = TiffWriter.Create(stream);

        Assert.That(writer.Success, Is.False);
        Assert.That(writer.GetDiagnostics().ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static byte[] CreateClassicFileBytes(byte[] stripBytes)
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));
        writer.WriteRawStrip(imageIndex, 0, stripBytes);
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
