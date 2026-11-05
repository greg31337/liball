using ttf;

namespace ttf.Tests;

internal class FontSubsetAuxiliaryTablesBuilderTests
{
    [Test]
    public void RewritesCompactColorTablesAndDropsVariationData()
    {
        byte[] cpal = BuildCpal();
        FontFace face = CreateFace(new int[]
        {
            FontTags.Colr, FontTags.Cpal, FontTags.Svg, FontTags.Sbix,
            FontTags.Avar, FontTags.Base
        }, new byte[][]
        {
            BuildColr(), cpal, BuildSvg(), BuildSbix(),
            new byte[] { 0, 1 }, new byte[] { 1, 2, 3 }
        });
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetColorPolicy(FontSubsetColorPolicy.Retain);

        FontSubsetAuxiliaryTables result =
            FontSubsetAuxiliaryTablesBuilder.Create(
                face.CreateDefaultInstance(),
                options,
                new int[] { 0, 2, -1, 1 },
                new int[] { 0, 3, 1 },
                CreateColorClosure());
        SfntTableCache cache = CreateOutputCache(result, 3);
        SfntColrTable? colr = SfntColrTableParser.FindTable(cache);
        SfntSvgTable? svg = SfntSvgTableParser.FindTable(cache);
        SfntSbixTable? sbix = SfntSbixTableParser.FindTable(cache);
        SfntColrBaseGlyphRecord? baseGlyph =
            colr!.FindBaseGlyphRecord(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTableCount(), Is.EqualTo(4));
            Assert.That(result.CopyTableData(FontTags.Cpal),
                Is.EqualTo(cpal));
            Assert.That(baseGlyph, Is.Not.Null);
            Assert.That(baseGlyph!.GetLayer(0).GetGlyphId(), Is.EqualTo(1));
            Assert.That(svg!.FindDocument(1), Is.Not.Null);
            Assert.That(svg.FindDocument(2), Is.Not.Null);
            Assert.That(sbix!.GetStrike(0).GetGlyphData(1)
                .GetDuplicateGlyphId(), Is.EqualTo(2));
            Assert.That(sbix.GetStrike(0).GetGlyphData(2)
                .GetGraphicKind(), Is.EqualTo(SfntSbixGraphicKind.Png));
            Assert.That(result.CopyDroppedTags(), Does.Contain(FontTags.Avar));
            Assert.That(result.CopyDroppedTags(), Does.Contain(FontTags.Base));
        });
    }

    [Test]
    public void ColorDropPolicyReportsEveryPresentColorTable()
    {
        FontFace face = CreateFace(
            new int[] { FontTags.Colr, FontTags.Cpal },
            new byte[][] { BuildColr(), BuildCpal() });

        FontSubsetAuxiliaryTables result =
            FontSubsetAuxiliaryTablesBuilder.Create(
                face.CreateDefaultInstance(),
                FontSubsetOptions.Create(),
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                CreateColorClosure());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTableCount(), Is.Zero);
            Assert.That(result.CopyDroppedTags(),
                Is.EqualTo(new int[] { FontTags.Colr, FontTags.Cpal }));
        });
    }

    [Test]
    public void FullPreserveProfileCopiesGlyphBearingLayoutTables()
    {
        byte[] baseBytes = new byte[] { 1, 2, 3 };
        byte[] jstfBytes = new byte[] { 4, 5 };
        byte[] mathBytes = new byte[] { 6 };
        FontFace face = CreateFace(
            new int[] { FontTags.Base, FontTags.Jstf, FontTags.Math },
            new byte[][] { baseBytes, jstfBytes, mathBytes });
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        options.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Preserve);
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);

        FontSubsetAuxiliaryTables result =
            FontSubsetAuxiliaryTablesBuilder.Create(
                face.CreateDefaultInstance(),
                options,
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                EmptyColorClosure());

        Assert.Multiple(() =>
        {
            Assert.That(result.CopyTableData(FontTags.Base),
                Is.EqualTo(baseBytes));
            Assert.That(result.CopyTableData(FontTags.Jstf),
                Is.EqualTo(jstfBytes));
            Assert.That(result.CopyTableData(FontTags.Math),
                Is.EqualTo(mathBytes));
            Assert.That(result.CopyDroppedTags(), Is.Empty);
        });
    }

    static FontSubsetColorClosure CreateColorClosure()
    {
        return new FontSubsetColorClosure(
            true,
            new int[] { 1, 3 },
            new int[] { 1 },
            new int[] { 0 },
            Array.Empty<int>(),
            new int[] { 0 },
            new int[] { 1, 3 },
            Array.Empty<int>(),
            Array.Empty<int>(),
            new int[] { 0 },
            new int[] { 0 });
    }

    static FontSubsetColorClosure EmptyColorClosure()
    {
        return new FontSubsetColorClosure(
            false,
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>());
    }

    static FontFace CreateFace(int[] tags, byte[][] values)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U).WriteUInt16(4).CopyBytes()));
        int index = 0;
        while (index < tags.Length)
        {
            tables.Add(CreateTable(tags[index], values[index]));
            index += 1;
        }
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableCache CreateOutputCache(
        FontSubsetAuxiliaryTables result,
        int glyphCount)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;
        while (index < result.GetTableCount())
        {
            FontSubsetAuxiliaryTable table = result.GetTable(index);
            tables.Add(CreateTable(table.GetTag(), table.CopyData()));
            index += 1;
        }
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes()));
        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
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

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(1).WriteUInt32(14).WriteUInt16(0)
            .WriteUInt8(10).WriteUInt8(20).WriteUInt8(30).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildColr()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt32(14)
            .WriteUInt32(20).WriteUInt16(1)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(3).WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildSvg()
    {
        byte[] xml = System.Text.Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt32(10).WriteUInt32(0)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(3)
            .WriteUInt32(14).WriteUInt32((uint)xml.Length)
            .WriteBytes(xml).CopyBytes();
    }

    static byte[] BuildSbix()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(1).WriteUInt32(1).WriteUInt32(12)
            .WriteUInt16(16).WriteUInt16(72)
            .WriteUInt32(24).WriteUInt32(24).WriteUInt32(35)
            .WriteUInt32(35).WriteUInt32(45)
            .WriteInt16(1).WriteInt16(2).WriteTag("png ")
            .WriteUInt8(7).WriteUInt8(8).WriteUInt8(9)
            .WriteInt16(3).WriteInt16(4).WriteTag("dupe")
            .WriteUInt16(1)
            .CopyBytes();
    }
}
