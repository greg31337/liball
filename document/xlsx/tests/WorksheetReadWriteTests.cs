using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetReadWriteTests
{
    [Test]
    public void WriteRead_RoundTripsWorksheetValuesLayoutAndProtection()
    {
        XlsxWorksheetPart worksheet = CreateWorksheet();

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        AssertWorksheetRoundTrip(roundTrip);
        Assert.That(writeResult.Xml, Does.Contain("<dimension ref=\"A1:E3\""));
        Assert.That(writeResult.Xml, Does.Contain("<pane xSplit=\"1\" ySplit=\"1\" topLeftCell=\"B2\" activePane=\"bottomRight\" state=\"frozen\""));
        Assert.That(writeResult.Xml, Does.Contain("<cols>"));
        Assert.That(writeResult.Xml, Does.Contain("<mergeCells count=\"2\""));
        Assert.That(writeResult.Xml, Does.Contain("<pageSetup"));
        Assert.That(writeResult.Xml, Does.Contain("<sheetProtection"));
    }

    [Test]
    public void WorkbookSaveOpen_RoundTripsLoadedWorksheetPart()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.RenameSheet("Sheet1", "Data");
        XlsxWorksheetPart worksheet = CreateWorksheet();
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.SelectedSheets.Add("Data");

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes, readerOptions);

        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.Worksheets.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Worksheets[0].PartPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        AssertWorksheetRoundTrip(openResult.Workbook.Worksheets[0]);
    }

    static XlsxWorksheetPart CreateWorksheet()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.Rows.Add(CreateRow(1, "1:5", 18d, 2, false));
        worksheet.Rows[0].Cells.Add(CreateCell("A1", XlsxCellValue.CreateNumber(12.5d)));
        worksheet.Rows[0].Cells.Add(CreateCell("B1", XlsxCellValue.CreateText("ready")));
        worksheet.Rows[0].Cells.Add(CreateCell("C1", XlsxCellValue.CreateBoolean(true)));
        worksheet.Rows[0].Cells.Add(CreateCell("D1", XlsxCellValue.CreateInlineString("inline")));
        worksheet.Rows[0].Cells.Add(CreateCell("E1", XlsxCellValue.CreateFormulaCachedValue("A1*2", "25", "n")));
        worksheet.Rows.Add(CreateRow(3, "1:5", 20d, 0, true));
        worksheet.Rows[1].Cells.Add(CreateCell("A3", XlsxCellValue.CreateDateTime(new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc))));
        worksheet.Rows[1].Cells.Add(CreateCell("B3", XlsxCellValue.CreateError("#N/A")));
        worksheet.Columns.Add(CreateColumn(1, 2, 18d, 3, false));
        worksheet.Columns.Add(CreateColumn(5, 5, 9d, 0, true));
        worksheet.ApplyCalculatedDimension();
        worksheet.FreezePanesByCount(1, 1);
        worksheet.MergedCells.Add(CreateMergeCell("A1:B1"));
        worksheet.MergedCells.Add(CreateMergeCell("D1:E1"));
        worksheet.HasPageSetup = true;
        worksheet.PageSetup!.PaperSize = 9;
        worksheet.PageSetup.HasPaperSize = true;
        worksheet.PageSetup.Orientation = "landscape";
        worksheet.PageSetup.Scale = 100;
        worksheet.PageSetup.HasScale = true;
        worksheet.PageSetup.FitToWidth = 1;
        worksheet.PageSetup.HasFitToWidth = true;
        worksheet.PageSetup.FitToHeight = 0;
        worksheet.PageSetup.HasFitToHeight = true;
        worksheet.PageSetup.FirstPageNumber = 3;
        worksheet.PageSetup.HasFirstPageNumber = true;
        worksheet.PageSetup.UseFirstPageNumber = true;
        worksheet.PageSetup.HasUseFirstPageNumber = true;
        worksheet.HasSheetProtection = true;
        worksheet.SheetProtection!.Sheet = true;
        worksheet.SheetProtection.HasSheet = true;
        worksheet.SheetProtection.Objects = false;
        worksheet.SheetProtection.HasObjects = true;
        worksheet.SheetProtection.Scenarios = true;
        worksheet.SheetProtection.HasScenarios = true;
        worksheet.SheetProtection.SelectLockedCells = false;
        worksheet.SheetProtection.HasSelectLockedCells = true;
        worksheet.SheetProtection.AutoFilter = true;
        worksheet.SheetProtection.HasAutoFilter = true;
        worksheet.SheetProtection.Password = "ABCD";
        worksheet.SheetProtection.AlgorithmName = "SHA-512";
        worksheet.SheetProtection.HashValue = "hash==";
        worksheet.SheetProtection.SaltValue = "salt==";
        worksheet.SheetProtection.SpinCount = 100000;
        worksheet.SheetProtection.HasSpinCount = true;
        return worksheet;
    }

    static void AssertWorksheetRoundTrip(XlsxWorksheetPart worksheet)
    {
        Assert.That(worksheet.HasDimension, Is.True);
        Assert.That(worksheet.Dimension.Reference, Is.EqualTo("A1:E3"));
        Assert.That(worksheet.Rows.Count, Is.EqualTo(2));
        Assert.That(worksheet.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(worksheet.Rows[0].Height, Is.EqualTo(18d).Within(0.0000001d));
        Assert.That(worksheet.Rows[0].HasCustomHeight, Is.True);
        Assert.That(worksheet.Rows[1].RowIndex, Is.EqualTo(3));
        Assert.That(worksheet.Rows[1].Hidden, Is.True);
        AssertNumber(worksheet.GetCell("A1"), 12.5d);
        AssertText(worksheet.GetCell("B1"), XlsxCellValueKind.Text, "ready");
        AssertBoolean(worksheet.GetCell("C1"), true);
        AssertText(worksheet.GetCell("D1"), XlsxCellValueKind.InlineString, "inline");
        AssertFormula(worksheet.GetCell("E1"), "A1*2", "25", "n");
        AssertDateTime(worksheet.GetCell("A3"), new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc));
        AssertText(worksheet.GetCell("B3"), XlsxCellValueKind.Error, "#N/A");
        Assert.That(worksheet.Columns.Count, Is.EqualTo(2));
        Assert.That(worksheet.Columns[0].Min, Is.EqualTo(1));
        Assert.That(worksheet.Columns[0].Max, Is.EqualTo(2));
        Assert.That(worksheet.Columns[0].Width, Is.EqualTo(18d).Within(0.0000001d));
        Assert.That(worksheet.Columns[0].Style, Is.EqualTo(3));
        Assert.That(worksheet.Columns[1].Hidden, Is.True);
        Assert.That(worksheet.SheetViews.Count, Is.EqualTo(1));
        Assert.That(worksheet.SheetViews[0].HasPane, Is.True);
        Assert.That(worksheet.SheetViews[0].Pane.TopLeftCell, Is.EqualTo("B2"));
        Assert.That(worksheet.SheetViews[0].Pane.State, Is.EqualTo("frozen"));
        Assert.That(worksheet.SheetViews[0].Selections.Count, Is.EqualTo(1));
        Assert.That(worksheet.SheetViews[0].Selections[0].ActiveCell, Is.EqualTo("B2"));
        Assert.That(worksheet.MergedCells.Count, Is.EqualTo(2));
        Assert.That(worksheet.MergedCells[0].Reference, Is.EqualTo("A1:B1"));
        Assert.That(worksheet.MergedCells[1].Reference, Is.EqualTo("D1:E1"));
        Assert.That(worksheet.HasPageSetup, Is.True);
        Assert.That(worksheet.PageSetup!.PaperSize, Is.EqualTo(9));
        Assert.That(worksheet.PageSetup.Orientation, Is.EqualTo("landscape"));
        Assert.That(worksheet.PageSetup.Scale, Is.EqualTo(100));
        Assert.That(worksheet.PageSetup.FirstPageNumber, Is.EqualTo(3));
        Assert.That(worksheet.PageSetup.UseFirstPageNumber, Is.True);
        Assert.That(worksheet.HasSheetProtection, Is.True);
        Assert.That(worksheet.SheetProtection!.Sheet, Is.True);
        Assert.That(worksheet.SheetProtection.Objects, Is.False);
        Assert.That(worksheet.SheetProtection.Scenarios, Is.True);
        Assert.That(worksheet.SheetProtection.AutoFilter, Is.True);
        Assert.That(worksheet.SheetProtection.Password, Is.EqualTo("ABCD"));
        Assert.That(worksheet.SheetProtection.SpinCount, Is.EqualTo(100000));
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex, string spans, double height, uint style, bool hidden)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        row.Spans = spans;
        row.Height = height;
        row.HasHeight = true;
        if (style > 0)
        {
            row.Style = style;
            row.HasStyle = true;
        }

        row.Hidden = hidden;
        row.HasHidden = true;
        row.CustomHeight = true;
        row.HasCustomHeight = true;
        return row;
    }

    static XlsxWorksheetCell CreateCell(string reference, XlsxCellValue value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        XlsxCellValueConverter.ApplyToWorksheetCell(value, cell);
        return cell;
    }

    static XlsxWorksheetColumn CreateColumn(uint min, uint max, double width, uint style, bool hidden)
    {
        XlsxWorksheetColumn column = new XlsxWorksheetColumn();
        column.Min = min;
        column.Max = max;
        column.Width = width;
        column.HasWidth = true;
        if (style > 0)
        {
            column.Style = style;
            column.HasStyle = true;
        }

        column.Hidden = hidden;
        column.HasHidden = true;
        column.CustomWidth = true;
        column.HasCustomWidth = true;
        return column;
    }

    static XlsxWorksheetMergeCell CreateMergeCell(string reference)
    {
        XlsxWorksheetMergeCell mergeCell = new XlsxWorksheetMergeCell();
        mergeCell.Reference = reference;
        return mergeCell;
    }

    static void AssertNumber(XlsxWorksheetCell? cell, double value)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.Number));
        Assert.That(result.Value.Number, Is.EqualTo(value).Within(0.0000001d));
    }

    static void AssertBoolean(XlsxWorksheetCell? cell, bool value)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.Boolean));
        Assert.That(result.Value.Boolean, Is.EqualTo(value));
    }

    static void AssertText(XlsxWorksheetCell? cell, XlsxCellValueKind kind, string value)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(kind));
        Assert.That(result.Value.Text, Is.EqualTo(value));
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

    static void AssertDateTime(XlsxWorksheetCell? cell, DateTime value)
    {
        XlsxCellValueResult result = XlsxCellValueConverter.FromWorksheetCell(cell);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Kind, Is.EqualTo(XlsxCellValueKind.DateTime));
        Assert.That(result.Value.DateTime, Is.EqualTo(value));
    }
}
