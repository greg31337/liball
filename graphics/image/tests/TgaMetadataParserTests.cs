using System.Collections.Generic;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TgaMetadataParserTests
{
    [Test]
    public void Parse_ExtractsHeaderExtensionDeveloperMetadataAndSummary()
    {
        byte[] bytes = CreateVersion2Tga(1);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tga));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Tga.Header.ImageType").DisplayValue, Is.EqualTo("Uncompressed true-color"));
        Assert.That(Find(document, "Tga.Header.Width").Value.IntegerValue, Is.EqualTo(320));
        Assert.That(Find(document, "Tga.Header.Height").Value.IntegerValue, Is.EqualTo(200));
        Assert.That(Find(document, "Tga.Footer.ExtensionOffset").Value.IntegerValue, Is.EqualTo(64));
        Assert.That(Find(document, "Tga.Footer.DeveloperOffset").Value.IntegerValue, Is.EqualTo(559));
        Assert.That(Find(document, "Tga.Extension.AuthorName").Value.Text, Is.EqualTo("Ada Artist"));
        Assert.That(Find(document, "Tga.Extension.AuthorComments").Value.Text, Is.EqualTo("First line\nSecond line"));
        Assert.That(Find(document, "Tga.Extension.Timestamp").Value.Text, Is.EqualTo("2026:08:12 14:35:09"));
        Assert.That(Find(document, "Tga.Extension.JobName").Value.Text, Is.EqualTo("Nightly render"));
        Assert.That(Find(document, "Tga.Extension.JobHours").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(Find(document, "Tga.Extension.JobMinutes").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(Find(document, "Tga.Extension.JobSeconds").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(Find(document, "Tga.Extension.SoftwareId").Value.Text, Is.EqualTo("RenderLab"));
        Assert.That(Find(document, "Tga.Extension.SoftwareVersionNumber").DisplayValue, Is.EqualTo("2.50b"));
        Assert.That(Find(document, "Tga.Extension.KeyColor").Value.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(Find(document, "Tga.Extension.PixelAspectRatio").Value.Numerator, Is.EqualTo(4));
        Assert.That(Find(document, "Tga.Extension.PixelAspectRatio").Value.Denominator, Is.EqualTo(3));
        Assert.That(Find(document, "Tga.Extension.Gamma").Value.Numerator, Is.EqualTo(22));
        Assert.That(Find(document, "Tga.Extension.Gamma").Value.Denominator, Is.EqualTo(10));
        Assert.That(Find(document, "Tga.Extension.AttributesType").DisplayValue, Is.EqualTo("Useful alpha data"));
        MetadataDirectory developer = document.Directories.Single(item => item.Path == "TGA/Developer");
        Assert.That(developer.Children, Has.Count.EqualTo(1));
        Assert.That(developer.Children[0].Tags[0].Value.IntegerValue, Is.EqualTo(42));
        Assert.That(developer.Children[0].Tags[1].Value.IntegerValue, Is.EqualTo(620));
        Assert.That(developer.Children[0].Tags[2].Value.IntegerValue, Is.EqualTo(4));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(320));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Summary.Author, Is.EqualTo("Ada Artist"));
        Assert.That(document.Summary.Description, Is.EqualTo("First line\nSecond line"));
        Assert.That(document.Summary.CaptureTimeText, Is.EqualTo("2026:08:12 14:35:09"));
        Assert.That(document.Summary.Software, Is.EqualTo("RenderLab 2.50b"));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Does.Contain("Extension"));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Does.Contain("DeveloperDirectory"));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Does.Contain("DeveloperData.42"));
    }

    [Test]
    public void Parse_ExtractsImageIdAndColorMapLayout()
    {
        byte[] bytes = new byte[126];
        WriteHeader(bytes, 4, 1, 9, 5, 2, 24, 7, 9, 32, 16, 8, 32);
        WriteText(bytes, 18, 4, "ICON");
        bytes[22] = 1;
        bytes[23] = 2;
        bytes[24] = 3;
        bytes[25] = 4;
        bytes[26] = 5;
        bytes[27] = 6;
        WriteFooter(bytes, 0, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Tga.ImageId").Value.Text, Is.EqualTo("ICON"));
        Assert.That(Find(document, "Tga.Header.ImageDataOffset").Value.IntegerValue, Is.EqualTo(28));
        Assert.That(Find(document, "Tga.ColorMap.FirstEntry").Value.IntegerValue, Is.EqualTo(5));
        Assert.That(Find(document, "Tga.ColorMap.EntryCount").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(Find(document, "Tga.ColorMap.ByteLength").Value.IntegerValue, Is.EqualTo(6));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Indexed));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(1));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EquivalentTo(new[] { "Header", "ImageId", "ColorMap", "Footer" }));
    }

    [TestCase(32, ImageOrientation.Normal)]
    [TestCase(48, ImageOrientation.MirrorHorizontal)]
    [TestCase(0, ImageOrientation.MirrorVertical)]
    [TestCase(16, ImageOrientation.Rotate180)]
    public void Parse_MapsImageOriginsToOrientations(int descriptor, ImageOrientation expected)
    {
        byte[] bytes = CreateHeaderOnly(3, 8, descriptor);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(expected));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Gray));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
    }

    [Test]
    public void Parse_LegacyFileWithoutFooterStillReturnsHeaderMetadata()
    {
        byte[] bytes = CreateHeaderOnly(10, 24, 32);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "TGA/Header" }));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
    }

    [Test]
    public void Parse_PublicReaderDetectsFooterBeyondLeadingProbe()
    {
        byte[] bytes = new byte[12000];
        WriteHeader(bytes, 0, 0, 2, 0, 0, 0, 0, 0, 640, 480, 24, 32);
        WriteFooter(bytes, 0, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tga));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
    }

    [Test]
    public void Parse_TruncatedHeaderReportsDiagnostic()
    {
        ImageMetadataDocument document = ParseDirect(new byte[] { 0, 0, 2, 0, 0 }, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "tga.header.truncated"), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    [TestCase(2, 12, "tga.pixel-depth.invalid")]
    [TestCase(7, 8, "tga.image-type.unsupported")]
    public void Parse_InvalidImageEncodingDoesNotCreateFrame(int imageType, int pixelDepth, string code)
    {
        byte[] bytes = CreateHeaderOnly(imageType, pixelDepth, 32);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, code), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories.Select(item => item.Path), Does.Contain("TGA/Header"));
    }

    [Test]
    public void Parse_MissingRequiredColorMapIsDiagnosed()
    {
        byte[] bytes = CreateHeaderOnly(1, 8, 32);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "tga.color-map.missing"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_TruncatedImageIdAndColorMapAreDiagnosed()
    {
        byte[] bytes = new byte[22];
        WriteHeader(bytes, 8, 1, 1, 0, 10, 24, 0, 0, 2, 2, 8, 32);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "tga.image-id.truncated"), Is.True);
        Assert.That(HasDiagnostic(document, "tga.color-map.truncated"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_InvalidExtensionOffsetDoesNotPreventDeveloperParsing()
    {
        byte[] bytes = CreateVersion2Tga(1);
        WriteUnsigned32(bytes, bytes.Length - 26, 680);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "tga.extension.offset.invalid"), Is.True);
        Assert.That(document.Directories.Select(item => item.Path), Does.Not.Contain("TGA/Extension"));
        Assert.That(document.Directories.Select(item => item.Path), Does.Contain("TGA/Developer"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_ShortExtensionIsRejectedWithoutLosingHeader()
    {
        byte[] bytes = CreateVersion2Tga(0);
        WriteUnsigned16(bytes, 64, 100);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "tga.extension.size.invalid"), Is.True);
        Assert.That(document.Directories.Select(item => item.Path), Does.Not.Contain("TGA/Extension"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_InvalidDeveloperPayloadDoesNotPreventLaterEntry()
    {
        byte[] bytes = CreateVersion2Tga(2);
        WriteUnsigned32(bytes, 563, 900);
        WriteUnsigned32(bytes, 567, 12);
        WriteUnsigned16(bytes, 571, 77);
        WriteUnsigned32(bytes, 573, 630);
        WriteUnsigned32(bytes, 577, 3);
        bytes[630] = 9;
        bytes[631] = 8;
        bytes[632] = 7;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "tga.developer.data.range"), Is.True);
        MetadataDirectory developer = document.Directories.Single(item => item.Path == "TGA/Developer");
        Assert.That(developer.Children, Has.Count.EqualTo(2));
        Assert.That(developer.Children[1].Tags[0].Value.IntegerValue, Is.EqualTo(77));
        Assert.That(document.RawBlocks.Any(item => item.BlockType == "DeveloperData.77" && item.Offset == 630 && item.Length == 3), Is.True);
        Assert.That(document.RawBlocks.Any(item => item.BlockType == "DeveloperData.42"), Is.False);
    }

    [Test]
    public void Parse_DeveloperCountHonorsTagLimit()
    {
        byte[] bytes = CreateVersion2Tga(2);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 1;

        ImageMetadataDocument document = ParseDirect(bytes, options);

        Assert.That(HasDiagnostic(document, "tga.developer.count.limit"), Is.True);
        Assert.That(document.Directories.Select(item => item.Path), Does.Not.Contain("TGA/Developer"));
        Assert.That(document.Directories.Select(item => item.Path), Does.Contain("TGA/Extension"));
    }

    [Test]
    public void Parse_RawBlockCaptureHonorsOptionsAndLimit()
    {
        byte[] bytes = CreateVersion2Tga(1);
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = ParseDirect(bytes, captured);

        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data.Length, Is.EqualTo(18));
        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "Extension").Data.Length, Is.EqualTo(495));
        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "DeveloperData.42").Data, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 30;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data.Length, Is.EqualTo(18));
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "Extension").Data, Is.Empty);
        Assert.That(HasDiagnostic(limitedDocument, "tga.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(ParseDirect(bytes, disabled).RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_InvalidExtensionTimestampWarnsAndOmitsNormalizedTime()
    {
        byte[] bytes = CreateVersion2Tga(0);
        WriteUnsigned16(bytes, 64 + 367, 2);
        WriteUnsigned16(bytes, 64 + 369, 30);
        WriteUnsigned16(bytes, 64 + 371, 2025);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "tga.extension.timestamp.invalid"), Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tga.Extension.Timestamp"), Is.Null);
        Assert.That(document.Summary.CaptureTimeText, Is.Empty);
    }

    public static byte[] CreateVersion2Tga(int developerCount)
    {
        byte[] bytes = new byte[700];
        WriteHeader(bytes, 0, 0, 2, 0, 0, 0, 3, 5, 320, 200, 32, 40);
        int extension = 64;
        WriteUnsigned16(bytes, extension, 495);
        WriteText(bytes, extension + 2, 41, "Ada Artist");
        WriteText(bytes, extension + 43, 81, "First line");
        WriteText(bytes, extension + 124, 81, "Second line");
        WriteUnsigned16(bytes, extension + 367, 8);
        WriteUnsigned16(bytes, extension + 369, 12);
        WriteUnsigned16(bytes, extension + 371, 2026);
        WriteUnsigned16(bytes, extension + 373, 14);
        WriteUnsigned16(bytes, extension + 375, 35);
        WriteUnsigned16(bytes, extension + 377, 9);
        WriteText(bytes, extension + 379, 41, "Nightly render");
        WriteUnsigned16(bytes, extension + 420, 1);
        WriteUnsigned16(bytes, extension + 422, 2);
        WriteUnsigned16(bytes, extension + 424, 3);
        WriteText(bytes, extension + 426, 41, "RenderLab");
        WriteUnsigned16(bytes, extension + 467, 250);
        bytes[extension + 469] = (byte)'b';
        bytes[extension + 470] = 1;
        bytes[extension + 471] = 2;
        bytes[extension + 472] = 3;
        bytes[extension + 473] = 4;
        WriteUnsigned16(bytes, extension + 474, 4);
        WriteUnsigned16(bytes, extension + 476, 3);
        WriteUnsigned16(bytes, extension + 478, 22);
        WriteUnsigned16(bytes, extension + 480, 10);
        bytes[extension + 494] = 3;

        int developer = 559;
        WriteUnsigned16(bytes, developer, developerCount);
        if (developerCount > 0)
        {
            WriteUnsigned16(bytes, developer + 2, 42);
            WriteUnsigned32(bytes, developer + 4, 620);
            WriteUnsigned32(bytes, developer + 8, 4);
            bytes[620] = 10;
            bytes[621] = 20;
            bytes[622] = 30;
            bytes[623] = 40;
        }
        if (developerCount > 1)
        {
            WriteUnsigned16(bytes, developer + 12, 43);
            WriteUnsigned32(bytes, developer + 14, 624);
            WriteUnsigned32(bytes, developer + 18, 2);
            bytes[624] = 50;
            bytes[625] = 60;
        }
        WriteFooter(bytes, extension, developer);
        return bytes;
    }

    public static byte[] CreateHeaderOnly(int imageType, int pixelDepth, int descriptor)
    {
        byte[] bytes = new byte[18];
        WriteHeader(bytes, 0, 0, imageType, 0, 0, 0, 0, 0, 16, 8, pixelDepth, descriptor);
        return bytes;
    }

    public static void WriteHeader(byte[] bytes, int idLength, int colorMapType, int imageType, int colorMapFirst, int colorMapLength, int colorMapBits, int xOrigin, int yOrigin, int width, int height, int pixelDepth, int descriptor)
    {
        bytes[0] = (byte)idLength;
        bytes[1] = (byte)colorMapType;
        bytes[2] = (byte)imageType;
        WriteUnsigned16(bytes, 3, colorMapFirst);
        WriteUnsigned16(bytes, 5, colorMapLength);
        bytes[7] = (byte)colorMapBits;
        WriteUnsigned16(bytes, 8, xOrigin);
        WriteUnsigned16(bytes, 10, yOrigin);
        WriteUnsigned16(bytes, 12, width);
        WriteUnsigned16(bytes, 14, height);
        bytes[16] = (byte)pixelDepth;
        bytes[17] = (byte)descriptor;
    }

    public static void WriteFooter(byte[] bytes, int extensionOffset, int developerOffset)
    {
        int offset = bytes.Length - 26;
        WriteUnsigned32(bytes, offset, extensionOffset);
        WriteUnsigned32(bytes, offset + 4, developerOffset);
        WriteText(bytes, offset + 8, 17, "TRUEVISION-XFILE.");
        bytes[offset + 25] = 0;
    }

    public static void WriteText(byte[] bytes, int offset, int length, string text)
    {
        int count = text.Length < length ? text.Length : length;
        int index = 0;
        while (index < count)
        {
            bytes[offset + index] = (byte)text[index];
            index = index + 1;
        }
    }

    public static void WriteUnsigned16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, long value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TgaMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tga), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
    }
}
