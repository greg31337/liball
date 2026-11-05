namespace uri.Tests;

public class UriTemplateAdvancedBehaviorTests
{
    [Test]
    public void DiagnosticExpansionRollsBackFailuresBeforeApplyingOutputLimit()
    {
        string malformed = new string('\ud800', 1);
        UriTemplate template = Parse("A{bad}B{good}C");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("bad", malformed),
            ScalarBinding("good", "ok")
        };
        UriTemplateExpansionOptions exactOptions = ExpansionOptions(10, 1024);
        UriTemplateExpansionOptions shortOptions = ExpansionOptions(9, 1024);

        UriTemplateExpansionResult exact =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                exactOptions);
        UriTemplateExpansionResult shortResult =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                shortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.False);
            Assert.That(exact.Text, Is.EqualTo("A{bad}BokC"));
            Assert.That(exact.Diagnostic, Is.Not.Null);
            Assert.That(exact.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Text, Is.EqualTo("A{bad}Bok"));
            Assert.That(shortResult.Diagnostic, Is.Not.Null);
            Assert.That(shortResult.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(shortResult.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
        });
    }

    [Test]
    public void StrictResultAndDiagnosticExpansionShareCompositeBudgetFailure()
    {
        UriTemplate template = Parse("{list}{?map*}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ListBinding("list", new string[] { "a", "b" }),
            MapBinding("map", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("x", true, "1")
            })
        };
        UriTemplateExpansionOptions options = ExpansionOptions(1024, 2);

        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() =>
                TemplateExpanderCore.ExpandStrict(
                    template,
                    bindings,
                    options))!;
        UriTemplateExpansionResult result = TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            options);
        UriTemplateExpansionResult diagnostic =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                options);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.TemplateVariable));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(8));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic!.Range.Offset, Is.EqualTo(8));
            Assert.That(diagnostic.Success, Is.False);
            Assert.That(diagnostic.Text, Is.EqualTo("a,b{?map*}"));
            Assert.That(diagnostic.Diagnostic!.Range.Offset, Is.EqualTo(8));
        });
    }

    [Test]
    public void PartialExpansionDefersAnUnresolvedExpressionsMemberBudget()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = Parse("{list}/{known}{?map*,missing}");
        UriTemplateBinding[] firstBindings = new UriTemplateBinding[]
        {
            ListBinding("list", new string[] { "a", "b" }),
            ScalarBinding("known", "ok"),
            MapBinding("map", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("x", true, "1"),
                new UriTemplateMapEntry("y", true, "2")
            })
        };

        string partial = expander.ExpandTemplatePartially(
            template,
            firstBindings,
            ExpansionOptions(1024, 2));
        string completed = expander.ExpandTemplate(
            Parse(partial),
            new UriTemplateBinding[]
            {
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("x", true, "1"),
                    new UriTemplateMapEntry("y", true, "2")
                }),
                ScalarBinding("missing", "yes")
            },
            ExpansionOptions(1024, 2));
        string direct = expander.ExpandTemplate(
            template,
            new UriTemplateBinding[]
            {
                ListBinding("list", new string[] { "a", "b" }),
                ScalarBinding("known", "ok"),
                MapBinding("map", new UriTemplateMapEntry[]
                {
                    new UriTemplateMapEntry("x", true, "1"),
                    new UriTemplateMapEntry("y", true, "2")
                }),
                ScalarBinding("missing", "yes")
            },
            ExpansionOptions(1024, 4));

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo(
                "a,b/ok{?map*,missing}"));
            Assert.That(completed, Is.EqualTo(
                "a,b/ok?x=1&y=2&missing=yes"));
            Assert.That(completed, Is.EqualTo(direct));
        });
    }

    [Test]
    public void ReparsedPartialTemplateCanBeComposedCanonicalizedAndInspected()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();
        IUriTemplateComposer composer = new UriTemplateComposer();
        IUriTemplateInspector inspector = new UriTemplateInspector();
        string partial = expander.ExpandTemplatePartially(
            Parse("pre%2f{?A%2ab,b,A%2ab}{/items*}"),
            new UriTemplateBinding[]
            {
                ListBinding("items", new string[] { "x" })
            },
            new UriTemplateExpansionOptions());
        UriTemplate reparsed = Parse(partial);

        string composed = composer.ComposeTemplate(
            reparsed,
            new OperationLimits());
        string canonical = composer.CanonicalizeTemplatePercentCase(
            reparsed,
            new OperationLimits());
        string[] variables = inspector.ListTemplateVariables(reparsed);
        UriTemplateVariable[] occurrences =
            inspector.ListTemplateOccurrences(reparsed);
        UriTemplateLevel level = inspector.DetermineTemplateLevel(reparsed);

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo(
                "pre%2f{?A%2ab,b,A%2ab}/x"));
            Assert.That(composed, Is.EqualTo(partial));
            Assert.That(canonical, Is.EqualTo(
                "pre%2F{?A%2Ab,b,A%2Ab}/x"));
            Assert.That(reparsed.OriginalText, Is.EqualTo(partial));
            Assert.That(variables, Is.EqualTo(new string[]
            {
                "A%2ab",
                "b"
            }));
            Assert.That(occurrences, Has.Length.EqualTo(3));
            Assert.That(level, Is.EqualTo(UriTemplateLevel.Level3));
        });
    }

    [Test]
    public void OneTemplateSupportsEveryHighLevelProcessingWorkflow()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplate template = Parse(
            "{scheme}://{host}/a/../{segment}{?q}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("scheme", "HTTP"),
            ScalarBinding("host", "EXAMPLE.COM"),
            ScalarBinding("segment", "c d"),
            ScalarBinding("q", "x/y")
        };

        UriReference parsed = processor.ExpandToUri(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            new UriParseOptions());
        string resolved = processor.ExpandAndResolve(
            template,
            bindings,
            "https://base.example/root/",
            new UriTemplateExpansionOptions(),
            new UriResolutionOptions());
        string normalized = processor.ExpandAndNormalize(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.OriginalText, Is.EqualTo(
                "HTTP://EXAMPLE.COM/a/../c%20d?q=x%2Fy"));
            Assert.That(resolved, Is.EqualTo(
                "HTTP://EXAMPLE.COM/c%20d?q=x%2Fy"));
            Assert.That(normalized, Is.EqualTo(
                "http://example.com/c%20d?q=x%2Fy"));
        });
    }

    [Test]
    public void HighLevelWorkflowsReportTheirDownstreamOutputLimits()
    {
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplateExpansionOptions expansionOptions =
            new UriTemplateExpansionOptions();
        UriTemplate resolveTemplate = Parse("{path}");
        UriTemplateBinding[] resolveBindings = new UriTemplateBinding[]
        {
            ScalarBinding("path", "next")
        };
        UriResolutionOptions resolutionOptions = new UriResolutionOptions();
        string resolved = processor.ExpandAndResolve(
            resolveTemplate,
            resolveBindings,
            "http://example.com/a/",
            expansionOptions,
            resolutionOptions);
        resolutionOptions.Limits.MaxOutputCharacters = resolved.Length - 1;

        UriTemplate normalizeTemplate = Parse(
            "HTTP://EXAMPLE.COM/a/./b");
        UriNormalizationOptions normalizationOptions =
            new UriNormalizationOptions();
        string normalized = processor.ExpandAndNormalize(
            normalizeTemplate,
            new UriTemplateBinding[0],
            expansionOptions,
            normalizationOptions);
        normalizationOptions.Limits.MaxOutputCharacters =
            normalized.Length - 1;

        UriResolutionException resolution = Assert.Throws<
            UriResolutionException>(() => processor.ExpandAndResolve(
                resolveTemplate,
                resolveBindings,
                "http://example.com/a/",
                expansionOptions,
                resolutionOptions))!;
        UriNormalizationException normalization = Assert.Throws<
            UriNormalizationException>(() => processor.ExpandAndNormalize(
                normalizeTemplate,
                new UriTemplateBinding[0],
                expansionOptions,
                normalizationOptions))!;

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(resolution.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
            Assert.That(normalization.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(normalization.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
        });
    }

    [Test]
    public void StrictExpansionExceptionIdentifiesTheFirstRejectedOutputCharacter()
    {
        IUriTemplateExpander expander = new UriTemplateExpander();

        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() => expander.ExpandTemplate(
                Parse("abcdef"),
                new UriTemplateBinding[0],
                ExpansionOptions(5, 1024)))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(5));
            Assert.That(exception.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    private static UriTemplate Parse(string text)
    {
        IUriTemplateParser parser = new UriTemplateParser();
        return parser.ParseTemplate(text, new UriTemplateParseOptions());
    }

    private static UriTemplateExpansionOptions ExpansionOptions(
        int maximumOutputCharacters,
        int maximumCompositeMembers)
    {
        UriTemplateExpansionOptions options =
            new UriTemplateExpansionOptions();
        options.Limits.MaxOutputCharacters = maximumOutputCharacters;
        options.Limits.MaxTemplateCompositeMembers = maximumCompositeMembers;
        return options;
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
}
