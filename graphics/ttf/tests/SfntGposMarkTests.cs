using ttf;

namespace ttf.Tests;

internal class SfntGposMarkFixture
{
    internal byte[]? Bytes;
    internal int MarkBaseSubtable;
    internal int MarkBaseMarkArray;
    internal int MarkBaseBaseArray;
    internal int MarkLigatureSubtable;
    internal int LigatureArray;
    internal int LigatureAttach;
    internal int MarkMarkSubtable;
    internal int MarkMarkBaseArray;
}

internal class SfntGposMarkTests
{
    [Test]
    public void ParsesAllMarkAttachmentLookupTypes()
    {
        SfntGposTable? table = Parse(BuildMarkGpos().Bytes);
        SfntGposMarkAttachment markBase = table!.GetLookup(0)
            .GetMarkBaseAdjustment(0);
        SfntGposMarkLigatureAdjustment markLigature = table.GetLookup(1)
            .GetMarkLigatureAdjustment(0);
        SfntGposMarkAttachment markMark = table.GetLookup(2)
            .GetMarkMarkAdjustment(0);
        SfntGposLigatureAttach? ligature =
            markLigature.FindLigatureAttach(3);

        Assert.Multiple(() =>
        {
            Assert.That(markBase.GetMarkClassCount(), Is.EqualTo(2));
            Assert.That(markBase.GetMarkArray().GetMarkCount(), Is.EqualTo(2));
            Assert.That(markBase.FindMarkRecord(11)!.GetMarkClass(),
                Is.EqualTo(1));
            Assert.That(markBase.FindBaseAnchor(1, 0)!.GetXCoordinate(),
                Is.EqualTo(100));
            Assert.That(markBase.FindBaseAnchor(2, 0), Is.Null);
            Assert.That(markLigature.GetLigatureCount(), Is.EqualTo(1));
            Assert.That(ligature!.GetComponentCount(), Is.EqualTo(3));
            Assert.That(ligature.GetAnchor(1, 0)!.GetXCoordinate(),
                Is.EqualTo(200));
            Assert.That(ligature.GetAnchor(1, 1), Is.Null);
            Assert.That(markMark.FindMarkRecord(13)!.GetMarkClass(),
                Is.EqualTo(1));
            Assert.That(markMark.FindBaseAnchor(11, 1)!.GetYCoordinate(),
                Is.EqualTo(230));
        });
    }

