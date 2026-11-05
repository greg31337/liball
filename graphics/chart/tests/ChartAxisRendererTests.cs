namespace chart.Tests;

[TestFixture]
public class ChartAxisRendererTests
{
    [Test]
    public void ChartAxis_CreateBottomCopiesTicksAndDefaults()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateNumericTicks(0.0, 10.0, 3);
        ChartAxis axis = ChartAxis.CreateBottom(ticks, new ChartRange(0.0, 10.0), new ChartRect(10.0, 20.0, 100.0, 60.0));

        ticks[0].Value = 99.0;

        Assert.That(axis.Kind, Is.EqualTo(ChartAxisKind.X));
        Assert.That(axis.Placement, Is.EqualTo(ChartAxisPlacement.Bottom));
        Assert.That(axis.Ticks[0].Value, Is.EqualTo(0.0));
        Assert.That(axis.Id, Is.EqualTo("axis-bottom"));
        Assert.That(axis.TickLength, Is.EqualTo(6.0));
        Assert.That(axis.Radius, Is.EqualTo(30.0));
    }

    [Test]
    public void RenderSvg_DrawsBottomCartesianAxisRuleAndTicks()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 5.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        SvgWriter writer = new SvgWriter(140, 120);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<g id=\"axis-bottom\">"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-rule\" x1=\"10\" y1=\"80\" x2=\"110\" y2=\"80\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-tick-0\" x1=\"10\" y1=\"80\" x2=\"10\" y2=\"86\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-tick-1\" x1=\"60\" y1=\"80\" x2=\"60\" y2=\"86\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-tick-2\" x1=\"110\" y1=\"80\" x2=\"110\" y2=\"86\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
    }

    [Test]
    public void RenderSvg_DrawsLeftCartesianAxisRuleAndTicks()
    {
        ChartAxis axis = ChartAxis.CreateLeft(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 5.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        SvgWriter writer = new SvgWriter(140, 120);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<line id=\"axis-left-rule\" x1=\"10\" y1=\"20\" x2=\"10\" y2=\"80\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-left-tick-0\" x1=\"10\" y1=\"80\" x2=\"4\" y2=\"80\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-left-tick-1\" x1=\"10\" y1=\"50\" x2=\"4\" y2=\"50\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-left-tick-2\" x1=\"10\" y1=\"20\" x2=\"4\" y2=\"20\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
    }

    [Test]
    public void RenderSvg_DrawsRadialRings()
    {
        ChartAxis axis = ChartAxis.CreateRadial(
            new ChartPoint(50.0, 50.0),
            40.0,
            ChartTickGeneratorOperations.GenerateRadialTicks(10.0, 3),
            new ChartRange(0.0, 10.0));
        SvgWriter writer = new SvgWriter(100, 100);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<g id=\"axis-radial\">"));
        Assert.That(svg, Does.Contain("<circle id=\"axis-radial-ring-1\" cx=\"50\" cy=\"50\" r=\"20\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<circle id=\"axis-radial-ring-2\" cx=\"50\" cy=\"50\" r=\"40\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Not.Contain("axis-radial-ring-0"));
    }

    [Test]
    public void RenderSvg_DrawsAngularCircleAndSpokes()
    {
        ChartAxis axis = ChartAxis.CreateAngular(
            new ChartPoint(50.0, 50.0),
            40.0,
            ChartTickGeneratorOperations.GenerateAngularTicks(4));
        SvgWriter writer = new SvgWriter(100, 100);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<circle id=\"axis-angular-rule\" cx=\"50\" cy=\"50\" r=\"40\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-angular-tick-0\" x1=\"50\" y1=\"50\" x2=\"50\" y2=\"10\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-angular-tick-1\" x1=\"50\" y1=\"50\" x2=\"90\" y2=\"50\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-angular-tick-2\" x1=\"50\" y1=\"50\" x2=\"50\" y2=\"90\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-angular-tick-3\" x1=\"50\" y1=\"50\" x2=\"10\" y2=\"50\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
    }

    [Test]
    public void RenderSvg_DrawsTitleLabelsMinorTicksGridLinesAndBands()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 2.5, 5.0, 7.5, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        SvgWriter writer = new SvgWriter(160, 130);

        axis.SetTitle("X Value");
        axis.SetTickLabels(true, true, 40.0);
        axis.SetMinorTicks(true, 1, 3.0);
        axis.SetGridLines(true, 0.0, ChartColor.FromRgb(220, 220, 220), 1.0);
        axis.SetGridBands(true, ChartColor.FromRgb(245, 245, 245));

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<rect id=\"axis-bottom-band-0\" x=\"10\" y=\"20\" width=\"25\" height=\"60\" style=\"fill:#F5F5F5;stroke:none\"/>"));
        Assert.That(svg, Does.Contain("<rect id=\"axis-bottom-band-2\" x=\"60\" y=\"20\" width=\"25\" height=\"60\" style=\"fill:#F5F5F5;stroke:none\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-grid-2\" x1=\"60\" y1=\"80\" x2=\"60\" y2=\"20\" style=\"fill:none;stroke:#DCDCDC;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-minor-0-1\" x1=\"22.5\" y1=\"80\" x2=\"22.5\" y2=\"83\" style=\"fill:none;stroke:#000000;stroke-width:1\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"axis-bottom-label-0\" x=\"10\" y=\"98\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">0</text>"));
        Assert.That(svg, Does.Contain("<text id=\"axis-bottom-label-2\" x=\"60\" y=\"98\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">5</text>"));
        Assert.That(svg, Does.Contain("<text id=\"axis-bottom-label-4\" x=\"110\" y=\"98\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">10</text>"));
        Assert.That(svg, Does.Not.Contain("axis-bottom-label-1"));
        Assert.That(svg, Does.Not.Contain("axis-bottom-label-3"));
        Assert.That(svg, Does.Contain("<text id=\"axis-bottom-title\" x=\"60\" y=\"112\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">X Value</text>"));
    }

    [Test]
    public void RenderSvg_DrawsZeroLineWhenDomainContainsZero()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { -10.0, 0.0, 10.0 }),
            new ChartRange(-10.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        SvgWriter writer = new SvgWriter(140, 120);

        axis.SetZeroLine(true, ChartColor.FromRgb(255, 0, 0), 2.0);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-zero\" x1=\"60\" y1=\"20\" x2=\"60\" y2=\"80\" style=\"fill:none;stroke:#FF0000;stroke-width:2\"/>"));
    }

    [Test]
    public void RenderSvg_DrawsVerticalTitleAndPolarLabels()
    {
        ChartAxis left = ChartAxis.CreateLeft(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        ChartAxis radial = ChartAxis.CreateRadial(
            new ChartPoint(50.0, 50.0),
            40.0,
            ChartTickGeneratorOperations.GenerateRadialTicks(10.0, 3),
            new ChartRange(0.0, 10.0));
        SvgWriter writer = new SvgWriter(160, 160);

        left.SetTitle("Y Value");
        radial.SetTitle("Radius");
        radial.SetTickLabels(true, false, 0.0);

        ChartAxisRenderer.RenderSvg(writer, left);
        ChartAxisRenderer.RenderSvg(writer, radial);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<text id=\"axis-left-title\" x=\"-22\" y=\"50\" transform=\"rotate(-90 -22 50)\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">Y Value</text>"));
        Assert.That(svg, Does.Contain("<text id=\"axis-radial-label-1\" x=\"82\" y=\"50\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">5</text>"));
        Assert.That(svg, Does.Contain("<text id=\"axis-radial-title\" x=\"50\" y=\"-22\" style=\"fill:#000000;stroke:none;font-family:Arial;font-size:10\">Radius</text>"));
    }

    [Test]
    public void AxisRenderer_MapsValuesToAxisCoordinates()
    {
        ChartAxis axis = ChartAxis.CreateRight(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));

        Assert.That(ChartAxisRenderer.MapX(axis, 5.0), Is.EqualTo(60.0));
        Assert.That(ChartAxisRenderer.MapY(axis, 5.0), Is.EqualTo(50.0));
        Assert.That(ChartAxisRenderer.MapRadius(axis, 5.0), Is.EqualTo(15.0));
        Assert.That(ChartAxisRenderer.PointAtAngle(axis, 90.0, 30.0).X, Is.EqualTo(90.0).Within(0.0000001));
    }

    [Test]
    public void ChartAxis_ValidatesInputs()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 });
        ChartRange domain = new ChartRange(0.0, 1.0);
        ChartRect plotArea = new ChartRect(0.0, 0.0, 10.0, 10.0);

        Assert.Throws<System.ArgumentException>(() => new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Left, ticks, domain, plotArea));
        Assert.Throws<System.ArgumentException>(() => new ChartAxis(ChartAxisKind.Y, ChartAxisPlacement.Top, ticks, domain, plotArea));
        Assert.Throws<System.ArgumentException>(() => new ChartAxis(ChartAxisKind.Ternary, ChartAxisPlacement.Bottom, ticks, domain, plotArea));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(null!, domain, plotArea));
        Assert.Throws<System.ArgumentException>(() => ChartAxis.CreateBottom(ticks, new ChartRange(1.0, 1.0), plotArea));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, null!, plotArea));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, null!));
        Assert.Throws<System.ArgumentException>(() => ChartAxis.CreateBottom(ticks, domain, new ChartRect(0.0, 0.0, 0.0, 10.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateRadial(null!, 10.0, ticks, domain));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateRadial(new ChartPoint(0.0, 0.0), 0.0, ticks, domain));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTickLength(-1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetMinorTicks(true, -1, 3.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetMinorTicks(true, 1, -1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTickLabels(true, true, -1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTitle(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetOffsets(-1.0, 2.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetStroke(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetStroke(ChartColor.Black(), -1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetGridLines(true, 1.0, null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetGridLines(true, -1.0, ChartColor.Black(), 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetZeroLine(true, null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetZeroLine(true, ChartColor.Black(), -1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetGridBands(true, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTextStyle(null!, "Arial", 10));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTextStyle(ChartColor.Black(), null!, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxis.CreateBottom(ticks, domain, plotArea).SetTextStyle(ChartColor.Black(), "Arial", 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.RenderSvg(null!, ChartAxis.CreateBottom(ticks, domain, plotArea)));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.RenderSvg(new SvgWriter(10, 10), null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.CreateAxisStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.CreateGridStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.CreateZeroLineStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.CreateBandStyle(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartAxisRenderer.CreateTextStyle(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxisRenderer.MinorTickValue(ChartAxis.CreateBottom(ticks, domain, plotArea), -1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAxisRenderer.MinorTickValue(ChartAxis.CreateBottom(ticks, domain, plotArea), 0, 1));
    }
}
