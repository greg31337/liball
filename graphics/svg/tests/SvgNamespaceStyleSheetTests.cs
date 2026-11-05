namespace svg.Tests;

public class SvgNamespaceStyleSheetTests
{
    [Test]
    public void NamespaceRulesDecodeDefaultPrefixedStringAndUrlBindings()
    {
        string css = "@namespace \"urn:default\"; @namespace s url(http://www.w3.org/2000/svg); @namespace x url(\"urn:x\"); s|rect, x|item {}";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        SvgStyleSheet sheet = result.StyleSheet;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(sheet.IsValid, Is.True);
            Assert.That(sheet.Namespaces.HasDefaultNamespace, Is.True);
            Assert.That(sheet.Namespaces.DefaultNamespace, Is.EqualTo("urn:default"));
            Assert.That(sheet.Namespaces.NamedNamespaces["s"], Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(sheet.Namespaces.NamedNamespaces["x"], Is.EqualTo("urn:x"));
            Assert.That(sheet.Rules.Take(3).Select(rule => rule.AtRule.Kind), Has.All.EqualTo(SvgCssAtRuleKind.Namespace));
            Assert.That(sheet.Rules[0].AtRule.NamespacePrefix, Is.Empty);
            Assert.That(sheet.Rules[0].AtRule.NamespaceUri, Is.EqualTo("urn:default"));
            Assert.That(sheet.Rules[1].AtRule.NamespacePrefix, Is.EqualTo("s"));
            Assert.That(sheet.Rules[2].AtRule.NamespaceUri, Is.EqualTo("urn:x"));
        });
    }

    [Test]
    public void LaterNamespaceDeclarationsReplaceBindingsWithinOnlyTheirSheet()
    {
        SvgStyleSheet first = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@namespace p \"urn:one\"; @namespace p \"urn:two\"; p|item {}", new SvgCssLimits()).StyleSheet;
        SvgStyleSheet second = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@namespace p \"urn:other\"; p|item {}", new SvgCssLimits()).StyleSheet;

        Assert.Multiple(() =>
        {
            Assert.That(first.Namespaces.NamedNamespaces["p"], Is.EqualTo("urn:two"));
            Assert.That(second.Namespaces.NamedNamespaces["p"], Is.EqualTo("urn:other"));
            Assert.That(first.Namespaces, Is.Not.SameAs(second.Namespaces));
        });
    }

    [TestCase("@namespace;", "optional prefix")]
    [TestCase("@namespace p;", "string or URL")]
    [TestCase("@namespace p \"urn:x\" extra;", "optional prefix")]
    [TestCase("@namespace p \"urn:x\" {}", "cannot have a block")]
    [TestCase("@namespace p \"urn:x\"", "requires a semicolon")]
    public void MalformedNamespaceRulesRemainLocatedAndInvalidateTheSheet(string css, string message)
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        SvgCssRule rule = result.StyleSheet.Rules[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.IsValid, Is.False);
            Assert.That(rule.IsValid, Is.False);
            Assert.That(rule.AtRule.IsValid, Is.False);
            Assert.That(rule.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
            Assert.That(rule.Diagnostics[0].Message, Does.Contain(message));
            Assert.That(rule.Diagnostics[0].Source, Is.SameAs(rule.Source));
        });
    }

    [Test]
    public void NamespaceRulesAfterBodyRulesAreInvalidAndDoNotChangeBindings()
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits("rect {} @namespace late \"urn:late\"; late|circle {}", new SvgCssLimits());
        SvgCssRule namespaceRule = result.StyleSheet.Rules[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.IsValid, Is.False);
            Assert.That(namespaceRule.IsValid, Is.False);
            Assert.That(namespaceRule.Diagnostics.Single().Message, Does.Contain("must precede"));
            Assert.That(result.StyleSheet.Namespaces.NamedNamespaces.ContainsKey("late"), Is.False);
        });
    }

    [Test]
    public void SheetBindingsDriveNamespaceAwareSelectorMatching()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x'><rect/><x:item/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgNode item = document.DocumentElement.Children[1];
        SvgStyleSheet sheet = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@namespace s \"http://www.w3.org/2000/svg\"; @namespace x \"urn:x\"; s|rect, x|item {}", new SvgCssLimits()).StyleSheet;
        SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
        options.Namespaces = sheet.Namespaces;

        Assert.Multiple(() =>
        {
            Assert.That(new SvgSelectorMatcher().Match(sheet.Rules[2].Selectors, rect, options).Matches, Is.True);
            Assert.That(new SvgSelectorMatcher().Match(sheet.Rules[2].Selectors, item, options).Matches, Is.True);
        });
    }

    [Test]
    public void CollectorOrdersEmbeddedThenCallerSheetsAndRetainsProvenance()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:h='http://www.w3.org/1999/xhtml'>" +
            "<style media='screen'>@namespace s 'http://www.w3.org/2000/svg'; s|rect { fill:red; }</style>" +
            "<g><style><![CDATA[s|circle { fill:blue; }]]></style></g>" +
            "<h:style>foreign {}</h:style><style type='text/less'>ignored {}</style><rect/></svg>";
        SvgDocument document = Load(source);
        SvgSourceUri callerSource = new SvgSourceUri("caller.css");
        SvgStyleSheet caller = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource("@namespace c 'urn:caller'; c|item {}", new SvgCssLimits(), SvgStyleOrigin.User, callerSource).StyleSheet;

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, new List<SvgStyleSheet> { caller }, new SvgCssLimits());
        SvgCollectedStyleSheet first = collection.StyleSheets[0];
        SvgCollectedStyleSheet second = collection.StyleSheets[1];
        SvgCollectedStyleSheet third = collection.StyleSheets[2];

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.True);
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(3));
            Assert.That(collection.StyleSheets.Select(entry => entry.CollectionOrder), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(collection.StyleSheets.Select(entry => entry.SourceKind), Is.EqualTo(new[] { SvgStyleSheetSourceKind.Embedded, SvgStyleSheetSourceKind.Embedded, SvgStyleSheetSourceKind.Caller }));
            Assert.That(first.Media, Is.EqualTo("screen"));
            Assert.That(first.SourceNode.ElementKind, Is.EqualTo(SvgElementKind.Style));
            Assert.That(second.SourceNode.Parent.LocalName, Is.EqualTo("g"));
            Assert.That(third.SourceNode, Is.Null);
            Assert.That(third.StyleSheet, Is.SameAs(caller));
            Assert.That(first.StyleSheet.Source.Source.Parent, Is.SameAs(document.Source));
            Assert.That(first.StyleSheet.Source.Source.ReferenceSpan, Is.SameAs(first.SourceNode.SourceSpan));
            Assert.That(first.ReferenceSource, Is.SameAs(first.SourceNode.SourceSpan));
            Assert.That(third.StyleSheet.Source.Source, Is.SameAs(callerSource));
            Assert.That(collection.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedFeature));
        });
    }

    [Test]
    public void EmbeddedNamespaceBindingsDoNotLeakBetweenCollectedSheets()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>@namespace s 'http://www.w3.org/2000/svg'; s|rect {}</style><style>s|rect {}</style><rect/></svg>");
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, new List<SvgStyleSheet>(), new SvgCssLimits());
        SvgNode rect = document.DocumentElement.Children[2];
        SvgSelectorMatchOptions firstOptions = new SvgSelectorMatchOptions();
        firstOptions.Namespaces = collection.StyleSheets[0].StyleSheet.Namespaces;
        SvgSelectorMatchOptions secondOptions = new SvgSelectorMatchOptions();
        secondOptions.Namespaces = collection.StyleSheets[1].StyleSheet.Namespaces;

        SvgSelectorMatchResult first = new SvgSelectorMatcher().Match(collection.StyleSheets[0].StyleSheet.Rules[1].Selectors, rect, firstOptions);
        SvgSelectorMatchResult second = new SvgSelectorMatcher().Match(collection.StyleSheets[1].StyleSheet.Rules[0].Selectors, rect, secondOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Matches, Is.True);
            Assert.That(second.Matches, Is.False);
            Assert.That(second.Diagnostics.Single().Message, Does.Contain("not bound"));
        });
    }

    [Test]
    public void CollectorEnforcesCumulativeRuleLimitsAcrossStyleElements()
    {
        SvgDocument document = Load("<svg><style>rect {}</style><style>circle {}</style></svg>");
        SvgCssLimits limits = new SvgCssLimits();
        limits.MaxRules = 1;

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, new List<SvgStyleSheet>(), limits);

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.False);
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(1));
            Assert.That(collection.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(collection.Diagnostics[0].Source, Is.SameAs(document.DocumentElement.Children[1].SourceSpan));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "document.svg";
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
