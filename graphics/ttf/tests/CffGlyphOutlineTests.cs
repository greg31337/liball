using ttf;

namespace ttf.Tests;

internal class CffGlyphOutlineTests
{
    [Test]
    public void NormalizesCubicPathAdvanceAndExactBounds()
    {
        SfntCffTable? table = ParseTable(
            BuildOutlineCff(),
            FontLimits.CreateCompatible());
        CffGlyph glyph = table!.GetNormalizedGlyph(4);
        SfntGlyphOutline outline = glyph.GetOutline();
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(4));
            Assert.That(glyph.GetAdvanceWidth(), Is.EqualTo(150));
            Assert.That(glyph.GetExecutionResult().HasExplicitWidth(), Is.True);
            Assert.That(outline.GetContourCount(), Is.EqualTo(1));
            Assert.That(outline.GetNormalizedPointCount(), Is.EqualTo(4));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(3));
            Assert.That(outline.GetCommand(0).GetKind(), Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(outline.GetCommand(1).GetKind(), Is.EqualTo(SfntPathCommandKind.Cubic));
            Assert.That(outline.GetCommand(2).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(100));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(75).Within(0.000001));
            Assert.That(glyph.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void ClosesContoursAndDropsEmptyMoves()
    {
        SfntCffTable? table = ParseTable(
            BuildOutlineCff(),
            FontLimits.CreateCompatible());
        SfntGlyphOutline outline = table!.GetNormalizedGlyph(5).GetOutline();
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(outline.GetContourCount(), Is.EqualTo(2));
            Assert.That(outline.GetNormalizedPointCount(), Is.EqualTo(4));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(6));
            Assert.That(outline.GetCommand(2).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(outline.GetCommand(5).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(20));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(30));
        });
    }

    [Test]
    public void ResolvesCompatibilityComponentsAndReportsDiagnostic()
    {
        SfntCffTable? table = ParseTable(
            BuildOutlineCff(),
            FontLimits.CreateCompatible());
        CffGlyph glyph = table!.GetNormalizedGlyph(3);
        SfntGlyphOutline outline = glyph.GetOutline();
        SfntPathBounds bounds = outline.GetBounds();
        FontDiagnostic diagnostic = glyph.GetDiagnostic(0);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetAdvanceWidth(), Is.EqualTo(500));
            Assert.That(outline.GetContourCount(), Is.EqualTo(2));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(8));
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(22));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(32));
            Assert.That(diagnostic.GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Information));
            Assert.That(diagnostic.GetCode(), Is.EqualTo(FontDiagnosticCode.DeprecatedOutlineOperator));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Cff));
            Assert.That(diagnostic.GetFaceIndex(), Is.EqualTo(-1));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(-1));
            Assert.That(diagnostic.GetLogicalPath(), Is.EqualTo("CFF.CharStrings[3]"));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph.GetDiagnostic(1));
        });
    }

    [Test]
    public void CachesGlyphAndProducesStableDotSectionDiagnostic()
    {
        SfntCffTable? table = ParseTable(
            BuildOutlineCff(),
            FontLimits.CreateCompatible());
        CffGlyph first = table!.GetNormalizedGlyph(6);
        CffGlyph second = table.GetNormalizedGlyph(6);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.GetOutline().GetBounds().IsEmpty(), Is.True);
            Assert.That(first.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(first.GetDiagnostic(0).GetMessage(), Is.EqualTo(
                "The Type 2 charstring uses a deprecated compatibility operator."));
            Assert.That(first.GetDiagnostic(0).GetLogicalPath(), Is.EqualTo(
                "CFF.CharStrings[6]"));
        });
    }

    [Test]
    public void RejectsNestedAndUnresolvableCompatibilityComponents()
    {
        SfntCffTable? table = ParseTable(
            BuildOutlineCff(),
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => table!.GetNormalizedGlyph(7));
            Assert.Throws<FontFormatException>(() => table!.GetNormalizedGlyph(8));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetNormalizedGlyph(9));
            Assert.Throws<ArgumentNullException>(() => CffGlyphNormalizer.Normalize(
                null!,
                0,
                FontLimits.CreateCompatible()));
            Assert.Throws<ArgumentNullException>(() => CffGlyphNormalizer.Normalize(
                table!,
                0,
                null!));
        });
    }

    [Test]
    public void EnforcesNormalizedPointAndContourLimits()
    {
        SfntCffTable? pointLimited = ParseTable(
            BuildOutlineCff(),
            CreateLimits(3, 10));
        SfntCffTable? contourLimited = ParseTable(
            BuildOutlineCff(),
            CreateLimits(100, 1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => pointLimited!.GetNormalizedGlyph(4));
            Assert.Throws<FontLimitException>(() => contourLimited!.GetNormalizedGlyph(5));
        });
    }

    static byte[] BuildOutlineCff()
    {
        byte[] strings = BuildIndex();
        byte[] charStrings = BuildIndex(
            new byte[] { 14 },
            new byte[] { 139, 139, 21, 149, 139, 5, 139, 149, 5, 14 },
            new byte[] { 139, 139, 21, 141, 139, 5, 139, 141, 5, 14 },
            new byte[] { 159, 169, 204, 205, 14 },
            new byte[] { 189, 139, 139, 21, 139, 239, 239, 139, 139, 39, 8, 14 },
            new byte[]
            {
                139, 139, 21, 149, 139, 5,
                149, 159, 21, 139, 149, 5,
                218, 218, 21, 14
            },
            new byte[] { 12, 0, 14 },
            new byte[] { 139, 139, 206, 205, 14 },
            new byte[] { 139, 139, 140, 205, 14 });
        byte[] charset = Join(
            new byte[] { 0 },
            UInt16(34),
            UInt16(35),
            UInt16(36),
            UInt16(37),
            UInt16(38),
            UInt16(39),
            UInt16(40),
            UInt16(41));
        byte[] privateValues = Join(
            EncodeInteger(500), Operator(CffDictOperator.DefaultWidthX),
            EncodeInteger(100), Operator(CffDictOperator.NominalWidthX));
        int prefixLength = BuildPrefix(
            BuildTop(0, 0, privateValues.Length, 0),
            strings).Length;

        for (int attempt = 0; attempt < 4; attempt += 1)
        {
            int charStringsOffset = prefixLength;
            int charsetOffset = charStringsOffset + charStrings.Length;
            int privateOffset = charsetOffset + charset.Length;
            byte[] top = BuildTop(
                charStringsOffset,
                charsetOffset,
                privateValues.Length,
                privateOffset);
            byte[] prefix = BuildPrefix(top, strings);

            if (prefix.Length == prefixLength)
            {
                return Join(prefix, charStrings, charset, privateValues);
            }

            prefixLength = prefix.Length;
        }

        throw new InvalidOperationException(
            "The test outline CFF offsets did not stabilize.");
    }

    static byte[] BuildTop(
        int charStringsOffset,
        int charsetOffset,
        int privateSize,
        int privateOffset)
    {
        return Join(
            EncodeInteger(charsetOffset), Operator(CffDictOperator.Charset),
            EncodeInteger(charStringsOffset), Operator(CffDictOperator.CharStrings),
            EncodeInteger(privateSize), EncodeInteger(privateOffset),
            Operator(CffDictOperator.Private));
    }

    static byte[] BuildPrefix(byte[] top, byte[] strings)
    {
        return Join(
            new byte[] { 1, 0, 4, 1 },
            BuildIndex(new byte[] { (byte)'X' }),
            BuildIndex(top),
            strings,
            BuildIndex());
    }

    static SfntCffTable? ParseTable(byte[] bytes, FontLimits limits)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
        return SfntCffTableParser.FindTable(cache);
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
            1000000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static byte[] BuildIndex(params byte[][] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)values.Length);

        if (values.Length == 0)
        {
            return builder.CopyBytes();
        }

        int dataLength = 0;

        foreach (byte[] value in values)
        {
            dataLength += value.Length;
        }

        int offsetSize = dataLength + 1 <= 255 ? 1 : 2;
        builder.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteOffset(builder, offsetSize, offset);

        foreach (byte[] value in values)
        {
            offset += value.Length;
            WriteOffset(builder, offsetSize, offset);
        }

        foreach (byte[] value in values)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static void WriteOffset(
        TestBinaryBuilder builder,
        int offsetSize,
        int offset)
    {
        if (offsetSize == 1)
        {
            builder.WriteUInt8((byte)offset);
        }
        else
        {
            builder.WriteUInt16((ushort)offset);
        }
    }

    static byte[] EncodeInteger(int value)
    {
        if (value >= -107 && value <= 107)
        {
            return new byte[] { (byte)(value + 139) };
        }

        if (value >= 108 && value <= 1131)
        {
            int adjusted = value - 108;
            return new byte[]
            {
                (byte)(247 + adjusted / 256),
                (byte)(adjusted % 256)
            };
        }

        return new TestBinaryBuilder()
            .WriteUInt8(28)
            .WriteInt16((short)value)
            .CopyBytes();
    }

    static byte[] Operator(int operatorCode)
    {
        return new byte[] { (byte)operatorCode };
    }

    static byte[] UInt16(int value)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)value)
            .CopyBytes();
    }

    static byte[] Join(params byte[][] values)
    {
        int length = 0;

        foreach (byte[] value in values)
        {
            length += value.Length;
        }

        byte[] result = new byte[length];
        int offset = 0;

        foreach (byte[] value in values)
        {
            value.CopyTo(result, offset);
            offset += value.Length;
        }

        return result;
    }
}
