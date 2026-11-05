namespace chart.Tests;

[TestFixture]
public class ChartRasterConformanceTests
{
    [Test]
    public void RetainedScene_RasterAcceptsEveryCommandKindAndAppliesStructuralCommands()
    {
        ChartRenderScene scene = new ChartRenderScene(48, 24);
        ChartRenderStyle fill = CreateSourceFill(new ChartColor(20, 80, 220, 255));
        ChartRenderStyle stroke = CreateSourceStroke(new ChartColor(220, 40, 20, 255), 1.0);
        ChartPath path = CreateBoxPath(19.0, 2.0, 4.0, 4.0);
        RawBitmap image = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);

        image.SetPixel(0, 0, new ChartColor(10, 180, 40, 255));
        scene.AddClear(ChartColor.White());
        scene.BeginGroup("all-commands");
        scene.AddLine("line", 1.5, 2.5, 5.5, 2.5, stroke);
        scene.AddPolyline("polyline", new ChartPoint[] { new ChartPoint(7.5, 2.5), new ChartPoint(11.5, 2.5), new ChartPoint(11.5, 5.5) }, stroke);
        scene.AddPolygon("polygon", new ChartPoint[] { new ChartPoint(13.0, 2.0), new ChartPoint(17.0, 2.0), new ChartPoint(15.0, 6.0) }, fill);
        scene.AddRect("rect", 1.0, 8.0, 4.0, 4.0, 0.0, 0.0, fill);
        scene.AddCircle("circle", 9.0, 10.0, 2.0, fill);
        scene.AddEllipse("ellipse", 15.0, 10.0, 2.0, 1.5, fill);
        scene.AddPath("path", path, fill);
        scene.AddText("text", "A", 25.0, 2.0, fill);
        scene.AddMarker("marker", ChartMarkerShape.Square, 34.0, 4.0, 4.0, fill);
        scene.AddArc("arc", 40.5, 4.5, 2.0, 0.0, System.Math.PI * 0.5, stroke);
        scene.AddWedge("wedge", 27.5, 11.5, 0.0, 2.0, 0.0, System.Math.PI * 0.5, fill);
        scene.AddImage("image", image, 32.0, 9.0, 3.0, 3.0, new ChartRenderStyle());
        scene.PushTransform("translation", ChartMatrix.Translation(5.0, 0.0));
        scene.AddRect("transformed", 32.0, 15.0, 3.0, 3.0, 0.0, 0.0, fill);
        scene.PopTransform();
        scene.PushClip("clip", new ChartRect(42.0, 15.0, 2.0, 3.0));
        scene.AddRect("clipped", 40.0, 15.0, 6.0, 3.0, 0.0, 0.0, fill);
        scene.PopClip();
        scene.EndGroup();

