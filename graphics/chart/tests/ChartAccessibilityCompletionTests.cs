namespace chart.Tests;

[TestFixture]
public class ChartAccessibilityCompletionTests
{
    [Test]
    public void StructuredAlternatives_IncludeDescriptionsCuesTableAndInteractionMetadata()
    {
        ChartDataTable table = CreateAccessibleTable();
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        metadata.SetTitleDescription("Revenue \"overview\"", "Quarterly revenue\nby region.");
        ChartSeriesAccessibility series = metadata.AddSeriesDescription("revenue", "Revenue series.");
        series.SetNonColorCues(ChartMarkerShape.Diamond, new double[] { 4.0, 2.0 }, "Q1 values", "diagonal hatch");
        series.AddPointDescription("revenue-q1", "First point.", 0);
        metadata.PlanDataTableSidecar("revenue-data", "Underlying values", table);
        ChartSelection selection = new ChartSelection("chosen", "shared");
        selection.SetSelectedRows(new int[] { 1 });
        ChartSelectionSidecar sidecar = new ChartSelectionSidecar();
        sidecar.AddSelection(selection);
        metadata.SetSelectionSidecar(sidecar);

        string text = metadata.StructuredText();
        string json = metadata.JsonAlternative();
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);
        System.Text.Json.JsonElement root = document.RootElement;

