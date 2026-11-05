namespace chart;
/// <summary>Formats value labels with deterministic invariant general-number formatting.</summary>
public class ChartDefaultValueLabelFormatter : IChartValueLabelFormatter
{
    /// <summary>Initializes the default value-label formatter.</summary>
    public ChartDefaultValueLabelFormatter()
    {
    }

    /// <summary>Formats a numeric value with invariant general-number formatting.</summary>
    /// <param name = "value">The finite value to format.</param>
    /// <returns>The formatted value label.</returns>
    public string FormatValue(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }
}
