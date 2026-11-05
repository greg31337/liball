using ttf;

namespace ttf.Tests;

internal class SfntCompositeResolverTests
{
    [Test]
    public void ResolvesSimpleAndEmptyGlyphsWithoutComponentClosure()
    {
        SfntLocaTable table = CreateTable(
            new byte[][]
            {
                BuildSimpleGlyph(
                    new int[] { 0, 10, 0 },
                    new int[] { 0, 0, 10 },
                    new bool[] { true, true, true }),
                Array.Empty<byte>()
            },
            CreateMaxp(2, 3, 1, 10, 10, 4, 4),
            FontLimits.Create());
        SfntResolvedGlyph simple = table.GetResolvedGlyph(0);
        SfntResolvedGlyph empty = table.GetResolvedGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(simple.IsComposite(), Is.False);
            Assert.That(simple.GetControlPointCount(), Is.EqualTo(3));
            Assert.That(simple.GetOutline().GetContourCount(), Is.EqualTo(1));
            Assert.That(simple.GetComponentClosureCount(), Is.Zero);
            Assert.That(simple.GetComponentDepth(), Is.Zero);
            Assert.That(simple.GetMetricsGlyphId(), Is.Zero);
            Assert.That(simple.UsesInheritedMetrics(), Is.False);
            Assert.That(simple.GetOutline(),
                Is.SameAs(table.GetSimpleGlyph(0)!.GetNormalizedOutline()));
            Assert.That(empty.GetControlPointCount(), Is.Zero);
            Assert.That(empty.GetOutline().GetContourCount(), Is.Zero);
            Assert.That(empty.GetComponentClosureCount(), Is.Zero);
            Assert.That(empty.GetMetricsGlyphId(), Is.EqualTo(1));
            Assert.That(table.GetResolvedGlyph(0), Is.SameAs(simple));
        });
    }

    [Test]
    public void AppliesComponentTransformAndScaledOffsetToPointsAndCommands()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0, 10, 0 },
            new int[] { 0, 0, 10 },
            new bool[] { true, true, true });
        byte[] composite = BuildComposite(
            BuildComponent(0x080B, 0, 20, 10, new int[] { 0x2000 }));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { simple, composite },
            CreateMaxp(2, 3, 1, 3, 1, 1, 1),
            FontLimits.Create()).GetResolvedGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.IsComposite(), Is.True);
            Assert.That(resolved.GetComponentDepth(), Is.EqualTo(1));
            Assert.That(resolved.GetControlPointCount(), Is.EqualTo(3));
            Assert.That(resolved.GetComponentClosureCount(), Is.EqualTo(1));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.Zero);
        });
        AssertPoint(resolved.GetControlPoint(0), 10, 5, true);
        AssertPoint(resolved.GetControlPoint(1), 15, 5, true);
        AssertPoint(resolved.GetControlPoint(2), 10, 10, true);
        AssertEnd(
            resolved.GetOutline().GetContour(0).GetCommand(0),
            SfntPathCommandKind.Move,
            10,
            5);
        AssertEnd(
            resolved.GetOutline().GetContour(0).GetCommand(1),
            SfntPathCommandKind.Line,
            15,
            5);
    }

    [Test]
    public void ResolvesNestedTransformsAndReportsMaximumDepth()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0, 2, 0 },
            new int[] { 0, 0, 2 },
            new bool[] { true, true, true });
        byte[] translated = BuildComposite(
            BuildComponent(0x0003, 0, 10, 0, Array.Empty<int>()));
        byte[] rotated = BuildComposite(
            BuildComponent(
                0x0083,
                1,
                0,
                20,
                new int[] { 0, 0x4000, -0x4000, 0 }));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { simple, translated, rotated },
            CreateMaxp(3, 3, 1, 3, 1, 1, 2),
            FontLimits.Create()).GetResolvedGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetComponentDepth(), Is.EqualTo(2));
            Assert.That(resolved.GetComponentClosureCount(), Is.EqualTo(2));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.EqualTo(1));
            Assert.That(resolved.GetComponentClosureGlyphId(1), Is.Zero);
        });
        AssertPoint(resolved.GetControlPoint(0), 0, 30, true);
        AssertPoint(resolved.GetControlPoint(1), 0, 32, true);
        AssertPoint(resolved.GetControlPoint(2), -2, 30, true);
    }

    [Test]
    public void ProducesStableFirstEncounterClosureAndKeepsRepeatedGeometry()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0 },
            new int[] { 0 },
            new bool[] { true });
        byte[] child = BuildComposite(
            BuildComponent(0x0003, 0, 1, 0, Array.Empty<int>()));
        byte[] root = BuildComposite(
            BuildComponent(0x0003, 1, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0003, 0, 10, 0, Array.Empty<int>()),
            BuildComponent(0x0003, 1, 20, 0, Array.Empty<int>()));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { simple, child, root },
            CreateMaxp(3, 1, 1, 3, 3, 3, 2),
            FontLimits.Create()).GetResolvedGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetComponentClosureCount(), Is.EqualTo(2));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.EqualTo(1));
            Assert.That(resolved.GetComponentClosureGlyphId(1), Is.Zero);
            Assert.That(resolved.GetControlPointCount(), Is.EqualTo(3));
            Assert.That(resolved.GetOutline().GetContourCount(), Is.EqualTo(3));
        });
        AssertPoint(resolved.GetControlPoint(0), 1, 0, true);
        AssertPoint(resolved.GetControlPoint(1), 10, 0, true);
        AssertPoint(resolved.GetControlPoint(2), 21, 0, true);
    }

    [Test]
    public void AlignsExplicitPointsAfterTransformWithoutUsingImpliedPointOrder()
    {
        byte[] first = BuildSimpleGlyph(
            new int[] { 0, 10 },
            new int[] { 0, 0 },
            new bool[] { true, true });
        byte[] second = BuildSimpleGlyph(
            new int[] { 2, 4 },
            new int[] { 3, 3 },
            new bool[] { false, false });
        byte[] composite = BuildComposite(
            BuildComponent(0x0003, 0, 5, 5, Array.Empty<int>()),
            BuildComponent(0x0009, 1, 1, 0, new int[] { 0x2000 }));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { first, second, composite },
            CreateMaxp(3, 2, 1, 4, 2, 2, 1),
            FontLimits.Create()).GetResolvedGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetControlPointCount(), Is.EqualTo(4));
            Assert.That(resolved.GetOutline().GetContourCount(), Is.EqualTo(2));
            Assert.That(resolved.GetOutline().GetContour(1).GetImpliedPointCount(),
                Is.EqualTo(2));
            Assert.That(resolved.GetOutline().GetContour(1).GetPoint(1).GetSourcePointIndex(),
                Is.EqualTo(2));
        });
        AssertPoint(resolved.GetControlPoint(0), 5, 5, true);
        AssertPoint(resolved.GetControlPoint(1), 15, 5, true);
        AssertPoint(resolved.GetControlPoint(2), 15, 5, false);
        AssertPoint(resolved.GetControlPoint(3), 16, 5, false);
    }

    [Test]
    public void AppliesNonSymmetricTransformBeforePointAttachmentAndBounds()
    {
        byte[] parent = BuildSimpleGlyph(
            new int[] { 10 },
            new int[] { 20 },
            new bool[] { true });
        byte[] child = BuildSimpleGlyph(
            new int[] { 2, 4, 8 },
            new int[] { 3, 9, 3 },
            new bool[] { true, false, true });
        byte[] composite = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(
                0x0081,
                1,
                0,
                0,
                new int[] { 0x4000, 0x2000, -0x1000, 0x7FFF }));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { parent, child, composite },
            CreateMaxp(3, 3, 1, 4, 2, 2, 1),
            FontLimits.Create()).GetResolvedGlyph(2);
        SfntOutlineContour transformed = resolved.GetOutline().GetContour(1);
        SfntPathBounds bounds = resolved.GetOutline().GetBounds();
        double expectedMaximumY = 27.347646096802894;

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetControlPointCount(), Is.EqualTo(4));
            Assert.That(resolved.GetOutline().GetContourCount(), Is.EqualTo(2));
            Assert.That(resolved.GetControlPoint(1).GetX(), Is.EqualTo(10));
            Assert.That(resolved.GetControlPoint(1).GetY(), Is.EqualTo(20));
            Assert.That(transformed.GetCommand(0).GetEndX(), Is.EqualTo(10));
            Assert.That(transformed.GetCommand(0).GetEndY(), Is.EqualTo(20));
            Assert.That(transformed.GetCommand(1).GetControlX(), Is.EqualTo(10.5));
            Assert.That(transformed.GetCommand(1).GetControlY(),
                Is.EqualTo(32.9996337890625));
            Assert.That(transformed.GetCommand(1).GetEndX(), Is.EqualTo(16));
            Assert.That(transformed.GetCommand(1).GetEndY(), Is.EqualTo(23));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(10));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(20));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(16));
            Assert.That(bounds.GetYMaximum(),
                Is.EqualTo(expectedMaximumY).Within(0.000000001));
        });
    }

    [Test]
    public void ResolvesInheritedMetricsTransitivelyWithLastComponentWinning()
    {
        byte[] first = BuildSimpleGlyph(
            new int[] { 0 },
            new int[] { 0 },
            new bool[] { true });
        byte[] second = BuildSimpleGlyph(
            new int[] { 1 },
            new int[] { 0 },
            new bool[] { true });
        byte[] metricsComposite = BuildComposite(
            BuildComponent(0x0203, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0203, 1, 0, 0, Array.Empty<int>()));
        byte[] root = BuildComposite(
            BuildComponent(0x0203, 2, 0, 0, Array.Empty<int>()));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { first, second, metricsComposite, root },
            CreateMaxp(4, 1, 1, 2, 2, 2, 2),
            FontLimits.Create()).GetResolvedGlyph(3);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetMetricsGlyphId(), Is.EqualTo(1));
            Assert.That(resolved.UsesInheritedMetrics(), Is.True);
            Assert.That(resolved.GetComponentClosureCount(), Is.EqualTo(3));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.EqualTo(2));
            Assert.That(resolved.GetComponentClosureGlyphId(1), Is.Zero);
            Assert.That(resolved.GetComponentClosureGlyphId(2), Is.EqualTo(1));
        });
    }

    [Test]
    public void RoundsScaledComponentOffsetsWhenRequested()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0 },
            new int[] { 0 },
            new bool[] { true });
        byte[] composite = BuildComposite(
            BuildComponent(0x080F, 0, 3, -3, new int[] { 0x2000 }));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { simple, composite },
            CreateMaxp(2, 1, 1, 1, 1, 1, 1),
            FontLimits.Create()).GetResolvedGlyph(1);

        AssertPoint(resolved.GetControlPoint(0), 2, -2, true);
    }

    [Test]
    public void IncludesEmptyComponentsInClosureWithoutAddingGeometry()
    {
        byte[] composite = BuildComposite(
            BuildComponent(0x0203, 0, 0, 0, Array.Empty<int>()));
        SfntResolvedGlyph resolved = CreateTable(
            new byte[][] { Array.Empty<byte>(), composite },
            CreateMaxp(2, 0, 0, 0, 0, 1, 1),
            FontLimits.Create()).GetResolvedGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.GetControlPointCount(), Is.Zero);
            Assert.That(resolved.GetOutline().GetContourCount(), Is.Zero);
            Assert.That(resolved.GetComponentClosureCount(), Is.EqualTo(1));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.Zero);
            Assert.That(resolved.GetMetricsGlyphId(), Is.Zero);
            Assert.That(resolved.GetComponentDepth(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsSelfAndIndirectCompositeCycles()
    {
        SfntLocaTable selfCycle = CreateTable(
            new byte[][]
            {
                BuildComposite(BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()))
            },
            CreateMaxp(1, 0, 0, 0, 0, 1, 4),
            FontLimits.Create());
        SfntLocaTable indirectCycle = CreateTable(
            new byte[][]
            {
                BuildComposite(BuildComponent(0x0003, 1, 0, 0, Array.Empty<int>())),
                BuildComposite(BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()))
            },
            CreateMaxp(2, 0, 0, 0, 0, 1, 4),
            FontLimits.Create());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => selfCycle.GetResolvedGlyph(0));
            Assert.Throws<FontFormatException>(() => indirectCycle.GetResolvedGlyph(0));
        });
    }

    [Test]
    public void RejectsCycleAfterResolvingValidSiblingGeometry()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0, 10 },
            new int[] { 0, 10 },
            new bool[] { true, true });
        byte[] partialThenCycle = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0003, 1, 20, 0, Array.Empty<int>()));
        SfntLocaTable table = CreateTable(
            new byte[][] { simple, partialThenCycle },
            CreateMaxp(2, 2, 1, 2, 1, 2, 4),
            FontLimits.Create());

        Assert.Throws<FontFormatException>(() => table.GetResolvedGlyph(1));
    }

    [Test]
    public void EnforcesDeclaredAndConfiguredCompositeDepth()
    {
        byte[][] glyphs = BuildDepthChain();
        SfntResolvedGlyph accepted = CreateTable(
            glyphs,
            CreateMaxp(4, 1, 1, 1, 1, 1, 3),
            CreateLimits(10, 10, 3)).GetResolvedGlyph(3);
        SfntLocaTable declared = CreateTable(
            glyphs,
            CreateMaxp(4, 1, 1, 1, 1, 1, 2),
            FontLimits.Create());
        SfntLocaTable configured = CreateTable(
            glyphs,
            CreateMaxp(4, 1, 1, 1, 1, 1, 3),
            CreateLimits(10, 10, 2));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.GetComponentDepth(), Is.EqualTo(3));
            Assert.That(accepted.GetComponentClosureCount(), Is.EqualTo(3));
            Assert.That(accepted.GetComponentClosureGlyphId(0), Is.EqualTo(2));
            Assert.That(accepted.GetComponentClosureGlyphId(1), Is.EqualTo(1));
            Assert.That(accepted.GetComponentClosureGlyphId(2), Is.Zero);
            Assert.Throws<FontFormatException>(() => declared.GetResolvedGlyph(3));
            Assert.Throws<FontLimitException>(() => configured.GetResolvedGlyph(3));
        });
    }

    [Test]
    public void EnforcesExpandedPointAndContourDeclarationsAndLimits()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0, 1 },
            new int[] { 0, 1 },
            new bool[] { true, true });
        byte[] duplicate = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0003, 0, 2, 0, Array.Empty<int>()));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CreateTable(
                new byte[][] { simple, duplicate },
                CreateMaxp(2, 2, 1, 3, 2, 2, 1),
                FontLimits.Create()).GetResolvedGlyph(1));
            Assert.Throws<FontFormatException>(() => CreateTable(
                new byte[][] { simple, duplicate },
                CreateMaxp(2, 2, 1, 4, 1, 2, 1),
                FontLimits.Create()).GetResolvedGlyph(1));
            Assert.Throws<FontLimitException>(() => CreateTable(
                new byte[][] { simple, duplicate },
                CreateMaxp(2, 2, 1, 4, 2, 2, 1),
                CreateLimits(3, 2, 4)).GetResolvedGlyph(1));
            Assert.Throws<FontLimitException>(() => CreateTable(
                new byte[][] { simple, duplicate },
                CreateMaxp(2, 2, 1, 4, 2, 2, 1),
                CreateLimits(4, 1, 4)).GetResolvedGlyph(1));
        });
    }

    [Test]
    public void RejectsPointAttachmentsOutsideAccumulatedExplicitPoints()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0 },
            new int[] { 0 },
            new bool[] { true });
        byte[] invalidParent = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0001, 0, 1, 0, Array.Empty<int>()));
        byte[] invalidChild = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()),
            BuildComponent(0x0001, 0, 0, 1, Array.Empty<int>()));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CreateTable(
                new byte[][] { simple, invalidParent },
                CreateMaxp(2, 1, 1, 2, 2, 2, 1),
                FontLimits.Create()).GetResolvedGlyph(1));
            Assert.Throws<FontFormatException>(() => CreateTable(
                new byte[][] { simple, invalidChild },
                CreateMaxp(2, 1, 1, 2, 2, 2, 1),
                FontLimits.Create()).GetResolvedGlyph(1));
        });
    }

    [Test]
    public void AccessorsAndResolverRejectIndicesAndNullDependencies()
    {
        SfntLocaTable table = CreateTable(
            new byte[][] { Array.Empty<byte>() },
            CreateMaxp(1, 0, 0, 0, 0, 1, 1),
            FontLimits.Create());
        SfntResolvedGlyph glyph = table.GetResolvedGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetResolvedGlyph(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetResolvedGlyph(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph.GetControlPoint(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph.GetControlPoint(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph.GetComponentClosureGlyphId(0));
            Assert.Throws<ArgumentNullException>(() => SfntCompositeResolver.Resolve(
                null!,
                0,
                CreateMaxp(1, 0, 0, 0, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<ArgumentNullException>(() => SfntCompositeResolver.Resolve(
                table,
                0,
                null!,
                FontLimits.Create()));
            Assert.Throws<ArgumentNullException>(() => SfntCompositeResolver.Resolve(
                table,
                0,
                CreateMaxp(1, 0, 0, 0, 0, 1, 1),
                null!));
        });
    }

    static void AssertPoint(
        SfntResolvedPoint point,
        double x,
        double y,
        bool onCurve)
    {
        Assert.Multiple(() =>
        {
            Assert.That(point.GetX(), Is.EqualTo(x));
            Assert.That(point.GetY(), Is.EqualTo(y));
            Assert.That(point.IsOnCurve(), Is.EqualTo(onCurve));
        });
    }

    static void AssertEnd(
        SfntPathCommand command,
        SfntPathCommandKind kind,
        double x,
        double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(kind));
            Assert.That(command.GetEndX(), Is.EqualTo(x));
            Assert.That(command.GetEndY(), Is.EqualTo(y));
        });
    }

    static SfntLocaTable CreateTable(
        byte[][] glyphs,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        int[] offsets = new int[glyphs.Length + 1];
        int totalLength = 0;
        int emptyCount = 0;

        for (int index = 0; index < glyphs.Length; index += 1)
        {
            offsets[index] = totalLength;
            totalLength += glyphs[index].Length;

            if (glyphs[index].Length == 0)
            {
                emptyCount += 1;
            }
        }

        offsets[glyphs.Length] = totalLength;
        byte[] allData = new byte[totalLength];
        int destinationOffset = 0;

        foreach (byte[] glyph in glyphs)
        {
            glyph.CopyTo(allData, destinationOffset);
            destinationOffset += glyph.Length;
        }

        FontData data = FontData.Create(allData);
        SfntGlyfRecord[] records = new SfntGlyfRecord[glyphs.Length];

        for (int glyphId = 0; glyphId < records.Length; glyphId += 1)
        {
            int length = offsets[glyphId + 1] - offsets[glyphId];
            records[glyphId] = new SfntGlyfRecord(
                glyphId,
                offsets[glyphId],
                data.GetSlice(offsets[glyphId], length));
        }

        return new SfntLocaTable(
            SfntLocaFormat.Long,
            offsets,
            records,
            totalLength,
            emptyCount,
            maxp,
            limits);
    }

    static SfntMaxpTable CreateMaxp(
        int glyphCount,
        int maxPoints,
        int maxContours,
        int maxCompositePoints,
        int maxCompositeContours,
        int maxComponentElements,
        int maxComponentDepth)
    {
        return new SfntMaxpTable(
            0x00010000U,
            SfntMaxpKind.TrueType,
            glyphCount,
            maxPoints,
            maxContours,
            maxCompositePoints,
            maxCompositeContours,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            maxComponentElements,
            maxComponentDepth);
    }

    static FontLimits CreateLimits(
        int maxPoints,
        int maxContours,
        int maxComponentDepth)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            100,
            maxPoints,
            maxContours,
            maxComponentDepth,
            10,
            10,
            100,
            100000,
            100000,
            100000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static byte[] BuildSimpleGlyph(int[] xs, int[] ys, bool[] onCurve)
    {
        int xMinimum = xs[0];
        int xMaximum = xs[0];
        int yMinimum = ys[0];
        int yMaximum = ys[0];

        for (int index = 1; index < xs.Length; index += 1)
        {
            xMinimum = Math.Min(xMinimum, xs[index]);
            xMaximum = Math.Max(xMaximum, xs[index]);
            yMinimum = Math.Min(yMinimum, ys[index]);
            yMaximum = Math.Max(yMaximum, ys[index]);
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(1)
            .WriteInt16((short)xMinimum)
            .WriteInt16((short)yMinimum)
            .WriteInt16((short)xMaximum)
            .WriteInt16((short)yMaximum)
            .WriteUInt16((ushort)(xs.Length - 1))
            .WriteUInt16(0);

        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteUInt8(onCurve[index] ? (byte)1 : (byte)0);
        }

        int previous = 0;
        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteInt16((short)(xs[index] - previous));
            previous = xs[index];
        }

        previous = 0;
        for (int index = 0; index < ys.Length; index += 1)
        {
            builder.WriteInt16((short)(ys[index] - previous));
            previous = ys[index];
        }

        if ((builder.GetLength() & 1) != 0)
        {
            builder.WriteUInt8(0);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildComposite(params byte[][] componentRecords)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(-1)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0);

        for (int index = 0; index < componentRecords.Length; index += 1)
        {
            byte[] record = componentRecords[index].ToArray();

            if (index < componentRecords.Length - 1)
            {
                record[1] |= 0x20;
            }

            builder.WriteBytes(record);
        }

        if ((builder.GetLength() & 1) != 0)
        {
            builder.WriteUInt8(0);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildComponent(
        int flags,
        int glyphId,
        int argument1,
        int argument2,
        int[] transformValues)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)flags)
            .WriteUInt16((ushort)glyphId);

        if ((flags & 0x0002) != 0)
        {
            builder.WriteInt16((short)argument1).WriteInt16((short)argument2);
        }
        else
        {
            builder.WriteUInt16((ushort)argument1).WriteUInt16((ushort)argument2);
        }

        foreach (int value in transformValues)
        {
            builder.WriteInt16((short)value);
        }

        return builder.CopyBytes();
    }

    static byte[][] BuildDepthChain()
    {
        byte[] simple = BuildSimpleGlyph(
            new int[] { 0 },
            new int[] { 0 },
            new bool[] { true });
        byte[] first = BuildComposite(
            BuildComponent(0x0003, 0, 0, 0, Array.Empty<int>()));
        byte[] second = BuildComposite(
            BuildComponent(0x0003, 1, 0, 0, Array.Empty<int>()));
        byte[] third = BuildComposite(
            BuildComponent(0x0003, 2, 0, 0, Array.Empty<int>()));
        return new byte[][] { simple, first, second, third };
    }
}
