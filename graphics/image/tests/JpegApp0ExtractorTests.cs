using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegApp0ExtractorTests
{
    [Test]
    public void Parse_JfifApp0ExtractsVersionDensityAndThumbnailMetadata()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 224, 0, 16,
            74, 70, 73, 70, 0,
            1, 2,
            1,
            0, 72,
            0, 96,
            2,
            3,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Jfif));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP0"));
        Assert.That(document.Directories[0].Name, Is.EqualTo("JFIF"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(2));
        Assert.That(document.Directories[0].Length, Is.EqualTo(18));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.Identifier").Value.Text, Is.EqualTo("JFIF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.VersionMajor").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.VersionMinor").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.Version").Value.Text, Is.EqualTo("1.02"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.DensityUnits").DisplayValue, Is.EqualTo("Pixels per inch"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.XDensity").Value.IntegerValue, Is.EqualTo(72));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.YDensity").Value.IntegerValue, Is.EqualTo(96));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.XThumbnail").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.YThumbnail").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.ThumbnailByteLength").Value.IntegerValue, Is.EqualTo(0));
    }

    [Test]
    public void Parse_JfxxApp0ExtractsExtensionAndThumbnailByteLength()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 224, 0, 12,
            74, 70, 88, 88, 0,
            19,
            1, 2, 3, 4,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Jfif));
        Assert.That(document.Directories[0].Name, Is.EqualTo("JFXX"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.JFXX.Identifier").Value.Text, Is.EqualTo("JFXX"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.JFXX.ExtensionCode").Value.IntegerValue, Is.EqualTo(19));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.JFXX.ExtensionName").Value.Text, Is.EqualTo("RGB thumbnail"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.JFXX.ThumbnailByteLength").Value.IntegerValue, Is.EqualTo(4));
    }

    [Test]
    public void Parse_TruncatedJfifApp0AddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 224, 0, 7,
            74, 70, 73, 70, 0,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Name, Is.EqualTo("JFIF"));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.jfif.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_UnknownApp0IdentifierIsIgnored()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 224, 0, 8,
            78, 79, 80, 69, 0, 1,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    public static ImageMetadataDocument ParseJpeg(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), new ImageMetadataReaderOptions());
    }
}
