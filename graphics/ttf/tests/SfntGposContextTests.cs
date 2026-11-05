using ttf;

namespace ttf.Tests;

internal class SfntGposContextFixture
{
    internal byte[]? Bytes;
    internal int Context1;
    internal int Context1Record;
    internal int Context2InputValue;
    internal int Context3;
    internal int Context3FirstLookup;
    internal int Chained1;
    internal int Chained3InputCount;
    internal int Extension;
}

internal class SfntGposContextTests
{
    [Test]
    public void ParsesAllContextFormatsAndExtensionPositioning()
    {
        SfntGposTable? table = Parse(BuildGpos().Bytes);
        SfntGposLookup contexts = table!.GetLookup(1);
        SfntGposLookup chained = table.GetLookup(2);
        SfntGposLookup extension = table.GetLookup(3);

        Assert.Multiple(() =>
        {
            Assert.That(contexts.GetContextAdjustment(0).GetFormat(),
                Is.EqualTo(1));
            Assert.That(contexts.GetContextAdjustment(1).GetFormat(),
                Is.EqualTo(2));
            Assert.That(contexts.GetContextAdjustment(2).GetFormat(),
                Is.EqualTo(3));
            Assert.That(chained.GetChainedContextAdjustment(0).GetFormat(),
                Is.EqualTo(1));
            Assert.That(chained.GetChainedContextAdjustment(1).GetFormat(),
                Is.EqualTo(2));
            Assert.That(chained.GetChainedContextAdjustment(2).GetFormat(),
                Is.EqualTo(3));
            Assert.That(extension.GetLayoutLookup().GetLookupType(),
                Is.EqualTo(9));
            Assert.That(extension.GetLookupType(), Is.EqualTo(1));
            Assert.That(extension.GetExtension(0).GetLookupType(),
                Is.EqualTo(1));
            Assert.That(extension.GetSingleAdjustment(0)
                .FindValueRecord(19)!.GetXPlacement(), Is.EqualTo(25));
        });
    }

