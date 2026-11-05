using ttf;

namespace ttf.Tests;

internal class SfntColorPaintGraphTests
{
    [Test]
    public void NormalizesVersionZeroLayersToExplicitColorNodes()
    {
        SfntColrLayerRecord[] layers = new SfntColrLayerRecord[]
        {
            new SfntColrLayerRecord(5, 7),
            new SfntColrLayerRecord(6, 0xFFFF)
        };
        SfntColrBaseGlyphRecord record =
            new SfntColrBaseGlyphRecord(2, 0, 2, layers);
        SfntColrTable table = SfntColrTable.Create(
            0,
            10,
            new SfntColrBaseGlyphRecord[] { record },
            layers,
            null!);

        SfntColorGlyphGraph? graph = SfntColrGraphNormalizer.Normalize(
            table,
            2,
            FontLimits.CreateCompatible());
        SfntColorPaintNode root = graph!.GetRoot();
        SfntColorPaintNode firstGlyph = root.GetChild(0);
        SfntColorPaintNode firstColor = firstGlyph.GetChild(0);
        SfntColorPaintNode secondColor = root.GetChild(1).GetChild(0);

        Assert.Multiple(() =>
        {
            Assert.That(graph.GetGlyphId(), Is.EqualTo(2));
            Assert.That(graph.GetColrVersion(), Is.Zero);
            Assert.That(graph.GetNodeCount(), Is.EqualTo(5));
            Assert.That(graph.GetMaximumDepth(), Is.EqualTo(3));
            Assert.That(root.GetKind(), Is.EqualTo(SfntColorPaintKind.LayerList));
            Assert.That(firstGlyph.GetKind(), Is.EqualTo(SfntColorPaintKind.Glyph));
            Assert.That(firstGlyph.GetGlyphId(), Is.EqualTo(5));
            Assert.That(firstColor.GetKind(), Is.EqualTo(SfntColorPaintKind.PaletteColor));
            Assert.That(firstColor.GetPaletteEntryIndex(), Is.EqualTo(7));
            Assert.That(firstColor.GetAlpha(), Is.EqualTo(1));
            Assert.That(secondColor.GetKind(), Is.EqualTo(SfntColorPaintKind.ForegroundColor));
            Assert.That(secondColor.GetAlpha(), Is.EqualTo(1));
            Assert.That(graph.GetReferencedGlyphCount(), Is.EqualTo(2));
            Assert.That(graph.GetReferencedGlyphId(0), Is.EqualTo(5));
            Assert.That(graph.GetReferencedGlyphId(1), Is.EqualTo(6));
        });
    }

