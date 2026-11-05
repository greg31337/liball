using ttf;

namespace ttf.Tests;

internal class FontSubsetTrueTypeTableBuilderTests
{
    [Test]
    public void RebuildsCompactCoreTablesWithCountsBoundsAndMetrics()
    {
        FontSubset subset = BuildSubset(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetVerticalPolicy.Drop,
            2);
        SfntSubsetTrueTypeData data = subset.CreateTrueTypeTables();
        FontInstance output = CreateOutputFace(data).CreateDefaultInstance();
        byte[] head = data.CopyHead();
        byte[] maxp = data.CopyMaxp();
        byte[] hhea = data.CopyHhea();

        Assert.Multiple(() =>
        {
            Assert.That(output.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(output.GetGlyphOutline(2).IsComposite(), Is.True);
            Assert.That(output.GetGlyphOutline(2)
                .GetComponent(0).GetGlyphId(), Is.Zero);
            Assert.That(output.GetGlyphOutline(2)
                .GetComponent(1).GetGlyphId(), Is.EqualTo(1));
            Assert.That(output.GetGlyphMetrics(0).GetAdvanceWidth(),
                Is.EqualTo(600));
            Assert.That(output.GetGlyphMetrics(2).GetAdvanceWidth(),
                Is.EqualTo(800));
            Assert.That(ReadInt16(head, 36), Is.Zero);
            Assert.That(ReadInt16(head, 38), Is.Zero);
            Assert.That(ReadInt16(head, 40), Is.EqualTo(20));
            Assert.That(ReadInt16(head, 42), Is.EqualTo(20));
            Assert.That(ReadInt16(head, 50), Is.EqualTo(1));
            Assert.That(ReadUInt16(maxp, 4), Is.EqualTo(3));
            Assert.That(ReadUInt16(maxp, 6), Is.EqualTo(3));
            Assert.That(ReadUInt16(maxp, 8), Is.EqualTo(1));
            Assert.That(ReadUInt16(maxp, 10), Is.EqualTo(5));
            Assert.That(ReadUInt16(maxp, 12), Is.EqualTo(2));
            Assert.That(ReadUInt16(maxp, 26), Is.Zero);
            Assert.That(ReadUInt16(maxp, 28), Is.EqualTo(2));
            Assert.That(ReadUInt16(maxp, 30), Is.EqualTo(1));
            Assert.That(ReadUInt16(hhea, 10), Is.EqualTo(800));
            Assert.That(ReadInt16(hhea, 12), Is.EqualTo(10));
            Assert.That(ReadInt16(hhea, 14), Is.EqualTo(570));
            Assert.That(ReadInt16(hhea, 16), Is.EqualTo(30));
            Assert.That(ReadUInt16(hhea, 34), Is.EqualTo(2));
            Assert.That(data.CopyHmtx(), Has.Length.EqualTo(10));
            Assert.That(data.CopyLoca(), Has.Length.EqualTo(16));
            Assert.That(data.HasVerticalMetrics(), Is.False);
        });
    }

    [Test]
    public void PreserveModeWritesZeroMetricsForUnassignedGlyphSlots()
    {
        FontSubset subset = BuildSubset(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            FontSubsetGlyphRetentionMode.Preserve,
            FontSubsetVerticalPolicy.Drop,
            3);
        SfntSubsetTrueTypeData data = subset.CreateTrueTypeTables();
        FontInstance output = CreateOutputFace(data).CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.That(output.GetGlyphCount(), Is.EqualTo(4));
            Assert.That(output.GetGlyphOutline(1).IsEmpty(), Is.True);
            Assert.That(output.GetGlyphOutline(2).IsEmpty(), Is.True);
            Assert.That(output.GetGlyphMetrics(1).GetAdvanceWidth(), Is.Zero);
            Assert.That(output.GetGlyphMetrics(1).GetLeftSideBearing(),
                Is.Zero);
            Assert.That(output.GetGlyphMetrics(2).GetAdvanceWidth(), Is.Zero);
            Assert.That(output.GetGlyphMetrics(3).GetAdvanceWidth(),
                Is.EqualTo(1000));
            Assert.That(output.GetGlyphMetrics(3).GetLeftSideBearing(),
                Is.EqualTo(40));
            Assert.That(ReadUInt16(data.CopyMaxp(), 4), Is.EqualTo(4));
            Assert.That(ReadUInt16(data.CopyHhea(), 34), Is.EqualTo(4));
            Assert.That(data.CopyHmtx(), Has.Length.EqualTo(16));
        });
    }

    [Test]
    public void RebuildsVerticalMetricsOnlyWhenRetainedAndAvailable()
    {
        FontInstance source = CreateVerticalFace().CreateDefaultInstance();
        SfntSubsetTrueTypeData retained = BuildSubset(
            source,
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetVerticalPolicy.Retain,
            2).CreateTrueTypeTables();
        SfntSubsetTrueTypeData dropped = BuildSubset(
            source,
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetVerticalPolicy.Drop,
            2).CreateTrueTypeTables();
        FontInstance output = CreateOutputFace(retained)
            .CreateDefaultInstance();
        byte[]? vhea = retained.CopyVhea();

        Assert.Multiple(() =>
        {
            Assert.That(retained.HasVerticalMetrics(), Is.True);
            Assert.That(dropped.HasVerticalMetrics(), Is.False);
            Assert.That(dropped.CopyVhea(), Is.Null);
            Assert.That(dropped.CopyVmtx(), Is.Null);
            Assert.That(output.GetGlyphMetrics(0).GetAdvanceHeight(),
                Is.EqualTo(900));
            Assert.That(output.GetGlyphMetrics(0).GetTopSideBearing(),
                Is.EqualTo(100));
            Assert.That(output.GetGlyphMetrics(2).GetAdvanceHeight(),
                Is.EqualTo(1000));
            Assert.That(output.GetGlyphMetrics(2).GetTopSideBearing(),
                Is.EqualTo(200));
            Assert.That(ReadUInt16(vhea, 10), Is.EqualTo(1000));
            Assert.That(ReadInt16(vhea, 12), Is.EqualTo(100));
            Assert.That(ReadInt16(vhea, 14), Is.EqualTo(780));
            Assert.That(ReadInt16(vhea, 16), Is.EqualTo(210));
            Assert.That(ReadUInt16(vhea, 34), Is.EqualTo(2));
            Assert.That(retained.CopyVmtx(), Has.Length.EqualTo(10));
        });
    }

    [Test]
    public void ReturnedCoreTableBytesAreIndependentCopies()
    {
        FontSubset subset = BuildSubset(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetVerticalPolicy.Drop,
            2);
        SfntSubsetTrueTypeData data = subset.CreateTrueTypeTables();
        byte[] head = data.CopyHead();
        byte[] maxp = data.CopyMaxp();
        byte[] hhea = data.CopyHhea();
        byte[] hmtx = data.CopyHmtx();
        head[0] = 99;
        maxp[0] = 99;
        hhea[0] = 99;
        hmtx[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(data.CopyHead()[0], Is.EqualTo(0));
            Assert.That(data.CopyMaxp()[0], Is.EqualTo(0));
            Assert.That(data.CopyHhea()[0], Is.EqualTo(0));
            Assert.That(data.CopyHmtx()[0], Is.EqualTo(2));
        });
    }

    static FontSubset BuildSubset(
        FontInstance instance,
        FontSubsetGlyphRetentionMode retentionMode,
        FontSubsetVerticalPolicy verticalPolicy,
        int glyphId)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(retentionMode);
        options.SetVerticalPolicy(verticalPolicy);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);
        builder.AddGlyph(glyphId);
        return builder.Build();
    }

    static FontFace CreateOutputFace(SfntSubsetTrueTypeData data)
    {
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        AddTable(tables, FontTags.Head, data.CopyHead());
        AddTable(tables, FontTags.Maxp, data.CopyMaxp());
        AddTable(tables, FontTags.Hhea, data.CopyHhea());
        AddTable(tables, FontTags.Hmtx, data.CopyHmtx());
        AddTable(tables, FontTags.Loca, data.CopyLoca());
        AddTable(tables, FontTags.Glyf, data.CopyGlyf());

        if (data.HasVerticalMetrics())
        {
            AddTable(tables, FontTags.Vhea, data.CopyVhea());
            AddTable(tables, FontTags.Vmtx, data.CopyVmtx());
        }

        return CreateFace(tables);
    }

    static FontFace CreateVerticalFace()
    {
        SfntTableStore source = FontInstanceTests.CreateFace()
            .GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            tables.Add(source.GetTable(index));
            index++;
        }

        AddTable(tables, FontTags.Vhea, BuildVhea());
        AddTable(tables, FontTags.Vmtx, BuildVmtx());
        return CreateFace(tables);
    }

    static FontFace CreateFace(
        System.Collections.Generic.List<SfntTableData> tables)
    {
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                System.Array.Empty<FontDiagnostic>()),
            cache);
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[]? bytes)
    {
        FontData data = FontData.Create(bytes!);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildVhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(0)
            .WriteUInt16(1200).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(4)
            .CopyBytes();
    }

    static byte[] BuildVmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(900).WriteInt16(100)
            .WriteUInt16(1000).WriteInt16(200)
            .WriteUInt16(1100).WriteInt16(300)
            .WriteUInt16(1200).WriteInt16(400)
            .CopyBytes();
    }

    static int ReadUInt16(byte[]? data, int offset)
    {
        return data![offset] << 8 | data[offset + 1];
    }

    static int ReadInt16(byte[]? data, int offset)
    {
        return (short)ReadUInt16(data, offset);
    }
}
