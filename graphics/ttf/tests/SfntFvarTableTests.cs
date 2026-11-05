using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntFvarTableTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");

    [Test]
    public void ParsesAxesInstancesAndResolvedNamesInRecordOrder()
    {
        SfntFvarTable? table = Parse(BuildFvar(true));
        SfntFvarAxis weight = table!.GetAxis(0);
        SfntFvarAxis width = table.GetAxis(1);
        SfntFvarInstance regular = table.GetInstance(0);
        SfntFvarInstance condensedBold = table.GetInstance(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetAxesArrayOffset(), Is.EqualTo(18));
            Assert.That(table.GetAxisSize(), Is.EqualTo(20));
            Assert.That(table.GetInstanceSize(), Is.EqualTo(14));
            Assert.That(table.GetAxisCount(), Is.EqualTo(2));
            Assert.That(weight.GetTag(), Is.EqualTo(WeightTag));
            Assert.That(weight.GetMinimumValue(), Is.EqualTo(100));
            Assert.That(weight.GetDefaultValue(), Is.EqualTo(400));
            Assert.That(weight.GetMaximumValue(), Is.EqualTo(900));
            Assert.That(weight.GetFlags(), Is.EqualTo(1));
            Assert.That(weight.IsHidden(), Is.True);
            Assert.That(weight.GetNameId(), Is.EqualTo(256));
            Assert.That(weight.GetName().GetValue(), Is.EqualTo("Weight"));
            Assert.That(width.GetTag(), Is.EqualTo(WidthTag));
            Assert.That(width.GetMinimumValue(), Is.EqualTo(62.5));
            Assert.That(width.GetDefaultValue(), Is.EqualTo(100));
            Assert.That(width.GetMaximumValue(), Is.EqualTo(150));
            Assert.That(width.IsHidden(), Is.False);
            Assert.That(width.GetName().GetValue(), Is.EqualTo("Width"));
            Assert.That(table.FindAxis(WeightTag), Is.SameAs(weight));
            Assert.That(table.FindAxis((int)TestSfntEncoding.EncodeTag("opsz")), Is.Null);
            Assert.That(table.GetInstanceCount(), Is.EqualTo(2));
            Assert.That(regular.GetSubfamilyNameId(), Is.EqualTo(258));
            Assert.That(regular.GetSubfamilyName().GetValue(), Is.EqualTo("Regular"));
            Assert.That(regular.GetCoordinateCount(), Is.EqualTo(2));
            Assert.That(regular.GetCoordinate(0), Is.EqualTo(400));
            Assert.That(regular.GetCoordinate(1), Is.EqualTo(100));
            Assert.That(regular.HasPostScriptName(), Is.True);
            Assert.That(regular.GetPostScriptNameId(), Is.EqualTo(260));
            Assert.That(regular.GetPostScriptName()!.GetValue(), Is.EqualTo("Demo-Regular"));
            Assert.That(condensedBold.GetSubfamilyName().GetValue(),
                Is.EqualTo("Condensed Bold"));
            Assert.That(condensedBold.GetCoordinate(0), Is.EqualTo(700));
            Assert.That(condensedBold.GetCoordinate(1), Is.EqualTo(75));
            Assert.That(condensedBold.HasPostScriptName(), Is.False);
            Assert.That(condensedBold.GetPostScriptNameId(), Is.EqualTo(0xFFFF));
            Assert.That(condensedBold.GetPostScriptName(), Is.Null);
        });
    }

    [Test]
    public void ParsesInstanceRecordsWithoutPostScriptNameFields()
    {
        SfntFvarTable? table = Parse(BuildFvar(false));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetInstanceSize(), Is.EqualTo(12));
            Assert.That(table.GetInstance(0).HasPostScriptName(), Is.False);
            Assert.That(table.GetInstance(0).GetPostScriptNameId(), Is.EqualTo(0xFFFF));
            Assert.That(table.GetInstance(0).GetPostScriptName(), Is.Null);
            Assert.That(table.GetInstance(1).HasPostScriptName(), Is.False);
        });
    }

    [Test]
    public void RetainsCompatibleHigherMinorRecordExtensions()
    {
        byte[] bytes = BuildFvar(true, 1, 2, 2);
        SfntParsedTableView? compatible = SfntFvarTableParser.FindView(CreateCache(
            bytes,
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntFvarTableParser.FindView(CreateCache(
            bytes,
            BuildNameTable(),
            FontLoadMode.Strict,
            FontLimits.CreateCompatible()));
        SfntFvarTable table = (SfntFvarTable)compatible!.GetParsedData();

        Assert.Multiple(() =>
        {
            Assert.That(compatible.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(table.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(table.GetAxisSize(), Is.EqualTo(22));
            Assert.That(table.GetInstanceSize(), Is.EqualTo(16));
            Assert.That(table.GetAxis(1).GetName().GetValue(), Is.EqualTo("Width"));
            Assert.That(table.GetInstance(0).GetPostScriptName()!.GetValue(),
                Is.EqualTo("Demo-Regular"));
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
        });
    }

    [TestCaseSource(nameof(InvalidFvarTables))]
    public void RejectsMalformedHeadersSizesTagsRangesAndRecords(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(36)]
    [TestCase(58)]
    [TestCase(70)]
    public void RejectsUnresolvedReferencedNames(int nameIdOffset)
    {
        byte[] bytes = BuildFvar(true);
        PatchUInt16(bytes, nameIdOffset, 261);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RequiresNameTableAndEnforcesVariationLimits()
    {
        byte[] bytes = BuildFvar(true);
        SfntTableCache missingName = CreateCache(
            bytes,
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache axisLimited = CreateCache(
            bytes,
            BuildNameTable(),
            FontLoadMode.Compatible,
            CreateLimits(100, 1));
        SfntTableCache instanceLimited = CreateCache(
            bytes,
            BuildNameTable(),
            FontLoadMode.Compatible,
            CreateLimits(1, 100));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntFvarTableParser.FindTable(missingName));
            Assert.Throws<FontLimitException>(() =>
                SfntFvarTableParser.FindTable(axisLimited));
            Assert.Throws<FontLimitException>(() =>
                SfntFvarTableParser.FindTable(instanceLimited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownMajorVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildFvar(true),
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
        byte[] unknown = BuildFvar(true);
        PatchUInt16(unknown, 0, 2);
        SfntParsedTableView? raw = SfntFvarTableParser.FindView(CreateCache(
            unknown,
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetFvarTable(), Is.Not.Null);
            Assert.That(face.GetFvarTableView()!.GetParsedData(),
                Is.SameAs(face.GetFvarTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntFvarTableParser.FindView(absent), Is.Null);
            Assert.That(SfntFvarTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntFvarTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesAccessorIndexes()
    {
        SfntFvarTable? table = Parse(BuildFvar(true));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxis(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAxis(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetInstance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetInstance(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetInstance(0).GetCoordinate(2));
        });
    }

    static IEnumerable<TestCaseData> InvalidFvarTables()
    {
        byte[] valid = BuildFvar(true);
        yield return InvalidCase(valid[..15], "TruncatedHeader");

        byte[] invalidCountSizePairs = BuildFvar(true);
        PatchUInt16(invalidCountSizePairs, 6, 3);
        yield return InvalidCase(invalidCountSizePairs, "CountSizePairs");

        byte[] noAxes = BuildFvar(true);
        PatchUInt16(noAxes, 8, 0);
        yield return InvalidCase(noAxes, "ZeroAxisCount");

        byte[] invalidAxisSize = BuildFvar(true);
        PatchUInt16(invalidAxisSize, 10, 19);
        yield return InvalidCase(invalidAxisSize, "AxisSize");

        byte[] invalidInstanceSize = BuildFvar(true);
        PatchUInt16(invalidInstanceSize, 14, 13);
        yield return InvalidCase(invalidInstanceSize, "InstanceSize");

        byte[] axesInHeader = BuildFvar(true);
        PatchUInt16(axesInHeader, 4, 12);
        yield return InvalidCase(axesInHeader, "AxesOffset");

        yield return InvalidCase(valid[..^1], "TruncatedArrays");

        byte[] invalidRegisteredTag = BuildFvar(true);
        PatchUInt32(invalidRegisteredTag, 18, TestSfntEncoding.EncodeTag("1ght"));
        yield return InvalidCase(invalidRegisteredTag, "InvalidRegisteredAxisTag");

        byte[] invalidPrivateTag = BuildFvar(true);
        PatchUInt32(invalidPrivateTag, 18, TestSfntEncoding.EncodeTag("Abcd"));
        yield return InvalidCase(invalidPrivateTag, "InvalidPrivateAxisTag");

        byte[] duplicateTag = BuildFvar(true);
        PatchUInt32(duplicateTag, 38, TestSfntEncoding.EncodeTag("wght"));
        yield return InvalidCase(duplicateTag, "DuplicateAxisTag");

        byte[] unorderedRange = BuildFvar(true);
        PatchInt32(unorderedRange, 26, Fixed(50));
        yield return InvalidCase(unorderedRange, "UnorderedAxisRange");

        byte[] reservedAxisFlag = BuildFvar(true);
        PatchUInt16(reservedAxisFlag, 34, 2);
        yield return InvalidCase(reservedAxisFlag, "ReservedAxisFlag");

        byte[] invalidAxisNameId = BuildFvar(true);
        PatchUInt16(invalidAxisNameId, 36, 255);
        yield return InvalidCase(invalidAxisNameId, "InvalidAxisNameId");

        byte[] reservedInstanceFlag = BuildFvar(true);
        PatchUInt16(reservedInstanceFlag, 60, 1);
        yield return InvalidCase(reservedInstanceFlag, "ReservedInstanceFlag");

        byte[] coordinateOutsideRange = BuildFvar(true);
        PatchInt32(coordinateOutsideRange, 62, Fixed(950));
        yield return InvalidCase(coordinateOutsideRange, "InstanceCoordinateOutsideRange");

        byte[] invalidSubfamilyNameId = BuildFvar(true);
        PatchUInt16(invalidSubfamilyNameId, 58, 3);
        yield return InvalidCase(invalidSubfamilyNameId, "InvalidSubfamilyNameId");

        byte[] invalidPostScriptNameId = BuildFvar(true);
        PatchUInt16(invalidPostScriptNameId, 70, 7);
        yield return InvalidCase(invalidPostScriptNameId, "InvalidPostScriptNameId");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntFvarTable? Parse(byte[] bytes)
    {
        return SfntFvarTableParser.FindTable(CreateCache(
            bytes,
            BuildNameTable(),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? fvar,
        byte[]? name,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

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

    static byte[] BuildFvar(bool includePostScriptNames)
    {
        return BuildFvar(includePostScriptNames, 0, 0, 0);
    }

    static byte[] BuildFvar(
        bool includePostScriptNames,
        int minorVersion,
        int axisExtensionLength,
        int instanceExtensionLength)
    {
        int axisSize = 20 + axisExtensionLength;
        int instanceSize = 12 +
            (includePostScriptNames ? 2 : 0) +
            instanceExtensionLength;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)minorVersion)
            .WriteUInt16(18)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16((ushort)axisSize)
            .WriteUInt16(2)
            .WriteUInt16((ushort)instanceSize)
            .WriteZeros(2);

        WriteAxis(builder, "wght", 100, 400, 900, 1, 256, axisExtensionLength);
        WriteAxis(builder, "wdth", 62.5, 100, 150, 0, 257, axisExtensionLength);
        WriteInstance(
            builder,
            258,
            400,
            100,
            includePostScriptNames,
            260,
            instanceExtensionLength);
        WriteInstance(
            builder,
            259,
            700,
            75,
            includePostScriptNames,
            0xFFFF,
            instanceExtensionLength);
        return builder.CopyBytes();
    }

    static void WriteAxis(
        TestBinaryBuilder builder,
        string tag,
        double minimum,
        double defaultValue,
        double maximum,
        int flags,
        int nameId,
        int extensionLength)
    {
        builder
            .WriteTag(tag)
            .WriteInt32(Fixed(minimum))
            .WriteInt32(Fixed(defaultValue))
            .WriteInt32(Fixed(maximum))
            .WriteUInt16((ushort)flags)
            .WriteUInt16((ushort)nameId)
            .WriteZeros(extensionLength);
    }

    static void WriteInstance(
        TestBinaryBuilder builder,
        int subfamilyNameId,
        double weight,
        double width,
        bool includePostScriptName,
        int postScriptNameId,
        int extensionLength)
    {
        builder
            .WriteUInt16((ushort)subfamilyNameId)
            .WriteUInt16(0)
            .WriteInt32(Fixed(weight))
            .WriteInt32(Fixed(width));

        if (includePostScriptName)
        {
            builder.WriteUInt16((ushort)postScriptNameId);
        }

        builder.WriteZeros(extensionLength);
    }

    static byte[] BuildNameTable()
    {
        int[] nameIds = new int[] { 256, 257, 258, 259, 260 };
        string[] values = new string[]
        {
            "Weight",
            "Width",
            "Regular",
            "Condensed Bold",
            "Demo-Regular"
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
