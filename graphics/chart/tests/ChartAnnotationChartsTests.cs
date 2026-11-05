namespace chart.Tests;

[TestFixture]
public class ChartAnnotationChartsTests
{
    [Test]
    public void CoordinateAnnotations_MapDataPlotPaperAndPixelUnits()
    {
        IChartAnnotationCharts annotations = new ChartAnnotationCharts();
        ChartRenderScene scene = new ChartRenderScene(200, 100);
        ChartConfiguration configuration = new ChartConfiguration();
        ChartRect plotArea = new ChartRect(10.0, 20.0, 100.0, 80.0);
        ChartRange domain = new ChartRange(0.0, 10.0);

        AssertPoint(annotations.MapCoordinatePoint(scene, new ChartPoint(5.0, 2.0), configuration, plotArea, domain, domain), 60.0, 84.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Plot);
        AssertPoint(annotations.MapCoordinatePoint(scene, new ChartPoint(3.0, 4.0), configuration, plotArea, domain, domain), 13.0, 24.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Paper);
        AssertPoint(annotations.MapCoordinatePoint(scene, new ChartPoint(0.25, 0.75), configuration, plotArea, domain, domain), 50.0, 75.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Pixel);
        AssertPoint(annotations.MapCoordinatePoint(scene, new ChartPoint(3.0, 4.0), configuration, plotArea, domain, domain), 3.0, 4.0);
    }

