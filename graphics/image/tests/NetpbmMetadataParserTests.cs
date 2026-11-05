using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class NetpbmMetadataParserTests
{
    [TestCase("P1", "PBM", "Plain", 1, 1, 0)]
    [TestCase("P2", "PGM", "Plain", 1, 4, 0)]
    [TestCase("P3", "PPM", "Plain", 3, 8, 0)]
    [TestCase("P4", "PBM", "Raw", 1, 1, 2)]
    [TestCase("P5", "PGM", "Raw", 1, 4, 6)]
    [TestCase("P6", "PPM", "Raw", 3, 8, 18)]
    public void Parse_ReadsPnmVariants(string magic, string variant, string encoding, int channels, int bitDepth, int expectedRasterBytes)
    {
        byte[] bytes = CreatePnm(magic, 3, 2, magic == "P1" || magic == "P4" ? 1 : magic == "P2" || magic == "P5" ? 15 : 255);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Netpbm));
        Assert.That(Find(document, "Netpbm.Header.Magic").Value.Text, Is.EqualTo(magic));
        Assert.That(Find(document, "Netpbm.Header.Variant").Value.Text, Is.EqualTo(variant));
        Assert.That(Find(document, "Netpbm.Header.Encoding").Value.Text, Is.EqualTo(encoding));
        Assert.That(Find(document, "Netpbm.Header.Width").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(Find(document, "Netpbm.Header.Height").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(3));
        Assert.That(document.Frames[0].Height, Is.EqualTo(2));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(channels));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(bitDepth));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(channels == 3 ? ImageColorSpaceKind.Rgb : ImageColorSpaceKind.Gray));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        if (expectedRasterBytes > 0)
        {
            Assert.That(Find(document, "Netpbm.Header.ExpectedRasterBytes").Value.IntegerValue, Is.EqualTo(expectedRasterBytes));
        }
        else
        {
            Assert.That(new ImageMetadataQuery().FindFirst(document, "Netpbm.Header.ExpectedRasterBytes"), Is.Null);
        }
    }

    [Test]
    public void Parse_PpmSixteenBitCalculatesTwoByteSamples()
    {
        byte[] header = Encoding.ASCII.GetBytes("P6\n2 1\n65535\n");
        byte[] bytes = Join(header, new byte[12]);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(16));
        Assert.That(Find(document, "Netpbm.Header.ExpectedRasterBytes").Value.IntegerValue, Is.EqualTo(12));
        Assert.That(Find(document, "Netpbm.Header.RasterOffset").Value.IntegerValue, Is.EqualTo(header.Length));
    }

    [Test]
    public void Parse_CommentsAreCollectedWithoutChangingRasterOffset()
    {
        byte[] header = Encoding.ASCII.GetBytes("P5\n# camera scan\n3 # width done\n2\n255\n");
        byte[] bytes = Join(header, new byte[6]);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Netpbm.Header.Comments").Value.TextValues, Is.EqualTo(new[] { "camera scan", "width done" }));
        Assert.That(Find(document, "Netpbm.Header.RasterOffset").Value.IntegerValue, Is.EqualTo(header.Length));
    }

    [Test]
    public void Parse_PamReadsTupleDepthAndAlpha()
    {
        string text = "P7\n# generated\nWIDTH 2\nHEIGHT 1\nDEPTH 4\nMAXVAL 65535\nTUPLTYPE RGB_ALPHA\nENDHDR\n";
        byte[] header = Encoding.ASCII.GetBytes(text);
        byte[] bytes = Join(header, new byte[16]);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Netpbm.Header.Variant").Value.Text, Is.EqualTo("PAM"));
        Assert.That(Find(document, "Netpbm.Header.TupleType").Value.Text, Is.EqualTo("RGB_ALPHA"));
        Assert.That(Find(document, "Netpbm.Header.Comments").Value.TextValues, Is.EqualTo(new[] { "generated" }));
        Assert.That(Find(document, "Netpbm.Header.ExpectedRasterBytes").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(16));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
    }

    [TestCase("PF", "-1.5", 3, "LittleEndian", 24)]
    [TestCase("Pf", "2.0", 1, "BigEndian", 8)]
    public void Parse_PfmReadsScaleByteOrderAndRowOrder(string magic, string scale, int channels, string byteOrder, int expectedBytes)
    {
        byte[] header = Encoding.ASCII.GetBytes(magic + "\n2 1\n" + scale + "\n");
        byte[] bytes = Join(header, new byte[expectedBytes]);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Netpbm.Header.Variant").Value.Text, Is.EqualTo("PFM"));
        Assert.That(Find(document, "Netpbm.Header.ScaleFactor").Value.DoubleValue, Is.EqualTo(double.Parse(scale.TrimStart('-'))));
        Assert.That(Find(document, "Netpbm.Header.ByteOrder").Value.Text, Is.EqualTo(byteOrder));
        Assert.That(Find(document, "Netpbm.Header.RowOrder").Value.Text, Is.EqualTo("BottomToTop"));
        Assert.That(Find(document, "Netpbm.Header.ExpectedRasterBytes").Value.IntegerValue, Is.EqualTo(expectedBytes));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(channels));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(32));
    }

    [Test]
    public void Parse_InvalidMaximumValueStopsBeforeAddingFrame()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("P5\n2 2\n65536\n");

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "netpbm.max-value.invalid"), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_MagicWithoutWhitespaceIsRejected()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("P63 2\n255\n");

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "netpbm.magic.separator.invalid"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_PamMissingRequiredFieldIsDiagnosed()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("P7\nWIDTH 2\nHEIGHT 1\nDEPTH 3\nENDHDR\n");

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "netpbm.pam.field.missing"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_TruncatedBinaryRasterKeepsHeaderMetadata()
    {
        byte[] bytes = Join(Encoding.ASCII.GetBytes("P6\n10 10\n255\n"), new byte[10]);

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "netpbm.raster.truncated"), Is.True);
        Assert.That(Find(document, "Netpbm.Header.ExpectedRasterBytes").Value.IntegerValue, Is.EqualTo(300));
        Assert.That(document.Frames[0].Width, Is.EqualTo(10));
    }

    [Test]
    public void Parse_HeaderLimitStopsTokenScanning()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("P5\n# comment consumes limit\n2 2\n255\n1234");
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 8;

        ImageMetadataDocument document = Parse(bytes, options);

        Assert.That(HasDiagnostic(document, "netpbm.header.limit"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_RawHeaderCaptureHonorsOptionsAndLimit()
    {
        byte[] header = Encoding.ASCII.GetBytes("P5\n2 2\n255\n");
        byte[] bytes = Join(header, new byte[4]);
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = Parse(bytes, captured);

        Assert.That(capturedDocument.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(capturedDocument.RawBlocks[0].Data, Is.EqualTo(header));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 4;
        ImageMetadataDocument limitedDocument = Parse(bytes, limited);
        Assert.That(limitedDocument.RawBlocks[0].Data, Is.Empty);
        Assert.That(HasDiagnostic(limitedDocument, "netpbm.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(Parse(bytes, disabled).RawBlocks, Is.Empty);
    }

    public static byte[] CreatePnm(string magic, int width, int height, int maxValue)
    {
        bool bitmap = magic == "P1" || magic == "P4";
        bool plain = magic == "P1" || magic == "P2" || magic == "P3";
        int channels = magic == "P3" || magic == "P6" ? 3 : 1;
        string headerText = magic + "\n" + width + " " + height + "\n";
        if (!bitmap)
        {
            headerText = headerText + maxValue + "\n";
        }
        byte[] header = Encoding.ASCII.GetBytes(headerText);
        if (plain)
        {
            StringBuilder raster = new StringBuilder();
            int samples = width * height * channels;
            for (int index = 0; index < samples; index++)
            {
                raster.Append(index == 0 ? "0" : " 0");
            }
            raster.Append('\n');
            return Join(header, Encoding.ASCII.GetBytes(raster.ToString()));
        }
        int bytesPerSample = maxValue < 256 ? 1 : 2;
        int rasterLength = bitmap ? ((width + 7) / 8) * height : width * height * channels * bytesPerSample;
        return Join(header, new byte[rasterLength]);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return NetpbmMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Netpbm), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
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
