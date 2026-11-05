namespace docx;

/// <summary>
/// Reads and applies direct table, row, and cell formatting.
/// </summary>
public interface IDocxTableFormatting
{
    /// <summary>
    /// Returns whether a table has direct formatting.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasTableFormat(DocxTable table);

    /// <summary>
    /// Returns a table's direct format model.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The table format model.</returns>
    DocxTableFormat GetTableFormat(DocxTable table);

    /// <summary>
    /// Replaces a table's direct format model.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="format">The replacement format.</param>
    void SetTableFormat(DocxTable table, DocxTableFormat format);

    /// <summary>
    /// Returns whether a row has direct formatting.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasRowFormat(DocxTableRow row);

    /// <summary>
    /// Returns a row's direct format model.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>The row format model.</returns>
    DocxTableRowFormat GetRowFormat(DocxTableRow row);

    /// <summary>
    /// Replaces a row's direct format model.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="format">The replacement format.</param>
    void SetRowFormat(DocxTableRow row, DocxTableRowFormat format);

    /// <summary>
    /// Returns whether a cell has direct formatting.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasCellFormat(DocxTableCell cell);

    /// <summary>
    /// Returns a cell's direct format model.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>The cell format model.</returns>
    DocxTableCellFormat GetCellFormat(DocxTableCell cell);

    /// <summary>
    /// Replaces a cell's direct format model.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="format">The replacement format.</param>
    void SetCellFormat(DocxTableCell cell, DocxTableCellFormat format);

    /// <summary>
    /// Returns a row format from a table by position.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <returns>The row format model.</returns>
    DocxTableRowFormat GetTableRowFormat(DocxTable table, int rowIndex);

    /// <summary>
    /// Applies a row format and marks its containing table as changed.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="format">The replacement format.</param>
    void SetTableRowFormat(DocxTable table, int rowIndex, DocxTableRowFormat format);

    /// <summary>
    /// Returns a cell format from a table by position.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The cell format model.</returns>
    DocxTableCellFormat GetTableCellFormat(DocxTable table, int rowIndex, int cellIndex);

    /// <summary>
    /// Applies a cell format and marks its containing row and table as changed.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <param name="format">The replacement format.</param>
    void SetTableCellFormat(DocxTable table, int rowIndex, int cellIndex, DocxTableCellFormat format);
}
