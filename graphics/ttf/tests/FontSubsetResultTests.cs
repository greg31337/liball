using ttf;

namespace ttf.Tests;

internal class FontSubsetResultTests
{
    [Test]
    public void ReturnsGeneratedFontAndAllSubsetReportsTogether()
    {
        int unknownTag = Tag("ZZZZ");
        FontFace face = CreateFace(unknownTag);
        FontInstance instance = face.CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        builder.AddCodePoint(0x41);

        FontSubsetResult result = builder.BuildResult();
        byte[] firstBytes = result.CopySfntBytes();
        FontSubset subset = builder.Build();
        byte[] secondBytes = subset.CopyFontBytes();
        FontSubsetUnicodeMap[] unicodeMappings =
            result.GetUnicodeMappings();
        FontSubsetTableReport tableReport = result.GetTableReport();
        FontDiagnostic[] repairs = result.GetRepairs();
        FontDiagnostic[] diagnostics = result.GetDiagnostics();
        FontSubsetOptions effectiveOptions = result.GetEffectiveOptions();
        FontSubsetPrefixReport prefixReport = result.GetPrefixReport();
        FontSubsetClosureReport closureReport = result.GetClosureReport();
        int[] compositeGlyphIds = closureReport.GetCompositeGlyphIds();
        compositeGlyphIds[0] = 99;
        FontPackage reopened = FontLoader.Load(
            firstBytes,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(firstBytes, Is.EqualTo(secondBytes));
            Assert.That(FontChecksum.VerifyFont(firstBytes), Is.True);
            Assert.That(reopened.GetFaceCount(), Is.EqualTo(1));
            Assert.That(result.GetSubsetName(),
                Does.Match("^[A-Z]{6}\\+Unnamed-Regular$"));
            Assert.That(subset.GetSubsetName(),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(result.GetOldToNewGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, -1, -1 }));
            Assert.That(result.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(unicodeMappings, Has.Length.EqualTo(1));
            Assert.That(unicodeMappings[0].GetOriginalGlyphId(),
                Is.EqualTo(1));
            Assert.That(unicodeMappings[0].GetSubsetGlyphId(),
                Is.EqualTo(1));
            Assert.That(unicodeMappings[0].GetUnicodeSequence(),
                Is.EqualTo(new int[] { 0x41 }));
            Assert.That(tableReport.GetRetainedTableTags(),
                Does.Contain(FontTags.Glyf));
            Assert.That(tableReport.GetRetainedTableTags(),
                Does.Contain(FontTags.Cmap));
            Assert.That(tableReport.GetRetainedTableTags(),
                Does.Contain(FontTags.Name));
            Assert.That(tableReport.GetDroppedTableTags(),
                Does.Contain(unknownTag));
            Assert.That(tableReport.GetDroppedTableTags(),
                Does.Contain(FontTags.Fpgm));
            Assert.That(tableReport.GetRewrittenTableTags(),
                Does.Contain(FontTags.Cmap));
            Assert.That(tableReport.GetRewrittenTableTags(),
                Does.Contain(FontTags.Glyf));
            Assert.That(tableReport.GetRewrittenTableTags(),
                Does.Contain(FontTags.Name));
            Assert.That(result.GetSourceInstance(), Is.SameAs(instance));
            Assert.That(result.GetOutputOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(result.HadVariationData(), Is.False);
            Assert.That(result.WasVariationFrozen(), Is.False);
            Assert.That(effectiveOptions.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(effectiveOptions.GetGlyphRetentionMode(),
                Is.EqualTo(FontSubsetGlyphRetentionMode.Compact));
            Assert.That(effectiveOptions.GetRecommendedGlyphPolicy(),
                Is.EqualTo(FontSubsetRecommendedGlyphPolicy.RequestedOnly));
            Assert.That(effectiveOptions.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Drop));
            Assert.That(effectiveOptions.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Drop));
            Assert.That(effectiveOptions.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Drop));
            Assert.That(effectiveOptions.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Drop));
            Assert.That(effectiveOptions.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(effectiveOptions.GetUnknownTablePolicy(),
                Is.EqualTo(FontSubsetUnknownTablePolicy.Drop));
            Assert.That(effectiveOptions.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.Reject));
            Assert.That(prefixReport.GetPrefix(),
                Is.EqualTo(result.GetSubsetName().Substring(0, 6)));
            Assert.That(prefixReport.WasDeterministicallyGenerated(), Is.True);
            Assert.That(prefixReport.WasOverrideUsed(), Is.False);
            Assert.That(prefixReport.GetSourceFaceIdentity(),
                Does.Match("^[0-9A-F]{64}$"));
            Assert.That(prefixReport.GetCoordinateKey(),
                Is.EqualTo(instance.GetCoordinates().GetKey()));
            Assert.That(prefixReport.GetSourcePostScriptName(),
                Is.EqualTo("Unnamed-Regular"));
            Assert.That(prefixReport.GetRetainedGlyphIds(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(prefixReport.GetPolicyOptions().GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(repairs, Has.Length.EqualTo(2));
            Assert.That(repairs[0].GetCode(),
                Is.EqualTo(FontDiagnosticCode.RepairApplied));
            Assert.That(ContainsPath(repairs, "test.validationRepair"), Is.True);
            Assert.That(ContainsPath(repairs, "test.tableRepair"), Is.True);
            Assert.That(ContainsDiagnostic(
                diagnostics,
                FontDiagnosticCode.TableDropped), Is.True);
            Assert.That(ContainsDiagnostic(
                diagnostics,
                FontDiagnosticCode.RepairApplied), Is.True);
            Assert.That(result.GetEmbeddingDecision().HasPolicyConflict(),
                Is.False);
            Assert.That(closureReport.GetRecommendedGlyphIds(), Is.Empty);
            Assert.That(closureReport.IsLayoutApplicable(), Is.False);
            Assert.That(closureReport.IsColorApplicable(), Is.False);
            Assert.That(closureReport.IsCffApplicable(), Is.False);
            Assert.That(closureReport.GetCompositeGlyphIds(),
                Is.EqualTo(subset.GetClosureReport().GetCompositeGlyphIds()));
            Assert.That(closureReport.GetCompositeGlyphIds()[0],
                Is.Not.EqualTo(99));
        });
    }

