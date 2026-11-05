using ttf;

namespace ttf.Tests;

internal class SfntGposCursiveFixture
{
    internal byte[]? Bytes;
    internal int Subtable;
}

internal class SfntGposCursiveTests
{
    [Test]
    public void ParsesEntryExitRecordsAndNullableAnchors()
    {
        SfntGposCursiveAdjustment cursive = Parse(
            BuildCursiveGpos(0).Bytes)!.GetLookup(0)
            .GetCursiveAdjustment(0);
        SfntGposEntryExitRecord first = cursive.GetEntryExitRecord(0);
        SfntGposEntryExitRecord middle = cursive.GetEntryExitRecord(1);
        SfntGposEntryExitRecord? last = cursive.FindEntryExitRecord(3);

        Assert.Multiple(() =>
        {
            Assert.That(cursive.GetEntryExitCount(), Is.EqualTo(3));
            Assert.That(first.HasEntryAnchor(), Is.False);
            Assert.That(first.GetExitAnchor()!.GetXCoordinate(),
                Is.EqualTo(600));
            Assert.That(middle.GetEntryAnchor()!.GetFormat(), Is.EqualTo(2));
            Assert.That(middle.GetEntryAnchor()!.GetAnchorPoint(),
                Is.EqualTo(7));
            Assert.That(middle.GetExitAnchor()!.GetYCoordinate(),
                Is.EqualTo(80));
            Assert.That(last!.GetEntryAnchor()!.GetXCoordinate(),
                Is.EqualTo(80));
            Assert.That(last.HasExitAnchor(), Is.False);
            Assert.That(cursive.FindEntryExitRecord(4), Is.Null);
        });
    }

