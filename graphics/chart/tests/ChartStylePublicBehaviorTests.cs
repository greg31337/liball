namespace chart.Tests;

[TestFixture]
public class ChartStylePublicBehaviorTests
{
    [Test]
    public void ChartStyle_CompoundMutationsSetAndClearEveryOptionalFeature()
    {
        ChartStyle style = new ChartStyle();
        ChartPath marker = TrianglePath();

        style.SetFill(new ChartColor(1, 2, 3, 4));
        style.SetStroke(new ChartColor(5, 6, 7, 8), 2.0);
        style.SetOpacity(0.5);
        style.SetFont("Test", 14);
        style.SetMarkerSize(6.0);
        style.SetDashArray(new double[] { 4.0, 2.0 });
        style.SetMiterLimit(3.0);
        style.SetFillOpacity(0.25);
        style.SetStrokeOpacity(0.75);
        style.SetBorder(new ChartColor(9, 10, 11, 12), 1.5);
        style.SetFontWeight(ChartFontWeight.Bold);
        style.SetFontStyle(ChartFontStyle.Italic);
        style.SetTextOutline(new ChartColor(13, 14, 15, 16), 0.5);
        style.SetTextShadow(new ChartColor(17, 18, 19, 20), 1.0, 2.0, 3.0);
        style.SetCustomMarker(marker);

        Assert.That(style.GetHasFillColor(), Is.True);
        Assert.That(style.GetFillNone(), Is.False);
        AssertColor(style.GetFillColor(), 1, 2, 3, 4);
        Assert.That(style.GetHasStrokeColor(), Is.True);
        Assert.That(style.GetStrokeNone(), Is.False);
        AssertColor(style.GetStrokeColor(), 5, 6, 7, 8);
        Assert.That(style.GetHasStrokeWidth(), Is.True);
        Assert.That(style.GetStrokeWidth(), Is.EqualTo(2.0));
        Assert.That(style.GetHasOpacity(), Is.True);
        Assert.That(style.GetOpacity(), Is.EqualTo(0.5));
        Assert.That(style.GetHasFontFamily(), Is.True);
        Assert.That(style.GetFontFamily(), Is.EqualTo("Test"));
        Assert.That(style.GetHasFontSize(), Is.True);
        Assert.That(style.GetFontSize(), Is.EqualTo(14));
        Assert.That(style.GetHasMarkerSize(), Is.True);
        Assert.That(style.GetMarkerSize(), Is.EqualTo(6.0));
        Assert.That(style.GetHasDashArray(), Is.True);
        Assert.That(style.GetDashArray(), Is.EqualTo(new double[] { 4.0, 2.0 }));
        Assert.That(style.GetHasMiterLimit(), Is.True);
        Assert.That(style.GetMiterLimit(), Is.EqualTo(3.0));
        Assert.That(style.GetHasFillOpacity(), Is.True);
        Assert.That(style.GetFillOpacity(), Is.EqualTo(0.25));
        Assert.That(style.GetHasStrokeOpacity(), Is.True);
        Assert.That(style.GetStrokeOpacity(), Is.EqualTo(0.75));
        Assert.That(style.GetHasBorder(), Is.True);
        Assert.That(style.GetBorderNone(), Is.False);
        AssertColor(style.GetBorderColor(), 9, 10, 11, 12);
        Assert.That(style.GetBorderWidth(), Is.EqualTo(1.5));
        Assert.That(style.GetHasFontWeight(), Is.True);
        Assert.That(style.GetFontWeight(), Is.EqualTo(ChartFontWeight.Bold));
        Assert.That(style.GetHasFontStyle(), Is.True);
        Assert.That(style.GetFontStyle(), Is.EqualTo(ChartFontStyle.Italic));
        Assert.That(style.GetHasTextOutline(), Is.True);
        AssertColor(style.GetTextOutlineColor(), 13, 14, 15, 16);
        Assert.That(style.GetTextOutlineWidth(), Is.EqualTo(0.5));
        Assert.That(style.GetHasTextShadow(), Is.True);
        AssertColor(style.GetTextShadowColor(), 17, 18, 19, 20);
        Assert.That(style.GetTextShadowOffsetX(), Is.EqualTo(1.0));
        Assert.That(style.GetTextShadowOffsetY(), Is.EqualTo(2.0));
        Assert.That(style.GetTextShadowBlur(), Is.EqualTo(3.0));
        Assert.That(style.GetHasCustomMarker(), Is.True);
        Assert.That(style.GetCustomMarker().GetCommands().Count, Is.EqualTo(4));

        ChartStyle copy = style.Copy();
        marker.LineTo(10.0, 10.0);
        style.GetDashArray()[0] = 99.0;
        Assert.That(copy.GetDashArray(), Is.EqualTo(new double[] { 4.0, 2.0 }));
        Assert.That(copy.GetCustomMarker().GetCommands().Count, Is.EqualTo(4));

        style.SetNoFill();
        style.SetNoStroke();
        style.SetNoBorder();
        style.ClearDashArray();
        style.ClearTextOutline();
        style.ClearTextShadow();
        style.ClearCustomMarker();
        Assert.That(style.GetFillNone(), Is.True);
        Assert.That(style.GetStrokeNone(), Is.True);
        Assert.That(style.GetBorderNone(), Is.True);
        Assert.That(style.GetHasDashArray(), Is.True);
        Assert.That(style.GetDashArray(), Is.Empty);
        Assert.That(style.GetHasTextOutline(), Is.True);
        Assert.That(style.GetTextOutlineWidth(), Is.Zero);
        Assert.That(style.GetHasTextShadow(), Is.True);
        Assert.That(style.GetTextShadowBlur(), Is.Zero);
        Assert.That(style.GetHasCustomMarker(), Is.True);
        Assert.That(style.GetCustomMarker().GetCommands(), Is.Empty);
    }

