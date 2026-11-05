namespace svg.Tests;

public class SvgTextGeometryTests
{
    [Test]
    public void ConvertsPositionedGlyphsAndComputesLogicalInkAndStrokeBounds()
    {
        SvgTextLayoutOptions layoutOptions = LayoutOptions();
        layoutOptions.InitialX = 10;
        layoutOptions.InitialY = 20;
        SvgTextLayoutResult layout = Text("AB", layoutOptions);
        SvgTextGeometryOptions options = GeometryOptions();
        options.StrokeWidth = 2;

        SvgTextGeometryResult result = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgTextGeometryError.None));
            Assert.That(result.Glyphs, Has.Count.EqualTo(2));
            Assert.That(result.Path.Commands, Has.Count.EqualTo(10));
            AssertRect(result.Glyphs[0].InkBounds, 10, 19, 1, 1);
            AssertRect(result.Glyphs[1].InkBounds, 16, 19, 1, 1);
            AssertRect(result.LogicalBounds, 10, 12, 12, 10);
            AssertRect(result.InkBounds, 10, 19, 7, 1);
            AssertRect(result.StrokeBounds, 9, 18, 9, 3);
            Assert.That(result.Glyphs[0].Path, Is.Not.SameAs(result.Glyphs[1].Path));
        });
    }

    [Test]
    public void GeneratesAllDecorationsFromWritingMetrics()
    {
        SvgTextLayoutResult layout = Text("AB", LayoutOptions());
        SvgTextGeometryOptions options = GeometryOptions();
        options.Decorations.Underline = true;
        options.Decorations.Overline = true;
        options.Decorations.LineThrough = true;
        options.Decorations.ThicknessFactor = 0.1;
        options.Decorations.UnderlineOffsetFactor = 0.2;

        SvgTextGeometryResult result = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Decorations, Has.Count.EqualTo(6));
            Assert.That(result.Decorations.Count(item => item.Kind == SvgTextDecorationKind.Underline), Is.EqualTo(2));
            Assert.That(result.Decorations.Count(item => item.Kind == SvgTextDecorationKind.Overline), Is.EqualTo(2));
            Assert.That(result.Decorations.Count(item => item.Kind == SvgTextDecorationKind.LineThrough), Is.EqualTo(2));
            AssertRect(result.Decorations[0].Bounds, 0, 2, 6, 1);
            AssertRect(result.Decorations[1].Bounds, 0, -8, 6, 1);
            AssertRect(result.Decorations[2].Bounds, 0, -3, 6, 1);
            AssertRect(result.InkBounds, 0, -8, 12, 11);
            Assert.That(result.Path.Commands, Has.Count.EqualTo(40));
        });
    }

    [Test]
    public void ExtractsPositionedClustersAndRestoresAccessibleSourceOrder()
    {
        SvgTextLayoutResult layout = Text("AB", LayoutOptions());
        layout.Glyphs.Reverse();

        SvgTextGeometryResult result = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, GeometryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ExtractedText, Is.EqualTo("BA"));
            Assert.That(result.AccessibleText, Is.EqualTo("AB"));
            Assert.That(result.Clusters.Select(cluster => cluster.Text), Is.EqualTo(new[] { "B", "A" }));
            Assert.That(result.AccessibleReadingOrder, Is.EqualTo(new[] { 1, 0 }));
            Assert.That(result.Clusters.Select(cluster => cluster.ReadingOrder), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.ClusterIndex), Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void HitTestsTopmostGlyphInkLogicalCellsAndClusters()
    {
        SvgTextLayoutResult layout = Text("AB", LayoutOptions());
        layout.Glyphs.Reverse();
        SvgTextGeometryResult geometry = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, GeometryOptions());
        SvgTextHitTestOptions logicalOptions = new SvgTextHitTestOptions();
        logicalOptions.Mode = SvgTextHitTestMode.GlyphLogical;
        SvgTextHitTestOptions clusterOptions = new SvgTextHitTestOptions();
        clusterOptions.Mode = SvgTextHitTestMode.ClusterLogical;

        SvgTextHitTestResult ink = geometry.HitTest(new SvgPoint(0.5, -0.5));
        SvgTextHitTestResult logical = geometry.HitTestWithOptions(new SvgPoint(4, 1), logicalOptions);
        SvgTextHitTestResult cluster = geometry.HitTestWithOptions(new SvgPoint(7, 1), clusterOptions);
        SvgTextHitTestResult outside = geometry.HitTest(new SvgPoint(30, 30));

        Assert.Multiple(() =>
        {
            Assert.That(ink.Success, Is.True);
            Assert.That(ink.Hit, Is.True);
            Assert.That(ink.GlyphIndex, Is.EqualTo(1));
            Assert.That(ink.ClusterIndex, Is.EqualTo(1));
            Assert.That(ink.CharacterIndex, Is.Zero);
            Assert.That(logical.Hit, Is.True);
            Assert.That(logical.GlyphIndex, Is.EqualTo(1));
            Assert.That(cluster.Hit, Is.True);
            Assert.That(cluster.ClusterIndex, Is.Zero);
            Assert.That(cluster.GlyphIndex, Is.Zero);
            Assert.That(outside.Success, Is.True);
            Assert.That(outside.Hit, Is.False);
        });
    }

    [Test]
    public void ConvertsTextPathGlyphsAndOmitsHiddenOverflowFromRenderedPath()
    {
        SvgTextLayoutResult text = Text("ABC", LayoutOptions());
        SvgTextPathResult pathLayout = new SvgTextPathLayout().LayoutWithTextAndPath(text, Path("M0 0 L10 0"));

        SvgTextGeometryResult result = new SvgTextGeometry().BuildWithSvgTextPathResultAndSvgTextGeometryOptions(pathLayout, GeometryOptions());
        SvgTextGeometryOptions includedOptions = GeometryOptions();
        includedOptions.IncludeInvisibleGlyphs = true;
        SvgTextGeometryResult included = new SvgTextGeometry().BuildWithSvgTextPathResultAndSvgTextGeometryOptions(pathLayout, includedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pathLayout.Success, Is.True);
            Assert.That(pathLayout.HiddenGlyphs, Is.EqualTo(1));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Glyphs, Has.Count.EqualTo(3));
            Assert.That(result.Glyphs.Select(glyph => glyph.Visible), Is.EqualTo(new[] { true, true, false }));
            AssertRect(result.Glyphs[0].InkBounds, 0, -1, 1, 1);
            AssertRect(result.Glyphs[1].InkBounds, 6, -1, 1, 1);
            Assert.That(result.Path.Commands, Has.Count.EqualTo(10));
            Assert.That(included.Success, Is.True);
            Assert.That(included.Glyphs.Select(glyph => glyph.Visible), Is.All.True);
            Assert.That(included.Path.Commands, Has.Count.EqualTo(15));
        });
    }

    [Test]
    public void ComputesVerticalLogicalCellsAlongTheInlineAxis()
    {
        SvgShapingResult shaping = SvgTextLayoutTests.Shape("A", new Dictionary<int, int> { [0x41] = 1 });
        shaping.Glyphs[0].YAdvance = 6;
        SvgTextLayoutOptions layoutOptions = LayoutOptions();
        layoutOptions.InitialX = 2;
        layoutOptions.InitialY = 3;
        layoutOptions.WritingMode = SvgWritingMode.VerticalRightToLeft;
        SvgTextLayoutResult layout = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, layoutOptions);

        SvgTextGeometryResult result = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, GeometryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            AssertRect(result.Glyphs[0].LogicalBounds, -6, 3, 10, 6);
            AssertRect(result.Glyphs[0].InkBounds, 2, 2, 1, 1);
        });
    }

    [Test]
    public void ReportsMissingUnsupportedBoundedCancelledAndInvalidInputs()
    {
        SvgTextLayoutResult layout = Text("A", LayoutOptions());
        SvgTextGeometryOptions missingOptions = new SvgTextGeometryOptions();
        SvgTextGeometryOptions limitedOptions = GeometryOptions();
        limitedOptions.MaximumPathCommands = 4;
        SvgTextGeometryOptions cancelledOptions = GeometryOptions();
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        cancelledOptions.CancellationToken = source.Token;
        SvgTextGeometryOptions invalidOptions = GeometryOptions();
        invalidOptions.StrokeWidth = -1;
        SvgTextGeometryOptions unsupportedOptions = new SvgTextGeometryOptions();
        unsupportedOptions.Fonts.Add(new SvgTextFontResource(0, SvgOpenTypeLayoutTests.CreateFont(new Dictionary<int, int> { [0x41] = 1 }, new Dictionary<string, byte[]>()), 10));

        SvgTextGeometryResult missing = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, missingOptions);
        SvgTextGeometryResult limited = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, limitedOptions);
        SvgTextGeometryResult cancelled = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, cancelledOptions);
        SvgTextGeometryResult invalid = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, invalidOptions);
        SvgTextGeometryResult unsupported = new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, unsupportedOptions);
        SvgTextHitTestResult badPoint = new SvgTextGeometry().HitTest(new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, GeometryOptions()), new SvgPoint(0, 0), null);
        SvgTextHitTestResult nullPoint = new SvgTextGeometry().HitTest(new SvgTextGeometry().BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(layout, GeometryOptions()), null, new SvgTextHitTestOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgTextGeometryError.FontNotFound));
            Assert.That(limited.Error, Is.EqualTo(SvgTextGeometryError.LimitExceeded));
            Assert.That(cancelled.Error, Is.EqualTo(SvgTextGeometryError.Cancelled));
            Assert.That(invalid.Error, Is.EqualTo(SvgTextGeometryError.InvalidArgument));
            Assert.That(unsupported.Error, Is.EqualTo(SvgTextGeometryError.UnsupportedOutline));
            Assert.That(badPoint.Error, Is.EqualTo(SvgTextGeometryError.InvalidArgument));
            Assert.That(nullPoint.Error, Is.EqualTo(SvgTextGeometryError.InvalidArgument));
        });
    }

    static SvgTextLayoutOptions LayoutOptions()
    {
        SvgTextLayoutOptions options = new SvgTextLayoutOptions();
        options.FontSize = 10;
        options.Ascent = 8;
        options.Descent = 2;
        options.XHeight = 5;
        return options;
    }

    static SvgTextLayoutResult Text(string text, SvgTextLayoutOptions options)
    {
        Dictionary<int, int> mappings = new Dictionary<int, int>();
        int index = 0;
        while (index < text.Length)
        {
            mappings[text[index]] = 1;
            index++;
        }
        return new SvgTextLayout().LayoutWithShapingAndOptions(SvgTextLayoutTests.Shape(text, mappings), options);
    }

    static SvgTextGeometryOptions GeometryOptions()
    {
        SvgFont font = SvgTrueTypeGlyphDecoderTests.CreateFont(false, Array.Empty<byte>(), SvgTrueTypeGlyphDecoderTests.BuildRepeatedRectangleGlyph());
        SvgTextGeometryOptions options = new SvgTextGeometryOptions();
        options.Fonts.Add(new SvgTextFontResource(0, font, 10));
        return options;
    }

    static SvgPathData Path(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static void AssertRect(SvgRect rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle, Is.Not.Null);
        Assert.That(rectangle.IsEmpty, Is.False);
        Assert.That(rectangle.X, Is.EqualTo(x).Within(0.000001));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(0.000001));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(0.000001));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(0.000001));
    }
}
