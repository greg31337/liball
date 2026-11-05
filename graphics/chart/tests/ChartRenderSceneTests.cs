namespace chart.Tests;

[TestFixture]
public class ChartRenderSceneTests
{
    [Test]
    public void RenderScene_RecordsCommandsAndCopiesInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 30);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0));
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(1.0, 1.0),
            new ChartPoint(8.0, 1.0),
            new ChartPoint(8.0, 8.0)
        };

        scene.AddPolygon("triangle", points, fill);
        points[0].X = 99.0;
        fill.FillColor.R = 0;

        ChartRenderScene copy = scene.Copy();
        copy.CommandAt(0).Points[0].X = 77.0;
        copy.CommandAt(0).Style.FillColor.G = 200;

        Assert.That(scene.CommandCount, Is.EqualTo(1));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(scene.CommandAt(0).Points[0].X, Is.EqualTo(1.0));
        AssertColor(scene.CommandAt(0).Style.FillColor, 255, 0, 0, 255);
        Assert.That(copy.CommandAt(0).Points[0].X, Is.EqualTo(77.0));
        AssertColor(copy.CommandAt(0).Style.FillColor, 255, 200, 0, 255);
    }

    [Test]
    public void RenderScene_ReplaysCommandsToSvg()
    {
        ChartRenderScene scene = CreateSampleScene();
        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("<rect x=\"0\" y=\"0\" width=\"40\" height=\"30\" style=\"fill:#FFFFFF;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<rect id=\"plot-bg\" x=\"2\" y=\"2\" width=\"12\" height=\"8\" style=\"fill:#FF0000;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"axis\" x1=\"0\" y1=\"20\" x2=\"30\" y2=\"20\" style=\"fill:none;stroke:#000000;stroke-width:1;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"label\" x=\"4\" y=\"16\" style=\"fill:#0000FF;font-family:Arial;font-size:10\">Hi</text>"));
        Assert.That(svg, Does.Contain("<circle id=\"point\" cx=\"22\" cy=\"8\" r=\"3\" style=\"fill:#00AA00;font-family:Arial;font-size:12\"/>"));
    }

    [Test]
    public void RenderScene_ReplaysCommandsToRaster()
    {
        ChartRenderScene scene = CreateSampleScene();
        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(0, 0), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 4), 255, 0, 0, 255);
        AssertColor(bitmap.GetPixel(22, 8), 0, 170, 0, 255);
    }

    [Test]
    public void RenderScene_RendersPathsAndMarkersToBothBackends()
    {
        ChartRenderScene scene = new ChartRenderScene(30, 30);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 200));
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartPath path = new ChartPath();
        ChartPath marker = new ChartPath();

        path.MoveTo(2.0, 2.0);
        path.LineTo(12.0, 2.0);
        path.LineTo(12.0, 12.0);
        path.Close();
        marker.MoveTo(-0.5, -0.5);
        marker.LineTo(0.5, -0.5);
        marker.LineTo(0.0, 0.5);
        marker.Close();

        scene.AddClear(ChartColor.White());
        scene.AddPath("shape", path, fill);
        scene.AddCustomMarker("custom", marker, 20.0, 20.0, 8.0, stroke);
        path.Commands.Clear();
        marker.Commands.Clear();

        string svg = scene.ToSvgString();
        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(svg, Does.Contain("<path id=\"shape\" d=\"M 2 2 L 12 2 L 12 12 Z\""));
        Assert.That(svg, Does.Contain("<path id=\"custom\" d=\"M -0.5 -0.5 L 0.5 -0.5 L 0 0.5 Z\""));
        AssertColor(bitmap.GetPixel(10, 4), 200, 0, 200, 255);
        Assert.That(bitmap.GetPixel(20, 18).A, Is.EqualTo(255));
    }

    [Test]
    public void RenderScene_RasterDispatchesPrimitivesLinesPolygonsTextAndMarkers()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 30);
        ChartRenderStyle red = CreateSourceFill(255, 0, 0);
        ChartRenderStyle green = CreateSourceFill(0, 170, 0);
        ChartRenderStyle blue = CreateSourceFill(0, 0, 255);
        ChartRenderStyle purple = CreateSourceFill(200, 0, 200);
        ChartRenderStyle yellow = CreateSourceFill(255, 220, 0);
        ChartRenderStyle orange = CreateSourceFill(255, 128, 0);
        ChartRenderStyle blackStroke = CreateSourceStroke(0, 0, 0, 1.0);
        ChartRenderStyle cyanStroke = CreateSourceStroke(0, 200, 220, 2.0);
        ChartRenderStyle text = CreateSourceFill(0, 0, 0);
        ChartPath path = new ChartPath();

        path.MoveTo(24.0, 12.0);
        path.LineTo(30.0, 12.0);
        path.LineTo(30.0, 18.0);
        path.LineTo(24.0, 18.0);
        path.Close();
        text.SetTextScale(1);

        scene.AddClear(ChartColor.White());
        scene.AddRect("rect", 2.0, 2.0, 5.0, 5.0, 0.0, 0.0, red);
        scene.AddCircle("circle", 12.0, 4.0, 2.0, green);
        scene.AddEllipse("ellipse", 20.0, 4.0, 3.0, 2.0, blue);
        scene.AddPolygon("polygon", new ChartPoint[] { new ChartPoint(28.0, 2.0), new ChartPoint(36.0, 2.0), new ChartPoint(32.0, 8.0) }, purple);
        scene.AddLine("line", 2.5, 12.5, 8.5, 12.5, blackStroke);
        scene.AddPolyline("polyline", new ChartPoint[] { new ChartPoint(12.5, 12.5), new ChartPoint(18.5, 12.5), new ChartPoint(18.5, 18.5) }, cyanStroke);
        scene.AddPath("path", path, yellow);
        scene.AddText("text", "A", 3.0, 22.0, text);
        scene.AddMarker("marker", ChartMarkerShape.Square, 36.0, 22.0, 4.0, orange);

        RawBitmap bitmap = scene.ToRawBitmap();

        AssertColor(bitmap.GetPixel(3, 3), 255, 0, 0, 255);
        AssertColor(bitmap.GetPixel(12, 4), 0, 170, 0, 255);
        AssertColor(bitmap.GetPixel(20, 4), 0, 0, 255, 255);
        AssertColor(bitmap.GetPixel(32, 4), 200, 0, 200, 255);
        AssertColor(bitmap.GetPixel(5, 12), 0, 0, 0, 255);
        AssertColor(bitmap.GetPixel(14, 12), 0, 200, 220, 255);
        AssertColor(bitmap.GetPixel(26, 14), 255, 220, 0, 255);
        AssertColor(bitmap.GetPixel(4, 22), 0, 0, 0, 255);
        AssertColor(bitmap.GetPixel(36, 22), 255, 128, 0, 255);
    }

    [Test]
    public void RenderScene_RasterPreservesCanvasClipAndAppliesBlendModes()
    {
        ChartRenderScene scene = new ChartRenderScene(8, 6);
        RawBitmap bitmap = new RawBitmap(8, 6, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartRenderStyle sourceOver = ChartRenderStyle.CreateFill(new ChartColor(255, 0, 0, 128));
        ChartRenderStyle source = ChartRenderStyle.CreateFill(new ChartColor(0, 255, 0, 128));

        source.SetBlendMode(ChartBlendMode.Source);
        canvas.SetClip(2, 1, 4, 4);
        scene.AddClear(ChartColor.White());
        scene.AddRect("source-over", 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, sourceOver);
        scene.AddRect("source", 4.0, 1.0, 2.0, 2.0, 0.0, 0.0, source);

        scene.RenderToRasterCanvas(canvas);

        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(2, 1), 255, 127, 127, 255);
        AssertColor(bitmap.GetPixel(4, 1), 0, 255, 0, 128);
        AssertColor(bitmap.GetPixel(6, 1), 255, 255, 255, 255);
        Assert.That(canvas.ClipX, Is.EqualTo(2));
        Assert.That(canvas.ClipY, Is.EqualTo(1));
        Assert.That(canvas.ClipWidth, Is.EqualTo(4));
        Assert.That(canvas.ClipHeight, Is.EqualTo(4));
    }

    [Test]
    public void RenderScene_SvgDispatchesPrimitivesStylesTextPathsAndMarkers()
    {
        string svg = CreateSvgCoverageScene().ToSvgString();

        Assert.That(svg, Does.Contain("<rect x=\"0\" y=\"0\" width=\"80\" height=\"40\" style=\"fill:#FFFFFF;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"line\" x1=\"1\" y1=\"2\" x2=\"10\" y2=\"2\" style=\"fill:none;stroke:#000000;stroke-width:1;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<polyline id=\"polyline\" points=\"12,2 18,4 22,2\" style=\"fill:none;stroke:#000000;stroke-width:1;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<polygon id=\"polygon\" points=\"26,2 34,2 30,8\" style=\"fill:#00AA00;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<rect id=\"styled-rect\" x=\"2\" y=\"12\" width=\"12\" height=\"8\" rx=\"1\" ry=\"2\" style=\"fill:#0A141E;fill-opacity:0.502;stroke:#28323C;stroke-opacity:0.251;stroke-width:2.5;opacity:0.75;font-family:Inter &amp; Mono;font-size:13\"/>"));
        Assert.That(svg, Does.Contain("<circle id=\"circle\" cx=\"22\" cy=\"16\" r=\"4\" style=\"fill:#0000FF;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<ellipse id=\"ellipse\" cx=\"34\" cy=\"16\" rx=\"5\" ry=\"3\" style=\"fill:#FF8000;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<path id=\"path\" d=\"M 42 12 L 50 12 Q 54 16 50 20 C 48 24 44 24 42 20 Z\" style=\"fill:#00AA00;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"label\" x=\"2\" y=\"34\" style=\"fill:#000000;font-family:Arial;font-size:12\">A&amp;B &lt;Label&gt;</text>"));
        Assert.That(svg, Does.Contain("<rect id=\"square\" x=\"58\" y=\"12\" width=\"6\" height=\"6\" style=\"fill:#FF8000;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<polygon id=\"diamond\" points=\"70,12 73,15 70,18 67,15\" style=\"fill:#0000FF;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"cross-a\" x1=\"57\" y1=\"27\" x2=\"63\" y2=\"33\" style=\"fill:none;stroke:#000000;stroke-width:1;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<line id=\"plus-h\" x1=\"67\" y1=\"30\" x2=\"73\" y2=\"30\" style=\"fill:none;stroke:#000000;stroke-width:1;font-family:Arial;font-size:12\"/>"));
        Assert.That(svg, Does.Contain("<path id=\"custom\" d=\"M -0.5 -0.5 L 0.5 -0.5 L 0 0.5 Z\" transform=\"translate(76 30) scale(6 6)\" style=\"fill:#00AA00;font-family:Arial;font-size:12\"/>"));
        AssertValidXml(svg);
    }

    [Test]
    public void RenderScene_SvgOutputIsDeterministicForStableCommandIds()
    {
        string first = CreateSvgCoverageScene().ToSvgString();
        string second = CreateSvgCoverageScene().ToSvgString();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first.IndexOf("id=\"line\"", System.StringComparison.Ordinal), Is.LessThan(first.IndexOf("id=\"polyline\"", System.StringComparison.Ordinal)));
        Assert.That(first.IndexOf("id=\"cross-a\"", System.StringComparison.Ordinal), Is.LessThan(first.IndexOf("id=\"cross-b\"", System.StringComparison.Ordinal)));
        Assert.That(first.IndexOf("id=\"plus-h\"", System.StringComparison.Ordinal), Is.LessThan(first.IndexOf("id=\"plus-v\"", System.StringComparison.Ordinal)));
    }

    [Test]
    public void RenderStyle_ConvertsToSvgStyleAndRasterColors()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFillStroke(ChartColor.FromRgba(10, 20, 30, 128), ChartColor.Black(), 2.0);

        style.SetOpacity(0.5);
        style.SetFont("Inter", 14);
        style.SetTextScale(2);
        SvgStyle svgStyle = style.ToSvgStyle();
        ChartColor raster = style.RasterFillColor();

        Assert.That(svgStyle.ToStyleString(), Does.Contain("fill:#0A141E"));
        Assert.That(svgStyle.ToStyleString(), Does.Contain("stroke:#000000"));
        Assert.That(svgStyle.ToStyleString(), Does.Contain("font-family:Inter"));
        Assert.That(style.TextScale, Is.EqualTo(2));
        AssertColor(raster, 10, 20, 30, 64);
    }

    [Test]
    public void RenderScene_ValidatesInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(10, 10);
        ChartRenderStyle style = new ChartRenderStyle();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartRenderScene(0, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartRenderScene(10, 0));
        Assert.Throws<System.ArgumentNullException>(() => new ChartRenderCommand(ChartRenderCommandKind.Rect, "x", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartRenderCommand((ChartRenderCommandKind)99, "x", style));
        Assert.Throws<System.ArgumentNullException>(() => scene.AddCommand(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => scene.CommandAt(0));
        Assert.Throws<System.ArgumentException>(() => scene.RenderRaster(new RawBitmap(1, 1, ChartPixelFormat.Rgba32)));
        Assert.Throws<System.ArgumentNullException>(() => scene.RenderRaster((RawBitmap)null!));
        Assert.Throws<System.ArgumentNullException>(() => scene.RenderToRasterCanvas((ChartRasterCanvas)null!));
        Assert.Throws<System.ArgumentNullException>(() => scene.RenderSvg(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderCommand.CreatePolyline("p", null!, style));
        Assert.Throws<System.ArgumentException>(() => ChartRenderCommand.CreatePolyline("p", new ChartPoint[] { new ChartPoint(0.0, 0.0) }, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRenderCommand.CreateRect("r", 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderCommand.CreatePath("path", null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderCommand.CreateText("text", null!, 0.0, 0.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRenderCommand.CreateMarker("m", (ChartMarkerShape)99, 0.0, 0.0, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderCommand.CreateCustomMarker("m", null!, 0.0, 0.0, 1.0, style));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFill(null!));
        Assert.Throws<System.ArgumentNullException>(() => style.SetStroke(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetStroke(ChartColor.Black(), 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetOpacity(double.NaN));
        Assert.Throws<System.ArgumentNullException>(() => style.SetFont(null!, 12));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFont("Arial", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetTextScale(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetQuality((ChartRenderQuality)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetBlendMode((ChartBlendMode)99));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderStyle.ApplyOpacity(null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRenderStyle.ApplyOpacity(ChartColor.Black(), 2.0));
    }

    static ChartRenderScene CreateSampleScene()
    {
        ChartRenderScene scene = new ChartRenderScene(40, 30);
        ChartRenderStyle red = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0));
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle text = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255));
        ChartRenderStyle green = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 170, 0));

        text.SetFont("Arial", 10);
        scene.AddClear(ChartColor.White());
        scene.AddRect("plot-bg", 2.0, 2.0, 12.0, 8.0, 0.0, 0.0, red);
        scene.AddLine("axis", 0.0, 20.0, 30.0, 20.0, line);
        scene.AddText("label", "Hi", 4.0, 16.0, text);
        scene.AddMarker("point", ChartMarkerShape.Circle, 22.0, 8.0, 6.0, green);
        return scene;
    }

    static ChartRenderScene CreateSvgCoverageScene()
    {
        ChartRenderScene scene = new ChartRenderScene(80, 40);
        ChartRenderStyle green = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 170, 0));
        ChartRenderStyle blue = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255));
        ChartRenderStyle orange = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 128, 0));
        ChartRenderStyle blackFill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle blackStroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle styled = ChartRenderStyle.CreateFillStroke(new ChartColor(10, 20, 30, 128), new ChartColor(40, 50, 60, 64), 2.5);
        ChartPath path = new ChartPath();
        ChartPath marker = new ChartPath();

        styled.SetOpacity(0.75);
        styled.SetFont("Inter & Mono", 13);
        path.MoveTo(42.0, 12.0);
        path.LineTo(50.0, 12.0);
        path.QuadraticBezierTo(54.0, 16.0, 50.0, 20.0);
        path.CubicBezierTo(48.0, 24.0, 44.0, 24.0, 42.0, 20.0);
        path.Close();
        marker.MoveTo(-0.5, -0.5);
        marker.LineTo(0.5, -0.5);
        marker.LineTo(0.0, 0.5);
        marker.Close();

        scene.AddClear(ChartColor.White());
        scene.AddLine("line", 1.0, 2.0, 10.0, 2.0, blackStroke);
        scene.AddPolyline("polyline", new ChartPoint[] { new ChartPoint(12.0, 2.0), new ChartPoint(18.0, 4.0), new ChartPoint(22.0, 2.0) }, blackStroke);
        scene.AddPolygon("polygon", new ChartPoint[] { new ChartPoint(26.0, 2.0), new ChartPoint(34.0, 2.0), new ChartPoint(30.0, 8.0) }, green);
        scene.AddRect("styled-rect", 2.0, 12.0, 12.0, 8.0, 1.0, 2.0, styled);
        scene.AddCircle("circle", 22.0, 16.0, 4.0, blue);
        scene.AddEllipse("ellipse", 34.0, 16.0, 5.0, 3.0, orange);
        scene.AddPath("path", path, green);
        scene.AddText("label", "A&B <Label>", 2.0, 34.0, blackFill);
        scene.AddMarker("square", ChartMarkerShape.Square, 61.0, 15.0, 6.0, orange);
        scene.AddMarker("diamond", ChartMarkerShape.Diamond, 70.0, 15.0, 6.0, blue);
        scene.AddMarker("cross", ChartMarkerShape.Cross, 60.0, 30.0, 6.0, blackStroke);
        scene.AddMarker("plus", ChartMarkerShape.Plus, 70.0, 30.0, 6.0, blackStroke);
        scene.AddCustomMarker("custom", marker, 76.0, 30.0, 6.0, green);
        return scene;
    }

    static ChartRenderStyle CreateSourceFill(int r, int g, int b)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.FromRgb((byte)r, (byte)g, (byte)b));
        style.SetBlendMode(ChartBlendMode.Source);
        return style;
    }

    static ChartRenderStyle CreateSourceStroke(int r, int g, int b, double strokeWidth)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.FromRgb((byte)r, (byte)g, (byte)b), strokeWidth);
        style.SetBlendMode(ChartBlendMode.Source);
        return style;
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }

    static void AssertValidXml(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        Assert.That(document.DocumentElement!.Name, Is.EqualTo("svg"));
    }
}
