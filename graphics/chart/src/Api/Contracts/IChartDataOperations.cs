namespace chart;
/// <summary>Defines substitutable chart data operations operations.</summary>
public interface IChartDataOperations
{
    /// <summary>
    /// Groups by category using the supplied data and configuration.
    /// </summary>
    /// <param name = "categoryColumn">The category column.</param>
    /// <returns>The resulting data group array.</returns>
    ChartDataGroup[] GroupByCategory(ChartDataColumn categoryColumn);
    /// <summary>
    /// Sorts indices by number using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    int[] SortIndicesByNumber(ChartDataColumn column, bool ascending);
    /// <summary>
    /// Sorts indices by text using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    int[] SortIndicesByText(ChartDataColumn column, bool ascending);
    /// <summary>Sorts text or category rows by an explicit stable value order.</summary>
    /// <param name = "column">The text or category column.</param>
    /// <param name = "customOrder">The values in their requested order.</param>
    /// <returns>The stable source indices with unlisted values placed last.</returns>
    int[] SortIndicesByCustomOrder(ChartDataColumn column, string? [] customOrder);
    /// <summary>
    /// Filters number range using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    int[] FilterNumberRange(ChartDataColumn column, double minimum, double maximum);
    /// <summary>
    /// Filters category in using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "categories">The categories.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    int[] FilterCategoryIn(ChartDataColumn column, string? [] categories);
    /// <summary>
    /// Filters text contains using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    int[] FilterTextContains(ChartDataColumn column, string text);
    /// <summary>
    /// Filters missing categories using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting int array.</returns>
    int[] FilterMissingCategories(ChartDataColumn column);
    /// <summary>
    /// Aggregates number using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "indices">The indices.</param>
    /// <param name = "aggregation">The aggregation.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    double AggregateNumber(ChartDataColumn column, int[] indices, ChartAggregationKind aggregation);
    /// <summary>Returns an R-7 percentile of the selected numeric rows.</summary>
    /// <param name = "column">The numeric column.</param>
    /// <param name = "indices">The selected source indices.</param>
    /// <param name = "percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    double AggregatePercentile(ChartDataColumn column, int[] indices, double percentile);
    /// <summary>Joins two tables by columns containing equal keys.</summary>
    /// <param name = "left">The left table.</param>
    /// <param name = "right">The right table.</param>
    /// <param name = "leftKeyName">The left key column name.</param>
    /// <param name = "rightKeyName">The right key column name.</param>
    /// <param name = "kind">The rows retained by the join.</param>
    /// <returns>A new table containing the joined rows.</returns>
    ChartDataTable JoinTablesByKey(ChartDataTable left, ChartDataTable right, string leftKeyName, string rightKeyName, ChartJoinKind kind);
    /// <summary>Calculates and adds a numeric column to a table.</summary>
    /// <param name = "table">The table to update.</param>
    /// <param name = "name">The new column name.</param>
    /// <param name = "calculation">The row calculation.</param>
    /// <returns>The added numeric column.</returns>
    ChartDataColumn AddCalculatedNumberColumn(ChartDataTable table, string name, IChartNumberCalculation calculation);
    /// <summary>Calculates and adds a text column to a table.</summary>
    /// <param name = "table">The table to update.</param>
    /// <param name = "name">The new column name.</param>
    /// <param name = "calculation">The row calculation.</param>
    /// <returns>The added text column.</returns>
    ChartDataColumn AddCalculatedTextColumn(ChartDataTable table, string name, IChartTextCalculation calculation);
    /// <summary>
    /// Pivots category number using the supplied data and configuration.
    /// </summary>
    /// <param name = "rowCategories">The row categories.</param>
    /// <param name = "columnCategories">The column categories.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "aggregation">The aggregation.</param>
    /// <returns>The resulting pivot table.</returns>
    ChartPivotTable PivotCategoryNumber(ChartDataColumn rowCategories, ChartDataColumn columnCategories, ChartDataColumn values, ChartAggregationKind aggregation);
}
