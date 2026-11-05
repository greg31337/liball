using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PsdMetadataParserTests
{
    [Test]
    public void ImageMetadataReader_ParsesPsdHeaderSectionsAndCompositeCompression()
    {
        byte[] bytes = CreateDocument(
            1,
            4,
            1080,
            1920,
            8,
            3,
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5 },
            new byte[] { 6, 7, 8, 9 },
            1,
            new byte[] { 10, 11 });
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Psd));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(5));
        Assert.That(document.Directories[0].Path, Is.EqualTo("PSD/Header"));
        Assert.That(document.Directories[1].Path, Is.EqualTo("PSD/ColorModeData"));
        Assert.That(document.Directories[2].Path, Is.EqualTo("PSD/ImageResources"));
        Assert.That(document.Directories[3].Path, Is.EqualTo("PSD/LayerAndMask"));
        Assert.That(document.Directories[4].Path, Is.EqualTo("PSD/ImageData"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Header.Width").Value.UnsignedIntegerValue, Is.EqualTo(1920));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Header.Height").Value.UnsignedIntegerValue, Is.EqualTo(1080));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Header.ColorMode").DisplayValue, Is.EqualTo("RGB"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ColorModeData.DeclaredLength").Value.UnsignedIntegerValue, Is.EqualTo(3));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageResources.DeclaredLength").Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LengthFieldSize").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageData.Compression").DisplayValue, Is.EqualTo("RLE"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(1920));
        Assert.That(document.Frames[0].Height, Is.EqualTo(1080));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Summary.Width, Is.EqualTo(1920));
        Assert.That(document.Summary.Height, Is.EqualTo(1080));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void ImageMetadataReader_UsesEightByteLayerSectionLengthForPsb()
    {
        byte[] bytes = CreateDocument(
            2,
            4,
            50000,
            40000,
            16,
            4,
            new byte[0],
            new byte[] { 1 },
            new byte[] { 2, 3, 4 },
            3,
            new byte[] { 5 });
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Psb));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.Header.Version").DisplayValue, Is.EqualTo("PSB"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LengthFieldSize").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.DeclaredLength").Value.UnsignedIntegerValue, Is.EqualTo(3));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Cmyk));
        Assert.That(document.Frames[0].HasAlpha, Is.False);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Deflate));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReportsInvalidHeaderFieldsAndOnlyCreatesFrameForUsableHeader()
    {
        byte[] bytes = CreateDocument(1, 0, 0, 40000, 7, 6, new byte[0], new byte[0], new byte[0], 0, new byte[0]);
        bytes[6] = 1;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "psd.reserved.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "psd.channels.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "psd.dimensions.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "psd.depth.unsupported", MetadataSeverity.Unsupported);
        AssertDiagnostic(document, "psd.color-mode.unsupported", MetadataSeverity.Unsupported);
        Assert.That(document.Directories, Has.Count.EqualTo(5));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_StopsAfterInvalidSignatureOrUnsupportedVersion()
    {
        byte[] badSignature = CreateDocument(1, 3, 10, 10, 8, 3, new byte[0], new byte[0], new byte[0], 0, new byte[0]);
        badSignature[0] = 0;
        byte[] badVersion = CreateDocument(3, 3, 10, 10, 8, 3, new byte[0], new byte[0], new byte[0], 0, new byte[0]);

        ImageMetadataDocument signatureDocument = Parse(badSignature, ImageFormatKind.Psd, new ImageMetadataReaderOptions());
        ImageMetadataDocument versionDocument = Parse(badVersion, ImageFormatKind.Psd, new ImageMetadataReaderOptions());

        AssertDiagnostic(signatureDocument, "psd.signature.invalid", MetadataSeverity.RecoverableError);
        Assert.That(signatureDocument.Directories, Has.Count.EqualTo(1));
        Assert.That(signatureDocument.Frames, Is.Empty);
        AssertDiagnostic(versionDocument, "psd.version.unsupported", MetadataSeverity.Unsupported);
        Assert.That(versionDocument.Directories, Has.Count.EqualTo(1));
        Assert.That(versionDocument.Frames, Is.Empty);
    }

    [Test]
    public void Parse_ReportsTruncatedHeaderLengthPayloadAndCompression()
    {
        ImageMetadataDocument header = Parse(new byte[] { 56, 66, 80, 83, 0, 1 }, ImageFormatKind.Psd, new ImageMetadataReaderOptions());
        AssertDiagnostic(header, "psd.header.truncated", MetadataSeverity.Truncated);
        Assert.That(header.Directories, Is.Empty);

        byte[] noColorLength = CreateHeader(1, 3, 10, 10, 8, 3);
        ImageMetadataDocument length = Parse(noColorLength, ImageFormatKind.Psd, new ImageMetadataReaderOptions());
        AssertDiagnostic(length, "psd.section.length.truncated", MetadataSeverity.Truncated);
        Assert.That(length.Directories, Has.Count.EqualTo(1));

        byte[] colorPastEnd = Join(CreateHeader(1, 3, 10, 10, 8, 3), Unsigned32(12), new byte[] { 1, 2 });
        ImageMetadataDocument payload = Parse(colorPastEnd, ImageFormatKind.Psd, new ImageMetadataReaderOptions());
        AssertDiagnostic(payload, "psd.section.truncated", MetadataSeverity.Truncated);
        Assert.That(payload.Directories, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(payload, "Psd.ColorModeData.AvailableLength").Value.IntegerValue, Is.EqualTo(2));

        byte[] noCompression = Join(CreateHeader(1, 3, 10, 10, 8, 3), Unsigned32(0), Unsigned32(0), Unsigned32(0));
        ImageMetadataDocument compression = Parse(noCompression, ImageFormatKind.Psd, new ImageMetadataReaderOptions());
        AssertDiagnostic(compression, "psd.image-data.compression.truncated", MetadataSeverity.Truncated);
        Assert.That(compression.Directories, Has.Count.EqualTo(5));
    }

    [Test]
    public void Parse_SkipsLimitedSectionsAndContinuesStructurally()
    {
        byte[] bytes = CreateDocument(1, 3, 20, 30, 8, 3, new byte[] { 1, 2, 3 }, new byte[] { 4, 5 }, new byte[] { 6 }, 0, new byte[] { 7, 8 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 1;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Psd, options);

        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "psd.section.length.limit"));
        Assert.That(document.Directories, Has.Count.EqualTo(5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ColorModeData.Limited").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageResources.Limited").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageData.Compression").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
    }

    [Test]
    public void Parse_ReportsUnsupportedCompositeCompressionAndFrameLimit()
    {
        byte[] bytes = CreateDocument(1, 3, 20, 30, 32, 9, new byte[0], new byte[0], new byte[0], 9, new byte[0]);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 0;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Psd, options);

        AssertDiagnostic(document, "psd.frame.count.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "psd.image-data.compression.unsupported", MetadataSeverity.Unsupported);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageData.Compression").DisplayValue, Is.EqualTo("Unknown"));
    }

    [Test]
    public void Parse_PreservesPsbLayerLengthBeyondThirtyTwoBits()
    {
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, CreateHeader(2, 3, 10, 10, 8, 3));
        AddUnsigned32(bytes, 0);
        AddUnsigned32(bytes, 0);
        AddUnsigned64(bytes, 4294967297);

        ImageMetadataDocument document = Parse(bytes.ToArray(), ImageFormatKind.Psb, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "psd.section.truncated", MetadataSeverity.Truncated);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.DeclaredLength").Value.UnsignedIntegerValue, Is.EqualTo(4294967297));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.LayerAndMask.LengthFieldSize").Value.IntegerValue, Is.EqualTo(8));
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageFormatKind kind, ImageMetadataReaderOptions options)
    {
        MemoryStream stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PsdDocumentParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
    }

    public static byte[] CreateDocument(int version, int channels, uint height, uint width, int depth, int colorMode, byte[] colorData, byte[] resources, byte[] layers, int compression, byte[] imageData)
    {
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, CreateHeader(version, channels, height, width, depth, colorMode));
        AddUnsigned32(bytes, (uint)colorData.Length);
        AddBytes(bytes, colorData);
        AddUnsigned32(bytes, (uint)resources.Length);
        AddBytes(bytes, resources);
        if (version == 2)
        {
            AddUnsigned64(bytes, (ulong)layers.Length);
        }
        else
        {
            AddUnsigned32(bytes, (uint)layers.Length);
        }

        AddBytes(bytes, layers);
        AddUnsigned16(bytes, (uint)compression);
        AddBytes(bytes, imageData);
        return bytes.ToArray();
    }

    public static byte[] CreateHeader(int version, int channels, uint height, uint width, int depth, int colorMode)
    {
        List<byte> bytes = new List<byte>();
        AddAscii(bytes, "8BPS");
        AddUnsigned16(bytes, (uint)version);
        AddBytes(bytes, new byte[6]);
        AddUnsigned16(bytes, (uint)channels);
        AddUnsigned32(bytes, height);
        AddUnsigned32(bytes, width);
        AddUnsigned16(bytes, (uint)depth);
        AddUnsigned16(bytes, (uint)colorMode);
        return bytes.ToArray();
    }

    public static byte[] Unsigned32(uint value)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, value);
        return bytes.ToArray();
    }

    public static byte[] Join(params byte[][] arrays)
    {
        List<byte> bytes = new List<byte>();
        foreach (byte[] array in arrays)
        {
            AddBytes(bytes, array);
        }

        return bytes.ToArray();
    }

    public static void AddUnsigned16(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned64(List<byte> bytes, ulong value)
    {
        for (int shift = 56; shift >= 0; shift -= 8)
        {
            bytes.Add((byte)(value >> shift));
        }
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        foreach (char value in text)
        {
            bytes.Add((byte)value);
        }
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }
}
