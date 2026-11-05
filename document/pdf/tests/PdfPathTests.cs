using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPathTests
{
    [Test]
    public void PathBuilder_StoresCommandsInOrder()
    {
        PdfPath path = new PdfPath();

        path.MoveTo(1, 2).LineTo(3, 4).CurveTo(5, 6, 7, 8, 9, 10).Close();

        Assert.That(path.Commands.Count, Is.EqualTo(4));
        Assert.That(path.Commands[0].OperatorName, Is.EqualTo("m"));
        Assert.That(path.Commands[1].OperatorName, Is.EqualTo("l"));
        Assert.That(path.Commands[2].OperatorName, Is.EqualTo("c"));
        Assert.That(path.Commands[3].OperatorName, Is.EqualTo("h"));
        Assert.That(path.Commands[2].X3, Is.EqualTo(9));
        Assert.That(path.Commands[2].Y3, Is.EqualTo(10));
    }

    [Test]
    public void Canvas_StrokePathWritesPathCommandsAndStrokeOperator()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfPath path = CreateSamplePath();

        canvas.StrokePath(path);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 m"));
        Assert.That(content, Does.Contain("30 40 l"));
        Assert.That(content, Does.Contain("45 50 55 60 65 70 c"));
        Assert.That(content, Does.Contain("h"));
        Assert.That(content, Does.Contain("S"));
    }

    [Test]
    public void Canvas_FillPathAndFillStrokePathWritePaintOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.FillPath(CreateSamplePath());
        canvas.FillStrokePath(CreateSamplePath());

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("f"));
        Assert.That(content, Does.Contain("B"));
    }

    [Test]
    public void Canvas_PathDrawingRejectsEmptyPaths()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfPath path = new PdfPath();

        Assert.Throws<ArgumentException>(() => canvas.StrokePath(path));
        Assert.Throws<ArgumentException>(() => canvas.FillPath(path));
        Assert.Throws<ArgumentException>(() => canvas.FillStrokePath(path));
    }

    [Test]
    public void Save_WritesPathCommandsToContentStream()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(120, 80);

        page.Canvas.FillStrokePath(CreateSamplePath());

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("10 20 m"));
        Assert.That(pdf, Does.Contain("30 40 l"));
        Assert.That(pdf, Does.Contain("45 50 55 60 65 70 c"));
        Assert.That(pdf, Does.Contain("B"));
    }

    static PdfPath CreateSamplePath()
    {
        PdfPath path = new PdfPath();

        path.MoveTo(10, 20);
        path.LineTo(30, 40);
        path.CurveTo(45, 50, 55, 60, 65, 70);
        path.Close();

        return path;
    }
}
