using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTextWrapperTests
{
    [Test]
    public void WrapText_BreaksTextAtWordBoundaries()
    {
        string[] lines = PdfTextWrapper.WrapText("one two three four", 46, 10, PdfFont.Courier);

        Assert.That(lines, Is.EqualTo(new string[] { "one two", "three", "four" }));
    }

    [Test]
    public void WrapText_SplitsWordsThatAreTooWide()
    {
        string[] lines = PdfTextWrapper.WrapText("abcdef", 18, 10, PdfFont.Courier);

        Assert.That(lines, Is.EqualTo(new string[] { "abc", "def" }));
    }

    [Test]
    public void WrapText_PreservesExplicitLineBreaksAndBlankLines()
    {
        string[] lines = PdfTextWrapper.WrapText("alpha\n\nbeta", 200, 10, PdfFont.Regular);

        Assert.That(lines, Is.EqualTo(new string[] { "alpha", string.Empty, "beta" }));
    }

    [Test]
    public void WrapText_UsesSelectedFontMetrics()
    {
        string[] helveticaLines = PdfTextWrapper.WrapText("WWW", 25, 10, PdfFont.Helvetica);
        string[] timesLines = PdfTextWrapper.WrapText("WWW", 25, 10, PdfFont.TimesRoman);

        Assert.That(helveticaLines, Is.EqualTo(new string[] { "WW", "W" }));
        Assert.That(timesLines, Is.EqualTo(new string[] { "WWW" }));
    }

    [Test]
    public void InstanceWrapper_ReusesConfiguredSettings()
    {
        PdfTextWrapper wrapper = new PdfTextWrapper(46, 10, PdfFont.Courier);

        string[] lines = wrapper.Wrap("one two three");

        Assert.That(wrapper.MaxWidth, Is.EqualTo(46));
        Assert.That(wrapper.FontSize, Is.EqualTo(10));
        Assert.That(wrapper.Font, Is.EqualTo(PdfFont.Courier));
        Assert.That(lines, Is.EqualTo(new string[] { "one two", "three" }));
    }

    [Test]
    public void WrapText_ReturnsNoLinesForWhitespaceOnlyText()
    {
        string[] lines = PdfTextWrapper.WrapText("   \t  ", 100, 10);

        Assert.That(lines, Is.Empty);
    }

    [Test]
    public void WrapText_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PdfTextWrapper.WrapText(null!, 100, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", double.NaN, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", double.PositiveInfinity, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", 100, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", 100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", 100, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.WrapText("x", 100, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextWrapper.Regular(0, 10));
    }
}
