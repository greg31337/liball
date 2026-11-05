using ttf;

namespace ttf.Tests;

internal class SfntGsubChainedFixture
{
    internal byte[]? Bytes;
    internal int Format1Lookup;
    internal int Format1Subtable;
    internal int Format1Rule;
    internal int Format2Subtable;
    internal int Format2Rule;
    internal int Format3Subtable;
    internal int ExtensionSubtable;
    internal int ReverseSubtable;
}

internal class SfntGsubChainedTests
{
    [Test]
    public void ParsesChainedExtensionAndReverseSubtables()
    {
        SfntGsubTable? table = Parse(BuildGsub().Bytes);
        SfntGsubChainedContextSubstitution format1 = table!.GetLookup(1)
            .GetChainedContextSubstitution(0);
        SfntGsubChainedContextSubstitution format2 = table.GetLookup(2)
            .GetChainedContextSubstitution(0);
        SfntGsubChainedContextSubstitution format3 = table.GetLookup(3)
            .GetChainedContextSubstitution(0);
        SfntGsubChainedRule rule1 = format1.GetRuleSet(0).GetRule(0);
        SfntGsubLookup extensionLookup = table.GetLookup(4);
        SfntGsubReverseSubstitution reverse = table.GetLookup(5)
            .GetReverseSubstitution(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetLookupCount(), Is.EqualTo(6));
            Assert.That(format1.GetFormat(), Is.EqualTo(1));
            Assert.That(rule1.GetBacktrackCount(), Is.EqualTo(2));
            Assert.That(rule1.GetBacktrackValue(0), Is.EqualTo(7));
            Assert.That(rule1.GetInputGlyphCount(), Is.EqualTo(2));
            Assert.That(rule1.GetLookaheadValue(1), Is.EqualTo(13));
            Assert.That(format2.GetFormat(), Is.EqualTo(2));
            Assert.That(format2.GetInputClassDefinition()!
                .GetClassForGlyph(32), Is.EqualTo(1));
            Assert.That(format2.GetRuleSet(1).GetRule(0)
                .GetInputValue(0), Is.EqualTo(2));
            Assert.That(format3.GetFormat(), Is.EqualTo(3));
            Assert.That(format3.GetBacktrackCoverageCount(), Is.EqualTo(2));
            Assert.That(format3.GetInputCoverageCount(), Is.EqualTo(2));
            Assert.That(format3.GetLookaheadCoverageCount(), Is.EqualTo(2));
            Assert.That(extensionLookup.GetLayoutLookup().GetLookupType(),
                Is.EqualTo(7));
            Assert.That(extensionLookup.GetLookupType(), Is.EqualTo(1));
            Assert.That(extensionLookup.GetExtension(0).GetLookupType(),
                Is.EqualTo(1));
            Assert.That(extensionLookup.GetSingleSubstitution(0)
                .GetSubstituteGlyphId(5), Is.EqualTo(15));
            Assert.That(reverse.GetBacktrackCoverageCount(), Is.EqualTo(1));
            Assert.That(reverse.GetLookaheadCoverageCount(), Is.EqualTo(1));
            Assert.That(reverse.GetSubstituteGlyphId(3), Is.EqualTo(13));
        });
    }

    [Test]
    public void AppliesSimpleChainedContextWithReverseOrderedBacktrack()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 8, 7, 10, 11, 12, 13 });
        int count = Apply(1, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 8, 7, 10, 21, 12, 13 }));
        });
    }

    [Test]
    public void RequiresBacktrackInputAndLookaheadBeforeApplyingActions()
    {
        SfntLayoutGlyphBuffer wrongBacktrack = SfntLayoutGlyphBuffer.Create(
            new int[] { 8, 6, 10, 11, 12, 13 });
        SfntLayoutGlyphBuffer wrongLookahead = SfntLayoutGlyphBuffer.Create(
            new int[] { 8, 7, 10, 11, 12, 14 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(1, wrongBacktrack, null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
            Assert.That(CopyGlyphIds(wrongBacktrack), Is.EqualTo(
                new int[] { 8, 6, 10, 11, 12, 13 }));
            Assert.That(Apply(1, wrongLookahead, null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
            Assert.That(CopyGlyphIds(wrongLookahead), Is.EqualTo(
                new int[] { 8, 7, 10, 11, 12, 14 }));
        });
    }

    [Test]
    public void AppliesClassBasedChainedContext()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 30, 31, 32, 33, 34, 35 });
        int count = Apply(2, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 30, 31, 42, 33, 34, 35 }));
        });
    }

    [Test]
    public void AppliesCoverageBasedChainedContext()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 46, 47, 48, 49, 50, 51 });
        int count = Apply(3, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 46, 47, 48, 59, 50, 51 }));
        });
    }

    [Test]
    public void UsesLookupFlagsAcrossEveryChainedSequence()
    {
        SfntGsubChainedFixture fixture = BuildGsub();
        PatchUInt16(fixture.Bytes, fixture.Format1Lookup + 2, 0x0008);
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 8, 60, 7, 60, 10, 60, 11, 60, 12, 60, 13 });
        int count = SfntGsubExecutor.ApplyLookup(
            Parse(fixture.Bytes)!,
            1,
            buffer,
            CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(new int[]
            {
                8, 60, 7, 60, 10, 60, 21, 60, 12, 60, 13
            }));
        });
    }

    [Test]
    public void MatchesChainedContextBackward()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 13, 12, 11, 10, 7, 8 });
        int count = Apply(1, buffer, null,
            SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 13, 12, 21, 10, 7, 8 }));
        });
    }

    [Test]
    public void ExecutesExtensionSubstitution()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 5 });
        int count = Apply(4, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(15));
        });
    }

    [Test]
    public void AppliesReverseChainedSingleOnlyInMatchingContext()
    {
        SfntLayoutGlyphBuffer matching = SfntLayoutGlyphBuffer.Create(
            new int[] { 2, 3, 4 });
        SfntLayoutGlyphBuffer missingLookahead = SfntLayoutGlyphBuffer.Create(
            new int[] { 2, 3, 5 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(5, matching, null,
                SfntLayoutTraversalDirection.Forward), Is.EqualTo(1));
            Assert.That(CopyGlyphIds(matching), Is.EqualTo(
                new int[] { 2, 13, 4 }));
            Assert.That(Apply(5, missingLookahead, null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
            Assert.That(CopyGlyphIds(missingLookahead), Is.EqualTo(
                new int[] { 2, 3, 5 }));
        });
    }

    [Test]
    public void ProcessesReverseChainedSubstitutionFromEndToStart()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 6, 6, 6 });
        int count = Apply(5, buffer, null,
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(CopyGlyphIds(buffer), Is.EqualTo(
                new int[] { 6, 16, 16 }));
        });
    }

    [TestCaseSource(nameof(InvalidTables))]
    public void RejectsMalformedChainedExtensionAndReverseTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidTables()
    {
        SfntGsubChainedFixture chainedFormat = BuildGsub();
        PatchUInt16(chainedFormat.Bytes, chainedFormat.Format1Subtable, 4);
        yield return InvalidCase(chainedFormat.Bytes, "ChainedFormat");

        SfntGsubChainedFixture emptyInput = BuildGsub();
        int inputCountOffset = emptyInput.Format1Rule + 6;
        PatchUInt16(emptyInput.Bytes, inputCountOffset, 0);
        yield return InvalidCase(emptyInput.Bytes, "EmptyChainedInput");

        SfntGsubChainedFixture invalidClass = BuildGsub();
        int inputClassOffset = invalidClass.Format2Rule + 8;
        PatchUInt16(invalidClass.Bytes, inputClassOffset, 3);
        yield return InvalidCase(invalidClass.Bytes, "ChainedClass");

        SfntGsubChainedFixture extensionFormat = BuildGsub();
        PatchUInt16(extensionFormat.Bytes,
            extensionFormat.ExtensionSubtable, 2);
        yield return InvalidCase(extensionFormat.Bytes, "ExtensionFormat");

        SfntGsubChainedFixture extensionType = BuildGsub();
        PatchUInt16(extensionType.Bytes,
            extensionType.ExtensionSubtable + 2, 7);
        yield return InvalidCase(extensionType.Bytes, "RecursiveExtension");

        SfntGsubChainedFixture reverseFormat = BuildGsub();
        PatchUInt16(reverseFormat.Bytes, reverseFormat.ReverseSubtable, 2);
        yield return InvalidCase(reverseFormat.Bytes, "ReverseFormat");

        SfntGsubChainedFixture reverseCount = BuildGsub();
        int reverseSubstituteCount = reverseCount.ReverseSubtable + 12;
        PatchUInt16(reverseCount.Bytes, reverseSubstituteCount, 0);
        yield return InvalidCase(reverseCount.Bytes, "ReverseCoverageCount");
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
        int[] classes = new int[64];
        classes[60] = 3;
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
                .WriteUInt16(64)
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

    static SfntGsubChainedFixture BuildGsub()
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
            .WriteUInt16(6).WriteZeros(12);
        SfntGsubChainedFixture fixture = new SfntGsubChainedFixture();

        int singleLookup = WriteLookupHeader(builder, lookupList, 0, 1, 1);
        int single = builder.GetLength();
        PatchLookupSubtable(builder, singleLookup, 0, single);
        WriteSingleFormat2(
            builder,
            new int[] { 11, 32, 49 },
            new int[] { 21, 42, 59 });

        fixture.Format1Lookup = WriteLookupHeader(
            builder,
            lookupList,
            1,
            6,
            1);
        fixture.Format1Subtable = builder.GetLength();
        PatchLookupSubtable(
            builder,
            fixture.Format1Lookup,
            0,
            fixture.Format1Subtable);
        fixture.Format1Rule = WriteChainedFormat1(builder);

        int format2Lookup = WriteLookupHeader(builder, lookupList, 2, 6, 1);
        fixture.Format2Subtable = builder.GetLength();
        PatchLookupSubtable(builder, format2Lookup, 0,
            fixture.Format2Subtable);
        fixture.Format2Rule = WriteChainedFormat2(builder);

        int format3Lookup = WriteLookupHeader(builder, lookupList, 3, 6, 1);
        fixture.Format3Subtable = builder.GetLength();
        PatchLookupSubtable(builder, format3Lookup, 0,
            fixture.Format3Subtable);
        WriteChainedFormat3(builder);

        int extensionLookup = WriteLookupHeader(builder, lookupList, 4, 7, 1);
        fixture.ExtensionSubtable = builder.GetLength();
        PatchLookupSubtable(builder, extensionLookup, 0,
            fixture.ExtensionSubtable);
        builder.WriteUInt16(1).WriteUInt16(1).WriteUInt32(0);
        int extendedSingle = builder.GetLength();
        builder.PatchUInt32(fixture.ExtensionSubtable + 4,
            (uint)(extendedSingle - fixture.ExtensionSubtable));
        WriteSingleFormat2(builder, new int[] { 5 }, new int[] { 15 });

        int reverseLookup = WriteLookupHeader(builder, lookupList, 5, 8, 2);
        fixture.ReverseSubtable = builder.GetLength();
        PatchLookupSubtable(builder, reverseLookup, 0,
            fixture.ReverseSubtable);
        WriteReverse(builder, 3, 13, new int[] { 2 }, new int[] { 4 });
        int reverseCascade = builder.GetLength();
        PatchLookupSubtable(builder, reverseLookup, 1, reverseCascade);
        WriteReverse(builder, 6, 16, new int[] { 6 }, new int[0]);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static int WriteLookupHeader(
        TestBinaryBuilder builder,
        int lookupList,
        int lookupIndex,
        int lookupType,
        int subtableCount)
    {
        int lookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 2 + lookupIndex * 2,
            (ushort)(lookup - lookupList));
        builder.WriteUInt16((ushort)lookupType)
            .WriteUInt16(0)
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

    static void WriteSingleFormat2(
        TestBinaryBuilder builder,
        int[] glyphIds,
        int[] substitutes)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0)
            .WriteUInt16((ushort)substitutes.Length);
        WriteValues(builder, substitutes);
        int coverage = WriteCoverage(builder, glyphIds);
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
    }

    static int WriteChainedFormat1(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int coverage = WriteCoverage(builder, new int[] { 10 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(subtable + 6, (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int rule = builder.GetLength();
        builder.PatchUInt16(ruleSet + 2, (ushort)(rule - ruleSet));
        WriteChainedRule(
            builder,
            new int[] { 7, 8 },
            new int[] { 11 },
            new int[] { 12, 13 },
            1,
            0);
        return rule;
    }

    static int WriteChainedFormat2(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(2).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(3).WriteZeros(6);
        int coverage = WriteCoverage(builder, new int[] { 32 });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        int backtrackClass = WriteClassDefinition(
            builder,
            30,
            new int[] { 1, 2 });
        builder.PatchUInt16(subtable + 4,
            (ushort)(backtrackClass - subtable));
        int inputClass = WriteClassDefinition(
            builder,
            32,
            new int[] { 1, 2 });
        builder.PatchUInt16(subtable + 6,
            (ushort)(inputClass - subtable));
        int lookaheadClass = WriteClassDefinition(
            builder,
            34,
            new int[] { 1, 2 });
        builder.PatchUInt16(subtable + 8,
            (ushort)(lookaheadClass - subtable));
        int ruleSet = builder.GetLength();
        builder.PatchUInt16(subtable + 14, (ushort)(ruleSet - subtable));
        builder.WriteUInt16(1).WriteUInt16(0);
        int rule = builder.GetLength();
        builder.PatchUInt16(ruleSet + 2, (ushort)(rule - ruleSet));
        WriteChainedRule(
            builder,
            new int[] { 2, 1 },
            new int[] { 2 },
            new int[] { 1, 2 },
            0,
            0);
        return rule;
    }

    static void WriteChainedFormat3(TestBinaryBuilder builder)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(3)
            .WriteUInt16(2).WriteZeros(4)
            .WriteUInt16(2).WriteZeros(4)
            .WriteUInt16(2).WriteZeros(4)
            .WriteUInt16(1);
        WriteLookupRecord(builder, 1, 0);
        int[] glyphIds = new int[] { 47, 46, 48, 49, 50, 51 };
        int[] patchOffsets = new int[]
        {
            subtable + 4,
            subtable + 6,
            subtable + 10,
            subtable + 12,
            subtable + 16,
            subtable + 18
        };
        int index = 0;

        while (index < glyphIds.Length)
        {
            int coverage = WriteCoverage(builder, new int[] { glyphIds[index] });
            builder.PatchUInt16(patchOffsets[index],
                (ushort)(coverage - subtable));
            index += 1;
        }
    }

    static void WriteChainedRule(
        TestBinaryBuilder builder,
        int[] backtrackValues,
        int[] trailingInputValues,
        int[] lookaheadValues,
        int sequenceIndex,
        int lookupIndex)
    {
        builder.WriteUInt16((ushort)backtrackValues.Length);
        WriteValues(builder, backtrackValues);
        builder.WriteUInt16((ushort)(trailingInputValues.Length + 1));
        WriteValues(builder, trailingInputValues);
        builder.WriteUInt16((ushort)lookaheadValues.Length);
        WriteValues(builder, lookaheadValues);
        builder.WriteUInt16(1);
        WriteLookupRecord(builder, sequenceIndex, lookupIndex);
    }

    static void WriteReverse(
        TestBinaryBuilder builder,
        int glyphId,
        int substituteGlyphId,
        int[] backtrackGlyphIds,
        int[] lookaheadGlyphIds)
    {
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16((ushort)backtrackGlyphIds.Length);
        int backtrackPatch = builder.GetLength();
        builder.WriteZeros(backtrackGlyphIds.Length * 2)
            .WriteUInt16((ushort)lookaheadGlyphIds.Length);
        int lookaheadPatch = builder.GetLength();
        builder.WriteZeros(lookaheadGlyphIds.Length * 2)
            .WriteUInt16(1)
            .WriteUInt16((ushort)substituteGlyphId);
        int coverage = WriteCoverage(builder, new int[] { glyphId });
        builder.PatchUInt16(subtable + 2, (ushort)(coverage - subtable));
        PatchCoverageSequence(
            builder,
            subtable,
            backtrackPatch,
            backtrackGlyphIds);
        PatchCoverageSequence(
            builder,
            subtable,
            lookaheadPatch,
            lookaheadGlyphIds);
    }

    static void PatchCoverageSequence(
        TestBinaryBuilder builder,
        int subtable,
        int patchOffset,
        int[] glyphIds)
    {
        int index = 0;

        while (index < glyphIds.Length)
        {
            int coverage = WriteCoverage(builder, new int[] { glyphIds[index] });
            builder.PatchUInt16(
                patchOffset + index * 2,
                (ushort)(coverage - subtable));
            index += 1;
        }
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
        WriteValues(builder, classValues);
        return offset;
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
        WriteValues(builder, glyphIds);
        return coverage;
    }

    static void WriteValues(TestBinaryBuilder builder, int[] values)
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
