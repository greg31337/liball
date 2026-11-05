namespace ttf.Tests;

internal class SfntLayoutPrimitiveTests
{
    [Test]
    public void ParsesPackedDeviceFormatsAndSignedDeltas()
    {
        SfntDeviceTable? twoBit = ParseDevice(
            BuildDevice(10, 13, 1, 0x7800));
        SfntDeviceTable? fourBit = ParseDevice(
            BuildDevice(20, 23, 2, 0x123F));
        SfntDeviceTable? eightBit = ParseDevice(
            BuildDevice(30, 31, 3, 0x7F80));

        Assert.Multiple(() =>
        {
            Assert.That(twoBit!.GetStartSize(), Is.EqualTo(10));
            Assert.That(twoBit.GetEndSize(), Is.EqualTo(13));
            Assert.That(twoBit.GetDeltaFormat(), Is.EqualTo(1));
            Assert.That(twoBit.GetPackedValueCount(), Is.EqualTo(1));
            Assert.That(twoBit.GetPackedValue(0), Is.EqualTo(0x7800));
            Assert.That(twoBit.GetDelta(9), Is.Zero);
            Assert.That(twoBit.GetDelta(10), Is.EqualTo(1));
            Assert.That(twoBit.GetDelta(11), Is.EqualTo(-1));
            Assert.That(twoBit.GetDelta(12), Is.EqualTo(-2));
            Assert.That(twoBit.GetDelta(13), Is.Zero);
            Assert.That(fourBit!.GetDelta(20), Is.EqualTo(1));
            Assert.That(fourBit.GetDelta(21), Is.EqualTo(2));
            Assert.That(fourBit.GetDelta(22), Is.EqualTo(3));
            Assert.That(fourBit.GetDelta(23), Is.EqualTo(-1));
            Assert.That(eightBit!.GetDelta(30), Is.EqualTo(127));
            Assert.That(eightBit.GetDelta(31), Is.EqualTo(-128));
        });
    }

