namespace chart;
/// <summary>Creates and converts chart matrix values.</summary>
public class ChartMatrixFactory : IChartMatrixFactory
{
    /// <summary>Initializes a stateless chart matrix factory facade.</summary>
    public ChartMatrixFactory()
    {
    }

    /// <summary>
    /// Applies the identity operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting matrix.</returns>
    public ChartMatrix Identity()
    {
        return ChartMatrix.Identity();
    }

    /// <summary>
    /// Applies the translation operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting matrix.</returns>
    public ChartMatrix Translation(double x, double y)
    {
        return ChartMatrix.Translation(x, y);
    }

    /// <summary>
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting matrix.</returns>
    public ChartMatrix Scale(double x, double y)
    {
        return ChartMatrix.Scale(x, y);
    }

    /// <summary>
    /// Applies the rotation operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "radians">The radians.</param>
    /// <returns>The resulting matrix.</returns>
    public ChartMatrix Rotation(double radians)
    {
        return ChartMatrix.Rotation(radians);
    }
}
