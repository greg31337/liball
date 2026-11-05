namespace svg.Tests;

public class SvgTextPathLayoutTests
{
    [Test]
    public void ProjectsHorizontalTextAtStartOffsetWithNormalDisplacement()
    {
        SvgTextLayoutOptions textOptions = new SvgTextLayoutOptions();
        textOptions.InitialY = 5;
        SvgTextLayoutResult text = Text("ABC", textOptions);
        SvgTextPathOptions options = new SvgTextPathOptions();
        options.StartOffset = 10;

        SvgTextPathResult result = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L100 0"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceKind, Is.EqualTo(SvgTextPathSourceKind.ReferencedPath));
            Assert.That(result.ActualPathLength, Is.EqualTo(100));
            Assert.That(result.CalibratedPathLength, Is.EqualTo(100));
            Assert.That(result.Closed, Is.False);
            Assert.That(result.Glyphs.Select(glyph => glyph.Point.X), Is.EqualTo(new[] { 10.0, 16.0, 22.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Point.Y), Is.All.EqualTo(5));
            Assert.That(result.Glyphs.Select(glyph => glyph.Rotation), Is.All.EqualTo(0));
            Assert.That(result.Glyphs.Select(glyph => glyph.Visible), Is.All.True);
        });
    }

    [Test]
    public void AlignsVerticalInlineAxesToPathTangents()
    {
        SvgShapingResult shaping = SvgTextLayoutTests.Shape("AB", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 });
        shaping.Glyphs[0].YAdvance = 6;
        shaping.Glyphs[1].YAdvance = 6;
        SvgTextLayoutOptions textOptions = new SvgTextLayoutOptions();
        textOptions.InitialX = 2;
        textOptions.WritingMode = SvgWritingMode.VerticalRightToLeft;
        textOptions.Rotate.Add(10);
        SvgTextLayoutResult text = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, textOptions);

        SvgTextPathResult result = new SvgTextPathLayout().LayoutWithTextAndPath(text, Path("M0 0 L0 100"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Glyphs.Select(glyph => glyph.Point.X), Is.All.EqualTo(-2));
            Assert.That(result.Glyphs.Select(glyph => glyph.Point.Y), Is.EqualTo(new[] { 0.0, 6.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Rotation), Is.EqualTo(new[] { 10.0, 10.0 }).Within(0.0001));
            Assert.That(result.Glyphs.Select(glyph => glyph.Tangent.X), Is.EqualTo(new[] { 0.0, 0.0 }).Within(0.0001));
            Assert.That(result.Glyphs.Select(glyph => glyph.Tangent.Y), Is.EqualTo(new[] { 1.0, 1.0 }).Within(0.0001));
        });
    }

    [Test]
    public void CalibratesPercentageStartOffsetThroughPathLength()
    {
        SvgTextLayoutResult text = Text("A", new SvgTextLayoutOptions());
        SvgTextPathOptions options = new SvgTextPathOptions();
        options.HasPathLength = true;
        options.PathLength = 50;
        options.StartOffset = 50;
        options.StartOffsetIsPercentage = true;

        SvgTextPathResult result = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L100 0"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResolvedStartOffset, Is.EqualTo(25));
            Assert.That(result.PathLengthScale, Is.EqualTo(2));
            Assert.That(result.Glyphs[0].PathDistance, Is.EqualTo(25));
            Assert.That(result.Glyphs[0].ActualPathDistance, Is.EqualTo(50));
            Assert.That(result.Glyphs[0].Point.X, Is.EqualTo(50));
        });
    }

    [Test]
    public void AppliesStretchMethodOrAutomaticSpacingToAvailablePath()
    {
        SvgTextLayoutResult text = Text("ABC", new SvgTextLayoutOptions());
        SvgPathData path = Path("M0 0 L36 0");
        SvgTextPathOptions stretchOptions = new SvgTextPathOptions();
        stretchOptions.Method = SvgTextPathMethod.Stretch;
        SvgTextPathOptions spacingOptions = new SvgTextPathOptions();
        spacingOptions.Spacing = SvgTextPathSpacing.Auto;

        SvgTextPathResult stretch = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, path, stretchOptions);
        SvgTextPathResult spacing = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, path, spacingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(stretch.Success, Is.True);
            Assert.That(stretch.MethodScale, Is.EqualTo(2));
            Assert.That(stretch.Glyphs.Select(glyph => glyph.Point.X), Is.EqualTo(new[] { 0.0, 12.0, 24.0 }));
            Assert.That(stretch.Glyphs.Select(glyph => glyph.MethodScale), Is.All.EqualTo(2));
            Assert.That(spacing.Success, Is.True);
            Assert.That(spacing.SpacingAdjustment, Is.EqualTo(9));
            Assert.That(spacing.Glyphs.Select(glyph => glyph.Point.X), Is.EqualTo(new[] { 0.0, 15.0, 30.0 }));
        });
    }

    [Test]
    public void ReversesTheRightSideAndWrapsClosedPaths()
    {
        SvgTextLayoutResult text = Text("A", new SvgTextLayoutOptions());
        SvgTextPathOptions rightOptions = new SvgTextPathOptions();
        rightOptions.Side = SvgTextPathSide.Right;
        SvgTextPathOptions closedOptions = new SvgTextPathOptions();
        closedOptions.StartOffset = 45;

        SvgTextPathResult right = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L20 0"), rightOptions);
        SvgTextPathResult closed = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L10 0 L10 10 L0 10 Z"), closedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(right.Success, Is.True);
            Assert.That(right.Glyphs[0].Point.X, Is.EqualTo(20));
            Assert.That(right.Glyphs[0].Tangent.X, Is.EqualTo(-1));
            Assert.That(System.Math.Abs(right.Glyphs[0].Rotation), Is.EqualTo(180).Within(0.0001));
            Assert.That(closed.Success, Is.True);
            Assert.That(closed.Closed, Is.True);
            Assert.That(closed.CalibratedPathLength, Is.EqualTo(40));
            Assert.That(closed.Glyphs[0].Wrapped, Is.True);
            Assert.That(closed.Glyphs[0].PathDistance, Is.EqualTo(5));
            Assert.That(closed.Glyphs[0].Point.X, Is.EqualTo(5));
            Assert.That(closed.Glyphs[0].Point.Y, Is.Zero);
        });
    }

    [Test]
    public void HidesOrExtrapolatesGlyphsBeyondOpenPathEndpoints()
    {
        SvgTextLayoutResult text = Text("ABC", new SvgTextLayoutOptions());
        SvgPathData path = Path("M0 0 L10 0");
        SvgTextPathOptions hiddenOptions = new SvgTextPathOptions();
        SvgTextPathOptions visibleOptions = new SvgTextPathOptions();
        visibleOptions.Overflow = SvgTextPathOverflow.Visible;

        SvgTextPathResult hidden = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, path, hiddenOptions);
        SvgTextPathResult visible = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, path, visibleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(hidden.Success, Is.True);
            Assert.That(hidden.Glyphs.Select(glyph => glyph.Visible), Is.EqualTo(new[] { true, true, false }));
            Assert.That(hidden.OverflowedGlyphs, Is.EqualTo(1));
            Assert.That(hidden.HiddenGlyphs, Is.EqualTo(1));
            Assert.That(visible.Success, Is.True);
            Assert.That(visible.Glyphs.Select(glyph => glyph.Visible), Is.All.True);
            Assert.That(visible.OverflowedGlyphs, Is.EqualTo(1));
            Assert.That(visible.HiddenGlyphs, Is.Zero);
            Assert.That(visible.Glyphs[2].Point.X, Is.EqualTo(12));
        });
    }

    [Test]
    public void SupportsInlinePathDataAndReferencedShapeCalibration()
    {
        SvgTextLayoutResult text = Text("A", new SvgTextLayoutOptions());
        SvgTextPathResult inline = new SvgTextPathLayout().LayoutInline(text, "M0 0 L20 0", new SvgTextPathOptions());
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg xmlns='http://www.w3.org/2000/svg'><line x1='0' y1='0' x2='20' y2='0' pathLength='10'/></svg>", new SvgLoadOptions());
        SvgShapeResult shape = new SvgShapeResolver().ResolveWithNode(loaded.Document.DocumentElement.Children[0]);
        SvgTextPathOptions shapeOptions = new SvgTextPathOptions();
        shapeOptions.StartOffset = 5;
        SvgTextPathResult referenced = new SvgTextPathLayout().LayoutShape(text, shape, shapeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(inline.Success, Is.True);
            Assert.That(inline.SourceKind, Is.EqualTo(SvgTextPathSourceKind.InlinePathData));
            Assert.That(inline.Path, Is.Not.Null);
            Assert.That(loaded.Success, Is.True);
            Assert.That(shape.Success, Is.True);
            Assert.That(referenced.Success, Is.True);
            Assert.That(referenced.SourceKind, Is.EqualTo(SvgTextPathSourceKind.ReferencedShape));
            Assert.That(referenced.CalibratedPathLength, Is.EqualTo(10));
            Assert.That(referenced.PathLengthScale, Is.EqualTo(2));
            Assert.That(referenced.Glyphs[0].Point.X, Is.EqualTo(10));
        });
    }

    [Test]
    public void RejectsMalformedEmptyBoundedAndCancelledInputs()
    {
        SvgTextLayoutResult text = Text("ABC", new SvgTextLayoutOptions());
        SvgTextPathOptions glyphLimit = new SvgTextPathOptions();
        glyphLimit.MaximumGlyphs = 2;
        SvgTextPathOptions queryLimit = new SvgTextPathOptions();
        queryLimit.MaximumPathQueries = 5;
        SvgTextPathOptions flattenLimit = new SvgTextPathOptions();
        flattenLimit.MaximumFlattenedSegments = 1;
        SvgTextPathOptions invalidOptions = new SvgTextPathOptions();
        invalidOptions.StartOffset = double.NaN;
        SvgTextPathOptions cancelled = new SvgTextPathOptions();
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;

        SvgTextPathResult malformed = new SvgTextPathLayout().LayoutInline(text, "M", new SvgTextPathOptions());
        SvgTextPathResult empty = new SvgTextPathLayout().LayoutWithTextAndPath(text, Path("M0 0 L0 0"));
        SvgTextPathResult tooManyGlyphs = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L20 0"), glyphLimit);
        SvgTextPathResult tooManyQueries = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L20 0"), queryLimit);
        SvgTextPathResult tooManySegments = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 Q10 20 20 0"), flattenLimit);
        SvgTextPathResult invalid = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L20 0"), invalidOptions);
        SvgTextPathResult stopped = new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, Path("M0 0 L20 0"), cancelled);
        SvgTextPathResult nullPath = new SvgTextPathLayout().LayoutWithTextAndPath(text, null);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(SvgTextPathError.InvalidPath));
            Assert.That(empty.Error, Is.EqualTo(SvgTextPathError.InvalidPath));
            Assert.That(tooManyGlyphs.Error, Is.EqualTo(SvgTextPathError.LimitExceeded));
            Assert.That(tooManyQueries.Error, Is.EqualTo(SvgTextPathError.LimitExceeded));
            Assert.That(tooManySegments.Error, Is.EqualTo(SvgTextPathError.LimitExceeded));
            Assert.That(invalid.Error, Is.EqualTo(SvgTextPathError.InvalidArgument));
            Assert.That(stopped.Error, Is.EqualTo(SvgTextPathError.Cancelled));
            Assert.That(nullPath.Error, Is.EqualTo(SvgTextPathError.InvalidArgument));
        });
    }

    static SvgTextLayoutResult Text(string value, SvgTextLayoutOptions options)
    {
        Dictionary<int, int> mappings = new Dictionary<int, int>();
        int index = 0;
        while (index < value.Length)
        {
            mappings[value[index]] = index + 1;
            index++;
        }
        return new SvgTextLayout().LayoutWithShapingAndOptions(SvgTextLayoutTests.Shape(value, mappings), options);
    }

    static SvgPathData Path(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }
}
