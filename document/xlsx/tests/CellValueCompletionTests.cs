using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

public class CellValueCompletionTests
{
    [Test]
    public void DecimalIsoDateAndDuration_WriteInvariantExactCellText()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            XlsxWorksheetPart sheet = new XlsxWorksheetPart();
            IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();

            XlsxWorksheetCellAccessResult decimalResult = operations.SetDecimalValue(sheet, "A1", 12345678901234567890.123400m);
            XlsxWorksheetCellAccessResult dateResult = operations.SetIsoDateValue(sheet, "B1", "2026-08-27");
            XlsxWorksheetCellAccessResult dateTimeResult = operations.SetIsoDateValue(sheet, "C1", "2026-08-27T14:15:16.1200-07:00");
            XlsxWorksheetCellAccessResult durationResult = operations.SetDurationValue(sheet, "D1", TimeSpan.FromHours(27));
            XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(sheet);
            XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadBytes(write.Bytes);

            Assert.That(decimalResult.IsValid, Is.True, Diagnostics(decimalResult.Validation));
            Assert.That(dateResult.IsValid, Is.True, Diagnostics(dateResult.Validation));
            Assert.That(dateTimeResult.IsValid, Is.True, Diagnostics(dateTimeResult.Validation));
            Assert.That(durationResult.IsValid, Is.True, Diagnostics(durationResult.Validation));
            Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
            Assert.That(write.Xml, Does.Contain("12345678901234567890.123400"));
            Assert.That(write.Xml, Does.Contain(">2026-08-27</v>"));
            Assert.That(write.Xml, Does.Contain(">2026-08-27T14:15:16.1200-07:00</v>"));
            Assert.That(write.Xml, Does.Contain(">1.125</v>"));

            AssertExactText(roundTrip, "A1", "12345678901234567890.123400");
            AssertExactText(roundTrip, "B1", "2026-08-27");
            AssertExactText(roundTrip, "C1", "2026-08-27T14:15:16.1200-07:00");
            AssertExactText(roundTrip, "D1", "1.125");
            Assert.That(operations.SetIsoDateValue(sheet, "E1", "27 August 2026").IsValid, Is.False);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void TypedErrors_CoverEverySupportedExcelError()
    {
        IXlsxCellValueOperations operations = new XlsxCellValueOperations();
        string[] expected = new string[]
        {
            "#NULL!", "#DIV/0!", "#VALUE!", "#REF!", "#NAME?", "#NUM!", "#N/A", "#GETTING_DATA",
            "#SPILL!", "#CALC!", "#FIELD!", "#BLOCKED!", "#UNKNOWN!", "#CONNECT!", "#DATA!", "#BUSY!", "#PYTHON!"
        };

        Array values = Enum.GetValues(typeof(XlsxCellErrorKind));
        Assert.That(values.Length, Is.EqualTo(expected.Length));
        int index = 0;
        foreach (XlsxCellErrorKind error in values)
        {
            XlsxCellValue value = operations.CreateTypedError(error);
            XlsxWorksheetCell cell = new XlsxWorksheetCell();
            XlsxCellValueResult applied = XlsxCellValueConverter.ApplyToWorksheetCell(value, cell);
            XlsxCellValueResult read = XlsxCellValueConverter.FromWorksheetCell(cell);

            Assert.That(applied.IsValid, Is.True, Diagnostics(applied.Validation));
            Assert.That(cell.Type, Is.EqualTo("e"));
            Assert.That(cell.Value, Is.EqualTo(expected[index]));
            Assert.That(read.Value.HasErrorKind, Is.True);
            Assert.That(read.Value.ErrorKind, Is.EqualTo(error));
            index++;
        }
    }

