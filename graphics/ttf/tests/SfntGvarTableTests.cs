using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntGvarTableTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void DecodesSharedTuplesHeadersPointNumbersAndDeltas(bool longOffsets)
    {
        SfntGvarTable? table = Parse(BuildGvar(longOffsets));
        SfntGlyphVariationData? glyph = table!.GetGlyphVariationData(0, 300);
        SfntGvarTupleVariation shared = glyph!.GetTupleVariation(0);
        SfntGvarTupleVariation intermediate = glyph.GetTupleVariation(1);
        SfntGvarTupleVariation allPoints = glyph.GetTupleVariation(2);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetAxisCount(), Is.EqualTo(2));
            Assert.That(table.UsesLongOffsets(), Is.EqualTo(longOffsets));
            Assert.That(table.GetSharedTupleCount(), Is.EqualTo(2));
            Assert.That(table.GetSharedTuple(0).GetCoordinate(0), Is.EqualTo(1));
            Assert.That(table.GetSharedTuple(0).GetCoordinate(1), Is.Zero);
            Assert.That(table.GetSharedTuple(1).GetCoordinate(1), Is.EqualTo(-1));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(table.GetGlyphDataOffset(0), Is.Zero);
            Assert.That(table.GetGlyphDataLength(0), Is.EqualTo(66));
            Assert.That(table.GetGlyphDataLength(1), Is.Zero);
            Assert.That(table.GetGlyphVariationData(1, 4), Is.Null);
            Assert.That(glyph.GetGlyphId(), Is.Zero);
            Assert.That(glyph.GetTargetPointCount(), Is.EqualTo(300));
            Assert.That(glyph.HasSharedPointNumbers(), Is.True);
            Assert.That(glyph.GetSharedPointNumbers()!.GetCount(), Is.EqualTo(3));
            Assert.That(glyph.GetSharedPointNumbers()!.GetPointNumber(0), Is.EqualTo(1));
            Assert.That(glyph.GetSharedPointNumbers()!.GetPointNumber(1), Is.EqualTo(3));
            Assert.That(glyph.GetSharedPointNumbers()!.GetPointNumber(2), Is.EqualTo(260));
            Assert.That(glyph.GetTupleVariationCount(), Is.EqualTo(3));
            Assert.That(shared.GetVariationDataSize(), Is.EqualTo(10));
            Assert.That(shared.GetTupleIndex(), Is.Zero);
            Assert.That(shared.HasEmbeddedPeakTuple(), Is.False);
            Assert.That(shared.HasIntermediateRegion(), Is.False);
            Assert.That(shared.HasPrivatePointNumbers(), Is.False);
            Assert.That(shared.GetPeakTuple(), Is.SameAs(table.GetSharedTuple(0)));
            Assert.That(shared.GetPointNumbers(), Is.SameAs(glyph.GetSharedPointNumbers()));
            Assert.That(shared.GetDeltaCount(), Is.EqualTo(3));
            Assert.That(shared.GetXDelta(0), Is.EqualTo(10));
            Assert.That(shared.GetXDelta(1), Is.Zero);
            Assert.That(shared.GetXDelta(2), Is.EqualTo(300));
            Assert.That(shared.GetYDelta(0), Is.EqualTo(-1));
            Assert.That(shared.GetYDelta(1), Is.EqualTo(2));
            Assert.That(shared.GetYDelta(2), Is.EqualTo(-3));
            Assert.That(intermediate.HasEmbeddedPeakTuple(), Is.True);
            Assert.That(intermediate.HasIntermediateRegion(), Is.True);
            Assert.That(intermediate.HasPrivatePointNumbers(), Is.True);
            Assert.That(intermediate.GetPeakTuple().GetCoordinate(0), Is.EqualTo(0.5));
            Assert.That(intermediate.GetPeakTuple().GetCoordinate(1), Is.EqualTo(-0.5));
            Assert.That(intermediate.GetIntermediateStartTuple()!.GetCoordinate(1), Is.EqualTo(-1));
            Assert.That(intermediate.GetIntermediateEndTuple()!.GetCoordinate(0), Is.EqualTo(1));
            Assert.That(intermediate.GetPointNumbers().GetPointNumber(0), Is.Zero);
            Assert.That(intermediate.GetPointNumbers().GetPointNumber(1), Is.EqualTo(2));
            Assert.That(intermediate.GetXDelta(0), Is.Zero);
            Assert.That(intermediate.GetXDelta(1), Is.Zero);
            Assert.That(intermediate.GetYDelta(0), Is.EqualTo(-300));
            Assert.That(intermediate.GetYDelta(1), Is.EqualTo(400));
            Assert.That(allPoints.GetPointNumbers().UsesAllPoints(), Is.True);
            Assert.That(allPoints.GetDeltaCount(), Is.EqualTo(300));
            Assert.That(allPoints.GetXDelta(299), Is.Zero);
            Assert.That(allPoints.GetYDelta(299), Is.Zero);
        });
    }

    [TestCaseSource(nameof(InvalidHeaderTables))]
    public void RejectsMalformedHeaderSharedTuplesAndOffsets(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsMalformedTupleHeadersAndPackedRunsDuringLazyGlyphDecode()
    {
        byte[] reservedCount = BuildGvar(true);
        PatchUInt16(reservedCount, 44, 0x9003);
        byte[] earlyData = BuildGvar(true);
        PatchUInt16(earlyData, 46, 3);
        byte[] reservedTupleFlag = BuildGvar(true);
        PatchUInt16(reservedTupleFlag, 50, 0x1000);
        byte[] missingSharedTuple = BuildGvar(true);
        PatchUInt16(missingSharedTuple, 50, 2);
        byte[] pointRunOverflow = BuildGvar(true);
        pointRunOverflow[73] = 3;
        byte[] deltaRunOverflow = BuildGvar(true);
        deltaRunOverflow[79] = 3;
        byte[] truncatedTupleData = BuildGvar(true);
        PatchUInt16(truncatedTupleData, 68, 20);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Decode(reservedCount));
            Assert.Throws<FontFormatException>(() => Decode(earlyData));
            Assert.Throws<FontFormatException>(() => Decode(reservedTupleFlag));
            Assert.Throws<FontFormatException>(() => Decode(missingSharedTuple));
            Assert.Throws<FontFormatException>(() => Decode(pointRunOverflow));
            Assert.Throws<FontFormatException>(() => Decode(deltaRunOverflow));
            Assert.Throws<FontFormatException>(() => Decode(truncatedTupleData));
        });
    }

    [Test]
    public void RejectsTruncatedAndOutOfRangePackedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ReadPoints(
                new byte[] { 0x80 }));
            Assert.Throws<FontFormatException>(() => ReadPoints(
                new byte[] { 2, 1, 1 }));
            Assert.Throws<FontFormatException>(() => ReadPoints(
                new byte[] { 1, 0x80, 1 }));
            Assert.Throws<FontFormatException>(() => ReadPoints(
                new byte[] { 1, 0, 5 },
                5));
            Assert.Throws<FontFormatException>(() => ReadPoints(
                new byte[] { 2, 0x81, 0xFF, 0xFF, 0, 1 },
                65536));
            Assert.Throws<FontFormatException>(() => ReadDeltas(
                new byte[] { 2, 1, 2, 3 },
                2));
            Assert.Throws<FontFormatException>(() => ReadDeltas(
                new byte[] { 0, },
                1));
            Assert.Throws<FontFormatException>(() => ReadDeltas(
                new byte[] { 0x40, 1 },
                1));
        });
    }

    [Test]
    public void RequiresMatchingFvarAndMaxpDependencies()
    {
        byte[] gvar = BuildGvar(true);
        SfntTableCache missingFvar = CreateCache(gvar, null, BuildMaxp(3));
        SfntTableCache missingMaxp = CreateCache(gvar, BuildFvar(), null);
        SfntTableCache wrongAxes = CreateCache(gvar, BuildFvarOneAxis(), BuildMaxp(3));
        SfntTableCache wrongGlyphs = CreateCache(gvar, BuildFvar(), BuildMaxp(2));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntGvarTableParser.FindTable(missingFvar));
            Assert.Throws<FontFormatException>(() =>
                SfntGvarTableParser.FindTable(missingMaxp));
            Assert.Throws<FontFormatException>(() =>
                SfntGvarTableParser.FindTable(wrongAxes));
            Assert.Throws<FontFormatException>(() =>
                SfntGvarTableParser.FindTable(wrongGlyphs));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndValidatesAccessorIndexes()
    {
        SfntTableCache cache = CreateCache(
            BuildGvar(true),
            BuildFvar(),
            BuildMaxp(3));
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        SfntGvarTable? table = face.GetGvarTable();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetGvarTableView()!.GetParsedData(), Is.SameAs(table));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetSharedTuple(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetGlyphDataLength(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetGlyphVariationData(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetSharedTuple(0).GetCoordinate(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetGlyphVariationData(0, 300)!.GetTupleVariation(3));
        });
    }

    static IEnumerable<TestCaseData> InvalidHeaderTables()
    {
        byte[] valid = BuildGvar(true);
        yield return Invalid(valid[..19], "TruncatedHeader");

        byte[] flags = BuildGvar(true);
        PatchUInt16(flags, 14, 2);
        yield return Invalid(flags, "ReservedFlags");

        byte[] firstOffset = BuildGvar(true);
        PatchUInt32(firstOffset, 20, 2);
        yield return Invalid(firstOffset, "NonzeroFirstGlyphOffset");

        byte[] unordered = BuildGvar(true);
        PatchUInt32(unordered, 28, 60);
        PatchUInt32(unordered, 32, 50);
        yield return Invalid(unordered, "UnorderedGlyphOffsets");

        byte[] outside = BuildGvar(true);
        PatchUInt32(outside, 32, 1000);
        yield return Invalid(outside, "GlyphOffsetOutsideTable");

        byte[] sharedInsideOffsets = BuildGvar(true);
        PatchUInt32(sharedInsideOffsets, 8, 20);
        yield return Invalid(sharedInsideOffsets, "SharedTuplesInsideOffsets");

        byte[] glyphInsideShared = BuildGvar(true);
        PatchUInt32(glyphInsideShared, 16, 40);
        yield return Invalid(glyphInsideShared, "GlyphDataInsideSharedTuples");

        byte[] coordinate = BuildGvar(true);
        PatchUInt16(coordinate, 36, 0x6000);
        yield return Invalid(coordinate, "SharedCoordinateOutsideRange");
    }

    static TestCaseData Invalid(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntGvarTable? Parse(byte[] bytes)
    {
        return SfntGvarTableParser.FindTable(CreateCache(
            bytes,
            BuildFvar(),
            BuildMaxp(3)));
    }

    static SfntGlyphVariationData? Decode(byte[] bytes)
    {
        return Parse(bytes)!.GetGlyphVariationData(0, 300);
    }

    static SfntGvarPointNumbers ReadPoints(byte[] bytes)
    {
        return ReadPoints(bytes, 300);
    }

    static SfntGvarPointNumbers ReadPoints(byte[] bytes, int maximumPointNumber)
    {
        FontData data = FontData.Create(bytes);
        return SfntGvarPackedDataDecoder.ReadPointNumbers(
            new BigEndianReader(data.GetSlice(0, data.GetLength())),
            maximumPointNumber,
            new SfntVariationParsingBudget(1000));
    }

    static int[] ReadDeltas(byte[] bytes, int count)
    {
        FontData data = FontData.Create(bytes);
        return SfntGvarPackedDataDecoder.ReadDeltas(
            new BigEndianReader(data.GetSlice(0, data.GetLength())),
            count,
            new SfntVariationParsingBudget(1000));
    }

    static byte[] BuildGvar(bool longOffsets)
    {
        byte[] glyph = BuildGlyphVariationData();
        int offsetArrayLength = longOffsets ? 16 : 8;
        int sharedOffset = 20 + offsetArrayLength;
        int glyphDataOffset = sharedOffset + 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt32((uint)sharedOffset)
            .WriteUInt16(3)
            .WriteUInt16((ushort)(longOffsets ? 1 : 0))
            .WriteUInt32((uint)glyphDataOffset);

        if (longOffsets)
        {
            builder
                .WriteUInt32(0)
                .WriteUInt32((uint)glyph.Length)
                .WriteUInt32((uint)glyph.Length)
                .WriteUInt32((uint)glyph.Length);
        }
        else
        {
            builder
                .WriteUInt16(0)
                .WriteUInt16((ushort)(glyph.Length / 2))
                .WriteUInt16((ushort)(glyph.Length / 2))
                .WriteUInt16((ushort)(glyph.Length / 2));
        }

        builder
            .WriteInt16(F2(1))
            .WriteInt16(F2(0))
            .WriteInt16(F2(0))
            .WriteInt16(F2(-1))
            .WriteBytes(glyph);
        return builder.CopyBytes();
    }

    static byte[] BuildGlyphVariationData()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0x8003)
            .WriteUInt16(28)
            .WriteUInt16(10)
            .WriteUInt16(0)
            .WriteUInt16(10)
            .WriteUInt16(0xE000)
            .WriteInt16(F2(0.5))
            .WriteInt16(F2(-0.5))
            .WriteInt16(F2(0))
            .WriteInt16(F2(-1))
            .WriteInt16(F2(1))
            .WriteInt16(F2(0))
            .WriteUInt16(11)
            .WriteUInt16(0x2001);

        WriteSharedPoints(builder);
        builder
            .WriteUInt8(0)
            .WriteInt8(10)
            .WriteUInt8(0x80)
            .WriteUInt8(0x40)
            .WriteInt16(300)
            .WriteUInt8(2)
            .WriteInt8(-1)
            .WriteInt8(2)
            .WriteInt8(-3)
            .WriteUInt8(2)
            .WriteUInt8(1)
            .WriteUInt8(0)
            .WriteUInt8(2)
            .WriteUInt8(0x81)
            .WriteUInt8(0x41)
            .WriteInt16(-300)
            .WriteInt16(400)
            .WriteUInt8(0);
        WriteZeroDeltas(builder, 300);
        WriteZeroDeltas(builder, 300);
        return builder.CopyBytes();
    }

    static void WriteSharedPoints(TestBinaryBuilder builder)
    {
        builder
            .WriteUInt8(3)
            .WriteUInt8(1)
            .WriteUInt8(1)
            .WriteUInt8(2)
            .WriteUInt8(0x80)
            .WriteUInt16(257);
    }

    static void WriteZeroDeltas(TestBinaryBuilder builder, int count)
    {
        int remaining = count;

        while (remaining > 0)
        {
            int runCount = System.Math.Min(remaining, 64);
            builder.WriteUInt8((byte)(0x80 | (runCount - 1)));
            remaining -= runCount;
        }
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
        WriteAxis(builder, "wght", 256);
        WriteAxis(builder, "wdth", 257);
        return builder.CopyBytes();
    }

    static byte[] BuildFvarOneAxis()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(8);
        WriteAxis(builder, "wght", 256);
        return builder.CopyBytes();
    }

    static void WriteAxis(TestBinaryBuilder builder, string tag, int nameId)
    {
        builder
            .WriteTag(tag)
            .WriteInt32(0)
            .WriteInt32(0)
            .WriteInt32(0x00010000)
            .WriteUInt16(0)
            .WriteUInt16((ushort)nameId);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildName()
    {
        byte[] weight = Encoding.BigEndianUnicode.GetBytes("Weight");
        byte[] width = Encoding.BigEndianUnicode.GetBytes("Width");
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(30)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(256)
            .WriteUInt16((ushort)weight.Length)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(257)
            .WriteUInt16((ushort)width.Length)
            .WriteUInt16((ushort)weight.Length)
            .WriteBytes(weight)
            .WriteBytes(width)
            .CopyBytes();
    }

    static SfntTableCache CreateCache(byte[] gvar, byte[]? fvar, byte[]? maxp)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (gvar != null)
        {
            tables.Add(CreateTable(FontTags.Gvar, gvar));
        }

        if (fvar != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, fvar));
            tables.Add(CreateTable(FontTags.Name, BuildName()));
        }

        if (maxp != null)
        {
            tables.Add(CreateTable(FontTags.Maxp, maxp));
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
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

    static short F2(double value)
    {
        return System.Convert.ToInt16(System.Math.Round(value * 16384));
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
