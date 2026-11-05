using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffHeaderParserTests
{
    [Test]
    public void Parse_LittleEndianClassicTiffHeader()
    {
        byte[] bytes = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        Assert.That(document.Format.Name, Is.EqualTo("TIFF"));
        Assert.That(document.ByteLength, Is.EqualTo(8));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Tiff));
        Assert.That(document.Directories[0].Group, Is.EqualTo("Header"));
        Assert.That(document.Directories[0].Length, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Magic").Value.IntegerValue, Is.EqualTo(42));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.IsBigTiff").Value.BooleanValue, Is.False);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.OffsetSize").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.FirstIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(8));
    }

    [Test]
    public void Parse_BigEndianClassicTiffHeader()
    {
        byte[] bytes = new byte[] { 77, 77, 0, 42, 0, 0, 0, 8 };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo("BigEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.FirstIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(8));
    }

    [Test]
    public void Parse_LittleEndianBigTiffHeader()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 43, 0,
            8, 0,
            0, 0,
            16, 0, 0, 0, 0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.BigTiff);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.BigTiff));
        Assert.That(document.Format.Name, Is.EqualTo("BigTIFF"));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Name, Is.EqualTo("BigTIFF Header"));
        Assert.That(document.Directories[0].Length, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Magic").Value.IntegerValue, Is.EqualTo(43));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.IsBigTiff").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.OffsetSize").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Reserved").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.FirstIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(16));
    }

    [Test]
    public void Parse_InvalidBigTiffOffsetSizeAndReservedAddDiagnostics()
    {
        byte[] bytes = new byte[]
        {
            77, 77, 0, 43,
            0, 4,
            0, 1,
            0, 0, 0, 0, 0, 0, 0, 16
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.BigTiff);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.OffsetSize").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Reserved").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bigtiff.offset.size.invalid"));
        Assert.That(document.Diagnostics[1].Code, Is.EqualTo("bigtiff.reserved.invalid"));
    }

    [Test]
    public void Parse_InvalidByteOrderAddsDiagnosticWithoutDirectory()
    {
        byte[] bytes = new byte[] { 90, 90, 42, 0, 8, 0, 0, 0 };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff);

        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.byte.order.invalid"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Parse_FirstIfdOffsetOutsideStreamAddsDiagnostic()
    {
        byte[] bytes = new byte[] { 73, 73, 42, 0, 16, 0, 0, 0 };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.offset.outside"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageFormatKind kind)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), new ImageMetadataReaderOptions());
    }
}
