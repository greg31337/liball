namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTableCellLayout"/>.</summary>
public interface IChartTableCellLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartTableCellLayout Copy();
    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    int GetRowIndex();
    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowIndex(int value);
    /// <summary>Returns the current column index.</summary>
    /// <returns>The current field value.</returns>
    int GetColumnIndex();
    /// <summary>Replaces the current column index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnIndex(int value);
    /// <summary>Returns the current header.</summary>
    /// <returns>The current field value.</returns>
    bool GetHeader();
    /// <summary>Replaces the current header.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeader(bool value);
    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    string GetText();
    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetText(string value);
    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetBounds();
    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBounds(ChartRect value);
}
