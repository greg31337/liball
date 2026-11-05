namespace svg.Tests;

public class SvgSelectorPseudoTests
{
    [Test]
    public void StructuralPseudoClassesAreTaggedInAuthoredOrder()
    {
        string input = ":root:scope:empty:first-child:last-child:only-child:first-of-type:last-of-type:only-of-type";

        SvgSelectorParseResult result = Parse(input);
        List<SvgSimpleSelector> pseudos = result.Selector.Items[0].Compounds[0].SimpleSelectors;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudos.Select(pseudo => pseudo.Kind).Distinct(), Is.EqualTo(new[] { SvgSimpleSelectorKind.PseudoClass }));
            Assert.That(pseudos.Select(pseudo => pseudo.PseudoClass), Is.EqualTo(new[]
            {
                SvgPseudoClassKind.Root,
                SvgPseudoClassKind.Scope,
                SvgPseudoClassKind.Empty,
                SvgPseudoClassKind.FirstChild,
                SvgPseudoClassKind.LastChild,
                SvgPseudoClassKind.OnlyChild,
                SvgPseudoClassKind.FirstOfType,
                SvgPseudoClassKind.LastOfType,
                SvgPseudoClassKind.OnlyOfType
            }));
            Assert.That(pseudos.All(pseudo => pseudo.SpecificityClasses == 1), Is.True);
            Assert.That(pseudos.All(pseudo => pseudo.Nth == null && pseudo.SelectorArgument == null), Is.True);
            Assert.That(result.Selector.RawText, Is.EqualTo(input));
        });
    }

    [TestCase(":nth-child(odd)", 2, 1, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(even)", 2, 0, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(2n+1)", 2, 1, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(-n+3)", -1, 3, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(n)", 1, 0, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(-2n-4)", -2, -4, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-child(5)", 0, 5, SvgPseudoClassKind.NthChild)]
    [TestCase(":nth-last-child(3n - 2)", 3, -2, SvgPseudoClassKind.NthLastChild)]
    [TestCase(":nth-of-type(2n)", 2, 0, SvgPseudoClassKind.NthOfType)]
    [TestCase(":nth-last-of-type(even)", 2, 0, SvgPseudoClassKind.NthLastOfType)]
    public void NthPseudoClassesParseAnPlusBForms(string input, int a, int b, SvgPseudoClassKind kind)
    {
        SvgSelectorParseResult result = Parse(input);
        SvgSimpleSelector pseudo = result.Selector.Items[0].Compounds[0].SimpleSelectors[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudo.PseudoClass, Is.EqualTo(kind));
            Assert.That(pseudo.Nth.A, Is.EqualTo(a));
            Assert.That(pseudo.Nth.B, Is.EqualTo(b));
            Assert.That(pseudo.Nth.OfSelector, Is.Null);
            Assert.That(pseudo.Nth.IsValid, Is.True);
            Assert.That(pseudo.SpecificityClasses, Is.EqualTo(1));
        });
    }

    [Test]
    public void NthChildOfSelectorsRetainFiltersAndUseTheirMaximumSpecificity()
    {
        SvgSelectorParseResult result = Parse(":nth-child(2n+1 of .item, #main)");
        SvgSimpleSelector pseudo = result.Selector.Items[0].Compounds[0].SimpleSelectors[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudo.Nth.A, Is.EqualTo(2));
            Assert.That(pseudo.Nth.B, Is.EqualTo(1));
            Assert.That(pseudo.Nth.OfSelector.Items, Has.Count.EqualTo(2));
            Assert.That(pseudo.Nth.OfSelector.Items[0].RawText, Is.EqualTo(".item"));
            Assert.That(pseudo.Nth.OfSelector.Items[1].RawText, Is.EqualTo("#main"));
            Assert.That(pseudo.SpecificityIds, Is.EqualTo(1));
            Assert.That(pseudo.SpecificityClasses, Is.EqualTo(1));
            Assert.That(pseudo.SpecificityTypes, Is.Zero);
        });

        SvgSelectorParseResult invalidTypeFilter = Parse(":nth-of-type(2n of .item)");
        Assert.That(invalidTypeFilter.Error, Is.EqualTo(SvgSelectorParseError.InvalidNthExpression));
    }

    [Test]
    public void LangAcceptsIdentifierStringWildcardAndCommaSeparatedRanges()
    {
        SvgSelectorParseResult result = Parse(":lang(en, \"fr-CA\", *)");
        SvgSimpleSelector pseudo = result.Selector.Items[0].Compounds[0].SimpleSelectors[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudo.PseudoClass, Is.EqualTo(SvgPseudoClassKind.Lang));
            Assert.That(pseudo.Languages, Is.EqualTo(new[] { "en", "fr-CA", "*" }));
            Assert.That(pseudo.SpecificityClasses, Is.EqualTo(1));
            Assert.That(pseudo.RawText, Is.EqualTo(":lang(en, \"fr-CA\", *)"));
        });
    }

    [Test]
    public void SelectorListPseudosApplyLexicographicMaximumAndWhereContributesZero()
    {
        SvgSelectorParseResult result = Parse(":is(.a, #b):not(rect, [x]):where(#zero, .many.classes)");
        List<SvgSimpleSelector> pseudos = result.Selector.Items[0].Compounds[0].SimpleSelectors;
        SvgSimpleSelector isPseudo = pseudos[0];
        SvgSimpleSelector notPseudo = pseudos[1];
        SvgSimpleSelector wherePseudo = pseudos[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(isPseudo.SelectorArgument.Items, Has.Count.EqualTo(2));
            Assert.That(isPseudo.SpecificityIds, Is.EqualTo(1));
            Assert.That(isPseudo.SpecificityClasses, Is.Zero);
            Assert.That(notPseudo.SpecificityIds, Is.Zero);
            Assert.That(notPseudo.SpecificityClasses, Is.EqualTo(1));
            Assert.That(notPseudo.SpecificityTypes, Is.Zero);
            Assert.That(wherePseudo.SpecificityIds, Is.Zero);
            Assert.That(wherePseudo.SpecificityClasses, Is.Zero);
            Assert.That(wherePseudo.SpecificityTypes, Is.Zero);
        });
    }

    [Test]
    public void ForgivingSelectorListsDiscardInvalidBranchesButNotValidOnes()
    {
        SvgSelectorParseResult result = Parse(":is(:unknown, .kept):where(, #also-kept,):has(:has(.bad), > rect)");
        List<SvgSimpleSelector> pseudos = result.Selector.Items[0].Compounds[0].SimpleSelectors;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudos[0].SelectorArgument.Items.Select(item => item.RawText), Is.EqualTo(new[] { ".kept" }));
            Assert.That(pseudos[1].SelectorArgument.Items.Select(item => item.RawText), Is.EqualTo(new[] { "#also-kept" }));
            Assert.That(pseudos[2].SelectorArgument.Items.Select(item => item.RawText), Is.EqualTo(new[] { "> rect" }));
            Assert.That(pseudos[2].SelectorArgument.Items[0].IsRelative, Is.True);
            Assert.That(pseudos[2].SelectorArgument.Items[0].LeadingCombinator, Is.EqualTo(SvgSelectorCombinator.Child));
        });
    }

    [Test]
    public void HasParsesBoundedRelativeSelectorListsAndLeadingCombinators()
    {
        SvgSelectorParseResult result = Parse(":has(> rect, + .next, ~ #later, path .desc)");
        SvgSimpleSelector pseudo = result.Selector.Items[0].Compounds[0].SimpleSelectors[0];
        List<SvgComplexSelector> relatives = pseudo.SelectorArgument.Items;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pseudo.PseudoClass, Is.EqualTo(SvgPseudoClassKind.Has));
            Assert.That(relatives, Has.Count.EqualTo(4));
            Assert.That(relatives.All(selector => selector.IsRelative), Is.True);
            Assert.That(relatives.Select(selector => selector.LeadingCombinator), Is.EqualTo(new[]
            {
                SvgSelectorCombinator.Child,
                SvgSelectorCombinator.AdjacentSibling,
                SvgSelectorCombinator.GeneralSibling,
                SvgSelectorCombinator.Descendant
            }));
            Assert.That(relatives[3].Combinators, Is.EqualTo(new[] { SvgSelectorCombinator.Descendant }));
            Assert.That(pseudo.SpecificityIds, Is.EqualTo(1));
            Assert.That(pseudo.SpecificityClasses, Is.Zero);
        });
    }

    [TestCase(":lang()", SvgSelectorParseError.ExpectedPseudoArgument)]
    [TestCase(":lang(en fr)", SvgSelectorParseError.ExpectedPseudoArgument)]
    [TestCase(":nth-child()", SvgSelectorParseError.InvalidNthExpression)]
    [TestCase(":nth-child(2n+)", SvgSelectorParseError.InvalidNthExpression)]
    [TestCase(":nth-child(999999999999n)", SvgSelectorParseError.InvalidNthExpression)]
    [TestCase(":nth-of-type(2n of )", SvgSelectorParseError.InvalidNthExpression)]
    [TestCase(":root()", SvgSelectorParseError.ExpectedPseudoClass)]
    [TestCase(":unknown", SvgSelectorParseError.ExpectedPseudoClass)]
    [TestCase("::before", SvgSelectorParseError.ExpectedPseudoClass)]
    [TestCase(":not()", SvgSelectorParseError.ExpectedSelector)]
    [TestCase(":has(:has(.x))", SvgSelectorParseError.NestedHasNotAllowed)]
    public void MalformedOrUnsupportedPseudosReturnStableFailures(string input, SvgSelectorParseError expected)
    {
        SvgSelectorParseResult result = Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(expected));
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Selector.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void NestedSelectorPseudoDepthUsesTheConfiguredBound()
    {
        SvgCssLimits oneLevel = Limits();
        oneLevel.MaxBlockDepth = 1;
        SvgCssLimits invalidLimits = Limits();
        invalidLimits.MaxBlockDepth = 0;

        SvgSelectorParseResult valid = new SvgSelectorParser().ParseWithValueAndLimits(":is(.x)", oneLevel);
        SvgSelectorParseResult invalid = new SvgSelectorParser().ParseWithValueAndLimits(":is(:not(.x))", oneLevel);
        SvgSelectorParseResult invalidArguments = new SvgSelectorParser().ParseWithValueAndLimits(":root", invalidLimits);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgSelectorParseError.PseudoDepthExceeded));
            Assert.That(invalid.Selector.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
            Assert.That(invalidArguments.Error, Is.EqualTo(SvgSelectorParseError.InvalidArgument));
        });
    }

    [Test]
    public void StylesheetRulesReceiveValidStructuralAndSelectorListPseudos()
    {
        string input = ":root > g:not(.hidden):has(> rect):nth-child(odd of .item) { fill:red; }";

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(input, Limits());
        SvgCssRule rule = result.StyleSheet.Rules[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(rule.IsValid, Is.True);
            Assert.That(rule.Selectors.IsValid, Is.True);
            Assert.That(rule.Selectors.Items[0].Combinators, Is.EqualTo(new[] { SvgSelectorCombinator.Child }));
            Assert.That(rule.Selectors.Items[0].Compounds[1].SimpleSelectors.Select(selector => selector.PseudoClass), Does.Contain(SvgPseudoClassKind.Has));
            Assert.That(rule.Declarations.Single().Name, Is.EqualTo("fill"));
        });
    }

    static SvgSelectorParseResult Parse(string input)
    {
        return new SvgSelectorParser().ParseWithValueAndLimits(input, Limits());
    }

    static SvgCssLimits Limits()
    {
        return new SvgCssLimits();
    }
}
