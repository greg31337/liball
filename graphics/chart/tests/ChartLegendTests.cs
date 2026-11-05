namespace chart.Tests;

[TestFixture]
public class ChartLegendTests
{
    [Test]
    public void LegendModel_AddsEntriesAndCopiesIndependently()
    {
        ChartLegend legend = new ChartLegend("legend");
        ChartStyle style = new ChartStyle();

        style.SetFill(ChartColor.FromRgb(255, 0, 0));
        legend.SetTitle("Legend");
        legend.SetSpacing(6.0, 4.0);
        legend.SetSymbolSize(10.0);
        legend.SetGradientSize(40.0, 8.0);
        legend.SetTextStyle(ChartColor.FromRgb(1, 2, 3), "Inter", 11);
        legend.SetBackground(ChartColor.White());
        legend.SetBorder(ChartColor.Black(), 1.0);
        ChartLegendEntry added = legend.AddSymbolEntry("alpha", "Alpha", ChartMarkerShape.Circle, style);
        legend.AddGradientEntry(
            "heat",
            "Heat",
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 });
        legend.AddCustomEntry("custom", "Custom", "<path id=\"custom-symbol\" d=\"M0 0\"/>");
        style.SetFill(ChartColor.FromRgb(0, 0, 255));

        ChartLegend copy = legend.Copy();
        added.Label = "Changed";