    [Test]
    public void ParsesVariationIndexThroughSharedDeviceFields()
    {
        SfntDeviceOrVariationIndex result = ParseAdjustment(
            BuildVariationIndex(2, 5));
        SfntVariationIndex? variation = result.GetVariationIndex();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsDevice(), Is.False);
            Assert.That(result.IsVariationIndex(), Is.True);
            Assert.That(result.GetDevice(), Is.Null);
            Assert.That(variation!.GetOuterIndex(), Is.EqualTo(2));
            Assert.That(variation.GetInnerIndex(), Is.EqualTo(5));
        });
    }

    [Test]
    public void ParsesAllAnchorFormatsAndAdjustmentOffsetBases()
    {
        SfntLayoutAnchor format1 = ParseAnchor(
            new TestBinaryBuilder()
                .WriteUInt16(1).WriteInt16(100).WriteInt16(-50)
                .CopyBytes());
        SfntLayoutAnchor format2 = ParseAnchor(
            new TestBinaryBuilder()
                .WriteUInt16(2).WriteInt16(200).WriteInt16(300)
                .WriteUInt16(17)
                .CopyBytes());
        byte[] format3Bytes = BuildAnchor3();
        SfntLayoutAnchor format3 = ParseAnchor(format3Bytes);
        SfntLayoutAnchor format3WithoutAdjustments = ParseAnchor(
            BuildAnchor3WithOffsets(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(format1.GetFormat(), Is.EqualTo(1));
            Assert.That(format1.GetXCoordinate(), Is.EqualTo(100));
            Assert.That(format1.GetYCoordinate(), Is.EqualTo(-50));
            Assert.That(format1.HasAnchorPoint(), Is.False);
            Assert.That(format1.GetXAdjustment(), Is.Null);
            Assert.That(format2.HasAnchorPoint(), Is.True);
            Assert.That(format2.GetAnchorPoint(), Is.EqualTo(17));
            Assert.That(format3.GetXAdjustmentOffset(), Is.EqualTo(10));
            Assert.That(format3.GetYAdjustmentOffset(), Is.EqualTo(18));
            Assert.That(format3.GetXAdjustmentTableOffset(), Is.EqualTo(14));
            Assert.That(format3.GetYAdjustmentTableOffset(), Is.EqualTo(22));
            Assert.That(format3.GetXAdjustment()!.IsDevice(), Is.True);
            Assert.That(format3.GetXAdjustment()!.GetDevice()!.GetDelta(15),
                Is.EqualTo(-1));
            Assert.That(format3.GetYAdjustment()!.IsVariationIndex(), Is.True);
            Assert.That(format3.GetYAdjustment()!.GetVariationIndex()!
                .GetOuterIndex(), Is.EqualTo(2));
            Assert.That(format3WithoutAdjustments.GetXAdjustment(), Is.Null);
            Assert.That(format3WithoutAdjustments.GetYAdjustment(), Is.Null);
            Assert.That(format3WithoutAdjustments.GetXAdjustmentTableOffset(),
                Is.Zero);
        });
    }

    [Test]
    public void ParsesMarkArrayClassesAndAnchorsFromArrayBase()
    {
        SfntMarkArray marks = ParseMarkArray(BuildMarkArray(), 2);
        SfntMarkRecord first = marks.GetMark(0);
        SfntMarkRecord second = marks.GetMark(1);

        Assert.Multiple(() =>
        {
            Assert.That(marks.GetMarkCount(), Is.EqualTo(2));
            Assert.That(first.GetMarkClass(), Is.Zero);
            Assert.That(first.GetAnchorOffset(), Is.EqualTo(10));
            Assert.That(first.GetAnchorTableOffset(), Is.EqualTo(14));
            Assert.That(first.GetAnchor().GetXCoordinate(), Is.EqualTo(100));
            Assert.That(second.GetMarkClass(), Is.EqualTo(1));
            Assert.That(second.GetAnchorOffset(), Is.EqualTo(16));
            Assert.That(second.GetAnchorTableOffset(), Is.EqualTo(20));
            Assert.That(second.GetAnchor().GetYCoordinate(), Is.EqualTo(-200));
        });
    }

    [Test]
    public void ParsesSequenceLookupRecordsInSourceOrder()
    {
        byte[] bytes = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(1)
            .CopyBytes();
        SfntSequenceLookupRecords records = ParseSequenceRecords(
            bytes,
            2,
            3,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(records.GetRecordCount(), Is.EqualTo(2));
            Assert.That(records.GetRecord(0).GetSequenceIndex(), Is.EqualTo(1));
            Assert.That(records.GetRecord(0).GetLookupListIndex(), Is.EqualTo(2));
            Assert.That(records.GetRecord(1).GetSequenceIndex(), Is.Zero);
            Assert.That(records.GetRecord(1).GetLookupListIndex(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ParsesExtensionLookupTypeAndThirtyTwoBitOffset()
    {
        byte[] bytes = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt32(8)
            .WriteUInt16(1)
            .CopyBytes();
        SfntExtensionLookup extension = ParseExtension(bytes, 7);

        Assert.Multiple(() =>
        {
            Assert.That(extension.GetLookupType(), Is.EqualTo(2));
            Assert.That(extension.GetExtensionOffset(), Is.EqualTo(8));
            Assert.That(extension.GetExtensionTableOffset(), Is.EqualTo(12));
        });
    }

    [TestCaseSource(nameof(InvalidDeviceTables))]
    public void RejectsMalformedDeviceAndVariationIndexTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseAdjustment(bytes));
    }

    [TestCaseSource(nameof(InvalidAnchorTables))]
    public void RejectsMalformedAnchorsAndAdjustmentOffsets(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseAnchor(bytes));
    }

    [TestCaseSource(nameof(InvalidMarkArrays))]
    public void RejectsMalformedMarkArrays(byte[] bytes, int classCount)
    {
        Assert.Throws<FontFormatException>(() =>
            ParseMarkArray(bytes, classCount));
    }

    [Test]
    public void RejectsInvalidSequenceRecordsAndExtensionOffsets()
    {
        byte[] invalidSequence = new TestBinaryBuilder()
            .WriteUInt16(3).WriteUInt16(0)
            .CopyBytes();
        byte[] invalidLookup = new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(4)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseSequenceRecords(
                invalidSequence,
                1,
                3,
                5));
            Assert.Throws<FontFormatException>(() => ParseSequenceRecords(
                invalidLookup,
                1,
                3,
                4));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(2, 2, 8),
                7));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(1, 7, 8),
                7));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(1, 0, 8),
                7));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(1, 2, 4),
                7));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(1, 2, 100),
                7));
            Assert.Throws<FontFormatException>(() => ParseExtension(
                BuildExtension(1, 2, 0x80000000U),
                7));
        });
    }

    [Test]
    public void EnforcesEntryLimitsAndValidatesPublicParserArguments()
    {
        FontLimits oneEntry = CreateLimits(1);
        FontData data = WithPrefix(BuildDevice(10, 10, 1, 0));
        FontDataSlice slice = data.GetSlice(0, data.GetLength());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => ParseAdjustment(
                BuildDevice(10, 12, 3, 0, 0),
                oneEntry));
            Assert.Throws<FontLimitException>(() => ParseMarkArray(
                BuildMarkArray(),
                2,
                oneEntry));
            Assert.Throws<FontLimitException>(() => ParseSequenceRecords(
                new byte[8],
                2,
                2,
                2,
                oneEntry));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutPrimitiveParser.ParseAnchor(
                    null!,
                    0,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
                    slice,
                    4,
                    null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutPrimitiveParser.ParseMarkArray(
                    slice,
                    4,
                    -1,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                    slice,
                    4,
                    -1,
                    1,
                    1,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                    slice,
                    4,
                    0,
                    0,
                    1,
                    FontLimits.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutPrimitiveParser.ParseExtension(
                    slice,
                    4,
                    0,
                    FontLimits.CreateStrict()));
        });
    }

    static IEnumerable<TestCaseData> InvalidDeviceTables()
    {
        yield return InvalidCase(
            BuildDevice(12, 11, 1, 0),
            "ReversedSizeRange");
        yield return InvalidCase(
            BuildDevice(10, 10, 4, 0),
            "ReservedDeltaFormat");
        yield return InvalidCase(
            BuildDevice(10, 12, 3, 0),
            "TruncatedDeltaArray");
        yield return InvalidCase(
            new byte[] { 0, 1, 0, 2, 0x80 },
            "TruncatedVariationIndex");
    }

    static IEnumerable<TestCaseData> InvalidAnchorTables()
    {
        yield return InvalidCase(
            new byte[] { 0, 1, 0, 0 },
            "TruncatedHeader");
        yield return InvalidCase(
            new TestBinaryBuilder()
                .WriteUInt16(4).WriteInt16(0).WriteInt16(0)
                .CopyBytes(),
            "UnsupportedFormat");
        yield return InvalidCase(
            new TestBinaryBuilder()
                .WriteUInt16(2).WriteInt16(0).WriteInt16(0)
                .CopyBytes(),
            "TruncatedFormat2");
        yield return InvalidCase(
            BuildAnchor3WithOffsets(2, 0),
            "AdjustmentIntoAnchorHeader");
        yield return InvalidCase(
            BuildAnchor3WithOffsets(100, 0),
            "AdjustmentOutsideTable");
        byte[] invalidNestedDevice = BuildAnchor3WithOffsets(10, 0);
        yield return InvalidCase(invalidNestedDevice, "TruncatedNestedDevice");
    }

    static IEnumerable<TestCaseData> InvalidMarkArrays()
    {
        byte[] invalidClass = BuildMarkArray();
        invalidClass[6] = 0;
        invalidClass[7] = 2;
        yield return new TestCaseData(invalidClass, 2)
            .SetName("Invalid_MarkClass");

        byte[] nullAnchor = BuildMarkArray();
        nullAnchor[4] = 0;
        nullAnchor[5] = 0;
        yield return new TestCaseData(nullAnchor, 2)
            .SetName("Invalid_NullMarkAnchor");

        byte[] anchorIntoRecords = BuildMarkArray();
        anchorIntoRecords[4] = 0;
        anchorIntoRecords[5] = 2;
        yield return new TestCaseData(anchorIntoRecords, 2)
            .SetName("Invalid_MarkAnchorIntoRecords");

        byte[] truncated = BuildMarkArray().AsSpan(0, 8).ToArray();
        yield return new TestCaseData(truncated, 2)
            .SetName("Invalid_TruncatedMarkRecords");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Invalid_" + name);
    }

    static byte[] BuildDevice(
        int startSize,
        int endSize,
        int deltaFormat,
        params int[] packedValues)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)startSize)
            .WriteUInt16((ushort)endSize)
            .WriteUInt16((ushort)deltaFormat);
        int index = 0;

        while (index < packedValues.Length)
        {
            builder.WriteUInt16((ushort)packedValues[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildVariationIndex(int outerIndex, int innerIndex)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)outerIndex)
            .WriteUInt16((ushort)innerIndex)
            .WriteUInt16(0x8000)
            .CopyBytes();
    }

    static byte[] BuildAnchor3()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(3)
            .WriteInt16(100)
            .WriteInt16(-50)
            .WriteUInt16(10)
            .WriteUInt16(18)
            .WriteBytes(BuildDevice(12, 15, 2, 0x123F))
            .WriteBytes(BuildVariationIndex(2, 5));
        return builder.CopyBytes();
    }

    static byte[] BuildAnchor3WithOffsets(int xOffset, int yOffset)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(3)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16((ushort)xOffset)
            .WriteUInt16((ushort)yOffset)
            .CopyBytes();
    }

    static byte[] BuildMarkArray()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(10)
            .WriteUInt16(1).WriteUInt16(16)
            .WriteUInt16(1).WriteInt16(100).WriteInt16(200)
            .WriteUInt16(1).WriteInt16(-100).WriteInt16(-200)
            .CopyBytes();
    }

    static byte[] BuildExtension(
        int format,
        int lookupType,
        uint extensionOffset)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)format)
            .WriteUInt16((ushort)lookupType)
            .WriteUInt32(extensionOffset)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static SfntDeviceOrVariationIndex ParseAdjustment(byte[] structure)
    {
        return ParseAdjustment(structure, FontLimits.CreateStrict());
    }

    static SfntDeviceOrVariationIndex ParseAdjustment(
        byte[] structure,
        FontLimits limits)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
            data.GetSlice(0, data.GetLength()),
            4,
            limits);
    }

    static SfntDeviceTable? ParseDevice(byte[] structure)
    {
        SfntDeviceOrVariationIndex result = ParseAdjustment(structure);
        Assert.That(result.IsDevice(), Is.True);
        return result.GetDevice();
    }

    static SfntLayoutAnchor ParseAnchor(byte[] structure)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutPrimitiveParser.ParseAnchor(
            data.GetSlice(0, data.GetLength()),
            4,
            FontLimits.CreateStrict());
    }

    static SfntMarkArray ParseMarkArray(
        byte[] structure,
        int classCount)
    {
        return ParseMarkArray(
            structure,
            classCount,
            FontLimits.CreateStrict());
    }

    static SfntMarkArray ParseMarkArray(
        byte[] structure,
        int classCount,
        FontLimits limits)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutPrimitiveParser.ParseMarkArray(
            data.GetSlice(0, data.GetLength()),
            4,
            classCount,
            limits);
    }

    static SfntSequenceLookupRecords ParseSequenceRecords(
        byte[] structure,
        int recordCount,
        int inputGlyphCount,
        int lookupCount)
    {
        return ParseSequenceRecords(
            structure,
            recordCount,
            inputGlyphCount,
            lookupCount,
            FontLimits.CreateStrict());
    }

    static SfntSequenceLookupRecords ParseSequenceRecords(
        byte[] structure,
        int recordCount,
        int inputGlyphCount,
        int lookupCount,
        FontLimits limits)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
            data.GetSlice(0, data.GetLength()),
            4,
            recordCount,
            inputGlyphCount,
            lookupCount,
            limits);
    }

    static SfntExtensionLookup ParseExtension(
        byte[] structure,
        int extensionLookupType)
    {
        FontData data = WithPrefix(structure);
        return SfntLayoutPrimitiveParser.ParseExtension(
            data.GetSlice(0, data.GetLength()),
            4,
            extensionLookupType,
            FontLimits.CreateStrict());
    }

    static FontData WithPrefix(byte[] structure)
    {
        byte[] bytes = new byte[structure.Length + 4];
        structure.CopyTo(bytes, 4);
        return FontData.Create(bytes);
    }

    static FontLimits CreateLimits(int maxEntries)
    {
        return FontLimits.Create(
            100000,
            100000,
            10,
            10,
            65535,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            maxEntries,
            100,
            100000,
            10,
            100,
            10,
            10);
    }
}
