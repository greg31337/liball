namespace uri.Tests;

public class WebLinkWorkflowIntegrationTests
{
    [Test]
    public void ParsedLinksConvertResolveFilterSerializeAndReparseAsOneWorkflow()
    {
        const string encodedTarget =
            "%E5%AD%90?%E5%95%8F=#%E7%89%87";
        const string encodedAnchor =
            "../%E6%96%87%E8%84%88/base?%E9%8C%A8=#%E7%AF%80";
        const string targetIri = "子?問=#片";
        const string anchorIri = "../文脈/base?錨=#節";
        const string representationIri =
            "https://例え.テスト/基点/path/index?表現=";
        const string expectedContext =
            "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/base" +
            "?%E9%8C%A8=#%E7%AF%80";
        const string expectedTarget =
            "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/%E5%AD%90" +
            "?%E5%95%8F=#%E7%89%87";
        const string expectedSerialized =
            "<%E5%AD%90?%E5%95%8F=#%E7%89%87>" +
            "; rel=\"next alternate\"" +
            "; anchor=\"../%E6%96%87%E8%84%88/base" +
            "?%E9%8C%A8=#%E7%AF%80\"" +
            "; hreflang=ja; type=\"text/html\"; profile=compact";
        IWebLinkParser parser = new WebLinkParser();
        IWebLinkResolver resolver = new WebLinkResolver();
        IWebLinkQuery query = new WebLinkQuery();
        IWebLinkComposer composer = new WebLinkComposer();
        IIriConverter iriConverter = new IriConverter();

        WebLinkSet parsed = parser.ParseLinkFieldValues(
            new string[]
            {
                "<" + encodedTarget + ">; rel=\"next alternate\"; " +
                "anchor=\"" + encodedAnchor + "\"; hreflang=ja; " +
                "type=\"text/html\"; profile=compact, " +
                "<https://example.test/other>; rel=previous; " +
                "type=\"application/json\""
            },
            new WebLinkParseOptions());
        WebLink selected = parsed.Links[0];
        string convertedTarget = iriConverter.ConvertUriToIri(
            selected.Target,
            new IriConversionOptions());
        string convertedAnchor = iriConverter.ConvertUriToIri(
            selected.Anchor,
            new IriConversionOptions());
        selected.Target = convertedTarget;
        selected.Anchor = convertedAnchor;
        selected.Parameters[1].Value = convertedAnchor;

        WebLinkResolutionResult[] resolutions = resolver.ResolveWebLinks(
            parsed,
            representationIri,
            new OperationLimits());
        WebLinkFilter filter = new WebLinkFilter();
        filter.HasRelation = true;
        filter.Relation = "NEXT";
        filter.HasTarget = true;
        filter.Target = encodedTarget;
        filter.HasContext = true;
        filter.Context = encodedAnchor;
        filter.HasMediaType = true;
        filter.MediaType = "TEXT/HTML";
        filter.HasLanguage = true;
        filter.Language = "JA";
        filter.HasExtensionAttribute = true;
        filter.ExtensionAttributeName = "PROFILE";
        filter.ExtensionAttributeHasValue = true;
        filter.ExtensionAttributeValue = "compact";
        WebLinkSet filtered = query.FilterWebLinks(parsed, filter);
        string serialized = composer.ComposeLinkFieldValue(
            filtered,
            new OperationLimits());
        WebLinkSet reparsed = parser.ParseLinkFieldValues(
            new string[] { serialized },
            new WebLinkParseOptions());
        WebLinkResolutionResult roundTrip = resolver.ResolveWebLink(
            reparsed.Links[0],
            representationIri,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Links, Has.Length.EqualTo(2));
            Assert.That(convertedTarget, Is.EqualTo(targetIri));
            Assert.That(convertedAnchor, Is.EqualTo(anchorIri));
            Assert.That(selected.Target, Is.EqualTo(targetIri));
            Assert.That(selected.Anchor, Is.EqualTo(anchorIri));
            Assert.That(resolutions, Has.Length.EqualTo(2));
            Assert.That(resolutions[0].Success, Is.True);
            Assert.That(resolutions[0].Context, Is.EqualTo(expectedContext));
            Assert.That(resolutions[0].Target, Is.EqualTo(expectedTarget));
            Assert.That(resolutions[0].AnchorHonored, Is.True);
            Assert.That(resolutions[1].Success, Is.True);
            Assert.That(resolutions[1].Target, Is.EqualTo(
                "https://example.test/other"));
            Assert.That(filtered.Links, Is.EqualTo(
                new WebLink[] { selected }));
            Assert.That(serialized, Is.EqualTo(expectedSerialized));
            Assert.That(reparsed.Links, Has.Length.EqualTo(1));
            Assert.That(reparsed.Links[0].Target, Is.EqualTo(encodedTarget));
            Assert.That(reparsed.Links[0].Anchor, Is.EqualTo(encodedAnchor));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Context, Is.EqualTo(expectedContext));
            Assert.That(roundTrip.Target, Is.EqualTo(expectedTarget));
        });
    }
}
