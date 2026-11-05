namespace chart.Tests;

[TestFixture]
public class ChartBitmapFontTests
{
    [Test]
    public void GetGlyph_ReturnsBuiltInFiveBySevenData()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
        ChartBitmapGlyph upper = font.GetGlyph('A');
        ChartBitmapGlyph lower = font.GetGlyph('a');
        ChartBitmapGlyph digit = font.GetGlyph('2');

        Assert.That(font.GlyphWidth, Is.EqualTo(5));
        Assert.That(font.GlyphHeight, Is.EqualTo(7));
        Assert.That(font.Spacing, Is.EqualTo(1));
        Assert.That(upper.IsPixelSet(1, 0), Is.True);
        Assert.That(upper.IsPixelSet(0, 0), Is.False);
        Assert.That(upper.IsPixelSet(0, 3), Is.True);
        Assert.That(lower.IsPixelSet(1, 0), Is.True);
        Assert.That(digit.IsPixelSet(4, 6), Is.True);
    }

    [Test]
    public void GetGlyph_ReturnsUnknownGlyphForUnsupportedCharacters()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
        ChartBitmapGlyph glyph = font.GetGlyph('@');

        Assert.That(glyph.IsPixelSet(1, 0), Is.True);
        Assert.That(glyph.IsPixelSet(2, 6), Is.True);
    }

    [Test]
    public void TextWidthAndHeight_MeasureScaledText()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();

        Assert.That(font.GlyphAdvance(2), Is.EqualTo(12));
        Assert.That(font.TextWidth("AB", 2), Is.EqualTo(22));
        Assert.That(font.TextWidth("A\nABC", 1), Is.EqualTo(17));
        Assert.That(font.TextHeight("A\nB", 2), Is.EqualTo(30));
    }

    [Test]
    public void MeasureText_ReturnsLineAndBaselineMetrics()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();

        ChartTextMeasurement measurement = font.MeasureText("A\nABC", 1);

        Assert.That(measurement.Width, Is.EqualTo(17));
        Assert.That(measurement.Height, Is.EqualTo(15));
        Assert.That(measurement.LineCount, Is.EqualTo(2));
        Assert.That(measurement.LineHeight, Is.EqualTo(7));
        Assert.That(measurement.Baseline, Is.EqualTo(7));
    }

    [Test]
    public void SplitLines_PreservesEmptyLines()
    {
        string[] lines = ChartBitmapFont.SplitLines("A\n\nB");

        Assert.That(lines, Is.EqualTo(new string[] { "A", "", "B" }));
        Assert.That(ChartBitmapFont.CountLines("A\n\nB"), Is.EqualTo(3));
    }

    [Test]
    public void CreateTextLayout_AppliesHorizontalAndVerticalAlignment()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();

        ChartTextLayout centered = font.CreateTextLayout("A\nABC", 20, 10, 1, ChartTextAlignment.Center, ChartVerticalTextAlignment.Top);
        ChartTextLayout baseline = font.CreateTextLayout("A", 5, 10, 1, ChartTextAlignment.Left, ChartVerticalTextAlignment.Baseline);
        ChartTextLayout bottomRight = font.CreateTextLayout("AB", 20, 20, 1, ChartTextAlignment.Right, ChartVerticalTextAlignment.Bottom);

        Assert.That(centered.Bounds.X, Is.EqualTo(12.0));
        Assert.That(centered.Bounds.Y, Is.EqualTo(10.0));
        Assert.That(centered.Bounds.Width, Is.EqualTo(17.0));
        Assert.That(centered.Bounds.Height, Is.EqualTo(15.0));
        Assert.That(centered.Lines[0].X, Is.EqualTo(18));
        Assert.That(centered.Lines[1].X, Is.EqualTo(12));
        Assert.That(centered.Lines[1].Y, Is.EqualTo(18));
        Assert.That(baseline.Bounds.Y, Is.EqualTo(3.0));
        Assert.That(baseline.Lines[0].BaselineY, Is.EqualTo(10));
        Assert.That(bottomRight.Bounds.X, Is.EqualTo(9.0));
        Assert.That(bottomRight.Bounds.Y, Is.EqualTo(13.0));
    }

    [Test]
    public void PlanRotation_ReturnsRotatedCornersAndBounds()
    {
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
        ChartTextLayout layout = new ChartTextLayout(new ChartTextLayoutLine[] { new ChartTextLayoutLine("A", 10, 20, 4, 27) }, 10, 20, 4, 2);

        ChartTextRotationPlan plan = font.PlanRotation(layout, System.Math.PI * 0.5, 10.0, 20.0);

        Assert.That(plan.AngleRadians, Is.EqualTo(System.Math.PI * 0.5));
        Assert.That(plan.Corners.Length, Is.EqualTo(4));
        Assert.That(plan.RotatedBounds.X, Is.EqualTo(8.0).Within(0.0000001));
        Assert.That(plan.RotatedBounds.Y, Is.EqualTo(20.0).Within(0.0000001));
        Assert.That(plan.RotatedBounds.Width, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(plan.RotatedBounds.Height, Is.EqualTo(4.0).Within(0.0000001));
    }

    [Test]
    public void DrawGlyph_RendersSingleGlyphPixels()
    {
        RawBitmap bitmap = new RawBitmap(8, 9, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.DrawGlyph('A', 1, 1, ChartBitmapFont.FiveBySeven(), 1, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(2, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 1), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(1, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 4), 255, 255, 255, 255);
    }

    [Test]
    public void DrawGlyph_RendersScaledPixels()
    {
        RawBitmap bitmap = new RawBitmap(12, 16, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.DrawGlyph('1', 0, 0, ChartBitmapFont.FiveBySeven(), 2, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(4, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
    }

    [Test]
    public void DrawText_AdvancesGlyphsAndHandlesNewlines()
    {
        RawBitmap bitmap = new RawBitmap(14, 16, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.DrawText("A\nB", 1, 0, ChartBitmapFont.FiveBySeven(), 1, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(2, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(1, 8), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(7, 0), 0, 0, 0, 0);
    }

    [Test]
    public void DrawText_RespectsClipAndBlendMode()
    {
        RawBitmap bitmap = new RawBitmap(8, 8, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        canvas.SetClip(3, 0, 3, 8);
        canvas.DrawText("A", 1, 1, ChartBitmapFont.FiveBySeven(), 1, ChartColor.White(), ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(2, 1), 0, 0, 0, 0);
        AssertColor(bitmap.GetPixel(3, 1), 255, 255, 255, 255);
    }

    [Test]
    public void DrawAlignedText_RendersUsingComputedLayout()
    {
        RawBitmap bitmap = new RawBitmap(12, 12, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

        ChartTextLayout layout = canvas.DrawAlignedText(
            "A",
            5,
            7,
            ChartBitmapFont.FiveBySeven(),
            1,
            ChartTextAlignment.Center,
            ChartVerticalTextAlignment.Baseline,
            ChartColor.White(),
            ChartBlendMode.Source);

        Assert.That(layout.Bounds.X, Is.EqualTo(3.0));
        Assert.That(layout.Bounds.Y, Is.EqualTo(0.0));
        AssertColor(bitmap.GetPixel(4, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 0), 0, 0, 0, 0);
    }

    [Test]
    public void BitmapFontAndTextDrawing_ValidateInputs()
    {
        RawBitmap bitmap = new RawBitmap(4, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => font.GlyphAdvance(0));
        Assert.Throws<System.ArgumentNullException>(() => font.TextWidth(null!, 1));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawText(null!, 0, 0, font, 1, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawText("A", 0, 0, null!, 1, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.DrawText("A", 0, 0, font, 0, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawGlyph('A', 0, 0, font, 1, null!, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => font.PlanRotation(null!, 0.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawTextLayout(null!, font, 1, ChartColor.White(), ChartBlendMode.Source));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
