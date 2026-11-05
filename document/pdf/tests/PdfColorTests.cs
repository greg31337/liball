using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfColorTests
{
    [Test]
    public void Rgb_CreatesRgbColor()
    {
        PdfColor color = PdfColor.Rgb(0.1, 0.2, 0.3);

        Assert.That(color.Mode, Is.EqualTo("RGB"));
        Assert.That(color.First, Is.EqualTo(0.1));
        Assert.That(color.Second, Is.EqualTo(0.2));
        Assert.That(color.Third, Is.EqualTo(0.3));
        Assert.That(color.Fourth, Is.EqualTo(0));
    }

    [Test]
    public void Gray_CreatesGrayColor()
    {
        PdfColor color = PdfColor.Gray(0.4);

        Assert.That(color.Mode, Is.EqualTo("GRAY"));
        Assert.That(color.First, Is.EqualTo(0.4));
        Assert.That(color.Second, Is.EqualTo(0));
        Assert.That(color.Third, Is.EqualTo(0));
        Assert.That(color.Fourth, Is.EqualTo(0));
    }

    [Test]
    public void Cmyk_CreatesCmykColor()
    {
        PdfColor color = PdfColor.Cmyk(0.1, 0.2, 0.3, 0.4);

        Assert.That(color.Mode, Is.EqualTo("CMYK"));
        Assert.That(color.First, Is.EqualTo(0.1));
        Assert.That(color.Second, Is.EqualTo(0.2));
        Assert.That(color.Third, Is.EqualTo(0.3));
        Assert.That(color.Fourth, Is.EqualTo(0.4));
    }

    [Test]
    public void ColorFactories_RejectChannelsOutsideUnitRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColor.Rgb(-0.1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColor.Rgb(0, 1.1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColor.Gray(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColor.Cmyk(0, 0, 0, -0.1));
    }

    [Test]
    public void Canvas_WritesRgbGrayAndCmykColorOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.SetStrokeColorValue(PdfColor.Rgb(0.1, 0.2, 0.3));
        canvas.SetFillColorValue(PdfColor.Rgb(0.4, 0.5, 0.6));
        canvas.SetStrokeColorValue(PdfColor.Gray(0.7));
        canvas.SetFillColorValue(PdfColor.Gray(0.8));
        canvas.SetStrokeColorValue(PdfColor.Cmyk(0.1, 0.2, 0.3, 0.4));
        canvas.SetFillColorValue(PdfColor.Cmyk(0.5, 0.6, 0.7, 0.8));

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("0.1 0.2 0.3 RG"));
        Assert.That(content, Does.Contain("0.4 0.5 0.6 rg"));
        Assert.That(content, Does.Contain("0.7 G"));
        Assert.That(content, Does.Contain("0.8 g"));
        Assert.That(content, Does.Contain("0.1 0.2 0.3 0.4 K"));
        Assert.That(content, Does.Contain("0.5 0.6 0.7 0.8 k"));
    }

    [Test]
    public void Canvas_NumericColorMethodsStillWriteRgbOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.SetStrokeColor(0.2, 0.3, 0.4);
        canvas.SetFillColor(0.5, 0.6, 0.7);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("0.2 0.3 0.4 RG"));
        Assert.That(content, Does.Contain("0.5 0.6 0.7 rg"));
    }

    [Test]
    public void Canvas_NumericColorMethodsUseRgbValidation()
    {
        PdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetStrokeColor(1.1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetFillColor(0, -0.1, 0));
    }
}
