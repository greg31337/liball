using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class SfntMaxpTableTests
{
    [Test]
    public void ParsesCffVersionAndGlyphCount()
    {
        SfntMaxpTable? table = Parse(BuildCffMaxp(321));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersionRaw(), Is.EqualTo(0x00005000U));
            Assert.That(table.GetVersion(), Is.EqualTo(0.5));
            Assert.That(table.GetKind(), Is.EqualTo(SfntMaxpKind.Cff));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(321));
            Assert.Throws<InvalidOperationException>(() => table.GetMaxPoints());
            Assert.Throws<InvalidOperationException>(() => table.GetMaxComponentDepth());
            Assert.Throws<InvalidOperationException>(() => table.VerifySimpleGlyph(
                0,
                0,
                0,
                FontLimits.CreateCompatible()));
        });
    }

    [Test]
    public void ParsesEveryTrueTypeMaximumField()
    {
        SfntMaxpTable? table = Parse(BuildTrueTypeMaxp());

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersionRaw(), Is.EqualTo(0x00010000U));
            Assert.That(table.GetVersion(), Is.EqualTo(1.0));
            Assert.That(table.GetKind(), Is.EqualTo(SfntMaxpKind.TrueType));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(123));
            Assert.That(table.GetMaxPoints(), Is.EqualTo(100));
            Assert.That(table.GetMaxContours(), Is.EqualTo(10));
            Assert.That(table.GetMaxCompositePoints(), Is.EqualTo(300));
            Assert.That(table.GetMaxCompositeContours(), Is.EqualTo(30));
            Assert.That(table.GetMaxZones(), Is.EqualTo(2));
            Assert.That(table.GetMaxTwilightPoints(), Is.EqualTo(20));
            Assert.That(table.GetMaxStorage(), Is.EqualTo(40));
            Assert.That(table.GetMaxFunctionDefinitions(), Is.EqualTo(10));
            Assert.That(table.GetMaxInstructionDefinitions(), Is.EqualTo(5));
            Assert.That(table.GetMaxStackElements(), Is.EqualTo(100));
            Assert.That(table.GetMaxInstructionSize(), Is.EqualTo(200));
            Assert.That(table.GetMaxComponentElements(), Is.EqualTo(8));
            Assert.That(table.GetMaxComponentDepth(), Is.EqualTo(4));
        });
    }

    [Test]
    public void RejectsEveryCffAndTrueTypeTruncationPoint()
    {
        byte[] cff = BuildCffMaxp(1);
        byte[] trueType = BuildTrueTypeMaxp();

        for (int length = 0; length < cff.Length; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => Parse(cff.AsSpan(0, length).ToArray()),
                "CFF length {0}",
                length);
        }

        for (int length = 0; length < trueType.Length; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => Parse(trueType.AsSpan(0, length).ToArray()),
                "TrueType length {0}",
                length);
        }
    }

    [Test]
    public void RejectsTrailingDataForBothPublishedForms()
    {
        byte[] cff = Append(BuildCffMaxp(1), new byte[] { 0 });
        byte[] trueType = Append(BuildTrueTypeMaxp(), new byte[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(cff));
            Assert.Throws<FontFormatException>(() => Parse(trueType));
        });
    }

    [TestCase(0x00004000U)]
    [TestCase(0x00006000U)]
    [TestCase(0x00010001U)]
    public void RejectsUnsupportedVersionsWithinKnownMajorFamilies(uint version)
    {
        byte[] bytes = BuildTrueTypeMaxp();
        WriteUInt32(bytes, 0, version);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void UnknownMajorVersionRemainsRaw()
    {
        byte[] bytes = BuildTrueTypeMaxp();
        WriteUInt32(bytes, 0, 0x00020000U);
        SfntTableCache cache = CreateCache(
            bytes,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        SfntParsedTableView? view = SfntMaxpTableParser.FindView(cache);

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(view.HasParsedData(), Is.False);
            Assert.That(SfntMaxpTableParser.FindTable(cache), Is.Null);
        });
    }

    [Test]
    public void RejectsZeroGlyphCountInBothForms()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(BuildCffMaxp(0)));
            Assert.Throws<FontFormatException>(() => Parse(BuildTrueTypeMaxp(0)));
        });
    }

    [Test]
    public void EnforcesConfiguredGlyphCountWithoutUsingOtherMaximaForAllocation()
    {
        FontLimits glyphLimits = CreateLimits(100, 10, 10, 2);
        FontLimits declarationLimits = CreateLimits(1000, 10, 10, 2);
        byte[] tooManyGlyphs = BuildTrueTypeMaxp(101);
        byte[] largeDeclarations = BuildTrueTypeMaxp();

        for (int offset = 6; offset < largeDeclarations.Length; offset += 2)
        {
            WriteUInt16(largeDeclarations, offset, ushort.MaxValue);
        }

        WriteUInt16(largeDeclarations, 14, 2);

        Assert.Throws<FontLimitException>(() => Parse(tooManyGlyphs, glyphLimits));
        SfntMaxpTable? parsed = Parse(largeDeclarations, declarationLimits);

        Assert.Multiple(() =>
        {
            Assert.That(parsed!.GetGlyphCount(), Is.EqualTo(123));
            Assert.That(parsed.GetMaxPoints(), Is.EqualTo(ushort.MaxValue));
            Assert.That(parsed.GetMaxCompositePoints(), Is.EqualTo(ushort.MaxValue));
            Assert.That(parsed.GetMaxComponentDepth(), Is.EqualTo(ushort.MaxValue));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(65535)]
    public void RejectsInvalidMaxZones(int maxZones)
    {
        byte[] bytes = BuildTrueTypeMaxp();
        WriteUInt16(bytes, 14, maxZones);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void AcceptsBothValidMaxZonesValues(int maxZones)
    {
        byte[] bytes = BuildTrueTypeMaxp();
        WriteUInt16(bytes, 14, maxZones);

        Assert.That(Parse(bytes)!.GetMaxZones(), Is.EqualTo(maxZones));
    }

    [Test]
    public void VerifiesObservedSimpleGlyphUsageAgainstDeclaredAndHardLimits()
    {
        SfntMaxpTable? table = Parse(BuildTrueTypeMaxp());
        FontLimits generous = CreateLimits(1000, 1000, 1000, 100);
        FontLimits bounded = CreateLimits(1000, 50, 5, 100);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => table!.VerifySimpleGlyph(100, 10, 200, generous));
            Assert.Throws<FontFormatException>(() => table!.VerifySimpleGlyph(101, 10, 200, generous));
            Assert.Throws<FontFormatException>(() => table!.VerifySimpleGlyph(100, 11, 200, generous));
            Assert.Throws<FontFormatException>(() => table!.VerifySimpleGlyph(100, 10, 201, generous));
            Assert.Throws<FontLimitException>(() => table!.VerifySimpleGlyph(51, 5, 0, bounded));
            Assert.Throws<FontLimitException>(() => table!.VerifySimpleGlyph(50, 6, 0, bounded));
        });
    }

    [Test]
    public void VerifiesObservedCompositeGlyphUsageAgainstDeclaredAndHardLimits()
    {
        SfntMaxpTable? table = Parse(BuildTrueTypeMaxp());
        FontLimits generous = CreateLimits(1000, 1000, 1000, 100);
        FontLimits bounded = CreateLimits(1000, 200, 20, 3);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => table!.VerifyCompositeGlyph(
                300,
                30,
                200,
                8,
                4,
                generous));
            Assert.Throws<FontFormatException>(() => table!.VerifyCompositeGlyph(
                301,
                30,
                200,
                8,
                4,
                generous));
            Assert.Throws<FontFormatException>(() => table!.VerifyCompositeGlyph(
                300,
                31,
                200,
                8,
                4,
                generous));
            Assert.Throws<FontFormatException>(() => table!.VerifyCompositeGlyph(
                300,
                30,
                200,
                9,
                4,
                generous));
            Assert.Throws<FontFormatException>(() => table!.VerifyCompositeGlyph(
                300,
                30,
                200,
                8,
                5,
                generous));
            Assert.Throws<FontLimitException>(() => table!.VerifyCompositeGlyph(
                200,
                20,
                0,
                1,
                4,
                bounded));
        });
    }

    [Test]
    public void VerifiesEveryHintingResourceMaximum()
    {
        SfntMaxpTable? table = Parse(BuildTrueTypeMaxp());

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => table!.VerifyHintingResourceUsage(
                2,
                20,
                40,
                10,
                5,
                100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                3, 20, 40, 10, 5, 100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                2, 21, 40, 10, 5, 100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                2, 20, 41, 10, 5, 100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                2, 20, 40, 11, 5, 100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                2, 20, 40, 10, 6, 100));
            Assert.Throws<FontFormatException>(() => table!.VerifyHintingResourceUsage(
                2, 20, 40, 10, 5, 101));
        });
    }

    [Test]
    public void GlyphCountVerificationRequiresAnExactNonnegativeCount()
    {
        SfntMaxpTable? table = Parse(BuildTrueTypeMaxp());

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => table!.VerifyGlyphCount(123));
            Assert.Throws<FontFormatException>(() => table!.VerifyGlyphCount(122));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.VerifyGlyphCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.VerifySimpleGlyph(
                -1,
                0,
                0,
                FontLimits.CreateCompatible()));
            Assert.Throws<ArgumentNullException>(() => table!.VerifySimpleGlyph(0, 0, 0, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.VerifyHintingResourceUsage(
                -1, 0, 0, 0, 0, 0));
        });
    }

    [Test]
    public void FontFaceRequiresMaxpFormMatchingOutlineKind()
    {
        FontFace trueType = CreateFace(BuildTrueTypeMaxp(), FontOutlineKind.TrueType);
        FontFace cff = CreateFace(BuildCffMaxp(12), FontOutlineKind.Cff1);
        FontFace wrongTrueType = CreateFace(BuildCffMaxp(12), FontOutlineKind.TrueType);
        FontFace wrongCff = CreateFace(BuildTrueTypeMaxp(), FontOutlineKind.Cff2);

        Assert.Multiple(() =>
        {
            Assert.That(trueType.GetMaxpTable()!.GetKind(), Is.EqualTo(SfntMaxpKind.TrueType));
            Assert.That(cff.GetMaxpTable()!.GetKind(), Is.EqualTo(SfntMaxpKind.Cff));
            Assert.Throws<FontFormatException>(() => wrongTrueType.GetMaxpTable());
            Assert.Throws<FontFormatException>(() => wrongCff.GetMaxpTable());
            Assert.That(trueType.GetMaxpTableView()!.GetParsedData(), Is.SameAs(
                trueType.GetMaxpTable()));
        });
    }

    [Test]
    public void MissingMaxpReturnsNoViewOrParsedTable()
    {
        SfntTableStore store = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache cache = SfntTableCache.Create(store, FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(SfntMaxpTableParser.FindView(cache), Is.Null);
            Assert.That(SfntMaxpTableParser.FindTable(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntMaxpTableParser.FindView(null!));
        });
    }

    static SfntMaxpTable? Parse(byte[] bytes)
    {
        return Parse(bytes, FontLimits.CreateCompatible());
    }

    static SfntMaxpTable? Parse(byte[] bytes, FontLimits limits)
    {
        return SfntMaxpTableParser.FindTable(CreateCache(
            bytes,
            FontLoadMode.Compatible,
            limits));
    }

    static FontFace CreateFace(byte[] bytes, FontOutlineKind outlineKind)
    {
        SfntTableCache cache = CreateCache(
            bytes,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntValidationResult validation = new SfntValidationResult(
            outlineKind,
            Array.Empty<FontDiagnostic>());
        return new FontFace(0, 0x00010000U, validation, cache);
    }

    static SfntTableCache CreateCache(
        byte[] bytes,
        FontLoadMode mode,
        FontLimits limits)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Maxp,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableStore store = new SfntTableStore(
            new SfntTableData[] { table },
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, mode, limits);
    }

    static byte[] BuildCffMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildTrueTypeMaxp()
    {
        return BuildTrueTypeMaxp(123);
    }

    static byte[] BuildTrueTypeMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(100)
            .WriteUInt16(10)
            .WriteUInt16(300)
            .WriteUInt16(30)
            .WriteUInt16(2)
            .WriteUInt16(20)
            .WriteUInt16(40)
            .WriteUInt16(10)
            .WriteUInt16(5)
            .WriteUInt16(100)
            .WriteUInt16(200)
            .WriteUInt16(8)
            .WriteUInt16(4)
            .CopyBytes();
    }

    static FontLimits CreateLimits(
        int maxGlyphs,
        int maxPoints,
        int maxContours,
        int maxComponentDepth)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            maxGlyphs,
            maxPoints,
            maxContours,
            maxComponentDepth,
            10,
            10,
            100,
            100000,
            100000,
            100000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), (ushort)value);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }
}
