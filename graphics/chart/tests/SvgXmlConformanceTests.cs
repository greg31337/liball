namespace chart.Tests;

[TestFixture]
public class SvgXmlConformanceTests
{
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    [Test]
    public void Svg_EscapesTextAttributesAndMetadataIntoWellFormedNamespacedXml()
    {
        string id = "series<&\"'";
        string special = "A&B <C> \"quoted\" 'single' Ω";
        ChartRenderScene scene = new ChartRenderScene(40, 20);
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();

        scene.AddText(id, special, 2.0, 10.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        accessibility.SetTitleDescription(special, special);
        accessibility.AddSeriesDescription(id, special);

        string svg = new ChartExport().ToSvg(scene, accessibility);
        System.Xml.XmlDocument document = ParseConformingXml(svg);
        System.Xml.XmlElement root = document.DocumentElement!;
        System.Xml.XmlElement text = ElementByLocalName(document, "text", 0);

        Assert.That(svg, Does.Contain("A&amp;B &lt;C&gt;"));
        Assert.That(svg, Does.Contain("&quot;quoted&quot;"));
        Assert.That(svg, Does.Contain("&apos;single&apos;"));
        Assert.That(root.NamespaceURI, Is.EqualTo(SvgNamespace));
        Assert.That(root.GetAttribute("aria-label"), Is.EqualTo(special));
        Assert.That(text.InnerText, Is.EqualTo(special));
        Assert.That(text.GetAttribute("aria-label"), Is.EqualTo(special));
        Assert.That(text.GetAttribute("data-series-id"), Is.EqualTo(id));
        Assert.That(ElementByLocalName(document, "title", 0).InnerText, Is.EqualTo(special));
        Assert.That(ElementByLocalName(document, "desc", 0).InnerText, Is.EqualTo(special));
        Assert.That(ElementByLocalName(document, "metadata", 0).InnerText, Does.Contain(id).And.Contain(special));
        AssertEveryElementUsesSvgNamespace(document);
    }

    [Test]
    public void Svg_GeometryAndPathsUseSpecifiedElementsCoordinatesAndPathCommands()
    {
        ChartRenderScene scene = new ChartRenderScene(120, 80);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(10, 20, 30, 255));
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(new ChartColor(40, 50, 60, 255), 1.5);
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 30.0);
        path.LineTo(5.0, 34.0);
        path.QuadraticBezierTo(8.0, 36.0, 10.0, 32.0);
        path.CubicBezierTo(12.0, 28.0, 14.0, 36.0, 18.0, 32.0);
        path.Close();
        scene.AddLine("line", 1.0, 2.0, 3.0, 4.0, stroke);
        scene.AddPolyline("polyline", new ChartPoint[] { new ChartPoint(5.0, 2.0), new ChartPoint(7.5, 4.0), new ChartPoint(9.0, 2.0) }, stroke);
        scene.AddPolygon("polygon", new ChartPoint[] { new ChartPoint(11.0, 2.0), new ChartPoint(17.0, 2.0), new ChartPoint(14.0, 7.0) }, fill);
        scene.AddRect("rect", 20.0, 2.0, 8.0, 6.0, 1.0, 2.0, fill);
        scene.AddCircle("circle", 34.0, 5.0, 3.0, fill);
        scene.AddEllipse("ellipse", 44.0, 5.0, 4.0, 2.0, fill);
        scene.AddPath("path", path, fill);
        scene.AddArc("arc", 28.0, 32.0, 5.0, 0.0, System.Math.PI * 0.5, stroke);
        scene.AddWedge("wedge", 44.0, 32.0, 2.0, 6.0, 0.0, System.Math.PI * 0.5, fill);
        scene.AddMarker("marker", ChartMarkerShape.Diamond, 60.0, 6.0, 6.0, fill);
        scene.AddText("text", "point", 70.0, 8.0, fill);

        System.Xml.XmlDocument document = ParseConformingXml(scene.ToSvgString());

