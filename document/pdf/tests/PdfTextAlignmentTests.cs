using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTextAlignmentTests
{
    [Test]
    public void DrawText_WithCenterAlignmentOffsetsTextWithinWidth()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(12, PdfFont.Courier);

        canvas.DrawAlignedStyledText("ABC", 10, 20, 60, style, PdfTextAlignment.Center);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("/F5 12 Tf"));
        Assert.That(content, Does.Contain("29.2 20 Td"));
        Assert.That(content, Does.Contain("(ABC) Tj"));
    }

    [Test]
    public void DrawText_WithRightAlignmentOffsetsTextWithinWidth()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(12, PdfFont.Courier);

        canvas.DrawAlignedStyledText("ABC", 10, 20, 60, style, PdfTextAlignment.Right);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("48.4 20 Td"));
        Assert.That(content, Does.Contain("(ABC) Tj"));
    }

    [Test]
    public void DrawText_WithLeftAlignmentKeepsOriginalX()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(12, PdfFont.Courier);

        canvas.DrawAlignedStyledText("ABC", 10, 20, 60, style, PdfTextAlignment.Left);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Not.Contain("Tw"));
    }

    [Test]
    public void DrawText_WithJustifyAlignmentUsesWordSpacing()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(10, PdfFont.Helvetica);

        canvas.DrawAlignedStyledText("A B", 10, 20, 40, style, PdfTextAlignment.Justify);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("/F1 10 Tf"));
        Assert.That(content, Does.Contain("23.88 Tw"));
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Contain("(A B) Tj"));
        Assert.That(content, Does.Contain("0 Tw"));
    }

    [Test]
    public void DrawText_WithJustifyAlignmentWithoutSpacesDrawsLeftAlignedText()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(10, PdfFont.Helvetica);

        canvas.DrawAlignedStyledText("ABC", 10, 20, 80, style, PdfTextAlignment.Justify);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Not.Contain("Tw"));
    }

    [Test]
    public void DrawText_UsesParagraphStyleAlignment()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle textStyle = PdfTextStyle.Create(12, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 15, 0, 0, 0, PdfTextAlignment.Right);

        canvas.DrawParagraphText("ABC", 10, 20, 60, paragraphStyle);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("48.4 20 Td"));
    }

    [Test]
    public void DrawText_AlignmentOverloadsRejectInvalidInputs()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = new PdfTextStyle();

        Assert.Throws<ArgumentNullException>(() => canvas.DrawAlignedStyledText("x", 0, 0, 100, null!, PdfTextAlignment.Left));
        Assert.Throws<ArgumentNullException>(() => canvas.DrawParagraphText("x", 0, 0, 100, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.DrawAlignedStyledText("x", 0, 0, 0, style, PdfTextAlignment.Left));
        Assert.Throws<ArgumentException>(() => canvas.DrawAlignedStyledText("x", 0, 0, 100, style, (PdfTextAlignment)99));
    }
}
