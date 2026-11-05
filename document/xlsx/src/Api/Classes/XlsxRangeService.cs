using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Provides complete range enumeration, mutation, query, and overlap validation operations.
/// </summary>
public class XlsxRangeService : IXlsxRangeService
{
    /// <summary>
    /// Initializes a range service.
    /// </summary>
    public XlsxRangeService()
    {
    }

    /// <summary>
    /// Enumerates range cells in row-major order.
    /// </summary>
    /// <param name="range">The worksheet range to enumerate.</param>
    /// <param name="includeBlankCells">Whether absent cells are included as blank items.</param>
    /// <returns>The enumerated cells.</returns>
    public XlsxRangeEnumerationResult EnumerateCells(XlsxWorksheetRange range, bool includeBlankCells)
    {
        return EnumerateRange(range, includeBlankCells, false, true);
    }

    /// <summary>
    /// Enumerates range rows and their cells.
    /// </summary>
    /// <param name="range">The worksheet range to enumerate.</param>
    /// <param name="includeBlankRows">Whether rows without physical cells are included.</param>
    /// <returns>The enumerated rows.</returns>
    public XlsxRangeEnumerationResult EnumerateRows(XlsxWorksheetRange range, bool includeBlankRows)
    {
        return EnumerateRange(range, true, true, includeBlankRows);
    }

    /// <summary>
    /// Gets typed values for every coordinate in a range.
    /// </summary>
    /// <param name="range">The worksheet range to read.</param>
    /// <returns>The row-major typed values.</returns>
    public XlsxRangeEnumerationResult GetRangeValues(XlsxWorksheetRange range)
    {
        return EnumerateRange(range, true, false, true);
    }

    /// <summary>
    /// Sets typed values in row-major order.
    /// </summary>
    /// <param name="range">The worksheet range to change.</param>
    /// <param name="values">One typed value for every coordinate.</param>
    /// <returns>The range mutation result.</returns>
    public XlsxWorksheetRangeResult SetRangeValues(XlsxWorksheetRange range, List<XlsxCellValue> values)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        if (values == null)
        {
            result.AddError("worksheetRange.valuesNull", "Range values list must not be null.");
            return result;
        }
        long expected = (long)(range.LastRow - range.FirstRow + 1) * (long)(range.LastColumn - range.FirstColumn + 1);
        if (expected != values.Count)
        {
            result.AddError("worksheetRange.valueCountMismatch", "Range values list must contain one item for every cell in row-major order.");
            return result;
        }

