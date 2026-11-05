namespace chart;
/// <summary>Provides the public chart palettes operations.</summary>
public class ChartPalettes : IChartPalettes
{
    /// <summary>Initializes a stateless chart palettes facade.</summary>
    public ChartPalettes()
    {
    }

    /// <summary>
    /// Creates the default categorical palette for visually distinct series.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public ChartPalette Categorical()
    {
        return ChartPalettesOperations.Categorical();
    }

    /// <summary>
    /// Creates a categorical palette designed to remain distinguishable with common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public ChartPalette ColorblindSafe()
    {
        return ChartPalettesOperations.ColorblindSafe();
    }

    /// <summary>
    /// Creates a categorical palette with strong luminance and hue separation.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public ChartPalette HighContrast()
    {
        return ChartPalettesOperations.HighContrast();
    }

    /// <summary>
    /// Creates a categorical palette containing ordered gray levels.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public ChartPalette Grayscale()
    {
        return ChartPalettesOperations.Grayscale();
    }

    /// <summary>
    /// Creates a continuous color map for values that increase in one direction.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap Sequential()
    {
        return ChartPalettesOperations.Sequential();
    }

    /// <summary>
    /// Creates a continuous color map with distinct colors on either side of a midpoint.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap Diverging()
    {
        return ChartPalettesOperations.Diverging();
    }

    /// <summary>
    /// Creates a continuous black-to-white color map.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap GrayscaleMap()
    {
        return ChartPalettesOperations.GrayscaleMap();
    }

    /// <summary>
    /// Creates a continuous color map designed for common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap ColorblindSafeMap()
    {
        return ChartPalettesOperations.ColorblindSafeMap();
    }

    /// <summary>
    /// Creates a continuous color map with strongly separated endpoint colors.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap HighContrastMap()
    {
        return ChartPalettesOperations.HighContrastMap();
    }
}
