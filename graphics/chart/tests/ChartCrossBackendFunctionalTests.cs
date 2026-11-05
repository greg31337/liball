namespace chart.Tests;

[TestFixture]
public class ChartCrossBackendFunctionalTests
{
    [Test]
    public void PrimitiveScene_PreservesGeometryAndSolidStylesAcrossRasterAndSvg()
    {
        ChartRenderScene scene = new ChartRenderScene(48, 32);
        ChartRenderStyle rectangleStyle = ChartRenderStyle.CreateFillStroke(
            new ChartColor(210, 40, 50, 255),
            new ChartColor(20, 40, 180, 255),
            2.0);
        ChartRenderStyle circleStyle = ChartRenderStyle.CreateFill(new ChartColor(20, 170, 80, 255));
        ChartRenderStyle polygonStyle = ChartRenderStyle.CreateFill(new ChartColor(130, 50, 190, 255));
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(new ChartColor(15, 25, 35, 255), 2.0);

        scene.AddClear(ChartColor.White());
        scene.AddRect("solid-rect", 4.0, 4.0, 14.0, 10.0, 0.0, 0.0, rectangleStyle);
        scene.AddCircle("solid-circle", 28.0, 9.0, 5.0, circleStyle);
        scene.AddPolygon(
            "solid-polygon",
            new ChartPoint[] { new ChartPoint(36.0, 4.0), new ChartPoint(45.0, 4.0), new ChartPoint(40.0, 14.0) },
            polygonStyle);
        scene.AddLine("solid-line", 5.0, 24.0, 42.0, 24.0, lineStyle);

        RawBitmap raster = scene.ToRawBitmap();
        System.Xml.XmlDocument svg = ParseSvg(scene.ToSvgString());
        System.Xml.XmlElement rectangle = ElementById(svg, "solid-rect");
        System.Xml.XmlElement circle = ElementById(svg, "solid-circle");
        System.Xml.XmlElement polygon = ElementById(svg, "solid-polygon");
        System.Xml.XmlElement line = ElementById(svg, "solid-line");

        AssertGeometry(rectangle, "rect", "x", 4.0, "y", 4.0, "width", 14.0, "height", 10.0);
        AssertGeometry(circle, "circle", "cx", 28.0, "cy", 9.0, "r", 5.0, "r", 5.0);
        Assert.That(polygon.LocalName, Is.EqualTo("polygon"));
        Assert.That(polygon.GetAttribute("points"), Is.EqualTo("36,4 45,4 40,14"));
        AssertGeometry(line, "line", "x1", 5.0, "y1", 24.0, "x2", 42.0, "y2", 24.0);
        Assert.That(rectangle.GetAttribute("style"), Does.Contain("fill:#D22832"));
        Assert.That(rectangle.GetAttribute("style"), Does.Contain("stroke:#1428B4"));
        Assert.That(rectangle.GetAttribute("style"), Does.Contain("stroke-width:2"));
        Assert.That(circle.GetAttribute("style"), Does.Contain("fill:#14AA50"));
        Assert.That(polygon.GetAttribute("style"), Does.Contain("fill:#8232BE"));
        Assert.That(line.GetAttribute("style"), Does.Contain("stroke:#0F1923"));

        AssertColor(raster.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(raster.GetPixel(11, 9), 210, 40, 50, 255);
        AssertColor(raster.GetPixel(28, 9), 20, 170, 80, 255);
        AssertColor(raster.GetPixel(40, 8), 130, 50, 190, 255);
        AssertColor(raster.GetPixel(20, 24), 15, 25, 35, 255);
    }

    [Test]
    public void StyledScene_AppliesTransformClipOpacityAndGradientAcrossBackends()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 20);
        ChartRenderStyle blue = ChartRenderStyle.CreateFill(new ChartColor(30, 90, 220, 255));
        ChartRenderStyle translucentRed = ChartRenderStyle.CreateFill(new ChartColor(240, 20, 20, 255));
        ChartRenderStyle gradient = new ChartRenderStyle();

        translucentRed.SetOpacity(0.5);
        gradient.SetFillGradient(
            new ChartPoint(20.0, 0.0),
            new ChartPoint(36.0, 0.0),
            new ChartColor[] { new ChartColor(255, 0, 0, 255), new ChartColor(0, 0, 255, 255) },
            new double[] { 0.0, 1.0 });

        scene.AddClear(ChartColor.White());
        scene.PushTransform("translation", new ChartMatrix(1.0, 0.0, 0.0, 1.0, 4.0, 2.0));
        scene.PushClip("local-clip", new ChartRect(0.0, 0.0, 10.0, 8.0));
        scene.AddRect("clipped-blue", 0.0, 0.0, 18.0, 8.0, 0.0, 0.0, blue);
        scene.PopClip();
        scene.PopTransform();
        scene.AddRect("translucent-red", 20.0, 2.0, 8.0, 8.0, 0.0, 0.0, translucentRed);
        scene.AddRect("gradient", 20.0, 13.0, 16.0, 4.0, 0.0, 0.0, gradient);

