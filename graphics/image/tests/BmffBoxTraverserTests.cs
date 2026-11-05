using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmffBoxTraverserTests
{
    [Test]
    public void Parse_ReadsRegularLargeUuidAndZeroSizedBoxes()
    {
        byte[] userType = new byte[]
        {
            0, 17, 34, 51, 68, 85, 102, 119,
            136, 153, 170, 187, 204, 221, 238, 255
        };
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, CreateBox("ftyp", new byte[] { 97, 118, 105, 102, 0, 0, 0, 0 }));
        AddBytes(bytes, CreateLargeUuidBox(userType, new byte[] { 9, 8 }));
        AddUnsigned32(bytes, 0);
        AddAscii(bytes, "free");
        AddBytes(bytes, new byte[] { 7, 6, 5 });

        ParseResult result = ParseBmffWithBoxes(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(result.Document.Diagnostics, Is.Empty);
        Assert.That(result.Boxes, Has.Count.EqualTo(3));
        Assert.That(result.Boxes[0].Type, Is.EqualTo("ftyp"));
        Assert.That(result.Boxes[0].Size, Is.EqualTo(16));
        Assert.That(result.Boxes[0].HeaderSize, Is.EqualTo(8));
        Assert.That(result.Boxes[1].Type, Is.EqualTo("uuid"));
        Assert.That(result.Boxes[1].UsesLargeSize, Is.True);
        Assert.That(result.Boxes[1].Size, Is.EqualTo(34));
        Assert.That(result.Boxes[1].HeaderSize, Is.EqualTo(32));
        Assert.That(result.Boxes[1].DataOffset, Is.EqualTo(48));
        Assert.That(result.Boxes[1].DataSize, Is.EqualTo(2));
        Assert.That(result.Boxes[1].UserType, Is.EqualTo(userType));
        Assert.That(result.Boxes[2].ExtendsToEnd, Is.True);
        Assert.That(result.Boxes[2].Size, Is.EqualTo(11));
        Assert.That(result.Document.Directories, Has.Count.EqualTo(3));
        Assert.That(result.Document.Directories[1].Path, Is.EqualTo("BMFF/uuid/0"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Bmff.uuid.UserType").DisplayValue, Is.EqualTo("00112233445566778899aabbccddeeff"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Bmff.free.ExtendsToEnd").Value.BooleanValue, Is.True);
        Assert.That(result.Stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_TraversesNestedContainersAndFullBoxPrefixes()
    {
        byte[] free = CreateBox("free", new byte[0]);
        byte[] trak = CreateBox("trak", free);
        byte[] moov = CreateBox("moov", trak);
        byte[] ispe = CreateBox("ispe", new byte[] { 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3 });
        byte[] ipco = CreateBox("ipco", ispe);
        byte[] iprp = CreateBox("iprp", ipco);
        byte[] meta = CreateBox("meta", Join(new byte[] { 0, 0, 0, 0 }, iprp));
        byte[] bytes = Join(moov, meta);

        ParseResult result = ParseBmffWithBoxes(bytes, new ImageMetadataReaderOptions());

        Assert.That(result.Document.Diagnostics, Is.Empty);
        Assert.That(result.Boxes, Has.Count.EqualTo(2));
        Assert.That(result.Boxes[0].Children[0].Type, Is.EqualTo("trak"));
        Assert.That(result.Boxes[0].Children[0].Children[0].Type, Is.EqualTo("free"));
        Assert.That(result.Boxes[1].Children[0].Type, Is.EqualTo("iprp"));
        Assert.That(result.Boxes[1].Children[0].Children[0].Type, Is.EqualTo("ipco"));
        Assert.That(result.Boxes[1].Children[0].Children[0].Children[0].Type, Is.EqualTo("ispe"));
        Assert.That(result.Document.Directories[0].Children[0].Children[0].Path, Is.EqualTo("BMFF/moov/0/trak/0/free/0"));
        Assert.That(result.Document.Directories[1].Children[0].Children[0].Children[0].Path, Is.EqualTo("BMFF/meta/0/iprp/0/ipco/0/ispe/0"));
        Assert.That(RequiredMetadataQuery.FindFirst(result.Document, "Bmff.ispe.Depth").Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Parse_TraversesVersionDependentItemInfoPrefix()
    {
        byte[] infe = CreateBox("infe", new byte[] { 0, 0, 0, 0 });
        byte[] iinf = CreateBox("iinf", Join(new byte[] { 0, 0, 0, 0, 0, 1 }, infe));

        ParseResult result = ParseBmffWithBoxes(iinf, new ImageMetadataReaderOptions());

        Assert.That(result.Document.Diagnostics, Is.Empty);
        Assert.That(result.Boxes[0].Children, Has.Count.EqualTo(1));
        Assert.That(result.Boxes[0].Children[0].Type, Is.EqualTo("infe"));
        Assert.That(result.Boxes[0].Children[0].Offset, Is.EqualTo(14));
    }

    [Test]
    public void Parse_ReportsMalformedHeadersAndDeclaredBounds()
    {
        ImageMetadataDocument smallSize = ParseBmff(CreateHeader(4, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(smallSize, "bmff.box.size.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument shortLargeSize = ParseBmff(CreateHeader(1, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(shortLargeSize, "bmff.box.largesize.truncated", MetadataSeverity.Truncated);

        byte[] shortUuid = Join(CreateHeader(24, "uuid"), new byte[8]);
        ImageMetadataDocument shortUserType = ParseBmff(shortUuid, new ImageMetadataReaderOptions());
        AssertDiagnostic(shortUserType, "bmff.box.usertype.truncated", MetadataSeverity.Truncated);

        ImageMetadataDocument undersizedUuid = ParseBmff(CreateHeader(8, "uuid"), new ImageMetadataReaderOptions());
        AssertDiagnostic(undersizedUuid, "bmff.box.size.invalid", MetadataSeverity.RecoverableError);

        ImageMetadataDocument declaredPastEnd = ParseBmff(CreateHeader(20, "free"), new ImageMetadataReaderOptions());
        AssertDiagnostic(declaredPastEnd, "bmff.box.truncated", MetadataSeverity.Truncated);
        Assert.That(declaredPastEnd.Directories, Has.Count.EqualTo(1));
        Assert.That(declaredPastEnd.Directories[0].Length, Is.EqualTo(20));
    }

    [Test]
    public void Parse_ReportsInvalidFourCcButPreservesTheBox()
    {
        byte[] bytes = new byte[] { 0, 0, 0, 8, 65, 1, 67, 68 };

        ImageMetadataDocument document = ParseBmff(bytes, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "bmff.box.type.invalid", MetadataSeverity.RecoverableError);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Tags[9].Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_EnforcesGlobalBoxCountAndNestingDepthLimits()
    {
        byte[] twoBoxes = Join(CreateBox("free", new byte[0]), CreateBox("skip", new byte[0]));
        ImageMetadataReaderOptions countOptions = new ImageMetadataReaderOptions();
        countOptions.MaxTagCount = 1;
        ImageMetadataDocument countLimited = ParseBmff(twoBoxes, countOptions);
        AssertDiagnostic(countLimited, "bmff.box.count.limit", MetadataSeverity.LimitExceeded);
        Assert.That(countLimited.Directories, Has.Count.EqualTo(1));

        byte[] nested = CreateBox("moov", CreateBox("trak", CreateBox("free", new byte[0])));
        ImageMetadataReaderOptions depthOptions = new ImageMetadataReaderOptions();
        depthOptions.MaxNestingDepth = 1;
        ImageMetadataDocument depthLimited = ParseBmff(nested, depthOptions);
        AssertDiagnostic(depthLimited, "bmff.box.depth.limit", MetadataSeverity.LimitExceeded);
        Assert.That(depthLimited.Directories[0].Children, Has.Count.EqualTo(1));
        Assert.That(depthLimited.Directories[0].Children[0].Children, Is.Empty);
    }

    [Test]
    public void Parse_SkipsOversizedContainerPayloadAndContinuesAtSibling()
    {
        byte[] bytes = Join(
            CreateBox("moov", CreateBox("free", new byte[0])),
            CreateBox("skip", new byte[0]));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 7;

        ImageMetadataDocument document = ParseBmff(bytes, options);

        AssertDiagnostic(document, "bmff.box.length.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Children, Is.Empty);
        Assert.That(document.Directories[1].Group, Is.EqualTo("skip"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.moov.DataLimited").Value.BooleanValue, Is.True);
    }

    [Test]
    public void ImageMetadataReader_DispatchesDetectedAvifToBoxTraverser()
    {
        byte[] ftyp = CreateBox("ftyp", new byte[] { 97, 118, 105, 102, 0, 0, 0, 0 });
        MemoryStream stream = new MemoryStream(ftyp);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Group, Is.EqualTo("ftyp"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    public static ImageMetadataDocument ParseBmff(byte[] bytes, ImageMetadataReaderOptions options)
    {
        return ParseBmffWithBoxes(bytes, options).Document;
    }

    internal static ParseResult ParseBmffWithBoxes(byte[] bytes, ImageMetadataReaderOptions options)
    {
        ParseResult result = new ParseResult();
        result.Stream = new MemoryStream(bytes);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = new BoundedStreamReader(result.Stream, result.Stream.Length, result.Stream.Length, diagnostics);
        result.Document = new ImageMetadataDocument();
        result.Document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Avif);
        result.Document.ByteLength = bytes.Length;
        result.Boxes = BmffBoxTraverser.ReadBoxes(result.Document, reader, options);
        BmffBoxTraverser.AddBoxDirectories(result.Document, result.Boxes, null, "BMFF");
        BmffBoxTraverser.Finish(result.Document, reader);
        return result;
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateLargeUuidBox(byte[] userType, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, 1);
        AddAscii(bytes, "uuid");
        AddUnsigned64(bytes, (ulong)(32 + payload.Length));
        AddBytes(bytes, userType);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] CreateHeader(uint size, string type)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, size);
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
        internal List<BmffBox> Boxes = new List<BmffBox>();
        internal MemoryStream Stream = new MemoryStream();
    }
}
