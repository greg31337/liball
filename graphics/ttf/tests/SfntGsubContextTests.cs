using ttf;

namespace ttf.Tests;

internal class SfntGsubContextFixture
{
    internal byte[]? Bytes;
    internal int Format1Lookup;
    internal int Format1Subtable;
    internal int Format1Rule;
    internal int Format2Subtable;
    internal int Format3Subtable;
    internal int Format3Lookup;
}

internal class SfntGsubContextTests
{
    [Test]
    public void ParsesContextualFormatsAndStoredLookupRecords()
    {
        SfntGsubTable? table = Parse(BuildGsub().Bytes);
        SfntGsubContextSubstitution format1 = table!.GetLookup(3)
            .GetContextSubstitution(0);
        SfntGsubContextSubstitution format2 = table.GetLookup(4)
            .GetContextSubstitution(0);
        SfntGsubContextSubstitution format3 = table.GetLookup(5)
            .GetContextSubstitution(0);
        SfntGsubContextRule rule1 = format1.GetRuleSet(0).GetRule(0);
        SfntSequenceLookupRecords records1 = rule1.GetLookupRecords();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetLookupCount(), Is.EqualTo(7));
            Assert.That(format1.GetFormat(), Is.EqualTo(1));
            Assert.That(format1.GetCoverage()!.GetGlyphId(0), Is.EqualTo(1));
            Assert.That(rule1.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(rule1.GetInputValue(0), Is.EqualTo(2));
            Assert.That(records1.GetRecordCount(), Is.EqualTo(2));
            Assert.That(records1.GetRecord(0).GetSequenceIndex(), Is.Zero);
            Assert.That(records1.GetRecord(0).GetLookupListIndex(),
                Is.Zero);
            Assert.That(records1.GetRecord(1).GetSequenceIndex(),
                Is.EqualTo(1));
            Assert.That(records1.GetRecord(1).GetLookupListIndex(),
                Is.EqualTo(1));
            Assert.That(format2.GetFormat(), Is.EqualTo(2));
            Assert.That(format2.GetClassDefinition()!.GetClassForGlyph(3),
                Is.EqualTo(1));
            Assert.That(format2.GetRuleSet(1).GetRule(0).GetInputValue(0),
                Is.EqualTo(2));
            Assert.That(format3.GetFormat(), Is.EqualTo(3));
            Assert.That(format3.GetInputCoverageCount(), Is.EqualTo(2));
            Assert.That(format3.GetInputCoverage(1).Covers(6), Is.True);
        });
    }

    [Test]
    public void AppliesNestedLookupsInOrderAfterBufferExpansion()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 2 });
        int count = Apply(3, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 10, 11, 12 }));
        });
    }

    [Test]
    public void MatchesTheWholeContextBeforeApplyingNestedLookups()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 3 });
        int count = Apply(3, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.Zero);
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 1, 3 }));
        });
    }

    [Test]
    public void AppliesClassBasedContextualSubstitution()
    {
        SfntLayoutGlyphBuffer matching = SfntLayoutGlyphBuffer.Create(
            new int[] { 3, 4 });
        SfntLayoutGlyphBuffer outsideCoverage = SfntLayoutGlyphBuffer.Create(
            new int[] { 8, 4 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(4, matching, null,
                SfntLayoutTraversalDirection.Forward), Is.EqualTo(1));
            Assert.That(CopyGlyphIds(matching), Is.EqualTo(
                new int[] { 3, 14 }));
            Assert.That(Apply(4, outsideCoverage, null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
            Assert.That(CopyGlyphIds(outsideCoverage), Is.EqualTo(
                new int[] { 8, 4 }));
        });
    }

    [Test]
    public void AppliesCoverageBasedContextualSubstitution()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5, 6 });
        int count = Apply(5, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 15, 6 }));
        });
    }

    [Test]
    public void UsesContextLookupFlagsWhileMatchingInput()
    {
        SfntGsubContextFixture fixture = BuildGsub();
        PatchUInt16(fixture.Bytes, fixture.Format1Lookup + 2, 0x0008);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 7, 2 });
        int count = SfntGsubExecutor.ApplyLookup(
            Parse(fixture.Bytes)!,
            3,
            buffer,
            CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 10, 11, 7, 12 }));
        });
    }

    [Test]
    public void RemapsContextIndicesDuringBackwardTraversal()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 2, 1 });
        int count = Apply(3, buffer, null,
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 12, 10, 11 }));
        });
    }

    [Test]
    public void RemapsLaterContextActionsAfterLigatureContraction()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 20, 21, 2 });
        int count = Apply(6, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 30, 12 }));
        });
    }

    [Test]
    public void StopsCyclicContextLookupsAtTheConfiguredNestingLimit()
    {
        SfntGsubContextFixture fixture = BuildGsub();
        PatchUInt16(fixture.Bytes, fixture.Format3Lookup, 5);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5, 6 });
        SfntLayoutExecutionContext context = CreateExecutionContext(2);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => SfntGsubExecutor.ApplyLookup(
                Parse(fixture.Bytes)!,
                5,
                buffer,
                null!,
                SfntLayoutTraversalDirection.Forward,
                0,
                context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout lookup nesting exceeds the configured limit."));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 5, 6 }));
        });
    }

    [TestCaseSource(nameof(InvalidContextTables))]
    public void RejectsMalformedContextualStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidContextTables()
    {
        SfntGsubContextFixture format = BuildGsub();
        PatchUInt16(format.Bytes, format.Format1Subtable, 4);
        yield return InvalidCase(format.Bytes, "UnsupportedContextFormat");

        SfntGsubContextFixture emptyRule = BuildGsub();
        PatchUInt16(emptyRule.Bytes, emptyRule.Format1Rule, 0);
        yield return InvalidCase(emptyRule.Bytes, "EmptyContextRule");

        SfntGsubContextFixture sequenceIndex = BuildGsub();
        PatchUInt16(sequenceIndex.Bytes,
            sequenceIndex.Format1Rule + 6, 2);
        yield return InvalidCase(sequenceIndex.Bytes,
            "ContextSequenceIndex");

        SfntGsubContextFixture lookupIndex = BuildGsub();
        PatchUInt16(lookupIndex.Bytes,
            lookupIndex.Format1Rule + 8, 7);
        yield return InvalidCase(lookupIndex.Bytes,
            "ContextLookupIndex");

        SfntGsubContextFixture emptyFormat3 = BuildGsub();
        PatchUInt16(emptyFormat3.Bytes,
            emptyFormat3.Format3Subtable + 2, 0);
        yield return InvalidCase(emptyFormat3.Bytes, "EmptyFormat3Input");

        SfntGsubContextFixture coverageOffset = BuildGsub();
        PatchUInt16(coverageOffset.Bytes,
            coverageOffset.Format3Subtable + 6, 2);
        yield return InvalidCase(coverageOffset.Bytes,
            "ContextCoverageIntoHeader");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static int Apply(
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        return SfntGsubExecutor.ApplyLookup(
            Parse(BuildGsub().Bytes)!,
            lookupIndex,
            buffer,
            gdef!,
            direction,
            0);
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
        int[] classes = new int[16];
        classes[7] = 3;
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

    static SfntGsubTable? Parse(byte[]? bytes)
    {
        return SfntGsubTableParser.FindTable(CreateCache(bytes));
    }

    static SfntTableCache CreateCache(byte[]? bytes)
    {
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Gsub, bytes),
            CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(32)
                .CopyBytes())
        };
        return SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
    }

    static SfntTableData CreateTable(int tag, byte[]? bytes)
    {
        FontData data = FontData.Create(bytes!);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static SfntGsubContextFixture BuildGsub()
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
            .WriteUInt16(7)
            .WriteZeros(14);
        SfntGsubContextFixture fixture = new SfntGsubContextFixture();

        int multipleLookup = WriteLookupHeader(builder, lookupList, 0, 2, 0);
        int multiple = builder.GetLength();
        PatchLookupSubtable(builder, multipleLookup, multiple);
        WriteMultiple(builder, 1, new int[] { 10, 11 });

        int singleLookup = WriteLookupHeader(builder, lookupList, 1, 1, 0);
        int single = builder.GetLength();
        PatchLookupSubtable(builder, singleLookup, single);
        WriteSingleFormat2(
            builder,
            new int[] { 2, 4, 5 },
            new int[] { 12, 14, 15 });

        int ligatureLookup = WriteLookupHeader(builder, lookupList, 2, 4, 0);
        int ligature = builder.GetLength();
        PatchLookupSubtable(builder, ligatureLookup, ligature);
        WriteLigature(builder, 20, 21, 30);

        fixture.Format1Lookup = WriteLookupHeader(
            builder,
            lookupList,
            3,
            5,
            0);
        fixture.Format1Subtable = builder.GetLength();
        PatchLookupSubtable(
            builder,
            fixture.Format1Lookup,
            fixture.Format1Subtable);
        fixture.Format1Rule = WriteContextFormat1(builder);

        int format2Lookup = WriteLookupHeader(builder, lookupList, 4, 5, 0);
        fixture.Format2Subtable = builder.GetLength();
        PatchLookupSubtable(builder, format2Lookup, fixture.Format2Subtable);
        WriteContextFormat2(builder);

        int format3Lookup = WriteLookupHeader(builder, lookupList, 5, 5, 0);
        fixture.Format3Subtable = builder.GetLength();
        PatchLookupSubtable(builder, format3Lookup, fixture.Format3Subtable);
        fixture.Format3Lookup = WriteContextFormat3(builder);

        int contractionLookup = WriteLookupHeader(
            builder,
            lookupList,
            6,
            5,
            0);
        int contraction = builder.GetLength();
        PatchLookupSubtable(builder, contractionLookup, contraction);
        WriteContractionContext(builder);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static int WriteLookupHeader(
        TestBinaryBuilder builder,
        int lookupList,
        int lookupIndex,
        int lookupType,
        int lookupFlags)
    {
        int lookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 2 + lookupIndex * 2,
            (ushort)(lookup - lookupList));
        builder.WriteUInt16((ushort)lookupType)
            .WriteUInt16((ushort)lookupFlags)
            .WriteUInt16(1)
            .WriteUInt16(0);
        return lookup;
    }

    static void PatchLookupSubtable(
        TestBinaryBuilder builder,
        int lookup,
        int subtable)
    {
        builder.PatchUInt16(lookup + 6, (ushort)(subtable - lookup));
    }

    static void WriteMultiple(
        TestBinaryBuilder builder,
        int glyphId,
        int[] substitutes)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { glyphId });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int sequence = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(sequence - subtable));
        builder.WriteUInt16((ushort)substitutes.Length);
        WriteUInt16Values(builder, substitutes);
    }

    static void WriteSingleFormat2(
        TestBinaryBuilder builder,
        int[] glyphIds,
        int[] substitutes)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0)
            .WriteUInt16((ushort)substitutes.Length);
        WriteUInt16Values(builder, substitutes);
        int coverage = WriteCoverage(builder, glyphIds);
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
    }

    static void WriteLigature(
        TestBinaryBuilder builder,
        int firstGlyphId,
        int secondGlyphId,
        int ligatureGlyphId)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { firstGlyphId });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int set = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(set - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int ligature = builder.GetLength();
        builder.PatchUInt16(set + 2, (ushort)(ligature - set));
        builder.WriteUInt16((ushort)ligatureGlyphId)
            .WriteUInt16(2)
            .WriteUInt16((ushort)secondGlyphId);
    }

    static int WriteContextFormat1(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 1 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int rule = builder.GetLength();
        builder.PatchUInt16(ruleSet + 2, (ushort)(rule - ruleSet));
        builder.WriteUInt16(2).WriteUInt16(2).WriteUInt16(2);
        WriteLookupRecord(builder, 0, 0);
        WriteLookupRecord(builder, 1, 1);
        return rule;
    }

    static void WriteContextFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(3).WriteUInt16(0).WriteUInt16(0).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 3 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int classDefinition = builder.GetLength();
        builder.PatchUInt16(subtable + 4,
            (ushort)(classDefinition - subtable));
        builder.WriteUInt16(1).WriteUInt16(3).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(2);
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(subtable + 10, (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int rule = builder.GetLength();
        builder.PatchUInt16(ruleSet + 2, (ushort)(rule - ruleSet));
        builder.WriteUInt16(2).WriteUInt16(1).WriteUInt16(2);
        WriteLookupRecord(builder, 1, 1);
    }

    static int WriteContextFormat3(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(3).WriteUInt16(2).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt16(0);
        int lookupRecord = builder.GetLength();
        WriteLookupRecord(builder, 0, 1);
        int firstCoverage = WriteCoverage(builder, new int[] { 5 });
        int secondCoverage = WriteCoverage(builder, new int[] { 6 });
        builder.PatchUInt16(subtable + 6,
            (ushort)(firstCoverage - subtable));
        builder.PatchUInt16(subtable + 8,
            (ushort)(secondCoverage - subtable));
        return lookupRecord + 2;
    }

    static SfntLayoutExecutionContext CreateExecutionContext(
        int maximumNesting)
    {
        FontLimits limits = FontLimits.Create(
            1000000,
            1000000,
            1,
            10,
            65535,
            1000,
            100,
            10,
            10,
            maximumNesting,
            10,
            1000000,
            1000,
            1000,
            1000000,
            100,
            100,
            10,
            10);
        return new SfntLayoutExecutionContext(
            limits,
            100,
            100,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);
    }

    static void WriteContractionContext(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 20 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int rule = builder.GetLength();
        builder.PatchUInt16(ruleSet + 2, (ushort)(rule - ruleSet));
        builder.WriteUInt16(3).WriteUInt16(2)
            .WriteUInt16(21).WriteUInt16(2);
        WriteLookupRecord(builder, 0, 2);
        WriteLookupRecord(builder, 2, 1);
    }

    static void WriteLookupRecord(
        TestBinaryBuilder builder,
        int sequenceIndex,
        int lookupIndex)
    {
        builder.WriteUInt16((ushort)sequenceIndex)
            .WriteUInt16((ushort)lookupIndex);
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
    {
        int coverage = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);
        WriteUInt16Values(builder, glyphIds);
        return coverage;
    }

    static void WriteUInt16Values(
        TestBinaryBuilder builder,
        int[] values)
    {
        int index = 0;

        while (index < values.Length)
        {
            builder.WriteUInt16((ushort)values[index]);
            index += 1;
        }
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
