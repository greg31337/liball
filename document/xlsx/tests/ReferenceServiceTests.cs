using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ReferenceServiceTests
{
    [Test]
    public void WorkbookService_ParsesQuotedThreeDimensionalAndWorkbookQualifiedA1References()
    {
        IXlsxWorkbookReferenceService service = new XlsxWorkbookReferenceService();

        XlsxReferenceResolutionResult quoted = service.ParseA1Reference("'O''Brien Data'!$A$1:B2");
        XlsxReferenceResolutionResult threeDimensional = service.ParseA1Reference("Sheet1:Sheet3!A1:C4");
        XlsxReferenceResolutionResult external = service.ParseA1Reference("'[Budget 2026.xlsx]North America'!C3");
        XlsxReferenceResolutionResult externalPath = service.ParseA1Reference("'C:\\Reports\\[Budget.xlsx]Summary'!D4");

        Assert.That(quoted.IsValid, Is.True, Diagnostics(quoted));
        Assert.That(quoted.FirstSheetName, Is.EqualTo("O'Brien Data"));
        Assert.That(quoted.A1Range.First.Row, Is.EqualTo(1));
        Assert.That(quoted.A1Range.Last.Column, Is.EqualTo(2));
        Assert.That(quoted.NormalizedText, Is.EqualTo("'O''Brien Data'!$A$1:B2"));
        Assert.That(threeDimensional.IsValid, Is.True, Diagnostics(threeDimensional));
        Assert.That(threeDimensional.IsThreeDimensional, Is.True);
        Assert.That(threeDimensional.FirstSheetName, Is.EqualTo("Sheet1"));
        Assert.That(threeDimensional.LastSheetName, Is.EqualTo("Sheet3"));
        Assert.That(threeDimensional.NormalizedText, Is.EqualTo("'Sheet1:Sheet3'!A1:C4"));
        Assert.That(external.IsValid, Is.True, Diagnostics(external));
        Assert.That(external.IsExternal, Is.True);
        Assert.That(external.WorkbookName, Is.EqualTo("Budget 2026.xlsx"));
        Assert.That(external.FirstSheetName, Is.EqualTo("North America"));
        Assert.That(externalPath.IsValid, Is.True, Diagnostics(externalPath));
        Assert.That(externalPath.IsExternal, Is.True);
        Assert.That(externalPath.WorkbookName, Is.EqualTo("Budget.xlsx"));
        Assert.That(externalPath.FirstSheetName, Is.EqualTo("Summary"));
    }

    [Test]
    public void WorkbookService_ParsesR1C1RangesAndResolvesRelativeCoordinates()
    {
        IXlsxWorkbookReferenceService service = new XlsxWorkbookReferenceService();

        XlsxReferenceResolutionResult result = service.ParseR1C1Reference("'Data Set'!R[-2]C[3]:R10C5", 5, 4);
        XlsxReferenceResolutionResult outsideSheet = service.ParseR1C1Reference("R[-1]C", 1, 1);

        Assert.That(result.IsValid, Is.True, Diagnostics(result));
        Assert.That(result.HasR1C1Reference, Is.True);
        Assert.That(result.HasLastR1C1, Is.True);
        Assert.That(result.FirstR1C1.Row, Is.EqualTo(-2));
        Assert.That(result.A1Range.First.Text, Is.EqualTo("G3"));
        Assert.That(result.A1Range.Last.Text, Is.EqualTo("$E$10"));
        Assert.That(result.A1Range.Text, Is.EqualTo("G3:$E$10"));
        Assert.That(result.NormalizedText, Is.EqualTo("'Data Set'!R[-2]C[3]:R10C5"));
        Assert.That(outsideSheet.IsValid, Is.False);
        Assert.That(HasDiagnostic(outsideSheet, "reference.resolvedRowRange"), Is.True);
    }

    [Test]
    public void WorkbookService_ResolvesDefinedNamesPrintNamesAndExternalNames()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("Rate", "Sheet1!$A$1", 0, false));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("Rate", "Sheet1!$B$2", 0, true));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("_xlnm.Print_Area", "Sheet1!$A$1:$D$20", 0, true));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("_xlnm.Print_Titles", "Sheet1!$1:$2", 0, true));
        IXlsxWorkbookReferenceService service = new XlsxWorkbookReferenceService();

        XlsxReferenceResolutionResult local = service.ResolveDefinedName(workbook, "Rate", 0);
        XlsxReferenceResolutionResult qualified = service.ResolveDefinedName(workbook, "'Sheet1'!Rate", 0);
        XlsxReferenceResolutionResult printArea = service.ResolvePrintArea(workbook, 0);
        XlsxReferenceResolutionResult printTitles = service.ResolvePrintTitles(workbook, 0);
        XlsxReferenceResolutionResult external = service.ResolveDefinedName(workbook, "[Other.xlsx]Rate", 0);

        Assert.That(local.IsValid, Is.True, Diagnostics(local));
        Assert.That(local.DefinedName!.Text, Is.EqualTo("Sheet1!$B$2"));
        Assert.That(local.A1Range.First.Text, Is.EqualTo("$B$2"));
        Assert.That(qualified.IsValid, Is.True, Diagnostics(qualified));
        Assert.That(qualified.DefinedName, Is.SameAs(local.DefinedName));
        Assert.That(printArea.IsValid, Is.True, Diagnostics(printArea));
        Assert.That(printArea.IsPrintArea, Is.True);
        Assert.That(printArea.A1Range.Text, Is.EqualTo("$A$1:$D$20"));
        Assert.That(printTitles.IsValid, Is.True, Diagnostics(printTitles));
        Assert.That(printTitles.IsPrintTitles, Is.True);
        Assert.That(printTitles.NormalizedText, Is.EqualTo("Sheet1!$1:$2"));
        Assert.That(external.IsValid, Is.True, Diagnostics(external));
        Assert.That(external.IsExternal, Is.True);
        Assert.That(external.HasDefinedName, Is.False);
        Assert.That(service.IsExternalName("'[Other Book.xlsx]Data'!Rate"), Is.True);
        Assert.That(service.IsExternalName("'C:\\Reports\\[Other.xlsx]Data'!Rate"), Is.True);
        Assert.That(service.IsExternalName("Sales[Amount]"), Is.False);
    }

    [Test]
    public void WorkbookService_ResolvesStructuredTableItemsAndColumnRanges()
    {
        XlsxWorkbook workbook = CreateWorkbookWithSalesTable();
        IXlsxWorkbookReferenceService service = new XlsxWorkbookReferenceService();

        XlsxReferenceResolutionResult data = service.ResolveTableReference(workbook, "Sales[[#Data],[Amount]:[Tax]]");
        XlsxReferenceResolutionResult header = service.ResolveTableReference(workbook, "Sales[[#Headers],[Region]]");
        XlsxReferenceResolutionResult totals = service.ResolveTableReference(workbook, "Sheet1!Sales[[#Totals],[Tax]]");
        XlsxReferenceResolutionResult missing = service.ResolveTableReference(workbook, "Sales[Missing]");

        Assert.That(data.IsValid, Is.True, Diagnostics(data));
        Assert.That(data.IsStructuredReference, Is.True);
        Assert.That(data.HasTable, Is.True);
        Assert.That(data.HasWorksheet, Is.True);
        Assert.That(data.A1Range.Text, Is.EqualTo("C3:D7"));
        Assert.That(data.NormalizedText, Is.EqualTo("Sheet1!C3:D7"));
        Assert.That(header.IsValid, Is.True, Diagnostics(header));
        Assert.That(header.A1Range.Text, Is.EqualTo("B2"));
        Assert.That(totals.IsValid, Is.True, Diagnostics(totals));
        Assert.That(totals.A1Range.Text, Is.EqualTo("D8"));
        Assert.That(missing.IsValid, Is.False);
        Assert.That(HasDiagnostic(missing, "reference.tableColumnNotFound"), Is.True);
    }

    [Test]
    public void WorksheetService_BindsUnqualifiedReferencesAndRejectsOtherSheets()
    {
        XlsxWorkbook workbook = CreateWorkbookWithSalesTable();
        XlsxWorksheetPart worksheet = workbook.Worksheets[0];
        IXlsxWorksheetReferenceService service = new XlsxWorksheetReferenceService();

        XlsxReferenceResolutionResult a1 = service.ParseA1Reference(worksheet, "Data Sheet", "A1:B2");
        XlsxReferenceResolutionResult r1c1 = service.ParseR1C1Reference(worksheet, "Data Sheet", "RC:R[1]C[1]", 4, 3);
        XlsxReferenceResolutionResult table = service.ResolveTableReference(worksheet, "Data Sheet", "Sales[Amount]");
        XlsxReferenceResolutionResult mismatch = service.ParseA1Reference(worksheet, "Data Sheet", "Other!A1");

        Assert.That(a1.IsValid, Is.True, Diagnostics(a1));
        Assert.That(a1.HasWorksheet, Is.True);
        Assert.That(a1.NormalizedText, Is.EqualTo("'Data Sheet'!A1:B2"));
        Assert.That(r1c1.IsValid, Is.True, Diagnostics(r1c1));
        Assert.That(r1c1.A1Range.Text, Is.EqualTo("C4:D5"));
        Assert.That(table.IsValid, Is.True, Diagnostics(table));
        Assert.That(table.A1Range.Text, Is.EqualTo("C3:C7"));
        Assert.That(table.NormalizedText, Is.EqualTo("'Data Sheet'!C3:C7"));
        Assert.That(mismatch.IsValid, Is.False);
        Assert.That(HasDiagnostic(mismatch, "reference.sheetMismatch"), Is.True);
    }

    static XlsxWorkbook CreateWorkbookWithSalesTable()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        workbook.Worksheets.Add(worksheet);
        XlsxTablePart table = new XlsxTablePart();
        table.Name = "Sales";
        table.DisplayName = "Sales";
        table.Reference = "B2:D8";
        table.HeaderRowCount = 1;
        table.HasHeaderRowCount = true;
        table.TotalsRowCount = 1;
        table.HasTotalsRowCount = true;
        table.Columns.Add(TableColumn("Region", 1));
        table.Columns.Add(TableColumn("Amount", 2));
        table.Columns.Add(TableColumn("Tax", 3));
        worksheet.Tables.Add(table);
        return workbook;
    }

    static XlsxTableColumn TableColumn(string name, uint id)
    {
        XlsxTableColumn column = new XlsxTableColumn();
        column.Name = name;
        column.Id = id;
        column.HasId = true;
        return column;
    }

    static XlsxWorkbookDefinedName DefinedName(string name, string text, int localSheetId, bool local)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = text;
        definedName.LocalSheetId = localSheetId;
        definedName.HasLocalSheetId = local;
        return definedName;
    }

    static bool HasDiagnostic(XlsxReferenceResolutionResult result, string code)
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

    static string Diagnostics(XlsxReferenceResolutionResult result)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }
        return text;
    }
}
