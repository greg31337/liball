using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegExifExtractorTests
{
    [Test]
    public void Parse_ExifApp1ExtractsTiffPayloadAsProfileAndDirectory()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 225, 0, 16,
            69, 120, 105, 102, 0, 0,
            73, 73, 42, 0, 8, 0, 0, 0,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Exif));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Exif.APP1"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("EXIF"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("image/tiff"));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(12));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(8));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 }));
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP1"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(2));
        Assert.That(document.Directories[0].Length, Is.EqualTo(18));
        Assert.That(document.Directories[1].Path, Is.EqualTo("JPEG/APP1/EXIF/TIFF/Header"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.Identifier").Value.Text, Is.EqualTo("Exif"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffPayloadOffset").Value.IntegerValue, Is.EqualTo(12));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffByteLength").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.RoutedToTiffParser").Value.BooleanValue, Is.True);
    }

    [Test]
    public void Parse_ExifApp1RecognizesBigEndianTiffPayload()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 225, 0, 16,
            69, 120, 105, 102, 0, 0,
            77, 77, 0, 42, 0, 0, 0, 8,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffByteOrder").Value.Text, Is.EqualTo("BigEndian"));
    }

    [Test]
    public void Parse_ExifProfileLimitKeepsOffsetsAndSkipsBufferedData()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 225, 0, 16,
            69, 120, 105, 102, 0, 0,
            73, 73, 42, 0, 8, 0, 0, 0,
            255, 217
        };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;

        ImageMetadataDocument document = ParseJpeg(bytes, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(12));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(8));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.exif.profile.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_PartialExifIdentifierAddsDiagnosticWithoutProfile()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 225, 0, 7,
            69, 120, 105, 102, 0,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.exif.identifier.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_InvalidExifTiffHeaderAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 225, 0, 12,
            69, 120, 105, 102, 0, 0,
            78, 79, 80, 69,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffByteOrder").Value.Text, Is.EqualTo("Unknown"));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.exif.tiff.header.invalid"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    public static ImageMetadataDocument ParseJpeg(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
    }
}
