namespace svg.Tests;

public class SvgSelectorMatcherTests
{
    [Test]
    public void ParsedSelectorListsExposeLexicographicMaximumSpecificity()
    {
        SvgSelectorParseResult result = Parse("g#outer.a[x]:hover:is(.minor, #inner):where(#zero), rect.many.classes");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Selector.Items[0].IdSpecificity, Is.EqualTo(2));
            Assert.That(result.Selector.Items[0].ClassSpecificity, Is.EqualTo(3));
            Assert.That(result.Selector.Items[0].TypeSpecificity, Is.EqualTo(1));
            Assert.That(result.Selector.Items[1].IdSpecificity, Is.Zero);
            Assert.That(result.Selector.Items[1].ClassSpecificity, Is.EqualTo(2));
            Assert.That(result.Selector.Items[1].TypeSpecificity, Is.EqualTo(1));
            Assert.That(result.Selector.IdSpecificity, Is.EqualTo(2));
            Assert.That(result.Selector.ClassSpecificity, Is.EqualTo(3));
            Assert.That(result.Selector.TypeSpecificity, Is.EqualTo(1));
        });
    }

    [Test]
    public void MatchReportsTheStrongestSpecificityAmongMatchingListItems()
    {
        SvgDocument document = Load("<svg><rect id='item' class='hot'/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];

        SvgSelectorMatchResult result = Match("rect, .hot, #item", rect, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.Matches, Is.True);
            Assert.That(result.IdSpecificity, Is.EqualTo(1));
            Assert.That(result.ClassSpecificity, Is.Zero);
            Assert.That(result.TypeSpecificity, Is.Zero);
        });
    }

    [Test]
    public void RightToLeftMatchingSupportsNamesAttributesAndEveryCombinator()
    {
        SvgDocument document = Load("<svg><g id='group'><rect id='first' class='hot' data-v='Alpha Beta'/><circle/><rect id='last' data-v='alphabet'/><path/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode first = group.Children[0];
        SvgNode last = group.Children[2];
        SvgSelectorMatchOptions options = Options();

        Assert.Multiple(() =>
        {
            Assert.That(Match("svg > g#group > rect.hot[data-v~='beta' i]", first, options).Matches, Is.True);
            Assert.That(Match("rect + circle + rect[data-v^='ALP' i]", last, options).Matches, Is.True);
            Assert.That(Match("rect.hot ~ rect[data-v$='bet']", last, options).Matches, Is.True);
            Assert.That(Match("svg path", group.Children[3], options).Matches, Is.True);
            Assert.That(Match("circle > rect", last, options).Matches, Is.False);
        });
    }

    [Test]
    public void NamespaceBindingsConstrainTypesUniversalNamesAndAttributes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x'><g><rect id='svg' x:flag='yes'/><x:rect id='foreign' x:flag='yes'/></g></svg>");
        SvgNode svgRect = document.DocumentElement.Children[0].Children[0];
        SvgNode foreignRect = document.DocumentElement.Children[0].Children[1];
        SvgSelectorMatchOptions options = Options();
        SvgSelectorMatchOptions unrestricted = Options();
        options.Namespaces.Bind("s", SvgNamespaces.Svg);
        options.Namespaces.Bind("x", "urn:x");

        SvgSelectorMatchResult unresolved = Match("missing|rect", svgRect, options);
        options.Namespaces.SetDefault(SvgNamespaces.Svg);

        Assert.Multiple(() =>
        {
            Assert.That(Match("s|rect[x|flag='yes']", svgRect, options).Matches, Is.True);
            Assert.That(Match("x|rect", foreignRect, options).Matches, Is.True);
            Assert.That(Match("*|rect", foreignRect, options).Matches, Is.True);
            Assert.That(Match("rect", foreignRect, unrestricted).Matches, Is.True);
            Assert.That(Match("rect", svgRect, options).Matches, Is.True);
            Assert.That(Match("rect", foreignRect, options).Matches, Is.False);
            Assert.That(Match("|rect", foreignRect, options).Matches, Is.False);
            Assert.That(unresolved.Matches, Is.False);
            Assert.That(unresolved.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
        });
    }

    [Test]
    public void StructuralLanguageAndSelectorListPseudosMatchDocumentState()
    {
        SvgDocument document = Load("<svg xml:lang='en-US'><g id='one'><rect id='a'/><rect id='b'/><circle/><rect id='c'/></g><g id='two'><rect id='d'/></g></svg>");
        SvgQuery query = new SvgQuery();
        SvgSelectorMatchOptions options = Options();
        SvgNode firstGroup = document.DocumentElement.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(query.QuerySelectorText(document, ":root", new SvgCssLimits(), options).First, Is.SameAs(document.DocumentElement));
            Assert.That(query.QuerySelectorAllText(document, ":scope > g", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(query.QuerySelectorAllText(document, "rect:first-child", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "a", "d" }));
            Assert.That(query.QuerySelectorAllText(document, "rect:nth-child(2 of rect)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "b" }));
            Assert.That(query.QuerySelectorAllText(document, "rect:nth-last-of-type(1)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "c", "d" }));
            Assert.That(query.QuerySelectorAllText(document, "rect:lang(en)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(query.QuerySelectorAllText(document, "rect:is(#a, #c):not(#b)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "a", "c" }));
            Assert.That(query.QuerySelectorAllTextFromNode(firstGroup, ":scope > rect", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "a", "b", "c" }));
        });
    }

    [Test]
    public void EmptyAndOnlyPseudosIgnoreCommentsButNotTextOrElements()
    {
        SvgDocument document = Load("<svg><g id='empty'><!--ok--></g><g id='text'> </g><g id='child'><rect/></g><g id='only'><circle/></g></svg>");
        SvgQuery query = new SvgQuery();
        SvgSelectorMatchOptions options = Options();

        Assert.Multiple(() =>
        {
            Assert.That(query.QuerySelectorAllText(document, "g:empty", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "empty" }));
            Assert.That(query.QuerySelectorAllText(document, "g > :only-child", new SvgCssLimits(), options).Nodes.Select(node => node.LocalName), Is.EqualTo(new[] { "rect", "circle" }));
            Assert.That(query.QuerySelectorAllText(document, "circle:only-of-type", new SvgCssLimits(), options).Nodes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void HasMatchesBoundedForwardRelativeSelectors()
    {
        SvgDocument document = Load("<svg><g id='first'><rect/><circle class='hit'/></g><g id='second'><path><circle class='deep'/></path></g><g id='third'/></svg>");
        SvgQuery query = new SvgQuery();
        SvgSelectorMatchOptions options = Options();

        Assert.Multiple(() =>
        {
            Assert.That(query.QuerySelectorAllText(document, "g:has(> rect + circle.hit)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "first" }));
            Assert.That(query.QuerySelectorAllText(document, "g:has(circle.deep)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "second" }));
            Assert.That(query.QuerySelectorAllText(document, "g:has(+ g#third)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "second" }));
            Assert.That(query.QuerySelectorAllText(document, "g:has(~ g#third)", new SvgCssLimits(), options).Nodes.Select(NodeId), Is.EqualTo(new[] { "first", "second" }));
        });
    }

    [Test]
    public void QueryIntegrationUsesHostDynamicStateAndDocumentOrder()
    {
        SvgDocument document = Load("<svg><rect id='one'/><rect id='two'/><rect id='three'/></svg>");
        SvgNode hovered = document.DocumentElement.Children[1];
        SvgSelectorMatchOptions options = Options();
        SvgDynamicSelectorState dynamicState = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        dynamicState.HoverState = node => ReferenceEquals(node, hovered);
        options.DynamicState = dynamicState;
        SvgQuery query = new SvgQuery();

        SvgSelectorQueryResult first = query.QuerySelectorText(document, "rect:hover", new SvgCssLimits(), options);
        SvgSelectorQueryResult all = query.QuerySelectorAllText(document, "rect", new SvgCssLimits(), options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.First, Is.SameAs(hovered));
            Assert.That(first.Nodes, Is.EqualTo(new[] { hovered }));
            Assert.That(all.Nodes.Select(NodeId), Is.EqualTo(new[] { "one", "two", "three" }));
        });
    }

    [Test]
    public void InvalidSelectorsAndUnmatchableDynamicPseudosReturnDiagnostics()
    {
        SvgDocument document = Load("<svg><rect/></svg>");
        SvgQuery query = new SvgQuery();
        SvgSelectorMatchOptions options = Options();

        SvgSelectorQueryResult invalid = query.QuerySelectorAllText(document, "rect:unknown", new SvgCssLimits(), options);
        SvgSelectorQueryResult unsupported = query.QuerySelectorAllText(document, "rect:focus-visible", new SvgCssLimits(), options);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Nodes, Is.Empty);
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
            Assert.That(unsupported.Success, Is.True);
            Assert.That(unsupported.Nodes, Is.Empty);
            Assert.That(unsupported.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.UnsupportedFeature), Is.True);
        });
    }

    [Test]
    public void MatchingWorkLimitStopsAdversarialQueriesDeterministically()
    {
        SvgDocument document = Load("<svg><g><g><g><rect/></g></g></g><circle/></svg>");
        SvgSelectorMatchOptions options = Options();
        options.MaxWork = 5;
        SvgQuery query = new SvgQuery();

        SvgSelectorQueryResult result = query.QuerySelectorAllText(document, "svg g g g rect.missing", new SvgCssLimits(), options);
        SvgNodeApi mutations = new SvgNodeApi();
        SvgDocumentApi factory = new SvgDocumentApi();
        SvgNode circle = document.DocumentElement.Children[1];
        mutations.AppendAttribute(circle, factory.CreateAttribute("data-v", "", "data-v", "", new string('a', 100)));
        SvgSelectorMatchOptions valueOptions = Options();
        valueOptions.MaxWork = 50;
        SvgSelectorMatchResult valueResult = Match("[data-v*='aaaaaaaa']", circle, valueOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LimitExceeded, Is.True);
            Assert.That(result.Work, Is.EqualTo(5));
            Assert.That(result.Nodes, Is.Empty);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(valueResult.Success, Is.False);
            Assert.That(valueResult.LimitExceeded, Is.True);
            Assert.That(valueResult.Work, Is.EqualTo(50));
        });
    }

    static SvgSelectorMatchResult Match(string selector, SvgNode node, SvgSelectorMatchOptions options)
    {
        return new SvgSelectorMatcher().Match(Parse(selector).Selector, node, options);
    }

    static SvgSelectorParseResult Parse(string selector)
    {
        return new SvgSelectorParser().ParseWithValueAndLimits(selector, new SvgCssLimits());
    }

    static SvgSelectorMatchOptions Options()
    {
        return new SvgSelectorMatchOptions();
    }

    static string NodeId(SvgNode node)
    {
        SvgAttribute attribute = node.Attributes.FirstOrDefault(item => item.NamespaceUri == "" && item.LocalName == "id");
        return attribute == null ? "" : attribute.Value;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
