namespace svg.Tests;

public class SvgElementPropertyConformanceTests
{
    static IEnumerable<TestCaseData> CurrentElementCases()
    {
        foreach (SvgElementDefinition definition in new SvgCatalogFactory().CreateElementCatalog().GetEntries())
        {
            string capability = ElementCapability(definition.Kind);
            yield return new TestCaseData(definition.Kind, definition.LocalName, capability)
                .SetName("current-element_" + definition.LocalName + "__" + capability);
        }
    }

    static IEnumerable<TestCaseData> LegacyElementCases()
    {
        foreach (SvgLegacyFeatureDefinition definition in new SvgCatalogFactory().CreateLegacyCatalog().GetElementEntries())
        {
            yield return new TestCaseData(definition.ElementKind, definition.LocalName, definition.Handling)
                .SetName("legacy-element_" + definition.LocalName + "__" + SvgFeatureIdentifiers.LegacyPreservation);
        }
    }

    static IEnumerable<TestCaseData> PropertyCases()
    {
        foreach (SvgPropertySupportEntry entry in SvgPropertyCatalog.CreateDefault().Entries)
        {
            string capability = PropertyCapability(entry);
            yield return new TestCaseData(entry.Name, entry.InitialValue, capability)
                .SetName("property_" + entry.Name + "__" + capability);
        }
    }

    [TestCaseSource(nameof(CurrentElementCases))]
    public void EveryCurrentElementBuildsRoundTripsAndMapsToAPublishedCapability(SvgElementKind kind, string localName, string capability)
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgBuilder builder = new SvgBuilder();
        builder.AddElement(kind);
        SvgNode generated = builder.LastNode;

