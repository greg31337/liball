namespace svg.Tests;

public class SvgElementReferenceCoverageTests
{
    [Test]
    public void EveryCurrentElementRoundTripsFromCatalogNameToLoadedNodeKind()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        List<SvgElementDefinition> entries = catalog.GetEntries();
        SvgDocument document = Load(CurrentElementDocument(entries));

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement.Children, Has.Count.EqualTo(entries.Count));
            Assert.That(entries, Has.Count.EqualTo(Enum.GetValues<SvgElementKind>().Count(kind => new SvgNodeNames().IsSvg2Element(kind))));
        });

        int index = 0;
        while (index < entries.Count)
        {
            SvgElementDefinition definition = entries[index];
            SvgNode node = document.DocumentElement.Children[index];
            Assert.Multiple(() =>
            {
                Assert.That(node.Kind, Is.EqualTo(SvgNodeKind.Element), definition.LocalName);
                Assert.That(node.ElementKind, Is.EqualTo(definition.Kind), definition.LocalName);
                Assert.That(node.LocalName, Is.EqualTo(definition.LocalName), definition.LocalName);
                Assert.That(node.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg), definition.LocalName);
                Assert.That(new SvgNodeNames().GetElementKind(SvgNamespaces.Svg, definition.LocalName), Is.EqualTo(definition.Kind), definition.LocalName);
                Assert.That(catalog.FindByName(node.NamespaceUri, node.LocalName), Is.SameAs(definition), definition.LocalName);
                Assert.That(node.SourceSpan, Is.Not.Null, definition.LocalName);
            });
            index = index + 1;
        }
    }

    [Test]
    public void EveryElementSpecificDefaultAndRequiredValueHasConsistentTypedAccess()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        List<SvgElementDefinition> entries = catalog.GetEntries();
        SvgDocument document = Load(CurrentElementDocument(entries));
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();
        int defaultCount = 0;
        int requiredCount = 0;

        int elementIndex = 0;
        while (elementIndex < entries.Count)
        {
            SvgElementDefinition definition = entries[elementIndex];
            SvgNode node = document.DocumentElement.Children[elementIndex];
            Assert.That(definition.Attributes.Select(attribute => attribute.NamespaceUri + "|" + attribute.LocalName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(definition.Attributes.Count), definition.LocalName);
            int attributeIndex = 0;
            while (attributeIndex < definition.Attributes.Count)
            {
                SvgElementAttributeDefinition attribute = definition.Attributes[attributeIndex];
                if (attribute.DefaultKind != SvgAttributeDefaultKind.None)
                {
                    SvgTypedAttributeValue value = accessor.GetNamespaced(node, attribute.NamespaceUri, attribute.LocalName);
                    Assert.That(value.Success, Is.True, definition.LocalName + "@" + attribute.LocalName);
                    Assert.That(value.Definition.LocalName, Is.EqualTo(attribute.LocalName), definition.LocalName + "@" + attribute.LocalName);
                    Assert.That(value.Definition.DefaultKind, Is.EqualTo(attribute.DefaultKind), definition.LocalName + "@" + attribute.LocalName);
                    if (attribute.DefaultKind == SvgAttributeDefaultKind.ContextDependent)
                    {
                        Assert.That(value.HasValue, Is.False, definition.LocalName + "@" + attribute.LocalName);
                        Assert.That(value.Origin, Is.EqualTo(SvgTypedAttributeOrigin.ContextDependentDefault), definition.LocalName + "@" + attribute.LocalName);
                    }
                    else
                    {
                        Assert.That(value.HasValue, Is.True, definition.LocalName + "@" + attribute.LocalName);
                        Assert.That(value.EffectiveText, Is.Not.Null, definition.LocalName + "@" + attribute.LocalName);
                    }
                    defaultCount = defaultCount + 1;
                }
                else if (attribute.Required)
                {
                    SvgTypedAttributeValue value = accessor.GetNamespaced(node, attribute.NamespaceUri, attribute.LocalName);
                    Assert.That(value.Success, Is.False, definition.LocalName + "@" + attribute.LocalName);
                    Assert.That(value.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue), definition.LocalName + "@" + attribute.LocalName);
                    Assert.That(value.Diagnostics.Single().Source, Is.SameAs(node.SourceSpan), definition.LocalName + "@" + attribute.LocalName);
                    requiredCount = requiredCount + 1;
                }
                attributeIndex = attributeIndex + 1;
            }
            elementIndex = elementIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(defaultCount, Is.GreaterThan(150));
            Assert.That(requiredCount, Is.GreaterThanOrEqualTo(8));
            Assert.That(accessor.GetStatistics().ParseCount, Is.GreaterThan(100));
        });
    }

    [Test]
    public void EveryLegacyDefinitionIsPreservedAndAnalyzedInItsDeclaredCategory()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();
        List<SvgLegacyFeatureDefinition> elements = catalog.GetElementEntries();
        List<SvgLegacyFeatureDefinition> attributes = catalog.GetAttributeEntries();
        SvgDocument document = Load(LegacyDocument(elements, attributes));

        SvgLegacyAnalysisResult result = catalog.Analyze(document.DocumentElement, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(document.DocumentElement.Children.Select(node => node.ElementKind), Is.EqualTo(elements.Select(entry => entry.ElementKind)));
            Assert.That(result.Occurrences, Has.Count.EqualTo(elements.Count + attributes.Count));
            Assert.That(result.ImplementedCount, Is.EqualTo(elements.Count(entry => entry.Handling == SvgLegacyHandling.Implemented) + attributes.Count(entry => entry.Handling == SvgLegacyHandling.Implemented)));
            Assert.That(result.PreserveOnlyCount, Is.EqualTo(elements.Count(entry => entry.Handling == SvgLegacyHandling.PreserveOnly) + attributes.Count(entry => entry.Handling == SvgLegacyHandling.PreserveOnly)));
            Assert.That(result.DeprecatedCount, Is.EqualTo(elements.Count(entry => entry.Handling == SvgLegacyHandling.Deprecated) + attributes.Count(entry => entry.Handling == SvgLegacyHandling.Deprecated)));
            Assert.That(result.UnsupportedCount, Is.EqualTo(elements.Count(entry => entry.Handling == SvgLegacyHandling.Unsupported) + attributes.Count(entry => entry.Handling == SvgLegacyHandling.Unsupported)));
            Assert.That(result.Occurrences.Select(occurrence => occurrence.Definition.Handling).Distinct(), Is.EquivalentTo(Enum.GetValues<SvgLegacyHandling>()));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
            Assert.That(result.Occurrences.All(occurrence => occurrence.Node != null), Is.True);
        });
    }

    [Test]
    public void HrefPrecedenceInheritanceAndCyclesAreConsistentAcrossReferenceFamilies()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'>" +
            "<g id='modern'><rect/></g><g id='legacy'><circle/></g><use href='#modern' xlink:href='#legacy'/>" +
            "<linearGradient id='paintBase'><stop stop-color='red'/></linearGradient><linearGradient id='paintWrong'><stop stop-color='blue'/></linearGradient><linearGradient id='paint' href='#paintBase' xlink:href='#paintWrong'/>" +
            "<linearGradient id='paintCycleA' href='#paintCycleB'/><linearGradient id='paintCycleB' href='#paintCycleA'/>" +
            "<filter id='filterBase'><feFlood/></filter><filter id='filter' href='#filterBase'/><filter id='filterCycleA' href='#filterCycleB'/><filter id='filterCycleB' href='#filterCycleA'/>" +
            "</svg>");

        SvgReferenceResult reference = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[2]);
        SvgPaintServerResolveOptions paintOptions = new SvgPaintServerResolveOptions();
        paintOptions.TargetBounds = SvgRect.Create(0, 0, 100, 100);
        SvgPaintServerResult paint = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[5], paintOptions);
        SvgPaintServerResult paintCycle = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[6], paintOptions);
        SvgFilterResolveOptions filterOptions = new SvgFilterResolveOptions();
        filterOptions.TargetBounds = SvgRect.Create(0, 0, 100, 100);
        SvgFilterResult filter = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[9], filterOptions);
        SvgFilterResult filterCycle = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[10], filterOptions);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Success, Is.True);
            Assert.That(reference.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(reference.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.Href));
            Assert.That(reference.XLinkHrefIgnored, Is.True);
            Assert.That(paint.Success, Is.True);
            Assert.That(paint.TemplateChain.Select(node => node.Attributes.Single(attribute => attribute.LocalName == "id").Value), Is.EqualTo(new[] { "paint", "paintBase" }));
            Assert.That(paint.Stops.Single().Color.Red, Is.EqualTo(1));
            Assert.That(paint.Stops.Single().Color.Blue, Is.EqualTo(0));
            Assert.That(paintCycle.Success, Is.False);
            Assert.That(paintCycle.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(filter.Success, Is.True);
            Assert.That(filter.TemplateChain.Select(node => node.Attributes.Single(attribute => attribute.LocalName == "id").Value), Is.EqualTo(new[] { "filter", "filterBase" }));
            Assert.That(filter.Primitives.Single().ElementKind, Is.EqualTo(SvgElementKind.FeFlood));
            Assert.That(filterCycle.Success, Is.False);
            Assert.That(filterCycle.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
        });
    }

    [Test]
    public void ViewportUseInstancesAndSwitchSelectionComposeInResolvedDocument()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100'>" +
            "<symbol id='icon' viewBox='0 0 20 10'><switch><rect requiredFeatures='urn:missing' width='20' height='10'/><circle systemLanguage='fr' r='5'/><path d='M0 0L20 10'/></switch></symbol>" +
            "<use href='#icon' x='10' y='20' width='80' height='40'/></svg>");
        SvgResolveOptions options = new SvgResolveOptions();
        options.LanguagePreferences.Add("en-US");

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, options);
        SvgResolvedNode instanceRoot = result.FindInstance("0/1/instance/0");
        SvgResolvedNode instanceSwitch = result.FindInstance("0/1/instance/0/0");
        SvgResolvedNode featureCandidate = result.FindInstance("0/1/instance/0/0/0");
        SvgResolvedNode languageCandidate = result.FindInstance("0/1/instance/0/0/1");
        SvgResolvedNode fallback = result.FindInstance("0/1/instance/0/0/2");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Root.Viewport.Width, Is.EqualTo(200));
            Assert.That(result.Root.Viewport.Height, Is.EqualTo(100));
            Assert.That(instanceRoot.Structural.Referenced, Is.True);
            Assert.That(instanceRoot.Bounds.X, Is.EqualTo(10));
            Assert.That(instanceRoot.Bounds.Y, Is.EqualTo(20));
            Assert.That(instanceRoot.Bounds.Width, Is.EqualTo(80));
            Assert.That(instanceRoot.Bounds.Height, Is.EqualTo(40));
            Assert.That(instanceSwitch.Switch.SelectedChild, Is.SameAs(fallback.SourceNode));
            Assert.That(featureCandidate.Active, Is.False);
            Assert.That(languageCandidate.Active, Is.False);
            Assert.That(fallback.Active, Is.True);
            Assert.That(fallback.Renderable, Is.True);
            Assert.That(result.Nodes.Select(node => node.InstancePath).Where(path => path.Length != 0).Distinct().Count(), Is.EqualTo(result.Nodes.Count(node => node.InstancePath.Length != 0)));
        });
    }

    [Test]
    public void InvalidRangesFailAtTheirAuthoredSourcesAcrossElementFamilies()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
            "<svg width='-1'/><rect width='-1' height='1'/><circle r='-1'/><image href='data:image/png;base64,' width='-1' height='1'/>" +
            "<radialGradient r='-1'><stop/></radialGradient><marker markerWidth='-1'><rect/></marker><filter><feGaussianBlur stdDeviation='-1'/></filter>" +
            "<symbol id='icon' viewBox='0 0 1 1'/><use href='#icon' width='-1' height='1'/></svg>");
        SvgViewportResult root = new SvgViewportResolver().ResolveRoot(document.DocumentElement, 100, 100);
        SvgViewportResult viewport = new SvgViewportResolver().ResolveNested(document.DocumentElement.Children[0], root);
        SvgShapeResult rectangle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgShapeResult circle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[2]);
        SvgEmbeddedContentResult image = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[3]);
        SvgPaintServerResolveOptions paintOptions = new SvgPaintServerResolveOptions();
        paintOptions.TargetBounds = SvgRect.Create(0, 0, 100, 100);
        SvgPaintServerResult paint = new SvgPaintServerResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[4], paintOptions);
        SvgEffectResult marker = new SvgEffectResolver().ResolveWithNode(document.DocumentElement.Children[5]);
        SvgFilterResolveOptions filterOptions = new SvgFilterResolveOptions();
        filterOptions.TargetBounds = SvgRect.Create(0, 0, 100, 100);
        SvgFilterResult filter = new SvgFilterResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[6], filterOptions);
        SvgUseResolveOptions useOptions = new SvgUseResolveOptions();
        useOptions.LengthContext.HasViewport = true;
        useOptions.LengthContext.ViewportWidth = 100;
        useOptions.LengthContext.ViewportHeight = 100;
        SvgUseResult use = new SvgUseResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[8], useOptions);

        Assert.Multiple(() =>
        {
            Assert.That(viewport.Success, Is.False);
            Assert.That(viewport.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[0].Attributes[0].SourceSpan)), Is.True);
            Assert.That(rectangle.Success, Is.False);
            Assert.That(rectangle.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[1].Attributes[0].SourceSpan)), Is.True);
            Assert.That(circle.Success, Is.False);
            Assert.That(circle.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[2].Attributes[0].SourceSpan)), Is.True);
            Assert.That(image.Success, Is.False);
            Assert.That(image.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[3].Attributes[1].SourceSpan)), Is.True);
            Assert.That(paint.Success, Is.False);
            Assert.That(paint.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[4].Attributes[0].SourceSpan)), Is.True);
            Assert.That(marker.Success, Is.False);
            Assert.That(marker.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[5].Attributes[0].SourceSpan)), Is.True);
            Assert.That(filter.Success, Is.False);
            Assert.That(filter.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[6].Children[0].Attributes[0].SourceSpan)), Is.True);
            Assert.That(use.Success, Is.False);
            Assert.That(use.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidValue && IsWithin(diagnostic.Source, document.DocumentElement.Children[8].Attributes[1].SourceSpan)), Is.True);
        });
    }

    [Test]
    public void UnknownSvgAndForeignContentRemainSourcePreservingResolvedNodes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><vendor-widget data-mode='raw'><x:payload xmlns:x='urn:vendor' x:value='42'>text</x:payload></vendor-widget></svg>");
        SvgNode unknown = document.DocumentElement.Children[0];
        SvgNode foreign = unknown.Children[0];

        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode resolvedUnknown = result.FindSource(unknown);
        SvgResolvedNode resolvedForeign = result.FindSource(foreign);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(unknown.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(unknown.QualifiedName, Is.EqualTo("vendor-widget"));
            Assert.That(unknown.Attributes.Single().Value, Is.EqualTo("raw"));
            Assert.That(foreign.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(foreign.NamespaceUri, Is.EqualTo("urn:vendor"));
            Assert.That(foreign.QualifiedName, Is.EqualTo("x:payload"));
            Assert.That(foreign.Attributes.Single(attribute => attribute.LocalName == "value").Value, Is.EqualTo("42"));
            Assert.That(resolvedUnknown.Kind, Is.EqualTo(SvgResolvedNodeKind.Unknown));
            Assert.That(resolvedForeign.Kind, Is.EqualTo(SvgResolvedNodeKind.Unknown));
            Assert.That(resolvedUnknown.SourceNode, Is.SameAs(unknown));
            Assert.That(resolvedForeign.SourceNode, Is.SameAs(foreign));
            Assert.That(resolvedForeign.Children.Single().Text, Is.EqualTo("text"));
        });
    }

    static string CurrentElementDocument(List<SvgElementDefinition> entries)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder("<svg xmlns='http://www.w3.org/2000/svg'>");
        int index = 0;
        while (index < entries.Count)
        {
            source.Append('<');
            source.Append(entries[index].LocalName);
            source.Append("/>");
            index = index + 1;
        }
        source.Append("</svg>");
        return source.ToString();
    }

    static string LegacyDocument(List<SvgLegacyFeatureDefinition> elements, List<SvgLegacyFeatureDefinition> attributes)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'");
        int index = 0;
        while (index < attributes.Count)
        {
            SvgLegacyFeatureDefinition definition = attributes[index];
            source.Append(' ');
            if (definition.NamespaceUri == SvgNamespaces.XLink) source.Append("xlink:");
            source.Append(definition.LocalName);
            source.Append("='legacy'");
            index = index + 1;
        }
        source.Append('>');
        index = 0;
        while (index < elements.Count)
        {
            source.Append('<');
            source.Append(elements[index].LocalName);
            source.Append("/>");
            index = index + 1;
        }
        source.Append("</svg>");
        return source.ToString();
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(System.Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Document;
    }

    static bool IsWithin(SvgSourceSpan source, SvgSourceSpan container)
    {
        return source != null && container != null && ReferenceEquals(source.Source, container.Source) && source.StartOffset >= container.StartOffset && source.EndOffset <= container.EndOffset;
    }
}
