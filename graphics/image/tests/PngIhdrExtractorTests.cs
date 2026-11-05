using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngIhdrExtractorTests
{
    [Test]
    public void Parse_IhdrExtractsFrameAndHeaderFields()
    {
        byte[] ihdr = CreateIhdr(640, 480, 8, 6, 0, 0, 1);

        ImageMetadataDocument document = ParsePng(PngChunkTraverserTests.CreatePng(new string[] { "IHDR", "IEND" }, new byte[][] { ihdr, new byte[0] }));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(640));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(480));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Deflate));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].SourceColorType, Is.EqualTo("Truecolor with alpha"));
        Assert.That(document.Frames[0].IsInterlaced, Is.True);
        Assert.That(document.Frames[0].IsProgressive, Is.False);
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(1));
        Assert.That(document.Summary.SourceColorType, Is.EqualTo("Truecolor with alpha"));
        Assert.That(document.Summary.IsInterlaced, Is.True);

        Assert.That(UnsignedTag(document, "Png.IHDR.Width").Value.UnsignedIntegerValue, Is.EqualTo(640));
        Assert.That(UnsignedTag(document, "Png.IHDR.Height").Value.UnsignedIntegerValue, Is.EqualTo(480));
        Assert.That(IntegerTag(document, "Png.IHDR.BitDepth").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(IntegerTag(document, "Png.IHDR.ColorType").DisplayValue, Is.EqualTo("Truecolor with alpha"));
        Assert.That(IntegerTag(document, "Png.IHDR.CompressionMethod").DisplayValue, Is.EqualTo("Deflate"));
        Assert.That(IntegerTag(document, "Png.IHDR.FilterMethod").DisplayValue, Is.EqualTo("Adaptive"));
        Assert.That(IntegerTag(document, "Png.IHDR.InterlaceMethod").DisplayValue, Is.EqualTo("Adam7"));
    }

    [TestCase(0, 16, 1, false, ImageColorSpaceKind.Gray)]
    [TestCase(2, 8, 3, false, ImageColorSpaceKind.Rgb)]
    [TestCase(3, 4, 1, false, ImageColorSpaceKind.Indexed)]
    [TestCase(4, 8, 2, true, ImageColorSpaceKind.Gray)]
    [TestCase(6, 16, 4, true, ImageColorSpaceKind.Rgb)]
    public void Parse_ColorTypeSetsFrameCharacteristics(int colorType, int bitDepth, int channelCount, bool hasAlpha, ImageColorSpaceKind colorSpace)
    {
        ImageMetadataDocument document = ParsePng(PngChunkTraverserTests.CreatePng(
            new string[] { "IHDR", "IEND" },
            new byte[][] { CreateIhdr(32, 24, bitDepth, colorType, 0, 0, 0), new byte[0] }));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(channelCount));
        Assert.That(document.Frames[0].HasAlpha, Is.EqualTo(hasAlpha));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(colorSpace));
    }

    [Test]
    public void Parse_ShortIhdrAddsTruncationDiagnosticAndNoFrame()
    {
        byte[] shortHeader = new byte[] { 0, 0, 0, 1, 0, 0, 0, 1 };

        ImageMetadataDocument document = ParsePng(PngChunkTraverserTests.CreatePng(new string[] { "IHDR", "IEND" }, new byte[][] { shortHeader, new byte[0] }));

        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.truncated" && diagnostic.Severity == MetadataSeverity.Truncated));
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Png.IHDR.Width"), Is.Null);
    }

    [Test]
    public void Parse_InvalidIhdrValuesArePreservedWithDiagnostics()
    {
        byte[] ihdr = CreateIhdr(0, 1, 4, 2, 1, 2, 3);

        ImageMetadataDocument document = ParsePng(PngChunkTraverserTests.CreatePng(new string[] { "IHDR", "IEND" }, new byte[][] { ihdr, new byte[0] }));

        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.width.invalid"));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.bit-depth.invalid"));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.compression.invalid"));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.filter.invalid"));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.interlace.invalid"));
        Assert.That(UnsignedTag(document, "Png.IHDR.Width").Value.UnsignedIntegerValue, Is.Zero);
        Assert.That(IntegerTag(document, "Png.IHDR.CompressionMethod").Value.IntegerValue, Is.EqualTo(1));
    }

    [Test]
    public void Parse_MissingAndDuplicateIhdrChunksAddDiagnostics()
    {
        ImageMetadataDocument missing = ParsePng(PngChunkTraverserTests.CreatePng(new string[] { "IEND" }, new byte[][] { new byte[0] }));
        ImageMetadataDocument duplicate = ParsePng(PngChunkTraverserTests.CreatePng(
            new string[] { "IHDR", "IHDR", "IEND" },
            new byte[][] { CreateIhdr(1, 1, 8, 6, 0, 0, 0), CreateIhdr(2, 2, 8, 6, 0, 0, 0), new byte[0] }));

        Assert.That(missing.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.missing"));
        Assert.That(missing.Frames, Is.Empty);
        Assert.That(duplicate.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "png.ihdr.duplicate"));
        Assert.That(duplicate.Frames, Has.Count.EqualTo(1));
        Assert.That(duplicate.Frames[0].Width, Is.EqualTo(1));
    }

    public static MetadataTag UnsignedTag(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedInteger));
        return tag;
    }

    public static MetadataTag IntegerTag(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.Integer));
        return tag;
    }

    public static byte[] CreateIhdr(uint width, uint height, int bitDepth, int colorType, int compressionMethod, int filterMethod, int interlaceMethod)
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddUnsigned32(bytes, width);
        PngChunkTraverserTests.AddUnsigned32(bytes, height);
        bytes.Add((byte)bitDepth);
        bytes.Add((byte)colorType);
        bytes.Add((byte)compressionMethod);
        bytes.Add((byte)filterMethod);
        bytes.Add((byte)interlaceMethod);
        return bytes.ToArray();
    }

    public static ImageMetadataDocument ParsePng(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PngMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), new ImageMetadataReaderOptions());
    }
}
