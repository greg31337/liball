using ttf;

namespace ttf.Tests;

internal class FontSubsetCmapBuilderTests
{
    [Test]
    public void BuildsMinimalFormatFourForBmpMappings()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        byte[] cmap = FontSubsetCmapBuilder.Create(
            instance,
            new FontSubsetUnicodeMapping[]
            {
                Mapping(1, 'A'),
                Mapping(3, 'C'),
                Mapping(3, 'f', 'i')
            },
            new int[] { 0, 1, -1, 2 },
            new int[] { 'A', 'C' });
        SfntCmapTable? table = Parse(cmap);
        SfntCmapEncodingRecord record = table!.GetEncodingRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetEncodingRecordCount(), Is.EqualTo(1));
            Assert.That(record.GetPlatformId(), Is.EqualTo(3));
            Assert.That(record.GetEncodingId(), Is.EqualTo(1));
            Assert.That(record.GetSubtable().GetFormat(), Is.EqualTo(4));
            Assert.That(table.GetMapping().MapUnicode('A'), Is.EqualTo(1));
            Assert.That(table.GetMapping().MapUnicode('B'), Is.Zero);
            Assert.That(table.GetMapping().MapUnicode('C'), Is.EqualTo(2));
        });
    }

    [Test]
    public void AddsFormatTwelveForNonBmpMappings()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        byte[] cmap = FontSubsetCmapBuilder.Create(
            instance,
            new FontSubsetUnicodeMapping[]
            {
                Mapping(1, 'A'),
                Mapping(2, 0x1F600)
            },
            new int[] { 0, 1, 2, -1 },
            new int[] { 'A', 0x1F600 });
        SfntCmapTable? table = Parse(cmap);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetEncodingRecordCount(), Is.EqualTo(2));
            Assert.That(table.GetEncodingRecord(0).GetEncodingId(),
                Is.EqualTo(1));
            Assert.That(table.GetEncodingRecord(0).GetSubtable().GetFormat(),
                Is.EqualTo(4));
            Assert.That(table.GetEncodingRecord(1).GetEncodingId(),
                Is.EqualTo(10));
            Assert.That(table.GetEncodingRecord(1).GetSubtable().GetFormat(),
                Is.EqualTo(12));
            Assert.That(table.GetMapping().MapUnicode('A'), Is.EqualTo(1));
            Assert.That(table.GetMapping().MapUnicode(0x1F600),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void BuildsDefaultAndNonDefaultFormatFourteenMappings()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        byte[] cmap = FontSubsetCmapBuilder.Create(
            instance,
            new FontSubsetUnicodeMapping[]
            {
                Mapping(1, 'A'),
                Mapping(1, 'A', 0xFE0F),
                Mapping(3, 'A', 0xE0100)
            },
            new int[] { 0, 1, -1, 2 },
            new int[] { 'A' });
        SfntCmapTable? table = Parse(cmap);
        SfntCmapFormat14Data? format = table!
            .GetEncodingRecord(0)
            .GetSubtable()
            .GetFormat14();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetEncodingRecordCount(), Is.EqualTo(2));
            Assert.That(table.GetEncodingRecord(0).GetPlatformId(),
                Is.Zero);
            Assert.That(table.GetEncodingRecord(0).GetEncodingId(),
                Is.EqualTo(5));
            Assert.That(format!.GetSelectorRecordCount(), Is.EqualTo(2));
            Assert.That(format.IsDefaultVariation('A', 0xFE0F), Is.True);
            Assert.That(format.MapNonDefaultVariation('A', 0xE0100),
                Is.EqualTo(2));
            Assert.That(table.GetMapping().MapVariation('A', 0xFE0F),
                Is.EqualTo(1));
            Assert.That(table.GetMapping().MapVariation('A', 0xE0100),
                Is.EqualTo(2));
            Assert.That(table.GetMapping().MapVariation('B', 0xFE0F),
                Is.Zero);
        });
    }

    [Test]
    public void PreservesSymbolEncodingOnlyForExplicitCodePointRequests()
    {
        FontInstance instance = CreateSymbolFace().CreateDefaultInstance();
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder requestedBuilder = new FontSubsetBuilder(
            instance,
            options);
        requestedBuilder.AddCodePoint('A');
        FontSubsetBuilder glyphBuilder = new FontSubsetBuilder(
            instance,
            options);
        glyphBuilder.AddGlyph(1);
        SfntCmapTable? requested = Parse(
            requestedBuilder.Build().CreateCmapData());
        SfntCmapTable? glyphOnly = Parse(
            glyphBuilder.Build().CreateCmapData());

        Assert.Multiple(() =>
        {
            Assert.That(requested!.GetEncodingRecordCount(), Is.EqualTo(2));
            Assert.That(requested.GetEncodingRecord(0).GetEncodingId(),
                Is.Zero);
            Assert.That(requested.GetMapping().IsSymbolMapping(), Is.True);
            Assert.That(requested.GetMapping().MapUnicode('A'), Is.EqualTo(1));
            Assert.That(requested.GetMapping().MapUnicode(0xF041),
                Is.EqualTo(1));
            Assert.That(glyphOnly!.GetEncodingRecordCount(), Is.EqualTo(1));
            Assert.That(glyphOnly.GetEncodingRecord(0).GetEncodingId(),
                Is.EqualTo(1));
            Assert.That(glyphOnly.GetMapping().IsSymbolMapping(), Is.False);
            Assert.That(glyphOnly.GetMapping().MapUnicode('A'), Is.Zero);
        });
    }

    [Test]
    public void OutputIsDeterministicAndRejectsConflictingMappings()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetUnicodeMapping[] mappings =
            new FontSubsetUnicodeMapping[]
            {
                Mapping(2, 'B'),
                Mapping(1, 'A')
            };
        byte[] first = FontSubsetCmapBuilder.Create(
            instance,
            mappings,
            new int[] { 0, 1, 2, -1 },
            new int[] { 'B', 'A' });
        byte[] second = FontSubsetCmapBuilder.Create(
            instance,
            mappings,
            new int[] { 0, 1, 2, -1 },
            new int[] { 'A', 'B' });

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.Throws<FontFormatException>(() =>
                FontSubsetCmapBuilder.Create(
                    instance,
                    new FontSubsetUnicodeMapping[]
                    {
                        Mapping(1, 'A'),
                        Mapping(2, 'A')
                    },
                    new int[] { 0, 1, 2, -1 },
                    new int[] { 'A' }));
        });
    }

    static FontSubsetUnicodeMapping Mapping(
        int glyphId,
        params int[] codePoints)
    {
        return new FontSubsetUnicodeMapping(glyphId, codePoints);
    }

    static SfntCmapTable? Parse(byte[] cmap)
    {
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        AddTable(tables, FontTags.Cmap, cmap);
        AddTable(
            tables,
            FontTags.Maxp,
            FontInstanceTests.CreateFace()
                .GetTableStore()
                .FindTable(FontTags.Maxp)!
                .GetDataSlice()
                .CopyBytes());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return SfntCmapTableParser.FindTable(cache);
    }

    static FontFace CreateSymbolFace()
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            tables.Add(source.GetTable(index));
            index++;
        }

        AddTable(tables, FontTags.Cmap, BuildSymbolCmap());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            sourceFace.GetScalerType(),
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                System.Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildSymbolCmap()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(0)
            .WriteUInt32(12)
            .WriteUInt16(4)
            .WriteUInt16(32)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0xF041)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(0)
            .WriteUInt16(0xF041)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(0x0FC0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }
}
