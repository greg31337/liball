namespace chart;
/// <summary>Defines substitutable chart grid charts operations.</summary>
public interface IChartGridCharts
{
    /// <summary>
    /// Creates the requested heatmap series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateHeatmapSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, double cellWidth, double cellHeight, ChartVisualMap visualMap);
    /// <summary>
    /// Creates the requested matrix image series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianSeries CreateMatrixImageSeries(string id, ChartDataMatrix values, ChartVisualMap visualMap);
    /// <summary>
    /// Creates the requested calendar heatmap series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "dates">The dates.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateCalendarHeatmapSeries(string id, System.DateTime[] dates, double[] values, double cellWidth, double cellHeight, ChartVisualMap visualMap);
    /// <summary>
    /// Groups values into 2 d using the supplied data and configuration.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "xBinCount">The requested number of x bin entries.</param>
    /// <param name = "yBinCount">The requested number of y bin entries.</param>
    /// <returns>The resulting histogram2 d.</returns>
    ChartHistogram2D Bin2D(double[] xValues, double[] yValues, int xBinCount, int yBinCount);
    /// <summary>
    /// Creates the requested histogram2 d series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "histogram">The histogram.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianSeries CreateHistogram2DSeries(string id, ChartHistogram2D histogram, ChartVisualMap visualMap);
    /// <summary>
    /// Applies the hexbin operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <returns>The resulting hexbin result.</returns>
    ChartHexbinResult Hexbin(double[] xValues, double[] yValues, double radius);
    /// <summary>
    /// Creates the requested hexbin series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "result">The validation result that receives any discovered issues.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianSeries CreateHexbinSeries(string id, ChartHexbinResult result, ChartVisualMap visualMap);
    /// <summary>
    /// Applies the contour segments operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "level">The level.</param>
    /// <returns>The resulting contour segment array.</returns>
    ChartContourSegment[] ContourSegments(double[] xCenters, double[] yCenters, ChartDataMatrix values, double level);
    /// <summary>
    /// Creates the requested contour line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "levels">The levels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateContourLineSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, double[] levels, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested filled contour series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateFilledContourSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, ChartVisualMap visualMap);
}
