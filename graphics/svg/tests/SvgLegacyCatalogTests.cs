namespace svg.Tests;

public class SvgLegacyCatalogTests
{
    [Test]
    public void CatalogCoversEveryRecognizedLegacyElementExactlyOnce()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();
        List<SvgLegacyFeatureDefinition> entries = catalog.GetElementEntries();
        SvgElementKind[] legacyKinds = Enum.GetValues<SvgElementKind>().Where(kind => new SvgNodeNames().IsSvg11LegacyElement(kind)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(legacyKinds.Length));
            Assert.That(entries.Select(entry => entry.ElementKind), Is.EquivalentTo(legacyKinds));
            Assert.That(entries.Select(entry => entry.ElementKind).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.All(entry => entry.FeatureType == SvgLegacyFeatureType.Element), Is.True);
            Assert.That(entries.All(entry => entry.NamespaceUri == SvgNamespaces.Svg), Is.True);
            Assert.That(legacyKinds.All(kind => catalog.FindElement(kind) != null), Is.True);
            Assert.That(catalog.FindElement(SvgElementKind.Rect), Is.Null);
            Assert.That(catalog.FindElement(SvgElementKind.Unknown), Is.Null);
        });
    }

    [Test]
    public void CatalogCoversEveryRecognizedLegacyAndXLinkAttribute()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();
        string legacyText = "accent-height,alphabetic,arabic-form,ascent,attributeType,baseProfile,bbox,cap-height,contentScriptType,contentStyleType,descent,enable-background,externalResourcesRequired,filterRes,format,g1,g2,glyph-name,glyph-orientation-horizontal,glyph-orientation-vertical,glyphRef,hanging,horiz-adv-x,horiz-origin-x,horiz-origin-y,ideographic,k,kerning,local,mathematical,name,onactivate,onzoom,orientation,overline-position,overline-thickness,panose-1,rendering-intent,requiredFeatures,stemh,stemv,strikethrough-position,strikethrough-thickness,string,u1,u2,underline-position,underline-thickness,unicode,unicode-range,units-per-em,v-alphabetic,v-hanging,v-ideographic,v-mathematical,version,vert-adv-y,vert-origin-x,vert-origin-y,viewTarget,widths,x-height,zoomAndPan";
        string[] legacyNames = legacyText.Split(',');
        string[] xlinkNames = new[] { "actuate", "arcrole", "href", "role", "show", "title", "type" };
        List<SvgLegacyFeatureDefinition> entries = catalog.GetAttributeEntries();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(legacyNames.Length + xlinkNames.Length));
            Assert.That(legacyNames, Has.Length.EqualTo(63));
            Assert.That(legacyNames.All(name => new SvgNodeNames().GetAttributeKind("", name) == SvgAttributeKind.Svg11Legacy), Is.True);
            Assert.That(legacyNames.All(name => catalog.FindAttribute("", name) != null), Is.True);
            Assert.That(xlinkNames.All(name => new SvgNodeNames().GetAttributeKind(SvgNamespaces.XLink, name) == SvgAttributeKind.XLink), Is.True);
            Assert.That(xlinkNames.All(name => catalog.FindAttribute(SvgNamespaces.XLink, name) != null), Is.True);
            Assert.That(entries.Select(entry => entry.NamespaceUri + "|" + entry.LocalName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(entries.Count));
            Assert.That(catalog.FindAttribute("", "id"), Is.Null);
            Assert.That(catalog.FindAttribute(SvgNamespaces.Xml, "lang"), Is.Null);
            Assert.That(catalog.FindAttribute(null, "version"), Is.Null);
        });
    }

    [Test]
    public void HandlingStatusesDistinguishCompatibilityPreservationMigrationAndFailure()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();
        SvgLegacyFeatureDefinition implemented = catalog.FindAttribute("", "kerning");
        SvgLegacyFeatureDefinition preserved = catalog.FindElement(SvgElementKind.FontFaceSrc);
        SvgLegacyFeatureDefinition deprecated = catalog.FindElement(SvgElementKind.AnimateColor);
        SvgLegacyFeatureDefinition unsupported = catalog.FindElement(SvgElementKind.AltGlyph);

        Assert.Multiple(() =>
        {
            Assert.That(implemented.Handling, Is.EqualTo(SvgLegacyHandling.Implemented));
            Assert.That(implemented.Replacement, Is.EqualTo("font-kerning"));
            Assert.That(preserved.Handling, Is.EqualTo(SvgLegacyHandling.PreserveOnly));
            Assert.That(preserved.Replacement, Is.Empty);
            Assert.That(deprecated.Handling, Is.EqualTo(SvgLegacyHandling.Deprecated));
            Assert.That(deprecated.Replacement, Is.EqualTo("animate"));
            Assert.That(unsupported.Handling, Is.EqualTo(SvgLegacyHandling.Unsupported));
            Assert.That(unsupported.Replacement, Is.EqualTo("text"));
            Assert.That(catalog.GetElementEntries().Select(entry => entry.Handling).Distinct(), Is.EquivalentTo(new[]
            {
                SvgLegacyHandling.Implemented,
                SvgLegacyHandling.PreserveOnly,
                SvgLegacyHandling.Deprecated,
                SvgLegacyHandling.Unsupported
            }));
            Assert.That(catalog.GetAttributeEntries().Select(entry => entry.Handling).Distinct(), Is.EquivalentTo(Enum.GetValues<SvgLegacyHandling>()));
        });
    }

    [Test]
    public void RecursiveAnalysisProducesLocatedDiagnosticsInDocumentOrder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' enable-background='new'>" +
            "<font horiz-adv-x='1000'><glyph unicode='A'/></font>" +
            "<animateColor attributeType='XML'/><altGlyph xlink:href='#g'/></svg>";
        SvgDocument document = Load(source);
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();

        SvgLegacyAnalysisResult result = catalog.Analyze(document.DocumentNode, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceVersion, Is.EqualTo(document.Version));
            Assert.That(result.Occurrences, Has.Count.EqualTo(10));
            Assert.That(result.ImplementedCount, Is.EqualTo(5));
            Assert.That(result.PreserveOnlyCount, Is.Zero);
            Assert.That(result.DeprecatedCount, Is.EqualTo(4));
            Assert.That(result.UnsupportedCount, Is.EqualTo(1));
            Assert.That(result.Occurrences.Select(occurrence => occurrence.Definition.LocalName), Is.EqualTo(new[]
            {
                "version", "enable-background", "font", "horiz-adv-x", "glyph", "unicode", "animateColor", "attributeType", "altGlyph", "href"
            }));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain(SvgDiagnosticCode.CompatibilityRecovery));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Not.Contain(SvgDiagnosticCode.PreservedFeature));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain(SvgDiagnosticCode.UnsupportedFeature));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
            Assert.That(result.Occurrences[0].Diagnostic.Source, Is.SameAs(result.Occurrences[0].Attribute.SourceSpan));
            Assert.That(result.Occurrences[2].Diagnostic.Source, Is.SameAs(result.Occurrences[2].Node.SourceSpan));
            Assert.That(result.Occurrences[6].Diagnostic.Message, Does.Contain("Use 'animate' instead"));
            Assert.That(result.Occurrences[9].Diagnostic.Message, Does.Contain("xlink:href"));
            Assert.That(result.Occurrences[1].Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Info));
            Assert.That(result.Occurrences[8].Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Warning));
        });
    }

    [Test]
    public void AnalysisScopeAndInvalidArgumentsAreExplicit()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><font/></g></svg>");
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();

        SvgLegacyAnalysisResult rootOnly = catalog.Analyze(document.DocumentNode, false);
        SvgLegacyAnalysisResult subtree = catalog.Analyze(document.DocumentElement.Children[0], true);
        SvgLegacyAnalysisResult invalid = catalog.Analyze(null, true);

        Assert.Multiple(() =>
        {
            Assert.That(rootOnly.Success, Is.True);
            Assert.That(rootOnly.Occurrences, Is.Empty);
            Assert.That(subtree.Success, Is.True);
            Assert.That(subtree.Occurrences.Single().Definition.ElementKind, Is.EqualTo(SvgElementKind.Font));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Occurrences, Is.Empty);
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(invalid.Diagnostics[0].Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
        });
    }

    [Test]
    public void DefinitionConstructionRejectsMismatchedKindsAndMissingMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Element, SvgElementKind.Rect, SvgNamespaces.Svg, "rect", SvgLegacyHandling.Unsupported, "", "detail"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Font, "", "name", SvgLegacyHandling.PreserveOnly, "", "detail"));
            Assert.Throws<ArgumentNullException>(() => new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Unknown, null, "name", SvgLegacyHandling.PreserveOnly, "", "detail"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Unknown, "", "", SvgLegacyHandling.PreserveOnly, "", "detail"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgLegacyFeatureDefinition(SvgLegacyFeatureType.Attribute, SvgElementKind.Unknown, "", "name", SvgLegacyHandling.PreserveOnly, "", ""));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
