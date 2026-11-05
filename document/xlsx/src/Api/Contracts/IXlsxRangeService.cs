using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Defines complete range enumeration, mutation, query, and validation operations.
/// </summary>
public interface IXlsxRangeService
{
    /// <summary>
    /// Enumerates range cells in row-major order.
    /// </summary>
    /// <param name="range">The worksheet range to enumerate.</param>
    /// <param name="includeBlankCells">Whether absent cells are included as blank items.</param>
    /// <returns>The enumerated cells.</returns>
    XlsxRangeEnumerationResult EnumerateCells(XlsxWorksheetRange range, bool includeBlankCells);
    /// <summary>
    /// Enumerates range rows and their cells.
    /// </summary>
    /// <param name="range">The worksheet range to enumerate.</param>
    /// <param name="includeBlankRows">Whether rows without physical cells are included.</param>
    /// <returns>The enumerated rows.</returns>
    XlsxRangeEnumerationResult EnumerateRows(XlsxWorksheetRange range, bool includeBlankRows);
    /// <summary>
    /// Gets typed values for every coordinate in a range.
    /// </summary>
    /// <param name="range">The worksheet range to read.</param>
    /// <returns>The row-major typed values.</returns>
    XlsxRangeEnumerationResult GetRangeValues(XlsxWorksheetRange range);
    /// <summary>
    /// Sets typed values in row-major order.
    /// </summary>
    /// <param name="range">The worksheet range to change.</param>
    /// <param name="values">One typed value for every coordinate.</param>
    /// <returns>The range mutation result.</returns>
    XlsxWorksheetRangeResult SetRangeValues(XlsxWorksheetRange range, List<XlsxCellValue> values);
    /// <summary>
    /// Clears values and formulas without clearing styles.
    /// </summary>
    /// <param name="range">The worksheet range to clear.</param>
    /// <returns>The range mutation result.</returns>
    XlsxWorksheetRangeResult ClearRangeValues(XlsxWorksheetRange range);
    /// <summary>
    /// Merges the complete range.
    /// </summary>
    /// <param name="range">The worksheet range to merge.</param>
    /// <returns>The merge result.</returns>
    XlsxWorksheetMergeCellsResult MergeRange(XlsxWorksheetRange range);
    /// <summary>
    /// Removes merged cells overlapping the range.
    /// </summary>
    /// <param name="range">The worksheet range to unmerge.</param>
    /// <returns>The unmerge result.</returns>
    XlsxWorksheetMergeCellsResult UnmergeRange(XlsxWorksheetRange range);
    /// <summary>
    /// Copies a range to another worksheet.
    /// </summary>
    /// <param name="range">The source range.</param>
    /// <param name="targetWorksheet">The destination worksheet.</param>
    /// <param name="topLeftReference">The destination top-left cell.</param>
    /// <returns>The copy result.</returns>
    XlsxWorksheetRangeResult CopyAcrossSheets(XlsxWorksheetRange range, XlsxWorksheetPart targetWorksheet, string topLeftReference);
    /// <summary>
    /// Moves a range to another worksheet and clears its source.
    /// </summary>
    /// <param name="range">The source range.</param>
    /// <param name="targetWorksheet">The destination worksheet.</param>
    /// <param name="topLeftReference">The destination top-left cell.</param>
    /// <returns>The move result.</returns>
    XlsxWorksheetRangeResult MoveAcrossSheets(XlsxWorksheetRange range, XlsxWorksheetPart targetWorksheet, string topLeftReference);
    /// <summary>
    /// Inserts blank cells and shifts cells down by the range height.
    /// </summary>
    /// <param name="range">The insertion range.</param>
    /// <returns>The shift result.</returns>
    XlsxWorksheetRangeResult ShiftCellsDown(XlsxWorksheetRange range);
    /// <summary>
    /// Deletes cells and shifts lower cells up by the range height.
    /// </summary>
    /// <param name="range">The deletion range.</param>
    /// <returns>The shift result.</returns>
    XlsxWorksheetRangeResult ShiftCellsUp(XlsxWorksheetRange range);
    /// <summary>
    /// Inserts blank cells and shifts cells right by the range width.
    /// </summary>
    /// <param name="range">The insertion range.</param>
    /// <returns>The shift result.</returns>
    XlsxWorksheetRangeResult ShiftCellsRight(XlsxWorksheetRange range);
    /// <summary>
    /// Deletes cells and shifts right-side cells left by the range width.
    /// </summary>
    /// <param name="range">The deletion range.</param>
    /// <returns>The shift result.</returns>
    XlsxWorksheetRangeResult ShiftCellsLeft(XlsxWorksheetRange range);
    /// <summary>
    /// Queries used, data, first, and last worksheet cells.
    /// </summary>
    /// <param name="worksheet">The worksheet to query.</param>
    /// <returns>The worksheet range query result.</returns>
    XlsxRangeQueryResult QueryWorksheetRanges(XlsxWorksheetPart worksheet);
    /// <summary>
    /// Applies an AutoFilter to the range.
    /// </summary>
    /// <param name="range">The filter range.</param>
    /// <returns>The filter result.</returns>
    XlsxWorksheetFilterResult ApplyRangeFilter(XlsxWorksheetRange range);
    /// <summary>
    /// Resizes a global or worksheet-scoped defined range.
    /// </summary>
    /// <param name="workbook">The workbook containing the defined name.</param>
    /// <param name="name">The defined name.</param>
    /// <param name="localSheetId">The zero-based local scope, or a negative value for global scope.</param>
    /// <param name="reference">The new A1 range, optionally sheet-qualified.</param>
    /// <returns>The named-range resize result.</returns>
    XlsxNamedRangeResizeResult ResizeNamedRange(XlsxWorkbook workbook, string name, int localSheetId, string reference);
    /// <summary>
    /// Validates overlap with merges, tables, validations, filters, and pivots.
    /// </summary>
    /// <param name="workbook">The workbook containing the worksheet and pivot definitions.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="reference">The candidate A1 range.</param>
    /// <returns>The overlap result.</returns>
    XlsxRangeOverlapResult ValidateRangeOverlaps(XlsxWorkbook workbook, string sheetName, string reference);
}
