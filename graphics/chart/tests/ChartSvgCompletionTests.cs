namespace chart.Tests;

[TestFixture]
public class ChartSvgCompletionTests
{
    [Test]
    public void AccessibleExport_AnnotatesRootSeriesAndPointGroups()
    {
        ChartRenderScene scene = new ChartRenderScene(30, 20);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(20, 40, 60, 255));
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();

        scene.BeginGroup("sales");
        scene.AddLine("sales", 1.0, 2.0, 20.0, 2.0, stroke);
        scene.BeginGroup("sales-point-0");
        scene.AddCircle("mark", 5.0, 5.0, 2.0, fill);
        scene.EndGroup();
        scene.EndGroup();
        accessibility.SetTitleDescription("Sales & margin", "A sales series with one highlighted point.");
        accessibility.AddSeriesDescription("sales", "Quarterly sales.");
        accessibility.AddPointDescription("sales", "sales-point-0", "First quarter.", 0);

        string svg = new ChartExport().ToSvg(scene, accessibility);

        Assert.That(svg, Does.StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"30\" height=\"20\" viewBox=\"0 0 30 20\" role=\"img\" aria-label=\"Sales &amp; margin\">"));
        Assert.That(svg, Does.Contain("<g id=\"sales\" role=\"group\" aria-label=\"Quarterly sales.\" data-series-id=\"sales\">"));
        Assert.That(svg, Does.Contain("<line id=\"sales-2\"").And.Contain("role=\"group\" aria-label=\"Quarterly sales.\" data-series-id=\"sales\""));
        Assert.That(svg, Does.Contain("<g id=\"sales-point-0\" role=\"img\" aria-label=\"First quarter.\" data-series-id=\"sales\" data-point-id=\"sales-point-0\" data-row-index=\"0\">"));
        AssertValidXml(svg);
    }

    [Test]
    public void SceneSvg_SanitizesAndDisambiguatesRetainedIdentifiersDeterministically()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());

        scene.AddRect("1 bad/id", 0.0, 0.0, 2.0, 2.0, 0.0, 0.0, fill);
        scene.AddRect("1 bad/id", 3.0, 0.0, 2.0, 2.0, 0.0, 0.0, fill);
        scene.AddCircle("1 bad/id", 8.0, 2.0, 1.0, fill);

        string first = scene.ToSvgString();
        string second = scene.ToSvgString();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.Contain("id=\"id-1-bad-id\""));
        Assert.That(first, Does.Contain("id=\"id-1-bad-id-2\""));
        Assert.That(first, Does.Contain("id=\"id-1-bad-id-3\""));
        AssertUniqueIds(first);
    }

    [Test]
    public void DuplicatePaintCommands_ReceiveUniqueDeterministicDefinitionReferences()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        ChartRenderStyle gradient = new ChartRenderStyle();
        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(10.0, 0.0),
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 });

        scene.AddRect("paint", 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, gradient);
        scene.AddRect("paint", 5.0, 0.0, 4.0, 4.0, 0.0, 0.0, gradient);

        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("<linearGradient id=\"paint-fill-gradient\""));
        Assert.That(svg, Does.Contain("<rect id=\"paint\"").And.Contain("fill:url(#paint-fill-gradient)"));
        Assert.That(svg, Does.Contain("<linearGradient id=\"paint-2-fill-gradient\""));
        Assert.That(svg, Does.Contain("<rect id=\"paint-2\"").And.Contain("fill:url(#paint-2-fill-gradient)"));
        AssertUniqueIds(svg);
    }

    [Test]
    public void CssClassExport_DeduplicatesStylesPrefixesIdsAndRetainsPaintAndImageOutput()
    {
        ChartRenderScene scene = new ChartRenderScene(24, 16);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(200, 10, 20, 255));
        ChartRenderStyle gradient = new ChartRenderStyle();
        ChartRenderStyle pattern = new ChartRenderStyle();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartSvgOptions options = new ChartSvgOptions();
        RawBitmap tile = new RawBitmapFactory().WrapIndexed(
            2,
            1,
            2,
            new byte[] { 0, 1 },
            new ChartColor[]
            {
                new ChartColor(0, 0, 0, 0),
                new ChartColor(0, 100, 220, 255)
            });

        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(8.0, 0.0),
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 });
        pattern.SetFillPattern(tile);
        scene.AddRect("box", 0.0, 0.0, 3.0, 3.0, 0.0, 0.0, fill);
        scene.AddCircle("point", 6.0, 2.0, 1.5, fill);
        scene.AddRect("gradient", 0.0, 5.0, 8.0, 2.0, 0.0, 0.0, gradient);
        scene.AddRect("pattern", 10.0, 5.0, 6.0, 2.0, 0.0, 0.0, pattern);
        scene.AddImage("bitmap", tile, 18.0, 0.0, 4.0, 2.0, new ChartRenderStyle());
        accessibility.AddPointDescription("sales", "point", "Selected point.", 2);
        options.UseCssClasses = true;
        options.IdPrefix = "chart one";

        ChartExport export = new ChartExport();
        string first = export.ToSvgWithOptions(scene, accessibility, options);
        string second = export.ToSvgWithOptions(scene, accessibility, options);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.Not.Contain(" style=\""));
        Assert.That(first, Does.Contain("<rect id=\"chart-one-box\"").And.Contain("class=\"chart-rect chart-style-1\""));
        Assert.That(first, Does.Contain("<circle id=\"chart-one-point\"").And.Contain("class=\"chart-circle chart-point chart-style-1\""));
        Assert.That(first, Does.Contain("role=\"img\" aria-label=\"Selected point.\" data-series-id=\"sales\" data-point-id=\"point\" data-row-index=\"2\""));
        Assert.That(first, Does.Contain("<linearGradient id=\"chart-one-gradient-fill-gradient\""));
        Assert.That(first, Does.Contain("fill:url(#chart-one-gradient-fill-gradient)"));
        Assert.That(first, Does.Contain("<pattern id=\"chart-one-pattern-fill-pattern\""));
        Assert.That(first, Does.Contain("<image id=\"chart-one-bitmap\"").And.Contain("href=\"data:image/png;base64,"));
        Assert.That(CountOccurrences(first, ".chart-style-1{"), Is.EqualTo(1));
        AssertValidXml(first);
        AssertUniqueIds(first);
    }

    [Test]
    public void SvgOptions_CanSuppressRolesWhileKeepingSemanticLabelsAndData()
    {
        ChartRenderScene scene = new ChartRenderScene(10, 10);
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartSvgOptions options = new ChartSvgOptions();

        scene.AddCircle("point", 5.0, 5.0, 2.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        accessibility.SetTitle("Chart title");
        accessibility.AddPointDescription("series", "point", "Point label", 1);
        options.IncludeAccessibilityRoles = false;

        string svg = new ChartExport().ToSvgWithOptions(scene, accessibility, options);

        Assert.That(svg, Does.Not.Contain(" role=\""));
        Assert.That(svg, Does.Contain("aria-label=\"Point label\""));
        Assert.That(svg, Does.Contain("data-series-id=\"series\" data-point-id=\"point\" data-row-index=\"1\""));
        AssertValidXml(svg);
    }

    [Test]
    public void SvgSerialization_RejectsInvalidOptionsAndUnbalancedStructuralGroups()
    {
        ChartRenderScene openGroup = new ChartRenderScene(10, 10);
        ChartRenderScene mismatched = new ChartRenderScene(10, 10);
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartSvgOptions invalidPrefix = new ChartSvgOptions();
        ChartExport export = new ChartExport();

        openGroup.BeginGroup("series");
        mismatched.BeginGroup("series");
        mismatched.PopClip();
        invalidPrefix.IdPrefix = null!;

        Assert.Throws<System.InvalidOperationException>(() => openGroup.ToSvgString());
        Assert.Throws<System.InvalidOperationException>(() => mismatched.ToSvgString());
        Assert.Throws<System.ArgumentNullException>(() => export.ToSvgWithOptions(new ChartRenderScene(10, 10), accessibility, null!));
        Assert.Throws<System.ArgumentNullException>(() => export.ToSvgWithOptions(new ChartRenderScene(10, 10), accessibility, invalidPrefix));
    }

    static int CountOccurrences(string value, string search)
    {
        int count = 0;
        int offset = 0;
        while (offset < value.Length)
        {
            int found = value.IndexOf(search, offset, System.StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count = count + 1;
            offset = found + search.Length;
        }

        return count;
    }

    static void AssertUniqueIds(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        System.Xml.XmlNodeList nodes = document.SelectNodes("//*[@id]")!;
        System.Collections.Generic.HashSet<string> ids = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Count)
        {
            string id = nodes[index]!.Attributes!["id"]!.Value;
            Assert.That(ids.Add(id), Is.True, "duplicate SVG id " + id);
            index = index + 1;
        }
    }

    static void AssertValidXml(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        Assert.That(document.DocumentElement!.Name, Is.EqualTo("svg"));
    }
}
