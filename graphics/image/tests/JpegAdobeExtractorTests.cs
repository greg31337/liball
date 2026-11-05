using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegAdobeExtractorTests
{
    [Test]
    public void Parse_AdobeApp14ExtractsVersionFlagsAndColorTransform()
    {
        byte[] payload = new byte[]
        {
            65, 100, 111, 98, 101,
            0, 100,
            0, 1,
            0, 2,
            1
        };

        ImageMetadataDocument document = ParseJpeg(payload);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Jpeg));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP14"));
        Assert.That(document.Directories[0].Name, Is.EqualTo("Adobe"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(2));
        Assert.That(document.Directories[0].Length, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.Identifier").Value.Text, Is.EqualTo("Adobe"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.Version").Value.IntegerValue, Is.EqualTo(100));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.Flags0").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.Flags1").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.ColorTransform").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.ColorTransformName").Value.Text, Is.EqualTo("YCbCr"));
    }

    [Test]
    public void ColorTransformName_MapsKnownValues()
    {
        Assert.That(JpegAdobeExtractor.ColorTransformName(0), Is.EqualTo("Unknown or RGB/CMYK"));
        Assert.That(JpegAdobeExtractor.ColorTransformName(1), Is.EqualTo("YCbCr"));
        Assert.That(JpegAdobeExtractor.ColorTransformName(2), Is.EqualTo("YCCK"));
        Assert.That(JpegAdobeExtractor.ColorTransformName(99), Is.EqualTo("Unknown"));
    }

    [Test]
    public void Parse_TruncatedAdobeApp14AddsDirectoryAndDiagnostic()
    {
        byte[] payload = new byte[]
        {
            65, 100, 111, 98, 101,
            0, 100
        };

        ImageMetadataDocument document = ParseJpeg(payload);

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.Identifier").Value.Text, Is.EqualTo("Adobe"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Jpeg.APP14.Version"), Is.Null);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.adobe.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_PartialAdobeIdentifierAddsDiagnosticWithoutDirectory()
    {
        byte[] payload = Encoding.ASCII.GetBytes("Adob");

        ImageMetadataDocument document = ParseJpeg(payload);

        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.adobe.identifier.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static ImageMetadataDocument ParseJpeg(byte[] app14Payload)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        AddApp14(bytes, app14Payload);
        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), new ImageMetadataReaderOptions());
    }

    public static void AddApp14(List<byte> bytes, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add(238);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }
}
