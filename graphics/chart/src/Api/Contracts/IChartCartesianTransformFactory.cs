namespace chart;
/// <summary>Defines substitutable chart cartesian transform factory operations.</summary>
public interface IChartCartesianTransformFactory
{
    /// <summary>
    /// Creates the requested a value from axes from the supplied data and configuration.
    /// </summary>
    /// <param name = "xAxis">The x axis.</param>
    /// <param name = "yAxis">The y axis.</param>
    /// <returns>The resulting cartesian transform.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianTransform FromAxes(ChartAxis xAxis, ChartAxis yAxis);
}
