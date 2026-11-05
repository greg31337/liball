namespace uri.Tests;

public class UriTemplatePublicApiTests
{
    [Test]
    public void ParserContractSupportsThrowingResultAndSpanPaths()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        UriTemplate parsed = parser.ParseTemplate(
            "pre{?x,y}post",
            new UriTemplateParseOptions());
        UriTemplateParseResult result = parser.TryParseTemplate(
            "{+value}",
            new UriTemplateParseOptions());
        UriTemplateParseResult span = parser.TryParseTemplateSpan(
            "{/path*}".AsSpan(),
            new UriTemplateParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.OriginalText, Is.EqualTo("pre{?x,y}post"));
            Assert.That(parsed.Level, Is.EqualTo(UriTemplateLevel.Level3));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level2));
            Assert.That(span.Success, Is.True);
            Assert.That(span.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level4));
        });
    }

    [Test]
    public void ParserContractReportsTheSameSyntaxFailureOnBothPaths()
    {
        IUriTemplateParser parser = new UriTemplateParser();

        UriTemplateParseResult result = parser.TryParseTemplate(
            "{a,}",
            new UriTemplateParseOptions());
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(() => parser.ParseTemplate(
                "{a,}",
                new UriTemplateParseOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                result.Diagnostic!.Code));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.TemplateVariable));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(2));
        });
    }

    [Test]
    public void ParserContractEnforcesInputExpressionAndVariableLimits()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        UriTemplateParseOptions inputOptions = new UriTemplateParseOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        UriTemplateParseOptions expressionOptions =
            new UriTemplateParseOptions();
        expressionOptions.Limits.MaxTemplateExpressions = 1;
        UriTemplateParseOptions variableOptions =
            new UriTemplateParseOptions();
        variableOptions.Limits.MaxTemplateVariables = 1;

        UriTemplateParseResult input = parser.TryParseTemplate(
            "abc",
            inputOptions);
        UriTemplateParseResult expressions = parser.TryParseTemplate(
            "{a}{b}",
            expressionOptions);
        UriTemplateParseResult variables = parser.TryParseTemplate(
            "{a,b}",
            variableOptions);

        Assert.Multiple(() =>
        {
            AssertParseLimit(input, UriComponent.Input);
            AssertParseLimit(expressions, UriComponent.TemplateExpression);
            AssertParseLimit(variables, UriComponent.TemplateVariable);
        });
    }

    [Test]
    public void ExpanderContractSupportsStrictResultAndPartialExpansion()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = Parse("{?x}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("x", "a b")
        };

        string strict = expander.ExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        string partial = expander.ExpandTemplatePartially(
            Parse("{known,missing}/{done}"),
            new UriTemplateBinding[]
            {
                ScalarBinding("known", "value"),
                ScalarBinding("done", "yes")
            },
            new UriTemplateExpansionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.EqualTo("?x=a%20b"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(strict));
            Assert.That(partial, Is.EqualTo("{known,missing}/yes"));
        });
    }

    [Test]
    public void ExpanderContractPreservesFailureDiagnostics()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        string malformed = new string('\ud800', 1);
        UriTemplate template = Parse("{value}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("value", malformed)
        };

        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() => expander.ExpandTemplate(
                template,
                bindings,
                new UriTemplateExpansionOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                result.Diagnostic!.Code));
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
        });
    }

    [Test]
    public void ComposerContractPreservesOrCanonicalizesPercentCase()
    {
        IUriTemplateComposer composer = new UriTemplateComposer();
        UriTemplate template = Parse("%2f{?Some%2athing,list*}");

        string preserved = composer.ComposeTemplate(
            template,
            new OperationLimits());
        string canonical = composer.CanonicalizeTemplatePercentCase(
            template,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo(
                "%2f{?Some%2athing,list*}"));
            Assert.That(canonical, Is.EqualTo(
                "%2F{?Some%2Athing,list*}"));
            Assert.That(template.OriginalText, Is.EqualTo(preserved));
        });
    }

    [Test]
    public void ComposerContractThrowsForInvalidModelsAndLimits()
    {
        IUriTemplateComposer composer = new UriTemplateComposer();
        UriTemplate invalid = new UriTemplate();
        UriTemplatePart literal = new UriTemplatePart();
        literal.Literal = "bad{x}";
        invalid.Parts = new UriTemplatePart[] { literal };
        OperationLimits shortOutput = new OperationLimits();
        shortOutput.MaxOutputCharacters = 2;

        UriTemplateSyntaxException model = Assert.Throws<
            UriTemplateSyntaxException>(() => composer.ComposeTemplate(
                invalid,
                new OperationLimits()));
        UriTemplateSyntaxException limit = Assert.Throws<
            UriTemplateSyntaxException>(() =>
                composer.CanonicalizeTemplatePercentCase(
                    Parse("abcd"),
                    shortOutput));

        Assert.Multiple(() =>
        {
            Assert.That(model.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidTemplate));
            Assert.That(limit.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(limit.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
        });
    }

    [Test]
    public void InspectorContractReturnsUniqueNamesOccurrencesAndLevel()
    {
        IUriTemplateInspector inspector = new UriTemplateInspector();
        UriTemplate template = Parse("{?b,a,b}{x*,A}");

        string[] names = inspector.ListTemplateVariables(template);
        UriTemplateVariable[] occurrences =
            inspector.ListTemplateOccurrences(template);
        UriTemplateLevel level = inspector.DetermineTemplateLevel(template);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new string[]
            {
                "b",
                "a",
                "x",
                "A"
            }));
            Assert.That(occurrences, Has.Length.EqualTo(5));
            Assert.That(occurrences[0], Is.SameAs(
                template.Parts[0].Variables[0]));
            Assert.That(occurrences[3].Explode, Is.True);
            Assert.That(level, Is.EqualTo(UriTemplateLevel.Level4));
        });
    }

    [Test]
    public void InspectorContractRejectsMissingModels()
    {
        IUriTemplateInspector inspector = new UriTemplateInspector();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateVariables(null!));
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateOccurrences(null!));
            Assert.Throws<ArgumentException>(() =>
                inspector.DetermineTemplateLevel(null!));
        });
    }

    [Test]
    public void ProcessorContractComposesParseResolveAndNormalizeWorkflows()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplate template = Parse("https://{host}{/path}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("host", "example.com"),
            ScalarBinding("path", "a b")
        };

        UriReference parsed = processor.ExpandToUri(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            new UriParseOptions());
        string resolved = processor.ExpandAndResolve(
            Parse("../{path}"),
            new UriTemplateBinding[] { ScalarBinding("path", "next") },
            "http://example.com/a/b/",
            new UriTemplateExpansionOptions(),
            new UriResolutionOptions());
        string normalized = processor.ExpandAndNormalize(
            Parse("HTTP://EXAMPLE.COM/a/./{path}%7e"),
            new UriTemplateBinding[] { ScalarBinding("path", "b") },
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.OriginalText, Is.EqualTo(
                "https://example.com/a%20b"));
            Assert.That(resolved, Is.EqualTo(
                "http://example.com/a/next"));
            Assert.That(normalized, Is.EqualTo(
                "http://example.com/a/b~"));
        });
    }

    [Test]
    public void ProcessorContractUsesStageSpecificExceptions()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        string malformed = new string('\udc00', 1);

        UriTemplateExpansionException expansion = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandToUri(
                Parse("{value}"),
                new UriTemplateBinding[]
                {
                    ScalarBinding("value", malformed)
                },
                new UriTemplateExpansionOptions(),
                new UriParseOptions()));
        UriSyntaxException syntax = Assert.Throws<UriSyntaxException>(() =>
            processor.ExpandToUri(
                Parse("{+value}"),
                new UriTemplateBinding[]
                {
                    ScalarBinding("value", "http://[bad]")
                },
                new UriTemplateExpansionOptions(),
                new UriParseOptions()));
        UriResolutionException resolution = Assert.Throws<
            UriResolutionException>(() => processor.ExpandAndResolve(
                Parse("{value}"),
                new UriTemplateBinding[]
                {
                    ScalarBinding("value", "path")
                },
                "relative/base",
                new UriTemplateExpansionOptions(),
                new UriResolutionOptions()));
        UriNormalizationOptions invalidOptions =
            new UriNormalizationOptions();
        invalidOptions.Profile = new UriSchemeProfile();
        UriNormalizationException normalization = Assert.Throws<
            UriNormalizationException>(() => processor.ExpandAndNormalize(
                Parse("http://example.com"),
                new UriTemplateBinding[0],
                new UriTemplateExpansionOptions(),
                invalidOptions));

        Assert.Multiple(() =>
        {
            Assert.That(expansion.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(syntax.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidIpv6Address));
            Assert.That(resolution.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.UnresolvableReference));
            Assert.That(normalization.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
        });
    }

    private static UriTemplate Parse(string text)
    {
        IUriTemplateParser parser = new UriTemplateParser();
        return parser.ParseTemplate(text, new UriTemplateParseOptions());
    }

    private static UriTemplateBinding ScalarBinding(string name, string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
    }

    private static void AssertParseLimit(
        UriTemplateParseResult result,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
            DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.LimitExceeded));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