        Assert.That(text, Does.Contain("Title: Revenue \"overview\""));
        Assert.That(text, Does.Contain("Description: Quarterly revenue\nby region."));
        Assert.That(text, Does.Contain("- revenue: Revenue series."));
        Assert.That(text, Does.Contain("Non-color cues: shape=Diamond; dash=4,2; label=Q1 values; pattern=diagonal hatch"));
        Assert.That(text, Does.Contain("- revenue-q1 (row 0): First point."));
        Assert.That(text, Does.Contain("data-table revenue-data: 2 rows, 5 columns"));
        Assert.That(text, Does.Contain("Interaction metadata:\nlinked-selection chosen group shared"));
        Assert.That(root.GetProperty("title").GetString(), Is.EqualTo("Revenue \"overview\""));
        Assert.That(root.GetProperty("description").GetString(), Is.EqualTo("Quarterly revenue\nby region."));
        Assert.That(root.GetProperty("series")[0].GetProperty("nonColorCues").GetProperty("shape").GetString(), Is.EqualTo("Diamond"));
        Assert.That(root.GetProperty("series")[0].GetProperty("nonColorCues").GetProperty("dash")[1].GetDouble(), Is.EqualTo(2.0));
        Assert.That(root.GetProperty("series")[0].GetProperty("points")[0].GetProperty("description").GetString(), Is.EqualTo("First point."));
        Assert.That(root.GetProperty("dataTable").GetProperty("columns")[4].GetProperty("kind").GetString(), Is.EqualTo("Matrix"));
        Assert.That(root.GetProperty("interactionMetadata").GetString(), Does.Contain("selected=1"));
    }

    [Test]
    public void TabularExports_PreserveTypesMissingValuesAndDelimitedEscaping()
    {
        ChartDataTable table = CreateAccessibleTable();

        string csv = table.ToCsv();
        string tsv = table.ToTsv();
        string json = table.ToJsonTable();
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);
        System.Text.Json.JsonElement root = document.RootElement;

        string expectedCsv = "region,sales,note,when,matrix\nWest,12.5,\"He said \"\"go\"\"\nnow\",2024-01-02T03:04:05.0000000Z,7\n\"East, North\",,,2024-01-03T00:00:00.0000000Z,8";
        Assert.That(csv, Is.EqualTo(expectedCsv));
        Assert.That(tsv, Does.StartWith("region\tsales\tnote\twhen\tmatrix\nWest\t12.5\t\"He said \"\"go\"\"\nnow\""));
        Assert.That(tsv, Does.Contain("\nEast, North\t\t\t2024-01-03T00:00:00.0000000Z\t8"));
        Assert.That(root.GetProperty("columns")[0].GetProperty("kind").GetString(), Is.EqualTo("Category"));
        Assert.That(root.GetProperty("rows")[0][1].GetDouble(), Is.EqualTo(12.5));
        Assert.That(root.GetProperty("rows")[0][2].GetString(), Is.EqualTo("He said \"go\"\nnow"));
        Assert.That(root.GetProperty("rows")[1][1].ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
        Assert.That(root.GetProperty("rows")[1][2].ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
        Assert.That(root.GetProperty("rows")[1][4].GetDouble(), Is.EqualTo(8.0));

        ChartDataTable empty = new ChartDataTable();
        Assert.That(empty.ToCsv(), Is.Empty);
        Assert.That(empty.ToTsv(), Is.Empty);
        Assert.That(empty.ToJsonTable(), Is.EqualTo("{\"columns\":[],\"rows\":[]}"));
    }

    [Test]
    public void AccessibleSvg_EmitsSemanticDescriptionsAndVisibleNonColorCues()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 24);
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        line.SetDashArray(new double[] { 4.0, 2.0 });
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle pattern = new ChartRenderStyle();
        RawBitmap tile = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        tile.SetPixel(0, 0, ChartColor.Black());
        tile.SetPixel(1, 0, ChartColor.White());
        pattern.SetFillPattern(tile);

        scene.BeginGroup("revenue");
        scene.AddLine("revenue-line", 1.0, 4.0, 30.0, 4.0, line);
        scene.AddMarker("revenue-point", ChartMarkerShape.Diamond, 8.0, 8.0, 5.0, fill);
        scene.AddText("value-label", "Q1", 12.0, 12.0, fill);
        scene.AddRect("pattern", 20.0, 8.0, 8.0, 5.0, 0.0, 0.0, pattern);
        scene.EndGroup();

        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        ChartSeriesAccessibility series = metadata.AddSeriesDescription("revenue", "Revenue series.");
        series.SetNonColorCues(ChartMarkerShape.Diamond, new double[] { 4.0, 2.0 }, "Q1 values", "diagonal hatch");
        series.AddPointDescription("revenue-point", "First point.", 0);

        string svg = new ChartExport().ToSvg(scene, metadata);

        Assert.That(svg, Does.Contain("<g id=\"revenue\" role=\"group\" aria-label=\"Revenue series. Non-color cues: shape=Diamond; dash=4,2; label=Q1 values; pattern=diagonal hatch\""));
        Assert.That(svg, Does.Contain("data-marker-shape=\"Diamond\""));
        Assert.That(svg, Does.Contain("data-dash-pattern=\"4,2\""));
        Assert.That(svg, Does.Contain("data-value-label=\"Q1 values\""));
        Assert.That(svg, Does.Contain("data-pattern-description=\"diagonal hatch\""));
        Assert.That(svg, Does.Contain("aria-label=\"First point. Series non-color cues: shape=Diamond; dash=4,2; label=Q1 values; pattern=diagonal hatch\""));
        Assert.That(svg, Does.Contain("series revenue: Revenue series.; non-color-cues=shape=Diamond; dash=4,2; label=Q1 values; pattern=diagonal hatch"));
        Assert.That(svg, Does.Contain("stroke-dasharray:4 2"));
        Assert.That(svg, Does.Contain("<polygon id=\"revenue-point\""));
        Assert.That(svg, Does.Contain(">Q1</text>"));
        Assert.That(svg, Does.Contain("<pattern id=\"pattern-fill-pattern\""));
        AssertValidXml(svg);
    }

    [Test]
    public void NonColorCueMetadata_CopiesInputsAndValidatesValues()
    {
        double[] dash = new double[] { 3.0, 1.0 };
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        ChartSeriesAccessibility series = metadata.AddSeriesDescription("series", "Description");
        series.SetNonColorCues(ChartMarkerShape.Triangle, dash, "labels", "dots");
        dash[0] = 99.0;
        ChartAccessibilityMetadata copy = metadata.Copy();
        series.SetNonColorCues(ChartMarkerShape.Circle, new double[0], "", "");

        Assert.That(copy.SeriesAt(0).UsesNonColorCues(), Is.True);
        Assert.That(copy.SeriesAt(0).GetCueShape(), Is.EqualTo(ChartMarkerShape.Triangle));
        Assert.That(copy.SeriesAt(0).CopyCueDashArray(), Is.EqualTo(new double[] { 3.0, 1.0 }));
        Assert.That(copy.SeriesAt(0).GetCueLabel(), Is.EqualTo("labels"));
        Assert.That(copy.SeriesAt(0).GetCuePatternDescription(), Is.EqualTo("dots"));

        Assert.Throws<System.ArgumentOutOfRangeException>(() => series.SetNonColorCues((ChartMarkerShape)99, new double[0], "", ""));
        Assert.Throws<System.ArgumentNullException>(() => series.SetNonColorCues(ChartMarkerShape.Circle, null!, "", ""));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => series.SetNonColorCues(ChartMarkerShape.Circle, new double[] { 0.0 }, "", ""));
        Assert.Throws<System.ArgumentNullException>(() => series.SetNonColorCues(ChartMarkerShape.Circle, new double[0], null!, ""));
        Assert.Throws<System.ArgumentNullException>(() => series.SetNonColorCues(ChartMarkerShape.Circle, new double[0], "", null!));
    }

    static ChartDataTable CreateAccessibleTable()
    {
        ChartDataMatrix matrix = new ChartDataMatrix(2, 1);
        matrix.SetValue(0, 0, 7.0);
        matrix.SetValue(1, 0, 8.0);
        ChartDataTable table = new ChartDataTable();
        table.AddCategoryColumn("region", new string? [] { "West", "East, North" });
        table.AddNumberColumn("sales", new double[] { 12.5, double.NaN });
        table.AddTextColumn("note", new string? [] { "He said \"go\"\nnow", null });
        table.AddDateTimeColumn("when", new System.DateTime[]
        {
            new System.DateTime(2024, 1, 2, 3, 4, 5, System.DateTimeKind.Utc),
            new System.DateTime(2024, 1, 3, 0, 0, 0, System.DateTimeKind.Utc)
        });
        table.AddMatrixColumn("matrix", matrix);
        return table;
    }

    static void AssertValidXml(string xml)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(xml);
    }
}
