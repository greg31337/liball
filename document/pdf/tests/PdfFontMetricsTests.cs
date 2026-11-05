using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfFontMetricsTests
{
    [Test]
    public void GetMetrics_ReturnsStandardHelveticaMetrics()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);

        Assert.That(metrics.BaseFontName, Is.EqualTo("Helvetica"));
        Assert.That(metrics.DefaultWidth, Is.EqualTo(500));
        Assert.That(metrics.SpaceWidth, Is.EqualTo(278));
        Assert.That(metrics.Ascent, Is.EqualTo(718));
        Assert.That(metrics.Descent, Is.EqualTo(-207));
        Assert.That(metrics.GetCharacterWidth(' '), Is.EqualTo(278));
        Assert.That(metrics.GetCharacterWidth('1'), Is.EqualTo(556));
        Assert.That(metrics.GetCharacterWidth('i'), Is.EqualTo(278));
        Assert.That(metrics.GetCharacterWidth('M'), Is.EqualTo(889));
    }

    [Test]
    public void GetMetrics_ReturnsFamilySpecificWidths()
    {
        PdfFontMetrics courier = PdfFontMetrics.GetMetrics(PdfFont.Courier);
        PdfFontMetrics times = PdfFontMetrics.GetMetrics(PdfFont.TimesRoman);
        PdfFontMetrics symbol = PdfFontMetrics.GetMetrics(PdfFont.Symbol);
        PdfFontMetrics dingbats = PdfFontMetrics.GetMetrics(PdfFont.ZapfDingbats);

        Assert.That(courier.GetCharacterWidth('A'), Is.EqualTo(600));
        Assert.That(courier.GetCharacterWidth(' '), Is.EqualTo(600));
        Assert.That(times.GetCharacterWidth(' '), Is.EqualTo(250));
        Assert.That(times.GetCharacterWidth('1'), Is.EqualTo(500));
        Assert.That(times.GetCharacterWidth('M'), Is.EqualTo(778));
        Assert.That(symbol.GetCharacterWidth(' '), Is.EqualTo(250));
        Assert.That(dingbats.GetCharacterWidth(' '), Is.EqualTo(278));
    }

    [Test]
    public void MeasureTextWidth_UsesFontSizeAndThousandUnitWidths()
    {
        PdfFontMetrics helvetica = PdfFontMetrics.GetMetrics(PdfFont.Helvetica);
        PdfFontMetrics courier = PdfFontMetrics.GetMetrics(PdfFont.Courier);

        Assert.That(helvetica.MeasureTextWidth("A 1", 10), Is.EqualTo(15.01).Within(0.0001));
        Assert.That(courier.MeasureTextWidth("ABC", 12), Is.EqualTo(21.6).Within(0.0001));
        Assert.That(PdfFontMetrics.MeasureText(PdfFont.TimesRoman, "11", 10), Is.EqualTo(10).Within(0.0001));
    }

    [Test]
    public void MeasureTextWidth_HandlesAggregateWidthsBeyondTheIntegerLimit()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);
        string text = new string('M', 2500000);
        double expected = (long)text.Length * 889 / 1000.0;

        Assert.That(metrics.MeasureTextWidth(text, 1), Is.EqualTo(expected));
    }

    [Test]
    public void GetLineHeight_UsesVerticalMetrics()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);

        Assert.That(metrics.GetLineHeight(10), Is.EqualTo(11.25).Within(0.0001));
    }

    [Test]
    public void GetLineHeight_HandlesExtremePublicMetricValues()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);
        metrics.Ascent = int.MaxValue;
        metrics.Descent = int.MinValue;
        metrics.LineGap = int.MaxValue;

        Assert.That(metrics.GetLineHeight(1), Is.EqualTo(6442450.942).Within(0.001));
    }

    [Test]
    public void GetMetrics_UsesRegistryAliases()
    {
        PdfFontMetrics bold = PdfFontMetrics.GetMetrics(PdfFont.Bold);
        PdfFontMetrics explicitBold = PdfFontMetrics.GetMetrics(PdfFont.HelveticaBold);

        Assert.That(bold.BaseFontName, Is.EqualTo("Helvetica-Bold"));
        Assert.That(explicitBold.BaseFontName, Is.EqualTo("Helvetica-Bold"));
        Assert.That(bold.GetCharacterWidth('W'), Is.EqualTo(explicitBold.GetCharacterWidth('W')));
    }

    [Test]
    public void GetMetrics_ReturnsMetricsForEveryStandardRegistryFont()
    {
        PdfFontRegistry registry = PdfFontRegistry.CreateStandard();

        foreach (PdfStandardFont font in registry.Fonts)
        {
            PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(font.Font);

            Assert.That(metrics.BaseFontName, Is.EqualTo(font.BaseFontName));
            Assert.That(metrics.DefaultWidth, Is.GreaterThan(0));
            Assert.That(metrics.SpaceWidth, Is.GreaterThan(0));
            Assert.That(metrics.MeasureTextWidth("Sample 123", 12), Is.GreaterThan(0));
        }
    }

    [Test]
    public void Metrics_RejectNegativeFontSize()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);

        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.MeasureTextWidth("x", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetLineHeight(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfFontMetrics.MeasureText(PdfFont.Regular, "x", -1));
    }

    [Test]
    public void Constructor_RejectsMissingBaseFontNames()
    {
        Assert.Throws<ArgumentNullException>(() => new PdfFontMetrics(PdfFont.Regular, null!, 500, 278, 718, -207, 718, 523, 200));
        Assert.Throws<ArgumentException>(() => new PdfFontMetrics(PdfFont.Regular, string.Empty, 500, 278, 718, -207, 718, 523, 200));
    }

    [Test]
    public void Metrics_RejectNullTextAndNonFiniteFontSizes()
    {
        PdfFontMetrics metrics = PdfFontMetrics.GetMetrics(PdfFont.Regular);

        Assert.Throws<ArgumentNullException>(() => metrics.MeasureTextWidth(null!, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.MeasureTextWidth("x", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.MeasureTextWidth("x", double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetLineHeight(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetLineHeight(double.NegativeInfinity));
    }
}
