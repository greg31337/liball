namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartCartesianRenderer"/>.</summary>
public interface IChartCartesianRenderer
{
    /// <summary>
    /// Adds the supplied series to this object.
    /// </summary>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <returns>The resulting cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianSeries AddSeries(ChartCartesianSeries series);
    /// <summary>Adds a line series that refreshes when its series data changes.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "data">The live x and y series data.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The current retained line series.</returns>
    ChartCartesianSeries AddStreamingLineSeries(string id, ChartSeriesData data, ChartRenderStyle style);
    /// <summary>Adds a scatter series that refreshes when its series data changes.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "data">The live x and y series data.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The current retained scatter series.</returns>
    ChartCartesianSeries AddStreamingScatterSeries(string id, ChartSeriesData data, ChartMarkerShape shape, double markerSize, ChartRenderStyle style);
    /// <summary>
    /// Returns the requested series.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting cartesian series.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartCartesianSeries SeriesAt(int index);
    /// <summary>
    /// Renders the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <returns>The resulting render scene.</returns>
    ChartRenderScene Render(int width, int height);
    /// <summary>
    /// Renders to scene using the supplied data and configuration.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void RenderToScene(ChartRenderScene scene);
    /// <summary>Returns the current transform.</summary>
    /// <returns>The current field value.</returns>
    ChartCartesianTransform GetTransform();
    /// <summary>Replaces the current transform.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTransform(ChartCartesianTransform value);
    /// <summary>Returns the current series.</summary>
    /// <returns>The current field value.</returns>
    ChartCartesianSeries[] GetSeries();
    /// <summary>Replaces the current series.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSeries(ChartCartesianSeries[] value);
    /// <summary>Returns the current series count.</summary>
    /// <returns>The current field value.</returns>
    int GetSeriesCount();
    /// <summary>Replaces the current series count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSeriesCount(int value);
}
