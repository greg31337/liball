using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageSizeTests
{
    [Test]
    public void StandardPageSizes_ReturnExpectedPointDimensions()
    {
        AssertDimensions(new PdfPageSizeFactory().Letter(), 612, 792);
        AssertDimensions(new PdfPageSizeFactory().Legal(), 612, 1008);
        AssertDimensions(new PdfPageSizeFactory().A3(), 841.89, 1190.55);
        AssertDimensions(new PdfPageSizeFactory().A4(), 595.28, 841.89);
        AssertDimensions(new PdfPageSizeFactory().A5(), 419.53, 595.28);
        AssertDimensions(new PdfPageSizeFactory().Receipt80(), 226.77, 841.89);
        AssertDimensions(new PdfPageSizeFactory().Receipt58(), 164.41, 841.89);
    }

    [Test]
    public void OrientationHelpers_ReturnPortraitAndLandscapeCopies()
    {
        PdfPageDimensions portrait = new PdfPageSizeFactory().Portrait(new PdfPageSizeFactory().Custom(300, 200));
        PdfPageDimensions landscape = new PdfPageSizeFactory().Landscape(new PdfPageSizeFactory().A4());
        PdfPageDimensions alreadyPortrait = new PdfPageSizeFactory().Portrait(new PdfPageSizeFactory().A5());
        PdfPageDimensions alreadyLandscape = new PdfPageSizeFactory().Landscape(new PdfPageSizeFactory().Custom(400, 200));

        AssertDimensions(portrait, 200, 300);
        AssertDimensions(landscape, 841.89, 595.28);
        AssertDimensions(alreadyPortrait, 419.53, 595.28);
        AssertDimensions(alreadyLandscape, 400, 200);
    }

    [Test]
    public void AddPage_AcceptsPageDimensions()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPageWithDimensions(new PdfPageSizeFactory().Landscape(new PdfPageSizeFactory().A4()));

        Assert.That(page.Width, Is.EqualTo(841.89).Within(0.001));
        Assert.That(page.Height, Is.EqualTo(595.28).Within(0.001));
        Assert.That(document.Pages[0], Is.SameAs(page));
    }

    [Test]
    public void Save_WritesMediaBoxForNamedPageSize()
    {
        PdfDocument document = new PdfDocument();
        document.AddPageWithDimensions(new PdfPageSizeFactory().Landscape(new PdfPageSizeFactory().A4()));

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/MediaBox [0 0 841.89 595.28]"));
    }

    [Test]
    public void PageSizeFactoryRejectsInvalidCustomSizesAndOrientationInputs()
    {
        PdfPageSizeFactory factory = new PdfPageSizeFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Custom(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Custom(100, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageDimensions(double.PositiveInfinity, 100));
        Assert.Throws<ArgumentNullException>(() => factory.Portrait(null!));
        Assert.Throws<ArgumentNullException>(() => factory.Landscape(null!));
    }

    static void AssertDimensions(PdfPageDimensions size, double width, double height)
    {
        Assert.That(size.Width, Is.EqualTo(width).Within(0.001));
        Assert.That(size.Height, Is.EqualTo(height).Within(0.001));
    }
}
