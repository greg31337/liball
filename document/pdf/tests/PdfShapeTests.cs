using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfShapeTests
{
    [Test]
    public void Circle_CreatesClosedBezierPath()
    {
        PdfPath path = PdfPath.Circle(50, 60, 10);

        Assert.That(path.Commands.Count, Is.EqualTo(6));
        Assert.That(path.Commands[0].OperatorName, Is.EqualTo("m"));
        Assert.That(path.Commands[0].X1, Is.EqualTo(60));
        Assert.That(path.Commands[0].Y1, Is.EqualTo(60));
        Assert.That(path.Commands[1].OperatorName, Is.EqualTo("c"));
        Assert.That(path.Commands[1].X1, Is.EqualTo(60));
        Assert.That(path.Commands[1].Y1, Is.EqualTo(65.523).Within(0.001));
        Assert.That(path.Commands[4].OperatorName, Is.EqualTo("c"));
        Assert.That(path.Commands[5].OperatorName, Is.EqualTo("h"));
    }

    [Test]
    public void Ellipse_CreatesClosedBezierPath()
    {
        PdfPath path = PdfPath.Ellipse(50, 60, 20, 10);

        Assert.That(path.Commands.Count, Is.EqualTo(6));
        Assert.That(path.Commands[0].X1, Is.EqualTo(70));
        Assert.That(path.Commands[0].Y1, Is.EqualTo(60));
        Assert.That(path.Commands[1].Y1, Is.EqualTo(65.523).Within(0.001));
        Assert.That(path.Commands[1].X2, Is.EqualTo(61.046).Within(0.001));
        Assert.That(path.Commands[5].OperatorName, Is.EqualTo("h"));
    }

    [Test]
    public void RoundedRectangle_CreatesClosedPathWithLinesAndCurves()
    {
        PdfPath path = PdfPath.RoundedRectangle(10, 20, 100, 50, 8);

        Assert.That(path.Commands.Count, Is.EqualTo(10));
        Assert.That(path.Commands[0].OperatorName, Is.EqualTo("m"));
        Assert.That(path.Commands[0].X1, Is.EqualTo(18));
        Assert.That(path.Commands[0].Y1, Is.EqualTo(20));
        Assert.That(path.Commands[1].OperatorName, Is.EqualTo("l"));
        Assert.That(path.Commands[2].OperatorName, Is.EqualTo("c"));
        Assert.That(path.Commands[9].OperatorName, Is.EqualTo("h"));
    }

    [Test]
    public void RoundedRectangle_ClampsRadiusToHalfSmallestSide()
    {
        PdfPath path = PdfPath.RoundedRectangle(0, 0, 30, 10, 99);

        Assert.That(path.Commands[0].X1, Is.EqualTo(5));
        Assert.That(path.Commands[1].X1, Is.EqualTo(25));
    }

    [Test]
    public void ShapeFactories_ValidateDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.Circle(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.Ellipse(0, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.Ellipse(0, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.RoundedRectangle(0, 0, 0, 10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.RoundedRectangle(0, 0, 10, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPath.RoundedRectangle(0, 0, 10, 10, -1));
    }

    [Test]
    public void Canvas_ShapeHelpersWritePaintOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.StrokeCircle(20, 20, 5);
        canvas.FillEllipse(30, 30, 10, 5);
        canvas.FillStrokeRoundedRectangle(10, 10, 40, 20, 4);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("25 20 m"));
        Assert.That(content, Does.Contain("40 30 m"));
        Assert.That(content, Does.Contain("14 10 m"));
        Assert.That(content, Does.Contain("S"));
        Assert.That(content, Does.Contain("f"));
        Assert.That(content, Does.Contain("B"));
    }

    [Test]
    public void Save_WritesShapeCommandsToContentStream()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(120, 80);

        page.Canvas.StrokeCircle(20, 20, 5);
        page.Canvas.FillRoundedRectangle(10, 10, 40, 20, 4);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("25 20 m"));
        Assert.That(pdf, Does.Contain("14 10 m"));
        Assert.That(pdf, Does.Contain("c"));
        Assert.That(pdf, Does.Contain("f"));
    }
}
