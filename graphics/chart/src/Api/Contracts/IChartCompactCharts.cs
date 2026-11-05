namespace chart;
/// <summary>Defines substitutable chart compact charts operations.</summary>
public interface IChartCompactCharts
{
    /// <summary>
    /// Creates the requested slope series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "startValues">The start values in data order.</param>
    /// <param name = "endValues">The end values in data order.</param>
    /// <param name = "startX">The start x.</param>
    /// <param name = "endX">The end x.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartCartesianSeries CreateSlopeSeries(string id, string[] labels, double[] startValues, double[] endValues, double startX, double endX, ChartPalette palette, double strokeWidth);
    /// <summary>
    /// Creates the requested dumbbell series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "startValues">The start values in data order.</param>
    /// <param name = "endValues">The end values in data order.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <param name = "startStyle">The rendering style used for start.</param>
    /// <param name = "endStyle">The rendering style used for end.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateDumbbellSeries(string id, double[] yValues, double[] startValues, double[] endValues, double markerSize, ChartRenderStyle lineStyle, ChartRenderStyle startStyle, ChartRenderStyle endStyle);
    /// <summary>
    /// Creates the requested bump series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "ranks">The ranks.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series array.</returns>
    ChartCartesianSeries[] CreateBumpSeries(string idPrefix, string[] labels, double[] xValues, ChartDataMatrix ranks, ChartPalette palette, double strokeWidth);
    /// <summary>
    /// Computes the layout for table within the supplied bounds.
    /// </summary>
    /// <param name = "headers">The headers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "columnCount">The number of logical columns represented by the input.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "headerHeight">The header height.</param>
    /// <returns>The resulting table layout.</returns>
    ChartTableLayout LayoutTable(string[] headers, string[] values, int rowCount, int columnCount, ChartRect bounds, double headerHeight);
    /// <summary>
    /// Adds the supplied table to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "cellStyle">The rendering style used for cell.</param>
    /// <param name = "textStyle">The rendering style used for generated text.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void AddTableToScene(IChartRenderTarget scene, string id, ChartTableLayout layout, ChartRenderStyle cellStyle, ChartRenderStyle textStyle);
    /// <summary>Creates a table scene directly from tabular values.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "headers">The column headers.</param>
    /// <param name = "values">The cell values in row-major order.</param>
    /// <param name = "rowCount">The row count.</param>
    /// <param name = "columnCount">The column count.</param>
    /// <param name = "bounds">The table bounds.</param>
    /// <param name = "headerHeight">The header height.</param>
    /// <param name = "cellStyle">The cell style.</param>
    /// <param name = "textStyle">The text style.</param>
    /// <returns>The newly created render scene.</returns>
    ChartRenderScene CreateTableScene(string id, int width, int height, string[] headers, string[] values, int rowCount, int columnCount, ChartRect bounds, double headerHeight, ChartRenderStyle cellStyle, ChartRenderStyle textStyle);
    /// <summary>
    /// Computes the layout for sparkline within the supplied bounds.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting sparkline layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartSparklineLayout LayoutSparkline(double[] values, ChartRect bounds);
    /// <summary>
    /// Adds the supplied sparkline to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void AddSparklineToScene(IChartRenderTarget scene, string id, ChartSparklineLayout layout, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested sparkline scene from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render scene.</returns>
    ChartRenderScene CreateSparklineScene(string id, int width, int height, double[] values, ChartRenderStyle style);
}
