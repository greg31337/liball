using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntCpalTableTests
{
    [Test]
    public void ParsesVersionZeroPalettesAndOverlappingColorRecords()
    {
        SfntCpalTable? table = Parse(BuildVersionZero(), null);
        SfntCpalPalette first = table!.GetPalette(0);
        SfntCpalPalette second = table.GetPalette(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.Zero);
            Assert.That(table.GetPaletteEntryCount(), Is.EqualTo(2));
            Assert.That(table.GetPaletteCount(), Is.EqualTo(2));
            Assert.That(table.GetColorRecordCount(), Is.EqualTo(3));
            Assert.That(table.GetDefaultPalette(), Is.SameAs(first));
            Assert.That(first.GetIndex(), Is.Zero);
            Assert.That(first.GetFirstColorRecordIndex(), Is.Zero);
            Assert.That(first.GetEntryCount(), Is.EqualTo(2));
            Assert.That(first.GetColor(0), Is.SameAs(table.GetColorRecord(0)));
            Assert.That(first.GetColor(1), Is.SameAs(table.GetColorRecord(1)));
            Assert.That(second.GetColor(0), Is.SameAs(table.GetColorRecord(1)));
            Assert.That(second.GetColor(1), Is.SameAs(table.GetColorRecord(2)));
            Assert.That(table.GetColorRecord(0).GetBlue(), Is.EqualTo(1));
            Assert.That(table.GetColorRecord(0).GetGreen(), Is.EqualTo(2));
            Assert.That(table.GetColorRecord(0).GetRed(), Is.EqualTo(3));
            Assert.That(table.GetColorRecord(0).GetAlpha(), Is.EqualTo(4));
            Assert.That(table.HasPaletteTypes(), Is.False);
            Assert.That(table.HasPaletteLabels(), Is.False);
            Assert.That(table.HasPaletteEntryLabels(), Is.False);
            Assert.That(first.HasType(), Is.False);
            Assert.That(first.HasLabel(), Is.False);
        });
    }

    [Test]
    public void ParsesVersionOneTypesAndLocalizedLabels()
    {
        SfntCpalTable? table = Parse(BuildVersionOne(), BuildNameTable());
        SfntCpalPalette light = table!.GetPalette(0);
        SfntCpalPalette dark = table.GetPalette(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1));
            Assert.That(table.HasPaletteTypes(), Is.True);
            Assert.That(table.HasPaletteLabels(), Is.True);
            Assert.That(table.HasPaletteEntryLabels(), Is.True);
            Assert.That(light.HasType(), Is.True);
            Assert.That(light.GetTypeFlags(), Is.EqualTo(1));
            Assert.That(light.IsUsableWithLightBackground(), Is.True);
            Assert.That(light.IsUsableWithDarkBackground(), Is.False);
            Assert.That(dark.GetTypeFlags(), Is.EqualTo(2));
            Assert.That(dark.IsUsableWithLightBackground(), Is.False);
            Assert.That(dark.IsUsableWithDarkBackground(), Is.True);
            Assert.That(light.HasLabel(), Is.True);
            Assert.That(light.GetLabelNameId(), Is.EqualTo(256));
            Assert.That(light.GetLabel()!.GetValue(), Is.EqualTo("Light"));
            Assert.That(dark.GetLabel()!.GetValue(), Is.EqualTo("Dark"));
            Assert.That(table.HasPaletteEntryLabel(0), Is.True);
            Assert.That(table.GetPaletteEntryLabelNameId(0), Is.EqualTo(258));
            Assert.That(table.GetPaletteEntryLabel(0)!.GetValue(), Is.EqualTo("Outline"));
            Assert.That(table.HasPaletteEntryLabel(1), Is.False);
            Assert.That(table.GetPaletteEntryLabel(1), Is.Null);
        });
    }

    [Test]
    public void SupportsAbsentVersionOneOptionalArrays()
    {
        SfntCpalTable? table = Parse(BuildVersionOneWithoutOptionalArrays(), null);

        Assert.Multiple(() =>
        {
            Assert.That(table!.HasPaletteTypes(), Is.False);
            Assert.That(table.HasPaletteLabels(), Is.False);
            Assert.That(table.HasPaletteEntryLabels(), Is.False);
            Assert.That(table.GetPalette(0).HasType(), Is.False);
            Assert.That(table.GetPalette(0).HasLabel(), Is.False);
            Assert.That(table.HasPaletteEntryLabel(0), Is.False);
            Assert.That(table.GetPaletteEntryLabel(0), Is.Null);
            Assert.Throws<InvalidOperationException>(() =>
                table.GetPalette(0).GetTypeFlags());
        });
    }

    [Test]
    public void ResolvesForegroundSentinelWithoutTreatingItAsAPaletteEntry()
    {
        SfntCpalTable? table = Parse(BuildVersionZero(), null);
        SfntCpalColor foreground = new SfntCpalColor(9, 8, 7, 6);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetForegroundPaletteEntryIndex(), Is.EqualTo(0xFFFF));
            Assert.That(table.IsForegroundPaletteEntry(0xFFFF), Is.True);
            Assert.That(table.IsForegroundPaletteEntry(1), Is.False);
            Assert.That(table.ResolveColor(1, 0xFFFF, foreground), Is.SameAs(foreground));
            Assert.That(table.ResolveColor(1, 1, foreground),
                Is.SameAs(table.GetPalette(1).GetColor(1)));
            Assert.Throws<ArgumentNullException>(() =>
                table.ResolveColor(0, 0xFFFF, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.ResolveColor(0, 2, foreground));
        });
    }

    [TestCaseSource(nameof(InvalidCpalTables))]
    public void RejectsMalformedCountsOffsetsRecordsTypesAndLabels(
        byte[] cpal,
        byte[] name)
    {
        Assert.Throws<FontFormatException>(() => Parse(cpal, name));
    }

    [Test]
    public void EnforcesConfiguredArrayLimit()
    {
        SfntTableCache cache = CreateCache(
            BuildVersionZero(),
            null,
            FontLoadMode.Compatible,
            CreateLimits(1));

        Assert.Throws<FontLimitException>(() =>
            SfntCpalTableParser.FindTable(cache));
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildVersionZero(),
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        SfntParsedTableView? raw = SfntCpalTableParser.FindView(CreateCache(
            new byte[] { 0, 2 },
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetCpalTable(), Is.Not.Null);
            Assert.That(face.GetCpalTableView()!.GetParsedData(),
                Is.SameAs(face.GetCpalTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntCpalTableParser.FindView(absent), Is.Null);
            Assert.That(SfntCpalTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntCpalTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesAccessorAndColorComponentRanges()
    {
        SfntCpalTable? table = Parse(BuildVersionOne(), BuildNameTable());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetPalette(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetPalette(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetColorRecord(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetPalette(0).GetColor(2));
            Assert.Throws<InvalidOperationException>(() =>
                table!.GetPaletteEntryLabelNameId(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.HasPaletteEntryLabel(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntCpalColor(256, 0, 0, 0));
        });
    }

    static IEnumerable<TestCaseData> InvalidCpalTables()
    {
        byte[] validZero = BuildVersionZero();
        yield return InvalidCase(validZero[..11], null, "TruncatedHeader");

        byte[] truncatedIndices = validZero[..13];
        yield return InvalidCase(truncatedIndices, null, "TruncatedPaletteIndices");

        byte[] noEntries = BuildVersionZero();
        PatchUInt16(noEntries, 2, 0);
        yield return InvalidCase(noEntries, null, "ZeroEntries");

        byte[] noPalettes = BuildVersionZero();
        PatchUInt16(noPalettes, 4, 0);
        yield return InvalidCase(noPalettes, null, "ZeroPalettes");

        byte[] noColors = BuildVersionZero();
        PatchUInt16(noColors, 6, 0);
        yield return InvalidCase(noColors, null, "ZeroColors");

        byte[] palettePastColors = BuildVersionZero();
        PatchUInt16(palettePastColors, 14, 2);
        yield return InvalidCase(palettePastColors, null, "PalettePastColors");

        byte[] colorsInHeader = BuildVersionZero();
        PatchUInt32(colorsInHeader, 8, 12);
        yield return InvalidCase(colorsInHeader, null, "ColorsInHeader");

        yield return InvalidCase(validZero[..27], null, "TruncatedColors");

        byte[] truncatedVersionOneHeader = BuildVersionOne()[..27];
        yield return InvalidCase(truncatedVersionOneHeader, BuildNameTable(),
            "TruncatedVersionOneHeader");

        byte[] typesInHeader = BuildVersionOne();
        PatchUInt32(typesInHeader, 16, 20);
        yield return InvalidCase(typesInHeader, BuildNameTable(), "TypesInHeader");

        byte[] labelsPastTable = BuildVersionOne();
        PatchUInt32(labelsPastTable, 20, 1000);
        yield return InvalidCase(labelsPastTable, BuildNameTable(), "LabelsPastTable");

        byte[] reservedType = BuildVersionOne();
        PatchUInt32(reservedType, 28, 4);
        yield return InvalidCase(reservedType, BuildNameTable(), "ReservedPaletteType");

        yield return InvalidCase(BuildVersionOne(), null, "MissingNameTable");

        byte[] unresolvedLabel = BuildVersionOne();
        PatchUInt16(unresolvedLabel, 36, 270);
        yield return InvalidCase(unresolvedLabel, BuildNameTable(), "UnresolvedLabel");
    }

    static TestCaseData InvalidCase(byte[] cpal, byte[]? name, string nameSuffix)
    {
        return new TestCaseData(cpal, name).SetName("Rejects" + nameSuffix);
    }

    static SfntCpalTable? Parse(byte[] cpal, byte[]? name)
    {
        return SfntCpalTableParser.FindTable(CreateCache(
            cpal,
            name,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? cpal,
        byte[]? name,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (cpal != null)
        {
            tables.Add(CreateTable(FontTags.Cpal, cpal));
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

    static byte[] BuildVersionZero()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(3)
            .WriteUInt32(16)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(4)
            .WriteUInt8(10).WriteUInt8(20).WriteUInt8(30).WriteUInt8(40)
            .WriteUInt8(100).WriteUInt8(110).WriteUInt8(120).WriteUInt8(130)
            .CopyBytes();
    }

    static byte[] BuildVersionOne()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(4)
            .WriteUInt32(44)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt32(28)
            .WriteUInt32(36)
            .WriteUInt32(40)
            .WriteUInt32(1)
            .WriteUInt32(2)
            .WriteUInt16(256)
            .WriteUInt16(257)
            .WriteUInt16(258)
            .WriteUInt16(0xFFFF)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(4).WriteUInt8(5).WriteUInt8(6).WriteUInt8(255)
            .WriteUInt8(7).WriteUInt8(8).WriteUInt8(9).WriteUInt8(255)
            .WriteUInt8(10).WriteUInt8(11).WriteUInt8(12).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildVersionOneWithoutOptionalArrays()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt32(26)
            .WriteUInt16(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(4)
            .CopyBytes();
    }

    static byte[] BuildNameTable()
    {
        int[] nameIds = new int[] { 256, 257, 258 };
        string[] values = new string[] { "Light", "Dark", "Outline" };
        byte[][] encoded = new byte[values.Length][];
        int[] offsets = new int[values.Length];
        int nextOffset = 0;
        int index = 0;

        while (index < values.Length)
        {
            encoded[index] = Encoding.BigEndianUnicode.GetBytes(values[index]);
            offsets[index] = nextOffset;
            nextOffset += encoded[index].Length;
            index += 1;
        }

        int storageOffset = 6 + values.Length * 12;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)storageOffset);
        index = 0;

        while (index < values.Length)
        {
            builder
                .WriteUInt16(3)
                .WriteUInt16(1)
                .WriteUInt16(0x0409)
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)offsets[index]);
            index += 1;
        }

        index = 0;

        while (index < encoded.Length)
        {
            builder.WriteBytes(encoded[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            1000,
            maxCoverageEntries,
            1000,
            1000000,
            100,
            100,
            10,
            100);
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
