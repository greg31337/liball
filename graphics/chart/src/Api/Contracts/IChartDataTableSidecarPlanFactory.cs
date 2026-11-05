namespace chart;
/// <summary>Defines substitutable chart data table sidecar plan factory operations.</summary>
public interface IChartDataTableSidecarPlanFactory
{
    /// <summary>
    /// Creates the requested a value from table from the supplied data and configuration.
    /// </summary>
    /// <param name = "tableId">The table id.</param>
    /// <param name = "caption">The caption.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <returns>The resulting data table sidecar plan.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataTableSidecarPlan FromTable(string tableId, string caption, ChartDataTable table);
}
