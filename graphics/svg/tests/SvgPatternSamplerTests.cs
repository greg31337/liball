namespace svg.Tests;

public class SvgPatternSamplerTests
{
    [Test]
    public void RepeatsTransformedTileInPatternCoordinates()
    {
        SvgSceneResource pattern = Pattern("repeat", 0, 0, 2, 1);
        pattern.PatternPaint.Transform = new SvgMatrix(2, 0, 0, 1, 10, 0);
        pattern.PatternPaint.ContentTransform = new SvgMatrix(2, 0, 0, 1, 10, 0);
        AddPath(pattern, "M0 0 H1 V1 H0 Z", Solid("red", 1, 0, 0, 1));

        SvgPaintSampleResult first = new SvgPaintSampler().SampleWithResourceAndPoint(pattern, Point(10.5, 0.5));
        SvgPaintSampleResult gap = new SvgPaintSampler().SampleWithResourceAndPoint(pattern, Point(12.5, 0.5));
        SvgPaintSampleResult repeated = new SvgPaintSampler().SampleWithResourceAndPoint(pattern, Point(14.5, 0.5));
        SvgPaintSampleResult negative = new SvgPaintSampler().SampleWithResourceAndPoint(pattern, Point(6.5, 0.5));

        AssertSample(first, 255, 0, 0, 255);
        AssertSample(gap, 0, 0, 0, 0);
        AssertSample(repeated, 255, 0, 0, 255);
        AssertSample(negative, 255, 0, 0, 255);
    }

