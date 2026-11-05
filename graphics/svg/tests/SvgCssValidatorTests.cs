namespace svg.Tests;

public class SvgCssValidatorTests
{
    [Test]
    public void ValidEmbeddedInlineAndPresentationCssPassTogether()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>@media screen { rect.primary:hover { fill:#123; stroke-width:2px } } @supports (display:block) { .primary { display:block } }</style><rect class='primary' width='10' height='10' opacity='.5' style='color:rgb(1 2 3); transform:translate(1px, 2px)'/></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(CssIssues(result), Is.Empty);
        });
    }

    [Test]
    public void SelectorFailuresAndUnsupportedDynamicSelectorsAreClassified()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>rect &gt; { fill:red } a:enabled { fill:blue }</style><rect/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.CssSelector);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues.Any(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Error), Is.True);
            Assert.That(issues.Any(item => item.Diagnostic.Code == SvgDiagnosticCode.UnsupportedFeature), Is.True);
            Assert.That(issues.All(item => item.Diagnostic.Source != null), Is.True);
            Assert.That(issues.All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void AtRuleGrammarAndUnknownAtRulesAreReportedSeparately()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>@supports (display:) { rect { fill:red } } @vendor demo;</style><rect/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.CssAtRule);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues.Any(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Error), Is.True);
            Assert.That(issues.Any(item => item.Diagnostic.Code == SvgDiagnosticCode.UnsupportedFeature && item.Diagnostic.Message.Contains("vendor")), Is.True);
            Assert.That(issues.All(item => item.Node.ElementKind == SvgElementKind.Style), Is.True);
        });
    }

    [Test]
    public void DeclarationsUnknownPropertiesGrammarAndUnsupportedValuesRemainDistinct()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>rect { broken; made-up:1; fill:not-a-paint; display:grid }</style><rect/></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.FindIssues(SvgValidationRule.CssDeclaration), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssProperty).Any(item => item.Diagnostic.Message.Contains("made-up")), Is.True);
            Assert.That(result.FindIssues(SvgValidationRule.CssProperty).Any(item => item.Diagnostic.Message.Contains("fill")), Is.True);
            Assert.That(result.FindIssues(SvgValidationRule.CssUnsupportedValue).Single().Diagnostic.Message, Does.Contain("display"));
            Assert.That(result.FindIssues(SvgValidationRule.CssUnsupportedValue).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Warning));
        });
    }

    [Test]
    public void InlineStyleFailuresMapBackToTheAttributeValue()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect style='fill:not-a-paint; broken; color:red'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        SvgValidationIssue grammar = result.FindIssues(SvgValidationRule.CssProperty).Single(item => item.Diagnostic.Message.Contains("fill"));
        SvgValidationIssue declaration = result.FindIssues(SvgValidationRule.CssDeclaration).Single();

        Assert.Multiple(() =>
        {
            Assert.That(grammar.Attribute.LocalName, Is.EqualTo("style"));
            Assert.That(grammar.Diagnostic.Source.StartOffset, Is.GreaterThanOrEqualTo(source.IndexOf("fill", StringComparison.Ordinal)));
            Assert.That(declaration.Attribute, Is.SameAs(grammar.Attribute));
            Assert.That(declaration.Diagnostic.Source.StartOffset, Is.EqualTo(source.IndexOf("broken", StringComparison.Ordinal)));
            Assert.That(grammar.RecoveryDescription, Is.Not.Empty);
        });
    }

    [Test]
    public void CustomPropertyCyclesAndComputedValueFailuresUseCssRecovery()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect style='--a:var(--b); --b:var(--a); --paint:not-a-paint; fill:var(--paint); color:var(--a); paint-order:banana'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> cycles = result.FindIssues(SvgValidationRule.CssVariable);
        List<SvgValidationIssue> computed = result.FindIssues(SvgValidationRule.CssComputedValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(cycles, Is.Not.Empty);
            Assert.That(cycles.All(item => item.Diagnostic.Message.Contains("cycle")), Is.True);
            Assert.That(computed.Any(item => item.Diagnostic.Message.Contains("fill")), Is.True);
            Assert.That(computed.Any(item => item.Diagnostic.Message.Contains("paint-order")), Is.True);
            Assert.That(cycles.Concat(computed).All(item => item.RecoveryDescription.Contains("initial value")), Is.True);
        });
    }

    [Test]
    public void InvalidCssValidationOptionsFailBeforeTraversal()
    {
        SvgValidationOptions options = new SvgValidationOptions();
        options.Css.Limits.MaxRules = 0;

        SvgValidationResult result = Load("<svg xmlns='http://www.w3.org/2000/svg'/>").ValidateWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    static List<SvgValidationIssue> CssIssues(SvgValidationResult result)
    {
        return result.Issues.Where(item => item.Rule >= SvgValidationRule.CssSelector && item.Rule <= SvgValidationRule.CssComputedValue).ToList();
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
