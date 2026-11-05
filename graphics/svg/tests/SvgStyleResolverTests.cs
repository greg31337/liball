namespace svg.Tests;

public class SvgStyleResolverTests
{
    [Test]
    public void ConvenienceResolutionUsesStableResolverDefaults()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        List<SvgStyleSheet> sheets = new List<SvgStyleSheet> { Sheet("rect { fill:red; }") };
        SvgStyleResolver resolver = new SvgStyleResolver();

        SvgComputedStyle first = resolver.Resolve(rect, sheets);
        SvgComputedStyle second = resolver.Resolve(rect, sheets);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(resolver.GetStatistics().CacheHits, Is.EqualTo(2));
        });
    }

    [Test]
    public void CacheHitsAndNodeVersionsTrackAttributeClassAndTreeChanges()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect class='cold'/><circle/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet(".cold { fill:blue; } .hot { fill:red; } g > rect:first-child { stroke:green; }");
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();

        SvgComputedStyle first = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgComputedStyle cached = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgAttribute replacement = new SvgAttribute("class", "", "class", "", "hot");
        SvgMutationResult mutation = new SvgNodeApi().ReplaceAttribute(rect, rect.Attributes.Single(attribute => attribute.LocalName == "class"), replacement);
        SvgComputedStyle changed = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgStyleInvalidationResult invalidated = resolver.InvalidateAttribute(rect, "class");
        SvgComputedStyle explicitRefresh = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgStyleInvalidationResult tree = resolver.InvalidateTree(group);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.GetPaint("fill").Color.Blue, Is.EqualTo(1));
            Assert.That(cached, Is.SameAs(first));
            Assert.That(mutation.Success, Is.True);
            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(changed.GetPaint("fill").Color.Red, Is.EqualTo(1));
            Assert.That(changed.GetPaint("stroke").Color.Green, Is.GreaterThan(0));
            Assert.That(invalidated.Kind, Is.EqualTo(SvgStyleInvalidationKind.ClassOrId));
            Assert.That(invalidated.RemovedEntries, Is.GreaterThanOrEqualTo(1));
            Assert.That(explicitRefresh, Is.Not.SameAs(changed));
            Assert.That(tree.RemovedEntries, Is.GreaterThanOrEqualTo(1));
            Assert.That(resolver.GetStatistics().CacheHits, Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void StylesheetVersionsAndCustomPropertyDependenciesInvalidateAffectedEntries()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g style='--tone:orange'><rect style='fill:var(--tone)'/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet("rect { stroke:black; }");
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();

        SvgComputedStyle first = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgStyleInvalidationResult variables = resolver.InvalidateCustomProperty(group, "--tone");
        SvgComputedStyle afterVariable = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        sheet.MarkChanged();
        SvgComputedStyle afterVersion = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgStyleInvalidationResult stylesheet = resolver.InvalidateStyleSheet(sheet);

        Assert.Multiple(() =>
        {
            Assert.That(first.Find("fill").Dependencies, Is.EqualTo(new[] { "--tone" }));
            Assert.That(variables.Kind, Is.EqualTo(SvgStyleInvalidationKind.CustomProperty));
            Assert.That(variables.RemovedEntries, Is.EqualTo(2));
            Assert.That(afterVariable, Is.Not.SameAs(first));
            Assert.That(afterVersion, Is.Not.SameAs(afterVariable));
            Assert.That(stylesheet.RemovedEntries, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public void ViewportMediaAndFontRevisionsParticipateInCacheValidity()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet("rect { fill:red; }");
        SvgCollectedStyleSheet collected = new SvgCollectedStyleSheet(sheet, SvgStyleSheetSourceKind.Caller, null, null, "screen and (min-width:500px)", 0);
        SvgStyleSheetCollection collection = new SvgStyleSheetCollection(new List<SvgCollectedStyleSheet> { collected }, new List<SvgDiagnostic>());
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();
        options.StyleEnvironment.ViewportWidth = 100;
        options.StyleEnvironment.ViewportHeight = 100;

        SvgComputedStyle narrow = resolver.ResolveCollection(rect, collection, options);
        options.StyleEnvironment.ViewportWidth = 800;
        options.StyleEnvironment.MarkChanged();
        SvgComputedStyle wide = resolver.ResolveCollection(rect, collection, options);
        options.FontVersion = 1;
        SvgComputedStyle newFonts = resolver.ResolveCollection(rect, collection, options);
        SvgStyleInvalidationResult viewport = resolver.InvalidateViewport(options.StyleEnvironment);

        Assert.Multiple(() =>
        {
            Assert.That(narrow.GetPaint("fill").Color.Red, Is.EqualTo(0));
            Assert.That(wide.GetPaint("fill").Color.Red, Is.EqualTo(1));
            Assert.That(wide, Is.Not.SameAs(narrow));
            Assert.That(newFonts, Is.Not.SameAs(wide));
            Assert.That(viewport.Kind, Is.EqualTo(SvgStyleInvalidationKind.Viewport));
            Assert.That(viewport.RemovedEntries, Is.EqualTo(2));
        });
    }

    [Test]
    public void InteractionRevisionRefreshesDynamicSelectorsAndExplicitInvalidation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet("rect { fill:blue; } rect:hover { fill:red; }");
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();
        bool hovered = false;
        SvgDynamicSelectorState dynamicState = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        dynamicState.HoverState = node => node == rect && hovered;
        options.SelectorOptions.DynamicState = dynamicState;

        SvgComputedStyle idle = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        hovered = true;
        dynamicState.MarkChanged();
        SvgComputedStyle hover = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { sheet }, options);
        SvgStyleInvalidationResult invalidated = resolver.InvalidateInteraction(options.SelectorOptions.DynamicState);

        Assert.Multiple(() =>
        {
            Assert.That(idle.GetPaint("fill").Color.Blue, Is.EqualTo(1));
            Assert.That(hover.GetPaint("fill").Color.Red, Is.EqualTo(1));
            Assert.That(hover, Is.Not.SameAs(idle));
            Assert.That(invalidated.Kind, Is.EqualTo(SvgStyleInvalidationKind.Interaction));
            Assert.That(invalidated.RemovedEntries, Is.EqualTo(2));
        });
    }

    [Test]
    public void DocumentResolutionIsPreorderedAndCacheCapacityIsBounded()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/><circle/></g></svg>");
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions { MaxCacheEntries = 2 };

        SvgComputedStyleTree result = resolver.ResolveDocumentFromStyleSheets(document, new List<SvgStyleSheet>(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Styles.Select(style => style.Node.LocalName), Is.EqualTo(new[] { "svg", "g", "rect", "circle" }));
            Assert.That(result.Find(document.DocumentElement.Children[0].Children[1]), Is.Not.Null);
            Assert.That(result.SourceVersion, Is.EqualTo(document.Version));
            Assert.That(resolver.GetStatistics().CacheEntries, Is.LessThanOrEqualTo(2));
            Assert.That(resolver.GetStatistics().EvictedEntries, Is.GreaterThan(0));
        });
    }

    static SvgStyleSheet Sheet(string css)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, new SvgSourceUri("resolver.css"));
        Assert.That(parsed.Success, Is.True);
        return parsed.StyleSheet;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
