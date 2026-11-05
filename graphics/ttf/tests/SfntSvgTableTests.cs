using System.IO.Compression;
using System.Text;
using System.Xml;
using ttf;

namespace ttf.Tests;

internal class SfntSvgTableTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";

    [Test]
    public void ParsesSortedGlyphRangesAndSharedPlainXmlDocuments()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"><g id=\"glyph1\"/></svg>");
        SfntSvgTable? table = Parse(BuildSharedDocumentTable(xml), 8);
        SfntSvgDocument first = table!.GetDocument(0);
        SfntSvgDocument second = table.GetDocument(1);
        byte[] copiedXml = first.CopyXmlBytes();
        copiedXml[0] = 0;
        XmlDocument document = first.LoadXml();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetDocumentCount(), Is.EqualTo(2));
            Assert.That(first.GetStartGlyphId(), Is.EqualTo(1));
            Assert.That(first.GetEndGlyphId(), Is.EqualTo(2));
            Assert.That(first.CoversGlyph(2), Is.True);
            Assert.That(first.IsGzipCompressed(), Is.False);
            Assert.That(first.GetEncodedLength(), Is.EqualTo(xml.Length));
            Assert.That(first.GetXmlLength(), Is.EqualTo(xml.Length));
            Assert.That(first.CopyEncodedBytes(), Is.EqualTo(xml));
            Assert.That(first.CopyXmlBytes(), Is.EqualTo(xml));
            Assert.That(second.CopyXmlBytes(), Is.EqualTo(xml));
            Assert.That(table.FindDocument(1), Is.SameAs(first));
            Assert.That(table.FindDocument(3), Is.Null);
            Assert.That(table.FindDocument(4), Is.SameAs(second));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.FindDocument(8));
            Assert.That(document.DocumentElement!.LocalName, Is.EqualTo("svg"));
            Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo(SvgNamespace));
        });
    }

    [Test]
    public void InflatesGzipDocumentsAndExposesEncodedAndXmlBytes()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\"?><svg xmlns=\"" + SvgNamespace +
            "\"><path id=\"glyph2\" d=\"M0 0L1 1\"/></svg>");
        byte[] gzip = Compress(xml);
        SfntSvgDocument document = Parse(BuildSingleDocumentTable(gzip), 5)!
            .GetDocument(0);

        Assert.Multiple(() =>
        {
            Assert.That(document.IsGzipCompressed(), Is.True);
            Assert.That(document.GetEncodedLength(), Is.EqualTo(gzip.Length));
            Assert.That(document.GetXmlLength(), Is.EqualTo(xml.Length));
            Assert.That(document.CopyEncodedBytes(), Is.EqualTo(gzip));
            Assert.That(document.CopyXmlBytes(), Is.EqualTo(xml));
            Assert.That(document.LoadXml().GetElementsByTagName("path").Count,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ExposesPublicCompressionLengthsAndSecureXmlLoading()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"><g id=\"glyph2\"/></svg>");
        byte[] gzip = Compress(xml);
        FontFace face = CreateFace(CreateCache(
            BuildSingleDocumentTable(gzip),
            5,
            FontLimits.CreateCompatible()));
        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyph(
            face.CreateDefaultInstance(),
            2);
        ColorSvgDocument document = glyph.GetSvgDocument()!;

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Svg));
            Assert.That(document.GetCompression(),
                Is.EqualTo(ColorSvgCompression.Gzip));
            Assert.That(document.IsGzipCompressed(), Is.True);
            Assert.That(document.GetEncodedLength(), Is.EqualTo(gzip.Length));
            Assert.That(document.GetXmlLength(), Is.EqualTo(xml.Length));
            Assert.That(document.CopyEncodedBytes(), Is.EqualTo(gzip));
            Assert.That(document.CopyXmlBytes(), Is.EqualTo(xml));
            Assert.That(document.LoadXml().DocumentElement!.LocalName,
                Is.EqualTo("svg"));
        });
    }

    [Test]
    public void ProhibitsDtdsAndExternalEntityResolution()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<!DOCTYPE svg [<!ENTITY external SYSTEM \"file:///etc/passwd\">]>" +
            "<svg xmlns=\"" + SvgNamespace + "\">&external;</svg>");

        Assert.Throws<XmlException>(() =>
            Parse(BuildSingleDocumentTable(xml), 5));
    }

    [Test]
    public void RejectsExternalAndActiveSvgContentDuringParsing()
    {
        byte[] externalHref = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace +
            "\"><image href=\"https://example.test/image.png\"/></svg>");
        byte[] externalCss = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace +
            "\"><rect style=\"fill:url(https://example.test/paint)\"/></svg>");
        byte[] script = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"><script/></svg>");
        byte[] eventHandler = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\" onload=\"run()\"/>");
        byte[] stylesheet = Encoding.UTF8.GetBytes(
            "<?xml-stylesheet href=\"https://example.test/style.css\"?>" +
            "<svg xmlns=\"" + SvgNamespace + "\"/>");
        byte[] localReferences = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"><defs>" +
            "<linearGradient id=\"paint\"/></defs>" +
            "<rect fill=\"url('#paint')\"/><use href=\"#paint\"/></svg>");

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildSingleDocumentTable(externalHref), 5));
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildSingleDocumentTable(externalCss), 5));
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildSingleDocumentTable(script), 5));
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildSingleDocumentTable(eventHandler), 5));
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildSingleDocumentTable(stylesheet), 5));
            Assert.That(Parse(
                BuildSingleDocumentTable(localReferences),
                5)!.GetDocument(0).LoadXml(), Is.Not.Null);
        });
    }

    [Test]
    public void RequiresAnSvgRootInTheSvgNamespace()
    {
        byte[] wrongRoot = Encoding.UTF8.GetBytes("<html/>");
        byte[] missingNamespace = Encoding.UTF8.GetBytes("<svg/>");

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                BuildSingleDocumentTable(wrongRoot), 5)!.GetDocument(0).LoadXml());
            Assert.Throws<FontFormatException>(() => Parse(
                BuildSingleDocumentTable(missingNamespace), 5)!
                .GetDocument(0).LoadXml());
        });
    }

    [TestCaseSource(nameof(InvalidSvgTables))]
    public void RejectsMalformedHeadersRangesAndDocumentBounds(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, 8));
    }

    [Test]
    public void EnforcesDocumentCountStringRatioAndCumulativeLimits()
    {
        byte[] smallXml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"/>");
        byte[] largeXml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"><desc>" +
            new string('x', 1000) + "</desc></svg>");
        byte[] gzip = Compress(largeXml);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                BuildSharedDocumentTable(smallXml),
                8,
                CreateLimits(5000, 5000, 100, 1)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildSingleDocumentTable(largeXml),
                8,
                CreateLimits(100, 5000, 100, 10)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildSingleDocumentTable(gzip),
                8,
                CreateLimits(5000, 5000, 1, 10)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildDistinctDocumentTable(smallXml, smallXml),
                8,
                CreateLimits(5000, smallXml.Length * 2 - 1, 100, 10)));
        });
    }

    [Test]
    public void KeepsUnknownVersionsAsRawTableData()
    {
        byte[] bytes = BuildSingleDocumentTable(Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"/>"));
        bytes[1] = 1;
        SfntParsedTableView? view = SfntSvgTableParser.FindView(CreateCache(
            bytes,
            5,
            FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(view!.HasParsedData(), Is.False);
            Assert.That(view.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    static IEnumerable<TestCaseData> InvalidSvgTables()
    {
        byte[] xml = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"" + SvgNamespace + "\"/>");

        yield return InvalidCase(new byte[9], "TruncatedHeader");

        byte[] zeroListOffset = BuildSingleDocumentTable(xml);
        PatchUInt32(zeroListOffset, 2, 0);
        yield return InvalidCase(zeroListOffset, "ZeroListOffset");

        byte[] nonzeroReserved = BuildSingleDocumentTable(xml);
        PatchUInt32(nonzeroReserved, 6, 1);
        yield return InvalidCase(nonzeroReserved, "NonzeroReserved");

        byte[] emptyList = BuildSingleDocumentTable(xml);
        PatchUInt16(emptyList, 10, 0);
        yield return InvalidCase(emptyList, "EmptyList");

        byte[] invertedRange = BuildSingleDocumentTable(xml);
        PatchUInt16(invertedRange, 12, 3);
        PatchUInt16(invertedRange, 14, 2);
        yield return InvalidCase(invertedRange, "InvertedRange");

        byte[] overlappingRanges = BuildSharedDocumentTable(xml);
        PatchUInt16(overlappingRanges, 24, 2);
        yield return InvalidCase(overlappingRanges, "OverlappingRanges");

        byte[] missingGlyph = BuildSingleDocumentTable(xml);
        PatchUInt16(missingGlyph, 14, 8);
        yield return InvalidCase(missingGlyph, "MissingGlyph");

        byte[] zeroDocumentOffset = BuildSingleDocumentTable(xml);
        PatchUInt32(zeroDocumentOffset, 16, 0);
        yield return InvalidCase(zeroDocumentOffset, "ZeroDocumentOffset");

        byte[] zeroDocumentLength = BuildSingleDocumentTable(xml);
        PatchUInt32(zeroDocumentLength, 20, 0);
        yield return InvalidCase(zeroDocumentLength, "ZeroDocumentLength");

        byte[] outsideDocument = BuildSingleDocumentTable(xml);
        PatchUInt32(outsideDocument, 16, uint.MaxValue);
        yield return InvalidCase(outsideDocument, "OutsideDocument");

        byte[] invalidGzip = BuildSingleDocumentTable(new byte[18]);
        int documentOffset = 24;
        invalidGzip[documentOffset] = 0x1F;
        invalidGzip[documentOffset + 1] = 0x8B;
        yield return InvalidCase(invalidGzip, "InvalidGzipMethod");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntSvgTable? Parse(byte[] bytes, int glyphCount)
    {
        return Parse(bytes, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntSvgTable? Parse(
        byte[] bytes,
        int glyphCount,
        FontLimits limits)
    {
        return SfntSvgTableParser.FindTable(CreateCache(bytes, glyphCount, limits));
    }

    static SfntTableCache CreateCache(
        byte[] svg,
        int glyphCount,
        FontLimits limits)
    {
        SfntTableData svgTable = CreateTable(FontTags.Svg, svg);
        SfntTableData maxpTable = CreateTable(FontTags.Maxp, BuildMaxp(glyphCount));
        return SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { svgTable, maxpTable },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static FontFace CreateFace(SfntTableCache cache)
    {
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildSingleDocumentTable(byte[] document)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt32(10)
            .WriteUInt32(0)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt32(14)
            .WriteUInt32((uint)document.Length)
            .WriteBytes(document)
            .CopyBytes();
    }

    static byte[] BuildSharedDocumentTable(byte[] document)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt32(10)
            .WriteUInt32(0)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt32(26)
            .WriteUInt32((uint)document.Length)
            .WriteUInt16(4)
            .WriteUInt16(4)
            .WriteUInt32(26)
            .WriteUInt32((uint)document.Length)
            .WriteBytes(document)
            .CopyBytes();
    }

    static byte[] BuildDistinctDocumentTable(byte[] first, byte[] second)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt32(10)
            .WriteUInt32(0)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt32(26)
            .WriteUInt32((uint)first.Length)
            .WriteUInt16(3)
            .WriteUInt16(3)
            .WriteUInt32((uint)(26 + first.Length))
            .WriteUInt32((uint)second.Length)
            .WriteBytes(first)
            .WriteBytes(second)
            .CopyBytes();
    }

    static byte[] Compress(byte[] bytes)
    {
        using MemoryStream output = new MemoryStream();

        using (GZipStream gzip = new GZipStream(
            output,
            CompressionLevel.SmallestSize,
            true))
        {
            gzip.Write(bytes);
        }

        return output.ToArray();
    }

    static void PatchUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static FontLimits CreateLimits(
        int maximumStringBytes,
        int maximumDecompressedBytes,
        int maximumRatio,
        int maximumCoverageEntries)
    {
        return FontLimits.Create(
            10000,
            10000,
            10,
            100,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            1000,
            maximumCoverageEntries,
            maximumStringBytes,
            maximumDecompressedBytes,
            maximumRatio,
            100,
            20,
            100);
    }
}
