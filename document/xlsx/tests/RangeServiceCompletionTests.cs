using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class RangeServiceCompletionTests
{
    [Test]
    public void EnumerationAndValueOperations_UseRowMajorCoordinatesAndTypedValues()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetNumberValue("A1", 1);
        worksheet.SetTextValue("B2", "last");
        worksheet.GetCell("A1")!.Style = 7;
        worksheet.GetCell("A1")!.HasStyle = true;
        XlsxWorksheetRange range = worksheet.GetRange("A1:B2");
        XlsxRangeService service = new XlsxRangeService();

        XlsxRangeEnumerationResult sparse = service.EnumerateCells(range, false);
        XlsxRangeEnumerationResult rows = service.EnumerateRows(range, false);
        XlsxRangeEnumerationResult values = service.GetRangeValues(range);
        XlsxWorksheetRangeResult set = service.SetRangeValues(range, new List<XlsxCellValue>
        {
            XlsxCellValue.CreateNumber(10),
            XlsxCellValue.CreateText("two"),
            XlsxCellValue.CreateBoolean(true),
            XlsxCellValue.CreateError("#N/A")
        });
        string setA1 = worksheet.GetCell("A1")!.Value;
        string setB1 = worksheet.GetCell("B1")!.Value;
        string setA2Type = worksheet.GetCell("A2")!.Type!;
        string setB2Type = worksheet.GetCell("B2")!.Type!;
        XlsxWorksheetRangeResult clear = service.ClearRangeValues(range);

        Assert.That(sparse.IsValid, Is.True);
        Assert.That(sparse.Cells.Count, Is.EqualTo(2));
        Assert.That(sparse.Cells[0].Reference, Is.EqualTo("A1"));
        Assert.That(sparse.Cells[1].Reference, Is.EqualTo("B2"));
        Assert.That(rows.Rows.Count, Is.EqualTo(2));
        Assert.That(rows.Rows[0].Cells.Count, Is.EqualTo(2));
        Assert.That(values.Values.Count, Is.EqualTo(4));
        Assert.That(values.Values[1].Kind, Is.EqualTo(XlsxCellValueKind.Blank));
        Assert.That(set.IsValid, Is.True);
        Assert.That(set.AffectedCellCount, Is.EqualTo(4));
        Assert.That(setA1, Is.EqualTo("10"));
        Assert.That(setB1, Is.EqualTo("two"));
        Assert.That(setA2Type, Is.EqualTo("b"));
        Assert.That(setB2Type, Is.EqualTo("e"));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(worksheet.GetCell("A1")!.Value, Is.EqualTo(""));
        Assert.That(worksheet.GetCell("A1")!.HasStyle, Is.True);
        Assert.That(worksheet.GetCell("A1")!.Style, Is.EqualTo(7));
    }

    [Test]
    public void MergeAndFilterOperations_ApplyToTheCompleteRange()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetRange range = worksheet.GetRange("B2:D4");
        XlsxRangeService service = new XlsxRangeService();

        XlsxWorksheetMergeCellsResult merge = service.MergeRange(range);
        XlsxWorksheetFilterResult filter = service.ApplyRangeFilter(range);
        int mergedCount = worksheet.MergedCells.Count;
        string mergedReference = worksheet.MergedCells[0].Reference!;
        XlsxWorksheetMergeCellsResult unmerge = service.UnmergeRange(worksheet.GetRange("C3"));

        Assert.That(merge.IsValid, Is.True);
        Assert.That(mergedCount, Is.EqualTo(1));
        Assert.That(mergedReference, Is.EqualTo("B2:D4"));
        Assert.That(filter.IsValid, Is.True);
        Assert.That(worksheet.HasAutoFilter, Is.True);
        Assert.That(worksheet.AutoFilter.Reference, Is.EqualTo("B2:D4"));
        Assert.That(unmerge.IsValid, Is.True);
        Assert.That(unmerge.RemovedCount, Is.EqualTo(1));
        Assert.That(worksheet.MergedCells, Is.Empty);
    }

    [Test]
    public void CrossSheetCopyAndMove_PreserveCompleteCellsAndClearMovedSource()
    {
        XlsxWorksheetPart source = new XlsxWorksheetPart();
        XlsxWorksheetPart copyTarget = new XlsxWorksheetPart();
        XlsxWorksheetPart moveTarget = new XlsxWorksheetPart();
        source.SetNumberValue("A1", 4);
        source.SetFormulaCachedValue("B1", "A1*2", "8", "n");
        source.GetCell("A1")!.Style = 3;
        source.GetCell("A1")!.HasStyle = true;
        XlsxRangeService service = new XlsxRangeService();

        XlsxWorksheetRangeResult copy = service.CopyAcrossSheets(source.GetRange("A1:B1"), copyTarget, "C2");
        XlsxWorksheetRangeResult move = service.MoveAcrossSheets(source.GetRange("A1:B1"), moveTarget, "D3");

        Assert.That(copy.IsValid, Is.True);
        Assert.That(copy.CopiedCellCount, Is.EqualTo(2));
        Assert.That(copyTarget.GetCell("C2")!.Value, Is.EqualTo("4"));
        Assert.That(copyTarget.GetCell("C2")!.Style, Is.EqualTo(3));
        Assert.That(copyTarget.GetCell("D2")!.Formula, Is.EqualTo("C2*2"));
        Assert.That(move.IsValid, Is.True);
        Assert.That(move.MovedCellCount, Is.EqualTo(2));
        Assert.That(moveTarget.GetCell("D3")!.Value, Is.EqualTo("4"));
        Assert.That(moveTarget.GetCell("E3")!.Value, Is.EqualTo("8"));
        Assert.That(source.GetCell("A1")!.Value, Is.EqualTo(""));
        Assert.That(source.GetCell("B1")!.HasFormula, Is.False);
    }

    [Test]
    public void DirectionalShifts_InsertAndDeleteCellsWithinSelectedRowsOrColumns()
    {
        XlsxRangeService service = new XlsxRangeService();
        XlsxWorksheetPart vertical = new XlsxWorksheetPart();
        vertical.SetTextValue("A1", "one");
        vertical.SetTextValue("A2", "two");
        vertical.SetTextValue("A3", "three");
        XlsxWorksheetPart horizontal = new XlsxWorksheetPart();
        horizontal.SetTextValue("A1", "one");
        horizontal.SetTextValue("B1", "two");
        horizontal.SetTextValue("C1", "three");

        XlsxWorksheetRangeResult down = service.ShiftCellsDown(vertical.GetRange("A2"));
        string shiftedDownA4 = vertical.GetCell("A4")!.Value;
        XlsxWorksheetRangeResult up = service.ShiftCellsUp(vertical.GetRange("A2"));
        XlsxWorksheetRangeResult right = service.ShiftCellsRight(horizontal.GetRange("B1"));
        string shiftedRightD1 = horizontal.GetCell("D1")!.Value;
        XlsxWorksheetRangeResult left = service.ShiftCellsLeft(horizontal.GetRange("B1"));

        Assert.That(down.IsValid, Is.True);
        Assert.That(shiftedDownA4, Is.EqualTo("three"));
        Assert.That(up.IsValid, Is.True);
        Assert.That(vertical.GetCell("A1")!.Value, Is.EqualTo("one"));
        Assert.That(vertical.GetCell("A2")!.Value, Is.EqualTo("two"));
        Assert.That(vertical.GetCell("A3")!.Value, Is.EqualTo("three"));
        Assert.That(vertical.GetCell("A4")!.Value, Is.EqualTo(""));
        Assert.That(right.IsValid, Is.True);
        Assert.That(shiftedRightD1, Is.EqualTo("three"));
        Assert.That(left.IsValid, Is.True);
        Assert.That(horizontal.GetCell("A1")!.Value, Is.EqualTo("one"));
        Assert.That(horizontal.GetCell("B1")!.Value, Is.EqualTo("two"));
        Assert.That(horizontal.GetCell("C1")!.Value, Is.EqualTo("three"));
        Assert.That(horizontal.GetCell("D1")!.Value, Is.EqualTo(""));
    }

    [Test]
    public void RangeQueries_DistinguishUsedCellsFromDataCellsAndReportFirstLastOrder()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.GetOrCreateCellByReference("A1");
        worksheet.GetCell("A1")!.Style = 5;
        worksheet.GetCell("A1")!.HasStyle = true;
        worksheet.SetTextValue("C2", "first data");
        worksheet.SetNumberValue("E4", 9);
        XlsxRangeService service = new XlsxRangeService();

        XlsxRangeQueryResult result = service.QueryWorksheetRanges(worksheet);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasUsedRange, Is.True);
        Assert.That(result.UsedRange, Is.EqualTo("A1:E4"));
        Assert.That(result.DataRange, Is.EqualTo("C2:E4"));
        Assert.That(result.FirstUsedCell, Is.EqualTo("A1"));
        Assert.That(result.LastUsedCell, Is.EqualTo("E4"));
        Assert.That(result.FirstDataCell, Is.EqualTo("C2"));
        Assert.That(result.LastDataCell, Is.EqualTo("E4"));
    }

    [Test]
    public void ResizeNamedRange_PreservesExistingSheetQualifierWhenNewRangeIsLocal()
    {
        XlsxWorkbook workbook = CreateWorkbook("Data Sheet");
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = "Sales";
        definedName.Text = "'Data Sheet'!A1:B2";
        workbook.WorkbookPart.DefinedNames.Add(definedName);
        XlsxRangeService service = new XlsxRangeService();

        XlsxNamedRangeResizeResult result = service.ResizeNamedRange(workbook, "Sales", -1, "C3:E8");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasDefinedName, Is.True);
        Assert.That(result.PreviousReference, Is.EqualTo("'Data Sheet'!A1:B2"));
        Assert.That(result.Reference, Is.EqualTo("'Data Sheet'!C3:E8"));
        Assert.That(definedName.Text, Is.EqualTo("'Data Sheet'!C3:E8"));
    }

    [Test]
    public void OverlapValidation_ReportsMergesTablesValidationsFiltersAndPivotsSeparately()
    {
        XlsxWorkbook workbook = CreateWorkbook("Sheet1");
        XlsxWorksheetPart worksheet = workbook.Worksheets[0];
        worksheet.MergeCells("A1:C3");
        XlsxTablePart table = new XlsxTablePart();
        table.Reference = "B2:D5";
        worksheet.Tables.Add(table);
        XlsxDataValidation validation = new XlsxDataValidation();
        validation.Sqref = "C3:E6 Z20";
        worksheet.DataValidations.Items.Add(validation);
        worksheet.AddAutoFilter("A3:F10");
        XlsxWorksheetPivotTableReference pivotReference = new XlsxWorksheetPivotTableReference();
        pivotReference.TargetPath = "/xl/pivotTables/pivotTable1.xml";
        worksheet.PivotTableReferences.Add(pivotReference);
        XlsxPivotTableDefinitionPart pivot = new XlsxPivotTableDefinitionPart();
        pivot.PartPath = "/xl/pivotTables/pivotTable1.xml";
        pivot.HasLocation = true;
        pivot.Location.Reference = "D1:G7";
        workbook.PivotTableDefinitions.Add(pivot);
        XlsxRangeService service = new XlsxRangeService();

        XlsxRangeOverlapResult overlap = service.ValidateRangeOverlaps(workbook, "Sheet1", "C3:D4");
        XlsxRangeOverlapResult clear = service.ValidateRangeOverlaps(workbook, "Sheet1", "X10:Y11");

        Assert.That(overlap.IsValid, Is.True);
        Assert.That(overlap.HasOverlap, Is.True);
        Assert.That(overlap.MergeReferences, Is.EqualTo(new[] { "A1:C3" }));
        Assert.That(overlap.TableReferences, Is.EqualTo(new[] { "B2:D5" }));
        Assert.That(overlap.ValidationReferences, Is.EqualTo(new[] { "C3:E6" }));
        Assert.That(overlap.FilterReferences, Is.EqualTo(new[] { "A3:F10" }));
        Assert.That(overlap.PivotReferences, Is.EqualTo(new[] { "D1:G7" }));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(clear.HasOverlap, Is.False);
    }

    static XlsxWorkbook CreateWorkbook(params string[] sheetNames)
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        uint sheetId = 1;
        foreach (string sheetName in sheetNames)
        {
            XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
            sheet.Name = sheetName;
            sheet.SheetId = sheetId;
            workbook.WorkbookPart.Sheets.Add(sheet);
            workbook.Worksheets.Add(new XlsxWorksheetPart());
            sheetId = sheetId + 1;
        }
        return workbook;
    }
}
