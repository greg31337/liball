using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookValidatorTests
{
    [Test]
    public void ValidateWorkbook_ValidWorkbookReportsNoDiagnostics()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetNumberValue("A1", 1);
        worksheet.SetFormulaCachedValue("B1", "SUM(A1,1)", "2", "n");
        workbook.Worksheets.Add(worksheet);

        IXlsxWorkbookValidatorOperations operations = new XlsxWorkbookValidator();
        XlsxValidationResult result = operations.ValidateWorkbook(workbook);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(0));
    }

    [Test]
    public void ValidateWorkbook_ReportsModelRelationshipFormulaUnknownPartAndLimitIssues()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.Sheets[0].RelationshipId = "rIdMissing";
        workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet("sheet1", 2, "rId2"));

        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetNumberValue("A1", 1);
        worksheet.SetFormulaCachedValue("B1", "SUM(A1", "0", "n");
        workbook.Worksheets.Add(worksheet);

        XlsxUnknownPart unknown = new XlsxUnknownPart();
        unknown.Path = "/xl/custom/bad.bin";
        unknown.ContentType = "application/vnd.example.bad";
        unknown.Data = null!;
        workbook.UnknownParts.Parts.Add(unknown);

        XlsxWorkbookValidationOptions options = new XlsxWorkbookValidationOptions();
        options.ReaderOptions.MaxRelationshipsPerPart = 1;
        options.ReaderOptions.MaxCells = 1;

        XlsxValidationResult result = workbook.ValidateWithOptions(options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameDuplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookValidator.sheetRelationshipMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "formulaParser.closeParenMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookValidator.unknownPartDataNull"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookValidator.relationshipLimit"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookValidator.cellLimit"), Is.True);
    }

    [Test]
    public void ValidateBytes_ReportsPackageAndReaderLimitDiagnostics()
    {
        IXlsxWorkbookValidatorOperations operations = new XlsxWorkbookValidator();
        XlsxValidationResult empty = operations.ValidateBytes(new byte[0]);

        XlsxWorkbookSaveResult save = XlsxWorkbook.Create().SaveBytes();
        XlsxWorkbookValidationOptions options = new XlsxWorkbookValidationOptions();
        options.ReaderOptions.MaxZipEntries = 1;

        XlsxValidationResult limited = operations.ValidateBytesWithOptions(save.Bytes, options);

        Assert.That(empty.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "workbookValidator.bytesEmpty"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(limited.IsValid, Is.False);
        Assert.That(HasDiagnostic(limited, "zip.maxEntries"), Is.True);
    }

    static bool HasDiagnostic(XlsxValidationResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
