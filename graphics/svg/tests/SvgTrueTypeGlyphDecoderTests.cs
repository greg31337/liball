namespace svg.Tests;

public class SvgTrueTypeGlyphDecoderTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void DecodesSimpleQuadraticContoursInstructionsPhantomsAndLocaFormats(bool longLocations)
    {
        SvgFont font = CreateFont(longLocations, Array.Empty<byte>(), BuildCurveGlyph(), BuildCompositeGlyph(), BuildRepeatedRectangleGlyph());

        SvgTrueTypeGlyphResult result = font.DecodeTrueTypeGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgTrueTypeGlyphError.None));
            Assert.That(result.Outline.Composite, Is.False);
            Assert.That(result.Outline.Empty, Is.False);
            Assert.That(result.Outline.InstructionLength, Is.EqualTo(2));
            Assert.That(result.Outline.Points.Select(point => point.X), Is.EqualTo(new[] { 0, 50, 100, 150 }));
            Assert.That(result.Outline.Points.Select(point => point.Y), Is.EqualTo(new[] { 0, 100, 100, 0 }));
            Assert.That(result.Outline.Points.Select(point => point.OnCurve), Is.EqualTo(new[] { true, false, false, true }));
            Assert.That(result.Outline.PhantomPoints, Has.Count.EqualTo(4));
            Assert.That(result.Outline.PhantomPoints[0].X, Is.EqualTo(-10));
            Assert.That(result.Outline.PhantomPoints[1].X, Is.EqualTo(590));
            Assert.That(result.Outline.PhantomPoints[2].Y, Is.EqualTo(120));
            Assert.That(result.Outline.PhantomPoints[3].Y, Is.EqualTo(-880));
            Assert.That(result.Outline.HasVerticalMetrics, Is.True);
            Assert.That(result.Outline.Path.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.ClosePath
            }));
            Assert.That(result.Outline.Path.Commands[1].Values, Is.EqualTo(new[] { 50, 100, 75, 100 }));
            Assert.That(result.Outline.Path.Commands[2].Values, Is.EqualTo(new[] { 100, 100, 150, 0 }));
        });
    }

    [Test]
    public void ExpandsCompositeOffsetsMatricesPointAlignmentAndUseMyMetrics()
    {
        SvgFont font = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildCompositeGlyph(), BuildRepeatedRectangleGlyph());

        SvgTrueTypeGlyphResult result = new SvgTrueTypeGlyphDecoder().DecodeWithFontAndGlyphIndex(font, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Outline.Composite, Is.True);
            Assert.That(result.Outline.Components, Has.Count.EqualTo(3));
            Assert.That(result.Outline.Contours, Has.Count.EqualTo(3));
            Assert.That(result.Outline.Points, Has.Count.EqualTo(12));
            Assert.That(result.Outline.Components[0].OffsetX, Is.EqualTo(10));
            Assert.That(result.Outline.Components[0].OffsetY, Is.EqualTo(20));
            Assert.That(result.Outline.Components[0].UsesMetrics, Is.True);
            Assert.That(result.Outline.Points[0].X, Is.EqualTo(10));
            Assert.That(result.Outline.Points[0].Y, Is.EqualTo(20));
            Assert.That(result.Outline.Components[1].A, Is.Zero);
            Assert.That(result.Outline.Components[1].B, Is.EqualTo(1));
            Assert.That(result.Outline.Components[1].C, Is.EqualTo(-1));
            Assert.That(result.Outline.Points[5].X, Is.EqualTo(100));
            Assert.That(result.Outline.Points[5].Y, Is.EqualTo(50));
            Assert.That(result.Outline.Components[2].UsesXYOffset, Is.False);
            Assert.That(result.Outline.Components[2].OffsetX, Is.EqualTo(160));
            Assert.That(result.Outline.Components[2].OffsetY, Is.EqualTo(20));
            Assert.That(result.Outline.InstructionLength, Is.EqualTo(2));
            Assert.That(result.Outline.MetricsGlyphIndex, Is.EqualTo(1));
            Assert.That(result.Outline.PhantomPoints[0].X, Is.Zero);
            Assert.That(result.Outline.PhantomPoints[1].X, Is.EqualTo(600));
            Assert.That(result.Outline.PhantomPoints[2].Y, Is.EqualTo(140));
            Assert.That(result.Outline.Path.GetSubpathCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ExpandsRepeatedFlagsAndReturnsAnEmptyMissingGlyphPath()
    {
        SvgFont font = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildCompositeGlyph(), BuildRepeatedRectangleGlyph());

        SvgTrueTypeGlyphResult rectangle = font.DecodeTrueTypeGlyph(3);
        SvgTrueTypeGlyphResult missing = font.DecodeTrueTypeGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(rectangle.Success, Is.True);
            Assert.That(rectangle.Outline.Points.Select(point => point.X), Is.EqualTo(new[] { 0, 100, 100, 0 }));
            Assert.That(rectangle.Outline.Points.Select(point => point.Y), Is.EqualTo(new[] { 0, 0, 100, 100 }));
            Assert.That(rectangle.Outline.Path.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.ClosePath
            }));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Outline.Empty, Is.True);
            Assert.That(missing.Outline.Points, Is.Empty);
            Assert.That(missing.Outline.Path.Commands, Is.Empty);
            Assert.That(missing.Outline.PhantomPoints, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void EnforcesPointComponentRecursionAndCycleBounds()
    {
        SvgFont font = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildCompositeGlyph(), BuildRepeatedRectangleGlyph());
        SvgTrueTypeGlyphOptions pointOptions = new SvgTrueTypeGlyphOptions();
        pointOptions.Limits.MaxPointsPerGlyph = 3;
        SvgTrueTypeGlyphOptions componentOptions = new SvgTrueTypeGlyphOptions();
        componentOptions.MaximumComponents = 2;

        SvgTrueTypeGlyphResult points = font.DecodeTrueTypeGlyphWithOptions(1, pointOptions);
        SvgTrueTypeGlyphResult components = font.DecodeTrueTypeGlyphWithOptions(2, componentOptions);

        SvgFont nestedFont = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildOneComponentGlyph(1), BuildOneComponentGlyph(2));
        nestedFont.MaximumProfile.MaximumComponentDepth = 3;
        SvgTrueTypeGlyphOptions depthOptions = new SvgTrueTypeGlyphOptions();
        depthOptions.MaximumCompositeDepth = 1;
        SvgTrueTypeGlyphResult depth = nestedFont.DecodeTrueTypeGlyphWithOptions(3, depthOptions);

        SvgFont cyclicFont = CreateFont(false, Array.Empty<byte>(), BuildOneComponentGlyph(1));
        SvgTrueTypeGlyphResult cycle = cyclicFont.DecodeTrueTypeGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(points.Error, Is.EqualTo(SvgTrueTypeGlyphError.PointLimitExceeded));
            Assert.That(components.Error, Is.EqualTo(SvgTrueTypeGlyphError.ComponentLimitExceeded));
            Assert.That(depth.Error, Is.EqualTo(SvgTrueTypeGlyphError.RecursionLimitExceeded));
            Assert.That(cycle.Error, Is.EqualTo(SvgTrueTypeGlyphError.CyclicComposite));
        });
    }

    [Test]
    public void RejectsMalformedLocationsInstructionsAndComponentPointReferences()
    {
        SvgFont badLocations = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildCompositeGlyph());
        WriteUInt16(badLocations.Bytes, 6, 1);
        SvgFont badInstructions = CreateFont(false, Array.Empty<byte>(), BuildTruncatedInstructionGlyph());
        SvgFont badPoint = CreateFont(false, Array.Empty<byte>(), BuildCurveGlyph(), BuildBadPointComponentGlyph());

        SvgTrueTypeGlyphResult locations = badLocations.DecodeTrueTypeGlyph(1);
        SvgTrueTypeGlyphResult instructions = badInstructions.DecodeTrueTypeGlyph(1);
        SvgTrueTypeGlyphResult point = badPoint.DecodeTrueTypeGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(locations.Error, Is.EqualTo(SvgTrueTypeGlyphError.InvalidLocation));
            Assert.That(instructions.Error, Is.EqualTo(SvgTrueTypeGlyphError.InvalidGlyphData));
            Assert.That(point.Error, Is.EqualTo(SvgTrueTypeGlyphError.InvalidGlyphData));
        });
    }

    internal static SvgFont CreateFont(bool longLocations, params byte[][] glyphs)
    {
        int glyphCount = glyphs.Length;
        int locationWidth = longLocations ? 4 : 2;
        int locationLength = (glyphCount + 1) * locationWidth;
        int[] offsets = new int[glyphCount + 1];
        int glyphLength = 0;
        int index = 0;
        while (index < glyphCount)
        {
            offsets[index] = glyphLength;
            glyphLength = glyphLength + glyphs[index].Length;
            if (!longLocations && glyphLength % 2 != 0) glyphLength = glyphLength + 1;
            index = index + 1;
        }
        offsets[glyphCount] = glyphLength;
        byte[] bytes = new byte[locationLength + glyphLength];
        index = 0;
        while (index <= glyphCount)
        {
            if (longLocations) WriteUInt32(bytes, index * 4, (uint)offsets[index]);
            else WriteUInt16(bytes, index * 2, offsets[index] / 2);
            index = index + 1;
        }
        index = 0;
        while (index < glyphCount)
        {
            Array.Copy(glyphs[index], 0, bytes, locationLength + offsets[index], glyphs[index].Length);
            index = index + 1;
        }

        SvgFont font = new SvgFont(bytes, false);
        font.OutlineKind = SvgOpenTypeOutlineKind.TrueType;
        font.Tables.Add(new SvgOpenTypeTable("glyf", 0, locationLength, glyphLength));
        font.Tables.Add(new SvgOpenTypeTable("loca", 0, 0, locationLength));
        font.Head = new SvgOpenTypeHeadTable();
        font.Head.IndexToLocationFormat = longLocations ? 1 : 0;
        font.Head.UnitsPerEm = 1000;
        font.MaximumProfile = new SvgOpenTypeMaximumProfileTable();
        font.MaximumProfile.GlyphCount = glyphCount;
        font.MaximumProfile.MaximumPoints = 32;
        font.MaximumProfile.MaximumContours = 8;
        font.MaximumProfile.MaximumCompositePoints = 64;
        font.MaximumProfile.MaximumCompositeContours = 16;
        font.MaximumProfile.MaximumComponentElements = 8;
        font.MaximumProfile.MaximumComponentDepth = 4;
        font.VerticalHeader = new SvgOpenTypeVerticalHeaderTable();
        index = 0;
        while (index < glyphCount)
        {
            font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(index, 500 + index * 100, index * 10));
            font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(index, 1000, 20));
            index = index + 1;
        }
        font.Success = true;
        return font;
    }

    static byte[] BuildCurveGlyph()
    {
        byte[] bytes = new byte[25];
        WriteInt16(bytes, 0, 1);
        WriteInt16(bytes, 2, 0);
        WriteInt16(bytes, 4, 0);
        WriteInt16(bytes, 6, 150);
        WriteInt16(bytes, 8, 100);
        WriteUInt16(bytes, 10, 3);
        WriteUInt16(bytes, 12, 2);
        bytes[14] = 0xB0;
        bytes[15] = 0x00;
        bytes[16] = 0x31;
        bytes[17] = 0x36;
        bytes[18] = 0x32;
        bytes[19] = 0x17;
        bytes[20] = 50;
        bytes[21] = 50;
        bytes[22] = 50;
        bytes[23] = 100;
        bytes[24] = 100;
        return bytes;
    }

    internal static byte[] BuildRepeatedRectangleGlyph()
    {
        byte[] bytes = new byte[32];
        WriteInt16(bytes, 0, 1);
        WriteInt16(bytes, 2, 0);
        WriteInt16(bytes, 4, 0);
        WriteInt16(bytes, 6, 100);
        WriteInt16(bytes, 8, 100);
        WriteUInt16(bytes, 10, 3);
        WriteUInt16(bytes, 12, 0);
        bytes[14] = 0x09;
        bytes[15] = 3;
        WriteInt16(bytes, 16, 0);
        WriteInt16(bytes, 18, 100);
        WriteInt16(bytes, 20, 0);
        WriteInt16(bytes, 22, -100);
        WriteInt16(bytes, 24, 0);
        WriteInt16(bytes, 26, 0);
        WriteInt16(bytes, 28, 100);
        WriteInt16(bytes, 30, 0);
        return bytes;
    }

    static byte[] BuildCompositeGlyph()
    {
        byte[] bytes = new byte[46];
        WriteInt16(bytes, 0, -1);
        WriteInt16(bytes, 2, -100);
        WriteInt16(bytes, 4, 0);
        WriteInt16(bytes, 6, 300);
        WriteInt16(bytes, 8, 200);
        WriteUInt16(bytes, 10, 0x0223);
        WriteUInt16(bytes, 12, 1);
        WriteInt16(bytes, 14, 10);
        WriteInt16(bytes, 16, 20);
        WriteUInt16(bytes, 18, 0x00A3);
        WriteUInt16(bytes, 20, 1);
        WriteInt16(bytes, 22, 200);
        WriteInt16(bytes, 24, 0);
        WriteInt16(bytes, 26, 0);
        WriteInt16(bytes, 28, 0x4000);
        WriteInt16(bytes, 30, -0x4000);
        WriteInt16(bytes, 32, 0);
        WriteUInt16(bytes, 34, 0x0101);
        WriteUInt16(bytes, 36, 1);
        WriteUInt16(bytes, 38, 3);
        WriteUInt16(bytes, 40, 0);
        WriteUInt16(bytes, 42, 2);
        bytes[44] = 0xB0;
        bytes[45] = 0;
        return bytes;
    }

    static byte[] BuildOneComponentGlyph(int childGlyph)
    {
        byte[] bytes = new byte[18];
        WriteInt16(bytes, 0, -1);
        WriteInt16(bytes, 2, 0);
        WriteInt16(bytes, 4, 0);
        WriteInt16(bytes, 6, 150);
        WriteInt16(bytes, 8, 100);
        WriteUInt16(bytes, 10, 0x0003);
        WriteUInt16(bytes, 12, childGlyph);
        WriteInt16(bytes, 14, 0);
        WriteInt16(bytes, 16, 0);
        return bytes;
    }

    static byte[] BuildBadPointComponentGlyph()
    {
        byte[] bytes = new byte[26];
        WriteInt16(bytes, 0, -1);
        WriteInt16(bytes, 2, 0);
        WriteInt16(bytes, 4, 0);
        WriteInt16(bytes, 6, 150);
        WriteInt16(bytes, 8, 100);
        WriteUInt16(bytes, 10, 0x0023);
        WriteUInt16(bytes, 12, 1);
        WriteInt16(bytes, 14, 0);
        WriteInt16(bytes, 16, 0);
        WriteUInt16(bytes, 18, 0x0001);
        WriteUInt16(bytes, 20, 1);
        WriteUInt16(bytes, 22, 99);
        WriteUInt16(bytes, 24, 0);
        return bytes;
    }

    static byte[] BuildTruncatedInstructionGlyph()
    {
        byte[] bytes = new byte[12];
        WriteInt16(bytes, 0, 0);
        WriteUInt16(bytes, 10, 5);
        return bytes;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        WriteUInt16(bytes, offset, value & 0xFFFF);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
