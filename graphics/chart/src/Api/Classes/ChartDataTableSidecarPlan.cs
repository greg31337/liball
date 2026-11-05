namespace chart;
/// <summary>
/// Represents a data table sidecar plan used by the chart model or rendering pipeline.
/// </summary>
public class ChartDataTableSidecarPlan : IChartDataTableSidecarPlan
{
    internal string TableId;
    internal string Caption;
    internal int RowCount;
    internal int ColumnCount;
    internal string[] ColumnNames;
    internal ChartDataKind[] ColumnKinds;
    /// <summary>
    /// Initializes a data table sidecar plan from the supplied table id, caption, row count, column count, column names, and column kinds.
    /// </summary>
    /// <param name = "tableId">The table id.</param>
    /// <param name = "caption">The caption.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "columnCount">The number of logical columns represented by the input.</param>
    /// <param name = "columnNames">The column names.</param>
    /// <param name = "columnKinds">The column kinds.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartDataTableSidecarPlan(string tableId, string caption, int rowCount, int columnCount, string[] columnNames, ChartDataKind[] columnKinds)
    {
        ChartPlot.ValidateId(tableId);
        ChartTextBlock.ValidateText(caption, "caption");
        if (rowCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        ValidateSidecarColumns(columnNames, columnKinds, columnCount);
        TableId = tableId;
        Caption = caption;
        RowCount = rowCount;
        ColumnCount = columnCount;
        ColumnNames = ChartDataColumn.CopyStringArray(columnNames);
        ColumnKinds = CopyKinds(columnKinds);
    }

    internal static ChartDataTableSidecarPlan FromTable(string tableId, string caption, ChartDataTable table)
    {
        ChartPlot.ValidateId(tableId);
        ChartTextBlock.ValidateText(caption, "caption");
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        string[] names = new string[table.ColumnCount];
        ChartDataKind[] kinds = new ChartDataKind[table.ColumnCount];
        int index = 0;
        while (index < table.ColumnCount)
        {
            ChartDataColumn column = table.GetColumnAt(index);
            names[index] = column.Name;
            kinds[index] = column.Kind;
            index = index + 1;
        }

        return new ChartDataTableSidecarPlan(tableId, caption, table.RowCount, table.ColumnCount, names, kinds);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDataTableSidecarPlan Copy()
    {
        return new ChartDataTableSidecarPlan(TableId, Caption, RowCount, ColumnCount, ColumnNames, ColumnKinds);
    }

    /// <summary>
    /// Applies the summary text operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting string.</returns>
    public string SummaryText()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("data-table ");
        builder.Append(TableId);
        builder.Append(": ");
        builder.Append(RowCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(" rows, ");
        builder.Append(ColumnCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(" columns");
        int index = 0;
        while (index < ColumnCount)
        {
            builder.Append("; ");
            builder.Append(ColumnNames[index]);
            builder.Append("=");
            builder.Append(ColumnKinds[index].ToString());
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void ValidateSidecarColumns(string[] columnNames, ChartDataKind[] columnKinds, int columnCount)
    {
        if (columnNames == null)
        {
            throw new System.ArgumentNullException("columnNames");
        }

        if (columnKinds == null)
        {
            throw new System.ArgumentNullException("columnKinds");
        }

        if (columnNames.Length != columnCount || columnKinds.Length != columnCount)
        {
            throw new System.ArgumentException("Sidecar column metadata must match column count.");
        }

        int index = 0;
        while (index < columnCount)
        {
            ChartDataColumn.ValidateName(columnNames[index]);
            ValidateDataKind(columnKinds[index]);
            index = index + 1;
        }
    }

    internal static void ValidateDataKind(ChartDataKind kind)
    {
        int value = (int)kind;
        if (value < 0 || value > 4)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    internal static ChartDataKind[] CopyKinds(ChartDataKind[] kinds)
    {
        if (kinds == null)
        {
            throw new System.ArgumentNullException("kinds");
        }

        ChartDataKind[] copy = new ChartDataKind[kinds.Length];
        System.Array.Copy(kinds, copy, kinds.Length);
        return copy;
    }

    /// <summary>Returns the current table id.</summary>
    /// <returns>The current field value.</returns>
    public string GetTableId()
    {
        return TableId;
    }

    /// <summary>Replaces the current table id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTableId(string value)
    {
        TableId = value;
    }

    /// <summary>Returns the current caption.</summary>
    /// <returns>The current field value.</returns>
    public string GetCaption()
    {
        return Caption;
    }

    /// <summary>Replaces the current caption.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCaption(string value)
    {
        Caption = value;
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

    /// <summary>Returns the current column names.</summary>
    /// <returns>The current field value.</returns>
    public string[] GetColumnNames()
    {
        return ColumnNames;
    }

    /// <summary>Replaces the current column names.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnNames(string[] value)
    {
        ColumnNames = value;
    }

    /// <summary>Returns the current column kinds.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataKind[] GetColumnKinds()
    {
        return ColumnKinds;
    }

    /// <summary>Replaces the current column kinds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnKinds(ChartDataKind[] value)
    {
        ColumnKinds = value;
    }
}
