namespace chart;
/// <summary>Provides the public chart declarative charts operations.</summary>
public class ChartDeclarativeCharts : IChartDeclarativeCharts
{
    /// <summary>Initializes a stateless chart declarative charts facade.</summary>
    public ChartDeclarativeCharts()
    {
    }

    /// <summary>Constructs layered, grouped, ordered, stacked, and faceted series with one shared categorical guide.</summary>
    /// <param name = "layers">The declarative layers in back-to-front order.</param>
    /// <returns>The flattened declarative view.</returns>
    public ChartDeclarativeView CreateView(ChartDeclarativeLayer[] layers)
    {
        return ChartDeclarativeViewOperations.CreateView(layers);
    }

    /// <summary>
    /// Creates the requested series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "mark">The mark.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateSeries(string id, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateSeries(id, mark, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateLineSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateLineSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateAreaSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateAreaSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateScatterSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateScatterSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested bubble series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "minimumSize">The minimum size.</param>
    /// <param name = "maximumSize">The maximum size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateBubbleSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, double minimumSize, double maximumSize, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateBubbleSeries(id, table, encoding, minimumSize, maximumSize, style);
    }

    /// <summary>
    /// Creates the requested vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateVerticalBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateVerticalBarSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested horizontal bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateHorizontalBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateHorizontalBarSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested range bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateRangeBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateRangeBarSeries(id, table, encoding, style);
    }

    /// <summary>Creates one series per stable value of the wrap-facet channel.</summary>
    /// <param name = "idPrefix">The stable series identifier prefix.</param>
    /// <param name = "mark">The declarative mark kind.</param>
    /// <param name = "table">The source data table.</param>
    /// <param name = "encoding">The visual encoding containing a wrap field.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The wrapped facet series in first-observed order.</returns>
    public ChartCartesianSeries[] CreateWrapFacetSeries(string idPrefix, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        return ChartDeclarativeChartsOperations.CreateWrapFacetSeries(idPrefix, mark, table, encoding, style);
    }
}
