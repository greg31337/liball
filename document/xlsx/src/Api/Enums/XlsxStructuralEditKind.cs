using System;

namespace xlsx;

/// <summary>
/// Lists structural workbook edit operations reported by the structural edit service.
/// </summary>
public enum XlsxStructuralEditKind
{
    /// <summary>
    /// Inserts worksheet rows.
    /// </summary>
    RowInsert = 1,
    /// <summary>
    /// Deletes worksheet rows.
    /// </summary>
    RowDelete = 2,
    /// <summary>
    /// Inserts worksheet columns.
    /// </summary>
    ColumnInsert = 3,
    /// <summary>
    /// Deletes worksheet columns.
    /// </summary>
    ColumnDelete = 4,
    /// <summary>
    /// Shifts a rectangular cell selection.
    /// </summary>
    CellShift = 5,
    /// <summary>
    /// Moves a rectangular range.
    /// </summary>
    RangeMove = 6,
    /// <summary>
    /// Adds a workbook sheet.
    /// </summary>
    SheetAdd = 7,
    /// <summary>
    /// Removes a workbook sheet.
    /// </summary>
    SheetRemove = 8,
    /// <summary>
    /// Renames a workbook sheet.
    /// </summary>
    SheetRename = 9,
    /// <summary>
    /// Reorders a workbook sheet.
    /// </summary>
    SheetReorder = 10
}
