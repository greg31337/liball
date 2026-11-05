namespace chart.Tests;

[TestFixture]
public class ChartRetainedSceneExtensionTests
{
    [Test]
    public void RetainedCommands_CopyTransformsClipsImagesAndPaintInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 20);
        ChartMatrix transform = new ChartMatrix(1.0, 0.0, 0.0, 1.0, 3.0, 4.0);
        ChartRect clip = new ChartRect(1.0, 2.0, 8.0, 9.0);
        RawBitmap image = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        ChartColor[] colors = new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) };
        double[] offsets = new double[] { 0.0, 1.0 };
        ChartRenderStyle gradient = new ChartRenderStyle();

        image.SetPixel(0, 0, new ChartColor(10, 20, 30, 255));
        gradient.SetFillGradient(new ChartPoint(0.0, 0.0), new ChartPoint(10.0, 0.0), colors, offsets);
        scene.BeginGroup("series");
        scene.PushTransform("move", transform);
        scene.PushClip("plot", clip);
        scene.AddArc("arc", 5.0, 5.0, 3.0, 0.0, System.Math.PI, gradient);
        scene.AddWedge("wedge", 5.0, 5.0, 1.0, 4.0, 0.0, System.Math.PI * 0.5, gradient);
        scene.AddImage("image", image, 0.0, 0.0, 2.0, 2.0, new ChartRenderStyle());
        scene.PopClip();
        scene.PopTransform();
        scene.EndGroup();

        transform.SetOffsetX(99.0);
        clip.SetX(99.0);
        image.SetPixel(0, 0, ChartColor.White());
        colors[0].SetR(0);
        offsets[0] = 0.5;

        Assert.That(scene.GetCommandCount(), Is.EqualTo(9));
        Assert.That(scene.CommandAt(0).GetKind(), Is.EqualTo(ChartRenderCommandKind.BeginGroup));
        Assert.That(scene.CommandAt(1).GetTransform().GetOffsetX(), Is.EqualTo(3.0));
        Assert.That(scene.CommandAt(2).GetClipBounds().GetX(), Is.EqualTo(1.0));
        Assert.That(scene.CommandAt(3).GetKind(), Is.EqualTo(ChartRenderCommandKind.Arc));
        Assert.That(scene.CommandAt(4).GetInnerRadius(), Is.EqualTo(1.0));
        Assert.That(scene.CommandAt(5).GetImage().GetPixel(0, 0).R, Is.EqualTo(10));
        Assert.That(scene.CommandAt(3).GetStyle().GetFillGradientColors()[0].R, Is.EqualTo(255));
        Assert.That(scene.CommandAt(3).GetStyle().GetFillGradientOffsets()[0], Is.EqualTo(0.0));
    }

    [Test]
    public void SvgReplay_EmitsGroupsTransformsClipsPaintsArcsWedgesAndImages()
    {
        ChartRenderScene scene = new ChartRenderScene(24, 20);
        ChartRenderStyle gradient = new ChartRenderStyle();
        ChartRenderStyle pattern = new ChartRenderStyle();
        RawBitmap tile = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);

        tile.SetPixel(0, 0, ChartColor.Black());
        tile.SetPixel(1, 0, ChartColor.White());
        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(10.0, 0.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 });
        pattern.SetFillPattern(tile);

        scene.BeginGroup("series");
        scene.PushTransform("translated", new ChartMatrix(1.0, 0.0, 0.0, 1.0, 2.0, 3.0));
        scene.PushClip("plot", new ChartRect(0.0, 0.0, 18.0, 14.0));
        scene.AddWedge("slice", 7.0, 7.0, 2.0, 5.0, 0.0, System.Math.PI, gradient);
        scene.AddArc("outline", 7.0, 7.0, 6.0, 0.0, System.Math.PI, pattern);
        scene.AddImage("logo", tile, 1.0, 1.0, 4.0, 2.0, new ChartRenderStyle());
        scene.PopClip();
        scene.PopTransform();
        scene.EndGroup();

        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("<g id=\"series\">"));
        Assert.That(svg, Does.Contain("<g id=\"translated\" transform=\"matrix(1 0 0 1 2 3)\">"));
        Assert.That(svg, Does.Contain("<clipPath id=\"plot-clip\">"));
        Assert.That(svg, Does.Contain("clip-path:url(#plot-clip)"));
        Assert.That(svg, Does.Contain("<linearGradient id=\"slice-fill-gradient\""));
        Assert.That(svg, Does.Contain("fill:url(#slice-fill-gradient)"));
        Assert.That(svg, Does.Contain("<pattern id=\"outline-fill-pattern\""));
        Assert.That(svg, Does.Contain("<path id=\"slice\""));
        Assert.That(svg, Does.Contain("<path id=\"outline\""));
        Assert.That(svg, Does.Contain("<image id=\"logo\""));
        Assert.That(svg, Does.Contain("href=\"data:image/png;base64,"));
    }

    [Test]
    public void RasterReplay_AppliesNestedTransformClipGradientPatternAndImagePlacement()
    {
        ChartRenderScene scene = new ChartRenderScene(14, 8);
        ChartRenderStyle red = new ChartRenderStyle();
        ChartRenderStyle gradient = new ChartRenderStyle();
        ChartRenderStyle pattern = new ChartRenderStyle();
        RawBitmap tile = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);

        red.SetFill(new ChartColor(255, 0, 0, 255));
        red.SetBlendMode(ChartBlendMode.Source);
        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(4.0, 0.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 });
        gradient.SetBlendMode(ChartBlendMode.Source);
        tile.SetPixel(0, 0, new ChartColor(0, 255, 0, 255));
        tile.SetPixel(1, 0, new ChartColor(255, 255, 0, 255));
        pattern.SetFillPattern(tile);
        pattern.SetBlendMode(ChartBlendMode.Source);

        scene.AddClear(ChartColor.White());
        scene.PushTransform("move", new ChartMatrix(1.0, 0.0, 0.0, 1.0, 4.0, 1.0));
        scene.PushClip("clip", new ChartRect(0.0, 0.0, 3.0, 3.0));
        scene.AddRect("clipped", 0.0, 0.0, 6.0, 5.0, 0.0, 0.0, red);
        scene.PopClip();
        scene.PopTransform();
        scene.AddRect("gradient", 0.0, 4.0, 4.0, 1.0, 0.0, 0.0, gradient);
        scene.AddRect("pattern", 4.0, 4.0, 4.0, 1.0, 0.0, 0.0, pattern);
        scene.AddImage("image", tile, 10.0, 1.0, 4.0, 2.0, new ChartRenderStyle());

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(4, 1).R, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(6, 3).R, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(7, 3).R, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(7, 3).G, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(0, 4).R, Is.GreaterThan(bitmap.GetPixel(0, 4).B));
        Assert.That(bitmap.GetPixel(3, 4).B, Is.GreaterThan(bitmap.GetPixel(3, 4).R));
        Assert.That(bitmap.GetPixel(4, 4).G, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(5, 4).R, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(10, 1).G, Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(13, 1).R, Is.EqualTo(255));
    }

    [Test]
    public void RetainedExtensions_ValidateInvalidGeometryAndPaintInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(10, 10);
        ChartRenderStyle style = new ChartRenderStyle();

        Assert.Throws<System.ArgumentException>(() => scene.AddArc("arc", 0.0, 0.0, 1.0, 0.0, 0.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => scene.AddWedge("wedge", 0.0, 0.0, 2.0, 1.0, 0.0, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => scene.AddImage("image", null!, 0.0, 0.0, 1.0, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => scene.PushTransform("transform", null!));
        Assert.Throws<System.ArgumentNullException>(() => scene.PushClip("clip", null!));
        Assert.Throws<System.ArgumentException>(() => style.SetFillGradient(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 0.0), new ChartColor[] { ChartColor.Black() }, new double[0]));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFillPattern(null!));
    }
}
