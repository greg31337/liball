namespace chart.Tests;

[TestFixture]
public class ChartAccessibilityTests
{
    [Test]
    public void AccessibilityMetadata_StoresSeriesPointsAndCopiesDeeply()
    {
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();

        metadata.SetTitleDescription("Sales chart", "Quarterly sales by region.");
        ChartSeriesAccessibility series = metadata.AddSeriesDescription("sales", "Sales values by quarter.");
        series.AddPointDescription("sales-q1", "Q1 sales were highest.", 0);
        metadata.AddPointDescription("profit", "profit-q1", "Profit margin for Q1.", 0);
        ChartAccessibilityMetadata copy = metadata.Copy();

        copy.SetTitle("Changed");
        copy.SeriesAt(0).SetDescription("Changed series");
        copy.SeriesAt(0).PointAt(0).Description = "Changed point";

        Assert.That(metadata.Title, Is.EqualTo("Sales chart"));
        Assert.That(metadata.Description, Is.EqualTo("Quarterly sales by region."));
        Assert.That(metadata.SeriesCount, Is.EqualTo(2));
        Assert.That(metadata.SeriesAt(0).SeriesId, Is.EqualTo("sales"));
        Assert.That(metadata.SeriesAt(0).Description, Is.EqualTo("Sales values by quarter."));
        Assert.That(metadata.SeriesAt(0).PointAt(0).Description, Is.EqualTo("Q1 sales were highest."));
        Assert.That(metadata.SeriesAt(1).SeriesId, Is.EqualTo("profit"));
        Assert.That(metadata.HasMetadata(), Is.True);
    }

    [Test]
    public void DataTableSidecarPlan_CapturesTableShapeAndColumnMetadata()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddCategoryColumn("region", new string[] { "West", "East" });
        table.AddNumberColumn("sales", new double[] { 10.0, 12.0 });
        ChartDataTableSidecarPlan plan = ChartDataTableSidecarPlan.FromTable("chart-data", "Source data", table);
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();

        metadata.SetDataTableSidecar(plan);
        plan.ColumnNames[0] = "changed";

        Assert.That(metadata.DataTableSidecar, Is.Not.Null);
        Assert.That(metadata.DataTableSidecar!.TableId, Is.EqualTo("chart-data"));
        Assert.That(metadata.DataTableSidecar.Caption, Is.EqualTo("Source data"));
        Assert.That(metadata.DataTableSidecar.RowCount, Is.EqualTo(2));
        Assert.That(metadata.DataTableSidecar.ColumnCount, Is.EqualTo(2));
        Assert.That(metadata.DataTableSidecar.ColumnNames[0], Is.EqualTo("region"));
        Assert.That(metadata.DataTableSidecar.ColumnKinds[0], Is.EqualTo(ChartDataKind.Category));
        Assert.That(metadata.DataTableSidecar.SummaryText(), Does.Contain("data-table chart-data: 2 rows, 2 columns"));
        Assert.That(metadata.MetadataText(), Does.Contain("region=Category"));
    }

    [Test]
    public void AccessibleSvgExport_EmbedsTitleDescriptionSeriesPointsAndSidecarMetadata()
    {
        ChartRenderScene scene = new ChartRenderScene(30, 20);
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartDataTable table = new ChartDataTable();
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();

        scene.AddLine("trend", 0.0, 10.0, 30.0, 10.0, style);
        table.AddNumberColumn("x", new double[] { 0.0, 1.0 });
        table.AddNumberColumn("y", new double[] { 3.0, 4.0 });
        metadata.SetTitleDescription("Sales & Profit", "Line chart with <two> points.");
        metadata.AddSeriesDescription("trend", "Trend line.");
        metadata.AddPointDescription("trend", "trend-0", "First plotted point.", 0);
        metadata.PlanDataTableSidecar("chart-data", "Underlying values", table);

        string svg = ChartExportOperations.ToSvg(scene, metadata);

        Assert.That(svg, Does.Contain("<title>Sales &amp; Profit</title>"));
        Assert.That(svg, Does.Contain("<desc>Line chart with &lt;two&gt; points.</desc>"));
        Assert.That(svg, Does.Contain("<metadata>series trend: Trend line."));
        Assert.That(svg, Does.Contain("point trend/trend-0 row 0: First plotted point."));
        Assert.That(svg, Does.Contain("data-table chart-data: 2 rows, 2 columns"));
        Assert.That(svg, Does.Contain("<line id=\"trend\""));
        AssertValidXml(svg);
    }

    [Test]
    public void ChartDocument_CopiesAccessibilityMetadata()
    {
        ChartDocument document = new ChartDocument(100.0, 60.0);
        document.SetAccessibilityTitleDescription("Document title", "Document description");
        document.Accessibility.AddSeriesDescription("series", "Series description");

        ChartDocument copy = document.Copy();
        copy.Accessibility.SetTitle("Changed");
        copy.Accessibility.SeriesAt(0).SetDescription("Changed series");

        Assert.That(document.Accessibility.Title, Is.EqualTo("Document title"));
        Assert.That(document.Accessibility.Description, Is.EqualTo("Document description"));
        Assert.That(document.Accessibility.SeriesAt(0).Description, Is.EqualTo("Series description"));
        Assert.That(copy.Accessibility.Title, Is.EqualTo("Changed"));
    }

    [Test]
    public void AccessibilityMetadata_ValidatesInputs()
    {
        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("value", new double[] { 1.0 });

        Assert.Throws<System.ArgumentNullException>(() => metadata.SetTitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => metadata.SetDescription(null!));
        Assert.Throws<System.ArgumentException>(() => metadata.AddSeriesDescription("", "description"));
        Assert.DoesNotThrow(() => metadata.AddSeriesDescription("series", "one"));
        Assert.Throws<System.ArgumentException>(() => metadata.AddSeriesDescription("series", "duplicate"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => metadata.AddPointDescription("series-two", "point", "description", -1));
        Assert.DoesNotThrow(() => metadata.AddPointDescription("series-two", "point", "description", 0));
        Assert.Throws<System.ArgumentException>(() => metadata.AddPointDescription("series-two", "point", "duplicate", 0));
        Assert.Throws<System.ArgumentNullException>(() => metadata.SetDataTableSidecar(null!));
        Assert.Throws<System.ArgumentNullException>(() => metadata.PlanDataTableSidecar("table", "caption", null!));
        Assert.Throws<System.ArgumentException>(() => ChartDataTableSidecarPlan.FromTable("", "caption", table));
        Assert.Throws<System.ArgumentException>(() => new ChartDataTableSidecarPlan("table", "caption", 1, 2, new string[] { "one" }, new ChartDataKind[] { ChartDataKind.Number, ChartDataKind.Text }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartDataTableSidecarPlan.ValidateDataKind((ChartDataKind)99));
        Assert.Throws<System.ArgumentNullException>(() => metadata.ApplyToSvgWriter(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvg((SvgWriter)null!, metadata));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvg(new SvgWriter(10, 10), null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvg((ChartRenderScene)null!, metadata));
        Assert.Throws<System.ArgumentNullException>(() => ChartExportOperations.ToSvg(new ChartRenderScene(10, 10), null!));
        Assert.Throws<System.ArgumentNullException>(() => new ChartDocument(10.0, 10.0).SetAccessibility(null!));
    }

    static void AssertValidXml(string xml)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(xml);
    }
}
