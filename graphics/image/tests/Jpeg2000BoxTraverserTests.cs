using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class Jpeg2000BoxTraverserTests
{
    [Test]
    public void Parse_ReadsRegularExtendedUuidAndZeroLengthBoxes()
    {
        byte[] identifier = new byte[]
        {
            0, 17, 34, 51, 68, 85, 102, 119,
            136, 153, 170, 187, 204, 221, 238, 255
        };
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, CreateSignatureBox());
        AddBytes(bytes, CreateExtendedUuidBox(identifier, new byte[] { 9, 8 }));
        AddUnsigned32(bytes, 0);
        AddAscii(bytes, "jp2c");
        AddBytes(bytes, new byte[] { 7, 6, 5 });

        ParseResult result = ParseJpeg2000WithBoxes(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(result.Document.Diagnostics, Is.Empty);
        Assert.That(result.Boxes, Has.Count.EqualTo(3));
        Assert.That(result.Boxes[0].Type, Is.EqualTo("jP  "));
        Assert.That(result.Boxes[0].Size, Is.EqualTo(12));
        Assert.That(result.Boxes[1].Type, Is.EqualTo("uuid"));
        Assert.That(result.Boxes[1].UsesExtendedLength, Is.True);
        Assert.That(result.Boxes[1].Size, Is.EqualTo(34));
        Assert.That(result.Boxes[1].HeaderSize, Is.EqualTo(16));
        Assert.That(result.Boxes[1].DataOffset, Is.EqualTo(28));
        Assert.That(result.Boxes[1].DataSize, Is.EqualTo(18));
        Assert.That(result.Boxes[1].Uuid, Is.EqualTo(identifier));
        Assert.That(result.Boxes[2].ExtendsToEnd, Is.True);
        Assert.That(result.Boxes[2].Size, Is.EqualTo(11));
        Assert.That(result.Document.Directories[1].Path, Is.EqualTo("JP2/uuid/0"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Jpeg2000.uuid.Identifier").DisplayValue, Is.EqualTo("00112233445566778899aabbccddeeff"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Jpeg2000.jp2c.ExtendsToEnd").Value.BooleanValue, Is.True);
        Assert.That(result.Stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_TraversesStandardSuperboxes()
    {
        byte[] resolution = CreateBox("res ", CreateBox("resc", new byte[0]));
        byte[] header = CreateBox("jp2h", Join(CreateBox("ihdr", new byte[0]), resolution));
        byte[] uuidInfo = CreateBox("uinf", Join(CreateBox("ulst", new byte[0]), CreateBox("url ", new byte[0])));
        byte[] association = CreateBox("asoc", Join(CreateBox("lbl ", new byte[0]), CreateBox("xml ", new byte[0])));
        byte[] composition = CreateBox("comp", CreateBox("copt", new byte[0]));
        byte[] desiredReproductions = CreateBox("drep", CreateBox("gtso", new byte[0]));
        byte[] crossReference = CreateBox("cref", Join(new byte[] { 106, 112, 50, 99 }, CreateBox("flst", new byte[0])));

        ParseResult result = ParseJpeg2000WithBoxes(Join(Join(Join(header, uuidInfo, association), composition, desiredReproductions), crossReference), new ImageMetadataReaderOptions());

        Assert.That(result.Document.Diagnostics, Is.Empty);
        Assert.That(result.Boxes, Has.Count.EqualTo(6));
        Assert.That(result.Boxes[0].Children[0].Type, Is.EqualTo("ihdr"));
        Assert.That(result.Boxes[0].Children[1].Children[0].Type, Is.EqualTo("resc"));
        Assert.That(result.Boxes[1].Children[1].Type, Is.EqualTo("url "));
        Assert.That(result.Boxes[2].Children[1].Type, Is.EqualTo("xml "));
        Assert.That(result.Boxes[3].Children[0].Type, Is.EqualTo("copt"));
        Assert.That(result.Boxes[4].Children[0].Type, Is.EqualTo("gtso"));
        Assert.That(result.Boxes[5].Children[0].Type, Is.EqualTo("flst"));
        Assert.That(result.Boxes[5].Children[0].Offset, Is.EqualTo(result.Boxes[5].DataOffset + 4));
        Assert.That(result.Document.Directories[0].Children[1].Children[0].Path, Is.EqualTo("JP2/jp2h/0/res /0/resc/0"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Jpeg2000.resc.Depth").Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ReportsMalformedLengthsAndTruncatedHeaders()
    {
        ImageMetadataDocument smallLength = ParseJpeg2000(CreateHeader(4, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(smallLength, "jp2.box.length.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument shortExtendedLength = ParseJpeg2000(CreateHeader(1, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(shortExtendedLength, "jp2.box.extended-length.truncated", MetadataSeverity.Truncated);

        ImageMetadataDocument declaredPastEnd = ParseJpeg2000(CreateHeader(20, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(declaredPastEnd, "jp2.box.truncated", MetadataSeverity.Truncated);
        Assert.That(declaredPastEnd.Directories, Has.Count.EqualTo(1));

        ImageMetadataDocument partialHeader = ParseJpeg2000(new byte[] { 0, 0, 0, 8 }, new ImageMetadataReaderOptions());
        AssertDiagnostic(partialHeader, "jp2.box.header.truncated", MetadataSeverity.Truncated);

        byte[] shortUuid = Join(CreateHeader(16, "uuid"), new byte[8]);
        ImageMetadataDocument truncatedIdentifier = ParseJpeg2000(shortUuid, new ImageMetadataReaderOptions());
        AssertDiagnostic(truncatedIdentifier, "jp2.uuid.identifier.truncated", MetadataSeverity.Truncated);
        Assert.That(truncatedIdentifier.Directories, Has.Count.EqualTo(1));

        ImageMetadataDocument shortCrossReference = ParseJpeg2000(CreateBox("cref", new byte[] { 106, 112 }), new ImageMetadataReaderOptions());
        AssertDiagnostic(shortCrossReference, "jp2.box.superbox-prefix.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_ReportsInvalidBoxTypeButPreservesTheBox()
    {
        byte[] bytes = new byte[] { 0, 0, 0, 8, 65, 1, 67, 68 };

        ImageMetadataDocument document = ParseJpeg2000(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "jp2.box.type.invalid", MetadataSeverity.RecoverableError);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Tags, Has.Some.Matches<MetadataTag>(tag => tag.Name == "Type Valid" && !tag.Value.BooleanValue));
    }

    [Test]
    public void Parse_EnforcesGlobalBoxCountAndNestingDepthLimits()
    {
        byte[] twoBoxes = Join(CreateBox("free", new byte[0]), CreateBox("skip", new byte[0]));
        ImageMetadataReaderOptions countOptions = new ImageMetadataReaderOptions();
        countOptions.MaxTagCount = 1;
        ImageMetadataDocument countLimited = ParseJpeg2000(twoBoxes, countOptions);
        AssertDiagnostic(countLimited, "jp2.box.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(countLimited.Directories, Has.Count.EqualTo(1));

        byte[] nested = CreateBox("jp2h", CreateBox("jp2h", CreateBox("free", new byte[0])));
        ImageMetadataReaderOptions depthOptions = new ImageMetadataReaderOptions();
        depthOptions.MaxNestingDepth = 1;
        ImageMetadataDocument depthLimited = ParseJpeg2000(nested, depthOptions);
        AssertDiagnostic(depthLimited, "jp2.box.depth.limit", MetadataSeverity.LimitExceeded);
        Assert.That(depthLimited.Directories[0].Children, Has.Count.EqualTo(1));
        Assert.That(depthLimited.Directories[0].Children[0].Children, Is.Empty);
    }

    [Test]
    public void Parse_SkipsOversizedSuperboxAndContinuesAtSibling()
    {
        byte[] bytes = Join(
            CreateBox("jp2h", CreateBox("free", new byte[0])),
            CreateBox("skip", new byte[0]));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 7;

        ImageMetadataDocument document = ParseJpeg2000(bytes, options);

        AssertDiagnostic(document, "jp2.box.length.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Children, Is.Empty);
        Assert.That(document.Directories[1].Group, Is.EqualTo("skip"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg2000.jp2h.DataLimited").Value.BooleanValue, Is.True);
    }

    [Test]
    public void ImageMetadataReader_DispatchesDetectedJp2ToBoxTraverser()
    {
        byte[] bytes = Join(CreateSignatureBox(), CreateBox("jp2h", new byte[0]));
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg2000));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[1].Group, Is.EqualTo("jp2h"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    public static ImageMetadataDocument ParseJpeg2000(byte[] bytes, ImageMetadataReaderOptions options)
    {
        return ParseJpeg2000WithBoxes(bytes, options).Document;
    }

    internal static ParseResult ParseJpeg2000WithBoxes(byte[] bytes, ImageMetadataReaderOptions options)
    {
        ParseResult result = new ParseResult();
        result.Stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(result.Stream, result.Stream.Length, result.Stream.Length, diagnostics);
        result.Document = new ImageMetadataDocument();
        result.Document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg2000);
        result.Document.ByteLength = bytes.Length;
        result.Boxes = Jpeg2000BoxTraverser.ReadBoxes(result.Document, reader, options);
        Jpeg2000BoxTraverser.AddBoxDirectories(result.Document, result.Boxes, null, "JP2");
        Jpeg2000BoxTraverser.Finish(result.Document, reader);
        return result;
    }

    public static byte[] CreateSignatureBox()
    {
        return new byte[] { 0, 0, 0, 12, 106, 80, 32, 32, 13, 10, 135, 10 };
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateExtendedUuidBox(byte[] identifier, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, 1);
        AddAscii(bytes, "uuid");
        AddUnsigned64(bytes, (ulong)(16 + identifier.Length + payload.Length));
        AddBytes(bytes, identifier);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateHeader(uint length, string type)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, length);
        AddAscii(bytes, type);
        return bytes.ToArray();
    }

    public static byte[] Join(byte[] first, byte[] second)
    {
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, first);
        AddBytes(bytes, second);
        return bytes.ToArray();
    }

    public static byte[] Join(byte[] first, byte[] second, byte[] third)
    {
        return Join(Join(first, second), third);
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
        int shift = 56;
        while (shift >= 0)
        {
            bytes.Add((byte)(value >> shift));
            shift = shift - 8;
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

    internal class ParseResult
    {
        internal ImageMetadataDocument Document = new ImageMetadataDocument();
        internal List<Jpeg2000Box> Boxes = new List<Jpeg2000Box>();
        internal MemoryStream Stream = new MemoryStream();
    }
}
