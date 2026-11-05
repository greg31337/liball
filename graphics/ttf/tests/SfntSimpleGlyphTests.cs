using ttf;

namespace ttf.Tests;

internal class SfntSimpleGlyphTests
{
    [Test]
    public void DecodesContourEndpointsInstructionsRepeatedFlagsAndDeltas()
    {
        SfntSimpleGlyph? glyph = Parse(
            BuildComplexSimpleGlyph(),
            CreateMaxp(5, 2, 2),
            FontLimits.Create());

        byte[] instructionCopy = glyph!.CopyInstructions();
        instructionCopy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(7));
            Assert.That(glyph.GetContourCount(), Is.EqualTo(2));
            Assert.That(glyph.GetContourStartPoint(0), Is.Zero);
            Assert.That(glyph.GetContourEndPoint(0), Is.EqualTo(2));
            Assert.That(glyph.GetContourStartPoint(1), Is.EqualTo(3));
            Assert.That(glyph.GetContourEndPoint(1), Is.EqualTo(4));
            Assert.That(glyph.GetPointCount(), Is.EqualTo(5));
            Assert.That(glyph.GetInstructionLength(), Is.EqualTo(2));
            Assert.That(glyph.CopyInstructions(), Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(glyph.HasOverlappingContours(), Is.True);
            Assert.That(glyph.GetTrailingPaddingLength(), Is.EqualTo(1));
            Assert.That(glyph.GetHeader().GetContourCount(), Is.EqualTo(2));
            Assert.That(glyph.GetHeader().GetXMinimum(), Is.EqualTo(7));
            Assert.That(glyph.GetHeader().GetYMinimum(), Is.EqualTo(14));
            Assert.That(glyph.GetHeader().GetXMaximum(), Is.EqualTo(307));
            Assert.That(glyph.GetHeader().GetYMaximum(), Is.EqualTo(20));
        });

        AssertPoint(glyph.GetPoint(0), 0, 0x77, true, 10, 20, 10, 20);
        AssertPoint(glyph.GetPoint(1), 1, 0x02, false, 7, 18, -3, -2);
        AssertPoint(glyph.GetPoint(2), 2, 0x05, true, 307, 14, 300, -4);
        AssertPoint(glyph.GetPoint(3), 3, 0x31, true, 307, 14, 0, 0);
        AssertPoint(glyph.GetPoint(4), 4, 0x31, true, 307, 14, 0, 0);
    }

    [Test]
    public void AcceptsHeaderOnlyAndInstructionBearingZeroContourGlyphs()
    {
        SfntSimpleGlyph? headerOnly = Parse(
            BuildGlyphHeader(0),
            CreateMaxp(0, 0, 3),
            FontLimits.Create());
        byte[] withInstructions = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(0))
            .WriteUInt16(3)
            .WriteBytes(new byte[] { 1, 2, 3 })
            .WriteUInt8(0)
            .CopyBytes();
        SfntSimpleGlyph? instructed = Parse(
            withInstructions,
            CreateMaxp(0, 0, 3),
            FontLimits.Create());

        Assert.Multiple(() =>
        {
            Assert.That(headerOnly!.GetContourCount(), Is.Zero);
            Assert.That(headerOnly.GetPointCount(), Is.Zero);
            Assert.That(headerOnly.GetInstructionLength(), Is.Zero);
            Assert.That(headerOnly.GetTrailingPaddingLength(), Is.Zero);
            Assert.That(instructed!.GetContourCount(), Is.Zero);
            Assert.That(instructed.GetPointCount(), Is.Zero);
            Assert.That(instructed.CopyInstructions(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(instructed.GetTrailingPaddingLength(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReturnsNoSimpleGlyphForEmptyAndCompositeRecords()
    {
        SfntMaxpTable maxp = CreateMaxp(1, 1, 0);
        SfntGlyfRecord empty = CreateRecord(Array.Empty<byte>());
        SfntGlyfRecord composite = CreateRecord(BuildGlyphHeader(-1));

        Assert.Multiple(() =>
        {
            Assert.That(SfntGlyphHeaderParser.Parse(empty), Is.Null);
            Assert.That(SfntSimpleGlyphParser.Parse(empty, maxp, FontLimits.Create()), Is.Null);
            Assert.That(SfntSimpleGlyphParser.Parse(composite, maxp, FontLimits.Create()), Is.Null);
            Assert.That(SfntGlyphHeaderParser.Parse(composite)!.GetContourCount(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void RejectsEveryTruncationOfRequiredSimpleGlyphData()
    {
        byte[] glyph = BuildComplexSimpleGlyph();
        SfntMaxpTable maxp = CreateMaxp(5, 2, 2);

        for (int length = 1; length < glyph.Length - 1; length += 1)
        {
            byte[] truncated = glyph.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(truncated, maxp, FontLimits.Create()),
                "Length {0}",
                length);
        }

        Assert.That(Parse(
            glyph.AsSpan(0, glyph.Length - 1).ToArray(),
            maxp,
            FontLimits.Create()), Is.Not.Null);
    }

    [Test]
    public void RejectsNonIncreasingContourEndpoints()
    {
        byte[] equal = BuildComplexSimpleGlyph();
        byte[] descending = BuildComplexSimpleGlyph();
        WriteUInt16(equal, 12, 2);
        WriteUInt16(descending, 10, 4);
        WriteUInt16(descending, 12, 3);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                equal,
                CreateMaxp(5, 2, 2),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                descending,
                CreateMaxp(5, 2, 2),
                FontLimits.Create()));
        });
    }

    [Test]
    public void EnforcesMaxpPointContourAndInstructionDeclarations()
    {
        byte[] glyph = BuildComplexSimpleGlyph();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                glyph,
                CreateMaxp(4, 2, 2),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                glyph,
                CreateMaxp(5, 1, 2),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                glyph,
                CreateMaxp(5, 2, 1),
                FontLimits.Create()));
        });
    }

    [Test]
    public void EnforcesConfiguredPointAndContourLimits()
    {
        byte[] glyph = BuildComplexSimpleGlyph();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                glyph,
                CreateMaxp(5, 2, 2),
                CreateLimits(4, 2)));
            Assert.Throws<FontLimitException>(() => Parse(
                glyph,
                CreateMaxp(5, 2, 2),
                CreateLimits(5, 1)));
        });
    }

    [Test]
    public void EnforcesFormatScalePointAndContourBoundaries()
    {
        byte[] maximumPoints = BuildRepeatedZeroGlyph(1, 65535);
        byte[] maximumContours = BuildRepeatedZeroGlyph(32767, 32767);
        SfntSimpleGlyph? pointGlyph = Parse(
            maximumPoints,
            CreateMaxp(65535, 1, 0),
            CreateLimits(65535, 1));
        SfntSimpleGlyph? contourGlyph = Parse(
            maximumContours,
            CreateMaxp(32767, 32767, 0),
            CreateLimits(32767, 32767));

        Assert.Multiple(() =>
        {
            Assert.That(pointGlyph!.GetPointCount(), Is.EqualTo(65535));
            Assert.That(pointGlyph.GetContourCount(), Is.EqualTo(1));
            Assert.That(contourGlyph!.GetPointCount(), Is.EqualTo(32767));
            Assert.That(contourGlyph.GetContourCount(), Is.EqualTo(32767));
            Assert.Throws<FontLimitException>(() => Parse(
                maximumPoints,
                CreateMaxp(65535, 1, 0),
                CreateLimits(65534, 1)));
            Assert.Throws<FontLimitException>(() => Parse(
                maximumContours,
                CreateMaxp(32767, 32767, 0),
                CreateLimits(32767, 32766)));
        });
    }

    [Test]
    public void RejectsTruncatedInstructions()
    {
        byte[] glyph = BuildComplexSimpleGlyph();
        WriteUInt16(glyph, 14, 20);

        Assert.Throws<FontFormatException>(() => Parse(
            glyph,
            CreateMaxp(5, 2, 20),
            FontLimits.Create()));
    }

    [Test]
    public void RejectsMissingAndOversizedFlagRepetitions()
    {
        byte[] missingCount = BuildOnePointGlyph(new byte[] { 0x39 });
        byte[] oversizedCount = BuildOnePointGlyph(new byte[] { 0x39, 1 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                missingCount,
                CreateMaxp(1, 1, 0),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                oversizedCount,
                CreateMaxp(1, 1, 0),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsReservedAndMisplacedOverlapFlags()
    {
        byte[] reserved = BuildOnePointGlyph(new byte[] { 0xB1 });
        byte[] misplacedOverlap = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(1))
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt8(0x31)
            .WriteUInt8(0x71)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                reserved,
                CreateMaxp(1, 1, 0),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                misplacedOverlap,
                CreateMaxp(2, 1, 0),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsTruncatedCoordinateArraysAndAccumulationOverflow()
    {
        byte[] truncatedCoordinates = BuildOnePointGlyph(new byte[] { 0x01 });
        byte[] overflow = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(1))
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt8(0x21)
            .WriteUInt8(0x21)
            .WriteInt16(short.MaxValue)
            .WriteInt16(1)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                truncatedCoordinates,
                CreateMaxp(1, 1, 0),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                overflow,
                CreateMaxp(2, 1, 0),
                FontLimits.Create()));
        });
    }

    [Test]
    public void RejectsNonzeroTrailingDataAndTruncatedZeroContourExtension()
    {
        byte[] nonzeroPadding = BuildComplexSimpleGlyph();
        nonzeroPadding[nonzeroPadding.Length - 1] = 1;
        byte[] zeroContourTruncation = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(0))
            .WriteUInt8(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(
                nonzeroPadding,
                CreateMaxp(5, 2, 2),
                FontLimits.Create()));
            Assert.Throws<FontFormatException>(() => Parse(
                zeroContourTruncation,
                CreateMaxp(0, 0, 0),
                FontLimits.Create()));
        });
    }

    [Test]
    public void AccessorsRejectInvalidContourAndPointIndices()
    {
        SfntSimpleGlyph? glyph = Parse(
            BuildComplexSimpleGlyph(),
            CreateMaxp(5, 2, 2),
            FontLimits.Create());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph!.GetContourStartPoint(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph!.GetContourEndPoint(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph!.GetPoint(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph!.GetPoint(5));
        });
    }

    [Test]
    public void FontFaceCachesSimpleGlyphsAndLeavesCompositeGlyphsForCompositeDecoder()
    {
        byte[] simple = BuildComplexSimpleGlyph();
        byte[] composite = BuildGlyphHeader(-1);
        byte[] glyf = Join(simple, composite);
        byte[] loca = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)(simple.Length / 2))
            .WriteUInt16((ushort)(glyf.Length / 2))
            .CopyBytes();
        SfntTableCache cache = CreateFaceCache(
            BuildHead(),
            BuildTrueTypeMaxp(2, 5, 2, 2),
            loca,
            glyf);
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);

        SfntSimpleGlyph? first = face.GetSimpleGlyph(0);
        SfntSimpleGlyph? second = face.GetSimpleGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first!.GetPointCount(), Is.EqualTo(5));
            Assert.That(face.GetSimpleGlyphOutline(0),
                Is.SameAs(first.GetNormalizedOutline()));
            Assert.That(face.GetSimpleGlyph(1), Is.Null);
            Assert.That(face.GetSimpleGlyphOutline(1), Is.Null);
        });
    }

    static void AssertPoint(
        SfntSimpleGlyphPoint point,
        int index,
        int flags,
        bool onCurve,
        int x,
        int y,
        int xDelta,
        int yDelta)
    {
        Assert.Multiple(() =>
        {
            Assert.That(point.GetPointIndex(), Is.EqualTo(index));
            Assert.That(point.GetFlags(), Is.EqualTo(flags));
            Assert.That(point.IsOnCurve(), Is.EqualTo(onCurve));
            Assert.That(point.GetX(), Is.EqualTo(x));
            Assert.That(point.GetY(), Is.EqualTo(y));
            Assert.That(point.GetXDelta(), Is.EqualTo(xDelta));
            Assert.That(point.GetYDelta(), Is.EqualTo(yDelta));
        });
    }

    static SfntSimpleGlyph? Parse(
        byte[] bytes,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        return SfntSimpleGlyphParser.Parse(CreateRecord(bytes), maxp, limits);
    }

    static SfntGlyfRecord CreateRecord(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntGlyfRecord(7, 0, data.GetSlice(0, data.GetLength()));
    }

    static SfntMaxpTable CreateMaxp(
        int maxPoints,
        int maxContours,
        int maxInstructionSize)
    {
        return new SfntMaxpTable(
            0x00010000U,
            SfntMaxpKind.TrueType,
            10,
            maxPoints,
            maxContours,
            0,
            0,
            1,
            0,
            0,
            0,
            0,
            0,
            maxInstructionSize,
            0,
            0);
    }

    static FontLimits CreateLimits(int maxPoints, int maxContours)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            100,
            maxPoints,
            maxContours,
            10,
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

    static byte[] BuildComplexSimpleGlyph()
    {
        return new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(2))
            .WriteUInt16(2)
            .WriteUInt16(4)
            .WriteUInt16(2)
            .WriteBytes(new byte[] { 0xAA, 0xBB })
            .WriteUInt8(0x77)
            .WriteUInt8(0x02)
            .WriteUInt8(0x05)
            .WriteUInt8(0x39)
            .WriteUInt8(1)
            .WriteUInt8(10)
            .WriteUInt8(3)
            .WriteInt16(300)
            .WriteUInt8(20)
            .WriteInt16(-2)
            .WriteUInt8(4)
            .WriteUInt8(0)
            .CopyBytes();
    }

    static byte[] BuildOnePointGlyph(byte[] flags)
    {
        return new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(1))
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteBytes(flags)
            .CopyBytes();
    }

    static byte[] BuildRepeatedZeroGlyph(int contourCount, int pointCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteBytes(BuildGlyphHeader(contourCount));

        if (contourCount == 1)
        {
            builder.WriteUInt16((ushort)(pointCount - 1));
        }
        else
        {
            for (int index = 0; index < contourCount; index += 1)
            {
                builder.WriteUInt16((ushort)index);
            }
        }

        builder.WriteUInt16(0);
        int remaining = pointCount;

        while (remaining != 0)
        {
            int repeated = Math.Min(remaining, 256);
            builder.WriteUInt8(0x39);
            builder.WriteUInt8((byte)(repeated - 1));
            remaining -= repeated;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildGlyphHeader(int contourCount)
    {
        return new TestBinaryBuilder()
            .WriteInt16((short)contourCount)
            .WriteInt16(7)
            .WriteInt16(14)
            .WriteInt16(307)
            .WriteInt16(20)
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
        int maxPoints,
        int maxContours,
        int maxInstructionSize)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16((ushort)maxPoints)
            .WriteUInt16((ushort)maxContours)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)maxInstructionSize)
            .WriteUInt16(0)
            .WriteUInt16(0)
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

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }
}
