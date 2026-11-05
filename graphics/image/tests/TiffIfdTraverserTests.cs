using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffIfdTraverserTests
{
    [Test]
    public void Parse_ClassicTiffTraversesLinkedIfds()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            0, 1, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0,
            26, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(3));
        Assert.That(document.Directories[1].Group, Is.EqualTo("IFD0"));
        Assert.That(document.Directories[1].Offset, Is.EqualTo(8));
        Assert.That(document.Directories[1].Length, Is.EqualTo(18));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.EntryCount").Value.UnsignedIntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.NextIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(26));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD1.EntryCount").Value.UnsignedIntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD1.NextIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(0));
    }

    [Test]
    public void Parse_BigTiffTraversesIfd()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 43, 0,
            8, 0,
            0, 0,
            16, 0, 0, 0, 0, 0, 0, 0,
            1, 0, 0, 0, 0, 0, 0, 0,
            1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.BigTiff, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[1].Group, Is.EqualTo("IFD0"));
        Assert.That(document.Directories[1].Offset, Is.EqualTo(16));
        Assert.That(document.Directories[1].Length, Is.EqualTo(36));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.EntryCount").Value.UnsignedIntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.CountSize").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.EntrySize").Value.IntegerValue, Is.EqualTo(20));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.NextOffsetSize").Value.IntegerValue, Is.EqualTo(8));
    }

    [Test]
    public void Parse_ClassicTiffLoopAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            8, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.loop"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Parse_RespectsMaxNestingDepth()
    {
        byte[] bytes = CreateTwoEmptyClassicIfds();
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxNestingDepth = 1;

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, options);

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.depth.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_RespectsMaxIfdCount()
    {
        byte[] bytes = CreateTwoEmptyClassicIfds();
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxIfdCount = 1;

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, options);

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.count.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_RespectsMaxTagCount()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            2, 0,
            0, 0, 0, 0
        };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 1;

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, options);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.tag.count.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_NegativeMaxTagCountStopsSafely()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = -1;

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, options);

        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.tag.count.limit"));
    }

    [Test]
    public void Parse_TruncatedIfdAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.count.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static byte[] CreateTwoEmptyClassicIfds()
    {
        return new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            14, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageFormatKind kind, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
    }
}