    [Test]
    public void GeneratesCffSubsetBytesWithCompactGlyphMaps()
    {
        FontFace face = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options);
        builder.AddGlyph(1);

        FontSubsetResult result = builder.BuildResult();
        byte[] bytes = result.CopySfntBytes();
        byte[] maxp = CopyTable(bytes, FontTags.Maxp);

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(bytes), Is.True);
            Assert.That(CopyTable(bytes, FontTags.Cff), Is.Not.Empty);
            Assert.That(HasTable(bytes, FontTags.Cff2), Is.False);
            Assert.That(ReadUInt16(maxp, 4), Is.EqualTo(2));
            Assert.That(result.GetOldToNewGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, -1, -1 }));
            Assert.That(result.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.GetClosureReport().IsCffApplicable(), Is.True);
            Assert.That(result.GetOutputOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(result.HadVariationData(), Is.False);
            Assert.That(result.WasVariationFrozen(), Is.False);
        });
    }

    [Test]
    public void FullFontResultReportsEveryPreservedSourceTable()
    {
        int unknownTag = Tag("ZZZZ");
        FontFace face = CreateFace(unknownTag);
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        FontSubsetResult result = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options).BuildResult();

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(result.CopySfntBytes()),
                Is.True);
            Assert.That(result.GetOldToNewGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.GetTableReport().GetRetainedTableTags(),
                Does.Contain(unknownTag));
            Assert.That(result.GetTableReport().GetPreservedTableTags(),
                Does.Contain(unknownTag));
            Assert.That(result.GetTableReport().GetDroppedTableTags(),
                Is.Empty);
            Assert.That(result.GetEffectiveOptions().GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(result.GetPrefixReport().GetPrefix(), Is.Empty);
            Assert.That(result.GetPrefixReport().WasOverrideUsed(), Is.False);
            Assert.That(result.GetPrefixReport()
                .WasDeterministicallyGenerated(), Is.False);
            Assert.That(result.GetRepairs(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ReportsCallerPrefixOverrideAndItsEffectiveInputs()
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetSubsetPrefixOverride("ABCDEF");
        FontSubsetBuilder builder = new FontSubsetBuilder(
            CreateFace(Tag("ZZZZ")).CreateDefaultInstance(),
            options);
        builder.AddGlyph(1);

        FontSubsetResult result = builder.BuildResult();
        FontSubsetPrefixReport report = result.GetPrefixReport();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSubsetName(),
                Is.EqualTo("ABCDEF+Unnamed-Regular"));
            Assert.That(report.GetPrefix(), Is.EqualTo("ABCDEF"));
            Assert.That(report.WasOverrideUsed(), Is.True);
            Assert.That(report.WasDeterministicallyGenerated(), Is.False);
            Assert.That(report.GetPolicyOptions().GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.That(report.GetRetainedGlyphIds(),
                Is.EqualTo(new int[] { 0, 1 }));
        });
    }

    [Test]
    public void ResultCollectionsAndBytesAreDefensiveCopies()
    {
        FontSubsetBuilder builder = new FontSubsetBuilder(
            CreateFace(Tag("ZZZZ")).CreateDefaultInstance(),
            FontSubsetOptions.CreateStrict());
        builder.AddCodePoint(0x41);
        FontSubsetResult result = builder.BuildResult();
        byte[] bytes = result.CopySfntBytes();
        int[] oldToNew = result.GetOldToNewGlyphMap();
        int[] retained = result.GetTableReport().GetRetainedTableTags();
        int[] rewritten = result.GetTableReport().GetRewrittenTableTags();
        int[] prefixGlyphs = result.GetPrefixReport().GetRetainedGlyphIds();
        FontSubsetOptions effectiveOptions = result.GetEffectiveOptions();
        FontSubsetOptions prefixOptions = result.GetPrefixReport()
            .GetPolicyOptions();
        FontSubsetUnicodeMap?[] mappings = result.GetUnicodeMappings();
        bytes[0] = 99;
        oldToNew[0] = 99;
        retained[0] = 0;
        rewritten[0] = 0;
        prefixGlyphs[0] = 99;
        effectiveOptions.SetMetadataPolicy(FontSubsetMetadataPolicy.RetainSafe);
        prefixOptions.SetMetadataPolicy(FontSubsetMetadataPolicy.RetainSafe);
        mappings[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.CopySfntBytes()[0], Is.Not.EqualTo(99));
            Assert.That(result.GetOldToNewGlyphMap()[0], Is.Zero);
            Assert.That(result.GetTableReport().GetRetainedTableTags()[0],
                Is.Not.Zero);
            Assert.That(result.GetTableReport().GetRewrittenTableTags()[0],
                Is.Not.Zero);
            Assert.That(result.GetPrefixReport().GetRetainedGlyphIds()[0],
                Is.Zero);
            Assert.That(result.GetEffectiveOptions().GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(result.GetPrefixReport().GetPolicyOptions()
                .GetMetadataPolicy(), Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(result.GetUnicodeMappings()[0], Is.Not.Null);
        });
    }

    static FontFace CreateFace(int unknownTag)
    {
        FontFace source = FontInstanceTests.CreateFace();
        SfntTableStore sourceStore = source.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < sourceStore.GetTableCount())
        {
            tables.Add(sourceStore.GetTable(index));
            index++;
        }

        tables.Add(CreateTable(
            FontTags.Cmap,
            BuildCmap(),
            SfntTableClassification.Known));
        tables.Add(CreateTable(
            FontTags.Os2,
            BuildOs2(),
            SfntTableClassification.Known));
        tables.Add(CreateTable(
            FontTags.Post,
            BuildPost(),
            SfntTableClassification.Known));
        tables.Add(CreateTable(
            unknownTag,
            new byte[] { 1, 2, 3 },
            SfntTableClassification.Unknown));
        FontDiagnostic tableRepair = new FontDiagnostic(
            FontDiagnosticSeverity.Information,
            FontDiagnosticCode.RepairApplied,
            "A deterministic table repair was applied.",
            0,
            FontTags.Head,
            0,
            "test.tableRepair");
        FontDiagnostic validationRepair = new FontDiagnostic(
            FontDiagnosticSeverity.Information,
            FontDiagnosticCode.RepairApplied,
            "A deterministic validation repair was applied.",
            0,
            FontTags.Head,
            4,
            "test.validationRepair");
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                new FontDiagnostic[] { tableRepair }),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                new FontDiagnostic[] { validationRepair }),
            cache);
    }

    static SfntTableData CreateTable(
        int tag,
        byte[] bytes,
        SfntTableClassification classification)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            classification,
            true);
    }

    static byte[] BuildCmap()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(10)
            .WriteUInt32(12)
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(28)
            .WriteUInt32(0)
            .WriteUInt32(1)
            .WriteUInt32(0x41)
            .WriteUInt32(0x41)
            .WriteUInt32(1)
            .CopyBytes();
    }

    static byte[] BuildOs2()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteInt16(500)
            .WriteUInt16(400)
            .WriteUInt16(5)
            .WriteUInt16(0)
            .WriteZeros(52)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0xFFFF)
            .WriteInt16(800)
            .WriteInt16(-200)
            .WriteInt16(0)
            .WriteUInt16(800)
            .WriteUInt16(200)
            .CopyBytes();
    }

    static byte[] BuildPost()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00030000U)
            .WriteZeros(28)
            .CopyBytes();
    }

    static bool ContainsDiagnostic(
        FontDiagnostic[] diagnostics,
        FontDiagnosticCode code)
    {
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetCode() == code) { return true; }
            index++;
        }

        return false;
    }

    static bool ContainsPath(
        FontDiagnostic[] diagnostics,
        string logicalPath)
    {
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetLogicalPath() == logicalPath)
            {
                return true;
            }
            index += 1;
        }

        return false;
    }

    static bool HasTable(byte[] bytes, int tag)
    {
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        int index = 0;

        while (index < directory.GetTableCount())
        {
            if (directory.GetTableRecord(index).GetTag() == tag)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static byte[] CopyTable(byte[] bytes, int tag)
    {
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        int index = 0;

        while (index < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(index);

            if (record.GetTag() == tag)
            {
                return bytes.AsSpan(
                    (int)record.GetOffset(),
                    (int)record.GetLength()).ToArray();
            }

            index++;
        }

        throw new System.InvalidOperationException(
            "The requested table was not found.");
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] * 256 + data[offset + 1];
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
