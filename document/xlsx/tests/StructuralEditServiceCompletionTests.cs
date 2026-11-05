using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StructuralEditServiceCompletionTests
{
    [Test]
    public void InsertRows_UpdatesAndReportsEverySupportedDependentCategory()
    {
        XlsxWorkbook workbook = CreateWorkbook("Sheet1");
        XlsxWorksheetPart worksheet = workbook.Worksheets[0];
        worksheet.SetFormulaCachedValue("F2", "A2+1", "", "");
        worksheet.MergeCells("A2:B3");
        XlsxTablePart table = new XlsxTablePart();
        table.Reference = "A1:C5";
        worksheet.Tables.Add(table);
        worksheet.AddCustomDataValidation("B2:B5", "B2>0");
        worksheet.AddExpressionConditionalFormatting("C2:C5", "C2>0");
        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = "D2";
        worksheet.Hyperlinks.Add(hyperlink);
        XlsxComment comment = new XlsxComment();
        comment.Reference = "E2";
        worksheet.CommentsPart.Comments.Add(comment);
        XlsxWorksheetPageBreak pageBreak = new XlsxWorksheetPageBreak();
        pageBreak.Id = 2;
        pageBreak.HasId = true;
        worksheet.RowBreaks.Add(pageBreak);
        worksheet.AddAutoFilter("A1:F5");
        AddNames(workbook);
        AddPivot(workbook, worksheet);
        AddDrawing(workbook, worksheet);
        AddChart(workbook);
        XlsxStructuralEditService service = new XlsxStructuralEditService();

        XlsxStructuralEditResult result = service.InsertRows(workbook, "Sheet1", 2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Kind, Is.EqualTo(XlsxStructuralEditKind.RowInsert));
        Assert.That(result.FormulaUpdateCount, Is.GreaterThan(0));
        Assert.That(result.NameUpdateCount, Is.EqualTo(1));
        Assert.That(result.PrintDefinitionUpdateCount, Is.EqualTo(1));
        Assert.That(result.TableUpdateCount, Is.EqualTo(1));
        Assert.That(result.PivotUpdateCount, Is.EqualTo(1));
        Assert.That(result.ValidationUpdateCount, Is.EqualTo(1));
        Assert.That(result.ConditionalFormattingUpdateCount, Is.EqualTo(1));
        Assert.That(result.HyperlinkUpdateCount, Is.EqualTo(1));
        Assert.That(result.CommentUpdateCount, Is.EqualTo(1));
        Assert.That(result.DrawingUpdateCount, Is.EqualTo(1));
        Assert.That(result.ChartUpdateCount, Is.EqualTo(2));
        Assert.That(result.PageBreakUpdateCount, Is.EqualTo(1));
        Assert.That(result.Updates.Count, Is.GreaterThanOrEqualTo(13));
        Assert.That(worksheet.GetCell("F3")!.Formula, Is.EqualTo("A3+1"));
        Assert.That(table.Reference, Is.EqualTo("A1:C6"));
        Assert.That(worksheet.DataValidations.Items[0].Sqref, Is.EqualTo("B3:B6"));
        Assert.That(worksheet.ConditionalFormattings[0].Sqref, Is.EqualTo("C3:C6"));
        Assert.That(hyperlink.Reference, Is.EqualTo("D3"));
        Assert.That(comment.Reference, Is.EqualTo("E3"));
        Assert.That(pageBreak.Id, Is.EqualTo(3));
        Assert.That(workbook.WorkbookPart.DefinedNames[0].Text, Is.EqualTo("Sheet1!$A$3:$A$6"));
        Assert.That(workbook.WorkbookPart.DefinedNames[1].Text, Is.EqualTo("Sheet1!$A$1:$F$6"));
        Assert.That(workbook.PivotTableDefinitions[0].Location.Reference, Is.EqualTo("A1:C6"));
        Assert.That(workbook.Drawings[0].ImageAnchors[0].From.Row, Is.EqualTo(2));
        Assert.That(workbook.Charts[0].Series[0].CategoryFormula, Is.EqualTo("Sheet1!$A$3:$A$6"));
        Assert.That(workbook.Charts[0].Series[0].ValuesFormula, Is.EqualTo("Sheet1!$B$3:$B$6"));
    }

    [Test]
    public void DeleteColumns_UpdatesWorkbookNamesPivotsDrawingsChartsAndWorksheetStructures()
    {
        XlsxWorkbook workbook = CreateWorkbook("Sheet1");
        XlsxWorksheetPart worksheet = workbook.Worksheets[0];
        worksheet.SetFormulaCachedValue("D1", "B1+C1", "", "");
        XlsxTablePart table = new XlsxTablePart();
        table.Reference = "A1:D4";
        worksheet.Tables.Add(table);
        XlsxWorkbookDefinedName name = new XlsxWorkbookDefinedName();
        name.Name = "Columns";
        name.Text = "Sheet1!$B$1:$D$4";
        workbook.WorkbookPart.DefinedNames.Add(name);
        AddPivot(workbook, worksheet);
        AddDrawing(workbook, worksheet);
        AddChart(workbook);
        XlsxStructuralEditService service = new XlsxStructuralEditService();

        XlsxStructuralEditResult result = service.DeleteColumns(workbook, "Sheet1", 2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Kind, Is.EqualTo(XlsxStructuralEditKind.ColumnDelete));
        Assert.That(result.NameUpdateCount, Is.EqualTo(1));
        Assert.That(result.PivotUpdateCount, Is.EqualTo(1));
        Assert.That(result.DrawingUpdateCount, Is.EqualTo(1));
        Assert.That(result.ChartUpdateCount, Is.EqualTo(1));
        Assert.That(table.Reference, Is.EqualTo("A1:C4"));
        Assert.That(name.Text, Is.EqualTo("Sheet1!$B$1:$C$4"));
        Assert.That(workbook.PivotTableDefinitions[0].Location.Reference, Is.EqualTo("A1:B5"));
        Assert.That(workbook.Drawings[0].ImageAnchors[0].From.Column, Is.EqualTo(1));
        Assert.That(workbook.Charts[0].Series[0].ValuesFormula, Is.EqualTo("Sheet1!#REF!"));
    }

    [Test]
    public void RenameSheet_UpdatesWorksheetFormulasNamesPrintDefinitionsAndChartsWithCategoryResults()
    {
        XlsxWorkbook workbook = CreateWorkbook("Data", "Report");
        workbook.Worksheets[1].SetFormulaCachedValue("A1", "'Data'!B2+1", "", "");
        XlsxWorkbookDefinedName name = new XlsxWorkbookDefinedName();
        name.Name = "Source";
        name.Text = "Data!$A$1:$B$5";
        workbook.WorkbookPart.DefinedNames.Add(name);
        XlsxWorkbookDefinedName print = new XlsxWorkbookDefinedName();
        print.Name = "_xlnm.Print_Area";
        print.Text = "Data!$A$1:$F$20";
        print.LocalSheetId = 0;
        print.HasLocalSheetId = true;
        workbook.WorkbookPart.DefinedNames.Add(print);
        XlsxChartPart chart = new XlsxChartPart();
        XlsxChartSeriesReference series = new XlsxChartSeriesReference();
        series.ValuesFormula = "Data!$B$2:$B$5";
        chart.Series.Add(series);
        workbook.Charts.Add(chart);
        XlsxStructuralEditService service = new XlsxStructuralEditService();

        XlsxStructuralEditResult result = service.RenameSheet(workbook, "Data", "Inputs");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FormulaUpdateCount, Is.EqualTo(1));
        Assert.That(result.NameUpdateCount, Is.EqualTo(1));
        Assert.That(result.PrintDefinitionUpdateCount, Is.EqualTo(1));
        Assert.That(result.ChartUpdateCount, Is.EqualTo(1));
        Assert.That(workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("Inputs"));
        Assert.That(workbook.Worksheets[1].GetCell("A1")!.Formula, Is.EqualTo("Inputs!B2+1"));
        Assert.That(name.Text, Is.EqualTo("Inputs!$A$1:$B$5"));
        Assert.That(print.Text, Is.EqualTo("Inputs!$A$1:$F$20"));
        Assert.That(series.ValuesFormula, Is.EqualTo("Inputs!$B$2:$B$5"));
    }

    [Test]
    public void SheetAddRemoveAndReorder_SynchronizeWorksheetsRelationshipsAndLocalScopes()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxStructuralEditService service = new XlsxStructuralEditService();
        XlsxStructuralEditResult add = service.AddSheet(workbook, "Data");
        XlsxWorkbookDefinedName first = CreateLocalName("First", 0);
        XlsxWorkbookDefinedName second = CreateLocalName("Second", 1);
        workbook.WorkbookPart.DefinedNames.Add(first);
        workbook.WorkbookPart.DefinedNames.Add(second);

        XlsxStructuralEditResult reorder = service.ReorderSheet(workbook, "Sheet1", 1);
        workbook.Worksheets[1].SetFormulaCachedValue("A1", "Data!A1+1", "", "");
        XlsxWorkbookDefinedName external = new XlsxWorkbookDefinedName();
        external.Name = "ExternalData";
        external.Text = "Data!$A$1";
        workbook.WorkbookPart.DefinedNames.Add(external);
        XlsxChartPart chart = new XlsxChartPart();
        XlsxChartSeriesReference series = new XlsxChartSeriesReference();
        series.ValuesFormula = "Data!$A$1:$A$2";
        chart.Series.Add(series);
        workbook.Charts.Add(chart);
        XlsxStructuralEditResult remove = service.RemoveSheet(workbook, "Data");

        Assert.That(add.IsValid, Is.True);
        Assert.That(add.RelationshipUpdateCount, Is.EqualTo(1));
        Assert.That(workbook.Worksheets.Count, Is.EqualTo(1));
        Assert.That(reorder.IsValid, Is.True);
        Assert.That(reorder.NameUpdateCount, Is.EqualTo(2));
        Assert.That(workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("Sheet1"));
        Assert.That(first.LocalSheetId, Is.EqualTo(0));
        Assert.That(remove.IsValid, Is.True);
        Assert.That(remove.RelationshipUpdateCount, Is.EqualTo(1));
        Assert.That(remove.FormulaUpdateCount, Is.EqualTo(1));
        Assert.That(remove.NameUpdateCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(remove.ChartUpdateCount, Is.EqualTo(1));
        Assert.That(remove.RemovedReferenceCount, Is.GreaterThanOrEqualTo(6));
        Assert.That(workbook.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(workbook.Worksheets.Count, Is.EqualTo(1));
        Assert.That(workbook.WorkbookPart.DefinedNames, Does.Contain(first));
        Assert.That(workbook.WorkbookPart.DefinedNames, Does.Not.Contain(second));
        Assert.That(workbook.Worksheets[0].GetCell("A1")!.Formula, Is.EqualTo("#REF!A1+1"));
        Assert.That(external.Text, Is.EqualTo("#REF!$A$1"));
        Assert.That(series.ValuesFormula, Is.EqualTo("#REF!$A$1:$A$2"));
    }

    [Test]
    public void CellShiftAndRangeMove_ReturnDeterministicStructuralResults()
    {
        XlsxWorkbook workbook = CreateWorkbook("Source", "Target");
        workbook.Worksheets[0].SetTextValue("A1", "one");
        workbook.Worksheets[0].SetTextValue("A2", "two");
        XlsxStructuralEditService service = new XlsxStructuralEditService();

        XlsxStructuralEditResult shift = service.ShiftCells(workbook, "Source", "A2", XlsxCellShiftDirection.Down);
        XlsxStructuralEditResult move = service.MoveRange(workbook, "Source", "A1:A3", "Target", "C2");

        Assert.That(shift.IsValid, Is.True);
        Assert.That(shift.Kind, Is.EqualTo(XlsxStructuralEditKind.CellShift));
        Assert.That(shift.Updates.Count, Is.EqualTo(1));
        Assert.That(move.IsValid, Is.True);
        Assert.That(move.Kind, Is.EqualTo(XlsxStructuralEditKind.RangeMove));
        Assert.That(move.AffectedCount, Is.GreaterThan(0));
        Assert.That(move.Updates.Count, Is.EqualTo(1));
        Assert.That(workbook.Worksheets[1].GetCell("C2")!.Value, Is.EqualTo("one"));
        Assert.That(workbook.Worksheets[1].GetCell("C4")!.Value, Is.EqualTo("two"));
    }

    static XlsxWorkbook CreateWorkbook(params string[] sheetNames)
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        uint id = 1;
        foreach (string sheetName in sheetNames)
        {
            XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
            sheet.Name = sheetName;
            sheet.SheetId = id;
            sheet.RelationshipId = "rId" + id;
            workbook.WorkbookPart.Sheets.Add(sheet);
            workbook.Worksheets.Add(new XlsxWorksheetPart());
            id = id + 1;
        }
        return workbook;
    }

    static void AddNames(XlsxWorkbook workbook)
    {
        XlsxWorkbookDefinedName name = new XlsxWorkbookDefinedName();
        name.Name = "DataRange";
        name.Text = "Sheet1!$A$2:$A$5";
        workbook.WorkbookPart.DefinedNames.Add(name);
        XlsxWorkbookDefinedName print = new XlsxWorkbookDefinedName();
        print.Name = "_xlnm.Print_Area";
        print.Text = "Sheet1!$A$1:$F$5";
        print.LocalSheetId = 0;
        print.HasLocalSheetId = true;
        workbook.WorkbookPart.DefinedNames.Add(print);
    }

    static void AddPivot(XlsxWorkbook workbook, XlsxWorksheetPart worksheet)
    {
        XlsxWorksheetPivotTableReference reference = new XlsxWorksheetPivotTableReference();
        reference.TargetPath = "/xl/pivotTables/pivotTable1.xml";
        worksheet.PivotTableReferences.Add(reference);
        XlsxPivotTableDefinitionPart pivot = new XlsxPivotTableDefinitionPart();
        pivot.PartPath = "/xl/pivotTables/pivotTable1.xml";
        pivot.HasLocation = true;
        pivot.Location.Reference = "A1:C5";
        workbook.PivotTableDefinitions.Add(pivot);
    }

    static void AddDrawing(XlsxWorkbook workbook, XlsxWorksheetPart worksheet)
    {
        worksheet.HasDrawing = true;
        worksheet.Drawing.TargetPath = "/xl/drawings/drawing1.xml";
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        drawing.PartPath = "/xl/drawings/drawing1.xml";
        XlsxDrawingImageAnchor anchor = new XlsxDrawingImageAnchor();
        anchor.From.Row = 1;
        anchor.From.Column = 1;
        anchor.To.Row = 4;
        anchor.To.Column = 3;
        drawing.ImageAnchors.Add(anchor);
        workbook.Drawings.Add(drawing);
    }

    static void AddChart(XlsxWorkbook workbook)
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.PartPath = "/xl/charts/chart1.xml";
        XlsxChartSeriesReference series = new XlsxChartSeriesReference();
        series.CategoryFormula = "Sheet1!$A$2:$A$5";
        series.ValuesFormula = "Sheet1!$B$2:$B$5";
        chart.Series.Add(series);
        workbook.Charts.Add(chart);
    }

    static XlsxWorkbookDefinedName CreateLocalName(string name, int scope)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = "A1";
        definedName.LocalSheetId = scope;
        definedName.HasLocalSheetId = true;
        return definedName;
    }
}
