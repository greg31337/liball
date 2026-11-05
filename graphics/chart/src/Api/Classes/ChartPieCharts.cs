namespace chart;
/// <summary>Provides the public chart pie charts operations.</summary>
public class ChartPieCharts : IChartPieCharts
{
    /// <summary>Initializes a stateless chart pie charts facade.</summary>
    public ChartPieCharts()
    {
    }

    /// <summary>
    /// Computes the layout for pie within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    public ChartPieLayout LayoutPie(string[] labels, double[] values, double centerX, double centerY, double radius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.LayoutPie(labels, values, centerX, centerY, radius, startAngle, order, palette);
    }

    /// <summary>
    /// Computes the layout for donut within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    public ChartPieLayout LayoutDonut(string[] labels, double[] values, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.LayoutDonut(labels, values, centerX, centerY, innerRadius, outerRadius, startAngle, order, palette);
    }

    /// <summary>
    /// Computes the layout for rose within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "maximumRadius">The maximum radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartPieLayout LayoutRose(string[] labels, double[] values, double centerX, double centerY, double innerRadius, double maximumRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.LayoutRose(labels, values, centerX, centerY, innerRadius, maximumRadius, startAngle, order, palette);
    }

    /// <summary>
    /// Creates the requested pie scene from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "layout">The layout.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreatePieScene(string id, int width, int height, ChartPieLayout layout)
    {
        return ChartPieChartsOperations.CreatePieScene(id, width, height, layout);
    }

    /// <summary>Creates a pie chart scene directly from values.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "labels">The slice labels.</param>
    /// <param name = "values">The slice values.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The pie radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The slice order.</param>
    /// <param name = "palette">The slice colors.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreatePieChartScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double radius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.CreatePieChartScene(id, width, height, labels, values, centerX, centerY, radius, startAngle, order, palette);
    }

    /// <summary>Creates a donut chart scene directly from values.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "labels">The slice labels.</param>
    /// <param name = "values">The slice values.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius.</param>
    /// <param name = "outerRadius">The outer radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The slice order.</param>
    /// <param name = "palette">The slice colors.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateDonutScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.CreateDonutScene(id, width, height, labels, values, centerX, centerY, innerRadius, outerRadius, startAngle, order, palette);
    }

    /// <summary>Creates a rose chart scene directly from values.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "labels">The slice labels.</param>
    /// <param name = "values">The slice values.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius.</param>
    /// <param name = "maximumRadius">The maximum radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The slice order.</param>
    /// <param name = "palette">The slice colors.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateRoseScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double innerRadius, double maximumRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return ChartPieChartsOperations.CreateRoseScene(id, width, height, labels, values, centerX, centerY, innerRadius, maximumRadius, startAngle, order, palette);
    }

    /// <summary>
    /// Adds the supplied slices to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddSlices(IChartRenderTarget scene, string id, ChartPieLayout layout)
    {
        ChartPieChartsOperations.AddSlices(scene, id, layout);
    }

    /// <summary>
    /// Adds the supplied labels and leader lines to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "labelRadius">The label radius.</param>
    /// <param name = "labelStyle">The rendering style used for label.</param>
    /// <param name = "leaderStyle">The rendering style used for leader.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddLabelsAndLeaderLines(IChartRenderTarget scene, string id, ChartPieLayout layout, double labelRadius, ChartRenderStyle labelStyle, ChartRenderStyle leaderStyle)
    {
        ChartPieChartsOperations.AddLabelsAndLeaderLines(scene, id, layout, labelRadius, labelStyle, leaderStyle);
    }

    /// <summary>
    /// Creates the requested slice path from the supplied data and configuration.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "slice">The slice.</param>
    /// <returns>The newly created path.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPath CreateSlicePath(double centerX, double centerY, ChartPieSlice slice)
    {
        return ChartPieChartsOperations.CreateSlicePath(centerX, centerY, slice);
    }
}
