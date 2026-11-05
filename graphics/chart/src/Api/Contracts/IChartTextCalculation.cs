namespace chart;

/// <summary>Defines a row calculation that produces text column values without reflection.</summary>
public interface IChartTextCalculation
{
    /// <summary>Calculates a text value for one table row.</summary>
    /// <param name="table">The table containing the source row.</param>
    /// <param name="row">The zero-based source row.</param>
    /// <returns>The calculated text value, or <c>null</c> when the value is missing.</returns>
    string? Calculate(ChartDataTable table, int row);
}
