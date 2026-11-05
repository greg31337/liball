namespace chart;
/// <summary>Defines substitutable chart palettes operations.</summary>
public interface IChartPalettes
{
    /// <summary>
    /// Creates the default categorical palette for visually distinct series.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    ChartPalette Categorical();
    /// <summary>
    /// Creates a categorical palette designed to remain distinguishable with common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    ChartPalette ColorblindSafe();
    /// <summary>
    /// Creates a categorical palette with strong luminance and hue separation.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    ChartPalette HighContrast();
    /// <summary>
    /// Creates a categorical palette containing ordered gray levels.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    ChartPalette Grayscale();
    /// <summary>
    /// Creates a continuous color map for values that increase in one direction.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    ChartColorMap Sequential();
    /// <summary>
    /// Creates a continuous color map with distinct colors on either side of a midpoint.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    ChartColorMap Diverging();
    /// <summary>
    /// Creates a continuous black-to-white color map.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    ChartColorMap GrayscaleMap();
    /// <summary>
    /// Creates a continuous color map designed for common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    ChartColorMap ColorblindSafeMap();
    /// <summary>
    /// Creates a continuous color map with strongly separated endpoint colors.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    ChartColorMap HighContrastMap();
}
