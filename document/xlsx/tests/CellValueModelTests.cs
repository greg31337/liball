using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CellValueModelTests
{
    [Test]
    public void FromWorksheetCell_DetectsSupportedCellValueKinds()
    {
        DateTime date = new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc);

        AssertValueKind(CreateCell("", ""), XlsxCellValueKind.Blank);
        AssertNumber(CreateCell("", "123.5"), 123.5d);
        AssertSharedString(CreateCell("s", "7"), 7u);
        AssertText(CreateCell("str", "plain"), XlsxCellValueKind.Text, "plain");
        AssertText(CreateInlineCell("inline text"), XlsxCellValueKind.InlineString, "inline text");
        AssertBoolean(CreateCell("b", "1"), true);
        AssertText(CreateCell("e", "#DIV/0!"), XlsxCellValueKind.Error, "#DIV/0!");
        AssertDateTime(CreateCell("d", XlsxXmlValueWriter.WriteDateTime(date)), date);
        AssertFormula(CreateFormulaCell("n", "42", "SUM(A1:A2)"), "SUM(A1:A2)", "42", "n");
        AssertUnknown(CreateCell("future", "raw"), "future", "raw");
    }

    [Test]
    public void ApplyToWorksheetCell_WritesSupportedCellValueKinds()
    {
        DateTime date = new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc);

        AssertApplied(XlsxCellValue.CreateBlank(), "", "", "", "");
        AssertApplied(XlsxCellValue.CreateNumber(12.25d), "n", "12.25", "", "");
        AssertApplied(XlsxCellValue.CreateText("plain"), "str", "plain", "", "");
        AssertApplied(XlsxCellValue.CreateSharedString(3u), "s", "3", "", "");
        AssertApplied(XlsxCellValue.CreateInlineString("inline"), "inlineStr", "", "", "inline");
        AssertApplied(XlsxCellValue.CreateBoolean(true), "b", "1", "", "");
        AssertApplied(XlsxCellValue.CreateError("#N/A"), "e", "#N/A", "", "");
        AssertApplied(XlsxCellValue.CreateDateTime(date), "d", XlsxXmlValueWriter.WriteDateTime(date), "", "");
        AssertApplied(XlsxCellValue.CreateFormulaCachedValue("A1+A2", "9", "n"), "n", "9", "A1+A2", "");
        AssertApplied(XlsxCellValue.CreateRichText("bold plain", "<r><t>bold plain</t></r>"), "inlineStr", "", "", "bold plain");
        AssertApplied(XlsxCellValue.CreateUnknownRawValue("future", "raw"), "future", "raw", "", "");
    }

    [Test]
    public void WorksheetWriterReader_RoundTripsRepresentativeCellValues()
    {
        DateTime date = new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc);
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = 1;
        row.Cells.Add(CreateAppliedCell("A1", XlsxCellValue.CreateNumber(1.5d)));
        row.Cells.Add(CreateAppliedCell("B1", XlsxCellValue.CreateSharedString(2u)));
        row.Cells.Add(CreateAppliedCell("C1", XlsxCellValue.CreateInlineString("inline")));
        row.Cells.Add(CreateAppliedCell("D1", XlsxCellValue.CreateBoolean(false)));
        row.Cells.Add(CreateAppliedCell("E1", XlsxCellValue.CreateFormulaCachedValue("A1*2", "3", "n")));
        row.Cells.Add(CreateAppliedCell("F1", XlsxCellValue.CreateError("#VALUE!")));
        row.Cells.Add(CreateAppliedCell("G1", XlsxCellValue.CreateDateTime(date)));
        part.Rows.Add(row);

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        AssertNumber(roundTrip.GetCell("A1"), 1.5d);
        AssertSharedString(roundTrip.GetCell("B1"), 2u);
        AssertText(roundTrip.GetCell("C1"), XlsxCellValueKind.InlineString, "inline");
        AssertBoolean(roundTrip.GetCell("D1"), false);
        AssertFormula(roundTrip.GetCell("E1"), "A1*2", "3", "n");
        AssertText(roundTrip.GetCell("F1"), XlsxCellValueKind.Error, "#VALUE!");
        AssertDateTime(roundTrip.GetCell("G1"), date);
    }

    [Test]
    public void FromWorksheetCell_ReportsInvalidTypedValuesAndNullInputs()
    {
        XlsxCellValueResult? nullCellResult = XlsxCellValueConverter.FromWorksheetCell(null);
        XlsxCellValueResult? nullValueResult = XlsxCellValueConverter.ApplyToWorksheetCell(null!, new XlsxWorksheetCell());
        XlsxCellValueResult? nullTargetResult = XlsxCellValueConverter.ApplyToWorksheetCell(XlsxCellValue.CreateBlank(), null!);
        XlsxCellValueResult invalidNumber = XlsxCellValueConverter.FromWorksheetCell(CreateCell("n", "bad"));
        XlsxCellValueResult invalidSharedString = XlsxCellValueConverter.FromWorksheetCell(CreateCell("s", "bad"));
        XlsxCellValueResult invalidBoolean = XlsxCellValueConverter.FromWorksheetCell(CreateCell("b", "maybe"));
        XlsxCellValueResult invalidDateTime = XlsxCellValueConverter.FromWorksheetCell(CreateCell("d", "bad"));

        Assert.That(nullCellResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCellResult, "cellValue.cellNull"), Is.True);
        Assert.That(nullValueResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullValueResult, "cellValue.valueNull"), Is.True);
        Assert.That(nullTargetResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullTargetResult, "cellValue.cellNull"), Is.True);
        AssertInvalidUnknown(invalidNumber, "xml.double.invalid", "n", "bad");
        AssertInvalidUnknown(invalidSharedString, "xml.unsignedInteger.invalid", "s", "bad");
        AssertInvalidUnknown(invalidBoolean, "xml.boolean.invalid", "b", "maybe");
        AssertInvalidUnknown(invalidDateTime, "xml.dateTime.invalid", "d", "bad");
    }

    static XlsxWorksheetCell CreateCell(string type, string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Type = type;
        cell.Value = value;
        return cell;
    }

    static XlsxWorksheetCell CreateInlineCell(string text)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Type = "inlineStr";
        cell.InlineStringText = text;
        return cell;
    }

    static XlsxWorksheetCell CreateFormulaCell(string type, string value, string formula)
    {
        XlsxWorksheetCell cell = CreateCell(type, value);
        cell.Formula = formula;
        return cell;
    }

    static XlsxWorksheetCell CreateAppliedCell(string reference, XlsxCellValue value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        XlsxCellValueResult result = XlsxCellValueConverter.ApplyToWorksheetCell(value, cell);
        Assert.That(result.IsValid, Is.True);
        return cell;
    }

    static void AssertValueKind(XlsxWorksheetCell cell, XlsxCellValueKind kind)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(kind));
    }

    static void AssertNumber(XlsxWorksheetCell? cell, double number)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(number).Within(0.0000001d));
    }

    static void AssertSharedString(XlsxWorksheetCell? cell, uint index)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.SharedString));
        Assert.That(result.Value.SharedStringIndex, Is.EqualTo(index));
    }

    static void AssertText(XlsxWorksheetCell? cell, XlsxCellValueKind kind, string text)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(kind));
        Assert.That(result.Value.Text, Is.EqualTo(text));
    }

    static void AssertBoolean(XlsxWorksheetCell? cell, bool boolean)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.Boolean));
        Assert.That(result.Value.Boolean, Is.EqualTo(boolean));
    }

    static void AssertDateTime(XlsxWorksheetCell? cell, DateTime dateTime)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.DateTime));
        Assert.That(result.Value.DateTime, Is.EqualTo(dateTime));
    }

    static void AssertFormula(XlsxWorksheetCell? cell, string formula, string cachedValue, string cachedType)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.FormulaCachedValue));
        Assert.That(result.Value.Formula, Is.EqualTo(formula));
        Assert.That(result.Value.CachedValue, Is.EqualTo(cachedValue));
        Assert.That(result.Value.CachedType, Is.EqualTo(cachedType));
    }

    static void AssertUnknown(XlsxWorksheetCell cell, string rawType, string rawValue)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.UnknownRawValue));
        Assert.That(result.Value.RawType, Is.EqualTo(rawType));
        Assert.That(result.Value.RawValue, Is.EqualTo(rawValue));
    }

    static void AssertApplied(XlsxCellValue value, string type, string rawValue, string formula, string inlineText)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Type = "old";
        cell.Value = "old";
        cell.Formula = "old";
        cell.InlineStringText = "old";

        XlsxCellValueResult result = XlsxCellValueConverter.ApplyToWorksheetCell(value, cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(cell.Type, Is.EqualTo(type));
        Assert.That(cell.Value, Is.EqualTo(rawValue));
        Assert.That(cell.Formula, Is.EqualTo(formula));
        Assert.That(cell.InlineStringText, Is.EqualTo(inlineText));
    }

    static void AssertInvalidUnknown(XlsxCellValueResult result, string code, string rawType, string rawValue)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, code), Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.UnknownRawValue));
        Assert.That(result.Value.RawType, Is.EqualTo(rawType));
        Assert.That(result.Value.RawValue, Is.EqualTo(rawValue));
    }

    static bool HasDiagnostic(XlsxCellValueResult result, string code)
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