        AssertElement(ElementById(document, "line"), "line", new string[] { "x1", "1", "y1", "2", "x2", "3", "y2", "4" });
        AssertElement(ElementById(document, "polyline"), "polyline", new string[] { "points", "5,2 7.5,4 9,2" });
        AssertElement(ElementById(document, "polygon"), "polygon", new string[] { "points", "11,2 17,2 14,7" });
        AssertElement(ElementById(document, "rect"), "rect", new string[] { "x", "20", "y", "2", "width", "8", "height", "6", "rx", "1", "ry", "2" });
        AssertElement(ElementById(document, "circle"), "circle", new string[] { "cx", "34", "cy", "5", "r", "3" });
        AssertElement(ElementById(document, "ellipse"), "ellipse", new string[] { "cx", "44", "cy", "5", "rx", "4", "ry", "2" });
        Assert.That(ElementById(document, "path").GetAttribute("d"), Is.EqualTo("M 1 30 L 5 34 Q 8 36 10 32 C 12 28 14 36 18 32 Z"));
        Assert.That(ElementById(document, "arc").LocalName, Is.EqualTo("path"));
        Assert.That(ElementById(document, "arc").GetAttribute("d"), Does.StartWith("M 33 32 L ").And.Not.EndWith("Z"));
        Assert.That(ElementById(document, "wedge").GetAttribute("d"), Does.StartWith("M 50 32 L ").And.EndWith("Z"));
        Assert.That(ElementById(document, "marker").LocalName, Is.EqualTo("polygon"));
        Assert.That(ElementById(document, "text").InnerText, Is.EqualTo("point"));
    }

    [Test]
    public void Svg_PlainMultilineRichAndRightToLeftTextRemainStructuredXmlText()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 60);
        ChartRenderStyle black = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartTextBlock multiline = new ChartTextBlock();
        ChartTextBlock rich = new ChartTextBlock();

        multiline.SetText("A\nB");
        multiline.SetRotation(15.0);
        rich.AddRichSpan(new ChartTextSpan("AB", ChartRenderStyle.CreateFill(new ChartColor(200, 0, 0, 255))));
        rich.AddRichSpan(new ChartTextSpan("CD", ChartRenderStyle.CreateFill(new ChartColor(0, 0, 200, 255))));
        rich.SetDirection(ChartTextDirection.RightToLeft);
        scene.AddText("plain", "A&B", 2.0, 8.0, black);
        scene.AddTextBlock("multiline", multiline, 20.0, 8.0);
        scene.AddTextBlock("rich", rich, 50.0, 8.0);

        System.Xml.XmlDocument document = ParseConformingXml(scene.ToSvgString());
        System.Xml.XmlElement plain = ElementById(document, "plain");
        System.Xml.XmlElement firstLine = ElementById(document, "multiline-line-0");
        System.Xml.XmlElement secondLine = ElementById(document, "multiline-line-1");
        System.Xml.XmlElement richText = ElementById(document, "rich");
        System.Xml.XmlNodeList spans = richText.GetElementsByTagName("tspan", SvgNamespace);

        Assert.That(plain.InnerText, Is.EqualTo("A&B"));
        Assert.That(firstLine.InnerText, Is.EqualTo("A"));
        Assert.That(secondLine.InnerText, Is.EqualTo("B"));
        Assert.That(firstLine.GetAttribute("transform"), Is.EqualTo("rotate(15 20 8)"));
        Assert.That(spans.Count, Is.EqualTo(2));
        Assert.That(spans[0]!.InnerText, Is.EqualTo("DC"));
        Assert.That(spans[1]!.InnerText, Is.EqualTo("BA"));
        Assert.That(richText.InnerText, Is.EqualTo("DCBA"));
    }

    [Test]
    public void Svg_GroupsTransformsAndClipsPreserveNestingAndResolvableReferences()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 30);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());

        scene.BeginGroup("series");
        scene.PushTransform("move", new ChartMatrix(1.0, 0.0, 0.0, 1.0, 5.0, 7.0));
        scene.PushClip("plot clip", new ChartRect(1.0, 2.0, 10.0, 8.0));
        scene.AddRect("mark", 0.0, 0.0, 20.0, 20.0, 0.0, 0.0, fill);
        scene.PopClip();
        scene.PopTransform();
        scene.EndGroup();

        System.Xml.XmlDocument document = ParseConformingXml(scene.ToSvgString());
        System.Xml.XmlElement series = ElementById(document, "series");
        System.Xml.XmlElement transform = ElementById(document, "move");
        System.Xml.XmlElement clipGroup = ElementById(document, "plot-clip");
        System.Xml.XmlElement clipPath = ElementById(document, "plot-clip-clip");
        System.Xml.XmlElement mark = ElementById(document, "mark");

        Assert.That(transform.ParentNode, Is.SameAs(series));
        Assert.That(clipGroup.ParentNode, Is.SameAs(transform));
        Assert.That(clipPath.ParentNode, Is.SameAs(transform));
        Assert.That(mark.ParentNode, Is.SameAs(clipGroup));
        Assert.That(transform.GetAttribute("transform"), Is.EqualTo("matrix(1 0 0 1 5 7)"));
        Assert.That(clipGroup.GetAttribute("style"), Does.Contain("clip-path:url(#plot-clip-clip)"));
        Assert.That(clipPath.LocalName, Is.EqualTo("clipPath"));
        AssertElement((System.Xml.XmlElement)clipPath.FirstChild!, "rect", new string[] { "x", "1", "y", "2", "width", "10", "height", "8" });
        AssertAllUrlReferencesResolve(document);
    }

    [Test]
    public void Svg_GradientAndPatternPaintServersResolveForFillsAndStrokes()
    {
        ChartRenderScene scene = new ChartRenderScene(50, 30);
        ChartRenderStyle gradient = new ChartRenderStyle();
        ChartRenderStyle pattern = new ChartRenderStyle();
        RawBitmap tile = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);

        tile.SetPixel(0, 0, new ChartColor(255, 0, 0, 128));
        tile.SetPixel(1, 0, new ChartColor(0, 0, 255, 255));
        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(10.0, 0.0),
            new ChartColor[] { new ChartColor(10, 20, 30, 64), new ChartColor(40, 50, 60, 255) },
            new double[] { 0.0, 1.0 });
        gradient.SetStrokeGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(0.0, 10.0),
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 },
            2.0);
        pattern.SetFillPattern(tile);
        pattern.SetStrokePattern(tile, 3.0);
        scene.AddRect("gradient", 1.0, 1.0, 10.0, 8.0, 0.0, 0.0, gradient);
        scene.AddRect("pattern", 15.0, 1.0, 10.0, 8.0, 0.0, 0.0, pattern);

        System.Xml.XmlDocument document = ParseConformingXml(scene.ToSvgString());
        System.Xml.XmlElement gradientRect = ElementById(document, "gradient");
        System.Xml.XmlElement patternRect = ElementById(document, "pattern");
        System.Xml.XmlElement fillGradient = ElementById(document, "gradient-fill-gradient");
        System.Xml.XmlElement strokeGradient = ElementById(document, "gradient-stroke-gradient");
        System.Xml.XmlElement fillPattern = ElementById(document, "pattern-fill-pattern");
        System.Xml.XmlElement strokePattern = ElementById(document, "pattern-stroke-pattern");

        Assert.That(gradientRect.GetAttribute("style"), Does.Contain("fill:url(#gradient-fill-gradient)").And.Contain("stroke:url(#gradient-stroke-gradient)"));
        Assert.That(patternRect.GetAttribute("style"), Does.Contain("fill:url(#pattern-fill-pattern)").And.Contain("stroke:url(#pattern-stroke-pattern)"));
        AssertElement(fillGradient, "linearGradient", new string[] { "x1", "0", "y1", "0", "x2", "10", "y2", "0" });
        AssertElement(strokeGradient, "linearGradient", new string[] { "x1", "0", "y1", "0", "x2", "0", "y2", "10" });
        Assert.That(fillGradient.GetElementsByTagName("stop", SvgNamespace).Count, Is.EqualTo(2));
        Assert.That(((System.Xml.XmlElement)fillGradient.FirstChild!).GetAttribute("stop-opacity"), Is.EqualTo("0.251"));
        AssertElement(fillPattern, "pattern", new string[] { "patternUnits", "userSpaceOnUse", "width", "2", "height", "1" });
        AssertElement(strokePattern, "pattern", new string[] { "patternUnits", "userSpaceOnUse", "width", "2", "height", "1" });
        Assert.That(fillPattern.GetElementsByTagName("image", SvgNamespace).Count, Is.EqualTo(1));
        Assert.That(strokePattern.GetElementsByTagName("image", SvgNamespace).Count, Is.EqualTo(1));
        AssertAllUrlReferencesResolve(document);
    }

    [Test]
    public void Svg_EmbeddedRawImageIsACompleteDecodablePngDataUrl()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new ChartColor(50, 60, 70, 80));
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetOpacity(0.5);
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        scene.AddImage("image", bitmap, 2.0, 3.0, 8.0, 4.0, style);

        System.Xml.XmlDocument document = ParseConformingXml(scene.ToSvgString());
        System.Xml.XmlElement image = ElementById(document, "image");
        string href = image.GetAttribute("href");
        byte[] png = System.Convert.FromBase64String(href.Substring("data:image/png;base64,".Length));
        ReferenceDecodedImage decoded = ReferencePngDecoder.Decode(png);

        Assert.That(href, Does.StartWith("data:image/png;base64,"));
        AssertElement(image, "image", new string[] { "x", "2", "y", "3", "width", "8", "height", "4", "opacity", "0.5" });
        Assert.That(decoded.Width, Is.EqualTo(2));
        Assert.That(decoded.Height, Is.EqualTo(1));
        AssertColor(decoded.GetPixel(0, 0), 10, 20, 30, 40);
        AssertColor(decoded.GetPixel(1, 0), 50, 60, 70, 80);
    }

    [Test]
    public void Svg_IdentifiersAndReferencesAreUniqueDeterministicAndStable()
    {
        ChartRenderScene scene = new ChartRenderScene(30, 20);
        ChartRenderStyle gradient = new ChartRenderStyle();
        gradient.SetFillGradient(
            new ChartPoint(0.0, 0.0),
            new ChartPoint(10.0, 0.0),
            new ChartColor[] { ChartColor.Black(), ChartColor.White() },
            new double[] { 0.0, 1.0 });
        scene.PushClip("same id", new ChartRect(0.0, 0.0, 20.0, 20.0));
        scene.AddRect("same id", 1.0, 1.0, 5.0, 5.0, 0.0, 0.0, gradient);
        scene.AddRect("same id", 8.0, 1.0, 5.0, 5.0, 0.0, 0.0, gradient);
        scene.PopClip();
        ChartSvgOptions options = new ChartSvgOptions();
        options.IdPrefix = "7 chart/id";
        ChartExport export = new ChartExport();

        string first = export.ToSvgWithOptions(scene, new ChartAccessibilityMetadata(), options);
        string second = export.ToSvgWithOptions(scene, new ChartAccessibilityMetadata(), options);
        System.Xml.XmlDocument document = ParseConformingXml(first);

        Assert.That(second, Is.EqualTo(first));
        AssertUniqueXmlIds(document);
        AssertAllUrlReferencesResolve(document);
        Assert.That(ElementById(document, "id-7-chart-id-same-id").LocalName, Is.EqualTo("g"));
        Assert.That(ElementById(document, "id-7-chart-id-same-id-2").LocalName, Is.EqualTo("rect"));
        Assert.That(ElementById(document, "id-7-chart-id-same-id-3").LocalName, Is.EqualTo("rect"));
    }

    [Test]
    public void Svg_AccessibilityMetadataCreatesSemanticRootSeriesAndPointStructure()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 20);
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();

        scene.BeginGroup("sales");
        scene.AddMarker("sales-q1", ChartMarkerShape.Diamond, 8.0, 8.0, 5.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        scene.EndGroup();
        accessibility.SetTitleDescription("Sales overview", "Quarterly sales.");
        ChartSeriesAccessibility series = accessibility.AddSeriesDescription("sales", "Sales series.");
        series.SetNonColorCues(ChartMarkerShape.Diamond, new double[] { 4.0, 2.0 }, "Q1", "diagonal hatch");
        series.AddPointDescription("sales-q1", "First quarter.", 0);

        System.Xml.XmlDocument document = ParseConformingXml(new ChartExport().ToSvg(scene, accessibility));
        System.Xml.XmlElement root = document.DocumentElement!;
        System.Xml.XmlElement group = ElementById(document, "sales");
        System.Xml.XmlElement point = ElementById(document, "sales-q1");

        Assert.That(root.GetAttribute("role"), Is.EqualTo("img"));
        Assert.That(root.GetAttribute("aria-label"), Is.EqualTo("Sales overview"));
        Assert.That(ElementByLocalName(document, "title", 0).InnerText, Is.EqualTo("Sales overview"));
        Assert.That(ElementByLocalName(document, "desc", 0).InnerText, Is.EqualTo("Quarterly sales."));
        Assert.That(ElementByLocalName(document, "metadata", 0).InnerText, Does.Contain("series sales: Sales series.").And.Contain("point sales/sales-q1 row 0: First quarter."));
        Assert.That(group.GetAttribute("role"), Is.EqualTo("group"));
        Assert.That(group.GetAttribute("data-series-id"), Is.EqualTo("sales"));
        Assert.That(group.GetAttribute("data-marker-shape"), Is.EqualTo("Diamond"));
        Assert.That(group.GetAttribute("data-dash-pattern"), Is.EqualTo("4,2"));
        Assert.That(point.GetAttribute("role"), Is.EqualTo("img"));
        Assert.That(point.GetAttribute("aria-label"), Does.StartWith("First quarter."));
        Assert.That(point.GetAttribute("data-point-id"), Is.EqualTo("sales-q1"));
        Assert.That(point.GetAttribute("data-row-index"), Is.EqualTo("0"));
    }

    [Test]
    public void Svg_DefaultOutputIsCssClassFreeAndOptionalCssClassesDeduplicateStyles()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(20, 40, 60, 255));
        scene.AddRect("one", 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, fill);
        scene.AddCircle("two", 8.0, 2.0, 2.0, fill);
        ChartExport export = new ChartExport();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartSvgOptions cssOptions = new ChartSvgOptions();
        cssOptions.UseCssClasses = true;

        System.Xml.XmlDocument inlineDocument = ParseConformingXml(export.ToSvg(scene, accessibility));
        System.Xml.XmlDocument cssDocument = ParseConformingXml(export.ToSvgWithOptions(scene, accessibility, cssOptions));
        System.Xml.XmlElement cssRect = ElementById(cssDocument, "one");
        System.Xml.XmlElement cssCircle = ElementById(cssDocument, "two");
        System.Xml.XmlElement style = ElementByLocalName(cssDocument, "style", 0);

        Assert.That(inlineDocument.GetElementsByTagName("style", SvgNamespace).Count, Is.EqualTo(0));
        Assert.That(inlineDocument.SelectNodes("//*[@class]")!.Count, Is.EqualTo(0));
        Assert.That(inlineDocument.SelectNodes("//*[@style]")!.Count, Is.EqualTo(2));
        Assert.That(cssDocument.SelectNodes("//*[@style]")!.Count, Is.EqualTo(0));
        Assert.That(cssRect.GetAttribute("class"), Is.EqualTo("chart-rect chart-style-1"));
        Assert.That(cssCircle.GetAttribute("class"), Is.EqualTo("chart-circle chart-style-1"));
        Assert.That(style.InnerText, Is.EqualTo(".chart-style-1{fill:#14283C;font-family:Arial;font-size:12}"));
        Assert.That(CountOccurrences(style.InnerText, ".chart-style-1{"), Is.EqualTo(1));
    }

    [Test]
    public void Svg_BytesStreamsAndFilesAreBomlessUtf8AndPreserveCallerStreams()
    {
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        scene.AddText("unicode", "Résumé Ω", 1.0, 8.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        string svg = scene.ToSvgString();
        ChartExport export = new ChartExport();
        byte[] bytes = export.ToSvgBytes(svg);
        System.IO.MemoryStream created = export.ToSvgStream(svg);
        System.IO.MemoryStream destination = new System.IO.MemoryStream();
        destination.Write(new byte[] { 1, 2, 3 }, 0, 3);
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "svg-xml-conformance.svg");

        export.WriteSvg(svg, destination);
        export.SaveSvg(svg, path);

        Assert.That(bytes, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(svg)));
        Assert.That(bytes.Length < 3 || bytes[0] != 239 || bytes[1] != 187 || bytes[2] != 191, Is.True);
        Assert.That(created.Position, Is.EqualTo(0));
        Assert.That(ReadAllUtf8(created), Is.EqualTo(svg));
        Assert.That(destination.CanWrite, Is.True);
        Assert.That(CopyRange(destination.ToArray(), 0, 3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(System.Text.Encoding.UTF8.GetString(destination.ToArray(), 3, (int)destination.Length - 3), Is.EqualTo(svg));
        Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(bytes));
        Assert.DoesNotThrow(() => ParseConformingXml(System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8)));
        destination.WriteByte(4);
    }

    private static System.Xml.XmlDocument ParseConformingXml(string svg)
    {
        System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
        settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        using System.IO.StringReader text = new System.IO.StringReader(svg);
        using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(text, settings);
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.XmlResolver = null;
        document.Load(reader);
        Assert.That(document.DocumentElement, Is.Not.Null);
        Assert.That(document.DocumentElement!.LocalName, Is.EqualTo("svg"));
        Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo(SvgNamespace));
        return document;
    }

    private static System.Xml.XmlElement ElementById(System.Xml.XmlDocument document, string id)
    {
        System.Xml.XmlNode? node = document.SelectSingleNode("//*[@id=" + XPathLiteral(id) + "]");
        Assert.That(node, Is.Not.Null, "missing SVG id " + id);
        return (System.Xml.XmlElement)node!;
    }

    private static System.Xml.XmlElement ElementByLocalName(System.Xml.XmlDocument document, string localName, int index)
    {
        System.Xml.XmlNodeList nodes = document.GetElementsByTagName(localName, SvgNamespace);
        Assert.That(nodes.Count, Is.GreaterThan(index), "missing SVG element " + localName);
        return (System.Xml.XmlElement)nodes[index]!;
    }

    private static void AssertElement(System.Xml.XmlElement element, string localName, string[] attributes)
    {
        Assert.That(element.LocalName, Is.EqualTo(localName));
        Assert.That(element.NamespaceURI, Is.EqualTo(SvgNamespace));
        int index = 0;
        while (index < attributes.Length)
        {
            Assert.That(element.GetAttribute(attributes[index]), Is.EqualTo(attributes[index + 1]), element.GetAttribute("id") + " attribute " + attributes[index]);
            index = index + 2;
        }
    }

    private static void AssertEveryElementUsesSvgNamespace(System.Xml.XmlDocument document)
    {
        System.Xml.XmlNodeList elements = document.SelectNodes("//*")!;
        int index = 0;
        while (index < elements.Count)
        {
            Assert.That(elements[index]!.NamespaceURI, Is.EqualTo(SvgNamespace), elements[index]!.Name);
            index++;
        }
    }

    private static void AssertUniqueXmlIds(System.Xml.XmlDocument document)
    {
        System.Collections.Generic.HashSet<string> ids = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Xml.XmlNodeList nodes = document.SelectNodes("//*[@id]")!;
        int index = 0;
        while (index < nodes.Count)
        {
            string id = nodes[index]!.Attributes!["id"]!.Value;
            Assert.That(ids.Add(id), Is.True, "duplicate SVG id " + id);
            index++;
        }
    }

    private static void AssertAllUrlReferencesResolve(System.Xml.XmlDocument document)
    {
        System.Xml.XmlNodeList nodes = document.SelectNodes("//*[@style]")!;
        int index = 0;
        while (index < nodes.Count)
        {
            string style = nodes[index]!.Attributes!["style"]!.Value;
            int search = 0;
            while (search < style.Length)
            {
                int start = style.IndexOf("url(#", search, System.StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                int end = style.IndexOf(')', start + 5);
                Assert.That(end, Is.GreaterThan(start));
                string id = style.Substring(start + 5, end - start - 5);
                Assert.That(document.SelectSingleNode("//*[@id=" + XPathLiteral(id) + "]"), Is.Not.Null, "missing SVG reference " + id);
                search = end + 1;
            }

            index++;
        }
    }

    private static string XPathLiteral(string value)
    {
        if (value.IndexOf('\'') < 0)
        {
            return "'" + value + "'";
        }

        return "\"" + value + "\"";
    }

    private static int CountOccurrences(string value, string search)
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

            count++;
            offset = found + search.Length;
        }

        return count;
    }

    private static string ReadAllUtf8(System.IO.Stream stream)
    {
        using System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, true);
        return reader.ReadToEnd();
    }

    private static byte[] CopyRange(byte[] source, int offset, int length)
    {
        byte[] copy = new byte[length];
        System.Array.Copy(source, offset, copy, 0, length);
        return copy;
    }

    private static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.R, Is.EqualTo(red));
        Assert.That(color.G, Is.EqualTo(green));
        Assert.That(color.B, Is.EqualTo(blue));
        Assert.That(color.A, Is.EqualTo(alpha));
    }
}
