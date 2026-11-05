namespace chart.Tests;

[TestFixture]
public class ChartGoldenTests
{
    [TestCase("line", "58CD9FE965518903", "id=\"gold-line-segment-0\"")]
    [TestCase("bar", "F9C7FE6FFD5EFEF5", "id=\"gold-bar-0\"")]
    [TestCase("scatter", "3EF132EC05F7E538", "id=\"gold-point-1\"")]
    [TestCase("heatmap", "AAFD9989B21B8378", "id=\"gold-cell-1-1\"")]
    [TestCase("pie", "BE1BF7C43777E538", "id=\"gold-slice-2\"")]
    [TestCase("polar", "511B552ADF245DF5", "id=\"gold-bar-1\"")]
    [TestCase("financial", "D0F0FA75B06AB40D", "id=\"gold-body-1\"")]
    [TestCase("hierarchy", "C81F823BD69A7B4B", "id=\"gold-sun-item-4\"")]
    [TestCase("network", "986480675B3B28DD", "id=\"gold-node-3\"")]
    public void SvgGoldenCharts_MatchDeterministicSnapshots(string chartKind, string expectedHash, string expectedFragment)
    {
        string first = BuildGoldenSvg(chartKind);
        string second = BuildGoldenSvg(chartKind);
        string actualHash = StableHash(first);

        Assert.That(second, Is.EqualTo(first), chartKind + " SVG should be deterministic");
        Assert.That(first, Does.Contain(expectedFragment), chartKind + " SVG should contain its expected retained id");
        AssertValidXml(first);
        Assert.That(actualHash, Is.EqualTo(expectedHash), chartKind + " SVG hash");
    }

    static string BuildGoldenSvg(string chartKind)
    {
        if (chartKind == "line")
        {
            return BuildLineGoldenSvg();
        }

        if (chartKind == "bar")
        {
            return BuildBarGoldenSvg();
        }

        if (chartKind == "scatter")
        {
            return BuildScatterGoldenSvg();
        }

        if (chartKind == "heatmap")
        {
            return BuildHeatmapGoldenSvg();
        }

        if (chartKind == "pie")
        {
            return BuildPieGoldenSvg();
        }

        if (chartKind == "polar")
        {
            return BuildPolarGoldenSvg();
        }

        if (chartKind == "financial")
        {
            return BuildFinancialGoldenSvg();
        }

        if (chartKind == "hierarchy")
        {
            return BuildHierarchyGoldenSvg();
        }

        if (chartKind == "network")
        {
            return BuildNetworkGoldenSvg();
        }

        throw new System.ArgumentException("Unknown golden chart kind.");
    }

    static string BuildLineGoldenSvg()
    {
        ChartCartesianRenderer renderer = CreateRenderer(80, 60, 0.0, 3.0, 0.0, 4.0);
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries(
            "gold",
            new double[] { 0.0, 1.0, 2.0, 3.0 },
            new double[] { 1.0, 3.0, 2.0, 4.0 },
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.5));

