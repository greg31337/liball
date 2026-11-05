using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents a rectangular worksheet range and applies bulk edits to its inclusive, one-based row and column bounds.
/// </summary>
public class XlsxWorksheetRange
{
    /// <summary>
    /// Stores the worksheet value used by this model.
    /// </summary>
    public XlsxWorksheetPart? Worksheet;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the first row value used by this model.
    /// </summary>
    public int FirstRow;
    /// <summary>
    /// Stores the first column value used by this model.
    /// </summary>
    public int FirstColumn;
    /// <summary>
    /// Stores the last row value used by this model.
    /// </summary>
    public int LastRow;
    /// <summary>
    /// Stores the last column value used by this model.
    /// </summary>
    public int LastColumn;
    /// <summary>
    /// Initializes a new worksheet range for the supplied worksheet and reference.
    /// </summary>
    /// <param name = "worksheet">The worksheet model used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    public XlsxWorksheetRange(XlsxWorksheetPart worksheet, string reference)
    {
        Worksheet = worksheet;
        Reference = XlsxCellValueConverter.NormalizeText(reference);
        FirstRow = 0;
        FirstColumn = 0;
        LastRow = 0;
        LastColumn = 0;
        InitializeBounds(reference);
    }

    internal void InitializeBounds(string reference)
    {
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            return;
        }

        FirstRow = range.First.Row;
        FirstColumn = range.First.Column;
        LastRow = range.Last.Row;
        LastColumn = range.Last.Column;
        if (FirstRow > LastRow)
        {
            int temporary = FirstRow;
            FirstRow = LastRow;
            LastRow = temporary;
        }

