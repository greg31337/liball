namespace svg.Tests;

public class SvgDocumentTransformJobTests
{
    [Test]
    public void RecolorRewritesPresentationInlineAndStylesheetColors()
    {
        SvgRecolorProcessOptions settings = new SvgRecolorProcessOptions();
        settings.ColorProvider = color => Same(color, "red") ? ParseColor("blue") : color;
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>.a{fill:red;stroke:#008000}</style><rect id='a' fill='red' style='stroke:red;stop-color:green'/></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().RecolorWithOptions(settings));
        SvgProcessResult second = Process(document, new SvgProcessJobs().RecolorWithOptions(settings));
        SvgNode rect = document.Index.GetById("a");
        string sheet = new SvgNodeApi().GetTextContent(document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Style));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(Attribute(rect, "fill"), Is.EqualTo("color(srgb 0 0 1)"));
            Assert.That(Attribute(rect, "style"), Does.Contain("stroke:color(srgb 0 0 1)"));
            Assert.That(Attribute(rect, "style"), Does.Contain("stop-color:green"));
            Assert.That(sheet, Does.Contain("fill:color(srgb 0 0 1)"));
            Assert.That(sheet, Does.Contain("stroke:#008000"));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Destructive));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void ThemeConversionSupportsCurrentColorAndCustomPropertyFallbacks()
    {
        SvgThemeColorProcessOptions custom = new SvgThemeColorProcessOptions();
        custom.Mode = SvgThemeColorMode.CustomProperty;
        custom.CustomPropertyName = "--accent";
        custom.SourceColor = ParseColor("red");
        SvgDocument customDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><path id='p' fill='#f00' stroke='blue' style='flood-color:red'/></svg>");
        SvgProcessResult customResult = Process(customDocument, new SvgProcessJobs().ConvertThemeColors(custom));
        SvgNode path = customDocument.Index.GetById("p");

        SvgDocument currentDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><circle id='c' fill='red' stroke='blue'/></svg>");
        SvgProcessResult currentResult = Process(currentDocument, new SvgProcessJobs().ConvertColorsToCurrentColor());
        SvgNode circle = currentDocument.Index.GetById("c");

        Assert.Multiple(() =>
        {
            Assert.That(customResult.Success, Is.True, Messages(customResult));
            Assert.That(Attribute(path, "fill"), Is.EqualTo("var(--accent, color(srgb 1 0 0))"));
            Assert.That(Attribute(path, "stroke"), Is.EqualTo("blue"));
            Assert.That(Attribute(path, "style"), Does.Contain("flood-color:var(--accent, color(srgb 1 0 0))"));
            Assert.That(customResult.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Representation));
            Assert.That(currentResult.Success, Is.True, Messages(currentResult));
            Assert.That(Attribute(circle, "fill"), Is.EqualTo("currentColor"));
            Assert.That(Attribute(circle, "stroke"), Is.EqualTo("currentColor"));
        });
    }

    [Test]
    public void ViewBoxJobResizesFitsAndCropsWithConfiguredAlignment()
    {
        SvgViewBoxResizeOptions resize = new SvgViewBoxResizeOptions();
        resize.Mode = SvgViewBoxResizeMode.ResizeViewport;
        resize.ViewportWidth = 640;
        resize.ViewportHeight = 480;
        SvgDocument resized = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'/>");
        SvgProcessResult resizeResult = Process(resized, new SvgProcessJobs().ResizeViewBoxWithOptions(resize));

        SvgViewBoxResizeOptions fit = new SvgViewBoxResizeOptions();
        fit.Mode = SvgViewBoxResizeMode.FitViewBox;
        fit.TargetAspectRatio = 2;
        SvgDocument fitted = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'/>");
        SvgProcessResult fitResult = Process(fitted, new SvgProcessJobs().ResizeViewBoxWithOptions(fit));

        SvgViewBoxResizeOptions crop = new SvgViewBoxResizeOptions();
        crop.Mode = SvgViewBoxResizeMode.CropViewBox;
        crop.TargetAspectRatio = 2;
        SvgDocument cropped = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'/>");
        SvgProcessResult cropResult = Process(cropped, new SvgProcessJobs().ResizeViewBoxWithOptions(crop));

        Assert.Multiple(() =>
        {
            Assert.That(resizeResult.Success, Is.True, Messages(resizeResult));
            Assert.That(Attribute(resized.DocumentElement, "width"), Is.EqualTo("640"));
            Assert.That(Attribute(resized.DocumentElement, "height"), Is.EqualTo("480"));
            Assert.That(Attribute(resized.DocumentElement, "viewBox"), Is.EqualTo("0 0 100 100"));
            Assert.That(fitResult.Success, Is.True, Messages(fitResult));
            Assert.That(Attribute(fitted.DocumentElement, "viewBox"), Is.EqualTo("-50 0 200 100"));
            Assert.That(cropResult.Success, Is.True, Messages(cropResult));
            Assert.That(Attribute(cropped.DocumentElement, "viewBox"), Is.EqualTo("0 25 100 50"));
            Assert.That(new[] { resizeResult, fitResult, cropResult }.SelectMany(item => item.Changes), Has.All.Matches<SvgProcessChange>(item => item.Severity == SvgProcessChangeSeverity.Destructive));
        });
    }

    [Test]
    public void CoordinateConversionWrapsOnlyGraphicalRootContentAndIsIdempotent()
    {
        SvgCoordinateSystemProcessOptions settings = new SvgCoordinateSystemProcessOptions();
        settings.Transform = new SvgMatrix(1, 0, 0, -1, 0, 100);
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><title>Chart</title><defs><path id='definition' d='M0 0'/></defs><path id='shape' d='M10 20L30 40'/></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().ConvertCoordinateSystemWithOptions(settings));
        SvgProcessResult second = Process(document, new SvgProcessJobs().ConvertCoordinateSystemWithOptions(settings));
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children.Single(item => item.ElementKind == SvgElementKind.G);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(group.Attributes.Single(item => item.LocalName == "transform").Value, Is.EqualTo("matrix(1 0 0 -1 0 100)"));
            Assert.That(group.Children.Single().Attributes.Single(item => item.LocalName == "id").Value, Is.EqualTo("shape"));
            Assert.That(root.Children.Any(item => item.ElementKind == SvgElementKind.Title), Is.True);
            Assert.That(root.Children.Any(item => item.ElementKind == SvgElementKind.Defs), Is.True);
            Assert.That(Attribute(root, "viewBox"), Is.EqualTo("0 0 100 100"));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Destructive));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void DrawingBoundsCropUsesResolvedFillBoundsAndPadding()
    {
        SvgDrawingBoundsCropOptions settings = new SvgDrawingBoundsCropOptions();
        settings.BoundsOptions.IncludeComputedStroke = false;
        settings.HorizontalPadding = 2;
        settings.VerticalPadding = 3;
        settings.ResizeViewport = true;
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'><rect x='10' y='20' width='30' height='40'/></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().CropToDrawingBoundsWithOptions(settings));
        SvgProcessResult second = Process(document, new SvgProcessJobs().CropToDrawingBoundsWithOptions(settings));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(Attribute(document.DocumentElement, "viewBox"), Is.EqualTo("8 17 34 46"));
            Assert.That(Attribute(document.DocumentElement, "width"), Is.EqualTo("34"));
            Assert.That(Attribute(document.DocumentElement, "height"), Is.EqualTo("46"));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Destructive));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    static SvgColor ParseColor(string value)
    {
        SvgColorParseResult result = new SvgColorParser().Parse(value, 128);
        Assert.That(result.Success, Is.True);
        return result.Color;
    }

    static bool Same(SvgColor color, string value)
    {
        SvgColor target = ParseColor(value);
        SvgColorOperationResult first = color.ConvertTo(SvgColorSpace.Srgb);
        SvgColorOperationResult second = target.ConvertTo(SvgColorSpace.Srgb);
        return first.Success && second.Success && first.Color.Red == second.Color.Red && first.Color.Green == second.Color.Green && first.Color.Blue == second.Color.Blue && first.Color.Alpha == second.Color.Alpha;
    }

    static SvgProcessResult Process(SvgDocument document, SvgProcessJob job)
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(job);
        return document.ProcessWithOptions(options);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Attribute(SvgNode node, string name)
    {
        return node.Attributes.Single(item => item.LocalName == name && item.NamespaceUri.Length == 0).Value;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
