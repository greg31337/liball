namespace chart;
/// <summary>Defines substitutable chart matrix factory operations.</summary>
public interface IChartMatrixFactory
{
    /// <summary>
    /// Applies the identity operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting matrix.</returns>
    ChartMatrix Identity();
    /// <summary>
    /// Applies the translation operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting matrix.</returns>
    ChartMatrix Translation(double x, double y);
    /// <summary>
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting matrix.</returns>
    ChartMatrix Scale(double x, double y);
    /// <summary>
    /// Applies the rotation operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "radians">The radians.</param>
    /// <returns>The resulting matrix.</returns>
    ChartMatrix Rotation(double radians);
}
