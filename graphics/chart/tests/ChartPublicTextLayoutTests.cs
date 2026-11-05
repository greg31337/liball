namespace chart.Tests;

[TestFixture]
public class ChartPublicTextLayoutTests
{
    [Test]
    public void PreparedBuiltInText_UsesOneFallbackPerGraphemeAndDeterministicRtlOrder()
    {
        ChartTextBlock block = new ChartTextBlock();

        block.SetText("ab😀e\u0301");
        Assert.That(block.PreparedBuiltInText(), Is.EqualTo("AB??"));

        block.SetDirection(ChartTextDirection.RightToLeft);
        Assert.That(block.PreparedBuiltInText(), Is.EqualTo("??BA"));

        block.SetText("AB\nCD");
        Assert.That(block.PreparedBuiltInText(), Is.EqualTo("BA\nDC"));

        block.SetText("AB\r\nCD");
        block.SetAlignment(ChartTextAlignment.Start, ChartVerticalTextAlignment.Top);
        ChartTextLayout layout = block.LayoutBuiltIn(20, 0, 1);
        Assert.That(layout.LineAt(0).GetText(), Is.EqualTo("BA"));
        Assert.That(layout.LineAt(1).GetText(), Is.EqualTo("DC"));
        Assert.That(layout.LineAt(0).GetX(), Is.EqualTo(9));
    }

    [Test]
    public void WrappingAndMeasurement_UseDeterministicGlyphWidths()
    {
        ChartTextBlock block = new ChartTextBlock();

        block.SetText("one two three");
        block.SetWrapping(29);

        string wrapped = block.WrappedBuiltInText(1);
        ChartTextMeasurement measurement = block.MeasureBuiltIn(1);

        Assert.That(wrapped, Is.EqualTo("ONE\nTWO\nTHREE"));
        Assert.That(measurement.GetWidth(), Is.EqualTo(29));
        Assert.That(measurement.GetHeight(), Is.EqualTo(23));
        Assert.That(measurement.GetLineCount(), Is.EqualTo(3));
        block.ClearWrapping();
        Assert.That(block.GetMaximumWidth(), Is.EqualTo(0));
    }

    [Test]
    public void Layout_AppliesMultilineAlignmentClipAndRotationPlanning()
    {
        ChartTextBlock block = new ChartTextBlock();

        block.SetText("A\nABC");
        block.SetAlignment(ChartTextAlignment.Center, ChartVerticalTextAlignment.Middle);
        block.SetClip(new ChartRect(10.0, 5.0, 20.0, 12.0));
        block.SetRotation(90.0);

        ChartTextLayout layout = block.LayoutBuiltIn(20, 20, 1);
        ChartTextRotationPlan rotation = block.PlanBuiltInRotation(layout, 20.0, 20.0);

        Assert.That(layout.GetLineCount(), Is.EqualTo(2));
        Assert.That(layout.LineAt(0).GetX(), Is.EqualTo(18));
        Assert.That(layout.LineAt(1).GetX(), Is.EqualTo(12));
        Assert.That(layout.GetBounds().GetY(), Is.EqualTo(13.0));
        Assert.That(layout.GetHasClip(), Is.True);
        Assert.That(layout.GetClipBounds().GetWidth(), Is.EqualTo(20.0));
        Assert.That(rotation.GetAngleRadians(), Is.EqualTo(System.Math.PI * 0.5).Within(0.0000001));
        Assert.That(rotation.GetRotatedBounds().GetWidth(), Is.EqualTo(15.0).Within(0.0000001));
        Assert.That(rotation.GetRotatedBounds().GetHeight(), Is.EqualTo(17.0).Within(0.0000001));
        Assert.That(rotation.GetCorners().Length, Is.EqualTo(4));
    }

