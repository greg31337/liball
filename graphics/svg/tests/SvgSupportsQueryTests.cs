namespace svg.Tests;

public class SvgSupportsQueryTests
{
    [Test]
    public void ParserBuildsNestedNotAndOrConditionsWithSourceProvenance()
    {
        string queryText = "((display: block) and selector(g > rect)) or (not (display: grid))";
        SvgSourceUri source = new SvgSourceUri("supports.css");

        SvgSupportsParseResult result = new SvgSupportsQueryParser().ParseWithValueAndLimitsAndSource(queryText, new SvgCssLimits(), source);
        SvgSupportsCondition root = result.Query.Condition;
        SvgSupportsCondition conjunction = root.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Query.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(root.Kind, Is.EqualTo(SvgSupportsConditionKind.Or));
            Assert.That(root.Children, Has.Count.EqualTo(2));
            Assert.That(conjunction.Kind, Is.EqualTo(SvgSupportsConditionKind.And));
            Assert.That(conjunction.Children.Select(child => child.Kind), Is.EqualTo(new[] { SvgSupportsConditionKind.Declaration, SvgSupportsConditionKind.Selector }));
            Assert.That(conjunction.Children[0].PropertyName, Is.EqualTo("display"));
            Assert.That(conjunction.Children[0].ValueText, Is.EqualTo("block"));
            Assert.That(conjunction.Children[1].SelectorText, Is.EqualTo("g > rect"));
            Assert.That(conjunction.Children[1].Selector, Is.Not.Null);
            Assert.That(root.Children[1].Kind, Is.EqualTo(SvgSupportsConditionKind.Not));
            Assert.That(root.Source.Source, Is.SameAs(source));
            Assert.That(root.Source.StartOffset, Is.Zero);
        });
    }

    [Test]
    public void DefaultPropertyCatalogValidatesRecognizedDeclarationsAndValues()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        SvgCssLimits limits = new SvgCssLimits();

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(display: block)", limits), Is.True);
            Assert.That(environment.Matches("(display: grid)", limits), Is.False);
            Assert.That(environment.Matches("(color: rgb(10 20 30 / .5))", limits), Is.True);
            Assert.That(environment.Matches("(color: definitely-not-a-color)", limits), Is.False);
            Assert.That(environment.Matches("(width: 25%)", limits), Is.True);
            Assert.That(environment.Matches("(transform: translate(10 20))", limits), Is.True);
            Assert.That(environment.Matches("(opacity: .5)", limits), Is.True);
            Assert.That(environment.Matches("(opacity: red)", limits), Is.False);
            Assert.That(environment.Matches("(unknown-property: value)", limits), Is.False);
            Assert.That(environment.Matches("(--Theme: any tokens here)", limits), Is.True);
            Assert.That(environment.Matches("(display: var(--layout))", limits), Is.True);
            Assert.That(environment.Matches("(display: inherit)", limits), Is.True);
        });
    }

    [Test]
    public void PropertyCatalogReplacementAndRequiredCapabilitiesControlEvaluation()
    {
        SvgPropertyCatalog catalog = new SvgPropertyCatalog();
        catalog.Add(new SvgPropertySupportEntry("host-mode", SvgPropertyValueSupportKind.Keyword, new[] { "safe" }, "host.css.mode"));
        catalog.Add(new SvgPropertySupportEntry("HOST-MODE", SvgPropertyValueSupportKind.Keyword, new[] { "fast" }, "host.css.mode"));
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        environment.Properties = catalog;
        SvgCssLimits limits = new SvgCssLimits();

        bool beforeCapability = environment.Matches("(host-mode: fast)", limits);
        environment.Capabilities.Add("host.css.mode");
        bool afterCapability = environment.Matches("(host-mode: fast)", limits);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Find("host-mode").Keywords, Is.EqualTo(new[] { "fast" }));
            Assert.That(beforeCapability, Is.False);
            Assert.That(afterCapability, Is.True);
            Assert.That(environment.Matches("(host-mode: safe)", limits), Is.False);
        });
    }

    [Test]
    public void CssParsingCapabilityGatesDeclarationsIncludingCustomProperties()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        environment.Capabilities.Remove(SvgFeatureIdentifiers.CssParsing);

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(display: block)", new SvgCssLimits()), Is.False);
            Assert.That(environment.Matches("(--x: value)", new SvgCssLimits()), Is.False);
        });
    }

    [Test]
    public void SelectorConditionsUseParserSupportAndSelectorCapabilities()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        SvgCssLimits limits = new SvgCssLimits();

        bool basic = environment.Matches("selector(svg > g.item[data-x])", limits);
        bool relational = environment.Matches("selector(g:has(> circle))", limits);
        bool malformed = environment.Matches("selector(g >)", limits);
        bool unsupportedDynamic = environment.Matches("selector(input:enabled)", limits);
        environment.Capabilities.Remove(SvgFeatureIdentifiers.CssSelectors);
        bool disabled = environment.Matches("selector(rect)", limits);

        Assert.Multiple(() =>
        {
            Assert.That(basic, Is.True);
            Assert.That(relational, Is.True);
            Assert.That(malformed, Is.False);
            Assert.That(unsupportedDynamic, Is.False);
            Assert.That(disabled, Is.False);
        });
    }

    [Test]
    public void BooleanOperatorsEvaluateWithCssSupportsSemantics()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        SvgCssLimits limits = new SvgCssLimits();

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(display: block) and (color: red)", limits), Is.True);
            Assert.That(environment.Matches("(display: grid) or (color: red)", limits), Is.True);
            Assert.That(environment.Matches("not (display: grid)", limits), Is.True);
            Assert.That(environment.Matches("not (display: block)", limits), Is.False);
            Assert.That(environment.Matches("((display: grid) or (color: red)) and (width: 10px)", limits), Is.True);
        });
    }

    [Test]
    public void GeneralEnclosedConditionsAreValidAndSafelyEvaluateFalse()
    {
        SvgSupportsParseResult function = new SvgSupportsQueryParser().ParseWithValueAndLimits("future-condition(example)", new SvgCssLimits());
        SvgSupportsParseResult parenthesized = new SvgSupportsQueryParser().ParseWithValueAndLimits("(future syntax)", new SvgCssLimits());
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();

        Assert.Multiple(() =>
        {
            Assert.That(function.Query.IsValid, Is.True);
            Assert.That(function.Query.Condition.Kind, Is.EqualTo(SvgSupportsConditionKind.Unknown));
            Assert.That(parenthesized.Query.IsValid, Is.True);
            Assert.That(new SvgSupportsQueryEvaluator().Matches(function.Query, environment, new SvgCssLimits()), Is.False);
            Assert.That(new SvgSupportsQueryEvaluator().Matches(parenthesized.Query, environment, new SvgCssLimits()), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("display: block")]
    [TestCase("()")]
    [TestCase("(display:)")]
    [TestCase("(display: block) and")]
    [TestCase("(display: block) and (color: red) or (width: 1px)")]
    [TestCase("not (display: block) and (color: red)")]
    public void MalformedConditionsProduceLocatedCssDiagnostics(string text)
    {
        SvgSourceUri source = new SvgSourceUri("bad-supports.css");
        SvgSupportsParseResult result = new SvgSupportsQueryParser().ParseWithValueAndLimitsAndSource(text, new SvgCssLimits(), source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Query.IsValid, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Source != null).All(diagnostic => diagnostic.Source.Source == source), Is.True);
            Assert.That(new SvgSupportsQueryEvaluator().Matches(result.Query, new SvgSupportsEnvironment(), new SvgCssLimits()), Is.False);
        });
    }

    [Test]
    public void TokenDepthAndConditionLimitsFailDeterministically()
    {
        SvgCssLimits tokenLimits = new SvgCssLimits();
        tokenLimits.MaxTokens = 2;
        SvgCssLimits depthLimits = new SvgCssLimits();
        depthLimits.MaxBlockDepth = 1;
        SvgCssLimits conditionLimits = new SvgCssLimits();
        conditionLimits.MaxDeclarations = 2;

        SvgSupportsParseResult tokens = new SvgSupportsQueryParser().ParseWithValueAndLimits("(display: block)", tokenLimits);
        SvgSupportsParseResult depth = new SvgSupportsQueryParser().ParseWithValueAndLimits("(((display: block)))", depthLimits);
        SvgSupportsParseResult conditions = new SvgSupportsQueryParser().ParseWithValueAndLimits("(display: block) and (color: red)", conditionLimits);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Success, Is.False);
            Assert.That(tokens.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(depth.Success, Is.False);
            Assert.That(depth.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(conditions.Success, Is.False);
            Assert.That(conditions.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void CssSupportsRulesAttachLocatedConditionsAndRequireBlocks()
    {
        string css = "@supports (display: block) and selector(g > rect) { g {} } @supports (color: red);";
        SvgSourceUri source = new SvgSourceUri("sheet.css");

        SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), SvgStyleOrigin.Author, source);
        SvgCssAtRule valid = result.StyleSheet.Rules[0].AtRule;
        SvgCssRule invalid = result.StyleSheet.Rules[1];

        Assert.Multiple(() =>
        {
            Assert.That(valid.SupportsQuery, Is.Not.Null);
            Assert.That(valid.SupportsQuery.IsValid, Is.True);
            Assert.That(valid.SupportsQuery.Condition.Kind, Is.EqualTo(SvgSupportsConditionKind.And));
            Assert.That(valid.SupportsQuery.Condition.Source.Source, Is.SameAs(source));
            Assert.That(valid.SupportsQuery.Condition.Source.StartOffset, Is.EqualTo(css.IndexOf("(display")));
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Diagnostics.Single().Message, Does.Contain("requires a block"));
            Assert.That(result.StyleSheet.IsValid, Is.False);
        });
    }

    [Test]
    public void DeclarationInjectionDoesNotPassPropertyCatalogValidation()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgCapabilitySet capabilities = SvgCapabilitySet.Create(new[] { SvgFeatureIdentifiers.CssParsing });

        Assert.That(catalog.Supports("display", "block; color:red", new SvgCssLimits(), capabilities), Is.False);
    }
}