    [Test]
    public void NormalizesVersionOneLayersGradientsTransformsAndComposites()
    {
        SfntColrPaint palette = CreateSolid(10, 3, 0.25);
        SfntColrPaint foreground = CreateSolid(20, 0xFFFF, 0.75);
        SfntColrPaint gradient = CreateLinearGradient(30);
        SfntColrPaint glyph = CreatePaint(
            40,
            10,
            0,
            0,
            7,
            0,
            new int[] { 30 },
            new SfntColrVariableValue[0],
            null,
            null,
            SfntColrCompositeMode.Clear);
        SfntColrPaint translate = CreatePaint(
            50,
            14,
            0,
            0,
            0,
            0,
            new int[] { 40 },
            new SfntColrVariableValue[] {
                CreateValue(10, 1), CreateValue(20, 1)
            },
            null,
            null,
            SfntColrCompositeMode.Clear);
        SfntColrPaint composite = CreatePaint(
            60,
            32,
            0,
            0,
            0,
            0,
            new int[] { 50, 20 },
            new SfntColrVariableValue[0],
            null,
            null,
            SfntColrCompositeMode.SourceOver);
        SfntColrPaint rootPaint = CreatePaint(
            70,
            1,
            0,
            2,
            0,
            0,
            new int[0],
            new SfntColrVariableValue[0],
            null,
            null,
            SfntColrCompositeMode.Clear);
        SfntColrPaint[] paints = new SfntColrPaint[] {
            palette, foreground, gradient, glyph, translate, composite, rootPaint
        };
        SfntColrTable table = CreateVersionOneTable(
            new SfntColrBaseGlyphPaintRecord[] {
                new SfntColrBaseGlyphPaintRecord(2, 70, rootPaint)
            },
            new SfntColrLayerList(100, new SfntColrPaint[] { composite, palette }),
            paints);

        SfntColorGlyphGraph? graph = SfntColrGraphNormalizer.Normalize(
            table,
            2,
            FontLimits.CreateCompatible());
        SfntColorPaintNode root = graph!.GetRoot();
        SfntColorPaintNode normalizedComposite = root.GetChild(0);
        SfntColorPaintNode transform = normalizedComposite.GetChild(0);
        SfntColorPaintNode normalizedGradient = transform.GetChild(0).GetChild(0);
        SfntColorPaintLine line = normalizedGradient.GetColorLine();

        Assert.Multiple(() =>
        {
            Assert.That(graph.GetNodeCount(), Is.EqualTo(9));
            Assert.That(graph.GetMaximumDepth(), Is.EqualTo(6));
            Assert.That(root.GetKind(), Is.EqualTo(SfntColorPaintKind.LayerList));
            Assert.That(normalizedComposite.GetKind(), Is.EqualTo(SfntColorPaintKind.Composite));
            Assert.That(normalizedComposite.GetCompositeMode(),
                Is.EqualTo(SfntColrCompositeMode.SourceOver));
            Assert.That(transform.GetKind(), Is.EqualTo(SfntColorPaintKind.Transform));
            Assert.That(transform.GetValue(0), Is.EqualTo(1));
            Assert.That(transform.GetValue(3), Is.EqualTo(1));
            Assert.That(transform.GetValue(4), Is.EqualTo(10));
            Assert.That(transform.GetValue(5), Is.EqualTo(20));
            Assert.That(normalizedGradient.GetKind(),
                Is.EqualTo(SfntColorPaintKind.LinearGradient));
            Assert.That(line.GetStop(0).GetColor().GetKind(),
                Is.EqualTo(SfntColorPaintKind.PaletteColor));
            Assert.That(line.GetStop(0).GetColor().GetPaletteEntryIndex(), Is.EqualTo(4));
            Assert.That(line.GetStop(0).GetColor().GetAlpha(), Is.EqualTo(0.5));
            Assert.That(line.GetStop(1).GetColor().GetKind(),
                Is.EqualTo(SfntColorPaintKind.ForegroundColor));
            Assert.That(normalizedComposite.GetChild(1).GetAlpha(), Is.EqualTo(0.75));
            Assert.That(root.GetChild(1).GetPaletteEntryIndex(), Is.EqualTo(3));
            Assert.That(graph.GetReferencedGlyphId(0), Is.EqualTo(7));
        });
    }

    [Test]
    public void RejectsPaintColrGlyphCycles()
    {
        SfntColrPaint first = CreateColrGlyph(10, 2);
        SfntColrPaint second = CreateColrGlyph(20, 1);
        SfntColrTable table = CreateVersionOneTable(
            new SfntColrBaseGlyphPaintRecord[] {
                new SfntColrBaseGlyphPaintRecord(1, 10, first),
                new SfntColrBaseGlyphPaintRecord(2, 20, second)
            },
            null,
            new SfntColrPaint[] { first, second });

        FontFormatException? exception = Assert.Throws<FontFormatException>(() =>
            SfntColrGraphNormalizer.Normalize(
                table,
                1,
                FontLimits.CreateCompatible()));

        Assert.That(exception!.Message, Does.Contain("cycle"));
        Assert.That(exception.GetDiagnosticCode(),
            Is.EqualTo(FontDiagnosticCode.ColorPaintCycle));
    }

