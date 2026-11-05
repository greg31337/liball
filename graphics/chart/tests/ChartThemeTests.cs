namespace chart.Tests;

[TestFixture]
public class ChartThemeTests
{
    [Test]
    public void ResolveStyle_CascadesLibraryThemeChartPlotSeriesAndPoint()
    {
        ChartTheme theme = new ChartTheme("custom");
        ChartStyle chartStyle = new ChartStyle();
        ChartStyle plotStyle = new ChartStyle();
        ChartStyle seriesStyle = new ChartStyle();
        ChartStyle pointStyle = new ChartStyle();

        theme.Style.SetStroke(ChartColor.FromRgb(200, 0, 0), 2.0);
        theme.Style.SetFont("Inter", 14);
        theme.Style.SetOpacity(0.8);
        chartStyle.SetOpacity(0.6);
        chartStyle.SetFont("ChartFont", 16);
        plotStyle.SetFill(ChartColor.FromRgb(0, 120, 0));
        plotStyle.SetMarkerSize(6.0);
        seriesStyle.SetStroke(ChartColor.FromRgb(0, 0, 200), 3.0);
        pointStyle.SetFill(ChartColor.FromRgb(250, 200, 0));
        pointStyle.SetOpacity(0.4);

        ChartResolvedStyle resolved = ChartStyleCascade.ResolveStyle(theme, chartStyle, plotStyle, seriesStyle, pointStyle, 0);

        AssertColor(resolved.FillColor, 250, 200, 0, 255);
        AssertColor(resolved.StrokeColor, 0, 0, 200, 255);
        Assert.That(resolved.StrokeWidth, Is.EqualTo(3.0));
        Assert.That(resolved.Opacity, Is.EqualTo(0.4));
        Assert.That(resolved.FontFamily, Is.EqualTo("ChartFont"));
        Assert.That(resolved.FontSize, Is.EqualTo(16));
        Assert.That(resolved.MarkerSize, Is.EqualTo(6.0));
    }

    [Test]
    public void ResolveStyle_UsesPaletteForSeriesFillUntilExplicitOverride()
    {
        ChartTheme theme = new ChartTheme("palette");
        ChartStyle empty = new ChartStyle();
        ChartStyle chartStyle = new ChartStyle();

        theme.SetPalette(new ChartPalette(
            "test",
            new ChartColor[]
            {
                ChartColor.FromRgb(10, 20, 30),
                ChartColor.FromRgb(40, 50, 60)
            }));

        ChartResolvedStyle paletteStyle = ChartStyleCascade.ResolveStyle(theme, empty, empty, empty, empty, 1);
        chartStyle.SetFill(ChartColor.FromRgb(90, 100, 110));
        ChartResolvedStyle chartOverride = ChartStyleCascade.ResolveStyle(theme, chartStyle, empty, empty, empty, 1);

        AssertColor(paletteStyle.FillColor, 40, 50, 60, 255);
        AssertColor(chartOverride.FillColor, 90, 100, 110, 255);
    }

    [Test]
    public void ResolveDocumentStyle_UsesDocumentThemeDocumentPlotSeriesAndPoint()
    {
        ChartDocument document = new ChartDocument(300.0, 200.0);
        ChartTheme theme = new ChartTheme("document-theme");
        ChartPlot plot = new ChartPlot("plot");
        ChartStyle documentStyle = new ChartStyle();
        ChartStyle plotStyle = new ChartStyle();
        ChartStyle seriesStyle = new ChartStyle();
        ChartStyle pointStyle = new ChartStyle();

        theme.Style.SetFont("ThemeFont", 11);
        documentStyle.SetOpacity(0.7);
        plotStyle.SetMarkerSize(8.0);
        seriesStyle.SetStroke(ChartColor.FromRgb(1, 2, 3), 2.5);
        pointStyle.SetNoFill();
        document.SetTheme(theme);
        document.SetStyle(documentStyle);
        plot.SetStyle(plotStyle);
        documentStyle.SetOpacity(0.1);
        plotStyle.SetMarkerSize(99.0);

        ChartResolvedStyle resolved = ChartStyleCascade.ResolveDocumentStyle(document, plot, seriesStyle, pointStyle, 0);
        ChartDocument copy = document.Copy();
        document.Theme.Style.SetFont("Changed", 30);

        Assert.That(resolved.FillNone, Is.True);
        AssertColor(resolved.StrokeColor, 1, 2, 3, 255);
        Assert.That(resolved.StrokeWidth, Is.EqualTo(2.5));
        Assert.That(resolved.Opacity, Is.EqualTo(0.7));
        Assert.That(resolved.FontFamily, Is.EqualTo("ThemeFont"));
        Assert.That(resolved.MarkerSize, Is.EqualTo(8.0));
        Assert.That(copy.Theme.Style.FontFamily, Is.EqualTo("ThemeFont"));
    }

    [Test]
    public void ResolvedStyle_ConvertsToSvgStyle()
    {
        ChartResolvedStyle resolved = new ChartResolvedStyle();

        resolved.FillColor = ChartColor.FromRgb(10, 20, 30);
        resolved.StrokeColor = ChartColor.FromRgb(40, 50, 60);
        resolved.StrokeWidth = 2.0;
        resolved.Opacity = 0.5;
        resolved.FontFamily = "Inter";
        resolved.FontSize = 15;

        Assert.That(resolved.ToSvgStyle().ToStyleString(), Is.EqualTo("fill:#0A141E;stroke:#28323C;stroke-width:2;opacity:0.5;font-family:Inter;font-size:15"));
    }

