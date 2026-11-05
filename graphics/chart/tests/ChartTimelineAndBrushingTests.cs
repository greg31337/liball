namespace chart.Tests;

[TestFixture]
public class ChartTimelineAndBrushingTests
{
    [Test]
    public void TimelineExport_PreservesSnapshotOrderAndTiming()
    {
        ChartRenderScene firstScene = new ChartRenderScene(12, 8);
        firstScene.AddRect("frame-a", 1.0, 1.0, 3.0, 2.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        ChartTimelineFrame firstFrame = new ChartTimelineFrame("first", firstScene, 100);
        firstScene.AddRect("late-change", 5.0, 1.0, 2.0, 2.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.White()));

        ChartRenderScene secondScene = new ChartRenderScene(12, 8);
        secondScene.AddRect("frame-b", 2.0, 2.0, 4.0, 3.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.White()));
        ChartTimeline timeline = new ChartTimeline();
        timeline.AddFrame(firstFrame);
        timeline.AddFrame(new ChartTimelineFrame("second", secondScene, 250));

        ChartExportOptions options = new ChartExportOptions();
        options.Format = ChartOutputFormat.Svg;
        ChartFrameExportResult[] results = new ChartExport().ExportTimeline(timeline, options);

        Assert.That(timeline.GetFrameCount(), Is.EqualTo(2));
        Assert.That(timeline.TotalDurationMilliseconds(), Is.EqualTo(350));
        Assert.That(results.Length, Is.EqualTo(2));
        Assert.That(results[0].FrameId, Is.EqualTo("first"));
        Assert.That(results[0].FrameIndex, Is.EqualTo(0));
        Assert.That(results[0].StartMilliseconds, Is.EqualTo(0));
        Assert.That(results[0].DurationMilliseconds, Is.EqualTo(100));
        Assert.That(results[0].Export.Text, Does.Contain("id=\"frame-a\""));
        Assert.That(results[0].Export.Text, Does.Not.Contain("late-change"));
        Assert.That(results[1].FrameId, Is.EqualTo("second"));
        Assert.That(results[1].FrameIndex, Is.EqualTo(1));
        Assert.That(results[1].StartMilliseconds, Is.EqualTo(100));
        Assert.That(results[1].DurationMilliseconds, Is.EqualTo(250));
        Assert.That(results[1].Export.Text, Does.Contain("id=\"frame-b\""));
    }

    [Test]
    public void TimelineCopies_AreIndependentAndValidateSequenceInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(10, 6);
        scene.AddClear(ChartColor.Black());
        ChartTimelineFrame frame = new ChartTimelineFrame("base", scene, 40);
        ChartTimeline timeline = new ChartTimeline();
        timeline.AddFrame(frame);
        ChartTimeline copy = timeline.Copy();

        ChartRenderScene returnedScene = timeline.FrameAt(0).GetScene();
        returnedScene.AddClear(ChartColor.White());
        timeline.AddFrame(new ChartTimelineFrame("next", new ChartRenderScene(10, 6), 60));

        Assert.That(frame.GetFrameId(), Is.EqualTo("base"));
        Assert.That(frame.GetDurationMilliseconds(), Is.EqualTo(40));
        Assert.That(timeline.FrameAt(0).GetScene().GetCommandCount(), Is.EqualTo(1));
        Assert.That(copy.GetFrameCount(), Is.EqualTo(1));
        Assert.That(copy.TotalDurationMilliseconds(), Is.EqualTo(40));

        Assert.Throws<System.ArgumentNullException>(() => new ChartTimelineFrame("null-scene", null!, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartTimelineFrame("duration", scene, 0));
        Assert.Throws<System.ArgumentNullException>(() => timeline.AddFrame(null!));
        Assert.Throws<System.ArgumentException>(() => timeline.AddFrame(frame));
        Assert.Throws<System.ArgumentException>(() => timeline.AddFrame(new ChartTimelineFrame("wrong-size", new ChartRenderScene(11, 6), 1)));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => timeline.FrameAt(2));
        Assert.Throws<System.ArgumentNullException>(() => new ChartExport().ExportTimeline(null!, new ChartExportOptions()));
        Assert.Throws<System.ArgumentNullException>(() => new ChartExport().ExportTimeline(timeline, null!));
    }

    [Test]
    public void LinkedBrushes_ResolveRowsAndProduceDeterministicMetadata()
    {
        ChartBrush zeta = new ChartBrush("zeta", "overview", "shared", new ChartRect(1.0, 2.0, 3.0, 4.0));
        zeta.SetRows(new int[] { 4, 2, 4 });
        ChartBrush alpha = new ChartBrush("alpha", "detail", "shared", new ChartRect(5.0, 6.0, 2.0, 1.0));
        alpha.SetRows(new int[] { 3, 2 });
        ChartSelectionSidecar sidecar = new ChartSelectionSidecar();
        sidecar.AddBrush(zeta);
        sidecar.AddBrush(alpha);

        zeta.SetRows(new int[] { 9 });
        ChartBrush returned = sidecar.BrushAt(0);
        returned.SetRows(new int[] { 8 });
        ChartSelection linked = sidecar.CreateLinkedSelection("target", "shared");

        Assert.That(sidecar.GetBrushCount(), Is.EqualTo(2));
        Assert.That(sidecar.CopyLinkedRows("shared"), Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(linked.CopySelectedRows(), Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(linked.GetLinkGroup(), Is.EqualTo("shared"));
        Assert.That(sidecar.BrushAt(0).CopyRows(), Is.EqualTo(new int[] { 2, 4 }));
        Assert.That(alpha.ContainsPoint(5.0, 6.0), Is.True);
        Assert.That(alpha.ContainsPoint(7.0, 7.0), Is.True);
        Assert.That(alpha.ContainsPoint(7.1, 7.0), Is.False);

        string expected = "linked-brush alpha source detail group shared: bounds=5,6,2,1; rows=2,3\nlinked-brush zeta source overview group shared: bounds=1,2,3,4; rows=2,4";
        Assert.That(sidecar.MetadataText(), Is.EqualTo(expected));

        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        metadata.SetSelectionSidecar(sidecar);
        Assert.That(metadata.HasMetadata(), Is.True);
        Assert.That(metadata.MetadataText(), Is.EqualTo(expected));
        Assert.That(new ChartExport().ToSvg(new ChartRenderScene(10, 10), metadata), Does.Contain("linked-brush alpha source detail group shared"));

        ChartSelectionSidecar copy = sidecar.Copy();
        sidecar.AddBrush(new ChartBrush("third", "overview", "other", new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.That(copy.GetBrushCount(), Is.EqualTo(2));
    }

    [Test]
    public void LinkedBrushes_ValidateGeometryRowsAndSidecarInputs()
    {
        Assert.Throws<System.ArgumentException>(() => new ChartBrush("", "source", "group", new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => new ChartBrush("brush", "", "group", new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => new ChartBrush("brush", "source", "", new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => new ChartBrush("brush", "source", "group", null!));
        Assert.Throws<System.ArgumentException>(() => new ChartBrush("brush", "source", "group", new ChartRect(0.0, 0.0, 0.0, 1.0)));

        ChartBrush brush = new ChartBrush("brush", "source", "group", new ChartRect(0.0, 0.0, 1.0, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => brush.SetRows(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => brush.SetRows(new int[] { -1 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => brush.ContainsPoint(double.NaN, 0.0));

        ChartSelectionSidecar sidecar = new ChartSelectionSidecar();
        sidecar.AddBrush(brush);
        Assert.Throws<System.ArgumentNullException>(() => sidecar.AddBrush(null!));
        Assert.Throws<System.ArgumentException>(() => sidecar.AddBrush(brush));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => sidecar.BrushAt(1));
        Assert.Throws<System.ArgumentException>(() => sidecar.CopyLinkedRows(""));
        Assert.That(sidecar.CopyLinkedRows("missing"), Is.Empty);
    }
}
