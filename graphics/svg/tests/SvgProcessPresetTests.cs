namespace svg.Tests;

public class SvgProcessPresetTests
{
    [Test]
    public void SafeDefaultPreservesInternalRendererPixelsAndIsIdempotent()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='4'><defs><path id='unused' d='M0 0L1 1'/></defs><!--remove--><g><rect width='8' height='4' fill='red'/></g></svg>";
        SvgDocument original = Load(source);
        SvgDocument processed = Load(source);
        SvgRenderOptions renderOptions = new SvgRenderOptions();
        renderOptions.ViewportWidth = 8;
        renderOptions.ViewportHeight = 4;
        renderOptions.SamplesPerAxis = 1;
        SvgRenderResult before = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(original, renderOptions);

        SvgProcessResult first = processed.ProcessWithOptions(new SvgProcessPresets().SafeDefault());
        SvgRenderResult after = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(processed, renderOptions);
        SvgProcessResult second = processed.ProcessWithOptions(new SvgProcessPresets().SafeDefault());

        Assert.Multiple(() =>
        {
            Assert.That(before.Success, Is.True, RenderMessages(before));
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(first.Changed, Is.True);
            Assert.That(after.Success, Is.True, RenderMessages(after));
            AssertSamePixels(before.Pixels, after.Pixels);
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void EveryNamedPresetPreservesAccessibilityAndViewBoxByDefault()
    {
        SvgProcessPresetKind[] kinds = new[]
        {
            SvgProcessPresetKind.SafeDefault,
            SvgProcessPresetKind.Canonicalize,
            SvgProcessPresetKind.Aggressive,
            SvgProcessPresetKind.Sprite,
            SvgProcessPresetKind.Sanitize
        };
        List<SvgProcessOptions> presets = kinds.Select(kind => new SvgProcessPresets().Create(kind)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(presets.Select(item => item.Preset), Is.EqualTo(kinds));
            Assert.That(presets, Has.All.Matches<SvgProcessOptions>(item => item.PreserveAccessibility));
            Assert.That(presets, Has.All.Matches<SvgProcessOptions>(item => item.PreserveViewBox));
            Assert.That(presets, Has.All.Matches<SvgProcessOptions>(item => item.Operations.Count > 0));
            Assert.That(presets, Has.All.Matches<SvgProcessOptions>(item => item.Operations.Distinct().Count() == item.Operations.Count));
            Assert.That(presets.Single(item => item.Preset == SvgProcessPresetKind.Aggressive).AllowSemanticLoss, Is.True);
            Assert.That(presets.Where(item => item.Preset != SvgProcessPresetKind.Aggressive), Has.All.Matches<SvgProcessOptions>(item => !item.AllowSemanticLoss));
        });
    }

    [Test]
    public void PresetOperationPlansExpressTheirDistinctPurposesInStableOrder()
    {
        SvgProcessOptions safe = new SvgProcessPresets().SafeDefault();
        SvgProcessOptions canonical = new SvgProcessPresets().Canonicalize();
        SvgProcessOptions aggressive = new SvgProcessPresets().Aggressive();
        SvgProcessOptions sprite = new SvgProcessPresets().Sprite();
        SvgProcessOptions sanitize = new SvgProcessPresets().Sanitize();

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Operations.Take(safe.Operations.Count), Is.EqualTo(safe.Operations));
            Assert.That(canonical.Operations.Last(), Is.EqualTo(SvgProcessOperation.CanonicalizeValues));
            Assert.That(aggressive.Operations.Take(safe.Operations.Count), Is.EqualTo(safe.Operations));
            Assert.That(aggressive.Operations, Does.Contain(SvgProcessOperation.SimplifyPaths));
            Assert.That(aggressive.Operations, Does.Contain(SvgProcessOperation.ConvertTextToPaths));
            Assert.That(aggressive.Operations, Does.Contain(SvgProcessOperation.FlattenEffects));
            Assert.That(sprite.Operations, Does.Contain(SvgProcessOperation.PrefixIds));
            Assert.That(sprite.Operations, Does.Contain(SvgProcessOperation.BuildSprite));
            Assert.That(sprite.Operations, Does.Not.Contain(SvgProcessOperation.MinifyIds));
            Assert.That(sanitize.Operations.First(), Is.EqualTo(SvgProcessOperation.Sanitize));
            Assert.That(sanitize.Operations, Does.Contain(SvgProcessOperation.RemoveUnusedNamespaces));
            Assert.That(sanitize.Operations, Does.Not.Contain(SvgProcessOperation.CanonicalizeValues));
            Assert.That(aggressive.MaximumPasses, Is.GreaterThan(safe.MaximumPasses));
            Assert.That(sprite.MaximumPasses, Is.GreaterThan(safe.MaximumPasses));
        });
    }

    [Test]
    public void FactoriesReturnIndependentConfigurationsAndRejectNonPresetKinds()
    {
        SvgProcessOptions first = new SvgProcessPresets().SafeDefault();
        SvgProcessOptions second = new SvgProcessPresets().SafeDefault();
        int expectedOperations = second.Operations.Count;
        int expectedJobs = second.Jobs.Count;
        first.Operations.Clear();
        first.Jobs.Clear();
        first.MaximumPasses = 1;

        Assert.Multiple(() =>
        {
            Assert.That(second.Operations, Has.Count.EqualTo(expectedOperations));
            Assert.That(second.Jobs, Has.Count.EqualTo(expectedJobs));
            Assert.That(second.MaximumPasses, Is.EqualTo(16));
            Assert.That(first.Operations, Is.Not.SameAs(second.Operations));
            Assert.That(first.Jobs, Is.Not.SameAs(second.Jobs));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgProcessPresets().Create(SvgProcessPresetKind.Custom));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgProcessPresets().Create((SvgProcessPresetKind)99));
        });
    }

    [Test]
    public void PresetsRejectDeclaredSemanticLossAndPreservationViolationsBeforeCallbacksRun()
    {
        int callbacks = 0;
        SvgProcessJob lossy = PreservingJob("lossy", _ => callbacks++);
        lossy.SemanticLoss = true;
        SvgProcessOptions safeOptions = new SvgProcessPresets().SafeDefault();
        safeOptions.Jobs.Add(lossy);

        SvgProcessJob accessibility = new SvgProcessJob("accessibility", _ => callbacks++);
        accessibility.PreservesViewBox = true;
        SvgProcessOptions accessibilityOptions = new SvgProcessPresets().Canonicalize();
        accessibilityOptions.Jobs.Add(accessibility);

        SvgProcessJob viewBox = new SvgProcessJob("viewbox", _ => callbacks++);
        viewBox.PreservesAccessibility = true;
        SvgProcessOptions viewBoxOptions = new SvgProcessPresets().Sprite();
        viewBoxOptions.Jobs.Add(viewBox);

        SvgProcessResult lossResult = Load(SemanticSource()).ProcessWithOptions(safeOptions);
        SvgProcessResult accessibilityResult = Load(SemanticSource()).ProcessWithOptions(accessibilityOptions);
        SvgProcessResult viewBoxResult = Load(SemanticSource()).ProcessWithOptions(viewBoxOptions);

        Assert.Multiple(() =>
        {
            Assert.That(lossResult.Error, Is.EqualTo(SvgProcessError.SemanticLossDenied));
            Assert.That(accessibilityResult.Error, Is.EqualTo(SvgProcessError.PreservationRequired));
            Assert.That(viewBoxResult.Error, Is.EqualTo(SvgProcessError.PreservationRequired));
            Assert.That(callbacks, Is.Zero);
            Assert.That(new[] { lossResult, accessibilityResult, viewBoxResult }, Has.All.Matches<SvgProcessResult>(item => !item.Success && item.Runs.Count == 0 && item.Changes.Count == 0));
        });
    }

    [Test]
    public void AggressivePresetRunsExplicitlyLossyJobsWhileStillRequiringProtectedSemantics()
    {
        SvgDocument document = Load(SemanticSource());
        SvgProcessJob removeData = PreservingJob("remove-data", context =>
        {
            SvgNode shape = context.Document.DocumentElement.Children.Single(item => item.Attributes.Any(attribute => attribute.LocalName == "aria-label" && attribute.Value == "Shape"));
            SvgAttribute attribute = shape.Attributes.FirstOrDefault(item => item.LocalName == "fill");
            if (attribute != null) context.Mutations.RemoveAttribute(shape, attribute);
        });
        removeData.SemanticLoss = true;
        removeData.Reason = "Remove optional application data.";
        SvgProcessOptions options = new SvgProcessPresets().Aggressive();
        options.Jobs.Add(removeData);

        SvgProcessResult result = document.ProcessWithOptions(options);
        SvgProcessChange loss = result.Changes.Single(change => change.JobId == "remove-data");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Converged, Is.True);
            Assert.That(loss.SemanticLoss, Is.True);
            Assert.That(loss.Reason, Is.EqualTo("Remove optional application data."));
            Assert.That(document.DocumentElement.Children.All(item => item.Attributes.All(attribute => attribute.LocalName != "fill")), Is.True);
            AssertProtectedContent(document);
        });
    }

    [Test]
    public void DynamicSemanticLossCannotBypassAConservativePreset()
    {
        SvgDocument document = Load(SemanticSource());
        SvgProcessJob job = PreservingJob("dynamic-loss", context =>
        {
            SvgNode shape = context.Document.Index.GetById("shape");
            SvgAttribute attribute = shape.Attributes.Single(item => item.LocalName == "data-extra");
            context.Mutations.RemoveAttribute(shape, attribute);
            context.SemanticLoss = true;
        });
        SvgProcessOptions options = new SvgProcessPresets().SafeDefault();
        options.Jobs.Add(job);

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(SvgProcessError.SemanticLossDenied));
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.Changes.Any(change => change.JobId == "dynamic-loss"), Is.True);
            Assert.That(document.Index.GetById("shape").Attributes.Any(item => item.LocalName == "data-extra"), Is.True);
            Assert.That(document.Version, Is.Zero);
            AssertProtectedContent(document);
        });
    }

    [TestCase(SvgProcessPresetKind.SafeDefault)]
    [TestCase(SvgProcessPresetKind.Canonicalize)]
    [TestCase(SvgProcessPresetKind.Aggressive)]
    [TestCase(SvgProcessPresetKind.Sprite)]
    [TestCase(SvgProcessPresetKind.Sanitize)]
    public void EveryPresetRetainsProtectedContentDuringDeclaredSafeJobs(SvgProcessPresetKind kind)
    {
        SvgDocument document = Load(SemanticSource());
        SvgProcessJob cleanup = PreservingJob("cleanup", context =>
        {
            SvgNode comment = context.Document.DocumentElement.Children.FirstOrDefault(item => item.Kind == SvgNodeKind.Comment);
            if (comment != null) context.Mutations.RemoveChild(comment.Parent, comment);
        });
        SvgProcessOptions options = new SvgProcessPresets().Create(kind);
        options.Jobs.Add(cleanup);

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.DocumentElement.Children.Any(item => item.Kind == SvgNodeKind.Comment), Is.False);
            AssertProtectedContent(document);
        });
    }

    static SvgProcessJob PreservingJob(string id, Action<SvgProcessContext> callback)
    {
        SvgProcessJob result = new SvgProcessJob(id, callback);
        result.PreservesAccessibility = true;
        result.PreservesViewBox = true;
        return result;
    }

    static void AssertProtectedContent(SvgDocument document)
    {
        SvgNode root = document.DocumentElement;
        Assert.That(root.Attributes.Single(item => item.LocalName == SvgAttributeNames.ViewBox).Value, Is.EqualTo("0 0 20 10"));
        Assert.That(root.Attributes.Single(item => item.LocalName == "role").Value, Is.EqualTo("graphics-document"));
        Assert.That(root.Children.Single(item => item.ElementKind == SvgElementKind.Title).Children.Single().Value, Is.EqualTo("Chart"));
        Assert.That(root.Children.Single(item => item.ElementKind == SvgElementKind.Desc).Children.Single().Value, Is.EqualTo("Description"));
        Assert.That(AllNodes(root).Single(item => item.Attributes.Any(attribute => attribute.LocalName == "aria-label" && attribute.Value == "Shape")).Attributes.Single(item => item.LocalName == "aria-label").Value, Is.EqualTo("Shape"));
    }

    static List<SvgNode> AllNodes(SvgNode root)
    {
        List<SvgNode> result = new List<SvgNode>();
        List<SvgNode> pending = new List<SvgNode> { root };
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            pending.AddRange(node.Children);
        }
        return result;
    }

    static string SemanticSource()
    {
        return "<svg xmlns='http://www.w3.org/2000/svg' role='graphics-document' viewBox='0 0 20 10'><title>Chart</title><desc>Description</desc><!--remove--><rect id='shape' aria-label='Shape' data-extra='value' fill='red'/></svg>";
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }

    static string RenderMessages(SvgRenderResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }

    static void AssertSamePixels(SvgPixelBuffer expected, SvgPixelBuffer actual)
    {
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        int y = 0;
        while (y < expected.Height)
        {
            int x = 0;
            while (x < expected.Width)
            {
                SvgPremultipliedRgba8 left = expected.Read(x, y).Pixel;
                SvgPremultipliedRgba8 right = actual.Read(x, y).Pixel;
                Assert.That(new[] { right.Red, right.Green, right.Blue, right.Alpha }, Is.EqualTo(new[] { left.Red, left.Green, left.Blue, left.Alpha }), $"pixel {x},{y}");
                x++;
            }
            y++;
        }
    }
}
