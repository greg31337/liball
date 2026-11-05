namespace chart;
/// <summary>
/// Represents a pivot table used by the chart model or rendering pipeline.
/// </summary>
public class ChartPivotTable : IChartPivotTable
{
    internal string? [] RowKeys;
    internal string? [] ColumnKeys;
    internal ChartDataMatrix Values;
    /// <summary>
    /// Initializes a pivot table from the supplied row keys, column keys, and values.
    /// </summary>
    /// <param name = "rowKeys">The row keys.</param>
    /// <param name = "columnKeys">The column keys.</param>
    /// <param name = "values">The input values to process.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPivotTable(string? [] rowKeys, string? [] columnKeys, ChartDataMatrix values)
    {
        if (rowKeys == null)
        {
            throw new System.ArgumentNullException("rowKeys");
        }

        if (columnKeys == null)
        {
            throw new System.ArgumentNullException("columnKeys");
        }

        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (rowKeys.Length != values.Rows || columnKeys.Length != values.Columns)
        {
            throw new System.ArgumentException("Pivot keys must match matrix dimensions.");
        }

        RowKeys = ChartDataColumn.CopyNullableStringArray(rowKeys);
        ColumnKeys = ChartDataColumn.CopyNullableStringArray(columnKeys);
        Values = values.Copy();
    }

    /// <summary>
    /// Returns the requested value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetValue(int row, int column)
    {
        return Values.GetValue(row, column);
    }

    /// <summary>
    /// Creates an independent copy of the row keys.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string? [] CopyRowKeys()
    {
        return ChartDataColumn.CopyNullableStringArray(RowKeys);
    }

    /// <summary>
    /// Creates an independent copy of the column keys.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string? [] CopyColumnKeys()
    {
        return ChartDataColumn.CopyNullableStringArray(ColumnKeys);
    }

    /// <summary>Returns the current row keys.</summary>
    /// <returns>The current field value.</returns>
    public string? [] GetRowKeys()
    {
        return RowKeys;
    }

    /// <summary>Replaces the current row keys.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowKeys(string? [] value)
    {
        RowKeys = value;
    }

    /// <summary>Returns the current column keys.</summary>
    /// <returns>The current field value.</returns>
    public string? [] GetColumnKeys()
    {
        return ColumnKeys;
    }

    /// <summary>Replaces the current column keys.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnKeys(string? [] value)
    {
        ColumnKeys = value;
    }

    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataMatrix GetValues()
    {
        return Values;
    }

    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValues(ChartDataMatrix value)
    {
        Values = value;
    }
}
