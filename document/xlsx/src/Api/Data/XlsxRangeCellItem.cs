using System;

namespace xlsx;

/// <summary>
/// Represents one coordinate returned by range enumeration.
/// </summary>
public class XlsxRangeCellItem
{
    /// <summary>
    /// Stores the one-based row index.
    /// </summary>
    public int Row;
    /// <summary>
    /// Stores the one-based column index.
    /// </summary>
    public int Column;
    /// <summary>
    /// Stores the A1 cell reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Indicates whether the worksheet contains a physical cell at this coordinate.
    /// </summary>
    public bool HasCell;
    /// <summary>
    /// Stores the physical worksheet cell when present.
    /// </summary>
    public XlsxWorksheetCell? Cell;
    /// <summary>
    /// Stores the typed cell value, using blank for absent cells.
    /// </summary>
    public XlsxCellValue Value;

    /// <summary>
    /// Initializes an empty range cell item.
    /// </summary>
    public XlsxRangeCellItem()
    {
        Row = 0;
        Column = 0;
        Reference = "";
        HasCell = false;
        Cell = null;
        Value = XlsxCellValue.CreateBlank();
    }
}
