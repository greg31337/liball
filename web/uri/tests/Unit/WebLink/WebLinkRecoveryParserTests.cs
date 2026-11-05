namespace uri.Tests;

public class WebLinkRecoveryParserTests
{
    [Test]
    public void RecoveryModeLeavesValidInputUnchangedWithoutDiagnostics()
    {
        const string text =
            "<https://example.test/a,b>; rel=\"next prev\"; title=\"a,b;c\", <other>";
        WebLinkParseOptions options = CreateRecoveryOptions();

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.LinkSet.Diagnostics, Is.Empty);
            Assert.That(result.LinkSet.Links, Has.Length.EqualTo(2));
            Assert.That(result.LinkSet.Links[0].Target,
                Is.EqualTo("https://example.test/a,b"));
            Assert.That(result.LinkSet.Links[0].Parameters[0].Value,
                Is.EqualTo("next prev"));
            Assert.That(result.LinkSet.Links[0].Parameters[1].Value,
                Is.EqualTo("a,b;c"));
            Assert.That(result.LinkSet.Links[1].Target,
                Is.EqualTo("other"));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(new string[] { text }));
        });
    }

    [Test]
    public void RecoveryStopsAtUnrecognizedTextAndRetainsEarlierLinks()
    {
        const string text = "<a>, garbage, <b>";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.LinkSet.Diagnostics,
                Is.SameAs(result.Diagnostics));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            text.IndexOf("garbage"),
            text.Length - text.IndexOf("garbage"),
            WebLinkRecoveryParserCore.DiscardedRemainderMessage);
    }

    [Test]
    public void RecoveryContinuesWithLaterFieldValuesAfterDiscardingOneField()
    {
        string[] fields = new string[]
        {
            "garbage, <ignored>",
            "<a>",
            "<b>;x=1"
        };

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
                fields,
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(fields));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            0,
            fields[0].Length,
            WebLinkRecoveryParserCore.DiscardedRemainderMessage);
    }

    [Test]
    public void RecoveryDiscardsATruncatedFinalTargetAndKeepsPriorLinks()
    {
        const string text = "<a>, <unterminated";
        int truncatedOffset = text.IndexOf("<unterminated");

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            truncatedOffset,
            text.Length - truncatedOffset,
            WebLinkRecoveryParserCore.TruncatedTargetMessage);
    }

    [Test]
    public void RecoveryDiscardsInvalidUriTargetsAndContinuesAfterComma()
    {
        const string text = "<http://[bad]>, <good>";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "good" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            12,
            0,
            WebLinkRecoveryParserCore.InvalidTargetMessage);
    }

    [TestCase("<a><b>", 3)]
    [TestCase("<a> <b>", 4)]
    [TestCase("<a>\t<b>", 4)]
    public void RecoveryRepairsMissingCommaSeparators(
        string text,
        int expectedOffset)
    {
        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            expectedOffset,
            0,
            WebLinkRecoveryParserCore.MissingSeparatorMessage);
    }

    [Test]
    public void RecoveryRetainsEmptyAndNonTokenParameterNames()
    {
        const string text =
            "<a>; =value; na@me=ok; good=x";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "", "na@me", "good" }));
            Assert.That(link.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[] { "value", "ok", "x" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.IndexOf('=', text.IndexOf(';')),
            0,
            WebLinkRecoveryParserCore.InvalidParameterNameMessage);
        AssertWarning(
            result.Diagnostics[1],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.IndexOf('@'),
            1,
            WebLinkRecoveryParserCore.InvalidParameterNameMessage);
    }

    [Test]
    public void RecoveryRetainsEmptyAndNonTokenUnquotedValues()
    {
        const string text =
            "<a>; empty=; spaced=hello world; good=ok";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "empty", "spaced", "good" }));
            Assert.That(link.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[] { "", "hello world", "ok" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.IndexOf("; spaced"),
            0,
            WebLinkRecoveryParserCore.InvalidParameterValueMessage);
        AssertWarning(
            result.Diagnostics[1],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.IndexOf("hello world") + "hello".Length,
            1,
            WebLinkRecoveryParserCore.InvalidParameterValueMessage);
    }

    [Test]
    public void RecoveryRetainsUnterminatedQuotedStrings()
    {
        const string text = "<a>; title=\"unterminated";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links[0].Parameters[0].Value,
                Is.EqualTo("unterminated"));
            Assert.That(result.LinkSet.Links[0].Parameters[0].WasQuoted,
                Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.Length,
            0,
            WebLinkRecoveryParserCore.UnterminatedQuotedStringMessage);
    }

    [Test]
    public void RecoveryRetainsQuotedTextBeforeADanglingEscape()
    {
        const string text = "<a>; title=\"abc\\";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links[0].Parameters[0].Value,
                Is.EqualTo("abc"));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.Length - 1,
            1,
            WebLinkRecoveryParserCore.IncompleteQuotedPairMessage);
    }

    [Test]
    public void RecoveryReportsInvalidQuotedTextAndContinuesToLaterParameters()
    {
        const string text =
            "<a>; first=\"a\rb\"; second=\"a\\\rb\"; good=yes";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(link.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[] { "a\rb", "a\rb", "yes" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        });
        AssertWarning(
            result.Diagnostics[0],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.IndexOf('\r'),
            1,
            WebLinkRecoveryParserCore.InvalidQuotedContentMessage);
        AssertWarning(
            result.Diagnostics[1],
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            text.LastIndexOf('\r'),
            1,
            WebLinkRecoveryParserCore.InvalidQuotedContentMessage);
    }

    [Test]
    public void RecoveryQuotedStringsStillProtectCommaAndSemicolonDelimiters()
    {
        const string text =
            "<a>; title=\"a,b;c\", <b>; x=\"c,d\"";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(result.LinkSet.Links[0].Parameters[0].Value,
                Is.EqualTo("a,b;c"));
            Assert.That(result.LinkSet.Links[1].Parameters[0].Value,
                Is.EqualTo("c,d"));
        });
    }

    [Test]
    public void RecoveryDiagnosticsRemainInFieldAndSourceOrder()
    {
        string[] fields = new string[]
        {
            "<a>; bad@name=x; empty=",
            "<b><c>",
            "trailing"
        };

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
                fields,
                CreateRecoveryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(4));
            Assert.That(result.Diagnostics.Select(item => item.Message),
                Is.EqualTo(new string[]
                {
                    WebLinkRecoveryParserCore.InvalidParameterNameMessage,
                    WebLinkRecoveryParserCore.InvalidParameterValueMessage,
                    WebLinkRecoveryParserCore.MissingSeparatorMessage,
                    WebLinkRecoveryParserCore.DiscardedRemainderMessage
                }));
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b", "c" }));
        });
    }

    [Test]
    public void RecoveryAndStrictModesRemainExplicitAndIndependent()
    {
        const string text = "<a><b>";
        WebLinkParseOptions recoveryOptions = CreateRecoveryOptions();
        WebLinkParseOptions strictOptions = new WebLinkParseOptions();

        WebLinkParseResult recovered =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                recoveryOptions);
        WebLinkParseResult strict =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                strictOptions);
        WebLinkParseResult recoveryWithStrictOptions =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                strictOptions);
        WebLinkParseResult strictWithRecoveryOptions =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                recoveryOptions);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(strict.Success, Is.False);
        });
        AssertInvalidInput(recoveryWithStrictOptions);
        AssertInvalidInput(strictWithRecoveryOptions);
    }

    [Test]
    public void RecoveryRejectsInvalidOptionsCollectionsAndMembers()
    {
        WebLinkParseOptions missingLimits = CreateRecoveryOptions();
        missingLimits.Limits = null!;

        WebLinkParseResult nullOptions =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<a>".AsSpan(),
                null);
        WebLinkParseResult nullLimits =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<a>".AsSpan(),
                missingLimits);
        WebLinkParseResult nullCollection =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
                null,
                CreateRecoveryOptions());
        WebLinkParseResult nullMember =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
                new string[] { "<a>", null! },
                CreateRecoveryOptions());

        AssertInvalidInput(nullOptions);
        AssertInvalidInput(nullLimits);
        AssertInvalidInput(nullCollection);
        AssertInvalidInput(nullMember);
    }

    [Test]
    public void RecoveryKeepsLimitsFatalInsteadOfRepairingThem()
    {
        WebLinkParseOptions inputOptions = CreateRecoveryOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        WebLinkParseOptions targetOptions = CreateRecoveryOptions();
        targetOptions.Limits.MaxComponentCharacters = 1;
        WebLinkParseOptions linkOptions = CreateRecoveryOptions();
        linkOptions.Limits.MaxWebLinks = 1;
        WebLinkParseOptions parameterOptions = CreateRecoveryOptions();
        parameterOptions.Limits.MaxWebLinkParameters = 1;

        WebLinkParseResult input =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<a>".AsSpan(),
                inputOptions);
        WebLinkParseResult target =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<ab>".AsSpan(),
                targetOptions);
        WebLinkParseResult links =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<a>,<b>".AsSpan(),
                linkOptions);
        WebLinkParseResult parameters =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "<a>;x=1;y=2".AsSpan(),
                parameterOptions);

        AssertFatalLimit(input, UriComponent.Input);
        AssertFatalLimit(target, UriComponent.LinkTarget);
        AssertFatalLimit(links, UriComponent.WebLink);
        AssertFatalLimit(parameters, UriComponent.LinkParameter);
    }

    [Test]
    public void RecoveryRetainsWarningsThatOccurBeforeAFatalLimit()
    {
        const string text = "<a>; bad@name=x; second=y";
        WebLinkParseOptions options = CreateRecoveryOptions();
        options.Limits.MaxWebLinkParameters = 1;

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.LinkSet.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity,
                Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(
                    WebLinkRecoveryParserCore.InvalidParameterNameMessage));
            Assert.That(result.Diagnostics[1], Is.SameAs(result.Diagnostic));
            Assert.That(result.Diagnostic!.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        });
    }

    [Test]
    public void RecoveryRejectsNegativeRelevantLimitsForEmptyInput()
    {
        WebLinkParseOptions input = CreateRecoveryOptions();
        input.Limits.MaxInputCharacters = -1;
        WebLinkParseOptions component = CreateRecoveryOptions();
        component.Limits.MaxComponentCharacters = -1;
        WebLinkParseOptions links = CreateRecoveryOptions();
        links.Limits.MaxWebLinks = -1;
        WebLinkParseOptions parameters = CreateRecoveryOptions();
        parameters.Limits.MaxWebLinkParameters = -1;

        AssertFatalLimit(
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "".AsSpan(),
                input),
            UriComponent.Input);
        AssertFatalLimit(
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "".AsSpan(),
                component),
            UriComponent.LinkTarget);
        AssertFatalLimit(
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "".AsSpan(),
                links),
            UriComponent.WebLink);
        AssertFatalLimit(
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                "".AsSpan(),
                parameters),
            UriComponent.LinkParameter);
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertWarning(
        UriDiagnostic diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(diagnostic.Message, Is.EqualTo(message));
        });
    }

    private static void AssertInvalidInput(WebLinkParseResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.Input));
        });
    }

    private static void AssertFatalLimit(
        WebLinkParseResult result,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
        });
    }
}
