namespace chart;
/// <summary>Provides the public chart line area charts operations.</summary>
public class ChartLineAreaCharts : IChartLineAreaCharts
{
    /// <summary>Initializes a stateless chart line area charts facade.</summary>
    public ChartLineAreaCharts()
    {
    }

    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateLineSeries(string id, double[] xValues, double[] yValues, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateLineSeries(id, xValues, yValues, style);
    }

    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "interpolation">The rule used to connect values between adjacent data points.</param>
    /// <param name = "splineSamplesPerSegment">The number of straight segments used to approximate each spline segment.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateInterpolatedLineSeries(string id, double[] xValues, double[] yValues, ChartLineInterpolation interpolation, int splineSamplesPerSegment, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateLineSeries(id, xValues, yValues, interpolation, splineSamplesPerSegment, style);
    }

    /// <summary>
    /// Creates the requested multi line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateMultiLineSeries(string idPrefix, double[] xValues, ChartDataMatrix yValues, ChartPalette palette, double strokeWidth)
    {
        return ChartLineAreaChartsOperations.CreateMultiLineSeries(idPrefix, xValues, yValues, palette, strokeWidth);
    }

    /// <summary>
    /// Creates the requested multi line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <param name = "interpolation">The rule used to connect values between adjacent data points.</param>
    /// <param name = "splineSamplesPerSegment">The number of straight segments used to approximate each spline segment.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateInterpolatedMultiLineSeries(string idPrefix, double[] xValues, ChartDataMatrix yValues, ChartPalette palette, double strokeWidth, ChartLineInterpolation interpolation, int splineSamplesPerSegment)
    {
        return ChartLineAreaChartsOperations.CreateMultiLineSeries(idPrefix, xValues, yValues, palette, strokeWidth, interpolation, splineSamplesPerSegment);
    }

    /// <summary>
    /// Creates the requested step line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "stepInterpolation">The before, after, or middle rule used to place each step transition.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartCartesianSeries CreateStepLineSeries(string id, double[] xValues, double[] yValues, ChartLineInterpolation stepInterpolation, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateStepLineSeries(id, xValues, yValues, stepInterpolation, style);
    }

    /// <summary>
    /// Creates the requested spline line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "samplesPerSegment">The samples per segment.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateSplineLineSeries(string id, double[] xValues, double[] yValues, int samplesPerSegment, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateSplineLineSeries(id, xValues, yValues, samplesPerSegment, style);
    }

    /// <summary>Creates a path whose straight segments sample the requested line interpolation.</summary>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "interpolation">The interpolation used between values.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <returns>The generated path.</returns>
    public ChartPath CreateInterpolatedPath(double[] xValues, double[] yValues, ChartLineInterpolation interpolation, int samplesPerSegment)
    {
        return ChartLineAreaChartsOperations.CreateInterpolatedPath(xValues, yValues, interpolation, samplesPerSegment);
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateAreaSeries(string id, double[] xValues, double[] yValues, double baseline, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateAreaSeries(id, xValues, yValues, baseline, style);
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "upperValues">The upper values in data order.</param>
    /// <param name = "lowerValues">The lower values in data order.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateRangeAreaSeries(string id, double[] xValues, double[] upperValues, double[] lowerValues, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateAreaSeries(id, xValues, upperValues, lowerValues, style);
    }

    /// <summary>Creates an interpolated area series between values and a constant baseline.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "baseline">The finite area baseline.</param>
    /// <param name = "interpolation">The interpolation used for both area boundaries.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <param name = "style">The area render style.</param>
    /// <returns>The generated area series.</returns>
    public ChartCartesianSeries CreateInterpolatedAreaSeries(string id, double[] xValues, double[] yValues, double baseline, ChartLineInterpolation interpolation, int samplesPerSegment, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateInterpolatedAreaSeries(id, xValues, yValues, baseline, interpolation, samplesPerSegment, style);
    }

    /// <summary>Creates an interpolated area series between upper and lower value arrays.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "upperValues">The upper boundary values.</param>
    /// <param name = "lowerValues">The lower boundary values.</param>
    /// <param name = "interpolation">The interpolation used for both area boundaries.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <param name = "style">The area render style.</param>
    /// <returns>The generated area series.</returns>
    public ChartCartesianSeries CreateInterpolatedRangeAreaSeries(string id, double[] xValues, double[] upperValues, double[] lowerValues, ChartLineInterpolation interpolation, int samplesPerSegment, ChartRenderStyle style)
    {
        return ChartLineAreaChartsOperations.CreateInterpolatedRangeAreaSeries(id, xValues, upperValues, lowerValues, interpolation, samplesPerSegment, style);
    }

    /// <summary>
    /// Creates the requested stacked area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "mode">The mode.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateStackedAreaSeries(string idPrefix, double[] xValues, ChartDataMatrix values, ChartStackMode mode, ChartPalette palette)
    {
        return ChartLineAreaChartsOperations.CreateStackedAreaSeries(idPrefix, xValues, values, mode, palette);
    }

    /// <summary>
    /// Creates the requested streamgraph series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateStreamgraphSeries(string idPrefix, double[] xValues, ChartDataMatrix values, ChartPalette palette)
    {
        return ChartLineAreaChartsOperations.CreateStreamgraphSeries(idPrefix, xValues, values, palette);
    }
}
