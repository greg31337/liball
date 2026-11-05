namespace odf;

/// <summary>
/// Provides typed lookup and editing for tables in any OpenDocument content kind.
/// </summary>
public interface IOdfTableEditor
{
    /// <summary>Gets the document tables in source order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array containing the document tables.</returns>
    public OdfTable[] GetTables(OdfDocument document);
    /// <summary>Finds an unambiguously named table.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The table name.</param>
    /// <returns>The table, or <c>null</c> when the name is missing or ambiguous.</returns>
    public OdfTable? FindTable(OdfDocument document, string name);
    /// <summary>Gets the row run containing a logical row.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="rowIndex">The zero-based logical row index.</param>
    /// <returns>The containing row run, or <c>null</c>.</returns>
    public OdfRow? GetTableRow(OdfTable table, long rowIndex);
    /// <summary>Gets the column run containing a logical column.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="columnIndex">The zero-based logical column index.</param>
    /// <returns>The containing column run, or <c>null</c>.</returns>
    public OdfColumn? GetTableColumn(OdfTable table, long columnIndex);
    /// <summary>Gets the cell run containing a logical cell.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="rowIndex">The zero-based logical row index.</param>
    /// <param name="columnIndex">The zero-based logical column index.</param>
    /// <returns>The containing cell run, or <c>null</c>.</returns>
    public OdfCell? GetTableCell(
        OdfTable table,
        long rowIndex,
        long columnIndex);
    /// <summary>Inserts logical rows.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The insertion index.</param>
    /// <param name="rowCount">The number of rows to insert.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable InsertTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount);
    /// <summary>Deletes logical rows.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row to delete.</param>
    /// <param name="rowCount">The number of rows to delete.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable DeleteTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount);
    /// <summary>Copies logical rows.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row to copy.</param>
    /// <param name="rowCount">The number of rows to copy.</param>
    /// <param name="destinationRowIndex">The insertion index for the copies.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable CopyTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex);
    /// <summary>Moves logical rows.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row to move.</param>
    /// <param name="rowCount">The number of rows to move.</param>
    /// <param name="destinationRowIndex">The insertion index before removal.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable MoveTableRows(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long rowCount,
        long destinationRowIndex);
    /// <summary>Inserts logical columns.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="columnIndex">The insertion index.</param>
    /// <param name="columnCount">The number of columns to insert.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable InsertTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount);
    /// <summary>Deletes logical columns.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="columnIndex">The first column to delete.</param>
    /// <param name="columnCount">The number of columns to delete.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable DeleteTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount);
    /// <summary>Copies logical columns.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="columnIndex">The first column to copy.</param>
    /// <param name="columnCount">The number of columns to copy.</param>
    /// <param name="destinationColumnIndex">The insertion index for the copies.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable CopyTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex);
    /// <summary>Moves logical columns.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="columnIndex">The first column to move.</param>
    /// <param name="columnCount">The number of columns to move.</param>
    /// <param name="destinationColumnIndex">The insertion index before removal.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable MoveTableColumns(
        OdfDocument document,
        OdfTable table,
        long columnIndex,
        long columnCount,
        long destinationColumnIndex);
    /// <summary>Inserts cells into one logical row.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The logical row index.</param>
    /// <param name="columnIndex">The insertion index.</param>
    /// <param name="cellCount">The number of cells to insert.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable InsertTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount);
    /// <summary>Deletes cells from one logical row.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The logical row index.</param>
    /// <param name="columnIndex">The first cell to delete.</param>
    /// <param name="cellCount">The number of cells to delete.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable DeleteTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount);
    /// <summary>Copies cells within one logical row.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The logical row index.</param>
    /// <param name="columnIndex">The first cell to copy.</param>
    /// <param name="cellCount">The number of cells to copy.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable CopyTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex);
    /// <summary>Moves cells within one logical row.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The logical row index.</param>
    /// <param name="columnIndex">The first cell to move.</param>
    /// <param name="cellCount">The number of cells to move.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable MoveTableCells(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long cellCount,
        long destinationColumnIndex);
    /// <summary>Copies a rectangular cell range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first source row.</param>
    /// <param name="columnIndex">The first source column.</param>
    /// <param name="rowCount">The source row count.</param>
    /// <param name="columnCount">The source column count.</param>
    /// <param name="destinationRowIndex">The first destination row.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable CopyTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex);
    /// <summary>Moves a rectangular cell range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first source row.</param>
    /// <param name="columnIndex">The first source column.</param>
    /// <param name="rowCount">The source row count.</param>
    /// <param name="columnCount">The source column count.</param>
    /// <param name="destinationRowIndex">The first destination row.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable MoveTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex);
    /// <summary>Merges a rectangular cell range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable MergeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount);
    /// <summary>Unmerges the range beginning at a merge origin.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The merge-origin row.</param>
    /// <param name="columnIndex">The merge-origin column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable UnmergeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex);
    /// <summary>Sets a matrix formula over a rectangular range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <param name="formula">The formula lexical value.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable SetTableMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula);
    /// <summary>Sets an array formula over a rectangular range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <param name="formula">The formula lexical value.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable SetTableArrayFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string formula);
    /// <summary>Clears a matrix or array formula at its origin.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The formula-origin row.</param>
    /// <param name="columnIndex">The formula-origin column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable ClearTableMatrixFormula(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex);
    /// <summary>Clears cell contents while retaining formatting.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable ClearTableContents(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount);
    /// <summary>Clears cell formatting while retaining contents.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable ClearTableFormats(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount);
    /// <summary>Transposes a rectangular cell range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first source row.</param>
    /// <param name="columnIndex">The first source column.</param>
    /// <param name="rowCount">The source row count.</param>
    /// <param name="columnCount">The source column count.</param>
    /// <param name="destinationRowIndex">The first destination row.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable TransposeTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long destinationRowIndex,
        long destinationColumnIndex);
    /// <summary>Fills a range with a numeric arithmetic series.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <param name="startValue">The first numeric value.</param>
    /// <param name="stepValue">The increment between values.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable FillTableSeries(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        double startValue,
        double stepValue);
    /// <summary>Repeats a seed range over a destination range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="sourceRowIndex">The first seed row.</param>
    /// <param name="sourceColumnIndex">The first seed column.</param>
    /// <param name="sourceRowCount">The seed row count.</param>
    /// <param name="sourceColumnCount">The seed column count.</param>
    /// <param name="destinationRowIndex">The first destination row.</param>
    /// <param name="destinationColumnIndex">The first destination column.</param>
    /// <param name="destinationRowCount">The destination row count.</param>
    /// <param name="destinationColumnCount">The destination column count.</param>
    /// <returns>The refreshed table.</returns>
    public OdfTable CopyTableSeries(
        OdfDocument document,
        OdfTable table,
        long sourceRowIndex,
        long sourceColumnIndex,
        long sourceRowCount,
        long sourceColumnCount,
        long destinationRowIndex,
        long destinationColumnIndex,
        long destinationRowCount,
        long destinationColumnCount);
    /// <summary>Searches cell values and formulas in a range.</summary>
    /// <param name="table">The table to search.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <param name="searchValue">The value to find.</param>
    /// <param name="matchCase">Whether matching is case-sensitive.</param>
    /// <param name="exactMatch">Whether the whole cell value must match.</param>
    /// <returns>The matches in logical range order.</returns>
    public OdfSearchMatch[] SearchTableRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string searchValue,
        bool matchCase,
        bool exactMatch);
    /// <summary>Replaces matching cell values and formulas in a range.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="rowIndex">The first row.</param>
    /// <param name="columnIndex">The first column.</param>
    /// <param name="rowCount">The row count.</param>
    /// <param name="columnCount">The column count.</param>
    /// <param name="searchValue">The value to find.</param>
    /// <param name="replacementValue">The replacement value.</param>
    /// <param name="matchCase">Whether matching is case-sensitive.</param>
    /// <param name="replaceAll">Whether all matches are replaced.</param>
    /// <returns>The replaced matches in logical range order.</returns>
    public OdfSearchMatch[] ReplaceTableRange(
        OdfDocument document,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        string searchValue,
        string replacementValue,
        bool matchCase,
        bool replaceAll);
}