        ChartRenderCommandKind[] expectedKinds = new ChartRenderCommandKind[]
        {
            ChartRenderCommandKind.Clear,
            ChartRenderCommandKind.Line,
            ChartRenderCommandKind.Polyline,
            ChartRenderCommandKind.Polygon,
            ChartRenderCommandKind.Rect,
            ChartRenderCommandKind.Circle,
            ChartRenderCommandKind.Ellipse,
            ChartRenderCommandKind.Path,
            ChartRenderCommandKind.Text,
            ChartRenderCommandKind.Marker,
            ChartRenderCommandKind.Arc,
            ChartRenderCommandKind.Wedge,
            ChartRenderCommandKind.Image,
            ChartRenderCommandKind.BeginGroup,
            ChartRenderCommandKind.EndGroup,
            ChartRenderCommandKind.PushTransform,
            ChartRenderCommandKind.PopTransform,
            ChartRenderCommandKind.PushClip,
            ChartRenderCommandKind.PopClip
        };
        int index = 0;
        while (index < expectedKinds.Length)
        {
            AssertCommandKindPresent(scene, expectedKinds[index]);
            index++;
        }

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(3, 9), 20, 80, 220, 255);
        AssertColor(bitmap.GetPixel(15, 3), 20, 80, 220, 255);
        AssertColor(bitmap.GetPixel(21, 4), 20, 80, 220, 255);
        AssertColor(bitmap.GetPixel(34, 10), 10, 180, 40, 255);
        AssertColor(bitmap.GetPixel(38, 16), 20, 80, 220, 255);
        AssertColor(bitmap.GetPixel(41, 16), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(42, 16), 20, 80, 220, 255);
        AssertColor(bitmap.GetPixel(44, 16), 255, 255, 255, 255);
    }

    [TestCase(ChartPixelFormat.Rgba32, 73, 73, 73, 201)]
    [TestCase(ChartPixelFormat.Bgra32, 73, 73, 73, 201)]
    [TestCase(ChartPixelFormat.Rgb24, 73, 73, 73, 255)]
    [TestCase(ChartPixelFormat.Grayscale8, 73, 73, 73, 255)]
    [TestCase(ChartPixelFormat.Alpha8, 0, 0, 0, 201)]
    [TestCase(ChartPixelFormat.Indexed8, 73, 73, 73, 255)]
    public void RetainedScene_RasterWritesEveryPixelFormat(ChartPixelFormat format, int red, int green, int blue, int alpha)
    {
        ChartRenderScene scene = new ChartRenderScene(2, 2);
        ChartRenderStyle style = CreateSourceFill(new ChartColor(73, 73, 73, 201));
        RawBitmap bitmap = new RawBitmap(2, 2, format);

        scene.AddRect("pixel", 0.0, 0.0, 2.0, 2.0, 0.0, 0.0, style);
        scene.RenderRaster(bitmap);

        AssertColor(bitmap.GetPixel(1, 1), red, green, blue, alpha);
    }

    [TestCase(ChartLineCap.Butt, false, false)]
    [TestCase(ChartLineCap.Round, true, false)]
    [TestCase(ChartLineCap.Square, true, true)]
    public void RetainedScene_RasterConformsToEveryLineCap(ChartLineCap cap, bool extendsEndpoint, bool fillsSquareCorner)
    {
        ChartRenderScene scene = new ChartRenderScene(10, 7);
        ChartRenderStyle style = CreateSourceStroke(ChartColor.Black(), 4.0);

        style.SetLine(cap, ChartLineJoin.Bevel);
        scene.AddLine("cap", 3.5, 3.5, 7.5, 3.5, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(2, 3).A > 0, Is.EqualTo(extendsEndpoint));
        Assert.That(bitmap.GetPixel(1, 1).A > 0, Is.EqualTo(fillsSquareCorner));
        Assert.That(bitmap.GetPixel(5, 3).A, Is.EqualTo(255));
    }

    [TestCase(ChartLineJoin.Bevel, false, false)]
    [TestCase(ChartLineJoin.Round, true, false)]
    [TestCase(ChartLineJoin.Miter, true, true)]
    public void RetainedScene_RasterConformsToEveryLineJoin(ChartLineJoin join, bool fillsNearCorner, bool fillsMiterTip)
    {
        ChartRenderScene scene = new ChartRenderScene(9, 8);
        ChartRenderStyle style = CreateSourceStroke(ChartColor.Black(), 3.0);

        style.SetLine(ChartLineCap.Butt, join);
        style.SetMiterLimit(2.0);
        scene.AddPolyline("join", new ChartPoint[]
        {
            new ChartPoint(2.5, 2.5),
            new ChartPoint(5.5, 2.5),
            new ChartPoint(5.5, 5.5)
        }, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(6, 1).A > 0, Is.EqualTo(fillsNearCorner));
        Assert.That(bitmap.GetPixel(7, 2).A > 0, Is.EqualTo(fillsMiterTip));
    }

    [TestCase(ChartFillRule.Nonzero, 255)]
    [TestCase(ChartFillRule.EvenOdd, 0)]
    public void RetainedScene_RasterConformsToEveryFillRule(ChartFillRule fillRule, int centerAlpha)
    {
        ChartRenderScene scene = new ChartRenderScene(9, 9);
        ChartRenderStyle style = CreateSourceFill(ChartColor.Black());

        style.SetFillRule(fillRule);
        scene.AddPolygon("double-wound", new ChartPoint[]
        {
            new ChartPoint(1.0, 1.0),
            new ChartPoint(8.0, 1.0),
            new ChartPoint(8.0, 8.0),
            new ChartPoint(1.0, 8.0),
            new ChartPoint(1.0, 1.0),
            new ChartPoint(8.0, 1.0),
            new ChartPoint(8.0, 8.0),
            new ChartPoint(1.0, 8.0)
        }, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(4, 4).A, Is.EqualTo(centerAlpha));
    }

    [Test]
    public void RetainedScene_RasterConformsToSolidEvenAndOddDashPatterns()
    {
        RawBitmap solid = RenderDashedLine(new double[0]);
        RawBitmap even = RenderDashedLine(new double[] { 2.0, 2.0 });
        RawBitmap odd = RenderDashedLine(new double[] { 2.0, 1.0, 1.0 });

        Assert.That(solid.GetPixel(2, 0).A, Is.EqualTo(255));
        Assert.That(even.GetPixel(2, 0).A, Is.EqualTo(0));
        Assert.That(even.GetPixel(4, 0).A, Is.EqualTo(255));
        Assert.That(odd.GetPixel(2, 0).A, Is.EqualTo(0));
        Assert.That(odd.GetPixel(3, 0).A, Is.EqualTo(255));
        Assert.That(odd.GetPixel(4, 0).A, Is.EqualTo(0));
        Assert.That(odd.GetPixel(6, 0).A, Is.EqualTo(255));
    }

    [TestCase(ChartMarkerShape.Circle)]
    [TestCase(ChartMarkerShape.Square)]
    [TestCase(ChartMarkerShape.Diamond)]
    [TestCase(ChartMarkerShape.Triangle)]
    [TestCase(ChartMarkerShape.Cross)]
    [TestCase(ChartMarkerShape.Plus)]
    [TestCase(ChartMarkerShape.Star)]
    [TestCase(ChartMarkerShape.Line)]
    public void RetainedScene_RasterRendersEveryBuiltInMarker(ChartMarkerShape shape)
    {
        ChartRenderScene scene = new ChartRenderScene(11, 11);
        ChartRenderStyle style = CreateSourceFill(ChartColor.Black());

        scene.AddMarker("marker", shape, 5.5, 5.5, 5.0, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(CountOpaquePixels(bitmap), Is.GreaterThan(0));
        Assert.That(scene.CommandAt(0).GetMarkerShape(), Is.EqualTo(shape));
    }

    [Test]
    public void RetainedScene_RasterRendersCustomMarkerAndBothTextDirections()
    {
        ChartPath markerPath = new ChartPath();
        ChartRenderStyle markerStyle = CreateSourceFill(new ChartColor(200, 20, 20, 255));
        ChartTextBlock leftToRight = new ChartTextBlock();
        ChartTextBlock rightToLeft = new ChartTextBlock();
        ChartTextBlock reversed = new ChartTextBlock();

        markerPath.MoveTo(0.0, -0.5);
        markerPath.LineTo(0.5, 0.5);
        markerPath.LineTo(-0.5, 0.5);
        markerPath.Close();
        leftToRight.SetText("AB");
        rightToLeft.SetText("AB");
        rightToLeft.SetDirection(ChartTextDirection.RightToLeft);
        reversed.SetText("BA");

        ChartRenderScene leftScene = new ChartRenderScene(20, 10);
        ChartRenderScene rightScene = new ChartRenderScene(20, 10);
        ChartRenderScene reversedScene = new ChartRenderScene(20, 10);
        leftScene.AddCustomMarker("custom", markerPath, 16.0, 5.0, 5.0, markerStyle);
        leftScene.AddTextBlock("ltr", leftToRight, 0.0, 0.0);
        rightScene.AddTextBlock("rtl", rightToLeft, 0.0, 0.0);
        reversedScene.AddTextBlock("reversed", reversed, 0.0, 0.0);

        RawBitmap leftBitmap = leftScene.ToRawBitmap();
        RawBitmap rightBitmap = rightScene.ToRawBitmap();
        RawBitmap reversedBitmap = reversedScene.ToRawBitmap();

        Assert.That(leftBitmap.GetPixel(16, 5).R, Is.EqualTo(200));
        Assert.That(leftBitmap.CopyBuffer(), Is.Not.EqualTo(rightBitmap.CopyBuffer()));
        Assert.That(reversedBitmap.CopyBuffer(), Is.EqualTo(rightBitmap.CopyBuffer()));
    }

    [Test]
    public void RetainedScene_RasterConformsToGradientPatternAndImagePaints()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        ChartRenderStyle gradientFill = new ChartRenderStyle();
        ChartRenderStyle patternStroke = new ChartRenderStyle();
        RawBitmap pattern = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        RawBitmap image = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);

        pattern.SetPixel(0, 0, new ChartColor(0, 200, 0, 255));
        pattern.SetPixel(1, 0, new ChartColor(220, 200, 0, 255));
        image.SetPixel(0, 0, new ChartColor(10, 20, 230, 255));
        image.SetPixel(1, 0, new ChartColor(230, 20, 10, 255));
        gradientFill.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(8.0, 0.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 });
        gradientFill.SetQuality(ChartRenderQuality.Aliased);
        gradientFill.SetBlendMode(ChartBlendMode.Source);
        patternStroke.SetNoFill();
        patternStroke.SetStrokePattern(pattern, 2.0);
        patternStroke.SetQuality(ChartRenderQuality.Aliased);
        patternStroke.SetBlendMode(ChartBlendMode.Source);
        scene.AddRect("gradient", 0.0, 0.0, 8.0, 4.0, 0.0, 0.0, gradientFill);
        scene.AddLine("pattern", 0.5, 6.5, 7.5, 6.5, patternStroke);
        scene.AddImage("image", image, 10.0, 0.0, 8.0, 4.0, new ChartRenderStyle());

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(1, 1).R, Is.GreaterThan(bitmap.GetPixel(1, 1).B));
        Assert.That(bitmap.GetPixel(6, 1).B, Is.GreaterThan(bitmap.GetPixel(6, 1).R));
        AssertColor(bitmap.GetPixel(0, 6), 0, 200, 0, 255);
        AssertColor(bitmap.GetPixel(1, 6), 220, 200, 0, 255);
        AssertColor(bitmap.GetPixel(11, 1), 10, 20, 230, 255);
        AssertColor(bitmap.GetPixel(16, 1), 230, 20, 10, 255);
    }

    [TestCase(ChartRenderQuality.Aliased, false)]
    [TestCase(ChartRenderQuality.Antialiased, true)]
    public void RetainedScene_RasterConformsToEveryQualitySetting(ChartRenderQuality quality, bool expectsPartialAlpha)
    {
        ChartRenderScene scene = new ChartRenderScene(10, 10);
        ChartRenderStyle style = CreateSourceFill(ChartColor.White());

        style.SetQuality(quality);
        style.SetBlendMode(ChartBlendMode.SourceOver);
        scene.AddCircle("circle", 5.0, 5.0, 3.5, style);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(HasPartialAlpha(bitmap), Is.EqualTo(expectsPartialAlpha));
        Assert.That(CountOpaquePixels(bitmap), Is.GreaterThan(0));
    }

    private static ChartRenderStyle CreateSourceFill(ChartColor color)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(color);
        style.SetQuality(ChartRenderQuality.Aliased);
        style.SetBlendMode(ChartBlendMode.Source);
        return style;
    }

    private static ChartRenderStyle CreateSourceStroke(ChartColor color, double width)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetNoFill();
        style.SetStroke(color, width);
        style.SetQuality(ChartRenderQuality.Aliased);
        style.SetBlendMode(ChartBlendMode.Source);
        return style;
    }

    private static ChartPath CreateBoxPath(double x, double y, double width, double height)
    {
        ChartPath path = new ChartPath();
        path.MoveTo(x, y);
        path.LineTo(x + width, y);
        path.LineTo(x + width, y + height);
        path.LineTo(x, y + height);
        path.Close();
        return path;
    }

    private static RawBitmap RenderDashedLine(double[] dashArray)
    {
        ChartRenderScene scene = new ChartRenderScene(10, 1);
        ChartRenderStyle style = CreateSourceStroke(ChartColor.Black(), 1.0);
        if (dashArray.Length > 0)
        {
            style.SetDashArray(dashArray);
        }

        scene.AddLine("dash", 0.5, 0.5, 9.5, 0.5, style);
        return scene.ToRawBitmap();
    }

    private static void AssertCommandKindPresent(ChartRenderScene scene, ChartRenderCommandKind expected)
    {
        bool found = false;
        int index = 0;
        while (index < scene.GetCommandCount())
        {
            if (scene.CommandAt(index).GetKind() == expected)
            {
                found = true;
            }

            index++;
        }

        Assert.That(found, Is.True, "Missing retained command kind " + expected);
    }

    private static int CountOpaquePixels(RawBitmap bitmap)
    {
        int count = 0;
        int y = 0;
        while (y < bitmap.GetHeight())
        {
            int x = 0;
            while (x < bitmap.GetWidth())
            {
                if (bitmap.GetPixel(x, y).A == 255)
                {
                    count++;
                }

                x++;
            }

            y++;
        }

        return count;
    }

    private static bool HasPartialAlpha(RawBitmap bitmap)
    {
        int y = 0;
        while (y < bitmap.GetHeight())
        {
            int x = 0;
            while (x < bitmap.GetWidth())
            {
                byte alpha = bitmap.GetPixel(x, y).A;
                if (alpha > 0 && alpha < 255)
                {
                    return true;
                }

                x++;
            }

            y++;
        }

        return false;
    }

    private static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.R, Is.EqualTo(red));
        Assert.That(color.G, Is.EqualTo(green));
        Assert.That(color.B, Is.EqualTo(blue));
        Assert.That(color.A, Is.EqualTo(alpha));
    }
}
