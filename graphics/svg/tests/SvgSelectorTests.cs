namespace svg.Tests;

public class SvgSelectorTests
{
    [Test]
    public void TypeAndUniversalSelectorsPreserveEveryNamespaceForm()
    {
        SvgSelectorParseResult result = Parse("svg|rect, *|circle, |path, *, svg|*");
        List<SvgSimpleSelector> simple = result.Selector.Items.Select(item => item.Compounds[0].SimpleSelectors[0]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Selector.Items, Has.Count.EqualTo(5));
            Assert.That(simple.Select(selector => selector.Kind), Is.EqualTo(new[]
            {
                SvgSimpleSelectorKind.Type,
                SvgSimpleSelectorKind.Type,
                SvgSimpleSelectorKind.Type,
                SvgSimpleSelectorKind.Universal,
                SvgSimpleSelectorKind.Universal
            }));
            Assert.That(simple.Select(selector => selector.NamespaceKind), Is.EqualTo(new[]
            {
                SvgSelectorNamespaceKind.Named,
                SvgSelectorNamespaceKind.Any,
                SvgSelectorNamespaceKind.None,
                SvgSelectorNamespaceKind.Default,
                SvgSelectorNamespaceKind.Named
            }));
            Assert.That(simple.Select(selector => selector.NamespacePrefix), Is.EqualTo(new[] { "svg", "", "", "", "svg" }));
            Assert.That(simple.Select(selector => selector.Name), Is.EqualTo(new[] { "rect", "circle", "path", "*", "*" }));
        });
    }

    [Test]
    public void CompoundSelectorsDecodeIdsClassesEscapesAndAttributePresence()
    {
        SvgSelectorParseResult result = Parse("rect#main.big.\\66 oo[data-x]");
        SvgCompoundSelector compound = result.Selector.Items[0].Compounds[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(compound.SimpleSelectors.Select(selector => selector.Kind), Is.EqualTo(new[]
            {
                SvgSimpleSelectorKind.Type,
                SvgSimpleSelectorKind.Id,
                SvgSimpleSelectorKind.Class,
                SvgSimpleSelectorKind.Class,
                SvgSimpleSelectorKind.Attribute
            }));
            Assert.That(compound.SimpleSelectors.Select(selector => selector.Name), Is.EqualTo(new[] { "rect", "main", "big", "foo", "data-x" }));
            Assert.That(compound.SimpleSelectors[1].RawText, Is.EqualTo("#main"));
            Assert.That(compound.SimpleSelectors[3].RawText, Is.EqualTo(".\\66 oo"));
            Assert.That(compound.SimpleSelectors[4].AttributeOperator, Is.EqualTo(SvgAttributeSelectorOperator.Exists));
            Assert.That(compound.RawText, Is.EqualTo("rect#main.big.\\66 oo[data-x]"));
        });
    }

    [Test]
    public void AttributeSelectorsSupportEveryOperatorQuotedValuesAndCaseFlags()
    {
        string input = "[a][b=x][c~=\"word\" i][d|=en][e^=pre s][f$=end][g*=mid]";

        SvgSelectorParseResult result = Parse(input);
        List<SvgSimpleSelector> attributes = result.Selector.Items[0].Compounds[0].SimpleSelectors;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(attributes.Select(selector => selector.AttributeOperator), Is.EqualTo(new[]
            {
                SvgAttributeSelectorOperator.Exists,
                SvgAttributeSelectorOperator.Equals,
                SvgAttributeSelectorOperator.Includes,
                SvgAttributeSelectorOperator.DashMatch,
                SvgAttributeSelectorOperator.PrefixMatch,
                SvgAttributeSelectorOperator.SuffixMatch,
                SvgAttributeSelectorOperator.SubstringMatch
            }));
            Assert.That(attributes.Select(selector => selector.Value), Is.EqualTo(new[] { "", "x", "word", "en", "pre", "end", "mid" }));
            Assert.That(attributes[2].ValueIsString, Is.True);
            Assert.That(attributes[2].AttributeCase, Is.EqualTo(SvgAttributeSelectorCase.Insensitive));
            Assert.That(attributes[4].AttributeCase, Is.EqualTo(SvgAttributeSelectorCase.Sensitive));
            Assert.That(attributes[1].AttributeCase, Is.EqualTo(SvgAttributeSelectorCase.Default));
            Assert.That(result.Selector.RawText, Is.EqualTo(input));
        });
    }

    [Test]
    public void AttributeNamespacesDoNotConflictWithDashMatchOperators()
    {
        SvgSelectorParseResult result = Parse("[*|href], [xlink|href], [|lang], [lang|=en]");
        List<SvgSimpleSelector> attributes = result.Selector.Items.Select(item => item.Compounds[0].SimpleSelectors[0]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(attributes.Select(selector => selector.NamespaceKind), Is.EqualTo(new[]
            {
                SvgSelectorNamespaceKind.Any,
                SvgSelectorNamespaceKind.Named,
                SvgSelectorNamespaceKind.None,
                SvgSelectorNamespaceKind.None
            }));
            Assert.That(attributes.Select(selector => selector.NamespacePrefix), Is.EqualTo(new[] { "", "xlink", "", "" }));
            Assert.That(attributes.Select(selector => selector.Name), Is.EqualTo(new[] { "href", "href", "lang", "lang" }));
            Assert.That(attributes[3].AttributeOperator, Is.EqualTo(SvgAttributeSelectorOperator.DashMatch));
            Assert.That(attributes[3].Value, Is.EqualTo("en"));
        });
    }

    [Test]
    public void ComplexSelectorsRetainAllRequestedCombinatorsInOrder()
    {
        SvgSelectorParseResult result = Parse("svg g > rect + circle ~ path");
        SvgComplexSelector selector = result.Selector.Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(selector.Compounds.Select(compound => compound.SimpleSelectors[0].Name), Is.EqualTo(new[] { "svg", "g", "rect", "circle", "path" }));
            Assert.That(selector.Combinators, Is.EqualTo(new[]
            {
                SvgSelectorCombinator.Descendant,
                SvgSelectorCombinator.Child,
                SvgSelectorCombinator.AdjacentSibling,
                SvgSelectorCombinator.GeneralSibling
            }));
            Assert.That(selector.RawText, Is.EqualTo("svg g > rect + circle ~ path"));
        });
    }

    [Test]
    public void CommentsAreRemovedWithoutCreatingDescendantCombinators()
    {
        SvgSelectorParseResult result = Parse("rect/**/.hot, g/*x*/>/*y*/path");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Selector.Items[0].Compounds, Has.Count.EqualTo(1));
            Assert.That(result.Selector.Items[0].Compounds[0].SimpleSelectors.Select(selector => selector.Name), Is.EqualTo(new[] { "rect", "hot" }));
            Assert.That(result.Selector.Items[0].Compounds[0].RawText, Is.EqualTo("rect/**/.hot"));
            Assert.That(result.Selector.Items[1].Combinators, Is.EqualTo(new[] { SvgSelectorCombinator.Child }));
            Assert.That(result.Selector.Items[1].RawText, Is.EqualTo("g/*x*/>/*y*/path"));
        });
    }

    [Test]
    public void SelectorListsRetainCallerSourceOffsetsLinesAndIndependentItems()
    {
        string input = " \r\nsvg,\n.x ";
        SvgSourceUri source = new SvgSourceUri("memory:selectors.css");

        SvgSelectorParseResult result = new SvgCssParser().ParseSelectorWithValueAndLimitsAndSource(input, Limits(), source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Selector.RawText, Is.EqualTo("svg,\n.x"));
            Assert.That(result.Selector.Source.Source, Is.SameAs(source));
            Assert.That(result.Selector.Source.StartOffset, Is.EqualTo(3));
            Assert.That(result.Selector.Source.StartLine, Is.EqualTo(2));
            Assert.That(result.Selector.Source.StartColumn, Is.EqualTo(1));
            Assert.That(result.Selector.Items[0].Source.StartLine, Is.EqualTo(2));
            Assert.That(result.Selector.Items[1].Source.StartLine, Is.EqualTo(3));
            Assert.That(result.Selector.Items[1].Compounds[0].SimpleSelectors[0].RawText, Is.EqualTo(".x"));
            Assert.That(result.Selector.IdSpecificity, Is.Zero);
            Assert.That(result.Selector.ClassSpecificity, Is.EqualTo(1));
            Assert.That(result.Selector.TypeSpecificity, Is.Zero);
        });
    }

    [TestCase("", SvgSelectorParseError.ExpectedSelector)]
    [TestCase("a,", SvgSelectorParseError.ExpectedSelector)]
    [TestCase(",a", SvgSelectorParseError.ExpectedSelector)]
    [TestCase("a >", SvgSelectorParseError.TrailingCombinator)]
    [TestCase(".", SvgSelectorParseError.ExpectedName)]
    [TestCase("#123", SvgSelectorParseError.ExpectedName)]
    [TestCase("[a?x]", SvgSelectorParseError.ExpectedAttributeOperator)]
    [TestCase("[a=x z]", SvgSelectorParseError.ExpectedCloseBracket)]
    [TestCase("[a=\"x\"i]", SvgSelectorParseError.ExpectedCloseBracket)]
    [TestCase("ns|", SvgSelectorParseError.UnexpectedToken)]
    public void InvalidSelectorsReturnLocatedStableFailures(string input, SvgSelectorParseError expected)
    {
        SvgSelectorParseResult result = Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(expected));
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Selector.IsValid, Is.False);
            Assert.That(result.Selector.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Selector.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
        });
    }

    [Test]
    public void SelectorCharacterAndTokenLimitsFailBeforeUnboundedParsing()
    {
        SvgCssLimits characters = Limits();
        characters.MaxCharacters = 3;
        SvgSelectorParseResult characterResult = new SvgSelectorParser().ParseWithValueAndLimits("abcd", characters);

        SvgCssLimits tokens = Limits();
        tokens.MaxSelectorTokens = 2;
        SvgSelectorParseResult tokenResult = new SvgSelectorParser().ParseWithValueAndLimits("a .b", tokens);

        SvgSelectorParseResult nullText = new SvgSelectorParser().ParseWithValueAndLimits(null, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(characterResult.Error, Is.EqualTo(SvgSelectorParseError.CharacterLimitExceeded));
            Assert.That(characterResult.ErrorOffset, Is.EqualTo(3));
            Assert.That(tokenResult.Error, Is.EqualTo(SvgSelectorParseError.TokenLimitExceeded));
            Assert.That(tokenResult.ErrorOffset, Is.EqualTo(2));
            Assert.That(characterResult.Selector.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(tokenResult.Selector.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(nullText.Error, Is.EqualTo(SvgSelectorParseError.InvalidArgument));
        });
    }

    [Test]
    public void QualifiedRulesReceiveTypedSelectorsAndInvalidSelectorsRemainDiagnosed()
    {
        SvgCssParseResult valid = new SvgCssParser().ParseStyleSheetWithValueAndLimits("svg|rect.foo, *|circle[data-x] { fill:red; }", Limits());
        SvgCssParseResult invalid = new SvgCssParser().ParseStyleSheetWithValueAndLimits("a:unknown { fill:red; }", Limits());

        SvgCssRule validRule = valid.StyleSheet.Rules[0];
        SvgCssRule invalidRule = invalid.StyleSheet.Rules[0];

        Assert.Multiple(() =>
        {
            Assert.That(validRule.Selectors, Is.Not.Null);
            Assert.That(validRule.Selectors.IsValid, Is.True);
            Assert.That(validRule.Selectors.Items, Has.Count.EqualTo(2));
            Assert.That(validRule.IsValid, Is.True);
            Assert.That(invalid.Success, Is.True);
            Assert.That(invalidRule.Selectors.IsValid, Is.False);
            Assert.That(invalidRule.IsValid, Is.False);
            Assert.That(invalidRule.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(invalid.StyleSheet.IsValid, Is.False);
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
