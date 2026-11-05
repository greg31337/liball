using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfImageFitTests
{
    [Test]
    public void PreserveAspectFit_LetterboxesImageInsideDestinationBox()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);

        PdfImagePlacement placement = new PdfImageFit().PreserveAspectFit(image, 10, 20, 100, 100);

        Assert.That(placement.X, Is.EqualTo(10).Within(0.0001));
        Assert.That(placement.Y, Is.EqualTo(45).Within(0.0001));
        Assert.That(placement.Width, Is.EqualTo(100).Within(0.0001));
        Assert.That(placement.Height, Is.EqualTo(50).Within(0.0001));
    }

    [Test]
    public void CropFill_CoversDestinationBoxAndCentersOverflow()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);

        PdfImagePlacement placement = new PdfImageFit().CropFill(image, 10, 20, 100, 100);

        Assert.That(placement.X, Is.EqualTo(-40).Within(0.0001));
        Assert.That(placement.Y, Is.EqualTo(20).Within(0.0001));
        Assert.That(placement.Width, Is.EqualTo(200).Within(0.0001));
        Assert.That(placement.Height, Is.EqualTo(100).Within(0.0001));
    }

    [Test]
    public void Stretch_ReturnsDestinationBoxPlacement()
    {
        PdfImagePlacement placement = new PdfImageFit().Stretch(10, 20, 30, 40);

        Assert.That(placement.X, Is.EqualTo(10));
        Assert.That(placement.Y, Is.EqualTo(20));
        Assert.That(placement.Width, Is.EqualTo(30));
        Assert.That(placement.Height, Is.EqualTo(40));
    }

    [Test]
    public void Canvas_DrawImagePreserveAspectFitUsesComputedPlacement()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawImagePreserveAspectFit(image, 10, 20, 100, 100);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("100 0 0 50 10 45 cm"));
        Assert.That(content, Does.Contain("/Im1 Do"));
    }

    [Test]
    public void Canvas_DrawImageCropFillClipsToDestinationBox()
    {
        PdfImage image = new PdfImage("Im1", 200, 100, new byte[200 * 100 * 3]);
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawImageCropFill(image, 10, 20, 100, 100);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 100 100 re"));
        Assert.That(content, Does.Contain("W"));
        Assert.That(content, Does.Contain("n"));
        Assert.That(content, Does.Contain("200 0 0 100 -40 20 cm"));
        Assert.That(content, Does.Contain("/Im1 Do"));
    }

    [Test]
    public void ImageFit_RejectsInvalidInputs()
    {
        PdfImage image = new PdfImage("Im1", 1, 1, new byte[3]);

        Assert.Throws<ArgumentNullException>(() => new PdfImageFit().PreserveAspectFit(null!, 0, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().PreserveAspectFit(image, 0, 0, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().CropFill(image, 0, 0, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().Stretch(0, 0, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().Stretch(0, 0, double.NaN, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().Stretch(0, 0, 10, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().Stretch(double.NaN, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageFit().Stretch(0, double.NegativeInfinity, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImagePlacement(0, 0, double.PositiveInfinity, 10));
    }
}
