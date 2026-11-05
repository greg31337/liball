namespace chart;
/// <summary>Defines substitutable chart theme factory operations.</summary>
public interface IChartThemeFactory
{
    /// <summary>
    /// Creates the baseline style used when no theme or local override supplies a value.
    /// </summary>
    /// <returns>The resulting style.</returns>
    ChartStyle LibraryDefaultStyle();
    /// <summary>
    /// Creates the library's default light theme.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme LibraryDefault();
    /// <summary>
    /// Creates the built-in light theme for screen output.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme Light();
    /// <summary>
    /// Creates the built-in dark-background theme.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme Dark();
    /// <summary>
    /// Creates a high-legibility theme intended for printed output.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme Print();
    /// <summary>
    /// Creates a grayscale theme that does not rely on color differences.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme Monochrome();
    /// <summary>
    /// Creates a theme with strongly separated foreground and background colors.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme HighContrast();
    /// <summary>
    /// Creates a theme using the built-in color-vision-deficiency-safe palette.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    ChartTheme ColorblindSafe();
}
