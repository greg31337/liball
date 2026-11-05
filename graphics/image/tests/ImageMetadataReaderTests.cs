using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataReaderTests
{
    [Test]
    public void ReadStream_DetectsFormatAndDispatchesParser()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/png"));
        Assert.That(document.ByteLength, Is.EqualTo(8));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.chunk.header.truncated"));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("PNG"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void ReadFile_DetectsFormatFromFile()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reader-test.jpg");
        File.WriteAllBytes(path, new byte[] { 255, 216, 255, 224 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(document.ByteLength, Is.EqualTo(4));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("stream"));

        File.Delete(path);
    }

    [Test]
    public void ReadStreamWithOptions_WhenFileLimitIsExceeded_CarriesLimitDiagnostic()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFileBytes = 4;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(stream, options);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.ByteLength, Is.EqualTo(8));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(2));
        Assert.That(HasDiagnostic(document, "reader.file.limit"), Is.True);
        Assert.That(HasDiagnostic(document, "png.signature.truncated"), Is.True);
    }

    [Test]
    public void ReadStreamWithOptions_NullStreamReturnsDiagnosticDocument()
    {
        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(null, null);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("reader.stream.null"));
    }

    [Test]
    public void ReadFileWithOptions_EmptyPathReturnsDiagnosticDocument()
    {
        ImageMetadataDocument document = new ImageMetadataReader().ReadFileWithOptions("", null);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("reader.path.empty"));
    }

    [Test]
    public void ReadNonSeekableBytes_ClampsToLimitAndReportsDiagnostic()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4, 5 });
        System.Collections.Generic.List<MetadataDiagnostic> diagnostics = new System.Collections.Generic.List<MetadataDiagnostic>();

        byte[] bytes = ImageMetadataReaderCore.ReadNonSeekableBytes(stream, 3, diagnostics);

        Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}

public class NonSeekableReadStream : Stream
{
    public byte[] Bytes;
    public int PositionValue;

    public NonSeekableReadStream(byte[] bytes)
    {
        Bytes = bytes;
        PositionValue = 0;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return Bytes.Length; }
    }

    public override long Position
    {
        get { return PositionValue; }
        set { PositionValue = (int)value; }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int available = Bytes.Length - PositionValue;
        if (available <= 0)
        {
            return 0;
        }

        int actual = count;
        if (actual > available)
        {
            actual = available;
        }

        int index = 0;
        while (index < actual)
        {
            buffer[offset + index] = Bytes[PositionValue + index];
            index = index + 1;
        }

        PositionValue = PositionValue + actual;
        return actual;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return PositionValue;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }
}
