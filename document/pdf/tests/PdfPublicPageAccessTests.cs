using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPublicPageAccessTests
{
    [Test]
    public void DocumentInterface_ReturnsPageCountAndIndexedPages()
    {
        IPdfDocument document = new PdfDocument();
        PdfPage first = document.AddPage(200, 100);
        PdfPage second = document.AddPage(300, 150);

        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        Assert.That(document.GetPage(0), Is.SameAs(first));
        Assert.That(document.GetPage(1), Is.SameAs(second));
    }

    [Test]
    public void GetPage_RejectsIndexesOutsideCurrentPageRange()
    {
        IPdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPage(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPage(0));

        document.AddLetterPage();

        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPage(1));
    }

    [Test]
    public void PageInterface_ReturnsCanvasAndDimensions()
    {
        IPdfDocument document = new PdfDocument();
        IPdfPage page = document.AddPage(240, 120);

        PdfCanvas canvas = page.GetCanvas();
        PdfPageDimensions dimensions = page.GetDimensions();
        canvas.DrawText("Public page access", 10, 20, 12);

        Assert.That(dimensions.Width, Is.EqualTo(240));
        Assert.That(dimensions.Height, Is.EqualTo(120));
        Assert.That(Encoding.ASCII.GetString(document.ToBytes()), Does.Contain("(Public page access) Tj"));
    }

    [Test]
    public void GetDimensions_ReturnsAnIndependentValue()
    {
        IPdfPage page = new PdfPage(240, 120);
        PdfPageDimensions dimensions = page.GetDimensions();

        dimensions.Width = 1;
        dimensions.Height = 2;

        PdfPageDimensions currentDimensions = page.GetDimensions();
        Assert.That(currentDimensions.Width, Is.EqualTo(240));
        Assert.That(currentDimensions.Height, Is.EqualTo(120));
    }
}