    [Test]
    public void AttachesMarksToBaseAcrossMarksAndCursivePlacement()
    {
        SfntGposTable? table = Parse(BuildMarkGpos().Bytes);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 9, 1, 10, 11 });
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(0, 5, 0, 0));
        buffer.AttachCursive(1, 0, 30);
        int count = Apply(
            table,
            0,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(90));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(215));
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentType(2), Is.EqualTo(
                SfntGposAttachmentType.Mark));
            Assert.That(buffer.GetXPlacement(3), Is.EqualTo(95));
            Assert.That(buffer.GetYPlacement(3), Is.EqualTo(220));
            Assert.That(buffer.GetAttachmentParent(3), Is.EqualTo(1));
            Assert.That(buffer.GetXAdvance(2), Is.Zero);
            Assert.That(buffer.GetYAdvance(3), Is.Zero);
        });
    }

    [Test]
    public void AppliesMarkToBaseInBackwardTraversal()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 10, 1 });
        int count = Apply(
            Parse(BuildMarkGpos().Bytes),
            0,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Backward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(1));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(90));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(180));
        });
    }

    [Test]
    public void SelectsAssociatedLigatureComponentAndFallsBackToLast()
    {
        SfntGposTable? table = Parse(BuildMarkGpos().Bytes);
        SfntLayoutGlyphBuffer associatedGlyphs = SfntLayoutGlyphBuffer.Create(
            new int[] { 3, 12 },
            new int[] { 0, 0 },
            new int[] { 2, 2 },
            new int[] { 3, 1 },
            new int[] { 0, 1 });
        SfntGposPositionBuffer associated =
            SfntGposPositionBuffer.Create(associatedGlyphs);
        SfntGposPositionBuffer unassociated =
            SfntGposPositionBuffer.Create(new int[] { 3, 12 });

        int associatedCount = Apply(
            table,
            1,
            associated,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        int unassociatedCount = Apply(
            table,
            1,
            unassociated,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        associated.ResolveAttachments(
            SfntLayoutTraversalDirection.Forward);
        unassociated.ResolveAttachments(
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(associatedCount, Is.EqualTo(1));
            Assert.That(associated.GetXPlacement(1), Is.EqualTo(195));
            Assert.That(associated.GetYPlacement(1), Is.EqualTo(190));
            Assert.That(associated.GetAttachmentParent(1), Is.Zero);
            Assert.That(unassociatedCount, Is.EqualTo(1));
            Assert.That(unassociated.GetXPlacement(1), Is.EqualTo(295));
            Assert.That(unassociated.GetYPlacement(1), Is.EqualTo(290));
        });
    }

    [Test]
    public void AttachesMarkToMarkUsingLookupMarkAttachmentFilter()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 11, 14, 13 });
        int count = Apply(
            Parse(BuildMarkGpos().Bytes),
            2,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(110));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(200));
            Assert.That(buffer.GetAttachmentParent(2), Is.Zero);
            Assert.That(buffer.GetAttachmentType(2), Is.EqualTo(
                SfntGposAttachmentType.Mark));
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(-1));
        });
    }

    [Test]
    public void BuildsAndPropagatesMarkToMarkAttachmentChain()
    {
        SfntGposTable? table = Parse(BuildMarkGpos().Bytes);
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 11, 13 });

        int baseCount = Apply(
            table,
            0,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        int markCount = Apply(
            table,
            2,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(baseCount, Is.EqualTo(1));
            Assert.That(markCount, Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentParent(1), Is.Zero);
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(1));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(95));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(185));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(205));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(385));
        });
    }

    [Test]
    public void LeavesMarkUnpositionedWhenItsClassAnchorIsNull()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 2, 10 });
        int count = Apply(
            Parse(BuildMarkGpos().Bytes),
            0,
            buffer,
            CreateGdef(),
            SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.Zero);
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(-1));
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
            Assert.That(buffer.GetYPlacement(1), Is.Zero);
        });
    }

    [TestCaseSource(nameof(InvalidMarkTables))]
    public void RejectsMalformedMarkAttachmentTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    static IEnumerable<TestCaseData> InvalidMarkTables()
    {
        SfntGposMarkFixture format = BuildMarkGpos();
        PatchUInt16(format.Bytes, format.MarkBaseSubtable, 2);
        yield return InvalidCase(format.Bytes, "MarkBaseFormat");

        SfntGposMarkFixture classes = BuildMarkGpos();
        PatchUInt16(classes.Bytes, classes.MarkBaseSubtable + 6, 0);
        yield return InvalidCase(classes.Bytes, "EmptyClasses");

        SfntGposMarkFixture markClass = BuildMarkGpos();
        PatchUInt16(markClass.Bytes, markClass.MarkBaseMarkArray + 2, 2);
        yield return InvalidCase(markClass.Bytes, "MarkClass");

        SfntGposMarkFixture baseCount = BuildMarkGpos();
        PatchUInt16(baseCount.Bytes, baseCount.MarkBaseBaseArray, 1);
        yield return InvalidCase(baseCount.Bytes, "BaseCount");

        SfntGposMarkFixture baseAnchor = BuildMarkGpos();
        PatchUInt16(
            baseAnchor.Bytes,
            baseAnchor.MarkBaseBaseArray + 2,
            2);
        yield return InvalidCase(baseAnchor.Bytes, "BaseAnchorOffset");

        SfntGposMarkFixture ligatureCount = BuildMarkGpos();
        PatchUInt16(ligatureCount.Bytes, ligatureCount.LigatureArray, 2);
        yield return InvalidCase(ligatureCount.Bytes, "LigatureCount");

        SfntGposMarkFixture componentCount = BuildMarkGpos();
        PatchUInt16(componentCount.Bytes, componentCount.LigatureAttach, 0);
        yield return InvalidCase(componentCount.Bytes, "ComponentCount");

        SfntGposMarkFixture mark2Count = BuildMarkGpos();
        PatchUInt16(mark2Count.Bytes, mark2Count.MarkMarkBaseArray, 2);
        yield return InvalidCase(mark2Count.Bytes, "Mark2Count");
    }

    static TestCaseData InvalidCase(byte[]? bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static int Apply(
        SfntGposTable? table,
        int lookupIndex,
        SfntGposPositionBuffer buffer,
        SfntGdefTable gdef,
        SfntLayoutTraversalDirection direction)
    {
        return SfntGposExecutor.ApplyLookup(
            table!,
            lookupIndex,
            buffer,
            gdef,
            direction,
            new SfntGposAdjustmentContext(1000, 0, 0, null));
    }

    static SfntGdefTable CreateGdef()
    {
        int[] glyphClasses = new int[16];
        glyphClasses[1] = 1;
        glyphClasses[2] = 1;
        glyphClasses[3] = 2;
        int glyphId = 10;

        while (glyphId <= 14)
        {
            glyphClasses[glyphId] = 3;
            glyphId += 1;
        }

        int[] markAttachmentClasses = new int[16];
        markAttachmentClasses[11] = 1;
        markAttachmentClasses[13] = 1;
        markAttachmentClasses[14] = 2;
        return new SfntGdefTable(
            1,
            0,
            CreateClassDefinition(glyphClasses, 3),
            null,
            null,
            CreateClassDefinition(markAttachmentClasses, 2),
            null,
            null);
    }

    static SfntClassDefinitionTable CreateClassDefinition(
        int[] classes,
        int maximumClass)
    {
        return new SfntClassDefinitionTable(
            1,
            0,
            classes,
            new SfntClassRange[0],
            maximumClass);
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

    static SfntGposMarkFixture BuildMarkGpos()
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
            .WriteUInt16(3).WriteZeros(6);
        SfntGposMarkFixture fixture = new SfntGposMarkFixture();

        int markBaseLookup = WriteLookup(
            builder,
            lookupList,
            0,
            4,
            0);
        fixture.MarkBaseSubtable = builder.GetLength();
        PatchLookupSubtable(
            builder,
            markBaseLookup,
            fixture.MarkBaseSubtable);
        WriteMarkBase(builder, fixture);

        int markLigatureLookup = WriteLookup(
            builder,
            lookupList,
            1,
            5,
            0);
        fixture.MarkLigatureSubtable = builder.GetLength();
        PatchLookupSubtable(
            builder,
            markLigatureLookup,
            fixture.MarkLigatureSubtable);
        WriteMarkLigature(builder, fixture);

        int markMarkLookup = WriteLookup(
            builder,
            lookupList,
            2,
            6,
            0x0100);
        fixture.MarkMarkSubtable = builder.GetLength();
        PatchLookupSubtable(
            builder,
            markMarkLookup,
            fixture.MarkMarkSubtable);
        WriteMarkMark(builder, fixture);

        fixture.Bytes = builder.CopyBytes();
        return fixture;
    }

    static int WriteLookup(
        TestBinaryBuilder builder,
        int lookupList,
        int lookupIndex,
        int lookupType,
        int lookupFlags)
    {
        int lookup = builder.GetLength();
        builder.PatchUInt16(
            lookupList + 2 + lookupIndex * 2,
            (ushort)(lookup - lookupList));
        builder.WriteUInt16((ushort)lookupType)
            .WriteUInt16((ushort)lookupFlags)
            .WriteUInt16(1)
            .WriteUInt16(0);
        return lookup;
    }

    static void PatchLookupSubtable(
        TestBinaryBuilder builder,
        int lookup,
        int subtable)
    {
        builder.PatchUInt16(
            lookup + 6,
            (ushort)(subtable - lookup));
    }

    static void WriteMarkBase(
        TestBinaryBuilder builder,
        SfntGposMarkFixture fixture)
    {
        int subtable = fixture.MarkBaseSubtable;
        builder.WriteUInt16(1).WriteZeros(4)
            .WriteUInt16(2).WriteZeros(4);
        int markCoverage = WriteCoverage(builder, new int[] { 10, 11 });
        int baseCoverage = WriteCoverage(builder, new int[] { 1, 2 });
        fixture.MarkBaseMarkArray = WriteMarkArray(
            builder,
            new int[] { 0, 1 },
            new int[][]
            {
                new int[] { 10, 20 },
                new int[] { 15, 25 }
            });
        fixture.MarkBaseBaseArray = WriteAnchorMatrix(
            builder,
            2,
            new int[][]
            {
                new int[] { 100, 200 },
                new int[] { 110, 210 },
                null!,
                new int[] { 120, 220 }
            });
        PatchAttachmentHeader(
            builder,
            subtable,
            markCoverage,
            baseCoverage,
            fixture.MarkBaseMarkArray,
            fixture.MarkBaseBaseArray);
    }

    static void WriteMarkLigature(
        TestBinaryBuilder builder,
        SfntGposMarkFixture fixture)
    {
        int subtable = fixture.MarkLigatureSubtable;
        builder.WriteUInt16(1).WriteZeros(4)
            .WriteUInt16(2).WriteZeros(4);
        int markCoverage = WriteCoverage(builder, new int[] { 12 });
        int ligatureCoverage = WriteCoverage(builder, new int[] { 3 });
        int markArray = WriteMarkArray(
            builder,
            new int[] { 0 },
            new int[][] { new int[] { 5, 10 } });
        fixture.LigatureArray = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(4);
        fixture.LigatureAttach = builder.GetLength();
        builder.WriteUInt16(3).WriteZeros(12);
        WriteRelativeAnchors(
            builder,
            fixture.LigatureAttach,
            fixture.LigatureAttach + 2,
            new int[][]
            {
                new int[] { 100, 100 },
                null!,
                new int[] { 200, 200 },
                null!,
                new int[] { 300, 300 },
                null!
            });
        PatchAttachmentHeader(
            builder,
            subtable,
            markCoverage,
            ligatureCoverage,
            markArray,
            fixture.LigatureArray);
    }

    static void WriteMarkMark(
        TestBinaryBuilder builder,
        SfntGposMarkFixture fixture)
    {
        int subtable = fixture.MarkMarkSubtable;
        builder.WriteUInt16(1).WriteZeros(4)
            .WriteUInt16(2).WriteZeros(4);
        int mark1Coverage = WriteCoverage(builder, new int[] { 13 });
        int mark2Coverage = WriteCoverage(builder, new int[] { 11 });
        int markArray = WriteMarkArray(
            builder,
            new int[] { 1 },
            new int[][] { new int[] { 20, 30 } });
        fixture.MarkMarkBaseArray = WriteAnchorMatrix(
            builder,
            1,
            new int[][]
            {
                null!,
                new int[] { 130, 230 }
            });
        PatchAttachmentHeader(
            builder,
            subtable,
            mark1Coverage,
            mark2Coverage,
            markArray,
            fixture.MarkMarkBaseArray);
    }

    static void PatchAttachmentHeader(
        TestBinaryBuilder builder,
        int subtable,
        int markCoverage,
        int baseCoverage,
        int markArray,
        int baseArray)
    {
        builder.PatchUInt16(
            subtable + 2,
            (ushort)(markCoverage - subtable));
        builder.PatchUInt16(
            subtable + 4,
            (ushort)(baseCoverage - subtable));
        builder.PatchUInt16(
            subtable + 8,
            (ushort)(markArray - subtable));
        builder.PatchUInt16(
            subtable + 10,
            (ushort)(baseArray - subtable));
    }

    static int WriteMarkArray(
        TestBinaryBuilder builder,
        int[] markClasses,
        int[][] anchorCoordinates)
    {
        int markArray = builder.GetLength();
        builder.WriteUInt16((ushort)markClasses.Length);
        int index = 0;

        while (index < markClasses.Length)
        {
            builder.WriteUInt16((ushort)markClasses[index])
                .WriteUInt16(0);
            index += 1;
        }

        index = 0;

        while (index < markClasses.Length)
        {
            int anchor = WriteAnchor(
                builder,
                anchorCoordinates[index][0],
                anchorCoordinates[index][1]);
            builder.PatchUInt16(
                markArray + 4 + index * 4,
                (ushort)(anchor - markArray));
            index += 1;
        }

        return markArray;
    }

    static int WriteAnchorMatrix(
        TestBinaryBuilder builder,
        int recordCount,
        int[][]? anchorCoordinates)
    {
        int matrix = builder.GetLength();
        builder.WriteUInt16((ushort)recordCount)
            .WriteZeros(anchorCoordinates!.Length * 2);
        WriteRelativeAnchors(
            builder,
            matrix,
            matrix + 2,
            anchorCoordinates);
        return matrix;
    }

    static void WriteRelativeAnchors(
        TestBinaryBuilder builder,
        int parentOffset,
        int offsetArray,
        int[][]? anchorCoordinates)
    {
        int index = 0;

        while (index < anchorCoordinates!.Length)
        {
            int[] coordinates = anchorCoordinates[index];

            if (coordinates != null)
            {
                int anchor = WriteAnchor(
                    builder,
                    coordinates[0],
                    coordinates[1]);
                builder.PatchUInt16(
                    offsetArray + index * 2,
                    (ushort)(anchor - parentOffset));
            }

            index += 1;
        }
    }

    static int WriteAnchor(
        TestBinaryBuilder builder,
        int xCoordinate,
        int yCoordinate)
    {
        int anchor = builder.GetLength();
        builder.WriteUInt16(1)
            .WriteInt16((short)xCoordinate)
            .WriteInt16((short)yCoordinate);
        return anchor;
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
    {
        int coverage = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);

        foreach (int glyphId in glyphIds)
        {
            builder.WriteUInt16((ushort)glyphId);
        }

        return coverage;
    }

    static void PatchUInt16(byte[]? bytes, int offset, int value)
    {
        bytes![offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
