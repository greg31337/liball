namespace chart;
/// <summary>Creates and converts chart theme values.</summary>
public class ChartThemeFactory : IChartThemeFactory
{
    /// <summary>Initializes a stateless chart theme factory facade.</summary>
    public ChartThemeFactory()
    {
    }

    /// <summary>
    /// Creates the baseline style used when no theme or local override supplies a value.
    /// </summary>
    /// <returns>The resulting style.</returns>
    public ChartStyle LibraryDefaultStyle()
    {
        return ChartTheme.LibraryDefaultStyle();
    }

    /// <summary>
    /// Creates the library's default light theme.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme LibraryDefault()
    {
        return ChartTheme.LibraryDefault();
    }

    /// <summary>
    /// Creates the built-in light theme for screen output.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme Light()
    {
        return ChartTheme.Light();
    }

    /// <summary>
    /// Creates the built-in dark-background theme.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme Dark()
    {
        return ChartTheme.Dark();
    }

    /// <summary>
    /// Creates a high-legibility theme intended for printed output.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme Print()
    {
        return ChartTheme.Print();
    }

    /// <summary>
    /// Creates a grayscale theme that does not rely on color differences.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme Monochrome()
    {
        return ChartTheme.Monochrome();
    }

    /// <summary>
    /// Creates a theme with strongly separated foreground and background colors.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme HighContrast()
    {
        return ChartTheme.HighContrast();
    }

    /// <summary>
    /// Creates a theme using the built-in color-vision-deficiency-safe palette.
    /// </summary>
    /// <returns>The resulting theme.</returns>
    public ChartTheme ColorblindSafe()
    {
        return ChartTheme.ColorblindSafe();
    }
}
