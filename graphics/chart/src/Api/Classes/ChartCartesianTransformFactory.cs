namespace chart;
/// <summary>Creates and converts chart cartesian transform values.</summary>
public class ChartCartesianTransformFactory : IChartCartesianTransformFactory
{
    /// <summary>Initializes a stateless chart cartesian transform factory facade.</summary>
    public ChartCartesianTransformFactory()
    {
    }

    /// <summary>
    /// Creates the requested a value from axes from the supplied data and configuration.
    /// </summary>
    /// <param name = "xAxis">The x axis.</param>
    /// <param name = "yAxis">The y axis.</param>
    /// <returns>The resulting cartesian transform.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartCartesianTransform FromAxes(ChartAxis xAxis, ChartAxis yAxis)
    {
        return ChartCartesianTransform.FromAxes(xAxis, yAxis);
    }
}
