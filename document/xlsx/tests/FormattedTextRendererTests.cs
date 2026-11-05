using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormattedTextRendererTests
{
    [Test]
    public void RenderNumber_FormatsGeneralDecimalsThousandsPercentAndNegativeSections()
    {
        XlsxFormattedTextRenderResult general = XlsxFormattedTextRenderer.RenderNumber(12.5d, "General", false);
        XlsxFormattedTextRenderResult fixedNumber = XlsxFormattedTextRenderer.RenderNumber(1234.5d, "#,##0.00", false);
        XlsxFormattedTextRenderResult percent = XlsxFormattedTextRenderer.RenderNumber(0.125d, "0.0%", false);
        XlsxFormattedTextRenderResult negative = XlsxFormattedTextRenderer.RenderNumber(-1234.5d, "#,##0.00;(#,##0.00)", false);
        XlsxFormattedTextRenderResult zeroLiteral = XlsxFormattedTextRenderer.RenderNumber(0d, "#,##0.00;(#,##0.00);\"-\"", false);

        Assert.That(general.IsValid, Is.True);
        Assert.That(general.Text, Is.EqualTo("12.5"));
        Assert.That(fixedNumber.Text, Is.EqualTo("1,234.50"));
        Assert.That(percent.Text, Is.EqualTo("12.5%"));
        Assert.That(negative.Text, Is.EqualTo("(1,234.50)"));
        Assert.That(zeroLiteral.Text, Is.EqualTo("-"));
    }

    [Test]
    public void RenderNumber_FormatsDateSerialsIn1900And1904Systems()
    {
        XlsxFormattedTextRenderResult date1900 = XlsxFormattedTextRenderer.RenderNumber(61d, "m/d/yy", false);
        XlsxFormattedTextRenderResult time1900 = XlsxFormattedTextRenderer.RenderNumber(2.5d, "h:mm", false);
        XlsxFormattedTextRenderResult ampm = XlsxFormattedTextRenderer.RenderNumber(2.75d, "h:mm AM/PM", false);
        XlsxFormattedTextRenderResult date1904 = XlsxFormattedTextRenderer.RenderNumber(1.25d, "m/d/yy h:mm", true);

        Assert.That(date1900.IsValid, Is.True);
        Assert.That(date1900.Text, Is.EqualTo("3/1/00"));
        Assert.That(time1900.IsValid, Is.True);
        Assert.That(time1900.Text, Is.EqualTo("12:00"));
        Assert.That(ampm.IsValid, Is.True);
        Assert.That(ampm.Text, Is.EqualTo("6:00 PM"));
        Assert.That(date1904.IsValid, Is.True);
        Assert.That(date1904.Text, Is.EqualTo("1/2/04 6:00"));
    }

    [Test]
    public void RenderDateTime_FormatsDateTimeValues()
    {
        DateTime value = new DateTime(2026, 8, 9, 14, 5, 6);

        XlsxFormattedTextRenderResult date = XlsxFormattedTextRenderer.RenderDateTime(value, "m/d/yy");
        XlsxFormattedTextRenderResult time = XlsxFormattedTextRenderer.RenderDateTime(value, "h:mm:ss AM/PM");

        Assert.That(date.IsValid, Is.True);
        Assert.That(date.Text, Is.EqualTo("8/9/26"));
        Assert.That(time.IsValid, Is.True);
        Assert.That(time.Text, Is.EqualTo("2:05:06 PM"));
    }

    [Test]
    public void RenderCellValue_FormatsBooleansErrorsTextAndBlank()
    {
        XlsxFormattedTextRenderResult blank = XlsxFormattedTextRenderer.RenderCellValue(XlsxCellValue.CreateBlank(), "General", false);
        XlsxFormattedTextRenderResult booleanTrue = XlsxFormattedTextRenderer.RenderCellValue(XlsxCellValue.CreateBoolean(true), "General", false);
        XlsxFormattedTextRenderResult error = XlsxFormattedTextRenderer.RenderCellValue(XlsxCellValue.CreateError("#DIV/0!"), "General", false);
        XlsxFormattedTextRenderResult text = XlsxFormattedTextRenderer.RenderCellValue(XlsxCellValue.CreateText("Ada"), "0;0;0;\"Name: \"@", false);

        Assert.That(blank.Text, Is.EqualTo(""));
        Assert.That(booleanTrue.Text, Is.EqualTo("TRUE"));
        Assert.That(error.Text, Is.EqualTo("#DIV/0!"));
        Assert.That(text.Text, Is.EqualTo("Name: Ada"));
    }

    [Test]
    public void RenderCellValue_ReportsInvalidInputs()
    {
        XlsxFormattedTextRenderResult? nullValue = XlsxFormattedTextRenderer.RenderCellValue(null!, "General", false);
        XlsxFormattedTextRenderResult badFormat = XlsxFormattedTextRenderer.RenderNumber(1d, "0\"bad", false);
        XlsxFormattedTextRenderResult badDateSerial = XlsxFormattedTextRenderer.RenderNumber(-1d, "m/d/yy", false);

        Assert.That(nullValue.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullValue, "formattedText.valueNull"), Is.True);
        Assert.That(badFormat.IsValid, Is.False);
        Assert.That(HasDiagnostic(badFormat, "numberFormat.quoteUnclosed"), Is.True);
        Assert.That(badDateSerial.IsValid, Is.False);
        Assert.That(HasDiagnostic(badDateSerial, "dateSerial.range"), Is.True);
    }

    static bool HasDiagnostic(XlsxFormattedTextRenderResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
