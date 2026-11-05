namespace svg.Tests;

public class SvgResolvedDocumentTests
{
    [Test]
    public void ResolvesViewportStylesStructureAndGeometryIntoOneSnapshot()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='120' height='80'><g style='fill:#123456'><rect x='2' y='3' width='10' height='20'/></g></svg>");

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode group = result.FindSource(document.DocumentElement.Children[0]);
        SvgResolvedNode rectangle = result.FindSource(document.DocumentElement.Children[0].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceDocument, Is.SameAs(document));
            Assert.That(result.SourceVersion, Is.EqualTo(document.Version));
            Assert.That(result.Root.SourceNode, Is.SameAs(document.DocumentElement));
            Assert.That(result.Root.SourcePath, Is.EqualTo("0"));
            Assert.That(result.Root.Kind, Is.EqualTo(SvgResolvedNodeKind.Viewport));
            Assert.That(result.Root.Viewport.Width, Is.EqualTo(120));
            Assert.That(result.Root.Viewport.Height, Is.EqualTo(80));
            Assert.That(group.SourcePath, Is.EqualTo("0/0"));
            Assert.That(group.Structural, Is.Not.Null);
            Assert.That(rectangle.SourcePath, Is.EqualTo("0/0/0"));
            Assert.That(rectangle.Kind, Is.EqualTo(SvgResolvedNodeKind.Shape));
            Assert.That(rectangle.Geometry.Kind, Is.EqualTo(SvgShapeKind.Rectangle));
            Assert.That(rectangle.Bounds.X, Is.EqualTo(2));
            Assert.That(rectangle.Bounds.Y, Is.EqualTo(3));
            Assert.That(rectangle.Bounds.Width, Is.EqualTo(10));
            Assert.That(rectangle.Bounds.Height, Is.EqualTo(20));
            Assert.That(rectangle.Fill.Color.Red, Is.EqualTo(0x12 / 255.0).Within(1e-12));
            Assert.That(rectangle.Fill.Color.Green, Is.EqualTo(0x34 / 255.0).Within(1e-12));
            Assert.That(rectangle.Fill.Color.Blue, Is.EqualTo(0x56 / 255.0).Within(1e-12));
            Assert.That(rectangle.Active, Is.True);
            Assert.That(rectangle.Renderable, Is.True);
            Assert.That(result.ComputedStyles.Find(rectangle.SourceNode), Is.SameAs(rectangle.ComputedStyle));
            Assert.That(result.Scene, Is.Null);
            Assert.That(result.FeatureReport, Is.Not.Null);
            Assert.That(result.Resources, Is.Not.Null);
            Assert.That(result.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void AppliesConditionalSwitchSelectionAndStructuralTraversalState()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><rect id='definition' width='1' height='1'/></defs><switch><rect requiredFeatures='urn:missing' width='2' height='2'/><circle r='3'/></switch></svg>");
        SvgResolveOptions options = new SvgResolveOptions();

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, options);
        SvgResolvedNode definitions = result.FindSource(document.DocumentElement.Children[0]);
        SvgResolvedNode definition = result.FindSource(document.DocumentElement.Children[0].Children[0]);
        SvgResolvedNode selection = result.FindSource(document.DocumentElement.Children[1]);
        SvgResolvedNode unsupported = result.FindSource(document.DocumentElement.Children[1].Children[0]);
        SvgResolvedNode fallback = result.FindSource(document.DocumentElement.Children[1].Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(definitions.Structural.RendersChildren, Is.False);
            Assert.That(definition.Active, Is.False);
            Assert.That(definition.Renderable, Is.False);
            Assert.That(selection.Switch.SelectedChild, Is.SameAs(fallback.SourceNode));
            Assert.That(unsupported.Conditional.Matches, Is.False);
            Assert.That(unsupported.Active, Is.False);
            Assert.That(unsupported.Renderable, Is.False);
            Assert.That(fallback.Active, Is.True);
            Assert.That(fallback.Renderable, Is.True);
        });
    }

    [Test]
    public void ExpandsUseWithStableGlobalInstanceAndSourcePathsWithoutDuplicates()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='art'><rect width='10' height='5'/></g><use href='#art'/></svg>");

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode use = result.FindSource(document.DocumentElement.Children[1]);
        SvgResolvedNode root = result.FindInstance("0/1/instance/0");
        SvgResolvedNode rectangle = result.FindInstance("0/1/instance/0/0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(use.Use.Success, Is.True);
            Assert.That(use.Children, Has.Count.EqualTo(1));
            Assert.That(root, Is.SameAs(use.Children[0]));
            Assert.That(root.SourceNode, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(root.SourcePath, Is.EqualTo("0/0"));
            Assert.That(root.InstancePath, Is.EqualTo("0/1/instance/0"));
            Assert.That(root.Children, Has.Count.EqualTo(1));
            Assert.That(rectangle.SourceNode, Is.SameAs(document.DocumentElement.Children[0].Children[0]));
            Assert.That(rectangle.SourcePath, Is.EqualTo("0/0/0"));
            Assert.That(rectangle.InstancePath, Is.EqualTo("0/1/instance/0/0"));
            Assert.That(root.Find(rectangle.SourceNode, rectangle.InstancePath), Is.SameAs(rectangle));
            Assert.That(result.Nodes, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public void RetainsUseFailureSourceAndGloballyQualifiedInstancePath()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><use/></svg>");
        SvgNode source = document.DocumentElement.Children[0];

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode use = result.FindSource(source);
        SvgDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == SvgDiagnosticCode.InvalidReference);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(use.Success, Is.False);
            Assert.That(use.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(use.Diagnostics[0], Is.SameAs(diagnostic));
            Assert.That(diagnostic.Source, Is.SameAs(source.SourceSpan));
            Assert.That(diagnostic.InstancePath, Is.EqualTo("0/0/instance/host"));
        });
    }

    [Test]
    public void ReusesOnlyCurrentDocumentEnvironmentAndOptionSnapshots()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='10'/></svg>");
        SvgResolveOptions options = new SvgResolveOptions();
        SvgDocumentResolver resolver = new SvgDocumentResolver();

        SvgResolvedDocument first = resolver.ResolveWithOptions(document, options);
        SvgResolvedDocument cached = resolver.ResolveWithOptions(document, options);
        options.ViewportWidth = 640;
        SvgResolvedDocument resized = resolver.ResolveWithOptions(document, options);
        options.StyleOptions.StyleEnvironment.MarkChanged();
        SvgResolvedDocument environmentChanged = resolver.ResolveWithOptions(document, options);
        document.DocumentElement.Children[0].MarkDirty();
        SvgResolvedDocument edited = resolver.ResolveWithOptions(document, options);
        SvgResolvedDocumentCacheStatistics statistics = resolver.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(cached, Is.SameAs(first));
            Assert.That(first.IsCurrent(), Is.False);
            Assert.That(resized, Is.Not.SameAs(first));
            Assert.That(environmentChanged, Is.Not.SameAs(resized));
            Assert.That(edited, Is.Not.SameAs(environmentChanged));
            Assert.That(edited.SourceVersion, Is.EqualTo(document.Version));
            Assert.That(edited.IsCurrent(), Is.True);
            Assert.That(statistics.CacheHits, Is.EqualTo(1));
            Assert.That(statistics.CacheMisses, Is.EqualTo(4));
            Assert.That(statistics.CacheEntries, Is.EqualTo(1));
            Assert.That(statistics.InvalidatedEntries, Is.EqualTo(3));
        });
    }

    [Test]
    public void OwnsBoundedCacheEvictionAndExplicitInvalidation()
    {
        SvgDocument firstDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgDocument secondDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgResolveOptions options = new SvgResolveOptions();
        options.MaxCacheEntries = 1;
        SvgDocumentResolver resolver = new SvgDocumentResolver();

        SvgResolvedDocument first = resolver.ResolveWithOptions(firstDocument, options);
        SvgResolvedDocument second = resolver.ResolveWithOptions(secondDocument, options);
        SvgResolvedDocument rebuilt = resolver.ResolveWithOptions(firstDocument, options);
        int removed = resolver.Invalidate(firstDocument);
        int cleared = resolver.ClearCache();
        SvgResolvedDocumentCacheStatistics statistics = resolver.GetStatistics();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(rebuilt, Is.Not.SameAs(first));
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(cleared, Is.EqualTo(0));
            Assert.That(statistics.CacheEntries, Is.EqualTo(0));
            Assert.That(statistics.CacheMisses, Is.EqualTo(3));
            Assert.That(statistics.EvictedEntries, Is.EqualTo(2));
            Assert.That(statistics.InvalidatedEntries, Is.EqualTo(1));
        });
    }

    [Test]
    public void EnforcesResolvedTreeLimitsAndDoesNotCacheCancellation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/><circle/></g></svg>");
        SvgResolveOptions limitedOptions = new SvgResolveOptions();
        limitedOptions.Limits.Tree.MaxNodes = 2;
        SvgDocumentResolver limitedResolver = new SvgDocumentResolver();

        SvgResolvedDocument limited = limitedResolver.ResolveWithOptions(document, limitedOptions);
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        SvgResolveOptions cancelledOptions = new SvgResolveOptions();
        cancelledOptions.CancellationToken = cancellation.Token;
        SvgDocumentResolver cancelledResolver = new SvgDocumentResolver();
        SvgResolvedDocument beforeCancellation = cancelledResolver.ResolveWithOptions(document, cancelledOptions);
        cancellation.Cancel();
        SvgResolvedDocument cancelled = cancelledResolver.ResolveWithOptions(document, cancelledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Nodes, Has.Count.EqualTo(2));
            Assert.That(limited.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.Root, Is.Null);
            Assert.That(cancelled, Is.Not.SameAs(beforeCancellation));
            Assert.That(cancelled.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(cancelledResolver.GetStatistics().CacheEntries, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidOptionsAndPublicNullArgumentsDeterministically()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgResolveOptions options = new SvgResolveOptions();
        options.ViewportWidth = double.NaN;
        SvgDocumentResolver resolver = new SvgDocumentResolver();

        SvgResolvedDocument invalid = resolver.ResolveWithOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Root, Is.Null);
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(resolver.GetStatistics().CacheEntries, Is.EqualTo(0));
            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null));
            Assert.Throws<ArgumentNullException>(() => resolver.ResolveWithOptions(document, null));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
