using ttf;

namespace ttf.Tests;

internal class FontSubsetUnknownTablesBuilderTests
{
    [Test]
    public void CopiesOnlyAuditedSafeTablesWithPreservedGlyphIds()
    {
        int ltag = Tag("ltag");
        int morx = Tag("morx");
        int privateTag = Tag("ZZZZ");
        FontFace face = CreateFace(new SfntTableData[]
        {
            CreateTable(privateTag, new byte[] { 9 },
                SfntTableClassification.Unknown, true),
            CreateTable(ltag, new byte[] { 1, 2, 3 },
                SfntTableClassification.Unsupported, true),
            CreateTable(morx, new byte[] { 4, 5 },
                SfntTableClassification.Unsupported, true)
        });
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Preserve);
        options.SetUnknownTablePolicy(
            FontSubsetUnknownTablePolicy.RetainSafe);

        FontSubsetUnknownTables result =
            FontSubsetUnknownTablesBuilder.Create(face, options);
        byte[]? copied = result.CopyTableData(ltag);
        copied![0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTableCount(), Is.EqualTo(1));
            Assert.That(result.GetTable(0).GetTag(), Is.EqualTo(ltag));
            Assert.That(result.CopyTableData(ltag),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.CopyTableData(morx), Is.Null);
            Assert.That(result.CopyTableData(privateTag), Is.Null);
            Assert.That(result.CopyDroppedTags(),
                Is.EqualTo(new int[] { privateTag, morx }));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(3));
            Assert.That(result.GetDiagnostic(0).GetTableTag(),
                Is.EqualTo(privateTag));
            Assert.That(result.GetDiagnostic(0).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(result.GetDiagnostic(1).GetTableTag(),
                Is.EqualTo(ltag));
            Assert.That(result.GetDiagnostic(1).GetCode(),
                Is.EqualTo(FontDiagnosticCode.TablePreservedAsRaw));
            Assert.That(result.GetDiagnostic(2).GetTableTag(),
                Is.EqualTo(morx));
            Assert.That(result.GetDiagnostic(2).GetMessage(),
                Does.Contain("unrewritten glyph references"));
        });
    }

    [Test]
    public void RetainSafeStillRejectsCompactFullFontOutput()
    {
        int ltag = Tag("ltag");
        FontFace face = CreateFace(new SfntTableData[]
        {
            CreateTable(ltag, new byte[] { 1 },
                SfntTableClassification.Unsupported, true)
        });
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        options.SetUnknownTablePolicy(
            FontSubsetUnknownTablePolicy.RetainSafe);

        FontSubsetUnknownTables result =
            FontSubsetUnknownTablesBuilder.Create(face, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTableCount(), Is.Zero);
            Assert.That(result.CopyDroppedTags(), Is.EqualTo(new int[] { ltag }));
            Assert.That(result.GetDiagnostic(0).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(result.GetDiagnostic(0).GetMessage(),
                Does.Contain("preserved glyph identifiers"));
        });
    }

    [Test]
    public void MissingRawPreservationPreventsSafeCopy()
    {
        int featureTag = Tag("Feat");
        FontFace face = CreateFace(new SfntTableData[]
        {
            CreateTable(featureTag, new byte[] { 6, 7 },
                SfntTableClassification.Unsupported, false)
        });
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Preserve);
        options.SetUnknownTablePolicy(
            FontSubsetUnknownTablePolicy.RetainSafe);

        FontSubsetUnknownTables result =
            FontSubsetUnknownTablesBuilder.Create(face, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTableCount(), Is.Zero);
            Assert.That(result.CopyDroppedTags(),
                Is.EqualTo(new int[] { featureTag }));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.TableDropped));
        });
    }

    static FontFace CreateFace(SfntTableData[] additionalTables)
    {
        SfntTableData[] tables = new SfntTableData[additionalTables.Length + 1];
        tables[0] = CreateTable(
            FontTags.Maxp,
            new TestBinaryBuilder().WriteUInt32(0x00005000U)
                .WriteUInt16(1).CopyBytes(),
            SfntTableClassification.Known,
            true);
        System.Array.Copy(
            additionalTables, 0, tables, 1, additionalTables.Length);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(
        int tag,
        byte[] bytes,
        SfntTableClassification classification,
        bool rawDataAvailable)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            classification,
            rawDataAvailable);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
