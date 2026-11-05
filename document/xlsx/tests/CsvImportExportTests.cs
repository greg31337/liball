using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CsvImportExportTests
{
    [Test]
    public void ImportCsv_WritesQuotedEscapedAndEmptyFields()
    {
        string csv = "Name,Note,Empty\r\nAda,\"Hello, \"\"world\"\"\",\r\nLinus,\"Line1\nLine2\",Tail";
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxCsvImportResult result = worksheet.ImportCsv("B2", csv);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("B2:D4"));
        Assert.That(result.ImportedRowCount, Is.EqualTo(3));
        Assert.That(result.ColumnCount, Is.EqualTo(3));
        Assert.That(result.ImportedCellCount, Is.EqualTo(9));
        Assert.That(result.CreatedCellCount, Is.EqualTo(9));
        Assert.That(worksheet.GetCell("B2")!.Value, Is.EqualTo("Name"));
        Assert.That(worksheet.GetCell("C3")!.Value, Is.EqualTo("Hello, \"world\""));
        Assert.That(worksheet.GetCell("D3")!.Type, Is.EqualTo("str"));
        Assert.That(worksheet.GetCell("D3")!.Value, Is.EqualTo(""));
        Assert.That(worksheet.GetCell("C4")!.Value, Is.EqualTo("Line1\nLine2"));
        Assert.That(worksheet.GetCell("D4")!.Value, Is.EqualTo("Tail"));
    }

    [Test]
    public void ExportCsv_WritesEscapedFieldsFromRange()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Name");
        worksheet.SetTextValue("B1", "Note");
        worksheet.SetTextValue("C1", "Active");
        worksheet.SetTextValue("A2", "Ada");
        worksheet.SetTextValue("B2", "Hello, \"world\"");
        worksheet.SetBooleanValue("C2", true);
        worksheet.SetTextValue("A3", "Line1\nLine2");
        worksheet.SetFormulaCachedValue("C3", "SUM(40,2)", "42", "");

        XlsxCsvExportResult result = worksheet.ExportCsv("A1:C3");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasCsv, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:C3"));
        Assert.That(result.RowCount, Is.EqualTo(3));
        Assert.That(result.ColumnCount, Is.EqualTo(3));
        Assert.That(result.ExportedRowCount, Is.EqualTo(3));
        Assert.That(result.ExportedCellCount, Is.EqualTo(9));
        Assert.That(result.Csv, Is.EqualTo("Name,Note,Active\r\nAda,\"Hello, \"\"world\"\"\",TRUE\r\n\"Line1\nLine2\",,42"));
    }

    [Test]
    public void CsvImportAndExport_HonorCustomDelimiterAndNewLine()
    {
        XlsxCsvOptions options = new XlsxCsvOptions();
        options.Delimiter = ';';
        options.NewLine = "\n";
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxCsvImportResult importResult = worksheet.ImportCsvWithOptions("A1", "One;\"Two;Too\"\nThree;Four", options);
        XlsxCsvExportResult exportResult = worksheet.ExportCsvWithOptions("A1:B2", options);

        Assert.That(importResult.IsValid, Is.True);
        Assert.That(exportResult.IsValid, Is.True);
        Assert.That(worksheet.GetCell("B1")!.Value, Is.EqualTo("Two;Too"));
        Assert.That(exportResult.Csv, Is.EqualTo("One;\"Two;Too\"\nThree;Four"));
    }

    [Test]
    public void ImportCsv_ReportsInvalidInputsAndMalformedQuotes()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxCsvOptions invalidOptions = new XlsxCsvOptions();
        invalidOptions.Delimiter = '"';

        XlsxCsvImportResult? nullCsv = worksheet.ImportCsv("A1", null!);
        XlsxCsvImportResult invalidAddress = worksheet.ImportCsv("XFE1", "A,B");
        XlsxCsvImportResult outside = worksheet.ImportCsv("XFD1048576", "A,B");
        XlsxCsvImportResult unclosedQuote = worksheet.ImportCsv("A1", "\"unterminated");
        XlsxCsvImportResult badOptions = worksheet.ImportCsvWithOptions("A1", "A,B", invalidOptions);

        Assert.That(nullCsv.IsValid, Is.False);
        Assert.That(invalidAddress.IsValid, Is.False);
        Assert.That(outside.IsValid, Is.False);
        Assert.That(unclosedQuote.IsValid, Is.False);
        Assert.That(badOptions.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCsv, "csvImport.csvNull"), Is.True);
        Assert.That(HasDiagnostic(invalidAddress, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(outside, "csvImport.rangeOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(unclosedQuote, "csv.quoteUnclosed"), Is.True);
        Assert.That(HasDiagnostic(badOptions, "csvOptions.delimiterQuoteSame"), Is.True);
    }

    [Test]
    public void ExportCsv_ReportsInvalidInputs()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;
        XlsxCsvOptions invalidOptions = new XlsxCsvOptions();
        invalidOptions.NewLine = "";

        XlsxCsvExportResult invalidRange = worksheet.ExportCsv("XFE1:XFE2");
        XlsxCsvExportResult rowsNull = nullRows.ExportCsv("A1:B2");
        XlsxCsvExportResult badOptions = worksheet.ExportCsvWithOptions("A1:B2", invalidOptions);

        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(rowsNull.IsValid, Is.False);
        Assert.That(badOptions.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(rowsNull, "csvExport.rowsNull"), Is.True);
        Assert.That(HasDiagnostic(badOptions, "csvOptions.newLineMissing"), Is.True);
    }

    static bool HasDiagnostic(XlsxCsvImportResult result, string code)
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

    static bool HasDiagnostic(XlsxCsvExportResult result, string code)
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
