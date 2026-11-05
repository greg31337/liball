namespace uri.Tests;

public class TemplateExpansionPathTests
{
    [Test]
    public void StrictResultAndDiagnosticPathsReturnTheSameSuccessfulExpansion()
    {
        UriTemplate template = Parse("pre{?x}post");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("x", "a b")
        };
        UriTemplateExpansionOptions options = new UriTemplateExpansionOptions();

        string strict = TemplateExpanderCore.ExpandStrict(
            template,
            bindings,
            options);
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
            Assert.That(strict, Is.EqualTo("pre?x=a%20bpost"));
            AssertSuccess(result, strict);
            AssertSuccess(diagnostic, strict);
        });
    }

    [Test]
    public void FailurePathsThrowReturnEmptyOrProduceRfcDiagnosticOutput()
    {
        string malformed = "x" + new string('\ud800', 1);
        UriTemplate template = Parse("pre{?bad}mid{good}post");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("bad", malformed),
            ScalarBinding("good", "ok")
        };
        UriTemplateExpansionOptions options = new UriTemplateExpansionOptions();

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
            AssertFailure(
                result,
                "",
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.TemplateVariable,
                1,
                1,
                DiagnosticFactory.InvalidUnicodeMessage);
            AssertFailure(
                diagnostic,
                "pre{?bad}midokpost",
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.TemplateVariable,
                1,
                1,
                DiagnosticFactory.InvalidUnicodeMessage);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(1));
            Assert.That(exception.Message, Is.EqualTo(
                DiagnosticFactory.InvalidUnicodeMessage));
        });
    }

    [Test]
    public void DiagnosticExpansionPreservesEveryFailingExpressionAndFirstError()
    {
        string malformed = new string('\udc00', 1);
        UriTemplate template = Parse("A{bad}B{list:1}C{good}D{other:2}E");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("bad", malformed),
            ListBinding("list", new string[] { "a" }),
            ScalarBinding("good", "ok"),
            MapBinding("other", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("k", true, "v")
            })
        };

        UriTemplateExpansionResult result =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                new UriTemplateExpansionOptions());

        AssertFailure(
            result,
            "A{bad}B{list:1}CokD{other:2}E",
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateVariable,
            0,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void LiteralFailureStopsAndAppendsTheOriginalRemainderUnexpanded()
    {
        UriTemplate template = Parse("okXtail{good}");
        template.Parts[0].Literal = "ok" + new string('\ud800', 1) + "tail";
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("good", "expanded")
        };

        UriTemplateExpansionResult result =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                new UriTemplateExpansionOptions());

        AssertFailure(
            result,
            "okXtail{good}",
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.TemplateLiteral,
            2,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void OutputLimitAllowsTheExactBoundaryAndRejectsTheNextCharacter()
    {
        UriTemplate template = Parse("abcdef");
        UriTemplateExpansionOptions exact = Options(6, 10);
        UriTemplateExpansionOptions shortLimit = Options(3, 10);

        UriTemplateExpansionResult success = TemplateExpanderCore.ExpandResult(
            template,
            new UriTemplateBinding[0],
            exact);
        UriTemplateExpansionResult failed = TemplateExpanderCore.ExpandResult(
            template,
            new UriTemplateBinding[0],
            shortLimit);
        UriTemplateExpansionResult diagnostic =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                new UriTemplateBinding[0],
                shortLimit);
        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() =>
                TemplateExpanderCore.ExpandStrict(
                    template,
                    new UriTemplateBinding[0],
                    shortLimit))!;

        Assert.Multiple(() =>
        {
            AssertSuccess(success, "abcdef");
            AssertFailure(
                failed,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                3,
                1,
                LimitGuard.OutputLimitMessage);
            AssertFailure(
                diagnostic,
                "abc",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                3,
                1,
                LimitGuard.OutputLimitMessage);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
        });
    }

    [Test]
    public void OutputLimitAlsoBoundsPercentEncodedScalarOutput()
    {
        UriTemplate template = Parse("{value}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("value", " ")
        };

        UriTemplateExpansionResult exact = TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            Options(3, 0));
        UriTemplateExpansionResult diagnostic =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                Options(2, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, "%20");
            AssertFailure(
                diagnostic,
                "%2",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                2,
                1,
                LimitGuard.OutputLimitMessage);
        });
    }

    [Test]
    public void CompositeMemberLimitIsCumulativeAcrossExpressions()
    {
        UriTemplate template = Parse("{list}{map*}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ListBinding("list", new string[] { "a", "b" }),
            MapBinding("map", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("one", true, "1"),
                new UriTemplateMapEntry("two", true, "2")
            })
        };

        UriTemplateExpansionResult exact = TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            Options(100, 4));
        UriTemplateExpansionResult failed = TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            Options(100, 3));
        UriTemplateExpansionResult diagnostic =
            TemplateExpanderCore.ExpandDiagnostic(
                template,
                bindings,
                Options(100, 3));

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, "a,bone=1,two=2");
            AssertFailure(
                failed,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                7,
                0,
                LimitGuard.CollectionLimitMessage);
            AssertFailure(
                diagnostic,
                "a,b{map*}",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                7,
                0,
                LimitGuard.CollectionLimitMessage);
        });
    }

    [Test]
    public void MemberLimitCountsUndefinedEntriesAndRepeatedConsumption()
    {
        UriTemplate mapTemplate = Parse("{map*}");
        UriTemplateBinding[] mapBindings = new UriTemplateBinding[]
        {
            MapBinding("map", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("ignored", false, ""),
                new UriTemplateMapEntry("used", true, "yes")
            })
        };
        UriTemplate listTemplate = Parse("{list}{list}");
        UriTemplateBinding[] listBindings = new UriTemplateBinding[]
        {
            ListBinding("list", new string[] { "one", "two" })
        };

        UriTemplateExpansionResult undefined = TemplateExpanderCore.ExpandResult(
            mapTemplate,
            mapBindings,
            Options(100, 1));
        UriTemplateExpansionResult repeated = TemplateExpanderCore.ExpandResult(
            listTemplate,
            listBindings,
            Options(100, 3));

        Assert.Multiple(() =>
        {
            AssertFailure(
                undefined,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                1,
                0,
                LimitGuard.CollectionLimitMessage);
            AssertFailure(
                repeated,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                7,
                0,
                LimitGuard.CollectionLimitMessage);
        });
    }

    [Test]
    public void InvalidNegativeLimitsFailBeforeExpansion()
    {
        UriTemplate template = Parse("value");
        UriTemplateExpansionOptions output = Options(-1, 0);
        UriTemplateExpansionOptions members = Options(100, -1);

        UriTemplateExpansionResult outputFailure =
            TemplateExpanderCore.ExpandResult(
                template,
                new UriTemplateBinding[0],
                output);
        UriTemplateExpansionResult memberFailure =
            TemplateExpanderCore.ExpandResult(
                template,
                new UriTemplateBinding[0],
                members);

        Assert.Multiple(() =>
        {
            AssertFailure(
                outputFailure,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                0,
                0,
                LimitGuard.OutputLimitMessage);
            AssertFailure(
                memberFailure,
                "",
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                0,
                0,
                LimitGuard.CollectionLimitMessage);
        });
    }

    private static UriTemplate Parse(string text)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Template;
    }

    private static UriTemplateExpansionOptions Options(
        int outputCharacters,
        int compositeMembers)
    {
        UriTemplateExpansionOptions options = new UriTemplateExpansionOptions();
        options.Limits.MaxOutputCharacters = outputCharacters;
        options.Limits.MaxTemplateCompositeMembers = compositeMembers;
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

    private static void AssertSuccess(
        UriTemplateExpansionResult result,
        string expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriTemplateExpansionResult result,
        string expectedText,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.EqualTo(expectedText));
        Assert.That(result.Length, Is.EqualTo(expectedText.Length));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
