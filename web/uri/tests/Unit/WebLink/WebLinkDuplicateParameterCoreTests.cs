namespace uri.Tests;

public class WebLinkDuplicateParameterCoreTests
{
    [Test]
    public void StrictParserKeepsFirstSingletonValuesAndMarksEveryRepeat()
    {
        const string text =
            "<a>; rel=next; REL=previous; rev=up; REV=down; anchor=first; ANCHOR=second; media=screen; MEDIA=print; title=First; TITLE=Second; title*=UTF-8''first; TITLE*=UTF-8''second; type=\"text/plain\"; TYPE=\"image/png\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(7));
            Assert.That(result.LinkSet.Diagnostics,
                Is.SameAs(result.Diagnostics));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "next" }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "up" }));
            Assert.That(link.Anchor, Is.EqualTo("first"));
            Assert.That(link.Media, Is.EqualTo("screen"));
            Assert.That(link.Title, Is.EqualTo("First"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8''first"));
            Assert.That(link.Type, Is.EqualTo("text/plain"));
            Assert.That(link.Parameters.Select(parameter => parameter.Ignored),
                Is.EqualTo(new bool[]
                {
                    false, true,
                    false, true,
                    false, true,
                    false, true,
                    false, true,
                    false, true,
                    false, true
                }));
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Relation,
                    WebLinkParameterKind.Relation,
                    WebLinkParameterKind.ReverseRelation,
                    WebLinkParameterKind.ReverseRelation,
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.Type,
                    WebLinkParameterKind.Type
                }));
        });

        string[] duplicateTexts = new string[]
        {
            "; REL=previous",
            "; REV=down",
            "; ANCHOR=second",
            "; MEDIA=print",
            "; TITLE=Second",
            "; TITLE*=UTF-8''second",
            "; TYPE=\"image/png\""
        };
        int index = 0;
        while (index < duplicateTexts.Length)
        {
            AssertDuplicateWarning(
                result.Diagnostics[index],
                text.IndexOf(duplicateTexts[index]),
                duplicateTexts[index].Length);
            index = index + 1;
        }
    }

    [Test]
    public void HrefLangAndExtensionParametersRemainRepeatable()
    {
        const string text =
            "<a>; hreflang=en; HREFLANG=de; custom=one; CUSTOM=two; custom=three";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en", "de" }));
            Assert.That(link.Parameters.All(parameter =>
                !parameter.Ignored), Is.True);
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Extension,
                    WebLinkParameterKind.Extension,
                    WebLinkParameterKind.Extension
                }));
            Assert.That(link.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[]
                {
                    "en", "de", "one", "two", "three"
                }));
        });
    }

    [Test]
    public void IgnoredRepeatsAreNotSemanticallyValidated()
    {
        const string text =
            "<a>; rel=next; REL=\"bad_name\"; anchor=good; ANCHOR=\"a b\"; type=\"text/plain\"; TYPE=bad; title=ok; TITLE";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Length.EqualTo(4));
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Code == UriDiagnosticCode.DuplicateParameter),
                Is.True);
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "next" }));
            Assert.That(link.Anchor, Is.EqualTo("good"));
            Assert.That(link.Type, Is.EqualTo("text/plain"));
            Assert.That(link.Title, Is.EqualTo("ok"));
            Assert.That(link.Parameters[1].Ignored, Is.True);
            Assert.That(link.Parameters[3].Ignored, Is.True);
            Assert.That(link.Parameters[5].Ignored, Is.True);
            Assert.That(link.Parameters[7].Ignored, Is.True);
        });
    }

    [Test]
    public void RecoveryDoesNotFallBackToAValidDuplicateAfterAnInvalidFirstValue()
    {
        const string text =
            "<a>; rel=\"bad_name\"; REL=next; anchor=\"a b\"; ANCHOR=good; type=\"text/@bad\"; TYPE=\"text/plain\"";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(6));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code),
                Is.EqualTo(new UriDiagnosticCode[]
                {
                    UriDiagnosticCode.InvalidRelationType,
                    UriDiagnosticCode.DuplicateParameter,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.DuplicateParameter,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.DuplicateParameter
                }));
            Assert.That(link.Relations, Is.Empty);
            Assert.That(link.HasAnchor, Is.False);
            Assert.That(link.HasType, Is.False);
            Assert.That(link.Parameters[1].Ignored, Is.True);
            Assert.That(link.Parameters[3].Ignored, Is.True);
            Assert.That(link.Parameters[5].Ignored, Is.True);
            Assert.That(link.Parameters[1].Kind,
                Is.EqualTo(WebLinkParameterKind.Relation));
            Assert.That(link.Parameters[3].Kind,
                Is.EqualTo(WebLinkParameterKind.Anchor));
            Assert.That(link.Parameters[5].Kind,
                Is.EqualTo(WebLinkParameterKind.Type));
        });
    }

    [Test]
    public void StrictParserDoesNotUseAValidDuplicateAfterAnInvalidFirstValue()
    {
        const string text = "<a>; rel=\"bad_name\"; REL=next";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidRelationType));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(text.IndexOf('_')));
        });
    }

    [Test]
    public void StrictDiagnosticsRemainInLinkAndFieldOrder()
    {
        string[] fields = new string[]
        {
            "<a>; title=one; TITLE=two, <b>; media=screen; MEDIA=print",
            "<c>; rel=next; REL=prev"
        };

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValues(
                fields,
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(3));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(fields[0].IndexOf("; TITLE")));
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(fields[0].IndexOf("; MEDIA")));
            Assert.That(result.Diagnostics[2].Range.Offset,
                Is.EqualTo(fields[1].IndexOf("; REL")));
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "a", "b", "c" }));
            Assert.That(result.LinkSet.Links[0].Title, Is.EqualTo("one"));
            Assert.That(result.LinkSet.Links[1].Media,
                Is.EqualTo("screen"));
            Assert.That(result.LinkSet.Links[2].Relations,
                Is.EqualTo(new string[] { "next" }));
        });
    }

    [Test]
    public void PlainAndExtendedTitlesAreDistinctSingletonParameters()
    {
        const string text =
            "<a>; title=fallback; title*=UTF-8''preferred";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.LinkSet.Links[0].HasTitle, Is.True);
            Assert.That(result.LinkSet.Links[0].HasTitleExtended, Is.True);
            Assert.That(result.LinkSet.Links[0].Parameters.All(parameter =>
                !parameter.Ignored), Is.True);
        });
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertDuplicateWarning(
        UriDiagnostic diagnostic,
        int offset,
        int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.DuplicateParameter));
            Assert.That(diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.Component,
                Is.EqualTo(UriComponent.LinkParameter));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(diagnostic.Message,
                Is.EqualTo(
                    WebLinkDuplicateParameterCore.DuplicateParameterMessage));
        });
    }
}
