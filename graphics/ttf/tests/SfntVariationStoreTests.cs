using ttf;

namespace ttf.Tests;

internal class SfntVariationStoreTests
{
    [TestCase(0, 0x03, 1, 4)]
    [TestCase(0, 0x11, 2, 2)]
    [TestCase(1, 0x27, 3, 8)]
    [TestCase(1, 0x3F, 4, 16)]
    public void ParsesBothIndexMapFormatsAndAllEntrySizes(
        int format,
        int entryFormat,
        int entrySize,
        int innerBitCount)
    {
        uint first = EncodeIndex(5, 1, innerBitCount);
        uint second = EncodeIndex(
            entrySize == 4 ? 0xFFFF : 7,
            entrySize == 4 ? 0xFFFF : 2,
            innerBitCount);
        SfntDeltaSetIndexMap map = ParseMap(BuildMap(
            format,
            entryFormat,
            new uint[] { first, second }));

        Assert.Multiple(() =>
        {
            Assert.That(map.GetOffset(), Is.EqualTo(3));
            Assert.That(map.GetFormat(), Is.EqualTo(format));
            Assert.That(map.GetEntryFormat(), Is.EqualTo(entryFormat));
            Assert.That(map.GetEntrySize(), Is.EqualTo(entrySize));
            Assert.That(map.GetInnerIndexBitCount(), Is.EqualTo(innerBitCount));
            Assert.That(map.GetEntryCount(), Is.EqualTo(2));
            Assert.That(map.GetEntry(0).GetOuterIndex(), Is.EqualTo(5));
            Assert.That(map.GetEntry(0).GetInnerIndex(), Is.EqualTo(1));
            Assert.That(map.Resolve(1), Is.SameAs(map.GetEntry(1)));
            Assert.That(map.Resolve(500), Is.SameAs(map.GetEntry(1)));
            Assert.That(map.GetEntry(1).HasVariationData(),
                Is.EqualTo(entrySize != 4));
        });
    }