    [Test]
    public void ChartStyle_DocumentedFailuresAndInclusiveBoundariesAreStable()
    {
        ChartStyle style = new ChartStyle();
        style.SetOpacity(0.0);
        style.SetOpacity(1.0);
        style.SetStrokeWidth(0.0);
        style.SetMarkerSize(0.0);
        style.SetFillOpacity(0.0);
        style.SetStrokeOpacity(1.0);

        Assert.Throws<System.ArgumentNullException>(() => style.SetFill(null!));
        Assert.Throws<System.ArgumentNullException>(() => style.SetStroke(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStrokeWidth(-0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetOpacity(double.NaN));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFont(null!, 12));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFont("Test", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetMarkerSize(-0.1));
        Assert.Throws<System.ArgumentException>(() => style.SetDashArray(new double[0]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetDashArray(new double[] { 1.0, 0.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetMiterLimit(0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFillOpacity(-0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStrokeOpacity(1.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetBorder(ChartColor.Black(), 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFontWeight((ChartFontWeight)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFontStyle((ChartFontStyle)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetTextOutline(ChartColor.Black(), 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetTextShadow(ChartColor.Black(), 0.0, 0.0, -1.0));
        Assert.Throws<System.ArgumentNullException>(() => style.SetCustomMarker(null!));
    }

    [Test]
    public void RenderStyle_CompoundMutationsCopyGradientsPatternsAndTextEffects()
    {
        ChartRenderStyle style = new ChartRenderStyle();
        ChartColor[] colors = new ChartColor[] { ChartColor.Black(), ChartColor.White() };
        double[] offsets = new double[] { 0.0, 1.0 };
        RawBitmap pattern = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        pattern.SetPixel(0, 0, new ChartColor(1, 2, 3, 4));

        style.SetFill(new ChartColor(10, 20, 30, 40));
        style.SetStroke(new ChartColor(50, 60, 70, 80), 2.0);
        style.SetFillGradient(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0), colors, offsets);
        style.SetStrokeGradient(new ChartPoint(1.0, 0.0), new ChartPoint(0.0, 1.0), colors, offsets, 3.0);
        ChartRenderStyle gradientCopy = style.Copy();
        style.SetFillPattern(pattern);
        style.SetStrokePattern(pattern, 4.0);
        ChartRenderStyle patternCopy = style.Copy();
        style.SetLine(ChartLineCap.Round, ChartLineJoin.Bevel);
        style.SetDashArray(new double[] { 2.0, 1.0 });
        style.SetMiterLimit(5.0);
        style.SetFillOpacity(0.2);
        style.SetStrokeOpacity(0.8);
        style.SetBorder(ChartColor.Black(), 1.0);
        style.SetFillRule(ChartFillRule.EvenOdd);
        style.SetOpacity(0.6);
        style.SetFont("Test", 16);
        style.SetFontWeight(ChartFontWeight.Bold);
        style.SetFontStyle(ChartFontStyle.Oblique);
        style.SetTextOutline(ChartColor.White(), 0.5);
        style.SetTextShadow(ChartColor.Black(), 1.0, 2.0, 3.0);
        style.SetTextScale(2);
        style.SetQuality(ChartRenderQuality.Antialiased);
        style.SetBlendMode(ChartBlendMode.Multiply);

        Assert.That(style.GetHasFill(), Is.True);
        Assert.That(style.GetHasStroke(), Is.True);
        Assert.That(style.GetHasFillGradient(), Is.False);
        Assert.That(style.GetHasStrokeGradient(), Is.False);
        Assert.That(gradientCopy.GetHasFillGradient(), Is.True);
        Assert.That(gradientCopy.GetHasStrokeGradient(), Is.True);
        Assert.That(gradientCopy.GetFillGradientColors().Length, Is.EqualTo(2));
        Assert.That(gradientCopy.GetStrokeGradientOffsets(), Is.EqualTo(new double[] { 0.0, 1.0 }));
        Assert.That(style.GetHasFillPattern(), Is.True);
        Assert.That(style.GetHasStrokePattern(), Is.False);
        Assert.That(patternCopy.GetHasStrokePattern(), Is.True);
        Assert.That(style.GetLineCap(), Is.EqualTo(ChartLineCap.Round));
        Assert.That(style.GetLineJoin(), Is.EqualTo(ChartLineJoin.Bevel));
        Assert.That(style.GetDashArray(), Is.EqualTo(new double[] { 2.0, 1.0 }));
        Assert.That(style.GetMiterLimit(), Is.EqualTo(5.0));
        Assert.That(style.GetFillOpacity(), Is.EqualTo(0.2));
        Assert.That(style.GetStrokeOpacity(), Is.EqualTo(0.8));
        Assert.That(style.GetHasBorder(), Is.True);
        Assert.That(style.GetFillRule(), Is.EqualTo(ChartFillRule.EvenOdd));
        Assert.That(style.GetOpacity(), Is.EqualTo(0.6));
        Assert.That(style.GetFontFamily(), Is.EqualTo("Test"));
        Assert.That(style.GetFontSize(), Is.EqualTo(16));
        Assert.That(style.GetFontWeight(), Is.EqualTo(ChartFontWeight.Bold));
        Assert.That(style.GetFontStyle(), Is.EqualTo(ChartFontStyle.Oblique));
        Assert.That(style.GetHasTextOutline(), Is.True);
        Assert.That(style.GetHasTextShadow(), Is.True);
        Assert.That(style.GetTextScale(), Is.EqualTo(2));
        Assert.That(style.GetQuality(), Is.EqualTo(ChartRenderQuality.Antialiased));
        Assert.That(style.GetBlendMode(), Is.EqualTo(ChartBlendMode.Multiply));

        ChartRenderStyle copy = style.Copy();
        colors[0].SetR(99);
        offsets[0] = 0.5;
        pattern.SetPixel(0, 0, ChartColor.White());
        AssertColor(gradientCopy.GetFillGradientColors()[0], 0, 0, 0, 255);
        Assert.That(gradientCopy.GetFillGradientOffsets()[0], Is.Zero);
        AssertColor(copy.GetFillPattern().GetPixel(0, 0), 1, 2, 3, 4);

        style.SetNoFill();
        style.SetNoStroke();
        style.ClearDashArray();
        style.ClearTextOutline();
        style.ClearTextShadow();
        Assert.That(style.GetFillNone(), Is.True);
        Assert.That(style.GetStrokeNone(), Is.True);
        Assert.That(style.GetDashArray(), Is.Empty);
        Assert.That(style.GetHasTextOutline(), Is.False);
        Assert.That(style.GetHasTextShadow(), Is.False);
    }

    [Test]
    public void RenderStyle_DocumentedFailuresRejectInvalidInputs()
    {
        ChartRenderStyle style = new ChartRenderStyle();
        ChartColor[] colors = new ChartColor[] { ChartColor.Black(), ChartColor.White() };
        double[] offsets = new double[] { 0.0, 1.0 };

        Assert.Throws<System.ArgumentNullException>(() => style.SetFill(null!));
        Assert.Throws<System.ArgumentNullException>(() => style.SetStroke(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetOpacity(-0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetTextScale(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetQuality((ChartRenderQuality)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetBlendMode((ChartBlendMode)99));
        Assert.Throws<System.ArgumentException>(() => style.SetFillGradient(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0), colors, new double[] { 0.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStrokeGradient(new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0), colors, offsets, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFillPattern(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStrokePattern(new RawBitmap(1, 1, ChartPixelFormat.Rgba32), 0.0));
    }

    static ChartPath TrianglePath()
    {
        ChartPath path = new ChartPath();
        path.MoveTo(0.0, 0.0);
        path.LineTo(1.0, 0.0);
        path.LineTo(0.0, 1.0);
        path.Close();
        return path;
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo((byte)red));
        Assert.That(color.GetG(), Is.EqualTo((byte)green));
        Assert.That(color.GetB(), Is.EqualTo((byte)blue));
        Assert.That(color.GetA(), Is.EqualTo((byte)alpha));
    }
}
