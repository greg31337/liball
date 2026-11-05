using ttf;

namespace ttf.Tests;

internal class SfntGposFixture
{
    internal byte[]? Bytes;
    internal int FirstLookup;
    internal int SingleFormat1;
    internal int SingleFormat2;
    internal int PairFormat1;
    internal int PairSet;
    internal int PairFormat2;
    internal int EmptySecondPair;
}

internal class SfntGposExecutorTests
{
    [Test]
    public void ParsesSingleAndPairPositioningFormats()
    {
        SfntGposTable? table = Parse(BuildGpos().Bytes);
        SfntGposLookup singleLookup = table!.GetLookup(0);
        SfntGposSingleAdjustment single1 =
            singleLookup.GetSingleAdjustment(0);
        SfntGposSingleAdjustment single2 =
            singleLookup.GetSingleAdjustment(1);
        SfntGposPairAdjustment pair1 = table.GetLookup(1)
            .GetPairAdjustment(0);
        SfntGposPairAdjustment pair2 = table.GetLookup(2)
            .GetPairAdjustment(0);
        SfntGposPairValueRecord explicitPair = pair1.GetPairSet(0)
            .GetPairValue(0);
        SfntGposClassPairRecord classPair = pair2.GetClassPairRecord(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetLayoutTable().GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetLookupCount(), Is.EqualTo(4));
            Assert.That(singleLookup.GetSubtableCount(), Is.EqualTo(2));
            Assert.That(single1.GetFormat(), Is.EqualTo(1));
            Assert.That(single1.GetValueFormat(), Is.EqualTo(0x000F));
            Assert.That(single1.FindValueRecord(1)!.GetYAdvance(),
                Is.EqualTo(40));
            Assert.That(single2.GetFormat(), Is.EqualTo(2));
            Assert.That(single2.GetValueRecordCount(), Is.EqualTo(2));
            Assert.That(single2.FindValueRecord(3)!.GetXPlacement(),
                Is.EqualTo(-6));
            Assert.That(pair1.GetFormat(), Is.EqualTo(1));
            Assert.That(pair1.GetPairSetCount(), Is.EqualTo(1));
            Assert.That(explicitPair.GetSecondGlyphId(), Is.EqualTo(5));
            Assert.That(explicitPair.GetFirstValueRecord().GetXAdvance(),
                Is.EqualTo(-20));
            Assert.That(explicitPair.GetSecondValueRecord().GetYAdvance(),
                Is.EqualTo(-30));
            Assert.That(pair2.GetFormat(), Is.EqualTo(2));
            Assert.That(pair2.GetFirstClassCount(), Is.EqualTo(2));
            Assert.That(pair2.GetSecondClassCount(), Is.EqualTo(3));
            Assert.That(classPair.GetFirstValueRecord().GetXAdvance(),
                Is.EqualTo(-40));
            Assert.That(classPair.GetSecondValueRecord().GetYPlacement(),
                Is.EqualTo(15));
        });
    }

    [Test]
    public void AppliesBothSingleFormatsAndAllPositionAxes()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2, 3, 1 });
        int count = Apply(0, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(4));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(10));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(20));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(30));
            Assert.That(buffer.GetYAdvance(0), Is.EqualTo(40));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(-5));
            Assert.That(buffer.GetYAdvance(1), Is.EqualTo(50));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(-6));
            Assert.That(buffer.GetYAdvance(2), Is.EqualTo(60));
            Assert.That(buffer.GetXAdvance(3), Is.EqualTo(30));
        });
    }

    [Test]
    public void AccumulatesPositionAdjustmentsAcrossApplications()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1 });

        Apply(0, buffer, null, SfntLayoutTraversalDirection.Forward);
        Apply(0, buffer, null, SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(20));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(40));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(60));
            Assert.That(buffer.GetYAdvance(0), Is.EqualTo(80));
        });
    }

    [Test]
    public void AppliesExplicitPairAcrossIgnoredMark()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 4, 12, 5 });
        int count = Apply(1, buffer, CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(-20));
            Assert.That(buffer.GetXAdvance(1), Is.Zero);
            Assert.That(buffer.GetYAdvance(2), Is.EqualTo(-30));
        });
    }

    [Test]
    public void AppliesExplicitPairBackward()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 5, 4 });
        int count = Apply(1, buffer, CreateMarkGdef(),
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetYAdvance(0), Is.EqualTo(-30));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(-20));
        });
    }

    [Test]
    public void AppliesClassPairMatrixIncludingVerticalPlacement()
    {
        SfntGposPositionBuffer matching = SfntGposPositionBuffer.Create(
            new int[] { 6, 8 });
        SfntGposPositionBuffer classZeroValues =
            SfntGposPositionBuffer.Create(new int[] { 6, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(2, matching, null,
                SfntLayoutTraversalDirection.Forward), Is.EqualTo(1));
            Assert.That(matching.GetXAdvance(0), Is.EqualTo(-40));
            Assert.That(matching.GetYPlacement(1), Is.EqualTo(15));
            Assert.That(Apply(2, classZeroValues, null,
                SfntLayoutTraversalDirection.Forward), Is.EqualTo(1));
            Assert.That(classZeroValues.GetXAdvance(0), Is.Zero);
            Assert.That(classZeroValues.GetYPlacement(1), Is.Zero);
        });
    }

    [Test]
    public void ProcessesSecondGlyphAgainWhenItsValueFormatIsEmpty()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 9, 10, 11 });
        int count = Apply(3, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(-5));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(-7));
            Assert.That(buffer.GetXAdvance(2), Is.Zero);
        });
    }

    [Test]
    public void IntegratesGposTableWithFontFace()
    {
        SfntTableCache cache = CreateCache(
            BuildGpos().Bytes,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetGposTable(), Is.Not.Null);
            Assert.That(face.GetGposTableView()!.GetParsedData(),
                Is.SameAs(face.GetGposTable()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposTableParser.FindView(null!));
        });
    }

    [Test]
    public void KeepsUnknownVersionsRawAndRequiresMaxp()
    {
        byte[] unknownBytes = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .CopyBytes();
        SfntParsedTableView? unknown = SfntGposTableParser.FindView(
            CreateCache(
                unknownBytes,
                true,
                FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            true,
            FontLimits.CreateCompatible());
        SfntTableCache missingMaxp = CreateCache(
            BuildGpos().Bytes,
            false,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(unknown!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(unknown.HasParsedData(), Is.False);
            Assert.That(SfntGposTableParser.FindView(absent), Is.Null);
            Assert.That(SfntGposTableParser.FindTable(absent), Is.Null);
            Assert.Throws<FontFormatException>(() =>
                SfntGposTableParser.FindTable(missingMaxp));
        });
    }

    [Test]
    public void RejectsVariationValueWithoutGdefItemVariationStore()
    {
        Assert.Throws<FontFormatException>(() => Parse(
            BuildVariationGpos()));
    }

    [Test]
    public void ValidatesExecutorAndPositionBufferArguments()
    {
        SfntGposTable? table = Parse(BuildGpos().Bytes);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1 });
        SfntGposAdjustmentContext context = CreateAdjustmentContext();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposExecutor.ApplyLookup(
                    null!, 0, buffer, null!,
                    SfntLayoutTraversalDirection.Forward, context));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposExecutor.ApplyLookup(
                    table!, 0, null!, null!,
                    SfntLayoutTraversalDirection.Forward, context));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGposExecutor.ApplyLookup(
                    table!, 4, buffer, null!,
                    SfntLayoutTraversalDirection.Forward, context));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGposExecutor.ApplyLookup(
                    table!, 0, buffer, null!,
                    (SfntLayoutTraversalDirection)4, context));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposExecutor.ApplyLookup(
                    table!, 0, buffer, null!,
                    SfntLayoutTraversalDirection.Forward, null!));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposPositionBuffer.Create(
                    (SfntLayoutGlyphBuffer?)null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.GetXPlacement(1));
            Assert.Throws<ArgumentNullException>(() =>
                buffer.ApplyAdjustment(0, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetLookup(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetLookup(2).GetPairAdjustment(0)
                    .GetClassPairRecord(2, 0));
        });
    }

    [TestCaseSource(nameof(InvalidGposTables))]
    public void RejectsMalformedPositioningStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidGposTables()
    {
        SfntGposFixture singleFormat = BuildGpos();
        PatchUInt16(singleFormat.Bytes, singleFormat.SingleFormat1, 3);
        yield return InvalidCase(singleFormat.Bytes, "SingleFormat");

        SfntGposFixture valueFormat = BuildGpos();
        PatchUInt16(valueFormat.Bytes, valueFormat.SingleFormat1 + 4,
            0x0100);
        yield return InvalidCase(valueFormat.Bytes, "ReservedValueFormat");

        SfntGposFixture singleCount = BuildGpos();
        PatchUInt16(singleCount.Bytes, singleCount.SingleFormat2 + 6, 1);
        yield return InvalidCase(singleCount.Bytes, "SingleCoverageCount");

        SfntGposFixture pairFormat = BuildGpos();
        PatchUInt16(pairFormat.Bytes, pairFormat.PairFormat1, 3);
        yield return InvalidCase(pairFormat.Bytes, "PairFormat");

        SfntGposFixture pairSetOffset = BuildGpos();
        PatchUInt16(pairSetOffset.Bytes, pairSetOffset.PairFormat1 + 10, 2);
        yield return InvalidCase(pairSetOffset.Bytes, "PairSetOffset");

        SfntGposFixture secondGlyph = BuildGpos();
        PatchUInt16(secondGlyph.Bytes, secondGlyph.PairSet + 2, 16);
        yield return InvalidCase(secondGlyph.Bytes, "PairSecondGlyph");

        SfntGposFixture classBounds = BuildGpos();
        PatchUInt16(classBounds.Bytes, classBounds.PairFormat2 + 14, 2);
        yield return InvalidCase(classBounds.Bytes, "PairClassBounds");

        SfntGposFixture lookupType = BuildGpos();
        PatchUInt16(lookupType.Bytes, lookupType.FirstLookup, 10);
        yield return InvalidCase(lookupType.Bytes, "LookupType");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static int Apply(
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        return SfntGposExecutor.ApplyLookup(
            Parse(BuildGpos().Bytes)!,
            lookupIndex,
            buffer,
            gdef!,
            direction,
            CreateAdjustmentContext());
    }

    static SfntGposAdjustmentContext CreateAdjustmentContext()
    {
        return new SfntGposAdjustmentContext(1000, 0, 0, null);
    }

    static SfntGdefTable CreateMarkGdef()
    {
        int[] classes = new int[16];
        classes[12] = 3;
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

    static SfntGposTable? Parse(byte[]? bytes)
    {
        return SfntGposTableParser.FindTable(CreateCache(
            bytes,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(byte[]? gpos, FontLimits limits)
    {
        return CreateCache(gpos, true, limits);
    }

    static SfntTableCache CreateCache(
        byte[]? gpos,
        bool includeMaxp,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (gpos != null)
        {
            tables.Add(CreateTable(FontTags.Gpos, gpos));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(16)
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

    static SfntGposFixture BuildGpos()
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
        SfntGposFixture fixture = new SfntGposFixture();

        fixture.FirstLookup = WriteLookupHeader(
            builder, lookupList, 0, 1, 0, 2);
        fixture.SingleFormat1 = builder.GetLength();
        PatchLookupSubtable(builder, fixture.FirstLookup, 0,
            fixture.SingleFormat1);
        WriteSingleFormat1(builder);
        fixture.SingleFormat2 = builder.GetLength();
        PatchLookupSubtable(builder, fixture.FirstLookup, 1,
            fixture.SingleFormat2);
        WriteSingleFormat2(builder);

        int pairLookup = WriteLookupHeader(
            builder, lookupList, 1, 2, 0x0008, 1);
        fixture.PairFormat1 = builder.GetLength();
        PatchLookupSubtable(builder, pairLookup, 0, fixture.PairFormat1);
        fixture.PairSet = WritePairFormat1(
            builder,
            new int[] { 4 },
            new int[][] { new int[] { 5, -20, -30 } },
            0x0004,
            0x0008);

        int classPairLookup = WriteLookupHeader(
            builder, lookupList, 2, 2, 0, 1);
        fixture.PairFormat2 = builder.GetLength();
        PatchLookupSubtable(builder, classPairLookup, 0,
            fixture.PairFormat2);
        WritePairFormat2(builder);

        int emptySecondLookup = WriteLookupHeader(
            builder, lookupList, 3, 2, 0, 1);
        fixture.EmptySecondPair = builder.GetLength();
        PatchLookupSubtable(builder, emptySecondLookup, 0,
            fixture.EmptySecondPair);
        WritePairFormat1(
            builder,
            new int[] { 9, 10 },
            new int[][]
            {
                new int[] { 10, -5 },
                new int[] { 11, -7 }
            },
            0x0004,
            0);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static byte[] BuildVariationGpos()
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
            .WriteUInt16(1).WriteUInt16(0);
        int lookup = WriteLookupHeader(builder, lookupList, 0, 1, 0, 1);
        int subtable = builder.GetLength();
        PatchLookupSubtable(builder, lookup, 0, subtable);
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0x0010).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 1 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int variation = builder.GetLength();
        builder.PatchUInt16(subtable + 6,
            (ushort)(variation - subtable));
        builder.WriteUInt16(0).WriteUInt16(0).WriteUInt16(0x8000);
        return builder.CopyBytes();
    }

    static int WriteLookupHeader(
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

    static void WriteSingleFormat1(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(0x000F)
            .WriteInt16(10).WriteInt16(20)
            .WriteInt16(30).WriteInt16(40);
        int coverage = WriteCoverage(builder, new int[] { 1 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
    }

    static void WriteSingleFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(0x0009)
            .WriteUInt16(2)
            .WriteInt16(-5).WriteInt16(50)
            .WriteInt16(-6).WriteInt16(60);
        int coverage = WriteCoverage(builder, new int[] { 2, 3 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
    }

    static int WritePairFormat1(
        TestBinaryBuilder builder,
        int[] firstGlyphIds,
        int[][] pairValues,
        int firstValueFormat,
        int secondValueFormat)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16((ushort)firstValueFormat)
            .WriteUInt16((ushort)secondValueFormat)
            .WriteUInt16((ushort)firstGlyphIds.Length)
            .WriteZeros(firstGlyphIds.Length * 2);
        int coverage = WriteCoverage(builder, firstGlyphIds);
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int firstPairSet = 0;
        int index = 0;

        while (index < firstGlyphIds.Length)
        {
            int pairSet = builder.GetLength();

            if (index == 0)
            {
                firstPairSet = pairSet;
            }

            builder.PatchUInt16(
                subtable + 10 + index * 2,
                (ushort)(pairSet - subtable));
            builder.WriteUInt16(1)
                .WriteUInt16((ushort)pairValues[index][0])
                .WriteInt16((short)pairValues[index][1]);

            if (secondValueFormat != 0)
            {
                builder.WriteInt16((short)pairValues[index][2]);
            }

            index += 1;
        }

        return firstPairSet;
    }

    static void WritePairFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0)
            .WriteUInt16(0x0004).WriteUInt16(0x0002)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(2).WriteUInt16(3);
        int recordIndex = 0;

        while (recordIndex < 6)
        {
            if (recordIndex == 5)
            {
                builder.WriteInt16(-40).WriteInt16(15);
            }
            else
            {
                builder.WriteInt16(0).WriteInt16(0);
            }

            recordIndex += 1;
        }

        int coverage = WriteCoverage(builder, new int[] { 6 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int firstClass = WriteClassDefinition(
            builder,
            6,
            new int[] { 1 });
        builder.PatchUInt16(subtable + 8, (ushort)(firstClass - subtable));
        int secondClass = WriteClassDefinition(
            builder,
            7,
            new int[] { 1, 2 });
        builder.PatchUInt16(subtable + 10,
            (ushort)(secondClass - subtable));
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
    {
        int coverage = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }

        return coverage;
    }

    static int WriteClassDefinition(
        TestBinaryBuilder builder,
        int startGlyphId,
        int[] classValues)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(1)
            .WriteUInt16((ushort)startGlyphId)
            .WriteUInt16((ushort)classValues.Length);
        int index = 0;

        while (index < classValues.Length)
        {
            builder.WriteUInt16((ushort)classValues[index]);
            index += 1;
        }

        return offset;
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
