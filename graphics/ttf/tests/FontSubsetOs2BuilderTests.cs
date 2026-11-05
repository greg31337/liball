using ttf;

namespace ttf.Tests;

internal class FontSubsetOs2BuilderTests
{
    [Test]
    public void RebuildsCharactersRangesMetricsAndPreservesRights()
    {
        FontFace face = CreateOs2Face();
        FontSubsetOptions options = CreateOptions();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options);
        builder.AddCodePoint('A');
        builder.AddCodePoint(0x0411);
        builder.AddCodePoint(0x1F600);
        byte[]? bytes = builder.Build().CreateOs2Data();
        SfntOs2Table? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetAverageCharacterWidth(), Is.EqualTo(800));
            Assert.That(table.GetEmbeddingFlags(), Is.EqualTo(0x0308));
            Assert.That(table.GetUnicodeRange(0), Is.EqualTo(0x00000201U));
            Assert.That(table.GetUnicodeRange(1), Is.EqualTo(0x02000000U));
            Assert.That(table.GetUnicodeRange(2), Is.Zero);
            Assert.That(table.GetUnicodeRange(3), Is.Zero);
            Assert.That(table.GetFirstCharacterIndex(), Is.EqualTo('A'));
            Assert.That(table.GetLastCharacterIndex(), Is.EqualTo(0xFFFF));
            Assert.That(table.GetSelectionFlags(), Is.EqualTo(0x01C0));
            Assert.That(table.GetTypographicAscender(), Is.EqualTo(900));
            Assert.That(table.GetTypographicDescender(), Is.EqualTo(-300));
            Assert.That(table.GetTypographicLineGap(), Is.EqualTo(20));
            Assert.That(table.GetWindowsAscent(), Is.EqualTo(1000));
            Assert.That(table.GetWindowsDescent(), Is.EqualTo(350));
            Assert.That(table.GetCodePageRange(0), Is.EqualTo(4U));
            Assert.That(table.GetCodePageRange(1), Is.Zero);
        });
    }

    [Test]
    public void ClearsAbsentRangesAndUsesCodePageFallback()
    {
        FontFace face = CreateOs2Face();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            CreateOptions());
        builder.AddCodePoint('A');
        SfntOs2Table? table = Parse(builder.Build().CreateOs2Data());

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetAverageCharacterWidth(), Is.EqualTo(700));
            Assert.That(table.GetUnicodeRange(0), Is.EqualTo(1U));
            Assert.That(table.GetUnicodeRange(1), Is.Zero);
            Assert.That(table.GetUnicodeRange(2), Is.Zero);
            Assert.That(table.GetUnicodeRange(3), Is.Zero);
            Assert.That(table.GetFirstCharacterIndex(), Is.EqualTo('A'));
            Assert.That(table.GetLastCharacterIndex(), Is.EqualTo('A'));
            Assert.That(table.GetCodePageRange(0), Is.EqualTo(1U));
            Assert.That(table.GetCodePageRange(1), Is.Zero);
        });
    }

    [Test]
    public void ClampsSupplementaryOnlyCharacterIndices()
    {
        FontFace face = CreateOs2Face();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            CreateOptions());
        builder.AddCodePoint(0x1F600);
        SfntOs2Table? table = Parse(builder.Build().CreateOs2Data());

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFirstCharacterIndex(), Is.EqualTo(0xFFFF));
            Assert.That(table.GetLastCharacterIndex(), Is.EqualTo(0xFFFF));
            Assert.That(table.HasUnicodeRange(57), Is.True);
        });
    }

    [Test]
    public void ReturnsNullWhenSourceHasNoOs2Table()
    {
        FontSubsetBuilder builder = new FontSubsetBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            CreateOptions());
        builder.AddGlyph(1);

        Assert.That(builder.Build().CreateOs2Data(), Is.Null);
    }

    static FontSubsetOptions CreateOptions()
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        return options;
    }

    static FontFace CreateOs2Face()
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        FontInstance sourceInstance = sourceFace.CreateDefaultInstance();
        byte[] cmap = FontSubsetCmapBuilder.Create(
            sourceInstance,
            new FontSubsetUnicodeMapping[]
            {
                new FontSubsetUnicodeMapping(1, new int[] { 'A' }),
                new FontSubsetUnicodeMapping(2, new int[] { 0x0411 }),
                new FontSubsetUnicodeMapping(3, new int[] { 0x1F600 })
            },
            new int[] { 0, 1, 2, 3 },
            new int[] { 'A', 0x0411, 0x1F600 });
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            tables.Add(source.GetTable(index));
            index++;
        }

        AddTable(tables, FontTags.Cmap, cmap);
        AddTable(tables, FontTags.Os2, BuildOs2());
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

    static SfntOs2Table? Parse(byte[]? os2)
    {
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        AddTable(tables, FontTags.Os2, os2);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return SfntOs2TableParser.FindTable(cache);
    }

    static byte[] BuildOs2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5)
            .WriteInt16(-123)
            .WriteUInt16(400)
            .WriteUInt16(5)
            .WriteUInt16(0x0308)
            .WriteInt16(601)
            .WriteInt16(602)
            .WriteInt16(-3)
            .WriteInt16(4)
            .WriteInt16(701)
            .WriteInt16(702)
            .WriteInt16(-5)
            .WriteInt16(6)
            .WriteInt16(45)
            .WriteInt16(250)
            .WriteInt16(0x0802);
        builder.WriteZeros(10);
        return builder
            .WriteUInt32(0xFFFFFFFFU)
            .WriteUInt32(0xFFFFFFFFU)
            .WriteUInt32(0xFFFFFFFFU)
            .WriteUInt32(0x07FFFFFFU)
            .WriteUInt32(0x54455354U)
            .WriteUInt16(0x01C0)
            .WriteUInt16(0x20)
            .WriteUInt16(0xFFFD)
            .WriteInt16(900)
            .WriteInt16(-300)
            .WriteInt16(20)
            .WriteUInt16(1000)
            .WriteUInt16(350)
            .WriteUInt32(1)
            .WriteUInt32(0x80000000U)
            .WriteInt16(450)
            .WriteInt16(700)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(3)
            .WriteUInt16(160)
            .WriteUInt16(720)
            .CopyBytes();
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
}
