namespace uri.Tests;

public class Rfc3987ConversionConformanceTests
{
    [TestCase(
        "http://résumé.example.org",
        "http://r%C3%A9sum%C3%A9.example.org")]
    [TestCase(
        "http://validator.w3.org/check?uri=http%3A%2F%2Frésumé.example.org",
        "http://validator.w3.org/check?uri=http%3A%2F%2Fr%C3%A9sum%C3%A9.example.org")]
    [TestCase(
        "http://www.example.org/red%09rosé#red",
        "http://www.example.org/red%09ros%C3%A9#red")]
    [TestCase(
        "http://example.com/𐌀𐌁𐌂",
        "http://example.com/%F0%90%8C%80%F0%90%8C%81%F0%90%8C%82")]
    public void Section31PublishedIriToUriExamplesProduceRfcMappings(
        string iri,
        string expected)
    {
        IIriConverter converter = new IriConverter();

        string result = converter.ConvertIriToUri(
            iri,
            new IriConversionOptions());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(
        "http://www.example.org/D%C3%BCrst",
        "http://www.example.org/Dürst")]
    [TestCase(
        "http://www.example.org/D%FCrst",
        "http://www.example.org/D%FCrst")]
    public void Section321Utf8AndNonUtf8ExamplesFollowAllConversionSteps(
        string uri,
        string expected)
    {
        IIriConverter converter = new IriConverter();

        string result = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Section321FormattingCharacterRemainsEncodedAndPunycodeIsUnchanged()
    {
        IIriConverter converter = new IriConverter();
        string uri = "http://xn--99zt52a.example.org/%e2%80%ae";

        string result = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.ToUpperInvariant(), Is.EqualTo(
                "HTTP://XN--99ZT52A.EXAMPLE.ORG/%E2%80%AE"));
            Assert.That(result, Does.Contain("xn--99zt52a.example.org"));
            Assert.That(result.IndexOf('\u202E'), Is.EqualTo(-1));
        });
    }

    [TestCase("%80")]
    [TestCase("%C0%AF")]
    [TestCase("%E0%80%AF")]
    [TestCase("%ED%A0%80")]
    [TestCase("%F0%80%80%AF")]
    [TestCase("%F4%90%80%80")]
    [TestCase("%F5%80%80%80")]
    [TestCase("%C2")]
    [TestCase("%E2%82")]
    [TestCase("%F0%90%80")]
    public void InvalidAndIncompleteUtf8IsRePercentEncodedWithoutGuessing(
        string encoded)
    {
        IIriConverter converter = new IriConverter();
        string uri = "x:/before" + encoded + "after";

        string iri = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.That(iri, Is.EqualTo(uri));
    }

    [TestCase("%3A")]
    [TestCase("%2F")]
    [TestCase("%3F")]
    [TestCase("%23")]
    [TestCase("%5B")]
    [TestCase("%5D")]
    [TestCase("%40")]
    [TestCase("%21")]
    [TestCase("%24")]
    [TestCase("%26")]
    [TestCase("%27")]
    [TestCase("%28")]
    [TestCase("%29")]
    [TestCase("%2A")]
    [TestCase("%2B")]
    [TestCase("%2C")]
    [TestCase("%3B")]
    [TestCase("%3D")]
    [TestCase("%25")]
    public void EveryPercentEncodedReservedCharacterRemainsEncoded(
        string encoded)
    {
        IIriConverter converter = new IriConverter();
        string uri = "x:/before" + encoded + "after";

        string iri = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.That(iri, Is.EqualTo(uri));
    }

    [Test]
    public void SupplementaryScalarsUseCanonicalFourByteUtf8InBothDirections()
    {
        IIriConverter converter = new IriConverter();
        string iri = "x://𐌀.test/𐌁?𐌂#𐌃";
        string uri = "x://%F0%90%8C%80.test/%F0%90%8C%81" +
            "?%F0%90%8C%82#%F0%90%8C%83";

        string mapped = converter.ConvertIriToUri(
            iri,
            new IriConversionOptions());
        string restored = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mapped, Is.EqualTo(uri));
            Assert.That(restored, Is.EqualTo(iri));
        });
    }

    [Test]
    public void PrivateScalarsDecodeOnlyWhereIqueryAllowsThem()
    {
        IIriConverter converter = new IriConverter();
        string uri = "x://u%EE%80%80@h%EE%80%80/p%EE%80%80" +
            "?q%EE%80%80#f%EE%80%80";
        string expected = "x://u%EE%80%80@h%EE%80%80/p%EE%80%80" +
            "?q\uE000#f%EE%80%80";

        string iri = converter.ConvertUriToIri(
            uri,
            new IriConversionOptions());

        Assert.That(iri, Is.EqualTo(expected));
    }

    [TestCase("x:/%41%C3%BC%2F")]
    [TestCase("x:/%80%C0%AF%E0%80%AF")]
    [TestCase("x:/%3A%2F%3F%23%25")]
    [TestCase("x:/%F0%90%8C%80%F0%90%8C%81")]
    [TestCase("x:/%EF%B7%90%EF%BF%BE")]
    [TestCase("x:/%e2%80%ae")]
    [TestCase("x:/path?q%EE%80%80#fragment")]
    [TestCase("http://example.org/%7euser")]
    public void UriToIriToUriRoundTripsModuloPermittedNormalization(string uri)
    {
        IIriConverter converter = new IriConverter();
        IUriComparer comparer = new UriComparer();
        IriConversionOptions conversionOptions = new IriConversionOptions();

        string iri = converter.ConvertUriToIri(uri, conversionOptions);
        string roundTrip = converter.ConvertIriToUri(iri, conversionOptions);
        UriComparisonResult comparison = comparer.CompareSyntax(
            uri,
            roundTrip,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(IriParserCore.ParseReference(iri.AsSpan()).Success, Is.True);
            Assert.That(UriParserCore.ParseReference(roundTrip.AsSpan()).Success, Is.True);
            Assert.That(comparison.Success, Is.True);
            Assert.That(comparison.Equal, Is.True, uri);
        });
    }

    [TestCase("x://usér@éxample.test/café?q=é#é")]
    [TestCase("x:/cafe\u0301")]
    [TestCase("x:/𐌀𐌁𐌂")]
    [TestCase("x:/path?private=\uE000#fragment")]
    [TestCase("x:/a%2Fb")]
    [TestCase("http://résumé.example.org/")]
    [TestCase("//例え.テスト/道")]
    [TestCase("?問\uE000#片")]
    public void IriToUriToIriRemainsMappedUriEquivalent(string iri)
    {
        IIriConverter converter = new IriConverter();
        IIriComparer comparer = new IriComparer();
        IriConversionOptions conversionOptions = new IriConversionOptions();

        string uri = converter.ConvertIriToUri(iri, conversionOptions);
        string roundTrip = converter.ConvertUriToIri(uri, conversionOptions);
        UriComparisonResult comparison = comparer.CompareIriAsUri(
            iri,
            roundTrip,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(UriParserCore.ParseReference(uri.AsSpan()).Success, Is.True);
            Assert.That(IriParserCore.ParseReference(roundTrip.AsSpan()).Success, Is.True);
            Assert.That(comparison.Success, Is.True);
            Assert.That(comparison.Equal, Is.True, iri);
        });
    }
}
