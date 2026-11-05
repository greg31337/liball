using whois;

namespace whois.Tests;

public class QueryNormalizationTests
{
    [TestCase("EXAMPLE.COM", "example.com")]
    [TestCase("EXAMPLE.COM.", "example.com")]
    [TestCase("  EXAMPLE.COM.  ", "example.com")]
    [TestCase("BÜCHER.Example.", "xn--bcher-kva.example")]
    [TestCase("例え。テスト。", "xn--r8jz45g.xn--zckzah")]
    public void NormalizesAsciiUnicodeAndRootDottedDomains(string input, string expected)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.Domain);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
    }

    [TestCase("0.0.0.0", "0.0.0.0")]
    [TestCase("255.255.255.255", "255.255.255.255")]
    [TestCase("  192.0.2.1  ", "192.0.2.1")]
    public void NormalizesStrictIpv4Variants(string input, string expected)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.IPv4Address);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.IPv4Address));
    }

    [TestCase("2001:0DB8:0000:0000:0000:0000:0000:0001", "2001:db8::1")]
    [TestCase("0:0:0:0:0:0:0:1", "::1")]
    [TestCase("::", "::")]
    [TestCase("::FFFF:192.0.2.128", "::ffff:192.0.2.128")]
    public void CanonicalizesIpv6AndMappedIpv6Variants(string input, string expected)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.IPv6Address);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.IPv6Address));
    }

    [TestCase("198.51.255.200/17", WhoisQueryKind.IPv4Prefix, "198.51.128.0/17")]
    [TestCase("192.0.2.255/31", WhoisQueryKind.IPv4Prefix, "192.0.2.254/31")]
    [TestCase("2001:db8:abcd:ffff::1/57", WhoisQueryKind.IPv6Prefix, "2001:db8:abcd:ff80::/57")]
    [TestCase("::ffff:192.0.2.129/120", WhoisQueryKind.IPv6Prefix, "::ffff:192.0.2.0/120")]
    public void MasksHostBitsForNonByteAlignedPrefixes(string input, WhoisQueryKind kind, string expected)
    {
        WhoisQuery query = Classify(input, kind);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(kind));
    }

    [TestCase("AS0000000016", "AS16")]
    [TestCase("as65536", "AS65536")]
    [TestCase("00065536", "AS65536")]
    [TestCase("  42  ", "AS42")]
    public void NormalizesAsPrefixedAndDecimalAsnForms(string input, string expected)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.AutonomousSystem);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected.ToLowerInvariant()));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.AutonomousSystem));
    }

    [TestCase("4.3.2.1.IN-ADDR.ARPA.", "4.3.2.1.in-addr.arpa")]
    [TestCase("4.3.2.1.in-addr.arpa。", "4.3.2.1.in-addr.arpa")]
    [TestCase("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.B.D.0.1.0.0.2.IP6.ARPA.", "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa")]
    public void NormalizesIpv4AndIpv6ReverseZones(string input, string expected)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.ReverseDomain);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(expected));
        Assert.That(query.RoutingKey, Is.EqualTo(expected));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.ReverseDomain));
    }

    [TestCase("  /e Example.COM  ")]
    [TestCase("domain=Example.COM; mode exact")]
    [TestCase("AS00042")]
    [TestCase("192.0.2.1/24")]
    public void ExplicitFreeFormPreservesExactCallerText(string input)
    {
        WhoisQuery query = Classify(input, WhoisQueryKind.FreeForm);

        Assert.That(query.OriginalText, Is.EqualTo(input));
        Assert.That(query.NormalizedText, Is.EqualTo(input));
        Assert.That(query.RoutingKey, Is.EqualTo(input));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
    }

    private static WhoisQuery Classify(string input, WhoisQueryKind kind)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = kind;
        return classifier.Classify(request);
    }
}