    [Test]
    public void EnforcesPaintDepthAndNodeCountLimits()
    {
        SfntColrTable table = CreateLayeredVersionOneTable();
        FontLimitException? depth = Assert.Throws<FontLimitException>(() =>
            SfntColrGraphNormalizer.Normalize(table, 2, CreateLimits(100, 5)));
        FontLimitException? nodes = Assert.Throws<FontLimitException>(() =>
            SfntColrGraphNormalizer.Normalize(table, 2, CreateLimits(8, 20)));

        Assert.Multiple(() =>
        {
            Assert.That(depth!.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.ColorPaintDepthExceeded));
            Assert.That(nodes!.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.ColorPaintNodeLimitExceeded));
        });
    }

    [Test]
    public void ReturnsNullForGlyphWithoutAColorDescription()
    {
        SfntColrTable table = SfntColrTable.Create(
            0,
            4,
            new SfntColrBaseGlyphRecord[0],
            new SfntColrLayerRecord[0],
            null!);

        Assert.That(SfntColrGraphNormalizer.Normalize(
            table,
            3,
            FontLimits.CreateCompatible()), Is.Null);
    }

    static SfntColrTable CreateLayeredVersionOneTable()
    {
        SfntColrPaint palette = CreateSolid(10, 3, 0.25);
        SfntColrPaint foreground = CreateSolid(20, 0xFFFF, 0.75);
        SfntColrPaint gradient = CreateLinearGradient(30);
        SfntColrPaint glyph = CreatePaint(
            40, 10, 0, 0, 7, 0, new int[] { 30 },
            new SfntColrVariableValue[0], null, null,
            SfntColrCompositeMode.Clear);
        SfntColrPaint translate = CreatePaint(
            50, 14, 0, 0, 0, 0, new int[] { 40 },
            new SfntColrVariableValue[] {
                CreateValue(10, 1), CreateValue(20, 1)
            },
            null, null, SfntColrCompositeMode.Clear);
        SfntColrPaint composite = CreatePaint(
            60, 32, 0, 0, 0, 0, new int[] { 50, 20 },
            new SfntColrVariableValue[0], null, null,
            SfntColrCompositeMode.SourceOver);
        SfntColrPaint root = CreatePaint(
            70, 1, 0, 2, 0, 0, new int[0],
            new SfntColrVariableValue[0], null, null,
            SfntColrCompositeMode.Clear);
        return CreateVersionOneTable(
            new SfntColrBaseGlyphPaintRecord[] {
                new SfntColrBaseGlyphPaintRecord(2, 70, root)
            },
            new SfntColrLayerList(100, new SfntColrPaint[] { composite, palette }),
            new SfntColrPaint[] {
                palette, foreground, gradient, glyph, translate, composite, root
            });
    }

    static SfntColrPaint CreateLinearGradient(int offset)
    {
        SfntColrColorStop[] stops = new SfntColrColorStop[]
        {
            new SfntColrColorStop(
                4,
                CreateValue(0, 16384),
                CreateValue(8192, 16384),
                -1),
            new SfntColrColorStop(
                0xFFFF,
                CreateValue(16384, 16384),
                CreateValue(16384, 16384),
                -1)
        };
        SfntColrColorLine line = new SfntColrColorLine(
            0,
            SfntColrExtendMode.Pad,
            false,
            stops);
        return CreatePaint(
            offset,
            4,
            0,
            0,
            0,
            0,
            new int[0],
            new SfntColrVariableValue[] {
                CreateValue(0, 1), CreateValue(0, 1), CreateValue(100, 1),
                CreateValue(0, 1), CreateValue(0, 1), CreateValue(100, 1)
            },
            line,
            null,
            SfntColrCompositeMode.Clear);
    }

    static SfntColrPaint CreateSolid(
        int offset,
        int paletteEntryIndex,
        double alpha)
    {
        return CreatePaint(
            offset,
            2,
            0,
            0,
            0,
            paletteEntryIndex,
            new int[0],
            new SfntColrVariableValue[] {
                CreateValue((long)(alpha * 16384), 16384)
            },
            null,
            null,
            SfntColrCompositeMode.Clear);
    }

    static SfntColrPaint CreateColrGlyph(int offset, int glyphId)
    {
        return CreatePaint(
            offset,
            11,
            0,
            0,
            glyphId,
            0,
            new int[0],
            new SfntColrVariableValue[0],
            null,
            null,
            SfntColrCompositeMode.Clear);
    }

    static SfntColrPaint CreatePaint(
        int offset,
        int format,
        int firstLayerIndex,
        int layerCount,
        int glyphId,
        int paletteEntryIndex,
        int[] childOffsets,
        SfntColrVariableValue[] values,
        SfntColrColorLine? line,
        SfntColrAffineTransform? transform,
        SfntColrCompositeMode mode)
    {
        return new SfntColrPaint(
            offset,
            format,
            true,
            false,
            firstLayerIndex,
            layerCount,
            glyphId,
            paletteEntryIndex,
            -1,
            (int)mode,
            mode,
            childOffsets,
            values,
            line,
            transform);
    }

    static SfntColrVariableValue CreateValue(long rawValue, double divisor)
    {
        return new SfntColrVariableValue(
            rawValue,
            divisor,
            0,
            0,
            false,
            false,
            -1,
            new SfntColrVariationData(null, null));
    }

    static SfntColrTable CreateVersionOneTable(
        SfntColrBaseGlyphPaintRecord[] records,
        SfntColrLayerList? layerList,
        SfntColrPaint[] paints)
    {
        return SfntColrTable.Create(
            1,
            10,
            new SfntColrBaseGlyphRecord[0],
            new SfntColrLayerRecord[0],
            null!,
            records,
            layerList,
            null,
            paints,
            new SfntColrVariationData(null, null));
    }

    static FontLimits CreateLimits(int maximumNodes, int maximumDepth)
    {
        return FontLimits.Create(
            1000,
            1000,
            10,
            100,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            1000,
            maximumNodes,
            1000,
            1000,
            10,
            100,
            maximumDepth,
            100);
    }
}
