namespace chart;
/// <summary>Provides the public chart data operations operations.</summary>
public class ChartDataOperations : IChartDataOperations
{
    /// <summary>Initializes a stateless chart data operations facade.</summary>
    public ChartDataOperations()
    {
    }

    /// <summary>
    /// Groups by category using the supplied data and configuration.
    /// </summary>
    /// <param name = "categoryColumn">The category column.</param>
    /// <returns>The resulting data group array.</returns>
    public ChartDataGroup[] GroupByCategory(ChartDataColumn categoryColumn)
    {
        return ChartDataOperationsOperations.GroupByCategory(categoryColumn);
    }

    /// <summary>
    /// Sorts indices by number using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    public int[] SortIndicesByNumber(ChartDataColumn column, bool ascending)
    {
        return ChartDataOperationsOperations.SortIndicesByNumber(column, ascending);
    }

    /// <summary>
    /// Sorts indices by text using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public int[] SortIndicesByText(ChartDataColumn column, bool ascending)
    {
        return ChartDataOperationsOperations.SortIndicesByText(column, ascending);
    }

    /// <summary>Sorts text or category rows by an explicit stable value order.</summary>
    /// <param name = "column">The text or category column.</param>
    /// <param name = "customOrder">The values in their requested order.</param>
    /// <returns>The stable source indices with unlisted values placed last.</returns>
    public int[] SortIndicesByCustomOrder(ChartDataColumn column, string? [] customOrder)
    {
        return ChartDataOperationsOperations.SortIndicesByCustomOrder(column, customOrder);
    }

    /// <summary>
    /// Filters number range using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public int[] FilterNumberRange(ChartDataColumn column, double minimum, double maximum)
    {
        return ChartDataOperationsOperations.FilterNumberRange(column, minimum, maximum);
    }

    /// <summary>
    /// Filters category in using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "categories">The categories.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public int[] FilterCategoryIn(ChartDataColumn column, string? [] categories)
    {
        return ChartDataOperationsOperations.FilterCategoryIn(column, categories);
    }

    /// <summary>
    /// Filters text contains using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public int[] FilterTextContains(ChartDataColumn column, string text)
    {
        return ChartDataOperationsOperations.FilterTextContains(column, text);
    }

    /// <summary>
    /// Filters missing categories using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting int array.</returns>
    public int[] FilterMissingCategories(ChartDataColumn column)
    {
        return ChartDataOperationsOperations.FilterMissingCategories(column);
    }

    /// <summary>
    /// Aggregates number using the supplied data and configuration.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <param name = "indices">The indices.</param>
    /// <param name = "aggregation">The aggregation.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public double AggregateNumber(ChartDataColumn column, int[] indices, ChartAggregationKind aggregation)
    {
        return ChartDataOperationsOperations.AggregateNumber(column, indices, aggregation);
    }

    /// <summary>Returns an R-7 percentile of the selected numeric rows.</summary>
    /// <param name = "column">The numeric column.</param>
    /// <param name = "indices">The selected source indices.</param>
    /// <param name = "percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    public double AggregatePercentile(ChartDataColumn column, int[] indices, double percentile)
    {
        return ChartDataOperationsOperations.AggregatePercentile(column, indices, percentile);
    }

    /// <summary>Joins two tables by columns containing equal keys.</summary>
    /// <param name = "left">The left table.</param>
    /// <param name = "right">The right table.</param>
    /// <param name = "leftKeyName">The left key column name.</param>
    /// <param name = "rightKeyName">The right key column name.</param>
    /// <param name = "kind">The rows retained by the join.</param>
    /// <returns>A new table containing the joined rows.</returns>
    public ChartDataTable JoinTablesByKey(ChartDataTable left, ChartDataTable right, string leftKeyName, string rightKeyName, ChartJoinKind kind)
    {
        return ChartDataOperationsOperations.JoinTablesByKey(left, right, leftKeyName, rightKeyName, kind);
    }

    /// <summary>Calculates and adds a numeric column to a table.</summary>
    /// <param name = "table">The table to update.</param>
    /// <param name = "name">The new column name.</param>
    /// <param name = "calculation">The row calculation.</param>
    /// <returns>The added numeric column.</returns>
    public ChartDataColumn AddCalculatedNumberColumn(ChartDataTable table, string name, IChartNumberCalculation calculation)
    {
        return ChartDataOperationsOperations.AddCalculatedNumberColumn(table, name, calculation);
    }

    /// <summary>Calculates and adds a text column to a table.</summary>
    /// <param name = "table">The table to update.</param>
    /// <param name = "name">The new column name.</param>
    /// <param name = "calculation">The row calculation.</param>
    /// <returns>The added text column.</returns>
    public ChartDataColumn AddCalculatedTextColumn(ChartDataTable table, string name, IChartTextCalculation calculation)
    {
        return ChartDataOperationsOperations.AddCalculatedTextColumn(table, name, calculation);
    }

    /// <summary>
    /// Pivots category number using the supplied data and configuration.
    /// </summary>
    /// <param name = "rowCategories">The row categories.</param>
    /// <param name = "columnCategories">The column categories.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "aggregation">The aggregation.</param>
    /// <returns>The resulting pivot table.</returns>
    public ChartPivotTable PivotCategoryNumber(ChartDataColumn rowCategories, ChartDataColumn columnCategories, ChartDataColumn values, ChartAggregationKind aggregation)
    {
        return ChartDataOperationsOperations.PivotCategoryNumber(rowCategories, columnCategories, values, aggregation);
    }
}
