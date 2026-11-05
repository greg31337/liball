using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfClippingTests
{
    [Test]
    public void ClipPath_WritesPathClipAndEndPathOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.ClipPath(CreateClipPath());

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 10 m"));
        Assert.That(content, Does.Contain("50 10 l"));
        Assert.That(content, Does.Contain("50 40 l"));
        Assert.That(content, Does.Contain("h"));
        Assert.That(content, Does.Contain("W"));
        Assert.That(content, Does.Contain("n"));
    }

    [Test]
    public void ClipPathEvenOdd_WritesEvenOddClipOperator()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.ClipPathEvenOdd(CreateClipPath());

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("W*"));
        Assert.That(content, Does.Contain("n"));
    }

    [Test]
    public void ClipRectangle_WritesRectangleClipOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.ClipRectangle(10, 20, 30, 40);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 30 40 re"));
        Assert.That(content, Does.Contain("W"));
        Assert.That(content, Does.Contain("n"));
    }

    [Test]
    public void Clipping_RejectsEmptyPathsOrInvalidRectangles()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfPath empty = new PdfPath();

        Assert.Throws<ArgumentException>(() => canvas.ClipPath(empty));
        Assert.Throws<ArgumentException>(() => canvas.ClipPathEvenOdd(empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.ClipRectangle(0, 0, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.ClipRectangle(0, 0, 10, 0));
    }

    [Test]
    public void Save_WritesClippingCommandsToContentStream()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(120, 80);

        page.Canvas.SaveState();
        page.Canvas.ClipRectangle(10, 20, 30, 40);
        page.Canvas.FillRectangle(0, 0, 100, 100);
        page.Canvas.RestoreState();

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("q"));
        Assert.That(pdf, Does.Contain("10 20 30 40 re"));
        Assert.That(pdf, Does.Contain("W"));
        Assert.That(pdf, Does.Contain("n"));
        Assert.That(pdf, Does.Contain("0 0 100 100 re f"));
        Assert.That(pdf, Does.Contain("Q"));
    }

    static PdfPath CreateClipPath()
    {
        PdfPath path = new PdfPath();

        path.MoveTo(10, 10);
        path.LineTo(50, 10);
        path.LineTo(50, 40);
        path.Close();

        return path;
    }
}
