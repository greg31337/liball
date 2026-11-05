namespace chart.Tests;

[TestFixture]
public class ChartRasterCanvasTests
{
    [Test]
    public void Clear_FillsEntireBitmapIgnoringClip()
    {
        RawBitmap bitmap = new RawBitmap(3, 2, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(1, 1, 1, 1);
        canvas.Clear(new ChartColor(5, 6, 7, 8));

        AssertColor(bitmap.GetPixel(0, 0), 5, 6, 7, 8);
        AssertColor(bitmap.GetPixel(2, 1), 5, 6, 7, 8);
    }

    [Test]
    public void ClearClip_FillsOnlyClippedPixels()
    {
        RawBitmap bitmap = new RawBitmap(3, 3, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.Clear(ChartColor.Black());
        canvas.SetClip(1, 1, 1, 1);
        canvas.ClearClip(ChartColor.White());

        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 255);
        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 2), 0, 0, 0, 255);
    }

    [Test]
    public void SetClip_ClampsToBitmapAndRejectsEmptyRegions()
    {
        RawBitmap bitmap = new RawBitmap(5, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(-2, 1, 5, 10);

        Assert.That(canvas.ClipX, Is.EqualTo(0));
        Assert.That(canvas.ClipY, Is.EqualTo(1));
        Assert.That(canvas.ClipWidth, Is.EqualTo(3));
        Assert.That(canvas.ClipHeight, Is.EqualTo(3));
        Assert.That(canvas.ContainsClipPixel(2, 3), Is.True);
        Assert.That(canvas.ContainsClipPixel(3, 3), Is.False);

        canvas.SetClip(10, 10, 1, 1);

        Assert.That(canvas.ClipWidth, Is.EqualTo(0));
        Assert.That(canvas.ClipHeight, Is.EqualTo(0));

        canvas.SetClip(int.MaxValue - 1, 0, 10, 1);

        Assert.That(canvas.ClipWidth, Is.EqualTo(0));
        Assert.That(canvas.ClipHeight, Is.EqualTo(0));
    }

    [Test]
    public void SetClipRect_UsesFloorCeilingPixelCoverage()
    {
        RawBitmap bitmap = new RawBitmap(10, 10, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClipRect(new ChartRect(1.2, 2.8, 3.1, 4.1));

        Assert.That(canvas.ClipX, Is.EqualTo(1));
        Assert.That(canvas.ClipY, Is.EqualTo(2));
        Assert.That(canvas.ClipWidth, Is.EqualTo(4));
        Assert.That(canvas.ClipHeight, Is.EqualTo(5));

        canvas.SetClipRect(new ChartRect(double.MaxValue, double.MaxValue, 1.0, 1.0));

        Assert.That(canvas.ClipWidth, Is.EqualTo(0));
        Assert.That(canvas.ClipHeight, Is.EqualTo(0));
    }

    [Test]
    public void WritePixel_RespectsClipAndBitmapBounds()
    {
        RawBitmap bitmap = new RawBitmap(3, 3, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartColor red = new ChartColor(255, 0, 0, 255);

        canvas.SetClip(1, 1, 1, 1);

        Assert.That(canvas.WritePixel(1, 1, red), Is.True);
        Assert.That(canvas.WritePixel(0, 0, red), Is.False);
        Assert.That(canvas.WritePixel(3, 3, red), Is.False);
        AssertColor(bitmap.GetPixel(1, 1), 255, 0, 0, 255);
        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
    }

    [Test]
    public void DrawPixel_SourceModeReplacesDestination()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.WritePixel(0, 0, new ChartColor(0, 0, 255, 255));
        canvas.DrawPixel(0, 0, new ChartColor(255, 0, 0, 128), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(0, 0), 255, 0, 0, 128);
    }

    [Test]
    public void BlendPixel_AppliesSourceOverAlpha()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.WritePixel(0, 0, new ChartColor(0, 0, 255, 255));
        canvas.BlendPixel(0, 0, new ChartColor(255, 0, 0, 128));

        AssertColor(bitmap.GetPixel(0, 0), 128, 0, 127, 255);
    }

    [Test]
    public void SourceOver_HandlesTransparentAndOpaqueFastPaths()
    {
        ChartColor destination = new ChartColor(0, 0, 255, 128);
        ChartColor transparentSource = ChartRasterCanvas.SourceOver(new ChartColor(255, 0, 0, 0), destination);
        ChartColor opaqueSource = ChartRasterCanvas.SourceOver(new ChartColor(255, 0, 0, 255), destination);
        ChartColor overTransparent = ChartRasterCanvas.SourceOver(new ChartColor(255, 0, 0, 128), ChartColor.Transparent());

        AssertColor(transparentSource, 0, 0, 255, 128);
        AssertColor(opaqueSource, 255, 0, 0, 255);
        AssertColor(overTransparent, 255, 0, 0, 128);
    }

    [Test]
    public void BlendPixel_RespectsClipAndBitmapBounds()
    {
        RawBitmap bitmap = new RawBitmap(2, 2, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(0, 0, 1, 1);

        Assert.That(canvas.BlendPixel(0, 0, ChartColor.White()), Is.True);
        Assert.That(canvas.BlendPixel(1, 1, ChartColor.White()), Is.False);
        Assert.That(canvas.BlendPixel(-1, 0, ChartColor.White()), Is.False);
        AssertColor(bitmap.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 1), 0, 0, 0, 0);
    }

    [Test]
    public void DrawLineAliased_DrawsInclusiveHorizontalAndDiagonalLines()
    {
        RawBitmap horizontal = new RawBitmap(5, 3, ChartPixelFormat.Rgba32);
        ChartRasterCanvas horizontalCanvas = new ChartRasterCanvas(horizontal);
        RawBitmap diagonal = new RawBitmap(4, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas diagonalCanvas = new ChartRasterCanvas(diagonal);

        horizontalCanvas.DrawLineAliased(0, 1, 3, 1, ChartColor.White(), ChartBlendMode.Source);
        diagonalCanvas.DrawLineAliased(0, 0, 3, 3, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(horizontal.GetPixel(0, 1), 255, 255, 255, 255);
        AssertColor(horizontal.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(horizontal.GetPixel(2, 1), 255, 255, 255, 255);
        AssertColor(horizontal.GetPixel(3, 1), 255, 255, 255, 255);
        AssertColor(horizontal.GetPixel(4, 1), 0, 0, 0, 0);
        AssertColor(diagonal.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(diagonal.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(diagonal.GetPixel(2, 2), 255, 255, 255, 255);
        AssertColor(diagonal.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(diagonal.GetPixel(1, 2), 0, 0, 0, 0);
    }

    [Test]
    public void DrawLineAliased_RespectsClip()
    {
        RawBitmap bitmap = new RawBitmap(5, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(1, 0, 2, 1);
        canvas.DrawLineAliased(0, 0, 4, 0, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(1, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 0), 0, 0, 0, 0);
    }

    [Test]
    public void DrawLineAntialiased_DrawsCoverageForShallowLine()
    {
        RawBitmap bitmap = new RawBitmap(5, 3, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.DrawLineAntialiased(0.0, 0.0, 4.0, 2.0, ChartColor.White(), ChartBlendMode.SourceOver);

        AssertColor(bitmap.GetPixel(0, 0), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(1, 0), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(2, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 1), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(3, 2), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(4, 2), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(4, 0), 0, 0, 0, 0);
    }

    [Test]
    public void DrawLineAntialiased_HandlesVerticalLines()
    {
        RawBitmap bitmap = new RawBitmap(5, 5, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.DrawLineAntialiased(2.0, 0.0, 2.0, 3.0, ChartColor.White(), ChartBlendMode.SourceOver);

        AssertColor(bitmap.GetPixel(2, 0), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(2, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 2), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 3), 255, 255, 255, 128);
        AssertColor(bitmap.GetPixel(1, 1), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(3, 1), 0, 0, 0, 0);
    }

    [Test]
    public void DrawLine_DispatchesByRenderQuality()
    {
        RawBitmap aliased = new RawBitmap(3, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas aliasedCanvas = new ChartRasterCanvas(aliased);
        RawBitmap antialiased = new RawBitmap(3, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas antialiasedCanvas = new ChartRasterCanvas(antialiased);

        aliasedCanvas.DrawLine(0.0, 0.0, 2.0, 0.0, ChartColor.White(), ChartRenderQuality.Aliased, ChartBlendMode.Source);
        antialiasedCanvas.DrawLine(0.0, 0.0, 2.0, 0.0, ChartColor.White(), ChartRenderQuality.Antialiased, ChartBlendMode.Source);

        AssertColor(aliased.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(aliased.GetPixel(1, 0), 255, 255, 255, 255);
        AssertColor(aliased.GetPixel(2, 0), 255, 255, 255, 255);
        AssertColor(antialiased.GetPixel(0, 0), 255, 255, 255, 128);
        AssertColor(antialiased.GetPixel(1, 0), 255, 255, 255, 255);
        AssertColor(antialiased.GetPixel(2, 0), 255, 255, 255, 128);
    }

    [Test]
    public void DrawPolyline_DrawsThickStroke()
    {
        RawBitmap bitmap = new RawBitmap(10, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(1.5, 3.5),
            new ChartPoint(8.5, 3.5)
        };

        canvas.DrawPolyline(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 2), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(8, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 1), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(4, 5), 0, 0, 0, 0);
    }

    [Test]
    public void DrawPolyline_AppliesDashPatternAlongPath()
    {
        RawBitmap bitmap = new RawBitmap(9, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.5, 0.5),
            new ChartPoint(8.5, 0.5)
        };

        canvas.DrawPolylineWithStyle(points, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, new double[] { 2.0, 2.0 }, 4.0, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(3, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(4, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(6, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(7, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(8, 0), 255, 255, 255, 255);
    }

    [Test]
    public void DrawPolyline_DistinguishesButtAndSquareCaps()
    {
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(2.5, 3.5),
            new ChartPoint(6.5, 3.5)
        };

        RawBitmap butt = new RawBitmap(8, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas buttCanvas = new ChartRasterCanvas(butt);
        RawBitmap square = new RawBitmap(8, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas squareCanvas = new ChartRasterCanvas(square);

        buttCanvas.DrawPolyline(points, 2.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source);
        squareCanvas.DrawPolyline(points, 2.0, ChartColor.White(), ChartLineCap.Square, ChartLineJoin.Bevel, ChartBlendMode.Source);

        AssertColor(butt.GetPixel(1, 2), 0, 0, 0, 0);
        AssertColor(square.GetPixel(1, 2), 255, 255, 255, 255);
        AssertColor(square.GetPixel(7, 4), 255, 255, 255, 255);
    }

    [Test]
    public void DrawPolyline_RoundJoinAddsCornerCoverage()
    {
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(2.5, 2.5),
            new ChartPoint(5.5, 2.5),
            new ChartPoint(5.5, 5.5)
        };

        RawBitmap bevel = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas bevelCanvas = new ChartRasterCanvas(bevel);
        RawBitmap round = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas roundCanvas = new ChartRasterCanvas(round);

        bevelCanvas.DrawPolyline(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source);
        roundCanvas.DrawPolyline(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Round, ChartBlendMode.Source);

        AssertColor(bevel.GetPixel(6, 1), 0, 0, 0, 0);
        AssertColor(round.GetPixel(6, 1), 255, 255, 255, 255);
    }

    [Test]
    public void DrawPolyline_MiterJoinHonorsMiterLimit()
    {
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(2.5, 2.5),
            new ChartPoint(5.5, 2.5),
            new ChartPoint(5.5, 5.5)
        };

        RawBitmap lowLimit = new RawBitmap(9, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas lowLimitCanvas = new ChartRasterCanvas(lowLimit);
        RawBitmap highLimit = new RawBitmap(9, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas highLimitCanvas = new ChartRasterCanvas(highLimit);

        lowLimitCanvas.DrawPolylineWithStyle(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Miter, new double[0], 1.0, ChartBlendMode.Source);
        highLimitCanvas.DrawPolylineWithStyle(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Miter, new double[0], 2.0, ChartBlendMode.Source);

        Assert.That(ChartRasterCanvas.MiterScale(points[0], points[1], points[2]), Is.EqualTo(System.Math.Sqrt(2.0)).Within(0.0000001));
        Assert.That(ChartRasterCanvas.IsMiterWithinLimit(points[0], points[1], points[2], 1.0), Is.False);
        Assert.That(ChartRasterCanvas.IsMiterWithinLimit(points[0], points[1], points[2], 2.0), Is.True);
        AssertColor(lowLimit.GetPixel(7, 2), 0, 0, 0, 0);
        AssertColor(highLimit.GetPixel(7, 2), 255, 255, 255, 255);
    }

    [Test]
    public void DrawPolyline_RespectsClip()
    {
        RawBitmap bitmap = new RawBitmap(7, 3, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.5, 1.5),
            new ChartPoint(6.5, 1.5)
        };

        canvas.SetClip(2, 0, 2, 3);
        canvas.DrawPolyline(points, 3.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 1), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(2, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 1), 0, 0, 0, 0);
    }

    [Test]
    public void DrawPolyline_ValidatesInputs()
    {
        RawBitmap bitmap = new RawBitmap(4, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(0.5, 0.5),
            new ChartPoint(2.5, 0.5)
        };

        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawPolyline(null!, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentException>(() => canvas.DrawPolyline(new ChartPoint[] { new ChartPoint(0.0, 0.0) }, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.DrawPolyline(points, 0.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawPolyline(points, 1.0, null!, ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.DrawPolylineWithStyle(points, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, new double[] { 1.0, 0.0 }, 4.0, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.DrawPolylineWithStyle(points, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, new double[0], 0.0, ChartBlendMode.Source));
    }

    [Test]
    public void FillRectangle_FillsInteriorAndRespectsClip()
    {
        RawBitmap bitmap = new RawBitmap(6, 5, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(1, 1, 4, 3);
        canvas.FillRectangle(new ChartRect(0.0, 0.0, 4.0, 4.0), ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 3), 0, 0, 0, 0);
    }

    [Test]
    public void StrokeRectangle_DrawsBorderWithoutFillingCenter()
    {
        RawBitmap bitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.StrokeRectangle(new ChartRect(1.5, 1.5, 4.0, 4.0), 1.0, ChartColor.White(), ChartLineJoin.Miter, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 3), 0, 0, 0, 0);
    }

    [Test]
    public void FillPolygon_FillsTriangleUsingRequestedFillRule()
    {
        RawBitmap bitmap = new RawBitmap(7, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] triangle = new ChartPoint[]
        {
            new ChartPoint(1.0, 1.0),
            new ChartPoint(6.0, 1.0),
            new ChartPoint(3.5, 6.0)
        };

        canvas.FillPolygon(triangle, ChartColor.White(), ChartFillRule.EvenOdd, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(3, 2), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
        Assert.That(ChartRasterCanvas.PolygonContainsPoint(triangle, 3.5, 2.5, ChartFillRule.Nonzero), Is.True);
    }

    [Test]
    public void StrokePolygon_DrawsClosedOutline()
    {
        RawBitmap bitmap = new RawBitmap(7, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] square = new ChartPoint[]
        {
            new ChartPoint(1.5, 1.5),
            new ChartPoint(5.5, 1.5),
            new ChartPoint(5.5, 5.5),
            new ChartPoint(1.5, 5.5)
        };

        canvas.StrokePolygon(square, 1.0, ChartColor.White(), ChartLineJoin.Miter, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 3), 0, 0, 0, 0);
    }

    [Test]
    public void FillCircleAndEllipse_FillExpectedPixels()
    {
        RawBitmap circle = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas circleCanvas = new ChartRasterCanvas(circle);
        RawBitmap ellipse = new RawBitmap(9, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas ellipseCanvas = new ChartRasterCanvas(ellipse);

        circleCanvas.FillCircle(3.5, 3.5, 2.0, ChartColor.White(), ChartBlendMode.Source);
        ellipseCanvas.FillEllipse(4.0, 3.0, 3.0, 1.5, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(circle.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(circle.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(circle.GetPixel(0, 0), 0, 0, 0, 0);
        AssertColor(ellipse.GetPixel(4, 3), 255, 255, 255, 255);
        AssertColor(ellipse.GetPixel(1, 3), 255, 255, 255, 255);
        AssertColor(ellipse.GetPixel(4, 1), 0, 0, 0, 0);
    }

    [Test]
    public void StrokeCircleAndEllipse_DrawRings()
    {
        RawBitmap circle = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas circleCanvas = new ChartRasterCanvas(circle);
        RawBitmap ellipse = new RawBitmap(10, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas ellipseCanvas = new ChartRasterCanvas(ellipse);

        circleCanvas.StrokeCircle(3.5, 3.5, 2.0, 1.0, ChartColor.White(), ChartBlendMode.Source);
        ellipseCanvas.StrokeEllipse(5.0, 4.0, 3.0, 2.0, 1.0, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(circle.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(circle.GetPixel(3, 3), 0, 0, 0, 0);
        AssertColor(ellipse.GetPixel(7, 4), 255, 255, 255, 255);
        AssertColor(ellipse.GetPixel(5, 4), 0, 0, 0, 0);
    }

    [Test]
    public void StrokeArc_DrawsOnlyRequestedAngularSweep()
    {
        RawBitmap bitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.StrokeArc(3.5, 3.5, 2.0, 2.0, 0.0, System.Math.PI * 0.5, 1.0, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 3), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(3, 1), 0, 0, 0, 0);
    }

    [Test]
    public void FillWedge_FillsEllipseSector()
    {
        RawBitmap bitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.FillWedge(3.5, 3.5, 2.0, 2.0, 0.0, System.Math.PI * 0.5, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(4, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 4), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(4, 2), 0, 0, 0, 0);
    }

    [Test]
    public void StrokeWedge_DrawsArcAndRadialEdges()
    {
        RawBitmap bitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.StrokeWedge(3.5, 3.5, 2.0, 2.0, 0.0, System.Math.PI * 0.5, 1.0, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 4), 0, 0, 0, 0);
    }

    [Test]
    public void ArcAndWedgeRendering_RespectClipAndSourceOverBlend()
    {
        RawBitmap arcBitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas arcCanvas = new ChartRasterCanvas(arcBitmap);
        RawBitmap wedgeBitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas wedgeCanvas = new ChartRasterCanvas(wedgeBitmap);
        ChartColor blue = new ChartColor(0, 0, 255, 255);
        ChartColor red = new ChartColor(255, 0, 0, 128);

        arcCanvas.Clear(blue);
        arcCanvas.SetClip(3, 3, 3, 3);
        arcCanvas.StrokeArc(3.5, 3.5, 2.0, 2.0, 0.0, System.Math.PI * 0.5, 1.0, red, ChartBlendMode.SourceOver);
        wedgeCanvas.Clear(blue);
        wedgeCanvas.SetClip(3, 3, 3, 3);
        wedgeCanvas.FillWedge(3.5, 3.5, 2.0, 2.0, 0.0, System.Math.PI * 0.5, red, ChartBlendMode.SourceOver);

        AssertColor(arcBitmap.GetPixel(5, 3), 128, 0, 127, 255);
        AssertColor(arcBitmap.GetPixel(3, 5), 128, 0, 127, 255);
        AssertColor(arcBitmap.GetPixel(1, 3), 0, 0, 255, 255);
        AssertColor(wedgeBitmap.GetPixel(4, 4), 128, 0, 127, 255);
        AssertColor(wedgeBitmap.GetPixel(2, 4), 0, 0, 255, 255);
    }

    [Test]
    public void PrimitiveMethods_ValidateInputs()
    {
        RawBitmap bitmap = new RawBitmap(4, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPoint[] triangle = new ChartPoint[]
        {
            new ChartPoint(0.0, 0.0),
            new ChartPoint(1.0, 0.0),
            new ChartPoint(0.0, 1.0)
        };

        Assert.Throws<System.ArgumentNullException>(() => canvas.FillRectangle(null!, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentException>(() => canvas.FillPolygon(new ChartPoint[] { new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0) }, ChartColor.White(), ChartFillRule.EvenOdd, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.FillPolygon(triangle, null!, ChartFillRule.EvenOdd, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.FillCircle(1.0, 1.0, 0.0, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.StrokeEllipse(1.0, 1.0, 1.0, 1.0, 0.0, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.FillWedge(1.0, 1.0, 1.0, 1.0, 0.0, 1.0, null!, ChartBlendMode.Source));
        Assert.DoesNotThrow(() => canvas.DrawText("A", 0, 0, ChartBitmapFont.FiveBySeven(), int.MaxValue, ChartColor.White(), ChartBlendMode.Source));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
