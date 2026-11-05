using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsExifProfileAndHeaderFields()
    {
        byte[] exif = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0, 0, 0, 0, 0 };
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "eXIf", "IEND" },
            new byte[][] { Ihdr(6), exif, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Exif));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Exif.PNG.eXIf"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("image/tiff"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(exif));
        Assert.That(IntegerValue(document, "Png.eXIf.ByteLength"), Is.EqualTo(exif.Length));
        Assert.That(TextValue(document, "Png.eXIf.ByteOrder"), Is.EqualTo("LittleEndian"));
    }

    [Test]
    public void Parse_ExifProfileLimitPreservesRangeWithoutPayload()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;
        byte[] exif = new byte[] { 77, 77, 0, 42, 0, 0, 0, 8 };

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "eXIf", "IEND" },
            new byte[][] { Ihdr(6), exif, new byte[0] }), options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(8));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        AssertDiagnostic(document, "png.exif.profile.limit");
    }

    [Test]
    public void Parse_ExtractsLatin1CompressedAndInternationalText()
    {
        byte[] text = Latin1Payload("Author", "René");
        byte[] compressedText = CompressedTextPayload("Description", "compressed caption");
        byte[] international = InternationalTextPayload("Title", 0, "fr", "Titre", Encoding.UTF8.GetBytes("Café"));
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "tEXt", "zTXt", "iTXt", "IEND" },
            new byte[][] { Ihdr(6), text, compressedText, international, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(TextValue(document, "Png.tEXt.Keyword"), Is.EqualTo("Author"));
        Assert.That(TextValue(document, "Png.tEXt.Text"), Is.EqualTo("René"));
        Assert.That(TextValue(document, "Png.zTXt.Keyword"), Is.EqualTo("Description"));
        Assert.That(TextValue(document, "Png.zTXt.Text"), Is.EqualTo("compressed caption"));
        Assert.That(BooleanValue(document, "Png.zTXt.Compressed"), Is.True);
        Assert.That(TextValue(document, "Png.iTXt.Keyword"), Is.EqualTo("Title"));
        Assert.That(TextValue(document, "Png.iTXt.LanguageTag"), Is.EqualTo("fr"));
        Assert.That(TextValue(document, "Png.iTXt.TranslatedKeyword"), Is.EqualTo("Titre"));
        Assert.That(TextValue(document, "Png.iTXt.Text"), Is.EqualTo("Café"));
    }

    [Test]
    public void Parse_CompressedInternationalXmpCreatesProfileWithinLimit()
    {
        string xml = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>";
        byte[] payload = InternationalTextPayload("XML:com.adobe.xmp", 1, "", "", Compress(Encoding.UTF8.GetBytes(xml)));
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "iTXt", "IEND" },
            new byte[][] { Ihdr(6), payload, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(BooleanValue(document, "Png.iTXt.Compressed"), Is.True);
        Assert.That(TextValue(document, "Png.iTXt.Text"), Is.EqualTo(xml));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.PNG.iTXt"));
        Assert.That(document.Profiles[0].Text, Is.EqualTo(xml));
    }

    [Test]
    public void Parse_ExtractsPhysicalDimensionsAndModificationTime()
    {
        List<byte> physical = new List<byte>();
        PngChunkTraverserTests.AddUnsigned32(physical, 3780);
        PngChunkTraverserTests.AddUnsigned32(physical, 1890);
        physical.Add(1);
        byte[] time = new byte[] { 7, 232, 6, 30, 23, 59, 60 };
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "pHYs", "tIME", "IEND" },
            new byte[][] { Ihdr(6), physical.ToArray(), time, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(UnsignedValue(document, "Png.pHYs.PixelsPerUnitX"), Is.EqualTo(3780));
        Assert.That(UnsignedValue(document, "Png.pHYs.PixelsPerUnitY"), Is.EqualTo(1890));
        Assert.That(IntegerValue(document, "Png.pHYs.UnitSpecifier"), Is.EqualTo(1));
        MetadataValue ratio = RequiredMetadataQuery.FindFirst(document, "Png.pHYs.PixelAspectRatio").Value;
        Assert.That(ratio.Numerator, Is.EqualTo(3780));
        Assert.That(ratio.Denominator, Is.EqualTo(1890));
        Assert.That(document.Summary.PixelAspectRatio, Is.EqualTo(2));
        Assert.That(document.Summary.HorizontalResolution, Is.EqualTo(3780));
        Assert.That(document.Summary.VerticalResolution, Is.EqualTo(1890));
        Assert.That(document.Summary.ResolutionUnit, Is.EqualTo("metre"));
        Assert.That(document.Frames[0].PixelAspectRatio, Is.EqualTo(2));
        Assert.That(document.Frames[0].HorizontalResolution, Is.EqualTo(3780));
        Assert.That(document.Frames[0].VerticalResolution, Is.EqualTo(1890));
        Assert.That(document.Frames[0].ResolutionUnit, Is.EqualTo("metre"));
        Assert.That(TextValue(document, "Png.tIME.LastModificationTime"), Is.EqualTo("2024-06-30T23:59:60Z"));
    }

    [Test]
    public void Parse_UsesCreationTimeTextForNormalizedCaptureTime()
    {
        string created = "2026-08-12T10:11:12-07:00";
        byte[] time = new byte[] { 7, 234, 8, 13, 1, 2, 3 };
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "tIME", "tEXt", "IEND" },
            new byte[][] { Ihdr(6), time, Latin1Payload("Creation Time", created), new byte[0] }), new ImageMetadataReaderOptions());

        MetadataTag creation = RequiredMetadataQuery.FindFirst(document, "Png.Text.CreationTime");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(creation, Is.Not.Null);
        Assert.That(creation.Value.Kind, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(creation.Value.Text, Is.EqualTo(created));
        Assert.That(document.Summary.CaptureTimeText, Is.EqualTo(created));
    }

    [Test]
    public void Parse_NormalizesNamedTextFields()
    {
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "tEXt", "tEXt", "tEXt", "tEXt", "tEXt", "tEXt", "IEND" },
            new byte[][]
            {
                Ihdr(6),
                Latin1Payload("Author", "Ada"),
                Latin1Payload("Copyright", "Copyright 2026"),
                Latin1Payload("Title", "Sunset"),
                Latin1Payload("Description", "Evening light"),
                Latin1Payload("Keywords", "travel; coast"),
                Latin1Payload("Software", "Image Tool"),
                new byte[0]
            }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary.Author, Is.EqualTo("Ada"));
        Assert.That(document.Summary.Copyright, Is.EqualTo("Copyright 2026"));
        Assert.That(document.Summary.Title, Is.EqualTo("Sunset"));
        Assert.That(document.Summary.Description, Is.EqualTo("Evening light"));
        Assert.That(document.Summary.Keywords, Is.EqualTo(new[] { "travel", "coast" }));
        Assert.That(document.Summary.Software, Is.EqualTo("Image Tool"));
    }

    [TestCase(0, 2, "Png.tRNS.GraySample", 17)]
    [TestCase(2, 6, "Png.tRNS.RedSample", 17)]
    [TestCase(3, 3, "Png.tRNS.AlphaValues", 0)]
    public void Parse_ExtractsTransparencyForColorType(int colorType, int length, string key, int expected)
    {
        byte[] transparency = new byte[length];
        transparency[0] = 0;
        if (length > 1)
        {
            transparency[1] = 17;
        }

        if (colorType == 3)
        {
            transparency[0] = 0;
            transparency[1] = 128;
            transparency[2] = 255;
        }

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "tRNS", "IEND" },
            new byte[][] { Ihdr(colorType), transparency, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        if (colorType == 3)
        {
            Assert.That(tag.Value.Bytes, Is.EqualTo(transparency));
        }
        else
        {
            Assert.That(tag.Value.IntegerValue, Is.EqualTo(expected));
        }
    }

    [TestCase(0, 2, "Png.bKGD.GraySample", 34)]
    [TestCase(2, 6, "Png.bKGD.RedSample", 34)]
    [TestCase(3, 1, "Png.bKGD.PaletteIndex", 2)]
    public void Parse_ExtractsBackgroundForColorType(int colorType, int length, string key, int expected)
    {
        byte[] background = new byte[length];
        background[0] = colorType == 3 ? (byte)2 : (byte)0;
        if (length > 1)
        {
            background[1] = 34;
        }

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "bKGD", "IEND" },
            new byte[][] { Ihdr(colorType), background, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(IntegerValue(document, key), Is.EqualTo(expected));
    }

    [Test]
    public void Parse_ApngExtractsControlsFramesTimingAndFrameData()
    {
        byte[] animationControl = Unsigned32Values(new uint[] { 2, 0 });
        byte[] firstFrame = FrameControl(0, 32, 24, 0, 0, 1, 10, 0, 0);
        byte[] secondFrame = FrameControl(1, 16, 12, 8, 6, 1, 20, 1, 1);
        byte[] frameData = Join(Unsigned32Values(new uint[] { 2 }), new byte[] { 120, 1, 2 });
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "acTL", "fcTL", "IDAT", "fcTL", "fdAT", "IEND" },
            new byte[][] { Ihdr(6), animationControl, firstFrame, new byte[0], secondFrame, frameData, new byte[0] }), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(UnsignedValue(document, "Png.acTL.FrameCount"), Is.EqualTo(2));
        Assert.That(UnsignedValue(document, "Png.acTL.PlayCount"), Is.Zero);
        Assert.That(document.Frames, Has.Count.EqualTo(2));
        Assert.That(document.Frames[0].Width, Is.EqualTo(32));
        Assert.That(document.Frames[0].Height, Is.EqualTo(24));
        Assert.That(document.Frames[0].DurationMilliseconds, Is.EqualTo(100));
        Assert.That(document.Frames[0].IsAnimated, Is.True);
        Assert.That(document.Frames[1].Width, Is.EqualTo(16));
        Assert.That(document.Frames[1].Height, Is.EqualTo(12));
        Assert.That(document.Frames[1].CanvasWidth, Is.EqualTo(32));
        Assert.That(document.Frames[1].CanvasX, Is.EqualTo(8));
        Assert.That(document.Frames[1].CanvasY, Is.EqualTo(6));
        Assert.That(document.Frames[1].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[1].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(new ImageMetadataQuery().Find(document, "Png.fcTL.SequenceNumber"), Has.Count.EqualTo(2));
        Assert.That(UnsignedValue(document, "Png.fdAT.SequenceNumber"), Is.EqualTo(2));
        Assert.That(IntegerValue(document, "Png.fdAT.FrameDataByteLength"), Is.EqualTo(3));
    }

    [Test]
    public void Parse_MalformedMetadataChunksProduceDiagnosticsAndPartialResults()
    {
        byte[] badInternational = new byte[] { 75, 0, 2, 1, 0, 0 };
        byte[] badFrame = FrameControl(0, 40, 30, 0, 0, 1, 0, 3, 2);
        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "eXIf", "tEXt", "zTXt", "iTXt", "pHYs", "tIME", "tRNS", "bKGD", "acTL", "fcTL", "fdAT", "IEND" },
            new byte[][]
            {
                Ihdr(6),
                new byte[] { 1, 2, 3, 4 },
                Encoding.ASCII.GetBytes("missing separator"),
                new byte[] { 75, 0 },
                badInternational,
                new byte[8],
                new byte[] { 7, 232, 13, 0, 24, 60, 61 },
                new byte[] { 0 },
                new byte[] { 0 },
                Unsigned32Values(new uint[] { 2, 1 }),
                badFrame,
                new byte[] { 0, 0, 0 },
                new byte[0]
            }), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "png.exif.tiff.header.invalid");
        AssertDiagnostic(document, "png.text.keyword.invalid");
        AssertDiagnostic(document, "png.ztxt.compression.truncated");
        AssertDiagnostic(document, "png.itxt.compression.flag.invalid");
        AssertDiagnostic(document, "png.phys.length.invalid");
        AssertDiagnostic(document, "png.time.value.invalid");
        AssertDiagnostic(document, "png.trns.layout.invalid");
        AssertDiagnostic(document, "png.bkgd.layout.invalid");
        AssertDiagnostic(document, "png.actl.frame-count.mismatch");
        AssertDiagnostic(document, "png.fctl.bounds.invalid");
        AssertDiagnostic(document, "png.fctl.dispose.invalid");
        AssertDiagnostic(document, "png.fctl.blend.invalid");
        AssertDiagnostic(document, "png.fdat.truncated");
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code), code);
    }

    public static long IntegerValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.IntegerValue;
    }

    public static ulong UnsignedValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.UnsignedIntegerValue;
    }

    public static bool BooleanValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.BooleanValue;
    }

    public static string TextValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.Text;
    }

    public static byte[] Ihdr(int colorType)
    {
        return PngIhdrExtractorTests.CreateIhdr(32, 24, 8, colorType, 0, 0, 0);
    }

    public static byte[] CreatePng(string[] types, byte[][] payloads)
    {
        return PngChunkTraverserTests.CreatePng(types, payloads);
    }

    public static ImageMetadataDocument ParsePng(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return PngMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), options);
    }

    public static byte[] Latin1Payload(string keyword, string text)
    {
        return Join(Encoding.Latin1.GetBytes(keyword), new byte[] { 0 }, Encoding.Latin1.GetBytes(text));
    }

    public static byte[] CompressedTextPayload(string keyword, string text)
    {
        return Join(Encoding.Latin1.GetBytes(keyword), new byte[] { 0, 0 }, Compress(Encoding.Latin1.GetBytes(text)));
    }

    public static byte[] InternationalTextPayload(string keyword, int compressionFlag, string language, string translated, byte[] text)
    {
        return Join(
            Encoding.Latin1.GetBytes(keyword),
            new byte[] { 0, (byte)compressionFlag, 0 },
            Encoding.ASCII.GetBytes(language),
            new byte[] { 0 },
            Encoding.UTF8.GetBytes(translated),
            new byte[] { 0 },
            text);
    }

    public static byte[] Compress(byte[] bytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZLibStream zlib = new ZLibStream(output, CompressionLevel.SmallestSize, true))
            {
                zlib.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }
    }

    public static byte[] FrameControl(uint sequence, uint width, uint height, uint x, uint y, int delayNumerator, int delayDenominator, int dispose, int blend)
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddUnsigned32(bytes, sequence);
        PngChunkTraverserTests.AddUnsigned32(bytes, width);
        PngChunkTraverserTests.AddUnsigned32(bytes, height);
        PngChunkTraverserTests.AddUnsigned32(bytes, x);
        PngChunkTraverserTests.AddUnsigned32(bytes, y);
        PngColorExtractorTests.AddUnsigned16(bytes, delayNumerator);
        PngColorExtractorTests.AddUnsigned16(bytes, delayDenominator);
        bytes.Add((byte)dispose);
        bytes.Add((byte)blend);
        return bytes.ToArray();
    }

    public static byte[] Unsigned32Values(uint[] values)
    {
        return PngColorExtractorTests.Unsigned32Values(values);
    }

    public static byte[] Join(params byte[][] arrays)
    {
        List<byte> bytes = new List<byte>();
        foreach (byte[] array in arrays)
        {
            bytes.AddRange(array);
        }

        return bytes.ToArray();
    }
}
