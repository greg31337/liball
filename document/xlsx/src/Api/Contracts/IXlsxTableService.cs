using System;
using System.Data;

namespace xlsx;

/// <summary>
/// Defines high-level worksheet table conversion, projection, mapping, and transfer operations.
/// </summary>
public interface IXlsxTableService
{
    /// <summary>
    /// Converts a worksheet range to a table.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the range.</param>
    /// <param name="reference">The A1 range reference to convert.</param>
    /// <param name="tableName">The table name to create.</param>
    /// <param name="firstRowAsHeaders">Whether the first range row contains headers.</param>
    /// <returns>The table operation result.</returns>
    XlsxWorksheetTableResult ConvertRange(XlsxWorksheetPart worksheet, string reference, string tableName, bool firstRowAsHeaders);
    /// <summary>
    /// Projects table cells to typed row arrays.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to project.</param>
    /// <param name="includeHeaderRow">Whether to include the table header row.</param>
    /// <param name="includeTotalsRow">Whether to include the table totals row.</param>
    /// <returns>The typed row projection.</returns>
    XlsxTableRowProjectionResult ProjectRows(XlsxWorksheetPart worksheet, string tableName, bool includeHeaderRow, bool includeTotalsRow);
    /// <summary>
    /// Opens a forward cursor over projected table rows.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to read.</param>
    /// <param name="includeHeaderRow">Whether to include the table header row.</param>
    /// <param name="includeTotalsRow">Whether to include the table totals row.</param>
    /// <returns>The opened row cursor.</returns>
    XlsxTableRowCursor OpenRowCursor(XlsxWorksheetPart worksheet, string tableName, bool includeHeaderRow, bool includeTotalsRow);
    /// <summary>
    /// Advances a table row cursor.
    /// </summary>
    /// <param name="cursor">The cursor to advance.</param>
    /// <returns><c>true</c> when a row was read; otherwise <c>false</c>.</returns>
    bool ReadRow(XlsxTableRowCursor? cursor);
    /// <summary>
    /// Resets a table row cursor to its initial position.
    /// </summary>
    /// <param name="cursor">The cursor to reset.</param>
    void ResetRowCursor(XlsxTableRowCursor? cursor);
    /// <summary>
    /// Sets a typed totals-row function on a table column.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to change.</param>
    /// <param name="columnName">The table column name to change.</param>
    /// <param name="function">The typed total function.</param>
    /// <returns>The table operation result.</returns>
    XlsxWorksheetTableResult SetTotalFunction(XlsxWorksheetPart worksheet, string tableName, string columnName, XlsxTableTotalFunction function);
    /// <summary>
    /// Sets typed XML mapping properties on a table column.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to change.</param>
    /// <param name="columnName">The table column name to change.</param>
    /// <param name="properties">The XML mapping properties.</param>
    /// <returns>The table operation result.</returns>
    XlsxWorksheetTableResult SetXmlColumnMapping(XlsxWorksheetPart worksheet, string tableName, string columnName, XlsxTableXmlColumnProperties? properties);
    /// <summary>
    /// Imports a <see cref="DataTable"/> and converts the imported range to a worksheet table.
    /// </summary>
    /// <param name="worksheet">The destination worksheet.</param>
    /// <param name="topLeftCell">The top-left destination cell.</param>
    /// <param name="dataTable">The data table to import.</param>
    /// <param name="tableName">The worksheet table name to create.</param>
    /// <param name="includeHeaders">Whether to write data-table column names as headers.</param>
    /// <returns>The import result.</returns>
    XlsxDataTableImportResult ImportDataTable(XlsxWorksheetPart worksheet, string topLeftCell, DataTable? dataTable, string tableName, bool includeHeaders);
    /// <summary>
    /// Exports a worksheet table to a built-in <see cref="DataTable"/>.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="tableName">The table name to export.</param>
    /// <returns>The export result.</returns>
    XlsxDataTableExportResult ExportDataTable(XlsxWorksheetPart worksheet, string tableName);
}