        renderer.AddSeries(line);
        return RenderCartesianGoldenSvg(renderer, 80, 60);
    }

    static string BuildBarGoldenSvg()
    {
        ChartCartesianRenderer renderer = CreateRenderer(80, 60, 0.0, 4.0, 0.0, 5.0);
        ChartCartesianSeries bars = ChartBarChartsOperations.CreateVerticalBarSeries(
            "gold",
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 3.0, 5.0, 2.0 },
            0.7,
            0.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 40, 40)));

        renderer.AddSeries(bars);
        return RenderCartesianGoldenSvg(renderer, 80, 60);
    }

    static string BuildScatterGoldenSvg()
    {
        ChartCartesianRenderer renderer = CreateRenderer(80, 60, 0.0, 4.0, 0.0, 4.0);
        ChartCartesianSeries scatter = ChartScatterChartsOperations.CreateScatterSeries(
            "gold",
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 1.0, 3.0, 2.0 },
            ChartMarkerShape.Diamond,
            6.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(40, 90, 200)));

        renderer.AddSeries(scatter);
        return RenderCartesianGoldenSvg(renderer, 80, 60);
    }

    static string BuildHeatmapGoldenSvg()
    {
        ChartCartesianRenderer renderer = CreateRenderer(80, 60, 0.0, 2.0, 0.0, 2.0);
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 3.0, ChartPalettesOperations.GrayscaleMap());
        ChartCartesianSeries heatmap = ChartGridChartsOperations.CreateHeatmapSeries(
            "gold",
            new double[] { 0.5, 1.5 },
            new double[] { 0.5, 1.5 },
            ChartDataMatrix.FromValues(2, 2, new double[] { 0.0, 1.0, 2.0, 3.0 }),
            1.0,
            1.0,
            map);

        renderer.AddSeries(heatmap);
        return RenderCartesianGoldenSvg(renderer, 80, 60);
    }

    static string BuildPieGoldenSvg()
    {
        ChartPieLayout layout = ChartPieChartsOperations.LayoutPie(
            new string[] { "North", "South", "West" },
            new double[] { 2.0, 1.0, 3.0 },
            50.0,
            50.0,
            28.0,
            0.0,
            ChartPieSliceOrder.Original,
            ChartPalettesOperations.Categorical());
        ChartRenderScene scene = ChartPieChartsOperations.CreatePieScene("gold", 100, 100, layout);

        scene.AddClear(ChartColor.White());
        ChartPieChartsOperations.AddLabelsAndLeaderLines(scene, "gold", layout, 36.0, ChartRenderStyle.CreateFill(ChartColor.Black()), ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        return scene.ToSvgString();
    }

    static string BuildPolarGoldenSvg()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 100);
        ChartPolarTransform transform = new ChartPolarTransform(50.0, 50.0, 8.0, 40.0, 0.0, 10.0, 0.0, true);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(40, 120, 200));

        scene.AddClear(ChartColor.White());
        ChartPolarChartsOperations.AddPolarLine(scene, "gold", new double[] { 0.0, 90.0, 180.0 }, new double[] { 4.0, 8.0, 6.0 }, transform, false, stroke);
        ChartPolarChartsOperations.AddPolarScatter(scene, "gold", new double[] { 45.0, 225.0 }, new double[] { 6.0, 4.0 }, transform, ChartMarkerShape.Circle, 5.0, fill);
        ChartPolarChartsOperations.AddPolarBars(scene, "gold", new double[] { 300.0, 330.0 }, new double[] { 5.0, 9.0 }, transform, 20.0, 0.0, fill);
        return scene.ToSvgString();
    }

    static string BuildFinancialGoldenSvg()
    {
        ChartCartesianRenderer renderer = CreateRenderer(100, 70, 0.5, 3.5, 8.0, 15.0);
        ChartRenderStyle up = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 160, 0));
        ChartRenderStyle down = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));
        ChartRenderStyle wick = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartCartesianSeries candles = ChartFinancialChartsOperations.CreateCandlestickSeries(
            "gold",
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 10.0, 13.0, 11.0 },
            new double[] { 13.0, 14.0, 12.0 },
            new double[] { 9.0, 10.0, 10.0 },
            new double[] { 12.0, 11.0, 12.0 },
            0.5,
            up,
            down,
            wick);
        ChartCartesianSeries average = ChartFinancialChartsOperations.CreateMovingAverageSeries(
            "gold-ma",
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 12.0, 11.0, 12.0 },
            2,
            ChartRenderStyle.CreateStroke(ChartColor.FromRgb(40, 80, 200), 1.0));

        average.SetOrder(1);
        renderer.AddSeries(candles);
        renderer.AddSeries(average);
        return RenderCartesianGoldenSvg(renderer, 100, 70);
    }

    static string BuildHierarchyGoldenSvg()
    {
        ChartRenderScene scene = new ChartRenderScene(120, 120);
        ChartHierarchyData data = CreateGoldenHierarchy();
        ChartHierarchyRectLayout treemap = ChartHierarchyChartsOperations.LayoutTreemap(data, new ChartRect(0.0, 0.0, 50.0, 50.0), ChartPalettesOperations.Categorical());
        ChartSunburstLayout sunburst = ChartHierarchyChartsOperations.LayoutSunburst(data, 80.0, 80.0, 8.0, 36.0, 0.0, 360.0, ChartPalettesOperations.Categorical());

        scene.AddClear(ChartColor.White());
        ChartHierarchyChartsOperations.AddRectLayoutToScene(scene, "gold-tree", treemap);
        ChartHierarchyChartsOperations.AddSunburstToScene(scene, "gold-sun", sunburst);
        return scene.ToSvgString();
    }

    static string BuildNetworkGoldenSvg()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 80);
        ChartGraphLink[] links = new ChartGraphLink[]
        {
            new ChartGraphLink(0, 1, 2.0),
            new ChartGraphLink(1, 2, 1.0),
            new ChartGraphLink(2, 3, 3.0),
            new ChartGraphLink(3, 0, 1.0)
        };
        ChartGraphLayout layout = ChartRelationshipChartsOperations.LayoutNodeLink(new string[] { "A", "B", "C", "D" }, links, new ChartRect(10.0, 10.0, 80.0, 60.0));
        ChartRenderStyle linkStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle nodeStyle = ChartRenderStyle.CreateFill(ChartColor.FromRgb(40, 90, 200));
        ChartRenderStyle labelStyle = ChartRenderStyle.CreateFill(ChartColor.Black());

        scene.AddClear(ChartColor.White());
        int index = 0;
        while (index < layout.LinkCount)
        {
            ChartGraphLinkLayout link = layout.LinkAt(index);
            scene.AddLine("gold-link-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), link.X1, link.Y1, link.X2, link.Y2, linkStyle);
            index = index + 1;
        }

        index = 0;
        while (index < layout.NodeCount)
        {
            ChartGraphNodeLayout node = layout.NodeAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddMarker("gold-node-" + suffix, ChartMarkerShape.Circle, node.X, node.Y, 7.0, nodeStyle);
            scene.AddText("gold-label-" + suffix, node.Label, node.X + 4.0, node.Y - 4.0, labelStyle);
            index = index + 1;
        }

        return scene.ToSvgString();
    }

    static ChartCartesianRenderer CreateRenderer(int width, int height, double xMinimum, double xMaximum, double yMinimum, double yMaximum)
    {
        return new ChartCartesianRenderer(new ChartCartesianTransform(new ChartRect(0.0, 0.0, width, height), new ChartRange(xMinimum, xMaximum), new ChartRange(yMinimum, yMaximum)));
    }

    static string RenderCartesianGoldenSvg(ChartCartesianRenderer renderer, int width, int height)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        scene.AddClear(ChartColor.White());
        renderer.RenderToScene(scene);
        return scene.ToSvgString();
    }

    static ChartHierarchyData CreateGoldenHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("A", 0.0, 0),
                new ChartHierarchyNode("B", 0.0, 0),
                new ChartHierarchyNode("A1", 2.0, 1),
                new ChartHierarchyNode("A2", 1.0, 1),
                new ChartHierarchyNode("B1", 3.0, 2)
            });
    }

    static string StableHash(string value)
    {
        ulong hash = 14695981039346656037UL;
        int index = 0;
        while (index < value.Length)
        {
            hash = hash ^ value[index];
            hash = hash * 1099511628211UL;
            index = index + 1;
        }

        return hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
    }

    static void AssertValidXml(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        Assert.That(document.DocumentElement!.Name, Is.EqualTo("svg"));
    }
}
