namespace svg.Tests;

public class SvgSelectorStateTests
{
    [Test]
    public void SupportedDynamicPseudosParseInAuthoredOrder()
    {
        SvgSelectorParseResult result = Parse(":link:visited:hover:active:focus:target");
        List<SvgSimpleSelector> pseudos = result.Selector.Items[0].Compounds[0].SimpleSelectors;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Selector.Diagnostics, Is.Empty);
            Assert.That(pseudos.Select(item => item.PseudoClass), Is.EqualTo(new[]
            {
                SvgPseudoClassKind.Link,
                SvgPseudoClassKind.Visited,
                SvgPseudoClassKind.Hover,
                SvgPseudoClassKind.Active,
                SvgPseudoClassKind.Focus,
                SvgPseudoClassKind.Target
            }));
            Assert.That(pseudos.All(item => item.SpecificityClasses == 1), Is.True);
        });
    }

    [Test]
    public void HostCallbacksDriveSupportedDynamicMatches()
    {
        SvgNode node = Element("rect");
        SvgNode other = Element("circle");
        List<SvgSimpleSelector> pseudos = Parse(":link:visited:hover:active:focus:target").Selector.Items[0].Compounds[0].SimpleSelectors;
        SvgDynamicSelectorState state = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        state.LinkState = candidate => candidate == node ? SvgSelectorLinkState.Visited : SvgSelectorLinkState.None;
        state.HoverState = candidate => candidate == node;
        state.ActiveState = candidate => candidate == node;
        state.FocusState = candidate => candidate == node;
        state.TargetState = candidate => candidate == node;

        Assert.Multiple(() =>
        {
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[0], node, state).Matches, Is.False);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[1], node, state).Matches, Is.True);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[2], node, state).Matches, Is.True);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[3], node, state).Matches, Is.True);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[4], node, state).Matches, Is.True);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[5], node, state).Matches, Is.True);
            Assert.That(pseudos.Select(item => new SvgDynamicSelectorMatcher().Match(item, other, state).Matches), Has.All.False);
        });

        state.LinkState = candidate => candidate == node ? SvgSelectorLinkState.Unvisited : SvgSelectorLinkState.None;
        Assert.Multiple(() =>
        {
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[0], node, state).Matches, Is.True);
            Assert.That(new SvgDynamicSelectorMatcher().Match(pseudos[1], node, state).Matches, Is.False);
        });
    }

    [Test]
    public void MissingHostStateSafelyDoesNotMatch()
    {
        SvgNode node = Element("rect");
        SvgDynamicSelectorState state = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        List<SvgSimpleSelector> pseudos = Parse(":link:visited:hover:active:focus:target").Selector.Items[0].Compounds[0].SimpleSelectors;

        List<SvgDynamicSelectorMatchResult> results = pseudos.Select(item => new SvgDynamicSelectorMatcher().Match(item, node, state)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.Matches), Has.All.False);
            Assert.That(results.Select(result => result.Supported), Has.All.True);
            Assert.That(results.SelectMany(result => result.Diagnostics), Is.Empty);
        });
    }

    [Test]
    public void DynamicPseudosNeverMatchNonElementNodes()
    {
        SvgSimpleSelector hover = Parse(":hover").Selector.Items[0].Compounds[0].SimpleSelectors[0];
        SvgDynamicSelectorState state = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        state.HoverState = candidate => true;
        SvgNode text = new SvgNode(SvgNodeKind.Text, "", "", "", "", "content");

        SvgDynamicSelectorMatchResult result = new SvgDynamicSelectorMatcher().Match(hover, text, state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Matches, Is.False);
            Assert.That(result.Supported, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void UnsupportedDynamicPseudoIsPreservedWarnedAndNeverMatches()
    {
        SvgSelectorParseResult parsed = Parse(":focus-visible");
        SvgSimpleSelector pseudo = parsed.Selector.Items[0].Compounds[0].SimpleSelectors[0];
        SvgDynamicSelectorMatchResult matched = new SvgDynamicSelectorMatcher().Match(pseudo, Element("rect"), new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(pseudo.PseudoClass, Is.EqualTo(SvgPseudoClassKind.UnsupportedDynamic));
            Assert.That(pseudo.PseudoName, Is.EqualTo("focus-visible"));
            Assert.That(pseudo.SpecificityClasses, Is.EqualTo(1));
            Assert.That(parsed.Selector.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(parsed.Selector.Diagnostics[0].Severity, Is.EqualTo(SvgDiagnosticSeverity.Warning));
            Assert.That(parsed.Selector.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedFeature));
            Assert.That(matched.Matches, Is.False);
            Assert.That(matched.Supported, Is.False);
            Assert.That(matched.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedFeature));
        });
    }

    [Test]
    public void NestedUnsupportedDynamicWarningsReachStylesheetDiagnostics()
    {
        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits("g:is(:focus-within, :hover) { fill: red; }", new SvgCssLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StyleSheet.Rules[0].IsValid, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedFeature));
            Assert.That(result.Diagnostics[0].Source.StartOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void InvalidHostLinkStateSafelyDoesNotMatch()
    {
        SvgSimpleSelector link = Parse(":link").Selector.Items[0].Compounds[0].SimpleSelectors[0];
        SvgDynamicSelectorState state = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        state.LinkState = candidate => (SvgSelectorLinkState)99;

        SvgDynamicSelectorMatchResult result = new SvgDynamicSelectorMatcher().Match(link, Element("a"), state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Matches, Is.False);
            Assert.That(result.Supported, Is.True);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    static SvgSelectorParseResult Parse(string value)
    {
        return new SvgSelectorParser().ParseWithValueAndLimits(value, new SvgCssLimits());
    }

    static SvgNode Element(string name)
    {
        return new SvgNode(SvgNodeKind.Element, name, "", name, "http://www.w3.org/2000/svg", "");
    }
}
