using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageLayoutTests
{
    [Test]
    public void NewPage_HasZeroMarginsAndFullContentArea()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfRectangle content = page.GetContentArea();

        Assert.That(page.Margins.Left, Is.EqualTo(0));
        Assert.That(page.Margins.Top, Is.EqualTo(0));
        Assert.That(page.Margins.Right, Is.EqualTo(0));
        Assert.That(page.Margins.Bottom, Is.EqualTo(0));
        Assert.That(content.X, Is.EqualTo(0));
        Assert.That(content.Y, Is.EqualTo(0));
        Assert.That(content.Width, Is.EqualTo(200));
        Assert.That(content.Height, Is.EqualTo(100));
    }

    [Test]
    public void SetMargins_ComputesContentEdgesAndArea()
    {
        PdfPage page = new PdfPage(612, 792);

        page.SetMargins(72, 36, 54, 48);
        PdfRectangle content = page.GetContentArea();

        Assert.That(page.GetContentLeft(), Is.EqualTo(72));
        Assert.That(page.GetContentRight(), Is.EqualTo(558));
        Assert.That(page.GetContentTop(), Is.EqualTo(756));
        Assert.That(page.GetContentBottom(), Is.EqualTo(48));
        Assert.That(page.GetContentWidth(), Is.EqualTo(486));
        Assert.That(page.GetContentHeight(), Is.EqualTo(708));
        Assert.That(content.X, Is.EqualTo(72));
        Assert.That(content.Y, Is.EqualTo(48));
        Assert.That(content.Width, Is.EqualTo(486));
        Assert.That(content.Height, Is.EqualTo(708));
    }

    [Test]
    public void SetUniformMargins_AppliesSameMarginToEverySide()
    {
        PdfPage page = new PdfPage(200, 120);

        page.SetUniformMargins(10);

        Assert.That(page.Margins.Left, Is.EqualTo(10));
        Assert.That(page.Margins.Top, Is.EqualTo(10));
        Assert.That(page.Margins.Right, Is.EqualTo(10));
        Assert.That(page.Margins.Bottom, Is.EqualTo(10));
        Assert.That(page.GetContentWidth(), Is.EqualTo(180));
        Assert.That(page.GetContentHeight(), Is.EqualTo(100));
    }

    [Test]
    public void SetMargins_RejectsNegativeOrOverfullMargins()
    {
        PdfPage page = new PdfPage(100, 80);

        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(0, 0, 0, -1));
        Assert.Throws<ArgumentException>(() => page.SetMargins(50, 0, 50, 0));
        Assert.Throws<ArgumentException>(() => page.SetMargins(0, 40, 0, 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageMargins(double.NaN, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageMargins(0, double.PositiveInfinity, 0, 0));
    }

    [Test]
    public void Margins_DoNotChangeSerializedPageSize()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.SetMargins(20, 10, 20, 10);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/MediaBox [0 0 200 100]"));
    }
}
