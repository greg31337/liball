namespace chart;
/// <summary>
/// Represents a theme used by the chart model or rendering pipeline.
/// </summary>
public class ChartTheme : IChartTheme
{
    internal string Name;
    internal ChartStyle Style;
    internal ChartPalette Palette;
    /// <summary>
    /// Initializes a theme from the supplied name.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    public ChartTheme(string name)
    {
        ChartPlot.ValidateId(name);
        Name = name;
        Style = new ChartStyle();
        Palette = ChartPalettesOperations.Categorical();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartTheme Copy()
    {
        ChartTheme theme = new ChartTheme(Name);
        theme.Style = Style.Copy();
        theme.Palette = new ChartPalette(Palette.Name, Palette.CopyColors());
        return theme;
    }

    /// <summary>
    /// Applies the series color operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "seriesIndex">The zero-based series index.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor SeriesColor(int seriesIndex)
    {
        return Palette.GetColor(seriesIndex);
    }

    /// <summary>
    /// Sets the palette configuration.
    /// </summary>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetPalette(ChartPalette palette)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }

        Palette = new ChartPalette(palette.Name, palette.CopyColors());
    }

    /// <summary>
    /// Selects colorblind palette using the supplied data and configuration.
    /// </summary>
    public void UseColorblindPalette()
    {
        SetPalette(ChartPalettesOperations.ColorblindSafe());
    }

    internal static ChartStyle LibraryDefaultStyle()
    {
        ChartStyle style = new ChartStyle();
        style.SetFill(ChartColor.FromRgb(31, 119, 180));
        style.SetStroke(ChartColor.Black(), 1.0);
        style.SetOpacity(1.0);
        style.SetFont("Arial", 12);
        style.SetMarkerSize(4.0);
        return style;
    }

    internal static ChartTheme LibraryDefault()
    {
        ChartTheme theme = new ChartTheme("library-default");
        theme.Style = new ChartStyle();
        theme.Palette = ChartPalettesOperations.Categorical();
        return theme;
    }

    internal static ChartTheme Light()
    {
        ChartTheme theme = new ChartTheme("light");
        theme.Style.SetFill(ChartColor.White());
        theme.Style.SetStroke(ChartColor.FromRgb(34, 34, 34), 1.0);
        theme.Style.SetOpacity(1.0);
        theme.Style.SetFont("Arial", 12);
        theme.Style.SetMarkerSize(4.0);
        theme.SetPalette(ChartPalettesOperations.Categorical());
        return theme;
    }

    internal static ChartTheme Dark()
    {
        ChartTheme theme = new ChartTheme("dark");
        theme.Style.SetFill(ChartColor.FromRgb(17, 17, 17));
        theme.Style.SetStroke(ChartColor.FromRgb(242, 242, 242), 1.0);
        theme.Style.SetOpacity(1.0);
        theme.Style.SetFont("Arial", 12);
        theme.Style.SetMarkerSize(4.0);
        theme.SetPalette(new ChartPalette("dark", new ChartColor[] { ChartColor.Parse("#56b4e9"), ChartColor.Parse("#e69f00"), ChartColor.Parse("#009e73"), ChartColor.Parse("#f0e442"), ChartColor.Parse("#cc79a7"), ChartColor.Parse("#d55e00") }));
        return theme;
    }

    internal static ChartTheme Print()
    {
        ChartTheme theme = new ChartTheme("print");
        theme.Style.SetFill(ChartColor.White());
        theme.Style.SetStroke(ChartColor.Black(), 1.0);
        theme.Style.SetOpacity(1.0);
        theme.Style.SetFont("Times New Roman", 12);
        theme.Style.SetMarkerSize(4.0);
        theme.SetPalette(ChartPalettesOperations.Grayscale());
        return theme;
    }

    internal static ChartTheme Monochrome()
    {
        ChartTheme theme = new ChartTheme("monochrome");
        theme.Style.SetFill(ChartColor.White());
        theme.Style.SetStroke(ChartColor.Black(), 1.0);
        theme.Style.SetOpacity(1.0);
        theme.Style.SetFont("Arial", 12);
        theme.Style.SetMarkerSize(4.0);
        theme.SetPalette(ChartPalettesOperations.Grayscale());
        return theme;
    }

    internal static ChartTheme HighContrast()
    {
        ChartTheme theme = new ChartTheme("high-contrast");
        theme.Style.SetFill(ChartColor.Black());
        theme.Style.SetStroke(ChartColor.White(), 2.0);
        theme.Style.SetOpacity(1.0);
        theme.Style.SetFont("Arial", 13);
        theme.Style.SetMarkerSize(5.0);
        theme.SetPalette(ChartPalettesOperations.HighContrast());
        return theme;
    }

    internal static ChartTheme ColorblindSafe()
    {
        ChartTheme theme = Light();
        theme.Name = "colorblind-safe";
        theme.SetPalette(ChartPalettesOperations.ColorblindSafe());
        return theme;
    }

    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetName(string value)
    {
        Name = value;
    }

    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    public ChartStyle GetStyle()
    {
        return Style;
    }

    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStyle(ChartStyle value)
    {
        Style = value;
    }

    /// <summary>Returns the current palette.</summary>
    /// <returns>The current field value.</returns>
    public ChartPalette GetPalette()
    {
        return Palette;
    }
}
