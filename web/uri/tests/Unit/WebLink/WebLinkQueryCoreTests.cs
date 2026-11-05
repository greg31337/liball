namespace uri.Tests;

public class WebLinkQueryCoreTests
{
    [Test]
    public void ActiveCriteriaCombineAndPreserveMatchingSourceOrder()
    {
        WebLinkSet linkSet = Parse(
            "<HTTP://EXAMPLE.COM/%7Edocs/./index>; " +
            "rel=\"NEXT https://REL.EXAMPLE/TYPE?X=Y\"; " +
            "anchor=\"HTTPS://CONTEXT.EXAMPLE/%7Ebase/./\"; " +
            "hreflang=EN-us; type=\"Application/Problem+JSON\"; " +
            "Profile=Compact, " +
            "<http://example.com/other>; rel=next; hreflang=en-US; " +
            "type=\"application/problem+json\"; profile=Compact");
        WebLinkFilter filter = new WebLinkFilter();
        filter.HasRelation = true;
        filter.Relation = "NeXt";
        filter.HasTarget = true;
        filter.Target = "http://example.com/~docs/index";
        filter.HasContext = true;
        filter.Context = "https://context.example/~base/";
        filter.HasMediaType = true;
        filter.MediaType = "application/problem+json";
        filter.HasLanguage = true;
        filter.Language = "en-US";
        filter.HasExtensionAttribute = true;
        filter.ExtensionAttributeName = "profile";
        filter.ExtensionAttributeHasValue = true;
        filter.ExtensionAttributeValue = "Compact";

        WebLinkSet result = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            filter);

        Assert.Multiple(() =>
        {
            Assert.That(result.Links, Has.Length.EqualTo(1));
            Assert.That(result.Links[0], Is.SameAs(linkSet.Links[0]));
            Assert.That(linkSet.Links, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void RelationsUseRfcCaseInsensitiveComparisonAndExcludeRev()
    {
        WebLinkSet linkSet = Parse(
            "<a>; rel=\"NEXT https://REL.EXAMPLE/TYPE?X=Y\", " +
            "<b>; rev=next, " +
            "<c>; rel=\"https://rel.example/other\"");

        WebLinkSet registered = WebLinkQueryCore.FindLinksByRelation(
            linkSet,
            "nExT");
        WebLinkSet extension = WebLinkQueryCore.FindLinksByRelation(
            linkSet,
            "https://rel.example/type?x=y");
        WebLinkSet invalid = WebLinkQueryCore.FindLinksByRelation(
            linkSet,
            "not/a/relation");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Links, Is.EqualTo(
                new WebLink[] { linkSet.Links[0] }));
            Assert.That(extension.Links, Is.EqualTo(
                new WebLink[] { linkSet.Links[0] }));
            Assert.That(invalid.Links, Is.Empty);
        });
    }

    [Test]
    public void TargetMatchingUsesIriMappedUriSyntaxEquivalence()
    {
        WebLink first = new WebLink();
        first.Target =
            "HTTP://例え.テスト/%7Euser/./道?Query#Fragment";
        WebLink second = new WebLink();
        second.Target = "x:/a%2Fb";
        WebLink third = new WebLink();
        third.Target = "x:/a/b";
        WebLinkSet linkSet = Set(first, second, third);

        WebLinkSet equivalent = WebLinkQueryCore.FindLinksByTarget(
            linkSet,
            "http://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "~user/%E9%81%93?Query#Fragment");
        WebLinkSet encodedDelimiter =
            WebLinkQueryCore.FindLinksByTarget(linkSet, "x:/a%2Fb");

        Assert.Multiple(() =>
        {
            Assert.That(equivalent.Links,
                Is.EqualTo(new WebLink[] { first }));
            Assert.That(encodedDelimiter.Links,
                Is.EqualTo(new WebLink[] { second }));
        });
    }

    [Test]
    public void ContextMatchingUsesOnlyExplicitEffectiveAnchorContext()
    {
        WebLink anchored = Parse(
            "<target>; anchor=\"HTTPS://EXAMPLE.TEST/%7Econtext/./\"")
            .Links[0];
        WebLink anonymous = Parse("<target>").Links[0];
        WebLinkSet linkSet = Set(anonymous, anchored);

        WebLinkSet result = WebLinkQueryCore.FindLinksByContext(
            linkSet,
            "https://example.test/~context/");

        Assert.That(result.Links,
            Is.EqualTo(new WebLink[] { anchored }));
    }

    [Test]
    public void MediaTypesAndLanguageTagsCompareCaseInsensitivelyButExactly()
    {
        WebLinkSet linkSet = Parse(
            "<a>; type=\"Application/Problem+JSON\"; " +
            "hreflang=en-US; hreflang=DE-ch, " +
            "<b>; type=\"application/json\"; hreflang=en");
        WebLinkFilter matching = new WebLinkFilter();
        matching.HasMediaType = true;
        matching.MediaType = "application/problem+json";
        matching.HasLanguage = true;
        matching.Language = "de-CH";
        WebLinkFilter languagePrefix = new WebLinkFilter();
        languagePrefix.HasLanguage = true;
        languagePrefix.Language = "en";

        WebLinkSet combined = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            matching);
        WebLinkSet exactLanguage = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            languagePrefix);

