namespace chart.Tests;

[TestFixture]
public class ChartRasterCompletionTests
{
    [TestCase(ChartBlendMode.Normal, 200, 100, 50)]
    [TestCase(ChartBlendMode.SourceOver, 200, 100, 50)]
    [TestCase(ChartBlendMode.Source, 200, 100, 50)]
    [TestCase(ChartBlendMode.Multiply, 78, 59, 39)]
    [TestCase(ChartBlendMode.Screen, 222, 191, 211)]
    [TestCase(ChartBlendMode.Darken, 100, 100, 50)]
    [TestCase(ChartBlendMode.Lighten, 200, 150, 200)]
    public void DrawPixel_ImplementsEveryDeclaredBlendMode(ChartBlendMode mode, int red, int green, int blue)
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        bitmap.SetPixel(0, 0, new ChartColor(100, 150, 200, 255));
        canvas.DrawPixel(0, 0, new ChartColor(200, 100, 50, 255), mode);

        AssertColor(bitmap.GetPixel(0, 0), red, green, blue, 255);
    }

    [Test]
    public void SceneRaster_PropagatesDashPatternAndMiterSettingsToStrokes()
    {
        ChartRenderScene scene = new ChartRenderScene(9, 3);
        ChartRenderStyle stroke = new ChartRenderStyle();

        stroke.SetNoFill();
        stroke.SetStroke(ChartColor.White(), 1.0);
        stroke.SetDashArray(new double[] { 2.0, 2.0 });
        stroke.SetMiterLimit(2.0);
        stroke.SetQuality(ChartRenderQuality.Aliased);
        stroke.SetBlendMode(ChartBlendMode.Source);
        scene.AddLine("dashed", 0.5, 1.5, 8.5, 1.5, stroke);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(0, 1).A, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(1, 1).A, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(2, 1).A, Is.EqualTo(0));
        Assert.That(bitmap.GetPixel(3, 1).A, Is.EqualTo(0));
        Assert.That(bitmap.GetPixel(4, 1).A, Is.EqualTo(255));
    }

    [Test]
    public void DashPattern_RepeatsOddLengthArraysWithAlternatingParity()
    {
        double[] pattern = new double[] { 2.0, 1.0, 1.0 };

        Assert.That(ChartRasterCanvas.IsDashOn(1.5, pattern), Is.True);
        Assert.That(ChartRasterCanvas.IsDashOn(2.5, pattern), Is.False);
        Assert.That(ChartRasterCanvas.IsDashOn(3.5, pattern), Is.True);
        Assert.That(ChartRasterCanvas.IsDashOn(4.5, pattern), Is.False);
        Assert.That(ChartRasterCanvas.IsDashOn(6.5, pattern), Is.True);
        Assert.That(ChartRasterCanvas.IsDashOn(7.5, pattern), Is.False);
        Assert.That(ChartRasterCanvas.IsDashOn(8.5, pattern), Is.True);
    }

    [Test]
    public void SceneRaster_RendersRoundedRectangleCornersAndDashedOutline()
    {
        ChartRenderScene scene = new ChartRenderScene(12, 12);
        ChartRenderStyle style = new ChartRenderStyle();

        style.SetFill(new ChartColor(220, 20, 20, 255));
        style.SetStroke(ChartColor.Black(), 1.0);
        style.SetDashArray(new double[] { 2.0, 1.0 });
        style.SetQuality(ChartRenderQuality.Aliased);
        style.SetBlendMode(ChartBlendMode.Source);
        scene.AddClear(ChartColor.White());
        scene.AddRect("rounded", 1.0, 1.0, 10.0, 10.0, 3.0, 3.0, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(6, 6), 220, 20, 20, 255);
        AssertColor(bitmap.GetPixel(4, 1), 0, 0, 0, 255);
        AssertColor(bitmap.GetPixel(6, 1), 220, 20, 20, 255);
    }

    [Test]
    public void SceneRaster_AppliesGradientAndPatternPaintsToShapesPathsAndStrokes()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 12);
        ChartRenderStyle gradientFill = new ChartRenderStyle();
        ChartRenderStyle patternFill = new ChartRenderStyle();
        ChartRenderStyle gradientStroke = new ChartRenderStyle();
        RawBitmap tile = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        ChartPath path = new ChartPath();

        tile.SetPixel(0, 0, new ChartColor(0, 200, 0, 255));
        tile.SetPixel(1, 0, new ChartColor(240, 220, 0, 255));
        gradientFill.SetFillGradient(
            new ChartPoint(1.0, 0.0),
            new ChartPoint(9.0, 0.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 });
        patternFill.SetFillPattern(tile);
        gradientStroke.SetNoFill();
        gradientStroke.SetStrokeGradient(
            new ChartPoint(1.0, 10.0),
            new ChartPoint(18.0, 10.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 },
            2.0);
        path.MoveTo(11.0, 1.0);
        path.LineTo(19.0, 1.0);
        path.LineTo(19.0, 8.0);
        path.LineTo(11.0, 8.0);
        path.Close();

        scene.AddCircle("gradient-circle", 5.0, 5.0, 4.0, gradientFill);
        scene.AddPath("pattern-path", path, patternFill);
        scene.AddLine("gradient-line", 1.0, 10.0, 18.0, 10.0, gradientStroke);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(2, 5).R, Is.GreaterThan(bitmap.GetPixel(2, 5).B));
        Assert.That(bitmap.GetPixel(8, 5).B, Is.GreaterThan(bitmap.GetPixel(8, 5).R));
        AssertColor(bitmap.GetPixel(12, 4), 0, 200, 0, 255);
        AssertColor(bitmap.GetPixel(13, 4), 240, 220, 0, 255);
        Assert.That(bitmap.GetPixel(2, 9).R, Is.GreaterThan(bitmap.GetPixel(2, 9).B));
        Assert.That(bitmap.GetPixel(17, 9).B, Is.GreaterThan(bitmap.GetPixel(17, 9).R));
    }

    [Test]
    public void SceneRaster_RotatesAndClipsRawBitmapImages()
    {
        ChartRenderScene scene = new ChartRenderScene(6, 5);
        RawBitmap image = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);

        image.SetPixel(0, 0, new ChartColor(255, 0, 0, 255));
        image.SetPixel(1, 0, new ChartColor(0, 0, 255, 255));
        scene.AddClear(ChartColor.White());
        scene.PushClip("image-clip", new ChartRect(3.0, 1.0, 1.0, 2.0));
        scene.PushTransform("rotate", new ChartMatrix(0.0, 1.0, -1.0, 0.0, 4.0, 1.0));
        scene.AddImage("image", image, 0.0, 0.0, 2.0, 1.0, new ChartRenderStyle());
        scene.PopTransform();
        scene.PopClip();

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(3, 1), 255, 0, 0, 255);
        AssertColor(bitmap.GetPixel(3, 2), 0, 0, 255, 255);
        AssertColor(bitmap.GetPixel(4, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 2), 255, 255, 255, 255);
    }

    [Test]
    public void SceneRaster_SupersamplesAntialiasedShapeCoverage()
    {
        ChartRenderScene aliasedScene = new ChartRenderScene(10, 10);
        ChartRenderScene antialiasedScene = new ChartRenderScene(10, 10);
        ChartRenderStyle aliasedStyle = new ChartRenderStyle();
        ChartRenderStyle antialiasedStyle = new ChartRenderStyle();

        aliasedStyle.SetFill(ChartColor.White());
        aliasedStyle.SetQuality(ChartRenderQuality.Aliased);
        antialiasedStyle.SetFill(ChartColor.White());
        antialiasedStyle.SetQuality(ChartRenderQuality.Antialiased);
        aliasedScene.AddCircle("circle", 5.0, 5.0, 3.5, aliasedStyle);
        antialiasedScene.AddCircle("circle", 5.0, 5.0, 3.5, antialiasedStyle);

        RawBitmap aliased = aliasedScene.ToRawBitmap();
        RawBitmap antialiased = antialiasedScene.ToRawBitmap();
        bool foundPartialCoverage = false;
        int y = 0;
        while (y < antialiased.GetHeight())
        {
            int x = 0;
            while (x < antialiased.GetWidth())
            {
                byte alpha = antialiased.GetPixel(x, y).A;
                if (alpha > 0 && alpha < 255)
                {
                    foundPartialCoverage = true;
                }

                Assert.That(aliased.GetPixel(x, y).A == 0 || aliased.GetPixel(x, y).A == 255, Is.True);
                x++;
            }

            y++;
        }

        Assert.That(foundPartialCoverage, Is.True);
    }

    [Test]
    public void SceneRaster_IntersectsTextBlockClipWithRetainedClip()
    {
        ChartRenderScene scene = new ChartRenderScene(12, 10);
        ChartTextBlock block = new ChartTextBlock();

        block.SetText("A");
        block.SetColor(ChartColor.Black());
        block.SetClip(new ChartRect(0.0, 0.0, 10.0, 8.0));
        scene.AddClear(ChartColor.White());
        scene.PushClip("outer", new ChartRect(2.0, 0.0, 2.0, 8.0));
        scene.AddTextBlock("text", block, 0.0, 0.0);
        scene.PopClip();

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(1, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 0), 0, 0, 0, 255);
        AssertColor(bitmap.GetPixel(4, 0), 255, 255, 255, 255);
    }

    [Test]
    public void SceneRaster_ClipsFilledPathsAtEveryClipBoundary()
    {
        ChartRenderScene scene = new ChartRenderScene(8, 8);
        ChartRenderStyle fill = new ChartRenderStyle();
        ChartPath path = new ChartPath();

        fill.SetFill(new ChartColor(20, 40, 220, 255));
        fill.SetQuality(ChartRenderQuality.Aliased);
        path.MoveTo(0.0, 0.0);
        path.LineTo(8.0, 0.0);
        path.LineTo(8.0, 8.0);
        path.LineTo(0.0, 8.0);
        path.Close();
        scene.AddClear(ChartColor.White());
        scene.PushClip("path-clip", new ChartRect(2.0, 2.0, 3.0, 3.0));
        scene.AddPath("path", path, fill);
        scene.PopClip();

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(2, 2), 20, 40, 220, 255);
        AssertColor(bitmap.GetPixel(4, 4), 20, 40, 220, 255);
        AssertColor(bitmap.GetPixel(1, 2), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 5), 255, 255, 255, 255);
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.R, Is.EqualTo(red));
        Assert.That(color.G, Is.EqualTo(green));
        Assert.That(color.B, Is.EqualTo(blue));
        Assert.That(color.A, Is.EqualTo(alpha));
    }
}
