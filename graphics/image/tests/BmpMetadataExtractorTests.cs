using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmpMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsDimensionsBitDepthCompressionAndResolution()
    {
        byte[] bytes = BmpHeaderParserTests.CreateBmp(40);
        BmpHeaderParserTests.WriteSigned32(bytes, 18, 640);
        BmpHeaderParserTests.WriteSigned32(bytes, 22, -480);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 26, 1);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 24);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 30, BmpMetadataExtractor.BiRgb);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 34, 921600);
        BmpHeaderParserTests.WriteSigned32(bytes, 38, 3780);
        BmpHeaderParserTests.WriteSigned32(bytes, 42, 1890);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(640));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(480));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(24));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].SourceColorType, Is.EqualTo("Rgb"));
        Assert.That(document.Frames[0].HorizontalResolution, Is.EqualTo(3780));
        Assert.That(document.Frames[0].VerticalResolution, Is.EqualTo(1890));
        Assert.That(document.Frames[0].ResolutionUnit, Is.EqualTo("pixels/meter"));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Width").Value.IntegerValue, Is.EqualTo(640));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Height").Value.IntegerValue, Is.EqualTo(-480));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.TopDown").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Compression").DisplayValue, Is.EqualTo("RGB"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.ImageSize").Value.UnsignedIntegerValue, Is.EqualTo(921600));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.XPixelsPerMeter").Value.IntegerValue, Is.EqualTo(3780));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.YPixelsPerMeter").Value.IntegerValue, Is.EqualTo(1890));
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
        Assert.That(document.Summary.ResolutionText, Is.EqualTo("3780 x 1890 pixels/meter"));
        Assert.That(document.Summary.HorizontalResolution, Is.EqualTo(3780));
        Assert.That(document.Summary.VerticalResolution, Is.EqualTo(1890));
        Assert.That(document.Summary.ResolutionUnit, Is.EqualTo("pixels/meter"));
    }

    [Test]
    public void Parse_ExtractsEmbeddedV3MasksAndAlpha()
    {
        byte[] bytes = BmpHeaderParserTests.CreateBmp(56);
        BmpHeaderParserTests.WriteSigned32(bytes, 18, 32);
        BmpHeaderParserTests.WriteSigned32(bytes, 22, 16);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 32);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 30, BmpMetadataExtractor.BiBitfields);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 54, 0x00ff0000);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 58, 0x0000ff00);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 62, 0x000000ff);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 66, 0xff000000);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.RedMask"), Is.EqualTo(0x00ff0000));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.GreenMask"), Is.EqualTo(0x0000ff00));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.BlueMask"), Is.EqualTo(0x000000ff));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.AlphaMask"), Is.EqualTo(0xff000000));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.AlphaMask").DisplayValue, Is.EqualTo("0xFF000000"));
    }

    [Test]
    public void Parse_ReadsExternalBitfieldMasksAfterInfoHeader()
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(40), 12);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 16);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 30, BmpMetadataExtractor.BiBitfields);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 54, 0x00007c00);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 58, 0x000003e0);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 62, 0x0000001f);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.RedMask"), Is.EqualTo(0x00007c00));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.GreenMask"), Is.EqualTo(0x000003e0));
        Assert.That(UnsignedValue(document, "Bmp.DIBHeader.BlueMask"), Is.EqualTo(0x0000001f));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.RedMask").Offset, Is.EqualTo(54));
    }

    [Test]
    public void Parse_ExtractsInfoHeaderPaletteAndRawRange()
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(40), 8);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 8);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 46, 2);
        bytes[54] = 3;
        bytes[55] = 2;
        bytes[56] = 1;
        bytes[57] = 0;
        bytes[58] = 30;
        bytes[59] = 20;
        bytes[60] = 10;
        bytes[61] = 0;

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Indexed));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(1));
        Assert.That(document.Directories, Has.Count.EqualTo(3));
        Assert.That(document.Directories[2].Path, Is.EqualTo("BMP/Palette"));
        Assert.That(UnsignedValue(document, "Bmp.Palette.EntryCount"), Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.EntrySize").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.Entries").Value.Bytes, Is.EqualTo(new byte[] { 3, 2, 1, 0, 30, 20, 10, 0 }));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(3));
        Assert.That(document.RawBlocks[2].BlockType, Is.EqualTo("Palette"));
        Assert.That(document.RawBlocks[2].Offset, Is.EqualTo(54));
        Assert.That(document.RawBlocks[2].Length, Is.EqualTo(8));
    }

    [Test]
    public void Parse_CoreHeaderPaletteUsesRgbTripleEntries()
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(12), 6);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 24, 1);
        bytes[26] = 1;
        bytes[27] = 2;
        bytes[28] = 3;
        bytes[29] = 4;
        bytes[30] = 5;
        bytes[31] = 6;

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(UnsignedValue(document, "Bmp.Palette.EntryCount"), Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.EntrySize").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.Entries").Value.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Parse_ExtractsV4ColorSpaceEndpointsAndGamma()
    {
        byte[] bytes = BmpHeaderParserTests.CreateBmp(108);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 32);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 70, BmpMetadataExtractor.LcsSrgb);
        int index = 0;
        while (index < 36)
        {
            bytes[74 + index] = (byte)(index + 1);
            index = index + 1;
        }

        BmpHeaderParserTests.WriteUnsigned32(bytes, 110, 65536);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 114, 131072);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 118, 32768);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.ColorSpaceType").DisplayValue, Is.EqualTo("sRGB"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.ColorSpaceEndpoints").Value.Bytes, Has.Length.EqualTo(36));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.GammaRed").DisplayValue, Is.EqualTo("1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.GammaGreen").DisplayValue, Is.EqualTo("2"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.GammaBlue").DisplayValue, Is.EqualTo("0.5"));
    }

    [Test]
    public void Parse_ExtractsEmbeddedV5IccProfile()
    {
        byte[] profile = CreateIccProfile(128);
        byte[] bytes = CreateV5ProfileBmp(BmpMetadataExtractor.ProfileEmbedded, profile);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.BMP.V5"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/vnd.iccprofile"));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(138));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(128));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(profile));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("BMP V5 ICC Profile"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.ProfileDataOffset").Value.UnsignedIntegerValue, Is.EqualTo(124));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.RenderingIntent").DisplayValue, Is.EqualTo("Images"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.ColorProfile.Type").Value.Text, Is.EqualTo("Embedded profile"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.ColorProfile.ByteLength").Value.UnsignedIntegerValue, Is.EqualTo(128));
    }

    [Test]
    public void Parse_V5LinkedProfileExtractsNameWithoutCreatingIccPayload()
    {
        byte[] linked = System.Text.Encoding.ASCII.GetBytes("display.icc\0ignored");
        byte[] bytes = CreateV5ProfileBmp(BmpMetadataExtractor.ProfileLinked, linked);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.ColorProfile.Type").Value.Text, Is.EqualTo("Linked profile"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.ColorProfile.LinkedName").Value.Text, Is.EqualTo("display.icc"));
    }

    [Test]
    public void Parse_ParseIccFalseKeepsV5ProfileMetadataWithoutPayload()
    {
        byte[] bytes = CreateV5ProfileBmp(BmpMetadataExtractor.ProfileEmbedded, CreateIccProfile(128));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseIcc = false;

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.ColorProfile.ByteLength").Value.UnsignedIntegerValue, Is.EqualTo(128));
    }

    [Test]
    public void Parse_ProfileLimitStopsPayloadReadAndAddsDiagnostic()
    {
        byte[] bytes = CreateV5ProfileBmp(BmpMetadataExtractor.ProfileEmbedded, CreateIccProfile(128));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 64;

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(128));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.profile.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_TruncatedPaletteAddsDiagnosticWithoutAllocatingEntries()
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(40), 4);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 8);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 46, 2);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(new ImageMetadataQuery().FindFirst(document, "Bmp.Palette.Entries"), Is.Null);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.palette.truncated"));
    }

    [Test]
    public void Parse_TruncatedExternalMasksAddsDiagnostic()
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(40), 8);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 16);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 30, BmpMetadataExtractor.BiBitfields);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(new ImageMetadataQuery().FindFirst(document, "Bmp.DIBHeader.RedMask"), Is.Null);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.masks.truncated"));
    }

    [Test]
    public void Parse_ProfileOutsideStreamAddsTruncatedDiagnostic()
    {
        byte[] bytes = BmpHeaderParserTests.CreateBmp(124);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 32);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 70, BmpMetadataExtractor.ProfileEmbedded);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 126, 124);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 130, 128);

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("bmp.profile.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [TestCase(BmpMetadataExtractor.BiRle8, ImageCompressionKind.Rle, "RLE8")]
    [TestCase(BmpMetadataExtractor.BiRle4, ImageCompressionKind.Rle, "RLE4")]
    [TestCase(BmpMetadataExtractor.BiJpeg, ImageCompressionKind.Jpeg, "JPEG")]
    [TestCase(BmpMetadataExtractor.BiPng, ImageCompressionKind.Deflate, "PNG")]
    public void Parse_MapsCompressionKinds(uint compression, ImageCompressionKind expectedKind, string expectedName)
    {
        byte[] bytes = BmpHeaderParserTests.CreateBmp(40);
        ushort bitCount = 8;
        if (compression == BmpMetadataExtractor.BiRle4)
        {
            bitCount = 4;
        }
        else if (compression == BmpMetadataExtractor.BiJpeg || compression == BmpMetadataExtractor.BiPng)
        {
            bitCount = 0;
        }

        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, bitCount);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 30, compression);
        if (compression == BmpMetadataExtractor.BiRle8 || compression == BmpMetadataExtractor.BiRle4)
        {
            BmpHeaderParserTests.WriteUnsigned32(bytes, 46, 0);
            bytes = ExtendBmp(bytes, compression == BmpMetadataExtractor.BiRle8 ? 1024 : 64);
        }

        ImageMetadataDocument document = BmpHeaderParserTests.ParseBmp(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(expectedKind));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Compression").DisplayValue, Is.EqualTo(expectedName));
    }

    public static byte[] ExtendBmp(byte[] source, int extraLength)
    {
        byte[] bytes = new byte[source.Length + extraLength];
        System.Array.Copy(source, bytes, source.Length);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 2, (uint)bytes.Length);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 10, (uint)bytes.Length);
        return bytes;
    }

    public static byte[] CreateV5ProfileBmp(uint profileType, byte[] profile)
    {
        byte[] bytes = ExtendBmp(BmpHeaderParserTests.CreateBmp(124), profile.Length);
        BmpHeaderParserTests.WriteSigned32(bytes, 18, 10);
        BmpHeaderParserTests.WriteSigned32(bytes, 22, 10);
        BmpHeaderParserTests.WriteUnsigned16(bytes, 28, 32);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 70, profileType);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 122, 4);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 126, 124);
        BmpHeaderParserTests.WriteUnsigned32(bytes, 130, (uint)profile.Length);
        System.Array.Copy(profile, 0, bytes, 138, profile.Length);
        return bytes;
    }

    public static byte[] CreateIccProfile(int length)
    {
        byte[] profile = new byte[length];
        profile[0] = (byte)(length >> 24);
        profile[1] = (byte)(length >> 16);
        profile[2] = (byte)(length >> 8);
        profile[3] = (byte)length;
        profile[36] = (byte)'a';
        profile[37] = (byte)'c';
        profile[38] = (byte)'s';
        profile[39] = (byte)'p';
        int index = 40;
        while (index < profile.Length)
        {
            profile[index] = (byte)index;
            index = index + 1;
        }

        return profile;
    }

    public static ulong UnsignedValue(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.UnsignedIntegerValue;
    }
}
