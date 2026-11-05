namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDataTable"/>.</summary>
public interface IChartDataTable
{
    /// <summary>
    /// Adds the supplied number column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data column.</returns>
    ChartDataColumn AddNumberColumn(string name, double[] values);
    /// <summary>
    /// Adds the supplied text column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The resulting data column.</returns>
    ChartDataColumn AddTextColumn(string name, string? [] values);
    /// <summary>
    /// Adds the supplied date time column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data column.</returns>
    ChartDataColumn AddDateTimeColumn(string name, System.DateTime[] values);
    /// <summary>
    /// Adds the supplied category column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The resulting data column.</returns>
    ChartDataColumn AddCategoryColumn(string name, string? [] values);
    /// <summary>
    /// Adds the supplied matrix column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The resulting data column.</returns>
    ChartDataColumn AddMatrixColumn(string name, ChartDataMatrix matrix);
    /// <summary>
    /// Adds the supplied column to this object.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn AddColumn(ChartDataColumn column);
    /// <summary>
    /// Determines whether this value contains column.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsColumn(string name);
    /// <summary>
    /// Returns the zero-based column index.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting int.</returns>
    int ColumnIndex(string name);
    /// <summary>
    /// Finds column using the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    ChartDataColumn? FindColumn(string name);
    /// <summary>
    /// Returns the requested column.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartDataColumn GetColumn(string name);
    /// <summary>
    /// Returns the requested column at.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartDataColumn GetColumnAt(int index);
    /// <summary>
    /// Returns the requested number.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The resulting double.</returns>
    double GetNumber(string name, int row);
    /// <summary>
    /// Returns the requested text.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The text value, or <c>null</c> when the value is missing.</returns>
    string? GetText(string name, int row);
    /// <summary>
    /// Returns the requested date time.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The resulting date time.</returns>
    System.DateTime GetDateTime(string name, int row);
    /// <summary>
    /// Returns the requested category.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The category value, or <c>null</c> when the value is missing.</returns>
    string? GetCategory(string name, int row);
    /// <summary>
    /// Returns the requested matrix value.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetMatrixValue(string name, int row, int column);
    /// <summary>
    /// Creates an independent copy of the columns.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDataColumn[] CopyColumns();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDataTable Copy();
    /// <summary>Exports column names and values as deterministic RFC-style comma-separated text.</summary>
    /// <returns>The comma-separated table.</returns>
    string ToCsv();
    /// <summary>Exports column names and values as deterministic tab-separated text.</summary>
    /// <returns>The tab-separated table.</returns>
    string ToTsv();
    /// <summary>Exports typed column metadata and row values as deterministic JSON.</summary>
    /// <returns>The JSON table.</returns>
    string ToJsonTable();
    /// <summary>Appends rows from a table with the same non-matrix column schema.</summary>
    /// <param name = "rows">The rows to append.</param>
    void AppendRows(ChartDataTable rows);
    /// <summary>Replaces existing rows from a table with the same non-matrix column schema.</summary>
    /// <param name = "startRow">The first row to replace.</param>
    /// <param name = "rows">The replacement rows.</param>
    void UpdateRows(int startRow, ChartDataTable rows);
    /// <summary>Explicitly marks the table data as changed.</summary>
    void Invalidate();
    /// <summary>Returns the mutation version used by streaming consumers.</summary>
    /// <returns>The current mutation version.</returns>
    int GetVersion();
    /// <summary>Returns the current columns.</summary>
    /// <returns>The current field value.</returns>
    ChartDataColumn[] GetColumns();
    /// <summary>Replaces the current columns.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumns(ChartDataColumn[] value);
    /// <summary>Returns the current column count.</summary>
    /// <returns>The current field value.</returns>
    int GetColumnCount();
    /// <summary>Replaces the current column count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnCount(int value);
    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    int GetRowCount();
    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowCount(int value);
}
