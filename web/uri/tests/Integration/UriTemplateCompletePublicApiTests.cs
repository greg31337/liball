namespace uri.Tests;

public class UriTemplateCompletePublicApiTests
{
    [Test]
    public void TemplateOptionsOwnIndependentMutableLimitSets()
    {
        UriTemplateParseOptions firstParse = new UriTemplateParseOptions();
        UriTemplateParseOptions secondParse = new UriTemplateParseOptions();
        UriTemplateExpansionOptions firstExpansion =
            new UriTemplateExpansionOptions();
        UriTemplateExpansionOptions secondExpansion =
            new UriTemplateExpansionOptions();

        firstParse.Limits.MaxTemplateExpressions = 7;
        firstExpansion.Limits.MaxTemplateCompositeMembers = 9;

        Assert.Multiple(() =>
        {
            Assert.That(firstParse.Limits, Is.Not.Null);
            Assert.That(firstParse.Limits, Is.Not.SameAs(secondParse.Limits));
            Assert.That(secondParse.Limits.MaxTemplateExpressions,
                Is.EqualTo(4096));
            Assert.That(firstExpansion.Limits, Is.Not.Null);
            Assert.That(firstExpansion.Limits,
                Is.Not.SameAs(secondExpansion.Limits));
            Assert.That(secondExpansion.Limits.MaxTemplateCompositeMembers,
                Is.EqualTo(65536));
        });
    }

