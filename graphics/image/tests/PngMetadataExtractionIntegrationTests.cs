using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngMetadataExtractionIntegrationTests
{
    [Test]
    public void ReadStream_CompositePngCombinesMetadataProfilesColorAndSummary()
    {
        byte[] png = BuildCompositePng();

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(png));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/png"));
        Assert.That(document.ByteLength, Is.EqualTo(png.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(document.Summary.Author, Is.EqualTo("Ada"));
        Assert.That(document.Summary.Title, Is.EqualTo("Coast"));
        Assert.That(document.Summary.Description, Is.EqualTo("Compressed caption"));
        Assert.That(document.Summary.Keywords, Is.EqualTo(new string[] { "ocean", "sunset" }));
        Assert.That(document.Summary.CaptureTimeText, Is.EqualTo("2026-08-12T10:11:12Z"));
        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Exif));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp));
        Assert.That(PngMetadataExtractorTests.TextValue(document, "Png.zTXt.Text"), Is.EqualTo("Compressed caption"));
        Assert.That(PngMetadataExtractorTests.TextValue(document, "Png.iTXt.Text"), Does.Contain("xmpmeta"));
        Assert.That(PngMetadataExtractorTests.UnsignedValue(document, "Png.pHYs.PixelsPerUnitX"), Is.EqualTo(3780));
    }

    [Test]
    public void Parse_CompressedTextChunksApplyDecompressionLimitIndependently()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxDecompressedBytes = 5;
        byte[] ztxt = PngMetadataExtractorTests.CompressedTextPayload("Description", "abcdefghij");
        byte[] itxt = PngMetadataExtractorTests.InternationalTextPayload(
            "Comment",
            1,
            "en",
            "Comment",
            PngMetadataExtractorTests.Compress(Encoding.UTF8.GetBytes("klmnopqrst")));
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "zTXt", "iTXt", "IEND" },
            new byte[][] { PngMetadataExtractorTests.Ihdr(6), ztxt, itxt, new byte[0] });

        ImageMetadataDocument document = PngMetadataExtractorTests.ParsePng(png, options);

        Assert.That(PngMetadataExtractorTests.TextValue(document, "Png.zTXt.Text"), Is.EqualTo("abcde"));
        Assert.That(PngMetadataExtractorTests.TextValue(document, "Png.iTXt.Text"), Is.EqualTo("klmno"));
        Assert.That(document.Summary.Description, Is.EqualTo("abcde"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.Text.Comment").Value.Text, Is.EqualTo("klmno"));
        Assert.That(document.Diagnostics, Has.Exactly(2).Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "zlib.limit"));
        Assert.That(document.Diagnostics, Has.All.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Severity == MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_CompressedTextAtExactLimitDoesNotReportLimit()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxDecompressedBytes = 5;
        byte[] payload = PngMetadataExtractorTests.CompressedTextPayload("Title", "abcde");
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "zTXt", "IEND" },
            new byte[][] { PngMetadataExtractorTests.Ihdr(6), payload, new byte[0] });

        ImageMetadataDocument document = PngMetadataExtractorTests.ParsePng(png, options);

        Assert.That(PngMetadataExtractorTests.TextValue(document, "Png.zTXt.Text"), Is.EqualTo("abcde"));
        Assert.That(document.Summary.Title, Is.EqualTo("abcde"));
        Assert.That(document.Diagnostics, Is.Empty);
    }

    [Test]
    public void Parse_IccUsesSmallerOfProfileAndDecompressionLimits()
    {
        byte[] profile = Encoding.ASCII.GetBytes("abcdefghijkl");
        byte[] payload = PngColorExtractorTests.IccPayload("Limited", 0, PngColorExtractorTests.Compress(profile));
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "iCCP", "IEND" },
            new byte[][] { PngMetadataExtractorTests.Ihdr(6), payload, new byte[0] });

        ImageMetadataReaderOptions decompressionOptions = new ImageMetadataReaderOptions();
        decompressionOptions.MaxProfileBytes = 20;
        decompressionOptions.MaxDecompressedBytes = 7;
        ImageMetadataDocument decompressionLimited = PngMetadataExtractorTests.ParsePng(png, decompressionOptions);

        ImageMetadataReaderOptions profileOptions = new ImageMetadataReaderOptions();
        profileOptions.MaxProfileBytes = 5;
        profileOptions.MaxDecompressedBytes = 20;
        ImageMetadataDocument profileLimited = PngMetadataExtractorTests.ParsePng(png, profileOptions);

        Assert.That(decompressionLimited.Profiles[0].Data, Is.EqualTo(Encoding.ASCII.GetBytes("abcdefg")));
        Assert.That(profileLimited.Profiles[0].Data, Is.EqualTo(Encoding.ASCII.GetBytes("abcde")));
        PngMetadataExtractorTests.AssertDiagnostic(decompressionLimited, "zlib.limit");
        PngMetadataExtractorTests.AssertDiagnostic(profileLimited, "zlib.limit");
    }

    [Test]
    public void Parse_NullReaderReturnsPngDiagnosticDocument()
    {
        ImageMetadataDocument document = PngMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), null);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.reader.null"));
    }

    public static byte[] BuildCompositePng()
    {
        byte[] exif = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
        string xml = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>";
        byte[] xmp = PngMetadataExtractorTests.InternationalTextPayload(
            "XML:com.adobe.xmp",
            1,
            "",
            "",
            PngMetadataExtractorTests.Compress(Encoding.UTF8.GetBytes(xml)));
        List<byte> physical = new List<byte>();
        PngChunkTraverserTests.AddUnsigned32(physical, 3780);
        PngChunkTraverserTests.AddUnsigned32(physical, 3780);
        physical.Add(1);

        return PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "cICP", "pHYs", "tEXt", "tEXt", "zTXt", "tEXt", "tEXt", "iTXt", "eXIf", "tRNS", "IEND" },
            new byte[][]
            {
                PngIhdrExtractorTests.CreateIhdr(640, 480, 8, 2, 0, 0, 0),
                new byte[] { 12, 13, 0, 1 },
                physical.ToArray(),
                PngMetadataExtractorTests.Latin1Payload("Author", "Ada"),
                PngMetadataExtractorTests.Latin1Payload("Title", "Coast"),
                PngMetadataExtractorTests.CompressedTextPayload("Description", "Compressed caption"),
                PngMetadataExtractorTests.Latin1Payload("Keywords", "ocean; sunset"),
                PngMetadataExtractorTests.Latin1Payload("Creation Time", "2026-08-12T10:11:12Z"),
                xmp,
                exif,
                new byte[] { 0, 17, 0, 34, 0, 51 },
                new byte[0]
            });
    }
}