    [Test]
    public void ReferenceShapes_MapEveryCoordinateUnitAndNormalizeDataRectangles()
    {
        IChartAnnotationCharts annotations = new ChartAnnotationCharts();
        ChartRenderScene scene = new ChartRenderScene(200, 100);
        ChartConfiguration configuration = new ChartConfiguration();
        ChartRect plotArea = new ChartRect(10.0, 20.0, 100.0, 80.0);
        ChartRange domain = new ChartRange(0.0, 10.0);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        annotations.AddReferenceRectangle(scene, "data", new ChartRect(2.0, 2.0, 4.0, 4.0), configuration, plotArea, domain, domain, 0.0, style);
        configuration.SetCoordinateUnit(ChartCoordinateUnit.Plot);
        annotations.AddReferenceRectangle(scene, "plot", new ChartRect(2.0, 3.0, 4.0, 5.0), configuration, plotArea, domain, domain, 0.0, style);
        configuration.SetCoordinateUnit(ChartCoordinateUnit.Paper);
        annotations.AddReferenceRectangle(scene, "paper", new ChartRect(0.1, 0.2, 0.3, 0.4), configuration, plotArea, domain, domain, 0.0, style);
        configuration.SetCoordinateUnit(ChartCoordinateUnit.Pixel);
        annotations.AddReferenceRectangle(scene, "pixel", new ChartRect(5.0, 6.0, 7.0, 8.0), configuration, plotArea, domain, domain, 0.0, style);

        AssertRect(scene.CommandAt(0), 30.0, 52.0, 40.0, 32.0);
        AssertRect(scene.CommandAt(1), 12.0, 23.0, 4.0, 5.0);
        AssertRect(scene.CommandAt(2), 20.0, 20.0, 60.0, 40.0);
        AssertRect(scene.CommandAt(3), 5.0, 6.0, 7.0, 8.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Data);
        ChartRenderCommand ellipse = annotations.AddReferenceEllipse(scene, "ellipse-data", new ChartPoint(5.0, 5.0), 2.0, 1.0, configuration, plotArea, domain, domain, style);
        AssertPoint(ellipse, 60.0, 60.0);
        Assert.That(ellipse.RadiusX, Is.EqualTo(20.0).Within(0.0000001));
        Assert.That(ellipse.RadiusY, Is.EqualTo(8.0).Within(0.0000001));

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Paper);
        ChartRenderCommand polygon = annotations.AddReferencePolygon(scene, "polygon-paper", new ChartPoint[] { new ChartPoint(0.1, 0.1), new ChartPoint(0.2, 0.1), new ChartPoint(0.1, 0.3) }, configuration, plotArea, domain, domain, style);
        AssertPoint(polygon.Points[0], 20.0, 10.0);
        AssertPoint(polygon.Points[2], 20.0, 30.0);
    }

    [Test]
    public void CoordinateTextArrowAndBackgroundImage_AddMappedCommands()
    {
        IChartAnnotationCharts annotations = new ChartAnnotationCharts();
        ChartRenderScene scene = new ChartRenderScene(40, 30);
        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetCoordinateUnit(ChartCoordinateUnit.Paper);
        ChartRect plotArea = new ChartRect(5.0, 5.0, 30.0, 20.0);
        ChartRange domain = new ChartRange(0.0, 1.0);
        ChartRenderStyle textStyle = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        RawBitmap background = new RawBitmap(2, 2, ChartPixelFormat.Rgba32);

        ChartRenderCommand image = annotations.AddBackgroundImage(scene, "background", background, new ChartRenderStyle());
        annotations.AddCoordinateText(scene, "note", "Half", new ChartPoint(0.5, 0.5), configuration, plotArea, domain, domain, textStyle);
        annotations.AddCoordinateArrow(scene, "arrow", new ChartPoint(0.25, 0.5), new ChartPoint(0.75, 0.5), configuration, plotArea, domain, domain, 4.0, 4.0, lineStyle);

        Assert.That(image.Kind, Is.EqualTo(ChartRenderCommandKind.Image));
        AssertRect(image, 0.0, 0.0, 40.0, 30.0);
        AssertPoint(scene.CommandAt(1), 20.0, 15.0);
        AssertLine(scene.CommandAt(2), 10.0, 15.0, 30.0, 15.0);
        Assert.That(scene.CommandAt(3).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
    }

    [Test]
    public void TextShapesPathWatermarkCaptionAndCredits_AddSceneCommands()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 80);
        ChartRenderStyle textStyle = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle fillStyle = ChartRenderStyle.CreateFillStroke(ChartColor.FromRgb(220, 230, 240), ChartColor.Black(), 1.0);
        ChartPath path = new ChartPath();
        path.MoveTo(1.0, 1.0);
        path.LineTo(10.0, 1.0);
        path.LineTo(10.0, 6.0);
        path.Close();

        ChartAnnotationChartsOperations.AddText(scene, "note", "Hello", 10.0, 20.0, textStyle);
        ChartAnnotationChartsOperations.AddRectangleShape(scene, "rect", new ChartRect(1.0, 2.0, 30.0, 10.0), 2.0, fillStyle);
        ChartAnnotationChartsOperations.AddCircleShape(scene, "circle", 50.0, 40.0, 5.0, fillStyle);
        ChartAnnotationChartsOperations.AddEllipseShape(scene, "ellipse", 70.0, 40.0, 8.0, 4.0, fillStyle);
        ChartAnnotationChartsOperations.AddPolygonShape(scene, "polygon", new ChartPoint[] { new ChartPoint(2.0, 2.0), new ChartPoint(8.0, 2.0), new ChartPoint(5.0, 9.0) }, fillStyle);
        ChartAnnotationChartsOperations.AddPath(scene, "path", path, fillStyle);
        ChartAnnotationChartsOperations.AddWatermark(scene, "watermark", "Draft", textStyle);
        ChartAnnotationChartsOperations.AddCaption(scene, "caption", "Source: sample", 4.0, textStyle);
        ChartAnnotationChartsOperations.AddCredits(scene, "credits", "Credits", 4.0, textStyle);

        Assert.That(scene.CommandCount, Is.EqualTo(9));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("note"));
        Assert.That(scene.CommandAt(0).Text, Is.EqualTo("Hello"));
        AssertPoint(scene.CommandAt(0), 10.0, 20.0);
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(scene.CommandAt(1).RadiusX, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(scene.CommandAt(2).Kind, Is.EqualTo(ChartRenderCommandKind.Circle));
        Assert.That(scene.CommandAt(3).Kind, Is.EqualTo(ChartRenderCommandKind.Ellipse));
        Assert.That(scene.CommandAt(4).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(scene.CommandAt(4).Points.Length, Is.EqualTo(3));
        Assert.That(scene.CommandAt(5).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(scene.CommandAt(5).Path.Commands.Count, Is.EqualTo(4));
        AssertPoint(scene.CommandAt(6), 50.0, 40.0);
        AssertPoint(scene.CommandAt(7), 4.0, 76.0);
        AssertPoint(scene.CommandAt(8), 96.0, 76.0);
    }

    [Test]
    public void ArrowAndCallout_AddExpectedGeometry()
    {
        ChartRenderScene scene = new ChartRenderScene(120, 90);
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle boxStyle = ChartRenderStyle.CreateFillStroke(ChartColor.White(), ChartColor.Black(), 1.0);
        ChartRenderStyle textStyle = ChartRenderStyle.CreateFill(ChartColor.Black());

        ChartAnnotationChartsOperations.AddArrow(scene, "arrow", 10.0, 10.0, 30.0, 10.0, 6.0, 4.0, lineStyle);
        ChartAnnotationChartsOperations.AddCallout(scene, "callout", "Peak", new ChartRect(40.0, 20.0, 20.0, 10.0), 80.0, 25.0, 3.0, boxStyle, lineStyle, textStyle);

        Assert.That(scene.CommandCount, Is.EqualTo(5));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("arrow-line"));
        AssertLine(scene.CommandAt(0), 10.0, 10.0, 30.0, 10.0);
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("arrow-head"));
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        AssertPoint(scene.CommandAt(1).Points[0], 30.0, 10.0);
        AssertPoint(scene.CommandAt(1).Points[1], 24.0, 12.0);
        AssertPoint(scene.CommandAt(1).Points[2], 24.0, 8.0);
        Assert.That(scene.CommandAt(2).Id, Is.EqualTo("callout-box"));
        Assert.That(scene.CommandAt(2).RadiusX, Is.EqualTo(3.0).Within(0.0000001));
        AssertLine(scene.CommandAt(3), 60.0, 25.0, 80.0, 25.0);
        Assert.That(scene.CommandAt(4).Text, Is.EqualTo("Peak"));
        AssertPoint(scene.CommandAt(4), 44.0, 25.0);
    }

    [Test]
    public void CalloutLeaderStart_ChoosesNearestEdgeTowardAnchor()
    {
        ChartRect box = new ChartRect(20.0, 30.0, 40.0, 20.0);

        AssertPoint(ChartAnnotationChartsOperations.CalloutLeaderStart(box, 10.0, 40.0), 20.0, 40.0);
        AssertPoint(ChartAnnotationChartsOperations.CalloutLeaderStart(box, 80.0, 40.0), 60.0, 40.0);
        AssertPoint(ChartAnnotationChartsOperations.CalloutLeaderStart(box, 40.0, 5.0), 40.0, 30.0);
        AssertPoint(ChartAnnotationChartsOperations.CalloutLeaderStart(box, 40.0, 80.0), 40.0, 50.0);
    }

    [Test]
    public void AnnotationCharts_ValidateInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 80);
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartPath path = new ChartPath();
        path.MoveTo(1.0, 1.0);
        path.LineTo(2.0, 2.0);

        Assert.Throws<System.ArgumentNullException>(() => ChartAnnotationChartsOperations.AddText(null!, "x", "text", 1.0, 1.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartAnnotationChartsOperations.AddText(scene, "", "text", 1.0, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartAnnotationChartsOperations.AddText(scene, "x", null!, 1.0, 1.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAnnotationChartsOperations.AddText(scene, "x", "text", double.NaN, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartAnnotationChartsOperations.AddText(scene, "x", "text", 1.0, 1.0, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAnnotationChartsOperations.AddArrow(scene, "a", 0.0, 0.0, 1.0, 1.0, 0.0, 2.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartAnnotationChartsOperations.AddArrow(scene, "a", 1.0, 1.0, 1.0, 1.0, 2.0, 2.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartAnnotationChartsOperations.AddCallout(scene, "c", "text", new ChartRect(0.0, 0.0, 0.0, 1.0), 1.0, 1.0, 0.0, fill, style, fill));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAnnotationChartsOperations.AddCallout(scene, "c", "text", new ChartRect(0.0, 0.0, 10.0, 10.0), 1.0, 1.0, -1.0, fill, style, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartAnnotationChartsOperations.AddPath(scene, "p", null!, style));
        Assert.Throws<System.ArgumentException>(() => ChartAnnotationChartsOperations.AddPath(scene, "p", new ChartPath(), style));
        Assert.Throws<System.ArgumentNullException>(() => ChartAnnotationChartsOperations.AddPolygonShape(scene, "poly", null!, style));
        Assert.Throws<System.ArgumentException>(() => ChartAnnotationChartsOperations.AddPolygonShape(scene, "poly", new ChartPoint[] { new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0) }, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAnnotationChartsOperations.AddCaption(scene, "cap", "text", -1.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartAnnotationChartsOperations.AddCredits(scene, "cred", "text", double.PositiveInfinity, style));
        Assert.DoesNotThrow(() => ChartAnnotationChartsOperations.AddPath(scene, "valid-path", path, style));
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.Y2, Is.EqualTo(y2).Within(0.0000001));
    }

    static void AssertPoint(ChartRenderCommand command, double x, double y)
    {
        Assert.That(command.X1, Is.EqualTo(x).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertRect(ChartRenderCommand command, double x, double y, double width, double height)
    {
        AssertPoint(command, x, y);
        Assert.That(command.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(command.Height, Is.EqualTo(height).Within(0.0000001));
    }
}
