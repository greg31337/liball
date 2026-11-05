using ttf;

namespace ttf.Tests;

internal class FontSubsetTrueTypeHintingTests
{
    [Test]
    public void RetainPreservesGlobalTablesAndRebuildsHintMaxima()
    {
        SfntSubsetTrueTypeData data = BuildSubset(
            CreateResourceFace().CreateDefaultInstance(),
            FontSubsetHintPolicy.Retain,
            2).CreateTrueTypeTables();
        byte[] maxp = data.CopyMaxp();
        FontFace output = CreateOutputFace(data);
        FontInstance outputInstance = output.CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.That(data.HasFpgm(), Is.True);
            Assert.That(data.CopyFpgm(),
                Is.EqualTo(new byte[] { 0xB0, 0x01, 0x2C }));
            Assert.That(data.HasPrep(), Is.True);
            Assert.That(data.CopyPrep(),
                Is.EqualTo(new byte[] { 0xB0, 0x02 }));
            Assert.That(data.HasCvt(), Is.True);
            Assert.That(data.CopyCvt(),
                Is.EqualTo(new byte[] { 0x00, 0x10, 0xFF, 0xF0 }));
            Assert.That(ReadUInt16(maxp, 14), Is.EqualTo(2));
            Assert.That(ReadUInt16(maxp, 16), Is.EqualTo(20));
            Assert.That(ReadUInt16(maxp, 18), Is.EqualTo(30));
            Assert.That(ReadUInt16(maxp, 20), Is.EqualTo(40));
            Assert.That(ReadUInt16(maxp, 22), Is.EqualTo(50));
            Assert.That(ReadUInt16(maxp, 24), Is.EqualTo(60));
            Assert.That(ReadUInt16(maxp, 26), Is.EqualTo(2));
            Assert.That(output.HasTable(FontTags.Fpgm), Is.True);
            Assert.That(output.HasTable(FontTags.Prep), Is.True);
            Assert.That(output.HasTable(FontTags.Cvt), Is.True);
            Assert.That(outputInstance.GetGlyphOutline(0)
                .CopyInstructions(),
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(outputInstance.GetGlyphOutline(2)
                .CopyInstructions(),
                Is.EqualTo(new byte[] { 0xCC }));
        });
    }

    [Test]
    public void DropRemovesAllHintsAndClearsHintMaxima()
    {
        SfntSubsetTrueTypeData data = BuildSubset(
            CreateResourceFace().CreateDefaultInstance(),
            FontSubsetHintPolicy.Drop,
            2).CreateTrueTypeTables();
        byte[] maxp = data.CopyMaxp();
        FontFace output = CreateOutputFace(data);
        FontInstance outputInstance = output.CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.That(data.HasFpgm(), Is.False);
            Assert.That(data.CopyFpgm(), Is.Null);
            Assert.That(data.HasPrep(), Is.False);
            Assert.That(data.CopyPrep(), Is.Null);
            Assert.That(data.HasCvt(), Is.False);
            Assert.That(data.CopyCvt(), Is.Null);
            Assert.That(ReadUInt16(maxp, 14), Is.EqualTo(1));
            Assert.That(ReadUInt16(maxp, 16), Is.Zero);
            Assert.That(ReadUInt16(maxp, 18), Is.Zero);
            Assert.That(ReadUInt16(maxp, 20), Is.Zero);
            Assert.That(ReadUInt16(maxp, 22), Is.Zero);
            Assert.That(ReadUInt16(maxp, 24), Is.Zero);
            Assert.That(ReadUInt16(maxp, 26), Is.Zero);
            Assert.That(output.HasTable(FontTags.Fpgm), Is.False);
            Assert.That(output.HasTable(FontTags.Prep), Is.False);
            Assert.That(output.HasTable(FontTags.Cvt), Is.False);
            Assert.That(outputInstance.GetGlyphOutline(0)
                .GetInstructionLength(), Is.Zero);
            Assert.That(outputInstance.GetGlyphOutline(2)
                .GetInstructionLength(), Is.Zero);
        });
    }

    [Test]
    public void RetainUsesInstancedCvtAndReturnsIndependentCopies()
    {
        FontFace face = FontInstanceTests.CreateVariableFace(false);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        SfntSubsetTrueTypeData data = BuildSubset(
            face.CreateInstance(coordinates),
            FontSubsetHintPolicy.Retain,
            0).CreateTrueTypeTables();
        byte[]? fontProgram = data.CopyFpgm();
        byte[]? prep = data.CopyPrep();
        byte[]? cvt = data.CopyCvt();
        fontProgram![0] = 0;
        prep![0] = 0;
        cvt![0] = 0x7F;

        Assert.Multiple(() =>
        {
            Assert.That(data.CopyFpgm(),
                Is.EqualTo(new byte[] { 0xB0, 0x01, 0x2C }));
            Assert.That(data.CopyPrep(),
                Is.EqualTo(new byte[] { 0xB0, 0x02 }));
            Assert.That(data.CopyCvt(), Is.EqualTo(new byte[]
            {
                0, 105, 0, 210, 0xFF, 0x92, 0, 20
            }));
        });
    }

    static FontSubset BuildSubset(
        FontInstance instance,
        FontSubsetHintPolicy hintPolicy,
        int glyphId)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetHintPolicy(hintPolicy);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);
        builder.AddGlyph(glyphId);
        return builder.Build();
    }

    static FontFace CreateResourceFace()
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            SfntTableData table = source.GetTable(index);

            if (table.GetTag() != FontTags.Maxp)
            {
                tables.Add(table);
            }

            index++;
        }

        byte[] maxp = source.FindTable(FontTags.Maxp)!
            .GetDataSlice()
            .CopyBytes();
        WriteUInt16(maxp, 14, 2);
        WriteUInt16(maxp, 16, 20);
        WriteUInt16(maxp, 18, 30);
        WriteUInt16(maxp, 20, 40);
        WriteUInt16(maxp, 22, 50);
        WriteUInt16(maxp, 24, 60);
        WriteUInt16(maxp, 26, 99);
        AddTable(tables, FontTags.Maxp, maxp);
        return CreateFace(tables);
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

        if (data.HasFpgm()) { AddTable(tables, FontTags.Fpgm, data.CopyFpgm()); }
        if (data.HasPrep()) { AddTable(tables, FontTags.Prep, data.CopyPrep()); }
        if (data.HasCvt()) { AddTable(tables, FontTags.Cvt, data.CopyCvt()); }
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

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] << 8 | data[offset + 1];
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }
}
