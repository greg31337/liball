using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookRepairerTests
{
    [Test]
    public void RepairWorkbook_FixesRelationshipsDimensionsStylesSharedStringsAndWorksheetLists()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.Sheets[0].Name = "Bad/Name";
        workbook.WorkbookPart.Sheets[0].SheetId = 0;
        workbook.WorkbookPart.Sheets[0].RelationshipId = "";
        workbook.WorkbookPart.Sheets[0].State = "invisible";
        workbook.WorkbookRelationships.Relationships.Clear();
        workbook.Worksheets.Clear();
        workbook.HasStyles = true;
        workbook.Styles.Fonts = null!;
        workbook.Styles.CellFormats.Add(null!);

        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = "";
        worksheet.HasDimension = true;
        worksheet.Dimension.Reference = "Z9";
        worksheet.Rows.Add(null!);
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.Cells.Add(null!);
        XlsxWorksheetCell sharedStringCell = new XlsxWorksheetCell();
        sharedStringCell.Reference = "XFE1";
        sharedStringCell.Type = "s";
        sharedStringCell.Value = "99";
        sharedStringCell.HasStyle = true;
        sharedStringCell.Style = 7;
        row.Cells.Add(sharedStringCell);
        worksheet.Rows.Add(row);
        workbook.Worksheets.Add(worksheet);

        IXlsxWorkbookRepairerOperations operations = new XlsxWorkbookRepairer();
        XlsxWorkbookRepairResult result = operations.RepairWorkbook(workbook);
        XlsxValidationResult validation = workbook.Validate();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RepairCount, Is.GreaterThan(0));
        Assert.That(HasDiagnostic(result, "workbookRepair.sheetNameFixed"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.sheetRelationshipCreated"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.dimensionRecalculated"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.sharedStringCellFixed"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.cellStyleFixed"), Is.True);
        Assert.That(workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("Sheet1"));
        Assert.That(workbook.WorkbookPart.Sheets[0].SheetId, Is.EqualTo(1));
        Assert.That(workbook.WorkbookPart.Sheets[0].RelationshipId, Is.Not.Empty);
        Assert.That(workbook.WorkbookRelationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(workbook.Worksheets[0].Dimension.Reference, Is.EqualTo("A1"));
        Assert.That(workbook.Worksheets[0].Rows.Count, Is.EqualTo(1));
        Assert.That(workbook.Worksheets[0].Rows[0].Cells.Count, Is.EqualTo(1));
        Assert.That(sharedStringCell.Type, Is.EqualTo("inlineStr"));
        Assert.That(sharedStringCell.HasStyle, Is.False);
        Assert.That(validation.IsValid, Is.True);
    }

    [Test]
    public void RepairWorkbook_RebuildsSharedStringTableMetadataAndInvalidReferences()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Type = "s";
        cell.Value = "1";
        row.Cells.Add(cell);
        worksheet.Rows.Add(row);
        workbook.Worksheets.Add(worksheet);

        XlsxSharedStringTablePart sharedStrings = new XlsxSharedStringTablePart();
        sharedStrings.Items.Add(null!);
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = null!;
        item.RichTextRuns = null!;
        sharedStrings.Items.Add(item);

        IXlsxWorkbookRepairerOperations operations = new XlsxWorkbookRepairer();
        XlsxWorkbookRepairResult result = operations.RepairWorkbookWithSharedStrings(workbook, sharedStrings, new XlsxWorkbookRepairOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.sharedStringNullRemoved"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.sharedStringTextFixed"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookRepair.richTextRunsCreated"), Is.True);
        Assert.That(sharedStrings.Items.Count, Is.EqualTo(1));
        Assert.That(sharedStrings.Count, Is.EqualTo(1));
        Assert.That(sharedStrings.UniqueCount, Is.EqualTo(1));
        Assert.That(cell.Type, Is.EqualTo("inlineStr"));
        Assert.That(cell.InlineStringText, Is.EqualTo(""));
    }

    [Test]
    public void RepairBytes_AddsMissingRequiredPackagePartsAndProducesValidWorkbook()
    {
        XlsxWorkbookSaveResult save = XlsxWorkbook.Create().SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (entry.Path != XlsxPackagePaths.ContentTypes && entry.Path != XlsxPackagePaths.WorkbookRelationships)
            {
                entries.Add(entry);
            }
        }

        XlsxZipPackageWriteResult malformed = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        XlsxValidationResult before = new XlsxMinimumPackageValidator().ValidateBytes(malformed.Bytes, new XlsxReaderOptions());

        IXlsxWorkbookRepairerOperations operations = new XlsxWorkbookRepairer();
        XlsxWorkbookRepairResult repair = operations.RepairBytes(malformed.Bytes);
        XlsxWorkbookRepairResult repairWithOptions = operations.RepairBytesWithOptions(malformed.Bytes, new XlsxWorkbookRepairOptions());
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(repair.Bytes);
        XlsxValidationResult after = new XlsxWorkbookValidator().ValidateBytes(repair.Bytes);

        Assert.That(save.IsValid, Is.True);
        Assert.That(malformed.IsValid, Is.True);
        Assert.That(before.IsValid, Is.False);
        Assert.That(repair.IsValid, Is.True);
        Assert.That(repairWithOptions.IsValid, Is.True);
        Assert.That(repair.Bytes.Length, Is.GreaterThan(0));
        Assert.That(HasDiagnostic(repair, "workbookRepair.packagePartAdded"), Is.True);
        Assert.That(open.IsValid, Is.True);
        Assert.That(after.IsValid, Is.True);
    }

    static bool HasDiagnostic(XlsxWorkbookRepairResult result, string code)
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
