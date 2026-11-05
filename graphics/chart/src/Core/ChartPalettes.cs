namespace chart;
internal static class ChartPalettesOperations
{
    /// <summary>
    /// Creates the default categorical palette for visually distinct series.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public static ChartPalette Categorical()
    {
        return new ChartPalette("categorical", new ChartColor[] { ChartColor.Parse("#1f77b4"), ChartColor.Parse("#ff7f0e"), ChartColor.Parse("#2ca02c"), ChartColor.Parse("#d62728"), ChartColor.Parse("#9467bd"), ChartColor.Parse("#8c564b"), ChartColor.Parse("#e377c2"), ChartColor.Parse("#7f7f7f"), ChartColor.Parse("#bcbd22"), ChartColor.Parse("#17becf") });
    }

    /// <summary>
    /// Creates a categorical palette designed to remain distinguishable with common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public static ChartPalette ColorblindSafe()
    {
        return new ChartPalette("colorblind-safe", new ChartColor[] { ChartColor.Parse("#0072b2"), ChartColor.Parse("#e69f00"), ChartColor.Parse("#009e73"), ChartColor.Parse("#d55e00"), ChartColor.Parse("#cc79a7"), ChartColor.Parse("#56b4e9"), ChartColor.Parse("#f0e442"), ChartColor.Parse("#000000") });
    }

    /// <summary>
    /// Creates a categorical palette with strong luminance and hue separation.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public static ChartPalette HighContrast()
    {
        return new ChartPalette("high-contrast", new ChartColor[] { ChartColor.Parse("#000000"), ChartColor.Parse("#ffffff"), ChartColor.Parse("#ffff00"), ChartColor.Parse("#00ffff"), ChartColor.Parse("#ff00ff"), ChartColor.Parse("#ff0000"), ChartColor.Parse("#00ff00"), ChartColor.Parse("#0000ff") });
    }

    /// <summary>
    /// Creates a categorical palette containing ordered gray levels.
    /// </summary>
    /// <returns>The resulting palette.</returns>
    public static ChartPalette Grayscale()
    {
        return new ChartPalette("grayscale", new ChartColor[] { ChartColor.Parse("#111111"), ChartColor.Parse("#444444"), ChartColor.Parse("#777777"), ChartColor.Parse("#aaaaaa"), ChartColor.Parse("#dddddd") });
    }

    /// <summary>
    /// Creates a continuous color map for values that increase in one direction.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public static ChartColorMap Sequential()
    {
        return new ChartColorMap("sequential", new ChartColor[] { ChartColor.Parse("#f7fbff"), ChartColor.Parse("#c6dbef"), ChartColor.Parse("#6baed6"), ChartColor.Parse("#2171b5"), ChartColor.Parse("#08306b") });
    }

    /// <summary>
    /// Creates a continuous color map with distinct colors on either side of a midpoint.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public static ChartColorMap Diverging()
    {
        return new ChartColorMap("diverging", new ChartColor[] { ChartColor.Parse("#2166ac"), ChartColor.Parse("#67a9cf"), ChartColor.Parse("#f7f7f7"), ChartColor.Parse("#ef8a62"), ChartColor.Parse("#b2182b") });
    }

    /// <summary>
    /// Creates a continuous black-to-white color map.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public static ChartColorMap GrayscaleMap()
    {
        return new ChartColorMap("grayscale", new ChartColor[] { ChartColor.Parse("#000000"), ChartColor.Parse("#ffffff") });
    }

    /// <summary>
    /// Creates a continuous color map designed for common color-vision deficiencies.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public static ChartColorMap ColorblindSafeMap()
    {
        return new ChartColorMap("colorblind-safe", new ChartColor[] { ChartColor.Parse("#f0e442"), ChartColor.Parse("#009e73"), ChartColor.Parse("#0072b2") });
    }

    /// <summary>
    /// Creates a continuous color map with strongly separated endpoint colors.
    /// </summary>
    /// <returns>The resulting color map.</returns>
    public static ChartColorMap HighContrastMap()
    {
        return new ChartColorMap("high-contrast", new ChartColor[] { ChartColor.Parse("#000000"), ChartColor.Parse("#ffff00"), ChartColor.Parse("#ffffff") });
    }
}