        RawBitmap raster = scene.ToRawBitmap();
        System.Xml.XmlDocument svg = ParseSvg(scene.ToSvgString());
        System.Xml.XmlElement transform = ElementById(svg, "translation");
        System.Xml.XmlElement clip = ElementById(svg, "local-clip-clip");
        System.Xml.XmlElement clippedRectangle = ElementById(svg, "clipped-blue");
        System.Xml.XmlElement opacityRectangle = ElementById(svg, "translucent-red");
        System.Xml.XmlElement gradientRectangle = ElementById(svg, "gradient");
        System.Xml.XmlElement gradientDefinition = ElementById(svg, "gradient-fill-gradient");

        Assert.That(transform.LocalName, Is.EqualTo("g"));
        Assert.That(transform.GetAttribute("transform"), Is.EqualTo("matrix(1 0 0 1 4 2)"));
        Assert.That(clip.LocalName, Is.EqualTo("clipPath"));
        Assert.That(clip.InnerXml, Does.Contain("width=\"10\""));
        Assert.That(clippedRectangle.GetAttribute("style"), Does.Contain("fill:#1E5ADC"));
        Assert.That(opacityRectangle.GetAttribute("style"), Does.Contain("opacity:0.5"));
        Assert.That(gradientRectangle.GetAttribute("style"), Does.Contain("fill:url(#gradient-fill-gradient)"));
        Assert.That(gradientDefinition.GetAttribute("x1"), Is.EqualTo("20"));
        Assert.That(gradientDefinition.GetAttribute("x2"), Is.EqualTo("36"));

