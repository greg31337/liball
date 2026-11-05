namespace chart;
/// <summary>
/// Represents a data table used by the chart model or rendering pipeline.
/// </summary>
public class ChartDataTable : IChartDataTable
{
    internal ChartDataColumn[] Columns;
    internal int ColumnCount;
    internal int RowCount;
    internal int Version;
    /// <summary>
    /// Initializes an empty data table with library defaults.
    /// </summary>
    public ChartDataTable()
    {
        Columns = new ChartDataColumn[4];
        ColumnCount = 0;
        RowCount = 0;
        Version = 0;
    }

    /// <summary>
    /// Adds the supplied number column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data column.</returns>
    public ChartDataColumn AddNumberColumn(string name, double[] values)
    {
        return AddColumn(ChartDataColumn.CreateNumber(name, values));
    }

    /// <summary>
    /// Adds the supplied text column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The resulting data column.</returns>
    public ChartDataColumn AddTextColumn(string name, string? [] values)
    {
        return AddColumn(ChartDataColumn.CreateText(name, values));
    }

    /// <summary>
    /// Adds the supplied date time column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data column.</returns>
    public ChartDataColumn AddDateTimeColumn(string name, System.DateTime[] values)
    {
        return AddColumn(ChartDataColumn.CreateDateTime(name, values));
    }

    /// <summary>
    /// Adds the supplied category column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The resulting data column.</returns>
    public ChartDataColumn AddCategoryColumn(string name, string? [] values)
    {
        return AddColumn(ChartDataColumn.CreateCategory(name, values));
    }

    /// <summary>
    /// Adds the supplied matrix column to this object.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The resulting data column.</returns>
    public ChartDataColumn AddMatrixColumn(string name, ChartDataMatrix matrix)
    {
        return AddColumn(ChartDataColumn.CreateMatrix(name, matrix));
    }

    /// <summary>
    /// Adds the supplied column to this object.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn AddColumn(ChartDataColumn column)
    {
        if (column == null)
        {
            throw new System.ArgumentNullException("column");
        }

        if (ContainsColumn(column.Name))
        {
            throw new System.ArgumentException("A column with the same name already exists.");
        }

        if (ColumnCount == 0)
        {
            RowCount = column.Length;
        }
        else if (column.Length != RowCount)
        {
            throw new System.ArgumentException("Column length must match table row count.");
        }

        EnsureCapacity(ColumnCount + 1);
        Columns[ColumnCount] = column.Copy();
        ColumnCount = ColumnCount + 1;
        Invalidate();
        return Columns[ColumnCount - 1];
    }

    /// <summary>
    /// Determines whether this value contains column.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsColumn(string name)
    {
        return ColumnIndex(name) >= 0;
    }

