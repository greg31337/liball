namespace uri.Tests;

public class WebLinkGeneratedRoundTripTests
{
    [TestCaseSource(nameof(RoundTripCases))]
    public void GeneratedFieldValuesPreserveSemanticLinkData(
        string target,
        string relations,
        string titleParameter)
    {
        string fieldValue =
            "<" + target + ">; rel=\"" + relations + "\"; rev=UP; " +
            "anchor=\"#context\"; hreflang=en; hreflang=de-CH; " +
            "media=\"screen, print\"; title=" + titleParameter + "; " +
            "title*=UTF-8'de'n%C3%A4chstes%20Kapitel; " +
            "type=\"application/problem+json\"; profile=compact; flag, " +
            "<second/path?value=%2F#part>; rel=alternate; x=\"a,b;c\"";
        IWebLinkParser parser = new WebLinkParser();
        IWebLinkComposer composer = new WebLinkComposer();
        OperationLimits limits = new OperationLimits();

        WebLinkSet original = parser.ParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());
        string canonical = composer.ComposeLinkFieldValue(original, limits);
        UriTextResult result = composer.TryComposeLinkFieldValue(
            original,
            limits);
        int measured = composer.MeasureLinkFieldValue(original, limits);
        WebLinkSet reparsed = parser.ParseLinkFieldValues(
            new string[] { canonical },
            new WebLinkParseOptions());
        string canonicalAgain = composer.ComposeLinkFieldValue(
            reparsed,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(canonical));
            Assert.That(result.Length, Is.EqualTo(canonical.Length));
            Assert.That(measured, Is.EqualTo(canonical.Length));
            Assert.That(canonicalAgain, Is.EqualTo(canonical));
            Assert.That(original.Links, Has.Length.EqualTo(2));
            Assert.That(reparsed.Links, Has.Length.EqualTo(2));
        });

        int index = 0;
        while (index < original.Links.Length)
        {
            AssertSemanticLinkEqual(
                original.Links[index],
                reparsed.Links[index]);
            index = index + 1;
        }
    }

    [Test]
    public void CombinedFieldAndParameterOrderingSurviveCanonicalRoundTrip()
    {
        string[] fields = new string[]
        {
            "<first>; z=1; rel=next; REL=ignored; a=2, " +
            "<second>; b=3; flag",
            "<third>; c=4; hreflang=en; hreflang=de"
        };
        IWebLinkParser parser = new WebLinkParser();
        IWebLinkComposer composer = new WebLinkComposer();
        WebLinkParseResult parsed = parser.TryParseLinkFieldValues(
            fields,
            new WebLinkParseOptions());

        string canonical = composer.ComposeLinkFieldValue(
            parsed.LinkSet,
            new OperationLimits());
        WebLinkSet reparsed = parser.ParseLinkFieldValues(
            new string[] { canonical },
            new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(parsed.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.DuplicateParameter));
            Assert.That(reparsed.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "first", "second", "third" }));
            Assert.That(reparsed.Links[0].Parameters.Select(parameter =>
                parameter.Name),
                Is.EqualTo(new string[] { "z", "rel", "REL", "a" }));
            Assert.That(reparsed.Links[0].Parameters.Select(parameter =>
                parameter.Ignored),
                Is.EqualTo(new bool[] { false, false, true, false }));
            Assert.That(reparsed.Links[1].Parameters.Select(parameter =>
                parameter.Name),
                Is.EqualTo(new string[] { "b", "flag" }));
            Assert.That(reparsed.Links[2].Parameters.Select(parameter =>
                parameter.Name),
                Is.EqualTo(new string[] { "c", "hreflang", "hreflang" }));
            Assert.That(reparsed.Links[2].HrefLanguages,
                Is.EqualTo(new string[] { "en", "de" }));
            Assert.That(reparsed.OriginalFieldValues,
                Is.EqualTo(new string[] { canonical }));
        });
    }

    private static IEnumerable<TestCaseData> RoundTripCases()
    {
        string[] targets = new string[]
        {
            "relative/path?x=1#fragment",
            "https://example.test/a,b;c?encoded=%2F#part",
            "//authority.test/path"
        };
        string[] relations = new string[]
        {
            "NEXT previous",
            "https://relations.test/custom",
            "next https://relations.test/other"
        };
        string[] titles = new string[]
        {
            "plain",
            "\"two words\"",
            "\"comma, semicolon; quote\\\" slash\\\\ end\""
        };

        int targetIndex = 0;
        while (targetIndex < targets.Length)
        {
            int relationIndex = 0;
            while (relationIndex < relations.Length)
            {
                int titleIndex = 0;
                while (titleIndex < titles.Length)
                {
                    yield return new TestCaseData(
                        targets[targetIndex],
                        relations[relationIndex],
                        titles[titleIndex]).SetName(
                            "GeneratedWebLinkRoundTrip_" +
                            targetIndex + "_" +
                            relationIndex + "_" +
                            titleIndex);
                    titleIndex = titleIndex + 1;
                }

                relationIndex = relationIndex + 1;
            }

            targetIndex = targetIndex + 1;
        }
    }

    private static void AssertSemanticLinkEqual(
        WebLink expected,
        WebLink actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Target, Is.EqualTo(expected.Target));
            Assert.That(actual.HasAnchor, Is.EqualTo(expected.HasAnchor));
            Assert.That(actual.Anchor, Is.EqualTo(expected.Anchor));
            Assert.That(actual.Relations, Is.EqualTo(expected.Relations));
            Assert.That(actual.RelationKinds,
                Is.EqualTo(expected.RelationKinds));
            Assert.That(actual.ReverseRelations,
                Is.EqualTo(expected.ReverseRelations));
            Assert.That(actual.ReverseRelationKinds,
                Is.EqualTo(expected.ReverseRelationKinds));
            Assert.That(actual.HrefLanguages,
                Is.EqualTo(expected.HrefLanguages));
            Assert.That(actual.HasMedia, Is.EqualTo(expected.HasMedia));
            Assert.That(actual.Media, Is.EqualTo(expected.Media));
            Assert.That(actual.HasTitle, Is.EqualTo(expected.HasTitle));
            Assert.That(actual.Title, Is.EqualTo(expected.Title));
            Assert.That(actual.HasTitleExtended,
                Is.EqualTo(expected.HasTitleExtended));
            Assert.That(actual.TitleExtended,
                Is.EqualTo(expected.TitleExtended));
            Assert.That(actual.TitleExtendedLanguage,
                Is.EqualTo(expected.TitleExtendedLanguage));
            Assert.That(actual.HasType, Is.EqualTo(expected.HasType));
            Assert.That(actual.Type, Is.EqualTo(expected.Type));
            Assert.That(actual.Parameters, Has.Length.EqualTo(
                expected.Parameters.Length));
        });

        int index = 0;
        while (index < expected.Parameters.Length)
        {
            AssertSemanticParameterEqual(
                expected.Parameters[index],
                actual.Parameters[index]);
            index = index + 1;
        }
    }

    private static void AssertSemanticParameterEqual(
        WebLinkParameter expected,
        WebLinkParameter actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.HasValue, Is.EqualTo(expected.HasValue));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.Extended, Is.EqualTo(expected.Extended));
            Assert.That(actual.Language, Is.EqualTo(expected.Language));
            Assert.That(actual.Ignored, Is.EqualTo(expected.Ignored));
        });
    }
}