        if (FirstColumn > LastColumn)
        {
            int temporary = FirstColumn;
            FirstColumn = LastColumn;
            LastColumn = temporary;
        }
    }

    /// <summary>
    /// Clears  while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult Clear()
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        ForEachCellClear(result, true, true);
        return result;
    }

    /// <summary>
    /// Clears values while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult ClearValues()
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        ForEachCellClear(result, true, false);
        return result;
    }

    /// <summary>
    /// Clears styles while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult ClearStyles()
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        ForEachCellClear(result, false, true);
        return result;
    }

    /// <summary>
    /// Sets style and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "style">The style value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult SetStyle(uint style)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid || Worksheet == null)
        {
            return result;
        }

        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetCellAccessResult cellResult = Worksheet.GetOrCreateCell(row, column);
                CopyCellAccessDiagnostics(result, cellResult);
                if (cellResult.IsValid && cellResult.HasCell && cellResult.Cell != null)
                {
                    cellResult.Cell.Style = style;
                    cellResult.Cell.HasStyle = true;
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.StyledCellCount = result.StyledCellCount + 1;
                    if (cellResult.CreatedCell)
                    {
                        result.CreatedCellCount = result.CreatedCellCount + 1;
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    /// <summary>
    /// Sets formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult SetFormula(string formula)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid || Worksheet == null)
        {
            return result;
        }

        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetCellAccessResult cellResult = Worksheet.GetOrCreateCell(row, column);
                CopyCellAccessDiagnostics(result, cellResult);
                if (cellResult.IsValid && cellResult.HasCell && cellResult.Cell != null)
                {
                    XlsxCellFormula formulaInfo = XlsxCellFormula.CreateNormal(formula);
                    XlsxWorksheetPart.ApplyFormulaInfoToCell(cellResult.Cell, formulaInfo, "", "");
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.FormulaCellCount = result.FormulaCellCount + 1;
                    if (cellResult.CreatedCell)
                    {
                        result.CreatedCellCount = result.CreatedCellCount + 1;
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the fill value operation using the current model state.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillValue(XlsxCellValue value)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid || Worksheet == null)
        {
            return result;
        }

        if (value == null)
        {
            result.AddError("worksheetRange.valueNull", "Range fill value must not be null.");
            return result;
        }

        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetCellAccessResult cellResult = Worksheet.GetOrCreateCell(row, column);
                CopyCellAccessDiagnostics(result, cellResult);
                if (cellResult.IsValid && cellResult.HasCell && cellResult.Cell != null)
                {
                    XlsxCellValueResult valueResult = XlsxCellValueConverter.ApplyToWorksheetCell(value, cellResult.Cell);
                    CopyCellValueDiagnostics(result, valueResult);
                    if (valueResult.IsValid)
                    {
                        result.AffectedCellCount = result.AffectedCellCount + 1;
                        result.FilledCellCount = result.FilledCellCount + 1;
                        if (cellResult.CreatedCell)
                        {
                            result.CreatedCellCount = result.CreatedCellCount + 1;
                        }
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the fill text operation using the current model state.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillText(string text)
    {
        return FillValue(XlsxCellValue.CreateText(text));
    }

    /// <summary>
    /// Performs the fill number operation using the current model state.
    /// </summary>
    /// <param name = "number">The number value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillNumber(double number)
    {
        return FillValue(XlsxCellValue.CreateNumber(number));
    }

    /// <summary>
    /// Performs the fill boolean operation using the current model state.
    /// </summary>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillBoolean(bool value)
    {
        return FillValue(XlsxCellValue.CreateBoolean(value));
    }

    /// <summary>
    /// Performs the fill formula operation using the current model state.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillFormula(string formula)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid || Worksheet == null)
        {
            return result;
        }

        string normalizedFormula = XlsxCellValueConverter.NormalizeText(formula);
        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetCellAccessResult cellResult = Worksheet.GetOrCreateCell(row, column);
                CopyCellAccessDiagnostics(result, cellResult);
                if (cellResult.IsValid && cellResult.HasCell && cellResult.Cell != null)
                {
                    XlsxFormulaReferenceUpdateResult formulaResult = XlsxWorksheetPart.CopyFormulaReferences(normalizedFormula, row - FirstRow, column - FirstColumn);
                    CopyFormulaReferenceDiagnostics(result, formulaResult);
                    string translated = formulaResult.Formula;
                    XlsxCellFormula formulaInfo = XlsxCellFormula.CreateNormal(translated);
                    XlsxWorksheetPart.ApplyFormulaInfoToCell(cellResult.Cell, formulaInfo, "", "");
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.FilledCellCount = result.FilledCellCount + 1;
                    result.FormulaCellCount = result.FormulaCellCount + 1;
                    if (cellResult.CreatedCell)
                    {
                        result.CreatedCellCount = result.CreatedCellCount + 1;
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the fill series operation using the current model state.
    /// </summary>
    /// <param name = "start">The start value used by the operation.</param>
    /// <param name = "step">The step value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillSeries(double start, double step)
    {
        return FillLinearSeries(start, step);
    }

    /// <summary>
    /// Performs the fill linear series operation using the current model state.
    /// </summary>
    /// <param name = "start">The start value used by the operation.</param>
    /// <param name = "step">The step value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult FillLinearSeries(double start, double step)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid || Worksheet == null)
        {
            return result;
        }

        int index = 0;
        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetCellAccessResult cellResult = Worksheet.GetOrCreateCell(row, column);
                CopyCellAccessDiagnostics(result, cellResult);
                if (cellResult.IsValid && cellResult.HasCell && cellResult.Cell != null)
                {
                    double number = start + step * index;
                    XlsxCellValue value = XlsxCellValue.CreateNumber(number);
                    XlsxCellValueResult valueResult = XlsxCellValueConverter.ApplyToWorksheetCell(value, cellResult.Cell);
                    CopyCellValueDiagnostics(result, valueResult);
                    if (valueResult.IsValid)
                    {
                        result.AffectedCellCount = result.AffectedCellCount + 1;
                        result.FilledCellCount = result.FilledCellCount + 1;
                        result.SeriesCellCount = result.SeriesCellCount + 1;
                        if (cellResult.CreatedCell)
                        {
                            result.CreatedCellCount = result.CreatedCellCount + 1;
                        }
                    }
                }

                index = index + 1;
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the copy to operation using the current model state.
    /// </summary>
    /// <param name = "topLeftReference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult CopyTo(string topLeftReference)
    {
        XlsxWorksheetRangeResult result = ValidateCopyMoveTarget(topLeftReference);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        List<XlsxWorksheetRangeCellSnapshot> snapshots = CreateSnapshots();
        ApplySnapshots(result, snapshots, target.Row, target.Column, false);
        return result;
    }

    /// <summary>
    /// Performs the move to operation using the current model state.
    /// </summary>
    /// <param name = "topLeftReference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult MoveTo(string topLeftReference)
    {
        XlsxWorksheetRangeResult result = ValidateCopyMoveTarget(topLeftReference);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        List<XlsxWorksheetRangeCellSnapshot> snapshots = CreateSnapshots();
        ApplySnapshots(result, snapshots, target.Row, target.Column, true);
        ClearSourceCellsAfterMove(result, target.Row, target.Column);
        return result;
    }

    /// <summary>
    /// Performs the transpose operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRangeResult Transpose()
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        int rowCount = LastRow - FirstRow + 1;
        int columnCount = LastColumn - FirstColumn + 1;
        if (rowCount != columnCount)
        {
            result.AddError("worksheetRange.transposeRequiresSquareRange", "In-place transpose requires a square worksheet range.");
            return result;
        }

        List<XlsxWorksheetRangeCellSnapshot> snapshots = CreateSnapshots();
        ApplyTransposeSnapshots(result, snapshots, FirstRow, FirstColumn);
        return result;
    }

    internal XlsxWorksheetRangeResult TransposeTo(string topLeftReference)
    {
        XlsxWorksheetRangeResult result = ValidateTransposeTarget(topLeftReference);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        List<XlsxWorksheetRangeCellSnapshot> snapshots = CreateSnapshots();
        ApplyTransposeSnapshots(result, snapshots, target.Row, target.Column);
        return result;
    }

    internal XlsxWorksheetRangeResult SortByColumn(int columnIndex, bool ascending)
    {
        return SortRowsByColumn(columnIndex, ascending);
    }

    internal XlsxWorksheetRangeResult SortRowsByColumn(int columnIndex, bool ascending)
    {
        XlsxWorksheetRangeResult result = ValidateSortColumn(columnIndex);
        if (!result.IsValid)
        {
            return result;
        }

        List<XlsxWorksheetRangeRowSnapshot> rows = CreateRowSnapshotsForSort(columnIndex);
        SortRowSnapshots(rows, ascending);
        ApplySortedRowSnapshots(result, rows);
        return result;
    }

    internal XlsxWorksheetRangeResult SortRowsByRelativeColumn(int relativeColumn, bool ascending)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        int width = LastColumn - FirstColumn + 1;
        if (relativeColumn <= 0 || relativeColumn > width)
        {
            result.AddError("worksheetRange.sortColumnOutsideRange", "Sort column must be inside the worksheet range.");
            return result;
        }

        return SortRowsByColumn(FirstColumn + relativeColumn - 1, ascending);
    }

    internal List<XlsxWorksheetRangeCellSnapshot> CreateSnapshots()
    {
        List<XlsxWorksheetRangeCellSnapshot> snapshots = new List<XlsxWorksheetRangeCellSnapshot>();
        if (Worksheet == null)
        {
            return snapshots;
        }

        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetRangeCellSnapshot snapshot = new XlsxWorksheetRangeCellSnapshot();
                snapshot.RowOffset = row - FirstRow;
                snapshot.ColumnOffset = column - FirstColumn;
                XlsxWorksheetCell? cell = Worksheet.GetCellAt(row, column);
                if (cell != null)
                {
                    snapshot.Cell = CloneCell(cell, cell.Reference);
                    snapshot.HasCell = true;
                }

                snapshots.Add(snapshot);
                column = column + 1;
            }

            row = row + 1;
        }

        return snapshots;
    }

    internal List<XlsxWorksheetRangeRowSnapshot> CreateRowSnapshotsForSort(int sortColumn)
    {
        List<XlsxWorksheetRangeRowSnapshot> rows = new List<XlsxWorksheetRangeRowSnapshot>();
        if (Worksheet == null)
        {
            return rows;
        }

        int row = FirstRow;
        int order = 0;
        while (row <= LastRow)
        {
            XlsxWorksheetRangeRowSnapshot rowSnapshot = new XlsxWorksheetRangeRowSnapshot();
            rowSnapshot.OriginalRowIndex = row;
            rowSnapshot.OriginalOrder = order;
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                XlsxWorksheetRangeCellSnapshot snapshot = new XlsxWorksheetRangeCellSnapshot();
                snapshot.RowOffset = row - FirstRow;
                snapshot.ColumnOffset = column - FirstColumn;
                XlsxWorksheetCell? cell = Worksheet.GetCellAt(row, column);
                if (cell != null)
                {
                    snapshot.Cell = CloneCell(cell, cell.Reference);
                    snapshot.HasCell = true;
                    if (column == sortColumn)
                    {
                        rowSnapshot.SortCell = CloneCell(cell, cell.Reference);
                        rowSnapshot.HasSortCell = true;
                    }
                }

                rowSnapshot.Cells.Add(snapshot);
                column = column + 1;
            }

            rows.Add(rowSnapshot);
            row = row + 1;
            order = order + 1;
        }

        return rows;
    }

    internal void SortRowSnapshots(List<XlsxWorksheetRangeRowSnapshot> rows, bool ascending)
    {
        int outer = 1;
        while (outer < rows.Count)
        {
            XlsxWorksheetRangeRowSnapshot current = rows[outer];
            int inner = outer - 1;
            while (inner >= 0 && CompareRowSnapshots(rows[inner], current, ascending) > 0)
            {
                rows[inner + 1] = rows[inner];
                inner = inner - 1;
            }

            rows[inner + 1] = current;
            outer = outer + 1;
        }
    }

    internal void ApplySortedRowSnapshots(XlsxWorksheetRangeResult result, List<XlsxWorksheetRangeRowSnapshot> rows)
    {
        if (Worksheet == null)
        {
            return;
        }

        int rowIndex = 0;
        while (rowIndex < rows.Count)
        {
            XlsxWorksheetRangeRowSnapshot rowSnapshot = rows[rowIndex];
            int targetRow = FirstRow + rowIndex;
            foreach (XlsxWorksheetRangeCellSnapshot cellSnapshot in rowSnapshot.Cells)
            {
                int targetColumn = FirstColumn + cellSnapshot.ColumnOffset;
                if (cellSnapshot.HasCell && cellSnapshot.Cell != null)
                {
                    string reference = XlsxA1CellReferenceConverter.FormatString(targetRow, targetColumn, false, false);
                    XlsxWorksheetCell clone = CloneCell(cellSnapshot.Cell, reference);
                    XlsxWorksheetCellAccessResult setResult = Worksheet.SetCell(targetRow, targetColumn, clone);
                    CopyCellAccessDiagnostics(result, setResult);
                    if (setResult.IsValid)
                    {
                        result.AffectedCellCount = result.AffectedCellCount + 1;
                        if (setResult.CreatedCell)
                        {
                            result.CreatedCellCount = result.CreatedCellCount + 1;
                        }
                    }
                }
                else
                {
                    ClearCellAt(result, targetRow, targetColumn, true, true);
                }
            }

            result.SortedRowCount = result.SortedRowCount + 1;
            rowIndex = rowIndex + 1;
        }
    }

    internal void ApplyTransposeSnapshots(XlsxWorksheetRangeResult result, List<XlsxWorksheetRangeCellSnapshot> snapshots, int targetRow, int targetColumn)
    {
        if (Worksheet == null)
        {
            return;
        }

        foreach (XlsxWorksheetRangeCellSnapshot snapshot in snapshots)
        {
            int row = targetRow + snapshot.ColumnOffset;
            int column = targetColumn + snapshot.RowOffset;
            if (snapshot.HasCell && snapshot.Cell != null)
            {
                string reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
                XlsxWorksheetCell clone = CloneCell(snapshot.Cell, reference);
                XlsxWorksheetCellAccessResult setResult = Worksheet.SetCell(row, column, clone);
                CopyCellAccessDiagnostics(result, setResult);
                if (setResult.IsValid)
                {
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.TransposedCellCount = result.TransposedCellCount + 1;
                    if (setResult.CreatedCell)
                    {
                        result.CreatedCellCount = result.CreatedCellCount + 1;
                    }
                }
            }
            else
            {
                ClearCellAt(result, row, column, true, true);
            }
        }
    }

    internal void ApplySnapshots(XlsxWorksheetRangeResult result, List<XlsxWorksheetRangeCellSnapshot> snapshots, int targetRow, int targetColumn, bool move)
    {
        if (Worksheet == null)
        {
            return;
        }

        foreach (XlsxWorksheetRangeCellSnapshot snapshot in snapshots)
        {
            int row = targetRow + snapshot.RowOffset;
            int column = targetColumn + snapshot.ColumnOffset;
            string reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
            if (snapshot.HasCell && snapshot.Cell != null)
            {
                XlsxWorksheetCell clone = CloneCell(snapshot.Cell, reference);
                if (!move)
                {
                    TranslateCopiedCellFormula(result, clone, targetRow - FirstRow, targetColumn - FirstColumn);
                }
                XlsxWorksheetCellAccessResult setResult = Worksheet.SetCell(row, column, clone);
                CopyCellAccessDiagnostics(result, setResult);
                if (setResult.IsValid)
                {
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.CopiedCellCount = result.CopiedCellCount + 1;
                    if (move)
                    {
                        result.MovedCellCount = result.MovedCellCount + 1;
                    }
                }
            }
            else
            {
                ClearCellAt(result, row, column, true, true);
            }
        }
    }

    internal static void TranslateCopiedCellFormula(XlsxWorksheetRangeResult result, XlsxWorksheetCell cell, int rowDelta, int columnDelta)
    {
        if (cell == null || (!cell.HasFormula && (cell.Formula == null || cell.Formula.Length == 0) && (cell.FormulaInfo == null || cell.FormulaInfo.Text == null || cell.FormulaInfo.Text.Length == 0)))
        {
            return;
        }

        string formula = XlsxWorksheetPart.GetFormulaText(cell);
        XlsxFormulaReferenceUpdateResult translated = XlsxWorksheetPart.CopyFormulaReferences(formula, rowDelta, columnDelta);
        CopyFormulaReferenceDiagnostics(result, translated);
        if (!translated.IsValid)
        {
            return;
        }

        cell.Formula = translated.Formula;
        if (cell.FormulaInfo == null)
        {
            cell.FormulaInfo = XlsxCellFormula.CreateNormal(translated.Formula);
        }
        else
        {
            cell.FormulaInfo.Text = translated.Formula;
        }
        cell.HasFormula = true;
        result.FormulaCellCount = result.FormulaCellCount + 1;
    }

    internal void ForEachCellClear(XlsxWorksheetRangeResult result, bool clearValues, bool clearStyles)
    {
        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                ClearCellAt(result, row, column, clearValues, clearStyles);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal void ClearSourceCellsAfterMove(XlsxWorksheetRangeResult result, int targetRow, int targetColumn)
    {
        int rowCount = LastRow - FirstRow + 1;
        int columnCount = LastColumn - FirstColumn + 1;
        int row = FirstRow;
        while (row <= LastRow)
        {
            int column = FirstColumn;
            while (column <= LastColumn)
            {
                if (!IsCellInsideTargetRectangle(row, column, targetRow, targetColumn, rowCount, columnCount))
                {
                    ClearCellAt(result, row, column, true, true);
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal void ClearCellAt(XlsxWorksheetRangeResult result, int row, int column, bool clearValues, bool clearStyles)
    {
        if (Worksheet == null)
        {
            return;
        }

        XlsxWorksheetCell? cell = Worksheet.GetCellAt(row, column);
        if (cell == null)
        {
            return;
        }

        if (clearValues)
        {
            XlsxCellValueConverter.ClearTypedCellContent(cell);
        }

        if (clearStyles)
        {
            cell.Style = 0;
            cell.HasStyle = false;
        }

        result.AffectedCellCount = result.AffectedCellCount + 1;
        result.ClearedCellCount = result.ClearedCellCount + 1;
    }

    internal XlsxWorksheetRangeResult Validate()
    {
        XlsxWorksheetRangeResult result = new XlsxWorksheetRangeResult();
        if (Worksheet == null)
        {
            result.AddError("worksheetRange.worksheetNull", "Worksheet range requires a worksheet part.");
        }

        if (FirstRow <= 0 || FirstColumn <= 0 || LastRow < FirstRow || LastColumn < FirstColumn)
        {
            result.AddError("worksheetRange.referenceInvalid", "Worksheet range reference must be a valid A1 cell or range reference.");
        }

        return result;
    }

    internal XlsxWorksheetRangeResult ValidateCopyMoveTarget(string topLeftReference)
    {
        XlsxWorksheetRangeResult result = Validate();
        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        if (!target.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }

            return result;
        }

        int rowCount = LastRow - FirstRow + 1;
        int columnCount = LastColumn - FirstColumn + 1;
        if (!XlsxWorksheetPart.IsValidCellCoordinate(target.Row + rowCount - 1, target.Column + columnCount - 1))
        {
            result.AddError("worksheetRange.targetOutsideWorksheet", "Worksheet range target must fit within worksheet bounds.");
        }

        return result;
    }

    internal XlsxWorksheetRangeResult ValidateTransposeTarget(string topLeftReference)
    {
        XlsxWorksheetRangeResult result = Validate();
        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        if (!target.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }

            return result;
        }

        int rowCount = LastColumn - FirstColumn + 1;
        int columnCount = LastRow - FirstRow + 1;
        if (!XlsxWorksheetPart.IsValidCellCoordinate(target.Row + rowCount - 1, target.Column + columnCount - 1))
        {
            result.AddError("worksheetRange.transposeTargetOutsideWorksheet", "Transposed range target must fit within worksheet bounds.");
        }

        return result;
    }

    internal XlsxWorksheetRangeResult ValidateSortColumn(int columnIndex)
    {
        XlsxWorksheetRangeResult result = Validate();
        if (!result.IsValid)
        {
            return result;
        }

        if (!XlsxWorksheetPart.IsValidCellColumn(columnIndex))
        {
            result.AddError("worksheetRange.sortColumnRange", "Sort column must be within worksheet bounds.");
            return result;
        }

        if (columnIndex < FirstColumn || columnIndex > LastColumn)
        {
            result.AddError("worksheetRange.sortColumnOutsideRange", "Sort column must be inside the worksheet range.");
        }

        return result;
    }

    internal static bool IsCellInsideTargetRectangle(int row, int column, int targetRow, int targetColumn, int rowCount, int columnCount)
    {
        return row >= targetRow && row < targetRow + rowCount && column >= targetColumn && column < targetColumn + columnCount;
    }

    internal static int CompareRowSnapshots(XlsxWorksheetRangeRowSnapshot left, XlsxWorksheetRangeRowSnapshot right, bool ascending)
    {
        bool leftBlank = GetSortCellText(left.SortCell, left.HasSortCell).Length == 0;
        bool rightBlank = GetSortCellText(right.SortCell, right.HasSortCell).Length == 0;
        if (leftBlank && !rightBlank)
        {
            return 1;
        }

        if (!leftBlank && rightBlank)
        {
            return -1;
        }

        int comparison = CompareSortCells(left.SortCell, left.HasSortCell, right.SortCell, right.HasSortCell);
        if (!ascending)
        {
            comparison = -comparison;
        }

        if (comparison == 0)
        {
            if (left.OriginalOrder < right.OriginalOrder)
            {
                return -1;
            }

            if (left.OriginalOrder > right.OriginalOrder)
            {
                return 1;
            }
        }

        return comparison;
    }

    internal static int CompareSortCells(XlsxWorksheetCell left, bool hasLeft, XlsxWorksheetCell right, bool hasRight)
    {
        string leftText = GetSortCellText(left, hasLeft);
        string rightText = GetSortCellText(right, hasRight);
        bool leftBlank = leftText.Length == 0;
        bool rightBlank = rightText.Length == 0;
        if (leftBlank && rightBlank)
        {
            return 0;
        }

        if (leftBlank)
        {
            return 1;
        }

        if (rightBlank)
        {
            return -1;
        }

        XlsxXmlValueParseResult leftNumber = XlsxXmlValueParser.ParseDouble(leftText);
        XlsxXmlValueParseResult rightNumber = XlsxXmlValueParser.ParseDouble(rightText);
        if (leftNumber.IsValid && rightNumber.IsValid)
        {
            if (leftNumber.DoubleValue < rightNumber.DoubleValue)
            {
                return -1;
            }

            if (leftNumber.DoubleValue > rightNumber.DoubleValue)
            {
                return 1;
            }

            return 0;
        }

        return string.Compare(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetSortCellText(XlsxWorksheetCell cell, bool hasCell)
    {
        if (!hasCell || cell == null)
        {
            return "";
        }

        if (cell.Value != null && cell.Value.Length > 0)
        {
            return cell.Value;
        }

        if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
        {
            return cell.InlineStringText;
        }

        if (cell.HasInlineString && cell.InlineString != null && cell.InlineString.Text != null && cell.InlineString.Text.Length > 0)
        {
            return cell.InlineString.Text;
        }

        return "";
    }

    internal static XlsxWorksheetCell CloneCell(XlsxWorksheetCell source, string? reference)
    {
        XlsxWorksheetCell clone = new XlsxWorksheetCell();
        clone.Reference = XlsxCellValueConverter.NormalizeText(reference);
        clone.Type = XlsxCellValueConverter.NormalizeText(source.Type);
        clone.Style = source.Style;
        clone.HasStyle = source.HasStyle;
        clone.Value = XlsxCellValueConverter.NormalizeText(source.Value);
        clone.Formula = XlsxCellValueConverter.NormalizeText(source.Formula);
        clone.FormulaInfo = CloneFormula(source.FormulaInfo);
        clone.HasFormula = source.HasFormula;
        clone.InlineStringText = XlsxCellValueConverter.NormalizeText(source.InlineStringText);
        clone.InlineString = CloneInlineString(source.InlineString);
        clone.HasInlineString = source.HasInlineString;
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        foreach (string unknownXml in source.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxCellFormula CloneFormula(XlsxCellFormula source)
    {
        XlsxCellFormula clone = new XlsxCellFormula();
        if (source == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(source.Text);
        clone.Type = XlsxCellValueConverter.NormalizeText(source.Type);
        clone.Reference = XlsxCellValueConverter.NormalizeText(source.Reference);
        clone.SharedIndex = source.SharedIndex;
        clone.HasSharedIndex = source.HasSharedIndex;
        clone.AlwaysCalculateArray = source.AlwaysCalculateArray;
        clone.HasAlwaysCalculateArray = source.HasAlwaysCalculateArray;
        clone.CalculateCell = source.CalculateCell;
        clone.HasCalculateCell = source.HasCalculateCell;
        clone.DataTable2D = source.DataTable2D;
        clone.HasDataTable2D = source.HasDataTable2D;
        clone.DataTableRow = source.DataTableRow;
        clone.HasDataTableRow = source.HasDataTableRow;
        clone.Input1Deleted = source.Input1Deleted;
        clone.HasInput1Deleted = source.HasInput1Deleted;
        clone.Input2Deleted = source.Input2Deleted;
        clone.HasInput2Deleted = source.HasInput2Deleted;
        clone.Input1Reference = XlsxCellValueConverter.NormalizeText(source.Input1Reference);
        clone.Input2Reference = XlsxCellValueConverter.NormalizeText(source.Input2Reference);
        clone.AssignsToName = source.AssignsToName;
        clone.HasAssignsToName = source.HasAssignsToName;
        clone.DynamicArray = source.DynamicArray;
        clone.HasDynamicArray = source.HasDynamicArray;
        clone.CachedValue = XlsxCellValueConverter.NormalizeText(source.CachedValue);
        clone.CachedType = XlsxCellValueConverter.NormalizeText(source.CachedType);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        return clone;
    }

    internal static XlsxInlineString CloneInlineString(XlsxInlineString source)
    {
        XlsxInlineString clone = new XlsxInlineString();
        if (source == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(source.Text);
        clone.IsRichText = source.IsRichText;
        foreach (XlsxSharedStringRichTextRun run in source.RichTextRuns)
        {
            clone.RichTextRuns.Add(CloneRangeRichTextRun(run));
        }

        foreach (XlsxPhoneticText phoneticText in source.PhoneticTexts)
        {
            clone.PhoneticTexts.Add(CloneRangePhoneticText(phoneticText));
        }

        clone.PhoneticPropertiesRawXml = XlsxCellValueConverter.NormalizeText(source.PhoneticPropertiesRawXml);
        clone.PhoneticProperties = CloneRangePhoneticProperties(source.PhoneticProperties);
        clone.HasPhoneticProperties = source.HasPhoneticProperties;
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        foreach (string unknownXml in source.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxXmlAttribute CloneRangeXmlAttribute(XlsxXmlAttribute source)
    {
        XlsxXmlAttribute clone = new XlsxXmlAttribute();
        if (source == null)
        {
            return clone;
        }

        clone.NamespaceUri = XlsxCellValueConverter.NormalizeText(source.NamespaceUri);
        clone.LocalName = XlsxCellValueConverter.NormalizeText(source.LocalName);
        clone.Prefix = XlsxCellValueConverter.NormalizeText(source.Prefix);
        clone.Value = XlsxCellValueConverter.NormalizeText(source.Value);
        return clone;
    }

    internal static XlsxSharedStringRichTextRun CloneRangeRichTextRun(XlsxSharedStringRichTextRun source)
    {
        XlsxSharedStringRichTextRun clone = new XlsxSharedStringRichTextRun();
        if (source == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(source.Text);
        clone.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        clone.PropertiesRawXml = XlsxCellValueConverter.NormalizeText(source.PropertiesRawXml);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        foreach (string unknownXml in source.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxPhoneticText CloneRangePhoneticText(XlsxPhoneticText source)
    {
        XlsxPhoneticText clone = new XlsxPhoneticText();
        if (source == null)
        {
            return clone;
        }

        clone.Text = XlsxCellValueConverter.NormalizeText(source.Text);
        clone.StartBaseIndex = source.StartBaseIndex;
        clone.HasStartBaseIndex = source.HasStartBaseIndex;
        clone.EndBaseIndex = source.EndBaseIndex;
        clone.HasEndBaseIndex = source.HasEndBaseIndex;
        clone.RawXml = XlsxCellValueConverter.NormalizeText(source.RawXml);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        foreach (string unknownXml in source.UnknownXml)
        {
            clone.UnknownXml.Add(unknownXml);
        }

        return clone;
    }

    internal static XlsxPhoneticProperties CloneRangePhoneticProperties(XlsxPhoneticProperties source)
    {
        XlsxPhoneticProperties clone = new XlsxPhoneticProperties();
        if (source == null)
        {
            return clone;
        }

        clone.FontId = source.FontId;
        clone.HasFontId = source.HasFontId;
        clone.Type = XlsxCellValueConverter.NormalizeText(source.Type);
        clone.Alignment = XlsxCellValueConverter.NormalizeText(source.Alignment);
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneRangeXmlAttribute(attribute));
        }

        return clone;
    }

    internal static void CopyCellAccessDiagnostics(XlsxWorksheetRangeResult result, XlsxWorksheetCellAccessResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyCellValueDiagnostics(XlsxWorksheetRangeResult result, XlsxCellValueResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyFormulaReferenceDiagnostics(XlsxWorksheetRangeResult result, XlsxFormulaReferenceUpdateResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
