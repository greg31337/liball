namespace chart.Tests;

[TestFixture]
public class ChartDiagnosticsTests
{
    [Test]
    public void LayoutOverlay_AddsBoundsAndLabelsForEveryLayoutItem()
    {
        ChartDocument document = CreateDiagnosticDocument();
        ChartLayoutResult layout = ChartLayoutEngineOperations.Layout(document);
        ChartRenderScene scene = new ChartRenderScene(320, 220);

        layout.AddDiagnosticOverlayWithPrefix(scene, "debug");

        Assert.That(scene.CommandCount, Is.EqualTo(layout.ItemCount * 2));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("debug-figure-bounds"));
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("debug-figure-label"));
        Assert.That(scene.CommandAt(1).Text, Does.Contain("Figure:figure"));
        Assert.That(scene.CommandAt(0).Style.HasStroke, Is.True);
        Assert.That(scene.CommandAt(0).Style.FillNone, Is.True);
        Assert.That(scene.CommandAt(scene.CommandCount - 1).Text, Does.Contain("axis-bottom"));
    }

    [Test]
    public void LayoutTrace_DescribesLayoutItemsDeterministically()
    {
        ChartDocument document = CreateDiagnosticDocument();
        ChartLayoutResult layout = ChartLayoutEngineOperations.Layout(document);

        string trace = layout.ToTraceString();

        Assert.That(trace, Does.StartWith("ChartLayoutTrace figure=x=0 y=0 width=320 height=220"));
        Assert.That(trace, Does.Contain("items=6"));
        Assert.That(trace, Does.Contain("[0] Figure id=figure side=Top bounds=x=0 y=0 width=320 height=220"));
        Assert.That(trace, Does.Contain("Title id=title side=Top"));
        Assert.That(trace, Does.Contain("Legend id=legend side=Right"));
        Assert.That(trace, Does.Contain("Axis id=axis-bottom side=Bottom"));
    }

    [Test]
    public void RenderTrace_DescribesSceneCommandsDeterministically()
    {
        ChartRenderScene scene = new ChartRenderScene(80, 40);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 2.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(10, 20, 30));

        scene.AddClear(ChartColor.White());
        scene.AddLine("axis", 1.0, 2.0, 30.0, 2.0, stroke);
        scene.AddRect("box", 4.0, 5.0, 20.0, 10.0, 1.0, 2.0, fill);
        scene.AddText("label", "Hello", 6.0, 18.0, fill);
        scene.AddMarker("point", ChartMarkerShape.Diamond, 30.0, 20.0, 8.0, fill);

        string trace = scene.ToTraceString();

        Assert.That(trace, Does.StartWith("ChartRenderTrace width=80 height=40 commands=5"));
        Assert.That(trace, Does.Contain("[0] Clear id= style=fill=#FFFFFFFF stroke=unset strokeWidth=1 opacity=1 blend=SourceOver"));
        Assert.That(trace, Does.Contain("[1] Line id=axis x1=1 y1=2 x2=30 y2=2 style=fill=none stroke=#000000FF strokeWidth=2 opacity=1 blend=SourceOver"));
        Assert.That(trace, Does.Contain("[2] Rect id=box x=4 y=5 width=20 height=10 radiusX=1 radiusY=2 style=fill=#0A141EFF"));
        Assert.That(trace, Does.Contain("[3] Text id=label text=\"Hello\" x=6 y=18"));
        Assert.That(trace, Does.Contain("[4] Marker id=point shape=Diamond cx=30 cy=20 size=8"));
    }

    [Test]
    public void Diagnostics_ValidateInputs()
    {
        ChartLayoutResult layout = ChartLayoutEngineOperations.Layout(new ChartDocument(200.0, 160.0));
        ChartRenderScene scene = new ChartRenderScene(200, 160);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.AddLayoutOverlay(null!, layout));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.AddLayoutOverlay(scene, null!));
        Assert.Throws<System.ArgumentException>(() => ChartDiagnosticsOperations.AddLayoutOverlay(scene, layout, ""));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.LayoutTrace(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.RenderTrace(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.LayoutItemLabel(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.FormatRect(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.StyleTrace(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDiagnosticsOperations.SafeText(null!));
        Assert.That(ChartDiagnosticsOperations.SafeText("a\nb\rc"), Is.EqualTo("a b c"));
        Assert.That(ChartDiagnosticsOperations.StyleTrace(style), Does.Contain("fill=#000000FF"));
    }

    static ChartDocument CreateDiagnosticDocument()
    {
        ChartDocument document = new ChartDocument(320.0, 220.0);
        ChartPlot plot = new ChartPlot("main");
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 1.0, 1.0));

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.SetTitle("Debug");
        document.AddLegendLayout("legend", ChartSide.Right, 40.0, 0.0, 4.0);
        axis.SetTickLabels(true, false, 0.0);
        plot.AddAxis(axis);
        document.AddPlot(plot);
        return document;
    }
}
