using whois;

namespace whois.Tests;

public class QueryClassifierTests
{
    [TestCase("Example.COM.", WhoisQueryKind.Domain, "example.com")]
    [TestCase("192.0.2.129", WhoisQueryKind.IPv4Address, "192.0.2.129")]
    [TestCase("2001:0DB8::1", WhoisQueryKind.IPv6Address, "2001:db8::1")]
    [TestCase("192.0.2.129/24", WhoisQueryKind.IPv4Prefix, "192.0.2.0/24")]
    [TestCase("2001:db8::1234/64", WhoisQueryKind.IPv6Prefix, "2001:db8::/64")]
    [TestCase("AS00123", WhoisQueryKind.AutonomousSystem, "AS123")]
    [TestCase("123", WhoisQueryKind.AutonomousSystem, "AS123")]
    [TestCase("1.0.0.127.IN-ADDR.ARPA.", WhoisQueryKind.ReverseDomain, "1.0.0.127.in-addr.arpa")]
    [TestCase("domain example.com", WhoisQueryKind.FreeForm, "domain example.com")]
    public void AutomaticallyClassifiesSupportedQueryForms(string input, WhoisQueryKind expectedKind, string expectedNormalized)
    {
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;

        WhoisQuery query = classifier.Classify(request);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.Kind, Is.EqualTo(expectedKind));
        Assert.That(query.NormalizedText, Is.EqualTo(expectedNormalized));
        Assert.That(query.IsValid, Is.True);
    }

    [Test]
    public void AutomaticallyClassifiesUnicodeDomainThroughIdna()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "BÜCHER.Example.";

        WhoisQuery query = classifier.Classify(request);

        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(query.NormalizedText, Is.EqualTo("xn--bcher-kva.example"));
        Assert.That(query.RoutingKey, Is.EqualTo("xn--bcher-kva.example"));
    }

    [TestCase(WhoisQueryKind.Domain, "Example.COM.", "example.com")]
    [TestCase(WhoisQueryKind.IPv4Address, "192.0.2.1", "192.0.2.1")]
    [TestCase(WhoisQueryKind.IPv6Address, "2001:db8::1", "2001:db8::1")]
    [TestCase(WhoisQueryKind.IPv4Prefix, "192.0.2.129/24", "192.0.2.0/24")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "2001:db8::1/64", "2001:db8::/64")]
    [TestCase(WhoisQueryKind.AutonomousSystem, "as00042", "AS42")]
    [TestCase(WhoisQueryKind.ReverseDomain, "1.0.0.127.in-addr.arpa.", "1.0.0.127.in-addr.arpa")]
    public void HonorsExplicitStructuredKindHints(WhoisQueryKind kind, string input, string expectedNormalized)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = kind;

        WhoisQuery query = classifier.Classify(request);

        Assert.That(query.Kind, Is.EqualTo(kind));
        Assert.That(query.NormalizedText, Is.EqualTo(expectedNormalized));
    }

    [Test]
    public void ExplicitFreeFormPreservesDomainLikeTextExactly()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "  Example.COM  ";
        request.Kind = WhoisQueryKind.FreeForm;

        WhoisQuery query = classifier.Classify(request);

        Assert.That(query.OriginalText, Is.EqualTo("  Example.COM  "));
        Assert.That(query.NormalizedText, Is.EqualTo("  Example.COM  "));
        Assert.That(query.RoutingKey, Is.EqualTo("  Example.COM  "));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
    }

    [TestCase(WhoisQueryKind.IPv4Address, "2001:db8::1")]
    [TestCase(WhoisQueryKind.IPv6Address, "192.0.2.1")]
    [TestCase(WhoisQueryKind.IPv4Prefix, "2001:db8::1/64")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "192.0.2.1/24")]
    [TestCase(WhoisQueryKind.Domain, "192.0.2.1")]
    [TestCase(WhoisQueryKind.ReverseDomain, "example.com")]
    public void RejectsConflictingExplicitKindHints(WhoisQueryKind kind, string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = kind;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Kind"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
        Assert.That(exception.QueryKind, Is.EqualTo(kind));
    }

    [Test]
    public void RejectsUnknownKindHint()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        request.Kind = (WhoisQueryKind)99;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Kind"));
        Assert.That(exception.QueryKind, Is.EqualTo((WhoisQueryKind)99));
    }

    [TestCase("0", "AS0")]
    [TestCase("4294967295", "AS4294967295")]
    public void AcceptsUnsignedThirtyTwoBitAsnBoundaries(string input, string expected)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = WhoisQueryKind.AutonomousSystem;

        WhoisQuery query = classifier.Classify(request);

        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected.ToLowerInvariant()));
    }
}
