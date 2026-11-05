using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntCvarTableTests
{
    [Test]
    public void DecodesSharedPrivateAndAllControlValueDeltas()
    {
        SfntCvarTable? table = Parse(BuildCvar());
        SfntCvarTupleVariation shared = table!.GetTupleVariation(0);
        SfntCvarTupleVariation intermediate = table.GetTupleVariation(1);
        SfntCvarTupleVariation all = table.GetTupleVariation(2);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetControlValueCount(), Is.EqualTo(4));
            Assert.That(table.HasSharedControlValueNumbers(), Is.True);
            Assert.That(table.GetSharedControlValueNumbers()!.GetCount(), Is.EqualTo(2));
            Assert.That(table.GetSharedControlValueNumbers()!.GetPointNumber(0), Is.Zero);
            Assert.That(table.GetSharedControlValueNumbers()!.GetPointNumber(1), Is.EqualTo(2));
            Assert.That(table.GetTupleVariationCount(), Is.EqualTo(3));
            Assert.That(shared.GetVariationDataSize(), Is.EqualTo(3));
            Assert.That(shared.GetTupleIndex(), Is.EqualTo(5));
            Assert.That(shared.HasIntermediateRegion(), Is.False);
            Assert.That(shared.HasPrivateControlValueNumbers(), Is.False);
            Assert.That(shared.GetControlValueNumbers(),
                Is.SameAs(table.GetSharedControlValueNumbers()));
            Assert.That(shared.GetPeakTuple().GetCoordinate(0), Is.EqualTo(1));
            Assert.That(shared.GetDelta(0), Is.EqualTo(11));
            Assert.That(shared.GetDelta(1), Is.EqualTo(-21));
            Assert.That(intermediate.HasIntermediateRegion(), Is.True);
            Assert.That(intermediate.HasPrivateControlValueNumbers(), Is.True);
            Assert.That(intermediate.GetPeakTuple().GetCoordinate(0), Is.EqualTo(0.5));
            Assert.That(intermediate.GetIntermediateStartTuple()!.GetCoordinate(0),
                Is.EqualTo(0.25));
            Assert.That(intermediate.GetIntermediateEndTuple()!.GetCoordinate(0),
                Is.EqualTo(0.75));
            Assert.That(intermediate.GetControlValueNumbers().GetPointNumber(0),
                Is.EqualTo(1));
            Assert.That(intermediate.GetControlValueNumbers().GetPointNumber(1),
                Is.EqualTo(3));
            Assert.That(intermediate.GetDelta(0), Is.EqualTo(30));
            Assert.That(intermediate.GetDelta(1), Is.EqualTo(40));
            Assert.That(all.GetControlValueNumbers().UsesAllPoints(), Is.True);
            Assert.That(all.GetDeltaCount(), Is.EqualTo(4));
            Assert.That(all.GetDelta(3), Is.EqualTo(8));
        });
    }

    [Test]
    public void AppliesScalarsWithoutInferringOmittedControlValues()
    {
        SfntCvarTable? table = Parse(BuildCvar());
        byte[] source = BuildCvt();

        Assert.Multiple(() =>
        {
            Assert.That(table!.Apply(source, new double[] { 0.5 }), Is.EqualTo(
                ControlValues(106, 230, -111, 40)));
            Assert.That(table.Apply(source, new double[] { -0.5 }), Is.EqualTo(
                ControlValues(101, 202, -97, 4)));
            Assert.That(table.Apply(source, new double[] { 0 }), Is.EqualTo(source));
            Assert.That(source, Is.EqualTo(BuildCvt()));
        });
    }

    [Test]
    public void RejectsMalformedHeadersTupleDataAndControlValueIndexes()
    {
        byte[] reservedCount = BuildCvar();
        PatchUInt16(reservedCount, 4, 0x9003);
        byte[] zeroCount = BuildCvar();
        PatchUInt16(zeroCount, 4, 0x8000);
        byte[] earlyData = BuildCvar();
        PatchUInt16(earlyData, 6, 8);
        byte[] outsideData = BuildCvar();
        PatchUInt16(outsideData, 6, 1000);
        byte[] reservedTuple = BuildCvar();
        PatchUInt16(reservedTuple, 10, 0x9005);
        byte[] missingPeak = BuildCvar();
        PatchUInt16(missingPeak, 10, 5);
        byte[] outsideControlValue = BuildCvar();
        outsideControlValue[33] = 4;
        byte[] truncatedTuple = BuildCvar();
        PatchUInt16(truncatedTuple, 8, 20);
        byte[] unusedTupleData = BuildCvar();
        PatchUInt16(unusedTupleData, 8, 4);
        byte[] trailingData = BuildCvar().Concat(new byte[] { 0 }).ToArray();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(BuildCvar()[..7]));
            Assert.Throws<FontFormatException>(() => Parse(reservedCount));
            Assert.Throws<FontFormatException>(() => Parse(zeroCount));
            Assert.Throws<FontFormatException>(() => Parse(earlyData));
            Assert.Throws<FontFormatException>(() => Parse(outsideData));
            Assert.Throws<FontFormatException>(() => Parse(reservedTuple));
            Assert.Throws<FontFormatException>(() => Parse(missingPeak));
            Assert.Throws<FontFormatException>(() => Parse(outsideControlValue));
            Assert.Throws<FontFormatException>(() => Parse(truncatedTuple));
            Assert.Throws<FontFormatException>(() => Parse(unusedTupleData));
            Assert.Throws<FontFormatException>(() => Parse(trailingData));
        });
    }

    [Test]
    public void RequiresFvarAndNonemptyCvtDependencies()
    {
        byte[] cvar = BuildCvar();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntCvarTableParser.FindTable(CreateCache(cvar, null, BuildCvt())));
            Assert.Throws<FontFormatException>(() =>
                SfntCvarTableParser.FindTable(CreateCache(cvar, BuildFvar(), null)));
            Assert.Throws<FontFormatException>(() =>
                SfntCvarTableParser.FindTable(CreateCache(
                    cvar,
                    BuildFvar(),
                    new byte[] { 0 })));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndValidatesArguments()
    {
        SfntTableCache cache = CreateCache(BuildCvar(), BuildFvar(), BuildCvt());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        SfntCvarTable? table = face.GetCvarTable();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetCvarTableView()!.GetParsedData(), Is.SameAs(table));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetTupleVariation(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetTupleVariation(0).GetDelta(2));
            Assert.Throws<ArgumentNullException>(() =>
                table!.Apply(null!, new double[] { 0 }));
            Assert.Throws<ArgumentNullException>(() =>
                table!.Apply(BuildCvt(), null!));
            Assert.Throws<ArgumentException>(() =>
                table!.Apply(new byte[] { 0, 0 }, new double[] { 0 }));
            Assert.Throws<ArgumentException>(() =>
                table!.Apply(BuildCvt(), Array.Empty<double>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.Apply(BuildCvt(), new double[] { 2 }));
        });
    }

    static SfntCvarTable? Parse(byte[] bytes)
    {
        return SfntCvarTableParser.FindTable(CreateCache(
            bytes,
            BuildFvar(),
            BuildCvt()));
    }

    static byte[] BuildCvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0x8003)
            .WriteUInt16(30)
            .WriteUInt16(3)
            .WriteUInt16(0x8005)
            .WriteInt16(F2(1))
            .WriteUInt16(7)
            .WriteUInt16(0xE002)
            .WriteInt16(F2(0.5))
            .WriteInt16(F2(0.25))
            .WriteInt16(F2(0.75))
            .WriteUInt16(6)
            .WriteUInt16(0xA000)
            .WriteInt16(F2(-1))
            .WriteUInt8(2)
            .WriteUInt8(1)
            .WriteUInt8(0)
            .WriteUInt8(2)
            .WriteUInt8(1)
            .WriteInt8(11)
            .WriteInt8(-21)
            .WriteUInt8(2)
            .WriteUInt8(1)
            .WriteUInt8(1)
            .WriteUInt8(2)
            .WriteUInt8(1)
            .WriteInt8(30)
            .WriteInt8(40)
            .WriteUInt8(0)
            .WriteUInt8(3)
            .WriteInt8(2)
            .WriteInt8(4)
            .WriteInt8(6)
            .WriteInt8(8)
            .CopyBytes();
    }

    static byte[] BuildCvt()
    {
        return ControlValues(100, 200, -100, 0);
    }

    static byte[] ControlValues(params short[] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();

        for (int index = 0; index < values.Length; index += 1)
        {
            builder.WriteInt16(values[index]);
        }

        return builder.CopyBytes();
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

    static byte[] BuildName()
    {
        byte[] value = Encoding.BigEndianUnicode.GetBytes("Weight");
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

    static SfntTableCache CreateCache(byte[] cvar, byte[]? fvar, byte[]? cvt)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (cvar != null)
        {
            tables.Add(CreateTable(FontTags.Cvar, cvar));
        }

        if (fvar != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, fvar));
            tables.Add(CreateTable(FontTags.Name, BuildName()));
        }

        if (cvt != null)
        {
            tables.Add(CreateTable(FontTags.Cvt, cvt));
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
}
