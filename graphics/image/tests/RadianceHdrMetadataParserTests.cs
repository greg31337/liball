using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class RadianceHdrMetadataParserTests
{
    [Test]
    public void Parse_ExtractsHeaderVariablesResolutionFrameAndSummary()
    {
        string text = "#?RADIANCE\n# studio render\n!rpict scene.oct\nSOFTWARE=Radiance 6.0\nFORMAT=32-bit_rle_rgbe\nEXPOSURE=2\nEXPOSURE=0.5\nPIXASPECT=2\nPIXASPECT=0.5\nCOLORCORR=1 2 3\nCOLORCORR=0.5 0.5 0.5\nGAMMA=2.2\nVIEW=-vtv -vp 0 0 0\nVIEW=-vh 60\nPRIMARIES=0.640 0.330 0.290 0.600 0.150 0.060 0.333 0.333\nCUSTOM FIELD=kept\n\n-Y 240 +X 320\n";
        byte[] bytes = CreateFile(text, new byte[] { 2, 2, 1, 64 });
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.RadianceHdr));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Radiance.Signature").Value.Text, Is.EqualTo("#?RADIANCE"));
        Assert.That(Find(document, "Radiance.Format").Value.Text, Is.EqualTo("32-bit_rle_rgbe"));
        Assert.That(document.Directories.Single().Tags.Count(item => item.Key == "Radiance.Exposure"), Is.EqualTo(2));
        Assert.That(document.Directories.Single().Tags.Count(item => item.Key == "Radiance.PixelAspect"), Is.EqualTo(2));
        Assert.That(document.Directories.Single().Tags.Count(item => item.Key == "Radiance.ColorCorrection"), Is.EqualTo(2));
        Assert.That(Find(document, "Radiance.EffectiveExposure").Value.DoubleValue, Is.EqualTo(1).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectivePixelAspect").Value.DoubleValue, Is.EqualTo(1).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectiveColorCorrectionRed").Value.DoubleValue, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectiveColorCorrectionGreen").Value.DoubleValue, Is.EqualTo(1).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectiveColorCorrectionBlue").Value.DoubleValue, Is.EqualTo(1.5).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectiveGamma").Value.DoubleValue, Is.EqualTo(2.2).Within(0.00001));
        Assert.That(Find(document, "Radiance.EffectiveView").Value.Text, Is.EqualTo("-vtv -vp 0 0 0 -vh 60"));
        Assert.That(Find(document, "Radiance.Primaries.RedX").Value.DoubleValue, Is.EqualTo(0.640).Within(0.00001));
        Assert.That(Find(document, "Radiance.Primaries.WhiteY").Value.DoubleValue, Is.EqualTo(0.333).Within(0.00001));
        Assert.That(Find(document, "Radiance.Variable.CUSTOM_FIELD").Value.Text, Is.EqualTo("kept"));
        Assert.That(Find(document, "Radiance.Comments").Value.TextValues, Is.EqualTo(new[] { "studio render" }));
        Assert.That(Find(document, "Radiance.Commands").Value.TextValues, Is.EqualTo(new[] { "rpict scene.oct" }));
        Assert.That(Find(document, "Radiance.Resolution").Value.Text, Is.EqualTo("-Y 240 +X 320"));
        Assert.That(Find(document, "Radiance.Width").Value.IntegerValue, Is.EqualTo(320));
        Assert.That(Find(document, "Radiance.Height").Value.IntegerValue, Is.EqualTo(240));
        Assert.That(Find(document, "Radiance.FirstAxis").DisplayValue, Is.EqualTo("Y decreasing"));
        Assert.That(Find(document, "Radiance.SecondAxis").DisplayValue, Is.EqualTo("X increasing"));
        Assert.That(Find(document, "Radiance.DataOffset").Value.IntegerValue, Is.EqualTo(text.Length));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(320));
        Assert.That(document.Frames[0].Height, Is.EqualTo(240));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].HasAlpha, Is.False);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Summary.Width, Is.EqualTo(320));
        Assert.That(document.Summary.Height, Is.EqualTo(240));
        Assert.That(document.Summary.Software, Is.EqualTo("Radiance 6.0"));
        Assert.That(document.RawBlocks.Single().Offset, Is.EqualTo(0));
        Assert.That(document.RawBlocks.Single().Length, Is.EqualTo(text.Length));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ExtractsXyzeColorSpaceAndRgbeSignatureAlias()
    {
        string text = "#?RGBE\nFORMAT=32-bit_rle_xyze\n\n-Y 16 +X 32\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 2, 2, 0, 32 })));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Radiance.Signature").Value.Text, Is.EqualTo("#?RGBE"));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Xyz));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Rle));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Xyz));
    }

    [TestCase("-Y", "+X", ImageOrientation.Normal)]
    [TestCase("-Y", "-X", ImageOrientation.MirrorHorizontal)]
    [TestCase("+Y", "-X", ImageOrientation.Rotate180)]
    [TestCase("+Y", "+X", ImageOrientation.MirrorVertical)]
    [TestCase("+X", "+Y", ImageOrientation.Rotate90)]
    [TestCase("-X", "+Y", ImageOrientation.MirrorHorizontalRotate90)]
    [TestCase("-X", "-Y", ImageOrientation.Rotate270)]
    [TestCase("+X", "-Y", ImageOrientation.MirrorHorizontalRotate270)]
    public void Parse_MapsAllResolutionScanOrders(string first, string second, ImageOrientation expected)
    {
        string resolution = first + " 7 " + second + " 11";
        string text = "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n" + resolution + "\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 1, 2, 3, 128 })));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(expected));
        Assert.That(document.Frames[0].Width, Is.EqualTo(first[1] == 'X' ? 7 : 11));
        Assert.That(document.Frames[0].Height, Is.EqualTo(first[1] == 'Y' ? 7 : 11));
        Assert.That(Find(document, "Radiance.Orientation").DisplayValue, Is.EqualTo(expected.ToString()));
    }

    [Test]
    public void Parse_AcceptsCrLfHeaderAndProgramLines()
    {
        string text = "#?RADIANCE\r\nrpict -ab 2 scene.oct\r\nFORMAT=32-bit_rle_rgbe\r\n\r\n-Y 2 +X 3\r\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 1, 1, 1, 128 })));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Radiance.Commands").Value.TextValues, Is.EqualTo(new[] { "rpict -ab 2 scene.oct" }));
        Assert.That(Find(document, "Radiance.DataOffset").Value.IntegerValue, Is.EqualTo(Encoding.ASCII.GetByteCount(text)));
        Assert.That(document.Frames[0].Width, Is.EqualTo(3));
        Assert.That(document.Frames[0].Height, Is.EqualTo(2));
    }

    [Test]
    public void Parse_TruncatedSignatureReportsDiagnostic()
    {
        ImageMetadataDocument document = ParseDirect(Encoding.ASCII.GetBytes("#?RADIANCE"), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.signature.truncated"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_InvalidSignatureReportsDiagnostic()
    {
        byte[] bytes = CreateFile("#?RADIANXX\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 1\n", new byte[] { 1, 2, 3, 4 });

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.signature.invalid"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_MissingInformationTerminatorReportsDiagnostic()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\nEXPOSURE=2\n");

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.header.terminator.missing"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_MissingResolutionReportsDiagnostic()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n");

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.resolution.missing"), Is.True);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Frames, Is.Empty);
    }

    [TestCase("-Y 20 +Y 10", "radiance.resolution.invalid")]
    [TestCase("Y 20 +X 10", "radiance.resolution.invalid")]
    [TestCase("-Y zero +X 10", "radiance.resolution.size.invalid")]
    [TestCase("-Y 20 +X 0", "radiance.resolution.size.invalid")]
    public void Parse_InvalidResolutionDoesNotCreateFrame(string resolution, string code)
    {
        string text = "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n" + resolution + "\n";

        ImageMetadataDocument document = ParseDirect(CreateFile(text, new byte[] { 1 }), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, code), Is.True);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_MissingFormatPreservesResolutionFrame()
    {
        string text = "#?RADIANCE\nSOFTWARE=Legacy Tool\n\n-Y 4 +X 5\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 1, 2, 3, 4 })));

        Assert.That(HasDiagnostic(document, "radiance.format.missing"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
        Assert.That(document.Frames[0].Width, Is.EqualTo(5));
        Assert.That(document.Summary.Software, Is.EqualTo("Legacy Tool"));
    }

    [Test]
    public void Parse_UnsupportedAndDuplicateFormatsAreDiagnosed()
    {
        string text = "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\nFORMAT=ascii\n\n-Y 4 +X 5\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 1, 2, 3, 4 })));

        Assert.That(HasDiagnostic(document, "radiance.format.duplicate"), Is.True);
        Assert.That(HasDiagnostic(document, "radiance.format.unsupported"), Is.True);
        Assert.That(document.Directories.Single().Tags.Count(item => item.Key == "Radiance.Format"), Is.EqualTo(2));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
    }

    [Test]
    public void Parse_InvalidKnownVariablesArePreservedAndDiagnosed()
    {
        string text = "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\nEXPOSURE=none\nPIXASPECT=-1\nCOLORCORR=1 2\nGAMMA=zero\nPRIMARIES=1 2 3\n\n-Y 2 +X 3\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CreateFile(text, new byte[] { 1, 2, 3, 4 })));

        Assert.That(HasDiagnostic(document, "radiance.exposure.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "radiance.pixel-aspect.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "radiance.color-correction.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "radiance.gamma.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "radiance.primaries.invalid"), Is.True);
        Assert.That(Find(document, "Radiance.Exposure").Value.Text, Is.EqualTo("none"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Radiance.EffectiveExposure"), Is.Null);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_EmptyRasterReportsTruncationButKeepsFrame()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 2 +X 3\n");

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "radiance.raster.empty"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_HeaderHonorsSegmentAndTagLimits()
    {
        string text = "#?RADIANCE\nSOFTWARE=Tool\nEXPOSURE=2\nFORMAT=32-bit_rle_rgbe\n\n-Y 2 +X 3\n";
        byte[] bytes = CreateFile(text, new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions segmentLimited = new ImageMetadataReaderOptions();
        segmentLimited.MaxSegmentBytes = 15;

        ImageMetadataDocument segmentDocument = ParseDirect(bytes, segmentLimited);

        Assert.That(HasDiagnostic(segmentDocument, "radiance.header.limit"), Is.True);
        Assert.That(segmentDocument.Directories, Is.Empty);

        ImageMetadataReaderOptions tagLimited = new ImageMetadataReaderOptions();
        tagLimited.MaxTagCount = 2;
        ImageMetadataDocument tagDocument = ParseDirect(bytes, tagLimited);
        Assert.That(HasDiagnostic(tagDocument, "radiance.header.tag.limit"), Is.True);
        Assert.That(tagDocument.Directories, Is.Empty);
    }

    [Test]
    public void Parse_NonAsciiHeaderByteIsDiagnosed()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("#?RADIANCE\nSOFTWARE=X\nFORMAT=32-bit_rle_rgbe\n\n-Y 2 +X 3\n");
        bytes[22] = 255;

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.header.non-ascii"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_RawHeaderCaptureHonorsOptionsAndLimit()
    {
        string text = "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 2 +X 3\n";
        byte[] bytes = CreateFile(text, new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = ParseDirect(bytes, captured);

        Assert.That(capturedDocument.RawBlocks.Single().Data, Is.EqualTo(bytes.Take(text.Length).ToArray()));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 20;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(limitedDocument.RawBlocks.Single().Data, Is.Empty);
        Assert.That(HasDiagnostic(limitedDocument, "radiance.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(ParseDirect(bytes, disabled).RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_NullReaderReportsDiagnostic()
    {
        ImageMetadataDocument document = RadianceHdrMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.RadianceHdr), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "radiance.reader.null"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    public static byte[] CreateFile(string header, byte[] raster)
    {
        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        byte[] bytes = new byte[headerBytes.Length + raster.Length];
        for (int index = 0; index < headerBytes.Length; index++)
        {
            bytes[index] = headerBytes[index];
        }
        for (int index = 0; index < raster.Length; index++)
        {
            bytes[headerBytes.Length + index] = raster[index];
        }
        return bytes;
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return RadianceHdrMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.RadianceHdr), options);
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
