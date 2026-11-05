using System.Collections.Generic;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PcxMetadataParserTests
{
    [Test]
    public void Parse_ExtractsRgbHeaderFramePaletteAndResolution()
    {
        byte[] palette = CreatePalette();
        byte[] bytes = CreateHeader(5, 8, 10, 20, 109, 69, 300, 240, 3, 100, 1, 1920, 1080, palette);
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Pcx));
        Assert.That(Find(document, "Pcx.Manufacturer").DisplayValue, Is.EqualTo("ZSoft PC Paintbrush"));
        Assert.That(Find(document, "Pcx.Version").Value.UnsignedIntegerValue, Is.EqualTo(5));
        Assert.That(Find(document, "Pcx.Version").DisplayValue, Is.EqualTo("PC Paintbrush 3.0 or later"));
        Assert.That(Find(document, "Pcx.Width").Value.IntegerValue, Is.EqualTo(100));
        Assert.That(Find(document, "Pcx.Height").Value.IntegerValue, Is.EqualTo(50));
        Assert.That(Find(document, "Pcx.HorizontalDpi").Value.IntegerValue, Is.EqualTo(300));
        Assert.That(Find(document, "Pcx.VerticalDpi").Value.IntegerValue, Is.EqualTo(240));
        Assert.That(Find(document, "Pcx.PlaneCount").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(Find(document, "Pcx.TotalBitsPerPixel").Value.IntegerValue, Is.EqualTo(24));
        Assert.That(Find(document, "Pcx.BytesPerLine").Value.IntegerValue, Is.EqualTo(100));
        Assert.That(Find(document, "Pcx.DecodedRowBytes").Value.IntegerValue, Is.EqualTo(300));
        Assert.That(Find(document, "Pcx.PaletteType").DisplayValue, Is.EqualTo("Color or black and white"));
        Assert.That(Find(document, "Pcx.HorizontalScreenSize").Value.IntegerValue, Is.EqualTo(1920));
        Assert.That(Find(document, "Pcx.VerticalScreenSize").Value.IntegerValue, Is.EqualTo(1080));
        Assert.That(Find(document, "Pcx.Palette.EntryCount").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(Find(document, "Pcx.Palette.Entries").Value.Bytes, Is.EqualTo(palette));
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "PCX/Header", "PCX/Palette" }));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(100));
        Assert.That(document.Frames[0].Height, Is.EqualTo(50));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Frames[0].SourceColorType, Is.EqualTo("Rgb"));
        Assert.That(document.Frames[0].HorizontalResolution, Is.EqualTo(300));
        Assert.That(document.Frames[0].VerticalResolution, Is.EqualTo(240));
        Assert.That(document.Frames[0].ResolutionUnit, Is.EqualTo("pixels/inch"));
        Assert.That(document.Summary.Width, Is.EqualTo(100));
        Assert.That(document.Summary.Height, Is.EqualTo(50));
        Assert.That(document.Summary.ResolutionText, Is.EqualTo("300 x 240 pixels/inch"));
        Assert.That(document.Summary.HorizontalResolution, Is.EqualTo(300));
        Assert.That(document.Summary.VerticalResolution, Is.EqualTo(240));
        Assert.That(document.Summary.ResolutionUnit, Is.EqualTo("pixels/inch"));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Offset, Is.EqualTo(0));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(128));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [TestCase(0, "PC Paintbrush 2.5")]
    [TestCase(2, "PC Paintbrush 2.8 with palette")]
    [TestCase(3, "PC Paintbrush 2.8 without palette")]
    [TestCase(4, "PC Paintbrush for Windows")]
    [TestCase(5, "PC Paintbrush 3.0 or later")]
    public void Parse_RecognizesDefinedVersions(int version, string versionName)
    {
        byte[] bytes = CreateHeader(version, 1, 0, 0, 15, 15, 72, 72, 4, 2, 1, 0, 0, CreatePalette());

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Pcx.Version").DisplayValue, Is.EqualTo(versionName));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Indexed));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(1));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
    }

    [Test]
    public void Parse_OnePlaneGrayscaleUsesPaletteInterpretation()
    {
        byte[] bytes = CreateHeader(5, 8, 0, 0, 31, 15, 96, 96, 1, 32, 2, 0, 0, CreatePalette());

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Gray));
        Assert.That(Find(document, "Pcx.PaletteType").DisplayValue, Is.EqualTo("Grayscale"));
    }

    [Test]
    public void Parse_NonzeroOriginUsesInclusiveCoordinateBounds()
    {
        byte[] bytes = CreateHeader(5, 4, 100, 200, 199, 249, 72, 72, 1, 50, 1, 0, 0, CreatePalette());

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(Find(document, "Pcx.XMinimum").Value.IntegerValue, Is.EqualTo(100));
        Assert.That(Find(document, "Pcx.YMinimum").Value.IntegerValue, Is.EqualTo(200));
        Assert.That(document.Frames[0].Width, Is.EqualTo(100));
        Assert.That(document.Frames[0].Height, Is.EqualTo(50));
    }

    [TestCase(0, 5, 1, 8, 1, "pcx.manufacturer.invalid")]
    [TestCase(10, 1, 1, 8, 1, "pcx.version.invalid")]
    [TestCase(10, 5, 0, 8, 1, "pcx.encoding.invalid")]
    [TestCase(10, 5, 1, 3, 1, "pcx.bits-per-plane.invalid")]
    [TestCase(10, 5, 1, 8, 0, "pcx.plane-count.invalid")]
    public void Parse_InvalidRequiredHeaderFieldsAreDiagnosed(int manufacturer, int version, int encoding, int bits, int planes, string code)
    {
        byte[] bytes = CreateHeader(version, bits, 0, 0, 15, 15, 72, 72, planes, 16, 1, 0, 0, CreatePalette());
        bytes[0] = (byte)manufacturer;
        bytes[2] = (byte)encoding;

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, code), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_ReversedBoundsAreRejected()
    {
        byte[] bytes = CreateHeader(5, 8, 20, 10, 19, 50, 72, 72, 1, 2, 1, 0, 0, CreatePalette());

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "pcx.bounds.invalid"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_ShortScanlineIsRejected()
    {
        byte[] bytes = CreateHeader(5, 8, 0, 0, 99, 49, 72, 72, 1, 50, 1, 0, 0, CreatePalette());

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "pcx.bytes-per-line.short"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_LegacyQuirksWarnButPreserveMetadata()
    {
        byte[] bytes = CreateHeader(5, 1, 0, 0, 7, 7, 72, 72, 1, 1, 7, 0, 0, CreatePalette());
        bytes[64] = 1;

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "pcx.bytes-per-line.odd"), Is.True);
        Assert.That(HasDiagnostic(document, "pcx.reserved.nonzero"), Is.True);
        Assert.That(HasDiagnostic(document, "pcx.palette-type.unknown"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(Find(document, "Pcx.PaletteType").DisplayValue, Is.EqualTo("Unknown"));
    }

    [Test]
    public void Parse_TruncatedHeaderReportsDiagnostic()
    {
        byte[] bytes = new byte[] { 10, 5, 1, 8, 0, 0 };

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "pcx.header.truncated"), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_RawHeaderCaptureHonorsOptionsAndLimit()
    {
        byte[] bytes = CreateHeader(5, 8, 0, 0, 15, 15, 72, 72, 1, 16, 1, 0, 0, CreatePalette());
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = Parse(bytes, captured);

        Assert.That(capturedDocument.RawBlocks[0].Data, Is.EqualTo(bytes));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 64;
        ImageMetadataDocument limitedDocument = Parse(bytes, limited);
        Assert.That(limitedDocument.RawBlocks[0].Data, Is.Empty);
        Assert.That(HasDiagnostic(limitedDocument, "pcx.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(Parse(bytes, disabled).RawBlocks, Is.Empty);
    }

    public static byte[] CreateHeader(int version, int bitsPerPlane, int xMin, int yMin, int xMax, int yMax, int horizontalDpi, int verticalDpi, int planeCount, int bytesPerLine, int paletteType, int horizontalScreenSize, int verticalScreenSize, byte[] palette)
    {
        byte[] bytes = new byte[128];
        bytes[0] = 10;
        bytes[1] = (byte)version;
        bytes[2] = 1;
        bytes[3] = (byte)bitsPerPlane;
        WriteUnsigned16(bytes, 4, xMin);
        WriteUnsigned16(bytes, 6, yMin);
        WriteUnsigned16(bytes, 8, xMax);
        WriteUnsigned16(bytes, 10, yMax);
        WriteUnsigned16(bytes, 12, horizontalDpi);
        WriteUnsigned16(bytes, 14, verticalDpi);
        int paletteLength = palette.Length < 48 ? palette.Length : 48;
        for (int index = 0; index < paletteLength; index++)
        {
            bytes[16 + index] = palette[index];
        }
        bytes[65] = (byte)planeCount;
        WriteUnsigned16(bytes, 66, bytesPerLine);
        WriteUnsigned16(bytes, 68, paletteType);
        WriteUnsigned16(bytes, 70, horizontalScreenSize);
        WriteUnsigned16(bytes, 72, verticalScreenSize);
        return bytes;
    }

    public static byte[] CreatePalette()
    {
        byte[] palette = new byte[48];
        for (int index = 0; index < palette.Length; index++)
        {
            palette[index] = (byte)(index * 5);
        }
        return palette;
    }

    public static void WriteUnsigned16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PcxMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Pcx), options);
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
