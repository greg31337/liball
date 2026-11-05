namespace chart;
/// <summary>Creates and converts chart legend entry values.</summary>
public class ChartLegendEntryFactory : IChartLegendEntryFactory
{
    /// <summary>Initializes a stateless chart legend entry factory facade.</summary>
    public ChartLegendEntryFactory()
    {
    }

    /// <summary>
    /// Creates the requested symbol from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartLegendEntry CreateSymbol(string id, string label, ChartMarkerShape shape, ChartStyle style)
    {
        return ChartLegendEntry.CreateSymbol(id, label, shape, style);
    }

    /// <summary>
    /// Creates the requested gradient from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "colors">The colors.</param>
    /// <param name = "offsets">The offsets.</param>
    /// <returns>The newly created legend entry.</returns>
    public ChartLegendEntry CreateGradient(string id, string label, ChartColor[] colors, double[] offsets)
    {
        return ChartLegendEntry.CreateGradient(id, label, colors, offsets);
    }

    /// <summary>
    /// Creates the requested custom from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "customMarkup">The custom markup.</param>
    /// <returns>The newly created legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartLegendEntry CreateCustom(string id, string label, string customMarkup)
    {
        return ChartLegendEntry.CreateCustom(id, label, customMarkup);
    }
}
