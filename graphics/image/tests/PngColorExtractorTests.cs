using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PngColorExtractorTests
{
    [Test]
    public void Parse_ExtractsPaletteGammaChromaticitiesAndSrgbIntent()
    {
        byte[] palette = new byte[] { 255, 0, 0, 0, 255, 0 };
        byte[] gamma = Unsigned32(45455);
        byte[] chromaticities = Unsigned32Values(new uint[] { 31270, 32900, 64000, 33000, 30000, 60000, 15000, 6000 });
        byte[] png = CreatePng(
            new string[] { "IHDR", "gAMA", "cHRM", "sRGB", "PLTE", "IEND" },
            new byte[][] { Ihdr(), gamma, chromaticities, new byte[] { 1 }, palette, new byte[0] });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(IntegerValue(document, "Png.PLTE.EntryCount"), Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.PLTE.Entries").Value.Bytes, Is.EqualTo(palette));
        Assert.That(UnsignedValue(document, "Png.gAMA.EncodedValue"), Is.EqualTo(45455));
        Assert.That(DoubleValue(document, "Png.gAMA.Gamma"), Is.EqualTo(0.45455).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cHRM.WhitePointX"), Is.EqualTo(0.3127).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cHRM.GreenY"), Is.EqualTo(0.6).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cHRM.BlueY"), Is.EqualTo(0.06).Within(0.000001));
        Assert.That(IntegerValue(document, "Png.sRGB.RenderingIntent"), Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.sRGB.RenderingIntent").DisplayValue, Is.EqualTo("Relative colorimetric"));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
    }

    [Test]
    public void Parse_ExtractsAndDecompressesIccProfile()
    {
        byte[] profileData = IccProfileParserTests.CreateProfile(false);
        byte[] iccp = IccPayload("Display profile", 0, Compress(profileData));
        byte[] png = CreatePng(
            new string[] { "IHDR", "iCCP", "IEND" },
            new byte[][] { Ihdr(), iccp, new byte[0] });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.PNG.iCCP"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("Display profile"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/vnd.iccprofile"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(profileData));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(profileData.Length));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(TextValue(document, "Png.iCCP.ProfileName"), Is.EqualTo("Display profile"));
        Assert.That(IntegerValue(document, "Png.iCCP.CompressionMethod"), Is.Zero);
        Assert.That(IntegerValue(document, "Png.iCCP.ProfileByteLength"), Is.EqualTo(profileData.Length));
    }

    [Test]
    public void Parse_ParseIccFalseKeepsChunkFieldsWithoutDecompressingProfile()
    {
        byte[] iccp = IccPayload("Profile", 0, Compress(new byte[] { 1, 2, 3 }));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseIcc = false;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "iCCP", "IEND" },
            new byte[][] { Ihdr(), iccp, new byte[0] }), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(TextValue(document, "Png.iCCP.ProfileName"), Is.EqualTo("Profile"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Png.iCCP.ProfileByteLength"), Is.Null);
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Profile"));
    }

    [Test]
    public void Parse_IccProfileHonorsDecompressionAndProfileLimits()
    {
        byte[] iccp = IccPayload("Limited", 0, Compress(Encoding.ASCII.GetBytes("abcdefghij")));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;
        options.MaxDecompressedBytes = 20;

        ImageMetadataDocument document = ParsePng(CreatePng(
            new string[] { "IHDR", "iCCP", "IEND" },
            new byte[][] { Ihdr(), iccp, new byte[0] }), options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(Encoding.ASCII.GetBytes("abcd")));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == "zlib.limit" && diagnostic.Severity == MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_ExtractsCicpMasteringDisplayAndContentLightLevels()
    {
        byte[] cicp = new byte[] { 12, 13, 0, 1 };
        byte[] mdcv = MdcvPayload();
        byte[] clli = Unsigned32Values(new uint[] { 10000000, 2500000 });
        byte[] png = CreatePng(
            new string[] { "IHDR", "cICP", "mDCV", "cLLI", "IEND" },
            new byte[][] { Ihdr(), cicp, mdcv, clli, new byte[0] });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(IntegerValue(document, "Png.cICP.ColorPrimaries"), Is.EqualTo(12));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.cICP.ColorPrimaries").DisplayValue, Is.EqualTo("Display P3"));
        Assert.That(IntegerValue(document, "Png.cICP.TransferFunction"), Is.EqualTo(13));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.cICP.VideoFullRange").Value.BooleanValue, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(DoubleValue(document, "Png.mDCV.RedX"), Is.EqualTo(0.68).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.mDCV.GreenY"), Is.EqualTo(0.69).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.mDCV.WhitePointX"), Is.EqualTo(0.3127).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.mDCV.MaximumLuminance"), Is.EqualTo(4000).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.mDCV.MinimumLuminance"), Is.EqualTo(0.0005).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cLLI.MaximumContentLightLevel"), Is.EqualTo(1000).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cLLI.MaximumFrameAverageLightLevel"), Is.EqualTo(250).Within(0.000001));
    }

    [Test]
    public void Parse_InvalidColorChunksAddDiagnosticsWithoutThrowing()
    {
        byte[] invalidIcc = IccPayload("Bad", 1, new byte[] { 1, 2 });
        byte[] png = CreatePng(
            new string[] { "IHDR", "PLTE", "gAMA", "cHRM", "sRGB", "iCCP", "cICP", "mDCV", "cLLI", "IEND" },
            new byte[][]
            {
                Ihdr(),
                new byte[] { 1, 2 },
                new byte[] { 0, 0, 0 },
                new byte[31],
                new byte[] { 4 },
                invalidIcc,
                new byte[] { 1, 1, 2, 3 },
                new byte[23],
                new byte[7],
                new byte[0]
            });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "png.plte.length.invalid");
        AssertDiagnostic(document, "png.gama.length.invalid");
        AssertDiagnostic(document, "png.chrm.length.invalid");
        AssertDiagnostic(document, "png.srgb.intent.invalid");
        AssertDiagnostic(document, "png.iccp.compression.invalid");
        AssertDiagnostic(document, "png.cicp.matrix.invalid");
        AssertDiagnostic(document, "png.cicp.range.invalid");
        AssertDiagnostic(document, "png.mdcv.length.invalid");
        AssertDiagnostic(document, "png.clli.length.invalid");
        Assert.That(document.Profiles, Is.Empty);
    }

    [Test]
    public void Parse_MdcvWithoutCicpAndConflictingProfilesAddDiagnostics()
    {
        byte[] iccp = IccPayload("Profile", 0, Compress(new byte[] { 1, 2, 3 }));
        byte[] png = CreatePng(
            new string[] { "IHDR", "sRGB", "iCCP", "mDCV", "IEND" },
            new byte[][] { Ihdr(), new byte[] { 0 }, iccp, MdcvPayload(), new byte[0] });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "png.color.profile.conflict");
        AssertDiagnostic(document, "png.mdcv.cicp.missing");
    }

    [Test]
    public void Parse_SupportsDraftHdrChunkNamesAndCicpColorPrecedence()
    {
        byte[] png = CreatePng(
            new string[] { "IHDR", "cICP", "sRGB", "mDCv", "cLLi", "IEND" },
            new byte[][]
            {
                Ihdr(),
                new byte[] { 12, 13, 0, 1 },
                new byte[] { 0 },
                MdcvPayload(),
                Unsigned32Values(new uint[] { 10000000, 2500000 }),
                new byte[0]
            });

        ImageMetadataDocument document = ParsePng(png, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(DoubleValue(document, "Png.mDCv.MaximumLuminance"), Is.EqualTo(4000).Within(0.000001));
        Assert.That(DoubleValue(document, "Png.cLLi.MaximumContentLightLevel"), Is.EqualTo(1000).Within(0.000001));
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code));
    }

    public static long IntegerValue(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.Integer));
        return tag.Value.IntegerValue;
    }

    public static ulong UnsignedValue(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedInteger));
        return tag.Value.UnsignedIntegerValue;
    }

    public static double DoubleValue(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.Double));
        return tag.Value.DoubleValue;
    }

    public static string TextValue(ImageMetadataDocument document, string key)
    {
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, key);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.String));
        return tag.Value.Text;
    }

    public static byte[] Ihdr()
    {
        return PngIhdrExtractorTests.CreateIhdr(32, 24, 8, 6, 0, 0, 0);
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

    public static byte[] IccPayload(string name, int compressionMethod, byte[] compressed)
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(Encoding.Latin1.GetBytes(name));
        bytes.Add(0);
        bytes.Add((byte)compressionMethod);
        bytes.AddRange(compressed);
        return bytes.ToArray();
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

    public static byte[] MdcvPayload()
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned16(bytes, 34000);
        AddUnsigned16(bytes, 16000);
        AddUnsigned16(bytes, 13250);
        AddUnsigned16(bytes, 34500);
        AddUnsigned16(bytes, 7500);
        AddUnsigned16(bytes, 3000);
        AddUnsigned16(bytes, 15635);
        AddUnsigned16(bytes, 16450);
        PngChunkTraverserTests.AddUnsigned32(bytes, 40000000);
        PngChunkTraverserTests.AddUnsigned32(bytes, 5);
        return bytes.ToArray();
    }

    public static void AddUnsigned16(List<byte> bytes, int value)
    {
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }

    public static byte[] Unsigned32(uint value)
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddUnsigned32(bytes, value);
        return bytes.ToArray();
    }

    public static byte[] Unsigned32Values(uint[] values)
    {
        List<byte> bytes = new List<byte>();
        foreach (uint value in values)
        {
            PngChunkTraverserTests.AddUnsigned32(bytes, value);
        }

        return bytes.ToArray();
    }
}
