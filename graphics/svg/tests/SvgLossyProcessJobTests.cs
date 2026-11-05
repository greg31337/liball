namespace svg.Tests;

public class SvgLossyProcessJobTests
{
    [Test]
    public void ShapeConversionPreservesCommonContentAndRecordsRepresentationSeverity()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='box' x='1' y='2' width='10' height='5' rx='1' fill='red'><title>Box</title></rect></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().ConvertShapesToPathsWithNoArguments());
        SvgProcessResult second = Process(document, new SvgProcessJobs().ConvertShapesToPathsWithNoArguments());
        SvgNode path = document.Index.GetById("box");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(path.ElementKind, Is.EqualTo(SvgElementKind.Path));
            Assert.That(Attribute(path, "fill"), Is.EqualTo("red"));
            Assert.That(path.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Title));
            Assert.That(path.Attributes.Any(item => item.LocalName == "x" || item.LocalName == "width" || item.LocalName == "rx"), Is.False);
            Assert.That(new SvgPathParser().Parse(Attribute(path, "d"), 32, 64).Success, Is.True);
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Representation));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void TransformBakingAndSimplificationUseBoundedGeometrySeverity()
    {
        SvgDocument baked = Load("<svg xmlns='http://www.w3.org/2000/svg'><path id='p' d='M1 2L3 4' transform='translate(10 20)'/></svg>");
        SvgProcessResult bakeResult = Process(baked, new SvgProcessJobs().BakeTransformsWithNoArguments());
        SvgNode bakedPath = baked.Index.GetById("p");
        SvgPathParseResult bakedData = new SvgPathParser().Parse(Attribute(bakedPath, "d"), 8, 64);

        SvgPathSimplifyOptions settings = new SvgPathSimplifyOptions();
        settings.Tolerance = 0.1;
        settings.DecimalPlaces = 2;
        string originalPath = "M0.001 0.001Q1 2 2.0001 0.001L3.0001 0.02L4.0001 0.001";
        SvgPathSimplifyResult expectedSimplification = ParsePath(originalPath).SimplifyWithOptions(settings);
        SvgPathFormatResult expectedPath = new SvgPathFormatter().FormatMinifiedWithPath(expectedSimplification.PathData);
        SvgDocument simplified = Load("<svg xmlns='http://www.w3.org/2000/svg'><path id='q' d='" + originalPath + "'/></svg>");
        SvgProcessResult simplifyResult = Process(simplified, new SvgProcessJobs().SimplifyPathsWithOptions(settings));

        Assert.Multiple(() =>
        {
            Assert.That(bakeResult.Success, Is.True, Messages(bakeResult));
            Assert.That(bakedPath.Attributes.Any(item => item.LocalName == "transform"), Is.False);
            Assert.That(bakedData.Success, Is.True);
            Assert.That(bakedData.PathData.Commands[0].EndX, Is.EqualTo(11));
            Assert.That(bakedData.PathData.Commands[0].EndY, Is.EqualTo(22));
            Assert.That(bakedData.PathData.Commands[1].EndX, Is.EqualTo(13));
            Assert.That(bakedData.PathData.Commands[1].EndY, Is.EqualTo(24));
            Assert.That(bakeResult.Changes, Has.All.Matches<SvgProcessChange>(item => item.Severity == SvgProcessChangeSeverity.Approximation));
            Assert.That(expectedSimplification.Success, Is.True);
            Assert.That(expectedSimplification.Report.IsLossy, Is.True);
            Assert.That(expectedSimplification.Report.MaximumErrorBound, Is.LessThanOrEqualTo(settings.Tolerance));
            Assert.That(expectedPath.Success, Is.True);
            Assert.That(simplifyResult.Success, Is.True, Messages(simplifyResult));
            Assert.That(Attribute(simplified.Index.GetById("q"), "d"), Is.EqualTo(expectedPath.Text));
            Assert.That(simplifyResult.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Approximation));
        });
    }

    [Test]
    public void TextAndEffectProvidersCreateAccessibleDerivedOutputWithDestructiveSeverity()
    {
        SvgTextToPathProcessOptions textOptions = new SvgTextToPathProcessOptions();
        textOptions.OutlineProvider = _ => ParsePath("M0 0L2 0L2 1Z");
        SvgDocument text = Load("<svg xmlns='http://www.w3.org/2000/svg'><text id='label' x='3' fill='blue'>Hello</text></svg>");
        SvgProcessResult textResult = Process(text, new SvgProcessJobs().ConvertTextToPathsWithOptions(textOptions));
        SvgNode outlined = text.Index.GetById("label");

        SvgFlattenEffectsProcessOptions effectOptions = new SvgFlattenEffectsProcessOptions();
        effectOptions.ImageProvider = _ => new SvgFlattenedEffectImage("data:image/png;base64,AA==", 1, 2, 3, 4);
        SvgDocument effect = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><filter id='f'/></defs><rect id='shape' filter='url(#f)' aria-label='Shape'/></svg>");
        SvgProcessResult effectResult = Process(effect, new SvgProcessJobs().FlattenEffectsWithOptions(effectOptions));
        SvgNode image = effect.Index.GetById("shape");

        Assert.Multiple(() =>
        {
            Assert.That(textResult.Success, Is.True, Messages(textResult));
            Assert.That(outlined.ElementKind, Is.EqualTo(SvgElementKind.Path));
            Assert.That(Attribute(outlined, "fill"), Is.EqualTo("blue"));
            Assert.That(Attribute(outlined, "aria-label"), Is.EqualTo("Hello"));
            Assert.That(outlined.Attributes.Any(item => item.LocalName == "x"), Is.False);
            Assert.That(textResult.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Destructive));
            Assert.That(effectResult.Success, Is.True, Messages(effectResult));
            Assert.That(image.ElementKind, Is.EqualTo(SvgElementKind.Image));
            Assert.That(Attribute(image, "href"), Is.EqualTo("data:image/png;base64,AA=="));
            Assert.That(Attribute(image, "x"), Is.EqualTo("1"));
            Assert.That(Attribute(image, "aria-label"), Is.EqualTo("Shape"));
            Assert.That(effectResult.Changes, Has.All.Matches<SvgProcessChange>(item => item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Destructive));
        });
    }

    [Test]
    public void ImageEmbeddingRewritesBothHrefFormsWithoutSemanticLoss()
    {
        SvgEmbedImagesProcessOptions settings = new SvgEmbedImagesProcessOptions();
        settings.ImageProvider = _ => new SvgEmbeddedImageData(new byte[] { 1, 2, 3 }, "image/png");
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><image id='image' href='asset.png' xlink:href='asset.png'/></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().EmbedImagesWithOptions(settings));
        SvgProcessResult second = Process(document, new SvgProcessJobs().EmbedImagesWithOptions(settings));
        SvgNode image = document.Index.GetById("image");
        SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValue(Attribute(image, "href"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(Attribute(image, "href", SvgNamespaces.XLink), Is.EqualTo(Attribute(image, "href")));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => !item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Representation));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void LossyJobsAreRejectedBySafePolicyBeforeCallbacksRun()
    {
        int calls = 0;
        SvgTextToPathProcessOptions settings = new SvgTextToPathProcessOptions();
        settings.OutlineProvider = _ =>
        {
            calls = calls + 1;
            return ParsePath("M0 0Z");
        };
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>x</text></svg>");
        SvgProcessOptions options = new SvgProcessOptions();
        options.AllowSemanticLoss = false;
        options.Jobs.Add(new SvgProcessJobs().ConvertTextToPathsWithOptions(settings));

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(SvgProcessError.SemanticLossDenied));
            Assert.That(calls, Is.Zero);
            Assert.That(document.DocumentElement.Children.Single().ElementKind, Is.EqualTo(SvgElementKind.Text));
        });
    }

    static SvgPathData ParsePath(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 100, 64);
        Assert.That(result.Success, Is.True);
        return result.PathData;
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
        return Attribute(node, name, "");
    }

    static string Attribute(SvgNode node, string name, string namespaceUri)
    {
        return node.Attributes.Single(item => item.LocalName == name && item.NamespaceUri == namespaceUri).Value;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
