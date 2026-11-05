using ttf;

namespace ttf.Tests;

internal class SfntGsubFixture
{
    internal byte[]? Bytes;
    internal int FirstLookup;
    internal int SingleFormat1;
    internal int SingleFormat2;
    internal int Multiple;
    internal int MultipleSequence;
    internal int Alternate;
    internal int Ligature;
    internal int FirstLigature;
    internal int SecondLigature;
}

internal class SfntGsubExecutorTests
{
    [Test]
    public void ParsesSingleMultipleAlternateAndLigatureSubtables()
    {
        SfntGsubTable? table = Parse(BuildGsub().Bytes);
        SfntGsubLookup singles = table!.GetLookup(0);
        SfntGsubMultipleSubstitution multiple = table.GetLookup(1)
            .GetMultipleSubstitution(0);
        SfntGsubAlternateSet? alternates = table.GetLookup(2)
            .GetAlternateSubstitution(0)
            .FindAlternateSet(4);
        SfntGsubLigatureSet? ligatures = table.GetLookup(3)
            .GetLigatureSubstitution(0)
            .FindLigatureSet(5);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetLayoutTable().GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetLookupCount(), Is.EqualTo(4));
            Assert.That(singles.GetSubtableCount(), Is.EqualTo(2));
            Assert.That(singles.GetSingleSubstitution(0).GetFormat(),
                Is.EqualTo(1));
            Assert.That(singles.GetSingleSubstitution(0)
                .GetSubstituteGlyphId(1), Is.EqualTo(11));
            Assert.That(singles.GetSingleSubstitution(1).GetFormat(),
                Is.EqualTo(2));
            Assert.That(singles.GetSingleSubstitution(1)
                .GetSubstituteGlyphId(2), Is.EqualTo(12));
            Assert.That(multiple.GetSequenceCount(), Is.EqualTo(1));
            Assert.That(multiple.FindSequence(3)!.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(multiple.FindSequence(3)!.GetGlyphId(1), Is.EqualTo(14));
            Assert.That(alternates!.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(alternates.GetGlyphId(0), Is.EqualTo(15));
            Assert.That(ligatures!.GetLigatureCount(), Is.EqualTo(2));
            Assert.That(ligatures.GetLigature(0).GetLigatureGlyphId(),
                Is.EqualTo(17));
            Assert.That(ligatures.GetLigature(0).GetComponentCount(),
                Is.EqualTo(3));
            Assert.That(ligatures.GetLigature(0).GetComponentGlyphId(1),
                Is.EqualTo(7));
        });
    }

    [Test]
    public void ResolvesCoverageRangeIndexesWithoutExpandingRanges()
    {
        SfntCoverageTable coverage = new SfntCoverageTable(
            2,
            new int[0],
            new SfntCoverageRange[]
            {
                new SfntCoverageRange(10, 12, 0),
                new SfntCoverageRange(20, 21, 3)
            },
            5);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetGlyphId(0), Is.EqualTo(10));
            Assert.That(coverage.GetGlyphId(2), Is.EqualTo(12));
            Assert.That(coverage.GetGlyphId(3), Is.EqualTo(20));
            Assert.That(coverage.GetGlyphId(4), Is.EqualTo(21));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                coverage.GetGlyphId(5));
        });
    }

    [Test]
    public void AppliesSingleDeltaModulo65536()
    {
        SfntGsubSingleSubstitution substitution =
            new SfntGsubSingleSubstitution(
                1,
                new SfntCoverageTable(
                    1,
                    new int[] { 65530 },
                    new SfntCoverageRange[0],
                    1),
                10,
                new int[0]);

        Assert.That(substitution.GetSubstituteGlyphId(65530), Is.EqualTo(4));
    }

    [Test]
    public void AppliesBothSingleFormatsAndPreservesMetadata()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 2, 20 },
            new int[] { 4, 8, 12 },
            new int[] { 2, 3, 1 },
            new int[] { 1, 2, 1 },
            new int[] { 0, 3, 0 });
        int count = Apply(0, buffer, null, 0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 11, 12, 20 }));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(8));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(3));
            Assert.That(buffer.GetLigatureComponentCount(1), Is.EqualTo(2));
            Assert.That(buffer.GetLigatureComponentIndex(1), Is.EqualTo(3));
        });
    }

    [Test]
    public void TestsTheOuterCurrentGlyphDespiteIgnoreFlags()
    {
        SfntGsubFixture fixture = BuildGsub();
        PatchUInt16(fixture.Bytes, fixture.FirstLookup + 2, 0x0008);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1 });
        int count = SfntGsubExecutor.ApplyLookup(
            Parse(fixture.Bytes)!,
            0,
            buffer,
            CreateSingleMarkGdef(),
            SfntLayoutTraversalDirection.Forward,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(11));
        });
    }

    [Test]
    public void AppliesMultipleSubstitutionOnceAndPropagatesCluster()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 3, 20 },
            new int[] { 5, 9 },
            new int[] { 4, 1 },
            new int[] { 3, 1 },
            new int[] { 2, 0 });
        int count = Apply(1, buffer, null, 0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 13, 14, 20 }));
            Assert.That(buffer.GetClusterStart(0), Is.EqualTo(5));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(5));
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(4));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(4));
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(1));
            Assert.That(buffer.GetLigatureComponentCount(1), Is.EqualTo(1));
            Assert.That(buffer.GetLigatureComponentIndex(0), Is.EqualTo(2));
            Assert.That(buffer.GetLigatureComponentIndex(1), Is.EqualTo(3));
        });
    }

    [Test]
    public void UsesOneBasedAlternateValuesAndRejectsUnavailableChoices()
    {
        SfntLayoutGlyphBuffer first = SfntLayoutGlyphBuffer.Create(
            new int[] { 4 });
        SfntLayoutGlyphBuffer second = SfntLayoutGlyphBuffer.Create(
            new int[] { 4 });
        SfntLayoutGlyphBuffer disabled = SfntLayoutGlyphBuffer.Create(
            new int[] { 4 });
        SfntLayoutGlyphBuffer unavailable = SfntLayoutGlyphBuffer.Create(
            new int[] { 4 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(2, first, null, 1), Is.EqualTo(1));
            Assert.That(first.GetGlyphId(0), Is.EqualTo(15));
            Assert.That(Apply(2, second, null, 2), Is.EqualTo(1));
            Assert.That(second.GetGlyphId(0), Is.EqualTo(16));
            Assert.That(Apply(2, disabled, null, 0), Is.Zero);
            Assert.That(disabled.GetGlyphId(0), Is.EqualTo(4));
            Assert.That(Apply(2, unavailable, null, 3), Is.Zero);
            Assert.That(unavailable.GetGlyphId(0), Is.EqualTo(4));
        });
    }

    [Test]
    public void AppliesPreferredLigatureAcrossIgnoredMarks()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5, 8, 6, 9, 7 },
            new int[] { 0, 1, 2, 3, 4 },
            new int[] { 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1 },
            new int[] { 0, 0, 0, 0, 0 });
        int count = Apply(3, buffer, CreateMarkGdef(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 17, 8, 9 }));
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(3));
            Assert.That(buffer.GetLigatureComponentIndex(1), Is.Zero);
            Assert.That(buffer.GetLigatureComponentIndex(2), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterStart(1), Is.Zero);
            Assert.That(buffer.GetClusterStart(2), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(5));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(5));
            Assert.That(buffer.GetClusterLength(2), Is.EqualTo(5));
        });
    }

    [Test]
    public void FallsBackToTheNextPreferredLigatureAndSumsComponents()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5, 6, 20 },
            new int[] { 2, 4, 8 },
            new int[] { 2, 2, 1 },
            new int[] { 2, 3, 1 },
            new int[] { 0, 0, 0 });
        int count = Apply(3, buffer, CreateMarkGdef(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 18, 20 }));
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(5));
            Assert.That(buffer.GetClusterStart(0), Is.EqualTo(2));
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(4));
        });
    }

    [Test]
    public void AppliesSupportedLookupsBackwardWithoutReprocessingOutputs()
    {
        SfntGsubTable? table = Parse(BuildGsub().Bytes);
        SfntLayoutGlyphBuffer singles = SfntLayoutGlyphBuffer.Create(
            new int[] { 2, 1 });
        SfntLayoutGlyphBuffer multiples = SfntLayoutGlyphBuffer.Create(
            new int[] { 20, 3 });
        SfntLayoutGlyphBuffer ligature = SfntLayoutGlyphBuffer.Create(
            new int[] { 6, 5 });

        int singleCount = SfntGsubExecutor.ApplyLookup(
            table!,
            0,
            singles,
            null!,
            SfntLayoutTraversalDirection.Backward,
            0);
        int multipleCount = SfntGsubExecutor.ApplyLookup(
            table!,
            1,
            multiples,
            null!,
            SfntLayoutTraversalDirection.Backward,
            0);
        int ligatureCount = SfntGsubExecutor.ApplyLookup(
            table!,
            3,
            ligature,
            CreateMarkGdef(),
            SfntLayoutTraversalDirection.Backward,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(singleCount, Is.EqualTo(2));
            Assert.That(CopyGlyphIds(singles), Is.EqualTo(
                new int[] { 12, 11 }));
            Assert.That(multipleCount, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(multiples), Is.EqualTo(
                new int[] { 20, 13, 14 }));
            Assert.That(ligatureCount, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(ligature), Is.EqualTo(
                new int[] { 18 }));
        });
    }

    [TestCaseSource(nameof(InvalidGsubTables))]
    public void RejectsMalformedSubstitutionStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void EnforcesLimitsAndRequiresMaxp()
    {
        byte[]? bytes = BuildGsub().Bytes;
        SfntTableCache missingMaxp = CreateCache(
            bytes,
            false,
            FontLimits.CreateCompatible());
        SfntTableCache limited = CreateCache(
            bytes,
            true,
            CreateLimits(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntGsubTableParser.FindTable(missingMaxp));
            Assert.Throws<FontLimitException>(() =>
                SfntGsubTableParser.FindTable(limited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildGsub().Bytes,
            true,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknown = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .CopyBytes();
        SfntParsedTableView? raw = SfntGsubTableParser.FindView(CreateCache(
            unknown,
            true,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            true,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetGsubTable(), Is.Not.Null);
            Assert.That(face.GetGsubTableView()!.GetParsedData(),
                Is.SameAs(face.GetGsubTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntGsubTableParser.FindView(absent), Is.Null);
            Assert.That(SfntGsubTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntGsubTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesExecutionAndBufferMutationArguments()
    {
        SfntGsubTable? table = Parse(BuildGsub().Bytes);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1 });
        SfntLayoutGlyphBuffer threeGlyphs = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntGsubExecutor.ApplyLookup(
                    null!,
                    0,
                    buffer,
                    null!,
                    SfntLayoutTraversalDirection.Forward,
                    0));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGsubExecutor.ApplyLookup(
                    table!,
                    0,
                    null!,
                    null!,
                    SfntLayoutTraversalDirection.Forward,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGsubExecutor.ApplyLookup(
                    table!,
                    4,
                    buffer,
                    null!,
                    SfntLayoutTraversalDirection.Forward,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGsubExecutor.ApplyLookup(
                    table!,
                    0,
                    buffer,
                    null!,
                    (SfntLayoutTraversalDirection)4,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGsubExecutor.ApplyLookup(
                    table!,
                    0,
                    buffer,
                    null!,
                    SfntLayoutTraversalDirection.Forward,
                    -1));
            Assert.Throws<ArgumentException>(() =>
                buffer.ReplaceWithSequence(0, Array.Empty<int>()));
            Assert.Throws<ArgumentException>(() =>
                buffer.ReplaceWithLigature(new int[] { 0, 0 }, 2));
            Assert.Throws<ArgumentException>(() =>
                threeGlyphs.ReplaceWithLigature(
                    new int[] { 0, 2, 1 },
                    4));
            Assert.Throws<ArgumentException>(() =>
                SfntLayoutGlyphBuffer.Create(
                    new int[] { 1 },
                    new int[] { 0, 1 },
                    new int[] { 1 },
                    new int[] { 1 },
                    new int[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutGlyphBuffer.Create(
                    new int[] { 1 },
                    new int[] { int.MaxValue },
                    new int[] { 1 },
                    new int[] { 1 },
                    new int[] { 0 }));
        });
    }

    static IEnumerable<TestCaseData> InvalidGsubTables()
    {
        SfntGsubFixture unsupportedSingle = BuildGsub();
        PatchUInt16(unsupportedSingle.Bytes, unsupportedSingle.SingleFormat1, 3);
        yield return InvalidCase(unsupportedSingle.Bytes, "SingleFormat");

        SfntGsubFixture singleCount = BuildGsub();
        PatchUInt16(singleCount.Bytes, singleCount.SingleFormat2 + 4, 0);
        yield return InvalidCase(singleCount.Bytes, "SingleCoverageCount");

        SfntGsubFixture singleOutput = BuildGsub();
        PatchUInt16(singleOutput.Bytes, singleOutput.SingleFormat2 + 6, 40);
        yield return InvalidCase(singleOutput.Bytes, "SingleOutputGlyph");

        SfntGsubFixture multipleOffset = BuildGsub();
        PatchUInt16(multipleOffset.Bytes, multipleOffset.Multiple + 6, 2);
        yield return InvalidCase(multipleOffset.Bytes, "MultipleSequenceOffset");

        SfntGsubFixture emptySequence = BuildGsub();
        PatchUInt16(emptySequence.Bytes, emptySequence.MultipleSequence, 0);
        yield return InvalidCase(emptySequence.Bytes, "EmptySequence");

        SfntGsubFixture alternateCount = BuildGsub();
        PatchUInt16(alternateCount.Bytes, alternateCount.Alternate + 4, 0);
        yield return InvalidCase(alternateCount.Bytes, "AlternateCoverageCount");

        SfntGsubFixture ligatureCount = BuildGsub();
        PatchUInt16(ligatureCount.Bytes,
            ligatureCount.FirstLigature + 2, 0);
        yield return InvalidCase(ligatureCount.Bytes, "LigatureComponentCount");

        SfntGsubFixture ligatureOutput = BuildGsub();
        PatchUInt16(ligatureOutput.Bytes,
            ligatureOutput.SecondLigature, 40);
        yield return InvalidCase(ligatureOutput.Bytes, "LigatureOutputGlyph");

        SfntGsubFixture coverageOffset = BuildGsub();
        PatchUInt16(coverageOffset.Bytes,
            coverageOffset.Ligature + 2, 2);
        yield return InvalidCase(coverageOffset.Bytes, "CoverageOffset");

        SfntGsubFixture lookupType = BuildGsub();
        PatchUInt16(lookupType.Bytes, lookupType.FirstLookup, 9);
        yield return InvalidCase(lookupType.Bytes, "LookupType");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static int Apply(
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        int alternateValue)
    {
        return SfntGsubExecutor.ApplyLookup(
            Parse(BuildGsub().Bytes)!,
            lookupIndex,
            buffer,
            gdef!,
            SfntLayoutTraversalDirection.Forward,
            alternateValue);
    }

    static int[] CopyGlyphIds(SfntLayoutGlyphBuffer buffer)
    {
        int[] glyphIds = new int[buffer.GetCount()];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphIds[index] = buffer.GetGlyphId(index);
            index += 1;
        }

        return glyphIds;
    }

    static SfntGdefTable CreateMarkGdef()
    {
        int[] classes = new int[21];
        classes[8] = 3;
        classes[9] = 3;
        return new SfntGdefTable(
            1,
            0,
            new SfntClassDefinitionTable(
                1,
                0,
                classes,
                new SfntClassRange[0],
                3),
            null,
            null,
            null,
            null,
            null);
    }

    static SfntGdefTable CreateSingleMarkGdef()
    {
        return new SfntGdefTable(
            1,
            0,
            new SfntClassDefinitionTable(
                1,
                0,
                new int[] { 0, 3 },
                new SfntClassRange[0],
                3),
            null,
            null,
            null,
            null,
            null);
    }

    static SfntGsubTable? Parse(byte[]? bytes)
    {
        return SfntGsubTableParser.FindTable(CreateCache(
            bytes,
            true,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? gsub,
        bool includeMaxp,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (gsub != null)
        {
            tables.Add(CreateTable(FontTags.Gsub, gsub));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(32)
                .CopyBytes()));
        }

        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
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

    static SfntGsubFixture BuildGsub()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList).WriteUInt16(0);
        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList).WriteUInt16(0);
        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(4)
            .WriteZeros(8);
        SfntGsubFixture fixture = new SfntGsubFixture();

        fixture.FirstLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 2,
            (ushort)(fixture.FirstLookup - lookupList));
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(2);
        builder.WriteUInt16(0).WriteUInt16(0);
        fixture.SingleFormat1 = builder.GetLength();
        builder.PatchUInt16(fixture.FirstLookup + 6,
            (ushort)(fixture.SingleFormat1 - fixture.FirstLookup));
        WriteSingleFormat1(builder, 1, 10);
        fixture.SingleFormat2 = builder.GetLength();
        builder.PatchUInt16(fixture.FirstLookup + 8,
            (ushort)(fixture.SingleFormat2 - fixture.FirstLookup));
        WriteSingleFormat2(builder, 2, 12);

        int multipleLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 4,
            (ushort)(multipleLookup - lookupList));
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        fixture.Multiple = builder.GetLength();
        builder.PatchUInt16(multipleLookup + 6,
            (ushort)(fixture.Multiple - multipleLookup));
        fixture.MultipleSequence = WriteMultiple(
            builder,
            3,
            new int[] { 13, 14 });

        int alternateLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 6,
            (ushort)(alternateLookup - lookupList));
        builder.WriteUInt16(3).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        fixture.Alternate = builder.GetLength();
        builder.PatchUInt16(alternateLookup + 6,
            (ushort)(fixture.Alternate - alternateLookup));
        WriteAlternate(builder, 4, new int[] { 15, 16 });

        int ligatureLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 8,
            (ushort)(ligatureLookup - lookupList));
        builder.WriteUInt16(4).WriteUInt16(0x0008)
            .WriteUInt16(1).WriteUInt16(0);
        fixture.Ligature = builder.GetLength();
        builder.PatchUInt16(ligatureLookup + 6,
            (ushort)(fixture.Ligature - ligatureLookup));
        int[] ligatureOffsets = WriteLigatureHeader(builder, 5, 2);
        fixture.FirstLigature = builder.GetLength();
        builder.PatchUInt16(ligatureOffsets[0],
            (ushort)(fixture.FirstLigature - ligatureOffsets[2]));
        builder.WriteUInt16(17).WriteUInt16(3)
            .WriteUInt16(6).WriteUInt16(7);
        fixture.SecondLigature = builder.GetLength();
        builder.PatchUInt16(ligatureOffsets[1],
            (ushort)(fixture.SecondLigature - ligatureOffsets[2]));
        builder.WriteUInt16(18).WriteUInt16(2).WriteUInt16(6);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static void WriteSingleFormat1(
        TestBinaryBuilder builder,
        int glyphId,
        int delta)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteInt16((short)delta);
        int coverage = WriteCoverage(builder, glyphId);
        builder.PatchUInt16(subtable + 2,
            (ushort)(coverage - subtable));
    }

    static void WriteSingleFormat2(
        TestBinaryBuilder builder,
        int glyphId,
        int substituteGlyphId)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(1);
        builder.WriteUInt16((ushort)substituteGlyphId);
        int coverage = WriteCoverage(builder, glyphId);
        builder.PatchUInt16(subtable + 2,
            (ushort)(coverage - subtable));
    }

    static int WriteMultiple(
        TestBinaryBuilder builder,
        int glyphId,
        int[] substitutes)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, glyphId);
        builder.PatchUInt16(subtable + 2,
            (ushort)(coverage - subtable));
        int sequence = builder.GetLength();
        builder.PatchUInt16(subtable + 6,
            (ushort)(sequence - subtable));
        builder.WriteUInt16((ushort)substitutes.Length);
        WriteGlyphIds(builder, substitutes);
        return sequence;
    }

    static void WriteAlternate(
        TestBinaryBuilder builder,
        int glyphId,
        int[] alternates)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, glyphId);
        builder.PatchUInt16(subtable + 2,
            (ushort)(coverage - subtable));
        int set = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(set - subtable));
        builder.WriteUInt16((ushort)alternates.Length);
        WriteGlyphIds(builder, alternates);
    }

    static int[] WriteLigatureHeader(
        TestBinaryBuilder builder,
        int glyphId,
        int ligatureCount)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, glyphId);
        builder.PatchUInt16(subtable + 2,
            (ushort)(coverage - subtable));
        int set = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(set - subtable));
        builder.WriteUInt16((ushort)ligatureCount);
        int[] patchOffsets = new int[ligatureCount + 1];
        int index = 0;

        while (index < ligatureCount)
        {
            patchOffsets[index] = builder.GetLength();
            builder.WriteUInt16(0);
            index += 1;
        }

        patchOffsets[ligatureCount] = set;
        return patchOffsets;
    }

    static int WriteCoverage(TestBinaryBuilder builder, int glyphId)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(1).WriteUInt16((ushort)glyphId);
        return offset;
    }

    static void WriteGlyphIds(TestBinaryBuilder builder, int[] glyphIds)
    {
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static FontLimits CreateLimits(int maximumEntries)
    {
        return FontLimits.Create(
            100000,
            100000,
            10,
            10,
            65535,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            maximumEntries,
            100,
            100000,
            10,
            100,
            10,
            10);
    }
}
