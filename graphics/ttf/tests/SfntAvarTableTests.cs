using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntAvarTableTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");

    [Test]
    public void ParsesVersionOneSegmentMapsInFvarAxisOrder()
    {
        SfntAvarTable? table = Parse(BuildAvar1());
        SfntAvarSegmentMap weight = table!.GetSegmentMap(0);
        SfntAvarSegmentMap width = table.GetSegmentMap(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetSegmentMapCount(), Is.EqualTo(2));
            Assert.That(weight.GetAxisTag(), Is.EqualTo(WeightTag));
            Assert.That(weight.GetValueMapCount(), Is.EqualTo(5));
            Assert.That(weight.HasRequiredMappings(), Is.True);
            Assert.That(weight.GetValueMap(0).GetFromCoordinate(), Is.EqualTo(-1));
            Assert.That(weight.GetValueMap(0).GetToCoordinate(), Is.EqualTo(-1));
            Assert.That(weight.GetValueMap(1).GetFromCoordinate(), Is.EqualTo(-0.5));
            Assert.That(weight.GetValueMap(1).GetToCoordinate(), Is.EqualTo(-0.25));
            Assert.That(weight.GetValueMap(3).GetFromCoordinate(), Is.EqualTo(0.5));
            Assert.That(weight.GetValueMap(3).GetToCoordinate(), Is.EqualTo(0.75));
            Assert.That(width.GetAxisTag(), Is.EqualTo(WidthTag));
            Assert.That(width.GetValueMapCount(), Is.Zero);
            Assert.That(width.HasRequiredMappings(), Is.True);
            Assert.That(table.GetAxisIndexMapOffset(), Is.Zero);
            Assert.That(table.GetVariationStoreOffset(), Is.Zero);
            Assert.That(table.GetAxisIndexMap(), Is.Null);
            Assert.That(table.GetVariationStore(), Is.Null);
        });
    }

    [Test]
    public void RetainsSegmentMapWithoutRequiredAnchorsAsInactive()
    {
        byte[] bytes = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteInt16(F2Dot14(-1))
            .WriteInt16(F2Dot14(-1))
            .WriteInt16(F2Dot14(1))
            .WriteInt16(F2Dot14(1))
            .WriteUInt16(0)
            .CopyBytes();
        SfntAvarTable? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetSegmentMap(0).GetValueMapCount(), Is.EqualTo(2));
            Assert.That(table.GetSegmentMap(0).HasRequiredMappings(), Is.False);
            Assert.That(table.GetSegmentMap(1).HasRequiredMappings(), Is.True);
        });
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    public void ParsesVersionTwoIndexMapRegionsAndDeltaSets(
        int mapFormat,
        bool usesLongWords)
    {
        SfntAvarTable? table = Parse(BuildAvar2(
            mapFormat,
            usesLongWords,
            true,
            true));
        SfntDeltaSetIndexMap? indexMap = table!.GetAxisIndexMap();
        SfntItemVariationStore? store = table.GetVariationStore();
        SfntVariationRegion firstRegion = store!.GetRegion(0);
        SfntItemVariationData data = store.GetVariationData(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(2));
            Assert.That(table.GetSegmentMapCount(), Is.EqualTo(2));
            Assert.That(table.GetAxisIndexMapOffset(), Is.EqualTo(40));
            Assert.That(table.GetVariationStoreOffset(),
                Is.EqualTo(mapFormat == 0 ? 48 : 50));
            Assert.That(indexMap!.GetOffset(), Is.EqualTo(40));
            Assert.That(indexMap.GetFormat(), Is.EqualTo(mapFormat));
            Assert.That(indexMap.GetEntryFormat(), Is.EqualTo(0x17));
            Assert.That(indexMap.GetEntrySize(), Is.EqualTo(2));
            Assert.That(indexMap.GetInnerIndexBitCount(), Is.EqualTo(8));
            Assert.That(indexMap.GetEntryCount(), Is.EqualTo(2));
            Assert.That(indexMap.GetEntry(0).GetOuterIndex(), Is.Zero);
            Assert.That(indexMap.GetEntry(0).GetInnerIndex(), Is.EqualTo(1));
            Assert.That(indexMap.Resolve(8).GetInnerIndex(), Is.Zero);
            Assert.That(table.GetVariationIndexForAxis(0).GetInnerIndex(), Is.EqualTo(1));
            Assert.That(table.GetVariationIndexForAxis(1).GetInnerIndex(), Is.Zero);
            Assert.That(store.GetFormat(), Is.EqualTo(1));
            Assert.That(store.GetAxisCount(), Is.EqualTo(2));
            Assert.That(store.GetRegionCount(), Is.EqualTo(2));
            Assert.That(firstRegion.GetAxisCount(), Is.EqualTo(2));
            Assert.That(firstRegion.GetAxis(0).GetStart(), Is.Zero);
            Assert.That(firstRegion.GetAxis(0).GetPeak(), Is.EqualTo(1));
            Assert.That(firstRegion.GetAxis(0).GetEnd(), Is.EqualTo(1));
            Assert.That(firstRegion.GetAxis(1).GetPeak(), Is.Zero);
            Assert.That(store.GetRegion(1).GetAxis(1).GetStart(), Is.EqualTo(-1));
            Assert.That(store.GetRegion(1).GetAxis(1).GetPeak(), Is.EqualTo(-1));
            Assert.That(store.GetRegion(1).GetAxis(1).GetEnd(), Is.Zero);
            Assert.That(store.GetVariationDataCount(), Is.EqualTo(1));
            Assert.That(data.GetItemCount(), Is.EqualTo(2));
            Assert.That(data.GetWordDeltaCount(), Is.EqualTo(1));
            Assert.That(data.UsesLongWords(), Is.EqualTo(usesLongWords));
            Assert.That(data.GetRegionIndexCount(), Is.EqualTo(2));
            Assert.That(data.GetRegionIndex(0), Is.Zero);
            Assert.That(data.GetRegionIndex(1), Is.EqualTo(1));
            Assert.That(data.GetDelta(0, 0), Is.EqualTo(usesLongWords ? 16384 : 1638));
            Assert.That(data.GetDelta(0, 1), Is.EqualTo(-5));
            Assert.That(data.GetDelta(1, 0), Is.EqualTo(usesLongWords ? -8192 : -819));
            Assert.That(data.GetDelta(1, 1), Is.EqualTo(10));
        });
    }

    [Test]
    public void ParsesVersionTwoWithNoSegmentMapsOrOptionalStructures()
    {
        SfntAvarTable? table = Parse(BuildAvar2(0, false, false, false));
        SfntDeltaSetIndex identity = table!.GetVariationIndexForAxis(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetSegmentMapCount(), Is.Zero);
            Assert.That(table.GetAxisIndexMapOffset(), Is.Zero);
            Assert.That(table.GetVariationStoreOffset(), Is.Zero);
            Assert.That(table.GetAxisIndexMap(), Is.Null);
            Assert.That(table.GetVariationStore(), Is.Null);
            Assert.That(identity.GetOuterIndex(), Is.Zero);
            Assert.That(identity.GetInnerIndex(), Is.EqualTo(1));
            Assert.That(identity.HasVariationData(), Is.True);
        });
    }

    [Test]
    public void AllowsEitherVersionTwoOptionalStructureToBeOmitted()
    {
        byte[] mapOnlyBytes = BuildAvar2(0, false, true, true);
        PatchUInt32(mapOnlyBytes, 36, 0);
        byte[] storeOnlyBytes = BuildAvar2(0, false, true, true);
        PatchUInt32(storeOnlyBytes, 32, 0);
        SfntAvarTable? mapOnly = Parse(mapOnlyBytes);
        SfntAvarTable? storeOnly = Parse(storeOnlyBytes);

        Assert.Multiple(() =>
        {
            Assert.That(mapOnly!.GetAxisIndexMap(), Is.Not.Null);
            Assert.That(mapOnly.GetVariationStore(), Is.Null);
            Assert.That(storeOnly!.GetAxisIndexMap(), Is.Null);
            Assert.That(storeOnly.GetVariationStore(), Is.Not.Null);
            Assert.That(storeOnly.GetVariationIndexForAxis(1).GetOuterIndex(), Is.Zero);
            Assert.That(storeOnly.GetVariationIndexForAxis(1).GetInnerIndex(), Is.EqualTo(1));
        });
    }

    [TestCaseSource(nameof(InvalidAvarTables))]
    public void RejectsMalformedAvarTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RequiresFvarAndEnforcesEntryAndRegionLimits()
    {
        SfntTableCache missingFvar = CreateCache(
            BuildAvar1(),
            null,
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache entryLimited = CreateCache(
            BuildAvar1(),
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            CreateLimits(6, 100));
        SfntTableCache regionLimited = CreateCache(
            BuildAvar2(0, false, true, true),
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            CreateLimits(100, 1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntAvarTableParser.FindTable(missingFvar));
            Assert.Throws<FontLimitException>(() =>
                SfntAvarTableParser.FindTable(entryLimited));
            Assert.Throws<FontLimitException>(() =>
                SfntAvarTableParser.FindTable(regionLimited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndAppliesVersionGating()
    {
        byte[] futureMinor = BuildAvar1();
        PatchUInt16(futureMinor, 2, 1);
        SfntParsedTableView? compatible = SfntAvarTableParser.FindView(CreateCache(
            futureMinor,
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntAvarTableParser.FindView(CreateCache(
            futureMinor,
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Strict,
            FontLimits.CreateCompatible()));
        SfntTableCache cache = CreateCache(
            BuildAvar1(),
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknownMajor = BuildAvar1();
        PatchUInt16(unknownMajor, 0, 3);
        SfntParsedTableView? raw = SfntAvarTableParser.FindView(CreateCache(
            unknownMajor,
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(compatible!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(face.GetAvarTable(), Is.Not.Null);
            Assert.That(face.GetAvarTableView()!.GetParsedData(),
                Is.SameAs(face.GetAvarTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntAvarTableParser.FindView(absent), Is.Null);
            Assert.That(SfntAvarTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntAvarTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesModelAccessorIndexes()
    {
        SfntAvarTable? table = Parse(BuildAvar2(0, false, true, true));
        SfntItemVariationStore? store = table!.GetVariationStore();
        SfntItemVariationData data = store!.GetVariationData(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetSegmentMap(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetSegmentMap(0).GetValueMap(5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetAxisIndexMap()!.GetEntry(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetAxisIndexMap()!.Resolve(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetVariationIndexForAxis(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRegion(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.GetRegion(0).GetAxis(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.GetVariationData(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetRegionIndex(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetDelta(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetDelta(0, 2));
        });
    }

    static IEnumerable<TestCaseData> InvalidAvarTables()
    {
        byte[] valid1 = BuildAvar1();
        yield return InvalidCase(valid1[..7], "TruncatedHeader");

        byte[] reserved = BuildAvar1();
        PatchUInt16(reserved, 4, 1);
        yield return InvalidCase(reserved, "ReservedField");

        byte[] countMismatch = BuildAvar1();
        PatchUInt16(countMismatch, 6, 1);
        yield return InvalidCase(countMismatch, "VersionOneAxisCountMismatch");

        byte[] truncatedMap = BuildAvar1();
        PatchUInt16(truncatedMap, 8, 20);
        yield return InvalidCase(truncatedMap, "TruncatedSegmentMap");

        byte[] outOfRange = BuildAvar1();
        PatchUInt16(outOfRange, 10, 0x6000);
        yield return InvalidCase(outOfRange, "CoordinateOutsideNormalizedRange");

        byte[] duplicateFrom = BuildAvar1();
        PatchInt16(duplicateFrom, 14, F2Dot14(-1));
        yield return InvalidCase(duplicateFrom, "DuplicateFromCoordinate");

        byte[] retrogradeTo = BuildAvar1();
        PatchInt16(retrogradeTo, 20, F2Dot14(-0.5));
        yield return InvalidCase(retrogradeTo, "RetrogradeToCoordinate");

        byte[] valid2 = BuildAvar2(0, false, true, true);
        yield return InvalidCase(valid2[..39], "TruncatedVersionTwoTail");

        byte[] v2CountMismatch = BuildAvar2(0, false, true, true);
        PatchUInt16(v2CountMismatch, 6, 1);
        yield return InvalidCase(v2CountMismatch, "VersionTwoAxisCountMismatch");

        byte[] mapIntoTail = BuildAvar2(0, false, true, true);
        PatchUInt32(mapIntoTail, 32, 36);
        yield return InvalidCase(mapIntoTail, "AxisIndexMapOffsetIntoTail");

        byte[] storePastTable = BuildAvar2(0, false, true, true);
        PatchUInt32(storePastTable, 36, (uint)storePastTable.Length);
        yield return InvalidCase(storePastTable, "VariationStoreOffsetPastTable");

        byte[] sharedStructureOffset = BuildAvar2(0, false, true, true);
        PatchUInt32(sharedStructureOffset, 32, 48);
        yield return InvalidCase(sharedStructureOffset, "SharedVersionTwoStructureOffset");

        byte[] invalidMapFormat = BuildAvar2(0, false, true, true);
        invalidMapFormat[40] = 2;
        yield return InvalidCase(invalidMapFormat, "AxisIndexMapFormat");

        byte[] reservedEntryFormat = BuildAvar2(0, false, true, true);
        reservedEntryFormat[41] = 0xC0;
        yield return InvalidCase(reservedEntryFormat, "ReservedEntryFormatBits");

        byte[] impossibleEntryFormat = BuildAvar2(0, false, true, true);
        impossibleEntryFormat[41] = 0x0F;
        yield return InvalidCase(impossibleEntryFormat, "ImpossibleEntryFormat");

        byte[] truncatedIndexMap = BuildAvar2(0, false, true, true);
        PatchUInt16(truncatedIndexMap, 42, 1000);
        yield return InvalidCase(truncatedIndexMap, "TruncatedAxisIndexMap");

        byte[] invalidStoreFormat = BuildAvar2(0, false, true, true);
        PatchUInt16(invalidStoreFormat, 48, 2);
        yield return InvalidCase(invalidStoreFormat, "VariationStoreFormat");

        byte[] storeAxisMismatch = BuildAvar2(0, false, true, true);
        PatchUInt16(storeAxisMismatch, 60, 1);
        yield return InvalidCase(storeAxisMismatch, "VariationStoreAxisCountMismatch");

        byte[] invalidRegion = BuildAvar2(0, false, true, true);
        PatchInt16(invalidRegion, 64, F2Dot14(1));
        PatchInt16(invalidRegion, 66, F2Dot14(0));
        yield return InvalidCase(invalidRegion, "InvalidVariationRegion");

        byte[] invalidWordCount = BuildAvar2(0, false, true, true);
        PatchUInt16(invalidWordCount, 90, 3);
        yield return InvalidCase(invalidWordCount, "InvalidWordDeltaCount");

        byte[] missingRegion = BuildAvar2(0, false, true, true);
        PatchUInt16(missingRegion, 94, 2);
        yield return InvalidCase(missingRegion, "MissingVariationRegion");

        byte[] missingDeltaSet = BuildAvar2(0, false, true, true);
        PatchUInt16(missingDeltaSet, 44, 2);
        yield return InvalidCase(missingDeltaSet, "MissingDeltaSet");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntAvarTable? Parse(byte[] avar)
    {
        return SfntAvarTableParser.FindTable(CreateCache(
            avar,
            BuildFvar(),
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? avar,
        byte[]? fvar,
        byte[] name,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (avar != null)
        {
            tables.Add(CreateTable(FontTags.Avar, avar));
        }

        if (fvar != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, fvar));
        }

        if (name != null)
        {
            tables.Add(CreateTable(FontTags.Name, name));
        }

        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            mode,
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

    static byte[] BuildAvar1()
    {
        TestBinaryBuilder builder = WriteAvarHeader(1, 2);
        WriteWeightSegmentMap(builder);
        builder.WriteUInt16(0);
        return builder.CopyBytes();
    }

    static byte[] BuildAvar2(
        int mapFormat,
        bool usesLongWords,
        bool includeSegmentMaps,
        bool includeVariationData)
    {
        TestBinaryBuilder builder = WriteAvarHeader(
            2,
            includeSegmentMaps ? 2 : 0);

        if (includeSegmentMaps)
        {
            WriteWeightSegmentMap(builder);
            builder.WriteUInt16(0);
        }

        int axisIndexMapOffsetField = builder.GetLength();
        builder.WriteUInt32(0);
        int variationStoreOffsetField = builder.GetLength();
        builder.WriteUInt32(0);

        if (!includeVariationData)
        {
            return builder.CopyBytes();
        }

        int axisIndexMapOffset = builder.GetLength();
        builder.PatchUInt32(axisIndexMapOffsetField, (uint)axisIndexMapOffset);
        builder.WriteUInt8((byte)mapFormat).WriteUInt8(0x17);

        if (mapFormat == 0)
        {
            builder.WriteUInt16(2);
        }
        else
        {
            builder.WriteUInt32(2);
        }

        builder.WriteUInt16(1).WriteUInt16(0);
        int storeOffset = builder.GetLength();
        builder.PatchUInt32(variationStoreOffsetField, (uint)storeOffset);
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(40)
            .WriteUInt16(2)
            .WriteUInt16(2);
        WriteRegionAxis(builder, 0, 1, 1);
        WriteRegionAxis(builder, 0, 0, 0);
        WriteRegionAxis(builder, 0, 0, 0);
        WriteRegionAxis(builder, -1, -1, 0);
        builder
            .WriteUInt16(2)
            .WriteUInt16((ushort)(usesLongWords ? 0x8001 : 1))
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(1);

        if (usesLongWords)
        {
            builder
                .WriteInt32(16384)
                .WriteInt16(-5)
                .WriteInt32(-8192)
                .WriteInt16(10);
        }
        else
        {
            builder
                .WriteInt16(1638)
                .WriteInt8(-5)
                .WriteInt16(-819)
                .WriteInt8(10);
        }

        return builder.CopyBytes();
    }

    static TestBinaryBuilder WriteAvarHeader(int majorVersion, int mapCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)majorVersion)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)mapCount);
    }

    static void WriteWeightSegmentMap(TestBinaryBuilder builder)
    {
        builder.WriteUInt16(5);
        WriteValueMap(builder, -1, -1);
        WriteValueMap(builder, -0.5, -0.25);
        WriteValueMap(builder, 0, 0);
        WriteValueMap(builder, 0.5, 0.75);
        WriteValueMap(builder, 1, 1);
    }

    static void WriteValueMap(
        TestBinaryBuilder builder,
        double fromCoordinate,
        double toCoordinate)
    {
        builder
            .WriteInt16(F2Dot14(fromCoordinate))
            .WriteInt16(F2Dot14(toCoordinate));
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

    static byte[] BuildFvar()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(12);
        WriteFvarAxis(builder, "wght", 100, 400, 900, 256);
        WriteFvarAxis(builder, "wdth", 50, 100, 200, 257);
        return builder.CopyBytes();
    }

    static void WriteFvarAxis(
        TestBinaryBuilder builder,
        string tag,
        double minimum,
        double defaultValue,
        double maximum,
        int nameId)
    {
        builder
            .WriteTag(tag)
            .WriteInt32(Fixed(minimum))
            .WriteInt32(Fixed(defaultValue))
            .WriteInt32(Fixed(maximum))
            .WriteUInt16(0)
            .WriteUInt16((ushort)nameId);
    }

    static byte[] BuildNameTable()
    {
        string[] values = new string[] { "Weight", "Width" };
        byte[][] encoded = new byte[][]
        {
            Encoding.BigEndianUnicode.GetBytes(values[0]),
            Encoding.BigEndianUnicode.GetBytes(values[1])
        };
        int storageOffset = 6 + values.Length * 12;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16((ushort)storageOffset)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(256)
            .WriteUInt16((ushort)encoded[0].Length)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(257)
            .WriteUInt16((ushort)encoded[1].Length)
            .WriteUInt16((ushort)encoded[0].Length)
            .WriteBytes(encoded[0])
            .WriteBytes(encoded[1]);
        return builder.CopyBytes();
    }

    static short F2Dot14(double value)
    {
        return System.Convert.ToInt16(System.Math.Round(value * 16384.0));
    }

    static int Fixed(double value)
    {
        return System.Convert.ToInt32(System.Math.Round(value * 65536.0));
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
