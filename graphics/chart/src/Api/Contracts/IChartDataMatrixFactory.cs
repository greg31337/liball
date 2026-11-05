namespace chart;
/// <summary>Defines substitutable chart data matrix factory operations.</summary>
public interface IChartDataMatrixFactory
{
    /// <summary>
    /// Creates a data matrix from the supplied rows, columns, and values.
    /// </summary>
    /// <param name = "rows">The rows.</param>
    /// <param name = "columns">The columns.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>A data matrix containing an independent copy of the supplied values.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataMatrix FromValues(int rows, int columns, double[] values);
}
