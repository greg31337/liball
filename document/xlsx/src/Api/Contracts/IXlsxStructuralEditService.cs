using System;

namespace xlsx;

/// <summary>
/// Defines workbook-aware row, column, cell, range, and sheet structural edits.
/// </summary>
public interface IXlsxStructuralEditService
{
    /// <summary>
    /// Inserts rows and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="rowIndex">The one-based insertion row.</param>
    /// <param name="count">The positive row count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult InsertRows(XlsxWorkbook workbook, string sheetName, int rowIndex, int count);
    /// <summary>
    /// Deletes rows and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="rowIndex">The one-based first row.</param>
    /// <param name="count">The positive row count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult DeleteRows(XlsxWorkbook workbook, string sheetName, int rowIndex, int count);
    /// <summary>
    /// Inserts columns and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="columnIndex">The one-based insertion column.</param>
    /// <param name="count">The positive column count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult InsertColumns(XlsxWorkbook workbook, string sheetName, int columnIndex, int count);
    /// <summary>
    /// Deletes columns and updates every supported dependent feature.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="columnIndex">The one-based first column.</param>
    /// <param name="count">The positive column count.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult DeleteColumns(XlsxWorkbook workbook, string sheetName, int columnIndex, int count);
    /// <summary>
    /// Shifts a rectangular cell selection and reports its structural effects.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="reference">The selected A1 range.</param>
    /// <param name="direction">The shift direction.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult ShiftCells(XlsxWorkbook workbook, string sheetName, string reference, XlsxCellShiftDirection direction);
    /// <summary>
    /// Moves a range within or between worksheets and reports its structural effects.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sourceSheetName">The source worksheet name.</param>
    /// <param name="sourceReference">The source A1 range.</param>
    /// <param name="targetSheetName">The destination worksheet name.</param>
    /// <param name="targetTopLeftReference">The destination top-left cell.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult MoveRange(XlsxWorkbook workbook, string sourceSheetName, string sourceReference, string targetSheetName, string targetTopLeftReference);
    /// <summary>
    /// Adds a sheet and synchronizes worksheet and relationship collections.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The new sheet name.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult AddSheet(XlsxWorkbook workbook, string sheetName);
    /// <summary>
    /// Removes a sheet and synchronizes dependent scopes and relationships.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The sheet name to remove.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult RemoveSheet(XlsxWorkbook workbook, string sheetName);
    /// <summary>
    /// Renames a sheet and updates formulas, names, and charts.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="oldSheetName">The existing sheet name.</param>
    /// <param name="newSheetName">The replacement sheet name.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult RenameSheet(XlsxWorkbook workbook, string oldSheetName, string newSheetName);
    /// <summary>
    /// Reorders a sheet and updates worksheet order and local defined-name scopes.
    /// </summary>
    /// <param name="workbook">The workbook to edit.</param>
    /// <param name="sheetName">The sheet name to move.</param>
    /// <param name="newIndex">The zero-based destination index.</param>
    /// <returns>The deterministic structural edit result.</returns>
    XlsxStructuralEditResult ReorderSheet(XlsxWorkbook workbook, string sheetName, int newIndex);
}
