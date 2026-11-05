using whois;

namespace whois.Tests;

public class CidrParserTests
{
    [TestCase("192.0.2.129/24", "192.0.2.0/24")]
    [TestCase("192.0.2.255/25", "192.0.2.128/25")]
    [TestCase("203.0.113.9/0", "0.0.0.0/0")]
    [TestCase("203.0.113.9/32", "203.0.113.9/32")]
    [TestCase("192.0.2.129/024", "192.0.2.0/24")]
    public void MasksAndCanonicalizesIpv4Prefixes(string input, string expected)
    {
        WhoisQuery query = CidrParser.ParsePrefix(input);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.IPv4Prefix));
        Assert.That(query.IsValid, Is.True);
    }

    [TestCase("2001:DB8:ABCD:12::1/48", "2001:db8:abcd::/48")]
    [TestCase("2001:0DB8:ABCD:0012:3456:789A:BCDE:F012/73", "2001:db8:abcd:12:3400::/73")]
    [TestCase("2001:db8::1/0", "::/0")]
    [TestCase("2001:db8::1/128", "2001:db8::1/128")]
    public void MasksAndCanonicalizesIpv6Prefixes(string input, string expected)
    {
        WhoisQuery query = CidrParser.ParsePrefix(input);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.IPv6Prefix));
        Assert.That(query.IsValid, Is.True);
    }

    [Test]
    public void TrimsRoutingWhitespaceWhilePreservingOriginalPrefix()
    {
        WhoisQuery query = CidrParser.ParsePrefix("  192.0.2.129/24  ");

        Assert.That(query.OriginalText, Is.EqualTo("  192.0.2.129/24  "));
        Assert.That(query.NormalizedText, Is.EqualTo("192.0.2.0/24"));
    }

    [TestCase("192.0.2.1")]
    [TestCase("/24")]
    [TestCase("192.0.2.1/")]
    [TestCase("192.0.2.1/24/1")]
    public void RejectsMissingOrRepeatedPrefixSeparators(string input)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => CidrParser.ParsePrefix(input));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
    }

    [TestCase("192.0.2.1/-1")]
    [TestCase("192.0.2.1/+1")]
    [TestCase("192.0.2.1/a")]
    [TestCase("192.0.2.1/1234")]
    public void RejectsNonnumericOrOversizedPrefixText(string input)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => CidrParser.ParsePrefix(input));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
    }

    [TestCase("192.0.2.1/33", WhoisQueryKind.IPv4Prefix)]
    [TestCase("2001:db8::1/129", WhoisQueryKind.IPv6Prefix)]
    public void RejectsWidthsBeyondAddressFamily(string input, WhoisQueryKind expectedKind)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => CidrParser.ParsePrefix(input));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryKind, Is.EqualTo(expectedKind));
    }

    [TestCase("example.com/24")]
    [TestCase("999.1.1.1/24")]
    [TestCase("2001:db8::g/64")]
    [TestCase("[2001:db8::1]/64")]
    [TestCase("fe80::1%4/64")]
    public void RejectsNonIpScopedAndBracketedAddresses(string input)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => CidrParser.ParsePrefix(input));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
    }
}
