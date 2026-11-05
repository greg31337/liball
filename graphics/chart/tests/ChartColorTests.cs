namespace chart.Tests;

[TestFixture]
public class ChartColorTests
{
    [Test]
    public void Parse_ReadsHexColors()
    {
        AssertColor(ChartColor.Parse("#1a2b3c"), 26, 43, 60, 255);
        AssertColor(ChartColor.Parse("#1a2b3c80"), 26, 43, 60, 128);
        AssertColor(ChartColor.Parse("#abc"), 170, 187, 204, 255);
        AssertColor(ChartColor.Parse("#abcd"), 170, 187, 204, 221);
    }

    [Test]
    public void Parse_ReadsNamedColorsCaseInsensitively()
    {
        AssertColor(ChartColor.Parse("WHITE"), 255, 255, 255, 255);
        AssertColor(ChartColor.Parse(" transparent "), 0, 0, 0, 0);
        AssertColor(ChartColor.Parse("gray"), 128, 128, 128, 255);
        AssertColor(ChartColor.Parse("grey"), 128, 128, 128, 255);
    }

    [Test]
    public void Parse_ReadsRgbAndRgbaFunctions()
    {
        AssertColor(ChartColor.Parse("rgb(12, 34, 56)"), 12, 34, 56, 255);
        AssertColor(ChartColor.Parse("rgb(100%, 50%, 0%)"), 255, 128, 0, 255);
        AssertColor(ChartColor.Parse("rgba(12, 34, 56, 0.5)"), 12, 34, 56, 128);
        AssertColor(ChartColor.Parse("rgba(12, 34, 56, 25%)"), 12, 34, 56, 64);
    }

    [Test]
    public void TryParse_ReturnsNullForInvalidColorText()
    {
        Assert.That(ChartColor.TryParse(null), Is.Null);
        Assert.That(ChartColor.TryParse(""), Is.Null);
        Assert.That(ChartColor.TryParse("not-a-color"), Is.Null);
        Assert.That(ChartColor.TryParse("#12"), Is.Null);
        Assert.That(ChartColor.TryParse("#zzzzzz"), Is.Null);
        Assert.That(ChartColor.TryParse("rgb(1, 2)"), Is.Null);
        Assert.That(ChartColor.TryParse("rgb(1, 2, 300)"), Is.Null);
        Assert.That(ChartColor.TryParse("rgba(1, 2, 3, 1.5)"), Is.Null);
    }

    [Test]
    public void AlphaHelpers_AdjustAndConvertAlpha()
    {
        ChartColor color = new ChartColor(100, 50, 25, 128);
        ChartColor halfOpacity = color.WithOpacity(0.5);
        ChartColor transparent = color.WithOpacity(-1.0);
        ChartColor opaque = color.WithOpacity(2.0);
        ChartColor premultiplied = color.PremultiplyAlpha();
        ChartColor unpremultiplied = premultiplied.UnpremultiplyAlpha();

        AssertColor(color.WithAlpha(64), 100, 50, 25, 64);
        AssertColor(halfOpacity, 100, 50, 25, 128);
        AssertColor(transparent, 100, 50, 25, 0);
        AssertColor(opaque, 100, 50, 25, 255);
        AssertColor(premultiplied, 50, 25, 13, 128);
        AssertColor(unpremultiplied, 100, 50, 26, 128);
        Assert.That(ChartColor.Transparent().IsTransparent(), Is.True);
        Assert.That(ChartColor.White().IsOpaque(), Is.True);
    }

    [Test]
    public void Lerp_InterpolatesChannelsAndClampsAmount()
    {
        ChartColor start = new ChartColor(0, 20, 100, 40);
        ChartColor end = new ChartColor(100, 220, 200, 240);

        AssertColor(ChartColor.Lerp(start, end, 0.25), 25, 70, 125, 90);
        AssertColor(ChartColor.Lerp(start, end, -1.0), 0, 20, 100, 40);
        AssertColor(ChartColor.Lerp(start, end, 2.0), 100, 220, 200, 240);
    }

    [Test]
    public void HexFormatting_UsesUppercaseDeterministicOutput()
    {
        ChartColor color = new ChartColor(10, 11, 12, 13);

        Assert.That(color.ToHexRgb(), Is.EqualTo("#0A0B0C"));
        Assert.That(color.ToHexRgba(), Is.EqualTo("#0A0B0C0D"));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
