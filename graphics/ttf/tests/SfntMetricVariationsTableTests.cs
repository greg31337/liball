using ttf;

namespace ttf.Tests;

internal class SfntMetricVariationsTableTests
{
    [Test]
    public void ParsesHvarImplicitAdvanceAndSideBearingMaps()
    {
        SfntTableCache cache = CreateCache(
            BuildMetricVariations(false, false),
            false,
            true,
            true);
        SfntParsedTableView? view =
            SfntMetricVariationsTableParser.FindHvarView(cache);
        SfntMetricVariationsTable? table =
            SfntMetricVariationsTableParser.FindHvarTable(cache);
        SfntMetricVariationsLocation location = table!.CreateLocation(
            new double[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(view!.HasParsedData(), Is.True);
            Assert.That(table.GetTag(), Is.EqualTo(FontTags.Hvar));
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetAdvanceMapping(), Is.Null);
            Assert.That(table.GetLeadingBearingMapping()!.GetEntryCount(),
                Is.EqualTo(2));
            Assert.That(table.GetTrailingBearingMapping()!.GetEntryCount(),
                Is.EqualTo(1));
            Assert.That(table.GetVerticalOriginMapping(), Is.Null);
            Assert.That(location.CalculateAdvanceDelta(0), Is.EqualTo(5));
            Assert.That(location.CalculateAdvanceDelta(2), Is.EqualTo(15));
            Assert.That(location.CalculateLeadingBearingDelta(0), Is.EqualTo(20));
            Assert.That(location.CalculateLeadingBearingDelta(2), Is.EqualTo(25));
            Assert.That(location.CalculateTrailingBearingDelta(2), Is.EqualTo(30));
            Assert.That(location.CalculateVerticalOriginDelta(1), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                location.CalculateAdvanceDelta(3));
        });
    }

    [Test]
    public void ParsesVvarExplicitMapsAndRepeatsLastMapEntry()
    {
        SfntTableCache cache = CreateCache(
            BuildMetricVariations(true, true),
            true,
            true,
            true);
        SfntMetricVariationsTable? table =
            SfntMetricVariationsTableParser.FindVvarTable(cache);
        SfntMetricVariationsLocation location = table!.CreateLocation(
            new double[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetTag(), Is.EqualTo(FontTags.Vvar));
            Assert.That(table.GetAdvanceMapping()!.GetEntryCount(), Is.EqualTo(2));
            Assert.That(table.GetVerticalOriginMapping()!.GetEntryCount(),
                Is.EqualTo(3));
            Assert.That(location.CalculateAdvanceDelta(0), Is.EqualTo(15));
            Assert.That(location.CalculateAdvanceDelta(2), Is.EqualTo(10));
            Assert.That(location.CalculateLeadingBearingDelta(2), Is.EqualTo(25));
            Assert.That(location.CalculateTrailingBearingDelta(2), Is.EqualTo(30));
            Assert.That(location.CalculateVerticalOriginDelta(0), Is.EqualTo(35));
            Assert.That(location.CalculateVerticalOriginDelta(2), Is.EqualTo(45));
        });
    }

    [Test]
    public void PreservesCompatibleMinorAndRawUnknownMajorVersions()
    {
        byte[] compatible = BuildMetricVariations(false, false);
        PatchUInt16(compatible, 2, 3);
        SfntParsedTableView? compatibleView =
            SfntMetricVariationsTableParser.FindHvarView(CreateCache(
                compatible,
                false,
                true,
                true));
        byte[] unknown = BuildMetricVariations(false, false);
        PatchUInt16(unknown, 0, 2);
        SfntParsedTableView? unknownView =
            SfntMetricVariationsTableParser.FindHvarView(CreateCache(
                unknown,
                false,
                true,
                true));

        Assert.Multiple(() =>
        {
            Assert.That(compatibleView!.HasParsedData(), Is.True);
            Assert.That(compatibleView.GetVersion().GetMinorVersion(),
                Is.EqualTo(3));
            Assert.That(unknownView!.HasParsedData(), Is.False);
            Assert.That(unknownView.GetVersion().GetMajorVersion(),
                Is.EqualTo(2));
        });
    }

    [TestCaseSource(nameof(InvalidMetricVariationTables))]
    public void RejectsMalformedMetricVariationTables(
        byte[] bytes,
        bool vertical)
    {
        SfntTableCache cache = CreateCache(bytes, vertical, true, true);

        Assert.Throws<FontFormatException>(() => FindTable(cache, vertical));
    }

    [Test]
    public void RequiresVariationAndDirectionSpecificMetricTables()
    {
        byte[] hvar = BuildMetricVariations(false, false);
        byte[] vvar = BuildMetricVariations(true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntMetricVariationsTableParser.FindHvarTable(
                    CreateCache(hvar, false, false, true)));
            Assert.Throws<FontFormatException>(() =>
                SfntMetricVariationsTableParser.FindHvarTable(
                    CreateCache(hvar, false, true, false)));
            Assert.Throws<FontFormatException>(() =>
                SfntMetricVariationsTableParser.FindVvarTable(
                    CreateCache(vvar, true, false, true)));
        });
    }

    static IEnumerable<TestCaseData> InvalidMetricVariationTables()
    {
        byte[] hvar = BuildMetricVariations(false, false);
        yield return InvalidCase(hvar[..19], false, "TruncatedHvarHeader");

        byte[] badStoreOffset = BuildMetricVariations(false, false);
        PatchUInt32(badStoreOffset, 4, 4);
        yield return InvalidCase(badStoreOffset, false, "StoreInHeader");

        byte[] unpairedSideMaps = BuildMetricVariations(false, false);
        PatchUInt32(unpairedSideMaps, 12, 0);
        yield return InvalidCase(unpairedSideMaps, false, "UnpairedSideMaps");

        byte[] wrongMapFormat = BuildMetricVariations(true, true);
        int advanceOffset = ReadUInt32(wrongMapFormat, 8);
        wrongMapFormat[advanceOffset] = 1;
        yield return InvalidCase(wrongMapFormat, true, "FormatOneMap");

        byte[] emptyMap = BuildMetricVariations(true, true);
        advanceOffset = ReadUInt32(emptyMap, 8);
        PatchUInt16(emptyMap, advanceOffset + 2, 0);
        yield return InvalidCase(emptyMap, true, "EmptyPresentMap");

        byte[] excessiveMap = BuildMetricVariations(true, true);
        advanceOffset = ReadUInt32(excessiveMap, 8);
        PatchUInt16(excessiveMap, advanceOffset + 2, 4);
        yield return InvalidCase(excessiveMap, true, "ExcessiveMapEntries");

        byte[] missingDelta = BuildMetricVariations(true, true);
        advanceOffset = ReadUInt32(missingDelta, 8);
        missingDelta[advanceOffset + 4] = 15;
        yield return InvalidCase(missingDelta, true, "MissingDeltaSet");
    }

    static TestCaseData InvalidCase(
        byte[] bytes,
        bool vertical,
        string name)
    {
        return new TestCaseData(bytes, vertical).SetName("Rejects" + name);
    }

    static SfntMetricVariationsTable? FindTable(
        SfntTableCache cache,
        bool vertical)
    {
        return vertical ?
            SfntMetricVariationsTableParser.FindVvarTable(cache) :
            SfntMetricVariationsTableParser.FindHvarTable(cache);
    }

    static byte[] BuildMetricVariations(
        bool vertical,
        bool includeAdvanceMap)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0);

        if (vertical)
        {
            builder.WriteUInt32(0);
        }

        int advanceOffset = 0;
        int leadingOffset;
        int trailingOffset;
        int originOffset = 0;

        if (includeAdvanceMap)
        {
            advanceOffset = builder.GetLength();
            WriteMap(builder, new byte[] { 2, 1 });
        }

        leadingOffset = builder.GetLength();
        WriteMap(builder, new byte[] { 3, 4 });
        trailingOffset = builder.GetLength();
        WriteMap(builder, new byte[] { 5 });

        if (vertical)
        {
            originOffset = builder.GetLength();
            WriteMap(builder, new byte[] { 6, 7, 8 });
        }

        int storeOffset = builder.GetLength();
        WriteStore(builder);
        byte[] result = builder.CopyBytes();
        PatchUInt32(result, 4, (uint)storeOffset);
        PatchUInt32(result, 8, (uint)advanceOffset);
        PatchUInt32(result, 12, (uint)leadingOffset);
        PatchUInt32(result, 16, (uint)trailingOffset);

        if (vertical)
        {
            PatchUInt32(result, 20, (uint)originOffset);
        }

        return result;
    }

    static void WriteMap(TestBinaryBuilder builder, byte[] innerIndices)
    {
        builder
            .WriteUInt8(0)
            .WriteUInt8(3)
            .WriteUInt16((ushort)innerIndices.Length)
            .WriteBytes(innerIndices);
    }

    static void WriteStore(TestBinaryBuilder builder)
    {
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16(9)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0);

        int delta = 10;

        while (delta <= 90)
        {
            builder.WriteInt16((short)delta);
            delta += 10;
        }
    }

    static SfntTableCache CreateCache(
        byte[] metricVariations,
        bool vertical,
        bool includeVariationTables,
        bool includeMetricTables)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, vertical ? FontTags.Vvar : FontTags.Hvar,
            metricVariations);
        AddTable(tables, FontTags.Maxp, BuildMaxp());

        if (includeVariationTables)
        {
            AddTable(tables, FontTags.Name, BuildName());
            AddTable(tables, FontTags.Fvar, BuildFvar());
        }

        if (includeMetricTables)
        {
            AddTable(tables, vertical ? FontTags.Vhea : FontTags.Hhea,
                BuildMetricHeader());
            AddTable(tables, vertical ? FontTags.Vmtx : FontTags.Hmtx,
                BuildMetrics());
        }

        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16(3)
            .CopyBytes();
    }

    static byte[] BuildName()
    {
        byte[] value = System.Text.Encoding.BigEndianUnicode.GetBytes("Weight");
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(18)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(256)
            .WriteUInt16((ushort)value.Length)
            .WriteUInt16(0)
            .WriteBytes(value)
            .CopyBytes();
    }

    static byte[] BuildFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(8)
            .WriteTag("wght")
            .WriteInt32(0)
            .WriteInt32(0)
            .WriteInt32(0x00010000)
            .WriteUInt16(0)
            .WriteUInt16(256)
            .CopyBytes();
    }

    static byte[] BuildMetricHeader()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(0)
            .WriteUInt16(800).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(3)
            .CopyBytes();
    }

    static byte[] BuildMetrics()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(600).WriteInt16(10)
            .WriteUInt16(700).WriteInt16(20)
            .WriteUInt16(800).WriteInt16(30)
            .CopyBytes();
    }

    static void AddTable(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static int ReadUInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
