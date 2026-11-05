namespace svg.Tests;

public class SvgCanvasTests
{
    [Test]
    public void SavesAndRestoresPaintOpacityAndAffineStateOnEditablePaths()
    {
        SvgCanvas canvas = SvgCanvas.CreateWithViewport(100, 50);
        SvgCanvas returned = canvas
            .SetFill("red")
            .BeginPath().Rectangle(0, 0, 10, 10).Fill()
            .Save()
            .Translate(10, 5).Scale(2).SetFill("blue").SetOpacity(.5)
            .BeginPath().Circle(5, 5, 3).Fill()
            .Restore()
            .BeginPath().Rectangle(20, 0, 5, 5).Fill();

        List<SvgNode> paths = canvas.Builder.Root.Children.Where(item => item.ElementKind == SvgElementKind.Path).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(canvas));
            Assert.That(canvas.Succeeded(), Is.True, canvas.Message);
            Assert.That(canvas.GetSaveDepth(), Is.Zero);
            Assert.That(Attribute(canvas.Builder.Root, "width"), Is.EqualTo("100"));
            Assert.That(Attribute(canvas.Builder.Root, "height"), Is.EqualTo("50"));
            Assert.That(Attribute(canvas.Builder.Root, "viewBox"), Is.EqualTo("0 0 100 50"));
            Assert.That(paths, Has.Count.EqualTo(3));
            Assert.That(Attribute(paths[0], "fill"), Is.EqualTo(PaintText("red")));
            Assert.That(Attribute(paths[0], "transform"), Is.Null);
            Assert.That(Attribute(paths[1], "fill"), Is.EqualTo(PaintText("blue")));
            Assert.That(Attribute(paths[1], "opacity"), Is.EqualTo("0.5"));
            Assert.That(Attribute(paths[1], "transform"), Is.EqualTo("matrix(2 0 0 2 10 5)"));
            Assert.That(Attribute(paths[2], "fill"), Is.EqualTo(PaintText("red")));
            Assert.That(Attribute(paths[2], "opacity"), Is.Null);
            Assert.That(Attribute(paths[2], "transform"), Is.Null);
            Assert.That(canvas.LastNode, Is.SameAs(paths[2]));
            Assert.That(canvas.Build(), Is.SameAs(canvas.Builder.Document));
        });
    }

    [Test]
    public void RecordsIntersectingClipsAndFillStrokePathsWithRestoredClipState()
    {
        SvgCanvas canvas = new SvgCanvas();
        canvas.BeginPath().Rectangle(0, 0, 20, 20).Clip();
        SvgNode firstClip = canvas.LastDefinition;
        canvas.Save()
            .Translate(5, 0)
            .BeginPath().Circle(10, 10, 8).Clip();
        SvgNode secondClip = canvas.LastDefinition;
        canvas.SetFill("orange").SetStroke("black").SetStrokeWidth(2)
            .BeginPath().Rectangle(0, 0, 20, 20).FillStroke();
        SvgNode nestedDrawing = canvas.LastNode;
        canvas.Restore()
            .BeginPath().MoveTo(0, 0).LineTo(20, 20).Stroke();
        SvgNode restoredDrawing = canvas.LastNode;

        SvgNode definitions = canvas.Builder.Root.Children.Single(item => item.ElementKind == SvgElementKind.Defs);
        SvgNode secondClipPath = secondClip.Children.Single();

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Succeeded(), Is.True, canvas.Message);
            Assert.That(definitions.Children, Is.EqualTo(new[] { firstClip, secondClip }));
            Assert.That(Attribute(firstClip, "id"), Is.EqualTo("svg-clip-1"));
            Assert.That(Attribute(secondClip, "id"), Is.EqualTo("svg-clip-2"));
            Assert.That(Attribute(secondClipPath, "clip-path"), Is.EqualTo("url(#svg-clip-1)"));
            Assert.That(Attribute(secondClipPath, "transform"), Is.EqualTo("matrix(1 0 0 1 5 0)"));
            Assert.That(Attribute(nestedDrawing, "clip-path"), Is.EqualTo("url(#svg-clip-2)"));
            Assert.That(Attribute(nestedDrawing, "fill"), Is.EqualTo(PaintText("orange")));
            Assert.That(Attribute(nestedDrawing, "stroke"), Is.EqualTo(PaintText("black")));
            Assert.That(Attribute(nestedDrawing, "stroke-width"), Is.EqualTo("2"));
            Assert.That(Attribute(restoredDrawing, "clip-path"), Is.EqualTo("url(#svg-clip-1)"));
            Assert.That(Attribute(restoredDrawing, "fill"), Is.EqualTo("none"));
            Assert.That(Attribute(restoredDrawing, "stroke"), Is.EqualTo("black"));
            Assert.That(Attribute(restoredDrawing, "transform"), Is.Null);
        });
    }

    [Test]
    public void DrawsNativeImagesAndTextInsideOpacityLayers()
    {
        SvgCanvas canvas = SvgCanvas.CreateWithViewport(40, 30);
        canvas.BeginNamedLayer("overlay", .4)
            .Translate(2, 3).SetOpacity(.8)
            .DrawImageWithAspectRatio("data:image/png;base64,AA==", 0, 0, 10, 8, "none")
            .SetFont("serif", 14).SetFontStyle("italic").SetFontWeight("bold").SetTextAnchor("middle")
            .SetFill("green").FillText("A < B", 10, 20)
            .SetStroke("purple").SetStrokeWidth(2).StrokeText("outline", 10, 25)
            .EndLayer();

        SvgNode layer = canvas.Builder.Document.Index.GetById("overlay");
        SvgNode image = layer.Children[0];
        SvgNode fillText = layer.Children[1];
        SvgNode strokeText = layer.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Succeeded(), Is.True, canvas.Message);
            Assert.That(canvas.GetLayerDepth(), Is.Zero);
            Assert.That(layer.ElementKind, Is.EqualTo(SvgElementKind.G));
            Assert.That(Attribute(layer, "opacity"), Is.EqualTo("0.4"));
            Assert.That(image.ElementKind, Is.EqualTo(SvgElementKind.Image));
            Assert.That(Attribute(image, "href"), Is.EqualTo("data:image/png;base64,AA=="));
            Assert.That(Attribute(image, "preserveAspectRatio"), Is.EqualTo("none"));
            Assert.That(Attribute(image, "opacity"), Is.EqualTo("0.8"));
            Assert.That(Attribute(image, "transform"), Is.EqualTo("matrix(1 0 0 1 2 3)"));
            Assert.That(fillText.ElementKind, Is.EqualTo(SvgElementKind.Text));
            Assert.That(new SvgNodeApi().GetTextContent(fillText), Is.EqualTo("A < B"));
            Assert.That(Attribute(fillText, "font-family"), Is.EqualTo("serif"));
            Assert.That(Attribute(fillText, "font-size"), Is.EqualTo("14"));
            Assert.That(Attribute(fillText, "font-style"), Is.EqualTo("italic"));
            Assert.That(Attribute(fillText, "font-weight"), Is.EqualTo("bold"));
            Assert.That(Attribute(fillText, "text-anchor"), Is.EqualTo("middle"));
            Assert.That(Attribute(fillText, "fill"), Is.EqualTo(PaintText("green")));
            Assert.That(Attribute(strokeText, "fill"), Is.EqualTo("none"));
            Assert.That(Attribute(strokeText, "stroke"), Is.EqualTo(PaintText("purple")));
            Assert.That(Attribute(strokeText, "stroke-width"), Is.EqualTo("2"));
        });
    }

    [Test]
    public void RecordsDefinitionsWithIsolatedCanvasStateAndUsesThemAsPaint()
    {
        SvgCanvas canvas = SvgCanvas.CreateWithViewport(30, 20);
        canvas.SetFill("red").BeginPath().MoveTo(0, 0);
        canvas.BeginDefinition(SvgElementKind.Pattern)
            .SetDefinitionAttribute("patternUnits", "userSpaceOnUse")
            .SetDefinitionAttribute("x", "0")
            .SetDefinitionAttribute("y", "0")
            .SetDefinitionAttribute("width", "4")
            .SetDefinitionAttribute("height", "4")
            .SetFill("gold").BeginPath().Rectangle(0, 0, 2, 4).Fill()
            .EndDefinition();
        SvgNode pattern = canvas.LastDefinition;
        canvas.LineTo(5, 5).Stroke();
        SvgNode restoredPath = canvas.LastNode;
        canvas.UseFillDefinition(pattern).BeginPath().Rectangle(0, 0, 30, 20).Fill();
        SvgNode patternedPath = canvas.LastNode;

        SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
        options.TargetBounds = SvgRect.Create(0, 0, 30, 20);
        SvgPaintServerResult resolved = new SvgPaintServerResolver().ResolveWithNodeAndOptions(pattern, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Succeeded(), Is.True, canvas.Message);
            Assert.That(Attribute(pattern, "id"), Is.EqualTo("svg-pattern-1"));
            Assert.That(pattern.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Path));
            Assert.That(Attribute(pattern.Children.Single(), "fill"), Is.EqualTo(PaintText("gold")));
            Assert.That(Attribute(restoredPath, "stroke"), Is.EqualTo("black"));
            Assert.That(Parse(Attribute(restoredPath, "d")).Commands, Has.Count.EqualTo(2));
            Assert.That(Attribute(patternedPath, "fill"), Is.EqualTo("url(#svg-pattern-1)"));
            Assert.That(canvas.Builder.Current, Is.SameAs(canvas.RecordingRoot));
            Assert.That(resolved.Success, Is.True, Diagnostics(resolved.Diagnostics));
            Assert.That(resolved.Renderable, Is.True);
            Assert.That(resolved.Content.Single(), Is.SameAs(pattern.Children.Single()));
        });
    }

    [Test]
    public void ReportsStickyLimitsUnbalancedStateAndPathOrTransformFailures()
    {
        SvgCanvas canvas = new SvgCanvas();
        canvas.MaximumSaveDepth = 1;
        canvas.Save().Save().BeginPath().Rectangle(0, 0, 2, 2).Fill();
        Assert.Multiple(() =>
        {
            Assert.That(canvas.Error, Is.EqualTo(SvgCanvasError.SaveLimitExceeded));
            Assert.That(canvas.GetSaveDepth(), Is.EqualTo(1));
            Assert.That(canvas.Builder.Root.Children, Is.Empty);
        });

        canvas.ClearError().Restore().Restore();
        Assert.That(canvas.Error, Is.EqualTo(SvgCanvasError.InvalidState));

        canvas.ClearError().BeginPath().LineTo(1, 1);
        Assert.Multiple(() =>
        {
            Assert.That(canvas.Error, Is.EqualTo(SvgCanvasError.PathFailed));
            Assert.That(canvas.PathError, Is.EqualTo(SvgPathBuilderError.ExpectedMoveTo));
        });

        SvgMatrix invalid = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalid.A = double.NaN;
        canvas.ClearError().SetTransform(invalid);
        Assert.Multiple(() =>
        {
            Assert.That(canvas.Error, Is.EqualTo(SvgCanvasError.TransformFailed));
            Assert.That(canvas.GeometryError, Is.EqualTo(SvgGeometryError.InvalidArgument));
        });

        SvgCanvas layers = new SvgCanvas();
        layers.MaximumLayerDepth = 1;
        layers.BeginLayer().BeginLayer();
        Assert.Multiple(() =>
        {
            Assert.That(layers.Error, Is.EqualTo(SvgCanvasError.LayerLimitExceeded));
            Assert.That(layers.GetLayerDepth(), Is.EqualTo(1));
        });

        SvgCanvas missing = SvgCanvas.CreateForBuilder((SvgBuilder)null);
        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgCanvasError.InvalidArgument));
            Assert.That(missing.Build(), Is.Null);
        });
    }

    static SvgPathData Parse(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 1000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static string Attribute(SvgNode element, string name)
    {
        SvgAttribute attribute = element.Attributes.SingleOrDefault(item => item.NamespaceUri.Length == 0 && item.LocalName == name);
        return attribute == null ? null : attribute.Value;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Message));
    }

    static string PaintText(string value)
    {
        SvgPaintParseResult result = new SvgPaintParser().Parse(value, 8, 128, false);
        Assert.That(result.Success, Is.True);
        return result.Paint.ToText();
    }
}