        AssertColor(raster.GetPixel(5, 3), 30, 90, 220, 255);
        AssertColor(raster.GetPixel(13, 3), 30, 90, 220, 255);
        AssertColor(raster.GetPixel(15, 3), 255, 255, 255, 255);
        AssertColor(raster.GetPixel(23, 5), 247, 137, 137, 255);
        Assert.That(raster.GetPixel(21, 14).GetR(), Is.GreaterThan(raster.GetPixel(21, 14).GetB()));
        Assert.That(raster.GetPixel(34, 14).GetB(), Is.GreaterThan(raster.GetPixel(34, 14).GetR()));
    }

    [Test]
    public void DeclarativeBarChart_MapsTheSameRetainedGeometryToRasterAndSvg()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, 3.0 });
        table.AddNumberColumn("y", new double[] { 4.0, 2.0 });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetConstantNumber(ChartEncodingChannel.Size, 1.0);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(new ChartColor(230, 120, 20, 255));
        ChartCartesianSeries bars = new ChartDeclarativeCharts().CreateVerticalBarSeries("bars", table, encoding, style);
        ChartCartesianTransform transform = new ChartCartesianTransform(
            new ChartRect(0.0, 0.0, 40.0, 30.0),
            new ChartRange(0.0, 4.0),
            new ChartRange(0.0, 5.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartRenderScene scene = new ChartRenderScene(40, 30);

        scene.AddClear(ChartColor.White());
        renderer.AddSeries(bars);
        renderer.RenderToScene(scene);

        ChartRenderCommand firstBar = CommandById(scene, "bars-bar-0");
        RawBitmap raster = scene.ToRawBitmap();
        System.Xml.XmlElement svgBar = ElementById(ParseSvg(scene.ToSvgString()), "bars-bar-0");
        ChartPoint[] retainedPoints = firstBar.GetPoints();
        ChartPoint[] svgPoints = ParsePoints(svgBar.GetAttribute("points"));
        int centerX = (int)System.Math.Floor(AverageX(retainedPoints));
        int centerY = (int)System.Math.Floor(AverageY(retainedPoints));

        Assert.That(firstBar.GetKind(), Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(svgBar.LocalName, Is.EqualTo("polygon"));
        Assert.That(svgPoints.Length, Is.EqualTo(retainedPoints.Length));
        int pointIndex = 0;
        while (pointIndex < retainedPoints.Length)
        {
            Assert.That(svgPoints[pointIndex].GetX(), Is.EqualTo(retainedPoints[pointIndex].GetX()).Within(0.0000001));
            Assert.That(svgPoints[pointIndex].GetY(), Is.EqualTo(retainedPoints[pointIndex].GetY()).Within(0.0000001));
            pointIndex = pointIndex + 1;
        }

        Assert.That(svgBar.GetAttribute("style"), Does.Contain("fill:#E67814"));
        AssertColor(raster.GetPixel(centerX, centerY), 230, 120, 20, 255);
    }

    [Test]
    public void FixedScene_ProducesDeterministicRasterAndSvgOnRepeatedReplay()
    {
        ChartRenderScene firstScene = DeterministicScene();
        string firstSvg = firstScene.ToSvgString();
        byte[] firstRaster = firstScene.ToRawBitmap().CopyBuffer();
        string repeatedSvg = firstScene.ToSvgString();
        byte[] repeatedRaster = firstScene.ToRawBitmap().CopyBuffer();
        ChartRenderScene equivalentScene = DeterministicScene();
        string equivalentSvg = equivalentScene.ToSvgString();
        byte[] equivalentRaster = equivalentScene.ToRawBitmap().CopyBuffer();

        Assert.That(repeatedSvg, Is.EqualTo(firstSvg));
        Assert.That(equivalentSvg, Is.EqualTo(firstSvg));
        Assert.That(repeatedRaster, Is.EqualTo(firstRaster));
        Assert.That(equivalentRaster, Is.EqualTo(firstRaster));
        Assert.That(firstScene.GetCommandCount(), Is.EqualTo(equivalentScene.GetCommandCount()));
    }

    static ChartRenderScene DeterministicScene()
    {
        ChartRenderScene scene = new ChartRenderScene(24, 18);
        ChartRenderStyle gradient = new ChartRenderStyle();
        gradient.SetFillGradient(
            new ChartPoint(2.0, 0.0),
            new ChartPoint(14.0, 0.0),
            new ChartColor[] { new ChartColor(10, 30, 200, 255), new ChartColor(20, 200, 80, 255) },
            new double[] { 0.0, 1.0 });

        scene.AddClear(new ChartColor(245, 245, 245, 255));
        scene.BeginGroup("fixed-group");
        scene.AddRect("fixed-gradient", 2.0, 2.0, 12.0, 8.0, 1.0, 1.0, gradient);
        scene.AddCircle("fixed-circle", 18.0, 6.0, 3.0, ChartRenderStyle.CreateFill(new ChartColor(200, 40, 60, 255)));
        scene.AddLine("fixed-line", 3.0, 14.0, 21.0, 14.0, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        scene.EndGroup();
        return scene;
    }

    static System.Xml.XmlDocument ParseSvg(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        return document;
    }

    static System.Xml.XmlElement ElementById(System.Xml.XmlDocument document, string id)
    {
        System.Xml.XmlNode? node = document.SelectSingleNode("//*[@id='" + id + "']");
        Assert.That(node, Is.Not.Null, "SVG element was not found: " + id);
        return (System.Xml.XmlElement)node!;
    }

    static ChartRenderCommand CommandById(ChartRenderScene scene, string id)
    {
        int index = 0;
        while (index < scene.GetCommandCount())
        {
            ChartRenderCommand command = scene.CommandAt(index);
            if (command.GetId() == id)
            {
                return command;
            }

            index = index + 1;
        }

        throw new AssertionException("Render command was not found: " + id);
    }

    static void AssertGeometry(
        System.Xml.XmlElement element,
        string elementName,
        string firstName,
        double firstValue,
        string secondName,
        double secondValue,
        string thirdName,
        double thirdValue,
        string fourthName,
        double fourthValue)
    {
        Assert.That(element.LocalName, Is.EqualTo(elementName));
        Assert.That(ParseNumber(element.GetAttribute(firstName)), Is.EqualTo(firstValue).Within(0.0000001));
        Assert.That(ParseNumber(element.GetAttribute(secondName)), Is.EqualTo(secondValue).Within(0.0000001));
        Assert.That(ParseNumber(element.GetAttribute(thirdName)), Is.EqualTo(thirdValue).Within(0.0000001));
        Assert.That(ParseNumber(element.GetAttribute(fourthName)), Is.EqualTo(fourthValue).Within(0.0000001));
    }

    static double ParseNumber(string value)
    {
        return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    static ChartPoint[] ParsePoints(string value)
    {
        string[] pairs = value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        ChartPoint[] points = new ChartPoint[pairs.Length];
        int index = 0;
        while (index < pairs.Length)
        {
            string[] coordinates = pairs[index].Split(',');
            points[index] = new ChartPoint(ParseNumber(coordinates[0]), ParseNumber(coordinates[1]));
            index = index + 1;
        }

        return points;
    }

    static double AverageX(ChartPoint[] points)
    {
        double total = 0.0;
        int index = 0;
        while (index < points.Length)
        {
            total = total + points[index].GetX();
            index = index + 1;
        }

        return total / points.Length;
    }

    static double AverageY(ChartPoint[] points)
    {
        double total = 0.0;
        int index = 0;
        while (index < points.Length)
        {
            total = total + points[index].GetY();
            index = index + 1;
        }

        return total / points.Length;
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo((byte)red));
        Assert.That(color.GetG(), Is.EqualTo((byte)green));
        Assert.That(color.GetB(), Is.EqualTo((byte)blue));
        Assert.That(color.GetA(), Is.EqualTo((byte)alpha));
    }
}
