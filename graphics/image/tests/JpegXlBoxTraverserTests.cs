using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegXlBoxTraverserTests
{
    [Test]
    public void ImageMetadataReader_TraversesContainerAndReadsCompleteCodestreamHeader()
    {
        byte[] bytes = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreateLargeBox("free", new byte[] { 1, 2 }),
            CreateBox("jxlc", CreateSmallCodestream(64, 32)));
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.JpegXl));
        Assert.That(document.Diagnostics, Is.Empty, DiagnosticCodes(document));
        Assert.That(document.Directories, Has.Count.EqualTo(5));
        Assert.That(document.Directories[2].Group, Is.EqualTo("free"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.free.UsesLargeSize").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.ftyp.MajorBrand").Value.Text, Is.EqualTo("jxl "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Width").Value.UnsignedIntegerValue, Is.EqualTo(64));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Height").Value.UnsignedIntegerValue, Is.EqualTo(32));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.ContainerKind").Value.Text, Is.EqualTo("Container"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.JpegXl));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Summary.Width, Is.EqualTo(64));
        Assert.That(document.Summary.Height, Is.EqualTo(32));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void ImageMetadataReader_ReadsNakedCodestreamRatioSizeHeader()
    {
        byte[] bytes = CreateRatioCodestream(90, 5);
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.JpegXl));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Width").Value.UnsignedIntegerValue, Is.EqualTo(160));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Height").Value.UnsignedIntegerValue, Is.EqualTo(90));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.AspectRatioCode").Value.IntegerValue, Is.EqualTo(5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.ContainerKind").Value.Text, Is.EqualTo("Naked"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReassemblesOrderedPartialCodestreamAcrossBoxBoundaries()
    {
        byte[] codestream = CreateSmallCodestream(128, 48);
        byte[] bytes = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreatePartialBox(0, false, new byte[] { codestream[0] }),
            CreatePartialBox(1, true, Slice(codestream, 1, codestream.Length - 1)));

        ImageMetadataDocument document = ParseJpegXl(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.jxlp.PartialIndex").Value.UnsignedIntegerValue, Is.EqualTo(0));
        Assert.That(new ImageMetadataQuery().Find(document, "JpegXl.jxlp.PartialIndex"), Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Fragmented").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Width").Value.UnsignedIntegerValue, Is.EqualTo(128));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Height").Value.UnsignedIntegerValue, Is.EqualTo(48));
    }

    [Test]
    public void Parse_AllowsVersionOneOutOfOrderPartsButReportsVersionZeroOrder()
    {
        byte[] codestream = CreateSmallCodestream(96, 40);
        byte[] second = Slice(codestream, 2, codestream.Length - 2);
        byte[] first = Slice(codestream, 0, 2);
        byte[] versionOne = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(1),
            CreatePartialBox(1, true, second),
            CreatePartialBox(0, false, first));
        byte[] versionZero = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreatePartialBox(1, true, second),
            CreatePartialBox(0, false, first));

        ImageMetadataDocument allowed = ParseJpegXl(versionOne, new ImageMetadataReaderOptions());
        ImageMetadataDocument rejectedOrder = ParseJpegXl(versionZero, new ImageMetadataReaderOptions());

        Assert.That(allowed.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(allowed, "JpegXl.Codestream.Width").Value.UnsignedIntegerValue, Is.EqualTo(96));
        AssertDiagnostic(rejectedOrder, "jxl.jxlp.order.invalid", MetadataSeverity.RecoverableError);
        Assert.That(RequiredMetadataQuery.FindFirst(rejectedOrder, "JpegXl.Codestream.Height").Value.UnsignedIntegerValue, Is.EqualTo(40));
    }

    [Test]
    public void Parse_TraversesNestedJumbBoxesAndHonorsDepthAndCountLimits()
    {
        byte[] nested = CreateBox("jumb", CreateBox("jumb", CreateBox("json", new byte[] { 1 })));
        byte[] bytes = Join(CreateSignatureBox(), CreateFileTypeBox(0), nested);
        ImageMetadataDocument document = ParseJpegXl(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories[2].Children[0].Children[0].Path, Is.EqualTo("JXL/jumb/0/jumb/0/json/0"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.json.Depth").Value.IntegerValue, Is.EqualTo(2));

        ImageMetadataReaderOptions depthOptions = new ImageMetadataReaderOptions();
        depthOptions.MaxNestingDepth = 1;
        ImageMetadataDocument depthLimited = ParseJpegXl(bytes, depthOptions);
        AssertDiagnostic(depthLimited, "jxl.box.depth.limit", MetadataSeverity.LimitExceeded);
        Assert.That(depthLimited.Directories[2].Children[0].Children, Is.Empty);

        ImageMetadataReaderOptions countOptions = new ImageMetadataReaderOptions();
        countOptions.MaxTagCount = 2;
        ImageMetadataDocument countLimited = ParseJpegXl(bytes, countOptions);
        AssertDiagnostic(countLimited, "jxl.box.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(countLimited.Directories, Has.Count.EqualTo(2));
    }

    [Test]
    public void ReadBoxes_HandlesZeroSizeAndSegmentLimitWithoutReadingPayload()
    {
        byte[] bytes = Join(CreateBox("jumb", CreateBox("free", new byte[0])), CreateToEndBox("skip", new byte[] { 1, 2, 3 }));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 7;
        ParseResult result = ReadJpegXlBoxes(bytes, options);

        AssertDiagnostic(result.Document, "jxl.box.length.limit", MetadataSeverity.LimitExceeded);
        Assert.That(result.Boxes, Has.Count.EqualTo(2));
        Assert.That(result.Boxes[0].DataLimited, Is.True);
        Assert.That(result.Boxes[0].Children, Is.Empty);
        Assert.That(result.Boxes[1].ExtendsToEnd, Is.True);
        Assert.That(result.Boxes[1].Size, Is.EqualTo(11));
        Assert.That(result.Stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReportsMalformedBoxAndCodestreamStructures()
    {
        ImageMetadataDocument smallLength = ParseJpegXl(CreateHeader(4, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(smallLength, "jxl.box.length.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument shortLargeSize = ParseJpegXl(CreateHeader(1, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(shortLargeSize, "jxl.box.large-size.truncated", MetadataSeverity.Truncated);

        ImageMetadataDocument declaredPastEnd = ParseJpegXl(CreateHeader(20, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(declaredPastEnd, "jxl.box.truncated", MetadataSeverity.Truncated);

        byte[] invalidType = new byte[] { 0, 0, 0, 8, 65, 1, 67, 68 };
        ImageMetadataDocument badType = ParseJpegXl(invalidType, new ImageMetadataReaderOptions());
        AssertDiagnostic(badType, "jxl.box.type.invalid", MetadataSeverity.RecoverableError);

        byte[] shortPartial = Join(CreateSignatureBox(), CreateFileTypeBox(0), CreateBox("jxlp", new byte[] { 0, 0 }));
        ImageMetadataDocument truncatedPartial = ParseJpegXl(shortPartial, new ImageMetadataReaderOptions());
        AssertDiagnostic(truncatedPartial, "jxl.jxlp.index.truncated", MetadataSeverity.Truncated);

        byte[] shortHeader = Join(CreateSignatureBox(), CreateFileTypeBox(0), CreateBox("jxlc", new byte[] { 255, 10 }));
        ImageMetadataDocument truncatedHeader = ParseJpegXl(shortHeader, new ImageMetadataReaderOptions());
        AssertDiagnostic(truncatedHeader, "jxl.codestream.size.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_ReportsContainerAndPartialSequenceConflicts()
    {
        byte[] codestream = CreateSmallCodestream(32, 24);
        byte[] conflict = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreateBox("jxlc", codestream),
            CreatePartialBox(0, true, codestream),
            CreateBox("jxlc", codestream));
        ImageMetadataDocument conflictDocument = ParseJpegXl(conflict, new ImageMetadataReaderOptions());

        AssertDiagnostic(conflictDocument, "jxl.codestream.box.conflict", MetadataSeverity.RecoverableError);
        AssertDiagnostic(conflictDocument, "jxl.jxlc.duplicate", MetadataSeverity.RecoverableError);

        byte[] missingPart = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(1),
            CreatePartialBox(1, false, codestream));
        ImageMetadataDocument missingDocument = ParseJpegXl(missingPart, new ImageMetadataReaderOptions());
        AssertDiagnostic(missingDocument, "jxl.jxlp.index.missing", MetadataSeverity.RecoverableError);
        AssertDiagnostic(missingDocument, "jxl.jxlp.last.missing", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_ExtractsOrientationColorAlphaPreviewAndAnimationBasics()
    {
        byte[] codestream = CreateDetailedCodestream();
        ImageMetadataDocument document = ParseJpegXl(codestream, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty, DiagnosticCodes(document));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        ImageMetadataFrame frame = document.Frames[0];
        Assert.That(frame.Width, Is.EqualTo(128));
        Assert.That(frame.Height, Is.EqualTo(64));
        Assert.That(frame.BitDepth, Is.EqualTo(10));
        Assert.That(frame.ChannelCount, Is.EqualTo(4));
        Assert.That(frame.HasAlpha, Is.True);
        Assert.That(frame.IsAnimated, Is.True);
        Assert.That(frame.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(frame.Orientation, Is.EqualTo(ImageOrientation.Rotate90));
        Assert.That(document.Summary.Orientation, Is.EqualTo(ImageOrientation.Rotate90));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.IntrinsicWidth").Value.UnsignedIntegerValue, Is.EqualTo(80));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.IntrinsicHeight").Value.UnsignedIntegerValue, Is.EqualTo(40));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.PreviewWidth").Value.UnsignedIntegerValue, Is.EqualTo(128));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.PreviewHeight").Value.UnsignedIntegerValue, Is.EqualTo(128));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.TicksPerSecondNumerator").Value.UnsignedIntegerValue, Is.EqualTo(1000));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.TicksPerSecondDenominator").Value.UnsignedIntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.LoopCount").Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.Primaries").DisplayValue, Is.EqualTo("P3"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Codestream.TransferFunction").DisplayValue, Is.EqualTo("sRGB"));
    }

    [Test]
    public void Parse_ExtractsExifXmpAndC2paJumbfMetadata()
    {
        byte[] tiff = CreateMinimalTiff();
        byte[] exif = Join(new byte[] { 0, 0, 0, 0 }, tiff);
        byte[] xmp = System.Text.Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x='adobe:ns:meta/'/>");
        byte[] jumb = CreateBox("jumd", System.Text.Encoding.ASCII.GetBytes("c2pa manifest"));
        byte[] bytes = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreateBox("jxlc", CreateSmallCodestream(32, 24)),
            CreateBox("Exif", exif),
            CreateBox("xml ", xmp),
            CreateBox("jumb", jumb));

        ImageMetadataDocument document = ParseJpegXl(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(3));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Exif && profile.Data.Length == tiff.Length));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp && profile.Text.Contains("xmpmeta")));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.C2pa));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Exif.TiffOffset").Value.UnsignedIntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Exif.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Xmp.Text").Value.Text, Does.Contain("xmpmeta"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Jumbf.IsC2pa").Value.BooleanValue, Is.True);
    }

    [Test]
    public void Parse_ExtractsBrotliCompressedXmpAndHonorsXmpOption()
    {
        byte[] xmp = System.Text.Encoding.UTF8.GetBytes("<rdf:RDF xmlns:rdf='urn:test'/>");
        byte[] brobPayload = Join(System.Text.Encoding.ASCII.GetBytes("xml "), CompressBrotli(xmp));
        byte[] bytes = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreateBox("jxlc", CreateSmallCodestream(40, 24)),
            CreateBox("brob", brobPayload));

        ImageMetadataDocument document = ParseJpegXl(bytes, new ImageMetadataReaderOptions());
        ImageMetadataReaderOptions disabledOptions = new ImageMetadataReaderOptions();
        disabledOptions.ParseXmp = false;
        ImageMetadataDocument disabled = ParseJpegXl(bytes, disabledOptions);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Text, Is.EqualTo("<rdf:RDF xmlns:rdf='urn:test'/>"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.brob.DecodedType").Value.Text, Is.EqualTo("xml "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "JpegXl.Xmp.Compressed").Value.BooleanValue, Is.True);
        Assert.That(disabled.Profiles, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(disabled, "JpegXl.brob.DecodedType"), Is.Not.Null);
    }

    [Test]
    public void Parse_ReportsMetadataOffsetsEncodingAndConfiguredLimits()
    {
        byte[] invalidExif = Join(new byte[] { 0, 0, 0, 32 }, CreateMinimalTiff());
        byte[] invalidXmp = new byte[] { 60, 120, 255, 62 };
        byte[] bytes = Join(
            CreateSignatureBox(),
            CreateFileTypeBox(0),
            CreateBox("jxlc", CreateSmallCodestream(32, 24)),
            CreateBox("Exif", invalidExif),
            CreateBox("xml ", invalidXmp));
        ImageMetadataDocument invalid = ParseJpegXl(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(invalid, "jxl.exif.offset.invalid", MetadataSeverity.Truncated);
        AssertDiagnostic(invalid, "jxl.xmp.utf8.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataReaderOptions limitedOptions = new ImageMetadataReaderOptions();
        limitedOptions.MaxXmlBytes = 3;
        ImageMetadataDocument limited = ParseJpegXl(bytes, limitedOptions);
        AssertDiagnostic(limited, "jxl.xmp.profile.limit", MetadataSeverity.LimitExceeded);
        Assert.That(limited.Profiles, Is.Empty);

        byte[] truncatedMetadata = Join(new byte[] { 255, 10 }, PackSizeBitsWithoutMetadata(32, 24));
        ImageMetadataDocument truncated = ParseJpegXl(truncatedMetadata, new ImageMetadataReaderOptions());
        AssertDiagnostic(truncated, "jxl.codestream.metadata.truncated", MetadataSeverity.Truncated);
        Assert.That(truncated.Frames, Is.Empty);
    }

    public static ImageMetadataDocument ParseJpegXl(byte[] bytes, ImageMetadataReaderOptions options)
    {
        MemoryStream stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        ImageFormatInfo format = ImageFormatMapping.CreateInfo(ImageFormatKind.JpegXl);
        return JpegXlBoxTraverser.Parse(reader, format, options);
    }

    internal static ParseResult ReadJpegXlBoxes(byte[] bytes, ImageMetadataReaderOptions options)
    {
        ParseResult result = new ParseResult();
        result.Stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(result.Stream, result.Stream.Length, result.Stream.Length, diagnostics);
        result.Document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.JpegXl);
        result.Document.ByteLength = bytes.Length;
        result.Boxes = JpegXlBoxTraverser.ReadBoxes(result.Document, reader, options);
        JpegXlBoxTraverser.AddBoxDirectories(result.Document, result.Boxes, null, "JXL");
        JpegXlBoxTraverser.Finish(result.Document, reader);
        return result;
    }

    public static byte[] CreateSignatureBox()
    {
        return new byte[] { 0, 0, 0, 12, 74, 88, 76, 32, 13, 10, 135, 10 };
    }

    public static byte[] CreateFileTypeBox(uint version)
    {
        List<byte> payload = new List<byte>();
        AddAscii(payload, "jxl ");
        AddUnsigned32(payload, version);
        AddAscii(payload, "jxl ");
        return CreateBox("ftyp", payload.ToArray());
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateLargeBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, 1);
        AddAscii(bytes, type);
        AddUnsigned64(bytes, (ulong)(16 + payload.Length));
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateToEndBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, 0);
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreatePartialBox(uint index, bool isLast, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        uint value = isLast ? index | 2147483648 : index;
        AddUnsigned32(bytes, value);
        AddBytes(bytes, payload);
        return CreateBox("jxlp", bytes.ToArray());
    }

    public static byte[] CreateSmallCodestream(int width, int height)
    {
        List<int> bits = new List<int>();
        AddBits(bits, 1, 1);
        AddBits(bits, (uint)(height / 8 - 1), 5);
        AddBits(bits, 0, 3);
        AddBits(bits, (uint)(width / 8 - 1), 5);
        AddBits(bits, 1, 1);
        return Join(new byte[] { 255, 10 }, PackBits(bits));
    }

    public static byte[] CreateRatioCodestream(int height, int ratioCode)
    {
        List<int> bits = new List<int>();
        AddBits(bits, 0, 1);
        AddBits(bits, 0, 2);
        AddBits(bits, (uint)(height - 1), 9);
        AddBits(bits, (uint)ratioCode, 3);
        AddBits(bits, 1, 1);
        return Join(new byte[] { 255, 10 }, PackBits(bits));
    }

    public static byte[] CreateDetailedCodestream()
    {
        List<int> bits = new List<int>();
        AddBits(bits, 1, 1);
        AddBits(bits, 7, 5);
        AddBits(bits, 0, 3);
        AddBits(bits, 15, 5);
        AddBits(bits, 0, 1);
        AddBits(bits, 1, 1);
        AddBits(bits, 5, 3);
        AddBits(bits, 1, 1);
        AddBits(bits, 1, 1);
        AddBits(bits, 4, 5);
        AddBits(bits, 0, 3);
        AddBits(bits, 9, 5);
        AddBits(bits, 1, 1);
        AddBits(bits, 1, 1);
        AddBits(bits, 0, 2);
        AddBits(bits, 1, 3);
        AddBits(bits, 1, 1);
        AddBits(bits, 1, 2);
        AddBits(bits, 0, 2);
        AddBits(bits, 1, 2);
        AddBits(bits, 2, 3);
        AddBits(bits, 1, 1);
        AddBits(bits, 0, 1);
        AddBits(bits, 1, 2);
        AddBits(bits, 1, 1);
        AddBits(bits, 1, 2);
        AddBits(bits, 1, 1);
        AddBits(bits, 0, 1);
        AddBits(bits, 0, 1);
        AddBits(bits, 0, 1);
        AddBits(bits, 0, 2);
        AddBits(bits, 1, 2);
        AddBits(bits, 2, 2);
        AddBits(bits, 9, 4);
        AddBits(bits, 0, 1);
        AddBits(bits, 2, 2);
        AddBits(bits, 11, 4);
        AddBits(bits, 1, 2);
        AddBits(bits, 1, 1);
        return Join(new byte[] { 255, 10 }, PackBits(bits));
    }

    public static byte[] PackSizeBitsWithoutMetadata(int width, int height)
    {
        List<int> bits = new List<int>();
        AddBits(bits, 1, 1);
        AddBits(bits, (uint)(height / 8 - 1), 5);
        AddBits(bits, 0, 3);
        AddBits(bits, (uint)(width / 8 - 1), 5);
        return PackBits(bits);
    }

    public static byte[] CreateMinimalTiff()
    {
        return new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0, 0, 0, 0, 0
        };
    }

    public static byte[] CompressBrotli(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (BrotliStream brotli = new BrotliStream(output, CompressionLevel.SmallestSize, true))
        {
            brotli.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public static byte[] PackBits(List<int> bits)
    {
        byte[] bytes = new byte[(bits.Count + 7) / 8];
        for (int index = 0; index < bits.Count; index++)
        {
            if (bits[index] != 0)
            {
                bytes[index / 8] = (byte)(bytes[index / 8] | (1 << (index % 8)));
            }
        }

        return bytes;
    }

    public static void AddBits(List<int> bits, uint value, int count)
    {
        for (int index = 0; index < count; index++)
        {
            bits.Add((int)((value >> index) & 1));
        }
    }

    public static byte[] Slice(byte[] source, int offset, int count)
    {
        byte[] result = new byte[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = source[offset + index];
        }

        return result;
    }

    public static byte[] CreateHeader(uint length, string type)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, length);
        AddAscii(bytes, type);
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

    public static string DiagnosticCodes(ImageMetadataDocument document)
    {
        List<string> codes = new List<string>();
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        return string.Join(", ", codes);
    }

    internal class ParseResult
    {
        internal ImageMetadataDocument Document = new ImageMetadataDocument();
        internal List<JpegXlBox> Boxes = new List<JpegXlBox>();
        internal MemoryStream Stream = new MemoryStream();
    }
}
