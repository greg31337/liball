namespace chart.Tests;

[TestFixture]
public class ChartUnifiedExportTests
{
    [Test]
    public void UnifiedSceneExport_DispatchesEveryOutputFormat()
    {
        ChartRenderScene scene = new ChartRenderScene(2, 1);
        scene.AddClear(new ChartColor(10, 20, 30, 40));
        ChartExport export = new ChartExport();
        ChartExportOptions options = new ChartExportOptions();

        options.Format = ChartOutputFormat.RawBitmap;
        ChartExportResult raw = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Bmp;
        ChartExportResult bmp = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Png;
        ChartExportResult png = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Svg;
        ChartExportResult svg = export.ExportScene(scene, options);

        Assert.That(raw.Bitmap, Is.Not.Null);
        Assert.That(raw.Bitmap!.GetWidth(), Is.EqualTo(2));
        Assert.That(raw.Bytes, Is.EqualTo(raw.Bitmap.CopyBuffer()));
        Assert.That(bmp.Bitmap, Is.Null);
        Assert.That(new byte[] { bmp.Bytes[0], bmp.Bytes[1] }, Is.EqualTo(new byte[] { 66, 77 }));
        Assert.That(new byte[] { png.Bytes[0], png.Bytes[1], png.Bytes[2], png.Bytes[3], png.Bytes[4], png.Bytes[5], png.Bytes[6], png.Bytes[7] }, Is.EqualTo(PngEncoder.Signature()));
        Assert.That(svg.Text, Does.StartWith("<svg "));
        Assert.That(svg.Text, Does.Contain("width=\"2\" height=\"1\""));
        Assert.That(svg.Bytes, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(svg.Text)));
    }

    [Test]
    public void UnifiedSceneExport_AppliesScaleTransparencyAndBackgroundOverride()
    {
        ChartRenderScene scene = new ChartRenderScene(2, 2);
        scene.AddClear(new ChartColor(200, 10, 20, 255));
        ChartExport export = new ChartExport();
        ChartExportOptions options = new ChartExportOptions();
        options.Format = ChartOutputFormat.RawBitmap;
        options.Scale = new ChartExportScaleSettings(1.0, 2.0);
        options.BackgroundMode = ChartExportBackgroundMode.Transparent;

        ChartExportResult transparent = export.ExportScene(scene, options);
        options.BackgroundMode = ChartExportBackgroundMode.SolidColor;
        options.BackgroundColor = new ChartColor(5, 100, 200, 255);
        ChartExportResult solid = export.ExportScene(scene, options);
        options.BackgroundMode = ChartExportBackgroundMode.PreserveScene;
        ChartExportResult preserved = export.ExportScene(scene, options);

        Assert.That(transparent.Bitmap!.GetWidth(), Is.EqualTo(4));
        Assert.That(transparent.Bitmap.GetHeight(), Is.EqualTo(4));
        Assert.That(transparent.Bitmap.GetPixel(3, 3).A, Is.EqualTo(0));
        Assert.That(solid.Bitmap!.GetPixel(3, 3).EqualsColor(new ChartColor(5, 100, 200, 255)), Is.True);
        Assert.That(preserved.Bitmap!.GetPixel(3, 3).EqualsColor(new ChartColor(200, 10, 20, 255)), Is.True);
    }

    [Test]
    public void UnifiedSceneExport_UsesAccessibilityAndEncoderMetadata()
    {
        ChartRenderScene scene = new ChartRenderScene(3, 2);
        ChartExport export = new ChartExport();
        ChartExportOptions options = new ChartExportOptions();
        options.Accessibility.SetTitleDescription("Unified title", "Unified description");
        options.Svg.IdPrefix = "export";
        options.Format = ChartOutputFormat.Svg;

        ChartExportResult svg = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Png;
        options.Png.Metadata.Author = "Chart author";
        ChartExportResult png = export.ExportScene(scene, options);
        string pngText = System.Text.Encoding.UTF8.GetString(png.Bytes);

        Assert.That(svg.Text, Does.Contain("<title>Unified title</title>"));
        Assert.That(svg.Text, Does.Contain("<desc>Unified description</desc>"));
        Assert.That(pngText, Does.Contain("Unified title"));
        Assert.That(pngText, Does.Contain("Unified description"));
        Assert.That(pngText, Does.Contain("Chart author"));
    }

    [Test]
    public void UnifiedDocumentExport_UsesDocumentDefaultsAndHonorsStreamOwnership()
    {
        ChartDocument document = new ChartDocument(3.0, 2.0);
        document.SetBackground(new ChartColor(9, 8, 7, 255));
        document.SetAccessibilityTitleDescription("Document title", "Document description");
        TestRenderer renderer = new TestRenderer();
        ChartExport export = new ChartExport();
        ChartExportOptions options = new ChartExportOptions();
        options.Format = ChartOutputFormat.Svg;
        options.Scale = new ChartExportScaleSettings(1.0, 2.0);

        ChartExportResult svg = export.ExportDocument(document, renderer, options);
        System.IO.MemoryStream openStream = new System.IO.MemoryStream();
        export.WriteDocumentExport(document, renderer, openStream, options);
        System.IO.MemoryStream closedStream = new System.IO.MemoryStream();
        options.LeaveStreamOpen = false;
        export.WriteDocumentExport(document, renderer, closedStream, options);

        Assert.That(svg.Text, Does.Contain("width=\"6\" height=\"4\""));
        Assert.That(svg.Text, Does.Contain("<title>Document title</title>"));
        Assert.That(svg.Text, Does.Contain("<desc>Document description</desc>"));
        Assert.That(svg.Text, Does.Contain("id=\"document-shape\""));
        Assert.That(openStream.CanWrite, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(openStream.ToArray()), Does.Contain("Document title"));
        Assert.That(closedStream.CanWrite, Is.False);
        Assert.That(renderer.RenderCount, Is.EqualTo(3));
    }

    [Test]
    public void UnifiedSceneExport_WritesFilesAndValidatesRequiredInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(1, 1);
        ChartExport export = new ChartExport();
        ChartExportOptions options = new ChartExportOptions();
        string path = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "unified-export.png");

        export.SaveSceneExport(scene, path, options);

        Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(export.ExportScene(scene, options).Bytes));
        Assert.Throws<System.ArgumentNullException>(() => export.ExportScene(null!, options));
        Assert.Throws<System.ArgumentNullException>(() => export.ExportScene(scene, null!));
        Assert.Throws<System.ArgumentNullException>(() => export.ExportDocument(null!, new TestRenderer(), options));
        Assert.Throws<System.ArgumentNullException>(() => export.ExportDocument(new ChartDocument(1.0, 1.0), null!, options));
        Assert.Throws<System.ArgumentNullException>(() => export.WriteSceneExport(scene, null!, options));
    }

    class TestRenderer : IChartRenderer
    {
        public int RenderCount;

        public void RenderTo(IChartRenderTarget target)
        {
            RenderCount++;
            target.AddRect("document-shape", 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        }
    }
}
