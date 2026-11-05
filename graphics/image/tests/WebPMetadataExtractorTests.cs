using System.Collections.Generic;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class WebPMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsSimpleVp8DimensionsAndFrameProperties()
    {
        byte[] bytes = CreateWebP(new string[] { "VP8 " }, new byte[][] { CreateVp8(640, 480) });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(640));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(480));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.WebP));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.YCbCr));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8.Width").Value.IntegerValue, Is.EqualTo(640));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8.Height").Value.IntegerValue, Is.EqualTo(480));
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
    }

    [Test]
    public void Parse_ExtractsSimpleVp8lDimensionsAlphaAndVersion()
    {
        byte[] bytes = CreateWebP(new string[] { "VP8L" }, new byte[][] { CreateVp8l(321, 123, true, 0) });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(321));
        Assert.That(document.Frames[0].Height, Is.EqualTo(123));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8L.Signature").Value.IntegerValue, Is.EqualTo(47));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8L.Version").Value.IntegerValue, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ExtractsVp8xFlagsCanvasAndTopLevelAlpha()
    {
        byte[] vp8x = CreateVp8x(16, 12, 16);
        byte[] alpha = new byte[] { 25, 1, 2, 3 };
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ALPH", "VP8 " },
            new byte[][] { vp8x, alpha, CreateVp8(16, 12) });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8X.HasAlpha").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8X.CanvasWidth").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.VP8X.CanvasHeight").Value.IntegerValue, Is.EqualTo(12));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ALPH.CompressionMethod").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ALPH.FilteringMethod").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ALPH.Preprocessing").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ALPH.DataByteLength").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
    }

    [Test]
    public void Parse_PreservesIccExifAndXmpProfiles()
    {
        byte[] icc = IccProfileParserTests.CreateProfile(false);
        byte[] tiff = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
        byte[] exif = Join(Encoding.ASCII.GetBytes("Exif\0\0"), tiff);
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x='adobe:ns:meta/'/>");
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ICCP", "VP8 ", "EXIF", "XMP " },
            new byte[][] { CreateVp8x(2, 2, 44), icc, CreateVp8(2, 2), exif, xmp });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(3));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.WebP.ICCP"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(icc));
        Assert.That(document.Profiles[1].Kind, Is.EqualTo(MetadataProfileKind.Exif));
        Assert.That(document.Profiles[1].Key, Is.EqualTo("Exif.WebP.EXIF"));
        Assert.That(document.Profiles[1].Data, Is.EqualTo(tiff));
        Assert.That(document.Profiles[2].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[2].Key, Is.EqualTo("Xmp.WebP.XMP"));
        Assert.That(document.Profiles[2].Data, Is.EqualTo(xmp));
        Assert.That(document.Profiles[2].Text, Is.EqualTo(Encoding.UTF8.GetString(xmp)));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.EXIF.HasIdentifier").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.EXIF.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
    }

    [Test]
    public void Parse_HonorsProfileOptionsAndLimits()
    {
        byte[] tiff = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
        byte[] xmp = Encoding.UTF8.GetBytes("<xmp/>");
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ICCP", "VP8 ", "EXIF", "XMP " },
            new byte[][] { CreateVp8x(1, 1, 44), new byte[] { 1, 2, 3 }, CreateVp8(1, 1), tiff, xmp });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseIcc = false;
        options.ParseXmp = false;
        options.MaxProfileBytes = 4;

        ImageMetadataDocument document = ParseWebP(bytes, options);

        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        AssertDiagnostic(document, "webp.exif.profile.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Exif));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(8));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Summary.HasColorProfile, Is.True);
    }

    [Test]
    public void Parse_ExtractsAnimationControlAndFrames()
    {
        byte[] animation = new byte[] { 10, 20, 30, 40, 3, 0 };
        byte[] firstFrame = CreateAnmf(2, 2, 4, 3, 125, 1, new string[] { "ALPH", "VP8 " }, new byte[][] { new byte[] { 0 }, CreateVp8(4, 3) });
        byte[] secondFrame = CreateAnmf(0, 0, 2, 2, 50, 2, new string[] { "VP8L" }, new byte[][] { CreateVp8l(2, 2, true, 0) });
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ANIM", "ANMF", "ANMF" },
            new byte[][] { CreateVp8x(10, 8, 18), animation, firstFrame, secondFrame });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ANIM.BackgroundColor").Value.UnsignedIntegerValue, Is.EqualTo(673059850));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "WebP.ANIM.LoopCount").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(document.Frames, Has.Count.EqualTo(2));
        Assert.That(document.Frames[0].Width, Is.EqualTo(4));
        Assert.That(document.Frames[0].Height, Is.EqualTo(3));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(10));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(8));
        Assert.That(document.Frames[0].CanvasX, Is.EqualTo(2));
        Assert.That(document.Frames[0].CanvasY, Is.EqualTo(2));
        Assert.That(document.Frames[0].DurationMilliseconds, Is.EqualTo(125));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.YCbCr));
        Assert.That(document.Frames[1].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(document.Frames[1].HasAlpha, Is.True);
        Assert.That(document.Frames[1].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].IsAnimated, Is.True);
        Assert.That(document.Frames[1].IsAnimated, Is.True);
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.X")[0].Value.IntegerValue, Is.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.Y")[0].Value.IntegerValue, Is.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.DisposeToBackground")[0].Value.BooleanValue, Is.True);
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.NoBlend")[1].Value.BooleanValue, Is.True);
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.SubChunkCount")[0].Value.IntegerValue, Is.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.BitstreamType")[0].Value.Text, Is.EqualTo("VP8 "));
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.BitstreamType")[1].Value.Text, Is.EqualTo("VP8L"));
        Assert.That(new ImageMetadataQuery().Find(document, "WebP.ANMF.BitstreamWidth")[0].Value.IntegerValue, Is.EqualTo(4));
    }

    [Test]
    public void Parse_ReportsMalformedHeadersFlagsAlphaAndFrameBounds()
    {
        byte[] invalidVp8x = CreateVp8x(2, 2, 195);
        invalidVp8x[1] = 1;
        byte[] invalidVp8l = CreateVp8l(1, 1, false, 1);
        byte[] invalidAlpha = new byte[] { 255 };
        byte[] invalidFrame = CreateAnmf(2, 2, 2, 2, 1, 252, new string[] { "VP8 " }, new byte[][] { CreateVp8(2, 2) });
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ALPH", "VP8L", "ANIM", "ANMF" },
            new byte[][] { invalidVp8x, invalidAlpha, invalidVp8l, new byte[6], invalidFrame });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "webp.vp8x.reserved.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.alph.reserved.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.alph.compression.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.alph.preprocessing.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.vp8l.version.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.anmf.reserved.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.anmf.bounds.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_ReportsTruncatedAndMissingAnimationFrameData()
    {
        byte[] missingBitstream = CreateAnmf(0, 0, 1, 1, 0, 0, new string[0], new byte[0][]);
        byte[] truncatedNested = new byte[21];
        AddUnsigned24(truncatedNested, 6, 0);
        AddUnsigned24(truncatedNested, 9, 0);
        byte[] type = Encoding.ASCII.GetBytes("VP8 ");
        int index = 0;
        while (index < type.Length)
        {
            truncatedNested[16 + index] = type[index];
            index = index + 1;
        }

        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ANIM", "ANMF", "ANMF" },
            new byte[][] { CreateVp8x(1, 1, 2), new byte[6], missingBitstream, truncatedNested });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "webp.anmf.bitstream.missing", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.anmf.chunk.header.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_ReportsVp8xFeatureMismatchAndFrameLimit()
    {
        byte[] firstFrame = CreateAnmf(0, 0, 1, 1, 1, 0, new string[] { "VP8 " }, new byte[][] { CreateVp8(1, 1) });
        byte[] secondFrame = CreateAnmf(0, 0, 1, 1, 1, 0, new string[] { "VP8 " }, new byte[][] { CreateVp8(1, 1) });
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ANIM", "ANMF", "ANMF", "XMP " },
            new byte[][] { CreateVp8x(1, 1, 2), new byte[6], firstFrame, secondFrame, Encoding.UTF8.GetBytes("<x/>") });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;

        ImageMetadataDocument document = ParseWebP(bytes, options);

        AssertDiagnostic(document, "webp.vp8x.feature.mismatch", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.frame.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_ReportsInvalidAndMismatchedNestedBitstreams()
    {
        byte[] invalid = CreateAnmf(0, 0, 1, 1, 1, 0, new string[] { "VP8 " }, new byte[][] { new byte[10] });
        byte[] mismatched = CreateAnmf(0, 0, 2, 2, 1, 0, new string[] { "VP8L" }, new byte[][] { CreateVp8l(3, 2, false, 0) });
        byte[] bytes = CreateWebP(
            new string[] { "VP8X", "ANIM", "ANMF", "ANMF" },
            new byte[][] { CreateVp8x(3, 2, 2), new byte[6], invalid, mismatched });

        ImageMetadataDocument document = ParseWebP(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "webp.anmf.bitstream.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "webp.anmf.bitstream.dimensions.mismatch", MetadataSeverity.RecoverableError);
    }

    public static byte[] CreateVp8(int width, int height)
    {
        return new byte[]
        {
            0, 0, 0, 157, 1, 42,
            (byte)(width & 255), (byte)((width >> 8) & 63),
            (byte)(height & 255), (byte)((height >> 8) & 63)
        };
    }

    public static byte[] CreateVp8l(int width, int height, bool alpha, int version)
    {
        int widthValue = width - 1;
        int heightValue = height - 1;
        return new byte[]
        {
            47,
            (byte)(widthValue & 255),
            (byte)(((widthValue >> 8) & 63) | ((heightValue & 3) << 6)),
            (byte)((heightValue >> 2) & 255),
            (byte)(((heightValue >> 10) & 15) | (alpha ? 16 : 0) | ((version & 7) << 5))
        };
    }

    public static byte[] CreateVp8x(int width, int height, int flags)
    {
        byte[] data = new byte[10];
        data[0] = (byte)flags;
        AddUnsigned24(data, 4, width - 1);
        AddUnsigned24(data, 7, height - 1);
        return data;
    }

    public static byte[] CreateAnmf(int x, int y, int width, int height, int duration, int flags, string[] types, byte[][] payloads)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned24(bytes, x / 2);
        AddUnsigned24(bytes, y / 2);
        AddUnsigned24(bytes, width - 1);
        AddUnsigned24(bytes, height - 1);
        AddUnsigned24(bytes, duration);
        bytes.Add((byte)flags);
        int index = 0;
        while (index < types.Length)
        {
            RiffChunkTraverserTests.AddAscii(bytes, types[index]);
            RiffChunkTraverserTests.AddUnsigned32(bytes, (uint)payloads[index].Length);
            RiffChunkTraverserTests.AddBytes(bytes, payloads[index]);
            if ((payloads[index].Length & 1) != 0)
            {
                bytes.Add(0);
            }

            index = index + 1;
        }

        return bytes.ToArray();
    }

    public static byte[] CreateWebP(string[] types, byte[][] payloads)
    {
        return RiffChunkTraverserTests.CreateWebP(types, payloads, new byte[types.Length]);
    }

    public static byte[] Join(byte[] first, byte[] second)
    {
        List<byte> bytes = new List<byte>();
        RiffChunkTraverserTests.AddBytes(bytes, first);
        RiffChunkTraverserTests.AddBytes(bytes, second);
        return bytes.ToArray();
    }

    public static void AddUnsigned24(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
        data[offset + 2] = (byte)((value >> 16) & 255);
    }

    public static void AddUnsigned24(List<byte> data, int value)
    {
        data.Add((byte)(value & 255));
        data.Add((byte)((value >> 8) & 255));
        data.Add((byte)((value >> 16) & 255));
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }

    public static ImageMetadataDocument ParseWebP(byte[] bytes, ImageMetadataReaderOptions options)
    {
        return RiffChunkTraverserTests.ParseRiff(bytes, options);
    }
}