    [Test]
    public void RichSpans_AreCopiedAndSerializedWithIndependentStyles()
    {
        ChartTextBlock block = new ChartTextBlock();
        ChartRenderStyle red = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));
        ChartRenderStyle blue = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 200));
        ChartTextSpan first = new ChartTextSpan("Hot", red);
        ChartTextSpan second = new ChartTextSpan("Cold", blue);
        ChartRenderScene scene = new ChartRenderScene(100, 40);

        block.AddRichSpan(first);
        block.AddRichSpan(second);
        ChartRenderCommand retained = scene.AddTextBlock("rich", block, 5.0, 20.0);
        first.SetText("changed");
        red.SetFill(ChartColor.Black());
        block.ClearRichSpans();

        RawBitmap bitmap = scene.ToRawBitmap();
        string svg = scene.ToSvgString();

        Assert.That(retained.GetHasTextBlock(), Is.True);
        Assert.That(retained.GetTextBlock().GetRichSpanCount(), Is.EqualTo(2));
        Assert.That(svg, Does.Contain("<text id=\"rich\""));
        Assert.That(svg, Does.Contain("<tspan style=\"fill:#C80000"));
        Assert.That(svg, Does.Contain(">HOT</tspan>"));
        Assert.That(svg, Does.Contain("<tspan style=\"fill:#0000C8"));
        Assert.That(svg, Does.Contain(">COLD</tspan>"));
        AssertColor(bitmap.GetPixel(5, 20), 200, 0, 0, 255);
        AssertColor(bitmap.GetPixel(24, 20), 0, 0, 200, 255);
    }

    [Test]
    public void TextBlockSvg_RetainsWrappingRotationAndClipping()
    {
        ChartTextBlock block = new ChartTextBlock();
        ChartRenderScene scene = new ChartRenderScene(120, 60);

        block.SetText("one two omega Ω");
        block.SetWrapping(35);
        block.SetAlignment(ChartTextAlignment.Center, ChartVerticalTextAlignment.Top);
        block.SetRotation(30.0);
        block.SetClip(new ChartRect(5.0, 5.0, 80.0, 40.0));
        scene.AddTextBlock("wrapped", block, 50.0, 10.0);

        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("<clipPath id=\"wrapped-clip\">"));
        Assert.That(svg, Does.Contain("clip-path:url(#wrapped-clip)"));
        Assert.That(svg, Does.Contain("transform=\"rotate(30 50 10)\""));
        Assert.That(svg, Does.Contain("id=\"wrapped-line-0\""));
        Assert.That(svg, Does.Contain("OMEGA"));
        Assert.That(svg, Does.Contain(">?</text>"));
    }

    [Test]
    public void TextBlockRaster_UsesConfiguredClipAndRestoresSceneClip()
    {
        ChartTextBlock clipped = new ChartTextBlock();
        ChartTextBlock visible = new ChartTextBlock();
        ChartRenderScene scene = new ChartRenderScene(16, 10);

        clipped.SetText("A");
        clipped.SetClip(new ChartRect(3.0, 0.0, 3.0, 8.0));
        visible.SetText("A");
        scene.AddTextBlock("clipped", clipped, 0.0, 0.0);
        scene.AddTextBlock("visible", visible, 8.0, 0.0);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(1, 0).GetA(), Is.EqualTo(0));
        Assert.That(bitmap.GetPixel(3, 0).GetA(), Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(9, 0).GetA(), Is.EqualTo(255));
    }

    [Test]
    public void TextBlockRaster_RotatesBuiltInGlyphPixelsAroundAnchor()
    {
        ChartTextBlock block = new ChartTextBlock();
        ChartRenderScene scene = new ChartRenderScene(12, 12);

        block.SetText("A");
        block.SetRotation(90.0);
        scene.AddTextBlock("rotated", block, 4.0, 4.0);

        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(bitmap.GetPixel(4, 5).GetA(), Is.EqualTo(255));
        Assert.That(bitmap.GetPixel(5, 4).GetA(), Is.EqualTo(0));
    }

    [Test]
    public void PublicTextLayout_ValidatesConfigurationAndIndexes()
    {
        ChartTextBlock block = new ChartTextBlock();
        ChartTextLayout layout = block.LayoutBuiltIn(0, 0, 1);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => block.SetWrapping(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => block.SetAlignment((ChartTextAlignment)99, ChartVerticalTextAlignment.Top));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => block.SetDirection((ChartTextDirection)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => block.SetRotation(double.NaN));
        Assert.Throws<System.ArgumentNullException>(() => block.SetClip(null!));
        Assert.Throws<System.ArgumentNullException>(() => block.AddRichSpan(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => layout.LineAt(1));
        Assert.Throws<System.ArgumentNullException>(() => new ChartTextSpan("x", null!));
    }

    private static void AssertColor(ChartColor color, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo(red));
        Assert.That(color.GetG(), Is.EqualTo(green));
        Assert.That(color.GetB(), Is.EqualTo(blue));
        Assert.That(color.GetA(), Is.EqualTo(alpha));
    }
}
