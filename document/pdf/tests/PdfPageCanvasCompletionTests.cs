using System.Globalization;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageCanvasCompletionTests
{
    [Test]
    public void GraphicsStateOperationsTrackNestedBalanceAndRejectUnderflow()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.SaveState();
        canvas.SaveState();
        canvas.RestoreState();
        canvas.RestoreState();

        Assert.That(canvas.ToPdfString(), Is.EqualTo("q\nq\nQ\nQ\n"));
        Assert.Throws<InvalidOperationException>(() => canvas.RestoreState());
    }

    [Test]
    public void SerializationRejectsUnclosedPageAndFormCanvasScopes()
    {
        PdfDocument graphicsDocument = CreateDocument();
        graphicsDocument.GetPage(0).GetCanvas().SaveState();
        Assert.Throws<InvalidOperationException>(() => graphicsDocument.ToBytes());
        graphicsDocument.GetPage(0).GetCanvas().RestoreState();
        Assert.That(graphicsDocument.ToBytes().Length, Is.GreaterThan(0));

        PdfDocument textDocument = CreateDocument();
        textDocument.GetPage(0).GetCanvas().BeginText();
        Assert.Throws<InvalidOperationException>(() => textDocument.ToBytes());

        PdfDocument artifactDocument = CreateDocument();
        artifactDocument.GetPage(0).GetCanvas().BeginArtifact(PdfArtifactType.Layout);
        Assert.Throws<InvalidOperationException>(() => artifactDocument.ToBytes());
        artifactDocument.GetPage(0).GetCanvas().EndMarkedContent();
        Assert.That(artifactDocument.ToBytes().Length, Is.GreaterThan(0));

        PdfDocument formDocument = CreateDocument();
        PdfFormXObject form = formDocument.AddForm(20, 20);
        form.Canvas.SaveState();
        Assert.Throws<InvalidOperationException>(() => formDocument.ToBytes());
    }

    [Test]
    public void CanvasCompositionRejectsIncompleteSourceOrDestinationState()
    {
        PdfCanvas destination = new PdfCanvas();
        PdfCanvas source = new PdfCanvas();
        source.SaveState();

        Assert.Throws<InvalidOperationException>(() => destination.AppendCanvas(source));
        source.RestoreState();
        destination.BeginText();
        Assert.Throws<InvalidOperationException>(() => destination.PrependCanvas(source));
        destination.EndText();

        destination.AppendCanvas(source);
        Assert.That(destination.ToPdfString(), Is.EqualTo("BT\nET\nq\nQ\n"));
    }

    [Test]
    public void GraphicsStateSettersRejectInvalidValuesWithoutPartialCommands()
    {
        PdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetLineWidth(0));
        Assert.Throws<ArgumentException>(() => canvas.SetLineCap((PdfLineCap)99));
        Assert.Throws<ArgumentException>(() => canvas.SetLineJoin((PdfLineJoin)99));
        Assert.Throws<ArgumentNullException>(() => canvas.SetDash(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetDash(new double[] { 2, double.NaN }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetDash(new double[] { 2 }, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => canvas.SetStrokeColorValue(null!));
        Assert.Throws<ArgumentNullException>(() => canvas.SetFillColorValue(null!));

        Assert.That(canvas.ToPdfString(), Is.Empty);
    }

    [Test]
    public void DrawingOperationsRejectNonFiniteCoordinatesAndDimensions()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.WithFont(10, PdfFont.Regular);
        PdfDocument document = CreateDocument();
        PdfImage image = document.AddImage(1, 1, new byte[] { 1, 2, 3 });

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.Transform(1, 0, 0, 1, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.Rotate(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawLine(0, 0, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.StrokeRectangle(0, 0, double.PositiveInfinity, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.FillRectangle(double.NaN, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.ClipRectangle(0, double.NaN, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawAlignedStyledText("x", 0, 0, double.NaN, style, PdfTextAlignment.Left));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawImage(image, double.NaN, 0, 10, 10));
    }

    [Test]
    public void PathsAndFormsRejectInvalidConstructionAndPlacement()
    {
        PdfPath path = new PdfPath();
        PdfCanvas canvas = new PdfCanvas();
        PdfFormXObject form = new PdfFormXObject("FmCustom", 20, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => path.MoveTo(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => path.LineTo(0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => path.CurveTo(0, 0, 1, 1, double.NaN, 2));
        Assert.Throws<ArgumentNullException>(() => new PdfFormXObject(null!, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfFormXObject("Fm", 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfFormXObject("Fm", 10, double.NaN));
        Assert.Throws<ArgumentNullException>(() => canvas.DrawForm(null!, 0, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawForm(form, 0, 0, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawForm(form, double.NaN, 0, 10, 10));
    }

    [Test]
    public void PageMarginsLinksAndAnnotationsRejectNonFiniteGeometry()
    {
        PdfPage page = new PdfPage(200, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(double.NaN, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMargins(0, 0, double.PositiveInfinity, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddInternalLink("target", double.NaN, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddExternalLink("https://example.com", 0, 0, double.PositiveInfinity, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddTextAnnotation("note", 0, double.NaN, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddSquareAnnotation("note", 0, 0, 10, double.PositiveInfinity));
        Assert.That(page.InternalLinks.Count, Is.EqualTo(0));
        Assert.That(page.ExternalLinks.Count, Is.EqualTo(0));
        Assert.That(page.Annotations.Count, Is.EqualTo(0));
    }

    [Test]
    public void NumericOperatorsUseInvariantCompactFormatting()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            PdfCanvas canvas = new PdfCanvas();
            canvas.SetLineWidth(1.25);
            canvas.Transform(1, 0, 0, 1, 2.5, 3.75);
            canvas.StrokeRectangle(0.125, 1.5, 20.25, 10.5);

            string content = canvas.ToPdfString();
            Assert.That(content, Does.Contain("1.25 w"));
            Assert.That(content, Does.Contain("1 0 0 1 2.5 3.75 cm"));
            Assert.That(content, Does.Contain("0.125 1.5 20.25 10.5 re S"));
            Assert.That(content, Does.Not.Contain(","));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    static PdfDocument CreateDocument()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 100);
        return document;
    }
}
