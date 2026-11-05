using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicReaderOptionsIntegrationTests
{
    [Test]
    public void FileSegmentAndTagLimits_ReturnPartialPublicResultsWithDiagnostics()
    {
        byte[] qoi = QoiMetadataParserTests.CreateQoi(9, 7, 4, 1, new byte[] { 255, 1, 2, 3, 4 });
        ImageMetadataReaderOptions fileOptions = new ImageMetadataReaderOptions();
        fileOptions.MaxFileBytes = 14;
        ImageMetadataDocument fileLimited = Read(qoi, fileOptions);

        string radianceText = "#?RADIANCE\nSOFTWARE=Tool\nEXPOSURE=2\nFORMAT=32-bit_rle_rgbe\n\n-Y 2 +X 3\n";
        byte[] radiance = RadianceHdrMetadataParserTests.CreateFile(radianceText, new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions segmentOptions = new ImageMetadataReaderOptions();
        segmentOptions.MaxSegmentBytes = 15;
        ImageMetadataDocument segmentLimited = Read(radiance, segmentOptions);
        ImageMetadataReaderOptions tagOptions = new ImageMetadataReaderOptions();
        tagOptions.MaxTagCount = 2;
        ImageMetadataDocument tagLimited = Read(radiance, tagOptions);

        Assert.That(fileLimited.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        Assert.That(fileLimited.Summary.Width, Is.EqualTo(9));
        Assert.That(fileLimited.Summary.Height, Is.EqualTo(7));
        AssertDiagnostic(fileLimited, "reader.file.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(fileLimited, "qoi.end-marker.truncated", MetadataSeverity.Truncated);
        Assert.That(segmentLimited.Format.Kind, Is.EqualTo(ImageFormatKind.RadianceHdr));
        Assert.That(segmentLimited.Directories, Is.Empty);
        AssertDiagnostic(segmentLimited, "radiance.header.limit", MetadataSeverity.LimitExceeded);
        Assert.That(tagLimited.Format.Kind, Is.EqualTo(ImageFormatKind.RadianceHdr));
        Assert.That(tagLimited.Directories, Is.Empty);
        AssertDiagnostic(tagLimited, "radiance.header.tag.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void RawBlockOptions_ControlListingPayloadCaptureAndSizeLimits()
    {
        byte[] qoi = QoiMetadataParserTests.CreateQoi(2, 2, 4, 0, new byte[] { 255, 1, 2, 3, 4 });
        ImageMetadataReaderOptions listed = new ImageMetadataReaderOptions();
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;
        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 10;
        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;

        ImageMetadataDocument listedDocument = Read(qoi, listed);
        ImageMetadataDocument capturedDocument = Read(qoi, captured);
        ImageMetadataDocument limitedDocument = Read(qoi, limited);
        ImageMetadataDocument disabledDocument = Read(qoi, disabled);

        Assert.That(listedDocument.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(listedDocument.RawBlocks, Has.All.Matches<MetadataRawBlock>(block => block.Data.Length == 0));
        Assert.That(capturedDocument.RawBlocks.Single(block => block.BlockType == "Header").Data, Is.EqualTo(qoi.Take(14).ToArray()));
        Assert.That(capturedDocument.RawBlocks.Single(block => block.BlockType == "EndMarker").Data.Length, Is.EqualTo(8));
        Assert.That(limitedDocument.RawBlocks.Single(block => block.BlockType == "Header").Data, Is.Empty);
        Assert.That(limitedDocument.RawBlocks.Single(block => block.BlockType == "EndMarker").Data.Length, Is.EqualTo(8));
        AssertDiagnostic(limitedDocument, "qoi.raw.block.limit", MetadataSeverity.LimitExceeded);
        Assert.That(disabledDocument.RawBlocks, Is.Empty);
    }

    [Test]
    public void XmlProfileAndDecompressionLimits_PreserveIdentityWhileBoundingPayloads()
    {
        byte[] xmp = Encoding.UTF8.GetBytes(XmpSidecarReaderTests.Packet("<xmp:CreatorTool>Tool</xmp:CreatorTool>"));
        ImageMetadataReaderOptions xmlOptions = new ImageMetadataReaderOptions();
        xmlOptions.MaxXmlBytes = 32;
        ImageMetadataDocument xmlLimited = Read(xmp, xmlOptions);

        byte[] profile = Encoding.ASCII.GetBytes("abcdefghijkl");
        byte[] payload = PngColorExtractorTests.IccPayload("Limited", 0, PngColorExtractorTests.Compress(profile));
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "iCCP", "IEND" },
            new byte[][] { PngMetadataExtractorTests.Ihdr(6), payload, new byte[0] });
        ImageMetadataReaderOptions profileOptions = new ImageMetadataReaderOptions();
        profileOptions.MaxProfileBytes = 5;
        profileOptions.MaxDecompressedBytes = 20;
        ImageMetadataReaderOptions decompressionOptions = new ImageMetadataReaderOptions();
        decompressionOptions.MaxProfileBytes = 20;
        decompressionOptions.MaxDecompressedBytes = 7;
        ImageMetadataDocument profileLimited = Read(png, profileOptions);
        ImageMetadataDocument decompressionLimited = Read(png, decompressionOptions);

        Assert.That(xmlLimited.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(xmlLimited.Profiles, Has.Count.EqualTo(1));
        Assert.That(xmlLimited.Profiles[0].Length, Is.EqualTo(xmp.Length));
        Assert.That(xmlLimited.Profiles[0].Data, Is.Empty);
        Assert.That(xmlLimited.Profiles[0].Text, Is.Empty);
        AssertDiagnostic(xmlLimited, "xmp.sidecar.limit", MetadataSeverity.LimitExceeded);
        Assert.That(profileLimited.Profiles[0].Data, Is.EqualTo(Encoding.ASCII.GetBytes("abcde")));
        Assert.That(decompressionLimited.Profiles[0].Data, Is.EqualTo(Encoding.ASCII.GetBytes("abcdefg")));
        AssertDiagnostic(profileLimited, "zlib.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(decompressionLimited, "zlib.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void IfdDepthAndTagLimits_AreAppliedThroughThePublicReader()
    {
        byte[] linkedIfds = TiffIfdTraverserTests.CreateTwoEmptyClassicIfds();
        ImageMetadataReaderOptions ifdOptions = new ImageMetadataReaderOptions();
        ifdOptions.MaxIfdCount = 1;
        ImageMetadataReaderOptions depthOptions = new ImageMetadataReaderOptions();
        depthOptions.MaxNestingDepth = 1;
        byte[] taggedTiff = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            2, 0,
            0, 1, 3, 0, 1, 0, 0, 0, 20, 0, 0, 0,
            1, 1, 3, 0, 1, 0, 0, 0, 10, 0, 0, 0,
            0, 0, 0, 0
        };
        ImageMetadataReaderOptions tagOptions = new ImageMetadataReaderOptions();
        tagOptions.MaxTagCount = 1;

        ImageMetadataDocument ifdLimited = Read(linkedIfds, ifdOptions);
        ImageMetadataDocument depthLimited = Read(linkedIfds, depthOptions);
        ImageMetadataDocument tagLimited = Read(taggedTiff, tagOptions);

        Assert.That(ifdLimited.Directories, Has.Count.EqualTo(2));
        AssertDiagnostic(ifdLimited, "tiff.ifd.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(depthLimited.Directories, Has.Count.EqualTo(2));
        AssertDiagnostic(depthLimited, "tiff.ifd.depth.limit", MetadataSeverity.LimitExceeded);
        Assert.That(tagLimited.Directories, Has.Count.EqualTo(1));
        AssertDiagnostic(tagLimited, "tiff.ifd.tag.count.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void FrameLimit_PreservesContainerMetadataAndBoundsFrames()
    {
        byte[] first = IcoMetadataParserTests.CreateDib(16, 16, 32);
        byte[] second = IcoMetadataParserTests.CreateDib(32, 32, 32);
        byte[] ico = IcoMetadataParserTests.CreateContainer(1, new byte[][] { first, second }, new int[] { 16, 32 }, new int[] { 16, 32 }, new int[] { 32, 32 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;

        ImageMetadataDocument document = Read(ico, options);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Ico));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(16));
        Assert.That(document.Directories, Is.Not.Empty);
        AssertDiagnostic(document, "ico.frame.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void OptionalMetadataFlags_IndependentlyControlXmpIccAndMakerNotes()
    {
        byte[] jpeg = EmbeddedMetadataIntegrationTests.CreateCompositeJpeg();
        ImageMetadataReaderOptions enabled = new ImageMetadataReaderOptions();
        enabled.DecodeMakerNotes = true;
        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.ParseXmp = false;
        disabled.ParseIcc = false;
        disabled.PreserveMakerNotes = false;
        disabled.DecodeMakerNotes = true;

        ImageMetadataDocument enabledDocument = Read(jpeg, enabled);
        ImageMetadataDocument disabledDocument = Read(jpeg, disabled);
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.FindFirst(enabledDocument, "Xmp.xmp.Rating"), Is.Not.Null);
        Assert.That(query.FindFirst(enabledDocument, "Icc.Header.Signature"), Is.Not.Null);
        Assert.That(query.FindFirst(enabledDocument, "MakerNote.MakerNote0.Vendor"), Is.Not.Null);
        Assert.That(query.FindFirst(enabledDocument, "MakerNote.MakerNote0.FirmwareVersion"), Is.Not.Null);
        Assert.That(query.FindFirst(disabledDocument, "Xmp.xmp.Rating"), Is.Null);
        Assert.That(query.FindFirst(disabledDocument, "Icc.Header.Signature"), Is.Null);
        Assert.That(query.FindFirst(disabledDocument, "MakerNote.MakerNote0.Vendor"), Is.Null);
        Assert.That(query.FindFirst(disabledDocument, "Exif.ExifIFD.MakerNote"), Is.Not.Null);
    }

    [Test]
    public void DiagnosticVerbosity_QuietSuppressesInformationalAndUnsupportedDiagnosticsOnly()
    {
        byte[] unknown = new byte[] { 1, 2, 3, 4 };
        ImageMetadataReaderOptions quiet = new ImageMetadataReaderOptions();
        quiet.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Quiet;
        ImageMetadataReaderOptions normal = new ImageMetadataReaderOptions();
        normal.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Normal;
        ImageMetadataReaderOptions verbose = new ImageMetadataReaderOptions();
        verbose.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Verbose;

        ImageMetadataDocument quietUnknown = Read(unknown, quiet);
        ImageMetadataDocument normalUnknown = Read(unknown, normal);
        ImageMetadataDocument verboseUnknown = Read(unknown, verbose);
        ImageMetadataDocument quietMalformed = Read(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, quiet);

        Assert.That(quietUnknown.Diagnostics, Is.Empty);
        Assert.That(quietUnknown.Format.Diagnostics, Is.Empty);
        AssertDiagnostic(normalUnknown, "format.unknown", MetadataSeverity.Unsupported);
        AssertDiagnostic(verboseUnknown, "format.unknown", MetadataSeverity.Unsupported);
        AssertDiagnostic(quietMalformed, "png.chunk.header.truncated", MetadataSeverity.Truncated);
        Assert.That(quietMalformed.IsCorrupt, Is.True);
        Assert.That(quietMalformed.IsIncomplete, Is.True);
    }

    [Test]
    public void NegativeFileLimit_IsDiagnosedAndProducesAStableEmptyPartialResult()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFileBytes = -1;
        NonSeekableReadStream stream = new NonSeekableReadStream(QoiMetadataParserTests.CreateQoi(2, 2, 3, 0, new byte[] { 254, 1, 2, 3 }));

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(stream, options);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.ByteLength, Is.Zero);
        AssertDiagnostic(document, "reader.file.limit.negative", MetadataSeverity.RecoverableError);
        Assert.That(stream.PositionValue, Is.EqualTo(1));
    }

    public static ImageMetadataDocument Read(byte[] bytes, ImageMetadataReaderOptions options)
    {
        return new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity), code);
    }
}
