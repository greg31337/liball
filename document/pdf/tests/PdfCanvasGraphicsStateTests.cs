using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfCanvasGraphicsStateTests
{
    [Test]
    public void SetLineCapAndJoin_WritePdfGraphicsStateOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.SetLineCap(PdfLineCap.Butt);
        canvas.SetLineCap(PdfLineCap.Round);
        canvas.SetLineCap(PdfLineCap.Square);
        canvas.SetLineJoin(PdfLineJoin.Miter);
        canvas.SetLineJoin(PdfLineJoin.Round);
        canvas.SetLineJoin(PdfLineJoin.Bevel);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("0 J"));
        Assert.That(content, Does.Contain("1 J"));
        Assert.That(content, Does.Contain("2 J"));
        Assert.That(content, Does.Contain("0 j"));
        Assert.That(content, Does.Contain("1 j"));
        Assert.That(content, Does.Contain("2 j"));
    }

    [Test]
    public void SetDashAndClearDash_WriteDashPatternOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.SetDash(new double[] { 3, 1.5, 2 }, 0.5);
        canvas.ClearDash();

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("[3 1.5 2] 0.5 d"));
        Assert.That(content, Does.Contain("[] 0 d"));
    }

    [Test]
    public void SetDash_RejectsNegativePatternValuesOrPhase()
    {
        PdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetDash(new double[] { 2, -1 }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetDash(new double[] { 2, 1 }, -0.1));
    }

    [Test]
    public void TransformMethods_WriteMatrixOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.Translate(10, 20);
        canvas.Scale(2, 3);
        canvas.Rotate(90);
        canvas.Skew(45, 0);
        canvas.Transform(1, 2, 3, 4, 5, 6);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("1 0 0 1 10 20 cm"));
        Assert.That(content, Does.Contain("2 0 0 3 0 0 cm"));
        Assert.That(content, Does.Contain("0 1 -1 0 0 0 cm"));
        Assert.That(content, Does.Contain("1 0 1 1 0 0 cm"));
        Assert.That(content, Does.Contain("1 2 3 4 5 6 cm"));
    }
}
