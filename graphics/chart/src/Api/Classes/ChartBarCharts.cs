namespace chart;
/// <summary>Provides the public chart bar charts operations.</summary>
public class ChartBarCharts : IChartBarCharts
{
    /// <summary>Initializes a stateless chart bar charts facade.</summary>
    public ChartBarCharts()
    {
    }

    /// <summary>
    /// Creates the requested vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateVerticalBarSeries(string id, double[] xValues, double[] yValues, double barWidth, double baseline, ChartRenderStyle style)
    {
        return ChartBarChartsOperations.CreateVerticalBarSeries(id, xValues, yValues, barWidth, baseline, style);
    }

    /// <summary>
    /// Creates the requested horizontal bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "barHeight">The bar height.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateHorizontalBarSeries(string id, double[] yValues, double[] xValues, double barHeight, double baseline, ChartRenderStyle style)
    {
        return ChartBarChartsOperations.CreateHorizontalBarSeries(id, yValues, xValues, barHeight, baseline, style);
    }

    /// <summary>
    /// Creates the requested grouped vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "groupWidth">The group width.</param>
    /// <param name = "innerGap">The inner gap.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateGroupedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double groupWidth, double innerGap, double baseline, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateGroupedVerticalBarSeries(idPrefix, xValues, values, groupWidth, innerGap, baseline, palette);
    }

    /// <summary>Creates grouped horizontal bars with stable per-series colors.</summary>
    /// <param name = "idPrefix">The stable series identifier prefix.</param><param name = "yValues">The vertical group centers.</param><param name = "values">The row-by-series bar values.</param><param name = "groupHeight">The total height occupied by each group.</param><param name = "innerGap">The gap between bars in a group.</param><param name = "baseline">The horizontal baseline.</param><param name = "palette">The stable series palette.</param>
    /// <returns>The grouped horizontal series.</returns>
    public ChartCartesianSeries[] CreateGroupedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double groupHeight, double innerGap, double baseline, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateGroupedHorizontalBarSeries(idPrefix, yValues, values, groupHeight, innerGap, baseline, palette);
    }

    /// <summary>
    /// Creates the requested stacked vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "mode">The mode.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateStackedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double barWidth, ChartStackMode mode, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateStackedVerticalBarSeries(idPrefix, xValues, values, barWidth, mode, palette);
    }

    /// <summary>Creates horizontal bars using the requested stacking mode.</summary>
    /// <param name = "idPrefix">The stable series identifier prefix.</param><param name = "yValues">The vertical bar centers.</param><param name = "values">The row-by-series values.</param><param name = "barHeight">The bar height.</param><param name = "mode">The stack mode.</param><param name = "palette">The stable series palette.</param>
    /// <returns>The stacked horizontal series.</returns>
    public ChartCartesianSeries[] CreateStackedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double barHeight, ChartStackMode mode, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateStackedHorizontalBarSeries(idPrefix, yValues, values, barHeight, mode, palette);
    }

    /// <summary>
    /// Creates the requested percent stacked vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreatePercentStackedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double barWidth, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreatePercentStackedVerticalBarSeries(idPrefix, xValues, values, barWidth, palette);
    }

    /// <summary>Creates percent-stacked horizontal bars.</summary>
    /// <param name = "idPrefix">The stable series identifier prefix.</param><param name = "yValues">The vertical bar centers.</param><param name = "values">The row-by-series values.</param><param name = "barHeight">The bar height.</param><param name = "palette">The stable series palette.</param>
    /// <returns>The percent-stacked horizontal series.</returns>
    public ChartCartesianSeries[] CreatePercentStackedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double barHeight, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreatePercentStackedHorizontalBarSeries(idPrefix, yValues, values, barHeight, palette);
    }

    /// <summary>Creates a centered funnel as retained trapezoid stages.</summary>
    /// <param name = "id">The stable funnel identifier.</param><param name = "values">Positive non-increasing stage values.</param><param name = "centerX">The horizontal funnel center.</param><param name = "topY">The first stage top.</param><param name = "stageHeight">The stage height.</param><param name = "stageGap">The stage gap.</param><param name = "maximumWidth">The maximum stage width.</param><param name = "palette">The stable stage palette.</param>
    /// <returns>The funnel series.</returns>
    public ChartCartesianSeries CreateFunnelSeries(string id, double[] values, double centerX, double topY, double stageHeight, double stageGap, double maximumWidth, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateFunnelSeries(id, values, centerX, topY, stageHeight, stageGap, maximumWidth, palette);
    }

    /// <summary>Creates a renderable scene containing a centered funnel.</summary>
    /// <param name = "id">The stable funnel identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param><param name = "values">Positive non-increasing stage values.</param><param name = "centerX">The horizontal funnel center.</param><param name = "topY">The first stage top.</param><param name = "stageHeight">The stage height.</param><param name = "stageGap">The stage gap.</param><param name = "maximumWidth">The maximum stage width.</param><param name = "palette">The stable stage palette.</param>
    /// <returns>The funnel render scene.</returns>
    public ChartRenderScene CreateFunnelScene(string id, int width, int height, double[] values, double centerX, double topY, double stageHeight, double stageGap, double maximumWidth, ChartPalette palette)
    {
        return ChartBarChartsOperations.CreateFunnelScene(id, width, height, values, centerX, topY, stageHeight, stageGap, maximumWidth, palette);
    }

    /// <summary>
    /// Creates the requested range bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "lowValues">The low values in data order.</param>
    /// <param name = "highValues">The high values in data order.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateRangeBarSeries(string id, double[] xValues, double[] lowValues, double[] highValues, double barWidth, ChartRenderStyle style)
    {
        return ChartBarChartsOperations.CreateRangeBarSeries(id, xValues, lowValues, highValues, barWidth, style);
    }

    /// <summary>
    /// Creates the requested lollipop series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "markerShape">The marker shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <param name = "markerStyle">The rendering style used for marker.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateLollipopSeries(string id, double[] xValues, double[] yValues, double baseline, ChartMarkerShape markerShape, double markerSize, ChartRenderStyle lineStyle, ChartRenderStyle markerStyle)
    {
        return ChartBarChartsOperations.CreateLollipopSeries(id, xValues, yValues, baseline, markerShape, markerSize, lineStyle, markerStyle);
    }

    /// <summary>
    /// Creates the requested waterfall series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "deltaValues">The delta values in data order.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "positiveStyle">The rendering style used for positive.</param>
    /// <param name = "negativeStyle">The rendering style used for negative.</param>
    /// <param name = "connectorStyle">The rendering style used for connector.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateWaterfallSeries(string id, double[] xValues, double[] deltaValues, double barWidth, ChartRenderStyle positiveStyle, ChartRenderStyle negativeStyle, ChartRenderStyle connectorStyle)
    {
        return ChartBarChartsOperations.CreateWaterfallSeries(id, xValues, deltaValues, barWidth, positiveStyle, negativeStyle, connectorStyle);
    }
}