        int valueIndex = 0;
        int row = range.FirstRow;
        while (row <= range.LastRow)
        {
            int column = range.FirstColumn;
            while (column <= range.LastColumn)
            {
                XlsxCellValue value = values[valueIndex];
                if (value == null)
                {
                    result.AddError("worksheetRange.valueNull", "Range values must not contain null items.");
                    return result;
                }
                XlsxWorksheetCellAccessResult access = range.Worksheet.GetOrCreateCell(row, column);
                XlsxWorksheetRange.CopyCellAccessDiagnostics(result, access);
                if (access.IsValid && access.HasCell && access.Cell != null)
                {
                    XlsxCellValueResult applied = XlsxCellValueConverter.ApplyToWorksheetCell(value, access.Cell);
                    XlsxWorksheetRange.CopyCellValueDiagnostics(result, applied);
                    if (applied.IsValid)
                    {
                        result.AffectedCellCount = result.AffectedCellCount + 1;
                        result.FilledCellCount = result.FilledCellCount + 1;
                        if (access.CreatedCell)
                        {
                            result.CreatedCellCount = result.CreatedCellCount + 1;
                        }
                    }
                }
                valueIndex = valueIndex + 1;
                column = column + 1;
            }
            row = row + 1;
        }
        return result;
    }

    /// <summary>
    /// Clears values and formulas without clearing styles.
    /// </summary>
    /// <param name="range">The worksheet range to clear.</param>
    /// <returns>The range mutation result.</returns>
    public XlsxWorksheetRangeResult ClearRangeValues(XlsxWorksheetRange range)
    {
        return range.ClearValues();
    }

    /// <summary>
    /// Merges the complete range.
    /// </summary>
    /// <param name="range">The worksheet range to merge.</param>
    /// <returns>The merge result.</returns>
    public XlsxWorksheetMergeCellsResult MergeRange(XlsxWorksheetRange range)
    {
        if (range == null || range.Worksheet == null)
        {
            XlsxWorksheetMergeCellsResult missing = new XlsxWorksheetMergeCellsResult();
            missing.AddError("worksheetRange.worksheetNull", "Worksheet range requires a worksheet part.");
            return missing;
        }
        return range.Worksheet.MergeCells(range.Reference);
    }

    /// <summary>
    /// Removes merged cells overlapping the range.
    /// </summary>
    /// <param name="range">The worksheet range to unmerge.</param>
    /// <returns>The unmerge result.</returns>
    public XlsxWorksheetMergeCellsResult UnmergeRange(XlsxWorksheetRange range)
    {
        if (range == null || range.Worksheet == null)
        {
            XlsxWorksheetMergeCellsResult missing = new XlsxWorksheetMergeCellsResult();
            missing.AddError("worksheetRange.worksheetNull", "Worksheet range requires a worksheet part.");
            return missing;
        }
        return range.Worksheet.UnmergeCells(range.Reference);
    }

    /// <summary>
    /// Copies a range to another worksheet.
    /// </summary>
    /// <param name="range">The source range.</param>
    /// <param name="targetWorksheet">The destination worksheet.</param>
    /// <param name="topLeftReference">The destination top-left cell.</param>
    /// <returns>The copy result.</returns>
    public XlsxWorksheetRangeResult CopyAcrossSheets(XlsxWorksheetRange range, XlsxWorksheetPart targetWorksheet, string topLeftReference)
    {
        return TransferAcrossSheets(range, targetWorksheet, topLeftReference, false);
    }

    /// <summary>
    /// Moves a range to another worksheet and clears its source.
    /// </summary>
    /// <param name="range">The source range.</param>
    /// <param name="targetWorksheet">The destination worksheet.</param>
    /// <param name="topLeftReference">The destination top-left cell.</param>
    /// <returns>The move result.</returns>
    public XlsxWorksheetRangeResult MoveAcrossSheets(XlsxWorksheetRange range, XlsxWorksheetPart targetWorksheet, string topLeftReference)
    {
        return TransferAcrossSheets(range, targetWorksheet, topLeftReference, true);
    }

    /// <summary>
    /// Inserts blank cells and shifts cells down by the range height.
    /// </summary>
    /// <param name="range">The insertion range.</param>
    /// <returns>The shift result.</returns>
    public XlsxWorksheetRangeResult ShiftCellsDown(XlsxWorksheetRange range)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        XlsxWorksheetUsedRangeResult used = range.Worksheet.CalculateUsedRange();
        CopyUsedDiagnostics(result, used);
        int maxRow = FindMaxUsedRowInColumns(range.Worksheet, range.FirstColumn, range.LastColumn, range.LastRow);
        int height = range.LastRow - range.FirstRow + 1;
        if (!XlsxWorksheetPart.IsValidCellRow(maxRow + height))
        {
            result.AddError("worksheetRange.shiftOutsideWorksheet", "Shifted cells must remain within worksheet row bounds.");
            return result;
        }
        string source = FormatRange(range.FirstRow, range.FirstColumn, maxRow, range.LastColumn);
        string target = XlsxA1CellReferenceConverter.FormatString(range.FirstRow + height, range.FirstColumn, false, false);
        return range.Worksheet.GetRange(source).MoveTo(target);
    }

    /// <summary>
    /// Deletes cells and shifts lower cells up by the range height.
    /// </summary>
    /// <param name="range">The deletion range.</param>
    /// <returns>The shift result.</returns>
    public XlsxWorksheetRangeResult ShiftCellsUp(XlsxWorksheetRange range)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        XlsxWorksheetUsedRangeResult used = range.Worksheet.CalculateUsedRange();
        CopyUsedDiagnostics(result, used);
        if (!result.IsValid)
        {
            return result;
        }
        int maxRow = FindMaxUsedRowInColumns(range.Worksheet, range.FirstColumn, range.LastColumn, range.LastRow);
        if (!used.HasRange || maxRow <= range.LastRow)
        {
            return range.Clear();
        }
        string source = FormatRange(range.LastRow + 1, range.FirstColumn, maxRow, range.LastColumn);
        string target = XlsxA1CellReferenceConverter.FormatString(range.FirstRow, range.FirstColumn, false, false);
        return range.Worksheet.GetRange(source).MoveTo(target);
    }

    /// <summary>
    /// Inserts blank cells and shifts cells right by the range width.
    /// </summary>
    /// <param name="range">The insertion range.</param>
    /// <returns>The shift result.</returns>
    public XlsxWorksheetRangeResult ShiftCellsRight(XlsxWorksheetRange range)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        XlsxWorksheetUsedRangeResult used = range.Worksheet.CalculateUsedRange();
        CopyUsedDiagnostics(result, used);
        int maxColumn = FindMaxUsedColumnInRows(range.Worksheet, range.FirstRow, range.LastRow, range.LastColumn);
        int width = range.LastColumn - range.FirstColumn + 1;
        if (!XlsxWorksheetPart.IsValidCellColumn(maxColumn + width))
        {
            result.AddError("worksheetRange.shiftOutsideWorksheet", "Shifted cells must remain within worksheet column bounds.");
            return result;
        }
        string source = FormatRange(range.FirstRow, range.FirstColumn, range.LastRow, maxColumn);
        string target = XlsxA1CellReferenceConverter.FormatString(range.FirstRow, range.FirstColumn + width, false, false);
        return range.Worksheet.GetRange(source).MoveTo(target);
    }

    /// <summary>
    /// Deletes cells and shifts right-side cells left by the range width.
    /// </summary>
    /// <param name="range">The deletion range.</param>
    /// <returns>The shift result.</returns>
    public XlsxWorksheetRangeResult ShiftCellsLeft(XlsxWorksheetRange range)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        XlsxWorksheetUsedRangeResult used = range.Worksheet.CalculateUsedRange();
        CopyUsedDiagnostics(result, used);
        if (!result.IsValid)
        {
            return result;
        }
        int maxColumn = FindMaxUsedColumnInRows(range.Worksheet, range.FirstRow, range.LastRow, range.LastColumn);
        if (!used.HasRange || maxColumn <= range.LastColumn)
        {
            return range.Clear();
        }
        string source = FormatRange(range.FirstRow, range.LastColumn + 1, range.LastRow, maxColumn);
        string target = XlsxA1CellReferenceConverter.FormatString(range.FirstRow, range.FirstColumn, false, false);
        return range.Worksheet.GetRange(source).MoveTo(target);
    }

    /// <summary>
    /// Queries used, data, first, and last worksheet cells.
    /// </summary>
    /// <param name="worksheet">The worksheet to query.</param>
    /// <returns>The worksheet range query result.</returns>
    public XlsxRangeQueryResult QueryWorksheetRanges(XlsxWorksheetPart worksheet)
    {
        XlsxRangeQueryResult result = new XlsxRangeQueryResult();
        if (worksheet == null || worksheet.Rows == null)
        {
            result.AddError("worksheetRange.worksheetNull", "A worksheet with rows is required for range queries.");
            return result;
        }
        XlsxWorksheetUsedRangeResult used = worksheet.CalculateUsedRange();
        foreach (XlsxDiagnostic diagnostic in used.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (used.HasRange)
        {
            result.HasUsedRange = true;
            result.UsedRange = used.Reference;
        }

        int[] dataBounds = new int[4];
        int[] usedOrder = new int[4];
        int[] dataOrder = new int[4];
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null)
                    {
                        int rowIndex = worksheet.GetEffectiveRowIndex(row, rowSequence);
                        int columnIndex = worksheet.GetEffectiveCellColumn(cell, cellSequence);
                        if (XlsxWorksheetPart.IsUsedCell(cell))
                        {
                            UpdateFirstLast(rowIndex, columnIndex, usedOrder);
                        }
                        if (HasCellData(cell))
                        {
                            UpdateBounds(rowIndex, columnIndex, dataBounds);
                            UpdateFirstLast(rowIndex, columnIndex, dataOrder);
                        }
                    }
                    cellSequence = cellSequence + 1;
                }
            }
            rowSequence = rowSequence + 1;
        }
        if (usedOrder[0] > 0)
        {
            result.FirstUsedCell = XlsxA1CellReferenceConverter.FormatString(usedOrder[0], usedOrder[1], false, false);
            result.LastUsedCell = XlsxA1CellReferenceConverter.FormatString(usedOrder[2], usedOrder[3], false, false);
        }
        if (dataBounds[0] > 0)
        {
            result.HasDataRange = true;
            result.DataRange = FormatRange(dataBounds[0], dataBounds[1], dataBounds[2], dataBounds[3]);
            result.FirstDataCell = XlsxA1CellReferenceConverter.FormatString(dataOrder[0], dataOrder[1], false, false);
            result.LastDataCell = XlsxA1CellReferenceConverter.FormatString(dataOrder[2], dataOrder[3], false, false);
        }
        return result;
    }

    /// <summary>
    /// Applies an AutoFilter to the range.
    /// </summary>
    /// <param name="range">The filter range.</param>
    /// <returns>The filter result.</returns>
    public XlsxWorksheetFilterResult ApplyRangeFilter(XlsxWorksheetRange range)
    {
        if (range == null || range.Worksheet == null)
        {
            XlsxWorksheetFilterResult missing = new XlsxWorksheetFilterResult();
            missing.AddError("worksheetRange.worksheetNull", "Worksheet range requires a worksheet part.");
            return missing;
        }
        return range.Worksheet.AddAutoFilter(range.Reference);
    }

    /// <summary>
    /// Resizes a global or worksheet-scoped defined range.
    /// </summary>
    /// <param name="workbook">The workbook containing the defined name.</param>
    /// <param name="name">The defined name.</param>
    /// <param name="localSheetId">The zero-based local scope, or a negative value for global scope.</param>
    /// <param name="reference">The new A1 range, optionally sheet-qualified.</param>
    /// <returns>The named-range resize result.</returns>
    public XlsxNamedRangeResizeResult ResizeNamedRange(XlsxWorkbook workbook, string name, int localSheetId, string reference)
    {
        XlsxNamedRangeResizeResult result = new XlsxNamedRangeResizeResult();
        if (workbook == null || workbook.WorkbookPart == null)
        {
            result.AddError("namedRange.workbookMissing", "A workbook is required to resize a named range.");
            return result;
        }
        XlsxWorkbookDefinedName? definedName = null;
        if (localSheetId < 0)
        {
            definedName = workbook.WorkbookPart.GetGlobalDefinedName(name);
        }
        else
        {
            definedName = workbook.WorkbookPart.GetLocalDefinedName(name, localSheetId);
        }
        if (definedName == null)
        {
            result.AddError("namedRange.notFound", "The requested defined range was not found in the requested scope.");
            return result;
        }
        XlsxReferenceResolutionResult parsedNew = XlsxWorkbookReferenceService.ParseA1Core(reference);
        if (!parsedNew.IsValid || !parsedNew.HasA1Range || parsedNew.IsExternal || parsedNew.IsThreeDimensional)
        {
            result.AddError("namedRange.referenceInvalid", "Named range must use a valid local A1 cell or range reference.");
            return result;
        }
        string resized = parsedNew.NormalizedText;
        XlsxReferenceResolutionResult parsedOld = XlsxWorkbookReferenceService.ParseA1Core(definedName.Text);
        if (!parsedNew.HasSheetName && parsedOld.IsValid && parsedOld.HasSheetName)
        {
            resized = QuoteSheetName(parsedOld.FirstSheetName) + "!" + parsedNew.A1Range.Text;
        }
        result.DefinedName = definedName;
        result.HasDefinedName = true;
        result.PreviousReference = definedName.Text;
        definedName.Text = resized;
        result.Reference = resized;
        return result;
    }

    /// <summary>
    /// Validates overlap with merges, tables, validations, filters, and pivots.
    /// </summary>
    /// <param name="workbook">The workbook containing the worksheet and pivot definitions.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="reference">The candidate A1 range.</param>
    /// <returns>The overlap result.</returns>
    public XlsxRangeOverlapResult ValidateRangeOverlaps(XlsxWorkbook workbook, string sheetName, string reference)
    {
        XlsxRangeOverlapResult result = new XlsxRangeOverlapResult();
        XlsxWorksheetPart? worksheet = FindWorksheet(workbook, sheetName);
        if (worksheet == null)
        {
            result.AddError("rangeOverlap.worksheetMissing", "The requested worksheet was not found.");
            return result;
        }
        XlsxA1RangeReferenceResult candidate = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!candidate.IsValid)
        {
            result.AddError("rangeOverlap.referenceInvalid", "Overlap candidate must be a valid A1 range.");
            return result;
        }
        foreach (XlsxWorksheetMergeCell merge in worksheet.MergedCells)
        {
            string mergeReference = "";
            if (merge != null)
            {
                mergeReference = XlsxCellValueConverter.NormalizeText(merge.Reference);
            }
            if (merge != null && Overlaps(candidate, mergeReference))
            {
                AddOverlap(result.MergeReferences, mergeReference, result);
            }
        }
        foreach (XlsxTablePart table in worksheet.Tables)
        {
            if (table != null && Overlaps(candidate, table.Reference))
            {
                AddOverlap(result.TableReferences, table.Reference, result);
            }
        }
        if (worksheet.DataValidations != null && worksheet.DataValidations.Items != null)
        {
            foreach (XlsxDataValidation validation in worksheet.DataValidations.Items)
            {
                if (validation != null)
                {
                    AddSquareReferenceOverlaps(candidate, validation.Sqref, result.ValidationReferences, result);
                }
            }
        }
        if (worksheet.HasAutoFilter && worksheet.AutoFilter != null && Overlaps(candidate, worksheet.AutoFilter.Reference))
        {
            AddOverlap(result.FilterReferences, worksheet.AutoFilter.Reference, result);
        }
        AddPivotOverlaps(workbook, worksheet, candidate, result);
        return result;
    }

    internal static XlsxRangeEnumerationResult EnumerateRange(XlsxWorksheetRange range, bool includeBlankCells, bool groupRows, bool includeBlankRows)
    {
        XlsxRangeEnumerationResult result = new XlsxRangeEnumerationResult();
        XlsxWorksheetRangeResult validation = ValidateRange(range);
        foreach (XlsxDiagnostic diagnostic in validation.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        result.Reference = range.Reference;
        int row = range.FirstRow;
        while (row <= range.LastRow)
        {
            XlsxRangeRowItem rowItem = new XlsxRangeRowItem();
            rowItem.Row = row;
            bool hasPhysicalCell = false;
            int column = range.FirstColumn;
            while (column <= range.LastColumn)
            {
                XlsxRangeCellItem item = CreateCellItem(range.Worksheet, row, column);
                if (item.HasCell)
                {
                    hasPhysicalCell = true;
                }
                if (includeBlankCells || item.HasCell)
                {
                    rowItem.Cells.Add(item);
                    if (!groupRows)
                    {
                        result.Cells.Add(item);
                        result.Values.Add(item.Value);
                    }
                }
                column = column + 1;
            }
            if (groupRows && (includeBlankRows || hasPhysicalCell))
            {
                result.Rows.Add(rowItem);
                foreach (XlsxRangeCellItem item in rowItem.Cells)
                {
                    result.Cells.Add(item);
                    result.Values.Add(item.Value);
                }
            }
            row = row + 1;
        }
        return result;
    }

    internal static XlsxRangeCellItem CreateCellItem(XlsxWorksheetPart worksheet, int row, int column)
    {
        XlsxRangeCellItem item = new XlsxRangeCellItem();
        item.Row = row;
        item.Column = column;
        item.Reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
        item.Cell = worksheet.GetCellAt(row, column);
        item.HasCell = item.Cell != null;
        if (item.Cell != null)
        {
            XlsxCellValueResult value = XlsxCellValueConverter.FromWorksheetCell(item.Cell);
            if (value.IsValid)
            {
                item.Value = value.Value;
            }
        }
        return item;
    }

    internal static XlsxWorksheetRangeResult TransferAcrossSheets(XlsxWorksheetRange range, XlsxWorksheetPart targetWorksheet, string topLeftReference, bool move)
    {
        XlsxWorksheetRangeResult result = ValidateRange(range);
        if (!result.IsValid || range.Worksheet == null)
        {
            return result;
        }
        if (targetWorksheet == null)
        {
            result.AddError("worksheetRange.targetWorksheetNull", "Cross-sheet range transfer requires a destination worksheet.");
            return result;
        }
        XlsxA1CellReferenceResult target = XlsxA1CellReferenceConverter.Parse(topLeftReference);
        foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        int height = range.LastRow - range.FirstRow + 1;
        int width = range.LastColumn - range.FirstColumn + 1;
        if (!target.IsValid || !XlsxWorksheetPart.IsValidCellCoordinate(target.Row + height - 1, target.Column + width - 1))
        {
            result.AddError("worksheetRange.targetOutsideWorksheet", "Cross-sheet range target must fit within worksheet bounds.");
            return result;
        }
        List<XlsxWorksheetRangeCellSnapshot> snapshots = range.CreateSnapshots();
        foreach (XlsxWorksheetRangeCellSnapshot snapshot in snapshots)
        {
            int row = target.Row + snapshot.RowOffset;
            int column = target.Column + snapshot.ColumnOffset;
            if (snapshot.HasCell && snapshot.Cell != null)
            {
                string cellReference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
                XlsxWorksheetCell clone = XlsxWorksheetRange.CloneCell(snapshot.Cell, cellReference);
                if (!move)
                {
                    XlsxWorksheetRange.TranslateCopiedCellFormula(result, clone, target.Row - range.FirstRow, target.Column - range.FirstColumn);
                }
                XlsxWorksheetCellAccessResult access = targetWorksheet.SetCell(row, column, clone);
                XlsxWorksheetRange.CopyCellAccessDiagnostics(result, access);
                if (access.IsValid)
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
                XlsxWorksheetCell? existing = targetWorksheet.GetCellAt(row, column);
                if (existing != null)
                {
                    XlsxCellValueConverter.ClearTypedCellContent(existing);
                    existing.Style = 0;
                    existing.HasStyle = false;
                    result.AffectedCellCount = result.AffectedCellCount + 1;
                    result.ClearedCellCount = result.ClearedCellCount + 1;
                }
            }
        }
        if (move && result.IsValid)
        {
            XlsxWorksheetRangeResult cleared = range.Clear();
            foreach (XlsxDiagnostic diagnostic in cleared.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
            result.ClearedCellCount = result.ClearedCellCount + cleared.ClearedCellCount;
        }
        return result;
    }

    internal static XlsxWorksheetRangeResult ValidateRange(XlsxWorksheetRange range)
    {
        if (range == null)
        {
            XlsxWorksheetRangeResult missing = new XlsxWorksheetRangeResult();
            missing.AddError("worksheetRange.rangeNull", "Worksheet range must not be null.");
            return missing;
        }
        return range.Validate();
    }

    internal static void CopyUsedDiagnostics(XlsxWorksheetRangeResult result, XlsxWorksheetUsedRangeResult used)
    {
        foreach (XlsxDiagnostic diagnostic in used.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static bool HasCellData(XlsxWorksheetCell cell)
    {
        return cell != null && ((cell.Value != null && cell.Value.Length > 0) || (cell.Formula != null && cell.Formula.Length > 0) || cell.HasFormula || (cell.InlineStringText != null && cell.InlineStringText.Length > 0) || cell.HasInlineString);
    }

    internal static int FindMaxUsedRowInColumns(XlsxWorksheetPart worksheet, int firstColumn, int lastColumn, int minimum)
    {
        int maximum = minimum;
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && XlsxWorksheetPart.IsUsedCell(cell))
                    {
                        int column = worksheet.GetEffectiveCellColumn(cell, cellSequence);
                        int rowIndex = worksheet.GetEffectiveRowIndex(row, rowSequence);
                        if (column >= firstColumn && column <= lastColumn && rowIndex > maximum)
                        {
                            maximum = rowIndex;
                        }
                    }
                    cellSequence = cellSequence + 1;
                }
            }
            rowSequence = rowSequence + 1;
        }
        return maximum;
    }

    internal static int FindMaxUsedColumnInRows(XlsxWorksheetPart worksheet, int firstRow, int lastRow, int minimum)
    {
        int maximum = minimum;
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                int rowIndex = worksheet.GetEffectiveRowIndex(row, rowSequence);
                if (rowIndex >= firstRow && rowIndex <= lastRow)
                {
                    int cellSequence = 1;
                    foreach (XlsxWorksheetCell cell in row.Cells)
                    {
                        if (cell != null && XlsxWorksheetPart.IsUsedCell(cell))
                        {
                            int column = worksheet.GetEffectiveCellColumn(cell, cellSequence);
                            if (column > maximum)
                            {
                                maximum = column;
                            }
                        }
                        cellSequence = cellSequence + 1;
                    }
                }
            }
            rowSequence = rowSequence + 1;
        }
        return maximum;
    }

    internal static void UpdateBounds(int row, int column, int[] bounds)
    {
        if (bounds[0] == 0 || row < bounds[0])
        {
            bounds[0] = row;
        }
        if (bounds[1] == 0 || column < bounds[1])
        {
            bounds[1] = column;
        }
        if (row > bounds[2])
        {
            bounds[2] = row;
        }
        if (column > bounds[3])
        {
            bounds[3] = column;
        }
    }

    internal static void UpdateFirstLast(int row, int column, int[] order)
    {
        if (order[0] == 0 || row < order[0] || (row == order[0] && column < order[1]))
        {
            order[0] = row;
            order[1] = column;
        }
        if (order[2] == 0 || row > order[2] || (row == order[2] && column > order[3]))
        {
            order[2] = row;
            order[3] = column;
        }
    }

    internal static string FormatRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        string first = XlsxA1CellReferenceConverter.FormatString(firstRow, firstColumn, false, false);
        string last = XlsxA1CellReferenceConverter.FormatString(lastRow, lastColumn, false, false);
        if (first == last)
        {
            return first;
        }
        return first + ":" + last;
    }

    internal static string QuoteSheetName(string sheetName)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(sheetName);
        bool quote = normalized.IndexOf(' ') >= 0 || normalized.IndexOf('\'') >= 0 || normalized.IndexOf('!') >= 0;
        normalized = normalized.Replace("'", "''", StringComparison.Ordinal);
        if (quote)
        {
            return "'" + normalized + "'";
        }
        return normalized;
    }

    internal static XlsxWorksheetPart? FindWorksheet(XlsxWorkbook workbook, string sheetName)
    {
        if (workbook == null || workbook.Worksheets == null)
        {
            return null;
        }
        int index = 0;
        while (index < workbook.Worksheets.Count)
        {
            if (string.Equals(XlsxFormulaService.GetSheetName(workbook, index), sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return workbook.Worksheets[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool Overlaps(XlsxA1RangeReferenceResult candidate, string reference)
    {
        XlsxA1RangeReferenceResult other = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!other.IsValid)
        {
            return false;
        }
        int candidateFirstRow = Math.Min(candidate.First.Row, candidate.Last.Row);
        int candidateLastRow = Math.Max(candidate.First.Row, candidate.Last.Row);
        int candidateFirstColumn = Math.Min(candidate.First.Column, candidate.Last.Column);
        int candidateLastColumn = Math.Max(candidate.First.Column, candidate.Last.Column);
        int otherFirstRow = Math.Min(other.First.Row, other.Last.Row);
        int otherLastRow = Math.Max(other.First.Row, other.Last.Row);
        int otherFirstColumn = Math.Min(other.First.Column, other.Last.Column);
        int otherLastColumn = Math.Max(other.First.Column, other.Last.Column);
        return candidateFirstRow <= otherLastRow && candidateLastRow >= otherFirstRow && candidateFirstColumn <= otherLastColumn && candidateLastColumn >= otherFirstColumn;
    }

    internal static void AddSquareReferenceOverlaps(XlsxA1RangeReferenceResult candidate, string squareReference, List<string> references, XlsxRangeOverlapResult result)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(squareReference);
        string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            if (Overlaps(candidate, part))
            {
                AddOverlap(references, part, result);
            }
        }
    }

    internal static void AddPivotOverlaps(XlsxWorkbook workbook, XlsxWorksheetPart worksheet, XlsxA1RangeReferenceResult candidate, XlsxRangeOverlapResult result)
    {
        if (workbook == null || workbook.PivotTableDefinitions == null || worksheet.PivotTableReferences == null)
        {
            return;
        }
        foreach (XlsxWorksheetPivotTableReference pivotReference in worksheet.PivotTableReferences)
        {
            if (pivotReference != null)
            {
                foreach (XlsxPivotTableDefinitionPart pivot in workbook.PivotTableDefinitions)
                {
                    if (pivot != null && pivot.HasLocation && pivot.Location != null && PivotPathMatches(pivotReference, pivot) && Overlaps(candidate, pivot.Location.Reference))
                    {
                        AddOverlap(result.PivotReferences, pivot.Location.Reference, result);
                    }
                }
            }
        }
    }

    internal static bool PivotPathMatches(XlsxWorksheetPivotTableReference reference, XlsxPivotTableDefinitionPart pivot)
    {
        string targetPath = XlsxCellValueConverter.NormalizeText(reference.TargetPath);
        string target = XlsxCellValueConverter.NormalizeText(reference.Target);
        return string.Equals(targetPath, pivot.PartPath, StringComparison.OrdinalIgnoreCase) || string.Equals(target, pivot.PartPath, StringComparison.OrdinalIgnoreCase) || targetPath.EndsWith("/" + GetFileName(pivot.PartPath), StringComparison.OrdinalIgnoreCase) || target.EndsWith("/" + GetFileName(pivot.PartPath), StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetFileName(string path)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(path);
        int slash = normalized.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < normalized.Length)
        {
            return normalized.Substring(slash + 1);
        }
        return normalized;
    }

    internal static void AddOverlap(List<string> references, string reference, XlsxRangeOverlapResult result)
    {
        foreach (string existing in references)
        {
            if (string.Equals(existing, reference, StringComparison.OrdinalIgnoreCase))
            {
                result.HasOverlap = true;
                return;
            }
        }
        references.Add(reference);
        result.HasOverlap = true;
    }

}