    [Test]
    public void AppliesForwardChainAcrossIgnoredMark()
    {
        SfntGposTable? table = Parse(BuildCursiveGpos(0x0008).Bytes);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 12, 2, 3 });
        buffer.ApplyAdjustment(
            2,
            new SfntGposAdjustment(0, 100, 0, 0));
        int count = Apply(
            table,
            buffer,
            CreateMarkGdef(),
            SfntLayoutTraversalDirection.Forward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(600));
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
            Assert.That(buffer.GetYPlacement(1), Is.Zero);
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(-100));
            Assert.That(buffer.GetXAdvance(2), Is.EqualTo(450));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(30));
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(0));
            Assert.That(buffer.GetAttachmentType(2), Is.EqualTo(
                SfntGposAttachmentType.Cursive));
            Assert.That(buffer.GetXPlacement(3), Is.EqualTo(-80));
            Assert.That(buffer.GetXAdvance(3), Is.EqualTo(-80));
            Assert.That(buffer.GetYPlacement(3), Is.EqualTo(100));
            Assert.That(buffer.GetAttachmentParent(3), Is.EqualTo(2));
        });
    }

    [Test]
    public void AppliesBackwardRightToLeftChainFromStableBaselineRoot()
    {
        SfntGposTable? table = Parse(BuildCursiveGpos(0x0001).Bytes);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 3, 2, 1 });
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(0, 15, 0, 0));
        int count = Apply(
            table,
            buffer,
            null,
            SfntLayoutTraversalDirection.Backward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.GetXAdvance(2), Is.EqualTo(-600));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(-600));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(-85));
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(1));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(-450));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(-550));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(-55));
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(0));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(80));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(15));
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(-1));
            Assert.That(buffer.GetAttachmentType(0), Is.EqualTo(
                SfntGposAttachmentType.None));
        });
    }

    [Test]
    public void RequiresAnExitAndFollowingEntryInTheSameSubtable()
    {
        SfntGposTable? table = Parse(BuildCursiveGpos(0).Bytes);
        SfntGposPositionBuffer missingEntry =
            SfntGposPositionBuffer.Create(new int[] { 3, 1 });
        SfntGposPositionBuffer missingExit =
            SfntGposPositionBuffer.Create(new int[] { 3, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(Apply(
                table,
                missingEntry,
                null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
            Assert.That(Apply(
                table,
                missingExit,
                null,
                SfntLayoutTraversalDirection.Forward), Is.Zero);
        });
    }

    [Test]
    public void ReversesExistingCursiveChainWhenChildIsReattached()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2, 3 });

        buffer.AttachCursive(1, 0, 25);
        buffer.AttachCursive(1, 2, -40);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(1));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(-25));
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(2));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(-40));
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(-1));
            Assert.Throws<ArgumentException>(() =>
                buffer.AttachCursive(1, 1, 0));
        });
    }

    [TestCaseSource(nameof(InvalidCursiveTables))]
    public void RejectsMalformedCursivePositioning(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidCursiveTables()
    {
        SfntGposCursiveFixture format = BuildCursiveGpos(0);
        PatchUInt16(format.Bytes, format.Subtable, 2);
        yield return InvalidCase(format.Bytes, "Format");

        SfntGposCursiveFixture count = BuildCursiveGpos(0);
        PatchUInt16(count.Bytes, count.Subtable + 4, 2);
        yield return InvalidCase(count.Bytes, "CoverageCount");

        SfntGposCursiveFixture anchorOffset = BuildCursiveGpos(0);
        PatchUInt16(anchorOffset.Bytes, anchorOffset.Subtable + 8, 6);
        yield return InvalidCase(anchorOffset.Bytes, "AnchorOffset");

        SfntGposCursiveFixture anchorFormat = BuildCursiveGpos(0);
        int exitAnchorOffset = ReadUInt16(
            anchorFormat.Bytes,
            anchorFormat.Subtable + 8);
        PatchUInt16(
            anchorFormat.Bytes,
            anchorFormat.Subtable + exitAnchorOffset,
            4);
        yield return InvalidCase(anchorFormat.Bytes, "AnchorFormat");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("RejectsCursive" + name);
    }

    static int Apply(
        SfntGposTable? table,
        SfntGposPositionBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        return SfntGposExecutor.ApplyLookup(
            table!,
            0,
            buffer,
            gdef!,
            direction,
            new SfntGposAdjustmentContext(1000, 0, 0, null));
    }

    static SfntGdefTable CreateMarkGdef()
    {
        int[] classes = new int[16];
        classes[12] = 3;
        return new SfntGdefTable(
            1,
            0,
            new SfntClassDefinitionTable(
                1,
                0,
                classes,
                new SfntClassRange[0],
                3),
            null,
            null,
            null,
            null,
            null);
    }

    static SfntGposTable? Parse(byte[]? bytes)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Gpos, bytes));
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(16)
            .CopyBytes()));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return SfntGposTableParser.FindTable(cache);
    }

    static SfntTableData CreateTable(int tag, byte[]? bytes)
    {
        FontData data = FontData.Create(bytes!);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static SfntGposCursiveFixture BuildCursiveGpos(int lookupFlags)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0);
        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList).WriteUInt16(0);
        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList).WriteUInt16(0);
        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(1).WriteUInt16(4);
        int lookup = builder.GetLength();
        builder.WriteUInt16(3)
            .WriteUInt16((ushort)lookupFlags)
            .WriteUInt16(1)
            .WriteUInt16(8);
        int subtable = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0).WriteUInt16(3)
            .WriteZeros(12);

        int firstExit = WriteAnchorFormat1(builder, 600, 50);
        builder.PatchUInt16(
            subtable + 8,
            (ushort)(firstExit - subtable));
        int middleEntry = WriteAnchorFormat2(builder, 100, 20, 7);
        builder.PatchUInt16(
            subtable + 10,
            (ushort)(middleEntry - subtable));
        int middleExit = WriteAnchorFormat1(builder, 550, 80);
        builder.PatchUInt16(
            subtable + 12,
            (ushort)(middleExit - subtable));
        int lastEntry = WriteAnchorFormat1(builder, 80, 10);
        builder.PatchUInt16(
            subtable + 14,
            (ushort)(lastEntry - subtable));
        int coverage = WriteCoverage(builder, new int[] { 1, 2, 3 });
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(coverage - subtable));

        return new SfntGposCursiveFixture
        {
            Bytes = builder.CopyBytes(),
            Subtable = subtable
        };
    }

    static int WriteAnchorFormat1(
        TestBinaryBuilder builder,
        int xCoordinate,
        int yCoordinate)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(1)
            .WriteInt16((short)xCoordinate)
            .WriteInt16((short)yCoordinate);
        return offset;
    }

    static int WriteAnchorFormat2(
        TestBinaryBuilder builder,
        int xCoordinate,
        int yCoordinate,
        int anchorPoint)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(2)
            .WriteInt16((short)xCoordinate)
            .WriteInt16((short)yCoordinate)
            .WriteUInt16((ushort)anchorPoint);
        return offset;
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);

        foreach (int glyphId in glyphIds)
        {
            builder.WriteUInt16((ushort)glyphId);
        }

        return offset;
    }

    static int ReadUInt16(byte[]? bytes, int offset)
    {
        return bytes![offset] << 8 | bytes[offset + 1];
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
