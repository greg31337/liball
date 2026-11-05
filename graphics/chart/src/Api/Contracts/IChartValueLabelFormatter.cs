namespace chart;
/// <summary>Formats numeric chart values for point, bar, cell, slice, contour, and annotation labels.</summary>
public interface IChartValueLabelFormatter
{
    /// <summary>Formats a numeric value as display text.</summary>
    /// <param name = "value">The finite value to format.</param>
    /// <returns>The formatted value label.</returns>
    string FormatValue(double value);
}
