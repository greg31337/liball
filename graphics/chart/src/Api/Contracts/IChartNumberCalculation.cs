namespace chart;

/// <summary>Defines a row calculation that produces numeric column values without reflection.</summary>
public interface IChartNumberCalculation
{
    /// <summary>Calculates a numeric value for one table row.</summary>
    /// <param name="table">The table containing the source row.</param>
    /// <param name="row">The zero-based source row.</param>
    /// <returns>The calculated numeric value.</returns>
    double Calculate(ChartDataTable table, int row);
}
