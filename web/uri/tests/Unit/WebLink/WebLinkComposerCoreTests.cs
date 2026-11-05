namespace uri.Tests;

public class WebLinkComposerCoreTests
{
    [Test]
    public void SerializerUsesModelValuesCanonicalSpacingAndOrderedParameters()
    {
        const string source =
            "<https://example.test/a?x=1> ; zeta = \"token\"" +
            " ; empty=\"\" ; quote=\"a\\\"b\\\\c\"" +
            " ; rel=\"next prev\" ; flag ; type=\"text/html\"";
        WebLinkSet linkSet = Parse(source);
        linkSet.OriginalFieldValues = new string[] { "ignored" };
        linkSet.Links[0].OriginalText = "also ignored";
        linkSet.Links[0].Range = new TextRange(800, 900);

        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            new OperationLimits());

        AssertSuccess(
            result,
            "<https://example.test/a?x=1>; zeta=token; empty=\"\"" +
            "; quote=\"a\\\"b\\\\c\"; rel=\"next prev\"" +
            "; flag; type=\"text/html\"");
    }

    [Test]
    public void SerializerPreservesLinkAndParameterArrayOrderIncludingIgnoredEntries()
    {
        WebLinkSet linkSet = Parse(
            "<a>; first=1; rel=next; REL=prev; last=\"two words\"," +
            " <b>; second=\"token\"");

        Assert.That(linkSet.Links[0].Parameters[2].Ignored, Is.True);
        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            new OperationLimits());
        WebLinkSet reparsed = Parse(result.Text);

        Assert.Multiple(() =>
        {
            AssertSuccess(
                result,
                "<a>; first=1; rel=next; REL=prev; last=\"two words\", " +
                "<b>; second=token");
            Assert.That(reparsed.Links, Has.Length.EqualTo(2));
            Assert.That(
                reparsed.Links[0].Parameters.Select(parameter =>
                    parameter.Name),
                Is.EqualTo(new string[]
                {
                    "first",
                    "rel",
                    "REL",
                    "last"
                }));
        });
    }

    [Test]
    public void SerializerEncodesExtendedValuesAsUtf8WithLanguageMetadata()
    {
        WebLinkParameter title = new WebLinkParameter();
        title.Name = "title*";
        title.Kind = WebLinkParameterKind.TitleExtended;
        title.HasValue = true;
        title.Value = "nächstes Kapitel 😀";
        title.RawValue = "ignored";
        title.WasQuoted = true;
        title.Extended = true;
        title.CharacterSet = "ISO-8859-1";
        title.Language = "de-CH";

        WebLinkParameter extension = new WebLinkParameter();
        extension.Name = "example*";
        extension.HasValue = true;
        extension.Value = "£ rates";
        extension.Extended = true;
        extension.Language = "en";

        WebLinkSet linkSet = Set(Link("resource", title, extension));
        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            new OperationLimits());

        AssertSuccess(
            result,
            "<resource>; title*=UTF-8'de-CH'n%C3%A4chstes%20Kapitel%20" +
            "%F0%9F%98%80; example*=UTF-8'en'%C2%A3%20rates");
    }

    [Test]
    public void SerializerQuotesEmptySeparatorsTabsAndObsText()
    {
        WebLinkParameter empty = Parameter("empty", "");
        WebLinkParameter separators = Parameter("separators", "a,b;c");
        WebLinkParameter tab = Parameter("tab", "a\tb");
        WebLinkParameter obsText = Parameter("label", "café");

        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            Set(Link("target", empty, separators, tab, obsText)),
            new OperationLimits());

        AssertSuccess(
            result,
            "<target>; empty=\"\"; separators=\"a,b;c\"" +
            "; tab=\"a\tb\"; label=\"café\"");
    }

    [Test]
    public void SerializerNeverIntroducesObsoleteLineFolding()
    {
        WebLinkParameter title = Parameter(
            "title",
            new string('x', 300));
        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            Set(Link("one", title), Link("two", title)),
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Does.Not.Contain("\r"));
            Assert.That(result.Text, Does.Not.Contain("\n"));
            Assert.That(result.Text, Does.Contain(", <two>"));
        });
    }

    [Test]
    public void MeasurementMatchesCompositionAndOutputLimitIsExact()
    {
        WebLinkSet linkSet = Set(Link(
            "target",
            Parameter("rel", "next prev"),
            Parameter("flag")));
        const string expected =
            "<target>; rel=\"next prev\"; flag";

        UriTextResult exact = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            Limits(expected.Length));
        UriMeasureResult measurement =
            WebLinkComposerCore.MeasureLinkFieldValue(
                linkSet,
                Limits(expected.Length));
        UriTextResult shortResult =
            WebLinkComposerCore.ComposeLinkFieldValue(
                linkSet,
                Limits(expected.Length - 1));

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, expected);
            Assert.That(measurement.Success, Is.True);
            Assert.That(measurement.Length, Is.EqualTo(expected.Length));
            Assert.That(measurement.Diagnostic, Is.Null);
            AssertFailure(
                shortResult,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
            Assert.That(shortResult.Diagnostic!.Range.Offset,
                Is.EqualTo(expected.Length - 1));
        });
    }

    [Test]
    public void SerializerRejectsValuesThatCannotFormAValidLinkField()
    {
        WebLinkParameter badName = Parameter("bad name", "value");
        WebLinkParameter control = Parameter("title", "bad\rvalue");
        WebLinkParameter unicode = Parameter("title", "€");
        WebLinkParameter badLanguage = Parameter("title*", "value");
        badLanguage.Extended = true;
        badLanguage.Language = "en_US";
        WebLinkParameter inconsistent = Parameter("plain", "value");
        inconsistent.Extended = true;

        UriTextResult targetResult =
            WebLinkComposerCore.ComposeLinkFieldValue(
                Set(Link("bad target")),
                new OperationLimits());
        UriTextResult nameResult = ComposeParameter(badName);
        UriTextResult controlResult = ComposeParameter(control);
        UriTextResult unicodeResult = ComposeParameter(unicode);
        UriTextResult languageResult = ComposeParameter(badLanguage);
        UriTextResult inconsistentResult = ComposeParameter(inconsistent);

        Assert.Multiple(() =>
        {
            AssertFailure(
                targetResult,
                UriDiagnosticCode.InvalidLinkTarget,
                UriComponent.LinkTarget);
            AssertFailure(
                nameResult,
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter);
            AssertFailure(
                controlResult,
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter);
            AssertFailure(
                unicodeResult,
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter);
            AssertFailure(
                languageResult,
                UriDiagnosticCode.InvalidLanguageTag,
                UriComponent.LanguageTag);
            AssertFailure(
                inconsistentResult,
                UriDiagnosticCode.InvalidWebLink,
                UriComponent.LinkParameter);
        });
    }

    private static UriTextResult ComposeParameter(WebLinkParameter parameter)
    {
        return WebLinkComposerCore.ComposeLinkFieldValue(
            Set(Link("target", parameter)),
            new OperationLimits());
    }

    private static WebLinkSet Parse(string text)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());
        Assert.That(result.Success, Is.True);
        return result.LinkSet;
    }

    private static WebLinkParameter Parameter(string name, string value)
    {
        WebLinkParameter parameter = new WebLinkParameter();
        parameter.Name = name;
        parameter.HasValue = true;
        parameter.Value = value;
        return parameter;
    }

    private static WebLinkParameter Parameter(string name)
    {
        WebLinkParameter parameter = new WebLinkParameter();
        parameter.Name = name;
        return parameter;
    }

    private static WebLink Link(
        string target,
        params WebLinkParameter[] parameters)
    {
        WebLink link = new WebLink();
        link.Target = target;
        link.Parameters = parameters;
        return link;
    }

    private static WebLinkSet Set(params WebLink[] links)
    {
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = links;
        return linkSet;
    }

    private static OperationLimits Limits(int outputCharacters)
    {
        OperationLimits limits = new OperationLimits();
        limits.MaxOutputCharacters = outputCharacters;
        return limits;
    }

    private static void AssertSuccess(UriTextResult result, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
        });
    }
}