    [TestCase(
        " ",
        UriDiagnosticCode.InvalidTemplate,
        UriComponent.TemplateLiteral)]
    [TestCase(
        "}",
        UriDiagnosticCode.InvalidTemplateExpression,
        UriComponent.TemplateExpression)]
    [TestCase(
        "{=x}",
        UriDiagnosticCode.UnsupportedTemplateOperator,
        UriComponent.TemplateExpression)]
    [TestCase(
        "{$x}",
        UriDiagnosticCode.InvalidTemplateVariable,
        UriComponent.TemplateVariable)]
    [TestCase(
        "{x:0}",
        UriDiagnosticCode.InvalidTemplateModifier,
        UriComponent.TemplateVariable)]
    [TestCase(
        "%",
        UriDiagnosticCode.InvalidPercentEncoding,
        UriComponent.TemplateLiteral)]
    public void EveryParserSyntaxDiagnosticIsExposedByAllPublicPaths(
        string text,
        UriDiagnosticCode code,
        UriComponent component)
    {
        IUriTemplateParser parser = new UriTemplateParser();
        UriTemplateParseResult stringResult = parser.TryParseTemplate(
            text,
            new UriTemplateParseOptions());
        UriTemplateParseResult spanResult = parser.TryParseTemplateSpan(
            text.AsSpan(),
            new UriTemplateParseOptions());
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(() => parser.ParseTemplate(
                text,
                new UriTemplateParseOptions()))!;

        AssertParseFailure(stringResult, code, component);
        AssertParseFailure(spanResult, code, component);
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(
                stringResult.Diagnostic!.Range.Offset));
            Assert.That(exception.Diagnostic.Range.Length, Is.EqualTo(
                stringResult.Diagnostic.Range.Length));
            Assert.That(exception.Message, Is.EqualTo(
                stringResult.Diagnostic.Message));
        });
    }

    [Test]
    public void ParserPublicPathsExposeInvalidUnicodeDiagnostics()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        string malformed = "a" + new string('\ud800', 1);

        UriTemplateParseResult stringResult = parser.TryParseTemplate(
            malformed,
            new UriTemplateParseOptions());
        UriTemplateParseResult spanResult = parser.TryParseTemplateSpan(
            malformed.AsSpan(),
            new UriTemplateParseOptions());
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(() => parser.ParseTemplate(
                malformed,
                new UriTemplateParseOptions()))!;

        AssertParseFailure(
            stringResult,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral);
        AssertParseFailure(
            spanResult,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral);
        Assert.Multiple(() =>
        {
            Assert.That(stringResult.Diagnostic!.Range.Offset, Is.EqualTo(1));
            Assert.That(stringResult.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
        });
    }

    [Test]
    public void ParserReportsEveryArgumentAndLimitFailureAsAnEmptyResult()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        UriTemplateParseOptions missingLimits =
            new UriTemplateParseOptions();
        missingLimits.Limits = null!;
        UriTemplateParseOptions inputLimit = new UriTemplateParseOptions();
        inputLimit.Limits.MaxInputCharacters = 2;
        UriTemplateParseOptions expressionLimit =
            new UriTemplateParseOptions();
        expressionLimit.Limits.MaxTemplateExpressions = 0;
        UriTemplateParseOptions variableLimit =
            new UriTemplateParseOptions();
        variableLimit.Limits.MaxTemplateVariables = 0;

        UriTemplateParseResult missingText = parser.TryParseTemplate(
            null!,
            new UriTemplateParseOptions());
        UriTemplateParseResult missingOptions = parser.TryParseTemplate(
            "x",
            null!);
        UriTemplateParseResult missingSpanOptions =
            parser.TryParseTemplateSpan("x".AsSpan(), null!);
        UriTemplateParseResult missingLimitSet = parser.TryParseTemplate(
            "x",
            missingLimits);
        UriTemplateParseResult input = parser.TryParseTemplate(
            "abc",
            inputLimit);
        UriTemplateParseResult expression = parser.TryParseTemplate(
            "{x}",
            expressionLimit);
        UriTemplateParseResult variable = parser.TryParseTemplate(
            "{x}",
            variableLimit);
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(() => parser.ParseTemplate(
                null!,
                new UriTemplateParseOptions()))!;

        AssertParseFailure(
            missingText,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertParseFailure(
            missingOptions,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertParseFailure(
            missingSpanOptions,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertParseFailure(
            missingLimitSet,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertParseFailure(
            input,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input);
        AssertParseFailure(
            expression,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.TemplateExpression);
        AssertParseFailure(
            variable,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.TemplateVariable);
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidInput));
    }

    [Test]
    public void ExpanderResultRepresentsEveryPublicValueShape()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplateValue undefined = new UriTemplateValue();
        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            Parse("{/undefined,scalar,list*,map*}"),
            new UriTemplateBinding[]
            {
                new UriTemplateBinding("undefined", undefined),
                ScalarBinding("scalar", "s"),
                ListBinding("list", new string[] { "a", "b" }),
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("x", true, "1"),
                    new UriTemplateMapEntry("ignored", false, "value"),
                    new UriTemplateMapEntry("y", true, "")
                })
            },
            new UriTemplateExpansionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("/s/a/b/x=1/y"));
            Assert.That(result.Length, Is.EqualTo(result.Text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ExpanderPublicMethodsHandleInvalidArgumentsConsistently()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = Parse("x");
        UriTemplateBinding[] bindings = new UriTemplateBinding[0];
        UriTemplateExpansionOptions missingLimits =
            new UriTemplateExpansionOptions();
        missingLimits.Limits = null!;

        UriTemplateExpansionResult missingTemplate =
            expander.TryExpandTemplate(
                null!,
                bindings,
                new UriTemplateExpansionOptions());
        UriTemplateExpansionResult missingBindings =
            expander.TryExpandTemplate(
                template,
                null!,
                new UriTemplateExpansionOptions());
        UriTemplateExpansionResult missingOptions =
            expander.TryExpandTemplate(template, bindings, null!);
        UriTemplateExpansionResult missingLimitSet =
            expander.TryExpandTemplate(template, bindings, missingLimits);
        UriTemplateExpansionException strict = Assert.Throws<
            UriTemplateExpansionException>(() => expander.ExpandTemplate(
                template,
                bindings,
                null!))!;
        UriTemplateExpansionException partial = Assert.Throws<
            UriTemplateExpansionException>(() =>
                expander.ExpandTemplatePartially(
                    template,
                    null!,
                    new UriTemplateExpansionOptions()))!;

        AssertExpansionFailure(
            missingTemplate,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertExpansionFailure(
            missingBindings,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertExpansionFailure(
            missingOptions,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertExpansionFailure(
            missingLimitSet,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        Assert.Multiple(() =>
        {
            Assert.That(strict.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(strict.Message, Is.EqualTo(strict.Diagnostic.Message));
            Assert.That(partial.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(partial.Message, Is.EqualTo(
                partial.Diagnostic.Message));
        });
    }

    [Test]
    public void ExpanderExposesEveryExpansionDiagnosticFamily()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        string malformed = new string('\udc00', 1);
        UriTemplate invalidLiteral = Parse("ok");
        invalidLiteral.Parts[0].Literal = "%";
        UriTemplateExpansionOptions outputLimit =
            new UriTemplateExpansionOptions();
        outputLimit.Limits.MaxOutputCharacters = 0;
        UriTemplateExpansionOptions memberLimit =
            new UriTemplateExpansionOptions();
        memberLimit.Limits.MaxTemplateCompositeMembers = 0;

        UriTemplateExpansionResult unicode = expander.TryExpandTemplate(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", malformed) },
            new UriTemplateExpansionOptions());
        UriTemplateExpansionResult percent = expander.TryExpandTemplate(
            invalidLiteral,
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions());
        UriTemplateExpansionResult modifier = expander.TryExpandTemplate(
            Parse("{list:1}"),
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "a" })
            },
            new UriTemplateExpansionOptions());
        UriTemplateExpansionResult output = expander.TryExpandTemplate(
            Parse("x"),
            new UriTemplateBinding[0],
            outputLimit);
        UriTemplateExpansionResult members = expander.TryExpandTemplate(
            Parse("{list}"),
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "a" })
            },
            memberLimit);
        UriTemplateExpansionException partial = Assert.Throws<
            UriTemplateExpansionException>(() =>
                expander.ExpandTemplatePartially(
                    Parse("{list:1}"),
                    new UriTemplateBinding[]
                    {
                        ListBinding("list", new string[] { "a" })
                    },
                    new UriTemplateExpansionOptions()))!;

        AssertExpansionFailure(
            unicode,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateVariable);
        AssertExpansionFailure(
            percent,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.TemplateLiteral);
        AssertExpansionFailure(
            modifier,
            UriDiagnosticCode.InvalidTemplateModifier,
            UriComponent.TemplateVariable);
        AssertExpansionFailure(
            output,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output);
        AssertExpansionFailure(
            members,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.TemplateVariable);
        Assert.That(partial.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidTemplateModifier));
    }

    [Test]
    public void ComposerPublicMethodsExposeEveryModelDiagnosticFamily()
    {
        IUriTemplateComposer composer = new UriTemplateComposer();
        UriTemplate invalidLiteral = Parse("ok");
        invalidLiteral.Parts[0].Literal = "bad{x}";
        UriTemplate invalidPercent = Parse("ok");
        invalidPercent.Parts[0].Literal = "%";
        UriTemplate invalidUnicode = Parse("ok");
        invalidUnicode.Parts[0].Literal = new string('\ud800', 1);
        UriTemplate emptyExpression = Parse("{x}");
        emptyExpression.Parts[0].Variables = new UriTemplateVariable[0];
        UriTemplate invalidOperator = Parse("{x}");
        invalidOperator.Parts[0].Operator = (UriTemplateOperator)99;
        UriTemplate invalidVariable = Parse("{x}");
        invalidVariable.Parts[0].Variables[0].Name = "a,b";
        UriTemplate invalidModifier = Parse("{x}");
        invalidModifier.Parts[0].Variables[0].HasPrefix = true;
        invalidModifier.Parts[0].Variables[0].PrefixLength = 0;

        AssertComposerFailure(
            () => composer.ComposeTemplate(null!, new OperationLimits()),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertComposerFailure(
            () => composer.CanonicalizeTemplatePercentCase(
                Parse("x"),
                null!),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                invalidLiteral,
                new OperationLimits()),
            UriDiagnosticCode.InvalidTemplate,
            UriComponent.TemplateLiteral);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                invalidPercent,
                new OperationLimits()),
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.TemplateLiteral);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                invalidUnicode,
                new OperationLimits()),
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                emptyExpression,
                new OperationLimits()),
            UriDiagnosticCode.InvalidTemplateExpression,
            UriComponent.TemplateExpression);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                invalidOperator,
                new OperationLimits()),
            UriDiagnosticCode.InvalidTemplate,
            UriComponent.TemplateExpression);
        AssertComposerFailure(
            () => composer.CanonicalizeTemplatePercentCase(
                invalidVariable,
                new OperationLimits()),
            UriDiagnosticCode.InvalidTemplateVariable,
            UriComponent.TemplateVariable);
        AssertComposerFailure(
            () => composer.ComposeTemplate(
                invalidModifier,
                new OperationLimits()),
            UriDiagnosticCode.InvalidTemplateModifier,
            UriComponent.TemplateVariable);
    }

    [Test]
    public void ComposerPublicMethodsExposeEveryConfiguredLimit()
    {
        IUriTemplateComposer composer = new UriTemplateComposer();
        UriTemplate template = Parse("x{a,b}");
        OperationLimits expressionLimit = new OperationLimits();
        expressionLimit.MaxTemplateExpressions = 0;
        OperationLimits variableLimit = new OperationLimits();
        variableLimit.MaxTemplateVariables = 1;
        OperationLimits outputLimit = new OperationLimits();
        outputLimit.MaxOutputCharacters = 5;

        AssertComposerFailure(
            () => composer.ComposeTemplate(template, expressionLimit),
            UriDiagnosticCode.LimitExceeded,
            UriComponent.TemplateExpression);
        AssertComposerFailure(
            () => composer.ComposeTemplate(template, variableLimit),
            UriDiagnosticCode.LimitExceeded,
            UriComponent.TemplateVariable);
        AssertComposerFailure(
            () => composer.CanonicalizeTemplatePercentCase(
                template,
                outputLimit),
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output);
    }

    [Test]
    public void InspectorPublicMethodsRejectEveryMalformedModelBoundary()
    {
        IUriTemplateInspector inspector = new UriTemplateInspector();
        UriTemplate missingParts = new UriTemplate();
        missingParts.Parts = null!;
        UriTemplate missingPart = new UriTemplate();
        missingPart.Parts = new UriTemplatePart[] { null! };
        UriTemplate missingVariables = Parse("{x}");
        missingVariables.Parts[0].Variables = null!;
        UriTemplate missingVariable = Parse("{x}");
        missingVariable.Parts[0].Variables =
            new UriTemplateVariable[] { null! };
        UriTemplate missingName = Parse("{x}");
        missingName.Parts[0].Variables[0].Name = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateVariables(missingParts));
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateOccurrences(missingPart));
            Assert.Throws<ArgumentException>(() =>
                inspector.DetermineTemplateLevel(missingVariables));
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateVariables(missingVariable));
            Assert.Throws<ArgumentException>(() =>
                inspector.ListTemplateOccurrences(missingName));
        });
    }

    [Test]
    public void EveryProcessorWorkflowPreservesExpansionExceptions()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        string malformed = new string('\ud800', 1);
        UriTemplate template = Parse("{value}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("value", malformed)
        };

        UriTemplateExpansionException parse = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandToUri(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                new UriParseOptions()))!;
        UriTemplateExpansionException resolve = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                "http://example.com/",
                new UriTemplateExpansionOptions(),
                new UriResolutionOptions()))!;
        UriTemplateExpansionException normalize = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandAndNormalize(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                new UriNormalizationOptions()))!;

        AssertProcessorExpansionFailure(parse);
        AssertProcessorExpansionFailure(resolve);
        AssertProcessorExpansionFailure(normalize);
    }

    [Test]
    public void ProcessorWorkflowsMapInvalidDownstreamArgumentsToStageExceptions()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplate template = Parse("next");
        UriTemplateBinding[] bindings = new UriTemplateBinding[0];
        UriParseOptions missingParseLimits = new UriParseOptions();
        missingParseLimits.Limits = null!;
        UriResolutionOptions missingResolutionLimits =
            new UriResolutionOptions();
        missingResolutionLimits.Limits = null!;
        UriNormalizationOptions missingNormalizationLimits =
            new UriNormalizationOptions();
        missingNormalizationLimits.Limits = null!;

        UriSyntaxException parseOptions = Assert.Throws<
            UriSyntaxException>(() => processor.ExpandToUri(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                null!))!;
        UriSyntaxException parseLimits = Assert.Throws<
            UriSyntaxException>(() => processor.ExpandToUri(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                missingParseLimits))!;
        UriResolutionException baseUri = Assert.Throws<
            UriResolutionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                null!,
                new UriTemplateExpansionOptions(),
                new UriResolutionOptions()))!;
        UriResolutionException resolutionLimits = Assert.Throws<
            UriResolutionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                "http://example.com/",
                new UriTemplateExpansionOptions(),
                missingResolutionLimits))!;
        UriNormalizationException normalizationOptions = Assert.Throws<
            UriNormalizationException>(() => processor.ExpandAndNormalize(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                null!))!;
        UriNormalizationException normalizationLimits = Assert.Throws<
            UriNormalizationException>(() => processor.ExpandAndNormalize(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                missingNormalizationLimits))!;

        AssertStageFailure(parseOptions.Diagnostic);
        AssertStageFailure(parseLimits.Diagnostic);
        AssertStageFailure(baseUri.Diagnostic);
        AssertStageFailure(resolutionLimits.Diagnostic);
        AssertStageFailure(normalizationOptions.Diagnostic);
        AssertStageFailure(normalizationLimits.Diagnostic);
    }

    [Test]
    public void ProcessorWorkflowsTreatMissingExpansionOptionsAsTemplateFailures()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplate template = Parse("next");
        UriTemplateBinding[] bindings = new UriTemplateBinding[0];

        UriTemplateExpansionException parse = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandToUri(
                template,
                bindings,
                null!,
                new UriParseOptions()))!;
        UriTemplateExpansionException resolve = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                "http://example.com/",
                null!,
                new UriResolutionOptions()))!;
        UriTemplateExpansionException normalize = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandAndNormalize(
                template,
                bindings,
                null!,
                new UriNormalizationOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(parse.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(resolve.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(normalize.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
        });
    }

    [TestCase("parts")]
    [TestCase("part")]
    [TestCase("original-text")]
    [TestCase("literal")]
    [TestCase("part-range")]
    [TestCase("part-range-offset")]
    [TestCase("part-range-length")]
    [TestCase("part-kind")]
    [TestCase("variables")]
    [TestCase("empty-variables")]
    [TestCase("variable")]
    [TestCase("variable-name")]
    [TestCase("variable-range")]
    [TestCase("variable-range-offset")]
    [TestCase("variable-range-length")]
    [TestCase("operator")]
    [TestCase("binding")]
    [TestCase("binding-name")]
    [TestCase("binding-value")]
    [TestCase("value-kind")]
    [TestCase("scalar")]
    [TestCase("list")]
    [TestCase("list-member")]
    [TestCase("map")]
    [TestCase("map-member")]
    [TestCase("map-key")]
    [TestCase("map-value")]
    public void ExpanderRejectsMalformedCallerCreatedDataFields(
        string mutation)
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        ExpansionInput input = CreateMalformedExpansionInput(mutation);

        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            input.Template,
            input.Bindings,
            new UriTemplateExpansionOptions());
        UriTemplateExpansionException strict = Assert.Throws<
            UriTemplateExpansionException>(() => expander.ExpandTemplate(
                input.Template,
                input.Bindings,
                new UriTemplateExpansionOptions()))!;
        UriTemplateExpansionException partial = Assert.Throws<
            UriTemplateExpansionException>(() =>
                expander.ExpandTemplatePartially(
                    input.Template,
                    input.Bindings,
                    new UriTemplateExpansionOptions()))!;

        Assert.That(result.Diagnostic, Is.Not.Null, mutation);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, mutation);
            Assert.That(result.Text, Is.Empty, mutation);
            Assert.That(result.Length, Is.Zero, mutation);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate), mutation);
            Assert.That(strict.Diagnostic.Code, Is.EqualTo(
                result.Diagnostic.Code), mutation);
            Assert.That(strict.Diagnostic.Component, Is.EqualTo(
                result.Diagnostic.Component), mutation);
            Assert.That(partial.Diagnostic.Code, Is.EqualTo(
                result.Diagnostic.Code), mutation);
            Assert.That(partial.Diagnostic.Component, Is.EqualTo(
                result.Diagnostic.Component), mutation);
        });
    }

    [Test]
    public void ExpanderIgnoresAbsentStorageForInactiveValueShapes()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplateValue undefined = new UriTemplateValue();
        undefined.Scalar = null!;
        undefined.List = null!;
        undefined.Map = null!;
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = "";
        scalar.List = null!;
        scalar.Map = null!;
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.Scalar = null!;
        list.List = new string[] { "a", "" };
        list.Map = null!;
        UriTemplateValue emptyList = new UriTemplateValue();
        emptyList.Kind = UriTemplateValueKind.List;
        UriTemplateValue emptyMap = new UriTemplateValue();
        emptyMap.Kind = UriTemplateValueKind.Map;
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Scalar = null!;
        map.List = null!;
        map.Map = new UriTemplateMapEntry[]
        {
            new UriTemplateMapEntry("k", true, "v"),
            new UriTemplateMapEntry("absent", false, "")
        };

        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            Parse("{?undefined,scalar,emptyList,emptyMap,list,map}"),
            new UriTemplateBinding[]
            {
                new UriTemplateBinding("undefined", undefined),
                new UriTemplateBinding("scalar", scalar),
                new UriTemplateBinding("emptyList", emptyList),
                new UriTemplateBinding("emptyMap", emptyMap),
                new UriTemplateBinding("list", list),
                new UriTemplateBinding("map", map)
            },
            new UriTemplateExpansionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                result.Diagnostic?.Message);
            Assert.That(result.Text, Is.EqualTo(
                "?scalar=&list=a,&map=k,v"));
            Assert.That(result.Length, Is.EqualTo(result.Text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ExpanderIgnoresAbsentFieldsOwnedByTheOtherPartKind()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = Parse("a{value}");
        template.Parts[0].Variables = null!;
        template.Parts[1].Literal = null!;
        template.Parts[1].Variables[0].PrefixLength = -1;

        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            template,
            new UriTemplateBinding[] { ScalarBinding("value", "text") },
            new UriTemplateExpansionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                result.Diagnostic?.Message);
            Assert.That(result.Text, Is.EqualTo("atext"));
            Assert.That(result.Length, Is.EqualTo(5));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static ExpansionInput CreateMalformedExpansionInput(
        string mutation)
    {
        UriTemplate template = Parse("{value}");
        UriTemplateValue value = new UriTemplateValue();
        value.Kind = UriTemplateValueKind.Scalar;
        value.Scalar = "text";
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            new UriTemplateBinding("value", value)
        };

        switch (mutation)
        {
            case "parts":
                template.Parts = null!;
                break;
            case "part":
                template.Parts[0] = null!;
                break;
            case "original-text":
                template.OriginalText = null!;
                break;
            case "literal":
                template = Parse("literal");
                template.Parts[0].Literal = null!;
                break;
            case "part-range":
                template.Parts[0].Range = null!;
                break;
            case "part-range-offset":
                template.Parts[0].Range.Offset = -1;
                break;
            case "part-range-length":
                template.Parts[0].Range.Length = -1;
                break;
            case "part-kind":
                template.Parts[0].Kind = (UriTemplatePartKind)99;
                break;
            case "variables":
                template.Parts[0].Variables = null!;
                break;
            case "empty-variables":
                template.Parts[0].Variables =
                    new UriTemplateVariable[0];
                break;
            case "variable":
                template.Parts[0].Variables[0] = null!;
                break;
            case "variable-name":
                template.Parts[0].Variables[0].Name = null!;
                break;
            case "variable-range":
                template.Parts[0].Variables[0].Range = null!;
                break;
            case "variable-range-offset":
                template.Parts[0].Variables[0].Range.Offset = -1;
                break;
            case "variable-range-length":
                template.Parts[0].Variables[0].Range.Length = -1;
                break;
            case "operator":
                template.Parts[0].Operator = (UriTemplateOperator)99;
                break;
            case "binding":
                bindings[0] = null!;
                break;
            case "binding-name":
                bindings[0].Name = null!;
                break;
            case "binding-value":
                bindings[0].Value = null!;
                break;
            case "value-kind":
                value.Kind = (UriTemplateValueKind)99;
                break;
            case "scalar":
                value.Scalar = null!;
                break;
            case "list":
                value.Kind = UriTemplateValueKind.List;
                value.List = null!;
                break;
            case "list-member":
                value.Kind = UriTemplateValueKind.List;
                value.List = new string[] { null! };
                break;
            case "map":
                value.Kind = UriTemplateValueKind.Map;
                value.Map = null!;
                break;
            case "map-member":
                value.Kind = UriTemplateValueKind.Map;
                value.Map = new UriTemplateMapEntry[] { null! };
                break;
            case "map-key":
                value.Kind = UriTemplateValueKind.Map;
                value.Map = new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry(null!, true, "value")
                };
                break;
            default:
                value.Kind = UriTemplateValueKind.Map;
                value.Map = new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("key", true, null!)
                };
                break;
        }

        return new ExpansionInput(template, bindings);
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

    private static UriTemplateBinding ListBinding(
        string name,
        string[] members)
    {
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = members;
        return new UriTemplateBinding(name, list);
    }

    private static UriTemplateBinding MapBinding(
        string name,
        UriTemplateMapEntry[] members)
    {
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Map = members;
        return new UriTemplateBinding(name, map);
    }

    private static void AssertParseFailure(
        UriTemplateParseResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Template.Parts, Is.Empty);
            Assert.That(result.Template.Level, Is.EqualTo(
                UriTemplateLevel.Level1));
            Assert.That(result.Template.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Message, Is.Not.Empty);
        });
    }

    private static void AssertExpansionFailure(
        UriTemplateExpansionResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Message, Is.Not.Empty);
        });
    }

    private static void AssertComposerFailure(
        TestDelegate action,
        UriDiagnosticCode code,
        UriComponent component)
    {
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(action)!;
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(exception.Message, Is.EqualTo(
                exception.Diagnostic.Message));
        });
    }

    private static void AssertProcessorExpansionFailure(
        UriTemplateExpansionException exception)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.UriTemplate));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
            Assert.That(exception.Message, Is.EqualTo(
                exception.Diagnostic.Message));
        });
    }

    private static void AssertStageFailure(UriDiagnostic diagnostic)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
        });
    }

    private class ExpansionInput
    {
        internal UriTemplate Template;

        internal UriTemplateBinding[] Bindings;

        internal ExpansionInput(
            UriTemplate template,
            UriTemplateBinding[] bindings)
        {
            Template = template;
            Bindings = bindings;
        }
    }
}