    [Test]
    public void UsesImplicitIdentityForAnEmptyIndexMap()
    {
        SfntDeltaSetIndexMap map = ParseMap(BuildMap(
            0,
            0,
            Array.Empty<uint>()));
        SfntDeltaSetIndex index = map.Resolve(37);
        SfntDeltaSetIndex wideIndex = map.Resolve(0x00020025L);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetEntryCount(), Is.Zero);
            Assert.That(index.GetOuterIndex(), Is.Zero);
            Assert.That(index.GetInnerIndex(), Is.EqualTo(37));
            Assert.That(wideIndex.GetOuterIndex(), Is.EqualTo(2));
            Assert.That(wideIndex.GetInnerIndex(), Is.EqualTo(37));
        });
    }

    [TestCaseSource(nameof(InvalidIndexMaps))]
    public void RejectsMalformedIndexMaps(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseMap(bytes));
    }

    [Test]
    public void EnforcesIndexMapLimitsArgumentsAndAccessors()
    {
        byte[] mapBytes = BuildMap(0, 0, new uint[] { 1, 2 });
        FontData data = FontData.Create(mapBytes);
        FontDataSlice slice = data.GetSlice(0, data.GetLength());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                SfntDeltaSetIndexMapParser.Parse(
                    slice,
                    3,
                    3,
                    new SfntVariationParsingBudget(1)));
            Assert.Throws<ArgumentNullException>(() =>
                SfntDeltaSetIndexMapParser.Parse(
                    null!,
                    0,
                    0,
                    new SfntVariationParsingBudget(10)));
            Assert.Throws<ArgumentNullException>(() =>
                SfntDeltaSetIndexMapParser.Parse(slice, 3, 3, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntDeltaSetIndexMapParser.Parse(
                    slice,
                    3,
                    -1,
                    new SfntVariationParsingBudget(10)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntDeltaSetIndex(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntDeltaSetIndex(0x10000, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntDeltaSetIndex(0, 0x10000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ParseMap(mapBytes).GetEntry(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ParseMap(mapBytes).Resolve(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ParseMap(mapBytes).Resolve((long)uint.MaxValue + 1));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ParsesRegionsSharedNullDataAndMixedDeltaWidths(bool longWords)
    {
        SfntItemVariationStore store = ParseStore(BuildStore(longWords));
        SfntVariationRegion positive = store.GetRegion(0);
        SfntVariationRegion negative = store.GetRegion(1);
        SfntItemVariationData first = store.GetVariationData(0);

        Assert.Multiple(() =>
        {
            Assert.That(store.GetOffset(), Is.EqualTo(5));
            Assert.That(store.GetFormat(), Is.EqualTo(1));
            Assert.That(store.GetAxisCount(), Is.EqualTo(2));
            Assert.That(store.GetRegionCount(), Is.EqualTo(2));
            Assert.That(positive.GetAxisCount(), Is.EqualTo(2));
            Assert.That(positive.GetAxis(0).GetStart(), Is.Zero);
            Assert.That(positive.GetAxis(0).GetPeak(), Is.EqualTo(1));
            Assert.That(positive.GetAxis(0).GetEnd(), Is.EqualTo(1));
            Assert.That(positive.GetAxis(1).GetPeak(), Is.Zero);
            Assert.That(negative.GetAxis(0).GetPeak(), Is.Zero);
            Assert.That(negative.GetAxis(1).GetStart(), Is.EqualTo(-1));
            Assert.That(negative.GetAxis(1).GetPeak(), Is.EqualTo(-1));
            Assert.That(negative.GetAxis(1).GetEnd(), Is.Zero);
            Assert.That(store.GetVariationDataCount(), Is.EqualTo(3));
            Assert.That(first.GetItemCount(), Is.EqualTo(2));
            Assert.That(first.GetWordDeltaCount(), Is.EqualTo(1));
            Assert.That(first.UsesLongWords(), Is.EqualTo(longWords));
            Assert.That(first.GetRegionIndexCount(), Is.EqualTo(2));
            Assert.That(first.GetRegionIndex(0), Is.Zero);
            Assert.That(first.GetRegionIndex(1), Is.EqualTo(1));
            Assert.That(first.GetDelta(0, 0),
                Is.EqualTo(longWords ? 100000 : 1000));
            Assert.That(first.GetDelta(0, 1),
                Is.EqualTo(longWords ? -1000 : -10));
            Assert.That(first.GetDelta(1, 0),
                Is.EqualTo(longWords ? -50000 : -500));
            Assert.That(first.GetDelta(1, 1),
                Is.EqualTo(longWords ? 2000 : 20));
            Assert.That(store.GetVariationData(1), Is.Null);
            Assert.That(store.GetVariationData(2), Is.SameAs(first));
        });
    }

    [Test]
    public void PrecomputesRegionScalarsAndEvaluatesDeltaSets()
    {
        SfntItemVariationStore store = ParseStore(BuildStore(false));
        SfntVariationLocation location = store.CreateLocation(
            new double[] { 0.5, -0.25 });

        Assert.Multiple(() =>
        {
            Assert.That(location.CalculateDelta(new SfntDeltaSetIndex(0, 0)),
                Is.EqualTo(497.5));
            Assert.That(location.CalculateDelta(new SfntDeltaSetIndex(0, 1)),
                Is.EqualTo(-245));
            Assert.That(location.CalculateDelta(new SfntDeltaSetIndex(1, 500)),
                Is.Zero);
            Assert.That(location.CalculateDelta(
                new SfntDeltaSetIndex(0xFFFF, 0xFFFF)), Is.Zero);
            Assert.Throws<FontFormatException>(() => location.CalculateDelta(
                new SfntDeltaSetIndex(0, 2)));
            Assert.Throws<FontFormatException>(() => location.CalculateDelta(
                new SfntDeltaSetIndex(3, 0)));
        });
    }

    [Test]
    public void EvaluatesGdefAndGposVariationIndexesAtSharedLocation()
    {
        SfntItemVariationStore store = ParseStore(BuildStore(false));
        SfntVariationLocation location = store.CreateLocation(
            new double[] { 0.5, -0.25 });
        SfntVariationIndex gdefCaret = new SfntVariationIndex(0, 0);
        SfntVariationIndex gposAdjustment = new SfntVariationIndex(0, 1);

        Assert.Multiple(() =>
        {
            Assert.That(gdefCaret.GetOuterIndex(), Is.Zero);
            Assert.That(gdefCaret.GetInnerIndex(), Is.Zero);
            Assert.That(gdefCaret.CalculateDelta(location), Is.EqualTo(497.5));
            Assert.That(gposAdjustment.CalculateDelta(location), Is.EqualTo(-245));
            Assert.That(gposAdjustment.GetDeltaSetIndex().GetInnerIndex(),
                Is.EqualTo(1));
            Assert.Throws<ArgumentNullException>(() =>
                gdefCaret.CalculateDelta(null!));
            Assert.Throws<FontFormatException>(() =>
                new SfntVariationIndex(0, 2).CalculateDelta(location));
        });
    }

    [Test]
    public void ValidatesVariationLocationsAndModelAccessors()
    {
        SfntItemVariationStore store = ParseStore(BuildStore(false));
        SfntItemVariationData data = store.GetVariationData(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => store.CreateLocation(null!));
            Assert.Throws<ArgumentException>(() =>
                store.CreateLocation(new double[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.CreateLocation(new double[] { double.NaN, 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.CreateLocation(new double[] { 0, 1.1 }));
            Assert.Throws<ArgumentNullException>(() =>
                store.ValidateDeltaSetIndex(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRegion(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.GetVariationData(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.GetRegion(0).GetAxis(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetRegionIndex(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetDelta(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetDelta(0, 2));
        });
    }

    [TestCaseSource(nameof(InvalidStores))]
    public void RejectsMalformedVariationStores(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseStore(bytes));
    }

    [Test]
    public void EnforcesStoreLimitsAndParserArguments()
    {
        byte[] bytes = BuildStore(false);
        FontData data = FontData.Create(bytes);
        FontDataSlice slice = data.GetSlice(0, data.GetLength());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    5,
                    2,
                    FontLimits.CreateCompatible(),
                    new SfntVariationParsingBudget(2)));
            Assert.Throws<FontLimitException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    5,
                    2,
                    CreateLimits(100, 1),
                    new SfntVariationParsingBudget(100)));
            Assert.Throws<ArgumentNullException>(() =>
                SfntItemVariationStoreParser.Parse(
                    null!,
                    0,
                    0,
                    0,
                    FontLimits.CreateCompatible(),
                    new SfntVariationParsingBudget(10)));
            Assert.Throws<ArgumentNullException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    5,
                    2,
                    null!,
                    new SfntVariationParsingBudget(10)));
            Assert.Throws<ArgumentNullException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    5,
                    2,
                    FontLimits.CreateCompatible(),
                    null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    -1,
                    2,
                    FontLimits.CreateCompatible(),
                    new SfntVariationParsingBudget(100)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntItemVariationStoreParser.Parse(
                    slice,
                    5,
                    5,
                    -1,
                    FontLimits.CreateCompatible(),
                    new SfntVariationParsingBudget(100)));
        });
    }

    static IEnumerable<TestCaseData> InvalidIndexMaps()
    {
        byte[] invalidFormat = BuildMap(0, 0, new uint[] { 0 });
        invalidFormat[3] = 2;
        yield return InvalidCase(invalidFormat, "IndexMapFormat");

        byte[] reservedBits = BuildMap(0, 0, new uint[] { 0 });
        reservedBits[4] = 0xC0;
        yield return InvalidCase(reservedBits, "IndexMapReservedBits");

        byte[] impossibleBits = BuildMap(0, 0, new uint[] { 0 });
        impossibleBits[4] = 0x0F;
        yield return InvalidCase(impossibleBits, "IndexMapImpossibleBits");

        byte[] truncated = BuildMap(1, 0x11, new uint[] { 1, 2 });
        yield return InvalidCase(truncated[..^1], "IndexMapTruncatedEntries");

        TestBinaryBuilder oversizedOuter = new TestBinaryBuilder()
            .WriteZeros(3)
            .WriteUInt8(0)
            .WriteUInt8(0x20)
            .WriteUInt16(1)
            .WriteUInt24(0x020000);
        yield return InvalidCase(oversizedOuter.CopyBytes(), "IndexMapOuterIndex");
    }

    static IEnumerable<TestCaseData> InvalidStores()
    {
        byte[] valid = BuildStore(false);
        yield return InvalidCase(valid[..12], "StoreTruncatedHeader");

        byte[] invalidFormat = BuildStore(false);
        PatchUInt16(invalidFormat, 5, 2);
        yield return InvalidCase(invalidFormat, "StoreFormat");

        byte[] regionListInHeader = BuildStore(false);
        PatchUInt32(regionListInHeader, 7, 12);
        yield return InvalidCase(regionListInHeader, "RegionListInHeader");

        byte[] dataInHeader = BuildStore(false);
        PatchUInt32(dataInHeader, 13, 12);
        yield return InvalidCase(dataInHeader, "VariationDataInHeader");

        byte[] axisMismatch = BuildStore(false);
        PatchUInt16(axisMismatch, 25, 1);
        yield return InvalidCase(axisMismatch, "RegionAxisCountMismatch");

        byte[] reservedRegionCount = BuildStore(false);
        PatchUInt16(reservedRegionCount, 27, 0x8000);
        yield return InvalidCase(reservedRegionCount, "ReservedRegionCountBit");

        byte[] invalidRegion = BuildStore(false);
        PatchInt16(invalidRegion, 29, F2Dot14(1));
        PatchInt16(invalidRegion, 31, F2Dot14(0));
        yield return InvalidCase(invalidRegion, "RegionCoordinates");

        byte[] invalidWordCount = BuildStore(false);
        PatchUInt16(invalidWordCount, 55, 3);
        yield return InvalidCase(invalidWordCount, "WordDeltaCount");

        byte[] missingRegion = BuildStore(false);
        PatchUInt16(missingRegion, 59, 2);
        yield return InvalidCase(missingRegion, "MissingRegion");

        yield return InvalidCase(valid[..^1], "TruncatedDeltas");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntDeltaSetIndexMap ParseMap(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return SfntDeltaSetIndexMapParser.Parse(
            data.GetSlice(0, data.GetLength()),
            3,
            3,
            new SfntVariationParsingBudget(100));
    }

    static byte[] BuildMap(
        int format,
        int entryFormat,
        uint[] entries)
    {
        int entrySize = ((entryFormat & 0x30) >> 4) + 1;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteZeros(3)
            .WriteUInt8((byte)format)
            .WriteUInt8((byte)entryFormat);

        if (format == 0)
        {
            builder.WriteUInt16((ushort)entries.Length);
        }
        else
        {
            builder.WriteUInt32((uint)entries.Length);
        }

        int index = 0;

        while (index < entries.Length)
        {
            WriteMapEntry(builder, entries[index], entrySize);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static void WriteMapEntry(
        TestBinaryBuilder builder,
        uint value,
        int entrySize)
    {
        if (entrySize == 1)
        {
            builder.WriteUInt8((byte)value);
        }
        else if (entrySize == 2)
        {
            builder.WriteUInt16((ushort)value);
        }
        else if (entrySize == 3)
        {
            builder.WriteUInt24(value);
        }
        else
        {
            builder.WriteUInt32(value);
        }
    }

    static uint EncodeIndex(int outer, int inner, int innerBitCount)
    {
        return ((uint)outer << innerBitCount) | (uint)inner;
    }

    static SfntItemVariationStore ParseStore(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return SfntItemVariationStoreParser.Parse(
            data.GetSlice(0, data.GetLength()),
            5,
            5,
            2,
            FontLimits.CreateCompatible(),
            new SfntVariationParsingBudget(100));
    }

    static byte[] BuildStore(bool longWords)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteZeros(5)
            .WriteUInt16(1)
            .WriteUInt32(20)
            .WriteUInt16(3)
            .WriteUInt32(48)
            .WriteUInt32(0)
            .WriteUInt32(48)
            .WriteUInt16(2)
            .WriteUInt16(2);
        WriteRegionAxis(builder, 0, 1, 1);
        WriteRegionAxis(builder, -1, 0, 1);
        WriteRegionAxis(builder, -1, 0, 1);
        WriteRegionAxis(builder, -1, -1, 0);
        builder
            .WriteUInt16(2)
            .WriteUInt16((ushort)(longWords ? 0x8001 : 1))
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(1);

        if (longWords)
        {
            builder
                .WriteInt32(100000)
                .WriteInt16(-1000)
                .WriteInt32(-50000)
                .WriteInt16(2000);
        }
        else
        {
            builder
                .WriteInt16(1000)
                .WriteInt8(-10)
                .WriteInt16(-500)
                .WriteInt8(20);
        }

        return builder.CopyBytes();
    }

    static void WriteRegionAxis(
        TestBinaryBuilder builder,
        double start,
        double peak,
        double end)
    {
        builder
            .WriteInt16(F2Dot14(start))
            .WriteInt16(F2Dot14(peak))
            .WriteInt16(F2Dot14(end));
    }

    static short F2Dot14(double value)
    {
        return System.Convert.ToInt16(System.Math.Round(value * 16384));
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchInt16(byte[] bytes, int offset, short value)
    {
        PatchUInt16(bytes, offset, (ushort)value);
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static FontLimits CreateLimits(
        int maximumCoverageEntries,
        int maximumVariationRegions)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            1000,
            1000,
            1000,
            10,
            10,
            10,
            100,
            10000,
            maximumCoverageEntries,
            10000,
            1000000,
            100,
            maximumVariationRegions,
            10,
            100);
    }
}