        Assert.That(legend.EntryCount, Is.EqualTo(3));
        Assert.That(legend.EntryAt(0).Label, Is.EqualTo("Changed"));
        Assert.That(copy.EntryAt(0).Label, Is.EqualTo("Alpha"));
        AssertColor(copy.EntryAt(0).Style.FillColor, 255, 0, 0, 255);
        Assert.That(copy.EntryAt(1).GradientColors.Length, Is.EqualTo(2));
        Assert.That(copy.EntryAt(2).CustomMarkup, Is.EqualTo("<path id=\"custom-symbol\" d=\"M0 0\"/>"));
        Assert.That(copy.FontFamily, Is.EqualTo("Inter"));
        Assert.That(copy.HasBackground, Is.True);
        Assert.That(copy.ShowBorder, Is.True);
    }

    [Test]
    public void LegendLayout_ComputesPreferredSizeAndEntryBounds()
    {
        ChartLegend legend = CreateSampleLegend();

        ChartSize size = ChartLegendLayoutEngine.PreferredSize(legend);
        ChartLegendLayout layout = ChartLegendLayoutEngine.LayoutAt(legend, 10.0, 20.0);

        Assert.That(size.Width, Is.EqualTo(94.0).Within(0.0000001));
        Assert.That(size.Height, Is.EqualTo(80.0).Within(0.0000001));
        AssertRect(layout.Bounds, 10.0, 20.0, 94.0, 80.0);
        AssertRect(layout.TitleBounds, 18.0, 28.0, 78.0, 10.0);
        AssertRect(layout.EntryAt(0).SymbolBounds, 18.0, 44.0, 12.0, 12.0);
        AssertRect(layout.EntryAt(0).LabelBounds, 36.0, 44.0, 60.0, 12.0);
        AssertRect(layout.EntryAt(1).SymbolBounds, 18.0, 63.0, 48.0, 10.0);
    }

    [Test]
    public void LegendRenderer_RendersSymbolGradientCustomAndLabels()
    {
        ChartLegend legend = CreateSampleLegend();
        SvgWriter writer = new SvgWriter(140, 120);
        ChartLegendLayout layout = ChartLegendLayoutEngine.LayoutAt(legend, 10.0, 20.0);

        legend.SetBackground(ChartColor.White());
        legend.SetBorder(ChartColor.Black(), 1.0);
        ChartLegendRenderer.RenderSvg(writer, legend, layout);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<g id=\"legend\">"));
        Assert.That(svg, Does.Contain("<rect id=\"legend-frame\" x=\"10\" y=\"20\" width=\"94\" height=\"80\" style=\"fill:#FFFFFF;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"legend-title\" x=\"18\" y=\"38\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">Legend</text>"));
        Assert.That(svg, Does.Contain("<circle id=\"alpha-symbol\" cx=\"24\" cy=\"50\" r=\"6\""));
        Assert.That(svg, Does.Contain("<text id=\"legend-alpha-label\" x=\"36\" y=\"54\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">Alpha</text>"));
        Assert.That(svg, Does.Contain("<linearGradient id=\"legend-heat-gradient\" x1=\"18\" y1=\"63\" x2=\"66\" y2=\"63\"><stop offset=\"0%\" stop-color=\"#000000\"/><stop offset=\"100%\" stop-color=\"#FFFFFF\"/></linearGradient>"));
        Assert.That(svg, Does.Contain("<rect id=\"legend-heat-symbol\" x=\"18\" y=\"63\" width=\"48\" height=\"10\" fill=\"url(#legend-heat-gradient)\"/>"));
        Assert.That(svg, Does.Contain("<path id=\"custom-symbol\" d=\"M0 0\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"legend-custom-label\""));
    }

    [Test]
    public void LegendRenderer_RendersPolygonAndLineSymbols()
    {
        ChartLegend legend = new ChartLegend("legend-shapes");
        ChartStyle style = new ChartStyle();
        SvgWriter writer = new SvgWriter(160, 120);

        style.SetFill(ChartColor.FromRgb(10, 20, 30));
        legend.AddSymbolEntry("diamond", "Diamond", ChartMarkerShape.Diamond, style);
        legend.AddSymbolEntry("line", "Line", ChartMarkerShape.Line, style);

        ChartLegendRenderer.RenderSvg(writer, legend, ChartLegendLayoutEngine.LayoutAt(legend, 0.0, 0.0));
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<polygon id=\"diamond-symbol\""));
        Assert.That(svg, Does.Contain("<line id=\"line-symbol\""));
    }

    [Test]
    public void Legend_ValidatesInputs()
    {
        ChartLegend legend = new ChartLegend("legend");
        ChartStyle style = new ChartStyle();
        ChartLegendLayout layout = ChartLegendLayoutEngine.LayoutAt(legend, 0.0, 0.0);
        SvgWriter writer = new SvgWriter(10, 10);

        Assert.Throws<System.ArgumentException>(() => new ChartLegend(""));
        Assert.Throws<System.ArgumentNullException>(() => legend.SetTitle(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.SetSpacing(-1.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.SetSymbolSize(0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.SetGradientSize(0.0, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => legend.SetTextStyle(null!, "Arial", 10));
        Assert.Throws<System.ArgumentNullException>(() => legend.SetTextStyle(ChartColor.Black(), null!, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.SetTextStyle(ChartColor.Black(), "Arial", 0));
        Assert.Throws<System.ArgumentNullException>(() => legend.SetBackground(null!));
        Assert.Throws<System.ArgumentNullException>(() => legend.SetBorder(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.SetBorder(ChartColor.Black(), -1.0));
        Assert.Throws<System.ArgumentNullException>(() => legend.AddEntry(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => legend.EntryAt(0));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendEntry.CreateSymbol("a", "A", ChartMarkerShape.Circle, null!));
        Assert.Throws<System.ArgumentException>(() => ChartLegendEntry.CreateGradient("g", "G", new ChartColor[] { ChartColor.Black() }, new double[] { 0.0, 1.0 }));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendEntry.CreateCustom("c", "C", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendLayoutEngine.PreferredSize(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendLayoutEngine.LayoutInBounds(null!, new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => ChartLegendLayoutEngine.LayoutInBounds(legend, new ChartRect(0.0, 0.0, 0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendLayoutEngine.TextWidth(null!, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLegendLayoutEngine.TextWidth("x", 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendRenderer.RenderSvg(null!, legend, layout));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendRenderer.RenderSvg(writer, null!, layout));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendRenderer.RenderSvg(writer, legend, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendRenderer.CreateEntryStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendRenderer.CreateTextStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLegendEntry.CopyDoubleArray(null!));
        Assert.That(style.Copy(), Is.Not.Null);
    }

    static ChartLegend CreateSampleLegend()
    {
        ChartLegend legend = new ChartLegend("legend");
        ChartStyle style = new ChartStyle();

        style.SetFill(ChartColor.FromRgb(255, 0, 0));
        legend.SetTitle("Legend");
        legend.SetTextStyle(ChartColor.Black(), "Arial", 10);
        legend.AddSymbolEntry("alpha", "Alpha", ChartMarkerShape.Circle, style);
        legend.AddGradientEntry(
            "heat",
            "Heat",
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 });
        legend.AddCustomEntry("custom", "Custom", "<path id=\"custom-symbol\" d=\"M0 0\"/>");
        return legend;
    }

    static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.Height, Is.EqualTo(height).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
        Assert.That(color.A, Is.EqualTo(a));
    }
}