        Assert.Multiple(() =>
        {
            Assert.That(combined.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(exactLanguage.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[1] }));
        });
    }

    [Test]
    public void ExtensionNamesIgnoreCaseWhileUnknownValuesRemainCaseSensitive()
    {
        WebLinkSet linkSet = Parse(
            "<a>; Profile=Compact, <b>; profile=compact, " +
            "<c>; flag, <d>; title=Compact");
        WebLinkFilter nameOnly = ExtensionFilter("PROFILE", false, "");
        WebLinkFilter exactValue = ExtensionFilter(
            "profile",
            true,
            "Compact");
        WebLinkFilter lowerValue = ExtensionFilter(
            "profile",
            true,
            "compact");
        WebLinkFilter flagValue = ExtensionFilter("flag", true, "");

        WebLinkSet names = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            nameOnly);
        WebLinkSet exact = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            exactValue);
        WebLinkSet lower = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            lowerValue);
        WebLinkSet flag = WebLinkQueryCore.FilterWebLinks(
            linkSet,
            flagValue);

        Assert.Multiple(() =>
        {
            Assert.That(names.Links, Is.EqualTo(new WebLink[]
            {
                linkSet.Links[0],
                linkSet.Links[1]
            }));
            Assert.That(exact.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[0] }));
            Assert.That(lower.Links,
                Is.EqualTo(new WebLink[] { linkSet.Links[1] }));
            Assert.That(flag.Links, Is.Empty);
        });
    }

    [Test]
    public void FilteringPreservesSetMetadataAndDoesNotAliasCollections()
    {
        WebLinkSet linkSet = Parse(
            "<a>; rel=next; REL=previous, <b>; rel=previous");
        WebLinkFilter all = new WebLinkFilter();

        WebLinkSet result = WebLinkQueryCore.FilterWebLinks(linkSet, all);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(linkSet));
            Assert.That(result.Links, Is.Not.SameAs(linkSet.Links));
            Assert.That(result.Links, Is.EqualTo(linkSet.Links));
            Assert.That(result.OriginalFieldValues,
                Is.Not.SameAs(linkSet.OriginalFieldValues));
            Assert.That(result.OriginalFieldValues,
                Is.EqualTo(linkSet.OriginalFieldValues));
            Assert.That(result.Diagnostics,
                Is.Not.SameAs(linkSet.Diagnostics));
            Assert.That(result.Diagnostics,
                Is.EqualTo(linkSet.Diagnostics));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void InvalidQueryModelsAreRejectedWithoutMutatingInput()
    {
        WebLinkSet linkSet = Set(new WebLink());
        WebLinkFilter invalid = new WebLinkFilter();
        invalid.Target = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                WebLinkQueryCore.FilterWebLinks(null, new WebLinkFilter()));
            Assert.Throws<ArgumentException>(() =>
                WebLinkQueryCore.FilterWebLinks(linkSet, invalid));
            Assert.Throws<ArgumentException>(() =>
                WebLinkQueryCore.FindLinksByTarget(linkSet, null));
            Assert.That(linkSet.Links, Has.Length.EqualTo(1));
        });
    }

    private static WebLinkFilter ExtensionFilter(
        string name,
        bool hasValue,
        string value)
    {
        WebLinkFilter filter = new WebLinkFilter();
        filter.HasExtensionAttribute = true;
        filter.ExtensionAttributeName = name;
        filter.ExtensionAttributeHasValue = hasValue;
        filter.ExtensionAttributeValue = value;
        return filter;
    }

    private static WebLinkSet Parse(string text)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.LinkSet;
    }

    private static WebLinkSet Set(params WebLink[] links)
    {
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = links;
        return linkSet;
    }
}
