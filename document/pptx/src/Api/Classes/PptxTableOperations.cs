using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTable values.
/// </summary>
public class PptxTableOperations : IPptxTableOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTableOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int RowCount(PptxTable target)
    {
        return target.RowCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ColumnCount(PptxTable target)
    {
        return target.ColumnCount();
    }

    /// <summary>
    /// Returns the row without changing the table.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table row value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTableRow GetRow(PptxTable target, int index)
    {
        return target.GetRow(index);
    }

    /// <summary>
    /// Returns the cell without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTableCell GetCell(PptxTable target, int rowIndex, int columnIndex)
    {
        return target.GetCell(rowIndex, columnIndex);
    }

    /// <summary>
    /// Returns the text without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxTable target, int rowIndex, int columnIndex)
    {
        return target.GetText(rowIndex, columnIndex);
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxTable target, int rowIndex, int columnIndex, string text)
    {
        target.SetText(rowIndex, columnIndex, text);
    }

    /// <summary>
    /// Returns the column width without changing the table.
    /// </summary>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetColumnWidth(PptxTable target, int columnIndex)
    {
        return target.GetColumnWidth(columnIndex);
    }

    /// <summary>
    /// Updates the column width and the underlying presentation data.
    /// </summary>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetColumnWidth(PptxTable target, int columnIndex, long width)
    {
        target.SetColumnWidth(columnIndex, width);
    }

    /// <summary>
    /// Returns the row height without changing the table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetRowHeight(PptxTable target, int rowIndex)
    {
        return target.GetRowHeight(rowIndex);
    }

    /// <summary>
    /// Updates the row height and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetRowHeight(PptxTable target, int rowIndex, long height)
    {
        target.SetRowHeight(rowIndex, height);
    }

    /// <summary>
    /// Inserts a blank row at the supplied index.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="height">The row height in English Metric Units (EMUs).</param>
    public void InsertRow(PptxTable target, int index, long height)
    {
        target.InsertRow(index, height);
    }

    /// <summary>
    /// Removes a row while preserving at least one table row.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based row index.</param>
    public void RemoveRow(PptxTable target, int index)
    {
        target.RemoveRow(index);
    }

    /// <summary>
    /// Inserts a blank column at the supplied index.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="width">The column width in English Metric Units (EMUs).</param>
    public void InsertColumn(PptxTable target, int index, long width)
    {
        target.InsertColumn(index, width);
    }

    /// <summary>
    /// Removes a column while preserving at least one table column.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="index">The zero-based column index.</param>
    public void RemoveColumn(PptxTable target, int index)
    {
        target.RemoveColumn(index);
    }

    /// <summary>
    /// Combines the supplied presentation content with this table.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MergeCells(PptxTable target, int rowIndex, int columnIndex, int rowCount, int columnCount)
    {
        target.MergeCells(rowIndex, columnIndex, rowCount, columnCount);
    }

    /// <summary>
    /// Splits a merged cell region so each cell can be edited independently.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SplitCell(PptxTable target, int rowIndex, int columnIndex)
    {
        target.SplitCell(rowIndex, columnIndex);
    }

    /// <summary>
    /// Finds the merge origin; returns the type's empty value when no match exists.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTableCell FindMergeOrigin(PptxTable target, int rowIndex, int columnIndex)
    {
        return target.FindMergeOrigin(rowIndex, columnIndex);
    }

    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetData(PptxTable target, List<List<string>> data)
    {
        target.SetData(data);
    }

    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDataAt(PptxTable target, int rowIndex, int columnIndex, List<List<string>> data)
    {
        target.SetDataAt(rowIndex, columnIndex, data);
    }

    /// <summary>
    /// Returns the data without changing the table.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<List<string>> GetData(PptxTable target)
    {
        return target.GetData();
    }

    /// <summary>
    /// Extracts data from the table without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<List<string>> ExtractData(PptxTable target)
    {
        return target.ExtractData();
    }

    /// <summary>
    /// Resizes and populates the table from built-in enumerable interfaces.
    /// </summary>
    /// <param name="target">The table to modify.</param>
    /// <param name="data">The rectangular rows to import.</param>
    public void ImportData(PptxTable target, IEnumerable<IEnumerable<string>> data)
    {
        target.ImportData(data);
    }

    /// <summary>
    /// Exports table rows through built-in read-only collection interfaces.
    /// </summary>
    /// <param name="target">The table to inspect.</param>
    /// <returns>The exported table rows.</returns>
    public IEnumerable<IReadOnlyList<string>> ExportData(PptxTable target)
    {
        return target.ExportData();
    }

    /// <summary>
    /// Returns the style id without changing the table.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetStyleId(PptxTable target)
    {
        return target.GetStyleId();
    }

    /// <summary>
    /// Updates the style id and the underlying presentation data.
    /// </summary>
    /// <param name="styleId">The style id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetStyleId(PptxTable target, string styleId)
    {
        target.SetStyleId(styleId);
    }

    /// <summary>
    /// Returns the style flag without changing the table.
    /// </summary>
    /// <param name="flagName">The flag name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetStyleFlag(PptxTable target, string flagName)
    {
        return target.GetStyleFlag(flagName);
    }

    /// <summary>
    /// Updates the style flag and the underlying presentation data.
    /// </summary>
    /// <param name="flagName">The flag name value to use.</param>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetStyleFlag(PptxTable target, string flagName, bool enabled)
    {
        target.SetStyleFlag(flagName, enabled);
    }

    /// <summary>
    /// Returns the first row without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetFirstRow(PptxTable target)
    {
        return target.GetFirstRow();
    }

    /// <summary>
    /// Updates the first row and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFirstRow(PptxTable target, bool enabled)
    {
        target.SetFirstRow(enabled);
    }

    /// <summary>
    /// Returns the last row without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetLastRow(PptxTable target)
    {
        return target.GetLastRow();
    }

    /// <summary>
    /// Updates the last row and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLastRow(PptxTable target, bool enabled)
    {
        target.SetLastRow(enabled);
    }

    /// <summary>
    /// Returns the first column without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetFirstColumn(PptxTable target)
    {
        return target.GetFirstColumn();
    }

    /// <summary>
    /// Updates the first column and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFirstColumn(PptxTable target, bool enabled)
    {
        target.SetFirstColumn(enabled);
    }

    /// <summary>
    /// Returns the last column without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetLastColumn(PptxTable target)
    {
        return target.GetLastColumn();
    }

    /// <summary>
    /// Updates the last column and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLastColumn(PptxTable target, bool enabled)
    {
        target.SetLastColumn(enabled);
    }

    /// <summary>
    /// Returns the banded rows without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetBandedRows(PptxTable target)
    {
        return target.GetBandedRows();
    }

    /// <summary>
    /// Updates the banded rows and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBandedRows(PptxTable target, bool enabled)
    {
        target.SetBandedRows(enabled);
    }

    /// <summary>
    /// Returns the banded columns without changing the table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetBandedColumns(PptxTable target)
    {
        return target.GetBandedColumns();
    }

    /// <summary>
    /// Updates the banded columns and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBandedColumns(PptxTable target, bool enabled)
    {
        target.SetBandedColumns(enabled);
    }
}