    [Test]
    public void TypedFormulaCachedValues_RoundTripScalarKindsAndExactText()
    {
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        IXlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
        IXlsxCellValueOperations valueOperations = new XlsxCellValueOperations();

        worksheetOperations.SetFormulaValue(sheet, "A1", "1/3", valueOperations.CreateDecimal(0.3333333333333333333333333333m));
        worksheetOperations.SetFormulaValue(sheet, "B1", "1=1", valueOperations.CreateBoolean(true));
        worksheetOperations.SetFormulaValue(sheet, "C1", "NA()", valueOperations.CreateTypedError(XlsxCellErrorKind.NotAvailable));
        worksheetOperations.SetFormulaValue(sheet, "D1", "\"done\"", valueOperations.CreateText("done"));
        worksheetOperations.SetFormulaValue(sheet, "E1", "DATE(2026,8,27)", valueOperations.CreateIsoDate("2026-08-27"));

        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(sheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadBytes(write.Bytes);

        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        AssertFormulaCache(roundTrip, "A1", XlsxCellValueKind.Number, "0.3333333333333333333333333333");
        AssertFormulaCache(roundTrip, "B1", XlsxCellValueKind.Boolean, "1");
        AssertFormulaCache(roundTrip, "C1", XlsxCellValueKind.Error, "#N/A");
        AssertFormulaCache(roundTrip, "D1", XlsxCellValueKind.Text, "done");
        AssertFormulaCache(roundTrip, "E1", XlsxCellValueKind.DateTime, "2026-08-27");
    }

    [Test]
    public void TypedRichTextAndPhonetics_RoundTripThroughCellValueConversion()
    {
        XlsxInlineString inline = new XlsxInlineString();
        inline.IsRichText = true;
        inline.Text = "Tokyo";
        inline.RichTextRuns.Add(CreateRun("To", "<rPr><b /></rPr>"));
        inline.RichTextRuns.Add(CreateRun("kyo", "<rPr><i /></rPr>"));
        inline.PhoneticTexts.Add(CreatePhonetic("とうきょう", 0, 5));
        inline.HasPhoneticProperties = true;
        inline.PhoneticProperties.FontId = 3;
        inline.PhoneticProperties.HasFontId = true;
        inline.PhoneticProperties.Type = "Hiragana";
        inline.PhoneticProperties.Alignment = "center";

        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        XlsxWorksheetCellAccessResult set = operations.SetRichTextValue(sheet, "A1", inline);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(sheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadBytes(write.Bytes);
        XlsxCellValueResult converted = XlsxCellValueConverter.FromWorksheetCell(roundTrip.GetCell("A1"));

        Assert.That(set.IsValid, Is.True, Diagnostics(set.Validation));
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(converted.IsValid, Is.True, Diagnostics(converted.Validation));
        Assert.That(converted.Value.Kind, Is.EqualTo(XlsxCellValueKind.RichText));
        Assert.That(converted.Value.RichTextRuns.Count, Is.EqualTo(2));
        Assert.That(converted.Value.RichTextRuns[0].Text, Is.EqualTo("To"));
        Assert.That(converted.Value.RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(converted.Value.PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(converted.Value.PhoneticTexts[0].Text, Is.EqualTo("とうきょう"));
        Assert.That(converted.Value.HasPhoneticProperties, Is.True);
        Assert.That(converted.Value.PhoneticProperties.FontId, Is.EqualTo(3));
        Assert.That(converted.Value.PhoneticProperties.Type, Is.EqualTo("Hiragana"));
        Assert.That(converted.Value.PhoneticProperties.Alignment, Is.EqualTo("center"));
    }

    static XlsxSharedStringRichTextRun CreateRun(string text, string propertiesRawXml)
    {
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = text;
        run.PropertiesRawXml = propertiesRawXml;
        return run;
    }

    static XlsxPhoneticText CreatePhonetic(string text, uint start, uint end)
    {
        XlsxPhoneticText phonetic = new XlsxPhoneticText();
        phonetic.Text = text;
        phonetic.StartBaseIndex = start;
        phonetic.HasStartBaseIndex = true;
        phonetic.EndBaseIndex = end;
        phonetic.HasEndBaseIndex = true;
        return phonetic;
    }

    static void AssertExactText(XlsxWorksheetPart sheet, string reference, string expected)
    {
        XlsxCellValueResult value = XlsxCellValueConverter.FromWorksheetCell(sheet.GetCell(reference));
        Assert.That(value.IsValid, Is.True, Diagnostics(value.Validation));
        Assert.That(value.Value.HasExactText, Is.True);
        Assert.That(value.Value.ExactText, Is.EqualTo(expected));
    }

    static void AssertFormulaCache(XlsxWorksheetPart sheet, string reference, XlsxCellValueKind kind, string expectedText)
    {
        XlsxCellValueResult value = XlsxCellValueConverter.FromWorksheetCell(sheet.GetCell(reference));
        Assert.That(value.IsValid, Is.True, Diagnostics(value.Validation));
        Assert.That(value.Value.Kind, Is.EqualTo(XlsxCellValueKind.FormulaCachedValue));
        Assert.That(value.Value.HasTypedCachedValue, Is.True);
        Assert.That(value.Value.TypedCachedValue, Is.Not.Null);
        Assert.That(value.Value.TypedCachedValue!.Kind, Is.EqualTo(kind));
        Assert.That(value.Value.CachedValue, Is.EqualTo(expectedText));
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        List<string> messages = new List<string>();
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            messages.Add(diagnostic.Code + ": " + diagnostic.Message);
        }
        return string.Join(" | ", messages);
    }
}
