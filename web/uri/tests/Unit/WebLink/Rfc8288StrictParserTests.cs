namespace uri.Tests;

public class Rfc8288StrictParserTests
{
    [Test]
    public void SectionThreePointFivePreviousChapterExampleParses()
    {
        const string fieldValue =
            "<http://example.com/TheBook/chapter2>; rel=\"previous\"; " +
            "title=\"previous chapter\"";

        WebLinkSet linkSet = ParseField(fieldValue);
        WebLink link = linkSet.Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(linkSet.Links, Has.Length.EqualTo(1));
            Assert.That(link.Target,
                Is.EqualTo("http://example.com/TheBook/chapter2"));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "previous" }));
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("previous chapter"));
        });
    }

    [Test]
    public void SectionThreePointFiveExtensionRelationExampleParses()
    {
        WebLink link = ParseField(
            "</>; rel=\"http://example.net/foo\"").Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(link.Target, Is.EqualTo("/"));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "http://example.net/foo" }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Extension
                }));
        });
    }

    [Test]
    public void SectionThreePointFiveCopyrightAnchorExampleParses()
    {
        WebLink link = ParseField(
            "</terms>; rel=\"copyright\"; anchor=\"#foo\"").Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(link.Target, Is.EqualTo("/terms"));
            Assert.That(link.Relations,
                Is.EqualTo(new string[] { "copyright" }));
            Assert.That(link.HasAnchor, Is.True);
            Assert.That(link.Anchor, Is.EqualTo("#foo"));
        });
    }

    [Test]
    public void SectionThreePointFiveInternationalizedTitleExampleParses()
    {
        const string fieldValue =
            "</TheBook/chapter2>; rel=\"previous\"; " +
            "title*=UTF-8'de'letztes%20Kapitel, " +
            "</TheBook/chapter4>; rel=\"next\"; " +
            "title*=UTF-8'de'n%c3%a4chstes%20Kapitel";

        WebLinkSet linkSet = ParseField(fieldValue);

        Assert.Multiple(() =>
        {
            Assert.That(linkSet.Links, Has.Length.EqualTo(2));
            Assert.That(linkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[]
                {
                    "/TheBook/chapter2",
                    "/TheBook/chapter4"
                }));
            Assert.That(linkSet.Links.Select(link => link.TitleExtended),
                Is.EqualTo(new string[]
                {
                    "letztes Kapitel",
                    "nächstes Kapitel"
                }));
            Assert.That(linkSet.Links.All(link =>
                link.TitleExtendedLanguage == "de"), Is.True);
            Assert.That(linkSet.Links.All(link =>
                link.TitleExtendedCharacterSet == "UTF-8"), Is.True);
        });
    }

    [Test]
    public void SectionThreePointFiveMultipleRelationsExampleParses()
    {
        WebLink link = ParseField(
            "<http://example.org/>; " +
            "rel=\"start http://example.net/relation/other\"")
            .Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(link.Relations,
                Is.EqualTo(new string[]
                {
                    "start",
                    "http://example.net/relation/other"
                }));
            Assert.That(link.RelationKinds,
                Is.EqualTo(new WebLinkRelationKind[]
                {
                    WebLinkRelationKind.Registered,
                    WebLinkRelationKind.Extension
                }));
        });
    }

    [Test]
    public void CombinedAndRepeatedSectionExamplesHaveEquivalentLinks()
    {
        const string first =
            "<https://example.org/>; rel=\"start\"";
        const string second =
            "<https://example.org/index>; rel=\"index\"";
        WebLinkSet combined = ParseFields(new string[]
        {
            first + ", " + second
        });
        WebLinkSet repeated = ParseFields(new string[]
        {
            first,
            second
        });

        Assert.Multiple(() =>
        {
            Assert.That(combined.Links.Select(link => link.Target),
                Is.EqualTo(repeated.Links.Select(link => link.Target)));
            Assert.That(combined.Links.Select(link => link.Relations[0]),
                Is.EqualTo(repeated.Links.Select(link =>
                    link.Relations[0])));
            Assert.That(combined.OriginalFieldValues, Has.Length.EqualTo(1));
            Assert.That(repeated.OriginalFieldValues, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void StrictGrammarKeepsQuotedAndAngleDelimitersInsideValues()
    {
        const string fieldValue =
            "\t<relative,a;b>\t ;\t title \t=\t " +
            "\"one,two;three\\\"four\\\\five\" \t,\t,\t <next>\t";

        WebLinkSet linkSet = ParseField(fieldValue);

        Assert.Multiple(() =>
        {
            Assert.That(linkSet.Links, Has.Length.EqualTo(2));
            Assert.That(linkSet.Links[0].Target,
                Is.EqualTo("relative,a;b"));
            Assert.That(linkSet.Links[0].Title,
                Is.EqualTo("one,two;three\"four\\five"));
            Assert.That(linkSet.Links[0].Parameters[0].WasQuoted,
                Is.True);
            Assert.That(linkSet.Links[1].Target, Is.EqualTo("next"));
        });
    }

    [TestCase("http://example.test/; rel=next")]
    [TestCase("\"http://example.test/\"; rel=next")]
    [TestCase("<http://example.test/; rel=next")]
    [TestCase("<one> <two>")]
    [TestCase("<one>; title=unquoted,value")]
    public void StrictGrammarRejectsMissingAngleOrCommaDelimiters(string text)
    {
        WebLinkParseResult result = TryParseField(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
        });
    }

    [Test]
    public void StrictGrammarMapsEveryStandardParameter()
    {
        const string fieldValue =
            "<target>; rel=\"next https://relations.test/custom\"; " +
            "rev=up; anchor=\"#part\"; hreflang=en; hreflang=de-CH; " +
            "media=\"screen, print\"; title=\"plain title\"; " +
            "title*=UTF-8'de'bevorzugter%20Titel; " +
            "type=\"application/problem+json\"";

        WebLink link = ParseField(fieldValue).Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Relation,
                    WebLinkParameterKind.ReverseRelation,
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.Type
                }));
            Assert.That(link.Relations,
                Is.EqualTo(new string[]
                {
                    "next",
                    "https://relations.test/custom"
                }));
            Assert.That(link.ReverseRelations,
                Is.EqualTo(new string[] { "up" }));
            Assert.That(link.Anchor, Is.EqualTo("#part"));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en", "de-CH" }));
            Assert.That(link.Media, Is.EqualTo("screen, print"));
            Assert.That(link.Title, Is.EqualTo("plain title"));
            Assert.That(link.TitleExtended,
                Is.EqualTo("bevorzugter Titel"));
            Assert.That(link.Type,
                Is.EqualTo("application/problem+json"));
        });
    }

    private static WebLinkSet ParseField(string fieldValue)
    {
        WebLinkParseResult result = TryParseField(fieldValue);
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.LinkSet;
    }

    private static WebLinkSet ParseFields(string[] fieldValues)
    {
        WebLinkParseResult result = WebLinkParserCore.ParseStrictFieldValues(
            fieldValues,
            new WebLinkParseOptions());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.LinkSet;
    }

    private static WebLinkParseResult TryParseField(string fieldValue)
    {
        return WebLinkParserCore.ParseStrictFieldValueSpan(
            fieldValue.AsSpan(),
            new WebLinkParseOptions());
    }
}
