namespace ttf.Tests;

internal class SfntLayoutGlyphClassTests
{
    [Test]
    public void ParsesAndQueriesCoverageFormat1()
    {
        SfntCoverageTable coverage = ParseCoverage(
            BuildCoverage1(2, 5, 9),
            10);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetFormat(), Is.EqualTo(1));
            Assert.That(coverage.GetCoveredGlyphCount(), Is.EqualTo(3));
            Assert.That(coverage.GetExplicitGlyphCount(), Is.EqualTo(3));
            Assert.That(coverage.GetExplicitGlyphId(1), Is.EqualTo(5));
            Assert.That(coverage.GetRangeCount(), Is.Zero);
            Assert.That(coverage.GetCoverageIndex(2), Is.Zero);
            Assert.That(coverage.GetCoverageIndex(5), Is.EqualTo(1));
            Assert.That(coverage.GetCoverageIndex(9), Is.EqualTo(2));
            Assert.That(coverage.GetCoverageIndex(4), Is.EqualTo(-1));
            Assert.That(coverage.GetCoverageIndex(-1), Is.EqualTo(-1));
            Assert.That(coverage.Covers(9), Is.True);
            Assert.That(coverage.Covers(10), Is.False);
        });
    }

    [Test]
    public void ParsesAndQueriesCoverageFormat2WithoutExpandingRanges()
    {
        SfntCoverageTable coverage = ParseCoverage(
            BuildCoverage2(
                new int[] { 1, 3, 0 },
                new int[] { 7, 9, 3 }),
            12);
        SfntCoverageRange first = coverage.GetRange(0);
        SfntCoverageRange second = coverage.GetRange(1);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetFormat(), Is.EqualTo(2));
            Assert.That(coverage.GetCoveredGlyphCount(), Is.EqualTo(6));
            Assert.That(coverage.GetExplicitGlyphCount(), Is.Zero);
            Assert.That(coverage.GetRangeCount(), Is.EqualTo(2));
            Assert.That(first.GetStartGlyphId(), Is.EqualTo(1));
            Assert.That(first.GetEndGlyphId(), Is.EqualTo(3));
            Assert.That(first.GetStartCoverageIndex(), Is.Zero);
            Assert.That(first.Contains(2), Is.True);
            Assert.That(first.GetCoverageIndex(3), Is.EqualTo(2));
            Assert.That(first.GetCoverageIndex(7), Is.EqualTo(-1));
            Assert.That(second.GetStartCoverageIndex(), Is.EqualTo(3));
            Assert.That(coverage.GetCoverageIndex(1), Is.Zero);
            Assert.That(coverage.GetCoverageIndex(3), Is.EqualTo(2));
            Assert.That(coverage.GetCoverageIndex(7), Is.EqualTo(3));
            Assert.That(coverage.GetCoverageIndex(9), Is.EqualTo(5));
            Assert.That(coverage.GetCoverageIndex(6), Is.EqualTo(-1));
        });
    }

    [Test]
    public void ParsesAndQueriesClassDefinitionFormat1()
    {
        SfntClassDefinitionTable classes = ParseClassDefinition(
            BuildClassDefinition1(3, 1, 0, 2, 2),
            10);

        Assert.Multiple(() =>
        {
            Assert.That(classes.GetFormat(), Is.EqualTo(1));
            Assert.That(classes.GetStartGlyphId(), Is.EqualTo(3));
            Assert.That(classes.GetClassValueCount(), Is.EqualTo(4));
            Assert.That(classes.GetClassValue(2), Is.EqualTo(2));
            Assert.That(classes.GetRangeCount(), Is.Zero);
            Assert.That(classes.GetMaximumClassValue(), Is.EqualTo(2));
            Assert.That(classes.GetClassForGlyph(2), Is.Zero);
            Assert.That(classes.GetClassForGlyph(3), Is.EqualTo(1));
            Assert.That(classes.GetClassForGlyph(4), Is.Zero);
            Assert.That(classes.GetClassForGlyph(5), Is.EqualTo(2));
            Assert.That(classes.GetClassForGlyph(7), Is.Zero);
        });
    }

    [Test]
    public void ParsesAndQueriesClassDefinitionFormat2WithoutExpandingRanges()
    {
        SfntClassDefinitionTable classes = ParseClassDefinition(
            BuildClassDefinition2(
                new int[] { 1, 2, 4 },
                new int[] { 6, 8, 2 }),
            10);
        SfntClassRange first = classes.GetRange(0);

        Assert.Multiple(() =>
        {
            Assert.That(classes.GetFormat(), Is.EqualTo(2));
            Assert.That(classes.GetClassValueCount(), Is.Zero);
            Assert.That(classes.GetRangeCount(), Is.EqualTo(2));
            Assert.That(classes.GetMaximumClassValue(), Is.EqualTo(4));
            Assert.That(first.GetStartGlyphId(), Is.EqualTo(1));
            Assert.That(first.GetEndGlyphId(), Is.EqualTo(2));
            Assert.That(first.GetClassValue(), Is.EqualTo(4));
            Assert.That(first.Contains(2), Is.True);
            Assert.That(classes.GetClassForGlyph(0), Is.Zero);
            Assert.That(classes.GetClassForGlyph(1), Is.EqualTo(4));
            Assert.That(classes.GetClassForGlyph(5), Is.Zero);
            Assert.That(classes.GetClassForGlyph(8), Is.EqualTo(2));
            Assert.That(classes.GetClassForGlyph(10), Is.Zero);
        });
    }

    [Test]
    public void KeepsMaximumGlyphRangesCompactAndCountsStoredEntries()
    {
        FontLimits oneEntry = CreateLimits(1);
        byte[] coverageBytes = BuildCoverage2(
            new int[] { 0, 65534, 0 });
        byte[] classBytes = BuildClassDefinition2(
            new int[] { 0, 65534, 7 });
        SfntCoverageTable coverage = ParseCoverage(
            coverageBytes,
            65535,
            oneEntry);
        SfntClassDefinitionTable classes = ParseClassDefinition(
            classBytes,
            65535,
            oneEntry);

        Assert.Multiple(() =>
        {
            Assert.That(coverage.GetRangeCount(), Is.EqualTo(1));
            Assert.That(coverage.GetExplicitGlyphCount(), Is.Zero);
            Assert.That(coverage.GetCoveredGlyphCount(), Is.EqualTo(65535));
            Assert.That(coverage.GetCoverageIndex(65534),
                Is.EqualTo(65534));
            Assert.That(classes.GetRangeCount(), Is.EqualTo(1));
            Assert.That(classes.GetClassValueCount(), Is.Zero);
            Assert.That(classes.GetClassForGlyph(65534), Is.EqualTo(7));
            Assert.Throws<FontLimitException>(() => ParseCoverage(
                BuildCoverage1(1, 2),
                10,
                oneEntry));
            Assert.Throws<FontLimitException>(() => ParseClassDefinition(
                BuildClassDefinition1(1, 1, 2),
                10,
                oneEntry));
        });
    }

    [TestCaseSource(nameof(InvalidCoverageTables))]
    public void RejectsMalformedCoverageTables(byte[] bytes, int glyphCount)
    {
        Assert.Throws<FontFormatException>(() =>
            ParseCoverage(bytes, glyphCount));
    }

    [TestCaseSource(nameof(InvalidClassDefinitionTables))]
    public void RejectsMalformedClassDefinitionTables(
        byte[] bytes,
        int glyphCount)
    {
        Assert.Throws<FontFormatException>(() =>
            ParseClassDefinition(bytes, glyphCount));
    }

    [Test]
    public void ValidatesOffsetsGlyphCountsAndNullArguments()
    {
        FontData data = FontData.Create(BuildCoverage1(1));
        FontDataSlice slice = data.GetSlice(0, data.GetLength());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntLayoutGlyphClassParser.ParseCoverage(
                    slice,
                    -1,
                    10,
                    FontLimits.CreateStrict()));
            Assert.Throws<FontFormatException>(() =>
                SfntLayoutGlyphClassParser.ParseCoverage(
                    slice,
                    slice.GetLength(),
                    10,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutGlyphClassParser.ParseCoverage(
                    slice,
                    0,
                    -1,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutGlyphClassParser.ParseClassDefinition(
                    slice,
                    0,
                    65536,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutGlyphClassParser.ParseCoverage(
                    null!,
                    0,
                    10,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutGlyphClassParser.ParseClassDefinition(
                    slice,
                    0,
                    10,
                    null!));
        });
    }

    static IEnumerable<TestCaseData> InvalidCoverageTables()
    {
        yield return InvalidCase(
            new byte[] { 0 },
            10,
            "TruncatedHeader");
        yield return InvalidCase(
            new byte[] { 0, 3 },
            10,
            "UnsupportedFormat");
        yield return InvalidCase(
            BuildCoverage1Declared(2, 1),
            10,
            "TruncatedFormat1");
        yield return InvalidCase(
            BuildCoverage1(2, 1),
            10,
            "UnorderedFormat1");
        yield return InvalidCase(
            BuildCoverage1(2, 2),
            10,
            "DuplicateFormat1");
        yield return InvalidCase(
            BuildCoverage1(10),
            10,
            "OutsideGlyphSetFormat1");
        yield return InvalidCase(
            BuildCoverage2(new int[] { 4, 3, 0 }),
            10,
            "ReversedRangeFormat2");
        yield return InvalidCase(
            BuildCoverage2(
                new int[] { 1, 4, 0 },
                new int[] { 4, 6, 4 }),
            10,
            "OverlappingRangesFormat2");
        yield return InvalidCase(
            BuildCoverage2(new int[] { 1, 2, 1 }),
            10,
            "NonzeroFirstCoverageIndex");
        yield return InvalidCase(
            BuildCoverage2(
                new int[] { 1, 2, 0 },
                new int[] { 5, 6, 3 }),
            10,
            "DiscontinuousCoverageIndex");
        yield return InvalidCase(
            BuildCoverage2(new int[] { 8, 10, 0 }),
            10,
            "OutsideGlyphSetFormat2");
        yield return InvalidCase(
            BuildCoverage2Declared(2, new int[] { 1, 2, 0 }),
            10,
            "TruncatedFormat2");
    }

    static IEnumerable<TestCaseData> InvalidClassDefinitionTables()
    {
        yield return InvalidCase(
            new byte[] { 0 },
            10,
            "TruncatedHeader");
        yield return InvalidCase(
            new byte[] { 0, 3 },
            10,
            "UnsupportedFormat");
        yield return InvalidCase(
            BuildClassDefinition1Declared(3, 2, 1),
            10,
            "TruncatedFormat1");
        yield return InvalidCase(
            BuildClassDefinition1(9, 1, 2),
            10,
            "OutsideGlyphSetFormat1");
        yield return InvalidCase(
            BuildClassDefinition2(new int[] { 4, 3, 1 }),
            10,
            "ReversedRangeFormat2");
        yield return InvalidCase(
            BuildClassDefinition2(
                new int[] { 1, 4, 1 },
                new int[] { 4, 6, 2 }),
            10,
            "OverlappingRangesFormat2");
        yield return InvalidCase(
            BuildClassDefinition2(new int[] { 8, 10, 1 }),
            10,
            "OutsideGlyphSetFormat2");
        yield return InvalidCase(
            BuildClassDefinition2Declared(
                2,
                new int[] { 1, 2, 1 }),
            10,
            "TruncatedFormat2");
    }

    static TestCaseData InvalidCase(
        byte[] bytes,
        int glyphCount,
        string name)
    {
        return new TestCaseData(bytes, glyphCount)
            .SetName("Invalid_" + name);
    }

    static byte[] BuildCoverage1(params int[] glyphIds)
    {
        return BuildCoverage1Declared(glyphIds.Length, glyphIds);
    }

    static byte[] BuildCoverage1Declared(
        int declaredCount,
        params int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)declaredCount);
        WriteValues(builder, glyphIds);
        return builder.CopyBytes();
    }

    static byte[] BuildCoverage2(params int[][] ranges)
    {
        return BuildCoverage2Declared(ranges.Length, ranges);
    }

    static byte[] BuildCoverage2Declared(
        int declaredCount,
        params int[][] ranges)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16((ushort)declaredCount);
        WriteRanges(builder, ranges);
        return builder.CopyBytes();
    }

    static byte[] BuildClassDefinition1(
        int startGlyphId,
        params int[] classValues)
    {
        return BuildClassDefinition1Declared(
            startGlyphId,
            classValues.Length,
            classValues);
    }

    static byte[] BuildClassDefinition1Declared(
        int startGlyphId,
        int declaredCount,
        params int[] classValues)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)startGlyphId)
            .WriteUInt16((ushort)declaredCount);
        WriteValues(builder, classValues);
        return builder.CopyBytes();
    }

    static byte[] BuildClassDefinition2(params int[][] ranges)
    {
        return BuildClassDefinition2Declared(ranges.Length, ranges);
    }

    static byte[] BuildClassDefinition2Declared(
        int declaredCount,
        params int[][] ranges)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16((ushort)declaredCount);
        WriteRanges(builder, ranges);
        return builder.CopyBytes();
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

    static void WriteRanges(TestBinaryBuilder builder, int[][] ranges)
    {
        int index = 0;

        while (index < ranges.Length)
        {
            WriteValues(builder, ranges[index]);
            index += 1;
        }
    }

    static SfntCoverageTable ParseCoverage(
        byte[] structure,
        int glyphCount)
    {
        return ParseCoverage(
            structure,
            glyphCount,
            FontLimits.CreateStrict());
    }

    static SfntCoverageTable ParseCoverage(
        byte[] structure,
        int glyphCount,
        FontLimits limits)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutGlyphClassParser.ParseCoverage(
            data.GetSlice(0, data.GetLength()),
            4,
            glyphCount,
            limits);
    }

    static SfntClassDefinitionTable ParseClassDefinition(
        byte[] structure,
        int glyphCount)
    {
        return ParseClassDefinition(
            structure,
            glyphCount,
            FontLimits.CreateStrict());
    }

    static SfntClassDefinitionTable ParseClassDefinition(
        byte[] structure,
        int glyphCount,
        FontLimits limits)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutGlyphClassParser.ParseClassDefinition(
            data.GetSlice(0, data.GetLength()),
            4,
            glyphCount,
            limits);
    }

    static FontData WithPrefix(byte[] structure)
    {
        byte[] bytes = new byte[structure.Length + 4];
        structure.CopyTo(bytes, 4);
        return FontData.Create(bytes);
    }

    static FontLimits CreateLimits(int maxEntries)
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
            maxEntries,
            100,
            100000,
            10,
            100,
            10,
            10);
    }
}
