namespace chart;
/// <summary>Creates and converts chart data table sidecar plan values.</summary>
public class ChartDataTableSidecarPlanFactory : IChartDataTableSidecarPlanFactory
{
    /// <summary>Initializes a stateless chart data table sidecar plan factory facade.</summary>
    public ChartDataTableSidecarPlanFactory()
    {
    }

    /// <summary>
    /// Creates the requested a value from table from the supplied data and configuration.
    /// </summary>
    /// <param name = "tableId">The table id.</param>
    /// <param name = "caption">The caption.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <returns>The resulting data table sidecar plan.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataTableSidecarPlan FromTable(string tableId, string caption, ChartDataTable table)
    {
        return ChartDataTableSidecarPlan.FromTable(tableId, caption, table);
    }
}
