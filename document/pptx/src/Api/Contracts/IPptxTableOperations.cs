using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTableOperations"/>.
/// </summary>
public interface IPptxTableOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int RowCount(PptxTable target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ColumnCount(PptxTable target);
    /// <summary>
    /// Returns the row without changing the table.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table row value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTableRow GetRow(PptxTable target, int index);
    /// <summary>
    /// Returns the cell without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTableCell GetCell(PptxTable target, int rowIndex, int columnIndex);
    /// <summary>
    /// Returns the text without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetText(PptxTable target, int rowIndex, int columnIndex);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxTable target, int rowIndex, int columnIndex, string text);
    /// <summary>
    /// Returns the column width without changing the table.
    /// </summary>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetColumnWidth(PptxTable target, int columnIndex);
    /// <summary>
    /// Updates the column width and the underlying presentation data.
    /// </summary>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetColumnWidth(PptxTable target, int columnIndex, long width);
    /// <summary>
    /// Returns the row height without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetRowHeight(PptxTable target, int rowIndex);
    /// <summary>
    /// Updates the row height and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetRowHeight(PptxTable target, int rowIndex, long height);
    /// <summary>
    /// Inserts a blank row at the supplied index.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="height">The row height in English Metric Units (EMUs).</param>
    void InsertRow(PptxTable target, int index, long height);
    /// <summary>
    /// Removes a row while preserving at least one table row.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based row index.</param>
    void RemoveRow(PptxTable target, int index);
    /// <summary>
    /// Inserts a blank column at the supplied index.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="width">The column width in English Metric Units (EMUs).</param>
    void InsertColumn(PptxTable target, int index, long width);
    /// <summary>
    /// Removes a column while preserving at least one table column.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based column index.</param>
    void RemoveColumn(PptxTable target, int index);
    /// <summary>
    /// Combines the supplied presentation content with this table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="target">The data value to operate on.</param>
    void MergeCells(PptxTable target, int rowIndex, int columnIndex, int rowCount, int columnCount);
    /// <summary>
    /// Splits a merged cell region so each cell can be edited independently.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="target">The data value to operate on.</param>
    void SplitCell(PptxTable target, int rowIndex, int columnIndex);
    /// <summary>
    /// Finds the merge origin; returns the type's empty value when no match exists.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTableCell FindMergeOrigin(PptxTable target, int rowIndex, int columnIndex);
    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetData(PptxTable target, List<List<string>> data);
    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDataAt(PptxTable target, int rowIndex, int columnIndex, List<List<string>> data);
    /// <summary>
    /// Returns the data without changing the table.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<List<string>> GetData(PptxTable target);
    /// <summary>
    /// Extracts data from the table without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<List<string>> ExtractData(PptxTable target);
    /// <summary>
    /// Resizes and populates the table from built-in enumerable interfaces.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="data">The rectangular rows to import.</param>
    void ImportData(PptxTable target, IEnumerable<IEnumerable<string>> data);
    /// <summary>
    /// Exports table rows through built-in read-only collection interfaces.
    /// </summary>
    /// <param name="target">The table to inspect.</param>
    /// <returns>The exported table rows.</returns>
    IEnumerable<IReadOnlyList<string>> ExportData(PptxTable target);
    /// <summary>
    /// Returns the style id without changing the table.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetStyleId(PptxTable target);
    /// <summary>
    /// Updates the style id and the underlying presentation data.
    /// </summary>
    /// <param name="styleId">The style id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetStyleId(PptxTable target, string styleId);
    /// <summary>
    /// Returns the style flag without changing the table.
    /// </summary>
    /// <param name="flagName">The flag name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetStyleFlag(PptxTable target, string flagName);
    /// <summary>
    /// Updates the style flag and the underlying presentation data.
    /// </summary>
    /// <param name="flagName">The flag name value to use.</param>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetStyleFlag(PptxTable target, string flagName, bool enabled);
    /// <summary>
    /// Returns the first row without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetFirstRow(PptxTable target);
    /// <summary>
    /// Updates the first row and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFirstRow(PptxTable target, bool enabled);
    /// <summary>
    /// Returns the last row without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetLastRow(PptxTable target);
    /// <summary>
    /// Updates the last row and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLastRow(PptxTable target, bool enabled);
    /// <summary>
    /// Returns the first column without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetFirstColumn(PptxTable target);
    /// <summary>
    /// Updates the first column and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFirstColumn(PptxTable target, bool enabled);
    /// <summary>
    /// Returns the last column without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetLastColumn(PptxTable target);
    /// <summary>
    /// Updates the last column and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLastColumn(PptxTable target, bool enabled);
    /// <summary>
    /// Returns the banded rows without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetBandedRows(PptxTable target);
    /// <summary>
    /// Updates the banded rows and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBandedRows(PptxTable target, bool enabled);
    /// <summary>
    /// Returns the banded columns without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetBandedColumns(PptxTable target);
    /// <summary>
    /// Updates the banded columns and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBandedColumns(PptxTable target, bool enabled);
}
