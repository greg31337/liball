namespace chart.Tests;

[TestFixture]
public class ChartInterfaceTests
{
    [Test]
    public void PublicInterfacesAreImplementedByBuiltInTypes()
    {
        Assert.That(typeof(IChartRenderTarget).IsInterface, Is.True);
        Assert.That(typeof(IChartRenderer).IsInterface, Is.True);
        Assert.That(typeof(IChartTickFormatter).IsInterface, Is.True);
        Assert.That(typeof(IChartRenderTarget).IsAssignableFrom(typeof(ChartRenderScene)), Is.True);
        Assert.That(typeof(IChartRenderer).IsAssignableFrom(typeof(ChartCartesianRenderer)), Is.True);
        Assert.That(typeof(IChartTickFormatter).IsAssignableFrom(typeof(ChartDefaultTickFormatter)), Is.True);
    }

    [Test]
    public void Document_RenderAcceptsCustomRendererAndPreservesDocumentInitialization()
    {
        ChartDocument document = new ChartDocument(80.0, 40.0);
        document.SetBackground(ChartColor.White());
        TestRenderer renderer = new TestRenderer();

        ChartRenderScene scene = document.Render(renderer);

        Assert.That(renderer.RenderCount, Is.EqualTo(1));
        Assert.That(scene.CommandCount, Is.EqualTo(2));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Clear));
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("custom-renderer"));
    }

    [Test]
    public void SceneBuildersAcceptRecordingRenderTarget()
    {
        RecordingRenderTarget target = new RecordingRenderTarget(100, 100);
        ChartPieLayout layout = ChartPieChartsOperations.LayoutPie(
            new string[] { "A", "B" },
            new double[] { 1.0, 2.0 },
            50.0,
            50.0,
            40.0,
            0.0,
            ChartPieSliceOrder.Original,
            ChartPalettesOperations.Categorical());

        ChartPieChartsOperations.AddSlices(target, "pie", layout);

        Assert.That(target.PathCount, Is.EqualTo(2));
        Assert.That(target.Scene.CommandAt(0).Id, Is.EqualTo("pie-slice-0"));
        Assert.That(target.Scene.CommandAt(1).Id, Is.EqualTo("pie-slice-1"));
    }

    [Test]
    public void AxisUsesCustomTickFormatterAndRetainsItWhenCopied()
    {
        PrefixTickFormatter formatter = new PrefixTickFormatter("value=");
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 100.0, 40.0));
        axis.SetTickLabels(true, false, 0.0);
        axis.SetTickFormatter(formatter);
        ChartAxis copy = axis.Copy();
        SvgWriter writer = new SvgWriter(120, 80);

        ChartAxisRenderer.RenderSvg(writer, copy);
        string svg = writer.ToSvgString();

        Assert.That(copy.TickFormatter, Is.SameAs(formatter));
        Assert.That(formatter.FormatCount, Is.EqualTo(2));
        Assert.That(svg, Does.Contain(">value=0<"));
        Assert.That(svg, Does.Contain(">value=1<"));
    }

    [Test]
    public void InterfaceEntryPointsValidateNullCollaborators()
    {
        ChartDocument document = new ChartDocument(10.0, 10.0);
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 10.0, 10.0));

        Assert.Throws<System.ArgumentNullException>(() => document.Render(null!));
        Assert.Throws<System.ArgumentNullException>(() => axis.SetTickFormatter(null!));
    }

    class TestRenderer : IChartRenderer
    {
        public int RenderCount;

        public void RenderTo(IChartRenderTarget target)
        {
            RenderCount++;
            target.AddText("custom-renderer", "ok", 4.0, 8.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        }
    }

    class PrefixTickFormatter : IChartTickFormatter
    {
        public string Prefix;
        public int FormatCount;

        public PrefixTickFormatter(string prefix)
        {
            Prefix = prefix;
        }

        public string FormatTick(ChartTick tick)
        {
            FormatCount++;
            return Prefix + ChartTickFormatterOperations.FormatNumber(tick.Value, 0);
        }
    }

    class RecordingRenderTarget : IChartRenderTarget
    {
        public ChartRenderScene Scene;
        public int PathCount;

        public RecordingRenderTarget(int width, int height)
        {
            Scene = new ChartRenderScene(width, height);
        }

        public int GetWidth()
        {
            return Scene.Width;
        }

        public int GetHeight()
        {
            return Scene.Height;
        }

        public ChartRenderCommand AddClear(ChartColor color)
        {
            return Scene.AddClear(color);
        }

        public ChartRenderCommand AddLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style)
        {
            return Scene.AddLine(id, x1, y1, x2, y2, style);
        }

        public ChartRenderCommand AddPolyline(string id, ChartPoint[] points, ChartRenderStyle style)
        {
            return Scene.AddPolyline(id, points, style);
        }

        public ChartRenderCommand AddPolygon(string id, ChartPoint[] points, ChartRenderStyle style)
        {
            return Scene.AddPolygon(id, points, style);
        }

        public ChartRenderCommand AddRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style)
        {
            return Scene.AddRect(id, x, y, width, height, radiusX, radiusY, style);
        }

        public ChartRenderCommand AddCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style)
        {
            return Scene.AddCircle(id, centerX, centerY, radius, style);
        }

        public ChartRenderCommand AddEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
        {
            return Scene.AddEllipse(id, centerX, centerY, radiusX, radiusY, style);
        }

        public ChartRenderCommand AddPath(string id, ChartPath path, ChartRenderStyle style)
        {
            PathCount++;
            return Scene.AddPath(id, path, style);
        }

        public ChartRenderCommand AddText(string id, string text, double x, double y, ChartRenderStyle style)
        {
            return Scene.AddText(id, text, x, y, style);
        }

        public ChartRenderCommand AddMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style)
        {
            return Scene.AddMarker(id, shape, centerX, centerY, size, style);
        }

        public ChartRenderCommand AddCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style)
        {
            return Scene.AddCustomMarker(id, markerPath, centerX, centerY, size, style);
        }

        public ChartRenderCommand AddArc(string id, double centerX, double centerY, double radius, double startAngle, double endAngle, ChartRenderStyle style)
        {
            return Scene.AddArc(id, centerX, centerY, radius, startAngle, endAngle, style);
        }

        public ChartRenderCommand AddWedge(string id, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartRenderStyle style)
        {
            return Scene.AddWedge(id, centerX, centerY, innerRadius, outerRadius, startAngle, endAngle, style);
        }

        public ChartRenderCommand AddImage(string id, RawBitmap bitmap, double x, double y, double width, double height, ChartRenderStyle style)
        {
            return Scene.AddImage(id, bitmap, x, y, width, height, style);
        }

        public ChartRenderCommand BeginGroup(string id)
        {
            return Scene.BeginGroup(id);
        }

        public ChartRenderCommand EndGroup()
        {
            return Scene.EndGroup();
        }

        public ChartRenderCommand PushTransform(string id, ChartMatrix transform)
        {
            return Scene.PushTransform(id, transform);
        }

        public ChartRenderCommand PopTransform()
        {
            return Scene.PopTransform();
        }

        public ChartRenderCommand PushClip(string id, ChartRect bounds)
        {
            return Scene.PushClip(id, bounds);
        }

        public ChartRenderCommand PopClip()
        {
            return Scene.PopClip();
        }
    }
}
