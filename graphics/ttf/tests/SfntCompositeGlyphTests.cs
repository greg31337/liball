using ttf;

namespace ttf.Tests;

internal class SfntCompositeGlyphTests
{
    [Test]
    public void DecodesAllPlacementsTransformsOffsetPoliciesMetricsAndInstructions()
    {
        SfntCompositeGlyph? glyph = Parse(
            BuildComprehensiveComposite(),
            CreateMaxp(10, 3, 4, 1),
            FontLimits.Create());
        SfntCompositeComponent uniform = glyph!.GetComponent(0);
        SfntCompositeComponent attached = glyph.GetComponent(1);
        SfntCompositeComponent matrix = glyph.GetComponent(2);
        SfntCompositeComponent identity = glyph.GetComponent(3);
        byte[] instructionCopy = glyph.CopyInstructions();
        instructionCopy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(7));
            Assert.That(glyph.GetHeader().GetContourCount(), Is.EqualTo(-1));
            Assert.That(glyph.GetComponentCount(), Is.EqualTo(4));
            Assert.That(glyph.GetInstructionLength(), Is.EqualTo(3));
            Assert.That(glyph.CopyInstructions(), Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC }));
            Assert.That(glyph.HasMetricsComponent(), Is.True);
            Assert.That(glyph.GetMetricsComponentIndex(), Is.EqualTo(3));
            Assert.That(glyph.GetMetricsGlyphId(), Is.EqualTo(5));
            Assert.That(glyph.HasOverlappingComponents(), Is.True);
            Assert.That(glyph.GetTrailingPaddingLength(), Is.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(uniform.GetComponentIndex(), Is.Zero);
            Assert.That(uniform.GetGlyphId(), Is.EqualTo(2));
            Assert.That(uniform.GetPlacementKind(), Is.EqualTo(SfntCompositePlacementKind.Offset));
            Assert.That(uniform.GetXOffset(), Is.EqualTo(-20));
            Assert.That(uniform.GetYOffset(), Is.EqualTo(30));
            Assert.That(uniform.GetOffsetScaling(), Is.EqualTo(SfntCompositeOffsetScaling.Scaled));
            Assert.That(uniform.GetEffectiveXOffset(), Is.EqualTo(-10));
            Assert.That(uniform.GetEffectiveYOffset(), Is.EqualTo(15));
            Assert.That(uniform.RoundsOffsetToGrid(), Is.True);
            Assert.That(uniform.HasMoreComponents(), Is.True);
            Assert.That(uniform.HasOverlappingComponents(), Is.True);
            Assert.That(uniform.GetTransform().GetKind(),
                Is.EqualTo(SfntCompositeTransformKind.UniformScale));
            Assert.That(uniform.GetTransform().GetXScale(), Is.EqualTo(0.5));
            Assert.That(uniform.GetTransform().GetYScale(), Is.EqualTo(0.5));
        });

        Assert.Multiple(() =>
        {
            Assert.That(attached.GetPlacementKind(),
                Is.EqualTo(SfntCompositePlacementKind.PointAttachment));
            Assert.That(attached.GetParentPointIndex(), Is.EqualTo(5));
            Assert.That(attached.GetChildPointIndex(), Is.EqualTo(7));
            Assert.That(attached.GetOffsetScaling(), Is.EqualTo(SfntCompositeOffsetScaling.Default));
            Assert.That(attached.GetTransform().GetKind(),
                Is.EqualTo(SfntCompositeTransformKind.SeparateScale));
            Assert.That(attached.GetTransform().GetXScale(), Is.EqualTo(1.5));
            Assert.That(attached.GetTransform().GetYScale(), Is.EqualTo(-0.5));
            Assert.That(attached.RoundsOffsetToGrid(), Is.False);
        });

        Assert.Multiple(() =>
        {
            Assert.That(matrix.GetXOffset(), Is.EqualTo(-5));
            Assert.That(matrix.GetYOffset(), Is.EqualTo(6));
            Assert.That(matrix.GetOffsetScaling(), Is.EqualTo(SfntCompositeOffsetScaling.Unscaled));
            Assert.That(matrix.GetEffectiveXOffset(), Is.EqualTo(-5));
            Assert.That(matrix.GetEffectiveYOffset(), Is.EqualTo(6));
            Assert.That(matrix.GetTransform().GetKind(),
                Is.EqualTo(SfntCompositeTransformKind.TwoByTwo));
            Assert.That(matrix.GetTransform().GetXScale(), Is.EqualTo(1));
            Assert.That(matrix.GetTransform().GetScale01(), Is.EqualTo(0.25));
            Assert.That(matrix.GetTransform().GetScale10(), Is.EqualTo(-0.25));
            Assert.That(matrix.GetTransform().GetYScale(), Is.EqualTo(1));
            Assert.That(matrix.GetTransform().TransformX(4, 8), Is.EqualTo(2));
            Assert.That(matrix.GetTransform().TransformY(4, 8), Is.EqualTo(9));
            Assert.That(matrix.HasCompositeInstructions(), Is.True);
            Assert.That(matrix.UsesMetrics(), Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(identity.GetOffsetScaling(), Is.EqualTo(SfntCompositeOffsetScaling.Default));
            Assert.That(identity.GetEffectiveXOffset(), Is.EqualTo(100));
            Assert.That(identity.GetEffectiveYOffset(), Is.EqualTo(-100));
            Assert.That(identity.GetTransform().GetKind(),
                Is.EqualTo(SfntCompositeTransformKind.Identity));
            Assert.That(identity.GetTransform().TransformX(4, 8), Is.EqualTo(4));
            Assert.That(identity.GetTransform().TransformY(4, 8), Is.EqualTo(8));
            Assert.That(identity.HasMoreComponents(), Is.False);
            Assert.That(identity.UsesMetrics(), Is.True);
        });
    }

    [Test]
    public void DecodesUnsignedWordPointAttachmentsAndIgnoresOffsetOnlyFlags()
    {
        byte[] bytes = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0023)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16(0x0805)
            .WriteUInt16(2)
            .WriteUInt16(500)
            .WriteUInt16(600)
            .CopyBytes();
        SfntCompositeGlyph? glyph = Parse(
            bytes,
            CreateMaxp(3, 0, 2, 1),
            FontLimits.Create());
        SfntCompositeComponent component = glyph!.GetComponent(1);

        Assert.Multiple(() =>
        {
            Assert.That(component.GetPlacementKind(),
                Is.EqualTo(SfntCompositePlacementKind.PointAttachment));
            Assert.That(component.GetParentPointIndex(), Is.EqualTo(500));
            Assert.That(component.GetChildPointIndex(), Is.EqualTo(600));
            Assert.That(component.GetOffsetScaling(), Is.EqualTo(SfntCompositeOffsetScaling.Default));
            Assert.That(component.RoundsOffsetToGrid(), Is.False);
        });
    }

    [Test]
    public void AcceptsAnyNegativeCompositeContourMarker()
    {
        byte[] bytes = BuildSingleOffsetComponent(0x0003, 0);
        WriteInt16(bytes, 0, -2);

        Assert.That(Parse(
            bytes,
            CreateMaxp(1, 0, 1, 1),
            FontLimits.Create())!.GetHeader().GetContourCount(), Is.EqualTo(-2));
    }

    [Test]
    public void ReturnsNoCompositeForEmptyAndSimpleRecords()
    {
        SfntMaxpTable maxp = CreateMaxp(1, 0, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(SfntCompositeGlyphParser.Parse(
                CreateRecord(Array.Empty<byte>()),
                maxp,
                FontLimits.Create()), Is.Null);
            Assert.That(SfntCompositeGlyphParser.Parse(
                CreateRecord(BuildGlyphHeader(0)),
                maxp,
                FontLimits.Create()), Is.Null);
        });
    }

    [Test]
    public void RejectsEveryTruncationOfRequiredCompositeData()
    {
        byte[] complete = BuildComprehensiveComposite();
        SfntMaxpTable maxp = CreateMaxp(10, 3, 4, 1);

        for (int length = 1; length < complete.Length - 1; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(truncated, maxp, FontLimits.Create()),
                "Length {0}",
                length);
        }

        Assert.That(Parse(
            complete.AsSpan(0, complete.Length - 1).ToArray(),
            maxp,
            FontLimits.Create()), Is.Not.Null);
    }

    [Test]
    public void RejectsReservedConflictingTransformAndOffsetFlags()
    {
        byte[] reserved = BuildSingleOffsetComponent(0x0013, 0);
        byte[] transforms = BuildSingleOffsetComponent(0x004B, 0);
        byte[] offsets = BuildSingleOffsetComponent(0x1803, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                reserved,
                CreateMaxp(1, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                transforms,
                CreateMaxp(1, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                offsets,
                CreateMaxp(1, 0, 1, 1),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsPointAttachedFirstComponentAndMisplacedOverlapFlag()
    {
        byte[] firstAttached = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0001)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
        byte[] lateOverlap = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0023)
            .WriteUInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16(0x0403)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                firstAttached,
                CreateMaxp(2, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                lateOverlap,
                CreateMaxp(2, 0, 2, 1),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsGlyphReferencesAndMaxpCompositeDeclarations()
    {
        byte[] invalidReference = BuildSingleOffsetComponent(0x0003, 2);
        byte[] twoComponents = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0023)
            .WriteUInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16(0x0003)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                invalidReference,
                CreateMaxp(2, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                twoComponents,
                CreateMaxp(2, 0, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                BuildSingleOffsetComponent(0x0003, 0),
                CreateMaxp(1, 0, 1, 0),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsOversizedOrTruncatedInstructionsAndNonzeroTrailingData()
    {
        byte[] oversized = BuildInstructionComposite(3, new byte[] { 1, 2, 3 });
        byte[] truncated = BuildInstructionComposite(5, new byte[] { 1 });
        byte[] nonzeroPadding = BuildComprehensiveComposite();
        nonzeroPadding[nonzeroPadding.Length - 1] = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                oversized,
                CreateMaxp(1, 2, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                truncated,
                CreateMaxp(1, 5, 1, 1),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                nonzeroPadding,
                CreateMaxp(10, 3, 4, 1),
                FontLimits.Create()));
        });
    }

    [Test]
    public void AccessorsRejectWrongPlacementMissingMetricsAndInvalidIndices()
    {
        SfntCompositeGlyph? noMetrics = Parse(
            BuildSingleOffsetComponent(0x0003, 0),
            CreateMaxp(1, 0, 1, 1),
            FontLimits.Create());
        SfntCompositeGlyph? comprehensive = Parse(
            BuildComprehensiveComposite(),
            CreateMaxp(10, 3, 4, 1),
            FontLimits.Create());
        SfntCompositeComponent offset = comprehensive!.GetComponent(0);
        SfntCompositeComponent attached = comprehensive.GetComponent(1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => comprehensive.GetComponent(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => comprehensive.GetComponent(4));
            Assert.Throws<InvalidOperationException>(() => offset.GetParentPointIndex());
            Assert.Throws<InvalidOperationException>(() => offset.GetChildPointIndex());
            Assert.Throws<InvalidOperationException>(() => attached.GetXOffset());
            Assert.Throws<InvalidOperationException>(() => attached.GetYOffset());
            Assert.Throws<InvalidOperationException>(() => attached.GetEffectiveXOffset());
            Assert.Throws<InvalidOperationException>(() => noMetrics!.GetMetricsComponentIndex());
            Assert.Throws<InvalidOperationException>(() => noMetrics!.GetMetricsGlyphId());
        });
    }

    [Test]
    public void FontFaceCachesCompositeGlyphsAndDistinguishesEmptyRecords()
    {
        byte[] composite = BuildSingleOffsetComponent(0x0003, 0);
        byte[] loca = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)(composite.Length / 2))
            .CopyBytes();
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            CreateFaceCache(
                BuildHead(),
                BuildTrueTypeMaxp(2, 0, 1, 1),
                loca,
                composite));

        SfntCompositeGlyph? first = face.GetCompositeGlyph(1);
        SfntCompositeGlyph? second = face.GetCompositeGlyph(1);
        SfntResolvedGlyph resolved = face.GetResolvedGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first!.GetComponent(0).GetGlyphId(), Is.Zero);
            Assert.That(face.GetCompositeGlyph(0), Is.Null);
            Assert.That(face.GetResolvedGlyph(1), Is.SameAs(resolved));
            Assert.That(resolved.GetComponentClosureGlyphId(0), Is.Zero);
            Assert.That(resolved.GetControlPointCount(), Is.Zero);
        });
    }

    static SfntCompositeGlyph? Parse(
        byte[] bytes,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        return SfntCompositeGlyphParser.Parse(CreateRecord(bytes), maxp, limits);
    }

    static SfntGlyfRecord CreateRecord(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntGlyfRecord(7, 0, data.GetSlice(0, data.GetLength()));
    }

    static SfntMaxpTable CreateMaxp(
        int glyphCount,
        int maxInstructionSize,
        int maxComponentElements,
        int maxComponentDepth)
    {
        return new SfntMaxpTable(
            0x00010000U,
            SfntMaxpKind.TrueType,
            glyphCount,
            0,
            0,
            100,
            10,
            1,
            0,
            0,
            0,
            0,
            0,
            maxInstructionSize,
            maxComponentElements,
            maxComponentDepth);
    }

    static byte[] BuildComprehensiveComposite()
    {
        return new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0C2F)
            .WriteUInt16(2)
            .WriteInt16(-20)
            .WriteInt16(30)
            .WriteInt16(0x2000)
            .WriteUInt16(0x0060)
            .WriteUInt16(3)
            .WriteUInt8(5)
            .WriteUInt8(7)
            .WriteInt16(0x6000)
            .WriteInt16(-0x2000)
            .WriteUInt16(0x13A2)
            .WriteUInt16(4)
            .WriteInt8(-5)
            .WriteInt8(6)
            .WriteInt16(0x4000)
            .WriteInt16(0x1000)
            .WriteInt16(-0x1000)
            .WriteInt16(0x4000)
            .WriteUInt16(0x0203)
            .WriteUInt16(5)
            .WriteInt16(100)
            .WriteInt16(-100)
            .WriteUInt16(3)
            .WriteBytes(new byte[] { 0xAA, 0xBB, 0xCC })
            .WriteUInt8(0)
            .CopyBytes();
    }

    static byte[] BuildSingleOffsetComponent(int flags, int glyphId)
    {
        return new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16((ushort)flags)
            .WriteUInt16((ushort)glyphId)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildInstructionComposite(int instructionLength, byte[] instructions)
    {
        return new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(-1))
            .WriteUInt16(0x0103)
            .WriteUInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16((ushort)instructionLength)
            .WriteBytes(instructions)
            .CopyBytes();
    }

    static byte[] BuildGlyphHeader(int contourCount)
    {
        return new TestBinaryBuilder()
            .WriteInt16((short)contourCount)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(400)
            .WriteInt16(500)
            .CopyBytes();
    }

    static byte[] BuildHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0)
            .WriteUInt16(1000)
            .WriteInt64(0)
            .WriteInt64(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(1000)
            .WriteInt16(1000)
            .WriteUInt16(0)
            .WriteUInt16(8)
            .WriteInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildTrueTypeMaxp(
        int glyphCount,
        int maxInstructionSize,
        int maxComponentElements,
        int maxComponentDepth)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(100)
            .WriteUInt16(10)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)maxInstructionSize)
            .WriteUInt16((ushort)maxComponentElements)
            .WriteUInt16((ushort)maxComponentDepth)
            .CopyBytes();
    }

    static SfntTableCache CreateFaceCache(
        byte[] head,
        byte[] maxp,
        byte[] loca,
        byte[] glyf)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, head);
        AddTable(tables, FontTags.Maxp, maxp);
        AddTable(tables, FontTags.Loca, loca);
        AddTable(tables, FontTags.Glyf, glyf);
        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
