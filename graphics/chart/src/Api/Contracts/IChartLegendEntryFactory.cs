namespace chart;
/// <summary>Defines substitutable chart legend entry factory operations.</summary>
public interface IChartLegendEntryFactory
{
    /// <summary>
    /// Creates the requested symbol from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartLegendEntry CreateSymbol(string id, string label, ChartMarkerShape shape, ChartStyle style);
    /// <summary>
    /// Creates the requested gradient from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "colors">The colors.</param>
    /// <param name = "offsets">The offsets.</param>
    /// <returns>The newly created legend entry.</returns>
    ChartLegendEntry CreateGradient(string id, string label, ChartColor[] colors, double[] offsets);
    /// <summary>
    /// Creates the requested custom from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "customMarkup">The custom markup.</param>
    /// <returns>The newly created legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartLegendEntry CreateCustom(string id, string label, string customMarkup);
}