    [Test]
    public void AppliesNormalizedViewBoxContentTransformAndOpacity()
    {
        SvgSceneResource pattern = Pattern("view-box", 0, 0, 4, 2);
        pattern.PatternPaint.ViewBox = new SvgViewBox(0, 0, 1, 1);
        pattern.PatternPaint.ContentTransform = new SvgMatrix(2, 0, 0, 2, 1, 0);
        SvgSceneNode path = AddPath(pattern, "M0 0 H1 V1 H0 Z", Solid("red", 1, 0, 0, 1));
        path.Opacity = 0.5;
        SvgPaintSampleOptions options = new SvgPaintSampleOptions();
        options.Opacity = 0.5;

        SvgPaintSampleResult left = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(0.5, 0.5), options);
        SvgPaintSampleResult center = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(1.5, 0.5), options);
        SvgPaintSampleResult right = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(3.5, 0.5), options);

        AssertSample(left, 0, 0, 0, 0);
        AssertSample(center, 64, 0, 0, 64);
        AssertSample(right, 0, 0, 0, 0);
    }

    [Test]
    public void UsesContextFillRetainedBySceneBuilder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><pattern id='p' width='50%' height='50%' patternContentUnits='objectBoundingBox'><rect width='.5' height='.5' fill='context-fill'/></pattern></defs><rect id='target' width='4' height='4' fill='url(#p)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult built = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode target = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneNode contentPath = target.Fill.PatternPaint.Content.Children[0];
        SvgPaintSampleOptions options = new SvgPaintSampleOptions();
        options.ContextFill = Solid("context", 0, 1, 0, 1);

        SvgPaintSampleResult sampled = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(target.Fill, Point(0.5, 0.5), options);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(contentPath.FillUsesContext, Is.True);
            Assert.That(contentPath.Fill, Is.Null);
            Assert.That(target.Fill.PatternPaint.TileBounds.Width, Is.EqualTo(0.5));
            Assert.That(target.Fill.PatternPaint.Transform.A, Is.EqualTo(4));
            Assert.That(target.Fill.PatternPaint.ContentTransform.A, Is.EqualTo(4));
        });
        AssertSample(sampled, 0, 255, 0, 255);
    }

    [Test]
    public void RendersContextStrokeInPaintOrder()
    {
        SvgSceneResource pattern = Pattern("context-stroke", 0, 0, 4, 4);
        SvgSceneNode path = AddPath(pattern, "M2 0 V4", null);
        path.StrokeUsesContext = true;
        path.Path.StrokeStyle.Width = 2;
        SvgPaintSampleOptions options = new SvgPaintSampleOptions();
        options.ContextStroke = Solid("context-blue", 0, 0, 1, 1);

        SvgPaintSampleResult stroke = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(1.5, 1.5), options);
        SvgPaintSampleResult gap = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(0.5, 1.5), options);

        AssertSample(stroke, 0, 0, 255, 255);
        AssertSample(gap, 0, 0, 0, 0);
    }

    [Test]
    public void SupportsNestedPatternPaintAndEnforcesDepth()
    {
        SvgSceneResource inner = Pattern("inner", 0, 0, 1, 1);
        AddPath(inner, "M0 0 H1 V1 H0 Z", Solid("blue", 0, 0, 1, 1));
        SvgSceneResource outer = Pattern("outer", 0, 0, 1, 1);
        AddPath(outer, "M0 0 H1 V1 H0 Z", inner);
        SvgPaintSampleOptions limited = new SvgPaintSampleOptions();
        limited.MaximumPatternDepth = 1;

        SvgPaintSampleResult sampled = new SvgPaintSampler().SampleWithResourceAndPoint(outer, Point(0.5, 0.5));
        SvgPaintSampleResult depth = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(outer, Point(0.5, 0.5), limited);

        AssertSample(sampled, 0, 0, 255, 255);
        AssertFailure(depth, SvgPaintSampleError.PatternDepthLimitExceeded);
    }

    [Test]
    public void RejectsPatternAndContentCycles()
    {
        SvgSceneResource paintCycle = Pattern("paint-cycle", 0, 0, 1, 1);
        AddPath(paintCycle, "M0 0 H1 V1 H0 Z", paintCycle);
        SvgSceneResource nodeCycle = Pattern("node-cycle", 0, 0, 1, 1);
        SvgSceneNode group = nodeCycle.PatternPaint.Content;
        group.Children.Add(group);

        SvgPaintSampleResult paint = new SvgPaintSampler().SampleWithResourceAndPoint(paintCycle, Point(0.5, 0.5));
        SvgPaintSampleResult node = new SvgPaintSampler().SampleWithResourceAndPoint(nodeCycle, Point(0.5, 0.5));

        AssertFailure(paint, SvgPaintSampleError.PatternRecursionDetected);
        AssertFailure(node, SvgPaintSampleError.PatternRecursionDetected);
    }

    [Test]
    public void EnforcesTileAllocationAndContentNodeLimits()
    {
        SvgSceneResource pattern = Pattern("limited", 0, 0, 2, 2);
        AddPath(pattern, "M0 0 H2 V2 H0 Z", Solid("red", 1, 0, 0, 1));
        SvgPaintSampleOptions tileLimit = new SvgPaintSampleOptions();
        tileLimit.RasterLimits.MaxWidth = 1;
        SvgPaintSampleOptions nodeLimit = new SvgPaintSampleOptions();
        nodeLimit.MaximumPatternNodes = 1;

        SvgPaintSampleResult tile = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(0.5, 0.5), tileLimit);
        SvgPaintSampleResult nodes = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(pattern, Point(0.5, 0.5), nodeLimit);

        AssertFailure(tile, SvgPaintSampleError.PatternTileLimitExceeded);
        AssertFailure(nodes, SvgPaintSampleError.PatternNodeLimitExceeded);
    }

    static SvgSceneResource Pattern(string key, double x, double y, double width, double height)
    {
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.PatternPaint);
        resource.PatternPaint.TileBounds = SvgRect.Create(x, y, width, height, false);
        resource.PatternPaint.Content = new SvgSceneNode(SvgSceneNodeKind.Group);
        return resource;
    }

    static SvgSceneNode AddPath(SvgSceneResource pattern, string pathData, SvgSceneResource fill)
    {
        SvgSceneNode path = new SvgSceneNode(SvgSceneNodeKind.Path);
        path.Path.Path = Parse(pathData);
        path.Fill = fill;
        Assert.That(pattern.PatternPaint.Content.AddChild(path), Is.True);
        return path;
    }

    static SvgSceneResource Solid(string key, double red, double green, double blue, double alpha)
    {
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.SolidPaint);
        resource.SolidPaint.Color = new SvgColor(SvgColorSpace.Srgb, red, green, blue, alpha);
        resource.SolidPaint.Opacity = alpha;
        return resource;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 256, 256);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertSample(SvgPaintSampleResult result, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPaintSampleError.None));
        Assert.That(result.HasPaint, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Pixel.Red, Is.EqualTo(red));
            Assert.That(result.Pixel.Green, Is.EqualTo(green));
            Assert.That(result.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertFailure(SvgPaintSampleResult result, SvgPaintSampleError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.HasPaint, Is.False);
    }
}