    [Test]
    public void AppliesContextFormatsAndOrderedNestedLookups()
    {
        SfntGposTable? table = Parse(BuildGpos().Bytes);
        SfntGposPositionBuffer format1 = SfntGposPositionBuffer.Create(
            new int[] { 1, 20, 2 });
        SfntGposPositionBuffer format2 = SfntGposPositionBuffer.Create(
            new int[] { 3, 4 });
        SfntGposPositionBuffer format3 = SfntGposPositionBuffer.Create(
            new int[] { 5, 6 });

        int count1 = Apply(table, 1, format1, CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward);
        int count2 = Apply(table, 1, format2, CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward);
        int count3 = Apply(table, 1, format3, CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(format1.GetXAdvance(0), Is.Zero);
            Assert.That(format1.GetXAdvance(1), Is.Zero);
            Assert.That(format1.GetXAdvance(2), Is.EqualTo(10));
            Assert.That(count2, Is.EqualTo(1));
            Assert.That(format2.GetXAdvance(0), Is.EqualTo(10));
            Assert.That(format2.GetXAdvance(1), Is.Zero);
            Assert.That(count3, Is.EqualTo(1));
            Assert.That(format3.GetXAdvance(0), Is.EqualTo(10));
            Assert.That(format3.GetXAdvance(1), Is.EqualTo(10));
        });
    }

    [Test]
    public void AppliesContextBackwardAcrossIgnoredMark()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 2, 20, 1 });
        int count = Apply(
            Parse(BuildGpos().Bytes),
            1,
            buffer,
            CreateMarkGdef(),
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(10));
            Assert.That(buffer.GetXAdvance(1), Is.Zero);
            Assert.That(buffer.GetXAdvance(2), Is.Zero);
        });
    }

    [Test]
    public void AppliesAllChainedContextFormats()
    {
        SfntGposTable? table = Parse(BuildGpos().Bytes);
        SfntGposPositionBuffer format1 = SfntGposPositionBuffer.Create(
            new int[] { 7, 8, 9, 10 });
        SfntGposPositionBuffer format2 = SfntGposPositionBuffer.Create(
            new int[] { 11, 12, 13, 14 });
        SfntGposPositionBuffer format3 = SfntGposPositionBuffer.Create(
            new int[] { 15, 16, 17, 18 });

        int count1 = Apply(table, 2, format1, null,
            SfntLayoutTraversalDirection.Forward);
        int count2 = Apply(table, 2, format2, null,
            SfntLayoutTraversalDirection.Forward);
        int count3 = Apply(table, 2, format3, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(format1.GetXAdvance(2), Is.EqualTo(10));
            Assert.That(count2, Is.EqualTo(1));
            Assert.That(format2.GetXAdvance(1), Is.EqualTo(10));
            Assert.That(count3, Is.EqualTo(1));
            Assert.That(format3.GetXAdvance(1), Is.EqualTo(10));
            Assert.That(format3.GetXAdvance(2), Is.EqualTo(10));
            Assert.That(format3.GetXAdvance(0), Is.Zero);
            Assert.That(format3.GetXAdvance(3), Is.Zero);
        });
    }

    [Test]
    public void AppliesExtensionLookupAsItsEffectiveType()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 19 });
        int count = Apply(
            Parse(BuildGpos().Bytes),
            3,
            buffer,
            null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(25));
            Assert.That(buffer.GetXAdvance(0), Is.Zero);
        });
    }

    [Test]
    public void StopsCyclicContextLookupsAtTheConfiguredNestingLimit()
    {
        SfntGposContextFixture fixture = BuildGpos();
        PatchUInt16(fixture.Bytes, fixture.Context3FirstLookup, 1);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 5, 6 });
        SfntLayoutExecutionContext context = CreateExecutionContext(2);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => SfntGposExecutor.ApplyLookup(
                Parse(fixture.Bytes)!,
                1,
                buffer,
                CreateMarkGdef(),
                SfntLayoutTraversalDirection.Forward,
                new SfntGposAdjustmentContext(1000, 0, 0, null),
                context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout lookup nesting exceeds the configured limit."));
            Assert.That(buffer.GetXPlacement(0), Is.Zero);
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
            Assert.That(buffer.GetXAdvance(0), Is.Zero);
            Assert.That(buffer.GetXAdvance(1), Is.Zero);
        });
    }

    [TestCaseSource(nameof(InvalidContextTables))]
    public void RejectsMalformedContextAndExtensionTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidContextTables()
    {
        SfntGposContextFixture contextFormat = BuildGpos();
        PatchUInt16(contextFormat.Bytes, contextFormat.Context1, 4);
        yield return InvalidCase(contextFormat.Bytes, "ContextFormat");

        SfntGposContextFixture lookupIndex = BuildGpos();
        PatchUInt16(lookupIndex.Bytes, lookupIndex.Context1Record + 2, 4);
        yield return InvalidCase(lookupIndex.Bytes, "NestedLookupIndex");

        SfntGposContextFixture contextClass = BuildGpos();
        PatchUInt16(contextClass.Bytes, contextClass.Context2InputValue, 3);
        yield return InvalidCase(contextClass.Bytes, "ContextClass");

        SfntGposContextFixture emptyContext = BuildGpos();
        PatchUInt16(emptyContext.Bytes, emptyContext.Context3 + 2, 0);
        yield return InvalidCase(emptyContext.Bytes, "EmptyContextInput");

        SfntGposContextFixture coverageOffset = BuildGpos();
        PatchUInt16(coverageOffset.Bytes,
            coverageOffset.Context3 + 6, 2);
        yield return InvalidCase(coverageOffset.Bytes,
            "ContextCoverageIntoHeader");

        SfntGposContextFixture chainedFormat = BuildGpos();
        PatchUInt16(chainedFormat.Bytes, chainedFormat.Chained1, 4);
        yield return InvalidCase(chainedFormat.Bytes, "ChainedFormat");

        SfntGposContextFixture emptyChained = BuildGpos();
        PatchUInt16(
            emptyChained.Bytes,
            emptyChained.Chained3InputCount,
            0);
        yield return InvalidCase(emptyChained.Bytes, "EmptyChainedInput");

        SfntGposContextFixture extensionFormat = BuildGpos();
        PatchUInt16(extensionFormat.Bytes, extensionFormat.Extension, 2);
        yield return InvalidCase(extensionFormat.Bytes, "ExtensionFormat");

        SfntGposContextFixture extensionType = BuildGpos();
        PatchUInt16(extensionType.Bytes, extensionType.Extension + 2, 9);
        yield return InvalidCase(extensionType.Bytes, "ExtensionType");

        SfntGposContextFixture extensionOffset = BuildGpos();
        PatchUInt32(extensionOffset.Bytes, extensionOffset.Extension + 4, 4);
        yield return InvalidCase(extensionOffset.Bytes, "ExtensionOffset");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("RejectsGpos" + name);
    }

    static int Apply(
        SfntGposTable? table,
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        return SfntGposExecutor.ApplyLookup(
            table!,
            lookupIndex,
            buffer,
            gdef!,
            direction,
            new SfntGposAdjustmentContext(1000, 0, 0, null));
    }

    static SfntGdefTable CreateMarkGdef()
    {
        int[] classes = new int[32];
        classes[20] = 3;
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
            0,
            100,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);
    }

    static SfntGposTable? Parse(byte[]? bytes)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Gpos, bytes));
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(32)
            .CopyBytes()));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return SfntGposTableParser.FindTable(cache);
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

    static SfntGposContextFixture BuildGpos()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0);
        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList).WriteUInt16(0);
        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList).WriteUInt16(0);
        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(4).WriteZeros(8);
        SfntGposContextFixture fixture = new SfntGposContextFixture();

        int singleLookup = WriteLookup(
            builder, lookupList, 0, 1, 0, 1);
        int single = builder.GetLength();
        PatchLookupSubtable(builder, singleLookup, 0, single);
        WriteSingleAdjustment(builder);

        int contextLookup = WriteLookup(
            builder, lookupList, 1, 7, 0x0008, 3);
        fixture.Context1 = builder.GetLength();
        PatchLookupSubtable(builder, contextLookup, 0, fixture.Context1);
        fixture.Context1Record = WriteContextFormat1(builder);
        int context2 = builder.GetLength();
        PatchLookupSubtable(builder, contextLookup, 1, context2);
        fixture.Context2InputValue = WriteContextFormat2(builder);
        fixture.Context3 = builder.GetLength();
        PatchLookupSubtable(builder, contextLookup, 2, fixture.Context3);
        fixture.Context3FirstLookup = WriteContextFormat3(builder);

        int chainedLookup = WriteLookup(
            builder, lookupList, 2, 8, 0, 3);
        fixture.Chained1 = builder.GetLength();
        PatchLookupSubtable(builder, chainedLookup, 0, fixture.Chained1);
        WriteChainedFormat1(builder);
        int chained2 = builder.GetLength();
        PatchLookupSubtable(builder, chainedLookup, 1, chained2);
        WriteChainedFormat2(builder);
        int chained3 = builder.GetLength();
        PatchLookupSubtable(builder, chainedLookup, 2, chained3);
        fixture.Chained3InputCount = WriteChainedFormat3(builder);

        int extensionLookup = WriteLookup(
            builder, lookupList, 3, 9, 0, 1);
        fixture.Extension = builder.GetLength();
        PatchLookupSubtable(
            builder,
            extensionLookup,
            0,
            fixture.Extension);
        WriteExtensionSingle(builder);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static int WriteLookup(
        TestBinaryBuilder builder,
        int lookupList,
        int lookupIndex,
        int lookupType,
        int lookupFlags,
        int subtableCount)
    {
        int lookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 2 + lookupIndex * 2,
            (ushort)(lookup - lookupList));
        builder.WriteUInt16((ushort)lookupType)
            .WriteUInt16((ushort)lookupFlags)
            .WriteUInt16((ushort)subtableCount)
            .WriteZeros(subtableCount * 2);
        return lookup;
    }

    static void PatchLookupSubtable(
        TestBinaryBuilder builder,
        int lookup,
        int subtableIndex,
        int subtable)
    {
        builder.PatchUInt16(
            lookup + 6 + subtableIndex * 2,
            (ushort)(subtable - lookup));
    }

    static void WriteSingleAdjustment(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0x0004).WriteInt16(10);
        int coverage = WriteCoverage(
            builder,
            new int[] { 2, 3, 5, 6, 9, 12, 16, 17 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));
    }

    static int WriteContextFormat1(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 1 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(
            subtable + 6,
            (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(4);
        builder.WriteUInt16(2).WriteUInt16(1).WriteUInt16(2);
        int record = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0);
        return record;
    }

    static int WriteContextFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(3).WriteZeros(6);
        int coverage = WriteCoverage(builder, new int[] { 3 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));
        int classDefinition = WriteClassDefinition(
            builder,
            3,
            new int[] { 1, 2 });
        builder.PatchUInt16(
            subtable + 4,
            (ushort)(classDefinition - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(
            subtable + 10,
            (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(4);
        builder.WriteUInt16(2).WriteUInt16(1);
        int inputValue = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);
        return inputValue;
    }

    static int WriteContextFormat3(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(3).WriteUInt16(2).WriteUInt16(2)
            .WriteZeros(4);
        builder.WriteUInt16(0).WriteUInt16(0);
        int firstLookup = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0);
        int firstCoverage = WriteCoverage(builder, new int[] { 5 });
        int secondCoverage = WriteCoverage(builder, new int[] { 6 });
        builder.PatchUInt16(
            subtable + 6,
            (ushort)(firstCoverage - subtable));
        builder.PatchUInt16(
            subtable + 8,
            (ushort)(secondCoverage - subtable));
        return firstLookup - 2;
    }

    static void WriteChainedFormat1(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 8 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(
            subtable + 6,
            (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(4);
        builder.WriteUInt16(1).WriteUInt16(7)
            .WriteUInt16(2).WriteUInt16(9)
            .WriteUInt16(1).WriteUInt16(10)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(0);
    }

    static void WriteChainedFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteZeros(6)
            .WriteUInt16(2).WriteZeros(4);
        int coverage = WriteCoverage(builder, new int[] { 12 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));
        int backtrackClass = WriteClassDefinition(
            builder, 11, new int[] { 1 });
        int inputClass = WriteClassDefinition(
            builder, 12, new int[] { 1, 2 });
        int lookaheadClass = WriteClassDefinition(
            builder, 14, new int[] { 1 });
        builder.PatchUInt16(
            subtable + 4,
            (ushort)(backtrackClass - subtable));
        builder.PatchUInt16(
            subtable + 6,
            (ushort)(inputClass - subtable));
        builder.PatchUInt16(
            subtable + 8,
            (ushort)(lookaheadClass - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(
            subtable + 14,
            (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(4);
        builder.WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(2).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0);
    }

    static int WriteChainedFormat3(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(3)
            .WriteUInt16(1).WriteUInt16(0);
        int inputCountOffset = builder.GetLength();
        builder.WriteUInt16(2).WriteZeros(4)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0);
        int backtrack = WriteCoverage(builder, new int[] { 15 });
        int input1 = WriteCoverage(builder, new int[] { 16 });
        int input2 = WriteCoverage(builder, new int[] { 17 });
        int lookahead = WriteCoverage(builder, new int[] { 18 });
        builder.PatchUInt16(
            subtable + 4,
            (ushort)(backtrack - subtable));
        builder.PatchUInt16(
            subtable + 8,
            (ushort)(input1 - subtable));
        builder.PatchUInt16(
            subtable + 10,
            (ushort)(input2 - subtable));
        builder.PatchUInt16(
            subtable + 14,
            (ushort)(lookahead - subtable));
        return inputCountOffset;
    }

    static void WriteExtensionSingle(TestBinaryBuilder builder)
    {
        int extension = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(1).WriteUInt32(8);
        int single = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0x0001).WriteInt16(25);
        int coverage = WriteCoverage(builder, new int[] { 19 });
        builder.PatchUInt16(
            single + 2,
            (ushort)(coverage - single));
        Assert.That(single, Is.EqualTo(extension + 8));
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
    {
        int coverage = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);

        foreach (int glyphId in glyphIds)
        {
            builder.WriteUInt16((ushort)glyphId);
        }

        return coverage;
    }

    static int WriteClassDefinition(
        TestBinaryBuilder builder,
        int startGlyphId,
        int[] classValues)
    {
        int classDefinition = builder.GetLength();
        builder.WriteUInt16(1)
            .WriteUInt16((ushort)startGlyphId)
            .WriteUInt16((ushort)classValues.Length);

        foreach (int classValue in classValues)
        {
            builder.WriteUInt16((ushort)classValue);
        }

        return classDefinition;
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
