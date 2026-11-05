using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTextStyleTests
{
    [Test]
    public void TextStyle_DefaultsToRegularBlackTwelvePointText()
    {
        PdfTextStyle style = new PdfTextStyle();

        Assert.That(style.Font, Is.EqualTo(PdfFont.Regular));
        Assert.That(style.FontSize, Is.EqualTo(12));
        Assert.That(style.FillColor.Mode, Is.EqualTo("RGB"));
        Assert.That(style.FillColor.First, Is.EqualTo(0));
        Assert.That(style.FillColor.Second, Is.EqualTo(0));
        Assert.That(style.FillColor.Third, Is.EqualTo(0));
    }

    [Test]
    public void TextStyle_FactoriesCreateCommonStyles()
    {
        PdfTextStyle regular = PdfTextStyle.Regular(10);
        PdfTextStyle bold = PdfTextStyle.Bold(14);
        PdfTextStyle colored = PdfTextStyle.WithColor(9, PdfFont.Courier, PdfColor.Gray(0.4));

        Assert.That(regular.Font, Is.EqualTo(PdfFont.Regular));
        Assert.That(regular.FontSize, Is.EqualTo(10));
        Assert.That(bold.Font, Is.EqualTo(PdfFont.Bold));
        Assert.That(bold.FontSize, Is.EqualTo(14));
        Assert.That(colored.Font, Is.EqualTo(PdfFont.Courier));
        Assert.That(colored.FontSize, Is.EqualTo(9));
        Assert.That(colored.FillColor.Mode, Is.EqualTo("GRAY"));
        Assert.That(colored.FillColor.First, Is.EqualTo(0.4));
    }

    [Test]
    public void Canvas_DrawTextWithStyleUsesFontSizeFontAndFillColor()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfTextStyle style = PdfTextStyle.Create(11, PdfFont.Courier, PdfColor.Rgb(0.2, 0.3, 0.4));

        canvas.DrawStyledText("Styled", 10, 20, style);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("0.2 0.3 0.4 rg"));
        Assert.That(content, Does.Contain("/F5 11 Tf"));
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Contain("(Styled) Tj"));
        Assert.That(canvas.Fonts.Count, Is.EqualTo(1));
        Assert.That(canvas.Fonts[0], Is.EqualTo(PdfFont.Courier));
    }

    [Test]
    public void MeasureText_UsesTextStyleFontAndSize()
    {
        PdfTextStyle style = PdfTextStyle.Create(12, PdfFont.Courier);

        Assert.That(PdfCanvas.MeasureText("ABC", style), Is.EqualTo(21.6).Within(0.0001));
    }

    [Test]
    public void TextWrapper_UsesTextStyleFontAndSize()
    {
        PdfTextStyle style = PdfTextStyle.Create(10, PdfFont.TimesRoman);

        string[] lines = PdfTextWrapper.WrapText("WWW", 25, style);
        PdfTextWrapper wrapper = PdfTextWrapper.FromStyle(25, style);

        Assert.That(lines, Is.EqualTo(new string[] { "WWW" }));
        Assert.That(wrapper.Font, Is.EqualTo(PdfFont.TimesRoman));
        Assert.That(wrapper.FontSize, Is.EqualTo(10));
        Assert.That(wrapper.Wrap("WWW"), Is.EqualTo(new string[] { "WWW" }));
    }

    [Test]
    public void ParagraphStyle_DefaultsAroundDefaultTextStyle()
    {
        PdfParagraphStyle style = new PdfParagraphStyle();

        Assert.That(style.TextStyle.Font, Is.EqualTo(PdfFont.Regular));
        Assert.That(style.TextStyle.FontSize, Is.EqualTo(12));
        Assert.That(style.LineHeight, Is.EqualTo(14.4).Within(0.0001));
        Assert.That(style.SpacingBefore, Is.EqualTo(0));
        Assert.That(style.SpacingAfter, Is.EqualTo(0));
        Assert.That(style.FirstLineIndent, Is.EqualTo(0));
        Assert.That(style.Alignment, Is.EqualTo(PdfTextAlignment.Left));
    }

    [Test]
    public void ParagraphStyle_CanUseConfiguredTextStyleAndSpacing()
    {
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.TimesRoman, PdfColor.Gray(0.2));
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 15, 3, 4, 8, PdfTextAlignment.Center);

        Assert.That(paragraphStyle.TextStyle, Is.SameAs(textStyle));
        Assert.That(paragraphStyle.LineHeight, Is.EqualTo(15));
        Assert.That(paragraphStyle.SpacingBefore, Is.EqualTo(3));
        Assert.That(paragraphStyle.SpacingAfter, Is.EqualTo(4));
        Assert.That(paragraphStyle.FirstLineIndent, Is.EqualTo(8));
        Assert.That(paragraphStyle.Alignment, Is.EqualTo(PdfTextAlignment.Center));
    }

    [Test]
    public void Styles_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextStyle.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextStyle.Create(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfTextStyle.Create(double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => PdfTextStyle.Create(10, PdfFont.Regular, null!));
        Assert.Throws<ArgumentNullException>(() => PdfCanvas.MeasureText("x", null!));
        Assert.Throws<ArgumentNullException>(() => PdfTextWrapper.WrapText("x", 100, null!));
        Assert.Throws<ArgumentNullException>(() => PdfParagraphStyle.Create(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, 0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), double.NaN, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, double.PositiveInfinity, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, 0, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, 0, 0, double.NegativeInfinity));
        Assert.Throws<ArgumentException>(() => PdfParagraphStyle.Create(new PdfTextStyle(), 12, 0, 0, 0, (PdfTextAlignment)99));
    }
}
