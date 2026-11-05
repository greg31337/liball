namespace chart;
/// <summary>
/// Represents a table cell layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartTableCellLayout : IChartTableCellLayout
{
    internal int RowIndex;
    internal int ColumnIndex;
    internal bool Header;
    internal string Text;
    internal ChartRect Bounds;
    /// <summary>
    /// Initializes a table cell layout from the supplied row index, column index, header, text, and bounds.
    /// </summary>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <param name = "columnIndex">The zero-based column index.</param>
    /// <param name = "header">The header.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTableCellLayout(int rowIndex, int columnIndex, bool header, string text, ChartRect bounds)
    {
        if (columnIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("columnIndex");
        }

        ChartTextBlock.ValidateText(text, "text");
        ChartAxis.ValidatePlotArea(bounds);
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Header = header;
        Text = text;
        Bounds = ChartPlot.CopyRect(bounds);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartTableCellLayout Copy()
    {
        return new ChartTableCellLayout(RowIndex, ColumnIndex, Header, Text, Bounds);
    }

    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowIndex()
    {
        return RowIndex;
    }

    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowIndex(int value)
    {
        RowIndex = value;
    }

    /// <summary>Returns the current column index.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumnIndex()
    {
        return ColumnIndex;
    }

    /// <summary>Replaces the current column index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnIndex(int value)
    {
        ColumnIndex = value;
    }

    /// <summary>Returns the current header.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHeader()
    {
        return Header;
    }

    /// <summary>Replaces the current header.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeader(bool value)
    {
        Header = value;
    }

    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetText(string value)
    {
        Text = value;
    }

    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetBounds()
    {
        return Bounds;
    }

    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBounds(ChartRect value)
    {
        Bounds = value;
    }
}
