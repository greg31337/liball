using ttf;

namespace ttf.Tests;

internal class SfntGposValueRecordFixture
{
    internal byte[]? Bytes;
    internal int ParentOffset;
    internal int RecordOffset;
    internal int XPlacementAdjustment;
}

internal class SfntGposValueRecordTests
{
    [Test]
    public void ParsesEveryValueFieldAndParentRelativeAdjustment()
    {
        SfntGposValueRecordFixture fixture = BuildValueRecord();
        SfntGposValueRecord record = Parse(
            fixture,
            0x00FF,
            16);

        Assert.Multiple(() =>
        {
            Assert.That(SfntGposValueRecordParser.GetRecordSize(0x00FF),
                Is.EqualTo(16));
            Assert.That(record.GetValueFormat(), Is.EqualTo(0x00FF));
            Assert.That(record.GetXPlacement(), Is.EqualTo(-10));
            Assert.That(record.GetYPlacement(), Is.EqualTo(20));
            Assert.That(record.GetXAdvance(), Is.EqualTo(30));
            Assert.That(record.GetYAdvance(), Is.EqualTo(-40));
            Assert.That(record.GetXPlacementAdjustmentOffset(),
                Is.EqualTo(16));
            Assert.That(record.GetXPlacementAdjustmentTableOffset(),
                Is.EqualTo(fixture.XPlacementAdjustment));
            Assert.That(record.GetXPlacementAdjustment()!.IsDevice(), Is.True);
            Assert.That(record.GetXPlacementAdjustment()!.GetDevice()!
                .GetDelta(20), Is.EqualTo(1));
            Assert.That(record.GetYPlacementAdjustment()!.IsVariationIndex(),
                Is.True);
            Assert.That(record.GetYPlacementAdjustment()!.GetVariationIndex()!
                .GetInnerIndex(), Is.Zero);
            Assert.That(record.GetXAdvanceAdjustment()!.GetDevice()!
                .GetDelta(20), Is.EqualTo(-1));
            Assert.That(record.GetYAdvanceAdjustment()!.GetVariationIndex()!
                .GetInnerIndex(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ParsesSparseAndEmptyValueRecordsInFieldOrder()
    {
        byte[] sparseBytes = new TestBinaryBuilder()
            .WriteInt16(-12)
            .WriteInt16(34)
            .CopyBytes();
        SfntGposValueRecord sparse = Parse(
            sparseBytes,
            0,
            0,
            0x0009,
            0);
        SfntGposValueRecord empty = Parse(
            Array.Empty<byte>(),
            0,
            0,
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(SfntGposValueRecordParser.GetRecordSize(0x0009),
                Is.EqualTo(4));
            Assert.That(sparse.GetXPlacement(), Is.EqualTo(-12));
            Assert.That(sparse.GetYPlacement(), Is.Zero);
            Assert.That(sparse.GetXAdvance(), Is.Zero);
            Assert.That(sparse.GetYAdvance(), Is.EqualTo(34));
            Assert.That(empty.GetValueFormat(), Is.Zero);
            Assert.That(empty.GetXPlacement(), Is.Zero);
            Assert.That(empty.GetXPlacementAdjustment(), Is.Null);
        });
    }

    [Test]
    public void ResolvesDeviceAndVariationAdjustmentsInDesignUnits()
    {
        SfntGposValueRecord record = Parse(
            BuildValueRecord(),
            0x00FF,
            16);
        SfntItemVariationStore store = CreateVariationStore();
        SfntVariationLocation location = store.CreateLocation(
            new double[] { 0.5 });
        SfntGposAdjustmentContext context =
            new SfntGposAdjustmentContext(1000, 20, 10, location);
        SfntGposAdjustment adjustment = context.ResolveValueRecord(record);

        Assert.Multiple(() =>
        {
            Assert.That(adjustment.GetXPlacement(), Is.EqualTo(40));
            Assert.That(adjustment.GetYPlacement(), Is.EqualTo(70));
            Assert.That(adjustment.GetXAdvance(), Is.EqualTo(-20));
            Assert.That(adjustment.GetYAdvance(), Is.EqualTo(-65));
        });
    }

    [Test]
    public void ResolvesAllAnchorFormatsAndPreservesContourPoint()
    {
        SfntGposAdjustmentContext context =
            new SfntGposAdjustmentContext(1000, 20, 10, null);
        SfntLayoutAnchor format1 = CreateAnchor(1, null, null);
        SfntLayoutAnchor format2 = new SfntLayoutAnchor(
            2,
            100,
            -50,
            7,
            0,
            0,
            0,
            0,
            null,
            null);
        SfntDeviceOrVariationIndex xDevice =
            SfntDeviceOrVariationIndex.Create(
                new SfntDeviceTable(20, 20, 1, new int[] { 0x4000 }));
        SfntDeviceOrVariationIndex yDevice =
            SfntDeviceOrVariationIndex.Create(
                new SfntDeviceTable(10, 10, 1, new int[] { 0xC000 }));
        SfntLayoutAnchor format3 = CreateAnchor(3, xDevice, yDevice);
        SfntGposAnchorPosition first = context.ResolveAnchor(format1);
        SfntGposAnchorPosition second = context.ResolveAnchor(format2);
        SfntGposAnchorPosition third = context.ResolveAnchor(format3);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetXCoordinate(), Is.EqualTo(100));
            Assert.That(first.GetYCoordinate(), Is.EqualTo(-50));
            Assert.That(first.HasAnchorPoint(), Is.False);
            Assert.That(second.GetXCoordinate(), Is.EqualTo(100));
            Assert.That(second.HasAnchorPoint(), Is.True);
            Assert.That(second.GetAnchorPoint(), Is.EqualTo(7));
            Assert.That(third.GetXCoordinate(), Is.EqualTo(150));
            Assert.That(third.GetYCoordinate(), Is.EqualTo(-150));
        });
    }

    [Test]
    public void RequiresVariationLocationAndOmitsDeviceWithoutSize()
    {
        SfntGposValueRecord variableRecord = Parse(
            BuildValueRecord(),
            0x00FF,
            16);
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(-10)
            .WriteUInt16(4);
        WriteDevice(builder, 20, 0x4000);
        SfntGposValueRecord deviceRecord = Parse(
            builder.CopyBytes(),
            0,
            0,
            0x0011,
            4);
        SfntGposAdjustmentContext context =
            new SfntGposAdjustmentContext(1000, 0, 0, null);
        SfntGposAdjustment adjustment =
            context.ResolveValueRecord(deviceRecord);

        Assert.Multiple(() =>
        {
            Assert.That(adjustment.GetXPlacement(), Is.EqualTo(-10));
            Assert.That(adjustment.GetYPlacement(), Is.Zero);
            Assert.Throws<InvalidOperationException>(() =>
                context.ResolveValueRecord(variableRecord));
        });
    }

    [Test]
    public void ValidatesValueAndAnchorVariationIndexesAgainstGdefStore()
    {
        SfntItemVariationStore store = CreateVariationStore();
        SfntGposValueRecord valid = Parse(
            BuildValueRecord(),
            0x00FF,
            16);
        SfntDeviceOrVariationIndex invalidVariation =
            SfntDeviceOrVariationIndex.Create(new SfntVariationIndex(0, 2));
        SfntGposValueRecord invalid = CreateRecord(invalidVariation);
        SfntLayoutAnchor anchor = CreateAnchor(
            3,
            SfntDeviceOrVariationIndex.Create(new SfntVariationIndex(0, 0)),
            null);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => valid.ValidateVariationIndexes(store));
            Assert.Throws<FontFormatException>(() =>
                valid.ValidateVariationIndexes(null));
            Assert.Throws<FontFormatException>(() =>
                invalid.ValidateVariationIndexes(store));
            Assert.DoesNotThrow(() =>
                SfntGposAdjustmentValidator.ValidateAnchorVariationIndexes(
                    anchor,
                    store));
            Assert.Throws<FontFormatException>(() =>
                SfntGposAdjustmentValidator.ValidateAnchorVariationIndexes(
                    anchor,
                    null));
        });
    }

    [Test]
    public void ValidatesParserAndEvaluationArguments()
    {
        FontData emptyData = FontData.Create(Array.Empty<byte>());
        FontDataSlice empty = emptyData.GetSlice(0, 0);
        SfntGposAdjustmentContext context =
            new SfntGposAdjustmentContext(1000, 0, 0, null);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGposValueRecordParser.GetRecordSize(-1));
            Assert.Throws<FontFormatException>(() =>
                SfntGposValueRecordParser.GetRecordSize(0x0100));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposValueRecordParser.Parse(
                    null!, 0, 0, 0, 0, FontLimits.CreateCompatible()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposValueRecordParser.Parse(
                    empty, 0, 0, 0, 0, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntGposAdjustmentContext(0, 0, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntGposAdjustmentContext(1000, -1, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntGposAdjustmentContext(1000, 0, 65536, null));
            Assert.Throws<ArgumentNullException>(() =>
                context.ResolveValueRecord(null!));
            Assert.Throws<ArgumentNullException>(() =>
                context.ResolveAnchor(null));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGposAdjustmentValidator.ValidateAnchorVariationIndexes(
                    null!,
                    null));
        });
    }

    [TestCaseSource(nameof(InvalidValueRecords))]
    public void RejectsMalformedValueRecords(
        byte[] bytes,
        int valueFormat,
        int minimumAdjustmentOffset)
    {
        Assert.Throws<FontFormatException>(() => Parse(
            bytes,
            0,
            0,
            valueFormat,
            minimumAdjustmentOffset));
    }

    static IEnumerable<TestCaseData> InvalidValueRecords()
    {
        yield return new TestCaseData(
            new byte[] { 0 },
            0x0001,
            0).SetName("RejectsTruncatedValueRecord");

        byte[] intoRecord = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteZeros(6)
            .CopyBytes();
        yield return new TestCaseData(
            intoRecord,
            0x0010,
            8).SetName("RejectsAdjustmentIntoValueRecord");

        byte[] outside = new TestBinaryBuilder()
            .WriteUInt16(100)
            .WriteZeros(6)
            .CopyBytes();
        yield return new TestCaseData(
            outside,
            0x0010,
            2).SetName("RejectsAdjustmentOutsideTable");

        byte[] badDevice = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
        yield return new TestCaseData(
            badDevice,
            0x0010,
            2).SetName("RejectsMalformedNestedDevice");
    }

    static SfntGposValueRecordFixture BuildValueRecord()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder().WriteZeros(4);
        SfntGposValueRecordFixture fixture = new SfntGposValueRecordFixture();
        fixture.ParentOffset = builder.GetLength();
        fixture.RecordOffset = builder.GetLength();
        builder.WriteInt16(-10).WriteInt16(20)
            .WriteInt16(30).WriteInt16(-40)
            .WriteZeros(8);
        fixture.XPlacementAdjustment = builder.GetLength();
        builder.PatchUInt16(fixture.RecordOffset + 8,
            (ushort)(fixture.XPlacementAdjustment - fixture.ParentOffset));
        WriteDevice(builder, 20, 0x4000);
        int yPlacementAdjustment = builder.GetLength();
        builder.PatchUInt16(fixture.RecordOffset + 10,
            (ushort)(yPlacementAdjustment - fixture.ParentOffset));
        WriteVariationIndex(builder, 0, 0);
        int xAdvanceAdjustment = builder.GetLength();
        builder.PatchUInt16(fixture.RecordOffset + 12,
            (ushort)(xAdvanceAdjustment - fixture.ParentOffset));
        WriteDevice(builder, 20, 0xC000);
        int yAdvanceAdjustment = builder.GetLength();
        builder.PatchUInt16(fixture.RecordOffset + 14,
            (ushort)(yAdvanceAdjustment - fixture.ParentOffset));
        WriteVariationIndex(builder, 0, 1);
        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static void WriteDevice(
        TestBinaryBuilder builder,
        int pixelsPerEm,
        int packedValue)
    {
        builder.WriteUInt16((ushort)pixelsPerEm)
            .WriteUInt16((ushort)pixelsPerEm)
            .WriteUInt16(1)
            .WriteUInt16((ushort)packedValue);
    }

    static void WriteVariationIndex(
        TestBinaryBuilder builder,
        int outerIndex,
        int innerIndex)
    {
        builder.WriteUInt16((ushort)outerIndex)
            .WriteUInt16((ushort)innerIndex)
            .WriteUInt16(0x8000);
    }

    static SfntItemVariationStore CreateVariationStore()
    {
        SfntVariationRegion region = new SfntVariationRegion(
            new SfntVariationRegionAxis[]
            {
                new SfntVariationRegionAxis(0, 1, 1)
            });
        SfntItemVariationData data = new SfntItemVariationData(
            2,
            1,
            false,
            new int[] { 0 },
            new int[][]
            {
                new int[] { 100 },
                new int[] { -50 }
            });
        return new SfntItemVariationStore(
            0,
            1,
            1,
            new SfntVariationRegion[] { region },
            new SfntItemVariationData[] { data });
    }

    static SfntLayoutAnchor CreateAnchor(
        int format,
        SfntDeviceOrVariationIndex? xAdjustment,
        SfntDeviceOrVariationIndex? yAdjustment)
    {
        return new SfntLayoutAnchor(
            format,
            100,
            -50,
            0,
            0,
            0,
            0,
            0,
            xAdjustment,
            yAdjustment);
    }

    static SfntGposValueRecord CreateRecord(
        SfntDeviceOrVariationIndex xPlacementAdjustment)
    {
        return new SfntGposValueRecord(
            0x0010,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            xPlacementAdjustment,
            null,
            null,
            null);
    }

    static SfntGposValueRecord Parse(
        SfntGposValueRecordFixture fixture,
        int valueFormat,
        int minimumAdjustmentOffset)
    {
        return Parse(
            fixture.Bytes,
            fixture.RecordOffset,
            fixture.ParentOffset,
            valueFormat,
            minimumAdjustmentOffset);
    }

    static SfntGposValueRecord Parse(
        byte[]? bytes,
        int recordOffset,
        int parentOffset,
        int valueFormat,
        int minimumAdjustmentOffset)
    {
        FontData data = FontData.Create(bytes!);
        return SfntGposValueRecordParser.Parse(
            data.GetSlice(0, data.GetLength()),
            recordOffset,
            valueFormat,
            parentOffset,
            minimumAdjustmentOffset,
            FontLimits.CreateCompatible());
    }
}