        SvgWriteResult written = new SvgWriter().WriteWithDocumentAndOptions(builder.Build(), new SvgWriteOptions());
        SvgDocument reloaded = Load(written.Text);
        SvgNode roundTripped = reloaded.DocumentElement.Children.Single();
        SvgFeatureAnalysisResult analysis = reloaded.AnalyzeFeatures();
        string granular = new SvgCapabilities().GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.Element, localName);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Find(kind).LocalName, Is.EqualTo(localName));
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(generated.ElementKind, Is.EqualTo(kind));
            Assert.That(written.Success, Is.True, written.Message);
            Assert.That(roundTripped.ElementKind, Is.EqualTo(kind));
            Assert.That(roundTripped.LocalName, Is.EqualTo(localName));
            Assert.That(analysis.Success, Is.True, Diagnostics(analysis.FeatureReport.Diagnostics));
            Assert.That(analysis.DynamicallyUsedFeatures, Does.Contain(capability));
            Assert.That(analysis.DynamicallyUsedFeatures, Does.Contain(granular));
            Assert.That(new SvgCapabilities().FindLibraryFeature(capability), Is.Not.Null, "Unpublished capability mapping: " + capability);
            Assert.That(new SvgCapabilities().FindLibraryFeature(granular), Is.Not.Null, "Unpublished capability mapping: " + granular);
        });
    }

    [TestCaseSource(nameof(LegacyElementCases))]
    public void EveryLegacyElementRoundTripsAndMapsToLegacyCapability(SvgElementKind kind, string localName, SvgLegacyHandling handling)
    {
        SvgBuilder builder = new SvgBuilder();
        builder.AddElement(kind);
        SvgWriteResult written = new SvgWriter().WriteWithDocumentAndOptions(builder.Build(), new SvgWriteOptions());
        SvgDocument reloaded = Load(written.Text);
        SvgNode roundTripped = reloaded.DocumentElement.Children.Single();
        SvgLegacyAnalysisResult legacy = new SvgCatalogFactory().CreateLegacyCatalog().Analyze(roundTripped, false);
        SvgFeatureAnalysisResult features = reloaded.AnalyzeFeatures();
        string granular = new SvgCapabilities().GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.Element, localName);

        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(written.Success, Is.True, written.Message);
            Assert.That(roundTripped.ElementKind, Is.EqualTo(kind));
            Assert.That(roundTripped.LocalName, Is.EqualTo(localName));
            Assert.That(legacy.Success, Is.True);
            Assert.That(legacy.Occurrences.Single().Definition.Handling, Is.EqualTo(handling));
            Assert.That(features.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.LegacyPreservation));
            Assert.That(features.DynamicallyUsedFeatures, Does.Contain(granular));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.LegacyPreservation), Is.Not.Null);
            Assert.That(new SvgCapabilities().FindLibraryFeature(granular), Is.Not.Null);
        });
    }

    [TestCaseSource(nameof(PropertyCases))]
    public void EveryInventoryPropertyParsesValidatesAndMapsToAPublishedCapability(string name, string initialValue, string capability)
    {
        SvgCssLimits limits = new SvgCssLimits();
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgCapabilitySet active = SvgCapabilitySet.Create(new[] { SvgFeatureIdentifiers.CssParsing });
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(name + ":" + initialValue + ";", limits);

        SvgBuilder builder = new SvgBuilder();
        builder.BeginElement(SvgElementKind.Rect).SetAttribute("style", name + ":" + initialValue + ";");
        SvgFeatureAnalysisResult analysis = builder.Build().AnalyzeFeatures();
        string granular = new SvgCapabilities().GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssProperty, name);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Find(name), Is.Not.Null);
            Assert.That(catalog.Supports(name, initialValue, limits, active), Is.True, name + ": " + initialValue);
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(parsed.Declarations, Has.Count.EqualTo(1));
            Assert.That(parsed.Declarations[0].IsValid, Is.True);
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(analysis.Success, Is.True, Diagnostics(analysis.FeatureReport.Diagnostics));
            Assert.That(analysis.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.CssParsing));
            Assert.That(analysis.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.CssCascade));
            Assert.That(analysis.DynamicallyUsedFeatures, Does.Contain(granular));
            Assert.That(new SvgCapabilities().FindLibraryFeature(capability), Is.Not.Null, "Unpublished capability mapping: " + capability);
            Assert.That(new SvgCapabilities().FindLibraryFeature(granular), Is.Not.Null, "Unpublished capability mapping: " + granular);
        });
    }

    static string ElementCapability(SvgElementKind kind)
    {
        if (kind == SvgElementKind.Path) return SvgFeatureIdentifiers.Paths;
        if (kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect) return SvgFeatureIdentifiers.Shapes;
        if (kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Pattern || kind == SvgElementKind.Stop) return SvgFeatureIdentifiers.PaintServers;
        if (kind == SvgElementKind.Marker) return SvgFeatureIdentifiers.Markers;
        if (kind == SvgElementKind.ClipPath) return SvgFeatureIdentifiers.Clipping;
        if (kind == SvgElementKind.Mask) return SvgFeatureIdentifiers.Masking;
        if (kind >= SvgElementKind.FeBlend && kind <= SvgElementKind.Filter) return SvgFeatureIdentifiers.Filters;
        if (kind == SvgElementKind.Image) return SvgFeatureIdentifiers.Images;
        if (kind == SvgElementKind.Text || kind == SvgElementKind.TSpan || kind == SvgElementKind.TextPath) return SvgFeatureIdentifiers.TextLayout;
        if (kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Discard || kind == SvgElementKind.MPath || kind == SvgElementKind.Set) return SvgFeatureIdentifiers.SmilAnimation;
        if (kind == SvgElementKind.A) return SvgFeatureIdentifiers.Interaction;
        if (kind == SvgElementKind.Title || kind == SvgElementKind.Desc) return SvgFeatureIdentifiers.Accessibility;
        if (kind == SvgElementKind.Script) return SvgFeatureIdentifiers.ScriptPreservation;
        if (kind == SvgElementKind.Style) return SvgFeatureIdentifiers.CssParsing;
        if (kind == SvgElementKind.ForeignObject || kind == SvgElementKind.Audio || kind == SvgElementKind.Canvas || kind == SvgElementKind.Iframe || kind == SvgElementKind.Video) return SvgFeatureIdentifiers.ForeignContent;
        return SvgFeatureIdentifiers.EditableDocument;
    }

    static string PropertyCapability(SvgPropertySupportEntry entry)
    {
        string name = entry.Name;
        if (entry.IsLegacy) return SvgFeatureIdentifiers.LegacyPreservation;
        if (name.StartsWith("animation", StringComparison.Ordinal)) return SvgFeatureIdentifiers.CssAnimation;
        if (name == "clip-path" || name == "clip-rule") return SvgFeatureIdentifiers.Clipping;
        if (name == "filter" || name == "color-interpolation-filters" || name == "flood-color" || name == "flood-opacity" || name == "lighting-color") return SvgFeatureIdentifiers.Filters;
        if (name == "marker" || name == "marker-start" || name == "marker-mid" || name == "marker-end") return SvgFeatureIdentifiers.Markers;
        if (name == "mask" || name == "mask-type") return SvgFeatureIdentifiers.Masking;
        if (name == "fill" || name == "fill-opacity" || name == "fill-rule" || name == "stroke" || name.StartsWith("stroke-", StringComparison.Ordinal) || name == "stop-color" || name == "stop-opacity" || name == "paint-order") return SvgFeatureIdentifiers.PaintServers;
        if (name == "d") return SvgFeatureIdentifiers.Paths;
        if (name == "cx" || name == "cy" || name == "height" || name == "r" || name == "rx" || name == "ry" || name == "width" || name == "x" || name == "y" || name == "shape-rendering" || name == "vector-effect") return SvgFeatureIdentifiers.Shapes;
        if (name.StartsWith("font-", StringComparison.Ordinal) || name.StartsWith("text-", StringComparison.Ordinal) || name == "font" || name == "alignment-baseline" || name == "baseline-shift" || name == "direction" || name == "dominant-baseline" || name == "inline-size" || name == "letter-spacing" || name == "unicode-bidi" || name == "white-space" || name == "word-spacing" || name == "writing-mode") return SvgFeatureIdentifiers.TextLayout;
        if (name == "cursor" || name == "pointer-events") return SvgFeatureIdentifiers.Interaction;
        if (name == "image-rendering") return SvgFeatureIdentifiers.Images;
        return SvgFeatureIdentifiers.CssCascade;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True, Diagnostics(loaded.Diagnostics));
        return loaded.Document;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Message));
    }
}
