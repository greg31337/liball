namespace chart.Tests;

[TestFixture]
public class ChartAdvancedAxisTests
{
    [Test]
    public void AxisRenderer_RendersSubtitleUnitsOffsetAndThresholdLines()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 5.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        axis.SetTitle("Elapsed");
        axis.SetSubtitle("Wall clock duration");
        axis.SetUnit("ms");
        axis.SetAxisOffset(10.0);
        axis.SetTickLabels(true, false, 0.0);
        axis.AddThresholdLine(5.0, "limit", ChartColor.FromRgb(255, 0, 0), 2.0);
        SvgWriter writer = new SvgWriter(160, 160);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-rule\" x1=\"10\" y1=\"90\" x2=\"110\" y2=\"90\""));
        Assert.That(svg, Does.Contain("<line id=\"axis-bottom-threshold-0\" x1=\"60\" y1=\"20\" x2=\"60\" y2=\"80\" style=\"fill:none;stroke:#FF0000;stroke-width:2\"/>"));
        Assert.That(svg, Does.Contain(">limit</text>"));
        Assert.That(svg, Does.Contain(">5 ms</text>"));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-title\""));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-subtitle\""));
    }

    [Test]
    public void AxisRenderer_UsesIndependentLineTickMinorBandAndLabelStyles()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 5.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        axis.SetStroke(ChartColor.FromRgb(255, 0, 0), 1.5);
        axis.SetTickStyle(ChartColor.FromRgb(0, 128, 0), 2.0);
        axis.SetMinorTicks(true, 1, 3.0);
        axis.SetMinorTickStyle(ChartColor.FromRgb(0, 0, 255), 3.0);
        axis.SetGridBands(true, ChartColor.FromRgb(255, 255, 0));
        axis.SetTickLabels(true, false, 0.0);
        axis.SetLabelStyle(ChartColor.FromRgb(255, 0, 255), "Inter", 12);
        SvgWriter writer = new SvgWriter(160, 140);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("id=\"axis-bottom-rule\"").And.Contain("stroke:#FF0000;stroke-width:1.5"));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-tick-1\"").And.Contain("stroke:#008000;stroke-width:2"));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-minor-0-1\"").And.Contain("stroke:#0000FF;stroke-width:3"));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-band-0\"").And.Contain("fill:#FFFF00"));
        Assert.That(svg, Does.Contain("id=\"axis-bottom-label-1\"").And.Contain("fill:#FF00FF;stroke:none;font-family:Inter;font-size:12"));
    }

    [Test]
    public void AxisBreaks_CollapseRenderingAndCartesianTransforms()
    {
        ChartAxis xAxis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 4.0, 5.0, 6.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));
        ChartAxis yAxis = ChartAxis.CreateLeft(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));
        xAxis.AddBreak(4.0, 6.0);
        xAxis.SetTickLabels(true, false, 0.0);
        SvgWriter writer = new SvgWriter(130, 130);

        ChartAxisRenderer.RenderSvg(writer, xAxis);
        ChartCartesianTransform transform = ChartCartesianTransform.FromAxes(xAxis, yAxis);
        string svg = writer.ToSvgString();

        Assert.That(ChartAxisRenderer.MapX(xAxis, 4.0), Is.EqualTo(50.0));
        Assert.That(ChartAxisRenderer.MapX(xAxis, 6.0), Is.EqualTo(50.0));
        Assert.That(svg, Does.Not.Contain("axis-bottom-tick-2"));
        Assert.That(svg, Does.Not.Contain("axis-bottom-label-2"));
        Assert.That(svg, Does.Contain("axis-bottom-break-0-a"));
        Assert.That(transform.MapX(4.0), Is.EqualTo(50.0));
        Assert.That(transform.MapX(6.0), Is.EqualTo(50.0));
        Assert.That(transform.MapX(8.0), Is.EqualTo(75.0));
        Assert.That(transform.InvertX(50.0), Is.EqualTo(4.0));
        Assert.That(transform.ContainsDataPoint(5.0, 5.0), Is.False);
    }

    [Test]
    public void LinkedAxis_ConvertsAndResynchronizesDomainAndTicks()
    {
        ChartAxis source = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 50.0, 100.0 }),
            new ChartRange(0.0, 100.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));
        source.SetId("celsius");
        ChartAxis linked = ChartAxis.CreateTop(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));

        linked.SetLinkedAxis(source, 1.8, 32.0);

        Assert.That(linked.GetDomain().GetMinimum(), Is.EqualTo(32.0));
        Assert.That(linked.GetDomain().GetMaximum(), Is.EqualTo(212.0));
        Assert.That(linked.GetTicks()[1].GetValue(), Is.EqualTo(122.0));

        source.SetDomain(new ChartRange(-10.0, 10.0));
        source.SetTicks(ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { -10.0, 0.0, 10.0 }));
        linked.SynchronizeLinkedAxis(source);

        Assert.That(linked.GetDomain().GetMinimum(), Is.EqualTo(14.0));
        Assert.That(linked.GetDomain().GetMaximum(), Is.EqualTo(50.0));
        Assert.That(linked.GetTicks()[1].GetValue(), Is.EqualTo(32.0));
    }

    [Test]
    public void TernaryAxisFactory_RendersEdgeTicksLabelsAndThresholds()
    {
        ChartAxisFactory factory = new ChartAxisFactory();
        ChartAxis axis = factory.CreateTernary(
            new ChartPoint(0.0, 100.0),
            new ChartPoint(100.0, 100.0),
            new ChartPoint(50.0, 0.0),
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 0.5, 1.0 }));
        axis.SetTickLabels(true, false, 0.0);
        axis.SetTitle("Component A");
        axis.SetSubtitle("fraction");
        axis.AddThresholdLine(0.25, "target", ChartColor.FromRgb(255, 0, 0), 1.0);
        SvgWriter writer = new SvgWriter(140, 150);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(axis.GetKind(), Is.EqualTo(ChartAxisKind.Ternary));
        Assert.That(svg, Does.Contain("<line id=\"axis-ternary-rule\" x1=\"0\" y1=\"100\" x2=\"100\" y2=\"100\""));
        Assert.That(svg, Does.Contain("<line id=\"axis-ternary-tick-1\" x1=\"50\" y1=\"100\" x2=\"50\" y2=\"106\""));
        Assert.That(svg, Does.Contain("<line id=\"axis-ternary-threshold-0\" x1=\"25\" y1=\"100\" x2=\"50\" y2=\"0\""));
        Assert.That(svg, Does.Contain("id=\"axis-ternary-title\""));
        Assert.That(svg, Does.Contain("id=\"axis-ternary-subtitle\""));
    }

    [Test]
    public void FixedAspectRatio_ConstrainsPlotAndTransformGeometry()
    {
        ChartPlot plot = new ChartPlot("plot");
        plot.SetBounds(new ChartRect(0.0, 0.0, 200.0, 100.0));
        plot.SetFixedAspectRatio(1.0);

        ChartCartesianTransform transform = plot.CreateCartesianTransform(new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));

        Assert.That(transform.GetPlotArea().GetX(), Is.EqualTo(50.0));
        Assert.That(transform.GetPlotArea().GetWidth(), Is.EqualTo(100.0));
        Assert.That(transform.MapX(0.0), Is.EqualTo(50.0));
        Assert.That(transform.MapX(10.0), Is.EqualTo(150.0));
        Assert.That(transform.MapY(0.0), Is.EqualTo(100.0));
        Assert.That(transform.MapY(10.0), Is.EqualTo(0.0));

        transform.ClearFixedAspectRatio();
        Assert.That(transform.GetPlotArea().GetWidth(), Is.EqualTo(200.0));
    }

    [Test]
    public void AdvancedAxisFeatures_ValidateInvalidConfiguration()
    {
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));
        ChartAxis yAxis = ChartAxis.CreateLeft(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));

        Assert.Throws<System.ArgumentNullException>(() => axis.SetSubtitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => axis.SetUnit(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => axis.SetAxisOffset(-1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => axis.SetLinkedAxis(axis, 0.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => axis.SetLinkedAxis(yAxis, 1.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => axis.AddBreak(5.0, 4.0));
        Assert.Throws<System.ArgumentException>(() => axis.AddBreak(0.0, 2.0));
        axis.AddBreak(2.0, 4.0);
        Assert.Throws<System.ArgumentException>(() => axis.AddBreak(3.0, 5.0));
        Assert.Throws<System.ArgumentNullException>(() => axis.AddThresholdLine(5.0, "", null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => axis.SetTickStyle(ChartColor.Black(), 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartCartesianTransform(new ChartRect(0.0, 0.0, 10.0, 10.0), new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)).SetFixedAspectRatio(0.0));
    }
}
