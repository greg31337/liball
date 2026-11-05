namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDataTableSidecarPlan"/>.</summary>
public interface IChartDataTableSidecarPlan
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDataTableSidecarPlan Copy();
    /// <summary>
    /// Applies the summary text operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting string.</returns>
    string SummaryText();
    /// <summary>Returns the current table id.</summary>
    /// <returns>The current field value.</returns>
    string GetTableId();
    /// <summary>Replaces the current table id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTableId(string value);
    /// <summary>Returns the current caption.</summary>
    /// <returns>The current field value.</returns>
    string GetCaption();
    /// <summary>Replaces the current caption.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCaption(string value);
    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    int GetRowCount();
    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowCount(int value);
    /// <summary>Returns the current column count.</summary>
    /// <returns>The current field value.</returns>
    int GetColumnCount();
    /// <summary>Replaces the current column count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnCount(int value);
    /// <summary>Returns the current column names.</summary>
    /// <returns>The current field value.</returns>
    string[] GetColumnNames();
    /// <summary>Replaces the current column names.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnNames(string[] value);
    /// <summary>Returns the current column kinds.</summary>
    /// <returns>The current field value.</returns>
    ChartDataKind[] GetColumnKinds();
    /// <summary>Replaces the current column kinds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnKinds(ChartDataKind[] value);
}
