using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntStatTableTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");
    static int OpticalSizeTag = (int)TestSfntEncoding.EncodeTag("opsz");

    [Test]
    public void ParsesAxesAllValueFormatsFallbackAndUnknownFormats()
    {
        SfntStatTable? table = Parse(BuildStat(), BuildFvar());
        SfntStatAxisValue formatOne = table!.GetAxisValue(0);
        SfntStatAxisValue formatTwo = table.GetAxisValue(1);
        SfntStatAxisValue formatThree = table.GetAxisValue(2);
        SfntStatAxisValue formatFour = table.GetAxisValue(3);
        SfntStatAxisValue unknown = table.GetAxisValue(4);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.EqualTo(2));
            Assert.That(table.GetDesignAxisSize(), Is.EqualTo(8));
            Assert.That(table.GetDesignAxesOffset(), Is.EqualTo(20));
            Assert.That(table.GetAxisValueOffsetsOffset(), Is.EqualTo(44));
            Assert.That(table.GetAxisCount(), Is.EqualTo(3));
            Assert.That(table.GetAxis(0).GetTag(), Is.EqualTo(WidthTag));
            Assert.That(table.GetAxis(0).GetNameId(), Is.EqualTo(257));
            Assert.That(table.GetAxis(0).GetOrdering(), Is.EqualTo(1));
            Assert.That(table.GetAxis(0).GetName().GetValue(), Is.EqualTo("Width"));
            Assert.That(table.GetAxis(1).GetTag(), Is.EqualTo(OpticalSizeTag));
            Assert.That(table.GetAxis(2).GetTag(), Is.EqualTo(WeightTag));
            Assert.That(table.FindAxis(WeightTag), Is.SameAs(table.GetAxis(2)));
            Assert.That(table.FindAxis((int)TestSfntEncoding.EncodeTag("slnt")), Is.Null);
            Assert.That(table.GetAxisValueCount(), Is.EqualTo(5));
            Assert.That(formatOne.GetOffset(), Is.EqualTo(54));
            Assert.That(formatOne.GetFormat(), Is.EqualTo(1));
            Assert.That(formatOne.IsSupported(), Is.True);
            Assert.That(formatOne.GetAxisIndex(), Is.EqualTo(2));
            Assert.That(formatOne.GetFlags(), Is.EqualTo(2));
            Assert.That(formatOne.IsElidable(), Is.True);
            Assert.That(formatOne.IsOlderSiblingFontAttribute(), Is.False);
            Assert.That(formatOne.GetNameId(), Is.EqualTo(259));
            Assert.That(formatOne.GetName()!.GetValue(), Is.EqualTo("Regular"));
            Assert.That(formatOne.GetValue(), Is.EqualTo(400));
            Assert.That(formatTwo.GetFormat(), Is.EqualTo(2));
            Assert.That(formatTwo.GetAxisIndex(), Is.EqualTo(1));
            Assert.That(formatTwo.GetNominalValue(), Is.EqualTo(12));
            Assert.That(formatTwo.GetRangeMinimumValue(), Is.EqualTo(-32768));
            Assert.That(formatTwo.GetRangeMaximumValue(), Is.EqualTo(72));
            Assert.That(formatThree.GetFormat(), Is.EqualTo(3));
            Assert.That(formatThree.GetValue(), Is.EqualTo(400));
            Assert.That(formatThree.GetLinkedValue(), Is.EqualTo(700));
            Assert.That(formatFour.GetFormat(), Is.EqualTo(4));
            Assert.That(formatFour.GetAxisIndex(), Is.EqualTo(-1));
            Assert.That(formatFour.IsOlderSiblingFontAttribute(), Is.True);
            Assert.That(formatFour.GetName()!.GetValue(), Is.EqualTo("Bold Condensed"));
            Assert.That(formatFour.GetRecordCount(), Is.EqualTo(2));
            Assert.That(formatFour.GetRecord(0).GetAxisIndex(), Is.EqualTo(2));
            Assert.That(formatFour.GetRecord(0).GetValue(), Is.EqualTo(700));
            Assert.That(formatFour.GetRecord(1).GetAxisIndex(), Is.Zero);
            Assert.That(formatFour.GetRecord(1).GetValue(), Is.EqualTo(75));
            Assert.That(unknown.GetFormat(), Is.EqualTo(5));
            Assert.That(unknown.IsSupported(), Is.False);
            Assert.That(unknown.GetName(), Is.Null);
            Assert.That(table.HasElidedFallbackName(), Is.True);
            Assert.That(table.GetElidedFallbackNameId(), Is.EqualTo(259));
            Assert.That(table.GetElidedFallbackName()!.GetValue(), Is.EqualTo("Regular"));
        });
    }

    [Test]
    public void SupportsVersionOneAndTreatsFormatFourAsUnknownBeforeVersionTwelve()
    {
        SfntStatTable? versionOne = Parse(BuildVersionOneStat(), null);
        byte[] versionElevenBytes = BuildStat();
        PatchUInt16(versionElevenBytes, 2, 1);
        SfntStatTable? versionEleven = Parse(versionElevenBytes, BuildFvar());

        Assert.Multiple(() =>
        {
            Assert.That(versionOne!.GetMinorVersion(), Is.Zero);
            Assert.That(versionOne.HasElidedFallbackName(), Is.False);
            Assert.That(versionOne.GetElidedFallbackName(), Is.Null);
            Assert.That(versionOne.GetAxisValue(0).GetValue(), Is.EqualTo(400));
            Assert.That(versionEleven!.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(versionEleven.GetAxisValue(3).GetFormat(), Is.EqualTo(4));
            Assert.That(versionEleven.GetAxisValue(3).IsSupported(), Is.False);
            Assert.That(versionEleven.GetAxisValue(4).IsSupported(), Is.False);
        });
    }

    [Test]
    public void RetainsCompatibleHigherMinorExtensionsAndReservedFlags()
    {
        byte[] bytes = BuildCompatibleStat();
        SfntParsedTableView? compatible = SfntStatTableParser.FindView(CreateCache(
            bytes,
            BuildNameTable(),
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntStatTableParser.FindView(CreateCache(
            bytes,
            BuildNameTable(),
            null,
            FontLoadMode.Strict,
            FontLimits.CreateCompatible()));
        SfntStatTable table = (SfntStatTable)compatible!.GetParsedData();

        Assert.Multiple(() =>
        {
            Assert.That(compatible.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(table.GetMinorVersion(), Is.EqualTo(3));
            Assert.That(table.GetDesignAxisSize(), Is.EqualTo(10));
            Assert.That(table.GetAxisValue(0).GetFlags(), Is.EqualTo(4));
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
        });
    }

    [TestCaseSource(nameof(InvalidStatTables))]
    public void RejectsMalformedHeadersAxesValuesAndNames(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, BuildFvar()));
    }

    [Test]
    public void RequiresResolvableNamesAndANameTable()
    {
        byte[] unresolvedAxis = BuildStat();
        PatchUInt16(unresolvedAxis, 24, 270);
        byte[] unresolvedValue = BuildStat();
        PatchUInt16(unresolvedValue, 60, 270);
        byte[] unresolvedFallback = BuildStat();
        PatchUInt16(unresolvedFallback, 18, 270);
        SfntTableCache missingName = CreateCache(
            BuildStat(),
            null,
            BuildFvar(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(unresolvedAxis, BuildFvar()));
            Assert.Throws<FontFormatException>(() => Parse(unresolvedValue, BuildFvar()));
            Assert.Throws<FontFormatException>(() => Parse(unresolvedFallback, BuildFvar()));
            Assert.Throws<FontFormatException>(() =>
                SfntStatTableParser.FindTable(missingName));
        });
    }

    [Test]
    public void CrossChecksFvarTagsAndNameIdsInArbitraryOrder()
    {
        byte[] missingTag = BuildFvar();
        PatchUInt32(missingTag, 16, TestSfntEncoding.EncodeTag("slnt"));
        byte[] mismatchedName = BuildFvar();
        PatchUInt16(mismatchedName, 34, 257);
        byte[] unsupportedFvar = BuildFvar();
        PatchUInt16(unsupportedFvar, 0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(Parse(BuildStat(), BuildFvar()), Is.Not.Null);
            Assert.Throws<FontFormatException>(() => Parse(BuildStat(), missingTag));
            Assert.Throws<FontFormatException>(() => Parse(BuildStat(), mismatchedName));
            Assert.Throws<FontFormatException>(() => Parse(BuildStat(), unsupportedFvar));
        });
    }

    [Test]
    public void EnforcesConfiguredAxisValueAndFormatFourLimits()
    {
        SfntTableCache axisLimited = CreateCache(
            BuildStat(),
            BuildNameTable(),
            BuildFvar(),
            FontLoadMode.Compatible,
            CreateLimits(100, 2));
        SfntTableCache valueLimited = CreateCache(
            BuildStat(),
            BuildNameTable(),
            BuildFvar(),
            FontLoadMode.Compatible,
            CreateLimits(4, 100));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                SfntStatTableParser.FindTable(axisLimited));
            Assert.Throws<FontLimitException>(() =>
                SfntStatTableParser.FindTable(valueLimited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownMajorVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildStat(),
            BuildNameTable(),
            BuildFvar(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknown = BuildStat();
        PatchUInt16(unknown, 0, 2);
        SfntParsedTableView? raw = SfntStatTableParser.FindView(CreateCache(
            unknown,
            BuildNameTable(),
            BuildFvar(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            BuildNameTable(),
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetStatTable(), Is.Not.Null);
            Assert.That(face.GetStatTableView()!.GetParsedData(),
                Is.SameAs(face.GetStatTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntStatTableParser.FindView(absent), Is.Null);
            Assert.That(SfntStatTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntStatTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesAccessorIndexes()
    {
        SfntStatTable? table = Parse(BuildStat(), BuildFvar());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxis(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxis(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxisValue(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxisValue(5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetAxisValue(3).GetRecord(2));
        });
    }

    static IEnumerable<TestCaseData> InvalidStatTables()
    {
        byte[] valid = BuildStat();
        yield return InvalidCase(valid[..19], "TruncatedHeader");

        byte[] invalidAxisSize = BuildStat();
        PatchUInt16(invalidAxisSize, 4, 7);
        yield return InvalidCase(invalidAxisSize, "InvalidAxisSize");

        byte[] axesInHeader = BuildStat();
        PatchUInt32(axesInHeader, 8, 12);
        yield return InvalidCase(axesInHeader, "AxesInHeader");

        byte[] offsetsInHeader = BuildStat();
        PatchUInt32(offsetsInHeader, 14, 16);
        yield return InvalidCase(offsetsInHeader, "OffsetsInHeader");

        byte[] valueWithoutAxes = BuildStat();
        PatchUInt16(valueWithoutAxes, 6, 0);
        PatchUInt32(valueWithoutAxes, 8, 0);
        yield return InvalidCase(valueWithoutAxes, "ValueWithoutAxes");

        byte[] duplicateAxis = BuildStat();
        PatchUInt32(duplicateAxis, 28, TestSfntEncoding.EncodeTag("wdth"));
        yield return InvalidCase(duplicateAxis, "DuplicateAxisTag");

        byte[] invalidAxisTag = BuildStat();
        PatchUInt32(invalidAxisTag, 20, TestSfntEncoding.EncodeTag("1dth"));
        yield return InvalidCase(invalidAxisTag, "InvalidAxisTag");

        byte[] offsetInArray = BuildStat();
        PatchUInt16(offsetInArray, 44, 9);
        yield return InvalidCase(offsetInArray, "ValueOffsetInArray");

        byte[] offsetPastTable = BuildStat();
        PatchUInt16(offsetPastTable, 44, 200);
        yield return InvalidCase(offsetPastTable, "ValueOffsetPastTable");

        yield return InvalidCase(valid[..63], "TruncatedFormatOne");

        byte[] badAxisIndex = BuildStat();
        PatchUInt16(badAxisIndex, 56, 3);
        yield return InvalidCase(badAxisIndex, "MissingAxisIndex");

        byte[] reservedFlags = BuildStat();
        PatchUInt16(reservedFlags, 58, 4);
        yield return InvalidCase(reservedFlags, "ReservedFlags");

        byte[] unorderedRange = BuildStat();
        PatchInt32(unorderedRange, 78, Fixed(20));
        yield return InvalidCase(unorderedRange, "UnorderedFormatTwoRange");

        byte[] zeroFormatFourCount = BuildStat();
        PatchUInt16(zeroFormatFourCount, 104, 0);
        yield return InvalidCase(zeroFormatFourCount, "ZeroFormatFourCount");

        byte[] duplicateFormatFourAxis = BuildStat();
        PatchUInt16(duplicateFormatFourAxis, 116, 2);
        yield return InvalidCase(duplicateFormatFourAxis, "DuplicateFormatFourAxis");

        byte[] invalidFallbackId = BuildStat();
        PatchUInt16(invalidFallbackId, 18, 255);
        yield return InvalidCase(invalidFallbackId, "InvalidFallbackNameId");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntStatTable? Parse(byte[] stat, byte[]? fvar)
    {
        return SfntStatTableParser.FindTable(CreateCache(
            stat,
            BuildNameTable(),
            fvar,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? stat,
        byte[]? name,
        byte[]? fvar,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (stat != null)
        {
            tables.Add(CreateTable(FontTags.Stat, stat));
        }

        if (name != null)
        {
            tables.Add(CreateTable(FontTags.Name, name));
        }

        if (fvar != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, fvar));
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

    static byte[] BuildStat()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(8)
            .WriteUInt16(3)
            .WriteUInt32(20)
            .WriteUInt16(5)
            .WriteUInt32(44)
            .WriteUInt16(259);
        WriteStatAxis(builder, "wdth", 257, 1, 0);
        WriteStatAxis(builder, "opsz", 258, 2, 0);
        WriteStatAxis(builder, "wght", 256, 0, 0);
        builder
            .WriteUInt16(10)
            .WriteUInt16(22)
            .WriteUInt16(42)
            .WriteUInt16(58)
            .WriteUInt16(78)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(259)
            .WriteInt32(Fixed(400))
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(260)
            .WriteInt32(Fixed(12))
            .WriteInt32(int.MinValue)
            .WriteInt32(Fixed(72))
            .WriteUInt16(3)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(261)
            .WriteInt32(Fixed(400))
            .WriteInt32(Fixed(700))
            .WriteUInt16(4)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(262)
            .WriteUInt16(2)
            .WriteInt32(Fixed(700))
            .WriteUInt16(0)
            .WriteInt32(Fixed(75))
            .WriteUInt16(5);
        return builder.CopyBytes();
    }

    static byte[] BuildVersionOneStat()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(8)
            .WriteUInt16(1)
            .WriteUInt32(18)
            .WriteUInt16(1)
            .WriteUInt32(26);
        WriteStatAxis(builder, "wght", 256, 0, 0);
        builder
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(259)
            .WriteInt32(Fixed(400));
        return builder.CopyBytes();
    }

    static byte[] BuildCompatibleStat()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(10)
            .WriteUInt16(1)
            .WriteUInt32(20)
            .WriteUInt16(1)
            .WriteUInt32(30)
            .WriteUInt16(259);
        WriteStatAxis(builder, "wght", 256, 0, 2);
        builder
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(259)
            .WriteInt32(Fixed(400));
        return builder.CopyBytes();
    }

    static void WriteStatAxis(
        TestBinaryBuilder builder,
        string tag,
        int nameId,
        int ordering,
        int extensionLength)
    {
        builder
            .WriteTag(tag)
            .WriteUInt16((ushort)nameId)
            .WriteUInt16((ushort)ordering)
            .WriteZeros(extensionLength);
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
        WriteFvarAxis(builder, "wdth", 62.5, 100, 150, 257);
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
        int[] nameIds = new int[] { 256, 257, 258, 259, 260, 261, 262 };
        string[] values = new string[]
        {
            "Weight",
            "Width",
            "Optical Size",
            "Regular",
            "Subhead",
            "Regular Linked",
            "Bold Condensed"
        };
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

    static int Fixed(double value)
    {
        return System.Convert.ToInt32(System.Math.Round(value * 65536.0));
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

    static void PatchInt32(byte[] bytes, int offset, int value)
    {
        PatchUInt32(bytes, offset, (uint)value);
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
