namespace uri.Tests;

public class IriUriWorkflowIntegrationTests
{
    [Test]
    public void ConvertedIrisNormalizeResolveAndReturnToReversibleDisplay()
    {
        const string baseIri =
            "HTTPS://例え.テスト/基点/./親/index?base=%2f";
        const string relativeIri =
            "../子/./cafe\u0301?問=%2F#片";
        const string expectedUri =
            "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "%E5%9F%BA%E7%82%B9/%E5%AD%90/caf%C3%A9" +
            "?%E5%95%8F=%2F#%E7%89%87";
        const string expectedDisplay =
            "https://例え.テスト/基点/子/café?問=%2F#片";
        IIriConverter converter = new IriConverter();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriResolver resolver = new UriResolver();
        IUriComparer comparer = new UriComparer();
        IIriParser iriParser = new IriParser();
        IriConversionOptions conversion = new IriConversionOptions();
        conversion.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;

        string baseUri = converter.ConvertIriToUri(baseIri, conversion);
        string relativeUri = converter.ConvertIriToUri(
            relativeIri,
            conversion);
        string normalizedBase = normalizer.NormalizeSyntax(
            baseUri,
            new UriNormalizationOptions());
        string resolved = resolver.ResolveReference(
            normalizedBase,
            relativeUri,
            new UriResolutionOptions());
        string normalizedResolved = normalizer.NormalizeSyntax(
            resolved,
            new UriNormalizationOptions());
        string display = converter.ConvertUriToIri(
            normalizedResolved,
            conversion);
        IriReference displayModel = iriParser.ParseIriReference(
            display,
            new IriParseOptions());
        string remapped = converter.ConvertIriToUri(display, conversion);
        UriComparisonResult comparison = comparer.CompareExact(
            normalizedResolved,
            remapped,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalizedResolved, Is.EqualTo(expectedUri));
            Assert.That(display, Is.EqualTo(expectedDisplay));
            Assert.That(displayModel.Authority.Host,
                Is.EqualTo("例え.テスト"));
            Assert.That(displayModel.Path,
                Is.EqualTo("/基点/子/café"));
            Assert.That(displayModel.Query, Is.EqualTo("問=%2F"));
            Assert.That(displayModel.Fragment, Is.EqualTo("片"));
            Assert.That(remapped, Is.EqualTo(expectedUri));
            Assert.That(comparison.Success, Is.True);
            Assert.That(comparison.Equal, Is.True);
            Assert.That(comparison.DifferentComponent,
                Is.EqualTo(UriComponent.None));
        });
    }

    [Test]
    public void DisplayConversionDecodesUnicodeButPreservesReservedTriplets()
    {
        const string iri =
            "HTTP://Example.TEST/道%2fpart/./子" +
            "?name=問%26x#片%3f";
        const string expectedUri =
            "http://example.test/%E9%81%93%2Fpart/%E5%AD%90" +
            "?name=%E5%95%8F%26x#%E7%89%87%3F";
        const string expectedDisplay =
            "http://example.test/道%2Fpart/子?name=問%26x#片%3F";
        IIriConverter converter = new IriConverter();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer uriComparer = new UriComparer();
        IIriComparer iriComparer = new IriComparer();

        string mapped = converter.ConvertIriToUri(
            iri,
            new IriConversionOptions());
        string normalized = normalizer.NormalizeSyntax(
            mapped,
            new UriNormalizationOptions());
        string display = converter.ConvertUriToIri(
            normalized,
            new IriConversionOptions());
        string remapped = converter.ConvertIriToUri(
            display,
            new IriConversionOptions());
        UriComparisonResult uriComparison = uriComparer.CompareExact(
            normalized,
            remapped,
            new UriComparisonOptions());
        UriComparisonResult iriComparison = iriComparer.CompareIriAsUri(
            iri,
            display,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(expectedUri));
            Assert.That(display, Is.EqualTo(expectedDisplay));
            Assert.That(display, Does.Contain("道%2Fpart"));
            Assert.That(display, Does.Contain("問%26x"));
            Assert.That(display, Does.EndWith("片%3F"));
            Assert.That(remapped, Is.EqualTo(expectedUri));
            Assert.That(uriComparison.Success, Is.True);
            Assert.That(uriComparison.Equal, Is.True);
            Assert.That(iriComparison.Success, Is.True);
            Assert.That(iriComparison.Equal, Is.True);
        });
    }

    [Test]
    public void ResolutionAndDisplayPreserveEmptyQueryAndUnicodeFragment()
    {
        const string baseIri = "https://例え.test/道?";
        const string fragmentIri = "#片";
        const string expectedUri =
            "https://%E4%BE%8B%E3%81%88.test/%E9%81%93?#%E7%89%87";
        const string expectedDisplay = "https://例え.test/道?#片";
        IIriConverter converter = new IriConverter();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriResolver resolver = new UriResolver();
        IUriParser uriParser = new UriParser();
        IIriParser iriParser = new IriParser();

        string baseUri = normalizer.NormalizeSyntax(
            converter.ConvertIriToUri(
                baseIri,
                new IriConversionOptions()),
            new UriNormalizationOptions());
        string fragmentUri = converter.ConvertIriToUri(
            fragmentIri,
            new IriConversionOptions());
        string resolved = resolver.ResolveReference(
            baseUri,
            fragmentUri,
            new UriResolutionOptions());
        string display = converter.ConvertUriToIri(
            resolved,
            new IriConversionOptions());
        string remapped = converter.ConvertIriToUri(
            display,
            new IriConversionOptions());
        UriReference uriModel = uriParser.ParseReference(
            resolved,
            new UriParseOptions());
        IriReference iriModel = iriParser.ParseIriReference(
            display,
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo(expectedUri));
            Assert.That(display, Is.EqualTo(expectedDisplay));
            Assert.That(remapped, Is.EqualTo(expectedUri));
            Assert.That(uriModel.HasQuery, Is.True);
            Assert.That(uriModel.Query, Is.Empty);
            Assert.That(uriModel.HasFragment, Is.True);
            Assert.That(uriModel.Fragment, Is.EqualTo("%E7%89%87"));
            Assert.That(iriModel.HasQuery, Is.True);
            Assert.That(iriModel.Query, Is.Empty);
            Assert.That(iriModel.HasFragment, Is.True);
            Assert.That(iriModel.Fragment, Is.EqualTo("片"));
        });
    }
}
