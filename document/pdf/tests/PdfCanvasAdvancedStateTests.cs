using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfCanvasAdvancedStateTests
{
    [Test]
    public void RuleMethodsWriteHorizontalAndVerticalLines()
    {
        IPdfCanvas canvas = new PdfCanvas();

        canvas.DrawHorizontalRule(10, 20, 30);
        canvas.DrawVerticalRule(40, 50, 60);

        Assert.That(canvas.ToPdfString(), Is.EqualTo("10 20 m 40 20 l S\n40 50 m 40 110 l S\n"));
    }

    [Test]
    public void RuleMethodsRejectInvalidDimensionsAndCoordinates()
    {
        IPdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawHorizontalRule(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawVerticalRule(0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawHorizontalRule(double.NaN, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawVerticalRule(0, double.PositiveInfinity, 1));
    }

    [Test]
    public void ExplicitTextObjectWritesTextStateAndMatrixOperators()
    {
        IPdfCanvas canvas = new PdfCanvas();

        canvas.BeginText();
        canvas.SetTextFont(PdfFont.Courier, 12);
        canvas.SetTextCharacterSpacing(0.25);
        canvas.SetTextWordSpacing(1.5);
        canvas.SetTextHorizontalScaling(80);
        canvas.SetTextLeading(14);
        canvas.SetTextRise(3);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.FillStroke);
        canvas.SetTextMatrix(1, 0.2, 0.3, 1, 10, 20);
        canvas.ShowText("First");
        canvas.MoveTextPosition(5, -14);
        canvas.ShowText("Second");
        canvas.MoveTextPositionAndSetLeading(0, -14);
        canvas.MoveToNextTextLine();
        canvas.ShowText("Third");
        canvas.EndText();

        string content = canvas.ToPdfString();
        Assert.That(content, Does.StartWith("BT\n/F5 12 Tf\n"));
        Assert.That(content, Does.Contain("0.25 Tc\n"));
        Assert.That(content, Does.Contain("1.5 Tw\n"));
        Assert.That(content, Does.Contain("80 Tz\n"));
        Assert.That(content, Does.Contain("14 TL\n"));
        Assert.That(content, Does.Contain("3 Ts\n"));
        Assert.That(content, Does.Contain("2 Tr\n"));
        Assert.That(content, Does.Contain("1 0.2 0.3 1 10 20 Tm\n"));
        Assert.That(content, Does.Contain("(First) Tj\n5 -14 Td\n(Second) Tj\n"));
        Assert.That(content, Does.Contain("0 -14 TD\nT*\n(Third) Tj\n"));
        Assert.That(content, Does.EndWith("ET\n"));
    }

    [Test]
    public void EveryTextRenderingModeWritesItsNumericOperator()
    {
        IPdfCanvas canvas = new PdfCanvas();
        canvas.BeginText();

        canvas.SetTextRenderingMode(PdfTextRenderingMode.Fill);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.Stroke);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.FillStroke);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.Invisible);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.FillClip);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.StrokeClip);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.FillStrokeClip);
        canvas.SetTextRenderingMode(PdfTextRenderingMode.Clip);
        canvas.EndText();

        string content = canvas.ToPdfString();
        for (int mode = 0; mode <= 7; mode++)
        {
            Assert.That(content, Does.Contain(mode.ToString() + " Tr"));
        }
    }

    [Test]
    public void TextOperatorsRejectInvalidStateTransitionsAndValues()
    {
        IPdfCanvas canvas = new PdfCanvas();

        Assert.Throws<InvalidOperationException>(() => canvas.EndText());
        Assert.Throws<InvalidOperationException>(() => canvas.SetTextCharacterSpacing(1));
        Assert.Throws<InvalidOperationException>(() => canvas.ShowText("closed"));

        canvas.BeginText();
        Assert.Throws<InvalidOperationException>(() => canvas.BeginText());
        Assert.Throws<InvalidOperationException>(() => canvas.DrawText("nested", 0, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextFont(PdfFont.Regular, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextCharacterSpacing(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextWordSpacing(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextHorizontalScaling(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextLeading(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextRise(double.NegativeInfinity));
        Assert.Throws<ArgumentException>(() => canvas.SetTextRenderingMode((PdfTextRenderingMode)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTextMatrix(1, 0, 0, 1, double.NaN, 0));
        Assert.Throws<ArgumentNullException>(() => canvas.ShowText(null!));
        canvas.EndText();
    }

    [Test]
    public void TransparencyMethodsWriteDeterministicGraphicsStateCommands()
    {
        IPdfCanvas canvas = new PdfCanvas();

        canvas.SetFillOpacity(0.5);
        canvas.SetStrokeOpacity(0.75);
        canvas.SetBlendMode(PdfBlendMode.Multiply);
        canvas.SetTransparency(0.2, 0.4, PdfBlendMode.Screen);

        string content = canvas.ToPdfString();
        Assert.That(content, Is.EqualTo("/GSF0p5 gs\n/GSS0p75 gs\n/GSB1 gs\n/GSF0p2S0p4B2 gs\n"));
    }

    [Test]
    public void TransparencyMethodsValidateOpacityAndBlendMode()
    {
        IPdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetFillOpacity(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetStrokeOpacity(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTransparency(double.NaN, 1, PdfBlendMode.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetTransparency(1, double.PositiveInfinity, PdfBlendMode.Normal));
        Assert.Throws<ArgumentException>(() => canvas.SetBlendMode((PdfBlendMode)99));
        Assert.Throws<ArgumentException>(() => canvas.SetTransparency(1, 1, (PdfBlendMode)99));
    }

    [Test]
    public void WriterSerializesEverySupportedBlendModeName()
    {
        PdfBlendMode[] modes = new PdfBlendMode[]
        {
            PdfBlendMode.Normal,
            PdfBlendMode.Multiply,
            PdfBlendMode.Screen,
            PdfBlendMode.Overlay,
            PdfBlendMode.Darken,
            PdfBlendMode.Lighten,
            PdfBlendMode.ColorDodge,
            PdfBlendMode.ColorBurn,
            PdfBlendMode.HardLight,
            PdfBlendMode.SoftLight,
            PdfBlendMode.Difference,
            PdfBlendMode.Exclusion,
            PdfBlendMode.Hue,
            PdfBlendMode.Saturation,
            PdfBlendMode.Color,
            PdfBlendMode.Luminosity
        };
        string[] names = new string[]
        {
            "Normal",
            "Multiply",
            "Screen",
            "Overlay",
            "Darken",
            "Lighten",
            "ColorDodge",
            "ColorBurn",
            "HardLight",
            "SoftLight",
            "Difference",
            "Exclusion",
            "Hue",
            "Saturation",
            "Color",
            "Luminosity"
        };
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(100, 60);
        for (int index = 0; index < modes.Length; index++)
        {
            page.GetCanvas().SetBlendMode(modes[index]);
        }

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        for (int index = 0; index < names.Length; index++)
        {
            Assert.That(pdf, Does.Contain("/BM /" + names[index]));
        }
    }

    [Test]
    public void WriterSerializesTransparencyResourcesForPagesAndForms()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(100, 60);
        PdfFormXObject form = document.AddForm(20, 10);
        page.GetCanvas().SetFillOpacity(0.5);
        page.GetCanvas().SetStrokeOpacity(0.75);
        page.GetCanvas().SetBlendMode(PdfBlendMode.Multiply);
        page.GetCanvas().SetTransparency(0.2, 0.4, PdfBlendMode.Screen);
        form.Canvas.SetTransparency(0.2, 0.4, PdfBlendMode.Screen);
        page.GetCanvas().DrawForm(form, 10, 10, 20, 10);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("5 0 obj\n<< /Type /ExtGState /ca 0.5 >>"));
        Assert.That(pdf, Does.Contain("6 0 obj\n<< /Type /ExtGState /CA 0.75 >>"));
        Assert.That(pdf, Does.Contain("7 0 obj\n<< /Type /ExtGState /BM /Multiply >>"));
        Assert.That(pdf, Does.Contain("8 0 obj\n<< /Type /ExtGState /ca 0.2 /CA 0.4 /BM /Screen >>"));
        Assert.That(pdf, Does.Contain("/ExtGState << /GSF0p5 5 0 R /GSS0p75 6 0 R /GSB1 7 0 R /GSF0p2S0p4B2 8 0 R >>"));
        Assert.That(Regex.Matches(pdf, "/Type /ExtGState").Count, Is.EqualTo(4));
        Assert.That(Regex.Matches(pdf, "/GSF0p2S0p4B2 8 0 R").Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void AppendedCanvasCarriesTransparencyResourcesIntoDocument()
    {
        PdfCanvas layer = new PdfCanvas();
        layer.SetFillOpacity(0.6);
        layer.FillRectangle(0, 0, 10, 10);
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(20, 20);
        page.GetCanvas().AppendCanvas(layer);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/GSF0p6 5 0 R"));
        Assert.That(pdf, Does.Contain("<< /Type /ExtGState /ca 0.6 >>"));
        Assert.That(pdf, Does.Contain("/GSF0p6 gs\n0 0 10 10 re f"));
    }
}