    /// <summary>
    /// Returns the zero-based column index.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting int.</returns>
    public int ColumnIndex(string name)
    {
        ChartDataColumn.ValidateName(name);
        int index = 0;
        while (index < ColumnCount)
        {
            if (Columns[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Finds column using the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The matching value, or <c>null</c> when no match exists.</returns>
    public ChartDataColumn? FindColumn(string name)
    {
        int index = ColumnIndex(name);
        if (index < 0)
        {
            return null;
        }

        return Columns[index];
    }

    /// <summary>
    /// Returns the requested column.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartDataColumn GetColumn(string name)
    {
        ChartDataColumn? column = FindColumn(name);
        if (column == null)
        {
            throw new System.ArgumentException("Column was not found.");
        }

        return column;
    }

    /// <summary>
    /// Returns the requested column at.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting data column.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartDataColumn GetColumnAt(int index)
    {
        if (index < 0 || index >= ColumnCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Columns[index];
    }

    /// <summary>
    /// Returns the requested number.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The resulting double.</returns>
    public double GetNumber(string name, int row)
    {
        return GetColumn(name).GetNumber(row);
    }

    /// <summary>
    /// Returns the requested text.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The text value, or <c>null</c> when the value is missing.</returns>
    public string? GetText(string name, int row)
    {
        return GetColumn(name).GetText(row);
    }

    /// <summary>
    /// Returns the requested date time.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The resulting date time.</returns>
    public System.DateTime GetDateTime(string name, int row)
    {
        return GetColumn(name).GetDateTime(row);
    }

    /// <summary>
    /// Returns the requested category.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <returns>The category value, or <c>null</c> when the value is missing.</returns>
    public string? GetCategory(string name, int row)
    {
        return GetColumn(name).GetCategory(row);
    }

    /// <summary>
    /// Returns the requested matrix value.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetMatrixValue(string name, int row, int column)
    {
        return GetColumn(name).GetMatrixValue(row, column);
    }

    /// <summary>
    /// Creates an independent copy of the columns.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDataColumn[] CopyColumns()
    {
        ChartDataColumn[] copy = new ChartDataColumn[ColumnCount];
        int index = 0;
        while (index < ColumnCount)
        {
            copy[index] = Columns[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDataTable Copy()
    {
        ChartDataTable table = new ChartDataTable();
        int index = 0;
        while (index < ColumnCount)
        {
            table.AddColumn(Columns[index]);
            index = index + 1;
        }

        table.Version = Version;

        return table;
    }

    /// <summary>Exports column names and values as deterministic RFC-style comma-separated text.</summary>
    /// <returns>The comma-separated table.</returns>
    public string ToCsv()
    {
        return ChartAccessibilityOutput.TableCsv(this);
    }

    /// <summary>Exports column names and values as deterministic tab-separated text.</summary>
    /// <returns>The tab-separated table.</returns>
    public string ToTsv()
    {
        return ChartAccessibilityOutput.TableTsv(this);
    }

    /// <summary>Exports typed column metadata and row values as deterministic JSON.</summary>
    /// <returns>The JSON table.</returns>
    public string ToJsonTable()
    {
        return ChartAccessibilityOutput.TableJson(this);
    }

    /// <summary>Appends rows from a table with the same non-matrix column schema.</summary>
    /// <param name = "rows">The rows to append.</param>
    public void AppendRows(ChartDataTable rows)
    {
        ChartStreamingTableOperations.AppendRows(this, rows);
        if (rows.RowCount > 0)
        {
            Invalidate();
        }
    }

    /// <summary>Replaces existing rows from a table with the same non-matrix column schema.</summary>
    /// <param name = "startRow">The first row to replace.</param>
    /// <param name = "rows">The replacement rows.</param>
    public void UpdateRows(int startRow, ChartDataTable rows)
    {
        ChartStreamingTableOperations.UpdateRows(this, startRow, rows);
        if (rows.RowCount > 0)
        {
            Invalidate();
        }
    }

    /// <summary>Explicitly marks the table data as changed.</summary>
    public void Invalidate()
    {
        if (Version == int.MaxValue)
        {
            Version = 1;
        }
        else
        {
            Version = Version + 1;
        }
    }

    /// <summary>Returns the mutation version used by streaming consumers.</summary>
    /// <returns>The current mutation version.</returns>
    public int GetVersion()
    {
        return Version;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Columns.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Columns.Length, capacity);
        ChartDataColumn[] columns = new ChartDataColumn[newCapacity];
        System.Array.Copy(Columns, columns, ColumnCount);
        Columns = columns;
    }

    /// <summary>Returns the current columns.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataColumn[] GetColumns()
    {
        return Columns;
    }

    /// <summary>Replaces the current columns.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumns(ChartDataColumn[] value)
    {
        Columns = value;
    }

    /// <summary>Returns the current column count.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumnCount()
    {
        return ColumnCount;
    }

    /// <summary>Replaces the current column count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnCount(int value)
    {
        ColumnCount = value;
    }

    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowCount()
    {
        return RowCount;
    }

    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowCount(int value)
    {
        RowCount = value;
    }
}
