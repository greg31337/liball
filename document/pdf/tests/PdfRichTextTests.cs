using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfRichTextTests
{
    [Test]
    public void TextSpan_StoresTextAndStyleAndMeasuresWidth()
    {
        PdfTextStyle style = PdfTextStyle.Create(12, PdfFont.Courier);
        PdfTextSpan span = new PdfTextSpan("ABC", style);

        Assert.That(span.Text, Is.EqualTo("ABC"));
        Assert.That(span.Style, Is.SameAs(style));
        Assert.That(span.MeasureWidth(), Is.EqualTo(21.6).Within(0.0001));
    }

    [Test]
    public void RichText_AddsSpansAndReturnsPlainText()
    {
        PdfRichText text = new PdfRichText();
        PdfTextSpan span = new PdfTextSpan("world", PdfTextStyle.Bold(10));

        PdfTextSpan first = text.AddSpan("Hello ");
        text.AddExistingSpan(span);

        Assert.That(first.Text, Is.EqualTo("Hello "));
        Assert.That(text.Spans.Count, Is.EqualTo(2));
        Assert.That(text.Spans[1], Is.SameAs(span));
        Assert.That(text.GetText(), Is.EqualTo("Hello world"));
    }

    [Test]
    public void RichText_MeasuresMixedStyleWidth()
    {
        PdfRichText text = new PdfRichText();

        text.AddStyledSpan("ABC", PdfTextStyle.Create(12, PdfFont.Courier));
        text.AddStyledSpan(" W", PdfTextStyle.Create(10, PdfFont.Helvetica));

        Assert.That(text.MeasureWidth(), Is.EqualTo(33.27).Within(0.0001));
        Assert.That(PdfCanvas.MeasureText(text), Is.EqualTo(33.27).Within(0.0001));
    }

    [Test]
    public void RichText_LineHeightUsesTallestSpan()
    {
        PdfRichText text = new PdfRichText();

        text.AddStyledSpan("small", PdfTextStyle.Create(10, PdfFont.Regular));
        text.AddStyledSpan("large", PdfTextStyle.Create(20, PdfFont.Courier));

        Assert.That(text.GetLineHeight(), Is.EqualTo(19.72).Within(0.0001));
    }

    [Test]
    public void Canvas_DrawTextDrawsRichTextSpansInSequence()
    {
        PdfRichText text = new PdfRichText();
        PdfCanvas canvas = new PdfCanvas();

        text.AddStyledSpan("AB", PdfTextStyle.Create(12, PdfFont.Courier, PdfColor.Rgb(0.2, 0.3, 0.4)));
        text.AddStyledSpan("W", PdfTextStyle.Create(10, PdfFont.Helvetica, PdfColor.Gray(0.5)));

        canvas.DrawRichText(text, 10, 20);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("0.2 0.3 0.4 rg"));
        Assert.That(content, Does.Contain("/F5 12 Tf"));
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Contain("(AB) Tj"));
        Assert.That(content, Does.Contain("0.5 g"));
        Assert.That(content, Does.Contain("/F1 10 Tf"));
        Assert.That(content, Does.Contain("24.4 20 Td"));
        Assert.That(content, Does.Contain("(W) Tj"));
        Assert.That(canvas.Fonts.Count, Is.EqualTo(2));
        Assert.That(canvas.Fonts[0], Is.EqualTo(PdfFont.Courier));
        Assert.That(canvas.Fonts[1], Is.EqualTo(PdfFont.Helvetica));
    }

    [Test]
    public void EmptyRichTextMeasuresAsZeroAndDrawsNothing()
    {
        PdfRichText text = new PdfRichText();
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawRichText(text, 10, 20);

        Assert.That(text.GetText(), Is.EqualTo(string.Empty));
        Assert.That(text.MeasureWidth(), Is.EqualTo(0));
        Assert.That(text.GetLineHeight(), Is.EqualTo(0));
        Assert.That(canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void RichText_RejectsInvalidInputs()
    {
        PdfRichText text = new PdfRichText();
        PdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentNullException>(() => PdfTextSpan.Plain(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTextSpan("x", null!));
        Assert.Throws<ArgumentNullException>(() => text.AddExistingSpan((PdfTextSpan)null!));
        Assert.Throws<ArgumentNullException>(() => PdfCanvas.MeasureText(null!));
        Assert.Throws<ArgumentNullException>(() => canvas.DrawRichText(null!, 10, 20));
    }
}