    [Test]
    public void ChartThemeAndStyle_CopyIndependently()
    {
        ChartTheme theme = new ChartTheme("copy");

        theme.Style.SetFill(ChartColor.FromRgb(1, 2, 3));
        ChartTheme copy = theme.Copy();
        theme.Style.SetFill(ChartColor.FromRgb(9, 9, 9));

        AssertColor(copy.Style.FillColor, 1, 2, 3, 255);
        Assert.That(copy.Palette.Count(), Is.EqualTo(theme.Palette.Count()));
    }

    [Test]
    public void BuiltInThemes_ProvideExpectedBaseStylesAndPalettes()
    {
        ChartStyle empty = new ChartStyle();
        ChartTheme light = ChartTheme.Light();
        ChartTheme dark = ChartTheme.Dark();
        ChartTheme print = ChartTheme.Print();
        ChartTheme monochrome = ChartTheme.Monochrome();
        ChartTheme highContrast = ChartTheme.HighContrast();
        ChartTheme colorblindSafe = ChartTheme.ColorblindSafe();

        ChartResolvedStyle lightBase = ChartStyleCascade.ResolveStyle(light, empty, empty, empty, empty, -1);
        ChartResolvedStyle darkBase = ChartStyleCascade.ResolveStyle(dark, empty, empty, empty, empty, -1);
        ChartResolvedStyle darkSeries = ChartStyleCascade.ResolveStyle(dark, empty, empty, empty, empty, 0);
        ChartResolvedStyle printBase = ChartStyleCascade.ResolveStyle(print, empty, empty, empty, empty, -1);
        ChartResolvedStyle highContrastBase = ChartStyleCascade.ResolveStyle(highContrast, empty, empty, empty, empty, -1);
        ChartResolvedStyle colorblindSeries = ChartStyleCascade.ResolveStyle(colorblindSafe, empty, empty, empty, empty, 0);

        Assert.That(light.Name, Is.EqualTo("light"));
        Assert.That(dark.Name, Is.EqualTo("dark"));
        Assert.That(print.Name, Is.EqualTo("print"));
        Assert.That(monochrome.Name, Is.EqualTo("monochrome"));
        Assert.That(highContrast.Name, Is.EqualTo("high-contrast"));
        Assert.That(colorblindSafe.Name, Is.EqualTo("colorblind-safe"));
        Assert.That(monochrome.Palette.Name, Is.EqualTo("grayscale"));
        Assert.That(colorblindSafe.Palette.Name, Is.EqualTo("colorblind-safe"));
        AssertColor(lightBase.FillColor, 255, 255, 255, 255);
        AssertColor(lightBase.StrokeColor, 34, 34, 34, 255);
        AssertColor(darkBase.FillColor, 17, 17, 17, 255);
        AssertColor(darkBase.StrokeColor, 242, 242, 242, 255);
        AssertColor(darkSeries.FillColor, 86, 180, 233, 255);
        Assert.That(printBase.FontFamily, Is.EqualTo("Times New Roman"));
        AssertColor(highContrastBase.FillColor, 0, 0, 0, 255);
        AssertColor(highContrastBase.StrokeColor, 255, 255, 255, 255);
        Assert.That(highContrastBase.StrokeWidth, Is.EqualTo(2.0));
        Assert.That(highContrastBase.MarkerSize, Is.EqualTo(5.0));
        AssertColor(colorblindSeries.FillColor, 0, 114, 178, 255);
    }

    [Test]
    public void UseColorblindPalette_ReplacesPaletteWithIndependentCopy()
    {
        ChartTheme theme = ChartTheme.Light();

        theme.UseColorblindPalette();
        theme.Palette.Colors[0] = ChartColor.White();
        ChartTheme fresh = ChartTheme.ColorblindSafe();

        Assert.That(theme.Palette.Name, Is.EqualTo("colorblind-safe"));
        AssertColor(fresh.Palette.GetColor(0), 0, 114, 178, 255);
    }

    [Test]
    public void ThemeCascade_ValidatesInputs()
    {
        ChartTheme theme = new ChartTheme("valid");
        ChartStyle style = new ChartStyle();
        ChartDocument document = new ChartDocument(100.0, 100.0);
        ChartPlot plot = new ChartPlot("plot");
        ChartResolvedStyle resolved = new ChartResolvedStyle();

        Assert.Throws<System.ArgumentException>(() => new ChartTheme(""));
        Assert.Throws<System.ArgumentNullException>(() => theme.SetPalette(null!));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFill(null!));
        Assert.Throws<System.ArgumentNullException>(() => style.SetStroke(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStrokeWidth(-1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetOpacity(2.0));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFont(null!, 12));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFont("Arial", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetMarkerSize(-1.0));
        Assert.Throws<System.ArgumentNullException>(() => document.SetTheme(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.SetStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => plot.SetStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ResolveStyle(null!, style, style, style, style, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ResolveStyle(theme, null!, style, style, style, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ResolveDocumentStyle(null!, plot, style, style, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ResolveDocumentStyle(document, null!, style, style, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ApplyStyle(null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartStyleCascade.ApplyStyle(resolved, null!));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
        Assert.That(color.A, Is.EqualTo(a));
    }
}
