namespace svg.Tests;

public class SvgCssConformanceTests
{
    [Test]
    public void TokenAndRuleRecoveryPreserveInputAndContinueAtSafeBoundaries()
    {
        string tokenSource = "\"broken\nnext url(bad\"x) /*open";
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndLimits(tokenSource, new SvgCssLimits());
        string css = "@vendor feature(foo); rect { fill:url(bad\"x); stroke:blue; broken } circle { color:green; }";
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(tokenized.Success, Is.True);
            Assert.That(string.Concat(tokenized.Tokens.Select(token => token.RawText)), Is.EqualTo(tokenSource));
            Assert.That(tokenized.Tokens.Any(token => token.Kind == SvgCssTokenKind.BadString), Is.True);
            Assert.That(tokenized.Tokens.Any(token => token.Kind == SvgCssTokenKind.BadUrl), Is.True);
            Assert.That(tokenized.Tokens.Any(token => token.Kind == SvgCssTokenKind.Comment), Is.True);
            Assert.That(tokenized.ParseErrorCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.StyleSheet.Rules, Has.Count.EqualTo(3));
            Assert.That(parsed.StyleSheet.Rules[0].AtRule.Kind, Is.EqualTo(SvgCssAtRuleKind.Unknown));
            Assert.That(parsed.StyleSheet.Rules[0].AtRule.UnknownSyntax.RawText, Is.EqualTo("@vendor feature(foo);"));
            Assert.That(parsed.StyleSheet.Rules[2].Selectors.Items[0].Compounds[0].SimpleSelectors[0].Name, Is.EqualTo("circle"));
            Assert.That(parsed.StyleSheet.Rules[2].Declarations.Single().Name, Is.EqualTo("color"));
            Assert.That(parsed.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
        });
    }

    [Test]
    public void NamespaceAwareSelectorsUseFunctionalPseudoSpecificityInTheCascade()
    {
        string xml = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x'><g><rect id='target' class='hot'/><x:rect/></g></svg>";
        SvgDocument document = Load(xml);
        SvgNode svgRect = document.DocumentElement.Children[0].Children[0];
        SvgNode foreignRect = document.DocumentElement.Children[0].Children[1];
        string css = "@namespace s 'http://www.w3.org/2000/svg'; @namespace x 'urn:x'; " +
            "s|g:where(.unused, g) > s|rect:is(.hot, #target):nth-child(1) { fill:red; } x|rect { fill:blue; }";
        SvgStyleSheet sheet = Sheet(css, SvgStyleOrigin.Author);
        SvgComplexSelector selector = sheet.Rules[2].Selectors.Items[0];

        SvgCascadeResult svgResult = new SvgCascade().ResolveWithNodeAndStyleSheets(svgRect, new List<SvgStyleSheet> { sheet });
        SvgCascadeResult foreignResult = new SvgCascade().ResolveWithNodeAndStyleSheets(foreignRect, new List<SvgStyleSheet> { sheet });

        Assert.Multiple(() =>
        {
            Assert.That(selector.IdSpecificity, Is.EqualTo(1));
            Assert.That(selector.ClassSpecificity, Is.EqualTo(1));
            Assert.That(selector.TypeSpecificity, Is.EqualTo(2));
            Assert.That(svgResult.Find("fill").ValueText, Is.EqualTo("red"));
            Assert.That(svgResult.Find("fill").Winner.IdSpecificity, Is.EqualTo(1));
            Assert.That(foreignResult.Find("fill").ValueText, Is.EqualTo("blue"));
            Assert.That(sheet.Namespaces.NamedNamespaces["s"], Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(sheet.Namespaces.NamedNamespaces["x"], Is.EqualTo("urn:x"));
        });
    }

    [Test]
    public void MediaAndSupportsGroupRulesGateNestedCascadeCandidates()
    {
        string css = "rect { fill:black; stroke:black; } " +
            "@media screen and (min-width:600px) { rect { fill:red; } } " +
            "@media print { rect { fill:yellow; } } " +
            "@supports (stroke:blue) and selector(rect:first-child) { rect { stroke:blue; } } " +
            "@supports (unknown-property:value) { rect { stroke:red; } }";
        SvgStyleSheet sheet = Sheet(css, SvgStyleOrigin.Author);
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgCascadeOptions wideOptions = new SvgCascadeOptions();
        wideOptions.StyleEnvironment.ViewportWidth = 800;
        wideOptions.StyleEnvironment.ViewportHeight = 600;
        SvgCascadeOptions narrowOptions = new SvgCascadeOptions();
        narrowOptions.StyleEnvironment.ViewportWidth = 320;
        narrowOptions.StyleEnvironment.ViewportHeight = 600;

        SvgCascadeResult wide = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { sheet }, wideOptions);
        SvgCascadeResult narrow = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { sheet }, narrowOptions);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.Rules[1].AtRule.MediaQueries.IsValid, Is.True);
            Assert.That(sheet.Rules[3].AtRule.SupportsQuery.IsValid, Is.True);
            Assert.That(new SvgMediaQueryEvaluator().Matches(sheet.Rules[1].AtRule.MediaQueries, wideOptions.StyleEnvironment), Is.True);
            Assert.That(new SvgMediaQueryEvaluator().Matches(sheet.Rules[1].AtRule.MediaQueries, narrowOptions.StyleEnvironment), Is.False);
            Assert.That(new SvgSupportsQueryEvaluator().Matches(sheet.Rules[3].AtRule.SupportsQuery, new SvgSupportsEnvironment(), new SvgCssLimits()), Is.True);
            Assert.That(new SvgSupportsQueryEvaluator().Matches(sheet.Rules[4].AtRule.SupportsQuery, new SvgSupportsEnvironment(), new SvgCssLimits()), Is.False);
            Assert.That(wide.Find("fill").ValueText, Is.EqualTo("red"));
            Assert.That(narrow.Find("fill").ValueText, Is.EqualTo("black"));
            Assert.That(wide.Find("stroke").ValueText, Is.EqualTo("blue"));
            Assert.That(narrow.Find("stroke").ValueText, Is.EqualTo("blue"));
            Assert.That(wide.Candidates.Any(candidate => candidate.ValueText == "yellow"), Is.False);
            Assert.That(wide.Candidates.Any(candidate => candidate.ValueText == "red" && candidate.PropertyName == "stroke"), Is.False);
        });
    }

    [Test]
    public void MalformedConditionalGroupsRetainChildrenWithoutApplyingThem()
    {
        string css = "rect { fill:black; stroke:black; } " +
            "@media screen and (min-width:) { rect { fill:red; } } " +
            "@supports (stroke:) { rect { stroke:red; } }";
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { parsed.StyleSheet });

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.StyleSheet.IsValid, Is.False);
            Assert.That(parsed.StyleSheet.Rules[1].IsValid, Is.False);
            Assert.That(parsed.StyleSheet.Rules[2].IsValid, Is.False);
            Assert.That(parsed.StyleSheet.Rules[1].ChildRules, Has.Count.EqualTo(1));
            Assert.That(parsed.StyleSheet.Rules[2].ChildRules, Has.Count.EqualTo(1));
            Assert.That(parsed.Diagnostics.Count(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("black"));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("black"));
            Assert.That(result.Candidates.Any(candidate => candidate.ValueText == "red"), Is.False);
        });
    }

    [Test]
    public void OriginsImportanceSpecificityAndPresentationAttributesShareOneOrdering()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect fill='orange' stroke='green' style='opacity:.5'/></svg>").DocumentElement.Children[0];
        SvgStyleSheet userAgent = Sheet("rect { fill:black !important; stroke:black; opacity:.1; }", SvgStyleOrigin.UserAgent);
        SvgStyleSheet user = Sheet("rect { fill:purple !important; stroke:blue; opacity:.2 !important; }", SvgStyleOrigin.User);
        SvgStyleSheet author = Sheet("* { fill:red; } rect { fill:yellow !important; stroke:red; opacity:.3 !important; }", SvgStyleOrigin.Author);

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { userAgent, user, author });

        Assert.Multiple(() =>
        {
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("black"));
            Assert.That(result.Find("fill").Winner.Origin, Is.EqualTo(SvgStyleOrigin.UserAgent));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("red"));
            Assert.That(result.Find("stroke").Winner.TypeSpecificity, Is.EqualTo(1));
            Assert.That(result.Find("opacity").ValueText, Is.EqualTo(".2"));
            Assert.That(result.Find("opacity").Winner.Origin, Is.EqualTo(SvgStyleOrigin.User));
            Assert.That(result.Candidates.Any(candidate => candidate.SourceKind == SvgCascadeSourceKind.PresentationAttribute && candidate.PropertyName == "fill"), Is.True);
            Assert.That(result.Candidates.Any(candidate => candidate.SourceKind == SvgCascadeSourceKind.StyleAttribute && candidate.PropertyName == "opacity"), Is.True);
        });
    }

    [Test]
    public void CustomPropertiesResolveTransitivelyIntoTypedComputedValuesAndRecovery()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgNode rect = document.DocumentElement.Children[0].Children[0];
        string css = "g { --base:rgb(255 0 0); --brand:var(--base); } " +
            "rect { fill:var(--brand); stroke-width:var(--width, 2px); opacity:var(--missing, .4); paint-order:fill fill; }";
        SvgStyleResolver resolver = new SvgStyleResolver();

        SvgComputedStyle style = resolver.ResolveWithOptions(rect, new List<SvgStyleSheet> { Sheet(css, SvgStyleOrigin.Author) }, new SvgStyleResolverOptions());

        Assert.Multiple(() =>
        {
            Assert.That(style.Success, Is.True);
            Assert.That(style.GetPaint("fill").Color.Red, Is.EqualTo(1));
            Assert.That(style.GetLength("stroke-width").ToText(), Is.EqualTo("2px"));
            Assert.That(style.GetNumber("opacity").Value, Is.EqualTo(.4));
            Assert.That(style.Find("fill").Dependencies, Is.EqualTo(new[] { "--brand", "--base" }));
            Assert.That(style.Find("stroke-width").Dependencies, Is.EqualTo(new[] { "--width" }));
            Assert.That(style.CustomProperties.Get("--brand"), Is.Not.Null);
            Assert.That(style.Find("paint-order").Recovered, Is.True);
            Assert.That(style.Find("paint-order").CssText, Is.EqualTo("fill stroke markers"));
            Assert.That(style.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
        });
    }

    [Test]
    public void ExplicitInvalidationCoversEveryCssDependencyCategory()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect class='hot'/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet("g { --tone:red; } rect.hot { fill:var(--tone); }", SvgStyleOrigin.Author);
        List<SvgStyleSheet> sheets = new List<SvgStyleSheet> { sheet };
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();

        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult attribute = resolver.InvalidateAttribute(rect, "data-state");
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult classOrId = resolver.InvalidateAttribute(rect, "class");
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult tree = resolver.InvalidateTree(group);
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult styleSheet = resolver.InvalidateStyleSheet(sheet);
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult customProperty = resolver.InvalidateCustomProperty(group, "--tone");
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult viewport = resolver.InvalidateViewport(options.StyleEnvironment);
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult media = resolver.InvalidateMedia(options.StyleEnvironment);
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult fonts = resolver.InvalidateFonts();
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult interaction = resolver.InvalidateInteraction(options.SelectorOptions.DynamicState);
        resolver.ResolveWithOptions(rect, sheets, options);
        SvgStyleInvalidationResult all = resolver.InvalidateAll();

        SvgStyleInvalidationResult[] results = new[] { attribute, classOrId, tree, styleSheet, customProperty, viewport, media, fonts, interaction, all };
        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.Kind), Is.EqualTo(new[]
            {
                SvgStyleInvalidationKind.Attribute,
                SvgStyleInvalidationKind.ClassOrId,
                SvgStyleInvalidationKind.Tree,
                SvgStyleInvalidationKind.StyleSheet,
                SvgStyleInvalidationKind.CustomProperty,
                SvgStyleInvalidationKind.Viewport,
                SvgStyleInvalidationKind.Media,
                SvgStyleInvalidationKind.Fonts,
                SvgStyleInvalidationKind.Interaction,
                SvgStyleInvalidationKind.All
            }));
            Assert.That(results.All(result => result.RemovedEntries > 0), Is.True);
            Assert.That(resolver.GetStatistics().CacheEntries, Is.Zero);
            Assert.That(resolver.GetStatistics().InvalidatedEntries, Is.EqualTo(results.Sum(result => result.RemovedEntries)));
        });
    }

    static SvgStyleSheet Sheet(string css, SvgStyleOrigin origin)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), origin, new SvgSourceUri("conformance.css"));
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
